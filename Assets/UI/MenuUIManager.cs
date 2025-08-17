using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUIManager : MonoBehaviour
{
    public Button startButton;
    public Button quitButton;

    void Start()
    {
        startButton.onClick.AddListener(StartGame);
        quitButton.onClick.AddListener(QuitGame);
    }

    void StartGame()
    {
        SFXManager.Instance.PlaySFX("button_click");

        SceneManager.LoadScene(1);
    }

    void QuitGame()
    {
        SFXManager.Instance.PlaySFX("button_click");

        Application.Quit();
    }
}
