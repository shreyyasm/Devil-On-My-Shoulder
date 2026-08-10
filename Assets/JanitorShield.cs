using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JanitorShield : MonoBehaviour
{
    public GameObject Shield;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            ShowShield(0.3f);
            SFXManager.Instance.PlaySFX("Janitor/ShieldHit", 0.8f);
        }

    }
    private Coroutine shieldCoroutine;
    public void ShowShield(float duration)
    {
        if (shieldCoroutine != null)
        {
            StopCoroutine(shieldCoroutine);
        }

        shieldCoroutine = StartCoroutine(ShieldRoutine(duration));
    }
    private IEnumerator ShieldRoutine(float duration)
    {
        if (Shield != null)
            Shield.SetActive(true);

        yield return new WaitForSeconds(duration);

        if (Shield != null)
            Shield.SetActive(false);

        shieldCoroutine = null;
    }
}
