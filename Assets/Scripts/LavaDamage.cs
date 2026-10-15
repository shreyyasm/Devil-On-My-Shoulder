using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Deals damage immediately when the player touches lava, then once per
/// interval for as long as the player remains in the trigger.
/// </summary>
[RequireComponent(typeof(Collider))]
public sealed class LavaDamage : MonoBehaviour
{
    [SerializeField, Min(0f)] private float damagePerTick = 10f;
    [SerializeField, Min(.01f)] private float damageInterval = 1f;

    private readonly Dictionary<PlayerHealth, int> overlapCounts = new();
    private readonly Dictionary<PlayerHealth, Coroutine> damageRoutines = new();

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player == null)
            return;

        overlapCounts.TryGetValue(player, out int overlapCount);
        overlapCounts[player] = overlapCount + 1;

        if (overlapCount != 0)
            return;

        ApplyDamage(player);
        damageRoutines[player] = StartCoroutine(DealDamageWhileTouching(player));
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player == null || !overlapCounts.TryGetValue(player, out int overlapCount))
            return;

        overlapCount--;
        if (overlapCount > 0)
        {
            overlapCounts[player] = overlapCount;
            return;
        }

        overlapCounts.Remove(player);
        if (damageRoutines.TryGetValue(player, out Coroutine routine))
            StopCoroutine(routine);
        damageRoutines.Remove(player);
    }

    private IEnumerator DealDamageWhileTouching(PlayerHealth player)
    {
        while (player != null && overlapCounts.ContainsKey(player))
        {
            yield return new WaitForSeconds(damageInterval);

            if (player != null && overlapCounts.ContainsKey(player))
                ApplyDamage(player);
        }

        damageRoutines.Remove(player);
    }

    private void ApplyDamage(PlayerHealth player)
    {
        if (player != null && !player.playerDead)
            player.ManageHealth(-damagePerTick);
    }

    private void OnDisable()
    {
        foreach (Coroutine routine in damageRoutines.Values)
            if (routine != null)
                StopCoroutine(routine);

        damageRoutines.Clear();
        overlapCounts.Clear();
    }
}
