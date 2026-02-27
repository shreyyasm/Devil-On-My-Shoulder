using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStats", menuName = "FPS/Enemy Stats")]
public class EnemyStats : ScriptableObject
{
    // ================================
    // TYPE DEFINITIONS
    // ================================

    public enum EnemyType { Melee, Shoot, Touch, Thrower, Boss }
    public enum CoverType { No, Yes }
    public enum DeathType { Simple, Explosion, ToxicSplash }
    public enum SurfaceType { Ground, Air }

    [Header("Enemy Type Settings")]
    public EnemyType enemyType;
    public CoverType coverType;
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
}