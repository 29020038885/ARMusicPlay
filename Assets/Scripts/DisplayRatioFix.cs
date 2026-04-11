using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 解决 Android 打包后显示比例异常：乐器/场景过大、UI 过小。
/// 挂在弹奏场景的任意物体上，运行时自动按当前屏幕比例修正 Canvas 与相机。
/// </summary>
public class DisplayRatioFix : MonoBehaviour
{
    [Header("参考分辨率（与编辑器中预览一致的分辨率）")]
    [Tooltip("设计时的参考宽")]
    public int referenceWidth = 1080;

    [Tooltip("设计时的参考高")]
    public int referenceHeight = 1920;

    [Header("修正对象")]
    [Tooltip("是否自动修正场景中的 Canvas（Scale With Screen Size）")]
    public bool fixCanvas = true;

    [Tooltip("是否按比例修正相机视野（避免乐器在竖屏上被“拉近”或过大）")]
    public bool fixCamera = true;

    [Header("Canvas 缩放模式")]
    [Tooltip("0=按宽匹配 1=按高匹配 0.5=折中，竖屏建议 0~0.5")]
    [Range(0f, 1f)]
    public float matchWidthOrHeight = 0.5f;

    [Header("安全区适配")]
    [Tooltip("是否自动给根 Canvas 添加 SafeAreaFitter，适配刘海/挖孔屏")]
    public bool autoApplySafeArea = true;

    [Header("相机（仅正交相机）")]
    [Tooltip("设计时的正交 size（若为 0 则用当前相机值作为参考）")]
    public float referenceOrthoSize = 0f;

    private float designAspect;

    void Awake()
    {
        designAspect = (float)referenceWidth / referenceHeight;

        if (fixCanvas)
            ApplyCanvasScaler();

        if (fixCamera)
            ApplyCameraScale();
    }

    void ApplyCanvasScaler()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(referenceWidth, referenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = matchWidthOrHeight;
            scaler.referencePixelsPerUnit = 100f;

            if (autoApplySafeArea && canvas.isRootCanvas)
            {
                // 根 Canvas 自动加安全区适配，避免异形屏遮挡 UI。
                var safe = canvas.GetComponent<SafeAreaFitter>();
                if (safe == null)
                    safe = canvas.gameObject.AddComponent<SafeAreaFitter>();
                safe.applyOnStart = true;
                safe.onlyForMobile = true;
            }
        }
    }

    void ApplyCameraScale()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam == null) return;

        float currentAspect = (float)Screen.width / Screen.height;
        float aspectRatio = currentAspect / designAspect;

        if (cam.orthographic)
        {
            if (referenceOrthoSize <= 0f)
                referenceOrthoSize = cam.orthographicSize;

            // 正交相机按“设计高度一致”策略，超宽屏时扩展可视范围，避免主体被放大。
            cam.orthographicSize = aspectRatio > 1f ? referenceOrthoSize * aspectRatio : referenceOrthoSize;
            return;
        }

        // 透视相机不直接改 FOV，使用 viewport letterbox/pillarbox 保持构图一致。
        // 这样不同长宽比设备看起来与编辑器构图更一致。
        Rect rect = cam.rect;
        if (aspectRatio < 1f)
        {
            // 设备更“窄”，上下保持，左右加黑边（pillarbox）
            float scale = aspectRatio;
            rect.width = scale;
            rect.height = 1f;
            rect.x = (1f - scale) * 0.5f;
            rect.y = 0f;
        }
        else
        {
            // 设备更“宽”，左右保持，上下加黑边（letterbox）
            float scale = 1f / aspectRatio;
            rect.width = 1f;
            rect.height = scale;
            rect.x = 0f;
            rect.y = (1f - scale) * 0.5f;
        }
        cam.rect = rect;
    }
}
