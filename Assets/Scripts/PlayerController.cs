using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class Player : MonoBehaviour
{
    // Thêm vào đầu class Player
    [SerializeField] private float speed = 5f;
    [SerializeField] private float runFastMultiplier = 1.5f;
    [SerializeField] private float jumpForce = 60f;
    [SerializeField] private float doubleJumpForce = 50f; // Force for second jump
    [SerializeField] private int maxJumpCount = 2; // Maximum number of jumps allowed
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float comboResetTime = 0.6f; // Thời gian tối đa giữa các đòn combo
    [SerializeField] private float comboDelayAfterFinish = 1.0f; // Thời gian delay sau khi hoàn thành combo
    [SerializeField] private Camera mainCamera; // Reference to main camera
    [SerializeField] private int maxRagePoints = 50; // Điểm tích nộ tối đa
    [SerializeField] private int minRageToActivate = 40; // Điểm tối thiểu để kích hoạt
    [SerializeField] private int currentRagePoints = 0; // Điểm tích nộ hiện tại
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private int rageGainPerHit = 5; // Rage points gained per successful hit
    [SerializeField] private int rageGainPerSuccessfulHit = 5; // Rage points gained when hitting boss
    public bool IsDead => isDead;
    public BoxCollider2D hitBoxCollider;

    public PlayerHitBoxHandle hitBoxHandle; // Kéo thả object con vào đây trong Inspector

    private bool isUltimateActive = false; // Trạng thái ulti
    private int currentJumpCount = 0; // Track current jump count

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private Vector2 moveInput;
    private bool isRunningFast;
    private bool isGrounded;
    private Transform groundCheck;

    private float minX, maxX; // Giới hạn di chuyển

    // Thêm biến tham chiếu đến Transform của hitBox (kéo thả trong Inspector)
    public Transform hitBoxTransform;
    public Transform hurtBoxTransform; // Thêm dòng này

    // Thêm tham chiếu đến PlayerHurtBoxHandle
    private PlayerHurtBoxHandle hurtBoxHandle;

    // Combo logic
    private int comboStepJ = 0;
    private int comboStepK = 0;
    private float lastAttackTimeJ = 0f;
    private float lastAttackTimeK = 0f;
    private bool isAttackingJ = false;
    private bool isAttackingK = false;
    private bool isComboWindowOpenJ = false;
    private bool isComboWindowOpenK = false;
    private bool hasBufferedInputJ = false;
    private bool hasBufferedInputK = false;
    
    // Combo delay variables
    private float comboFinishedTimeJ = 0f; // Thời gian hoàn thành combo J
    private float comboFinishedTimeK = 0f; // Thời gian hoàn thành combo K
    private bool isComboDelayActiveJ = false; // Trạng thái delay sau combo J
    private bool isComboDelayActiveK = false; // Trạng thái delay sau combo K

    // Other logic
    private bool isBlocking = false;
    private float idleTime = 0f;
    private float idleThreshold = 10f;
    private bool isIdleForLong = false;
    private bool isDead = false;
    private int lives = 1;

    // Slash logic
    public GameObject EffectChem1;
    public Transform attackPoint;
    public Transform attackPointUlti; // Ultimate attack point position
    public GameObject UltiEffect; // Ultimate effect prefab

    // Skill effect logic
    public GameObject Effect_Chuong; // Kéo thả prefab Effect_Chuong vào Inspector

    // Properties
    public int CurrentHealth 
    { 
        get => currentHealth;
        private set 
        {
            currentHealth = Mathf.Clamp(value, 0, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth); // Event khi máu thay đổi
        }
    }
    public int MaxHealth => maxHealth;
    public int CurrentRagePoints => currentRagePoints;
    public int MaxRagePoints => maxRagePoints;

    // Event để thông báo khi máu thay đổi
    public delegate void HealthChangeHandler(int currentHealth, int maxHealth);
    public event HealthChangeHandler OnHealthChanged;

    // Add rage change event similar to health change event
    public delegate void RageChangeHandler(int currentRage, int maxRage);
    public event RageChangeHandler OnRageChanged;

    private float hitBoxOriginalPosX;
    //private float hitBoxOriginalOffsetX;

    [SerializeField] private float jumpBufferTime = 0.2f; // Thời gian buffer cho việc nhảy
    private float jumpBufferCounter;
    private bool hasUsedDoubleJump = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;

        groundCheck = transform.Find("GroundCheck");
        if (groundCheck == null)
        {
            Debug.LogError("GroundCheck transform is missing. Please add a child object named 'GroundCheck' to the player.");
        }

        // Lưu giá trị gốc của hitbox
        if (hitBoxTransform != null)
            hitBoxOriginalPosX = hitBoxTransform.localPosition.x;
        
        // IMPORTANT: Ensure hitbox is disabled at awake
        if (hitBoxHandle != null)
        {
            hitBoxHandle.DisableHitbox();
        }
    }

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Khởi tạo hurtBoxHandle
        if (hurtBoxTransform != null)
            hurtBoxHandle = hurtBoxTransform.GetComponent<PlayerHurtBoxHandle>();
        
        // IMPORTANT: Disable player hitbox at start
        DisablePlayerHitbox();
    }

    private void Update()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);

        // If just landed, reset the jump trigger
        if (!wasGrounded && isGrounded)
        {
            animator.ResetTrigger("Jump");
            hasUsedDoubleJump = false; // Reset double jump khi chạm đất
        }

        // Reset jump count when touching ground
        if (isGrounded)
        {
            currentJumpCount = 0;
        }

        float moveX = Input.GetAxis("Horizontal");
        moveInput = new Vector2(moveX, 0).normalized;

        isRunningFast = Input.GetKey(KeyCode.LeftShift);

        animator.SetFloat("Speed", Mathf.Abs(moveX));
        animator.SetBool("IsRunningFast", isRunningFast);
        animator.SetBool("IsGrounded", isGrounded);

        if (Mathf.Abs(moveX) == 0 && !Input.anyKey && isGrounded)
        {
            idleTime += Time.deltaTime;
            if (idleTime >= idleThreshold && !isIdleForLong)
            {
                isIdleForLong = true;
                animator.SetTrigger("IdleForLong");
            }
        }
        else
        {
            idleTime = 0f;
            isIdleForLong = false;
        }

        // Jump buffer system
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // Jump with buffer
        if (jumpBufferCounter > 0)
        {
            // Normal jump from ground
            if (isGrounded)
            {
                DoJump(jumpForce);
                jumpBufferCounter = 0;
            }
            // Double jump in air
            else if (!hasUsedDoubleJump)
            {
                DoJump(doubleJumpForce);
                hasUsedDoubleJump = true;
                jumpBufferCounter = 0;
            }
        }

        // Kiểm tra và cập nhật trạng thái delay combo
        UpdateComboDelayStatus();

        // Combo attack J - chỉ nhận input nếu không trong trạng thái delay
        if (Input.GetKeyDown(KeyCode.J) && !isComboDelayActiveJ)
        {
            TryAttackJ();
            SpawnSkillEffect("Attack1"); // Hiển thị effect chém khi bấm J
        }
        // Combo attack K - chỉ nhận input nếu không trong trạng thái delay
        if (Input.GetKeyDown(KeyCode.K) && !isComboDelayActiveK)
        {
            TryAttackK();
        }
        // Attack3
        if (Input.GetKeyDown(KeyCode.U))
        {
            animator.SetTrigger("Attack3");
            //SpawnSkillEffect("Attack3"); // Hiển thị effect chém khi bấm J

        }
        
        // COMMENT CHIÊU CHÉM CHƯỞNG J+U CHO LEVEL 1
        /*
        if (Input.GetKey(KeyCode.J) && Input.GetKey(KeyCode.U))
        {
            animator.SetTrigger("Attack3+");
        }
        */
        
        if (Input.GetKey(KeyCode.L))
        {
            HandleBlock();
        }
        else if (Input.GetKeyUp(KeyCode.L))
        {
            StopBlock();
        }

        // Ultimate
        HandleUltimate();

        // Reset combo nếu quá thời gian không bấm tiếp
        if (isAttackingJ && Time.time - lastAttackTimeJ > comboResetTime && !hasBufferedInputJ)
        {
            ResetComboJ();
        }
        if (isAttackingK && Time.time - lastAttackTimeK > comboResetTime && !hasBufferedInputK)
        {
            ResetComboK();
        }

        // Thêm vào cuối hàm Update hiện có
        //HandleUltimate();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void LateUpdate()
    {
        // Đảm bảo hitbox luôn có hướng đúng mỗi frame
        if (hitBoxHandle != null)
            hitBoxHandle.FlipHitbox(!spriteRenderer.flipX);
        if (hurtBoxHandle != null)
            hurtBoxHandle.FlipHurtbox(!spriteRenderer.flipX);

        FlipBossAttackPoint();
    }

    // Hàm kiểm tra và cập nhật trạng thái delay combo
    private void UpdateComboDelayStatus()
    {
        // Kiểm tra delay cho combo J
        if (isComboDelayActiveJ && Time.time - comboFinishedTimeJ >= comboDelayAfterFinish)
        {
            isComboDelayActiveJ = false;
            Debug.Log("Combo J delay ended - can attack again");
        }

        // Kiểm tra delay cho combo K
        if (isComboDelayActiveK && Time.time - comboFinishedTimeK >= comboDelayAfterFinish)
        {
            isComboDelayActiveK = false;
            Debug.Log("Combo K delay ended - can attack again");
        }
    }

    //[System.Obsolete]
    void MovePlayer()
    {
        if (isBlocking)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        float currentSpeed = isRunningFast ? speed * runFastMultiplier : speed;
        Vector2 movement = new Vector2(moveInput.x * currentSpeed, rb.linearVelocity.y);

        // Tính toán giới hạn màn hình
        float halfWidth = spriteRenderer.bounds.extents.x;
        float cameraHalfWidth = mainCamera.orthographicSize * mainCamera.aspect;

        // Tính giới hạn trái phải dựa theo vị trí camera
        float leftLimit = mainCamera.transform.position.x - cameraHalfWidth + halfWidth;
        float rightLimit = mainCamera.transform.position.x + cameraHalfWidth - halfWidth;

        // Tính vị trí mới dự kiến
        float newX = transform.position.x + movement.x * Time.fixedDeltaTime;
        // Giới hạn vị trí X trong khoảng cho phép
        newX = Mathf.Clamp(newX, leftLimit, rightLimit);

        // Áp dụng movement với vị trí X đã được giới hạn
        rb.linearVelocity = new Vector2((newX - transform.position.x) / Time.fixedDeltaTime, movement.y);

        // Flip sprite và transforms
        if (moveInput.x < 0)
        {
            spriteRenderer.flipX = true;
            FlipTransforms(false);
        }
        else if (moveInput.x > 0)
        {
            spriteRenderer.flipX = false;
            FlipTransforms(true);
        }
    }

    void DoJump(float force)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
        animator.ResetTrigger("Jump");
        animator.SetTrigger("Jump");
    }

    void Jump()
    {
        jumpBufferCounter = jumpBufferTime;
    }

    // --- Combo J ---
    void TryAttackJ()
    {
        if (isAttackingJ)
        {
            if (isComboWindowOpenJ)
            {
                ContinueComboJ();
            }
            else
            {
                hasBufferedInputJ = true;
            }
        }
        else
        {
            StartComboJ();
        }
    }

    void StartComboJ()
    {
        comboStepJ = 1;
        isAttackingJ = true;
        lastAttackTimeJ = Time.time;
        animator.SetTrigger("Attack1");
    }

    void ContinueComboJ()
    {
        comboStepJ++;
        lastAttackTimeJ = Time.time;
        isComboWindowOpenJ = false;
        hasBufferedInputJ = false;

        if (comboStepJ == 2)
        {
            animator.SetTrigger("Attack1+");
            // Kích hoạt delay sau khi hoàn thành combo 2 hit
            StartComboDelayJ();
        }
        // Nếu có nhiều bước combo hơn, thêm tại đây
    }

    public void OpenComboWindowJ()
    {
        isComboWindowOpenJ = true;
        if (hasBufferedInputJ)
        {
            ContinueComboJ();
        }
    }

    public void CloseComboWindowJ()
    {
        isComboWindowOpenJ = false;
    }

    public void OnAttackAnimationEndJ()
    {
        if (comboStepJ >= 2 || !hasBufferedInputJ)
        {
            ResetComboJ();
        }
    }

    void ResetComboJ()
    {
        comboStepJ = 0;
        isAttackingJ = false;
        isComboWindowOpenJ = false;
        hasBufferedInputJ = false;
    }

    // Hàm bắt đầu delay cho combo J
    void StartComboDelayJ()
    {
        comboFinishedTimeJ = Time.time;
        isComboDelayActiveJ = true;
        Debug.Log($"Combo J finished - delay started for {comboDelayAfterFinish} seconds");
    }

    // --- Combo K ---
    void TryAttackK()
    {
        if (isAttackingK)
        {
            if (isComboWindowOpenK)
            {
                ContinueComboK();
            }
            else
            {
                hasBufferedInputK = true;
            }
        }
        else
        {
            StartComboK();
        }
    }

    void StartComboK()
    {
        comboStepK = 1;
        isAttackingK = true;
        lastAttackTimeK = Time.time;
        animator.SetTrigger("Attack2");
    }

    void ContinueComboK()
    {
        comboStepK++;
        lastAttackTimeK = Time.time;
        isComboWindowOpenK = false;
        hasBufferedInputK = false;

        if (comboStepK == 2)
        {
            animator.SetTrigger("Attack2+");
            // Kích hoạt delay sau khi hoàn thành combo 2 hit
            StartComboDelayK();
        }
        // Nếu có nhiều bước combo hơn, thêm tại đây
    }

    public void OpenComboWindowK()
    {
        isComboWindowOpenK = true;
        if (hasBufferedInputK)
        {
            ContinueComboK();
        }
    }

    public void CloseComboWindowK()
    {
        isComboWindowOpenK = false;
    }

    public void OnAttackAnimationEndK()
    {
        if (comboStepK >= 2 || !hasBufferedInputK)
        {
            ResetComboK();
        }
    }

    void ResetComboK()
    {
        comboStepK = 0;
        isAttackingK = false;
        isComboWindowOpenK = false;
        hasBufferedInputK = false;
    }

    // Hàm bắt đầu delay cho combo K
    void StartComboDelayK()
    {
        comboFinishedTimeK = Time.time;
        isComboDelayActiveK = true;
        Debug.Log($"Combo K finished - delay started for {comboDelayAfterFinish} seconds");
    }

    // --- Các hàm khác giữ nguyên ---
    void HandleBlock()
    {
        isBlocking = true;
        animator.SetBool("IsBlocking", true);
        
        // Only stop horizontal movement, keep vertical movement (gravity)
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    void StopBlock()
    {
        isBlocking = false;
        animator.SetBool("IsBlocking", false);
    }

    void HandleHit()
    {
        animator.SetTrigger("IsHit");
        //Vector2 knockback = new Vector2(-transform.localScale.x * 5f, 2f);
        //rb.AddForce(knockback, ForceMode2D.Impulse);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        
        // Sử dụng property để trigger event OnHealthChanged
        CurrentHealth = currentHealth - damage;
        
        if (CurrentHealth <= 0)
        {
            Die();
        }
        else
        {
            HandleHit();
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        CurrentHealth = currentHealth + amount;
        Debug.Log($"Healed {amount}. Current Health: {currentHealth}/{maxHealth}");
    }

    public void Die()
    {
        if (isDead) return;
        
        isDead = true;
        // Thay đổi từ "IsDead" thành "PlayerDie"
        animator.SetTrigger("IsDead");
        
        // Dừng movement
        rb.linearVelocity = Vector2.zero;
        
        // Disable các input và movement
        enabled = false; // Tắt script này
        
        if (lives > 0)
        {
            lives--;
            Invoke(nameof(Respawn), 3f);
        }
        else
        {
            // Thông báo GameOver cho GameManager
            GameManager.Instance?.GameOver();
        }
    }

    private void Respawn()
    {
        isDead = false;
        enabled = true; // Bật lại script
        CurrentHealth = maxHealth / 2; // Sử dụng property
        animator.SetTrigger("StandUp");
        rb.linearVelocity = Vector2.zero;
        transform.position = new Vector3(0, 0, 0);
    }

    private void HandleUltimate()
    {
        // Kiểm tra đủ điểm tích nộ tối thiểu
        if (Input.GetKeyDown(KeyCode.I) && currentRagePoints >= minRageToActivate && !isUltimateActive)
        {
            ActivateUltimate();
        }
    }

    private void ActivateUltimate()
    {
        if (currentRagePoints >= minRageToActivate)
        {
            // Kích hoạt ulti animation ONLY - effect will be spawned from animator
            isUltimateActive = true;
            animator.SetTrigger("IsUlti");

            // Sử dụng hết điểm tích nộ
            currentRagePoints -= 40;
            if (currentRagePoints < 0) currentRagePoints = 0;
            
            // Tự động tắt ulti sau một khoảng thời gian
            StartCoroutine(DeactivateUltimateAfterDelay(5f));
            
            Debug.Log("Ultimate activated - waiting for animation event to spawn effect");
        }
    }

    private IEnumerator DeactivateUltimateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isUltimateActive = false;

        // Hủy các hiệu ứng ulti
    }

    // Thêm phương thức để tăng điểm tích nộ
    public void AddRagePoints(int points)
    {
        int oldRage = currentRagePoints;
        currentRagePoints = Mathf.Min(currentRagePoints + points, maxRagePoints);
        
        if (currentRagePoints != oldRage)
        {
            Debug.Log($"Rage gained: +{points}. Current rage: {currentRagePoints}/{maxRagePoints}");
            
            // Optional: Trigger rage gain event for UI updates
            OnRageChanged?.Invoke(currentRagePoints, maxRagePoints);
        }
    }

    public void EnablePlayerHitbox()
    {
        if (hitBoxHandle != null)
        {
            hitBoxHandle.EnableHitbox();
            // Flip sẽ được xử lý trong LateUpdate()
        }
    }
    public void DisablePlayerHitbox()
    {
        if (hitBoxHandle != null)
            hitBoxHandle.DisableHitbox();
    }

    public void SpawnSkillEffect(string effectName)
    {
        GameObject prefab = null;
        Transform spawnPoint = attackPoint;
        
        switch (effectName)
        {
            case "Attack1":
                prefab = EffectChem1;
                spawnPoint = attackPoint;
                break;
            case "Attack3":
                prefab = Effect_Chuong;
                spawnPoint = attackPoint;
                break;
            case "Ultimate":
                prefab = UltiEffect;
                spawnPoint = attackPointUlti != null ? attackPointUlti : attackPoint;
                break;
            default:
                Debug.LogWarning("Skill effect not found: " + effectName);
                return;
        }

        if (prefab != null && spawnPoint != null)
        {
            GameObject effect = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
            
            // Sử dụng rotation thay vì scale cho tất cả effect
            if (spriteRenderer.flipX)
            {
                effect.transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            else
            {
                effect.transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            
            Debug.Log($"Spawned {effectName} effect facing {(spriteRenderer.flipX ? "LEFT (rotation)" : "RIGHT (rotation)")}");
        }
    }

    // Add this method to be called when player deals damage (for effects to call)
    public void OnDamageDealt(int damageAmount)
    {
        AddRagePoints(rageGainPerHit);
        Debug.Log($"Player dealt {damageAmount} damage and gained {rageGainPerHit} rage points!");
    }

    // Add this method to be called when player deals damage with specific rage gain
    public void OnDamageDealt(int damageAmount, int rageGain)
    {
        AddRagePoints(rageGain);
        Debug.Log($"Player dealt {damageAmount} damage and gained {rageGain} rage points!");
    }

    // Add this method specifically for animator events
    public void SpawnUlti()
    {
        // This method will be called from animation events
        SpawnSkillEffect("Ultimate");
        Debug.Log("Ultimate effect spawned from animator event!");
    }

    // Alternative method if you want more control
    public void SpawnUltiAtPoint()
    {
        if (UltiEffect != null)
        {
            Transform spawnPoint = attackPointUlti != null ? attackPointUlti : attackPoint;
            
            if (spawnPoint != null)
            {
                GameObject effect = Instantiate(UltiEffect, spawnPoint.position, Quaternion.identity);
                // Flip theo hướng player
                effect.transform.localScale = new Vector3(spriteRenderer.flipX ? -1 : 1, 1, 1);
                Debug.Log("Ultimate effect spawned at specific point from animator!");
            }
        }
    }

    // Keep the existing SpawnUltimateEffect method for compatibility
    public void SpawnUltimateEffect()
    {
        SpawnSkillEffect("Ultimate");
        Debug.Log("Ultimate spawned from SpawnUltimateEffect method!");
    }

    // Add method to be called when player successfully hits boss
    public void OnSuccessfulHit(int damageDealt)
    {
        AddRagePoints(rageGainPerSuccessfulHit);
        Debug.Log($"Player hit boss for {damageDealt} damage and gained {rageGainPerSuccessfulHit} rage points!");
    }

    // Method để gọi từ Animation Event của Attack3 (chiêu chưởng)
    public void SpawnChuong()
    {
        SpawnSkillEffect("Attack3");
        Debug.Log("Chuong effect spawned from animator event!");
    }

    // Method để spawn Effect_Chuong với xử lý hướng tại attackPoint
    public void SpawnChuongAtAttackPoint()
    {
        if (Effect_Chuong != null && attackPoint != null)
        {
            // Spawn effect tại vị trí attackPoint
            GameObject effect = Instantiate(Effect_Chuong, attackPoint.position, Quaternion.identity);
            
            // Lật bằng rotation thay vì scale
            if (spriteRenderer.flipX)
            {
                effect.transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            else
            {
                effect.transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            
            Debug.Log($"Chuong effect spawned facing {(spriteRenderer.flipX ? "LEFT (rotation)" : "RIGHT (rotation)")}");
        }
        else
        {
            Debug.LogError("Effect_Chuong prefab or attackPoint is missing!");
        }
    }

    // Method tổng hợp - xử lý cả vị trí và hướng - SỬA LOGIC LẬT
    public void SpawnChuongWithDirection()
    {
        if (Effect_Chuong != null && attackPoint != null)
        {
            // 1. AttackPoint đã được lật trong MovePlayer()
            // 2. Spawn effect tại attackPoint
            GameObject effect = Instantiate(Effect_Chuong, attackPoint.position, Quaternion.identity);
            
            // 3. LẬT BẰNG ROTATION thay vì scale
            if (spriteRenderer.flipX)
            {
                // Player quay trái - quay effect 180 độ
                effect.transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            else
            {
                // Player quay phải - giữ nguyên rotation
                effect.transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            
            Debug.Log($"Chuong spawned at ({attackPoint.position}) facing {(spriteRenderer.flipX ? "LEFT (rotY=180)" : "RIGHT (rotY=0)")}");
        }
    }

    // Thêm vào cuối class Player
    private void FlipTransforms(bool facingRight)
    {
        // Cập nhật cách gọi flip methods
        if (hitBoxHandle != null)
            hitBoxHandle.FlipHitbox(facingRight);
        if (hurtBoxHandle != null)
            hurtBoxHandle.FlipHurtbox(facingRight);

        FlipAttackPoint(attackPoint, facingRight);
        FlipAttackPoint(attackPointUlti, facingRight);
    }

    private void FlipAttackPoint(Transform point, bool facingRight)
    {
        if (point != null)
        {
            Vector3 localPos = point.localPosition;
            localPos.x = facingRight ? Mathf.Abs(localPos.x) : -Mathf.Abs(localPos.x);
            point.localPosition = localPos;
        }
    }

    // Thêm method này vào PlayerController.cs
    private void FlipBossAttackPoint()
    {
        FlipAttackPoint(attackPoint, !spriteRenderer.flipX);
        FlipAttackPoint(attackPointUlti, !spriteRenderer.flipX);
    }
}
