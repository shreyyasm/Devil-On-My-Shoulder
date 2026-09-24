using UnityEngine;
using DG.Tweening;

public class AbilityActiveIcon : MonoBehaviour
{
    [Header("Sprite Renderers")]
    [SerializeField] private SpriteRenderer mainSprite;
    [SerializeField] private SpriteRenderer redRing;

    [Header("Main Sprite Pop")]
    [Tooltip("How much the main sprite scales above its base scale.")]
    [SerializeField] private float mainPopAmount = 0.15f;

    [Tooltip("Time to scale up.")]
    [SerializeField] private float mainScaleUpDuration = 0.15f;

    [Tooltip("Time to scale back down.")]
    [SerializeField] private float mainScaleDownDuration = 0.15f;

    [SerializeField] private Ease mainScaleUpEase = Ease.OutBack;
    [SerializeField] private Ease mainScaleDownEase = Ease.InOutSine;

    [Header("Red Ring")]
    [Tooltip("First scale increase. Example: 0.12.")]
    [SerializeField] private float ringFirstPop = 0.12f;

    [Tooltip("Second scale increase. Example: 0.20.")]
    [SerializeField] private float ringSecondPop = 0.20f;

    [Tooltip("Time for the first scale up.")]
    [SerializeField] private float ringFirstUpDuration = 0.12f;

    [Tooltip("Time to return to base after first pop.")]
    [SerializeField] private float ringFirstDownDuration = 0.12f;

    [Tooltip("Time for the second scale up.")]
    [SerializeField] private float ringSecondUpDuration = 0.12f;

    [Tooltip("Time to return to base after second pop.")]
    [SerializeField] private float ringSecondDownDuration = 0.12f;

    [SerializeField] private Ease ringUpEase = Ease.OutQuad;
    [SerializeField] private Ease ringDownEase = Ease.InOutSine;

    [Header("Look At Player")]
    [SerializeField] private bool lookAtPlayer = true;

    private Vector3 mainBaseScale;
    private Vector3 ringBaseScale;

    private Sequence mainSequence;
    private Sequence ringSequence;

    private Transform player;

    private void Awake()
    {
        // -----------------------------------------
        // STORE BASE SCALES
        // -----------------------------------------

        if (mainSprite != null)
            mainBaseScale = mainSprite.transform.localScale;

        if (redRing != null)
            ringBaseScale = redRing.transform.localScale;

        // -----------------------------------------
        // FIND PLAYER
        // -----------------------------------------

      
            player = FindObjectOfType<PlayerMovement>().transform;
       
        Activate();
    }

    private void Update()
    {
        LookAtPlayer();
    }

    // ============================================================
    // LOOK AT PLAYER
    // ============================================================

    private void LookAtPlayer()
    {
        if (!lookAtPlayer || player == null)
            return;

        Vector3 direction =
            player.position - transform.position;

        // Ignore Y difference.
        // This makes the object rotate horizontally only.
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        // Get the rotation required to face the player.
        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        // Keep the current X and Z.
        // ONLY change Y.
        Vector3 currentEuler =
            transform.eulerAngles;

        transform.rotation = Quaternion.Euler(
            currentEuler.x,
            targetRotation.eulerAngles.y,
            currentEuler.z
        );
    }

    // ============================================================
    // ACTIVATE
    // ============================================================

    public void Activate()
    {
        StopAnimation();

        // Reset both to base scale.
        if (mainSprite != null)
            mainSprite.transform.localScale = mainBaseScale;

        if (redRing != null)
            redRing.transform.localScale = ringBaseScale;

        CreateMainPop();
        CreateRingPop();
    }

    // ============================================================
    // MAIN SPRITE POP
    // ============================================================

    private void CreateMainPop()
    {
        if (mainSprite == null)
            return;

        Vector3 popScale =
            mainBaseScale *
            (1f + mainPopAmount);

        mainSequence = DOTween.Sequence();

        // Base -> Pop
        mainSequence.Append(
            mainSprite.transform.DOScale(
                popScale,
                mainScaleUpDuration
            ).SetEase(mainScaleUpEase)
        );

        // Pop -> Base
        mainSequence.Append(
            mainSprite.transform.DOScale(
                mainBaseScale,
                mainScaleDownDuration
            ).SetEase(mainScaleDownEase)
        );

        // Loop forever.
        mainSequence.SetLoops(
            -1,
            LoopType.Restart
        );
    }

    // ============================================================
    // RED RING POP
    // ============================================================

    private void CreateRingPop()
    {
        if (redRing == null)
            return;

        Vector3 firstPopScale =
            ringBaseScale +
            Vector3.one * ringFirstPop;

        Vector3 secondPopScale =
            ringBaseScale +
            Vector3.one * ringSecondPop;

        ringSequence = DOTween.Sequence();

        // Base -> +0.12
        ringSequence.Append(
            redRing.transform.DOScale(
                firstPopScale,
                ringFirstUpDuration
            ).SetEase(ringUpEase)
        );

        // +0.12 -> Base
        ringSequence.Append(
            redRing.transform.DOScale(
                ringBaseScale,
                ringFirstDownDuration
            ).SetEase(ringDownEase)
        );

        // Base -> +0.20
        ringSequence.Append(
            redRing.transform.DOScale(
                secondPopScale,
                ringSecondUpDuration
            ).SetEase(ringUpEase)
        );

        // +0.20 -> Base
        ringSequence.Append(
            redRing.transform.DOScale(
                ringBaseScale,
                ringSecondDownDuration
            ).SetEase(ringDownEase)
        );

        // Loop forever.
        ringSequence.SetLoops(
            -1,
            LoopType.Restart
        );
    }

    // ============================================================
    // STOP
    // ============================================================

    public void StopAnimation()
    {
        if (mainSequence != null)
        {
            mainSequence.Kill();
            mainSequence = null;
        }

        if (ringSequence != null)
        {
            ringSequence.Kill();
            ringSequence = null;
        }

        if (mainSprite != null)
            mainSprite.transform.localScale = mainBaseScale;

        if (redRing != null)
            redRing.transform.localScale = ringBaseScale;
    }

    // ============================================================
    // ON DESTROY
    // ============================================================

    private void OnDestroy()
    {
        if (mainSequence != null)
            mainSequence.Kill();

        if (ringSequence != null)
            ringSequence.Kill();
    }
}