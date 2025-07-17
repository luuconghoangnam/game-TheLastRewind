using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarControllerLevel2 : MonoBehaviour
{
    [Header("Health Bar Components")]
    public Slider healthSlider;
    public Image fillImage;
    public Image frameImage;
    
    [Header("Level 2 Features")]
    public Color phase1Color = Color.red;
    public Color phase2Color = Color.magenta; // Thay purple bằng magenta
    public Color phase3Color = Color.black; // Thêm phase 3 cho boss level 2
    public float animationSpeed = 2f;
    
    [Header("Boss Reference")]
    public Boss2AiController boss2Controller; // Tham chiếu boss level 2
    
    private float targetHealth;
    private bool isAnimating = false;
    
    void Start()
    {
        if (boss2Controller != null)
        {
            // Đăng ký events từ boss level 2
            boss2Controller.OnHealthChanged += UpdateHealthBar;
            boss2Controller.OnPhaseChanged += UpdatePhaseColor;
            
            // Khởi tạo health bar
            UpdateHealthBar(boss2Controller.CurrentHealth, boss2Controller.MaxHealth);
        }
        else
        {
            Debug.LogError("Boss2Controller reference is missing!");
        }
    }
    
    void OnDestroy()
    {
        if (boss2Controller != null)
        {
            boss2Controller.OnHealthChanged -= UpdateHealthBar;
            boss2Controller.OnPhaseChanged -= UpdatePhaseColor;
        }
    }
    
    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (healthSlider != null)
        {
            targetHealth = (float)currentHealth / maxHealth;
            
            if (!isAnimating)
            {
                StartCoroutine(AnimateHealthBar());
            }
        }
    }
    
    private System.Collections.IEnumerator AnimateHealthBar()
    {
        isAnimating = true;
        float startValue = healthSlider.value;
        float elapsedTime = 0f;
        
        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime * animationSpeed;
            healthSlider.value = Mathf.Lerp(startValue, targetHealth, elapsedTime);
            yield return null;
        }
        
        healthSlider.value = targetHealth;
        isAnimating = false;
    }
    
    public void UpdatePhaseColor(int phase)
    {
        if (fillImage != null)
        {
            Color newColor = phase switch
            {
                1 => phase1Color,
                2 => phase2Color,
                3 => phase3Color,
                _ => phase1Color
            };
            
            fillImage.color = newColor;
            Debug.Log($"Health bar color changed to phase {phase}");
        }
    }
}