using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class SnakeController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float minDistanceToTarget = 0.1f;
    public float boostMultiplier = 1.5f;
    public float boostDuration = 3f; // Duration of boost effect
    public float bodySmoothTime = 0.1f; // Added for smooth body movement

    [Header("Body Settings")]
    public GameObject bodySegmentPrefab;
    public int initialBodySegments = 3;
    public float segmentDistance = 0.5f;
    public float colliderRadius = 0.3f;

    [Header("Network Settings")]
    public float networkUpdateRate = 0.1f;
    public bool isLocalPlayer = true;

    [Header("Game Settings")]
    public int score = 0;
    public int kills = 0;

    private Vector3 targetPosition;
    private List<Transform> bodySegments = new List<Transform>();
    private List<Vector3> positionsHistory = new List<Vector3>();
    private List<Quaternion> rotationsHistory = new List<Quaternion>();
    private float lastNetworkUpdate;
    private bool isBoosting;
    private float currentSpeed;
    private Coroutine boostCoroutine;
    private Vector3[] bodyVelocities; // Added for smooth body movement

    private void Awake()
    {
        Debug.Log("SnakeController Awake");
        // Set the tag
        gameObject.tag = "Snake";

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

        currentSpeed = moveSpeed;
        Debug.Log($"Initial speed set to: {currentSpeed}");
    }

    private void Start()
    {
        Debug.Log("SnakeController Start");
        InitializeSnake();
    }

    private void InitializeSnake()
    {
        // Initialize body velocities array
        bodyVelocities = new Vector3[initialBodySegments];
        for (int i = 0; i < initialBodySegments; i++)
        {
            AddBodySegment();
        }
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            float deltaTime = Time.deltaTime;
            
            // Update target position based on mouse
            UpdateTargetPosition();
            
            // Check for boost input
            HandleBoostInput();
            
            // Move snake
            MoveSnake(deltaTime);
            
            // Update body segments
            UpdateBodySegments(deltaTime);

            // Send network updates
            if (Time.time - lastNetworkUpdate >= networkUpdateRate)
            {
                SendNetworkUpdate();
                lastNetworkUpdate = Time.time;
            }
        }
    }

    private void HandleBoostInput()
    {
        if (Input.GetMouseButtonDown(1)) // RMB pressed
        {
            Debug.Log("RMB Pressed - Starting Boost");
            StartBoost();
        }
        else if (Input.GetMouseButtonUp(1)) // RMB released
        {
            Debug.Log("RMB Released - Stopping Boost");
            StopBoost();
        }
    }

    private void StartBoost()
    {
        isBoosting = true;
        currentSpeed = moveSpeed * boostMultiplier;
        Debug.Log($"Boost started - Speed increased to: {currentSpeed} (Base: {moveSpeed}, Multiplier: {boostMultiplier})");
        
        // Stop any existing boost coroutine
        if (boostCoroutine != null)
        {
            StopCoroutine(boostCoroutine);
        }
        
        // Start new boost coroutine
        boostCoroutine = StartCoroutine(BoostCoroutine());
        
        // Notify server
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.EmitBoost(true);
        }
    }

    private void StopBoost()
    {
        isBoosting = false;
        currentSpeed = moveSpeed;
        Debug.Log($"Boost stopped - Speed returned to: {currentSpeed}");
        
        // Notify server
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.EmitBoost(false);
        }
    }

    private IEnumerator BoostCoroutine()
    {
        Debug.Log($"Boost coroutine started - Will last {boostDuration} seconds");
        yield return new WaitForSeconds(boostDuration);
        if (isBoosting) // Only stop if still boosting
        {
            Debug.Log("Boost duration expired - Stopping boost");
            StopBoost();
        }
    }

    private void UpdateTargetPosition()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;
        targetPosition = mousePosition;
    }

    private void MoveSnake(float deltaTime)
    {
        // Calculate direction to target
        Vector3 direction = (targetPosition - transform.position).normalized;
        
        // Rotate towards target
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * deltaTime);
        
        // Move forward using current speed
        Vector3 movement = transform.right * currentSpeed * deltaTime;
        transform.position += movement;
        
        // Debug movement
        if (isBoosting)
        {
            Debug.Log($"Moving with boost - Speed: {currentSpeed}, Movement: {movement.magnitude}");
        }
        
        // Store position and rotation history
        positionsHistory.Insert(0, transform.position);
        rotationsHistory.Insert(0, transform.rotation);
        
        // Keep history lists at appropriate size
        int maxHistorySize = Mathf.CeilToInt(bodySegments.Count * segmentDistance);
        while (positionsHistory.Count > maxHistorySize)
        {
            positionsHistory.RemoveAt(positionsHistory.Count - 1);
            rotationsHistory.RemoveAt(rotationsHistory.Count - 1);
        }
    }

    private void UpdateBodySegments(float deltaTime)
    {
        for (int i = 0; i < bodySegments.Count; i++)
        {
            int positionIndex = Mathf.FloorToInt(i * segmentDistance);
            if (positionIndex < positionsHistory.Count)
            {
                Vector3 targetPosition = positionsHistory[positionIndex];
                Quaternion targetRotation = rotationsHistory[positionIndex];
                
                // Smoothly move body segment to target position
                bodySegments[i].position = Vector3.SmoothDamp(
                    bodySegments[i].position,
                    targetPosition,
                    ref bodyVelocities[i],
                    bodySmoothTime,
                    Mathf.Infinity,
                    deltaTime
                );
                
                // Smoothly rotate body segment
                bodySegments[i].rotation = Quaternion.Slerp(
                    bodySegments[i].rotation,
                    targetRotation,
                    rotationSpeed * deltaTime
                );
            }
        }
    }

    private void SendNetworkUpdate()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.EmitSnakePosition(transform.position, transform.rotation.eulerAngles.z);
        }
    }

    public void OnFoodEaten(Food food)
    {
        Debug.Log($"Food eaten! Score: {food.ScoreValue}, IsBoost: {food.IsBoost}");
        
        // Update score
        score += food.ScoreValue;

        // Add body segment
        AddBodySegment();

        // If it's a boost food, apply temporary speed boost
        if (food.IsBoost)
        {
            StartCoroutine(ApplyTemporaryBoost());
        }

        // Notify server
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.EmitFoodEaten(food.IsBoost);
        }
    }

    private IEnumerator ApplyTemporaryBoost()
    {
        Debug.Log("Applying temporary boost from food");
        float originalSpeed = currentSpeed;
        currentSpeed *= boostMultiplier;
        Debug.Log($"Temporary boost applied - Speed increased to: {currentSpeed}");
        yield return new WaitForSeconds(boostDuration);
        if (!isBoosting) // Only reset if not manually boosting
        {
            currentSpeed = originalSpeed;
            Debug.Log($"Temporary boost ended - Speed returned to: {currentSpeed}");
        }
    }

    public void AddBodySegment()
    {
        Vector3 spawnPosition = bodySegments.Count > 0 
            ? bodySegments[bodySegments.Count - 1].position 
            : transform.position;

        GameObject segment = Instantiate(bodySegmentPrefab, spawnPosition, Quaternion.identity);
        // Don't parent to snake head
        segment.transform.SetParent(null);
        
        // Add collider to body segment
        CircleCollider2D collider = segment.GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = segment.AddComponent<CircleCollider2D>();
        }
        collider.isTrigger = true;
        collider.radius = colliderRadius;
        collider.offset = Vector2.zero;

        // Add Rigidbody2D to body segment
        Rigidbody2D rb = segment.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = segment.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.isKinematic = true;
        }
        
        bodySegments.Add(segment.transform);
        
        // Resize velocities array
        System.Array.Resize(ref bodyVelocities, bodySegments.Count);
    }

    public void SetLocalPlayer(bool isLocal)
    {
        isLocalPlayer = isLocal;
        Debug.Log($"Set as local player: {isLocal}");
    }

    public void OnKill()
    {
        kills++;
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.EmitKill();
        }
    }

    public void Die()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.EmitDeath();
        }
        // Handle death (e.g., respawn, game over, etc.)
    }
} 