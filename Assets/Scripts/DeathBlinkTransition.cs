using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Screen-space eyelid transition for player death. It blinks once, then closes
/// completely in time with the final impact of PlayerDeathFall.
/// </summary>
[DisallowMultipleComponent]
public class DeathBlinkTransition : MonoBehaviour
{
    public static DeathBlinkTransition Instance { get; private set; }

    [Header("Timing")]
    [SerializeField, Range(0.05f, 0.45f)] private float firstClosePortion = 0.25f;
    [SerializeField, Range(0.05f, 0.45f)] private float reopenPortion = 0.2f;
    [SerializeField, Range(0f, 1f)] private float firstBlinkClosure = 0.42f;
    [SerializeField, Range(0f, 1f)] private float reopenAmount = 0.08f;
    [SerializeField, Min(0f)] private float scoreCanvasDelay = 0.6f;

    [Header("Death Blur")]
    [SerializeField, Min(0.1f)] private float maximumBlurRadius = 2.5f;
    [SerializeField, Min(0.01f)] private float blurStartDistance = 0.1f;
    [SerializeField, Min(0.02f)] private float blurEndDistance = 0.25f;

    [Header("Overlay")]
    [SerializeField] private int sortingOrder = 5000;
    [SerializeField] private Color eyelidColor = Color.black;
    [SerializeField, Range(0f, 0.2f)] private float eyelidFeatherOverlap = 0.08f;

    private Canvas overlayCanvas;
    private RectTransform topLid;
    private RectTransform bottomLid;
    private bool playing;
    private float duration;
    private float elapsed;
    private Action onCovered;

    private Volume globalVolume;
    private DepthOfField depthOfField;
    private bool blurCreated;
    private bool blurStateCaptured;
    private bool originalBlurActive;
    private DepthOfFieldMode originalBlurMode;
    private float originalBlurStart;
    private float originalBlurEnd;
    private float originalBlurRadius;
    private bool originalStartOverride;
    private bool originalEndOverride;
    private bool originalRadiusOverride;
    private bool originalModeOverride;

    public int ScoreCanvasSortingOrder => sortingOrder + 1;

    public void Cancel()
    {
        playing = false;
        elapsed = 0f;
        onCovered = null;
        StopAllCoroutines();
        RestoreBlur();

        if (overlayCanvas != null)
        {
            overlayCanvas.gameObject.SetActive(false);
            SetClosure(0f);
        }
    }

    private void Awake()
    {
        Instance = this;
        // Build the UI while the scene loads rather than at the instant of death.
        // This avoids relying on a runtime-created sprite/material path that can be
        // stripped from a standalone player build.
        EnsureOverlay();
    }

    private void OnDestroy()
    {
        RestoreBlur();
        if (Instance == this)
            Instance = null;
    }

    public void Play(float totalDuration, Action coveredCallback)
    {
        EnsureOverlay();
        if (overlayCanvas == null)
        {
            coveredCallback?.Invoke();
            return;
        }

        duration = Mathf.Max(0.1f, totalDuration);
        elapsed = 0f;
        onCovered = coveredCallback;
        playing = true;
        overlayCanvas.gameObject.SetActive(true);
        SetClosure(0f);
        ResolveBlur();
        SetBlur(0f);
    }

