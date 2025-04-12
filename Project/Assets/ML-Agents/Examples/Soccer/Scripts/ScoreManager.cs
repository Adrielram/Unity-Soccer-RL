using UnityEngine;
using TMPro;
public class ScoreManager : MonoBehaviour
{

    public TextMeshProUGUI scoreText;

    public int blueScore = 0;
    public int purpleScore = 0;

    public void GoalScored(Team scoringTeam)
    {
        if (scoringTeam == Team.Blue)
        {
            blueScore++;
        }
        else
        {
            purpleScore++;
        }

        UpdateScoreText(); // Actualizar UI
        Debug.Log($"🏆 Score - Blue: {blueScore} | Purple: {purpleScore}");
    }

    void UpdateScoreText()
    {
        scoreText.text = $"Blue: {blueScore} - Purple: {purpleScore}";
    }

    public void ResetScore()
    {
        blueScore = 0;
        purpleScore = 0;
    }
}
