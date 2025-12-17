using UnityEngine;

public class Door : MonoBehaviour
{
    public Transform SnapPoint;
    public GameObject Blocker;

    [HideInInspector] public bool IsUsed;

    public void Open()
    {
        IsUsed = true;
        if (Blocker != null)
            Blocker.SetActive(false);
    }

    public void Close()
    {
        if (Blocker != null)
            Blocker.SetActive(true);
    }
}
