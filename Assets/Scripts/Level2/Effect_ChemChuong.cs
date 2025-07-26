using UnityEngine;
using System.Collections;

public class Effect_ChemChuongLevel2 : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damage = 25; // Damage cao hơn vì là chiêu kết hợp J+U
    public float lifetime = 1.2f; // Thời gian tồn tại lâu hơn
    public Vector2 knockback = new Vector2(10, 4); // Knockback mạnh hơn vì là combo attack

    [Header("Movement Settings")]
    public float moveSpeed = 6f; // Tốc độ di chuyển (chậm hơn Ultimate một chút)
    public bool canMove = true; // Cho phép bật/tắt di chuyển

    private Vector2 moveDirection;
    private Rigidbody2D rb;
    private bool hasHitTarget = false; // Prevent multiple hits với mọi target

    private void Start()
    {
        // Get or add Rigidbody2D nếu có movement
        if (canMove)
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0; // No gravity for ChemChuong effect
            }

            // Set movement direction based on player
            SetMovementDirection();

            // Start moving immediately
            rb.linearVelocity = moveDirection * moveSpeed;
        }

        // Auto-destroy after lifetime
        Destroy(gameObject, lifetime);
    }

    private void SetMovementDirection()
    {
        // Find PlayerLevel2 to get direction
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
        // Xử lý damage cho cả Boss và Clone
        if (hasHitTarget) return; // Prevent multiple hits

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
                Debug.Log($"ChemChuong dealt {damage} damage to Boss2!");
            }
            else
            {
                // Fallback cho BossHurtboxHandle (Level 1)
                var bossHurtbox = collision.GetComponent<BossHurtboxHandle>();
                if (bossHurtbox != null)
                {
                    bossHurtbox.TakeDamage(damage);
                    hitEnemy = true;
                    Debug.Log($"ChemChuong dealt {damage} damage to Boss1!");
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
                Debug.Log($"ChemChuong dealt {damage} damage to Clone!");
            }
        }

        // Nếu đã hit enemy, xử lý logic chung
        if (hitEnemy)
        {
            hasHitTarget = true;

            // Notify player that they dealt damage for rage gain
            PlayerLevel2 player = FindFirstObjectByType<PlayerLevel2>();
            if (player != null)
            {
                player.OnSuccessfulHit(damage);
            }
        }
    }
}