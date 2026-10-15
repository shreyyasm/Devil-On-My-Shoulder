using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro; // Requires TextMeshPro (Window > TextMeshPro > Import TMP Essential Resources)
using DG.Tweening;

/// <summary>
/// Defines under what movement condition an input is accepted for a step.
/// </summary>
public enum MovementRequirement
{
    Any,              // doesn't matter if player is moving or not
    MustBeMoving,      // e.g. Shift while walking -> Slide
    MustBeStationary   // e.g. Shift while standing still -> Kick
}

public enum MouseButtonType
{
    None,
    Left,
    Right,
    Middle
}

/// <summary>
/// One step of the tutorial: the text/icon shown, and the input+condition
/// that must occur for the step to be marked complete and move to the next one.
/// </summary>
[System.Serializable]
public class TutorialStep
{
    [Header("Display")]
    [TextArea] public string instructionText;
    public Sprite iconSprite;
    [Tooltip("Context-only steps do not run in the normal tutorial sequence. Trigger them with TryShowContextStep().")]
    public bool contextOnly;

    [Header("Input required to complete this step")]
    public KeyCode[] keys = new KeyCode[0];
    public MouseButtonType mouseButton = MouseButtonType.None;

    [Header("Condition")]
    public MovementRequirement movementRequirement = MovementRequirement.Any;

    [Header("Special case (e.g. Parry)")]
    [Tooltip("If true, pressing the key is not enough on its own - some other " +
             "gameplay system must call SetExternalTriggerFlag(true) first " +
             "(e.g. an enemy projectile is incoming) before the key press counts.")]
    public bool waitForExternalTrigger = false;

    [Tooltip("If true, the prompt stays hidden until SetExternalTriggerFlag(true) is called. Use this for context prompts such as an enemy encounter or a slide room.")]
    public bool waitToShowForExternalTrigger = false;

    [Header("Timing (this step only)")]
    [Tooltip("How long this prompt stays visible after the correct input is given, " +
             "before it hides and the next step appears. Overrides the manager's " +
             "global default delay.")]
    public float delayAfterComplete = 0.4f;

    [Tooltip("Optional: auto-advance this step after this many seconds even if the " +
             "player never presses the input. Set to 0 to disable (wait forever). " +
             "Measured in real (unscaled) time, so it still works even if this step pauses time.")]
    public float autoAdvanceTimeout = 0f;

    [Header("Pause")]
    [Tooltip("If true, Time.timeScale uses the manager's Tutorial Slow Motion value while this step is shown, slowing " +
             "gameplay/physics until the player presses the required input. Good for " +
             "explanatory prompts (e.g. Space to jump). Avoid enabling this on steps " +
             "that require physics-driven movement to be demonstrated (e.g. WASD or " +
             "Slide), since Rigidbody/FixedUpdate motion won't run while paused.")]
    public bool pauseTimeUntilInput = false;

    [Header("Objects")]
    [Tooltip("GameObjects that get SetActive(true) the moment this step is shown, and " +
             "SetActive(false) the moment the step completes (or the tutorial resets). " +
             "Useful for spotlighting a UI arrow, highlighting a prop, spawning a practice " +
             "dummy, disabling a barrier, etc. for just that step.")]
    public List<GameObject> objectsToEnable = new List<GameObject>();

    [HideInInspector] public bool isComplete = false;
    [HideInInspector] public float timeShown = 0f;
}

