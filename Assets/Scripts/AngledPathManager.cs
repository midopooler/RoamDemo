using UnityEngine;
using System.Collections.Generic;

public class AngledPathManager : MonoBehaviour
{
    public GameObject startPrefab;    // Point A
    public GameObject endPrefab;      // Point B
    
    public GameObject cube1Prefab;    // 30x10x10
    public GameObject cube2Prefab;    // 30x10x20
    public GameObject cube3Prefab;    // 10x10x40

    [Range(1, 2)]
    public int numberOfTurns = 1;     // Number of right angle turns
    
    private GameObject startInstance;
    private GameObject endInstance;
    private List<GameObject> pathObjects = new List<GameObject>();
    
    private Vector3 startPosition = new Vector3(-75f, 5f, 0f);     // Point A
    private Vector3 endPosition = new Vector3(75f, 5f, 0f);        // Point B
    private Vector3 cornerPosition1;                               // Point P1
    private Vector3 cornerPosition2;                               // Point P2

    void Start()
    {
        CalculateCornerPositions();
        InitializeStartAndEnd();
        GeneratePath();
    }

    void CalculateCornerPositions()
    {
        float minZ = 30f;  // Minimum height for turns
        float maxZ = 75f;  // Maximum height for turns
        float minX = startPosition.x + 30f;  // Minimum X for first turn
        float maxX = endPosition.x - 30f;    // Maximum X for last turn

        if (numberOfTurns == 1)
        {
            // Randomly choose left or right turn
            bool turnLeft = Random.value > 0.5f;
            float turnX = turnLeft ? minX : maxX;
            float randomZ = Random.Range(minZ, maxZ);
            cornerPosition1 = new Vector3(turnX, 5f, randomZ);
        }
        else // numberOfTurns == 2
        {
            // First turn
            float firstTurnX = Random.Range(minX, maxX - 30f); // Leave space for second turn
            float firstTurnZ = Random.Range(minZ, maxZ);
            cornerPosition1 = new Vector3(firstTurnX, 5f, firstTurnZ);

            // Second turn
            float secondTurnX = Random.Range(firstTurnX + 30f, maxX);
            float secondTurnZ = Random.Range(minZ, maxZ);
            cornerPosition2 = new Vector3(secondTurnX, 5f, secondTurnZ);
        }
    }

    void InitializeStartAndEnd()
    {
        if (startInstance != null) Destroy(startInstance);
        if (endInstance != null) Destroy(endInstance);
        
        startInstance = Instantiate(startPrefab, startPosition, Quaternion.identity);
        endInstance = Instantiate(endPrefab, endPosition, Quaternion.identity);
    }

    void GeneratePath()
    {
        foreach (GameObject obj in pathObjects)
        {
            Destroy(obj);
        }
        pathObjects.Clear();

        if (numberOfTurns == 1)
        {
            // Generate path with one turn
            GenerateVerticalSegment(startPosition, new Vector3(startPosition.x, 5f, cornerPosition1.z), true);
            GenerateHorizontalSegment(new Vector3(startPosition.x, 5f, cornerPosition1.z), 
                                    new Vector3(cornerPosition1.x, 5f, cornerPosition1.z));
            GenerateVerticalSegment(new Vector3(cornerPosition1.x, 5f, cornerPosition1.z), 
                                    new Vector3(cornerPosition1.x, 5f, endPosition.z), false);
            GenerateHorizontalSegment(new Vector3(cornerPosition1.x, 5f, endPosition.z), endPosition);
        }
        else // numberOfTurns == 2
        {
            // First vertical segment
            GenerateVerticalSegment(startPosition, new Vector3(startPosition.x, 5f, cornerPosition1.z), true);
            
            // First horizontal segment
            GenerateHorizontalSegment(new Vector3(startPosition.x, 5f, cornerPosition1.z), 
                                    new Vector3(cornerPosition1.x, 5f, cornerPosition1.z));
            
            // Middle vertical segment
            bool goingUp = cornerPosition2.z > cornerPosition1.z;
            GenerateVerticalSegment(new Vector3(cornerPosition1.x, 5f, cornerPosition1.z),
                                  new Vector3(cornerPosition1.x, 5f, cornerPosition2.z), goingUp);
            
            // Second horizontal segment
            GenerateHorizontalSegment(new Vector3(cornerPosition1.x, 5f, cornerPosition2.z),
                                    new Vector3(cornerPosition2.x, 5f, cornerPosition2.z));
            
            // Final vertical segment
            GenerateVerticalSegment(new Vector3(cornerPosition2.x, 5f, cornerPosition2.z),
                                  new Vector3(cornerPosition2.x, 5f, endPosition.z), false);
            
            // Final horizontal segment to end
            GenerateHorizontalSegment(new Vector3(cornerPosition2.x, 5f, endPosition.z), endPosition);
        }
    }

