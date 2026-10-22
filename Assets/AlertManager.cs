using System.Collections;
using UnityEngine;

public class AlertManager : MonoBehaviour
{
    [Header("Alert Sprites")]
    [SerializeField] private SpriteRenderer sprite1;
    [SerializeField] private SpriteRenderer sprite2;
    [SerializeField] private SpriteRenderer sprite3;

    [Header("Reveal")]
    [SerializeField] private float revealDuration = 0.15f;

    [Header("Final Parent Scale")]
    [Tooltip("Final parent scale relative to its original scale.")]
    [SerializeField] private float finalParentScale = 0.5f;

    [SerializeField] private float parentScaleDuration = 0.15f;

    [Header("Reveal Pop")]
    [Tooltip("Temporary scale multiplier during the reveal pop.")]
    [SerializeField] private float revealPopScale = 1.15f;

    [Header("Final Color")]
    [SerializeField] private Color revealColor = Color.red;

    [Header("Final Parent Movement")]
    [Tooltip("How far the parent moves downward on the local Y axis.")]
    [SerializeField] private float parentMoveDownY = 0.3f;

    [Header("Look At Player")]
    [SerializeField] private bool lookAtPlayer = true;

    [Header("Sprite Shake")]
    [SerializeField] private float shakeAngle = 5f;
    [SerializeField] private float shakeSpeed = 0.08f;

    [Tooltip("Delay between each sprite's shake.")]
    [SerializeField] private float shakeOffset = 0.08f;

    private static readonly int RevealProperty =
        Shader.PropertyToID("_Reveal");

    private MaterialPropertyBlock propertyBlock;

    private Coroutine alertCoroutine;

    private Transform player;

    // Original parent transform values
    private Vector3 originalParentScale;
    private Vector3 originalParentPosition;

    // Original sprite rotations
    private Quaternion sprite1OriginalRotation;
    private Quaternion sprite2OriginalRotation;
    private Quaternion sprite3OriginalRotation;

    // Original sprite colors
    private Color sprite1OriginalColor;
    private Color sprite2OriginalColor;
    private Color sprite3OriginalColor;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();

        // -----------------------------------------
        // ORIGINAL PARENT VALUES
        // -----------------------------------------

        originalParentScale = transform.localScale;
        originalParentPosition = transform.localPosition;

        // -----------------------------------------
        // PLAYER
        // -----------------------------------------

        if (PlayerMovement.Instance != null)
        {
            player = PlayerMovement.Instance.transform;
        }

        // -----------------------------------------
        // SPRITE 1
        // -----------------------------------------

        if (sprite1 != null)
        {
            sprite1OriginalRotation =
                sprite1.transform.localRotation;

            sprite1OriginalColor =
                sprite1.color;
        }

        // -----------------------------------------
        // SPRITE 2
        // -----------------------------------------

        if (sprite2 != null)
        {
            sprite2OriginalRotation =
                sprite2.transform.localRotation;

            sprite2OriginalColor =
                sprite2.color;
        }

        // -----------------------------------------
        // SPRITE 3
        // -----------------------------------------

        if (sprite3 != null)
        {
            sprite3OriginalRotation =
                sprite3.transform.localRotation;

            sprite3OriginalColor =
                sprite3.color;
        }

        // Start with reveal at zero.
        SetReveal(sprite1, 0f);
        SetReveal(sprite2, 0f);
        SetReveal(sprite3, 0f);
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
        if (!lookAtPlayer)
            return;

        if (player == null)
        {
            if (PlayerMovement.Instance != null)
            {
                player = PlayerMovement.Instance.transform;
            }
            else
            {
                return;
            }
        }

        Vector3 direction =
            player.position - transform.position;

        // Ignore vertical difference.
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        Vector3 currentEuler =
            transform.eulerAngles;

        // ONLY change Y.
        // X and Z remain untouched.

