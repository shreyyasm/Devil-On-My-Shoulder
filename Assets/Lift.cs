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
        Invoke(nameof(OpenDoors), initialOpenDelay);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playersInDoorway++;
        OpenDoors();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playersInDoorway = Mathf.Max(0, playersInDoorway - 1);

        if (playersInDoorway == 0)
            ScheduleClose();
    }

    private void OpenDoors()
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
            animator.SetTrigger("OpenLift");
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
