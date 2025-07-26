using UnityEngine;
using UnityEngine.UI;

public class CloneHealthBarController : MonoBehaviour
{
    [SerializeField] private Image healthFillBar; // Reference to FillBar máu clone
    [SerializeField] private Image frameBar;      // Reference to Frame Bar máu clone
    [SerializeField] private CloneController clone; // Reference to CloneController script

    private void Start()
    {
        if (healthFillBar == null)
            Debug.LogError("Health Fill Bar is not assigned!");
        if (clone == null)
            Debug.LogError("CloneController is not assigned!");

        UpdateHealthBar();
    }

    private void Update()
    {
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (healthFillBar != null && clone != null)
        {
            float healthRatio = (float)clone.currentHealth / clone.maxHealth;
            healthFillBar.fillAmount = healthRatio;
        }
    }

    public void ReduceHealth(int amount)
    {
        if (clone != null)
        {
            clone.TakeDamage(amount);
            UpdateHealthBar();
        }
    }
}