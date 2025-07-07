using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class GameStatsPanel : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject statsPanel;

    [Header("Current Score")]
    public TextMeshProUGUI currentScoreText;

    [Header("Room Info")]
    public TextMeshProUGUI friendCodeText;

    [Header("Leaderboard")]
    public Transform leaderboardContent;
    public GameObject leaderboardEntryPrefab;
    public TextMeshProUGUI leaderboardTitleText;

    [Header("Settings")]
    public bool showLeaderboard = true;
    public int maxLeaderboardEntries = 10;
    public float updateInterval = 0.5f;

    private List<GameObject> leaderboardEntries = new List<GameObject>();
    private List<PlayerData> currentPlayers = new List<PlayerData>();
    private int currentPlayerId = -1;
    private string currentFriendCode = "";
    private float lastUpdateTime;

    [System.Serializable]
    public class PlayerData
    {
        public int id;
        public string name;
        public int score;
        public int kills;
        public int length;
        public bool isPlayer;
        public bool isAlive;
    }

    private void Start()
    {
        // Subscribe to game events
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnGameStateUpdated += OnGameStateUpdated;
        }

        // Initialize UI
        InitializeLeaderboard();
        UpdatePanelVisibility(true);
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnGameStateUpdated -= OnGameStateUpdated;
        }
    }

    private void Update()
    {
        // Update stats at regular intervals
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateStats();
            lastUpdateTime = Time.time;
        }
    }

    private void InitializeLeaderboard()
    {
        // Clear existing entries
        ClearLeaderboard();

        // Create leaderboard title
        if (leaderboardTitleText != null)
        {
            leaderboardTitleText.text = "TOP 10 SNAKES";
        }

        // Debug check for required components
        if (leaderboardEntryPrefab == null)
        {
            Debug.LogError("GameStatsPanel: leaderboardEntryPrefab is null!");
            return;
        }
        
        if (leaderboardContent == null)
        {
            Debug.LogError("GameStatsPanel: leaderboardContent is null!");
            return;
        }

        // Create entry placeholders
        for (int i = 0; i < maxLeaderboardEntries; i++)
        {
            CreateLeaderboardEntry(i + 1);
        }
        
        Debug.Log($"GameStatsPanel: Initialized leaderboard with {leaderboardEntries.Count} entries");
    }

    private void CreateLeaderboardEntry(int position)
    {
        if (leaderboardEntryPrefab == null || leaderboardContent == null)
        {
            Debug.LogError($"CreateLeaderboardEntry: Missing required components for position {position}");
            return;
        }

        GameObject entry = Instantiate(leaderboardEntryPrefab, leaderboardContent);
        leaderboardEntries.Add(entry);

        // Get the LeaderboardEntry component
        LeaderboardEntry leaderboardEntry = entry.GetComponent<LeaderboardEntry>();
        
        if (leaderboardEntry == null)
        {
            Debug.LogError($"CreateLeaderboardEntry: Entry {position} missing LeaderboardEntry component");
            return;
        }

        // Set initial data using the component
        leaderboardEntry.SetData(position, "", 0, false, true);
        
        if (position == 1) // Only log for first entry to avoid spam
        {
            Debug.Log($"CreateLeaderboardEntry: Created entry {position} successfully");
        }
    }

    private void UpdateLeaderboardEntry(GameObject entry, int position, string playerName, int score, bool isPlayer, bool isAlive = true)
    {
        // Get the LeaderboardEntry component
        LeaderboardEntry leaderboardEntry = entry.GetComponent<LeaderboardEntry>();
        Image background = entry.GetComponent<Image>();

        if (leaderboardEntry == null)
        {
            Debug.LogError($"UpdateLeaderboardEntry: Entry missing LeaderboardEntry component");
            return;
        }

        // Set data using the component (handles text colors)
        leaderboardEntry.SetData(position, playerName, score, isPlayer, isAlive);

        // Highlight player's entry with background color
        if (background != null)
        {
            if (!isAlive)
            {
                background.color = new Color(0.5f, 0.5f, 0.5f, 0.2f); // Dead players with gray background
            }
            else if (isPlayer)
            {
                background.color = new Color(1f, 1f, 0f, 0.3f); // Local player with yellow background
            }
            else
            {
                background.color = new Color(0f, 0f, 0f, 0.1f); // Other players with normal background
            }
        }
    }

    private void ClearLeaderboard()
    {
        foreach (GameObject entry in leaderboardEntries)
        {
            if (entry != null)
                DestroyImmediate(entry);
        }
        leaderboardEntries.Clear();
    }

    private void OnGameStateUpdated(NetworkManager.GameState gameState)
    {
        if (gameState == null || gameState.players == null)
            return;

        // Update room ID (we'll get friend code from NetworkManager)
        // currentRoomId = gameState.roomId;

        // Auto-detect local player if not set
        if (currentPlayerId == -1)
        {
            // Try to get from PlayerManager first
            if (PlayerManager.Instance != null && PlayerManager.Instance.IsLocalPlayerIdSet())
            {
                currentPlayerId = PlayerManager.Instance.GetLocalPlayerId();
                Debug.Log($"GameStatsPanel: Auto-detected local player ID from PlayerManager: {currentPlayerId}");
            }
            // Fallback: find first non-bot player
            else
            {
                var localPlayer = gameState.players.FirstOrDefault(p => !p.isBot && p.id > 0);
                if (localPlayer != null)
                {
                    currentPlayerId = localPlayer.id;
                    Debug.Log($"GameStatsPanel: Auto-detected local player ID from game state: {currentPlayerId}");
                }
            }
        }

        // Update current players data
        currentPlayers.Clear();
        foreach (var player in gameState.players)
        {
            bool isLocalPlayer = (player.id == currentPlayerId);
            currentPlayers.Add(new PlayerData
            {
                id = player.id,
                name = player.name,
                score = player.score,
                kills = player.kills,
                length = player.length,
                isPlayer = isLocalPlayer,
                isAlive = player.isAlive
            });
        }

        // Sort by score (highest first) - include all players (bots and dead players)
        var sortedPlayers = currentPlayers
            .OrderByDescending(p => p.score)
            .ThenByDescending(p => p.kills)
            .ToList();
            
        // Ensure current player is always included in the list, even if they have low score
        var currentPlayer = sortedPlayers.FirstOrDefault(p => p.id == currentPlayerId);
        if (currentPlayer != null && !sortedPlayers.Take(maxLeaderboardEntries).Any(p => p.id == currentPlayerId))
        {
            // Current player is not in top entries, add them at the end
            var topPlayers = sortedPlayers.Take(maxLeaderboardEntries - 1).ToList();
            topPlayers.Add(currentPlayer);
            currentPlayers = topPlayers;
        }
        else
        {
            // Current player is already in top entries or not found
            currentPlayers = sortedPlayers.Take(maxLeaderboardEntries).ToList();
        }
            
        // Debug logging (commented out to reduce spam)
        // if (Time.frameCount % 60 == 0) // Log every 60 frames (about once per second)
        // {
        //     Debug.Log($"OnGameStateUpdated: Processed {gameState.players.Length} players, {currentPlayers.Count} players for leaderboard");
        //     if (currentPlayers.Count > 0)
        //     {
        //         Debug.Log($"Top player: {currentPlayers[0].name} (Score: {currentPlayers[0].score}, IsPlayer: {currentPlayers[0].isPlayer}, Alive: {currentPlayers[0].isAlive})");
        //     }
        // }
    }

    private void UpdateStats()
    {
        UpdateLeaderboard();
        UpdateCurrentScore();
        UpdateRoomInfo();
    }

    private void UpdateLeaderboard()
    {
        // Debug logging (commented out to reduce spam)
        // if (Time.frameCount % 60 == 0) // Log every 60 frames (about once per second)
        // {
        //     Debug.Log($"UpdateLeaderboard: showLeaderboard={showLeaderboard}, leaderboardEntries.Count={leaderboardEntries.Count}, currentPlayers.Count={currentPlayers.Count}");
        // }
        
        if (!showLeaderboard || leaderboardEntries.Count == 0)
        {
            // if (Time.frameCount % 60 == 0) // Log every 60 frames
            // {
            //     Debug.LogWarning($"UpdateLeaderboard: Skipping update - showLeaderboard={showLeaderboard}, leaderboardEntries.Count={leaderboardEntries.Count}");
            // }
            return;
        }

        // Update each leaderboard entry
        for (int i = 0; i < leaderboardEntries.Count; i++)
        {
            if (i < currentPlayers.Count)
            {
                var player = currentPlayers[i];
                UpdateLeaderboardEntry(
                    leaderboardEntries[i], 
                    i + 1, 
                    player.name, 
                    player.score, 
                    player.isPlayer,
                    player.isAlive
                );
                
                // Debug log occasionally (commented out to reduce spam)
                // if (Time.frameCount % 60 == 0 && i == 0) // Log first player every 60 frames
                // {
                //     Debug.Log($"UpdateLeaderboard: Updated entry {i + 1} - {player.name} (Score: {player.score}, IsPlayer: {player.isPlayer}, Alive: {player.isAlive})");
                // }
            }
            else
            {
                // Empty slot
                UpdateLeaderboardEntry(leaderboardEntries[i], i + 1, "", 0, false, true);
            }
        }
    }

    private void UpdateCurrentScore()
    {
        // Find current player data - first try to find by isPlayer flag, then by ID
        var currentPlayer = currentPlayers.FirstOrDefault(p => p.isPlayer);
        
        // If not found by isPlayer flag, try to find by ID directly
        if (currentPlayer == null && currentPlayerId != -1)
        {
            currentPlayer = currentPlayers.FirstOrDefault(p => p.id == currentPlayerId);
        }
        
        if (currentPlayer != null)
        {
            if (currentScoreText != null)
                currentScoreText.text = $"{currentPlayer.score}";
            
            // Debug log occasionally to track score updates (commented out to reduce spam)
            // if (Time.frameCount % 60 == 0) // Log every 60 frames (about once per second)
            // {
            //     Debug.Log($"GameStatsPanel: Current player score updated - ID: {currentPlayer.id}, Score: {currentPlayer.score}, Alive: {currentPlayer.isAlive}");
            // }
        }
        else
        {
            // Only log warning if we have a valid player ID and players in the list
            if (currentPlayerId != -1 && currentPlayers.Count > 0)
            {
                Debug.LogWarning($"GameStatsPanel: Player not found - CurrentPlayerId: {currentPlayerId}, TotalPlayers: {currentPlayers.Count}");
                
                // Debug: log all players for troubleshooting (commented out to reduce spam)
                // if (currentPlayers.Count > 0)
                // {
                //     Debug.Log("Available players:");
                //     foreach (var player in currentPlayers)
                //     {
                //         Debug.Log($"  Player {player.id} ({player.name}): Score={player.score}, Alive={player.isAlive}, IsPlayer={player.isPlayer}");
                //     }
                // }
            }

            // Player not found or not alive
            if (currentScoreText != null)
                currentScoreText.text = "0";
        }
    }

    private void UpdateRoomInfo()
    {
        if (friendCodeText != null)
        {
            // Get friend code from NetworkManager if available
            if (NetworkManager.Instance != null && NetworkManager.Instance.IsInRoom())
            {
                string friendCode = NetworkManager.Instance.GetCurrentFriendCode();
                if (!string.IsNullOrEmpty(friendCode))
                {
                    friendCodeText.text = $"Friend Code: {friendCode}";
                }
                else
                {
                    friendCodeText.text = "Friend Code: --";
                }
            }
            else
            {
                friendCodeText.text = "Friend Code: --";
            }
        }
    }

    private void UpdatePanelVisibility(bool isAlive)
    {
        if (statsPanel != null)
            statsPanel.SetActive(isAlive);
    }

    public void SetShowLeaderboard(bool show)
    {
        showLeaderboard = show;
    }

    public void SetUpdateInterval(float interval)
    {
        updateInterval = interval;
    }

    public void SetCurrentPlayerId(int playerId)
    {
        currentPlayerId = playerId;
    }
} 