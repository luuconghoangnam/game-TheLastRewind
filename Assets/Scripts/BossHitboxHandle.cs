using UnityEngine;

public class BossHitboxHandle : MonoBehaviour
{
    public int damage = 10;
    private Collider2D hitboxCollider;
    private bool hasHitPlayer = false;

    // Thêm biến lưu vị trí gốc
    private Vector3 originalLocalPosition;

    void Awake()
    {
        hitboxCollider = GetComponent<Collider2D>();
        if (hitboxCollider != null)
            hitboxCollider.enabled = false; // Tắt mặc định

        // Lưu vị trí gốc
        originalLocalPosition = transform.localPosition;
    }

    // Thêm hàm lật hitbox bằng scale
    public void FlipHitbox(bool facingRight)
    {
        // Lật bằng localScale thay vì offset
        transform.localScale = new Vector3(facingRight ? 1 : -1, 1, 1);
        
        // Điều chỉnh position tương ứng 
        Vector3 pos = transform.localPosition;
        pos.x = Mathf.Abs(originalLocalPosition.x) * (facingRight ? 1 : -1);
        transform.localPosition = pos;
        
        Debug.Log($"Boss FlipHitbox: facingRight={facingRight}, scale={transform.localScale.x}, pos={pos.x}");
    }

    // Gọi từ Animation Event khi bắt đầu frame đánh
    public void EnableHitbox()
    {
        if (hitboxCollider != null)
            hitboxCollider.enabled = true;
        hasHitPlayer = false;
    }

    // Gọi từ Animation Event khi kết thúc frame đánh
    public void DisableHitbox()
    {
        if (hitboxCollider != null)
            hitboxCollider.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hitboxCollider != null && hitboxCollider.enabled && !hasHitPlayer)
        {
            if (collision.CompareTag("PlayerHurtBox"))
            {
                PlayerHurtBoxHandle playerHurtBox = collision.GetComponent<PlayerHurtBoxHandle>();
                if (playerHurtBox != null)
                {
                    // Apply damage reduction if player is blocking
                    Player player = playerHurtBox.GetComponentInParent<Player>();
                    int finalDamage = damage;
                    
                    if (player != null)
                    {
                        // Check the animator parameter instead since isBlocking is private
                        bool playerIsBlocking = player.GetComponent<Animator>().GetBool("IsBlocking");
                        
                        if (playerIsBlocking)
                        {
                            finalDamage = Mathf.FloorToInt(damage / 3f); // Player only takes 1/3 damage when blocking
                            Debug.Log($"Player blocked! Reduced damage to {finalDamage}");
                        }
                    }
                    
                    playerHurtBox.TakeDamage(finalDamage);
                    hasHitPlayer = true;
                    
                    // Notify boss that it dealt damage for rage and Ki gain
                    bossAiController bossAI = GetComponentInParent<bossAiController>();
                    if (bossAI != null)
                    {
                        bossAI.OnPlayerDamageDealt(finalDamage); // For rage
                        bossAI.OnSuccessfulHitPlayer(); // For Ki gain
                    }
                }
            }
        }
    }

    // Thêm hàm vẽ gizmos để debug hitbox
    private void OnDrawGizmos()
    {
        if (hitboxCollider != null)
        {
            // Màu khác với player để phân biệt
            Gizmos.color = hitboxCollider.enabled ? Color.blue : new Color(0, 0, 0.8f, 0.4f);

            // Vẽ hitbox
            Vector3 center = transform.position;
            Vector3 size = Vector3.zero;

            if (hitboxCollider is BoxCollider2D boxCollider)
            {
                center += new Vector3(boxCollider.offset.x * transform.localScale.x, boxCollider.offset.y, 0);
                size = new Vector3(boxCollider.size.x * Mathf.Abs(transform.localScale.x),
                                   boxCollider.size.y, 0.1f);
            }
            else if (hitboxCollider is CircleCollider2D circleCollider)
            {
                center += new Vector3(circleCollider.offset.x * transform.localScale.x, circleCollider.offset.y, 0);
                size = new Vector3(circleCollider.radius * 2 * Mathf.Abs(transform.localScale.x),
                                   circleCollider.radius * 2, 0.1f);
            }

            Gizmos.DrawWireCube(center, size);
        }
    }
}
