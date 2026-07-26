using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro; // Requires TextMeshPro (Window > TextMeshPro > Import TMP Essential Resources)

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
    [Tooltip("If true, Time.timeScale is set to 0 while this step is shown, freezing " +
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

    [Header("Tutorial Steps (in order)")]
    [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();

    [Header("Settings")]
    [Tooltip("Used only as the default value when new steps are created in the " +
             "Inspector. Each step's own 'Delay After Complete' is what actually " +
             "controls its timing.")]
    [SerializeField] private float defaultDelayBetweenSteps = 0.4f;
    [SerializeField] private float movementDeadzone = 0.1f;
    [Tooltip("If true, movement state must be pushed in via SetExternalMovingState() " +
             "from your own player controller instead of being read from raw WASD axes.")]
    [SerializeField] private bool useExternalMovementState = false;

    [Header("Events")]
    public UnityEvent onTutorialCompleted;
    public UnityEvent<int> onStepCompleted;

    private int currentStepIndex = -1;
    private bool isTransitioning = false;
    private bool externalIsMoving = false;
    private bool externalTriggerFlag = false;
    private bool isTimePausedByTutorial = false;
    private float cachedTimeScale = 1f;

    private void Start()
    {
        if (steps.Count > 0)
        {
            ShowStep(0);
        }
        else if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (isTransitioning) return;
        if (currentStepIndex < 0 || currentStepIndex >= steps.Count) return;

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
    }

    private void CompleteCurrentStep()
    {
        steps[currentStepIndex].isComplete = true;
        ResumeTimeIfPaused();
        SetStepObjectsActive(steps[currentStepIndex], false);
        onStepCompleted?.Invoke(currentStepIndex);
        StartCoroutine(TransitionToNextStep());
    }

    private IEnumerator TransitionToNextStep()
    {
        isTransitioning = true;

        float delay = steps[currentStepIndex].delayAfterComplete;

        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        // Realtime wait so the hide-delay still elapses even if time is scaled/paused.
        yield return new WaitForSecondsRealtime(delay);

        int next = currentStepIndex + 1;
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
        step.timeShown = 0f;

        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        if (instructionText != null) instructionText.text = step.instructionText;

        if (iconImage != null)
        {
            iconImage.sprite = step.iconSprite;
            iconImage.enabled = step.iconSprite != null;
        }

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

    private void PauseTime()
    {
        if (isTimePausedByTutorial) return; // already paused by us, don't overwrite the cached value
        cachedTimeScale = Time.timeScale;
        Time.timeScale = 0f;
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
    }

    private void OnDestroy()
    {
        ResumeTimeIfPaused();
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
        ResumeTimeIfPaused();

        if (currentStepIndex >= 0 && currentStepIndex < steps.Count)
        {
            SetStepObjectsActive(steps[currentStepIndex], false);
        }

        foreach (var s in steps)
        {
            s.isComplete = false;
            s.timeShown = 0f;
        }

        if (steps.Count > 0) ShowStep(0);
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