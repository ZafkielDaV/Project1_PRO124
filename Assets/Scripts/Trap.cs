using UnityEngine;

public class Trap : MonoBehaviour
{
    [Header("Damage")]
    public float damageAmount = 30f;
    public float trapKnockback = 0f; // lực đẩy khi trúng bẫy, để 0 nếu chỉ muốn trừ máu mà không đẩy lùi Player

    [Header("Teleport To Nearest Ground")]
    public LayerMask groundLayer;
    public string groundTag = "Ground";
    public float teleportRayDistance = 20f; // tầm quét tìm mặt đất, để đủ dài quét qua nhiều tile/platform
    public float groundStandOffset = 0.05f; // nhích nhẹ lên trên mặt đất để player không bị lún nửa người vào tile

    [Header("Trigger Behaviour")]
    public bool triggerOncePerContact = true; // true: chỉ gây damage 1 lần khi vừa chạm vào (OnTriggerEnter2D)
    public float retriggerCooldown = 1f;      // nếu triggerOncePerContact = false, khoảng cách tối thiểu giữa 2 lần gây damage liên tiếp

    private float lastHitTime = -999f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!triggerOncePerContact) return;
        TryHitPlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (triggerOncePerContact) return;

        if (Time.time - lastHitTime < retriggerCooldown) return;
        TryHitPlayer(other);
    }

    private void TryHitPlayer(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Player player = other.GetComponent<Player>();
        if (player == null) return;

        lastHitTime = Time.time;

        // Hướng "kẻ tấn công" tính theo vị trí Trap so với Player, để khớp đúng chữ ký TakeDamage(damage, attackerDirection, knockback)
        float attackerDirection = Mathf.Sign(other.transform.position.x - transform.position.x);
        if (attackerDirection == 0f) attackerDirection = 1f;

        player.TakeDamage(damageAmount, attackerDirection, trapKnockback);

        // ---- Dịch chuyển tới mặt đất gần nhất ----
        TeleportToNearestGround(other);
    }

    private void TeleportToNearestGround(Collider2D playerCollider)
    {
        Vector2 playerPos = playerCollider.transform.position;

        // Ưu tiên quét 2 bên trái/phải trước
        RaycastHit2D hitLeft = Physics2D.Raycast(playerPos, Vector2.left, teleportRayDistance, groundLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(playerPos, Vector2.right, teleportRayDistance, groundLayer);

        RaycastHit2D bestHit = default;
        bool foundGround = false;
        bool horizontalHit = false;

        if (hitLeft.collider != null && hitLeft.collider.CompareTag(groundTag))
        {
            bestHit = hitLeft;
            foundGround = true;
            horizontalHit = true;
        }

        if (hitRight.collider != null && hitRight.collider.CompareTag(groundTag))
        {
            if (!foundGround || hitRight.distance < bestHit.distance)
            {
                bestHit = hitRight;
                foundGround = true;
                horizontalHit = true;
            }
        }

        // Chỉ quét lên/xuống nếu 2 bên trái/phải không tìm thấy đất
        if (!foundGround)
        {
            RaycastHit2D hitDown = Physics2D.Raycast(playerPos, Vector2.down, teleportRayDistance, groundLayer);
            RaycastHit2D hitUp = Physics2D.Raycast(playerPos, Vector2.up, teleportRayDistance, groundLayer);

            if (hitDown.collider != null && hitDown.collider.CompareTag(groundTag))
            {
                bestHit = hitDown;
                foundGround = true;
            }

            if (hitUp.collider != null && hitUp.collider.CompareTag(groundTag))
            {
                if (!foundGround || hitUp.distance < bestHit.distance)
                {
                    bestHit = hitUp;
                    foundGround = true;
                }
            }
        }

        if (!foundGround)
        {
            Debug.LogWarning("[Trap] Không tìm thấy mặt đất (tag Ground) trong tầm quét để dịch chuyển player.");
            return;
        }

        Vector2 standPosition;

        if (horizontalHit)
        {
            // Đứng cách mặt tường một chút theo phương ngang, giữ nguyên độ cao hiện tại
            float dirX = (bestHit.point.x <= playerPos.x) ? 1f : -1f;
            standPosition = new Vector2(bestHit.point.x + dirX * groundStandOffset, playerPos.y);
        }
        else
        {
            // Đứng phía trên điểm chạm một chút (nếu quét xuống) hoặc phía dưới một chút (nếu quét lên) để không lún vào tile
            float dirY = (bestHit.point.y <= playerPos.y) ? 1f : -1f;
            standPosition = new Vector2(playerPos.x, bestHit.point.y + dirY * groundStandOffset);
        }

        Rigidbody2D playerRb = playerCollider.attachedRigidbody;
        if (playerRb != null)
        {
            playerRb.position = standPosition;
            // Xoá vận tốc cũ để tránh player bị trượt/giật do quán tính còn sót lại sau khi teleport
            playerRb.linearVelocity = Vector2.zero;
        }
        else
        {
            playerCollider.transform.position = standPosition;
        }
    }
}