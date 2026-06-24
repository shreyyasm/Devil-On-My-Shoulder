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

    private PlayerMovement playerMovement;

    public bool SpawnedProjectile;
 
    private void OnEnable()
    {
        playerMovement = FindObjectOfType<PlayerMovement>();
        rb = GetComponent<Rigidbody>();
        //LeanTween.delayedCall(5f, () => { enemyRigibody.GetComponent<EnemyManager>().ResetParrayable(); });
    }
    public Transform eliminatorTarget;
    public float eliminatorSpeed = 20f;
    public float eliminatorTurnSpeed = 10f;

    Rigidbody rb;

    public float turnSpeed = 10f;
    void Update()
    {
        if (!enemyRigibody.GetComponent<EnemyManager>().projectile_Parried) return;
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
            // Calculate the direction away from the flame
            Vector3 directionAway = (transform.position - flame.position).normalized;

            // Move the GameObject away from the flame
            transform.position += flame.forward * projSpeed * Time.deltaTime;
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
    [SerializeField] GameObject Sender;
    public void HurtPlayer()
    {

        //PlayerHealth.instance.Damage(damageDone);
        bl_DamageInfo info = new bl_DamageInfo(playerDamage);
        info.Sender = Sender;
        Sender.SetIndicator();
        bl_DamageDelegate.OnDamageEvent(info);
    }
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Player"))
        {
            if(!collision.GetComponent<PlayerMovement>().isDashing)
            {
                Rigidbody playerRigidbody = collision.GetComponent<Rigidbody>();
                if (playerRigidbody != null)
                {
                    Vector3 directionAway = (collision.transform.position - transform.position).normalized;
                    playerRigidbody.AddForce(directionAway * forceMagnitude, ForceMode.Impulse);

                    ImpactFrameEffect.Instance.GoBlack();
                    LeanTween.delayedCall(0.2f, () => { ImpactFrameEffect.Instance.GoColor(); });
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
            else
            {
                if(parryable)
                {
                    collision.GetComponent<PlayerMovement>().ParryProjectiles(this);
                }
            }
            
        }
        if (collision.CompareTag("Untagged"))
        {
            //bullet.Release(gameObject);
            //Destroy(gameObject);
            if(!SpawnedProjectile)
                bullet.Release(gameObject);

            //else
            //    gameObject.SetActive(false);
        }
        if (collision.CompareTag("Blood") &&  parryable && enemyRigibody.GetComponent<EnemyManager>().projectile_Parried)
        {
            
            Debug.Log("Hit Enemy");
            ImpactFrameEffect.Instance.GoBlack();
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
