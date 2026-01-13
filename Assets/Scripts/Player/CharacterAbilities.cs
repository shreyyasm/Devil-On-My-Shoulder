using QFSW.MOP2;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static UnityEngine.GraphicsBuffer;



public class CharacterAbilities : MonoBehaviour
{
    public enum AbilityType
    {
        Ability1,
        Ability2,
        Ability3,
        Ability4
    }

    [Header("Selected Ability")]
    public AbilityType Ability;

    [Header("Ability Character Data")]
    public CharacterData ability1Data;
    public CharacterData ability2Data;
    public CharacterData ability3Data;
    public CharacterData ability4Data;

    private CharacterData currentAbilityData;

    [Header("Runtime Values (Cached From Scriptable)")]
    private float abilityDuration;
    private float abilityRechargeTime;

    [Header("Runtime State")]
    [SerializeField] private float abilityTimer;
    [SerializeField] private float rechargeTimer;
    [SerializeField] private bool abilityActive;
    [SerializeField] private bool recharging;

    private bool abilityPressed;
    private Quaternion baseCameraRotation;

    [Header("Camera FOV")]
    [SerializeField] private Camera playerCam;
    [SerializeField] private float fovSmoothSpeed = 12f; // how fast it blends

    private float targetFOV;
    private bool fovTransitionActive;

    #region Ability1_Fields
    // ================= ABILITY 1 : DEVIL DUAL GUNS =================

    [Header("Ability 1 - Devil Dual Guns")]
    [SerializeField] private Transform leftGun;
    [SerializeField] private Transform rightGun;
    [SerializeField] private Animator flashLeft;
    [SerializeField] private Animator flashRight;
    [SerializeField] private Transform SocketPointLeft;
    [SerializeField] private Transform SocketPointRight;
    [SerializeField] private LayerMask enemyLayer;

    public ObjectPool BulletPool;

    [SerializeField] private float autoAimRange = 50f;
    [SerializeField] private float fireInterval = 0.18f;
    [SerializeField] private float projectileImpulse = 400f;
    [SerializeField] private float damageMultiplier = 1.2f;
    [SerializeField] private AudioClip audioClipFire;
    public AudioSource audioSource;

    //Ability 1 Settings

    [Header("Ability 1 Fire Rate Scaling")]
    [SerializeField] private float startFireInterval = 0.18f;
    [SerializeField] private float minFireInterval = 0.10f;

    public float currentFireInterval;


    [Header("Ability 1 Recoil")]
    [SerializeField] private Vector2 Recoil = new Vector2(1.5f, 0.6f);
    [SerializeField] private float recoilReturnSpeed = 10f;
    [SerializeField] private float recoilSnappiness = 15f;
    public Transform playerCamera;

    private Vector3 leftGunStartPos;
    private Vector3 rightGunStartPos;

    private bool fireLeftNext = true;
    private Coroutine ability1Routine;

    [Header("Ability 1 - Vision Settings")]
    [SerializeField] private float visionAngle = 45f; // total cone angle
    [SerializeField] private bool requireLineOfSight = true;
    [SerializeField] private Color visionGizmoColor = new Color(1, 0, 0, 0.25f);

    [Header("Ability 1 - Target UI")]
    [SerializeField] GameObject TargetIconCanvas;
    [SerializeField] private RectTransform targetIcon;
    [SerializeField] private Canvas targetCanvas;
    private Transform currentAbility1Target;

    [SerializeField] private float targetIconSmoothTime = 0.06f; // lower = faster
    private Vector3 targetIconVelocity;

    #endregion

    #region Ability2_Fields
    //Ability 2 Settings
    [Header("Ability 2 - Sutaa")]
    public bool Ability2_Active;
    [SerializeField] public float movementSpeedModifer;
    [SerializeField] public float slideSpeedModifer;
    [SerializeField] public float fireRateModifer;
    [SerializeField] public bool healthStable;
    #endregion

