using UnityEngine;

public class BossEffect2 : MonoBehaviour
{
    [Header("Boss Attack Settings")]
    public int damage = 20; // Damage của boss attack2
    public float lifetime = 1.5f; // Thời gian tồn tại
    public Vector2 knockback = new Vector2(12, 5); // Knockback force cho player

    [Header("Movement Settings")]
    public float moveSpeed = 8f; // Tốc độ di chuyển theo hướng boss
    public bool canMove = true; // Cho phép bật/tắt di chuyển

    [Header("Boss Effect Features")]
    public bool canPiercePlayer = false; // Có thể xuyên qua player hay không

    private Vector2 moveDirection;
    private Rigidbody2D rb;
    private bool hasHitTarget = false; // Prevent multiple hits

    private void Start()
    {
        Debug.Log("BossEffect2 Start() called");

        // Setup direction based on boss TRƯỚC KHI setup movement
        SetMovementDirection();

        // Setup movement nếu được bật
        if (canMove)
        {
            SetupMovement();
        }

        // Auto-destroy after lifetime
        Destroy(gameObject, lifetime);

        Debug.Log($"BossEffect2 initialized - moveDirection: {moveDirection}, canMove: {canMove}");
    }

    private void SetupMovement()
    {
        Debug.Log("Setting up movement...");

        // Get or add Rigidbody2D
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            Debug.Log("Added Rigidbody2D component");
        }

        rb.gravityScale = 0; // No gravity for boss effect
        rb.linearDamping = 0; // No drag for smooth movement
        rb.freezeRotation = true; // Prevent rotation

        Debug.Log($"Rigidbody2D setup complete - moveDirection: {moveDirection}, moveSpeed: {moveSpeed}");

        // Start moving immediately
        if (moveDirection != Vector2.zero)
        {
            rb.linearVelocity = moveDirection * moveSpeed;
            Debug.Log($"Started moving with velocity: {rb.linearVelocity}");
        }
        else
        {
            Debug.LogWarning("moveDirection is zero - effect won't move!");
        }
    }

    private void SetMovementDirection()
    {
        Debug.Log("Setting movement direction...");

        // Find Boss2Controller để lấy hướng
        Boss2Controller boss = FindFirstObjectByType<Boss2Controller>();
        if (boss != null)
        {
            SpriteRenderer bossSprite = boss.GetComponent<SpriteRenderer>();
            if (bossSprite != null)
            {
                // ===== SỬA: Logic đơn giản - di chuyển theo hướng boss đang nhìn =====
                // Mặc định boss quay trái (flipX = false), effect di chuyển sang trái
                // Khi boss quay phải (flipX = true), effect di chuyển sang phải
                moveDirection = bossSprite.flipX ? Vector2.right : Vector2.left;

                // Set scale direction để effect quay đúng hướng
                SetScaleDirection(bossSprite.flipX);

                Debug.Log($"Boss found - Effect moving {(bossSprite.flipX ? "RIGHT" : "LEFT")}, direction: {moveDirection}");
            }
            else
            {
                // Default: Boss mặc định quay trái, effect di chuyển sang trái
                moveDirection = Vector2.left;
                Debug.LogWarning("Boss SpriteRenderer not found, using default LEFT direction");
            }
        }
        else
        {
            // Default: Effect di chuyển sang trái (mặc định boss quay trái)
            moveDirection = Vector2.left;
            Debug.LogWarning("Boss2Controller not found, using default LEFT direction");
        }
    }

    private void SetScaleDirection(bool bossFlipX)
    {
        // ===== SỬA: Logic scale đơn giản =====
        // Mặc định effect quay trái (scale 1), khi boss quay phải thì effect cũng quay phải (scale -1)
        float scaleX = bossFlipX ? -1f : 1f;
        transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
        Debug.Log($"Effect scale set to: {transform.localScale} (Boss flipX: {bossFlipX})");
    }

    private void Update()
    {
        // ===== Debug velocity mỗi giây =====
        if (Time.frameCount % 60 == 0) // Log mỗi giây
        {
            if (rb != null)
            {
                Debug.Log($"BossEffect2 velocity: {rb.linearVelocity}, position: {transform.position}");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"BossEffect2 collision detected with: {collision.name}, tag: {collision.tag}");

        // Prevent multiple hits
        if (hasHitTarget) return;

        // Xử lý damage cho Player
        if (collision.CompareTag("PlayerHurtBox"))
        {
            PlayerHurtBoxHandle playerHurtBox = collision.GetComponent<PlayerHurtBoxHandle>();
            if (playerHurtBox != null)
            {
                playerHurtBox.TakeDamage(damage);
                hasHitTarget = true;

                Debug.Log($"Boss Effect2 dealt {damage} damage to Player via HurtBox!");

                // Apply knockback to player
                ApplyKnockbackToPlayer(collision);

                // Destroy effect after hit (unless can pierce)
                if (!canPiercePlayer)
                {
                    Destroy(gameObject);
                }
            }
        }

        // Xử lý va chạm với Player
        if (collision.CompareTag("Player"))
        {
            PlayerLevel2 playerLevel2 = collision.GetComponent<PlayerLevel2>();
            Player playerLevel1 = collision.GetComponent<Player>();

            if (playerLevel2 != null)
            {
                playerLevel2.TakeDamage(damage);
                hasHitTarget = true;
                Debug.Log($"Boss Effect2 dealt {damage} damage to PlayerLevel2!");
            }
            else if (playerLevel1 != null)
            {
                playerLevel1.TakeDamage(damage);
                hasHitTarget = true;
                Debug.Log($"Boss Effect2 dealt {damage} damage to PlayerLevel1!");
            }

            // Apply knockback
            ApplyKnockbackToPlayer(collision);

            // Destroy effect after hit (unless can pierce)
            if (!canPiercePlayer)
            {
                Destroy(gameObject);
            }
        }
    }

    private void ApplyKnockbackToPlayer(Collider2D playerCollider)
    {
        Rigidbody2D playerRb = playerCollider.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            // Knockback direction same as effect movement
            Vector2 knockbackDirection = moveDirection.normalized;
            Vector2 knockbackForce = new Vector2(knockbackDirection.x * knockback.x, knockback.y);

            playerRb.AddForce(knockbackForce, ForceMode2D.Impulse);
            Debug.Log($"Applied knockback force: {knockbackForce}");
        }
    }

    // Method để boss gọi để spawn effect
    public static GameObject SpawnAtPosition(GameObject prefab, Vector3 position)
    {
        if (prefab != null)
        {
            GameObject effect = Instantiate(prefab, position, Quaternion.identity);
            Debug.Log($"Boss Effect2 spawned at {position}");
            return effect;
        }
        else
        {
            Debug.LogError("BossEffect2 prefab is null!");
            return null;
        }
    }
}