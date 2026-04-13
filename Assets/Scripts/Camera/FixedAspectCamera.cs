using UnityEngine;

/// <summary>
/// 固定相机构图比例（方案A）：
/// - 通过设置 Camera.rect 添加黑边，保证不同设备看到的内容一致
/// - 适合“打包后不要多看到模型”的需求
/// </summary>
[RequireComponent(typeof(Camera))]
public class FixedAspectCamera : MonoBehaviour
{
    [Header("目标比例（推荐 16:9）")]
    public int targetAspectWidth = 16;
    public int targetAspectHeight = 9;

    [Header("更新策略")]
    [Tooltip("分辨率/横竖屏变化时自动重算")]
    public bool updateContinuously = true;

    [Header("黑边背景（可选）")]
    [Tooltip("开启后会把相机清屏改为纯色，黑边更稳定")]
    public bool forceSolidColorBackground = true;
    public Color backgroundColor = Color.black;

    Camera cachedCamera;
    int lastWidth;
    int lastHeight;

    void Awake()
    {
        cachedCamera = GetComponent<Camera>();
    }

    void OnEnable()
    {
        ApplyAspect();
    }

    void Update()
    {
        if (!updateContinuously) return;

        if (Screen.width != lastWidth || Screen.height != lastHeight)
            ApplyAspect();
    }

    public void ApplyAspect()
    {
        if (cachedCamera == null)
            cachedCamera = GetComponent<Camera>();
        if (cachedCamera == null) return;
        if (targetAspectWidth <= 0 || targetAspectHeight <= 0) return;

        lastWidth = Screen.width;
        lastHeight = Screen.height;

        float targetAspect = (float)targetAspectWidth / targetAspectHeight;
        float screenAspect = (float)Screen.width / Screen.height;

        Rect rect = new Rect(0f, 0f, 1f, 1f);

        if (screenAspect > targetAspect)
        {
            // 屏幕更宽：左右黑边（pillarbox）
            float width = targetAspect / screenAspect;
            rect.x = (1f - width) * 0.5f;
            rect.y = 0f;
            rect.width = width;
            rect.height = 1f;
        }
        else if (screenAspect < targetAspect)
        {
            // 屏幕更高：上下黑边（letterbox）
            float height = screenAspect / targetAspect;
            rect.x = 0f;
            rect.y = (1f - height) * 0.5f;
            rect.width = 1f;
            rect.height = height;
        }

        cachedCamera.rect = rect;

        if (forceSolidColorBackground)
        {
            cachedCamera.clearFlags = CameraClearFlags.SolidColor;
            cachedCamera.backgroundColor = backgroundColor;
        }
    }
}

