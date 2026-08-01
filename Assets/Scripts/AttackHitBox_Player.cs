using UnityEngine;

public class AttackHitBox : MonoBehaviour
{
    private bool attackFacingRight = true;
    private float knockbackForce = 6f;

    public void Setup(bool facingRight, float force)
    {
        attackFacingRight = facingRight;
        knockbackForce = force;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // DEBUG: in ra MỌI thứ hitbox chạm vào, bất kể tag gì
        Debug.Log($"[HitBox] Trigger với: {other.gameObject.name}, Tag: {other.tag}, Layer: {LayerMask.LayerToName(other.gameObject.layer)}");

        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                float dir = attackFacingRight ? 1f : -1f;
                enemy.ApplyKnockback(dir, knockbackForce);
                Debug.Log("[HitBox] Đã gọi ApplyKnockback thành công!");
            }
            else
            {
                Debug.Log("[HitBox] Có tag Enemy nhưng KHÔNG tìm thấy component Enemy.cs trên object!");
            }
        }
    }
}