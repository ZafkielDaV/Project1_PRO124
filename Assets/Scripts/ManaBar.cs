using UnityEngine;
using UnityEngine.UI;

public class ManaBar : MonoBehaviour
{
    [Header("References")]
    public Image fillImage;

    [Header("Smooth Fill Effect")]
    public bool smoothFill = true;
    public float fillLerpSpeed = 5f; // tốc độ thanh mana chạy mượt tới giá trị thật

    [Header("Color")]
    public Color manaColor = new Color(0.2f, 0.5f, 1f); // xanh dương mặc định
    public bool useLowManaColor = false;
    public Color lowManaColor = new Color(0.4f, 0.1f, 0.6f); // tím khi mana thấp (tùy chọn)
    [Range(0f, 1f)] public float lowManaThreshold = 0.25f;

    private float maxMana;
    private float targetFillAmount = 1f;

    void Update()
    {
        if (smoothFill && fillImage != null)
        {
            if (Mathf.Abs(fillImage.fillAmount - targetFillAmount) > 0.001f)
            {
                fillImage.fillAmount = Mathf.Lerp(
                    fillImage.fillAmount, targetFillAmount, Time.deltaTime * fillLerpSpeed);
            }
            else
            {
                fillImage.fillAmount = targetFillAmount;
            }
        }
    }

    public void SetMaxHealth(float maxMana)
    {
        // Giữ tên SetMaxHealth để tương thích với Player.cs (manaBar.SetMaxHealth(maxMana))
        this.maxMana = maxMana;
        targetFillAmount = 1f;

        if (fillImage != null)
            fillImage.fillAmount = 1f; // set thẳng, không lerp lúc khởi tạo

        UpdateColor(1f);
    }

    public void SetHealth(float mana)
    {
        // Giữ tên SetHealth để tương thích với Player.cs (manaBar.SetHealth(currentMana))
        if (maxMana <= 0f) return; // tránh NaN

        float percent = Mathf.Clamp01(mana / maxMana);
        targetFillAmount = percent;

        if (!smoothFill && fillImage != null)
            fillImage.fillAmount = percent;

        UpdateColor(percent);
    }

    private void UpdateColor(float percent)
    {
        if (fillImage == null) return;

        if (useLowManaColor && percent <= lowManaThreshold)
        {
            float t = percent / lowManaThreshold;
            fillImage.color = Color.Lerp(lowManaColor, manaColor, t);
        }
        else
        {
            fillImage.color = manaColor;
        }
    }
}