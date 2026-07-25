using QFSW.MOP2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableDecal : MonoBehaviour
{
    public ObjectPool Pool;
    private void OnEnable()
    {
        StartCoroutine(DisableDecals());
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    IEnumerator DisableDecals()
    {
        yield return new WaitForSeconds(4f);
        Pool.Release(gameObject);
    }
}
