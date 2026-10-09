using System;
using System.Collections.Generic;
using DG.Tweening;
using InfimaGames.LowPolyShooterPack;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;public class ShopGunOfferController : MonoBehaviour
{
    [Header("3D Gun Display Prefabs")]
    [Tooltip("Assign the gun-model prefabs here. Three distinct prefabs are selected for each shop.")]
    [SerializeField] private GameObject[] gunDisplayPrefabs;
    private readonly List<GameObject> offers = new();
    private readonly List<WeaponBehaviour> offeredWeapons = new();
    private Action completed;
    private Camera playerCamera;
    private Vector2 cursorPosition;
    private RectTransform cursorVisual;
    private GameObject crosshair;
    private GameObject hoveredOffer;
    private GameObject confirmation;
    private bool awaitingConfirmation;

    public void ShowOffers(Transform shopRoot, Action onCompleted)
    {
        ClearOffers();
        completed = onCompleted;
        playerCamera = FindGameplayCamera();

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null && playerCamera != null)
        {
            Vector3 direction = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized;
            player.transform.DOMove(player.transform.position + direction * .3f, .32f)
                .SetEase(Ease.InOutSine).SetUpdate(true);
            playerCamera.transform.DOPunchPosition(Vector3.up * .012f, .24f, 2, .35f).SetUpdate(true);
        }

        Inventory inventory = player != null ? player.GetComponentInChildren<Inventory>(true) : null;
        WeaponBehaviour equipped = inventory != null ? inventory.GetEquipped() : null;
        var candidates = new List<WeaponBehaviour>();
        if (inventory != null && inventory.weapons != null)
            foreach (WeaponBehaviour weapon in inventory.weapons)
                if (weapon != null && weapon != equipped) candidates.Add(weapon);

        while (candidates.Count > 0 && offeredWeapons.Count < 3)
        {
            int index = UnityEngine.Random.Range(0, candidates.Count);
            offeredWeapons.Add(candidates[index]);
            candidates.RemoveAt(index);
        }

        while (offeredWeapons.Count < 3)
            offeredWeapons.Add(null);

        Transform table = FindChild(shopRoot, "Table");
        Vector3 center = table != null ? table.position : shopRoot.position + shopRoot.forward * 2f;
        center.y = -9.5f;
        center.z = -116.5f;
        Vector3 right = table != null ? table.right : shopRoot.right;
        for (int i = 0; i < 3; i++)
        {
            Transform slot = FindChild(shopRoot, "Gun Offer Slot " + (i + 1));
            Vector3 position = slot != null ? slot.position : center + right * ((i - 1) * .65f);
            Quaternion rotation = slot != null ? slot.rotation : shopRoot.rotation;
            offers.Add(CreateOffer(i, position, rotation));
        }

        offers.Add(CreateCloseButton(center + (table != null ? table.forward : shopRoot.forward) * .55f, shopRoot.rotation));
        cursorPosition = new Vector2(Screen.width * .5f, Screen.height * .5f);
        cursorVisual = FindCursor();
        if (cursorVisual != null) cursorVisual.gameObject.SetActive(true);
        crosshair = FindCrosshair();
        if (crosshair != null) crosshair.SetActive(false);
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
            if (candidate == null || candidate.closeShop)
                continue;

            hoveredOffer = candidate.gameObject;
            break;
        }
        foreach (GameObject offer in offers)
        {
            if (offer == null) continue;
            bool hovered = offer == hoveredOffer;
            offer.transform.localScale = hovered ? Vector3.one * 1.1f : Vector3.one;
            offer.GetComponent<ShopGunOffer>()?.SetHovered(hovered);
        }

        if (mouse.leftButton.wasPressedThisFrame && hoveredOffer != null)
        {
            ShopGunOffer offer = hoveredOffer.GetComponent<ShopGunOffer>();
            if (offer.closeShop)
                Complete();
            else if (!awaitingConfirmation)
                ShowConfirmation(offer);
            else if (hoveredOffer == confirmation)
                Complete();
        }

        if (mouse.rightButton.wasPressedThisFrame && awaitingConfirmation)
            HideConfirmation();
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
        gun.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

        string gunName = offeredWeapons[index] == null ? "PROTOTYPE WEAPON" : offeredWeapons[index].name.ToUpperInvariant();
        Canvas canvas = CreateGunInformationCard(root.transform, gunName, index);
        Vector3 cardPosition = root.transform.TransformPoint(new Vector3(0f, 1.05f, 0f));
        Vector3 towardPlayer = Vector3.ProjectOnPlane(playerCamera.transform.position - cardPosition, Vector3.up).normalized;
        cardPosition += towardPlayer * .65f + playerCamera.transform.right * .28f;
        canvas.transform.position = cardPosition;
        canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - playerCamera.transform.position);
        offerComponent.SetInfo(canvas.GetComponentInChildren<Text>(true));
        return root;
    }

