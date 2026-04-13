using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 乐器场景设置助手 - 编辑器工具
/// 帮助快速设置古琴和琵琶的弦和碰撞体
/// </summary>
#if UNITY_EDITOR
[ExecuteInEditMode]
public class InstrumentSetupHelper : MonoBehaviour
{
    [Header("乐器选择")]
    [Tooltip("要设置的乐器类型")]
    public InstrumentType instrumentType = InstrumentType.Guqin;

    [Header("乐器模型")]
    [Tooltip("乐器的根对象")]
    public GameObject instrumentModel;

    [Header("弦设置")]
    [Tooltip("弦的数量")]
    public int numberOfStrings = 7;

    [Tooltip("弦的起始位置（本地坐标）")]
    public Vector3 stringStartPosition = new Vector3(-0.15f, 0.02f, 0.5f);

    [Tooltip("弦的结束位置（本地坐标）")]
    public Vector3 stringEndPosition = new Vector3(-0.15f, 0.02f, -0.5f);

    [Tooltip("弦之间的间距")]
    public float stringSpacing = 0.05f;

    [Tooltip("弦的厚度（用于碰撞体）")]
    public float stringThickness = 0.005f;

    [Tooltip("弦的宽度（用于碰撞体）")]
    public float stringWidth = 0.01f;

    [Header("材质")]
    [Tooltip("弦的材质")]
    public Material stringMaterial;

    [Header("自动设置")]
    [Tooltip("自动创建控制器组件")]
    public bool autoCreateController = true;

    [Tooltip("自动创建碰撞体")]
    public bool autoCreateColliders = true;

    public enum InstrumentType
    {
        Guqin,  // 古琴 (7弦)
        Pipa    // 琵琶 (4弦)
    }

    /// <summary>
    /// 创建弦对象
    /// </summary>
    public void CreateStrings()
    {
        if (instrumentModel == null)
        {
            Debug.LogError("请先指定乐器模型！");
            return;
        }

        // 设置弦的数量
        if (instrumentType == InstrumentType.Guqin)
        {
            numberOfStrings = 7;
        }
        else if (instrumentType == InstrumentType.Pipa)
        {
            numberOfStrings = 4;
        }

        // 清除旧的弦对象
        Transform stringsContainer = instrumentModel.transform.Find("Strings");
        if (stringsContainer != null)
        {
            DestroyImmediate(stringsContainer.gameObject);
        }

        // 创建弦容器
        GameObject stringsObj = new GameObject("Strings");
        stringsObj.transform.SetParent(instrumentModel.transform);
        stringsObj.transform.localPosition = Vector3.zero;
        stringsObj.transform.localRotation = Quaternion.identity;
        stringsObj.transform.localScale = Vector3.one;

        // 创建每根弦
        for (int i = 0; i < numberOfStrings; i++)
        {
            CreateString(stringsObj.transform, i);
        }

        Debug.Log($"创建了 {numberOfStrings} 根弦");
    }

    /// <summary>
    /// 创建单根弦
    /// </summary>
    void CreateString(Transform parent, int index)
    {
        GameObject stringObj = new GameObject($"String_{index}");
        stringObj.transform.SetParent(parent);

        // 计算弦的位置
        Vector3 offset = Vector3.right * (stringSpacing * index);
        Vector3 startPos = stringStartPosition + offset;
        Vector3 endPos = stringEndPosition + offset;
        Vector3 centerPos = (startPos + endPos) / 2f;

        stringObj.transform.localPosition = centerPos;
        stringObj.transform.localRotation = Quaternion.identity;

        // 计算弦的长度
        float stringLength = Vector3.Distance(startPos, endPos);

        // 创建视觉表示（线渲染器）
        LineRenderer lineRenderer = stringObj.AddComponent<LineRenderer>();
        lineRenderer.startWidth = stringThickness;
        lineRenderer.endWidth = stringThickness;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = false;
        lineRenderer.SetPosition(0, startPos - centerPos);
        lineRenderer.SetPosition(1, endPos - centerPos);

        if (stringMaterial != null)
            lineRenderer.sharedMaterial = stringMaterial;
        else
        {
            Material def = InstrumentStringLineMaterialCache.Get();
            if (def != null)
                lineRenderer.sharedMaterial = def;
        }

        // 创建碰撞体
        if (autoCreateColliders)
        {
            BoxCollider collider = stringObj.AddComponent<BoxCollider>();
            collider.size = new Vector3(stringWidth, stringThickness * 2, stringLength);
            collider.center = Vector3.zero;
        }

        // 添加 InstrumentString 组件
        InstrumentString instrumentString = stringObj.AddComponent<InstrumentString>();
        instrumentString.stringIndex = index;

        // 设置弦的标签（如果标签存在）
        try
        {
            stringObj.tag = "InstrumentString";
        }
        catch
        {
            Debug.LogWarning("InstrumentSetupHelper: 标签 'InstrumentString' 未定义");
        }
        stringObj.layer = LayerMask.NameToLayer("Default");
    }

    /// <summary>
    /// 创建控制器
    /// </summary>
    public void CreateController()
    {
        if (instrumentModel == null)
        {
            Debug.LogError("请先指定乐器模型！");
            return;
        }

        if (instrumentType == InstrumentType.Guqin)
        {
            GuqinController controller = instrumentModel.GetComponent<GuqinController>();
            if (controller == null)
            {
                controller = instrumentModel.AddComponent<GuqinController>();
            }

            // 自动查找弦
            InstrumentString[] strings = instrumentModel.GetComponentsInChildren<InstrumentString>();
            controller.strings = strings;

            Debug.Log("创建了古琴控制器");
        }
        else if (instrumentType == InstrumentType.Pipa)
        {
            PipaController controller = instrumentModel.GetComponent<PipaController>();
            if (controller == null)
            {
                controller = instrumentModel.AddComponent<PipaController>();
            }

            // 自动查找弦
            InstrumentString[] strings = instrumentModel.GetComponentsInChildren<InstrumentString>();
            controller.strings = strings;

            Debug.Log("创建了琵琶控制器");
        }
    }

    /// <summary>
    /// 完整设置
    /// </summary>
    public void SetupInstrument()
    {
        CreateStrings();
        
        if (autoCreateController)
        {
            CreateController();
        }

        Debug.Log($"{instrumentType} 设置完成！");
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(InstrumentSetupHelper))]
public class InstrumentSetupHelperEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        InstrumentSetupHelper helper = (InstrumentSetupHelper)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("快速设置", EditorStyles.boldLabel);

        if (GUILayout.Button("创建弦", GUILayout.Height(30)))
        {
            helper.CreateStrings();
        }

        if (GUILayout.Button("创建控制器", GUILayout.Height(30)))
        {
            helper.CreateController();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("完整设置乐器", GUILayout.Height(40)))
        {
            helper.SetupInstrument();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "使用步骤：\n" +
            "1. 选择乐器类型（古琴或琵琶）\n" +
            "2. 拖拽乐器模型到 Instrument Model 字段\n" +
            "3. 调整弦的位置和间距参数\n" +
            "4. 点击'完整设置乐器'按钮",
            MessageType.Info
        );
    }
}
#endif
#endif

