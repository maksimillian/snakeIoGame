using UnityEngine;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public CinemachineVirtualCamera virtualCamera;
    public float followSpeed = 8f;
    public float cameraDistance = 10f;
    
    [Header("Auto Follow Settings")]
    public bool autoFollowLocalPlayer = true;
    public float searchInterval = 0.1f;
    
    private GameObject localPlayerSnake;
    private float lastSearchTime;
    
    private void Start()
    {
        // If no virtual camera assigned, try to find one in the scene
        if (virtualCamera == null)
        {
            virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
            if (virtualCamera == null)
            {
                Debug.LogError("No CinemachineVirtualCamera found in scene!");
                return;
            }
        }
        
        Debug.Log("CameraController initialized - will auto-follow local player snake");
    }
    
    private void Update()
    {
        if (!autoFollowLocalPlayer || virtualCamera == null) return;
        
        // Search for local player more frequently
        if (Time.time - lastSearchTime >= searchInterval)
        {
            FindAndFollowLocalPlayer();
            lastSearchTime = Time.time;
        }
    }
    
    private void FindAndFollowLocalPlayer()
    {
        // Try to get local player from PlayerManager first
        if (PlayerManager.Instance != null)
        {
            GameObject localSnake = PlayerManager.Instance.GetLocalPlayerSnake();
            if (localSnake != null && localSnake != localPlayerSnake)
            {
                SetCameraTarget(localSnake);
                return;
            }
        }
        
        // Fallback: search for snake with local player tag or component
        if (localPlayerSnake == null)
        {
            var snakeControllers = FindObjectsOfType<SnakeController>();
            foreach (var controller in snakeControllers)
            {
                // Look for non-bot players with positive IDs (real players)
                if (controller.isLocalPlayer && controller.playerId > 0 && controller.isAlive)
                {
                    SetCameraTarget(controller.gameObject);
                    return;
                }
            }
        }
        else
        {
            // Check if current target is still valid
            SnakeController currentController = localPlayerSnake.GetComponent<SnakeController>();
            if (currentController == null || !currentController.isAlive || currentController.playerId <= 0)
            {
                // Current target is invalid, clear it and search for a new one
                localPlayerSnake = null;
                if (virtualCamera != null)
                {
                    virtualCamera.Follow = null;
                    virtualCamera.LookAt = null;
                }
            }
        }
    }
    
    private void SetCameraTarget(GameObject target)
    {
        if (target == null || virtualCamera == null) return;
        
        localPlayerSnake = target;
        virtualCamera.Follow = target.transform;
        virtualCamera.LookAt = target.transform;
        
        Debug.Log($"Camera now following: {target.name} (Player ID: {target.GetComponent<SnakeController>()?.playerId})");
    }
    
    // Public method to manually set camera target
    public void SetTarget(GameObject target)
    {
        SetCameraTarget(target);
    }
    
    // Public method to enable/disable auto follow
    public void SetAutoFollow(bool enabled)
    {
        autoFollowLocalPlayer = enabled;
        if (!enabled)
        {
            // Clear current target
            if (virtualCamera != null)
            {
                virtualCamera.Follow = null;
                virtualCamera.LookAt = null;
            }
            localPlayerSnake = null;
        }
    }
    
    // Method to get current camera target
    public GameObject GetCurrentTarget()
    {
        return localPlayerSnake;
    }
    
    // Method to check if camera is following a target
    public bool IsFollowingTarget()
    {
        return localPlayerSnake != null && virtualCamera != null && virtualCamera.Follow != null;
    }
} 