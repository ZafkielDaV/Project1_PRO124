using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameOverUI : MonoBehaviour
{
    [Header("Buttons")]
    public Button playAgainButton;
    public Button mainMenuButton;

    [Header("Main Menu")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;      // nếu để trống sẽ tự GetComponent/AddComponent
    [SerializeField] private AudioClip clickSfxClip;     // tiếng click chung cho cả 2 nút
    [SerializeField][Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField] private float sfxDelayBeforeLoad = 0.15f; // đợi tiếng click kêu xong rồi mới chuyển scene

    void Awake()
    {
        SetupAudioSource();

        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(PlayAgain);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    public void PlayAgain()
    {
        Debug.Log("[GameOverUI] PlayAgain() called");
        PlayClickSfx();
        StartCoroutine(PlayAgainAfterSfx());
    }

    private IEnumerator PlayAgainAfterSfx()
    {
        // WaitForSecondsRealtime vì GameOver thường bị Time.timeScale = 0 (freeze)
        yield return new WaitForSecondsRealtime(sfxDelayBeforeLoad);

        Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.ReloadSceneWithSavePoint(); // tự bật useSavePoint = true
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        PlayClickSfx();
        StartCoroutine(GoToMainMenuAfterSfx());
    }

    private IEnumerator GoToMainMenuAfterSfx()
    {
        yield return new WaitForSecondsRealtime(sfxDelayBeforeLoad);

        Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.LoadScene(mainMenuSceneName, loadFromSavePoint: false); // tự bật useSavePoint = false
        else
            SceneManager.LoadScene(mainMenuSceneName);
    }

    // ---------------- SFX ----------------

    private void SetupAudioSource()
    {
        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f; // âm thanh UI luôn nghe rõ 2D, không phụ thuộc vị trí
    }

    private void PlayClickSfx()
    {
        if (sfxSource == null || clickSfxClip == null) return;
        sfxSource.PlayOneShot(clickSfxClip, sfxVolume);
    }
}