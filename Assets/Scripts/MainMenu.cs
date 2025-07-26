using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainMenuPanel; // Panel chính chứa các nút menu
    public GameObject settingsPanel; // Panel cài đặt

    // Flag để kiểm tra trạng thái settings
    private bool isSettingsOpen = false;

    private void Start()
    {
        // Đảm bảo settings panel được ẩn khi bắt đầu
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void Update()
    {
        // Kiểm tra nhấn phím Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettings();
        }
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
        // Load the game scene (assuming it's named "GameScene")
        SceneManager.LoadScene("Level1");
    }

    public void QuitGame()
    {
        // Quit the application
        Application.Quit();

        // If running in the editor, stop playing
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
