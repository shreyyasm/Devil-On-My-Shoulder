using UnityEngine;

/// <summary>
/// Keeps the first-person hand camera at the same field of view as the world
/// camera. The settings screen changes the world camera's FOV, and without
/// this sync the hands appear much closer after applying a setting.
/// </summary>
public class FirstPersonHandCameraFovSync : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Camera handCamera;

    private void LateUpdate()
    {
        if (worldCamera == null || handCamera == null)
            return;

        if (!Mathf.Approximately(handCamera.fieldOfView, worldCamera.fieldOfView))
            handCamera.fieldOfView = worldCamera.fieldOfView;
    }
}
