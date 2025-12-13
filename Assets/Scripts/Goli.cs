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
    void Update()
    {
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
        CameraShake.Instance.shakeDuration += 0.3f;
    }
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Player") && !PowerUpSelect.Instance.playerShielded)
        {
            Rigidbody playerRigidbody = collision.GetComponent<Rigidbody>();
            if (playerRigidbody != null)
            {
                Vector3 directionAway = (collision.transform.position - transform.position).normalized;
                playerRigidbody.AddForce(directionAway * forceMagnitude, ForceMode.Impulse);
                if (collision.gameObject.CompareTag("Player") && !PowerUpSelect.Instance.playerShielded)
                {
                    ImpactFrameEffect.Instance.TriggerImpact(0, 1.2f);
                    LeanTween.delayedCall(0.2f, () => { ImpactFrameEffect.Instance.TriggerImpact(1, 1.05f); });
                    HurtPlayer();
                    collision.gameObject.GetComponent<PlayerHealth>().ManageHealth(-playerDamage);
                    collision.gameObject.GetComponent<PlayerHealth>().hit = true;
                    bullet.Release(gameObject);
                }
            }
        }
        if (collision.CompareTag("Untagged"))
        {
            bullet.Release(gameObject);
        }
    }
}
