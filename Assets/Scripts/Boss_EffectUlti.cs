using UnityEngine;
using System.Collections;

public class Boss_UltimateEffect : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damage = 35; // Higher damage for boss ultimate
    public float lifetime = 1.5f; // Longer lifetime for ultimate
    public Vector2 knockback = new Vector2(12, 5); // Stronger knockback for ultimate

    [Header("Movement Settings")]
    public float moveSpeed = 7f; // Faster than regular attacks
    public bool canMove = true; // Allow toggling movement

    [Header("Visual Effects")]
    public GameObject hitEffectPrefab; // Optional effect on hit
    public Color effectColor = new Color(1f, 0.5f, 0f); // Orange glow for ultimate

    private Vector2 moveDirection;
    private Rigidbody2D rb;
    private bool hasHitPlayer = false; // Prevent multiple hits
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        // Get sprite renderer for visual effects
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && effectColor != null)
        {
            spriteRenderer.color = effectColor; // Apply ultimate glow color
        }

        // Get or add Rigidbody2D if we need movement
        if (canMove)
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0; // No gravity for effect
            }

            // Set movement direction based on boss facing direction
            SetMovementDirection();

            // Start moving immediately
            rb.linearVelocity = moveDirection * moveSpeed;
        }

        // Auto-destroy after lifetime
        Destroy(gameObject, lifetime);
    }

    private void SetMovementDirection()
    {
        // Find the boss to get direction
        bossAiController boss = FindFirstObjectByType<bossAiController>();
        if (boss != null)
        {
            // Get the boss's sprite renderer
            SpriteRenderer bossSprite = boss.GetComponent<SpriteRenderer>();
            if (bossSprite != null)
            {
                // Determine movement direction based on which way boss is facing
                moveDirection = bossSprite.flipX ? Vector2.left : Vector2.right;

                // Correctly orient the effect based on direction
                if (bossSprite.flipX)
                {
                    // Boss facing left, rotate effect to face left
                    transform.rotation = Quaternion.Euler(0, 180, 0);
                }
                else
                {
                    // Boss facing right, normal orientation
                    transform.rotation = Quaternion.Euler(0, 0, 0);
                }

                Debug.Log($"Boss Ultimate moving {(bossSprite.flipX ? "LEFT" : "RIGHT")}");
            }
            else
            {
                moveDirection = Vector2.right; // Default direction
            }
        }
        else
        {
            moveDirection = Vector2.right; // Default direction
            Debug.Log("Boss not found, Ultimate moving RIGHT by default");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if we hit player's hurtbox and haven't hit the player yet
        if (!hasHitPlayer && collision.CompareTag("PlayerHurtBox"))
        {
            // Deal damage to player
            PlayerHurtBoxHandle playerHurtbox = collision.GetComponent<PlayerHurtBoxHandle>();
            if (playerHurtbox != null)
            {
                playerHurtbox.TakeDamage(damage);
            }

            // Also try to find Player component (either directly or through parents)
            Player player = collision.GetComponent<Player>();
            if (player == null)
            {
                player = collision.GetComponentInParent<Player>();
                if (player == null)
                {
                    player = FindFirstObjectByType<Player>();
                }
            }

            if (player != null)
            {
                player.TakeDamage(damage);
                
                // Remove knockback line since Player doesn't have ApplyKnockback method
                // If the Player class has a different method for knockback, you can use that instead
            }

            // Notify boss that it dealt damage for rage gain
            bossAiController boss = FindFirstObjectByType<bossAiController>();
            if (boss != null)
            {
                boss.OnPlayerDamageDealt(damage);
            }

            // Spawn hit effect if specified
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, collision.transform.position, Quaternion.identity);
            }

            // Mark as hit to prevent multiple hits
            hasHitPlayer = true;

            Debug.Log($"Boss Ultimate dealt {damage} damage to player!");
        }
    }
}