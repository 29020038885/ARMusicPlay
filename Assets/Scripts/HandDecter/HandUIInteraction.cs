using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 手部 UI 交互 - 捏合手势点击 UI（按钮、翻页等），仅此功能。
/// </summary>
public class HandUIInteraction : MonoBehaviour
{
    [Header("组件引用")]
    [Tooltip("手势检测器（捏合=点击）")]
    public HandGestureDetector gestureDetector;

    [Tooltip("UI 摄像机，为空则用主摄像机")]
    public Camera uiCamera;

    [Header("交互设置")]
    [Tooltip("捏合后触发点击的冷却时间（秒），防误触")]
    [Range(0.1f, 1f)]
    public float interactionCooldown = 0.3f;

    [Tooltip("射线未命中时，用距离判定按钮的半径（像素）；越小越不易误点旁边按钮")]
    [Range(8f, 80f)]
    public float detectionRadius = 28f;

    [Tooltip("捏合点周围圆形采样半径（像素）：在圆心及圆周多点做 UI 射线，0 则仅圆心一点")]
    [Range(0f, 80f)]
    public float pinchRaycastProbeRadiusPixels = 24f;

    [Tooltip("圆周采样条数（不含圆心）；半径为 0 时不使用")]
    [Range(4, 24)]
    public int pinchRaycastCircleSegments = 12;

    [Tooltip("从系统判定捏合起，指尖需保持捏合至少该秒数后才允许触发点击（与 HandGestureDetector 稳定帧叠加，减轻误触）")]
    [Range(0f, 0.45f)]
    public float pinchHoldSecondsBeforeClick = 0.08f;

    [Tooltip("同一帧最多触发一次 UI 点击，避免双手/误检导致一次手势触发多次")]
    public bool singleClickPerFrame = true;

    [Tooltip("全局点击冷却（秒），跨手生效，防止一次手势连点两次")]
    [Range(0f, 0.5f)]
    public float globalInteractionCooldown = 0.15f;

    [Tooltip("开启后：一次连续捏合只触发一次点击，必须先松手再捏合才会再次触发")]
    public bool triggerOncePerPinchSession = true;

    [Header("状态")]
    public bool isInteracting;
    public Button currentInteractingButton;

    [Header("调试")]
    [Tooltip("勾选后在 Console 打印捏合与点击信息，便于排查无反应")]
    public bool showDebugLog = false;

    private EventSystem eventSystem;
    private List<Button> allButtons = new List<Button>();
    private Dictionary<int, float> lastInteractionTimes = new Dictionary<int, float>();
    private Dictionary<int, GameObject> currentInteractingByHand = new Dictionary<int, GameObject>();
    private float lastGlobalInteractionTime = -10f;
    private bool hasTriggeredInCurrentPinchSession = false;
    private readonly Dictionary<int, float> _pinchHoldStartTimeByHand = new Dictionary<int, float>();
    private readonly List<RaycastResult> _raycastScratch = new List<RaycastResult>(32);

    void Start()
    {
        if (gestureDetector == null)
            gestureDetector = FindObjectOfType<HandGestureDetector>();
        if (gestureDetector == null)
            Debug.LogWarning("HandUIInteraction: 未找到 HandGestureDetector");

        if (uiCamera == null)
        {
            Canvas c = FindObjectOfType<Canvas>();
            uiCamera = (c != null && c.renderMode == RenderMode.ScreenSpaceCamera) ? c.worldCamera : Camera.main;
        }

        eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
            Debug.LogWarning("HandUIInteraction: 场景中需要 EventSystem，否则无法检测 UI 点击");

        CollectAllButtons();
    }

    private void CollectAllButtons()
    {
        allButtons.Clear();
        Button[] buttons = FindObjectsOfType<Button>(true);
        allButtons.AddRange(buttons);
    }

    void Update()
    {
        if (gestureDetector == null || gestureDetector.pinchCount == 0)
        {
            currentInteractingButton = null;
            currentInteractingByHand.Clear();
            isInteracting = false;
            hasTriggeredInCurrentPinchSession = false;
            _pinchHoldStartTimeByHand.Clear();
            return;
        }

        if (triggerOncePerPinchSession && hasTriggeredInCurrentPinchSession)
            return;

        CheckPinchInteraction();
    }

