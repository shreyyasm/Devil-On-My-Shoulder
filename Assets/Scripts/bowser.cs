using QFSW.MOP2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bowser : MonoBehaviour
{
    
    public float radius = 5f;
    public int numObjects = 36;
    public float instantiationInterval = 1f;

    private Coroutine instantiationCoroutine;

    public ObjectPool enemies;
    public GameObject prefabToInstantiate;
    public bool Fireball;

    void Start()
    {
        InvokeRepeating("InstantiateObjects", 5, instantiationInterval);
    }
    public AudioSource audioSource;
    public AudioClip FireSFX;
    void InstantiateObjects()
    {

        for (int i = 0; i < numObjects; i++)
        {
            float angle = i * (360f / numObjects);
            float radians = angle * Mathf.Deg2Rad;
            float x = Mathf.Cos(radians) * radius;
            float z = Mathf.Sin(radians) * radius;

            //Instantiate(prefabToInstantiate, transform.position + new Vector3(x, 0f, z), Quaternion.identity);
            GameObject enemy = enemies.GetObject();
            enemy.transform.position = transform.position + new Vector3(x, 0f, z);
            if (!Fireball)
                enemy.transform.rotation = Quaternion.identity;
            else
                enemy.transform.rotation = Quaternion.LookRotation(enemy.transform.forward);
        }
        if (Fireball)
            audioSource.PlayOneShot(FireSFX, 1f);

    }

   
}
 