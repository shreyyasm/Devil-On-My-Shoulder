using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using InfimaGames.LowPolyShooterPack;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShopGunOfferController : MonoBehaviour{
    [Serializable]
    private class GunOfferDefinition
    {
        [Tooltip("The actual player weapon. Its projectile, fire rate, capacity, and recoil drive this offer's statistics.")]
        public Weapon weaponPrefab;
        [Tooltip("The mesh-only prefab presented on the shop table.")]
        public GameObject displayPrefab;
        [Tooltip("The 2D image shown in the left preview panel of this gun's information card.")]
        public Sprite previewSprite;
        [Tooltip("Optional label. Leave empty to use the weapon object name.")]
        public string displayName;
        [Min(0), Tooltip("Soul currency required to purchase this weapon.")]
        public int soulPrice = 1000;
        [Range(0f, 100f), Tooltip("Selection chance weight from 0 to 100. Zero disables this gun; a value of 50 is half as likely as a gun set to 100.")]
        public float probability = 100f;
    }
    [Header("3D Gun Display Prefabs")]
    [Tooltip("Legacy mesh list used only while Gun Offers is empty.")]
    [SerializeField] private GameObject[] gunDisplayPrefabs;
    [Header("Gun Offers")]
    [Tooltip("Assign each actual player weapon, its matching presentation mesh, and its relative selection chance.")]
    [SerializeField] private List<GunOfferDefinition> gunOffers = new();
    [SerializeField, Min(.05f)] private float gunOfferSpacing = .7f;
    [SerializeField] private float gunOfferGroupLeftOffset = .25f;
    [SerializeField, Min(0f)] private float gunTableSurfaceOffset = .015f;
    [SerializeField] private Vector3 gunInfoCardLocalPosition = new Vector3(0f, .45f, 0f);
    [SerializeField] private Vector3 gunInfoCardLocalScale = new Vector3(.00052f, .000576f, .000576f);
    [SerializeField, Min(0f)] private float gunInfoCardCameraClearance = .18f;
    [SerializeField, Min(0f)] private float gunInfoCardViewportPadding = .06f;
    [Header("Gun Information Card")]
    [SerializeField, Tooltip("Editable scene canvas cloned when a gun is hovered. It is created as a child named Gun Information Card Template when this field is empty.")]
    private Canvas gunInformationCardTemplate;
    [SerializeField, Tooltip("Animated polygon material used by each hover information panel.")]
    private Material gunInformationBackgroundMaterial;
    [SerializeField] private Color gunInformationPolygonColor = Color.black;
    [Header("Shop Controls")]
    [SerializeField, Tooltip("Saved world-space skip button. Edit its canvas, text, collider, and appearance directly in the hierarchy.")]
    private GameObject skipShopButton;
    [SerializeField, Tooltip("Name of the transform inside the Shop Room prefab that controls where the saved Skip Shop Button appears.")]
    private string skipButtonAnchorName = "Skip Shop Button Anchor";
    [Header("Shop Wallet")]
    [SerializeField, Tooltip("Font used by the SOULS wallet shown in the top-left of the shop view. Leave empty to use Unity's built-in font.")]
    private Font shopSoulCurrencyFont;
    [SerializeField, Tooltip("BoldPixels font used for the insufficient-sol purchase message.")]
    private TMP_FontAsset insufficientSolsFont;
    [Header("Hold To Purchase")]
    [SerializeField, Min(.1f)] private float purchaseHoldDuration = 1.15f;
    [SerializeField, Min(.1f)] private float purchaseReleaseDrainDuration = .85f;
[Header("Arsenal Camera")]
    [SerializeField, Min(.1f)] private float arsenalCameraTransitionDuration = .85f;
    [SerializeField, Min(.05f)] private float arsenalCameraReturnDuration = .45f;
    [SerializeField, Min(0f), Tooltip("Extra pause after returning to the player camera before drawing the purchased weapon.")]
    private float weaponRevealDelayAfterReturn = .1f;
    [SerializeField, Min(.1f)] private float arsenalCameraHeight = 1.65f;
    [SerializeField, Min(0f)] private float arsenalCameraBackOffset = 1.65f;
    [SerializeField, Range(20f, 90f)] private float arsenalCameraFieldOfView = 40f;
    [SerializeField, Range(5f, 85f)] private float arsenalCameraPitch = 45f;
    [SerializeField, Min(0f)] private float arsenalCameraLookAtPlayerOffset = .2f;
    [SerializeField] private int arsenalCameraCanvasOrder = 250;
    private readonly List<GameObject> offers = new();
    private readonly List<WeaponBehaviour> offeredWeapons = new();
    private readonly List<GunOfferDefinition> offeredDefinitions = new();
    private Action completed;
    private Camera playerCamera;
    private Camera gameplayCamera;
    private bool completingPurchase;
    private Vector2 cursorPosition;
    private RectTransform cursorVisual;
    private GameObject crosshair;
    private GameObject hoveredOffer;
    private Camera arsenalCamera;
    private GameObject arsenalCameraRoot;
    private Canvas arsenalCameraCanvas;
    private Text shopSoulCurrencyText;
    private RenderTexture arsenalRenderTexture;
    private Material arsenalRetroLookMaterial;
    private Character pendingWeaponRevealCharacter;
    private GunStats equippedWeaponStats;
    private Vector3 skipButtonRestScale;
    private bool hasSkipButtonRestScale;
    private bool skipButtonHovered;
    private Transform playerTransform;
    private Vector3 playerPositionBeforeOffers;
    private Vector3 gameplayCameraPositionBeforeOffers;
    private Quaternion gameplayCameraRotationBeforeOffers;
    private float gameplayCameraFieldOfViewBeforeOffers;
    private bool hasReturnSnapshot;
    private void OnValidate()
    {
        if (!Application.isPlaying)
            EnsureGunInformationCardTemplate();
    }
    public void ShowOffers(Transform shopRoot, Action onCompleted)
    {
        ClearOffers();
        completed = onCompleted;
        gameplayCamera = FindGameplayCamera();
        playerCamera = gameplayCamera;
        GameObject player = GameObject.FindWithTag("Player");
        playerTransform = player != null ? player.transform : null;
        hasReturnSnapshot = playerTransform != null && gameplayCamera != null;
        if (hasReturnSnapshot)
        {
            playerPositionBeforeOffers = playerTransform.position;
            gameplayCameraPositionBeforeOffers = gameplayCamera.transform.position;
            gameplayCameraRotationBeforeOffers = gameplayCamera.transform.rotation;
            gameplayCameraFieldOfViewBeforeOffers = gameplayCamera.fieldOfView;
        }
        Inventory inventory = player != null ? player.GetComponentInChildren<Inventory>(true) : null;
        WeaponBehaviour equipped = inventory != null ? inventory.GetEquipped() : null;
        equippedWeaponStats = GetStats(equipped);
        List<GunOfferDefinition> candidates = GetOfferCandidates(player, inventory, equipped);
        while (candidates.Count > 0 && offeredDefinitions.Count < 3)
        {
            GunOfferDefinition chosen = ChooseWeightedAndRemove(candidates);
            if (chosen == null)
                break;
            offeredDefinitions.Add(chosen);
            offeredWeapons.Add(chosen.weaponPrefab);
        }
        while (offeredWeapons.Count < 3)
        {
            offeredWeapons.Add(null);
            offeredDefinitions.Add(null);
        }
        Transform table = FindChild(shopRoot, "Table");
        Vector3 center = table != null ? table.position : shopRoot.position + shopRoot.forward * 2f;
        center.y = -9.5f;
        center.z = -116.5f;
        Vector3 right = table != null ? table.right : shopRoot.right;
        Transform shopkeeper = FindChild(shopRoot, "Shop Keeper") ?? FindChild(shopRoot, "Shopkeeper");
        Transform centerSlot = FindChild(shopRoot, "Gun Offer Slot 2");
        Vector3 offerCenter = centerSlot != null ? centerSlot.position : center;
        Quaternion offerRotation = shopRoot.rotation;
        if (shopkeeper != null)
        {
            Vector3 sharedFacing = Vector3.ProjectOnPlane(shopkeeper.position - offerCenter, Vector3.up);
            if (sharedFacing.sqrMagnitude > .001f)
                offerRotation = Quaternion.LookRotation(sharedFacing.normalized, Vector3.up);
        }
        for (int i = 0; i < 3; i++)
        {
            // Keep the guns parallel and evenly spaced. Individually looking
            // at the Shop Keeper made them fan out across the tabletop.
            Vector3 position = offerCenter + right.normalized * (((i - 1) * gunOfferSpacing) + gunOfferGroupLeftOffset);
            offers.Add(CreateOffer(i, position, offerRotation));
        }
        GameObject skipButton = ActivateSkipShopButton(shopRoot, offerCenter, offerRotation);
        if (skipButton != null)
            offers.Add(skipButton);
        cursorPosition = new Vector2(Screen.width * .5f, Screen.height * .5f);
        cursorVisual = FindCursor();
        if (cursorVisual != null) cursorVisual.gameObject.SetActive(true);
        crosshair = FindCrosshair();
        if (crosshair != null) crosshair.SetActive(false);
        StartArsenalCameraTransition(shopRoot);
    }
    private void Update()
    {
        if (offers.Count == 0 || playerCamera == null)
            return;
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;
        cursorPosition += mouse.delta.ReadValue();
        cursorPosition.x = Mathf.Clamp(cursorPosition.x, 0, Screen.width);
        cursorPosition.y = Mathf.Clamp(cursorPosition.y, 0, Screen.height);
        MoveCursor();
        Ray ray = playerCamera.ScreenPointToRay(cursorPosition);
        hoveredOffer = null;
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f, ~0, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        foreach (RaycastHit hit in hits)
        {
            ShopGunOffer candidate = hit.collider.GetComponentInParent<ShopGunOffer>();
            if (candidate == null)
                continue;
            hoveredOffer = candidate.gameObject;
            break;
        }
        foreach (GameObject offer in offers)
        {
            if (offer == null) continue;
            bool hovered = offer == hoveredOffer;
            // This is an authored, world-space canvas with its own tiny
            // hierarchy scale. Gun hover scaling must never reset it to one.
            if (offer == skipShopButton)
            {
                SetSkipButtonHovered(hovered);
                continue;
            }
            offer.transform.localScale = hovered ? Vector3.one * 1.1f : Vector3.one;
            offer.GetComponent<ShopGunOffer>()?.SetHovered(hovered);
        }
        if (mouse.leftButton.wasPressedThisFrame && hoveredOffer != null &&
            hoveredOffer.GetComponent<ShopGunOffer>()?.closeShop == true)
        {
            SkipShop();
            return;
        }
        foreach (GameObject offerObject in offers)
        {
            if (offerObject == null)
                continue;
            ShopGunOffer offer = offerObject.GetComponent<ShopGunOffer>();
            if (offer == null || offer.closeShop)
                continue;
            bool isHoldingThisOffer = hoveredOffer == offerObject && mouse.leftButton.isPressed;
            if (offer.UpdatePurchaseHold(isHoldingThisOffer, purchaseHoldDuration, purchaseReleaseDrainDuration))
            {
                CompletePurchase(offer.weapon, offer.soulPrice);
                return;
            }
        }
    }
private GameObject CreateOffer(int index, Vector3 position, Quaternion rotation)
    {
        GameObject root = new GameObject("Gun Offer " + (index + 1));
        root.transform.SetPositionAndRotation(position, rotation);
        BoxCollider interactionBox = root.AddComponent<BoxCollider>();
        interactionBox.center = new Vector3(0f, .22f, 0f);
        interactionBox.size = new Vector3(.55f, .5f, .8f);
        ShopGunOffer offerComponent = root.AddComponent<ShopGunOffer>();
        offerComponent.weapon = offeredWeapons[index];
        GameObject gun = CreateDisplayModel(index);
        gun.transform.SetParent(root.transform, false);
        gun.transform.localPosition = Vector3.zero;
        AlignDisplayModelForTable(gun);
        NormalizeDisplayModelToOffer(gun, root.transform);
        GunOfferDefinition definition = offeredDefinitions[index];
        offerComponent.soulPrice = definition != null ? definition.soulPrice : 0;
        string gunName = definition == null || string.IsNullOrWhiteSpace(definition.displayName)
            ? (offeredWeapons[index] == null ? "PROTOTYPE WEAPON" : offeredWeapons[index].gameObject.name.ToUpperInvariant())
            : definition.displayName.ToUpperInvariant();
        Canvas canvas = CreateGunInformationCard(root.transform, gunName, GetStats(offeredWeapons[index]), equippedWeaponStats, definition != null ? definition.previewSprite : null, offerComponent.soulPrice, index);
        canvas.transform.localPosition = gunInfoCardLocalPosition;
        canvas.transform.localRotation = Quaternion.identity;
        offerComponent.SetInfo(canvas.GetComponentInChildren<Text>(true), offerComponent.soulPrice);
        return root;
    }
    private void NormalizeDisplayModelToOffer(GameObject gun, Transform offerRoot)
    {
        Renderer[] renderers = gun.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;
        Physics.SyncTransforms();
        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            combinedBounds.Encapsulate(renderers[i].bounds);
        // Imported weapon prefabs have different pivot locations. Centre the
        // visible mesh on its offer root, then seat its lowest point on the
        // table surface so every offer uses the same actual mesh spacing.
        Vector3 centreInOfferSpace = offerRoot.InverseTransformPoint(combinedBounds.center);
        gun.transform.localPosition -= new Vector3(centreInOfferSpace.x, 0f, centreInOfferSpace.z);
        Physics.SyncTransforms();
        combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            combinedBounds.Encapsulate(renderers[i].bounds);
        gun.transform.position += Vector3.up * (offerRoot.position.y - combinedBounds.min.y + gunTableSurfaceOffset);
    }
    private static void AlignDisplayModelForTable(GameObject gun)
    {
        Renderer[] renderers = gun.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;
        Quaternion[] orientations =
        {
            Quaternion.identity,
            Quaternion.Euler(0f, 0f, 90f),
            Quaternion.Euler(90f, 0f, 0f),
            Quaternion.Euler(0f, 90f, 0f),
            Quaternion.Euler(90f, 90f, 0f),
            Quaternion.Euler(0f, 90f, 90f)
        };
        float bestHeight = float.PositiveInfinity;
        Quaternion bestOrientation = Quaternion.identity;
        foreach (Quaternion orientation in orientations)
        {
            gun.transform.localRotation = orientation;
            Physics.SyncTransforms();
            Bounds bounds = GetRendererBounds(renderers);
            if (bounds.size.y >= bestHeight)
                continue;
            bestHeight = bounds.size.y;
            bestOrientation = orientation;
        }
        gun.transform.localRotation = bestOrientation;
    }
    private static Bounds GetRendererBounds(Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }
    private Canvas CreateGunInformationCard(Transform parent, string gunName, GunStats stats, GunStats equippedStats, Sprite previewSprite, int soulPrice, int index)
    {
        if (gunInformationCardTemplate != null)
            return CreateGunInformationCardFromTemplate(parent, gunName, stats, equippedStats, previewSprite, soulPrice, index);
        GameObject card = new GameObject("Gun Information", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        card.transform.SetParent(parent, false);
        Canvas canvas = card.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = playerCamera;
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(1150f, 650f);
        card.transform.localScale = gunInfoCardLocalScale;
        Image background = card.AddComponent<Image>();
        // The material owns the colour; a white Image tint keeps its animated
        // polygon pattern visible on every generated gun panel.
        background.color = Color.white;
        if (gunInformationBackgroundMaterial != null)
        {
            background.material = new Material(gunInformationBackgroundMaterial);
            if (background.material.HasProperty("_Color"))
                background.material.SetColor("_Color", gunInformationPolygonColor);
        }
        CreateImage(card.transform, "Glow Frame", Vector2.zero, new Vector2(1125f, 625f), new Color(.03f, .7f, .85f, .22f));
        Image media = CreateImage(card.transform, "Media Placeholder", new Vector2(-365f, 0f), new Vector2(330f, 470f), new Color(.04f, .18f, .25f, 1f));
        Text mediaLabel = CreateText(media.transform, "Media Label", "LIVE PREVIEW\n\nGUN IMAGE / VIDEO\nPLACEHOLDER", Vector2.zero, new Vector2(300f, 430f), 32, TextAnchor.MiddleCenter, new Color(.2f, .95f, 1f));
        SetPreviewSprite(media, mediaLabel, previewSprite);
        CreateText(card.transform, "Weapon Label", "ARSENAL OFFER // 0" + (index + 1), new Vector2(125f, 235f), new Vector2(610f, 55f), 28, TextAnchor.MiddleLeft, new Color(.2f, .95f, 1f));
        CreateText(card.transform, "Weapon Name", gunName, new Vector2(125f, 165f), new Vector2(610f, 85f), 50, TextAnchor.MiddleLeft, Color.white);
        CreateText(card.transform, "Weapon Description", "Experimental weapon platform. Precise, reliable, and ready for field evaluation.", new Vector2(125f, 95f), new Vector2(610f, 75f), 28, TextAnchor.UpperLeft, new Color(.78f, .9f, .94f));
        AddStatBar(card.transform, "DAMAGE", stats.damage, stats.damageNormalized, equippedStats.damageNormalized, equippedStats.isValid, false, 25f);
        AddStatBar(card.transform, "FIRE RATE", stats.fireRate, stats.fireRateNormalized, equippedStats.fireRateNormalized, equippedStats.isValid, false, -35f);
        AddStatBar(card.transform, "CAPACITY", stats.capacity, stats.capacityNormalized, equippedStats.capacityNormalized, equippedStats.isValid, false, -95f);
        AddStatBar(card.transform, "RECOIL", stats.recoil, stats.recoilNormalized, equippedStats.recoilNormalized, equippedStats.isValid, true, -155f);
        CreateText(card.transform, "Soul Price", "PRICE  " + soulPrice.ToString("N0") + " SOULS", new Vector2(125f, -195f), new Vector2(610f, 42f), 24, TextAnchor.MiddleLeft, new Color(.25f, .95f, 1f));
        CreateText(card.transform, "Purchase Hint", "HOLD LEFT CLICK TO PURCHASE", new Vector2(125f, -235f), new Vector2(610f, 42f), 24, TextAnchor.MiddleLeft, new Color(1f, .76f, .25f));
        card.AddComponent<PolygonPanelFloat>().CacheContents();
        return canvas;
    }
    private Canvas CreateGunInformationCardFromTemplate(Transform parent, string gunName, GunStats stats, GunStats equippedStats, Sprite previewSprite, int soulPrice, int index)
    {
        Canvas canvas = Instantiate(gunInformationCardTemplate, parent, false);
        GameObject card = canvas.gameObject;
        card.name = "Gun Information";
        card.SetActive(true);
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = playerCamera;
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(1150f, 650f);
        card.transform.localScale = gunInfoCardLocalScale;
        GunOfferPurchaseHold clonedPurchaseHold = card.GetComponent<GunOfferPurchaseHold>();
        if (clonedPurchaseHold != null)
            clonedPurchaseHold.ConfigureInsufficientSolsFont(insufficientSolsFont);
        SetTemplatePreviewSprite(card.transform, previewSprite);
        SetTemplateText(card.transform, "Weapon Label", "ARSENAL OFFER // 0" + (index + 1));
        SetTemplateText(card.transform, "Weapon Name", gunName);
        SetTemplateText(card.transform, "Weapon Description", "Experimental weapon platform. Precise, reliable, and ready for field evaluation.");
        SetTemplateText(card.transform, "Soul Price", "PRICE  " + soulPrice.ToString("N0") + " SOULS");
        SetTemplateText(card.transform, "Purchase Hint", "HOLD LEFT CLICK TO PURCHASE");
        SetTemplateStat(card.transform, "DAMAGE", stats.damage, stats.damageNormalized, equippedStats.damageNormalized, equippedStats.isValid, false);
        SetTemplateStat(card.transform, "FIRE RATE", stats.fireRate, stats.fireRateNormalized, equippedStats.fireRateNormalized, equippedStats.isValid, false);
        SetTemplateStat(card.transform, "CAPACITY", stats.capacity, stats.capacityNormalized, equippedStats.capacityNormalized, equippedStats.isValid, false);
        SetTemplateStat(card.transform, "RECOIL", stats.recoil, stats.recoilNormalized, equippedStats.recoilNormalized, equippedStats.isValid, true);
        PolygonPanelFloat floatingPanel = card.GetComponent<PolygonPanelFloat>();
        if (floatingPanel == null)
            floatingPanel = card.AddComponent<PolygonPanelFloat>();
        floatingPanel.CacheContents();
        return canvas;
    }
    private static void SetTemplateText(Transform root, string childName, string value)
    {
        Text text = FindDescendant(root, childName)?.GetComponent<Text>();
        if (text != null)
            text.text = value;
    }
    private static void SetTemplatePreviewSprite(Transform root, Sprite previewSprite)
    {
        Image preview = FindDescendant(root, "Media Placeholder")?.GetComponent<Image>();
        Text placeholderLabel = FindDescendant(root, "Media Label")?.GetComponent<Text>();
        SetPreviewSprite(preview, placeholderLabel, previewSprite);
    }
    private static void SetPreviewSprite(Image preview, Text placeholderLabel, Sprite previewSprite)
    {
        if (preview == null)
            return;
        preview.sprite = previewSprite;
        preview.preserveAspect = previewSprite != null;
        // UI Image colours multiply the assigned sprite. The authored
        // placeholder is intentionally dark, so restore a white tint for a
        // real gun image to preserve its original colours.
        preview.color = previewSprite != null ? Color.white : new Color(.04f, .18f, .25f, 1f);
        if (placeholderLabel != null)
            placeholderLabel.gameObject.SetActive(previewSprite == null);
    }
    private static Transform FindDescendant(Transform root, string objectName)
    {
        if (root == null)
            return null;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == objectName)
                return child;
        return null;
    }
    private static void SetTemplateStat(Transform root, string label, string value, float normalizedValue, float equippedNormalizedValue, bool hasEquippedWeapon, bool lowerIsBetter)
    {
        SetTemplateText(root, label + " Label", label + "  " + value);
        Transform track = root.Find(label + " Track");
        Image fill = track?.Find(label + " Fill")?.GetComponent<Image>();
        if (fill == null)
            return;
        RectTransform fillRect = fill.rectTransform;
        fillRect.sizeDelta = new Vector2(Mathf.Clamp01(normalizedValue) * 286f, 12f);
        SetComparisonSegment(track, label, normalizedValue, equippedNormalizedValue, hasEquippedWeapon, lowerIsBetter);
    }
    private void EnsureGunInformationCardTemplate()
    {
        if (gunInformationCardTemplate != null)
        {
            EnsureSoulPriceTemplateText(gunInformationCardTemplate.transform);
            EnsurePurchaseTemplateControls();
            return;
        }
        Transform existing = transform.Find("Gun Information Card Template");
        if (existing != null)
        {
            gunInformationCardTemplate = existing.GetComponent<Canvas>();
            if (gunInformationCardTemplate != null)
            {
                EnsureSoulPriceTemplateText(gunInformationCardTemplate.transform);
                EnsurePurchaseTemplateControls();
                return;
            }
        }
        GameObject card = new GameObject("Gun Information Card Template", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        card.transform.SetParent(transform, false);
        Canvas canvas = card.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(1150f, 650f);
        card.transform.localScale = gunInfoCardLocalScale;
        Image background = card.AddComponent<Image>();
        background.color = Color.white;
        background.material = gunInformationBackgroundMaterial;
        CreateImage(card.transform, "Glow Frame", Vector2.zero, new Vector2(1125f, 625f), new Color(.03f, .7f, .85f, .22f));
        Image media = CreateImage(card.transform, "Media Placeholder", new Vector2(-365f, 0f), new Vector2(330f, 470f), new Color(.04f, .18f, .25f, 1f));
        Text mediaLabel = CreateText(media.transform, "Media Label", "LIVE PREVIEW\n\nGUN IMAGE / VIDEO\nPLACEHOLDER", Vector2.zero, new Vector2(300f, 430f), 32, TextAnchor.MiddleCenter, new Color(.2f, .95f, 1f));
        CreateText(card.transform, "Weapon Label", "ARSENAL OFFER // 01", new Vector2(125f, 235f), new Vector2(610f, 55f), 28, TextAnchor.MiddleLeft, new Color(.2f, .95f, 1f));
        CreateText(card.transform, "Weapon Name", "WEAPON NAME", new Vector2(125f, 165f), new Vector2(610f, 85f), 50, TextAnchor.MiddleLeft, Color.white);
        CreateText(card.transform, "Weapon Description", "Experimental weapon platform. Precise, reliable, and ready for field evaluation.", new Vector2(125f, 95f), new Vector2(610f, 75f), 28, TextAnchor.UpperLeft, new Color(.78f, .9f, .94f));
        AddStatBar(card.transform, "DAMAGE", "0", 0f, 0f, false, false, 25f);
        AddStatBar(card.transform, "FIRE RATE", "0", 0f, 0f, false, false, -35f);
        AddStatBar(card.transform, "CAPACITY", "0", 0f, 0f, false, false, -95f);
        AddStatBar(card.transform, "RECOIL", "0", 0f, 0f, false, true, -155f);
        CreateText(card.transform, "Soul Price", "PRICE  0 SOULS", new Vector2(125f, -195f), new Vector2(610f, 42f), 24, TextAnchor.MiddleLeft, new Color(.25f, .95f, 1f));
        CreateText(card.transform, "Purchase Hint", "HOLD LEFT CLICK TO PURCHASE", new Vector2(125f, -235f), new Vector2(610f, 42f), 24, TextAnchor.MiddleLeft, new Color(1f, .76f, .25f));
        card.AddComponent<PolygonPanelFloat>().CacheContents();
        gunInformationCardTemplate = canvas;
        EnsurePurchaseTemplateControls();
        card.SetActive(false);
    }
    private void EnsurePurchaseTemplateControls()
    {
        if (gunInformationCardTemplate == null)
            return;
        GunOfferPurchaseHold purchaseHold = gunInformationCardTemplate.GetComponent<GunOfferPurchaseHold>();
        if (purchaseHold == null)
            purchaseHold = gunInformationCardTemplate.gameObject.AddComponent<GunOfferPurchaseHold>();
        purchaseHold.ConfigureInsufficientSolsFont(insufficientSolsFont);
        purchaseHold.Initialize();
    }
    private static void EnsureSoulPriceTemplateText(Transform cardRoot)
    {
        if (FindDescendant(cardRoot, "Soul Price") != null)
            return;

        CreateText(cardRoot, "Soul Price", "PRICE  0 SOULS", new Vector2(125f, -195f), new Vector2(610f, 42f), 24, TextAnchor.MiddleLeft, new Color(.25f, .95f, 1f));
    }
    private static Image CreateImage(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject item = new GameObject(name, typeof(Image));
        item.transform.SetParent(parent, false);
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = item.GetComponent<Image>();
        image.color = color;
        return image;
    }
    private static Text CreateText(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment, Color color)
    {
        GameObject item = new GameObject(name, typeof(Text));
        item.transform.SetParent(parent, false);
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Text text = item.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
    private static void AddStatBar(Transform parent, string label, string value, float normalizedValue, float equippedNormalizedValue, bool hasEquippedWeapon, bool lowerIsBetter, float y)
    {
        CreateText(parent, label + " Label", label + "  " + value, new Vector2(-10f, y), new Vector2(340f, 38f), 26, TextAnchor.MiddleLeft, Color.white);
        Image track = CreateImage(parent, label + " Track", new Vector2(270f, y), new Vector2(300f, 20f), new Color(.1f, .16f, .2f, 1f));
        Image fill = CreateImage(track.transform, label + " Fill", Vector2.zero, new Vector2(Mathf.Clamp01(normalizedValue) * 286f, 12f), new Color(.15f, .9f, 1f, 1f));
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, .5f);
        fillRect.anchorMax = new Vector2(0f, .5f);
        fillRect.pivot = new Vector2(0f, .5f);
        fillRect.anchoredPosition = new Vector2(7f, 0f);
        SetComparisonSegment(track.transform, label, normalizedValue, equippedNormalizedValue, hasEquippedWeapon, lowerIsBetter);
    }
    private static void SetComparisonSegment(Transform track, string label, float offeredNormalizedValue, float equippedNormalizedValue, bool hasEquippedWeapon, bool lowerIsBetter)
    {
        Transform segmentTransform = track.Find(label + " Comparison");
        Image segment = segmentTransform != null ? segmentTransform.GetComponent<Image>() : null;
        if (segment == null)
            segment = CreateImage(track, label + " Comparison", Vector2.zero, Vector2.zero, Color.clear);

        float offered = Mathf.Clamp01(offeredNormalizedValue);
        float equipped = Mathf.Clamp01(equippedNormalizedValue);
        float difference = Mathf.Abs(offered - equipped);
        bool showDifference = hasEquippedWeapon && difference > .0001f;
        segment.gameObject.SetActive(showDifference);
        if (!showDifference)
            return;

        bool improvement = lowerIsBetter ? offered < equipped : offered > equipped;
        segment.color = improvement ? new Color(.2f, 1f, .35f, 1f) : new Color(1f, .22f, .25f, 1f);

        RectTransform segmentRect = segment.rectTransform;
        segmentRect.anchorMin = new Vector2(0f, .5f);
        segmentRect.anchorMax = new Vector2(0f, .5f);
        segmentRect.pivot = new Vector2(0f, .5f);
        segmentRect.sizeDelta = new Vector2(difference * 286f, 12f);
        segmentRect.anchoredPosition = new Vector2(7f + Mathf.Min(offered, equipped) * 286f, 0f);
    }
    private void CompletePurchase(WeaponBehaviour purchasedWeapon, int soulPrice)
    {
        if (completingPurchase)
            return;
        if (!SoulCurrency.EnsureInstance().TrySpend(soulPrice))
            return;
        MovePurchasedWeaponToUnlocked(purchasedWeapon);
        completingPurchase = true;
        StartCoroutine(ReturnToPlayerThenCloseShop());
    }
    private void MovePurchasedWeaponToUnlocked(WeaponBehaviour purchasedWeapon)
    {
        if (purchasedWeapon == null)
            return;
        GameObject player = GameObject.FindWithTag("Player");
        Inventory inventory = player != null ? player.GetComponentInChildren<Inventory>(true) : null;
        Transform unlockedWeapons = player != null ? FindChild(player.transform, "Unlocked Weapons") : null;
        Transform lockedWeapons = player != null ? FindChild(player.transform, "Locked Weapons") : null;
        if (inventory == null || unlockedWeapons == null || lockedWeapons == null)
        {
            Debug.LogWarning("[ShopGunOffer] Could not swap the purchased weapon because the player's inventory or weapon parents are missing.", this);
            return;
        }
        WeaponBehaviour previouslyEquipped = inventory.GetEquipped();
        if (previouslyEquipped != null && previouslyEquipped != purchasedWeapon)
        {
            previouslyEquipped.transform.SetParent(lockedWeapons, false);
            previouslyEquipped.gameObject.SetActive(false);
        }
        purchasedWeapon.transform.SetParent(unlockedWeapons, false);
        // Inventory.Init equips element zero, so make the bought weapon first.
        purchasedWeapon.transform.SetSiblingIndex(0);
        inventory.Init();

        // Inventory is below the player in this hierarchy, while Character is
        // on the player root. Resolve from the player first so this never
        // silently misses the animator.
        pendingWeaponRevealCharacter = player.GetComponent<Character>() ??
                                         player.GetComponentInChildren<Character>(true) ??
                                         inventory.GetComponentInParent<Character>();
        if (pendingWeaponRevealCharacter != null && pendingWeaponRevealCharacter.characterAnimator != null)
            pendingWeaponRevealCharacter.characterAnimator.SetBool("FinisherUnholstered", true);
    }
    private IEnumerator ReturnToPlayerThenCloseShop()
    {
        float returnDuration = arsenalCamera != null && gameplayCamera != null
            ? arsenalCameraReturnDuration
            : 0f;

        // Undo the forward shop move while the arsenal camera returns to the
        // original player view. Both arrive together.
        if (hasReturnSnapshot && playerTransform != null)
        {
            playerTransform.DOMove(playerPositionBeforeOffers, returnDuration)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
        }

        if (arsenalCamera != null && gameplayCamera != null)
        {
            Vector3 targetPosition = hasReturnSnapshot ? gameplayCameraPositionBeforeOffers : gameplayCamera.transform.position;
            Quaternion targetRotation = hasReturnSnapshot ? gameplayCameraRotationBeforeOffers : gameplayCamera.transform.rotation;
            float targetFieldOfView = hasReturnSnapshot ? gameplayCameraFieldOfViewBeforeOffers : gameplayCamera.fieldOfView;

            arsenalCamera.transform.DOMove(targetPosition, arsenalCameraReturnDuration)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
            arsenalCamera.transform.DORotateQuaternion(targetRotation, arsenalCameraReturnDuration)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
            arsenalCamera.DOFieldOfView(targetFieldOfView, arsenalCameraReturnDuration)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
            yield return new WaitForSecondsRealtime(arsenalCameraReturnDuration);

            arsenalCamera.transform.SetPositionAndRotation(targetPosition, targetRotation);
            arsenalCamera.fieldOfView = targetFieldOfView;
        }

        if (hasReturnSnapshot && playerTransform != null)
            playerTransform.position = playerPositionBeforeOffers;
        hasReturnSnapshot = false;

        ClearOffers();
        FindFirstObjectByType<LevelComplete>()?.SnapPlayerToShopExitView();

        if (weaponRevealDelayAfterReturn > 0f)
            yield return new WaitForSecondsRealtime(weaponRevealDelayAfterReturn);

        // The purchase sets this true while the shop view is active. Setting it
        // false here creates the animator transition that draws the new gun.
        if (pendingWeaponRevealCharacter != null && pendingWeaponRevealCharacter.characterAnimator != null)
            pendingWeaponRevealCharacter.characterAnimator.SetBool("FinisherUnholstered", false);
        pendingWeaponRevealCharacter = null;

        completingPurchase = false;
        completed?.Invoke();
    }
    public void CloseShop()
    {
        SkipShop();
    }
    public void SkipShop()
    {
        if (completingPurchase)
            return;
        // LevelComplete calls CloseShop once more after the completed callback.
        // At that point the first return has already cleared this presentation.
        if (offers.Count == 0 && arsenalCamera == null)
            return;

        completingPurchase = true;
        StartCoroutine(ReturnToPlayerThenCloseShop());
    }
    private void ClearOffers()
    {
        foreach (GameObject offer in offers)
        {
            if (offer == null)
                continue;
            if (offer == skipShopButton)
            {
                offer.transform.DOKill();
                if (hasSkipButtonRestScale)
                    offer.transform.localScale = skipButtonRestScale;
                skipButtonHovered = false;
                offer.SetActive(false);
                continue;
            }
            Destroy(offer);
        }
        offers.Clear(); offeredWeapons.Clear(); offeredDefinitions.Clear();
        if (cursorVisual != null) cursorVisual.gameObject.SetActive(false);
        if (crosshair != null) crosshair.SetActive(true);
        CleanupArsenalCamera();
        playerCamera = gameplayCamera;
    }
    private void StartArsenalCameraTransition(Transform shopRoot)
    {
        if (playerCamera == null || offers.Count < 3)
            return;
        CleanupArsenalCamera();
        // The camera begins exactly at the player's current view. Its image is
        // displayed through a RenderTexture, then it moves independently to
        // the table so the player object never needs to move for this shot.
        arsenalCameraRoot = new GameObject("Shop Arsenal Camera");
        arsenalCamera = arsenalCameraRoot.AddComponent<Camera>();
        arsenalCamera.CopyFrom(playerCamera);
        arsenalCamera.enabled = true;
        arsenalCamera.tag = "Untagged";
        arsenalCamera.transform.SetPositionAndRotation(playerCamera.transform.position, playerCamera.transform.rotation);
        // Preserve the gameplay FOV for the first rendered frame. The FOV then
        // changes during the same tween as position and rotation.
        arsenalRenderTexture = new RenderTexture(
            Mathf.Max(2, Screen.width), Mathf.Max(2, Screen.height), 24, RenderTextureFormat.ARGB32)
        {
            name = "Shop Arsenal Camera Render Texture"
        };
        arsenalRenderTexture.Create();
        arsenalCamera.targetTexture = arsenalRenderTexture;
        CreateArsenalCameraCanvas();
        Vector3 gunCenter = Vector3.zero;
        for (int i = 0; i < 3; i++)
            gunCenter += offers[i].transform.position;
        gunCenter /= 3f;
        // Place the top-down camera on the player side of the table. The
        // earlier direction put it behind the Shop Keeper, reversing the
        // presentation from the player's perspective.
        Vector3 backward = shopRoot != null
            ? Vector3.ProjectOnPlane(shopRoot.forward, Vector3.up).normalized
            : Vector3.back;
        if (backward.sqrMagnitude < .001f)
            backward = Vector3.back;
        Vector3 lookAt = gunCenter + backward * arsenalCameraLookAtPlayerOffset;
        Vector3 destination = gunCenter + Vector3.up * arsenalCameraHeight + backward * arsenalCameraBackOffset;
        Vector3 horizontalFacing = Vector3.ProjectOnPlane(lookAt - destination, Vector3.up);
        if (horizontalFacing.sqrMagnitude < .001f)
            horizontalFacing = shopRoot != null ? shopRoot.forward : Vector3.forward;
        Quaternion destinationRotation = Quaternion.LookRotation(horizontalFacing.normalized, Vector3.up) *
                                         Quaternion.Euler(arsenalCameraPitch, 0f, 0f);
        // The offer cards are world-space nameplates.  Their orientation is
        // chosen once for the final Arsenal shot, then stays fixed while the
        // player hovers and selects a weapon.
        for (int i = 0; i < 3; i++)
        {
            // Keep each world-space panel above its gun, then only move it
            // inward when its edges would leave the completed Arsenal shot.
            offers[i].GetComponent<ShopGunOffer>()?.PositionCardForArsenal(
                arsenalCamera, destination, destinationRotation, gunInfoCardCameraClearance, gunInfoCardViewportPadding);
        }
        arsenalCamera.transform.DOMove(destination, arsenalCameraTransitionDuration)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
        arsenalCamera.transform.DORotateQuaternion(destinationRotation, arsenalCameraTransitionDuration)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
        arsenalCamera.DOFieldOfView(arsenalCameraFieldOfView, arsenalCameraTransitionDuration)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
        // Raycasts and the information cards now follow the camera that the
        // player sees on the RenderTexture.
        playerCamera = arsenalCamera;
    }
    private void CreateArsenalCameraCanvas()
    {
        GameObject canvasObject = new GameObject("Shop Arsenal Camera Render", typeof(Canvas), typeof(CanvasScaler));
        arsenalCameraCanvas = canvasObject.GetComponent<Canvas>();
        arsenalCameraCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        arsenalCameraCanvas.sortingOrder = arsenalCameraCanvasOrder;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        GameObject imageObject = new GameObject("Arsenal RenderTexture", typeof(RawImage));
        imageObject.transform.SetParent(canvasObject.transform, false);
        RawImage image = imageObject.GetComponent<RawImage>();
        image.texture = arsenalRenderTexture;
        image.raycastTarget = false;
        ApplyRetroLook(image);
        RectTransform imageRect = image.rectTransform;
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;
        CreateShopSoulCurrencyDisplay(canvasObject.transform);
        // The gameplay cursor lives behind this full-screen image, so the
        // arsenal view owns an equivalent cursor above its RenderTexture.
        if (cursorVisual != null)
            cursorVisual.gameObject.SetActive(false);
        GameObject cursorObject = new GameObject("Shop Arsenal Cursor", typeof(Text));
        cursorObject.transform.SetParent(canvasObject.transform, false);
        Text cursorText = cursorObject.GetComponent<Text>();
        cursorText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cursorText.text = "\u2295";
        cursorText.fontSize = 44;
        cursorText.alignment = TextAnchor.MiddleCenter;
        cursorText.color = new Color(.25f, 1f, 1f, .95f);
        cursorText.raycastTarget = false;
        RectTransform cursorRect = cursorText.rectTransform;
        cursorRect.anchorMin = new Vector2(.5f, .5f);
        cursorRect.anchorMax = new Vector2(.5f, .5f);
        cursorRect.sizeDelta = new Vector2(56f, 56f);
        cursorRect.anchoredPosition = Vector2.zero;
        cursorVisual = cursorRect;
    }
    private void CreateShopSoulCurrencyDisplay(Transform parent)
    {
        GameObject walletObject = new GameObject("Shop Soul Currency", typeof(Text));
        walletObject.transform.SetParent(parent, false);
        shopSoulCurrencyText = walletObject.GetComponent<Text>();
        shopSoulCurrencyText.font = shopSoulCurrencyFont != null
            ? shopSoulCurrencyFont
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        shopSoulCurrencyText.fontSize = 34;
        shopSoulCurrencyText.fontStyle = FontStyle.Bold;
        shopSoulCurrencyText.alignment = TextAnchor.UpperLeft;
        shopSoulCurrencyText.color = new Color(.25f, 1f, 1f, 1f);
        shopSoulCurrencyText.raycastTarget = false;
        RectTransform walletRect = shopSoulCurrencyText.rectTransform;
        walletRect.anchorMin = new Vector2(0f, 1f);
        walletRect.anchorMax = new Vector2(0f, 1f);
        walletRect.pivot = new Vector2(0f, 1f);
        walletRect.anchoredPosition = new Vector2(38f, -32f);
        walletRect.sizeDelta = new Vector2(500f, 64f);

        SoulCurrency wallet = SoulCurrency.EnsureInstance();
        wallet.BalanceChanged += UpdateShopSoulCurrencyDisplay;
        UpdateShopSoulCurrencyDisplay(wallet.Balance);
    }
    private void UpdateShopSoulCurrencyDisplay(int souls)
    {
        if (shopSoulCurrencyText != null)
            shopSoulCurrencyText.text = "SOULS  " + souls.ToString("N0");
    }
    private void ApplyRetroLook(RawImage targetImage)
    {
        foreach (RetroLookResolutionManager manager in Resources.FindObjectsOfTypeAll<RetroLookResolutionManager>())
        {
            if (!manager.gameObject.scene.IsValid() || manager.retroLookMaterial == null)
                continue;
            // Each temporary arsenal view owns its material instance. Updating
            // its input texture therefore cannot overwrite the main camera or
            // finisher camera's retro render texture.
            arsenalRetroLookMaterial = new Material(manager.retroLookMaterial)
            {
                name = "Shop Arsenal Retro Look Material"
            };
            string property = string.IsNullOrEmpty(manager.renderTexturePropertyName)
                ? "_MainTex"
                : manager.renderTexturePropertyName;
            if (arsenalRetroLookMaterial.HasProperty(property))
                arsenalRetroLookMaterial.SetTexture(property, arsenalRenderTexture);
            targetImage.material = arsenalRetroLookMaterial;
            return;
        }
        Debug.LogWarning("[ShopGunOffer] No RetroLookResolutionManager material was found for the Arsenal camera.");
    }
    private void CleanupArsenalCamera()
    {
        if (SoulCurrency.Instance != null)
            SoulCurrency.Instance.BalanceChanged -= UpdateShopSoulCurrencyDisplay;
        shopSoulCurrencyText = null;
        if (arsenalCameraRoot != null)
            Destroy(arsenalCameraRoot);
        arsenalCameraRoot = null;
        arsenalCamera = null;
        if (arsenalCameraCanvas != null)
        {
            // Destroy is deferred until frame end. Disable first so the shop
            // overlay, including the wallet, cannot remain visible after close.
            arsenalCameraCanvas.gameObject.SetActive(false);
            Destroy(arsenalCameraCanvas.gameObject);
        }
        arsenalCameraCanvas = null;
        if (arsenalRenderTexture != null)
        {
            arsenalRenderTexture.Release();
            Destroy(arsenalRenderTexture);
        }
        arsenalRenderTexture = null;
        if (arsenalRetroLookMaterial != null)
            Destroy(arsenalRetroLookMaterial);
        arsenalRetroLookMaterial = null;
    }
    private Camera FindGameplayCamera()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Camera best = null;
            foreach (Camera camera in player.GetComponentsInChildren<Camera>(true))
                if (camera.isActiveAndEnabled && camera.cullingMask != 0 && (best == null || camera.depth > best.depth)) best = camera;
            if (best != null) return best;
        }
        return Camera.main;
    }
    private static Transform FindChild(Transform root, string part)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0) return child;
        return null;
    }
    private static GameObject FindCrosshair()
    {
        foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
            if (t.gameObject.scene.IsValid() && t.name == "Crosshair" && t.parent != null && t.parent.name == "P_LPSP_UI_Canvas") return t.gameObject;
        return null;
    }
    private static RectTransform FindCursor()
    {
        foreach (RectTransform t in Resources.FindObjectsOfTypeAll<RectTransform>())
            if (t.gameObject.scene.IsValid() && t.name == "Shop Hologram Cursor") return t;
        return null;
    }
    private void MoveCursor()
    {
        if (cursorVisual == null) return;
        RectTransform parent = cursorVisual.parent as RectTransform;
        Vector2 local; RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, cursorPosition, null, out local);
        cursorVisual.anchoredPosition = local;
    }
    private GameObject ActivateSkipShopButton(Transform shopRoot, Vector3 offerCenter, Quaternion offerRotation)
    {
        if (skipShopButton == null)
            skipShopButton = transform.Find("Skip Shop Button")?.gameObject;
        if (skipShopButton == null)
        {
            Debug.LogWarning("[ShopGunOffer] Assign the saved Skip Shop Button in the controller hierarchy.", this);
            return null;
        }

        // Artists place this anchor in the Shop Room prefab. It moves with
        // each spawned shop and controls the button position and rotation.
        Transform anchor = FindChild(shopRoot, skipButtonAnchorName);
        if (anchor != null)
        {
            skipShopButton.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
        }
        else
        {
            // Keep the original hardcoded placement as a safe fallback for
            // any older Shop Room prefab that does not yet have the anchor.
            Vector3 fallbackPosition = offerCenter + shopRoot.forward * .7f + Vector3.up * .04f;
            skipShopButton.transform.SetPositionAndRotation(fallbackPosition, offerRotation);
        }
        if (!hasSkipButtonRestScale)
        {
            skipButtonRestScale = skipShopButton.transform.localScale;
            hasSkipButtonRestScale = true;
        }
        ShopGunOffer offer = skipShopButton.GetComponent<ShopGunOffer>();
        if (offer == null)
        {
            Debug.LogWarning("[ShopGunOffer] The saved Skip Shop Button needs a ShopGunOffer component.", skipShopButton);
            return null;
        }

        offer.closeShop = true;
        Canvas canvas = skipShopButton.GetComponent<Canvas>();
        if (canvas != null)
            canvas.worldCamera = playerCamera;
        skipShopButton.SetActive(true);
        return skipShopButton;
    }
    private void SetSkipButtonHovered(bool hovered)
    {
        if (skipShopButton == null || !hasSkipButtonRestScale || skipButtonHovered == hovered)
            return;

        skipButtonHovered = hovered;
        Transform buttonTransform = skipShopButton.transform;
        buttonTransform.DOKill();
        buttonTransform.DOScale(hovered ? skipButtonRestScale * 1.18f : skipButtonRestScale, .16f)
            .SetEase(hovered ? Ease.OutBack : Ease.OutQuad)
            .SetUpdate(true);
    }
    private List<GunOfferDefinition> GetOfferCandidates(GameObject player, Inventory inventory, WeaponBehaviour equipped)
    {
        var candidates = new List<GunOfferDefinition>();
        HashSet<string> unlockedWeaponNames = GetUnlockedWeaponNames(player);
        // Explicit inspector entries take priority. This is the editable setup
        // for each real player weapon and its matching display mesh.
        foreach (GunOfferDefinition definition in gunOffers)
        {
            if (definition != null && definition.weaponPrefab != null &&
                definition.weaponPrefab != equipped && definition.probability > 0f &&
                !unlockedWeaponNames.Contains(NormalizeGunName(definition.weaponPrefab.gameObject.name)))
                candidates.Add(definition);
        }
        if (candidates.Count > 0)
            return candidates;
        // Existing scenes can use the system immediately: find every weapon
        // below Player, including disabled weapons, then pair each with the
        // matching mesh in the legacy list. The list above remains available
        // to set custom weights and meshes in the Inspector.
        if (player != null)
        {
            foreach (Weapon weapon in player.GetComponentsInChildren<Weapon>(true))
            {
                if (weapon == null || weapon == equipped ||
                    unlockedWeaponNames.Contains(NormalizeGunName(weapon.gameObject.name)))
                    continue;
                candidates.Add(new GunOfferDefinition
                {
                    weaponPrefab = weapon,
                    displayPrefab = FindDisplayPrefab(weapon.gameObject.name),
                    displayName = weapon.gameObject.name,
                    soulPrice = 1000,
                    probability = 1f
                });
            }
        }
        if (candidates.Count == 0 && inventory != null && inventory.weapons != null)
        {
            foreach (WeaponBehaviour weapon in inventory.weapons)
            {
                if (!(weapon is Weapon realWeapon) || realWeapon == equipped ||
                    unlockedWeaponNames.Contains(NormalizeGunName(realWeapon.gameObject.name)))
                    continue;
                candidates.Add(new GunOfferDefinition
                {
                    weaponPrefab = realWeapon,
                    displayPrefab = FindDisplayPrefab(realWeapon.gameObject.name),
                    displayName = realWeapon.gameObject.name,
                    soulPrice = 1000,
                    probability = 1f
                });
            }
        }
        return candidates;
    }
    private static HashSet<string> GetUnlockedWeaponNames(GameObject player)
    {
        var names = new HashSet<string>();
        if (player == null)
            return names;
        Transform unlockedWeapons = FindChild(player.transform, "Unlocked Weapons");
        if (unlockedWeapons == null)
            return names;
        // Include inactive weapons because the player may own a gun without
        // currently holding it. Names are used so this also works when a
        // purchased weapon is instantiated rather than moved from Locked Weapons.
        foreach (Weapon weapon in unlockedWeapons.GetComponentsInChildren<Weapon>(true))
            if (weapon != null)
                names.Add(NormalizeGunName(weapon.gameObject.name));
        return names;
    }
    private static GunOfferDefinition ChooseWeightedAndRemove(List<GunOfferDefinition> candidates)
    {
        float total = 0f;
        foreach (GunOfferDefinition candidate in candidates)
            total += Mathf.Max(0f, candidate.probability);
        if (total <= 0f)
            return null;
        float roll = UnityEngine.Random.value * total;
        for (int i = 0; i < candidates.Count; i++)
        {
            roll -= Mathf.Max(0f, candidates[i].probability);
            if (roll > 0f)
                continue;
            GunOfferDefinition selected = candidates[i];
            candidates.RemoveAt(i);
            return selected;
        }
        GunOfferDefinition fallback = candidates[candidates.Count - 1];
        candidates.RemoveAt(candidates.Count - 1);
        return fallback;
    }
    private GameObject FindDisplayPrefab(string weaponName)
    {
        if (gunDisplayPrefabs == null)
            return null;
        string key = NormalizeGunName(weaponName);
        foreach (GameObject display in gunDisplayPrefabs)
        {
            if (display != null && NormalizeGunName(display.name) == key)
                return display;
        }
        return null;
    }
    private static string NormalizeGunName(string value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace(" ", string.Empty).Replace("-", string.Empty).Replace("_", string.Empty).ToUpperInvariant();
    }
    private struct GunStats
    {
        public bool isValid;
        public string damage;
        public string fireRate;
        public string capacity;
        public string recoil;
        public float damageNormalized;
        public float fireRateNormalized;
        public float capacityNormalized;
        public float recoilNormalized;
    }
    private static GunStats GetStats(WeaponBehaviour weapon)
    {
        if (!(weapon is Weapon gun))
            return new GunStats { damage = "--", fireRate = "--", capacity = "--", recoil = "--" };
        float damage = gun.GetShopBaseDamage();
        int fireRate = gun.GetShopBaseFireRate();
        int capacity = gun.GetShopBaseCapacity();
        float recoil = gun.GetShopRecoil();
        // Recoil values are intentionally small for gameplay. The shop card
        // presents a readable rating only; it never writes this value back to
        // the weapon.
        float displayedRecoil = recoil * 10f;
        return new GunStats
        {
            isValid = true,
            damage = FormatStat(damage),
            fireRate = FormatStat(fireRate),
            capacity = FormatStat(capacity),
            recoil = FormatStat(displayedRecoil),
            damageNormalized = damage / 100f,
            fireRateNormalized = fireRate / 1000f,
            capacityNormalized = capacity / 100f,
            recoilNormalized = displayedRecoil / 50f
        };
    }
    private static string FormatStat(float value)
    {
        return Mathf.Approximately(value, Mathf.Round(value)) ? Mathf.RoundToInt(value).ToString() : value.ToString("0.0");
    }
    private GameObject CreateDisplayModel(int offerIndex)
    {
        GunOfferDefinition definition = offerIndex >= 0 && offerIndex < offeredDefinitions.Count
            ? offeredDefinitions[offerIndex]
            : null;
        GameObject prefab = definition != null ? definition.displayPrefab : null;
        if (prefab == null && definition != null && definition.weaponPrefab != null)
            prefab = FindDisplayPrefab(definition.weaponPrefab.gameObject.name);
        if (prefab != null)
            return Instantiate(prefab);
        GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
        placeholder.name = "Placeholder Gun Model";
        Destroy(placeholder.GetComponent<Collider>());
        return placeholder;
    }
}

