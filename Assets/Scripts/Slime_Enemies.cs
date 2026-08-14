using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemySlime : MonoBehaviour, IDamageable
{
    [Header("Di chuyển")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float chaseSpeed = 3f;
    [SerializeField] private float minMoveDistance = 2f;   // quãng đường tối thiểu mỗi lượt
    [SerializeField] private float maxMoveDistance = 5f;   // quãng đường tối đa mỗi lượt

    [Header("Dừng ngẫu nhiên")]
    [SerializeField] private float minPauseTime = 0.5f;
    [SerializeField] private float maxPauseTime = 2f;

    [Header("Raycast va chạm 2 bên (trái/phải)")]
    [SerializeField] private Transform wallCheckPoint;      // đặt ngang tâm enemy, hướng ra 2 bên
    [SerializeField] private float wallCheckDistance = 0.3f;

    [Header("Raycast chân (rơi & bám Ground)")]
    [SerializeField] private Transform groundCheckPoint;    // đặt ở chân enemy
    [SerializeField] private float groundCheckDistance = 0.6f;
    [SerializeField] private float fallSpeed = 5f;

    [Header("Ground tag")]
    [SerializeField] private string groundTag = "Ground";

    [Header("Player Detection (Raycast)")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectRange = 5f;
    [SerializeField] private float loseSightExtraRange = 2f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float behindDetectRange = 2f; // raycast phía sau lưng

    [Header("Aggro On Hit")]
    [SerializeField] private float hitAggroDuration = 3f;
    private float hitAggroTimer = 0f;

    [Header("Attack Player")]
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackDamage = 1f;
    [SerializeField] private float attackKnockbackForce = 6f;
    [SerializeField] private float attackWindup = 0.2f;
    [SerializeField] private GameObject attackHitBox; // GameObject hitbox riêng của Enemy
    private EnemyHitBox hitBoxScript;
    private bool isAttacking = false;
    private float lastAttackTime = -999f;

    [Header("Health")]
    [SerializeField] private float maxHealth = 20f;
    private float currentHealth;
    private bool isDead = false;

    [Header("Death")]
    [SerializeField] private string deathAnimStateName = "Die";
    [SerializeField] private float deathAnimDuration = 1f;

    [Header("Knockback")]
    [SerializeField] private float knockbackDuration = 0.2f;
    private bool isKnockedBack = false;
    private float knockbackTimer = 0f;
    private Coroutine knockbackRoutine;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkBoolParam = "IsWalking";

    [Header("SFX")]
    [SerializeField] private AudioSource audioSource;          // nếu để trống sẽ tự GetComponent/AddComponent
    [SerializeField] private AudioClip[] moveSfxClips;         // tiếng bước đi / trườn, phát lặp lại theo interval
    [SerializeField] private float moveSfxInterval = 0.4f;     // khoảng cách giữa 2 lần phát âm thanh di chuyển
    [SerializeField] private AudioClip[] hurtSfxClips;         // phát khi bị đánh trúng / knockback
    [SerializeField] private AudioClip[] attackSfxClips;       // phát khi bắt đầu ra đòn (lúc windup)
    [SerializeField] private AudioClip dieSfxClip;             // phát khi chết
    [SerializeField][Range(0f, 1f)] private float sfxVolume = 1f;

    [Header("SFX - Khoảng cách nghe")]
    [SerializeField] private bool limitSfxByDistance = true;   // bật để chỉ nghe khi player ở gần
    [SerializeField] private float sfxMinDistance = 1.5f;      // trong khoảng này nghe full volume
    [SerializeField] private float sfxMaxDistance = 6f;        // ngoài khoảng này không nghe thấy nữa
    [SerializeField] private AudioRolloffMode sfxRolloffMode = AudioRolloffMode.Linear;

    private float moveSfxTimer = 0f;

    private Rigidbody2D rb;
    private int direction = 1;          // 1 = phải, -1 = trái
    private float distanceTarget;       // quãng đường cần đi trong lượt hiện tại
    private float distanceMoved;        // quãng đường đã đi được
    private bool isPausing;
    private float pauseTimer;
    private bool isGrounded;
    private bool isWalking;
    private bool isChasing = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (animator == null)
            animator = GetComponent<Animator>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        if (limitSfxByDistance)
        {
            // spatialBlend = 1 -> âm thanh 3D, tự nhỏ dần theo khoảng cách tới AudioListener
            // (AudioListener thường gắn trên Player hoặc Camera bám theo Player)
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = sfxRolloffMode;
            audioSource.minDistance = sfxMinDistance;
            audioSource.maxDistance = sfxMaxDistance;
        }
        else
        {
            audioSource.spatialBlend = 0f; // 2D, nghe rõ mọi lúc
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
        PickNewDistance();
        FlipVisual();

        if (attackHitBox != null)
        {
            attackHitBox.SetActive(false);
            hitBoxScript = attackHitBox.GetComponent<EnemyHitBox>();
        }
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        CheckGrounded();

        // ----- Knockback: khoá toàn bộ AI -----
        if (isKnockedBack)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            if (knockbackTimer <= 0f)
                isKnockedBack = false;

            SetWalking(false);
            return;
        }

        // ----- Đang tự ra đòn: đứng yên -----
        if (isAttacking)
        {
            SetWalking(false);
            return;
        }

        if (!isGrounded)
        {
            SetWalking(false);
            Vector2 fallPos = rb.position + Vector2.down * fallSpeed * Time.fixedDeltaTime;
            rb.MovePosition(fallPos);
            return;
        }

        // ----- Phát hiện Player -----
        bool playerDetected = DetectPlayerByRaycast() || DetectPlayerBehind();

        if (hitAggroTimer > 0f)
        {
            hitAggroTimer -= Time.fixedDeltaTime;
            playerDetected = true;
        }

        if (playerDetected)
        {
            isChasing = true;
        }
        else if (isChasing)
        {
            isChasing = false;
            isPausing = false;
            PickNewDistance();
        }

        if (isChasing)
        {
            HandleChaseAndAttack();
            return;
        }

        // ----- Không thấy Player -> patrol như cũ -----
        if (isPausing)
        {
            SetWalking(false);
            HandlePause();
            return;
        }

        HandleMovement();
    }

    // ---------------- CHASE + ATTACK ----------------

    private void HandleChaseAndAttack()
    {
        if (player == null) return;

        float distToPlayer = Mathf.Abs(player.position.x - transform.position.x);

        if (distToPlayer <= attackRange)
        {
            // Đã vào tầm đánh -> đứng yên
            SetWalking(false);

            float dir = player.position.x - transform.position.x >= 0f ? 1f : -1f;
            int newDir = dir > 0f ? 1 : -1;
            if (newDir != direction)
            {
                direction = newDir;
                FlipVisual();
            }

            if (Time.time - lastAttackTime >= attackCooldown)
            {
                StartCoroutine(DoAttackPlayer());
            }
        }
        else
        {
            // Đuổi theo Player
            float dir = player.position.x - transform.position.x >= 0f ? 1f : -1f;
            int newDir = dir > 0f ? 1 : -1;
            if (newDir != direction)
            {
                direction = newDir;
                FlipVisual();
            }

            SetWalking(true);
            Vector2 moveDelta = new Vector2(dir, 0f) * chaseSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + moveDelta);
            distanceMoved += moveDelta.magnitude;
        }
    }

    private IEnumerator DoAttackPlayer()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        SetWalking(false);

        if (animator != null)
            animator.SetTrigger("Attack");

        PlayAttackSfx();

        yield return new WaitForSeconds(attackWindup);

        if (attackHitBox != null)
        {
            attackHitBox.SetActive(true);

            if (hitBoxScript != null)
                hitBoxScript.Setup(direction > 0, attackDamage, attackKnockbackForce);
        }

        yield return new WaitForSeconds(0.1f);

        if (attackHitBox != null)
            attackHitBox.SetActive(false);

        isAttacking = false;
    }

    // ---------------- DETECTION ----------------

    private bool DetectPlayerByRaycast()
    {
        if (player == null) return false;

        Vector2 origin = transform.position;
        float dir = direction;
        float range = isChasing ? detectRange + loseSightExtraRange : detectRange;

        RaycastHit2D hit = Physics2D.Raycast(origin, new Vector2(dir, 0f), range, obstacleLayer | playerLayer);

        return hit.collider != null && ((1 << hit.collider.gameObject.layer) & playerLayer) != 0;
    }

    private bool DetectPlayerBehind()
    {
        if (player == null) return false;

        Vector2 origin = transform.position;
        float dir = -direction; // ngược hướng đang quay

        RaycastHit2D hit = Physics2D.Raycast(origin, new Vector2(dir, 0f), behindDetectRange, obstacleLayer | playerLayer);

        return hit.collider != null && ((1 << hit.collider.gameObject.layer) & playerLayer) != 0;
    }

    // ---------------- HEALTH / DAMAGE ----------------

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

    public void ApplyKnockback(float dir, float force, float upForce = 0f, float damage = 0f)
    {
        if (isDead) return;

        if (damage > 0f)
            TakeDamage(damage);

        if (isDead) return;

        isKnockedBack = true;
        knockbackTimer = knockbackDuration;

        // Player đánh trúng -> ngay lập tức chuyển sang trạng thái đuổi theo
        isChasing = true;
        hitAggroTimer = hitAggroDuration;

        if (animator != null)
            animator.SetTrigger("Hurt");

        PlayHurtSfx();

        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);

        knockbackRoutine = StartCoroutine(KnockbackRoutine(dir, force));
    }

    private IEnumerator KnockbackRoutine(float dir, float force)
    {
        float elapsed = 0f;
        Vector2 startPos = rb.position;
        float targetX = startPos.x + dir * force * knockbackDuration;

        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / knockbackDuration);
            float x = Mathf.Lerp(startPos.x, targetX, t);

            rb.MovePosition(new Vector2(x, rb.position.y));
            yield return null;
        }

        knockbackRoutine = null;
    }

    private void Die()
    {
        isDead = true;
        isKnockedBack = false;
        isAttacking = false;

        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);

        if (attackHitBox != null)
            attackHitBox.SetActive(false);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (animator != null)
        {
            animator.ResetTrigger("Hurt");
            animator.ResetTrigger("Attack");
        }

        PlayDieSfx();
        PlayDeathAnimation();
        Destroy(gameObject, deathAnimDuration);
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

    // ---------------- PATROL (giữ nguyên logic cũ) ----------------

    private void CheckGrounded()
    {
        RaycastHit2D hit = Physics2D.Raycast(
            groundCheckPoint.position,
            Vector2.down,
            groundCheckDistance
        );

        isGrounded = hit.collider != null && hit.collider.CompareTag(groundTag);

        Debug.DrawRay(groundCheckPoint.position, Vector2.down * groundCheckDistance,
            isGrounded ? Color.green : Color.red);
    }

    private void HandlePause()
    {
        pauseTimer -= Time.fixedDeltaTime;
        if (pauseTimer <= 0f)
        {
            isPausing = false;
            direction *= -1;
            PickNewDistance();
            FlipVisual();
        }
    }

    private void HandleMovement()
    {
        Vector2 rayDir = direction == 1 ? Vector2.right : Vector2.left;
        RaycastHit2D wallHit = Physics2D.Raycast(
            wallCheckPoint.position,
            rayDir,
            wallCheckDistance
        );

        Debug.DrawRay(wallCheckPoint.position, rayDir * wallCheckDistance, Color.yellow);

        bool hitWall = wallHit.collider != null && wallHit.collider.CompareTag(groundTag);

        if (hitWall || distanceMoved >= distanceTarget)
        {
            SetWalking(false);
            StartPause();
            return;
        }

        SetWalking(true);
        Vector2 moveDelta = rayDir * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + moveDelta);
        distanceMoved += moveDelta.magnitude;
    }

    private void SetWalking(bool value)
    {
        if (isWalking == value)
        {
            // Vẫn đang đi -> tiếp tục đếm giờ để phát SFX di chuyển theo interval
            if (isWalking)
                UpdateMoveSfx();
            return;
        }

        isWalking = value;

        if (animator != null)
            animator.SetBool(walkBoolParam, isWalking);

        if (isWalking)
        {
            // Bắt đầu đi -> phát ngay 1 tiếng bước rồi reset timer
            moveSfxTimer = 0f;
            UpdateMoveSfx();
        }
    }

    private void StartPause()
    {
        isPausing = true;
        pauseTimer = Random.Range(minPauseTime, maxPauseTime);
    }

    private void PickNewDistance()
    {
        distanceTarget = Random.Range(minMoveDistance, maxMoveDistance);
        distanceMoved = 0f;
    }

    private void FlipVisual()
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * direction;
        transform.localScale = scale;
    }

    // ---------------- SFX ----------------

    private void UpdateMoveSfx()
    {
        moveSfxTimer -= Time.fixedDeltaTime;
        if (moveSfxTimer > 0f) return;

        moveSfxTimer = moveSfxInterval;
        PlayRandomClip(moveSfxClips);
    }

    private void PlayAttackSfx()
    {
        PlayRandomClip(attackSfxClips);
    }

    private void PlayHurtSfx()
    {
        PlayRandomClip(hurtSfxClips);
    }

    private void PlayDieSfx()
    {
        if (dieSfxClip == null || audioSource == null) return;
        // Dùng PlayOneShot để tiếng chết vẫn kêu hết dù object bị Destroy sau đó không lâu
        audioSource.PlayOneShot(dieSfxClip, sfxVolume);
    }

    private void PlayRandomClip(AudioClip[] clips)
    {
        if (audioSource == null || clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;

        audioSource.PlayOneShot(clip, sfxVolume);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
            Gizmos.DrawLine(groundCheckPoint.position, groundCheckPoint.position + Vector3.down * groundCheckDistance);

        if (wallCheckPoint != null)
        {
            Gizmos.DrawLine(wallCheckPoint.position, wallCheckPoint.position + Vector3.right * wallCheckDistance);
            Gizmos.DrawLine(wallCheckPoint.position, wallCheckPoint.position + Vector3.left * wallCheckDistance);
        }

        Gizmos.color = Color.red;
        Vector3 origin = transform.position;
        Gizmos.DrawLine(origin, origin + new Vector3(direction * detectRange, 0f, 0f));

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawLine(origin, origin + new Vector3(-direction * behindDetectRange, 0f, 0f));

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}