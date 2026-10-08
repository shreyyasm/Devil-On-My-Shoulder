using System.Collections;
using System.Collections.Generic;
using QFSW.MOP2;
using UnityEngine;
using UnityEngine.AI;

public class BossManager : MonoBehaviour
{
    private enum Attack { HomingMissile, Spread, Laser, DashPunch, Slam, Teleport, SummonDemonicBooks }
    private enum CombatState { Dormant, Intro, Telegraph, Attacking, Repositioning, Vulnerable, Midpoint, Defeated }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private GameObject hibernationPod;
    [SerializeField] private GameObject podGlass;
    [SerializeField] private Transform[] floatingFragments;
    [SerializeField] private Renderer[] arenaLights;
    [SerializeField] private BossHealthBar healthBar;
    [SerializeField] private BossDebugCanvas debugCanvas;
    [Header("Intro")]
    [SerializeField] private float awakenDistance = 14f;
    [SerializeField] private float introDuration = 2.4f;
    [Header("Combat")]
    [SerializeField] private float movementSpeed = 7.5f;
    [SerializeField] private float attackCooldown = .75f;
    [SerializeField] private float projectileDamage = 8f;
    [SerializeField] private float projectileSpeed = 24f;
    [SerializeField, Min(1)] private int projectileCount = 18;
    [Header("Homing Missile")]
    [SerializeField] private float missileDamage = 22f;
    [SerializeField] private float missileLifetime = 5.5f;
    [SerializeField] private float missileSpeed = 21f;
    [SerializeField] private float missileTurnRate = 150f;
    [SerializeField] private float missileExplosionRadius = 2.4f;
    [Tooltip("Minimum time between homing-missile launches. Keeps the missile pressure consistent.")]
    [SerializeField, Min(.1f)] private float homingMissileInterval = 1.25f;
    [Header("Demonic Book Summon")]
    [Tooltip("The existing Demonic Book pool used by the boss in phase two.")]
    [SerializeField] private ObjectPool demonicBookPool;
    [SerializeField, Min(1)] private int demonicBookSummonCount = 2;
    [SerializeField, Min(2f)] private float demonicBookSummonRadius = 7f;
    [SerializeField, Min(0f)] private float demonicBookSummonInterval = .25f;
    [SerializeField] private float punchDamage = 18f;
    [SerializeField] private float slamDamage = 26f;
    [SerializeField] private float laserDamage = 20f;
    [SerializeField] private float laserLength = 55f;
    [SerializeField] private float laserSweepDuration = 2.2f;
    [SerializeField] private float repositionMinimumDistance = 8f;
    [SerializeField] private float repositionMaximumDistance = 12.5f;
    [Header("Attack Strategy")]
    [SerializeField, Range(0f, .5f)] private float surpriseAttackChance = .18f;
    [SerializeField] private float minimumTimeBetweenSurprises = 10f;
    [SerializeField] private float telegraphDuration = .35f;
    [SerializeField] private float vulnerabilityDuration = 1.1f;
    [SerializeField] private float vulnerabilityDamageMultiplier = 2.25f;
    [SerializeField] private float comboDelay = .2f;
    [Header("Counterplay")]
    [SerializeField] private int powerNodeCount = 3;
    [SerializeField] private float powerNodeStunDuration = 3f;
    [SerializeField] private float powerNodeDamageMultiplier = 3f;

