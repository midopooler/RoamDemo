using Battlehub.RTHandles.Demo;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Battlehub.RTEditor.Demo
{
    public static class RuntimeHandlesMenu
    {
        public static GameObject InstantiatePrefab(string name)
        {
            string path = BHRoot.PackageRuntimeContentPath + "/RTHandles/Prefabs/" + name;
            Object prefab = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject));
            if (prefab == null)
            {
                throw new FileNotFoundException($"{path} does not exist.");
            }
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.SetActive(true);
            return go;
        }

        [MenuItem("Tools/Runtime Handles/Show me examples", priority = -1000)]
        public static void ShowMeExamples()
        {
            EditorUtility.FocusProjectWindow();

            const string path = "Assets/Battlehub/RTEditorDemo/Content/Runtime/RTHandles/RTHandles.unity";

            var obj = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }


        [MenuItem("Tools/Runtime Handles/Create Transform Handles UI", priority = 15)]
        public static void CreateTransfomrHandlesUI()
        {
            var demoEditor = Object.FindAnyObjectByType<SimpleEditor>(FindObjectsInactive.Include);
            if (demoEditor != null)
            {
                Selection.activeGameObject = demoEditor.gameObject;
                EditorGUIUtility.PingObject(Selection.activeGameObject);
            }
            else
            {
                GameObject go = InstantiatePrefab("SimpleTransformHandlesUI.prefab");

                if(!Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include))
                {
                    GameObject eventSystem = new GameObject("Event System");
                    eventSystem.AddComponent<EventSystem>();
                    if (eventSystem.GetComponent<StandaloneInputModule>() == null)
                    {
                        eventSystem.AddComponent<StandaloneInputModule>();
                    }
                    Undo.RegisterCreatedObjectUndo(eventSystem, "Event System");
                }

                Undo.RegisterCreatedObjectUndo(go, "Transform Handles UI");
            }
        }

        [MenuItem("Tools/Runtime Handles/Create Demo Editor", priority = 16)]
        public static void CreateDemoEditor()
        {
            var demoEditor = Object.FindAnyObjectByType<RTE>(FindObjectsInactive.Include);
            if (demoEditor != null)
            {
                EditorUtility.DisplayDialog("Can't create demo editor", "Another editor already exists in the scene", "OK");
                Selection.activeGameObject = demoEditor.gameObject;
                EditorGUIUtility.PingObject(Selection.activeGameObject);
            }
            else
            {
                if (!Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include))
                {
                    GameObject eventSystem = new GameObject("Event System");
                    eventSystem.AddComponent<EventSystem>();
                    if (eventSystem.GetComponent<StandaloneInputModule>() == null)
                    {
                        eventSystem.AddComponent<StandaloneInputModule>();
                    }
                    Undo.RegisterCreatedObjectUndo(eventSystem, "Event System");
                }

                GameObject go = InstantiatePrefab("DemoEditor.prefab");
                Undo.RegisterCreatedObjectUndo(go, "Demo Editor");
            }
        }

    }
}