public class ShopGunOffer : MonoBehaviour
{
        public bool closeShop;public WeaponBehaviour weapon;
        [HideInInspector] public int soulPrice;
        private bool isHovered;private Text info;
    private Vector3 hologramRestScale;
    private GunOfferPurchaseHold purchaseHold;private void Awake() { }
    public void FaceCameraAt(Vector3 cameraPosition)
    {
        if (info == null)
            return;
        Transform hologram = info.GetComponentInParent<Canvas>().transform;
        Vector3 facing = hologram.position - cameraPosition;
        if (facing.sqrMagnitude > .0001f)
            hologram.rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
    }
    public void PositionCardForArsenal(Camera camera, Vector3 cameraPosition, Quaternion cameraRotation,
        float cameraClearance, float viewportPadding)
    {
        if (info == null || camera == null)
            return;
        Canvas canvas = info.GetComponentInParent<Canvas>();
        canvas.worldCamera = camera;
        Transform hologram = canvas.transform;
        FaceCameraAt(cameraPosition);
        // Keep the panel in front of the weapon geometry from the Arsenal
        // camera's point of view. This prevents a tall gun mesh from cutting
        // through the lower portion of its own card.
        Vector3 towardCard = hologram.position - cameraPosition;
        if (towardCard.sqrMagnitude > .0001f)
            hologram.position -= towardCard.normalized * cameraClearance;
        // Reserve room for the card's hover scale so neither outside offer
        // can be clipped by the left or right edge of the final camera view.
        RectTransform card = (RectTransform)hologram;
        Vector3 cameraForward = cameraRotation * Vector3.forward;
        Vector3 cameraRight = cameraRotation * Vector3.right;
        Vector3 cameraUp = cameraRotation * Vector3.up;
        Vector3 fromCamera = hologram.position - cameraPosition;
        float depth = Vector3.Dot(fromCamera, cameraForward);
        if (depth <= .01f)
            return;
        float halfHeight = depth * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * .5f);
        float halfWidth = halfHeight * camera.aspect;
        float cardHalfWidth = Mathf.Abs(card.rect.width * hologram.lossyScale.x) * .56f;
        float cardHalfHeight = Mathf.Abs(card.rect.height * hologram.lossyScale.y) * .56f;
        float maxX = Mathf.Max(0f, halfWidth - cardHalfWidth - viewportPadding);
        float maxY = Mathf.Max(0f, halfHeight - cardHalfHeight - viewportPadding);
        float currentX = Vector3.Dot(fromCamera, cameraRight);
        float currentY = Vector3.Dot(fromCamera, cameraUp);
        hologram.position += cameraRight * (Mathf.Clamp(currentX, -maxX, maxX) - currentX) +
                             cameraUp * (Mathf.Clamp(currentY, -maxY, maxY) - currentY);
    }
    public void FaceCamera(Camera camera)
    {
        if (info == null || camera == null)
            return;
        Transform hologram = info.GetComponentInParent<Canvas>().transform;
        Vector3 facing = hologram.position - camera.transform.position;
        if (facing.sqrMagnitude > .0001f)
            hologram.rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
    }
    public bool UpdatePurchaseHold(bool holding, float holdDuration, float releaseDuration)
    {
        return purchaseHold != null && purchaseHold.Tick(holding, holdDuration, releaseDuration);
    }
