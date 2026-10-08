using UnityEngine;
using UnityEngine.UI;

/// <summary>Creates the boss combat health display when the encounter begins.</summary>
public class BossHealthBar : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private string bossName = "THE ARCHITECT";
    [SerializeField] private Color healthColour = new Color(0.86f, 0.08f, 0.14f, 1f);
    [SerializeField] private float healthLerpSpeed = 6f;

    private GameObject display;
    private RectTransform fill;
    private CanvasGroup canvasGroup;
    private float targetHealth = 1f;
    private float displayedHealth = 1f;
    private bool visible;

    private void Awake()
    {
        CreateDisplay();
        display.SetActive(false);
    }

    private void Update()
    {
        if (!visible || fill == null) return;
        displayedHealth = Mathf.MoveTowards(displayedHealth, targetHealth, healthLerpSpeed * Time.deltaTime);
        fill.anchorMax = new Vector2(displayedHealth, 1f);
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, 5f * Time.deltaTime);
    }

    public void Show(float normalizedHealth)
    {
        targetHealth = displayedHealth = Mathf.Clamp01(normalizedHealth);
        fill.anchorMax = new Vector2(displayedHealth, 1f);
        display.SetActive(true);
        canvasGroup.alpha = 0f;
        visible = true;
    }

    public void SetHealth(float normalizedHealth) => targetHealth = Mathf.Clamp01(normalizedHealth);

    public void Hide()
    {
        visible = false;
        if (display != null) display.SetActive(false);
    }

    private void CreateDisplay()
    {
        GameObject canvasObject = new GameObject("Boss Health Bar Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        display = new GameObject("Boss Health Bar", typeof(RectTransform), typeof(CanvasGroup));
        display.transform.SetParent(canvasObject.transform, false);
        canvasGroup = display.GetComponent<CanvasGroup>();
        RectTransform root = display.GetComponent<RectTransform>();
        root.anchorMin = root.anchorMax = new Vector2(.5f, .91f);
        root.sizeDelta = new Vector2(1120f, 74f);

        RectTransform title = MakeText("Boss Name", root, bossName, 27, TextAnchor.MiddleCenter, new Color(.94f, .9f, .8f));
        title.anchorMin = new Vector2(0f, 1f); title.anchorMax = new Vector2(1f, 1f);
        title.pivot = new Vector2(.5f, 1f); title.anchoredPosition = new Vector2(0f, 0f); title.sizeDelta = new Vector2(0f, 34f);

        RectTransform frame = MakeImage("Frame", root, new Color(.04f, .025f, .025f, .96f));
        frame.anchorMin = new Vector2(0f, 0f); frame.anchorMax = new Vector2(1f, 0f);
        frame.pivot = new Vector2(.5f, 0f); frame.sizeDelta = new Vector2(0f, 25f);

        RectTransform empty = MakeImage("Empty", frame, new Color(.18f, .07f, .07f, 1f));
        empty.anchorMin = new Vector2(0f, 0f); empty.anchorMax = new Vector2(1f, 1f);
        empty.offsetMin = new Vector2(4f, 4f);
        empty.offsetMax = new Vector2(-4f, -4f);

        fill = MakeImage("Health", empty, healthColour);
        fill.anchorMin = new Vector2(0f, 0f); fill.anchorMax = new Vector2(1f, 1f);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
    }

    private static RectTransform MakeImage(string name, Transform parent, Color colour)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = CreateWhiteSprite();
        image.color = colour;
        return go.GetComponent<RectTransform>();
    }

    private static RectTransform MakeText(string name, Transform parent, string content, int fontSize, TextAnchor alignment, Color colour)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = colour;
        text.fontStyle = FontStyle.Bold;
        return go.GetComponent<RectTransform>();
    }

    private static Sprite CreateWhiteSprite()
    {
        return Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(.5f, .5f));
    }
}
