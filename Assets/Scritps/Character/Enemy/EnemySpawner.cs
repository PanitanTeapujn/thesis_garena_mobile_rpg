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

// 🎯 ระบบ Multi-Point ที่เรียบง่าย
public enum MultiSpawnMode
{
    Off,              // ปิดใช้งาน - spawn ปกติ
    Balanced,         // spawn กระจายทุกจุดเท่าๆ กัน
    ClusterAttack,    // spawn รุมหลายจุดใกล้ๆ กัน
    SurroundPlayer,   // spawn ล้อมรอบผู้เล่น
    RandomBurst,      // spawn สุ่มหลายจุดพร้อมกัน
    EdgeSpawn         // spawn แค่จุดที่ไกลจากผู้เล่น
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

    [Header("🌊 Simple Multi-Point Spawning")]
    [Tooltip("เลือกรูปแบบการ spawn หลายจุด")]
    public MultiSpawnMode multiSpawnMode = MultiSpawnMode.Balanced;

    [Tooltip("จำนวนจุดที่จะ spawn พร้อมกัน (2-8)")]
    [Range(2, 8)]
    public int spawnPointCount = 3;

    [Tooltip("จำนวนศัตรูต่อจุด (1-4)")]
    [Range(1, 4)]
    public int enemiesPerPoint = 1;

    [Tooltip("ระยะห่างขั้นต่ำจากผู้เล่น (เมตร)")]
    [Range(5f, 20f)]
    public float minPlayerDistance = 8f;

    [Tooltip("เวลาคูลดาวน์หลัง multi-spawn (วินาที)")]
    [Range(3f, 15f)]
    public float multiSpawnCooldown = 5f;

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
    public bool verboseKillTracking = true;
    public bool showMultiSpawnInfo = true; // 🆕 แสดงข้อมูล multi-spawn

    [Header("Advanced Settings")]
    public bool balanceEnemyTypes = false;
    public bool spawnInWaves = false;
    public int enemiesPerWave = 3;
    public float waveCooldown = 10f;
    [Header("🏁 Stage Completion")]
    [Tooltip("ชื่อด่านปัจจุบัน (ถ้าไม่ใส่จะใช้ชื่อ Scene)")]
    public string currentStageName = "";

    [Tooltip("หยุด spawn เมื่อด่านเสร็จแล้ว")]
    public bool stopSpawningWhenStageCompleted = true;

    [Header("🔧 Stage Debug")]
    public bool showStageDebugInfo = true;
    [Header("🏆 Stage Complete UI")]
    public StageCompleteUI stageCompleteUI;
    // เพิ่มตัวแปรสำหรับเช็คสถานะด่าน
    private bool isStageCompleted = false;
    private float lastStageCheckTime = 0f;
    private const float STAGE_CHECK_INTERVAL = 2f; // เช็คทุก 2 วินาที
    [Header("🧹 Auto Cleanup")]
    [Tooltip("ทำลาย enemy ทั้งหมดเมื่อด่านเสร็จ")]
    public bool destroyRemainingEnemiesOnStageComplete = true;

    [Tooltip("หน่วงเวลาก่อนทำลาย enemy (วินาที)")]
    [Range(0f, 5f)]
    public float destroyDelay = 1f;

    public int currentSessionKills { get; private set; } = 0; // เปลี่ยนเป็น property
    private int requiredKillsForStage ;

    private float nextSpawnTime = 0f;
    private float nextWaveTime = 0f;
    private float nextMultiSpawnTime = 0f;
    private List<NetworkEnemy> activeEnemies = new List<NetworkEnemy>();
    private List<NetworkEnemy> activeBosses = new List<NetworkEnemy>();

    // ระบบ spawn points ที่เรียบง่าย
    private List<int> availableSpawnPoints = new List<int>();
    private List<int> recentlyUsedPoints = new List<int>();
    private int lastUsedSpawnPoint = -1;

    // สถิติ
    private Dictionary<string, int> spawnedCounts = new Dictionary<string, int>();
    private Dictionary<string, int> killedCounts = new Dictionary<string, int>();
    private int totalEnemiesKilled = 0;
    private Dictionary<NetworkObject, string> spawnedEnemyTypes = new Dictionary<NetworkObject, string>();

    // 🆕 Multi-spawn ที่เรียบง่าย
    private bool isMultiSpawning = false;
    private Queue<Vector3> pendingSpawnPositions = new Queue<Vector3>();
    private Queue<EnemySpawnData> pendingSpawnEnemies = new Queue<EnemySpawnData>();