public void SetHovered(bool hovered)
    {
        if (isHovered == hovered)
            return;
        isHovered = hovered;
        if (info == null || closeShop)
            return;
        Canvas canvas = info.GetComponentInParent<Canvas>();
        Transform hologram = canvas.transform;
        hologram.DOKill();
        if (hovered)
        {
            canvas.enabled = true;
            hologram.localScale = hologramRestScale * .72f;
            hologram.DOScale(hologramRestScale, .22f).SetEase(Ease.OutBack).SetUpdate(true);
        }
        else if (canvas.enabled)
        {
            hologram.DOScale(hologramRestScale * .72f, .16f).SetEase(Ease.InBack).SetUpdate(true)
                .OnComplete(() => canvas.enabled = false);
        }
    }
    public void SetInfo(Text value, int price)
    {
        info = value;
        if (info != null)
        {
            Canvas canvas = info.GetComponentInParent<Canvas>();
            hologramRestScale = canvas.transform.localScale;
            purchaseHold = canvas.GetComponent<GunOfferPurchaseHold>();
            if (purchaseHold == null)
                purchaseHold = canvas.gameObject.AddComponent<GunOfferPurchaseHold>();
            purchaseHold.Initialize();
            purchaseHold.ConfigurePrice(price);
            canvas.enabled = false;
        }
    }
}

