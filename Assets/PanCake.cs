using UnityEngine;

public class PanCake : MonoBehaviour
{
    private Transform player;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (player == null) return;

        Vector3 targetPosition = player.position;
        targetPosition.y = -90; // Ignore vertical difference

        transform.LookAt(targetPosition);
    }
}