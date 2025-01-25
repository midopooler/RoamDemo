using UnityEngine;
using System.Collections.Generic;

public class StraightPathManager : MonoBehaviour
{
    public GameObject startPrefab;
    public GameObject endPrefab;
    
    public GameObject cube1Prefab; // 30x10x10
    public GameObject cube2Prefab; // 30x10x20
    public GameObject cube3Prefab; // 10x10x40
    
    private GameObject startInstance;
    private GameObject endInstance;
    private List<GameObject> pathObjects = new List<GameObject>();
    
    private Vector3 startPosition = new Vector3(-75f, 0f, 0f);
    private Vector3 endPosition = new Vector3(75f, 0f, 0f);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitializeStartAndEnd();
        GeneratePath();
    }

    void InitializeStartAndEnd()
    {
        // Clean up existing instances if any
        if (startInstance != null) Destroy(startInstance);
        if (endInstance != null) Destroy(endInstance);
        
        // Create start and end points with y = 5f
        Vector3 startPos = new Vector3(startPosition.x, 5f, startPosition.z);
        Vector3 endPos = new Vector3(endPosition.x, 5f, endPosition.z);
        startInstance = Instantiate(startPrefab, startPos, Quaternion.identity);
        endInstance = Instantiate(endPrefab, endPos, Quaternion.identity);
    }

    void GeneratePath()
    {
        // Clean up existing path
        foreach (GameObject obj in pathObjects)
        {
            Destroy(obj);
        }
        pathObjects.Clear();

        float currentX = startPosition.x + 5f; // Start after half of start prefab
        float endX = endPosition.x - 5f; // End before half of end prefab

        while (currentX < endX)
        {
            GameObject prefabToUse;
            float stepSize;
            Vector3 position;

            // Calculate remaining distance
            float remainingDistance = endX - currentX;

            // Choose prefab based on remaining distance
            int randomChoice;
            if (remainingDistance < 15f)
            {
                randomChoice = 2; // Force cube3 (10x10x40)
            }
            else if (remainingDistance < 35f)
            {
                randomChoice = Random.Range(0, 2); // Force either cube1 or cube2 (30-unit prefabs)
            }
            else
            {
                randomChoice = Random.Range(0, 3); // Any prefab can be used
            }

            switch (randomChoice)
            {
                case 0:
                    prefabToUse = cube1Prefab; // 30x10x10
                    position = new Vector3(currentX + 15f, 5f, 0f); // Changed Y to 5f
                    stepSize = 30f;
                    break;
                case 1:
                    prefabToUse = cube2Prefab; // 30x10x20
                    position = new Vector3(currentX + 15f, 5f, 0f); // Changed Y to 5f
                    stepSize = 30f;
                    break;
                default:
                    prefabToUse = cube3Prefab; // 10x10x40
                    position = new Vector3(currentX + 5f, 5f, 0f); // Changed Y to 5f
                    stepSize = 10f;
                    break;
            }

            GameObject pathSegment = Instantiate(prefabToUse, position, Quaternion.identity);
            pathObjects.Add(pathSegment);
            
            currentX += stepSize;
        }
    }

    void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 150, 50), "Regenerate Path"))
        {
            GeneratePath();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
