using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyManager : MonoBehaviour
{

    // ================================
    // SCRIPTABLE OBJECT
    // ================================

    [Header("Stats Reference")]
    public EnemyStats enemyStats;


    // ================================
    // ENUMS
    // ================================

    
    public enum EnemyType { Melee, Shoot, Touch, Thrower, Boss }
    public enum CoverType { No, Yes }
    public enum DeathType { Simple, Explosion, ToxicSplash }
    public enum SurfaceType { Ground, Air }

    public enum State { Idle, Wander, Chase, Attack }

    // ================================
    // CONFIGURATION
    // ================================
    // Instead use:
    [Header("Types Reference")]
    public EnemyStats.EnemyType enemyType;
    public EnemyStats.CoverType coverType;
    public EnemyStats.DeathType deathType;
    public EnemyStats.SurfaceType surfaceType;

   
    // ================================
    // RUNTIME COMBAT VALUES
    // ================================

    [Header("Runtime Combat Values")]
    public float damage;
    public float speed;

    // ================================
    // MELEE ATTACK SETTINGS
    // ================================

    [Header("Melee Attack Settings")]
    public float attackRange;
    public float attackIntervalMin;
    public float attackIntervalMax;
    public float recoverTime;
    public bool isAttacking;
    // ================================
    // DASH SETTINGS
    // ================================

    [Header("Dash Settings")]
    public bool canDash;
    public float dashForce;
    public float dashLoadTime;
    public float dashDuration = 0.3f;
    public float dashStopDistance = 1.5f;

    [Header("Grab Settings")]
    public bool canGrab;
    public float grabDamageInterval = 0.5f;
    public float grabOffset = 1.0f;
    public float grabHeightOffset = 1.5f;

    public bool isGrabbing;
    private float grabDamageTimer;

    private Collider enemyCollider;
    private Collider playerCollider;
    private FixedJoint grabJoint;
    private Rigidbody playerRb;
    private static EnemyManager currentGrabber = null;
    // ================================
    // SHOOT ENEMY SETTINGS
    // ================================

    [Header("Shoot Enemy Settings")]

    public Transform firePoint;
    public GameObject projectilePrefab;
    public float shootSpeed;
    public float shootInterval;

    public bool canShootMove;
    public float changePositionTime;   // Time before reposition
    public float moveRadius;           // Random move radius


    [Header("Cover Settings")]

    public float coverTime = 2f;
    public float peekDuration = 2f;
    public float peekRadius = 2f;      // random exposed point radius
    public float coverSearchRadius = 25f;

    public float visionHeight = 1.8f;   // eye height
    public int maxPeekAttempts = 5;     // prevent infinite searching

    private float coverTimer;
    private float peekTimer;
    private int currentPeekAttempts;

    private Transform currentCover;
    private Vector3 coverPos;
    private Vector3 peekPos;

    private CoverState coverState;

    private enum CoverState
    {
        MovingToCover,
        Waiting,
        MovingToPeek,
        Peeking
    }
    // ================================
    // PRIVATE VARIABLES
    // ================================

    private NavMeshAgent agent;
    private Transform player;
    private State currentState;

    private float nextAttackTime;
    private bool isBusy;


    private float shootTimer;
    private float positionTimer;
    private Vector3 shootMoveTarget;

    // ================================
    // UNITY METHODS
    // ================================
    public bool RotateExternally;
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        enemyCollider = GetComponent<Collider>();
        playerCollider = player.GetComponent<Collider>();
    }

    private void Start()
    {
        if (enemyStats == null)
            return;

        // =====================
        // TYPE SETTINGS (FROM SO)
        // =====================
        enemyType = enemyStats.enemyType;
        coverType = enemyStats.coverType;
        deathType = enemyStats.deathType;
        surfaceType = enemyStats.surfaceType;

        // =====================
        // BASE STATS
        // =====================
        damage = enemyStats.damage;
        speed = enemyStats.speed;

        if (agent != null)
        {
            agent.speed = speed;
            agent.stoppingDistance = enemyStats.StoppingDistance;
        }

        // =====================
        // MELEE STATS
        // =====================
        attackRange = enemyStats.AttackRange;
        attackIntervalMin = enemyStats.AttackIntervalMin;
        attackIntervalMax = enemyStats.AttackIntervalMax;
        recoverTime = enemyStats.RecoverTime;

        // =====================
        // DASH STATS
        // =====================
        dashForce = enemyStats.DashForce;
        dashLoadTime = enemyStats.DashLoadTime;
        dashDuration = enemyStats.DashDurationTime;
        dashStopDistance = enemyStats.DashStopTime;
        canDash = enemyStats.canDash;

        // =====================
        // Grabber STATS
        // =====================

        canGrab = enemyStats.canGrab;
        grabDamageInterval = enemyStats.grabDamageInterval;
        grabOffset = enemyStats.grabOffset;
        grabHeightOffset = enemyStats.grabHeightOffset;
        // =====================
        // SHOOT STATS
        // =====================
        shootSpeed = enemyStats.ShootSpeed;
        shootInterval = enemyStats.ShootInterval;
        changePositionTime = enemyStats.ChangePositionTime;
        moveRadius = enemyStats.MoveRadius;
        canShootMove = enemyStats.canShootMove;


        // =====================
        // Cover STATS
        // =====================

        coverTime = enemyStats.CoverTime;
        peekDuration = enemyStats.PeekDuration;
        peekRadius = enemyStats.PeekRadius;
        coverSearchRadius = enemyStats.CoverSearchRadius;
        visionHeight = enemyStats.VisionHeight;

        // =====================
        // STATE INITIALIZATION
        // =====================

        if (enemyType == EnemyStats.EnemyType.Melee)
        {
            currentState = State.Chase;
            SetNextAttackTime();
        }
      
        if (enemyType == EnemyStats.EnemyType.Shoot)
        {
            shootTimer = shootInterval;
            positionTimer = changePositionTime;

            peekTimer = peekDuration;
        }
    }

    private void Update()
    {
        // Get direction to player (ignore Y so it only rotates horizontally)
        Vector3 direction = player.transform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f) // Avoid errors if player is at same position
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            if (!RotateExternally)
                transform.rotation = targetRotation;
        }

    
        if (player == null || isBusy) return;

        float distance = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Chase:
                HandleChase();
                break;
        }
        if (enemyType == EnemyStats.EnemyType.Shoot)
        {
            if (coverType == EnemyStats.CoverType.Yes)
            {
                HandleCoverShooter();
            }
            else if (canShootMove)
            {
                HandleMovingShooter();
            }
            else
            {
                HandleStaticShooter();
            }
        }
    }

    // =======================================================================
    // Melee Enemy Methods
    // =======================================================================

    void HandleChase()
    {
        float currentDistance = Vector3.Distance(transform.position, player.position);

        if (enemyType == EnemyStats.EnemyType.Melee && canDash)
            agent.stoppingDistance = attackRange;

        if (currentDistance > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            agent.isStopped = true;

            if (!isBusy && Time.time >= nextAttackTime)
            {
                // =============================
                // GRAB LOGIC
                // =============================

                if (canGrab && !isGrabbing)
                {
                    // If no one is grabbing → grab
                    if (currentGrabber == null)
                    {
                        currentGrabber = this;
                        StartCoroutine(GrabRoutine());
                    }
                    else
                    {
                        // Someone else is grabbing → fallback to melee attack
                        StartCoroutine(AttackRoutine());
                    }
                }
                else
                {
                    // Normal melee
                    StartCoroutine(AttackRoutine());
                }
            }
        }
    }
    // ================================
    // ATTACK ROUTINE
    // ================================

    IEnumerator AttackRoutine()
    {
      
        isBusy = true;
        currentState = State.Attack;
        isAttacking = true;
        agent.isStopped = true;

        // ================= DASH PHASE =================
        // ================= DASH PHASE =================
        if (canDash)
        {
            yield return new WaitForSeconds(dashLoadTime);

            Rigidbody rb = GetComponent<Rigidbody>();

            // Disable NavMesh movement control
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.isStopped = true;

            // Enable physics temporarily
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // PURE HORIZONTAL DIRECTION
            Vector3 dashDir = player.position - transform.position;
            dashDir.y = 0f;                 // 🚀 REMOVE vertical influence
            dashDir = dashDir.normalized;   // normalize AFTER removing Y

            rb.AddForce(dashDir * dashForce, ForceMode.Impulse);

            yield return new WaitForSeconds(dashDuration);

            // Stop movement
            rb.velocity = Vector3.zero;

            // Restore controlled state
            rb.isKinematic = true;

            agent.Warp(transform.position);
            agent.updatePosition = true;
            agent.updateRotation = true;
        }

        // ================= ATTACK =================
        PerformAttack();

        // ================= RECOVER =================
        currentState = State.Idle;
        yield return new WaitForSeconds(recoverTime);

        SetNextAttackTime();
        currentState = State.Chase;
        isBusy = false;
        isAttacking = false;
    }

    // ================================
    // ATTACK EXECUTION
    // ================================
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.transform.CompareTag("Player")) return;
        if (enemyType != EnemyStats.EnemyType.Melee) return;
        if (!isBusy) return;

        // Immediate first hit
        Debug.Log("Melee Damage: " + damage);

        // collision.transform.GetComponent<PlayerHealth>()?.TakeDamage(damage);

        // Start interval cooldown
        nextAttackTime = Time.time + Random.Range(attackIntervalMin, attackIntervalMax);
    }
    private void OnCollisionStay(Collision collision)
    {
        if (!collision.transform.CompareTag("Player")) return;
        if (enemyType != EnemyStats.EnemyType.Melee) return;
        if (!isBusy) return;

        if (Time.time < nextAttackTime) return;

        Debug.Log("Melee Damage (Interval): " + damage);

        // collision.transform.GetComponent<PlayerHealth>()?.TakeDamage(damage);

        nextAttackTime = Time.time + Random.Range(attackIntervalMin, attackIntervalMax);
    }
    void PerformAttack()
    {
        
        // Later you can add hit detection here
    }

    // ================================
    // RANDOM ATTACK INTERVAL
    // ================================

    void SetNextAttackTime()
    {
        float randomTime = Random.Range(attackIntervalMin, attackIntervalMax);
        nextAttackTime = Time.time + randomTime;
    }

    IEnumerator GrabRoutine()
    {
        if (isGrabbing) yield break;

        isBusy = true;
        isGrabbing = true;
        Physics.IgnoreCollision(enemyCollider, playerCollider, true);
        // Disable NavMesh
        agent.isStopped = true;
        agent.enabled = false;

        Rigidbody enemyRb = GetComponent<Rigidbody>();

        // Keep rigidbody NON-kinematic but frozen
        enemyRb.isKinematic = false;
        enemyRb.velocity = Vector3.zero;
        enemyRb.angularVelocity = Vector3.zero;

        // Freeze physics movement completely
        enemyRb.constraints =
            RigidbodyConstraints.FreezePosition |
            RigidbodyConstraints.FreezeRotation;

        yield return new WaitForSeconds(0.2f);

        grabDamageTimer = 0f;

        while (isGrabbing)
        {
            Vector3 targetPos =
                player.position +
                player.forward * grabOffset +
                Vector3.up * grabHeightOffset;

            transform.position = targetPos;

            // Face player
            Vector3 lookDir = player.position - transform.position;
            lookDir.y = 0f;
            if (lookDir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookDir);

            grabDamageTimer -= Time.deltaTime;

            if (grabDamageTimer <= 0f)
            {
                Debug.Log("Grab Damage: " + damage);
                // player.GetComponent<PlayerHealth>()?.TakeDamage(damage);

                grabDamageTimer = grabDamageInterval;
            }

            yield return null;
        }
    }
    public void EndGrab()
    {
        if (currentGrabber == this)
            currentGrabber = null;

        Rigidbody enemyRb = GetComponent<Rigidbody>();

        enemyRb.constraints = RigidbodyConstraints.FreezeRotation;
        enemyRb.isKinematic = true;

        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
        }

        Physics.IgnoreCollision(enemyCollider, playerCollider, false);

        isGrabbing = false;
        isBusy = false;

        SetNextAttackTime();
    }
    // =======================================================================
    // Shoot Enemy Methods
    // =======================================================================

    void HandleStaticShooter()
    {
        if (player == null) return;

        shootTimer -= Time.deltaTime;

        if (shootTimer <= 0f)
        {
            Shoot();
            shootTimer = shootInterval;
        }
    }
    void HandleMovingShooter()
    {
        if (player == null) return;

        // Shooting
        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0f)
        {
            Shoot();
            shootTimer = shootInterval;
        }

        // Random reposition
        positionTimer -= Time.deltaTime;

        if (positionTimer <= 0f)
        {
            SetRandomShootPosition();
            positionTimer = changePositionTime;
        }

        agent.SetDestination(shootMoveTarget);
    }
    void HandleCoverShooter()
    {
        if (player == null) return;

        if (currentCover == null)
        {
            FindNearestCover();
            return;
        }

        switch (coverState)
        {
            case CoverState.MovingToCover:

                agent.stoppingDistance = 0.1f;
                agent.SetDestination(coverPos);

                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    coverTimer = coverTime;
                    coverState = CoverState.Waiting;
                }

                break;

            case CoverState.Waiting:

                coverTimer -= Time.deltaTime;

                if (coverTimer <= 0f)
                {
                    GenerateRandomPeekPosition();
                    coverState = CoverState.MovingToPeek;
                }

                break;

            case CoverState.MovingToPeek:

                agent.SetDestination(peekPos);

                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    // Check visibility
                    if (HasLineOfSight())
                    {
                        Shoot();
                    }
                    else
                    {
                        // Try another peek point
                        GenerateRandomPeekPosition();
                        break;
                    }

                    // Immediately go back to cover
                    coverState = CoverState.MovingToCover;
                }

                break;

            case CoverState.Peeking:

                // Check visibility
                if (!HasLineOfSight())
                {
                    currentPeekAttempts++;

                    if (currentPeekAttempts >= maxPeekAttempts)
                    {
                        // Give up and return to cover
                        coverState = CoverState.MovingToCover;
                        currentPeekAttempts = 0;
                        break;
                    }

                    // Try another peek position
                    GenerateRandomPeekPosition();
                    coverState = CoverState.MovingToPeek;
                    break;
                }

                // Reset attempts if visible
                currentPeekAttempts = 0;

                peekTimer -= Time.deltaTime;

                shootTimer -= Time.deltaTime;
                if (shootTimer <= 0f)
                {
                    Shoot();
                    shootTimer = shootInterval;
                }

                if (peekTimer <= 0f)
                {
                    coverState = CoverState.MovingToCover;
                }

                break;
        }
    }
    void FindNearestCover()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, coverSearchRadius);

        float closest = Mathf.Infinity;
        Transform best = null;

        foreach (Collider col in hits)
        {
            if (!col.CompareTag("Destroyable")) continue;

            float dist = Vector3.Distance(transform.position, col.transform.position);

            if (dist < closest)
            {
                closest = dist;
                best = col.transform;
            }
        }

        if (best == null) return;

        currentCover = best;

        Vector3 dirFromPlayer = (best.position - player.position).normalized;
        dirFromPlayer.y = 0f;

        coverPos = best.position + dirFromPlayer * 1.5f;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(coverPos, out hit, 2f, NavMesh.AllAreas))
            coverPos = hit.position;

        coverState = CoverState.MovingToCover;
    }
    void GenerateRandomPeekPosition()
    {
        int attempts = 5;

        for (int i = 0; i < attempts; i++)
        {
            float randomAngle = Random.Range(0f, 360f);

            float rad = randomAngle * Mathf.Deg2Rad;

            Vector3 randomOffset = new Vector3(
                Mathf.Cos(rad),
                0f,
                Mathf.Sin(rad)
            ) * peekRadius;

            Vector3 candidatePos = currentCover.position + randomOffset;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(candidatePos, out hit, 2f, NavMesh.AllAreas))
            {
                peekPos = hit.position;
                return;
            }
        }

        // fallback
        peekPos = currentCover.position;
    }

    bool HasLineOfSight()
    {
        Vector3 origin = transform.position + Vector3.up * visionHeight;
        Vector3 target = player.position + Vector3.up * 1.5f;

        Vector3 direction = (target - origin).normalized;

        float distance = Vector3.Distance(origin, target);

        if (Physics.Raycast(origin, direction, out RaycastHit hit, distance))
        {
            if (hit.transform.CompareTag("Player"))
                return true;
        }

        return false;
    }
    void Shoot()
    {
       
        if (projectilePrefab == null || firePoint == null || player == null)
            return;
       
        Vector3 targetPosition = player.position + Vector3.up * 2.3f; // Aim slightly above feet

        Vector3 direction = (targetPosition - firePoint.position).normalized;

        GameObject proj = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.LookRotation(direction)
        );

        Rigidbody rb = proj.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.velocity = Vector3.zero; // Clear anything
            rb.angularVelocity = Vector3.zero;

            rb.useGravity = false; // VERY IMPORTANT for straight bullets
            rb.drag = 0f;

            rb.velocity = direction * shootSpeed;
        }

        Debug.DrawRay(firePoint.position, direction * 5f, Color.red, 2f);
    }
    void SetRandomShootPosition()
    {
        Vector3 randomDir = Random.insideUnitSphere * moveRadius;
        randomDir.y = 0;

        Vector3 targetPos = transform.position + randomDir;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(targetPos, out hit, moveRadius, NavMesh.AllAreas))
        {
            shootMoveTarget = hit.position;
        }
    }
  
   
}