using System.Collections.Generic;
using UnityEngine;

public class RoomVisibilityManager : MonoBehaviour
{
    public static RoomVisibilityManager Instance;

    [SerializeField] private int visibleDistance = 1;

    private List<RoomInstance> rooms;

    private void Awake()
    {
        Instance = this;
    }

    public void Initialize(List<RoomInstance> generatedRooms)
    {
        rooms = generatedRooms;
    }

    public void SetCurrentRoom(int currentRoom)
    {
        foreach (RoomInstance room in rooms)
        {
            bool active = Mathf.Abs(room.RoomIndex - currentRoom) <= visibleDistance;

            room.Instance.SetActive(active);
        }
    }
}