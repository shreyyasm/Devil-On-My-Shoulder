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

            // The End Room becomes visible one room before the player reaches
            // it. Open its lift at that point and leave it open for approach.
            // Do not issue this command once the player is inside the End
            // Room, because LevelComplete then owns the closing sequence.
            if (active && room.RoomIndex != currentRoom)
            {
                LevelComplete[] endLifts = room.Instance.GetComponentsInChildren<LevelComplete>(true);
                foreach (LevelComplete endLift in endLifts)
                    if (!endLift.IsTransitionInProgress)
                        endLift.OpenLiftForPlayer();
            }
        }
    }
}
