using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class CryoFreeze : MonoBehaviour
{
    private float freezeDuration;
    private Color freezeColor;

    private NavMeshAgent agent;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private Enemy enemy;

    public void Initialize(float duration, Color color)
    {
        freezeDuration = duration;
        freezeColor = color;
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemy = GetComponent<Enemy>();
    }

    private void Start()
    {
        // Safety
        if (enemy == null)
        {
            Destroy(this);
            return;
        }

        // 🧊 Freeze movement
        if (agent != null)
            agent.enabled = false;

      
        // ❤️ Cut health to half instantly
        enemy.EnemyHit(enemy.health * 0.5f);

        StartCoroutine(FreezeTimer());
    }

    private IEnumerator FreezeTimer()
    {
        yield return new WaitForSeconds(0.1f);
        // 💙 Change color
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            spriteRenderer.color = freezeColor;
        }

        yield return new WaitForSeconds(freezeDuration);

        // 🔓 Unfreeze
        if (agent != null)
            agent.enabled = true;

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        Destroy(this);
    }
}
