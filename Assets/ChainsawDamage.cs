using System.Collections;
using UnityEngine;

public class ChainsawDamageOverlap : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damagePerTick = 15f;
    [SerializeField] private float damageInterval = 0.25f;
    [SerializeField] private float hitRadius = 0.9f;
    public GameObject SawPoint;
    [SerializeField] private LayerMask enemyLayer;

    private Coroutine damageRoutine;
    private bool isActive;

    private void OnEnable()
    {
        isActive = true;
        damageRoutine = StartCoroutine(DamageLoop());
    }

    private void OnDisable()
    {
        isActive = false;

        if (damageRoutine != null)
            StopCoroutine(damageRoutine);
    }

    private IEnumerator DamageLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(damageInterval);

        while (isActive)
        {
            Debug.Log("work");
            Collider[] hits = Physics.OverlapSphere(
                SawPoint.transform.position,
                hitRadius,
                enemyLayer,
                QueryTriggerInteraction.Ignore
            );

            foreach (Collider c in hits)
            {
                Enemy enemy = c.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.EnemyHit(enemy.maxHealth);
                }
            }

            yield return wait;
        }
    }

    // 🔍 DEBUG
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(SawPoint.transform.position, hitRadius);
    }
}
