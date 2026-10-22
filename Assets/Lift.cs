using System.Collections;
using UnityEngine;

public class Lift : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField, Min(0f)] private float initialOpenDelay = 1f;
    [SerializeField, Min(0f)] private float closeDelay = 2f;
    [SerializeField, Min(0.1f)] private float spawnLiftDoorProximityDepth = 3.5f;

    private AudioSource audioSource;
    private Coroutine closeRoutine;
    private int playersInDoorway;
    private bool isOpen;
    private bool iPodCuePlayedForThisArrival;

    private void Awake()
    {
        // Some room prefabs retain an old inactive Lift_M reference. Always use
        // the active lift animator so the opening trigger can actually play.
        if (animator == null || !animator.gameObject.activeInHierarchy)
        {
            foreach (Animator candidate in GetComponentsInChildren<Animator>(true))
            {
                if (candidate.gameObject.activeInHierarchy)
                {
                    animator = candidate;
                    break;
                }
            }
        }

        audioSource = GetComponent<AudioSource>();
        CreateSpawnLiftEntryTrigger();
        Invoke(nameof(OpenDoorsForEntry), initialOpenDelay);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerHealth>() != null)
            NotifyPlayerEnteredDoorway();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerHealth>() != null)
            NotifyPlayerExitedDoorway();
    }

    /// <summary>Called by the dedicated Spawn Lift entrance trigger.</summary>
    public void NotifyPlayerEnteredDoorway()
    {
        playersInDoorway++;
        OpenDoorsForEntry();
    }

    /// <summary>Called by the dedicated Spawn Lift entrance trigger.</summary>
    public void NotifyPlayerExitedDoorway()
    {
        playersInDoorway = Mathf.Max(0, playersInDoorway - 1);
        ProceduralGenerationManager.Instance?.NotifyPlayerExitedArrivalLift(this);
        LevelComplete.RestorePlayerGameplayUi(GameObject.FindGameObjectWithTag("Player")?.transform);
        PlayIPodCueAfterPlayerLeavesLift();
        if (playersInDoorway == 0)
            ScheduleClose();
    }

    /// <summary>Called continuously while the player is near a spawn-lift door.</summary>
    public void NotifyPlayerNearDoorway()
    {
        OpenDoorsForEntry();
    }

    private void PlayIPodCueAfterPlayerLeavesLift()
    {
        if (iPodCuePlayedForThisArrival)
            return;

        iPodCuePlayedForThisArrival = true;
        FindFirstObjectByType<IPodMusicSequenceController>()?.PlayForSpawnLiftExit();
    }

    private void CreateSpawnLiftEntryTrigger()
    {
        // Spawn rooms can be revisited after their arrival animation. Copy the
        // authored doorway volume to a child that remains active after closing.
        if (gameObject.name.IndexOf("Spawn Room", System.StringComparison.OrdinalIgnoreCase) < 0 ||
            transform.Find("Spawn Lift Entry Trigger") != null)
            return;

        BoxCollider authoredTrigger = GetComponent<BoxCollider>();
        if (authoredTrigger == null)
            return;

        GameObject doorway = new GameObject("Spawn Lift Entry Trigger");
        doorway.layer = gameObject.layer;
        doorway.transform.SetParent(transform, false);
        doorway.transform.localPosition = authoredTrigger.center;
        doorway.transform.localRotation = Quaternion.identity;
        doorway.transform.localScale = Vector3.one;

        BoxCollider entryCollider = doorway.AddComponent<BoxCollider>();
        entryCollider.isTrigger = true;
        // The old trigger was a thin strip exactly on the threshold. A player
        // could turn around before receiving another enter event and become
        // blocked by a closed door. This persistent zone reaches both inside
        // and outside the lift, making the doors behave like automatic doors.
        entryCollider.size = new Vector3(
            Mathf.Max(authoredTrigger.size.x, spawnLiftDoorProximityDepth),
            authoredTrigger.size.y,
            authoredTrigger.size.z);
        doorway.AddComponent<SpawnLiftEntryTrigger>().Initialize(this);

        // The child owns the interaction; disabling the authored collider
        // prevents a single player from being counted twice.
        authoredTrigger.enabled = false;
        closeDelay = 0.5f;
    }

    private void OpenDoorsForEntry()
    {
        OpenDoors("OpenLift");
    }

    private void OpenDoorsForExit()
    {
        OpenDoors("OpenLift Front");
    }

    private void OpenDoors(string trigger)
    {
        if (closeRoutine != null)
        {
            StopCoroutine(closeRoutine);
            closeRoutine = null;
        }

        if (isOpen)
            return;

        isOpen = true;

        if (audioSource != null && SFXManager.Instance != null)
            SFXManager.Instance.PlayNPC_SFX(audioSource, "Lift/Open", 0.7f);

        if (animator != null)
        {
            animator.ResetTrigger("CloseLift");
            animator.ResetTrigger("CloseLift Front");
            animator.SetTrigger(trigger);
        }
    }

    /// <summary>For the level-transition controller after a new level arrives.</summary>
    public void OpenDoorsForArrival()
    {
        if (closeRoutine != null)
        {
            StopCoroutine(closeRoutine);
            closeRoutine = null;
        }

        isOpen = true;
        if (animator != null)
        {
            animator.ResetTrigger("CloseLift");
            animator.SetTrigger("OpenLift");
        }
    }

    /// <summary>Forces a prepared spawn lift back to its closed arrival state.</summary>
    public void CloseDoorsForTransition()
    {
        if (closeRoutine != null)
        {
            StopCoroutine(closeRoutine);
            closeRoutine = null;
        }

        isOpen = false;
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    private void ScheduleClose()
    {
        if (closeRoutine != null)
            StopCoroutine(closeRoutine);

        closeRoutine = StartCoroutine(CloseWhenDoorwayIsEmpty());
    }

    private IEnumerator CloseWhenDoorwayIsEmpty()
    {
        yield return new WaitForSecondsRealtime(closeDelay);

        if (playersInDoorway == 0)
        {
            if (animator != null)
            {
                animator.ResetTrigger("OpenLift");
                animator.ResetTrigger("OpenLift Front");
                animator.SetTrigger("CloseLift");
            }

            isOpen = false;
        }

        closeRoutine = null;
    }
}

public sealed class SpawnLiftEntryTrigger : MonoBehaviour
{
    private Lift lift;

    public void Initialize(Lift owner)
    {
        lift = owner;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerHealth>() != null)
            lift?.NotifyPlayerEnteredDoorway();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerHealth>() != null)
            lift?.NotifyPlayerExitedDoorway();
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.GetComponentInParent<PlayerHealth>() != null)
            lift?.NotifyPlayerNearDoorway();
    }
}
