using UnityEngine;

public class Trap : MonoBehaviour
{
    [Header("Damage")]
    public float damageAmount = 30f;
    public float trapKnockback = 8f; // lực đẩy ngang khi trúng bẫy (Player.TakeDamage chỉ hỗ trợ đẩy ngang)

    [Header("Trigger Behaviour")]
    public bool triggerOncePerContact = true; // true: chỉ gây damage 1 lần khi vừa va chạm vào (OnCollisionEnter2D)
    public float retriggerCooldown = 1f;      // nếu triggerOncePerContact = false, khoảng cách tối thiểu giữa 2 lần gây damage liên tiếp

    private float lastHitTime = -999f;

    // Trap dạng va chạm vật lý thật: Collider2D của Trap KHÔNG tick Is Trigger,
    // nên dùng OnCollisionEnter2D/OnCollisionStay2D thay vì OnTriggerEnter2D/OnTriggerStay2D.
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!triggerOncePerContact) return;
        TryHitPlayer(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (triggerOncePerContact) return;

        if (Time.time - lastHitTime < retriggerCooldown) return;
        TryHitPlayer(collision);
    }

    private void TryHitPlayer(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        Player player = collision.gameObject.GetComponent<Player>();
        if (player == null) return;

        lastHitTime = Time.time;

        // Dùng normal của điểm va chạm đầu tiên: normal luôn hướng từ Trap ra ngoài phía Player
        ContactPoint2D contact = collision.GetContact(0);
        float yDir = contact.normal.y >= 0f ? 1f : -1f;
        Debug.Log($"[Trap] player.y={collision.transform.position.y}, trap.y={transform.position.y}, yDir={yDir}");
        player.TakeDamage(damageAmount, new Vector2(0f, yDir), trapKnockback);

        player.TakeDamage(damageAmount, new Vector2(0f, yDir), trapKnockback);
    }
}