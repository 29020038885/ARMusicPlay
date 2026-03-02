using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 乐器音频管理器 - 管理和加载乐器音频资源
/// 支持从 Resources 文件夹加载或使用程序化生成的音频
/// </summary>
public class InstrumentAudioManager : MonoBehaviour
{
    [Header("音频资源路径")]
    [Tooltip("古琴音频资源文件夹路径（相对于 Resources）")]
    public string guqinAudioPath = "Audio/Guqin";

    [Tooltip("琵琶音频资源文件夹路径（相对于 Resources）")]
    public string pipaAudioPath = "Audio/Pipa";

    [Header("程序化音频")]
    [Tooltip("如果没有音频文件，使用程序化生成")]
    public bool useProceduralAudio = true;

    [Tooltip("音频采样率")]
    public int sampleRate = 44100;

    [Tooltip("音频时长（秒）")]
    public float audioLength = 2.0f;

    [Tooltip("音量")]
    [Range(0f, 1f)]
    public float volume = 0.5f;

    [Header("音色设置")]
    [Tooltip("古琴音色参数")]
    public AudioTimbre guqinTimbre = new AudioTimbre()
    {
        attackTime = 0.01f,
        decayTime = 0.5f,
        sustainLevel = 0.3f,
        releaseTime = 1.0f,
        harmonics = new float[] { 1.0f, 0.5f, 0.25f, 0.125f }
    };

    [Tooltip("琵琶音色参数")]
    public AudioTimbre pipaTimbre = new AudioTimbre()
    {
        attackTime = 0.005f,
        decayTime = 0.3f,
        sustainLevel = 0.2f,
        releaseTime = 0.8f,
        harmonics = new float[] { 1.0f, 0.7f, 0.4f, 0.2f, 0.1f }
    };

    [Header("调试")]
    [Tooltip("显示调试信息")]
    public bool showDebugInfo = true;

    // 音频缓存
    private Dictionary<string, AudioClip> audioCache = new Dictionary<string, AudioClip>();

    void Start()
    {
        if (showDebugInfo)
        {
            Debug.Log("InstrumentAudioManager: 初始化音频管理器");
        }
    }

    /// <summary>
    /// 获取古琴音频片段
    /// </summary>
    /// <param name="stringIndex">弦索引（0-6）</param>
    /// <param name="fretIndex">品位索引（-1表示空弦）</param>
    public AudioClip GetGuqinAudio(int stringIndex, int fretIndex)
    {
        string key = $"guqin_s{stringIndex}_f{fretIndex}";

        // 检查缓存
        if (audioCache.ContainsKey(key))
        {
            return audioCache[key];
        }

        // 尝试从 Resources 加载
        string resourcePath = $"{guqinAudioPath}/string{stringIndex}_fret{fretIndex}";
        AudioClip clip = Resources.Load<AudioClip>(resourcePath);

        if (clip != null)
        {
            audioCache[key] = clip;
            if (showDebugInfo)
                Debug.Log($"加载古琴音频: {resourcePath}");
            return clip;
        }

        // 如果没有找到，使用程序化生成
        if (useProceduralAudio)
        {
            // 计算频率（基于 MIDI 音符）
            int baseMidi = 60 + stringIndex * 2; // C4 开始，每弦相差2个半音
            int midiNote = baseMidi + (fretIndex >= 0 ? fretIndex : 0);
            float frequency = MidiNoteToFrequency(midiNote);

            clip = GenerateTone(frequency, guqinTimbre, $"Guqin_S{stringIndex}_F{fretIndex}");
            audioCache[key] = clip;

            if (showDebugInfo)
                Debug.Log($"生成古琴音频: 弦{stringIndex} 品{fretIndex} 频率{frequency}Hz");

            return clip;
        }

        Debug.LogWarning($"未找到古琴音频: {resourcePath}");
        return null;
    }

    /// <summary>
    /// 获取琵琶音频片段
    /// </summary>
    /// <param name="stringIndex">弦索引（0-3）</param>
    /// <param name="fretIndex">品位索引（-1表示空弦）</param>
    public AudioClip GetPipaAudio(int stringIndex, int fretIndex)
    {
        string key = $"pipa_s{stringIndex}_f{fretIndex}";

        // 检查缓存
        if (audioCache.ContainsKey(key))
        {
            return audioCache[key];
        }

        // 尝试从 Resources 加载
        string resourcePath = $"{pipaAudioPath}/string{stringIndex}_fret{fretIndex}";
        AudioClip clip = Resources.Load<AudioClip>(resourcePath);

        if (clip != null)
        {
            audioCache[key] = clip;
            if (showDebugInfo)
                Debug.Log($"加载琵琶音频: {resourcePath}");
            return clip;
        }

        // 如果没有找到，使用程序化生成
        if (useProceduralAudio)
        {
            // 琵琶的音域设置
            int[] pipaBaseMidi = { 50, 55, 59, 62 }; // D3, G3, B3, D4
            int baseMidi = pipaBaseMidi[Mathf.Clamp(stringIndex, 0, 3)];
            int midiNote = baseMidi + (fretIndex >= 0 ? fretIndex : 0);
            float frequency = MidiNoteToFrequency(midiNote);

            clip = GenerateTone(frequency, pipaTimbre, $"Pipa_S{stringIndex}_F{fretIndex}");
            audioCache[key] = clip;

            if (showDebugInfo)
                Debug.Log($"生成琵琶音频: 弦{stringIndex} 品{fretIndex} 频率{frequency}Hz");

            return clip;
        }

        Debug.LogWarning($"未找到琵琶音频: {resourcePath}");
        return null;
    }

