using UnityEngine;
using System.Collections;
using QFSW.MOP2;

public class ECdestroyMe : MonoBehaviour{

    float timer;
    public float deathtimer = 10;
	public ObjectPool ImpactPool;

    private void OnEnable()
    {
        StartCoroutine(DisableImpact());
    }

  
	IEnumerator DisableImpact()
	{
		yield return new WaitForSeconds(deathtimer);
        ImpactPool.Release(gameObject);
    }
}

