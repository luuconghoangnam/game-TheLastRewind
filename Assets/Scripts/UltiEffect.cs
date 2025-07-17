using UnityEngine;
using System.Collections;

public class UltiEffect : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damage = 50; // Higher damage for ultimate
    public float lifetime = 3f; // Longer duration for ultimate effect
    
    [Header("Movement Settings")]
    public float moveSpeed = 8f; // Speed of movement
    public Vector2 knockback = new Vector2(15, 5); // Stronger knockback for ultimate
    
    private Vector2 moveDirection;
    private Rigidbody2D rb;
    private bool hasHitBoss = false; // Prevent multiple hits like other effects

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
        
        // Auto-destroy after lifetime (like Effect_Chuong)
        Destroy(gameObject, lifetime);
    }
    
    private void SetMovementDirection()
    {
        // Find player to get direction
        Player player = FindFirstObjectByType<Player>();
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
        if (!hasHitBoss && collision.CompareTag("BossHurtBox")) // Tag kẻ địch
        {
            // Gọi hàm nhận sát thương từ enemy (same as Effect_Chuong)
            collision.GetComponent<BossHurtboxHandle>()?.TakeDamage(damage);
            
            // Notify player that they dealt damage for rage gain
            Player player = FindFirstObjectByType<Player>();
            if (player != null)
            {
                player.OnSuccessfulHit(damage);
            }
            
            // Mark as hit to prevent multiple hits
            hasHitBoss = true;
            
            Debug.Log($"Ultimate dealt {damage} damage to boss!");
        }
    }
}