private Canvas CreateGunInformationCard(Transform parent, string gunName, int index)
    {
        GameObject card = new GameObject("Gun Information", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        card.transform.SetParent(parent, false);
        card.transform.localScale = new Vector3(.0015f, .0021f, .0021f);

        Canvas canvas = card.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = playerCamera;
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(1150f, 650f);

        Image background = card.AddComponent<Image>();
        background.color = new Color(.015f, .05f, .08f, .96f);

        CreateImage(card.transform, "Glow Frame", Vector2.zero, new Vector2(1125f, 625f), new Color(.03f, .7f, .85f, .22f));
        Image media = CreateImage(card.transform, "Media Placeholder", new Vector2(-365f, 0f), new Vector2(330f, 470f), new Color(.04f, .18f, .25f, 1f));
        CreateText(media.transform, "Media Label", "LIVE PREVIEW\n\nGUN IMAGE / VIDEO\nPLACEHOLDER", Vector2.zero, new Vector2(300f, 430f), 32, TextAnchor.MiddleCenter, new Color(.2f, .95f, 1f));

        CreateText(card.transform, "Weapon Label", "ARSENAL OFFER // 0" + (index + 1), new Vector2(125f, 235f), new Vector2(610f, 55f), 28, TextAnchor.MiddleLeft, new Color(.2f, .95f, 1f));
        CreateText(card.transform, "Weapon Name", gunName, new Vector2(125f, 165f), new Vector2(610f, 85f), 50, TextAnchor.MiddleLeft, Color.white);
        Text description = CreateText(card.transform, "Weapon Description", "Experimental weapon platform. Precise, reliable, and ready for field evaluation.", new Vector2(125f, 95f), new Vector2(610f, 75f), 28, TextAnchor.UpperLeft, new Color(.78f, .9f, .94f));

        AddStatBar(card.transform, "DAMAGE", 80, 25f);
        AddStatBar(card.transform, "FIRE RATE", 70, -35f);
        AddStatBar(card.transform, "RELOAD", 50, -95f);
        AddStatBar(card.transform, "CAPACITY", 60, -155f);
        CreateText(card.transform, "Purchase Hint", "LEFT CLICK: PURCHASE", new Vector2(125f, -235f), new Vector2(610f, 42f), 24, TextAnchor.MiddleLeft, new Color(1f, .76f, .25f));
        return canvas;
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

    private static void AddStatBar(Transform parent, string label, int value, float y)
    {
        CreateText(parent, label + " Label", label + "  " + value, new Vector2(-10f, y), new Vector2(340f, 38f), 26, TextAnchor.MiddleLeft, Color.white);
        Image track = CreateImage(parent, label + " Track", new Vector2(270f, y), new Vector2(300f, 20f), new Color(.1f, .16f, .2f, 1f));
        Image fill = CreateImage(track.transform, label + " Fill", Vector2.zero, new Vector2(Mathf.Clamp01(value / 100f) * 286f, 12f), new Color(.15f, .9f, 1f, 1f));
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, .5f);
        fillRect.anchorMax = new Vector2(0f, .5f);
        fillRect.pivot = new Vector2(0f, .5f);
        fillRect.anchoredPosition = new Vector2(7f, 0f);
    }


    private void ShowConfirmation(ShopGunOffer offer)
    {
        awaitingConfirmation = true;
        confirmation = offer.gameObject;
        offer.SetConfirmation(true);
    }

    private void HideConfirmation()
    {
        if (confirmation != null) confirmation.GetComponent<ShopGunOffer>().SetConfirmation(false);
        confirmation = null; awaitingConfirmation = false;
    }

    private void Complete()
    {
        ClearOffers();
        completed?.Invoke();
    }

    public void CloseShop()
    {
        ClearOffers();
        completed?.Invoke();
    }

    private void ClearOffers()
    {
        foreach (GameObject offer in offers) if (offer != null) Destroy(offer);
        offers.Clear(); offeredWeapons.Clear();
        if (cursorVisual != null) cursorVisual.gameObject.SetActive(false);
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


private GameObject CreateCloseButton(Vector3 position, Quaternion rotation)
    {
        GameObject root = new GameObject("Close Shop Button");
        root.transform.position = position;
        root.transform.rotation = rotation;
        root.AddComponent<BoxCollider>().size = new Vector3(.7f, .2f, .15f);
        ShopGunOffer offer = root.AddComponent<ShopGunOffer>();
        offer.closeShop = true;

        GameObject label = new GameObject("Close Shop Label", typeof(Canvas), typeof(Text));
        label.transform.SetParent(root.transform, false);
        label.transform.localPosition = Vector3.up * .25f;
        label.transform.localScale = Vector3.one * .002f;
        Canvas canvas = label.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = playerCamera;
        Text text = label.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = "CLOSE SHOP"; text.alignment = TextAnchor.MiddleCenter; text.fontSize = 40; text.color = Color.red;
        return root;
    }


private GameObject CreateDisplayModel(int offerIndex)
    {
        GameObject prefab = null;
        if (gunDisplayPrefabs != null && gunDisplayPrefabs.Length > 0)
            prefab = gunDisplayPrefabs[offerIndex % gunDisplayPrefabs.Length];

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
    
    public bool closeShop;
public WeaponBehaviour weapon;
    
    private bool isHovered;
private Text info;

private void Awake() { }
    public void SetConfirmation(bool active)
    {
        if (info == null) return;
        info.text = active ? "PURCHASE " + (weapon == null ? "PROTOTYPE" : weapon.name) + "?\nLEFT CLICK AGAIN: OK\nRIGHT CLICK: CANCEL" : info.text;
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
            hologram.localScale = new Vector3(.00015f, .0002f, .0002f);
            hologram.DOScale(new Vector3(.00165f, .00225f, .00225f), .22f).SetEase(Ease.OutBack).SetUpdate(true);
        }
        else if (canvas.enabled)
        {
            hologram.DOScale(new Vector3(.00015f, .0002f, .0002f), .16f).SetEase(Ease.InBack).SetUpdate(true)
                .OnComplete(() => canvas.enabled = false);
        }
    }


public void SetInfo(Text value)
    {
        info = value;
        if (info != null)
            info.GetComponentInParent<Canvas>().enabled = false;
    }
}