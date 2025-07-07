using UnityEngine;
using QFSW.QC;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Cinemachine;

[AddComponentMenu("Testing/Server Test Commands")]
public class ServerTestCommands : MonoBehaviour
{
    private NetworkManager networkManager;
    private bool isInitialized = false;

    private void Awake()
    {
        // Ensure this GameObject persists between scenes
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        networkManager = NetworkManager.Instance;
        if (networkManager == null)
        {
            Debug.LogError("NetworkManager not found! Make sure it's in the scene.");
        }
        else
        {
            isInitialized = true;
        }
    }

    [Command("server-list")]
    [CommandDescription("Lists all available game servers")]
    public async Task ListServers()
    {
        if (!isInitialized || networkManager == null)
        {
            Debug.LogError("NetworkManager not initialized! Make sure it's in the scene.");
            return;
        }

        Debug.Log("Fetching server list...");
        var servers = await networkManager.GetServers();
        
        if (servers == null || servers.Count == 0)
        {
            Debug.Log("No servers found or connection error occurred.");
            return;
        }

        Debug.Log($"Available servers: {servers.Count}");
        foreach (var server in servers)
        {
            Debug.Log($"Server: {server.name} - Status: {server.status}");
        }
    }

    [Command("room-list")]
    [CommandDescription("Lists all rooms in the current server")]
    public async Task ListRooms()
    {
        if (!isInitialized || networkManager == null)
        {
            Debug.LogError("NetworkManager not initialized! Make sure it's in the scene.");
            return;
        }

        Debug.Log("Fetching room list...");
        var rooms = await networkManager.GetRooms();
        
        if (rooms == null || rooms.Count == 0)
        {
            Debug.Log("No rooms found or connection error occurred.");
            return;
        }

        Debug.Log($"Available rooms: {rooms.Count}");
        foreach (var room in rooms)
        {
            Debug.Log($"Room: {room.name} - Players: {room.players}/{room.maxPlayers}");
        }
    }

    [Command("room-create")]
    [CommandDescription("Creates a new room")]
    public async Task CreateRoom(string roomName)
    {
        if (!isInitialized || networkManager == null)
        {
            Debug.LogError("NetworkManager not initialized! Make sure it's in the scene.");
            return;
        }

        Debug.Log($"Creating room: {roomName}...");
        var result = await networkManager.CreateRoom(roomName);
        if (result != null)
        {
            Debug.Log($"Room created: {result.name} (ID: {result.id})");
        }
        else
        {
            Debug.LogError("Failed to create room.");
        }
    }

    [Command("room-join")]
    [CommandDescription("Joins a room by ID")]
    public async Task JoinRoom(string roomId)
    {
        if (!isInitialized || networkManager == null)
        {
            Debug.LogError("NetworkManager not initialized! Make sure it's in the scene.");
            return;
        }

        Debug.Log($"Joining room: {roomId}...");
        var result = await networkManager.JoinRoom(roomId);
        if (result != null)
        {
            Debug.Log($"Joined room: {result.name}");
        }
        else
        {
            Debug.LogError("Failed to join room.");
        }
    }

    [Command("room-leave")]
    [CommandDescription("Leaves the current room")]
    public async Task LeaveRoom()
    {
        if (!isInitialized || networkManager == null)
        {
            Debug.LogError("NetworkManager not initialized! Make sure it's in the scene.");
            return;
        }

        Debug.Log("Leaving current room...");
        await networkManager.LeaveRoom();
        Debug.Log("Left current room");
    }

    [Command("player-stats")]
    [CommandDescription("Gets current player stats")]
    public async Task GetPlayerStats()
    {
        if (!isInitialized || networkManager == null)
        {
            Debug.LogError("NetworkManager not initialized! Make sure it's in the scene.");
            return;
        }

        Debug.Log("Fetching player stats...");
        var stats = await networkManager.GetPlayerStats();
        if (stats != null)
        {
            Debug.Log($"Player Stats:\n" +
                     $"Score: {stats.score}\n" +
                     $"Kills: {stats.kills}\n" +
                     $"Best Score: {stats.bestScore}\n" +
                     $"Total Kills: {stats.totalKills}");
        }
        else
        {
            Debug.LogError("Failed to fetch player stats.");
        }
    }

    [Command("skin-list")]
    [CommandDescription("Lists all available skins")]
    public async Task ListSkins()
    {
        if (!isInitialized || networkManager == null)
        {
            Debug.LogError("NetworkManager not initialized! Make sure it's in the scene.");
            return;
        }

        Debug.Log("Fetching skin list...");
        var skins = await networkManager.GetSkins();
        if (skins != null && skins.Count > 0)
        {
            Debug.Log($"Available skins: {skins.Count}");
            foreach (var skin in skins)
            {
                Debug.Log($"Skin: {skin.name} - Unlocked: {skin.isUnlocked}");
            }
        }
        else
        {
            Debug.Log("No skins found or connection error occurred.");
        }
    }

    [Command("skin-equip")]
    [CommandDescription("Equips a skin by ID")]
    public async Task EquipSkin(string skinId)
    {
        if (!isInitialized || networkManager == null)
        {
            Debug.LogError("NetworkManager not initialized! Make sure it's in the scene.");
            return;
        }

        Debug.Log($"Equipping skin: {skinId}...");
        await networkManager.EquipSkin(skinId);
        Debug.Log($"Equipped skin: {skinId}");
    }

    [Command("test-connection", "Test server connection status")]
    public void TestConnection()
    {
        try
        {
            if (NetworkManager.Instance == null)
            {
                Debug.LogError("NetworkManager.Instance is null!");
                return;
            }
            
            Debug.Log($"NetworkManager found: {NetworkManager.Instance.name}");
            Debug.Log($"Connection status: {NetworkManager.Instance.IsConnected()}");
            
            // Test ping to server
            Debug.Log("Testing ping to server...");
            // Note: We could add a ping method to NetworkManager if needed
        }
        catch (Exception e)
        {
            Debug.LogError($"Connection test failed: {e.Message}");
        }
    }

