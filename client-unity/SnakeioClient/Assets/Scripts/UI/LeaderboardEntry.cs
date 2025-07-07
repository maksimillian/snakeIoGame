using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeaderboardEntry : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI positionText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scoreText;

    [Header("Colors")]
    public Color playerTextColor = Color.yellow;
    public Color normalTextColor = Color.white;
    public Color deadTextColor = Color.gray;

    public void SetData(int position, string playerName, int score, bool isPlayer, bool isAlive = true)
    {
        if (positionText != null)
            positionText.text = position.ToString();

        if (nameText != null)
        {
            nameText.text = string.IsNullOrEmpty(playerName) ? "---" : playerName;
            
            // Set color based on status
            if (!isAlive)
            {
                nameText.color = deadTextColor;
            }
            else if (isPlayer)
            {
                nameText.color = playerTextColor;
            }
            else
            {
                nameText.color = normalTextColor;
            }
        }

        if (scoreText != null)
        {
            scoreText.text = score.ToString();
            
            // Set color based on status
            if (!isAlive)
            {
                scoreText.color = deadTextColor;
            }
            else if (isPlayer)
            {
                scoreText.color = playerTextColor;
            }
            else
            {
                scoreText.color = normalTextColor;
            }
        }
    }

    public void Clear()
    {
        SetData(0, "", 0, false, true);
    }
} 