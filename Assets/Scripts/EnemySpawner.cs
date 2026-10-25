using QFSW.MOP2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawn
    {
        public ObjectPool enemyPool;
        [Min(1)]
        public int count = 1;
    }

    [System.Serializable]
    public class SetSpawnPoint
    {
        public Transform point;
        public List<EnemySpawn> enemies = new List<EnemySpawn>();
    }

    [System.Serializable]
    public class SpawnSet
    {
        public string name = "Set 0";
        public List<SetSpawnPoint> spawnPoints = new List<SetSpawnPoint>();
        [Tooltip("Enemies available in this game set. Their weights determine how often they are chosen.")]
        public List<WeightedEnemy> enemyChoices = new List<WeightedEnemy>();
    }

    [System.Serializable]
    public class WeightedEnemy
    {
        public string name;
        public ObjectPool enemyPool;
        [Min(0f)]
        public float weight = 1f;
    }

    [Header("Spawn Sets")]
    public List<SpawnSet> spawnSets = new List<SpawnSet>();

    [Header("Spawned Enemies")]
    public List<GameObject> spawnedEnemies = new List<GameObject>();

    [Header("Spawn Settings")]
    public float spawnRange = 100f;
    public float spawnSpreadRadius = 1.5f;
    public float navMeshSearchDistance = 2f;
    [Tooltip("Keeps the spawn point's authored height while snapping its horizontal position to the NavMesh. Use this for rooms whose visible floor sits above the baked NavMesh surface.")]
    public bool preserveSpawnPointHeight = false;

    [Header("Level Settings")]
    public int maxNumber = 9;

    [Tooltip("Optional offset applied to the selected procedural set. The room prefabs now contain the consecutive enemy progression, so this remains zero.")]
    [Min(0)]
    public int enemySetAdvance = 0;

    [HideInInspector]
    public int activeSet;

    private GameObject player;
    
    private bool activationAllowed = true;
