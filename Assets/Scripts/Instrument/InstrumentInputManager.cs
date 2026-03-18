using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 乐器输入管理器 - 统一处理鼠标和触摸输入
/// 支持编辑器模式（鼠标）和移动设备模式（触摸）
/// </summary>
public class InstrumentInputManager : MonoBehaviour
{
    [Header("输入模式")]
    [Tooltip("是否使用触摸输入（移动设备）")]
    public bool useTouchInput = false;

    [Tooltip("编辑器模式下自动使用鼠标输入")]
    public bool autoDetectPlatform = true;

    [Header("射线检测")]
    [Tooltip("用于射线检测的摄像机")]
    public Camera inputCamera;

    [Tooltip("射线检测的最大距离")]
    public float raycastDistance = 100f;

    [Tooltip("射线检测的层级遮罩")]
    public LayerMask raycastLayerMask = -1;

    [Header("乐器引用")]
    [Tooltip("古琴控制器")]
    public GuqinController guqinController;

    [Tooltip("琵琶控制器")]
    public PipaController pipaController;

    [Header("乐器显示/隐藏")]
    [Tooltip("切换乐器时是否自动隐藏未激活的乐器物体（推荐开启）")]
    public bool toggleInstrumentGameObjectsOnSwitch = true;

    [Header("音量面板联动（可选）")]
    [Tooltip("可选：技术演示用每弦音量面板脚本。若指定，在切换乐器时会同步内部激活乐器与滑块数据。")]
    public InstrumentVolumePanel volumePanel;

    [Header("交互模式")]
    [Tooltip("当前激活的乐器：0-古琴，1-琵琶")]
    public int activeInstrument = 0;

    [Header("手势识别")]
    [Tooltip("长按时间阈值（用于区分点击和按住）")]
    public float longPressThreshold = 0.2f;

    [Tooltip("滑动距离阈值（用于检测扫弦）")]
    public float swipeThreshold = 50f;

    [Header("快捷键")]
    [Tooltip("切换乐器按键（避免与右键记录按品冲突）")]
    public KeyCode instrumentSwitchKey = KeyCode.Tab;

    [Header("古琴奏法测试键（PC）")]
    public KeyCode guqinSanYinKey = KeyCode.Alpha1;
    public KeyCode guqinAnYinKey = KeyCode.Alpha2;
    public KeyCode guqinFanYinKey = KeyCode.Alpha3;
    public KeyCode guqinTuiLaKey = KeyCode.Alpha4;

    [Header("琵琶奏法测试键（PC）")]
    public KeyCode pipaSanYinKey = KeyCode.Alpha1;
    public KeyCode pipaAnYinKey = KeyCode.Alpha2;
    public KeyCode pipaFanYinKey = KeyCode.Alpha3;
    public KeyCode pipaStrumKey = KeyCode.Alpha4;
    public KeyCode pipaTuiLaKey = KeyCode.Alpha5;

    [Header("调试")]
    [Tooltip("显示调试射线")]
    public bool showDebugRay = true;

    [Tooltip("显示调试信息")]
    public bool showDebugInfo = true;

    // 输入状态
    private bool isPressed = false;
    private float pressStartTime = 0f;
    private Vector2 pressStartPosition;
    private Vector2 currentInputPosition;
    private bool isDragging = false;

    // 触摸追踪（单指兼容）
    private int currentTouchId = -1;
    private Dictionary<int, (int stringIndex, int fretIndex)> touchToFretHold = new Dictionary<int, (int, int)>();
    private Dictionary<int, Vector2> touchStartOnString = new Dictionary<int, Vector2>();
    /// <summary>推拉：按品指按下品位时的锚点，位移用于计算推拉量（半音）</summary>
    private Dictionary<int, Vector2> touchFretAnchor = new Dictionary<int, Vector2>();
    /// <summary>记录每个触点上一帧命中的弦索引，用于“滑到新弦时自动触发一次拨弦”</summary>
    private Dictionary<int, int> touchLastStringIndex = new Dictionary<int, int>();
    [Tooltip("推拉：屏幕像素位移多少视为 1 半音（越大越不敏感）")]
    public float bendPixelsPerSemitone = 80f;

