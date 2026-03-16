using UnityEngine;
using System.Collections;

/// <summary>
/// 乐器调试工具 - 帮助诊断音频和交互问题
/// </summary>
public class InstrumentDebugger : MonoBehaviour
{
    [Header("调试目标")]
    [Tooltip("古琴控制器")]
    public GuqinController guqinController;
    
    [Tooltip("琵琶控制器")]
    public PipaController pipaController;

    [Header("调试选项")]
    [Tooltip("显示详细日志")]
    public bool showDetailedLogs = true;

    [Tooltip("自动测试所有弦")]
    public bool autoTestMode = false;

    [Tooltip("自动测试间隔（秒）")]
    public float autoTestInterval = 1f;

    [Header("状态信息（只读）")]
    public int totalStrings = 0;
    public int stringsWithAudio = 0;
    public int stringsWithCollider = 0;
    public bool hasInputManager = false;
    public bool hasAudioManager = false;

    private InstrumentString[] strings;
    private float nextTestTime = 0f;
    private int currentTestString = 0;

    void Start()
    {
        Invoke("PerformDiagnostic", 0.5f);
    }

    void Update()
    {
        if (autoTestMode && Time.time >= nextTestTime)
        {
            TestNextString();
            nextTestTime = Time.time + autoTestInterval;
        }
    }

    /// <summary>
    /// 执行完整诊断
    /// </summary>
    public void PerformDiagnostic()
    {
        Debug.Log("=== 🔍 开始乐器系统诊断 ===\n");

        CheckController();
        CheckStrings();
        CheckInputManager();
        CheckAudioManager();
        CheckCamera();

        Debug.Log("\n=== ✅ 诊断完成 ===");
        ShowSummary();
    }

    void CheckController()
    {
        Debug.Log("📋 检查控制器...");

        if (guqinController != null)
        {
            Debug.Log($"✅ 找到古琴控制器：{guqinController.gameObject.name}");
            strings = guqinController.strings;
            totalStrings = strings != null ? strings.Length : 0;
        }
        else if (pipaController != null)
        {
            Debug.Log($"✅ 找到琵琶控制器：{pipaController.gameObject.name}");
            strings = pipaController.strings;
            totalStrings = strings != null ? strings.Length : 0;
        }
        else
        {
            Debug.LogError("❌ 未指定乐器控制器！请在 Inspector 中设置。");
        }
    }

    void CheckStrings()
    {
        Debug.Log("\n🎸 检查弦...");

        if (strings == null || strings.Length == 0)
        {
            Debug.LogError("❌ 没有找到弦！请使用 InstrumentSetupHelper 创建弦。");
            return;
        }

        stringsWithAudio = 0;
        stringsWithCollider = 0;

        for (int i = 0; i < strings.Length; i++)
        {
            InstrumentString str = strings[i];
            if (str == null)
            {
                Debug.LogError($"❌ 弦 {i} 为空！");
                continue;
            }

            bool hasCollider = str.GetComponent<Collider>() != null;
            bool hasAudioSource = str.GetComponent<AudioSource>() != null;
            bool hasOpenStringAudio = str.openStringSound != null;
            bool hasFretAudio = str.fretSounds != null && str.fretSounds.Length > 0;

            if (hasCollider) stringsWithCollider++;
            if (hasOpenStringAudio || hasFretAudio) stringsWithAudio++;

            if (showDetailedLogs)
            {
                string status = (hasCollider && (hasOpenStringAudio || hasFretAudio)) ? "✅" : "⚠️";
                Debug.Log($"{status} 弦 {i} ({str.gameObject.name}):");
                Debug.Log($"   - Collider: {(hasCollider ? "✅" : "❌")}");
                Debug.Log($"   - AudioSource: {(hasAudioSource ? "✅" : "❌")}");
                Debug.Log($"   - 空弦音: {(hasOpenStringAudio ? "✅" : "❌")}");
                Debug.Log($"   - 品位音: {(hasFretAudio ? $"✅ ({str.fretSounds.Length}个)" : "❌")}");
            }
        }

        Debug.Log($"\n总结：{strings.Length} 根弦，{stringsWithCollider} 个有碰撞体，{stringsWithAudio} 个有音频");
    }

