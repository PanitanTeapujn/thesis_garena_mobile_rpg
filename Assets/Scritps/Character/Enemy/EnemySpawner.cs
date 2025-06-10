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

[System.Serializable]
public class BossSpawnCondition
{
    public NetworkEnemy bossPrefab;
    public string bossName;
    public int enemiesToKill = 50; // จำนวน enemy ที่ต้องฆ่าก่อน spawn บอส
    public bool includeSpecificEnemies = false; // นับเฉพาะ enemy บางประเภท
    public string[] specificEnemyNames; // enemy ที่ต้องฆ่าถ้า includeSpecificEnemies = true
    public float bossRespawnCooldown = 300f; // 5 นาที cooldown หลังบอสตาย
    public int maxBossInstances = 1; // จำนวนบอสสูงสุดที่สามารถมีพร้อมกันได้
    public bool announceSpawn = true; // ประกาศเมื่อบอสจะเกิด
    public float spawnWarningTime = 5f; // เวลาเตือนก่อนบอสเกิด (วินาที)

    [HideInInspector]
    public int currentKillCount = 0; // จำนวน enemy ที่ฆ่าไปแล้ว
    [HideInInspector]
    public float lastBossDeathTime = 0f; // เวลาที่บอสตายครั้งล่าสุด
    [HideInInspector]
    public int currentBossCount = 0; // จำนวนบอสปัจจุบัน
    [HideInInspector]
    public bool isSpawningBoss = false; // กำลัง spawn บอสอยู่
}

public class EnemySpawner : NetworkBehaviour
{
    [Header("Enemy Prefabs")]
    public EnemySpawnData[] enemyPrefabs;

    [Header("🏆 Boss Spawn Conditions")]
    public BossSpawnCondition[] bossConditions;
    public bool enableBossSpawning = true;
    public Transform[] bossSpawnPoints; // จุด spawn พิเศษสำหรับบอส
    public bool useBossSpawnPoints = true;

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
    private List<NetworkEnemy> activeBosses = new List<NetworkEnemy>();
    private List<int> availableSpawnPoints = new List<int>();

    // สถิติ
    private Dictionary<string, int> spawnedCounts = new Dictionary<string, int>();
    private Dictionary<string, int> killedCounts = new Dictionary<string, int>(); // เพิ่มการนับ enemy ที่ตาย
    private int totalEnemiesKilled = 0; // นับรวมทุกตัว

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
        InitializeBossConditions();

