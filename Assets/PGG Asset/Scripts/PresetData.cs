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

    [Header("Active Room Pool (Used by Generator)")]
    [Tooltip("This is the room list the procedural generator selects from. It includes rooms inherited or previously configured for this preset.")]
    public List<RoomGroup> AllRooms;

    [Header("Rooms Unlocked In This Set")]
    public List<RoomGroup> NewlyUnlockedRooms;
}


