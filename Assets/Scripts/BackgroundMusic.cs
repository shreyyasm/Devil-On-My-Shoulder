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

    [Header("Gameplay Music")]
    [SerializeField] private List<AudioClip> gameplayMusic = new();
    [SerializeField, Range(0f, 1f)] private float gameplayMusicVolume = 0.7f;

    [Header("Transition")]
    [SerializeField, Min(0f)] private float transitionDuration = 1.5f;

    private AudioSource defaultMusicSource;
    private AudioSource gameplayMusicSource;
    private Coroutine transitionRoutine;
    private bool gameplayStarted;

    private void Awake()
    {
        Instance = this;

        defaultMusicSource = GetComponent<AudioSource>();
        gameplayMusicSource = gameObject.AddComponent<AudioSource>();
        CopyMusicSettings(defaultMusicSource, gameplayMusicSource);
        defaultMusicSource.playOnAwake = false;
        defaultMusicSource.Stop();

        if (defaultMusic == null)
            defaultMusic = defaultMusicSource.clip;
    }

    private void Start()
    {
        if (!gameplayStarted)
            PlayDefaultMusic();
    }

    private void Update()
    {
        if (gameplayStarted &&
            transitionRoutine == null &&
            !AudioListener.pause &&
            !gameplayMusicSource.isPlaying)
            PlayRandomGameplayTrack();
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
        defaultMusicSource.Stop();
        gameplayMusicSource.clip = nextTrack;
        gameplayMusicSource.loop = false;
        gameplayMusicSource.volume = gameplayMusicVolume;
        gameplayMusicSource.Play();
    }

    private void PlayDefaultMusic()
    {
        if (defaultMusic == null)
            return;

        defaultMusicSource.Stop();
        defaultMusicSource.clip = defaultMusic;
        defaultMusicSource.loop = true;
        defaultMusicSource.volume = defaultMusicVolume;
        defaultMusicSource.Play();
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

        float startingDefaultVolume = defaultMusicSource.volume;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = transitionDuration <= 0f ? 1f : elapsed / transitionDuration;

            defaultMusicSource.volume = Mathf.Lerp(startingDefaultVolume, 0f, progress);
            gameplayMusicSource.volume = Mathf.Lerp(0f, gameplayMusicVolume, progress);
            yield return null;
        }

        defaultMusicSource.Stop();
        defaultMusicSource.volume = defaultMusicVolume;
        gameplayMusicSource.volume = gameplayMusicVolume;
        transitionRoutine = null;
    }

    private void PlayRandomGameplayTrack()
    {
        AudioClip nextTrack = GetRandomGameplayTrack();

        if (nextTrack == null)
            return;

        gameplayMusicSource.clip = nextTrack;
        gameplayMusicSource.loop = false;
        gameplayMusicSource.volume = gameplayMusicVolume;
        gameplayMusicSource.Play();
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
