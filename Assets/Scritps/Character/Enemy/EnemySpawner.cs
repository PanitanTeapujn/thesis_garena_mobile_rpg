using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;

[System.Serializable]
public class EnemySpawnData
{
    public NetworkEnemy enemyPrefab;
    public string enemyName;
    [Range(1, 10)]
    public int spawnWeight = 1;        // น้ำหนักการสุ่ม (ยิ่งสูงยิ่งมีโอกาสมาก)
    public int maxCount = -1;          // จำนวนสูงสุดที่สามารถมีพร้อมกันได้ (-1 = ไม่จำกัด)
    public float spawnCooldown = 0f;   // Cooldown หลัง spawn (วินาที)

    [HideInInspector] public int currentCount = 0;
    [HideInInspector] public float lastSpawnTime = 0f;
}

public class EnemySpawner : NetworkBehaviour
{
    [Header("📦 Enemy Prefabs")]
    public EnemySpawnData[] enemyPrefabs;

    [Header("⚙️ Spawn Settings")]
    [Tooltip("จำนวนศัตรูสูงสุดที่อยู่ในแมพพร้อมกัน")]
    public int maxTotalEnemies = 10;

    [Tooltip("ระยะเวลาระหว่างการ spawn (วินาที)")]
    public float spawnInterval = 2f;

    [Tooltip("สุ่มเวลา spawn ±")]
    public bool randomizeSpawnInterval = true;
    [Range(0f, 2f)]
    public float spawnIntervalVariation = 0.5f;

    // 🆕 Spawn หลายตัวพร้อมกัน
    [Header("🌊 Batch Spawning")]
    [Tooltip("Spawn หลายตัวพร้อมกัน")]
    public bool enableBatchSpawning = true;

    [Tooltip("จำนวนขั้นต่ำที่จะ spawn ต่อครั้ง")]
    [Range(1, 5)]
    public int minEnemiesPerSpawn = 2;

    [Tooltip("จำนวนสูงสุดที่จะ spawn ต่อครั้ง")]
    [Range(1, 10)]
    public int maxEnemiesPerSpawn = 7;

    [Header("🎯 Player-Based Spawning")]
    [Tooltip("Spawn รอบๆ ผู้เล่นแทนที่จะใช้ spawn points")]
    public bool spawnAroundPlayers = true;

    [Tooltip("ระยะห่างขั้นต่ำจากผู้เล่น (เมตร)")]
    [Range(5f, 20f)]
    public float minSpawnDistanceFromPlayer = 8f;

    [Tooltip("ระยะห่างสูงสุดจากผู้เล่น (เมตร)")]
    [Range(10f, 30f)]
    public float maxSpawnDistanceFromPlayer = 15f;

    [Tooltip("ตรวจสอบกำแพงก่อน spawn")]
    public bool checkWallsBeforeSpawn = true;

    [Tooltip("ตรวจสอบพื้นก่อน spawn")]
    public bool checkGroundBeforeSpawn = true;

    [Tooltip("ความสูงสูงสุดจากพื้น (ถ้าเกินจะปรับลงมา)")]
    [Range(0.5f, 3f)]
    public float maxGroundHeight = 1f;

    [Tooltip("จำนวนครั้งที่พยายามหาตำแหน่งใหม่")]
    [Range(3, 10)]
    public int maxSpawnAttempts = 5;
    private float firstSpawnTime = 0f;
    private bool isFirstSpawnReady = false;
    [Header("🏁 Stage System")]
    [Tooltip("ชื่อด่านปัจจุบัน (ถ้าไม่ใส่จะใช้ชื่อ Scene)")]
    public string currentStageName = "";

    [Tooltip("หยุด spawn เมื่อด่านเสร็จแล้ว")]
    public bool stopSpawningWhenStageCompleted = true;

    [Tooltip("ทำลาย enemy ที่เหลือเมื่อด่านเสร็จ")]
    public bool destroyRemainingEnemiesOnStageComplete = true;

    [Header("🎯 Lobby Dummy System")]
    public NetworkEnemy dummyPrefab;
    public Transform dummySpawnPoint;
    public bool isLobbyScene = false;

    [Header("🔧 Debug Settings")]
    public bool showDebugInfo = false;
    public bool showSpawnInfo = true;

