using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 古琴控制器 - 管理古琴的7根弦和交互
/// </summary>
public class GuqinController : MonoBehaviour
{
    [Header("古琴设置")]
    [Tooltip("古琴的7根弦")]
    public InstrumentString[] strings = new InstrumentString[7];

    [Tooltip("品位标记物体数组（可选）")]
    public Transform[] fretMarkers;

    [Header("音域设置")]
    [Tooltip("7根弦的基础音高（MIDI音符）")]
    public int[] stringBasePitches = new int[7] 
    { 
        60, // C4 - 第一弦（最粗）
        62, // D4 - 第二弦
        64, // E4 - 第三弦
        65, // F4 - 第四弦
        67, // G4 - 第五弦
        69, // A4 - 第六弦
        71  // B4 - 第七弦（最细）
    };

    [Tooltip("品位数量（古琴通常有13个徽位）")]
    public int numberOfFrets = 13;

    [Header("音频资源")]
    [Tooltip("古琴音频片段 - 7根弦 x N个品位")]
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

    [Header("调试")]
    [Tooltip("显示调试信息")]
    public bool showDebug = true;

    private Dictionary<int, int> currentFretPositions = new Dictionary<int, int>();
    private Dictionary<int, int> recordedFretPositions = new Dictionary<int, int>(); // 右键记录，左键拨弦时应用
    private InstrumentInputManager inputManager;

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
            Debug.LogWarning("GuqinController: 未找到 InstrumentInputManager");
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
        // 输入管理器会调用我们的方法，这里不需要额外处理
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
                Debug.Log($"GuqinController: 自动找到 {foundStrings.Count} 根弦");
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
                    Debug.Log($"古琴弦 {i} 初始化完成，基础音高: {stringBasePitches[i]}");
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
        
