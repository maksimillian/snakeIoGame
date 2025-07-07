using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TestUIManager : MonoBehaviour
{
    [Header("UI Settings")]
    public bool createTestButtonOnStart = true;
    public Vector2 buttonPosition = new Vector2(100, 100);
    public Vector2 buttonSize = new Vector2(200, 50);

    [Header("Button Style")]
    public Color primaryColor = new Color(0.2f, 0.6f, 1f, 1f);
    public Color secondaryColor = new Color(0.1f, 0.4f, 0.8f, 1f);
    public Color hoverColor = new Color(0.3f, 0.7f, 1f, 1f);
    public Color pressedColor = new Color(0.1f, 0.5f, 0.9f, 1f);
    public Color textColor = Color.white;
    public int fontSize = 16;

    private GameObject testButtonObject;
    private TestRoomJoinButton testRoomJoinButton;

    private void Start()
    {
        if (createTestButtonOnStart)
        {
            CreateTestRoomJoinButton();
        }
    }

    [ContextMenu("Create Test Room Join Button")]
    public void CreateTestRoomJoinButton()
    {
        // Check if button already exists
        if (testButtonObject != null)
        {
            Debug.LogWarning("Test button already exists!");
            return;
        }

        // Create Canvas if it doesn't exist
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Test Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        // Create main button container
        testButtonObject = new GameObject("Test Room Join Button");
        testButtonObject.transform.SetParent(canvas.transform, false);

        // Add Button component
        Button button = testButtonObject.AddComponent<Button>();
        Image buttonImage = testButtonObject.AddComponent<Image>();
        
        // Create modern button appearance
        buttonImage.color = primaryColor;
        buttonImage.type = Image.Type.Sliced;
        
        // Add shadow for depth
        Shadow shadow = testButtonObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.3f);
        shadow.effectDistance = new Vector2(2, -2);

        // Set button position and size
        RectTransform rectTransform = testButtonObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = buttonPosition;
        rectTransform.sizeDelta = buttonSize;

        // Create button text
        GameObject textObject = new GameObject("Button Text");
        textObject.transform.SetParent(testButtonObject.transform, false);
        
        TextMeshProUGUI buttonText = textObject.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Join Test Room";
        buttonText.color = textColor;
        buttonText.fontSize = fontSize;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.enableWordWrapping = false;
        buttonText.overflowMode = TextOverflowModes.Overflow;

        // Set text position and size
        RectTransform textRectTransform = textObject.GetComponent<RectTransform>();
        textRectTransform.anchorMin = Vector2.zero;
        textRectTransform.anchorMax = Vector2.one;
        textRectTransform.offsetMin = Vector2.zero;
        textRectTransform.offsetMax = Vector2.zero;

        // Set up button colors
        ColorBlock colors = button.colors;
        colors.normalColor = primaryColor;
        colors.highlightedColor = hoverColor;
        colors.pressedColor = pressedColor;
        colors.selectedColor = secondaryColor;
        colors.fadeDuration = 0.1f;
        button.colors = colors;

        // Add TestRoomJoinButton component
        testRoomJoinButton = testButtonObject.AddComponent<TestRoomJoinButton>();
        testRoomJoinButton.playButton = button;

        Debug.Log("Modern Test Room Join Button created successfully!");
    }

    [ContextMenu("Destroy Test Button")]
    public void DestroyTestButton()
    {
        if (testButtonObject != null)
        {
            DestroyImmediate(testButtonObject);
            testButtonObject = null;
            testRoomJoinButton = null;
            Debug.Log("Test button destroyed!");
        }
    }

    private void OnDestroy()
    {
        // Clean up button when this manager is destroyed
        if (testButtonObject != null)
        {
            Destroy(testButtonObject);
        }
    }
} 