    // ========== Private Variables ==========
    private float nextSpawnTime = 0f;
    private List<NetworkEnemy> activeEnemies = new List<NetworkEnemy>();
    private Dictionary<NetworkObject, string> spawnedEnemyTypes = new Dictionary<NetworkObject, string>();

    // Stage tracking
    private bool isStageCompleted = false;
    private float lastStageCheckTime = 0f;
    private const float STAGE_CHECK_INTERVAL = 2f;
    public int currentSessionKills { get; private set; } = 0;
    private int requiredKillsForStage = 0;

    // Lobby dummy
    private NetworkEnemy currentDummyInstance;
    private bool hasDummySpawned = false;
    private float lastDummyCheckTime = 0f;
    private const float DUMMY_CHECK_INTERVAL = 10f;
    [Header("🏷️ Enemy Tag Filter")]
    private EnemyTag[] requiredEnemyTags = new EnemyTag[] { };
    // ========== Unity Lifecycle ==========
    private void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        isLobbyScene = sceneName.ToLower().Contains("lobby");

        if (isLobbyScene)
        {
            Debug.Log("🏠 [EnemySpawner] Lobby scene detected");
            if (HasStateAuthority)
            {
                StartCoroutine(SpawnLobbyDummy());
            }
            return;
        }

        // === Stage Scene Setup ===
        if (string.IsNullOrEmpty(currentStageName))
        {
            currentStageName = PlayerPrefs.GetString("SelectedStage", SceneManager.GetActiveScene().name);
        }

        currentSessionKills = 0;
        requiredKillsForStage = EnemyKillTracker.GetRequiredKillsForStage(currentStageName);

        if (requiredKillsForStage <= 0)
        {
            requiredKillsForStage = 10;
        }
        LoadRequiredEnemyTags();

        // ✅ รอ 6 วินาทีก่อน spawn ครั้งแรก
        firstSpawnTime = Time.time + 6f;
        nextSpawnTime = firstSpawnTime;
        isFirstSpawnReady = false;

        isStageCompleted = false;

        Debug.Log($"🎯 [EnemySpawner] Stage: {currentStageName}, Required Kills: {requiredKillsForStage}");
        Debug.Log($"⏰ First spawn will occur at {firstSpawnTime:F1}s (in 6 seconds)");

