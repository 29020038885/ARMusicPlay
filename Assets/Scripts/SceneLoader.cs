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
    /// 按场景名加载（可被 Button 直接绑定）
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("SceneLoader: 场景名为空");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 按 Build Index 加载（场景在 File → Build Settings 里的序号）
    /// </summary>
    public void LoadSceneByIndex(int buildIndex)
    {
        SceneManager.LoadScene(buildIndex);
    }
}
