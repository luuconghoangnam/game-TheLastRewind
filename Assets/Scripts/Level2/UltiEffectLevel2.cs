using UnityEngine;
using System.Collections;

public class UltiEffectLevel2 : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damage = 60; // Tăng damage cho level 2
    public float lifetime = 3.5f; // Thời gian tồn tại lâu hơn
    
    [Header("Movement Settings")]
    public float moveSpeed = 10f; // Tốc độ nhanh hơn cho level 2
    public Vector2 knockback = new Vector2(20, 7); // Knockback mạnh hơn
    
    [Header("Level 2 Features")]
    public bool canPierceEnemies = true; // Xuyên qua nhiều kẻ địch
    public int maxHits = 3; // Số lượng kẻ địch tối đa có thể đánh
    
    private Vector2 moveDirection;
    private Rigidbody2D rb;
    private int currentHits = 0;

    private void Start()
    {
        // Get or add Rigidbody2D
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0; // No gravity for ultimate effect
        }
        
        // Set movement direction based on player
        SetMovementDirection();
        
        // Start moving immediately
        rb.linearVelocity = moveDirection * moveSpeed;
        
        // Auto-destroy after lifetime
        Destroy(gameObject, lifetime);
    }
    
    private void SetMovementDirection()
    {
        // Find PlayerLevel2 specifically
        PlayerLevel2 player = FindFirstObjectByType<PlayerLevel2>();
        if (player != null)
        {
            // Move in the direction player is facing
            SpriteRenderer playerSprite = player.GetComponent<SpriteRenderer>();
            if (playerSprite != null)
            {
                // If player is flipped (facing left), move left; otherwise move right
                moveDirection = playerSprite.flipX ? Vector2.left : Vector2.right;
            }
            else
            {
                moveDirection = Vector2.right; // Default direction
            }
        }
        else
        {
            moveDirection = Vector2.right; // Default direction
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ===== SỬA ĐỔI: Xử lý damage cho cả Boss và Clone =====
        bool hitEnemy = false;

        // Xử lý Boss (Level 1 và Level 2)
        if (collision.CompareTag("Boss2HurtBox") || collision.CompareTag("BossHurtBox"))
        {
            // Thử Boss2HurtBox trước (Level 2)
            Boss2HurtBox boss2HurtBox = collision.GetComponent<Boss2HurtBox>();
            if (boss2HurtBox != null)
            {
                boss2HurtBox.TakeDamage(damage);
                hitEnemy = true;
                Debug.Log($"Ultimate Level 2 dealt {damage} damage to Boss2!");
            }
            else
            {
                // Fallback cho BossHurtboxHandle (Level 1)
                var bossHurtbox = collision.GetComponent<BossHurtboxHandle>();
                if (bossHurtbox != null)
                {
                    bossHurtbox.TakeDamage(damage);
                    hitEnemy = true;
                    Debug.Log($"Ultimate Level 2 dealt {damage} damage to Boss1!");
                }
            }
        }
        
        // Xử lý Clone
        if (collision.CompareTag("CloneHurtBox"))
        {
            CloneHurtBox cloneHurtBox = collision.GetComponent<CloneHurtBox>();
            if (cloneHurtBox != null)
            {
                cloneHurtBox.TakeDamage(damage);
                hitEnemy = true;
                Debug.Log($"Ultimate Level 2 dealt {damage} damage to Clone!");
            }
        }

        // Nếu đã hit enemy, xử lý logic chung
        if (hitEnemy)
        {
            // Notify player that they dealt damage for rage gain
            PlayerLevel2 player = FindFirstObjectByType<PlayerLevel2>();
            if (player != null)
            {
                player.OnSuccessfulHit(damage);
            }
            
            currentHits++;
            Debug.Log($"Ultimate Level 2 hit count: ({currentHits}/{maxHits})");
            
            // Destroy if reached max hits and can't pierce
            if (!canPierceEnemies || currentHits >= maxHits)
            {
                Destroy(gameObject);
            }
        }
    }
}