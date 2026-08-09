using UnityEngine;
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
    public float deathTotalDuration = 0f;    // tổng thời gian đóng băng trước khi xử lý bước tiếp theo (vd: load lại scene)

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
    }

    void Update()
    {
        if (isDead) return; // không cho làm gì nữa nếu đã chết

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
        // Cho Animator của Player chạy bằng Unscaled Time để animation "Die"
        // vẫn tiếp tục phát dù Time.timeScale = 0 (mọi thứ khác sẽ đứng yên)
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;

        // Đóng băng toàn bộ scene: Rigidbody2D (vật lý), enemy, và mọi script
        // khác dùng Time.deltaTime sẽ tự động dừng lại
        Time.timeScale = 0f;

        // Làm chậm Animator lại để clip Die (vốn dài deathAnimDuration giây,
        // vd 0.5s @ 12 sprite/24fps) được kéo dãn ra và chạy trọn trong
        // đúng deathTotalDuration giây (vd 10s) — hiệu ứng slow-motion khi chết
        animator.speed = deathAnimDuration / deathTotalDuration;

        // WaitForSecondsRealtime KHÔNG bị ảnh hưởng bởi Time.timeScale
        // nên vẫn đếm đúng thời gian thực deathTotalDuration
        yield return new WaitForSecondsRealtime(deathTotalDuration);

        // Dừng hẳn Animator lại đúng lúc này để giữ nguyên sprite cuối cùng
        // của animation Die (không cho animation loop hay chạy tiếp)
        animator.speed = 0f;

        // KHÔNG trả Time.timeScale về 1 nữa — giữ nguyên 0 vĩnh viễn,
        // toàn bộ scene (vật lý, enemy, input di chuyển...) đứng im luôn
        // cho đến khi có hành động khác (ví dụ load lại scene / về menu)

        // TODO: xử lý thêm khi cần, ví dụ:
        // - Disable collider của Player
        // - Hiện UI Game Over (UI thường không phụ thuộc timeScale nên vẫn hiện được)
        // - Nút Restart trong UI đó gọi SceneManager.LoadScene(...) để chơi lại
    }
}