using UnityEngine;

public class BodySegment : MonoBehaviour
{
    [Header("Body Segment Settings")]
    public int ownerPlayerId = -1; // ID of the snake that owns this segment
    public int segmentIndex = -1; // Index of this segment in the snake's body
    
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Ensure the tag is set correctly
        gameObject.tag = "Snake";
        
        // Set sorting order to be behind UI elements
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = -1; // Render behind UI elements
        }
        
        // Add collider if not present
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<CircleCollider2D>();
        }
        collider.isTrigger = true;
        collider.radius = 0.3f;
        collider.offset = Vector2.zero;
        
        // Add Rigidbody2D if not present
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.isKinematic = true;
        }
    }

    public void Initialize(int playerId, int index)
    {
        ownerPlayerId = playerId;
        segmentIndex = index;
        
        // Set a proper name for reliable cleanup
        gameObject.name = $"BodySegment_{playerId}_{index}";
        
        // Set sorting order based on segment index for natural layering
        // Each segment appears behind the previous one
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = -1 - index; // Head is 0, first segment is -1, second is -2, etc.
        }
    }

    public void SetColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }
    
    public void Cleanup()
    {
        // Ensure this segment is properly destroyed
        if (gameObject != null && gameObject.activeInHierarchy)
        {
            // Removed verbose logging to reduce spam
            DestroyImmediate(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        // Removed verbose logging to reduce spam
    }
} 