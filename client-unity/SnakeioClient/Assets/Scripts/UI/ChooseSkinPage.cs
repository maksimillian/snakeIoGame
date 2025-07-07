using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChooseSkinPage : MonoBehaviour
{
    [Header("UI References")]
    public GameObject skinPreviewPrefab;
    public Transform skinContainer;
    public Button backButton;
    
    [Header("Skin Info Panel")]
    public GameObject skinInfoPanel;
    public TextMeshProUGUI selectedSkinNameText;
    public TextMeshProUGUI selectedSkinDescriptionText;
    public TextMeshProUGUI selectedSkinPriceText;
    public Button selectedSkinActionButton;
    public TextMeshProUGUI selectedSkinButtonText;
    
    [Header("Settings")]
    public float previewSpacing = 10f;
    
    private List<SkinPreviewItem> skinPreviewItems = new List<SkinPreviewItem>();
    private SkinManager skinManager;
    private SnakeSkin currentlySelectedSkin;
    private bool isPageOpen = false;
    
    private void Awake()
    {
        Debug.Log("ChooseSkinPage.Awake() called");
        skinManager = SkinManager.Instance;
        
        // Setup back button
        if (backButton != null)
        {
            backButton.onClick.AddListener(ClosePage);
        }
        
        // Setup action button
        if (selectedSkinActionButton != null)
        {
            selectedSkinActionButton.onClick.AddListener(OnActionButtonClicked);
        }
    }
    
    private void Start()
    {
        Debug.Log("ChooseSkinPage.Start() called");
        // UIManager now handles the initial state, so we don't need to hide the page here
        // The page will be properly managed by UIManager.ShowChooseSkinPanel() and UIManager.ShowMenu()
    }
    
    public void OpenPage()
    {
        Debug.Log("ChooseSkinPage.OpenPage() called");
        
        // Prevent multiple calls
        if (isPageOpen)
        {
            Debug.Log("Page is already open, skipping OpenPage call");
            return;
        }
        
        Debug.Log($"ChooseSkinPage GameObject name: {gameObject.name}");
        Debug.Log($"ChooseSkinPage GameObject active before: {gameObject.activeInHierarchy}");
        
        // Ensure the page is active first
        gameObject.SetActive(true);
        Debug.Log($"ChooseSkinPage GameObject active after: {gameObject.activeInHierarchy}");
        
        // Check if parent objects are active
        Transform parent = transform.parent;
        while (parent != null)
        {
            Debug.Log($"Parent '{parent.name}' active: {parent.gameObject.activeInHierarchy}");
            parent = parent.parent;
        }
        
        // Check UI element references
        Debug.Log($"skinPreviewPrefab: {(skinPreviewPrefab != null ? "Set" : "NULL")}");
        Debug.Log($"skinContainer: {(skinContainer != null ? "Set" : "NULL")}");
        Debug.Log($"backButton: {(backButton != null ? "Set" : "NULL")}");
        Debug.Log($"skinInfoPanel: {(skinInfoPanel != null ? "Set" : "NULL")}");
        
        // Check if skinContainer is active
        if (skinContainer != null)
        {
            Debug.Log($"skinContainer active: {skinContainer.gameObject.activeInHierarchy}");
        }
        
        // Check if backButton is active
        if (backButton != null)
        {
            Debug.Log($"backButton active: {backButton.gameObject.activeInHierarchy}");
        }
        
        // Check Canvas and positioning
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Debug.Log($"Canvas found: {canvas.name}");
            Debug.Log($"Canvas render mode: {canvas.renderMode}");
            Debug.Log($"Canvas sort order: {canvas.sortingOrder}");
        }
        else
        {
            Debug.LogError("No Canvas found in parent hierarchy!");
        }
        
        // Check RectTransform positioning
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            Debug.Log($"RectTransform position: {rectTransform.position}");
            Debug.Log($"RectTransform size: {rectTransform.sizeDelta}");
            Debug.Log($"RectTransform anchors: {rectTransform.anchorMin} to {rectTransform.anchorMax}");
        }
        
        // Force layout update to ensure everything is properly initialized
        Canvas.ForceUpdateCanvases();
        
        // Clear any existing previews first
        ClearSkinPreviews();
        
        // Refresh the skin list
        RefreshSkinList();
        
        // Select the currently equipped skin by default
        SnakeSkin currentSkin = skinManager.GetCurrentSkin();
        if (currentSkin != null)
        {
            SelectSkin(currentSkin);
        }
        else
        {
            // If no skin is equipped, select the first available skin
            var allSkins = skinManager.GetAllSkins();
            if (allSkins.Count > 0)
            {
                SelectSkin(allSkins[0]);
            }
        }
        
        isPageOpen = true;
        Debug.Log("ChooseSkinPage opened successfully");
    }
    
    public void ClosePage()
    {
        // Hide the skin page
        gameObject.SetActive(false);
        
        // Reset the open flag
        isPageOpen = false;
        
        // Use UIManager singleton for consistent UI state management
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMenu();
            Debug.Log("ChooseSkinPage closed - using UIManager.Instance.ShowMenu()");
        }
        else
        {
            Debug.LogError("UIManager.Instance is null! Please ensure UIManager is properly set up.");
        }
        
        Debug.Log("ChooseSkinPage closed successfully");
    }
    
    private void RefreshSkinList()
    {
        Debug.Log("RefreshSkinList() called");
        
        // Store the currently selected skin before clearing
        SnakeSkin previouslySelectedSkin = currentlySelectedSkin;
        
        // Clear existing previews
        ClearSkinPreviews();
        
        if (skinManager == null)
        {
            Debug.LogError("SkinManager not found!");
            return;
        }
        
        Debug.Log("SkinManager found, getting skins...");
        
        // Get all available skins
        List<SnakeSkin> allSkins = skinManager.GetAllSkins();
        Debug.Log($"Found {allSkins.Count} skins");
        
        // Sort skins: Defaults first, then unlocked, then locked
        List<SnakeSkin> sortedSkins = allSkins.OrderBy(skin => 
        {
            if (skin.isDefault) return 0;        // Defaults first (priority 0)
            if (skin.isUnlocked) return 1;       // Unlocked second (priority 1)
            return 2;                            // Locked last (priority 2)
        }).ThenBy(skin => skin.skinId).ToList(); // Then by skin ID for consistent ordering
        
        // Debug: Log each skin to check for duplicates and sorting
        for (int i = 0; i < sortedSkins.Count; i++)
        {
            var skin = sortedSkins[i];
            string category = skin.isDefault ? "Default" : (skin.isUnlocked ? "Unlocked" : "Locked");
            Debug.Log($"Skin {i}: ID={skin.skinId}, Name='{skin.skinName}', Category={category}");
        }
        
        // Create preview for each skin in sorted order
        foreach (var skin in sortedSkins)
        {
            CreateSkinPreview(skin);
        }
        
        Debug.Log($"Created {skinPreviewItems.Count} skin preview items");
        
        // Force layout update
        Canvas.ForceUpdateCanvases();
        
        // Restore the selection if we had a previously selected skin
        if (previouslySelectedSkin != null)
        {
            SelectSkin(previouslySelectedSkin);
        }
    }
    
    private void CreateSkinPreview(SnakeSkin skin)
    {
        if (skinPreviewPrefab == null || skinContainer == null)
        {
            Debug.LogError("Skin preview prefab or container is null!");
            return;
        }
        
        Debug.Log($"Creating preview for skin: ID={skin.skinId}, Name='{skin.skinName}'");
        
        // Instantiate preview item
        GameObject previewObject = Instantiate(skinPreviewPrefab, skinContainer);
        SkinPreviewItem previewItem = previewObject.GetComponent<SkinPreviewItem>();
        
        if (previewItem != null)
        {
            // Setup the preview item with selection callback
            previewItem.SetupSkinPreview(skin, OnSkinSelected);
            skinPreviewItems.Add(previewItem);
            Debug.Log($"Added skin preview item. Total items: {skinPreviewItems.Count}");
        }
        else
        {
            Debug.LogError("SkinPreviewItem component not found on prefab!");
        }
    }
    
    private void ClearSkinPreviews()
    {
        Debug.Log($"Clearing {skinPreviewItems.Count} skin preview items");
        
        // Destroy all preview items
        foreach (var item in skinPreviewItems)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
        skinPreviewItems.Clear();
        
        Debug.Log("Skin preview items cleared");
    }
    
    private void OnSkinSelected(SnakeSkin skin, SkinPreviewItem.ButtonAction action)
    {
        // Only select the skin, don't perform the action yet
        SelectSkin(skin);
        
        // Update the action button to reflect the current skin's action
        UpdateActionButton(skin);
    }
    
    private void SelectSkin(SnakeSkin skin)
    {
        currentlySelectedSkin = skin;
        UpdateSkinInfoPanel(skin);
        UpdateSkinSelectionVisuals(skin);
    }
    
    private void UpdateSkinInfoPanel(SnakeSkin skin)
    {
        if (skinInfoPanel == null) return;
        
        if (skin == null)
        {
            // No skin selected
            if (selectedSkinNameText != null)
                selectedSkinNameText.text = "No skin selected";
            if (selectedSkinDescriptionText != null)
                selectedSkinDescriptionText.text = "Select a skin to see details";
            if (selectedSkinPriceText != null)
                selectedSkinPriceText.text = "";
            UpdateActionButton(null);
            return;
        }
        
        // Update skin name text only
        if (selectedSkinNameText != null)
        {
            selectedSkinNameText.text = skin.skinName;
        }
        
        // Update skin description text only
        if (selectedSkinDescriptionText != null)
        {
            selectedSkinDescriptionText.text = skin.description;
        }
        
        // Update price text only
        if (selectedSkinPriceText != null)
        {
            if (!skin.isUnlocked && skin.unlockPrice > 0)
            {
                selectedSkinPriceText.text = $"Price: ${skin.unlockPrice}";
            }
            else
            {
                selectedSkinPriceText.text = ""; // Clear text instead of hiding object
            }
        }
        
        // Update action button text only
        UpdateActionButton(skin);
    }
    
    private void UpdateSkinSelectionVisuals(SnakeSkin selectedSkin)
    {
        // Update all skin preview items to show which one is selected
        foreach (var previewItem in skinPreviewItems)
        {
            if (previewItem != null)
            {
                // Check if this preview item matches the selected skin
                bool isSelected = previewItem.GetCurrentSkin() == selectedSkin;
                previewItem.SetSelected(isSelected);
                
                // Also update the equipped indicator for each skin
                previewItem.UpdateEquippedIndicator();
            }
        }
    }
    
    private void UpdateActionButton(SnakeSkin skin)
    {
        if (selectedSkinButtonText == null || selectedSkinActionButton == null) return;
        
        if (skin == null)
        {
            // No skin selected
            selectedSkinButtonText.text = "Select a skin";
            selectedSkinActionButton.interactable = false;
            return;
        }
        
        SkinPreviewItem.ButtonAction action = DetermineButtonAction(skin);
        string buttonText = "";
        bool buttonEnabled = true;
        
        switch (action)
        {
            case SkinPreviewItem.ButtonAction.Use:
                buttonText = "Use";
                buttonEnabled = true;
                break;
                
            case SkinPreviewItem.ButtonAction.Equip:
                buttonText = "Equip";
                buttonEnabled = true;
                break;
                
            case SkinPreviewItem.ButtonAction.Buy:
                buttonText = $"Buy ${skin.unlockPrice}";
                buttonEnabled = true;
                break;
                
            case SkinPreviewItem.ButtonAction.Equipped:
                buttonText = "Equipped";
                buttonEnabled = false; // Disable button when already equipped
                break;
        }
        
        // Update button text and state
        selectedSkinButtonText.text = buttonText;
        selectedSkinActionButton.interactable = buttonEnabled;
    }
    
    private void OnActionButtonClicked()
    {
        if (currentlySelectedSkin == null) return;
        
        SkinPreviewItem.ButtonAction action = DetermineButtonAction(currentlySelectedSkin);
        
        switch (action)
        {
            case SkinPreviewItem.ButtonAction.Equip:
                EquipSkin(currentlySelectedSkin);
                break;
                
            case SkinPreviewItem.ButtonAction.Buy:
                BuySkin(currentlySelectedSkin);
                break;
                
            case SkinPreviewItem.ButtonAction.Use:
                UseSkin(currentlySelectedSkin);
                break;
                
            case SkinPreviewItem.ButtonAction.Equipped:
                // Button should be disabled for equipped skins
                break;
        }
        
        // Update the action button state for the currently selected skin
        UpdateActionButton(currentlySelectedSkin);
        
        // Update the visual selection state without recreating all items
        UpdateSkinSelectionVisuals(currentlySelectedSkin);
    }
    
    private SkinPreviewItem.ButtonAction DetermineButtonAction(SnakeSkin skin)
    {
        if (skin == null) return SkinPreviewItem.ButtonAction.Buy;
        
        // Check if skin is unlocked
        if (!skin.isUnlocked)
        {
            return SkinPreviewItem.ButtonAction.Buy;
        }
        
        // Check if skin is currently equipped
        if (IsSkinEquipped(skin))
        {
            return SkinPreviewItem.ButtonAction.Equipped;
        }
        
        // Check if skin is the default skin or has price 0 (free skin)
        if (skin.isDefault || skin.unlockPrice == 0)
        {
            return SkinPreviewItem.ButtonAction.Use;
        }
        
        // Regular unlocked skin
        return SkinPreviewItem.ButtonAction.Equip;
    }
    
    private bool IsSkinEquipped(SnakeSkin skin)
    {
        if (skinManager == null || skin == null) return false;
        
        SnakeSkin currentEquippedSkin = skinManager.GetCurrentSkin();
        return currentEquippedSkin != null && currentEquippedSkin.skinId == skin.skinId;
    }
    

    
    private void EquipSkin(SnakeSkin skin)
    {
        if (skinManager != null)
        {
            skinManager.SetCurrentSkin(skin);
            skinManager.SaveCurrentSkin();
            Debug.Log($"Equipped skin: {skin.skinName}");
            
            // Apply to local player snake if it exists
            var playerManager = PlayerManager.Instance;
            if (playerManager != null)
            {
                var localSnake = playerManager.GetLocalPlayerSnake();
                if (localSnake != null)
                {
                    var snakeController = localSnake.GetComponent<SnakeController>();
                    if (snakeController != null)
                    {
                        snakeController.chosenSkinId = skin.skinId;
                        snakeController.RefreshAllSegmentsWithChosenSkin();
                    }
                }
            }
            
            // Update main menu skin display
            UpdateMainMenuSkinDisplay();
            
            // Update the action button state immediately after equipping
            UpdateActionButton(skin);
        }
    }
    
    private void BuySkin(SnakeSkin skin)
    {
        // TODO: Implement skin purchase logic
        // This would typically involve:
        // 1. Checking if player has enough currency
        // 2. Deducting currency
        // 3. Unlocking the skin
        // 4. Saving the unlock state
        
        Debug.Log($"Buy skin: {skin.skinName} - Price: {skin.unlockPrice}");
        
        // For now, just unlock the skin (remove this in production)
        skin.isUnlocked = true;
        skinManager.SaveCurrentSkin(); // This will save the unlock state
        
        // Immediately update the lock overlay for this skin
        UpdateSkinLockState(skin);
        
        // Update the action button state immediately after buying
        UpdateActionButton(skin);
    }
    
    private void UpdateSkinLockState(SnakeSkin skin)
    {
        // Find the preview item for this skin and update its lock state
        foreach (var previewItem in skinPreviewItems)
        {
            if (previewItem != null && previewItem.GetCurrentSkin() == skin)
            {
                // Update the lock state using the new method
                previewItem.UpdateLockState();
                break;
            }
        }
    }

    /// <summary>
    /// Update the main menu skin display when a skin is equipped
    /// </summary>
    private void UpdateMainMenuSkinDisplay()
    {
        // Find TestRoomJoinButton and update its skin display
        TestRoomJoinButton testRoomJoinButton = FindObjectOfType<TestRoomJoinButton>();
        if (testRoomJoinButton != null)
        {
            testRoomJoinButton.UpdateMainMenuSkinDisplayPublic();
            Debug.Log("Updated main menu skin display via TestRoomJoinButton");
        }
        else
        {
            Debug.LogWarning("TestRoomJoinButton not found for main menu skin display update");
        }
    }
    
    private void UseSkin(SnakeSkin skin)
    {
        // Same as equip for now
        EquipSkin(skin);
        
        // Update the action button state immediately after using
        UpdateActionButton(skin);
    }
    
    private void OnDestroy()
    {
        // Clean up button listeners
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(ClosePage);
        }
        
        if (selectedSkinActionButton != null)
        {
            selectedSkinActionButton.onClick.RemoveListener(OnActionButtonClicked);
        }
    }

    private void OnEnable()
    {
        Debug.Log("ChooseSkinPage enabled");
        // Don't call OpenPage here - it should only be called explicitly
    }
    private void OnDisable()
    {
        Debug.Log("ChooseSkinPage disabled");
    }
} 