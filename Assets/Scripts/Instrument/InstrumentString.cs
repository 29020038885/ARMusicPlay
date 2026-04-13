using UnityEngine;
using UnityEngine.Rendering;
using System;

/// <summary>
/// 乐器琴弦 - 可以被触碰产生声音
/// </summary>
[RequireComponent(typeof(Collider))]
public class InstrumentString : MonoBehaviour
{
    [Header("弦的属性")]
    [Tooltip("弦的编号（从0开始）")]
    public int stringIndex = 0;

    [Tooltip("基础音高（C4 = 60 in MIDI）")]
    public int basePitch = 60;

    [Header("音频设置")]
    [Tooltip("该弦的音频片段数组 - 对应不同的品位")]
    public AudioClip[] fretSounds;

    [Tooltip("空弦音（不按品时的声音）")]
    public AudioClip openStringSound;

    [Header("音量控制")]
    [Tooltip("该弦的基础响度（用于整体混音微调），1 为默认响度")]
    [Range(0f, 2f)]
    public float stringVolume = 1f;

    [Tooltip("全局基础音量（一般保持 1，特殊需要时可整体放大/压低该弦）")]
    [Range(0f, 2f)]
    public float baseVolume = 1f;
    
    [Header("🎼 智能音高调整")]
    [Tooltip("是否使用音高变调（只需要空弦音源，自动生成品位音）")]
    public bool usePitchShifting = true;
    
    [Tooltip("是否优先使用 fretSounds 数组中的音频（如果有）")]
    public bool preferCustomFretSounds = true;

    [Header("🎨 模型琴弦设置（视觉仅震动，不改材质颜色）")]
    [Tooltip("可选：你的模型琴弦（视觉模型）。如果设置，震动效果将应用到这个物体上")]
    public Transform visualStringObject;
    
    [Tooltip("是否也震动脚本所在的物体（碰撞琴弦）")]
    public bool alsoVibrateCollider = false;
    
    [Header("🔍 碰撞琴弦可见性")]
    [Tooltip("让碰撞琴弦透明（推荐：如果有单独的模型琴弦）")]
    public bool makeColliderInvisible = false;
    
    [Range(0f, 1f)]
    [Tooltip("碰撞琴弦的透明度（0=完全透明，1=完全不透明）")]
    public float colliderAlpha = 0.1f;

    [Header("线条材质（LineRenderer）")]
    [Tooltip("可选：指定 LineRenderer 材质。不指定时若材质为空或错误 Shader，会自动使用完全透明的 URP 默认线条材质（避免全粉弦，碰撞仍在）")]
    public Material lineRendererMaterialOverride;

    [Header("状态")]
    [Tooltip("当前是否被按住品")]
    public bool isFretPressed = false;

    [Tooltip("当前按住的品位（-1表示空弦）")]
    public int currentFret = -1;

    [Tooltip("是否正在播放声音")]
    public bool isPlaying = false;

    // 事件：当弦被拨动时触发
    public event Action<int, int> OnStringPlucked; // stringIndex, fretIndex

    private Renderer stringRenderer;
    private AudioSource audioSource;
    private float vibrationTime = 0f;
    private float vibrationDuration = 0.5f;
    
    // 🆕 用于震动的物体
    private Transform vibrationTarget;
    private Vector3 originalScale; // 震动目标的原始缩放
    private Vector3 colliderOriginalScale; // 碰撞琴弦的原始缩放（如果需要同时震动）
    private Renderer visualRenderer; // 模型琴弦的 Renderer
    private Coroutine currentVibrationCoroutine; // 当前正在运行的震动协程
    /// <summary>拨弦时的基础音高倍率（不含推拉），用于拨弦后推拉时实时改音高</summary>
    private float sustainBasePitchMultiplier = 1f;

    void Awake()
    {
        var lr = GetComponent<LineRenderer>();
        if (lr == null) return;

        if (lineRendererMaterialOverride != null)
        {
            lr.sharedMaterial = lineRendererMaterialOverride;
            return;
        }

        if (LineRendererNeedsDefaultMaterial(lr))
        {
            Material fallback = InstrumentStringLineMaterialCache.Get();
            if (fallback != null)
                lr.sharedMaterial = fallback;
        }
    }

    static bool LineRendererNeedsDefaultMaterial(LineRenderer lr)
    {
        Material[] mats = lr.sharedMaterials;
        if (mats == null || mats.Length == 0) return true;
        foreach (Material m in mats)
        {
            if (m == null) return true;
            if (m.shader == null) return true;
            if (m.shader.name == "Hidden/InternalErrorShader") return true;
        }
        return false;
    }