    private bool awake, attacking, defeated;
    private int phase;
    private float nextAttack;
    private float nextHomingMissileLaunch;
    private Attack lastAttack;
    private bool forceDashPunch, forceTeleport;
    private float combatHeight;
    private int patternStep;
    private float nextSurpriseTime;
    private bool midpointQueued, midpointComplete;
    private CombatState combatState = CombatState.Dormant;
    private string activeAttack = "None";
    private string damageState = "Normal";
    private readonly Queue<Attack> attackPlan = new Queue<Attack>();
    private bool attackHitPlayer, nodeStunQueued, wasPlayerAirborne;
    private float jumpPressure;
    private float previousHealthPercent = 1f;
    private bool retreatToPodQueued;

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (bossHealth == null) bossHealth = GetComponent<BossHealth>();
        if (healthBar == null) healthBar = GetComponent<BossHealthBar>();
        if (healthBar == null) healthBar = gameObject.AddComponent<BossHealthBar>();
        if (debugCanvas == null) debugCanvas = GetComponent<BossDebugCanvas>();
        if (debugCanvas == null) debugCanvas = gameObject.AddComponent<BossDebugCanvas>();
        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerHealth == null && player != null) playerHealth = player.GetComponent<PlayerHealth>();
        if (agent != null)
        {
            agent.speed = movementSpeed;
            agent.stoppingDistance = .2f;
            agent.isStopped = true;
            agent.updatePosition = true;
        }
        if (!PlaceBossOnNavMesh()) SnapBossToArenaFloor();
        SpawnPowerNodes();
        bossHealth?.SetVulnerable(false);
        SetIntensity(0f);
        UpdateDebug();
    }
    private void Update()
    {
        if (defeated || player == null) return;
        if (!awake) { if (Vector3.Distance(transform.position, player.position) <= awakenDistance) StartCoroutine(Awaken()); return; }
        UpdatePhase(bossHealth != null ? bossHealth.HealthPercent : 1f);
        TrackPlayerBehaviour();
        FacePlayer();
        if (!attacking && midpointQueued) StartCoroutine(MidpointTransition());
        else if (!attacking && Time.time >= nextAttack) StartCoroutine(DoAttack(ChooseAttack()));
    }
    public void OnBossDamaged(float healthPercent)
    {
        healthBar?.SetHealth(healthPercent);
        if (combatState == CombatState.Vulnerable && previousHealthPercent - healthPercent >= .06f)
            retreatToPodQueued = true;
        previousHealthPercent = healthPercent;
        UpdatePhase(healthPercent);
        if (healthPercent <= .5f && !midpointComplete)
        {
            midpointComplete = true;
            midpointQueued = true;
        }
        UpdateDebug();
    }
    private void UpdatePhase(float healthPercent)
    {
        int newPhase = healthPercent <= .33f ? 3 : healthPercent <= .66f ? 2 : 1;
        if (newPhase == phase) return;
        bool enteredPhaseTwo = newPhase == 2 && phase < 2;
        bool enteredPhaseThree = newPhase == 3 && phase < 3;
        phase = newPhase;
        patternStep = 0;
        attackPlan.Clear();
        SetIntensity(phase / 3f);
        if (enteredPhaseTwo)
        {
            forceDashPunch = true;
            nextAttack = Mathf.Min(nextAttack, Time.time + .35f);
        }
        if (enteredPhaseThree)
        {
            forceTeleport = true;
            nextAttack = Mathf.Min(nextAttack, Time.time + .35f);
        }
    }
    public void OnBossDefeated() { defeated = true; combatState = CombatState.Defeated; activeAttack = "Defeated"; damageState = "Defeated"; healthBar?.Hide(); if (agent != null) agent.isStopped = true; StopAllCoroutines(); SetIntensity(0f); UpdateDebug(); }
    private IEnumerator Awaken()
    {
        if (awake) yield break; awake = true; attacking = true; combatState = CombatState.Intro; activeAttack = "Awakening"; UpdateDebug();
        Debug.Log("[Boss] You shouldn't have come here");
        if (podGlass != null) podGlass.SetActive(false);
        if (hibernationPod != null) hibernationPod.SetActive(false);
        yield return new WaitForSeconds(introDuration);
        bossHealth?.SetVulnerable(true); healthBar?.Show(bossHealth != null ? bossHealth.HealthPercent : 1f); if (agent != null) agent.isStopped = false;
        attacking = false; combatState = CombatState.Dormant; activeAttack = "Opening Pattern"; nextAttack = Time.time + 1f; UpdateDebug();
    }
    private Attack ChooseAttack()
    {
        if (forceTeleport) { forceTeleport = false; attackPlan.Clear(); attackPlan.Enqueue(Attack.HomingMissile); return Attack.Teleport; }
        if (forceDashPunch) { forceDashPunch = false; attackPlan.Clear(); attackPlan.Enqueue(Attack.HomingMissile); return Attack.DashPunch; }
        if (attackPlan.Count == 0) BuildAttackPlan();
        Attack selected = attackPlan.Dequeue();
        lastAttack = selected;
        return selected;
    }
    private void BuildAttackPlan()
    {
        // Phase one teaches the two core threats. Missiles occupy two of every three slots,
        // while the laser remains a less frequent, clearly readable area-control attack.
        // Phase two and three add the movement attacks and book summon without reviving
        // retired projectile-ring or slam attacks.
        Attack[] pattern = phase == 1
            ? new[] { Attack.HomingMissile, Attack.HomingMissile, Attack.Laser }
            : new[] { Attack.HomingMissile, Attack.DashPunch, Attack.HomingMissile, Attack.Laser, Attack.Teleport, Attack.SummonDemonicBooks };
        attackPlan.Enqueue(pattern[patternStep % pattern.Length]);
        attackPlan.Enqueue(pattern[(patternStep + 1) % pattern.Length]);
        patternStep += 2;
    }
    private IEnumerator DoAttack(Attack attack)
    {
        attacking = true;
        attackHitPlayer = false;
        if (agent != null) { agent.isStopped = true; agent.updatePosition = false; }
        activeAttack = AttackName(attack);
        combatState = CombatState.Telegraph;
        damageState = "Armored";
        UpdateDebug();
        yield return new WaitForSeconds(telegraphDuration);
        combatState = CombatState.Attacking;
        UpdateDebug();
        if (attack == Attack.HomingMissile) yield return HomingMissile();
        if (attack == Attack.Spread) yield return Spread();
        if (attack == Attack.Laser) yield return Laser();
        if (attack == Attack.DashPunch) yield return DashPunch();
        if (attack == Attack.Slam) yield return Slam();
        if (attack == Attack.Teleport) yield return Teleport();
        if (attack == Attack.SummonDemonicBooks) yield return SummonDemonicBooks();
        if (nodeStunQueued) yield return PowerNodeStun();
        if (attack == Attack.Laser || ((attack == Attack.DashPunch || attack == Attack.Teleport) && !attackHitPlayer))
            yield return VulnerabilityWindow();
        bool continueCombo = attackPlan.Count > 0 && !defeated;
        if (continueCombo)
        {
            combatState = CombatState.Dormant;
            activeAttack = "Combo Continues";
            damageState = "Normal";
            UpdateDebug();
            yield return new WaitForSeconds(comboDelay);
        }
        else if (!defeated && retreatToPodQueued) yield return RetreatToPod();
        else if (!defeated) yield return RepositionAfterAttack();
        attacking = false;
        combatState = CombatState.Dormant;
        activeAttack = "Pattern Decision";
        damageState = "Normal";
        nextAttack = Time.time + (continueCombo ? .05f : attackCooldown * (phase == 3 ? .6f : phase == 2 ? .8f : 1f));
        UpdateDebug();
    }
    private IEnumerator RepositionAfterAttack()
    {
        combatState = CombatState.Repositioning;
        activeAttack = "Relocating";
        UpdateDebug();
        Vector2 circle = Random.insideUnitCircle;
        if (circle.sqrMagnitude < .01f) circle = Vector2.right;
        circle.Normalize();
        Vector3 target = player.position + new Vector3(circle.x, 0f, circle.y) * Random.Range(repositionMinimumDistance, repositionMaximumDistance);
        target.y = combatHeight;
        bool useAgent = EnableAgentAtCurrentPosition();
        if (useAgent && NavMesh.SamplePosition(target, out NavMeshHit hit, 8f, NavMesh.AllAreas))
            target = hit.position;
        else useAgent = false;

        float end = Time.time + Random.Range(1.35f, 1.9f);
        while (Time.time < end && Vector3.Distance(transform.position, target) > 1.2f)
        {
            if (useAgent)
            {
                agent.SetDestination(target);
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + .25f) break;
            }
            else transform.position = Vector3.MoveTowards(transform.position, target, movementSpeed * Time.deltaTime);
            yield return null;
        }
        if (agent != null && agent.enabled) agent.isStopped = true;
    }
    private IEnumerator RetreatToPod()
    {
        retreatToPodQueued = false;
        attackPlan.Clear();
        combatState = CombatState.Repositioning;
        activeAttack = "Retreating to Reactor";
        damageState = "Recalibrating";
        UpdateDebug();
        Vector3 target = hibernationPod != null ? hibernationPod.transform.position : transform.position - Direction() * 8f;
        target.y = combatHeight;
        float end = Time.time + 1.4f;
        while (Time.time < end && Vector3.Distance(transform.position, target) > 1.2f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, movementSpeed * 1.8f * Time.deltaTime);
            yield return null;
        }
        damageState = "Normal";
    }
    private IEnumerator VulnerabilityWindow()
    {
        combatState = CombatState.Vulnerable;
        activeAttack = "Recovery Window";
        damageState = "EXPOSED x" + vulnerabilityDamageMultiplier.ToString("0.0");
        bossHealth?.SetDamageMultiplier(vulnerabilityDamageMultiplier);
        UpdateDebug();
        yield return new WaitForSeconds(vulnerabilityDuration);
        bossHealth?.SetDamageMultiplier(1f);
        damageState = "Normal";
    }
    private IEnumerator MidpointTransition()
    {
        midpointQueued = false;
        attacking = true;
        if (agent != null) agent.isStopped = true;
        combatState = CombatState.Midpoint;
        activeAttack = "Reactor Overload";
        damageState = "Shielded";
        bossHealth?.SetDamageMultiplier(0f);
        SetIntensity(.85f);
        UpdateDebug();
        yield return new WaitForSeconds(1.2f);

        combatState = CombatState.Vulnerable;
        activeAttack = "Overload Recovery";
        damageState = "EXPOSED x" + vulnerabilityDamageMultiplier.ToString("0.0");
        bossHealth?.SetDamageMultiplier(vulnerabilityDamageMultiplier);
        UpdateDebug();
        yield return new WaitForSeconds(3f);

        bossHealth?.SetDamageMultiplier(1f);
        damageState = "Normal";
        SetIntensity(phase / 3f);
        attacking = false;
        combatState = CombatState.Dormant;
        activeAttack = "Escalated Pattern";
        nextAttack = Time.time + .75f;
        UpdateDebug();
    }
    private IEnumerator PowerNodeStun()
    {
        nodeStunQueued = false;
        combatState = CombatState.Vulnerable;
        activeAttack = "Power Node Feedback";
        damageState = "STUNNED x" + powerNodeDamageMultiplier.ToString("0.0");
        bossHealth?.SetDamageMultiplier(powerNodeDamageMultiplier);
        UpdateDebug();
        yield return new WaitForSeconds(powerNodeStunDuration);
        bossHealth?.SetDamageMultiplier(1f);
        damageState = "Normal";
    }
    public void OnPowerNodeOverloaded()
    {
        nodeStunQueued = true;
        activeAttack = "Power Node Overloaded";
        damageState = "Feedback Pending";
        UpdateDebug();
    }
    private void TrackPlayerBehaviour()
    {
        Collider playerCollider = player != null ? player.GetComponent<Collider>() : null;
        bool airborne = playerCollider != null && playerCollider.bounds.min.y > GroundHeight() + .8f;
        if (airborne && !wasPlayerAirborne) jumpPressure = Mathf.Min(3f, jumpPressure + 1f);
        wasPlayerAirborne = airborne;
        jumpPressure = Mathf.Max(0f, jumpPressure - Time.deltaTime * .18f);
    }
    private void SpawnPowerNodes()
    {
        if (powerNodeCount <= 0) return;
        float floorHeight = GroundHeight();
        for (int i = 0; i < powerNodeCount; i++)
        {
            float angle = 360f * i / powerNodeCount + 30f;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            BossPowerNode.Create(transform.position + direction * 10.5f + Vector3.up * (floorHeight - transform.position.y + 1.2f), this);
        }
    }
    private string AttackName(Attack attack)
    {
        switch (attack)
        {
            case Attack.HomingMissile: return "Homing Missile";
            case Attack.Spread: return "Projectile Ring + Laser";
            case Attack.Laser: return "360 Laser Sweep";
            case Attack.DashPunch: return "Zig-Zag Arm Punch";
            case Attack.Slam: return "Ground Shockwave";
            case Attack.Teleport: return "Teleport Punch";
            case Attack.SummonDemonicBooks: return "Summon Demonic Books";
            default: return attack.ToString();
        }
    }
    private void UpdateDebug()
    {
        debugCanvas?.SetStatus("Phase " + phase, combatState.ToString(), activeAttack, bossHealth != null ? bossHealth.HealthPercent : 1f, damageState);
    }
    private IEnumerator Spread()
    {
        yield return new WaitForSeconds(.45f);
        Vector3 origin = GetComponent<Collider>() != null
            ? GetComponent<Collider>().bounds.center
            : transform.position + Vector3.up * .7f;

        // Fire a complete ring around the boss. Projectile Count controls its density in the Inspector.
        for (int i = 0; i < projectileCount; i++)
        {
            float angle = 360f * i / projectileCount;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            BossProjectile.Create(origin, direction, projectileDamage, projectileSpeed);
        }
        activeAttack = "Projectile Ring + Laser";
        UpdateDebug();
        yield return new WaitForSeconds(.16f);
        yield return Laser();
    }
    private IEnumerator HomingMissile()
    {
        yield return new WaitForSeconds(.35f);
        float remainingInterval = nextHomingMissileLaunch - Time.time;
        if (remainingInterval > 0f) yield return new WaitForSeconds(remainingInterval);
        Vector3 origin = GetComponent<Collider>() != null
            ? GetComponent<Collider>().bounds.center
            : transform.position + Vector3.up;
        BossHomingMissile.Create(origin, player, missileDamage, missileSpeed, missileTurnRate, missileLifetime, missileExplosionRadius);
        nextHomingMissileLaunch = Time.time + homingMissileInterval;
        Debug.Log("[Boss] Homing missile launched");
    }
    private IEnumerator SummonDemonicBooks()
    {
        activeAttack = "Summoning Demonic Books";
        UpdateDebug();
        yield return new WaitForSeconds(.45f);

        if (demonicBookPool == null)
        {
            Debug.LogWarning("[Boss] No Demonic Book pool is assigned. Assign it on Boss Manager to enable summons.");
            yield break;
        }

        for (int i = 0; i < demonicBookSummonCount; i++)
        {
            Vector2 ringPoint = Random.insideUnitCircle;
            if (ringPoint.sqrMagnitude < .01f) ringPoint = Vector2.right;
            ringPoint.Normalize();
            float distance = Random.Range(demonicBookSummonRadius * .55f, demonicBookSummonRadius);
            Vector3 spawnPosition = player.position + new Vector3(ringPoint.x, 0f, ringPoint.y) * distance;
            if (NavMesh.SamplePosition(spawnPosition, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
                spawnPosition = navHit.position;
            else
                spawnPosition.y = GroundHeight() + .1f;

            Vector3 facing = player.position - spawnPosition;
            facing.y = 0f;
            Quaternion rotation = facing.sqrMagnitude > .01f ? Quaternion.LookRotation(facing) : Quaternion.identity;
            demonicBookPool.GetObject(spawnPosition, rotation);
            yield return new WaitForSeconds(demonicBookSummonInterval);
        }
    }
    private IEnumerator Laser()
    {
        GameObject beam = new GameObject("Boss Laser Sweep", typeof(LineRenderer));
        LineRenderer line = beam.GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = .45f;
        line.endWidth = .45f;
        line.numCapVertices = 8;
        line.textureMode = LineTextureMode.Stretch;
        line.material = CreateEffectMaterial(new Color(1f, .03f, .08f, 1f));
        line.startColor = new Color(1f, .1f, .14f, 1f);
        line.endColor = new Color(1f, .02f, .05f, 1f);
        activeAttack = "360 Laser Sweep";
        UpdateDebug();
        Debug.Log("[Boss] Laser sweep");
        Vector3 bossMiddle = GetComponent<Collider>() != null ? GetComponent<Collider>().bounds.center : transform.position;
        Vector3 sweepStartDirection = -transform.right;
        sweepStartDirection.y = 0f;
        if (sweepStartDirection.sqrMagnitude < .01f) sweepStartDirection = Vector3.left;
        sweepStartDirection.Normalize();
        float halfLength = laserLength * .5f;
        float t = 0; bool hit = false; while (t < laserSweepDuration) { t += Time.deltaTime; Vector3 forward = Quaternion.AngleAxis(Mathf.Lerp(0f, 360f, t / laserSweepDuration), Vector3.up) * sweepStartDirection; line.SetPosition(0, bossMiddle - forward * halfLength); line.SetPosition(1, bossMiddle + forward * halfLength); if (!hit && CanHitPlayerWithLaser(bossMiddle, forward, halfLength)) { Debug.Log("[Boss] Laser hit player"); Hurt(laserDamage, Vector3.up * 5); hit = true; } yield return null; } Destroy(beam);
    }
    private IEnumerator DashPunch()
    {
        yield return new WaitForSeconds(.35f);
        Vector3 forward = Direction();
        Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
        for (int turn = 0; turn < 5; turn++)
        {
            float sideOffset = turn % 2 == 0 ? 9f : -9f;
            float forwardOffset = Mathf.Lerp(8f, 2f, turn / 4f);
            yield return MoveDashSegment(player.position - forward * forwardOffset + side * sideOffset, .17f);
        }
        yield return MoveDashSegment(player.position - Direction() * 1.4f, .2f);
        if (Vector3.Distance(transform.position, player.position) < 4.2f)
            Hurt(punchDamage, Direction() * 10 + Vector3.up * 2);
        yield return new WaitForSeconds(.3f);
    }
    private IEnumerator MoveDashSegment(Vector3 target, float duration)
    {
        target.y = transform.position.y;
        float end = Time.time + duration;
        while (Time.time < end)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, 58f * Time.deltaTime);
            FacePlayer();
            yield return null;
        }
    }
    private IEnumerator Slam()
    {
        yield return new WaitForSeconds(.75f); GameObject wave = GameObject.CreatePrimitive(PrimitiveType.Cylinder); wave.name = "Boss Shockwave"; wave.transform.position = transform.position + Vector3.up * .04f; Destroy(wave.GetComponent<Collider>()); Colour(wave.GetComponent<Renderer>(), Color.red); float t = 0; bool hit = false; while (t < .7f) { t += Time.deltaTime; float radius = Mathf.Lerp(.5f, 12f, t / .7f); wave.transform.localScale = new Vector3(radius * 2, .04f, radius * 2); if (!hit && Vector3.Distance(transform.position, player.position) < radius + .8f) { Hurt(slamDamage, Direction() * 7 + Vector3.up * 15); hit = true; } yield return null; } Destroy(wave);
    }
    private IEnumerator Teleport()
    {
        Vector3 p = player.position + Random.insideUnitSphere.normalized * 8;
        p.y = combatHeight;
        if (NavMesh.SamplePosition(p, out NavMeshHit markerHit, 8, NavMesh.AllAreas)) p = markerHit.position;
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "Teleport Arrival Marker";
        marker.transform.position = new Vector3(p.x, GroundHeight() + .04f, p.z);
        marker.transform.localScale = new Vector3(2.2f, .03f, 2.2f);
        Destroy(marker.GetComponent<Collider>());
        Colour(marker.GetComponent<Renderer>(), new Color(1f, .55f, .02f));
        Visible(false);
        yield return new WaitForSeconds(.6f);
        Destroy(marker);
        if (agent != null && agent.enabled && NavMesh.SamplePosition(p, out NavMeshHit hit, 8, NavMesh.AllAreas))
        {
            agent.updatePosition = true;
            agent.Warp(hit.position);
            combatHeight = transform.position.y;
        }
        else transform.position = p;
        Visible(true);
        yield return new WaitForSeconds(.3f);

        // The brief wind-up gives the player time to leave the spot before the follow-up punch.
        yield return MoveDashSegment(player.position - Direction() * 1.25f, .22f);
        if (Vector3.Distance(transform.position, player.position) < 4f)
            Hurt(punchDamage * 1.1f, Direction() * 12f + Vector3.up * 2.5f);
        yield return new WaitForSeconds(.25f);
    }
    private Vector3 Direction() { Vector3 d = player.position - transform.position; d.y = 0; return d.sqrMagnitude < .01f ? transform.forward : d.normalized; }
    private float GroundHeight()
    {
        GameObject floor = GameObject.Find("Hex Lab Floor");
        Collider floorCollider = floor != null ? floor.GetComponent<Collider>() : null;
        // The player's Ground object moves with the player, so it cannot define laser height.
        return floorCollider != null ? floorCollider.bounds.max.y : combatHeight - .75f;
    }
    private void SnapBossToArenaFloor()
    {
        combatHeight = transform.position.y;
        GameObject floor = GameObject.Find("Hex Lab Floor");
        Collider floorCollider = floor != null ? floor.GetComponent<Collider>() : null;
        if (floorCollider == null) return;

        float lowestVisualPoint = float.PositiveInfinity;
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer is ParticleSystemRenderer) continue;
            lowestVisualPoint = Mathf.Min(lowestVisualPoint, renderer.bounds.min.y);
        }
        if (float.IsPositiveInfinity(lowestVisualPoint)) return;

        transform.position += Vector3.up * (floorCollider.bounds.max.y + .02f - lowestVisualPoint);
        combatHeight = transform.position.y;
    }
    private bool PlaceBossOnNavMesh()
    {
        if (agent == null || !agent.enabled || !NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 10f, NavMesh.AllAreas)) return false;
        if (!agent.Warp(hit.position)) return false;
        combatHeight = transform.position.y;
        return true;
    }
    private bool EnableAgentAtCurrentPosition()
    {
        if (agent == null || !agent.enabled || !agent.Warp(transform.position)) return false;
        agent.updatePosition = true;
        agent.isStopped = false;
        return agent.isOnNavMesh;
    }
    private Vector3 AimDirection(Vector3 origin) { Collider playerCollider = player != null ? player.GetComponent<Collider>() : null; Vector3 target = playerCollider != null ? playerCollider.bounds.center : player.position + Vector3.up * .8f; Vector3 d = target - origin; return d.sqrMagnitude < .01f ? transform.forward : d.normalized; }
    private void FacePlayer() { Vector3 d = Direction(); transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 8); }
    private void Hurt(float damage, Vector3 force) { if (playerHealth == null) return; attackHitPlayer = true; playerHealth.ManageHealth(-damage); PlayerMovement move = playerHealth.GetComponent<PlayerMovement>(); if (move != null) move.ApplyKnockback(force, .25f); }
    private void SetIntensity(float intensity) { Color color = Color.Lerp(new Color(.05f,.8f,1), new Color(1,.08f,.28f), intensity); foreach (Renderer r in arenaLights) Colour(r, color); for (int i=0;i<floatingFragments.Length;i++) if (floatingFragments[i] != null) floatingFragments[i].gameObject.SetActive(intensity > .2f + i * .08f); }
    private void Visible(bool visible) { foreach(Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = visible; }
    public static void Colour(Renderer r, Color c)
    {
        if (r == null) return;
        Material material = r.material;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", c);
        if (material.HasProperty("_Color")) material.color = c;
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", c * 2f);
        }
    }
    private static Material CreateEffectMaterial(Color colour)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", colour);
        if (material.HasProperty("_Color")) material.color = colour;
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", colour * 3f);
        }
        return material;
    }
    private bool CanHitPlayerWithLaser(Vector3 center, Vector3 forward, float halfLength)
    {
        Collider playerCollider = player != null ? player.GetComponent<Collider>() : null;
        if (playerCollider == null) return DistanceToLaserBar(player.position, center, forward, halfLength) < 1.4f;
        if (center.y < playerCollider.bounds.min.y - .2f || center.y > playerCollider.bounds.max.y + .2f) return false;
        float playerRadius = Mathf.Max(1.4f, new Vector2(playerCollider.bounds.extents.x, playerCollider.bounds.extents.z).magnitude + .5f);
        return DistanceToLaserBar(playerCollider.bounds.center, center, forward, halfLength) < playerRadius;
    }
    private static float DistanceToLaserBar(Vector3 point, Vector3 center, Vector3 forward, float halfLength)
    {
        point.y = center.y;
        forward.y = 0f;
        forward.Normalize();
        float distanceAlongBar = Mathf.Clamp(Vector3.Dot(point - center, forward), -halfLength, halfLength);
        return Vector3.Distance(point, center + forward * distanceAlongBar);
    }
}

