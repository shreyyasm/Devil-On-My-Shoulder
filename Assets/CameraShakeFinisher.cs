using UnityEngine;
using System.Collections;

public class CameraShakeFinisher : MonoBehaviour
{
    public static CameraShakeFinisher Instance;

    private Vector3 originalLocalPos;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        Instance = this;
        originalLocalPos = transform.localPosition;
    }

    // 🔥 MAIN METHOD
    public void Shake(float intensity, float duration, float frequency = 30f)
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeRoutine(intensity, duration, frequency));
    }

    private IEnumerator ShakeRoutine(float intensity, float duration, float frequency)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            // random offset
            Vector3 offset = Random.insideUnitSphere * intensity;

            transform.localPosition = originalLocalPos + offset;

            // control how fast it jitters
            yield return new WaitForSeconds(1f / frequency);
        }

        // reset position
        transform.localPosition = originalLocalPos;
        shakeRoutine = null;
    }
}