    private void CheckPinchInteraction()
    {
        bool clickedThisFrame = false;

        // 清理已松开的捏合手
        List<int> toRemove = new List<int>();
        foreach (var handIndex in currentInteractingByHand.Keys)
        {
            bool stillPinching = false;
            foreach (var h in gestureDetector.pinchHands)
            {
                if (h.handIndex == handIndex) { stillPinching = true; break; }
            }
            if (!stillPinching) toRemove.Add(handIndex);
        }
        foreach (var i in toRemove)
        {
            currentInteractingByHand.Remove(i);
            lastInteractionTimes.Remove(i);
        }

        foreach (var hand in gestureDetector.pinchHands)
        {
            if (singleClickPerFrame && clickedThisFrame)
                break;

            if (Time.time - lastGlobalInteractionTime < globalInteractionCooldown)
                continue;

            int handIndex = hand.handIndex;
            if (lastInteractionTimes.TryGetValue(handIndex, out float t) && Time.time - t < interactionCooldown)
                continue;

            if (pinchHoldSecondsBeforeClick > 0f)
            {
                if (!_pinchHoldStartTimeByHand.TryGetValue(handIndex, out float pinchStart))
                {
                    _pinchHoldStartTimeByHand[handIndex] = Time.time;
                    continue;
                }
                if (Time.time - pinchStart < pinchHoldSecondsBeforeClick)
                    continue;
            }

            GameObject hit = RaycastUI(hand.pinchScreenPosition);
            if (hit != null)
            {
                if (!currentInteractingByHand.ContainsKey(handIndex) || currentInteractingByHand[handIndex] != hit)
                {
                    TriggerClick(hit, hand.pinchScreenPosition);
                    currentInteractingByHand[handIndex] = hit;
                    lastInteractionTimes[handIndex] = Time.time;
                    lastGlobalInteractionTime = Time.time;
                    clickedThisFrame = true;
                    hasTriggeredInCurrentPinchSession = true;
                    isInteracting = true;
                    currentInteractingButton = hit.GetComponent<Button>();
                    if (showDebugLog)
                        Debug.Log($"[手势UI] 点击: {hit.name}");
                }
            }
            else
            {
                Button nearest = FindNearestButton(hand.pinchScreenPosition);
                if (nearest != null)
                {
                    if (!currentInteractingByHand.ContainsKey(handIndex) || currentInteractingByHand[handIndex] != nearest.gameObject)
                    {
                        TriggerButtonClick(nearest);
                        currentInteractingByHand[handIndex] = nearest.gameObject;
                        lastInteractionTimes[handIndex] = Time.time;
                        lastGlobalInteractionTime = Time.time;
                        clickedThisFrame = true;
                        hasTriggeredInCurrentPinchSession = true;
                        isInteracting = true;
                        currentInteractingButton = nearest;
                        if (showDebugLog)
                            Debug.Log($"[手势UI] 点击(备用): {nearest.name}");
                    }
                }
                else
                {
                    if (showDebugLog && Time.frameCount % 90 == 0)
                        Debug.Log($"[手势UI] 未命中 UI 屏幕位置:({hand.pinchScreenPosition.x:F0},{hand.pinchScreenPosition.y:F0}) 请确认有 EventSystem、Canvas 带 GraphicRaycaster、按钮 Raycast Target 开启");
                    if (currentInteractingByHand.ContainsKey(handIndex))
                        currentInteractingByHand.Remove(handIndex);
                }
            }
        }

        PrunePinchHoldTimes(gestureDetector.pinchHands);

        isInteracting = currentInteractingByHand.Count > 0;
    }

    private void PrunePinchHoldTimes(List<HandGestureDetector.PinchHandInfo> activePinchHands)
    {
        if (_pinchHoldStartTimeByHand.Count == 0) return;
        var active = new HashSet<int>();
        for (int i = 0; i < activePinchHands.Count; i++)
            active.Add(activePinchHands[i].handIndex);
        List<int> toRemove = null;
        foreach (var kv in _pinchHoldStartTimeByHand)
        {
            if (!active.Contains(kv.Key))
            {
                if (toRemove == null) toRemove = new List<int>();
                toRemove.Add(kv.Key);
            }
        }
        if (toRemove != null)
        {
            for (int i = 0; i < toRemove.Count; i++)
                _pinchHoldStartTimeByHand.Remove(toRemove[i]);
        }
    }

    /// <summary>在捏合点及周围小圆上多点射线，优先圆心命中，再试圆周（比单点更容易点中按钮）。</summary>
    private GameObject RaycastUI(Vector2 screenPos)
    {
        GameObject center = RaycastUIAtPoint(screenPos);
        if (center != null)
            return center;
        if (eventSystem == null || pinchRaycastProbeRadiusPixels < 1f)
            return null;

        int n = Mathf.Clamp(pinchRaycastCircleSegments, 4, 24);
        for (int i = 0; i < n; i++)
        {
            float a = i * (Mathf.PI * 2f / n);
            Vector2 offset = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * pinchRaycastProbeRadiusPixels;
            GameObject hit = RaycastUIAtPoint(screenPos + offset);
            if (hit != null)
                return hit;
        }
        return null;
    }

    private GameObject RaycastUIAtPoint(Vector2 screenPos)
    {
        if (eventSystem == null) return null;
        var pointerData = new PointerEventData(eventSystem) { position = screenPos };
        _raycastScratch.Clear();
        eventSystem.RaycastAll(pointerData, _raycastScratch);
        return GetFirstClickableFromResults(_raycastScratch);
    }

