using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    public EnemyManager enemyManager;
    [Header("Stats Reference")]
    private EnemyStats enemyStats;

    [Header("Runtime Health Values")]
    public float health;
    public float gunHealth;
    public ShieldType shieldType;
    public float shieldHealth;

    private NavMeshAgent agent;

    public enum ShieldType
    {
        No,
        Yes
    }

    private void Awake()
    {
        enemyStats = enemyManager.enemyStats;
        agent = GetComponent<NavMeshAgent>();
    }
    private void Start()
    {
        if (enemyStats != null)
        {
            health = enemyStats.health;
            gunHealth = enemyStats.gunHealth;
            shieldHealth = enemyStats.shieldHealth;
            agent.speed = enemyStats.speed;
        }
    }

    public void TakeDamage(float amount)
    {
        if (shieldHealth > 0)
        {
            shieldHealth -= amount;
            if (shieldHealth < 0)
                shieldHealth = 0;
        }
        else
        {
            health -= amount;
            if (health <= 0)
                Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }
}