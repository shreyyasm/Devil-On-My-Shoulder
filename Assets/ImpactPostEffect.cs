using UnityEngine;

[ExecuteInEditMode]
public class ImpactPostEffect : MonoBehaviour
{
    public Material effectMat;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (effectMat != null)
            Graphics.Blit(src, dest, effectMat);
        else
            Graphics.Blit(src, dest);
    }
}
