using QFSW.MOP2;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.Controls;
using static UnityEngine.UI.Image;

public class EnemyManager : MonoBehaviour
{
    #region EnemyData Settings
    // ================================
    // SCRIPTABLE OBJECT
    // ================================

    [Header("Stats Reference")]
    public EnemyStats enemyStats;

    public string enemyName;

    public ObjectPool After_DeathExplosion;
    public ObjectPool After_BloodExplosion;
    public Transform finisherTarget;
    public GameObject finisherIndicator;
    public float finisherTime;


    // ================================
    // ENUMS
    // ================================

    public Animator animator;
    public enum EnemyType { Melee, Shoot, Touch, Thrower, Tombstone, Guard, Jumper, Suicider, Spider }
    public enum CoverType { No, Yes }
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


    #endregion

    #region Melee Enemy Settings
    // ================================
    // MELEE ATTACK SETTINGS
    // ================================

    [Header("Melee Attack Settings")]
    public float attackRange;
    public float attackIntervalMin;
    public float attackIntervalMax;
    public float recoverTime;
    public bool isAttacking;

    [SerializeField] private bool airEnemy;

    [SerializeField] float directAttackRadius = 8f;

    [SerializeField] float zigZagDistance = 4f;
    [SerializeField] float zigZagInterval = 0.5f;

    float zigZagTimer;
    bool zigLeft;
    Vector3 currentMoveTarget;
    // ================================
    // DASH SETTINGS
    // ================================

    [Header("Dash Settings")]
    public bool canDash;
    public float dashForce;
    [SerializeField] private float jumpHeight = 3f;
    [SerializeField] private float landingCheckDistance = 0.5f;
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

    private Coroutine dashCoroutine;
    public bool isDashing;
    #endregion

    #region Shooter Enemy Settings
    // ================================
    // SHOOTER ENEMY SETTINGS
    // ================================

    [Header("Shoot Enemy Settings")]

    public Transform firePoint;
    public ObjectPool projectilePrefab;
    public float shootSpeed;
    public float shootInterval;

    public bool canShootMove;
    public float changePositionTime;   // Time before reposition
    public float moveRadius;           // Random move radius

    [Header("Burst Shooting")]

    public bool burstShooting = false;
    public int burstCount = 3;
    public float burstInterval = 0.1f;

    private bool isBursting;

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

    public AudioSource attack3dAudioSource;
    private CoverState coverState;

    private enum CoverState
    {
        MovingToCover,
        Waiting,
        MovingToPeek,
        Peeking
    }
    #endregion

    #region TombStone Enemy Settings
    // ================================
    // TOMBSTONE (SPAWNER)
    // ================================

    [Header("Tombstone Settings")]
    public ObjectPool spawnEnemyPrefab;
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

    private Transform Target;
    [SerializeField]

    private Rigidbody AttackProjectile;
    [Range(0, 1)]
    [Tooltip("Using a values closer to 0 will make the agent throw with the lower force"
        + "down to the least possible force (highest angle) to reach the target.\n"
        + "Using a value of 1 the agent will always throw with the MaxThrowForce below.")]
    public float ForceRatio = 0;
    [SerializeField]
    [Tooltip("If the required force to throw the attack is greater than this value, "
        + "the agent will move closer until they come within range.")]
    private float MaxThrowForce = 25;

    [SerializeField]
    private LayerMask SightLayers;
    [SerializeField]
    private float AttackDelay = 5f;

    [SerializeField]
    private bool UseMovementPrediction;
    public PredictionMode MovementPredictionMode;
    [Range(0.01f, 5f)]
    public float HistoricalTime = 1f;
    [Range(1, 100)]
    public int HistoricalResolution = 10;
    private Queue<Vector3> HistoricalPositions;

    private float HistoricalPositionInterval;
    private float LastHistoryRecordedTime;


    private float SpherecastRadius = 0.5f;
    private float LastAttackTime;

    #endregion

    #region Thrower Enemy Settings
    // ================================
    // THROWER DASH SETTINGS
    // ================================

    [Header("Thrower Dash Settings")]

    public bool canThrowerDash = true;

    public float throwerDashForce = 15f;
    public float throwerDashDuration = 0.35f;

    public float throwerDashInterval = 2f;

    private float throwerDashTimer;
    private bool isThrowerDashing;

    #endregion

    #region Guard Type Settings
    [Header("Guard Shooter Settings")]
    public float ImmortalRadius;
    private List<EnemyHealth> guardedEnemies = new List<EnemyHealth>();

    [Header("Guard-Shooter Settings")]
    public bool isShooterGuard;

    public ObjectPool GuardProjectile;
    public float GuardShootSpeed;
    public float GuardShootInterval;

    private float guardShootTimer;

    #endregion

    #region Suicider Enemy Settings
    // ================================
    // SUICIDER SETTINGS
    // ================================

    [Header("Suicider Settings")]

    public float detonateDistance = 2.5f;

    #endregion

    #region Private Variables
    // ================================
    // PRIVATE VARIABLES
    // ================================

    private NavMeshAgent agent;
    private Transform player;
    private PlayerMovement playerMovement;
    private State currentState;

    private float nextAttackTime;
    public float attackCooldownTimer;
    private bool isBusy;


