using UnityEngine;

[ExecuteInEditMode]
public class ImpactFrameEffect : MonoBehaviour
{
    public static ImpactFrameEffect Instance;
    public Material effectMaterial;

    [Range(0, 1)]
    public float effectAmount = 0f;

    private void Awake()
    {
        Instance = this;
    }
    public void TriggerImpact(float saturation, float contrast)
    {
       effectMaterial.SetFloat("_Saturation_Layers", saturation);
        effectMaterial.SetFloat("_Contrast", contrast);
    }
    public void GoBlack()
    {
        TriggerImpact(0, 1.5f);
    }
    public void GoColor()
    {
        TriggerImpact(1.2f, 1.05f);
    }
   
}
