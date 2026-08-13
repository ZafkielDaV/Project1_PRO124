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

        if (attackHitBox != null)
        {
            attackHitBox.SetActive(false);
            hitBoxScript = attackHitBox.GetComponent<AttackHitBox>();
        }

        currentHealth = maxHealth;
        if (healthBar != null)
            healthBar.SetMaxHealth(maxHealth);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        bool shouldUse = GameManager.Instance != null && GameManager.Instance.ShouldUseSavePoint();
        Debug.Log($"[Player] Start() shouldUseSavePoint={shouldUse}, HasSaveData={PlayerPrefs.GetInt("HasSaveData", 0)}");
        if (shouldUse)
        {
            SavePoint.LoadLastPosition(transform);
        }

        // ---- SỬA: chỉ load vị trí save point nếu GameManager cho phép ----
        if (GameManager.Instance != null && GameManager.Instance.ShouldUseSavePoint())
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

    public void TakeDamage(float damage, float attackerDirection, float knockback)
    {
        if (isHurt || isAttacking || isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (healthBar != null)
            healthBar.SetHealth(currentHealth);

        animator.SetTrigger("Hurt");

        rb.linearVelocity = new Vector2(attackerDirection * knockback, rb.linearVelocity.y);

        StartCoroutine(HurtLock());

        if (currentHealth <= 0f)
        {
            Die();
        }
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