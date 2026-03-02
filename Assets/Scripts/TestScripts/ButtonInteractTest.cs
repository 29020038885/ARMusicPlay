using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonInteractTest : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]private GameObject Image;
    private int count = 0;
    public void ImageHandle()
    {
        count++;
        if(count % 2 == 0)
        {
            Image.SetActive(false);
            count = 0;
        }
        else
        {
            Image.SetActive(true);
        }
    }

}
