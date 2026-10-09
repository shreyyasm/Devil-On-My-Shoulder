using System.Collections;
using UnityEngine;

/// <summary>
/// Keeps an End Room lift in its approach-ready state whenever the room is
/// spawned or made visible. The LevelComplete trigger owns the later close.
/// </summary>
[RequireComponent(typeof(Animator))]
public class EndLiftStartsOpen : MonoBehaviour
{
    private Animator liftAnimator;

    private void Awake()
    {
        liftAnimator = GetComponent<Animator>();
        OpenImmediately();
    }

    private void OnEnable()
    {
        StartCoroutine(OpenAfterAnimatorInitializes());
    }

    private IEnumerator OpenAfterAnimatorInitializes()
    {
        yield return null;
        OpenImmediately();
    }

    private void OpenImmediately()
    {
        if (liftAnimator == null)
            liftAnimator = GetComponent<Animator>();

        if (liftAnimator == null)
            return;

        liftAnimator.enabled = true;
        liftAnimator.ResetTrigger("OpenLift Front");
        liftAnimator.ResetTrigger("CloseLift");
        liftAnimator.ResetTrigger("CloseLift Front");
        liftAnimator.SetTrigger("OpenLift");
    }
}
