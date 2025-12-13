using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Transitioner : MonoBehaviour
{
    public static Transitioner Instance;
    public Animator animator;
    public GameObject Parent;
    // Start is called before the first frame update
    void Start()
    {
        if(Instance == null)
            Instance = this;

        TransitionToFix();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void TransitionToFix()
    {
        Parent.SetActive(true);
        animator.SetBool("Transition", true);
        //LeanTween.delayedCall(2f, () => { animator.SetBool("Transition", false); Parent.SetActive(false); });
    }
}
