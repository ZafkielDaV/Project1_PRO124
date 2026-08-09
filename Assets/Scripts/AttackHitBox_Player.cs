using UnityEngine;

public class AttackHitBox : MonoBehaviour
{
    private bool attackFacingRight = true;
    private float knockbackForce = 6f;
    private float knockbackUpForce = 4f; // lực đẩy lên
    private float attackDamage = 10f;    // sát thương gây ra khi trúng Enemy

    public void Setup(bool facingRight, float force, float upForce = 4f)
    {
        attackFacingRight = facingRight;
        knockbackForce = force;
        knockbackUpForce = upForce;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[HitBox] Trigger với: {other.gameObject.name}, Tag: {other.tag}, Layer: {LayerMask.LayerToName(other.gameObject.layer)}");

        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                float dir = attackFacingRight ? 1f : -1f;
                enemy.ApplyKnockback(dir, knockbackForce, knockbackUpForce, attackDamage);
                Debug.Log("[HitBox] Đã gọi ApplyKnockback thành công!");
            }
            else
            {
                Debug.Log("[HitBox] Có tag Enemy nhưng KHÔNG tìm thấy component Enemy.cs trên object!");
            }
        }
    }
}