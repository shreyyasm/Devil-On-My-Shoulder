using UnityEngine;

public class StartTheGameplay : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // The player controller can enter through an untagged child collider.
        // Resolve the health component from its root so this gameplay-start
        // trigger always resumes the survival drain after the spawn lift.
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.ResumeHealthDrain();
            BackgroundMusic.Instance?.StartGameplayMusic();
            MadDashTutorialDirector.Instance?.NotifyPlayerExitedSpawnRoom();
            enabled = false;
        }
    }
}
