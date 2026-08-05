using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("References")]
    public Image fillImage;
    public Image damageFillImage; // thanh phụ hiển thị lượng máu vừa mất (kéo trễ)
    public Text damagePercentText; // (tùy chọn) hiển thị số % máu vừa mất

    [Header("Damage Delay Effect")]
    public float damageDelay = 0.5f;   // thời gian chờ trước khi thanh phụ bắt đầu chạy xuống
    public float damageLerpSpeed = 3f; // tốc độ thanh phụ đuổi theo thanh chính

    [Header("Color by Health %")]
    public Color fullHealthColor = Color.green;
    public Color midHealthColor = Color.yellow;
    public Color lowHealthColor = Color.red;
    [Range(0f, 1f)] public float midThreshold = 0.6f;
    [Range(0f, 1f)] public float lowThreshold = 0.3f;

    private float maxHealth;
    private float targetFillAmount = 1f;
    private float damageTimer = 0f;

    void Update()
    {
        // Xử lý thanh phụ (damage bar) chạy trễ
        if (damageFillImage != null)
        {
            if (damageTimer > 0f)
            {
                damageTimer -= Time.deltaTime;
            }
            else if (damageFillImage.fillAmount > targetFillAmount)
            {
                damageFillImage.fillAmount = Mathf.Lerp(
                    damageFillImage.fillAmount, targetFillAmount, Time.deltaTime * damageLerpSpeed);

                if (Mathf.Abs(damageFillImage.fillAmount - targetFillAmount) < 0.001f)
                {
                    damageFillImage.fillAmount = targetFillAmount;
                    if (damagePercentText != null)
                        damagePercentText.text = "";
                }
            }
        }
    }

    public void SetMaxHealth(float maxHealth)
    {
        this.maxHealth = maxHealth;
        targetFillAmount = 1f;
        fillImage.fillAmount = 1f; // set thẳng, không lerp
        if (damageFillImage != null) damageFillImage.fillAmount = 1f;
        UpdateColor(1f);
    }

    public void SetHealth(float health)
    {
        if (maxHealth <= 0f) return; // tránh NaN

        float percent = Mathf.Clamp01(health / maxHealth);
        targetFillAmount = percent;
        fillImage.fillAmount = percent; // set thẳng, không lerp
        UpdateColor(percent);

        // reset damage timer để thanh phụ bắt đầu đếm ngược trước khi đuổi theo
        damageTimer = damageDelay;
    }

    private void UpdateColor(float percent)
    {
        if (percent <= lowThreshold)
        {
            float t = percent / lowThreshold;
            fillImage.color = Color.Lerp(lowHealthColor, midHealthColor, t);
        }
        else if (percent <= midThreshold)
        {
            float t = (percent - lowThreshold) / (midThreshold - lowThreshold);
            fillImage.color = Color.Lerp(midHealthColor, fullHealthColor, t);
        }
        else
        {
            fillImage.color = fullHealthColor;
        }
    }
}