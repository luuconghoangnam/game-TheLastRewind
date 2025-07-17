using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Boss2AiController : MonoBehaviour
{
    [Header("AI Settings")]
    public float moveSpeed = 3f; // Nhanh hơn boss 1
    public float attackRange = 3f; // Tầm đánh xa hơn
    public float attackCooldown = 1.5f; // Tấn công nhanh hơn
    public int maxHealth = 400; // Máu nhiều hơn boss 1
    public Transform player;

    [Header("Level 2 Features")]
    public int currentPhase = 1;
    public float phase2HealthThreshold = 0.6f; // 60% health
    public float phase3HealthThreshold = 0.25f; // 25% health
    public bool canTeleport = true;
    public float teleportCooldown = 5f;
    public float teleportRange = 8f;

    [Header("Enhanced Combat")]
    public int multiHitAttackCount = 3; // Đánh combo nhiều đòn
    public float comboDelay = 0.3f;
    public bool canSummonMinions = true;
    public GameObject minionPrefab;
    public int maxMinions = 2;

    [Header("Rage System")]
    public int maxBossRage = 80; // Rage cao hơn boss 1
    public int currentBossRage = 0;
    public int rageGainPerHit = 8;
    public int minRageForSpecialAttack = 40;

    // Components
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private PlayerLevel2 playerLevel2;

    // State
    [SerializeField] private int currentHealth;
    private float lastAttackTime;
    private float lastTeleportTime;
    private bool isStunned = false;
    private bool isBlocking = false;
    private bool isDead = false;
    private int currentMinionCount = 0;

    // Hitbox references
    public BossHitboxHandle hitBoxHandle;
    public Transform hitBoxTransform;
    public Transform hurtBoxTransform;

    // Properties
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public int CurrentBossRage => currentBossRage;
    public int MaxBossRage => maxBossRage;

    // Events
    public delegate void HealthChangeHandler(int currentHealth, int maxHealth);
    public event HealthChangeHandler OnHealthChanged;

    public delegate void PhaseChangeHandler(int phase);
    public event PhaseChangeHandler OnPhaseChanged;

    public delegate void BossRageChangeHandler(int currentRage, int maxRage);
    public event BossRageChangeHandler OnBossRageChanged;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;

        // Tìm PlayerLevel2 thay vì Player
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null)
            playerLevel2 = player.GetComponent<PlayerLevel2>();

        // Trigger initial events
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnPhaseChanged?.Invoke(currentPhase);
    }

    void Update()
    {
        if (isDead) return;

        // Kiểm tra chuyển phase
        CheckPhaseTransition();

        // AI logic theo phase
        switch (currentPhase)
        {
            case 1:
                Phase1AI();
                break;
            case 2:
                Phase2AI();
                break;
            case 3:
                Phase3AI();
                break;
        }
    }

    private void CheckPhaseTransition()
    {
        float healthPercentage = (float)currentHealth / maxHealth;
        
        if (currentPhase == 1 && healthPercentage <= phase2HealthThreshold)
        {
            TransitionToPhase2();
        }
        else if (currentPhase == 2 && healthPercentage <= phase3HealthThreshold)
        {
            TransitionToPhase3();
        }
    }

    private void TransitionToPhase2()
    {
        currentPhase = 2;
        OnPhaseChanged?.Invoke(currentPhase);
        
        // Tăng sức mạnh
        attackCooldown *= 0.8f; // Tấn công nhanh hơn 20%
        moveSpeed *= 1.2f; // Di chuyển nhanh hơn 20%
        
        animator.SetTrigger("PhaseTransition");
        Debug.Log("Boss entered Phase 2!");
    }

    private void TransitionToPhase3()
    {
        currentPhase = 3;
        OnPhaseChanged?.Invoke(currentPhase);
        
        // Tăng sức mạnh tối đa
        attackCooldown *= 0.6f; // Tấn công nhanh hơn 40%
        moveSpeed *= 1.5f; // Di chuyển nhanh hơn 50%
        
        animator.SetTrigger("PhaseTransition");
        Debug.Log("Boss entered Phase 3 - BERSERKER MODE!");
    }

    private void Phase1AI()
    {
        // AI cơ bản giống boss 1 nhưng cải tiến
        BasicAI();
    }

    private void Phase2AI()
    {
        // AI nâng cao: có thể teleport và combo attack
        BasicAI();
        
        // Teleport ability
        if (canTeleport && Time.time - lastTeleportTime > teleportCooldown)
        {
            if (Vector2.Distance(transform.position, player.position) > 6f)
            {
                TeleportToPlayer();
            }
        }
        
        // Summon minions
        if (canSummonMinions && currentMinionCount < maxMinions && Random.value < 0.01f)
        {
            SummonMinion();
        }
    }

    private void Phase3AI()
    {
        // AI tấn công liên tục, berserker mode
        BasicAI();
        
        // Berserker attacks
        if (Time.time - lastAttackTime > attackCooldown * 0.5f)
        {
            StartCoroutine(BerserkerCombo());
        }
    }

    private void BasicAI()
    {
        if (isStunned || isBlocking) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            // Tấn công
            if (Time.time - lastAttackTime > attackCooldown)
            {
                PerformAttack();
            }
        }
        else
        {
            // Di chuyển về phía player
            MoveTowardPlayer();
        }
    }

    private void MoveTowardPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
        
        // Flip sprite
        spriteRenderer.flipX = direction.x < 0;
        
        animator.SetBool("IsMoving", true);
    }

    private void PerformAttack()
    {
        animator.SetBool("IsMoving", false);
        
        // Chọn loại tấn công dựa trên phase
        int attackType = currentPhase switch
        {
            1 => Random.Range(1, 3), // Attack 1-2
            2 => Random.Range(1, 4), // Attack 1-3
            3 => Random.Range(1, 5), // Attack 1-4
            _ => 1
        };
        
        animator.SetTrigger("Attack" + attackType);
        lastAttackTime = Time.time;
        
        // Tăng rage khi tấn công
        AddRage(rageGainPerHit);
    }

    private void TeleportToPlayer()
    {
        // Tính vị trí teleport gần player
        Vector2 teleportPos = (Vector2)player.position + Random.insideUnitCircle * 2f;
        
        // Effect trước khi teleport
        animator.SetTrigger("TeleportOut");
        
        StartCoroutine(TeleportCoroutine(teleportPos));
    }

    private System.Collections.IEnumerator TeleportCoroutine(Vector2 targetPos)
    {
        // Biến mất
        spriteRenderer.color = new Color(1, 1, 1, 0.5f);
        
        yield return new WaitForSeconds(0.5f);
        
        // Teleport
        transform.position = targetPos;
        
        // Xuất hiện
        animator.SetTrigger("TeleportIn");
        spriteRenderer.color = Color.white;
        
        lastTeleportTime = Time.time;
        
        Debug.Log("Boss teleported!");
    }

    private void SummonMinion()
    {
        if (minionPrefab != null)
        {
            Vector2 spawnPos = (Vector2)transform.position + Random.insideUnitCircle * 3f;
            GameObject minion = Instantiate(minionPrefab, spawnPos, Quaternion.identity);
            
            currentMinionCount++;
            
            // Giảm minion count khi minion chết
            StartCoroutine(TrackMinionLife(minion));
            
            Debug.Log("Minion summoned!");
        }
    }

    private System.Collections.IEnumerator TrackMinionLife(GameObject minion)
    {
        yield return new WaitUntil(() => minion == null);
        currentMinionCount--;
    }

    private System.Collections.IEnumerator BerserkerCombo()
    {
        for (int i = 0; i < multiHitAttackCount; i++)
        {
            animator.SetTrigger("Attack" + Random.Range(1, 4));
            yield return new WaitForSeconds(comboDelay);
        }
        
        lastAttackTime = Time.time;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Reaction dựa trên phase
            float reactionChance = currentPhase switch
            {
                1 => 0.3f,
                2 => 0.5f,
                3 => 0.7f,
                _ => 0.3f
            };
            
            if (Random.value < reactionChance)
            {
                animator.SetTrigger("TakeDamage");
            }
        }
    }

    private void Die()
    {
        isDead = true;
        animator.SetTrigger("Die");
        
        // Disable AI
        enabled = false;
        
        // Notify victory
        GameManager.Instance?.Victory();
        
        Debug.Log("Boss Level 2 defeated!");
    }

    public void AddRage(int amount)
    {
        currentBossRage = Mathf.Min(currentBossRage + amount, maxBossRage);
        OnBossRageChanged?.Invoke(currentBossRage, maxBossRage);
    }

    public void EnableBossHitbox()
    {
        if (hitBoxHandle != null)
        {
            hitBoxHandle.EnableHitbox();
        }
    }

    public void DisableBossHitbox()
    {
        if (hitBoxHandle != null)
        {
            hitBoxHandle.DisableHitbox();
        }
    }
}