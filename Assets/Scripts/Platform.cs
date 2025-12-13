using UnityEngine;

public class Platform : MonoBehaviour
{
    public Transform pointA;  // The first position (left side)
    public Transform pointB;  // The second position (right side)
    public float speed = 2f;  // Speed of the crusher
    public  bool movingToPointB = true; // Direction tracker

    private void Update()
    {
        transform.rotation *= Quaternion.Euler(0, 30 * Time.deltaTime, 0);
        MoveCrusher();
    }

    void MoveCrusher()
    {
        // Move towards pointA or pointB based on direction
        if (movingToPointB)
        {
            transform.position = Vector3.MoveTowards(transform.position, pointB.position, speed * Time.deltaTime);

            // If reached pointB, switch direction
            if (Vector3.Distance(transform.position, pointB.position) < 0.1f)
                movingToPointB = false;
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, pointA.position, speed * Time.deltaTime);

            // If reached pointA, switch direction
            if (Vector3.Distance(transform.position, pointA.position) < 0.1f)
                movingToPointB = true;
        }
    }

    
}
