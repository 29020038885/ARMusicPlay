using UnityEngine;
using Vuforia;

public class ImageTargetHandler : MonoBehaviour
{
    private ObserverBehaviour observerBehaviour;
    public GameObject model;

    void Start()
    {
        observerBehaviour = GetComponent<ObserverBehaviour>();

        if (observerBehaviour)
        {
            observerBehaviour.OnTargetStatusChanged += OnStatusChanged;
        }
    }

    void OnStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        if (status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED)
        {
            model.SetActive(true);
        }
        else
        {
            model.SetActive(false);
        }
    }
}