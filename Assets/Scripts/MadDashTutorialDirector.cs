using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runs the first two Mad Dash tutorial prompts on Active Set 0.
/// Movement is shown as soon as the scene starts; firing is shown only when
/// the player can see an enemy in the first room.
/// </summary>
public class MadDashTutorialDirector : MonoBehaviour
{
    public static MadDashTutorialDirector Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerTutorialManager tutorialManager;

    [Header("First-room enemy detection")]
    [SerializeField, Min(1f)] private float enemyEncounterDistance = 14f;
    [SerializeField] private bool onlyRunOnActiveSetZero = true;

    private Transform player;
    private Camera playerCamera;
    private bool tutorialStarted;
    private bool hasLeftSpawnRoom;

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
        tutorialManager?.ConfigureSteps(CreateFirstRoomSteps());
    }

    private void Start()
    {
        // This happens immediately when Mad Dash begins, while the player is still in the spawn lift.
        BeginFirstRoomTutorial();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!tutorialStarted || tutorialManager == null)
            return;

        if (player == null || playerCamera == null)
            FindPlayerReferences();

        // Step 1 is the fire prompt. Keep it hidden until an enemy is visible on screen.
        bool enemyIsVisible = tutorialManager.CurrentStepIndex == 1 && hasLeftSpawnRoom && HasVisibleEnemy();
        tutorialManager.SetExternalTriggerFlag(enemyIsVisible);
    }

    /// <summary>Called when the player clears the spawn-lift corridor.</summary>
    public void NotifyPlayerExitedSpawnRoom()
    {
        hasLeftSpawnRoom = true;
    }

    private void BeginFirstRoomTutorial()
    {
        if (tutorialStarted || tutorialManager == null || !CanRunTutorialForCurrentSet())
            return;

        tutorialStarted = true;
        tutorialManager.BeginTutorial();
    }

    private bool CanRunTutorialForCurrentSet()
    {
        if (!onlyRunOnActiveSetZero || ProceduralGenerationManager.Instance == null)
            return true;

        return ProceduralGenerationManager.Instance.ActiveSet == 0;
    }

    private void FindPlayerReferences()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
            return;

        player = playerObject.transform;
        playerCamera = playerObject.GetComponentInChildren<Camera>();
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private bool HasVisibleEnemy()
    {
        if (player == null || playerCamera == null)
            return false;

        foreach (EnemyManager enemy in FindObjectsOfType<EnemyManager>())
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
                continue;

            Vector3 toEnemy = enemy.transform.position - player.position;
            if (toEnemy.sqrMagnitude > enemyEncounterDistance * enemyEncounterDistance)
                continue;

            Vector3 viewportPoint = playerCamera.WorldToViewportPoint(enemy.transform.position);
            bool isOnScreen = viewportPoint.z > 0f
                              && viewportPoint.x > 0.05f && viewportPoint.x < 0.95f
                              && viewportPoint.y > 0.05f && viewportPoint.y < 0.95f;
            if (isOnScreen)
                return true;
        }

        return false;
    }

    private static List<TutorialStep> CreateFirstRoomSteps()
    {
        return new List<TutorialStep>
        {
            new TutorialStep
            {
                instructionText = "USE W / A / S / D TO MOVE",
                keys = new[] { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D },
                delayAfterComplete = 0.2f
            },
            new TutorialStep
            {
                instructionText = "ENEMY DETECTED — MOVE AND KILL / PRESS MOUSE 1 TO FIRE",
                mouseButton = MouseButtonType.Left,
                waitToShowForExternalTrigger = true,
                pauseTimeUntilInput = true,
                delayAfterComplete = 0.25f
            }
        };
    }
}
