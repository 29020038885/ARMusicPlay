using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 技术演示用：通过 UI 滑块控制每根弦的响度。
/// - 支持古琴 / 琵琶两套面板，各自一组滑块；
/// - 每个滑块对应一根弦，直接映射到 InstrumentString.stringVolume；
/// - 可由按钮调用 ShowGuqinPanel / ShowPipaPanel 在手机端弹出对应面板。
/// </summary>
public class InstrumentVolumePanel : MonoBehaviour
{
    [Header("乐器引用")]
    public GuqinController guqinController;
    public PipaController pipaController;

    [Header("面板对象")]
    [Tooltip("古琴每弦音量滑块面板")]
    public GameObject guqinPanel;

    [Tooltip("琵琶每弦音量滑块面板")]
    public GameObject pipaPanel;

    [Header("古琴弦滑块（顺序需与 stringIndex 对应）")]
    public Slider[] guqinStringSliders;

    [Header("琵琶弦滑块（顺序需与 stringIndex 对应）")]
    public Slider[] pipaStringSliders;

    [Header("滑块范围")]
    [Tooltip("每根弦音量滑块的最小值")]
    public float sliderMin = 0f;

    [Tooltip("每根弦音量滑块的最大值（1 为原始响度，可略大一点做演示）")]
    public float sliderMax = 1.5f;

    /// <summary>记录当前逻辑上的激活乐器：0=古琴，1=琵琶</summary>
    public int activeInstrumentIndex = 0;

    void Start()
    {
        if (guqinController == null)
            guqinController = FindObjectOfType<GuqinController>();
        if (pipaController == null)
            pipaController = FindObjectOfType<PipaController>();

        SetupSliders(guqinStringSliders, true);
        SetupSliders(pipaStringSliders, false);

        // 初始同步一次，使滑块值与当前弦的 stringVolume 一致
        SyncGuqinFromStrings();
        SyncPipaFromStrings();
    }

    void SetupSliders(Slider[] sliders, bool isGuqin)
    {
        if (sliders == null) return;

        for (int i = 0; i < sliders.Length; i++)
        {
            var s = sliders[i];
            if (s == null) continue;
            s.minValue = sliderMin;
            s.maxValue = sliderMax;

            int index = i; // 捕获局部变量，避免闭包问题
            s.onValueChanged.AddListener(v =>
            {
                if (isGuqin)
                    SetGuqinStringVolume(index, v);
                else
                    SetPipaStringVolume(index, v);
            });
        }
    }

    /// <summary>
    /// UI 按钮可直接绑定：显示古琴每弦音量面板，隐藏琵琶面板并同步当前值。
    /// </summary>
    public void ShowGuqinPanel()
    {
        activeInstrumentIndex = 0;
        if (guqinPanel != null) guqinPanel.SetActive(true);
        if (pipaPanel != null) pipaPanel.SetActive(false);
        SyncGuqinFromStrings();
    }

    /// <summary>
    /// UI 按钮可直接绑定：显示琵琶每弦音量面板，隐藏古琴面板并同步当前值。
    /// </summary>
    public void ShowPipaPanel()
    {
        activeInstrumentIndex = 1;
        if (pipaPanel != null) pipaPanel.SetActive(true);
        if (guqinPanel != null) guqinPanel.SetActive(false);
        SyncPipaFromStrings();
    }

    /// <summary>
    /// 供 InstrumentInputManager 调用：当当前激活乐器改变时同步内部状态。
    /// 不会强制弹出/关闭面板，只在面板当前已显示时切换内部子面板，并总是刷新滑块数据。
    /// </summary>
    public void OnInstrumentChanged(int index)
    {
        activeInstrumentIndex = index;

        bool panelVisible = (guqinPanel != null && guqinPanel.activeInHierarchy) ||
                            (pipaPanel != null && pipaPanel.activeInHierarchy);

        if (!panelVisible)
        {
            // 面板当前是被你自己的按钮隐藏着的，只同步数据，不改显示状态
            if (index == 0) SyncGuqinFromStrings();
            else SyncPipaFromStrings();
            return;
        }

        // 面板当前已打开，则顺带切换内部显示，使演示状态和当前乐器一致
        if (index == 0) ShowGuqinPanel();
        else ShowPipaPanel();
    }

    void SetGuqinStringVolume(int stringIndex, float value)
    {
        if (guqinController == null || guqinController.strings == null) return;
        if (stringIndex < 0 || stringIndex >= guqinController.strings.Length) return;
        var str = guqinController.strings[stringIndex];
        if (str == null) return;
        str.stringVolume = Mathf.Clamp(value, sliderMin, sliderMax);
    }

    void SetPipaStringVolume(int stringIndex, float value)
    {
        if (pipaController == null || pipaController.strings == null) return;
        if (stringIndex < 0 || stringIndex >= pipaController.strings.Length) return;
        var str = pipaController.strings[stringIndex];
        if (str == null) return;
        str.stringVolume = Mathf.Clamp(value, sliderMin, sliderMax);
    }

    /// <summary>
    /// 将古琴当前每弦的 stringVolume 同步到滑块（用于面板打开时刷新 UI）。
    /// </summary>
    public void SyncGuqinFromStrings()
    {
        if (guqinController == null || guqinController.strings == null || guqinStringSliders == null) return;
        int count = Mathf.Min(guqinController.strings.Length, guqinStringSliders.Length);
        for (int i = 0; i < count; i++)
        {
            var str = guqinController.strings[i];
            var slider = guqinStringSliders[i];
            if (str == null || slider == null) continue;
            slider.SetValueWithoutNotify(Mathf.Clamp(str.stringVolume, sliderMin, sliderMax));
        }
    }

    /// <summary>
    /// 将琵琶当前每弦的 stringVolume 同步到滑块（用于面板打开时刷新 UI）。
    /// </summary>
    public void SyncPipaFromStrings()
    {
        if (pipaController == null || pipaController.strings == null || pipaStringSliders == null) return;
        int count = Mathf.Min(pipaController.strings.Length, pipaStringSliders.Length);
        for (int i = 0; i < count; i++)
        {
            var str = pipaController.strings[i];
            var slider = pipaStringSliders[i];
            if (str == null || slider == null) continue;
            slider.SetValueWithoutNotify(Mathf.Clamp(str.stringVolume, sliderMin, sliderMax));
        }
    }
}

