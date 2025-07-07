using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class ComprehensiveStatsPanel : MonoBehaviour
{
    [Header("Panel Structure")]
    public GameObject mainPanel;
    public Button continueButton;

    [Header("Session Stats Component")]
    public SessionStatsComponent sessionStats;

    [Header("All Time Stats Component")]
    public AllTimeStatsComponent allTimeStats;

    [Header("Settings")]
    public float updateInterval = 0.5f;

    private int currentPlayerId = -1;
    private float lastUpdateTime;

    // Session stats (current game)
    private int sessionScore = 0;
    private int sessionKills = 0;

    // All time stats (from server)
    private int allTimeBestScore = 0;
    private int allTimeTotalKills = 0;
    private int allTimeGamesPlayed = 0;

    private void Start()
    {
        // Subscribe to game events
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnGameStateUpdated += OnGameStateUpdated;
        }

        // Initialize UI
        LoadAllTimeStats();
        UpdatePanelVisibility(false);

        // Set up continue button
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnGameStateUpdated -= OnGameStateUpdated;
        }

        // Remove button listener
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinueButtonClicked);
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

    private void LoadAllTimeStats()
    {
        // TODO: Load from server API
        // For now, load from PlayerPrefs as placeholder
        allTimeBestScore = PlayerPrefs.GetInt("AllTimeBestScore", 0);
        allTimeTotalKills = PlayerPrefs.GetInt("AllTimeTotalKills", 0);
        allTimeGamesPlayed = PlayerPrefs.GetInt("AllTimeGamesPlayed", 0);
        
        Debug.Log($"ComprehensiveStatsPanel.LoadAllTimeStats - Loaded: BestScore={allTimeBestScore}, TotalKills={allTimeTotalKills}, GamesPlayed={allTimeGamesPlayed}");
    }

    private void SaveAllTimeStats()
    {
        // TODO: Save to server API
        // For now, save to PlayerPrefs as placeholder
        PlayerPrefs.SetInt("AllTimeBestScore", allTimeBestScore);
        PlayerPrefs.SetInt("AllTimeTotalKills", allTimeTotalKills);
        PlayerPrefs.SetInt("AllTimeGamesPlayed", allTimeGamesPlayed);
        PlayerPrefs.Save();
        
        Debug.Log($"ComprehensiveStatsPanel.SaveAllTimeStats - Saved: BestScore={allTimeBestScore}, TotalKills={allTimeTotalKills}, GamesPlayed={allTimeGamesPlayed}");
    }

    private void OnGameStateUpdated(NetworkManager.GameState gameState)
    {
        if (gameState == null || gameState.players == null)
            return;

        // Find current player and update session stats
        var currentPlayer = gameState.players.FirstOrDefault(p => p.id == currentPlayerId);
        if (currentPlayer != null)
        {
            sessionScore = currentPlayer.score;
            sessionKills = currentPlayer.kills;
        }
    }

    public void SetSessionStats(int score, int kills, int topPosition)
    {
        Debug.Log($"ComprehensiveStatsPanel.SetSessionStats - Score: {score}, Kills: {kills}");
        
        sessionScore = score;
        sessionKills = kills;
        
        // Update all-time stats if this session was better
        if (score > allTimeBestScore)
        {
            allTimeBestScore = score;
            Debug.Log($"ComprehensiveStatsPanel: New best score: {allTimeBestScore}");
        }
        
        allTimeTotalKills += kills;
        allTimeGamesPlayed++;
        
        SaveAllTimeStats();
        
        // Force show the panel
        UpdatePanelVisibility(true);
        
        // Force update the stats immediately
        UpdateStats();
    }

    private void UpdateStats()
    {
        // Update session stats component
        if (sessionStats != null)
        {
            sessionStats.UpdateStats(sessionScore, sessionKills);
        }

        // Update all-time stats component
        if (allTimeStats != null)
        {
            allTimeStats.UpdateStats(allTimeBestScore, allTimeTotalKills);
        }
    }

    private void UpdatePanelVisibility(bool show)
    {
        if (mainPanel != null)
            mainPanel.SetActive(show);
    }

    private void OnContinueButtonClicked()
    {
        // Use UIManager singleton for consistent UI state management
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnContinueButtonClicked();
        }
        else
        {
            Debug.LogError("UIManager.Instance is null! Please ensure UIManager is properly set up.");
        }
    }

    public void SetUpdateInterval(float interval)
    {
        updateInterval = interval;
    }

    public void ForceShowPanel()
    {
        UpdatePanelVisibility(true);
    }

    public void ForceHidePanel()
    {
        UpdatePanelVisibility(false);
    }

    public void UpdateSessionScore(int score)
    {
        sessionScore = score;
    }

    public void UpdateSessionKills(int kills)
    {
        sessionKills = kills;
    }
} 