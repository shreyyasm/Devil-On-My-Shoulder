using UnityEngine;

public class MaterialTilingAnimator : MonoBehaviour
{
    [Header("Target Renderer / Material")]
    public Renderer targetRenderer;

    private Material materialInstance;

    [Header("X Tiling Range")]
    public float minX = 0.2f;
    public float maxX = 1.0f;

    [Header("Y Tiling Range")]
    public float minY = 0.5f;
    public float maxY = 1.5f;

    [Header("Animation Speed")]
    public float speedX = 2f;
    public float speedY = 2.5f;

    void Start()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if (targetRenderer != null)
        {
            // Create a unique material instance so we don’t overwrite the shared material
            materialInstance = targetRenderer.material;
        }
        else
        {
            Debug.LogError("No Renderer assigned or found on this GameObject!");
        }
    }

    void Update()
    {
        if (materialInstance == null) return;

        // Smooth oscillation for X
        float tX = (Mathf.Sin(Time.time * speedX) + 1f) / 2f;
        float xValue = Mathf.Lerp(minX, maxX, tX);

        // Smooth oscillation for Y
        float tY = (Mathf.Sin(Time.time * speedY) + 1f) / 2f;
        float yValue = Mathf.Lerp(minY, maxY, tY);

        // Apply tiling
        materialInstance.mainTextureScale = new Vector2(xValue, yValue);
    }
}
