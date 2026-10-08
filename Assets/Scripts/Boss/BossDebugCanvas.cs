using UnityEngine;
using UnityEngine.UI;

/// <summary>Runtime combat readout used while tuning the boss encounter.</summary>
public class BossDebugCanvas : MonoBehaviour
{
    private Text readout;
    private string phase = "Phase 1";
    private string state = "Dormant";
    private string attack = "None";
    private string vulnerability = "Normal";
    private float healthPercent = 1f;

    private void Awake()
    {
        GameObject canvasObject = new GameObject("Boss Debug Canvas", typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject panel = new GameObject("Boss Combat Debug", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, .7f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(24f, -24f);
        panelRect.sizeDelta = new Vector2(420f, 140f);

        GameObject textObject = new GameObject("Readout", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(panel.transform, false);
        readout = textObject.GetComponent<Text>();
        readout.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        readout.fontSize = 22;
        readout.fontStyle = FontStyle.Bold;
        readout.alignment = TextAnchor.UpperLeft;
        readout.color = new Color(.7f, .95f, 1f, 1f);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 12f);
        textRect.offsetMax = new Vector2(-16f, -12f);
        Refresh();
    }

    public void SetStatus(string phase, string state, string attack, float healthPercent, string vulnerability)
    {
        this.phase = phase;
        this.state = state;
        this.attack = attack;
        this.healthPercent = healthPercent;
        this.vulnerability = vulnerability;
        Refresh();
    }

    private void Refresh()
    {
        if (readout == null) return;
        readout.text = "BOSS COMBAT DEBUG\n" +
                       "Phase: " + phase + "   HP: " + Mathf.CeilToInt(healthPercent * 100f) + "%\n" +
                       "State: " + state + "\n" +
                       "Attack: " + attack + "\n" +
                       "Damage State: " + vulnerability;
    }
}
