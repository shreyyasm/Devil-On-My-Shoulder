using UnityEngine;

public class KnifeProjectile : MonoBehaviour
{
    private Transform target;
    private float speed;

    public float rotateSpeed = 720f;

    public void Init(Transform target, float speed)
    {
        this.target = target;
        this.speed = speed;
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // 🎯 move toward target
        Vector3 direction = (target.position + Vector3.up * 1f - transform.position).normalized;

        transform.position += direction * speed * Time.deltaTime;

        // 🔁 spin (knife feel)
        transform.Rotate(Vector3.right * rotateSpeed * Time.deltaTime);

        // 🎯 hit check
        if (Vector3.Distance(transform.position, target.position) < 0.5f)
        {
            HitTarget();
        }
    }

    void HitTarget()
    {
        EnemyHealth enemy = target.GetComponent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.EnemyHit(50f); // adjust damage
        }

        Destroy(gameObject);
    }
}