/// <summary>
/// Handles the hold-to-purchase display on a cloned gun information card.
/// It builds its controls by name so the canvas remains editable in the scene.
/// </summary>
public sealed class GunOfferPurchaseHold : MonoBehaviour
{
    private Slider purchaseSlider;
    private RectTransform sliderRect;
    private Text hint;
    private TextMeshProUGUI insufficientSolsText;
    private TMP_FontAsset insufficientSolsFont;
    private GameObject holdControls;
    private Vector2 sliderBasePosition;
    private bool isButtonVibrating;
    private float progress;
    private int soulPrice;
    public void Initialize()
    {
        BuildSlider();
        hint = transform.Find("Purchase Hint")?.GetComponent<Text>();
        CreateInsufficientSolsText();
        ResetHold();
    }
    public void ConfigureInsufficientSolsFont(TMP_FontAsset font)
    {
        insufficientSolsFont = font;
        if (insufficientSolsText != null)
            insufficientSolsText.font = font;
    }
    public void ConfigurePrice(int price)
    {
        soulPrice = Mathf.Max(0, price);
        ResetHold();
        ShowPurchaseReady();
    }
    public bool Tick(bool holding, float holdDuration, float releaseDuration)
    {
        if (purchaseSlider == null)
            return false;

        // Keep the buy control visible when a card opens. It becomes disabled
        // only after the player actually tries to hold-buy without enough souls.
        if (holding && !SoulCurrency.EnsureInstance().CanAfford(soulPrice))
        {
            ShowInsufficientFunds();
            return false;
        }
        float fillRate = 1f / Mathf.Max(.01f, holdDuration);
        float drainRate = 1f / Mathf.Max(.01f, releaseDuration);
        progress = holding
            ? Mathf.Min(1f, progress + Time.unscaledDeltaTime * fillRate)
            : Mathf.Max(0f, progress - Time.unscaledDeltaTime * drainRate);
        purchaseSlider.SetValueWithoutNotify(Mathf.Lerp(purchaseSlider.minValue, purchaseSlider.maxValue, progress));
        if (holding)
        {
            StartButtonVibration();
            holdControls.transform.localScale = Vector3.one * 1.045f;
            if (hint != null)
                hint.transform.localScale = Vector3.one * 1.06f;
        }
        else
        {
            StopButtonVibration();
            holdControls.transform.localScale = Vector3.one;
            if (hint != null)
                hint.transform.localScale = Vector3.one;
        }
        return progress >= 1f;
    }
    private void ShowPurchaseReady()
    {
        if (holdControls != null)
            holdControls.SetActive(true);
        if (insufficientSolsText != null)
            insufficientSolsText.gameObject.SetActive(false);
        if (hint != null)
        {
            hint.text = "HOLD LEFT CLICK TO PURCHASE";
            hint.color = new Color(1f, .76f, .25f);
        }
    }
    private void ShowInsufficientFunds()
    {
        ResetHold();
        if (holdControls != null)
            holdControls.SetActive(false);
        if (insufficientSolsText != null)
            insufficientSolsText.gameObject.SetActive(true);
        if (hint != null)
        {
            hint.text = "NOT ENOUGH SOULS";
            hint.color = new Color(1f, .26f, .26f);
        }
    }
    private void StartButtonVibration()
    {
        if (isButtonVibrating || sliderRect == null)
            return;
        isButtonVibrating = true;
        sliderRect.DOKill();
        sliderRect.anchoredPosition = sliderBasePosition;
        sliderRect.DOShakeAnchorPos(.16f, new Vector2(3.25f, 1.25f), 18, 80f, false, true)
            .SetLoops(-1, LoopType.Restart)
            .SetUpdate(true);
    }
    private void StopButtonVibration()
    {
        if (sliderRect == null)
            return;
        sliderRect.DOKill();
        sliderRect.anchoredPosition = sliderBasePosition;
        isButtonVibrating = false;
    }
    private void ResetHold()
    {
        progress = 0f;
        if (purchaseSlider != null)
            purchaseSlider.SetValueWithoutNotify(purchaseSlider.minValue);
        StopButtonVibration();
        if (holdControls != null)
            holdControls.transform.localScale = Vector3.one;
        if (hint != null)
            hint.transform.localScale = Vector3.one;
    }
    private void BuildSlider()
    {
        purchaseSlider = null;
        foreach (Slider candidate in GetComponentsInChildren<Slider>(true))
        {
            if (candidate.gameObject.name == "Buy Slider")
            {
                purchaseSlider = candidate;
                break;
            }
        }
        if (purchaseSlider == null)
        {
            Debug.LogWarning("[ShopGunOffer] The gun information card template needs a Slider named 'Buy Slider'.", this);
            return;
        }
        // The template owns the bar artwork and Fill Rect. This code only
        // drives the slider value, shake, and pop state.
        purchaseSlider.minValue = 0f;
        purchaseSlider.maxValue = 100f;
        purchaseSlider.wholeNumbers = false;
        purchaseSlider.interactable = false;
        sliderRect = purchaseSlider.GetComponent<RectTransform>();
        sliderBasePosition = sliderRect.anchoredPosition;
        holdControls = purchaseSlider.gameObject;
        Transform legacyControl = transform.Find("Purchase Hold Control");
        if (legacyControl != null && !purchaseSlider.transform.IsChildOf(legacyControl))
            legacyControl.gameObject.SetActive(false);
    }
    private void CreateInsufficientSolsText()
    {
        if (purchaseSlider == null || insufficientSolsText != null)
            return;

        RectTransform sliderParent = purchaseSlider.transform.parent as RectTransform;
        if (sliderParent == null)
            return;

        Transform existing = sliderParent.Find("Not Enough Sols");
        if (existing != null)
        {
            insufficientSolsText = existing.GetComponent<TextMeshProUGUI>();
            return;
        }

        GameObject messageObject = new GameObject("Not Enough Sols", typeof(RectTransform), typeof(TextMeshProUGUI));
        messageObject.transform.SetParent(sliderParent, false);
        insufficientSolsText = messageObject.GetComponent<TextMeshProUGUI>();
        RectTransform messageRect = insufficientSolsText.rectTransform;
        messageRect.anchorMin = sliderRect.anchorMin;
        messageRect.anchorMax = sliderRect.anchorMax;
        messageRect.pivot = sliderRect.pivot;
        messageRect.anchoredPosition = sliderRect.anchoredPosition;
        messageRect.sizeDelta = sliderRect.sizeDelta;
        messageRect.localRotation = sliderRect.localRotation;
        messageRect.localScale = sliderRect.localScale;
        insufficientSolsText.font = insufficientSolsFont;
        insufficientSolsText.text = "NOT ENOUGH SOLS";
        insufficientSolsText.fontSize = 28f;
        insufficientSolsText.alignment = TextAlignmentOptions.Center;
        insufficientSolsText.color = new Color(1f, .26f, .26f);
        insufficientSolsText.enableWordWrapping = false;
        insufficientSolsText.raycastTarget = false;
        messageObject.SetActive(false);
    }
    private static Sprite LoadSprite(string resourcePath)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        return texture == null ? null : Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(.5f, .5f));
    }
}
