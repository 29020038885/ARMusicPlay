using UnityEngine;

/// <summary>
/// 自动将当前 RectTransform 约束到安全区（刘海/挖孔/圆角屏）。
/// 建议挂在根 Canvas（或其第一层全屏容器）上。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    [Tooltip("启动时自动应用一次")]
    public bool applyOnStart = true;

    [Tooltip("仅在移动平台生效")]
    public bool onlyForMobile = true;

    [Tooltip("持续检测安全区变化（旋转/分屏等）")]
    public bool checkEveryFrame = true;

    private RectTransform _rectTransform;
    private Rect _lastSafeArea = new Rect(0, 0, 0, 0);
    private Vector2Int _lastScreenSize = Vector2Int.zero;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        if (applyOnStart)
            ApplySafeArea();
    }

    private void Update()
    {
        if (!checkEveryFrame) return;
        if (ShouldSkipForPlatform()) return;

        Rect current = Screen.safeArea;
        Vector2Int size = new Vector2Int(Screen.width, Screen.height);
        if (current != _lastSafeArea || size != _lastScreenSize)
            ApplySafeArea();
    }

    public void ApplySafeArea()
    {
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform == null) return;
        if (ShouldSkipForPlatform()) return;

        Rect safeArea = Screen.safeArea;
        float width = Screen.width <= 0 ? 1f : Screen.width;
        float height = Screen.height <= 0 ? 1f : Screen.height;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= width;
        anchorMin.y /= height;
        anchorMax.x /= width;
        anchorMax.y /= height;

        _rectTransform.anchorMin = anchorMin;
        _rectTransform.anchorMax = anchorMax;
        _rectTransform.offsetMin = Vector2.zero;
        _rectTransform.offsetMax = Vector2.zero;

        _lastSafeArea = safeArea;
        _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }

    private bool ShouldSkipForPlatform()
    {
        if (!onlyForMobile) return false;
#if UNITY_ANDROID || UNITY_IOS
        return false;
#else
        return true;
#endif
    }
}