    private void Start()
    {
        // โค้ดเดิม...
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
        if (StageRewardTracker.Instance != null)
        {
            StageRewardTracker.Instance.StartStageTracking(currentStageName);
            Debug.Log($"🎯 [EnemySpawner] Forced StageRewardTracker to start tracking: {currentStageName}");
        }
        if (string.IsNullOrEmpty(currentStageName))
        {
            // อ่านจาก PlayerPrefs ก่อน
            string selectedStage = PlayerPrefs.GetString("SelectedStage", "");
            if (!string.IsNullOrEmpty(selectedStage))
            {
                currentStageName = selectedStage;
                Debug.Log($"🎯 [EnemySpawner] Using stage from PlayerPrefs: '{currentStageName}'");
            }
            else
            {
                currentStageName = SceneManager.GetActiveScene().name;
                Debug.LogWarning($"🎯 [EnemySpawner] No PlayerPrefs, using scene name: '{currentStageName}'");
            }
        }

        // 🆕 ตรวจสอบว่า currentStageName ไม่ว่าง
        if (string.IsNullOrEmpty(currentStageName))
        {
            Debug.LogError("🚨 [EnemySpawner] currentStageName is still empty after setup!");
            currentStageName = "playroom1_1"; // fallback
        }

        // 🆕 Debug ก่อนเรียก GetRequiredKillsForStage
        Debug.Log($"🔍 [DEBUG] About to check required kills for stage: {currentStageName}");
        Debug.Log($"🔍 [DEBUG] PlayerPrefs key will be: RequiredKills_{currentStageName}");
        Debug.Log($"🔍 [DEBUG] PlayerPrefs value: {PlayerPrefs.GetInt($"RequiredKills_{currentStageName}", -999)}");

        currentSessionKills = 0;
        requiredKillsForStage = EnemyKillTracker.GetRequiredKillsForStage(currentStageName);
        nextSpawnTime = Time.time + 2f; // รอ 2 วินาทีก่อน spawn
        lastStageCheckTime = Time.time + 3f; // รอ 3 วินาทีก่อนเช็ค stage
        // 🆕 Debug หลังได้รับค่า
        Debug.Log($"🔍 [DEBUG] Got required kills: {requiredKillsForStage}");

        // 🆕 บังคับให้มีค่าขั้นต่ำ
        if (requiredKillsForStage <= 0)
        {
            Debug.LogError($"❌ [ERROR] Invalid requiredKillsForStage: {requiredKillsForStage}. Setting to 10.");
            requiredKillsForStage = 10;
        }

        isStageCompleted = false;

        Debug.Log($"🎯 [FINAL] Stage: {currentStageName}, Required: {requiredKillsForStage}, Session: {currentSessionKills}");

    }

