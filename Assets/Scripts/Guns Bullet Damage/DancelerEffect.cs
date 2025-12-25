using UnityEngine;
using UnityEngine.AI;

public class DancelerEffect : MonoBehaviour
{
    private float rotationSpeed;
    private int rotations;

    private float rotatedDegrees;

    private NavMeshAgent agent;
    private bool effectActive;

    public void Initialize(float speed, int rotCount)
    {
        rotationSpeed = speed;
        rotations = Mathf.Max(1, rotCount);
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        // Stop movement
        if (agent != null)
            agent.enabled = false;

        effectActive = true;
        GetComponent<Enemy>().RotateExternally = true;
    }

    private void Update()
    {
        if (!effectActive)
            return;

        float delta = rotationSpeed * Time.deltaTime;

        transform.Rotate(Vector3.up, delta, Space.World);
        rotatedDegrees += delta;

        // ✔ Stop after full rotations
        if (rotatedDegrees >= 360f * rotations)
        {
            EndEffect();
        }
    }

    private void EndEffect()
    {
        effectActive = false;

        // Restore movement
        if (agent != null)
            agent.enabled = true;
        GetComponent<Enemy>().RotateExternally = false;
        Destroy(this);
    }
}
