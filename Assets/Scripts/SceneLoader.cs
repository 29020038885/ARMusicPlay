using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景切换：用按钮从 UI 场景切到弹奏场景等。
/// 用法：挂在任意物体上，在 Button 的 On Click() 里添加该物体，选 SceneLoader → LoadScene 或 LoadPlayScene。
/// </summary>
public class SceneLoader : MonoBehaviour
{
    [Header("场景名（需在 Build Settings 中勾选）")]
    [Tooltip("弹奏场景的名称（例如：interact scence）")]
    public string playSceneName = "interact scence";

    [Tooltip("UI 场景的名称（返回主界面用）")]
    public string uiSceneName = "UIpaper";

    [Tooltip("AR 识别场景的名称（例如：ARDector）")]
    public string arDectorSceneName = "ARDector";

    [Header("手势摄像头控制（可选）")]
    [Tooltip("当前场景中的 HandInteractionManager。若指定，在切换场景前会关闭手势摄像头。")]
    public HandInteractionManager handInteractionManager;

    /// <summary>
    /// 加载弹奏场景（给「开始弹奏」等按钮用）
    /// </summary>
    public void LoadPlayScene()
    {
        LoadScene(playSceneName);
    }

    /// <summary>
    /// 加载 UI 场景（给「返回」按钮用）
    /// </summary>
    public void LoadUIScene()
    {
        LoadScene(uiSceneName);
    }

    /// <summary>
    /// 加载 AR 识别场景（给「进入 AR 识别」按钮用）
    /// </summary>
    public void LoadARDectorScene()
    {
        LoadScene(arDectorSceneName);
    }

    /// <summary>
    /// 按场景名加载（可被 Button 直接绑定）
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("SceneLoader: 场景名为空");
            return;
        }

        // 离开当前场景前，如有手势交互管理器，则先关闭摄像头
        if (handInteractionManager != null)
        {
            handInteractionManager.DisableHandCamera();
        }

        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 按 Build Index 加载（场景在 File → Build Settings 里的序号）
    /// </summary>
    public void LoadSceneByIndex(int buildIndex)
    {
        if (handInteractionManager != null)
        {
            handInteractionManager.DisableHandCamera();
        }

        SceneManager.LoadScene(buildIndex);
    }
}
