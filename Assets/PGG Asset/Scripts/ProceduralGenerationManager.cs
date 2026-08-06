using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ProceduralGenerationManager : MonoBehaviour
{
    public static ProceduralGenerationManager Instance;

  

    public int ActiveSet = 0;
    [Header("Generation Settings")]
    public int RoomsToEnd = 10;
    public float DistanceOffset = 0.2f;

    [Header("Difficulty Scaling")]
    public int RoomsPerSetIncrease = 5;
    private int baseRoomsToEnd;


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
    private int roomIndexCounter = 0;
    private Dictionary<DungeonNode, RoomInstance> placedNodes = new();

    // ================= UNITY =================
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        baseRoomsToEnd = RoomsToEnd;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Replaces Start() as the trigger for generation.
    // sceneLoaded fires for the very first scene too (after Awake/OnEnable,
    // before Start), so this single path covers both the initial load
    // and every subsequent scene load — no double-generation.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
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
            RoomsToEnd = baseRoomsToEnd + (ActiveSet * RoomsPerSetIncrease);
            GenerationPreset preset = Presets[ActiveSet];
            DungeonGraph graph = GenerateGraph(preset);

            bool success = BuildGeometry(graph);

            if (success)
            {
                CloseAllUnusedDoors();
                RoomVisibilityManager.Instance.Initialize(placedRooms);
                RoomVisibilityManager.Instance.SetCurrentRoom(0);
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
        roomIndexCounter = 0;
    }

    // ================= GRAPH PASS =================

    DungeonGraph GenerateGraph(GenerationPreset preset)
    {
        DungeonGraph graph = new();

        DungeonNode start = new DungeonNode(NodeType.Start, preset.StartRoom);
        graph.StartNode = start;
        graph.AllNodes.Add(start);

        DungeonNode current = start;

        List<RoomData> roomBag = new();
        RoomData lastRoom = null;

        for (int i = 0; i < RoomsToEnd; i++)
        {
            RoomData data = GetNextRoomFromBag(preset, roomBag, lastRoom);
            lastRoom = data;

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

    // ================= ROOM BAG (no consecutive repeats, full coverage before repeat) =================

    RoomData GetNextRoomFromBag(GenerationPreset preset, List<RoomData> bag, RoomData lastRoom)
    {
        if (bag.Count == 0)
        {
            RefillBag(preset, bag);

            // If the freshly shuffled bag would start with the same room that
            // just ended the previous bag, swap it elsewhere in the bag.
            if (lastRoom != null && bag.Count > 1 && bag[bag.Count - 1] == lastRoom)
            {
                int swapIndex = Random.Range(0, bag.Count - 1);
                (bag[bag.Count - 1], bag[swapIndex]) = (bag[swapIndex], bag[bag.Count - 1]);
            }
        }

        RoomData picked = bag[bag.Count - 1];
        bag.RemoveAt(bag.Count - 1);
        return picked;
    }

    void RefillBag(GenerationPreset preset, List<RoomData> bag)
    {
        bag.Clear();

        foreach (RoomGroup group in preset.AllRooms)
            foreach (RoomData data in group.Rooms)
                bag.Add(data);

        ShuffleList(bag);
    }

    void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
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

        instance.RoomIndex = roomIndexCounter++; Debug.Log($"Spawned room index: {instance.RoomIndex}");
        RoomTrigger trigger = obj.GetComponentInChildren<RoomTrigger>();

        if (trigger != null)
        {
            trigger.RoomIndex = instance.RoomIndex;

            Debug.Log($"Assigned trigger index: {trigger.RoomIndex} to {obj.name}");
        }
        else
        {
            Debug.LogError($"No RoomTrigger found in {obj.name}");
        }
        if (trigger != null)
        {
            trigger.RoomIndex = instance.RoomIndex;
        }
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

        // Prefer the room's designated start door, fall back to random.
        Door entryDoor = System.Array.Find(doors, d => d.IsStartDoor);
        if (entryDoor == null)
            entryDoor = doors[Random.Range(0, doors.Length)];

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
        instance.RoomIndex = roomIndexCounter++;

        RoomTrigger trigger = obj.GetComponentInChildren<RoomTrigger>();

        if (trigger != null)
        {
            trigger.RoomIndex = instance.RoomIndex;
        }

        //Debug.Log($"Spawned room index: {instance.RoomIndex}");

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