    /// <summary>从射线结果里取第一个可点击的物体；若命中 Body 等遮挡，会沿层级找到背后的 Button</summary>
    private GameObject GetFirstClickableFromResults(List<RaycastResult> results)
    {
        if (eventSystem == null) return null;
        foreach (var r in results)
        {
            if (r.gameObject == null) continue;
            GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(r.gameObject);
            if (handler != null)
            {
                if (ShouldSuppressInteractionUnderModal(handler.transform))
                    continue;
                return handler;
            }
            var btn = r.gameObject.GetComponent<Button>();
            if (btn != null && btn.interactable)
            {
                if (ShouldSuppressInteractionUnderModal(btn.transform))
                    continue;
                return r.gameObject;
            }
            var toggle = r.gameObject.GetComponent<Toggle>();
            if (toggle != null && toggle.interactable)
            {
                if (ShouldSuppressInteractionUnderModal(toggle.transform))
                    continue;
                return r.gameObject;
            }
        }
        return null;
    }

    /// <summary>手势打开的面板显示时，不处理背景层上的点击（捏合会绕过 CanvasGroup.interactable）。</summary>
    private static bool ShouldSuppressInteractionUnderModal(Transform t)
    {
        return HandPanelGestureOpener.ShouldSuppressHandUiForTransform(t);
    }

    /// <summary>对命中的物体触发点击（Button / Toggle / 任意 IPointerClickHandler）</summary>
    private void TriggerClick(GameObject target, Vector2 screenPos)
    {
        if (target == null) return;
        if (ShouldSuppressInteractionUnderModal(target.transform))
            return;
        Button btn = target.GetComponent<Button>();
        if (btn != null && btn.interactable)
        {
            btn.onClick.Invoke();
            if (showDebugLog) Debug.Log($"[手势UI] 已触发 Button: {target.name}");
            return;
        }
        Toggle toggle = target.GetComponent<Toggle>();
        if (toggle != null && toggle.interactable)
        {
            toggle.isOn = !toggle.isOn;
            if (showDebugLog) Debug.Log($"[手势UI] 已切换 Toggle: {target.name}");
            return;
        }
        if (eventSystem != null)
        {
            var pointerData = new PointerEventData(eventSystem) { position = screenPos };
            ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerClickHandler);
            if (showDebugLog) Debug.Log($"[手势UI] 已发送点击事件: {target.name}");
        }
    }

    private void TriggerButtonClick(Button button)
    {
        if (button == null || !button.interactable)
            return;
        if (ShouldSuppressInteractionUnderModal(button.transform))
            return;
        button.onClick.Invoke();
    }

    private Button FindNearestButton(Vector2 screenPosition)
    {
        Camera cam = uiCamera != null ? uiCamera : Camera.main;
        if (cam == null) return null;

        Button nearest = null;
        float pickGate = detectionRadius;
        if (pinchRaycastProbeRadiusPixels > 0.5f)
            pickGate += pinchRaycastProbeRadiusPixels * 0.65f;
        float minDist = pickGate;

        foreach (Button b in allButtons)
        {
            if (b == null || !b.interactable || !b.gameObject.activeInHierarchy) 
                continue;

            if (ShouldSuppressInteractionUnderModal(b.transform))
                continue;

            // 关键：过滤掉被 RectMask2D 裁剪掉的按钮
            if (!IsButtonVisibleInMask(b))
                continue;

            RectTransform rt = b.GetComponent<RectTransform>();
            if (rt == null) continue;

            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector3 center = (corners[0] + corners[2]) * 0.5f;

            Vector2 btnScreen = RectTransformUtility.WorldToScreenPoint(cam, center);
            float d = Vector2.Distance(screenPosition, btnScreen);

            if (IsPointInsideRect(screenPosition, corners, cam) || d < minDist)
            {
                minDist = d;
                nearest = b;
            }
        }
        return nearest;
    }
    private bool IsButtonVisibleInMask(Button button)
    {
        RectMask2D mask = button.GetComponentInParent<RectMask2D>();
        if (mask == null) return true;

        RectTransform maskRect = mask.GetComponent<RectTransform>();
        RectTransform btnRect = button.GetComponent<RectTransform>();

        Vector3[] corners = new Vector3[4];
        btnRect.GetWorldCorners(corners);

        for (int i = 0; i < 4; i++)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(
                maskRect,
                RectTransformUtility.WorldToScreenPoint(uiCamera, corners[i]),
                uiCamera))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPointInsideRect(Vector2 screenPoint, Vector3[] worldCorners, Camera cam)
    {
        if (cam == null) return false;
        Vector2[] screenCorners = new Vector2[4];
        for (int i = 0; i < 4; i++)
            screenCorners[i] = RectTransformUtility.WorldToScreenPoint(cam, worldCorners[i]);
        return IsPointInPolygon(screenPoint, screenCorners);
    }

    private static bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
    {
        int n = polygon.Length;
        bool inside = false;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            if (((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
                (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x))
                inside = !inside;
        }
        return inside;
    }

    public void RefreshButtonList()
    {
        CollectAllButtons();
    }
}
