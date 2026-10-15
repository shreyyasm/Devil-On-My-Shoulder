using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gives a UI panel and its direct visual elements one shared floating motion.
/// The panel shape can animate in its shader while the title, images, and
/// values drift with the same beat.
/// </summary>
[DisallowMultipleComponent]
public sealed class PolygonPanelFloat : MonoBehaviour
{
    [Header("Panel Motion")]
    [SerializeField] private bool floatPanelPosition;
    [SerializeField] private Vector2 panelFloatDistance = new Vector2(8f, 5f);
    [SerializeField] private Vector2 contentFloatDistance = new Vector2(6f, 4f);
    [SerializeField, Min(.01f)] private float floatSpeed = .75f;
    [SerializeField, Range(0f, 5f)] private float tiltDegrees = 1.25f;
    [SerializeField] private float phaseOffset;

    private RectTransform panel;
    private Vector2 panelBasePosition;
    private Quaternion panelBaseRotation;
    private readonly List<RectTransform> contents = new();
    private readonly List<Vector2> contentBasePositions = new();

    private void Awake()
    {
        CacheContents();
    }

    private void Start()
    {
        CacheContents();
    }

    public void CacheContents()
    {
        panel = transform as RectTransform;
        if (panel == null)
            return;

        panelBaseRotation = panel.localRotation;
        panelBasePosition = panel.anchoredPosition;
        contents.Clear();
        contentBasePositions.Clear();

        foreach (Transform child in transform)
        {
            RectTransform childRect = child as RectTransform;
            if (childRect == null)
                continue;

            contents.Add(childRect);
            contentBasePositions.Add(childRect.anchoredPosition);
        }
    }

    private void LateUpdate()
    {
        if (panel == null)
            CacheContents();
        if (panel == null)
            return;

        float time = Time.unscaledTime * floatSpeed + phaseOffset;
        Vector2 offset = new Vector2(Mathf.Sin(time * 1.13f), Mathf.Cos(time * .91f)) * contentFloatDistance;
        float tilt = Mathf.Sin(time * .83f) * tiltDegrees;

        if (floatPanelPosition)
        {
            Vector2 panelOffset = new Vector2(Mathf.Sin(time * .79f), Mathf.Cos(time * 1.07f)) * panelFloatDistance;
            panel.anchoredPosition = panelBasePosition + panelOffset;
        }

        panel.localRotation = panelBaseRotation * Quaternion.Euler(0f, 0f, tilt);

        for (int i = 0; i < contents.Count; i++)
            if (contents[i] != null)
                contents[i].anchoredPosition = contentBasePositions[i] + offset;
    }

    private void OnDisable()
    {
        if (panel != null)
        {
            panel.anchoredPosition = panelBasePosition;
            panel.localRotation = panelBaseRotation;
        }

        for (int i = 0; i < contents.Count; i++)
            if (contents[i] != null)
                contents[i].anchoredPosition = contentBasePositions[i];
    }
}
