using System.Collections;
using UnityEngine;
using TMPro; // nếu dùng Text (Legacy) thay vì TextMeshPro, đổi sang using UnityEngine.UI; và đổi kiểu biến bên dưới thành Text

public class CoinDisplay : MonoBehaviour
{
    public TextMeshProUGUI coinText; // kéo Text (TMP) từ UI vào ô này trong Inspector
    public string format = "{0} x"; // đổi thành "Coin: {0}" hoặc "x{0}" tuỳ ý muốn hiển thị

    private bool isSubscribed = false;

    void Start()
    {
        // Dùng Start() thay vì OnEnable() vì Start chạy sau khi TẤT CẢ Awake() trong scene
        // đã chạy xong, đảm bảo CoinManager.Instance chắc chắn đã được gán.
        TrySubscribe();
    }

    void OnEnable()
    {
        // Trường hợp object bị tắt/bật lại sau khi Start() đã chạy 1 lần
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
        UpdateDisplay(CoinManager.Instance.currentCoin); // hiển thị đúng số ngay khi UI bật lên
    }

    // Phòng trường hợp CoinManager.Awake() chạy muộn hơn Start() của chính CoinDisplay
    // (ví dụ CoinManager nằm trên object bị disable lúc đầu rồi mới được bật ở script khác).
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
    }
}