    void Start()
    {
        // 获取或添加组件
        stringRenderer = GetComponent<Renderer>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 配置音频源
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.5f; // 半3D音效

        // 🆕 确定震动目标
        if (visualStringObject != null)
        {
            // 使用模型琴弦作为震动目标
            vibrationTarget = visualStringObject;
            visualRenderer = visualStringObject.GetComponent<Renderer>();
            Debug.Log($"✅ 弦 {stringIndex} 使用模型琴弦进行震动：{visualStringObject.name}");
        }
        else
        {
            // 使用脚本所在物体作为震动目标
            vibrationTarget = transform;
            visualRenderer = stringRenderer;
            Debug.Log($"✅ 弦 {stringIndex} 使用碰撞琴弦进行震动");
        }
        
        // 保存原始缩放（在 Start 时保存，确保是真实的原始值）
        if (vibrationTarget != null)
        {
            originalScale = vibrationTarget.localScale;
        }
        
        // 保存碰撞琴弦的原始缩放（如果需要同时震动）
        colliderOriginalScale = transform.localScale;

        // 确保有 Collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"InstrumentString on {gameObject.name}: 没有 Collider，添加 BoxCollider");
            gameObject.AddComponent<BoxCollider>();
        }
        
        // 🆕 如果需要，让碰撞琴弦透明
        if (makeColliderInvisible && stringRenderer != null)
        {
            SetColliderTransparency(colliderAlpha);
        }
    }
    
    /// <summary>
    /// 设置碰撞琴弦的透明度
    /// </summary>
    void SetColliderTransparency(float alpha)
    {
        if (stringRenderer == null) return;
        
        // 获取材质的副本（避免修改共享材质）
        Material mat = stringRenderer.material;
        
        if (mat == null)
        {
            Debug.LogWarning($"⚠️ 弦 {stringIndex} 的碰撞琴弦没有材质");
            return;
        }
        
        // 检查并设置渲染模式为支持透明的模式
        if (mat.HasProperty("_Mode"))
        {
            // Standard Shader
            mat.SetFloat("_Mode", 3); // 3 = Transparent mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
        
        // 设置透明度
        Color color = mat.color;
        color.a = alpha;
        mat.color = color;
        
        Debug.Log($"✅ 弦 {stringIndex} 碰撞琴弦透明度设置为 {alpha:F2}");
    }

    void Update()
    {
        // 处理弦的振动视觉效果
        if (isPlaying && vibrationTime > 0)
        {
            vibrationTime -= Time.deltaTime;
            if (vibrationTime <= 0)
            {
                ResetStringVisual();
            }
        }
    }
    
    void OnDisable()
    {
        // 🔧 当物体被禁用时，确保停止协程并恢复位置
        if (currentVibrationCoroutine != null)
        {
            StopCoroutine(currentVibrationCoroutine);
            currentVibrationCoroutine = null;
        }
        ForceResetScale();
    }

    /// <summary>
    /// 按住品位
    /// </summary>
    /// <param name="fretIndex">品位索引（0表示第一品）</param>
    public void PressFret(int fretIndex)
    {
        isFretPressed = true;
        currentFret = fretIndex;
        Debug.Log($"弦 {stringIndex} 按住第 {fretIndex} 品");
    }

    /// <summary>
    /// 释放品位
    /// </summary>
    public void ReleaseFret()
    {
        isFretPressed = false;
        currentFret = -1;
        Debug.Log($"弦 {stringIndex} 释放品位");
    }

    /// <summary>
    /// 拨弦 - 主要交互方法
    /// </summary>
    public void PluckString()
    {
        PluckStringWithBend(0f);
    }

    /// <summary>
    /// 拨弦并施加推拉弦导致的音高偏移（同一品上推拉，bendSemitones 为正=推高，负=拉低）
    /// </summary>
    /// <param name="bendSemitones">推拉导致的半音偏移，如 0.5 表示约半音，-0.5 表示略低</param>
    public void PluckStringWithBend(float bendSemitones)
    {
        Debug.Log($"🎵 PluckString 被调用！弦 {stringIndex}" + (Mathf.Abs(bendSemitones) > 0.01f ? $" 推拉 {bendSemitones:F2} 半音" : ""));
        
        // 选择要播放的音频
        AudioClip clipToPlay = null;
        float pitchShift = 1.0f; // 音高倍率（1.0 = 原始音高）

        if (isFretPressed && currentFret >= 0)
        {
            // 按住品位时
            
            // 优先使用自定义品位音频（如果有）
            if (preferCustomFretSounds && fretSounds != null && currentFret < fretSounds.Length && fretSounds[currentFret] != null)
            {
                clipToPlay = fretSounds[currentFret];
                Debug.Log($"✅ 拨动弦 {stringIndex}，品位 {currentFret}（使用自定义音频）");
            }
            // 否则使用空弦音源 + 音高变调
            else if (usePitchShifting && openStringSound != null)
            {
                clipToPlay = openStringSound;
                // 计算音高倍率：当前项目约定 0=泛音点，1=第一品，2=第二品...
                // 2^(currentFret/12)，第一品≈1.059，第二品≈1.122…
                pitchShift = Mathf.Pow(2f, currentFret / 12f);
                Debug.Log($"✅ 拨动弦 {stringIndex}，品位 {currentFret}（音高变调 x{pitchShift:F3}）");
            }
            // 如果都没有，尝试使用 InstrumentAudioManager 生成
            else
            {
                clipToPlay = TryGetAudioFromManager(currentFret);
                if (clipToPlay != null)
                {
                    Debug.Log($"✅ 拨动弦 {stringIndex}，品位 {currentFret}（使用音频管理器）");
                }
            }
        }
        else
        {
            // 空弦音
            clipToPlay = openStringSound;
            
            // 如果没有空弦音，尝试从管理器获取
            if (clipToPlay == null)
            {
                clipToPlay = TryGetAudioFromManager(-1);
            }
            
            Debug.Log($"✅ 拨动弦 {stringIndex}，空弦");
        }

        // 检查音频源
        if (audioSource == null)
        {
            Debug.LogError($"❌ 弦 {stringIndex} 没有 AudioSource 组件！");
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.5f;
        }

        // 播放声音（拨弦时记下基础音高，拨弦后推拉可实时改音高）
        if (clipToPlay != null && audioSource != null)
        {
            sustainBasePitchMultiplier = pitchShift;
            audioSource.clip = clipToPlay;
            float bendMult = Mathf.Pow(2f, bendSemitones / 12f);
            audioSource.pitch = pitchShift * bendMult;
            audioSource.volume = CalculateCurrentVolume();
            audioSource.Play();
            isPlaying = true;
            
            Debug.Log($"🔊 播放音频：{clipToPlay.name}，音量：{audioSource.volume}，音高：{pitchShift:F3}");
            
            // 触发事件
            OnStringPlucked?.Invoke(stringIndex, currentFret);
            
            // 视觉反馈
            ShowStringVibration();
        }
        else
        {
            if (clipToPlay == null)
            {
                Debug.LogWarning($"⚠️ 弦 {stringIndex} 没有可播放的音频！" +
                    $"\n  - openStringSound: {(openStringSound != null ? "✅" : "❌")}" +
                    $"\n  - fretSounds 数量: {(fretSounds != null ? fretSounds.Length : 0)}" +
                    $"\n  - usePitchShifting: {usePitchShifting}" +
                    $"\n  提示：勾选 usePitchShifting 并设置 openStringSound 即可自动生成品位音");
            }
        }
    }

    /// <summary>
    /// 拨弦后推拉：在持续发音期间根据当前推拉量实时更新音高（真实奏法：先拨弦，再推拉）
    /// </summary>
    public void UpdateSustainBend(float bendSemitones)
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            float bendMult = Mathf.Pow(2f, Mathf.Clamp(bendSemitones, -2f, 2f) / 12f);
            audioSource.pitch = sustainBasePitchMultiplier * bendMult;
        }
    }

    /// <summary>
    /// 拨弦后按品/推拉：在持续发音期间用「当前」按品与推拉对应的总音高倍率更新（先弹弦后按品，音也会变成对应的音）
    /// </summary>
    /// <param name="totalPitchMultiplier">当前应有的总音高倍率（按品+推拉已算好）</param>
    public void UpdateSustainPitch(float totalPitchMultiplier)
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.pitch = Mathf.Clamp(totalPitchMultiplier, 0.5f, 4f);
    }

    /// <summary>
    /// 根据品位计算音高倍率（空弦=1，第 n 品 = 2^(n/12)；本项目中 0 由控制器作为泛音点处理）
    /// </summary>
    public float GetPitchMultiplierForFret(int fretIndex)
    {
        if (fretIndex < 0) return 1f;
        return Mathf.Pow(2f, fretIndex / 12f);
    }

    /// <summary>
    /// 泛音拨弦（古琴泛音奏法）：用空弦音源做音高倍率播放，不按品
    /// </summary>
    /// <param name="pitchMultiplier">泛音倍率，如 2=1/2弦（高八度），3=1/3弦，4=1/4弦…</param>
    public void PluckStringHarmonic(float pitchMultiplier)
    {
        AudioClip clipToPlay = openStringSound ?? TryGetAudioFromManager(-1);
        if (clipToPlay == null)
        {
            Debug.LogWarning($"弦 {stringIndex} 无空弦音源，无法播放泛音");
            return;
        }
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.5f;
        }
        audioSource.clip = clipToPlay;
        audioSource.pitch = pitchMultiplier;
        audioSource.volume = CalculateCurrentVolume();
        audioSource.Play();
        isPlaying = true;
        OnStringPlucked?.Invoke(stringIndex, -1);
        ShowStringVibration();
        if (Debug.isDebugBuild) Debug.Log($"🎵 泛音 弦{stringIndex} 倍率{pitchMultiplier:F2}");
    }

    /// <summary>
    /// 计算当前应使用的音量（包含基础音量、每弦音量、乐器主音量）
    /// </summary>
    float CalculateCurrentVolume()
    {
        float instrumentVolume = 1f;

        // 优先判断父级是否为古琴或琵琶控制器，从中读取主音量
        GuqinController guqin = GetComponentInParent<GuqinController>();
        if (guqin != null)
            instrumentVolume = Mathf.Clamp01(guqin.masterVolume);
        else
        {
            PipaController pipa = GetComponentInParent<PipaController>();
            if (pipa != null)
                instrumentVolume = Mathf.Clamp01(pipa.masterVolume);
        }

        // baseVolume 和 stringVolume 允许适当放大/压低（0~2），最终 Clamp 到 0~1
        float vol = baseVolume * stringVolume * instrumentVolume;
        return Mathf.Clamp01(vol);
    }
    
    /// <summary>
    /// 尝试从 InstrumentAudioManager 获取音频
    /// </summary>
    AudioClip TryGetAudioFromManager(int fretIndex)
    {
        InstrumentAudioManager audioManager = FindObjectOfType<InstrumentAudioManager>();
        if (audioManager != null)
        {
            // 根据父物体判断是古琴还是琵琶
            GuqinController guqin = GetComponentInParent<GuqinController>();
            if (guqin != null)
            {
                return audioManager.GetGuqinAudio(stringIndex, fretIndex);
            }
            
            PipaController pipa = GetComponentInParent<PipaController>();
            if (pipa != null)
            {
                return audioManager.GetPipaAudio(stringIndex, fretIndex);
            }
        }
        return null;
    }

    /// <summary>
    /// 显示弦的振动效果
    /// </summary>
    void ShowStringVibration()
    {
        vibrationTime = vibrationDuration;

        // 🔧 停止之前的震动协程（如果有）
        if (currentVibrationCoroutine != null)
        {
            StopCoroutine(currentVibrationCoroutine);
            // 立即恢复位置
            ForceResetScale();
        }
        
        // 添加缩放动画效果
        currentVibrationCoroutine = StartCoroutine(VibrateEffect());
    }
    
    /// <summary>
    /// 振动效果协程
    /// </summary>
    System.Collections.IEnumerator VibrateEffect()
    {
        // 如果没有震动目标，直接返回
        if (vibrationTarget == null)
        {
            Debug.LogWarning($"⚠️ 弦 {stringIndex} 没有震动目标");
            currentVibrationCoroutine = null;
            yield break;
        }
        
        // 🔧 使用 Start() 中保存的原始缩放值，而不是当前值
        float elapsed = 0f;
        
        Debug.Log($"🎵 开始震动：{vibrationTarget.name}，原始缩放：{originalScale}");
        
        while (elapsed < vibrationDuration)
        {
            float progress = elapsed / vibrationDuration;
            // 🔧 减弱震动幅度：从 0.1f 改为 0.03f
            float scale = 1f + Mathf.Sin(progress * Mathf.PI * 10) * 0.03f;
            
            // 🔧 基于原始缩放值进行缩放
            vibrationTarget.localScale = originalScale * scale;
            
            // 如果同时震动碰撞琴弦
            if (alsoVibrateCollider && visualStringObject != null && transform != vibrationTarget)
            {
                transform.localScale = colliderOriginalScale * scale;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 🔧 强制恢复到原始缩放（使用保存的原始值）
        vibrationTarget.localScale = originalScale;
        
        if (alsoVibrateCollider && visualStringObject != null && transform != vibrationTarget)
        {
            transform.localScale = colliderOriginalScale;
        }
        
        Debug.Log($"✅ 震动完成：{vibrationTarget.name}，恢复到：{originalScale}");
        
        // 清除协程引用
        currentVibrationCoroutine = null;
    }
    
    /// <summary>
    /// 强制重置缩放到原始值
    /// </summary>
    void ForceResetScale()
    {
        if (vibrationTarget != null)
        {
            vibrationTarget.localScale = originalScale;
        }
        
        if (alsoVibrateCollider && visualStringObject != null && transform != vibrationTarget)
        {
            transform.localScale = colliderOriginalScale;
        }
        
        if (showDebug)
            Debug.Log($"🔄 强制恢复缩放：{vibrationTarget?.name} → {originalScale}");
    }
    
    // 🆕 添加公开字段以便在 Inspector 中查看调试信息
    private bool showDebug => true; // 始终显示调试信息

    /// <summary>
    /// 重置弦的视觉效果
    /// </summary>
    void ResetStringVisual()
    {
        isPlaying = false;

        // 🔧 同时确保缩放恢复到原始值
        ForceResetScale();
    }

    /// <summary>
    /// 停止播放
    /// </summary>
    public void StopPlaying()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        // 🔧 停止震动协程
        if (currentVibrationCoroutine != null)
        {
            StopCoroutine(currentVibrationCoroutine);
            currentVibrationCoroutine = null;
        }
        
        isPlaying = false;
        ResetStringVisual();
    }

    /// <summary>
    /// 设置音频片段
    /// </summary>
    public void SetAudioClips(AudioClip openString, AudioClip[] frets)
    {
        openStringSound = openString;
        fretSounds = frets;
    }
}

