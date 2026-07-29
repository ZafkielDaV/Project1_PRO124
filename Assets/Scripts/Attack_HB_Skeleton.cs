using UnityEngine;

public class AttackHitBox : MonoBehaviour
{
    public float knockbackForce = 5f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Xác định hướng đẩy dựa vào vị trí Enemy (parent)
                Vector2 direction = (other.transform.position - transform.parent.position).normalized;
                rb.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
            }
        }
    }
}
