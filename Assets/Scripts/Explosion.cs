using QFSW.MOP2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    public ObjectPool explosionPool;
    public int timer = 2;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DisableExplosion());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    IEnumerator DisableExplosion()
    {
        yield return new WaitForSeconds(timer);
        explosionPool.Release(gameObject);
    }
}