    /// <summary>
    /// 生成音调
    /// </summary>
    AudioClip GenerateTone(float frequency, AudioTimbre timbre, string name)
    {
        int samples = Mathf.RoundToInt(sampleRate * audioLength);
        float[] data = new float[samples];

        // 计算包络和波形
        for (int i = 0; i < samples; i++)
        {
            float time = i / (float)sampleRate;
            float envelope = CalculateEnvelope(time, timbre);
            float wave = 0f;

            // 添加谐波
            for (int h = 0; h < timbre.harmonics.Length; h++)
            {
                float harmonicFreq = frequency * (h + 1);
                float harmonicAmp = timbre.harmonics[h];
                wave += Mathf.Sin(2 * Mathf.PI * harmonicFreq * time) * harmonicAmp;
            }

            // 归一化
            wave /= timbre.harmonics.Length;
            data[i] = wave * envelope * volume;
        }

        // 创建 AudioClip
        AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(data, 0);

        return clip;
    }

    /// <summary>
    /// 计算 ADSR 包络
    /// </summary>
    float CalculateEnvelope(float time, AudioTimbre timbre)
    {
        float attackEnd = timbre.attackTime;
        float decayEnd = attackEnd + timbre.decayTime;
        float sustainEnd = audioLength - timbre.releaseTime;

        if (time < attackEnd)
        {
            // Attack 阶段
            return time / attackEnd;
        }
        else if (time < decayEnd)
        {
            // Decay 阶段
            float t = (time - attackEnd) / timbre.decayTime;
            return Mathf.Lerp(1.0f, timbre.sustainLevel, t);
        }
        else if (time < sustainEnd)
        {
            // Sustain 阶段
            return timbre.sustainLevel;
        }
        else
        {
            // Release 阶段
            float t = (time - sustainEnd) / timbre.releaseTime;
            return Mathf.Lerp(timbre.sustainLevel, 0f, t);
        }
    }

    /// <summary>
    /// MIDI 音符转频率
    /// </summary>
    float MidiNoteToFrequency(int midiNote)
    {
        // A4 (MIDI 69) = 440 Hz
        return 440f * Mathf.Pow(2f, (midiNote - 69f) / 12f);
    }

    /// <summary>
    /// 批量加载古琴音频
    /// </summary>
    public void PreloadGuqinAudio(int numStrings, int numFrets)
    {
        if (showDebugInfo)
            Debug.Log($"预加载古琴音频: {numStrings}根弦 x {numFrets}品");

        for (int s = 0; s < numStrings; s++)
        {
            // 空弦
            GetGuqinAudio(s, -1);

            // 各品位
            for (int f = 0; f < numFrets; f++)
            {
                GetGuqinAudio(s, f);
            }
        }
    }

    /// <summary>
    /// 批量加载琵琶音频
    /// </summary>
    public void PreloadPipaAudio(int numStrings, int numFrets)
    {
        if (showDebugInfo)
            Debug.Log($"预加载琵琶音频: {numStrings}根弦 x {numFrets}品");

        for (int s = 0; s < numStrings; s++)
        {
            // 空弦
            GetPipaAudio(s, -1);

            // 各品位
            for (int f = 0; f < numFrets; f++)
            {
                GetPipaAudio(s, f);
            }
        }
    }

    /// <summary>
    /// 清除音频缓存
    /// </summary>
    public void ClearCache()
    {
        audioCache.Clear();
        if (showDebugInfo)
            Debug.Log("音频缓存已清除");
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    public string GetCacheStats()
    {
        return $"缓存音频数量: {audioCache.Count}";
    }
}

/// <summary>
/// 音色参数
/// </summary>
[System.Serializable]
public class AudioTimbre
{
    [Tooltip("起音时间")]
    public float attackTime = 0.01f;

    [Tooltip("衰减时间")]
    public float decayTime = 0.3f;

    [Tooltip("持续电平")]
    [Range(0f, 1f)]
    public float sustainLevel = 0.3f;

    [Tooltip("释放时间")]
    public float releaseTime = 1.0f;

    [Tooltip("谐波强度数组")]
    public float[] harmonics = new float[] { 1.0f, 0.5f, 0.25f };
}

