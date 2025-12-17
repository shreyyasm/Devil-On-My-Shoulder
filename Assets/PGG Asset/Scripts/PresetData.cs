using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "ProcGen/Generation Preset")]
public class GenerationPreset : ScriptableObject
{
    public string PresetName;

    public RoomData StartRoom;
    public RoomData EndRoom;
    public RoomData RewardRoom;

    public List<RoomGroup> AllRooms;
}
