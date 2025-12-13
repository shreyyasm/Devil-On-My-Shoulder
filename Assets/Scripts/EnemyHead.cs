using QFSW.MOP2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHead : MonoBehaviour
{
    public ObjectPool enemy1Pool;
    public AudioClip jumpKillSFX;
    GameObject explosion;
    public ObjectPool explosionPool;
    public ObjectPool batteryLife;

    Animator anim;
    public bool boss;
    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        if(boss)
            InvokeRepeating("JumpToPlayer", 5f, 8f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            Enemy.Instance.health -= Enemy.Instance.maxHealth;
            explosion = explosionPool.GetObject();
            explosion.transform.position = transform.position;
            batteryLife.GetObject().transform.position = transform.position;

            GameObject obj = batteryLife.GetObject();
            obj.transform.position = transform.position;

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Give it an upward "pop"
                rb.AddForce(Vector3.up * 7, ForceMode.Impulse);
            }
            //AudioSource.PlayClipAtPoint(jumpKillSFX, Camera.main.transform.position, 2f);
            //Instantiate(Enemy.Instance.explosionPrefab, transform.position, Quaternion.identity);
            AudioSource.PlayClipAtPoint(Enemy.Instance.destroySFX, transform.position, 0.3f);

            //Instantiate(Enemy.Instance.coin, transform.position, Quaternion.identity);
            //enemy1Pool.Release(gameObject);
            gameObject.SetActive(false);


        }

    }
    public void ReleaseThisEnemy()
    {
        enemy1Pool.Release(gameObject);
    }
    public void Explode()
    {
        Enemy.Instance.health -= Enemy.Instance.maxHealth;
        explosion = explosionPool.GetObject();
        explosion.transform.position = transform.position;
        batteryLife.GetObject().transform.position = transform.position;
        //AudioSource.PlayClipAtPoint(jumpKillSFX, Camera.main.transform.position, 2f);
        //Instantiate(Enemy.Instance.explosionPrefab, transform.position, Quaternion.identity);
        AudioSource.PlayClipAtPoint(Enemy.Instance.destroySFX, transform.position, 0.3f);

        //Instantiate(Enemy.Instance.coin, transform.position, Quaternion.identity);
        enemy1Pool.Release(gameObject);
    }
    public AudioSource audioSource;
    public AudioClip jumpSFX;
    public AudioClip RoarSFX;
    public void JumpToPlayer()
    {
        
        StartCoroutine(FalseJump());
    }
    IEnumerator FalseJump()
    {
        audioSource.PlayOneShot(RoarSFX, 1f);
        yield return new WaitForSeconds(1f);
        anim.SetBool("Jump", true);
        yield return new WaitForSeconds(1f);
        anim.SetBool("Jump", false);

    }
    public void PlayJumpSound()
    {
        audioSource.PlayOneShot(jumpSFX, 1f);
    }
}
