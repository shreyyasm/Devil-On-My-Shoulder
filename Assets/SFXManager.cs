using UnityEngine;
using System.Collections.Generic;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    [System.Serializable]
    public class SFXEntry
    {
        public string name; // e.g., "Walk", "Jump"
        public List<AudioClip> clips = new(); // supports multiple variations
    }

    [System.Serializable]
    public class SFXCategory
    {
        public string categoryName; // e.g., "Player", "Enemy"
        public List<SFXEntry> sounds = new();
    }

    public AudioSource audioSource;
    public List<SFXCategory> categories = new();

    private Dictionary<string, List<AudioClip>> clipDict;

    private void Awake()
    {
        if (Instance != this)
            Instance = this;

        clipDict = new Dictionary<string, List<AudioClip>>();

        foreach (var category in categories)
        {
            foreach (var entry in category.sounds)
            {
                string key = $"{category.categoryName}/{entry.name}";
                if (!clipDict.ContainsKey(key))
                    clipDict[key] = new List<AudioClip>();

                clipDict[key].AddRange(entry.clips);
            }
        }
    }

    public void StopSFX()
    {
        audioSource.Stop();
    }
    public void PauseSFX()
    {
        audioSource.Pause();
    }
    public void UnPauseSFX()
    {
        audioSource.UnPause();
    }

    public void PlaySFX()
    {
        audioSource.Play();
    }

    /// <summary>
    /// Play sound by full name, e.g. "Player/Jump"
    /// </summary>
    public void PlaySFX(string fullName, float volume = 1f, float steroPlane = 0f)
    {

        if (clipDict == null || clipDict.Count == 0)
        {
            Debug.LogError("SFXManager: clipDict is not initialized.");
            return;
        }

        if (!clipDict.TryGetValue(fullName, out var clips) || clips.Count == 0)
        {
            Debug.LogWarning($"SFXManager: No clips found for key '{fullName}'.");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogError("SFXManager: AudioSource is not assigned.");
            return;
        }

        audioSource.panStereo = steroPlane;
        AudioClip clip = clips[Random.Range(0, clips.Count)];
        if (clip != null)
            audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    public void PlayNPC_SFX(AudioSource audioSource, string fullName, float volume = 1f, float steroPlane = 0f)
    {

        if (clipDict == null || clipDict.Count == 0)
        {
            Debug.LogError("SFXManager: clipDict is not initialized.");
            return;
        }

        if (!clipDict.TryGetValue(fullName, out var clips) || clips.Count == 0)
        {
            Debug.LogWarning($"SFXManager: No clips found for key '{fullName}'.");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogError("SFXManager: AudioSource is not assigned.");
            return;
        }

        audioSource.panStereo = steroPlane;
        AudioClip clip = clips[Random.Range(0, clips.Count)];
        if (clip != null)
            audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }
}
