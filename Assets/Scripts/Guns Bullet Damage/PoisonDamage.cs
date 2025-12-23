using System.Collections;
using UnityEngine;

public class PoisonDamage : MonoBehaviour
{
    [SerializeField] private float duration = 3f;
    [SerializeField] private float tickInterval = 1f;
    [SerializeField] private float tickDamage = 20f;

    private Coroutine poisonRoutine;

    private void OnEnable()
    {
        poisonRoutine = StartCoroutine(PoisonRoutine());
    }

    private IEnumerator PoisonRoutine()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            yield return new WaitForSeconds(tickInterval);

            // Damage method on enemy
            GetComponent<Enemy>().health -= tickDamage;
            elapsed += tickInterval;

            Debug.Log("damage depletion");
        }

        Destroy(this); // 🔥 remove script after completion
    }
}
