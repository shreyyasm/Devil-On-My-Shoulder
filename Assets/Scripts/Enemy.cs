using QFSW.MOP2;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class Enemy : MonoBehaviour
{
    public static Enemy Instance;
    public GameObject player;
    public NavMeshAgent agent;
    public float movementSpeed;
    public PowerUpSelect powerUpSelect;
    [Header("EnemyHealth")]
    public float health = 100;
    public float maxHealth = 100;
    public float playerDamage = 10f;

    [Header("SFX")]
    public AudioClip destroySFX;
    public AudioClip[] BulletHitSFX;
    public AudioClip killSound;
    public AudioSource AttackAudioSource;


    [Header("ObjectPool")]
    public ObjectPool explosionPool;
    public ObjectPool batteryLife;
    public ObjectPool BloodPool;
   
    [Header("EnemyType")]
    public bool slashEnemy;
    public bool wanderShootEnemy;
    public bool ShootEnemy;
    public bool EyeEnemy;
    public bool selfDestruct;

    public bool RotateExternally;
    private void OnEnable()
    {
         health = maxHealth;
        spriteRenderer.color = Color.white;
    }
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        player = GameObject.FindGameObjectWithTag("Player");
        powerUpSelect = FindFirstObjectByType<PowerUpSelect>();

    }
    // Start is called before the first frame update
    void Start()
    {

        originalColor = spriteRenderer.color;
        if (agent == null) agent = GetComponent<NavMeshAgent>();

    }

    // Update is called once per frame
    void Update()
    {
        if (!attack)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < attackRange)
            {
                agent.enabled = true;
                attack = true;
                if (slashEnemy)
                    StartCoroutine(AttackLoop());

                if (wanderShootEnemy && agent.enabled)
                    StartCoroutine(WanderRoutine());
            }
        }

        // Get direction to player (ignore Y so it only rotates horizontally)
        Vector3 direction = player.transform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f) // Avoid errors if player is at same position
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            if(!RotateExternally)
                transform.rotation = targetRotation;
        }



        if (!attack) return;
 
        if(agent.enabled && !slashEnemy && !wanderShootEnemy && !ShootEnemy)
            agent.SetDestination(player.transform.position);

        if(wanderShootEnemy || ShootEnemy)
            HandleShooting();

        if(powerUpSelect.EnemyStill)
            agent.speed = 0;
        else
            agent.speed = movementSpeed;
    }

    public ObjectPool EnemyPool;
    public void EnemyHit(float damageValue)
    {
        health -= damageValue;

        BloodPool.GetObject().transform.position = transform.position;
        AudioSource.PlayClipAtPoint(BulletHitSFX[UnityEngine.Random.Range(0, 2)], transform.position, 1f);
        ScoreManager.Instance.RegisterShotHit();
        if (health <= 0)
        {

            GameObject explosion = explosionPool.GetObject();
            explosion.transform.position = transform.position + new Vector3(0, 0.6f, 0);

            //Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            ImpactFrameEffect.Instance.TriggerImpact(0, 1.2f);
            LeanTween.delayedCall(0.2f, () => { ImpactFrameEffect.Instance.TriggerImpact(1, 1.05f); });

            AudioSource.PlayClipAtPoint(destroySFX, transform.position, 1f);
            AudioSource.PlayClipAtPoint(killSound, Camera.main.transform.position, 1f);
            //Instantiate(coin, transform.position, Quaternion.identity);


            GameObject obj = batteryLife.GetObject();
            obj.transform.position = transform.position;

            //SFXManager.Instance.PlaySFX($"Enemy/Death", 1f);
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Give it an upward "pop"
                rb.AddForce(Vector3.up * 7, ForceMode.Impulse);
            }
            ScoreManager.Instance.Kills++;
            KillStreak.Instance.KilledEnemy();
            EnemyPool.Release(gameObject);
            //gameObject.SetActive(false);

        }
        if (health <= maxHealth / 1.5 && health >= maxHealth / 2.5)
            spriteRenderer.color = collisionColorYellow;

        else if (health <= maxHealth / 2.5)
            spriteRenderer.color = collisionColorRed;
        else
            spriteRenderer.color = Color.white;
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            
            health -= collision.gameObject.GetComponent<Projectile>().Damage(gameObject);
           
            BloodPool.GetObject().transform.position = transform.position;
            AudioSource.PlayClipAtPoint(BulletHitSFX[UnityEngine.Random.Range(0, 2)], transform.position, 1f);
            ScoreManager.Instance.RegisterShotHit();
            if (health <= 0)
            {
               
                GameObject explosion = explosionPool.GetObject();
                explosion.transform.position = transform.position + new Vector3(0, 0.6f, 0);
               
                //Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                ImpactFrameEffect.Instance.TriggerImpact(0,1.2f);
                LeanTween.delayedCall(0.2f, () => { ImpactFrameEffect.Instance.TriggerImpact(1, 1.05f); });

                AudioSource.PlayClipAtPoint(destroySFX, transform.position, 1f);
                AudioSource.PlayClipAtPoint(killSound, Camera.main.transform.position, 1f);
                //Instantiate(coin, transform.position, Quaternion.identity);


                GameObject obj = batteryLife.GetObject();
                obj.transform.position = transform.position;

                //SFXManager.Instance.PlaySFX($"Enemy/Death", 1f);
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // Give it an upward "pop"
                    rb.AddForce(Vector3.up * 7, ForceMode.Impulse);
                }
                ScoreManager.Instance.Kills++;
                KillStreak.Instance.KilledEnemy();
                EnemyPool.Release(gameObject);
                   //gameObject.SetActive(false);

            }

            if (health <= maxHealth / 1.5 && health >= maxHealth / 2.5)
                spriteRenderer.color = collisionColorYellow;

            else if (health <= maxHealth / 2.5)
                spriteRenderer.color = collisionColorRed;
            else
                spriteRenderer.color = Color.white;
        }
        if (collision.gameObject.CompareTag("Player") && !powerUpSelect.playerShielded)
        {
            if(!slashEnemy)
            {
                HurtPlayer();
                collision.gameObject.GetComponent<PlayerHealth>().ManageHealth(-playerDamage);
                collision.gameObject.GetComponent<PlayerHealth>().hit = true;

                if(EyeEnemy)
                    SFXManager.Instance.PlayNPC_SFX(AttackAudioSource, $"Enemy/EyeAttack", 1f);
                if (selfDestruct)
                {
                    //SFXManager.Instance.PlaySFX($"Enemy/Death", 1f);
                    SFXManager.Instance.PlayNPC_SFX(AttackAudioSource, $"Enemy/FireBallAttack", 1f);
                    GameObject explosion = explosionPool.GetObject();
                    explosion.transform.position = transform.position + new Vector3(0, 01f, 0);
                    gameObject.SetActive(false);
                   
                    AudioSource.PlayClipAtPoint(destroySFX, transform.position, 1f);

                }
                ImpactFrameEffect.Instance.TriggerImpact(0, 1.2f);
                LeanTween.delayedCall(0.2f, () => { ImpactFrameEffect.Instance.TriggerImpact(1, 1.05f); });
            }
            else
            {
                
                collision.gameObject.GetComponent<ExplosionScript>().Slash();

                if (PowerUpSelect.Instance.playerShielded) return;
                HurtPlayer();
                collision.gameObject.GetComponent<PlayerHealth>().ManageHealth(-playerDamage);
                collision.gameObject.GetComponent<PlayerHealth>().hit = true;

            }

        }
       
            
    }
    [Header("Enemy Health UI")]
    public SpriteRenderer spriteRenderer;
    public Color collisionColorRed = Color.red;
    public Color collisionColorYellow = Color.yellow;
    private Color originalColor;

    [SerializeField] GameObject Sender;
    public void HurtPlayer()
    {
       
        //PlayerHealth.instance.Damage(damageDone);
        bl_DamageInfo info = new bl_DamageInfo(playerDamage);
        info.Sender = Sender;
        Sender.SetIndicator();
        bl_DamageDelegate.OnDamageEvent(info);
        //CameraShake.Instance.shakeDuration += 0.3f;
    }
   
    [Header("Shoot Settings")]
    public float safeDistance = 5f; // Distance to maintain from the player
    public float shootTime = 3f; // Time between each shot
    public Transform shootPoint; // Point from which the projectile is instantiated
    private float shootTimer; // Timer to keep track of shooting intervals
    public ObjectPool enemies;
    private void HandleShooting()
    {
        shootTimer += Time.deltaTime;
        if (shootTimer >= shootTime)
        {
            shootTimer = 0f;
            float distance = Vector2.Distance(transform.position,player.transform.position );
            if (shootPoint && distance < 40)
            {
                // Instantiate the projectile and give it a direction
               
                GameObject enemy = enemies.GetObject(); 
                enemy.transform.position = shootPoint.position;
                if(ShootEnemy)
                    SFXManager.Instance.PlayNPC_SFX(AttackAudioSource, $"Enemy/SkeletonAttack", 1f);

                if(wanderShootEnemy)
                     SFXManager.Instance.PlayNPC_SFX(AttackAudioSource, $"Enemy/BatAttack", 1f);
                // make it face the player
                Vector3 direction = (player.transform.position - shootPoint.position).normalized;
                enemy.transform.rotation = Quaternion.LookRotation(direction + new Vector3(0,0.05f,0));
  
   
            }
        }
    }

    [Header("Wander Settings")]
    public float wanderRadius = 10f;
    public float waitTime = 3f; // gap between movements


    private IEnumerator WanderRoutine()
    {
        while (true)
        {
            // pick a random point on NavMesh within radius
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
            agent.SetDestination(newPos);

            // wait before picking a new spot
            yield return new WaitForSeconds(waitTime);

            // wait until agent has almost reached
            while (agent.pathPending || agent.remainingDistance > 1.5f)
            {
                yield return null;
            }
        }
    }

    // Generate random point on NavMesh within radius
    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layerMask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;

        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layerMask);

        return navHit.position;
    
    }

    [Header("Attack Settings")]
    public float attackRange = 2f;   // distance to attack
    public bool attack;


    private IEnumerator AttackLoop()
    {
        while (true)
        {

                // Apply impulse force in the forward direction
                Rigidbody rb = GetComponent<Rigidbody>();

                    //Debug.Log("Attack Player!");
                if (!powerUpSelect.EnemyStill)
                    rb.AddForce(rb.transform.forward * 20, ForceMode.VelocityChange);

                SFXManager.Instance.PlayNPC_SFX(AttackAudioSource, $"Enemy/VampireAttack", 1f);

                // Small pause for attack animation
                yield return new WaitForSeconds(waitTime);
                rb.velocity = Vector3.zero;
            

        }
    }
}
