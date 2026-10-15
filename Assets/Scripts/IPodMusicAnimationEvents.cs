using UnityEngine;

/// <summary>Animation-event receiver attached to the animated iPod object.</summary>
public class IPodMusicAnimationEvents : MonoBehaviour
{
    public void OnIPodPlayButtonPressed()
    {
        GetComponentInParent<IPodMusicSequenceController>()?.OnIPodPlayButtonPressed();
    }

    public void OnIPodAnimationCompleted()
    {
        GetComponentInParent<IPodMusicSequenceController>()?.OnIPodAnimationCompleted();
    }
}