    #region Ability3_Fields
    [Header("Ability 3 - Shield Ricochet")]
    public GameObject shieldSaw;
    public GameObject shieldParent;
    [SerializeField] private float ricochetSpeed = 65f;
    [SerializeField] private float shieldRange = 50f;
    [SerializeField] private float ricochetDamage = 120f;
    [SerializeField] private float hitRadius = 1.2f;
    [SerializeField] private int maxRicochetCount = 5;
    [SerializeField] private float retargetDelay = 0.05f;
    public GameObject speedLines;

    [SerializeField] private LayerMask ricochetEnemyLayer;

    private Coroutine ability3Routine;
    private bool isRicocheting;

    [Header("Time Slow Settings")]
    [SerializeField] private float slowTimeScale = 0.15f;
    [SerializeField] private float slowDuration = 1f;
    [SerializeField] private Volume postProcessVolume;
    private MotionBlur motionBlur;
    private Coroutine timeRoutine;

    #endregion

    #region Ability4_Fields
    [Header("Ability 4 - Chainsaw Rush")]
    [SerializeField] private GameObject chainsawObject;
    [SerializeField] private float chainsawMoveSpeed = 25f;
    [SerializeField] private float chainsawFOV = 110f;
    [SerializeField] public bool Ability4_Active;
    private Rigidbody rb;
    public Transform orientation;

    #endregion

    // ===============================================================

