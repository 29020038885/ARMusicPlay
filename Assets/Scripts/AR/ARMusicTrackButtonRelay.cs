using UnityEngine;

/// <summary>
/// 挂在名曲按钮上：无参 OnClick → 按配置的曲目名调用总控播放。
/// 当 Inspector 不方便绑定「带 string 参数」的 UnityEvent 时使用。
/// </summary>
public class ARMusicTrackButtonRelay : MonoBehaviour
{
    public ARSceneInteractionController controller;

    [Tooltip("与 ARSceneInteractionController 里该乐器的 pieceName / title 一致，或与按钮文案子串匹配（见 looseTrackNameMatch）")]
    public string pieceNameOrLabel = "高山流水";

    public void OnClickPlayThisTrack()
    {
        if (controller == null) return;
        controller.ToggleTrackByPieceName(pieceNameOrLabel);
    }
}
