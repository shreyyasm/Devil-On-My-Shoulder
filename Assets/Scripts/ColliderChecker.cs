using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderChecker : MonoBehaviour
{
    public DungeonGenerator DungeonGenerator;
    // Start is called before the first frame update
    private void Awake()
    {
        DungeonGenerator = FindObjectOfType<DungeonGenerator>();
    }
    void Start()
    {
      
        //gameObject.tag = "Untagged";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    //private void OnCollisionEnter(Collision collision)
    //{
    //    if(collision.gameObject.CompareTag("ColliderChecker"))
    //        DungeonGenerator.ClearCurrentDungeon();
    //}
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("ColliderChecker"))
        {
            //DungeonGenerator.ClearCurrentDungeon();
            //Debug.Log("work");
        }
           
    }
}
