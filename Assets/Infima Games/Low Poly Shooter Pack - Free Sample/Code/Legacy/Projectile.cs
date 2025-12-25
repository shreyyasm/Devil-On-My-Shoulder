using InfimaGames.LowPolyShooterPack;
using QFSW.MOP2;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.UIElements;
using static ExplosionSystem;
using static InfimaGames.LowPolyShooterPack.Weapon;
using static UnityEngine.EventSystems.EventTrigger;
using Random = UnityEngine.Random;

public class Projectile : MonoBehaviour {
    public enum SpecialBullets
    {
        Deafult,
        PeaPosion_Bullets,
        Motar_Bullets,
        Ricocheter,
        YourMom,
        Riftsaw,
        CryoShattergun,
        GiggleBlaster,
        Dragunfire,
        StickyVicky,
        Danceler,
        Eliminator

    }

    [Header("Special Bullets")]
    [SerializeField] private SpecialBullets specialBullets;

    [Range(3, 100)]
	[Tooltip("After how long time should the bullet prefab be destroyed?")]
	public float destroyAfter;

	public ObjectPool bloodPool;
	public ObjectPool bulletPool;

    public float bulletDamage = 10f;

    [Header("Mortar Bullet Settings")]
    [SerializeField] private GameObject mortarExplosionPrefab;

    [Header("Ricocheter Bullet Settings")]
    [SerializeField] private int maxRicochetCount = 1;
    [SerializeField] private float ricochetSpeedMultiplier = 1.15f; // 15% faster per bounce
    [SerializeField] private float maxRicochetSpeed = 80f;          // safety cap

    public int currentRicochetCount;

    [Header("Demo Bullet Settings")]
    public bool demoBullet;
	public float baseDemoDamage = 10f;
	public float multiplerDemoDamage = 10f;

    public GameObject impactPrefab;
    public bool ignorePrevRotation = false;

    [Header("Your Mom Bullet Settings")]
    [SerializeField] private GameObject FirePrefab;

    [Header("Riftsaw Bullet Settings")]
    [SerializeField] private float riftsawRotateSpeed = 360f;
    [SerializeField] private float riftsawDamageInterval = 1f;
    [SerializeField] private float riftsawDamagePerTick = 8f;
    [SerializeField] private float riftsawLifeTime = 4f;
    [SerializeField] private GameObject riftsawExplosionPrefab;

    [Header("Cryo Shattergun Settings")]
    [SerializeField] private float cryoFreezeDuration = 5f;
    [SerializeField] private Color cryoFreezeColor = new Color(0.4f, 0.8f, 1f);

    [Header("Giggle Blaster Settings")]
    [SerializeField] private GameObject giggleConfettiPrefab;
    [SerializeField] private GameObject giggleSmilePrefab;
    [SerializeField] private float giggleStunDuration = 3f;

    [Header("StickyVicky Settings")]
    [SerializeField] private GameObject stickyBombPrefab;
    [SerializeField] private float stickyBombAttachForce = 1000f;

    [Header("Danceler Settings")]
    [SerializeField] private float mouthMelterRotationSpeed = 360f; // degrees per second
    [SerializeField] private int mouthMelterRotations = 3;

    [Header("Eliminator (Homing Missile) Settings")]
    [SerializeField] private float eliminatorTurnSpeed = 8f;
    [SerializeField] private float eliminatorSpeed = 35f;
    [SerializeField] private float eliminatorTargetSearchRadius = 60f;

    [SerializeField] private float eliminatorHomingDelay = 1f;

    private float eliminatorTimer;
    private bool eliminatorHomingActive;


    private Transform eliminatorTarget;


    private bool riftsawActive;
    private Transform riftsawTarget;

