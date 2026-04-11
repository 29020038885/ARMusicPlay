using System.Collections.Generic;
using UnityEngine;
using Vuforia;

/// <summary>
/// 随 Vuforia 识别状态显示/隐藏模型；丢失目标时强制隐藏。
/// 可由 <see cref="ARSceneInteractionController"/> 在故事/名曲/弹奏 UI 打开时临时压制显示。
/// </summary>
public class ImageTargetHandler : MonoBehaviour
{
    static readonly List<ImageTargetHandler> Instances = new List<ImageTargetHandler>();

    /// <summary>与 Vuforia DefaultObserverEventHandler 一致，决定何种 Status 视为「可见」。</summary>
    public enum TrackingStatusFilter
    {
        TrackedOnly,
        Tracked_ExtendedTracked,
        Tracked_ExtendedTracked_Limited
    }

    ObserverBehaviour observerBehaviour;

    [Tooltip("识别到目标时显示、未识别或 UI 压制时隐藏的模型根物体")]
    public GameObject model;

    [Tooltip("图片移出相机后 Vuforia 仍常为 EXTENDED_TRACKED，模型会留在画面上。默认仅用 TRACKED（图在视野内）；需要扩展跟踪稳定性的可改为 Tracked_ExtendedTracked。")]
    public TrackingStatusFilter statusFilter = TrackingStatusFilter.TrackedOnly;

    [Tooltip("勾选则不受 ARSceneInteractionController 的全局「UI 时隐藏模型」影响")]
    public bool excludeFromUiOverlayHide;

    bool lastTargetVisible;
    bool uiOverlaySuppressed;

    void Awake()
    {
        observerBehaviour = GetComponent<ObserverBehaviour>()
            ?? GetComponentInParent<ObserverBehaviour>()
            ?? GetComponentInChildren<ObserverBehaviour>(true);
        if (observerBehaviour == null)
            Debug.LogError($"ImageTargetHandler: 未找到 ObserverBehaviour（{name}）。请挂在 ImageTarget 或其父/子物体上。");
    }

    void OnEnable()
    {
        Instances.Add(this);
        if (model != null)
            model.SetActive(false);

        if (observerBehaviour != null)
        {
            observerBehaviour.OnTargetStatusChanged += OnTargetStatusChanged;
            OnTargetStatusChanged(observerBehaviour, observerBehaviour.TargetStatus);
        }
        else
        {
            lastTargetVisible = false;
            ApplyModelVisibility();
        }
    }

    void OnDisable()
    {
        if (observerBehaviour != null)
            observerBehaviour.OnTargetStatusChanged -= OnTargetStatusChanged;
        Instances.Remove(this);
        if (model != null)
            model.SetActive(false);
    }

    void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        lastTargetVisible = ShouldBeRendered(status.Status);
        ApplyModelVisibility();
    }

    bool ShouldBeRendered(Status status)
    {
        if (status == Status.TRACKED)
            return true;
        if (statusFilter == TrackingStatusFilter.Tracked_ExtendedTracked && status == Status.EXTENDED_TRACKED)
            return true;
        if (statusFilter == TrackingStatusFilter.Tracked_ExtendedTracked_Limited &&
            (status == Status.EXTENDED_TRACKED || status == Status.LIMITED))
            return true;
        return false;
    }

    /// <summary>由 ARSceneInteractionController 调用：打开故事/名曲/弹奏时为 true，回到功能菜单或全关 UI 时为 false。</summary>
    public void SetUiOverlaySuppressed(bool suppressed)
    {
        if (excludeFromUiOverlayHide)
            return;
        uiOverlaySuppressed = suppressed;
        ApplyModelVisibility();
    }

    public static void SetUiOverlaySuppressedOnAll(bool suppressed)
    {
        for (int i = Instances.Count - 1; i >= 0; i--)
        {
            if (Instances[i] != null)
                Instances[i].SetUiOverlaySuppressed(suppressed);
        }
    }

    void ApplyModelVisibility()
    {
        if (model == null) return;
        model.SetActive(lastTargetVisible && !uiOverlaySuppressed);
    }
}
