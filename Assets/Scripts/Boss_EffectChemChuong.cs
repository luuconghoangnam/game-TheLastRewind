using UnityEngine;
using System.Collections;

public class Boss_EffectChemChuong : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damage = 20; // Damage cao cho boss attack
    public float lifetime = 1.0f; // Thời gian tồn tại
    public Vector2 knockback = new Vector2(8, 3); // Knockback mạnh

    [Header("Movement Settings")]
    public float moveSpeed = 4f; // Tốc độ di chuyển (chậm hơn player)
    public bool canMove = true; // Cho phép bật/tắt di chuyển

    private Vector2 moveDirection;
    private Rigidbody2D rb;
    private bool hasHitPlayer = false; // Prevent multiple hits

    private void Start()
    {
        // Get or add Rigidbody2D nếu có movement
        if (canMove)
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0; // No gravity for boss effect
            }

            // Set movement direction based on boss direction
            SetMovementDirection();

            // Start moving immediately
            rb.linearVelocity = moveDirection * moveSpeed;
        }

        // Auto-destroy after lifetime
        Destroy(gameObject, lifetime);
    }

    private void SetMovementDirection()
    {
        // Find boss to get direction
        bossAiController boss = FindFirstObjectByType<bossAiController>();
        if (boss != null)
        {
            // Get boss sprite renderer
            SpriteRenderer bossSprite = boss.GetComponent<SpriteRenderer>();
            if (bossSprite != null)
            {
                // DIRECTION LOGIC:
                // Boss flipX = true means facing LEFT
                // Boss flipX = false means facing RIGHT
                
                if (bossSprite.flipX)
                {
                    // Boss is facing LEFT
                    moveDirection = Vector2.left;
                    
                    // Rotate the entire effect to face left
                    transform.rotation = Quaternion.Euler(0, 180, 0);
                }
                else
                {
                    // Boss is facing RIGHT
                    moveDirection = Vector2.right;
                    
                    // Keep normal rotation for right
                    transform.rotation = Quaternion.Euler(0, 0, 0);
                }
                
                Debug.Log($"Boss ChemChuong moving {(bossSprite.flipX ? "LEFT" : "RIGHT")}, rotation Y: {transform.rotation.eulerAngles.y}");
            }
            else
            {
                // Fallback to default direction
                moveDirection = Vector2.right;
            }
        }
        else
        {
            // Default direction if boss not found
            moveDirection = Vector2.right;
            Debug.Log("Boss not found, ChemChuong moving RIGHT by default");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Hit player hurtbox
        if (!hasHitPlayer && collision.CompareTag("PlayerHurtBox"))
        {
            // Deal damage to player
            PlayerHurtBoxHandle playerHurtbox = collision.GetComponent<PlayerHurtBoxHandle>();
            if (playerHurtbox != null)
            {
                playerHurtbox.TakeDamage(damage);
            }

            // Try to find Player component directly
            Player player = collision.GetComponent<Player>();
            if (player == null)
            {
                // Try to find Player in parent or root
                player = collision.GetComponentInParent<Player>();
                if (player == null)
                {
                    player = FindFirstObjectByType<Player>();
                }
            }

            if (player != null)
            {
                player.TakeDamage(damage);
            }

            // Mark as hit to prevent multiple hits
            hasHitPlayer = true;

            Debug.Log($"Boss ChemChuong dealt {damage} damage to player!");
        }
    }
}