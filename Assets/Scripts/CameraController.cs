using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Zoom Settings")]
    [SerializeField] private float minZoomHeight = 10f;
    [SerializeField] private float maxZoomHeight = 50f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float smoothTime = 0.3f;

    private float targetHeight;
    private float currentVelocity;
    private Vector2 touchStart1, touchStart2;
    private float initialPinchDistance;
    private float initialHeight;

    private void Start()
    {
        targetHeight = transform.position.y;
        initialHeight = transform.position.y;
    }

    private void Update()
    {
        // Handle mouse scroll wheel input
        if (Input.mouseScrollDelta.y != 0)
        {
            float scrollInput = Input.mouseScrollDelta.y;
            targetHeight = Mathf.Clamp(targetHeight - scrollInput * zoomSpeed, minZoomHeight, maxZoomHeight);
        }

        // Handle touch input (pinch to zoom)
        if (Input.touchCount == 2)
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
            {
                touchStart1 = touch1.position;
                touchStart2 = touch2.position;
                initialPinchDistance = Vector2.Distance(touchStart1, touchStart2);
                initialHeight = transform.position.y;
            }
            else if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
            {
                float currentPinchDistance = Vector2.Distance(touch1.position, touch2.position);
                float pinchDelta = (initialPinchDistance - currentPinchDistance) * 0.01f * zoomSpeed;
                
                targetHeight = Mathf.Clamp(initialHeight + pinchDelta, minZoomHeight, maxZoomHeight);
            }
        }

        // Smoothly update camera height
        Vector3 currentPosition = transform.position;
        currentPosition.y = Mathf.SmoothDamp(currentPosition.y, targetHeight, ref currentVelocity, smoothTime);
        transform.position = currentPosition;
    }

    // Public method to set camera height directly (useful for reset functionality)
    public void SetCameraHeight(float height)
    {
        targetHeight = Mathf.Clamp(height, minZoomHeight, maxZoomHeight);
        Vector3 pos = transform.position;
        pos.y = targetHeight;
        transform.position = pos;
    }
} 