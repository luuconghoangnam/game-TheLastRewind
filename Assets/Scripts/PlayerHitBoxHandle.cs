using UnityEngine;

public class PlayerHitBoxHandle : MonoBehaviour
{
    public int damage = 10; // Damage dealt to boss
    private BoxCollider2D hitboxCollider; // Thay đổi thành BoxCollider2D
    private bool hasHitBoss = false;

    private float originalOffsetX;
    private Vector3 originalLocalPosition;

    void Awake()
    {
        // Lấy BoxCollider2D thay vì Collider2D
        hitboxCollider = GetComponent<BoxCollider2D>();
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = true;
            originalOffsetX = hitboxCollider.offset.x;
            Debug.Log($"Original offset X: {originalOffsetX}");
        }
        originalLocalPosition = transform.localPosition;
    }

    // Gọi hàm này từ PlayerController khi đổi hướng
    public void FlipHitbox(bool facingRight)
    {
        // Lật bằng localScale
        transform.localScale = new Vector3(facingRight ? 1 : -1, 1, 1);
        
        // Thêm điều chỉnh position
        Vector3 pos = transform.localPosition;
        pos.x = Mathf.Abs(originalLocalPosition.x) * (facingRight ? 1 : -1);
        transform.localPosition = pos;
        
        // Log có thể loại bỏ sau khi đã kiểm tra hoạt động đúng
         //Debug.Log($"FlipHitbox: facingRight={facingRight}, scale={transform.localScale.x}, pos={pos.x}");
    }

    public void EnableHitbox()
    {
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = true;
            // Không thực hiện flip ở đây, để Player.EnablePlayerHitbox() gọi FlipHitbox
        }
        hasHitBoss = false;
    }

    public void DisableHitbox()
    {
        if (hitboxCollider != null)
            hitboxCollider.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"Player hitbox trigger entered by: {collision.name}");
        
        if (hitboxCollider != null && hitboxCollider.enabled && !hasHitBoss)
        {
            if (collision.CompareTag("BossHurtBox"))
            {
                Debug.Log("Player hitbox hit boss hurtbox!");
                BossHurtboxHandle bossHurtBox = collision.GetComponent<BossHurtboxHandle>();
                if (bossHurtBox != null)
                {
                    bossHurtBox.TakeDamage(damage);
                    hasHitBoss = true;
                    
                    // Notify player that they successfully hit the boss for rage gain
                    Player player = GetComponentInParent<Player>();
                    if (player != null)
                    {
                        player.OnSuccessfulHit(damage);
                    }
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (hitboxCollider != null)
        {
            // Màu đỏ khi active, màu hồng nhạt khi inactive
            Gizmos.color = hitboxCollider.enabled ? 
                new Color(1f, 0.2f, 0.2f, 0.8f) : // Đỏ đậm khi active
                new Color(1f, 0.5f, 0.5f, 0.3f);  // Hồng nhạt khi inactive

            // Tính toán center và size dựa trên transform và scale
            Vector3 center = transform.position + new Vector3(
                hitboxCollider.offset.x * transform.localScale.x, 
                hitboxCollider.offset.y, 0);
            Vector3 size = new Vector3(
                hitboxCollider.size.x * Mathf.Abs(transform.localScale.x), 
                hitboxCollider.size.y, 0.1f);

            Gizmos.DrawWireCube(center, size);
        }
    }
}