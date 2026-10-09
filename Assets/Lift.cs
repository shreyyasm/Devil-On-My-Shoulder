using System.Collections;
using UnityEngine;

public class Lift : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField, Min(0f)] private float initialOpenDelay = 1f;
    [SerializeField, Min(0f)] private float closeDelay = 2f;

    private AudioSource audioSource;
    private Coroutine closeRoutine;
    private int playersInDoorway;
    private bool isOpen;

    private void Awake()
    {
        // Some room prefabs retain an old inactive Lift_M reference. Always use
        // the active lift animator so the opening trigger can actually play.
        if (animator == null || !animator.gameObject.activeInHierarchy)
        {
            Animator[] animators = GetComponentsInChildren<Animator>(true);
            foreach (Animator candidate in animators)
            {
                if (candidate.gameObject.activeInHierarchy)
                {
                    animator = candidate;
                    break;
                }
            }
        }

        audioSource = GetComponent<AudioSource>();
        Invoke(nameof(OpenDoorsForEntry), initialOpenDelay);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playersInDoorway++;
        OpenDoorsForEntry();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playersInDoorway = Mathf.Max(0, playersInDoorway - 1);

        if (playersInDoorway == 0)
            ScheduleClose();
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
            animator.SetTrigger(trigger);
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
            // A newly loaded level starts with the Spawn Lift's normal open.
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
            // Reset to the controller's initial closed state so OpenLift is
            // valid again after the one-second arrival delay.
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
        yield return new WaitForSeconds(closeDelay);

        if (playersInDoorway == 0)
        {
            if (animator != null)
                animator.SetTrigger("CloseLift");
            isOpen = false;
        }

        closeRoutine = null;
    }
}
