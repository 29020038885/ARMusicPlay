using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CategoryData
{
    public string categoryName;
    public List<PageData> pages = new List<PageData>();
}