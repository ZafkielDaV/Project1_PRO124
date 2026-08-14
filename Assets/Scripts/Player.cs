using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Player : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 7f;

    [Header("Attack")]
    public GameObject attackHitBox;
    public float attackDuration = 0.3f;
    public float attackMoveDistance = 0.5f;
    public float knockbackForce = 8f;

    [Header("Dash Skill")]
    public KeyCode dashKey = KeyCode.LeftShift;
    public float dashSpeed = 18f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    public float dashManaCost = 15f;
    public float dashKnockback = 10f;
    public bool dashIgnoresGravity = true;
    public bool invincibleDuringDash = true;

    private bool isDashing = false;
    private float lastDashTime = -999f;

    [Header("Hurt")]
    public float hurtLockDuration = 0.2f;

    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;
    public HealthBar healthBar;

    [Header("Mana")]
    public float maxMana = 50f;
    public float currentMana;
    public ManaBar manaBar; // thay vì public HealthBar manaBar;
    [Header("Mana Regen")]
    public bool autoRegenMana = true;
    public float manaRegenRate = 5f;      // mana hồi mỗi giây
    public float manaRegenDelay = 1.5f;   // thời gian chờ sau khi dùng skill mới bắt đầu hồi

    private float lastManaUseTime = -999f;

    [Header("Death")]
    public float deathAnimDuration = 0.5f;
    public float deathTotalDuration = 1f;
    public GameObject gameOverPanel;

    private Rigidbody2D rb;
    private bool isGrounded;
    private Animator animator;
    private bool facingRight = true;

    private bool isAttacking = false;
    private AttackHitBox hitBoxScript;

    private bool isHurt = false;
    private bool isDead = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (rb == null)
            Debug.LogError("[Player] Thiếu Rigidbody2D trên GameObject!");
        if (animator == null)
            Debug.LogError("[Player] Thiếu Animator trên GameObject!");

        if (attackHitBox != null)
        {
            attackHitBox.SetActive(false);
            hitBoxScript = attackHitBox.GetComponent<AttackHitBox>();
        }

        currentHealth = maxHealth;
        if (healthBar != null)
            healthBar.SetMaxHealth(maxHealth);

        currentMana = maxMana;
        if (manaBar != null)
            manaBar.SetMaxHealth(maxMana);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        bool shouldUse = GameManager.Instance != null && GameManager.Instance.ShouldUseSavePoint();
        Debug.Log($"[Player] Start() shouldUseSavePoint={shouldUse}, HasSaveData={PlayerPrefs.GetInt("HasSaveData", 0)}");
        if (shouldUse)
        {
            SavePoint.LoadLastPosition(transform);
        }
    }

    void Update()
    {

        if (isDead) return;

        if (DialogueManager.IsDialogueActive)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            animator.SetFloat("Speed", 0f);

            if (Input.GetKeyDown(KeyCode.J))
            {
                DialogueManager.Instance.CloseDialogueUI();
            }

            return;
        }

        if (!isAttacking && !isHurt && !isDashing)
        {
            float moveInput = Input.GetAxis("Horizontal");
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

            animator.SetFloat("Speed", Mathf.Abs(moveInput));

            if (moveInput > 0 && !facingRight) Flip();
            else if (moveInput < 0 && facingRight) Flip();
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isAttacking && !isHurt && !isDashing)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            animator.SetBool("isJumping", true);
        }

        // [DASH] Không cho Update ghi đè isJumping/isFalling/isGrounded trong lúc đang lướt
        if (!isDashing)
        {
            animator.SetBool("isFalling", !isGrounded && rb.linearVelocity.y < -1f);
            animator.SetBool("isGrounded", isGrounded);
        }

        if (Input.GetKeyDown(KeyCode.J) && !isAttacking && !isHurt && !isDashing && isGrounded)
        {
            StartCoroutine(DoAttack());
        }

        if (Input.GetKeyDown(dashKey)
            && !isAttacking && !isHurt && !isDashing
            && Time.time - lastDashTime >= dashCooldown
            && currentMana >= dashManaCost)
        {
            StartCoroutine(DoDash());
        }
        if (autoRegenMana && !isDead)
        {
            RegenManaOverTime();
        }
    }
    private void RegenManaOverTime()
    {
        if (currentMana >= maxMana) return;
        if (Time.time - lastManaUseTime < manaRegenDelay) return;

        currentMana += manaRegenRate * Time.deltaTime;
        currentMana = Mathf.Clamp(currentMana, 0f, maxMana);

        if (manaBar != null)
            manaBar.SetHealth(currentMana);
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scaler = transform.localScale;
        scaler.x *= -1;
        transform.localScale = scaler;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;

            // [DASH] Không đụng vào animator bool trong lúc đang lướt
            if (!isDashing)
            {
                animator.SetBool("isJumping", false);
                animator.SetBool("isFalling", false);
                animator.SetBool("isGrounded", true);
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;

            if (!isDashing)
                animator.SetBool("isGrounded", false);
        }
    }

    IEnumerator DoAttack()
    {
        isAttacking = true;

        animator.SetTrigger("Attack");

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        transform.position += new Vector3(facingRight ? attackMoveDistance : -attackMoveDistance, 0f, 0f);

        if (attackHitBox != null)
        {
            attackHitBox.SetActive(true);

            if (hitBoxScript != null)
                hitBoxScript.Setup(facingRight, knockbackForce);
        }

        yield return new WaitForSeconds(attackDuration);

        if (attackHitBox != null)
            attackHitBox.SetActive(false);

        isAttacking = false;
    }

    // [DASH] Coroutine skill lướt: giữ sprite cuối cho tới khi hết dashDuration, tắt Jump/Fall trong lúc lướt
    // [DASH COOLDOWN] Cho UI bên ngoài đọc trạng thái hồi chiêu
    public float DashCooldownRemaining => Mathf.Max(0f, dashCooldown - (Time.time - lastDashTime));
    public float DashCooldownPercent01 => dashCooldown <= 0f ? 1f : Mathf.Clamp01(1f - (DashCooldownRemaining / dashCooldown));
    public bool IsDashReady => DashCooldownRemaining <= 0f && currentMana >= dashManaCost;
    IEnumerator DoDash()
    {
        isDashing = true;
        lastDashTime = Time.time;

        currentMana -= dashManaCost;
        currentMana = Mathf.Clamp(currentMana, 0f, maxMana);
        Debug.Log($"[Mana] currentMana = {currentMana}, manaBar null? {manaBar == null}"); // thêm dòng này
        lastManaUseTime = Time.time;   // thêm dòng này
        if (manaBar != null)
            manaBar.SetHealth(currentMana); 

        // Tắt hẳn Jump/Fall để không đè lên Dash
        animator.SetBool("isJumping", false);
        animator.SetBool("isFalling", false);

        animator.speed = 1f;
        animator.SetTrigger("Dash");

        float dashDir = facingRight ? 1f : -1f;

        float originalGravity = rb.gravityScale;
        if (dashIgnoresGravity)
            rb.gravityScale = 0f;

        if (attackHitBox != null)
        {
            attackHitBox.SetActive(true);
            if (hitBoxScript != null)
                hitBoxScript.Setup(facingRight, dashKnockback);
        }

        // Đợi 1 frame để Animator chuyển hẳn sang state Dash trước khi đọc độ dài animation
        yield return null;

        float animLength = GetCurrentStateLength();
        bool frozen = false;
        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            rb.linearVelocity = new Vector2(
                dashDir * dashSpeed,
                dashIgnoresGravity ? 0f : rb.linearVelocity.y
            );

            // Animation Dash đã chạy hết nhưng dash vẫn còn thời gian -> đóng băng ở frame cuối
            if (!frozen && elapsed >= animLength)
            {
                animator.speed = 0f;
                frozen = true;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        animator.speed = 1f; // trả tốc độ animator về bình thường

        if (attackHitBox != null)
            attackHitBox.SetActive(false);

        if (dashIgnoresGravity)
            rb.gravityScale = originalGravity;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        isDashing = false;
    }

    // [DASH] Lấy độ dài (giây) của state animation đang chạy trên layer 0 (dùng để biết khi nào Dash anim đã hết)
    private float GetCurrentStateLength()
    {
        if (animator == null) return dashDuration;
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        return info.length;
    }

    public void TakeDamage(float damage, float attackerDirection, float knockback)
    {
        TakeDamage(damage, new Vector2(attackerDirection, 0f), knockback);
    }

    public void TakeDamage(float damage, Vector2 knockbackDir, float knockback)
    {
        if (isHurt || isAttacking || isDead) return;
        if (isDashing && invincibleDuringDash) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (healthBar != null)
            healthBar.SetHealth(currentHealth);

        animator.SetTrigger("Hurt");

        Vector2 newVelocity = rb.linearVelocity;
        if (knockbackDir.x != 0f) newVelocity.x = Mathf.Sign(knockbackDir.x) * knockback;
        if (knockbackDir.y != 0f) newVelocity.y = Mathf.Sign(knockbackDir.y) * knockback;
        rb.linearVelocity = newVelocity;

        StartCoroutine(HurtLock());

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (healthBar != null)
            healthBar.SetHealth(currentHealth);
    }

    public void RestoreMana(float amount)
    {
        if (isDead) return;

        currentMana += amount;
        currentMana = Mathf.Clamp(currentMana, 0f, maxMana);

        if (manaBar != null)
            manaBar.SetHealth(currentMana);
    }

    IEnumerator HurtLock()
    {
        isHurt = true;
        yield return new WaitForSeconds(hurtLockDuration);
        isHurt = false;
    }

    private void Die()
    {
        isDead = true;
        isHurt = true;

        rb.linearVelocity = Vector2.zero;
        animator.SetTrigger("Die");

        StartCoroutine(DeathFreezeSequence());
    }

    IEnumerator DeathFreezeSequence()
    {
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        Time.timeScale = 0f;
        animator.speed = deathAnimDuration / deathTotalDuration;
        yield return new WaitForSecondsRealtime(deathTotalDuration);
        animator.speed = 0f;
        ShowGameOver();
    }

    private void ShowGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }
}