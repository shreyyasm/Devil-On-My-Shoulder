using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lift : MonoBehaviour
{
    public Animator animator;
    AudioSource audioSource;
    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        StartCoroutine(OpenLift());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    IEnumerator OpenLift()
    {
        yield return new WaitForSeconds(1f);
        SFXManager.Instance.PlayNPC_SFX(audioSource, "Lift/Open", 0.7f);
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