private bool activated;

    private void Start()
    {
        player = FindAnyObjectByType<PlayerMovement>().gameObject;
        activeSet = GetNumberForLevel();

        SpawnEnemies();
    }

    private void Update()
    {
        if (activated || !activationAllowed)
            return;

        if (player == null)
            return;

        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (distance <= spawnRange)
        {
            activated = true;

            foreach (GameObject enemy in spawnedEnemies)
            {
                if (enemy != null)
                    enemy.SetActive(true);
            }
        }
    }

    public int GetNumberForLevel()
    {
        int sourceSet;

        if (ProceduralGenerationManager.Instance != null)
            sourceSet = ProceduralGenerationManager.Instance.ActiveSet;
        else if (ScoreManager.Instance != null)
            sourceSet = Mathf.Max(0, ScoreManager.Instance.level);
        else
            sourceSet = 0;

        // The room prefabs contain the complete consecutive progression.
        // Keep this offset available for tuning, but do not skip the early
        // sets during normal gameplay.
        return Mathf.Clamp(sourceSet + enemySetAdvance, 0, maxNumber);
    }

    private void SpawnEnemies()
    {
        spawnedEnemies.Clear();

        if (spawnSets.Count == 0)
            return;

        int setIndex = Mathf.Clamp(activeSet, 0, spawnSets.Count - 1);

        SpawnSet currentSet = spawnSets[setIndex];
        List<ObjectPool> distinctPools = new List<ObjectPool>();

        foreach (SetSpawnPoint spawnPoint in currentSet.spawnPoints)
        {
            if (spawnPoint.point == null)
                continue;

            foreach (EnemySpawn enemySpawn in spawnPoint.enemies)
            {
                for (int i = 0; i < enemySpawn.count; i++)
                {
                    // The first two placements use different pools whenever this set has at least two choices.
                    // This keeps a room from becoming a single-enemy encounter.
                    ObjectPool enemyPool = GetWeightedEnemyPool(currentSet, distinctPools);
                    if (enemyPool == null)
                        enemyPool = enemySpawn.enemyPool;

                    if (enemyPool == null)
                        continue;

                    if (distinctPools.Count < 2 && !distinctPools.Contains(enemyPool))
                        distinctPools.Add(enemyPool);

                    GameObject enemy = enemyPool.GetObject();

                    // Disable first so OnEnable doesn't run while repositioning.
                    enemy.SetActive(false);

                    Vector3 desiredPosition = spawnPoint.point.position;

                    if (enemySpawn.count > 1)
                    {
                        Vector2 offset = Random.insideUnitCircle * spawnSpreadRadius;
                        desiredPosition += new Vector3(offset.x, 0f, offset.y);
                    }

                    NavMeshHit hit;

                    if (NavMesh.SamplePosition(desiredPosition, out hit, navMeshSearchDistance, NavMesh.AllAreas))
                    {
                        enemy.transform.position = GetSpawnPosition(hit.position, desiredPosition.y);
                    }
                    else if (NavMesh.SamplePosition(spawnPoint.point.position, out hit, navMeshSearchDistance, NavMesh.AllAreas))
                    {
                        enemy.transform.position = GetSpawnPosition(hit.position, spawnPoint.point.position.y);
                    }
                    else
                    {
                        enemy.transform.position = spawnPoint.point.position;
                    }

                    if (player != null)
                    {
                        Vector3 directionToPlayer = player.transform.position - enemy.transform.position;
                        directionToPlayer.y = 0f;

                        if (directionToPlayer.sqrMagnitude > 0.0001f)
                        {
                            enemy.transform.rotation = Quaternion.LookRotation(directionToPlayer);
                        }
                        else
                        {
                            enemy.transform.rotation = spawnPoint.point.rotation;
                        }
                    }
                    else
                    {
                        enemy.transform.rotation = spawnPoint.point.rotation;
                    }

                    spawnedEnemies.Add(enemy);
                }
            }
        }
    }

    private Vector3 GetSpawnPosition(Vector3 navMeshPosition, float authoredHeight)
    {
        if (preserveSpawnPointHeight)
            navMeshPosition.y = authoredHeight;

        return navMeshPosition;
    }

    /// <summary>
    /// Repositions this room's encounter once its generated NavMesh is ready.
    /// Prepared rooms are built offstage, where no usable NavMesh exists yet.
    /// </summary>
    public void RespawnForRebuiltNavMesh()
    {
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
                enemy.SetActive(false);
        }

        activated = false;
        activeSet = GetNumberForLevel();
        SpawnEnemies();
    }

public void SetActivationAllowed(bool allowed)
    {
        activationAllowed = allowed;
        if (allowed)
            return;

        activated = false;
        foreach (GameObject enemy in spawnedEnemies)
            if (enemy != null)
                enemy.SetActive(false);
    }


    private ObjectPool GetWeightedEnemyPool(SpawnSet spawnSet, List<ObjectPool> excludedPools)
    {
        if (spawnSet.enemyChoices == null || spawnSet.enemyChoices.Count == 0)
            return null;

        bool requireDifferentEnemy = excludedPools.Count < 2 && spawnSet.enemyChoices.Count > excludedPools.Count;
        float totalWeight = 0f;

        foreach (WeightedEnemy choice in spawnSet.enemyChoices)
        {
            if (choice.enemyPool == null || choice.weight <= 0f)
                continue;

            if (requireDifferentEnemy && excludedPools.Contains(choice.enemyPool))
                continue;

            totalWeight += choice.weight;
        }

        if (totalWeight <= 0f)
            return null;

        float roll = Random.value * totalWeight;

        foreach (WeightedEnemy choice in spawnSet.enemyChoices)
        {
            if (choice.enemyPool == null || choice.weight <= 0f)
                continue;

            if (requireDifferentEnemy && excludedPools.Contains(choice.enemyPool))
                continue;

            roll -= choice.weight;
            if (roll <= 0f)
                return choice.enemyPool;
        }

        return null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRange);

        Gizmos.color = Color.red;

        foreach (SpawnSet spawnSet in spawnSets)
        {
            foreach (SetSpawnPoint spawnPoint in spawnSet.spawnPoints)
            {
                if (spawnPoint.point == null)
                    continue;

                Gizmos.DrawSphere(spawnPoint.point.position, 0.2f);
                Gizmos.DrawWireSphere(spawnPoint.point.position, spawnSpreadRadius);
            }
        }
    }
#endif
}
