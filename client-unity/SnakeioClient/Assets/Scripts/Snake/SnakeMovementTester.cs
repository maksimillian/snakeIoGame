using UnityEngine;

/// <summary>
/// Helper script to test and adjust snake movement parameters in real-time
/// Attach this to a snake GameObject to see live parameter adjustments
/// </summary>
public class SnakeMovementTester : MonoBehaviour
{
    [Header("Movement Testing")]
    [SerializeField] private bool enableTesting = true;
    [SerializeField] private bool showDebugInfo = true;
    
    [Header("Quick Parameter Adjustments")]
    [SerializeField, Range(0.1f, 0.5f)] private float bezierTension = 0.3f;
    [SerializeField, Range(0.05f, 0.3f)] private float velocitySmoothing = 0.15f;
    [SerializeField, Range(1.5f, 3.0f)] private float accelerationCurve = 2.5f;
    [SerializeField, Range(1.2f, 2.5f)] private float decelerationCurve = 1.8f;
    [SerializeField, Range(0.3f, 0.9f)] private float pathInterpolationStrength = 0.7f;
    
    [Header("Toggle Features")]
    [SerializeField] private bool useCubicBezier = true;
    [SerializeField] private bool useVelocityBasedMovement = true;
    
    private SnakeController snakeController;
    
    private void Start()
    {
        snakeController = GetComponent<SnakeController>();
        if (snakeController == null)
        {
            Debug.LogWarning("SnakeMovementTester: No SnakeController found on this GameObject");
            enabled = false;
        }
    }
    
    private void Update()
    {
        if (!enableTesting || snakeController == null) return;
        
        // Apply parameter changes in real-time
        snakeController.cubicBezierTension = bezierTension;
        snakeController.velocitySmoothing = velocitySmoothing;
        snakeController.accelerationCurve = accelerationCurve;
        snakeController.decelerationCurve = decelerationCurve;
        snakeController.pathInterpolationStrength = pathInterpolationStrength;
        snakeController.useCubicBezier = useCubicBezier;
        snakeController.useVelocityBasedMovement = useVelocityBasedMovement;
    }
    
    private void OnGUI()
    {
        if (!showDebugInfo || snakeController == null) return;
        
        // Display current movement info
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("Snake Movement Debug", GUI.skin.box);
        GUILayout.Label($"Position: {snakeController.transform.position}");
        GUILayout.Label($"Velocity: {snakeController.movementDirection}");
        GUILayout.Label($"Speed: {snakeController.movementSpeed:F2}");
        GUILayout.Label($"Is Moving: {snakeController.isMoving}");
        GUILayout.Label($"Is Local Player: {snakeController.IsLocalPlayer()}");
        GUILayout.Label($"Body Segments: {snakeController.bodySegments.Count}");
        GUILayout.EndArea();
    }
    
    [ContextMenu("Reset to Default Values")]
    public void ResetToDefaults()
    {
        bezierTension = 0.3f;
        velocitySmoothing = 0.15f;
        accelerationCurve = 2.5f;
        decelerationCurve = 1.8f;
        pathInterpolationStrength = 0.7f;
        useCubicBezier = true;
        useVelocityBasedMovement = true;
    }
    
    [ContextMenu("Apply Smooth Settings")]
    public void ApplySmoothSettings()
    {
        bezierTension = 0.4f;
        velocitySmoothing = 0.1f;
        accelerationCurve = 2.0f;
        decelerationCurve = 1.5f;
        pathInterpolationStrength = 0.8f;
        useCubicBezier = true;
        useVelocityBasedMovement = true;
    }
    
    [ContextMenu("Apply Responsive Settings")]
    public void ApplyResponsiveSettings()
    {
        bezierTension = 0.2f;
        velocitySmoothing = 0.2f;
        accelerationCurve = 3.0f;
        decelerationCurve = 2.0f;
        pathInterpolationStrength = 0.6f;
        useCubicBezier = true;
        useVelocityBasedMovement = true;
    }
} 