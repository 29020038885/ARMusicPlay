using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonChangeImage : MonoBehaviour
{
    private int count;
    [SerializeField]private GameObject[] images;
    void Start()
    {
        count = 0;
        if(images != null)
        {
            for(int i = 0; i < images.Length; i++)
            {
                images[i].gameObject.SetActive(false);
            }
            images[0].gameObject.SetActive(true);
        }
    }
    public void changeImage()
    {
    // 先隐藏所有图片
        for(int i = 0; i < images.Length; i++)
        {
            images[i].gameObject.SetActive(false);
        }
        
        // 更新计数器
        count++;
        if(count >= images.Length)
        {
            count = 0;
        }
        
        // 只激活当前目标图片
        images[count].gameObject.SetActive(true);
    }
}

