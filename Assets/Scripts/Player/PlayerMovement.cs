// Some stupid rigidbody based movement by Dani

using InfimaGames.LowPolyShooterPack;
using QFSW.MOP2;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.VFX;

public class PlayerMovement : MonoBehaviour 
{

    public static PlayerMovement Instance;
   
    //Assingables
    public Transform playerCam;
    public Transform orientation;

    //Other
    private Rigidbody rb;

    [Header("Movement Settings")]
    public float moveSpeed = 4500;
    public float maxSpeed = 20;
    public bool grounded;
    public LayerMask whatIsGround;   
    public float counterMovement = 0.175f;
    private float threshold = 0.01f;
    public float maxSlopeAngle = 35f;


    [Header("Jump Settings")]
    public float jumpForce = 550f;
    private bool readyToJump = true;
    public float jumpCooldown = 0.25f;
    private bool jumpPressed = false;
    private bool slidePressed = false;


    public AudioClip jumpSFX;
    public AudioSource audioSource;

    [Header("Slide Settings")]
    public float slideForce = 15f;
    public float slideDuration = 0.5f;
    public float slideCooldown = 1f;
    public float slideHeight = 0.5f; // target height when sliding
    public bool isSliding = false;
    public bool readyToSlide = true;

    //Slide
    private Vector3 crouchScale = new Vector3(1, 0.5f, 1);
    private Vector3 playerScale;
    public float slideCounterMovement = 0.2f;

    // For smooth height change
    private float targetHeight;
    private float heightSmoothSpeed = 10f;
    private float originalHeight;

    public AudioClip slideSFX;
    public Animator anim;
    public Animator speedline;



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

   
    void Awake() {
        rb = GetComponent<Rigidbody>();
        if(Instance == null)
            Instance = this;
        targetHeight = 1.4f;

        Shader.Find("Hidden/ImpactFrame");

       
    }

    void Start()
    {
        ApplyCharacterData();
        playerScale = transform.localScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerCam != null)
            initialCamRot = playerCameraMain.transform.localEulerAngles;
    }

    private void ApplyCharacterData()
    {

        // Movement
        moveSpeed = PlayerCharacterData.Instance.moveSpeed;

        // Jump
        jumpForce = PlayerCharacterData.Instance.jumpForce;

        // Slide
        slideForce = PlayerCharacterData.Instance.slideForce;

        //Health
        playerHealth.maxHealth = PlayerCharacterData.Instance.maxHealth;
    }

    private void FixedUpdate() {
        Movement();
    }
    private Vector2 moveInput = Vector2.zero;   // x = horizontal, y = vertical
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
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

    private void Update() {
        MyInput();
        CameraTiltSway();
        HandleFootsteps();

        // Smoothly adjust height
        Vector3 scale = transform.localScale;
        scale.y = Mathf.Lerp(scale.y, targetHeight, Time.deltaTime * heightSmoothSpeed);
        transform.localScale = scale;

        // Smoothly adjust FOV
        float targetFOV = isSliding ? slideFOV : normalFOV;
        playerCameraMain.fieldOfView = Mathf.Lerp(playerCameraMain.fieldOfView, targetFOV, Time.deltaTime * fovSmoothSpeed);
    }

    /// <summary>
    /// Find user input. Should put this in its own class but im lazy
    /// </summary>
    public PlayerHealth playerHealth;
    private void MyInput()
    {
        if (mainOpen) return;
        y = moveInput.y;

        if (mainMenu) return;
        x = moveInput.x;


        jumping = jumpPressed;
        if (slidePressed && readyToSlide && (x != 0 || y != 0))
        {
            StartCoroutine(Slide());
            slidePressed = false;
        }

    }


    

    private void Movement() {
        //If holding jump && ready to jump, then jump
        if (readyToJump && jumping) Jump();
        if (isSliding) return;
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
        
        // Movement in air
        if (!grounded) {
            multiplier = 0.5f;
            multiplierV = 0.5f;
        }
        
        // Movement while sliding
        if (grounded && crouching) multiplierV = 0f;

        //Apply forces to move player
        rb.AddForce(orientation.transform.forward * y * moveSpeed * Time.deltaTime * multiplier * multiplierV);
        rb.AddForce(orientation.transform.right * x * moveSpeed * Time.deltaTime * multiplier);
    }

    private void Jump()
    {
        if (grounded && readyToJump)
        {
            readyToJump = false;

            audioSource.PlayOneShot(jumpSFX, 0.7f);
            //Add jump forces
            rb.AddForce(Vector2.up * jumpForce * 1.5f);
            rb.AddForce(normalVector * jumpForce * 0.5f);

            //If jumping while falling, reset y velocity.
            Vector3 vel = rb.velocity;
            if (rb.velocity.y < 0.5f)
                rb.velocity = new Vector3(vel.x, 0, vel.z);
            else if (rb.velocity.y > 0)
                rb.velocity = new Vector3(vel.x, vel.y / 2, vel.z);

            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void ResetJump()
    {
        readyToJump = true;
    }


    private void CounterMovement(float x, float y, Vector2 mag) {
        if (!grounded || jumping) return;

        //Slow down sliding
        if (crouching) {
            rb.AddForce(moveSpeed * Time.deltaTime * -rb.velocity.normalized * slideCounterMovement);
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

        // Calculate target tilt
        float tiltZ = -x * swayTiltX; // roll for strafing
        float tiltX = y * swayTiltY;  // pitch for forward/back

        Quaternion targetRot = Quaternion.Euler(initialCamRot.x + tiltX,
                                                 initialCamRot.y,
                                                 initialCamRot.z + tiltZ);

        // Smoothly rotate
        playerCameraMain.transform.localRotation = Quaternion.Lerp(playerCameraMain.transform.localRotation, targetRot, Time.deltaTime * swaySmooth);
    }
   
    private IEnumerator Slide()
    {
        isSliding = true;
        readyToSlide = false;
        //const string boolNameRun = "Running";
        //anim.SetTrigger(boolNameRun);
        audioSource.PlayOneShot(slideSFX, 0.8F);
        originalHeight = transform.localScale.y;
        targetHeight = slideHeight; // start lowering height
        speedline.SetTrigger("SpeedLines");
        // Capture input direction at slide start
        Vector3 inputDir = orientation.forward * y + orientation.right * x;
        if (inputDir.magnitude < 0.1f)
            inputDir = orientation.forward; // default forward if no input
        inputDir.Normalize();

        float timer = 0f;

        while (timer < slideDuration)
        {
            // Move in input direction
            Vector3 vel = inputDir * slideForce;
            vel.y = rb.velocity.y; // preserve vertical velocity
            rb.velocity = vel;

            timer += Time.deltaTime;
            yield return null;
        }

        // Restore height smoothly
        targetHeight = originalHeight;
        isSliding = false;
        // Wait for cooldown
        yield return new WaitForSeconds(slideCooldown);
        readyToSlide = true;
        isSliding = false;
        //anim.ResetTrigger(boolNameRun);
        speedline.ResetTrigger("SpeedLines");
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

}
