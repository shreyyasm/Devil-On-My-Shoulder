using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelComplete : MonoBehaviour
{
    [Header("Shop Lift References")]
    [SerializeField] private GameObject shopRoomPrefab;
    [SerializeField] private Animator liftAnimator;
    [SerializeField] private Transform liftTransform;
    [SerializeField] private Transform liftFrontSnapPoint;

    [Header("Lift Timing")]
    [SerializeField, Min(0f)] private float doorCloseDelay = .45f;
    [SerializeField, Min(.1f)] private float travelShakeDuration = 1.6f;
    [SerializeField, Min(0f)] private float travelShakeDistance = .045f;
    [SerializeField, Min(0f)] private float shopDoorOffset = .15f;
    [SerializeField, Min(0f)] private float shopSpawnSettleDelay = .15f;

    private bool transitionStarted;
    private GameObject spawnedShop;

    private void Awake()
    {
        ResolveLiftReferences();
    }

    private IEnumerator Start()
    {
        // The generated room can be enabled before its lift Animator has
        // initialized. Wait one frame so the open state is not overwritten.
        yield return null;
        OpenEndLiftForPlayer();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (transitionStarted || !other.CompareTag("Player"))
            return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null || playerHealth.playerDead)
            return;

        StartCoroutine(TravelToShop());
    }

    private IEnumerator TravelToShop()
    {
        transitionStarted = true;
        ResolveLiftReferences();

        TriggerLift("CloseLift");
        yield return new WaitForSeconds(doorCloseDelay);
        yield return ShakeLift();

        SpawnShopRoom();
        yield return new WaitForSeconds(shopSpawnSettleDelay);

        if (spawnedShop != null)
            TriggerLift("OpenLift Front");

        while (!Input.GetKeyDown(KeyCode.Escape))
            yield return null;

        TriggerLift("CloseLift Front");
        yield return new WaitForSeconds(doorCloseDelay);
        yield return ShakeLift();

        if (ProceduralGenerationManager.Instance != null)
            ProceduralGenerationManager.Instance.ActiveSet++;

        ScoreManager.Instance?.IncreaseLevel();
        SceneManager.LoadSceneAsync("Mad Dash Game");
    }

    private void ResolveLiftReferences()
    {
        // At runtime every generated room is parented under ProceduralGenerator.
        // This trigger is a direct child of the End Room, so use that room
        // instance instead of transform.root (which would find the start lift).
        Transform roomRoot = transform.parent != null ? transform.parent : transform.root;

        if (liftTransform == null)
        {
            Transform lift = FindChildRecursive(roomRoot, "Lift_New");
            liftTransform = lift != null ? lift : roomRoot;
        }

        if (liftAnimator == null && liftTransform != null)
            liftAnimator = liftTransform.GetComponentInChildren<Animator>(true);

        if (liftFrontSnapPoint == null)
        {
            // This marker is authored specifically for the shop connection.
            // Align the Shop Room's own Door/Snap Point directly to it.
            liftFrontSnapPoint = FindChildRecursive(roomRoot, "Shop Snap Point");

            if (liftFrontSnapPoint == null && liftTransform != null)
                liftFrontSnapPoint = FindChildRecursive(liftTransform, "Door");
        }
    }

    private void OpenEndLiftForPlayer()
    {
        // The End Room has its own controller. Start its front-door opening
        // through the controller trigger so its transition plays normally.
        ResolveLiftReferences();
        if (liftAnimator == null)
            return;

        TriggerLift("OpenLift");
    }

    private void TriggerLift(string trigger)
    {
        if (liftAnimator == null)
            return;

        liftAnimator.enabled = true;
        liftAnimator.ResetTrigger("OpenLift");
        liftAnimator.ResetTrigger("CloseLift");
        liftAnimator.ResetTrigger("OpenLift Front");
        liftAnimator.ResetTrigger("CloseLift Front");
        liftAnimator.SetTrigger(trigger);
    }

    private void SpawnShopRoom()
    {
        if (spawnedShop != null || shopRoomPrefab == null || liftFrontSnapPoint == null)
        {
            if (shopRoomPrefab == null)
                Debug.LogError("[LevelComplete] Shop Room prefab is not assigned.", this);
            return;
        }

        spawnedShop = Instantiate(shopRoomPrefab);
        Door shopDoor = FindEntryDoor(spawnedShop);

        if (shopDoor == null || shopDoor.SnapPoint == null)
        {
            Debug.LogError("[LevelComplete] Shop Room requires a Door with a Snap Point.", spawnedShop);
            Destroy(spawnedShop);
            spawnedShop = null;
            return;
        }

        Quaternion liftFacing = Quaternion.LookRotation(-liftFrontSnapPoint.forward, Vector3.up);
        Quaternion shopFacing = Quaternion.LookRotation(shopDoor.SnapPoint.forward, Vector3.up);
        spawnedShop.transform.rotation = liftFacing * Quaternion.Inverse(shopFacing);
        spawnedShop.transform.position += liftFrontSnapPoint.position - shopDoor.SnapPoint.position;
        spawnedShop.transform.position += liftFrontSnapPoint.forward * shopDoorOffset;
        shopDoor.Open();
    }

    private IEnumerator ShakeLift()
    {
        if (liftTransform == null)
        {
            yield return new WaitForSeconds(travelShakeDuration);
            yield break;
        }

        Vector3 startingPosition = liftTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < travelShakeDuration)
        {
            elapsed += Time.deltaTime;
            Vector2 shake = Random.insideUnitCircle * travelShakeDistance;
            liftTransform.localPosition = startingPosition + new Vector3(shake.x, shake.y, 0f);
            yield return null;
        }

        liftTransform.localPosition = startingPosition;
    }

    private void PlayLiftState(string stateName, string fallbackTrigger)
    {
        if (liftAnimator == null)
            return;

        liftAnimator.enabled = true;
        // Animator state lookups need the full layer path. The short name
        // failed on the generated End Room lift and fell back to a transition
        // that does not leave its player-facing doors open.
        int stateHash = Animator.StringToHash("Base Layer." + stateName);
        if (liftAnimator.HasState(0, stateHash))
        {
            liftAnimator.Play(stateHash, 0, 0f);
            liftAnimator.Update(0f);
        }
        else
            liftAnimator.SetTrigger(fallbackTrigger);
    }

    private static Door FindEntryDoor(GameObject room)
    {
        Door[] doors = room.GetComponentsInChildren<Door>(true);
        foreach (Door door in doors)
            if (door.IsStartDoor)
                return door;

        return doors.Length > 0 ? doors[0] : null;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
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
}
