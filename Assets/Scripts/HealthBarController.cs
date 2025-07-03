using UnityEngine;
using UnityEngine.UI;

public class HealthBarController : MonoBehaviour
{
    [SerializeField] private Image healthFillBar; // Reference to FillBar
    [SerializeField] private Image frameBar; // Reference to Frame Bar
    [SerializeField] private Player player; // Reference to Player script

    private void Start()
    {
        // Kiểm tra references
        if (healthFillBar == null)
            Debug.LogError("Health Fill Bar is not assigned!");
        if (player == null)
            Debug.LogError("Player is not assigned!");

        // Set giá trị ban đầu cho thanh máu
        UpdateHealthBar();
    }

    private void Update()
    {
        // Cập nhật thanh máu
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (healthFillBar != null && player != null)
        {
            // Tính toán tỷ lệ máu hiện tại
            float healthRatio = (float)player.CurrentHealth / player.MaxHealth;
            // Cập nhật fill amount
            healthFillBar.fillAmount = healthRatio;
        }
    }
}