    private void Awake()
    {
        ResolveAbilityData();
        if (postProcessVolume == null)
        {
            Debug.LogError("Post Process Volume not assigned!");
            return;
        }


        if (!postProcessVolume.profile.TryGet(out motionBlur))
        {
            Debug.LogError("Motion Blur not found in Volume Profile!");
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (playerCam == null)
            playerCam = GetComponentInChildren<Camera>();

        SetMotionBlur(false);
        CacheAbilityValues();

        if (leftGun != null)
        {
            leftGunStartPos = leftGun.localPosition;
            leftGun.gameObject.SetActive(false);
        }

        if (rightGun != null)
        {
            rightGunStartPos = rightGun.localPosition;
            rightGun.gameObject.SetActive(false);
        }
        TargetIconCanvas.SetActive(false);
    }

    private void Update()
    {
        HandleAbilityLogic();
        UpdateAbility1TargetUI();

        if (!fovTransitionActive)
            return;

        if (!fovTransitionActive) return;

        playerCam.fieldOfView = Mathf.Lerp(
            playerCam.fieldOfView,
            targetFOV,
            fovSmoothSpeed * Time.deltaTime
        );

        if (Mathf.Abs(playerCam.fieldOfView - targetFOV) < 0.05f)
        {
            playerCam.fieldOfView = targetFOV;
            fovTransitionActive = false;
        }

        if (abilityActive && Ability == AbilityType.Ability4)
        {
            Vector3 forwardMove = -orientation.transform.forward * chainsawMoveSpeed;
            rb.velocity = new Vector3(
                forwardMove.x,
                rb.velocity.y,
                forwardMove.z
            );
        }


    }
    private void LateUpdate()
    {
        // Smooth recoil return
        targetRecoil = Vector2.Lerp(
            targetRecoil,
            Vector2.zero,
            recoilReturnSpeed * Time.deltaTime
        );

        currentRecoil = Vector2.Lerp(
            currentRecoil,
            targetRecoil,
            recoilSnappiness * Time.deltaTime
        );

        // 🔹 Capture camera rotation AFTER mouse look / sway
        baseCameraRotation = playerCamera.localRotation;

        // 🔹 Add recoil as an offset (NOT overwrite)
        Quaternion recoilRotation = Quaternion.Euler(
            -currentRecoil.y,
            currentRecoil.x,
            0f
        );

        playerCamera.localRotation = baseCameraRotation * recoilRotation;
    }



    // ================= INPUT =================
    public void OnAbility(InputAction.CallbackContext context)
    {
        if (context.performed)
            abilityPressed = true;
    }


    // ================= DATA =================
    private void ResolveAbilityData()
    {
        switch (Ability)
        {
            case AbilityType.Ability1: currentAbilityData = ability1Data; break;
            case AbilityType.Ability2: currentAbilityData = ability2Data; break;
            case AbilityType.Ability3: currentAbilityData = ability3Data; break;
            case AbilityType.Ability4: currentAbilityData = ability4Data; break;
        }

        if (currentAbilityData == null)
            Debug.LogError($"Missing CharacterData for {Ability}", this);
    }

    private void CacheAbilityValues()
    {
        if (currentAbilityData == null) return;

        abilityDuration = currentAbilityData.duration;
        abilityRechargeTime = currentAbilityData.rechargeTime;

        abilityTimer = abilityDuration;
        rechargeTimer = abilityRechargeTime;
    }

    // ================= CORE =================
    private void HandleAbilityLogic()
    {
        if (abilityPressed)
        {
            abilityPressed = false;

            if (!abilityActive && !recharging)
                ActivateAbility();
        }

        // ACTIVE: duration ↓
        if (abilityActive)
        {
            abilityTimer -= Time.deltaTime;

            if (abilityTimer <= 0f)
                EndAbility();
        }

        // RECHARGE: cooldown ↑
        if (recharging)
        {
            rechargeTimer += Time.deltaTime;

            if (rechargeTimer >= abilityRechargeTime)
            {
                rechargeTimer = abilityRechargeTime;
                recharging = false;
            }
        }
    }

    private void ActivateAbility()
    {
        abilityActive = true;
        recharging = false;
        abilityTimer = abilityDuration;

        if (Ability == AbilityType.Ability1)
            StartAbility1();

        if (Ability == AbilityType.Ability2)
            StartAbility2();

        if (Ability == AbilityType.Ability3)
            StartAbility3();

        if (Ability == AbilityType.Ability4)
            StartAbility4();



        Debug.Log($"{Ability} Activated");
    }

    private void EndAbility()
    {
        abilityActive = false;
        recharging = true;
        rechargeTimer = 0f;

        if (Ability == AbilityType.Ability1)
            StopAbility1();

        if (Ability == AbilityType.Ability2)
            StopAbility2();

        if (Ability == AbilityType.Ability3)
            StopAbility3();

        if (Ability == AbilityType.Ability4)
            StopAbility4();



        Debug.Log($"{Ability} Ended");
    }
    public void SetTargetFOV(float newFOV, float transitionSpeed)
    {
        targetFOV = newFOV;
        fovSmoothSpeed = transitionSpeed;
        fovTransitionActive = true;
    }

    #region Ability1
    // ================= ABILITY 1 =================

    private void StartAbility1()
    {
        if (leftGun != null) leftGun.gameObject.SetActive(true);
        if (rightGun != null) rightGun.gameObject.SetActive(true);

        currentFireInterval = startFireInterval; // RESET fire rate
        ability1Routine = StartCoroutine(Ability1FireLoop());

        TargetIconCanvas.SetActive(true);
        SetTargetFOV(85f, 10f);

    }
    private IEnumerator Ability1FireLoop()
    {
        while (abilityActive)
        {
            UpdateFireRate();     // 🔥 UPDATE BASED ON TIMER
            FireAbility1Shot();

            yield return new WaitForSeconds(currentFireInterval);
        }
    }

    private void UpdateFireRate()
    {
        // Normalize ability time (1 → 0)
        float t = 1f - (abilityTimer / abilityDuration);

        // Lerp interval (slow → fast)
        currentFireInterval = Mathf.Lerp(
            startFireInterval,
            minFireInterval,
            t
        );
    }

    private void StopAbility1()
    {
        SetTargetFOV(90f, 10f);
        if (ability1Routine != null)
            StopCoroutine(ability1Routine);

        if (leftGun != null)
        {
            leftGun.localPosition = leftGunStartPos;
            leftGun.gameObject.SetActive(false);
        }

        if (rightGun != null)
        {
            rightGun.localPosition = rightGunStartPos;
            rightGun.gameObject.SetActive(false);
        }
        TargetIconCanvas.SetActive(false);
    }

    

    private void FireAbility1Shot()
    {
        Transform target = FindNearestEnemy();
        currentAbility1Target = target;

        if (target == null)
            return;

        bool fireLeft = fireLeftNext;

        Transform gun = fireLeft ? leftGun : rightGun;
        Transform socket = fireLeft ? SocketPointLeft : SocketPointRight;
        Animator flash = fireLeft ? flashLeft : flashRight;

        if (gun == null || socket == null) return;

        Vector3 dir = (target.position - socket.position).normalized;

        flash.SetTrigger("Flash");

        GameObject projectile =
            BulletPool.GetObject(socket.position, Quaternion.LookRotation(dir));

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        rb.velocity = dir * projectileImpulse;

        Projectile proj = projectile.GetComponent<Projectile>();
        if (proj != null)
            proj.SetDamage(proj.bulletDamage * damageMultiplier);

        ApplyRecoil(Recoil);
        audioSource.PlayOneShot(audioClipFire);
        // ✅ TOGGLE ONLY AFTER SUCCESSFUL FIRE
        fireLeftNext = !fireLeftNext;
    }


    private Transform FindNearestEnemy()
    {
        if (playerCamera == null) return null;

        Collider[] hits = Physics.OverlapSphere(
            playerCamera.position,
            autoAimRange,
            enemyLayer
        );

        float bestScore = float.MaxValue;
        Transform bestTarget = null;

        Vector3 camPos = playerCamera.position;
        Vector3 camForward = playerCamera.forward;

        foreach (Collider c in hits)
        {
            Vector3 toEnemy = (c.transform.position - camPos);
            float distance = toEnemy.magnitude;
            Vector3 dir = toEnemy.normalized;

            // 🔹 ANGLE CHECK (VISION CONE)
            float angle = Vector3.Angle(camForward, dir);
            if (angle > visionAngle * 0.5f)
                continue;

            // 🔹 LINE OF SIGHT CHECK
            if (requireLineOfSight)
            {
                if (Physics.Raycast(
                    camPos,
                    dir,
                    out RaycastHit hit,
                    autoAimRange
                ))
                {
                    if (hit.transform != c.transform)
                        continue; // blocked by wall or other object
                }
            }

            // 🔹 PICK NEAREST VALID
            if (distance < bestScore)
            {
                bestScore = distance;
                bestTarget = c.transform;
            }
        }

        return bestTarget;
    }

   
    private void UpdateAbility1TargetUI()
    {
        if (!abilityActive || Ability != AbilityType.Ability1 || currentAbility1Target == null)
        {
            targetIcon.gameObject.SetActive(false);
            return;
        }

        Camera cam = playerCamera.GetComponent<Camera>();

        Vector3 worldPos = GetTargetWorldPos(currentAbility1Target);
        Vector3 viewportPos = cam.WorldToViewportPoint(worldPos);

        // Off-screen or behind camera
        if (viewportPos.z <= 0f ||
            viewportPos.x < 0f || viewportPos.x > 1f ||
            viewportPos.y < 0f || viewportPos.y > 1f)
        {
            targetIcon.gameObject.SetActive(false);
            return;
        }

        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        targetIcon.gameObject.SetActive(true);

        // 🔥 Smooth but fast tracking
        targetIcon.position = Vector3.SmoothDamp(
            targetIcon.position,
            screenPos,
            ref targetIconVelocity,
            targetIconSmoothTime,
            Mathf.Infinity,
            Time.deltaTime
        );
    }

    private Vector3 GetTargetWorldPos(Transform enemy)
    {
        Renderer r = enemy.GetComponentInChildren<Renderer>();
        if (r != null)
            return r.bounds.center;

        return enemy.position;
    }


    private void OnDrawGizmosSelected()
    {
        if (playerCamera == null) return;

        Gizmos.color = visionGizmoColor;

        Vector3 camPos = playerCamera.position;
        Vector3 forward = playerCamera.forward;

        // Draw range
        Gizmos.DrawWireSphere(camPos, autoAimRange);

        // Draw cone edges
        Vector3 leftDir = Quaternion.Euler(0, -visionAngle * 0.5f, 0) * forward;
        Vector3 rightDir = Quaternion.Euler(0, visionAngle * 0.5f, 0) * forward;

        Gizmos.DrawRay(camPos, leftDir * autoAimRange);
        Gizmos.DrawRay(camPos, rightDir * autoAimRange);
    }

    // Recoil runtime
    private Vector2 currentRecoil;
    private Vector2 targetRecoil;
    private float laserTickTimer;
    private void ApplyRecoil(Vector2 recoil)
    {
        targetRecoil += new Vector2(
            UnityEngine.Random.Range(-recoil.x, recoil.x),
            recoil.y
        );
    }


    // ================= UI =================
    public float GetAbilityNormalized()
    {
        if (abilityActive)
            return abilityTimer / abilityDuration;

        if (recharging)
            return rechargeTimer / abilityRechargeTime;

        return 1f;
    }
    #endregion

    #region Ability2
    public void StartAbility2()
    {
        Ability2_Active = true;
        SetTargetFOV(120f, 10f);
        healthStable = true;
    }
    public void StopAbility2()
    {
        Ability2_Active = false;
        SetTargetFOV(90f, 10f);
        healthStable = false;
    }

    public float GetMovementSpeedModifier()
    {
        return Ability2_Active ? movementSpeedModifer : 1f;
    }

    public float GetSlideSpeedModifier()
    {
        return Ability2_Active ? slideSpeedModifer : 1f;
    }

    public float GetFireRateModifier()
    {
        return Ability2_Active ? fireRateModifer : 1f;
    }
    public bool GetHealthModifier()
    {
        return Ability2_Active && healthStable;
    }

    #endregion

    #region Ability3

    private void StartAbility3()
    {
        if (isRicocheting)
            return;

        shieldSaw.SetActive(true);
        shieldSaw.transform.SetParent(null);
 
        ability3Routine = StartCoroutine(ShieldThrowRoutine());


        DoTimeSlow();
        SetTargetFOV(120f, 10f);
        speedLines.SetActive(true);
        SetMotionBlur(true);
    }
    private void StopAbility3()
    {
        if (ability3Routine != null)
        {
            StopCoroutine(ability3Routine);
            ability3Routine = null;
        }

        isRicocheting = false;

        speedLines.SetActive(false);
        SetTargetFOV(90f, 10f);
        SetMotionBlur(false);

        // Shield reset
        shieldSaw.transform.SetParent(shieldParent.transform);
        shieldSaw.transform.localPosition = Vector3.zero;
        shieldSaw.SetActive(false);
    }


    private IEnumerator ShieldThrowRoutine()
    {
        isRicocheting = true;

        Vector3 dir = GetCrosshairDirection().normalized;
        float traveled = 0f;

        Transform firstHitEnemy = null;

        // Detect EVERYTHING
        int hitMask = ~0;

        while (traveled < shieldRange)
        {
            Vector3 step = dir * ricochetSpeed * Time.deltaTime;

            // 🔥 ONE SphereCast to rule them all
            if (Physics.SphereCast(
                shieldSaw.transform.position,
                hitRadius,
                dir,
                out RaycastHit hit,
                step.magnitude,
                hitMask,
                QueryTriggerInteraction.Ignore))
            {
                // ✅ ENEMY HIT
                Enemy enemy = hit.collider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.EnemyHit(enemy.maxHealth);
                    firstHitEnemy = enemy.transform;
                    break;
                }

                // ❌ HIT ANYTHING ELSE → RETURN
                yield return ReturnShieldToPlayer();
                yield break;
            }

            // No hit → move
            shieldSaw.transform.position += step;
            RotateShield(dir);

            traveled += step.magnitude;
            yield return null;
        }

        // ❌ No enemy hit at all
        if (firstHitEnemy == null)
        {
            yield return ReturnShieldToPlayer();
            yield break;
        }

        // ✅ Enemy hit → ricochet
        yield return RicochetLoop(firstHitEnemy);
    }



