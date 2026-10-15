using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

[DefaultExecutionOrder(-500)]
public class AudioMixerRoutingManager : MonoBehaviour
{
    public static AudioMixerRoutingManager Instance { get; private set; }

    [SerializeField] private AudioMixerGroup musicOutput;
    [SerializeField] private AudioMixerGroup effectsOutput;
    [SerializeField] private AudioMixerGroup voiceOutput;
    [SerializeField] private Kamgam.SettingsGenerator.SettingsProvider audioSettingsProvider;
    [SerializeField, Min(0.05f)] private float rescanInterval = 0.25f;

    private readonly HashSet<int> routedSources = new HashSet<int>();
    private float nextRescanTime;

    private void Awake()
    {
        Instance = this;
        RouteAllAudioSources();
        StartCoroutine(ApplySavedAudioSettingsAtStartup());
    }

    private System.Collections.IEnumerator ApplySavedAudioSettingsAtStartup()
    {
        // SettingsInitializer creates its runtime settings copy in Start(), so wait one frame
        // before sending the stored slider values to the mixer.
        yield return null;

        // The Audio tab uses its own SettingsProvider. Do not use the generic
        // scene initializer here: it can point at a different settings asset.
        if (audioSettingsProvider == null)
            yield break;

        Kamgam.SettingsGenerator.Settings settings = audioSettingsProvider.Settings;
        if (settings == null)
            yield break;

        // Unity applies mixer snapshots during startup. Reapply after that work
        // completes so the saved values are already audible before input begins.
        for (int attempt = 0; attempt < 3; attempt++)
        {
            ApplyAudioSetting(settings, "audioMasterVolume");
            ApplyAudioSetting(settings, "audioMusicVolume");
            ApplyAudioSetting(settings, "audioEffectVolume");
            ApplyAudioSetting(settings, "audioVoiceVolume");

            yield return new WaitForEndOfFrame();
        }

        CreateVoiceVolumeSlider();
    }

    private static void ApplyAudioSetting(Kamgam.SettingsGenerator.Settings settings, string id)
    {
        Kamgam.SettingsGenerator.SettingFloat setting = settings.GetOrCreateFloat(id);
        if (setting.IsActive && setting.HasConnection())
            setting.PushToConnection();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRescanTime)
            return;
        nextRescanTime = Time.unscaledTime + rescanInterval;
        RouteAllAudioSources();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RouteAllAudioSources()
    {
        if (effectsOutput == null)
            return;

        foreach (AudioSource source in FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (source == null || !routedSources.Add(source.GetInstanceID()))
                continue;

            if (source.GetComponent<VoiceLineManager>() != null && voiceOutput != null)
                source.outputAudioMixerGroup = voiceOutput;
            else
                source.outputAudioMixerGroup = source.GetComponent<BackgroundMusic>() != null ? musicOutput : effectsOutput;
        }

        foreach (BackgroundMusic backgroundMusic in FindObjectsByType<BackgroundMusic>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            foreach (AudioSource source in backgroundMusic.GetComponents<AudioSource>())
                source.outputAudioMixerGroup = musicOutput;
        }
    }

    private void CreateVoiceVolumeSlider()
    {
        // The Settings Generator UI is prefab-based. Copy its existing Effects slider so
        // this control inherits the same layout, styling, and save/apply behavior.
        Kamgam.SettingsGenerator.SettingResolver[] resolvers =
            FindObjectsByType<Kamgam.SettingsGenerator.SettingResolver>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        Kamgam.SettingsGenerator.SettingResolver effectSlider = null;
        foreach (Kamgam.SettingsGenerator.SettingResolver resolver in resolvers)
        {
            if (resolver.ID == "audioVoiceVolume")
            {
                resolver.Refresh();
                // Refresh rebuilds the generated setting label from its source
                // prefab, so rename it afterwards.
                SetVoiceSliderLabel(resolver.gameObject);
                return;
            }

            if (resolver.ID == "audioEffectVolume")
                effectSlider = resolver;
        }

        if (effectSlider == null)
        {
            Debug.LogWarning("AudioMixerRoutingManager could not find the Effects volume slider to duplicate.", this);
            return;
        }

        GameObject voiceSliderObject = Instantiate(effectSlider.gameObject, effectSlider.transform.parent);
        voiceSliderObject.name = "SliderUGUI (Demon Dialogs and Voice Lines)";
        voiceSliderObject.transform.SetSiblingIndex(effectSlider.transform.GetSiblingIndex() + 1);

        Kamgam.SettingsGenerator.SettingResolver voiceSlider =
            voiceSliderObject.GetComponent<Kamgam.SettingsGenerator.SettingResolver>();
        voiceSlider.ID = "audioVoiceVolume";
        voiceSlider.Refresh();
        // Refresh copies "Effect Volume" from the duplicated template.
        SetVoiceSliderLabel(voiceSliderObject);
    }

    private static void SetVoiceSliderLabel(GameObject voiceSliderObject)
    {
        foreach (TextMeshProUGUI text in voiceSliderObject.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text.text == "Effect Volume")
                text.text = "Demon Voice Volume";
        }

        foreach (Text text in voiceSliderObject.GetComponentsInChildren<Text>(true))
        {
            if (text.text == "Effect Volume")
                text.text = "Demon Voice Volume";
        }
    }

    public static void PlayClipAtPoint(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null)
            return;

        GameObject audioObject = new GameObject("One Shot SFX");
        audioObject.transform.position = position;
        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 1f;
        if (Instance != null && Instance.effectsOutput != null)
            source.outputAudioMixerGroup = Instance.effectsOutput;
        source.Play();
        Destroy(audioObject, clip.length / Mathf.Max(0.01f, source.pitch));
    }
}
