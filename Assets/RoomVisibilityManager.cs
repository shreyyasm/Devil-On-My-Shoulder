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

    public void SetCurrentRoom(int currentRoom, Vector3? playerPosition = null)
    {
        // Start-room triggers can run while procedural generation is still
        // placing rooms. Visibility is initialized only after a complete map.
        if (rooms == null || rooms.Count == 0)
            return;

        // Room trigger volumes can overlap at connected doors. Use the
        // player's actual position to select the closest generated room
        // rather than trusting a neighboring trigger that happens to fire.
        // This prevents an overlapping room volume from hiding the spawn
        // lift while the player is still inside it.
        if (playerPosition.HasValue)
            currentRoom = ResolveRoomIndex(playerPosition.Value, currentRoom);

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

    private int ResolveRoomIndex(Vector3 playerPosition, int fallbackRoomIndex)
    {
        RoomInstance closestRoom = null;
        float closestDistance = float.PositiveInfinity;

        foreach (RoomInstance room in rooms)
        {
            if (room == null || room.Instance == null)
                continue;

            room.RefreshBounds();
            float distance = room.Bounds.SqrDistance(playerPosition);

            // Keep the first room when two trigger bounds overlap exactly.
            // Rooms are ordered along the generated path, so this favors the
            // room the player was already standing in at a shared doorway.
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestRoom = room;
            }
        }

        return closestRoom != null ? closestRoom.RoomIndex : fallbackRoomIndex;
    }
}