    [Command("test-room-join", "Test room joining functionality")]
    public async Task TestRoomJoin()
    {
        try
        {
            Debug.Log("=== ROOM JOIN TEST START ===");
            
            // Check current room state
            if (NetworkManager.Instance.IsInRoom())
            {
                Debug.Log($"Currently in room: {NetworkManager.Instance.GetCurrentRoomId()}");
                Debug.Log("Leaving current room first...");
                await NetworkManager.Instance.LeaveCurrentRoom();
            }
            else
            {
                Debug.Log("Not currently in any room");
            }
            
            Debug.Log("Testing room joining...");
            
            // Auto-join will put the player directly in the game room
            var room = await NetworkManager.Instance.AutoJoinRoom();
            if (room != null)
            {
                Debug.Log($"=== ROOM JOIN SUCCESS ===");
                Debug.Log($"Auto-joined room: {room.name} (ID: {room.id}, Code: {room.friendCode ?? "N/A"})");
                Debug.Log($"Current room state: {NetworkManager.Instance.GetCurrentRoomId()}");
                Debug.Log("Player is now in the game room and ready to play!");
            }
            else
            {
                Debug.LogError("=== ROOM JOIN FAILED ===");
                Debug.LogError("Failed to auto-join room");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"=== ROOM JOIN EXCEPTION ===: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
        }
    }

    [Command("test-movement", "Test snake movement with optimized data")]
    public void TestMovement()
    {
        try
        {
            Debug.Log("Testing optimized movement...");
            
            // Test optimized movement data
            Vector2 position = new Vector2(100f, 200f);
            float rotation = 45f;
            bool isBoosting = true;
            
            NetworkManager.Instance.EmitSnakePositionOptimized(position, rotation, isBoosting);
            Debug.Log($"Sent optimized movement: pos=({position.x}, {position.y}), rot={rotation}, boost={isBoosting}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Movement test failed: {e.Message}");
        }
    }

    [Command("test-friend-join", "Test joining room by friend code")]
    public async void TestFriendJoin(string friendCode)
    {
        try
        {
            Debug.Log($"Testing friend code join: {friendCode}");
            
            var room = await NetworkManager.Instance.JoinRoomByFriendCode(friendCode);
            if (room != null)
            {
                Debug.Log($"Joined room by friend code: {room.name} (ID: {room.id})");
                
                // Join the game room
                bool joined = await NetworkManager.Instance.JoinGameRoom(room.id.ToString());
                if (joined)
                {
                    Debug.Log("Successfully joined game room via friend code!");
                }
                else
                {
                    Debug.LogError("Failed to join game room via friend code");
                }
            }
            else
            {
                Debug.LogError("Failed to join room by friend code");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Friend join test failed: {e.Message}");
        }
    }

    [Command("test-ping", "Test basic server connectivity")]
    public async Task TestPing()
    {
        try
        {
            Debug.Log("=== PING TEST ===");
            
            if (NetworkManager.Instance == null)
        {
                Debug.LogError("NetworkManager not found!");
            return;
        }

            if (!NetworkManager.Instance.IsConnected())
            {
                Debug.LogError("Not connected to server!");
                return;
            }
            
            Debug.Log("Sending ping to server...");
            
            // Send a ping event
            await NetworkManager.Instance.EmitCustomEvent("ping", new { });
            
            Debug.Log("Ping sent. Check server logs for response.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Ping test failed: {e.Message}");
        }
    }

    [Command("test-server-status", "Check if server is running and responsive")]
    public async Task TestServerStatus()
    {
        try
        {
            Debug.Log("=== SERVER STATUS TEST ===");
            
            if (NetworkManager.Instance == null)
            {
                Debug.LogError("NetworkManager not found!");
                return;
            }
            
            Debug.Log($"Connection status: {NetworkManager.Instance.IsConnected()}");
            Debug.Log($"In room: {NetworkManager.Instance.IsInRoom()}");
            if (NetworkManager.Instance.IsInRoom())
            {
                Debug.Log($"Current room ID: {NetworkManager.Instance.GetCurrentRoomId()}");
            }
            
            // Test ping
            await TestPing();
            
            // Test manual broadcast if in room
            if (NetworkManager.Instance.IsInRoom())
            {
                Debug.Log("Testing manual broadcast...");
                await TestManualBroadcast();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Server status test failed: {e.Message}");
        }
    }

    [Command("test-game-state", "Test server-authoritative game state")]
    public void TestGameState()
    {
        if (!isInitialized || networkManager == null)
        {
            Debug.LogError("NetworkManager not initialized! Make sure it's in the scene.");
            return;
        }

        Debug.Log("Testing server-authoritative game state...");
        
        // Subscribe to game state updates
        networkManager.OnGameStateUpdated += (gameState) =>
        {
            Debug.Log($"Game State Update Received:");
            Debug.Log($"  Room ID: {gameState.roomId}");
            Debug.Log($"  Timestamp: {gameState.timestamp}");
            Debug.Log($"  Players: {gameState.players?.Length ?? 0}");
            Debug.Log($"  Food: {gameState.food?.Length ?? 0}");
            Debug.Log($"  Leaderboard: {gameState.leaderboard?.Length ?? 0}");
            
            if (gameState.players != null)
            {
                foreach (var player in gameState.players)
                {
                    Debug.Log($"    Player {player.id}: {player.name} at ({player.x}, {player.y}) - Score: {player.score} - Alive: {player.isAlive}");
                }
            }
        };

        Debug.Log("Game state listener added. Join a room to see updates.");
    }

    [Command("test-room-state", "Check current room state")]
    public void TestRoomState()
    {
        try
        {
            Debug.Log("=== ROOM STATE CHECK ===");
            Debug.Log($"Is in room: {NetworkManager.Instance.IsInRoom()}");
            Debug.Log($"Current room ID: {NetworkManager.Instance.GetCurrentRoomId()}");
            Debug.Log($"Is connected: {NetworkManager.Instance.IsConnected()}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Room state check failed: {e.Message}");
        }
    }

    [Command("test-room-leave", "Leave current room")]
    public async void TestRoomLeave()
    {
        try
        {
            Debug.Log("=== ROOM LEAVE TEST ===");
            
            if (NetworkManager.Instance.IsInRoom())
            {
                Debug.Log($"Leaving room: {NetworkManager.Instance.GetCurrentRoomId()}");
                bool success = await NetworkManager.Instance.LeaveCurrentRoom();
                if (success)
                {
                    Debug.Log("Successfully left room");
                }
                else
                {
                    Debug.LogError("Failed to leave room");
                }
            }
            else
            {
                Debug.Log("Not in any room to leave");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Room leave test failed: {e.Message}");
        }
    }

    [Command("test-snake-input", "Test snake input and game state updates")]
    public void TestSnakeInput()
    {
        try
        {
            Debug.Log("=== SNAKE INPUT TEST ===");
            
            if (!NetworkManager.Instance.IsInRoom())
            {
                return;
            }
            
            Debug.Log($"Testing snake input in room: {NetworkManager.Instance.GetCurrentRoomId()}");
            
            // Test different input directions
            Vector2[] testDirections = {
                new Vector2(1f, 0f),   // Right
                new Vector2(-1f, 0f),  // Left
                new Vector2(0f, 1f),   // Up
                new Vector2(0f, -1f),  // Down
                new Vector2(0.7f, 0.7f), // Diagonal
            };
            
            foreach (var direction in testDirections)
            {
                NetworkManager.Instance.EmitSnakeInput(direction, false);
                Debug.Log($"Sent input: direction=({direction.x}, {direction.y}), boost=false");
            }
            
            // Test with boost
            NetworkManager.Instance.EmitSnakeInput(new Vector2(1f, 0f), true);
            Debug.Log("Sent input: direction=(1, 0), boost=true");
            
            Debug.Log("Snake input test completed. Check server logs for game state updates.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Snake input test failed: {e.Message}");
        }
    }

    [Command("test-game-state-debug", "Debug game state with detailed logging")]
    public void TestGameStateDebug()
    {
        if (!isInitialized || networkManager == null)
        {
            Debug.LogError("NetworkManager not initialized! Make sure it's in the scene.");
            return;
        }

        Debug.Log("=== GAME STATE DEBUG ===");
        
        // Subscribe to game state updates with detailed logging
        networkManager.OnGameStateUpdated += (gameState) =>
        {
            Debug.Log($"=== GAME STATE UPDATE RECEIVED ===");
            Debug.Log($"Room ID: {gameState.roomId}");
            Debug.Log($"Timestamp: {gameState.timestamp}");
            Debug.Log($"Players: {gameState.players?.Length ?? 0}");
            Debug.Log($"Food: {gameState.food?.Length ?? 0}");
            Debug.Log($"Leaderboard: {gameState.leaderboard?.Length ?? 0}");
            
            if (gameState.players != null && gameState.players.Length > 0)
            {
                Debug.Log("=== PLAYERS ===");
                foreach (var player in gameState.players)
                {
                    Debug.Log($"  Player {player.id}: {player.name}");
                    Debug.Log($"    Position: ({player.x}, {player.y})");
                    Debug.Log($"    Score: {player.score}, Kills: {player.kills}");
                    Debug.Log($"    Length: {player.length}, Alive: {player.isAlive}");
                    Debug.Log($"    Is Bot: {player.isBot}");
                    Debug.Log($"    Segments: {player.segments?.Length ?? 0}");
                }
            }
            else
            {
                Debug.LogWarning("No players in game state!");
            }
            
            if (gameState.food != null && gameState.food.Length > 0)
            {
                Debug.Log("=== FOOD ===");
                foreach (var food in gameState.food)
                {
                    Debug.Log($"  Food {food.id}: ({food.x}, {food.y}) - Type: {food.type}, Value: {food.value}");
                }
            }
            else
            {
                Debug.LogWarning("No food in game state!");
            }
            
            if (gameState.leaderboard != null && gameState.leaderboard.Length > 0)
            {
                Debug.Log("=== LEADERBOARD ===");
                foreach (var entry in gameState.leaderboard)
                {
                    Debug.Log($"  #{entry.rank}: {entry.name} - Score: {entry.score}, Kills: {entry.kills}");
                }
            }
            else
            {
                Debug.LogWarning("No leaderboard in game state!");
            }
        };

        Debug.Log("Game state debug listener added. Join a room and send snake input to see updates.");
    }

    [Command("test-snake-status", "Check if snake was spawned and get current game state")]
    public void TestSnakeStatus()
    {
        try
        {
            Debug.Log("=== SNAKE STATUS CHECK ===");
            
            if (!NetworkManager.Instance.IsInRoom())
            {
                Debug.LogError("Not in a room! Join a room first with test-room-join");
                return;
            }
            
            Debug.Log($"Current room ID: {NetworkManager.Instance.GetCurrentRoomId()}");
            
            // Get current game state
            var gameState = NetworkManager.Instance.GetCurrentGameState();
            if (gameState != null)
            {
                Debug.Log($"Current game state:");
                Debug.Log($"  Room ID: {gameState.roomId}");
                Debug.Log($"  Timestamp: {gameState.timestamp}");
                Debug.Log($"  Players: {gameState.players?.Length ?? 0}");
                Debug.Log($"  Food: {gameState.food?.Length ?? 0}");
                Debug.Log($"  Leaderboard: {gameState.leaderboard?.Length ?? 0}");
                
                if (gameState.players != null && gameState.players.Length > 0)
                {
                    Debug.Log("Players in game:");
                    foreach (var player in gameState.players)
                    {
                        Debug.Log($"  Player {player.id}: {player.name} at ({player.x}, {player.y}) - Alive: {player.isAlive}");
                    }
                }
                else
                {
                    Debug.LogWarning("No players found in game state!");
                }
            }
            else
            {
                Debug.LogWarning("No game state available yet. Make sure you're in a room and game state updates are being received.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Snake status check failed: {e.Message}");
        }
    }

    [Command("test-request-game-state", "Manually request game state from server")]
    public async void TestRequestGameState()
    {
        try
        {
            Debug.Log("=== REQUEST GAME STATE TEST ===");
            
            if (!NetworkManager.Instance.IsInRoom())
            {
                Debug.LogError("Not in a room! Join a room first with test-room-join");
                return;
            }
            
            int roomId = NetworkManager.Instance.GetCurrentRoomId();
            Debug.Log($"Requesting game state for room: {roomId}");
            
            // Send a request to get room state
            await NetworkManager.Instance.EmitCustomEvent("room:get-state", new { roomId = roomId });
            
            Debug.Log("Game state request sent. Check for room:state response.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Game state request failed: {e.Message}");
        }
    }

    [Command("test-manual-broadcast", "Manually trigger game state broadcast from server")]
    public async Task TestManualBroadcast()
    {
        try
        {
            Debug.Log("=== MANUAL BROADCAST TEST ===");
            
            if (!NetworkManager.Instance.IsInRoom())
            {
                Debug.LogError("Not in a room! Join a room first with test-room-join");
                return;
            }
            
            Debug.Log($"Triggering manual broadcast for room: {NetworkManager.Instance.GetCurrentRoomId()}");
            
            // Send test broadcast request
            await NetworkManager.Instance.EmitCustomEvent("test:broadcast", new { });
            
            Debug.Log("Manual broadcast request sent. Check for game:state response.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Manual broadcast test failed: {e.Message}");
        }
    }

    [Command("test-game-state-polling", "Test polling for game state data from JS bridge")]
    public void TestGameStatePolling()
    {
        try
        {
            Debug.Log("=== GAME STATE POLLING TEST ===");
            
            if (!NetworkManager.Instance.IsInRoom())
            {
                Debug.LogError("Not in a room! Join a room first with test-room-join");
                return;
            }
            
            Debug.Log("Starting game state polling test...");
            
            // Start a coroutine to poll for game state data
            MonoBehaviour.FindObjectOfType<MonoBehaviour>().StartCoroutine(PollGameStateData());
        }
        catch (Exception e)
        {
            Debug.LogError($"Game state polling test failed: {e.Message}");
        }
    }

    private System.Collections.IEnumerator PollGameStateData()
    {
        for (int i = 0; i < 10; i++) // Poll for 20 seconds
        {
            yield return new WaitForSeconds(2f);
            
            // Check if we're in WebGL mode
#if UNITY_WEBGL
            if (NetworkManager.Instance != null)
            {
                var jsSocket = NetworkManager.Instance.GetJSSocket();
                if (jsSocket != null)
                {
                    string gameStateJson = jsSocket.GetGameStateData();
                    string roomDataJson = jsSocket.GetRoomData();
                    string playerDataJson = jsSocket.GetPlayerData();
                    
                    Debug.Log($"Game state data: {(string.IsNullOrEmpty(gameStateJson) ? "null" : "available")}");
                    Debug.Log($"Room data: {(string.IsNullOrEmpty(roomDataJson) ? "null" : "available")}");
                    Debug.Log($"Player data: {(string.IsNullOrEmpty(playerDataJson) ? "null" : "available")}");
                }
                else
                {
                    Debug.LogError("JS socket is null!");
                }
            }
            else
            {
                Debug.LogError("NetworkManager is null!");
            }
#else
            Debug.Log("Not in WebGL mode - skipping JS bridge test");
#endif
        }
        
        Debug.Log("Game state polling test completed.");
    }

    [Command("test-player-manager", "Check PlayerManager status and local player")]
    public void TestPlayerManager()
    {
        if (PlayerManager.Instance == null)
        {
            Debug.LogError("PlayerManager not found in scene!");
            return;
        }

        Debug.Log("=== PLAYER MANAGER STATUS ===");
        Debug.Log($"Local Player ID: {PlayerManager.Instance.GetLocalPlayerId()}");
        
        GameObject localSnake = PlayerManager.Instance.GetLocalPlayerSnake();
        if (localSnake != null)
        {
            Debug.Log($"Local Snake: {localSnake.name} at {localSnake.transform.position}");
            SnakeController controller = localSnake.GetComponent<SnakeController>();
            if (controller != null)
            {
                Debug.Log($"  Player ID: {controller.playerId}");
                Debug.Log($"  Is Local: {controller.isLocalPlayer}");
                Debug.Log($"  Is Alive: {controller.isAlive}");
            }
        }
        else
        {
            Debug.LogWarning("No local snake found!");
        }

        Debug.Log("=== CURRENT GAME STATE ===");
        var gameState = NetworkManager.Instance.GetCurrentGameState();
        if (gameState != null)
        {
            Debug.Log($"Room ID: {gameState.roomId}");
            Debug.Log($"Players: {gameState.players?.Length ?? 0}");
            Debug.Log($"Food: {gameState.food?.Length ?? 0}");
            
            if (gameState.players != null)
            {
                foreach (var player in gameState.players)
                {
                    bool isLocal = PlayerManager.Instance.IsLocalPlayer(player.id);
                    Debug.Log($"  Player {player.id}: {player.name} at ({player.x}, {player.y}) - Local: {isLocal}");
                }
            }
        }
        else
        {
            Debug.LogWarning("No game state available!");
        }
    }

    [Command("test-performance", "Test performance and memory usage")]
    public void TestPerformance()
    {
        Debug.Log("=== PERFORMANCE TEST ===");
        
        // Check memory usage
        long totalMemory = System.GC.GetTotalMemory(false);
        Debug.Log($"Total memory usage: {totalMemory / 1024 / 1024} MB");
        
        // Check frame rate
        float fps = 1.0f / Time.deltaTime;
        Debug.Log($"Current FPS: {fps:F1}");
        
        // Check active objects
        int activeObjects = FindObjectsOfType<GameObject>().Length;
        Debug.Log($"Active GameObjects: {activeObjects}");
        
        // Check if we're in WebGL
        #if UNITY_WEBGL
        Debug.Log("Running in WebGL mode");
        #else
        Debug.Log("Running in non-WebGL mode");
        #endif
        
        Debug.Log("=== END PERFORMANCE TEST ===");
    }

    [Command("test-lag-reduction", "Apply lag reduction settings")]
    public void TestLagReduction()
    {
        Debug.Log("=== APPLYING LAG REDUCTION SETTINGS ===");
        
        // Find all snake controllers and reduce their update rates
        var snakeControllers = FindObjectsOfType<SnakeController>();
        foreach (var controller in snakeControllers)
        {
            controller.visualUpdateRate = 10f; // Reduce to 10 FPS for visual updates
            Debug.Log($"Reduced visual update rate for snake {controller.playerId}");
        }
        
        Debug.Log($"Updated {snakeControllers.Length} snake controllers");
        Debug.Log("Restart the game to see improvements");
    }

    [Command("disable-debug-logs", "Disable debug logging for maximum performance")]
    public void DisableDebugLogs()
    {
        Debug.Log("Disabling debug logs for performance...");
        
        // Disable Unity debug logging
        Debug.unityLogger.logEnabled = false;
        
        // Also disable our custom debug logging
        if (PlayerManager.Instance != null)
        {
            // We could add a flag to PlayerManager to disable debug logs
            Debug.Log("PlayerManager debug logging disabled");
        }
        
        Debug.Log("Debug logging disabled. Use 'enable-debug-logs' to re-enable.");
    }

    [Command("enable-debug-logs", "Re-enable debug logging")]
    public void EnableDebugLogs()
    {
        Debug.Log("Re-enabling debug logs...");
        Debug.unityLogger.logEnabled = true;
        Debug.Log("Debug logging re-enabled");
    }

    [Command("test-camera", "Test camera following and control")]
    public void TestCamera()
    {
        var cameraController = FindObjectOfType<CameraController>();
        if (cameraController == null)
        {
            Debug.LogError("CameraController not found in scene!");
            return;
        }
        
        Debug.Log("=== CAMERA TEST ===");
        Debug.Log($"Auto Follow Enabled: {cameraController.autoFollowLocalPlayer}");
        Debug.Log($"Is Following Target: {cameraController.IsFollowingTarget()}");
        
        var currentTarget = cameraController.GetCurrentTarget();
        if (currentTarget != null)
        {
            var snakeController = currentTarget.GetComponent<SnakeController>();
            Debug.Log($"Current Target: {currentTarget.name}");
            Debug.Log($"Player ID: {snakeController?.playerId}");
            Debug.Log($"Is Local Player: {snakeController?.isLocalPlayer}");
            Debug.Log($"Position: {currentTarget.transform.position}");
        }
        else
        {
            Debug.LogWarning("No camera target found!");
        }
        
        // Check for virtual camera
        var virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
        if (virtualCamera != null)
        {
            Debug.Log($"Virtual Camera Found: {virtualCamera.name}");
            Debug.Log($"Follow Target: {virtualCamera.Follow?.name ?? "None"}");
            Debug.Log($"Look At Target: {virtualCamera.LookAt?.name ?? "None"}");
        }
        else
        {
            Debug.LogError("No CinemachineVirtualCamera found!");
        }
    }

    [Command("camera-follow-local", "Enable camera to follow local player")]
    public void CameraFollowLocal()
    {
        var cameraController = FindObjectOfType<CameraController>();
        if (cameraController == null)
        {
            Debug.LogError("CameraController not found in scene!");
            return;
        }
        
        cameraController.SetAutoFollow(true);
        Debug.Log("Camera auto-follow enabled - will follow local player snake");
    }

    [Command("camera-stop-follow", "Stop camera from following any target")]
    public void CameraStopFollow()
    {
        var cameraController = FindObjectOfType<CameraController>();
        if (cameraController == null)
        {
            Debug.LogError("CameraController not found in scene!");
            return;
        }
        
        cameraController.SetAutoFollow(false);
        Debug.Log("Camera auto-follow disabled");
    }

    [Command("camera-follow-player", "Manually set camera to follow specific player by ID")]
    public void CameraFollowPlayer(int playerId)
    {
        var cameraController = FindObjectOfType<CameraController>();
        if (cameraController == null)
        {
            Debug.LogError("CameraController not found in scene!");
            return;
        }
        
        // Find player by ID
        var snakeControllers = FindObjectsOfType<SnakeController>();
        foreach (var controller in snakeControllers)
        {
            if (controller.playerId == playerId)
            {
                cameraController.SetTarget(controller.gameObject);
                Debug.Log($"Camera now following player {playerId}: {controller.gameObject.name}");
                return;
            }
        }
        
        Debug.LogError($"Player with ID {playerId} not found!");
    }

    [Command("test-force-broadcast", "Force server to broadcast current game state")]
    public async Task TestForceBroadcast()
    {
        try
        {
            Debug.Log("=== FORCE BROADCAST TEST ===");
            
            if (!NetworkManager.Instance.IsInRoom())
            {
                Debug.LogError("Not in a room! Join a room first with test-room-join");
                return;
            }
            
            Debug.Log("Forcing server to broadcast current game state...");
            
            // Send multiple requests to trigger broadcasts
            await NetworkManager.Instance.EmitCustomEvent("test:broadcast", new { });
            await Task.Delay(100);
            await NetworkManager.Instance.EmitCustomEvent("room:get-state", new { roomId = NetworkManager.Instance.GetCurrentRoomId() });
            await Task.Delay(100);
            await NetworkManager.Instance.EmitCustomEvent("ping", new { });
            
            Debug.Log("Force broadcast requests sent. Check for game:state responses.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Force broadcast test failed: {e.Message}");
        }
    }

    [Command("test-debug-full", "Comprehensive debugging test for connection, room join, and game state")]
    public async Task TestDebugFull()
    {
        try
        {
            Debug.Log("=== COMPREHENSIVE DEBUG TEST ===");
            
            // Step 1: Check NetworkManager
            if (NetworkManager.Instance == null)
            {
                Debug.LogError("[ERROR] NetworkManager not found!");
                return;
            }
            Debug.Log("[SUCCESS] NetworkManager found");
            
            // Step 2: Check connection
            bool isConnected = NetworkManager.Instance.IsConnected();
            Debug.Log($"Connection status: {(isConnected ? "[CONNECTED]" : "[DISCONNECTED]")}");
            
            if (!isConnected)
            {
                Debug.LogError("Cannot proceed - not connected to server");
                return;
            }
            
            // Step 3: Check room status
            bool isInRoom = NetworkManager.Instance.IsInRoom();
            Debug.Log($"Room status: {(isInRoom ? "[IN ROOM]" : "[NOT IN ROOM]")}");
            
            if (isInRoom)
            {
                int roomId = NetworkManager.Instance.GetCurrentRoomId();
                Debug.Log($"Current room ID: {roomId}");
            }
            
            // Step 4: Test ping
            Debug.Log("Testing ping...");
            await TestPing();
            
            // Step 5: Join room if not in one
            if (!isInRoom)
            {
                Debug.Log("Joining room...");
                await TestRoomJoin();
                
                // Check room status again
                isInRoom = NetworkManager.Instance.IsInRoom();
                Debug.Log($"After join - Room status: {(isInRoom ? "[IN ROOM]" : "[NOT IN ROOM]")}");
            }
            
            // Step 6: Test game state polling
            if (isInRoom)
            {
                Debug.Log("Testing game state polling...");
                TestGameStatePolling();
                
                // Step 7: Force broadcast
                Debug.Log("Testing force broadcast...");
                await TestForceBroadcast();
            }
            
            Debug.Log("=== DEBUG TEST COMPLETED ===");
        }
        catch (Exception e)
        {
            Debug.LogError($"Debug test failed: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
        }
    }

    [Command("test-webgl-bridge", "Test WebGL bridge connection and basic functionality")]
    public void TestWebGLBridge()
    {
        Debug.Log("=== WEBGL BRIDGE TEST ===");
        
#if UNITY_WEBGL
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("NetworkManager not found!");
            return;
        }
        
        var jsSocket = NetworkManager.Instance.GetJSSocket();
        if (jsSocket == null)
        {
            Debug.LogError("JS socket is null!");
            return;
        }
        
        Debug.Log($"JS Socket Connected: {jsSocket.IsConnected()}");
        
        // Test basic emit
        jsSocket.Emit("test", "{\"message\":\"WebGL bridge test\"}");
        
        // Test data retrieval
        string gameStateData = jsSocket.GetGameStateData();
        string roomData = jsSocket.GetRoomData();
        string playerData = jsSocket.GetPlayerData();
        
        Debug.Log($"Game State Data: {(string.IsNullOrEmpty(gameStateData) ? "null" : "available")}");
        Debug.Log($"Room Data: {(string.IsNullOrEmpty(roomData) ? "null" : "available")}");
        Debug.Log($"Player Data: {(string.IsNullOrEmpty(playerData) ? "null" : "available")}");
        
        Debug.Log("=== WEBGL BRIDGE TEST COMPLETED ===");
#else
        Debug.Log("Not in WebGL mode - skipping WebGL bridge test");
#endif
    }

    [Command("test-camera-collision", "Test camera following and collision detection")]
    public void TestCameraAndCollision()
    {
        Debug.Log("=== CAMERA AND COLLISION TEST ===");
        
        // Test camera
        var cameraController = FindObjectOfType<CameraController>();
        if (cameraController != null)
        {
            Debug.Log($"Camera Auto Follow: {cameraController.autoFollowLocalPlayer}");
            Debug.Log($"Camera Following Target: {cameraController.IsFollowingTarget()}");
            
            var currentTarget = cameraController.GetCurrentTarget();
            if (currentTarget != null)
            {
                var snakeController = currentTarget.GetComponent<SnakeController>();
                Debug.Log($"Camera Target: {currentTarget.name} (ID: {snakeController?.playerId}, Local: {snakeController?.isLocalPlayer}, Alive: {snakeController?.isAlive})");
            }
            else
            {
                Debug.LogWarning("No camera target found!");
            }
        }
        else
        {
            Debug.LogError("CameraController not found!");
        }
        
        // Test collision detection
        var snakeControllers = FindObjectsOfType<SnakeController>();
        Debug.Log($"Found {snakeControllers.Length} snake controllers:");
        
        foreach (var controller in snakeControllers)
        {
            var collider = controller.GetComponent<CircleCollider2D>();
            var rigidbody = controller.GetComponent<Rigidbody2D>();
            
            Debug.Log($"  Snake {controller.playerId}: Local={controller.isLocalPlayer}, Alive={controller.isAlive}, Collider={collider != null}, Rigidbody={rigidbody != null}");
            
            if (collider != null)
            {
                Debug.Log($"    Collider: radius={collider.radius}, isTrigger={collider.isTrigger}");
            }
        }
        
        // Test PlayerManager
        if (PlayerManager.Instance != null)
        {
            Debug.Log($"PlayerManager: Local Player ID = {PlayerManager.Instance.GetLocalPlayerId()}");
            var localSnake = PlayerManager.Instance.GetLocalPlayerSnake();
            if (localSnake != null)
            {
                Debug.Log($"Local Snake: {localSnake.name} at {localSnake.transform.position}");
            }
            else
            {
                Debug.LogWarning("No local snake found in PlayerManager!");
            }
        }
        else
        {
            Debug.LogError("PlayerManager not found!");
        }
    }

    [Command("test-smoothness", "Test movement smoothness and responsiveness")]
    public void TestSmoothness()
    {
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("NetworkManager not found!");
            return;
        }

        var localPlayer = PlayerManager.Instance?.GetLocalPlayerSnake();
        if (localPlayer == null)
        {
            Debug.LogError("Local player not found!");
            return;
        }

        var snakeController = localPlayer.GetComponent<SnakeController>();
        if (snakeController == null)
        {
            Debug.LogError("SnakeController not found on local player!");
            return;
        }

        Debug.Log($"=== SMOOTHNESS TEST ===");
        Debug.Log($"Visual Update Rate: {snakeController.visualUpdateRate} FPS");
        Debug.Log($"Segment Speed: {snakeController.segmentSpeed}");
        Debug.Log($"Segment Speed Multiplier: {snakeController.segmentSpeedMultiplier}");
        Debug.Log($"Rotation Speed: {snakeController.rotationSpeed}");
        Debug.Log($"Segment Spacing: {snakeController.segmentSpacing}");
        Debug.Log($"Current Position: {snakeController.transform.position}");
        Debug.Log($"Server Position: {snakeController.serverPosition}");
        Debug.Log($"Movement Direction: {snakeController.movementDirection}");
        Debug.Log($"Movement Speed: {snakeController.movementSpeed} (Should be 8 or 12)");
        Debug.Log($"Is Moving: {snakeController.isMoving}");
        Debug.Log($"Is Boosting: {snakeController.isBoosting}");
        Debug.Log($"Direction Magnitude: {snakeController.movementDirection.magnitude:F3}");
        Debug.Log($"Is Alive: {snakeController.isAlive}");
        Debug.Log($"Has Received First Update: {snakeController.hasReceivedFirstUpdate}");
        Debug.Log($"Body Segments Count: {snakeController.bodySegments?.Count ?? 0}");
        
        // Test camera
        var cameraController = FindObjectOfType<CameraController>();
        if (cameraController != null)
        {
            Debug.Log($"Camera Follow Speed: {cameraController.followSpeed}");
            Debug.Log($"Camera Search Interval: {cameraController.searchInterval * 1000:F1}ms");
            Debug.Log($"Camera Following Target: {cameraController.IsFollowingTarget()}");
            Debug.Log($"Camera Target: {cameraController.GetCurrentTarget()?.name ?? "None"}");
        }
        
        Debug.Log($"Network Connected: {NetworkManager.Instance.IsConnected()}");
        Debug.Log($"Current Room ID: {NetworkManager.Instance.GetCurrentRoomId()}");
        Debug.Log($"=== END SMOOTHNESS TEST ===");
    }

    [Command("test-collision", "Test collision detection and self-collision ignoring")]
    public void TestCollision()
    {
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("NetworkManager not found!");
            return;
        }

        var localPlayer = PlayerManager.Instance?.GetLocalPlayerSnake();
        if (localPlayer == null)
        {
            Debug.LogError("Local player not found!");
            return;
        }

        var snakeController = localPlayer.GetComponent<SnakeController>();
        if (snakeController == null)
        {
            Debug.LogError("SnakeController not found on local player!");
            return;
        }

        Debug.Log($"=== COLLISION TEST ===");
        Debug.Log($"Player ID: {snakeController.playerId}");
        Debug.Log($"Is Alive: {snakeController.isAlive}");
        Debug.Log($"Has Collided: {snakeController.hasCollided}");
        Debug.Log($"Collision Cooldown: {snakeController.collisionCooldown:F2}");
        Debug.Log($"Body Segments Count: {snakeController.bodySegments?.Count ?? 0}");
        
        // Test self-collision detection
        bool hasSelfCollision = false;
        foreach (var segment in snakeController.bodySegments)
        {
            if (segment != null)
            {
                var segmentController = segment.GetComponent<SnakeController>();
                var bodySegment = segment.GetComponent<BodySegment>();
                
                if (segmentController != null && segmentController.playerId == snakeController.playerId)
                {
                    hasSelfCollision = true;
                    Debug.LogWarning($"SELF-COLLISION DETECTED: Segment {segment.name} has same player ID!");
                }
                
                if (bodySegment != null)
                {
                    Debug.Log($"Body Segment {bodySegment.segmentIndex}: Owner ID = {bodySegment.ownerPlayerId}, Tag = {segment.tag}");
                    if (bodySegment.ownerPlayerId == snakeController.playerId)
                    {
                        Debug.Log($"✓ Body segment {bodySegment.segmentIndex} properly owned by player {snakeController.playerId}");
                    }
                    else
                    {
                        Debug.LogWarning($"⚠ Body segment {bodySegment.segmentIndex} has wrong owner ID: {bodySegment.ownerPlayerId}");
                    }
                }
                else
                {
                    Debug.LogWarning($"⚠ Body segment {segment.name} missing BodySegment component!");
                }
            }
        }
        
        if (!hasSelfCollision)
        {
            Debug.Log("✓ Self-collision properly ignored - no segments with same player ID");
        }
        
        // Check for other snakes in the scene
        var allSnakes = FindObjectsOfType<SnakeController>();
        int otherSnakes = 0;
        foreach (var snake in allSnakes)
        {
            if (snake.playerId != snakeController.playerId)
            {
                otherSnakes++;
            }
        }
        
        Debug.Log($"Other snakes in scene: {otherSnakes}");
        Debug.Log($"=== END COLLISION TEST ===");
    }

    [Command("test-death", "Test snake death and food spawning")]
    public void TestDeath()
    {
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("NetworkManager not found!");
            return;
        }

        var localPlayer = PlayerManager.Instance?.GetLocalPlayerSnake();
        if (localPlayer == null)
        {
            Debug.LogError("Local player not found!");
            return;
        }

        var snakeController = localPlayer.GetComponent<SnakeController>();
        if (snakeController == null)
        {
            Debug.LogError("SnakeController not found on local player!");
            return;
        }

        Debug.Log($"=== DEATH TEST ===");
        Debug.Log($"Player ID: {snakeController.playerId}");
        Debug.Log($"Is Alive: {snakeController.isAlive}");
        Debug.Log($"Server Is Alive: {snakeController.serverIsAlive}");
        Debug.Log($"Body Segments Count: {snakeController.bodySegments?.Count ?? 0}");
        
        // Check FoodManager
        if (FoodManager.Instance != null)
        {
            Debug.Log($"FoodManager found - foodPrefab: {FoodManager.Instance.foodPrefab != null}");
        }
        else
        {
            Debug.LogError("FoodManager not found!");
        }
        
        // Manually trigger death for testing
        Debug.Log("Manually triggering death...");
        snakeController.Die();
        
        Debug.Log($"=== END DEATH TEST ===");
    }

    [Command("test-death-state", "Check current death state and debug food spawning")]
    public void TestDeathState()
    {
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("NetworkManager not found!");
            return;
        }

        var localPlayer = PlayerManager.Instance?.GetLocalPlayerSnake();
        if (localPlayer == null)
        {
            Debug.LogError("Local player not found!");
            return;
        }

        var snakeController = localPlayer.GetComponent<SnakeController>();
        if (snakeController == null)
        {
            Debug.LogError("SnakeController not found on local player!");
            return;
        }

        Debug.Log($"=== DEATH STATE DEBUG ===");
        Debug.Log($"Player ID: {snakeController.playerId}");
        Debug.Log($"Is Alive: {snakeController.isAlive}");
        Debug.Log($"Server Is Alive: {snakeController.serverIsAlive}");
        Debug.Log($"Body Segments Count: {snakeController.bodySegments?.Count ?? 0}");
        Debug.Log($"Game Object Active: {snakeController.gameObject.activeInHierarchy}");
        Debug.Log($"Has Received First Update: {snakeController.hasReceivedFirstUpdate}");
        
        // Check PlayerManager
        if (PlayerManager.Instance != null)
        {
            Debug.Log($"PlayerManager found - foodPrefab: {PlayerManager.Instance.foodPrefab != null}");
        }
        else
        {
            Debug.LogError("PlayerManager not found!");
        }
        
        // Check if snake is visible
        SpriteRenderer renderer = snakeController.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            Debug.Log($"Snake renderer enabled: {renderer.enabled}");
            Debug.Log($"Snake renderer visible: {renderer.isVisible}");
        }
        
        // Check body segments
        if (snakeController.bodySegments != null)
        {
            for (int i = 0; i < snakeController.bodySegments.Count; i++)
            {
                var segment = snakeController.bodySegments[i];
                if (segment != null)
                {
                    Debug.Log($"Body segment {i}: {segment.name}, Active: {segment.gameObject.activeInHierarchy}");
                }
                else
                {
                    Debug.Log($"Body segment {i}: null");
                }
            }
        }
        
        Debug.Log($"=== END DEATH STATE DEBUG ===");
    }

