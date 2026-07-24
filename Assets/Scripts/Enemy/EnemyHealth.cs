using DamageNumbersPro;
using QFSW.MOP2;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;
public class EnemyHealth : MonoBehaviour
{
    public EnemyManager enemyManager;
    [Header("Stats Reference")]
    private EnemyStats enemyStats;

    public GameObject ragdoll;
    public Rigidbody ragdollRB;


    [Header("Runtime Health Values")]
    public float maxHealth;
    public float health;
    public float gunHealth;

    [Header("Shield Settings")]
    public EnemyStats.ShieldType shieldType;
    public EnemyStats.DeathType deathType;
    public float shieldHealth;
    public GameObject Shield;

    public bool isInvincible;
    private NavMeshAgent agent;

    [Header("Enemy Health UI")]
    public SpriteRenderer spriteRenderer;
    public Color collisionColorRed = Color.red;
    public Color collisionColorYellow = Color.yellow;
    private Color originalColor;



    [Header("ObjectPool")]
    public ObjectPool BloodPool;
    public enum ShieldType { No, Yes }
    public enum DeathType { Simple, Explosion }

    public ObjectPool AfterDeath_ExplosionPrefab;

    public bool dead;
    public bool finisherActive;
    public System.Action OnDeath;
    private AudioSource audioSource;

