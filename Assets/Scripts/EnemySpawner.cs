using QFSW.MOP2;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Analytics.Internal;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemyItem
    {
        public string name;
        public List<ObjectPool> enemies = new List<ObjectPool>();
    }

    [Header("Enemies")]
    public List<EnemyItem> enemyItems = new List<EnemyItem>();

    [Header("Enemies")]
    public List<GameObject> spawnedEnemies = new List<GameObject>();

    [Header("Spawn Positions")]
    public List<Transform> spawnPositions = new List<Transform>();
    public GameObject player;
    public int activeSet;

    private void Start()
    {
        player = FindAnyObjectByType<PlayerMovement>().gameObject;


        activeSet = GetNumberForLevel();
        
    }

    [Header("Level Settings")]
    public int levelsPerEntry = 3; // Number of levels per chunk
    public int maxNumber = 10;     // Maximum number to return

    public int GetNumberForLevel()
    {
        int playerLevel = ScoreManager.Instance.level;

        if (playerLevel < 0)
            return 0; // fallback for negative levels

        // Calculate which "chunk" the level belongs to, starting from 0
        int number = playerLevel / levelsPerEntry;

        // Clamp to maximum number
        number = Mathf.Clamp(number, 0, maxNumber);

        return number;
    }
    private void Update()
    {
        SpawnWhenInRange();
    }
    private float spawnRange = 100f;
    public bool spawned;
    public void SpawnWhenInRange()
    {
        if (!spawned)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < spawnRange)
            {
                SpawnEnemies();
                spawned = true;
            }
        }

    }
    private void SpawnEnemies()
    {
        if (enemyItems.Count == 0 || spawnPositions.Count == 0)
        {
            Debug.LogWarning("No enemies or spawn positions assigned!");
            return;
        }

        // Loop through each spawn position
        for (int i = 0; i < spawnPositions.Count; i++)
        {
            Transform spawnPos = spawnPositions[i];
            if (spawnPos == null) continue;

            // Choose a random EnemyItem
            EnemyItem randomItem = enemyItems[activeSet];
            if (randomItem.enemies.Count == 0) continue;

            // Choose a random enemy from that item
            ObjectPool enemyPrefab = randomItem.enemies[Random.Range(0, randomItem.enemies.Count)];
            if (enemyPrefab != null)
            {               
                GameObject enemy = enemyPrefab.GetObject();
                enemy.transform.position = spawnPos.position;
                spawnedEnemies.Add(enemy);
            }
        }
    }
}