        // 古琴的品位通常沿着琴体分布
        // 这里创建简单的盒型碰撞体
        for (int i = 0; i < numberOfFrets; i++)
        {
            GameObject fretObj = new GameObject($"Fret_{i + 1}");
            fretObj.transform.SetParent(fretContainer.transform);
            
            // 品位位置计算（沿Z轴分布）
            float fretPosition = -0.5f + (i / (float)(numberOfFrets - 1)) * 1.0f;
            fretObj.transform.localPosition = new Vector3(0, 0.01f, fretPosition);
            
            // 添加碰撞体
            BoxCollider col = fretObj.AddComponent<BoxCollider>();
            col.size = new Vector3(0.3f, 0.02f, 0.02f); // 横向覆盖所有弦
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
                    Debug.LogWarning("GuqinController: 标签 'Fret' 未定义，请使用菜单 Tools/乐器系统/设置所需标签");
            }
        }
        
        if (showDebug)
            Debug.Log($"GuqinController: 创建了 {numberOfFrets} 个品位碰撞体（旧版）");
    }

    /// <summary>
    /// 为每根弦创建独立品位碰撞体，并挂 FretInfo
    /// </summary>
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
                float z = -0.5f + (f / (float)(numberOfFrets - 1)) * 1f;
                go.transform.localPosition = new Vector3(stringLocal.x, 0.01f, z);

                var col = go.AddComponent<BoxCollider>();
                col.size = new Vector3(0.02f, 0.02f, 0.02f);
                col.isTrigger = true;

                var info = go.AddComponent<FretInfo>();
                info.stringIndex = s;
                info.fretIndex = f;
                info.showDebug = showDebug;
                fretCollidersPerString[s][f] = col;
            }
        }
        if (showDebug) Debug.Log($"GuqinController: 为 {strings.Length} 根弦创建了每弦 {numberOfFrets} 个品位碰撞体");
    }

    /// <summary>
    /// 处理拨弦（左键）：有记录则先应用按品再拨弦，然后清空该弦记录
    /// </summary>
    public void HandlePluck(Ray ray)
    {
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, 100f)) return;

        var clickedString = hit.collider.GetComponent<InstrumentString>();
        if (clickedString == null) return;

        int stringIndex = clickedString.stringIndex;
        if (recordedFretPositions.TryGetValue(stringIndex, out int fretIndex))
        {
            if (fretIndex >= 0)
            {
                clickedString.PressFret(fretIndex);
                currentFretPositions[stringIndex] = fretIndex;
                if (showDebug) Debug.Log($"古琴：应用记录按品 第{stringIndex + 1}弦 第{fretIndex + 1}品，再拨弦");
            }
            else
            {
                clickedString.ReleaseFret();
                if (currentFretPositions.ContainsKey(stringIndex)) currentFretPositions.Remove(stringIndex);
                if (showDebug) Debug.Log($"古琴：记录为空弦 第{stringIndex + 1}弦，拨弦");
            }
            recordedFretPositions.Remove(stringIndex);
        }
        else
        {
            clickedString.ReleaseFret();
            if (currentFretPositions.ContainsKey(stringIndex)) currentFretPositions.Remove(stringIndex);
        }

        clickedString.PluckString();
        if (showDebug) Debug.Log($"古琴：拨动第 {stringIndex + 1} 弦");
    }

    /// <summary>
    /// 处理按品记录（右键）：只记录，不拨弦
    /// </summary>
    public void HandleFretRecord(Ray ray)
    {
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, 100f))
        {
            if (recordedFretPositions.Count > 0) { recordedFretPositions.Clear(); if (showDebug) Debug.Log("古琴：清空按品记录"); }
            return;
        }

        var fretInfo = hit.collider.GetComponent<FretInfo>();
        if (fretInfo != null)
        {
            int si = fretInfo.stringIndex, fi = fretInfo.treatAsOpenString ? -1 : fretInfo.fretIndex;
            if (si >= 0 && si < strings.Length && strings[si] != null)
            {
                recordedFretPositions[si] = fi;
                if (showDebug) Debug.Log(fi < 0 ? $"古琴：记录空弦 第{si + 1}弦" : $"古琴：记录按品 第{si + 1}弦 第{fi + 1}品");
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
                        if (showDebug) Debug.Log($"古琴：记录按品 第{s + 1}弦 第{f + 1}品");
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
                        if (showDebug) Debug.Log($"古琴：记录按品 第{si + 1}弦 第{i + 1}品（旧版）");
                    }
                    return;
                }
            }
        }

        if (recordedFretPositions.Count > 0) { recordedFretPositions.Clear(); if (showDebug) Debug.Log("古琴：清空按品记录"); }
    }

    /// <summary>
    /// 兼容：点击即拨弦
    /// </summary>
    public void HandleClick(Ray ray)
    {
        HandlePluck(ray);
    }

    /// <summary>
    /// 处理按压输入 - 用于按品位
    /// </summary>
    public void HandlePress(Ray ray, bool isPressed)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f))
        {
            var fretInfo = hit.collider.GetComponent<FretInfo>();
            if (fretInfo != null)
            {
                HandleFretPress(fretInfo.fretIndex, hit.point, isPressed, fretInfo.stringIndex);
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
                        return;
                    }
                }
            }
        }
        if (!isPressed) ReleaseAllFrets();
    }

    void HandleFretPress(int fretIndex, Vector3 hitPoint, bool isPressed, int stringIndex = -1)
    {
        if (stringIndex < 0) stringIndex = DetermineStringFromPosition(hitPoint);
        if (stringIndex < 0 || stringIndex >= strings.Length || strings[stringIndex] == null) return;
        if (isPressed)
        {
            strings[stringIndex].PressFret(fretIndex);
            currentFretPositions[stringIndex] = fretIndex;
            if (showDebug) Debug.Log($"古琴：按住第 {stringIndex + 1} 弦第 {fretIndex + 1} 品");
        }
        else
        {
            strings[stringIndex].ReleaseFret();
            currentFretPositions.Remove(stringIndex);
            if (showDebug) Debug.Log($"古琴：释放第 {stringIndex + 1} 弦品位");
        }
    }

    /// <summary>
    /// 根据世界坐标确定是哪根弦
    /// </summary>
    int DetermineStringFromPosition(Vector3 worldPosition)
    {
        float minDistance = float.MaxValue;
        int closestString = -1;
        
        for (int i = 0; i < strings.Length; i++)
        {
            if (strings[i] != null)
            {
                float distance = Vector3.Distance(
                    new Vector3(strings[i].transform.position.x, 0, 0),
                    new Vector3(worldPosition.x, 0, 0)
                );
                
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
            Debug.Log($"古琴音符：第{stringIndex}弦 {fretInfo}");
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

