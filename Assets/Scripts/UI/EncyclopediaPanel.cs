using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EncyclopediaPanel : MonoBehaviour
{
    [Header("Panel Root")]
    public GameObject panelRoot;

    [Header("UI Components")]
    public Image displayImage;
    public TMP_Text titleText;
    public TMP_Text contentText;
    public TMP_Text pageNumberText;

    [Header("Buttons")]
    public Button prevButton;
    public Button nextButton;
    public Button closeButton;

    [Header("All Categories")]
    public List<CategoryData> categories = new List<CategoryData>();

    private int currentCategoryIndex = 0;
    private int currentPageIndex = 0;

    void Awake()
    {
        prevButton.onClick.AddListener(PrevPage);
        nextButton.onClick.AddListener(NextPage);
        closeButton.onClick.AddListener(ClosePanel);
    }

    void Start()
    {
        panelRoot.SetActive(false);
    }

    // 打开某个类别
    public void OpenCategory(int categoryIndex)
    {
        currentCategoryIndex = categoryIndex;
        currentPageIndex = 0;

        panelRoot.SetActive(true);
        RefreshPage();
    }

    public void ClosePanel()
    {
        panelRoot.SetActive(false);
    }

    // 下一页
    public void NextPage()
    {
        var category = categories[currentCategoryIndex];

        if (currentPageIndex < category.pages.Count - 1)
        {
            currentPageIndex++;
            RefreshPage();
        }
    }

    // 上一页
    public void PrevPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            RefreshPage();
        }
    }

    // 刷新页面
    void RefreshPage()
    {
        var category = categories[currentCategoryIndex];
        var page = category.pages[currentPageIndex];

        displayImage.sprite = page.image;
        titleText.text = page.title;
        contentText.text = page.content;

        // 页码显示
        pageNumberText.text = (currentPageIndex + 1) + " / " + category.pages.Count;

        UpdateButtons();
    }

    // 更新按钮状态
    void UpdateButtons()
    {
        var category = categories[currentCategoryIndex];

        prevButton.interactable = currentPageIndex > 0;
        nextButton.interactable = currentPageIndex < category.pages.Count - 1;
    }
}