    void CheckInputManager()
    {
        Debug.Log("\n🎮 检查输入管理器...");

        InstrumentInputManager inputManager = FindObjectOfType<InstrumentInputManager>();
        hasInputManager = inputManager != null;

        if (inputManager == null)
        {
            Debug.LogWarning("⚠️ 未找到 InstrumentInputManager！\n" +
                           "请创建一个空对象并添加 InstrumentInputManager 组件。");
            return;
        }

        Debug.Log($"✅ 找到输入管理器：{inputManager.gameObject.name}");

        if (inputManager.inputCamera == null)
        {
            Debug.LogWarning("⚠️ InputManager 未设置摄像机！");
        }
        else
        {
            Debug.Log($"   - 摄像机: ✅ {inputManager.inputCamera.name}");
        }

        if (guqinController != null && inputManager.guqinController != guqinController)
        {
            Debug.LogWarning("⚠️ InputManager 的 guqinController 未正确设置！");
        }
        else if (pipaController != null && inputManager.pipaController != pipaController)
        {
            Debug.LogWarning("⚠️ InputManager 的 pipaController 未正确设置！");
        }
        else
        {
            Debug.Log("   - 控制器引用: ✅");
        }
    }

    void CheckAudioManager()
    {
        Debug.Log("\n🔊 检查音频管理器...");

        InstrumentAudioManager audioManager = FindObjectOfType<InstrumentAudioManager>();
        hasAudioManager = audioManager != null;

        if (audioManager == null)
        {
            Debug.LogWarning("⚠️ 未找到 InstrumentAudioManager（可选）\n" +
                           "如果需要程序化音频，请创建一个空对象并添加 InstrumentAudioManager 组件。");
            return;
        }

        Debug.Log($"✅ 找到音频管理器：{audioManager.gameObject.name}");
        Debug.Log($"   - 程序化音频: {(audioManager.useProceduralAudio ? "✅ 启用" : "❌ 禁用")}");
    }

    void CheckCamera()
    {
        Debug.Log("\n📷 检查摄像机...");

        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("❌ 场景中没有主摄像机！请确保摄像机有 MainCamera 标签。");
            return;
        }

        Debug.Log($"✅ 找到主摄像机：{mainCam.name}");
    }

    void ShowSummary()
    {
        Debug.Log("\n" + new string('=', 50));
        Debug.Log("📊 诊断摘要");
        Debug.Log(new string('=', 50));
        Debug.Log($"弦: {totalStrings} 根");
        Debug.Log($"有音频的弦: {stringsWithAudio}/{totalStrings}");
        Debug.Log($"有碰撞体的弦: {stringsWithCollider}/{totalStrings}");
        Debug.Log($"输入管理器: {(hasInputManager ? "✅" : "❌")}");
        Debug.Log($"音频管理器: {(hasAudioManager ? "✅" : "⚠️ (可选)")}");
        Debug.Log(new string('=', 50) + "\n");

        // 给出建议
        if (stringsWithAudio == 0 && totalStrings > 0)
        {
            Debug.LogWarning("💡 建议：弦没有音频！\n" +
                           "解决方法：\n" +
                           "1. 在 InstrumentString 组件中设置 openStringSound\n" +
                           "2. 或者创建 InstrumentAudioManager 并启用程序化音频");
        }

        if (stringsWithCollider < totalStrings)
        {
            Debug.LogWarning("💡 建议：有些弦没有碰撞体，无法被点击！");
        }

        if (!hasInputManager)
        {
            Debug.LogError("💡 必须创建 InstrumentInputManager 才能交互！");
        }
    }

    /// <summary>
    /// 测试所有弦
    /// </summary>
    public void TestAllStrings()
    {
        if (strings == null || strings.Length == 0)
        {
            Debug.LogError("没有弦可以测试！");
            return;
        }

        Debug.Log("🎵 开始测试所有弦...");
        StartCoroutine(TestAllStringsCoroutine());
    }

    IEnumerator TestAllStringsCoroutine()
    {
        for (int i = 0; i < strings.Length; i++)
        {
            if (strings[i] != null)
            {
                Debug.Log($"测试弦 {i}...");
                strings[i].PluckString();
                yield return new WaitForSeconds(0.5f);
            }
        }
        Debug.Log("✅ 测试完成！");
    }

    void TestNextString()
    {
        if (strings == null || strings.Length == 0) return;

        if (strings[currentTestString] != null)
        {
            Debug.Log($"自动测试：弦 {currentTestString}");
            strings[currentTestString].PluckString();
        }

        currentTestString = (currentTestString + 1) % strings.Length;
    }

    void OnDrawGizmos()
    {
        if (strings == null) return;

        Gizmos.color = Color.cyan;
        foreach (var str in strings)
        {
            if (str != null)
            {
                Gizmos.DrawWireSphere(str.transform.position, 0.02f);
            }
        }
    }
}
