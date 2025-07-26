using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class bossAiController : MonoBehaviour
{
    [Header("AI Settings")]
    public float moveSpeed = 2f;
    public float attackRange = 2.5f;
    public float attackCooldown = 2f;
    public float blockChance = 0.2f;
    public int maxHealth = 300;
    public Transform player;

    [Header("Ki Settings")]
    public int maxKi = 50;
    public int currentKi = 0;
    public int kiPerAttack = 5;

    [Header("Retreat Settings")]
    public float minDistanceToPlayer = 1.2f; // Có thể chỉnh trong Inspector

    public BossHitboxHandle hitBoxHandle; // Kéo thả object con chứa BossHitboxHandle vào đây trong Inspector
    public Transform hitBoxTransform;   // Kéo thả object con hitbox vào đây
    public Transform hurtBoxTransform;  // Kéo thả object con hurtbox vào đây
    public BoxCollider2D hitBoxCollider;

    [Header("Phase Settings")]
    public int currentPhase = 1;
    public float phase2HealthThreshold = 0.1f; // 10% máu để chuyển phase
    public int phase2HealthMultiplier = 2;     // Phase 2 có máu gấp đôi phase 1
    public float phase2DamageMultiplier = 1.5f; // Dame tăng 50% ở phase 2
    public float phase2SpeedMultiplier = 1.3f;  // Tốc độ tăng 30% ở phase 2

    [Header("Background Settings")]
    public GameObject phase1Background;
    public GameObject phase2Background;
    public SpriteRenderer[] backgroundLayers; // Các layer background cần thay đổi
    public Sprite[] phase2BackgroundSprites;  // Sprites mới cho phase 2

    [Header("Phase 2 Attack Patterns")]
    public float phase2AttackCooldownReduction = 0.3f; // Giảm 30% cooldown

    [Header("Phase 1 Settings")]
    public float phase1AttackTelegraphTime = 0.8f; // Thời gian "báo hiệu" trước khi tấn công
    public float phase1RecoveryMultiplier = 1.5f;  // Thời gian hồi phục lâu hơn sau đòn đánh
    public float phase1BlockDuration = 1.5f;       // Thời gian chặn lâu hơn (dễ counter)
    public float phase1StunDuration = 1.3f;        // Thời gian choáng lâu hơn

    [Header("Phase 2 Advanced Settings")]
    public bool enableCounterAttacks = true;       // Khả năng phản đòn
    public float counterAttackChance = 0.4f;       // Xác suất phản đòn
    public float desperationModeThreshold = 0.3f;  // Kích hoạt chế độ tuyệt vọng khi máu thấp
    public float desperationDamageMultiplier = 1.3f; // Sát thương tăng trong chế độ tuyệt vọng

    [Header("Phase Transition Settings")]
    public float invulnerabilityFlashDuration = 2f; // Duration of flashing before phase transition
    public float flashInterval = 0.2f; // How fast the colors flash
    public Color invulnerabilityColor1 = Color.red; // First flash color
    public Color invulnerabilityColor2 = Color.yellow; // Second flash color
    public float phaseTransitionAnimationTime = 3f; // Thời gian animation chuyển phase
    public float additionalInvulnerabilityTime = 0f; // Thời gian bất tử thêm sau khi animation kết thúc (mặc định 0)

    [Header("Boss Rage System")]
    [SerializeField] private int maxBossRage = 50; // Boss rage points maximum
    [SerializeField] private int currentBossRage = 0; // Current boss rage points
    [SerializeField] private int rageGainPerHit = 5; // Rage gained when boss hits player

    [Header("Attack Points & Effects")]
    public Transform attackPoint; // Attack point của boss
    public Transform attackPointUlti; // Attack point for ultimate attacks
    public GameObject bossEffectChemChuong; // Prefab Boss_EffectChemChuong
    public GameObject bossEffectUltimate; // Prefab for boss ultimate effect

    [Header("Ultimate Settings")]
    public float ultimateDistance = 5f; // Distance to player for ultimate (tùy chỉnh)
    public float attack3Distance = 20f;  // Distance to use attack3 (chém chưởng) - 20 đơn vị
    public float ultimateKiCost = 20f;   // Ki cost for ultimate
    public float attack3KiCost = 10f;    // Ki cost for attack3
    public float hitKiGain = 2f;         // Ki gained when hitting player

    [Header("Special States System")]
    public float noDamageTimeout = 10f;   // Thời gian không tương tác để kích hoạt trạng thái đặc biệt
    public float idleDuration = 2f;       // Thời gian idle
    public float freeTimeDuration = 2f;   // Thời gian freeTime

    [Header("Chase System")]
    public float chaseTime = 5f;          // Thời gian đuổi theo player
    public float chaseIdleDuration = 2f;  // Thời gian idle sau khi đuổi

    private bool isInvulnerable = false; // Boss is invulnerable during phase 1 transition
    private Color originalColor; // Store original sprite color

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    [SerializeField] private int currentHealth;
    private float lastAttackTime;
    private bool isStunned = false;
    private bool isBlocking = false;

    private float lastDamageInteractionTime = 0f; // Thời gian tương tác sát thương gần nhất
    private bool isInSpecialState = false;

    // Chase system variables
    private float chaseStartTime = 0f;
    private bool isChasing = false;
    private bool isChaseIdle = false;

    private Player playerScript;

    private float hitBoxOriginalPosX;
    private float hitBoxOriginalOffsetX;
    private float attackPointOriginalPosX;
    private float attackPointUltiOriginalPosX;

    private Dictionary<string, int> playerAttackPatterns = new Dictionary<string, int>();
    private string lastPlayerAttack = "";

    private bool isDead = false;
    private bool isUlti = false; // Ngăn di chuyển khi Ultimate

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public int CurrentKi => currentKi;
    public int MaxKi => maxKi;
    public int CurrentBossRage => currentBossRage;
    public int MaxBossRage => maxBossRage;

    public delegate void BossRageChangeHandler(int currentRage, int maxRage);
    public event BossRageChangeHandler OnBossRageChanged;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
        originalColor = spriteRenderer.color;

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null)
            playerScript = player.GetComponent<Player>();

        // Khởi tạo thời gian tương tác sát thương
        lastDamageInteractionTime = Time.time;

        animator.ResetTrigger("isDead");

        if (hitBoxTransform != null)
            hitBoxOriginalPosX = hitBoxTransform.localPosition.x;
        if (hitBoxCollider != null)
            hitBoxOriginalOffsetX = hitBoxCollider.offset.x;

        if (attackPoint != null)
            attackPointOriginalPosX = attackPoint.localPosition.x;
        if (attackPointUlti != null)
            attackPointUltiOriginalPosX = attackPointUlti.localPosition.x;

        if (hitBoxHandle != null)
            hitBoxHandle.FlipHitbox(!spriteRenderer.flipX);

        if (phase1Background != null) phase1Background.SetActive(true);
        if (phase2Background != null) phase2Background.SetActive(false);
    }

    void Update()
    {
        if (isDead) return;

        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Boss_Die") || isStunned || isBlocking || isInvulnerable) return;

        if (playerScript != null && playerScript.IsDead)
            return;

        if (currentPhase == 1 && currentHealth <= maxHealth * phase2HealthThreshold)
        {
            StartInvulnerabilityTransition();
            return;
        }

        // Kiểm tra nếu không có tương tác sát thương trong thời gian dài
        if (Time.time - lastDamageInteractionTime > noDamageTimeout && !isInSpecialState && !isUlti)
        {
            StartCoroutine(RandomSpecialState());
            return;
        }

        if (currentPhase == 2)
        {
            Phase2AI();
            return;
        }

        // Phase 1 AI
        Phase1AI();
    }

    private void Phase1AI()
    {
        float distanceToPlayer = player != null ? Vector2.Distance(transform.position, player.position) : Mathf.Infinity;

        // Nếu đang ở trạng thái đặc biệt, Ultimate, idle hoặc freeTime thì không di chuyển
        if (isInSpecialState || isUlti || animator.GetBool("isIdle") || animator.GetBool("freeTime"))
            return;

        // Nếu đang trong chase idle, không di chuyển
        if (isChaseIdle)
            return;

        // Nếu boss đứng quá sát player thì lùi lại
        if (distanceToPlayer < minDistanceToPlayer)
        {
            animator.SetBool("isMoving", true);
            float directionX = Mathf.Sign(transform.position.x - player.position.x);
            Vector3 move = new Vector3(directionX, 0, 0);
            transform.position += move * moveSpeed * Time.deltaTime;

            FlipBossDirection(directionX);
            return;
        }

        // Di chuyển liên tục về phía player nếu ngoài tầm tấn công
        if (distanceToPlayer > attackRange)
        {
            // Bắt đầu chase nếu chưa chase hoặc đã hết thời gian chase idle
            if (!isChasing)
            {
                isChasing = true;
                chaseStartTime = Time.time;
                Debug.Log("Boss started chasing player");
            }

            // Kiểm tra nếu đã chase quá 5 giây
            if (Time.time - chaseStartTime > chaseTime)
            {
                StartCoroutine(ChaseIdleRoutine());
                return;
            }

            animator.SetBool("isMoving", true);
            animator.SetBool("isIdle", false);
            animator.SetBool("freeTime", false);

            float directionX = Mathf.Sign(player.position.x - transform.position.x);
            Vector3 move = new Vector3(directionX, 0, 0);
            transform.position += move * moveSpeed * Time.deltaTime;

            FlipBossDirection(directionX);
        }
        else
        {
            // Đã đến gần player, reset chase
            isChasing = false;

            animator.SetBool("isMoving", false);
            animator.SetBool("isIdle", false);
            animator.SetBool("freeTime", false);

            // Tấn công nếu hết cooldown
            if (Time.time - lastAttackTime > attackCooldown)
            {
                StartCoroutine(Phase1AttackSequence());
            }
        }
    }

    // Chase idle routine
    private IEnumerator ChaseIdleRoutine()
    {
        isChasing = false;
        isChaseIdle = true;

        animator.SetBool("isMoving", false);
        animator.SetBool("isIdle", true);

        Debug.Log($"Boss entered chase idle for {chaseIdleDuration} seconds");
        yield return new WaitForSeconds(chaseIdleDuration);

        animator.SetBool("isIdle", false);
        isChaseIdle = false;

        Debug.Log("Boss finished chase idle, ready to chase again");
    }

    // Phase 1 attack sequence với 2 kịch bản
    private IEnumerator Phase1AttackSequence()
    {
        float distanceToPlayer = player != null ? Vector2.Distance(transform.position, player.position) : 0f;

        // Nếu player xa quá 20 đơn vị và đủ Ki thì dùng attack3
        if (distanceToPlayer > attack3Distance && currentKi >= attack3KiCost)
        {
            Debug.Log("Player too far, using attack3");

            // Telegraph effect
            spriteRenderer.color = Color.yellow;
            yield return new WaitForSeconds(phase1AttackTelegraphTime);
            spriteRenderer.color = originalColor;

            animator.SetTrigger("attack3");
            currentKi = Mathf.Max(0, currentKi - Mathf.FloorToInt(attack3KiCost));
            AddKi(kiPerAttack);

            Debug.Log($"Boss used attack3, spending {attack3KiCost} Ki. Ki remaining: {currentKi}");

            lastAttackTime = Time.time;
            lastDamageInteractionTime = Time.time;
            yield break;
        }

        // Telegraph effect cho attack sequence
        spriteRenderer.color = Color.yellow;
        yield return new WaitForSeconds(phase1AttackTelegraphTime);
        spriteRenderer.color = originalColor;

        // Random giữa 2 kịch bản
        if (Random.value < 0.5f)
        {
            // Kịch bản 1: 2 lần chiêu 1 + 1 lần chiêu 2
            Debug.Log("Boss executing attack sequence 1: attack1 -> attack1 -> attack2");

            animator.SetTrigger("attack1");
            AddKi(kiPerAttack);
            yield return new WaitForSeconds(0.5f);

            animator.SetTrigger("attack1");
            AddKi(kiPerAttack);
            yield return new WaitForSeconds(0.5f);

            animator.SetTrigger("attack2");
            AddKi(kiPerAttack);
        }
        else
        {
            // Kịch bản 2: 1 lần chiêu 1 + 1 lần chiêu 2 + 1 lần chiêu 1 + 1 lần chiêu 2
            Debug.Log("Boss executing attack sequence 2: attack1 -> attack2 -> attack1 -> attack2");

            animator.SetTrigger("attack1");
            AddKi(kiPerAttack);
            yield return new WaitForSeconds(0.5f);

            animator.SetTrigger("attack2");
            AddKi(kiPerAttack);
            yield return new WaitForSeconds(0.5f);

            animator.SetTrigger("attack1");
            AddKi(kiPerAttack);
            yield return new WaitForSeconds(0.5f);

            animator.SetTrigger("attack2");
            AddKi(kiPerAttack);
        }

        lastAttackTime = Time.time;
        lastDamageInteractionTime = Time.time;
    }

    private void Phase2AI()
    {
        if (isDead) return;

        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Boss_Die") || isStunned || isBlocking) return;
        if (playerScript != null && playerScript.IsDead) return;
        if (isInSpecialState) return;

        // Nếu đang Ultimate, idle hoặc freeTime thì KHÔNG được di chuyển
        if (isUlti || animator.GetBool("isIdle") || animator.GetBool("freeTime"))
        {
            Debug.Log("Boss is in ultimate, idle, or freeTime state - cannot move");
            return;
        }

        float distanceToPlayer = player != null ? Vector2.Distance(transform.position, player.position) : Mathf.Infinity;
        float phase2Speed = moveSpeed * phase2SpeedMultiplier;

        // Nếu boss đứng quá sát player thì lùi lại
        if (distanceToPlayer < minDistanceToPlayer * 1.2f)
        {
            animator.SetBool("isMoving", true);
            float directionX = Mathf.Sign(transform.position.x - player.position.x);
            Vector3 move = new Vector3(directionX, 0, 0);
            transform.position += move * phase2Speed * 1.2f * Time.deltaTime;

            FlipBossDirection(directionX);
            return;
        }

        // Di chuyển thông minh hơn - giữ khoảng cách tối ưu
        float optimalDistance = attackRange * 0.8f;

        if (Mathf.Abs(distanceToPlayer - optimalDistance) > 0.5f)
        {
            animator.SetBool("isMoving", true);
            float directionX = distanceToPlayer > optimalDistance ?
                Mathf.Sign(player.position.x - transform.position.x) :
                Mathf.Sign(transform.position.x - player.position.x);

            Vector3 move = new Vector3(directionX * (distanceToPlayer > optimalDistance ? 1 : -1), 0, 0);
            transform.position += move * phase2Speed * Time.deltaTime;

            FlipBossDirection(Mathf.Sign(player.position.x - transform.position.x));
        }
        else
        {
            animator.SetBool("isMoving", false);

            // Tấn công với cooldown giảm ở phase 2
            if (Time.time - lastAttackTime > attackCooldown * (1 - phase2AttackCooldownReduction))
            {
                ExecutePhase2Attack();
                lastAttackTime = Time.time;
                // Cập nhật thời gian tương tác khi tấn công
                lastDamageInteractionTime = Time.time;
            }
        }
    }

    // Random special state system
    private IEnumerator RandomSpecialState()
    {
        isInSpecialState = true;
        animator.SetBool("isMoving", false);

        // Random giữa idle và freeTime
        if (Random.value < 0.5f)
        {
            // Idle state
            animator.SetBool("isIdle", true);
            animator.SetBool("freeTime", false);
            Debug.Log($"Boss entered idle state for {idleDuration} seconds (no damage interaction)");
            yield return new WaitForSeconds(idleDuration);
            animator.SetBool("isIdle", false);
        }
        else
        {
            // FreeTime state
            animator.SetBool("freeTime", true);
            animator.SetBool("isIdle", false);
            Debug.Log($"Boss entered freeTime state for {freeTimeDuration} seconds (no damage interaction)");
            yield return new WaitForSeconds(freeTimeDuration);
            animator.SetBool("freeTime", false);
        }

        isInSpecialState = false;

        // Reset thời gian tương tác sau khi kết thúc trạng thái đặc biệt
        lastDamageInteractionTime = Time.time;
        Debug.Log("Special state ended - damage interaction timer reset");
    }

    // Giữ lại method cũ cho logic không tiếp xúc với player (legacy)
    private IEnumerator SpecialStateRoutine()
    {
        isInSpecialState = true;
        animator.SetBool("isMoving", false);

        animator.SetBool("freeTime", true);
        animator.SetBool("isIdle", false);
        Debug.Log($"Boss entered freeTime state for {freeTimeDuration} seconds (no player contact)");
        yield return new WaitForSeconds(freeTimeDuration);
        animator.SetBool("freeTime", false);

        isInSpecialState = false;
    }

    // Giữ lại method cũ để tương thích (deprecated)
    private IEnumerator TelegraphAttack()
    {
        yield return StartCoroutine(Phase1AttackSequence());
    }

    private void ExecutePhase2Attack()
    {
        if (isUlti || animator.GetBool("isIdle") || animator.GetBool("freeTime")) return;

        float distanceToPlayer = player != null ? Vector2.Distance(transform.position, player.position) : 0f;

        // Logic thông minh hơn cho Phase 2
        // 1. Nếu player xa và đủ Ki -> dùng attack3 (chém chưởng) để lại gần
        if (currentKi >= attack3KiCost && distanceToPlayer > attack3Distance)
        {
            if (Random.value < 0.7f) // 70% xác suất dùng attack3 khi player xa
            {
                animator.SetTrigger("attack3");
                currentKi = Mathf.Max(0, currentKi - Mathf.FloorToInt(attack3KiCost));
                Debug.Log($"Boss used attack3 to close distance, spending {attack3KiCost} Ki. Ki remaining: {currentKi}");
                AddKi(kiPerAttack);
                return;
            }
        }

        // 2. Nếu player gần và đủ Ki -> có thể dùng Ultimate nhưng không phải lúc nào cũng dùng
        if (currentKi >= ultimateKiCost && distanceToPlayer <= ultimateDistance)
        {
            // Chỉ dùng Ultimate khi cần thiết (máu thấp hoặc xác suất thấp)
            bool shouldUseUltimate = false;

            if (currentHealth < (maxHealth * phase2HealthMultiplier * 0.5f)) // Máu dưới 50%
            {
                shouldUseUltimate = Random.value < 0.4f; // 40% xác suất khi máu thấp
            }
            else
            {
                shouldUseUltimate = Random.value < 0.15f; // 15% xác suất khi máu cao
            }

            if (shouldUseUltimate)
            {
                StartCoroutine(PrepareAndExecuteUltimate());
                return;
            }
        }

        // 3. Nếu boss đang ở chế độ tuyệt vọng
        bool isInDesperationMode = currentHealth < (maxHealth * phase2HealthMultiplier * desperationModeThreshold);
        if (isInDesperationMode && Random.value < 0.6f)
        {
            StartCoroutine(DesperationAttack());
            return;
        }

        // 4. Tấn công bình thường
        float rand = Random.value;
        if (rand < 0.6f)
        {
            animator.SetTrigger("attack1");
            Debug.Log("Boss used attack1 in phase 2");
        }
        else
        {
            animator.SetTrigger("attack2");
            Debug.Log("Boss used attack2 in phase 2");
        }

        AddKi(kiPerAttack);
    }

    private IEnumerator PrepareAndExecuteUltimate()
    {
        Debug.Log("Starting Ultimate Attack");
        isUlti = true; // Ngăn di chuyển

        // Ngừng di chuyển
        animator.SetBool("isMoving", false);

        // Đợi một chút để chuẩn bị
        yield return new WaitForSeconds(0.5f);

        // Kích hoạt Ultimate
        animator.SetBool("isUlti", true);
        currentKi = Mathf.Max(0, currentKi - Mathf.FloorToInt(ultimateKiCost));

        Debug.Log($"Boss executed ultimate attack, spending {ultimateKiCost} Ki. Ki remaining: {currentKi}");

        // Đợi animation kết thúc (có thể điều chỉnh thời gian này)
        yield return new WaitForSeconds(3f);

        // Reset states
        animator.SetBool("isUlti", false);
        isUlti = false;

        // Cập nhật thời gian tương tác khi sử dụng Ultimate
        lastDamageInteractionTime = Time.time;

        Debug.Log("Ultimate attack complete - boss can move again");
    }

    // Helper để lật hướng boss
    private void FlipBossDirection(float directionX)
    {
        if (directionX != 0)
        {
            spriteRenderer.flipX = directionX < 0;
            if (hitBoxHandle != null)
                hitBoxHandle.FlipHitbox(!spriteRenderer.flipX);

            BossHurtboxHandle hurtBoxHandle = hurtBoxTransform?.GetComponent<BossHurtboxHandle>();
            if (hurtBoxHandle != null)
                hurtBoxHandle.FlipHurtbox(!spriteRenderer.flipX);

            FlipBossAttackPoint();
        }
    }

    // Hàm lật attackPoint theo hướng của boss
    private void FlipBossAttackPoint()
    {
        if (attackPoint != null)
        {
            if (spriteRenderer.flipX)
            {
                attackPoint.localPosition = new Vector3(
                    -Mathf.Abs(attackPointOriginalPosX),
                    attackPoint.localPosition.y,
                    attackPoint.localPosition.z);
            }
            else
            {
                attackPoint.localPosition = new Vector3(
                    Mathf.Abs(attackPointOriginalPosX),
                    attackPoint.localPosition.y,
                    attackPoint.localPosition.z);
            }
        }

        if (attackPointUlti != null)
        {
            if (spriteRenderer.flipX)
            {
                attackPointUlti.localPosition = new Vector3(
                    -Mathf.Abs(attackPointUltiOriginalPosX),
                    attackPointUlti.localPosition.y,
                    attackPointUlti.localPosition.z);
            }
            else
            {
                attackPointUlti.localPosition = new Vector3(
                    Mathf.Abs(attackPointUltiOriginalPosX),
                    attackPointUlti.localPosition.y,
                    attackPointUlti.localPosition.z);
            }
        }
    }

    // Phần còn lại của code giữ nguyên...
    private void StartInvulnerabilityTransition()
    {
        if (isInvulnerable) return;

        Debug.Log("Starting invulnerability transition");
        isInvulnerable = true;

        DisableAllColliders();
        DisableHurtbox();

        animator.SetBool("isIdle", true);
        animator.SetBool("isMoving", false);
        animator.SetBool("isBlocking", false);
        animator.SetBool("isStunned", false);
        animator.SetBool("freeTime", false);

        StopAllCoroutines();
        isBlocking = false;
        isStunned = false;

        StartCoroutine(InvulnerabilityFlashEffect());
    }

    private void DisableHurtbox()
    {
        if (hurtBoxTransform != null)
        {
            Collider2D hurtBoxCollider = hurtBoxTransform.GetComponent<Collider2D>();
            if (hurtBoxCollider != null)
            {
                hurtBoxCollider.enabled = false;
                Debug.Log("Hurtbox disabled for invulnerability");
            }
        }
    }

    private void EnableHurtbox()
    {
        if (hurtBoxTransform != null)
        {
            Collider2D hurtBoxCollider = hurtBoxTransform.GetComponent<Collider2D>();
            if (hurtBoxCollider != null)
            {
                hurtBoxCollider.enabled = true;
                Debug.Log("Hurtbox re-enabled for phase 2");
            }
        }
    }

    private IEnumerator InvulnerabilityFlashEffect()
    {
        float elapsed = 0f;
        bool useColor1 = true;

        while (elapsed < invulnerabilityFlashDuration)
        {
            spriteRenderer.color = useColor1 ? invulnerabilityColor1 : invulnerabilityColor2;
            useColor1 = !useColor1;

            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        TransitionToPhase2();
    }

    private void TransitionToPhase2()
    {
        currentPhase = 2;
        // KHÔNG đặt isInvulnerable = false ở đây
        StartCoroutine(Phase2TransitionEffect());
    }

    private IEnumerator Phase2TransitionEffect()
    {
        SetAIActive(false);

        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Boss_Die"))
        {
            animator.ResetTrigger("isDead");
        }

        animator.SetTrigger("phaseTransition");
        yield return new WaitForSeconds(phaseTransitionAnimationTime);

        if (phase1Background != null && phase2Background != null)
        {
            phase1Background.SetActive(false);
            phase2Background.SetActive(true);
        }

        if (backgroundLayers != null && phase2BackgroundSprites != null)
        {
            for (int i = 0; i < backgroundLayers.Length && i < phase2BackgroundSprites.Length; i++)
            {
                backgroundLayers[i].sprite = phase2BackgroundSprites[i];
            }
        }

        // Cập nhật maxHealth cho phase 2
        maxHealth = maxHealth * phase2HealthMultiplier;
        currentHealth = maxHealth;
        
        Debug.Log($"Phase 2: Boss max health increased to {maxHealth}");
        
        spriteRenderer.color = new Color(1f, 0.5f, 0.5f);

        animator.SetBool("isStunned", false);
        animator.SetBool("isBlocking", false);
        animator.SetBool("isIdle", false);
        animator.SetBool("freeTime", false);
        animator.SetBool("isMoving", true);
        isStunned = false;
        isBlocking = false;

        // Bật lại colliders trước khi tắt bất tử
        EnableAllColliders();
        EnableHurtbox();
        
        // CHỈ tắt bất tử sau khi đã bật colliders
        isInvulnerable = false;
        Debug.Log("Phase 2 transition complete - boss is now vulnerable again");

        SetAIActive(true);
    }

    private void EnableAllColliders()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = true;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead ||
            isInvulnerable ||
            animator.GetCurrentAnimatorStateInfo(0).IsName("Boss_Die") ||
            (currentPhase == 1 && currentHealth <= maxHealth * phase2HealthThreshold))
        {
            Debug.Log($"Boss taking no damage - Dead: {isDead}, Invulnerable: {isInvulnerable}, Phase: {currentPhase}, Health: {currentHealth}");
            return;
        }

        if (isBlocking)
        {
            damage = Mathf.FloorToInt(damage / 3f);
            Debug.Log($"Boss blocked! Reduced damage to {damage}");
        }

        if (currentPhase == 1)
        {
            animator.ResetTrigger("isDead");

            int thresholdHealth = Mathf.CeilToInt(maxHealth * phase2HealthThreshold);

            if (currentHealth - damage <= thresholdHealth)
            {
                currentHealth = thresholdHealth;
                Debug.Log("Phase 1 threshold reached - starting invulnerability");
                StartInvulnerabilityTransition();
                return;
            }

            if (currentHealth <= thresholdHealth)
            {
                Debug.Log("Already at threshold - starting invulnerability");
                StartInvulnerabilityTransition();
                return;
            }
        }

        if (currentPhase == 2)
        {
            damage = Mathf.FloorToInt(damage * 0.8f);
        }

        currentHealth -= damage;
        AddKi(kiPerAttack);

        // Cập nhật thời gian tương tác khi nhận sát thương
        lastDamageInteractionTime = Time.time;

        Debug.Log($"Boss took {damage} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0 && currentPhase == 2)
        {
            Die();
            return;
        }

        if (!isInvulnerable && !isDead && currentPhase == 2)
        {
            float rand = Random.value;
            float reactionThreshold = 0.35f;

            if (rand < reactionThreshold)
            {
                if (rand < reactionThreshold / 2)
                {
                    isBlocking = true;
                    animator.SetBool("isBlocking", true);
                    Invoke(nameof(StopBlocking), 0.7f);
                }
                else
                {
                    StartCoroutine(Stun(0.6f));
                }
            }
        }
    }

    private void Die()
    {
        Debug.Log($"Die() called at phase {currentPhase} with health {currentHealth}");

        if (currentPhase == 1)
        {
            Debug.LogWarning("Die() called in phase 1 - starting invulnerability transition");
            currentHealth = Mathf.CeilToInt(maxHealth * phase2HealthThreshold);
            animator.ResetTrigger("isDead");
            StartInvulnerabilityTransition();
            return;
        }

        if (isDead)
        {
            Debug.Log("Boss is already dead - ignoring duplicate Die() call");
            return;
        }

        isDead = true;
        SetAIActive(false);
        DisableAllColliders();
        animator.SetTrigger("isDead");
        NotifyVictory();
    }

    private void DisableAllColliders()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = false;
        }

        DisableBossHitbox();
    }

    private void NotifyVictory()
    {
        Debug.Log("Boss defeated - showing victory screen");
        GameManager.Instance?.Victory();
    }

    private void StopBlocking()
    {
        isBlocking = false;
        animator.SetBool("isBlocking", false);
    }

    private IEnumerator Stun(float duration)
    {
        isStunned = true;
        animator.SetBool("isStunned", true);
        yield return new WaitForSeconds(duration);
        animator.SetBool("isStunned", false);
        isStunned = false;
    }

    private void AddKi(int amount)
    {
        currentKi = Mathf.Clamp(currentKi + amount, 0, maxKi);
    }

    public void EnableBossHitbox()
    {
        if (hitBoxHandle != null)
        {
            hitBoxHandle.EnableHitbox();
            hitBoxHandle.FlipHitbox(!spriteRenderer.flipX);
        }
    }

    public void DisableBossHitbox()
    {
        if (hitBoxHandle != null)
            hitBoxHandle.DisableHitbox();
    }

    public void SetAIActive(bool isActive)
    {
        this.enabled = isActive;
        if (!isActive)
        {
            animator.SetBool("isMoving", false);
        }
    }

    private void LateUpdate()
    {
        if (currentPhase == 1 && !isDead)
        {
            animator.ResetTrigger("isDead");

            int thresholdHealth = Mathf.CeilToInt(maxHealth * phase2HealthThreshold);
            if (currentHealth <= thresholdHealth && !isInvulnerable)
            {
                Debug.LogWarning("LateUpdate: Health at threshold - forcing invulnerability");
                StartInvulnerabilityTransition();
            }

            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Boss_Die"))
            {
                Debug.LogWarning("LateUpdate: Death animation in phase 1 - forcing invulnerability");
                StartInvulnerabilityTransition();
            }
        }

        FlipBossAttackPoint();
    }

    public void RegisterPlayerAttack(string attackType)
    {
        lastPlayerAttack = attackType;

        if (!playerAttackPatterns.ContainsKey(attackType))
            playerAttackPatterns[attackType] = 0;

        playerAttackPatterns[attackType]++;
    }

    private IEnumerator DesperationAttack()
    {
        spriteRenderer.color = new Color(1f, 0.3f, 0.3f);

        float delayBetween = 0.3f;
        int[] sequence = new int[] { 1, 2, 3, 3 };

        foreach (int type in sequence)
        {
            animator.SetTrigger("attack" + type);
            AddKi(kiPerAttack);
            yield return new WaitForSeconds(delayBetween);
        }

        spriteRenderer.color = new Color(1f, 0.5f, 0.5f);
    }

    public void OnPlayerDamageDealt(int damageAmount)
    {
        int oldRage = currentBossRage;
        currentBossRage = Mathf.Min(currentBossRage + rageGainPerHit, maxBossRage);

        if (currentBossRage != oldRage)
        {
            Debug.Log($"Boss gained rage: +{rageGainPerHit}. Current boss rage: {currentBossRage}/{maxBossRage}");
            OnBossRageChanged?.Invoke(currentBossRage, maxBossRage);
        }
    }

    public bool TryUseRage(int rageCost)
    {
        if (currentBossRage >= rageCost)
        {
            currentBossRage -= rageCost;
            Debug.Log($"Boss used {rageCost} rage. Remaining: {currentBossRage}/{maxBossRage}");
            OnBossRageChanged?.Invoke(currentBossRage, maxBossRage);
            return true;
        }
        return false;
    }

    public void SpawnBossChemChuong()
    {
        if (bossEffectChemChuong != null && attackPoint != null)
        {
            GameObject effect = Instantiate(bossEffectChemChuong, attackPoint.position, Quaternion.identity);
            Debug.Log("Boss ChemChuong spawned!");
        }
    }

    public void SpawnBossChemChuongFromAnimator()
    {
        SpawnBossChemChuong();
        Debug.Log("Boss ChemChuong spawned from animator!");
    }

    public void SpawnBossUltimateEffect()
    {
        if (bossEffectUltimate != null && attackPointUlti != null)
        {
            GameObject effect = Instantiate(bossEffectUltimate, attackPointUlti.position, Quaternion.identity);
            Debug.Log("Boss Ultimate effect spawned!");
        }
    }

    public void SpawnBossUltimateFromAnimator()
    {
        SpawnBossUltimateEffect();
        Debug.Log("Boss Ultimate spawned from animator!");
    }

    public void OnSuccessfulHitPlayer()
    {
        AddKi(Mathf.FloorToInt(hitKiGain));

        // Cập nhật thời gian tương tác khi đánh trúng player
        lastDamageInteractionTime = Time.time;

        Debug.Log($"Boss gained {hitKiGain} Ki from hitting player! Current Ki: {currentKi}");
    }

    public void ForceResetBossState()
    {
        Debug.Log("Force resetting boss state");
        isUlti = false;
        isInSpecialState = false;
        isChasing = false;
        isChaseIdle = false;

        animator.SetBool("isUlti", false);
        animator.SetBool("isMoving", false);
        animator.SetBool("isIdle", false);
        animator.SetBool("freeTime", false);

        // Reset thời gian tương tác
        lastDamageInteractionTime = Time.time;

        StopCoroutine("PrepareAndExecuteUltimate");
        StopCoroutine("RandomSpecialState");
        StopCoroutine("SpecialStateRoutine");
        StopCoroutine("ChaseIdleRoutine");
        StopCoroutine("Phase1AttackSequence");
    }
}