using QFSW.MOP2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Goli : MonoBehaviour
{
    [Header("References")]
    public Transform flame; // Reference to the flame GameObject

    [Header("Settings")]
    public float projSpeed = 5f; // Speed at which the GameObject moves away

    public ObjectPool bullet;

    public bool parryable;
    public Rigidbody enemyRigibody;

    private PlayerMovement playerMovement;
    private void Awake()
    {
        playerMovement = FindObjectOfType<PlayerMovement>();
    }
    void Update()
    {
        if(parryable) return;

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
        if (collision.CompareTag("Player") && !collision.GetComponent<PlayerMovement>().playerKickedSomething )
        {
            //if (collision.GetComponent<PlayerMovement>().kickAnimator[PlayerCharacterData.Instance.characterIndex - 1].GetComponent<KickAnimationEvents>().isKicking) return;

            Rigidbody playerRigidbody = collision.GetComponent<Rigidbody>();
            if (playerRigidbody != null)
            {
                Vector3 directionAway = (collision.transform.position - transform.position).normalized;
                playerRigidbody.AddForce(directionAway * forceMagnitude, ForceMode.Impulse);
                if (collision.gameObject.CompareTag("Player") && !PowerUpSelect.Instance.playerShielded)
                {
                    ImpactFrameEffect.Instance.GoBlack();
                    LeanTween.delayedCall(0.2f, () => { ImpactFrameEffect.Instance.GoColor(); });
                    HurtPlayer();
                    collision.gameObject.GetComponent<PlayerHealth>().ManageHealth(-playerDamage);
                    collision.gameObject.GetComponent<PlayerHealth>().hit = true;
                    Debug.Log("playerHit");
                    bullet.Release(gameObject);
                }
            }
        }
        if (collision.CompareTag("Untagged"))
        {
            //bullet.Release(gameObject);
            //Destroy(gameObject);
        }
        if (collision.CompareTag("Blood") &&  parryable && enemyRigibody.GetComponent<EnemyManager>().projectile_Parried)
        {
            
            Debug.Log("Hit Enemy");
            collision.GetComponent<EnemyHealth>().EnemyHit(playerDamage);
            collision.GetComponent<EnemyManager>().ResetParrayable();
            enemyRigibody = null;
            playerMovement.playerKickedSomething = false;       
            gameObject.SetActive(false); 

        }

    }
  
}