    private void InitializeSpawnPoints()
    {
        availableSpawnPoints.Clear();
        recentlyUsedPoints.Clear();

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
                Debug.Log($"[EnemySpawner] Initialized {availableSpawnPoints.Count} valid spawn points");
            }
        }
        else
        {
            Debug.LogWarning("[EnemySpawner] No spawn points configured! Multi-spawn will be disabled.");
            multiSpawnMode = MultiSpawnMode.Off;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Runner == null || !Runner.IsServer) return;

        // เช็คสถานะด่านก่อน
        CheckStageCompletionStatus();

        // ถ้าด่านเสร็จแล้วและต้องหยุด spawn ให้หยุดเลย
        if (isStageCompleted && stopSpawningWhenStageCompleted)
        {
            CleanupDeadEnemies();
            CleanupDeadBosses();
            ProcessPendingMultiSpawns();

            if (showStageDebugInfo && Time.time % 5f < 0.1f)
            {
                Debug.Log($"🏁 Stage {currentStageName} completed - Spawning stopped");
            }
            return;
        }

        // 🔍 Debug ก่อนเรียก CleanupDeadEnemies
        if (showDebugInfo && Time.time % 2f < 0.1f && activeEnemies.Count > 0)
        {
            Debug.Log($"🔍 About to call CleanupDeadEnemies. Active enemies: {activeEnemies.Count}");

            // ตรวจสอบสถานะของแต่ละ enemy
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                var enemy = activeEnemies[i];
                if (enemy != null)
                {
                    Debug.Log($"🔍 Enemy {i}: {enemy.name}, IsDead: {enemy.IsDead}, HP: {enemy.CurrentHp}");
                }
                else
                {
                    Debug.Log($"🔍 Enemy {i}: NULL");
                }
            }
        }

        CleanupDeadEnemies();
        CleanupDeadBosses();
        ProcessPendingMultiSpawns();

        if (enableBossSpawning)
        {
            CheckBossSpawnConditions();
        }

        if (spawnInWaves)
        {
            HandleWaveSpawning();
        }
        else if (multiSpawnMode != MultiSpawnMode.Off)
        {
            HandleSimpleMultiSpawning();
        }
        else
        {
            HandleNormalSpawning();
        }
    }
    public void OnEnemyDeath(NetworkEnemy deadEnemy, string enemyTypeName)
    {
        if (!HasStateAuthority) return;

        Debug.Log($"🔥 EnemySpawner: Received death notification for {enemyTypeName}");
        Debug.Log($"🔥 currentSessionKills BEFORE: {currentSessionKills}");

        // อัพเดท session kills ทันที
        currentSessionKills++;
        totalEnemiesKilled++;

        Debug.Log($"🔥 currentSessionKills AFTER: {currentSessionKills}");
        Debug.Log($"🎯 Stage progress: {currentSessionKills}/{requiredKillsForStage} for {currentStageName}");

        // 🆕 ส่งข้อมูลไป StageRewardTracker (ใช้ RPC เพื่อให้ทุกคนได้ข้อมูลเดียวกัน)
        RPC_UpdateStageRewardTracker(enemyTypeName);

        // อัพเดท kill statistics
        if (killedCounts.ContainsKey(enemyTypeName))
        {
            killedCounts[enemyTypeName]++;
        }
        else
        {
            killedCounts[enemyTypeName] = 1;
        }

        // อัพเดท boss kill counts
        UpdateBossKillCounts(enemyTypeName);

        // เช็คว่าด่านเสร็จหรือยังทันที
        if (currentSessionKills >= requiredKillsForStage && !isStageCompleted)
        {
            Debug.Log($"🎉 Stage completion triggered by direct death notification! Kill #{currentSessionKills}");
            ForceCheckStageStatus();
        }

        if (verboseKillTracking)
        {
            Debug.Log($"📊 Direct Kill Update - {enemyTypeName}: {killedCounts[enemyTypeName]}, Total: {totalEnemiesKilled}");
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateStageRewardTracker(string enemyTypeName)
    {
        // อัพเดท StageRewardTracker ให้ทุกคน
        StageRewardTracker.AddEnemyKill();

        Debug.Log($"[RPC_UpdateStageRewardTracker] Added enemy kill for {enemyTypeName}");
    }
    private void CheckStageCompletionStatus()
    {
        // เช็คทุก STAGE_CHECK_INTERVAL วินาที เพื่อไม่ให้ส่งผลต่อ performance
        if (Time.time - lastStageCheckTime < STAGE_CHECK_INTERVAL)
            return;

        lastStageCheckTime = Time.time;

        // 🆕 ป้องกัน stage name ว่าง
        if (string.IsNullOrEmpty(currentStageName))
        {
            Debug.LogWarning("🚨 [CheckStageCompletionStatus] currentStageName is empty, skipping check");
            return;
        }

        bool wasCompleted = isStageCompleted;

        // ใช้ currentSessionKills แทนค่าที่เก็บถาวร
        isStageCompleted = currentSessionKills >= requiredKillsForStage;

        if (showStageDebugInfo && Time.time % 3f < 0.1f) // Debug ทุก 3 วินาที
        {
            Debug.Log($"🎯 Stage '{currentStageName}': {currentSessionKills}/{requiredKillsForStage} kills (Session) - Completed: {isStageCompleted}");
        }

        // ถ้าเพิ่งเสร็จใหม่
        if (!wasCompleted && isStageCompleted)
        {
            OnStageJustCompleted();
        }
    }

    // 🆕 เรียกเมื่อด่านเพิ่งเสร็จใหม่
    private void OnStageJustCompleted()
    {
        // ตรวจสอบว่า stageName ไม่ว่าง
        if (string.IsNullOrEmpty(currentStageName))
        {
            return;
        }

        // Mark ด่านเป็น completed ใน StageProgressManager (สำหรับ save ถาวร)
        StageProgressManager.CompleteStage(currentStageName);

        if (stopSpawningWhenStageCompleted)
        {
            // หยุด spawning ทันที
            nextSpawnTime = float.MaxValue;
            nextWaveTime = float.MaxValue;
            nextMultiSpawnTime = float.MaxValue;

            // หยุด multi-spawn ที่กำลังทำอยู่
            isMultiSpawning = false;
            pendingSpawnPositions.Clear();
            pendingSpawnEnemies.Clear();

            // 🆕 ทำลาย enemy ที่เหลือทั้งหมด
            if (destroyRemainingEnemiesOnStageComplete)
            {
                DestroyRemainingEnemies();
            }

            // แจ้งเตือนผู้เล่น (ผ่าน RPC)
            RPC_AnnounceStageCompleted(currentStageName);
        }
    }
    private void DestroyRemainingEnemies()
    {
        if (!HasStateAuthority) return;

        int destroyedCount = 0;
        int destroyedBossCount = 0;

        // ทำลาย enemy ปกติ
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            NetworkEnemy enemy = activeEnemies[i];
            if (enemy != null && !enemy.IsDead)
            {
                // ลบออกจาก tracking ก่อน
                if (enemy.Object != null && spawnedEnemyTypes.ContainsKey(enemy.Object))
                {
                    string enemyTypeName = spawnedEnemyTypes[enemy.Object];
                    spawnedEnemyTypes.Remove(enemy.Object);

                    // ลด current count
                    foreach (EnemySpawnData enemyData in enemyPrefabs)
                    {
                        if (enemyData.enemyName == enemyTypeName)
                        {
                            enemyData.currentCount = Mathf.Max(0, enemyData.currentCount - 1);
                            break;
                        }
                    }
                }

                // ทำลายทันที (ไม่ผ่าน death system)
                if (enemy.Object != null)
                {
                    Runner.Despawn(enemy.Object);
                    destroyedCount++;
                }
            }
            activeEnemies.RemoveAt(i);
        }

        // ทำลาย boss ที่เหลือ
        for (int i = activeBosses.Count - 1; i >= 0; i--)
        {
            NetworkEnemy boss = activeBosses[i];
            if (boss != null && !boss.IsDead)
            {
                // อัพเดท boss condition
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
                        break;
                    }
                }

                // ทำลายทันที
                if (boss.Object != null)
                {
                    Runner.Despawn(boss.Object);
                    destroyedBossCount++;
                }
            }
            activeBosses.RemoveAt(i);
        }

        // ลบ pending spawns ทั้งหมด
        pendingSpawnPositions.Clear();
        pendingSpawnEnemies.Clear();
        isMultiSpawning = false;

        // ส่ง RPC แจ้งการทำลาย
        if (destroyedCount > 0 || destroyedBossCount > 0)
        {
            RPC_AnnounceEnemiesDestroyed(destroyedCount, destroyedBossCount);
        }

        if (showStageDebugInfo)
        {
            Debug.Log($"🧹 Stage cleanup: Destroyed {destroyedCount} enemies and {destroyedBossCount} bosses");
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AnnounceEnemiesDestroyed(int enemyCount, int bossCount)
    {
        if (showStageDebugInfo)
        {
            string message = "🧹 Stage cleared! ";
            if (enemyCount > 0)
            {
                message += $"Removed {enemyCount} enemies";
            }
            if (bossCount > 0)
            {
                message += $"{(enemyCount > 0 ? " and " : "")}Removed {bossCount} bosses";
            }
            Debug.Log(message);
        }
    }

    // 🆕 Method สำหรับทำลาย enemy ที่เหลือแบบ manual (สำหรับเรียกจากภายนอก)
    public void ForceDestroyAllRemainingEnemies()
    {
        if (!HasStateAuthority)
        {
            Debug.LogWarning("🚨 Cannot destroy enemies - not server authority");
            return;
        }

        DestroyRemainingEnemies();
        Debug.Log("🧹 Manually destroyed all remaining enemies");
    }

    // 🆕 เพิ่ม method สำหรับ toggle การทำลาย enemy อัตโนมัติ
    public void SetAutoDestroyEnemies(bool enable)
    {
        destroyRemainingEnemiesOnStageComplete = enable;

        if (showStageDebugInfo)
        {
            Debug.Log($"🧹 Auto destroy enemies on stage complete: {enable}");
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AnnounceStageCompleted(string stageName)
    {
        // 🆕 เพิ่ม debug
       

        // อัพเดทจำนวน enemy สุดท้ายให้ StageRewardTracker
        StageRewardTracker.Instance.SetCorrectEnemyCount(currentSessionKills);
        StageCompleteUI stageUI = FindObjectOfType<StageCompleteUI>();

        StageCompleteUI stageCompleteUI = FindObjectOfType<StageCompleteUI>();
        if (stageUI != null)
        {
            bool isPanelActive = stageUI.stageCompletePanel != null && stageUI.stageCompletePanel.activeSelf;

            // ถ้า panel เปิดอยู่แล้ว ให้ปิด
            if (isPanelActive)
            {
                stageUI.HideStageComplete();
            }
        }
        if (stageCompleteUI != null)
        {
            stageCompleteUI.ShowStageComplete(stageName);
        }
        else
        {
        }
    }
    // เพิ่ม method สำหรับ manual testing
 

    public void SetCurrentStage(string stageName)
    {
        currentStageName = stageName;
        isStageCompleted = false; // รีเซ็ตสถานะ

        if (showStageDebugInfo)
        {
            Debug.Log($"🏁 Stage set to: {stageName}");
        }
    }
    public int GetRequiredKills()
    {
        return requiredKillsForStage;
    }
    public bool IsCurrentStageCompleted()
    {
        return isStageCompleted;
    }

    /// <summary>
    /// บังคับให้เช็คสถานะด่านทันที
    /// </summary>
    public void ForceCheckStageStatus()
    {
        lastStageCheckTime = 0f; // รีเซ็ตเวลาเพื่อให้เช็คใหม่ทันที
        CheckStageCompletionStatus();
    }

    /// <summary>
    /// เปิด/ปิดการหยุด spawn เมื่อด่านเสร็จ
    /// </summary>
    public void SetStopSpawningWhenCompleted(bool shouldStop)
    {
        stopSpawningWhenStageCompleted = shouldStop;

        if (showStageDebugInfo)
        {
            Debug.Log($"🏁 Stop spawning when completed: {shouldStop}");
        }
    }

    /// <summary>
    /// รีสตาร์ทการ spawn หลังจากที่หยุดไปแล้ว (สำหรับ restart ด่าน)
    /// </summary>
    public void RestartSpawning()
    {
        if (!HasStateAuthority) return;

        isStageCompleted = false;
        currentSessionKills = 0;
        totalEnemiesKilled = 0; // รีเซ็ต total kills ด้วย

        // รีเซ็ต timers
        nextSpawnTime = Time.time + 1f;
        nextWaveTime = Time.time + waveCooldown;
        nextMultiSpawnTime = Time.time + multiSpawnCooldown;
        lastStageCheckTime = 0f;

        // รีเซ็ต kill counts และ boss conditions
        InitializeSpawnCounts();
        InitializeBossConditions();

        Debug.Log($"🔄 Complete restart for stage: {currentStageName}");
        Debug.Log($"🎯 Required kills: {requiredKillsForStage}");
        Debug.Log($"🔥 Session kills reset to: {currentSessionKills}");
    }

    /// <summary>
    /// เปิด/ปิดการหยุด spawn เมื่อด่านเสร็จ
    /// </summary>

  

    // 🆕 ระบบ Multi-Spawn ที่เรียบง่าย
    private void HandleSimpleMultiSpawning()
    {
        if (Time.time >= nextMultiSpawnTime && CanSpawnMore() && !isMultiSpawning)
        {
            StartSimpleMultiSpawn();
        }
    }

    private void StartSimpleMultiSpawn()
    {
        if (isMultiSpawning || availableSpawnPoints.Count == 0) return;

        List<Vector3> spawnPositions = GetMultiSpawnPositions();
        if (spawnPositions.Count == 0) return;

        isMultiSpawning = true;
        pendingSpawnPositions.Clear();
        pendingSpawnEnemies.Clear();

        // เตรียม spawn requests
        int totalPlanned = 0;
        foreach (Vector3 position in spawnPositions)
        {
            for (int i = 0; i < enemiesPerPoint && totalPlanned < (maxTotalEnemies - activeEnemies.Count); i++)
            {
                EnemySpawnData selectedEnemy = SelectEnemyToSpawn();
                if (selectedEnemy != null)
                {
                    // เพิ่ม offset เล็กน้อยสำหรับตัวที่ 2, 3, 4...
                    Vector3 adjustedPosition = position;
                    if (i > 0)
                    {
                        Vector2 randomOffset = Random.insideUnitCircle * 2f;
                        adjustedPosition += new Vector3(randomOffset.x, 0, randomOffset.y);
                    }

                    pendingSpawnPositions.Enqueue(adjustedPosition);
                    pendingSpawnEnemies.Enqueue(selectedEnemy);
                    totalPlanned++;
                }
            }
        }

        nextMultiSpawnTime = Time.time + multiSpawnCooldown;

        if (showMultiSpawnInfo && totalPlanned > 0)
        {
        }
    }

    // 🎯 หาตำแหน่ง spawn ตามโหมดที่เลือก
    private List<Vector3> GetMultiSpawnPositions()
    {
        if (!useSpawnPoints || availableSpawnPoints.Count == 0)
            return new List<Vector3>();

        List<Vector3> positions = new List<Vector3>();
        List<Transform> playerTransforms = GetAllPlayerTransforms();

        // จำกัดจำนวนจุดไม่ให้เกินที่มี
        int actualPointCount = Mathf.Min(spawnPointCount, availableSpawnPoints.Count);

        switch (multiSpawnMode)
        {
            case MultiSpawnMode.Balanced:
                positions = GetBalancedSpawnPositions(actualPointCount);
                break;

            case MultiSpawnMode.ClusterAttack:
                positions = GetClusterSpawnPositions(actualPointCount);
                break;

            case MultiSpawnMode.SurroundPlayer:
                positions = GetSurroundPlayerPositions(actualPointCount, playerTransforms);
                break;

            case MultiSpawnMode.RandomBurst:
                positions = GetRandomSpawnPositions(actualPointCount);
                break;

            case MultiSpawnMode.EdgeSpawn:
                positions = GetEdgeSpawnPositions(actualPointCount, playerTransforms);
                break;
        }

        return FilterPositionsByPlayerDistance(positions, playerTransforms);
    }

    // 🎯 Balanced: กระจายทุกจุดเท่าๆ กัน
    private List<Vector3> GetBalancedSpawnPositions(int count)
    {
        List<Vector3> positions = new List<Vector3>();

        if (count >= availableSpawnPoints.Count)
        {
            // ใช้ทุกจุด
            foreach (int pointIndex in availableSpawnPoints)
            {
                positions.Add(spawnPoints[pointIndex].position);
            }
        }
        else
        {
            // เลือกจุดที่กระจายเท่าๆ กัน
            float step = (float)availableSpawnPoints.Count / count;
            for (int i = 0; i < count; i++)
            {
                int index = Mathf.RoundToInt(i * step) % availableSpawnPoints.Count;
                int pointIndex = availableSpawnPoints[index];
                positions.Add(spawnPoints[pointIndex].position);
            }
        }

        return positions;
    }

    // 🎯 Cluster: spawn รุมหลายจุดใกล้ๆ กัน
    private List<Vector3> GetClusterSpawnPositions(int count)
    {
        List<Vector3> positions = new List<Vector3>();

        // หาจุดกลางที่สุ่ม
        int centerIndex = availableSpawnPoints[Random.Range(0, availableSpawnPoints.Count)];
        Vector3 centerPos = spawnPoints[centerIndex].position;
        positions.Add(centerPos);

        // หาจุดใกล้เคียงกับจุดกลาง
        List<(int index, float distance)> nearbyPoints = new List<(int, float)>();
        foreach (int pointIndex in availableSpawnPoints)
        {
            if (pointIndex != centerIndex)
            {
                float distance = Vector3.Distance(centerPos, spawnPoints[pointIndex].position);
                nearbyPoints.Add((pointIndex, distance));
            }
        }

        // เรียงตามระยะทางจากใกล้ไปไกล
        nearbyPoints.Sort((a, b) => a.distance.CompareTo(b.distance));

        // เพิ่มจุดใกล้เคียง
        for (int i = 0; i < count - 1 && i < nearbyPoints.Count; i++)
        {
            positions.Add(spawnPoints[nearbyPoints[i].index].position);
        }

        return positions;
    }

    // 🎯 Surround: ล้อมรอบผู้เล่น
    private List<Vector3> GetSurroundPlayerPositions(int count, List<Transform> players)
    {
        List<Vector3> positions = new List<Vector3>();

        if (players.Count == 0)
            return GetRandomSpawnPositions(count);

        // หาตำแหน่งกลางของผู้เล่น
        Vector3 playerCenter = Vector3.zero;
        foreach (Transform player in players)
        {
            playerCenter += player.position;
        }
        playerCenter /= players.Count;

        // หาจุดที่ล้อมรอบผู้เล่น (วางเป็นวงกลม)
        List<(int index, float angle)> surroundPoints = new List<(int, float)>();

        foreach (int pointIndex in availableSpawnPoints)
        {
            Vector3 direction = spawnPoints[pointIndex].position - playerCenter;
            direction.y = 0;
            float angle = Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360;
            surroundPoints.Add((pointIndex, angle));
        }

        // เรียงตามมุม
        surroundPoints.Sort((a, b) => a.angle.CompareTo(b.angle));

        // เลือกจุดที่กระจายเป็นวงกลม
        float angleStep = 360f / count;
        for (int i = 0; i < count && i < surroundPoints.Count; i++)
        {
            int selectedIndex = Mathf.RoundToInt((float)i / count * surroundPoints.Count) % surroundPoints.Count;
            positions.Add(spawnPoints[surroundPoints[selectedIndex].index].position);
        }

        return positions;
    }

    // 🎯 Random: สุ่มจุด
    private List<Vector3> GetRandomSpawnPositions(int count)
    {
        List<Vector3> positions = new List<Vector3>();
        List<int> availableCopy = new List<int>(availableSpawnPoints);

        // ลบจุดที่ใช้ไปแล้วเมื่อเร็วๆ นี้
        foreach (int usedPoint in recentlyUsedPoints)
        {
            availableCopy.Remove(usedPoint);
        }

        if (availableCopy.Count == 0)
            availableCopy = new List<int>(availableSpawnPoints);

        for (int i = 0; i < count && availableCopy.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, availableCopy.Count);
            int pointIndex = availableCopy[randomIndex];
            positions.Add(spawnPoints[pointIndex].position);
            availableCopy.RemoveAt(randomIndex);
        }

        return positions;
    }

    // 🎯 Edge: จุดที่ไกลจากผู้เล่นที่สุด
    private List<Vector3> GetEdgeSpawnPositions(int count, List<Transform> players)
    {
        List<Vector3> positions = new List<Vector3>();

        if (players.Count == 0)
            return GetRandomSpawnPositions(count);

        // คำนวณระยะห่างขั้นต่ำของแต่ละจุดจากผู้เล่น
        List<(int index, float minDistance)> pointDistances = new List<(int, float)>();

        foreach (int pointIndex in availableSpawnPoints)
        {
            float minDistance = float.MaxValue;
            Vector3 pointPos = spawnPoints[pointIndex].position;

            foreach (Transform player in players)
            {
                float distance = Vector3.Distance(pointPos, player.position);
                if (distance < minDistance)
                    minDistance = distance;
            }

            pointDistances.Add((pointIndex, minDistance));
        }

        // เรียงจากไกลที่สุด
        pointDistances.Sort((a, b) => b.minDistance.CompareTo(a.minDistance));

        for (int i = 0; i < count && i < pointDistances.Count; i++)
        {
            positions.Add(spawnPoints[pointDistances[i].index].position);
        }

        return positions;
    }

    // 🔍 กรองตำแหน่งตามระยะห่างจากผู้เล่น
    private List<Vector3> FilterPositionsByPlayerDistance(List<Vector3> positions, List<Transform> players)
    {
        if (players.Count == 0) return positions;

        List<Vector3> validPositions = new List<Vector3>();

        foreach (Vector3 position in positions)
        {
            bool isSafe = true;
            foreach (Transform player in players)
            {
                if (Vector3.Distance(position, player.position) < minPlayerDistance)
                {
                    isSafe = false;
                    break;
                }
            }

            if (isSafe)
                validPositions.Add(position);
        }

        // ถ้าไม่มีจุดที่ปลอดภัย ให้ใช้จุดเดิม (อาจจะ spawn ใกล้ผู้เล่น)
        if (validPositions.Count == 0)
        {
            if (showMultiSpawnInfo)
            return positions;
        }

        return validPositions;
    }

    // 🆕 ประมวลผล queue ที่เรียบง่าย
    private void ProcessPendingMultiSpawns()
    {
        if (!isMultiSpawning) return;

        if (pendingSpawnPositions.Count == 0)
        {
            isMultiSpawning = false;
            return;
        }

        // Spawn ทีละตัวในแต่ละ frame เพื่อลด lag
        if (pendingSpawnPositions.Count > 0 && pendingSpawnEnemies.Count > 0 && CanSpawnMore())
        {
            Vector3 position = pendingSpawnPositions.Dequeue();
            EnemySpawnData enemyData = pendingSpawnEnemies.Dequeue();

            ExecuteSpawn(enemyData, position);
        }
    }

    private void ExecuteSpawn(EnemySpawnData enemyData, Vector3 position)
    {
        NetworkEnemy enemy = Runner.Spawn(enemyData.enemyPrefab, position, Quaternion.identity, PlayerRef.None);

        if (enemy != null)
        {
            activeEnemies.Add(enemy);
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

            if (showMultiSpawnInfo)
            {
                Debug.Log($"🌊 Spawned {enemyData.enemyName} at {position} | Active: {activeEnemies.Count}/{maxTotalEnemies}");
            }
        }
    }

    private List<Transform> GetAllPlayerTransforms()
    {
        List<Transform> playerTransforms = new List<Transform>();
        Hero[] heroes = FindObjectsOfType<Hero>();

        foreach (Hero hero in heroes)
        {
            if (hero != null && hero.IsSpawned)
            {
                playerTransforms.Add(hero.transform);
            }
        }

        return playerTransforms;
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
            if (multiSpawnMode != MultiSpawnMode.Off)
            {
                // แปลง wave เป็น multi-spawn แบบง่ายๆ
                int originalEnemiesPerPoint = enemiesPerPoint;
                int originalPointCount = spawnPointCount;

                enemiesPerPoint = 1; // 1 ตัวต่อจุดสำหรับ wave
                spawnPointCount = Mathf.Min(enemiesPerWave, availableSpawnPoints.Count);

                StartSimpleMultiSpawn();

                // คืนค่าเดิม
                enemiesPerPoint = originalEnemiesPerPoint;
                spawnPointCount = originalPointCount;
            }
            else
            {
                SpawnWave();
            }
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

    private Vector3 GetRandomSpawnPosition()
    {
        if (useSpawnPoints && spawnPoints != null && availableSpawnPoints.Count > 0)
        {
            int selectedIndex;

            if (availableSpawnPoints.Count == 1)
            {
                selectedIndex = 0;
            }
            else if (balanceEnemyTypes)
            {
                int currentIndex = availableSpawnPoints.IndexOf(lastUsedSpawnPoint);
                int nextIndex = (currentIndex + 1) % availableSpawnPoints.Count;
                selectedIndex = nextIndex;
            }
            else
            {
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

    // ระบบการจัดการ dead enemies (เหมือนเดิม)
    private void CleanupDeadEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            NetworkEnemy enemy = activeEnemies[i];

            // ถ้า enemy ตายหรือ null ให้ลบออกจาก active list
            if (enemy == null || enemy.IsDead)
            {
                // ลดจำนวน current count ของ enemy type นั้นๆ
                if (enemy != null && enemy.Object != null && spawnedEnemyTypes.ContainsKey(enemy.Object))
                {
                    string enemyTypeName = spawnedEnemyTypes[enemy.Object];
                    spawnedEnemyTypes.Remove(enemy.Object);

                    // หา enemy data แล้วลด count
                    foreach (EnemySpawnData enemyData in enemyPrefabs)
                    {
                        if (enemyData.enemyName == enemyTypeName)
                        {
                            enemyData.currentCount = Mathf.Max(0, enemyData.currentCount - 1);
                            break;
                        }
                    }

                    if (showDebugInfo)
                    {
                    }
                }

                activeEnemies.RemoveAt(i);
            }
        }
    }
 

    private System.Collections.IEnumerator KillEnemyAfterDelay(NetworkEnemy enemy, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (enemy != null && !enemy.IsDead)
        {
            Debug.Log($"🧪 Killing test enemy...");

            // ตั้งค่า HP เป็น 0 และ IsDead เป็น true
            enemy.CurrentHp = 0;
            enemy.IsDead = true;

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

    

    // 🧪 3. เพิ่ม method สำหรับทดสอบการฆ่า enemy ตรงๆ
   

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
                shouldCount = true;
            }

            if (shouldCount)
            {
                condition.currentKillCount++;

                if (verboseKillTracking && condition.currentKillCount % 5 == 0)
                {
                    int remaining = condition.enemiesToKill - condition.currentKillCount;
                    float progress = (float)condition.currentKillCount / condition.enemiesToKill * 100f;

                    Debug.Log($"🎯 Boss {condition.bossName} progress: {condition.currentKillCount}/{condition.enemiesToKill} " +
                             $"({progress:F1}%) - Remaining: {remaining}");

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

    // ========== Boss Spawning System (เหมือนเดิม) ==========

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
            Transform bossSpawnPoint = bossSpawnPoints[Random.Range(0, bossSpawnPoints.Length)];
            return bossSpawnPoint.position;
        }
        else
        {
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

        int validEnemies = 0;
        foreach (EnemySpawnData enemy in enemyPrefabs)
        {
            if (enemy.enemyPrefab == null)
            {
                Debug.LogWarning($"[EnemySpawner] Enemy prefab is null for '{enemy.enemyName}'");
                continue;
            }

            if (string.IsNullOrEmpty(currentStageName))
            {
                currentStageName = SceneManager.GetActiveScene().name;
                Debug.LogWarning($"[EnemySpawner] No stage name set, using scene name: {currentStageName}");
            }

            validEnemies++;
        }

        Debug.Log($"[EnemySpawner] Validated {validEnemies} valid enemy types");

        if (useSpawnPoints)
        {
            if (spawnPoints == null || availableSpawnPoints.Count == 0)
            {
                Debug.LogWarning("[EnemySpawner] useSpawnPoints is enabled but no valid spawn points found! Multi-spawn disabled.");
                useSpawnPoints = false;
                multiSpawnMode = MultiSpawnMode.Off;
            }
            else
            {
                Debug.Log($"[EnemySpawner] Using {availableSpawnPoints.Count} spawn points");

                if (multiSpawnMode != MultiSpawnMode.Off)
                {
                    Debug.Log($"🌊 Multi-Spawn enabled: {multiSpawnMode} mode");
                }
            }
        }
        if (string.IsNullOrEmpty(currentStageName))
        {
            currentStageName = SceneManager.GetActiveScene().name;
            Debug.LogWarning($"[EnemySpawner] No stage name set, using scene name: {currentStageName}");
        }

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

    // ========== Public Methods สำหรับการควบคุม ==========

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

    // 🆕 เปลี่ยนโหมด Multi-Spawn แบบง่ายๆ
    public void SetMultiSpawnMode(MultiSpawnMode newMode)
    {
        multiSpawnMode = newMode;

        if (showMultiSpawnInfo)
        {
            Debug.Log($"🌊 Multi-Spawn mode changed to: {newMode}");
        }
    }

    // 🆕 บังคับให้เกิด Multi-Spawn ทันที
    public void ForceMultiSpawn()
    {
        if (!Runner.IsServer || multiSpawnMode == MultiSpawnMode.Off) return;

        nextMultiSpawnTime = 0f; // Reset cooldown
        StartSimpleMultiSpawn();

        Debug.Log("🌊 Force triggered multi-spawn!");
    }

    // 🆕 ตั้งค่าง่ายๆ สำหรับ intensity
    public void SetSpawnIntensity(int intensity)
    {
        // intensity 1-5
        intensity = Mathf.Clamp(intensity, 1, 5);

        switch (intensity)
        {
            case 1: // Easy
                spawnPointCount = 2;
                enemiesPerPoint = 1;
                multiSpawnCooldown = 8f;
                break;
            case 2: // Normal
                spawnPointCount = 3;
                enemiesPerPoint = 1;
                multiSpawnCooldown = 6f;
                break;
            case 3: // Hard
                spawnPointCount = 4;
                enemiesPerPoint = 2;
                multiSpawnCooldown = 5f;
                break;
            case 4: // Very Hard
                spawnPointCount = 5;
                enemiesPerPoint = 2;
                multiSpawnCooldown = 4f;
                break;
            case 5: // Insane
                spawnPointCount = 6;
                enemiesPerPoint = 3;
                multiSpawnCooldown = 3f;
                break;
        }

        if (showMultiSpawnInfo)
        {
            Debug.Log($"🌊 Spawn intensity set to {intensity}: {spawnPointCount} points, {enemiesPerPoint} per point, {multiSpawnCooldown}s cooldown");
        }
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
        nextMultiSpawnTime = Time.time + duration;
        Debug.Log($"[EnemySpawner] All spawning paused for {duration} seconds");
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

        // ล้าง multi-spawn queue
        pendingSpawnPositions.Clear();
        pendingSpawnEnemies.Clear();
        isMultiSpawning = false;

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

    // 🆕 Get simple status
    public bool IsMultiSpawning()
    {
        return isMultiSpawning;
    }

    public int GetPendingSpawnCount()
    {
        return pendingSpawnPositions.Count;
    }

    public MultiSpawnMode GetCurrentMode()
    {
        return multiSpawnMode;
    }

    // ========== Debug Visualization ==========
    private void OnDrawGizmosSelected()
    {
        if (spawnPoints == null) return;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                // เปลี่ยนสีตามโหมด Multi-Spawn
                switch (multiSpawnMode)
                {
                    case MultiSpawnMode.Off:
                        Gizmos.color = Color.gray;
                        break;
                    case MultiSpawnMode.Balanced:
                        Gizmos.color = Color.green;
                        break;
                    case MultiSpawnMode.ClusterAttack:
                        Gizmos.color = Color.red;
                        break;
                    case MultiSpawnMode.SurroundPlayer:
                        Gizmos.color = Color.blue;
                        break;
                    case MultiSpawnMode.RandomBurst:
                        Gizmos.color = Color.yellow;
                        break;
                    case MultiSpawnMode.EdgeSpawn:
                        Gizmos.color = Color.magenta;
                        break;
                }

                Gizmos.DrawWireSphere(spawnPoints[i].position, 1f);

#if UNITY_EDITOR
                UnityEditor.Handles.Label(spawnPoints[i].position + Vector3.up * 2f, $"SP {i}");
#endif
            }
        }

        // วาดรัศมีป้องกันผู้เล่น
        if (multiSpawnMode != MultiSpawnMode.Off)
        {
            Gizmos.color = Color.red;
            List<Transform> players = GetAllPlayerTransforms();
            foreach (Transform player in players)
            {
                Gizmos.DrawWireSphere(player.position, minPlayerDistance);
            }
        }

        // วาดรัศมี spawn ปกติ
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}