        transform.rotation = Quaternion.Euler(
            currentEuler.x,
            targetRotation.eulerAngles.y,
            currentEuler.z
        );
    }

    // ============================================================
    // REVEAL ALERT
    // ============================================================

    public void RevealAlert()
    {
        // Pooled enemies can be re-enabled while their alert child remains
        // inactive from a previous death/reset. A coroutine cannot run on an
        // inactive GameObject, so make the visual available before starting
        // its reveal sequence.
        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        if (alertCoroutine != null)
        {
            StopCoroutine(alertCoroutine);
        }

        alertCoroutine =
            StartCoroutine(RevealAlertSequence());
    }

    private IEnumerator RevealAlertSequence()
    {
        // Reset everything.
        ResetSpritesForReveal();

        // Reset parent transform.
        transform.localScale =
            originalParentScale;

        transform.localPosition =
            originalParentPosition;

        // ========================================================
        // SPRITE 1
        // ========================================================

        yield return StartCoroutine(
            RevealSprite(sprite1)
        );

        // ========================================================
        // SPRITE 2
        // ========================================================

        yield return StartCoroutine(
            RevealSprite(sprite2)
        );

        // ========================================================
        // SPRITE 3
        // ========================================================

        yield return StartCoroutine(
            RevealSprite(sprite3)
        );

        // ========================================================
        // ALL THREE ARE FULLY REVEALED
        // ========================================================
        //
        // Only NOW do all three become red.
        //

        SetAllSpritesColor(revealColor);

        // ========================================================
        // SCALE PARENT DOWN + MOVE DOWN
        // ========================================================

        yield return StartCoroutine(
            ScaleParentDown()
        );

        // ========================================================
        // START OFFSET SHAKES
        // ========================================================

        StartCoroutine(
            ShakeSprite(
                sprite1,
                sprite1OriginalRotation,
                0f
            )
        );

        StartCoroutine(
            ShakeSprite(
                sprite2,
                sprite2OriginalRotation,
                shakeOffset
            )
        );

        StartCoroutine(
            ShakeSprite(
                sprite3,
                sprite3OriginalRotation,
                shakeOffset * 2f
            )
        );
    }

    // ============================================================
    // REVEAL SPRITE
    // ============================================================

    private IEnumerator RevealSprite(
        SpriteRenderer sprite
    )
    {
        if (sprite == null)
            yield break;

        float elapsed = 0f;

        // Store the sprite's original scale.
        Vector3 originalScale =
            sprite.transform.localScale;

        while (elapsed < revealDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / revealDuration
                );

            // ====================================================
            // REVEAL 0 -> 1
            // ====================================================

            float revealT =
                1f - Mathf.Pow(
                    1f - t,
                    3f
                );

            SetReveal(
                sprite,
                revealT
            );

            // ====================================================
            // SMALL POP
            // ====================================================

            float popT =
                Mathf.Sin(
                    t * Mathf.PI
                );

            float scaleMultiplier =
                Mathf.Lerp(
                    1f,
                    revealPopScale,
                    popT
                );

            sprite.transform.localScale =
                originalScale *
                scaleMultiplier;

            yield return null;
        }

        // ========================================================
        // FINAL REVEAL
        // ========================================================

        SetReveal(sprite, 1f);

        // Return sprite to original scale.
        sprite.transform.localScale =
            originalScale;

        // IMPORTANT:
        // No color change here.
        //
        // All three change color together after
        // all three reveals have completed.
    }

    // ============================================================
    // SCALE PARENT DOWN + MOVE DOWN
    // ============================================================

    private IEnumerator ScaleParentDown()
    {
        Vector3 startScale =
            transform.localScale;

        Vector3 targetScale =
            originalParentScale *
            finalParentScale;

        Vector3 startPosition =
            transform.localPosition;

        Vector3 targetPosition =
            startPosition;

        // Move downward on LOCAL Y.
        targetPosition.y -= parentMoveDownY;
        targetPosition.x -= parentMoveDownY;

        float elapsed = 0f;

        while (elapsed < parentScaleDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / parentScaleDuration
                );

            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            // -----------------------------------------
            // SCALE DOWN
            // -----------------------------------------

            transform.localScale =
                Vector3.Lerp(
                    startScale,
                    targetScale,
                    t
                );

            // -----------------------------------------
            // MOVE DOWN
            // -----------------------------------------

            transform.localPosition =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    t
                );

            yield return null;
        }

        // Ensure exact final values.
        transform.localScale =
            targetScale;

        transform.localPosition =
            targetPosition;
    }

    // ============================================================
    // SET ALL SPRITES COLOR
    // ============================================================

    private void SetAllSpritesColor(
        Color color
    )
    {
        if (sprite1 != null)
            sprite1.color = color;

        if (sprite2 != null)
            sprite2.color = color;

        if (sprite3 != null)
            sprite3.color = color;
    }

    // ============================================================
    // SHAKE SPRITE
    // ============================================================

    private IEnumerator ShakeSprite(
        SpriteRenderer sprite,
        Quaternion originalRotation,
        float startDelay
    )
    {
        if (sprite == null)
            yield break;

        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(
                startDelay
            );
        }

        while (true)
        {
            // 0 -> +5
            yield return StartCoroutine(
                RotateSpriteTo(
                    sprite,
                    originalRotation,
                    shakeAngle
                )
            );

            // +5 -> 0
            yield return StartCoroutine(
                RotateSpriteTo(
                    sprite,
                    originalRotation,
                    0f
                )
            );

            // 0 -> -5
            yield return StartCoroutine(
                RotateSpriteTo(
                    sprite,
                    originalRotation,
                    -shakeAngle
                )
            );

            // -5 -> 0
            yield return StartCoroutine(
                RotateSpriteTo(
                    sprite,
                    originalRotation,
                    0f
                )
            );
        }
    }

    // ============================================================
    // ROTATE SPRITE
    // ============================================================

    private IEnumerator RotateSpriteTo(
        SpriteRenderer sprite,
        Quaternion originalRotation,
        float zOffset
    )
    {
        float elapsed = 0f;

        Quaternion startRotation =
            sprite.transform.localRotation;

        Quaternion targetRotation =
            originalRotation *
            Quaternion.Euler(
                0f,
                0f,
                zOffset
            );

        while (elapsed < shakeSpeed)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / shakeSpeed
                );

            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            sprite.transform.localRotation =
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    t
                );

            yield return null;
        }

        sprite.transform.localRotation =
            targetRotation;
    }

    // ============================================================
    // MATERIAL REVEAL
    // ============================================================

    private void SetReveal(
        SpriteRenderer sprite,
        float value
    )
    {
        if (sprite == null)
            return;

        sprite.GetPropertyBlock(
            propertyBlock
        );

        propertyBlock.SetFloat(
            RevealProperty,
            value
        );

        sprite.SetPropertyBlock(
            propertyBlock
        );
    }

    // ============================================================
    // RESET BEFORE REVEAL
    // ============================================================

    private void ResetSpritesForReveal()
    {
        if (sprite1 != null)
        {
            SetReveal(sprite1, 0f);

            sprite1.transform.localRotation =
                sprite1OriginalRotation;

            sprite1.color =
                sprite1OriginalColor;
        }

        if (sprite2 != null)
        {
            SetReveal(sprite2, 0f);

            sprite2.transform.localRotation =
                sprite2OriginalRotation;

            sprite2.color =
                sprite2OriginalColor;
        }

        if (sprite3 != null)
        {
            SetReveal(sprite3, 0f);

            sprite3.transform.localRotation =
                sprite3OriginalRotation;

            sprite3.color =
                sprite3OriginalColor;
        }
    }

    // ============================================================
    // RESET ALERT
    // ============================================================

    public void ResetAlert()
    {
        if (alertCoroutine != null)
        {
            StopCoroutine(alertCoroutine);
            alertCoroutine = null;
        }

        SetReveal(sprite1, 0f);
        SetReveal(sprite2, 0f);
        SetReveal(sprite3, 0f);

        // Restore parent.
        transform.localScale =
            originalParentScale;

        transform.localPosition =
            originalParentPosition;

        // Restore sprite 1.
        if (sprite1 != null)
        {
            sprite1.transform.localRotation =
                sprite1OriginalRotation;

            sprite1.color =
                sprite1OriginalColor;
        }

        // Restore sprite 2.
        if (sprite2 != null)
        {
            sprite2.transform.localRotation =
                sprite2OriginalRotation;

            sprite2.color =
                sprite2OriginalColor;
        }

        // Restore sprite 3.
        if (sprite3 != null)
        {
            sprite3.transform.localRotation =
                sprite3OriginalRotation;

            sprite3.color =
                sprite3OriginalColor;
        }
    }
}
