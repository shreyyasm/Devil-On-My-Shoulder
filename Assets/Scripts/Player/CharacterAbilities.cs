using QFSW.MOP2;
using System.Collections;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

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







    // ===============================================================

    private void Awake()
    {
        ResolveAbilityData();
    }

    private void Start()
    {
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
;
    }

    private Quaternion baseCameraRotation;

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

        Debug.Log($"{Ability} Activated");
    }

    private void EndAbility()
    {
        abilityActive = false;
        recharging = true;
        rechargeTimer = 0f;

        if (Ability == AbilityType.Ability1)
            StopAbility1();

        Debug.Log($"{Ability} Ended");
    }

    // ================= ABILITY 1 =================

    private void StartAbility1()
    {
        if (leftGun != null) leftGun.gameObject.SetActive(true);
        if (rightGun != null) rightGun.gameObject.SetActive(true);

        currentFireInterval = startFireInterval; // RESET fire rate
        ability1Routine = StartCoroutine(Ability1FireLoop());

        TargetIconCanvas.SetActive(true);
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
}
