using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 手势检测器 - 仅检测捏合（拇指+食指尖靠拢），用于 UI 点击、翻页等。
/// </summary>
public class HandGestureDetector : MonoBehaviour
{
    [Header("数据源")]
    [Tooltip("手部特征点数据收集器")]
    public HandLandmarkDataCollector dataCollector;

    [Header("捏合参数")]
    [Tooltip("拇指尖与食指尖距离小于此值视为捏合")]
    [Range(0.02f, 0.12f)]
    public float pinchDistanceThreshold = 0.07f;

    [Header("状态输出")]
    [Tooltip("捏合的手列表（用于 UI 点击）")]
    public List<PinchHandInfo> pinchHands = new List<PinchHandInfo>();

    private const int thumbTipIndex = 4;
    private const int indexTipIndex = 8;

    [System.Serializable]
    public class PinchHandInfo
    {
        public int handIndex;
        public string handedness;
        public Vector2 pinchScreenPosition;
        public Vector3 pinchWorldPosition;
    }

    void Start()
    {
        if (dataCollector == null)
            dataCollector = FindObjectOfType<HandLandmarkDataCollector>();
        if (dataCollector == null)
            Debug.LogWarning("HandGestureDetector: 未找到 HandLandmarkDataCollector");
    }

    void Update()
    {
        pinchHands.Clear();
        if (dataCollector == null || !dataCollector.hasHand) return;

        for (int handIndex = 0; handIndex < dataCollector.handCount; handIndex++)
        {
            if (!IsHandPinching(handIndex)) continue;

            Vector3 thumbTip = dataCollector.GetLandmark(handIndex, thumbTipIndex);
            Vector3 indexTip = dataCollector.GetLandmark(handIndex, indexTipIndex);
            Vector3 pinchCenter = (thumbTip + indexTip) * 0.5f;

            var info = new PinchHandInfo
            {
                handIndex = handIndex,
                handedness = GetHandedness(handIndex)
            };
            info.pinchScreenPosition = new Vector2(pinchCenter.x * Screen.width, (1f - pinchCenter.y) * Screen.height);
            if (dataCollector.targetCamera != null)
            {
                Vector3 screenPos = new Vector3(info.pinchScreenPosition.x, info.pinchScreenPosition.y, dataCollector.handDepth);
                info.pinchWorldPosition = dataCollector.targetCamera.ScreenToWorldPoint(screenPos);
            }
            pinchHands.Add(info);
        }
    }

    private bool IsHandPinching(int handIndex)
    {
        Vector3 thumbTip = dataCollector.GetLandmark(handIndex, thumbTipIndex);
        Vector3 indexTip = dataCollector.GetLandmark(handIndex, indexTipIndex);
        float dist = Vector2.Distance(new Vector2(thumbTip.x, thumbTip.y), new Vector2(indexTip.x, indexTip.y));
        return dist <= pinchDistanceThreshold;
    }

    private string GetHandedness(int handIndex)
    {
        if (handIndex == 0)
            return dataCollector.firstHandHandedness ?? "";
        return "Unknown";
    }

    public int pinchCount => pinchHands != null ? pinchHands.Count : 0;

    public Vector2 GetFirstPinchScreen()
    {
        if (pinchHands != null && pinchHands.Count > 0)
            return pinchHands[0].pinchScreenPosition;
        return Vector2.zero;
    }
}
