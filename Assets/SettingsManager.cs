using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider mouseSensitivitySlider;

    [Header("Audio (Optional but Recommended)")]
    [SerializeField] private AudioMixer audioMixer;         // Your mixer asset
    [SerializeField] private string sfxVolumeParam = "SFXVol"; // Exposed parameter on the mixer

    [Header("Defaults")]
    [Range(0f, 1f)] public float defaultSfxVolume = 0.8f;   // slider value (0..1)
    [Range(0.1f, 2f)] public float defaultMouseSens = 1f;

    public TextMeshProUGUI username;
    // Public accessors if other scripts want current values
    public static float MouseSensitivity { get; private set; } = 1f;
    public static float SfxVolume01 { get; private set; } = 0.8f;

    // PlayerPrefs keys
    const string KEY_SFX = "SETTINGS_SFX_VOLUME";
    const string KEY_SENS = "SETTINGS_MOUSE_SENS";
    public FunticoSDKExample FunticoSDKExample;

    public TextMeshProUGUI highscoreText;
    private void Start()
    {
        if(username != null)
        {
            Debug.Log(FunticoSDKExample.username);
            username.text = FunticoSDKExample.username;
        }
        if(highscoreText != null)
        {
            highscoreText.text = "Highscore: " + PlayerPrefs.GetInt("HighScore", 0);
        }
    }
    public void StartGame()
    {
        SceneManager.LoadScene(5);
    }
    void Awake()
    {
        FunticoSDKExample = FindFirstObjectByType<FunticoSDKExample>();
        // Load saved values or defaults
        float sfx = PlayerPrefs.GetFloat(KEY_SFX, defaultSfxVolume);
        float sens = PlayerPrefs.GetFloat(KEY_SENS, defaultMouseSens);

        // Clamp just in case
        sfx = Mathf.Clamp01(sfx);
        sens = Mathf.Clamp(sens, 0.1f, 2f);

        // Apply & push to UI
        ApplySfxVolume(sfx);
        ApplyMouseSensitivity(sens);

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.value = sfx;
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxSliderChanged);
        }

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.minValue = 0.1f;
            mouseSensitivitySlider.maxValue = 2f;
            mouseSensitivitySlider.value = sens;
            mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSliderChanged);
        }
    }

    // UI callbacks
    public void OnSfxSliderChanged(float value)
    {
        ApplySfxVolume(value);
        PlayerPrefs.SetFloat(KEY_SFX, SfxVolume01);
        PlayerPrefs.Save();
    }

    public void OnMouseSliderChanged(float value)
    {
        ApplyMouseSensitivity(value);
        PlayerPrefs.SetFloat(KEY_SENS, MouseSensitivity);
        PlayerPrefs.Save();
    }

    // Applyers
    void ApplySfxVolume(float value01)
    {
        SfxVolume01 = Mathf.Clamp01(value01);

        // Prefer AudioMixer (logarithmic)
        if (audioMixer != null && !string.IsNullOrEmpty(sfxVolumeParam))
        {
            // Convert 0..1 slider to decibels (-80dB .. 0dB). Use -80 as "mute".
            float dB = (SfxVolume01 > 0.0001f) ? Mathf.Lerp(-30f, 0f, Mathf.Log10(Mathf.Lerp(0.001f, 1f, SfxVolume01))) : -80f;
            audioMixer.SetFloat(sfxVolumeParam, dB);
        }
        else
        {
            // Fallback: scale all active AudioSources tagged "SFX" (or just set AudioListener.volume)
            // Simple global fallback:
            AudioListener.volume = SfxVolume01;
        }
    }

    void ApplyMouseSensitivity(float sens)
    {
        MouseSensitivity = Mathf.Clamp(sens, 0.1f, 2f);
        // If your look script subscribes, you could broadcast an event here.
    }

    public AudioSource audioSource;
    public AudioClip hoverSFX;

    public void HoverSFX()
    {
        audioSource.PlayOneShot(hoverSFX, 1f);
    }
    public GameObject settingsMenu;
    public void OpenSettings()
    {
        settingsMenu.SetActive(true);
    }
    public void closeSettings()
    {
        settingsMenu.SetActive(false);
    }
}
