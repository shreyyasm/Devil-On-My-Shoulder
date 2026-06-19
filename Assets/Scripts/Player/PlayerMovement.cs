// Some stupid rigidbody based movement by Dani

using InfimaGames.LowPolyShooterPack;
using QFSW.MOP2;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Analytics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.VFX;
using static UnityEngine.EventSystems.EventTrigger;
using Random = UnityEngine.Random;

public class PlayerMovement : MonoBehaviour 
{

    public static PlayerMovement Instance;
   
    //Assingables
    public Transform playerCam;
    public Transform orientation;

    //Other
    private Rigidbody rb;

    [Header("Movement Settings")]
    public int characterindex = 1;
    public float moveSpeed = 4500;
    public float maxSpeed = 20;
    public bool grounded;
    public LayerMask whatIsGround;   
    public float counterMovement = 0.175f;
    private float threshold = 0.01f;
    public float maxSlopeAngle = 35f;
    public bool playerMoving;
    public float itemUseMovementSpeed = 1f;

    [Header("Momentum Settings")]
    public float airAcceleration = 2500f;
    public float airControlMultiplier = 0.9f;
    public float jumpMomentumBoost = 1.05f; // slight speed gain
    public float slopeJumpBoost = 1.15f;
    private float lastJumpTime;



    [Header("Jump Settings")]
    public float jumpForce = 550f;
    private bool readyToJump = true;
    public float jumpCooldown = 0.25f;
    private bool jumpPressed = false;
    private bool slidePressed = false;


    public AudioClip jumpSFX;
    public AudioSource audioSource;

    [Header("Dash Settings")]
    public float dashForce = 15f;
    public float dashDuration = 0.5f;
    public float dashCooldown = 1f;
    public float dashHeight = 0.5f; // target height when sliding
    public bool isDashing = false;
    public bool isSliding = false;
    public bool readyToDash = true;

    //Slide
    private Vector3 crouchScale = new Vector3(1, 0.5f, 1);
    private Vector3 playerScale;
    public float dashCounterMovement = 0.2f;

    // For smooth height change
    private float targetHeight;
    private float heightSmoothSpeed = 10f;
    private float originalHeight;

    public AudioClip slideSFX;
    public Animator anim;
    public GameObject speedline;


    [Header("Kick Settings")]
    public float kickCooldown = 1.0f;
    public float baseKickForce = 12f;
    public float dashKickMultiplier = 1.6f;
    public float airKickMultiplier = 1.4f;
    public float velocityKickMultiplier = 0.05f;
    public float kickUpwardForce = 2.5f;

    [SerializeField] private float detectionRadius = 3f;
    [SerializeField] private float viewAngle = 60f;


    public List<Transform> legKickPoint; // empty object near foot
    public float kickRadius = 0.6f;
    public LayerMask enemyLayer;

    public bool canKick = true;
    private bool kickPressed;
    public List<Animator> kickAnimator;

    [Header("Kick NavMesh Knockback")]
    [SerializeField] private float kickGravity = 30f;
    [SerializeField] private float kickMaxAirTime = 1.2f;

    [SerializeField] private float enemySlideDuration = 0.4f;
    [SerializeField] private float enemySlideDamping = 8f;

    [SerializeField] private float enemyGroundCheckDistance = 2.5f;
    [SerializeField] private float enemyGroundCheckRadius = 0.35f;
    public GameObject KickVFX;



    [Header("Kick Camera Tilt")]
    [SerializeField] private float kickTiltAngle = 12f;
    [SerializeField] private float kickReturnSpeed = 25f;
    private float currentKickTilt;
    private float targetKickTilt;



    [Header("Camera Slide FOV")]
    public float slideFOV = 80f;      // FOV during slide
    public float normalFOV = 60f;     // Default FOV
    public float fovSmoothSpeed = 5f; // Smooth speed


    [Header("Footstep Settings")]
    public float runStepInterval = 0.3f;

    private float stepTimer = 0f;
    private string currentFloorType = "Default";

    [Header("Camera Tilt Sway")]
    public float swayTiltX = 5f; // Tilt left/right when strafing
    public float swayTiltY = 2f; // Tilt forward/backward
    public float swaySmooth = 4f;

    private Vector3 initialCamRot;

    public Camera playerCameraMain;


    [Header("Others")]
    public bool mainMenu = false;
    public bool mainOpen = false;
    float x, y;
    bool jumping, sprinting, crouching;

    private Vector3 normalVector = Vector3.up;
    private Vector3 wallNormalVector;

    public Slider HealthSlider;
    public Slider AgilitySlider;
    public Slider DamageSlider;
    [HideInInspector]
    public CharacterAbilities characterAbilities;

   
    public void LoadSliderPlayerData()
    {
        //HealthSlider.value = playerHealth.maxHealth;
        //AgilitySlider.value = moveSpeed;
        //DamageSlider.value = 50;
    }
    void Awake() {
        rb = GetComponent<Rigidbody>();
        characterAbilities = GetComponent<CharacterAbilities>();    
        if(Instance == null)
            Instance = this;
        targetHeight = 1.4f;

        Shader.Find("Hidden/ImpactFrame");
        baseRotation = hands.transform.localRotation;


       
    }

