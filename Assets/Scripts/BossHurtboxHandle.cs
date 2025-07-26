using UnityEngine;

public class BossHurtboxHandle : MonoBehaviour
{
    private Vector3 originalLocalPosition;
    private BoxCollider2D hurtboxCollider;

    // Biến này chỉ là tham chiếu đến component bossAiController trên GameObject cha
    public bossAiController bossAIController;

    void Awake()
    {
        originalLocalPosition = transform.localPosition;
        hurtboxCollider = GetComponent<BoxCollider2D>();

        if (bossAIController == null)
            bossAIController = GetComponentInParent<bossAiController>();
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

    public void TakeDamage(int damage)
    {
        if (bossAIController != null)
        {
            bossAIController.TakeDamage(damage);
        }
    }
}