    private void Awake()
    {
        enemyStats = enemyManager.enemyStats;
        audioSource = GetComponent<AudioSource>();
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
    public void TakeDamage(float amount, DamageInfo info)
    {
        // Shield check
        if (shieldType == EnemyStats.ShieldType.Yes && shieldHealth > 0)
        {
            ShowShield(1f);
            if (Shield != null)
            {
                Shield.SetActive(true);
            }
               
            shieldHealth -= amount;
            //SFXManager.Instance.PlayNPC_SFX(audioSource, "Janitor/Shield", 1f);
            SFXManager.Instance.PlaySFX("Janitor/Shield", 1f);
            // Overflow damage goes to health
            if (shieldHealth < 0)
            {
                if(Shield!= null)
                    Shield.SetActive(false);
                float overflow = -shieldHealth;
                shieldHealth = 0;

                ApplyHealthDamage(overflow, info); // 🔥 pass info
            }
        }
        else
        {
            BloodPool.GetObject(transform.position, Quaternion.identity);
            ApplyHealthDamage(amount, info); // 🔥 pass info
        }
    }
    void ApplyHealthDamage(float amount, DamageInfo info)
    {
        health -= amount;

        if (health <= 0)
        {
            health = 0;
            //enemyManager.animator.SetTrigger("Death");


            //StartCoroutine(SelfKnockback(50));
            // 🔥 pass ALL info to Die
            Die(info.isEnvironmental, info.isFinisher, info.isAirKill, info.isHeadshot);

        }
    }
    private Coroutine shieldCoroutine;

    public void ShowShield(float duration)
    {
        if (shieldCoroutine != null)
        {
            StopCoroutine(shieldCoroutine);
        }

        shieldCoroutine = StartCoroutine(ShieldRoutine(duration));
    }

    private IEnumerator ShieldRoutine(float duration)
    {
        if (Shield != null)
            Shield.SetActive(true);

        yield return new WaitForSeconds(duration);

        if (Shield != null)
            Shield.SetActive(false);

        shieldCoroutine = null;
    }
    public IEnumerator SelfKnockback(float force)
    {
        if (ragdollRB == null)
        {
            //Debug.Log("No ragdoll RB");
            yield break;
        }

        //Debug.Log(ragdollRB.name);


        ragdollRB.isKinematic = false;
        ragdollRB.useGravity = true;

        Vector3 launchVelocity =
            -transform.forward * force;

        // Debug.Log(launchVelocity);

        ragdollRB.velocity = launchVelocity;
    }
    public void Die(bool isEnvironmental, bool isFinisher, bool isAirKill, bool isHeadshot)
    {
        dead = true;
        Transform bloodPoint = transform;
        GameObject bloodBlast = enemyManager.After_BloodExplosion.GetObject();
        bloodBlast.transform.position = bloodPoint.position + new Vector3(0, 0.6f, 0);

        if(!isFinisher)
        {
            Transform explosionPoint = enemyManager.firePoint;
            GameObject explosionBlast = enemyManager.After_DeathExplosion.GetObject();
            explosionBlast.transform.position = explosionPoint.position + new Vector3(0.5f, 0.6f, 0);
        }
      


        RedVisionToggle.Instance.EnableRedVisionForSeconds(0.25f);
        ///ImpactFrameEffect.Instance.GoBlack();
        //LeanTween.delayedCall(0.2f, () => { ImpactFrameEffect.Instance.GoColor(); });

        SFXManager.Instance.PlaySFX("Enemy/KillSound", 1f);
        AudioClip clip = SFXManager.Instance.GetAudioClip("Enemy/Destroy");
        Play3DDeathSound(clip, transform.position);
        //SFXManager.Instance.PlayNPC_SFX(audioSource,"Enemy/Destroy", 1f);
        //SFXManager.Instance.PlayNPC_SFX(audioSource, enemyManager.enemyName + "/" + enemyManager.attackTryKey, 1f);
        //SFXManager.Instance.PlaySFX("Enemy/Destroy", 1f);

        ScoreManager.Instance.Kills++;


        CharacterAbilities.instance.AddKillValue(20);

        // Spawn damage number
        string localizedKilled =
        UnityEngine.Localization.Settings.LocalizationSettings
        .StringDatabase
        .GetLocalizedString("Kill Meter", "KilledText");

        DamageNumber killText = KillTextPrefab.Spawn(
            transform.position + new Vector3(0, 2.5f, 0),
            localizedKilled,
            transform
        );

        killText.UpdateText();

        //Debug.Log("Killed");
        //EnemyPool.Release(gameObject);



        ComboManager.Instance.RegisterKill(EnemyType.Basic, isEnvironmental, isFinisher, isAirKill, isHeadshot);


        enemyManager.RemoveAllGuardEffects();
        OnDeath?.Invoke();

        if (deathType == EnemyStats.DeathType.Explosion)
        {
           // AfterDeath_ExplosionPrefab.GetObject(explosionPoint.position, Quaternion.identity);
        }
        if (isHeadshot)
        {
            enemyManager.animator.SetBool("Headshot", true);
            agent.isStopped = true;
            agent.ResetPath();                 // 🔥 removes path completely
            agent.velocity = Vector3.zero;
            LeanTween.delayedCall(1.8f, () => { gameObject.SetActive(false); });
        }
        else
        {
            StartCoroutine(SelfKnockback(100));
            gameObject.SetActive(false);
            if (ragdoll != null && !isFinisher)
            {
                ragdoll.SetActive(true);
                ragdoll.transform.SetParent(null);
            }


        }
        //Destroy(gameObject);

    }
    public void Play3DDeathSound(AudioClip clip, Vector3 position)
    {
        GameObject audioObj = new GameObject("DeathSFX");

        audioObj.transform.position = position;

        AudioSource source = audioObj.AddComponent<AudioSource>();

        source.clip = clip;

        // Your settings
        source.spatialBlend = 1f;
        source.volume = 1f;
        source.pitch = 1f;
        source.priority = 128;

        source.dopplerLevel = 1f;
        source.spread = 0f;

        source.rolloffMode = AudioRolloffMode.Linear;

        source.minDistance = 0.6f;
        source.maxDistance = 15f;

        source.Play();

        Destroy(audioObj, clip.length);
    }
    public void FinisherActive()
    {
        finisherActive = true;
        agent.isStopped = true;
        agent.ResetPath();                 // 🔥 removes path completely
        agent.velocity = Vector3.zero;
        enemyManager.enabled = false;

    }
    public void FinisherAnimation()
    {
        enemyManager.animator.SetBool("Finisher", true);
    }
    public void EnemyHit(float damageValue, bool HealthUI = false)
    {
        if (isInvincible)
            return;

        if (dead) return;
        float damage = damageValue;

        DamageInfo info = new DamageInfo();

        // Apply damage with info
        TakeDamage(damage, info);

        // Spawn damage number
        DamageNumber damageNumber = DamageNumberPrefab.Spawn(
            transform.position + new Vector3(1, 2f, 0),
            damage,
            transform
        );
        
        SFXManager.Instance.PlaySFX("Enemy/BulletHit", 1f);
        ScoreManager.Instance.RegisterShotHit();

        if (spriteRenderer == null) return;

        if (health <= maxHealth / 1.5 && health >= maxHealth / 2.5)
            spriteRenderer.color = collisionColorYellow;

        else if (health <= maxHealth / 2.5)
            spriteRenderer.color = collisionColorRed;
        else
            spriteRenderer.color = Color.white;
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
    public DamageNumber DamageNumberPrefab;
    public DamageNumber KillTextPrefab;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            if (isInvincible)
                return;

            if (dead) return;
            float damage = collision.gameObject.GetComponent<Projectile>().Damage(gameObject);

            DamageInfo info = new DamageInfo();
            // 🔥 Detect headshot
            if (collision.GetContact(0).thisCollider.CompareTag("Head"))
            {
                damage *= 2f;
                info.isHeadshot = true;
                Debug.Log("HEADSHOT!");
            }

            // Apply damage with info
            TakeDamage(damage, info);

            // Spawn damage number
            DamageNumber damageNumber = DamageNumberPrefab.Spawn(
                transform.position + new Vector3(1, 2f, 0),
                damage,
                transform
            );
           
            SFXManager.Instance.PlaySFX("Enemy/BulletHit", 1f);
            ScoreManager.Instance.RegisterShotHit();

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

            if (dead) return;

            float damage = collision.gameObject.GetComponent<Projectile>().Damage(gameObject);

            DamageInfo info = new DamageInfo();

            // Apply damage with info
            TakeDamage(damage, info);

            // Spawn damage number
            DamageNumber damageNumber = DamageNumberPrefab.Spawn(
                transform.position + new Vector3(1, 2f, 0),
                damage,
                transform
            );
            
            SFXManager.Instance.PlaySFX("Enemy/BulletHit", 1f);
            ScoreManager.Instance.RegisterShotHit();

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
public struct DamageInfo
{
    public bool isHeadshot;
    public bool isAirKill;
    public bool isFinisher;
    public bool isEnvironmental;
}