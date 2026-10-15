using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopUpgradeSelection : MonoBehaviour
{
    [SerializeField] private Canvas hologramCanvas;
    [SerializeField] private RectTransform cardContainer;
    [SerializeField] private ShopUpgradeCard[] cards;
    [SerializeField] private RectTransform virtualCursorVisual;
    [SerializeField] private float distanceInFrontOfPlayer = 2.2f;
    [SerializeField] private float verticalOffset = -0.05f;
    [SerializeField] private float selectedCardScale = 1.2f;
    [SerializeField] private float selectionDuration = 0.3f;
    [SerializeField] private float virtualCursorSensitivity = 1f;

    private Camera playerCamera;
    private GraphicRaycaster graphicRaycaster;
    private PointerEventData virtualPointer;
    private ShopUpgradeCard hoveredCard;
    private Vector2 virtualCursorPosition;
    private Vector3 hologramBaseScale;
    private bool hasSelection;
    private System.Action selectionCallback;
    private GameObject gameplayCrosshair;
    private RoguelikeManager.UpgradeOffer[] currentOffers;

    private void Awake()
    {
        if (hologramCanvas == null)
            hologramCanvas = GetComponent<Canvas>();

        graphicRaycaster = GetComponent<GraphicRaycaster>();
        hologramBaseScale = hologramCanvas.transform.localScale;
        HideImmediate();
    }

    private void Update()
    {
        if (hologramCanvas.enabled && currentOffers == null)
            PopulateRoguelikeCards();

        if (!hologramCanvas.enabled || hasSelection)
            return;

        UnityEngine.InputSystem.Mouse mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse == null)
            return;

        virtualCursorPosition += mouse.delta.ReadValue() * virtualCursorSensitivity;
        virtualCursorPosition.x = Mathf.Clamp(virtualCursorPosition.x, 0f, Screen.width);
        virtualCursorPosition.y = Mathf.Clamp(virtualCursorPosition.y, 0f, Screen.height);

        UpdateVirtualCursorVisual();
        UpdateHoveredCard();

        if (mouse.leftButton.wasPressedThisFrame && hoveredCard != null)
            SelectCard(hoveredCard);
    }

