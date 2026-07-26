using UnityEngine;

public class Door : MonoBehaviour
{
    public Transform SnapPoint;
    public GameObject Blocker;

    [Tooltip("If true, this door is always used as the entry point when this room is attached to a parent room, instead of picking a random free door.")]
    public bool IsStartDoor;

    [HideInInspector] public bool IsUsed;

    public void Open()
    {
        IsUsed = true;
        if (Blocker != null)
            Blocker.SetActive(false);
    }

    public void Close()
    {
        if (Blocker != null)
            Blocker.SetActive(true);
    }
}