using UnityEngine;
using UnityEngine.UI;

public class CloneController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float chaseRadius = 3f;
    public float moveSpeed = 2f;
    public float maxChaseDistance = 10f; // Khoảng cách tối đa để đuổi theo player

    [Header("Attack Settings")]
    public Transform attackPoint1, attackPoint2;
    public GameObject attackEffect1, attackEffect2;
    [SerializeField] private float attackDelay = 2f; // GIẢM TỪ 10F XUỐNG 2F
    private float attackRange = 2f; // Tầm tấn công

    [Header("Health")]
    public int maxHealth = 50;
    public Slider healthBar;
    public int currentHealth;

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundLayer;

    [Header("References")]
    public Transform player;
    public ObjectPoolingClone pooling;
    public CloneHitBox cloneHitBox;
    public CloneHurtBox cloneHurtBox;

    // Private variables
    private Vector3 spawnPosition;
    private bool isChasing = false;
    private bool isDie = false;
    private bool isGrounded = false;
    private bool isReturningHome = false;

    // Patrol logic
    private int moveDirection = -1; // -1: left, 1: right (default facing left)
    private bool isIdle = false;
    private float idleTimer = 0f;
    private float idleDuration = 2f;

    // Attack logic
    private float lastAttackTime = 0f;
    private int attackStep = 1; // 1: attack1, 2: attack2
    private bool isInAttackRange = false;
    private bool isCurrentlyAttacking = false; // THÊM BIẾN MỚI

    // Components
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    void Start()
    {
        currentHealth = maxHealth;
        spawnPosition = transform.position;
        
        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null)
                Debug.LogError("CloneController: Không tìm thấy Player trong scene!");
        }

        // Tự động lấy components
        if (cloneHitBox == null)
            cloneHitBox = GetComponentInChildren<CloneHitBox>();
        if (cloneHurtBox == null)
            cloneHurtBox = GetComponentInChildren<CloneHurtBox>();

        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (isDie || player == null) return;

        // Check if grounded
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);
        animator.SetBool("IsGround", isGrounded);

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        float distanceToSpawn = Vector2.Distance(transform.position, spawnPosition);

        // Logic chính
        if (distanceToPlayer > maxChaseDistance || isReturningHome)
        {
            ReturnToSpawn();
        }
        else if (distanceToPlayer < chaseRadius)
        {
            isChasing = true;
            isReturningHome = false;
            ChasePlayer(distanceToPlayer);
        }
        else
        {
            isChasing = false;
            isReturningHome = false;
            PatrolAroundSpawn();
        }
    }

    void ChasePlayer(float distanceToPlayer)
    {
        Vector3 direction = (player.position - transform.position).normalized;
        float moveX = direction.x;

        // Di chuyển nếu player xa hơn tầm tấn công
        if (distanceToPlayer > attackRange)
        {
            transform.position += direction * moveSpeed * Time.deltaTime;
            animator.SetFloat("Speed", Mathf.Abs(moveX));
            isInAttackRange = false;
            isCurrentlyAttacking = false; // Reset khi ra khỏi tầm
        }
        else
        {
            // Trong tầm tấn công - dừng di chuyển và tấn công
            animator.SetFloat("Speed", 0f);
            isInAttackRange = true;
            HandleAttackSequence();
        }

        // Flip sprite, hitbox, hurtbox
        bool facingRight = moveX > 0;
        spriteRenderer.flipX = facingRight;
        cloneHitBox?.FlipHitbox(facingRight);
        cloneHurtBox?.FlipHurtbox(facingRight);
    }

    // SỬA LẠI LOGIC ATTACK
    void HandleAttackSequence()
    {
        // Chỉ tấn công khi trong tầm, đã hết delay và không đang tấn công
        if (isInAttackRange && !isCurrentlyAttacking && Time.time - lastAttackTime >= attackDelay)
        {
            isCurrentlyAttacking = true; // Đánh dấu đang tấn công
            
            if (attackStep == 1)
            {
                animator.SetTrigger("attack1");
                if (attackEffect1 != null && attackPoint1 != null)
                    Instantiate(attackEffect1, attackPoint1.position, Quaternion.identity);
                
                Debug.Log("Clone triggered attack1 - Next will be attack2");
                attackStep = 2; // Chuẩn bị cho lần tấn công tiếp theo
            }
            else if (attackStep == 2)
            {
                animator.SetTrigger("attack2");
                if (attackEffect2 != null && attackPoint2 != null)
                    Instantiate(attackEffect2, attackPoint2.position, Quaternion.identity);
                
                Debug.Log("Clone triggered attack2 - Next will be attack1");
                attackStep = 1; // Quay lại attack1
            }
            
            lastAttackTime = Time.time;
        }
    }

    // THÊM PHƯƠNG THỨC GỌI TỪ ANIMATION EVENT
    public void OnAttackAnimationEnd()
    {
        isCurrentlyAttacking = false; // Cho phép tấn công tiếp theo
        Debug.Log($"Attack animation ended. Next attack step: {attackStep}");
    }

    void ReturnToSpawn()
    {
        isReturningHome = true;
        isInAttackRange = false;
        isCurrentlyAttacking = false; // Reset attack state
        
        float distanceToSpawn = Vector2.Distance(transform.position, spawnPosition);
        
        if (distanceToSpawn > 0.5f)
        {
            Vector3 direction = (spawnPosition - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
            animator.SetFloat("Speed", Mathf.Abs(direction.x));
            
            // Flip theo hướng về spawn
            bool facingRight = direction.x > 0;
            spriteRenderer.flipX = facingRight;
            cloneHitBox?.FlipHitbox(facingRight);
            cloneHurtBox?.FlipHurtbox(facingRight);
        }
        else
        {
            // Đã về đến spawn point, chuyển sang patrol
            isReturningHome = false;
            animator.SetFloat("Speed", 0f);
        }
    }

    void PatrolAroundSpawn()
    {
        isCurrentlyAttacking = false; // Không tấn công khi patrol
        
        float leftEdge = spawnPosition.x - chaseRadius;
        float rightEdge = spawnPosition.x + chaseRadius;

        if (!isIdle)
        {
            transform.position += new Vector3(moveDirection * moveSpeed * Time.deltaTime, 0, 0);
            animator.SetFloat("Speed", Mathf.Abs(moveSpeed));
            
            // Check if reached edge
            if ((moveDirection < 0 && transform.position.x <= leftEdge) ||
                (moveDirection > 0 && transform.position.x >= rightEdge))
            {
                isIdle = true;
                idleTimer = 0f;
                animator.SetFloat("Speed", 0f);
            }
        }
        else
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleDuration)
            {
                isIdle = false;
                moveDirection *= -1; // Turn around
            }
        }

        // Flip theo direction patrol
        bool facingRight = moveDirection > 0;
        spriteRenderer.flipX = facingRight;
        cloneHitBox?.FlipHitbox(facingRight);
        cloneHurtBox?.FlipHurtbox(facingRight);
    }

    public void TakeDamage(int damage)
    {
        if (isDie) return;

        currentHealth -= damage;
        if (healthBar != null)
            healthBar.value = currentHealth;

        Debug.Log($"Clone took {damage} damage. Current health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            if (animator != null)
            {
                animator.SetTrigger("IsGetHit");
            }
        }
    }

    void Die()
    {
        isDie = true;
        pooling.OnCloneDie(gameObject);
    }

    // Animation Events
    public void EnableHitbox()
    {
        cloneHitBox?.EnableHitbox();
        Debug.Log("Clone hitbox enabled");
    }

    public void DisableHitbox()
    {
        cloneHitBox?.DisableHitbox();
        Debug.Log("Clone hitbox disabled");
    }

    public void EnableHurtbox()
    {
        if (cloneHurtBox != null && cloneHurtBox.GetComponent<Collider2D>() != null)
            cloneHurtBox.GetComponent<Collider2D>().enabled = true;
    }

    public void DisableHurtbox()
    {
        if (cloneHurtBox != null && cloneHurtBox.GetComponent<Collider2D>() != null)
            cloneHurtBox.GetComponent<Collider2D>().enabled = false;
    }
}