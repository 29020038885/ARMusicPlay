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

    [Header("琵琶奏法测试键（PC）")]
    public KeyCode pipaSanYinKey = KeyCode.Alpha1;
    public KeyCode pipaAnYinKey = KeyCode.Alpha2;
    public KeyCode pipaFanYinKey = KeyCode.Alpha3;
    public KeyCode pipaStrumKey = KeyCode.Alpha4;

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
    /// <summary>古琴/琵琶：手指ID → (弦索引, 品索引)，表示该指正在按住的品位（滑音即该指滑动更新）</summary>
    private Dictionary<int, (int stringIndex, int fretIndex)> touchToFretHold = new Dictionary<int, (int, int)>();
    /// <summary>琵琶触控：在弦上按下时记录起点，用于区分点击拨弦与滑动扫弦</summary>
    private Dictionary<int, Vector2> touchStartOnString = new Dictionary<int, Vector2>();

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

        if (showDebugInfo)
        {
            Debug.Log($"InstrumentInputManager: 初始化完成，输入模式: {(useTouchInput ? "触摸" : "鼠标")}");
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
            if (showDebugInfo && guqinController.currentTechnique != prev)
                Debug.Log($"古琴奏法：{GetGuqinTechniqueName(guqinController.currentTechnique)}");
        }
        // PC：琵琶奏法 1/2/3/4（4=扫弦）
        if (!useTouchInput && activeInstrument == 1 && pipaController != null)
        {
            var prev = pipaController.currentTechnique;
            if (Input.GetKey(pipaSanYinKey)) pipaController.currentTechnique = PipaTechnique.SanYin;
            else if (Input.GetKey(pipaAnYinKey)) pipaController.currentTechnique = PipaTechnique.AnYin;
            else if (Input.GetKey(pipaFanYinKey)) pipaController.currentTechnique = PipaTechnique.FanYin;
            else if (Input.GetKey(pipaStrumKey)) pipaController.currentTechnique = PipaTechnique.Strum;
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
            if (activeInstrument == 0 && guqinController != null) { guqinController.ClearAllLiveFretHolds(); touchToFretHold.Clear(); }
            if (activeInstrument == 1 && pipaController != null) { pipaController.ClearAllLiveFretHolds(); touchToFretHold.Clear(); touchStartOnString.Clear(); }
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

    /// <summary>琵琶触控：按品=一指持续按住品位（可滑动），拨弦=另一指点弦；在弦上滑动=扫弦</summary>
    void HandlePipaTouchMultiFinger()
    {
        RaycastHit hit;
        foreach (Touch t in Input.touches)
        {
            if (IsPointerOverUITouch(t.fingerId)) continue;
            Ray ray = GetRayFromScreenPosition(t.position);
            if (!Physics.Raycast(ray, out hit, raycastDistance)) continue;

            var str = hit.collider.GetComponent<InstrumentString>();
            if (str != null)
            {
                if (t.phase == TouchPhase.Began)
                    touchStartOnString[t.fingerId] = t.position;
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    if (touchStartOnString.TryGetValue(t.fingerId, out Vector2 start))
                    {
                        touchStartOnString.Remove(t.fingerId);
                        float dist = Vector2.Distance(start, t.position);
                        Vector2 dir = (t.position - start).normalized;
                        if (dist > swipeThreshold && Mathf.Abs(dir.y) > 0.5f)
                        {
                            pipaController.StrumStrings(dir.y > 0);
                            if (showDebugInfo) Debug.Log("[触控] 琵琶扫弦");
                        }
                        else
                            pipaController.HandlePluckWithLiveFret(GetRayFromScreenPosition(t.position));
                    }
                }
                continue;
            }

            if (pipaController.GetFretFromHit(hit, out int si, out int fi))
            {
                if (t.phase == TouchPhase.Began || t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                    touchToFretHold[t.fingerId] = (si, fi);
                else
                    touchToFretHold.Remove(t.fingerId);
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                touchToFretHold.Remove(t.fingerId);
                touchStartOnString.Remove(t.fingerId);
            }
        }
        pipaController.ClearAllLiveFretHolds();
        foreach (var kv in touchToFretHold)
            pipaController.SetLiveFretHold(kv.Value.stringIndex, kv.Value.fretIndex);
    }

    /// <summary>
    /// 古琴触控：按品=拇指持续按住品位（可滑动），拨弦=另一指点击琴弦
    /// </summary>
    void HandleGuqinTouchMultiFinger()
    {
        RaycastHit hit;
        bool pluckTriggered = false;

        foreach (Touch t in Input.touches)
        {
            if (IsPointerOverUITouch(t.fingerId)) continue;
            Ray ray = GetRayFromScreenPosition(t.position);
            if (!Physics.Raycast(ray, out hit, raycastDistance)) continue;

            var str = hit.collider.GetComponent<InstrumentString>();
            if (str != null)
            {
                // 点在弦上 → 拨弦（散音/按音/泛音由当前拇指按品状态决定）
                if (t.phase == TouchPhase.Began)
                {
                    guqinController.HandlePluckWithLiveFret(ray);
                    pluckTriggered = true;
                    if (showDebugInfo) Debug.Log("[触控] 拨弦");
                }
                continue;
            }

            // 点在品位上 → 记录该指「按品」（每帧更新，滑音即拇指滑动）
            if (guqinController.GetFretFromHit(hit, out int si, out int fi))
            {
                if (t.phase == TouchPhase.Began || t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                    touchToFretHold[t.fingerId] = (si, fi);
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    touchToFretHold.Remove(t.fingerId);
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                touchToFretHold.Remove(t.fingerId);
            }
        }

        // 同步实时按品到古琴（多指同弦时保留最后一个）
        guqinController.ClearAllLiveFretHolds();
        foreach (var kv in touchToFretHold)
            guqinController.SetLiveFretHold(kv.Value.stringIndex, kv.Value.fretIndex);
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
        switch (t) { case GuqinTechnique.SanYin: return "散音（空弦）"; case GuqinTechnique.AnYin: return "按音（按品）"; case GuqinTechnique.FanYin: return "泛音"; default: return t.ToString(); }
    }
    static string GetPipaTechniqueName(PipaTechnique t)
    {
        switch (t) { case PipaTechnique.SanYin: return "散音（空弦）"; case PipaTechnique.AnYin: return "按音（按品/滑音）"; case PipaTechnique.FanYin: return "泛音"; case PipaTechnique.Strum: return "扫弦"; default: return t.ToString(); }
    }

    /// <summary>
    /// 切换当前激活的乐器
    /// </summary>
    public void SwitchInstrument()
    {
        activeInstrument = (activeInstrument + 1) % 2;
        
        string instrumentName = activeInstrument == 0 ? "古琴" : "琵琶";
        Debug.Log($"切换到: {instrumentName}");
    }

    /// <summary>
    /// 设置激活的乐器
    /// </summary>
    public void SetActiveInstrument(int index)
    {
        if (index >= 0 && index <= 1)
        {
            activeInstrument = index;
            
            string instrumentName = activeInstrument == 0 ? "古琴" : "琵琶";
            Debug.Log($"激活乐器: {instrumentName}");
        }
    }

    /// <summary>
    /// 检查指针是否在UI上
    /// </summary>
    bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        if (useTouchInput)
        {
            if (Input.touchCount > 0)
            {
                return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            }
        }
        else
        {
            return EventSystem.current.IsPointerOverGameObject();
        }

        return false;
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

