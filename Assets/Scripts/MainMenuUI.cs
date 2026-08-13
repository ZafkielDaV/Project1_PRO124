using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Gắn script này vào GameObject chứa UI của Main Menu
public class MainMenuUI : MonoBehaviour
{
    [Header("Buttons")]
    public Button newGameButton;   // kéo thả Button "New Game" vào đây
    public Button continueButton;  // (tùy chọn) kéo thả Button "Continue" vào đây

    [Header("Gameplay Scene")]
    [SerializeField] private string gameplaySceneName = "Map1"; // đổi thành tên scene chơi chính của bạn

    void Awake()
    {
        if (newGameButton != null)
            newGameButton.onClick.AddListener(NewGame);

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(Continue);
            // Ẩn nút Continue nếu chưa có save data nào
            continueButton.gameObject.SetActive(PlayerPrefs.GetInt("HasSaveData", 0) == 1);
        }
    }

    // ---- Bắt đầu chơi mới: xóa save cũ, vào scene ở vị trí spawn gốc ----
    public void NewGame()
    {
        Time.timeScale = 1f;

        // Xóa sạch save data để đảm bảo không load nhầm vị trí SavePoint cũ
        SavePoint.ClearSaveData();

        if (GameManager.Instance != null)
            GameManager.Instance.LoadScene(gameplaySceneName, loadFromSavePoint: false);
        else
            SceneManager.LoadScene(gameplaySceneName);
    }

    // ---- (Tùy chọn) Chơi tiếp từ save gần nhất ----
    public void Continue()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.LoadScene(gameplaySceneName, loadFromSavePoint: true);
        else
            SceneManager.LoadScene(gameplaySceneName);
    }
}