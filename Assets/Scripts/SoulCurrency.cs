using System;
using UnityEngine;

/// <summary>
/// Session wallet for souls earned from combat XP. It deliberately never
/// writes to ComboManager.currentXP, which remains responsible for the
/// kill-meter and multiplier only.
/// </summary>
public sealed class SoulCurrency : MonoBehaviour
{
    public static SoulCurrency Instance { get; private set; }

    public int Balance { get; private set; }
    public event Action<int> BalanceChanged;

    public static SoulCurrency EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        SoulCurrency existing = FindFirstObjectByType<SoulCurrency>();
        if (existing != null)
            return existing;

        GameObject wallet = new GameObject("Soul Currency");
        return wallet.AddComponent<SoulCurrency>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Credits only the newly displayed portion of ComboManager's score. This
    /// lets the wallet track the score total without changing that score.
    /// </summary>
    public void SyncWithComboScore(int currentScore)
    {
        currentScore = Mathf.Max(0, currentScore);
        // A new level/run can recreate ComboManager with a fresh score. Its
        // new score should start contributing immediately instead of being
        // compared against the previous level's total.
        if (currentScore < lastObservedComboScore)
            lastObservedComboScore = 0;
        if (currentScore == lastObservedComboScore)
        {
            return;
        }

        Add(currentScore - lastObservedComboScore);
        lastObservedComboScore = currentScore;
    }

    public void Add(int amount)
    {
        if (amount <= 0)
            return;

        Balance += amount;
        BalanceChanged?.Invoke(Balance);
    }

    public bool CanAfford(int price)
    {
        return price >= 0 && Balance >= price;
    }

    public bool TrySpend(int price)
    {
        if (!CanAfford(price))
            return false;

        Balance -= price;
        BalanceChanged?.Invoke(Balance);
        return true;
    }

    /// <summary>Clears the current run's wallet after a full player death.</summary>
    public void ResetForDeath()
    {
        Balance = 0;
        lastObservedComboScore = 0;
        BalanceChanged?.Invoke(Balance);
    }

    private int lastObservedComboScore;
}
