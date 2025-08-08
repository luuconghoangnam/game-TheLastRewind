using UnityEngine;

public class Boss2Controller : MonoBehaviour
{
    [Header("AI Settings")]
    public float moveSpeed = 2f;
    public float attackRange = 2.5f;
    public float minDistanceToPlayer = 1.2f;
    public float attackCooldown = 2f;
    public int maxHealth = 300;
    public Transform player;

    [Header("Attack Points & Effects")]
    public Transform[] attackPoints; // 0-3: attackPoint1-4
    public GameObject[] attackEffects; // 0-3: effect1-4

    [Header("Ground Check")]
    public Transform groundCheck;
    public LayerMask groundLayer;

    [Header("Animation")]
    public Animator animator;

    [Header("HitBox & HurtBox")]
    public Boss2HitBox bossHitBox;
    public Boss2HurtBox bossHurtBox;

    public int currentHealth;
    // ===== SỬA: Đổi isDie từ bool thành property để kiểm tra =====
    public bool IsDead { get; private set; } = false;
    public bool IsOriginal { get; set; }
    public bool IsGetHit { get; private set; }

    // ===== THÊM: Flag để disable hoàn toàn movement trong clone phase =====
    public bool CanMove { get; set; } = false;

    public event System.Action<int, int> OnHealthChanged;
    public event System.Action OnCloneDefeated;

    private float lastAttackTime;
    private SpriteRenderer spriteRenderer;
    private bool isAttacking = false; // THÊM BIẾN NÀY

    // ===== THÊM: Biến để kiểm soát behavior khi ở xa player =====
    private bool shouldChasePlayer = true; // Flag để quyết định đuổi hay tấn công từ xa
    private float lastBehaviorDecisionTime = 0f; // Thời gian quyết định behavior lần cuối
    private float behaviorDecisionCooldown = 1f; // Thời gian chờ giữa các quyết định behavior

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    [Header("Attack Effects")]
    public GameObject bossEffect2Prefab; // Kéo thả prefab vào Inspector
    public Transform attackPoint2; // Attack point cho effect2

    void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // ===== SỬA: Khởi tạo IsOriginal = false, Boss2Idle là default =====
        IsOriginal = false;
        CanMove = false; // ===== THÊM: Mặc định không thể di chuyển =====
        IsDead = false; // ===== SỬA: Khởi tạo IsDead =====
        
        if (animator != null)
        {
            animator.SetBool("IsOriginal", false);
            animator.SetBool("Transition", false);
            animator.SetBool("IsDie", false); // ===== THÊM: Reset IsDie trong animator =====
        }

