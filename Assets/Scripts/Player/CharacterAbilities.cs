using InfimaGames.LowPolyShooterPack;
using QFSW.MOP2;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;



public class CharacterAbilities : MonoBehaviour
{
    public static CharacterAbilities instance;
    [Header("Kill Meter System")]
    [SerializeField] private float maxKillValue = 100f;
    [SerializeField] private float currentKillValue = 0f;
    [SerializeField] private Slider AbilitySlider;
    [SerializeField] private Image sliderFill;

    [SerializeField] private Image[] finisherBoxes; // size = 3 (30,60,90)

    [SerializeField] private float sliderSmoothSpeed = 8f;

    [SerializeField] private float breatheSpeed = 8f;
    [SerializeField] private float breatheAmount = 0.5f;


    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color glowColor = new Color(1f, 0.2f, 0.2f); // red glow

    [SerializeField] private float boxGlowSpeed = 6f;
    [SerializeField] private float barGlowSpeed = 14f; // 🔥 faster for full



    public bool abilityReady;
    public bool finisherReady;

    private float targetSliderValue;
    private bool canFinisherGlow;
    private bool isFullBreathe;

    [Header("Finisher Data")]
    [SerializeField] private float finisherRange = 6f;
    [SerializeField] private float finisherMoveSpeed = 25f;
    [SerializeField] float horizontalAngle = 60f;
    [SerializeField] float verticalAngle = 30f;
    [SerializeField] private Vector3 destinationOffset = Vector3.zero;
    public Transform rootCamera;
    public GameObject finisherCamera;
    public GameObject renderFinisherCanvas;
    public GameObject mainCameraTexture;
    public Transform finisherPosition;

    public GameObject playerRightHand;
    public GameObject playerLeftHand;
    public Character character;
    public bool reloadIncomplete;
    private bool isFinishing = false;

    // Finisher cost (you can tweak per enemy later)
    [SerializeField] private float finisherCost = 30f;

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

    [Header("Runtime State")]
    [SerializeField] private float abilityTimer;
    [SerializeField] private bool abilityActive;
    [SerializeField] private bool FinisherActive;

    private bool abilityPressed;
    private Quaternion baseCameraRotation;

    [Header("Camera FOV")]
    [SerializeField] private Camera playerCam;
    [SerializeField] private float fovSmoothSpeed = 12f; // how fast it blends

    private float targetFOV;
    private bool fovTransitionActive;
    private PlayerMovement playerMovement;

    #region Ability1_Fields
    // ================= ABILITY 1 : DEVIL DUAL GUNS =================

    [Header("Ability 1 - Devil Dual Guns")]
    [SerializeField] private Transform leftGun;
    [SerializeField] private Transform rightGun;
    [SerializeField] private Animator flashLeft;
    [SerializeField] private Animator flashRight;
    [SerializeField] private Transform SocketPointLeft;
    [SerializeField] private Transform SocketPointRight;
    public ParticleSystem leftGunFlash;
    public ParticleSystem rightGunFlash;
    public ParticleSystem leftGunSmoke;
    public ParticleSystem rightGunSmoke;
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
    [Header("Ability 4 - Inferno Room Lockdown")]
    public bool Ability4_Active;
    [SerializeField] private GameObject fireRoomPrefab;

    [SerializeField] private LayerMask roomLayer;

    private GameObject spawnedFire;
    private List<UnityEngine.AI.NavMeshAgent> frozenAgents = new List<UnityEngine.AI.NavMeshAgent>();

    private float currentY;
    private Rigidbody rb;
    public Transform orientation;

    #endregion

    // ===============================================================

