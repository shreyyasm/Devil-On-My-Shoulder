using UnityEngine;

public class KickAnimationEvents : MonoBehaviour
{
    private PlayerMovement playerMovement;

    private void Awake()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
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
}
