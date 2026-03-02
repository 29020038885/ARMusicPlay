using UnityEngine;

/// <summary>
/// 快速场景配置示例
/// 这个脚本展示了如何在运行时动态创建和配置乐器系统
/// </summary>
public class InstrumentSceneSetup : MonoBehaviour
{
    [Header("乐器模型预制体")]
    [Tooltip("古琴模型预制体或场景中的对象")]
    public GameObject guqinPrefab;

    [Tooltip("琵琶模型预制体或场景中的对象")]
    public GameObject pipaPrefab;

    [Header("位置设置")]
    [Tooltip("古琴放置位置")]
    public Vector3 guqinPosition = new Vector3(-0.5f, 0, 0);

    [Tooltip("琵琶放置位置")]
    public Vector3 pipaPosition = new Vector3(0.5f, 0, 0);

    [Header("自动设置")]
    [Tooltip("启动时自动设置场景")]
    public bool autoSetupOnStart = false;

    private GameObject guqinInstance;
    private GameObject pipaInstance;
    private InstrumentInputManager inputManager;
    private InstrumentAudioManager audioManager;

    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupScene();
        }
    }

    /// <summary>
    /// 设置完整场景
    /// </summary>
    public void SetupScene()
    {
        Debug.Log("开始设置乐器场景...");

        // 1. 创建音频管理器
        CreateAudioManager();

        // 2. 创建输入管理器
        CreateInputManager();

        // 3. 设置古琴
        if (guqinPrefab != null)
        {
            SetupGuqin();
        }

        // 4. 设置琵琶
        if (pipaPrefab != null)
        {
            SetupPipa();
        }

        // 5. 连接输入管理器和乐器控制器
        ConnectInputManager();

        Debug.Log("乐器场景设置完成！");
    }

    /// <summary>
    /// 创建音频管理器
    /// </summary>
    void CreateAudioManager()
    {
        audioManager = FindObjectOfType<InstrumentAudioManager>();

        if (audioManager == null)
        {
            GameObject audioObj = new GameObject("AudioManager");
            audioManager = audioObj.AddComponent<InstrumentAudioManager>();
            audioManager.useProceduralAudio = true; // 默认使用程序化音频
            audioManager.showDebugInfo = true;

            Debug.Log("创建了音频管理器");
        }
    }

    /// <summary>
    /// 创建输入管理器
    /// </summary>
    void CreateInputManager()
    {
        inputManager = FindObjectOfType<InstrumentInputManager>();

        if (inputManager == null)
        {
            GameObject inputObj = new GameObject("InputManager");
            inputManager = inputObj.AddComponent<InstrumentInputManager>();
            inputManager.autoDetectPlatform = true;
            inputManager.showDebugInfo = true;
            inputManager.showDebugRay = true;

            Debug.Log("创建了输入管理器");
        }
    }

    /// <summary>
    /// 设置古琴
    /// </summary>
    void SetupGuqin()
    {
        // 检查是否已存在
        guqinInstance = GameObject.Find("Guqin");

        if (guqinInstance == null && guqinPrefab != null)
        {
            guqinInstance = Instantiate(guqinPrefab, guqinPosition, Quaternion.identity);
            guqinInstance.name = "Guqin";
        }

        if (guqinInstance != null)
        {
            // 确保有控制器
            GuqinController controller = guqinInstance.GetComponent<GuqinController>();
            if (controller == null)
            {
                controller = guqinInstance.AddComponent<GuqinController>();
            }

            controller.showDebug = true;

            // 如果没有弦，尝试查找或创建
            if (controller.strings == null || controller.strings.Length == 0)
            {
                InstrumentString[] strings = guqinInstance.GetComponentsInChildren<InstrumentString>();
                if (strings.Length > 0)
                {
                    controller.strings = strings;
                    Debug.Log($"古琴：找到 {strings.Length} 根弦");
                }
                else
                {
                    Debug.LogWarning("古琴：没有找到弦对象，请使用 InstrumentSetupHelper 创建弦");
                }
            }

            Debug.Log("古琴设置完成");
        }
    }

    /// <summary>
    /// 设置琵琶
    /// </summary>
    void SetupPipa()
    {
        // 检查是否已存在
        pipaInstance = GameObject.Find("Pipa");

        if (pipaInstance == null && pipaPrefab != null)
        {
            pipaInstance = Instantiate(pipaPrefab, pipaPosition, Quaternion.identity);
            pipaInstance.name = "Pipa";
        }

        if (pipaInstance != null)
        {
            // 确保有控制器
            PipaController controller = pipaInstance.GetComponent<PipaController>();
            if (controller == null)
            {
                controller = pipaInstance.AddComponent<PipaController>();
            }

            controller.showDebug = true;

            // 如果没有弦，尝试查找或创建
            if (controller.strings == null || controller.strings.Length == 0)
            {
                InstrumentString[] strings = pipaInstance.GetComponentsInChildren<InstrumentString>();
                if (strings.Length > 0)
                {
                    controller.strings = strings;
                    Debug.Log($"琵琶：找到 {strings.Length} 根弦");
                }
                else
                {
                    Debug.LogWarning("琵琶：没有找到弦对象，请使用 InstrumentSetupHelper 创建弦");
                }
            }

            Debug.Log("琵琶设置完成");
        }
    }

    /// <summary>
    /// 连接输入管理器和乐器控制器
    /// </summary>
    void ConnectInputManager()
    {
        if (inputManager == null) return;

        // 连接古琴控制器
        if (guqinInstance != null)
        {
            inputManager.guqinController = guqinInstance.GetComponent<GuqinController>();
        }

        // 连接琵琶控制器
        if (pipaInstance != null)
        {
            inputManager.pipaController = pipaInstance.GetComponent<PipaController>();
        }

        // 设置默认激活古琴
        inputManager.activeInstrument = 0;

        Debug.Log("输入管理器已连接到乐器控制器");
    }

    /// <summary>
    /// 清除场景设置
    /// </summary>
    public void ClearScene()
    {
        if (guqinInstance != null)
        {
            DestroyImmediate(guqinInstance);
        }

        if (pipaInstance != null)
        {
            DestroyImmediate(pipaInstance);
        }

        if (inputManager != null)
        {
            DestroyImmediate(inputManager.gameObject);
        }

        if (audioManager != null)
        {
            DestroyImmediate(audioManager.gameObject);
        }

        Debug.Log("场景已清除");
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(InstrumentSceneSetup))]
public class InstrumentSceneSetupEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        InstrumentSceneSetup setup = (InstrumentSceneSetup)target;

        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.LabelField("场景设置工具", UnityEditor.EditorStyles.boldLabel);

        if (GUILayout.Button("设置完整场景", GUILayout.Height(40)))
        {
            setup.SetupScene();
        }

        UnityEditor.EditorGUILayout.Space();

        if (GUILayout.Button("清除场景设置", GUILayout.Height(30)))
        {
            if (UnityEditor.EditorUtility.DisplayDialog(
                "确认清除",
                "这将删除场景中的乐器和管理器对象。确定要继续吗？",
                "确定",
                "取消"))
            {
                setup.ClearScene();
            }
        }

        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.HelpBox(
            "使用步骤：\n" +
            "1. 将古琴和琵琶模型拖入对应字段\n" +
            "2. 调整位置设置\n" +
            "3. 点击'设置完整场景'按钮\n" +
            "4. 使用 InstrumentSetupHelper 为每个乐器创建弦",
            UnityEditor.MessageType.Info
        );
    }
}
#endif

