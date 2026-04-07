using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelComplete : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if(!other.GetComponent<PlayerHealth>().playerDead)
            {
                ScoreManager.Instance.IncreaseLevel();
                SceneManager.LoadScene("Mad Dash Game");

            }

        }
    }
}
