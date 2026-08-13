using UnityEngine;

public class WindFan : MonoBehaviour
{
    [SerializeField] private float windForce = 15f;
    [SerializeField] private float maxRiseSpeed = 8f;
    [SerializeField] private Vector2 windDirection = Vector2.up;

    private void OnTriggerStay2D(Collider2D other)
    {
        Debug.Log($"[WindFan] Trigger với: {other.name}, tag: {other.tag}");

        if (other.CompareTag("Player"))
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                Debug.LogError("[WindFan] Player không có Rigidbody2D!");
                return;
            }

            Debug.Log($"[WindFan] BodyType={rb.bodyType}, velocity.y={rb.linearVelocity.y}, gravityScale={rb.gravityScale}");

            if (rb.linearVelocity.y < maxRiseSpeed)
            {
                rb.AddForce(windDirection.normalized * windForce, ForceMode2D.Force);
                Debug.Log("[WindFan] Đã AddForce!");
            }
        }
    }
}