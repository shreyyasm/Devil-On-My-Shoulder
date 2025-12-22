using InfimaGames.LowPolyShooterPack;
using QFSW.MOP2;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class Projectile : MonoBehaviour {

	
	[Range(3, 100)]
	[Tooltip("After how long time should the bullet prefab be destroyed?")]
	public float destroyAfter;

	public ObjectPool bloodPool;
	public ObjectPool bulletPool;

    public float bulletDamage = 10f;

    [Header("Demo Gun Settings")]
    public bool demoBullet;
	public float baseDemoDamage = 10f;
	public float multiplerDemoDamage = 10f;

    public GameObject impactPrefab;
    public bool ignorePrevRotation = false;

    private void Start ()
	{
		//Grab the game mode service, we need it to access the player character!
		var gameModeService = ServiceLocator.Current.Get<IGameModeService>();
		//Ignore the main player character's collision. A little hacky, but it should work.
		Physics.IgnoreCollision(gameModeService.GetPlayerCharacter().GetComponent<Collider>(), GetComponent<Collider>());
        //Start destroy timer
        StartCoroutine (DestroyAfter());


    }
	public void SetDamage(float damage)
	{
		bulletDamage = damage;
    }
    public float Damage()
	{
        if (demoBullet)
        {
            if (Weapon.Instance.damageMultipler < Weapon.Instance.maxDamageMultiplerLimit)
                Weapon.Instance.damageMultipler += multiplerDemoDamage;

            bulletDamage += Weapon.Instance.damageMultipler;
        }
        Debug.Log("Enemy Hit By " + bulletDamage + "Damage");

        return bulletDamage;
    }
	//If the bullet collides with anything
	private void OnCollisionEnter (Collision collision)
	{


        if (collision.gameObject.tag != "FX" && collision.gameObject.layer != 16)
        {
            ContactPoint contact = collision.contacts[0];
            Quaternion rot = Quaternion.FromToRotation(Vector3.forward, contact.normal);
            if (ignorePrevRotation)
            {
                rot = Quaternion.Euler(0, 0, 0);
            }
            Vector3 pos = contact.point;
            Instantiate(impactPrefab, pos, rot);
            bulletPool.Release(gameObject);
        }

        //Ignore collisions with other projectiles.
        if (collision.gameObject.GetComponent<Projectile>() != null)
			return;
		
		if (demoBullet)
		{
            if (collision.gameObject.layer != 16)
            {
                Weapon.Instance.damageMultipler = 0;
                bulletDamage = baseDemoDamage;
				
            }
        }
		

		GameObject bloodSplatter;
		//If bullet collides with "Blood" tag
		if (collision.transform.tag == "Blood") 
		{
			bloodSplatter =  bloodPool.GetObject();
			bloodSplatter.transform.position = transform.position;
			bloodSplatter.transform.rotation = Quaternion.LookRotation(collision.contacts[0].normal);

			StartCoroutine(DisableSplat());
			//Destroy bullet object
			Destroy(gameObject);
		}
		IEnumerator DisableSplat()
        {
			yield return new WaitForSeconds(2f);
			bloodPool.Release(bloodSplatter);
			bulletPool.Release(gameObject);
		}

	}
	private IEnumerator DestroyAfter () 
	{
		//Wait for set amount of time
		yield return new WaitForSeconds (destroyAfter);
        //Destroy bullet object
        bulletPool.Release(gameObject);
    }
}