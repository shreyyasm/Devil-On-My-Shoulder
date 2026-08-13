using System.Collections.Generic;
using UnityEngine;

public class RoomVisibilityManager : MonoBehaviour
{
    public static RoomVisibilityManager Instance;

    [SerializeField] private int visibleDistance = 1;

    private List<RoomInstance> rooms;

    public int CurrentRoomIndex { get; private set; }
    public RoomInstance CurrentRoom { get; private set; }

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
        CurrentRoomIndex = currentRoom;
        CurrentRoom = null;

        foreach (RoomInstance room in rooms)
        {
            if (room.RoomIndex == currentRoom)
            {
                CurrentRoom = room;
            }

            bool active =
                Mathf.Abs(room.RoomIndex - currentRoom) <= visibleDistance;

            room.Instance.SetActive(active);
        }
    }
}