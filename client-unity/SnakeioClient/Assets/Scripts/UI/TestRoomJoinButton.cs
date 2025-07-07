using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using System.Collections;
using System.Linq;

public class TestRoomJoinButton : MonoBehaviour
{
    [Header("UI References")]
    public Button playButton;
    public Button[] skinSelectionButtons;
    public TMP_InputField playerNameInput;
    public TMP_InputField roomCodeInput;
    public GameObject menuPanel;
    public GameObject gamePanel;
    public TextMeshProUGUI bestScoreText;
    public ChooseSkinPage chooseSkinPage;

    [Header("Animation Settings")]
    public float pulseSpeed = 2f;
    public float pulseIntensity = 0.1f;

    [Header("Game Settings")]
    public string defaultPlayerName = "Player";
    public bool showMenuOnDeath = true;

    [Header("Main Menu Skin Display")]
    public SpriteRenderer mainMenuHeadSprite;
    public SpriteRenderer[] mainMenuBodySprites;

    // Random name generation
    private string[] randomNames = {
        "SnakeMaster", "Viper", "Cobra", "Python", "Anaconda", "Rattlesnake", "KingCobra", "BlackMamba",
        "Copperhead", "Cottonmouth", "Boa", "Adder", "Viper", "Asp", "Taipan", "Krait", "SeaSnake",
        "TreeSnake", "GrassSnake", "WaterSnake", "GarterSnake", "RatSnake", "MilkSnake", "CornSnake",
        "BullSnake", "PineSnake", "Hognose", "Ringneck", "WormSnake", "BlindSnake", "ThreadSnake",
        "Slither", "Glide", "Slide", "Crawl", "Wriggle", "Slink", "Coil", "Wind", "Twist", "Spiral"
    };

    private bool isJoining = false;
    private bool isInGame = false;
    private Coroutine pulseCoroutine;
    private string currentPlayerName;
    private string currentRoomCode;

