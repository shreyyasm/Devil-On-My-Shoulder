using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelComplete : MonoBehaviour
{
    private static readonly string[] LiftHiddenPlayerUiNames =
    {
        "Finisher UI",
        "Demon Ability UI",
        "Reload UI"
    };

    [Header("Shop Lift References")]
    [SerializeField] private GameObject shopRoomPrefab;
    [SerializeField] private Animator liftAnimator;
    [SerializeField] private Transform liftTransform;
    [SerializeField] private Transform liftFrontSnapPoint;
    [SerializeField] private Transform liftMidpoint;
    [SerializeField] private ShopUpgradeSelection shopUpgradeSelection;
    [SerializeField, Min(.1f)] private float requiredLiftEntryDistance = 2.35f;

    [Header("Lift Timing")]
    [SerializeField, Min(.1f)] private float liftCloseAnimationTimeout = 2f;
    [SerializeField, Min(1f)] private float liftCloseAnimationSpeed = 2f;
    [SerializeField, Min(0f)] private float delayBeforeTravelShake = .15f;
    [SerializeField, Min(.05f)] private float liftEntryAlignmentDuration = .15f;
    [SerializeField, Min(.1f)] private float travelShakeDuration = .8f;
    [SerializeField, Min(0f)] private float travelShakeDistance = .045f;
    [SerializeField, Min(0f)] private float shopDoorOffset = .15f;
    [SerializeField, Min(0f)] private float shopSpawnSettleDelay = .15f;

    [Header("Shop Arrival Travel")]
    [Tooltip("How far above its door alignment the shop room begins before arriving.")]
    [SerializeField, Min(0f)] private float shopLiftTravelHeight = 12f;
    [SerializeField, Min(.05f)] private float shopLiftArrivalDuration = 1.75f;
    [SerializeField, Min(.05f)] private float shopLiftDepartureDuration = 1.5f;

    [Header("First Shop Introduction")]
    [SerializeField, Min(0f)] private float firstShopIntroductionDuration = 3.35f;
    [SerializeField, Min(0f)] private float shopIntroSlowMotionDelay = .5f;
    [SerializeField, Min(.01f)] private float shopIntroSlowMotionDuration = 1f;
    [SerializeField, Range(.01f, 1f)] private float firstShopTimeScale = .05f;
    [SerializeField, Min(0f)] private float soulKeeperTitleTravelDistance = 500f;

    private const string FirstShopIntroductionPreferenceKey = "DevilOnMyShoulder.FirstShopIntroductionPlayed";

    private bool transitionStarted;
    
    private bool shopSelectionMade;
private bool shopOpenForPlayer;
    private Transform shopkeeperTransform;
    private Vector3 lastShopkeeperPosition;
    private bool hasShopkeeperPosition;

    public bool IsTransitionInProgress => transitionStarted;
    private GameObject spawnedShop;
    private Vector3 authoredShopPosition;
    private bool hasAuthoredShopPosition;

    private void Awake()
    {
        ResolveLiftReferences();
    }

    private void OnEnable()
    {
        // Prepared rooms can be hidden and enabled again by the room
        // visibility system. Re-issue the open command when this happens so
        // the End Lift is always waiting open for the player.
        if (!transitionStarted)
            StartCoroutine(EnsureEndLiftIsOpen());
    }

    private IEnumerator Start()
    {
        yield return EnsureEndLiftIsOpen();
    }

    private IEnumerator EnsureEndLiftIsOpen()
    {
        // A generated End Room may be created while offstage, then enabled
        // later when the player reaches it. Give its controller several
        // frames to settle and keep the approach doors in their open state.
        yield return new WaitForEndOfFrame();

        for (int attempt = 0; attempt < 4 && !transitionStarted; attempt++)
        {
            OpenEndLiftForPlayer();
            yield return new WaitForSeconds(.2f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryBeginLiftTravel(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryBeginLiftTravel(other);
    }

    private void Update()
    {
        // Begin the lift handoff just as the player reaches its entrance,
        // instead of requiring them to walk all the way into the middle.
        if (transitionStarted)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Collider playerCollider = player != null ? player.GetComponent<Collider>() : null;
        if (playerCollider != null)
            TryBeginLiftTravel(playerCollider);
    }

    private void TryBeginLiftTravel(Collider other)
    {
        if (transitionStarted || !other.CompareTag("Player"))
            return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null || playerHealth.playerDead)
            return;

        ResolveLiftReferences();
        if (!IsPlayerInsideLift(other.transform.position))
            return;

        // The current floor ends as soon as the player commits to the lift.
        // Hide and clear the entire kill meter before the shop transition.
        ComboManager.Instance?.ResetForLiftTravel();
        HidePlayerGameplayUi(other.transform);
        playerHealth.CancelDeathCountdownForLift();
        playerHealth.PauseHealthDrain();
        DisableCurrentLevelEnemies();
        StartCoroutine(TravelToShop());
    }

    private bool IsPlayerInsideLift(Vector3 playerPosition)
    {
        if (liftTransform == null)
            return false;

        Vector3 localPosition = liftTransform.InverseTransformPoint(playerPosition);
        return Mathf.Abs(localPosition.x) <= requiredLiftEntryDistance &&
               Mathf.Abs(localPosition.z) <= requiredLiftEntryDistance;
    }

    private IEnumerator TravelToShop()
    {
        transitionStarted = true;
        ResolveLiftReferences();

        // The lift gets its own track once the player enters the End Room.
        // Regular gameplay music fades away before the doors close.
        BackgroundMusic.Instance?.StartElevatorMusic();

        // Build the shop behind the still-open front doors so its Shopkeeper
        // provides a real facing target during the entry alignment.
        SpawnShopRoom();
        ProceduralGenerationManager.Instance?.PrepareNextLevel();
        KeepCurrentLiftActive();
        ProceduralGenerationManager.Instance?.LockPlayerForLiftTravel(
            GetShopkeeperFacing(), liftMidpoint);
        yield return new WaitForSeconds(liftEntryAlignmentDuration);

        TriggerLift("CloseLift", liftCloseAnimationSpeed);
        yield return WaitForLiftToClose("LiftClose");
        ResetLiftAnimationSpeed();

        // The spawned shop begins two storeys above its door alignment, then descends
        // into place while the player is sealed inside. The End Lift stays fixed.
        yield return BringShopInFromAbove();
        yield return new WaitForSeconds(delayBeforeTravelShake);
        yield return ShakeLift();
        yield return new WaitForSeconds(shopSpawnSettleDelay);

        if (spawnedShop != null)
        {
            TriggerLift("OpenLift Front");
            shopOpenForPlayer = true;
            shopSelectionMade = false;

            yield return PlayShopIntroduction();

            StartCoroutine(RestoreShopControlAfterDoorOpens());
        }

        while (!shopSelectionMade && !Input.GetKeyDown(KeyCode.P))
            yield return null;

        
        FindFirstObjectByType<ShopGunOfferController>()?.CloseShop();
shopUpgradeSelection?.HideImmediate();
        shopOpenForPlayer = false;
        TriggerLift("CloseLift Front", liftCloseAnimationSpeed);
        yield return WaitForLiftToClose("Lift Close Front");
        ResetLiftAnimationSpeed();
        yield return new WaitForSeconds(delayBeforeTravelShake);
        ProceduralGenerationManager.Instance?.LockPlayerForLiftTravel(GetShopkeeperFacing());
        PlayerHealth.Instance?.RestoreHealthDuringLiftTravel();
        yield return ShakeLift();

        // Keep the doors closed while the shop room rises away, then reveal the
        // next level through the normal lift handoff.
        yield return SendShopUpward();

        if (spawnedShop != null)
            Destroy(spawnedShop);

        ProceduralGenerationManager.Instance?.ActivatePreparedLevel(liftTransform);
    }

private IEnumerator RestoreShopControlAfterDoorOpens()
    {
        yield return new WaitForSeconds(.6f);
        if (!shopOpenForPlayer)
            yield break;

        if (shopUpgradeSelection == null)
        {
            foreach (ShopUpgradeSelection candidate in Resources.FindObjectsOfTypeAll<ShopUpgradeSelection>())
            {
                if (candidate.gameObject.scene.IsValid())
                {
                    shopUpgradeSelection = candidate;
                    break;
                }
            }
        }

        ProceduralGenerationManager.Instance?.EnableShopCameraLook();
        shopUpgradeSelection?.SetSelectionCallback(BeginGunOffers);
        shopUpgradeSelection?.ShowForShop();
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

        if (liftMidpoint == null && liftTransform != null)
            liftMidpoint = FindChildRecursive(liftTransform, "Lift Midpoint");
    }

    private void OpenEndLiftForPlayer()
    {
        // The End Room has its own controller. Start its front-door opening
        // through the controller trigger so its transition plays normally.
        ResolveLiftReferences();
        if (liftAnimator == null)
            return;

        // Entering the End Lift uses its normal opening direction.
        TriggerLift("OpenLift");
    }

    /// <summary>Called by level generation after a prepared level becomes live.</summary>
    public void OpenLiftForPlayer()
    {
        if (!transitionStarted)
            OpenEndLiftForPlayer();
    }

    private void TriggerLift(string trigger, float animationSpeed = 1f)
    {
        if (liftAnimator == null)
            return;

        liftAnimator.enabled = true;
        liftAnimator.speed = animationSpeed;
        liftAnimator.ResetTrigger("OpenLift");
        liftAnimator.ResetTrigger("CloseLift");
        liftAnimator.ResetTrigger("OpenLift Front");
        liftAnimator.ResetTrigger("CloseLift Front");
        liftAnimator.SetTrigger(trigger);
    }

    private void ResetLiftAnimationSpeed()
    {
        if (liftAnimator != null)
            liftAnimator.speed = 1f;
    }

    private void KeepCurrentLiftActive()
    {
        Transform roomRoot = transform.parent != null ? transform.parent : transform.root;
        if (roomRoot != null)
            roomRoot.gameObject.SetActive(true);

        if (liftTransform != null)
            liftTransform.gameObject.SetActive(true);

        if (liftAnimator != null)
            liftAnimator.enabled = true;
    }

    private static void DisableCurrentLevelEnemies()
    {
        EnemyManager[] enemies = FindObjectsOfType<EnemyManager>(true);
        foreach (EnemyManager enemy in enemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
                enemy.gameObject.SetActive(false);
        }
    }

    private static void HidePlayerGameplayUi(Transform playerTransform)
    {
        if (playerTransform == null)
            return;

        Transform uiRoot = FindChildRecursive(playerTransform, "UI");
        if (uiRoot == null)
            return;

        foreach (string uiName in LiftHiddenPlayerUiNames)
        {
            Transform uiElement = FindChildRecursive(uiRoot, uiName);
            if (uiElement != null)
                uiElement.gameObject.SetActive(false);
        }
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
        // The authored prefab calls this child "Shop Keeper". Keep the old
        // spelling as a fallback so alternate shop prefabs still work.
        shopkeeperTransform = FindChildRecursive(spawnedShop.transform, "Shop Keeper");
        if (shopkeeperTransform == null)
            shopkeeperTransform = FindChildRecursive(spawnedShop.transform, "Shopkeeper");
        if (shopkeeperTransform != null)
        {
            lastShopkeeperPosition = shopkeeperTransform.position;
            hasShopkeeperPosition = true;
        }

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

        // Keep the End Lift fixed. Only the newly spawned shop room starts high
        // above its door alignment and descends after the lift seals.
        CacheAuthoredShopPosition();
        spawnedShop.transform.position = authoredShopPosition + Vector3.up * shopLiftTravelHeight;
        shopDoor.Open();
    }

    /// <summary>Locks the player and resets the view before the shop exit doors close.</summary>
    public void SnapPlayerToShopExitView()
    {
        ProceduralGenerationManager.Instance?.LockPlayerForLiftTravel(
            GetShopkeeperFacing(),
            null,
            float.NaN,
            true);
    }
    private Vector3 GetShopkeeperFacing()
    {
        Vector3 targetPosition = shopkeeperTransform != null
            ? shopkeeperTransform.position
            : hasShopkeeperPosition
                ? lastShopkeeperPosition
                : liftFrontSnapPoint != null
                    ? liftFrontSnapPoint.position + liftFrontSnapPoint.forward
                    : Vector3.zero;

        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
            return Vector3.zero;

        Vector3 facing = targetPosition - player.position;
        facing.y = 0f;
        return facing;
    }

    private IEnumerator PlayShopIntroduction()
    {
        // The character and camera introductions are part of every shop visit.
        // Only the slow-motion title beat is a one-time introduction.
        bool isFirstShopEncounter = PlayerPrefs.GetInt(FirstShopIntroductionPreferenceKey, 0) == 0;
        SetShopIntroVisualsActive(true);

        CharacterAbilities abilities = CharacterAbilities.instance;
        Animator finisherCameraAnimator = null;
        // CharacterAbilities already stores the player Character reference.
        // Use it directly because this lift component is not on the player.
        var playerCharacter = abilities != null ? abilities.character : null;

        if (abilities != null)
        {
            // Match the normal finisher-camera setup so this animation is
            // actually visible before its Animator receives the trigger.
            if (abilities.finisherCamera != null)
            {
                abilities.finisherCamera.SetActive(true);
                finisherCameraAnimator = abilities.finisherCamera.GetComponent<Animator>();
            }

            if (playerCharacter != null && playerCharacter.characterAnimator != null)
                playerCharacter.characterAnimator.SetBool("FinisherUnholstered", true);

            if (abilities.renderFinisherCanvas != null)
                abilities.renderFinisherCanvas.SetActive(true);

            if (abilities.mainCameraTexture != null)
                abilities.mainCameraTexture.SetActive(false);
        }

        Animator shopkeeperAnimator = FindShopkeeperIntroductionAnimator();
        TriggerAnimatorIfPresent(shopkeeperAnimator, "Introduction");
        TriggerAnimatorIfPresent(finisherCameraAnimator, "Shop Introduction");

        if (isFirstShopEncounter)
        {
            StartCoroutine(AnimateSoulKeeperTitle());

            // The beat deliberately starts half a second into the intro, so
            // the animation is already in motion before time slows down.
            yield return new WaitForSecondsRealtime(shopIntroSlowMotionDelay);

            // Preserve any time scale already established by another system,
            // then restore that exact value after the first Shop Keeper beat.
            float previousTimeScale = Time.timeScale;
            Time.timeScale = Mathf.Min(previousTimeScale, firstShopTimeScale);
            yield return new WaitForSecondsRealtime(shopIntroSlowMotionDuration);
            Time.timeScale = previousTimeScale;
            PlayerPrefs.SetInt(FirstShopIntroductionPreferenceKey, 1);
            PlayerPrefs.Save();

            yield return new WaitForSeconds(Mathf.Max(0f, firstShopIntroductionDuration - shopIntroSlowMotionDelay));

            // Keep the title readable at centre until the authored Shop Keeper
            // and finisher-camera introductions have finished, then send it away.
            yield return AnimateSoulKeeperTitleExit();
        }
        else
        {
            yield return new WaitForSeconds(firstShopIntroductionDuration);
        }

        // Return to the regular shop view before the card hologram appears.
        if (abilities != null)
        {
            if (playerCharacter != null && playerCharacter.characterAnimator != null)
                playerCharacter.characterAnimator.SetBool("FinisherUnholstered", false);

            if (abilities.finisherCamera != null)
                abilities.finisherCamera.SetActive(false);

            if (abilities.renderFinisherCanvas != null)
                abilities.renderFinisherCanvas.SetActive(false);

            if (abilities.mainCameraTexture != null)
                abilities.mainCameraTexture.SetActive(true);
        }

        SetShopIntroVisualsActive(false);
    }

    private Animator FindShopkeeperIntroductionAnimator()
    {
        if (spawnedShop == null)
            return null;

        // Resolve by the trigger the introduction needs, rather than relying
        // on the exact object that happens to hold the Animator component.
        foreach (Animator animator in spawnedShop.GetComponentsInChildren<Animator>(true))
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Trigger &&
                    parameter.name == "Introduction")
                    return animator;
            }
        }

        Debug.LogWarning("[LevelComplete] Could not find a Shop Keeper Animator with an Introduction trigger.", spawnedShop);
        return null;
    }

    private void SetShopIntroVisualsActive(bool active)
    {
        if (spawnedShop == null)
            return;

        Transform background = FindChildRecursive(spawnedShop.transform, "Intro BG");
        if (background != null)
            background.gameObject.SetActive(active);

        Transform title = FindChildRecursive(spawnedShop.transform, "Soul Keeper") ??
                          FindChildRecursive(spawnedShop.transform, "Sol - keeper Sprite");
        if (title != null && !active)
            title.gameObject.SetActive(false);
    }

    private IEnumerator AnimateSoulKeeperTitle()
    {
        if (spawnedShop == null)
            yield break;

        Transform title = FindChildRecursive(spawnedShop.transform, "Soul Keeper") ??
                          FindChildRecursive(spawnedShop.transform, "Sol - keeper Sprite");
        if (title == null)
            yield break;

        Vector3 centre = title.localPosition;
        Vector3 left = centre + Vector3.left * soulKeeperTitleTravelDistance;
        title.localPosition = left;
        title.gameObject.SetActive(true);

        // Arrive at centre exactly when the slow-motion beat begins.
        float elapsed = 0f;
        while (elapsed < shopIntroSlowMotionDelay)
        {
            elapsed += Time.unscaledDeltaTime;
            title.localPosition = Vector3.Lerp(left, centre,
                Mathf.Clamp01(elapsed / Mathf.Max(.01f, shopIntroSlowMotionDelay)));
            yield return null;
        }

        title.localPosition = centre;
    }

    private IEnumerator AnimateSoulKeeperTitleExit()
    {
        if (spawnedShop == null)
            yield break;

        Transform title = FindChildRecursive(spawnedShop.transform, "Soul Keeper") ??
                          FindChildRecursive(spawnedShop.transform, "Sol - keeper Sprite");
        if (title == null || !title.gameObject.activeSelf)
            yield break;

        Vector3 centre = title.localPosition;
        Vector3 right = centre + Vector3.right * soulKeeperTitleTravelDistance;
        const float exitDuration = .35f;
        float elapsed = 0f;
        while (elapsed < exitDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            title.localPosition = Vector3.Lerp(centre, right, Mathf.Clamp01(elapsed / exitDuration));
            yield return null;
        }

        title.localPosition = centre;
        title.gameObject.SetActive(false);
    }

    private static void TriggerAnimatorIfPresent(Animator animator, string trigger)
    {
        if (animator == null)
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == trigger)
            {
                animator.SetTrigger(trigger);
                return;
            }
        }

        Debug.LogWarning($"[LevelComplete] Animator '{animator.name}' does not define the '{trigger}' trigger.", animator);
    }

    private void CacheAuthoredShopPosition()
    {
        if (spawnedShop == null || hasAuthoredShopPosition)
            return;

        authoredShopPosition = spawnedShop.transform.position;
        hasAuthoredShopPosition = true;
    }

    private IEnumerator BringShopInFromAbove()
    {
        if (spawnedShop == null)
            yield break;

        CacheAuthoredShopPosition();
        yield return MoveShopTo(authoredShopPosition, shopLiftArrivalDuration);
    }

    private IEnumerator SendShopUpward()
    {
        if (spawnedShop == null)
            yield break;

        CacheAuthoredShopPosition();
        yield return MoveShopTo(authoredShopPosition + Vector3.up * shopLiftTravelHeight, shopLiftDepartureDuration);
    }

    private IEnumerator MoveShopTo(Vector3 targetPosition, float duration)
    {
        if (spawnedShop == null)
            yield break;

        Vector3 start = spawnedShop.transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            if (spawnedShop != null)
                spawnedShop.transform.position = Vector3.LerpUnclamped(start, targetPosition, progress);
            yield return null;
        }

        if (spawnedShop != null)
            spawnedShop.transform.position = targetPosition;
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

    private IEnumerator WaitForLiftToClose(string closeStateName)
    {
        if (liftAnimator == null)
            yield break;

        float elapsed = 0f;
        bool enteredCloseState = false;

        while (elapsed < liftCloseAnimationTimeout)
        {
            AnimatorStateInfo state = liftAnimator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(closeStateName))
            {
                enteredCloseState = true;
                if (state.normalizedTime >= .98f)
                    yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!enteredCloseState)
            Debug.LogWarning($"[LevelComplete] Lift did not enter close state '{closeStateName}' before the timeout.", this);
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


private void BeginGunOffers()
    {
        shopUpgradeSelection?.HideImmediate();

        ShopGunOfferController gunOffers =
            FindFirstObjectByType<ShopGunOfferController>();

        if (gunOffers == null || spawnedShop == null)
        {
            shopSelectionMade = true;
            return;
        }

        gunOffers.ShowOffers(spawnedShop.transform, () => shopSelectionMade = true);
    }
}




