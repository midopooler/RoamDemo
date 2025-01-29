using UnityEngine;
using UnityEditor;

public class CreatePrefabs : EditorWindow
{
    [MenuItem("Tools/Level Designer/Create Default Prefabs")]
    static void CreateDefaultPrefabs()
    {
        // Create Prefabs folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        // Create Cuboid
        GameObject cuboid = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cuboid.name = "Cuboid";
        cuboid.transform.localScale = new Vector3(2f, 1f, 1f); // 2x1x1 ratio
        
        // Create Cylinder
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = "Cylinder";
        cylinder.transform.localScale = new Vector3(1f, 0.5f, 1f); // Height is half the diameter

        // Save prefabs
        string cuboidPath = "Assets/Prefabs/Cuboid.prefab";
        string cylinderPath = "Assets/Prefabs/Cylinder.prefab";

        // Delete existing prefabs if they exist
        if (AssetDatabase.LoadAssetAtPath<GameObject>(cuboidPath) != null)
            AssetDatabase.DeleteAsset(cuboidPath);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(cylinderPath) != null)
            AssetDatabase.DeleteAsset(cylinderPath);

        // Create new prefabs
        PrefabUtility.SaveAsPrefabAsset(cuboid, cuboidPath);
        PrefabUtility.SaveAsPrefabAsset(cylinder, cylinderPath);

        // Clean up scene objects
        DestroyImmediate(cuboid);
        DestroyImmediate(cylinder);

        Debug.Log("Created default prefabs in Assets/Prefabs folder");
    }

    [MenuItem("Tools/Level Designer/Create Custom Prefab")]
    static void ShowWindow()
    {
        GetWindow<CreatePrefabs>("Create Custom Prefab");
    }

    private PrimitiveType primitiveType = PrimitiveType.Cube;
    private Vector3 scale = Vector3.one;
    private string prefabName = "CustomPrefab";

    void OnGUI()
    {
        GUILayout.Label("Custom Prefab Creator", EditorStyles.boldLabel);

        primitiveType = (PrimitiveType)EditorGUILayout.EnumPopup("Primitive Type", primitiveType);
        scale = EditorGUILayout.Vector3Field("Scale", scale);
        prefabName = EditorGUILayout.TextField("Prefab Name", prefabName);

        if (GUILayout.Button("Create Prefab"))
        {
            CreateCustomPrefab();
        }
    }

    void CreateCustomPrefab()
    {
        // Create Prefabs folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        // Create primitive
        GameObject obj = GameObject.CreatePrimitive(primitiveType);
        obj.name = prefabName;
        obj.transform.localScale = scale;

        // Save prefab
        string prefabPath = $"Assets/Prefabs/{prefabName}.prefab";

        // Delete existing prefab if it exists
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            if (!EditorUtility.DisplayDialog("Overwrite Prefab?", 
                $"Prefab {prefabName} already exists. Do you want to overwrite it?", "Yes", "No"))
            {
                DestroyImmediate(obj);
                return;
            }
            AssetDatabase.DeleteAsset(prefabPath);
        }

        // Create new prefab
        PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);

        // Clean up scene object
        DestroyImmediate(obj);

        Debug.Log($"Created prefab: {prefabPath}");
    }
} 