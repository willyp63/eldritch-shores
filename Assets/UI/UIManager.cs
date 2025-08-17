using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    public TextMeshProUGUI scoreText;

    public TextMeshProUGUI livesText;

    public Button pauseToggleButton;
    public Button resumeButton;
    public Button mainMenuButton;
    public Button otherMainMenuButton;
    public Button restartButton;
    public TextMeshProUGUI gameOverText;
    public GameObject gameOverPanel;
    public GameObject pauseMenuPanel;

    protected override void Awake()
    {
        base.Awake();

        GameManager.Instance.OnScoreChanged += UpdateScoreText;
        GameManager.Instance.OnLivesChanged += UpdateLivesText;
        GameManager.Instance.OnPauseStateChanged += TogglePauseMenu;
        GameManager.Instance.OnGameOver += ShowGameOver;

        pauseToggleButton.onClick.AddListener(TogglePause);
        resumeButton.onClick.AddListener(ResumeGame);
        mainMenuButton.onClick.AddListener(LoadMainMenu);
        otherMainMenuButton.onClick.AddListener(LoadMainMenu);
        restartButton.onClick.AddListener(RestartGame);

        pauseMenuPanel.SetActive(false);
        gameOverPanel.SetActive(false);
    }

    void ShowGameOver()
    {
        StartCoroutine(PauseGameAfterDelay(0.5f));

        gameOverPanel.SetActive(true);
        gameOverText.text =
            $"YOU SCORED\n<color=#{ColorUtility.ToHtmlStringRGBA(FloatingTextManager.pointsColor)}>{GameManager.Instance.CurrentScore:N0} PTS</color>";

        pauseToggleButton.gameObject.SetActive(false);
        scoreText.gameObject.SetActive(false);
        livesText.gameObject.SetActive(false);
    }

    IEnumerator PauseGameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        GameManager.Instance.PauseGame();
    }

    void RestartGame()
    {
        SFXManager.Instance.PlaySFX("button_click");

        GameManager.Instance.ResetGame();
    }

    void LoadMainMenu()
    {
        SFXManager.Instance.PlaySFX("button_click");

        SceneManager.LoadScene(0);
    }

    void TogglePause()
    {
        if (GameManager.Instance.IsPaused())
        {
            GameManager.Instance.ResumeGame();
        }
        else
        {
            GameManager.Instance.PauseGame();
        }
    }

    void ResumeGame()
    {
        SFXManager.Instance.PlaySFX("button_click");

        GameManager.Instance.ResumeGame();
    }

    void UpdateScoreText(int score)
    {
        if (scoreText == null)
            return;

        scoreText.text = $"{score:N0} PTS";
    }

    void UpdateLivesText(int lives)
    {
        if (livesText == null)
            return;

        livesText.text = string.Join("  ", Enumerable.Repeat("<3", lives));
    }

    void TogglePauseMenu(bool isPaused)
    {
        SFXManager.Instance.PlaySFX("button_click");

        if (pauseMenuPanel == null)
            return;

        pauseMenuPanel.SetActive(isPaused);

        // Update pause button text
        if (pauseToggleButton != null)
        {
            TextMeshProUGUI buttonText =
                pauseToggleButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = isPaused ? "Resume" : "Pause";
            }
        }
    }
}
