using QFSW.MOP2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ItemExplosion : MonoBehaviour
{
    [SerializeField] private float radius = 5f;
    [SerializeField] private LayerMask enemyLayer;

    [SerializeField] public float damage;


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

    public ObjectPool pool;
    public bool RotateExternally;

    private void OnEnable()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        DealDamage();
        CameraShake.Instance.ShakeWithValue(1f,0.3f);
        ImpactFrameEffect.Instance.GoBlack();
        LeanTween.delayedCall(0.2f, () => { ImpactFrameEffect.Instance.GoColor(); });
    }
    private Transform player;
    // Update is called once per frame
    void Update()
    {
        //Get direction to player(ignore Y so it only rotates horizontally)
            Vector3 direction = player.transform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f) // Avoid errors if player is at same position
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            if (!RotateExternally)
                transform.rotation = targetRotation;
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        
    }
    public void DealDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, enemyLayer);
        foreach (var hit in hits)
        {
            DoExplosionDamge(hit);
            DoKnockback(hit.gameObject);
           
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
        StartCoroutine(disableExplosion());
    }
    IEnumerator disableExplosion()
    {

        yield return new WaitForSeconds(2f);
        pool.Release(gameObject);
    }

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
}
