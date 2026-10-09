using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scene-only bootstrap for DOMS Tutorial. It never changes the normal
/// procedural generation flow used by Mad Dash Game.
/// </summary>
public class DomesTutorialBootstrap : MonoBehaviour
{
    [Serializable]
    public class FixedRoom
    {
        public string label;
        public GameObject prefab;
        [Tooltip("Deterministic door index used to leave this room.")]
        public int exitDoorIndex;
        [Tooltip("-1 uses the authored Is Start Door, otherwise this exact door index is used.")]
        public int entryDoorIndex = -1;
    }

    [Header("Fixed Tutorial Layout")]
    [SerializeField] private FixedRoom[] roomSequence;
    [SerializeField, Min(0f)] private float doorGap = .2f;
    [SerializeField] private bool disableRoomEnemySpawners = true;

    [Header("Tutorial Rules")]
    [SerializeField] private bool allowHealthDrainForDemo;
    [SerializeField, Min(.05f)] private float blackoutFadeDuration = .85f;

    private GameObject player;
    private PlayerHealth playerHealth;
    private CanvasGroup blackout;
    private Transform layoutRoot;

    private void Awake()
    {
        CreateBlackoutOverlay();

        // The tutorial owns its layout. The main procedural generator is
        // disabled only in this scene, before it can subscribe to generation.
        ProceduralGenerationManager generator = FindFirstObjectByType<ProceduralGenerationManager>();
        if (generator != null)
            generator.enabled = false;

        RoomVisibilityManager visibility = FindFirstObjectByType<RoomVisibilityManager>();
        if (visibility != null)
            visibility.enabled = false;

        player = GameObject.FindGameObjectWithTag("Player");
        playerHealth = player != null ? player.GetComponent<PlayerHealth>() : null;
        if (!allowHealthDrainForDemo)
            playerHealth?.PauseHealthDrain();
    }

    private IEnumerator Start()
    {
        yield return null;
        BuildFixedLayout();
        PlacePlayerInSpawnLift();
        yield return new WaitForSeconds(.15f);
        yield return FadeBlackoutOut();
    }

    private void LateUpdate()
    {
        if (!allowHealthDrainForDemo)
        {
            if (playerHealth == null)
                playerHealth = FindFirstObjectByType<PlayerHealth>();

            playerHealth?.PauseHealthDrain();
        }
    }

    /// <summary>Available for a later health-drain tutorial step.</summary>
    public void SetHealthDrainDemo(bool enabled)
    {
        allowHealthDrainForDemo = enabled;
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (enabled)
            playerHealth?.ResumeHealthDrain();
        else
            playerHealth?.PauseHealthDrain();
    }

    private void BuildFixedLayout()
    {
        if (roomSequence == null || roomSequence.Length == 0)
        {
            Debug.LogError("[Tutorial] No fixed tutorial rooms are assigned.", this);
            return;
        }

        layoutRoot = new GameObject("Tutorial Fixed Layout").transform;

        GameObject firstPrefab = roomSequence[0].prefab;
        if (firstPrefab == null)
        {
            Debug.LogError("[Tutorial] The first room prefab is missing.", this);
            return;
        }

        GameObject previous = Instantiate(firstPrefab, Vector3.zero, Quaternion.identity, layoutRoot);
        previous.name = "Tutorial 01 - " + roomSequence[0].label;
        PrepareTutorialRoom(previous);
        Door[] previousDoors = GetDoors(previous);

        for (int index = 1; index < roomSequence.Length; index++)
        {
            FixedRoom step = roomSequence[index];
            if (step.prefab == null)
            {
                Debug.LogError("[Tutorial] Missing prefab at sequence index " + index, this);
                break;
            }

            Door exitDoor = GetDoor(previousDoors, roomSequence[index - 1].exitDoorIndex);
            if (exitDoor == null || exitDoor.SnapPoint == null)
            {
                Debug.LogError("[Tutorial] Could not find the fixed exit door for room " + index, previous);
                break;
            }

            GameObject next = Instantiate(step.prefab, Vector3.zero, Quaternion.identity, layoutRoot);
            next.name = "Tutorial " + (index + 1).ToString("00") + " - " + step.label;
            PrepareTutorialRoom(next);

            Door entryDoor = GetEntryDoor(next, step.entryDoorIndex);
            if (entryDoor == null || entryDoor.SnapPoint == null)
            {
                Debug.LogError("[Tutorial] Could not find the fixed entry door for room " + (index + 1), next);
                Destroy(next);
                break;
            }

            Quaternion exitFacing = Quaternion.LookRotation(-exitDoor.SnapPoint.forward, Vector3.up);
            Quaternion entryFacing = Quaternion.LookRotation(entryDoor.SnapPoint.forward, Vector3.up);
            next.transform.rotation = exitFacing * Quaternion.Inverse(entryFacing);
            next.transform.position += exitDoor.SnapPoint.position - entryDoor.SnapPoint.position;
            next.transform.position += exitDoor.SnapPoint.forward * doorGap;

            exitDoor.Open();
            entryDoor.Open();
            previous = next;
            previousDoors = GetDoors(previous);
        }

        foreach (Door door in layoutRoot.GetComponentsInChildren<Door>(true))
            if (!door.IsUsed)
                door.Close();
    }

