using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using UnityEngine.SceneManagement;

[System.Serializable]
public class EnemySpawnData
{
    public NetworkEnemy enemyPrefab;
    public string enemyName;
    public int spawnWeight = 1;
    public int maxCount = -1;
    public float spawnCooldown = 0f;

    [HideInInspector]
    public float lastSpawnTime = 0f;
    [HideInInspector]
    public int currentCount = 0;
}

[System.Serializable]
public class BossSpawnCondition
{
    public NetworkEnemy bossPrefab;
    public string bossName;
    public int enemiesToKill = 50;
    public bool includeSpecificEnemies = false;
    public string[] specificEnemyNames;
    public float bossRespawnCooldown = 300f;
    public int maxBossInstances = 1;
    public bool announceSpawn = true;
    public float spawnWarningTime = 5f;

    [HideInInspector]
    public int currentKillCount = 0;
    [HideInInspector]
    public float lastBossDeathTime = 0f;
    [HideInInspector]
    public int currentBossCount = 0;
    [HideInInspector]
    public bool isSpawningBoss = false;
}

public class EnemySpawner : NetworkBehaviour
{
    [Header("Enemy Prefabs")]
    public EnemySpawnData[] enemyPrefabs;

    [Header("🏆 Boss Spawn Conditions")]
    public BossSpawnCondition[] bossConditions;
    public bool enableBossSpawning = true;
    public Transform[] bossSpawnPoints;
    public bool useBossSpawnPoints = true;

    [Header("Spawn Settings")]
    public int maxTotalEnemies = 10;
    public float spawnInterval = 5f;
    public bool randomizeSpawnInterval = true;
    public float spawnIntervalVariation = 2f;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;
    public float spawnRadius = 10f;
    public bool useSpawnPoints = true;

    [Header("🔧 Debug Settings")]
    public bool showDebugInfo = false;
    public bool verboseKillTracking = true; // เพิ่มการ debug kill tracking

    [Header("Advanced Settings")]
    public bool balanceEnemyTypes = false;
    public bool spawnInWaves = false;
    public int enemiesPerWave = 3;
    public float waveCooldown = 10f;

    private float nextSpawnTime = 0f;
    private float nextWaveTime = 0f;
    private List<NetworkEnemy> activeEnemies = new List<NetworkEnemy>();
    private List<NetworkEnemy> activeBosses = new List<NetworkEnemy>();

    // 🔧 ปรับปรุงระบบ spawn points
    private List<int> availableSpawnPoints = new List<int>();
    private int lastUsedSpawnPoint = -1; // เพื่อป้องกันการใช้จุดเดิมซ้ำ

    // สถิติ
    private Dictionary<string, int> spawnedCounts = new Dictionary<string, int>();
    private Dictionary<string, int> killedCounts = new Dictionary<string, int>();
    private int totalEnemiesKilled = 0;

    // 🔧 เพิ่มระบบติดตาม enemy ที่ spawn
    private Dictionary<NetworkObject, string> spawnedEnemyTypes = new Dictionary<NetworkObject, string>();

    private void Start()
    {
        if (Runner == null)
        {
            var networkRunner = FindObjectOfType<NetworkRunner>();
            if (networkRunner != null)
            {
                Debug.Log("EnemySpawner found NetworkRunner");
            }
        }

        InitializeSpawnPoints();
        InitializeSpawnCounts();
        InitializeBossConditions();
        ValidateSettings();
    }

