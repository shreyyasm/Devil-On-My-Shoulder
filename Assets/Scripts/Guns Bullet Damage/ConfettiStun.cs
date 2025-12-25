using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ConfettiStun : MonoBehaviour
{
    private float stunDuration;

    private NavMeshAgent agent;
    private Enemy enemy;

    public void Initialize(float duration)
    {
        stunDuration = duration;
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemy = GetComponent<Enemy>();
    }

    private void Start()
    {
        // Safety check
        if (agent == null)
        {
            Destroy(this);
            return;
        }

        // 🚫 Disable movement
        agent.enabled = false;

        StartCoroutine(StunTimer());
    }
    public GameObject giggleSmilePrefab;
    private IEnumerator StunTimer()
    {
        yield return new WaitForSeconds(0.2f);
        Instantiate(
              giggleSmilePrefab,
             enemy.transform.position + Vector3.up * 2.8f,
             Quaternion.identity
         );

        yield return new WaitForSeconds(stunDuration);

        // 🔄 Re-enable movement
        if (agent != null)
            agent.enabled = true;

        // 🧹 Clean up
        Destroy(this);
    }

}