public class BossProjectile : MonoBehaviour
{
    private Vector3 direction; private float damage, speed;
    public static void Create(Vector3 position, Vector3 direction, float damage, float speed) { GameObject o = GameObject.CreatePrimitive(PrimitiveType.Sphere); o.name = "Boss Energy Shot"; o.transform.position = position; o.transform.localScale = Vector3.one * .42f; o.GetComponent<Collider>().isTrigger = true; BossManager.Colour(o.GetComponent<Renderer>(), new Color(1,.1f,.35f)); BossProjectile p = o.AddComponent<BossProjectile>(); p.direction = direction; p.damage = damage; p.speed = speed; Destroy(o, 5); }
    private void Update() { transform.position += direction * speed * Time.deltaTime; }
    private void OnTriggerEnter(Collider other) { PlayerHealth h = other.GetComponent<PlayerHealth>(); if (h == null) return; h.ManageHealth(-damage); PlayerMovement m = h.GetComponent<PlayerMovement>(); if (m != null) m.ApplyKnockback(direction * 5 + Vector3.up * 2, .2f); Destroy(gameObject); }
}

public class BossHomingMissile : MonoBehaviour
{
    private Transform target;
    private float damage, speed, turnRate, lifetime, explosionRadius;
    private Vector3 direction;
    private float launchedAt;
    private bool exploded;