    [Command("test-death-with-positions", "Test snake death with stored positions for food spawning")]
    public void TestDeathWithPositions()
    {
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("NetworkManager not found!");
            return;
        }

        var localPlayer = PlayerManager.Instance?.GetLocalPlayerSnake();
        if (localPlayer == null)
        {
            Debug.LogError("Local player not found!");
            return;
        }

        var snakeController = localPlayer.GetComponent<SnakeController>();
        if (snakeController == null)
        {
            Debug.LogError("SnakeController not found on local player!");
            return;
        }

        Debug.Log($"=== DEATH WITH POSITIONS TEST ===");
        Debug.Log($"Player ID: {snakeController.playerId}");
        Debug.Log($"Is Alive: {snakeController.isAlive}");
        Debug.Log($"Server Is Alive: {snakeController.serverIsAlive}");
        Debug.Log($"Body Segments Count: {snakeController.bodySegments?.Count ?? 0}");
        
        // Store current positions
        Vector3 headPosition = snakeController.transform.position;
        List<Vector3> segmentPositions = new List<Vector3>();
        foreach (var segment in snakeController.bodySegments)
        {
            if (segment != null)
            {
                segmentPositions.Add(segment.position);
                Debug.Log($"Segment position: {segment.position}");
            }
        }
        
