using UnityEngine;

public class BossHurtboxHandle : MonoBehaviour
{
    //public bossAiController bossController;
    private Vector3 originalLocalPosition;

    // Biến này chỉ là tham chiếu đến component bossAiController trên GameObject cha
    public bossAiController bossAIController;

    void Awake()
    {
        // Lưu vị trí gốc
        originalLocalPosition = transform.localPosition;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Thêm hàm FlipHurtbox
    public void FlipHurtbox(bool facingRight)
    {
        // Lật bằng localScale
        transform.localScale = new Vector3(facingRight ? 1 : -1, 1, 1);
        
        // Thêm phần này để điều chỉnh position tương ứng
        Vector3 pos = transform.localPosition;
        pos.x = Mathf.Abs(originalLocalPosition.x) * (facingRight ? 1 : -1);
        transform.localPosition = pos;
        
        // Log để debug
        Debug.Log($"[{Time.frameCount}] Boss FlipHurtbox: facingRight={facingRight}, scale={transform.localScale.x}, pos={pos.x}");
    }

    public void TakeDamage(int damage)
    {
        if (bossAIController != null)
        {
            bossAIController.TakeDamage(damage);
        }
    }

    // Vẽ gizmos để debug
    private void OnDrawGizmos()
    {
        Collider2D hurtboxCollider = GetComponent<Collider2D>();
        if (hurtboxCollider != null)
        {
            // Màu khác với boss hitbox để phân biệt (cam nhạt)
            Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.4f);
            
            Vector3 center = transform.position;
            Vector3 size = Vector3.zero;
            
            if (hurtboxCollider is BoxCollider2D boxCollider)
            {
                center += new Vector3(boxCollider.offset.x * transform.localScale.x, boxCollider.offset.y, 0);
                size = new Vector3(boxCollider.size.x * Mathf.Abs(transform.localScale.x),
                                  boxCollider.size.y, 0.1f);
                Gizmos.DrawWireCube(center, size);
            }
            else if (hurtboxCollider is CircleCollider2D circleCollider)
            {
                center += new Vector3(circleCollider.offset.x * transform.localScale.x, circleCollider.offset.y, 0);
                float radius = circleCollider.radius * Mathf.Abs(transform.localScale.x);
                Gizmos.DrawWireSphere(center, radius);
            }
        }
    }
}