    public static void Create(Vector3 position, Transform target, float damage, float speed, float turnRate, float lifetime, float explosionRadius)
    {
        GameObject missile = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        missile.name = "Boss Homing Missile";
        missile.transform.position = position;
        missile.transform.localScale = new Vector3(.38f, .7f, .38f);
        missile.GetComponent<Collider>().isTrigger = true;
        BossManager.Colour(missile.GetComponent<Renderer>(), new Color(1f, .32f, .04f));

        BossHomingMissile controller = missile.AddComponent<BossHomingMissile>();
        controller.target = target;
        controller.damage = damage;
        controller.speed = speed;
        controller.turnRate = turnRate;
        controller.lifetime = lifetime;
        controller.explosionRadius = explosionRadius;
        controller.direction = target != null ? (target.position - position).normalized : Vector3.forward;
        controller.launchedAt = Time.time;
    }

    private void Update()
    {
        if (exploded) return;
        if (target != null)
        {
            Collider targetCollider = target.GetComponent<Collider>();
            Vector3 targetPoint = targetCollider != null ? targetCollider.bounds.center : target.position;
            Vector3 desiredDirection = targetPoint - transform.position;
            if (desiredDirection.sqrMagnitude > .01f)
                direction = Vector3.RotateTowards(direction, desiredDirection.normalized, turnRate * Mathf.Deg2Rad * Time.deltaTime, 0f);

            if (targetCollider != null && targetCollider.bounds.SqrDistance(transform.position) < .04f)
            {
                Explode(target.GetComponent<PlayerHealth>());
                return;
            }
        }

        Collider[] nearby = Physics.OverlapSphere(transform.position, .45f);
        foreach (Collider nearbyCollider in nearby)
        {
            BossPowerNode node = nearbyCollider.GetComponent<BossPowerNode>();
            if (node == null || node.IsOverloaded) continue;
            node.Overload();
            Explode(null);
            return;
        }

        transform.position += direction * speed * Time.deltaTime;
        if (direction.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(direction);
        if (Time.time >= launchedAt + lifetime) Explode(null);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (exploded) return;
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null) Explode(playerHealth);
    }