    private IEnumerator RicochetLoop(Transform firstTarget)
    {
        HashSet<Transform> hitEnemies = new HashSet<Transform>();
        Transform currentTarget = firstTarget;
        int hitCount = 1;

        hitEnemies.Add(currentTarget);
        DoTimeSlow();

        while (hitCount < maxRicochetCount)
        {
            Transform nextTarget = FindNextRicochetTarget(hitEnemies);
            if (nextTarget == null)
                break;

            Vector3 dir = (nextTarget.position - shieldSaw.transform.position).normalized;
            RotateShield(dir);
            yield return null;

            while (Vector3.Distance(shieldSaw.transform.position, nextTarget.position) > hitRadius)
            {
                shieldSaw.transform.position += dir * ricochetSpeed * Time.deltaTime;
                yield return null;
            }

            Enemy enemy = nextTarget.GetComponent<Enemy>();
            if (enemy != null)
                enemy.EnemyHit(enemy.maxHealth);

            hitEnemies.Add(nextTarget);
            hitCount++;

            yield return new WaitForSecondsRealtime(retargetDelay);
        }

        yield return ReturnShieldToPlayer();
    }


    private void RotateShield(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion lookRot = Quaternion.LookRotation(direction);

        // 🔧 Model alignment fix (adjust ONLY this if needed)
        Quaternion modelFix = Quaternion.Euler(0f, 0f, -90f);

        shieldSaw.transform.rotation = lookRot * modelFix;
    }


