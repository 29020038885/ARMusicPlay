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
    [Tooltip("拇指尖与食指尖距离小于此值视为捏合（略小更严、更准）")]
    [Range(0.02f, 0.12f)]
    public float pinchDistanceThreshold = 0.055f;

    [Header("捏合稳定性（提高识别精准度）")]
    [Tooltip("归一化平面下两指尖距离需连续低于阈值的帧数，才输出为捏合；减轻抖动误检")]
    [Range(0, 12)]
    public int pinchStableFramesRequired = 3;

    [Tooltip("捏合点屏幕坐标平滑：0=不平滑；越大越跟手（建议 0.25~0.5）")]
    [Range(0f, 1f)]
    public float pinchScreenFollow = 0.4f;

    [Header("状态输出")]
    [Tooltip("捏合的手列表（用于 UI 点击）")]
    public List<PinchHandInfo> pinchHands = new List<PinchHandInfo>();

    private const int thumbTipIndex = 4;
    private const int indexTipIndex = 8;

    private const int MaxTrackedHands = 2;
    private readonly int[] _pinchStableCount = new int[MaxTrackedHands];
    private readonly Vector2[] _pinchSmoothedScreen = new Vector2[MaxTrackedHands];
    private readonly bool[] _pinchSmoothInitialized = new bool[MaxTrackedHands];

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
        if (dataCollector == null || !dataCollector.hasHand)
        {
            ResetPinchTracking();
            return;
        }

        int count = Mathf.Min(dataCollector.handCount, MaxTrackedHands);
        for (int hi = count; hi < MaxTrackedHands; hi++)
            ResetHandPinchTracking(hi);

        for (int handIndex = 0; handIndex < dataCollector.handCount; handIndex++)
        {
            if (handIndex >= MaxTrackedHands)
                break;

            if (!IsHandPinchingRaw(handIndex))
            {
                ResetHandPinchTracking(handIndex);
                continue;
            }

            _pinchStableCount[handIndex]++;
            if (_pinchStableCount[handIndex] < Mathf.Max(1, pinchStableFramesRequired))
                continue;

            Vector3 thumbTip = dataCollector.GetLandmark(handIndex, thumbTipIndex);
            Vector3 indexTip = dataCollector.GetLandmark(handIndex, indexTipIndex);
            Vector3 pinchCenter = (thumbTip + indexTip) * 0.5f;

            var info = new PinchHandInfo
            {
                handIndex = handIndex,
                handedness = GetHandedness(handIndex)
            };
            Vector2 rawScreen = new Vector2(pinchCenter.x * Screen.width, (1f - pinchCenter.y) * Screen.height);
            info.pinchScreenPosition = SmoothPinchScreen(handIndex, rawScreen);
            if (dataCollector.targetCamera != null)
            {
                Vector3 screenPos = new Vector3(info.pinchScreenPosition.x, info.pinchScreenPosition.y, dataCollector.handDepth);
                info.pinchWorldPosition = dataCollector.targetCamera.ScreenToWorldPoint(screenPos);
            }
            pinchHands.Add(info);
        }
    }

    private void ResetPinchTracking()
    {
        for (int i = 0; i < MaxTrackedHands; i++)
            ResetHandPinchTracking(i);
    }

    private void ResetHandPinchTracking(int handIndex)
    {
        if (handIndex < 0 || handIndex >= MaxTrackedHands) return;
        _pinchStableCount[handIndex] = 0;
        _pinchSmoothInitialized[handIndex] = false;
    }

    private Vector2 SmoothPinchScreen(int handIndex, Vector2 rawScreen)
    {
        if (pinchScreenFollow <= 0.001f)
            return rawScreen;
        if (!_pinchSmoothInitialized[handIndex])
        {
            _pinchSmoothedScreen[handIndex] = rawScreen;
            _pinchSmoothInitialized[handIndex] = true;
            return rawScreen;
        }
        float t = Mathf.Clamp01(pinchScreenFollow);
        _pinchSmoothedScreen[handIndex] = Vector2.Lerp(_pinchSmoothedScreen[handIndex], rawScreen, t);
        return _pinchSmoothedScreen[handIndex];
    }

    private bool IsHandPinchingRaw(int handIndex)
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
