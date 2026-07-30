using UnityEngine;
using System.Collections;

public class EnemyPatrol : MonoBehaviour
{
    public float moveSpeed = 2f;          // tốc độ di chuyển
    public float moveRange = 3f;          // phạm vi di chuyển từ vị trí ban đầu
    public Animator animator;             // Animator để chuyển Idle/Walk

    private Vector2 startPos;
    private Vector2 targetPos;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        startPos = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
        StartCoroutine(PatrolRoutine());
    }

    IEnumerator PatrolRoutine()
    {
        while (true)
        {
            // chọn vị trí ngẫu nhiên trong phạm vi, tránh quá gần
            float randX;
            do
            {
                randX = Random.Range(-moveRange, moveRange);
            } while (Mathf.Abs(randX) < 0.2f); // đảm bảo có khoảng cách để di chuyển

            targetPos = new Vector2(startPos.x + randX, startPos.y);

            // nếu có khoảng cách đủ lớn thì bật Walk
            if (Vector2.Distance(transform.position, targetPos) > 0.1f)
                animator.SetBool("isWalking", true);

            // di chuyển tới target
            while (Vector2.Distance(transform.position, targetPos) > 0.1f)
            {
                transform.position = Vector2.MoveTowards(
                    transform.position,
                    targetPos,
                    moveSpeed * Time.deltaTime
                );

                // flip theo hướng
                spriteRenderer.flipX = targetPos.x > transform.position.x ? false : true;

                yield return null;
            }

            // tới nơi → Idle
            animator.SetBool("isWalking", false);

            // dừng ngẫu nhiên 1–3s
            yield return new WaitForSeconds(Random.Range(1f, 3f));
        }
    }
}
