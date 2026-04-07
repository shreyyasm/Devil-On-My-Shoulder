using QFSW.MOP2;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PlayerUseItem : MonoBehaviour
{
    public enum ItemType
    {
        Grenade,
        Molotov,
        Dynamite,
        Gatolaid,
        Knife,
        Flashbang,
        Smoke
    }
    public enum SystemType
    {
        Default,
        ExplosionOnly,
        KnockbackOnly
    }
    [SerializeField] private ItemType itemType;

    public ObjectPool pool;
    [Header("Grenade")]
    [SerializeField] private float fuseTime = 2f;
    [SerializeField] private float radius = 5f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private ParticleSystem explosionPrefabParticle;

    [Header("Molotov Cocktail")]
    public ObjectPool Fireprefab;


    [Header("Gatolaid")]
    public float effectTime = 5f;
    [SerializeField] public float movementSpeedModifer;
    private PlayerMovement playerMovement;

    [Header("Knife Settings")]
    [SerializeField] private GameObject knifePrefab;
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float knifeSpeed = 25f;
    [SerializeField] private float knifeDetectionRadius = 15f;
    [SerializeField] private float knifeFOV = 120f;


    [Header("System Type")]
    [SerializeField] private SystemType systemType;

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

   

    public GameObject mesh;

    private void OnEnable()
    {
        if(itemType == ItemType.Grenade)
            StartCoroutine(FuseRoutine());

        if (itemType == ItemType.Gatolaid)
        {
            playerMovement = FindObjectOfType<PlayerMovement>();
            StartCoroutine(UseGatolaid());
        }

        if (itemType == ItemType.Knife)
        {
            ThrowKnife();
            gameObject.SetActive(false);
        }

    }

    IEnumerator FuseRoutine()
    {
        yield return new WaitForSeconds(fuseTime);
        Detonate();
    }

    void Detonate()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, enemyLayer);

        foreach (var hit in hits)
        {
            if (systemType == SystemType.Default)
            {
                DoExplosionDamge(hit);
                DoKnockback(hit.gameObject);
            }
            else if (systemType == SystemType.ExplosionOnly)
            {
                DoExplosionDamge(hit);
            }
            else if (systemType == SystemType.KnockbackOnly)
            {
                DoKnockback(hit.gameObject);
            }
        }

        explosionPrefabParticle.Play();
        StartCoroutine(disableExplosion());
        
    }
    IEnumerator disableExplosion()
    {
        yield return new WaitForSeconds(.2f);

        if(mesh != null)
            mesh.SetActive(false);

        yield return new WaitForSeconds(3f);
        gameObject.SetActive(false);
        //pool.Release(gameObject);
    }
    // ================= DAMAGE =================
    public void DoExplosionDamge(Collider other)
    {
        EnemyHealth enemy = other.gameObject.GetComponent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.EnemyHit(damage);
            Debug.Log($"Enemy Hit By Explosion - {damage} Damage");
        }
    }

    // ================= KNOCKBACK =================

    public void DoKnockback(GameObject targetObj)
    {
        Vector3 direction = (targetObj.transform.position - transform.position).normalized;
        StartCoroutine(ExplosionKnockbackCoroutine(targetObj.transform, direction, knockbackForce));
    }

    private IEnumerator ExplosionKnockbackCoroutine(
        Transform target,
        Vector3 direction,
        float force)
    {
        NavMeshAgent agent = target.GetComponent<NavMeshAgent>();
        Rigidbody rb = target.GetComponent<Rigidbody>();

        if (agent != null)
            agent.enabled = false;

        if (rb == null)
            yield break;

        rb.isKinematic = false;
        rb.useGravity = true;

        // 🔥 EXPLOSION FORCE (instead of kick)
        Vector3 launchVelocity = direction * force;
        launchVelocity.y = upwardForce; // use your grenade upward force
        rb.velocity = launchVelocity;

        // -------- WAIT UNTIL GROUNDED --------
        float airTimer = 0f;
        bool grounded = false;

        while (!grounded && airTimer < maxAirTime)
        {
            if (rb == null)
                yield break;

            airTimer += Time.deltaTime;

            if (rb.velocity.y <= 0f)
            {
                Debug.DrawRay(target.position + Vector3.up * 0.3f, Vector3.down * 2.5f, Color.red);
                Vector3 origin = target.position + Vector3.up * 0.3f;

                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 2.5f))
                {
                    grounded = true;
                   
                }
            }

            yield return null;
        }

        Vector3 horizontal = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // fallback if too small
        if (horizontal.magnitude < 1f)
        {
            horizontal = (target.position - transform.position).normalized * knockbackForce * 0.5f;
        }

        float slideTimer = 0f;

        while (slideTimer < slideDuration)
        {
            slideTimer += Time.deltaTime;

            // decay speed
            horizontal = Vector3.Lerp(horizontal, Vector3.zero, slideDamping * Time.deltaTime);

            // 🚀 move manually (THIS is the key fix)
            target.position += horizontal * Time.deltaTime;

            yield return null;
        }
        Debug.Log("navamesh detected");
        // -------- NAVMESH RECOVERY --------
        if (agent != null)
        {
            // Always try to snap to navmesh
            if (NavMesh.SamplePosition(target.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                // fallback: try current position anyway
                agent.Warp(target.position);
            }

            agent.enabled = true;
           rb.isKinematic = true;
            rb.useGravity = false;
            // 🔥 FORCE RESET (VERY IMPORTANT)
            agent.ResetPath();
            agent.velocity = Vector3.zero;

        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.layer == 17 && itemType == ItemType.Molotov)
        {
            Fireprefab.GetObject(transform.position, Quaternion.identity);
      
            gameObject.SetActive(false);
        }
        if (collision.gameObject.layer == 17 && itemType == ItemType.Dynamite)
        {
            Detonate();
        }
    }
    public IEnumerator UseGatolaid()
    {
        playerMovement.itemUseMovementSpeed = movementSpeedModifer;

        yield return new WaitForSeconds(effectTime);

        playerMovement.itemUseMovementSpeed = 1;
        gameObject.SetActive(false);
    }


    Transform FindClosestEnemyInFOV()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, knifeDetectionRadius, enemyLayer);

        Transform bestTarget = null;
        float closestDist = Mathf.Infinity;

        foreach (var hit in hits)
        {
            Vector3 dir = (hit.transform.position - transform.position).normalized;

            float angle = Vector3.Angle(transform.forward, dir);

            if (angle <= knifeFOV * 0.5f)
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    bestTarget = hit.transform;
                }
            }
        }

        return bestTarget;
    }

    void ThrowKnife()
    {
        Transform target = FindClosestEnemyInFOV();

        if (target == null)
        {
            Debug.Log("No target found");
            return;
        }

        GameObject knife = Instantiate(knifePrefab, throwPoint.position, Quaternion.identity);

        KnifeProjectile proj = knife.GetComponent<KnifeProjectile>();
        proj.Init(target, knifeSpeed);
    }
}