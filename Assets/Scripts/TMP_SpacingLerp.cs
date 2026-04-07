using UnityEngine;
using TMPro;

public class TMP_SpacingLerp : MonoBehaviour
{
    [Header("Text Settings")]
    public TextMeshProUGUI tmpText;  // Assign your TextMeshProUGUI here

    [Header("Lerp Settings")]
    public float minSpacing = 0f;
    public float maxSpacing = 3f;
    public float speed = 1f;

    private float t = 0f;
    private bool increasing = true;

    void Reset()
    {
        // Auto-assign TextMeshProUGUI if attached to same GameObject
        if (tmpText == null)
            tmpText = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (tmpText == null) return;

        // Lerp value between min and max
        float spacing = Mathf.Lerp(minSpacing, maxSpacing, t);
        tmpText.characterSpacing = spacing;

        // Update t value
        if (increasing)
        {
            t += Time.deltaTime * speed;
            if (t >= 1f)
            {
                t = 1f;
                increasing = false;
            }
        }
        else
        {
            t -= Time.deltaTime * speed;
            if (t <= 0f)
            {
                t = 0f;
                increasing = true;
            }
        }
    }
}