    private float shootTimer;
    private float positionTimer;
    private Vector3 shootMoveTarget;


    // ================================
    // UNITY METHODS
    // ================================
    public bool RotateExternally;

    #endregion

    #region Audio Keys
    [Header("Audio")]
    public string attackHitKey;
    public string attackTryKey;

    #endregion

    #region Surface Type Settings
    // ================================
    // SURFACE TYPE SETTINGS
    // ================================

    [Header("Surface Type Settings")]

    public float spriteHeight;
    public float colliderHeight;

    public GameObject sprite;

    #endregion

    public bool kickOn;

    public bool projectile_Parried;
    private EnemyHealth enemyHealth;
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        playerMovement = player.GetComponent<PlayerMovement>();
        enemyCollider = GetComponent<Collider>();
        playerCollider = player.GetComponent<Collider>();
        attack3dAudioSource = GetComponent<AudioSource>();
        enemyHealth = GetComponent<EnemyHealth>();
        Target = player;


    }

    private void Start()
    {
        if (enemyStats == null)
            return;

        #region Type Reference Set
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

        #endregion

        #region Melee Stats Reference Set
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

        #endregion

        #region Shooter Stats Reference Set
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
        // Burst STATS
        // =====================

        burstShooting = enemyStats.burstShooting;
        burstCount = enemyStats.burstCount;
        burstInterval = enemyStats.burstInterval;


        // =====================
        // Cover STATS
        // =====================

        coverTime = enemyStats.CoverTime;
        peekDuration = enemyStats.PeekDuration;
        peekRadius = enemyStats.PeekRadius;
        coverSearchRadius = enemyStats.CoverSearchRadius;
        visionHeight = enemyStats.VisionHeight;

        #endregion

        #region Tombstone Stats Reference Set

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
        recoverAfterShot = enemyStats.recoverAfterShot;
        initialSpawnDelay = enemyStats.initialSpawnDelay;

        //EnemyThrowData
        AttackProjectile.useGravity = false;
        AttackProjectile.isKinematic = true;
        SpherecastRadius = AttackProjectile.GetComponent<SphereCollider>().radius;
        LastAttackTime = Random.Range(0, 5);

        int capacity = Mathf.CeilToInt(HistoricalResolution * HistoricalTime);
        HistoricalPositions = new Queue<Vector3>(capacity);
        for (int i = 0; i < capacity; i++)
        {
            HistoricalPositions.Enqueue(Target.position);
        }
        HistoricalPositionInterval = HistoricalTime / HistoricalResolution;
        #endregion

        #region Thrower Stats Reference Set

        canThrowerDash = enemyStats.canThrowerDash;
        throwerDashDuration = enemyStats.throwerDashDuration;
        throwerDashForce = enemyStats.throwerDashForce;
        throwerDashInterval = enemyStats.throwerDashInterval;
        #endregion

        #region Guard Stats Reference Set

        ImmortalRadius = enemyStats.ImmortalRadius;
        isShooterGuard = enemyStats.isShooterGuard;
        GuardProjectile = enemyStats.GuardProjectile;
        GuardShootSpeed = enemyStats.GuardShootSpeed;
        GuardShootInterval = enemyStats.GuardShootInterval;
        #endregion

        #region Suicider Enemy Settings
        detonateDistance = enemyStats.detonateDistance;
        #endregion
        After_BloodExplosion = enemyStats.AfterBlood_ExplosionPrefab;
        After_DeathExplosion = enemyStats.AfterDeath_ExplosionPrefab;
        finisherTime = enemyStats.finisherTime;

        #region Audio key Set
        // =====================
        // Audio KEYS
        // =====================
        attackHitKey = enemyStats.attackHitKey;
        attackTryKey = enemyStats.attackTryKey;
        #endregion

        enemyName = enemyStats.enemyName;
        spriteHeight = enemyStats.spriteHeight;
        colliderHeight = enemyStats.colliderHeight;

        // =====================
        // STATE INITIALIZATION
        // =====================

        if (enemyType == EnemyStats.EnemyType.Melee)
        {
            currentState = State.Chase;
            SetNextAttackTime();
        }
        if (enemyType == EnemyStats.EnemyType.Spider)
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
            spawnTimer = spawnInterval;
        }


        if (enemyType == EnemyStats.EnemyType.Tombstone && isShooterTombstone)
        {
            repositionTimer = repositionInterval;

            initialDelayTimer = initialSpawnDelay;
            hasStartedCycle = false;
        }


        if (surfaceType == EnemyStats.SurfaceType.Air)
        {
            // Move sprite up
            if (sprite != null)
            {
                Vector3 pos = sprite.transform.localPosition;
                pos.y = spriteHeight;
                sprite.transform.localPosition = pos;
            }

            // Adjust collider center
            Collider col = GetComponent<Collider>();

            if (col is CapsuleCollider capsule)
            {
                Vector3 center = capsule.center;
                center.y = colliderHeight;
                capsule.center = center;
            }
            else if (col is BoxCollider box)
            {
                Vector3 center = box.center;
                center.y = colliderHeight;
                box.center = center;
            }
        }

    }
    private bool lastFinisherState;
    private void Update()
    {
        CheckGrounded();
        if (kickOn) return;
        if (enemyHealth.dead) return;
        if (enemyHealth.finisherActive) return;

        if (playerMovement.finisherActive != lastFinisherState)
        {
            lastFinisherState = playerMovement.finisherActive;

            if (lastFinisherState)
            {
                agent.enabled = false;
                animator.SetFloat("Movement", 0);
            }
            else
            {
                agent.enabled = true;
            }
        }

        if (playerMovement.finisherActive)
            return;
        //if (!kickOn && enemyType == EnemyStats.EnemyType.Shoot && !canShootMove)
        //{
        //    // Get direction to player (ignore Y so it only rotates horizontally)
        //    Vector3 direction = player.transform.position - transform.position;
        //    direction.y = 0f;
        //    if (direction.sqrMagnitude > 0.001f) // Avoid errors if player is at same position
        //    {
        //        Quaternion targetRotation = Quaternion.LookRotation(direction);
        //        if (!RotateExternally)
        //            transform.rotation = targetRotation;
        //    }
        //}



        if (player == null && isBusy) return;

        float distance = Vector3.Distance(transform.position, player.position);

        animator.SetFloat("Movement", agent.velocity.magnitude);
        //float speed = agent.velocity.magnitude / agent.speed;

        //bool isStopping =
        //    !agent.pathPending &&
        //    agent.remainingDistance <= agent.stoppingDistance;

        //if (isStopping)
        //{
        //    speed = 0f;

        //    // 🔥 force immediate stop (no damping)
        //    animator.SetFloat("Movement", 0f);
        //}
        //else
        //{
        //    // smooth only while moving
        //    animator.SetFloat("Movement", speed, 0.1f, Time.deltaTime);
        //}

        switch (currentState)
        {
            case State.Chase:
                if (enemyType == EnemyStats.EnemyType.Spider)
                    HandleChaseSpider();
                else
                    HandleChase();
                break;
        }
        if (enemyType == EnemyStats.EnemyType.Shoot)
        {
            Vector3 dir =
       player.position - transform.position;

            dir.y = 0f;

            if (dir.sqrMagnitude < 0.01f)
                return;

            Quaternion targetRot =
                Quaternion.LookRotation(dir);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    10 * Time.deltaTime
                );
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
            if (isShooterTombstone)
                HandleShooterTombstone();
            else
                HandleTombstone();

            return;
        }
        if (enemyType == EnemyStats.EnemyType.Thrower)
        {
            if (isShooterTombstone)
            {
                HandleShooterTombstone();

            }
            if (canThrowerDash)
                HandleThrower();


            return;
        }

        if (enemyType == EnemyStats.EnemyType.Guard)
        {
            HandleGuardAura();

            if (enemyStats.isShooterGuard)
                HandleShooterGuard();

            return;
        }
        if (enemyType == EnemyStats.EnemyType.Suicider)
        {
            HandleSuicider();
        }


        if (LastHistoryRecordedTime + HistoricalPositionInterval < Time.time && isShooterTombstone)
        {
            LastHistoryRecordedTime = Time.time;
            HistoricalPositions.Dequeue();
            HistoricalPositions.Enqueue(Target.position);
        }


    }

    #region Collision - Methods
    // ================================
    // ATTACK EXECUTION
    // ================================
    [SerializeField] private float knockbackForce = 8f;
    [SerializeField] private float knockbackUpward = 2f;
    public bool hasHitPlayer;
    public void AttackRefernce(Collision collision)
    {

        if (hasHitPlayer) return;
        //Debug.Log("kaam nhi kr rha ha!");
        if (!collision.gameObject.CompareTag("Player")) return;

        if (enemyType != EnemyStats.EnemyType.Melee && enemyType != EnemyStats.EnemyType.Spider) return;
        //if (!isBusy) return;

        // 🔥 IMPORTANT: Cooldown check
        //if (Time.time < nextAttackTime) return;

        //Debug.Log("Player");
        if (!collision.gameObject.GetComponent<PlayerMovement>().canKick) return;

        SFXManager.Instance.PlayNPC_SFX(attack3dAudioSource, enemyName + "/" + attackHitKey, 0.5f);
        PlayerMovement pm = collision.gameObject.GetComponent<PlayerMovement>();

        if (pm != null)
        {
            Vector3 dir = (collision.transform.position - transform.position).normalized;
            dir.y = 0f;

            Vector3 force = dir * 20f + Vector3.up * 2f;

            pm.ApplyKnockback(force, 0.25f);
        }

        Debug.Log("Melee Damage: " + damage);

        HurtPlayer();


        PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
        playerHealth.ManageHealth(-damage);
        playerHealth.hit = true;

        //SFXManager.Instance.PlaySFX(enemyName+ "/"+ attackHitKey, 1f);
        hasHitPlayer = true;

        // Set cooldown immediately
        nextAttackTime = Time.time + Random.Range(attackIntervalMin, attackIntervalMax);
    }


    #endregion

    #region Melee - Enemy Method
    // =======================================================================
    // Melee Enemy Methods
    // =======================================================================

    void HandleChase()
    {
        if (airEnemy)
        {
            HandleZigZagChase();
            return;
        }

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
            agent.isStopped = true;              // 🔥 removes path completely
            agent.velocity = Vector3.zero;    // 🔥 force velocity to zero

            if (!isAttacking)
            {
                // =============================
                // GRAB LOGIC
                // =============================

                hasHitPlayer = false;

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
  
    void HandleZigZagChase()
    {
        if (player == null || !agent.enabled)
            return;

        float distanceToPlayer =
            Vector3.Distance(
                transform.position,
                player.position
            );

        // ==========================
        // DIRECT CHASE + ATTACK
        // ==========================
        if (distanceToPlayer <= directAttackRadius)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);

            if (distanceToPlayer <= attackRange)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;

                if (!isAttacking)
                {
                    hasHitPlayer = false;

                    if (canGrab && !isGrabbing)
                    {
                        if (currentGrabber == null)
                        {
                            currentGrabber = this;
                            StartCoroutine(GrabRoutine());
                        }
                        else
                        {
                            StartCoroutine(AttackRoutine());
                        }
                    }
                    else
                    {
                        StartCoroutine(AttackRoutine());
                    }
                }
            }

            return;
        }

        // ==========================
        // ZIG-ZAG CHASE
        // ==========================
        zigZagTimer -= Time.deltaTime;

        if (zigZagTimer <= 0f)
        {
            zigZagTimer = zigZagInterval;

            Vector3 dirToPlayer =
                (player.position - transform.position).normalized;

            Vector3 side =
                zigLeft
                ? -Vector3.Cross(dirToPlayer, Vector3.up)
                : Vector3.Cross(dirToPlayer, Vector3.up);

            zigLeft = !zigLeft;

            // Reduce zig-zag amount when getting closer
            float sideAmount =
                Mathf.Clamp(
                    distanceToPlayer * 0.25f,
                    0f,
                    zigZagDistance
                );

            // IMPORTANT:
            // Always move FORWARD toward player
            // while adding side movement
            currentMoveTarget =
                transform.position +
                dirToPlayer * 4f +
                side * sideAmount;
        }

        agent.isStopped = false;
        agent.SetDestination(currentMoveTarget);

        // Optional: smoothly face movement direction
        Vector3 moveDir =
            agent.desiredVelocity;

        moveDir.y = 0f;

        if (moveDir.sqrMagnitude > 0.1f)
        {
            Quaternion targetRot =
                Quaternion.LookRotation(moveDir);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    Time.deltaTime * 8f
                );
        }
    }
    void HandleChaseSpider()
    {
        float currentDistance = Vector3.Distance(transform.position, player.position);

        if (!isBusy && Time.time >= nextAttackTime)
        {
            // =============================
            // GRAB LOGIC
            // =============================

            if (currentDistance < attackRange)
            {
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
    [HideInInspector]
    public int dashToken = 0;
    private bool dashForceCancelled;
    IEnumerator AttackRoutine()
    {
        isBusy = true;
        dashForceCancelled = false;   // 🔥 Reset at start


        //yield return new WaitForSeconds(0.1f);

        // 🔥 If dash was cancelled during delay → exit attack
        if (dashForceCancelled)
        {
            isBusy = false;
            yield break;
        }

        currentState = State.Attack;
        isAttacking = true;

        animator.SetTrigger("Attack");


        if (enemyType != EnemyStats.EnemyType.Spider && agent.enabled)
            agent.isStopped = true;

        if (canDash && !dashForceCancelled)
        {
            StartDash();

            if (dashCoroutine != null)
                yield return dashCoroutine;
        }

        if (dashForceCancelled)
        {
            isBusy = false;
            isAttacking = false;
            yield break;
        }

        currentState = State.Idle;
        yield return new WaitForSeconds(recoverTime);

        currentState = State.Chase;
        if (agent.enabled)
            agent.ResetPath();                 // 🔥 removes path completely
        isBusy = false;
        isAttacking = false;


    }
    public void AttackPlaySoundEffect()
    {
        SFXManager.Instance.PlayNPC_SFX(attack3dAudioSource, enemyName + "/" + attackTryKey, 1f);

    }
    void StartDash()
    {
        if (dashCoroutine != null)
            return;

        dashToken++; // New dash session

        dashCoroutine = StartCoroutine(DashRoutine(dashToken));
    }
    IEnumerator DashRoutine(int token)
    {
        yield return new WaitForSeconds(dashLoadTime);

        if (token != dashToken)
            yield break;

        Rigidbody rb = GetComponent<Rigidbody>();

        // Disable NavMesh completely during jump
        if (agent.enabled)
            agent.enabled = false;

        rb.isKinematic = false;
        rb.useGravity = true;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Face player
        Vector3 lookDir = player.position - transform.position;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(lookDir);

        // -------- Calculate jump arc --------
        SFXManager.Instance.PlayNPC_SFX(attack3dAudioSource, enemyName + "/" + attackTryKey, 0.5f);
        Vector3 targetPos = player.position;

        Vector3 horizontalDir = targetPos - transform.position;
        horizontalDir.y = 0f;

        float distance = horizontalDir.magnitude;

        if (distance > 0.1f)
            horizontalDir.Normalize();

        float gravity = Mathf.Abs(Physics.gravity.y);

        float verticalVelocity =
            Mathf.Sqrt(2f * gravity * jumpHeight);

        float timeUp =
            verticalVelocity / gravity;

        float totalAirTime =
            timeUp * 2f;

        Vector3 horizontalVelocity =
            horizontalDir * (distance / totalAirTime) * 5f;

        Vector3 launchVelocity =
            horizontalVelocity +
            Vector3.up * verticalVelocity;

        rb.velocity = launchVelocity;

        // -------- Wait until landed --------

        while (true)
        {
            if (token != dashToken)
                yield break;

            bool grounded =
                Physics.Raycast(
                    transform.position + Vector3.up * 0.1f,
                    Vector3.down,
                    landingCheckDistance
                );

            if (grounded && rb.velocity.y <= 0.1f)
                break;

            yield return null;
        }

        EndDash(token);
    }
    public void EndDash(int token)
    {
        if (token != dashToken)
            return;

        Rigidbody rb = GetComponent<Rigidbody>();

        if (!rb.isKinematic)
            rb.velocity = Vector3.zero;
        if (!rb.isKinematic)
            rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // Re-enable NavMesh
        if (!agent.enabled)
            agent.enabled = true;

        agent.Warp(transform.position);

        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.isStopped = true;

        dashCoroutine = null;

        //Debug.Log("Spider Jump Finished");
    }
    public void StopDashImmediate()
    {
        dashForceCancelled = true;   // 🔥 Mark dash as cancelled

        if (dashCoroutine != null)
        {
            StopCoroutine(dashCoroutine);
            dashCoroutine = null;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (agent != null)
        {
            agent.updatePosition = true;
            agent.updateRotation = true;

            if (!agent.enabled)
                agent.enabled = true;

            agent.Warp(transform.position);
            agent.isStopped = true;
        }
        SetNextAttackTime();
        currentState = State.Chase;
        isBusy = false;
        isAttacking = false;
        Debug.Log("DashStopped immediately");
    }
    // ================================
    // RANDOM ATTACK INTERVAL
    // ================================

    void SetNextAttackTime()
    {
        float randomTime = Random.Range(attackIntervalMin, attackIntervalMax);
        nextAttackTime = randomTime;
    }

    IEnumerator GrabRoutine()
    {
        if (isGrabbing) yield break;

        isBusy = true;
        isGrabbing = true;
        Physics.IgnoreCollision(enemyCollider, playerCollider, true);
        // Disable NavMesh
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }


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
    #endregion

    #region Shooter - Enemy Method
    // =======================================================================
    // Shoot Enemy Methods
    // =======================================================================

    void HandleStaticShooter()
    {
        if (player == null) return;

        shootTimer -= Time.deltaTime;

        if (shootTimer <= 0f)
        {
            TryShoot();
            shootTimer = shootInterval;
        }
    }
    bool isShooting;
    void HandleMovingShooter()
    {
        if (player == null)
            return;

        shootTimer -= Time.deltaTime;

        if (shootTimer <= 0f && !isShooting)
        {
            StartCoroutine(HandleShooting());
        }

        agent.SetDestination(shootMoveTarget);
    }
    IEnumerator HandleShooting()
    {
        isShooting = true;

        TryShoot();
        
        yield return new WaitForSeconds(1f);

        SetRandomShootPosition();

        shootTimer = shootInterval;

        isShooting = false;
    }
    void HandleCoverShooter()
    {
        if (player == null) return;

        if (currentCover == null)
        {
            FindNearestCover();
            // If still no cover found → behave like moving shooter
            if (currentCover == null)
            {
                HandleMovingShooter();
                return;
            }
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
                        TryShoot();
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
                    TryShoot();
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
    void TryShoot()
    {
        if (burstShooting)
        {
            if (!isBursting)
                StartCoroutine(BurstShoot());
        }
        else
        {
            Shoot();
        }


    }
    IEnumerator BurstShoot()
    {
        isBursting = true;

        for (int i = 0; i < burstCount; i++)
        {
            Shoot();

            if (i < burstCount - 1)
                yield return new WaitForSeconds(burstInterval);

            SFXManager.Instance.PlayNPC_SFX(attack3dAudioSource, enemyName + "/" + attackTryKey, 1f);
        }

        isBursting = false;
    }
    void Shoot()
    {


        if (projectilePrefab == null || firePoint == null || player == null)
            return;

        SFXManager.Instance.PlayNPC_SFX(attack3dAudioSource, enemyName + "/" + attackTryKey, 1f);
        animator.SetTrigger("Attack");
        Vector3 targetPosition = player.position + Vector3.up * 2.3f; // Aim slightly above feet

        Vector3 direction = (targetPosition - firePoint.position).normalized;

        GameObject proj = projectilePrefab.GetObject(firePoint.position, Quaternion.LookRotation(direction));

        Rigidbody rb = proj.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.velocity = Vector3.zero; // Clear anything
            rb.angularVelocity = Vector3.zero;

            rb.useGravity = false; // VERY IMPORTANT for straight bullets
            rb.drag = 0f;

            rb.velocity = direction * shootSpeed;
        }
        //animator.SetTrigger("Attack");
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
    #endregion

    #region Tombstone - Enemy Methods
    void HandleTombstone()
    {
        if (spawnEnemyPrefab == null) return;

        spawnTimer -= Time.deltaTime;

        if (spawnTimer > 0f) return;

        spawnTimer = spawnInterval;

        // =============================
        // LIMIT MODE
        // =============================
        if (useSpawnLimit)
        {
            if (currentSpawnedCount < spawnLimit)
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

        GameObject newEnemy =
            spawnEnemyPrefab.GetObject(spawnPos,
            Quaternion.identity);



        // Small pop effect
        Rigidbody rb = newEnemy.GetComponent<Rigidbody>();
        if (rb != null)
            rb.AddForce(Vector3.up * 4f, ForceMode.Impulse);

        if (useSpawnLimit)
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
        int count = spawnBatchCount;

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

            GameObject enemy =
                spawnEnemyPrefab.GetObject(spawnPos, Quaternion.identity);


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
            repositionTimer = repositionInterval;
        }
        if (agent.enabled)
            agent.SetDestination(tombstoneTargetPos);

        // =========================
        // SPAWN PHASE
        // =========================
        if (!isSpawningPhase && !isChargingShot && !isRecovering)
        {
            SpawnBatch();
            isSpawningPhase = true;
            shootTimer = shootDelayAfterSpawn;
        }

        // =========================
        // CHARGE SHOT PHASE
        // =========================
        if (isSpawningPhase)
        {
            shootTimer -= Time.deltaTime;

            if (shootTimer <= 0f)
            {
                if (projectile_Parried)
                    return;

                agent.isStopped = true;
                //FireProjectileAtPlayer();

                // EnemyThrow Data

                Vector3 origin = transform.position + Vector3.up * 1.5f; // cast from chest height
                Vector3 targetPoint = Target.position + Vector3.up * 1.0f; // aim at chest height

                Vector3 direction = (targetPoint - origin).normalized;
                float distance = Vector3.Distance(origin, targetPoint);

                bool canSeeTarget = Physics.SphereCast(
                    origin,
                    SpherecastRadius,
                    direction,
                    out RaycastHit hit,
                    distance,                // limit cast to target distance
                    SightLayers,
                    QueryTriggerInteraction.Ignore
                );

                if (canSeeTarget && hit.transform.root == Target.root)
                {
                    LastAttackTime = Time.time;
                    AttackProjectile.GetComponent<EnemyProjectile>().enemyRigibody = GetComponent<Rigidbody>();
                    AttackProjectile.transform.SetParent(transform, true);
                    AttackProjectile.transform.localPosition = new Vector3(0, 0, 1f);

                    AttackProjectile.useGravity = false;
                    AttackProjectile.velocity = Vector3.zero;

                    StartCoroutine(Attack());
                }
                isSpawningPhase = false;
                isRecovering = true;
                recoverTimer = recoverAfterShot;
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
    private void OnDrawGizmos()
    {
        if (Target == null) return;

        Vector3 origin = transform.position;
        Vector3 direction = (Target.position + Vector3.up - transform.position).normalized;
        float distance = Vector3.Distance(origin, Target.position);

        // Check hit
        bool hasHit = Physics.SphereCast(
            origin,
            SpherecastRadius,
            direction,
            out RaycastHit hit,
            distance,
            SightLayers
        );

        // Choose color
        if (hasHit && hit.transform == Target)
            Gizmos.color = Color.green;
        else if (hasHit)
            Gizmos.color = Color.red;
        else
            Gizmos.color = Color.yellow;

        // Draw center line
        Gizmos.DrawLine(origin, origin + direction * distance);

        // Draw start sphere
        Gizmos.DrawWireSphere(origin, SpherecastRadius);

        // Draw end sphere
        Gizmos.DrawWireSphere(origin + direction * distance, SpherecastRadius);

        // Draw hit point if something was hit
        if (hasHit)
        {
            Gizmos.DrawWireSphere(hit.point, SpherecastRadius * 0.5f);
        }
    }

    public IEnumerator Attack()
    {

        transform.LookAt(Target);

        AttackProjectile.gameObject.SetActive(true);
        AttackProjectile.transform.SetParent(null, true);
        yield return null;

        if (playerRb == null)
        {
            playerRb = Target.GetComponent<Rigidbody>();
        }

        if (playerCollider == null)
        {
            playerCollider = Target.GetComponent<Collider>();
        }

        // Use collider bounds center if available, otherwise use position
        Vector3 targetPoint = Target.position;

        if (playerCollider != null)
            targetPoint = playerCollider.bounds.center;

        ThrowData throwData = CalculateThrowData(
            targetPoint,
            AttackProjectile.position
        );

        if (UseMovementPrediction && playerRb != null)
        {
            throwData = GetPredictedPositionThrowData(throwData);
        }

        DoThrow(throwData);

        yield return null;

        agent.isStopped = false;

    }

    private ThrowData GetPredictedPositionThrowData(ThrowData directThrowData)
    {
        Vector3 throwVelocity = directThrowData.ThrowVelocity;
        throwVelocity.y = 0;

        float time = directThrowData.DeltaXZ / throwVelocity.magnitude;

        Vector3 playerMovement = Vector3.zero;

        if (MovementPredictionMode == PredictionMode.CurrentVelocity)
        {
            if (playerRb != null)
            {
                playerMovement = playerRb.velocity * time;
            }
        }
        else
        {
            Vector3[] positions = HistoricalPositions.ToArray();
            Vector3 averageVelocity = Vector3.zero;

            for (int i = 1; i < positions.Length; i++)
            {
                averageVelocity += (positions[i] - positions[i - 1]) / HistoricalPositionInterval;
            }

            if (positions.Length > 1)
            {
                averageVelocity /= (positions.Length - 1);
            }

            playerMovement = averageVelocity * time;
        }

        Vector3 baseTargetPosition = Target.position;

        if (playerCollider != null)
            baseTargetPosition = playerCollider.bounds.center;

        Vector3 newTargetPosition = baseTargetPosition + playerMovement;

        ThrowData predictiveThrowData = CalculateThrowData(
            newTargetPosition,
            AttackProjectile.position
        );

        predictiveThrowData.ThrowVelocity = Vector3.ClampMagnitude(
            predictiveThrowData.ThrowVelocity,
            MaxThrowForce
        );

        return predictiveThrowData;
    }

    private void DoThrow(ThrowData ThrowData)
    {
        AttackProjectile.useGravity = true;
        AttackProjectile.isKinematic = false;
        AttackProjectile.velocity = ThrowData.ThrowVelocity;
    }

    private ThrowData CalculateThrowData(Vector3 TargetPosition, Vector3 StartPosition)
    {
        // v = initial velocity, assume max speed for now
        // x = distance to travel on X/Z plane only
        // y = difference in altitudes from thrown point to target hit point
        // g = gravity

        Vector3 displacement = new Vector3(
            TargetPosition.x,
            StartPosition.y,
            TargetPosition.z
        ) - StartPosition;
        float deltaY = TargetPosition.y - StartPosition.y;
        float deltaXZ = displacement.magnitude;

        // find lowest initial launch velocity with other magic formula from https://en.wikipedia.org/wiki/Projectile_motion
        // v^2 / g = y + sqrt(y^2 + x^2)
        // meaning.... v = sqrt(g * (y+ sqrt(y^2 + x^2)))
        float gravity = Mathf.Abs(Physics.gravity.y);
        float throwStrength = Mathf.Clamp(
            Mathf.Sqrt(
                gravity
                * (deltaY + Mathf.Sqrt(Mathf.Pow(deltaY, 2)
                + Mathf.Pow(deltaXZ, 2)))),
            0.01f,
            MaxThrowForce
        );
        throwStrength = Mathf.Lerp(throwStrength, MaxThrowForce, ForceRatio);

        float angle;
        if (ForceRatio == 0)
        {
            // optimal angle is chosen with a relatively simple formula
            angle = Mathf.PI / 2f - (0.5f * (Mathf.PI / 2 - (deltaY / deltaXZ)));
        }
        else
        {
            // when we know the initial velocity, we have to calculate it with this formula
            // Angle to throw = arctan((v^2 +- sqrt(v^4 - g * (g * x^2 + 2 * y * v^2)) / g*x)
            angle = Mathf.Atan(
                (Mathf.Pow(throwStrength, 2) - Mathf.Sqrt(
                    Mathf.Pow(throwStrength, 4) - gravity
                    * (gravity * Mathf.Pow(deltaXZ, 2)
                    + 2 * deltaY * Mathf.Pow(throwStrength, 2)))
                ) / (gravity * deltaXZ)
            );
        }

        if (float.IsNaN(angle))
        {
            // you will need to handle this case when there
            // is no feasible angle to throw the object and reach the target.
            return new ThrowData();
        }

        Vector3 initialVelocity =
            Mathf.Cos(angle) * throwStrength * displacement.normalized
            + Mathf.Sin(angle) * throwStrength * Vector3.up;

        return new ThrowData
        {
            ThrowVelocity = initialVelocity,
            Angle = angle,
            DeltaXZ = deltaXZ,
            DeltaY = deltaY
        };
    }

    private struct ThrowData
    {
        public Vector3 ThrowVelocity;
        public float Angle;
        public float DeltaXZ;
        public float DeltaY;
    }

    public enum PredictionMode
    {
        CurrentVelocity,
        AverageVelocity
    }

    void SetRandomTombstonePosition()
    {

        agent.isStopped = false;
        Vector3 randomDir = Random.insideUnitSphere * moveRadius;
        randomDir.y = 0;

        Vector3 targetPos = transform.position + randomDir;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, moveRadius, NavMesh.AllAreas))
        {
            tombstoneTargetPos = hit.position;
        }
    }
    #endregion

    #region Guard - Enemy Methods
    void HandleGuardAura()
    {
        float radius = enemyStats.ImmortalRadius;

        Collider[] hits = Physics.OverlapSphere(transform.position, radius);

        List<EnemyHealth> currentlyInside = new List<EnemyHealth>();

        foreach (Collider col in hits)
        {
            EnemyHealth eh = col.GetComponent<EnemyHealth>();

            if (eh == null)
                continue;

            if (eh == GetComponent<EnemyHealth>())
                continue; // Skip self

            currentlyInside.Add(eh);

            if (!eh.isInvincible)
            {
                eh.isInvincible = true;
            }
        }

        // Remove invincibility from enemies that left radius
        foreach (EnemyHealth eh in guardedEnemies)
        {
            if (eh != null && !currentlyInside.Contains(eh))
            {
                eh.isInvincible = false;
            }
        }

        guardedEnemies = currentlyInside;
    }
    void HandleShooterGuard()
    {
        if (player == null)
            return;

        guardShootTimer -= Time.deltaTime;

        if (guardShootTimer <= 0f)
        {
            ShootGuardProjectile();
            guardShootTimer = enemyStats.GuardShootInterval;
        }
    }

    void ShootGuardProjectile()
    {
        if (enemyStats.GuardProjectile == null || firePoint == null || player == null)
            return;

        Vector3 targetPosition = player.position + Vector3.up * 2.3f;

        Vector3 direction = (targetPosition - firePoint.position).normalized;

        GameObject proj =
            GuardProjectile.GetObject(firePoint.position,
            Quaternion.LookRotation(direction));


        Rigidbody rb = proj.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.useGravity = false;
            rb.drag = 0f;

            rb.velocity = direction * enemyStats.GuardShootSpeed;
        }

        Debug.DrawRay(firePoint.position, direction * 5f, Color.cyan, 2f);
    }
    public void RemoveAllGuardEffects()
    {
        if (enemyType != EnemyStats.EnemyType.Guard)
            return;
        foreach (EnemyHealth eh in guardedEnemies)
        {
            if (eh != null)
                eh.isInvincible = false;
        }

        guardedEnemies.Clear();
    }
    #endregion

    #region Thrower Enemy Method
    void HandleThrower()
    {
        if (!canThrowerDash || isThrowerDashing)
            return;

        if (projectile_Parried) return;
        throwerDashTimer -= Time.deltaTime;

        if (throwerDashTimer <= 0f)
        {
            throwerDashCoroutine = StartCoroutine(ThrowerDash());
            throwerDashTimer = throwerDashInterval;
        }
    }
    Coroutine throwerDashCoroutine;
    bool dashCancelled;
    IEnumerator ThrowerDash()
    {
        isThrowerDashing = true;
        dashCancelled = false;

        Rigidbody rb = GetComponent<Rigidbody>();

        if (agent != null && agent.enabled)
        {
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.isStopped = true;
        }

        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.None;
        rb.velocity = Vector3.zero;

        int dir = Random.value > 0.5f ? 1 : -1;
        Vector3 dashDir = transform.right * dir;

        rb.AddForce(dashDir * throwerDashForce, ForceMode.Impulse);

        float timer = 0f;

        while (timer < throwerDashDuration)
        {
            if (dashCancelled)
                yield break;

            timer += Time.deltaTime;
            yield return null;
        }
        StopThrowerDash();
    }
    public void StopThrowerDash()
    {
        if (!isThrowerDashing)
            return;

        // Stop coroutine
        if (throwerDashCoroutine != null)
        {
            StopCoroutine(throwerDashCoroutine);
            throwerDashCoroutine = null;
        }

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll; // fully freeze
        }

        if (agent != null)
        {
            if (!agent.enabled)
                agent.enabled = true;

            agent.updatePosition = true;
            agent.updateRotation = true;

            agent.Warp(transform.position);

            // make enemy completely stop
            agent.isStopped = true;
        }

        isThrowerDashing = false;
    }
    public void StopEnemy()
    {
        StopThrowerDash();
        //Rigidbody rb = GetComponent<Rigidbody>();
        //rb.velocity = Vector3.zero ;
        //if (agent != null)
        //{
        //    agent.updatePosition = true;
        //    agent.updateRotation = true;

        //    if (!agent.enabled)
        //        agent.enabled = true;

        //    agent.Warp(transform.position);
        //    agent.isStopped = false;
        //}

        //isThrowerDashing = false;
    }
    #endregion

    #region Thrower Enemy Method
    void HandleSuicider()
    {
        if (player == null)
            return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Chase player
        if (agent != null && agent.enabled)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }

        // Detonate
        if (distance <= detonateDistance)
        {
            EnemyHealth enemyHealth = GetComponent<EnemyHealth>();

            if (enemyHealth != null)
                enemyHealth.Die(false, false, false, false);
            else
                Destroy(gameObject);
        }
    }
    #endregion

    #region Hurt Player Method
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
    public void ResetParrayable()
    {
        projectile_Parried = false;
        //if (gameObject.activeInHierarchy)
        //    StartCoroutine(ResetEnemy());
    }
    public IEnumerator ResetEnemy()
    {
        yield return new WaitForSeconds(2f);
        projectile_Parried = false;
    }
    #endregion
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private float groundCheckOffset = 0.1f;

    public bool IsGrounded;
    public bool CheckGrounded()
    {
        Collider col = GetComponent<Collider>();

        if (col == null)
            return false;

        Vector3 checkPos = new Vector3(
            col.bounds.center.x,
            col.bounds.min.y + groundCheckOffset,
            col.bounds.center.z
        );

        IsGrounded = Physics.CheckSphere(
            checkPos,
            groundCheckRadius,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        return IsGrounded;
    }
    private void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();

        if (col == null)
            return;

        Vector3 checkPos = new Vector3(
            col.bounds.center.x,
            col.bounds.min.y + groundCheckOffset,
            col.bounds.center.z
        );

        Gizmos.color = IsGrounded
            ? Color.green
            : Color.red;

        Gizmos.DrawWireSphere(
            checkPos,
            groundCheckRadius
        );
    }
}