    private void Update()
    {
        if (!playing)
            return;

        elapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(elapsed / duration);
        float closure = EvaluateClosure(progress);
        SetClosure(closure);
        // Blur rises with the eyelid movement and becomes strongest at the final impact.
        float blurStrength = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.05f, 0.9f, closure));
        SetBlur(blurStrength);

        if (progress < 1f)
            return;

        playing = false;
        StartCoroutine(FinishTransition());
    }

    private IEnumerator FinishTransition()
    {
        if (scoreCanvasDelay > 0f)
            yield return new WaitForSecondsRealtime(scoreCanvasDelay);
        // Keep the eyelids closed behind the score screen. The scene must not flash back in.
        RestoreBlur();
        onCovered?.Invoke();
        onCovered = null;
    }

    private float EvaluateClosure(float progress)
    {
        float firstCloseEnd = Mathf.Clamp01(firstClosePortion);
        float reopenEnd = Mathf.Clamp01(firstCloseEnd + reopenPortion);

        if (progress < firstCloseEnd)
            return Mathf.SmoothStep(0f, firstBlinkClosure, progress / Mathf.Max(0.001f, firstCloseEnd));

        if (progress < reopenEnd)
            return Mathf.SmoothStep(firstBlinkClosure, reopenAmount,
                (progress - firstCloseEnd) / Mathf.Max(0.001f, reopenEnd - firstCloseEnd));

        return Mathf.SmoothStep(reopenAmount, 1f,
            (progress - reopenEnd) / Mathf.Max(0.001f, 1f - reopenEnd));
    }

    private void SetClosure(float closure)
    {
        if (topLid == null || bottomLid == null)
            return;

        float closureAmount = Mathf.Clamp01(closure);
        float halfScreenHeight = Screen.height * 0.5f * closureAmount;
        // Overlap the soft inner edges so a full closure is genuinely black.
        float overlap = Screen.height * eyelidFeatherOverlap * closureAmount;
        topLid.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, halfScreenHeight + overlap);
        bottomLid.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, halfScreenHeight + overlap);
    }

    private void ResolveBlur()
    {
        if (depthOfField != null)
            return;

        globalVolume = FindFirstObjectByType<Volume>();
        if (globalVolume == null || globalVolume.profile == null)
            return;

        if (!globalVolume.profile.TryGet(out depthOfField))
        {
            depthOfField = globalVolume.profile.Add<DepthOfField>(true);
            blurCreated = true;
        }

        originalBlurActive = depthOfField.active;
        originalBlurMode = depthOfField.mode.value;
        originalBlurStart = depthOfField.gaussianStart.value;
        originalBlurEnd = depthOfField.gaussianEnd.value;
        originalBlurRadius = depthOfField.gaussianMaxRadius.value;
        originalModeOverride = depthOfField.mode.overrideState;
        originalStartOverride = depthOfField.gaussianStart.overrideState;
        originalEndOverride = depthOfField.gaussianEnd.overrideState;
        originalRadiusOverride = depthOfField.gaussianMaxRadius.overrideState;
        blurStateCaptured = true;
    }

    private void SetBlur(float strength)
    {
        if (depthOfField == null)
            return;

        depthOfField.active = strength > 0.001f;
        depthOfField.mode.overrideState = true;
        depthOfField.mode.value = DepthOfFieldMode.Gaussian;
        depthOfField.gaussianStart.overrideState = true;
        depthOfField.gaussianEnd.overrideState = true;
        depthOfField.gaussianMaxRadius.overrideState = true;
        depthOfField.gaussianStart.value = blurStartDistance;
        depthOfField.gaussianEnd.value = blurEndDistance;
        depthOfField.gaussianMaxRadius.value = Mathf.Lerp(0f, maximumBlurRadius, strength);
    }

    private void RestoreBlur()
    {
        if (depthOfField == null || !blurStateCaptured)
            return;

        depthOfField.active = blurCreated ? false : originalBlurActive;
        depthOfField.mode.value = originalBlurMode;
        depthOfField.gaussianStart.value = originalBlurStart;
        depthOfField.gaussianEnd.value = originalBlurEnd;
        depthOfField.gaussianMaxRadius.value = originalBlurRadius;
        depthOfField.mode.overrideState = originalModeOverride;
        depthOfField.gaussianStart.overrideState = originalStartOverride;
        depthOfField.gaussianEnd.overrideState = originalEndOverride;
        depthOfField.gaussianMaxRadius.overrideState = originalRadiusOverride;
        blurStateCaptured = false;
        depthOfField = null;
        blurCreated = false;
    }

    private void EnsureOverlay()
    {
        if (overlayCanvas != null)
            return;

        GameObject canvasObject = new GameObject("Death Blink Overlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        overlayCanvas = canvasObject.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = sortingOrder;
        canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.GetComponent<GraphicRaycaster>().enabled = false;

        topLid = CreateLid("Upper Eyelid", canvasObject.transform, true);
        bottomLid = CreateLid("Lower Eyelid", canvasObject.transform, false);
        canvasObject.SetActive(false);
    }

    private RectTransform CreateLid(string lidName, Transform parent, bool upper)
    {
        // CanvasRenderer is explicit here so standalone builds do not depend on the
        // editor adding Image's required component during object construction.
        GameObject lidObject = new GameObject(lidName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lidObject.transform.SetParent(parent, false);

        RectTransform rect = lidObject.GetComponent<RectTransform>();
        rect.anchorMin = upper ? new Vector2(0f, 1f) : Vector2.zero;
        rect.anchorMax = upper ? Vector2.one : new Vector2(1f, 0f);
        rect.pivot = upper ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);

        Image image = lidObject.GetComponent<Image>();
        // Leave sprite empty so Unity uses its built-in white UI sprite. Tinting it
        // black gives the eyelid shape without a generated texture or custom shader,
        // both of which are candidates for stripping from a player build.
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = eyelidColor;
        image.raycastTarget = false;
        return rect;
    }
}
