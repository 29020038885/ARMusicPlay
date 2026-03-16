using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class ShowNDisButton : MonoBehaviour
{
    [SerializeField] private GameObject[] dispalyObjects;
    [SerializeField] private GameObject[] showObjects;
    // Start is called before the first frame update
    public void HideObjects()
    {
        if(dispalyObjects == null) return;
        foreach (var item in dispalyObjects)
        {
            item.SetActive(false);
        }
    }

    public void ShowObjects()
    {
        if(showObjects == null) return;
        foreach (var item in showObjects)
        {
            item.SetActive(true);
        }
    }
}