public void ShowForShop()
    {
        if (hologramCanvas == null)
            hologramCanvas = GetComponent<Canvas>();

        if (graphicRaycaster == null)
            graphicRaycaster = GetComponent<GraphicRaycaster>();

        if (hologramBaseScale.sqrMagnitude < 0.000001f)
            hologramBaseScale = hologramCanvas.transform.localScale;

        hasSelection = false;
        playerCamera = FindPlayerCamera();

        if (playerCamera != null)
        {
            Vector3 forward = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f)
                forward = playerCamera.transform.forward;

            Transform canvasTransform = hologramCanvas.transform;
            Vector3 targetPosition = playerCamera.transform.position + forward * distanceInFrontOfPlayer + Vector3.up * verticalOffset;
            canvasTransform.DOKill();
            canvasTransform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            canvasTransform.position = targetPosition + Vector3.down * 0.25f;
            canvasTransform.localScale = hologramBaseScale * 0.05f;
            canvasTransform.DOMove(targetPosition, 0.42f).SetEase(Ease.OutCubic).SetUpdate(true);
            canvasTransform.DOScale(hologramBaseScale, 0.42f).SetEase(Ease.OutBack).SetUpdate(true);
            hologramCanvas.worldCamera = playerCamera;
        }

        PopulateRoguelikeCards();

        hologramCanvas.enabled = true;
        graphicRaycaster.enabled = true;

        gameplayCrosshair = FindGameplayCrosshair();
        if (gameplayCrosshair != null)
            gameplayCrosshair.SetActive(false);

        virtualCursorPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        virtualPointer = new PointerEventData(EventSystem.current);
        UpdateVirtualCursorVisual();
        if (virtualCursorVisual != null)
            virtualCursorVisual.gameObject.SetActive(true);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void SetSelectionCallback(System.Action callback)
    {
        selectionCallback = callback;
    }

    private void PopulateRoguelikeCards()
    {
        RoguelikeManager manager = RoguelikeManager.Instance;
        if (manager == null)
            manager = FindFirstObjectByType<RoguelikeManager>(FindObjectsInactive.Include);

        currentOffers = manager != null
            ? manager.RollThreeUpgrades().ToArray()
            : System.Array.Empty<RoguelikeManager.UpgradeOffer>();

        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].ResetCard();
            cards[i].SetUpgrade(i < currentOffers.Length ? currentOffers[i] : null);
        }
    }

    public void HideImmediate()
    {
        if (hologramCanvas != null)
        {
            hologramCanvas.transform.DOKill();
            hologramCanvas.enabled = false;
        }

        if (graphicRaycaster != null)
            graphicRaycaster.enabled = false;

        if (virtualCursorVisual != null)
            virtualCursorVisual.gameObject.SetActive(false);

        if (gameplayCrosshair != null)
            gameplayCrosshair.SetActive(true);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void SelectCard(ShopUpgradeCard selectedCard)
    {
        if (hasSelection || selectedCard == null)
            return;

        hasSelection = true;
        selectedCard.ApplyUpgrade();
        if (hoveredCard != null)
            hoveredCard.SetHovered(false);

        StartCoroutine(SelectCardRoutine(selectedCard));
    }

    private IEnumerator SelectCardRoutine(ShopUpgradeCard selectedCard)
    {
        foreach (ShopUpgradeCard card in cards)
        {
            card.SetInteractable(false);
            if (card != selectedCard)
                card.transform.DOScale(Vector3.zero, selectionDuration).SetEase(Ease.InBack);
        }

        selectedCard.transform.DOLocalMove(Vector3.zero, selectionDuration).SetEase(Ease.OutCubic);
        selectedCard.transform.DOScale(Vector3.one * selectedCardScale, selectionDuration).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(selectionDuration);

        selectedCard.PlaySelectedFeedback();
        yield return new WaitForSecondsRealtime(.3f);

        Transform hologram = hologramCanvas.transform;
        hologram.DOKill();
        hologram.DOScale(hologramBaseScale * .05f, .22f).SetEase(Ease.InBack).SetUpdate(true);
        yield return new WaitForSecondsRealtime(.22f);

        HideImmediate();
        selectionCallback?.Invoke();
    }

private void UpdateHoveredCard()
    {
        if (playerCamera == null)
            return;

        ShopUpgradeCard nextCard = null;
        foreach (ShopUpgradeCard card in cards)
        {
            if (card.gameObject.activeInHierarchy &&
                RectTransformUtility.RectangleContainsScreenPoint(
                    card.GetComponent<RectTransform>(),
                    virtualCursorPosition,
                    playerCamera))
            {
                nextCard = card;
                break;
            }
        }

        if (nextCard == hoveredCard)
            return;

        if (hoveredCard != null)
            hoveredCard.SetHovered(false);

        hoveredCard = nextCard;
        if (hoveredCard != null)
            hoveredCard.SetHovered(true);
    }

    private void UpdateVirtualCursorVisual()
    {
        if (virtualCursorVisual == null || virtualCursorVisual.parent == null)
            return;

        RectTransform parentRect = virtualCursorVisual.parent as RectTransform;
        if (parentRect == null)
            return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, virtualCursorPosition, null, out localPoint);
        virtualCursorVisual.anchoredPosition = localPoint;
    }

private Camera FindPlayerCamera()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Camera bestCamera = null;
            foreach (Camera candidate in player.GetComponentsInChildren<Camera>(true))
            {
                if (!candidate.isActiveAndEnabled || candidate.cullingMask == 0)
                    continue;

                if (bestCamera == null || candidate.depth > bestCamera.depth)
                    bestCamera = candidate;
            }

            if (bestCamera != null)
                return bestCamera;
        }

        return Camera.main;
    }

    private GameObject FindGameplayCrosshair()
    {
        foreach (Transform candidate in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (candidate.gameObject.scene.IsValid() &&
                candidate.name == "Crosshair" &&
                candidate.parent != null &&
                candidate.parent.name == "P_LPSP_UI_Canvas")
                return candidate.gameObject;
        }

        return null;
    }
}

