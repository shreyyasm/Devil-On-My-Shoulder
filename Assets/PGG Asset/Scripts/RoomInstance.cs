using UnityEngine;
using System.Collections.Generic;

public class RoomInstance
{
    public GameObject Instance;
    public Bounds Bounds;
    public List<Door> Doors;

    private const float BOUNDS_SHRINK = 0.5f;

    public RoomInstance(GameObject obj)
    {
        Instance = obj;
        Doors = new List<Door>(obj.GetComponentsInChildren<Door>());

        BoxCollider box = FindRoomBoundsCollider(obj);
        if (box == null)
        {
            Debug.LogError("[ProcGen] RoomBounds collider missing on " + obj.name);
            Bounds = new Bounds(obj.transform.position, Vector3.zero);
            return;
        }

        // 🔴 FORCE collider bounds to update after transform changes
        Physics.SyncTransforms();

        Bounds = box.bounds;
        Bounds.Expand(-BOUNDS_SHRINK);
    }

    BoxCollider FindRoomBoundsCollider(GameObject root)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>())
        {
            if (t.CompareTag("RoomBounds"))
            {
                return t.GetComponent<BoxCollider>();
            }
        }
        return null;
    }
}
