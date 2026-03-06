using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    public Transform camTransform;

    public float duration;
    public float shakeMagnitudePower;
    float shakeDuration = 0f;
    float shakeMagnitude = 0.2f;
    float dampingSpeed = 1.5f;
    float noiseSeed;

    Vector3 originalPos;

    void Awake()
    {
        if (Instance == null)
            Instance = this;

        if (camTransform == null)
            camTransform = transform;
    }

    void OnEnable()
    {
        originalPos = camTransform.localPosition;
        noiseSeed = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (shakeDuration > 0)
        {
            float x = (Mathf.PerlinNoise(noiseSeed, Time.time * 20f) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(Time.time * 20f, noiseSeed) - 0.5f) * 2f;

            Vector3 offset = new Vector3(x, y, 0) * shakeMagnitude;

            camTransform.localPosition = originalPos + offset;

            shakeDuration -= Time.deltaTime * dampingSpeed;
        }
        else
        {
            shakeDuration = 0;
            camTransform.localPosition = originalPos;
        }
    }

    public void Shake()
    {
        shakeDuration = duration;
        shakeMagnitude = shakeMagnitudePower;
    }
}