    private void PrepareTutorialRoom(GameObject room)
    {
        foreach (EnemySpawner spawner in room.GetComponentsInChildren<EnemySpawner>(true))
            spawner.enabled = !disableRoomEnemySpawners;

        foreach (LevelComplete completion in room.GetComponentsInChildren<LevelComplete>(true))
            completion.enabled = false;

        foreach (StartTheGameplay start in room.GetComponentsInChildren<StartTheGameplay>(true))
            start.enabled = false;
    }

    private void PlacePlayerInSpawnLift()
    {
        if (player == null || layoutRoot == null || layoutRoot.childCount == 0)
            return;

        Transform spawnRoom = layoutRoot.GetChild(0);
        Transform lift = FindChild(spawnRoom, "Lift_New");
        Transform midpoint = lift != null ? FindChild(lift, "Lift Midpoint") : null;
        if (midpoint != null)
        {
            Vector3 position = midpoint.position;
            position.y = player.transform.position.y;
            player.transform.position = position;
        }

        foreach (Lift liftController in spawnRoom.GetComponentsInChildren<Lift>(true))
            liftController.OpenDoorsForArrival();
    }

    private void CreateBlackoutOverlay()
    {
        GameObject overlay = new GameObject("Tutorial Blackout", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        blackout = overlay.GetComponent<CanvasGroup>();
        blackout.alpha = 1f;
        blackout.blocksRaycasts = true;

        GameObject blackImage = new GameObject("Black", typeof(Image));
        blackImage.transform.SetParent(overlay.transform, false);
        RectTransform rect = blackImage.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        blackImage.GetComponent<Image>().color = Color.black;
    }

    private IEnumerator FadeBlackoutOut()
    {
        float elapsed = 0f;
        while (elapsed < blackoutFadeDuration)
        {
            elapsed += Time.deltaTime;
            blackout.alpha = 1f - Mathf.Clamp01(elapsed / blackoutFadeDuration);
            yield return null;
        }

        blackout.alpha = 0f;
        blackout.blocksRaycasts = false;
    }

    private static Door[] GetDoors(GameObject room)
    {
        return room.GetComponentsInChildren<Door>(true);
    }

    private static Door GetDoor(Door[] doors, int index)
    {
        if (doors == null)
            return null;

        // Use the requested authored door when it is still available. If it
        // was consumed as this room's entry, use the next free door in the
        // prefab hierarchy. This is stable and never randomizes the route.
        if (index >= 0 && index < doors.Length && !doors[index].IsUsed)
            return doors[index];

        foreach (Door door in doors)
            if (!door.IsUsed)
                return door;

        return null;
    }

    private static Door GetEntryDoor(GameObject room, int explicitIndex)
    {
        Door[] doors = GetDoors(room);
        if (explicitIndex >= 0)
            return GetDoor(doors, explicitIndex);

        foreach (Door door in doors)
            if (door.IsStartDoor)
                return door;

        return GetDoor(doors, 0);
    }

    private static Transform FindChild(Transform root, string childName)
    {
        if (root == null)
            return null;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == childName)
                return child;

        return null;
    }
}
