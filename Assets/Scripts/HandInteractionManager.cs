using UnityEngine;

/// <summary>
/// 手部交互管理器 - 统一管理手部检测和UI交互系统
/// </summary>
public class HandInteractionManager : MonoBehaviour
{
    [Header("组件引用")]
    [Tooltip("手部特征点数据收集器")]
    public HandLandmarkDataCollector dataCollector;

    [Tooltip("手势检测器")]
    public HandGestureDetector gestureDetector;

    [Tooltip("UI交互系统")]
    public HandUIInteraction uiInteraction;

    [Header("自动设置")]
    [Tooltip("是否在Start时自动查找和设置所有组件")]
    public bool autoSetup = true;

    void Start()
    {
        if (autoSetup)
        {
            SetupComponents();
        }
    }

    /// <summary>
    /// 自动设置所有组件
    /// </summary>
    [ContextMenu("自动设置组件")]
    public void SetupComponents()
    {
        // 查找或创建 HandLandmarkDataCollector
        if (dataCollector == null)
        {
            dataCollector = FindObjectOfType<HandLandmarkDataCollector>();
            if (dataCollector == null)
            {
                GameObject collectorObj = new GameObject("HandLandmarkDataCollector");
                dataCollector = collectorObj.AddComponent<HandLandmarkDataCollector>();
                Debug.Log("HandInteractionManager: 已创建 HandLandmarkDataCollector");
            }
        }

        // 查找或创建 HandGestureDetector
        if (gestureDetector == null)
        {
            gestureDetector = FindObjectOfType<HandGestureDetector>();
            if (gestureDetector == null)
            {
                GameObject detectorObj = new GameObject("HandGestureDetector");
                gestureDetector = detectorObj.AddComponent<HandGestureDetector>();
                Debug.Log("HandInteractionManager: 已创建 HandGestureDetector");
            }
        }

        // 设置手势检测器的数据源
        if (gestureDetector != null && gestureDetector.dataCollector == null)
        {
            gestureDetector.dataCollector = dataCollector;
        }

        // 查找或创建 HandUIInteraction
        if (uiInteraction == null)
        {
            uiInteraction = FindObjectOfType<HandUIInteraction>();
            if (uiInteraction == null)
            {
                GameObject interactionObj = new GameObject("HandUIInteraction");
                uiInteraction = interactionObj.AddComponent<HandUIInteraction>();
                Debug.Log("HandInteractionManager: 已创建 HandUIInteraction");
            }
        }

        // 设置UI交互系统的组件引用
        if (uiInteraction != null)
        {
            if (uiInteraction.gestureDetector == null)
            {
                uiInteraction.gestureDetector = gestureDetector;
            }
        }

        Debug.Log("HandInteractionManager: 组件设置完成");
    }

    /// <summary>
    /// 在Inspector中显示当前状态
    /// </summary>
    void OnGUI()
    {
        if (gestureDetector == null || uiInteraction == null)
        {
            return;
        }

        GUILayout.BeginArea(new Rect(10, 10, 350, 250));
        GUILayout.Box("手部交互系统状态", GUILayout.Width(330));

        // 握拳手势状态
        string gestureStatus = gestureDetector.clenchedHandCount > 0 
            ? $"✓ 检测到 {gestureDetector.clenchedHandCount} 只握拳手" 
            : "✗ 未检测到握拳";
        GUILayout.Label($"手势状态: {gestureStatus}");

        // 显示每只握拳手的信息
        if (gestureDetector.clenchedHands.Count > 0)
        {
            GUILayout.Space(5);
            GUILayout.Label("握拳手信息:", GUI.skin.box);
            foreach (var hand in gestureDetector.clenchedHands)
            {
                string handName = hand.handedness == "Left" ? "左手" : 
                                 hand.handedness == "Right" ? "右手" : 
                                 $"手{hand.handIndex}";
                GUILayout.Label($"  {handName}: ({hand.centerScreenPosition.x:F0}, {hand.centerScreenPosition.y:F0})");
            }
        }

        GUILayout.Space(5);

        // UI交互状态
        if (uiInteraction.isInteracting)
        {
            GUILayout.Label("正在交互的按钮:", GUI.skin.box);
            if (uiInteraction.currentInteractingButtons.Count > 0)
            {
                foreach (var kvp in uiInteraction.currentInteractingButtons)
                {
                    string handName = kvp.Key == 0 ? "第一只手" : $"手{kvp.Key}";
                    GUILayout.Label($"  {handName}: {kvp.Value.name}");
                }
            }
            else if (uiInteraction.currentInteractingButton != null)
            {
                GUILayout.Label($"  第一只手: {uiInteraction.currentInteractingButton.name}");
            }
        }
        else
        {
            GUILayout.Label("未交互");
        }

        GUILayout.EndArea();
    }
}

