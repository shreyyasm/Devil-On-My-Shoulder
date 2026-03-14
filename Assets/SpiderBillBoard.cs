using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderBillBoard : MonoBehaviour
{
    private Transform player;
    public bool RotateExternally;

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    // Update is called once per frame
    void Update()
    {
        // Get direction to player (ignore Y so it only rotates horizontally)
        Vector3 direction = player.transform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f) // Avoid errors if player is at same position
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            if (!RotateExternally)
                transform.rotation = targetRotation;
        }

    }
}
