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
    public float knockbackForce = 8f; // lực đẩy lùi enemy khi trúng đòn

    [Header("Hurt")]
    public float hurtLockDuration = 0.2f; // thời gian bị khóa di chuyển/attack sau khi trúng đòn

    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;
    public HealthBar healthBar; // kéo thả HealthBar UI vào đây trong Inspector

    [Header("Death")]
    public float deathAnimDuration = 0.5f;   // thời lượng animation Die (khớp 12 sprite @ 24fps)
    public float deathTotalDuration = 1f;    // tổng thời gian đóng băng trước khi xử lý bước tiếp theo (vd: load lại scene)
    public GameObject gameOverPanel;         // kéo thả Panel Game Over (UI) vào đây, để inactive sẵn trong scene

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

        // Khởi tạo máu
        currentHealth = maxHealth;
        if (healthBar != null)
            healthBar.SetMaxHealth(maxHealth);

        // Đảm bảo Game Over panel tắt khi bắt đầu
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // ---- THÊM DÒNG NÀY: khôi phục vị trí về save point gần nhất ----
        SavePoint.LoadLastPosition(transform);
    }

    void Update()
    {
        if (isDead) return; // không cho làm gì nữa nếu đã chết

        // ---------------- DIALOGUE LOCK ----------------
        if (DialogueManager.IsDialogueActive)
        {
            // Đứng yên hoàn toàn trong lúc dialogue đang mở
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            animator.SetFloat("Speed", 0f);

            // Bấm J: đóng dialogue lại, đồng thời KHÔNG cho tấn công trong frame này
            if (Input.GetKeyDown(KeyCode.J))
            {
                DialogueManager.Instance.CloseDialogueUI();
            }

            return; // chặn toàn bộ input di chuyển/nhảy/tấn công phía dưới khi dialogue còn active
        }

        if (!isAttacking && !isHurt) // chỉ cho di chuyển khi không attack và không đang hurt
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

        // Chỉ nhận phím tấn công khi: KHÔNG đang attack, KHÔNG đang hurt, VÀ đang đứng trên đất
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

    // ---------------- HURT / TAKE DAMAGE ----------------

    // Được Enemy gọi khi tấn công trúng Player
    public void TakeDamage(float damage, float attackerDirection, float knockback)
    {
        if (isHurt || isAttacking || isDead) return; // tránh dính đòn liên tục / chồng animation Attack

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

    // ---------------- DEATH ----------------

    private void Die()
    {
        isDead = true;
        isHurt = true; // khóa mọi hành động khác

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
    public void RestartLevel()
    {
        Time.timeScale = 1f; // reset time trước khi load lại scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Gắn hàm này vào nút "Quit" trên Game Over UI (nếu cần)
    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }
}