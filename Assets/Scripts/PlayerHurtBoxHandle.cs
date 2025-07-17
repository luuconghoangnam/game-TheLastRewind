using UnityEngine;

public class PlayerHurtBoxHandle : MonoBehaviour
{
    public HealthBarController healthController;
    private Vector3 originalLocalPosition;

    void Awake()
    {
        // Lưu vị trí gốc
        originalLocalPosition = transform.localPosition;
    }

    public void FlipHurtbox(bool facingRight)
    {
        // Lật bằng localScale
        transform.localScale = new Vector3(facingRight ? 1 : -1, 1, 1);
        
        // Thêm điều chỉnh position
        Vector3 pos = transform.localPosition;
        pos.x = Mathf.Abs(originalLocalPosition.x) * (facingRight ? 1 : -1);
        transform.localPosition = pos;
    }

    public void TakeDamage(int damage)
    {
        if (healthController != null)
        {
            healthController.ReduceHealth(damage);
        }
    }

    // Vẽ gizmos để debug
    private void OnDrawGizmos()
    {
        Collider2D hurtboxCollider = GetComponent<Collider2D>();
        if (hurtboxCollider != null)
        {
            // Màu xanh lá khi active, màu xanh nhạt khi inactive
            Gizmos.color = hurtboxCollider.enabled ? 
                new Color(0.2f, 0.8f, 0.2f, 0.8f) : // Xanh lá đậm khi active
                new Color(0.5f, 0.9f, 0.5f, 0.3f);  // Xanh lá nhạt khi inactive
            
            Vector3 center = transform.position;
            Vector3 size = Vector3.zero;
            
            if (hurtboxCollider is BoxCollider2D boxCollider)
            {
                center += new Vector3(boxCollider.offset.x * transform.localScale.x, 
                                     boxCollider.offset.y, 0);
                size = new Vector3(boxCollider.size.x * Mathf.Abs(transform.localScale.x),
                                  boxCollider.size.y, 0.1f);
                Gizmos.DrawWireCube(center, size);
            }
            else if (hurtboxCollider is CircleCollider2D circleCollider)
            {
                center += new Vector3(circleCollider.offset.x * transform.localScale.x, 
                                     circleCollider.offset.y, 0);
                float radius = circleCollider.radius * Mathf.Abs(transform.localScale.x);
                Gizmos.DrawWireSphere(center, radius);
            }
        }
    }
}
