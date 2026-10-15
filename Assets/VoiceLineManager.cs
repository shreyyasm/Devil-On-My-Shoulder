using UnityEngine;
using TMPro;

public class VoiceLineManager : MonoBehaviour
{
    public static VoiceLineManager instance;

    [Header("Database")]
    [SerializeField] private VoiceLineDatabase database;

    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] public Animator animator;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Voice Playback")]
    [Tooltip("Extra gain for demon dialogue. The Demon Voice Volume slider controls the final voice volume.")]
    [Range(0f, 2f)]
    [SerializeField] private float voiceVolumeMultiplier = 4f;

    [Header("Music Ducking")]
    [SerializeField] private AudioSource musicAudioSource;

    [Tooltip("Music volume while dialogue is playing.")]
    [Range(0f, 1f)]
    [SerializeField] private float musicDuckVolume = 0.25f;

    [Tooltip("How quickly the music fades down/up.")]
    [SerializeField] private float musicFadeSpeed = 4f;

    [Header("Settings")]
    [SerializeField] private bool preventVoiceInterrupts = true;

    private VoiceLineEntry currentLine;
    private VoiceLineEntry lastPlayedLine;

    private float lastPlayTime = -Mathf.Infinity;

    private float normalMusicVolume;
    private float targetMusicVolume;

    private void Awake()
    {
        instance = this;

        // Subtitle starts disabled
        if (subtitleText != null)
            subtitleText.gameObject.SetActive(false);

        if (musicAudioSource != null)
        {
            normalMusicVolume = musicAudioSource.volume;
            targetMusicVolume = normalMusicVolume;
        }

        // Dialogue is narration, so it should not become quiet when the demon head
        // is off screen or farther away from the listener.
        if (audioSource != null)
            audioSource.spatialBlend = 0f;

        PlayRandom("Game Start", 20f);
    }

    private void Update()
    {
        UpdateMusicVolume();

        // Voice dialogue has finished
        if (audioSource != null && !audioSource.isPlaying)
        {
            // Only restore/disable if a dialogue was actually playing
            if (subtitleText != null && subtitleText.gameObject.activeSelf)
            {
                DialogueFinished();
            }
        }
    }

    // ---------------------------------------------------------
    // PLAY RANDOM LINE
    // ---------------------------------------------------------

    public void PlayRandom(string groupName, float chanceToPlay = 100f)
    {
        chanceToPlay = Mathf.Clamp(chanceToPlay, 0f, 100f);

        // Chance check
        if (Random.Range(0f, 100f) >= chanceToPlay)
            return;
        animator.gameObject.SetActive(true);
        animator.SetTrigger("HeadIn");
        if (database == null)
        {
            Debug.LogWarning(
                "VoiceLineManager: Database is missing.",
                this
            );

            return;
        }

        VoiceLineGroup group = database.GetGroup(groupName);

        if (group == null)
        {
            Debug.LogWarning(
                $"VoiceLineManager: Group '{groupName}' was not found.",
                this
            );

            return;
        }

        if (group.voiceLines == null || group.voiceLines.Count == 0)
        {
            Debug.LogWarning(
                $"VoiceLineManager: Group '{groupName}' has no voice lines.",
                this
            );

            return;
        }

        if (preventVoiceInterrupts && IsPlaying())
            return;

        VoiceLineEntry selectedLine;

        // If there's only one line, just use it
        if (group.voiceLines.Count == 1)
        {
            selectedLine = group.voiceLines[0];
        }
        else
        {
            // Prevent same line twice in a row
            do
            {
                selectedLine =
                    group.voiceLines[
                        Random.Range(0, group.voiceLines.Count)
                    ];

            } while (selectedLine == lastPlayedLine);
        }

        PlayLine(selectedLine);

        lastPlayedLine = selectedLine;
    }

    // ---------------------------------------------------------
    // PLAY SPECIFIC LINE
    // ---------------------------------------------------------

    public void PlayLine(VoiceLineEntry line)
    {
        if (line == null)
            return;

        if (line.preventInterrupt && IsPlaying())
            return;

        currentLine = line;

        // -----------------------------------------
        // AUDIO
        // -----------------------------------------

        if (audioSource != null && line.audioClip != null)
        {
            audioSource.PlayOneShot(
                line.audioClip,
                Mathf.Clamp(line.volume * voiceVolumeMultiplier, 0f, 4f)
            );

            DuckMusic();
        }

        // -----------------------------------------
        // SUBTITLE
        // -----------------------------------------

        if (subtitleText != null)
        {
           subtitleText.text = "Demon: " + line.lineName;
            subtitleText.gameObject.SetActive(true);
        }

        // -----------------------------------------
        // ANIMATION
        // -----------------------------------------

        if (animator != null && line.animationClip != null)
        {
            PlayAnimation(line.animationClip);
        }

        lastPlayTime = Time.time;
    }

    // ---------------------------------------------------------
    // PLAY ANIMATION
    // ---------------------------------------------------------

    private void PlayAnimation(AnimationClip clip)
    {
        if (clip == null)
            return;

        animator.Play(
            clip.name,
            0,
            0f
        );
    }

    // ---------------------------------------------------------
    // DIALOGUE FINISHED
    // ---------------------------------------------------------

    private void DialogueFinished()
    {
        // Disable subtitle
        if (subtitleText != null)
        {
            subtitleText.text = "";
            subtitleText.gameObject.SetActive(false);
        }
        animator.SetTrigger("HeadOut");
        LeanTween.delayedCall(0.2f, () => { animator.gameObject.SetActive(false); });
       
        // Restore music
        RestoreMusic();
    }
    public void HeadOut()
    {
        animator.SetTrigger("HeadOut");
    }
    // ---------------------------------------------------------
    // MUSIC DUCKING
    // ---------------------------------------------------------

    private void DuckMusic()
    {
        if (BackgroundMusic.Instance != null)
        {
            BackgroundMusic.Instance.SetDialogueDucking(musicDuckVolume);
            return;
        }

        if (musicAudioSource == null)
            return;

        targetMusicVolume =
            normalMusicVolume * musicDuckVolume;
    }

    private void RestoreMusic()
    {
        if (BackgroundMusic.Instance != null)
        {
            BackgroundMusic.Instance.SetDialogueDucking(1f);
            return;
        }

        targetMusicVolume = normalMusicVolume;
    }

    private void UpdateMusicVolume()
    {
        if (musicAudioSource == null)
            return;

        musicAudioSource.volume = Mathf.MoveTowards(
            musicAudioSource.volume,
            targetMusicVolume,
            musicFadeSpeed * Time.deltaTime
        );
    }

    // ---------------------------------------------------------
    // STOP VOICE
    // ---------------------------------------------------------

    public void StopVoice()
    {
        if (audioSource != null)
            audioSource.Stop();

        DialogueFinished();
    }

    // ---------------------------------------------------------
    // CHECK IF SPEAKING
    // ---------------------------------------------------------

    public bool IsPlaying()
    {
        return audioSource != null &&
               audioSource.isPlaying;
    }

    // ---------------------------------------------------------
    // GET CURRENT LINE
    // ---------------------------------------------------------

    public VoiceLineEntry GetCurrentLine()
    {
        return currentLine;
    }

    // ---------------------------------------------------------
    // GET CURRENT AUDIO
    // ---------------------------------------------------------

    public AudioClip GetCurrentAudioClip()
    {
        if (currentLine == null)
            return null;

        return currentLine.audioClip;
    }
}
