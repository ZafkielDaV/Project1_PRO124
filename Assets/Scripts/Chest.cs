using System.Collections;
using UnityEngine;

public class ChestController : MonoBehaviour
{
    public LootTable lootTable;
    public int maxHealth = 1;      // số hit để vỡ (1 = vỡ ngay)
    private int currentHealth;

    public Animator animator;      // optional: animation mở chest
    public SpriteRenderer sprite;
    public Sprite openedSprite;

    private bool isOpened = false;

    [Header("Burst Settings")]
    public float burstForceMin = 0.5f;
    public float burstForceMax = 1.5f;
    public float upwardForce = 2f;
    [Range(0f, 180f)]
    public float spreadAngle = 60f; // độ rộng góc bung ngang, càng nhỏ càng gọn (180 = bung đủ 2 phía)

    [Header("Open Animation")]
    public float openAnimDuration = 0.5f; // thời lượng clip "Open", để đóng băng đúng lúc animation kết thúc

    void Start()
    {
        currentHealth = maxHealth;
    }

    // Chest có collider dạng va chạm (Is Trigger = false) để chặn player đi xuyên qua.
    // AttackHitBox của Player là Trigger, nên khi nó chạm vào chest, OnTriggerEnter2D vẫn được gọi
    // (Unity chỉ cần 1 trong 2 collider là Trigger, và 1 trong 2 có Rigidbody2D).
    private void OnTriggerEnter2D(Collider2D other)
    {
        AttackHitBox attackHitBox = other.GetComponent<AttackHitBox>();
        if (attackHitBox != null)
        {
            OnAttacked(1);
        }
    }

    // Vẫn giữ hàm public này để có thể gọi thủ công từ script khác nếu cần
    public void OnAttacked(int damage = 1)
    {
        if (isOpened) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            OpenChest();
        }
        else
        {
            // hiệu ứng hit nhẹ (rung lắc, particle...) nếu muốn nhiều hit mới vỡ
            StartCoroutine(ShakeEffect());
        }
    }

    private void OpenChest()
    {
        isOpened = true;

        if (animator != null)
        {
            animator.SetTrigger("Open");
            // Đóng băng animation ở frame cuối cùng sau khi clip "Open" chạy xong,
            // để chest giữ nguyên trạng thái mở vĩnh viễn thay vì lặp lại hoặc quay về Idle.
            StartCoroutine(FreezeAnimatorAfter(openAnimDuration));
        }
        else if (sprite != null && openedSprite != null)
        {
            sprite.sprite = openedSprite;
        }

        var loot = lootTable.RollLoot();
        foreach (var (data, amount) in loot)
        {
            SpawnLootBurst(data, amount);
        }

        // Disable collider để không bị đánh/va chạm tiếp nữa (item vẫn bung ra bình thường trước đó)
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }

    private IEnumerator FreezeAnimatorAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (animator != null)
            animator.enabled = false; // dừng hẳn Animator, giữ nguyên sprite ở frame hiện tại
    }

    private void SpawnLootBurst(LootItemData data, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            GameObject drop = Instantiate(data.prefab, transform.position, Quaternion.identity);

            Rigidbody2D rb = drop.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Góc chỉ dao động quanh phương thẳng đứng (90°) trong khoảng spreadAngle,
                // thay vì random đủ 360° -> item bay lên gọn quanh chest thay vì văng tứ phía.
                float halfSpread = spreadAngle * 0.5f;
                float angleDeg = 90f + Random.Range(-halfSpread, halfSpread);
                float angle = angleDeg * Mathf.Deg2Rad;
                float force = Random.Range(burstForceMin, burstForceMax);

                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                rb.AddForce(dir * force + Vector2.up * upwardForce, ForceMode2D.Impulse);
                rb.AddTorque(Random.Range(-5f, 5f), ForceMode2D.Impulse);
            }

            // Gắn data lên item để script pickup biết loại item + số lượng
            var lootDrop = drop.GetComponent<LootDrop>();
            if (lootDrop != null) lootDrop.Init(data, 1);
            // nếu amount stack được (như coin), có thể set 1 object mang amount = data.amount thay vì loop từng cái
        }
    }

    private IEnumerator ShakeEffect()
    {
        Vector3 originalPos = transform.position;
        float duration = 0.15f, elapsed = 0f;
        while (elapsed < duration)
        {
            transform.position = originalPos + (Vector3)Random.insideUnitCircle * 0.05f;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPos;
    }
}