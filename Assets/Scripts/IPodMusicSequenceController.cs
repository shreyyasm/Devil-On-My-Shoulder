using System.Collections;
using UnityEngine;

/// <summary>
/// Owns the player iPod presentation. The iPod hand animation raises the
/// device, its animation event starts the requested music, and a second event
/// hides the device again after the hand leaves the screen.
/// </summary>
public class IPodMusicSequenceController : MonoBehaviour
{
    private enum MusicCue
    {
        None,
        Default,
        Gameplay,
        FreshGameplay
    }

    [Header("iPod Presentation")]
    [SerializeField] private GameObject iPodObject;
    [SerializeField] private Animator iPodAnimator;
    [SerializeField] private string animationStateName = "TURNONIPOD";

    [Header("Animation Event Fallback")]
    [SerializeField, Min(0f)] private float fallbackPlayButtonTime = .5f;
    [SerializeField, Min(.1f)] private float fallbackAnimationDuration = 2.46f;

    private Coroutine sequenceRoutine;
    private MusicCue pendingCue;
    private bool musicCuePlayed;

    private void Awake()
    {
        ResolveReferences();
        if (iPodObject != null)
            iPodObject.SetActive(false);
    }

    public void PlayDefaultMusicSequence() => PlaySequence(MusicCue.Default);
    public void PlayGameplayMusicSequence() => PlaySequence(MusicCue.Gameplay);
    public void PlayFreshGameplayMusicSequence() => PlaySequence(MusicCue.FreshGameplay);

    /// <summary>Plays the appropriate iPod cue once the player leaves a spawn lift.</summary>
    public void PlayForSpawnLiftExit()
    {
        BackgroundMusic music = BackgroundMusic.Instance;
        if (music != null && music.IsElevatorMusicActive)
            PlayFreshGameplayMusicSequence();
        else if (music == null || !music.HasStartedGameplay)
            PlayDefaultMusicSequence();
        else
            PlayGameplayMusicSequence();
    }

    public void OnIPodPlayButtonPressed()
    {
        if (musicCuePlayed)
            return;

        musicCuePlayed = true;
        switch (pendingCue)
        {
            case MusicCue.Default:
                BackgroundMusic.Instance?.PlayDefaultMusicFromIPod();
                break;
            case MusicCue.Gameplay:
                BackgroundMusic.Instance?.StartGameplayMusicImmediately();
                break;
            case MusicCue.FreshGameplay:
                BackgroundMusic.Instance?.StopElevatorMusicAndStartFreshGameplayTrack();
                break;
        }
    }

    public void OnIPodAnimationCompleted()
    {
        if (iPodObject == null)
            return;

        if (!musicCuePlayed)
            OnIPodPlayButtonPressed();

        iPodObject.SetActive(false);
        NowPlayingMusicUI.Instance?.ShowNowPlaying(BackgroundMusic.Instance?.CurrentTrackName);
        pendingCue = MusicCue.None;
    }

    private void PlaySequence(MusicCue cue)
    {
        ResolveReferences();
        if (iPodObject == null || iPodAnimator == null)
        {
            PlayMusicWithoutPresentation(cue);
            return;
        }

        if (sequenceRoutine != null)
            StopCoroutine(sequenceRoutine);

        pendingCue = cue;
        musicCuePlayed = false;
        iPodObject.SetActive(true);
        iPodAnimator.Rebind();
        iPodAnimator.Update(0f);
        if (!string.IsNullOrWhiteSpace(animationStateName))
            iPodAnimator.Play(animationStateName, 0, 0f);

        sequenceRoutine = StartCoroutine(AnimationEventFallback());
    }

    private IEnumerator AnimationEventFallback()
    {
        yield return new WaitForSeconds(fallbackPlayButtonTime);
        OnIPodPlayButtonPressed();

        float remainingTime = Mathf.Max(0f, fallbackAnimationDuration - fallbackPlayButtonTime);
        yield return new WaitForSeconds(remainingTime);
        OnIPodAnimationCompleted();
        sequenceRoutine = null;
    }

    private void PlayMusicWithoutPresentation(MusicCue cue)
    {
        pendingCue = cue;
        musicCuePlayed = false;
        OnIPodPlayButtonPressed();
        pendingCue = MusicCue.None;
    }

    private void ResolveReferences()
    {
        if (iPodObject == null)
        {
            Transform iPod = FindDescendant(transform, "I pod");
            if (iPod != null)
                iPodObject = iPod.gameObject;
        }

        if (iPodAnimator == null && iPodObject != null)
            iPodAnimator = iPodObject.GetComponent<Animator>();
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == objectName)
                return child;
        return null;
    }
}
