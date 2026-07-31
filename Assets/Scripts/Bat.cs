using UnityEngine;
using System.Collections;

public class Bat : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float chaseSpeed = 3f;
    public float moveRange = 3f;
    public float detectDistance = 6f;
    public float attackDistance = 2.3f;
    public float attackCooldown = 1f;

    [Header("References")]
    public Animator animator;
    public LayerMask playerLayer;
    public Transform player;
    public GameObject attackHitBox;

    private Vector2 startPos;
    private Vector2 targetPos;
    private bool isPatrolling = true;
    private bool isChasing = false;
    private bool isAttacking = false;
    private bool canAttack = true;

    private SpriteRenderer spriteRenderer;
    private int attackIndex = 0;
    private Coroutine patrolWaitCoroutine;

    void Start()
    {
        startPos = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        SetRandomTarget();

        if (attackHitBox != null)
            attackHitBox.SetActive(false);
    }

    void Update()
    {
        // Flip HitBox theo hướng của Sprite
        if (attackHitBox != null)
        {
            Vector3 scale = attackHitBox.transform.localScale;
            scale.x = spriteRenderer.flipX ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            attackHitBox.transform.localScale = scale;
        }

        if (isAttacking) return;

        CheckForPlayer();

        if (isChasing)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }
    }

    void CheckForPlayer()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Kiểm tra khoảng cách phát hiện
        if (distanceToPlayer <= detectDistance)
        {
            isChasing = true;

            // Hủy đếm giờ đi tuần nếu phát hiện thấy Player
            if (patrolWaitCoroutine != null)
            {
                StopCoroutine(patrolWaitCoroutine);
                patrolWaitCoroutine = null;
            }

            // Tấn công nếu trong tầm và đủ điều kiện
            if (distanceToPlayer <= attackDistance && canAttack)
            {
                string attackTrigger = attackIndex % 2 == 0 ? "At1" : "At2";
                attackIndex++;
                StartCoroutine(PlayAttack(attackTrigger));
            }
        }
        else
        {
            if (isChasing)
            {
                // Mất dấu Player, quay lại đi tuần
                isChasing = false;
                SetRandomTarget();
            }
        }
    }

    void Patrol()
    {
        if (isChasing || isAttacking) return;

        transform.position = Vector2.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
        animator.SetBool("isWalking", true);

        UpdateFlip(targetPos.x - transform.position.x);

        if (Vector2.Distance(transform.position, targetPos) < 0.1f)
        {
            animator.SetBool("isWalking", false);
            if (patrolWaitCoroutine == null)
            {
                patrolWaitCoroutine = StartCoroutine(WaitAndMove());
            }
        }
    }

    void SetRandomTarget()
    {
        float randX = Random.Range(-moveRange, moveRange);
        targetPos = new Vector2(startPos.x + randX, startPos.y);
    }

    IEnumerator WaitAndMove()
    {
        yield return new WaitForSeconds(Random.Range(1f, 3f));
        SetRandomTarget();
        patrolWaitCoroutine = null;
    }

    void ChasePlayer()
    {
        if (player == null || isAttacking) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > attackDistance)
        {
            Vector2 newPos = new Vector2(
                Mathf.MoveTowards(transform.position.x, player.position.x, chaseSpeed * Time.deltaTime),
                transform.position.y
            );
            transform.position = newPos;

            UpdateFlip(player.position.x - transform.position.x);
            animator.SetBool("isWalking", true);
        }
        else
        {
            animator.SetBool("isWalking", false);
        }
    }

    IEnumerator PlayAttack(string attackTrigger)
    {
        isAttacking = true;
        canAttack = false;
        animator.SetBool("isWalking", false);

        // Quay mặt về phía Player trước khi đánh
        UpdateFlip(player.position.x - transform.position.x);

        animator.SetTrigger(attackTrigger);

        if (attackHitBox != null)
            attackHitBox.SetActive(true);

        yield return new WaitForSeconds(0.5f); // Thời gian bật HitBox

        if (attackHitBox != null)
            attackHitBox.SetActive(false);

        yield return new WaitForSeconds(0.3f); // Thời gian chờ kết thúc Animation

        isAttacking = false;

        // Cooldown trước khi có thể đánh tiếp
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    void UpdateFlip(float directionX)
    {
        if (directionX > 0.01f) spriteRenderer.flipX = false;
        else if (directionX < -0.01f) spriteRenderer.flipX = true;
    }
}