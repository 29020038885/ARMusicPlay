using UnityEngine;

/// <summary>
/// 品位信息组件 - 标识品位碰撞体属于哪根弦和哪个品
/// </summary>
public class FretInfo : MonoBehaviour
{
    [Header("品位信息")]
    [Tooltip("该品位属于哪根弦（从0开始）")]
    public int stringIndex = 0;

    [Tooltip("该品位的索引（从0开始）。0=第一品；空弦=不按品（不记录或拨弦时不应用按品）")]
    public int fretIndex = 0;

    [Tooltip("若勾选：该碰撞体视为「空弦」区域，点击后拨弦时按空弦发音，不应用按品")]
    public bool treatAsOpenString = false;

    [Tooltip("是否显示调试信息")]
    public bool showDebug = false;

    void Start()
    {
        if (showDebug)
        {
            Debug.Log($"FretInfo: 弦 {stringIndex} 第 {fretIndex} 品已初始化");
        }
    }

    /// <summary>
    /// 获取品位信息
    /// </summary>
    public (int stringIndex, int fretIndex) GetFretInfo()
    {
        return (stringIndex, fretIndex);
    }
}
