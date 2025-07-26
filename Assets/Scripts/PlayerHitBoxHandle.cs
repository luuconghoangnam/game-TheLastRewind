using UnityEngine;

public class PlayerHitBoxHandle : MonoBehaviour
{
    public int damage = 10;

    [Header("Player Controller Reference")]
    [SerializeField] private Player playerLevel1;        // Kéo Player Level 1 vào đây
    [SerializeField] private PlayerLevel2 playerLevel2;  // Kéo Player Level 2 vào đây

    private Vector3 originalLocalPosition;
    private BoxCollider2D hitboxCollider;
    private bool hasHitTarget = false;

    void Awake()
    {
        originalLocalPosition = transform.localPosition;
        hitboxCollider = GetComponent<BoxCollider2D>();

        if (hitboxCollider != null)
            hitboxCollider.enabled = false;

        // Nếu chưa assign trong Inspector, tự động tìm
        if (playerLevel1 == null && playerLevel2 == null)
        {
            playerLevel1 = GetComponentInParent<Player>();
            playerLevel2 = GetComponentInParent<PlayerLevel2>();
        }

        if (playerLevel1 == null && playerLevel2 == null)
        {
            Debug.LogError("PlayerHitBoxHandle: No Player controller assigned! Please drag Player to Inspector.");
        }
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
        if (facingRight)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else
        {
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
        hasHitTarget = false;
    }

    public void DisableHitbox()
    {
        if (hitboxCollider != null)
            hitboxCollider.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hitboxCollider != null && hitboxCollider.enabled && !hasHitTarget)
        {
            // Va chạm với BossHurtBox
            if (collision.CompareTag("BossHurtBox"))
            {
                BossHurtboxHandle bossHurtBox = collision.GetComponent<BossHurtboxHandle>();
                if (bossHurtBox != null)
                {
                    bossHurtBox.TakeDamage(damage);
                    hasHitTarget = true;
                    NotifySuccessfulHit(damage);
                }
            }
            // Va chạm với CloneHurtBox
            else if (collision.CompareTag("CloneHurtBox"))
            {
                CloneHurtBox cloneHurtBox = collision.GetComponent<CloneHurtBox>();
                if (cloneHurtBox != null)
                {
                    cloneHurtBox.TakeDamage(damage);
                    hasHitTarget = true;
                    NotifySuccessfulHit(damage);
                }
            }
            // Va chạm với Boss2HurtBox (Level 2)
            else if (collision.CompareTag("Boss2HurtBox"))
            {
                Boss2HurtBox boss2HurtBox = collision.GetComponent<Boss2HurtBox>();
                if (boss2HurtBox != null)
                {
                    boss2HurtBox.TakeDamage(damage);
                    hasHitTarget = true;
                    NotifySuccessfulHit(damage);
                }
            }
        }
    }

    private void NotifySuccessfulHit(int damageDealt)
    {
        if (playerLevel1 != null)
        {
            playerLevel1.OnSuccessfulHit(damageDealt);
        }
        else if (playerLevel2 != null)
        {
            playerLevel2.OnSuccessfulHit(damageDealt);
        }
    }
}