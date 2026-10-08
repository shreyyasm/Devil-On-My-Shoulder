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
        // Start-room triggers can run while procedural generation is still
        // placing rooms. Visibility is initialized only after a complete map.
        if (rooms == null)
            return;

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
