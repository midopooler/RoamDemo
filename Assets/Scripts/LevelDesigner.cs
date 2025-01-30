using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LevelDesigner : MonoBehaviour
{
    [Header("Spline Settings")]
    public SplineContainer splineContainer;
    [Min(1f)]
    public float minimumKnotDistance = 1f;
    [Range(2, 25)]
    public int maximumKnots = 25;

    [Header("Prefabs")]
    public GameObject cuboidPrefab;
    public GameObject cylinderPrefab;

    [Header("UI Settings")]
    public float buttonSize = 60f;
    public float spacing = 10f;

    private List<GameObject> placedObjects = new List<GameObject>();
    private Stack<LevelAction> undoStack = new Stack<LevelAction>();
    private Stack<LevelAction> redoStack = new Stack<LevelAction>();
    
    private bool isAddingKnot = false;
    private bool isMovingKnot = false;
    private int selectedKnotIndex = -1;
    private bool isCuboidSelected = true;
    private Camera mainCamera;
    private bool isPlacingObject = false;
    private bool isDeleteMode = false;

    private void Start()
    {
        mainCamera = Camera.main;
        InitializeSpline();
    }

    private void InitializeSpline()
    {
        if (splineContainer == null)
        {
            GameObject splineObj = new GameObject("LevelSpline");
            splineContainer = splineObj.AddComponent<SplineContainer>();
            splineContainer.Spline.Add(new BezierKnot(new float3(0, 0, 0)));
            splineContainer.Spline.Add(new BezierKnot(new float3(5, 0, 0)));
        }
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0) && !IsMouseOverUI())
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (isDeleteMode)
                {
                    // Check if we hit a placed object
                    if (placedObjects.Contains(hit.collider.gameObject))
                    {
                        DeleteObject(hit.collider.gameObject);
                        isDeleteMode = false; // Turn off delete mode after successful deletion
                        UpdateButtonColors();
                        return;
                    }
                    // Check if we hit a knot
                    Vector3 hitPoint = hit.point;
                    for (int i = 0; i < splineContainer.Spline.Count; i++)
                    {
                        if (Vector3.Distance(hitPoint, splineContainer.Spline[i].Position) < 1f)
                        {
                            DeleteKnot(i);
                            isDeleteMode = false; // Turn off delete mode after successful deletion
                            UpdateButtonColors();
                            return;
                        }
                    }
                }
                else if (hit.collider.gameObject.CompareTag("Ground"))
                {
                    if (isAddingKnot)
                    {
                        AddKnotAtPosition(hit.point);
                    }
                    else if (!isMovingKnot)
                    {
                        TrySelectKnot(hit.point);
                    }
                }
            }
        }

        if (Input.GetMouseButton(0) && isMovingKnot && selectedKnotIndex != -1)
        {
            MoveSelectedKnot();
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (isMovingKnot && selectedKnotIndex != -1)
            {
                FinishMovingKnot();
            }
            isMovingKnot = false;
            selectedKnotIndex = -1;
        }
    }

    private bool IsMouseOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }

    private void AddKnotAtPosition(Vector3 position)
    {
        if (splineContainer.Spline.Count >= maximumKnots)
        {
            Debug.LogWarning($"Cannot add more knots. Maximum ({maximumKnots}) reached.");
            return;
        }

        Debug.Log($"Checking distances from position {position}");
        // Check minimum distance from other knots
        for (int i = 0; i < splineContainer.Spline.Count; i++)
        {
            float distance = Vector3.Distance(position, splineContainer.Spline[i].Position);
            Debug.Log($"Distance to knot {i}: {distance}");
            if (distance < minimumKnotDistance)
            {
                Debug.LogWarning($"Too close to existing knot {i}. Distance: {distance}");
                return;
            }
        }

        position.y = 0;
        var newKnot = new BezierKnot(new float3(position.x, position.y, position.z));
        splineContainer.Spline.Add(newKnot);
        Debug.Log($"Added new knot at position {position}. Total knots: {splineContainer.Spline.Count}");
        
        RecordAction(new AddKnotAction(splineContainer.Spline.Count - 1, newKnot));
    }

    private void TrySelectKnot(Vector3 position)
    {
        for (int i = 0; i < splineContainer.Spline.Count; i++)
        {
            if (Vector3.Distance(position, splineContainer.Spline[i].Position) < 1f)
            {
                selectedKnotIndex = i;
                isMovingKnot = true;
                break;
            }
        }
    }

    private void MoveSelectedKnot()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit) && hit.collider.gameObject.CompareTag("Ground"))
        {
            Vector3 newPosition = hit.point;
            
            // Check minimum distance from other knots
            for (int i = 0; i < splineContainer.Spline.Count; i++)
            {
                if (i != selectedKnotIndex && 
                    Vector3.Distance(newPosition, splineContainer.Spline[i].Position) < minimumKnotDistance)
                    return;
            }

            var knot = splineContainer.Spline[selectedKnotIndex];
            knot.Position = new float3(newPosition.x, 0, newPosition.z);
            splineContainer.Spline[selectedKnotIndex] = knot;
        }
    }

    private void FinishMovingKnot()
    {
        RecordAction(new MoveKnotAction(selectedKnotIndex, splineContainer.Spline[selectedKnotIndex]));
    }
    public void PlaceCuboidPrefab()
    {
         Instantiate(cuboidPrefab, new Vector3(0,0,-10F) , Quaternion.identity);
    }
    public void PlaceCylinderPrefab()
    {
         Instantiate(cylinderPrefab, new Vector3(0,0,-10F) , Quaternion.identity);
    }

    private void PlaceObject(Vector3 position)
    {
        // Clear existing objects first
        foreach (GameObject obj in placedObjects)
        {
            Destroy(obj);
        }
        placedObjects.Clear();

        // Place objects at all knots
        for (int i = 0; i < splineContainer.Spline.Count; i++)
        {
            float3 knotPos = splineContainer.Spline[i].Position;
            Vector3 placementPos = new Vector3(knotPos.x, 0f, knotPos.z);
            
            // Use cylinder for first and last knots, cuboid for middle knots
            GameObject prefab;
            if (i == 0 || i == splineContainer.Spline.Count - 1)
            {
                prefab = cylinderPrefab;
            }
            else
            {
                prefab = cuboidPrefab;
            }

            GameObject obj = Instantiate(prefab, placementPos, Quaternion.identity);
            
            // Get direction for rotation
            float3 direction;
            if (i < splineContainer.Spline.Count - 1)
            {
                // If not last knot, point towards next knot
                direction = splineContainer.Spline[i + 1].Position - knotPos;
            }
            else if (i > 0)
            {
                // If last knot, use direction from previous knot
                direction = knotPos - splineContainer.Spline[i - 1].Position;
            }
            else
            {
                direction = new float3(1, 0, 0);
            }

            Vector3 directionVector = new Vector3(direction.x, 0, direction.z).normalized;
            if (directionVector != Vector3.zero)
            {
                // Add 90 degrees to Y rotation
                Quaternion rotation = Quaternion.LookRotation(directionVector);
                Vector3 eulerAngles = rotation.eulerAngles;
                eulerAngles.y += 90f;
                obj.transform.rotation = Quaternion.Euler(eulerAngles);
            }

            placedObjects.Add(obj);
        }

        // Record single action for all objects
        RecordAction(new PlaceAllObjectsAction(placedObjects));
    }

    private void RecordAction(LevelAction action)
    {
        undoStack.Push(action);
        redoStack.Clear();
    }

    public void Undo()
    {
        if (undoStack.Count > 0)
        {
            LevelAction action = undoStack.Pop();
            action.Undo(splineContainer, placedObjects);
            redoStack.Push(action);
        }
    }

    public void Redo()
    {
        if (redoStack.Count > 0)
        {
            LevelAction action = redoStack.Pop();
            action.Redo(splineContainer, placedObjects);
            undoStack.Push(action);
        }
    }

    private void Awake()
    {
        // Find and set up button references
        SetupButtonListeners();
    }

    private void SetupButtonListeners()
    {
        // Find buttons by name and add listeners
        Button[] buttons = FindObjectsOfType<Button>();
        foreach (Button button in buttons)
        {
            switch (button.gameObject.name)
            {
                case "Add KnotButton":
                    button.onClick.AddListener(() => {
                        isAddingKnot = !isAddingKnot;
                        isPlacingObject = false;
                        Debug.Log($"Add Knot mode: {isAddingKnot}");
                        UpdateButtonColors();
                    });
                    break;

                case "Cuboid/CylinderButton":
                    button.onClick.AddListener(() => {
                        isCuboidSelected = !isCuboidSelected;
                        Debug.Log($"Selected object type: {(isCuboidSelected ? "Cuboid" : "Cylinder")}");
                        UpdateButtonColors();
                    });
                    break;

                case "Place ObjectButton":
                    button.onClick.AddListener(() => {
                        isPlacingObject = !isPlacingObject;
                        isAddingKnot = false;
                        Debug.Log($"Place Object mode: {isPlacingObject}");
                        if (isPlacingObject)
                        {
                            PlaceObject(Vector3.zero); // Position doesn't matter anymore
                        }
                        UpdateButtonColors();
                    });
                    break;

                case "UndoButton":
                    button.onClick.AddListener(Undo);
                    break;

                case "RedoButton":
                    button.onClick.AddListener(Redo);
                    break;

                case "DeleteButton":
                    button.onClick.AddListener(ToggleDeleteMode);
                    break;
            }
        }
    }

    private void UpdateButtonColors()
    {
        Button[] buttons = FindObjectsOfType<Button>();
        foreach (Button button in buttons)
        {
            ColorBlock colors = button.colors;
            switch (button.gameObject.name)
            {
                case "Add KnotButton":
                    colors.normalColor = isAddingKnot ? Color.cyan : Color.white;
                    break;
                case "Cuboid/CylinderButton":
                    colors.normalColor = isCuboidSelected ? Color.yellow : Color.white;
                    break;
                case "Place ObjectButton":
                    colors.normalColor = isPlacingObject ? Color.green : Color.white;
                    break;
                case "DeleteButton":
                    colors.normalColor = isDeleteMode ? Color.red : Color.white;
                    break;
            }
            button.colors = colors;
        }
    }

    public void ToggleDeleteMode()
    {
        isDeleteMode = !isDeleteMode;
        Debug.Log($"Delete mode: {isDeleteMode}");
        UpdateButtonColors(); // Update the delete button color to show active state
    }

    private void DeleteObject(GameObject obj)
    {
        placedObjects.Remove(obj);
        Destroy(obj);
        RecordAction(new DeleteObjectAction(obj));
        Debug.Log("Object deleted");
    }

    private void DeleteKnot(int index)
    {
        if (splineContainer.Spline.Count <= 2)
        {
            Debug.LogWarning("Cannot delete knot: Minimum 2 knots required");
            return;
        }
        
        var deletedKnot = splineContainer.Spline[index];
        splineContainer.Spline.RemoveAt(index);
        RecordAction(new DeleteKnotAction(index, deletedKnot));
        Debug.Log($"Knot {index} deleted");
        
        // Refresh placed objects to update the path
        if (placedObjects.Count > 0)
        {
            PlaceObject(Vector3.zero);
        }
    }
} 