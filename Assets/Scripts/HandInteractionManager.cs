using UnityEngine;

/// <summary>
/// 手部交互管理器 - 只负责串联「手部数据 → 手势检测 → UI 点击」，便于一键配置。
/// </summary>
public class HandInteractionManager : MonoBehaviour
{
    [Header("组件引用")]
    public HandLandmarkDataCollector dataCollector;
    public HandGestureDetector gestureDetector;
    public HandUIInteraction uiInteraction;

    [Tooltip("是否在 Start 时自动查找/创建上述组件")]
    public bool autoSetup = true;

    [Header("调试")]
    [Tooltip("是否在屏幕上显示捏合状态")]
    public bool showDebugGUI = true;

    void Start()
    {
        if (autoSetup)
            SetupComponents();
    }

    [ContextMenu("自动设置组件")]
    public void SetupComponents()
    {
        if (dataCollector == null)
        {
            dataCollector = FindObjectOfType<HandLandmarkDataCollector>();
            if (dataCollector == null)
            {
                var go = new GameObject("HandLandmarkDataCollector");
                dataCollector = go.AddComponent<HandLandmarkDataCollector>();
            }
        }

        if (gestureDetector == null)
        {
            gestureDetector = FindObjectOfType<HandGestureDetector>();
            if (gestureDetector == null)
            {
                var go = new GameObject("HandGestureDetector");
                gestureDetector = go.AddComponent<HandGestureDetector>();
            }
        }
        if (gestureDetector != null && gestureDetector.dataCollector == null)
            gestureDetector.dataCollector = dataCollector;

        if (uiInteraction == null)
        {
            uiInteraction = FindObjectOfType<HandUIInteraction>();
            if (uiInteraction == null)
            {
                var go = new GameObject("HandUIInteraction");
                uiInteraction = go.AddComponent<HandUIInteraction>();
            }
        }
        if (uiInteraction != null && uiInteraction.gestureDetector == null)
            uiInteraction.gestureDetector = gestureDetector;
    }

    void OnGUI()
    {
        if (!showDebugGUI || gestureDetector == null || uiInteraction == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 280, 120));
        GUILayout.Box("手势 UI 状态");
        GUILayout.Label(gestureDetector.pinchCount > 0
            ? $"捏合: {gestureDetector.pinchCount} 只"
            : "捏合: 0");
        if (gestureDetector.pinchCount > 0 && gestureDetector.pinchHands.Count > 0)
            GUILayout.Label($"  位置: ({gestureDetector.pinchHands[0].pinchScreenPosition.x:F0}, {gestureDetector.pinchHands[0].pinchScreenPosition.y:F0})");
        GUILayout.Label(uiInteraction.isInteracting && uiInteraction.currentInteractingButton != null
            ? $"点击: {uiInteraction.currentInteractingButton.name}"
            : "点击: 无");
        GUILayout.EndArea();
    }
}
