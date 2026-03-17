using UnityEngine;
using System.Collections.Generic;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Unity.Sample;

public class HandLandmarkDataCollector : MonoBehaviour
{
[Header("MediaPipe Runner")]
public Mediapipe.Unity.Sample.HandLandmarkDetection.HandLandmarkerRunner handLandmarkerRunner;

[Header("Camera")]
public Camera targetCamera;

public float handDepth = 0.5f;

[Header("Landmark Data")]
public Vector3[] normalizedLandmarks = new Vector3[21];
public Vector3[] worldLandmarks = new Vector3[21];

public List<Vector3[]> allHandsLandmarks = new List<Vector3[]>();
public List<Vector3[]> allHandsWorldLandmarks = new List<Vector3[]>();

[Header("Status")]
public bool hasHand = false;
public int handCount = 0;
public string firstHandHandedness = "";

private HandLandmarkerResult latestResult;
private object resultLock = new object();

private Coroutine startRunnerRoutine;

void Start()
{
    Debug.Log("HandLandmarkDataCollector 初始化");

    if (handLandmarkerRunner == null)
    {
        handLandmarkerRunner = FindObjectOfType<Mediapipe.Unity.Sample.HandLandmarkDetection.HandLandmarkerRunner>();

        if (handLandmarkerRunner == null)
        {
            Debug.LogError("未找到 HandLandmarkerRunner");
            return;
        }
    }

    if (targetCamera == null)
    {
        targetCamera = Camera.main;

        if (targetCamera == null)
        {
            Debug.LogWarning("没有找到 MainCamera");
        }
    }

    handLandmarkerRunner.OnHandLandmarkResult += OnHandLandmarkResultReceived;
}

void OnDestroy()
{
    if (handLandmarkerRunner != null)
    {
        handLandmarkerRunner.OnHandLandmarkResult -= OnHandLandmarkResultReceived;
    }
}

public void StartCamera()
{
    Debug.Log("StartCamera 调用");

    if (handLandmarkerRunner == null)
    {
        Debug.LogError("Runner 不存在");
        return;
    }

    // 检测摄像头设备
    var devices = WebCamTexture.devices;

    Debug.Log("检测到摄像头数量: " + devices.Length);

    foreach (var cam in devices)
    {
        Debug.Log("Camera: " + cam.name);
    }

    if (devices.Length == 0)
    {
        Debug.LogError("没有检测到摄像头设备");
        return;
    }

    // 安全启动 Runner：不要在未 Play() 前强行 Stop()（会触发 StopCoroutine(null)）
    if (!handLandmarkerRunner.gameObject.activeInHierarchy)
    {
        handLandmarkerRunner.gameObject.SetActive(true);
    }

    if (startRunnerRoutine != null)
    {
        StopCoroutine(startRunnerRoutine);
        startRunnerRoutine = null;
    }
    startRunnerRoutine = StartCoroutine(StartRunnerNextFrame());
}

private System.Collections.IEnumerator StartRunnerNextFrame()
{
    // 等一帧让 Runner/GameObject 完成 OnEnable 等初始化
    yield return null;

    if (handLandmarkerRunner == null)
        yield break;

    // 如果场景中使用了 MediaPipe 官方 Bootstrap，这里假设它已经完成初始化；
    // 若未使用 Bootstrap，则依赖 AppSettings/样例工程自身的配置。
    Debug.Log("调用 HandLandmarkerRunner.Play()");
    handLandmarkerRunner.Play();
}

public void StopCamera()
{
    if (handLandmarkerRunner != null)
    {
        handLandmarkerRunner.Stop();
        handLandmarkerRunner.gameObject.SetActive(false);
    }

    if (startRunnerRoutine != null)
    {
        StopCoroutine(startRunnerRoutine);
        startRunnerRoutine = null;
    }

    hasHand = false;
    handCount = 0;

    allHandsLandmarks.Clear();
    allHandsWorldLandmarks.Clear();
}

private void OnHandLandmarkResultReceived(HandLandmarkerResult result)
{
    lock (resultLock)
    {
        latestResult = result;
    }
}

void Update()
{
    ProcessLatestResult();
}

void ProcessLatestResult()
{
    HandLandmarkerResult result;

    lock (resultLock)
    {
        result = latestResult;
    }

    if (result.handLandmarks == null || result.handLandmarks.Count == 0)
    {
        hasHand = false;
        handCount = 0;
        return;
    }

    hasHand = true;
    handCount = result.handLandmarks.Count;

    allHandsLandmarks.Clear();
    allHandsWorldLandmarks.Clear();

    for (int i = 0; i < result.handLandmarks.Count; i++)
    {
        var hand = result.handLandmarks[i];

        if (hand.landmarks.Count < 21) continue;

        Vector3[] normalized = new Vector3[21];
        Vector3[] world = new Vector3[21];

        for (int j = 0; j < 21; j++)
        {
            var lm = hand.landmarks[j];

            Vector3 pos = new Vector3(lm.x, lm.y, lm.z);
            normalized[j] = pos;
            world[j] = NormalizedToWorld(pos);
        }

        allHandsLandmarks.Add(normalized);
        allHandsWorldLandmarks.Add(world);
    }

    if (allHandsLandmarks.Count > 0)
    {
        System.Array.Copy(allHandsLandmarks[0], normalizedLandmarks, 21);
        System.Array.Copy(allHandsWorldLandmarks[0], worldLandmarks, 21);
    }

    if (result.handedness != null && result.handedness.Count > 0)
    {
        firstHandHandedness = result.handedness[0].categories[0].categoryName;
    }
}

Vector3 NormalizedToWorld(Vector3 normalizedPos)
{
    if (targetCamera == null) return Vector3.zero;

    Vector3 screenPos = new Vector3(
        normalizedPos.x * Screen.width,
        (1 - normalizedPos.y) * Screen.height,
        handDepth
    );

    return targetCamera.ScreenToWorldPoint(screenPos);
}

public Vector3 GetLandmark(int handIndex, int landmarkIndex)
{
    if (handIndex >= allHandsLandmarks.Count) return Vector3.zero;

    return allHandsLandmarks[handIndex][landmarkIndex];
}

public Vector3 GetFirstHandLandmark(int index)
{
    return normalizedLandmarks[index];
}

}
