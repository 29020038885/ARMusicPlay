using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 手势检测器 - 检测握拳手势（支持两只手）
/// </summary>
public class HandGestureDetector : MonoBehaviour
{
    [Header("数据源")]
    [Tooltip("手部特征点数据收集器")]
    public HandLandmarkDataCollector dataCollector;

    [Header("手势检测参数")]
    [Tooltip("指尖到手腕的最大距离阈值（归一化坐标），用于判断是否握拳")]
    [Range(0.05f, 0.2f)]
    public float fingerToWristDistanceThreshold = 0.12f;

    [Tooltip("手掌中心到手腕的距离（用于计算手掌中心位置）")]
    [Range(0.05f, 0.15f)]
    public float palmCenterOffset = 0.08f;

    [Header("状态输出")]
    [Tooltip("检测到握拳的手的数量")]
    public int clenchedHandCount = 0;

    [Tooltip("所有握拳手的信息列表")]
    public List<ClenchedHandInfo> clenchedHands = new List<ClenchedHandInfo>();

    // 指尖特征点索引：拇指(4), 食指(8), 中指(12), 无名指(16), 小指(20)
    private readonly int[] fingerTipIndices = { 4, 8, 12, 16, 20 };
    private const int wristIndex = 0;
    private const int middleFingerMCPIndex = 9; // 中指掌骨关节

    /// <summary>
    /// 握拳手的信息
    /// </summary>
    [System.Serializable]
    public class ClenchedHandInfo
    {
        public int handIndex;
        public string handedness; // "Left" or "Right"
        public Vector3 centerPosition; // 归一化坐标
        public Vector3 centerWorldPosition; // 世界坐标
        public Vector2 centerScreenPosition; // 屏幕坐标
    }

    void Start()
    {
        if (dataCollector == null)
        {
            dataCollector = FindObjectOfType<HandLandmarkDataCollector>();
            if (dataCollector == null)
            {
                Debug.LogWarning("HandGestureDetector: 未找到 HandLandmarkDataCollector");
            }
        }
    }

    void Update()
    {
        if (dataCollector == null || !dataCollector.hasHand)
        {
            clenchedHandCount = 0;
            clenchedHands.Clear();
            return;
        }

        DetectClenchedHands();
    }

    /// <summary>
    /// 检测所有握拳的手
    /// </summary>
    private void DetectClenchedHands()
    {
        clenchedHands.Clear();
        clenchedHandCount = 0;

        // 遍历所有检测到的手
        for (int handIndex = 0; handIndex < dataCollector.handCount; handIndex++)
        {
            if (IsHandClenched(handIndex))
            {
                ClenchedHandInfo handInfo = new ClenchedHandInfo
                {
                    handIndex = handIndex,
                    handedness = GetHandedness(handIndex)
                };

                CalculateHandCenter(handIndex, ref handInfo);
                clenchedHands.Add(handInfo);
                clenchedHandCount++;
            }
        }
    }

    /// <summary>
    /// 检测指定手是否握拳
    /// </summary>
    private bool IsHandClenched(int handIndex)
    {
        Vector3 wrist = dataCollector.GetLandmark(handIndex, wristIndex);
        int clenchedFingerCount = 0;

        // 检查每个指尖是否接近手腕（握拳时指尖会靠近手腕）
        for (int i = 0; i < fingerTipIndices.Length; i++)
        {
            Vector3 fingerTip = dataCollector.GetLandmark(handIndex, fingerTipIndices[i]);
            
            // 计算指尖到手腕的2D距离（忽略Z深度）
            float distance2D = Vector2.Distance(
                new Vector2(wrist.x, wrist.y),
                new Vector2(fingerTip.x, fingerTip.y)
            );

            // 如果指尖距离手腕很近，说明该手指是弯曲的（握拳状态）
            if (distance2D <= fingerToWristDistanceThreshold)
            {
                clenchedFingerCount++;
            }
        }

        // 如果至少4个手指（80%）都接近手腕，认为是握拳
        // 拇指可能比较特殊，所以要求至少4个手指
        return clenchedFingerCount >= 4;
    }

    /// <summary>
    /// 计算握拳手的中心位置
    /// </summary>
    private void CalculateHandCenter(int handIndex, ref ClenchedHandInfo handInfo)
    {
        Vector3 wrist = dataCollector.GetLandmark(handIndex, wristIndex);
        Vector3 middleFingerMCP = dataCollector.GetLandmark(handIndex, middleFingerMCPIndex);

        // 手掌中心大约在手腕和中指掌骨关节之间
        Vector3 palmCenter = Vector3.Lerp(wrist, middleFingerMCP, 0.5f);
        handInfo.centerPosition = palmCenter;

        // 转换为世界坐标
        if (dataCollector.targetCamera != null)
        {
            Vector3 screenPos = new Vector3(
                palmCenter.x * Screen.width,
                (1 - palmCenter.y) * Screen.height,
                dataCollector.handDepth
            );
            handInfo.centerWorldPosition = dataCollector.targetCamera.ScreenToWorldPoint(screenPos);
        }

        // 转换为屏幕坐标
        handInfo.centerScreenPosition = new Vector2(
            palmCenter.x * Screen.width,
            (1 - palmCenter.y) * Screen.height
        );
    }

    /// <summary>
    /// 获取指定手的左右手信息
    /// </summary>
    private string GetHandedness(int handIndex)
    {
        // 从 dataCollector 获取 handedness 信息
        // 这里简化处理，实际应该从 HandLandmarkerResult 中获取
        if (handIndex == 0)
        {
            return dataCollector.firstHandHandedness;
        }
        // 对于第二只手，可以根据位置判断（左手通常在屏幕左侧）
        // 或者从 HandLandmarkerResult 中获取完整信息
        return "Unknown";
    }

    /// <summary>
    /// 获取第一只握拳手的中心位置（屏幕坐标）- 兼容旧接口
    /// </summary>
    public Vector2 GetFingersCenterScreen()
    {
        if (clenchedHands.Count > 0)
        {
            return clenchedHands[0].centerScreenPosition;
        }
        return Vector2.zero;
    }

    /// <summary>
    /// 获取第一只握拳手的中心位置（归一化坐标）
    /// </summary>
    public Vector3 GetFingersCenterNormalized()
    {
        if (clenchedHands.Count > 0)
        {
            return clenchedHands[0].centerPosition;
        }
        return Vector3.zero;
    }

    /// <summary>
    /// 获取第一只握拳手的中心位置（世界坐标）
    /// </summary>
    public Vector3 GetFingersCenterWorld()
    {
        if (clenchedHands.Count > 0)
        {
            return clenchedHands[0].centerWorldPosition;
        }
        return Vector3.zero;
    }

    /// <summary>
    /// 检查是否有握拳手势（兼容旧接口）
    /// </summary>
    public bool isFingersTogether
    {
        get { return clenchedHandCount > 0; }
    }

    /// <summary>
    /// 获取指定手的中心位置（屏幕坐标）
    /// </summary>
    public Vector2 GetHandCenterScreen(int handIndex)
    {
        foreach (var hand in clenchedHands)
        {
            if (hand.handIndex == handIndex)
            {
                return hand.centerScreenPosition;
            }
        }
        return Vector2.zero;
    }
}

