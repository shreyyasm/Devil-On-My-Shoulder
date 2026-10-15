using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    public static BackgroundMusic Instance { get; private set; }

    [Header("Intro Music")]
    [SerializeField] private AudioClip defaultMusic;
    [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 0.7f;
    [SerializeField] private string defaultTrackDisplayName = "THE RISER";

    [Header("Gameplay Music")]
    [SerializeField] private List<AudioClip> gameplayMusic = new();
    [SerializeField] private List<string> gameplayTrackDisplayNames = new()
    {
        "BUTCHER",
        "VENGEANCE",
        "REBELLION TOUCH THE SKY"
    };
    [SerializeField, Range(0f, 1f)] private float gameplayMusicVolume = 0.7f;

    [Header("Elevator Music")]
    [SerializeField] private AudioClip elevatorMusic;
    [SerializeField, Range(0f, 1f)] private float elevatorMusicVolume = 0.7f;
    [SerializeField, Min(0f)] private float elevatorMusicFadeDuration = 1f;

    [Header("Transition")]
    [SerializeField, Min(0f)] private float transitionDuration = 1.5f;
    [SerializeField, Min(0f)] private float iPodMusicFadeDuration = .75f;

    private AudioSource defaultMusicSource;
    private AudioSource gameplayMusicSource;
    private AudioSource elevatorMusicSource;
    private Coroutine transitionRoutine;
    private Coroutine elevatorTransitionRoutine;
    private bool gameplayStarted;
    private bool elevatorMusicActive;
    private float dialogueDuckMultiplier = 1f;
    private bool applicationHasFocus = true;
    private bool applicationIsPaused;
    private bool gameplayPlaybackSuspended;
    private AudioClip suspendedGameplayClip;
    private float suspendedGameplayTime;
    private float resumeRecoveryTime;

    public bool HasStartedGameplay => gameplayStarted;
    public bool IsElevatorMusicActive => elevatorMusicActive;
    public string CurrentTrackName
    {
        get
        {
            if (elevatorMusicSource != null && elevatorMusicSource.isPlaying && elevatorMusicSource.clip != null)
                return elevatorMusicSource.clip.name;
            if (gameplayMusicSource != null && gameplayMusicSource.isPlaying && gameplayMusicSource.clip != null)
                return GetGameplayTrackDisplayName(gameplayMusicSource.clip);
            return defaultMusicSource != null && defaultMusicSource.isPlaying && defaultMusicSource.clip != null
                ? defaultTrackDisplayName
                : string.Empty;
        }
    }

    private void Awake()
    {
        Instance = this;

        defaultMusicSource = GetComponent<AudioSource>();
        gameplayMusicSource = gameObject.AddComponent<AudioSource>();
        CopyMusicSettings(defaultMusicSource, gameplayMusicSource);
        elevatorMusicSource = gameObject.AddComponent<AudioSource>();
        CopyMusicSettings(defaultMusicSource, elevatorMusicSource);
        defaultMusicSource.playOnAwake = false;
        defaultMusicSource.Stop();

        if (defaultMusic == null)
            defaultMusic = defaultMusicSource.clip;
    }

    /// <summary>Starts the intro/default track from the player iPod cue.</summary>
    public void PlayDefaultMusicFromIPod()
    {
        if (gameplayStarted || elevatorMusicActive)
            return;

        PlayDefaultMusic();
    }

    private void Update()
    {
        if (IsMusicTemporarilySuspended())
        {
            RememberSuspendedGameplayTrack();
            return;
        }

        if (TryResumeSuspendedGameplayTrack())
            return;

        if (!elevatorMusicActive &&
            gameplayStarted &&
            transitionRoutine == null &&
            !gameplayMusicSource.isPlaying)
            PlayRandomGameplayTrack();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        applicationHasFocus = hasFocus;

        if (!hasFocus)
            RememberSuspendedGameplayTrack();
    }

    private void OnApplicationPause(bool paused)
    {
        applicationIsPaused = paused;

        if (paused)
            RememberSuspendedGameplayTrack();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void StartGameplayMusic()
    {
        if (gameplayStarted)
            return;

        gameplayStarted = true;
        elevatorMusicActive = false;
        ClearSuspendedGameplayTrack();

        if (GetRandomGameplayTrack() == null)
            return;

        transitionRoutine = StartCoroutine(TransitionToGameplay());
    }

    public void StartGameplayMusicImmediately()
    {
        if (gameplayStarted)
            return;

        AudioClip nextTrack = GetRandomGameplayTrack();

        if (nextTrack == null)
            return;

        gameplayStarted = true;
        elevatorMusicActive = false;
        ClearSuspendedGameplayTrack();
        defaultMusicSource.Stop();
        gameplayMusicSource.clip = nextTrack;
        gameplayMusicSource.loop = false;
        gameplayMusicSource.volume = 0f;
        gameplayMusicSource.Play();
        transitionRoutine = StartCoroutine(FadeInMusic(gameplayMusicSource, gameplayMusicVolume));
    }

    private void PlayDefaultMusic()
    {
        if (defaultMusic == null)
            return;

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        defaultMusicSource.Stop();
        defaultMusicSource.clip = defaultMusic;
        defaultMusicSource.loop = true;
        defaultMusicSource.volume = 0f;
        defaultMusicSource.Play();
        transitionRoutine = StartCoroutine(FadeInMusic(defaultMusicSource, defaultMusicVolume));
    }

    private IEnumerator TransitionToGameplay()
    {
        AudioClip nextTrack = GetRandomGameplayTrack();

        if (nextTrack == null)
        {
            transitionRoutine = null;
            yield break;
        }

        gameplayMusicSource.clip = nextTrack;
        gameplayMusicSource.loop = false;
        gameplayMusicSource.volume = 0f;
        gameplayMusicSource.Play();

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = transitionDuration <= 0f ? 1f : elapsed / transitionDuration;

            defaultMusicSource.volume = GetEffectiveVolume(Mathf.Lerp(defaultMusicVolume, 0f, progress));
            gameplayMusicSource.volume = GetEffectiveVolume(Mathf.Lerp(0f, gameplayMusicVolume, progress));
            yield return null;
        }

        defaultMusicSource.Stop();
        defaultMusicSource.volume = GetEffectiveVolume(defaultMusicVolume);
        gameplayMusicSource.volume = GetEffectiveVolume(gameplayMusicVolume);
        transitionRoutine = null;
    }

    private void PlayRandomGameplayTrack()
    {
        AudioClip nextTrack = GetRandomGameplayTrack();

        if (nextTrack == null)
            return;

        gameplayMusicSource.clip = nextTrack;
        gameplayMusicSource.loop = false;
        gameplayMusicSource.volume = GetEffectiveVolume(gameplayMusicVolume);
        gameplayMusicSource.Play();
    }

    /// <summary>Fades all regular background music to silence, then loops the lift track.</summary>
    public void StartElevatorMusic()
    {
        if (elevatorMusic == null)
            return;

        elevatorMusicActive = true;
        ClearSuspendedGameplayTrack();

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        if (elevatorTransitionRoutine != null)
            StopCoroutine(elevatorTransitionRoutine);

        elevatorTransitionRoutine = StartCoroutine(FadeToElevatorMusic());
    }

    /// <summary>Ends the lift track and starts a newly selected gameplay song.</summary>
    public void StopElevatorMusicAndStartFreshGameplayTrack()
    {
        if (elevatorTransitionRoutine != null)
        {
            StopCoroutine(elevatorTransitionRoutine);
            elevatorTransitionRoutine = null;
        }

        elevatorMusicActive = false;
        gameplayStarted = true;
        ClearSuspendedGameplayTrack();

        AudioClip nextTrack = GetRandomGameplayTrack();
        if (nextTrack == null)
            return;

        defaultMusicSource.Stop();
        gameplayMusicSource.Stop();
        gameplayMusicSource.clip = nextTrack;
        gameplayMusicSource.loop = false;
        gameplayMusicSource.volume = 0f;
        gameplayMusicSource.Play();
        transitionRoutine = StartCoroutine(FadeElevatorIntoGameplay());
    }

    private IEnumerator FadeInMusic(AudioSource source, float targetVolume)
    {
        float elapsed = 0f;

        while (elapsed < iPodMusicFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = iPodMusicFadeDuration <= 0f ? 1f : elapsed / iPodMusicFadeDuration;
            source.volume = GetEffectiveVolume(Mathf.Lerp(0f, targetVolume, progress));
            yield return null;
        }

        source.volume = GetEffectiveVolume(targetVolume);
        transitionRoutine = null;
    }

    private IEnumerator FadeElevatorIntoGameplay()
    {
        float elevatorStartVolume = elevatorMusicSource != null ? elevatorMusicSource.volume : 0f;
        float elapsed = 0f;

        while (elapsed < iPodMusicFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = iPodMusicFadeDuration <= 0f ? 1f : elapsed / iPodMusicFadeDuration;

            if (elevatorMusicSource != null)
                elevatorMusicSource.volume = Mathf.Lerp(elevatorStartVolume, 0f, progress);
            gameplayMusicSource.volume = GetEffectiveVolume(Mathf.Lerp(0f, gameplayMusicVolume, progress));
            yield return null;
        }

        if (elevatorMusicSource != null)
        {
            elevatorMusicSource.Stop();
            elevatorMusicSource.volume = GetEffectiveVolume(elevatorMusicVolume);
        }

        gameplayMusicSource.volume = GetEffectiveVolume(gameplayMusicVolume);
        transitionRoutine = null;
    }

    private IEnumerator FadeToElevatorMusic()
    {
        float defaultStartVolume = defaultMusicSource != null ? defaultMusicSource.volume : 0f;
        float gameplayStartVolume = gameplayMusicSource != null ? gameplayMusicSource.volume : 0f;
        float elapsed = 0f;

        while (elapsed < elevatorMusicFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elevatorMusicFadeDuration <= 0f ? 1f : elapsed / elevatorMusicFadeDuration;

            if (defaultMusicSource != null)
                defaultMusicSource.volume = Mathf.Lerp(defaultStartVolume, 0f, progress);
            if (gameplayMusicSource != null)
                gameplayMusicSource.volume = Mathf.Lerp(gameplayStartVolume, 0f, progress);

            yield return null;
        }

        defaultMusicSource.Stop();
        gameplayMusicSource.Stop();
        defaultMusicSource.volume = GetEffectiveVolume(defaultMusicVolume);
        gameplayMusicSource.volume = GetEffectiveVolume(gameplayMusicVolume);

        elevatorMusicSource.clip = elevatorMusic;
        elevatorMusicSource.loop = true;
        elevatorMusicSource.volume = GetEffectiveVolume(elevatorMusicVolume);
        elevatorMusicSource.Play();
        elevatorTransitionRoutine = null;
    }

    private bool IsMusicTemporarilySuspended()
    {
        return !applicationHasFocus || applicationIsPaused || AudioListener.pause;
    }

    private void RememberSuspendedGameplayTrack()
    {
        if (!gameplayStarted || gameplayMusicSource == null || gameplayMusicSource.clip == null)
            return;

        if (!gameplayPlaybackSuspended)
        {
            suspendedGameplayClip = gameplayMusicSource.clip;
            suspendedGameplayTime = gameplayMusicSource.time;
        }

        gameplayPlaybackSuspended = true;
    }

    // Returns true while this source is being restored, preventing Update from
    // misreading a system pause as the end of a gameplay track.
    private bool TryResumeSuspendedGameplayTrack()
    {
        if (!gameplayPlaybackSuspended)
            return false;

        gameplayMusicSource.UnPause();

        if (gameplayMusicSource.isPlaying)
        {
            ClearSuspendedGameplayTrack();
            return false;
        }

        if (resumeRecoveryTime <= 0f)
        {
            // Give Unity's audio system a short moment to restore sound after
            // Alt-Tab, the Windows key, or the pause menu is closed.
            resumeRecoveryTime = Time.unscaledTime + .25f;
            return true;
        }

        if (Time.unscaledTime < resumeRecoveryTime)
            return true;

        if (suspendedGameplayClip != null)
        {
            gameplayMusicSource.clip = suspendedGameplayClip;
            gameplayMusicSource.time = Mathf.Clamp(suspendedGameplayTime, 0f,
                Mathf.Max(0f, suspendedGameplayClip.length - .01f));
            gameplayMusicSource.volume = GetEffectiveVolume(gameplayMusicVolume);
            gameplayMusicSource.Play();
        }

        ClearSuspendedGameplayTrack();
        return true;
    }

    private void ClearSuspendedGameplayTrack()
    {
        gameplayPlaybackSuspended = false;
        suspendedGameplayClip = null;
        suspendedGameplayTime = 0f;
        resumeRecoveryTime = 0f;
    }

    private AudioClip GetRandomGameplayTrack()
    {
        List<AudioClip> availableTracks = gameplayMusic.FindAll(clip => clip != null);

        if (availableTracks.Count == 0)
            return null;

        AudioClip nextTrack = availableTracks[Random.Range(0, availableTracks.Count)];

        if (availableTracks.Count > 1 && nextTrack == gameplayMusicSource.clip)
        {
            do
            {
                nextTrack = availableTracks[Random.Range(0, availableTracks.Count)];
            }
            while (nextTrack == gameplayMusicSource.clip);
        }

        return nextTrack;
    }

    private string GetGameplayTrackDisplayName(AudioClip clip)
    {
        int index = gameplayMusic.IndexOf(clip);
        if (index >= 0 && index < gameplayTrackDisplayNames.Count &&
            !string.IsNullOrWhiteSpace(gameplayTrackDisplayNames[index]))
            return gameplayTrackDisplayNames[index];

        return clip.name;
    }

    public void SetDialogueDucking(float multiplier)
    {
        dialogueDuckMultiplier = Mathf.Clamp01(multiplier);

        if (defaultMusicSource != null && defaultMusicSource.isPlaying)
            defaultMusicSource.volume = GetEffectiveVolume(defaultMusicVolume);

        if (gameplayMusicSource != null && gameplayMusicSource.isPlaying)
            gameplayMusicSource.volume = GetEffectiveVolume(gameplayMusicVolume);
    }

    private float GetEffectiveVolume(float baseVolume)
    {
        return baseVolume * dialogueDuckMultiplier;
    }

    private static void CopyMusicSettings(AudioSource source, AudioSource destination)
    {
        destination.outputAudioMixerGroup = source.outputAudioMixerGroup;
        destination.playOnAwake = false;
        destination.spatialBlend = source.spatialBlend;
        destination.dopplerLevel = source.dopplerLevel;
        destination.spread = source.spread;
        destination.priority = source.priority;
        destination.ignoreListenerPause = source.ignoreListenerPause;
        destination.ignoreListenerVolume = source.ignoreListenerVolume;
    }
}
