using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectAbility : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    bool choosed;
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player") && !choosed)
        {
            if(ScoreManager.Instance.level != 0)
            {
                ScoreManager.Instance.StartChoice();
                choosed = true; 
            }
              
        }
    }
}
