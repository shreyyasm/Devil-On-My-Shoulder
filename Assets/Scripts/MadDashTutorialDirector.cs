using System.Collections;
using UnityEngine;

/// <summary>
/// Releases the authored Player Tutorial Manager steps at the right gameplay
/// moments. It never replaces the Inspector-authored prompt text or bindings.
/// </summary>
public class MadDashTutorialDirector : MonoBehaviour
{
    public static MadDashTutorialDirector Instance { get; private set; }

    private const int FireStep = 1;
    private const int HeartStep = 2;
    private const int KickStep = 3;
    private const int DashSlideStep = 4;
    private const int FinisherStep = 5;
    private const int DemonStep = 6;
    private const int ParryStep = 7;
    private const int JumpDashStep = 8;
    private const int SlideTunnelsStep = 9;

    [Header("References")]
    [SerializeField] private PlayerTutorialManager tutorialManager;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float firePromptDelay = 1f;
    [SerializeField, Min(1f)] private float enemyEncounterDistance = 7f;
    [SerializeField] private bool onlyRunOnActiveSetZero = true;

    private Transform player;
    private CharacterAbilities characterAbilities;
    private PlayerHealth playerHealth;
    private bool tutorialStarted;
    private bool hasLeftSpawnRoom;
    private bool firePromptReleased;
    private int currentRoomIndex = -1;
    private bool normalHealthDrainRestored;
    private float normalHealthDrainRate;
    private bool slideRoomPromptQueued;
    private bool parryPromptQueued;
    private bool jumpRoomPromptQueued;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        if (tutorialManager == null)
            tutorialManager = GetComponent<PlayerTutorialManager>();

