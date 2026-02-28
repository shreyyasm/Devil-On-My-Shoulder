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
    public float shieldHealth;

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
        OnDeath?.Invoke();
        Destroy(gameObject);
    }

    public void EnemyHit(float damageValue, bool HealthUI = false)
    {
        health -= damageValue;

        BloodPool.GetObject().transform.position = transform.position;

        ScoreManager.Instance.RegisterShotHit();
        if (health <= 0)
        {

            GameObject explosion = explosionPool.GetObject();
            explosion.transform.position = transform.position + new Vector3(0, 0.6f, 0);

            //Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            ImpactFrameEffect.Instance.GoBlack();
            LeanTween.delayedCall(0.2f, () =>
            {
                ImpactFrameEffect.Instance.GoColor();
            }).setIgnoreTimeScale(true);


            SFXManager.Instance.PlaySFX("Enemy/KillSoud", 1f);
            SFXManager.Instance.PlaySFX("Enemy/Destory", 1f);
   
          
            
            ////SFXManager.Instance.PlaySFX($"Enemy/Death", 1f);
            //Rigidbody rb = obj.GetComponent<Rigidbody>();
            //if (rb != null)
            //{
            //    // Give it an upward "pop"
            //    rb.AddForce(Vector3.up * 7, ForceMode.Impulse);
            //}
            ScoreManager.Instance.Kills++;
            KillStreak.Instance.KilledEnemy();
            //EnemyPool.Release(gameObject);
            gameObject.SetActive(false);
            Die();

        }
        if (!HealthUI)
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

            health -= collision.gameObject.GetComponent<Projectile>().Damage(gameObject);

            BloodPool.GetObject().transform.position = transform.position;
            SFXManager.Instance.PlaySFX("Enemy/BulletHit", 1f);
            ScoreManager.Instance.RegisterShotHit();
            if (health <= 0)
            {

                GameObject explosion = explosionPool.GetObject();
                explosion.transform.position = transform.position + new Vector3(0, 0.6f, 0);

                //Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                ImpactFrameEffect.Instance.GoBlack();
                LeanTween.delayedCall(0.2f, () => { ImpactFrameEffect.Instance.GoColor(); });

                SFXManager.Instance.PlaySFX("Enemy/KillSoud", 1f);
                SFXManager.Instance.PlaySFX("Enemy/Destory", 1f);

                //Instantiate(coin, transform.position, Quaternion.identity);


                //GameObject obj = batteryLife.GetObject();
                //obj.transform.position = transform.position;

                //SFXManager.Instance.PlaySFX($"Enemy/Death", 1f);
                //Rigidbody rb = obj.GetComponent<Rigidbody>();
                //if (rb != null)
                //{
                //    // Give it an upward "pop"
                //    rb.AddForce(Vector3.up * 7, ForceMode.Impulse);
                //}
                ScoreManager.Instance.Kills++;
                KillStreak.Instance.KilledEnemy();
                //EnemyPool.Release(gameObject);
                gameObject.SetActive(false);
                Die();
            }

            if (health <= maxHealth / 1.5 && health >= maxHealth / 2.5)
                spriteRenderer.color = collisionColorYellow;

            else if (health <= maxHealth / 2.5)
                spriteRenderer.color = collisionColorRed;
            else
                spriteRenderer.color = Color.white;
        }
       
    }

   
}