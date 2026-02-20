using UnityEngine;
using System.Collections;

public class FireRoomSpawner : MonoBehaviour
{
    public GameObject fireTilePrefab;
    public LayerMask groundLayer;

    [SerializeField] private float tileSpacing = 2f;
    [SerializeField] private float spreadDelay = 0.02f;
    [SerializeField] private float rayHeight = 10f;

    public void SpawnFireInRoom(Bounds bounds)
    {
        StartCoroutine(SpawnRoutine(bounds));
    }

    private IEnumerator SpawnRoutine(Bounds bounds)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        for (float x = min.x; x < max.x; x += tileSpacing)
        {
            for (float z = min.z; z < max.z; z += tileSpacing)
            {
                // Cast ray downward to find ground
                Vector3 rayOrigin = new Vector3(x, max.y + rayHeight, z);

                if (Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out RaycastHit hit,
                    rayHeight * 2f,
                    groundLayer))
                {
                    Instantiate(
                        fireTilePrefab,
                        hit.point + Vector3.up * 0.02f,
                        Quaternion.identity,
                        transform
                    );
                }

                yield return new WaitForSeconds(spreadDelay);
            }
        }
    }
}