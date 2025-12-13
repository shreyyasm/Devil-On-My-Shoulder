using QFSW.MOP2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour
{
    public float rotationSpeed = 85;
    public AudioClip coinSFX;
    public ObjectPool coinsPool;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.rotation *= Quaternion.Euler(0, rotationSpeed * Time.deltaTime, 0);
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            AudioSource.PlayClipAtPoint(coinSFX, transform.position, 1);
            KillStreak.Instance.CoinCollectProgress();
            coinsPool.Release(gameObject);
            LevelCompletion.Instance.CoinCheck();
        }
        
    }
}