/// <summary>
/// Drives a sequential, on-screen input tutorial. Shows one prompt at a time,
/// listens for the matching input (plus optional movement condition / external
/// gameplay trigger), then hides that prompt and reveals the next one.
/// </summary>
public class PlayerTutorialManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private Image iconImage;

    [Header("Prompt Image Animation")]
    [SerializeField, Min(0.01f)] private float promptEnterDuration = 0.35f;
    [SerializeField, Min(0f)] private float promptPopStrength = 0.12f;
    [SerializeField, Min(0.01f)] private float promptPopDuration = 0.18f;

    [Header("Tutorial Steps (in order)")]
    [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();

    [Header("Settings")]
    [Tooltip("Used only as the default value when new steps are created in the " +
             "Inspector. Each step's own 'Delay After Complete' is what actually " +
             "controls its timing.")]
    [SerializeField] private float defaultDelayBetweenSteps = 0.4f;
    [SerializeField] private float movementDeadzone = 0.1f;
    [Header("Tutorial Slow Motion")]
    [Tooltip("Time scale used by steps with Pause Time Until Input enabled. Set 1 for no slow motion; values near 0 are slower.")]
    [SerializeField, Range(0.01f, 1f)] private float tutorialSlowMotionScale = 0.15f;
    [Tooltip("If true, movement state must be pushed in via SetExternalMovingState() " +
             "from your own player controller instead of being read from raw WASD axes.")]
    [SerializeField] private bool useExternalMovementState = false;
    [Tooltip("Disabled for Mad Dash so the lift controller can start this tutorial after the player exits the spawn lift.")]
    [SerializeField] private bool startAutomatically = false;
    [Header("Saved Progress")]
    [SerializeField] private bool saveCompletedSteps = true;
    [SerializeField] private string completedStepPreferencePrefix = "MadDashTutorialStep_v1_";

    [Header("Events")]
    public UnityEvent onTutorialCompleted;
    public UnityEvent<int> onStepCompleted;

    private int currentStepIndex = -1;
    private bool isTransitioning = false;
    private bool externalIsMoving = false;
    private bool externalTriggerFlag = false;
    private bool isTimePausedByTutorial = false;
    private bool waitingToShowCurrentStep = false;
    private bool waitingForRoomTransition = false;
    private int currentRoomIndex = int.MinValue;
    private int roomVisitSerial = 0;
    private int requiredRoomVisitForCurrentStep = 0;
    private float cachedTimeScale = 1f;
    private TutorialStep interruptStep;
    private Action interruptCompleted;
    private bool showingInterruptStep;
    private int interruptStepIndex = -1;

    public int CurrentStepIndex => currentStepIndex;
    public int StepCount => steps.Count;
    public bool HasCompletedAllSteps => FindFirstIncompleteStep(0) >= steps.Count;
    public bool IsWaitingForContext => waitingToShowCurrentStep;
    public bool IsShowingPrompt => showingInterruptStep || (tutorialPanel != null && tutorialPanel.activeSelf);
    public bool CanShowInterruptPrompt => !isTransitioning && !IsShowingPrompt;
    public event Action<int> StepCompleted;

    private void Awake()
    {
        // Tutorial prompts are authored as banner sprites. Keep the legacy text
        // object disabled so it cannot overlap a banner in the canvas.
        if (instructionText != null)
            instructionText.gameObject.SetActive(false);

        if (iconImage != null)
            iconImage.preserveAspect = true;
    }

    private void Start()
    {
        if (startAutomatically)
            BeginTutorial();
        else if (tutorialPanel != null && currentStepIndex < 0)
            tutorialPanel.SetActive(false);
    }

    private void Update()
    {
        if (showingInterruptStep)
        {
            UpdateInterruptStep();
            return;
        }

        if (isTransitioning) return;
        if (currentStepIndex < 0 || currentStepIndex >= steps.Count) return;

        // A completed prompt always needs a real room transition before the
        // next one can start timing or receive input.
        if (waitingForRoomTransition)
            return;

        if (waitingToShowCurrentStep)
        {
            if (externalTriggerFlag)
                ShowStep(currentStepIndex);
            return;
        }

        TutorialStep step = steps[currentStepIndex];
        step.timeShown += Time.unscaledDeltaTime;

        // Optional per-step timeout: move on automatically even if the player
        // never performs the input (0 = disabled, wait indefinitely).
        if (step.autoAdvanceTimeout > 0f && step.timeShown >= step.autoAdvanceTimeout)
        {
            CompleteCurrentStep();
            return;
        }

        bool inputTriggered = CheckStepInput(step);
        if (!inputTriggered) return;

        if (!IsMovementRequirementMet(step)) return;

        // For steps like "Parry", the key press only counts once gameplay
        // has flagged that the moment is relevant (e.g. projectile incoming).
        if (step.waitForExternalTrigger && !externalTriggerFlag) return;

        CompleteCurrentStep();
    }

    private void UpdateInterruptStep()
    {
        if (interruptStep == null)
            return;

        interruptStep.timeShown += Time.unscaledDeltaTime;
        if (interruptStep.autoAdvanceTimeout > 0f && interruptStep.timeShown >= interruptStep.autoAdvanceTimeout)
        {
            CompleteInterruptStep();
            return;
        }

        if (CheckStepInput(interruptStep) && IsMovementRequirementMet(interruptStep))
            CompleteInterruptStep();
    }

    private bool CheckStepInput(TutorialStep step)
    {
        if (step.mouseButton != MouseButtonType.None)
        {
            int btnIndex = step.mouseButton == MouseButtonType.Left ? 0 :
                           step.mouseButton == MouseButtonType.Right ? 1 : 2;
            return Input.GetMouseButtonDown(btnIndex);
        }

        if (step.keys != null && step.keys.Length > 0)
        {
            foreach (var k in step.keys)
            {
                if (Input.GetKeyDown(k)) return true;
            }
        }

        return false;
    }

    private bool IsMovementRequirementMet(TutorialStep step)
    {
        bool moving = useExternalMovementState ? externalIsMoving : IsMovingRaw();

        switch (step.movementRequirement)
        {
            case MovementRequirement.MustBeMoving: return moving;
            case MovementRequirement.MustBeStationary: return !moving;
            default: return true;
        }
    }

    private bool IsMovingRaw()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        return new Vector2(h, v).sqrMagnitude > movementDeadzone * movementDeadzone;
    }

    /// <summary>
    /// Call this from your player controller every frame (or on state change)
    /// if useExternalMovementState is enabled, so slide/kick steps read the
    /// same "am I moving" logic your actual gameplay uses.
    /// </summary>
    public void SetExternalMovingState(bool moving)
    {
        externalIsMoving = moving;
    }

    /// <summary>
    /// Call this from your combat/projectile system to open or close the
    /// window during which a "waitForExternalTrigger" step (e.g. Parry) can
    /// be completed. Example: set true when an enemy projectile is about to
    /// arrive, set false once it has passed or been parried.
    /// </summary>
    public void SetExternalTriggerFlag(bool flag)
    {
        externalTriggerFlag = flag;
        if (flag && waitingToShowCurrentStep && currentStepIndex >= 0 && currentStepIndex < steps.Count)
            ShowStep(currentStepIndex);
    }

    /// <summary>
    /// Called by the generated room trigger. Every tutorial step after the
    /// first is held until this reports entry into a different room.
    /// </summary>
    public void NotifyRoomEntered(int roomIndex)
    {
        if (roomIndex == currentRoomIndex)
            return;

        currentRoomIndex = roomIndex;
        roomVisitSerial++;

        if (waitingForRoomTransition && roomVisitSerial >= requiredRoomVisitForCurrentStep)
            ShowStep(currentStepIndex);
    }

    /// <summary>
    /// Shows a one-off contextual prompt without changing the main sequence.
    /// Use this for combat and room tutorials that can happen at different times.
    /// </summary>
    public bool TryShowInterruptPrompt(TutorialStep step, Action onCompleted = null)
    {
        if (step == null || !CanShowInterruptPrompt)
            return false;

        interruptStep = step;
        interruptStepIndex = -1;
        interruptCompleted = onCompleted;
        showingInterruptStep = true;
        interruptStep.timeShown = 0f;

        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);
        SetPromptSprite(interruptStep);

        if (interruptStep.pauseTimeUntilInput)
            PauseTime();
        SetStepObjectsActive(interruptStep, true);
        return true;
    }

    /// <summary>
    /// Shows an Inspector-authored context step (such as a skill-room or enemy
    /// encounter prompt) without putting it into the normal tutorial sequence.
    /// The completion is saved exactly like a normal step.
    /// </summary>
    public bool TryShowContextStep(int index)
    {
        if (index < 0 || index >= steps.Count || !steps[index].contextOnly ||
            IsStepCompleted(index) || !CanShowInterruptPrompt)
            return false;

        interruptStep = steps[index];
        interruptStepIndex = index;
        interruptCompleted = null;
        showingInterruptStep = true;
        interruptStep.timeShown = 0f;

        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);
        SetPromptSprite(interruptStep);

        if (interruptStep.pauseTimeUntilInput)
            PauseTime();
        SetStepObjectsActive(interruptStep, true);
        return true;
    }

    private void CompleteCurrentStep()
    {
        steps[currentStepIndex].isComplete = true;
        SaveStepCompletion(currentStepIndex);
        // Context flags belong to one step only. Clearing here prevents a previous encounter from revealing the next contextual prompt.
        externalTriggerFlag = false;
        ResumeTimeIfPaused();
        SetStepObjectsActive(steps[currentStepIndex], false);
        onStepCompleted?.Invoke(currentStepIndex);
        StepCompleted?.Invoke(currentStepIndex);
        requiredRoomVisitForCurrentStep = roomVisitSerial + 1;
        StartCoroutine(TransitionToNextStep());
    }

    private void CompleteInterruptStep()
    {
        TutorialStep completedStep = interruptStep;
        int completedIndex = interruptStepIndex;
        interruptStep = null;
        interruptStepIndex = -1;
        showingInterruptStep = false;
        ResumeTimeIfPaused();
        SetStepObjectsActive(completedStep, false);
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        if (completedIndex >= 0)
        {
            completedStep.isComplete = true;
            SaveStepCompletion(completedIndex);
            onStepCompleted?.Invoke(completedIndex);
            StepCompleted?.Invoke(completedIndex);
        }

        Action callback = interruptCompleted;
        interruptCompleted = null;
        callback?.Invoke();
    }

    private IEnumerator TransitionToNextStep()
    {
        isTransitioning = true;

        float delay = steps[currentStepIndex].delayAfterComplete;

        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        // Realtime wait so the hide-delay still elapses even if time is scaled/paused.
        yield return new WaitForSecondsRealtime(delay);

        int next = FindFirstIncompleteStep(currentStepIndex + 1);
        if (next < steps.Count)
        {
            ShowStep(next);
        }
        else
        {
            currentStepIndex = steps.Count;
            onTutorialCompleted?.Invoke();
        }

        isTransitioning = false;
    }

    private void ShowStep(int index)
    {
        currentStepIndex = index;
        TutorialStep step = steps[index];

        if (index > 0 && roomVisitSerial < requiredRoomVisitForCurrentStep)
        {
            waitingForRoomTransition = true;
            waitingToShowCurrentStep = false;
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            return;
        }

        waitingForRoomTransition = false;

        if (step.waitToShowForExternalTrigger && !externalTriggerFlag)
        {
            waitingToShowCurrentStep = true;
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            return;
        }

        waitingToShowCurrentStep = false;
        step.timeShown = 0f;

        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        SetPromptSprite(step);

        if (step.pauseTimeUntilInput)
        {
            PauseTime();
        }

        SetStepObjectsActive(step, true);
    }

    private void SetStepObjectsActive(TutorialStep step, bool active)
    {
        if (step.objectsToEnable == null) return;
        foreach (var obj in step.objectsToEnable)
        {
            if (obj != null) obj.SetActive(active);
        }
    }

    private void SetPromptSprite(TutorialStep step)
    {
        if (iconImage == null)
            return;

        iconImage.sprite = step != null ? step.iconSprite : null;
        iconImage.enabled = iconImage.sprite != null;

        if (iconImage.enabled)
            PlayPromptImageAnimation();
    }

    private void PlayPromptImageAnimation()
    {
        RectTransform promptTransform = iconImage.rectTransform;
        promptTransform.DOKill();
        promptTransform.localScale = Vector3.zero;
        promptTransform.localRotation = Quaternion.identity;

        // SetUpdate(true) makes the banner animation independent of Time.timeScale.
        DOTween.Sequence()
            .SetUpdate(true)
            .Append(promptTransform.DOScale(Vector3.one, promptEnterDuration).SetEase(Ease.OutBack))
            .Join(promptTransform.DOLocalRotate(new Vector3(0f, 0f, -360f), promptEnterDuration, RotateMode.FastBeyond360).SetEase(Ease.OutCubic))
            .Append(promptTransform.DOPunchScale(Vector3.one * promptPopStrength, promptPopDuration, 8, 0.7f));
    }

    private void PauseTime()
    {
        if (isTimePausedByTutorial) return; // already paused by us, don't overwrite the cached value
        cachedTimeScale = Time.timeScale;
        Time.timeScale = tutorialSlowMotionScale;
        isTimePausedByTutorial = true;
    }

    private void ResumeTimeIfPaused()
    {
        if (!isTimePausedByTutorial) return;
        Time.timeScale = cachedTimeScale;
        isTimePausedByTutorial = false;
    }

    private void OnDisable()
    {
        // Safety net: never leave the game frozen, or step objects stuck active,
        // if this component is disabled mid-step.
        ResumeTimeIfPaused();
        if (currentStepIndex >= 0 && currentStepIndex < steps.Count)
        {
            SetStepObjectsActive(steps[currentStepIndex], false);
        }
        if (interruptStep != null)
            SetStepObjectsActive(interruptStep, false);
        if (iconImage != null)
            iconImage.rectTransform.DOKill();
    }

    private void OnDestroy()
    {
        ResumeTimeIfPaused();
    }

    /// <summary>Begins the configured sequence. Safe to call after a lift or cinematic gate.</summary>
    public void BeginTutorial()
    {
        StopAllCoroutines();
        isTransitioning = false;
        waitingToShowCurrentStep = false;
        waitingForRoomTransition = false;
        currentRoomIndex = int.MinValue;
        roomVisitSerial = 0;
        requiredRoomVisitForCurrentStep = 0;
        externalTriggerFlag = false;
        ResumeTimeIfPaused();

        foreach (TutorialStep step in steps)
        {
            step.isComplete = IsStepCompleted(step);
            step.timeShown = 0f;
            SetStepObjectsActive(step, false);
        }

        int firstIncomplete = FindFirstIncompleteStep(0);
        if (firstIncomplete < steps.Count)
        {
            requiredRoomVisitForCurrentStep = firstIncomplete > 0 ? roomVisitSerial + 1 : roomVisitSerial;
            ShowStep(firstIncomplete);
        }
        else if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }

    public void ConfigureSteps(List<TutorialStep> configuredSteps)
    {
        steps = configuredSteps ?? new List<TutorialStep>();
    }

    /// <summary>
    /// Changes only the runtime gate and timeout of an Inspector-authored step.
    /// Its authored wording, key, icon, pause setting and other data stay intact.
    /// </summary>
    public void ConfigureExistingStepGate(int index, bool waitForContext, float autoAdvanceTimeout = -1f)
    {
        if (index < 0 || index >= steps.Count)
            return;

        steps[index].waitToShowForExternalTrigger = waitForContext;
        if (autoAdvanceTimeout >= 0f)
            steps[index].autoAdvanceTimeout = autoAdvanceTimeout;
    }

    public bool IsStepCompleted(int index)
    {
        return index >= 0 && index < steps.Count && IsStepCompleted(steps[index]);
    }

    private bool IsStepCompleted(TutorialStep step)
    {
        int index = steps.IndexOf(step);
        return step.isComplete || (saveCompletedSteps && index >= 0 && PlayerPrefs.GetInt(GetStepPreferenceKey(index), 0) == 1);
    }

    private int FindFirstIncompleteStep(int startIndex)
    {
        for (int index = Mathf.Max(0, startIndex); index < steps.Count; index++)
        {
            if (steps[index].contextOnly)
                continue;

            if (!IsStepCompleted(steps[index]))
                return index;
        }
        return steps.Count;
    }

    private void SaveStepCompletion(int index)
    {
        if (!saveCompletedSteps || index < 0 || index >= steps.Count)
            return;

        PlayerPrefs.SetInt(GetStepPreferenceKey(index), 1);
        PlayerPrefs.Save();
    }

    private string GetStepPreferenceKey(int index)
    {
        return completedStepPreferencePrefix + index;
    }

    [ContextMenu("Clear Saved Tutorial Progress")]
    public void ClearSavedTutorialProgress()
    {
        for (int index = 0; index < steps.Count; index++)
            PlayerPrefs.DeleteKey(GetStepPreferenceKey(index));
        PlayerPrefs.Save();
    }
    /// <summary>Manually force-complete the current step (useful for debugging or skip buttons).</summary>
    public void ForceCompleteCurrentStep()
    {
        if (currentStepIndex >= 0 && currentStepIndex < steps.Count)
        {
            CompleteCurrentStep();
        }
    }

    /// <summary>Restart the whole tutorial from step 0.</summary>
    public void ResetTutorial()
    {
        StopAllCoroutines();
        isTransitioning = false;
        externalTriggerFlag = false;
        waitingForRoomTransition = false;
        currentRoomIndex = int.MinValue;
        roomVisitSerial = 0;
        requiredRoomVisitForCurrentStep = 0;
        ResumeTimeIfPaused();

        if (currentStepIndex >= 0 && currentStepIndex < steps.Count)
        {
            SetStepObjectsActive(steps[currentStepIndex], false);
        }

        foreach (var s in steps)
        {
            s.isComplete = IsStepCompleted(s);
            s.timeShown = 0f;
        }

        int firstIncomplete = FindFirstIncompleteStep(0);
        if (firstIncomplete < steps.Count)
        {
            requiredRoomVisitForCurrentStep = firstIncomplete > 0 ? roomVisitSerial + 1 : roomVisitSerial;
            ShowStep(firstIncomplete);
        }
    }

    /// <summary>
    /// Fills the Steps list in the Inspector with the exact sequence you described:
    /// WASD -> Space -> Shift(move)=Slide -> Shift(still)=Kick -> Shift=Parry -> RMB=Finisher.
    /// Right-click the component header in the Inspector and choose this to auto-populate,
    /// then just assign icon sprites and tweak wording.
    /// </summary>
    [ContextMenu("Load Default Tutorial Sequence")]
    private void LoadDefaultSteps()
    {
        steps = new List<TutorialStep>
        {
            new TutorialStep
            {
                instructionText = "Press W / A / S / D to move",
                keys = new [] { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D },
                movementRequirement = MovementRequirement.Any,
                delayAfterComplete = 0.3f,   // hides quickly once they've clearly got it
                autoAdvanceTimeout = 0f      // wait indefinitely, movement is mandatory to proceed
            },
            new TutorialStep
            {
                instructionText = "Press SPACE to jump",
                keys = new [] { KeyCode.Space },
                movementRequirement = MovementRequirement.Any,
                delayAfterComplete = 0.3f,
                autoAdvanceTimeout = 0f
            },
            new TutorialStep
            {
                instructionText = "Hold SHIFT while moving to Slide",
                keys = new [] { KeyCode.LeftShift, KeyCode.RightShift },
                movementRequirement = MovementRequirement.MustBeMoving,
                delayAfterComplete = 0.5f,   // slide has a visible animation, give it a beat
                autoAdvanceTimeout = 0f
            },
            new TutorialStep
            {
                instructionText = "Press SHIFT while standing still to Kick",
                keys = new [] { KeyCode.LeftShift, KeyCode.RightShift },
                movementRequirement = MovementRequirement.MustBeStationary,
                delayAfterComplete = 0.5f,
                autoAdvanceTimeout = 0f
            },
            new TutorialStep
            {
                instructionText = "Press SHIFT to Parry incoming enemy projectiles",
                keys = new [] { KeyCode.LeftShift, KeyCode.RightShift },
                movementRequirement = MovementRequirement.Any,
                waitForExternalTrigger = true, // your projectile/combat system must call SetExternalTriggerFlag(true)
                delayAfterComplete = 0.6f,
                autoAdvanceTimeout = 12f       // e.g. skip after 12s in case no projectile ever spawns
            },
            new TutorialStep
            {
                instructionText = "Right-click (RMB) to use your Finisher / Demon Ability",
                mouseButton = MouseButtonType.Right,
                movementRequirement = MovementRequirement.Any,
                delayAfterComplete = 0.4f,
                autoAdvanceTimeout = 0f
            }
        };
    }
}
