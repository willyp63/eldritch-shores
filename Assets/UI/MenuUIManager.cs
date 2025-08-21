using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUIManager : MonoBehaviour
{
    public Button startButton;

    public GameObject highScorePanel;
    public TextMeshProUGUI highScoreText;

    void Start()
    {
        startButton.onClick.AddListener(StartGame);

        FetchHighScores();
    }

    void StartGame()
    {
        SFXManager.Instance.PlaySFX("button_click");

        SceneManager.LoadScene(1);
    }

    void FetchHighScores()
    {
        highScorePanel.SetActive(false);
        AnalyticsManager.Instance.FetchHighScores(
            "arcade",
            (highScores) =>
            {
                highScoreText.text = string.Join(
                    "\n",
                    highScores.Take(10).Select(hs => $"{hs.player_name}: {hs.score}")
                );
                highScorePanel.SetActive(true);
            },
            (error) =>
            {
                Debug.LogError($"Error fetching high scores: {error}");
                highScorePanel.SetActive(false);
            }
        );
    }
}
