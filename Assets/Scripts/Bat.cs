using UnityEngine;
using System.Collections;

public class Bat : MonoBehaviour
{
        public float moveSpeed = 2f;
    public float chaseSpeed = 3f;
    public float moveRange = 3f;
    public Animator animator;
    public float detectDistance = 6f;
    public LayerMask playerLayer;
    public Transform player;

    public GameObject attackHitBox; // gán trong Inspector (child của Enemy)

    private Vector2 startPos;
    private Vector2 targetPos;
    private bool isMoving = false;
    private bool isAttacking = false;
    private SpriteRenderer spriteRenderer;

    private bool lastAttackWasAt1 = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPos = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
        SetRandomTarget();

        if (attackHitBox != null)
            attackHitBox.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
          if (!isAttacking)
        {
            if (isMoving)
                Patrol();
            else
                ChasePlayer();
        }

        DetectPlayer();

        // Flip Attack_HitBox theo Enemy
        if (attackHitBox != null)
        {
            Vector3 scale = attackHitBox.transform.localScale;
            scale.x = spriteRenderer.flipX ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            attackHitBox.transform.localScale = scale;
        }
    }
      void Patrol()
    {
        transform.position = Vector2.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
        animator.SetBool("isWalking", true);

        if (targetPos.x > transform.position.x) spriteRenderer.flipX = false;
        else if (targetPos.x < transform.position.x) spriteRenderer.flipX = true;

        if (Vector2.Distance(transform.position, targetPos) < 0.1f)
        {
            isMoving = false;
            animator.SetBool("isWalking", false);
            StartCoroutine(WaitAndMove());
        }
    }

    void SetRandomTarget()
    {
        float randX = Random.Range(-moveRange, moveRange);
        targetPos = new Vector2(startPos.x + randX, startPos.y);
        isMoving = true;
    }

    IEnumerator WaitAndMove()
    {
        yield return new WaitForSeconds(Random.Range(1f, 3f));
        SetRandomTarget();
    }

    void DetectPlayer()
    {
        Vector2 direction = spriteRenderer.flipX ? Vector2.left : Vector2.right;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, detectDistance, playerLayer);
        Debug.DrawRay(transform.position, direction * detectDistance, Color.red);

        if (hit.collider != null)
        {
            float distance = Vector2.Distance(transform.position, hit.collider.transform.position);

            if (player.position.x > transform.position.x) spriteRenderer.flipX = false;
            else spriteRenderer.flipX = true;

            if (distance <= 2.3f && !isAttacking)
            {
                if (lastAttackWasAt1)
                {
                    StartCoroutine(PlayAttack("At2"));
                    lastAttackWasAt1 = false;
                }
                else
                {
                    StartCoroutine(PlayAttack("At1"));
                    lastAttackWasAt1 = true;
                }
            }
        }
        else
        {
            isAttacking = false;
            animator.SetBool("At1", false);
            animator.SetBool("At2", false);
        }
    }

    IEnumerator PlayAttack(string attackBool)
    {
        isAttacking = true;

        animator.SetBool("At1", false);
        animator.SetBool("At2", false);

        animator.SetBool(attackBool, true);
        Debug.Log("Enemy dùng " + attackBool);

        // Bật HitBox
        if (attackHitBox != null)
            attackHitBox.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        animator.SetBool(attackBool, false);

        // Tắt HitBox sau khi đánh
        if (attackHitBox != null)
            attackHitBox.SetActive(false);

        yield return new WaitForSeconds(0.3f);

        isAttacking = false;
    }

    void ChasePlayer()
    {
        if (player != null && !isAttacking)
        {
            float distance = Vector2.Distance(transform.position, player.position);
            if (distance <= detectDistance && distance > 2f)
            {
                Vector2 newPos = new Vector2(
                    Mathf.MoveTowards(transform.position.x, player.position.x, chaseSpeed * Time.deltaTime),
                    transform.position.y
                );
                transform.position = newPos;

                if (player.position.x > transform.position.x) spriteRenderer.flipX = false;
                else spriteRenderer.flipX = true;

                animator.SetBool("isWalking", true);
            }
            else
            {
                animator.SetBool("isWalking", false);
            }
        }
    }
}

