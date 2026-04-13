using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AR 场景交互总控：
/// 1) 双击模型弹出功能菜单（option）
/// 2) 故事/名曲等面板与 option 平级、互不父子；打开子面板时隐藏 option 的界面；关闭当前子面板后重新显示 option
/// 3) 名曲播放互斥，支持按曲目名播放
/// 4) 演奏模式切换（禁用 AR 输入，启用弹奏输入）
/// </summary>
public class ARSceneInteractionController : MonoBehaviour
{
    [Serializable]
    public class StoryPage
    {
        [Tooltip("本页插图，可为空")]
        public Sprite image;

        [Tooltip("本页标题，可为空")]
        public string pageTitle = "";

        [TextArea(3, 16)]
        [Tooltip("本页正文")]
        public string pageBody = "";
    }

    [Serializable]
    public class InstrumentBinding
    {
        [Tooltip("调试用名称")]
        public string instrumentName = "Instrument";

        [Tooltip("用于双击识别的模型根节点（点击其子节点也算）")]
        public Transform modelRoot;

        [Tooltip("演奏模式下显示的物体根节点（通常可与 modelRoot 相同）")]
        public GameObject performanceRoot;

        [Header("演奏模式位置（相机前方）")]
        [Tooltip("演奏模式时，将该乐器固定在 AR 相机前方的距离（米）；可为每个乐器单独设置")]
        public float performanceDistanceFromCamera = 0.7f;

        [Tooltip("相机局部坐标偏移（米）。X=左右，Y=上下，Z=前后微调。可为每个乐器单独设置")]
        public Vector3 performanceCameraLocalOffset = Vector3.zero;

        [Tooltip("演奏模式时是否让乐器朝向随相机旋转")]
        public bool alignPerformanceRotationToCamera = false;

        [Tooltip("当 alignPerformanceRotationToCamera=true 时生效：在相机旋转基础上的欧拉角偏移")]
        public Vector3 performanceRotationOffsetEuler = Vector3.zero;

        [Tooltip("该乐器对应的名曲面板（每乐器独立）")]
        public GameObject musicPanel;

        [Header("共用故事面板 — 本乐器多页数据")]
        [Tooltip("顺序即页码：第 1 条为第 1 页，依此类推")]
        public List<StoryPage> storyPages = new List<StoryPage>();

        [Header("名曲（本乐器）")]
        [Tooltip("pieceName 须与名曲按钮绑定的文案一致（如 高山流水），用于 ToggleTrackByPieceName")]
        public List<TrackItem> musicTracks = new List<TrackItem>();
    }

    [Serializable]
    public class TrackItem
    {
        [Tooltip("曲目标识：与按钮 OnClick 传入的字符串一致（如 高山流水）；为空则用 title 匹配")]
        public string pieceName = "";

        [Tooltip("仅作备注或备用匹配（pieceName 为空时）")]
        public string title = "Track";

        public AudioClip clip;
    }

    [Header("双击检测")]
    public Camera arCamera;
    public LayerMask modelLayerMask = ~0;
    public float doubleTapInterval = 0.35f;
    public float doubleTapMaxMovePixels = 40f;
    public float rayDistance = 100f;

    [Header("乐器配置（建议顺序：0=古琴，1=琵琶）")]
    public List<InstrumentBinding> instruments = new List<InstrumentBinding>();

    [Header("功能入口面板（双击模型后显示）")]
    public GameObject optionPanel;

    [Tooltip("可选：仅隐藏功能按钮区域；不填则打开子面板时隐藏整个 optionPanel")]
    public GameObject optionMenuContentRoot;

    [Tooltip("双击进入 option 后隐藏；故事/名曲/演奏模式期间仍保持隐藏；ExitOptionPanel/CloseAllPanels 或退出演奏且未在 option 会话时才恢复")]
    public GameObject[] objectsHiddenWhileOptionMenuOnTop = Array.Empty<GameObject>();

    [Header("共用故事面板")]
    [Tooltip("所有乐器共用的故事面板根物体")]
    public GameObject sharedStoryPanel;

    [Tooltip("当前页插图")]
    public Image sharedStoryImage;