    private void Awake()
    {
        instance = this;
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
        AbilitySlider.maxValue = maxKillValue;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerMovement = GetComponent<PlayerMovement>();

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

        // Force UI to white
        for (int i = 0; i < finisherBoxes.Length; i++)
        {
            finisherBoxes[i].color = normalColor;
        }

        sliderFill.color = normalColor;
    }
    private EnemyManager lastIndicatorEnemy;
    Transform target;
    private void Update()
    {


        if (Input.GetKeyDown(KeyCode.X) && !playerMovement.isDashing && playerMovement.grounded)
        {
            PerformFinisher();
        }
        //if (Input.GetKeyDown(KeyCode.M))
        //{
        //    ActivateAbility();
        //}


        HandleAbilityLogic();
        UpdateAbility1TargetUI();
        UpdateKillUI();
        //if (Input.GetKeyDown(KeyCode.M))
        //    AddKillValue(20);

        target = FindFinisherTarget();

        if (target != null && !playerMovement.isDashing && playerMovement.grounded && !playerMovement.finisherActive)
        {
            EnemyManager enemyManager = target.GetComponentInParent<EnemyManager>();

            if (enemyManager != lastIndicatorEnemy)
            {
                if (lastIndicatorEnemy != null)
                    lastIndicatorEnemy.finisherIndicator.SetActive(false);

                lastIndicatorEnemy = enemyManager;
            }

            if (lastIndicatorEnemy != null)
                lastIndicatorEnemy.finisherIndicator.SetActive(true);
        }
        else
        {
            if (lastIndicatorEnemy != null)
            {
                lastIndicatorEnemy.finisherIndicator.SetActive(false);
                lastIndicatorEnemy = null;
            }
        }

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

    public void AddKillValue(float amount)
    {
        currentKillValue += amount;
        currentKillValue = Mathf.Clamp(currentKillValue, 0f, maxKillValue);

        targetSliderValue = currentKillValue;

        CheckStates();
    }

    private void TryFinisher()
    {
        if (currentKillValue >= finisherCost)
        {
            currentKillValue -= finisherCost;
            targetSliderValue = currentKillValue;
            CheckStates();
            PerformFinisher();
            
        }
    }

    private void CheckStates()
    {
        abilityReady = currentKillValue >= maxKillValue;

        finisherReady = currentKillValue >= 30;
    }
    private void UpdateKillUI()
    {
        // -------- SMOOTH SLIDER --------
        AbilitySlider.value = Mathf.Lerp(
            AbilitySlider.value,
            targetSliderValue,
            Time.deltaTime * sliderSmoothSpeed
        );

        float[] thresholds = { 30f, 60f, 90f };

        // -------- FULL ABILITY (FAST GLOW) --------
        if (abilityReady)
        {
            float t = (Mathf.Sin(Time.time * barGlowSpeed) + 1f) * 0.5f;

            sliderFill.color = Color.Lerp(
                normalColor,
                glowColor,
                t * 0.7f
            );

            // turn OFF boxes
            for (int i = 0; i < finisherBoxes.Length; i++)
            {
                finisherBoxes[i].color = normalColor;
            }
            finisherReady = false;
            return;
        }

        // -------- NORMAL BAR --------
        sliderFill.color = normalColor;

        // -------- FINISHER BOXES --------
        for (int i = 0; i < finisherBoxes.Length; i++)
        {
            if (currentKillValue >= thresholds[i] && currentKillValue >= 30f)
            {
                float t = (Mathf.Sin(Time.time * boxGlowSpeed) + 1f) * 0.5f;

                finisherBoxes[i].color = Color.Lerp(
                    normalColor,
                    glowColor,
                    t
                );
               
            }
            else
            {
                finisherBoxes[i].color = normalColor;
               
            }
        }
    }

    private void PerformFinisher()
    {

      
        if (isFinishing) return;
        FinisherActive = true;
        Transform target = FindFinisherTarget();

        EnemyManager[] enemies =
    FindObjectsOfType<EnemyManager>();

        foreach (EnemyManager e in enemies)
        {
            if (e.canDash)
                e.StopDashImmediate();
        }
        if (target != null)
        {
            EnemyManager enemyManager = target.GetComponentInParent<EnemyManager>();
            enemyManager.EndDash(enemyManager.dashToken);

            EnemyHealth enemyHealth = target.GetComponentInParent<EnemyHealth>();

            enemyHealthTemp = enemyHealth;
            enemyHealth.FinisherActive();
            Debug.Log("Finisher performed");
            if(character.reloading)
            {
                reloadIncomplete = true;
            }
            StartCoroutine(FinisherRoutine(target));
            // TODO: enable finisher camera here
            finisherCamera.SetActive(true);
            renderFinisherCanvas.SetActive(true);
            finisherCamera.transform.rotation = Quaternion.identity;
            rootCamera.GetComponent<CameraLook>().enabled = false;

            playerLeftHand.SetActive(false);
            playerRightHand.SetActive(false);

            mainCameraTexture.SetActive(false);
            GetComponent<Character>().characterAnimator.SetBool("FinisherUnholstered", true);
            Debug.Log("working ha ji");
        }
        // TODO: implement finisher logic later
    }
    private EnemyHealth enemyHealthTemp;
    public Transform enemyTargetTemp;
    Transform FindFinisherTarget()
    {
        if (playerMovement.isDashing && !playerMovement.grounded)
            return null;

        Transform cam = Camera.main.transform;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            finisherRange,
            enemyLayer
        );

        Transform bestTarget = null;
        float closestDist = Mathf.Infinity;

        foreach (var hit in hits)
        {
            // Aim at enemy chest instead of feet
            Vector3 targetPos =
            hit.GetComponentInParent<EnemyManager>().finisherTarget.position;

            // Range check from player
            float dist = Vector3.Distance(
                transform.position,
                targetPos
            );

            float currentHorizontalAngle = horizontalAngle;
            float currentVerticalAngle = verticalAngle;

            // Relax the vision cone when enemy is very close
            if (dist <= 1.5f)
            {
                currentHorizontalAngle *= 2f;
                currentVerticalAngle *= 2f;
            }

            Vector3 dir = (targetPos - cam.position).normalized;

            // Convert to camera local space
            Vector3 localDir = cam.InverseTransformDirection(dir);

            // Horizontal angle (left/right)
            float horizontalAngleToTarget =
                Mathf.Abs(Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg);

            // Vertical angle (up/down)
            float verticalAngleToTarget =
                Mathf.Abs(Mathf.Atan2(localDir.y, localDir.z) * Mathf.Rad2Deg);

            bool insideHorizontal =
     horizontalAngleToTarget <= currentHorizontalAngle * 0.5f;

            bool insideVertical =
                verticalAngleToTarget <= currentVerticalAngle * 0.5f;

            if (insideHorizontal && insideVertical)
            {
                if (dist < closestDist)
                {
                    closestDist = dist;
                    bestTarget = hit.transform;
                }
            }
        }

        return bestTarget;
    }

