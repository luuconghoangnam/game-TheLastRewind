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
    public string level1DialogueMessage = "It's you! Again...";
    public string level1PostBossDialogue = "I once was harsh, too proud to speak \nYour silent pain—my heart grew weak.";

    // ===== THÊM: Level 2 dialogue messages =====
    [Header("Level 2 Dialogue Messages")]
    public string level2BossIntroDialogue = "Remember me?...I guess not...";
    public string level2BossReappearDialogue = "You think defeating my minions means victory? \nNow face my true wrath!";
    public string level2PostBossDialogue = "You stood alone beside the door \nI let you drift, ignored once more.";

    [Header("Victory Panel Buttons")]
    public Button continueButton;
    public Button saveGameButton;
    public Button victorySettingsButton;
    public Button victoryQuitButton;

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
    public Toggle audioToggle;
    public Image audioToggleImage;
    public Sprite audioOnSprite;
    public Sprite audioOffSprite;

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

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Đăng ký callback khi scene được tải
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
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

        // Find UI elements in new scene
        FindUIReferences();

        // Ensure all panels are hidden on scene load
        HideAllPanels();

        // Setup UI listeners
        SetupUIListeners();

        // ===== LEVEL-SPECIFIC INITIALIZATION =====
        if (scene.name == "Level1" || scene.name.Contains("Level1"))
        {
            InitializeLevel1();
        }
        else if (scene.name == "Level2" || scene.name.Contains("Level2"))
        {
            InitializeLevel2();
        }
    }

    // ===== LEVEL 1 INITIALIZATION =====
    private void InitializeLevel1()
    {
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

        // ===== THÊM: Kích hoạt clone phase SAU KHI boss đã biến mất =====
        if (clonePooling != null)
        {
            clonePooling.StartClonePhase();
            Debug.Log("Clone phase activated after boss disappeared");
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
            victorySettingsButton = FindButtonInChildren(victoryPanel, "SettingsButton");
            victoryQuitButton = FindButtonInChildren(victoryPanel, "QuitButton");
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
            audioToggle = FindComponentInChildren<Toggle>(settingsPanel, "AudioToggle");
            if (audioToggle != null)
            {
                audioToggleImage = audioToggle.transform.Find("Image")?.GetComponent<Image>();
                if (audioToggleImage == null)
                    audioToggleImage = audioToggle.GetComponentInChildren<Image>();
            }
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

        // Audio toggle
        if (audioToggle)
        {
            audioToggle.onValueChanged.AddListener(ToggleAudio);
            // Khởi tạo trạng thái âm thanh
            bool isMuted = PlayerPrefs.GetInt("AudioMuted", 0) == 1;
            audioToggle.isOn = !isMuted;
            UpdateAudioState(!isMuted);
        }
    }

    private void Start()
    {
        // Initial setup for the first scene
        HideAllPanels();
        SetupUIListeners();
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
        if (isShowingDialogue) return;

        // Xử lý nút Esc để mở/đóng Settings panel
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Nếu settings panel đang mở, đóng nó lại
            if (settingsPanel && settingsPanel.activeSelf)
            {
                CloseSettings();
            }
            // Nếu settings panel đang đóng, mở nó ra (trừ khi đang ở game over/victory)
            else if (settingsPanel && !settingsPanel.activeSelf &&
                    !(gameOverPanel && gameOverPanel.activeSelf) &&
                    !(victoryPanel && victoryPanel.activeSelf))
            {
                OpenSettings();
            }
        }

        // Cho phép skip dialogue bằng Space hoặc Enter
        if (isShowingDialogue && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
        {
            // Skip typing effect và hiển thị toàn bộ text
            StopAllCoroutines();
            if (dialogueText != null)
            {
                // ===== SỬA: Xác định message hiện tại để skip cho Level 2 =====
                if (SceneManager.GetActiveScene().name.Contains("Level1"))
                {
                    dialogueText.text = level1DialogueMessage.Contains("dare") ? level1DialogueMessage : level1PostBossDialogue;
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
        StartCoroutine(ShowGameOverPanel());
    }

    // ===== SỬA ĐỔI: Victory() giờ sẽ hiển thị dialogue trước cho cả Level 1 và Level 2 =====
    public void Victory()
    {
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

    public void SaveGame()
    {
        Debug.Log("Saving game...");
        // Lưu scene hiện tại
        PlayerPrefs.SetString("SavedScene", SceneManager.GetActiveScene().name);

        // Lưu vị trí người chơi nếu có thể
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerPrefs.SetFloat("PlayerPosX", player.transform.position.x);
            PlayerPrefs.SetFloat("PlayerPosY", player.transform.position.y);
            PlayerPrefs.SetFloat("PlayerPosZ", player.transform.position.z);

            // Lưu máu nếu Player có component máu
            Player playerComponent = player.GetComponent<Player>();
            if (playerComponent != null)
            {
                PlayerPrefs.SetInt("PlayerHealth", playerComponent.CurrentHealth);
                PlayerPrefs.SetInt("PlayerMaxHealth", playerComponent.MaxHealth);
            }
        }

        // Lưu trữ
        PlayerPrefs.Save();

        Debug.Log("Game saved successfully!");
    }

    public void LoadGame()
    {
        Debug.Log("Loading game...");

        // Kiểm tra xem có save game không
        if (PlayerPrefs.HasKey("SavedScene"))
        {
            string savedScene = PlayerPrefs.GetString("SavedScene");

            // Tải scene đã lưu
            SceneManager.LoadScene(savedScene);

            // Đặt lại thời gian
            Time.timeScale = 1f;

            // Các giá trị khác sẽ được thiết lập sau khi scene được tải
            StartCoroutine(SetupPlayerAfterLoad());
        }
        else
        {
            Debug.Log("No saved game found!");
        }
    }

    private IEnumerator SetupPlayerAfterLoad()
    {
        // Đợi một frame để đảm bảo scene đã tải xong
        yield return null;

        // Tìm player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Đặt lại vị trí
            if (PlayerPrefs.HasKey("PlayerPosX"))
            {
                float x = PlayerPrefs.GetFloat("PlayerPosX");
                float y = PlayerPrefs.GetFloat("PlayerPosY");
                float z = PlayerPrefs.GetFloat("PlayerPosZ");
                player.transform.position = new Vector3(x, y, z);
            }

            // Đặt lại máu
            Player playerComponent = player.GetComponent<Player>();
            if (playerComponent != null && PlayerPrefs.HasKey("PlayerHealth"))
            {
                // Dùng reflection hoặc phương thức public để thiết lập máu
                // Giả sử Player có phương thức SetHealth
                int health = PlayerPrefs.GetInt("PlayerHealth");
                playerComponent.Heal(health);
            }
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

    public void ToggleAudio(bool isOn)
    {
        UpdateAudioState(isOn);

        // Lưu trạng thái âm thanh
        PlayerPrefs.SetInt("AudioMuted", isOn ? 0 : 1);
        PlayerPrefs.Save();
    }

    private void UpdateAudioState(bool audioEnabled)
    {
        // Thay đổi icon
        if (audioToggleImage != null)
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
}