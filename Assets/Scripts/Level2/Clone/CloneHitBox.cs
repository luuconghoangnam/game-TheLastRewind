using UnityEngine;

public class CloneHitBox : MonoBehaviour
{
    public int damage = 10;
    [SerializeField] public CloneController cloneController;
    private Vector3 originalLocalPosition;
    private BoxCollider2D hitboxCollider;
    private bool hasHitPlayer = false;

    void Awake()
    {
        originalLocalPosition = transform.localPosition;
        hitboxCollider = GetComponent<BoxCollider2D>();

        if (hitboxCollider != null)
            hitboxCollider.enabled = false; // Tắt mặc định

        if (cloneController == null)
            cloneController = GetComponentInParent<CloneController>();
    }

    // Properties để Animation có thể keyframe
    public Vector2 HitboxSize
    {
        get { return hitboxCollider != null ? hitboxCollider.size : Vector2.zero; }
        set { if (hitboxCollider != null) hitboxCollider.size = value; }
    }

    public Vector2 HitboxOffset
    {
        get { return hitboxCollider != null ? hitboxCollider.offset : Vector2.zero; }
        set { if (hitboxCollider != null) hitboxCollider.offset = value; }
    }

    public void FlipHitbox(bool facingRight)
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
                        // Check if player is blocking
                        bool playerIsBlocking = player.GetComponent<Animator>().GetBool("IsBlocking");

                        if (playerIsBlocking)
                        {
                            finalDamage = Mathf.FloorToInt(damage / 3f); // Player chỉ nhận 1/3 sát thương khi block
                        }
                    }

                    playerHurtBox.TakeDamage(finalDamage);
                    hasHitPlayer = true;
                }
            }
        }
    }
}