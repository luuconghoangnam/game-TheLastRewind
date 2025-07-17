using UnityEngine;
using UnityEngine.UI;

public class HealthBarController : MonoBehaviour
{
    [SerializeField] private Image healthFillBar; // Reference to FillBar máu
    [SerializeField] private Image frameBar;      // Reference to Frame Bar máu
    [SerializeField] private Image rageFillBar;   // Reference to FillBar nộ
    [SerializeField] private Player player;       // Reference to Player script

    private void Start()
    {
        if (healthFillBar == null)
            Debug.LogError("Health Fill Bar is not assigned!");
        if (rageFillBar == null)
            Debug.LogError("Rage Fill Bar is not assigned!");
        if (player == null)
            Debug.LogError("Player is not assigned!");

        UpdateHealthBar();
        UpdateRageBar();
    }

    private void Update()
    {
        UpdateHealthBar();
        UpdateRageBar();
    }

    private void UpdateHealthBar()
    {
        if (healthFillBar != null && player != null)
        {
            float healthRatio = (float)player.CurrentHealth / player.MaxHealth;
            healthFillBar.fillAmount = healthRatio;
        }
    }

    private void UpdateRageBar()
    {
        if (rageFillBar != null && player != null)
        {
            float rageRatio = (float)player.CurrentRagePoints / player.MaxRagePoints;
            rageFillBar.fillAmount = rageRatio;
        }
    }

    public void ReduceHealth(int amount)
    {
        if (player != null)
        {
            player.TakeDamage(amount);
            UpdateHealthBar();
        }
    }
}