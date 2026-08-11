using UnityEngine;
using System.Collections;

public class Slime : MonoBehaviour, IDamageable
{
    [Header("Hop Movement")]
    public float hopForce = 5f;         // lực nhảy lên (Y) mỗi lần hop - đủ cao để rời hẳn groundCheck
    public float hopSpeed = 1.5f;       // tốc độ ngang trong lúc hop - slime nhảy tại chỗ nhiều hơn di chuyển xa
    public float chaseHopForce = 6f;
    public float chaseHopSpeed = 2.5f;
    public float minHopInterval = 1.2f; // khoảng nghỉ giữa 2 lần hop khi wander
    public float maxHopInterval = 2.2f;
    public float minIdleTime = 0.4f;
    public float maxIdleTime = 1.2f;
    public float wanderRange = 3f;

    [Header("Player Detection (Raycast)")]
    public Transform player;
    public float detectRange = 4f;
    public LayerMask obstacleLayer;
    public LayerMask playerLayer;
    public float loseSightExtraRange = 1f;

    [Header("Player Detection - Behind")]
    public float behindDetectRange = 2f;

    [Header("Aggro On Hit")]
    public float hitAggroDuration = 2.5f;
    private float hitAggroTimer = 0f;

    [Header("Ground / Physics")]
    public LayerMask groundLayer;
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;

    [Header("Edge Detection (Left/Right Ground Check)")]
    public bool avoidFallingOffEdge = true;
    public float edgeCheckOffset = 0.3f;
    public float edgeCheckDistance = 0.6f;

    [Header("Knockback")]
    public float knockbackDuration = 0.15f;

    [Header("Knockback - Wall Collision")]
    public float bodyCollisionRadius = 0.25f;
    public float wallCheckSkin = 0.05f;

    [Header("Attack Player (Body Bump)")]
    public float attackRange = 0.6f;    // slime tấn công bằng cách nảy vào người chơi khi ở gần
    public float attackCooldown = 1.2f;
    public float attackDamage = 1f;
    public float attackKnockbackForce = 4f;
    public float attackHopForce = 6f;   // hop mạnh hơn khi lao vào tấn công
    public GameObject attackHitBox;

    [Header("Health")]
    public float maxHealth = 10f;       // slime thường máu ít hơn enemy thường
    private float currentHealth;
    private bool isDead = false;

    [Header("Death / Split")]
    public string deathAnimStateName = "Die";
    public float deathAnimDuration = 0.6f;
    public bool splitOnDeath = false;       // slime nhỏ có thể tách đôi khi chết
    public GameObject miniSlimePrefab;
    public int splitCount = 2;
    public float splitMinHealthToSplit = 8f; // chỉ tách nếu máu tối đa đủ lớn (tránh mini slime tách vô hạn)

    private SlimeHitBox hitBoxScript;
    private Rigidbody2D rb;
    private Animator animator;
    private bool facingRight = true;
    private bool isGrounded;

    private Vector3 spawnPosition;
    private float moveDirection = 1f;
    private float stateTimer = 0f;
    private bool isIdling = false;
    private bool isAirborne = false; // đang trong pha nhảy (chưa chạm đất lại)
    private bool hasLeftGround = false; // đảm bảo slime đã thực sự rời mặt đất trước khi được tính là "đã tiếp đất" lại

    private bool isChasing = false;

    private bool isKnockedBack = false;
    private float knockbackTimer = 0f;

    private bool isAttacking = false;
    private float lastAttackTime = -999f;

    private Coroutine knockbackRoutine;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spawnPosition = transform.position;

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        currentHealth = maxHealth;

        PickNewIdleOrHop();

