using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public int movesLeft = 30; // Starting number of moves
    public int currentScore = 0;
    public int scoreToWin = 300; // Initial score needed to win the game
    public TMP_Text scoreText; // Assign in the inspector
    public TMP_Text limitText; // Assign in the inspector
    public TMP_Text lvlText;

    public GameObject winPanel; // Assign in the inspector
    public GameObject losePanel; // Assign in the inspector

    private int currentLevel = 1; // Starting level

    private void Start()
    {
        currentLevel = PlayerPrefs.GetInt("currentLevel", 1); // Load the current level or default to 1
        scoreToWin = PlayerPrefs.GetInt("scoreToWin", 300); // Load scoreToWin or default to 300
        UpdateUI();
        winPanel.SetActive(false);
        losePanel.SetActive(false);
    }

    public void UpdateUI()
    {
        limitText.text = movesLeft.ToString();
        scoreText.text = "SCORE: " + currentScore.ToString() + "/" + scoreToWin;
        lvlText.text = "Level " + currentLevel.ToString();
    }

    public void AddScore(int scoreToAdd)
    {
        currentScore += scoreToAdd;
        UpdateUI();
        CheckWinCondition();
    }

    public void UseMove()
    {
        if (movesLeft > 0)
        {
            movesLeft--;
            UpdateUI();

            if (movesLeft <= 0)
            {
                CheckLoseCondition();
            }
        }
    }

    private void CheckWinCondition()
    {
        if (currentScore >= scoreToWin)
        {
            winPanel.SetActive(true);
        }
    }

    private void CheckLoseCondition()
    {
        if (currentScore < scoreToWin)
        {
            losePanel.SetActive(true);
        }
    }

    public void NextLevel()
    {
        currentLevel++;
        PlayerPrefs.SetInt("currentLevel", currentLevel);

        // Increment scoreToWin for the next level and save it
        scoreToWin += 30; // Increase scoreToWin by 100 points for each new level
        PlayerPrefs.SetInt("scoreToWin", scoreToWin); // Save the new scoreToWin
        PlayerPrefs.Save();

        string nextLevelSceneName = "Level" + currentLevel;
        SceneManager.LoadScene(nextLevelSceneName);
    }

    public void RestartLevel()
    {
        // Reset the current score and moves
        currentScore = 0;
        movesLeft = 30;
        UpdateUI();

        // Reload the current level without changing the scoreToWin
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
