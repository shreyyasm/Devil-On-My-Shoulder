using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Heartbeat : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;
    public Image heartbeatImage;
    public Animator heartbeatAnimator;
    public RectTransform heartbeatRect;
    public TextMeshProUGUI healthText;
    public Image heartbeatBG;
    public Image heartbeatBGStrip;
    public Image humanBodyUI;
    public Slider healthSlider;

    [Header("Colors")]
    public Color greenColor = Color.green;
    public Color yellowColor = Color.yellow;
    public Color redColor = Color.red;

    [Header("Animator Speeds")]
    public float greenSpeed = 1f;
    public float yellowSpeed = 2f;
    public float redSpeed = 3f;

    [Header("Y Scale")]
    public float greenYScale = 1f;
    public float yellowYScale = 0.6f;
    public float redYScale = 0.3f;

    private float maxHealth;

    private void Start()
    {
        maxHealth = playerHealth.maxHealth;
        healthSlider.maxValue = playerHealth.maxHealth;
        UpdateHeartbeat();
    }

    private void Update()
    {
        UpdateHeartbeat();
        healthText.text = Mathf.RoundToInt(playerHealth.currentHealth).ToString();
    }

    void UpdateHeartbeat()
    {
        float healthPercent = playerHealth.currentHealth / maxHealth;
        healthSlider.value = playerHealth.currentHealth;
        Color targetColor;
        float targetSpeed;
        float targetYScale;

        if (healthPercent > 2f / 3f)
        {
            targetColor = greenColor;
            targetSpeed = greenSpeed;
            targetYScale = greenYScale;
        }
        else if (healthPercent > 1f / 3f)
        {
            targetColor = yellowColor;
            targetSpeed = yellowSpeed;
            targetYScale = yellowYScale;
        }
        else
        {
            targetColor = redColor;
            targetSpeed = redSpeed;
            targetYScale = redYScale;
        }

        heartbeatImage.color = targetColor;
        humanBodyUI.color = targetColor;
        heartbeatBG.color = new Color(
      targetColor.r,
      targetColor.g,
      targetColor.b,
      heartbeatBG.color.a
  );


        heartbeatBGStrip.color = new Color(
     targetColor.r,
     targetColor.g,
     targetColor.b,
     heartbeatBGStrip.color.a
 );


        healthText.color = targetColor;
        if (heartbeatAnimator != null)
            heartbeatAnimator.speed = targetSpeed;

        if (heartbeatRect != null)
        {
            Vector3 scale = heartbeatRect.localScale;
            scale.y = targetYScale;
            heartbeatRect.localScale = scale;
        }
    }
}