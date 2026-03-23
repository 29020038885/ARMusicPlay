using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 根据手部关键点识别特定手型，在稳定保持若干帧后打开/切换指定面板。
/// 与「捏合点 UI」分离：可勾选「排除捏合」避免与 HandGestureDetector 冲突。
/// 默认手势为「金属礼」（食指+小指伸直、中指无名指弯曲），比「比耶」更易与捏合、指向区分。
/// </summary>
public class HandPanelGestureOpener : MonoBehaviour
{
    public enum GesturePreset
    {
        [Tooltip("食指+小指伸直，中指无名指弯曲（推荐，误触相对较少）")]
        RockHorns,
        [Tooltip("食指+中指伸直，无名指小指弯曲（经典比耶）")]
        PeaceVictory,
        [Tooltip("拇指伸直，其余四指明显弯曲")]
        ThumbsUp
    }

    [Header("引用")]
    [Tooltip("手部关键点数据源")]
    public HandLandmarkDataCollector dataCollector;

    [Tooltip("要控制显示的面板（拖你自己的 UI 根物体即可）")]
    public GameObject targetPanel;

    [Header("手势类型")]
    public GesturePreset gesturePreset = GesturePreset.RockHorns;

    [Header("与捏合共存")]
    [Tooltip("为 true 时：拇指与食指尖过近（捏合）则不触发本手势，减少与 UI 捏合冲突")]
    public bool rejectWhilePinching = true;

    [Tooltip("拇指尖(4)与食指尖(8)距离小于此值视为捏合（与 HandGestureDetector 量级一致，归一化坐标）")]
    [Range(0.02f, 0.15f)]
    public float pinchRejectDistance = 0.08f;

    [Header("手指形态阈值（归一化平面 xy，相对手腕）")]
    [Tooltip("指尖到手腕距离 > 指关节到手腕距离 × 该值 → 视为「伸直」")]
    [Range(1f, 1.25f)]
    public float extendedRatio = 1.06f;

    [Tooltip("指尖到手腕距离 < 指关节到手腕距离 × 该值 → 视为「弯曲」")]
    [Range(0.65f, 0.98f)]
    public float curledRatio = 0.88f;

    [Header("稳定性（防闪烁）")]
    [Tooltip("手型需连续满足多少帧才触发一次")]
    [Range(3, 45)]
    public int stableFramesRequired = 10;

    [Tooltip("触发后多少秒内不再响应（秒）")]
    [Range(0.3f, 5f)]
    public float cooldownSeconds = 1.2f;

    [Header("行为")]
    [Tooltip("为 true：每次识别到一次完整手势则切换 显示/隐藏；为 false：仅设为显示")]
    public bool togglePanel = true;

    [Tooltip("游戏开始时是否先隐藏面板（仅当 targetPanel 非空时生效）")]
    public bool hidePanelOnAwake = true;

    [Header("事件（可选）")]
    public UnityEvent onPanelShown;
    public UnityEvent onPanelHidden;

    // MediaPipe 手部 21 点索引
    private const int Wrist = 0;
    private const int ThumbTip = 4;
    private const int ThumbIp = 3;
    private const int IndexPip = 6;
    private const int IndexTip = 8;
    private const int MiddlePip = 10;
    private const int MiddleTip = 12;
    private const int RingPip = 14;
    private const int RingTip = 16;
    private const int PinkyPip = 18;
    private const int PinkyTip = 20;

    private int _stableFrames;
    private float _cooldownUntil;

    private void Awake()
    {
        if (dataCollector == null)
            dataCollector = FindObjectOfType<HandLandmarkDataCollector>();

        if (hidePanelOnAwake && targetPanel != null)
            targetPanel.SetActive(false);
    }

    private void Update()
    {
        if (dataCollector == null || targetPanel == null)
            return;

        if (Time.unscaledTime < _cooldownUntil)
            return;

        if (!dataCollector.hasHand || dataCollector.handCount <= 0)
        {
            _stableFrames = 0;
            return;
        }

        // 只用手型最稳定的一只手：第一只
        int hand = 0;
        if (rejectWhilePinching && IsPinching(hand))
        {
            _stableFrames = 0;
            return;
        }

        bool match = EvaluatePreset(hand);
        if (match)
        {
            _stableFrames++;
            if (_stableFrames >= stableFramesRequired)
            {
                _stableFrames = 0;
                _cooldownUntil = Time.unscaledTime + cooldownSeconds;
                ApplyPanelAction();
            }
        }
        else
            _stableFrames = 0;
    }

