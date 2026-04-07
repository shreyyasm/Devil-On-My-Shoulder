using UnityEngine;

public class RoomVolume : MonoBehaviour
{
    public Bounds GetCombinedBounds()
    {
        Collider[] cols = GetComponentsInChildren<Collider>();

        if (cols.Length == 0)
            return new Bounds(transform.position, Vector3.zero);

        Bounds combined = cols[0].bounds;

        for (int i = 1; i < cols.Length; i++)
        {
            combined.Encapsulate(cols[i].bounds);
        }

        return combined;
    }
}