using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using UnityEngine.UI;

public class GamepadCursor : MonoBehaviour
{
    public static GamepadCursor Instance { get; private set; }

    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private RectTransform cursorTransform;

    public float cursorSpeed = 1000;
    private Mouse virtualMouse;
    bool previousMouseState;

    public RectTransform canvasRectTransform;
    public Canvas canvas;
    private Camera mainCamera;

    public float padding = 35f;

    const string gamepadScheme = "Gamepad";
    const string mouseScheme = "Keyboard&Mouse";
    public string previousControlScheme = "";

    Mouse currentMouse;

    [Header("Optional UI Selection")]
    public GraphicRaycaster canvasRaycaster;
    public bool clearSelectionWhenNotHovering = true;
    private PointerEventData pointerEventData;
    private EventSystem eventSystem;

    [Header("Behavior")]
    [Tooltip("If true, prints debug logs to help diagnose device presence (useful for WebGL).")]
    public bool enableDebugLogs = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        DisableCursor();
    }

    private void OnEnable()
    {
        mainCamera = Camera.main;
        currentMouse = Mouse.current;

        // Try to find an existing VirtualMouse first (avoid duplicates)
        foreach (var d in InputSystem.devices)
        {
            if (d is Mouse m && d.name == "VirtualMouse")
            {
                virtualMouse = m;
                if (enableDebugLogs) Debug.Log("[GamepadCursor] Found existing VirtualMouse device.");
                break;
            }
        }

        // If we didn't find one, try to add it safely
        if (virtualMouse == null)
        {
            try
            {
                virtualMouse = (Mouse)InputSystem.AddDevice("VirtualMouse");
                if (enableDebugLogs) Debug.Log("[GamepadCursor] Added VirtualMouse device.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[GamepadCursor] Could not create VirtualMouse: " + ex.Message);
                virtualMouse = null;
            }
        }
        else if (!virtualMouse.added)
        {
            try
            {
                InputSystem.AddDevice(virtualMouse);
            }
            catch { }
        }

        // Attempt pairing immediately if possible; otherwise start a short retry coroutine
        if (virtualMouse != null && playerInput != null)
        {
            TryPairVirtualMouse();
            SafeOnControlsChanged();
        }
        else
        {
            if (playerInput == null && enableDebugLogs) Debug.LogWarning("[GamepadCursor] playerInput is null on enable — will retry pairing shortly.");
            if (virtualMouse == null && enableDebugLogs) Debug.LogWarning("[GamepadCursor] virtualMouse is null on enable — pairing skipped.");
            StartCoroutine(TryPairingCoroutine(0.75f, 60)); // retry for up to ~0.75s or 60 frames
        }

        // initialize position from cursorTransform if possible and virtualMouse exists
        if (cursorTransform != null && virtualMouse != null)
        {
            Vector2 position = cursorTransform.anchoredPosition;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay && mainCamera != null)
            {
                Vector3 worldPos = cursorTransform.TransformPoint(cursorTransform.rect.center);
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(mainCamera, worldPos);
                InputState.Change(virtualMouse.position, screenPoint);
            }
            else
            {
                InputState.Change(virtualMouse.position, position);
            }
        }

        // Setup optional EventSystem/PointerEventData only if an EventSystem exists in scene
        eventSystem = EventSystem.current;
        if (eventSystem != null)
            pointerEventData = new PointerEventData(eventSystem);
        else
            pointerEventData = null; // do not create a new EventSystem (per request)

        if (canvas != null && canvasRectTransform == null)
            canvasRectTransform = canvas.GetComponent<RectTransform>();

        if (canvas != null && canvasRaycaster == null)
            canvasRaycaster = canvas.GetComponent<GraphicRaycaster>();

        InputSystem.onAfterUpdate += UpdateMotion;
        CheckForGamepadConnection();
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDisable()
    {
        // Do NOT remove virtual mouse here to avoid pairing issues across scenes.
        InputSystem.onAfterUpdate -= UpdateMotion;
        InputSystem.onDeviceChange -= OnDeviceChange;
        StopAllCoroutines();
    }

    // Coroutine that retries pairing for a short window in case playerInput appears a frame or two later
    private IEnumerator TryPairingCoroutine(float timeoutSeconds, int maxFrames)
    {
        float start = Time.realtimeSinceStartup;
        int frames = 0;

        while (Time.realtimeSinceStartup - start < timeoutSeconds && frames < maxFrames)
        {
            if (virtualMouse != null && playerInput != null)
            {
                TryPairVirtualMouse();
                SafeOnControlsChanged();
                yield break;
            }

            frames++;
            yield return null;
        }

        if (enableDebugLogs)
        {
            Debug.LogWarning($"[GamepadCursor] Pairing retry timed out. virtualMouse present:{(virtualMouse != null)}, playerInput present:{(playerInput != null)}");
        }
    }

    private void TryPairVirtualMouse()
    {
        if (virtualMouse == null || playerInput == null) return;
        try
        {
            InputUser.PerformPairingWithDevice(virtualMouse, playerInput.user);
            if (enableDebugLogs) Debug.Log("[GamepadCursor] Paired VirtualMouse with PlayerInput.user.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[GamepadCursor] Pairing VirtualMouse failed: " + ex.Message);
        }
    }

    private void UpdateMotion()
    {
        if (virtualMouse == null || Gamepad.current == null || cursorTransform == null)
            return;

        Vector2 deltaValue = Gamepad.current.rightStick.ReadValue();
        deltaValue *= cursorSpeed * Time.unscaledDeltaTime;

        Vector2 currentPosition;
        try
        {
            currentPosition = virtualMouse.position.ReadValue();
        }
        catch
        {
            // reading may fail in some runtime scenarios; bail safely
            return;
        }

        Vector2 newPosition = currentPosition + deltaValue;

        newPosition.x = Mathf.Clamp(newPosition.x, padding, Screen.width - padding);
        newPosition.y = Mathf.Clamp(newPosition.y, padding, Screen.height - padding);

        InputState.Change(virtualMouse.position, newPosition);
        InputState.Change(virtualMouse.delta, deltaValue);
        bool aButtonIsPressed = Gamepad.current.aButton.IsPressed();

        if (previousMouseState != aButtonIsPressed)
        {
            virtualMouse.CopyState<MouseState>(out var mouseState);
            mouseState.WithButton(MouseButton.Left, aButtonIsPressed);
            InputState.Change(virtualMouse, mouseState);
            previousMouseState = aButtonIsPressed;
        }

        AnchorCursor(newPosition);

        // Optional: update selection if raycaster & EventSystem available
        UpdateUISelection(newPosition);
    }

    private void AnchorCursor(Vector2 position)
    {
        if (cursorTransform == null)
            return;

        if (canvasRectTransform == null)
        {
            if (canvas != null)
                canvasRectTransform = canvas.GetComponent<RectTransform>();
            if (canvasRectTransform == null)
                return;
        }

        Camera cam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : mainCamera;
        Vector2 anchoredPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, position, cam, out anchoredPosition);
        cursorTransform.anchoredPosition = anchoredPosition;
    }

    // Renamed to SafeOnControlsChanged to avoid accidental calls to the unsafe version
    public void SafeOnControlsChanged()
    {
        // guard missing cursorTransform
        if (cursorTransform == null)
        {
            Debug.LogWarning("[GamepadCursor] cursorTransform is null in OnControlsChanged — aborting.");
            return;
        }

        // refresh device flags
        CheckForGamepadConnection();

        if (!isGamepadConnected)
        {
            // hide virtual cursor visuals & show OS cursor
            cursorTransform.gameObject.SetActive(false);
            Cursor.visible = true;

#if !UNITY_WEBGL
            // Warp is not supported on WebGL; only do this on native platforms and when currentMouse exists
            if (currentMouse != null && virtualMouse != null)
            {
                try
                {
                    currentMouse.WarpCursorPosition(virtualMouse.position.ReadValue());
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[GamepadCursor] WarpCursorPosition failed: " + ex.Message);
                }
            }
            else
            {
                if (enableDebugLogs)
                {
                    if (currentMouse == null) Debug.Log("[GamepadCursor] currentMouse is null (cannot warp).");
                    if (virtualMouse == null) Debug.Log("[GamepadCursor] virtualMouse is null (cannot warp).");
                }
            }
#else
            if (enableDebugLogs) Debug.Log("[GamepadCursor] Skipping WarpCursorPosition on WebGL (unsupported).");
#endif
            previousControlScheme = mouseScheme;
        }
        else
        {
            // show virtual cursor visuals & hide OS cursor
            cursorTransform.gameObject.SetActive(true);
            Cursor.visible = false;

            if (currentMouse != null && virtualMouse != null)
            {
                try
                {
                    var physPos = currentMouse.position.ReadValue();
                    InputState.Change(virtualMouse.position, physPos);
                    AnchorCursor(physPos);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[GamepadCursor] Setting virtual mouse position from currentMouse failed: " + ex.Message);
                }
            }
            else
            {
                // fallback: anchor from virtualMouse if present
                if (virtualMouse != null)
                {
                    try
                    {
                        var vmPos = virtualMouse.position.ReadValue();
                        AnchorCursor(vmPos);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("[GamepadCursor] Anchoring from virtualMouse failed: " + ex.Message);
                    }
                }
                else
                {
                    if (enableDebugLogs) Debug.Log("[GamepadCursor] No currentMouse and no virtualMouse available to position UI cursor.");
                }
            }

            previousControlScheme = gamepadScheme;
        }

        if (enableDebugLogs) LogDeviceState("SafeOnControlsChanged");
    }

    // Keep original method name for external calls but redirect to safe version
    public void OnControlsChanged()
    {
        SafeOnControlsChanged();
    }

    public void DisableCursor()
    {
        if (cursorTransform != null)
        {
            cursorTransform.gameObject.SetActive(false);
            Cursor.visible = false;
        }
    }

    public bool IsGamepadConnected()
    {
        return isGamepadConnected;
    }

    private void Update()
    {
        // intentionally empty
    }

    [Header("Status (Read-Only)")]
    [Tooltip("True if any Gamepad is connected, false if not.")]
    public bool isGamepadConnected = false;
    /// <summary>
    /// Checks if any gamepad is currently connected.
    /// You can call this manually if needed.
    /// </summary>
    public void CheckForGamepadConnection()
    {
        isGamepadConnected = Gamepad.current != null;
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device is Gamepad)
        {
            switch (change)
            {
                case InputDeviceChange.Added:
                    isGamepadConnected = true;
                    if (enableDebugLogs) Debug.Log($"🎮 Gamepad connected: {device.displayName}");
                    break;

                case InputDeviceChange.Removed:
                case InputDeviceChange.Disconnected:
                    // Check if any gamepads remain
                    isGamepadConnected = Gamepad.all.Count > 0;
                    if (enableDebugLogs) Debug.Log($"❌ Gamepad disconnected: {device.displayName}");
                    break;
            }

            // update pairing/visibility after device changes
            SafeOnControlsChanged();
        }
    }

    // Optional helper: update selected UI element so highlights persist (only if EventSystem exists)
    private void UpdateUISelection(Vector2 screenPosition)
    {
        if (eventSystem == null || pointerEventData == null || canvasRaycaster == null)
            return;

        pointerEventData.position = screenPosition;

        var results = new List<RaycastResult>();
        canvasRaycaster.Raycast(pointerEventData, results);

        GameObject topHit = null;
        if (results.Count > 0)
        {
            foreach (var r in results)
            {
                if (r.gameObject == null) continue;
                var selectable = r.gameObject.GetComponent<Selectable>();
                if (selectable != null && selectable.interactable && selectable.IsActive())
                {
                    topHit = r.gameObject;
                    break;
                }
                if (topHit == null) topHit = r.gameObject;
            }
        }

        if (topHit != null)
        {
            if (eventSystem.currentSelectedGameObject != topHit)
                eventSystem.SetSelectedGameObject(topHit);
        }
        else if (clearSelectionWhenNotHovering)
        {
            if (eventSystem != null && eventSystem.currentSelectedGameObject != null)
                eventSystem.SetSelectedGameObject(null);
        }
    }

    private void LogDeviceState(string context)
    {
        if (!enableDebugLogs) return;
        bool mouseCurrent = Mouse.current != null;
        bool vMouse = virtualMouse != null;
        bool pInput = playerInput != null;
        bool gpad = Gamepad.current != null;
        Debug.Log($"[GamepadCursor] ({context}) Mouse.current: {mouseCurrent}, VirtualMouse: {vMouse}, PlayerInput: {pInput}, Gamepad.current: {gpad}");
    }
}