    [Tooltip("当前页标题")]
    public TMP_Text sharedStoryTitleText;

    [Tooltip("当前页正文")]
    public TMP_Text sharedStoryContentText;

    [Tooltip("页码，如 1 / 5；可不绑定")]
    public TMP_Text sharedStoryPageNumberText;

    [Tooltip("上一页；可不绑定，改由其它 UI 调用 StoryPrevPage")]
    public Button sharedStoryPrevButton;

    [Tooltip("下一页；可不绑定")]
    public Button sharedStoryNextButton;

    [Header("名曲播放")]
    public AudioSource musicAudioSource;

    [Tooltip("无完全匹配时，用曲目配置名是否包含查找串（或查找串包含曲目名）再匹配首条")]
    public bool looseTrackNameMatch = true;

    [Header("输入切换")]
    [Tooltip("进入演奏模式时需要禁用的 AR 输入脚本（如 ARGestureController）")]
    public MonoBehaviour[] arInputToDisable;

    [Tooltip("进入演奏模式时要启用的弹奏输入脚本（如 InstrumentInputManager）")]
    public MonoBehaviour instrumentInputToEnable;

    [Tooltip("若 instrumentInputToEnable 是 InstrumentInputManager，可自动切换激活乐器")]
    public bool autoSyncInstrumentInInputManager = true;

    [Header("演奏模式公共返回按钮（可选）")]
    [Tooltip("若指定，运行时自动监听点击：退出演奏模式并回到 optionPanel")]
    public Button performanceBackToOptionButton;

    private int currentInstrumentIndex = -1;
    private bool isInPerformanceMode = false;

    private float lastTapTime = -10f;
    private Vector2 lastTapPosition;

    private int currentTrackIndex = -1;
    private int playingInstrumentIndex = -1;

    private int currentStoryPageIndex;

    /// <summary>从双击打开 option 到 Exit/CloseAll 前为 true；与演奏模式标志共同决定 objectsHiddenWhileOptionMenuOnTop 的显隐。</summary>
    bool optionFlowSessionActive;

    void Awake()
    {
        if (arCamera == null) arCamera = Camera.main;

        if (sharedStoryPrevButton != null)
            sharedStoryPrevButton.onClick.AddListener(StoryPrevPage);
        if (sharedStoryNextButton != null)
            sharedStoryNextButton.onClick.AddListener(StoryNextPage);
        if (performanceBackToOptionButton != null)
            performanceBackToOptionButton.onClick.AddListener(ReturnFromPerformanceToOptionPanel);
    }

    void OnDestroy()
    {
        if (sharedStoryPrevButton != null)
            sharedStoryPrevButton.onClick.RemoveListener(StoryPrevPage);
        if (sharedStoryNextButton != null)
            sharedStoryNextButton.onClick.RemoveListener(StoryNextPage);
        if (performanceBackToOptionButton != null)
            performanceBackToOptionButton.onClick.RemoveListener(ReturnFromPerformanceToOptionPanel);
    }

    void Start()
    {
        SetActiveSafe(optionPanel, false);
        SetActiveSafe(sharedStoryPanel, false);
        foreach (var item in instruments)
        {
            SetActiveSafe(item.musicPanel, false);
            SetActiveSafe(item.performanceRoot, false);
        }

        SetBehaviourEnabled(instrumentInputToEnable, false);
        SetPerformanceBackButtonVisible(false);
        ApplyOptionAuxiliaryVisibility();
    }

    void Update()
    {
        if (isInPerformanceMode)
        {
            UpdateActivePerformanceRootPose();
            return;
        }
        HandleDoubleTap();
    }

    void OnDisable()
    {
        StopCurrentTrack();
        optionFlowSessionActive = false;
        isInPerformanceMode = false;
        SetPerformanceBackButtonVisible(false);
        ApplyOptionAuxiliaryVisibility();
    }

