using UnityEngine;
using System.Collections.Generic;

public class RoomInstance
{
    public GameObject Instance;
    public Bounds Bounds;
    public List<Door> Doors;

    // Rooms are allowed to touch at their door seams, but their playable
    // bounds must never interpenetrate. A tiny inset avoids floating-point
    // contact being treated as an overlap without hiding a real collision.
    private const float BOUNDS_INSET = 0.02f;
    public int RoomIndex;
    private BoxCollider roomBoundsCollider;

    public RoomInstance(GameObject obj)
    {
        Instance = obj;
        Doors = new List<Door>(obj.GetComponentsInChildren<Door>());

        roomBoundsCollider = FindRoomBoundsCollider(obj);
        if (roomBoundsCollider == null)
        {
            Debug.LogError("[ProcGen] RoomBounds collider missing on " + obj.name);
            Bounds = new Bounds(obj.transform.position, Vector3.zero);
            return;
        }

        RefreshBounds();
    }

    public void RefreshBounds()
    {
        if (roomBoundsCollider == null)
        {
            Bounds = new Bounds(Instance != null ? Instance.transform.position : Vector3.zero, Vector3.zero);
            return;
        }

        // Keep placement checks accurate after a room or its level root has
        // been moved, rotated, or handed off from the prepared level.
        Physics.SyncTransforms();

        Bounds = roomBoundsCollider.bounds;
        Bounds.Expand(-BOUNDS_INSET);
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
