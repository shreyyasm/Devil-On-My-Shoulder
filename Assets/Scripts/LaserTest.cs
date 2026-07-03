using UnityEngine;

public class LaserTest : MonoBehaviour
{
    public LineRenderer lr;
    public Transform startPoint;
    public Transform endPoint;

    void Update()
    {
        if (!lr || !startPoint || !endPoint) return;

        lr.positionCount = 2;
        lr.SetPosition(0, startPoint.position);
        lr.SetPosition(1, endPoint.position);
    }
}