    IEnumerator FinisherRoutine(Transform target)
    {
        isFinishing = true;
        playerMovement.finisherActive = true;
        playerMovement.CancelSlideImmediately();

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        EnemyManager enemyManager =
            target.GetComponentInParent<EnemyManager>();

        Transform finisherPoint =
            enemyManager.finisherTarget;

        float duration = 0.2f;
        float timer = 0f;

        Vector3 startPos = transform.position;

        Vector3 destination =
            finisherPoint.position +
            finisherPoint.right * destinationOffset.x +
            finisherPoint.forward * destinationOffset.z;

        // Match the finisher target's height
        destination.y = finisherPoint.position.y;

        // Move player to finisher position
        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.SmoothStep(
                0f,
                1f,
                timer / duration
            );

            destination =
                finisherPoint.position +
                finisherPoint.right * destinationOffset.x +
                finisherPoint.forward * destinationOffset.z;

            // Match the finisher target's height
            destination.y = finisherPoint.position.y - 3f;


            rootCamera.localRotation = Quaternion.Euler(0f,rootCamera.localEulerAngles.y,rootCamera.localEulerAngles.z);

            transform.position =
                Vector3.Lerp(
                    startPos,
                    destination,
                    t
                );

            Vector3 lookDir =
                enemyManager.transform.position -
                transform.position;

            lookDir.y = 0f;

            if (lookDir.sqrMagnitude > 0.001f)
            {
                transform.rotation =
                    Quaternion.LookRotation(
                        lookDir
                    );
            }

            yield return null;
        }

