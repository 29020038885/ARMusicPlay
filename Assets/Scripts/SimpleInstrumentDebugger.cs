using UnityEngine;
using System.Collections;

/// <summary>
/// 乐器调试工具 - 简化版
/// 帮助诊断音频和交互问题
/// </summary>
public class SimpleInstrumentDebugger : MonoBehaviour
{
    [Header("要调试的乐器")]
    public GuqinController guqinController;
    public PipaController pipaController;

    [Header("自动测试")]
    public bool autoTestStrings = false;
    public float testInterval = 1f;

    private InstrumentString[] strings;
    private int currentTestIndex = 0;
    private float nextTestTime = 0f;

    void Start()
    {
        Invoke("RunDiagnostic", 0.5f);
    }

    void Update()
    {
        if (autoTestStrings && Time.time >= nextTestTime && strings != null)
        {
            TestNextString();
            nextTestTime = Time.time + testInterval;
        }
    }

    public void RunDiagnostic()
    {
        Debug.Log("=== 🔍 乐器系统诊断 ===");
        
        // 获取弦
        if (guqinController != null)
        {
            strings = guqinController.strings;
            Debug.Log($"✅ 找到古琴，{strings?.Length ?? 0} 根弦");
        }
        else if (pipaController != null)
        {
            strings = pipaController.strings;
            Debug.Log($"✅ 找到琵琶，{strings?.Length ?? 0} 根弦");
        }
        else
        {
            Debug.LogError("❌ 请设置古琴或琵琶控制器！");
            return;
        }

        if (strings == null || strings.Length == 0)
        {
            Debug.LogError("❌ 没有找到弦！");
            return;
        }

        // 检查每根弦
        int audioCount = 0;
        int colliderCount = 0;

        for (int i = 0; i < strings.Length; i++)
        {
            if (strings[i] == null) continue;

            bool hasCollider = strings[i].GetComponent<Collider>() != null;
            bool hasAudio = strings[i].openStringSound != null;

            if (hasCollider) colliderCount++;
            if (hasAudio) audioCount++;

            Debug.Log($"弦 {i}: Collider={hasCollider}, Audio={hasAudio}");
        }

        Debug.Log($"\n总计: {colliderCount}/{strings.Length} 有碰撞体, {audioCount}/{strings.Length} 有音频");

        // 检查 InputManager
        InstrumentInputManager inputMgr = FindObjectOfType<InstrumentInputManager>();
        if (inputMgr == null)
        {
            Debug.LogWarning("⚠️ 未找到 InstrumentInputManager!");
        }
        else
        {
            Debug.Log("✅ 找到 InputManager");
        }

        // 检查 AudioManager
        InstrumentAudioManager audioMgr = FindObjectOfType<InstrumentAudioManager>();
        if (audioMgr == null)
        {
            Debug.LogWarning("⚠️ 未找到 InstrumentAudioManager (可选)");
        }
        else
        {
            Debug.Log($"✅ 找到 AudioManager，程序化音频={audioMgr.useProceduralAudio}");
        }

        Debug.Log("=== ✅ 诊断完成 ===\n");
    }

    public void TestAllStrings()
    {
        if (strings == null || strings.Length == 0)
        {
            Debug.LogError("没有弦可测试！");
            return;
        }

        StartCoroutine(TestAllStringsRoutine());
    }

    IEnumerator TestAllStringsRoutine()
    {
        Debug.Log("🎵 测试所有弦...");

        for (int i = 0; i < strings.Length; i++)
        {
            if (strings[i] != null)
            {
                Debug.Log($"测试弦 {i}");
                strings[i].PluckString();
                yield return new WaitForSeconds(0.5f);
            }
        }

        Debug.Log("✅ 测试完成！");
    }

    void TestNextString()
    {
        if (strings == null || strings.Length == 0) return;

        if (strings[currentTestIndex] != null)
        {
            strings[currentTestIndex].PluckString();
        }

        currentTestIndex = (currentTestIndex + 1) % strings.Length;
    }
}

