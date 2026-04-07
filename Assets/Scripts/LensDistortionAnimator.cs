using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LensDistortionAnimator : MonoBehaviour
{
    private Volume volume;
    private LensDistortion lensDistortion;

    [Header("X/Y Multiplier Range")]
    public float minValue = 0.2f;
    public float maxValue = 1.0f;

    [Header("Animation Speed")]
    public float speed = 2f;

    void Start()
    {
        volume = GetComponent<Volume>();
        if (volume == null)
        {
            Debug.LogError("No Volume component found on this GameObject!");
            return;
        }

        // Try get Lens Distortion
        volume.profile.TryGet(out lensDistortion);
        if (lensDistortion == null)
        {
            Debug.LogError("No Lens Distortion override found in the Volume profile!");
        }
    }

    void Update()
    {
        if (lensDistortion == null) return;

        // Smooth oscillation between minValue and maxValue
        float t = (Mathf.Sin(Time.time * speed) + 1f) / 2f;
        float value = Mathf.Lerp(minValue, maxValue, t);

        lensDistortion.xMultiplier.value = value;
        lensDistortion.yMultiplier.value = value;
    }
}
