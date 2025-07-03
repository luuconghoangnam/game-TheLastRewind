using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class Player : MonoBehaviour
{
    // Thêm vào đầu class Player
    [SerializeField] private float speed = 5f;
    [SerializeField] private float runFastMultiplier = 1.5f;
    [SerializeField] private float jumpForce = 60f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float comboResetTime = 0.6f; // Thời gian tối đa giữa các đòn combo
    [SerializeField] private Camera mainCamera; // Reference to main camera
    [SerializeField] private float maxRagePoints = 50f; // Điểm tích nộ tối đa
    [SerializeField] private float minRageToActivate = 40f; // Điểm tối thiểu để kích hoạt
    [SerializeField] private float currentRagePoints = 0f; // Điểm tích nộ hiện tại
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    private bool isUltimateActive = false; // Trạng thái ulti

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private Vector2 moveInput;
    private bool isRunningFast;
    private bool isGrounded;
    private Transform groundCheck;

    private float minX, maxX; // Giới hạn di chuyển

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

    // Other logic
    private bool isBlocking = false;
    private float idleTime = 0f;
    private float idleThreshold = 10f;
    private bool isIdleForLong = false;
    private bool isDead = false;
    private int lives = 3;

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

    // Event để thông báo khi máu thay đổi
    public delegate void HealthChangeHandler(int currentHealth, int maxHealth);
    public event HealthChangeHandler OnHealthChanged;

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
    }

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);

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

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Jump();
        }

        // Combo attack J
        if (Input.GetKeyDown(KeyCode.J))
        {
            TryAttackJ();
        }
        // Combo attack K
        if (Input.GetKeyDown(KeyCode.K))
        {
            TryAttackK();
        }
        // Attack3
        if (Input.GetKeyDown(KeyCode.U))
        {
            animator.SetTrigger("Attack3");
        }
        if (Input.GetKey(KeyCode.J) && Input.GetKey(KeyCode.U))
        {
            animator.SetTrigger("Attack3+");
        }
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

    [System.Obsolete]
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

        // Flip sprite
        if (moveInput.x < 0)
        {
            spriteRenderer.flipX = true;
        }
        else if (moveInput.x > 0)
        {
            spriteRenderer.flipX = false;
        }
    }

    void Jump()
    {
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            animator.SetTrigger("Jump");
        }
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
            animator.SetTrigger("Attack1+");
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
            animator.SetTrigger("Attack2+");
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

    // --- Các hàm khác giữ nguyên ---
    void HandleBlock()
    {
        isBlocking = true;
        animator.SetBool("IsBlocking", true);
        rb.linearVelocity = Vector2.zero;
    }

    void StopBlock()
    {
        isBlocking = false;
        animator.SetBool("IsBlocking", false);
    }

    void HandleHit()
    {
        animator.SetTrigger("IsHit");
        Vector2 knockback = new Vector2(-transform.localScale.x * 5f, 2f);
        rb.AddForce(knockback, ForceMode2D.Impulse);
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
            // Kích hoạt ulti với full điểm
            isUltimateActive = true;
            animator.SetTrigger("IsUlti");
            // Sử dụng hết điểm tích nộ
            currentRagePoints = 0f;
            
            // Thêm hiệu ứng ulti ở đây
            // Ví dụ: tăng sát thương, tốc độ, hiệu ứng đặc biệt...
            
            // Tự động tắt ulti sau một khoảng thời gian
            StartCoroutine(DeactivateUltimateAfterDelay(5f));
        }
        //else if (currentRagePoints >= minRageToActivate)
        //{
        //    // Kích hoạt ulti với điểm tích nộ một phần
        //    isUltimateActive = true;
        //    animator.SetTrigger("IsUlti");
        //    // Sử dụng hết điểm tích nộ
        //    currentRagePoints = 0f;
            
        //    // Thêm hiệu ứng ulti yếu hơn
            
        //    StartCoroutine(DeactivateUltimateAfterDelay(3f));
        //}
    }

    private IEnumerator DeactivateUltimateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isUltimateActive = false;
        // Hủy các hiệu ứng ulti
    }

    // Thêm phương thức để tăng điểm tích nộ
    public void AddRagePoints(float points)
    {
        currentRagePoints = Mathf.Min(currentRagePoints + points, maxRagePoints);
    }
}
