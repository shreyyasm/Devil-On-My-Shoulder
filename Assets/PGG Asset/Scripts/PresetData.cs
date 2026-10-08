using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "ProcGen/Generation Preset")]
public class GenerationPreset : ScriptableObject
{
    public string PresetName;

    [Header("Start Rooms")]
    public RoomData StartRoomFirstSet;
    public RoomData StartRoomRegular;
    public RoomData EndRoom;
    public RoomData RewardRoom;

    [HideInInspector] public List<RoomGroup> AllRooms;

    [Header("Rooms Unlocked In This Set")]
    public List<RoomGroup> NewlyUnlockedRooms;
}
