using UnityEngine;

public static class BoundsGizmo
{
    public static void DrawBounds(Bounds b, Color c)
    {
        Vector3 center = b.center;
        Vector3 ext = b.extents;

        Vector3[] corners =
        {
            center + new Vector3(-ext.x,  ext.y, -ext.z),
            center + new Vector3( ext.x,  ext.y, -ext.z),
            center + new Vector3( ext.x,  ext.y,  ext.z),
            center + new Vector3(-ext.x,  ext.y,  ext.z),

            center + new Vector3(-ext.x, -ext.y, -ext.z),
            center + new Vector3( ext.x, -ext.y, -ext.z),
            center + new Vector3( ext.x, -ext.y,  ext.z),
            center + new Vector3(-ext.x, -ext.y,  ext.z)
        };

        void Line(int a, int b) =>
            Gizmos.DrawLine(corners[a], corners[b]);

        Gizmos.color = c;

        // Top
        Line(0, 1); Line(1, 2); Line(2, 3); Line(3, 0);
        // Bottom
        Line(4, 5); Line(5, 6); Line(6, 7); Line(7, 4);
        // Sides
        Line(0, 4); Line(1, 5); Line(2, 6); Line(3, 7);
    }
}
