using UnityEngine;

public class MainMenuMusicController : MonoBehaviour
{
    [Header("Main Menu Music")]
    public AudioClip mainMenuMusic; // Kéo thả file nhạc vào đây

    [Header("Audio Settings")]
    [Range(0f, 1f)]
    public float volume = 0.7f;
    public bool playOnAwake = true;
    public bool loop = true;

    private AudioSource audioSource;

    private void Awake()
    {
        // Setup AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        ConfigureAudioSource();
    }

    private void Start()
    {
        // Load audio settings từ PlayerPrefs (tương thích với GameManager)
        LoadAudioSettings();

        // Phát nhạc nếu được bật
        if (playOnAwake && mainMenuMusic != null)
        {
            PlayMusic();
        }
    }

    private void ConfigureAudioSource()
    {
        audioSource.clip = mainMenuMusic;
        audioSource.loop = loop;
        audioSource.playOnAwake = false; // Chúng ta sẽ tự control
        audioSource.volume = volume;
    }

    private void LoadAudioSettings()
    {
        // Kiểm tra setting từ GameManager (nếu có)
        bool isMuted = PlayerPrefs.GetInt("AudioMuted", 0) == 1;

        if (isMuted)
        {
            audioSource.volume = 0f;
        }
        else
        {
            audioSource.volume = volume;
        }

        Debug.Log($"MainMenu music volume set to: {audioSource.volume} (Muted: {isMuted})");
    }

    public void PlayMusic()
    {
        if (mainMenuMusic != null && audioSource != null)
        {
            audioSource.clip = mainMenuMusic;
            audioSource.Play();
            Debug.Log("MainMenu music started playing");
        }
        else
        {
            Debug.LogWarning("MainMenu music clip is missing!");
        }
    }

    public void StopMusic()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            Debug.Log("MainMenu music stopped");
        }
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }

    public void ToggleMute(bool mute)
    {
        if (audioSource != null)
        {
            audioSource.volume = mute ? 0f : volume;
        }
    }

    // Method để gọi từ MainMenu buttons (nếu cần)
    public void OnStartGame()
    {
        // Stop music khi chuyển scene
        StopMusic();
    }

    private void OnDestroy()
    {
        // Clean up khi scene thay đổi
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}