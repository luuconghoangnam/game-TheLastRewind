using UnityEngine;

public class Effect_Chuong : MonoBehaviour
{
    public int damage = 10;
    public float lifetime = 0.5f;
    public Vector2 knockback = new Vector2(5, 0);

    private void Start()
    {
        Destroy(gameObject, lifetime); // Tự huỷ sau thời gian
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("BossHurtBox")) // Tag kẻ địch
        {
            // Gọi hàm nhận sát thương từ enemy
            collision.GetComponent<BossHurtboxHandle>()?.TakeDamage(damage);
            
            // Notify player that they dealt damage for rage gain
            Player player = FindFirstObjectByType<Player>();
            if (player != null)
            {
                player.OnSuccessfulHit(damage);
            }
        }
    }
}
