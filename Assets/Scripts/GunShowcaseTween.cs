using UnityEngine;

public class GunShowcaseRotate : MonoBehaviour
{
    public float speed = 50f;

    private Quaternion targetRotation;

    void Start()
    {
        targetRotation = transform.rotation;
    }

    void Update()
    {
        // Keep adding smooth rotation over time
        targetRotation *= Quaternion.Euler(0f, speed * Time.deltaTime, 0f);

        // Smoothly interpolate toward it
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.1f);
    }
}