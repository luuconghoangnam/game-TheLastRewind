using UnityEngine;

public class Boss2HurtBox : MonoBehaviour
{
    [SerializeField] public Boss2Controller bossController;
    private Vector3 originalLocalPosition;
    private BoxCollider2D hurtboxCollider;

    void Awake()
    {
        originalLocalPosition = transform.localPosition;
        bossController = GetComponentInParent<Boss2Controller>();
        hurtboxCollider = GetComponent<BoxCollider2D>();
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

    public void EnableHurtbox()
    {
        if (hurtboxCollider != null)
            hurtboxCollider.enabled = true;
    }

    public void DisableHurtbox()
    {
        if (hurtboxCollider != null)
            hurtboxCollider.enabled = false;
    }

    // ===== THÊM MỚI: LOGIC XỬ LÝ VA CHẠM =====
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlayerHitBox"))
        {
            PlayerHitBoxHandle playerHitBox = collision.GetComponent<PlayerHitBoxHandle>();
            if (playerHitBox != null)
            {
                int damage = playerHitBox.damage;
                TakeDamage(damage);

                // Hỗ trợ cả Player Level 1 và Level 2
                Player playerLevel1 = playerHitBox.GetComponentInParent<Player>();
                PlayerLevel2 playerLevel2 = playerHitBox.GetComponentInParent<PlayerLevel2>();
                
                if (playerLevel1 != null)
                {
                    playerLevel1.OnSuccessfulHit(damage);
                }
                else if (playerLevel2 != null)
                {
                    playerLevel2.OnSuccessfulHit(damage);
                }
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (bossController != null)
            bossController.TakeDamage(damage);
        
        // Thêm hiệu ứng phản hồi
        if (CombatFeedbackManager.Instance != null)
        {
            bool isHeavyHit = damage > 20; // Điều chỉnh ngưỡng theo game
            CombatFeedbackManager.Instance.ShakeCamera(isHeavyHit ? 0.15f : 0.08f);
            CombatFeedbackManager.Instance.DoHitStop(isHeavyHit ? 0.15f : 0.08f);
            CombatFeedbackManager.Instance.PlayHitSound(isHeavyHit);
        }
    }
}