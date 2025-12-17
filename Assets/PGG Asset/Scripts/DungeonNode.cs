using System.Collections.Generic;

public enum NodeType
{
    Start,
    Room,
    Corridor,
    End,
    Reward
}

[System.Serializable]
public class DungeonNode
{
    public NodeType Type;
    public RoomData RoomData;
    public List<DungeonNode> Connections = new();

    public DungeonNode(NodeType type, RoomData data)
    {
        Type = type;
        RoomData = data;
    }
}