    private void ApplyPanelAction()
    {
        if (togglePanel)
        {
            bool next = !targetPanel.activeSelf;
            targetPanel.SetActive(next);
            if (next) onPanelShown?.Invoke();
            else onPanelHidden?.Invoke();
        }
        else
        {
            if (!targetPanel.activeSelf)
            {
                targetPanel.SetActive(true);
                onPanelShown?.Invoke();
            }
        }
    }

    private bool IsPinching(int handIndex)
    {
        Vector3 t = dataCollector.GetLandmark(handIndex, ThumbTip);
        Vector3 i = dataCollector.GetLandmark(handIndex, IndexTip);
        float d = Vector2.Distance(new Vector2(t.x, t.y), new Vector2(i.x, i.y));
        return d < pinchRejectDistance;
    }

    private bool EvaluatePreset(int handIndex)
    {
        switch (gesturePreset)
        {
            case GesturePreset.RockHorns:
                return IsExtended(handIndex, IndexPip, IndexTip)
                    && IsExtended(handIndex, PinkyPip, PinkyTip)
                    && IsCurled(handIndex, MiddlePip, MiddleTip)
                    && IsCurled(handIndex, RingPip, RingTip);
            case GesturePreset.PeaceVictory:
                return IsExtended(handIndex, IndexPip, IndexTip)
                    && IsExtended(handIndex, MiddlePip, MiddleTip)
                    && IsCurled(handIndex, RingPip, RingTip)
                    && IsCurled(handIndex, PinkyPip, PinkyTip);
            case GesturePreset.ThumbsUp:
                return IsThumbExtended(handIndex)
                    && IsCurled(handIndex, IndexPip, IndexTip)
                    && IsCurled(handIndex, MiddlePip, MiddleTip)
                    && IsCurled(handIndex, RingPip, RingTip)
                    && IsCurled(handIndex, PinkyPip, PinkyTip);
            default:
                return false;
        }
    }

    private Vector2 WristXY(int handIndex)
    {
        Vector3 w = dataCollector.GetLandmark(handIndex, Wrist);
        return new Vector2(w.x, w.y);
    }

    private float DistWristTo(int handIndex, int landmarkIndex)
    {
        Vector3 p = dataCollector.GetLandmark(handIndex, landmarkIndex);
        return Vector2.Distance(WristXY(handIndex), new Vector2(p.x, p.y));
    }

    /// <summary>PIP→TIP：指尖离手腕比关节离手腕更远 → 伸直</summary>
    private bool IsExtended(int handIndex, int pipIndex, int tipIndex)
    {
        float dTip = DistWristTo(handIndex, tipIndex);
        float dPip = DistWristTo(handIndex, pipIndex);
        if (dPip < 1e-4f) return false;
        return dTip >= dPip * extendedRatio;
    }

    private bool IsCurled(int handIndex, int pipIndex, int tipIndex)
    {
        float dTip = DistWristTo(handIndex, tipIndex);
        float dPip = DistWristTo(handIndex, pipIndex);
        if (dPip < 1e-4f) return dTip < 0.02f;
        return dTip <= dPip * curledRatio;
    }

    private bool IsThumbExtended(int handIndex)
    {
        float dTip = DistWristTo(handIndex, ThumbTip);
        float dIp = DistWristTo(handIndex, ThumbIp);
        if (dIp < 1e-4f) return false;
        return dTip >= dIp * extendedRatio;
    }

    /// <summary>供调试或 UI 显示：当前帧第一只手是否满足所选手势（未计稳定帧）</summary>
    public bool IsCurrentGestureMatchedRaw()
    {
        if (dataCollector == null || !dataCollector.hasHand || dataCollector.handCount <= 0)
            return false;
        if (rejectWhilePinching && IsPinching(0)) return false;
        return EvaluatePreset(0);
    }
}
