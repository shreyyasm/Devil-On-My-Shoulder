using UnityEngine;
using InfimaGames.LowPolyShooterPack;

[DisallowMultipleComponent]
public class PlayerDeathFall : MonoBehaviour
{
    [Header("Editable Animation")]
    [Tooltip("The Animator on the Death Camera Animation Pivot. Its PlayerDeathFall clip controls the complete death motion.")]
    [SerializeField] private Animator deathCameraAnimator;

    [Tooltip("The state played when the player dies.")]
    [SerializeField] private string deathStateName = "DeathFall";

    [Tooltip("Used only if the clip cannot be found at runtime.")]
    [SerializeField, Min(0.05f)] private float fallbackDuration = 0.98f;

    [Tooltip("Playback speed for the editable death Animator clip.")]
    [SerializeField, Range(0.1f, 1f)] private float animationSpeed = 0.6f;

    private PlayerMovement playerMovement;
    private CameraLook cameraLook;
    private Animator characterAnimator;
    private bool playing;

    public float FallDuration
    {
        get
        {
            ResolveAnimation();
            if (deathCameraAnimator != null && deathCameraAnimator.runtimeAnimatorController != null)
            {
                AnimationClip[] clips = deathCameraAnimator.runtimeAnimatorController.animationClips;
                if (clips != null && clips.Length > 0 && clips[0] != null)
                    return clips[0].length / Mathf.Max(0.01f, animationSpeed);
            }

            return fallbackDuration / Mathf.Max(0.01f, animationSpeed);
        }
    }

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        ResolveAnimation();
    }

    public void PlayDeathFall()
    {
        if (playing)
            return;

        ResolveAnimation();

        if (cameraLook != null)
            cameraLook.enabled = false;

        // This is the existing character Animator on the first-person rig.
        // Disable it during the death animation so it cannot move the camera socket.
        if (characterAnimator != null)
            characterAnimator.enabled = false;

        if (deathCameraAnimator == null)
            return;

        deathCameraAnimator.enabled = true;
        deathCameraAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        deathCameraAnimator.speed = animationSpeed;
        deathCameraAnimator.Rebind();
        deathCameraAnimator.Update(0f);
        deathCameraAnimator.Play(deathStateName, 0, 0f);
        playing = true;
    }

    private void ResolveAnimation()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (cameraLook == null && playerMovement != null)
            cameraLook = playerMovement.cameraLook;

        if (characterAnimator == null && cameraLook != null)
            characterAnimator = cameraLook.GetComponent<Animator>();

        if (deathCameraAnimator != null)
            return;

        Camera activeCamera = playerMovement != null ? playerMovement.playerCameraMain : Camera.main;
        if (activeCamera == null)
            return;

        Transform pivot = activeCamera.transform.parent;
        if (pivot != null && pivot.name == "Death Camera Animation Pivot")
            deathCameraAnimator = pivot.GetComponent<Animator>();
    }
}




