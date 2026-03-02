using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 手部UI交互系统 - 检测手部位置是否在UI按钮上并触发交互
/// </summary>
public class HandUIInteraction : MonoBehaviour
{
    [Header("组件引用")]
    [Tooltip("手势检测器")]
    public HandGestureDetector gestureDetector;

    [Tooltip("UI摄像机（用于UI交互，如果为空则使用主摄像机）")]
    public Camera uiCamera;

    [Header("交互设置")]
    [Tooltip("是否启用自动检测场景中的所有按钮")]
    public bool autoDetectButtons = true;

    [Tooltip("手动指定的按钮列表（如果autoDetectButtons为false）")]
    public List<Button> targetButtons = new List<Button>();

    [Tooltip("交互触发延迟（秒），避免频繁触发")]
    [Range(0f, 1f)]
    public float interactionCooldown = 0.3f;

    [Tooltip("手部位置检测半径（屏幕像素）")]
    [Range(10f, 100f)]
    public float detectionRadius = 30f;

    [Header("状态")]
    [Tooltip("当前交互的按钮")]
    public Button currentInteractingButton;

    [Tooltip("是否正在交互")]
    public bool isInteracting = false;

    private float lastInteractionTime = 0f;
    private List<Button> allButtons = new List<Button>();
    private GraphicRaycaster graphicRaycaster;
    private EventSystem eventSystem;

    void Start()
    {
        // 查找手势检测器
        if (gestureDetector == null)
        {
            gestureDetector = FindObjectOfType<HandGestureDetector>();
            if (gestureDetector == null)
            {
                Debug.LogWarning("HandUIInteraction: 未找到 HandGestureDetector");
            }
        }

        // 查找UI摄像机
        if (uiCamera == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                uiCamera = canvas.worldCamera;
            }
            else
            {
                uiCamera = Camera.main;
            }
        }

