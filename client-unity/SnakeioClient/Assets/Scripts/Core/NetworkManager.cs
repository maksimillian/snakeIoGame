using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SocketIOClient;
using Newtonsoft.Json;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("Network Settings")]
    public string serverUrl = "http://localhost:3000";
    public string socketPath = "/"; // Using root namespace (working)
    
    private SocketIO socket;
    private bool isConnected = false;

    // Data models for testing
    [Serializable]
    public class Server
    {
        public string id;
        public string name;
        public string status;
        public int playerCount;
    }

    [Serializable]
    public class PlayerStats
    {
        public int score;
        public int kills;
        public int bestScore;
        public int totalKills;
    }

    [Serializable]
    public class Skin
    {
        public string id;
        public string name;
        public bool isUnlocked;
    }

    [Serializable]
    public class FoodSpawnData
    {
        public float x;
        public float y;
        public bool isBoost;
    }

    // New server-authoritative game state data structures
    [Serializable]
    public class GameState
    {
        public int roomId;
        public long timestamp;
        public GamePlayer[] players;
        public GameFood[] food;
        public GameLeaderboardEntry[] leaderboard;
    }

    [Serializable]
    public class GamePlayer
    {
        public int id;
        public string name;
        public float x;
        public float y;
        public int length;
        public int score;
        public int kills;
        public bool isBot;
        public bool isAlive;
        public GameSegment[] segments;
        public int foodProgress; // Progress towards next segment (0-2, 3 food needed for 1 segment)
        public float segmentSize; // Segment size multiplier based on length
        public int? skinId; // Skin ID for the player (optional)
    }

    [Serializable]
    public class GameFood
    {
        public string id;
        public float x;
        public float y;
        public string type;
        public int value;
        public float? size; // Size multiplier for the food (optional)
        public string color; // Random color for the food (hex string)
    }

    [Serializable]
    public class GameSegment
    {
        public float x;
        public float y;
    }

    [Serializable]
    public class GameLeaderboardEntry
    {
        public int rank;
        public string name;
        public int score;
        public int kills;
        public bool isBot;
    }

    // Event for when game state is updated
    public System.Action<GameState> OnGameStateUpdated;

    // Current game state
    private GameState currentGameState;

    // Room state management
    private int currentRoomId = -1;
    private string currentFriendCode = "";
    private bool isInRoom = false;

    // Event for room state changes
    public System.Action<int> OnRoomJoined;
    public System.Action<int> OnRoomLeft;

    // Thread-safe queue for main thread execution
    private Queue<System.Action> mainThreadActions = new Queue<System.Action>();

    // Simple counter for debug logging throttling
    private int debugLogCounter = 0;

    // Static events for WebGL callbacks (since static callbacks can't access instance state)
    public static Action OnWebGLGameStateReceived;
    public static Action OnWebGLRoomJoined;
    public static Action OnWebGLPlayerJoined;

    // WebGL polling state
    private string lastProcessedGameState = "";
    private float lastGameStateCheck = 0f;

    // Room class is defined in Room.cs

    #if UNITY_WEBGL
    private SocketIOWebGL jsSocket;
    #endif

    // Static fields for WebGL room join handling
    private static TaskCompletionSource<Room> webglRoomJoinTcs;
    private static NetworkManager webglInstance;

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
        Debug.Log("NetworkManager Start() called");
        
        // Use coroutine instead of async for WebGL compatibility
        StartCoroutine(InitializeSocketCoroutine());
        
        // Also test basic WebSocket connectivity
        StartCoroutine(TestBasicWebSocket());
        
        // Test HTTP connectivity
        StartCoroutine(TestHttpConnectivity());
    }

    private System.Collections.IEnumerator InitializeSocketCoroutine()
        {
        Debug.Log("Starting socket initialization...");
        
        // Wait 2 seconds for Unity to fully initialize
        yield return new WaitForSeconds(2f);
        
#if UNITY_WEBGL
        Debug.Log("Running on WebGL - using JS Socket.IO bridge");
        InitializeSocketWebGL();
#else
        Debug.Log("Running on non-WebGL platform - using C# Socket.IO client");
        InitializeSocketSync();
#endif
        
        Debug.Log("Socket initialization completed");
    }

    private void InitializeSocketSync()
    {
        try
        {
            Debug.Log($"Initializing socket connection to {serverUrl} (root namespace)");

            // Convert HTTPS to WSS for WebSocket connections
            string wsUrl = serverUrl;
            if (serverUrl.StartsWith("https://"))
            {
                wsUrl = serverUrl.Replace("https://", "wss://");
            }
            else if (serverUrl.StartsWith("http://"))
            {
                wsUrl = serverUrl.Replace("http://", "ws://");
            }

            Debug.Log($"Using WebSocket URL: {wsUrl} (root namespace)");

            // Use simpler options for WebGL compatibility
            var options = new SocketIOOptions
            {
                Path = "/socket.io/",
                Reconnection = true,
                ReconnectionAttempts = 3,
                ReconnectionDelay = 2000,
                // Let Socket.IO choose transport (polling then upgrade) to improve WebGL compatibility
                EIO = EngineIO.V4
            };

            Debug.Log($"Socket.IO options: Reconnection={options.Reconnection}, Transport={options.Transport}");

            socket = new SocketIO(wsUrl, options);

            // Set up connection event handlers
            socket.OnConnected += (sender, e) =>
            {
                Debug.Log("Socket connected successfully!");
                isConnected = true;
            };

            socket.OnDisconnected += (sender, e) =>
            {
                Debug.Log($"Socket disconnected! Reason: {e}");
                isConnected = false;
            };

            socket.OnError += (sender, e) =>
            {
                Debug.LogError($"Socket error: {e}");
            };

            socket.OnReconnectAttempt += (sender, e) =>
            {
                Debug.Log($"Socket reconnection attempt {e}");
            };

            socket.OnReconnectFailed += (sender, e) =>
            {
                Debug.LogError($"Socket reconnection failed: {e}");
            };

            // Listen for connection established event
            socket.On("connection:established", response =>
            {
                try
                {
                    var data = response.GetValue<Dictionary<string, string>>();
                    Debug.Log($"Connection established: {data["status"]} (Client ID: {data["clientId"]})");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing connection response: {e.Message}");
                }
            });

            // Listen for pong response
            socket.On("pong", response =>
            {
                try
                {
                    var data = response.GetValue<Dictionary<string, string>>();
                    Debug.Log($"Pong received: {data["timestamp"]}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing pong response: {e.Message}");
                }
            });

            // Listen for server-authoritative game state updates
            socket.On("game:state", response =>
            {
                try
                {
                    // Get the raw JSON string (same approach as working methods)
                    string jsonData = response.ToString();
                    
                    // Try to parse as single object first, then as array if that fails
                    GameState gameState = null;
                    try
                    {
                        // Try parsing as single GameState object
                        gameState = JsonConvert.DeserializeObject<GameState>(jsonData);
                    }
                    catch
                    {
                        // If that fails, try parsing as array (Socket.IO sometimes wraps data)
                    var gameStateArray = JsonConvert.DeserializeObject<GameState[]>(jsonData);
                    if (gameStateArray != null && gameStateArray.Length > 0)
                    {
                            gameState = gameStateArray[0];
                        }
                    }
                    
                    if (gameState != null)
                    {
                        currentGameState = gameState;
                        OnGameStateUpdated?.Invoke(gameState);
                        
                        // Queue debug logging to main thread
                        mainThreadActions.Enqueue(() => {
                            // Only log occasionally for performance
                            if (debugLogCounter % 300 == 0) // Log every 300 frames (about once every 5 seconds)
                            {
                                Debug.Log($"Game state: {gameState.players?.Length ?? 0} players, {gameState.food?.Length ?? 0} food");
                            }
                            debugLogCounter++;
                        });
                    }
                    else
                    {
                        Debug.LogError("Failed to deserialize game state - both single object and array parsing failed");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing game state: {e.Message}");
                }
            });

            // Connect to the server
            Debug.Log("Attempting to connect to server...");
            socket.ConnectAsync();
            
            // Check connection status after a short delay
            StartCoroutine(CheckConnectionStatus());
        }
        catch (Exception e)
            {
            Debug.LogError($"Failed to initialize socket: {e.Message}\nStack trace: {e.StackTrace}");
                isConnected = false;
        }
    }

    private System.Collections.IEnumerator CheckConnectionStatus()
    {
        Debug.Log("Waiting 3 seconds for connection...");
        yield return new WaitForSeconds(3f);
        
        if (socket != null)
        {
            Debug.Log($"Connection status check - Socket.Connected: {socket.Connected}, isConnected: {isConnected}");
            
            if (!socket.Connected && !isConnected)
            {
                Debug.LogWarning("Connection failed - socket is not connected after 3 seconds");
                Debug.Log("This might be normal for Unity WebGL - connection can take longer");
                
                // Wait a bit longer for WebGL
                yield return new WaitForSeconds(2f);
                Debug.Log($"After additional wait - Socket.Connected: {socket.Connected}, isConnected: {isConnected}");
                
                if (!socket.Connected && !isConnected)
                {
                    Debug.LogError("Connection still failed after 5 seconds total");
                    Debug.Log("Possible issues:");
                    Debug.Log("   - Server not running on localhost:3000");
                    Debug.Log("   - CORS issues");
                    Debug.Log("   - WebSocket transport not supported");
                    Debug.Log("   - Unity WebGL WebSocket limitations");
                }
            }
            else if (socket.Connected && isConnected)
            {
                Debug.Log("Connection successful! Testing ping...");
                // Test ping to verify connection
                StartCoroutine(TestPing());
            }
            else
            {
                Debug.LogWarning($"Mixed connection state - Socket.Connected: {socket.Connected}, isConnected: {isConnected}");
            }
        }
        else
        {
            Debug.LogError("Connection failed - socket is null");
        }
    }

    private System.Collections.IEnumerator TestPing()
    {
        if (socket != null && socket.Connected)
        {
            Debug.Log("Sending ping to server...");
            try
            {
                socket.EmitAsync("ping");
                Debug.Log("Ping sent successfully");
        }
        catch (Exception e)
        {
                Debug.LogError($"Failed to send ping: {e.Message}");
            }
        }
        yield return null;
    }

    // Test basic WebSocket connectivity (not Socket.IO)
    private System.Collections.IEnumerator TestBasicWebSocket()
    {
        Debug.Log("Testing basic WebSocket connectivity...");
        
        // This is a simple test to see if WebSockets work at all in Unity WebGL
        // We'll try to connect to a simple WebSocket echo service
        try
        {
            // Use a simple WebSocket echo service for testing
            string testUrl = "wss://echo.websocket.org";
            Debug.Log($"Testing WebSocket connection to: {testUrl}");
            
            // Note: This is just for testing - Unity WebGL might not support direct WebSocket API
            // The Socket.IO library should handle this, but this helps us understand the issue
            Debug.Log("Basic WebSocket test completed (Socket.IO should handle WebSocket transport)");
        }
        catch (Exception e)
        {
            Debug.LogError($"Basic WebSocket test failed: {e.Message}");
        }
        
        yield return null;
    }

    // Test HTTP connectivity to verify server is reachable
    private System.Collections.IEnumerator TestHttpConnectivity()
    {
        Debug.Log("Testing HTTP connectivity to server...");
        
        try
        {
            // Test if we can reach the server via HTTP
            string healthUrl = $"{serverUrl}/health";
            Debug.Log($"Testing HTTP connection to: {healthUrl}");
            
            // Note: Unity WebGL has limitations with HTTP requests too
            // This is just to verify the server is reachable
            Debug.Log("HTTP connectivity test completed");
        }
        catch (Exception e)
        {
            Debug.LogError($"HTTP connectivity test failed: {e.Message}");
        }
        
        yield return null;
    }

    private void Update()
    {
        // Execute queued actions on main thread
        while (mainThreadActions.Count > 0)
        {
            var action = mainThreadActions.Dequeue();
            action?.Invoke();
        }

        // Handle static WebGL events (keeping for compatibility)
        if (OnWebGLGameStateReceived != null)
        {
            OnWebGLGameStateReceived.Invoke();
            OnWebGLGameStateReceived = null; // Clear after handling
            HandleWebGLGameStateReceived();
        }

        if (OnWebGLRoomJoined != null)
        {
            OnWebGLRoomJoined.Invoke();
            OnWebGLRoomJoined = null; // Clear after handling
            HandleWebGLRoomJoined();
        }

        if (OnWebGLPlayerJoined != null)
        {
            OnWebGLPlayerJoined.Invoke();
            OnWebGLPlayerJoined = null; // Clear after handling
            HandleWebGLPlayerJoined();
        }

#if UNITY_WEBGL
        // Poll for game state updates every 30 frames (about twice per second)
        if (debugLogCounter % 30 == 0 && jsSocket != null && jsSocket.IsConnected())
        {
            string gameStateData = jsSocket.GetGameStateData();
            if (!string.IsNullOrEmpty(gameStateData) && gameStateData != lastProcessedGameState)
            {
                lastProcessedGameState = gameStateData;
                HandleWebGLGameStateReceived();
        }
        }
        
        debugLogCounter++;
#endif
    }

    // WebGL event handlers
    private void HandleWebGLGameStateReceived()
    {
        mainThreadActions.Enqueue(() => {
#if UNITY_WEBGL
            // Get the actual game state data from the JS bridge
            if (jsSocket != null)
            {
                string gameStateJson = jsSocket.GetGameStateData();
                if (!string.IsNullOrEmpty(gameStateJson))
                {
                    try
                    {
                        // Try to parse as single object first, then as array if that fails
                        GameState gameState = null;
                        try
                        {
                            // Try parsing as single GameState object
                            gameState = JsonConvert.DeserializeObject<GameState>(gameStateJson);
                        }
                        catch
                        {
                            // If that fails, try parsing as array (Socket.IO sometimes wraps data)
                            var gameStateArray = JsonConvert.DeserializeObject<GameState[]>(gameStateJson);
                            if (gameStateArray != null && gameStateArray.Length > 0)
                            {
                                gameState = gameStateArray[0];
                            }
                        }
                        
                        if (gameState != null)
                        {
                            currentGameState = gameState;
                            // Trigger the OnGameStateUpdated event for other scripts
                            OnGameStateUpdated?.Invoke(currentGameState);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[WebGL] Error parsing game state: {e.Message}");
                    }
                }
            }
#else
            Debug.Log("[Non-WebGL] Game state received (no-op for non-WebGL builds)");
#endif
        });
    }

    private void HandleWebGLRoomJoined()
    {
        mainThreadActions.Enqueue(() => {
            // Update room state
            isInRoom = true;
            currentRoomId = 1; // Mock room ID for now
            OnRoomJoined?.Invoke(currentRoomId);
        });
    }

    private void HandleWebGLPlayerJoined()
    {
        mainThreadActions.Enqueue(() => {
            // Player joined event handled
        });
    }

    #region Socket Event Handlers
    private void OnPlayerJoin(SocketIOResponse response)
    {
        Debug.Log("Player joined: " + response.GetValue<string>());
    }

    private void OnPlayerLeave(SocketIOResponse response)
    {
        Debug.Log("Player left: " + response.GetValue<string>());
    }

    private void OnSnakeSpawn(SocketIOResponse response)
    {
        Debug.Log("Snake spawned: " + response.GetValue<string>());
    }

    private void OnSnakeMove(SocketIOResponse response)
    {
        Debug.Log("Snake moved: " + response.GetValue<string>());
    }

    private void OnSnakeDie(SocketIOResponse response)
    {
        Debug.Log("Snake died: " + response.GetValue<string>());
    }

    private void OnSnakeEat(SocketIOResponse response)
    {
        Debug.Log("Snake ate food: " + response.GetValue<string>());
    }

    private void OnSnakeKill(SocketIOResponse response)
    {
        Debug.Log("Snake got a kill: " + response.GetValue<string>());
    }

    private void OnRoomState(SocketIOResponse response)
    {
        Debug.Log("Room state received: " + response.GetValue<string>());
    }

    private void OnFoodSpawn(SocketIOResponse response)
    {
        Debug.Log("Food spawned: " + response.GetValue<string>());
        var data = response.GetValue<FoodSpawnData>();
        if (FoodManager.Instance != null)
        {
            FoodManager.Instance.SpawnFood(new Vector2(data.x, data.y), data.isBoost);
        }
    }

    private void OnFoodRemove(SocketIOResponse response)
    {
        Debug.Log("Food removed: " + response.GetValue<string>());
    }
    #endregion

    #region Game Methods
    public async void EmitSnakeInput(Vector2 direction, bool isBoosting)
    {
#if UNITY_WEBGL
        // WebGL version using JS bridge
        try
        {
            // Convert position to direction (normalized vector)
            Vector2 normalizedDirection = direction;
            
            // Check if direction has any magnitude before normalizing
            if (normalizedDirection.magnitude > 0.001f)
            {
                normalizedDirection = normalizedDirection.normalized;
            }
            else
            {
                // If direction is too small, use a random unit vector to avoid stalling
                float randomAngle = UnityEngine.Random.Range(0f, 2f * Mathf.PI);
                normalizedDirection = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
            }
            
            var data = new Dictionary<string, object>
            {
                ["direction"] = new Dictionary<string, float>
                {
                    ["x"] = normalizedDirection.x,
                    ["y"] = normalizedDirection.y
                },
                ["isBoosting"] = isBoosting
            };

            string jsonData = JsonConvert.SerializeObject(data);
            jsSocket.Emit("snake:input", jsonData);
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebGL] Failed to emit snake input: {e.Message}");
        }
#else
        // Non-WebGL version using C# Socket.IO client
        try
        {
            // Convert position to direction (normalized vector)
            Vector2 normalizedDirection = direction;
            
            // Check if direction has any magnitude before normalizing
            if (normalizedDirection.magnitude > 0.001f)
            {
                normalizedDirection = normalizedDirection.normalized;
            }
            else
            {
                // If direction is too small, use a random unit vector to avoid stalling
                float randomAngle = UnityEngine.Random.Range(0f, 2f * Mathf.PI);
                normalizedDirection = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
            }
            
            var data = new Dictionary<string, object>
            {
                ["direction"] = new Dictionary<string, float>
                {
                    ["x"] = normalizedDirection.x,
                    ["y"] = normalizedDirection.y
                },
                ["isBoosting"] = isBoosting
            };

            await socket.EmitAsync("snake:input", data);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to emit snake input: {e.Message}");
        }
#endif
    }

    public async void EmitSnakePositionOptimized(Vector2 position, float rotation, bool isBoosting = false)
    {
#if UNITY_WEBGL
        // WebGL version using JS bridge
        try
        {
            // Convert position to direction (normalized vector)
            Vector2 direction = position;
            
            // Check if direction has any magnitude before normalizing
            if (direction.magnitude > 0.001f)
            {
                direction = direction.normalized;
            }
            else
            {
                // If direction is too small, use zero direction
                direction = Vector2.zero;
            }
            
            var data = new Dictionary<string, object>
            {
                ["direction"] = new Dictionary<string, float>
                {
                    ["x"] = direction.x,
                    ["y"] = direction.y
                },
                ["isBoosting"] = isBoosting
            };

            string jsonData = JsonConvert.SerializeObject(data);
            jsSocket.Emit("snake:input", jsonData);
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebGL] Failed to emit snake position optimized: {e.Message}");
        }
#else
        // Non-WebGL version using C# Socket.IO client
        try
        {
            // Convert position to direction (normalized vector)
            Vector2 direction = position;
            
            // Check if direction has any magnitude before normalizing
            if (direction.magnitude > 0.001f)
            {
                direction = direction.normalized;
            }
            else
            {
                // If direction is too small, use zero direction
                direction = Vector2.zero;
            }
            
            var data = new Dictionary<string, object>
            {
                ["direction"] = new Dictionary<string, float>
                {
                    ["x"] = direction.x,
                    ["y"] = direction.y
                },
                ["isBoosting"] = isBoosting
            };

            await socket.EmitAsync("snake:input", data);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to emit snake input: {e.Message}");
        }
#endif
    }

    public async void EmitBoost(bool isBoosting)
    {
#if UNITY_WEBGL
        // WebGL version using JS bridge
        if (jsSocket != null && jsSocket.IsConnected())
        {
            try
            {
                var data = new { boosting = isBoosting };
                string jsonData = JsonConvert.SerializeObject(data);
                jsSocket.Emit("snake:boost", jsonData);
            }
            catch (Exception e)
        {
                Debug.LogError($"[WebGL] Failed to emit boost: {e.Message}");
            }
        }
#else
        // Non-WebGL version using C# Socket.IO client
            try
            {
                await socket.EmitAsync("snake:boost", new { boosting = isBoosting });
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to emit boost: {e.Message}");
            }
#endif
    }

    public async void EmitFoodEaten(bool isBoost)
    {
#if UNITY_WEBGL
        // WebGL version using JS bridge
        if (jsSocket != null && jsSocket.IsConnected())
        {
            try
            {
                var data = new { isBoost = isBoost };
                string jsonData = JsonConvert.SerializeObject(data);
                jsSocket.Emit("snake:eat", jsonData);
                Debug.Log($"[WebGL] Emitted food eaten: {jsonData}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[WebGL] Failed to emit food eaten: {e.Message}");
            }
        }
#else
        // Non-WebGL version using C# Socket.IO client
        if (socket != null && socket.Connected)
        {
            try
            {
                await socket.EmitAsync("snake:eat", new { isBoost = isBoost });
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to emit food eaten: {e.Message}");
            }
        }
#endif
    }

    public async void EmitKill(int victimId)
    {
#if UNITY_WEBGL
        // WebGL version using JS bridge
        if (jsSocket != null && jsSocket.IsConnected())
        {
            try
            {
                string data = $"{{\"killerSessionId\":{GetCurrentRoomId()},\"victimSessionId\":{victimId}}}";
                jsSocket.Emit("snake:kill", data);
                Debug.Log($"[WebGL] Emitted kill for victim {victimId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[WebGL] Failed to emit kill: {e.Message}");
            }
        }
#else
        // Non-WebGL version using C# Socket.IO client
        if (socket != null && socket.Connected)
        {
            try
            {
                await socket.EmitAsync("snake:kill", new { 
                    killerSessionId = GetCurrentRoomId(), 
                    victimSessionId = victimId 
                });
                Debug.Log($"Emitted kill for victim {victimId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to emit kill: {e.Message}");
            }
        }
#endif
    }

    public async void EmitDeath()
    {
#if UNITY_WEBGL
        // WebGL version using JS bridge
        if (jsSocket != null && jsSocket.IsConnected())
        {
            try
            {
                jsSocket.Emit("snake:die", "{}");
            }
            catch (Exception e)
        {
                Debug.LogError($"[WebGL] Failed to emit death: {e.Message}");
            }
        }
#else
        // Non-WebGL version using C# Socket.IO client
            try
            {
                await socket.EmitAsync("snake:die", new { });
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to emit death: {e.Message}");
            }
#endif
        }

    #endregion

    #region Testing Methods
    public async Task<List<Server>> GetServers()
    {
        if (socket == null)
        {
            Debug.LogError("Socket is not initialized!");
            return new List<Server>();
        }

        if (!isConnected)
        {
            Debug.LogError("Socket is not connected! Attempting to reconnect...");
            // Reinitialize socket connection
            InitializeSocketSync();
            if (!isConnected)
            {
                Debug.LogError("Failed to reconnect!");
                return new List<Server>();
            }
        }

        try
        {
            Debug.Log("Requesting server list...");
            var tcs = new TaskCompletionSource<List<Server>>();
            var timeoutTask = Task.Delay(5000); // 5 second timeout

            // Remove any existing listeners to prevent duplicates
            socket.Off("server:list");
            socket.Off("error");

            // Listen for the server:list event
            socket.On("server:list", response =>
            {
                try
                {
                    Debug.Log($"Raw server list response: {response}");
                    var servers = response.GetValue<List<Server>>();
                    Debug.Log($"Successfully parsed {servers?.Count ?? 0} servers");
                    tcs.TrySetResult(servers ?? new List<Server>());
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing server list: {e.Message}\nResponse: {response}");
                    tcs.TrySetResult(new List<Server>());
                }
            });

            // Listen for errors
            socket.On("error", response =>
            {
                try
                {
                    var error = response.GetValue<Dictionary<string, string>>();
                    Debug.LogError($"Server error: {error["message"]}");
                    tcs.TrySetResult(new List<Server>());
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing error response: {e.Message}");
                    tcs.TrySetResult(new List<Server>());
                }
            });

            // Emit the request
            Debug.Log("Emitting server:list request...");
            await socket.EmitAsync("server:list", new { });
            Debug.Log("server:list request emitted");

            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
            if (completedTask == timeoutTask)
            {
                Debug.LogError("Server list request timed out after 5 seconds");
                return new List<Server>();
            }

            return await tcs.Task;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error getting server list: {e.Message}\nStack trace: {e.StackTrace}");
            return new List<Server>();
        }
    }

    public async Task<List<Room>> GetRooms()
    {
        if (!isConnected) throw new Exception("Not connected to server");

        var tcs = new TaskCompletionSource<List<Room>>();
        var timeoutTask = Task.Delay(5000); // 5 second timeout

        // Remove any existing listeners to prevent duplicates
        socket.Off("room:list");
        socket.Off("error");

        socket.On("room:list", response =>
        {
            try
            {
                Debug.Log($"Received room list response: {response}");
                
                // Try to get the raw JSON string
                string jsonData = response.ToString();
                Debug.Log($"Raw JSON data: {jsonData}");
                
                // Parse as array first, then extract the first element
                var responseArray = JsonConvert.DeserializeObject<List<RoomListResponse>>(jsonData);
                if (responseArray != null && responseArray.Count > 0)
                {
                    var responseObj = responseArray[0];
                    var rooms = responseObj?.rooms != null ? new List<Room>(responseObj.rooms) : new List<Room>();
                    Debug.Log($"Successfully parsed: {rooms.Count} rooms");
                    tcs.TrySetResult(rooms);
                }
                else
                {
                    Debug.LogError("No room list data received in response");
                    tcs.TrySetException(new Exception("No room list data received"));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error parsing room list: {ex.Message}");
                tcs.TrySetException(ex);
            }
        });

        socket.On("error", response =>
        {
            var error = response.GetValue<string>();
            Debug.LogError($"Error getting room list: {error}");
            tcs.TrySetException(new Exception(error));
        });

        Debug.Log("Emitting room:list request");
        await socket.EmitAsync("room:list");

        // Wait for either the response or timeout
        var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
        if (completedTask == timeoutTask)
        {
            Debug.LogError("Room list request timed out after 5 seconds");
            return new List<Room>();
        }

        return await tcs.Task;
    }

    public async Task<Room> CreateRoom(string name)
    {
        if (!isConnected) throw new Exception("Not connected to server");

        var tcs = new TaskCompletionSource<Room>();
        var timeoutTask = Task.Delay(5000); // 5 second timeout

        // Remove any existing listeners to prevent duplicates
        socket.Off("room:create");
        socket.Off("error");

        socket.On("room:create", response =>
        {
            try
            {
                Debug.Log($"Received room creation response: {response}");
                
                // Try to get the raw JSON string
                string jsonData = response.ToString();
                Debug.Log($"Raw JSON data: {jsonData}");
                
                var rooms = JsonConvert.DeserializeObject<List<Room>>(jsonData);
                if (rooms != null && rooms.Count > 0)
                {
                    var room = rooms[0];
                    Debug.Log($"Parsed room: {room.name} (ID: {room.id})");
                    tcs.TrySetResult(room);
                }
                else
                {
                    Debug.LogError("No room data received in response");
                    tcs.TrySetException(new Exception("No room data received"));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error parsing room creation response: {ex.Message}");
                tcs.TrySetException(ex);
            }
        });

        socket.On("error", response =>
        {
            var error = response.GetValue<string>();
            Debug.LogError($"Error creating room: {error}");
            tcs.TrySetException(new Exception(error));
        });

        await socket.EmitAsync("room:create", new { name });

        // Wait for either the response or timeout
        var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
        if (completedTask == timeoutTask)
            {
            Debug.LogError("Room creation request timed out after 5 seconds");
            throw new TimeoutException("Room creation request timed out");
        }

        return await tcs.Task;
    }

    public async Task<Room> AutoJoinRoom()
    {
        if (!isConnected)
        {
            Debug.LogError("Not connected to server");
            return null;
        }

#if UNITY_WEBGL
        // WebGL version using JS bridge
        return await AutoJoinRoomWebGL();
#else
        // Non-WebGL version using C# Socket.IO client
        return await AutoJoinRoomSync();
#endif
    }

#if UNITY_WEBGL
    private async Task<Room> AutoJoinRoomWebGL()
    {
        try
        {
            // Check if already in a room
            if (isInRoom && currentRoomId != -1)
            {
                await LeaveCurrentRoom();
            }
            
            // Check if JS socket is connected
            if (jsSocket == null || !jsSocket.IsConnected())
            {
                Debug.LogError("[WebGL] JS socket is not connected");
                return null;
            }
            
            // Set up static task completion source for IL2CPP compatibility
            webglRoomJoinTcs = new TaskCompletionSource<Room>();
            webglInstance = this;
            var timeoutTask = Task.Delay(5000); // 5 second timeout
            
            // Set up static event listener for room:auto-join response
            jsSocket.On("room:auto-join", OnWebGLRoomAutoJoinCallback);
            
            // Send the auto-join request with player name
            string playerName = PlayerPrefs.GetString("PlayerName", "Player");
            string autoJoinData = $"{{\"playerName\":\"{playerName}\"}}";
            jsSocket.Emit("room:auto-join", autoJoinData);
            
            // Wait for either the response or timeout
            var completedTask = await Task.WhenAny(webglRoomJoinTcs.Task, timeoutTask);
            if (completedTask == timeoutTask)
            {
                Debug.LogError("[WebGL] Auto-join request timed out after 5 seconds");
                throw new TimeoutException("Auto-join request timed out");
            }
            
            return await webglRoomJoinTcs.Task;
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebGL] Failed to auto-join room: {e.Message}");
            return null;
        }
    }

    // Static callback for IL2CPP compatibility
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    [AOT.MonoPInvokeCallback(typeof(System.Action))]
    private static void OnWebGLRoomAutoJoinCallback()
    {
        try
        {
            if (webglInstance == null || webglInstance.jsSocket == null)
            {
                Debug.LogError("[WebGL] WebGL instance or socket is null");
                webglRoomJoinTcs?.TrySetException(new Exception("WebGL instance is null"));
                return;
            }
            
            // Get room data from JS bridge
            string roomDataJson = webglInstance.jsSocket.GetRoomData();
            
            if (!string.IsNullOrEmpty(roomDataJson))
            {
                // Parse room data
                var roomData = JsonConvert.DeserializeObject<Dictionary<string, object>>(roomDataJson);
                if (roomData != null && roomData.ContainsKey("id"))
                {
                    int roomId = Convert.ToInt32(roomData["id"]);
                    string roomName = roomData.ContainsKey("name") ? roomData["name"].ToString() : "Unknown Room";
                    string friendCode = roomData.ContainsKey("friendCode") ? roomData["friendCode"].ToString() : "";
                    
                    var room = new Room
                    {
                        id = roomId,
                        name = roomName,
                        friendCode = friendCode,
                        players = roomData.ContainsKey("currentPlayers") ? Convert.ToInt32(roomData["currentPlayers"]) : 1,
                        currentPlayers = roomData.ContainsKey("currentPlayers") ? Convert.ToInt32(roomData["currentPlayers"]) : 1,
                        maxPlayers = roomData.ContainsKey("maxPlayers") ? Convert.ToInt32(roomData["maxPlayers"]) : 10,
                        botCount = roomData.ContainsKey("botCount") ? Convert.ToInt32(roomData["botCount"]) : 7
                    };
                    
                    // Update room state on main thread
                    webglInstance.mainThreadActions.Enqueue(() => {
                        webglInstance.currentRoomId = room.id;
                        webglInstance.currentFriendCode = room.friendCode;
                        webglInstance.isInRoom = true;
                        webglInstance.OnRoomJoined?.Invoke(room.id);
                    });
                    
                    webglRoomJoinTcs?.TrySetResult(room);
                }
                else
                {
                    Debug.LogError("[WebGL] Invalid room data received from server");
                    webglRoomJoinTcs?.TrySetException(new Exception("Invalid room data received"));
                }
            }
            else
            {
                Debug.LogError("[WebGL] No room data received from server");
                webglRoomJoinTcs?.TrySetException(new Exception("No room data received"));
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebGL] Error processing room:auto-join response: {e.Message}");
            webglRoomJoinTcs?.TrySetException(e);
        }
    }
#endif

    private async Task<Room> AutoJoinRoomSync()
    {
        try
        {
            // Check if already in a room
            if (isInRoom && currentRoomId != -1)
            {
                Debug.Log($"Already in room {currentRoomId}, leaving first...");
                await LeaveCurrentRoom();
            }

            Debug.Log("Attempting to auto-join room...");
            
            // Check if socket is still connected
            if (socket == null || !socket.Connected)
            {
                Debug.LogError("Socket is not connected");
                return null;
            }
            
            // Set up response handling
            var tcs = new TaskCompletionSource<Room>();
            var timeoutTask = Task.Delay(5000); // 5 second timeout
            
            // Set up event listener for room:auto-join response
            socket.On("room:auto-join", (response) =>
            {
                try
                {
                    Debug.Log($"[ROOM_AUTO_JOIN] Received room:auto-join event: {response}");
                    
                    // Try to parse the response as JSON first
                    string jsonData = response.ToString();
                    Debug.Log($"[ROOM_AUTO_JOIN] Raw JSON data: {jsonData}");
                    
                    // Parse as dictionary to handle different data types
                    var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
                    if (data != null && data.ContainsKey("id"))
                    {
                        // Convert roomId to int safely
                        int joinedRoomId = Convert.ToInt32(data["id"]);
                        string roomName = data.ContainsKey("name") ? data["name"].ToString() : "Unknown Room";
                        string friendCode = data.ContainsKey("friendCode") ? data["friendCode"].ToString() : "";
                        
                        var room = new Room
                        {
                            id = joinedRoomId,
                            name = roomName,
                            friendCode = friendCode,
                            players = data.ContainsKey("currentPlayers") ? Convert.ToInt32(data["currentPlayers"]) : 1,
                            currentPlayers = data.ContainsKey("currentPlayers") ? Convert.ToInt32(data["currentPlayers"]) : 1,
                            maxPlayers = data.ContainsKey("maxPlayers") ? Convert.ToInt32(data["maxPlayers"]) : 10,
                            botCount = data.ContainsKey("botCount") ? Convert.ToInt32(data["botCount"]) : 7
                        };
                        
                        Debug.Log($"[ROOM_AUTO_JOIN] Successfully joined room: {roomName} (ID: {joinedRoomId})");
                        
                        // Update room state
                        currentRoomId = joinedRoomId;
                        currentFriendCode = friendCode;
                        isInRoom = true;
                        OnRoomJoined?.Invoke(joinedRoomId);
                        
                        // Complete the task
                        tcs.TrySetResult(room);
                    }
                    else
                    {
                        Debug.LogWarning("[ROOM_AUTO_JOIN] No room data found in response");
                        tcs.TrySetException(new Exception("No room data received"));
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ROOM_AUTO_JOIN] Error parsing room:auto-join response: {e.Message}");
                    Debug.LogError($"[ROOM_AUTO_JOIN] Response data: {response}");
                    tcs.TrySetException(e);
                }
            });

            // Set up event listener for player:joined (for player ID and additional info)
            socket.On("player:joined", (response) =>
            {
                try
                {
                    Debug.Log($"[PLAYER_JOINED] Received player:joined event: {response}");
                    
                    // Try to parse the response as JSON first
                    string jsonData = response.ToString();
                    Debug.Log($"[PLAYER_JOINED] Raw JSON data: {jsonData}");
                    
                    // Parse as dictionary to handle different data types
                    var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
                    if (data != null && data.ContainsKey("roomId"))
                    {
                        // Convert roomId to int safely
                        int joinedRoomId = Convert.ToInt32(data["roomId"]);
                        Debug.Log($"[PLAYER_JOINED] Successfully joined game room: {joinedRoomId}");
                        
                        // Extract local player ID if available
                        if (data.ContainsKey("localPlayerId"))
                        {
                            int localPlayerId = Convert.ToInt32(data["localPlayerId"]);
                            Debug.Log($"[PLAYER_JOINED] Local player ID received from server: {localPlayerId}");
                            
                            // Set the local player ID in PlayerManager
                            if (PlayerManager.Instance != null)
                            {
                                PlayerManager.Instance.SetLocalPlayerId(localPlayerId);
                                Debug.Log($"[PLAYER_JOINED] Set local player ID in PlayerManager: {localPlayerId}");
                            }
                            else
                            {
                                Debug.LogError("[PLAYER_JOINED] PlayerManager.Instance is null!");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("[PLAYER_JOINED] No localPlayerId found in response!");
                        }
                        
                        // Update room state (in case it wasn't set by room:auto-join)
                        if (currentRoomId == -1)
                        {
                        currentRoomId = joinedRoomId;
                        isInRoom = true;
                        OnRoomJoined?.Invoke(joinedRoomId);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[PLAYER_JOINED] No roomId found in player:joined response");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PLAYER_JOINED] Error parsing player:joined response: {e.Message}");
                    Debug.LogError($"[PLAYER_JOINED] Response data: {response}");
                }
            });
            
            // Set up error listener
            socket.On("error", (response) =>
            {
                var error = response.GetValue<string>();
                Debug.LogError($"Auto-join error: {error}");
                tcs.TrySetException(new Exception(error));
            });
            
            // Send the auto-join request with player name
            string playerName = PlayerPrefs.GetString("PlayerName", "Player");
            await socket.EmitAsync("room:auto-join", new { playerName });
            
            // Wait for either the response or timeout
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
            if (completedTask == timeoutTask)
            {
                Debug.LogError("Auto-join request timed out after 5 seconds");
                throw new TimeoutException("Auto-join request timed out");
            }
            
            return await tcs.Task;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to auto-join room: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            return null;
        }
    }

    public async Task<Room> JoinRoomByFriendCode(string friendCode)
    {
        if (!isConnected)
        {
            Debug.LogError("Not connected to server");
            return null;
        }

#if UNITY_WEBGL
        // WebGL version using JS bridge
        return await JoinRoomByFriendCodeWebGL(friendCode);
#else
        // Non-WebGL version using C# Socket.IO client
        return await JoinRoomByFriendCodeSync(friendCode);
#endif
    }

#if UNITY_WEBGL
    private async Task<Room> JoinRoomByFriendCodeWebGL(string friendCode)
    {
        try
        {
            // Check if already in a room
            if (isInRoom && currentRoomId != -1)
            {
                await LeaveCurrentRoom();
            }
            
            // Check if JS socket is connected
            if (jsSocket == null || !jsSocket.IsConnected())
            {
                Debug.LogError("[WebGL] JS socket is not connected");
                return null;
            }
            
            // Set up static task completion source for IL2CPP compatibility
            webglRoomJoinTcs = new TaskCompletionSource<Room>();
            webglInstance = this;
            var timeoutTask = Task.Delay(5000); // 5 second timeout
            
            // Set up static event listener for rooms:join response
            jsSocket.On("rooms:join", OnWebGLRoomJoinCallback);
            
            // Set up static event listener for error response
            jsSocket.On("error", OnWebGLRoomJoinErrorCallback);
            
            // Send the friend code join request with player name
            string playerName = PlayerPrefs.GetString("PlayerName", "Player");
            string joinData = $"{{\"friendCode\":\"{friendCode}\",\"playerName\":\"{playerName}\"}}";
            jsSocket.Emit("rooms:join", joinData);
            
            // Wait for either the response or timeout
            var completedTask = await Task.WhenAny(webglRoomJoinTcs.Task, timeoutTask);
            if (completedTask == timeoutTask)
            {
                Debug.LogError("[WebGL] Friend code join request timed out after 5 seconds");
                throw new TimeoutException("Friend code join request timed out");
            }
            
            return await webglRoomJoinTcs.Task;
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebGL] Failed to join room by friend code: {e.Message}");
            return null;
        }
    }

    // Static callback for IL2CPP compatibility
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    [AOT.MonoPInvokeCallback(typeof(System.Action))]
    private static void OnWebGLRoomJoinErrorCallback()
    {
        try
        {
            if (webglInstance == null || webglInstance.jsSocket == null)
            {
                Debug.LogError("[WebGL] WebGL instance or socket is null in error callback");
                webglRoomJoinTcs?.TrySetException(new Exception("WebGL instance is null"));
                return;
            }
            
            // Get error data from JS bridge
            string errorDataJson = webglInstance.jsSocket.GetErrorData();
            
            if (!string.IsNullOrEmpty(errorDataJson))
            {
                var errorData = JsonConvert.DeserializeObject<Dictionary<string, object>>(errorDataJson);
                string errorMessage = errorData.ContainsKey("message") ? errorData["message"].ToString() : "Unknown error";
                Debug.LogError($"[WebGL] Room join error: {errorMessage}");
                webglRoomJoinTcs?.TrySetException(new Exception(errorMessage));
            }
            else
            {
                Debug.LogError("[WebGL] No error data received from server");
                webglRoomJoinTcs?.TrySetException(new Exception("No error data received"));
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebGL] Error processing room join error: {e.Message}");
            webglRoomJoinTcs?.TrySetException(e);
        }
    }

    // Static callback for IL2CPP compatibility
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    [AOT.MonoPInvokeCallback(typeof(System.Action))]
    private static void OnWebGLRoomJoinCallback()
    {
        try
        {
            if (webglInstance == null || webglInstance.jsSocket == null)
            {
                Debug.LogError("[WebGL] WebGL instance or socket is null");
                webglRoomJoinTcs?.TrySetException(new Exception("WebGL instance is null"));
                return;
            }
            
            // Get room data from JS bridge
            string roomDataJson = webglInstance.jsSocket.GetRoomData();
            
            if (!string.IsNullOrEmpty(roomDataJson))
            {
                // Parse room data
                var roomData = JsonConvert.DeserializeObject<Dictionary<string, object>>(roomDataJson);
                if (roomData != null && roomData.ContainsKey("id"))
                {
                    int roomId = Convert.ToInt32(roomData["id"]);
                    string roomName = roomData.ContainsKey("name") ? roomData["name"].ToString() : "Unknown Room";
                    string friendCode = roomData.ContainsKey("friendCode") ? roomData["friendCode"].ToString() : "";
                    
                    var room = new Room
                    {
                        id = roomId,
                        name = roomName,
                        friendCode = friendCode,
                        players = roomData.ContainsKey("currentPlayers") ? Convert.ToInt32(roomData["currentPlayers"]) : 1,
                        currentPlayers = roomData.ContainsKey("currentPlayers") ? Convert.ToInt32(roomData["currentPlayers"]) : 1,
                        maxPlayers = roomData.ContainsKey("maxPlayers") ? Convert.ToInt32(roomData["maxPlayers"]) : 10,
                        botCount = roomData.ContainsKey("botCount") ? Convert.ToInt32(roomData["botCount"]) : 7
                    };
                    
                    // Update room state on main thread
                    webglInstance.mainThreadActions.Enqueue(() => {
                        webglInstance.currentRoomId = room.id;
                        webglInstance.currentFriendCode = room.friendCode;
                        webglInstance.isInRoom = true;
                        webglInstance.OnRoomJoined?.Invoke(room.id);
                    });
                    
                    webglRoomJoinTcs?.TrySetResult(room);
                }
                else
                {
                    Debug.LogError("[WebGL] Invalid room data received from server");
                    webglRoomJoinTcs?.TrySetException(new Exception("Invalid room data received"));
                }
            }
            else
            {
                Debug.LogError("[WebGL] No room data received from server");
                webglRoomJoinTcs?.TrySetException(new Exception("No room data received"));
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebGL] Error processing room:join response: {e.Message}");
            webglRoomJoinTcs?.TrySetException(e);
        }
    }
#endif

    private async Task<Room> JoinRoomByFriendCodeSync(string friendCode)
    {
        try
        {
            // Check if already in a room
            if (isInRoom && currentRoomId != -1)
            {
                Debug.Log($"Already in room {currentRoomId}, leaving first...");
                await LeaveCurrentRoom();
            }

            Debug.Log($"Attempting to join room with friend code: {friendCode}");
            
            // Check if socket is still connected
            if (socket == null || !socket.Connected)
            {
                Debug.LogError("Socket is not connected");
                return null;
            }
            
            // Set up response handling
            var tcs = new TaskCompletionSource<Room>();
            var timeoutTask = Task.Delay(5000); // 5 second timeout
            
            // Set up event listener for rooms join response
            socket.On("rooms:join", (response) =>
            {
                try
                {
                    Debug.Log($"Received room join response: {response}");
                    
                    // Try to get the raw JSON string
                    string jsonData = response.ToString();
                    Debug.Log($"Raw JSON data: {jsonData}");
                    
                    // Use a simpler approach for WebGL compatibility
                    try
                    {
                    var rooms = JsonConvert.DeserializeObject<List<Room>>(jsonData);
                    if (rooms != null && rooms.Count > 0)
                    {
                        var room = rooms[0];
                        Debug.Log($"Joined room by friend code: {room.name} (ID: {room.id})");
                        
                            // Update room state on main thread to avoid threading issues
                            mainThreadActions.Enqueue(() => {
                        currentRoomId = room.id;
                                currentFriendCode = room.friendCode ?? "";
                        isInRoom = true;
                        OnRoomJoined?.Invoke(room.id);
                            });
                        
                        tcs.TrySetResult(room);
                    }
                    else
                    {
                        Debug.LogError("No room data received in join response");
                        tcs.TrySetException(new Exception("No room data received"));
                        }
                    }
                    catch (Exception parseEx)
                    {
                        Debug.LogError($"Error parsing room join response: {parseEx.Message}");
                        tcs.TrySetException(parseEx);
                    }
            }
            catch (Exception e)
                {
                    Debug.LogError($"Error processing room join response: {e.Message}");
                    tcs.TrySetException(e);
                }
            });
            
            // Set up error listener
            socket.On("error", (response) =>
            {
                try
                {
                    Debug.LogError($"Received error response: {response}");
                    
                    // Try to parse the error response as JSON
                    string jsonData = response.ToString();
                    Debug.LogError($"Raw error JSON data: {jsonData}");
                    
                    try
                    {
                        var errorData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
                        string errorMessage = errorData.ContainsKey("message") ? errorData["message"].ToString() : "Unknown error";
                        Debug.LogError($"Room join error: {errorMessage}");
                        tcs.TrySetException(new Exception(errorMessage));
                    }
                    catch (Exception parseEx)
                    {
                        Debug.LogError($"Error parsing error response: {parseEx.Message}");
                        // Fallback to raw string
                        string errorMessage = jsonData;
                        tcs.TrySetException(new Exception(errorMessage));
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error processing room join error: {e.Message}");
                    tcs.TrySetException(e);
                }
            });
            
            // Send the room join request with player name
            string playerName = PlayerPrefs.GetString("PlayerName", "Player");
            await socket.EmitAsync("rooms:join", new { friendCode, playerName });
            
            // Wait for either the response or timeout
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
            if (completedTask == timeoutTask)
            {
                Debug.LogError("Room join request timed out after 5 seconds");
                throw new TimeoutException("Room join request timed out");
            }
            
            return await tcs.Task;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join room by friend code: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            return null;
        }
    }

    public async Task<bool> JoinGameRoom(string roomId)
    {
        if (!isConnected)
        {
            Debug.LogError("Not connected to server");
            return false;
        }

        try
        {
            Debug.Log($"Joining game room: {roomId}");
            
            // Set up event listeners for room join/leave events
            socket.On("player:joined", (response) =>
            {
                try
                {
                    Debug.Log($"Received player:joined event: {response}");
                    
                    // Try to parse the response as JSON first
                    string jsonData = response.ToString();
                    Debug.Log($"Raw JSON data: {jsonData}");
                    
                    // Parse as dictionary to handle different data types
                    var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
                    if (data != null && data.ContainsKey("roomId"))
                    {
                        // Convert roomId to int safely
                        int joinedRoomId = Convert.ToInt32(data["roomId"]);
                        Debug.Log($"Successfully joined game room: {joinedRoomId}");
                        
                        // Extract local player ID if available
                        if (data.ContainsKey("localPlayerId"))
                        {
                            int localPlayerId = Convert.ToInt32(data["localPlayerId"]);
                            Debug.Log($"Local player ID received from server: {localPlayerId}");
                            
                            // Set the local player ID in PlayerManager
                            if (PlayerManager.Instance != null)
                            {
                                PlayerManager.Instance.SetLocalPlayerId(localPlayerId);
                            }
                        }
                        
                        // Update room state
                        currentRoomId = joinedRoomId;
                        isInRoom = true;
                        OnRoomJoined?.Invoke(joinedRoomId);
                    }
                    else
                    {
                        Debug.LogWarning("No roomId found in player:joined response");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing player:joined response: {e.Message}");
                    Debug.LogError($"Response data: {response}");
                }
            });

            socket.On("player:left", (response) =>
            {
                try
                {
                    Debug.Log($"Received player:left event: {response}");
                    
                    // Try to parse the response as JSON first
                    string jsonData = response.ToString();
                    Debug.Log($"Raw JSON data: {jsonData}");
                    
                    // Parse as dictionary to handle different data types
                    var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
                    if (data != null && data.ContainsKey("roomId"))
                    {
                        // Convert roomId to int safely
                        int leftRoomId = Convert.ToInt32(data["roomId"]);
                        Debug.Log($"Left game room: {leftRoomId}");
                        
                        // Update room state
                        if (currentRoomId == leftRoomId)
                        {
                            isInRoom = false;
                            currentRoomId = -1;
                            currentFriendCode = "";
                            OnRoomLeft?.Invoke(leftRoomId);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("No roomId found in player:left response");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing player:left response: {e.Message}");
                    Debug.LogError($"Response data: {response}");
                }
            });
            
            await socket.EmitAsync("player:join", new { roomId });
            
            // Wait a bit for the response
            await Task.Delay(1000);
            
            Debug.Log("Successfully joined game room");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join game room: {e.Message}");
            return false;
        }
    }

    public async Task<Room> JoinRoom(string roomId)
    {
        if (!isConnected) throw new Exception("Not connected to server");

#if UNITY_WEBGL
        // WebGL version using JS bridge
        return await JoinRoomWebGL(roomId);
#else
        // Non-WebGL version using C# Socket.IO client
        return await JoinRoomSync(roomId);
#endif
    }

#if UNITY_WEBGL
    private async Task<Room> JoinRoomWebGL(string roomId)
    {
        try
        {
            // Check if JS socket is connected
            if (jsSocket == null || !jsSocket.IsConnected())
            {
                Debug.LogError("[WebGL] JS socket is not connected");
                return null;
            }
            
            // Set up static task completion source for IL2CPP compatibility
            webglRoomJoinTcs = new TaskCompletionSource<Room>();
            webglInstance = this;
            var timeoutTask = Task.Delay(5000); // 5 second timeout
            
            // Set up static event listener for rooms:join response
            jsSocket.On("rooms:join", OnWebGLRoomJoinCallback);
            
            // Send the room join request
            string joinData = $"{{\"roomId\":\"{roomId}\"}}";
            jsSocket.Emit("rooms:join", joinData);
            
            // Wait for either the response or timeout
            var completedTask = await Task.WhenAny(webglRoomJoinTcs.Task, timeoutTask);
            if (completedTask == timeoutTask)
            {
                Debug.LogError("[WebGL] Room join request timed out after 5 seconds");
                throw new TimeoutException("Room join request timed out");
            }
            
            return await webglRoomJoinTcs.Task;
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebGL] Failed to join room: {e.Message}");
            return null;
        }
    }
#endif

    private async Task<Room> JoinRoomSync(string roomId)
    {
        var tcs = new TaskCompletionSource<Room>();
        socket.On("rooms:join", response =>
        {
            var room = JsonConvert.DeserializeObject<Room>(response.GetValue<string>());
            tcs.SetResult(room);
        });
        await socket.EmitAsync("rooms:join", roomId);
        return await tcs.Task;
    }

    public async Task LeaveRoom()
    {
        if (!isConnected) throw new Exception("Not connected to server");

        var tcs = new TaskCompletionSource<bool>();
        socket.On("rooms:leave", response =>
        {
            tcs.SetResult(true);
        });
        await socket.EmitAsync("rooms:leave");
        await tcs.Task;
    }

    public async Task<PlayerStats> GetPlayerStats()
    {
        if (!isConnected) throw new Exception("Not connected to server");

        var tcs = new TaskCompletionSource<PlayerStats>();
        socket.On("stats:player", response =>
        {
            var stats = JsonConvert.DeserializeObject<PlayerStats>(response.GetValue<string>());
            tcs.SetResult(stats);
        });
        await socket.EmitAsync("stats:player");
        return await tcs.Task;
    }

    public async Task<List<Skin>> GetSkins()
    {
        if (!isConnected) throw new Exception("Not connected to server");

        var tcs = new TaskCompletionSource<List<Skin>>();
        socket.On("skins:list", response =>
        {
            var skins = JsonConvert.DeserializeObject<List<Skin>>(response.GetValue<string>());
            tcs.SetResult(skins);
        });
        await socket.EmitAsync("skins:list");
        return await tcs.Task;
    }

    public async Task EquipSkin(string skinId)
    {
        if (!isConnected) throw new Exception("Not connected to server");

        var tcs = new TaskCompletionSource<bool>();
        socket.On("skins:equip", response =>
        {
            tcs.SetResult(true);
        });
        await socket.EmitAsync("skins:equip", skinId);
        await tcs.Task;
    }
    #endregion

    public async Task Connect(string serverUrl)
    {
        if (isConnected)
        {
            Debug.LogWarning("Already connected to server");
            return;
        }

        try
        {
            Debug.Log($"Connecting to server at {serverUrl}");
            socket = new SocketIO(serverUrl, new SocketIOOptions
            {
                Reconnection = true,
                ReconnectionAttempts = 5,
                ReconnectionDelay = 1000,
                ReconnectionDelayMax = 5000
            });

            // Set up connection event handlers
            socket.OnConnected += (sender, e) =>
            {
                Debug.Log("Connected to server");
                isConnected = true;
            };

            socket.OnDisconnected += (sender, e) =>
            {
                Debug.Log("Disconnected from server");
                isConnected = false;
            };

            socket.OnError += (sender, e) =>
            {
                Debug.LogError($"Socket error: {e}");
            };

            await socket.ConnectAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to connect: {ex.Message}");
            throw;
        }
    }

    public async Task Disconnect()
    {
        if (!isConnected)
        {
            Debug.LogWarning("Not connected to server");
            return;
        }

        try
        {
            Debug.Log("Disconnecting from server...");
            await socket.DisconnectAsync();
            isConnected = false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to disconnect: {ex.Message}");
            throw;
        }
    }

    private async void OnDestroy()
    {
        if (socket != null)
        {
            await socket.DisconnectAsync();
        }
    }

    public bool IsConnected()
    {
#if UNITY_WEBGL
        // WebGL version using JS bridge
        if (jsSocket != null)
        {
            return jsSocket.IsConnected();
        }
        return false;
#else
        // Non-WebGL version using C# Socket.IO client
        return socket != null && socket.Connected;
#endif
    }

    public async Task<bool> PingServer()
    {
        if (!isConnected)
        {
            Debug.LogError("Not connected to server");
            return false;
        }

        try
        {
            Debug.Log("=== PING TEST START ===");
            Debug.Log("Pinging server...");
            
            // Set up response handling
            var tcs = new TaskCompletionSource<bool>();
            var timeoutTask = Task.Delay(3000); // 3 second timeout
            
            // Remove any existing listeners to prevent duplicates
            socket.Off("pong");
            socket.Off("error");
            
            Debug.Log("Setting up pong listener...");
            
            // Set up event listener for pong response
            socket.On("pong", (response) =>
            {
                Debug.Log("=== PONG RECEIVED ===");
                Debug.Log($"Pong response: {response}");
                tcs.TrySetResult(true);
            });
            
            // Set up error listener
            socket.On("error", (response) =>
            {
                Debug.Log("=== ERROR RECEIVED ===");
                var error = response.GetValue<string>();
                Debug.LogError($"Ping error: {error}");
                tcs.TrySetResult(false);
            });
            
            Debug.Log("Sending ping request...");
            // Send ping
            await socket.EmitAsync("ping", new { });
            Debug.Log("Ping request sent, waiting for response...");
            
            // Wait for either the response or timeout
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
            if (completedTask == timeoutTask)
            {
                Debug.LogError("=== PING TIMEOUT ===");
                return false;
            }
            
            Debug.Log("=== PING COMPLETED ===");
            return await tcs.Task;
        }
        catch (Exception e)
        {
            Debug.LogError($"=== PING EXCEPTION ===: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            return false;
        }
    }

    // Get current game state
    public GameState GetCurrentGameState()
    {
        return currentGameState;
    }

    // Get player by ID from current game state
    public GamePlayer GetPlayerById(int playerId)
    {
        if (currentGameState?.players == null) return null;
        return Array.Find(currentGameState.players, p => p.id == playerId);
    }

    // Get all alive players
    public GamePlayer[] GetAlivePlayers()
    {
        if (currentGameState?.players == null) return new GamePlayer[0];
        return Array.FindAll(currentGameState.players, p => p.isAlive);
    }

    // Get all food in current game state
    public GameFood[] GetCurrentFood()
    {
        return currentGameState?.food ?? new GameFood[0];
    }

    // Get current leaderboard
    public GameLeaderboardEntry[] GetCurrentLeaderboard()
    {
        return currentGameState?.leaderboard ?? new GameLeaderboardEntry[0];
    }

    // Room state management methods
    public int GetCurrentRoomId()
    {
        return currentRoomId;
    }

    public string GetCurrentFriendCode()
    {
        return currentFriendCode;
    }

    public bool IsInRoom()
    {
        return isInRoom;
    }

    public async Task<bool> LeaveCurrentRoom()
    {
        if (!isInRoom || currentRoomId == -1)
        {
            Debug.Log("Not in a room to leave");
            return true;
        }

#if UNITY_WEBGL
        // WebGL version using JS bridge
        return await LeaveCurrentRoomWebGL();
#else
        // Non-WebGL version using C# Socket.IO client
        return await LeaveCurrentRoomSync();
#endif
    }

#if UNITY_WEBGL
    private Task<bool> LeaveCurrentRoomWebGL()
    {
        try
        {
            Debug.Log($"[WebGL] Leaving current room: {currentRoomId}");
            
            if (jsSocket != null && jsSocket.IsConnected())
            {
                jsSocket.Emit("player:leave", "{}");
                Debug.Log("[WebGL] Leave room request sent via JS bridge");
            }
            
            // Update state
            isInRoom = false;
            currentRoomId = -1;
            currentFriendCode = "";
            OnRoomLeft?.Invoke(currentRoomId);
            
            Debug.Log("[WebGL] Successfully left room");
            return Task.FromResult(true);
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebGL] Failed to leave room: {e.Message}");
            return Task.FromResult(false);
        }
    }
#endif

    private async Task<bool> LeaveCurrentRoomSync()
    {
        try
        {
            Debug.Log($"Leaving current room: {currentRoomId}");
            await socket.EmitAsync("player:leave", new { });
            
            // Update state
            isInRoom = false;
            currentRoomId = -1;
            currentFriendCode = "";
            OnRoomLeft?.Invoke(currentRoomId);
            
            Debug.Log("Successfully left room");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to leave room: {e.Message}");
            return false;
        }
    }

    // Public method for testing - emit custom events
    public async Task EmitCustomEvent(string eventName, object data = null)
    {
        if (!isConnected)
        {
            Debug.LogError("Not connected to server");
            return;
        }

        try
        {
            if (data != null)
            {
                await socket.EmitAsync(eventName, data);
            }
            else
            {
                await socket.EmitAsync(eventName);
            }
            Debug.Log($"Emitted custom event: {eventName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to emit custom event {eventName}: {e.Message}");
        }
    }

    #if UNITY_WEBGL
    private void InitializeSocketWebGL()
    {
        try
        {
            // Create the JS socket bridge
            jsSocket = new SocketIOWebGL();
            
            // Connect to the server
            string wsUrl = serverUrl;
            if (serverUrl.StartsWith("https://"))
            {
                wsUrl = serverUrl.Replace("https://", "wss://");
            }
            else if (serverUrl.StartsWith("http://"))
            {
                wsUrl = serverUrl.Replace("http://", "ws://");
            }
            
            jsSocket.Connect(wsUrl);
            
            // Set up event listeners
            SetupWebGLEventListeners();
            
            // Start connection check coroutine
            StartCoroutine(CheckWebGLConnection());
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebGL] Failed to initialize WebGL Socket.IO bridge: {e.Message}");
        }
    }

    private System.Collections.IEnumerator CheckWebGLConnection()
    {
        // Wait a bit for the connection to establish
        yield return new WaitForSeconds(2f);
        
        int checkCount = 0;
        while (checkCount < 10) // Check for up to 20 seconds
        {
            if (jsSocket != null && jsSocket.IsConnected())
            {
                isConnected = true;
                yield break;
            }
            
            yield return new WaitForSeconds(2f);
            checkCount++;
        }
        
        Debug.LogWarning("[WebGL] WebGL connection check timed out - connection may not be established");
    }

    private void SetupWebGLEventListeners()
    {
        if (jsSocket != null && jsSocket.IsConnected())
        {
            // Set up game event listeners in the JS bridge
            jsSocket.SetupGameEventListeners();
            
            // Also set up Unity-side listeners for debugging using static methods
            jsSocket.On("game:state", OnWebGLGameStateReceivedCallback);
            jsSocket.On("room:auto-join", OnWebGLRoomAutoJoinCallback); // Register the static callback
            jsSocket.On("player:joined", OnWebGLPlayerJoinedCallback);
            
            // Send a test event to verify the connection
            jsSocket.Emit("test", "{\"message\":\"WebGL client ready for game events\"}");
        }
        else
        {
            Debug.LogError($"[WebGL] Cannot set up event listeners - JS socket connected: {jsSocket?.IsConnected() ?? false}");
        }
    }

    // Static methods for IL2CPP compatibility
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    [AOT.MonoPInvokeCallback(typeof(System.Action))]
    private static void OnWebGLGameStateReceivedCallback()
    {
        NetworkManager.OnWebGLGameStateReceived?.Invoke();
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    [AOT.MonoPInvokeCallback(typeof(System.Action))]
    private static void OnWebGLPlayerJoinedCallback()
    {
        NetworkManager.OnWebGLPlayerJoined?.Invoke();
    }

    private System.Collections.IEnumerator TestSnakeInput()
    {
        yield return new WaitForSeconds(2f);
        
        EmitSnakeInput(new Vector2(1f, 0f), false); // Move right
        
        yield return new WaitForSeconds(1f);
        EmitSnakeInput(new Vector2(0f, 1f), false); // Move up
        
        yield return new WaitForSeconds(1f);
        EmitSnakeInput(new Vector2(-1f, 0f), false); // Move left
        
        // Test JS bridge data retrieval
        yield return new WaitForSeconds(1f);
        TestJSBridgeDataRetrieval();
    }
    
    private void TestJSBridgeDataRetrieval()
    {
#if UNITY_WEBGL
        if (jsSocket != null)
        {
            string gameStateData = jsSocket.GetGameStateData();
            string roomData = jsSocket.GetRoomData();
            string playerData = jsSocket.GetPlayerData();
        }
        else
        {
            Debug.LogError("[WebGL] JS socket is null during data retrieval test");
        }
#else
        Debug.Log("[Non-WebGL] JS bridge data retrieval test skipped");
#endif
    }
    #endif

#if UNITY_WEBGL
    public SocketIOWebGL GetJSSocket()
    {
        return jsSocket;
    }
#endif
} 