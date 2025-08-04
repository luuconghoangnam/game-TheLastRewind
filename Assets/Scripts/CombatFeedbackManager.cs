using UnityEngine;
using System.Collections;
#if CINEMACHINE_PRESENT
using Cinemachine;
#endif

public class CombatFeedbackManager : MonoBehaviour
{
    public static CombatFeedbackManager Instance { get; private set; }

    [Header("Camera Shake Settings")]
    public bool useCinemachineImpulse = true;
    public float shakeIntensity = 0.5f;
    public float shakeDuration = 0.2f;

    [Header("Hit Stop")]
    public float hitStopDuration = 0.1f;
    public float hitStopTimeScale = 0.1f;

    [Header("Hit Flash")]
    public Color hitFlashColor = new Color(1f, 0.5f, 0.5f, 1f);
    public float hitFlashDuration = 0.1f;

    [Header("Audio Feedback")]
    public AudioClip[] lightHitSounds;
    public AudioClip[] heavyHitSounds;
    public AudioClip[] blockSounds;
    public AudioClip[] playerHurtSounds;

    private Camera mainCamera;
    private Vector3 originalCameraPosition;
    private AudioSource audioSource;
#if CINEMACHINE_PRESENT
    private CinemachineImpulseSource impulseSource;
#endif

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = 0.7f;

#if CINEMACHINE_PRESENT
            // Thử thêm Cinemachine Impulse Source nếu có Cinemachine
            try {
                impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
                if (impulseSource != null) {
                    impulseSource.m_ImpulseDefinition.m_ImpulseDuration = shakeDuration;
                    impulseSource.m_ImpulseDefinition.m_AmplitudeGain = shakeIntensity;
                    impulseSource.m_ImpulseDefinition.m_FrequencyGain = 1f;
                    impulseSource.m_DefaultVelocity = new Vector3(1f, 1f, 0);
                    Debug.Log("Cinemachine Impulse Source added successfully");
                }
            }
            catch (System.Exception e) {
                Debug.LogError("Failed to add Cinemachine Impulse Source: " + e.Message);
            }
#endif
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        FindMainCamera();
    }

    private void FindMainCamera()
    {
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            originalCameraPosition = mainCamera.transform.localPosition;
        }
        else
        {
            Debug.LogWarning("Main camera not found!");
        }
    }

    public void UpdateReferences()
    {
        FindMainCamera();
    }

    public void ShakeCamera(float intensity = -1, float duration = -1)
    {
        float finalIntensity = intensity > 0 ? intensity : shakeIntensity;
        float finalDuration = duration > 0 ? duration : shakeDuration;

#if CINEMACHINE_PRESENT
        if (impulseSource != null)
        {
            // Cập nhật các thông số Impulse
            impulseSource.m_ImpulseDefinition.m_ImpulseDuration = finalDuration;
            impulseSource.m_ImpulseDefinition.m_AmplitudeGain = finalIntensity;

            // Kích hoạt impulse
            impulseSource.GenerateImpulse();
            Debug.Log($"Cinemachine impulse generated with intensity: {finalIntensity}, duration: {finalDuration}");
        }
        else
#endif
        {
            // Fallback nếu không dùng Cinemachine
            if (mainCamera == null)
            {
                FindMainCamera();
                if (mainCamera == null) return;
            }

            StopCoroutine("DoCameraShake");
            StartCoroutine(DoCameraShake(finalIntensity, finalDuration));
        }
    }

    private IEnumerator DoCameraShake(float intensity, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            Vector3 randomOffset = Random.insideUnitSphere * intensity;
            mainCamera.transform.localPosition = originalCameraPosition + randomOffset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.localPosition = originalCameraPosition;
    }

    public void DoHitStop(float duration = -1)
    {
        StopCoroutine("DoTimeScale");
        StartCoroutine(DoTimeScale(
            duration > 0 ? duration : hitStopDuration));
    }

    private IEnumerator DoTimeScale(float duration)
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = hitStopTimeScale;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = originalTimeScale;
    }

    public void PlayHitSound(bool isHeavyHit = false)
    {
        AudioClip[] soundArray = isHeavyHit ? heavyHitSounds : lightHitSounds;
        if (soundArray != null && soundArray.Length > 0)
        {
            AudioClip clip = soundArray[Random.Range(0, soundArray.Length)];
            if (clip != null)
                audioSource.PlayOneShot(clip);
        }
    }

    public void PlayBlockSound()
    {
        if (blockSounds != null && blockSounds.Length > 0)
        {
            AudioClip clip = blockSounds[Random.Range(0, blockSounds.Length)];
            if (clip != null)
                audioSource.PlayOneShot(clip);
        }
    }

    public void PlayPlayerHurtSound()
    {
        if (playerHurtSounds != null && playerHurtSounds.Length > 0)
        {
            AudioClip clip = playerHurtSounds[Random.Range(0, playerHurtSounds.Length)];
            if (clip != null)
                audioSource.PlayOneShot(clip);
        }
    }
}