using UnityEngine;
using UnityEngine.EventSystems;

public class ARGestureController : MonoBehaviour
{
    public float rotationSpeed = 0.2f;
    public float scaleSpeed = 0.01f;
    public float minScale = 0.1f;
    public float maxScale = 5f;

    void Update()
    {
        if (Input.touchCount == 1)
        {
            RotateObject();
        }
        else if (Input.touchCount == 2)
        {
            ScaleObject();
        }
    }

    void RotateObject()
    {
        Touch touch = Input.GetTouch(0);

        // 防止点到UI
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            return;

        if (touch.phase == TouchPhase.Moved)
        {
            float rotX = touch.deltaPosition.y * rotationSpeed;
            float rotY = -touch.deltaPosition.x * rotationSpeed;

            transform.Rotate(rotX, rotY, 0, Space.World);
        }
    }

    void ScaleObject()
    {
        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);

        Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
        Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

        float prevDistance = Vector2.Distance(touch0PrevPos, touch1PrevPos);
        float currentDistance = Vector2.Distance(touch0.position, touch1.position);

        float distanceDifference = currentDistance - prevDistance;

        Vector3 scale = transform.localScale;
        scale += Vector3.one * distanceDifference * scaleSpeed;

        scale.x = Mathf.Clamp(scale.x, minScale, maxScale);
        scale.y = Mathf.Clamp(scale.y, minScale, maxScale);
        scale.z = Mathf.Clamp(scale.z, minScale, maxScale);

        transform.localScale = scale;
    }
}