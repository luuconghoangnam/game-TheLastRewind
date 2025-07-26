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
        // ===== SỬA: Dùng rotation giống Level 1 =====
        SetRotationDirection();

        Destroy(gameObject, lifetime);
    }

    // ===== SỬA: Dùng rotation thay vì scale =====
    private void SetRotationDirection()
    {
        // Find PlayerLevel2 to get direction
        PlayerLevel2 player = FindFirstObjectByType<PlayerLevel2>();
        if (player != null)
        {
            SpriteRenderer playerSprite = player.GetComponent<SpriteRenderer>();
            if (playerSprite != null)
            {
                // Dùng rotation giống Level 1
                if (playerSprite.flipX)
                {
                    // Player quay trái - quay effect 180 độ
                    transform.rotation = Quaternion.Euler(0, 180, 0);
                    Debug.Log("Chuong Level 2 facing LEFT (rotY=180)");
                }
                else
                {
                    // Player quay phải - giữ nguyên rotation
                    transform.rotation = Quaternion.Euler(0, 0, 0);
                    Debug.Log("Chuong Level 2 facing RIGHT (rotY=0)");
                }
            }
            else
            {
                // Default rotation if no player sprite found
                transform.rotation = Quaternion.Euler(0, 0, 0);
                Debug.LogWarning("Player SpriteRenderer not found, using default rotation for Chuong Level 2");
            }
        }
        else
        {
            // Default rotation if no player found
            transform.rotation = Quaternion.Euler(0, 0, 0);
            Debug.LogWarning("PlayerLevel2 not found, using default rotation for Chuong Level 2");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ===== GIỮ NGUYÊN: Logic damage giống Level 1 nhưng có crit =====

        // Tính toán damage với cơ hội crit (feature Level 2)
        int finalDamage = damage;
        bool isCritical = Random.value < criticalChance;

        if (isCritical)
        {
            finalDamage = Mathf.RoundToInt(damage * criticalMultiplier);
        }

        // Xử lý Boss (tương tự Level 1 nhưng với crit)
        if (collision.CompareTag("BossHurtBox") || collision.CompareTag("Boss2HurtBox"))
        {
            // Sử dụng BossHurtboxHandle như Level 1
            collision.GetComponent<BossHurtboxHandle>()?.TakeDamage(finalDamage);

            // Notify player that they dealt damage for rage gain (ưu tiên Level 2)
            PlayerLevel2 playerLevel2 = FindFirstObjectByType<PlayerLevel2>();
            if (playerLevel2 != null)
            {
                playerLevel2.OnSuccessfulHit(finalDamage);
            }
            else
            {
                Player player = FindFirstObjectByType<Player>();
                if (player != null)
                {
                    player.OnSuccessfulHit(finalDamage);
                }
            }

            Debug.Log($"Effect_Chuong Level 2 dealt {finalDamage} damage to boss! {(isCritical ? "(CRITICAL)" : "")}");

            if (isCritical)
            {
                Debug.Log("CRITICAL HIT!");
            }
        }

        // ===== THÊM: Xử lý Clone (feature mới Level 2) =====
        if (collision.CompareTag("CloneHurtBox"))
        {
            CloneHurtBox cloneHurtBox = collision.GetComponent<CloneHurtBox>();
            if (cloneHurtBox != null)
            {
                cloneHurtBox.TakeDamage(finalDamage);

                // Notify player for rage gain
                PlayerLevel2 playerLevel2 = FindFirstObjectByType<PlayerLevel2>();
                if (playerLevel2 != null)
                {
                    playerLevel2.OnSuccessfulHit(finalDamage);
                }

                Debug.Log($"Effect_Chuong Level 2 dealt {finalDamage} damage to Clone! {(isCritical ? "(CRITICAL)" : "")}");

                if (isCritical)
                {
                    Debug.Log("CRITICAL HIT!");
                }
            }
        }
    }
}