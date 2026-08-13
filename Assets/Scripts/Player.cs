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

    [Header("Hurt")]
    public float hurtLockDuration = 0.2f;

    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;
    public HealthBar healthBar;

    [Header("Mana")]
    public float maxMana = 50f;
    public float currentMana;
    public HealthBar manaBar; // tái dùng script HealthBar, hoặc thay bằng script ManaBar riêng nếu có

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

        // Chỉ load vị trí save point nếu GameManager cho phép (đã gộp, bỏ đoạn gọi trùng)
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

        if (!isAttacking && !isHurt)
        {
            float moveInput = Input.GetAxis("Horizontal");
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

            animator.SetFloat("Speed", Mathf.Abs(moveInput));

            if (moveInput > 0 && !facingRight) Flip();
            else if (moveInput < 0 && facingRight) Flip();
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isAttacking && !isHurt)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            animator.SetBool("isJumping", true);
        }

        animator.SetBool("isFalling", !isGrounded && rb.linearVelocity.y < -1f);
        animator.SetBool("isGrounded", isGrounded);

        if (Input.GetKeyDown(KeyCode.J) && !isAttacking && !isHurt && isGrounded)
        {
            StartCoroutine(DoAttack());
        }
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
            animator.SetBool("isJumping", false);
            animator.SetBool("isFalling", false);
            animator.SetBool("isGrounded", true);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
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

    // Chữ ký cũ (giữ tương thích ngược cho AttackHitBox và các nơi khác đang gọi):
    // chỉ đẩy theo trục ngang, KHÔNG đụng tới vận tốc Y (rơi/nhảy vẫn giữ nguyên).
    public void TakeDamage(float damage, float attackerDirection, float knockback)
    {

        TakeDamage(damage, new Vector2(attackerDirection, 0f), knockback);
    }

    // Chữ ký mới: nhận hướng đẩy dạng Vector2.
    // Trục nào của knockbackDir khác 0 thì trục đó của vận tốc bị ghi đè theo dấu của nó;
    // trục nào = 0 thì giữ nguyên vận tốc hiện tại (trap chỉ đẩy Y thì X không đổi).

    public void TakeDamage(float damage, Vector2 knockbackDir, float knockback)
    {
        if (isHurt || isAttacking || isDead) return;

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

    // Gọi hàm này khi nhặt bình máu / item hồi máu
    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (healthBar != null)
            healthBar.SetHealth(currentHealth);
    }

    // Gọi hàm này khi nhặt bình mana / item hồi mana
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

    // Gắn hàm này vào nút "Restart" trên Game Over UI
    // -> reload scene KHÔNG lấy save point (xóa hẳn save data)


    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }
}