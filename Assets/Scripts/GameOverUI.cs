using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Gắn script này vào GameObject "GameOverPanel" (panel chứa nút Play Again)
public class GameOverUI : MonoBehaviour
{
    [Header("Buttons")]
    public Button playAgainButton; // kéo thả Button "Play Again" vào đây trong Inspector

    void Awake()
    {
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(PlayAgain);
    }

    public void PlayAgain()
    {
        Time.timeScale = 1f; // reset time trước khi load lại scene, không thì scene mới cũng bị đứng
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}