        foreach (EnemySpawnData enemy in enemyPrefabs)
        {
            enemy.currentCount = 0;
            enemy.lastSpawnTime = 0f;
        }
    }

    // ========== Network Update ==========
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (isLobbyScene)
        {
            if (Time.time - lastDummyCheckTime >= DUMMY_CHECK_INTERVAL)
            {
                lastDummyCheckTime = Time.time;
                CheckAndSpawnDummyIfNeeded();
            }
            return;
        }

        // ✅ เช็คว่าถึงเวลา spawn ครั้งแรกหรือยัง
        if (!isFirstSpawnReady)
        {
            if (Time.time >= firstSpawnTime)
            {
                isFirstSpawnReady = true;
                Debug.Log($"✅ First spawn time reached! Starting enemy spawning...");
            }
            else
            {
                return; // ยังไม่ถึง 6 วินาที
            }
        }

        // Stage completion check
        if (Time.time - lastStageCheckTime >= STAGE_CHECK_INTERVAL)
        {
            lastStageCheckTime = Time.time;
            CheckStageCompletion();
        }

        if (isStageCompleted && stopSpawningWhenStageCompleted)
        {
            CleanupDeadEnemies();
            return;
        }

        CleanupDeadEnemies();

        // ✅ Spawn logic - รองรับ batch spawning
        if (Time.time >= nextSpawnTime && CanSpawnMore())
        {
            if (enableBatchSpawning)
            {
                SpawnBatch();
            }
            else
            {
                SpawnRandomEnemy();
            }
            UpdateNextSpawnTime();
        }
    }
    private void SpawnBatch()
    {
        // คำนวณจำนวนที่จะ spawn
        int availableSlots = maxTotalEnemies - activeEnemies.Count;
        int enemiesToSpawn = Random.Range(minEnemiesPerSpawn, maxEnemiesPerSpawn + 1);
        enemiesToSpawn = Mathf.Min(enemiesToSpawn, availableSlots);

        if (enemiesToSpawn <= 0) return;

        List<string> spawnedList = new List<string>();

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            EnemySpawnData selectedEnemy = SelectRandomEnemyByWeight();

            if (selectedEnemy == null) continue;

            Vector3 spawnPosition = spawnAroundPlayers
                ? GetSpawnPositionAroundPlayers()
                : transform.position;

            NetworkEnemy enemy = Runner.Spawn(
                selectedEnemy.enemyPrefab,
                spawnPosition,
                Quaternion.identity,
                PlayerRef.None
            );

            if (enemy != null)
            {
                activeEnemies.Add(enemy);
                spawnedEnemyTypes[enemy.Object] = selectedEnemy.enemyName;
                selectedEnemy.currentCount++;
                selectedEnemy.lastSpawnTime = Time.time;

                spawnedList.Add(selectedEnemy.enemyName);
            }
        }

        if (showSpawnInfo && spawnedList.Count > 0)
        {
            // นับจำนวนแต่ละประเภท
            Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (string name in spawnedList)
            {
                if (counts.ContainsKey(name))
                    counts[name]++;
                else
                    counts[name] = 1;
            }

            string spawnInfo = "🌊 Spawned batch: ";
            foreach (var kvp in counts)
            {
                spawnInfo += $"{kvp.Key} x{kvp.Value}, ";
            }
            spawnInfo += $"| Active: {activeEnemies.Count}/{maxTotalEnemies}";

            Debug.Log(spawnInfo);
        }
    }

    // ========== Spawn Logic ==========
    private void SpawnRandomEnemy()
    {
        EnemySpawnData selectedEnemy = SelectRandomEnemyByWeight();

        if (selectedEnemy == null)
        {
            if (showDebugInfo)
                Debug.LogWarning("[Spawn] No valid enemy to spawn");
            return;
        }

        Vector3 spawnPosition = spawnAroundPlayers
            ? GetSpawnPositionAroundPlayers()
            : transform.position;

        NetworkEnemy enemy = Runner.Spawn(
            selectedEnemy.enemyPrefab,
            spawnPosition,
            Quaternion.identity,
            PlayerRef.None
        );

        if (enemy != null)
        {
            activeEnemies.Add(enemy);
            spawnedEnemyTypes[enemy.Object] = selectedEnemy.enemyName;
            selectedEnemy.currentCount++;
            selectedEnemy.lastSpawnTime = Time.time;

            if (showSpawnInfo)
            {
                Debug.Log($"✅ Spawned {selectedEnemy.enemyName} | Active: {activeEnemies.Count}/{maxTotalEnemies}");
            }
        }
    }

    private EnemySpawnData SelectRandomEnemyByWeight()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            return null;

        List<EnemySpawnData> weightedList = new List<EnemySpawnData>();

        foreach (EnemySpawnData enemy in enemyPrefabs)
        {
            if (enemy.enemyPrefab == null) continue;
            if (enemy.maxCount > 0 && enemy.currentCount >= enemy.maxCount) continue;
            if (Time.time < enemy.lastSpawnTime + enemy.spawnCooldown) continue;

            for (int i = 0; i < enemy.spawnWeight; i++)
            {
                weightedList.Add(enemy);
            }
        }

        if (weightedList.Count == 0)
            return null;

        return weightedList[Random.Range(0, weightedList.Count)];
    }

    private bool CanSpawnMore()
    {
        return activeEnemies.Count < maxTotalEnemies;
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

    // ========== Spawn Position ==========
    private Vector3 GetSpawnPositionAroundPlayers()
    {
        List<Transform> players = GetAllPlayerTransforms();

        if (players.Count == 0)
            return transform.position;

        Transform targetPlayer = players[Random.Range(0, players.Count)];

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            float randomDistance = Random.Range(minSpawnDistanceFromPlayer, maxSpawnDistanceFromPlayer);
            float randomAngle = Random.Range(0f, 360f);
            Vector3 direction = Quaternion.Euler(0, randomAngle, 0) * Vector3.forward;

            // ✅ ใช้ตำแหน่ง Y ของผู้เล่นเป็นจุดเริ่มต้น
            Vector3 spawnPosition = targetPlayer.position + direction * randomDistance;

            // เช็คกำแพง
            if (checkWallsBeforeSpawn)
            {
                if (IsPositionBlockedByWall(targetPlayer.position, spawnPosition))
                    continue;
            }

            // ✅ เช็คพื้น (บังคับเสมอ ไม่สนใจ flag)
            Vector3 groundPosition = FindGroundPosition(spawnPosition);

            // ✅ ถ้าได้ตำแหน่งกลับมา ให้ใช้เลย (ไม่เช็คว่าเป็น Vector3.zero)
            return groundPosition;
        }

        // Fallback: ใช้ตำแหน่งผู้เล่น + offset
        Vector3 fallbackPosition = targetPlayer.position +
                                   (Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward) *
                                   maxSpawnDistanceFromPlayer;

        Vector3 finalPosition = FindGroundPosition(fallbackPosition);
        return finalPosition;
    }

    private bool IsPositionBlockedByWall(Vector3 startPos, Vector3 targetPos)
    {
        Vector3 direction = (targetPos - startPos).normalized;
        float distance = Vector3.Distance(startPos, targetPos);

        RaycastHit hit;
        return Physics.Raycast(
            startPos + Vector3.up * 0.5f,
            direction,
            out hit,
            distance,
            LayerMask.GetMask("Wall", "Obstacle", "Default")
        );
    }
    // ✅ หาตำแหน่งพื้นที่ถูกต้อง (แก้ไขใหม่)
    private Vector3 FindGroundPosition(Vector3 position)
    {
        // 1. ยิง Raycast ลงมาจากสูงเพื่อหาพื้น
        RaycastHit hit;
        Vector3 startPos = position + Vector3.up * 10f;

        // ✅ เพิ่ม Layer: "Ground", "Default", "Floor"
        LayerMask groundLayers = LayerMask.GetMask("Default", "Ground", "Floor");

        if (Physics.Raycast(startPos, Vector3.down, out hit, 20f, groundLayers))
        {
            // ✅ ปรับตำแหน่งให้สูงจากพื้น 0.5 เมตร
            Vector3 groundPosition = hit.point + Vector3.up * 0.5f;

            if (showDebugInfo)
            {
                Debug.Log($"✅ Found ground at Y={hit.point.y:F2}, spawning at Y={groundPosition.y:F2}");
                Debug.DrawLine(startPos, hit.point, Color.green, 2f);
            }

            return groundPosition;
        }

        // 2. ถ้าไม่เจอพื้นด้วย Raycast ลองใช้ SphereCast
        if (Physics.SphereCast(startPos, 0.5f, Vector3.down, out hit, 20f, groundLayers))
        {
            Vector3 groundPosition = hit.point + Vector3.up * 0.5f;

            if (showDebugInfo)
            {
                Debug.Log($"✅ Found ground (sphere) at Y={hit.point.y:F2}");
            }

            return groundPosition;
        }

        // 3. ถ้ายังไม่เจอ ใช้ตำแหน่งผู้เล่นแทน
        List<Transform> players = GetAllPlayerTransforms();
        if (players.Count > 0)
        {
            Vector3 playerGroundPos = players[0].position;
            playerGroundPos.y += 0.5f; // สูงจากผู้เล่นนิดหน่อย

            if (showDebugInfo)
            {
                Debug.LogWarning($"⚠️ No ground found, using player Y position: {playerGroundPos.y:F2}");
            }

            return playerGroundPos;
        }

        if (showDebugInfo)
        {
            Debug.LogError($"❌ No ground found at all! Using original position");
            Debug.DrawLine(startPos, startPos + Vector3.down * 20f, Color.red, 2f);
        }

        return position; // ใช้ตำแหน่งเดิม (แทนที่จะ return Vector3.zero)
    }
    private List<Transform> GetAllPlayerTransforms()
    {
        List<Transform> playerTransforms = new List<Transform>();
        Hero[] heroes = FindObjectsOfType<Hero>();

        foreach (Hero hero in heroes)
        {
            if (hero != null && hero.IsSpawned)
                playerTransforms.Add(hero.transform);
        }

        return playerTransforms;
    }

    // ========== Cleanup ==========
    private void CleanupDeadEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            NetworkEnemy enemy = activeEnemies[i];

            if (enemy == null || enemy.IsDead)
            {
                if (enemy != null && enemy.Object != null && spawnedEnemyTypes.ContainsKey(enemy.Object))
                {
                    string enemyTypeName = spawnedEnemyTypes[enemy.Object];
                    spawnedEnemyTypes.Remove(enemy.Object);

                    foreach (EnemySpawnData enemyData in enemyPrefabs)
                    {
                        if (enemyData.enemyName == enemyTypeName)
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

    // ========== Enemy Death Handler ==========
    public void OnEnemyDeath(NetworkEnemy deadEnemy, string enemyTypeName)
    {
        if (!HasStateAuthority) return;

        // ✅ เช็ค Enemy Tag
        EnemyTagComponent tagComponent = deadEnemy.GetComponent<EnemyTagComponent>();

        bool shouldCount = false;
        EnemyTag enemyTag = EnemyTag.Normal; // default

        if (tagComponent != null)
        {
            enemyTag = tagComponent.enemyTag;
            shouldCount = DoesEnemyCountForStage(enemyTag);
        }
        else
        {
            // ถ้าไม่มี Component ให้ถือว่าเป็น Normal
            shouldCount = DoesEnemyCountForStage(EnemyTag.Normal);

            if (showDebugInfo)
            {
                Debug.LogWarning($"⚠️ {enemyTypeName} has no EnemyTagComponent! Treating as Normal");
            }
        }

        // ✅ นับเฉพาะที่ shouldCount = true
        if (shouldCount)
        {
            currentSessionKills++;

            if (showDebugInfo || showSpawnInfo)
            {
                Debug.Log($"🔥 Enemy killed: {enemyTypeName} [{enemyTag}] | Total: {currentSessionKills}/{requiredKillsForStage}");
            }
        }
        else
        {
            if (showDebugInfo || showSpawnInfo)
            {
                Debug.Log($"💨 Enemy killed (not counted): {enemyTypeName} [{enemyTag}] | Total: {currentSessionKills}/{requiredKillsForStage}");
            }
        }

        // อัปเดต StageRewardTracker
        RPC_UpdateStageRewardTracker(enemyTypeName);

        // เช็คการผ่านด่าน
        if (currentSessionKills >= requiredKillsForStage && !isStageCompleted)
        {
            ForceCheckStageCompletion();
        }
    }
    public NetworkEnemy SpawnSummonedEnemy(NetworkEnemy prefab, Vector3 position)
    {
        if (!HasStateAuthority)
        {
            Debug.LogWarning("[EnemySpawner] Only Host can spawn summoned enemies!");
            return null;
        }

        NetworkEnemy summonedEnemy = Runner.Spawn(prefab, position, Quaternion.identity, PlayerRef.None);

        if (summonedEnemy != null)
        {
            // ตั้ง Tag เป็น Summoned
            EnemyTagComponent tagComponent = summonedEnemy.GetComponent<EnemyTagComponent>();

            if (tagComponent == null)
            {
                tagComponent = summonedEnemy.gameObject.AddComponent<EnemyTagComponent>();
            }

            tagComponent.SetTag(EnemyTag.Summoned);

            activeEnemies.Add(summonedEnemy);

            if (showDebugInfo)
            {
                Debug.Log($"👻 Boss summoned: {prefab.name} [Tag: Summoned - Won't count]");
            }
        }

        return summonedEnemy;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateStageRewardTracker(string enemyTypeName)
    {
        StageRewardTracker.AddEnemyKill();
    }

    // ========== Stage Completion ==========
    private void CheckStageCompletion()
    {
        if (string.IsNullOrEmpty(currentStageName)) return;

        bool wasCompleted = isStageCompleted;
        isStageCompleted = currentSessionKills >= requiredKillsForStage;

        if (!wasCompleted && isStageCompleted)
        {
            OnStageCompleted();
        }
    }

    private void ForceCheckStageCompletion()
    {
        lastStageCheckTime = 0f;
        CheckStageCompletion();
    }

    private void OnStageCompleted()
    {
        StageProgressManager.CompleteStage(currentStageName);

        if (stopSpawningWhenStageCompleted)
        {
            nextSpawnTime = float.MaxValue;

            if (destroyRemainingEnemiesOnStageComplete)
            {
                DestroyRemainingEnemies();
            }

            RPC_AnnounceStageCompleted(currentStageName);
        }
    }

    private void DestroyRemainingEnemies()
    {
        if (!HasStateAuthority) return;

        int destroyedCount = 0;

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            NetworkEnemy enemy = activeEnemies[i];
            if (enemy != null && !enemy.IsDead)
            {
                if (enemy.Object != null && spawnedEnemyTypes.ContainsKey(enemy.Object))
                {
                    string enemyTypeName = spawnedEnemyTypes[enemy.Object];
                    spawnedEnemyTypes.Remove(enemy.Object);

                    foreach (EnemySpawnData enemyData in enemyPrefabs)
                    {
                        if (enemyData.enemyName == enemyTypeName)
                        {
                            enemyData.currentCount = Mathf.Max(0, enemyData.currentCount - 1);
                            break;
                        }
                    }
                }

                if (enemy.Object != null)
                {
                    Runner.Despawn(enemy.Object);
                    destroyedCount++;
                }
            }
            activeEnemies.RemoveAt(i);
        }

        if (showDebugInfo && destroyedCount > 0)
        {
            Debug.Log($"🧹 Destroyed {destroyedCount} remaining enemies");
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AnnounceStageCompleted(string stageName)
    {
        StageRewardTracker.Instance?.SetCorrectEnemyCount(currentSessionKills);

        StageCompleteUI stageUI = FindObjectOfType<StageCompleteUI>();
        if (stageUI != null)
        {
            if (stageUI.stageCompletePanel != null && stageUI.stageCompletePanel.activeSelf)
            {
                stageUI.HideStageComplete();
            }
            stageUI.ShowStageComplete(stageName);
        }
    }

    // ========== Lobby Dummy System ==========
    private IEnumerator SpawnLobbyDummy()
    {
        yield return new WaitForSeconds(1f);

        NetworkEnemy[] existingEnemies = FindObjectsOfType<NetworkEnemy>();
        bool hasExistingDummy = false;

        foreach (NetworkEnemy enemy in existingEnemies)
        {
            if (enemy != null && enemy.isLobbyDummy && !enemy.IsDead)
            {
                hasExistingDummy = true;
                break;
            }
        }

        if (!hasExistingDummy && dummyPrefab != null && dummySpawnPoint != null)
        {
            SpawnDummyAtPosition(dummySpawnPoint.position);
        }
    }

    private void CheckAndSpawnDummyIfNeeded()
    {
        if (!HasStateAuthority) return;

        NetworkEnemy[] allEnemies = FindObjectsOfType<NetworkEnemy>();
        NetworkEnemy existingDummy = null;

        foreach (NetworkEnemy enemy in allEnemies)
        {
            if (enemy != null && enemy.IsDummy && !enemy.IsDead)
            {
                existingDummy = enemy;
                break;
            }
        }

        if (existingDummy == null)
        {
            SpawnDummy();
        }
        else
        {
            currentDummyInstance = existingDummy;
        }
    }

    private void SpawnDummy()
    {
        if (dummyPrefab == null)
        {
            Debug.LogError("[EnemySpawner] Dummy prefab is not assigned!");
            return;
        }

        Vector3 spawnPosition = dummySpawnPoint != null
            ? dummySpawnPoint.position
            : transform.position + Vector3.forward * 5f;

        SpawnDummyAtPosition(spawnPosition);
    }

    public void SpawnDummyAtPosition(Vector3 position)
    {
        if (!HasStateAuthority) return;

        try
        {
            int dummyLevel = LoadDummyLevelFromFirebase();

            var spawnedObject = Runner.Spawn(
                dummyPrefab,
                position,
                Quaternion.identity,
                PlayerRef.None
            );

            if (spawnedObject != null)
            {
                var enemy = spawnedObject.GetComponent<NetworkEnemy>();
                if (enemy != null)
                {
                    enemy.IsDummy = true;
                    enemy.DummyLevel = dummyLevel;
                    enemy.isLobbyDummy = true;

                    currentDummyInstance = enemy;
                    hasDummySpawned = true;

                    Debug.Log($"[EnemySpawner] ✅ Dummy spawned at Level {dummyLevel}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnemySpawner] Error spawning Dummy: {e.Message}");
        }
    }

    private int LoadDummyLevelFromFirebase()
    {
        try
        {
            var persistentData = PersistentPlayerData.Instance;

            if (persistentData?.multiCharacterData == null)
                return 1;

            persistentData.multiCharacterData.CheckAndResetDummyIfNewDay();
            return persistentData.multiCharacterData.trainingDummy.currentLevel;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnemySpawner] Error loading level: {e.Message}");
            return 1;
        }
    }

    // ========== Public Methods ==========
    public void RestartSpawning()
    {
        if (!HasStateAuthority) return;

        isStageCompleted = false;
        currentSessionKills = 0;

        // ✅ รอ 6 วินาทีใหม่
        firstSpawnTime = Time.time + 8f;
        nextSpawnTime = firstSpawnTime;
        isFirstSpawnReady = false;

        lastStageCheckTime = 0f;

        foreach (EnemySpawnData enemy in enemyPrefabs)
        {
            enemy.currentCount = 0;
            enemy.lastSpawnTime = 0f;
        }

        Debug.Log($"🔄 Spawning restarted for stage: {currentStageName}");
        Debug.Log($"⏰ Next spawn in 6 seconds at {firstSpawnTime:F1}s");
    }

    public int GetRequiredKills()
    {
        return requiredKillsForStage;
    }

    public bool IsCurrentStageCompleted()
    {
        return isStageCompleted;
    }

    public void ClearAllEnemies()
    {
        if (!HasStateAuthority) return;

        foreach (NetworkEnemy enemy in activeEnemies)
        {
            if (enemy != null)
                Runner.Despawn(enemy.Object);
        }

        activeEnemies.Clear();
        spawnedEnemyTypes.Clear();

        foreach (EnemySpawnData enemy in enemyPrefabs)
        {
            enemy.currentCount = 0;
        }

        Debug.Log("[EnemySpawner] All enemies cleared");
    }
    private void LoadRequiredEnemyTags()
    {
        string normalizedSceneName = currentStageName.ToLower();
        string tagsString = PlayerPrefs.GetString($"RequiredTags_{normalizedSceneName}", "");

        if (string.IsNullOrEmpty(tagsString))
        {
            requiredEnemyTags = new EnemyTag[] { };
            Debug.Log($"[EnemySpawner] No specific tags - counting all combat enemies");
        }
        else
        {
            string[] tagNames = tagsString.Split(',');
            List<EnemyTag> tagList = new List<EnemyTag>();

            foreach (string tagName in tagNames)
            {
                try
                {
                    EnemyTag tag = (EnemyTag)System.Enum.Parse(typeof(EnemyTag), tagName.Trim());
                    tagList.Add(tag);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[EnemySpawner] Failed to parse tag: {tagName}");
                }
            }

            requiredEnemyTags = tagList.ToArray();
            Debug.Log($"[EnemySpawner] Loaded tags: {string.Join(", ", requiredEnemyTags)}");
        }
    }

    /// <summary>
    /// แสดง Required Tags เป็น String
    /// </summary>
    private string GetRequiredTagsString()
    {
        if (requiredEnemyTags == null || requiredEnemyTags.Length == 0)
        {
            return "All Combat Enemies (Normal, Boss, MiniBoss, Elite)";
        }

        return string.Join(", ", requiredEnemyTags);
    }

    /// <summary>
    /// เช็คว่า Enemy Tag นี้นับในด่านนี้หรือไม่
    /// </summary>
    private bool DoesEnemyCountForStage(EnemyTag tag)
    {
        // ถ้าไม่ได้กำหนด tags = นับทุกตัวที่เป็น combat enemy
        if (requiredEnemyTags == null || requiredEnemyTags.Length == 0)
        {
            return tag == EnemyTag.Normal ||
                   tag == EnemyTag.Boss ||
                   tag == EnemyTag.MiniBoss ||
                   tag == EnemyTag.Elite;
        }

        // ถ้ากำหนดไว้ = ต้องตรงกับที่กำหนดเท่านั้น
        foreach (EnemyTag requiredTag in requiredEnemyTags)
        {
            if (tag == requiredTag)
                return true;
        }

        return false;
    }

}