using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using static Projectile;

public class ExplosionSystem : MonoBehaviour
{
    public enum SystemType
    {
       Default,
       ExplosionOnly,
       KnockbackOnly

    }
    [Header("Special Bullets")]
    [SerializeField] public SystemType systemType;

    [Header("Explosion Damage")]
    [SerializeField] private float damage = 40f;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float upwardForce = 4f;
    [SerializeField] private float gravity = 30f;
    [SerializeField] private float maxAirTime = 1.2f;

    [Header("Ground Slide")]
    [SerializeField] private float slideDuration = 0.4f;
    [SerializeField] private float slideDamping = 8f;

    [Header("Ground Detection")]
    [SerializeField] private float groundCheckDistance = 2.5f;
    [SerializeField] private float groundCheckRadius = 0.35f;

    [Header("Lifetime")]
    [SerializeField] private float lifeTime = 2f;

    public GameObject explosionPrefab;


    private void Start()
    {
        if (systemType == SystemType.Default)
        {
            explosionPrefab.SetActive(true);
        }

        if (systemType == SystemType.ExplosionOnly)
        {
            explosionPrefab.SetActive(true);
        }

        Destroy(gameObject, lifeTime);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != 16)
            return;

        if (systemType == SystemType.Default)
        {
            DoExplosionDamge(other);
            DoKnockback(other.gameObject);
        }

        if (systemType == SystemType.ExplosionOnly)
        {
            DoExplosionDamge(other);
        }

        if (systemType == SystemType.KnockbackOnly)
        {
            DoKnockback(other.gameObject);
        }

    }
    public void DoExplosionDamge(Collider other)
    {
        Enemy enemy = other.gameObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.EnemyHit(damage);
            Debug.Log($"Enemy Hit By Explosion - {damage} Damage");
        }

    }
    public void DoKnockback(GameObject other)
    {
        StartCoroutine(KnockbackCoroutine(other.transform));
    }
    private IEnumerator KnockbackCoroutine(Transform target)
    {
        NavMeshAgent agent = target.GetComponent<NavMeshAgent>();
        if (agent != null)
            agent.enabled = false;

        Collider col = target.GetComponent<Collider>();
        float heightOffset = col != null ? col.bounds.extents.y : 0.5f;

        Vector3 direction = (target.position - transform.position).normalized;

        Vector3 velocity = direction * knockbackForce;
        velocity.y = upwardForce;

        float airTimer = 0f;
        bool grounded = false;

        // ---------------- AIR PHASE ----------------
        while (!grounded && airTimer < maxAirTime)
        {
            airTimer += Time.deltaTime;

            velocity.y -= gravity * Time.deltaTime;
            target.position += velocity * Time.deltaTime;

            if (velocity.y <= 0f)
            {
                Vector3 castOrigin = target.position + Vector3.up * 0.2f;

                if (Physics.SphereCast(
                    castOrigin,
                    groundCheckRadius,
                    Vector3.down,
                    out RaycastHit hit,
                    groundCheckDistance,
                    ~0,
                    QueryTriggerInteraction.Ignore))
                {
                    grounded = true;
                    target.position = hit.point + Vector3.up * heightOffset;
                }
            }

            yield return null;
        }

        // ---------------- SLIDE PHASE ----------------
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
        float slideTimer = 0f;

        while (slideTimer < slideDuration && horizontalVelocity.magnitude > 0.05f)
        {
            slideTimer += Time.deltaTime;

            horizontalVelocity = Vector3.Lerp(
                horizontalVelocity,
                Vector3.zero,
                slideDamping * Time.deltaTime
            );

            target.position += horizontalVelocity * Time.deltaTime;
            yield return null;
        }

        // ---------------- NAVMESH RECOVERY ----------------
        if (agent != null)
        {
            if (NavMesh.SamplePosition(target.position, out NavMeshHit navHit, 3f, NavMesh.AllAreas))
            {
                agent.Warp(navHit.position);
            }
            else
            {
                agent.Warp(target.position);
            }

            agent.enabled = true;
        }
    }
}