    void GenerateVerticalSegment(Vector3 from, Vector3 to, bool goingUp)
    {
        float currentZ = from.z;
        float targetZ = to.z;
        float direction = goingUp ? 1f : -1f;

        while ((direction > 0 && currentZ < targetZ - 5f) || 
               (direction < 0 && currentZ > targetZ + 5f))
        {
            GameObject prefabToUse;
            float stepSize;

            float remainingDistance = Mathf.Abs(targetZ - currentZ) - 5f;

            int randomChoice;
            if (remainingDistance < 15f)
            {
                randomChoice = 2;
            }
            else if (remainingDistance < 35f)
            {
                randomChoice = Random.Range(0, 2);
            }
            else
            {
                randomChoice = Random.Range(0, 3);
            }

            Vector3 position;
            switch (randomChoice)
            {
                case 0:
                    prefabToUse = cube1Prefab;
                    stepSize = 30f;
                    position = new Vector3(from.x, from.y, currentZ + (direction * 15f));
                    GameObject segment1 = Instantiate(prefabToUse, position, Quaternion.Euler(0f, 90f, 0f));
                    pathObjects.Add(segment1);
                    break;
                case 1:
                    prefabToUse = cube2Prefab;
                    stepSize = 30f;
                    position = new Vector3(from.x, from.y, currentZ + (direction * 15f));
                    GameObject segment2 = Instantiate(prefabToUse, position, Quaternion.Euler(0f, 90f, 0f));
                    pathObjects.Add(segment2);
                    break;
                default:
                    prefabToUse = cube3Prefab;
                    stepSize = 10f;
                    position = new Vector3(from.x, from.y, currentZ + (direction * 5f));
                    GameObject segment3 = Instantiate(prefabToUse, position, Quaternion.Euler(0f, 90f, 0f));
                    pathObjects.Add(segment3);
                    break;
            }
            
            currentZ += direction * stepSize;
        }
    }

    void GenerateHorizontalSegment(Vector3 from, Vector3 to)
    {
        float currentX = from.x + 5f;

        while (currentX < to.x - 5f)
        {
            GameObject prefabToUse;
            float stepSize;

            float remainingDistance = Mathf.Abs(to.x - currentX) - 5f;

            int randomChoice;
            if (remainingDistance < 15f)
            {
                randomChoice = 2;
            }
            else if (remainingDistance < 35f)
            {
                randomChoice = Random.Range(0, 2);
            }
            else
            {
                randomChoice = Random.Range(0, 3);
            }

            switch (randomChoice)
            {
                case 0:
                    prefabToUse = cube1Prefab;
                    stepSize = 30f;
                    Vector3 pos1 = new Vector3(currentX + 15f, from.y, from.z);
                    GameObject segment1 = Instantiate(prefabToUse, pos1, Quaternion.identity);
                    pathObjects.Add(segment1);
                    break;
                case 1:
                    prefabToUse = cube2Prefab;
                    stepSize = 30f;
                    Vector3 pos2 = new Vector3(currentX + 15f, from.y, from.z);
                    GameObject segment2 = Instantiate(prefabToUse, pos2, Quaternion.identity);
                    pathObjects.Add(segment2);
                    break;
                default:
                    prefabToUse = cube3Prefab;
                    stepSize = 10f;
                    Vector3 pos3 = new Vector3(currentX + 5f, from.y, from.z);
                    GameObject segment3 = Instantiate(prefabToUse, pos3, Quaternion.identity);
                    pathObjects.Add(segment3);
                    break;
            }
            
            currentX += stepSize;
        }
    }

    void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 150, 50), "Regenerate Path"))
        {
            CalculateCornerPositions();
            GeneratePath();
        }
    }
} 