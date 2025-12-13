using UnityEngine;
using System.Collections;
using QFSW.MOP2;
using Unity.VisualScripting;

public class ExplosionScript : MonoBehaviour {

	[Header("Customizable Options")]
	//How long before the explosion prefab is destroyed
	public float despawnTime = 10.0f;
	//How long the light flash is visible
	public float lightDuration = 0.02f;
	[Header("Light")]
	public Light lightFlash;

	[Header("Audio")]
	public AudioClip[] explosionSounds;
	public AudioSource audioSource;
	public GameObject player;

	public ObjectPool pool;

	public bool slash;
    private void Awake()
    {
		player = FindFirstObjectByType<PlayerMovement>().gameObject;
    }
    private void Start () {
		//Start the coroutines
		if(!slash)
		{
            StartCoroutine(DestroyTimer());
            StartCoroutine(LightFlash());

            //Get a random impact sound from the array
            audioSource.clip = explosionSounds
                [Random.Range(0, explosionSounds.Length)];
            //Play the random explosion sound
            audioSource.Play();
        }
			
	}
    private void Update()
    {
        if (player == null && slash) return;

        // Get direction to player (ignore Y so it only rotates horizontally)
        Vector3 direction = player.transform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f) // Avoid errors if player is at same position
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = targetRotation;
        }
    }
    private IEnumerator LightFlash () {
		//Show the light
		lightFlash.GetComponent<Light>().enabled = true;
		//Wait for set amount of time
		yield return new WaitForSeconds (lightDuration);
		//Hide the light
		lightFlash.GetComponent<Light>().enabled = false;
	}

	private IEnumerator DestroyTimer () {
		//Destroy the explosion prefab after set amount of seconds
		yield return new WaitForSeconds (despawnTime);
		pool.Release(gameObject);
	}
	public Animator anim;
	public void Slash()
	{
        StartCoroutine(LightFlash());
        anim.SetTrigger("Slash");
        //Get a random impact sound from the array
        audioSource.clip = explosionSounds
            [Random.Range(0, explosionSounds.Length)];
        //Play the random explosion sound
        audioSource.Play();
    }

}