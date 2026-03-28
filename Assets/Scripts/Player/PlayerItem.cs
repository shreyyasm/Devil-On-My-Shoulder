using UnityEngine;

public class PlayerItem : MonoBehaviour
{
    public bool throwable = true;

    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public Collider col;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public void OnUse(PlayerMovement player)
    {
        Debug.Log("Item Used!");

        // 👉 Add custom logic here (heal, buff, etc.)
        Destroy(gameObject);
    }


}