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

    
    public enum EnemyType { Melee, Shoot, Touch, Thrower, Tombstone }
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

    private AudioSource attack3dAudioSource;
    private CoverState coverState;

    private enum CoverState
    {
        MovingToCover,
        Waiting,
        MovingToPeek,
        Peeking
    }


    // ================================
    // TOMBSTONE (SPAWNER)
    // ================================

    [Header("Tombstone Settings")]
    public GameObject spawnEnemyPrefab;
    public float spawnInterval = 3f;
    public int spawnLimit = 5;
    public bool useSpawnLimit = true;

    private float spawnTimer;
    private int currentSpawnedCount;


    // ================================
    // SHOOTER TOMBSTONE
    // ================================

    [Header("Shooter Tombstone Settings")]
    public bool isShooterTombstone;
    public float repositionInterval = 6f;

    public int spawnBatchCount = 3;

    public float shootDelayAfterSpawn = 2f;
    public GameObject shootProjectilePrefab;
    public float mortarArcHeight = 6f;
    public float shootProjectileSpeed = 20f;

    public float recoverAfterShot = 3f;
    public float initialSpawnDelay = 3f;

    private float repositionTimer;
    private float recoverTimer;

    private bool isSpawningPhase;
    private bool isChargingShot;
    private bool isRecovering;

    private Vector3 tombstoneTargetPos;
    private float initialDelayTimer;
    private bool hasStartedCycle;
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

    [Header("Audio")]
    public string attackSoundKey;
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        enemyCollider = GetComponent<Collider>();
        playerCollider = player.GetComponent<Collider>();
        attack3dAudioSource = GetComponent<AudioSource>();
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
        projectilePrefab = enemyStats.projectilePrefab;
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
        //TombStone STATS
        // =====================

        spawnEnemyPrefab = enemyStats.spawnEnemyPrefab;
        spawnInterval = enemyStats.spawnInterval;
        spawnLimit = enemyStats.spawnLimit;
        useSpawnLimit = enemyStats.useSpawnLimit;

        // =====================
        //Royal Shooter TombStone STATS
        // =====================

        isShooterTombstone = enemyStats.isShooterTombstone;
        repositionInterval = enemyStats.repositionInterval;
        spawnBatchCount = enemyStats.spawnBatchCount;
        shootDelayAfterSpawn = enemyStats.shootDelayAfterSpawn;
        shootProjectilePrefab = enemyStats.shootProjectilePrefab;
        shootProjectileSpeed = enemyStats.shootProjectileSpeed;
        recoverAfterShot = enemyStats.recoverAfterShot;
        initialSpawnDelay = enemyStats.initialSpawnDelay;
        mortarArcHeight = enemyStats.mortarArcHeight;

        // =====================
        // Audio KEYS
        // =====================
        attackSoundKey = enemyStats.attackSoundKey;

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

        if (enemyType == EnemyStats.EnemyType.Tombstone)
        {
            spawnTimer = enemyStats.spawnInterval;
        }
        if (enemyType == EnemyStats.EnemyType.Tombstone && enemyStats.isShooterTombstone)
        {
            repositionTimer = enemyStats.repositionInterval;

            initialDelayTimer = enemyStats.initialSpawnDelay;
            hasStartedCycle = false;
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
        if (enemyType == EnemyStats.EnemyType.Tombstone)
        {
            if (enemyStats.isShooterTombstone)
                HandleShooterTombstone();
            else
                HandleTombstone();

            return;
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

        if (currentDistance > attackRange && agent.enabled)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            if (!agent.enabled) return;

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
        if (collision.gameObject.CompareTag("Player"))
        {          
            if (enemyType != EnemyStats.EnemyType.Melee) return;
            if (!isBusy) return;

            // Immediate first hit
            Debug.Log("Melee Damage: " + damage);

            // Start interval cooldown
            HurtPlayer();
            collision.gameObject.GetComponent<PlayerHealth>().ManageHealth(-damage);
            collision.gameObject.GetComponent<PlayerHealth>().hit = true;

            SFXManager.Instance.PlayNPC_SFX(attack3dAudioSource,attackSoundKey, 1f);

            nextAttackTime = Time.time + Random.Range(attackIntervalMin, attackIntervalMax);
        }

    }
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {

            if (enemyType != EnemyStats.EnemyType.Melee) return;
            if (!isBusy) return;

            if (Time.time < nextAttackTime) return;

            Debug.Log("Melee Damage (Interval): " + damage);

            HurtPlayer();
            collision.gameObject.GetComponent<PlayerHealth>().ManageHealth(-damage);
            collision.gameObject.GetComponent<PlayerHealth>().hit = true;

            SFXManager.Instance.PlaySFX(attackSoundKey, 1f);


            nextAttackTime = Time.time + Random.Range(attackIntervalMin, attackIntervalMax);
        }
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

    void HandleTombstone()
    {
        if (enemyStats.spawnEnemyPrefab == null) return;

        spawnTimer -= Time.deltaTime;

        if (spawnTimer > 0f) return;

        spawnTimer = enemyStats.spawnInterval;

        // =============================
        // LIMIT MODE
        // =============================
        if (enemyStats.useSpawnLimit)
        {
            if (currentSpawnedCount < enemyStats.spawnLimit)
            {
                SpawnEnemy();
                currentSpawnedCount++;
            }
        }
        else
        {
            // =============================
            // ENDLESS MODE
            // =============================
            SpawnEnemy();
        }
    }

    void SpawnEnemy()
    {
        Vector3 spawnPos =
            transform.position +
            transform.forward * 1.5f +
            Vector3.up * 0.5f;

        GameObject newEnemy = Instantiate(
            enemyStats.spawnEnemyPrefab,
            spawnPos,
            Quaternion.identity
        );

        // Small pop effect
        Rigidbody rb = newEnemy.GetComponent<Rigidbody>();
        if (rb != null)
            rb.AddForce(Vector3.up * 4f, ForceMode.Impulse);

        if (enemyStats.useSpawnLimit)
        {
            EnemyHealth health = newEnemy.GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.OnDeath += () =>
                {
                    currentSpawnedCount--;
                };
            }
        }
    }
    void SpawnBatch()
    {
        int count = enemyStats.spawnBatchCount;

        float coneAngle = 60f;
        float angleStep = count > 1 ? coneAngle / (count - 1) : 0f;
        float startAngle = -coneAngle / 2f;

        for (int i = 0; i < count; i++)
        {
            float currentAngle = startAngle + angleStep * i;

            Quaternion rotation = Quaternion.Euler(0, currentAngle, 0);
            Vector3 direction = rotation * transform.forward;

            Vector3 spawnPos =
                transform.position +
                direction * 1.5f +
                Vector3.up * 0.5f;

            GameObject enemy = Instantiate(
                enemyStats.spawnEnemyPrefab,
                spawnPos,
                Quaternion.identity
            );

            StartCoroutine(LaunchSpawnedEnemy(enemy, direction));
        }
    }
    IEnumerator LaunchSpawnedEnemy(GameObject enemy, Vector3 direction)
    {
        NavMeshAgent spawnedAgent = enemy.GetComponent<NavMeshAgent>();
        Rigidbody rb = enemy.GetComponent<Rigidbody>();

        if (spawnedAgent != null)
            spawnedAgent.enabled = false;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = true;
            rb.AddForce(direction * 30f + Vector3.up * 4f, ForceMode.Impulse);
        }


        // Small settle delay (optional but smoother)
        yield return new WaitForSeconds(1f);

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (spawnedAgent != null)
        {
            spawnedAgent.enabled = true;
            spawnedAgent.Warp(enemy.transform.position);
        }
    }
 
    void HandleShooterTombstone()
    {
        if (player == null) return;

        // =========================
        // INITIAL DELAY
        // =========================
        if (!hasStartedCycle)
        {
            initialDelayTimer -= Time.deltaTime;

            if (initialDelayTimer <= 0f)
                hasStartedCycle = true;

            return;
        }

        // =========================
        // MOVEMENT PHASE
        // =========================
        repositionTimer -= Time.deltaTime;

        if (repositionTimer <= 0f)
        {
            SetRandomTombstonePosition();
            repositionTimer = enemyStats.repositionInterval;
        }

        agent.SetDestination(tombstoneTargetPos);

        // =========================
        // SPAWN PHASE
        // =========================
        if (!isSpawningPhase && !isChargingShot && !isRecovering)
        {
            //SpawnBatch();
            isSpawningPhase = true;
            shootTimer = enemyStats.shootDelayAfterSpawn;
        }

        // =========================
        // CHARGE SHOT PHASE
        // =========================
        if (isSpawningPhase)
        {
            shootTimer -= Time.deltaTime;

            if (shootTimer <= 0f)
            {
                FireProjectileAtPlayer();
                isSpawningPhase = false;
                isRecovering = true;
                recoverTimer = enemyStats.recoverAfterShot;
            }
        }

        // =========================
        // RECOVER PHASE
        // =========================
        if (isRecovering)
        {
            recoverTimer -= Time.deltaTime;

            if (recoverTimer <= 0f)
            {
                isRecovering = false;
            }
        }
    }
    void FireProjectileAtPlayer()
    {
        if (enemyStats.shootProjectilePrefab == null || player == null)
            return;

        Vector3 start = transform.position + Vector3.up;
        Vector3 target = player.position + Vector3.up;

        GameObject proj = Instantiate(
            enemyStats.shootProjectilePrefab,
            start,
            Quaternion.identity
        );

        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb == null) return;

        rb.useGravity = true;
        rb.drag = 0f;

        Vector3 velocity;

        if (SolveBallisticArc(start, target, Mathf.Abs(Physics.gravity.y), out velocity))
        {
            rb.velocity = velocity;
            proj.transform.rotation = Quaternion.LookRotation(rb.velocity);
        }
    }
    bool SolveBallisticArc(Vector3 start, Vector3 target, float gravity, out Vector3 velocity)
    {
        velocity = Vector3.zero;

        Vector3 toTarget = target - start;

        Vector3 toTargetXZ = new Vector3(toTarget.x, 0f, toTarget.z);
        float horizontalDistance = toTargetXZ.magnitude;
        float verticalOffset = toTarget.y;

        float speed = enemyStats.shootProjectileSpeed; // use fixed launch speed

        float speedSquared = speed * speed;
        float speedFourth = speedSquared * speedSquared;

        float gravityTimesDistance = gravity * horizontalDistance;

        float discriminant = speedFourth
                             - gravity * (gravity * horizontalDistance * horizontalDistance
                             + 2 * verticalOffset * speedSquared);

        if (discriminant < 0)
        {
            // No solution (target too far for given speed)
            return false;
        }

        float sqrt = Mathf.Sqrt(discriminant);

        // Low arc solution (more mortar-like feel)
        float angle = Mathf.Atan((speedSquared - sqrt) / gravityTimesDistance);

        Vector3 direction = toTargetXZ.normalized;

        velocity =
            direction * Mathf.Cos(angle) * speed +
            Vector3.up * Mathf.Sin(angle) * speed;

        return true;
    }
    void SetRandomTombstonePosition()
    {
        Vector3 randomDir = Random.insideUnitSphere * enemyStats.MoveRadius;
        randomDir.y = 0;

        Vector3 targetPos = transform.position + randomDir;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, enemyStats.MoveRadius, NavMesh.AllAreas))
        {
            tombstoneTargetPos = hit.position;
        }
    }   

    [SerializeField] GameObject Sender;
    public void HurtPlayer()
    {

        //PlayerHealth.instance.Damage(damageDone);
        bl_DamageInfo info = new bl_DamageInfo(damage);
        info.Sender = Sender;
        Sender.SetIndicator();
        bl_DamageDelegate.OnDamageEvent(info);
        //CameraShake.Instance.shakeDuration += 0.3f;
    }
}