using UnityEngine;

public class RoomTrigger : MonoBehaviour
{
    public int RoomIndex;
    private bool tutorialEntryReported;

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        RoomVisibilityManager visibilityManager = RoomVisibilityManager.Instance;
        if (visibilityManager == null)
            return;

        visibilityManager.SetCurrentRoom(RoomIndex, other.bounds.center);

        // Room visibility needs to update continuously, but a contextual
        // tutorial should receive only the first entry into this room.
        if (!tutorialEntryReported)
        {
            tutorialEntryReported = true;
            MadDashTutorialDirector.Instance?.NotifyRoomEntered(RoomIndex, GetRoomIdentity());
        }
    }

    private string GetRoomIdentity()
    {
        System.Text.StringBuilder identity = new System.Text.StringBuilder();
        for (Transform current = transform; current != null; current = current.parent)
            identity.Append('/').Append(current.name);
        return identity.ToString();
    }
}
