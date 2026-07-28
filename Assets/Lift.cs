using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lift : MonoBehaviour
{
    public Animator animator;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(OpenLift());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    IEnumerator OpenLift()
    {
        yield return new WaitForSeconds(5f);
        animator.SetTrigger("OpenLift");
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            animator.SetTrigger("CloseLift");
        }
    }

}