    Rigidbody rb;
    private void Start ()
	{
       
		//Grab the game mode service, we need it to access the player character!
		var gameModeService = ServiceLocator.Current.Get<IGameModeService>();
		//Ignore the main player character's collision. A little hacky, but it should work.
		Physics.IgnoreCollision(gameModeService.GetPlayerCharacter().GetComponent<Collider>(), GetComponent<Collider>());
        currentRicochetCount = 0;


       
    }
    private void OnEnable()
    {
        rb = GetComponent<Rigidbody>();
        StartCoroutine(DestroyAfter());

        if (specialBullets == SpecialBullets.Riftsaw)
        {
            rb.isKinematic = false;
            ApplyRandomZRotation();
        }

        if (specialBullets == SpecialBullets.Eliminator)
        {
            eliminatorTarget = null;
            eliminatorTimer = 0f;
            eliminatorHomingActive = false;

            if (rb != null)
            {
                rb.velocity = transform.forward * eliminatorSpeed;
            }
        }

    }

    public void SetDamage(float damage)
	{
		bulletDamage = damage;
    }
    public float Damage(GameObject enemy = null)
	{
        if (demoBullet)
        {
            if (Weapon.Instance.damageMultipler < Weapon.Instance.maxDamageMultiplerLimit)
                Weapon.Instance.damageMultipler += multiplerDemoDamage;

            bulletDamage += Weapon.Instance.damageMultipler;
        }
        Debug.Log("Enemy Hit By " + bulletDamage + "Damage");


        if (specialBullets == SpecialBullets.PeaPosion_Bullets)
        {
            // Attach poison script if not already present
            if (enemy.GetComponent<PoisonDamage>() == null)
            {
                enemy.AddComponent<PoisonDamage>();
            }
        }

        if (specialBullets == SpecialBullets.CryoShattergun && enemy != null)
        {
            if (enemy.GetComponent<CryoFreeze>() == null)
            {
                CryoFreeze freeze = enemy.AddComponent<CryoFreeze>();
                freeze.Initialize(cryoFreezeDuration, cryoFreezeColor);
            }
        }
        if (specialBullets == SpecialBullets.GiggleBlaster && enemy != null)
        {
            // Spawn confetti
            if (giggleConfettiPrefab != null)
            {
                Instantiate(
                    giggleConfettiPrefab,
                    enemy.transform.position + Vector3.up * 2.5f,
                    Quaternion.identity
                );
            }

            // 🔁 RESET stun if already present
            ConfettiStun existingStun = enemy.GetComponent<ConfettiStun>();
            if (existingStun != null)
            {
                Destroy(existingStun);
            }

            // ✅ Add fresh stun
            ConfettiStun stun = enemy.AddComponent<ConfettiStun>();
            stun.Initialize(giggleStunDuration);
            stun.giggleSmilePrefab = giggleSmilePrefab;
        }



        return bulletDamage;
    }
    private void Update()
    {
        if (specialBullets == SpecialBullets.Riftsaw && riftsawActive)
        {
            transform.Rotate(Vector3.up, riftsawRotateSpeed * Time.deltaTime, Space.Self);
        }

        // 🎯 ELIMINATOR BEHAVIOR
        if (specialBullets == SpecialBullets.Eliminator && rb != null)
        {
            eliminatorTimer += Time.deltaTime;

            // ⏳ Still wandering
            if (!eliminatorHomingActive)
            {
                if (eliminatorTimer >= eliminatorHomingDelay)
                {
                    eliminatorHomingActive = true;
                    eliminatorTarget = FindNearestEnemy();
                }

                // Keep flying forward
                rb.velocity = transform.forward * eliminatorSpeed;
                return;
            }

            // 🚀 Homing phase
            if (eliminatorTarget != null)
            {
                Vector3 direction =
                    (eliminatorTarget.position - transform.position).normalized;

                Vector3 newDir = Vector3.Lerp(
                    rb.velocity.normalized,
                    direction,
                    eliminatorTurnSpeed * Time.deltaTime
                );

                rb.velocity = newDir * eliminatorSpeed;
                transform.rotation = Quaternion.LookRotation(rb.velocity);
            }
        }

    }

