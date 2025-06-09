using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;

[System.Serializable]
public class EnemySpawnData
{
    public NetworkEnemy enemyPrefab;
    public string enemyName;
    public int spawnWeight = 1; // น้ำหนักการสุ่ม (ยิ่งมากยิ่งมีโอกาสมาก)
    public int maxCount = -1; // จำนวนสูงสุดของ enemy นี้ (-1 = ไม่จำกัด)
    public float spawnCooldown = 0f; // เวลาต้องรอก่อน spawn ตัวนี้อีกครั้ง

    [HideInInspector]
    public float lastSpawnTime = 0f; // เวลาที่ spawn ครั้งล่าสุด
    [HideInInspector]
    public int currentCount = 0; // จำนวนปัจจุบันที่ spawn แล้ว
}

public class EnemySpawner : NetworkBehaviour
{
    [Header("Enemy Prefabs")]
    public EnemySpawnData[] enemyPrefabs;

    [Header("Spawn Settings")]
    public int maxTotalEnemies = 10;
    public float spawnInterval = 5f;
    public bool randomizeSpawnInterval = true;
    public float spawnIntervalVariation = 2f; // ±2 วินาที

    [Header("Spawn Points")]
    public Transform[] spawnPoints;
    public float spawnRadius = 10f;
    public bool useSpawnPoints = true; // ใช้ spawn points หรือสุ่มในรัศมี

    [Header("Advanced Settings")]
    public bool balanceEnemyTypes = false; // พยายามสุ่มให้ครบทุกประเภท
    public bool spawnInWaves = false; // spawn เป็น wave
    public int enemiesPerWave = 3;
    public float waveCooldown = 10f;

    private float nextSpawnTime = 0f;
    private float nextWaveTime = 0f;
    private List<NetworkEnemy> activeEnemies = new List<NetworkEnemy>();
    private List<int> availableSpawnPoints = new List<int>();

    // สถิติ
    private Dictionary<string, int> spawnedCounts = new Dictionary<string, int>();

