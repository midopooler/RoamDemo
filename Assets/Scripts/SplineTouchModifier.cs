using UnityEngine;
using UnityEngine.Splines;

public class SplineTouchModifier : MonoBehaviour
{
    public SplineContainer splineContainer; // Reference to your spline container
    public Camera mainCamera;               // Camera to cast ray from

    private int selectedKnotIndex = -1;
    private Vector3 offset;                 // Offset between the cursor and knot position
    private float lockedYPosition;          // Store the initial Y position of the knot

    void Update()
    {
#if UNITY_EDITOR
        HandleMouseInput(); // For testing in the Unity Editor
#else
        HandleTouchInput(); // For actual mobile runtime
#endif
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Select the nearest knot when the mouse button is pressed
            selectedKnotIndex = GetNearestKnot(Input.mousePosition);

            if (selectedKnotIndex != -1)
            {
                // Get the knot's current position and calculate the offset
                Vector3 knotPosition = splineContainer.Spline[selectedKnotIndex].Position;
                Vector3 worldCursorPosition = GetWorldPosition(Input.mousePosition);
                offset = knotPosition - worldCursorPosition;

                // Store the initial Y position of the knot to lock it
                lockedYPosition = knotPosition.y;
            }
        }
        else if (Input.GetMouseButton(0) && selectedKnotIndex != -1)
        {
            // Drag the selected knot while holding the mouse button
            Vector3 worldPos = GetWorldPosition(Input.mousePosition);

            // Apply the offset to the dragged position
            Vector3 newKnotPosition = worldPos + offset;

            // Lock the Y position
            newKnotPosition.y = lockedYPosition;

            // Get the current knot, modify it, and reassign it
            BezierKnot knot = splineContainer.Spline[selectedKnotIndex];
            knot.Position = newKnotPosition;
            splineContainer.Spline[selectedKnotIndex] = knot; // Reassign the modified knot
        }
        else if (Input.GetMouseButtonUp(0))
        {
            // Release the selected knot
            selectedKnotIndex = -1;
        }
    }

    private int GetNearestKnot(Vector3 screenPosition)
    {
        float minDistance = float.MaxValue;
        int nearestIndex = -1;

        Vector3 cursorWorldPosition = GetWorldPosition(screenPosition);

        for (int i = 0; i < splineContainer.Spline.Count; i++)
        {
            Vector3 knotPosition = splineContainer.Spline[i].Position;
            float distance = Vector3.Distance(cursorWorldPosition, knotPosition);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    //private Vector3 GetWorldPosition(Vector3 screenPosition)
    //{
    //    Ray ray = mainCamera.ScreenPointToRay(screenPosition);
    //    if (Physics.Raycast(ray, out RaycastHit hit))
    //    {
    //        return hit.point; // Return the hit point on a collider
    //    }
    //    return Vector3.zero;
    //}
    private Vector3 GetWorldPosition(Vector3 screenPosition)
    {
        // Define a plane at the Y level of the locked position
        Plane plane = new Plane(Vector3.up, new Vector3(0, lockedYPosition, 0));

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        // Check if the ray intersects the plane
        if (plane.Raycast(ray, out float distance))
        {
            // Calculate the world position at the intersection point
            return ray.GetPoint(distance);
        }

        // Fallback: Return a zero vector if no intersection
        return Vector3.zero;
    }
}