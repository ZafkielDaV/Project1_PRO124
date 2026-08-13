using UnityEngine;

public class Trap : MonoBehaviour
{
    public enum PushMode
    {
        Up,
        Down,
        Custom,
        FollowTransform,   // hướng đẩy = transform.up của Trap (xoay object là đổi hướng)
        AwayFromCenter     // hướng đẩy = từ tâm Trap ra vị trí Player (đẩy toả ra)
    }

    [Header("Damage")]
    public float damageAmount = 30f;

    [Header("Push (kiểu Wind Fan)")]
    public PushMode pushMode = PushMode.Up;
    public float trapForce = 15f;
    public float maxPushSpeed = 8f;
    public Vector2 pushDirection = Vector2.up;   // chỉ dùng khi pushMode = Custom

    [Header("Trigger Behaviour")]
    public bool triggerOncePerContact = true;
    public float retriggerCooldown = 1f;

    [Header("Debug")]
    public bool debugLog = false;

    private float lastHitTime = -999f;

    private void OnValidate()
    {
        switch (pushMode)
        {
            case PushMode.Up: pushDirection = Vector2.up; break;
            case PushMode.Down: pushDirection = Vector2.down; break;
                // Custom / FollowTransform / AwayFromCenter: tính runtime, không set cứng ở đây
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (debugLog)
            Debug.Log($"[Trap] Player vào vùng trap ({pushMode}): {other.name}");

        if (triggerOncePerContact)
            TryDamagePlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (!triggerOncePerContact && Time.time - lastHitTime >= retriggerCooldown)
            TryDamagePlayer(other);

        ApplyPush(other);
    }

    private void TryDamagePlayer(Collider2D other)
    {
        Player player = other.GetComponent<Player>();
        if (player == null) return;

        lastHitTime = Time.time;
        player.TakeDamage(damageAmount, Vector2.zero, 0f);
    }

    // Tính hướng đẩy tuỳ theo pushMode
    private Vector2 GetPushDirection(Collider2D other)
    {
        switch (pushMode)
        {
            case PushMode.FollowTransform:
                return transform.up;

            case PushMode.AwayFromCenter:
                {
                    // Dùng tâm bounds của Collider Trap (hoạt động tốt với Tilemap Collider / Composite Collider)
                    Collider2D trapCollider = GetComponent<Collider2D>();
                    Vector2 center = trapCollider != null
                        ? (Vector2)trapCollider.bounds.center
                        : (Vector2)transform.position;

                    Vector2 diff = (Vector2)other.transform.position - center;
                    return diff.sqrMagnitude > 0.0001f ? diff.normalized : Vector2.up; // fallback tránh vector 0
                }

            default: // Up, Down, Custom
                return pushDirection;
        }
    }

    private void ApplyPush(Collider2D other)
    {
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            if (debugLog) Debug.LogError("[Trap] Player không có Rigidbody2D!");
            return;
        }

        Vector2 dir = GetPushDirection(other).normalized;
        float currentSpeedAlongDir = Vector2.Dot(rb.linearVelocity, dir);

        if (debugLog)
            Debug.Log($"[Trap] mode={pushMode}, dir={dir}, speedAlongDir={currentSpeedAlongDir}, cap={maxPushSpeed}");

        if (currentSpeedAlongDir < maxPushSpeed)
        {
            rb.AddForce(dir * trapForce, ForceMode2D.Force);
            if (debugLog) Debug.Log("[Trap] Đã AddForce!");
        }
    }
}