/// <summary>
/// LineRenderer 未指定材质时 Unity 会显示粉色；此处用 URP 常见 Shader 生成一份共享默认材质（完全透明线条，仅保留碰撞/逻辑）。
/// </summary>
static class InstrumentStringLineMaterialCache
{
    /// <summary>默认线条不透明度；0 为完全透明，1 为不透明。</summary>
    public const float DefaultLineAlpha = 0f;

    static Material s_mat;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ClearCache()
    {
        s_mat = null;
    }

    public static Material Get()
    {
        if (s_mat != null) return s_mat;

        // Particles/Unlit 对 LineRenderer + 透明度通常最省事
        string[] shaderNames =
        {
            "Universal Render Pipeline/Particles/Unlit",
            "Universal Render Pipeline/Unlit",
            "Universal Render Pipeline/Lit",
            "Sprites/Default",
            "Unlit/Color",
        };

        foreach (string name in shaderNames)
        {
            Shader s = Shader.Find(name);
            if (s == null) continue;

            s_mat = new Material(s) { name = "InstrumentString_DefaultLine" };
            var baseColor = new Color(1f, 1f, 1f, DefaultLineAlpha);

            if (s.name.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal))
                ApplyUrpTransparentLine(s_mat, baseColor);
            else
                ApplyBuiltinTransparentLine(s_mat, baseColor);

            return s_mat;
        }

        Debug.LogError("InstrumentString: 找不到可用的线条 Shader（URP Unlit / Lit 等），琴弦仍可能显示为粉色。请在 InstrumentString 上指定 lineRendererMaterialOverride。");
        return null;
    }

    static void ApplyUrpTransparentLine(Material m, Color baseColor)
    {
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", baseColor);
        if (m.HasProperty("_Color"))
            m.SetColor("_Color", baseColor);

        if (m.HasProperty("_Surface"))
        {
            m.SetFloat("_Surface", 1f);
            if (m.HasProperty("_Blend"))
                m.SetFloat("_Blend", 0f);
            if (m.HasProperty("_AlphaClip"))
                m.SetFloat("_AlphaClip", 0f);
            if (m.HasProperty("_ZWrite"))
                m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_SURFACE_TYPE_OPAQUE");
        }

        m.renderQueue = (int)RenderQueue.Transparent;
    }

    static void ApplyBuiltinTransparentLine(Material m, Color baseColor)
    {
        if (m.HasProperty("_Color"))
            m.SetColor("_Color", baseColor);
        m.renderQueue = (int)RenderQueue.Transparent;
    }
}