    void Start()
    {
        // 自动检测平台
        if (autoDetectPlatform)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            useTouchInput = false;
#elif UNITY_ANDROID || UNITY_IOS
            useTouchInput = true;
#endif
        }

        // 获取摄像机
        if (inputCamera == null)
        {
            inputCamera = Camera.main;
            if (inputCamera == null)
            {
                Debug.LogError("InstrumentInputManager: 未找到摄像机");
            }
        }

        // 查找乐器控制器
        if (guqinController == null)
        {
            guqinController = FindObjectOfType<GuqinController>();
        }

        if (pipaController == null)
        {
            pipaController = FindObjectOfType<PipaController>();
        }

        if (volumePanel == null)
        {
            volumePanel = FindObjectOfType<InstrumentVolumePanel>();
        }

        if (showDebugInfo)
        {
            Debug.Log($"InstrumentInputManager: 初始化完成，输入模式: {(useTouchInput ? "触摸" : "鼠标")}");
        }

        // 启动时按 activeInstrument 同步一次显示状态
        ApplyInstrumentVisibility(activeInstrument);
        if (volumePanel != null)
        {
            volumePanel.OnInstrumentChanged(activeInstrument);
        }
    }

    void Update()
    {
        // PC：古琴奏法 1/2/3
        if (!useTouchInput && activeInstrument == 0 && guqinController != null)
        {
            var prev = guqinController.currentTechnique;
            if (Input.GetKey(guqinSanYinKey)) guqinController.currentTechnique = GuqinTechnique.SanYin;
            else if (Input.GetKey(guqinAnYinKey)) guqinController.currentTechnique = GuqinTechnique.AnYin;
            else if (Input.GetKey(guqinFanYinKey)) guqinController.currentTechnique = GuqinTechnique.FanYin;
            else if (Input.GetKey(guqinTuiLaKey)) guqinController.currentTechnique = GuqinTechnique.TuiLa;
            if (showDebugInfo && guqinController.currentTechnique != prev)
                Debug.Log($"古琴奏法：{GetGuqinTechniqueName(guqinController.currentTechnique)}");
        }
        if (!useTouchInput && activeInstrument == 1 && pipaController != null)
        {
            var prev = pipaController.currentTechnique;
            if (Input.GetKey(pipaSanYinKey)) pipaController.currentTechnique = PipaTechnique.SanYin;
            else if (Input.GetKey(pipaAnYinKey)) pipaController.currentTechnique = PipaTechnique.AnYin;
            else if (Input.GetKey(pipaFanYinKey)) pipaController.currentTechnique = PipaTechnique.FanYin;
            else if (Input.GetKey(pipaStrumKey)) pipaController.currentTechnique = PipaTechnique.Strum;
            else if (Input.GetKey(pipaTuiLaKey)) pipaController.currentTechnique = PipaTechnique.TuiLa;
            if (showDebugInfo && pipaController.currentTechnique != prev)
                Debug.Log($"琵琶奏法：{GetPipaTechniqueName(pipaController.currentTechnique)}");
        }

        if (useTouchInput)
        {
            HandleTouchInput();
        }
        else
        {
            HandleMouseInput();
        }
    }

    /// <summary>
    /// 处理鼠标输入（编辑器模式）
    /// </summary>
    void HandleMouseInput()
    {
        currentInputPosition = Input.mousePosition;

        // 左键：拨弦（有记录的按品则先应用再拨弦）
        if (Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverUI())
            {
                Ray ray = GetRayFromScreenPosition(currentInputPosition);
                SendPluckEvent(ray);
            }
        }

        // 鼠标按住（拖拽）
        if (Input.GetMouseButton(0) && isPressed)
        {
            OnInputDrag(currentInputPosition);
        }

        // 鼠标松开
        if (Input.GetMouseButtonUp(0))
        {
            OnInputUp(currentInputPosition);
        }

        // 右键：记录按品（不拨弦，等左键拨弦时再应用）
        if (Input.GetMouseButtonDown(1))
        {
            if (!IsPointerOverUI())
            {
                Ray ray = GetRayFromScreenPosition(currentInputPosition);
                SendFretRecordEvent(ray);
            }
        }

        // 切换乐器用键盘按键，避免与右键「记录按品」冲突
        if (Input.GetKeyDown(instrumentSwitchKey))
        {
            if (!IsPointerOverUI())
                SwitchInstrument();
        }
    }

    /// <summary>
    /// 处理触摸输入（移动设备）
    /// </summary>
    void HandleTouchInput()
    {
        if (Input.touchCount == 0)
        {
            if (activeInstrument == 0 && guqinController != null) { guqinController.ClearAllLiveFretHolds(); guqinController.ClearAllBendAmounts(); touchToFretHold.Clear(); touchFretAnchor.Clear(); }
            if (activeInstrument == 1 && pipaController != null) { pipaController.ClearAllLiveFretHolds(); pipaController.ClearAllBendAmounts(); touchToFretHold.Clear(); touchStartOnString.Clear(); touchFretAnchor.Clear(); }
            if (isPressed) OnInputUp(currentInputPosition);
            return;
        }

        if (activeInstrument == 0 && guqinController != null)
        {
            HandleGuqinTouchMultiFinger();
            return;
        }
        if (activeInstrument == 1 && pipaController != null)
        {
            HandlePipaTouchMultiFinger();
            return;
        }

        // 其他：保持原单指逻辑
        Touch? activeTouch = null;
        foreach (Touch t in Input.touches)
        {
            if (currentTouchId == -1 || t.fingerId == currentTouchId)
            {
                activeTouch = t;
                currentTouchId = t.fingerId;
                break;
            }
        }
        if (!activeTouch.HasValue) return;
        Touch touch = activeTouch.Value;
        currentInputPosition = touch.position;
        switch (touch.phase)
        {
            case TouchPhase.Began:
                if (!IsPointerOverUI()) OnInputDown(touch.position);
                break;
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                if (isPressed) OnInputDrag(touch.position);
                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                OnInputUp(touch.position);
                currentTouchId = -1;
                break;
        }
    }

    /// <summary>琵琶触控：按品=一指持续按住品位（可滑动），拨弦=另一指点弦；在弦上滑动=扫弦。先更新按品/推拉状态再触发拨弦，保证与真实奏法一致。</summary>
    void HandlePipaTouchMultiFinger()
    {
        RaycastHit hit;
        // 第一遍：只更新 touchToFretHold / touchFretAnchor / touchLastStringIndex，并收集本帧要触发的拨弦
        var pluckRay = (Ray?)null;
        foreach (Touch t in Input.touches)
        {
            if (IsPointerOverUITouch(t.fingerId)) continue;
            Ray ray = GetRayFromScreenPosition(t.position);
            if (!Physics.Raycast(ray, out hit, raycastDistance)) continue;

            var str = hit.collider.GetComponent<InstrumentString>();
            if (str != null)
            {
                // 每次“点下”都要发声；若手指滑动跨到新弦，也触发一次新弦拨弦。
                // 关键：同一根弦上的一次触摸会经历 Began->...->Ended，如果 Ended 也命中弦，这里必须清理 lastIndex，
                // 否则下一次用同一个 fingerId 再点同一根弦时会被误判为“重复命中”而不发声。
                if (t.phase == TouchPhase.Began)
                {
                    pluckRay = ray;
                    touchLastStringIndex[t.fingerId] = str.stringIndex;
                }
                else if (t.phase == TouchPhase.Moved)
                {
                    int lastIndex = -1;
                    touchLastStringIndex.TryGetValue(t.fingerId, out lastIndex);
                    if (str.stringIndex != lastIndex)
                    {
                        pluckRay = ray;
                        touchLastStringIndex[t.fingerId] = str.stringIndex;
                    }
                }
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    touchLastStringIndex.Remove(t.fingerId);
                }
                continue;
            }

            if (pipaController.GetFretFromHit(hit, out int si, out int fi))
            {
                if (t.phase == TouchPhase.Began || t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                {
                    if (t.phase == TouchPhase.Began)
                        touchFretAnchor[t.fingerId] = t.position;
                    touchToFretHold[t.fingerId] = (si, fi);
                }
                else
                {
                    touchToFretHold.Remove(t.fingerId);
                    touchFretAnchor.Remove(t.fingerId);
                }
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                touchToFretHold.Remove(t.fingerId);
                touchStartOnString.Remove(t.fingerId);
                touchFretAnchor.Remove(t.fingerId);
                touchLastStringIndex.Remove(t.fingerId);
            }
        }
        // 先写入控制器：当前帧的按品与推拉量，这样拨弦时能用到“先按品再拨弦”的正确状态
        pipaController.ClearAllLiveFretHolds();
        pipaController.ClearAllBendAmounts();
        foreach (var kv in touchToFretHold)
        {
            pipaController.SetLiveFretHold(kv.Value.stringIndex, kv.Value.fretIndex);
            if (touchFretAnchor.TryGetValue(kv.Key, out Vector2 anchor))
            {
                Touch tt = default;
                foreach (Touch t in Input.touches) if (t.fingerId == kv.Key) { tt = t; break; }
                if (tt.fingerId == kv.Key && bendPixelsPerSemitone > 0f)
                    pipaController.SetBendAmount(kv.Value.stringIndex, (tt.position.y - anchor.y) / bendPixelsPerSemitone);
            }
        }
        if (pluckRay.HasValue) pipaController.HandlePluckWithLiveFret(pluckRay.Value);
    }

    /// <summary>古琴触控：按品=拇指持续按住品位（可滑动），拨弦=另一指点击琴弦。先更新按品/推拉状态再触发拨弦。</summary>
    void HandleGuqinTouchMultiFinger()
    {
        RaycastHit hit;
        Ray? pluckRay = null;
        foreach (Touch t in Input.touches)
        {
            if (IsPointerOverUITouch(t.fingerId)) continue;
            Ray ray = GetRayFromScreenPosition(t.position);
            if (!Physics.Raycast(ray, out hit, raycastDistance)) continue;

            var str = hit.collider.GetComponent<InstrumentString>();
            if (str != null)
            {
                // 古琴同样支持：触碰到弦或滑到新弦时触发拨弦
                if (t.phase == TouchPhase.Began)
                {
                    pluckRay = ray;
                    touchLastStringIndex[t.fingerId] = str.stringIndex;
                }
                else if (t.phase == TouchPhase.Moved)
                {
                    int lastIndex = -1;
                    touchLastStringIndex.TryGetValue(t.fingerId, out lastIndex);
                    if (str.stringIndex != lastIndex)
                    {
                        pluckRay = ray;
                        touchLastStringIndex[t.fingerId] = str.stringIndex;
                    }
                }
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    touchLastStringIndex.Remove(t.fingerId);
                }
                continue;
            }

            if (guqinController.GetFretFromHit(hit, out int si, out int fi))
            {
                if (t.phase == TouchPhase.Began || t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                {
                    if (t.phase == TouchPhase.Began)
                        touchFretAnchor[t.fingerId] = t.position;
                    touchToFretHold[t.fingerId] = (si, fi);
                }
                else
                {
                    touchToFretHold.Remove(t.fingerId);
                    touchFretAnchor.Remove(t.fingerId);
                }
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                touchToFretHold.Remove(t.fingerId);
                touchFretAnchor.Remove(t.fingerId);
                touchLastStringIndex.Remove(t.fingerId);
            }
        }
        guqinController.ClearAllLiveFretHolds();
        guqinController.ClearAllBendAmounts();
        foreach (var kv in touchToFretHold)
        {
            guqinController.SetLiveFretHold(kv.Value.stringIndex, kv.Value.fretIndex);
            if (touchFretAnchor.TryGetValue(kv.Key, out Vector2 anchor))
            {
                Touch tt = default;
                foreach (Touch t in Input.touches) if (t.fingerId == kv.Key) { tt = t; break; }
                if (tt.fingerId == kv.Key && bendPixelsPerSemitone > 0f)
                    guqinController.SetBendAmount(kv.Value.stringIndex, (tt.position.y - anchor.y) / bendPixelsPerSemitone);
            }
        }
        if (pluckRay.HasValue) { guqinController.HandlePluckWithLiveFret(pluckRay.Value); if (showDebugInfo) Debug.Log("[触控] 拨弦"); }
    }

    /// <summary>触摸时判断该指是否点在 UI 上（古琴多指用）</summary>
    bool IsPointerOverUITouch(int fingerId)
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject(fingerId);
    }

    /// <summary>
    /// 输入按下
    /// </summary>
    void OnInputDown(Vector2 position)
    {
        isPressed = true;
        pressStartTime = Time.time;
        pressStartPosition = position;
        isDragging = false;

        // 发送射线
        Ray ray = GetRayFromScreenPosition(position);
        
        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * raycastDistance, Color.green, 1f);
        }

        // 🔧 按下时立即拨弦（点击发声）
        SendClickEvent(ray);

        if (showDebugInfo)
        {
            Debug.Log($"🎵 [InputManager] 输入按下（立即发声）: {position}");
        }
    }

    /// <summary>
    /// 输入拖拽
    /// </summary>
    void OnInputDrag(Vector2 position)
    {
        if (!isPressed) return;

        float distance = Vector2.Distance(pressStartPosition, position);

        // 检测滑动手势
        if (distance > swipeThreshold && !isDragging)
        {
            isDragging = true;
            
            // 检测滑动方向（用于扫弦）
            Vector2 direction = (position - pressStartPosition).normalized;
            
            if (Mathf.Abs(direction.y) > 0.5f) // 垂直滑动
            {
                bool upward = direction.y > 0;
                HandleSwipe(upward);
            }

            if (showDebugInfo)
            {
                Debug.Log($"检测到滑动: 距离 {distance}, 方向 {direction}");
            }
        }

        // 更新射线位置
        Ray ray = GetRayFromScreenPosition(position);
        
        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * raycastDistance, Color.yellow, 0.1f);
        }
    }

    /// <summary>
    /// 输入松开
    /// </summary>
    void OnInputUp(Vector2 position)
    {
        if (!isPressed) return;

        float pressDuration = Time.time - pressStartTime;
        float distance = Vector2.Distance(pressStartPosition, position);

        // 🔧 松开时不再发声，只重置状态
        
        isPressed = false;
        isDragging = false;

        if (showDebugInfo)
        {
            Debug.Log($"🔵 [InputManager] 输入松开（不发声）: {position}, 时长 {pressDuration:F2}s");
        }
    }

    /// <summary>
    /// 从屏幕坐标获取射线
    /// </summary>
    Ray GetRayFromScreenPosition(Vector2 screenPosition)
    {
        if (inputCamera != null)
        {
            return inputCamera.ScreenPointToRay(screenPosition);
        }
        return new Ray(Vector3.zero, Vector3.forward);
    }

    /// <summary>
    /// 发送拨弦事件（左键）：有记录的按品则先应用再拨弦，然后清空该弦记录
    /// </summary>
    void SendPluckEvent(Ray ray)
    {
        switch (activeInstrument)
        {
            case 0:
                if (guqinController != null) guqinController.HandlePluck(ray);
                break;
            case 1:
                if (pipaController != null) pipaController.HandlePluck(ray);
                break;
        }
    }

    /// <summary>
    /// 发送按品记录事件（右键）：只记录按品，不拨弦
    /// </summary>
    void SendFretRecordEvent(Ray ray)
    {
        switch (activeInstrument)
        {
            case 0:
                if (guqinController != null) guqinController.HandleFretRecord(ray);
                break;
            case 1:
                if (pipaController != null) pipaController.HandleFretRecord(ray);
                break;
        }
    }

    /// <summary>
    /// 发送点击事件（兼容触摸等：直接拨弦）
    /// </summary>
    void SendClickEvent(Ray ray)
    {
        switch (activeInstrument)
        {
            case 0:
                if (guqinController != null) guqinController.HandlePluck(ray);
                break;
            case 1:
                if (pipaController != null) pipaController.HandlePluck(ray);
                break;
        }
    }

    /// <summary>
    /// 发送按压事件到当前激活的乐器
    /// </summary>
    void SendPressEvent(Ray ray, bool pressed)
    {
        switch (activeInstrument)
        {
            case 0: // 古琴
                if (guqinController != null)
                {
                    guqinController.HandlePress(ray, pressed);
                }
                break;

            case 1: // 琵琶
                if (pipaController != null)
                {
                    pipaController.HandlePress(ray, pressed);
                }
                break;
        }
    }

    /// <summary>
    /// 处理滑动手势（扫弦）
    /// </summary>
    void HandleSwipe(bool upward)
    {
        // 目前只有琵琶支持扫弦
        if (activeInstrument == 1 && pipaController != null)
        {
            pipaController.StrumStrings(upward);
        }

        if (showDebugInfo)
        {
            Debug.Log($"扫弦: {(upward ? "向上" : "向下")}");
        }
    }

    static string GetGuqinTechniqueName(GuqinTechnique t)
    {
        switch (t) { case GuqinTechnique.SanYin: return "散音（空弦）"; case GuqinTechnique.AnYin: return "按音（按品）"; case GuqinTechnique.FanYin: return "泛音"; case GuqinTechnique.TuiLa: return "推拉"; default: return t.ToString(); }
    }
    static string GetPipaTechniqueName(PipaTechnique t)
    {
        switch (t) { case PipaTechnique.SanYin: return "散音（空弦）"; case PipaTechnique.AnYin: return "按音（按品/滑音）"; case PipaTechnique.FanYin: return "泛音"; case PipaTechnique.Strum: return "扫弦"; case PipaTechnique.TuiLa: return "推拉"; default: return t.ToString(); }
    }

    /// <summary>
    /// 切换当前激活的乐器
    /// </summary>
    public void SwitchInstrument()
    {
        SetActiveInstrument((activeInstrument + 1) % 2);
    }

    /// <summary>
    /// 设置激活的乐器
    /// </summary>
    public void SetActiveInstrument(int index)
    {
        if (index < 0 || index > 1) return;

        // 切换前做一次清理，避免多指按品/推拉残留到下一个乐器
        StopAllInstruments();
        touchToFretHold.Clear();
        touchStartOnString.Clear();
        touchFretAnchor.Clear();
        currentTouchId = -1;

        activeInstrument = index;

        ApplyInstrumentVisibility(activeInstrument);

        if (volumePanel != null)
        {
            volumePanel.OnInstrumentChanged(activeInstrument);
        }

        string instrumentName = activeInstrument == 0 ? "古琴" : "琵琶";
        Debug.Log($"激活乐器: {instrumentName}");
    }

    /// <summary>
    /// UI 按钮可直接绑定：切到古琴并显示古琴、隐藏琵琶（若开启 toggleInstrumentGameObjectsOnSwitch）
    /// </summary>
    public void ActivateGuqin()
    {
        SetActiveInstrument(0);
    }

    /// <summary>
    /// UI 按钮可直接绑定：切到琵琶并显示琵琶、隐藏古琴（若开启 toggleInstrumentGameObjectsOnSwitch）
    /// </summary>
    public void ActivatePipa()
    {
        SetActiveInstrument(1);
    }

    /// <summary>
    /// 将 activeInstrument 同步到乐器物体的显示/隐藏状态。
    /// 约定：guqinController / pipaController 挂在对应乐器根节点上。
    /// </summary>
    void ApplyInstrumentVisibility(int instrumentIndex)
    {
        if (!toggleInstrumentGameObjectsOnSwitch) return;

        GameObject guqinGO = guqinController != null ? guqinController.gameObject : null;
        GameObject pipaGO = pipaController != null ? pipaController.gameObject : null;

        if (guqinGO != null) guqinGO.SetActive(instrumentIndex == 0);
        if (pipaGO != null) pipaGO.SetActive(instrumentIndex == 1);
    }

    /// <summary>
    /// 检查指针是否在UI上
    /// </summary>
    bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        if (useTouchInput && Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        return EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>
    /// 停止所有乐器的声音
    /// </summary>
    public void StopAllInstruments()
    {
        if (guqinController != null)
        {
            guqinController.StopAllSounds();
        }

        if (pipaController != null)
        {
            pipaController.StopAllSounds();
        }
    }

    void OnDrawGizmos()
    {
        // 在场景视图中显示当前输入位置
        if (isPressed && inputCamera != null)
        {
            Ray ray = GetRayFromScreenPosition(currentInputPosition);
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(ray.origin, ray.direction * raycastDistance);
        }
    }
}

