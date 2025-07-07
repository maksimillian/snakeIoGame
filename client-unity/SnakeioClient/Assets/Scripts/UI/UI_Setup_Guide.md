# Skin Selection UI Setup Guide

## Overview
This guide explains how to set up the skin selection UI components in Unity.

## Required Components

### 1. ChooseSkinPage GameObject
Create a GameObject with the `ChooseSkinPage` script and set up the following hierarchy:

```
ChooseSkinPage (GameObject with ChooseSkinPage script)
├── Background (Image)
├── Title (TextMeshPro - "Choose Your Skin")
├── CloseButton (Button)
├── BackButton (Button)
├── ScrollView (ScrollRect)
│   ├── Viewport (Mask)
│   │   └── SkinContainer (GameObject with HorizontalLayoutGroup)
└── EquippedIndicator (Optional - TextMeshPro)
```

**ChooseSkinPage Script Settings:**
- `skinPreviewPrefab`: Reference to the SkinPreviewItem prefab
- `skinContainer`: Reference to the SkinContainer GameObject
- `layoutGroup`: Reference to the HorizontalLayoutGroup component
- `scrollRect`: Reference to the ScrollRect component
- `closeButton`: Reference to the CloseButton
- `backButton`: Reference to the BackButton

### 2. SkinPreviewItem Prefab
Create a prefab with the `SkinPreviewItem` script and set up the following hierarchy:

```
SkinPreviewItem (GameObject with SkinPreviewItem script)
├── Background (Image)
├── SkinPreviewImage (Image)
├── LockOverlay (Image with lock icon)
├── SkinNameText (TextMeshPro)
├── PriceText (TextMeshPro)
├── ActionButton (Button)
│   └── ButtonText (TextMeshPro)
└── EquippedIndicator (GameObject)
    └── EquippedText (TextMeshPro - "EQUIPPED")
```

**SkinPreviewItem Script Settings:**
- `skinPreviewImage`: Reference to the SkinPreviewImage
- `lockOverlay`: Reference to the LockOverlay
- `actionButton`: Reference to the ActionButton
- `buttonText`: Reference to the ButtonText
- `skinNameText`: Reference to the SkinNameText
- `priceText`: Reference to the PriceText
- `equippedIndicator`: Reference to the EquippedIndicator

### 3. Update TestRoomJoinButton
Add a skin selection button to your existing UI:

```
TestRoomJoinButton GameObject
├── ... (existing UI elements)
└── SkinSelectionButton (Button)
    └── ButtonText (TextMeshPro - "Choose Skin")
```

**TestRoomJoinButton Script Settings:**
- Add `skinSelectionButton`: Reference to the new SkinSelectionButton
- Add `chooseSkinPage`: Reference to the ChooseSkinPage GameObject

## Visual Design Recommendations

### ChooseSkinPage
- Use a semi-transparent dark background
- Position in center of screen
- Add smooth fade in/out animations
- Make it modal (disable background interaction)

### SkinPreviewItem
- Use consistent sizing (e.g., 120x120 pixels)
- Add hover effects
- Use different colors for different button states:
  - Blue for "Use"
  - Green for "Equip"
  - Yellow for "Buy"
  - Gray for "Equipped"

### Button States
- **Use**: For default skins (always available)
- **Equip**: For unlocked skins that aren't currently equipped
- **Buy**: For locked skins (shows price)
- **Equipped**: For currently equipped skin (disabled button)

## Setup Steps

1. **Create the SkinPreviewItem prefab** with the hierarchy above
2. **Create the ChooseSkinPage GameObject** with the hierarchy above
3. **Add a skin selection button** to your main menu
4. **Assign all references** in the inspector
5. **Test the functionality** by clicking the skin selection button

## Notes

- The system automatically loads and saves skin unlock states using PlayerPrefs
- Default skins are always unlocked
- The current skin selection is saved and restored between sessions
- Bot skins are not affected by the player's skin selection 