using UnityEngine;

public class WindFan : MonoBehaviour
{
    [SerializeField] private float windForce = 15f;      // Lực gió đẩy lên
    [SerializeField] private float maxRiseSpeed = 8f;     // Tốc độ bay lên tối đa
    [SerializeField] private Vector2 windDirection = Vector2.up; // Hướng gió (có thể chỉnh nghiêng)

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Chỉ đẩy lên nếu chưa vượt quá tốc độ tối đa
                if (rb.linearVelocity.y < maxRiseSpeed)
                {
                    rb.AddForce(windDirection.normalized * windForce, ForceMode2D.Force);
                }
            }
        }
    }
}