    void HandleDoubleTap()
    {
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Ended)
            {
                if (IsPointerOverUI(t.fingerId)) return;
                TryHandleTap(t.position, Time.unscaledTime);
            }
            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (IsPointerOverUI()) return;
            TryHandleTap(Input.mousePosition, Time.unscaledTime);
        }
    }

    void TryHandleTap(Vector2 tapPos, float tapTime)
    {
        bool timeOk = tapTime - lastTapTime <= doubleTapInterval;
        bool moveOk = Vector2.Distance(tapPos, lastTapPosition) <= doubleTapMaxMovePixels;
        lastTapTime = tapTime;
        lastTapPosition = tapPos;

        if (!timeOk || !moveOk) return;

        int hitIndex = RaycastInstrumentIndex(tapPos);
        if (hitIndex >= 0)
        {
            currentInstrumentIndex = hitIndex;
            ShowOptionMenuFromModelTap();
            Debug.Log($"[ARSceneInteraction] 双击命中乐器：{instruments[hitIndex].instrumentName}");
        }
    }

    int RaycastInstrumentIndex(Vector2 screenPos)
    {
        if (arCamera == null) return -1;

        Ray ray = arCamera.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out var hit, rayDistance, modelLayerMask)) return -1;

        Transform hitTr = hit.transform;
        for (int i = 0; i < instruments.Count; i++)
        {
            var root = instruments[i].modelRoot;
            if (root == null) continue;
            if (hitTr == root || hitTr.IsChildOf(root)) return i;
        }
        return -1;
    }

    public void OpenStoryPanel()
    {
        if (!HasCurrentInstrument()) return;
        if (sharedStoryPanel == null)
        {
            Debug.LogWarning("[ARSceneInteraction] 未配置 sharedStoryPanel");
            return;
        }

        var pages = instruments[currentInstrumentIndex].storyPages;
        if (pages == null || pages.Count == 0)
        {
            Debug.LogWarning($"[ARSceneInteraction] 乐器「{instruments[currentInstrumentIndex].instrumentName}」未配置 storyPages");
            return;
        }

        currentStoryPageIndex = 0;
        ApplyStoryPage(currentStoryPageIndex);
        SetArModelsSuppressedForUi(true);
        HideAllMusicPanels();
        StopCurrentTrackIfAnyMusicVisible();
        HideOptionMenuVisual();
        SetActiveSafe(sharedStoryPanel, true);
        ApplyOptionAuxiliaryVisibility();
    }

    /// <summary>上一页（可绑到按钮）</summary>
    public void StoryPrevPage()
    {
        if (!HasCurrentInstrument()) return;
        if (!IsStoryPanelActive()) return;
        var pages = instruments[currentInstrumentIndex].storyPages;
        if (pages == null || pages.Count == 0) return;
        if (currentStoryPageIndex <= 0) return;
        ApplyStoryPage(currentStoryPageIndex - 1);
    }

    /// <summary>下一页（可绑到按钮）</summary>
    public void StoryNextPage()
    {
        if (!HasCurrentInstrument()) return;
        if (!IsStoryPanelActive()) return;
        var pages = instruments[currentInstrumentIndex].storyPages;
        if (pages == null || pages.Count == 0) return;
        if (currentStoryPageIndex >= pages.Count - 1) return;
        ApplyStoryPage(currentStoryPageIndex + 1);
    }

    bool IsStoryPanelActive()
    {
        return sharedStoryPanel != null && sharedStoryPanel.activeInHierarchy;
    }

    void ApplyStoryPage(int pageIndex)
    {
        var b = instruments[currentInstrumentIndex];
        var pages = b.storyPages;
        if (pages == null || pages.Count == 0) return;

        pageIndex = Mathf.Clamp(pageIndex, 0, pages.Count - 1);
        currentStoryPageIndex = pageIndex;
        var p = pages[pageIndex];

        if (sharedStoryImage != null)
        {
            sharedStoryImage.sprite = p.image;
            sharedStoryImage.enabled = p.image != null;
        }

        if (sharedStoryTitleText != null)
            sharedStoryTitleText.text = p.pageTitle ?? "";

        if (sharedStoryContentText != null)
            sharedStoryContentText.text = p.pageBody ?? "";

        if (sharedStoryPageNumberText != null)
            sharedStoryPageNumberText.text = $"{pageIndex + 1} / {pages.Count}";

        UpdateStoryNavButtons();
    }

    void UpdateStoryNavButtons()
    {
        if (!HasCurrentInstrument()) return;
        var pages = instruments[currentInstrumentIndex].storyPages;
        int n = pages != null ? pages.Count : 0;

        if (sharedStoryPrevButton != null)
            sharedStoryPrevButton.interactable = n > 1 && currentStoryPageIndex > 0;

        if (sharedStoryNextButton != null)
            sharedStoryNextButton.interactable = n > 1 && currentStoryPageIndex < n - 1;
    }

    public void OpenMusicPanel()
    {
        if (!HasCurrentInstrument()) return;
        var panel = instruments[currentInstrumentIndex].musicPanel;
        if (panel == null)
        {
            Debug.LogWarning("[ARSceneInteraction] 当前乐器未配置 musicPanel");
            return;
        }
        StopCurrentTrack();
        SetArModelsSuppressedForUi(true);
        SetActiveSafe(sharedStoryPanel, false);
        HideOptionMenuVisual();
        SetActiveSafe(panel, true);
        ApplyOptionAuxiliaryVisibility();
    }

    /// <summary>
    /// 关闭当前打开的子面板（故事或名曲），并重新显示 option 功能菜单。
    /// </summary>
    public void CloseCurrentSubPanel()
    {
        SetActiveSafe(sharedStoryPanel, false);
        HideAllMusicPanels();
        StopCurrentTrack();
        ShowOptionMenuVisual();
        SetArModelsSuppressedForUi(false);
        ApplyOptionAuxiliaryVisibility();
    }

    /// <summary>与 CloseCurrentSubPanel 相同，兼容旧按钮绑定。</summary>
    public void BackPanel() => CloseCurrentSubPanel();

    /// <summary>与 CloseCurrentSubPanel 相同，兼容旧按钮绑定。</summary>
    public void ReturnToOptionPanel() => CloseCurrentSubPanel();

    /// <summary>
    /// 关闭 option 及所有子面板，回到未打开 UI 的状态（供「退出」按钮绑定）。
    /// </summary>
    public void ExitOptionPanel()
    {
        CloseAllPanels();
    }

    /// <summary>功能菜单是否在前台：option 可见且无故事/名曲子面板打开。</summary>
    public bool IsOptionMenuVisible()
    {
        if (optionPanel == null || !optionPanel.activeInHierarchy) return false;
        if (optionMenuContentRoot != null && !optionMenuContentRoot.activeInHierarchy) return false;
        if (sharedStoryPanel != null && sharedStoryPanel.activeInHierarchy) return false;
        foreach (var inst in instruments)
        {
            if (inst.musicPanel != null && inst.musicPanel.activeInHierarchy)
                return false;
        }
        return true;
    }

    /// <summary>兼容旧命名。</summary>
    public bool IsOptionPanelOnTop() => IsOptionMenuVisible();

    void ApplyOptionAuxiliaryVisibility()
    {
        if (objectsHiddenWhileOptionMenuOnTop == null) return;
        bool hideAux = optionFlowSessionActive || isInPerformanceMode;
        foreach (var go in objectsHiddenWhileOptionMenuOnTop)
        {
            if (go == null) continue;
            go.SetActive(!hideAux);
        }
    }

    void ShowOptionMenuFromModelTap()
    {
        optionFlowSessionActive = true;
        StopCurrentTrackIfAnyMusicVisible();
        SetActiveSafe(sharedStoryPanel, false);
        HideAllMusicPanels();
        ShowOptionMenuVisual();
        SetArModelsSuppressedForUi(false);
        ApplyOptionAuxiliaryVisibility();
    }

    void HideOptionMenuVisual()
    {
        if (optionMenuContentRoot != null)
        {
            SetActiveSafe(optionPanel, true);
            SetActiveSafe(optionMenuContentRoot, false);
        }
        else
        {
            SetActiveSafe(optionPanel, false);
        }
    }

    void ShowOptionMenuVisual()
    {
        SetActiveSafe(optionPanel, true);
        if (optionMenuContentRoot != null)
            SetActiveSafe(optionMenuContentRoot, true);
    }

    void HideAllMusicPanels()
    {
        foreach (var inst in instruments)
            SetActiveSafe(inst.musicPanel, false);
    }

    void StopCurrentTrackIfAnyMusicVisible()
    {
        foreach (var inst in instruments)
        {
            if (inst.musicPanel != null && inst.musicPanel.activeInHierarchy)
            {
                StopCurrentTrack();
                return;
            }
        }
    }

    bool IsMusicPanel(GameObject go)
    {
        if (go == null) return false;
        foreach (var inst in instruments)
        {
            if (inst.musicPanel == go) return true;
        }
        return false;
    }

    public void CloseAllPanels()
    {
        SetActiveSafe(sharedStoryPanel, false);
        HideAllMusicPanels();
        StopCurrentTrack();
        SetActiveSafe(optionPanel, false);
        if (optionMenuContentRoot != null)
            SetActiveSafe(optionMenuContentRoot, false);
        SetArModelsSuppressedForUi(false);
        optionFlowSessionActive = false;
        ApplyOptionAuxiliaryVisibility();
    }

    /// <summary>
    /// 按列表下标播放（按钮可绑 int 动态参数）。
    /// </summary>
    public void ToggleTrack(int trackIndex)
    {
        if (musicAudioSource == null)
        {
            Debug.LogWarning("[ARSceneInteraction] 未配置 musicAudioSource");
            return;
        }
        if (!HasCurrentInstrument()) return;

        var tracks = instruments[currentInstrumentIndex].musicTracks;
        if (tracks == null || trackIndex < 0 || trackIndex >= tracks.Count)
        {
            Debug.LogWarning($"[ARSceneInteraction] 曲目索引越界: {trackIndex}");
            return;
        }
        if (tracks[trackIndex].clip == null)
        {
            Debug.LogWarning($"[ARSceneInteraction] 曲目未配置 AudioClip: {trackIndex}");
            return;
        }

        if (playingInstrumentIndex == currentInstrumentIndex &&
            currentTrackIndex == trackIndex &&
            musicAudioSource.isPlaying)
        {
            StopCurrentTrack();
            return;
        }

        musicAudioSource.Stop();
        musicAudioSource.clip = tracks[trackIndex].clip;
        musicAudioSource.Play();
        currentTrackIndex = trackIndex;
        playingInstrumentIndex = currentInstrumentIndex;
    }

    /// <summary>
    /// 按曲目名播放：pieceName 或 title 与参数一致即可（忽略首尾空格，忽略大小写）。
    /// 名曲按钮 OnClick 选本方法，参数填与界面一致的「高山流水」等。
    /// </summary>
    public void ToggleTrackByPieceName(string pieceName)
    {
        int idx = FindTrackIndexByPieceName(pieceName);
        if (idx < 0)
        {
            Debug.LogWarning($"[ARSceneInteraction] 未找到曲目: 「{pieceName}」");
            return;
        }
        ToggleTrack(idx);
    }

    int FindTrackIndexByPieceName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || !HasCurrentInstrument()) return -1;
        string key = raw.Trim();
        var tracks = instruments[currentInstrumentIndex].musicTracks;
        if (tracks == null) return -1;

        for (int i = 0; i < tracks.Count; i++)
        {
            var t = tracks[i];
            string matchName = GetTrackMatchName(t);
            if (matchName.Length == 0) continue;
            if (string.Equals(matchName, key, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        if (!looseTrackNameMatch) return -1;

        for (int i = 0; i < tracks.Count; i++)
        {
            string matchName = GetTrackMatchName(tracks[i]);
            if (matchName.Length < 2 || key.Length < 2) continue;
            if (matchName.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf(matchName, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return i;
            }
        }
        return -1;
    }

    static string GetTrackMatchName(TrackItem t)
    {
        if (t == null) return "";
        if (!string.IsNullOrWhiteSpace(t.pieceName)) return t.pieceName.Trim();
        return (t.title ?? "").Trim();
    }

    public void StopCurrentTrack()
    {
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Stop();
        }
        if (musicAudioSource != null) musicAudioSource.clip = null;
        currentTrackIndex = -1;
        playingInstrumentIndex = -1;
    }

    public void EnterPerformanceMode()
    {
        if (!HasCurrentInstrument()) return;

        isInPerformanceMode = true;
        CloseAllPanels();
        SetArModelsSuppressedForUi(true);
        ApplyInputStateForPerformance(true);

        for (int i = 0; i < instruments.Count; i++)
        {
            bool active = i == currentInstrumentIndex;
            SetActiveSafe(instruments[i].performanceRoot, active);
        }

        SetPerformanceBackButtonVisible(true);

        // 进入演奏模式时先对齐一次位置，后续在 Update 中持续跟随相机
        UpdateActivePerformanceRootPose();
    }

    public void ExitPerformanceMode()
    {
        isInPerformanceMode = false;
        StopCurrentTrack();
        SetArModelsSuppressedForUi(false);
        ApplyInputStateForPerformance(false);

        for (int i = 0; i < instruments.Count; i++)
        {
            SetActiveSafe(instruments[i].performanceRoot, false);
        }

        SetPerformanceBackButtonVisible(false);

        ApplyOptionAuxiliaryVisibility();
    }

    /// <summary>
    /// 演奏模式下返回 optionPanel（可由 performanceBackToOptionButton 自动调用，或手动绑按钮）。
    /// </summary>
    public void ReturnFromPerformanceToOptionPanel()
    {
        // 先退出演奏态：关闭演奏模型、恢复输入
        if (isInPerformanceMode)
        {
            ExitPerformanceMode();
        }

        // 回到 option 流程并展示 option 菜单
        optionFlowSessionActive = true;
        SetActiveSafe(sharedStoryPanel, false);
        HideAllMusicPanels();
        ShowOptionMenuVisual();
        SetArModelsSuppressedForUi(false);
        ApplyOptionAuxiliaryVisibility();
    }

    void ApplyInputStateForPerformance(bool inPerformance)
    {
        if (arInputToDisable != null)
        {
            foreach (var arInput in arInputToDisable)
            {
                SetBehaviourEnabled(arInput, !inPerformance);
            }
        }

        SetBehaviourEnabled(instrumentInputToEnable, inPerformance);

        if (inPerformance && autoSyncInstrumentInInputManager && instrumentInputToEnable != null)
        {
            var inputManager = instrumentInputToEnable as InstrumentInputManager;
            if (inputManager != null)
            {
                inputManager.SetActiveInstrument(currentInstrumentIndex);
            }
        }
    }

    /// <summary>
    /// 演奏模式下：将当前乐器模型固定在 AR 相机前方指定距离与偏移处。
    /// </summary>
    void UpdateActivePerformanceRootPose()
    {
        if (!HasCurrentInstrument()) return;
        if (arCamera == null) arCamera = Camera.main;
        if (arCamera == null) return;

        var binding = instruments[currentInstrumentIndex];
        if (binding.performanceRoot == null) return;

        var camTr = arCamera.transform;
        float distance = Mathf.Max(0.05f, binding.performanceDistanceFromCamera);
        Vector3 worldPos = camTr.position
            + camTr.forward * distance
            + camTr.TransformVector(binding.performanceCameraLocalOffset);

        var targetTr = binding.performanceRoot.transform;
        targetTr.position = worldPos;

        if (binding.alignPerformanceRotationToCamera)
        {
            targetTr.rotation = camTr.rotation * Quaternion.Euler(binding.performanceRotationOffsetEuler);
        }
    }

    bool HasCurrentInstrument()
    {
        return currentInstrumentIndex >= 0 && currentInstrumentIndex < instruments.Count;
    }

    void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    void SetBehaviourEnabled(MonoBehaviour mb, bool enabled)
    {
        if (mb != null) mb.enabled = enabled;
    }

    void SetPerformanceBackButtonVisible(bool visible)
    {
        if (performanceBackToOptionButton == null) return;
        performanceBackToOptionButton.gameObject.SetActive(visible);
    }

    bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    bool IsPointerOverUI(int fingerId)
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(fingerId);
    }

    static void SetArModelsSuppressedForUi(bool suppressed)
    {
        ImageTargetHandler.SetUiOverlaySuppressedOnAll(suppressed);
    }
}