    //If the bullet collides with anything
    private void OnCollisionEnter (Collision collision)
	{
        if (specialBullets == SpecialBullets.Eliminator)
        {
            eliminatorHomingActive = false;
            eliminatorTarget = null;
        }


        if (collision.gameObject.layer != 16 && specialBullets != SpecialBullets.Eliminator)
        {
            ContactPoint contact = collision.contacts[0];
            Quaternion rot = Quaternion.FromToRotation(Vector3.forward, contact.normal);
            if (ignorePrevRotation)
            {
                rot = Quaternion.Euler(0, 0, 0);
            }
            Vector3 pos = contact.point;
            Instantiate(impactPrefab, pos, rot);
            bulletPool.Release(gameObject);
        }

        if (specialBullets == SpecialBullets.Danceler && collision.gameObject.layer == 16)
        {
            HandleMouthMelter(collision.gameObject);
            bulletPool.Release(gameObject);
            return;
        }



        if (specialBullets == SpecialBullets.StickyVicky)
        {
            SpawnStickyVicky(collision);
            return;
        }


        if (specialBullets == SpecialBullets.Riftsaw)
        {
            HandleRiftsawImpact(collision);
            return;
        }

        if (specialBullets == SpecialBullets.Ricocheter)
        {
            HandleRicochet(collision);
        }
        if (specialBullets == SpecialBullets.YourMom)
        {

            HandleMortarImpact(collision);
            ContactPoint contact = collision.contacts[0];

            Instantiate(
               FirePrefab,
                contact.point,
                Quaternion.identity
            );
        }
        if (specialBullets == SpecialBullets.Dragunfire)
        {
            ContactPoint contact = collision.contacts[0];

            Instantiate(
               FirePrefab,
                contact.point,
                Quaternion.identity
            );
        }
        if (specialBullets == SpecialBullets.Motar_Bullets)
        {
            HandleMortarImpact(collision);
        }

      

        //Ignore collisions with other projectiles.
        if (collision.gameObject.GetComponent<Projectile>() != null)
			return;


        if (demoBullet)
		{
            if (collision.gameObject.layer != 16)
            {
                Weapon.Instance.damageMultipler = 0;
                bulletDamage = baseDemoDamage;
				
            }
        }
		

		GameObject bloodSplatter;
		//If bullet collides with "Blood" tag
		if (collision.transform.tag == "Blood") 
		{
			bloodSplatter =  bloodPool.GetObject();
			bloodSplatter.transform.position = transform.position;
			bloodSplatter.transform.rotation = Quaternion.LookRotation(collision.contacts[0].normal);

			StartCoroutine(DisableSplat());
            bulletPool.Release(gameObject);
            //Destroy bullet object
            //Destroy(gameObject);
        }
		IEnumerator DisableSplat()
        {
			yield return new WaitForSeconds(2f);
			bloodPool.Release(bloodSplatter);
		}

	}
	private IEnumerator DestroyAfter () 
	{
		//Wait for set amount of time
		yield return new WaitForSeconds (destroyAfter);
        //Destroy bullet object
        if(specialBullets == SpecialBullets.Ricocheter)
            currentRicochetCount = 0;
        bulletPool.Release(gameObject);

    }
    private void HandleMortarImpact(Collision collision)
    {
        ContactPoint contact = collision.contacts[0];

        // Spawn explosion VFX
        if (mortarExplosionPrefab != null)
        {
            GameObject explsoion = 
            Instantiate(
                mortarExplosionPrefab,
                contact.point,
                Quaternion.identity
            );
            explsoion.GetComponent<ExplosionSystem>().systemType = SystemType.ExplosionOnly;
        }

    }
    private void HandleRicochet(Collision collision)
    {
        // If hit enemy → deal damage & destroy
        if (collision.gameObject.layer == 16)
        {
            Damage(collision.gameObject);
            bulletPool.Release(gameObject);
            currentRicochetCount = 0;
            return;
        }

        // If exceeded bounce count → destroy
        if (currentRicochetCount >= maxRicochetCount)
        {
            bulletPool.Release(gameObject);
            currentRicochetCount = 0;
            return;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
            return;

        // --- CURRENT SPEED ---
        float speed = rb.velocity.magnitude;

        // --- SPEED BOOST ---
        speed *= ricochetSpeedMultiplier;
        speed = Mathf.Min(speed, maxRicochetSpeed); // safety cap

        // --- REFLECT DIRECTION ---
        Vector3 incomingDir = rb.velocity.normalized;
        Vector3 normal = collision.contacts[0].normal;
        Vector3 reflectedDir = Vector3.Reflect(incomingDir, normal);

        // --- APPLY BOOSTED SPEED ---
        rb.velocity = reflectedDir * speed;

        currentRicochetCount++;

        // Impact VFX
        if (impactPrefab != null)
        {
            Instantiate(
                impactPrefab,
                collision.contacts[0].point,
                Quaternion.LookRotation(normal)
            );
        }

        Debug.Log($"Bounce {currentRicochetCount} | Speed: {speed}");
    }
    private IEnumerator RiftsawDamageCoroutine()
    {
        while (riftsawActive && riftsawTarget != null)
        {
            Enemy enemy = riftsawTarget.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.EnemyHit(riftsawDamagePerTick);
            }

            yield return new WaitForSeconds(riftsawDamageInterval);
        }
    }
    private IEnumerator RiftsawLifeCoroutine()
    {
        yield return new WaitForSeconds(riftsawLifeTime);

        if (riftsawExplosionPrefab != null)
        {
            Instantiate(
                riftsawExplosionPrefab,
                transform.position,
                Quaternion.identity
            );
        }

        CleanupRiftsaw();
    }
    private void CleanupRiftsaw()
    {
        riftsawActive = false;
        riftsawTarget = null;

        transform.SetParent(null);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        bulletPool.Release(gameObject);
    }

