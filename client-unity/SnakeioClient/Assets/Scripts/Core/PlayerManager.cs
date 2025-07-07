using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Header("Player Prefabs")]
    public GameObject playerSnakePrefab;
    public GameObject otherPlayerSnakePrefab;

    [Header("Player Settings")]
    public Color localPlayerColor = Color.blue;
    public Color otherPlayerColor = Color.red;
    public Color botPlayerColor = Color.gray;

    // Player tracking
    private Dictionary<int, GameObject> playerSnakes = new Dictionary<int, GameObject>();
    private int localPlayerId = -1;
    private GameObject localPlayerSnake;
    private bool localPlayerIdSet = false; // Track if we've explicitly set the local player ID

    // Food tracking
    private Dictionary<string, GameObject> foodObjects = new Dictionary<string, GameObject>();
    public GameObject foodPrefab;

    // Thread-safe queue for main thread execution
    private Queue<System.Action> mainThreadActions = new Queue<System.Action>();

    // Simple counters for debug logging throttling
    private int playerLogCounter = 0;
    private int foodLogCounter = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Subscribe to network events
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnGameStateUpdated += OnGameStateUpdated;
            NetworkManager.Instance.OnRoomJoined += OnRoomJoined;
            NetworkManager.Instance.OnRoomLeft += OnRoomLeft;
        }
    }

    private void Update()
    {
        // Execute queued actions on main thread
        while (mainThreadActions.Count > 0)
        {
            var action = mainThreadActions.Dequeue();
            action?.Invoke();
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from network events
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnGameStateUpdated -= OnGameStateUpdated;
            NetworkManager.Instance.OnRoomJoined -= OnRoomJoined;
            NetworkManager.Instance.OnRoomLeft -= OnRoomLeft;
        }
    }

    private void OnRoomJoined(int roomId)
    {
        // Only log occasionally to reduce spam
        if (playerLogCounter % 600 == 0) // Log every 600 frames (about once every 10 seconds)
        {
        Debug.Log($"PlayerManager: Joined room {roomId}");
        }
        // Clear existing players when joining a new room
        mainThreadActions.Enqueue(() => ClearAllPlayers());
    }

    private void OnRoomLeft(int roomId)
    {
        // Only log occasionally to reduce spam
        if (playerLogCounter % 600 == 0) // Log every 600 frames (about once every 10 seconds)
    {
        Debug.Log($"PlayerManager: Left room {roomId}");
        }
        mainThreadActions.Enqueue(() => ClearAllPlayers());
    }

    private void OnGameStateUpdated(NetworkManager.GameState gameState)
    {
        // Queue the update to run on main thread
        mainThreadActions.Enqueue(() => {
            UpdatePlayers(gameState);
            UpdateFood(gameState);
        });
    }

    private void UpdatePlayers(NetworkManager.GameState gameState)
    {
        if (gameState.players == null) return;

        // Track which players we've seen in this update
        HashSet<int> currentPlayerIds = new HashSet<int>();

        foreach (var player in gameState.players)
        {
            currentPlayerIds.Add(player.id);

            // Check if this is a new player
            if (!playerSnakes.ContainsKey(player.id))
            {
                SpawnPlayerSnake(player);
            }

            // Update existing player
            if (playerSnakes.ContainsKey(player.id))
            {
                UpdatePlayerSnake(player);
            }
        }

        // Remove players that are no longer in the game state
        var playersToRemove = playerSnakes.Keys.Where(id => !currentPlayerIds.Contains(id)).ToList();
        foreach (var playerId in playersToRemove)
        {
            RemovePlayerSnake(playerId);
        }
    }

    private void SpawnPlayerSnake(NetworkManager.GamePlayer player)
    {
        // Determine if this is the local player
        bool isLocalPlayer = false;
        
        // If we have a server-provided local player ID, use that
        if (localPlayerIdSet && localPlayerId != -1)
        {
            isLocalPlayer = (player.id == localPlayerId);
        }
        // Fallback: if we haven't set a local player ID yet, use the first non-bot player
        else if (!localPlayerIdSet && localPlayerId == -1)
        {
            if (!player.isBot && player.id > 0) // Only real players (not bots, positive IDs)
            {
                isLocalPlayer = true;
                localPlayerId = player.id;
                localPlayerIdSet = true;
                Debug.Log($"[LOCAL] Set local player ID to: {player.id}");
            }
        }
        else
        {
            // Check if this player matches our local player ID
            isLocalPlayer = (player.id == localPlayerId);
        }
        
        GameObject prefabToUse = isLocalPlayer ? playerSnakePrefab : otherPlayerSnakePrefab;
        if (prefabToUse == null)
        {
            Debug.LogError($"[ERROR] Player snake prefab is null! Local: {isLocalPlayer}");
            return;
        }

        // Spawn the snake
        Vector3 spawnPosition = new Vector3(player.x, player.y, 0);
        GameObject snakeObject = Instantiate(prefabToUse, spawnPosition, Quaternion.identity);
        
        // Set up the snake controller
        SnakeController snakeController = snakeObject.GetComponent<SnakeController>();
        if (snakeController != null)
        {
            snakeController.SetPlayerId(player.id);
            snakeController.SetLocalPlayer(isLocalPlayer);
            
            // Set skin ID BEFORE initializing segments
            SetSnakeVisualProperties(snakeObject, player, isLocalPlayer);
            
            // Now initialize segments with the correct skin
            snakeController.InitializeSnake(player.id);
        }

        // Store the snake
        playerSnakes[player.id] = snakeObject;

        // Track local player
        if (isLocalPlayer)
        {
            localPlayerSnake = snakeObject;
            Debug.Log($"[LOCAL] Local player snake spawned with ID: {player.id}");
        }

        // Only log spawns occasionally for performance
        if (playerLogCounter % 300 == 0) // Log every 300 frames (about once every 5 seconds)
        {
            Debug.Log($"[SPAWN] Spawned snake for player {player.id} - Local: {isLocalPlayer}, Bot: {player.isBot}");
        }
        playerLogCounter++;
    }

    private void UpdatePlayerSnake(NetworkManager.GamePlayer player)
    {
        if (!playerSnakes.ContainsKey(player.id)) return;

        GameObject snakeObject = playerSnakes[player.id];
        if (snakeObject == null) return;

        // Update position and rotation
        Vector3 targetPosition = new Vector3(player.x, player.y, 0);
        snakeObject.transform.position = targetPosition;

        // Update snake controller state
        SnakeController snakeController = snakeObject.GetComponent<SnakeController>();
        if (snakeController != null)
        {
            // The snake controller will handle the visual updates through OnGameStateUpdated
        }

        // Note: Removed SetSnakeVisualProperties from here since skin should only be applied once on spawn
    }

    private void RemovePlayerSnake(int playerId)
    {
        if (playerSnakes.ContainsKey(playerId))
        {
            GameObject snakeObject = playerSnakes[playerId];
            if (snakeObject != null)
            {
                // Trigger proper cleanup on the snake controller
                SnakeController snakeController = snakeObject.GetComponent<SnakeController>();
                if (snakeController != null)
                {
                    // Trigger death cleanup to ensure all segments are destroyed
                    snakeController.Die();
                }
                else
                {
                    // Fallback: destroy immediately
                    DestroyImmediate(snakeObject);
                }
            }
            playerSnakes.Remove(playerId);

            // Clear local player reference if this was the local player
            if (playerId == localPlayerId)
            {
                localPlayerId = -1;
                localPlayerSnake = null;
            }

            // Only log occasionally to reduce spam
            if (playerLogCounter % 300 == 0) // Log every 300 frames (about once every 5 seconds)
            {
            Debug.Log($"Removed snake for player {playerId}");
            }
            playerLogCounter++;
        }
    }

    private void SetSnakeVisualProperties(GameObject snakeObject, NetworkManager.GamePlayer player, bool isLocalPlayer)
    {
        // Apply skin for bots or local player
        if (SkinManager.Instance != null)
        {
            SnakeSkin skin = null;
            
            // For bots, randomly choose from available skins
            if (player.isBot)
            {
                // Get a random skin from available skins
                SnakeSkin randomSkin = SkinManager.Instance.GetRandomSkin();
                if (randomSkin != null)
                {
                    // Store the randomly chosen skin ID in the snake controller
                    SnakeController botSnakeController = snakeObject.GetComponent<SnakeController>();
                    if (botSnakeController != null)
                    {
                        botSnakeController.botSkinId = randomSkin.skinId;
                        botSnakeController.chosenSkinId = randomSkin.skinId;
                    }
                    
                    skin = randomSkin;
                }
                else
                {
                    Debug.LogWarning($"No available skins found for bot {player.id}");
                }
            }
            // For local player, use the current selected skin
            else if (isLocalPlayer)
            {
                skin = SkinManager.Instance.GetCurrentSkin();
                if (skin != null)
                {
                    // Store the current skin ID in the snake controller
                    SnakeController localSnakeController = snakeObject.GetComponent<SnakeController>();
                    if (localSnakeController != null)
                    {
                        localSnakeController.chosenSkinId = skin.skinId;
                        Debug.Log($"Local player assigned skin ID: {skin.skinId}");
                    }
                }
                Debug.Log($"Local player skin: {(skin != null ? skin.skinName : "null")}");
            }
            
            // Apply skin if we have one
            if (skin != null)
            {
                ApplySkinToSnake(snakeObject, skin);
                return; // Skip color-based visual properties when using skin
            }
        }

        // Fallback to color-based visual properties
        Color playerColor;
        if (isLocalPlayer)
        {
            playerColor = localPlayerColor;
        }
        else if (player.isBot)
        {
            playerColor = botPlayerColor;
        }
        else
        {
            playerColor = otherPlayerColor;
        }

        // Apply color to the snake head
        SpriteRenderer headRenderer = snakeObject.GetComponent<SpriteRenderer>();
        if (headRenderer != null)
        {
            headRenderer.color = playerColor;
        }

        // Apply color to body segments
        SnakeController snakeController = snakeObject.GetComponent<SnakeController>();
        if (snakeController != null)
        {
            // The snake controller will handle body segment colors
        }
    }

    private void ApplySkinToSnake(GameObject snakeObject, SnakeSkin skin)
    {
        SnakeController snakeController = snakeObject.GetComponent<SnakeController>();
        if (snakeController != null)
        {
            // Apply skin directly to the snake controller
            skin.ApplyToSnake(snakeController);
        }
    }

    // Helper method to convert hex color string to Unity Color
    private Color HexToColor(string hex)
    {
        if (string.IsNullOrEmpty(hex) || !hex.StartsWith("#"))
        {
            return Color.white; // Default to white if invalid
        }

        // Remove the # if present
        hex = hex.TrimStart('#');

        // Parse the hex values
        if (ColorUtility.TryParseHtmlString("#" + hex, out Color color))
        {
            return color;
        }

        return Color.white; // Default to white if parsing fails
    }

    private void UpdateFood(NetworkManager.GameState gameState)
    {
        if (gameState.food == null) return;

        // Track which food we've seen in this update
        HashSet<string> currentFoodIds = new HashSet<string>();

        foreach (var food in gameState.food)
        {
            currentFoodIds.Add(food.id);

            // Check if this is new food
            if (!foodObjects.ContainsKey(food.id))
            {
                SpawnFood(food);
            }
            else
            {
                UpdateFoodPosition(food);
            }
        }

        // Remove food that's no longer in the game state
        var foodToRemove = foodObjects.Keys.Where(id => !currentFoodIds.Contains(id)).ToList();
        if (foodToRemove.Count > 0)
        {
            // Only log food removal occasionally to reduce spam
            if (foodLogCounter % 300 == 0) // Log every 300 frames (about once every 5 seconds)
            {
                Debug.Log($"Removing {foodToRemove.Count} food items that are no longer in server state");
            }
            foreach (var foodId in foodToRemove)
            {
                RemoveFood(foodId);
            }
        }
        
        // Only log food counts occasionally to reduce spam
        if (foodLogCounter % 600 == 0) // Log every 600 frames (about once every 10 seconds)
        {
            var serverFoodIds = gameState.food.Select(f => f.id).ToList();
            var clientFoodIds = foodObjects.Keys.ToList();
            Debug.Log($"Food sync: Server={serverFoodIds.Count}, Client={clientFoodIds.Count}");
        }
        foodLogCounter++;
    }

    private void SpawnFood(NetworkManager.GameFood food)
    {
        if (foodPrefab == null)
        {
            Debug.LogError("Food prefab is null!");
            return;
        }

        Vector3 spawnPosition = new Vector3(food.x, food.y, 0);
        GameObject foodObject = Instantiate(foodPrefab, spawnPosition, Quaternion.identity);
        
        // Set food size if specified
        if (food.size.HasValue && food.size.Value != 1.0f)
        {
            foodObject.transform.localScale = Vector3.one * food.size.Value;
        }
        
        // Set food properties
        SpriteRenderer renderer = foodObject.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            // Set color based on server-provided color or fallback to green
            if (!string.IsNullOrEmpty(food.color))
            {
                renderer.color = HexToColor(food.color);
            }
            else
            {
                renderer.color = Color.green; // Fallback color
            }
            
            // Set sorting order to render food under snake sprites
            renderer.sortingOrder = -10; // Lower than snake segments which are 2-9
        }

        foodObjects[food.id] = foodObject;
        
        // Only log occasionally for performance
        if (foodLogCounter % 600 == 0) // Log every 600 frames (about once every 10 seconds)
        {
            Debug.Log($"Spawned food {food.id} at ({food.x:F1}, {food.y:F1}) with size {food.size?.ToString() ?? "default"}");
        }
        foodLogCounter++;
    }

    private void UpdateFoodPosition(NetworkManager.GameFood food)
    {
        if (!foodObjects.ContainsKey(food.id)) return;

        GameObject foodObject = foodObjects[food.id];
        if (foodObject != null)
        {
            Vector3 targetPosition = new Vector3(food.x, food.y, 0);
            foodObject.transform.position = targetPosition;
        }
    }

    private void RemoveFood(string foodId)
    {
        if (foodObjects.ContainsKey(foodId))
        {
            GameObject foodObject = foodObjects[foodId];
            if (foodObject != null)
            {
                // Check if food is already being collected (has collection effect)
                Food foodComponent = foodObject.GetComponent<Food>();
                if (foodComponent != null)
                {
                    // Start delayed destruction for food that wasn't collected
                    // This handles cases where server removes food but no snake collected it
                    foodComponent.StartDelayedDestruction(10f); // 10 second delay
                }
                else
                {
                    // Fallback: destroy immediately if no food component
                    Destroy(foodObject);
                }
            }
            foodObjects.Remove(foodId);
            // Only log occasionally to reduce spam
            if (foodLogCounter % 300 == 0) // Log every 300 frames (about once every 5 seconds)
            {
                Debug.Log($"Removed food {foodId}");
            }
        }
    }
    
    /// <summary>
    /// Remove food by GameObject reference (used by collection effects)
    /// </summary>
    public void RemoveFoodByGameObject(GameObject foodObject)
    {
        if (foodObject == null) return;
        
        // Find the food ID by searching through the dictionary
        string foodIdToRemove = null;
        foreach (var kvp in foodObjects)
        {
            if (kvp.Value == foodObject)
            {
                foodIdToRemove = kvp.Key;
                break;
            }
        }
        
        if (foodIdToRemove != null)
        {
            foodObjects.Remove(foodIdToRemove);
        }
    }

    public void SetLocalPlayerId(int playerId)
    {
        localPlayerId = playerId;
        localPlayerIdSet = true;
        Debug.Log($"Manually set local player ID to: {playerId}");
        
        // Update existing snake if it exists
        if (playerSnakes.ContainsKey(playerId))
        {
            GameObject snakeObject = playerSnakes[playerId];
            SnakeController snakeController = snakeObject.GetComponent<SnakeController>();
            if (snakeController != null)
            {
                snakeController.SetLocalPlayer(true);
                localPlayerSnake = snakeObject;
                Debug.Log($"Updated existing snake {playerId} to be local player");
            }
        }
    }

    private void ClearAllPlayers()
    {
        // First, trigger proper cleanup on all snake controllers
        foreach (var snake in playerSnakes.Values)
        {
            if (snake != null)
            {
                SnakeController snakeController = snake.GetComponent<SnakeController>();
                if (snakeController != null)
                {
                    // Trigger death cleanup to ensure all segments are destroyed
                    snakeController.Die();
                }
                else
                {
                    // Fallback: destroy immediately
                    DestroyImmediate(snake);
                }
            }
        }
        
        // Clear the dictionary
        playerSnakes.Clear();
        localPlayerSnake = null;
        localPlayerId = -1;
        localPlayerIdSet = false;

        // Only log occasionally to reduce spam
        if (playerLogCounter % 600 == 0) // Log every 600 frames (about once every 10 seconds)
        {
            Debug.Log("Clearing all players");
        }
        playerLogCounter++;
    }

    // Public methods for external access
    public GameObject GetLocalPlayerSnake()
    {
        return localPlayerSnake;
    }

    public int GetLocalPlayerId()
    {
        return localPlayerId;
    }

    public bool IsLocalPlayerIdSet()
    {
        return localPlayerIdSet;
    }

    public bool IsLocalPlayer(int playerId)
    {
        return playerId == localPlayerId && localPlayerIdSet;
    }

    // Debug methods
    public void DebugLocalPlayerInfo()
    {
        Debug.Log("=== LOCAL PLAYER DEBUG INFO ===");
        Debug.Log($"localPlayerId: {localPlayerId}");
        Debug.Log($"localPlayerIdSet: {localPlayerIdSet}");
        Debug.Log($"localPlayerSnake: {(localPlayerSnake != null ? localPlayerSnake.name : "null")}");
        Debug.Log($"Total playerSnakes: {playerSnakes.Count}");
        
        foreach (var kvp in playerSnakes)
        {
            int playerId = kvp.Key;
            GameObject snake = kvp.Value;
            SnakeController controller = snake?.GetComponent<SnakeController>();
            Debug.Log($"Player {playerId}: Snake={snake?.name}, IsLocal={controller?.isLocalPlayer}, IsAlive={controller?.isAlive}");
        }
        Debug.Log("=== END LOCAL PLAYER DEBUG INFO ===");
    }
    
    public void DebugSetLocalPlayer(int playerId)
    {
        Debug.Log($"Manually setting local player to ID: {playerId}");
        SetLocalPlayerId(playerId);
        DebugLocalPlayerInfo();
    }
} 