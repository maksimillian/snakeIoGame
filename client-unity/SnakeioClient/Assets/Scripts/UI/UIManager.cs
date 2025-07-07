using UnityEngine;

public class UIManager : MonoBehaviour
{
    // Singleton pattern
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject menuPanel;
    public GameObject gameStatsPanel;
    public GameObject comprehensiveStatsPanel;
    public GameObject chooseSkinPanel;

    [Header("Components")]
    public TestRoomJoinButton menuScript;
    public GameStatsPanel gameStatsScript;
    public ComprehensiveStatsPanel comprehensiveStatsScript;
    public ChooseSkinPage chooseSkinScript;

    [Header("Game Start Protection")]
    private float gameStartTime = -1f;
    private readonly float GAME_START_PROTECTION_DURATION = 1f; // 1 second of protection

    private void Awake()
    {
        // Singleton pattern implementation
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
        // Ensure only menu is visible at start
        ShowMenu();
    }

    public void ShowMenu()
    {
        menuPanel.SetActive(true);
        gameStatsPanel.SetActive(false);
        comprehensiveStatsPanel.SetActive(false);
        chooseSkinPanel.SetActive(false);
    }

    public void ShowGameStats()
    {
        menuPanel.SetActive(false);
        gameStatsPanel.SetActive(true);
        comprehensiveStatsPanel.SetActive(false);
        chooseSkinPanel.SetActive(false);
    }

    public void ShowComprehensiveStats()
    {
        menuPanel.SetActive(false);
        gameStatsPanel.SetActive(false);
        comprehensiveStatsPanel.SetActive(true);
        chooseSkinPanel.SetActive(false);
    }

    // Called when player dies - transition from game stats to comprehensive stats
    public void OnPlayerDeath(int finalScore, int finalKills, int finalPosition)
    {
        // Check if we're in the game start protection period
        if (gameStartTime > 0 && (Time.time - gameStartTime) < GAME_START_PROTECTION_DURATION)
        {
            float remainingProtection = GAME_START_PROTECTION_DURATION - (Time.time - gameStartTime);
            Debug.Log($"Ignoring death event during game start protection period. {remainingProtection:F1} seconds remaining.");
            return;
        }
        
        Debug.Log($"UIManager.OnPlayerDeath called - Score: {finalScore}, Kills: {finalKills}, Position: {finalPosition}");
        
        // Pass session stats to comprehensive stats panel
        if (comprehensiveStatsScript != null)
        {
            Debug.Log("Setting session stats in comprehensive stats panel");
            comprehensiveStatsScript.SetSessionStats(finalScore, finalKills, finalPosition);
        }
        else
        {
            Debug.LogError("comprehensiveStatsScript is null - cannot set session stats!");
        }
        
        Debug.Log("Showing comprehensive stats panel");
        ShowComprehensiveStats();
    }

    // Called when game starts successfully
    public void OnGameStart(int playerId)
    {
        // Set the game start time for protection
        gameStartTime = Time.time;
        Debug.Log($"Game started at {gameStartTime} - protection active for {GAME_START_PROTECTION_DURATION} seconds");
        
        // Set the player ID for the game stats panel
        if (gameStatsScript != null)
        {
            gameStatsScript.SetCurrentPlayerId(playerId);
        }
        
        ShowGameStats();
    }
    
    // Called from comprehensive stats continue button
    public void OnContinueButtonClicked()
    {
        ShowMenu();
    }

    public void ShowChooseSkinPanel()
    {
        menuPanel.SetActive(false);
        gameStatsPanel.SetActive(false);
        comprehensiveStatsPanel.SetActive(false);
        chooseSkinPanel.SetActive(true);
        
        // Open the skin selection page
        if (chooseSkinScript != null)
        {
            chooseSkinScript.OpenPage();
        }
    }
    
    // Get the game start time for protection checks
    public float GetGameStartTime()
    {
        return gameStartTime;
    }
} 