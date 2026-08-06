using UnityEngine;

public class RoomTrigger : MonoBehaviour
{
    public int RoomIndex;

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        RoomVisibilityManager.Instance.SetCurrentRoom(RoomIndex);
    }
}