        // Tự động lấy hitbox/hurtbox nếu chưa gán
        if (bossHitBox == null)
            bossHitBox = GetComponentInChildren<Boss2HitBox>();
        if (bossHurtBox == null)
            bossHurtBox = GetComponentInChildren<Boss2HurtBox>();
    }

    void Update()
    {
        // ===== SỬA: Sử dụng IsDead thay vì isDie =====
        if (IsDead) return;
        
        // ===== SỬA: Kiểm tra IsOriginal để điều khiển behavior =====
        if (IsOriginal)
        {
            // Boss đang trong trạng thái intro - không di chuyển, không tấn công
            animator.SetBool("IsOriginal", true);
            animator.SetFloat("Speed", 0);
            return;
        }

        // ===== THÊM: Kiểm tra CanMove để disable movement hoàn toàn =====
        if (!CanMove)
        {
            // Boss không thể di chuyển (clone phase hoặc chưa được kích hoạt)
            animator.SetBool("IsOriginal", false);
            animator.SetFloat("Speed", 0);
            return;
        }
        
        // ===== Boss ở trạng thái combat bình thường =====
        animator.SetBool("IsOriginal", false);

        float distanceToPlayer = player != null ? Vector2.Distance(transform.position, player.position) : Mathf.Infinity;

        // Nếu boss đứng quá sát player thì lùi lại
        if (distanceToPlayer < minDistanceToPlayer)
        {
            MoveAwayFromPlayer();
        }
        // ===== SỬA: Logic mới khi boss ở xa player =====
        else if (distanceToPlayer > attackRange)
        {
            HandleLongRangeBehavior(distanceToPlayer);
        }
        // Nếu boss ở khoảng cách tấn công thì dừng lại và tấn công
        else
        {
            animator.SetBool("IsGround", IsGrounded());
            animator.SetFloat("Speed", 0);
            TryCloseRangeAttack(distanceToPlayer);
        }
    }

    // ===== THÊM: Method xử lý behavior khi ở xa player =====
    void HandleLongRangeBehavior(float distanceToPlayer)
    {
        // Kiểm tra xem có cần quyết định behavior mới không
        if (Time.time - lastBehaviorDecisionTime > behaviorDecisionCooldown && !isAttacking)
        {
            // Random quyết định: 50% đuổi theo, 50% tấn công từ xa
            shouldChasePlayer = Random.value < 0.5f;
            lastBehaviorDecisionTime = Time.time;
            
            Debug.Log($"Boss behavior decision: {(shouldChasePlayer ? "Chase player" : "Long range attack")}");
        }

        // Thực hiện behavior đã quyết định
        if (shouldChasePlayer && !isAttacking)
        {
            // Đuổi theo player
            MoveToPlayer();
        }
        else if (!isAttacking)
        {
            // Dừng lại và tấn công từ xa
            animator.SetFloat("Speed", 0);
            TryLongRangeAttack();
        }
    }

    // ===== THÊM: Method tấn công từ xa =====
    void TryLongRangeAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown || isAttacking || !CanMove) return;

        isAttacking = true;

        // Random giữa attack2 và attack4 cho tấn công từ xa
        if (Random.value < 0.5f)
        {
            animator.SetTrigger("attack2");
            if (attackEffects.Length > 1 && attackPoints.Length > 1)
                Instantiate(attackEffects[1], attackPoints[1].position, Quaternion.identity);
            Debug.Log("Boss used long range attack2");
        }
        else
        {
            animator.SetTrigger("attack4");
            if (attackEffects.Length > 3 && attackPoints.Length > 3)
                Instantiate(attackEffects[3], attackPoints[3].position, Quaternion.identity);
            Debug.Log("Boss used long range attack4");
        }

        lastAttackTime = Time.time;

        // Sau khi tấn công từ xa, chuyển sang đuổi theo player
        shouldChasePlayer = true;
        lastBehaviorDecisionTime = Time.time; // Reset decision timer

        // Reset isAttacking sau delay
        Invoke(nameof(ResetAttacking), attackCooldown);
    }

    // ===== SỬA: Method tấn công khi ở gần player =====
    void TryCloseRangeAttack(float distanceToPlayer)
    {
        if (Time.time - lastAttackTime < attackCooldown || isAttacking || !CanMove) return;

        isAttacking = true;

        // Random giữa attack1 và attack3 cho tấn công cận chiến
        if (Random.value < 0.5f)
        {
            animator.SetTrigger("attack1");
            if (attackEffects.Length > 0 && attackPoints.Length > 0)
                Instantiate(attackEffects[0], attackPoints[0].position, Quaternion.identity);
            Debug.Log("Boss used close range attack1");
        }
        else
        {
            animator.SetTrigger("attack3");
            if (attackEffects.Length > 2 && attackPoints.Length > 2)
                Instantiate(attackEffects[2], attackPoints[2].position, Quaternion.identity);
            Debug.Log("Boss used close range attack3");
        }

        lastAttackTime = Time.time;

        // Reset isAttacking sau delay
        Invoke(nameof(ResetAttacking), attackCooldown);
    }

    void MoveToPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
        animator.SetFloat("Speed", Mathf.Abs(moveSpeed));
        Flip(direction.x);
    }

    void MoveAwayFromPlayer()
    {
        Vector3 direction = (transform.position - player.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
        animator.SetFloat("Speed", Mathf.Abs(moveSpeed));
        Flip(direction.x);
    }

    void Flip(float directionX)
    {
        // Boss mặc định quay trái (flipX = false), chỉ lật khi đi sang phải
        bool facingRight = directionX > 0;
        if (spriteRenderer != null)
            spriteRenderer.flipX = facingRight;

        // Lật hitbox và hurtbox ngược lại với sprite
        bossHitBox?.FlipHitbox(!facingRight);
        bossHurtBox?.FlipHurtbox(!facingRight);
    }

    // ===== SỬA: Method cũ giờ không dùng nữa, giữ lại để tương thích =====
    void TryAttack(float distanceToPlayer)
    {
        // Method này giờ không được dùng nữa, thay bằng TryCloseRangeAttack và TryLongRangeAttack
        // Giữ lại để tránh lỗi nếu có code khác gọi
        TryCloseRangeAttack(distanceToPlayer);
    }

    void ResetAttacking()
    {
        isAttacking = false;
    }

    // HOẶC gọi từ Animation Event
    public void OnAttackAnimationEnd()
    {
        isAttacking = false;
    }

    public void TakeDamage(int damage)
    {
        // ===== SỬA: Sử dụng IsDead thay vì isDie =====
        if (IsDead) return;

        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        animator.SetTrigger("IsGetHit");
        IsGetHit = true;
        Invoke(nameof(ResetGetHit), 0.1f);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void ResetGetHit()
    {
        IsGetHit = false;
    }

    void Die()
    {
        // ===== SỬA: Đổi isDie thành IsDead và set animator trigger =====
        IsDead = true;
        
        // ===== THÊM: Set trigger thay vì bool =====
        animator.SetTrigger("IsDie");
        
        // ===== SỬA: Reset các animator parameters =====
        animator.SetFloat("Speed", 0);
        animator.SetBool("IsGround", false);
        animator.SetBool("IsOriginal", false);

        GameManager.Instance?.Victory();
        OnCloneDefeated?.Invoke();
    }

    bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);
    }

    public void EndOriginalState()
    {
        IsOriginal = false;
        if (animator != null)
        {
            animator.SetBool("IsOriginal", false);
            animator.SetBool("Transition", false); // Đảm bảo transition được reset
        }
        Debug.Log("Boss2 ended original state - now can move and attack");
    }

    // ===== THÊM: Methods để control movement =====
    public void EnableMovement()
    {
        CanMove = true;
        Debug.Log("Boss2 movement enabled");
    }

    public void DisableMovement()
    {
        CanMove = false;
        animator.SetFloat("Speed", 0); // Dừng animation di chuyển
        Debug.Log("Boss2 movement disabled");
    }

    // Hàm gọi từ Animation Event
    public void EnableHitbox()
    {
        bossHitBox?.EnableHitbox();
    }
    public void DisableHitbox()
    {
        bossHitBox?.DisableHitbox();
    }
    public void EnableHurtbox()
    {
        bossHurtBox?.EnableHurtbox();
    }
    public void DisableHurtbox()
    {
        bossHurtBox?.DisableHurtbox();
    }

    // Thêm method này vào Boss2Controller
    public void OnIntroAnimationComplete()
    {
        // Gọi GameManager thay vì Level2GameController
        GameManager.Instance?.OnLevel2BossIntroComplete();
    }

    // Method gọi từ Animation Event
    public void SpawnBossEffect2()
    {
        if (bossEffect2Prefab != null && attackPoint2 != null)
        {
            BossEffect2.SpawnAtPosition(bossEffect2Prefab, attackPoint2.position);
        }
    }
}