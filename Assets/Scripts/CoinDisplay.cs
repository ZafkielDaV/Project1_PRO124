using System.Collections;
using UnityEngine;
using TMPro;

public class CoinDisplay : MonoBehaviour
{
    public TextMeshProUGUI coinText;
    public string format = "{0} x";

    [Header("Sound")]
    public AudioSource sfxSource; // kéo AudioSource vào đây (có thể để chung với PlayerSFX hoặc riêng)
    public AudioClip coinSound;

    private bool isSubscribed = false;
    private bool isFirstUpdate = true; // THÊM DÒNG NÀY — để bỏ qua lần đầu (không phát âm thanh lúc khởi tạo)

    void Start()
    {
        TrySubscribe();
    }

    void OnEnable()
    {
        if (!isSubscribed)
            TrySubscribe();
    }

    void OnDisable()
    {
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.OnCoinChanged -= UpdateDisplay;
        }
        isSubscribed = false;
    }

    private void TrySubscribe()
    {
        if (CoinManager.Instance == null)
        {
            Debug.LogWarning("[CoinDisplay] Không tìm thấy CoinManager.Instance. Kiểm tra: có GameObject nào gắn script CoinManager trong scene không, và nó có đang active không.");
            StartCoroutine(RetrySubscribeNextFrame());
            return;
        }

        CoinManager.Instance.OnCoinChanged += UpdateDisplay;
        isSubscribed = true;
        UpdateDisplay(CoinManager.Instance.currentCoin); // hiển thị đúng số ngay khi UI bật lên (không phát âm thanh)
    }

    private IEnumerator RetrySubscribeNextFrame()
    {
        yield return null;
        if (!isSubscribed)
            TrySubscribe();
    }

    private void UpdateDisplay(int newAmount)
    {
        if (coinText != null)
            coinText.text = string.Format(format, newAmount);
        else
            Debug.LogWarning("[CoinDisplay] Chưa gán coinText trong Inspector.");

        // THÊM ĐOẠN NÀY — phát âm thanh, bỏ qua lần đầu
        if (isFirstUpdate)
        {
            isFirstUpdate = false;
        }
        else
        {
            PlayCoinSound();
        }
    }

    private void PlayCoinSound() // THÊM HÀM NÀY
    {
        if (sfxSource != null && coinSound != null)
            sfxSource.PlayOneShot(coinSound);
    }
}