using QFSW.MOP2;
using UnityEngine;

public class BatteryLife : MonoBehaviour
{
    public GameObject player;
    public float moveSpeed = 5f;

    private bool movingToPlayer = false;
    public Transform healthPos;

    public ObjectPool batteryLife;

    private void Start()
    {
        player = FindFirstObjectByType<PlayerMovement>().gameObject;
        healthPos = player.GetComponent<PlayerHealth>().healthAnim.transform;
        movingToPlayer = true;
    }

    void Update()
    {
        if (player == null) return;

        if (movingToPlayer)
        {
            // Move towards target
            transform.position = Vector3.MoveTowards(
                transform.position,
                healthPos.position,
                moveSpeed * Time.deltaTime
            );

            // Rotate to face player (optional)
            Vector3 direction = player.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }

            // If close enough, auto trigger
            if (Vector3.Distance(transform.position, healthPos.position) < 0.1f)
            {
                Collect();
            }
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Collect();
        }
    }

    private void Collect()
    {
        player.GetComponent<PlayerHealth>().ManageHealth(30);
        batteryLife.Release(gameObject);
    }
}
