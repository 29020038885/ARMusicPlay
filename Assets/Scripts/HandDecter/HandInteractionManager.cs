using UnityEngine;
using System.Collections;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

/// <summary>
/// 手部交互管理器 - 负责权限申请 + 启动摄像头 + 手势系统
/// </summary>
public class HandInteractionManager : MonoBehaviour
{
[Header("组件引用")]
public HandLandmarkDataCollector dataCollector;
public HandGestureDetector gestureDetector;
public HandUIInteraction uiInteraction;

[Header("自动设置")]
public bool autoSetup = true;

[Header("摄像头控制")]
public bool startCameraOnStart = true;

void Start()
{
    if (autoSetup)
        SetupComponents();

    if (startCameraOnStart && dataCollector != null)
    {
        StartCoroutine(InitCameraRoutine());
    }
}

void SetupComponents()
{
    if (dataCollector == null)
        dataCollector = FindObjectOfType<HandLandmarkDataCollector>();

    if (gestureDetector == null)
        gestureDetector = FindObjectOfType<HandGestureDetector>();

    if (uiInteraction == null)
        uiInteraction = FindObjectOfType<HandUIInteraction>();

    if (gestureDetector != null && gestureDetector.dataCollector == null)
        gestureDetector.dataCollector = dataCollector;

    if (uiInteraction != null && uiInteraction.gestureDetector == null)
        uiInteraction.gestureDetector = gestureDetector;

    // 面板手势（比耶/金属礼等）：未手动拖 DataCollector 时自动补上
    var panelGesture = FindObjectOfType<HandPanelGestureOpener>();
    if (panelGesture != null && panelGesture.dataCollector == null)
        panelGesture.dataCollector = dataCollector;
}

IEnumerator InitCameraRoutine()
{
    Debug.Log("开始初始化摄像头...");

#if UNITY_ANDROID
// 1 申请权限
if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
{
Debug.Log("请求 Camera 权限...");
Permission.RequestUserPermission(Permission.Camera);

        // 等待用户操作
        yield return new WaitForSeconds(1.5f);
    }


#endif

    // 2 再检查 Unity Webcam 权限
    if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
    {
        Debug.Log("Unity 请求 WebCam 权限...");
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
    }

    // 3 检测摄像头设备
    var devices = WebCamTexture.devices;

    Debug.Log("检测到摄像头数量: " + devices.Length);

    foreach (var cam in devices)
    {
        Debug.Log("Camera Device: " + cam.name);
    }

    if (devices.Length == 0)
    {
        Debug.LogError("没有检测到手机摄像头！");
        yield break;
    }

    // 4 启动 MediaPipe
    Debug.Log("启动手势摄像头...");

    dataCollector.StartCamera();
}

public void EnableHandCamera()
{
    if (dataCollector != null)
        dataCollector.StartCamera();
}

public void DisableHandCamera()
{
    if (dataCollector != null)
        dataCollector.StopCamera();
}

}
