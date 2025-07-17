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
    private bool hasHitBoss = false; // Prevent multiple hits

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
        // Có thể hit cả Boss2HurtBox và BossHurtBox để tương thích
        if (!hasHitBoss && (collision.CompareTag("Boss2HurtBox") || collision.CompareTag("BossHurtBox")))
        {
            // Gọi hàm nhận sát thương từ boss
            collision.GetComponent<BossHurtboxHandle>()?.TakeDamage(damage);

            // Notify player that they dealt damage for rage gain
            PlayerLevel2 player = FindFirstObjectByType<PlayerLevel2>();
            if (player != null)
            {
                player.OnSuccessfulHit(damage);
            }

            // Mark as hit to prevent multiple hits
            hasHitBoss = true;

            Debug.Log($"ChemChuong dealt {damage} damage to boss!");
        }
    }
}