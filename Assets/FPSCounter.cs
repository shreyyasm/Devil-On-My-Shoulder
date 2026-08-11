using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI fpsText;

    private float timer;
    private int frameCount;

    private void Update()
    {
        frameCount++;
        timer += Time.unscaledDeltaTime;

        if (timer >= 1f)
        {
            fpsText.text = $"FPS: {frameCount}";

            frameCount = 0;
            timer = 0f;
        }
    }
}