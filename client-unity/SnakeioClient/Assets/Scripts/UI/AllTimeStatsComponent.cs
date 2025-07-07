using UnityEngine;
using TMPro;

public class AllTimeStatsComponent : MonoBehaviour
{
    [Header("All Time Stats")]
    public TextMeshProUGUI allTimeScoreText;
    public TextMeshProUGUI allTimeKillsText;

    public void UpdateStats(int bestScore, int totalKills)
    {
        if (allTimeScoreText != null)
            allTimeScoreText.text = $"{bestScore}";
        
        if (allTimeKillsText != null)
            allTimeKillsText.text = $"{totalKills}";
    }

    public void ClearStats()
    {
        UpdateStats(0, 0);
    }
} 