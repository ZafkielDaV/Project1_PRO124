using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DashCooldownIcon : MonoBehaviour
{
    [Header("References")]
    public Player player;              // kéo GameObject Player vào đây
    public Image cooldownOverlay;      // Image kiểu Filled (Radial 360), phủ lên icon, thể hiện thời gian còn lại
    public TMP_Text cooldownText;      // (tùy chọn) hiển thị số giây còn lại, để trống nếu không cần
    public Image iconImage;            // (tùy chọn) icon chính, dùng để làm tối khi chưa sẵn sàng

    [Header("Visuals")]
    public bool dimIconWhenNotReady = true;
    [Range(0f, 1f)] public float dimAlpha = 0.4f;
    public bool hideOverlayWhenReady = true;

    void Update()
    {
        if (player == null) return;

        float remaining = player.DashCooldownRemaining;
        float percent = player.DashCooldownPercent01; // 0 = vừa dùng xong, 1 = đã sẵn sàng

        // Overlay hiển thị phần THỜI GIAN CÒN LẠI -> fillAmount = phần trăm CÒN CHỜ (1 - percent)
        if (cooldownOverlay != null)
        {
            float overlayFill = 1f - percent;
            cooldownOverlay.fillAmount = overlayFill;

            if (hideOverlayWhenReady)
                cooldownOverlay.enabled = overlayFill > 0.001f;
        }

        // Text đếm ngược số giây còn lại
        if (cooldownText != null)
        {
            if (remaining > 0.05f)
                cooldownText.text = remaining.ToString("F1");
            else
                cooldownText.text = "";
        }

        // Làm tối icon khi kỹ năng chưa sẵn sàng (hết cooldown NHƯNG thiếu mana vẫn tính là chưa sẵn sàng)
        if (dimIconWhenNotReady && iconImage != null)
        {
            bool ready = player.IsDashReady;
            Color c = iconImage.color;
            c.a = ready ? 1f : dimAlpha;
            iconImage.color = c;
        }
    }
}