using UnityEngine;

public class KickAnimationEvents : MonoBehaviour
{
    private PlayerMovement playerMovement;
    public Camera playerCameraMain;

    [Header("Camera Slide FOV")]
    public float kickFOV = 80f;
    public float normalFOV = 60f;     // Default FOV
    public float fovSmoothSpeed = 5f; // Smooth speed
    public bool isKicking = false;

    private void Awake()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
    }
    private void Update()
    {
        float targetFOVKick = isKicking ? kickFOV : normalFOV;

        float newFOV = Mathf.Lerp(
            playerCameraMain.fieldOfView,
            targetFOVKick,
            Time.deltaTime * fovSmoothSpeed
        );

        if (Mathf.Abs(newFOV - targetFOVKick) < 0.05f)
        {
            newFOV = targetFOVKick;
        }

        playerCameraMain.fieldOfView = newFOV;
    }

    public void StartKickCameraTilt()
    {
        if (playerMovement != null)
            playerMovement.StartKickCameraTilt();
    }

    public void EndKickCameraTilt()
    {
        if (playerMovement != null)
            playerMovement.EndKickCameraTilt();
    }
    public  void KickOn()
    {
        isKicking = true;
    }
    public void KickOff()
    {
        isKicking = false;
    }
}
