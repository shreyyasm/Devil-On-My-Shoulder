using UnityEngine;

public class DestructibleExplosionBarrel : MonoBehaviour
{
    [Header("Explosion")]
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private Transform explosionSpawnPoint;

    [Header("Debris")]
    [SerializeField] private GameObject[] debrisPrefabs;
    [SerializeField] private int debrisCount = 8;
    [SerializeField] private float debrisForce = 8f;
    [SerializeField] private float debrisUpwardForce = 3f;
    [SerializeField] private float debrisTorque = 10f;

    [Header("Debris Lifetime")]
    [SerializeField] private float debrisLifetime = 5f;

    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 4f;
    [SerializeField] private float explosionForce = 10f;

    private bool exploded = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (exploded)
            return;

        if (!collision.gameObject.CompareTag("Bullet"))
            return;

        Explode();
    }

    private void Explode()
    {
        exploded = true;

        // Use the assigned spawn point, otherwise use the barrel position.
        Vector3 explosionPosition = explosionSpawnPoint != null
            ? explosionSpawnPoint.position
            : transform.position;

        // Spawn explosion VFX
        if (explosionEffect != null)
        {
            Instantiate(
                explosionEffect,
                explosionPosition,
                Quaternion.identity
            );
        }

        // Spawn physical debris
        SpawnDebris(explosionPosition);

        // Disable barrel
        gameObject.SetActive(false);
    }

    private void SpawnDebris(Vector3 explosionPosition)
    {
        if (debrisPrefabs == null || debrisPrefabs.Length == 0)
            return;

        for (int i = 0; i < debrisCount; i++)
        {
            GameObject debrisPrefab =
                debrisPrefabs[Random.Range(0, debrisPrefabs.Length)];

            Vector3 randomDirection = Random.onUnitSphere;

            // Make sure some force goes upward.
            randomDirection.y = Mathf.Abs(randomDirection.y);

            randomDirection.Normalize();

            GameObject debris = Instantiate(
                debrisPrefab,
                explosionPosition,
                Random.rotation
            );

            Rigidbody rb = debris.GetComponent<Rigidbody>();

            if (rb != null)
            {
                Vector3 force =
                    randomDirection * debrisForce;

                force += Vector3.up * debrisUpwardForce;

                rb.AddForce(
                    force,
                    ForceMode.Impulse
                );

                rb.AddTorque(
                    Random.insideUnitSphere * debrisTorque,
                    ForceMode.Impulse
                );
            }

            Destroy(debris, debrisLifetime);
        }
    }
}