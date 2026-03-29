using QFSW.MOP2;
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
    public ObjectPool pool;
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

    [SerializeField] private float detonateDelay = 1.5f;

    private float timer;
    public bool isArmed = false;

    public GameObject explosionPrefab;
    public ParticleSystem explosionParticle;

    private void OnEnable()
    {

        timer = 0f;
        isArmed = false;

        if (systemType == SystemType.Default)
        {
            explosionPrefab.SetActive(true);
        }

        if (systemType == SystemType.ExplosionOnly)
        {
            explosionPrefab.SetActive(true);
        }


        StartCoroutine(DisableExplosion());
    }
    private void Update()
    {
        if (isArmed) return;

        timer += Time.deltaTime;

        if (timer >= detonateDelay)
        {
            isArmed = true;
            explosionParticle.Play();
            // Optional debug
            // Debug.Log("Grenade Armed!");
        }
    }

    IEnumerator DisableExplosion()
    {
        yield return new WaitForSeconds(lifeTime);
        pool.Release(gameObject);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (!isArmed) return;

        if (collision.gameObject.layer != 16)
            return;

        HandleExplosion(collision.gameObject, collision.collider);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!isArmed) return;

        if (other.gameObject.layer != 16)
            return;

        HandleExplosion(other.gameObject, other);
    }

    private void HandleExplosion(GameObject target, Collider col)
    {
        if (systemType == SystemType.Default)
        {
            DoExplosionDamge(col);
            DoKnockback(target);
        }
        else if (systemType == SystemType.ExplosionOnly)
        {
            DoExplosionDamge(col);
        }
        else if (systemType == SystemType.KnockbackOnly)
        {
            DoKnockback(target);
        }
    }
    public void DoExplosionDamge(Collider other)
    {
        EnemyHealth enemy = other.gameObject.GetComponent<EnemyHealth>();
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