        FindPlayerReferences();
        if (tutorialManager != null)
        {
            // Keep the authored text and inputs. Only control when each step can appear.
            tutorialManager.ConfigureExistingStepGate(FireStep, true);
            tutorialManager.ConfigureExistingStepGate(HeartStep, true, 3f);
            tutorialManager.ConfigureExistingStepGate(KickStep, true);
            tutorialManager.ConfigureExistingStepGate(DashSlideStep, true);
            tutorialManager.ConfigureExistingStepGate(FinisherStep, true);
            tutorialManager.ConfigureExistingStepGate(DemonStep, true);
            tutorialManager.StepCompleted += OnStepCompleted;
        }
    }

    private void Start()
    {
        if (tutorialManager == null || tutorialStarted || !CanRunTutorialForCurrentSet())
            return;

        if (tutorialManager.HasCompletedAllSteps)
            return;

        tutorialStarted = true;
        // Keep the drain gentle only until the player has seen and completed
        // the health instruction. Its completion is persisted by PlayerTutorialManager.
        if (!tutorialManager.IsStepCompleted(HeartStep))
            SetTutorialHealthDrain();
        tutorialManager.BeginTutorial();
    }

    private void OnDestroy()
    {
        if (tutorialManager != null)
            tutorialManager.StepCompleted -= OnStepCompleted;
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!tutorialStarted || tutorialManager == null)
            return;

        if (player == null || characterAbilities == null)
            FindPlayerReferences();

        ReleaseCurrentStepWhenReady();
        QueueMessLadyParryPrompt();
        ReleaseQueuedContextPrompts();
    }

    /// <summary>Called when the player clears the spawn-lift corridor.</summary>
    public void NotifyPlayerExitedSpawnRoom()
    {
        if (!tutorialStarted || hasLeftSpawnRoom)
            return;

        hasLeftSpawnRoom = true;
        StartCoroutine(ReleaseFireAfterDelay());
    }

    /// <summary>Called by RoomTrigger. OnTriggerStay repeats are ignored.</summary>
    public void NotifyRoomEntered(int roomIndex)
    {
        NotifyRoomEntered(roomIndex, string.Empty);
    }

    /// <summary>Called by RoomTrigger with the spawned room hierarchy so skill-room prompts only fire in their own rooms.</summary>
    public void NotifyRoomEntered(int roomIndex, string roomIdentity)
    {
        if (!tutorialStarted || roomIndex == currentRoomIndex)
            return;

        currentRoomIndex = roomIndex;
        tutorialManager?.NotifyRoomEntered(roomIndex);

        if (IsSkillRoom(roomIdentity, "Slide"))
            slideRoomPromptQueued = true;
        if (IsSkillRoom(roomIdentity, "Jump"))
            jumpRoomPromptQueued = true;

        ReleaseQueuedContextPrompts();
    }

    private IEnumerator ReleaseFireAfterDelay()
    {
        yield return new WaitForSecondsRealtime(firePromptDelay);
        firePromptReleased = true;
    }

    private void ReleaseCurrentStepWhenReady()
    {
        switch (tutorialManager.CurrentStepIndex)
        {
            case FireStep:
                if (firePromptReleased)
                    tutorialManager.SetExternalTriggerFlag(true);
                break;

            // This prompt appears only after the fire prompt and in a later room.
            case HeartStep:
                tutorialManager.SetExternalTriggerFlag(HasNearbyEnemy());
                break;

            // The authored kick prompt deliberately does not pause time.
            case KickStep:
                tutorialManager.SetExternalTriggerFlag(HasNearbyEnemy());
                break;

            case FinisherStep:
                bool finisherAvailable = characterAbilities != null && characterAbilities.finisherReady && !characterAbilities.abilityReady && HasNearbyEnemy();
                tutorialManager.SetExternalTriggerFlag(finisherAvailable);
                break;

            case DemonStep:
                tutorialManager.SetExternalTriggerFlag(characterAbilities != null && characterAbilities.abilityReady && HasNearbyEnemy());
                break;
        }
    }

    private void OnStepCompleted(int completedStep)
    {
        // Per-step progress is saved by PlayerTutorialManager before this event.
        // Once the health instruction is completed, return to the normal
        // inspector-authored depletion rate for the rest of this run.
        if (completedStep == HeartStep)
            RestoreNormalHealthDrain();
    }

    private void QueueMessLadyParryPrompt()
    {
        if (parryPromptQueued || tutorialManager.IsStepCompleted(ParryStep) || player == null)
            return;

        foreach (EnemyManager enemy in FindObjectsOfType<EnemyManager>())
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy || !IsMessLady(enemy.transform) ||
                (enemy.transform.position - player.position).sqrMagnitude > enemyEncounterDistance * enemyEncounterDistance)
                continue;

            parryPromptQueued = true;
            return;
        }
    }

    private void ReleaseQueuedContextPrompts()
    {
        if (tutorialManager == null)
            return;

        // Only one banner may be on screen at a time. Queued prompts remain
        // pending until the player has finished the current instruction.
        if (slideRoomPromptQueued && tutorialManager.TryShowContextStep(SlideTunnelsStep))
        {
            slideRoomPromptQueued = false;
            return;
        }

        if (parryPromptQueued && tutorialManager.TryShowContextStep(ParryStep))
        {
            parryPromptQueued = false;
            return;
        }

        if (jumpRoomPromptQueued && tutorialManager.TryShowContextStep(JumpDashStep))
            jumpRoomPromptQueued = false;
    }

    private static bool IsSkillRoom(string roomIdentity, string skillName)
    {
        return !string.IsNullOrEmpty(roomIdentity) &&
               roomIdentity.IndexOf("Skill", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
               roomIdentity.IndexOf(skillName, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsMessLady(Transform enemyTransform)
    {
        for (Transform current = enemyTransform; current != null; current = current.parent)
        {
            if (current.name.IndexOf("MessLady", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                current.name.IndexOf("Mess Lady", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private bool CanRunTutorialForCurrentSet()
    {
        return !onlyRunOnActiveSetZero || ProceduralGenerationManager.Instance == null || ProceduralGenerationManager.Instance.ActiveSet == 0;
    }

    private void FindPlayerReferences()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
            return;

        player = playerObject.transform;
        characterAbilities = playerObject.GetComponent<CharacterAbilities>();
        playerHealth = playerObject.GetComponent<PlayerHealth>();
    }

    private void SetTutorialHealthDrain()
    {
        if (playerHealth == null)
            FindPlayerReferences();
        if (playerHealth == null)
            return;

        normalHealthDrainRate = playerHealth.healthDecreaseRate;
        playerHealth.healthDecreaseRate = 1f;
        normalHealthDrainRestored = false;
    }

    private void RestoreNormalHealthDrain()
    {
        if (normalHealthDrainRestored || playerHealth == null)
            return;

        playerHealth.healthDecreaseRate = normalHealthDrainRate;
        normalHealthDrainRestored = true;
    }

    private bool HasNearbyEnemy()
    {
        if (player == null)
            return false;

        foreach (EnemyManager enemy in FindObjectsOfType<EnemyManager>())
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy &&
                (enemy.transform.position - player.position).sqrMagnitude <= enemyEncounterDistance * enemyEncounterDistance)
                return true;
        }

        return false;
    }

}