    private void HandleRiftsawImpact(Collision collision)
    {
        // Only stick to enemies
        if (collision.gameObject.layer == 16)
        {
           
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.isKinematic = true;
            }

            // Stick to enemy
            riftsawTarget = collision.transform;
            transform.SetParent(riftsawTarget);

            // Optional: offset slightly from surface
            transform.position = collision.contacts[0].point;

            riftsawActive = true;

            // Start behaviors
            if(gameObject.activeSelf)
            {
                StartCoroutine(RiftsawDamageCoroutine());
                StartCoroutine(RiftsawLifeCoroutine());
            }
          
        }
        else
        {
            Instantiate(
               riftsawExplosionPrefab,
               transform.position,
               Quaternion.identity
           );
          
            return;
        }

       
    }
    private void SpawnStickyVicky(Collision collision)
    {
        if (stickyBombPrefab == null)
            return;

        ContactPoint contact = collision.contacts[0];
        Transform enemy = collision.transform;



        // Face the surface normal
        Quaternion surfaceRotation = Quaternion.LookRotation(contact.normal);

        // Apply 90° X rotation offset
        Quaternion finalRotation = surfaceRotation * Quaternion.Euler(90f, 0f, 0f);

        GameObject bomb = Instantiate(
            stickyBombPrefab,
            contact.point,
            finalRotation
        );


        // 🔒 Parent to enemy (THIS is the key fix)
        bomb.transform.SetParent(enemy);

        // Disable bomb physics (parent-driven movement)
        Rigidbody bombRb = bomb.GetComponent<Rigidbody>();
        if (bombRb != null)
        {
            bombRb.isKinematic = true;
            bombRb.detectCollisions = false;
        }

        // Force sticky state
        StickyBomb sticky = bomb.GetComponent<StickyBomb>();
        if (sticky != null)
        {
            sticky.ForceStick();
        }
    }
    private void HandleMouthMelter(GameObject enemy)
    {
        if (enemy == null)
            return;

        // Prevent stacking
        if (enemy.GetComponent<DancelerEffect>() != null)
            return;

        DancelerEffect effect = enemy.AddComponent<DancelerEffect>();
        effect.Initialize(
            mouthMelterRotationSpeed,
            mouthMelterRotations
        );
    }
    private Transform FindNearestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            eliminatorTargetSearchRadius,
            1 << 16 // Enemy layer
        );

        float closestDist = Mathf.Infinity;
        Transform closest = null;

        foreach (Collider col in hits)
        {
            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = col.transform;
            }
        }

        return closest;
    }

    private void ApplyRandomZRotation()
    {
        float randomZ = Random.Range(0f, 360f);
        transform.rotation = Quaternion.Euler(
            transform.eulerAngles.x,
            transform.eulerAngles.y,
            randomZ
        );
    }
   

}