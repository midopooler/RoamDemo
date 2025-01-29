using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class CreateLevelDesignerUI : EditorWindow
{
    [MenuItem("Tools/Level Designer/Create UI Canvas")]
    static void CreateUICanvas()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("LevelDesignerCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Create left panel for main controls
        GameObject leftPanelObj = new GameObject("LeftButtonPanel");
        RectTransform leftPanelRect = leftPanelObj.AddComponent<RectTransform>();
        leftPanelObj.transform.SetParent(canvasObj.transform, false);
        
        VerticalLayoutGroup leftVerticalLayout = leftPanelObj.AddComponent<VerticalLayoutGroup>();
        leftVerticalLayout.padding = new RectOffset(10, 10, 10, 10);
        leftVerticalLayout.spacing = 10;
        leftVerticalLayout.childAlignment = TextAnchor.MiddleCenter;
        leftVerticalLayout.childControlHeight = false;
        leftVerticalLayout.childForceExpandHeight = false;
        
        // Position the left panel
        leftPanelRect.anchorMin = new Vector2(0, 0);
        leftPanelRect.anchorMax = new Vector2(0.15f, 0.5f);
        leftPanelRect.pivot = new Vector2(0, 0);
        leftPanelRect.anchoredPosition = new Vector2(10, 10);

        // Create right panel for edit controls
        GameObject rightPanelObj = new GameObject("RightButtonPanel");
        RectTransform rightPanelRect = rightPanelObj.AddComponent<RectTransform>();
        rightPanelObj.transform.SetParent(canvasObj.transform, false);
        
        VerticalLayoutGroup rightVerticalLayout = rightPanelObj.AddComponent<VerticalLayoutGroup>();
        rightVerticalLayout.padding = new RectOffset(10, 10, 10, 10);
        rightVerticalLayout.spacing = 10;
        rightVerticalLayout.childAlignment = TextAnchor.MiddleCenter;
        rightVerticalLayout.childControlHeight = false;
        rightVerticalLayout.childForceExpandHeight = false;
        
        // Position the right panel
        rightPanelRect.anchorMin = new Vector2(0.85f, 0);
        rightPanelRect.anchorMax = new Vector2(1f, 0.5f);
        rightPanelRect.pivot = new Vector2(1, 0);
        rightPanelRect.anchoredPosition = new Vector2(-10, 10);

        // Create buttons for left panel (Main Controls)
        CreateButton("Add Knot", leftPanelObj);
        CreateButton("Move Knot", leftPanelObj);
        CreateButton("Delete Knot", leftPanelObj);
        CreateButton("Clear All", leftPanelObj);
        CreateSeparator(leftPanelObj);
        CreateButton("Cuboid/Cylinder", leftPanelObj);
        CreateButton("Place Object", leftPanelObj);
        CreateButton("Delete Object", leftPanelObj);
        CreateSeparator(leftPanelObj);
        CreateButton("Save Path", leftPanelObj);
        CreateButton("Load Path", leftPanelObj);

        // Create buttons for right panel (Edit Controls)
        CreateButton("Undo", rightPanelObj);
        CreateButton("Redo", rightPanelObj);
        CreateSeparator(rightPanelObj);
        CreateButton("Test Path", rightPanelObj);
        CreateButton("Reset Camera", rightPanelObj);

        // Find or create EventSystem
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        Debug.Log("UI Canvas created successfully!");
    }

    static void CreateSeparator(GameObject parent)
    {
        GameObject sepObj = new GameObject("Separator");
        sepObj.transform.SetParent(parent.transform, false);

        RectTransform rectTransform = sepObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(0, 2);

        Image image = sepObj.AddComponent<Image>();
        image.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    }

    static GameObject CreateButton(string buttonText, GameObject parent)
    {
        GameObject buttonObj = new GameObject(buttonText + "Button");
        buttonObj.transform.SetParent(parent.transform, false);

        // Add RectTransform first
        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
        
        // Add button component
        Button button = buttonObj.AddComponent<Button>();
        rectTransform.sizeDelta = new Vector2(0, 60);

        // Create background image
        Image image = buttonObj.AddComponent<Image>();
        button.targetGraphic = image;

        // Create text object
        GameObject textObj = new GameObject("Text");
        textObj.AddComponent<RectTransform>(); // Add RectTransform to text object too
        textObj.transform.SetParent(buttonObj.transform, false);
        
        Text text = textObj.AddComponent<Text>();
        text.text = buttonText;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;
        text.fontSize = 14;

        // Set up text RectTransform to fill button
        RectTransform textRectTransform = textObj.GetComponent<RectTransform>();
        textRectTransform.anchorMin = Vector2.zero;
        textRectTransform.anchorMax = Vector2.one;
        textRectTransform.sizeDelta = Vector2.zero;

        return buttonObj;
    }
} 