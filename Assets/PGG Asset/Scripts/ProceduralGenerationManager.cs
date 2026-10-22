using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using InfimaGames.LowPolyShooterPack;

public class ProceduralGenerationManager : MonoBehaviour
{
    public static ProceduralGenerationManager Instance;

  

    public int ActiveSet = 0;
    [Header("Generation Settings")]
    public int RoomsToEnd = 8;
    public float DistanceOffset = 0.2f;

    [Header("Difficulty Scaling")]
    public int RoomsPerSetIncrease = 3;
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
    private int currentSetIndex;
    private static bool creatingPreviewGenerator;
    private bool previewGenerator;
    private Transform activeLevelRoot;
    private ProceduralGenerationManager preparedGenerator;
    private Vector3 playerSpawnOffset = new Vector3(1.46f, -1.66f, .01f);
    private Quaternion playerSpawnRotation = Quaternion.Euler(0f, 270f, 0f);
    private const float PreparedLevelYOffset = -10000f;
    [SerializeField, Min(.05f)] private float liftPlayerRotateDuration = .1f;
    [SerializeField, Min(.05f)] private float liftCameraCenterDuration = .125f;
    private PlayerMovement lockedPlayerMovement;
    
    private Character lockedCharacter;
private CameraLook lockedCameraLook;
    private bool restorePlayerMovement;
    private bool restoreCameraLook;
    private bool liftControlSessionActive;
    private readonly HashSet<Lift> waitingArrivalLiftExits = new();
    private Coroutine cameraCenterRoutine;

