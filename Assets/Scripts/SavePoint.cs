using UnityEngine;
using System.Collections;

public class SavePoint : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject pressPromptUI;
    [SerializeField] private KeyCode saveKey = KeyCode.Y;
    [SerializeField] private GameObject visualObject;     // object con chứa SpriteRenderer + Animator
    [SerializeField] private Animator saveAnimator;       // Animator nằm trên visualObject
    [SerializeField] private string saveAnimTrigger = "Save"; // tên Trigger trong Animator Controller

    [Header("Effects (tùy chọn)")]
    [SerializeField] private ParticleSystem saveEffect;
    [SerializeField] private AudioSource saveSound;

    private bool playerInRange = false;
    private Transform playerTransform;
    private Coroutine visualRoutine; // để tránh chạy chồng nếu bấm Y liên tục

    private void Start()
    {
        if (pressPromptUI != null)
            pressPromptUI.SetActive(false);

        if (visualObject != null)
            visualObject.SetActive(false); // ẩn sẵn từ đầu, chỉ hiện khi save
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerTransform = other.transform;

            if (pressPromptUI != null)
                pressPromptUI.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            playerTransform = null;

            if (pressPromptUI != null)
                pressPromptUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(saveKey))
        {
            SaveGame();
        }
    }

    private void SaveGame()
    {
        if (playerTransform == null) return;

        // --- Lưu vị trí ---
        PlayerPrefs.SetFloat("PlayerPosX", playerTransform.position.x);
        PlayerPrefs.SetFloat("PlayerPosY", playerTransform.position.y);
        PlayerPrefs.SetString("LastSavePointID", gameObject.name);
        PlayerPrefs.SetInt("HasSaveData", 1);
        PlayerPrefs.Save();

        Debug.Log($"Game Saved at {gameObject.name} - Position: {playerTransform.position}");


        // --- Bật Visual, chạy animation, rồi tự tắt khi xong ---
        if (visualRoutine != null)
            StopCoroutine(visualRoutine);

        visualRoutine = StartCoroutine(PlayVisualThenHide());

        // --- Hiệu ứng / âm thanh ---
        if (saveEffect != null)
            saveEffect.Play();

        if (saveSound != null)
            saveSound.Play();
    }

    private IEnumerator PlayVisualThenHide()
    {
        if (visualObject == null) yield break;

        // Bật lại từ đầu để animation replay đúng
        visualObject.SetActive(false);
        yield return null; // chờ 1 frame để Unity nhận việc tắt/bật
        visualObject.SetActive(true);

        if (saveAnimator != null && !string.IsNullOrEmpty(saveAnimTrigger))
            saveAnimator.SetTrigger(saveAnimTrigger);

        // Chờ 1 frame để Animator chuyển sang state "Save" trước khi đọc length
        yield return null;

        float clipLength = 0.5f; // fallback mặc định nếu không lấy được info
        if (saveAnimator != null)
        {
            AnimatorStateInfo state = saveAnimator.GetCurrentAnimatorStateInfo(0);
            clipLength = state.length;
        }

        yield return new WaitForSeconds(clipLength);

        // Ẩn lại object sau khi animation chạy xong
        visualObject.SetActive(false);
    }

    // ============================================
    // Player.cs gọi hàm này trong Start() - CHỈ khi GameManager.ShouldUseSavePoint() == true
    public static void LoadLastPosition(Transform player)
    {
        if (PlayerPrefs.GetInt("HasSaveData", 0) == 1)
        {
            float x = PlayerPrefs.GetFloat("PlayerPosX");
            float y = PlayerPrefs.GetFloat("PlayerPosY");

            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.position = new Vector2(x, y);
            }

            player.position = new Vector2(x, y);
        }
    }

    // ---- MỚI: GameManager.ReloadSceneFresh() gọi hàm này để xóa sạch save data ----
    // Dùng khi người chơi bấm "Restart" trên Game Over UI -> chơi lại từ đầu, không dùng save point cũ
    public static void ClearSaveData()
    {
        PlayerPrefs.DeleteKey("PlayerPosX");
        PlayerPrefs.DeleteKey("PlayerPosY");
        PlayerPrefs.DeleteKey("LastSavePointID");
        PlayerPrefs.DeleteKey("HasSaveData");
        PlayerPrefs.Save();

        Debug.Log("Save data cleared.");
    }
}