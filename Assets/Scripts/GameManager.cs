using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro; // Thêm dòng này

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject gameOverPanel;
    public GameObject victoryPanel;
    public GameObject settingsPanel;
    
    [Header("Dialogue Banner (Level 1 Only)")]
    public GameObject dialogueBanner;
    public TextMeshProUGUI dialogueText; // Thay đổi từ Text thành TextMeshProUGUI
    public string level1DialogueMessage = "You dare enter my domain? Prepare to face your doom!";
    public float textSpeed = 0.05f; // Tốc độ xuất hiện từng ký tự
    
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
    public string mainMenuSceneName = "MainMenu";
    public string nextLevelSceneName = "Level2";

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
        
        // Show dialogue banner if this is Level 1
        if (scene.name == "Level1" || scene.name.Contains("Level1"))
        {
            StartCoroutine(ShowLevel1Dialogue());
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
            if (playerComponent != null)
            {
                playerComponent.enabled = false;
            }
        }
        
        // Vô hiệu hóa boss AI - Sử dụng FindFirstObjectByType thay vì FindObjectOfType
        bossAiController bossAI = FindFirstObjectByType<bossAiController>();
        if (bossAI != null)
        {
            bossAI.SetAIActive(false);
        }
    }

    private void EnableGameplayControls()
    {
        // Kích hoạt lại player movement - Sửa từ PlayerController thành Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Player playerComponent = player.GetComponent<Player>();
            if (playerComponent != null)
            {
                playerComponent.enabled = true;
            }
        }
        
        // Kích hoạt lại boss AI - Sử dụng FindFirstObjectByType thay vì FindObjectOfType
        bossAiController bossAI = FindFirstObjectByType<bossAiController>();
        if (bossAI != null)
        {
            bossAI.SetAIActive(true);
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

    private void OnDestroy()
    {
        // Hủy đăng ký sự kiện khi GameManager bị hủy
        SceneManager.sceneLoaded -= OnSceneLoaded;
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
                dialogueText.text = level1DialogueMessage;
            }
            StartCoroutine(SkipToEndDialogue());
        }
    }

    private IEnumerator SkipToEndDialogue()
    {
        // Đợi 1 giây rồi kết thúc dialogue
        yield return new WaitForSeconds(1f);
        
        if (dialogueBanner) dialogueBanner.SetActive(false);
        EnableGameplayControls();
        isShowingDialogue = false;
        
        Debug.Log("Level 1 dialogue skipped!");
    }

    public void GameOver()
    {
        StartCoroutine(ShowGameOverPanel());
    }

    public void Victory()
    {
        StartCoroutine(ShowVictoryPanel());
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
}
