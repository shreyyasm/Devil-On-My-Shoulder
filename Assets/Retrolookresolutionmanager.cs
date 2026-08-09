using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps the "Retro Look" Render Texture matched to the actual screen/window resolution
/// at runtime, so the dither/halftone shader always maps 1 dither texel to exactly
/// 1 real output pixel — no downscaling, no aliasing, regardless of what resolution
/// the game is running at (1920x1080, 1280x720, phone screens, etc).
///
/// The shader's Tiling And Offset node already reads the Render Texture's live size
/// (Width / Height) to compute the dither tiling, so no shader changes are needed —
/// this script just needs to give it a Render Texture of the right size.
///
/// USAGE:
/// - Runs automatically once on Start().
/// - Call RefreshRenderTexture() manually from your Settings menu (or anywhere else)
///   whenever the resolution changes — e.g. after the player picks a new resolution
///   from a dropdown and calls Screen.SetResolution(...).
/// </summary>
[DisallowMultipleComponent]
public class RetroLookResolutionManager : MonoBehaviour
{
    [Header("Camera that renders INTO the Retro Look Render Texture")]
    [Tooltip("The camera whose 'Output Texture' field currently points at the Retro Look Render Texture asset (e.g. Camera_1 / SM).")]
    public Camera targetCamera;

    [Header("Material using the RetroLook Shader Graph")]
    [Tooltip("The material on your RawImage (e.g. 'Shader Graphs_RetroLook').")]
    public Material retroLookMaterial;

    [Tooltip("The Reference name (NOT the display name) of the 'Render Texture' property in the Shader Graph Blackboard. Click the property in the Blackboard and check the 'Reference' field in the graph inspector to find this.")]
    public string renderTexturePropertyName = "_MainTex";

    [Header("Optional: also update a RawImage directly")]
    [Tooltip("Optional. If your RawImage's Texture field also needs to stay in sync, assign it here.")]
    public RawImage targetRawImage;

    [Header("Render Texture Format")]
    [Tooltip("ARGB64 = 16 bits per channel UNORM, closest built-in match to R16G16B16A16_UNORM used in your original asset.")]
    public RenderTextureFormat colorFormat = RenderTextureFormat.ARGB64;

    [Tooltip("24 = 24-bit depth + 8-bit stencil, matching D32_SFLOAT_S8_UINT closely enough for most setups. Use 32 if you specifically need a 32-bit depth buffer.")]
    public int depthBits = 24;

    [Tooltip("Clamp the smallest dimension the Render Texture is allowed to be, to avoid crashes if the window is minimized (0 width/height).")]
    public int minDimension = 2;

    private RenderTexture _currentRT;
    private int _lastWidth = -1;
    private int _lastHeight = -1;

    private void Start()
    {
        RefreshRenderTexture();
    }

    /// <summary>
    /// Recreates the Render Texture to match the CURRENT Screen.width / Screen.height
    /// exactly, and reassigns it to the camera, material, and (optionally) RawImage.
    /// The camera's field of view adapts to each aspect ratio automatically, so nothing
    /// gets stretched/squeezed — wider screens simply reveal more of the scene left-right,
    /// taller screens reveal more top-bottom. No bars, no cropping, no distortion.
    /// Call this manually whenever resolution changes at runtime — e.g. right after
    /// Screen.SetResolution(...) in your Settings menu.
    /// </summary>
    public void RefreshRenderTexture()
    {
        int width = Mathf.Max(Screen.width, minDimension);
        int height = Mathf.Max(Screen.height, minDimension);
        CreateAndAssignRenderTexture(width, height);
    }

    /// <summary>
    /// Same as RefreshRenderTexture(), but lets you pass an explicit resolution instead
    /// of reading Screen.width/height — useful if you want to call this the same frame
    /// you call Screen.SetResolution() before the Screen values have actually updated.
    /// </summary>
    public void RefreshRenderTexture(int width, int height)
    {
        width = Mathf.Max(width, minDimension);
        height = Mathf.Max(height, minDimension);
        CreateAndAssignRenderTexture(width, height);
    }

    private void CreateAndAssignRenderTexture(int width, int height)
    {
        // Skip if nothing actually changed
        if (_currentRT != null && width == _lastWidth && height == _lastHeight)
            return;

        RenderTexture oldRT = _currentRT;

        // Explicitly match the project's color space so the RT's gamma handling is
        // identical every time it's recreated — without this, Unity's inferred default
        // can vary slightly and cause a faint color/brightness shift on resolution change.
        RenderTextureReadWrite readWrite = QualitySettings.activeColorSpace == ColorSpace.Linear
            ? RenderTextureReadWrite.Linear
            : RenderTextureReadWrite.sRGB;

        RenderTexture newRT = new RenderTexture(width, height, depthBits, colorFormat, readWrite)
        {
            name = "RetroLook_RT_Runtime",
            filterMode = FilterMode.Point,   // must stay Point — crisp dither dots
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false,
            antiAliasing = 1
        };
        newRT.Create();

        // Point the camera's output at the new RT
        if (targetCamera != null)
        {
            targetCamera.targetTexture = newRT;

            // IMPORTANT: if camera.aspect was ever set manually anywhere in the project
            // (common for FOV tweaks), Unity locks it and stops auto-syncing to the
            // target texture's shape. That mismatch is what causes the scene content
            // itself to look stretched/squeezed after a resolution change. ResetAspect()
            // clears that lock so the camera always matches the new RT's aspect exactly.
            targetCamera.ResetAspect();
        }

        // Point the shader's "Render Texture" input at the new RT
        if (retroLookMaterial != null)
        {
            if (retroLookMaterial.HasProperty(renderTexturePropertyName))
            {
                retroLookMaterial.SetTexture(renderTexturePropertyName, newRT);
            }
            else
            {
                Debug.LogWarning(
                    $"[RetroLookResolutionManager] Material '{retroLookMaterial.name}' has no property " +
                    $"named '{renderTexturePropertyName}'. Check the Reference name in the Shader Graph Blackboard.");
            }
        }

        // Optional: keep a RawImage's Texture field in sync too
        if (targetRawImage != null)
        {
            targetRawImage.texture = newRT;
        }

        _currentRT = newRT;
        _lastWidth = width;
        _lastHeight = height;

        // Release the previous RT now that nothing still points at it
        if (oldRT != null)
        {
            oldRT.Release();
            Destroy(oldRT);
        }
    }

    private void OnDestroy()
    {
        if (_currentRT == null) return;

        if (targetCamera != null && targetCamera.targetTexture == _currentRT)
            targetCamera.targetTexture = null;

        _currentRT.Release();
        Destroy(_currentRT);
    }
}