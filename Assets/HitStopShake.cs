using System.Collections;
using UnityEngine;

public class HitstopShake : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private float shakeAmount = 5f;     // Units to move left/right
    [SerializeField] private float shakeSpeed = 40f;     // Oscillation speed
    [SerializeField] private float shakeDuration = 0.15f;

    private Vector3 originalLocalPos;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        originalLocalPos = transform.localPosition;
    }

    /// <summary>
    /// Call this to trigger a hitstop-style shake (time-scale independent)
    /// </summary>
    public void DoShake()
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            // Use unscaled time ONLY
            elapsed += Time.unscaledDeltaTime;

            // Normalized 0 → 1
            float t = elapsed / shakeDuration;

            // Oscillation (left → right → left)
            float offset = Mathf.Sin(t * shakeSpeed * Mathf.PI * 2f) * shakeAmount;

            transform.localPosition =
                originalLocalPos + Vector3.right * offset;

            // Wait one frame, unaffected by timescale
            yield return null;
        }

        // Hard snap back (important)
        transform.localPosition = originalLocalPos;
        shakeRoutine = null;
    }
}