[RequireComponent(typeof(Button))]
public class ShopUpgradeCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private ShopUpgradeSelection selection;
    [SerializeField] private Image frame;
    [SerializeField] private float hoverScale = 1.12f;
    [SerializeField] private float hoverDuration = 0.12f;

    private Button button;
    private Vector3 startingLocalPosition;
    private RoguelikeManager.UpgradeOffer upgrade;
    private Material polygonMaterial;
    private Color activeBackgroundColor;
    private bool hasActiveBackgroundColor;
    [Header("Scene Card Text")]
    [SerializeField] private Text upgradeTitle;
    [SerializeField] private Text upgradeCurrent;
    [SerializeField] private Text upgradeNext;
    [SerializeField, Tooltip("Image shown below the upgrade title. Created automatically if the card has no child named Upgrade Sprite.")] private Image upgradeSprite;

    private void Awake()
    {
        button = GetComponent<Button>();
        startingLocalPosition = transform.localPosition;
        button.onClick.AddListener(Choose);
        ConfigurePolygonBackground();

        PolygonPanelFloat floatingPanel = GetComponent<PolygonPanelFloat>();
        if (floatingPanel == null)
            floatingPanel = gameObject.AddComponent<PolygonPanelFloat>();
        floatingPanel.CacheContents();
    }

    public void ResetCard()
    {
        if (button == null)
            button = GetComponent<Button>();

        transform.DOKill();
        transform.localPosition = startingLocalPosition;
        transform.localScale = Vector3.one;
        frame.color = Color.white;
        SetPolygonColor(GetBaseBackgroundColor());
        button.interactable = true;

        PolygonPanelFloat floatingPanel = GetComponent<PolygonPanelFloat>();
        if (floatingPanel != null)
        {
            floatingPanel.enabled = true;
            floatingPanel.CacheContents();
        }
    }

    public void SetInteractable(bool value)
    {
        if (button == null)
            button = GetComponent<Button>();

        button.interactable = value;

        if (!value)
        {
            PolygonPanelFloat floatingPanel = GetComponent<PolygonPanelFloat>();
            if (floatingPanel != null)
                floatingPanel.enabled = false;
        }
    }

    public void SetUpgrade(RoguelikeManager.UpgradeOffer value)
    {
        upgrade = value;
        ResolveSceneText();
        SetUpgradeSprite(upgrade != null ? upgrade.displaySprite : null);
        SetBackgroundColor(upgrade != null ? upgrade.cardBackgroundColor : Color.white);
        if (upgradeTitle == null || upgradeCurrent == null || upgradeNext == null)
            return;

        float current = RoguelikeManager.Instance != null ? RoguelikeManager.Instance.GetCurrentDisplayValue(upgrade) : 0f;
        float next = current + (upgrade != null ? upgrade.amount : 0f);
        string name = upgrade != null ? upgrade.displayName.ToUpperInvariant() : "MYSTERY UPGRADE";
        // Keep both values together on one row so players can read the change at a glance.
        // The UI's legacy font has no reliable Unicode arrow glyph, so use an ASCII arrow.
        string currentText = current.ToString("0.##");
        string nextText = "->  " + next.ToString("0.##");
        upgradeTitle.gameObject.SetActive(true);
        upgradeCurrent.gameObject.SetActive(true);
        upgradeNext.gameObject.SetActive(true);

        upgradeTitle.text = name;
        upgradeCurrent.text = currentText;
        upgradeNext.text = nextText;
        upgradeTitle.alignment = TextAnchor.MiddleCenter;
        upgradeCurrent.alignment = TextAnchor.MiddleCenter;
        upgradeNext.alignment = TextAnchor.MiddleCenter;
        upgradeNext.color = new Color(0.25f, 1f, 0.38f, 1f);
    }

    private void ResolveSceneText()
    {
        if (upgradeTitle == null)
            upgradeTitle = transform.Find("Card Text")?.GetComponent<Text>();
        if (upgradeCurrent == null)
            upgradeCurrent = transform.Find("Current Value")?.GetComponent<Text>();
        if (upgradeNext == null)
            upgradeNext = transform.Find("Next Value")?.GetComponent<Text>();
    }

    private void SetUpgradeSprite(Sprite sprite)
    {
        if (upgradeSprite == null)
            upgradeSprite = transform.Find("Upgrade Sprite")?.GetComponent<Image>();

        if (upgradeSprite == null)
        {
            GameObject iconObject = new GameObject("Upgrade Sprite", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(transform, false);
            upgradeSprite = iconObject.GetComponent<Image>();
            upgradeSprite.raycastTarget = false;

            RectTransform iconRect = upgradeSprite.rectTransform;
            iconRect.anchorMin = new Vector2(.5f, .5f);
            iconRect.anchorMax = new Vector2(.5f, .5f);
            iconRect.pivot = new Vector2(.5f, .5f);
            iconRect.anchoredPosition = new Vector2(0f, 28f);
            iconRect.sizeDelta = new Vector2(150f, 150f);
        }

        bool hasSprite = sprite != null;
        upgradeSprite.sprite = sprite;
        upgradeSprite.preserveAspect = true;
        upgradeSprite.color = Color.white;
        upgradeSprite.gameObject.SetActive(hasSprite);

        PolygonPanelFloat floatingPanel = GetComponent<PolygonPanelFloat>();
        if (floatingPanel != null)
            floatingPanel.CacheContents();
    }
    public void ApplyUpgrade()
    {
        if (upgrade != null)
            RoguelikeManager.Instance?.ApplyUpgrade(upgrade);
    }

public void OnPointerEnter(PointerEventData eventData)
    {
        SetHovered(true);
    }

public void OnPointerExit(PointerEventData eventData)
    {
        SetHovered(false);
    }

    public void PlaySelectedFeedback()
    {
        // The roll's Roguelike Manager color remains unchanged on selection.
        transform.DOPunchScale(Vector3.one * 0.15f, 0.3f, 6, 0.8f);
    }

    private void Choose()
    {
        selection.SelectCard(this);
    }


public void SetHovered(bool hovered)
    {
        if (button == null)
            button = GetComponent<Button>();

        if (!button.interactable)
            return;

        transform.DOKill();
        transform.DOScale(hovered ? Vector3.one * hoverScale : Vector3.one, hoverDuration)
            .SetEase(hovered ? Ease.OutBack : Ease.OutQuad);
    }

    private void SetBackgroundColor(Color color)
    {
        // Existing scene data predates this field and therefore deserializes as clear.
        if (color == Color.clear)
            color = Color.white;

        activeBackgroundColor = color;
        hasActiveBackgroundColor = true;

        // The shader reads its color from the material. Keep the UI vertex
        // color white so it cannot tint or conflict with the Manager color.
        if (frame != null)
            frame.color = Color.white;
        SetPolygonColor(activeBackgroundColor);
    }

    private Color GetBaseBackgroundColor()
    {
        return hasActiveBackgroundColor ? activeBackgroundColor : Color.white;
    }
    private void ConfigurePolygonBackground()
    {
        if (frame == null || frame.material == null || !frame.material.HasProperty("_Color"))
            return;

        polygonMaterial = new Material(frame.material);
        frame.material = polygonMaterial;
        SetPolygonColor(GetBaseBackgroundColor());
    }

    private void SetPolygonColor(Color color)
    {
        if (polygonMaterial != null && polygonMaterial.HasProperty("_Color"))
            polygonMaterial.SetColor("_Color", color);
    }
}

