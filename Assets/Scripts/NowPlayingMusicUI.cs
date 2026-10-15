using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Brief upper-left music banner shown after the iPod hand animation ends.</summary>
public sealed class NowPlayingMusicUI : MonoBehaviour
{
    public static NowPlayingMusicUI Instance { get; private set; }

    [SerializeField] private TMP_FontAsset boldPixelsFont;
    [SerializeField, Min(0f)] private float holdDuration = 2f;
    [SerializeField, Min(.01f)] private float slideDuration = .35f;
    [SerializeField, Min(0f)] private float dotBounceHeight = 9f;
    [SerializeField, Min(0f)] private float dotBounceSpeed = 7f;

    [SerializeField] private RectTransform banner;
    [SerializeField] private TMP_Text songText;
    [SerializeField] private RectTransform[] dots = new RectTransform[3];
    private Coroutine displayRoutine;
    [SerializeField] private Vector2 shownPosition = new Vector2(32f, -38f);
    [SerializeField] private Vector2 hiddenPosition = new Vector2(-510f, -38f);
    private bool isVisible;
    private Sprite circleSprite;

    private void Awake()
    {
        Instance = this;
        if (banner == null)
            CreateBanner();
        else
            banner.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!isVisible)
            return;

        for (int index = 0; index < dots.Length; index++)
        {
            if (dots[index] == null)
                continue;

            float offset = Mathf.Sin(Time.unscaledTime * dotBounceSpeed - index * .65f) * dotBounceHeight;
            dots[index].anchoredPosition = new Vector2(index * 20f + 6f, offset);
        }
    }

    public void ShowNowPlaying(string trackName)
    {
        if (string.IsNullOrWhiteSpace(trackName))
            return;

        if (displayRoutine != null)
            StopCoroutine(displayRoutine);

        songText.text = trackName.ToUpperInvariant();
        displayRoutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        banner.gameObject.SetActive(true);
        isVisible = true;
        yield return Slide(hiddenPosition, shownPosition);
        yield return new WaitForSecondsRealtime(holdDuration);
        yield return Slide(shownPosition, hiddenPosition);
        isVisible = false;
        banner.gameObject.SetActive(false);
        displayRoutine = null;
    }

    private IEnumerator Slide(Vector2 from, Vector2 to)
    {
        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = slideDuration <= 0f ? 1f : Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
            banner.anchoredPosition = Vector2.LerpUnclamped(from, to, progress);
            yield return null;
        }

        banner.anchoredPosition = to;
    }

    private void CreateBanner()
    {
        circleSprite = CreateCircleSprite();

        GameObject canvasObject = new GameObject("Now Playing Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject bannerObject = new GameObject("Now Playing Banner", typeof(RectTransform), typeof(Image));
        bannerObject.transform.SetParent(canvasObject.transform, false);
        banner = bannerObject.GetComponent<RectTransform>();
        banner.anchorMin = banner.anchorMax = new Vector2(0f, 1f);
        banner.pivot = new Vector2(0f, 1f);
        banner.sizeDelta = new Vector2(470f, 112f);
        banner.anchoredPosition = hiddenPosition;
        bannerObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, .8f);

        CreateText("Now Playing Label", "NOW PLAYING", banner, new Vector2(18f, -14f), new Vector2(430f, 28f), 23f, out _);
        CreateText("Song", string.Empty, banner, new Vector2(18f, -46f), new Vector2(420f, 40f), 30f, out songText);

        GameObject dotsRootObject = new GameObject("Animated Dots", typeof(RectTransform));
        dotsRootObject.transform.SetParent(banner, false);
        RectTransform dotsRoot = dotsRootObject.GetComponent<RectTransform>();
        dotsRoot.anchorMin = dotsRoot.anchorMax = new Vector2(0f, 1f);
        dotsRoot.pivot = new Vector2(0f, 1f);
        dotsRoot.anchoredPosition = new Vector2(20f, -91f);
        dotsRoot.sizeDelta = new Vector2(84f, 24f);

        for (int index = 0; index < dots.Length; index++)
        {
            GameObject dotObject = new GameObject("Dot " + (index + 1), typeof(RectTransform), typeof(Image));
            dotObject.transform.SetParent(dotsRoot, false);
            dots[index] = dotObject.GetComponent<RectTransform>();
            dots[index].anchorMin = dots[index].anchorMax = new Vector2(0f, 1f);
            dots[index].pivot = new Vector2(.5f, .5f);
            dots[index].anchoredPosition = new Vector2(index * 20f + 6f, 0f);
            dots[index].sizeDelta = new Vector2(11f, 11f);
            Image dotImage = dotObject.GetComponent<Image>();
            dotImage.sprite = circleSprite;
            dotImage.type = Image.Type.Simple;
            dotImage.color = new Color(.15f, .95f, 1f, 1f);
        }

        bannerObject.SetActive(false);
    }

    private static Sprite CreateCircleSprite()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = "Now Playing Circle"
        };

        float center = (size - 1) * .5f;
        float radius = center;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01((radius - distance) * 2f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(.5f, .5f), size);
    }

    private void CreateText(string objectName, string text, RectTransform parent, Vector2 position, Vector2 size, float fontSize, out TMP_Text result)
    {
        CreateText(objectName, text, parent, position, size, fontSize, out result, out _);
    }

    private void CreateText(string objectName, string text, RectTransform parent, Vector2 position, Vector2 size, float fontSize, out TMP_Text result, out RectTransform rect)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.font = boldPixelsFont;
        label.fontSize = fontSize;
        label.text = text;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Left;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        result = label;
    }
}
