using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStats", menuName = "FPS/Enemy Stats")]
public class EnemyStats : ScriptableObject
{
    // ================================
    // TYPE DEFINITIONS
    // ================================

    public enum EnemyType { Melee, Shoot, Touch, Thrower, Tombstone }
    public enum CoverType { No, Yes }
    public enum ShieldType { No, Yes }
    public enum DeathType { Simple, Explosion, ToxicSplash }
    public enum SurfaceType { Ground, Air }

    [Header("Enemy Type Settings")]
    public EnemyType enemyType;
    public CoverType coverType;
    public ShieldType shieldType;
    public DeathType deathType;
    public SurfaceType surfaceType;

    // ================================
    // HEALTH STATS
    // ================================

    [Header("Health Stats")]
    public float health;
    public float gunHealth;
    public float shieldHealth;

    // ================================
    // MOVEMENT
    // ================================

    [Header("Movement Stats")]
    public float speed;

    // ================================
    // ATTACK
    // ================================

    [Header("Attack Stats")]
    public float damage;

    // ================================
    // MELEE
    // ================================

    [Header("Melee Stats")]
    public float AttackRange;
    public float AttackIntervalMin;
    public float AttackIntervalMax;
    public float RecoverTime;
    public float StoppingDistance;

    public bool canDash;

    // ================================
    // GRAB (Melee Subtype)
    // ================================

    [Header("Grab Settings")]
    public bool canGrab;
    public float grabDamageInterval = 0.5f;
    public float grabOffset = 1.0f;
    public float grabHeightOffset;
    // ================================
    // DASH
    // ================================

    [Header("Dash Stats")]
    public float DashForce;
    public float DashLoadTime;
    public float DashDurationTime;
    public float DashStopTime;

    // ================================
    // SHOOTING
    // ================================

    [Header("Shooting Stats")]
    public GameObject projectilePrefab;
    public float ShootSpeed;
    public float ShootInterval;
    public float ChangePositionTime;
    public float MoveRadius;
    public bool canShootMove;

    [Header("Cover Settingns")]
    public float CoverTime;
    public float PeekDuration;
    public float PeekRadius;
    public float CoverSearchRadius;
    public float VisionHeight;


    [Header("Tombstone Settings")]
    public GameObject spawnEnemyPrefab;
    public float spawnInterval = 3f;
    public int spawnLimit = 5;
    public bool useSpawnLimit;

    [Header("Shooter Tombstone Settings")]
    public bool isShooterTombstone;
    public float repositionInterval = 6f;

    public int spawnBatchCount = 3;

    public float shootDelayAfterSpawn = 2f;
    public GameObject shootProjectilePrefab;
    public float mortarArcHeight = 6f;
    public float shootProjectileSpeed = 20f;

    public float recoverAfterShot = 3f;
    public float initialSpawnDelay = 3f;

    [Header("Audio")]
    public string attackSoundKey;
}