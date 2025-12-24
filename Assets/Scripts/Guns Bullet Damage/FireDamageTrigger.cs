using System.Collections.Generic;
using UnityEngine;

public class FireDamageTrigger : MonoBehaviour
{
    [Header("Damage")]
    public float damagePerTick = 5f;
    public float damageInterval = 1f;

    private PlayerMovement player;
    private void Awake()
    {
        player = FindAnyObjectByType<PlayerMovement>();
    }
    private void Start()
    {
        Destroy(gameObject,5);
    }
    // Tracks enemies and their timers
    private Dictionary<GameObject, float> damageTimers = new Dictionary<GameObject, float>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != 16)
            return;
       
        // Immediate damage on enter
        DealDamage(other.gameObject);

        // Start tracking for DOT
        if (!damageTimers.ContainsKey(other.gameObject))
            damageTimers.Add(other.gameObject, damageInterval);
    }

    private void OnTriggerExit(Collider other)
    {
        if (damageTimers.ContainsKey(other.gameObject))
            damageTimers.Remove(other.gameObject);
    }

    private void Update()
    {
        FacePlayerZAxisOnly(player.gameObject.transform, 8f); // smooth turn

        //if (damageTimers.Count == 0)
        //    return;

        // Copy keys to avoid collection modification issues
        var keys = new List<GameObject>(damageTimers.Keys);

        foreach (GameObject enemy in keys)
        {
            if (enemy == null)
            {
                damageTimers.Remove(enemy);
                continue;
            }

            damageTimers[enemy] -= Time.deltaTime;

            if (damageTimers[enemy] <= 0f)
            {
                DealDamage(enemy);
                damageTimers[enemy] = damageInterval;
            }
        }
    }

    private void DealDamage(GameObject enemy)
    {
        // Example: generic health script
        var health = enemy.GetComponent<Enemy>();
        if (health != null)
        {
            health.EnemyHit(damagePerTick);
            Debug.Log("Enemy Hit By " + damagePerTick + "Damage");

        }
    }
    public void FacePlayerZAxisOnly(Transform player, float rotationSpeed = 0f)
    {
        if (player == null) return;

        Vector2 direction = player.position - transform.position;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        // Calculate Z rotation angle
        float angleZ = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        Quaternion targetRotation = Quaternion.Euler(-90f, 0f, angleZ);

        if (rotationSpeed > 0f)
        {
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
        else
        {
            transform.rotation = targetRotation;
        }
    }




}
