using UnityEngine;
using System.Collections.Generic;

public class ProceduralGenerationManager : MonoBehaviour
{
    [Header("Generation Settings")]
    public int RoomsToEnd = 10;
    public float DistanceOffset = 0.2f;

    [Header("Seed Settings")]
    public bool RandomizeSeed = true;
    public int Seed;

    [Header("Retry / Backtracking")]
    public int MaxAttachAttempts = 10;
    public int MaxGlobalRetries = 10;

    [Header("Presets")]
    public List<GenerationPreset> Presets;

    // ================= RUNTIME =================

    private List<RoomInstance> placedRooms = new();
    private Dictionary<DungeonNode, RoomInstance> placedNodes = new();

    // ================= UNITY =================

    private void Start()
    {
        GenerateWithRetries();
    }

    // ================= ENTRY =================

    void GenerateWithRetries()
    {
        for (int attempt = 0; attempt < MaxGlobalRetries; attempt++)
        {
            SetupSeed();

            ClearPrevious();

            GenerationPreset preset = Presets[Random.Range(0, Presets.Count)];
            DungeonGraph graph = GenerateGraph(preset);

            bool success = BuildGeometry(graph);

            if (success)
            {
                CloseAllUnusedDoors();
                Debug.Log($"[ProcGen] SUCCESS with Seed: {Seed}");
                return;
            }

            Debug.LogWarning($"[ProcGen] Global failure with Seed: {Seed}");
        }

        Debug.LogError("[ProcGen] Failed to generate dungeon after global retries");
    }

    void SetupSeed()
    {
        if (RandomizeSeed)
            Seed = Random.Range(int.MinValue, int.MaxValue);

        Random.InitState(Seed);
    }

    void ClearPrevious()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        placedRooms.Clear();
        placedNodes.Clear();
    }

    // ================= GRAPH PASS =================

    DungeonGraph GenerateGraph(GenerationPreset preset)
    {
        DungeonGraph graph = new();

        DungeonNode start = new DungeonNode(NodeType.Start, preset.StartRoom);
        graph.StartNode = start;
        graph.AllNodes.Add(start);

        DungeonNode current = start;

        for (int i = 0; i < RoomsToEnd; i++)
        {
            RoomData data = GetRandomRoomFromPreset(preset);

            DungeonNode next = new DungeonNode(NodeType.Room, data);
            current.Connections.Add(next);
            next.Connections.Add(current);

            graph.AllNodes.Add(next);
            current = next;
        }

        DungeonNode end = new DungeonNode(NodeType.End, preset.EndRoom);
        current.Connections.Add(end);
        end.Connections.Add(current);
        graph.AllNodes.Add(end);

        return graph;
    }

    RoomData GetRandomRoomFromPreset(GenerationPreset preset)
    {
        RoomGroup group = preset.AllRooms[Random.Range(0, preset.AllRooms.Count)];
        return group.Rooms[Random.Range(0, group.Rooms.Count)];
    }

    // ================= GEOMETRY PASS =================

    bool BuildGeometry(DungeonGraph graph)
    {
        RoomInstance startRoom = SpawnRoom(
            graph.StartNode.RoomData,
            Vector3.zero,
            Quaternion.identity
        );

        placedNodes[graph.StartNode] = startRoom;

        return PlaceChildren(graph.StartNode);
    }

    bool PlaceChildren(DungeonNode node)
    {
        RoomInstance parentRoom = placedNodes[node];

        foreach (DungeonNode child in node.Connections)
        {
            if (placedNodes.ContainsKey(child))
                continue;

            bool placed = false;

            for (int attempt = 0; attempt < MaxAttachAttempts; attempt++)
            {
                if (TryAttachRoom(parentRoom, child.RoomData, out RoomInstance childRoom))
                {
                    placedNodes[child] = childRoom;

                    if (PlaceChildren(child))
                    {
                        placed = true;
                        break;
                    }

                    // BACKTRACK
                    RemoveRoom(child, childRoom);
                }
            }

            if (!placed)
                return false; // propagate failure upward
        }

        return true;
    }

    void RemoveRoom(DungeonNode node, RoomInstance room)
    {
        placedNodes.Remove(node);
        placedRooms.Remove(room);
        Destroy(room.Instance);
    }

    // ================= ROOM PLACEMENT =================

    RoomInstance SpawnRoom(RoomData data, Vector3 pos, Quaternion rot)
    {
        GameObject obj = Instantiate(data.Prefab, pos, rot, transform);
        RoomInstance instance = new RoomInstance(obj);
        placedRooms.Add(instance);
        return instance;
    }

    bool TryAttachRoom(RoomInstance fromRoom, RoomData data, out RoomInstance newRoom)
    {
        newRoom = null;

        List<Door> freeDoors = fromRoom.Doors.FindAll(d => !d.IsUsed);
        if (freeDoors.Count == 0)
            return false;

        Door exitDoor = freeDoors[Random.Range(0, freeDoors.Count)];

        GameObject obj = Instantiate(data.Prefab, Vector3.zero, Quaternion.identity, transform);
        Door[] doors = obj.GetComponentsInChildren<Door>();
        if (doors.Length == 0)
        {
            Destroy(obj);
            return false;
        }

        Door entryDoor = doors[Random.Range(0, doors.Length)];

        // ---------- DOOR SNAP ----------

        Quaternion exitFacing =
            Quaternion.LookRotation(-exitDoor.SnapPoint.forward, Vector3.up);
        Quaternion entryFacing =
            Quaternion.LookRotation(entryDoor.SnapPoint.forward, Vector3.up);

        obj.transform.rotation = exitFacing * Quaternion.Inverse(entryFacing);

        obj.transform.position +=
            exitDoor.SnapPoint.position - entryDoor.SnapPoint.position;

        obj.transform.position +=
            exitDoor.SnapPoint.forward * DistanceOffset;

        // ------------------------------

        RoomInstance instance = new RoomInstance(obj);

        if (CheckOverlap(instance, fromRoom))
        {
            Destroy(obj);
            return false;
        }

        exitDoor.Open();
        entryDoor.Open();

        placedRooms.Add(instance);
        newRoom = instance;
        return true;
    }

    // ================= FINALIZATION =================

    void CloseAllUnusedDoors()
    {
        foreach (RoomInstance room in placedRooms)
        {
            foreach (Door door in room.Doors)
            {
                if (!door.IsUsed)
                    door.Close();
            }
        }
    }

    // ================= COLLISION =================

    bool CheckOverlap(RoomInstance newRoom, RoomInstance ignoreRoom)
    {
        foreach (RoomInstance room in placedRooms)
        {
            if (room == ignoreRoom)
                continue;

            if (room.Bounds.Intersects(newRoom.Bounds))
                return true;
        }
        return false;
    }

    // ================= DEBUG =================

    void OnDrawGizmos()
    {
        if (placedRooms == null)
            return;

        foreach (RoomInstance room in placedRooms)
        {
            BoundsGizmo.DrawBounds(room.Bounds, Color.green);
        }
    }
}