        if (attackHitBox != null)
        {
            attackHitBox.SetActive(false);
            hitBoxScript = attackHitBox.GetComponent<SlimeHitBox>();
        }
    }

    void Update()
    {
        if (isDead) return;

        isGrounded = groundCheck != null &&
            Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Track việc slime đã thực sự rời mặt đất (isGrounded == false ít nhất 1 frame) trong pha nhảy này chưa.
        // Nếu không có bước này, groundCheck có thể vẫn overlap mặt đất suốt cú hop yếu -> isAirborne
        // bị reset false ngay khi velocity.y vừa chuyển sang <= 0 (tức lúc lên đỉnh, CHƯA rơi xuống chạm đất),
        // khiến slime nhảy tiếp liên tục giữa không trung mà chưa từng chạm đất thật sự.
        if (!isGrounded)
            hasLeftGround = true;

        if (isGrounded && hasLeftGround && rb.linearVelocity.y <= 0f)
        {
            isAirborne = false;
            hasLeftGround = false;
        }

        if (isKnockedBack)
        {
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0f)
                isKnockedBack = false;

            UpdateAnimator();
            return;
        }

        if (isAttacking)
        {
            UpdateAnimator();
            return;
        }

        bool playerDetected = DetectPlayerByRaycast() || DetectPlayerBehind();

        if (hitAggroTimer > 0f)
        {
            hitAggroTimer -= Time.deltaTime;
            playerDetected = true;
        }

        if (playerDetected)
        {
            isChasing = true;
        }
        else if (isChasing)
        {
            isChasing = false;
            PickNewIdleOrHop();
        }

        if (isChasing)
        {
            float distToPlayer = player != null ? Mathf.Abs(player.position.x - transform.position.x) : Mathf.Infinity;

            if (distToPlayer <= attackRange)
            {
                float dir = player.position.x - transform.position.x >= 0f ? 1f : -1f;
                if ((dir > 0 && !facingRight) || (dir < 0 && facingRight))
                    Flip();

                if (isGrounded && !isAirborne)
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

                if (Time.time - lastAttackTime >= attackCooldown && isGrounded)
                {
                    StartCoroutine(DoAttackPlayer());
                }
            }
            else
            {
                ChaseHop();
            }
        }
        else
        {
            Wander();
        }

        UpdateAnimator();
    }

    // ---------------- ATTACK PLAYER (lao vào bằng cú nhảy mạnh) ----------------

    IEnumerator DoAttackPlayer()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        if (animator != null)
            animator.SetTrigger("Attack");

        float dir = facingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * chaseHopSpeed, attackHopForce);
        isAirborne = true;
        hasLeftGround = false;

        if (attackHitBox != null)
        {
            attackHitBox.SetActive(true);
            if (hitBoxScript != null)
                hitBoxScript.Setup(facingRight, attackDamage, attackKnockbackForce);
        }

        // Hitbox bật trong lúc slime đang bay tới, tắt sau khi tiếp đất hoặc hết thời gian tối đa
        float safety = 0f;
        while (!isGrounded && safety < 1.5f)
        {
            safety += Time.deltaTime;
            yield return null;
        }

        if (attackHitBox != null)
            attackHitBox.SetActive(false);

        isAttacking = false;
    }

    // ---------------- HEALTH ----------------

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        isKnockedBack = false;
        isAttacking = false;

        rb.linearVelocity = Vector2.zero;
        StopKnockbackRoutineIfRunning();

        if (attackHitBox != null)
            attackHitBox.SetActive(false);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (animator != null)
        {
            animator.ResetTrigger("Hurt");
            animator.ResetTrigger("Attack");
        }

        if (splitOnDeath && miniSlimePrefab != null && maxHealth >= splitMinHealthToSplit)
            SpawnMiniSlimes();

        PlayDeathAnimation();

        Destroy(gameObject, deathAnimDuration);
    }

    private void SpawnMiniSlimes()
    {
        for (int i = 0; i < splitCount; i++)
        {
            float offsetX = (i == 0) ? -0.3f : 0.3f;
            Vector3 spawnPos = transform.position + new Vector3(offsetX, 0.1f, 0f);
            GameObject mini = Instantiate(miniSlimePrefab, spawnPos, Quaternion.identity);

            Rigidbody2D miniRb = mini.GetComponent<Rigidbody2D>();
            if (miniRb != null)
            {
                float dir = (i == 0) ? -1f : 1f;
                miniRb.linearVelocity = new Vector2(dir * hopSpeed, hopForce * 0.75f);
            }
        }
    }

    private void PlayDeathAnimation()
    {
        if (animator == null) return;

        float clipLength = GetClipLength(deathAnimStateName);

        if (clipLength > 0f && deathAnimDuration > 0f)
            animator.speed = clipLength / deathAnimDuration;

        animator.Play(deathAnimStateName, 0, 0f);
    }

    private float GetClipLength(string clipName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return 0f;

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip != null && clip.name == clipName)
                return clip.length;
        }

        return 0f;
    }

    // ---------------- KNOCKBACK ----------------

    public void ApplyKnockback(float direction, float force, float upForce = 3f, float damage = 0f)
    {
        if (isDead) return;

        if (damage > 0f)
            TakeDamage(damage);

        if (isDead) return;

        isKnockedBack = true;
        knockbackTimer = knockbackDuration;

        isChasing = true;
        hitAggroTimer = hitAggroDuration;

        if (animator != null)
            animator.SetTrigger("Hurt");

        StopKnockbackRoutineIfRunning();
        knockbackRoutine = StartCoroutine(KnockbackRoutine(direction, force, upForce));
    }

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

        float clampedTargetX = startPos.x + horizontalDistance;

        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / knockbackDuration);

            float x = startPos.x + horizontalDistance * t;
            float y = startPos.y + (upForce * knockbackDuration * 0.5f) * Mathf.Sin(t * Mathf.PI);

            Vector2 currentPos = rb.position;
            float moveX = x - currentPos.x;

            if (Mathf.Abs(moveX) > 0.0001f)
            {
                float castDir = Mathf.Sign(moveX);
                float castDist = Mathf.Abs(moveX) + wallCheckSkin;

                RaycastHit2D wallHit = Physics2D.CircleCast(currentPos, bodyCollisionRadius, new Vector2(castDir, 0f), castDist, obstacleLayer);

                if (wallHit.collider != null)
                {
                    float allowedDist = Mathf.Max(0f, wallHit.distance - wallCheckSkin);
                    x = currentPos.x + castDir * allowedDist;

                    clampedTargetX = x;
                    horizontalDistance = clampedTargetX - startPos.x;
                }
            }

            rb.MovePosition(new Vector2(x, y));

            yield return null;
        }

        float finalX = startPos.x + horizontalDistance;
        rb.MovePosition(new Vector2(finalX, startPos.y));

        rb.linearVelocity = Vector2.zero;
        knockbackRoutine = null;
    }

    // ---------------- WANDER (nhảy ngẫu nhiên) ----------------

    void Wander()
    {
        stateTimer -= Time.deltaTime;

        if (isIdling)
        {
            // Đứng yên chờ giữa các lần nhảy, không tác động lực ngang
            if (isGrounded)
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        else if (isGrounded && !isAirborne)
        {
            float distFromSpawn = transform.position.x - spawnPosition.x;
            if (distFromSpawn > wanderRange && moveDirection > 0f)
                moveDirection = -1f;
            else if (distFromSpawn < -wanderRange && moveDirection < 0f)
                moveDirection = 1f;

            if (avoidFallingOffEdge)
            {
                if (moveDirection > 0f && !IsGroundOnRight())
                    moveDirection = -1f;
                else if (moveDirection < 0f && !IsGroundOnLeft())
                    moveDirection = 1f;
            }

            if ((moveDirection > 0 && !facingRight) || (moveDirection < 0 && facingRight))
                Flip();

            // Thực hiện 1 cú hop rồi chuyển sang idle chờ lần sau
            rb.linearVelocity = new Vector2(moveDirection * hopSpeed, hopForce);
            isAirborne = true;
            hasLeftGround = false;

            isIdling = true;
            stateTimer = Random.Range(minIdleTime, maxIdleTime);
        }

        if (stateTimer <= 0f && isIdling)
        {
            PickNewIdleOrHop();
        }
    }

    void PickNewIdleOrHop()
    {
        isIdling = true;
        stateTimer = Random.Range(minHopInterval, maxHopInterval);
        moveDirection = Random.value > 0.5f ? 1f : -1f;
    }

    // ---------------- CHASE (nhảy liên tục về phía player) ----------------

    void ChaseHop()
    {
        if (player == null) return;
        if (!isGrounded || isAirborne) return; // chờ tiếp đất mới nhảy tiếp

        float dirX = player.position.x - transform.position.x;
        float dir = Mathf.Sign(dirX);

        if (avoidFallingOffEdge)
        {
            bool groundAhead = dir > 0f ? IsGroundOnRight() : IsGroundOnLeft();
            if (!groundAhead)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

                if ((dir > 0 && !facingRight) || (dir < 0 && facingRight))
                    Flip();

                return;
            }
        }

        if ((dir > 0 && !facingRight) || (dir < 0 && facingRight))
            Flip();

        rb.linearVelocity = new Vector2(dir * chaseHopSpeed, chaseHopForce);
        isAirborne = true;
        hasLeftGround = false;
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

    bool DetectPlayerBehind()
    {
        if (player == null) return false;

        Vector2 origin = transform.position;
        float dir = facingRight ? -1f : 1f;

        RaycastHit2D hit = Physics2D.Raycast(origin, new Vector2(dir, 0f), behindDetectRange, obstacleLayer | playerLayer);

        if (hit.collider != null)
        {
            if (((1 << hit.collider.gameObject.layer) & playerLayer) != 0)
                return true;
        }

        return false;
    }

    // ---------------- EDGE DETECTION (LEFT / RIGHT GROUND CHECK) ----------------

    Vector2 GetGroundCheckOrigin()
    {
        return groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position;
    }

    bool CheckGroundAtOffset(float horizontalOffset)
    {
        Vector2 origin = GetGroundCheckOrigin() + new Vector2(horizontalOffset, 0f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, edgeCheckDistance, groundLayer);
        return hit.collider != null;
    }

    bool IsGroundOnRight()
    {
        return CheckGroundAtOffset(edgeCheckOffset);
    }

    bool IsGroundOnLeft()
    {
        return CheckGroundAtOffset(-edgeCheckOffset);
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

        animator.SetBool("isGrounded", isGrounded);
        animator.SetFloat("VerticalSpeed", rb.linearVelocity.y);
        animator.SetBool("isJumping", isAirborne);
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

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Vector3 behindOrigin = transform.position;
        float behindDir = facingRight ? -1f : 1f;
        Gizmos.DrawLine(behindOrigin, behindOrigin + new Vector3(behindDir * behindDetectRange, 0f, 0f));

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;
        Vector3 groundOrigin = groundCheck != null ? groundCheck.position : transform.position;
        Vector3 rightOrigin = groundOrigin + Vector3.right * edgeCheckOffset;
        Vector3 leftOrigin = groundOrigin + Vector3.left * edgeCheckOffset;
        Gizmos.DrawLine(rightOrigin, rightOrigin + Vector3.down * edgeCheckDistance);
        Gizmos.DrawLine(leftOrigin, leftOrigin + Vector3.down * edgeCheckDistance);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, bodyCollisionRadius);
    }
}