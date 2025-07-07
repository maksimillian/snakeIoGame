using UnityEngine;

/// <summary>
/// Helper script to test and adjust food collection effect parameters in real-time
/// Attach this to a food GameObject to see live parameter adjustments
/// </summary>
public class FoodCollectionTester : MonoBehaviour
{
    [Header("Collection Effect Testing")]
    [SerializeField] private bool enableTesting = true;
    [SerializeField] private bool showDebugInfo = true;
    
    [Header("Quick Parameter Adjustments")]
    [SerializeField, Range(0.02f, 0.15f)] private float colliderRadius = 0.05f;
    [SerializeField, Range(0.2f, 1.5f)] private float collectionDuration = 0.6f;
    [SerializeField, Range(1f, 5f)] private float shrinkSpeed = 2f;
    [SerializeField, Range(4f, 15f)] private float moveSpeed = 8f;
    [SerializeField, Range(180f, 720f)] private float rotationSpeed = 360f;
    [SerializeField, Range(0.1f, 0.8f)] private float trailDuration = 0.3f;
    [SerializeField] private Color trailColor = Color.yellow;
    [SerializeField] private bool useTrailEffect = true;
    
    [Header("Animation Curves")]
    [SerializeField] private AnimationCurve shrinkCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Test Controls")]
    [SerializeField] private bool testCollectionEffect = false;
    [SerializeField] private Vector3 testTargetPosition = Vector3.zero;
    
    private Food foodComponent;
    
    private void Start()
    {
        foodComponent = GetComponent<Food>();
        if (foodComponent == null)
        {
            Debug.LogWarning("FoodCollectionTester: No Food component found on this GameObject");
            enabled = false;
        }
    }
    
    private void Update()
    {
        if (!enableTesting || foodComponent == null) return;
        
        // Apply parameter changes in real-time
        foodComponent.colliderRadius = colliderRadius;
        foodComponent.collectionDuration = collectionDuration;
        foodComponent.shrinkSpeed = shrinkSpeed;
        foodComponent.moveSpeed = moveSpeed;
        foodComponent.rotationSpeed = rotationSpeed;
        foodComponent.trailDuration = trailDuration;
        foodComponent.useTrailEffect = useTrailEffect;
        foodComponent.shrinkCurve = shrinkCurve;
        foodComponent.moveCurve = moveCurve;
        
        // Test collection effect
        if (testCollectionEffect)
        {
            testCollectionEffect = false;
            TestCollectionEffect();
        }
    }
    
    private void TestCollectionEffect()
    {
        if (foodComponent == null) return;
        
        Vector3 targetPos = testTargetPosition;
        if (targetPos == Vector3.zero)
        {
            // Use a random position for testing
            targetPos = transform.position + Random.insideUnitSphere * 5f;
            targetPos.z = 0;
        }
        
        foodComponent.StartCollectionEffect(targetPos);
    }
    
    private void OnGUI()
    {
        if (!showDebugInfo || foodComponent == null) return;
        
        // Display current food info
        GUILayout.BeginArea(new Rect(10, 220, 300, 150));
        GUILayout.Label("Food Collection Debug", GUI.skin.box);
        GUILayout.Label($"Position: {transform.position}");
        GUILayout.Label($"Scale: {transform.localScale}");
        GUILayout.Label($"Is Boost: {foodComponent.IsBoost}");
        GUILayout.Label($"Score Value: {foodComponent.ScoreValue}");
        GUILayout.Label($"Is Being Collected: {foodComponent.isBeingCollected}");
        GUILayout.Label($"Food Color: {foodComponent.GetComponent<SpriteRenderer>()?.color ?? Color.white}");
        GUILayout.EndArea();
    }
    
    [ContextMenu("Reset to Default Values")]
    public void ResetToDefaults()
    {
        colliderRadius = 0.05f;
        collectionDuration = 0.6f;
        shrinkSpeed = 2f;
        moveSpeed = 8f;
        rotationSpeed = 360f;
        trailDuration = 0.3f;
        trailColor = Color.yellow;
        useTrailEffect = true;
        shrinkCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }
    
    [ContextMenu("Apply Smooth Settings")]
    public void ApplySmoothSettings()
    {
        colliderRadius = 0.06f;
        collectionDuration = 0.8f;
        shrinkSpeed = 1.5f;
        moveSpeed = 6f;
        rotationSpeed = 240f;
        trailDuration = 0.4f;
        trailColor = Color.yellow;
        useTrailEffect = true;
        shrinkCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }
    
    [ContextMenu("Apply Fast Settings")]
    public void ApplyFastSettings()
    {
        colliderRadius = 0.04f;
        collectionDuration = 0.4f;
        shrinkSpeed = 3f;
        moveSpeed = 12f;
        rotationSpeed = 480f;
        trailDuration = 0.2f;
        trailColor = Color.yellow;
        useTrailEffect = true;
        shrinkCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }
    
    [ContextMenu("Test Collection Effect")]
    public void TestCollectionEffectMenu()
    {
        TestCollectionEffect();
    }
} 