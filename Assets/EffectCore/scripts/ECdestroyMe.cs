using UnityEngine;
using System.Collections;
using QFSW.MOP2;

public class ECdestroyMe : MonoBehaviour{

    float timer;
    public float deathtimer = 10;
	public ObjectPool ImpactPool;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update ()
    {
        timer += Time.deltaTime;

        if(timer >= deathtimer)
        {
			//Destroy(gameObject);
			ImpactPool.Release(gameObject);
        }
	
	}
}
