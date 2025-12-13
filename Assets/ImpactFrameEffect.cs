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
    public void TriggerImpact(int value, float contrast)
    {
       effectMaterial.SetFloat("_Saturation_Layers", value);
        effectMaterial.SetFloat("_Contrast", contrast);
    }

   
}
