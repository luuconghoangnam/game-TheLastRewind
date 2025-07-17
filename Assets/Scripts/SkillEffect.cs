using UnityEngine;

public class SkillEffect : MonoBehaviour
{
    public int damage = 10;
    public float lifetime = 1f;
    public Vector2 knockback = new Vector2(5, 0);

    private void Start()
    {
        Destroy(gameObject, lifetime); // Tự huỷ sau thời gian
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Boss")) // Tag kẻ địch
        {
            // Gọi hàm nhận sát thương từ enemy
            collision.GetComponent<bossAiController>().TakeDamage(damage);
            
            // Notify player that they dealt damage for rage gain
            Player player = FindFirstObjectByType<Player>();
            if (player != null)
            {
                player.OnSuccessfulHit(damage);
            }
        }
    }
}
