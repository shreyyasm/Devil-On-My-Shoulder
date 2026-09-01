using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HPAdded : MonoBehaviour
{
    [Header("Main Sprite")]
    [SerializeField] private SpriteRenderer mainSpriteRenderer;

    [Header("Available Sprites")]
    [SerializeField] private List<Sprite> sprites = new List<Sprite>();

    [Header("Parent Pop")]
    [SerializeField] private float popScale = 1.2f;
    [SerializeField] private float popDuration = 0.2f;
    [SerializeField] private Ease popEase = Ease.OutBack;

    [Header("Sprite 1 Scale Down")]
    [SerializeField] private SpriteRenderer spriteRenderer1;
    [SerializeField] private float sprite1ScaleDown = 0.8f;
    [SerializeField] private float sprite1ScaleDownDuration = 0.15f;
    [SerializeField] private Ease sprite1Ease = Ease.OutQuad;

    [Header("Sprite 2 Scale Down")]
    [SerializeField] private SpriteRenderer spriteRenderer2;
    [SerializeField] private float sprite2ScaleDown = 0.8f;
    [SerializeField] private float sprite2ScaleDownDuration = 0.15f;
    [SerializeField] private Ease sprite2Ease = Ease.OutQuad;

    private Vector3 originalParentScale;
    private Vector3 originalSprite1Scale;
    private Vector3 originalSprite2Scale;

    private Sequence currentSequence;

    [Header("Random X Rotation")]
    [SerializeField] private float[] randomXRotations = { -5f, 0f, 5f };

    private void Awake()
    {
        originalParentScale = transform.localScale;

        if (spriteRenderer1 != null)
            originalSprite1Scale =
                spriteRenderer1.transform.localScale;

        if (spriteRenderer2 != null)
            originalSprite2Scale =
                spriteRenderer2.transform.localScale;
    }
    private void OnEnable()
    {
        StartCoroutine(Show());
    }

    IEnumerator Show()
    {
        yield return new WaitForSeconds(0.25f);

        HPAdd();
    }
    // ============================================================
    // SET MAIN SPRITE BY INDEX
    // ============================================================

    public void SetMainSprite(int index)
    {
        if (mainSpriteRenderer == null)
        {
            Debug.LogWarning(
                "HPAdded: Main Sprite Renderer is not assigned!"
            );

            return;
        }

        if (index < 0 || index >= sprites.Count)
        {
            Debug.LogWarning(
                $"HPAdded: Sprite index {index} is invalid. " +
                $"Available indices: 0 - {sprites.Count - 1}"
            );

            return;
        }

        Sprite selectedSprite = sprites[index];

        if (selectedSprite == null)
        {
            Debug.LogWarning(
                $"HPAdded: Sprite at index {index} is empty!"
            );

            return;
        }

        // Set the selected sprite.
        mainSpriteRenderer.sprite = selectedSprite;

        // Make sure it is visible.
        mainSpriteRenderer.enabled = true;

        // -----------------------------------------
        // RANDOM PARENT X ROTATION
        // -----------------------------------------

        float randomX =
            randomXRotations[
                Random.Range(0, randomXRotations.Length)
            ];

        Vector3 currentRotation = transform.localEulerAngles;

        transform.localRotation = Quaternion.Euler(
            randomX,
            currentRotation.y,
            currentRotation.z
        );
    }

    // ============================================================
    // HP ADD
    // ============================================================

    public void HPAdd()
    {
        currentSequence?.Kill();

        // Reset parent.
        transform.localScale = Vector3.zero;

        // Reset both child sprites.
        if (spriteRenderer1 != null)
        {
            spriteRenderer1.transform.localScale =
                originalSprite1Scale;
        }

        if (spriteRenderer2 != null)
        {
            spriteRenderer2.transform.localScale =
                originalSprite2Scale;
        }

        currentSequence = DOTween.Sequence();

        // ========================================================
        // PARENT POP
        // ========================================================

        currentSequence.Append(
            transform.DOScale(
                originalParentScale * popScale,
                popDuration
            ).SetEase(popEase)
        );

        // ========================================================
        // RETURN TO ORIGINAL SCALE
        // ========================================================

        currentSequence.Append(
            transform.DOScale(
                originalParentScale,
                popDuration
            ).SetEase(Ease.OutQuad)
        );

        // ========================================================
        // BOTH SPRITES SCALE DOWN AT THE SAME TIME
        // ========================================================

        if (spriteRenderer1 != null)
        {
            currentSequence.Join(
                spriteRenderer1.transform.DOScale(
                    originalSprite1Scale * sprite1ScaleDown,
                    sprite1ScaleDownDuration
                ).SetEase(sprite1Ease)
            );
        }

        if (spriteRenderer2 != null)
        {
            currentSequence.Join(
                spriteRenderer2.transform.DOScale(
                    originalSprite2Scale * sprite2ScaleDown,
                    sprite2ScaleDownDuration
                ).SetEase(sprite2Ease)
            );
        }
    }

    // ============================================================
    // RESET
    // ============================================================

    public void ResetHPAdded()
    {
        currentSequence?.Kill();

        transform.localScale = originalParentScale;

        if (spriteRenderer1 != null)
        {
            spriteRenderer1.transform.localScale =
                originalSprite1Scale;
        }

        if (spriteRenderer2 != null)
        {
            spriteRenderer2.transform.localScale =
                originalSprite2Scale;
        }
    }
}