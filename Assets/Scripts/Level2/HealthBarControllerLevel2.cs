using UnityEngine;
using UnityEngine.UI;

public class HealthBarControllerLevel2 : MonoBehaviour
{
    [SerializeField] private Image healthFillBar; // Reference to FillBar máu
    [SerializeField] private Image frameBar;      // Reference to Frame Bar máu
    [SerializeField] private Image rageFillBar;   // Reference to FillBar nộ
    [SerializeField] private PlayerLevel2 playerLevel2; // Reference to PlayerLevel2 script

    private void Start()
    {
        if (healthFillBar == null)
            Debug.LogError("Health Fill Bar is not assigned!");
        if (rageFillBar == null)
            Debug.LogError("Rage Fill Bar is not assigned!");
        if (playerLevel2 == null)
            Debug.LogError("PlayerLevel2 is not assigned!");

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
        if (healthFillBar != null && playerLevel2 != null)
        {
            float healthRatio = (float)playerLevel2.CurrentHealth / playerLevel2.MaxHealth;
            healthFillBar.fillAmount = healthRatio;
        }
    }

    private void UpdateRageBar()
    {
        if (rageFillBar != null && playerLevel2 != null)
        {
            float rageRatio = (float)playerLevel2.CurrentRagePoints / playerLevel2.MaxRagePoints;
            rageFillBar.fillAmount = rageRatio;
        }
    }

    public void ReduceHealth(int amount)
    {
        if (playerLevel2 != null)
        {
            playerLevel2.TakeDamage(amount);
            UpdateHealthBar();
        }
    }
}