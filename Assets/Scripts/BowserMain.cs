using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BowserMain : MonoBehaviour
{
    public string bowserTag = "Bowser";
    public string playerTag = "player";
    public float speed = 5f;
    public float forceMagnitude = 10f;

    void Update()
    {
        if(FindBowser() != null)
        {
            Vector3 directionAway = transform.position - FindBowser().position;
            directionAway.Normalize();
            transform.Translate(directionAway * speed * Time.deltaTime);
        }
        
    }
    public float playerDamage = 10f;
    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag(playerTag))
        {
            Rigidbody playerRigidbody = collision.collider.GetComponent<Rigidbody>();
            if (playerRigidbody != null)
            {
                Vector3 directionAway = (collision.transform.position - transform.position).normalized;
                playerRigidbody.AddForce(directionAway * forceMagnitude, ForceMode.Impulse);
                if (collision.gameObject.CompareTag("Player"))
                {
                    HurtPlayer();
                    collision.gameObject.GetComponent<PlayerHealth>().ManageHealth(- playerDamage);
                    collision.gameObject.GetComponent<PlayerHealth>().hit = true;
                }
            }
        }
    }
    [SerializeField] GameObject Sender;
    public void HurtPlayer()
    {

        //PlayerHealth.instance.Damage(damageDone);
        bl_DamageInfo info = new bl_DamageInfo(playerDamage);
        info.Sender = Sender;
        Sender.SetIndicator();
        bl_DamageDelegate.OnDamageEvent(info);
        CameraShake.Instance.shakeDuration += 0.3f;
    }

    Transform FindBowser()
    {
        GameObject bowser = GameObject.FindGameObjectWithTag(bowserTag);
        if (bowser != null)
        {
            return bowser.transform;
        }
        else
        {
            //Debug.LogWarning("Bowser GameObject not found.");
            return null;
        }
    }
}