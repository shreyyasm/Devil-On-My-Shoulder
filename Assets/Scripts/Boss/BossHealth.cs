using UnityEngine;

public class BossHealth : MonoBehaviour
{
    [Header("Health")]
    [Min(1f)] public float maxHealth = 2500f;
    [SerializeField] private float currentHealth;
    [SerializeField] private bool invulnerableDuringIntro = true;
    [SerializeField] private Renderer[] flashRenderers;
    private float damageMultiplier = 1f;

    public float CurrentHealth => currentHealth;
    public float HealthPercent => maxHealth <= 0f ? 0f : currentHealth / maxHealth;
    public bool IsDead { get; private set; }

    private BossManager manager;
    private void Awake()
    {
        manager = GetComponent<BossManager>();
        currentHealth = maxHealth;
        if (flashRenderers == null || flashRenderers.Length == 0)
            flashRenderers = GetComponentsInChildren<Renderer>(true);
    }

    public void SetVulnerable(bool vulnerable) => invulnerableDuringIntro = !vulnerable;
    public void SetDamageMultiplier(float multiplier) => damageMultiplier = Mathf.Max(0f, multiplier);
    public void EnemyHit(float damageValue, bool healthUI = false) => TakeDamage(damageValue);
    public void TakeDamage(float amount)
    {
        if (IsDead || invulnerableDuringIntro || amount <= 0f) return;
        currentHealth = Mathf.Max(0f, currentHealth - amount * damageMultiplier);
        if (manager == null) manager = GetComponent<BossManager>();
        manager?.OnBossDamaged(HealthPercent);
        if (currentHealth <= 0f) { IsDead = true; manager?.OnBossDefeated(); }
    }
    private void OnCollisionEnter(Collision collision) => TryBullet(collision.gameObject);
    private void OnTriggerEnter(Collider other) => TryBullet(other.gameObject);
    private void TryBullet(GameObject source)
    {
        if (!source.CompareTag("Bullet")) return;
        Projectile projectile = source.GetComponent<Projectile>();
        if (projectile != null) TakeDamage(projectile.Damage(gameObject));
    }
}
