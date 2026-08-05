using UnityEngine;

public class EnemyHitBox : MonoBehaviour
{
    private float damage = 1f;
    private float knockbackForce = 6f;
    private bool attackFacingRight = true;

    public void Setup(bool facingRight, float dmg, float force)
    {
        attackFacingRight = facingRight;
        damage = dmg;
        knockbackForce = force;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Dùng GetComponentInParent để không bị lỡ nếu Collider nằm ở object con của Player
        Player playerScript = other.GetComponentInParent<Player>();
        if (playerScript != null)
        {
            float dir = attackFacingRight ? 1f : -1f;
            playerScript.TakeDamage(damage, dir, knockbackForce);
        }
    }
}