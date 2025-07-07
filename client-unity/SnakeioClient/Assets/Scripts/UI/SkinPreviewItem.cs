using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class SkinPreviewItem : MonoBehaviour
{
    [Header("UI References")]
    public Image headSegmentImage;
    public List<Image> bodySegmentImages = new List<Image>();
    public GameObject lockOverlay;
    public GameObject equippedIndicator;
    public GameObject selectedOverlay;
    public Button selectButton;
    
    [Header("Visual Settings")]
    // All visual styling handled in Unity Editor
    
    private SnakeSkin currentSkin;
    private Action<SnakeSkin, ButtonAction> onSkinSelected;
    private SkinManager skinManager;
    private bool isSelected = false;
    
    public enum ButtonAction
    {
        Use,
        Equip,
        Buy,
        Equipped
    }
    
    private void Awake()
    {
        skinManager = SkinManager.Instance;
        
        // Setup selection button
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnSelectButtonClicked);
        }
    }
    
    public void SetupSkinPreview(SnakeSkin skin, Action<SnakeSkin, ButtonAction> callback)
    {
        currentSkin = skin;
        onSkinSelected = callback;
        
        if (skin == null)
        {
            Debug.LogError("Skin is null in SetupSkinPreview!");
            return;
        }
        
        // Setup the complete snake preview
        SetupSnakePreview(skin);
        
        // Setup lock overlay - hide lock for unlocked skins or skins with price 0
        if (lockOverlay != null)
        {
            bool shouldShowLock = !skin.isUnlocked && skin.unlockPrice > 0;
            lockOverlay.SetActive(shouldShowLock);
        }
        
        // Setup equipped indicator
        if (equippedIndicator != null)
        {
            bool isEquipped = IsSkinEquipped(skin);
            equippedIndicator.SetActive(isEquipped);
        }
    }
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisualState();
        
        // Update selected overlay
        if (selectedOverlay != null)
        {
            selectedOverlay.SetActive(selected);
        }
    }
    
    private void UpdateVisualState()
    {
        // Visual feedback handled in Unity Editor
        // You can add your own visual feedback here if needed
    }
    
    private void SetupSnakePreview(SnakeSkin skin)
    {
        // Setup head segment
        if (headSegmentImage != null)
        {
            if (skin.headSprite != null)
            {
                headSegmentImage.sprite = skin.headSprite;
            }
            else if (skin.bodySprite != null)
            {
                // Fallback to body sprite if head sprite is null
                headSegmentImage.sprite = skin.bodySprite;
            }
            else
            {
                headSegmentImage.sprite = null;
            }
        }
        
        // Setup body segments
        SetupBodySegments(skin);
    }
    
    private void SetupBodySegments(SnakeSkin skin)
    {
        if (bodySegmentImages == null || bodySegmentImages.Count == 0)
        {
            Debug.LogWarning("No body segment images assigned to SkinPreviewItem!");
            return;
        }
        
        // Setup each body segment image
        for (int i = 0; i < bodySegmentImages.Count; i++)
        {
            Image segmentImage = bodySegmentImages[i];
            if (segmentImage != null)
            {
                // Set the body sprite
                if (skin.bodySprite != null)
                {
                    segmentImage.sprite = skin.bodySprite;
                }
                else
                {
                    segmentImage.sprite = null;
                }
            }
        }
    }
    
    private ButtonAction DetermineButtonAction()
    {
        if (currentSkin == null) return ButtonAction.Buy;
        
        // Check if skin is unlocked
        if (!currentSkin.isUnlocked)
        {
            return ButtonAction.Buy;
        }
        
        // Check if skin is currently equipped
        if (IsSkinEquipped(currentSkin))
        {
            return ButtonAction.Equipped;
        }
        
        // Check if skin is the default skin or has price 0 (free skin)
        if (currentSkin.isDefault || currentSkin.unlockPrice == 0)
        {
            return ButtonAction.Use;
        }
        
        // Regular unlocked skin
        return ButtonAction.Equip;
    }
    
    private bool IsSkinEquipped(SnakeSkin skin)
    {
        if (skinManager == null || skin == null) return false;
        
        SnakeSkin currentEquippedSkin = skinManager.GetCurrentSkin();
        return currentEquippedSkin != null && currentEquippedSkin.skinId == skin.skinId;
    }
    
    private void OnSelectButtonClicked()
    {
        if (currentSkin != null && onSkinSelected != null)
        {
            // Only select the skin, don't perform the action
            // The action will be handled by the separate action button
            onSkinSelected(currentSkin, ButtonAction.Equip); // Pass any action, it will be ignored
        }
    }
    
    public SnakeSkin GetCurrentSkin()
    {
        return currentSkin;
    }
    
    public void UpdateEquippedIndicator()
    {
        if (equippedIndicator != null && currentSkin != null)
        {
            bool isEquipped = IsSkinEquipped(currentSkin);
            equippedIndicator.SetActive(isEquipped);
        }
    }
    
    public void UpdateLockState()
    {
        if (lockOverlay != null && currentSkin != null)
        {
            bool shouldShowLock = !currentSkin.isUnlocked && currentSkin.unlockPrice > 0;
            lockOverlay.SetActive(shouldShowLock);
        }
    }
    
    private void OnDestroy()
    {
        // Clean up button listener
        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(OnSelectButtonClicked);
        }
    }
}