    void Start()
    {
        ApplyCharacterData();
        playerScale = transform.localScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerCam != null)
            initialCamRot = playerCameraMain.transform.localEulerAngles;

        originalRootLocalPos = rootGameObject.transform.localPosition;
        rootVisualY = rootGameObject.transform.localPosition.y;
    }

    private void ApplyCharacterData()
    {
        characterindex = PlayerCharacterData.Instance.characterIndex;

        // Movement
        moveSpeed = PlayerCharacterData.Instance.moveSpeed;

        // Jump
        jumpForce = PlayerCharacterData.Instance.jumpForce;

        // Slide
        dashForce = PlayerCharacterData.Instance.slideForce;

        //Health
        playerHealth.maxHealth = PlayerCharacterData.Instance.maxHealth;

       
        LoadSliderPlayerData();
        //RefreshLeg();


    }

    private void FixedUpdate() {
        Movement();
    }
    [HideInInspector]
    public Vector2 moveInput = Vector2.zero;   // x = horizontal, y = vertical
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnKick(InputAction.CallbackContext context)
    {
        if (context.performed)
            kickPressed = true;
    }

    // Jump: Button (performed = pressed)
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
            jumpPressed = true;
        else if (context.canceled)
            jumpPressed = false; // optional — we use performed to trigger
    }
    // Slide: button (we use performed to trigger a slide)
    public void OnSlide(InputAction.CallbackContext context)
    {
        if (context.performed)
            slidePressed = true;
    }
    private bool IsForwardInput()
    {
        return y > 0.1f;
    }



    private bool HasMovementInput()
    {
        return Mathf.Abs(x) > 0.1f || Mathf.Abs(y) > 0.1f;
    }

    private void Update() {
 
        MyInput();
        CameraTiltSway();
        HandleFootsteps();

        if (Input.GetKeyDown(KeyCode.T))
        {
            GetRandomItem();
        }

        //if (kickPressed)
        //{
        //    TryKick();
        //    kickPressed = false;
        //}

        // Smoothly adjust height
        Vector3 scale = transform.localScale;
        scale.y = Mathf.Lerp(scale.y, targetHeight, Time.deltaTime * heightSmoothSpeed);
        transform.localScale = scale;

        float targetFOV = isDashing ? slideFOV : normalFOV;

        if (Mathf.Abs(playerCameraMain.fieldOfView - targetFOV) > 0.05f)
        {
            playerCameraMain.fieldOfView = Mathf.Lerp(
                playerCameraMain.fieldOfView,
                targetFOV,
                Time.deltaTime * fovSmoothSpeed
            );
        }
        if (grounded && !isDashing)
        {
            standHeightY = rootGameObject.transform.position.y;
        }

        if (itemPressed)
        {
            UseOrThrowItem();
            itemPressed = false;
        }


    }
    private Quaternion baseRotation;
    void LateUpdate()
    {
        currentKickTilt = Mathf.Lerp(
            currentKickTilt,
            targetKickTilt,
            Time.deltaTime * kickReturnSpeed
        );

        Quaternion kickRotation = Quaternion.Euler(0f, 0f, -currentKickTilt);

        hands.transform.localRotation = baseRotation * kickRotation;
    }

    /// <summary>
    /// Find user input. Should put this in its own class but im lazy
    /// </summary>
    public PlayerHealth playerHealth;
    private void MyInput()
    {
        if (finisherActive) return;

        if (mainOpen) return;
        y = moveInput.y;

        if (mainMenu) return;
        x = moveInput.x;

        playerMoving = moveInput.magnitude > 0;

        jumping = jumpPressed;
        if (slidePressed)
        {
            if (readyToDash && HasMovementInput())
            {
                HandleDashOrSlide();
            }
            if(!HasMovementInput() && readyToDash)
            {
                TryKick();
            }

            // ❌ ALWAYS consume the input
            slidePressed = false;
        }



    }

    private bool isKnocked = false;
    public bool finisherActive = false;
    public void ApplyKnockback(Vector3 force, float duration)
    {
        StartCoroutine(KnockbackRoutine(force, duration));
    }

    private IEnumerator KnockbackRoutine(Vector3 force, float duration)
    {
        isKnocked = true;

        rb.velocity = Vector3.zero;
        rb.AddForce(force, ForceMode.Impulse);

        yield return new WaitForSeconds(duration);

        isKnocked = false;
    }
    private void Movement() {

        if (isKnocked) return;
        if (finisherActive) return;
        //If holding jump && ready to jump, then jump
        if (readyToJump && jumping) Jump();
        if (isDashing) return;
        //Extra gravity
        rb.AddForce(Vector3.down * Time.deltaTime * 20);
        
        //Find actual velocity relative to where player is looking
        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x, yMag = mag.y;

        //Counteract sliding and sloppy movement
        CounterMovement(x, y, mag);
        
        

        //Set max speed
        float maxSpeed = this.maxSpeed;
        
        //If sliding down a ramp, add force down so player stays grounded and also builds speed
        if (crouching && grounded && readyToJump) {
            rb.AddForce(Vector3.down * Time.deltaTime * 3000);
            return;
        }
        
        //If speed is larger than maxspeed, cancel out the input so you don't go over max speed
        if (x > 0 && xMag > maxSpeed) x = 0;
        if (x < 0 && xMag < -maxSpeed) x = 0;
        if (y > 0 && yMag > maxSpeed) y = 0;
        if (y < 0 && yMag < -maxSpeed) y = 0;

        //Some multipliers
        float multiplier = 1f, multiplierV = 1f;

        if (!grounded)
        {
            Vector3 wishDir =
                orientation.forward * y +
                orientation.right * x;

            if (wishDir.magnitude > 0.1f)
            {
                rb.AddForce(
                    wishDir.normalized * airAcceleration * Time.deltaTime,
                    ForceMode.Acceleration
                );
            }

            multiplier = airControlMultiplier;
            multiplierV = airControlMultiplier;
        }



        // Movement while sliding
        if (grounded && crouching) multiplierV = 0f;

        //Apply forces to move player
        rb.AddForce(orientation.transform.forward * y * moveSpeed* itemUseMovementSpeed * characterAbilities.GetMovementSpeedModifier() * Time.deltaTime * multiplier * multiplierV);
        rb.AddForce(orientation.transform.right * x * moveSpeed * itemUseMovementSpeed * characterAbilities.GetMovementSpeedModifier() * Time.deltaTime * multiplier );

      
    }

    private void Jump()
    {
        if (finisherActive) return;
        if (!grounded || !readyToJump)
            return;

        // 🚨 CANCEL SLIDE FIRST
        if(isDashing)
            kickAnimator[characterindex - 1].Play("Slide", 0, 0.75f);
        CancelSlideImmediately();

        readyToJump = false;

        audioSource.PlayOneShot(jumpSFX, 0.7f);

        Vector3 horizontalVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.velocity = horizontalVel * jumpMomentumBoost;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);


        // 🎥 LOCK CAMERA FOR JUMP
        cameraLocked = true;

        // stop any previous camera lerp
        if (cameralerpRoutine != null)
        {
            StopCoroutine(cameralerpRoutine);
            cameralerpRoutine = null;
        }

        Vector3 jumpTilt = Vector3.zero;

        // Determine dominant axis
        if (Mathf.Abs(x) > Mathf.Abs(y))
        {
            // LEFT / RIGHT
            if (x < -0.1f)
            {
                jumpTilt = new Vector3(0f, 0f, -6f);
                targetKickTilt = -6;
            }
               
            else if (x > 0.1f)
            {
                jumpTilt = new Vector3(0f, 0f, 6f);
                targetKickTilt = 6;
            }
           
        }
        else
        {
            // FORWARD / BACK
            if (y > 0.1f)
                jumpTilt = new Vector3(8f, 0f, 0f);
            else if (y < -0.1f)
            {
                jumpTilt = new Vector3(-8f, 0f, 0f);
                StartCoroutine(
                LerpCameraZPosition(-0.1f, 0.5f, 8f, 0f));

            }
           

        }

        if (jumpTilt != Vector3.zero)
        {
            cameralerpRoutine = StartCoroutine(
                LerpCameraRotation(jumpTilt, 0.8f, 80f, 0f)
            );
        }
        else
        {
            cameraLocked = false;
        }

        lastJumpTime = Time.time;
        Invoke(nameof(ResetJump), jumpCooldown);
    }



    private void ResetJump()
    {
        readyToJump = true;
        //targetKickTilt = 0;
    }


    private void CounterMovement(float x, float y, Vector2 mag) {
        if (!grounded || jumping || Time.time - lastJumpTime < 0.15f)
            return;

        //Slow down sliding
        if (crouching) {
            rb.AddForce(moveSpeed * Time.deltaTime * -rb.velocity.normalized * dashCounterMovement);
            return;
        }

        //Counter movement
        if (Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0)) {
            rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * -mag.x * counterMovement);
        }
        if (Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0)) {
            rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * -mag.y * counterMovement);
        }
        
        //Limit diagonal running. This will also cause a full stop if sliding fast and un-crouching, so not optimal.
        if (Mathf.Sqrt((Mathf.Pow(rb.velocity.x, 2) + Mathf.Pow(rb.velocity.z, 2))) > maxSpeed) {
            float fallspeed = rb.velocity.y;
            Vector3 n = rb.velocity.normalized * maxSpeed;
            rb.velocity = new Vector3(n.x, fallspeed, n.z);
        }
    }

    /// <summary>
    /// Find the velocity relative to where the player is looking
    /// Useful for vectors calculations regarding movement and limiting movement
    /// </summary>
    /// <returns></returns>
    public Vector2 FindVelRelativeToLook() {
        float lookAngle = orientation.transform.eulerAngles.y;
        float moveAngle = Mathf.Atan2(rb.velocity.x, rb.velocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float magnitue = rb.velocity.magnitude;
        float yMag = magnitue * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitue * Mathf.Cos(v * Mathf.Deg2Rad);
        
        return new Vector2(xMag, yMag);
    }

    private bool IsFloor(Vector3 v) {
        float angle = Vector3.Angle(Vector3.up, v);
        return angle < maxSlopeAngle;
    }

    private bool cancellingGrounded;
    
    /// <summary>
    /// Handle ground detection
    /// </summary>
    private void OnCollisionStay(Collision other) {
        //Make sure we are only checking for walkable layers
        int layer = other.gameObject.layer;
        if (whatIsGround != (whatIsGround | (1 << layer))) return;

        //Iterate through every collision in a physics update
        for (int i = 0; i < other.contactCount; i++) {
            Vector3 normal = other.contacts[i].normal;
            //FLOOR
            if (IsFloor(normal)) {
                grounded = true;
                cancellingGrounded = false;
                normalVector = normal;
                CancelInvoke(nameof(StopGrounded));
            }
        }

        //Invoke ground/wall cancel, since we can't check normals with CollisionExit
        float delay = 3f;
        if (!cancellingGrounded) {
            cancellingGrounded = true;
            Invoke(nameof(StopGrounded), Time.deltaTime * delay);
        }
    }

    private void StopGrounded() {
        grounded = false;
    }
    public ObjectPool explosionPrefab;
    public AudioClip explosionSound;
    public GameObject hands;
    public void ExplodeItself()
    {
        audioSource.PlayOneShot(explosionSound, 1);
        explosionPrefab.GetObject().transform.position = transform.position;
        gameObject.GetComponent<PlayerMovement>().enabled = false;
        gameObject.GetComponent<Character>().enabled = false;
        gameObject.GetComponent<Movement>().enabled = false;
        //gameObject.GetComponent<PlayerInput>().enabled = false;
        gameObject.GetComponent<CapsuleCollider>().providesContacts = false;
        hands.SetActive(false);
    }

    private void CameraTiltSway()
    {
        if (playerCameraMain == null) return;
        if (cameraLocked) return; // 🚨 STOP sway during slide

        float tiltZ = -x * swayTiltX;
        float tiltX = y * swayTiltY;

        Quaternion targetRot = Quaternion.Euler(
            initialCamRot.x + tiltX,
            initialCamRot.y,
            initialCamRot.z + tiltZ
        );

        playerCameraMain.transform.localRotation =
            Quaternion.Lerp(
                playerCameraMain.transform.localRotation,
                targetRot,
                Time.deltaTime * swaySmooth
            );
    }
   

    private void HandleDashOrSlide()
    {
        Vector3 origin = transform.position + Vector3.up * 1f;

        Collider[] hits = Physics.OverlapSphere(origin, detectionRadius, enemyLayer);

        foreach (var hit in hits)
        {
            Vector3 dirToTarget = (hit.transform.position - origin).normalized;

            float angle = Vector3.Angle(transform.forward, dirToTarget);

            if (angle <= viewAngle * 0.5f)
            {
                // 🔥 Enemy inside vision cone
                TryKick();
                Debug.Log("Enemy in vision");
                return;
            }
        }

        if (!grounded)
        {
            // AIR → always dash
            StartCoroutine(Dash());
            return;
        }

        // GROUNDED
        if (IsForwardInput())
        {

            slideRoutine = StartCoroutine(Slide()); // forward = slide
        }
        else
        {
            StartCoroutine(Dash()); // left/right/back = dash
        }
    }
    void OnDrawGizmos()
    {
        Vector3 origin = transform.position + Vector3.up * 1f;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, detectionRadius);

        Vector3 left = Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(origin, origin + left * detectionRadius);
        Gizmos.DrawLine(origin, origin + right * detectionRadius);
    }

    [SerializeField] private float slideDownY = 1.4f;
    [SerializeField] private float slideUpY = 1.8f;
    [SerializeField] private float heightLerpSpeed = 10f;
    [SerializeField] private CameraLook cameraLook;
    public GameObject rootGameObject;
    public bool cameraLocked;
    private float standHeightY;
    private float tempHeight;


    [SerializeField] private CapsuleCollider capsule;

    [SerializeField] private float standingHeight = 1.8f;
    [SerializeField] private float slidingHeight = 1.0f;

    [SerializeField] private float heightLerpDuration = 0.15f;

    private Vector3 originalRootLocalPos;
    private float rootVisualY;
    private float rootVelocityY;

    private IEnumerator Slide()
    {
        kickAnimator[characterindex - 1].gameObject.SetActive(true);

        Transform root = rootGameObject.transform;

        cameraLocked = true;

        if (cameralerpRoutine != null)
            StopCoroutine(cameralerpRoutine);

        cameralerpRoutine = StartCoroutine(
            LerpCameraRotation(new Vector3(0f, 0f, 8f), 0.8f, 100, 0)
        );

        isDashing = true;
        isSliding = true;
        readyToDash = false;

        audioSource.PlayOneShot(slideSFX, 0.8f);
        speedline.SetActive(true);
        kickAnimator[characterindex - 1].SetTrigger("Slide");

        // -------- SLIDE DOWN --------
        float t = 0f;
        float startHeight = capsule.height;

        while (t < 1f)
        {
            t += Time.deltaTime / heightLerpDuration;

            float newHeight = Mathf.Lerp(startHeight, slidingHeight, t);

            capsule.height = newHeight;
            capsule.center = new Vector3(0, newHeight / 2f, 0);

            float targetY = newHeight / 2f;

            rootVisualY = Mathf.SmoothDamp(
                rootVisualY,
                targetY,
                ref rootVelocityY,
                0.08f
            );

            root.localPosition = new Vector3(
                originalRootLocalPos.x,
                rootVisualY,
                originalRootLocalPos.z
            );

            yield return null;
        }

        // -------- SLIDE MOVEMENT --------
        Vector3 inputDir = orientation.forward;
        inputDir.y = 0f;
        inputDir.Normalize();

        float timer = 0f;

        while (timer < dashDuration)
        {
            Vector3 vel = inputDir * dashForce * characterAbilities.GetSlideSpeedModifier();
            vel.y = rb.velocity.y;
            rb.velocity = vel;

            timer += Time.deltaTime;
            yield return null;
        }

        speedline.SetActive(false);
        rb.velocity *= 1.1f;

        // -------- SLIDE UP --------
        t = 0f;
        startHeight = capsule.height;

        while (t < 1f)
        {
            t += Time.deltaTime / heightLerpDuration;

            float newHeight = Mathf.Lerp(startHeight, standingHeight, t);

            capsule.height = newHeight;
            capsule.center = new Vector3(0, newHeight / 2f, 0);

            float targetY = newHeight / 2f;

            rootVisualY = Mathf.SmoothDamp(
                rootVisualY,
                targetY,
                ref rootVelocityY,
                0.08f
            );

            root.localPosition = new Vector3(
                originalRootLocalPos.x,
                rootVisualY,
                originalRootLocalPos.z
            );

            yield return null;
        }

        // -------- FINAL SMOOTH SETTLE (no snap) --------
        while (Mathf.Abs(rootVisualY - originalRootLocalPos.y) > 0.001f)
        {
            rootVisualY = Mathf.SmoothDamp(
                rootVisualY,
                originalRootLocalPos.y,
                ref rootVelocityY,
                0.08f
            );

            root.localPosition = new Vector3(
                originalRootLocalPos.x,
                rootVisualY,
                originalRootLocalPos.z
            );

            yield return null;
        }

        // -------- HARD FINAL (tiny correction only) --------
        capsule.height = standingHeight;
        capsule.center = new Vector3(0, standingHeight / 2f, 0);
        root.localPosition = originalRootLocalPos;

        // -------- RESET --------
        isDashing = false;
        isSliding = false;
        slideCancelled = false;

        kickAnimator[characterindex - 1].gameObject.SetActive(false);

        yield return new WaitForSeconds(dashCooldown);
        readyToDash = true;
    }
    private Coroutine slideRoutine;
    private Coroutine cameralerpRoutine;
    private bool slideCancelled;

    public void CancelSlideImmediately()
    {
        if (!isDashing)
            return;

        if (slideRoutine != null)
        {
            StopCoroutine(slideRoutine);
            slideRoutine = null;
        }

        if (cameralerpRoutine != null)
        {
            StopCoroutine(cameralerpRoutine);
            cameralerpRoutine = null;
        }

        capsule.height = standingHeight;
        capsule.center = new Vector3(0f, standingHeight * 0.5f, 0f);

        rootGameObject.transform.localPosition = originalRootLocalPos;

        rootVisualY = originalRootLocalPos.y;
        rootVelocityY = 0f;

        //// CAMERA RESET
        //playerCameraMain.transform.localRotation =
        //    Quaternion.Euler(initialCamRot);

        cameraLook.SyncRotation();

        cameraLocked = false;

        isDashing = false;
        isSliding = false;

        speedline.SetActive(false);

        readyToDash = true;
    }

    private IEnumerator Dash()
    {
        
        isDashing = true;
        readyToDash = false;
        //const string boolNameRun = "Running";
        //anim.SetTrigger(boolNameRun);
        audioSource.PlayOneShot(slideSFX, 0.8F);
        originalHeight = transform.localScale.y;
        //targetHeight = slideHeight; // start lowering height
        speedline.SetActive(true);
        // Capture input direction at slide start
        Vector3 inputDir = orientation.forward * y + orientation.right * x;
        if (inputDir.magnitude < 0.1f)
            inputDir = orientation.forward; // default forward if no input
        inputDir.Normalize();

        float timer = 0f;

        while (timer < dashDuration)
        {
            // Move in input direction
            Vector3 vel = inputDir * dashForce * characterAbilities.GetSlideSpeedModifier();
            vel.y = rb.velocity.y; // preserve vertical velocity
            rb.velocity = vel;

            timer += Time.deltaTime;
            yield return null;
        }
        speedline.SetActive(false);
        // 🔥 Preserve slide momentum
        rb.velocity *= 1.1f;

        // Restore height smoothly
        //targetHeight = originalHeight;
        isDashing = false;
        // Wait for cooldown
        yield return new WaitForSeconds(dashCooldown);
        readyToDash = true;
        isDashing = false;
        //anim.ResetTrigger(boolNameRun);


    }
    private IEnumerator KickCooldownRoutine()
    {
        yield return new WaitForSeconds(kickCooldown);
        canKick = true;
         kickAnimator[characterindex - 1].gameObject.SetActive(false);
        playerKickedSomething = false;

      
    }

    private void TryKick()
    {
        if (!canKick) return;

        kickAnimator[characterindex - 1].gameObject.SetActive(true);
        kickAnimator[characterindex - 1].SetTrigger("Kick");
        //StartCoroutine(Dash());
        StartCoroutine(KickDelay());
        if(!cameraLocked)
            StartCoroutine(LerpCameraRotation(new Vector3(-11f, 0.7f, -3.7f), 0.15f, 100,0.24f)); // time in seconds

    }
    public IEnumerator LerpCameraRotation(
     Vector3 targetEuler,
     float duration,
     float rotationSpeed,
     float delay)
    {
        yield return new WaitForSeconds(delay);

        Transform cam = playerCameraMain.transform;
        Quaternion targetRot = Quaternion.Euler(targetEuler);

        float elapsed = 0f;
        bool reachedTarget = false;

        while (elapsed < duration)
        {
            if (!reachedTarget)
            {
                float angle = Quaternion.Angle(cam.localRotation, targetRot);

                if (angle <= 0.1f)
                {
                    reachedTarget = true;
                }
                else
                {
                    cam.localRotation = Quaternion.RotateTowards(
                        cam.localRotation,
                        targetRot,
                        rotationSpeed * Time.deltaTime
                    );
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // -------- SMOOTH BLEND OUT --------
        Quaternion startRot = cam.localRotation;
        Quaternion neutralRot = Quaternion.Euler(initialCamRot);

        float outTime = 0f;
        float outDuration = 0.2f; // tweak: how fast it wears off

        while (outTime < outDuration)
        {
            cam.localRotation = Quaternion.Slerp(
                startRot,
                neutralRot,
                outTime / outDuration
            );

            outTime += Time.deltaTime;
            yield return null;
        }

        // snap to exact neutral at end
        cam.localRotation = neutralRot;

        cameraLook.SyncRotation();
        // NOW release control
        cameraLocked = false;
        targetKickTilt = 0;

    }
    public IEnumerator LerpCameraZPosition(
    float zOffset,
    float duration,
    float lerpSpeed,
    float delay)
    {
        yield return new WaitForSeconds(delay);

        Transform cam = playerCameraMain.transform;

        cameraLocked = true;

        Vector3 startPos = cam.localPosition;
        Vector3 targetPos = new Vector3(
            startPos.x,
            startPos.y,
            startPos.z + zOffset
        );

        float elapsed = 0f;

        // -------- BLEND IN --------
        while (elapsed < duration)
        {
            cam.localPosition = Vector3.Lerp(
                cam.localPosition,
                targetPos,
                lerpSpeed * Time.deltaTime
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        // -------- BLEND OUT --------
        Vector3 blendOutStart = cam.localPosition;
        float outTime = 0f;
        float outDuration = 0.2f; // wear-off speed

        while (outTime < outDuration)
        {
            cam.localPosition = Vector3.Lerp(
                blendOutStart,
                startPos,
                outTime / outDuration
            );

            outTime += Time.deltaTime;
            yield return null;
        }

        cam.localPosition = startPos;

        cameraLocked = false;
    }


    public bool playerKickedSomething;
    IEnumerator KickDelay()
    {
        
        

        canKick = false;
        SFXManager.Instance.PlaySFX("Player/Kick", 0.6f);
        StartCoroutine(KickCooldownRoutine());

        // Detect enemies using leg collider sphere
        Collider[] hits = Physics.OverlapSphere(
            legKickPoint[characterindex - 1].position,
            kickRadius,
            enemyLayer
        );

        if (hits.Length == 0)
            yield return null;
      

        float finalKickForce = baseKickForce;

        // 🔥 Scale by player velocity
        float speed = rb.velocity.magnitude;
        finalKickForce += speed * velocityKickMultiplier;

        // 🔥 Dash bonus (sliding OR very fast)
        if (isDashing || speed > maxSpeed * 0.8f)
            finalKickForce *= dashKickMultiplier;

        // 🔥 Air kick bonus
        if (!grounded)
            finalKickForce *= airKickMultiplier;
        
        foreach (Collider hit in hits)
        {
            hit.GetComponent<EnemyManager>().kickOn = true;
            //yield return new WaitForSeconds(0.13f);
            //yield return new WaitForSeconds(0.13f);
           
            hit.GetComponent<EnemyManager>().animator.SetBool("Kicked",true);

            if (hit.gameObject.layer == 16)
            {
               
                if (hit.GetComponent<EnemyManager>() != null)
                {
                    if (hit.GetComponent<EnemyManager>().isGrabbing)
                        hit.GetComponent<EnemyManager>().EndGrab();

                    if (hit.GetComponent<EnemyManager>().canDash)
                        hit.GetComponent<EnemyManager>().StopDashImmediate();

                }

               
                Vector3 kickDir = (hit.transform.position - transform.position + orientation.forward * 0.4f).normalized;

                DoTimeSlow();

               
                // 🔥 APPLY NAVMESH KNOCKBACK
                DoKickKnockback(hit.transform, kickDir, finalKickForce);


                hands.GetComponent<HitstopShake>().DoShake();
                kickAnimator[characterindex - 1].GetComponent<HitstopShake>().DoShake();

                ImpactFrameEffect.Instance.GoBlack();
                LeanTween.delayedCall(0.05f, () => { ImpactFrameEffect.Instance.GoColor(); });
                // Optional: damage call if present

                EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
                StartCoroutine(ImpactColorCoroutine(enemy.GetComponentInChildren<SpriteRenderer>(), Color.red, 0.4f));
                
                //Impact VFX
                Vector3 hitPoint = enemy.transform.position + new Vector3(0, 0.55f, 0);

                // Normal = direction from surface to attacker
                Vector3 normal = (transform.position - hitPoint).normalized;

                Quaternion rot = Quaternion.LookRotation(normal);

                Instantiate(KickVFX, hitPoint, rot);
                //kickAnimator[characterindex - 1].gameObject.SetActive(false);
                if (enemy != null)
                {

                    SFXManager.Instance.PlaySFX("Player/KickImpact", 1f);
                    enemy.EnemyHit(15f, true); // tweakable
                    HitstopShake hitstop = enemy.GetComponentInChildren<HitstopShake>();
                    hitstop.DoShake();
                    //hit.GetComponent<EnemyManager>().kickOn = false;
                    //LeanTween.delayedCall(1f, () => { hit.GetComponent<EnemyManager>().animator.SetBool("Kicked", false); });

                }
        
            }
            
        }
    }

    public void ParryProjectiles(EnemyProjectile hit)
    {
        Debug.Log("Projectile Parryed");
        hit.enemyRigibody.GetComponent<EnemyManager>().projectile_Parried = true;
        hit.enemyRigibody.GetComponent<EnemyManager>().StopEnemy();
        DoTimeSlow();

        targetEnemy = hit.enemyRigibody.gameObject;
        Flash(0f, 0.2f, 0.1f);
        SFXManager.Instance.PlaySFX("Player/KickImpact", 1f);
        LeanTween.delayedCall(0.03f, () => { SFXManager.Instance.PlaySFX("Player/KickParry", 1f); });
        LeanTween.delayedCall(0.05f, () => { SFXManager.Instance.PlaySFX("Player/KickParry2", 1f); });

        Rigidbody rb = hit.GetComponent<Rigidbody>();

        ShootProjectile(rb, targetEnemy.transform, 70f);
        playerKickedSomething = true;
    }
    public Volume volume;

    Coroutine flashRoutine;

    public void Flash(float startWeight, float targetWeight, float duration)
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine(startWeight, targetWeight, duration));
    }

    IEnumerator FlashRoutine(float startWeight, float targetWeight, float duration)
    {
        float time = 0f;

        volume.weight = startWeight;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = time / duration;

            volume.weight = Mathf.Lerp(startWeight, targetWeight, t);

            yield return null;
        }

        volume.weight = targetWeight;
    }

    public GameObject targetEnemy;
    public static IEnumerator ImpactColorCoroutine(
     SpriteRenderer spriteRenderer,
     Color impactColor,
     float duration)
    {
        if (spriteRenderer == null)
            yield break;

        Color originalColor = spriteRenderer.color;

        spriteRenderer.color = impactColor;

        // NOT affected by Time.timeScale
        yield return new WaitForSecondsRealtime(duration);

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    [Header("Time Slow Settings")]
    [SerializeField] private float slowTimeScale = 0.15f;
    [SerializeField] private float slowDuration = 1f;

    private Coroutine timeRoutine;
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

        // IMPORTANT: use unscaled time
        yield return new WaitForSecondsRealtime(slowDuration);

        // Restore normal time
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        Flash(0.2f, 0f, 0.1f);
        timeRoutine = null;
    }
    private void DoKickKnockback(Transform target, Vector3 direction, float force)
    {
        StartCoroutine(KickKnockbackCoroutine(target, direction, force));
    }

    private IEnumerator KickKnockbackCoroutine(
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

        // 🔥 Launch
        Vector3 launchVelocity = direction * force;
        launchVelocity.y = kickUpwardForce;
        rb.velocity = launchVelocity;

        // -------- WAIT UNTIL GROUNDED --------

        EnemyManager enemyManager =
            target.GetComponent<EnemyManager>();
        float groundCheckDelay = 1f;
        float timer = 0f;

        while (true)
        {
            if (rb == null || enemyManager == null)
                yield break;

            timer += Time.deltaTime;

            // Don't start checking for landing yet
            if (timer < groundCheckDelay)
            {
                yield return null;
                continue;
            }

            if (enemyManager.CheckGrounded())
            {
               // Debug.Log("Enemy Landed");
              
                break;
            }

            yield return null;
        }
        // -------- SLIDE PHASE --------
        Vector3 slideVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.velocity = slideVelocity;

        float slideTimer = 0f;

        while (slideTimer < enemySlideDuration && rb.velocity.magnitude > 0.1f)
        {
            slideTimer += Time.deltaTime;

            Vector3 horizontal = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            horizontal = Vector3.Lerp(
                horizontal,
                Vector3.zero,
                enemySlideDamping * Time.deltaTime
            );

            rb.velocity = new Vector3(horizontal.x, rb.velocity.y, horizontal.z);
            //rb.velocity = Vector3.zero;
            //rb.isKinematic = true;
            yield return null;
        }
        // -------- NAVMESH RECOVERY --------
        if (agent != null)
        {
            if (NavMesh.SamplePosition(
                target.position,
                out NavMeshHit hit,
                3f,
                NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }

            agent.enabled = true;
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;

            enemyManager.kickOn = false;
            enemyManager.animator.SetBool("Kicked", false);

        }
    }
    // Called on kick impact frame
    public void StartKickCameraTilt()
    {
        targetKickTilt = kickTiltAngle;
    }

    // Called near end of kick animation
    public void EndKickCameraTilt()
    {
        targetKickTilt = 0f; // SNAP BACK
    }


    private void OnDrawGizmosSelected()
    {
        if (legKickPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(legKickPoint[characterindex - 1].position, kickRadius);
    }

    private void HandleFootsteps()
    {
        if (!grounded) return;
        if (rb.velocity.magnitude < 0.1f) return; // not moving

        stepTimer -= Time.deltaTime;
        if (stepTimer <= 0f)
        {
            PlayFootstepSFX();

            // Set next interval based on state
                stepTimer = runStepInterval;

        }
    }
    private void PlayFootstepSFX()
    {
        string sfxKey = $"Player/Footstep";
        SFXManager.Instance.PlaySFX(sfxKey, 1f);
    }

    //Projectile Kickback

    public void ShootProjectile(Rigidbody projectile, Transform target, float speed)
    {
        if (projectile == null || target == null)
            return;

        Vector3 targetPos = target.position + Vector3.up * 1.5f;

        Vector3 direction = (targetPos - projectile.position).normalized;

        projectile.useGravity = false;
        projectile.velocity = Vector3.zero;
        projectile.angularVelocity = Vector3.zero;

        projectile.velocity = direction * speed;

       
        var homing = projectile.GetComponent<EnemyProjectile>();
        if (homing != null)
        {
            homing.eliminatorTarget = target;
            homing.eliminatorSpeed = speed;
        }
    }
    [SerializeField] private List<PlayerItem> availableItems;

    private PlayerItem currentItemPrefab; // 🔥 store prefab only
    private PlayerItem spawnedItem;       // actual instance (only when needed)

    [Header("Item System")]
    public GameObject currentItem;
    public Transform itemHoldPoint;

    [Header("Throw Settings")]
    public float throwForce = 15f;
    public float throwUpwardForce = 5f;
    public float throwCooldown = 0.5f;
    public float throwDistance = 30f;

    private bool canThrow = true;

    private bool itemPressed;

    public void OnItem(InputAction.CallbackContext context)
    {
        if (context.performed)
            itemPressed = true;
    }
    private void OnCollisionEnter(Collision collision)
    {
     
        if(isSliding && collision.gameObject.CompareTag("Blood"))
        {
            TryKick();   
        }

    }
    public void GetRandomItem()
    {
        if (availableItems == null || availableItems.Count == 0)
            return;

        int index = Random.Range(0, availableItems.Count);

        currentItemPrefab = availableItems[index];

        Debug.Log("Picked item: " + currentItemPrefab.name);
    }
   
    PlayerItem SpawnItem()
    {
        if (currentItemPrefab == null) return null;

        spawnedItem = Instantiate(currentItemPrefab, itemHoldPoint.position, itemHoldPoint.rotation);

        return spawnedItem;
    }
    void UseOrThrowItem()
    {
        if (currentItemPrefab == null || !canThrow) return;

        PlayerItem item = SpawnItem();

        if (item == null) return;

        if (item.throwable)
        {
            ThrowItem(item);
        }
        else
        {
            item.OnUse(this);
        }

        currentItemPrefab = null; // clear after use
    }
    void ThrowItem(PlayerItem item)
    {
        canThrow = false;

        Rigidbody rb = item.rb;

        item.transform.position = itemHoldPoint.position + itemHoldPoint.forward * 0.5f;

        rb.isKinematic = false;
        item.col.enabled = true;

        Physics.IgnoreCollision(item.col, GetComponent<Collider>(), false);

        Ray ray = playerCameraMain.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        Vector3 targetPoint = Physics.Raycast(ray, out hit, throwDistance)
            ? hit.point
            : ray.GetPoint(throwDistance);

        Vector3 direction = (targetPoint - item.transform.position);

        Vector3 force = direction.normalized * throwForce;
        force.y += throwUpwardForce;

        rb.velocity = force;
        rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 5f;

        Invoke(nameof(ResetThrow), throwCooldown);
    }
    void ResetThrow()
    {
        canThrow = true;
    }
}