        Debug.Log($"Head position: {headPosition}");
        Debug.Log($"Stored {segmentPositions.Count} segment positions");
        
        // Manually trigger death with stored positions
        Debug.Log("Manually triggering death with stored positions...");
        snakeController.Die();
        
        Debug.Log($"=== END DEATH WITH POSITIONS TEST ===");
    }

    [Command("test-client-movement", "Check client-side movement continuation status")]
    public void TestClientMovement()
    {
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("NetworkManager not found!");
            return;
        }

        var localPlayer = PlayerManager.Instance?.GetLocalPlayerSnake();
        if (localPlayer == null)
        {
            Debug.LogError("Local player not found!");
            return;
        }

        var snakeController = localPlayer.GetComponent<SnakeController>();
        if (snakeController == null)
        {
            Debug.LogError("SnakeController not found on local player!");
            return;
        }

        Debug.Log($"=== CLIENT MOVEMENT STATUS ===");
        Debug.Log($"Player ID: {snakeController.playerId}");
        Debug.Log($"Has Received First Update: {snakeController.hasReceivedFirstUpdate}");
        Debug.Log($"Last Server Update Time: {snakeController.lastServerUpdateTime:F2}");
        Debug.Log($"Time Since Last Server Update: {Time.time - snakeController.lastServerUpdateTime:F2}s");
        Debug.Log($"Server Timeout Threshold: 0.5s");
        Debug.Log($"Is Using Client Prediction: {snakeController.isUsingClientPrediction}");
        Debug.Log($"Last Known Direction: {snakeController.lastKnownDirection}");
        Debug.Log($"Last Known Speed: {snakeController.lastKnownSpeed:F2}");
        Debug.Log($"Last Known Boosting: {snakeController.lastKnownBoosting}");
        Debug.Log($"Current Movement Direction: {snakeController.movementDirection}");
        Debug.Log($"Current Position: {snakeController.transform.position}");
        Debug.Log($"Server Position: {snakeController.serverPosition}");
        Debug.Log($"=== END CLIENT MOVEMENT STATUS ===");
    }

    [Command("test-disconnect", "Simulate player disconnect to test death handling")]
    public void TestDisconnect()
    {
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("NetworkManager not found!");
            return;
        }

        Debug.Log("=== DISCONNECT TEST ===");
        Debug.Log("Simulating player disconnect...");
        
        // Disconnect from the server
        NetworkManager.Instance.Disconnect();
        
        Debug.Log("Disconnect initiated - check if food spawns at snake positions");
        Debug.Log("=== END DISCONNECT TEST ===");
    }

    [Command("test-death-reset", "Reset death state and test death again")]
    public void TestDeathReset()
    {
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("NetworkManager not found!");
            return;
        }

        var localPlayer = PlayerManager.Instance?.GetLocalPlayerSnake();
        if (localPlayer == null)
        {
            Debug.LogError("Local player not found!");
            return;
        }

        var snakeController = localPlayer.GetComponent<SnakeController>();
        if (snakeController == null)
        {
            Debug.LogError("SnakeController not found on local player!");
            return;
        }

        Debug.Log($"=== DEATH RESET TEST ===");
        Debug.Log($"Player ID: {snakeController.playerId}");
        Debug.Log($"Before reset - Is Alive: {snakeController.isAlive}");
        Debug.Log($"Before reset - Server Is Alive: {snakeController.serverIsAlive}");
        Debug.Log($"Before reset - Body Segments Count: {snakeController.bodySegments?.Count ?? 0}");
        
        // Force reset death state using reflection to access private field
        var hasDiedField = typeof(SnakeController).GetField("hasDied", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (hasDiedField != null)
        {
            hasDiedField.SetValue(snakeController, false);
            Debug.Log("Reset hasDied flag to false");
        }
        
        // Reset alive states
        snakeController.isAlive = true;
        snakeController.serverIsAlive = true;
        snakeController.gameObject.SetActive(true);
        
        Debug.Log($"After reset - Is Alive: {snakeController.isAlive}");
        Debug.Log($"After reset - Server Is Alive: {snakeController.serverIsAlive}");
        Debug.Log($"After reset - Body Segments Count: {snakeController.bodySegments?.Count ?? 0}");
        
        // Manually trigger death again
        Debug.Log("Manually triggering death again...");
        snakeController.Die();
        
        Debug.Log($"=== END DEATH RESET TEST ===");
    }

    [Command("test-cleanup-segments", "Manually trigger segment cleanup")]
    public void TestCleanupSegments()
    {
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("NetworkManager not found!");
            return;
        }

        var localPlayer = PlayerManager.Instance?.GetLocalPlayerSnake();
        if (localPlayer == null)
        {
            Debug.LogError("Local player not found!");
            return;
        }

        var snakeController = localPlayer.GetComponent<SnakeController>();
        if (snakeController == null)
        {
            Debug.LogError("SnakeController not found on local player!");
            return;
        }

        Debug.Log($"=== SEGMENT CLEANUP TEST ===");
        Debug.Log($"Player ID: {snakeController.playerId}");
        Debug.Log($"Body Segments Count: {snakeController.bodySegments?.Count ?? 0}");
        Debug.Log($"Is Alive: {snakeController.isAlive}");
        Debug.Log($"Has Died: {snakeController.hasDied}");
        
        // Count all BodySegment objects in scene
        BodySegment[] allBodySegments = FindObjectsOfType<BodySegment>();
        Debug.Log($"Total BodySegment objects in scene: {allBodySegments.Length}");
        
        // Manually trigger death if not already dead
        if (snakeController.isAlive && !snakeController.hasDied)
        {
            Debug.Log("Manually triggering death...");
            snakeController.Die();
        }
        else
        {
            Debug.Log("Snake is already dead, resetting and testing again...");
            // Reset death state
            snakeController.isAlive = true;
            snakeController.serverIsAlive = true;
            snakeController.hasDied = false;
            snakeController.gameObject.SetActive(true);
            
            // Trigger death again
            snakeController.Die();
        }
        
        Debug.Log($"=== END SEGMENT CLEANUP TEST ===");
    }

    [Command("test-respawn-cleanup", "Test respawn cleanup functionality")]
    public void TestRespawnCleanup()
    {
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("NetworkManager not found!");
            return;
        }

        var localPlayer = PlayerManager.Instance?.GetLocalPlayerSnake();
        if (localPlayer == null)
        {
            Debug.LogError("Local player not found!");
            return;
        }

        var snakeController = localPlayer.GetComponent<SnakeController>();
        if (snakeController == null)
        {
            Debug.LogError("SnakeController not found on local player!");
            return;
        }

        Debug.Log($"=== RESPAWN CLEANUP TEST ===");
        Debug.Log($"Player ID: {snakeController.playerId}");
        Debug.Log($"Is Alive: {snakeController.isAlive}");
        Debug.Log($"Has Died: {snakeController.hasDied}");
        
        // Count current segments and food
        BodySegment[] allBodySegments = FindObjectsOfType<BodySegment>();
        Food[] allFood = FindObjectsOfType<Food>();
        Debug.Log($"Current BodySegment objects: {allBodySegments.Length}");
        Debug.Log($"Current Food objects: {allFood.Length}");
        
        // Test death first
        if (snakeController.isAlive && !snakeController.hasDied)
        {
            Debug.Log("Step 1: Triggering death...");
            snakeController.Die();
            
            // Wait a moment then check
            StartCoroutine(CheckAfterDeath());
        }
        else
        {
            Debug.Log("Snake is already dead, testing respawn cleanup...");
            TestRespawnCleanupStep();
        }
        
        Debug.Log($"=== END RESPAWN CLEANUP TEST ===");
    }
    
    private System.Collections.IEnumerator CheckAfterDeath()
    {
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("Step 2: Checking after death...");
        BodySegment[] segmentsAfterDeath = FindObjectsOfType<BodySegment>();
        Food[] foodAfterDeath = FindObjectsOfType<Food>();
        Debug.Log($"BodySegment objects after death: {segmentsAfterDeath.Length}");
        Debug.Log($"Food objects after death: {foodAfterDeath.Length}");
        
        // Test respawn cleanup
        TestRespawnCleanupStep();
    }
    
    private void TestRespawnCleanupStep()
    {
        var localPlayer = PlayerManager.Instance?.GetLocalPlayerSnake();
        if (localPlayer == null) return;
        
        var snakeController = localPlayer.GetComponent<SnakeController>();
        if (snakeController == null) return;
        
        Debug.Log("Step 3: Testing respawn cleanup...");
        
        // Simulate respawn by reinitializing
        snakeController.InitializeSnake(snakeController.playerId);
        
        // Check after respawn
        BodySegment[] segmentsAfterRespawn = FindObjectsOfType<BodySegment>();
        Food[] foodAfterRespawn = FindObjectsOfType<Food>();
        Debug.Log($"BodySegment objects after respawn: {segmentsAfterRespawn.Length}");
        Debug.Log($"Food objects after respawn: {foodAfterRespawn.Length}");
        
        Debug.Log("Respawn cleanup test completed!");
    }

    [Command("test-cleanup-debug-markers", "Manually clean up all debug markers")]
    public void TestCleanupDebugMarkers()
    {
        Debug.Log("=== DEBUG MARKERS CLEANUP TEST ===");
        
        // Count current debug markers
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int debugMarkerCount = 0;
        
        foreach (var obj in allObjects)
        {
            if (obj != null && obj.name.StartsWith("ServerSegment_"))
            {
                debugMarkerCount++;
                Debug.Log($"Found debug marker: {obj.name}");
            }
        }
        
        Debug.Log($"Found {debugMarkerCount} debug markers before cleanup");
        
        // Clean up all debug markers
        int cleanedCount = 0;
        foreach (var obj in allObjects)
        {
            if (obj != null && obj.name.StartsWith("ServerSegment_"))
            {
                Debug.Log($"Destroying debug marker: {obj.name}");
                DestroyImmediate(obj);
                cleanedCount++;
            }
        }
        
        Debug.Log($"Cleaned up {cleanedCount} debug markers");
        
        // Verify cleanup
        GameObject[] remainingObjects = FindObjectsOfType<GameObject>();
        int remainingCount = 0;
        foreach (var obj in remainingObjects)
        {
            if (obj != null && obj.name.StartsWith("ServerSegment_"))
            {
                remainingCount++;
                Debug.Log($"Remaining debug marker: {obj.name}");
            }
        }
        
        Debug.Log($"Remaining debug markers after cleanup: {remainingCount}");
        Debug.Log("=== END DEBUG MARKERS CLEANUP TEST ===");
    }

    [Command("debug-local-player", "Debug local player identification")]
    public void DebugLocalPlayer()
    {
        Debug.Log("=== LOCAL PLAYER DEBUG ===");
        
        if (PlayerManager.Instance == null)
        {
            Debug.LogError("PlayerManager.Instance is null!");
            return;
        }
        
        PlayerManager.Instance.DebugLocalPlayerInfo();
        
        // Check camera controller
        CameraController cameraController = FindObjectOfType<CameraController>();
        if (cameraController != null)
        {
            Debug.Log($"Camera Controller: AutoFollow={cameraController.IsFollowingTarget()}, Target={cameraController.GetCurrentTarget()?.name ?? "null"}");
        }
        else
        {
            Debug.LogError("CameraController not found!");
        }
        
        Debug.Log("=== END LOCAL PLAYER DEBUG ===");
    }

    [Command("set-local-player", "Manually set local player ID")]
    public void SetLocalPlayer(int playerId)
    {
        Debug.Log($"=== MANUALLY SETTING LOCAL PLAYER ===");
        
        if (PlayerManager.Instance == null)
        {
            Debug.LogError("PlayerManager.Instance is null!");
            return;
        }
        
        PlayerManager.Instance.DebugSetLocalPlayer(playerId);
        
        // Also update camera
        CameraController cameraController = FindObjectOfType<CameraController>();
        if (cameraController != null)
        {
            GameObject localSnake = PlayerManager.Instance.GetLocalPlayerSnake();
            if (localSnake != null)
            {
                cameraController.SetTarget(localSnake);
                Debug.Log($"Camera now following: {localSnake.name}");
            }
        }
        
        Debug.Log("=== END SETTING LOCAL PLAYER ===");
    }

    [Command("list-all-players", "List all players in the game")]
    public void ListAllPlayers()
    {
        Debug.Log("=== ALL PLAYERS LIST ===");
        
        // Get current game state
        if (NetworkManager.Instance != null)
        {
            var gameState = NetworkManager.Instance.GetCurrentGameState();
            if (gameState != null && gameState.players != null)
            {
                Debug.Log($"Total players in game state: {gameState.players.Length}");
                foreach (var player in gameState.players)
                {
                    Debug.Log($"Player {player.id}: Name='{player.name}', IsBot={player.isBot}, IsAlive={player.isAlive}, Pos=({player.x:F1}, {player.y:F1})");
                }
            }
            else
            {
                Debug.Log("No game state or players available");
            }
        }
        else
        {
            Debug.LogError("NetworkManager.Instance is null!");
        }
        
        // Check PlayerManager
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.DebugLocalPlayerInfo();
        }
        
        Debug.Log("=== END ALL PLAYERS LIST ===");
    }

    [Command("test-local-player-fix", "Test and fix local player identification")]
    public void TestLocalPlayerFix()
    {
        Debug.Log("=== LOCAL PLAYER FIX TEST ===");
        
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("[ERROR] NetworkManager not found!");
            return;
        }
        
        if (PlayerManager.Instance == null)
        {
            Debug.LogError("[ERROR] PlayerManager not found!");
            return;
        }
        
        // Check current state
        Debug.Log($"Current room ID: {NetworkManager.Instance.GetCurrentRoomId()}");
        Debug.Log($"Is in room: {NetworkManager.Instance.IsInRoom()}");
        
        // Get current game state
        var gameState = NetworkManager.Instance.GetCurrentGameState();
        if (gameState != null && gameState.players != null)
        {
            Debug.Log($"Total players in game state: {gameState.players.Length}");
            
            // Find the first non-bot player
            var realPlayer = System.Array.Find(gameState.players, p => !p.isBot && p.id > 0);
            if (realPlayer != null)
            {
                Debug.Log($"[FOUND] Real player: ID={realPlayer.id}, Name={realPlayer.name}");
                
                // Manually set this as the local player
                PlayerManager.Instance.SetLocalPlayerId(realPlayer.id);
                Debug.Log($"[SET] Manually set local player ID to: {realPlayer.id}");
                
                // Check if camera follows now
                CameraController cameraController = FindObjectOfType<CameraController>();
                if (cameraController != null)
                {
                    GameObject localSnake = PlayerManager.Instance.GetLocalPlayerSnake();
                    if (localSnake != null)
                    {
                        cameraController.SetTarget(localSnake);
                        Debug.Log($"[CAMERA] Camera now following: {localSnake.name}");
                    }
                    else
                    {
                        Debug.LogWarning("[CAMERA] No local snake found after setting ID");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[ERROR] No real player found in game state!");
                
                // List all players for debugging
                foreach (var player in gameState.players)
                {
                    Debug.Log($"Player {player.id}: {player.name} (Bot: {player.isBot})");
                }
            }
        }
        else
        {
            Debug.LogWarning("[ERROR] No game state or players available!");
        }
        
        Debug.Log("=== END LOCAL PLAYER FIX TEST ===");
    }

    [Command("test-real-player-spawn", "Check if real player snake is being spawned")]
    public void TestRealPlayerSpawn()
    {
        Debug.Log("=== REAL PLAYER SPAWN TEST ===");
        
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("[ERROR] NetworkManager not found!");
            return;
        }
        
        if (PlayerManager.Instance == null)
        {
            Debug.LogError("[ERROR] PlayerManager not found!");
            return;
        }
        
        // Check current state
        Debug.Log($"Current room ID: {NetworkManager.Instance.GetCurrentRoomId()}");
        Debug.Log($"Is in room: {NetworkManager.Instance.IsInRoom()}");
        Debug.Log($"Local player ID: {PlayerManager.Instance.GetLocalPlayerId()}");
        Debug.Log($"Local player ID set: {PlayerManager.Instance.IsLocalPlayerIdSet()}");
        
        // Get current game state
        var gameState = NetworkManager.Instance.GetCurrentGameState();
        if (gameState != null && gameState.players != null)
        {
            Debug.Log($"Total players in game state: {gameState.players.Length}");
            
            // Count bots vs real players
            int botCount = 0;
            int realPlayerCount = 0;
            
            foreach (var player in gameState.players)
            {
                if (player.isBot)
                {
                    botCount++;
                    Debug.Log($"Bot: ID={player.id}, Name={player.name}, Alive={player.isAlive}");
                }
                else
                {
                    realPlayerCount++;
                    Debug.Log($"Real Player: ID={player.id}, Name={player.name}, Alive={player.isAlive}");
                }
            }
            
            Debug.Log($"Bot count: {botCount}, Real player count: {realPlayerCount}");
            
            // Check if there are any real players
            if (realPlayerCount == 0)
            {
                Debug.LogError("[ERROR] No real players found! This suggests the real player snake is not being spawned.");
                Debug.Log("[SUGGESTION] Try reconnecting or check server logs for real player spawn issues.");
            }
            else
            {
                Debug.Log("[SUCCESS] Real players found in game state");
            }
        }
        else
        {
            Debug.LogWarning("[ERROR] No game state or players available!");
        }
        
        // Check if there are any snake GameObjects in the scene
        var allSnakes = FindObjectsOfType<SnakeController>();
        Debug.Log($"Snake GameObjects in scene: {allSnakes.Length}");
        
        foreach (var snake in allSnakes)
        {
            var playerId = snake.GetPlayerId();
            var isLocal = snake.IsLocalPlayer();
            Debug.Log($"Snake GameObject: PlayerID={playerId}, IsLocal={isLocal}, Name={snake.name}");
        }
        
        Debug.Log("=== END REAL PLAYER SPAWN TEST ===");
    }

    [Command("test-manual-room-join", "Manually trigger room join to test real player spawning")]
    public async void TestManualRoomJoin()
    {
        Debug.Log("=== MANUAL ROOM JOIN TEST ===");
        
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("[ERROR] NetworkManager not found!");
            return;
        }
        
        if (!NetworkManager.Instance.IsConnected())
        {
            Debug.LogError("[ERROR] Not connected to server!");
            return;
        }
        
        Debug.Log("[INFO] Attempting to auto-join a room...");
        
        try
        {
            var room = await NetworkManager.Instance.AutoJoinRoom();
            if (room != null)
            {
                Debug.Log($"[SUCCESS] Auto-joined room: {room.name} (ID: {room.id})");
                
                // Wait a bit for the game state to update
                await Task.Delay(2000);
                
                // Check the game state after joining
                var gameState = NetworkManager.Instance.GetCurrentGameState();
                if (gameState != null && gameState.players != null)
                {
                    Debug.Log($"Players in room after join: {gameState.players.Length}");
                    
                    var realPlayer = System.Array.Find(gameState.players, p => !p.isBot);
                    if (realPlayer != null)
                    {
                        Debug.Log($"[SUCCESS] Real player found: ID={realPlayer.id}, Name={realPlayer.name}");
                    }
                    else
                    {
                        Debug.LogWarning("[WARNING] No real player found after room join");
                    }
                }
            }
            else
            {
                Debug.LogError("[ERROR] Failed to auto-join room");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ERROR] Exception during room join: {e.Message}");
        }
        
        Debug.Log("=== END MANUAL ROOM JOIN TEST ===");
    }

    [Command("test-bot-movement", "Debug bot movement and check if bots are moving properly")]
    public void TestBotMovement()
    {
        if (!isInitialized || networkManager == null)
        {
            Debug.LogError("NetworkManager not initialized!");
            return;
        }

        Debug.Log("=== BOT MOVEMENT DEBUG ===");
        
        // Check current game state
        var gameState = networkManager.GetCurrentGameState();
        if (gameState == null)
        {
            Debug.LogError("No game state available!");
            return;
        }

        Debug.Log($"Total players in game state: {gameState.players.Length}");
        
        int botCount = 0;
        int aliveBotCount = 0;
        int deadBotCount = 0;
        
        foreach (var player in gameState.players)
        {
            if (player.isBot)
            {
                botCount++;
                if (player.isAlive)
                {
                    aliveBotCount++;
                    Debug.Log($"Bot {player.id} ({player.name}) - ALIVE - Position: ({player.x:F1}, {player.y:F1}), Length: {player.length}, Score: {player.score}");
                }
                else
                {
                    deadBotCount++;
                    Debug.Log($"Bot {player.id} ({player.name}) - DEAD - Position: ({player.x:F1}, {player.y:F1}), Length: {player.length}, Score: {player.score}");
                }
            }
        }
        
        Debug.Log($"Bot Summary: Total={botCount}, Alive={aliveBotCount}, Dead={deadBotCount}");
        
        // Check if we have a local player
        var playerManager = FindObjectOfType<PlayerManager>();
        var localPlayerId = playerManager != null ? playerManager.GetLocalPlayerId() : -1;
        var localPlayer = System.Array.Find(gameState.players, p => p.id == localPlayerId);
        if (localPlayer != null)
        {
            Debug.Log($"Local Player: {localPlayer.id} ({localPlayer.name}) - Alive: {localPlayer.isAlive}, Position: ({localPlayer.x:F1}, {localPlayer.y:F1})");
        }
        else
        {
            Debug.LogWarning("No local player found in game state!");
        }
    }

    [Command("test-bot-collision", "Debug bot collision detection and death handling")]
    public void TestBotCollision()
    {
        if (!isInitialized || networkManager == null)
        {
            Debug.LogError("NetworkManager not initialized!");
            return;
        }

        Debug.Log("=== BOT COLLISION DEBUG ===");
        
        // Check current game state
        var gameState = networkManager.GetCurrentGameState();
        if (gameState == null)
        {
            Debug.LogError("No game state available!");
            return;
        }

        // Find local player
        var playerManager = FindObjectOfType<PlayerManager>();
        var localPlayerId = playerManager != null ? playerManager.GetLocalPlayerId() : -1;
        var localPlayer = System.Array.Find(gameState.players, p => p.id == localPlayerId);
        if (localPlayer == null)
        {
            Debug.LogError("No local player found!");
            return;
        }

        Debug.Log($"Local Player: {localPlayer.id} - Position: ({localPlayer.x:F1}, {localPlayer.y:F1}), Alive: {localPlayer.isAlive}");

        // Check for nearby bots
        var nearbyBots = new List<NetworkManager.GamePlayer>();
        foreach (var player in gameState.players)
        {
            if (player.isBot && player.isAlive)
            {
                float distance = Vector2.Distance(
                    new Vector2(localPlayer.x, localPlayer.y),
                    new Vector2(player.x, player.y)
                );
                
                if (distance < 10f) // Within 10 units
                {
                    nearbyBots.Add(player);
                    Debug.Log($"Nearby Bot: {player.id} ({player.name}) - Distance: {distance:F1}, Position: ({player.x:F1}, {player.y:F1})");
                }
            }
        }

        if (nearbyBots.Count == 0)
        {
            Debug.Log("No nearby bots found. Try moving closer to a bot to test collision.");
        }
        else
        {
            Debug.Log($"Found {nearbyBots.Count} nearby bots. Try colliding with them to test death handling.");
        }

        // Check PlayerManager for local snake
        if (playerManager != null)
        {
            var localSnake = playerManager.GetLocalPlayerSnake();
            if (localSnake != null)
            {
                var snakeController = localSnake.GetComponent<SnakeController>();
                if (snakeController != null)
                {
                    Debug.Log($"Local Snake: ID={snakeController.GetPlayerId()}, Alive={snakeController.isAlive}, Position={localSnake.transform.position}");
                }
                else
                {
                    Debug.LogWarning("Local snake has no SnakeController component!");
                }
            }
            else
            {
                Debug.LogWarning("No local snake found in PlayerManager!");
            }
        }
        else
        {
            Debug.LogError("PlayerManager not found!");
        }
    }

    [Command("test-food-debug", "Debug food spawning and collision issues")]
    public void TestFoodDebug()
    {
        if (!isInitialized || networkManager == null)
        {
            Debug.LogError("NetworkManager not initialized!");
            return;
        }

        Debug.Log("=== FOOD DEBUG ===");
        
        // Check current game state
        var gameState = networkManager.GetCurrentGameState();
        if (gameState == null)
        {
            Debug.LogError("No game state available!");
            return;
        }

        Debug.Log($"Total food items in game state: {gameState.food.Length}");
        
        // Count food by type
        int regularFood = 0;
        int boostFood = 0;
        int deathFood = 0;
        
        foreach (var food in gameState.food)
        {
            if (food.type == "regular")
                regularFood++;
            else if (food.type == "boost")
                boostFood++;
            
            if (food.id.Contains("death"))
                deathFood++;
        }
        
        Debug.Log($"Food breakdown: Regular={regularFood}, Boost={boostFood}, Death food={deathFood}");
        
        // Check for food explosion (too many food items)
        if (gameState.food.Length > 100)
        {
            Debug.LogWarning($"WARNING: Too many food items ({gameState.food.Length}) - this may cause performance issues!");
        }
        
        // Check PlayerManager for food objects
        var playerManager = FindObjectOfType<PlayerManager>();
        if (playerManager != null)
        {
            var foodObjects = FindObjectsOfType<Food>();
            Debug.Log($"Food GameObjects in scene: {foodObjects.Length}");
            
            if (foodObjects.Length > 50)
            {
                Debug.LogWarning($"WARNING: Too many food GameObjects ({foodObjects.Length}) - this may cause performance issues!");
            }
        }
        else
        {
            Debug.LogError("PlayerManager not found!");
        }
    }

    [Command("test-game-stats", "Test game stats panel updates")]
    public void TestGameStats()
    {
        Debug.Log("=== TESTING GAME STATS PANEL ===");
        
        // Find GameStatsPanel
        var gameStatsPanel = FindObjectOfType<GameStatsPanel>();
        if (gameStatsPanel == null)
        {
            Debug.LogError("GameStatsPanel not found in scene!");
            return;
        }
        
        Debug.Log("GameStatsPanel found!");
        
        // Get current game state
        var gameState = NetworkManager.Instance?.GetCurrentGameState();
        if (gameState == null)
        {
            Debug.LogError("No game state available!");
            return;
        }
        
        Debug.Log($"Game state has {gameState.players?.Length ?? 0} players");
        
        // Manually trigger game state update
        if (gameState.players != null && gameState.players.Length > 0)
        {
            Debug.Log("Manually triggering game state update...");
            NetworkManager.Instance.OnGameStateUpdated?.Invoke(gameState);
        }
        
        // Test setting player ID
        if (PlayerManager.Instance != null && PlayerManager.Instance.IsLocalPlayerIdSet())
        {
            int playerId = PlayerManager.Instance.GetLocalPlayerId();
            Debug.Log($"Setting GameStatsPanel player ID to: {playerId}");
            gameStatsPanel.SetCurrentPlayerId(playerId);
        }
        else
        {
            Debug.LogWarning("PlayerManager local player ID not set!");
        }
    }

    [Command("test-leaderboard", "Test leaderboard functionality")]
    public void TestLeaderboard()
    {
        Debug.Log("=== TESTING LEADERBOARD ===");
        
        // Find GameStatsPanel
        var gameStatsPanel = FindObjectOfType<GameStatsPanel>();
        if (gameStatsPanel == null)
        {
            Debug.LogError("GameStatsPanel not found in scene!");
            return;
        }
        
        Debug.Log("GameStatsPanel found!");
        
        // Check leaderboard components
        var leaderboardContent = gameStatsPanel.GetType().GetField("leaderboardContent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(gameStatsPanel) as Transform;
        var leaderboardEntryPrefab = gameStatsPanel.GetType().GetField("leaderboardEntryPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(gameStatsPanel) as GameObject;
        var leaderboardEntries = gameStatsPanel.GetType().GetField("leaderboardEntries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(gameStatsPanel) as System.Collections.Generic.List<GameObject>;
        
        Debug.Log($"Leaderboard components - Content: {leaderboardContent != null}, Prefab: {leaderboardEntryPrefab != null}, Entries: {leaderboardEntries?.Count ?? 0}");
        
        // Check if leaderboard is visible
        var statsPanel = gameStatsPanel.GetType().GetField("statsPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(gameStatsPanel) as GameObject;
        Debug.Log($"Stats panel active: {statsPanel?.activeInHierarchy}");
        
        // Get current game state
        var gameState = NetworkManager.Instance?.GetCurrentGameState();
        if (gameState == null)
        {
            Debug.LogError("No game state available!");
            return;
        }
        
        Debug.Log($"Game state has {gameState.players?.Length ?? 0} players");
        
        // Manually trigger game state update
        if (gameState.players != null && gameState.players.Length > 0)
        {
            Debug.Log("Manually triggering game state update...");
            NetworkManager.Instance.OnGameStateUpdated?.Invoke(gameState);
        }
        
        // Test setting player ID
        if (PlayerManager.Instance != null && PlayerManager.Instance.IsLocalPlayerIdSet())
        {
            int playerId = PlayerManager.Instance.GetLocalPlayerId();
            Debug.Log($"Setting GameStatsPanel player ID to: {playerId}");
            gameStatsPanel.SetCurrentPlayerId(playerId);
        }
        else
        {
            Debug.LogWarning("PlayerManager local player ID not set!");
        }
        
        // Force update
        Debug.Log("Forcing stats update...");
        gameStatsPanel.SendMessage("UpdateStats", SendMessageOptions.DontRequireReceiver);
    }

    [Command("test-death-ui", "Test death UI flow and comprehensive stats panel")]
    public void TestDeathUI()
    {
        Debug.Log("=== TESTING DEATH UI FLOW ===");
        
        // Use UIManager singleton for consistent UI state management
        if (UIManager.Instance == null)
        {
            Debug.LogError("UIManager.Instance is null! Please ensure UIManager is properly set up.");
            return;
        }
        
        Debug.Log("UIManager.Instance found!");
        
        // Check comprehensive stats panel
        var comprehensiveStatsPanel = FindObjectOfType<ComprehensiveStatsPanel>();
        if (comprehensiveStatsPanel == null)
        {
            Debug.LogError("ComprehensiveStatsPanel not found!");
            return;
        }
        
        Debug.Log("ComprehensiveStatsPanel found!");
        
        // Check session stats component
        var sessionStats = comprehensiveStatsPanel.GetType().GetField("sessionStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(comprehensiveStatsPanel) as SessionStatsComponent;
        if (sessionStats == null)
        {
            Debug.LogError("SessionStatsComponent not found!");
            return;
        }
        
        Debug.Log("SessionStatsComponent found!");
        
        // Check all time stats component
        var allTimeStats = comprehensiveStatsPanel.GetType().GetField("allTimeStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(comprehensiveStatsPanel) as AllTimeStatsComponent;
        if (allTimeStats == null)
        {
            Debug.LogError("AllTimeStatsComponent not found!");
            return;
        }
        
        Debug.Log("AllTimeStatsComponent found!");
        
        // Manually trigger death UI flow
        Debug.Log("Manually triggering death UI flow...");
        UIManager.Instance.OnPlayerDeath(150, 3, 2);
        
        Debug.Log("=== END DEATH UI TEST ===");
    }

    [Command("test-leaderboard-update", "Test leaderboard update functionality")]
    public void TestLeaderboardUpdate()
    {
        Debug.Log("=== TESTING LEADERBOARD UPDATE ===");
        
        // Find GameStatsPanel
        var gameStatsPanel = FindObjectOfType<GameStatsPanel>();
        if (gameStatsPanel == null)
        {
            Debug.LogError("GameStatsPanel not found!");
            return;
        }
        
        Debug.Log("GameStatsPanel found!");
        
        // Get current game state
        var gameState = NetworkManager.Instance?.GetCurrentGameState();
        if (gameState == null)
        {
            Debug.LogError("No game state available!");
            return;
        }
        
        Debug.Log($"Game state has {gameState.players?.Length ?? 0} players");
        
        // Log all players in game state
        if (gameState.players != null)
        {
            Debug.Log("All players in game state:");
            foreach (var player in gameState.players)
            {
                Debug.Log($"  Player {player.id} ({player.name}): Score={player.score}, Kills={player.kills}, Alive={player.isAlive}, Bot={player.isBot}");
            }
        }
        
        // Manually trigger game state update
        Debug.Log("Manually triggering game state update...");
        NetworkManager.Instance.OnGameStateUpdated?.Invoke(gameState);
        
        // Force update stats
        Debug.Log("Forcing stats update...");
        gameStatsPanel.SendMessage("UpdateStats", SendMessageOptions.DontRequireReceiver);
        
        Debug.Log("=== END LEADERBOARD UPDATE TEST ===");
    }
} 