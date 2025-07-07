using UnityEngine;

[CreateAssetMenu(fileName = "New Snake Skin", menuName = "Snake.io/Snake Skin")]
public class SnakeSkin : ScriptableObject
{
    [Header("Skin Information")]
    public string skinName = "Default Skin";
    public string description = "A default snake skin";
    public int skinId = 1;
    public bool isUnlocked = true;
    public bool isDefault = true;
    
    [Header("Skin Sprites")]
    [Tooltip("Sprite for the snake head")]
    public Sprite headSprite;
    
    [Tooltip("Sprite for the snake body segments")]
    public Sprite bodySprite;
    
    [Header("Skin Properties")]
    [Tooltip("Color tint to apply to the skin (optional)")]
    public Color skinTint = Color.white;
    
    [Tooltip("Whether this skin has special effects")]
    public bool hasSpecialEffects = false;
    
    [Header("Unlock Requirements")]
    [Tooltip("Score required to unlock this skin")]
    public int requiredScore = 0;
    
    [Tooltip("Kills required to unlock this skin")]
    public int requiredKills = 0;
    
    [Tooltip("Games played required to unlock this skin")]
    public int requiredGames = 0;
    
    [Tooltip("Price to unlock this skin (0 = free)")]
    public int unlockPrice = 0;
    
    [Header("Skin Preview")]
    [Tooltip("Preview image for the skin selection UI")]
    public Sprite previewSprite;
    
    /// <summary>
    /// Check if the player meets the requirements to unlock this skin
    /// </summary>
    /// <param name="playerScore">Player's current score</param>
    /// <param name="playerKills">Player's total kills</param>
    /// <param name="gamesPlayed">Player's games played</param>
    /// <returns>True if the skin can be unlocked</returns>
    public bool CanUnlock(int playerScore, int playerKills, int gamesPlayed)
    {
        if (isDefault || isUnlocked || unlockPrice == 0) return true;
        
        return playerScore >= requiredScore && 
               playerKills >= requiredKills && 
               gamesPlayed >= requiredGames;
    }
    
    /// <summary>
    /// Ensure default skins and free skins (price 0) are always unlocked
    /// </summary>
    public void EnsureDefaultSkinUnlocked()
    {
        if (isDefault || unlockPrice == 0)
        {
            isUnlocked = true;
        }
    }
    
    /// <summary>
    /// Get the head sprite with optional tint applied
    /// </summary>
    /// <returns>The head sprite</returns>
    public Sprite GetHeadSprite()
    {
        return headSprite;
    }
    
    /// <summary>
    /// Get the body sprite with optional tint applied
    /// </summary>
    /// <returns>The body sprite</returns>
    public Sprite GetBodySprite()
    {
        return bodySprite;
    }
    
    /// <summary>
    /// Apply this skin to a snake controller
    /// </summary>
    /// <param name="snakeController">The snake controller to apply the skin to</param>
    public void ApplyToSnake(SnakeController snakeController)
    {
        if (snakeController == null) return;
        
        // Don't apply head sprite to the main snake controller - the first body segment will be the head
        // Just apply body sprites to existing segments using the snake's chosen skin
        SnakeSkin snakeSkin = snakeController.GetSnakeSkin();
        if (snakeSkin != null)
        {
            foreach (var segment in snakeController.bodySegments)
            {
                if (segment != null)
                {
                    SpriteRenderer segmentRenderer = segment.GetComponent<SpriteRenderer>();
                    if (segmentRenderer != null && snakeSkin.bodySprite != null)
                    {
                        segmentRenderer.sprite = snakeSkin.bodySprite;
                        segmentRenderer.color = snakeSkin.skinTint;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Apply this skin to a body segment
    /// </summary>
    /// <param name="segment">The body segment to apply the skin to</param>
    /// <param name="isFirstSegment">Whether this is the first body segment (should use head sprite)</param>
    public void ApplyToSegment(Transform segment, bool isFirstSegment = false)
    {
        if (segment == null) return;
        
        SpriteRenderer segmentRenderer = segment.GetComponent<SpriteRenderer>();
        if (segmentRenderer != null)
        {
            // Use head sprite for first segment, body sprite for others
            if (isFirstSegment && headSprite != null)
            {
                segmentRenderer.sprite = headSprite;
            }
            else if (isFirstSegment && headSprite == null)
            {
                // Fallback to body sprite if head sprite is null
                if (bodySprite != null)
                {
                    segmentRenderer.sprite = bodySprite;
                }
            }
            else if (bodySprite != null)
            {
                segmentRenderer.sprite = bodySprite;
            }
            
            segmentRenderer.color = skinTint;
        }
    }
} 