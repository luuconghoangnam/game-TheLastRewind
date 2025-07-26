using UnityEngine;

public class PlayerHurtBoxHandle : MonoBehaviour
{
    [Header("Player Controller Reference")]
    [SerializeField] private Player playerLevel1;        // Kéo Player Level 1 vào đây
    [SerializeField] private PlayerLevel2 playerLevel2;  // Kéo Player Level 2 vào đây

    private Vector3 originalLocalPosition;
    private BoxCollider2D hurtboxCollider;

    void Awake()
    {
        originalLocalPosition = transform.localPosition;
        hurtboxCollider = GetComponent<BoxCollider2D>();

        // Nếu chưa assign trong Inspector, tự động tìm
        if (playerLevel1 == null && playerLevel2 == null)
        {
            playerLevel1 = GetComponentInParent<Player>();
            playerLevel2 = GetComponentInParent<PlayerLevel2>();
        }

        if (playerLevel1 == null && playerLevel2 == null)
        {
            Debug.LogError("PlayerHurtBoxHandle: No Player controller assigned! Please drag Player to Inspector.");
        }
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Va chạm với BossHitBox (Level 1)
        if (collision.CompareTag("BossHitBox"))
        {
            BossHitboxHandle bossHitBox = collision.GetComponent<BossHitboxHandle>();
            if (bossHitBox != null)
            {
                int damage = bossHitBox.damage;
                int finalDamage = CalculateDamageWithBlocking(damage);
                TakeDamage(finalDamage);
            }
        }
        // Va chạm với CloneHitBox
        else if (collision.CompareTag("CloneHitBox"))
        {
            CloneHitBox cloneHitBox = collision.GetComponent<CloneHitBox>();
            if (cloneHitBox != null)
            {
                int damage = cloneHitBox.damage;
                int finalDamage = CalculateDamageWithBlocking(damage);
                TakeDamage(finalDamage);
            }
        }
        // Va chạm với Boss2HitBox (Level 2)
        else if (collision.CompareTag("Boss2HitBox"))
        {
            Boss2HitBox boss2HitBox = collision.GetComponent<Boss2HitBox>();
            if (boss2HitBox != null)
            {
                int damage = boss2HitBox.damage;
                int finalDamage = CalculateDamageWithBlocking(damage);
                TakeDamage(finalDamage);
            }
        }
    }

    private int CalculateDamageWithBlocking(int originalDamage)
    {
        Animator playerAnimator = null;

        if (playerLevel1 != null)
        {
            playerAnimator = playerLevel1.GetComponent<Animator>();
        }
        else if (playerLevel2 != null)
        {
            playerAnimator = playerLevel2.GetComponent<Animator>();
        }

        if (playerAnimator != null)
        {
            bool playerIsBlocking = playerAnimator.GetBool("IsBlocking");

            if (playerIsBlocking)
            {
                int reducedDamage = Mathf.FloorToInt(originalDamage / 3f);
                return reducedDamage;
            }
        }

        return originalDamage;
    }

    public void TakeDamage(int damage)
    {
        if (playerLevel1 != null)
        {
            playerLevel1.TakeDamage(damage);
        }
        else if (playerLevel2 != null)
        {
            playerLevel2.TakeDamage(damage);
        }
    }
}