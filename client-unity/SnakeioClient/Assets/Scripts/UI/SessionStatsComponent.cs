using UnityEngine;
using TMPro;

public class SessionStatsComponent : MonoBehaviour
{
    [Header("Session Stats (This Game)")]
    public TextMeshProUGUI sessionScoreText;
    public TextMeshProUGUI sessionKillsText;

    public void UpdateStats(int score, int kills)
    {
        if (sessionScoreText != null)
            sessionScoreText.text = $"{score}";
        
        if (sessionKillsText != null)
            sessionKillsText.text = $"{kills}";
    }

    public void ClearStats()
    {
        UpdateStats(0, 0);
    }
} 