using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarController : MonoBehaviour
{
    [SerializeField] private Image healthFillBar; // Reference to FillBar máu boss
    [SerializeField] private Image frameBar;      // Reference to Frame Bar máu boss
    private Image kiFillBar;     // Reference to FillBar Ki boss
    [SerializeField] private bossAiController boss; // Reference to Boss script

    private void Start()
    {
        if (healthFillBar == null)
            Debug.LogError("Health Fill Bar is not assigned!");
        //if (kiFillBar == null)
        //    Debug.LogError("Ki Fill Bar is not assigned!");
        if (boss == null)
            Debug.LogError("Boss is not assigned!");

        UpdateHealthBar();
        UpdateKiBar();
    }

    private void Update()
    {
        UpdateHealthBar();
        UpdateKiBar();
    }

    private void UpdateHealthBar()
    {
        if (healthFillBar != null && boss != null)
        {
            float healthRatio = (float)boss.CurrentHealth / boss.MaxHealth;
            healthFillBar.fillAmount = healthRatio;
        }
    }

    private void UpdateKiBar()
    {
        if (kiFillBar != null && boss != null)
        {
            float kiRatio = (float)boss.CurrentKi / boss.MaxKi;
            kiFillBar.fillAmount = kiRatio;
        }
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
