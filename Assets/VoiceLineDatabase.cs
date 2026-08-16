using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "VoiceLineDatabase", menuName = "Voice/Voice Line Database")]
public class VoiceLineDatabase : ScriptableObject
{
    public List<VoiceLineGroup> groups = new List<VoiceLineGroup>();

    public VoiceLineGroup GetGroup(string groupName)
    {
        return groups.Find(group => group.groupName == groupName);
    }
}

[System.Serializable]
public class VoiceLineGroup
{
    public string groupName;

    public List<VoiceLineEntry> voiceLines = new List<VoiceLineEntry>();
}

[System.Serializable]
public class VoiceLineEntry
{
    public string lineName;

    [Header("Voice")]
    public AudioClip audioClip;

    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("Animation")]
    public AnimationClip animationClip;

    [Header("Optional")]
    [Tooltip("If enabled, this line cannot be played while another voice line is playing.")]
    public bool preventInterrupt = true;
}