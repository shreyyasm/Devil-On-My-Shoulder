using UnityEngine;
using UnityEngine.Audio;
using Kamgam.SettingsGenerator;

[CreateAssetMenu(fileName = "DOMS Audio Mixer Volume", menuName = "SettingsGenerator/Connection/DOMS Audio Mixer Volume")]
public class DOMSAudioMixerVolumeConnectionSO : FloatConnectionSO
{
    public AudioMixer mixer;
    public string exposedParameterName;
    [Min(0f)] public float minimumDecibels = -80f;
    [Min(1f)] public float sliderMaximum = 100f;

    private DOMSAudioMixerVolumeConnection connection;

    public override IConnection<float> GetConnection()
    {
        if (connection == null)
            connection = new DOMSAudioMixerVolumeConnection(mixer, exposedParameterName, minimumDecibels, sliderMaximum);
        return connection;
    }

    public override void DestroyConnection()
    {
        connection?.Destroy();
        connection = null;
    }
}

public sealed class DOMSAudioMixerVolumeConnection : Connection<float>
{
    private readonly AudioMixer mixer;
    private readonly string parameter;
    private readonly float minimumDecibels;
    private readonly float sliderMaximum;

    public DOMSAudioMixerVolumeConnection(AudioMixer mixer, string parameter, float minimumDecibels, float sliderMaximum)
    {
        this.mixer = mixer;
        this.parameter = parameter;
        this.minimumDecibels = minimumDecibels;
        this.sliderMaximum = sliderMaximum;
    }

    public override float Get()
    {
        if (mixer == null || !mixer.GetFloat(parameter, out float decibels))
            return sliderMaximum;
        return Mathf.InverseLerp(minimumDecibels, 0f, decibels) * sliderMaximum;
    }

    public override void Set(float value)
    {
        if (mixer == null || string.IsNullOrEmpty(parameter))
            return;
        float normalized = Mathf.Clamp01(value / sliderMaximum);
        mixer.SetFloat(parameter, Mathf.Lerp(minimumDecibels, 0f, normalized));
        NotifyListenersIfChanged(value);
    }
}
