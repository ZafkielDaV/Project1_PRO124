using UnityEngine;

public class LootDrop : MonoBehaviour
{
    public LootItemData data;
    public int amount = 1;

    [Header("Settle On Ground")]
    public float settleVelocityThreshold = 0.05f; // vận tốc dưới mức này coi như đã dừng hẳn
    public float settleCheckDelay = 0.4f;         // chỉ bắt đầu check sau khi rơi được 1 lúc (tránh dừng ngay lúc vừa bung ra)
    private bool isSettled = false;

    private Rigidbody2D rb;
    private float spawnTime;

    [Header("Ignore Item-Item Collision")]
    public string lootItemLayerName = "LootItem"; // tạo layer này trong Project Settings > Tags and Layers

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spawnTime = Time.time;

        // Cho item rơi xuyên qua nhau: gán vào 1 layer riêng và tắt va chạm giữa layer đó với chính nó,
        // đất/tường vẫn nằm ở layer khác nên item vẫn va chạm và nằm trên đất bình thường.
        int lootLayer = LayerMask.NameToLayer(lootItemLayerName);
        if (lootLayer != -1)
        {
            gameObject.layer = lootLayer;
            Physics2D.IgnoreLayerCollision(lootLayer, lootLayer, true);
        }
        else
        {
            Debug.LogWarning($"[LootDrop] Chưa tạo layer '{lootItemLayerName}' trong Project Settings > Tags and Layers.");
        }
    }

    public void Init(LootItemData itemData, int itemAmount)
    {
        data = itemData;
        amount = itemAmount;
    }

    void Update()
    {
        CheckSettle();
    }

    // Khi item đã rơi xuống đất và gần như hết vận tốc, khoá cứng lại để nằm yên,
    // tránh trường hợp cứ trượt/lắc/xoay mãi do lực bung ban đầu còn dư.
    private void CheckSettle()
    {
        if (isSettled || rb == null) return;
        if (Time.time - spawnTime < settleCheckDelay) return;

        if (rb.linearVelocity.magnitude < settleVelocityThreshold)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeAll; // khoá cứng vị trí + xoay, item nằm yên hẳn trên đất
            isSettled = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Pickup(other);
    }

    private void Pickup(Collider2D playerCollider)
    {
        Player player = playerCollider.GetComponent<Player>();
        if (player == null) return;

        switch (data.type)
        {
            case LootType.Coin:
                if (CoinManager.Instance != null)
                    CoinManager.Instance.AddCoin(amount);
                break;

            case LootType.HealthPotion:
                player.Heal(amount * 10);
                break;

            case LootType.ManaPotion:
                player.RestoreMana(amount * 15);
                break;

            case LootType.RareItem:
                // TODO: gắn vào inventory thật của bạn khi có hệ thống
                break;
        }

        Destroy(gameObject);
    }
}