using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("Buttons")]
    public Button playAgainButton;
    public Button mainMenuButton;

    [Header("Main Menu")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    void Awake()
    {
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(PlayAgain);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    public void PlayAgain()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.ReloadSceneWithSavePoint(); // tự bật useSavePoint = true
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.LoadScene(mainMenuSceneName, loadFromSavePoint: false); // tự bật useSavePoint = false
        else
            SceneManager.LoadScene(mainMenuSceneName);
    }
}