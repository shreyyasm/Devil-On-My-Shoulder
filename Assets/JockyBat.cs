using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JockyBat : MonoBehaviour
{
    public GameObject WeaponPrefab;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void ActivateBatCollider()
    {
        WeaponPrefab.SetActive(true);
    }
    public void DisableBatCollider()
    {
        WeaponPrefab.SetActive(false);
    }
}
