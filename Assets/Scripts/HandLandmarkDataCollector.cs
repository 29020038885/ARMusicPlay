using UnityEngine;
using System.Collections.Generic;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Tasks.Components.Containers;

/// <summary>
/// 用于收集 MediaPipe 手部特征点归一化坐标的脚本
/// 将检测到的手部特征点坐标填充到数组中
/// </summary>
public class HandLandmarkDataCollector : MonoBehaviour
{
    [Header("MediaPipe Runner 引用")]
    [Tooltip("拖拽场景中的 HandLandmarkerRunner 组件到这里")]
    public Mediapipe.Unity.Sample.HandLandmarkDetection.HandLandmarkerRunner handLandmarkerRunner;

    [Header("数据存储")]
    [Tooltip("存储第一只手的21个特征点的归一化坐标")]
    public Vector3[] normalizedLandmarks = new Vector3[21];

    [Tooltip("存储所有检测到的手的特征点（支持多手）")]
    public List<Vector3[]> allHandsLandmarks = new List<Vector3[]>();

    [Header("状态信息")]
    [Tooltip("是否检测到手部")]
    public bool hasHand = false;

    [Tooltip("当前检测到的手的数量")]
    public int handCount = 0;

    [Tooltip("第一只手的左右手信息（Left/Right）")]
    public string firstHandHandedness = "";

    // 用于存储最新的结果
    private HandLandmarkerResult latestResult;
    private object resultLock = new object();

    void Start()
    {
        // 如果没有手动指定 runner，尝试自动查找
        if (handLandmarkerRunner == null)
        {
            handLandmarkerRunner = FindObjectOfType<Mediapipe.Unity.Sample.HandLandmarkDetection.HandLandmarkerRunner>();
            if (handLandmarkerRunner == null)
            {
                Debug.LogWarning("HandLandmarkDataCollector: 未找到 HandLandmarkerRunner，请手动指定或在场景中添加 HandLandmarkerRunner 组件");
            }
        }

        // 订阅 HandLandmarkerRunner 的事件
        if (handLandmarkerRunner != null)
        {
            handLandmarkerRunner.OnHandLandmarkResult += OnHandLandmarkResultReceived;
            Debug.Log("HandLandmarkDataCollector: 已订阅 HandLandmarkerRunner 事件");
        }
    }

    void OnDestroy()
    {
        // 取消订阅事件
        if (handLandmarkerRunner != null)
        {
            handLandmarkerRunner.OnHandLandmarkResult -= OnHandLandmarkResultReceived;
        }
    }

    /// <summary>
    /// 当收到手部特征点检测结果时的回调
    /// </summary>
    private void OnHandLandmarkResultReceived(HandLandmarkerResult result)
    {
        SetLatestResult(result);
    }

    void Update()
    {
        // 在 Update 中处理结果（适用于 IMAGE 和 VIDEO 模式）
        // LIVE_STREAM 模式通过事件回调处理
        ProcessLatestResult();
    }

    /// <summary>
    /// 处理最新的检测结果
    /// </summary>
    private void ProcessLatestResult()
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
            firstHandHandedness = "";
            return;
        }

        hasHand = true;
        handCount = result.handLandmarks.Count;

        // 更新所有手的数据
        allHandsLandmarks.Clear();
        for (int i = 0; i < result.handLandmarks.Count; i++)
        {
            var handLandmarks = result.handLandmarks[i];
            if (handLandmarks.landmarks != null && handLandmarks.landmarks.Count >= 21)
            {
                Vector3[] landmarks = new Vector3[21];
                for (int j = 0; j < 21 && j < handLandmarks.landmarks.Count; j++)
                {
                    var landmark = handLandmarks.landmarks[j];
                    landmarks[j] = new Vector3(landmark.x, landmark.y, landmark.z);
                }
                allHandsLandmarks.Add(landmarks);
            }
        }

        // 填充第一只手的数据到 normalizedLandmarks 数组
        if (allHandsLandmarks.Count > 0)
        {
            System.Array.Copy(allHandsLandmarks[0], normalizedLandmarks, Mathf.Min(allHandsLandmarks[0].Length, normalizedLandmarks.Length));
            
            // 获取第一只手的左右手信息
            if (result.handedness != null && result.handedness.Count > 0)
            {
                var firstHandHandednessData = result.handedness[0];
                if (firstHandHandednessData.categories != null && firstHandHandednessData.categories.Count > 0)
                {
                    firstHandHandedness = firstHandHandednessData.categories[0].categoryName;
                }
            }
        }
    }

    /// <summary>
    /// 设置最新的检测结果（由 HandLandmarkerRunner 调用）
    /// </summary>
    public void SetLatestResult(HandLandmarkerResult result)
    {
        lock (resultLock)
        {
            latestResult = result;
        }
    }

    /// <summary>
    /// 获取指定手的指定特征点的归一化坐标
    /// </summary>
    /// <param name="handIndex">手的索引（0为第一只手）</param>
    /// <param name="landmarkIndex">特征点索引（0-20）</param>
    /// <returns>归一化坐标 Vector3(x, y, z)，如果不存在则返回 Vector3.zero</returns>
    public Vector3 GetLandmark(int handIndex, int landmarkIndex)
    {
        if (allHandsLandmarks == null || handIndex < 0 || handIndex >= allHandsLandmarks.Count)
        {
            return Vector3.zero;
        }

        var hand = allHandsLandmarks[handIndex];
        if (landmarkIndex < 0 || landmarkIndex >= hand.Length)
        {
            return Vector3.zero;
        }

        return hand[landmarkIndex];
    }

    /// <summary>
    /// 获取第一只手的指定特征点的归一化坐标
    /// </summary>
    /// <param name="landmarkIndex">特征点索引（0-20）</param>
    /// <returns>归一化坐标 Vector3(x, y, z)</returns>
    public Vector3 GetFirstHandLandmark(int landmarkIndex)
    {
        if (landmarkIndex < 0 || landmarkIndex >= normalizedLandmarks.Length)
        {
            return Vector3.zero;
        }

        return normalizedLandmarks[landmarkIndex];
    }

    /// <summary>
    /// 在 Inspector 中显示调试信息
    /// </summary>
    void OnGUI()
    {
        if (!hasHand) return;

        GUILayout.BeginArea(new UnityEngine.Rect(10, 10, 300, 200));
        GUILayout.Label($"检测到的手数量: {handCount}");
        GUILayout.Label($"第一只手: {firstHandHandedness}");
        
        if (normalizedLandmarks != null && normalizedLandmarks.Length > 0)
        {
            GUILayout.Label($"特征点 0 (手腕): ({normalizedLandmarks[0].x:F3}, {normalizedLandmarks[0].y:F3}, {normalizedLandmarks[0].z:F3})");
            GUILayout.Label($"特征点 8 (食指指尖): ({normalizedLandmarks[8].x:F3}, {normalizedLandmarks[8].y:F3}, {normalizedLandmarks[8].z:F3})");
        }
        GUILayout.EndArea();
    }
}

