using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance;

    public int currentCoin = 0;

    // UI hoặc bất kỳ script nào khác có thể đăng ký lắng nghe sự kiện này để tự cập nhật
    // khi số coin thay đổi, thay vì phải Update() kiểm tra liên tục mỗi frame.
    public event System.Action<int> OnCoinChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // giữ số coin khi qua scene khác, bỏ dòng này nếu không cần
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddCoin(int amount)
    {
        currentCoin += amount;
        OnCoinChanged?.Invoke(currentCoin);
    }

    public bool SpendCoin(int amount)
    {
        if (currentCoin < amount) return false;

        currentCoin -= amount;
        OnCoinChanged?.Invoke(currentCoin);
        return true;
    }
}