    private void Start()
    {
        // Set up button click handlers
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayButtonClicked);
        }

        if (skinSelectionButtons != null)
        {
            Debug.Log($"Setting up {skinSelectionButtons.Length} skin selection buttons");
            foreach (var button in skinSelectionButtons)
            {
                if (button != null)
        {
                    button.onClick.AddListener(OnSkinSelectionButtonClicked);
                    Debug.Log("Skin selection button listener added");
        }
                else
                {
                    Debug.LogWarning("Null button in skinSelectionButtons array!");
                }
            }
        }
        else
        {
            Debug.LogWarning("skinSelectionButtons array is null!");
        }

        // Initialize input fields
        InitializeInputFields();

        // Load and display best score
        LoadAndDisplayBestScore();

        // Initialize UI state
        ShowMenu();

        // Subscribe to game events
        SubscribeToGameEvents();

        // Update main menu skin display
        UpdateMainMenuSkinDisplay();
    }

    private void OnEnable()
    {
        // Update best score display every time the GameObject is enabled
        LoadAndDisplayBestScore();
        
        // Update main menu skin display every time the GameObject is enabled
        UpdateMainMenuSkinDisplay();
    }

    private void OnDestroy()
    {
        // Clean up button listeners
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(OnPlayButtonClicked);
        }
        
        if (skinSelectionButtons != null)
        {
            foreach (var button in skinSelectionButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveListener(OnSkinSelectionButtonClicked);
                }
            }
        }

        // Stop any running coroutines
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }

        // Unsubscribe from game events
        UnsubscribeFromGameEvents();
    }

    private void OnSkinSelectionButtonClicked()
    {
        Debug.Log("Skin selection button clicked!");
        
        // Use UIManager for consistent UI state management
        if (UIManager.Instance != null)
        {
            Debug.Log("UIManager.Instance found, calling ShowChooseSkinPanel()...");
            UIManager.Instance.ShowChooseSkinPanel();
            Debug.Log("Skin selection button clicked - using UIManager.ShowChooseSkinPanel()");
        }
        else
        {
            Debug.LogError("UIManager.Instance is null! Please ensure UIManager is properly set up.");
        }
    }

    private async void OnPlayButtonClicked()
    {
        if (isJoining) return;

        // Get player name
        currentPlayerName = GetPlayerName();
        if (string.IsNullOrEmpty(currentPlayerName))
        {
            Debug.LogError("Please enter a player name!");
            return;
        }

        // Check if room code is provided
        currentRoomCode = GetRoomCode();
        bool hasRoomCode = !string.IsNullOrEmpty(currentRoomCode);

        try
        {
            isJoining = true;
            StartPulseAnimation();

            Debug.Log($"Starting game with player: {currentPlayerName}, room code: {currentRoomCode}");

            // Check if NetworkManager is available
            if (NetworkManager.Instance == null)
            {
                Debug.LogError("NetworkManager not found!");
                return;
            }

            // Check if connected
            if (!NetworkManager.Instance.IsConnected())
            {
                Debug.LogError("Not connected to server!");
                return;
            }

            // Join room based on whether room code is provided
            if (hasRoomCode)
            {
                await JoinSpecificRoom(currentRoomCode);
            }
            else
            {
                await JoinRandomRoom();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Play Button Error: {e.Message}");
            // Show error message to user (you can add UI for this)
            Debug.LogWarning($"Failed to start game: {e.Message}");
        }
        finally
        {
            isJoining = false;
            StopPulseAnimation();
        }
    }

    private async Task JoinSpecificRoom(string roomCode)
    {
        // Check if NetworkManager is available
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("NetworkManager not found!");
            return;
        }

        // Determine if roomCode is a friend code or room ID
        bool isFriendCode = !int.TryParse(roomCode, out int roomId);
        
        try
        {
            Room room = null;
            
            if (isFriendCode)
            {
                // Normalize friend code to uppercase for consistency
                string normalizedFriendCode = roomCode.ToUpper();
                Debug.Log($"Joining room by friend code: {roomCode} (normalized: {normalizedFriendCode})");
                room = await NetworkManager.Instance.JoinRoomByFriendCode(normalizedFriendCode);
            }
            else
            {
                Debug.Log($"Joining room by ID: {roomId}");
                room = await NetworkManager.Instance.JoinRoom(roomId.ToString());
            }
            
            if (room != null)
            {
                Debug.Log($"Joined room: {room.name}!");
                StartGame();
            }
            else
            {
                Debug.LogError($"Failed to join room: {roomCode}");
                // Show error message to user (you can add UI for this)
                Debug.LogWarning($"Room '{roomCode}' not found. Try auto-join instead.");
                throw new System.Exception($"Room '{roomCode}' not found");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error joining room {roomCode}: {e.Message}");
            // Show error message to user (you can add UI for this)
            Debug.LogWarning($"Error joining room '{roomCode}': {e.Message}");
            throw; // Re-throw to ensure the finally block in OnPlayButtonClicked is executed
        }
    }

    private async Task JoinRandomRoom()
    {
        Debug.Log("=== JOIN RANDOM ROOM START ===");
        
        // Check if NetworkManager is available
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("NetworkManager not found!");
            return;
        }

        Debug.Log("NetworkManager found, checking connection...");

        // Check if connected
        if (!NetworkManager.Instance.IsConnected())
        {
            Debug.LogError("Not connected to server!");
            return;
        }

        Debug.Log("Connected to server, attempting auto-join...");

        // Try to join random room
        try
        {
            var room = await NetworkManager.Instance.AutoJoinRoom();
            if (room != null)
        {
                Debug.Log($"=== JOIN RANDOM ROOM SUCCESS ===");
                Debug.Log($"Joined room: {room.name} (ID: {room.id})!");
                StartGame();
            }
            else
            {
                Debug.LogError("=== JOIN RANDOM ROOM FAILED ===");
                Debug.LogError("Failed to join any room!");
                // Show error message to user (you can add UI for this)
                Debug.LogWarning("No rooms available. Try again or create a new room.");
                throw new System.Exception("No rooms available");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"=== JOIN RANDOM ROOM EXCEPTION ===");
            Debug.LogError($"Exception during auto-join: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            // Show error message to user (you can add UI for this)
            Debug.LogWarning($"Auto-join failed: {e.Message}. Try again.");
            throw; // Re-throw to ensure the finally block in OnPlayButtonClicked is executed
        }
    }

    private void StartGame()
    {
        isInGame = true;
        ShowGame();
        
        // Use UIManager singleton for consistent UI state management
        if (UIManager.Instance != null)
        {
            // Get player ID from PlayerManager first (most reliable)
            int playerId = -1;
            if (PlayerManager.Instance != null && PlayerManager.Instance.IsLocalPlayerIdSet())
        {
                playerId = PlayerManager.Instance.GetLocalPlayerId();
                Debug.Log($"Using PlayerManager local player ID: {playerId}");
            }
            // Fallback: try to find by name in game state
            else if (NetworkManager.Instance != null)
            {
                var gameState = NetworkManager.Instance.GetCurrentGameState();
                if (gameState != null && gameState.players != null)
                {
                    var player = gameState.players.FirstOrDefault(p => p.name == currentPlayerName);
                    if (player != null)
                    {
                        playerId = player.id;
                        Debug.Log($"Found player by name '{currentPlayerName}': {playerId}");
                    }
                    else
                    {
                        // Last resort: use first non-bot player
                        var firstPlayer = gameState.players.FirstOrDefault(p => !p.isBot && p.id > 0);
                        if (firstPlayer != null)
                        {
                            playerId = firstPlayer.id;
                            Debug.Log($"Using first non-bot player as local: {playerId}");
                        }
                    }
                }
        }

            if (playerId != -1)
            {
                UIManager.Instance.OnGameStart(playerId);
                Debug.Log($"Game started with player ID: {playerId}");
            }
            else
            {
                Debug.LogWarning("Could not determine player ID for game start!");
                // Still call OnGameStart with -1, let the UI handle it
                UIManager.Instance.OnGameStart(-1);
            }
        }
        else
        {
            Debug.LogError("UIManager.Instance is null! Please ensure UIManager is properly set up.");
        }
    }

    private void InitializeInputFields()
    {
        // Set default values for input fields
        if (playerNameInput != null)
        {
            // Check if player name is already saved, otherwise generate random name
            string savedName = PlayerPrefs.GetString("PlayerName", "");
            if (string.IsNullOrEmpty(savedName))
            {
                savedName = GenerateRandomName();
                PlayerPrefs.SetString("PlayerName", savedName);
                PlayerPrefs.Save();
            }
            playerNameInput.text = savedName;
            
            // Add listener to save name when changed
            playerNameInput.onEndEdit.AddListener(OnPlayerNameChanged);
        }

        if (roomCodeInput != null)
        {
            roomCodeInput.text = "";
            roomCodeInput.placeholder.GetComponent<TextMeshProUGUI>().text = "Room Code";
        }
    }

    private void OnPlayerNameChanged(string newName)
    {
        if (!string.IsNullOrEmpty(newName.Trim()))
        {
            PlayerPrefs.SetString("PlayerName", newName.Trim());
            PlayerPrefs.Save();
            Debug.Log($"Saved new player name: {newName.Trim()}");
        }
    }

    private string GenerateRandomName()
    {
        if (randomNames.Length == 0) return "Player";
        
        string baseName = randomNames[Random.Range(0, randomNames.Length)];
        int randomNumber = Random.Range(100, 999);
        return $"{baseName}{randomNumber}";
    }

    private void LoadAndDisplayBestScore()
    {
        if (bestScoreText != null)
        {
            int bestScore = PlayerPrefs.GetInt("AllTimeBestScore", 0);
            bestScoreText.text = $"{bestScore}";
        }
        else
        {
            Debug.LogWarning("bestScoreText is not assigned in TestRoomJoinButton");
        }
    }

    private string GetPlayerName()
    {
        return playerNameInput != null ? playerNameInput.text.Trim() : defaultPlayerName;
    }

    private string GetRoomCode()
    {
        return roomCodeInput != null ? roomCodeInput.text.Trim() : "";
    }

    private void ShowMenu()
    {
        if (menuPanel != null)
            menuPanel.SetActive(true);
        if (gamePanel != null)
            gamePanel.SetActive(false);
        
        // Refresh best score display when showing menu
        LoadAndDisplayBestScore();
    }

    private void ShowGame()
    {
        if (menuPanel != null)
            menuPanel.SetActive(false);
        if (gamePanel != null)
            gamePanel.SetActive(true);
    }

    private void SubscribeToGameEvents()
    {
        // Subscribe to any game events if needed
        // This can be expanded based on your game's event system
    }

    private void UnsubscribeFromGameEvents()
    {
        // Unsubscribe from any game events if needed
        // This can be expanded based on your game's event system
    }

    /// <summary>
    /// Update the main menu skin display with the current selected skin
    /// </summary>
    private void UpdateMainMenuSkinDisplay()
    {
        UpdateMainMenuSkinDisplayInternal();
    }

    /// <summary>
    /// Public method to update main menu skin display (can be called from other scripts)
    /// </summary>
    public void UpdateMainMenuSkinDisplayPublic()
    {
        UpdateMainMenuSkinDisplayInternal();
    }

    /// <summary>
    /// Internal method to update the main menu skin display with the current selected skin
    /// </summary>
    private void UpdateMainMenuSkinDisplayInternal()
    {
        if (SkinManager.Instance == null)
        {
            Debug.LogWarning("SkinManager not found!");
            return;
        }

        SnakeSkin currentSkin = SkinManager.Instance.GetCurrentSkin();
        if (currentSkin == null)
        {
            Debug.LogWarning("No current skin found!");
            return;
        }

        Debug.Log($"Updating main menu skin display with: {currentSkin.skinName}");

        // Update head sprite
        if (mainMenuHeadSprite != null)
        {
            if (currentSkin.headSprite != null)
            {
                mainMenuHeadSprite.sprite = currentSkin.headSprite;
                Debug.Log($"Updated main menu head sprite to: {currentSkin.headSprite.name}");
            }
            else if (currentSkin.bodySprite != null)
            {
                // Fallback to body sprite if head sprite is null
                mainMenuHeadSprite.sprite = currentSkin.bodySprite;
                Debug.Log($"Updated main menu head sprite to body sprite: {currentSkin.bodySprite.name}");
            }
            else
            {
                Debug.LogWarning($"No head or body sprite found for skin: {currentSkin.skinName}");
            }
        }
        else
        {
            Debug.LogWarning("mainMenuHeadSprite is not assigned!");
        }

        // Update body sprites
        if (mainMenuBodySprites != null && mainMenuBodySprites.Length > 0)
        {
            for (int i = 0; i < mainMenuBodySprites.Length; i++)
            {
                SpriteRenderer bodySprite = mainMenuBodySprites[i];
                if (bodySprite != null)
                {
                    if (currentSkin.bodySprite != null)
                    {
                        bodySprite.sprite = currentSkin.bodySprite;
                        Debug.Log($"Updated main menu body sprite {i} to: {currentSkin.bodySprite.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"No body sprite found for skin: {currentSkin.skinName}");
                    }
                }
                else
                {
                    Debug.LogWarning($"mainMenuBodySprites[{i}] is null!");
                }
            }
        }
        else
        {
            Debug.LogWarning("mainMenuBodySprites array is null or empty!");
        }
    }

    public void OnSnakeDeath()
    {
        if (showMenuOnDeath)
        {
            isInGame = false;
            ShowMenu();
        }
    }

    private void StartPulseAnimation()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }
        pulseCoroutine = StartCoroutine(PulseAnimation());
    }

    private void StopPulseAnimation()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
    }

    private IEnumerator PulseAnimation()
    {
        Vector3 originalScale = transform.localScale;
        while (isJoining)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
            transform.localScale = originalScale * pulse;
            yield return null;
        }
        transform.localScale = originalScale;
    }

    [ContextMenu("Play Game")]
    public void PlayGame()
        {
        OnPlayButtonClicked();
        }

    [ContextMenu("Show Menu")]
    public void ShowMenuPublic()
        {
        ShowMenu();
        }

    [ContextMenu("Show Game")]
    public void ShowGamePublic()
    {
        ShowGame();
    }

    public void OnSnakeDeathPublic()
    {
        OnSnakeDeath();
    }
} 