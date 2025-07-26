using UnityEngine;

public class Boss2HitBox : MonoBehaviour
{
    public int damage = 20;
    [SerializeField] public Boss2Controller bossController;
    private Vector3 originalLocalPosition;
    private BoxCollider2D hitboxCollider;
    private bool hasHitPlayer = false;

    void Awake()
    {
        originalLocalPosition = transform.localPosition;
        hitboxCollider = GetComponent<BoxCollider2D>();

        if (hitboxCollider != null)
            hitboxCollider.enabled = false; // Tắt mặc định

        if (bossController == null)
            bossController = GetComponentInParent<Boss2Controller>();
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
        // LOGIC: Mặc định Boss quay trái (scale -1)
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

        Vector3 pos = transform.localPosition;
        pos.x = Mathf.Abs(originalLocalPosition.x) * (facingRight ? 1 : -1);
        transform.localPosition = pos;
    }

    public void EnableHitbox()
    {
        if (hitboxCollider != null)
            hitboxCollider.enabled = true;
        hasHitPlayer = false;
    }

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
                    // Lấy Player từ cả Level 1 và Level 2
                    Player playerLevel1 = playerHurtBox.GetComponentInParent<Player>();
                    PlayerLevel2 playerLevel2 = playerHurtBox.GetComponentInParent<PlayerLevel2>();

                    int finalDamage = damage;

                    // Kiểm tra blocking cho Player Level 1
                    if (playerLevel1 != null)
                    {
                        bool playerIsBlocking = playerLevel1.GetComponent<Animator>().GetBool("IsBlocking");
                        if (playerIsBlocking)
                        {
                            finalDamage = Mathf.FloorToInt(damage / 3f);
                        }
                    }
                    // Kiểm tra blocking cho Player Level 2
                    else if (playerLevel2 != null)
                    {
                        bool playerIsBlocking = playerLevel2.GetComponent<Animator>().GetBool("IsBlocking");
                        if (playerIsBlocking)
                        {
                            finalDamage = Mathf.FloorToInt(damage / 3f);
                        }
                    }

                    playerHurtBox.TakeDamage(finalDamage);
                    hasHitPlayer = true;
                }
            }
        }
    }
}