    private void Explode(PlayerHealth directHit)
    {
        if (exploded) return;
        exploded = true;
        if (directHit != null)
        {
            directHit.ManageHealth(-damage);
            PlayerMovement movement = directHit.GetComponent<PlayerMovement>();
            if (movement != null) movement.ApplyKnockback(direction * 9f + Vector3.up * 5f, .25f);
        }

        GameObject burst = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        burst.name = "Homing Missile Air Burst";
        burst.transform.position = transform.position;
        burst.transform.localScale = Vector3.one * explosionRadius * 2f;
        Collider burstCollider = burst.GetComponent<Collider>();
        if (burstCollider != null) Destroy(burstCollider);
        BossManager.Colour(burst.GetComponent<Renderer>(), new Color(1f, .16f, .02f));
        Destroy(burst, .18f);
        Destroy(gameObject);
    }
}

public class BossPowerNode : MonoBehaviour
{
    private BossManager boss;
    public bool IsOverloaded { get; private set; }

    public static void Create(Vector3 position, BossManager boss)
    {
        GameObject node = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        node.name = "Lab Power Node";
        node.transform.position = position;
        node.transform.localScale = new Vector3(.7f, 1.2f, .7f);
        BossManager.Colour(node.GetComponent<Renderer>(), new Color(.05f, .85f, 1f));
        BossPowerNode component = node.AddComponent<BossPowerNode>();
        component.boss = boss;
    }

    public void Overload()
    {
        if (IsOverloaded) return;
        IsOverloaded = true;
        BossManager.Colour(GetComponent<Renderer>(), new Color(1f, .12f, .02f));
        boss?.OnPowerNodeOverloaded();
        Destroy(gameObject, .5f);
    }
}