        // 查找EventSystem和GraphicRaycaster
        eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogWarning("HandUIInteraction: 场景中未找到 EventSystem，请添加 EventSystem 组件");
        }

        Canvas canvasComponent = FindObjectOfType<Canvas>();
        if (canvasComponent != null)
        {
            graphicRaycaster = canvasComponent.GetComponent<GraphicRaycaster>();
            if (graphicRaycaster == null)
            {
                graphicRaycaster = canvasComponent.gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        // 收集所有按钮
        if (autoDetectButtons)
        {
            CollectAllButtons();
        }
        else
        {
            allButtons = new List<Button>(targetButtons);
        }
    }

    [Header("多手交互设置")]
    [Tooltip("是否支持两只手同时交互")]
    public bool allowMultipleHands = true;

    [Tooltip("当前交互的按钮（每只手）")]
    public Dictionary<int, Button> currentInteractingButtons = new Dictionary<int, Button>();

    [Tooltip("每只手的最后交互时间")]
    private Dictionary<int, float> lastInteractionTimes = new Dictionary<int, float>();

    void Update()
    {
        if (gestureDetector == null || gestureDetector.clenchedHandCount == 0)
        {
            currentInteractingButton = null;
            currentInteractingButtons.Clear();
            isInteracting = false;
            return;
        }

        // 检测所有握拳手的位置是否在按钮上
        CheckAllHandsInteraction();
    }

    /// <summary>
    /// 收集场景中的所有按钮
    /// </summary>
    private void CollectAllButtons()
    {
        allButtons.Clear();
        Button[] buttons = FindObjectsOfType<Button>(true); // true 表示包括未激活的
        allButtons.AddRange(buttons);
        Debug.Log($"HandUIInteraction: 找到 {allButtons.Count} 个按钮");
    }

    /// <summary>
    /// 检查所有手的按钮交互
    /// </summary>
    private void CheckAllHandsInteraction()
    {
        // 清理不再握拳的手的交互状态
        List<int> handsToRemove = new List<int>();
        foreach (var handIndex in currentInteractingButtons.Keys)
        {
            bool handStillClenched = false;
            foreach (var hand in gestureDetector.clenchedHands)
            {
                if (hand.handIndex == handIndex)
                {
                    handStillClenched = true;
                    break;
                }
            }
            if (!handStillClenched)
            {
                handsToRemove.Add(handIndex);
            }
        }
        foreach (var handIndex in handsToRemove)
        {
            currentInteractingButtons.Remove(handIndex);
            lastInteractionTimes.Remove(handIndex);
        }

        // 检查每只握拳手
        foreach (var handInfo in gestureDetector.clenchedHands)
        {
            int handIndex = handInfo.handIndex;

            // 检查冷却时间
            if (lastInteractionTimes.ContainsKey(handIndex))
            {
                if (Time.time - lastInteractionTimes[handIndex] < interactionCooldown)
                {
                    continue;
                }
            }

            // 检测手部位置是否在按钮上
            Button hitButton = CheckButtonInteraction(handInfo.centerScreenPosition);

            if (hitButton != null)
            {
                // 检查这只手是否已经在交互这个按钮
                if (!currentInteractingButtons.ContainsKey(handIndex) || 
                    currentInteractingButtons[handIndex] != hitButton)
                {
                    // 触发按钮点击
                    TriggerButtonClick(hitButton);
                    currentInteractingButtons[handIndex] = hitButton;
                    lastInteractionTimes[handIndex] = Time.time;
                    isInteracting = true;

                    // 兼容旧接口（第一只手）
                    if (handIndex == 0)
                    {
                        currentInteractingButton = hitButton;
                    }
                }
            }
            else
            {
                // 如果这只手不再在按钮上，清除状态
                if (currentInteractingButtons.ContainsKey(handIndex))
                {
                    currentInteractingButtons.Remove(handIndex);
                    if (handIndex == 0)
                    {
                        currentInteractingButton = null;
                    }
                }
            }
        }

        // 更新总体交互状态
        isInteracting = currentInteractingButtons.Count > 0;
    }

    /// <summary>
    /// 检查指定位置的按钮交互
    /// </summary>
    private Button CheckButtonInteraction(Vector2 screenPos)
    {
        // 方法1：使用射线检测
        Button hitButton = RaycastButton(screenPos);
        
        // 方法2：直接计算距离（备用方法）
        if (hitButton == null)
        {
            hitButton = FindNearestButton(screenPos);
        }

        return hitButton;
    }

    /// <summary>
    /// 使用射线检测按钮
    /// </summary>
    private Button RaycastButton(Vector2 screenPosition)
    {
        if (eventSystem == null || graphicRaycaster == null)
        {
            return null;
        }

        // 创建指针事件数据
        PointerEventData pointerData = new PointerEventData(eventSystem);
        pointerData.position = screenPosition;

        // 执行射线检测
        List<RaycastResult> results = new List<RaycastResult>();
        graphicRaycaster.Raycast(pointerData, results);

        // 查找第一个按钮
        foreach (RaycastResult result in results)
        {
            Button button = result.gameObject.GetComponent<Button>();
            if (button != null && button.interactable)
            {
                return button;
            }
        }

        return null;
    }

    /// <summary>
    /// 查找距离手部位置最近的按钮（备用方法）
    /// </summary>
    private Button FindNearestButton(Vector2 screenPosition)
    {
        Button nearestButton = null;
        float minDistance = detectionRadius;

        foreach (Button button in allButtons)
        {
            if (button == null || !button.interactable || !button.gameObject.activeInHierarchy)
            {
                continue;
            }

            // 获取按钮的屏幕位置
            RectTransform rectTransform = button.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                continue;
            }

            Vector3[] worldCorners = new Vector3[4];
            rectTransform.GetWorldCorners(worldCorners);

            // 计算按钮中心点的屏幕坐标
            Vector3 buttonCenter = (worldCorners[0] + worldCorners[2]) / 2f;
            Vector2 buttonScreenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, buttonCenter);

            // 计算距离
            float distance = Vector2.Distance(screenPosition, buttonScreenPos);

            // 检查是否在按钮范围内
            bool isInsideButton = IsPointInsideRect(screenPosition, worldCorners, uiCamera);

            if (isInsideButton || distance < minDistance)
            {
                minDistance = distance;
                nearestButton = button;
            }
        }

        return nearestButton;
    }

    /// <summary>
    /// 检查点是否在矩形内
    /// </summary>
    private bool IsPointInsideRect(Vector2 screenPoint, Vector3[] worldCorners, Camera camera)
    {
        // 将世界坐标转换为屏幕坐标
        Vector2[] screenCorners = new Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            screenCorners[i] = RectTransformUtility.WorldToScreenPoint(camera, worldCorners[i]);
        }

        // 使用射线法判断点是否在多边形内
        return IsPointInPolygon(screenPoint, screenCorners);
    }

    /// <summary>
    /// 使用射线法判断点是否在多边形内
    /// </summary>
    private bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
    {
        int intersections = 0;
        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 p1 = polygon[i];
            Vector2 p2 = polygon[(i + 1) % polygon.Length];

            if (RayIntersectsSegment(point, p1, p2))
            {
                intersections++;
            }
        }
        return (intersections % 2) == 1;
    }

    /// <summary>
    /// 检查射线是否与线段相交
    /// </summary>
    private bool RayIntersectsSegment(Vector2 point, Vector2 segStart, Vector2 segEnd)
    {
        if (segStart.y > segEnd.y)
        {
            Vector2 temp = segStart;
            segStart = segEnd;
            segEnd = temp;
        }

        if (point.y < segStart.y || point.y > segEnd.y)
        {
            return false;
        }

        if (point.x > Mathf.Max(segStart.x, segEnd.x))
        {
            return false;
        }

        if (point.x < Mathf.Min(segStart.x, segEnd.x))
        {
            return true;
        }

        float red = (point.y - segStart.y) / (segEnd.y - segStart.y);
        float blue = (point.x - segStart.x) / (segEnd.x - segStart.x);
        return blue >= red;
    }

    /// <summary>
    /// 触发按钮点击
    /// </summary>
    private void TriggerButtonClick(Button button)
    {
        if (button != null && button.interactable)
        {
            button.onClick.Invoke();
            Debug.Log($"HandUIInteraction: 触发按钮点击 - {button.name}");
        }
    }

    /// <summary>
    /// 手动添加按钮到检测列表
    /// </summary>
    public void AddButton(Button button)
    {
        if (button != null && !allButtons.Contains(button))
        {
            allButtons.Add(button);
        }
    }

    /// <summary>
    /// 手动移除按钮
    /// </summary>
    public void RemoveButton(Button button)
    {
        if (allButtons.Contains(button))
        {
            allButtons.Remove(button);
        }
    }

    /// <summary>
    /// 刷新按钮列表
    /// </summary>
    public void RefreshButtonList()
    {
        if (autoDetectButtons)
        {
            CollectAllButtons();
        }
    }
}

