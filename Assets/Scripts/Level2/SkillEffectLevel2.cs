using UnityEngine;

public class SkillEffectLevel2 : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damage = 12; // Tăng damage cho level 2
    public float lifetime = 1.2f; // Thời gian tồn tại lâu hơn
    
    [Header("Knockback Settings")]
    public Vector2 knockback = new Vector2(6, 1); // Knockback mạnh hơn
    
    [Header("Level 2 Features")]
    public bool hasStatusEffect = true; // Có thể gây hiệu ứng trạng thái
    public float stunDuration = 0.3f; // Thời gian choáng

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Có thể hit cả Boss2HurtBox và BossHurtBox để tương thích
        if (collision.CompareTag("Boss2HurtBox") || collision.CompareTag("BossHurtBox"))
        {
            // Gọi hàm nhận sát thương từ boss
            var bossHurtbox = collision.GetComponent<BossHurtboxHandle>();
            if (bossHurtbox != null)
            {
                bossHurtbox.TakeDamage(damage);
                
                // Áp dụng hiệu ứng choáng nếu có
                if (hasStatusEffect)
                {
                    // Có thể gọi hàm stun của boss nếu cần
                    Debug.Log($"Applied stun effect for {stunDuration} seconds");
                }
            }
            
            // Notify player that they dealt damage for rage gain
            PlayerLevel2 player = FindFirstObjectByType<PlayerLevel2>();
            if (player != null)
            {
                player.OnSuccessfulHit(damage);
            }
            
            Debug.Log($"Skill Level 2 dealt {damage} damage to boss!");
        }
    }
}