        ValidateSettings();
    }

    public override void FixedUpdateNetwork()
    {
        // Only Host/Server spawns enemies
        if (Runner == null || !Runner.IsServer) return;

        // Clean up dead enemies and bosses
        CleanupDeadEnemies();
        CleanupDeadBosses();

        // Check boss spawning conditions
        if (enableBossSpawning)
        {
            CheckBossSpawnConditions();
        }

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
                // ลด count ของ enemy type นั้น และเพิ่มการนับที่ตาย
                NetworkEnemy deadEnemy = activeEnemies[i];
                string enemyName = "";

                if (deadEnemy != null)
                {
                    // หา enemy data ที่ตรงกัน
                    foreach (EnemySpawnData enemyData in enemyPrefabs)
                    {
                        if (enemyData.enemyPrefab.name == deadEnemy.name.Replace("(Clone)", ""))
                        {
                            enemyData.currentCount = Mathf.Max(0, enemyData.currentCount - 1);
                            enemyName = enemyData.enemyName;
                            break;
                        }
                    }

                    // นับ enemy ที่ตาย
                    if (!string.IsNullOrEmpty(enemyName))
                    {
                        RecordEnemyKill(enemyName);
                    }
                }

                activeEnemies.RemoveAt(i);
            }
        }
    }

    private void CleanupDeadBosses()
    {
        for (int i = activeBosses.Count - 1; i >= 0; i--)
        {
            if (activeBosses[i] == null || activeBosses[i].IsDead)
            {
                NetworkEnemy deadBoss = activeBosses[i];

                if (deadBoss != null)
                {
                    // อัปเดต boss condition เมื่อบอสตาย
                    foreach (BossSpawnCondition condition in bossConditions)
                    {
                        if (condition.bossPrefab != null &&
                            condition.bossPrefab.name == deadBoss.name.Replace("(Clone)", ""))
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

    private void RecordEnemyKill(string enemyName)
    {
        // นับ enemy ที่ตายรวม
        totalEnemiesKilled++;

        // นับแยกตามประเภท
        if (killedCounts.ContainsKey(enemyName))
        {
            killedCounts[enemyName]++;
        }
        else
        {
            killedCounts[enemyName] = 1;
        }

        Debug.Log($"💀 {enemyName} killed! Total killed: {totalEnemiesKilled}");

        // อัปเดต kill count ใน boss conditions
        UpdateBossKillCounts(enemyName);
    }

    private void UpdateBossKillCounts(string killedEnemyName)
    {
        foreach (BossSpawnCondition condition in bossConditions)
        {
            if (condition.includeSpecificEnemies)
            {
                // เช็คเฉพาะ enemy ที่กำหนด
                if (condition.specificEnemyNames != null)
                {
                    foreach (string specificName in condition.specificEnemyNames)
                    {
                        if (specificName == killedEnemyName)
                        {
                            condition.currentKillCount++;
                            break;
                        }
                    }
                }
            }
            else
            {
                // นับทุก enemy
                condition.currentKillCount++;
            }

            // แสดง progress
            if (condition.currentKillCount % 10 == 0) // ทุก 10 kills
            {
                int remaining = condition.enemiesToKill - condition.currentKillCount;
                Debug.Log($"🎯 Boss {condition.bossName} progress: {condition.currentKillCount}/{condition.enemiesToKill} " +
                         $"(Remaining: {remaining})");
            }
        }
    }

    private void InitializeSpawnCounts()
    {
        spawnedCounts.Clear();
        killedCounts.Clear();
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
    }

    // ========== Boss Spawning System ==========

    private void CheckBossSpawnConditions()
    {
        if (bossConditions == null) return;

        foreach (BossSpawnCondition condition in bossConditions)
        {
            if (ShouldSpawnBoss(condition))
            {
                StartCoroutine(SpawnBossWithWarning(condition));
            }
        }
    }

    private bool ShouldSpawnBoss(BossSpawnCondition condition)
    {
        // เช็คเงื่อนไขต่างๆ
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

        // ประกาศเตือน
        if (condition.announceSpawn)
        {
            RPC_AnnounceBossSpawning(condition.bossName, condition.spawnWarningTime);
            yield return new WaitForSeconds(condition.spawnWarningTime);
        }

        // Spawn บอส
        SpawnBoss(condition);

        // รีเซ็ต kill count
        condition.currentKillCount = 0;
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

    // ========== RPC Methods for Boss Announcements ==========

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AnnounceBossSpawning(string bossName, float warningTime)
    {
        Debug.Log($"🚨 WARNING: {bossName} will spawn in {warningTime} seconds!");
        // สามารถเพิ่ม UI notification ที่นี่
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AnnounceBossSpawned(string bossName, Vector3 position)
    {
        Debug.Log($"🏆 {bossName} HAS SPAWNED at {position}!");
        // สามารถเพิ่ม UI notification หรือ sound effect ที่นี่
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AnnounceBossDefeated(string bossName)
    {
        Debug.Log($"⚔️ {bossName} HAS BEEN DEFEATED!");
        // สามารถเพิ่ม celebration effects ที่นี่
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

        // Validate boss conditions
        if (bossConditions != null)
        {
            foreach (BossSpawnCondition condition in bossConditions)
            {
                if (condition.bossPrefab == null)
                {
                    Debug.LogWarning($"[EnemySpawner] Boss prefab is null for '{condition.bossName}'");
                }

                if (string.IsNullOrEmpty(condition.bossName))
                {
                    condition.bossName = condition.bossPrefab != null ? condition.bossPrefab.name : "Unknown Boss";
                }

                if (condition.enemiesToKill <= 0)
                {
                    Debug.LogWarning($"[EnemySpawner] Boss '{condition.bossName}' has invalid enemiesToKill value: {condition.enemiesToKill}");
                    condition.enemiesToKill = 10; // Default value
                }

                if (condition.includeSpecificEnemies &&
                    (condition.specificEnemyNames == null || condition.specificEnemyNames.Length == 0))
                {
                    Debug.LogWarning($"[EnemySpawner] Boss '{condition.bossName}' requires specific enemies but none specified!");
                }
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
    /// Force spawn บอสทันที (ไม่เช็คเงื่อนไข)
    /// </summary>
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

    /// <summary>
    /// เพิ่ม kill count สำหรับทดสอบ
    /// </summary>
    public void AddKillCount(string enemyName, int count = 1)
    {
        if (!Runner.IsServer) return;

        for (int i = 0; i < count; i++)
        {
            RecordEnemyKill(enemyName);
        }
    }

    /// <summary>
    /// รีเซ็ต kill count สำหรับบอสเฉพาะ
    /// </summary>
    public void ResetBossKillCount(string bossName)
    {
        if (!Runner.IsServer) return;

        foreach (BossSpawnCondition condition in bossConditions)
        {
            if (condition.bossName == bossName)
            {
                condition.currentKillCount = 0;
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
    /// Clear enemies ทั้งหมด (ไม่รวมบอส)
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
    /// Clear บอสทั้งหมด
    /// </summary>
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
    }

    /// <summary>
    /// Clear ทุกอย่าง
    /// </summary>
    public void ClearAll()
    {
        ClearAllEnemies();
        ClearAllBosses();
    }

    /// <summary>
    /// ดูสถิติการ spawn
    /// </summary>
    public Dictionary<string, int> GetSpawnStatistics()
    {
        return new Dictionary<string, int>(spawnedCounts);
    }

    /// <summary>
    /// ดูสถิติการฆ่า enemy
    /// </summary>
    public Dictionary<string, int> GetKillStatistics()
    {
        return new Dictionary<string, int>(killedCounts);
    }

    /// <summary>
    /// ดู progress ของบอสทั้งหมด
    /// </summary>
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

    [ContextMenu("Log All Statistics")]
    public void LogAllStatistics()
    {
        Debug.Log("=== Enemy Spawn Statistics ===");
        Debug.Log($"Active Enemies: {activeEnemies.Count}/{maxTotalEnemies}");
        Debug.Log($"Active Bosses: {activeBosses.Count}");
        Debug.Log($"Total Enemies Killed: {totalEnemiesKilled}");

        Debug.Log("\n--- Spawn Counts ---");
        foreach (var kvp in spawnedCounts)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value} spawned");
        }

        Debug.Log("\n--- Kill Counts ---");
        foreach (var kvp in killedCounts)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value} killed");
        }

        Debug.Log("\n--- Enemy Stats ---");
        foreach (EnemySpawnData enemy in enemyPrefabs)
        {
            Debug.Log($"{enemy.enemyName}: Current={enemy.currentCount}, Max={enemy.maxCount}, Weight={enemy.spawnWeight}");
        }

        Debug.Log("\n--- Boss Progress ---");
        if (bossConditions != null)
        {
            foreach (BossSpawnCondition condition in bossConditions)
            {
                float progress = (float)condition.currentKillCount / condition.enemiesToKill * 100f;
                float cooldownRemaining = Mathf.Max(0, (condition.lastBossDeathTime + condition.bossRespawnCooldown) - Time.time);

                Debug.Log($"{condition.bossName}: " +
                         $"Progress={condition.currentKillCount}/{condition.enemiesToKill} ({progress:F1}%), " +
                         $"Active={condition.currentBossCount}/{condition.maxBossInstances}, " +
                         $"Cooldown={cooldownRemaining:F1}s");
            }
        }
    }

    [ContextMenu("Log Boss Progress Only")]
    public void LogBossProgress()
    {
        Debug.Log("=== Boss Spawn Progress ===");
        if (bossConditions != null)
        {
            foreach (BossSpawnCondition condition in bossConditions)
            {
                float progress = (float)condition.currentKillCount / condition.enemiesToKill * 100f;
                float cooldownRemaining = Mathf.Max(0, (condition.lastBossDeathTime + condition.bossRespawnCooldown) - Time.time);

                Debug.Log($"🏆 {condition.bossName}: " +
                         $"{condition.currentKillCount}/{condition.enemiesToKill} ({progress:F1}%) " +
                         $"| Active: {condition.currentBossCount}/{condition.maxBossInstances} " +
                         $"| Cooldown: {cooldownRemaining:F1}s");

                if (condition.includeSpecificEnemies && condition.specificEnemyNames != null)
                {
                    Debug.Log($"   Requires killing: {string.Join(", ", condition.specificEnemyNames)}");
                }
            }
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

    [ContextMenu("Test Add 10 Kills")]
    public void TestAdd10Kills()
    {
        if (Application.isPlaying && enemyPrefabs.Length > 0)
        {
            string enemyName = enemyPrefabs[0].enemyName;
            AddKillCount(enemyName, 10);
            Debug.Log($"Added 10 kills for {enemyName}");
        }
    }

    [ContextMenu("Force Spawn First Boss")]
    public void ForceSpawnFirstBoss()
    {
        if (Application.isPlaying && bossConditions != null && bossConditions.Length > 0)
        {
            ForceSpawnBoss(bossConditions[0].bossName);
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

    [ContextMenu("Clear All Bosses")]
    public void DebugClearAllBosses()
    {
        if (Application.isPlaying)
        {
            ClearAllBosses();
        }
    }

    [ContextMenu("Reset All Boss Progress")]
    public void ResetAllBossProgress()
    {
        if (Application.isPlaying && bossConditions != null)
        {
            foreach (BossSpawnCondition condition in bossConditions)
            {
                condition.currentKillCount = 0;
                condition.lastBossDeathTime = 0f;
            }
            Debug.Log("All boss progress reset!");
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw spawn radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

        // Draw normal spawn points
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

        // Draw boss spawn points
        if (bossSpawnPoints != null)
        {
            Gizmos.color = Color.red;
            foreach (Transform point in bossSpawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawCube(point.position, Vector3.one * 1f); // Use cube for boss spawns
                    Gizmos.DrawWireSphere(point.position, 2f); // Larger radius for bosses
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

        // Draw connections to boss spawn points
        if (useBossSpawnPoints && bossSpawnPoints != null)
        {
            Gizmos.color = Color.magenta;
            foreach (Transform point in bossSpawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawLine(transform.position, point.position);
                }
            }
        }
    }
}