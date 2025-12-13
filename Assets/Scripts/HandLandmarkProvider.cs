using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 手部特征点提供者 - 从 MediaPipe 获取归一化坐标并转换为世界坐标
/// </summary>
public class HandLandmarkProvider : MonoBehaviour
{
    [Header("组件引用")]
    [Tooltip("手部特征点数据收集器")]
    public HandLandmarkDataCollector dataCollector;

    [Tooltip("AR 摄像机（用于坐标转换）")]
    public Camera arCamera;

    [Header("输出数据")]
    [Tooltip("21 个世界坐标点")]
    public Vector3[] worldLandmarks = new Vector3[21];

    [Tooltip("21 个归一化坐标点（0-1范围）")]
    public Vector3[] normalizedLandmarks = new Vector3[21];

    [Header("参数设置")]
    [Tooltip("手部距离摄像机的深度（米）")]
    public float handDepth = 0.5f;

    [Header("状态")]
    [Tooltip("是否检测到手部")]
    public bool hasHand = false;

    void Start()
    {
        // 如果没有手动指定 dataCollector，尝试自动查找
        if (dataCollector == null)
        {
            dataCollector = FindObjectOfType<HandLandmarkDataCollector>();
            if (dataCollector == null)
            {
                Debug.LogWarning("HandLandmarkProvider: 未找到 HandLandmarkDataCollector，请手动指定或添加该组件");
            }
        }

        // 如果没有指定摄像机，尝试使用主摄像机
        if (arCamera == null)
        {
            arCamera = Camera.main;
            if (arCamera == null)
            {
                Debug.LogWarning("HandLandmarkProvider: 未找到摄像机，请手动指定");
            }
        }
    }

    void Update()
    {
        if (dataCollector == null || arCamera == null)
        {
            hasHand = false;
            return;
        }

        // 从数据收集器获取归一化坐标
        hasHand = dataCollector.hasHand;
        
        if (hasHand)
        {
            // 复制归一化坐标
            System.Array.Copy(dataCollector.normalizedLandmarks, normalizedLandmarks, 21);

            // 将归一化坐标转换为世界坐标
            for (int i = 0; i < 21; i++)
            {
                worldLandmarks[i] = NormalizedToWorld(normalizedLandmarks[i]);
            }
        }
    }

    /// <summary>
    /// 将归一化坐标转换为世界坐标
    /// </summary>
    /// <param name="normalizedPos">归一化坐标 (x, y 范围 0-1)</param>
    /// <returns>世界坐标</returns>
    Vector3 NormalizedToWorld(Vector3 normalizedPos)
    {
        Vector3 screenPos = new Vector3(
            normalizedPos.x * Screen.width,
            (1 - normalizedPos.y) * Screen.height,  // Unity 的 Y 轴是反向的
            handDepth
        );
        return arCamera.ScreenToWorldPoint(screenPos);
    }

    /// <summary>
    /// 获取指定特征点的世界坐标
    /// </summary>
    /// <param name="landmarkIndex">特征点索引（0-20）</param>
    /// <returns>世界坐标</returns>
    public Vector3 GetWorldLandmark(int landmarkIndex)
    {
        if (landmarkIndex < 0 || landmarkIndex >= worldLandmarks.Length)
        {
            return Vector3.zero;
        }
        return worldLandmarks[landmarkIndex];
    }

    /// <summary>
    /// 获取指定特征点的归一化坐标
    /// </summary>
    /// <param name="landmarkIndex">特征点索引（0-20）</param>
    /// <returns>归一化坐标</returns>
    public Vector3 GetNormalizedLandmark(int landmarkIndex)
    {
        if (landmarkIndex < 0 || landmarkIndex >= normalizedLandmarks.Length)
        {
            return Vector3.zero;
        }
        return normalizedLandmarks[landmarkIndex];
    }
}
