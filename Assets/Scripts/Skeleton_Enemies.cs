using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float chaseSpeed = 3.5f;

    [Header("Random Wander")]
    public float minMoveTime = 1f;
    public float maxMoveTime = 3f;
    public float minIdleTime = 0.5f;
    public float maxIdleTime = 2f;
    public float wanderRange = 4f;

    [Header("Player Detection (Raycast)")]
    public Transform player;
    public float detectRange = 6f;
    public LayerMask obstacleLayer;
    public LayerMask playerLayer;
    public float loseSightExtraRange = 2f;

    [Header("Ground / Physics")]
    public LayerMask groundLayer;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;

    [Header("Knockback")]
    public float knockbackDuration = 0.2f; // thời gian AI bị khóa sau khi trúng đòn

    [Header("Attack Player")]
    public float attackRange = 1f;
    public float attackCooldown = 1.5f;
    public float attackDamage = 1f;
    public float attackKnockbackForce = 6f;
    public float attackWindup = 0.2f;
    public GameObject attackHitBox; // GameObject hitbox riêng của Enemy

    private EnemyHitBox hitBoxScript;
    private Rigidbody2D rb;
    private Animator animator;
    private bool facingRight = true;
    private bool isGrounded;

    private Vector3 spawnPosition;
    private float moveDirection = 1f;
    private float stateTimer = 0f;
    private bool isIdling = false;

    private bool isChasing = false;

    private bool isKnockedBack = false;
    private float knockbackTimer = 0f;

    private bool isAttacking = false;
    private float lastAttackTime = -999f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spawnPosition = transform.position;

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        PickNewIdleOrMove();
        if (attackHitBox != null)
        {
            attackHitBox.SetActive(false);
            hitBoxScript = attackHitBox.GetComponent<EnemyHitBox>();
        }
    }

    void Update()
    {
        isGrounded = groundCheck != null &&
            Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Trong lúc bị knockback: bỏ qua toàn bộ AI, chỉ đếm ngược thời gian
        if (isKnockedBack)
        {
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0f)
                isKnockedBack = false;

            UpdateAnimator();
            return; // không chạy Wander/Chase/Detect trong lúc này
        }

        // Trong lúc đang tự ra đòn: đứng yên, không chạy AI khác
        if (isAttacking)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            UpdateAnimator();
            return;
        }

        bool playerDetected = DetectPlayerByRaycast();

        if (playerDetected)
        {
            isChasing = true;
        }
        else if (isChasing)
        {
            isChasing = false;
            PickNewIdleOrMove();
        }

        if (isChasing)
        {
            float distToPlayer = player != null ? Mathf.Abs(player.position.x - transform.position.x) : Mathf.Infinity;

            if (distToPlayer <= attackRange)
            {
                // Đã vào tầm đánh -> luôn đứng yên tại đây, không lao thêm vào Player
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

                // Quay mặt đúng hướng Player dù đang đứng yên chờ
                float dir = player.position.x - transform.position.x >= 0f ? 1f : -1f;
                if ((dir > 0 && !facingRight) || (dir < 0 && facingRight))
                    Flip();

                // Chỉ ra đòn khi hết cooldown, còn không thì tiếp tục đứng yên chờ
                if (Time.time - lastAttackTime >= attackCooldown)
                {
                    StartCoroutine(DoAttackPlayer());
                }
            }
            else
            {
                ChasePlayer();
            }
        }
        else
        {
            Wander();
        }

        UpdateAnimator();
    }

    // ---------------- ATTACK PLAYER ----------------

    IEnumerator DoAttackPlayer()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (animator != null)
            animator.SetTrigger("Attack");

        // Chờ "vung tay" xong rồi mới bật hitbox thật sự gây damage
        yield return new WaitForSeconds(attackWindup);

        if (attackHitBox != null)
        {
            attackHitBox.SetActive(true);

            if (hitBoxScript != null)
                hitBoxScript.Setup(facingRight, attackDamage, attackKnockbackForce);
        }

        // Hitbox chỉ bật trong khoảng thời gian ngắn (khớp lúc vũ khí thật sự vung tới)
        yield return new WaitForSeconds(0.1f);

        if (attackHitBox != null)
            attackHitBox.SetActive(false);

        isAttacking = false;
    }

    // ---------------- KNOCKBACK ----------------


    public void ApplyKnockback(float direction, float force, float upForce = 4f)
    {
        isKnockedBack = true;
        knockbackTimer = knockbackDuration;

        if (animator != null)
            animator.SetTrigger("Hurt");

        StopKnockbackRoutineIfRunning();
        knockbackRoutine = StartCoroutine(KnockbackRoutine(direction, force, upForce));
    }

    private Coroutine knockbackRoutine;

    private void StopKnockbackRoutineIfRunning()
    {
        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);
    }

    private IEnumerator KnockbackRoutine(float direction, float force, float upForce)
    {
        float elapsed = 0f;
        Vector2 startPos = rb.position;
        float horizontalDistance = direction * force * knockbackDuration;

        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;

            // Clamp để không bao giờ vượt quá 1 -> tránh sin() ra số âm ở frame cuối
            float t = Mathf.Clamp01(elapsed / knockbackDuration);

            float x = startPos.x + horizontalDistance * t;
            float y = startPos.y + (upForce * knockbackDuration * 0.5f) * Mathf.Sin(t * Mathf.PI);

            rb.MovePosition(new Vector2(x, y));

            yield return null;
        }

        // Ép về đúng vị trí Y ban đầu để chắc chắn không bị lệch/tụt đất
        float finalX = startPos.x + horizontalDistance;
        rb.MovePosition(new Vector2(finalX, startPos.y));

        rb.linearVelocity = Vector2.zero;
        knockbackRoutine = null;
    }

    // ---------------- WANDER ----------------

    void Wander()
    {
        stateTimer -= Time.deltaTime;

        if (isIdling)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        else
        {
            float distFromSpawn = transform.position.x - spawnPosition.x;
            if (distFromSpawn > wanderRange && moveDirection > 0f)
                moveDirection = -1f;
            else if (distFromSpawn < -wanderRange && moveDirection < 0f)
                moveDirection = 1f;

            rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);

            if ((moveDirection > 0 && !facingRight) || (moveDirection < 0 && facingRight))
                Flip();
        }

        if (stateTimer <= 0f)
        {
            PickNewIdleOrMove();
        }
    }

    void PickNewIdleOrMove()
    {
        isIdling = !isIdling;

        if (isIdling)
        {
            stateTimer = Random.Range(minIdleTime, maxIdleTime);
        }
        else
        {
            stateTimer = Random.Range(minMoveTime, maxMoveTime);
            moveDirection = Random.value > 0.5f ? 1f : -1f;
        }
    }

    // ---------------- CHASE ----------------

    void ChasePlayer()
    {
        if (player == null) return;

        float dirX = player.position.x - transform.position.x;
        float dir = Mathf.Sign(dirX);

        rb.linearVelocity = new Vector2(dir * chaseSpeed, rb.linearVelocity.y);

        if ((dir > 0 && !facingRight) || (dir < 0 && facingRight))
            Flip();
    }

    // ---------------- DETECTION ----------------

    bool DetectPlayerByRaycast()
    {
        if (player == null) return false;

        Vector2 origin = transform.position;
        float dir = facingRight ? 1f : -1f;
        float range = isChasing ? detectRange + loseSightExtraRange : detectRange;

        RaycastHit2D hit = Physics2D.Raycast(origin, new Vector2(dir, 0f), range, obstacleLayer | playerLayer);

        if (hit.collider != null)
        {
            if (((1 << hit.collider.gameObject.layer) & playerLayer) != 0)
                return true;
        }

        return false;
    }

    // ---------------- FLIP ----------------

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    // ---------------- ANIMATION ----------------

    void UpdateAnimator()
    {
        if (animator == null) return;

        float speed = Mathf.Abs(rb.linearVelocity.x);
        animator.SetFloat("Speed", speed);
        animator.SetBool("isWalking", speed > 0.05f);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        float dir = facingRight ? 1f : -1f;
        Vector3 origin = transform.position;
        Gizmos.DrawLine(origin, origin + new Vector3(dir * detectRange, 0f, 0f));

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}