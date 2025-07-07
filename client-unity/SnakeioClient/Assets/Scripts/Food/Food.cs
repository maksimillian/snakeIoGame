using UnityEngine;
using System.Collections;

public class Food : MonoBehaviour
{
    public bool IsBoost { get; private set; }
    public int ScoreValue { get; private set; } = 1;
    
    [Header("Collision Settings")]
    [Tooltip("Increased collider size to prevent server-client collision mismatches")]
    public float colliderRadius = 0.05f; // Increased for more forgiving collision detection
    
    [Header("Collection Effect Settings")]
    public float collectionDuration = 0.6f; // Duration of the collection animation
    public float shrinkSpeed = 2f; // How fast the food shrinks
    public float moveSpeed = 8f; // How fast the food moves toward player
    public float rotationSpeed = 360f; // Degrees per second rotation during collection
    public AnimationCurve shrinkCurve = AnimationCurve.EaseInOut(0, 1, 1, 0); // Custom shrink curve
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Custom move curve
    public bool useTrailEffect = true; // Enable trail effect during collection
    public Color trailColor = Color.yellow; // Trail color during collection
    public float trailDuration = 0.3f; // Trail effect duration
    
    public bool isBeingCollected = false;
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private TrailRenderer trailRenderer;
    private SpriteRenderer spriteRenderer;
    private CircleCollider2D foodCollider;
    private Rigidbody2D foodRigidbody;