    private void Start()
    {
        // ถ้ายังไม่มี Runner reference ให้หา
        if (Runner == null)
        {
            var networkRunner = FindObjectOfType<NetworkRunner>();
            if (networkRunner != null)
            {
                Debug.Log("EnemySpawner found NetworkRunner");
            }
        }

        // เตรียม available spawn points
        if (spawnPoints != null)
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                availableSpawnPoints.Add(i);
            }
        }

        // Initialize spawn counts
        InitializeSpawnCounts();

        ValidateSettings();
    }

    public override void FixedUpdateNetwork()
    {
        // Only Host/Server spawns enemies
        if (Runner == null || !Runner.IsServer) return;

        // Clean up dead enemies
        CleanupDeadEnemies();

        // Check wave spawning
        if (spawnInWaves)
        {
            HandleWaveSpawning();
        }
        else
        {
            HandleNormalSpawning();
        }
    }

    private void HandleNormalSpawning()
    {
        if (Time.time >= nextSpawnTime && CanSpawnMore())
        {
            EnemySpawnData selectedEnemy = SelectEnemyToSpawn();
            if (selectedEnemy != null)
            {
                SpawnEnemy(selectedEnemy);
                UpdateNextSpawnTime();
            }
        }
    }

    private void HandleWaveSpawning()
    {
        if (Time.time >= nextWaveTime && CanSpawnMore())
        {
            SpawnWave();
            nextWaveTime = Time.time + waveCooldown;
        }
    }

    private void SpawnWave()
    {
        int enemiesToSpawn = Mathf.Min(enemiesPerWave, maxTotalEnemies - activeEnemies.Count);

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            EnemySpawnData selectedEnemy = SelectEnemyToSpawn();
            if (selectedEnemy != null)
            {
                SpawnEnemy(selectedEnemy);
            }
        }

        Debug.Log($"[EnemySpawner] Spawned wave of {enemiesToSpawn} enemies");
    }

    private EnemySpawnData SelectEnemyToSpawn()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("[EnemySpawner] No enemy prefabs configured!");
            return null;
        }

        List<EnemySpawnData> validEnemies = new List<EnemySpawnData>();

        // กรองเฉพาะ enemy ที่สามารถ spawn ได้
        foreach (EnemySpawnData enemy in enemyPrefabs)
        {
            if (CanSpawnEnemy(enemy))
            {
                // เพิ่มตาม weight
                for (int i = 0; i < enemy.spawnWeight; i++)
                {
                    validEnemies.Add(enemy);
                }
            }
        }

        if (validEnemies.Count == 0)
        {
            return null;
        }

        // Balance enemy types ถ้าเปิดใช้งาน
        if (balanceEnemyTypes)
        {
            return SelectBalancedEnemy();
        }

        // สุ่มเลือก
        return validEnemies[Random.Range(0, validEnemies.Count)];
    }

    private EnemySpawnData SelectBalancedEnemy()
    {
        // หา enemy ที่มีจำนวนน้อยที่สุด
        EnemySpawnData leastSpawned = null;
        int minCount = int.MaxValue;

        foreach (EnemySpawnData enemy in enemyPrefabs)
        {
            if (CanSpawnEnemy(enemy) && enemy.currentCount < minCount)
            {
                minCount = enemy.currentCount;
                leastSpawned = enemy;
            }
        }

        return leastSpawned;
    }

    private bool CanSpawnEnemy(EnemySpawnData enemy)
    {
        if (enemy.enemyPrefab == null) return false;

        // เช็ค cooldown
        if (Time.time < enemy.lastSpawnTime + enemy.spawnCooldown) return false;

        // เช็ค max count
        if (enemy.maxCount > 0 && enemy.currentCount >= enemy.maxCount) return false;

        return true;
    }

    private bool CanSpawnMore()
    {
        return activeEnemies.Count < maxTotalEnemies;
    }

    private void SpawnEnemy(EnemySpawnData enemyData)
    {
        if (Runner == null || !Runner.IsServer)
        {
            Debug.LogWarning("[EnemySpawner] Runner is not server or null. Cannot spawn.");
            return;
        }

        Vector3 spawnPosition = GetRandomSpawnPosition();
        NetworkEnemy enemy = Runner.Spawn(enemyData.enemyPrefab, spawnPosition, Quaternion.identity, PlayerRef.None);

        if (enemy != null)
        {
            activeEnemies.Add(enemy);

            // อัพเดทสถิติ
            enemyData.currentCount++;
            enemyData.lastSpawnTime = Time.time;

            // อัพเดทสถิติรวม
            if (spawnedCounts.ContainsKey(enemyData.enemyName))
            {
                spawnedCounts[enemyData.enemyName]++;
            }
            else
            {
                spawnedCounts[enemyData.enemyName] = 1;
            }

            Debug.Log($"[EnemySpawner] Spawned {enemyData.enemyName} at {spawnPosition}. " +
                     $"Active: {activeEnemies.Count}/{maxTotalEnemies}");
        }
        else
        {
            Debug.LogWarning("[EnemySpawner] Runner.Spawn returned null.");
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        if (useSpawnPoints && spawnPoints != null && spawnPoints.Length > 0)
        {
            // Use predefined spawn points
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            return spawnPoint.position;
        }
        else
        {
            // Random position in radius
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            return transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        }
    }

    private void UpdateNextSpawnTime()
    {
        float interval = spawnInterval;

        if (randomizeSpawnInterval)
        {
            interval += Random.Range(-spawnIntervalVariation, spawnIntervalVariation);
            interval = Mathf.Max(interval, 0.5f); // อย่าให้น้อยกว่า 0.5 วินาที
        }

        nextSpawnTime = Time.time + interval;
    }

    private void CleanupDeadEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] == null || activeEnemies[i].IsDead)
            {
                // ลด count ของ enemy type นั้น
                NetworkEnemy deadEnemy = activeEnemies[i];
                if (deadEnemy != null)
                {
                    // หา enemy data ที่ตรงกัน
                    foreach (EnemySpawnData enemyData in enemyPrefabs)
                    {
                        if (enemyData.enemyPrefab == deadEnemy)
                        {
                            enemyData.currentCount = Mathf.Max(0, enemyData.currentCount - 1);
                            break;
                        }
                    }
                }

                activeEnemies.RemoveAt(i);
            }
        }
    }

    private void InitializeSpawnCounts()
    {
        spawnedCounts.Clear();

        foreach (EnemySpawnData enemy in enemyPrefabs)
        {
            if (!string.IsNullOrEmpty(enemy.enemyName))
            {
                spawnedCounts[enemy.enemyName] = 0;
            }
            enemy.currentCount = 0;
            enemy.lastSpawnTime = 0f;
        }
    }

    private void ValidateSettings()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogError("[EnemySpawner] No enemy prefabs configured! Please add enemy prefabs to spawn.");
            return;
        }

        foreach (EnemySpawnData enemy in enemyPrefabs)
        {
            if (enemy.enemyPrefab == null)
            {
                Debug.LogWarning($"[EnemySpawner] Enemy prefab is null for '{enemy.enemyName}'");
            }

            if (string.IsNullOrEmpty(enemy.enemyName))
            {
                enemy.enemyName = enemy.enemyPrefab != null ? enemy.enemyPrefab.name : "Unknown";
            }
        }
    }

    // ========== Public Methods ==========

    /// <summary>
    /// Force spawn enemy ประเภทเฉพาะ
    /// </summary>
    public void ForceSpawnEnemy(string enemyName)
    {
        if (!Runner.IsServer) return;

        foreach (EnemySpawnData enemy in enemyPrefabs)
        {
            if (enemy.enemyName == enemyName && enemy.enemyPrefab != null)
            {
                SpawnEnemy(enemy);
                break;
            }
        }
    }

    /// <summary>
    /// หยุด spawn ชั่วคราว
    /// </summary>
    public void PauseSpawning(float duration)
    {
        nextSpawnTime = Time.time + duration;
        nextWaveTime = Time.time + duration;
    }

    /// <summary>
    /// Clear enemies ทั้งหมด
    /// </summary>
    public void ClearAllEnemies()
    {
        if (!Runner.IsServer) return;

        foreach (NetworkEnemy enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Runner.Despawn(enemy.Object);
            }
        }

        activeEnemies.Clear();
        InitializeSpawnCounts();
    }

    /// <summary>
    /// ดูสถิติการ spawn
    /// </summary>
    public Dictionary<string, int> GetSpawnStatistics()
    {
        return new Dictionary<string, int>(spawnedCounts);
    }

    // ========== Debug Methods ==========

    [ContextMenu("Log Spawn Statistics")]
    public void LogSpawnStatistics()
    {
        Debug.Log("=== Enemy Spawn Statistics ===");
        Debug.Log($"Active Enemies: {activeEnemies.Count}/{maxTotalEnemies}");

        foreach (var kvp in spawnedCounts)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value} spawned");
        }

        foreach (EnemySpawnData enemy in enemyPrefabs)
        {
            Debug.Log($"{enemy.enemyName}: Current={enemy.currentCount}, Max={enemy.maxCount}, Weight={enemy.spawnWeight}");
        }
    }

    [ContextMenu("Force Spawn Wave")]
    public void ForceSpawnWave()
    {
        if (Application.isPlaying && Runner != null && Runner.IsServer)
        {
            SpawnWave();
        }
    }

    [ContextMenu("Clear All Enemies")]
    public void DebugClearAllEnemies()
    {
        if (Application.isPlaying)
        {
            ClearAllEnemies();
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw spawn radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

        // Draw spawn points
        if (spawnPoints != null)
        {
            Gizmos.color = Color.yellow;
            foreach (Transform point in spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawSphere(point.position, 0.5f);
                }
            }
        }

        // Draw connections between spawner and spawn points
        if (useSpawnPoints && spawnPoints != null)
        {
            Gizmos.color = Color.green;
            foreach (Transform point in spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawLine(transform.position, point.position);
                }
            }
        }
    }
}