    // ================= UNITY =================
    private void Awake()
    {
        if (creatingPreviewGenerator)
        {
            previewGenerator = true;
            baseRoomsToEnd = RoomsToEnd;
            return;
        }

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
        if (!previewGenerator)
            SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (!previewGenerator)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Clears the persistent procedural-run state before loading a completely
    /// fresh match. Both New Game and Restart begin from procedural set zero.
    /// </summary>
    public static void ResetForNewGame()
    {
        if (Instance == null)
            return;

        Instance.ActiveSet = 0;
        Instance.currentSetIndex = 0;
        Instance.preparedGenerator = null;
        Instance.waitingArrivalLiftExits.Clear();

        if (Instance.cameraCenterRoutine != null)
        {
            Instance.StopCoroutine(Instance.cameraCenterRoutine);
            Instance.cameraCenterRoutine = null;
        }

        // Prevent this old DontDestroyOnLoad manager from receiving the scene
        // loaded event during the transition. The newly loaded Mad Dash scene
        // then owns the only generator and starts at ActiveSet 0.
        Instance.StopAllCoroutines();

        // Destroy() is deferred until the end of this frame, but the gameplay
        // scene loads immediately. Hide the entire old generator now so its
        // generated rooms, colliders, and prepared preview cannot coexist
        // with the fresh set-zero generator for even one frame.
        Instance.gameObject.SetActive(false);

        // The manager is DontDestroyOnLoad. Clear the singleton before the
        // old persistent object is destroyed so the newly loaded scene can
        // create and own a fresh generator immediately.
        Instance = null;
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

    bool GenerateWithRetries()
    {
        if (Presets == null || Presets.Count == 0)
        {
            Debug.LogError("[ProcGen] No generation presets are configured.");
            return false;
        }

        currentSetIndex = Mathf.Clamp(ActiveSet, 0, Presets.Count - 1);

        for (int attempt = 0; attempt < MaxGlobalRetries; attempt++)
        {
            SetupSeed();

            ClearPrevious();
            RoomsToEnd = baseRoomsToEnd + (currentSetIndex * RoomsPerSetIncrease);
            GenerationPreset preset = Presets[currentSetIndex];
            DungeonGraph graph = GenerateGraph(preset);

            if (graph == null)
                return false;

            bool success = BuildGeometry(graph);

            if (success)
            {
                CloseAllUnusedDoors();
                if (!previewGenerator)
                {
                    RoomVisibilityManager.Instance.Initialize(placedRooms);
                    RoomVisibilityManager.Instance.SetCurrentRoom(0);
                    CapturePlayerSpawnPoint();
                }
                Debug.Log($"[ProcGen] SUCCESS with Seed: {Seed}");                         
                return true;
            }

            Debug.LogWarning($"[ProcGen] Global failure with Seed: {Seed}");
        }

        Debug.LogError("[ProcGen] Failed to generate dungeon after global retries");
        return false;
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

        RoomData startRoom = currentSetIndex == 0
            ? preset.StartRoomFirstSet
            : preset.StartRoomRegular;

        Debug.Log($"[ProcGen] Active Set {currentSetIndex} selected start room '{startRoom?.RoomName ?? "None"}'.");

        if (startRoom == null)
        {
            Debug.LogError($"[ProcGen] Missing start room for active set {currentSetIndex} in preset '{preset.name}'.");
            return null;
        }

        DungeonNode start = new DungeonNode(NodeType.Start, startRoom);
        graph.StartNode = start;
        graph.AllNodes.Add(start);

        DungeonNode current = start;

        List<RoomData> roomBag = new();
        RoomData lastRoom = null;

        for (int i = 0; i < RoomsToEnd; i++)
        {
            RoomData data = GetNextRoomFromBag(roomBag, lastRoom);
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

    RoomData GetNextRoomFromBag(List<RoomData> bag, RoomData lastRoom)
    {
        if (bag.Count == 0)
        {
            RefillBag(bag);

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

    void RefillBag(List<RoomData> bag)
    {
        bag.Clear();

        // Existing presets already have a tested AllRooms list. Keep using it
        // so the original Set 0 layout continues to generate as before.
        GenerationPreset activePreset = Presets[currentSetIndex];
        if (activePreset.AllRooms != null && activePreset.AllRooms.Count > 0)
        {
            foreach (RoomGroup group in activePreset.AllRooms)
                foreach (RoomData data in group.Rooms)
                    if (data != null)
                        bag.Add(data);
        }
        else
        {
            for (int setIndex = 0; setIndex <= currentSetIndex; setIndex++)
            {
                GenerationPreset preset = Presets[setIndex];

                if (preset.NewlyUnlockedRooms == null)
                    continue;

                foreach (RoomGroup group in preset.NewlyUnlockedRooms)
                    foreach (RoomData data in group.Rooms)
                        if (data != null)
                            bag.Add(data);
            }
        }

        if (bag.Count == 0)
        {
            Debug.LogError($"[ProcGen] No room prefabs are unlocked for active set {currentSetIndex}.");
            return;
        }

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

        if (!previewGenerator)
            PrepareHealthDrainForSpawnRoomExit();

        return PlaceChildren(graph.StartNode);
    }

    private void PrepareHealthDrainForSpawnRoomExit()
    {
        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerHealth != null)
        {
            // Set 0 resumes only after its dedicated StartTheGameplay trigger
            // at the far end of the spawn corridor. Sets 1+ resume as soon as
            // the player has left their arrival lift.
            playerHealth.PauseHealthDrain();
        }
        else
            Debug.LogWarning("[ProcGen] Could not find PlayerHealth to prepare spawn-room health drain.");

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
                if (TryAttachRoom(parentRoom, child.RoomData, child.Type == NodeType.End, out RoomInstance childRoom))
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

        instance.RoomIndex = roomIndexCounter++;
        Debug.Log($"[ProcGen] Spawned room {instance.RoomIndex}: '{data.RoomName}' from '{data.Prefab.name}'.");
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

    bool TryAttachRoom(RoomInstance fromRoom, RoomData data, bool isEndRoom, out RoomInstance newRoom)
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

        // Prefer the room's designated start door. The End Room has two snap
        // points: Door is its fixed procedural entry, while Door_1 is the
        // shop-side exit. Never randomize the End Room entry or the lift can
        // be rotated 180 degrees and expose the wrong set of doors.
        Door entryDoor = System.Array.Find(doors, d => d.IsStartDoor);
        if (entryDoor == null)
        {
            if (isEndRoom)
            {
                Debug.LogError("[ProcGen] End Room is missing its fixed entry Door (IsStartDoor).", obj);
                Destroy(obj);
                return false;
            }

            entryDoor = doors[Random.Range(0, doors.Length)];
        }

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

        //Debug.Log($"Spawned room index: {instance.RoomIndex}");

        if (CheckOverlap(instance, fromRoom))
        {
            Destroy(obj);
            return false;
        }

        // Room visibility uses RoomIndex as the generated-path distance.
        // Do not consume an index until this attachment has passed overlap
        // validation: a three-door room can reject several candidate exits,
        // and those failed attempts previously left index gaps that caused its
        // actually connected next room to be hidden as if it were far away.
        instance.RoomIndex = roomIndexCounter++;
        RoomTrigger trigger = obj.GetComponentInChildren<RoomTrigger>();
        if (trigger != null)
            trigger.RoomIndex = instance.RoomIndex;

        exitDoor.Open();
        entryDoor.Open();

        // The End Room already contains the lift doors. Remove the ordinary
        // room door used for this final connection so it cannot overlap them.
        if (isEndRoom)
            exitDoor.gameObject.SetActive(false);

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
        // Room roots can move while attaching and while a prepared level is
        // handed off into the active world. Re-read the authored RoomBounds
        // colliders instead of trusting bounds cached at instantiation.
        Physics.SyncTransforms();
        newRoom.RefreshBounds();

        foreach (RoomInstance room in placedRooms)
        {
            if (room == ignoreRoom)
                continue;

            room.RefreshBounds();
            if (room.Bounds.Intersects(newRoom.Bounds))
            {
                Debug.LogWarning($"[ProcGen] Rejected '{newRoom.Instance.name}' because its RoomBounds overlap '{room.Instance.name}'.");
                return true;
            }
        }
        return false;
    }

    // ================= SEAMLESS LEVEL TRANSITION =================

    public bool PrepareNextLevel()
    {
        if (preparedGenerator != null)
            return true;

        if (Presets == null || Presets.Count == 0 || ActiveSet >= Presets.Count - 1)
            return false;

        EnsureActiveLevelRoot();

        GameObject previewRoot = new GameObject("Prepared Level");
        previewRoot.transform.SetParent(transform);
        previewRoot.transform.position = new Vector3(0f, PreparedLevelYOffset, 0f);

        creatingPreviewGenerator = true;
        ProceduralGenerationManager preview = previewRoot.AddComponent<ProceduralGenerationManager>();
        creatingPreviewGenerator = false;

        preview.ActiveSet = ActiveSet + 1;
        preview.RoomsToEnd = baseRoomsToEnd;
        preview.DistanceOffset = DistanceOffset;
        preview.RoomsPerSetIncrease = RoomsPerSetIncrease;
        preview.RandomizeSeed = RandomizeSeed;
        preview.Seed = Seed;
        preview.MaxAttachAttempts = MaxAttachAttempts;
        preview.MaxGlobalRetries = MaxGlobalRetries;
        preview.Presets = Presets;
        preview.baseRoomsToEnd = baseRoomsToEnd;

        if (!preview.GenerateWithRetries())
        {
            Destroy(previewRoot);
            return false;
        }

        SetLevelEnemyActivation(preview.transform, false);

        // Keeping the prepared root merely below the map still leaves its
        // colliders and renderers active. That can let a prepared room bleed
        // into the shop/current level. The completed level stays wholly
        // inactive until the old level has been hidden during handoff.
        previewRoot.SetActive(false);
        preparedGenerator = preview;
        return true;
    }

    public bool ActivatePreparedLevel(Transform arrivalLift = null)
    {
        if (preparedGenerator == null && !PrepareNextLevel())
            return false;

        ProceduralGenerationManager preview = preparedGenerator;
        Transform startRoom = preview.placedRooms.Count > 0
            ? preview.placedRooms[0].Instance.transform
            : null;

        if (startRoom == null)
            return false;

        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        Vector3 playerPosition = player != null ? player.position : Vector3.zero;
        Quaternion playerRotation = player != null ? player.rotation : Quaternion.identity;
        Transform preparedLift = FindChildRecursive(startRoom, "Lift_New");

        if (arrivalLift != null && preparedLift != null)
        {
            // Replace the current End Lift with the next level's Spawn Lift
            // at the exact same position and facing. The player therefore
            // remains in one continuous lift instead of appearing elsewhere.
            Vector3 playerLocalToLift = arrivalLift.InverseTransformPoint(playerPosition);
            // Match the physical End Lift exactly. This keeps the button
            // console on the same side and prevents the new level from
            // appearing behind the player during the handoff.
            Quaternion desiredSpawnLiftRotation = arrivalLift.rotation;
            Quaternion rotationDelta = desiredSpawnLiftRotation * Quaternion.Inverse(preparedLift.rotation);
            preview.transform.rotation = rotationDelta * preview.transform.rotation;
            preview.transform.position += arrivalLift.position - preparedLift.position;
            playerPosition = preparedLift.TransformPoint(playerLocalToLift);
        }
        else
        {
            // Fallback for a transition without a lift reference.
            Quaternion desiredStartRotation = playerRotation * Quaternion.Inverse(playerSpawnRotation);
            Quaternion rotationDelta = desiredStartRotation * Quaternion.Inverse(startRoom.rotation);
            preview.transform.rotation = rotationDelta * preview.transform.rotation;
            Vector3 preparedSpawnPosition = startRoom.TransformPoint(playerSpawnOffset);
            preview.transform.position += playerPosition - preparedSpawnPosition;
        }

        if (activeLevelRoot != null)
        {
            activeLevelRoot.gameObject.SetActive(false);
            Destroy(activeLevelRoot.gameObject);
        }

        // The old root is now inactive, so it is safe to reveal the prepared
        // level at the aligned lift position. This ordering prevents one
        // frame where both room sets can render or collide together.
        preview.gameObject.SetActive(true);
        activeLevelRoot = preview.transform;

        placedRooms = preview.placedRooms;
        placedNodes = preview.placedNodes;
        roomIndexCounter = preview.roomIndexCounter;
        currentSetIndex = preview.currentSetIndex;
        RoomsToEnd = preview.RoomsToEnd;
        ActiveSet = preview.ActiveSet;
        preparedGenerator = null;

        RefreshPlacedRoomBounds();

        if (player != null)
        {
            Rigidbody playerBody = player.GetComponent<Rigidbody>();
            if (playerBody != null)
            {
                playerBody.velocity = Vector3.zero;
                playerBody.angularVelocity = Vector3.zero;
            }

            player.SetPositionAndRotation(playerPosition, playerRotation);
        }

        Physics.SyncTransforms();
        RebuildActiveLevelNavMesh();
        SetLevelEnemyActivation(activeLevelRoot, false);

        RoomVisibilityManager.Instance.Initialize(placedRooms);
        RoomVisibilityManager.Instance.SetCurrentRoom(0);
        StartCoroutine(EnsurePreparedEndLiftIsOpen());
        StartCoroutine(OpenArrivalLiftAndRestoreControl());
        ScoreManager.Instance?.IncreaseLevel();

        Destroy(preview);
        return true;
    }

    private void RefreshPlacedRoomBounds()
    {
        Physics.SyncTransforms();
        foreach (RoomInstance room in placedRooms)
            room?.RefreshBounds();
    }

    private System.Collections.IEnumerator EnsurePreparedEndLiftIsOpen()
    {
        // The End Room was built while the level sat below the map. Its
        // Animator can therefore finish in its default closed state before
        // this level becomes visible. Reapply OpenLift after activation.
        for (int attempt = 0; attempt < 4; attempt++)
        {
            if (activeLevelRoot == null)
                yield break;

            LevelComplete[] endLifts = activeLevelRoot.GetComponentsInChildren<LevelComplete>(true);
            foreach (LevelComplete endLift in endLifts)
                endLift.OpenLiftForPlayer();

            yield return new WaitForSeconds(.2f);
        }
    }

    public void LockPlayerForLiftTravel(
        Vector3 desiredForward = default,
        Transform liftMidpoint = null,
        float forcedPlayerYaw = float.NaN,
        bool snapCameraToNeutralImmediately = false)
    {
        ScoreManager.Instance?.PauseSurvivalTimer();
        lockedPlayerMovement = FindFirstObjectByType<PlayerMovement>();
        
        lockedCharacter = FindFirstObjectByType<Character>();
        lockedCharacter?.SetShopFiringLocked(true);
lockedCameraLook = FindFirstObjectByType<CameraLook>();

        if (lockedPlayerMovement != null)
        {
            if (!liftControlSessionActive)
                restorePlayerMovement = lockedPlayerMovement.enabled;
            lockedPlayerMovement.cameraLocked = true;
            lockedPlayerMovement.enabled = false;

            Rigidbody body = lockedPlayerMovement.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        if (lockedCameraLook != null)
        {
            if (!liftControlSessionActive)
                restoreCameraLook = lockedCameraLook.enabled;
            lockedCameraLook.SetRestrictedShopLook(false);
            lockedCameraLook.SyncRotationToCurrentView();
            lockedCameraLook.enabled = false;

            if (cameraCenterRoutine != null)
                StopCoroutine(cameraCenterRoutine);

            Quaternion playerTargetRotation = lockedPlayerMovement != null
                ? lockedPlayerMovement.transform.rotation
                : transform.root.rotation;
            if (!float.IsNaN(forcedPlayerYaw))
            {
                // The player rig's neutral forward direction is -90° yaw.
                // With CameraLook at local zero, this faces the shop entrance.
                playerTargetRotation = Quaternion.Euler(0f, forcedPlayerYaw, 0f);
            }
            else
            {
                // The target comes from the actual spawned Shopkeeper, so
                // this remains correct if the lift or shop is rotated.
                desiredForward.y = 0f;
                if (desiredForward.sqrMagnitude > .0001f)
                {
                    float shopYaw = Quaternion.LookRotation(desiredForward.normalized, Vector3.up).eulerAngles.y;
                    if (shopYaw > 180f)
                        shopYaw -= 360f;

                    // Lift and shop layouts use right-angle orientations.
                    // Snap away tiny world-position offsets such as -102°.
                    shopYaw = Mathf.Round(shopYaw / 90f) * 90f;
                    playerTargetRotation = Quaternion.Euler(0f, shopYaw, 0f);
                }
            }
            if (snapCameraToNeutralImmediately)
            {
                if (lockedPlayerMovement != null)
                    lockedPlayerMovement.transform.rotation = playerTargetRotation;

                lockedCameraLook.transform.localRotation = Quaternion.identity;
                lockedCameraLook.SyncRotationToCurrentView();
                cameraCenterRoutine = null;
            }
            else
            {
                cameraCenterRoutine = StartCoroutine(SmoothlyCenterLiftCamera(
                    lockedPlayerMovement.transform,
                    playerTargetRotation,
                    liftMidpoint));
            }
        }

        liftControlSessionActive = true;
    }

    public void EnableShopCameraLook()
    {
        if (lockedCameraLook == null)
            return;

        lockedCameraLook.SyncRotationToCurrentView();
        lockedCameraLook.SetRestrictedShopLook(true, .5f, 8f);
        lockedCameraLook.enabled = true;
    }

    public void UnlockPlayerAfterLiftTravel()
    {
        if (lockedCameraLook != null)
        {
            lockedCameraLook.SyncRotationToCurrentView();
            lockedCameraLook.SetRestrictedShopLook(false);
            lockedCameraLook.enabled = restoreCameraLook;
        }

        if (lockedPlayerMovement != null)
        {
            lockedPlayerMovement.cameraLocked = false;
            lockedPlayerMovement.enabled = restorePlayerMovement;
        }

        
        if (lockedCharacter != null)
            lockedCharacter.SetShopFiringLocked(false);
        lockedCharacter = null;
lockedCameraLook = null;
        lockedPlayerMovement = null;
        liftControlSessionActive = false;
    }

    /// <summary>
    /// Starts the survival health drain only after the player has actually
    /// crossed out of the arriving procedural lift.
    /// </summary>
    public void NotifyPlayerExitedArrivalLift(Lift exitedLift)
    {
        if (exitedLift == null)
            return;

        // During a prepared-level handoff, only the aligned arrival lift is
        // allowed to resume the drain.
        if (waitingArrivalLiftExits.Count > 0)
        {
            if (!waitingArrivalLiftExits.Remove(exitedLift))
                return;

            waitingArrivalLiftExits.Clear();
            FindFirstObjectByType<PlayerHealth>()?.ResumeHealthDrain();
            return;
        }

        // Set 0 has a StartTheGameplay trigger at the end of its spawn
        // corridor. Do not resume here or health starts draining as soon as
        // the player leaves the lift. The regular spawn-room prefab used by
        // sets 1–10 has no such corridor trigger, so those sets resume at
        // the initial lift exit instead.
        if (ActiveSet == 0)
            return;

        if (placedRooms.Count > 0 &&
            exitedLift.transform.IsChildOf(placedRooms[0].Instance.transform))
        {
            FindFirstObjectByType<PlayerHealth>()?.ResumeHealthDrain();
        }
    }

    private System.Collections.IEnumerator SmoothlyCenterLiftCamera(
        Transform playerTransform,
        Quaternion targetPlayerRotation,
        Transform liftMidpoint)
    {
        if (playerTransform == null)
            yield break;

        Quaternion startPlayerRotation = playerTransform.rotation;
        Vector3 startPlayerPosition = playerTransform.position;
        Vector3 targetPlayerPosition = startPlayerPosition;
        if (liftMidpoint != null)
        {
            targetPlayerPosition = liftMidpoint.position;
            // Keep the player's current ground height; the marker defines the
            // square's horizontal center rather than a vertical teleport.
            targetPlayerPosition.y = startPlayerPosition.y;
        }
        if (lockedCameraLook == null)
        {
            playerTransform.rotation = targetPlayerRotation;
            playerTransform.position = targetPlayerPosition;
            cameraCenterRoutine = null;
            yield break;
        }

        Transform cameraTransform = lockedCameraLook.transform;
        Quaternion startCameraRotation = cameraTransform.localRotation;
        Quaternion targetCameraRotation = Quaternion.identity;
        float elapsed = 0f;
        float duration = Mathf.Max(liftPlayerRotateDuration, liftCameraCenterDuration);

        // Rotate the player and center both camera axes in the same motion.
        // Neither yaw nor pitch waits for the other to finish.
        while (elapsed < duration && cameraTransform != null && playerTransform != null)
        {
            elapsed += Time.deltaTime;
            float playerT = Mathf.SmoothStep(0f, 1f, elapsed / liftPlayerRotateDuration);
            float cameraT = Mathf.SmoothStep(0f, 1f, elapsed / liftCameraCenterDuration);
            playerTransform.rotation = Quaternion.Slerp(startPlayerRotation, targetPlayerRotation, playerT);
            playerTransform.position = Vector3.Lerp(startPlayerPosition, targetPlayerPosition, playerT);
            cameraTransform.localRotation = Quaternion.Slerp(startCameraRotation, targetCameraRotation, cameraT);
            yield return null;
        }

        if (playerTransform != null)
        {
            playerTransform.rotation = targetPlayerRotation;
            playerTransform.position = targetPlayerPosition;
        }

        if (cameraTransform != null)
            cameraTransform.localRotation = targetCameraRotation;

        lockedCameraLook.SyncRotationToCurrentView();

        cameraCenterRoutine = null;
    }

    private System.Collections.IEnumerator OpenArrivalLiftAndRestoreControl()
    {
        if (activeLevelRoot == null)
            yield break;

        Lift[] arrivalLifts = activeLevelRoot.GetComponentsInChildren<Lift>(true);
        waitingArrivalLiftExits.Clear();
        foreach (Lift lift in arrivalLifts)
            waitingArrivalLiftExits.Add(lift);
        foreach (Lift lift in arrivalLifts)
            lift.CloseDoorsForTransition();

        // The new level is now in place. Hold the closed doors for one second
        // after travel has stopped, then reveal the new level in front of the
        // player by opening this same aligned lift.
        yield return new WaitForSeconds(1f);

        // Activate the prepared encounter while the doors still hide the new
        // level. It is already positioned on the rebuilt NavMesh, so it is
        // present as soon as the lift opens instead of popping in afterward.
        SetLevelEnemyActivation(activeLevelRoot, true);
        foreach (Lift lift in arrivalLifts)
            lift.OpenDoorsForArrival();

        // Survival time starts again when the arriving lift opens onto the
        // newly generated level, excluding all travel and shop time.
        ScoreManager.Instance?.ResumeSurvivalTimer();

        yield return new WaitForSeconds(.65f);
        UnlockPlayerAfterLiftTravel();
    }

    private void RebuildActiveLevelNavMesh()
    {
        if (activeLevelRoot == null)
            return;

        // Some room prefabs keep their NavMeshSurface on a scene-level helper
        // rather than directly below their room root. Rebuild every active
        // surface, while skipped old/prepared roots remain inactive.
        NavMeshSurface[] surfaces = FindObjectsOfType<NavMeshSurface>(true);
        foreach (NavMeshSurface surface in surfaces)
        {
            if (!surface.gameObject.activeInHierarchy)
                continue;

            surface.RemoveData();
            surface.BuildNavMesh();
        }

        // EnemySpawner.Start runs while the prepared level is far below the
        // map. Recreate its encounter now that the NavMesh is in its final
        // location, so enemies land on the generated rooms.
        EnemySpawner[] spawners = activeLevelRoot.GetComponentsInChildren<EnemySpawner>(true);
        foreach (EnemySpawner spawner in spawners)
            spawner.RespawnForRebuiltNavMesh();
    }

private static void SetLevelEnemyActivation(Transform levelRoot, bool allowed)
    {
        if (levelRoot == null)
            return;

        EnemySpawner[] spawners = levelRoot.GetComponentsInChildren<EnemySpawner>(true);
        foreach (EnemySpawner spawner in spawners)
            spawner.SetActivationAllowed(allowed);
    }


    private void EnsureActiveLevelRoot()
    {
        if (activeLevelRoot != null)
            return;

        GameObject root = new GameObject("Active Level");
        root.transform.SetParent(transform);

        List<Transform> roomRoots = new();
        foreach (Transform child in transform)
            if (child != root.transform)
                roomRoots.Add(child);

        foreach (Transform roomRoot in roomRoots)
            roomRoot.SetParent(root.transform, true);

        activeLevelRoot = root.transform;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        if (parent.name == childName)
            return parent;

        foreach (Transform child in parent)
        {
            Transform found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }

        return null;
    }

    private void CapturePlayerSpawnPoint()
    {
        if (placedRooms.Count == 0)
            return;

        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
            return;

        Transform startRoom = placedRooms[0].Instance.transform;
        playerSpawnOffset = startRoom.InverseTransformPoint(player.position);
        playerSpawnRotation = Quaternion.Inverse(startRoom.rotation) * player.rotation;
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