    // 🔧 ปรับปรุงการจัดการ spawn points
    private void InitializeSpawnPoints()
    {
        availableSpawnPoints.Clear();

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    availableSpawnPoints.Add(i);
                }
                else
                {
                    Debug.LogWarning($"[EnemySpawner] Spawn point {i} is null!");
                }
            }

            if (showDebugInfo)
            {
                Debug.Log($"[EnemySpawner] Initialized {availableSpawnPoints.Count} valid spawn points out of {spawnPoints.Length}");
            }
        }
        else
        {
            Debug.LogWarning("[EnemySpawner] No spawn points configured! Will use random positions.");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Runner == null || !Runner.IsServer) return;

        CleanupDeadEnemies();
        CleanupDeadBosses();

        if (enableBossSpawning)
        {
            CheckBossSpawnConditions();
        }

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

        foreach (EnemySpawnData enemy in enemyPrefabs)
        {
            if (CanSpawnEnemy(enemy))
            {
                for (int i = 0; i < enemy.spawnWeight; i++)
                {
                    validEnemies.Add(enemy);
                }
            }
        }

        if (validEnemies.Count == 0) return null;

        if (balanceEnemyTypes)
        {
            return SelectBalancedEnemy();
        }

        return validEnemies[Random.Range(0, validEnemies.Count)];
    }

    private EnemySpawnData SelectBalancedEnemy()
    {
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
        if (Time.time < enemy.lastSpawnTime + enemy.spawnCooldown) return false;
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

            // 🔧 เพิ่มการติดตาม enemy type ที่ spawn
            spawnedEnemyTypes[enemy.Object] = enemyData.enemyName;

            enemyData.currentCount++;
            enemyData.lastSpawnTime = Time.time;

            if (spawnedCounts.ContainsKey(enemyData.enemyName))
            {
                spawnedCounts[enemyData.enemyName]++;
            }
            else
            {
                spawnedCounts[enemyData.enemyName] = 1;
            }

            if (showDebugInfo)
            {
                Debug.Log($"[EnemySpawner] Spawned {enemyData.enemyName} at {spawnPosition}. " +
                         $"Active: {activeEnemies.Count}/{maxTotalEnemies}");
            }
        }
        else
        {
            Debug.LogWarning("[EnemySpawner] Runner.Spawn returned null.");
        }
    }

    // 🔧 ปรับปรุงระบบ spawn position
    private Vector3 GetRandomSpawnPosition()
    {
        if (useSpawnPoints && spawnPoints != null && availableSpawnPoints.Count > 0)
        {
            // 🔧 ใช้ระบบหมุนเวียนจุด spawn เพื่อให้ใช้ครบทุกจุด
            int selectedIndex;

            if (availableSpawnPoints.Count == 1)
            {
                selectedIndex = 0;
            }
            else if (balanceEnemyTypes) // ถ้า balance ให้ใช้จุดแบบหมุนเวียน
            {
                // หาจุดถัดไปจาก lastUsedSpawnPoint
                int currentIndex = availableSpawnPoints.IndexOf(lastUsedSpawnPoint);
                int nextIndex = (currentIndex + 1) % availableSpawnPoints.Count;
                selectedIndex = nextIndex;
            }
            else
            {
                // สุ่มแบบปกติ แต่พยายามไม่ใช้จุดเดิม
                List<int> otherPoints = new List<int>(availableSpawnPoints);
                if (lastUsedSpawnPoint >= 0 && otherPoints.Count > 1)
                {
                    otherPoints.Remove(lastUsedSpawnPoint);
                }
                selectedIndex = Random.Range(0, otherPoints.Count);
                selectedIndex = availableSpawnPoints.IndexOf(otherPoints[selectedIndex]);
            }

            int pointIndex = availableSpawnPoints[selectedIndex];
            lastUsedSpawnPoint = pointIndex;

            Vector3 position = spawnPoints[pointIndex].position;

            if (showDebugInfo)
            {
                Debug.Log($"[EnemySpawner] Using spawn point {pointIndex} at {position}");
            }

            return position;
        }
        else
        {
            // Random position in radius
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 position = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            if (showDebugInfo)
            {
                Debug.Log($"[EnemySpawner] Using random position at {position}");
            }

            return position;
        }
    }

    private void UpdateNextSpawnTime()
    {
        float interval = spawnInterval;

        if (randomizeSpawnInterval)
        {
            interval += Random.Range(-spawnIntervalVariation, spawnIntervalVariation);
            interval = Mathf.Max(interval, 0.5f);
        }

        nextSpawnTime = Time.time + interval;
    }

    // 🔧 ปรับปรุงการนับ kill count
    private void CleanupDeadEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            NetworkEnemy enemy = activeEnemies[i];

            if (enemy == null || enemy.IsDead)
            {
                string enemyTypeName = "";

                // 🔧 ใช้ระบบติดตามที่ปรับปรุงแล้ว
                if (enemy != null && enemy.Object != null && spawnedEnemyTypes.ContainsKey(enemy.Object))
                {
                    enemyTypeName = spawnedEnemyTypes[enemy.Object];
                    spawnedEnemyTypes.Remove(enemy.Object); // ลบออกจาก tracking
                }
                else if (enemy != null)
                {
                    // Fallback: ใช้วิธีเดิมแต่ปรับปรุง
                    string enemyName = enemy.name.Replace("(Clone)", "").Trim();

                    foreach (EnemySpawnData enemyData in enemyPrefabs)
                    {
                        if (enemyData.enemyPrefab != null &&
                            (enemyData.enemyPrefab.name == enemyName ||
                             enemyData.enemyName == enemyName ||
                             enemyData.enemyPrefab.name.Contains(enemyName) ||
                             enemyName.Contains(enemyData.enemyPrefab.name)))
                        {
                            enemyTypeName = enemyData.enemyName;
                            enemyData.currentCount = Mathf.Max(0, enemyData.currentCount - 1);
                            break;
                        }
                    }
                }

                // 🔧 บันทึกการฆ่า enemy
                if (!string.IsNullOrEmpty(enemyTypeName))
                {
                    RecordEnemyKill(enemyTypeName);
                    EnemyKillTracker.OnEnemyKilled();

                    if (verboseKillTracking)
                    {
                        Debug.Log($"💀 {enemyTypeName} killed! Total: {totalEnemiesKilled}");
                    }
                }
                else if (verboseKillTracking)
                {
                    Debug.LogWarning($"[EnemySpawner] Could not identify enemy type for kill tracking! Enemy name: {(enemy != null ? enemy.name : "null")}");
                }

                activeEnemies.RemoveAt(i);
            }
        }
    }

    private void CleanupDeadBosses()
    {
        for (int i = activeBosses.Count - 1; i >= 0; i--)
        {
            NetworkEnemy boss = activeBosses[i];

            if (boss == null || boss.IsDead)
            {
                if (boss != null)
                {
                    string bossName = boss.name.Replace("(Clone)", "").Trim();

                    foreach (BossSpawnCondition condition in bossConditions)
                    {
                        if (condition.bossPrefab != null &&
                            (condition.bossPrefab.name == bossName ||
                             condition.bossName == bossName ||
                             condition.bossPrefab.name.Contains(bossName) ||
                             bossName.Contains(condition.bossPrefab.name)))
                        {
                            condition.currentBossCount = Mathf.Max(0, condition.currentBossCount - 1);
                            condition.lastBossDeathTime = Time.time;

                            RPC_AnnounceBossDefeated(condition.bossName);
                            Debug.Log($"🏆 Boss {condition.bossName} defeated! Cooldown started.");
                            break;
                        }
                    }
                }

                activeBosses.RemoveAt(i);
            }
        }
    }

    // 🔧 ปรับปรุงการนับ kill
    private void RecordEnemyKill(string enemyName)
    {
        totalEnemiesKilled++;

        if (killedCounts.ContainsKey(enemyName))
        {
            killedCounts[enemyName]++;
        }
        else
        {
            killedCounts[enemyName] = 1;
        }

        // 🔧 แสดงสถิติทุกๆ 5 kills
        if (verboseKillTracking && totalEnemiesKilled % 5 == 0)
        {
            Debug.Log($"📊 Kill Stats - Total: {totalEnemiesKilled}, {enemyName}: {killedCounts[enemyName]}");
        }

        UpdateBossKillCounts(enemyName);
    }

    // 🔧 ปรับปรุงการอัปเดต boss kill counts
    private void UpdateBossKillCounts(string killedEnemyName)
    {
        foreach (BossSpawnCondition condition in bossConditions)
        {
            bool shouldCount = false;

            if (condition.includeSpecificEnemies)
            {
                if (condition.specificEnemyNames != null)
                {
                    foreach (string specificName in condition.specificEnemyNames)
                    {
                        if (specificName == killedEnemyName)
                        {
                            shouldCount = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                shouldCount = true; // นับทุก enemy
            }

            if (shouldCount)
            {
                condition.currentKillCount++;

                // 🔧 แสดง progress ของบอส
                if (verboseKillTracking && condition.currentKillCount % 5 == 0)
                {
                    int remaining = condition.enemiesToKill - condition.currentKillCount;
                    float progress = (float)condition.currentKillCount / condition.enemiesToKill * 100f;

                    Debug.Log($"🎯 Boss {condition.bossName} progress: {condition.currentKillCount}/{condition.enemiesToKill} " +
                             $"({progress:F1}%) - Remaining: {remaining}");

                    // เตือนเมื่อใกล้จะ spawn บอส
                    if (remaining <= 5 && remaining > 0)
                    {
                        Debug.Log($"⚠️ WARNING: {condition.bossName} will spawn in {remaining} more kills!");
                    }
                }
            }
        }
    }

    private void InitializeSpawnCounts()
    {
        spawnedCounts.Clear();
        killedCounts.Clear();
        spawnedEnemyTypes.Clear();
        totalEnemiesKilled = 0;

        foreach (EnemySpawnData enemy in enemyPrefabs)
        {
            if (!string.IsNullOrEmpty(enemy.enemyName))
            {
                spawnedCounts[enemy.enemyName] = 0;
                killedCounts[enemy.enemyName] = 0;
            }
            enemy.currentCount = 0;
            enemy.lastSpawnTime = 0f;
        }

        if (showDebugInfo)
        {
            Debug.Log($"[EnemySpawner] Initialized spawn counts for {enemyPrefabs.Length} enemy types");
        }
    }

    private void InitializeBossConditions()
    {
        if (bossConditions == null) return;

        foreach (BossSpawnCondition condition in bossConditions)
        {
            condition.currentKillCount = 0;
            condition.lastBossDeathTime = 0f;
            condition.currentBossCount = 0;
            condition.isSpawningBoss = false;

            if (string.IsNullOrEmpty(condition.bossName) && condition.bossPrefab != null)
            {
                condition.bossName = condition.bossPrefab.name;
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"[EnemySpawner] Initialized {bossConditions.Length} boss conditions");
        }
    }

    // ========== Boss Spawning System ==========

    private void CheckBossSpawnConditions()
    {
        if (bossConditions == null) return;

        foreach (BossSpawnCondition condition in bossConditions)
        {
            if (ShouldSpawnBoss(condition))
            {
                if (verboseKillTracking)
                {
                    Debug.Log($"🏆 Boss {condition.bossName} conditions met! Starting spawn process...");
                }
                StartCoroutine(SpawnBossWithWarning(condition));
            }
        }
    }

    private bool ShouldSpawnBoss(BossSpawnCondition condition)
    {
        if (condition.bossPrefab == null) return false;
        if (condition.isSpawningBoss) return false;
        if (condition.currentBossCount >= condition.maxBossInstances) return false;
        if (condition.currentKillCount < condition.enemiesToKill) return false;

        // เช็ค cooldown
        if (Time.time < condition.lastBossDeathTime + condition.bossRespawnCooldown) return false;

        return true;
    }

    private IEnumerator SpawnBossWithWarning(BossSpawnCondition condition)
    {
        condition.isSpawningBoss = true;

        if (condition.announceSpawn)
        {
            RPC_AnnounceBossSpawning(condition.bossName, condition.spawnWarningTime);
            yield return new WaitForSeconds(condition.spawnWarningTime);
        }

        SpawnBoss(condition);
        condition.currentKillCount = 0; // รีเซ็ต kill count
        condition.isSpawningBoss = false;
    }

    private void SpawnBoss(BossSpawnCondition condition)
    {
        if (Runner == null || !Runner.IsServer) return;

        Vector3 bossSpawnPosition = GetBossSpawnPosition();
        NetworkEnemy boss = Runner.Spawn(condition.bossPrefab, bossSpawnPosition, Quaternion.identity, PlayerRef.None);

        if (boss != null)
        {
            activeBosses.Add(boss);
            condition.currentBossCount++;

            RPC_AnnounceBossSpawned(condition.bossName, bossSpawnPosition);

            Debug.Log($"🏆 BOSS SPAWNED: {condition.bossName} at {bossSpawnPosition}! " +
                     $"Active bosses: {activeBosses.Count}");
        }
        else
        {
            Debug.LogWarning($"[EnemySpawner] Failed to spawn boss: {condition.bossName}");
            condition.isSpawningBoss = false;
        }
    }

    private Vector3 GetBossSpawnPosition()
    {
        if (useBossSpawnPoints && bossSpawnPoints != null && bossSpawnPoints.Length > 0)
        {
            // ใช้ boss spawn points พิเศษ
            Transform bossSpawnPoint = bossSpawnPoints[Random.Range(0, bossSpawnPoints.Length)];
            return bossSpawnPoint.position;
        }
        else
        {
            // ใช้ spawn points ปกติหรือสุ่มในรัศมี
            return GetRandomSpawnPosition();
        }
    }

    // ========== RPC Methods ==========

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AnnounceBossSpawning(string bossName, float warningTime)
    {
        Debug.Log($"🚨 WARNING: {bossName} will spawn in {warningTime} seconds!");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AnnounceBossSpawned(string bossName, Vector3 position)
    {
        Debug.Log($"🏆 {bossName} HAS SPAWNED at {position}!");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AnnounceBossDefeated(string bossName)
    {
        Debug.Log($"⚔️ {bossName} HAS BEEN DEFEATED!");
    }

    private void ValidateSettings()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogError("[EnemySpawner] No enemy prefabs configured! Please add enemy prefabs to spawn.");
            return;
        }

        // 🔧 ปรับปรุงการ validate
        int validEnemies = 0;
        foreach (EnemySpawnData enemy in enemyPrefabs)
        {
            if (enemy.enemyPrefab == null)
            {
                Debug.LogWarning($"[EnemySpawner] Enemy prefab is null for '{enemy.enemyName}'");
                continue;
            }

            if (string.IsNullOrEmpty(enemy.enemyName))
            {
                enemy.enemyName = enemy.enemyPrefab.name;
                Debug.Log($"[EnemySpawner] Auto-assigned name '{enemy.enemyName}' to enemy");
            }

            validEnemies++;
        }

        Debug.Log($"[EnemySpawner] Validated {validEnemies} valid enemy types");

        // Validate spawn points
        if (useSpawnPoints)
        {
            if (spawnPoints == null || availableSpawnPoints.Count == 0)
            {
                Debug.LogWarning("[EnemySpawner] useSpawnPoints is enabled but no valid spawn points found! Will use random spawning.");
                useSpawnPoints = false;
            }
            else
            {
                Debug.Log($"[EnemySpawner] Using {availableSpawnPoints.Count} spawn points");
            }
        }

        // Validate boss conditions
        if (bossConditions != null)
        {
            int validBosses = 0;
            foreach (BossSpawnCondition condition in bossConditions)
            {
                if (condition.bossPrefab == null)
                {
                    Debug.LogWarning($"[EnemySpawner] Boss prefab is null for '{condition.bossName}'");
                    continue;
                }

                if (string.IsNullOrEmpty(condition.bossName))
                {
                    condition.bossName = condition.bossPrefab.name;
                }

                if (condition.enemiesToKill <= 0)
                {
                    Debug.LogWarning($"[EnemySpawner] Boss '{condition.bossName}' has invalid enemiesToKill: {condition.enemiesToKill}");
                    condition.enemiesToKill = 10;
                }

                if (condition.includeSpecificEnemies &&
                    (condition.specificEnemyNames == null || condition.specificEnemyNames.Length == 0))
                {
                    Debug.LogWarning($"[EnemySpawner] Boss '{condition.bossName}' requires specific enemies but none specified!");
                }

                validBosses++;
            }

            Debug.Log($"[EnemySpawner] Validated {validBosses} boss conditions");
        }
    }

    // ========== Public Methods ==========

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

    public void ForceSpawnBoss(string bossName)
    {
        if (!Runner.IsServer) return;

        foreach (BossSpawnCondition condition in bossConditions)
        {
            if (condition.bossName == bossName && condition.bossPrefab != null)
            {
                SpawnBoss(condition);
                break;
            }
        }
    }

    // 🔧 เพิ่ม method สำหรับทดสอบ
    public void AddKillCount(string enemyName, int count = 1)
    {
        if (!Runner.IsServer) return;

        for (int i = 0; i < count; i++)
        {
            RecordEnemyKill(enemyName);
        }

        Debug.Log($"[DEBUG] Added {count} kills for {enemyName}. Total killed: {totalEnemiesKilled}");
    }

    public void ResetBossKillCount(string bossName)
    {
        if (!Runner.IsServer) return;

        foreach (BossSpawnCondition condition in bossConditions)
        {
            if (condition.bossName == bossName)
            {
                condition.currentKillCount = 0;
                Debug.Log($"[DEBUG] Reset kill count for boss {bossName}");
                break;
            }
        }
    }

    public void PauseSpawning(float duration)
    {
        nextSpawnTime = Time.time + duration;
        nextWaveTime = Time.time + duration;
        Debug.Log($"[EnemySpawner] Spawning paused for {duration} seconds");
    }

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
        spawnedEnemyTypes.Clear();
        InitializeSpawnCounts();
        Debug.Log("[EnemySpawner] All enemies cleared");
    }

    public void ClearAllBosses()
    {
        if (!Runner.IsServer) return;

        foreach (NetworkEnemy boss in activeBosses)
        {
            if (boss != null)
            {
                Runner.Despawn(boss.Object);
            }
        }

        activeBosses.Clear();
        InitializeBossConditions();
        Debug.Log("[EnemySpawner] All bosses cleared");
    }

    public void ClearAll()
    {
        ClearAllEnemies();
        ClearAllBosses();
    }

    public Dictionary<string, int> GetSpawnStatistics()
    {
        return new Dictionary<string, int>(spawnedCounts);
    }

    public Dictionary<string, int> GetKillStatistics()
    {
        return new Dictionary<string, int>(killedCounts);
    }

    public Dictionary<string, float> GetBossProgress()
    {
        Dictionary<string, float> progress = new Dictionary<string, float>();

        if (bossConditions != null)
        {
            foreach (BossSpawnCondition condition in bossConditions)
            {
                float progressPercent = (float)condition.currentKillCount / condition.enemiesToKill;
                progress[condition.bossName] = Mathf.Clamp01(progressPercent);
            }
        }

        return progress;
    }
   
    // ========== Debug Methods ==========





}