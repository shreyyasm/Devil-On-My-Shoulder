using UnityEngine;

public class StickyBomb : MonoBehaviour
{
    [Header("Explosion")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float detonateTime = 5f;

    [Header("Blinking Light")]
    [SerializeField] private Light bombLight;
    [SerializeField] private float minBlinkInterval = 0.05f; // fastest pulse
    [SerializeField] private float maxBlinkInterval = 0.6f;  // slowest pulse
    [SerializeField] private float maxLightIntensity = 1f;

    private float timer;
    private bool isStuck;

    private float blinkTimer;

    private void Start()
    {
        timer = detonateTime;

        if (bombLight != null)
        {
            bombLight.intensity = 0f;
            bombLight.enabled = true;
        }
    }

    private void Update()
    {
        if (!isStuck)
            return;

        // Countdown
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            Detonate();
            return;
        }

        HandleBlinking();
    }

    private void HandleBlinking()
    {
        if (bombLight == null)
            return;

        // t = 1 at start, 0 near explosion
        float t = Mathf.Clamp01(timer / detonateTime);

        // Blink interval decreases as time runs out
        float blinkInterval = Mathf.Lerp(
            minBlinkInterval,
            maxBlinkInterval,
            t
        );

        blinkTimer += Time.deltaTime;

        // 0 → 1 → 0
        float lerpT = Mathf.PingPong(blinkTimer / blinkInterval, 1f);
        bombLight.intensity = Mathf.Lerp(0f, maxLightIntensity, lerpT);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isStuck)
            return;

        StickToSurface(collision);
    }

    private void StickToSurface(Collision collision)
    {
        isStuck = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
        }

        transform.SetParent(collision.transform);
    }

    // 🔑 CALLED BY StickyVicky TOO
    public void ForceStick()
    {
        isStuck = true;
    }

    private void Detonate()
    {
        if (explosionPrefab != null)
        {
            Instantiate(
                explosionPrefab,
                transform.position,
                Quaternion.identity
            );
        }

        Destroy(gameObject);
    }
}
