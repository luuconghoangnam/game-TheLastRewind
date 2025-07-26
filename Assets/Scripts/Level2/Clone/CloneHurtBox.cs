using UnityEngine;

public class CloneHurtBox : MonoBehaviour
{
    [SerializeField] public CloneController cloneController;
    private Vector3 originalLocalPosition;
    private BoxCollider2D hurtboxCollider;

    void Awake()
    {
        originalLocalPosition = transform.localPosition;
        hurtboxCollider = GetComponent<BoxCollider2D>();

        if (cloneController == null)
            cloneController = GetComponentInParent<CloneController>();
    }

    // Properties để Animation có thể keyframe
    public Vector2 HurtboxSize
    {
        get { return hurtboxCollider != null ? hurtboxCollider.size : Vector2.zero; }
        set { if (hurtboxCollider != null) hurtboxCollider.size = value; }
    }

    public Vector2 HurtboxOffset
    {
        get { return hurtboxCollider != null ? hurtboxCollider.offset : Vector2.zero; }
        set { if (hurtboxCollider != null) hurtboxCollider.offset = value; }
    }

    public void FlipHurtbox(bool facingRight)
    {
        // LOGIC: Mặc định Clone quay trái (scale -1)
        if (facingRight)
        {
            // Di chuyển sang PHẢI: scale = 1 (flip từ mặc định)
            transform.localScale = new Vector3(1, 1, 1);
        }
        else
        {
            // Di chuyển sang TRÁI: scale = -1 (giữ nguyên mặc định)
            transform.localScale = new Vector3(-1, 1, 1);
        }

        // Điều chỉnh position tương ứng
        Vector3 pos = transform.localPosition;
        pos.x = Mathf.Abs(originalLocalPosition.x) * (facingRight ? 1 : -1);
        transform.localPosition = pos;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerHitBox"))
        {
            PlayerHitBoxHandle playerHitBox = collision.GetComponent<PlayerHitBoxHandle>();
            if (playerHitBox != null)
            {
                int damage = playerHitBox.damage;
                TakeDamage(damage);

                Player player = playerHitBox.GetComponentInParent<Player>();
                if (player != null)
                {
                    player.OnSuccessfulHit(damage);
                }
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (cloneController != null)
        {
            cloneController.TakeDamage(damage);
        }
    }
}