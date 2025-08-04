using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject gameOverPanel;
    public GameObject victoryPanel;
    public GameObject settingsPanel;

    [Header("Dialogue Banner")]
    public GameObject dialogueBanner;
    public TextMeshProUGUI dialogueText;
    public float textSpeed = 0.05f;

    [Header("Dialogue Messages")]
    public string level1DialogueMessage = "It you!...Remember me?...\nI guess NOT!";
    public string level1PostBossDialogue = "I once was harsh, too proud to speak\nYour silent pain—my heart grew weak.";
    // ===== THÊM: Level 1 Boss Phase 2 Transition Dialogue =====
    public string level1BossPhase2Dialogue = "ENOUGH! You will witness my TRUE POWER!\nThis is where your journey ENDS!";

    // ===== THÊM: Level 2 dialogue messages =====
    [Header("Level 2 Dialogue Messages")]
    public string level2BossIntroDialogue = "You, the one I loved, once was.\nBut now, you are the one I hate the most.";
    public string level2BossReappearDialogue = "You think defeating my minions means victory?\nNow face my true wrath!";
    public string level2PostBossDialogue = "You stood alone beside the door,\nI let you drift, ignored once more.";

    [Header("Victory Panel Buttons")]
    public Button continueButton;
    public Button saveGameButton;
    public Button victoryClearSaveButton; // ===== THÊM: Clear Save button cho Victory Panel =====
    public Button victorySettingsButton;
    public Button victoryQuitButton;
    public TextMeshProUGUI victorySaveInfoText; // ===== THÊM: Save Info text cho Victory Panel =====

    [Header("Game Over Panel Buttons")]
    public Button tryAgainButton;
    public Button mainMenuButton;
    public Button gameOverSettingsButton;
    public Button exitButton;

    [Header("Settings Panel")]
    public Button settingsMainMenuButton;
    public Button settingsExitButton;
    public Button saveGameSettingsButton;
    public Button loadGameButton;
    public Button clearSaveButton; // ===== THÊM: Button để clear save game =====
    public Toggle audioToggle;
    public Image audioToggleImage;
    public Sprite audioOnSprite;
    public Sprite audioOffSprite;
    public TextMeshProUGUI saveInfoText; // ===== THÊM: Text hiển thị thông tin save game =====

    // ===== SỬA: Enhanced Audio System =====
    [Header("Audio System")]
    public AudioSource musicSource; // Audio source cho background music
    public AudioSource fadeAudioSource; // Audio source thứ hai cho crossfade effect
    
    [Header("Level 1 Music")]
    public AudioClip level1NormalMusic; // Nhạc nền Level 1 bình thường
    public AudioClip level1BossPhase1Music; // Nhạc boss Level 1 Phase 1
    public AudioClip level1BossPhase2Music; // Nhạc boss Level 1 Phase 2
    
    [Header("Level 2 Music")]
    public AudioClip level2NormalMusic; // Nhạc nền Level 2 bình thường
    public AudioClip level2ClonePhaseMusic; // Nhạc khi clone xuất hiện
    public AudioClip level2BossMusic; // Nhạc boss Level 2
    
    [Header("General Music")]
    public AudioClip victoryMusic; // Nhạc victory
    public AudioClip gameOverMusic; // Nhạc game over
    
    [Header("Audio Settings")]
    [Range(0f, 1f)]
    public float musicVolume = 0.7f; // Volume nhạc nền
    public float crossfadeDuration = 2f; // Thời gian crossfade giữa các bản nhạc

    [Header("Settings")]
    public float delayBeforeShowingUI = 1f;
    public float delayAfterBossDeath = 1f;
    public float postBossDialogueDisplayTime = 3f;
    public string mainMenuSceneName = "MainMenu";
    public string nextLevelSceneName = "Level2";

    // ===== SỬA PHẦN NÀY TRONG GAMEMANAGER =====
    [Header("Level 2 Boss Control")]
    public int clonesRequiredToSummonBoss = 15; // Đổi từ 5 thành 15 để match với totalClonesToKill
    public float bossRespawnDelay = 0.5f;
    public float bossFlashDuration = 2f;
    public float flashSpeed = 0.1f;

    // Level 2 References (sẽ được tìm tự động khi load scene)
    private Boss2Controller boss2;
    private PlayerLevel2 playerLevel2;
    private ObjectPoolingClone clonePooling;

    // Level 2 States
    private bool level2BossIntroCompleted = false;
    private bool level2BossIsHidden = false;
    private bool level2BossHasReappeared = false;
    private int level2ClonesDefeated = 0;
    private Vector3 level2BossOriginalPosition;
    private SpriteRenderer level2BossRenderer;
    private Color level2BossOriginalColor;

    // Để theo dõi panel nào đang được hiển thị
    private bool wasGameOverPanelActive = false;
    private bool wasVictoryPanelActive = false;
    private bool wasInGameplay = true;
    private bool isShowingDialogue = false;

    // ===== THÊM: Enhanced Audio system variables =====
    private AudioClip currentMusicClip;
    private bool isMusicEnabled = true;
    private bool isTransitioning = false;
    private string currentMusicState = ""; // Track current music state

    // ===== THÊM: Level 1 Boss Phase Tracking =====
    private bossAiController level1Boss;
    private bool isLevel1BossPhase1 = false;
    private bool isLevel1BossPhase2 = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // ===== THÊM: Initialize audio system =====
            InitializeAudioSystem();

            // Đăng ký callback khi scene được tải
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ===== SỬA: Enhanced Initialize Audio System =====
    private void InitializeAudioSystem()
    {
        // Setup main music source if not assigned
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Setup fade audio source for crossfade
        if (fadeAudioSource == null)
        {
            fadeAudioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure both audio sources
        ConfigureAudioSource(musicSource);
        ConfigureAudioSource(fadeAudioSource);

        // Load audio settings
        LoadAudioSettings();
    }

    private void ConfigureAudioSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = true;
        source.volume = 0f; // Start with 0 volume
    }

    // ===== THÊM: Load Audio Settings =====
    private void LoadAudioSettings()
    {
        isMusicEnabled = PlayerPrefs.GetInt("AudioMuted", 0) == 0; // 0 = not muted, 1 = muted
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);

        // Set global audio listener
        AudioListener.volume = isMusicEnabled ? 1f : 0f;

        Debug.Log($"Audio settings loaded - Enabled: {isMusicEnabled}, Volume: {musicVolume}");
    }

    // ===== THÊM: Save Audio Settings =====
    private void SaveAudioSettings()
    {
        PlayerPrefs.SetInt("AudioMuted", isMusicEnabled ? 0 : 1);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.Save();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset game state
        Time.timeScale = 1f;

        // Reset UI tracking variables
        wasGameOverPanelActive = false;
        wasVictoryPanelActive = false;
        wasInGameplay = true;
        isShowingDialogue = false;

        // Reset music state
        currentMusicState = "";

        // Find UI elements in new scene
        FindUIReferences();

        // Ensure all panels are hidden on scene load
        HideAllPanels();

        // Setup UI listeners
        SetupUIListeners();

        // ===== THÊM: Handle scene music =====
        HandleSceneMusic(scene.name);

        // ===== LEVEL-SPECIFIC INITIALIZATION =====
        if (scene.name == "Level1" || scene.name.Contains("Level1"))
        {
            InitializeLevel1();
        }
        else if (scene.name == "Level2" || scene.name.Contains("Level2"))
        {
            InitializeLevel2();
        }

        // Cập nhật references cho CombatFeedbackManager
        if (CombatFeedbackManager.Instance != null)
        {
            CombatFeedbackManager.Instance.UpdateReferences();
        }
    }

    // ===== SỬA: Enhanced Handle Scene Music =====
    private void HandleSceneMusic(string sceneName)
    {
        AudioClip musicToPlay = null;
        string musicState = "";

        if (sceneName.Contains("Level1"))
        {
            musicToPlay = level1NormalMusic;
            musicState = "Level1_Normal";
        }
        else if (sceneName.Contains("Level2"))
        {
            musicToPlay = level2NormalMusic;
            musicState = "Level2_Normal";
        }

        if (musicToPlay != null)
        {
            PlayMusic(musicToPlay, musicState);
            Debug.Log($"Playing music for scene: {sceneName} - State: {musicState}");
        }
    }

    // ===== THÊM: Enhanced Music Control Methods =====
    private void PlayMusic(AudioClip clip, string musicState = "", bool useCrossfade = true)
    {
        if (clip == null) return;

        // Don't restart if same music is already playing
        if (currentMusicClip == clip && musicSource.isPlaying) return;

        currentMusicState = musicState;

        if (useCrossfade && musicSource.isPlaying)
        {
            StartCoroutine(CrossfadeMusic(clip));
        }
        else
        {
            PlayMusicImmediate(clip);
        }
    }

    private void PlayMusicImmediate(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;

        musicSource.clip = clip;
        currentMusicClip = clip;

        if (isMusicEnabled)
        {
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
        else
        {
            musicSource.volume = 0f;
            musicSource.Play(); // Still play but with 0 volume for consistency
        }
    }

    private IEnumerator CrossfadeMusic(AudioClip newClip)
    {
        if (isTransitioning) yield break;
        
        isTransitioning = true;

        // Setup fade audio source with new clip
        fadeAudioSource.clip = newClip;
        fadeAudioSource.volume = 0f;
        fadeAudioSource.Play();

        float fadeTime = 0f;
        float originalVolume = isMusicEnabled ? musicVolume : 0f;

        // Crossfade
        while (fadeTime < crossfadeDuration)
        {
            fadeTime += Time.unscaledDeltaTime; // Use unscaled time to work during pause
            float t = fadeTime / crossfadeDuration;

            if (isMusicEnabled)
            {
                musicSource.volume = Mathf.Lerp(originalVolume, 0f, t);
                fadeAudioSource.volume = Mathf.Lerp(0f, musicVolume, t);
            }

            yield return null;
        }

        // Swap audio sources
        musicSource.Stop();
        
        // Swap the sources
        var tempSource = musicSource;
        musicSource = fadeAudioSource;
        fadeAudioSource = tempSource;
        
        currentMusicClip = newClip;
        isTransitioning = false;

        Debug.Log($"Crossfaded to: {newClip.name}");
    }

    private void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
            currentMusicClip = null;
            currentMusicState = "";
        }
        if (fadeAudioSource != null)
        {
            fadeAudioSource.Stop();
        }
    }

    // ===== THÊM: Specific Music Methods =====
    
    // Level 1 Music Methods
    public void PlayLevel1BossPhase1Music()
    {
        if (level1BossPhase1Music != null && currentMusicState != "Level1_BossPhase1")
        {
            PlayMusic(level1BossPhase1Music, "Level1_BossPhase1");
            isLevel1BossPhase1 = true;
            isLevel1BossPhase2 = false;
            Debug.Log("Playing Level 1 Boss Phase 1 music");
        }
    }

    public void PlayLevel1BossPhase2Music()
    {
        if (level1BossPhase2Music != null && currentMusicState != "Level1_BossPhase2")
        {
            PlayMusic(level1BossPhase2Music, "Level1_BossPhase2");
            isLevel1BossPhase1 = false;
            isLevel1BossPhase2 = true;
            Debug.Log("Playing Level 1 Boss Phase 2 music");
        }
    }

    // Level 2 Music Methods
    public void PlayLevel2ClonePhaseMusic()
    {
        if (level2ClonePhaseMusic != null && currentMusicState != "Level2_ClonePhase")
        {
            PlayMusic(level2ClonePhaseMusic, "Level2_ClonePhase");
            Debug.Log("Playing Level 2 Clone Phase music");
        }
    }

    public void PlayLevel2BossMusic()
    {
        if (level2BossMusic != null && currentMusicState != "Level2_Boss")
        {
            PlayMusic(level2BossMusic, "Level2_Boss");
            Debug.Log("Playing Level 2 Boss music");
        }
    }

    // General Music Methods
    public void PlayVictoryMusic()
    {
        if (victoryMusic != null)
        {
            PlayMusic(victoryMusic, "Victory", false); // No crossfade for victory
            Debug.Log("Playing Victory music");
        }
    }

    public void PlayGameOverMusic()
    {
        if (gameOverMusic != null)
        {
            PlayMusic(gameOverMusic, "GameOver", false); // No crossfade for game over
            Debug.Log("Playing Game Over music");
        }
    }

    // ===== THÊM: Level 1 Boss Phase Detection =====
    private void SetupLevel1BossPhaseDetection()
    {
        level1Boss = FindFirstObjectByType<bossAiController>();
        if (level1Boss != null)
        {
            // Subscribe to boss events if available
            Debug.Log("Level 1 Boss found for phase detection");
            
            // Start checking boss health periodically
            StartCoroutine(MonitorLevel1BossPhases());
        }
    }

    private IEnumerator MonitorLevel1BossPhases()
    {
        while (level1Boss != null && !level1Boss.IsDead)
        {
            // Check boss health to determine phase
            float healthPercentage = (float)level1Boss.CurrentHealth / level1Boss.MaxHealth;
            
            if (healthPercentage > 0.5f && !isLevel1BossPhase1)
            {
                // Phase 1: Health > 50%
                PlayLevel1BossPhase1Music();
            }
            else if (healthPercentage <= 0.5f && healthPercentage > 0f && !isLevel1BossPhase2)
            {
                // Phase 2: Health <= 50%
                PlayLevel1BossPhase2Music();
            }

            yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds
        }
    }

    // ===== LEVEL 1 INITIALIZATION =====
    private void InitializeLevel1()
    {
        // Setup boss phase detection
        SetupLevel1BossPhaseDetection();
        
        StartCoroutine(ShowLevel1Dialogue());
    }

    // ===== LEVEL 2 INITIALIZATION =====
    private void InitializeLevel2()
    {
        Debug.Log("Initializing Level 2");

        // Reset Level 2 states
        level2BossIntroCompleted = false;
        level2BossIsHidden = false;
        level2BossHasReappeared = false;
        level2ClonesDefeated = 0;

        // Find Level 2 components
        FindLevel2Components();

        // ===== SỬA: Bắt đầu với boss bị ẩn, chờ 1 giây rồi hiện + dialogue =====
        StartCoroutine(StartLevel2Sequence());
    }

    // Sửa method FindLevel2Components để không tự động subscribe events
    private void FindLevel2Components()
    {
        // Tìm Boss2Controller
        boss2 = FindFirstObjectByType<Boss2Controller>();
        if (boss2 != null)
        {
            level2BossOriginalPosition = boss2.transform.position;
            level2BossRenderer = boss2.GetComponent<SpriteRenderer>();
            if (level2BossRenderer != null)
                level2BossOriginalColor = level2BossRenderer.color;

            // ===== THÊM: Disable movement ngay khi tìm thấy boss =====
            boss2.DisableMovement();

            // ===== THÊM: Ẩn boss sprite ngay từ đầu =====
            if (level2BossRenderer != null)
            {
                level2BossRenderer.color = new Color(level2BossOriginalColor.r, level2BossOriginalColor.g, level2BossOriginalColor.b, 0f);
                Debug.Log("Boss2 sprite hidden at start");
            }
        }

        // Tìm PlayerLevel2
        playerLevel2 = FindFirstObjectByType<PlayerLevel2>();

        // Tìm ObjectPoolingClone và đăng ký event
        clonePooling = FindFirstObjectByType<ObjectPoolingClone>();
        if (clonePooling != null)
        {
            // ===== SỬA: Đăng ký event clone defeated =====
            clonePooling.OnCloneDefeatedEvent += OnLevel2CloneDefeated;
            // ===== SỬA: Đăng ký event all clones killed =====
            clonePooling.OnAllClonesKilled += OnLevel2AllClonesKilled;

            // ===== THÊM: Đảm bảo clone không spawn ngay =====
            clonePooling.StopClonePhase();
        }

        Debug.Log($"Level 2 Components Found - Boss2: {boss2 != null}, Player: {playerLevel2 != null}, ClonePooling: {clonePooling != null}");
    }

    // ===== THÊM: Level 2 sequence với dialogue =====
    private IEnumerator StartLevel2Sequence()
    {
        Debug.Log("Starting Level 2 Sequence");

        // Disable player movement ngay từ đầu
        DisableLevel2PlayerMovement();

        // Đợi 1 giây để scene settle
        yield return new WaitForSeconds(1f);

        // Hiển thị boss sprite và bắt đầu dialogue
        yield return StartCoroutine(ShowLevel2BossAndDialogue());
    }

    // ===== THÊM: Show boss sprite và dialogue =====
    private IEnumerator ShowLevel2BossAndDialogue()
    {
        Debug.Log("Showing Level 2 Boss and Dialogue");

        // Hiện boss sprite
        if (boss2 != null && level2BossRenderer != null)
        {
            // Set IsOriginal = true để trigger animation
            boss2.IsOriginal = true;
            boss2.animator.SetBool("IsOriginal", true);

            // Fade in boss sprite
            level2BossRenderer.color = level2BossOriginalColor;
            Debug.Log("Boss2 sprite revealed");
        }

        // Đợi một chút để boss animation bắt đầu
        yield return new WaitForSeconds(0.5f);

        // Hiển thị dialogue
        yield return StartCoroutine(ShowLevel2Dialogue(level2BossIntroDialogue));

        // Sau dialogue xong, tiếp tục intro sequence
        StartCoroutine(ContinueLevel2BossIntroduction());
    }

    // ===== SỬA: Level 2 Dialogue System với parameter =====
    private IEnumerator ShowLevel2Dialogue(string dialogueMessage)
    {
        // Kiểm tra xem có dialogue banner không
        if (dialogueBanner == null || dialogueText == null)
        {
            Debug.LogWarning("DialogueBanner or DialogueText not found in Level 2!");
            yield break;
        }

        isShowingDialogue = true;

        // Hiển thị banner
        dialogueBanner.SetActive(true);

        // Clear text và bắt đầu hiệu ứng typing
        dialogueText.text = "";
        yield return StartCoroutine(TypeText(dialogueMessage));

        // Đợi thêm 2 giây để player đọc
        yield return new WaitForSeconds(2f);

        // Ẩn banner
        dialogueBanner.SetActive(false);
        isShowingDialogue = false;

        Debug.Log($"Level 2 dialogue completed: {dialogueMessage.Substring(0, Mathf.Min(20, dialogueMessage.Length))}...");
    }

    // ===== SỬA: Tiếp tục boss intro sau dialogue =====
    private IEnumerator ContinueLevel2BossIntroduction()
    {
        Debug.Log("Continuing Level 2 Boss Introduction after dialogue");

        // Đợi một chút sau dialogue
        yield return new WaitForSeconds(0.5f);

        // BƯỚC 2: Set Transition = true để chuyển từ Boss2 → Boss2Transition
        if (boss2 != null && boss2.animator != null)
        {
            boss2.animator.SetBool("Transition", true);
            Debug.Log("Boss2Transition animation started");
        }
    }

    // Gọi từ Animation Event của Boss2Transition khi animation intro kết thúc
    public void OnLevel2BossIntroComplete()
    {
        Debug.Log("Level 2 Boss Introduction Complete");
        level2BossIntroCompleted = true;

        // BƯỚC 3: Reset các bool để về Boss2Idle
        if (boss2 != null && boss2.animator != null)
        {
            boss2.animator.SetBool("Transition", false);
            boss2.animator.SetBool("IsOriginal", false);
            boss2.IsOriginal = false;
            // ===== THÊM: Đảm bảo boss không thể di chuyển sau intro =====
            boss2.DisableMovement();
            Debug.Log("Boss returned to Boss2Idle state and movement disabled");
        }

        // BƯỚC 4: Đợi một chút rồi hide boss
        StartCoroutine(HideBossAfterTransition());
    }

    // Sửa method HideBossAfterTransition
    private IEnumerator HideBossAfterTransition()
    {
        // Đợi animation transition về Boss2Idle hoàn thành
        yield return new WaitForSeconds(0.3f);

        // Hide boss
        HideLevel2Boss();

        // ===== SỬA: Kích hoạt clone phase và phát nhạc clone =====
        if (clonePooling != null)
        {
            clonePooling.StartClonePhase();
            Debug.Log("Clone phase activated after boss disappeared");
            
            // ===== THÊM: Phát nhạc clone phase =====
            PlayLevel2ClonePhaseMusic();
        }

        // Enable player movement để đánh clone
        EnableLevel2PlayerMovement();

        Debug.Log("Level 2 clone phase started");
    }

    private void HideLevel2Boss()
    {
        Debug.Log("Hiding Level 2 Boss");
        level2BossIsHidden = true;

        if (boss2 != null)
        {
            boss2.gameObject.SetActive(false);
        }
    }

    // ===== SỬA: Logic clone defeated =====
    public void OnLevel2CloneDefeated()
    {
        level2ClonesDefeated++;
        Debug.Log($"Level 2 Clone defeated: {level2ClonesDefeated}/{clonesRequiredToSummonBoss}");

        // Không cần check condition ở đây nữa vì ObjectPoolingClone sẽ tự xử lý
        // Logic reveal boss sẽ được handle bởi OnLevel2AllClonesKilled
    }

    // Sửa method OnLevel2AllClonesKilled để xử lý boss reappear
    public void OnLevel2AllClonesKilled()
    {
        Debug.Log("All clones killed - preparing boss reveal with dialogue");

        // ===== THÊM: Dừng clone phase =====
        if (clonePooling != null)
        {
            clonePooling.StopClonePhase();
        }

        // Disable player movement khi chờ boss xuất hiện
        DisableLevel2PlayerMovement();

        // ===== THÊM: Hiển thị dialogue trước khi boss reappear =====
        StartCoroutine(RevealLevel2BossWithDialogue());
    }

    // ===== THÊM: Method mới để hiển thị dialogue khi boss reappear =====
    private IEnumerator RevealLevel2BossWithDialogue()
    {
        Debug.Log("Showing boss reappear dialogue");

        // Đợi delay ngắn
        yield return new WaitForSeconds(bossRespawnDelay);

        // Hiển thị dialogue trước khi boss xuất hiện
        yield return StartCoroutine(ShowLevel2Dialogue(level2BossReappearDialogue));

        // Sau dialogue, reveal boss
        RevealLevel2Boss();
    }

    private IEnumerator RevealLevel2BossAfterDelay()
    {
        Debug.Log("Preparing to reveal Level 2 boss...");

        // Đợi delay
        yield return new WaitForSeconds(bossRespawnDelay);

        RevealLevel2Boss();
    }

    private void RevealLevel2Boss()
    {
        Debug.Log("Revealing Level 2 Boss");
        level2BossHasReappeared = true;

        // ===== SỬA: Play boss music khi boss xuất hiện =====
        PlayLevel2BossMusic();

        if (boss2 != null)
        {
            // Đặt boss về vị trí ban đầu
            boss2.transform.position = level2BossOriginalPosition;

            // Kích hoạt boss
            boss2.gameObject.SetActive(true);

            // ===== THÊM: Đảm bảo sprite visible =====
            if (level2BossRenderer != null)
            {
                level2BossRenderer.color = level2BossOriginalColor;
            }

            // Reset boss state
            boss2.IsOriginal = false; // Cho phép boss di chuyển và tấn công

            // ===== THÊM: Enable movement chỉ sau khi boss đã reappear =====
            boss2.EnableMovement();

            // Start flash effect
            StartCoroutine(FlashLevel2BossWhite());

            // Enable player movement sau khi boss xuất hiện
            EnableLevel2PlayerMovement();
        }
    }

    private IEnumerator FlashLevel2BossWhite()
    {
        if (level2BossRenderer == null) yield break;

        float elapsedTime = 0f;

        while (elapsedTime < bossFlashDuration)
        {
            // Nháy trắng
            level2BossRenderer.color = Color.white;
            yield return new WaitForSeconds(flashSpeed);

            // Về màu gốc
            level2BossRenderer.color = level2BossOriginalColor;
            yield return new WaitForSeconds(flashSpeed);

            elapsedTime += flashSpeed * 2;
        }

        // Đảm bảo màu cuối cùng là màu gốc
        level2BossRenderer.color = level2BossOriginalColor;

        Debug.Log("Level 2 Boss flash effect completed");
    }

    private void DisableLevel2PlayerMovement()
    {
        if (playerLevel2 != null)
        {
            playerLevel2.enabled = false;
            Debug.Log("Level 2 Player movement disabled");
        }
    }

    private void EnableLevel2PlayerMovement()
    {
        if (playerLevel2 != null)
        {
            playerLevel2.enabled = true;
            Debug.Log("Level 2 Player movement enabled");
        }
    }

    // ===== DEBUG METHODS CHO LEVEL 2 =====
    [ContextMenu("Force Level 2 Boss Reappear")]
    public void ForceRevealLevel2Boss()
    {
        if (SceneManager.GetActiveScene().name.Contains("Level2"))
        {
            if (clonePooling != null)
            {
                // Trigger manually by setting clone count to required amount
                clonePooling.OnAllClonesKilled?.Invoke();
            }
        }
    }

    [ContextMenu("Reset Level 2")]
    public void ResetLevel2()
    {
        if (SceneManager.GetActiveScene().name.Contains("Level2"))
        {
            level2BossIntroCompleted = false;
            level2BossIsHidden = false;
            level2BossHasReappeared = false;
            level2ClonesDefeated = 0;

            if (boss2 != null)
            {
                boss2.gameObject.SetActive(true);
                boss2.transform.position = level2BossOriginalPosition;
                boss2.IsOriginal = true;
                // ===== THÊM: Disable movement khi reset =====
                boss2.DisableMovement();

                // ===== THÊM: Reset sprite visibility =====
                if (level2BossRenderer != null)
                {
                    level2BossRenderer.color = new Color(level2BossOriginalColor.r, level2BossOriginalColor.g, level2BossOriginalColor.b, 0f);
                }
            }

            EnableLevel2PlayerMovement();
        }
    }

    private void FindUIReferences()
    {
        // Find panels in the scene
        gameOverPanel = GameObject.Find("GameOverPanel");
        victoryPanel = GameObject.Find("VictoryPanel");
        settingsPanel = GameObject.Find("SettingsPanel");
        dialogueBanner = GameObject.Find("DialogueBanner");

        // Find dialogue text if banner exists
        if (dialogueBanner != null)
        {
            dialogueText = FindComponentInChildren<TextMeshProUGUI>(dialogueBanner, "DialogueText");
        }

        // Find Victory Panel buttons
        if (victoryPanel != null)
        {
            continueButton = FindButtonInChildren(victoryPanel, "ContinueButton");
            saveGameButton = FindButtonInChildren(victoryPanel, "SaveGameButton");
            victoryClearSaveButton = FindButtonInChildren(victoryPanel, "ClearSaveButton"); // ===== THÊM: Clear Save button cho Victory Panel =====
            victorySettingsButton = FindButtonInChildren(victoryPanel, "SettingsButton");
            victoryQuitButton = FindButtonInChildren(victoryPanel, "QuitButton");
            victorySaveInfoText = FindComponentInChildren<TextMeshProUGUI>(victoryPanel, "SaveInfoText"); // ===== THÊM: Save Info text cho Victory Panel =====
        }

        // Find Game Over Panel buttons
        if (gameOverPanel != null)
        {
            tryAgainButton = FindButtonInChildren(gameOverPanel, "TryAgainButton");
            mainMenuButton = FindButtonInChildren(gameOverPanel, "MainMenuButton");
            gameOverSettingsButton = FindButtonInChildren(gameOverPanel, "SettingsButton");
            exitButton = FindButtonInChildren(gameOverPanel, "QuitButton");
        }

        // Find Settings Panel elements
        if (settingsPanel != null)
        {
            settingsMainMenuButton = FindButtonInChildren(settingsPanel, "MainMenuButton");
            settingsExitButton = FindButtonInChildren(settingsPanel, "ExitButton");
            saveGameSettingsButton = FindButtonInChildren(settingsPanel, "SaveGameButton");
            loadGameButton = FindButtonInChildren(settingsPanel, "LoadGameButton");
            clearSaveButton = FindButtonInChildren(settingsPanel, "ClearSaveButton"); // ===== THÊM: Tìm button clear save =====
            audioToggle = FindComponentInChildren<Toggle>(settingsPanel, "AudioToggle");
            if (audioToggle != null)
            {
                // ===== SỬA: Tìm image theo cấu trúc Background/Checkmark =====
                audioToggleImage = audioToggle.transform.Find("Background/Checkmark")?.GetComponent<Image>();
                if (audioToggleImage == null)
                {
                    audioToggleImage = audioToggle.transform.Find("Background")?.GetComponent<Image>();
                }
                if (audioToggleImage == null)
                {
                    audioToggleImage = audioToggle.GetComponentInChildren<Image>();
                }
            }
            saveInfoText = FindComponentInChildren<TextMeshProUGUI>(settingsPanel, "SaveInfoText"); // ===== THÊM: Tìm text hiển thị save info =====
        }
    }

    // Helper method to find buttons anywhere in the hierarchy of a GameObject
    private Button FindButtonInChildren(GameObject parent, string buttonName)
    {
        // Try direct child first
        Transform buttonTransform = parent.transform.Find(buttonName);
        if (buttonTransform != null)
            return buttonTransform.GetComponent<Button>();

        // Search deeper in hierarchy
        Button[] buttons = parent.GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
        {
            if (button.name == buttonName || button.gameObject.name == buttonName)
                return button;
        }

        Debug.LogWarning($"Button '{buttonName}' not found in {parent.name}");
        return null;
    }

    // Generic version for other component types
    private T FindComponentInChildren<T>(GameObject parent, string name) where T : Component
    {
        // Try direct child first
        Transform transform = parent.transform.Find(name);
        if (transform != null)
            return transform.GetComponent<T>();

        // Search deeper in hierarchy
        T[] components = parent.GetComponentsInChildren<T>();
        foreach (T component in components)
        {
            if (component.name == name || component.gameObject.name == name)
                return component;
        }

        Debug.LogWarning($"Component '{name}' of type {typeof(T)} not found in {parent.name}");
        return null;
    }

    private void HideAllPanels()
    {
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (victoryPanel) victoryPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (dialogueBanner) dialogueBanner.SetActive(false);
    }

    // Level 1 Dialogue System
    private IEnumerator ShowLevel1Dialogue()
    {
        // Đợi 1 giây để scene load hoàn toàn
        yield return new WaitForSeconds(1f);

        // Kiểm tra xem có dialogue banner không
        if (dialogueBanner == null || dialogueText == null)
        {
            Debug.LogWarning("DialogueBanner or DialogueText not found in Level 1!");
            yield break;
        }

        isShowingDialogue = true;

        // Vô hiệu hóa player và boss movement
        DisableGameplayControls();

        // Hiển thị banner
        dialogueBanner.SetActive(true);

        // Clear text và bắt đầu hiệu ứng typing
        dialogueText.text = "";
        yield return StartCoroutine(TypeText(level1DialogueMessage));

        // Đợi thêm 2 giây để player đọc
        yield return new WaitForSeconds(2f);

        // Ẩn banner và kích hoạt lại gameplay
        dialogueBanner.SetActive(false);
        EnableGameplayControls();

        isShowingDialogue = false;

        Debug.Log("Level 1 dialogue completed!");
    }

    // ===== THÊM: Level 1 Boss Phase 2 Transition Dialogue =====
    public void ShowLevel1BossPhase2Dialogue()
    {
        if (isShowingDialogue) return; // Tránh hiển thị nhiều dialogue cùng lúc
        
        StartCoroutine(ShowLevel1BossPhase2DialogueCoroutine());
    }

    private IEnumerator ShowLevel1BossPhase2DialogueCoroutine()
    {
        // Kiểm tra xem có dialogue banner không
        if (dialogueBanner == null || dialogueText == null)
        {
            Debug.LogWarning("DialogueBanner or DialogueText not found for Boss Phase 2 dialogue!");
            yield break;
        }

        isShowingDialogue = true;

        // Vô hiệu hóa player movement
        DisablePlayerOnly();

        // Hiển thị banner
        dialogueBanner.SetActive(true);

        // Clear text và bắt đầu hiệu ứng typing
        dialogueText.text = "";
        yield return StartCoroutine(TypeText(level1BossPhase2Dialogue));

        // Đợi thêm 2.5 giây để player đọc (hơi lâu hơn vì là dialogue quan trọng)
        yield return new WaitForSeconds(1.5f);

        // Ẩn banner và kích hoạt lại player movement
        dialogueBanner.SetActive(false);
        EnablePlayerOnly();

        isShowingDialogue = false;

        Debug.Log("Level 1 Boss Phase 2 dialogue completed!");
    }

    // Helper methods để chỉ disable/enable player mà không ảnh hưởng boss
    private void DisablePlayerOnly()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Player playerComponent = player.GetComponent<Player>();
            if (playerComponent != null)
            {
                playerComponent.enabled = false;
            }
        }
    }

    private void EnablePlayerOnly()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Player playerComponent = player.GetComponent<Player>();
            if (playerComponent != null)
            {
                playerComponent.enabled = true;
            }
        }
    }

    // Level 1 Post-Boss Dialogue (khi boss chết)
    // ===== THÊM MỚI: POST-BOSS DIALOGUE SYSTEM =====
    private IEnumerator ShowPostBossDialogue()
    {
        // Đợi sau khi boss chết
        yield return new WaitForSeconds(delayAfterBossDeath);

        // Kiểm tra xem có dialogue banner không
        if (dialogueBanner == null || dialogueText == null)
        {
            Debug.LogWarning("DialogueBanner or DialogueText not found for post-boss dialogue!");
            // Nếu không có dialogue, chuyển thẳng đến victory panel
            yield return StartCoroutine(ShowVictoryPanel());
            yield break;
        }

        isShowingDialogue = true;

        // Hiển thị banner
        dialogueBanner.SetActive(true);

        // Clear text và bắt đầu hiệu ứng typing
        dialogueText.text = "";
        yield return StartCoroutine(TypeText(level1PostBossDialogue));

        // Đợi để player đọc
        yield return new WaitForSeconds(postBossDialogueDisplayTime);

        // Ẩn banner
        dialogueBanner.SetActive(false);
        isShowingDialogue = false;

        Debug.Log("Post-boss dialogue completed!");

        // Sau đó hiển thị victory panel
        yield return StartCoroutine(ShowVictoryPanel());
    }

    // ===== THÊM MỚI: Level 2 POST-BOSS DIALOGUE SYSTEM =====
    private IEnumerator ShowLevel2PostBossDialogue()
    {
        // Đợi sau khi boss chết
        yield return new WaitForSeconds(delayAfterBossDeath);

        // Kiểm tra xem có dialogue banner không
        if (dialogueBanner == null || dialogueText == null)
        {
            Debug.LogWarning("DialogueBanner or DialogueText not found for Level 2 post-boss dialogue!");
            // Nếu không có dialogue, chuyển thẳng đến victory panel
            yield return StartCoroutine(ShowVictoryPanel());
            yield break;
        }

        isShowingDialogue = true;

        // Hiển thị banner
        dialogueBanner.SetActive(true);

        // Clear text và bắt đầu hiệu ứng typing
        dialogueText.text = "";
        yield return StartCoroutine(TypeText(level2PostBossDialogue));

        // Đợi để player đọc
        yield return new WaitForSeconds(postBossDialogueDisplayTime);

        // Ẩn banner
        dialogueBanner.SetActive(false);
        isShowingDialogue = false;

        Debug.Log("Level 2 post-boss dialogue completed!");

        // Sau đó hiển thị victory panel
        yield return StartCoroutine(ShowVictoryPanel());
    }

    private IEnumerator TypeText(string message)
    {
        dialogueText.text = "";

        foreach (char letter in message.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(textSpeed);
        }
    }

    private void DisableGameplayControls()
    {
        // Vô hiệu hóa player movement - Sửa từ PlayerController thành Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Player playerComponent = player.GetComponent<Player>();
            PlayerLevel2 playerLevel2Component = player.GetComponent<PlayerLevel2>();

            if (playerComponent != null)
            {
                playerComponent.enabled = false;
            }
            if (playerLevel2Component != null)
            {
                playerLevel2Component.enabled = false;
            }
        }

        // Vô hiệu hóa boss AI - Sử dụng FindFirstObjectByType thay vì FindObjectOfType
        bossAiController bossAI = FindFirstObjectByType<bossAiController>();
        if (bossAI != null)
        {
            bossAI.SetAIActive(false);
        }

        // Vô hiệu hóa Boss2Controller nếu có
        Boss2Controller boss2 = FindFirstObjectByType<Boss2Controller>();
        if (boss2 != null)
        {
            boss2.enabled = false;
        }
    }

    private void EnableGameplayControls()
    {
        // Kích hoạt lại player movement - Sửa từ PlayerController thành Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Player playerComponent = player.GetComponent<Player>();
            PlayerLevel2 playerLevel2Component = player.GetComponent<PlayerLevel2>();

            if (playerComponent != null)
            {
                playerComponent.enabled = true;
            }
            if (playerLevel2Component != null)
            {
                playerLevel2Component.enabled = true;
            }
        }

        // Kích hoạt lại boss AI - Sử dụng FindFirstObjectByType thay vì FindObjectOfType
        bossAiController bossAI = FindFirstObjectByType<bossAiController>();
        if (bossAI != null)
        {
            bossAI.SetAIActive(true);
        }

        // Kích hoạt lại Boss2Controller nếu có
        Boss2Controller boss2 = FindFirstObjectByType<Boss2Controller>();
        if (boss2 != null)
        {
            boss2.enabled = true;
        }
    }

    private void SetupUIListeners()
    {
        // Thêm listeners cho Victory Panel buttons
        if (continueButton) continueButton.onClick.AddListener(LoadNextLevel);
        if (saveGameButton) saveGameButton.onClick.AddListener(SaveGame);
        if (victoryClearSaveButton) victoryClearSaveButton.onClick.AddListener(ClearSaveGame); // ===== THÊM: Listener cho button clear save =====
        if (victorySettingsButton) victorySettingsButton.onClick.AddListener(OpenSettings);
        if (victoryQuitButton) victoryQuitButton.onClick.AddListener(QuitGame);

        // Thêm listeners cho Game Over Panel buttons
        if (tryAgainButton) tryAgainButton.onClick.AddListener(RestartGame);
        if (mainMenuButton) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        if (gameOverSettingsButton) gameOverSettingsButton.onClick.AddListener(OpenSettings);
        if (exitButton) exitButton.onClick.AddListener(QuitGame);

        // Settings panel
        if (settingsMainMenuButton) settingsMainMenuButton.onClick.AddListener(ReturnToMainMenu);
        if (settingsExitButton) settingsExitButton.onClick.AddListener(QuitGame);
        if (saveGameSettingsButton) saveGameSettingsButton.onClick.AddListener(SaveGame);
        if (loadGameButton) loadGameButton.onClick.AddListener(LoadGame);
        if (clearSaveButton) clearSaveButton.onClick.AddListener(ClearSaveGame); // ===== THÊM: Listener cho button clear save =====

        // ===== SỬA: Audio toggle với logic tương tự MainMenu =====
        if (audioToggle)
        {
            audioToggle.onValueChanged.AddListener(ToggleAudio);
            // Khởi tạo trạng thái âm thanh
            bool isMuted = PlayerPrefs.GetInt("AudioMuted", 0) == 1;
            audioToggle.isOn = !isMuted;
            UpdateAudioState(!isMuted);
        }

        // ===== THÊM: Update Save/Load UI trong Level scenes =====
        UpdateSaveLoadUI();
    }

    // ===== THÊM: Update Save/Load UI trong Level scenes =====
    private void UpdateSaveLoadUI()
    {
        bool hasSave = HasSaveGame();
        
        // ===== Settings Panel UI =====
        // Enable/disable Load Game button
        if (loadGameButton != null)
        {
            loadGameButton.interactable = hasSave;
        }
        
        // Ẩn/hiện Clear Save button
        if (clearSaveButton != null)
        {
            clearSaveButton.gameObject.SetActive(hasSave);
        }
        
        // Update save info text
        if (saveInfoText != null)
        {
            if (hasSave)
            {
                saveInfoText.text = "Save Game:\n" + GetSaveInfo();
                saveInfoText.color = Color.white;
            }
            else
            {
                saveInfoText.text = "Không có save game";
                saveInfoText.color = Color.gray;
            }
        }
        
        // ===== Victory Panel UI =====
        // Ẩn/hiện Victory Clear Save button
        if (victoryClearSaveButton != null)
        {
            victoryClearSaveButton.gameObject.SetActive(hasSave);
        }
        
        // Update Victory save info text
        if (victorySaveInfoText != null)
        {
            if (hasSave)
            {
                victorySaveInfoText.text = "Save Game:\n" + GetSaveInfo();
                victorySaveInfoText.color = Color.white;
            }
            else
            {
                victorySaveInfoText.text = "Không có save game";
                victorySaveInfoText.color = Color.gray;
            }
        }
        
        Debug.Log($"GameManager Save/Load UI updated - Has save: {hasSave}, Clear buttons visible: {hasSave}");
    }

    // ===== THÊM: Public method để refresh save info từ bên ngoài =====
    public void RefreshSaveInfo()
    {
        UpdateSaveLoadUI();
    }

    // ===== THÊM: Method để test ESC functionality =====
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestESCFunctionality()
    {
        Debug.Log("=== ESC FUNCTIONALITY TEST ===");
        Debug.Log($"GameManager enabled: {enabled}");
        Debug.Log($"GameManager gameObject active: {gameObject.activeInHierarchy}");
        Debug.Log($"Settings Panel found: {settingsPanel != null}");
        if (settingsPanel != null)
        {
            Debug.Log($"Settings Panel active: {settingsPanel.activeSelf}");
        }
        Debug.Log($"Is showing dialogue: {isShowingDialogue}");
        Debug.Log($"Game Over Panel active: {gameOverPanel != null && gameOverPanel.activeSelf}");
        Debug.Log($"Victory Panel active: {victoryPanel != null && victoryPanel.activeSelf}");
        Debug.Log("=== END TEST ===");
    }

    // ===== THÊM: Method để force mở Settings (cho test) =====
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ForceOpenSettings()
    {
        Debug.Log("Force opening Settings Panel...");
        if (settingsPanel == null)
        {
            FindUIReferences();
        }
        
        if (settingsPanel != null)
        {
            OpenSettings();
            Debug.Log("Settings Panel forced open!");
        }
        else
        {
            Debug.LogError("Cannot force open - Settings Panel not found!");
        }
    }

    // ===== THÊM: Method để update UI riêng cho Victory Panel =====
    public void UpdateVictoryPanelUI()
    {
        bool hasSave = HasSaveGame();
        
        // Victory Panel UI only
        if (victoryClearSaveButton != null)
        {
            victoryClearSaveButton.gameObject.SetActive(hasSave);
        }
        
        if (victorySaveInfoText != null)
        {
            if (hasSave)
            {
                victorySaveInfoText.text = "Save Game:\n" + GetSaveInfo();
                victorySaveInfoText.color = Color.white;
            }
            else
            {
                victorySaveInfoText.text = "Không có save game";
                victorySaveInfoText.color = Color.gray;
            }
        }
        
        Debug.Log($"Victory Panel UI updated - Has save: {hasSave}");
    }

    private void Start()
    {
        // Initial setup for the first scene
        HideAllPanels();
        SetupUIListeners();
        
        // ===== THÊM: Debug ESC functionality setup =====
        Debug.Log($"GameManager started in scene: {SceneManager.GetActiveScene().name}");
        Debug.Log($"Settings Panel found: {settingsPanel != null}");
        
        // Test ESC functionality after a short delay
        Invoke(nameof(TestESCSetup), 1f);
    }
    
    // ===== THÊM: Test ESC setup after Start =====
    private void TestESCSetup()
    {
        Debug.Log("=== ESC SETUP TEST AFTER START ===");
        Debug.Log($"GameManager active: {gameObject.activeInHierarchy}");
        Debug.Log($"GameManager enabled: {enabled}");
        Debug.Log($"Settings Panel: {(settingsPanel != null ? "Found" : "NOT FOUND")}");
        if (settingsPanel != null)
        {
            Debug.Log($"Settings Panel parent: {settingsPanel.transform.parent?.name}");
            Debug.Log($"Settings Panel active: {settingsPanel.activeSelf}");
        }
        Debug.Log("=== Press ESC to test Settings Panel ===");
    }

    // ===== SỬA OnDestroy =====
    private void OnDestroy()
    {
        // Hủy đăng ký sự kiện khi GameManager bị hủy
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // Hủy đăng ký Level 2 events
        if (clonePooling != null)
        {
            clonePooling.OnCloneDefeatedEvent -= OnLevel2CloneDefeated;
            clonePooling.OnAllClonesKilled -= OnLevel2AllClonesKilled; // THÊM DÒNG NÀY
        }
    }

    private void Update()
    {
        // Không cho phép mở Settings khi đang hiển thị dialogue
        if (isShowingDialogue) 
        {
            Debug.Log("ESC ignored - dialogue is showing");
            return;
        }

        // ===== CẢIỆN: Xử lý nút ESC để mở/đóng Settings panel với debug =====
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("ESC key pressed!");
            
            // Kiểm tra settingsPanel có tồn tại không
            if (settingsPanel == null)
            {
                Debug.LogWarning("Settings Panel not found! Trying to find it...");
                FindUIReferences();
            }
            
            if (settingsPanel == null)
            {
                Debug.LogError("Settings Panel still not found after FindUIReferences!");
                return;
            }
            
            // Nếu settings panel đang mở, đóng nó lại
            if (settingsPanel.activeSelf)
            {
                Debug.Log("Closing Settings Panel");
                CloseSettings();
            }
            // Nếu settings panel đang đóng, mở nó ra (trừ khi đang ở game over/victory)
            else
            {
                bool gameOverActive = gameOverPanel && gameOverPanel.activeSelf;
                bool victoryActive = victoryPanel && victoryPanel.activeSelf;
                
                if (gameOverActive)
                {
                    Debug.Log("Cannot open Settings - Game Over Panel is active");
                }
                else if (victoryActive)
                {
                    Debug.Log("Cannot open Settings - Victory Panel is active");
                }
                else
                {
                    Debug.Log("Opening Settings Panel");
                    OpenSettings();
                }
            }
        }

        // Cho phép skip dialogue bằng Space hoặc Enter
        if (isShowingDialogue && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
        {
            // Skip typing effect và hiển thị toàn bộ text
            StopAllCoroutines();
            if (dialogueText != null)
            {
                // ===== SỬA: Xác định message hiện tại để skip cho Level 1 bao gồm cả Phase 2 dialogue =====
                if (SceneManager.GetActiveScene().name.Contains("Level1"))
                {
                    // Xác định dialogue nào đang hiển thị dựa trên context
                    string currentDialogue = level1DialogueMessage;
                    
                    // Kiểm tra xem có phải là boss phase 2 dialogue không
                    if (level1Boss != null && level1Boss.CurrentPhase == 2 && !level1Boss.IsDead)
                    {
                        currentDialogue = level1BossPhase2Dialogue;
                    }
                    // Kiểm tra xem có phải là post-boss dialogue không  
                    else if (level1Boss != null && level1Boss.IsDead)
                    {
                        currentDialogue = level1PostBossDialogue;
                    }
                    // Nếu không thì là dialogue ban đầu
                    else if (dialogueText.text.Contains("Remember"))
                    {
                        currentDialogue = level1DialogueMessage;
                    }
                    // Fallback để phân biệt giữa các dialogue
                    else if (dialogueText.text.Contains("ENOUGH"))
                    {
                        currentDialogue = level1BossPhase2Dialogue;
                    }
                    else if (dialogueText.text.Contains("harsh"))
                    {
                        currentDialogue = level1PostBossDialogue;
                    }
                    
                    dialogueText.text = currentDialogue;
                }
                else if (SceneManager.GetActiveScene().name.Contains("Level2"))
                {
                    // Xác định dialogue nào đang hiển thị dựa trên context
                    string currentDialogue = level2BossIntroDialogue;
                    if (level2BossHasReappeared && !boss2.isDie)
                    {
                        currentDialogue = level2BossReappearDialogue;
                    }
                    else if (boss2 != null && boss2.isDie)
                    {
                        currentDialogue = level2PostBossDialogue;
                    }
                    dialogueText.text = currentDialogue;
                }
            }
            StartCoroutine(SkipToEndDialogue());
        }
    }

    private IEnumerator SkipToEndDialogue()
    {
        // Đợi 1 giây rồi kết thúc dialogue
        yield return new WaitForSeconds(1f);

        if (dialogueBanner) dialogueBanner.SetActive(false);
        isShowingDialogue = false;

        // ===== SỬA: Xử lý khác nhau cho mỗi level =====
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName.Contains("Level1"))
        {
            EnableGameplayControls();
        }
        else if (currentSceneName.Contains("Level2"))
        {
            // Kiểm tra context để xác định action sau khi skip
            if (!level2BossIntroCompleted)
            {
                // Skip intro dialogue
                StartCoroutine(ContinueLevel2BossIntroduction());
            }
            else if (level2BossHasReappeared && boss2 != null && !boss2.isDie)
            {
                // Skip reappear dialogue - enable player movement
                EnableLevel2PlayerMovement();
            }
            else if (boss2 != null && boss2.isDie)
            {
                // Skip post-boss dialogue - show victory panel
                StartCoroutine(ShowVictoryPanel());
            }
        }

        Debug.Log("Dialogue skipped!");
    }

    public void GameOver()
    {
        // ===== SỬA: Play game over music =====
        PlayGameOverMusic();

        StartCoroutine(ShowGameOverPanel());
    }

    // ===== SỬA ĐỔI: Victory() giờ sẽ hiển thị dialogue trước cho cả Level 1 và Level 2 =====
    public void Victory()
    {
        // ===== SỬA: Play victory music =====
        PlayVictoryMusic();

        string currentSceneName = SceneManager.GetActiveScene().name;

        // Nếu là Level 1, hiển thị dialogue post-boss trước
        if (currentSceneName == "Level1" || currentSceneName.Contains("Level1"))
        {
            StartCoroutine(ShowPostBossDialogue());
        }
        // ===== THÊM: Nếu là Level 2, hiển thị Level 2 post-boss dialogue =====
        else if (currentSceneName == "Level2" || currentSceneName.Contains("Level2"))
        {
            StartCoroutine(ShowLevel2PostBossDialogue());
        }
        else
        {
            // Các level khác hiển thị victory panel trực tiếp
            StartCoroutine(ShowVictoryPanel());
        }
    }

    private IEnumerator ShowGameOverPanel()
    {
        yield return new WaitForSeconds(delayBeforeShowingUI);

        // Ensure we found the panel in this scene
        if (gameOverPanel == null)
        {
            FindUIReferences();
        }

        if (gameOverPanel)
        {
            gameOverPanel.SetActive(true);
            wasGameOverPanelActive = true;
            wasVictoryPanelActive = false;
            wasInGameplay = false;
        }
        Time.timeScale = 0f; // Tạm dừng game
    }

    private IEnumerator ShowVictoryPanel()
    {
        yield return new WaitForSeconds(delayBeforeShowingUI);

        // Ensure we found the panel in this scene
        if (victoryPanel == null)
        {
            FindUIReferences();
        }

        if (victoryPanel)
        {
            victoryPanel.SetActive(true);
            wasGameOverPanelActive = false;
            wasVictoryPanelActive = true;
            wasInGameplay = false;
            
            // ===== THÊM: Update Save/Load UI khi hiển thị Victory Panel =====
            UpdateSaveLoadUI();
        }
        Time.timeScale = 0f; // Tạm dừng game
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        // Force hide all panels before restarting
        HideAllPanels();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Level2");
    }

    // ===== HỆ THỐNG SAVE/LOAD ĐỠN GIẢN =====
    public void SaveGame()
    {
        Debug.Log("Đang lưu game...");
        
        try
        {
            // Lưu scene hiện tại
            string currentScene = SceneManager.GetActiveScene().name;
            PlayerPrefs.SetString("SavedScene", currentScene);
            
            // Lưu thời gian save
            string saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            PlayerPrefs.SetString("SaveTime", saveTime);
            
            // Lưu level đã hoàn thành (nếu có)
            if (currentScene.Contains("Level1"))
            {
                PlayerPrefs.SetInt("ReachedLevel1", 1);
            }
            else if (currentScene.Contains("Level2"))
            {
                PlayerPrefs.SetInt("ReachedLevel1", 1);
                PlayerPrefs.SetInt("ReachedLevel2", 1);
            }
            
            // Lưu audio settings
            SaveAudioSettings();
            
            // Commit save
            PlayerPrefs.Save();
            
            Debug.Log($"✅ Game đã được lưu! Scene: {currentScene}, Thời gian: {saveTime}");
            
            // ===== THÊM: Update UI sau khi save =====
            UpdateSaveLoadUI();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Lỗi khi lưu game: {e.Message}");
        }
    }

    public void LoadGame()
    {
        Debug.Log("Đang tải game...");
        
        try
        {
            // Kiểm tra có save game không
            if (!PlayerPrefs.HasKey("SavedScene"))
            {
                Debug.Log("⚠️ Không tìm thấy save game! Quay về Main Menu.");
                SceneManager.LoadScene("MainMenu");
                return;
            }
            
            // Lấy thông tin save
            string savedScene = PlayerPrefs.GetString("SavedScene");
            string saveTime = PlayerPrefs.GetString("SaveTime", "Không rõ thời gian");
            
            Debug.Log($"📁 Tìm thấy save game: {savedScene} (Lưu lúc: {saveTime})");
            
            // Kiểm tra scene có tồn tại không
            if (IsValidScene(savedScene))
            {
                // Load scene
                Time.timeScale = 1f; // Đảm bảo game không bị pause
                SceneManager.LoadScene(savedScene);
                
                Debug.Log($"✅ Đã load thành công scene: {savedScene}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Scene '{savedScene}' không hợp lệ! Load Main Menu thay thế.");
                SceneManager.LoadScene("MainMenu");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Lỗi khi load game: {e.Message}");
            // Fallback về Main Menu nếu có lỗi
            SceneManager.LoadScene("MainMenu");
        }
    }
    
    // Kiểm tra scene có hợp lệ không
    private bool IsValidScene(string sceneName)
    {
        // Danh sách scene hợp lệ
        string[] validScenes = { "MainMenu", "Level1", "Level2" };
        
        foreach (string validScene in validScenes)
        {
            if (sceneName == validScene || sceneName.Contains(validScene))
            {
                return true;
            }
        }
        
        return false;
    }
    
    // Kiểm tra có save game không (dùng cho UI)
    public bool HasSaveGame()
    {
        return PlayerPrefs.HasKey("SavedScene");
    }
    
    // Lấy thông tin save game (dùng cho hiển thị UI)
    public string GetSaveInfo()
    {
        if (!HasSaveGame())
            return "Không có save game";
            
        string savedScene = PlayerPrefs.GetString("SavedScene");
        string saveTime = PlayerPrefs.GetString("SaveTime", "Không rõ thời gian");
        
        // Format tên scene cho dễ đọc
        string sceneName = savedScene;
        if (savedScene.Contains("Level1")) sceneName = "Level 1";
        else if (savedScene.Contains("Level2")) sceneName = "Level 2";
        else if (savedScene.Contains("MainMenu")) sceneName = "Main Menu";
        
        return $"{sceneName}\n{saveTime}";
    }
    
    // ===== THÊM: Method để xóa save game =====
    public void ClearSaveGame()
    {
        Debug.Log("Đang xóa save game...");
        
        try
        {
            // Xóa các key liên quan đến save game
            PlayerPrefs.DeleteKey("SavedScene");
            PlayerPrefs.DeleteKey("SaveTime");
            PlayerPrefs.DeleteKey("ReachedLevel1");
            PlayerPrefs.DeleteKey("ReachedLevel2");
            
            // Commit changes
            PlayerPrefs.Save();
            
            Debug.Log("✅ Đã xóa save game thành công!");
            
            // ===== THÊM: Update UI sau khi clear save =====
            UpdateSaveLoadUI();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Lỗi khi xóa save game: {e.Message}");
        }
    }

    public void OpenSettings()
    {
        // Không cho phép mở settings khi đang hiển thị dialogue
        if (isShowingDialogue) return;

        // Lưu trạng thái hiện tại
        wasGameOverPanelActive = gameOverPanel && gameOverPanel.activeSelf;
        wasVictoryPanelActive = victoryPanel && victoryPanel.activeSelf;
        wasInGameplay = !wasGameOverPanelActive && !wasVictoryPanelActive;

        // Ẩn các panel khác nếu đang mở
        if (gameOverPanel && gameOverPanel.activeSelf)
            gameOverPanel.SetActive(false);
        if (victoryPanel && victoryPanel.activeSelf)
            victoryPanel.SetActive(false);

        // Hiển thị panel settings
        if (settingsPanel)
            settingsPanel.SetActive(true);

        // Tạm dừng game nếu đang trong gameplay
        if (wasInGameplay)
            Time.timeScale = 0f;

        // Update save/load UI khi mở settings
        UpdateSaveLoadUI();
    }

    public void CloseSettings()
    {
        if (settingsPanel)
            settingsPanel.SetActive(false);

        // Hiển thị lại panel trước đó
        if (gameOverPanel && wasGameOverPanelActive)
            gameOverPanel.SetActive(true);
        else if (victoryPanel && wasVictoryPanelActive)
            victoryPanel.SetActive(true);
        else if (wasInGameplay)
            Time.timeScale = 1f; // Tiếp tục game nếu đang trong gameplay
    }

    // ===== SỬA: Audio toggle với logic tương tự MainMenu =====
    public void ToggleAudio(bool isOn)
    {
        Debug.Log($"GameManager Audio toggle changed to: {isOn}");

        isMusicEnabled = isOn;

        // Update both music sources
        if (musicSource != null)
        {
            musicSource.volume = isMusicEnabled ? musicVolume : 0f;
        }
        if (fadeAudioSource != null)
        {
            fadeAudioSource.volume = isMusicEnabled ? musicVolume : 0f;
        }

        // Update global audio listener
        AudioListener.volume = isMusicEnabled ? 1f : 0f;

        // Update icon
        UpdateAudioState(isOn);

        // Save setting
        SaveAudioSettings();

        Debug.Log($"GameManager Audio set to: {(isOn ? "ON" : "OFF")}");
    }

    private void UpdateAudioState(bool audioEnabled)
    {
        // Thay đổi icon
        if (audioToggleImage != null && audioOnSprite != null && audioOffSprite != null)
        {
            audioToggleImage.sprite = audioEnabled ? audioOnSprite : audioOffSprite;
        }

        // Bật/tắt âm thanh
        AudioListener.volume = audioEnabled ? 1f : 0f;
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Thêm debug method
    [ContextMenu("Test Clone Phase")]
    public void TestClonePhase()
    {
        if (SceneManager.GetActiveScene().name.Contains("Level2"))
        {
            if (clonePooling != null)
            {
                clonePooling.StartClonePhase();
                Debug.Log("Clone phase manually started for testing");
            }
        }
    }

    // ===== THÊM: Enhanced Audio Debug Methods =====
    [ContextMenu("Test Audio System")]
    public void TestAudioSystem()
    {
        Debug.Log($"Music Source: {musicSource != null}");
        Debug.Log($"Fade Audio Source: {fadeAudioSource != null}");
        Debug.Log($"Current Music: {currentMusicClip?.name ?? "None"}");
        Debug.Log($"Current Music State: {currentMusicState}");
        Debug.Log($"Music Enabled: {isMusicEnabled}");
        Debug.Log($"Music Volume: {musicVolume}");
        Debug.Log($"AudioListener Volume: {AudioListener.volume}");
        Debug.Log($"Is Transitioning: {isTransitioning}");
    }

    [ContextMenu("Test Level 1 Boss Phase Music")]
    public void TestLevel1BossPhaseMusic()
    {
        if (SceneManager.GetActiveScene().name.Contains("Level1"))
        {
            if (!isLevel1BossPhase1)
                PlayLevel1BossPhase1Music();
            else
                PlayLevel1BossPhase2Music();
        }
    }

    [ContextMenu("Test Level 2 Clone Music")]
    public void TestLevel2CloneMusic()
    {
        if (SceneManager.GetActiveScene().name.Contains("Level2"))
        {
            PlayLevel2ClonePhaseMusic();
        }
    }
}