        if (!rb.isKinematic)
        {
            Rigidbody enemyRb = enemyManager.GetComponent<Rigidbody>();

            if (enemyRb != null)
            {
                enemyRb.velocity = Vector3.zero;
                enemyRb.angularVelocity = Vector3.zero;
            }
        }

        enemyHealthTemp.FinisherAnimation();

        string enemyName =
            enemyHealthTemp.GetComponent<EnemyManager>().enemyName;

        finisherCamera.GetComponent<Animator>()
            .SetTrigger(enemyName + "Finisher");

        SFXManager.Instance.PlaySFX(
            enemyManager.enemyName + "/Finisher",
            1f
        );

        // Lock player during finisher
        float finisherDuration = enemyManager.finisherTime;
        float finisherTimer = 0f;

        while (finisherTimer < finisherDuration)
        {
            destination =
                finisherPoint.position +
                finisherPoint.right * destinationOffset.x +
                finisherPoint.forward * destinationOffset.z;

            // Match the finisher target's height
            destination.y = finisherPoint.position.y - 3f;

            rootCamera.localRotation = Quaternion.Euler(0f, rootCamera.localEulerAngles.y, rootCamera.localEulerAngles.z);

            transform.position = destination;



            Vector3 lookDir =
                enemyManager.transform.position -
                transform.position;

            lookDir.y = 0f;

            if (lookDir.sqrMagnitude > 0.001f)
            {
                transform.rotation =
                    Quaternion.LookRotation(
                        lookDir
                    );
            }

            if (!rb.isKinematic)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            finisherTimer += Time.deltaTime;
            yield return null;
        }

        rootCamera.GetComponent<CameraLook>().enabled = true;
        finisherCamera.SetActive(false);
        renderFinisherCanvas.SetActive(false);
        mainCameraTexture.SetActive(true);

        playerMovement.finisherActive = false;
        isFinishing = false;

        GetComponent<Character>()
            .characterAnimator
            .SetBool("FinisherUnholstered", false);

        playerLeftHand.SetActive(true);
        playerRightHand.SetActive(true);

