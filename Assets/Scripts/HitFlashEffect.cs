using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach this to your enemy/character GameObject (the one with the SkinnedMeshRenderer
/// or MeshRenderer). Assign your "Custom_HitFlash" material (set its Flash Amount to 1
/// so it's permanently plain white) into the Flash Material field below.
///
/// IMPORTANT: Do NOT add the flash material as an extra element in the renderer's
/// Materials list in the Inspector. Leave your normal materials (Element 0, Element 1,
/// etc.) exactly as they are — this script temporarily swaps the whole materials array
/// out and back in via code, so your character keeps its normal lit look the rest
/// of the time.
///
/// Call TriggerHitFlash() from anywhere (damage handler, event, animation event, etc.)
/// to instantly flash the object plain white for a short duration.
///
/// Example:
///     GetComponent<HitFlashEffect>().TriggerHitFlash();
///     // or with a custom duration:
///     GetComponent<HitFlashEffect>().TriggerHitFlash(0.5f);
/// </summary>
[DisallowMultipleComponent]
public class HitFlashEffect : MonoBehaviour
{
    [Header("Flash Settings")]
    [Tooltip("The plain-white material (using the Custom/HitFlash shader, Flash Amount = 1).")]
    [SerializeField] private Material flashMaterial;

    [Tooltip("How long the object stays fully white before returning to normal.")]
    [SerializeField] private float flashDuration = 1f;

    private Renderer[] renderers;
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private Coroutine flashRoutine;

    private void Awake()
    {
        // GetComponentsInChildren so this also works on characters made of multiple
        // meshes/renderers (e.g. a body + weapon + accessories).
        renderers = GetComponentsInChildren<Renderer>();

        foreach (var r in renderers)
        {
            // Cache each renderer's original material array so we can restore it later.
            originalMaterials[r] = r.sharedMaterials;
        }
    }

    /// <summary>
    /// Triggers the hit flash using the duration configured in the inspector.
    /// Call this from your damage/hit-handling code.
    /// </summary>
    public void TriggerHitFlash()
    {
        TriggerHitFlash(flashDuration);
    }

    /// <summary>
    /// Triggers the hit flash with a custom hold duration (in seconds).
    /// </summary>
    public void TriggerHitFlash(float duration)
    {
        if (flashMaterial == null)
        {
            Debug.LogWarning($"{name}: HitFlashEffect has no Flash Material assigned.", this);
            return;
        }

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine(duration));
    }

    private IEnumerator FlashRoutine(float duration)
    {
        ApplyFlashMaterial();

        yield return new WaitForSeconds(duration);

        RestoreOriginalMaterials();
        flashRoutine = null;
    }

    private void ApplyFlashMaterial()
    {
        foreach (var r in renderers)
        {
            if (r == null) continue;

            int slotCount = originalMaterials[r].Length;
            Material[] flashArray = new Material[slotCount];
            for (int i = 0; i < slotCount; i++)
                flashArray[i] = flashMaterial;

            r.materials = flashArray; // every slot becomes the plain white material
        }
    }

    private void RestoreOriginalMaterials()
    {
        foreach (var r in renderers)
        {
            if (r == null) continue;
            r.materials = originalMaterials[r];
        }
    }

    // Optional: cancel a flash early, e.g. if the character dies mid-flash.
    public void CancelFlash()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
        RestoreOriginalMaterials();
    }
}