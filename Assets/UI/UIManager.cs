using System.Linq;
using TMPro;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    public Image energyBar;

    public TextMeshProUGUI scoreText;

    public TextMeshProUGUI livesText;

    protected override void Awake()
    {
        base.Awake();

        GameManager.Instance.OnEnergyChanged += UpdateEnergyBar;
        GameManager.Instance.OnScoreChanged += UpdateScoreText;
        GameManager.Instance.OnLivesChanged += UpdateLivesText;
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

    void UpdateEnergyBar(float energy)
    {
        if (energyBar == null)
            return;

        energyBar.fillAmount = energy / GameManager.Instance.maxEnergy;
    }
}