    private IEnumerator ReturnShieldToPlayer()
    {
        Transform player = shieldParent.transform;

        Vector3 dir = (player.position - shieldSaw.transform.position).normalized;
        RotateShield(-dir);

        while (Vector3.Distance(shieldSaw.transform.position, player.position) > hitRadius)
        {
            shieldSaw.transform.position += dir * ricochetSpeed * Time.deltaTime;
            RotateShield(dir);
            yield return null;
        }

        shieldSaw.transform.SetParent(shieldParent.transform);
        shieldSaw.transform.localPosition = Vector3.zero;
        shieldSaw.SetActive(false);

        StopAbility3();
    }


    private Transform FindNextRicochetTarget(HashSet<Transform> ignored)
    {
        Collider[] hits = Physics.OverlapSphere(
            shieldSaw.transform.position,
            autoAimRange,
            ricochetEnemyLayer
        );

        float bestDist = float.MaxValue;
        Transform best = null;

        foreach (Collider c in hits)
        {
            if (ignored.Contains(c.transform))
                continue;

            float d = Vector3.Distance(shieldSaw.transform.position, c.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = c.transform;
            }
        }

        return best;
    }
    private Vector3 GetCrosshairDirection()
    {
        return playerCamera.forward;
    }

    public void DoTimeSlow()
    {
        if (timeRoutine != null)
            StopCoroutine(timeRoutine);

        timeRoutine = StartCoroutine(TimeSlowCoroutine());
    }

    private IEnumerator TimeSlowCoroutine()
    {
        // Apply slow motion
        Time.timeScale = slowTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        Debug.Log("Smow-mo");

        // IMPORTANT: use unscaled time
        yield return new WaitForSecondsRealtime(slowDuration);
        speedLines.SetActive(false);

        // Restore normal time
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        timeRoutine = null;
    }
    public void SetMotionBlur(bool enabled)
    {
        if (motionBlur == null)
            return;

        motionBlur.active = enabled;
    }

    #endregion

    private void StartAbility4()
    {
        chainsawObject.SetActive(true);

        SetTargetFOV(chainsawFOV, 10f);
        SetMotionBlur(true);
        Ability4_Active = true;
        chainsawObject.SetActive(true);

     
    }

    private void StopAbility4()
    {
        chainsawObject.SetActive(false);

        SetTargetFOV(90f, 10);
        SetMotionBlur(false);

        Ability4_Active = false;
        chainsawObject.SetActive(false);
        // Stop forced movement
        rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
    }


}