        rb.isKinematic = false;
        FinisherActive = false;
        if(reloadIncomplete)
        {
            character.PlayReloadAnimation();
          reloadIncomplete = false;

        }
        Debug.Log("Finisher End");
    }
    private void OnDrawGizmos()
    {
        Vector3 origin = transform.position;

        // Detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, finisherRange);

        Camera cam = Camera.main;

        if (cam == null)
            return;

        Transform camTransform = cam.transform;

        // Horizontal FOV lines
        Vector3 left =
            Quaternion.AngleAxis(-horizontalAngle * 0.5f, camTransform.up)
            * camTransform.forward;

        Vector3 right =
            Quaternion.AngleAxis(horizontalAngle * 0.5f, camTransform.up)
            * camTransform.forward;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(camTransform.position, camTransform.position + left * finisherRange);
        Gizmos.DrawLine(camTransform.position, camTransform.position + right * finisherRange);

        // Vertical FOV lines
        Vector3 up =
            Quaternion.AngleAxis(-verticalAngle * 0.5f, camTransform.right)
            * camTransform.forward;

        Vector3 down =
            Quaternion.AngleAxis(verticalAngle * 0.5f, camTransform.right)
            * camTransform.forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(camTransform.position, camTransform.position + up * finisherRange);
        Gizmos.DrawLine(camTransform.position, camTransform.position + down * finisherRange);

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            finisherRange,
            enemyLayer
        );

        foreach (var hit in hits)
        {
            Vector3 targetPos = hit.bounds.center;

            float dist = Vector3.Distance(
                camTransform.position,
                targetPos
            );

            bool validTarget = false;

            // Auto-accept close enemies
            if (dist < 1.5f)
            {
                validTarget = true;
            }
            else
            {
                Vector3 worldDir =
                    (targetPos - camTransform.position).normalized;

                Vector3 localDir =
                    camTransform.InverseTransformDirection(worldDir);

                float horizontal =
                    Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;

                float vertical =
                    Mathf.Atan2(localDir.y, localDir.z) * Mathf.Rad2Deg;

                validTarget =
                    Mathf.Abs(horizontal) <= horizontalAngle * 0.5f &&
                    Mathf.Abs(vertical) <= verticalAngle * 0.5f;
            }

            Gizmos.color = validTarget ? Color.green : Color.red;

            Gizmos.DrawLine(camTransform.position, targetPos);
            Gizmos.DrawSphere(targetPos, 0.2f);
        }
    }
    // ================= INPUT =================
    public void OnAbility(InputAction.CallbackContext context)
    {
        if (context.performed && abilityReady)
            abilityPressed = true;

        if (context.performed && finisherReady)
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
        abilityTimer = abilityDuration;

    }

    // ================= CORE =================
    private void HandleAbilityLogic()
    {
        // ACTIVE: duration ↓
        if (abilityActive)
        {
            abilityTimer -= Time.deltaTime;

            if (abilityTimer <= 0f)
                EndAbility();
        }

        if (abilityPressed && abilityReady)
        {
            abilityPressed = false;

            if (!abilityActive)
            {
                // 🔥 Ability requires FULL meter
                if (currentKillValue >= maxKillValue)
                {
                    currentKillValue = 0f; // consume all
                    targetSliderValue = currentKillValue;
                    ActivateAbility();
                    return;
                }

            }
        }
        if(abilityPressed && finisherReady)
        {
            abilityPressed = false;

            if (!FinisherActive)
            {
                TryFinisher();
               
            }
        }
       

       
    }

    private void ActivateAbility()
    {

        abilityActive = true;
      
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
        LeanTween.delayedCall(0.3f, () => { if (leftGun != null) leftGun.gameObject.SetActive(true); });

        if (rightGun != null) rightGun.gameObject.SetActive(true);

        currentFireInterval = startFireInterval; // RESET fire rate
        ability1Routine = StartCoroutine(Ability1FireLoop());

        TargetIconCanvas.SetActive(true);


    }
    private IEnumerator Ability1FireLoop()
    {
        yield return new WaitForSeconds(1f);
        SetTargetFOV(120f, 5f);
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

        // ❌ No enemy nearby -> don't shoot
        if (target == null)
            return;

        currentAbility1Target = target;

        bool fireLeft = fireLeftNext;

        Transform gun = fireLeft ? leftGun : rightGun;
        Transform socket = fireLeft ? SocketPointLeft : SocketPointRight;
        Animator flash = fireLeft ? flashLeft : flashRight;

        if (fireLeft)
        {
            leftGun.GetComponent<Animator>().SetTrigger("Fire");
            leftGunFlash.Play();
            leftGunSmoke.Play();
        }
        else
        {
            rightGun.GetComponent<Animator>().SetTrigger("Fire");
            rightGunFlash.Play();
            rightGunSmoke.Play();
        }

        if (gun == null || socket == null)
            return;

        // Always shoot at the locked target
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
                EnemyHealth enemy = hit.collider.GetComponent<EnemyHealth>();
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

            EnemyHealth enemy = nextTarget.GetComponent<EnemyHealth>();
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

        while (Vector3.Distance(shieldSaw.transform.position, player.position) > hitRadius)
        {
            // 🔄 Recalculate direction every frame
            Vector3 dir = (player.position - shieldSaw.transform.position).normalized;

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

    #region Ability4
    private Bounds detectedRoomBounds;
    private bool roomDetected;
    private RoomVolume currentRoom;
    private void StartAbility4()
    {
        Ability4_Active = true;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            8f,
            roomLayer
        );

        if (hits.Length == 0)
        {
            Debug.LogWarning("No room colliders detected.");
            return;
        }

        // Collect unique RoomVolume parents
        HashSet<RoomVolume> foundRooms = new HashSet<RoomVolume>();

        foreach (Collider col in hits)
        {
            RoomVolume room = col.GetComponentInParent<RoomVolume>();
            if (room != null)
                foundRooms.Add(room);
        }

        if (foundRooms.Count == 0)
        {
            Debug.LogWarning("No RoomVolume found in parents.");
            return;
        }

        // Select the room that contains the player
        RoomVolume selectedRoom = null;

        foreach (RoomVolume room in foundRooms)
        {
            Bounds b = room.GetCombinedBounds();
            if (b.Contains(transform.position))
            {
                selectedRoom = room;
                break;
            }
        }

        // Fallback to closest room if needed
        if (selectedRoom == null)
        {
            float closest = float.MaxValue;

            foreach (RoomVolume room in foundRooms)
            {
                float dist = Vector3.Distance(
                    transform.position,
                    room.transform.position
                );

                if (dist < closest)
                {
                    closest = dist;
                    selectedRoom = room;
                }
            }
        }

        if (selectedRoom == null)
            return;

        detectedRoomBounds = selectedRoom.GetCombinedBounds();
        roomDetected = true;

        // 🔥 Spawn fire
        spawnedFire = new GameObject("RoomFire");
        FireRoomSpawner spawner = spawnedFire.AddComponent<FireRoomSpawner>();
        spawner.fireTilePrefab = fireRoomPrefab;
        spawner.groundLayer = LayerMask.GetMask("Ground");
        spawner.SpawnFireInRoom(detectedRoomBounds);

        // ❄ Freeze enemies inside FULL room bounds
        Collider[] enemies = Physics.OverlapBox(
            detectedRoomBounds.center,
            detectedRoomBounds.extents,
            Quaternion.identity,
            enemyLayer
        );

        foreach (Collider col in enemies)
        {
            UnityEngine.AI.NavMeshAgent agent =
                col.GetComponent<UnityEngine.AI.NavMeshAgent>();

            if (agent != null && !frozenAgents.Contains(agent))
            {
                agent.isStopped = true;
                frozenAgents.Add(agent);
            }
        }

        StartCoroutine(Ability4DurationRoutine());
    }
    private void StopAbility4()
    {
        Ability4_Active = false;

        // Unfreeze enemies
        foreach (var agent in frozenAgents)
        {
            if (agent != null)
                agent.isStopped = false;
        }

        frozenAgents.Clear();

        // Destroy fire
        if (spawnedFire != null)
            Destroy(spawnedFire);

        roomDetected = false;
    }
    private void OnTriggerEnter(Collider other)
    {
        RoomVolume room = other.GetComponent<RoomVolume>();
        if (room != null)
            currentRoom = room;
    }

    private void OnTriggerExit(Collider other)
    {
        RoomVolume room = other.GetComponent<RoomVolume>();
        if (room != null && currentRoom == room)
            currentRoom = null;
    }
    private IEnumerator Ability4DurationRoutine()
    {
        yield return new WaitForSeconds(abilityDuration);
        StopAbility4();
    }
    //private void OnDrawGizmos()
    //{
    //    // 🔵 Detection Sphere
    //    Gizmos.color = Color.blue;
    //    Gizmos.DrawWireSphere(transform.position, 8f);

    //    if (!roomDetected)
    //        return;

    //    // 🟢 Selected Room Bounds
    //    Gizmos.color = Color.green;
    //    Gizmos.DrawWireCube(
    //        detectedRoomBounds.center,
    //        detectedRoomBounds.size
    //    );

    //    // 🟡 Enemy Freeze Box (same as OverlapBox) 
    //    Gizmos.color = Color.yellow;
    //    Gizmos.DrawWireCube(
    //        detectedRoomBounds.center,
    //        detectedRoomBounds.size
    //    );
    //}
    #endregion
}