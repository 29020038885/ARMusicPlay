using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageChange : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform content;
    public List<RectTransform> pages;
    public List<Image> dots;
    public HorizontalLayoutGroup layoutGroup;

    [Header("Buttons")]
    public Button prevButton;
    public Button nextButton;

    [Header("Settings")]
    public float moveSpeed = 10f;
    public float selectedScale = 1.2f;
    public float normalScale = 0.8f;

    private int currentIndex = 0;
    private float stepDistance;
    private Vector2 targetPos;

    void Start()
    {
        CalculateStepDistance();
        MoveToPage(0, true);
        UpdateDots();
        UpdateButtonState();
    }

    void Update()
    {
        SmoothMove();
        UpdateScale();
    }

    // 自动计算步长
    void CalculateStepDistance()
    {
        float pageWidth;

        // 如果使用 LayoutElement 控制宽度
        LayoutElement le = pages[0].GetComponent<LayoutElement>();
        if (le != null)
            pageWidth = le.preferredWidth;
        else
            pageWidth = pages[0].rect.width;

        float spacing = layoutGroup.spacing;

        stepDistance = pageWidth + spacing;

        Debug.Log("Step Distance = " + stepDistance);
    }

    public void NextPage()
    {
        if (currentIndex >= pages.Count - 1)
            return;

        currentIndex++;
        MoveToPage(currentIndex, false);
        UpdateDots();
        UpdateButtonState();
    }

    public void PrevPage()
    {
        if (currentIndex <= 0)
            return;

        currentIndex--;
        MoveToPage(currentIndex, false);
        UpdateDots();
        UpdateButtonState();
    }

    void MoveToPage(int index, bool instant)
    {
        float targetX = index * stepDistance;
        targetPos = new Vector2(-targetX, content.anchoredPosition.y);

        if (instant)
            content.anchoredPosition = targetPos;
    }

    void SmoothMove()
    {
        content.anchoredPosition = Vector2.Lerp(
            content.anchoredPosition,
            targetPos,
            Time.deltaTime * moveSpeed
        );
    }

    void UpdateDots()
    {
        if (dots == null) return;

        for (int i = 0; i < dots.Count; i++)
        {
            dots[i].color = (i == currentIndex) ? Color.green : Color.gray;
        }
    }

    void UpdateScale()
    {
        for (int i = 0; i < pages.Count; i++)
        {
            float distance = Mathf.Abs(i - currentIndex);
            float scale = Mathf.Lerp(selectedScale, normalScale, distance);

            pages[i].localScale = Vector3.Lerp(
                pages[i].localScale,
                Vector3.one * scale,
                Time.deltaTime * 8f
            );
        }
    }

    // 更新按钮是否可点击
    void UpdateButtonState()
    {
        if (prevButton != null)
            prevButton.interactable = currentIndex > 0;

        if (nextButton != null)
            nextButton.interactable = currentIndex < pages.Count - 1;
    }
}