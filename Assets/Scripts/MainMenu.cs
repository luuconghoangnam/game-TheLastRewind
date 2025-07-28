using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainMenuPanel; // Panel chính chứa các nút menu
    public GameObject settingsPanel; // Panel cài đặt

    [Header("Audio Control")]
    public MainMenuMusicController musicController; // Kéo thả MainMenuMusicController vào đây
    
    [Header("Settings UI")]
    public Toggle audioToggle; // Kéo thả Audio Toggle từ Settings Panel vào đây
    public Image audioToggleImage; // Image của toggle để thay đổi icon
    public Sprite audioOnSprite; // Icon khi audio bật
    public Sprite audioOffSprite; // Icon khi audio tắt

    // ===== THÊM: Save/Load Game UI =====
    [Header("Save/Load Game UI")]
    public Button loadGameButton; // Button để load game
    public Button clearSaveButton; // Button để xóa save
    public TextMeshProUGUI saveInfoText; // Text hiển thị thông tin save game

    // Flag để kiểm tra trạng thái settings
    private bool isSettingsOpen = false;

    private void Start()
    {
        // Đảm bảo settings panel được ẩn khi bắt đầu
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
            
        // Tự động tìm MusicController nếu chưa assign
        if (musicController == null)
        {
            musicController = FindFirstObjectByType<MainMenuMusicController>();
        }
        
        // ===== THÊM: Setup Audio Toggle =====
        SetupAudioToggle();
        
        // ===== THÊM: Setup Save/Load UI =====
        SetupSaveLoadUI();
    }

    private void Update()
    {
        // Kiểm tra nhấn phím Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettings();
        }
    }

    // ===== THÊM: Setup Audio Toggle =====
    private void SetupAudioToggle()
    {
        if (audioToggle != null)
        {
            // Load trạng thái audio đã lưu
            bool isMuted = PlayerPrefs.GetInt("AudioMuted", 0) == 1;
            audioToggle.isOn = !isMuted; // Toggle là "Audio ON", nên reverse logic
            
            // Setup listener cho toggle
            audioToggle.onValueChanged.AddListener(OnAudioToggleChanged);
            
            // Update icon ban đầu
            UpdateAudioToggleIcon(!isMuted);
            
            Debug.Log($"Audio toggle setup - isMuted: {isMuted}, toggle.isOn: {audioToggle.isOn}");
        }
        else
        {
            // Tự động tìm Audio Toggle nếu chưa assign
            audioToggle = FindAudioToggleInSettings();
            if (audioToggle != null)
            {
                SetupAudioToggle(); // Gọi lại để setup
            }
        }
    }

    // ===== THÊM: Setup Save/Load UI =====
    private void SetupSaveLoadUI()
    {
        // Setup Load Game button
        if (loadGameButton != null)
        {
            loadGameButton.onClick.AddListener(LoadGame);
        }
        
        // Setup Clear Save button
        if (clearSaveButton != null)
        {
            clearSaveButton.onClick.AddListener(ClearSaveGame);
        }
        
        // Update UI state
        UpdateSaveLoadUI();
    }
    
    // Cập nhật UI dựa trên trạng thái save game
    private void UpdateSaveLoadUI()
    {
        bool hasSave = GameManager.Instance != null && GameManager.Instance.HasSaveGame();
        
        // Enable/disable Load Game button
        if (loadGameButton != null)
        {
            loadGameButton.interactable = hasSave;
        }
        
        // ===== SỬA: Ẩn/hiện Clear Save button thay vì chỉ disable =====
        if (clearSaveButton != null)
        {
            clearSaveButton.gameObject.SetActive(hasSave);
        }
        
        // Update info text
        if (saveInfoText != null)
        {
            if (hasSave && GameManager.Instance != null)
            {
                saveInfoText.text = "Save Game:\n" + GameManager.Instance.GetSaveInfo();
                saveInfoText.color = Color.white;
            }
            else
            {
                saveInfoText.text = "Không có save game";
                saveInfoText.color = Color.gray;
            }
        }
        
        Debug.Log($"Save/Load UI updated - Has save: {hasSave}, Clear button visible: {hasSave}");
    }
    
    // Load game khi nhấn button
    public void LoadGame()
    {
        Debug.Log("Loading game from Main Menu...");
        
        if (GameManager.Instance != null)
        {
            // Stop music trước khi load
            if (musicController != null)
            {
                musicController.OnStartGame();
            }
            
            GameManager.Instance.LoadGame();
        }
        else
        {
            Debug.LogWarning("GameManager instance not found!");
        }
    }
    
    // Clear save game khi nhấn button
    public void ClearSaveGame()
    {
        Debug.Log("Clearing save game from Main Menu...");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ClearSaveGame();
            
            // Update UI after clearing
            UpdateSaveLoadUI();
        }
        else
        {
            Debug.LogWarning("GameManager instance not found!");
        }
    }
    
    // Update UI khi mở settings (để refresh save info)
    public void RefreshSaveInfo()
    {
        UpdateSaveLoadUI();
    }

    // ===== THÊM: Tìm Audio Toggle tự động =====
    private Toggle FindAudioToggleInSettings()
    {
        if (settingsPanel != null)
        {
            // Tìm toggle có tên "AudioToggle" hoặc chứa "Audio"
            Toggle[] toggles = settingsPanel.GetComponentsInChildren<Toggle>(true);
            foreach (Toggle toggle in toggles)
            {
                if (toggle.name.Contains("Audio") || toggle.name.Contains("audio"))
                {
                    Debug.Log($"Found audio toggle: {toggle.name}");
                    return toggle;
                }
            }
        }
        return null;
    }

    // ===== THÊM: Callback khi Audio Toggle thay đổi =====
    public void OnAudioToggleChanged(bool isAudioOn)
    {
        Debug.Log($"Audio toggle changed to: {isAudioOn}");
        
        // Update music controller
        if (musicController != null)
        {
            musicController.ToggleMute(!isAudioOn); // Reverse logic: isAudioOn = true → mute = false
        }
        
        // Update global audio listener (tương thích với GameManager)
        AudioListener.volume = isAudioOn ? 1f : 0f;
        
        // Save setting
        PlayerPrefs.SetInt("AudioMuted", isAudioOn ? 0 : 1);
        PlayerPrefs.Save();
        
        // Update icon
        UpdateAudioToggleIcon(isAudioOn);
        
        Debug.Log($"Audio set to: {(isAudioOn ? "ON" : "OFF")}");
    }

    // ===== SỬA: Update Audio Toggle Icon để match cấu trúc của bạn =====
    private void UpdateAudioToggleIcon(bool isAudioOn)
    {
        if (audioToggleImage != null && audioOnSprite != null && audioOffSprite != null)
        {
            audioToggleImage.sprite = isAudioOn ? audioOnSprite : audioOffSprite;
        }
        else if (audioToggle != null)
        {
            // ===== SỬA: Tìm Image theo cấu trúc AudioToggle > Background > Checkmark =====
            if (audioToggleImage == null)
            {
                // Thử tìm Checkmark image trước (thường là nơi hiển thị icon)
                Transform checkmarkTransform = audioToggle.transform.Find("Background/Checkmark");
                if (checkmarkTransform != null)
                {
                    audioToggleImage = checkmarkTransform.GetComponent<Image>();
                    Debug.Log("Found Checkmark Image for audio toggle");
                }
                
                // Nếu không có Checkmark, thử tìm Background
                if (audioToggleImage == null)
                {
                    Transform backgroundTransform = audioToggle.transform.Find("Background");
                    if (backgroundTransform != null)
                    {
                        audioToggleImage = backgroundTransform.GetComponent<Image>();
                        Debug.Log("Found Background Image for audio toggle");
                    }
                }
                
                // Cuối cùng, thử tìm bất kỳ Image nào
                if (audioToggleImage == null)
                {
                    audioToggleImage = audioToggle.GetComponentInChildren<Image>();
                    Debug.Log("Found generic Image for audio toggle");
                }
            }
            
            // Nếu có image nhưng không có sprites, chỉ log thông tin
            if (audioToggleImage != null)
            {
                Debug.Log($"Audio toggle image found: {audioToggleImage.name}, but sprites not assigned");
            }
            else
            {
                Debug.LogWarning("No Image component found in AudioToggle hierarchy");
            }
        }
    }

    // ===== THÊM: Public method để gọi từ Toggle Event (alternative) =====
    public void ToggleAudio(bool isAudioOn)
    {
        OnAudioToggleChanged(isAudioOn);
    }

    // Hàm để bật/tắt bảng settings
    public void ToggleSettings()
    {
        if (settingsPanel != null)
        {
            isSettingsOpen = !isSettingsOpen;
            settingsPanel.SetActive(isSettingsOpen);
            
            Debug.Log(isSettingsOpen ? "Settings panel opened" : "Settings panel closed");
        }
    }

    // Hàm để mở bảng settings
    public void OpenSettings()
    {
        if (settingsPanel != null && !isSettingsOpen)
        {
            isSettingsOpen = true;
            settingsPanel.SetActive(true);
            
            // ===== THÊM: Refresh save info khi mở settings =====
            RefreshSaveInfo();
            
            Debug.Log("Settings panel opened");
        }
    }

    // Hàm để đóng bảng settings
    public void CloseSettings()
    {
        if (settingsPanel != null && isSettingsOpen)
        {
            isSettingsOpen = false;
            settingsPanel.SetActive(false);
            Debug.Log("Settings panel closed");
        }
    }

    public void StartGame()
    {
        // ===== THÊM: Stop music khi start game =====
        if (musicController != null)
        {
            musicController.OnStartGame();
        }
        
        // Load the game scene (assuming it's named "GameScene")
        SceneManager.LoadScene("Level1");
    }

    // ===== THÊM: Start New Game (clear save và bắt đầu từ đầu) =====
    public void StartNewGame()
    {
        Debug.Log("Starting new game...");
        
        // Clear any existing save data để bắt đầu fresh
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ClearSaveGame();
        }
        
        // Stop music
        if (musicController != null)
        {
            musicController.OnStartGame();
        }
        
        // Load Level 1
        SceneManager.LoadScene("Level1");
    }

    public void QuitGame()
    {
        // ===== THÊM: Stop music khi quit =====
        if (musicController != null)
        {
            musicController.StopMusic();
        }
        
        // Quit the application
        Application.Quit();

        // If running in the editor, stop playing
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
