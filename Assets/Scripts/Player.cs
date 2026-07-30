using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public GameObject attackHitBox;
    public float attackDuration = 0.1f;
    public float comboResetTime = 1f;
    public int damage = 1;
    public float knockbackForce = 5f;

    private Rigidbody2D rb;
    private bool isGrounded;
    private Animator animator;
    private bool facingRight = true;

    private int comboStep = 0;
    private float lastAttackTime = 0f;
    private bool isAttacking = false;   // chống spam

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (attackHitBox != null)
            attackHitBox.SetActive(false);
    }

    void Update()
    {
        if (!isAttacking) // chỉ cho di chuyển khi không attack
        {
            float moveInput = Input.GetAxis("Horizontal");
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

            animator.SetFloat("Speed", Mathf.Abs(moveInput));

            if (moveInput > 0 && !facingRight) Flip();
            else if (moveInput < 0 && facingRight) Flip();
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isAttacking)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            animator.SetBool("isJumping", true);
        }

        animator.SetBool("isFalling", !isGrounded && rb.linearVelocity.y < -1f);
        animator.SetBool("isGrounded", isGrounded);

        // Attack combo bằng phím J
        if (Input.GetKeyDown(KeyCode.J) && !isAttacking)
        {
            if (Time.time - lastAttackTime > comboResetTime)
                comboStep = 0;

            comboStep++;
            if (comboStep > 3) comboStep = 1;

            lastAttackTime = Time.time;

            animator.SetFloat("AttackStep", comboStep);
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

        // nhích lên một đoạn nhỏ
        transform.position += new Vector3(0.15f, 0f, 0f);

        // khóa di chuyển ngang
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (attackHitBox != null)
            attackHitBox.SetActive(true);

        yield return new WaitForSeconds(attackDuration);

        if (attackHitBox != null)
            attackHitBox.SetActive(false);

        animator.SetFloat("AttackStep", 0);

        isAttacking = false;
    }
}
