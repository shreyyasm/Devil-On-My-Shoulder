using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FinisherIndicator : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI text;

    [Header("Colors")]
    [SerializeField] private Color normalBackgroundColor = Color.black;
    [SerializeField] private Color normalTextColor = Color.white;

    [SerializeField] private Color activeBackgroundColor = Color.red;
    [SerializeField] private Color activeTextColor = Color.black;

    [Header("Blink Settings")]
    [SerializeField] private float switchInterval = 0.5f;

    private float timer;
    private bool isActive;

    private void OnEnable()
    {
        timer = 0f;
        isActive = false;

        SetColors();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= switchInterval)
        {
            timer = 0f;

            isActive = !isActive;

            SetColors();
        }
    }

    private void SetColors()
    {
        if (isActive)
        {
            background.color = activeBackgroundColor;
            //text.color = activeTextColor;
        }
        else
        {
            background.color = normalBackgroundColor;
           // text.color = normalTextColor;
        }
    }
}