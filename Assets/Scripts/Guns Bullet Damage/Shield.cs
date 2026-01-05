using UnityEngine;

public class Shield : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 180f;

    private float currentX;

    void Start()
    {
        // Force initial Z rotation
        Vector3 euler = transform.localEulerAngles;
        transform.localEulerAngles = new Vector3(euler.x, euler.y, 90f);
        currentX = euler.x;
    }

    void Update()
    {
        currentX += rotationSpeed * Time.deltaTime;
        transform.localEulerAngles = new Vector3(180, currentX, 90f);
    }
}
