using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarControllerLevel2 : MonoBehaviour
{
    [SerializeField] private Image healthFillBar; // FillBar máu boss
    [SerializeField] private Image frameBar;      // Frame Bar máu boss
    [SerializeField] private Boss2Controller boss; // Script Boss2Controller

    private void Start()
    {
        if (healthFillBar == null)
            Debug.LogError("Health Fill Bar is not assigned!");
        if (boss == null)
            Debug.LogError("Boss2Controller is not assigned!");

        UpdateHealthBar();

        // Đăng ký sự kiện khi máu boss thay đổi
        if (boss != null)
            boss.OnHealthChanged += OnBossHealthChanged;
    }

    private void OnDestroy()
    {
        if (boss != null)
            boss.OnHealthChanged -= OnBossHealthChanged;
    }

    private void UpdateHealthBar()
    {
        if (healthFillBar != null && boss != null)
        {
            float healthRatio = (float)boss.CurrentHealth / boss.MaxHealth;
            healthFillBar.fillAmount = healthRatio;
        }
    }

    private void OnBossHealthChanged(int current, int max)
    {
        UpdateHealthBar();
    }

    public void ReduceHealth(int amount)
    {
        if (boss != null)
        {
            boss.TakeDamage(amount);
            UpdateHealthBar();
        }
    }
}