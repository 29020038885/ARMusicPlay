using UnityEngine;

[System.Serializable]
public class PageData
{
    public Sprite image;
    public string title;
    [TextArea(3,10)]
    public string content;
}