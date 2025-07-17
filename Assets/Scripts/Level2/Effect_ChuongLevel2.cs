using UnityEngine;

public class Effect_ChuongLevel2 : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damage = 15; // Tăng damage cho level 2
    public float lifetime = 0.7f; // Thời gian tồn tại lâu hơn
    
    [Header("Knockback Settings")]
    public Vector2 knockback = new Vector2(7, 2); // Knockback mạnh hơn
    
    [Header("Level 2 Features")]
    public float criticalChance = 0.15f; // 15% cơ hội crit
    public float criticalMultiplier = 1.5f; // Damage x1.5 khi crit

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Có thể hit cả Boss2HurtBox và BossHurtBox để tương thích
        if (collision.CompareTag("Boss2HurtBox") || collision.CompareTag("BossHurtBox"))
        {
            // Tính toán damage với cơ hội crit
            int finalDamage = damage;
            bool isCritical = Random.value < criticalChance;
            
            if (isCritical)
            {
                finalDamage = Mathf.RoundToInt(damage * criticalMultiplier);
                Debug.Log("CRITICAL HIT!");
            }
            
            // Gọi hàm nhận sát thương từ boss
            var bossHurtbox = collision.GetComponent<BossHurtboxHandle>();
            if (bossHurtbox != null)
            {
                bossHurtbox.TakeDamage(finalDamage);
            }
            
            // Notify player that they dealt damage for rage gain
            PlayerLevel2 player = FindFirstObjectByType<PlayerLevel2>();
            if (player != null)
            {
                player.OnSuccessfulHit(finalDamage);
            }
            
            Debug.Log($"Effect_Chuong Level 2 dealt {finalDamage} damage to boss! {(isCritical ? "(CRITICAL)" : "")}");
        }
    }
}