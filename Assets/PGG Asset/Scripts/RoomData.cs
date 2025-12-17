using UnityEngine;

[CreateAssetMenu(menuName = "ProcGen/Room Data")]
public class RoomData : ScriptableObject
{
    public string RoomName;
    public GameObject Prefab;
}
