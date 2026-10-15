using System;
using System.Collections.Generic;
using UnityEngine;
using InfimaGames.LowPolyShooterPack;

/// <summary>Run-only, weighted upgrades used by the shop and reset after a player death.</summary>
public sealed class RoguelikeManager : MonoBehaviour
{
    public static RoguelikeManager Instance { get; private set; }

    public enum UpgradeType { PlayerSpeed, DashSpeed, PlayerHealth, FireRate, MagazineSize, BulletDamage }

    [Serializable]
    public class UpgradeOffer
    {
        public string displayName;
        public UpgradeType type;
        [Tooltip("Sprite displayed beneath this upgrade title on the shop card.")] public Sprite displaySprite;
        [Tooltip("Color applied to this upgrade card background in the shop.")] public Color cardBackgroundColor = new Color(.05f, .7f, 1f, .82f);
        [Min(0f)] public float probability = 1f;
        [Min(0.01f)] public float amount = .1f;
        [Min(0f)] public float displayPercentage = 10f;
    }

    [Header("Weighted Roguelike Elements")]
    [SerializeField] private List<UpgradeOffer> upgrades = new List<UpgradeOffer>
    {
        new UpgradeOffer { displayName = "Player Speed", type = UpgradeType.PlayerSpeed, probability = 90, amount = 1f, displayPercentage = 1f },
        new UpgradeOffer { displayName = "Dash Speed", type = UpgradeType.DashSpeed, probability = 60, amount = 1f, displayPercentage = 1f },
        new UpgradeOffer { displayName = "Player Health", type = UpgradeType.PlayerHealth, probability = 80, amount = 1f, displayPercentage = 5f },
        new UpgradeOffer { displayName = "Fire Rate", type = UpgradeType.FireRate, probability = 40, amount = 50f, displayPercentage = 5f },
        new UpgradeOffer { displayName = "Magazine Size", type = UpgradeType.MagazineSize, probability = 40, amount = 2f, displayPercentage = 5f },
        new UpgradeOffer { displayName = "Bullet Damage", type = UpgradeType.BulletDamage, probability = 30, amount = 1f, displayPercentage = 5f }
    };

    [Header("Current Run Bonuses")]
    [SerializeField] private float playerSpeedBonus;
    [SerializeField] private float dashSpeedBonus;
    [SerializeField] private float playerHealthBonus;
    [SerializeField] private float fireRateBonus;
    [SerializeField] private float magazineSizeBonus;
    [SerializeField] private float bulletDamageBonus;

    private PlayerMovement playerMovement;
    private PlayerHealth playerHealth;
    private float baseMoveSpeed, baseMaxSpeed, baseDashForce, baseMaxHealth;
    private bool basesCaptured;
    private readonly int[] upgradeStacks = new int[6];

    public float FireRateBonusRpm => fireRateBonus;
    public int MagazineSizeBonus => Mathf.RoundToInt(magazineSizeBonus);
    public float BulletDamageBonus => bulletDamageBonus;
    public IReadOnlyList<UpgradeOffer> Upgrades => upgrades;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start() => CaptureBases();

    public List<UpgradeOffer> RollThreeUpgrades()
    {
        var pool = new List<UpgradeOffer>(upgrades.FindAll(x => x != null && x.probability > 0f));
        var result = new List<UpgradeOffer>();
        while (pool.Count > 0 && result.Count < 3)
        {
            float total = 0f; foreach (var offer in pool) total += offer.probability;
            float roll = UnityEngine.Random.Range(0f, total); int index = 0;
            for (; index < pool.Count - 1; index++) { roll -= pool[index].probability; if (roll <= 0f) break; }
            result.Add(pool[index]); pool.RemoveAt(index);
        }
        return result;
    }

    public void ApplyUpgrade(UpgradeOffer offer)
    {
        if (offer == null) return;
        upgradeStacks[(int)offer.type]++;
        switch (offer.type)
        {
            case UpgradeType.PlayerSpeed: playerSpeedBonus += offer.amount; break;
            case UpgradeType.DashSpeed: dashSpeedBonus += offer.amount; break;
            case UpgradeType.PlayerHealth: playerHealthBonus += offer.amount; break;
            case UpgradeType.FireRate: fireRateBonus += offer.amount; break;
            case UpgradeType.MagazineSize: magazineSizeBonus += offer.amount; break;
            case UpgradeType.BulletDamage: bulletDamageBonus += offer.amount; break;
        }
        ApplyRunStats();
    }

    public void ResetRunStats()
    {
        playerSpeedBonus = dashSpeedBonus = playerHealthBonus = fireRateBonus = magazineSizeBonus = bulletDamageBonus = 0f;
        Array.Clear(upgradeStacks, 0, upgradeStacks.Length);
        ApplyRunStats();
    }

    public float GetCurrentDisplayPercentage(UpgradeOffer offer)
    {
        return offer == null ? 0f : upgradeStacks[(int)offer.type] * offer.displayPercentage;
    }

    public void ApplyRunStats()
    {
        CaptureBases();
        if (playerMovement != null)
        {
            playerMovement.moveSpeed = baseMoveSpeed * (1f + playerSpeedBonus);
            playerMovement.maxSpeed = baseMaxSpeed * (1f + playerSpeedBonus);
            playerMovement.dashForce = baseDashForce * (1f + dashSpeedBonus);
        }
        if (playerHealth != null)
        {
            float oldMax = playerHealth.maxHealth;
            playerHealth.maxHealth = baseMaxHealth * (1f + playerHealthBonus);
            playerHealth.currentHealth = Mathf.Clamp(playerHealth.currentHealth + Mathf.Max(0f, playerHealth.maxHealth - oldMax), 0f, playerHealth.maxHealth);
        }
        foreach (Weapon weapon in FindObjectsOfType<Weapon>(true)) weapon.ApplyRoguelikeMagazineBonus(MagazineSizeBonus);
    }

    private void CaptureBases()
    {
        if (basesCaptured) return;
        playerMovement = FindFirstObjectByType<PlayerMovement>();
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerMovement == null || playerHealth == null) return;
        baseMoveSpeed = playerMovement.moveSpeed; baseMaxSpeed = playerMovement.maxSpeed;
        baseDashForce = playerMovement.dashForce; baseMaxHealth = playerHealth.maxHealth;
        basesCaptured = true;
    }
}
