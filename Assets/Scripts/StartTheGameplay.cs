using UnityEngine;

public class StartTheGameplay : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (other.TryGetComponent(out PlayerHealth playerHealth))
        {
            playerHealth.isTick = true;
            BackgroundMusic.Instance?.StartGameplayMusic();
            MadDashTutorialDirector.Instance?.NotifyPlayerExitedSpawnRoom();
            enabled = false;
        }
    }
}
