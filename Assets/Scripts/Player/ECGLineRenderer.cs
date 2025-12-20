using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ECGLineRenderer : MonoBehaviour
{
    public PlayerHealth playerHealth;

    [Header("ECG Shape")]
    public int resolution = 300;
    public float xSpacing = 0.04f;
    public float baselineY = 0f;

    [Header("Spike Height")]
    public float normalSpike = 1.2f;
    public float criticalSpike = 2.0f;

    private LineRenderer line;
    private float[] samples;

    private float beatTimer;

    // ECG spike pattern (simple but visible)
    private readonly float[] spikePattern =
    {
        0f,
        0.4f,
        1.0f,
        -0.6f,
        0f
    };

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = resolution;

        samples = new float[resolution];

        for (int i = 0; i < resolution; i++)
            samples[i] = baselineY;
    }

    void Update()
    {
        float bpm = playerHealth.currentBPM;

        // FLATLINE
        if (bpm <= 0f)
        {
            for (int i = 0; i < resolution; i++)
                samples[i] = baselineY;

            Draw();
            return;
        }

        float beatInterval = 60f / bpm;
        beatTimer += Time.deltaTime;

        // SHIFT LEFT (scroll)
        for (int i = 0; i < resolution - 1; i++)
            samples[i] = samples[i + 1];

        // DEFAULT NEW POINT
        samples[resolution - 1] = baselineY;

        // HEARTBEAT
        if (beatTimer >= beatInterval)
        {
            beatTimer = 0f;

            float height = playerHealth.IsCritical ? criticalSpike : normalSpike;

            // Inject spike at END of buffer
            for (int i = 0; i < spikePattern.Length; i++)
            {
                int index = resolution - 1 - i;
                if (index < 0) break;

                samples[index] = baselineY + spikePattern[i] * height;
            }
        }

        Draw();
    }

    void Draw()
    {
        for (int i = 0; i < resolution; i++)
        {
            line.SetPosition(i, new Vector3(i * xSpacing, samples[i], 0f));
        }
    }
}
