using UnityEngine;

public class EnemyHitBox : MonoBehaviour
{
    private float damage = 1f;
    private float knockbackForce = 6f;
    private bool attackFacingRight = true;

    // Được Enemy gọi mỗi lần bắt đầu ra đòn
    public void Setup(bool facingRight, float dmg, float force)
    {
        attackFacingRight = facingRight;
        damage = dmg;
        knockbackForce = force;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Player playerScript = other.GetComponent<Player>();
            if (playerScript != null)
            {
                float dir = attackFacingRight ? 1f : -1f;
                playerScript.TakeDamage(damage, dir, knockbackForce);
            }
        }
    }
}