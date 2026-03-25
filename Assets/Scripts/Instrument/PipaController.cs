using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 琵琶奏法：散音、按音（按品/滑音）、泛音、扫弦、推拉（同品推拉弦）
/// </summary>
public enum PipaTechnique
{
    SanYin = 0,
    AnYin = 1,
    FanYin = 2,
    Strum = 3,
    TuiLa = 4
}

/// <summary>
/// 琵琶控制器 - 管理琵琶的4根弦和交互
/// </summary>
public class PipaController : MonoBehaviour
{
    [Header("琵琶设置")]
    [Tooltip("琵琶的4根弦")]
    public InstrumentString[] strings = new InstrumentString[4];

    [Tooltip("品位标记物体数组")]
    public Transform[] fretMarkers;

    [Header("音域设置")]
    [Tooltip("4根弦的基础音高（MIDI音符）- 从低到高")]
    public int[] stringBasePitches = new int[4] 
    { 
        50, // D3 - 第一弦（最粗，最低音）
        55, // G3 - 第二弦
        59, // B3 - 第三弦
        62  // D4 - 第四弦（最细，最高音）
    };

    [Header("音量控制")]
    [Tooltip("琵琶主音量（0~1），可由 UI 滑块控制整体响度")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;

    [Tooltip("品位数量（琵琶通常有24-30个品）")]
    public int numberOfFrets = 24;

    [Header("音频资源")]
    [Tooltip("琵琶音频片段 - 4根弦 x N个品位")]
    public AudioClip[][] stringAudioClips;

    [Tooltip("是否使用程序化生成音频（如果没有音频文件）")]
    public bool useProceduralAudio = false;

    [Header("品位检测")]
    [Tooltip("品位检测的碰撞体（旧版）")]
    public Collider[] fretColliders;

    [Tooltip("是否为每根弦创建独立品位碰撞体（推荐）")]
    public bool createFretCollidersPerString = true;

    [Tooltip("每根弦的品位碰撞体 [弦索引][品索引]")]
    public Collider[][] fretCollidersPerString;

    [Tooltip("品位区域的宽度")]
    public float fretWidth = 0.15f;

    [Tooltip("品位区域的高度")]
    public float fretHeight = 0.01f;

    [Header("琵琶特性")]
    [Tooltip("是否启用推弦效果")]
    public bool enableBending = true;

    [Tooltip("是否启用扫弦模式")]
    public bool enableStrumming = true;

    [Header("奏法（PC 数字键 / 手机由触控决定）")]
    [Tooltip("当前奏法：散音/按音/泛音/扫弦")]
    public PipaTechnique currentTechnique = PipaTechnique.AnYin;

    [Header("调试")]
    [Tooltip("显示调试信息")]
    public bool showDebug = true;

    private Dictionary<int, int> currentFretPositions = new Dictionary<int, int>();
    private Dictionary<int, int> recordedFretPositions = new Dictionary<int, int>();
    private Dictionary<int, int> liveFretHoldByString = new Dictionary<int, int>();
    private Dictionary<int, float> liveBendByString = new Dictionary<int, float>();
    private InstrumentInputManager inputManager;

    private static readonly float[] HarmonicPitchMultipliers = { 2f, 3f, 4f, 5f, 6f, 8f };
    private bool isPressingFret = false;
    private int lastPressedFretIndex = -1;

    void Start()
    {
        // 自动查找弦对象（如果没有手动指定）
        if (strings == null || strings.Length == 0 || strings[0] == null)
        {
            AutoFindStrings();
        }

        // 初始化每根弦
        InitializeStrings();

        // 获取输入管理器
        inputManager = FindObjectOfType<InstrumentInputManager>();
        if (inputManager == null)
        {
            Debug.LogWarning("PipaController: 未找到 InstrumentInputManager");
        }

        if (createFretCollidersPerString)
        {
            CreateFretCollidersPerString();
        }
        else if (fretColliders == null || fretColliders.Length == 0)
        {
            CreateFretColliders();
        }
    }

    void Update()
    {
        if (strings == null) return;
        for (int i = 0; i < strings.Length; i++)
        {
            if (strings[i] == null || !strings[i].isPlaying) continue;
            int liveFret = liveFretHoldByString.TryGetValue(i, out int f) ? f : -1;
            float bend = liveBendByString.TryGetValue(i, out float b) ? b : 0f;
            float baseMult = GetSustainPitchMultiplierForFret(liveFret);
            float bendMult = Mathf.Pow(2f, Mathf.Clamp(bend, -2f, 2f) / 12f);
            strings[i].UpdateSustainPitch(baseMult * bendMult);
        }
    }

    /// <summary>当前按品对应的音高倍率（-1=空弦，0=泛音点，>0=按品）</summary>
    private float GetSustainPitchMultiplierForFret(int liveFret)
    {
        if (liveFret < 0) return 1f;
        if (liveFret == 0) return HarmonicPitchMultipliers.Length > 0 ? HarmonicPitchMultipliers[0] : 2f;
        // 规则：0=泛音点；1=1品；2=2品...
        return Mathf.Pow(2f, liveFret / 12f);
    }

    /// <summary>
    /// 自动查找弦对象
    /// </summary>
    void AutoFindStrings()
    {
        List<InstrumentString> foundStrings = new List<InstrumentString>();
        
        // 在子对象中查找所有 InstrumentString 组件
        InstrumentString[] allStrings = GetComponentsInChildren<InstrumentString>();
        
        if (allStrings.Length > 0)
        {
            System.Array.Sort(allStrings, (a, b) => a.stringIndex.CompareTo(b.stringIndex));
            foundStrings.AddRange(allStrings);
            
            if (showDebug)
                Debug.Log($"PipaController: 自动找到 {foundStrings.Count} 根弦");
        }
        
        strings = foundStrings.ToArray();
    }

    /// <summary>
    /// 初始化所有弦
    /// </summary>
    void InitializeStrings()
    {
        for (int i = 0; i < strings.Length && i < stringBasePitches.Length; i++)
        {
            if (strings[i] != null)
            {
                strings[i].stringIndex = i;
                strings[i].basePitch = stringBasePitches[i];
                
                // 订阅弦的拨动事件
                strings[i].OnStringPlucked += OnStringPlucked;

                if (showDebug)
                    Debug.Log($"琵琶弦 {i} 初始化完成，基础音高: {stringBasePitches[i]}");
            }
        }
    }

    /// <summary>
    /// 创建品位碰撞体
    /// </summary>
    void CreateFretColliders()
    {
        GameObject fretContainer = new GameObject("FretColliders");
        fretContainer.transform.SetParent(transform);
        fretContainer.transform.localPosition = Vector3.zero;
        
        fretColliders = new Collider[numberOfFrets];
        
        // 琵琶的品位沿着琴颈分布，间距逐渐变小
        for (int i = 0; i < numberOfFrets; i++)
        {
            GameObject fretObj = new GameObject($"Fret_{i + 1}");
            fretObj.transform.SetParent(fretContainer.transform);
            
            // 品位位置计算（对数分布，模拟真实琵琶）
            float normalizedPosition = i / (float)numberOfFrets;
            float fretPosition = CalculateFretPosition(normalizedPosition);
            fretObj.transform.localPosition = new Vector3(0, 0.02f, fretPosition);
            
            // 添加碰撞体
            BoxCollider col = fretObj.AddComponent<BoxCollider>();
            col.size = new Vector3(fretWidth, fretHeight, 0.01f);
            col.isTrigger = true;
            
            fretColliders[i] = col;
            
            // 添加标签（如果标签存在）
            try
            {
                fretObj.tag = "Fret";
            }
            catch
            {
                if (showDebug)
                    Debug.LogWarning("PipaController: 标签 'Fret' 未定义，请使用菜单 Tools/乐器系统/设置所需标签");
            }
        }
        
        if (showDebug)
            Debug.Log($"PipaController: 创建了 {numberOfFrets} 个品位碰撞体（旧版）");
    }

    void CreateFretCollidersPerString()
    {
        GameObject root = new GameObject("FretCollidersPerString");
        root.transform.SetParent(transform);
        root.transform.localPosition = Vector3.zero;
        fretCollidersPerString = new Collider[strings.Length][];
        for (int s = 0; s < strings.Length; s++)
        {
            if (strings[s] == null) continue;
            GameObject stringRoot = new GameObject($"String_{s + 1}_Frets");
            stringRoot.transform.SetParent(root.transform);
            stringRoot.transform.localPosition = Vector3.zero;
            Vector3 stringLocal = transform.InverseTransformPoint(strings[s].transform.position);
            fretCollidersPerString[s] = new Collider[numberOfFrets];
            for (int f = 0; f < numberOfFrets; f++)
            {
                GameObject go = new GameObject($"String_{s + 1}_Fret_{f + 1}");
                go.transform.SetParent(stringRoot.transform);
                float z = CalculateFretPosition(f / (float)numberOfFrets);
                go.transform.localPosition = new Vector3(stringLocal.x, 0.02f, z);
                var col = go.AddComponent<BoxCollider>();
                col.size = new Vector3(0.02f, fretHeight, 0.01f);
                col.isTrigger = true;
                var info = go.AddComponent<FretInfo>();
                info.stringIndex = s;
                info.fretIndex = f;
                info.showDebug = showDebug;
                fretCollidersPerString[s][f] = col;
            }
        }
        if (showDebug) Debug.Log($"PipaController: 为 {strings.Length} 根弦创建了每弦 {numberOfFrets} 个品位碰撞体");
    }

    /// <summary>
    /// 计算品位位置（对数分布）
    /// </summary>
    float CalculateFretPosition(float normalized)
    {
        // 使用对数曲线模拟真实乐器的品位分布
        float start = 0.5f;  // 琴颈开始位置
        float end = -0.5f;   // 琴颈结束位置
        
        // 对数分布
        float curve = Mathf.Pow(normalized, 1.2f);
        return Mathf.Lerp(start, end, curve);
    }

    public void ClearAllLiveFretHolds() { liveFretHoldByString.Clear(); }
    public void SetLiveFretHold(int stringIndex, int fretIndex)
    {
        if (stringIndex >= 0 && stringIndex < strings.Length) liveFretHoldByString[stringIndex] = fretIndex;
    }
    public void ClearLiveFretHold(int stringIndex) { liveFretHoldByString.Remove(stringIndex); }

    public void SetBendAmount(int stringIndex, float bendSemitones)
    {
        if (stringIndex >= 0 && stringIndex < strings.Length)
            liveBendByString[stringIndex] = Mathf.Clamp(bendSemitones, -2f, 2f);
    }
    public void ClearAllBendAmounts() { liveBendByString.Clear(); }

    public bool GetFretFromHit(RaycastHit hit, out int stringIndex, out int fretIndex)
    {
        stringIndex = -1; fretIndex = -1;
        var fretInfo = hit.collider.GetComponent<FretInfo>();
        if (fretInfo != null)
        {
            stringIndex = fretInfo.stringIndex;
            fretIndex = fretInfo.treatAsOpenString ? -1 : fretInfo.fretIndex;
            return stringIndex >= 0 && stringIndex < strings.Length;
        }
        if (fretCollidersPerString != null)
        {
            for (int s = 0; s < fretCollidersPerString.Length; s++)
            {
                if (fretCollidersPerString[s] == null) continue;
                for (int f = 0; f < fretCollidersPerString[s].Length; f++)
                {
                    if (fretCollidersPerString[s][f] == hit.collider)
                    {
                        stringIndex = s; fretIndex = f; return true;
                    }
                }
            }
        }
        if (fretColliders != null)
        {
            for (int i = 0; i < fretColliders.Length; i++)
            {
                if (fretColliders[i] == hit.collider)
                {
                    fretIndex = i; stringIndex = DetermineStringFromPosition(hit.point);
                    return stringIndex >= 0 && stringIndex < strings.Length;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 手机端：拨弦时由手势推断奏法（还原现实逻辑）。仅由触控调用，不依赖 currentTechnique。
    /// 扫弦由触控滑动单独处理。无按品→散音；按在泛音点（第1品）→泛音；按品+拨弦→按音/推拉。
    /// </summary>
    public void HandlePluckWithLiveFret(Ray ray)
    {
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, 100f)) return;
        var clickedString = hit.collider.GetComponent<InstrumentString>();
        if (clickedString == null) return;
        int stringIndex = clickedString.stringIndex;

        if (!liveFretHoldByString.TryGetValue(stringIndex, out int liveFret))
        {
            clickedString.ReleaseFret();
            if (currentFretPositions.ContainsKey(stringIndex)) currentFretPositions.Remove(stringIndex);
            clickedString.PluckString();
            if (showDebug) Debug.Log($"琵琶【散音】触控 第{stringIndex + 1}弦");
            return;
        }
        if (liveFret == 0)
        {
            clickedString.ReleaseFret();
            if (currentFretPositions.ContainsKey(stringIndex)) currentFretPositions.Remove(stringIndex);
            float harmonicMult = HarmonicPitchMultipliers.Length > 0 ? HarmonicPitchMultipliers[0] : 2f;
            clickedString.PluckStringHarmonic(harmonicMult);
            if (showDebug) Debug.Log($"琵琶【泛音】触控 第{stringIndex + 1}弦 泛音点第1品");
            return;
        }
        clickedString.PressFret(liveFret);
        currentFretPositions[stringIndex] = liveFret;
        float bend = liveBendByString.TryGetValue(stringIndex, out float b) ? b : 0f;
        clickedString.PluckStringWithBend(bend);
        if (showDebug) Debug.Log($"琵琶【按音/推拉】触控 第{stringIndex + 1}弦 第{liveFret + 1}品 推拉{bend:F2}半音");
    }

    /// <summary>PC：根据 currentTechnique 与 recordedFretPositions 拨弦；扫弦由 Input 直接调 StrumStrings</summary>
    public void HandlePluck(Ray ray)
    {
        if (currentTechnique == PipaTechnique.Strum)
        {
            StrumStrings(true);
            return;
        }
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, 100f)) return;
        var clickedString = hit.collider.GetComponent<InstrumentString>();
        if (clickedString == null) return;
        int stringIndex = clickedString.stringIndex;

        switch (currentTechnique)
        {
            case PipaTechnique.SanYin:
                clickedString.ReleaseFret();
                if (currentFretPositions.ContainsKey(stringIndex)) currentFretPositions.Remove(stringIndex);
                recordedFretPositions.Remove(stringIndex);
                clickedString.PluckString();
                if (showDebug) Debug.Log($"琵琶【散音】第{stringIndex + 1}弦");
                break;
            case PipaTechnique.AnYin:
                if (recordedFretPositions.TryGetValue(stringIndex, out int anFret))
                {
                    if (anFret >= 0)
                    {
                        clickedString.PressFret(anFret);
                        currentFretPositions[stringIndex] = anFret;
                    }
                    else
                    {
                        clickedString.ReleaseFret();
                        if (currentFretPositions.ContainsKey(stringIndex)) currentFretPositions.Remove(stringIndex);
                    }
                    recordedFretPositions.Remove(stringIndex);
                }
                else
                {
                    clickedString.ReleaseFret();
                    if (currentFretPositions.ContainsKey(stringIndex)) currentFretPositions.Remove(stringIndex);
                }
                clickedString.PluckString();
                if (showDebug) Debug.Log($"琵琶【按音】第{stringIndex + 1}弦");
                break;
            case PipaTechnique.FanYin:
                clickedString.ReleaseFret();
                if (currentFretPositions.ContainsKey(stringIndex)) currentFretPositions.Remove(stringIndex);
                float harmonicMult = 2f;
                if (recordedFretPositions.TryGetValue(stringIndex, out int fanFret) && fanFret >= 0)
                {
                    int idx = Mathf.Clamp(fanFret, 0, HarmonicPitchMultipliers.Length - 1);
                    harmonicMult = HarmonicPitchMultipliers[idx];
                    recordedFretPositions.Remove(stringIndex);
                }
                clickedString.PluckStringHarmonic(harmonicMult);
                if (showDebug) Debug.Log($"琵琶【泛音】第{stringIndex + 1}弦");
                break;
            case PipaTechnique.TuiLa:
                if (recordedFretPositions.TryGetValue(stringIndex, out int tuiFret) && tuiFret >= 0)
                {
                    clickedString.PressFret(tuiFret);
                    currentFretPositions[stringIndex] = tuiFret;
                    recordedFretPositions.Remove(stringIndex);
                    float testBend = 0.5f;
                    clickedString.PluckStringWithBend(testBend);
                    if (showDebug) Debug.Log($"琵琶【推拉】PC 第{stringIndex + 1}弦 第{tuiFret + 1}品 测试推拉{testBend}半音");
                }
                else
                {
                    clickedString.ReleaseFret();
                    if (currentFretPositions.ContainsKey(stringIndex)) currentFretPositions.Remove(stringIndex);
                    recordedFretPositions.Remove(stringIndex);
                    clickedString.PluckString();
                }
                break;
            default:
                clickedString.ReleaseFret();
                if (currentFretPositions.ContainsKey(stringIndex)) currentFretPositions.Remove(stringIndex);
                clickedString.PluckString();
                break;
        }
    }

    public void HandleFretRecord(Ray ray)
    {
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, 100f))
        {
            if (recordedFretPositions.Count > 0) { recordedFretPositions.Clear(); if (showDebug) Debug.Log("琵琶：清空按品记录"); }
            return;
        }
        var fretInfo = hit.collider.GetComponent<FretInfo>();
        if (fretInfo != null)
        {
            int si = fretInfo.stringIndex, fi = fretInfo.treatAsOpenString ? -1 : fretInfo.fretIndex;
            if (si >= 0 && si < strings.Length && strings[si] != null)
            {
                recordedFretPositions[si] = fi;
                if (showDebug) Debug.Log(fi < 0 ? $"琵琶：记录空弦 第{si + 1}弦" : $"琵琶：记录按品 第{si + 1}弦 第{fi + 1}品");
            }
            return;
        }
        if (fretCollidersPerString != null)
        {
            for (int s = 0; s < fretCollidersPerString.Length; s++)
            {
                if (fretCollidersPerString[s] == null) continue;
                for (int f = 0; f < fretCollidersPerString[s].Length; f++)
                {
                    if (fretCollidersPerString[s][f] == hit.collider && strings[s] != null)
                    {
                        recordedFretPositions[s] = f;
                        if (showDebug) Debug.Log($"琵琶：记录按品 第{s + 1}弦 第{f + 1}品");
                        return;
                    }
                }
            }
        }
        if (fretColliders != null)
        {
            for (int i = 0; i < fretColliders.Length; i++)
            {
                if (fretColliders[i] == hit.collider)
                {
                    int si = DetermineStringFromPosition(hit.point);
                    if (si >= 0 && si < strings.Length && strings[si] != null)
                    {
                        recordedFretPositions[si] = i;
                        if (showDebug) Debug.Log($"琵琶：记录按品 第{si + 1}弦 第{i + 1}品（旧版）");
                    }
                    return;
                }
            }
        }
        if (recordedFretPositions.Count > 0) { recordedFretPositions.Clear(); if (showDebug) Debug.Log("琵琶：清空按品记录"); }
    }

    public void HandleClick(Ray ray) { HandlePluck(ray); }

    public void HandlePress(Ray ray, bool isPressed)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f))
        {
            var fretInfo = hit.collider.GetComponent<FretInfo>();
            if (fretInfo != null)
            {
                HandleFretPress(fretInfo.fretIndex, hit.point, isPressed, fretInfo.stringIndex);
                isPressingFret = isPressed;
                lastPressedFretIndex = isPressed ? fretInfo.fretIndex : -1;
                return;
            }
            if (fretCollidersPerString != null)
            {
                for (int s = 0; s < fretCollidersPerString.Length; s++)
                {
                    if (fretCollidersPerString[s] == null) continue;
                    for (int f = 0; f < fretCollidersPerString[s].Length; f++)
                    {
                        if (fretCollidersPerString[s][f] == hit.collider)
                        {
                            HandleFretPress(f, hit.point, isPressed, s);
                            isPressingFret = isPressed;
                            lastPressedFretIndex = isPressed ? f : -1;
                            return;
                        }
                    }
                }
            }
            if (fretColliders != null)
            {
                for (int i = 0; i < fretColliders.Length; i++)
                {
                    if (fretColliders[i] == hit.collider)
                    {
                        HandleFretPress(i, hit.point, isPressed);
                        isPressingFret = isPressed;
                        lastPressedFretIndex = isPressed ? i : -1;
                        return;
                    }
                }
            }
            if (isPressed)
            {
                var clickedString = hit.collider.GetComponent<InstrumentString>();
                if (clickedString != null) clickedString.PluckString();
            }
        }
        if (!isPressed)
        {
            ReleaseAllFrets();
            isPressingFret = false;
            lastPressedFretIndex = -1;
        }
    }

    void HandleFretPress(int fretIndex, Vector3 hitPoint, bool isPressed, int stringIndex = -1)
    {
        if (stringIndex < 0) stringIndex = DetermineStringFromPosition(hitPoint);
        if (stringIndex < 0 || stringIndex >= strings.Length || strings[stringIndex] == null) return;
        if (isPressed)
        {
            strings[stringIndex].PressFret(fretIndex);
            currentFretPositions[stringIndex] = fretIndex;
            if (showDebug) Debug.Log($"琵琶：按住第 {stringIndex + 1} 弦第 {fretIndex + 1} 品");
        }
        else
        {
            strings[stringIndex].ReleaseFret();
            currentFretPositions.Remove(stringIndex);
            if (showDebug) Debug.Log($"琵琶：释放第 {stringIndex + 1} 弦品位");
        }
    }

    /// <summary>
    /// 根据世界坐标确定是哪根弦
    /// </summary>
    int DetermineStringFromPosition(Vector3 worldPosition)
    {
        float minDistance = float.MaxValue;
        int closestString = -1;
        
        // 将世界坐标转换为本地坐标
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        
        for (int i = 0; i < strings.Length; i++)
        {
            if (strings[i] != null)
            {
                Vector3 stringLocalPos = transform.InverseTransformPoint(strings[i].transform.position);
                float distance = Mathf.Abs(localPosition.x - stringLocalPos.x);
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestString = i;
                }
            }
        }
        
        return closestString;
    }

    /// <summary>
    /// 扫弦 - 快速拨动所有弦
    /// </summary>
    public void StrumStrings(bool upward = true)
    {
        if (!enableStrumming) return;

        int start = upward ? 0 : strings.Length - 1;
        int end = upward ? strings.Length : -1;
        int step = upward ? 1 : -1;

        for (int i = start; i != end; i += step)
        {
            if (strings[i] != null)
            {
                // 添加轻微延迟以模拟扫弦效果
                StartCoroutine(DelayedPluck(strings[i], (Mathf.Abs(i - start)) * 0.05f));
            }
        }

        if (showDebug)
            Debug.Log($"琵琶：扫弦 ({(upward ? "向上" : "向下")})");
    }

    /// <summary>
    /// 延迟拨弦
    /// </summary>
    System.Collections.IEnumerator DelayedPluck(InstrumentString str, float delay)
    {
        yield return new WaitForSeconds(delay);
        str.PluckString();
    }

    /// <summary>
    /// 释放所有品位
    /// </summary>
    void ReleaseAllFrets()
    {
        foreach (var str in strings)
        {
            if (str != null)
            {
                str.ReleaseFret();
            }
        }
        currentFretPositions.Clear();
    }

    /// <summary>
    /// 弦被拨动时的回调
    /// </summary>
    void OnStringPlucked(int stringIndex, int fretIndex)
    {
        if (showDebug)
        {
            string fretInfo = fretIndex >= 0 ? $"第{fretIndex}品" : "空弦";
            Debug.Log($"琵琶音符：第{stringIndex}弦 {fretInfo}");
        }
    }

    /// <summary>
    /// 获取弦对象
    /// </summary>
    public InstrumentString GetString(int index)
    {
        if (index >= 0 && index < strings.Length)
        {
            return strings[index];
        }
        return null;
    }

    /// <summary>
    /// 停止所有声音
    /// </summary>
    public void StopAllSounds()
    {
        foreach (var str in strings)
        {
            if (str != null)
            {
                str.StopPlaying();
            }
        }
    }

    void OnDestroy()
    {
        // 取消订阅事件
        foreach (var str in strings)
        {
            if (str != null)
            {
                str.OnStringPlucked -= OnStringPlucked;
            }
        }
    }
}