    private void Awake()
    {
        // Store original scale and position
        originalScale = transform.localScale;
        originalPosition = transform.position;
        
        // Get components
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Note: Food sorting order is set to -10 in PlayerManager and FoodManager
        // to ensure food appears under snake sprites (which have sorting orders 2-9)
        
        // Add collider if not present
        foodCollider = GetComponent<CircleCollider2D>();
        if (foodCollider == null)
        {
            foodCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        foodCollider.isTrigger = true;
        foodCollider.radius = colliderRadius;
        foodCollider.offset = Vector2.zero; // Ensure collider is centered
        
        // Apply collider radius immediately
        ApplyColliderRadius();

        // Add Rigidbody2D for better collision detection
        foodRigidbody = GetComponent<Rigidbody2D>();
        if (foodRigidbody == null)
        {
            foodRigidbody = gameObject.AddComponent<Rigidbody2D>();
            foodRigidbody.gravityScale = 0;
            foodRigidbody.isKinematic = true;
        }

        // Ensure we have a sprite renderer
        if (spriteRenderer == null)
        {
            Debug.LogError("Food prefab is missing SpriteRenderer component!");
        }
        
        // Add trail renderer for collection effect
        if (useTrailEffect)
        {
            trailRenderer = GetComponent<TrailRenderer>();
            if (trailRenderer == null)
            {
                trailRenderer = gameObject.AddComponent<TrailRenderer>();
            }
            SetupTrailRenderer();
        }
    }

    private void SetupTrailRenderer()
    {
        if (trailRenderer == null) return;
        
        trailRenderer.time = trailDuration;
        trailRenderer.startWidth = 0.1f;
        trailRenderer.endWidth = 0.01f;
        
        // Use food's color for trail, fallback to trailColor if no sprite renderer
        Color trailStartColor = trailColor;
        if (spriteRenderer != null)
        {
            trailStartColor = spriteRenderer.color;
        }
        
        // Set trail color with 0.7 alpha
        trailRenderer.startColor = new Color(trailStartColor.r, trailStartColor.g, trailStartColor.b, 0.7f);
        trailRenderer.endColor = new Color(trailStartColor.r, trailStartColor.g, trailStartColor.b, 0f);
        trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
        trailRenderer.enabled = false; // Start disabled
    }

    public void Initialize(bool isBoost)
    {
        IsBoost = isBoost;
        ScoreValue = isBoost ? 2 : 1;
        //Debug.Log($"Food initialized at position {transform.position}: IsBoost={isBoost}, ScoreValue={ScoreValue}");
    }
    
    /// <summary>
    /// Start the collection effect animation
    /// </summary>
    /// <param name="targetPosition">Position to move toward (usually the player)</param>
    public void StartCollectionEffect(Vector3 targetPosition)
    {
        if (isBeingCollected) return; // Prevent multiple collection effects
        
        // Set trail color to match food color with 0.7 alpha
        if (trailRenderer != null && useTrailEffect && spriteRenderer != null)
        {
            Color foodColor = spriteRenderer.color;
            trailRenderer.startColor = new Color(foodColor.r, foodColor.g, foodColor.b, 0.7f);
            trailRenderer.endColor = new Color(foodColor.r, foodColor.g, foodColor.b, 0f);
        }
        
        isBeingCollected = true;
        StartCoroutine(CollectionEffectCoroutine(targetPosition));
    }
    
    /// <summary>
    /// Start delayed destruction for food that wasn't collected
    /// </summary>
    /// <param name="delay">Delay in seconds before destruction</param>
    public void StartDelayedDestruction(float delay)
    {
        if (isBeingCollected) return; // Don't start delayed destruction if already being collected
        
        StartCoroutine(DelayedDestructionCoroutine(delay));
    }
    
    private IEnumerator CollectionEffectCoroutine(Vector3 targetPosition)
    {
        // Disable collider during collection
        if (foodCollider != null)
        {
            foodCollider.enabled = false;
        }
        
        // Enable trail effect
        if (trailRenderer != null && useTrailEffect)
        {
            trailRenderer.enabled = true;
            
            // Update trail color to match food color with 0.7 alpha
            if (spriteRenderer != null)
            {
                Color foodColor = spriteRenderer.color;
                trailRenderer.startColor = new Color(foodColor.r, foodColor.g, foodColor.b, 0.7f);
                trailRenderer.endColor = new Color(foodColor.r, foodColor.g, foodColor.b, 0f);
            }
        }
        
        Vector3 startPosition = transform.position;
        Vector3 startScale = transform.localScale;
        float elapsedTime = 0f;
        
        // Store original color for fade effect
        Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        
        while (elapsedTime < collectionDuration)
        {
            float progress = elapsedTime / collectionDuration;
            
            // Calculate eased progress
            float easedProgress = moveCurve.Evaluate(progress);
            float shrinkProgress = shrinkCurve.Evaluate(progress);
            
            // Move toward target
            Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, easedProgress);
            transform.position = newPosition;
            
            // Shrink the food
            Vector3 newScale = Vector3.Lerp(startScale, Vector3.zero, shrinkProgress);
            transform.localScale = newScale;
            
            // Rotate the food
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
            
            // Fade out the sprite
            if (spriteRenderer != null)
            {
                Color newColor = originalColor;
                newColor.a = 1f - progress;
                spriteRenderer.color = newColor;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final state
        transform.position = targetPosition;
        transform.localScale = Vector3.zero;
        
        // Disable trail effect
        if (trailRenderer != null)
        {
            trailRenderer.enabled = false;
        }
        
        // Remove from PlayerManager's dictionary before destroying
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.RemoveFoodByGameObject(gameObject);
        }
        
        // Destroy the food object
        Destroy(gameObject);
    }
    
    private IEnumerator DelayedDestructionCoroutine(float delay)
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(delay);
        
        // Check if we're still not being collected
        if (!isBeingCollected)
        {
            // Remove from PlayerManager's dictionary before destroying
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.RemoveFoodByGameObject(gameObject);
            }
            
            // Destroy the food object
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Apply the current collider radius to the collider component
    /// </summary>
    private void ApplyColliderRadius()
    {
        if (foodCollider != null)
        {
            foodCollider.radius = colliderRadius;
        }
    }
    
    /// <summary>
    /// Get the closest player position for collection effect
    /// </summary>
    private Vector3 GetClosestPlayerPosition()
    {
        // Find the local player first
        GameObject localPlayer = PlayerManager.Instance?.GetLocalPlayerSnake();
        if (localPlayer != null)
        {
            return localPlayer.transform.position;
        }
        
        // Fallback: find any snake
        SnakeController[] snakes = FindObjectsOfType<SnakeController>();
        if (snakes.Length > 0)
        {
            return snakes[0].transform.position;
        }
        
        // Final fallback: use current position
        return transform.position;
    }
    
    /// <summary>
    /// Get the position of the snake that triggered the collection
    /// </summary>
    private Vector3 GetSnakePosition(Collider2D snakeCollider)
    {
        // Try to get the snake controller from the collider
        SnakeController snakeController = snakeCollider.GetComponent<SnakeController>();
        if (snakeController != null)
        {
            return snakeController.transform.position;
        }
        
        // If no snake controller, try to find it in parent objects
        snakeController = snakeCollider.GetComponentInParent<SnakeController>();
        if (snakeController != null)
        {
            return snakeController.transform.position;
        }
        
        // Fallback: use the collider's position
        return snakeCollider.transform.position;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore collisions with other food objects
        if (other.CompareTag("Food"))
        {
            return; // Food should not trigger with other food
        }
        
        // Check if snake collided with food
        if (other.CompareTag("Snake"))
        {
            // Start collection effect when snake touches food
            if (!isBeingCollected)
            {
                Vector3 snakePosition = GetSnakePosition(other);
                StartCollectionEffect(snakePosition);
            }
        }
        // Log any other unexpected collisions for debugging
        else
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"Food unexpected collision with: {other.gameObject.name}, Tag: {other.tag}");
            #endif
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Ignore collisions with other food objects
        if (collision.gameObject.CompareTag("Food"))
        {
            return; // Food should not collide with other food
        }
        
        Debug.Log($"Food collision with: {collision.gameObject.name}, Tag: {collision.gameObject.tag}");
    }
} 