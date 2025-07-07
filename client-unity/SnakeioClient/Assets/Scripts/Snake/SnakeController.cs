using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class SnakeController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField]
    public float visualUpdateRate = 60f; // Increased to 60 FPS for smoother updates
    public float segmentSpeed = 30f; // Increased for faster body segment following
    public float segmentSpeedMultiplier = 0.05f; // Reduced multiplier for more consistent segment movement
    public float distanceThreshold = 0.001f; // Threshold for meaningful position differences
    public float rotationSpeed = 0.1f; // Decreased from 0.25f to 0.1f for more gradual turning

    [Header("Smoothness Settings")]
    public float lerpDurationMultiplier = 2.5f; // Increased for smoother movement
    public float bodySegmentLerpSpeed = 8f; // Reduced for smoother body following
    public float predictionSmoothness = 0.8f; // Smoothing factor for prediction
    public float headSmoothTime = 0.08f; // Smooth time for head movement
    public float headMaxSpeed = 100f; // Max speed for SmoothDamp
    
    [Header("Advanced Smoothness Settings")]
    public float cubicBezierTension = 0.3f; // Controls curve smoothness (0.1-0.5)
    public float velocitySmoothing = 0.15f; // Velocity smoothing factor
    public float accelerationCurve = 2.5f; // Easing curve for acceleration (1.5-3.0)
    public float decelerationCurve = 1.8f; // Easing curve for deceleration (1.2-2.5)
    public float pathInterpolationStrength = 0.7f; // How much to interpolate along path vs direct
    public bool useCubicBezier = true; // Enable cubic bezier interpolation
    public bool useVelocityBasedMovement = true; // Use velocity-based instead of position-based

    [Header("Body Settings")]
    public GameObject bodySegmentPrefab;
    public int initialBodySegments = 5; // Match server's initial length
    public float colliderRadius = 0.3f; // Increased for better collision detection
    public bool showServerPositions = false; // Debug: show server segment positions in different color
    public Color serverPositionColor = Color.red; // Color for server segment positions

    [Header("Network Settings")]
    public bool isLocalPlayer = true;
    public int playerId = -1; // Set by the game manager

    [Header("Game Settings")]
    public int score = 0;
    public int kills = 0;
    public bool isAlive = true;
    public int foodProgress = 0; // Progress towards next segment (0-2, 3 food needed for 1 segment)
    public float segmentSize = 0.1f; // Segment size multiplier based on length
    
    [Header("Bot Settings")]
    public int? botSkinId = null; // Skin ID for bots (null for real players)
    
    [Header("Skin Settings")]
    public int chosenSkinId = -1; // The skin ID chosen for this snake (both players and bots)

    public List<Transform> bodySegments = new List<Transform>();
    public bool isBoosting;
    private Vector3 targetPosition;
    private Vector3 currentVelocity;
    private Vector3[] bodyVelocities;

    // Server-authoritative state
    public Vector3 serverPosition;
    private Quaternion serverRotation;
    private int serverLength;
    public bool serverIsAlive = true;
    private Vector3[] serverSegments;

    // Client-side prediction and interpolation
    private Vector3 interpolatedPosition;
    private float interpolationTime = 0f;
    public float interpolationDuration = 0.016f; // Reduced to 16ms (60 FPS) for more responsive movement with 120 FPS server updates
    private Vector3 lastServerPosition;
    public float lastServerUpdateTime;
    public bool hasReceivedFirstUpdate = false;
    
    // Client-side prediction for local player
    private Vector3 predictedPosition;
    private float predictionTime = 0f;
    public float maxPredictionTime = 0.1f; // Maximum prediction time (100ms)
    private Vector3 localInputDirection;
    private float localInputSpeed;
    
    // Smooth lerp with offset for variable server timing
    private Vector3 lerpStartPosition;
    private Vector3 lerpTargetPosition;
    private float lerpStartTime;
    private float lerpDuration;
    private bool isLerping = false;
    
    // Continuous movement prediction
    public Vector3 lastDirection;
    public Vector3 smoothedDirection;
    private float lastSpeed;
    private bool lastIsBoosting;
    public float directionSmoothTime = 0.05f; // Reduced for more responsive movement
    private Vector3 directionVelocity;
    
    // Smooth correction from server
    public Vector3 correctedPosition;
    public float correctionSmoothTime = 0.05f; // Reduced for more responsive movement
    private Vector3 positionVelocity;

    // Fixed time step for consistent interpolation regardless of frame rate
    private const float FIXED_TIME_STEP = 1f / 60f; // 60 FPS fixed time step
    private float fixedTimeAccumulator = 0f;

    // Thread-safe queue for main thread execution
    private Queue<System.Action> mainThreadActions = new Queue<System.Action>();

    // Performance optimization
    private float lastVisualUpdate = 0f;
    private float visualUpdateInterval;

    // Input rate limiting
    private float inputTimer;
    private const float INPUT_INTERVAL = 1f / 60f; // 60 Hz input rate

    // Collision detection
    public bool hasCollided = false;
    public float collisionCooldown = 0f;
    private const float COLLISION_COOLDOWN_TIME = 0.5f; // Prevent multiple collision events
    public bool hasDied = false; // Prevent multiple death calls

    // Spawn protection
    public float spawnTime = 0f; // Time when snake was spawned
    private const float SPAWN_PROTECTION_TIME = 2f; // 2 seconds of spawn protection
    
    // Cleanup logging throttling
    private int cleanupLogCounter = 0;
    private int deathLogCounter = 0;
    private int skinRefreshCounter = 0;

    // Continuous movement system
    public Vector3 currentPosition;
    public Vector3 movementDirection;
    public float movementSpeed;
    public bool isMoving;
    private float lastMovementTime;
    public float segmentSpacing = 0.4f; // Match server segment distance

    // Client-side movement continuation
    public Vector3 lastKnownDirection = Vector3.right; // Default direction
    public float lastKnownSpeed = 4f; // Default speed
    public bool lastKnownBoosting = false;
    private float serverTimeoutThreshold = 0.2f; // Reduced to 200ms for smoother movement
    public bool isUsingClientPrediction = false; // Flag to track when we're using client-side movement

    // Head movement velocity used by SmoothDamp
    private Vector3 headVelocity = Vector3.zero;
    
    // Advanced movement system variables
    private Vector3[] movementHistory = new Vector3[4]; // Store last 4 positions for bezier curves
    private int historyIndex = 0;
    private Vector3 smoothedVelocity = Vector3.zero;
    private Vector3 targetVelocity = Vector3.zero;
    private float currentSpeed = 0f;
    private float targetSpeed = 0f;
    private Vector3[] bodySegmentVelocities; // Individual velocities for body segments
    private Vector3[] bodySegmentTargets; // Target positions for body segments
    private float[] bodySegmentSpeeds; // Individual speeds for body segments

    // ==== Interpolation buffer (renders 50-ms behind real time for ultra smooth motion) ====
    private struct BufferedState
    {
        public Vector3 headPos;
        public Vector3[] segmentPos;
        public float time; // Timestamp when we received the snapshot (Time.time)
    }
    private readonly List<BufferedState> _stateBuffer = new List<BufferedState>();
    private const float BUFFER_DELAY = 0.05f; // render 50 ms in the past

    private void Awake()
    {
        // Set the tag
        gameObject.tag = "Snake";

        // Set sorting order for snake head to be behind UI elements but on top of body segments
        SpriteRenderer headRenderer = GetComponent<SpriteRenderer>();
        if (headRenderer != null)
        {
            headRenderer.sortingOrder = 1; // Render behind UI elements but on top of body segments
        }

        // Add collider if not present
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<CircleCollider2D>();
        }
        collider.isTrigger = true;
        collider.radius = colliderRadius;
        collider.offset = Vector2.zero;

        // Add Rigidbody2D for better collision detection
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.isKinematic = true;
        }
    }

    private void Start()
    {
        InitializeSnake();
        
        // Calculate visual update interval
        visualUpdateInterval = 1f / visualUpdateRate;
        
        // Subscribe to game state updates for this specific player
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnGameStateUpdated += OnGameStateUpdated;
        }
    }

    void Update()
    {
        // Execute queued actions on main thread
        while (mainThreadActions.Count > 0)
        {
            var action = mainThreadActions.Dequeue();
            action?.Invoke();
        }

        // Don't process anything until we have a valid player ID
        if (playerId == -1)
        {
            return;
        }

        // Update collision cooldown
        if (collisionCooldown > 0)
        {
            collisionCooldown -= Time.deltaTime;
        }

        if (isLocalPlayer && isAlive)
        {
            // Handle input every frame for maximum responsiveness
            HandleInput();
        }

        // Update visual representation every frame for smooth movement
        UpdateVisuals();
    }

    private void OnDestroy()
    {
        // Unsubscribe from game state updates
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnGameStateUpdated -= OnGameStateUpdated;
        }
        
        // Clean up debug markers
        CleanupDebugMarkers();
    }
    
    private void CleanupDebugMarkers()
    {
        // Find and destroy ALL debug markers for this snake (not just first 20)
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int cleanedCount = 0;
        
        foreach (var obj in allObjects)
        {
            if (obj != null && obj.name.StartsWith($"ServerSegment_{playerId}_"))
            {
                DestroyImmediate(obj);
                cleanedCount++;
            }
        }
        
        // Also clean up any debug markers that might be orphaned (no player ID in name)
        // This handles cases where debug markers might have been created without proper naming
        GameObject[] allSpheres = FindObjectsOfType<GameObject>();
        foreach (var sphere in allSpheres)
        {
            if (sphere != null && sphere.name.StartsWith("ServerSegment_") && !sphere.name.Contains($"_{playerId}_"))
            {
                // This is a debug marker for a different player, but let's clean it up too
                // to prevent any potential issues
                DestroyImmediate(sphere);
                cleanedCount++;
            }
        }
    }

    private void InitializeSnake()
    {
        // Initialize body velocities array
        bodyVelocities = new Vector3[initialBodySegments];
        
        // Initialize advanced movement system
        InitializeBodySegmentArrays();
        
        // Initialize movement history
        for (int i = 0; i < movementHistory.Length; i++)
        {
            movementHistory[i] = Vector3.zero;
        }
        
        // Add initial body segments
        for (int i = 0; i < initialBodySegments; i++)
        {
            AddBodySegment();
        }
    }

    private void HandleInput()
    {
        // Check if we're connected
        if (NetworkManager.Instance == null || !NetworkManager.Instance.IsConnected())
        {
            return;
        }
        
        // Allow input even if playerId is not set yet (during game start)
        // This prevents blocking input during the initial connection phase
        if (playerId == -1)
        {
            // Still allow input during game start, but don't send to server
            // This prevents the snake from being unresponsive during initialization
            HandleLocalInputOnly();
            return;
        }

        // Get mouse position for direction
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;

        // Calculate direction to mouse
        Vector3 dir = mousePosition - transform.position;
        
        // Always send a normalized direction vector for constant movement
        if (dir.magnitude > 0.001f)
        {
            dir.Normalize();
            // Update movement direction for continuous movement
            movementDirection = dir;
            // Store local input for prediction
            localInputDirection = dir;
        }
        else
        {
            // If mouse is very close to snake head, use last direction or a default direction
            if (movementDirection.magnitude > 0.001f)
            {
                dir = movementDirection;
            }
            else
            {
                // Use a default direction to ensure constant movement
                dir = Vector3.right;
                movementDirection = dir;
            }
            // Store local input for prediction
            localInputDirection = dir;
        }

        // Handle boost input
        isBoosting = Input.GetMouseButton(0); // LMB for boost
        localInputSpeed = isBoosting ? 6f : 4f; // Store local speed for prediction
        
        // Store for client-side continuation
        if (dir.magnitude > 0.001f)
        {
            lastKnownDirection = dir;
            lastKnownSpeed = localInputSpeed;
            lastKnownBoosting = isBoosting;
        }

        // Send input to server
        NetworkManager.Instance.EmitSnakeInput(dir, isBoosting);
    }
    
    private void HandleLocalInputOnly()
    {
        // Handle input locally without sending to server (for game start period)
        // Get mouse position for direction
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;

        // Calculate direction to mouse
        Vector3 dir = mousePosition - transform.position;
        
        // Always send a normalized direction vector for constant movement
        if (dir.magnitude > 0.001f)
        {
            dir.Normalize();
            // Update movement direction for continuous movement
            movementDirection = dir;
            // Store local input for prediction
            localInputDirection = dir;
        }
        else
        {
            // If mouse is very close to snake head, use last direction or a default direction
            if (movementDirection.magnitude > 0.001f)
            {
                dir = movementDirection;
            }
            else
            {
                // Use a default direction to ensure constant movement
                dir = Vector3.right;
                movementDirection = dir;
            }
            // Store local input for prediction
            localInputDirection = dir;
        }

        // Handle boost input
        isBoosting = Input.GetMouseButton(0); // LMB for boost
        localInputSpeed = isBoosting ? 6f : 4f; // Store local speed for prediction
        
        // Store for client-side continuation
        if (dir.magnitude > 0.001f)
        {
            lastKnownDirection = dir;
            lastKnownSpeed = localInputSpeed;
            lastKnownBoosting = isBoosting;
        }
        
        // Don't send to server yet - just handle locally
        Debug.Log($"Handling local input only (playerId not set yet) - Direction: {dir}, Boosting: {isBoosting}");
    }

    private void UpdateVisuals()
    {
        if (!serverIsAlive || !isAlive || hasDied)
        {
            // Snake is dead - don't update visuals at all
            // The Die() method will handle proper deactivation after spawning food
            return;
        }

        // Show snake if alive
        gameObject.SetActive(true);

        // Check if server updates are delayed and use client-side movement continuation
        float timeSinceLastServerUpdate = Time.time - lastServerUpdateTime;
        bool serverUpdateDelayed = timeSinceLastServerUpdate > serverTimeoutThreshold;
        
        if (serverUpdateDelayed && hasReceivedFirstUpdate)
        {
            // Use client-side movement continuation
            UpdateWithClientContinuation(timeSinceLastServerUpdate);
            isUsingClientPrediction = true;
        }
        else
        {
            // Use normal server-authoritative movement
            isUsingClientPrediction = false;
            
            // Use client-side prediction for local player, smooth lerp for others
            if (hasReceivedFirstUpdate)
            {
                if (isLocalPlayer)
                {
                    // Use client-side prediction for local player
                    UpdateLocalPlayerWithPrediction();
                }
                else
                {
                    // Use smooth lerp interpolation for other players
                    UpdateOtherPlayerWithLerp();
                }
                
                lastMovementTime = Time.time;
            }
            else
            {
                // Initialize with server position
                currentPosition = serverPosition;
                transform.position = currentPosition;
                lastMovementTime = Time.time;
            }
        }
        
        // Calculate rotation based on movement direction
        if (movementDirection.magnitude > 0.001f)
        {
            float angle = Mathf.Atan2(movementDirection.y, movementDirection.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed);
        }
    }
    
    private void UpdateLocalPlayerWithPrediction()
    {
        // Calculate time since last server update
        float timeSinceLastUpdate = Time.time - lastServerUpdateTime;
        
        // Predict where the snake head should be based on local input
        Vector3 predictedPos = serverPosition;
        if (localInputDirection.magnitude > 0.001f && timeSinceLastUpdate > 0)
        {
            // Predict movement based on local input direction and speed
            float predictionDuration = Mathf.Min(timeSinceLastUpdate, maxPredictionTime);
            Vector3 predictedMovement = localInputDirection.normalized * localInputSpeed * predictionDuration;
            predictedPos += predictedMovement;
        }
        
        // Apply smoothing to the predicted position to reduce jerkiness
        Vector3 smoothedPredictedPos = Vector3.Lerp(currentPosition, predictedPos, predictionSmoothness);
        
        // Use smooth lerp with adaptive timing for maximum smoothness
        if (isLerping)
        {
            UpdateWithSmoothLerp(smoothedPredictedPos);
        }
        else
        {
            // Initialize new lerp with adaptive duration
            InitializeAdaptiveLerp(smoothedPredictedPos);
        }
        
        // Update body segments with natural path following (NOT prediction)
        UpdateBodySegmentsWithNaturalFollowing();
    }
    
    private void UpdateOtherPlayerWithLerp()
    {
        // Try to obtain an interpolated state from the buffer (returns false if not enough data)
        if (TryGetBufferedState(out Vector3 headPos, out Vector3[] segPos))
        {
            // Use smooth lerp with adaptive timing for other players
            if (isLerping)
            {
                UpdateWithSmoothLerp(headPos);
            }
            else
            {
                // Initialize new lerp with adaptive duration
                InitializeAdaptiveLerp(headPos);
            }
            
            // Update body segments with natural path following
            if (segPos != null)
            {
                serverSegments = segPos;
            }
            UpdateBodySegmentsWithNaturalFollowing();
        }
        else
        {
            // Fallback to direct adaptive lerp if not enough buffered states
            if (isLerping)
            {
                UpdateWithSmoothLerp(serverPosition);
            }
            else
            {
                InitializeAdaptiveLerp(serverPosition);
            }
            UpdateBodySegmentsWithNaturalFollowing();
        }
    }
    
    private bool TryGetBufferedState(out Vector3 headPos, out Vector3[] segPos)
    {
        headPos = serverPosition;
        segPos = serverSegments;
        if (_stateBuffer.Count < 2) return false;
        float renderTime = Time.time - BUFFER_DELAY;
        // Ensure the buffer is in chronological order (it is, because we append)
        BufferedState older = default;
        BufferedState newer = default;
        bool found = false;
        for (int i = 0; i < _stateBuffer.Count - 1; i++)
        {
            if (_stateBuffer[i].time <= renderTime && _stateBuffer[i + 1].time >= renderTime)
            {
                older = _stateBuffer[i];
                newer = _stateBuffer[i + 1];
                found = true;
                break;
            }
        }
        if (!found) return false;
        float t = Mathf.InverseLerp(older.time, newer.time, renderTime);
        headPos = Vector3.Lerp(older.headPos, newer.headPos, t);
        if (older.segmentPos != null && newer.segmentPos != null && older.segmentPos.Length == newer.segmentPos.Length)
        {
            segPos = new Vector3[older.segmentPos.Length];
            for (int j = 0; j < segPos.Length; j++)
            {
                segPos[j] = Vector3.Lerp(older.segmentPos[j], newer.segmentPos[j], t);
            }
        }
        return true;
    }

    private void UpdateWithSmoothLerp(Vector3 targetPosition)
    {
        // Update movement history for bezier interpolation
        UpdateMovementHistory(currentPosition);
        
        if (useVelocityBasedMovement)
        {
            // Use velocity-based movement with advanced smoothing
            Vector3 smoothedVelocity = GetSmoothedVelocity(currentPosition, targetPosition, Time.deltaTime);
            currentPosition += smoothedVelocity * Time.deltaTime;
            
            // Apply path interpolation for more natural movement
            Vector3 direction = (targetPosition - currentPosition).normalized;
            float distance = Vector3.Distance(currentPosition, targetPosition);
            float interpolationT = Mathf.Clamp01(distance / (headMaxSpeed * Time.deltaTime));
            
            Vector3 interpolatedPosition = InterpolateAlongPath(currentPosition, targetPosition, direction, interpolationT);
            currentPosition = Vector3.Lerp(currentPosition, interpolatedPosition, pathInterpolationStrength);
        }
        else
        {
            // Fallback to original SmoothDamp for compatibility
            currentPosition = Vector3.SmoothDamp(currentPosition, targetPosition, ref headVelocity, headSmoothTime, headMaxSpeed, Time.deltaTime);
        }
        
        transform.position = currentPosition;
        
        // Keep lerping continuously
        isLerping = true;
    }

    private void UpdateBodySegmentsWithNaturalFollowing()
    {
        if (serverSegments == null) return;
        
        // Don't update segments if snake is dead
        if (!serverIsAlive || !isAlive || hasDied) return;

        // Initialize body segment arrays if needed
        InitializeBodySegmentArrays();

        // Ensure we have enough body segments
        while (bodySegments.Count < serverSegments.Length)
        {
            AddBodySegment();
        }

        // Use advanced path following for body segments with individual velocities
        for (int i = 0; i < serverSegments.Length && i < bodySegments.Count; i++)
        {
            if (bodySegments[i] != null)
            {
                Vector3 currentSegmentPos = bodySegments[i].position;
                Vector3 targetSegmentPos = serverSegments[i];
                
                // Calculate individual segment velocity and speed
                Vector3 segmentDirection = (targetSegmentPos - currentSegmentPos).normalized;
                float segmentDistance = Vector3.Distance(currentSegmentPos, targetSegmentPos);
                
                // Update segment velocity with smoothing
                if (bodySegmentVelocities[i] == Vector3.zero)
                {
                    bodySegmentVelocities[i] = segmentDirection * bodySegmentLerpSpeed;
                }
                else
                {
                    bodySegmentVelocities[i] = Vector3.Lerp(bodySegmentVelocities[i], segmentDirection * bodySegmentLerpSpeed, velocitySmoothing);
                }
                
                // Apply velocity-based movement with easing
                float segmentSpeed = Mathf.Lerp(bodySegmentSpeeds[i], bodySegmentLerpSpeed, 0.1f);
                bodySegmentSpeeds[i] = segmentSpeed;
                
                // Use path interpolation for more natural following
                Vector3 interpolatedPos = InterpolateAlongPath(currentSegmentPos, targetSegmentPos, segmentDirection, 0.5f);
                
                // Apply movement with velocity and path interpolation
                Vector3 newPosition = Vector3.Lerp(currentSegmentPos, interpolatedPos, segmentSpeed * Time.deltaTime);
                bodySegments[i].position = newPosition;
                bodySegments[i].gameObject.SetActive(true);
            }
        }

        // Hide extra segments
        for (int i = serverSegments.Length; i < bodySegments.Count; i++)
        {
            if (bodySegments[i] != null)
                bodySegments[i].gameObject.SetActive(false);
        }
        
        // Visual debugging: show server segment positions in different color
        if (showServerPositions && serverSegments != null)
        {
            ShowServerSegmentPositions();
        }
    }
    
    private void UpdateSegmentTransparency()
    {
        // Keep snake head fully opaque
        SpriteRenderer headRenderer = GetComponent<SpriteRenderer>();
        if (headRenderer != null)
        {
            Color headColor = headRenderer.color;
            headColor.a = 1.0f; // Head always fully opaque
            headRenderer.color = headColor;
        }
        
        // Update the last 3 segments with progressive transparency based on food progress
        int segmentsToUpdate = Mathf.Min(3, bodySegments.Count);
        
        for (int i = 0; i < segmentsToUpdate; i++)
        {
            int segmentIndex = bodySegments.Count - segmentsToUpdate + i;
            var segment = bodySegments[segmentIndex];
            
            if (segment != null)
            {
                SpriteRenderer segmentRenderer = segment.GetComponent<SpriteRenderer>();
                if (segmentRenderer != null)
                {
                    Color segmentColor = segmentRenderer.color;
                    
                    // Calculate transparency based on food progress and segment position
                    float alpha = 1.0f; // Default fully opaque
                    
                    if (foodProgress == 0)
                    {
                        // First food: 50% 75% 85%
                        if (i == 2) alpha = 0.50f;      // Last segment
                        else if (i == 1) alpha = 0.75f; // Second to last
                        else if (i == 0) alpha = 0.85f; // Third to last
                    }
                    else if (foodProgress == 1)
                    {
                        // Second food: 75% 85% 95%
                        if (i == 2) alpha = 0.75f;      // Last segment
                        else if (i == 1) alpha = 0.85f; // Second to last
                        else if (i == 0) alpha = 0.95f; // Third to last
                    }
                    else if (foodProgress == 2)
                    {
                        // Third food: 100% 100% 100%
                        alpha = 1.0f; // All fully opaque
                    }
                    
                    segmentColor.a = alpha;
                    segmentRenderer.color = segmentColor;
                }
            }
        }
        
        // Keep all other segments (older than the last 3) fully opaque
        for (int i = 0; i < bodySegments.Count - 3; i++)
        {
            var segment = bodySegments[i];
            if (segment != null)
            {
                SpriteRenderer segmentRenderer = segment.GetComponent<SpriteRenderer>();
                if (segmentRenderer != null)
                {
                    Color segmentColor = segmentRenderer.color;
                    segmentColor.a = 1.0f; // All older segments fully opaque
                    segmentRenderer.color = segmentColor;
                }
            }
        }
    }
    
    private void UpdateSegmentScaling()
    {
        // Update the scale of all body segments based on segment size (10 times smaller than head)
        for (int i = 0; i < bodySegments.Count; i++)
        {
            var segment = bodySegments[i];
            if (segment != null)
            {
                // Apply scale to the segment (10 times smaller)
                segment.localScale = Vector3.one * segmentSize * 0.1f;
                
                // Update collider radius (also 10 times smaller)
                CircleCollider2D collider = segment.GetComponent<CircleCollider2D>();
                if (collider != null)
                {
                    collider.radius = colliderRadius * segmentSize;
                }
                
                // Re-apply skin to maintain first segment head sprite
                if (SkinManager.Instance != null)
                {
                    bool isFirstSegment = (i == 0); // First body segment (index 0)
                    // Use the snake's specific skin, not the global current skin
                    SnakeSkin snakeSkin = GetSnakeSkin();
                    if (snakeSkin != null)
                    {
                        snakeSkin.ApplyToSegment(segment, isFirstSegment);
                    }
                }
            }
        }
        
        // Keep the head at normal scale
        transform.localScale = Vector3.one * segmentSize;
        
        // Update head collider (normal size)
        CircleCollider2D headCollider = GetComponent<CircleCollider2D>();
        if (headCollider != null)
        {
            headCollider.radius = colliderRadius * segmentSize;
        }
    }

    private void ShowServerSegmentPositions()
    {
        // Create or update debug markers for server segment positions
        for (int i = 0; i < serverSegments.Length; i++)
        {
            // Create a small debug sphere at server segment position
            GameObject debugMarker = GameObject.Find($"ServerSegment_{playerId}_{i}");
            if (debugMarker == null)
            {
                debugMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                debugMarker.name = $"ServerSegment_{playerId}_{i}";
                debugMarker.transform.localScale = Vector3.one * 0.2f; // Small sphere
                
                // Set color to show server position
                Renderer renderer = debugMarker.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = serverPositionColor;
                }
                
                // Remove collider to avoid interference
                Collider collider = debugMarker.GetComponent<Collider>();
                if (collider != null)
                {
                    DestroyImmediate(collider);
                }
            }
            
            // Update position to server segment position
            debugMarker.transform.position = serverSegments[i];
        }
    }

    private void OnGameStateUpdated(NetworkManager.GameState gameState)
    {
        // Queue the update to run on main thread
        mainThreadActions.Enqueue(() => {
            if (gameState.players == null) return;

            // Find this player's data
            var player = gameState.players.FirstOrDefault(p => p.id == playerId);
            if (player == null) return;

            // Extract new state values
            Vector3 newServerPosition = new Vector3(player.x, player.y, 0);
            int newServerLength = player.length;
            int newScore = player.score;
            int newKills = player.kills;
            bool newServerIsAlive = player.isAlive;

            // Store the previous alive state BEFORE updating
            bool wasAlive = serverIsAlive;
            
            // Update state FIRST
            UpdateSnakeState(newServerPosition, newServerLength, newScore, newKills, newServerIsAlive, player);
            
            // THEN check for state changes that require special handling
            bool isNowAlive = newServerIsAlive;
            
            // Handle death state change
            if (wasAlive && !isNowAlive && !hasDied)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"Snake {playerId} death detected - wasAlive: {wasAlive}, isNowAlive: {isNowAlive}");
                #endif
                HandleSnakeDeath();
            }
            else if (!wasAlive && isNowAlive)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"Snake {playerId} revival detected");
                #endif
                HandleSnakeRevival();
            }

            // ---- Add snapshot to interpolation buffer ----
            BufferedState snapshot = new BufferedState
            {
                headPos = serverPosition,
                segmentPos = serverSegments != null ? (Vector3[])serverSegments.Clone() : null,
                time = Time.time
            };
            _stateBuffer.Add(snapshot);
            // Keep only recent states (last 1 second)
            _stateBuffer.RemoveAll(s => Time.time - s.time > 1f);
        });
    }

    private void HandleSnakeDeath()
    {
        // Prevent multiple death calls
        if (hasDied)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"HandleSnakeDeath() called again for snake {playerId} - already dead, ignoring");
            #endif
            return;
        }
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Snake {playerId} death detected from server - triggering destruction");
        Debug.Log($"Previous serverIsAlive: {serverIsAlive}, New serverIsAlive: false");
        Debug.Log($"Current isAlive: {isAlive}, Body segments count: {bodySegments.Count}");
        #endif
        Die();
    }

    private void HandleSnakeRevival()
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Snake {playerId} revival detected");
        #endif
        
        // Reset spawn time for protection on revival
        spawnTime = Time.time;
        
        // Reset death flags
        hasDied = false;
        hasCollided = false;
        collisionCooldown = 0f;
        
        // Show the snake head
        gameObject.SetActive(true);
        
        // Initialize snake again
        InitializeSnake(playerId);
    }

    private void UpdateSnakeState(Vector3 newServerPosition, int newServerLength, int newScore, int newKills, bool newServerIsAlive, NetworkManager.GamePlayer player)
    {
        // Debug logging for death state tracking
        if (serverIsAlive != newServerIsAlive)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"Snake {playerId} server alive state changed: {serverIsAlive} -> {newServerIsAlive}");
            #endif
        }
        
        // Calculate and store movement direction and speed for client-side continuation
        if (hasReceivedFirstUpdate)
        {
            Vector3 movement = newServerPosition - serverPosition;
            if (movement.magnitude > 0.001f)
            {
                lastKnownDirection = movement.normalized;
                // Calculate speed based on movement distance and time
                float timeSinceLastUpdate = Time.time - lastServerUpdateTime;
                if (timeSinceLastUpdate > 0)
                {
                    lastKnownSpeed = movement.magnitude / timeSinceLastUpdate;
                }
            }
        }
        
        // Update basic state
        serverPosition = newServerPosition;
        serverLength = newServerLength;
        score = newScore;
        kills = newKills;
        serverIsAlive = newServerIsAlive;
        isAlive = newServerIsAlive;
        
        // Update food progress
        int newFoodProgress = player.foodProgress;
        if (foodProgress != newFoodProgress)
        {
            foodProgress = newFoodProgress;
            UpdateSegmentTransparency();
        }
        
        // Update segment size
        float newSegmentSize = player.segmentSize;
        if (segmentSize != newSegmentSize)
        {
            segmentSize = newSegmentSize;
            UpdateSegmentScaling();
        }

                // Update server segments
                if (player.segments != null)
                {
                    serverSegments = new Vector3[player.segments.Length];
                    for (int i = 0; i < player.segments.Length; i++)
                    {
                        serverSegments[i] = new Vector3(player.segments[i].x, player.segments[i].y, 0);
                    }
                }

        // Store boost state
        lastIsBoosting = isBoosting;
        lastKnownBoosting = isBoosting;
        lastServerUpdateTime = Time.time;
        
        // Initialize if this is the first update
        if (!hasReceivedFirstUpdate)
        {
            InitializeFirstUpdate();
        }
    }

    private void InitializeFirstUpdate()
    {
        hasReceivedFirstUpdate = true;
        currentPosition = serverPosition;
        lastServerPosition = serverPosition;
        movementDirection = Vector3.right; // Default direction
        movementSpeed = isBoosting ? 6f : 4f; // Use boost state for initial speed
        isMoving = true;
        lastMovementTime = Time.time;
        
        // Initialize lerp for first update
        InitializeSmoothLerp(serverPosition);
    }

    private void InitializeSmoothLerp(Vector3 targetPosition)
    {
        // Start lerp from current position to target position (HEAD ONLY)
        lerpStartPosition = currentPosition;
        lerpTargetPosition = targetPosition;
        
        // Body segments will follow naturally via UpdateBodySegmentsWithNaturalFollowing()
        // No need to initialize segment lerp arrays
        
        // Calculate smoother lerp duration based on expected server update interval
        float expectedServerInterval = 1f / 120f; // 120 FPS server updates
        float actualTimeSinceLastUpdate = Time.time - lastServerUpdateTime;
        
        // Use longer, smoother lerp duration to reduce jerkiness
        float baseDuration = expectedServerInterval * lerpDurationMultiplier; // Longer base duration
        float distance = Vector3.Distance(currentPosition, targetPosition);
        float distanceMultiplier = Mathf.Clamp(distance / 1f, 0.5f, 3f); // Scale based on distance
        
        // Smooth out the duration calculation
        lerpDuration = baseDuration * distanceMultiplier;
        
        // Add extra smoothing time
        lerpDuration += 0.05f; // 50ms extra for smoother feel
        
        lerpStartTime = Time.time;
        isLerping = true;
    }

    public void AddBodySegment()
    {
        // Don't add segments if snake is dead
        if (!serverIsAlive || !isAlive || hasDied)
        {
            Debug.LogWarning($"AddBodySegment() called for dead snake {playerId} - ignoring");
            return;
        }
        
        Vector3 spawnPosition = bodySegments.Count > 0 
            ? bodySegments[bodySegments.Count - 1].position 
            : transform.position;

        GameObject segment = Instantiate(bodySegmentPrefab, spawnPosition, Quaternion.identity);
        segment.transform.SetParent(null); // Parent to snake for better hierarchy
        
        // Set the tag to "Snake" for proper collision detection
        segment.tag = "Snake";
        
        // Add or get BodySegment component
        BodySegment bodySegment = segment.GetComponent<BodySegment>();
        if (bodySegment == null)
        {
            bodySegment = segment.AddComponent<BodySegment>();
        }
        bodySegment.Initialize(playerId, bodySegments.Count);
        
        // Add collider to body segment
        CircleCollider2D collider = segment.GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = segment.AddComponent<CircleCollider2D>();
        }
        collider.isTrigger = true;
        collider.radius = colliderRadius * segmentSize; // Scale collider based on segment size
        collider.offset = Vector2.zero;

        // Add Rigidbody2D to body segment
        Rigidbody2D rb = segment.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = segment.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.isKinematic = true;
        }
        
        // Apply initial scale to the new segment (10 times smaller than head)
        segment.transform.localScale = Vector3.one * segmentSize * 0.1f;
        
        // Apply skin to the new segment using the snake's chosen skin
        SnakeSkin snakeSkin = GetSnakeSkin();
        if (snakeSkin != null)
        {
            // Check if this is the first body segment (index 0)
            bool isFirstSegment = bodySegments.Count == 0;
            
            snakeSkin.ApplyToSegment(segment.transform, isFirstSegment);
            
            // Ensure the first segment has a visible sprite (fallback for missing head sprites)
            if (isFirstSegment)
            {
                SpriteRenderer segmentRenderer = segment.GetComponent<SpriteRenderer>();
                if (segmentRenderer != null && segmentRenderer.sprite == null)
                {
                    // Apply default skin as fallback (not current skin to avoid interference)
                    SnakeSkin defaultSkin = SkinManager.Instance.GetDefaultSkin();
                    if (defaultSkin != null)
                    {
                        defaultSkin.ApplyToSegment(segment.transform, true);
                    }
                }
            }
        }
        
        bodySegments.Add(segment.transform);
        
        // Resize velocities array
        System.Array.Resize(ref bodyVelocities, bodySegments.Count);
        
        // Initialize advanced movement arrays for the new segment
        InitializeBodySegmentArrays();
        
        // Update transparency for the new segment
        UpdateSegmentTransparency();
    }

    public void SetLocalPlayer(bool isLocal)
    {
        isLocalPlayer = isLocal;
    }

    public void SetPlayerId(int id)
    {
        playerId = id;
    }

    public int GetPlayerId()
        {
        return playerId;
        }

    public bool IsLocalPlayer()
    {
        return isLocalPlayer;
    }
    
    /// <summary>
    /// Get the skin for this snake based on its chosen skin ID
    /// </summary>
    /// <returns>The skin for this snake, or null if not found</returns>
    public SnakeSkin GetSnakeSkin()
    {
        if (SkinManager.Instance == null) return null;
        
        // If this snake has a chosen skin ID, use that
        if (chosenSkinId != -1)
        {
            SnakeSkin skin = SkinManager.Instance.GetSkinById(chosenSkinId);
            if (skin != null)
            {
                return skin;
            }
        }
        
        // For bots, fallback to bot skin ID
        if (botSkinId.HasValue)
        {
            SnakeSkin botSkin = SkinManager.Instance.GetSkinById(botSkinId.Value);
            if (botSkin != null)
            {
                return botSkin;
            }
        }
        
        // For local player, use current skin
        if (isLocalPlayer)
        {
            return SkinManager.Instance.GetCurrentSkin();
        }
        
        // For non-local players (other players and bots), use default skin as fallback
        // This prevents interference with the player's current skin selection
        return SkinManager.Instance.GetDefaultSkin();
    }
    
    /// <summary>
    /// Refresh all body segments with the snake's chosen skin
    /// </summary>
    public void RefreshAllSegmentsWithChosenSkin()
    {
        SnakeSkin snakeSkin = GetSnakeSkin();
        if (snakeSkin == null) return;
        
        for (int i = 0; i < bodySegments.Count; i++)
        {
            var segment = bodySegments[i];
            if (segment != null)
            {
                bool isFirstSegment = (i == 0);
                snakeSkin.ApplyToSegment(segment, isFirstSegment);
            }
        }
    }

    public void InitializeSnake(int playerId)
    {
        this.playerId = playerId;
        
        // Set spawn time for protection
        spawnTime = Time.time;
        
        // Clean up any existing segments from previous deaths
        CleanupExistingSegments();
        
        // Clean up any existing debug markers from previous deaths
        CleanupDebugMarkers();
        
        // Initialize transparency
        UpdateSegmentTransparency();
        
        // Removed verbose logging to reduce spam
    }
    
    private void CleanupExistingSegments()
    {
        // Find and destroy any existing body segments for this player
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int cleanedCount = 0;
        
        foreach (var obj in allObjects)
        {
            if (obj != null)
            {
                // Check if it's a body segment for this player by component
                BodySegment bodySegment = obj.GetComponent<BodySegment>();
                if (bodySegment != null && bodySegment.ownerPlayerId == playerId)
                {
                    bodySegment.Cleanup();
                cleanedCount++;
            }
                
                // Also check by name pattern as backup
                if (obj.name.StartsWith($"BodySegment_{playerId}_"))
                {
                    DestroyImmediate(obj);
                    cleanedCount++;
                }
            }
        }
        
        // Only log if we actually cleaned up segments
        if (cleanedCount > 0)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Cleaned up {cleanedCount} existing segments for snake {playerId}");
            #endif
        }
    }

    public void Die()
    {
        if (hasDied)
        {
            // Only log occasionally to reduce spam
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"Die() called again for snake {playerId} - already dead, ignoring");
            #endif
            return;
        }
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Die() called for snake {playerId} - isAlive: {isAlive}, serverIsAlive: {serverIsAlive}");
        Debug.Log($"Snake {playerId} marked as dead - starting cleanup");
        #endif
        
        hasDied = true;
        isAlive = false;
        serverIsAlive = false;
        
        // Create death effect
        CreateDeathEffect();
        
        // Enhanced segment cleanup - destroy all segments for this player
        CleanupAllSegmentsForPlayer();
        
        // Clear the list
        bodySegments.Clear();
        
        // Hide the snake head
        gameObject.SetActive(false);
        
        // Clean up debug markers
        CleanupDebugMarkers();
        
        // Notify UI system of player death (only for local player)
        if (isLocalPlayer)
        {
            NotifyPlayerDeath();
        }
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Snake {playerId} death cleanup completed");
        #endif
    }
    
    private void NotifyPlayerDeath()
    {
        // Use UIManager singleton for consistent UI state management
        if (UIManager.Instance != null)
        {
            // Calculate final position from current game state
            int finalPosition = 0;
            var gameState = NetworkManager.Instance?.GetCurrentGameState();
            if (gameState != null && gameState.players != null)
            {
                var sortedPlayers = gameState.players
                    .Where(p => p.isAlive)
                    .OrderByDescending(p => p.score)
                    .ThenByDescending(p => p.kills)
                    .ToList();
                
                var currentPlayer = gameState.players.FirstOrDefault(p => p.id == playerId);
                if (currentPlayer != null)
                {
                    finalPosition = sortedPlayers.IndexOf(currentPlayer) + 1;
                }
            }
            
            // Only log occasionally to reduce spam - log every 10th death notification
            if (deathLogCounter++ % 10 == 0)
            {
                Debug.Log($"Notifying UI of player death - Score: {score}, Kills: {kills}, Position: {finalPosition}");
            }
            UIManager.Instance.OnPlayerDeath(score, kills, finalPosition);
        }
        else
        {
            Debug.LogError("UIManager.Instance is null! Please ensure UIManager is properly set up.");
        }
    }
    
    private void CleanupAllSegmentsForPlayer()
    {
        // Only log occasionally to reduce spam - log every 10th cleanup or if there are orphaned segments
        bool shouldLog = false;
        
        // First, destroy segments in our list
        for (int i = bodySegments.Count - 1; i >= 0; i--)
        {
            var segment = bodySegments[i];
            if (segment != null)
            {
                // Use the BodySegment's cleanup method if available
                BodySegment bodySegment = segment.GetComponent<BodySegment>();
                if (bodySegment != null)
                {
                    bodySegment.Cleanup();
                }
                else
                {
                DestroyImmediate(segment.gameObject);
            }
        }
        }
        
        // Then, find and destroy ANY remaining segments for this player in the scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int additionalCleanedCount = 0;
        
        foreach (var obj in allObjects)
        {
            if (obj != null)
            {
                // Check if it's a body segment for this player
                BodySegment bodySegment = obj.GetComponent<BodySegment>();
                if (bodySegment != null && bodySegment.ownerPlayerId == playerId)
                {
                    bodySegment.Cleanup();
                    additionalCleanedCount++;
                    shouldLog = true; // Log if we found orphaned segments
                }
                
                // Also check by name pattern as backup
                if (obj.name.StartsWith($"BodySegment_{playerId}_"))
                {
                    DestroyImmediate(obj);
                    additionalCleanedCount++;
                    shouldLog = true; // Log if we found orphaned segments
                }
            }
        }
        
        // Only log if we found orphaned segments or occasionally for normal cleanups
        if (shouldLog || (cleanupLogCounter++ % 10 == 0))
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (additionalCleanedCount > 0)
            {
                Debug.Log($"Cleaned up {bodySegments.Count} listed segments + {additionalCleanedCount} orphaned segments for snake {playerId}");
            }
            else
            {
                Debug.Log($"Cleaned up {bodySegments.Count} segments for snake {playerId}");
            }
            #endif
        }
    }
    
    private void CreateDeathEffect()
    {
        // Check if we're in the game start protection period
        if (UIManager.Instance != null)
        {
            float gameStartTime = UIManager.Instance.GetGameStartTime();
            if (gameStartTime > 0 && (Time.time - gameStartTime) < 1f) // 1 second protection
            {
                float remainingProtection = 1f - (Time.time - gameStartTime);
                Debug.Log($"Ignoring death effect creation during game start protection period. {remainingProtection:F1} seconds remaining.");
                return;
            }
        }
        
        // Use DeathEffectManager for reliable WebGL effects
        if (DeathEffectManager.Instance != null)
        {
            DeathEffectManager.Instance.CreateDeathEffect(transform.position, playerId);
        }
        else
        {
            // Fallback if DeathEffectManager not found
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"DeathEffectManager not found - using fallback effect for snake {playerId}");
            #endif
            
            // Create simple fallback effect
            GameObject deathEffect = new GameObject($"FallbackDeathEffect_{playerId}");
        deathEffect.transform.position = transform.position;
        
        ParticleSystem particles = deathEffect.AddComponent<ParticleSystem>();
        var main = particles.main;
            main.startLifetime = 2.5f;
            main.startSpeed = 4f;
            main.startSize = 0.3f;
            main.maxParticles = 30;
            main.startColor = new Color(1f, 0.4f, 0.2f); // Orange-red
        
        var emission = particles.emission;
        emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });
        
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.8f;
        
        Destroy(deathEffect, 3f);
        }
    }
    
    private void CreateSegmentDestructionEffect(Vector3 position)
    {
        // Check if we're in the game start protection period
        if (UIManager.Instance != null)
        {
            float gameStartTime = UIManager.Instance.GetGameStartTime();
            if (gameStartTime > 0 && (Time.time - gameStartTime) < 1f) // 1 second protection
            {
                float remainingProtection = 1f - (Time.time - gameStartTime);
                Debug.Log($"Ignoring segment destruction effect creation during game start protection period. {remainingProtection:F1} seconds remaining.");
                return;
            }
        }
        
        // Use DeathEffectManager for reliable WebGL effects
        if (DeathEffectManager.Instance != null)
        {
            DeathEffectManager.Instance.CreateSegmentEffect(position, playerId);
        }
        else
        {
            // Fallback if DeathEffectManager not found
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"DeathEffectManager not found - using fallback segment effect for snake {playerId}");
            #endif
            
            // Create simple fallback effect
            GameObject segmentEffect = new GameObject($"FallbackSegmentEffect_{playerId}");
        segmentEffect.transform.position = position;
        
        ParticleSystem particles = segmentEffect.AddComponent<ParticleSystem>();
        var main = particles.main;
            main.startLifetime = 1.5f;
            main.startSpeed = 3f;
            main.startSize = 0.15f;
            main.maxParticles = 8;
            main.startColor = new Color(1f, 1f, 0.2f); // Bright yellow
        
        var emission = particles.emission;
        emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 8) });
        
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.3f;
        
        Destroy(segmentEffect, 2f);
    }
    }

    // Collision detection methods - DISABLED: Server handles all collisions
    // private void OnTriggerEnter2D(Collider2D other)
    // {
    //     // Client-side collision detection removed - server handles all collisions
    // }

    private bool IsSelfCollision(Collider2D other)
    {
        // Check if it's another snake head belonging to us
        SnakeController otherSnake = other.GetComponent<SnakeController>();
        if (otherSnake != null && otherSnake.playerId == this.playerId)
        {
            return true;
        }

        // Check if it's a body segment belonging to us
        BodySegment bodySegment = other.GetComponent<BodySegment>();
        if (bodySegment != null && bodySegment.ownerPlayerId == this.playerId)
        {
            return true;
        }

        // Check if it's one of our own body segments
        if (bodySegments.Contains(other.transform))
        {
            return true;
        }

        // Check if the collider is a child of our snake
        if (other.transform.IsChildOf(this.transform))
        {
            return true;
        }

        return false;
    }

    // HandleCollision method - DISABLED: Server handles all collisions
    // private void HandleCollision(Collider2D other)
    // {
    //     // Client-side collision handling removed - server handles all collisions
    // }

    private System.Collections.IEnumerator ResetCollisionFlag()
    {
        yield return new WaitForSeconds(0.1f);
        hasCollided = false;
    }

    private void UpdateWithClientContinuation(float timeSinceLastUpdate)
    {
        // Continue movement in the last known direction at the last known speed (HEAD ONLY)
        float movementDistance = lastKnownSpeed * timeSinceLastUpdate;
        Vector3 movement = lastKnownDirection * movementDistance;
        
        // Apply advanced smoothing to the movement to reduce jerkiness
        if (useVelocityBasedMovement)
        {
            // Use velocity-based movement with bezier interpolation
            Vector3 smoothedVelocity = GetSmoothedVelocity(currentPosition, currentPosition + movement, Time.deltaTime);
            currentPosition += smoothedVelocity * timeSinceLastUpdate;
            
            // Apply path interpolation for more natural movement
            Vector3 direction = movement.normalized;
            Vector3 interpolatedMovement = InterpolateAlongPath(currentPosition, currentPosition + movement, direction, 0.5f);
            currentPosition = Vector3.Lerp(currentPosition, interpolatedMovement, pathInterpolationStrength);
        }
        else
        {
            // Fallback to original smoothing
            Vector3 smoothedMovement = Vector3.Lerp(Vector3.zero, movement, predictionSmoothness);
            currentPosition += smoothedMovement;
        }
        
        transform.position = currentPosition;
        
        // Update movement direction for rotation
        movementDirection = lastKnownDirection;
        
        // Update body segments with natural following (not continuation)
        UpdateBodySegmentsWithNaturalFollowing();
        
        // Log when we're using client prediction (but not too frequently)
        if (Time.time % 10f < Time.deltaTime) // Log every ~10 seconds instead of every ~2 seconds
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"Using client-side movement continuation for snake {playerId} - Server update delayed by {timeSinceLastUpdate:F2}s");
            #endif
        }
    }

    private void InitializeAdaptiveLerp(Vector3 targetPosition)
    {
        // Start lerp from current position to target position (HEAD ONLY)
        lerpStartPosition = currentPosition;
        lerpTargetPosition = targetPosition;
        
        // Calculate adaptive lerp duration based on distance and expected server interval
        float distance = Vector3.Distance(currentPosition, targetPosition);
        float expectedServerInterval = 1f / 120f; // 120 FPS server updates
        float actualTimeSinceLastUpdate = Time.time - lastServerUpdateTime;
        
        // Use smoother, longer duration calculation with advanced movement
        float baseDuration = expectedServerInterval * lerpDurationMultiplier; // Longer base duration
        float distanceMultiplier = Mathf.Clamp(distance / 1f, 0.5f, 3f); // Scale based on distance
        float timeMultiplier = Mathf.Clamp(actualTimeSinceLastUpdate / expectedServerInterval, 0.5f, 2f);
        
        lerpDuration = baseDuration * distanceMultiplier * timeMultiplier;
        
        // Add extra smoothing time for more fluid movement
        lerpDuration += 0.08f; // 80ms extra for smoother feel
        
        lerpStartTime = Time.time;
        isLerping = true;
        
        // Initialize movement history for bezier interpolation
        if (useCubicBezier)
        {
            UpdateMovementHistory(currentPosition);
        }
        
        // Initialize body segment arrays for advanced movement
        InitializeBodySegmentArrays();
        
        // Body segments will follow naturally via UpdateBodySegmentsWithNaturalFollowing()
        // No need to initialize segment lerp arrays
    }

    // ==== Advanced Movement Functions ====
    
    private Vector3 CubicBezierInterpolation(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        // Cubic Bezier curve interpolation for smooth movement
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;
        
        Vector3 result = uuu * p0;
        result += 3f * uu * t * p1;
        result += 3f * u * tt * p2;
        result += ttt * p3;
        
        return result;
    }
    
    private void GetBezierControlPoints(Vector3 current, Vector3 target, Vector3 direction, out Vector3 control1, out Vector3 control2)
    {
        // Calculate control points for smooth bezier curve
        float distance = Vector3.Distance(current, target);
        float controlDistance = distance * cubicBezierTension;
        
        control1 = current + direction * controlDistance;
        control2 = target - direction * controlDistance;
    }
    
    private float EaseInOutCubic(float t)
    {
        // Smooth easing function for acceleration/deceleration
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
    
    private float EaseInOutQuart(float t)
    {
        // Quartic easing for more pronounced curves
        return t < 0.5f ? 8f * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 4f) / 2f;
    }
    
    private float CustomEasing(float t, float curve)
    {
        // Custom easing with configurable curve
        if (t < 0.5f)
        {
            return Mathf.Pow(2f * t, curve) / 2f;
        }
        else
        {
            return 1f - Mathf.Pow(2f * (1f - t), curve) / 2f;
        }
    }
    
    private void UpdateMovementHistory(Vector3 newPosition)
    {
        // Update movement history for bezier interpolation
        movementHistory[historyIndex] = newPosition;
        historyIndex = (historyIndex + 1) % movementHistory.Length;
    }
    
    private Vector3 GetSmoothedVelocity(Vector3 currentPos, Vector3 targetPos, float deltaTime)
    {
        // Calculate smoothed velocity with acceleration/deceleration curves
        Vector3 rawVelocity = (targetPos - currentPos) / deltaTime;
        
        // Apply velocity smoothing
        smoothedVelocity = Vector3.Lerp(smoothedVelocity, rawVelocity, velocitySmoothing);
        
        // Calculate speed with easing
        float distance = Vector3.Distance(currentPos, targetPos);
        float speed = distance / deltaTime;
        
        // Apply acceleration/deceleration curves
        if (speed > currentSpeed)
        {
            // Accelerating
            currentSpeed = Mathf.Lerp(currentSpeed, speed, CustomEasing(Time.deltaTime, accelerationCurve));
        }
        else
        {
            // Decelerating
            currentSpeed = Mathf.Lerp(currentSpeed, speed, CustomEasing(Time.deltaTime, decelerationCurve));
        }
        
        return smoothedVelocity.normalized * currentSpeed;
    }
    
    private Vector3 InterpolateAlongPath(Vector3 start, Vector3 end, Vector3 direction, float t)
    {
        // Interpolate along a curved path instead of straight line
        if (useCubicBezier && movementHistory[0] != Vector3.zero)
        {
            // Use bezier curve interpolation
            Vector3 control1, control2;
            GetBezierControlPoints(start, end, direction, out control1, out control2);
            return CubicBezierInterpolation(start, control1, control2, end, t);
        }
        else
        {
            // Fallback to smooth lerp with easing
            float easedT = EaseInOutCubic(t);
            return Vector3.Lerp(start, end, easedT);
        }
    }
    
    private void InitializeBodySegmentArrays()
    {
        // Initialize arrays for body segment movement
        if (bodySegmentVelocities == null || bodySegmentVelocities.Length != bodySegments.Count)
        {
            bodySegmentVelocities = new Vector3[bodySegments.Count];
            bodySegmentTargets = new Vector3[bodySegments.Count];
            bodySegmentSpeeds = new float[bodySegments.Count];
        }
    }
} 