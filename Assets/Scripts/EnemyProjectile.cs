using QFSW.MOP2;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("References")]
    public Transform flame; // Reference to the flame GameObject

    [Header("Settings")]
    public float projSpeed = 5f; // Speed at which the GameObject moves away

    public ObjectPool bullet;

    public bool parryable;
    public Rigidbody enemyRigibody;

    [Header("World Collision")]
    [SerializeField, Min(0.01f)] private float collisionSweepRadius = 0.1f;
    [SerializeField] private LayerMask blockingLayers = ~0;

    private PlayerMovement playerMovement;

    public bool SpawnedProjectile;
 
    private void OnEnable()
    {
        playerMovement = FindObjectOfType<PlayerMovement>();
        rb = GetComponent<Rigidbody>();
        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider != null)
            collisionSweepRadius = Mathf.Max(0.01f, sphereCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z));
        //LeanTween.delayedCall(5f, () => { enemyRigibody.GetComponent<EnemyManager>().ResetParrayable(); });
    }
    public Transform eliminatorTarget;
    public float eliminatorSpeed = 20f;
    public float eliminatorTurnSpeed = 10f;

    Rigidbody rb;

    public float turnSpeed = 10f;
    void Update()
    {
        if (enemyRigibody != null && !enemyRigibody.GetComponent<EnemyManager>().projectile_Parried) return;
        // Keep flying forward
        //rb.velocity = transform.forward * eliminatorSpeed;

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
            Debug.Log("Work");
        }

        if (parryable) return;

        if (flame != null)
        {
            Vector3 movement = flame.forward * projSpeed * Time.deltaTime;
            if (HitsWorldBeforeMoving(movement))
            {
                ReleaseProjectile();
                return;
            }

            transform.position += movement;
        }
        else
        {
            Debug.LogWarning("Flame reference is missing!");
        }

       
    }
    public float playerDamage = 10f;
    public float forceMagnitude = 10f;
    void OnCollisionEnter(Collision collision)
    {
      
    }

    private bool HitsWorldBeforeMoving(Vector3 movement)
    {
        float distance = movement.magnitude;
        if (distance <= Mathf.Epsilon)
            return false;

        RaycastHit[] hits = Physics.SphereCastAll(
            transform.position,
            collisionSweepRadius,
            movement.normalized,
            distance,
            blockingLayers,
            QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            Collider hitCollider = hit.collider;
            if (hitCollider == null || hitCollider.transform.IsChildOf(transform))
                continue;

            // The bullet originates inside its shooter and player contact is
            // handled by OnTriggerEnter, so neither should count as a wall.
            if (hitCollider.CompareTag("Player") || hitCollider.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                continue;

            return true;
        }

        return false;
    }

    private void ReleaseProjectile()
    {
        if (!SpawnedProjectile && bullet != null)
            bullet.Release(gameObject);
        else
            gameObject.SetActive(false);
    }
    [SerializeField] GameObject Sender;
    public void HurtPlayer()
    {

        //PlayerHealth.instance.Damage(damageDone);
        bl_DamageInfo info = new bl_DamageInfo(playerDamage);
        info.Sender = Sender;
        Sender.SetIndicator();
        bl_DamageDelegate.OnDamageEvent(info);
    }
    public AudioSource attack3dAudioSource;
    public string enemyName;
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (!collision.GetComponent<PlayerMovement>().canKick || collision.GetComponent<PlayerMovement>().isDashing)
            {
                if(parryable)
                {
                    collision.GetComponent<PlayerMovement>().ParryProjectiles(this);
                }
               
            }
            else
            {
                Rigidbody playerRigidbody = collision.GetComponent<Rigidbody>();
                if (playerRigidbody != null)
                {
                    PlayerMovement pm = collision.gameObject.GetComponent<PlayerMovement>();

                    if (pm != null)
                    {
                        Vector3 dir = (collision.transform.position - transform.position).normalized;
                        dir.y = 0f;

                        Vector3 force = dir * 30f + Vector3.up * 2f;

                        pm.ApplyKnockback(force, 0.25f);
                    }

                    //Vector3 directionAway = (collision.transform.position - transform.position).normalized;
                    //playerRigidbody.AddForce(directionAway * forceMagnitude, ForceMode.Impulse);
                    SFXManager.Instance.PlaySFX(enemyName + "/" + "AttackHit", 1f);
                   
                    HurtPlayer();

                    collision.gameObject.GetComponent<PlayerHealth>().ManageHealth(-playerDamage);
                    collision.gameObject.GetComponent<PlayerHealth>().hit = true;
                    Debug.Log("playerHit");
                    if (!SpawnedProjectile)
                        bullet.Release(gameObject);
                    else
                        gameObject.SetActive(false);
                }

            }
        }
        if (collision.CompareTag("Untagged"))
        {
            //bullet.Release(gameObject);
            //Destroy(gameObject);
            if(!SpawnedProjectile)
                bullet.Release(gameObject);
            Debug.Log("Disable Bulet");
            //else
            //    gameObject.SetActive(false);
        }
        if (collision.CompareTag("Blood") &&  parryable && enemyRigibody.GetComponent<EnemyManager>().projectile_Parried)
        {
            
            Debug.Log("Hit Enemy");
            SFXManager.Instance.PlaySFX(enemyName + "/" + "AttackHit", 0.8f);
            
            RedVisionToggle.Instance.EnableRedVisionForSeconds(0.25f);
            //ImpactFrameEffect.Instance.GoBlack();
            LeanTween.delayedCall(0.2f, () => { ImpactFrameEffect.Instance.GoColor(); });
            collision.GetComponent<EnemyHealth>().EnemyHit(playerDamage);
            collision.GetComponent<EnemyManager>().ResetParrayable();
            enemyRigibody = null;
            playerMovement.playerKickedSomething = false;
            if (!SpawnedProjectile)
                bullet.Release(gameObject);

            else
                gameObject.SetActive(false);

        }

    }
   


}
