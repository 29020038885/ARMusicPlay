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
        }
    }

    void ApplyCameraScale()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam == null || !cam.orthographic) return;

        float currentAspect = (float)Screen.width / Screen.height;
        if (referenceOrthoSize <= 0f)
            referenceOrthoSize = cam.orthographicSize;

        // 设计时是按 reference 宽高比；当前屏幕宽高比不同时，用“按高度一致”来保持乐器在屏幕上的相对大小
        // 即 orthoSize 以“高度”为基准，宽度方向多出来的用更多视野填满，避免乐器被拉大
        float aspectRatio = currentAspect / designAspect;
        if (aspectRatio > 1f)
            cam.orthographicSize = referenceOrthoSize * aspectRatio;
        else
            cam.orthographicSize = referenceOrthoSize;
    }
}
