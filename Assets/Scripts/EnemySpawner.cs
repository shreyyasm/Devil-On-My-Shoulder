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
        public string name = "Set";
        public List<SetSpawnPoint> spawnPoints = new List<SetSpawnPoint>();
    }

    [Header("Spawn Sets")]
    public List<SpawnSet> spawnSets = new List<SpawnSet>();

    [Header("Spawned Enemies")]
    public List<GameObject> spawnedEnemies = new List<GameObject>();

    [Header("Spawn Settings")]
    public float spawnRange = 100f;
    public float spawnSpreadRadius = 1.5f;
    public float navMeshSearchDistance = 2f;

    [Header("Level Settings")]
    public int levelsPerEntry = 3;
    public int maxNumber = 10;

    [HideInInspector]
    public int activeSet;

    private GameObject player;
    private bool activated;

    private void Start()
    {
        player = FindAnyObjectByType<PlayerMovement>().gameObject;

        activeSet = GetNumberForLevel();

        SpawnEnemies();
    }

    private void Update()
    {
        if (activated)
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
        int playerLevel = ScoreManager.Instance.level;

        if (playerLevel < 0)
            return 0;

        int number = playerLevel / levelsPerEntry;
        number = Mathf.Clamp(number, 0, maxNumber);

        return number;
    }

    private void SpawnEnemies()
    {
        spawnedEnemies.Clear();

        if (spawnSets.Count == 0)
            return;

        int setIndex = Mathf.Clamp(activeSet, 0, spawnSets.Count - 1);

        SpawnSet currentSet = spawnSets[setIndex];

        foreach (SetSpawnPoint spawnPoint in currentSet.spawnPoints)
        {
            if (spawnPoint.point == null)
                continue;

            foreach (EnemySpawn enemySpawn in spawnPoint.enemies)
            {
                if (enemySpawn.enemyPool == null)
                    continue;

                for (int i = 0; i < enemySpawn.count; i++)
                {
                    GameObject enemy = enemySpawn.enemyPool.GetObject();

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
                        enemy.transform.position = hit.position;
                    }
                    else if (NavMesh.SamplePosition(spawnPoint.point.position, out hit, navMeshSearchDistance, NavMesh.AllAreas))
                    {
                        enemy.transform.position = hit.position;
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