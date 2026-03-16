using QFSW.MOP2;
using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    public EnemyManager enemyManager;
    [Header("Stats Reference")]
    private EnemyStats enemyStats;

    [Header("Runtime Health Values")]
    public float maxHealth;
    public float health;
    public float gunHealth;

    [Header("Shield Settings")]
    public EnemyStats.ShieldType shieldType;
    public EnemyStats.DeathType deathType;
    public float shieldHealth;

    public bool isInvincible;
    private NavMeshAgent agent;

    [Header("Enemy Health UI")]
    public SpriteRenderer spriteRenderer;
    public Color collisionColorRed = Color.red;
    public Color collisionColorYellow = Color.yellow;
    private Color originalColor;



    [Header("ObjectPool")]
    public ObjectPool explosionPool;
    public ObjectPool BloodPool;
    public enum ShieldType { No, Yes }
    public enum DeathType { Simple, Explosion}

    public GameObject AfterDeath_ExplosionPrefab;

    public System.Action OnDeath;
    
    private void Awake()
    {
        enemyStats = enemyManager.enemyStats;
        agent = GetComponent<NavMeshAgent>();
    }
    private void Start()
    {
        if (enemyStats != null)
        {
            maxHealth = enemyStats.health;
            health = enemyStats.health;
            gunHealth = enemyStats.gunHealth;

            shieldType = enemyStats.shieldType;
            shieldHealth = enemyStats.shieldHealth;

            deathType = enemyStats.deathType;
            AfterDeath_ExplosionPrefab = enemyStats.AfterDeath_ExplosionPrefab;

            if (agent != null)
                agent.speed = enemyStats.speed;
        }
    }
    public void TakeDamage(float amount)
    {

        // If shield exists AND still has health
        if (shieldType == EnemyStats.ShieldType.Yes && shieldHealth > 0)
        {
            shieldHealth -= amount;

            // Handle overflow damage
            if (shieldHealth < 0)
            {
                float overflow = -shieldHealth;
                shieldHealth = 0;

                ApplyHealthDamage(overflow);
            }
        }
        else
        {
            ApplyHealthDamage(amount);
        }
    }
    void ApplyHealthDamage(float amount)
    {
        health -= amount;

        if (health <= 0)
        {
            health = 0;
            Die();
        }
    }
    void Die()
    {
        Transform explosionPoint = enemyManager.firePoint;
        GameObject explosion = explosionPool.GetObject();
        explosion.transform.position = explosionPoint.position + new Vector3(0, 0.6f, 0);

        ImpactFrameEffect.Instance.GoBlack();
        LeanTween.delayedCall(0.2f, () => { ImpactFrameEffect.Instance.GoColor(); });

        SFXManager.Instance.PlaySFX("Enemy/KillSound", 1f);
        SFXManager.Instance.PlaySFX("Enemy/Destroy", 1f);

        ScoreManager.Instance.Kills++;
        KillStreak.Instance.KilledEnemy();
        //EnemyPool.Release(gameObject);
        gameObject.SetActive(false);

        enemyManager.RemoveAllGuardEffects();
        OnDeath?.Invoke();

        if(deathType == EnemyStats.DeathType.Explosion)
        {
            Instantiate(AfterDeath_ExplosionPrefab,explosionPoint.position, Quaternion.identity);
        }

        Destroy(gameObject);

    }

    public void EnemyHit(float damageValue, bool HealthUI = false)
    {
        if (isInvincible)
            return;

        health -= damageValue;
        ScoreManager.Instance.RegisterShotHit();

        if (health <= 0)
        {
            Die();
        }
        if (!HealthUI && spriteRenderer != null)
        {
            if (health <= maxHealth / 1.5 && health >= maxHealth / 2.5)
                spriteRenderer.color = collisionColorYellow;

            else if (health <= maxHealth / 2.5)
                spriteRenderer.color = collisionColorRed;
            else
                spriteRenderer.color = Color.white;
        }

    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {

            if (isInvincible)
                return;

            health -= collision.gameObject.GetComponent<Projectile>().Damage(gameObject);
            SFXManager.Instance.PlaySFX("Enemy/BulletHit", 1f);
            ScoreManager.Instance.RegisterShotHit();

            if (health <= 0)
            {           
                Die();
            }

            if (spriteRenderer == null) return;

            if (health <= maxHealth / 1.5 && health >= maxHealth / 2.5)
                spriteRenderer.color = collisionColorYellow;

            else if (health <= maxHealth / 2.5)
                spriteRenderer.color = collisionColorRed;
            else
                spriteRenderer.color = Color.white;
        }
       
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            if (isInvincible)
                return;
            health -= collision.gameObject.GetComponent<Projectile>().Damage(gameObject);
            BloodPool.GetObject().transform.position = transform.position;
            SFXManager.Instance.PlaySFX("Enemy/BulletHit", 1f);
            ScoreManager.Instance.RegisterShotHit();
            if (health <= 0)
            {
                Die();
            }

            if (spriteRenderer == null) return;

            if (health <= maxHealth / 1.5 && health >= maxHealth / 2.5)
                spriteRenderer.color = collisionColorYellow;

            else if (health <= maxHealth / 2.5)
                spriteRenderer.color = collisionColorRed;
            else
                spriteRenderer.color = Color.white;
        }

    }
}