using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class HealthBar : MonoBehaviour
{
    public Image[] blocks;

    public Sprite filledSprite;
    public Sprite emptySprite;

    public int maxValue = 10;

    private void Start()
    {
        SetValue(4);
    }
    public void SetValue(int value)
    {
        value = Mathf.Clamp(value, 0, maxValue);

        for (int i = 0; i < blocks.Length; i++)
        {
            blocks[i].sprite = i < value ? filledSprite : emptySprite;
        }
    }
}