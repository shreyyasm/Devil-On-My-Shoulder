using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossChallenge : MonoBehaviour
{
    public static BossChallenge instance;
    public float timer;
    bool timerStart;
    public bool UnderOneMin = true;
    private void Awake()
    {
        if(instance == null)
            instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        LeanTween.delayedCall(3, () => { timerStart = true; });
    }

    // Update is called once per frame
    void Update()
    {
        if(timerStart)
        {
            timer += Time.deltaTime;
            if(timer >= 60)
            {
                UnderOneMin = false;
                timerStart = false;
                timer = 0;
            }
        }
    }
}
