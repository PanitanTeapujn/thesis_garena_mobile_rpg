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
    // แก้ไข SelectRandomEnemyByWeight() ให้ไม่สนใจ currentCount ใน batch เดียวกัน

    private void SpawnBatch()
    {
        int availableSlots = maxTotalEnemies - activeEnemies.Count;
        int enemiesToSpawn = Random.Range(minEnemiesPerSpawn, maxEnemiesPerSpawn + 1);
        enemiesToSpawn = Mathf.Min(enemiesToSpawn, availableSlots);

        if (enemiesToSpawn <= 0) return;

        List<string> spawnedList = new List<string>();

        // ✅ เก็บจำนวนชั่วคราวสำหรับ batch นี้
        Dictionary<string, int> tempSpawnCount = new Dictionary<string, int>();

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            // ✅ สุ่มใหม่ทุกครั้ง โดยใช้ weight อย่างเดียว (ไม่สน currentCount)
            EnemySpawnData selectedEnemy = SelectRandomEnemyByWeightForBatch(); // ✅ เปลี่ยนตรงนี้

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

                // ✅ อัพเดท count หลังจาก spawn สำเร็จแล้ว
                selectedEnemy.currentCount++;
                selectedEnemy.lastSpawnTime = Time.time;

                spawnedList.Add(selectedEnemy.enemyName);
            }
        }

        if (showSpawnInfo && spawnedList.Count > 0)
        {
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

    // ✅ Method ใหม่: สุ่มตาม weight เท่านั้น (ไม่เช็ค maxCount)
    private EnemySpawnData SelectRandomEnemyByWeightOnly()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            return null;

        List<EnemySpawnData> weightedList = new List<EnemySpawnData>();

        foreach (EnemySpawnData enemy in enemyPrefabs)
        {
            if (enemy.enemyPrefab == null) continue;

            // ✅ ไม่เช็ค currentCount และ cooldown ใน batch spawn
            // เช็คแค่ว่ามี prefab หรือไม่

            for (int i = 0; i < enemy.spawnWeight; i++)
            {
                weightedList.Add(enemy);
            }
        }

        if (weightedList.Count == 0)
            return null;

        return weightedList[Random.Range(0, weightedList.Count)];
    }
    private EnemySpawnData SelectRandomEnemyByWeightForBatch()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            return null;

        List<EnemySpawnData> weightedList = new List<EnemySpawnData>();

        foreach (EnemySpawnData enemy in enemyPrefabs)
        {
            if (enemy.enemyPrefab == null) continue;

            // ✅ เช็คแค่ maxCount (ถ้ามี)
            if (enemy.maxCount > 0 && enemy.currentCount >= enemy.maxCount) continue;

            // ✅ ไม่เช็ค cooldown ใน batch spawn

            for (int i = 0; i < enemy.spawnWeight; i++)
            {
                weightedList.Add(enemy);
            }
        }

        if (weightedList.Count == 0)
            return null;

        return weightedList[Random.Range(0, weightedList.Count)];
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
            // ✅ สุ่มระยะและมุมใหม่ทุกครั้ง
            float randomDistance = Random.Range(minSpawnDistanceFromPlayer, maxSpawnDistanceFromPlayer);
            float randomAngle = Random.Range(0f, 360f); // ✅ สุ่ม 0-360 องศา

            Vector3 direction = Quaternion.Euler(0, randomAngle, 0) * Vector3.forward;

            // ✅ คำนวณตำแหน่งใหม่
            Vector3 spawnPosition = targetPlayer.position + direction * randomDistance;

            // เช็คกำแพง
            if (checkWallsBeforeSpawn)
            {
                if (IsPositionBlockedByWall(targetPlayer.position, spawnPosition))
                {
                    if (showDebugInfo)
                    {
                        Debug.Log($"❌ Attempt {attempt + 1}: Wall blocked at distance {randomDistance:F1}m, angle {randomAngle:F0}°");
                    }
                    continue; // ลองใหม่
                }
            }

            // ✅ หาพื้น
            Vector3 groundPosition = FindGroundPosition(spawnPosition);

            if (showDebugInfo)
            {
                Debug.Log($"✅ Spawn attempt {attempt + 1}: Distance={randomDistance:F1}m, Angle={randomAngle:F0}°, Y={groundPosition.y:F2}");
            }

            return groundPosition;
        }

        // Fallback: ถ้าลองหมดแล้วไม่ได้
        Vector3 fallbackPosition = targetPlayer.position +
                                   (Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward) *
                                   maxSpawnDistanceFromPlayer;

        Vector3 finalPosition = FindGroundPosition(fallbackPosition);

        if (showDebugInfo)
        {
            Debug.LogWarning($"⚠️ Used fallback position: {finalPosition}");
        }

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

        LayerMask groundLayers = LayerMask.GetMask("Default", "Ground", "Floor");

        if (Physics.Raycast(startPos, Vector3.down, out hit, 20f, groundLayers))
        {
            // ✅ ใช้ตำแหน่ง Y ของผู้เล่น + offset
            List<Transform> players = GetAllPlayerTransforms();
            float targetY = players.Count > 0 ? players[0].position.y : hit.point.y;

            // ✅ เพิ่ม offset สำหรับ enemy ตัวใหญ่ (ปรับได้ตามต้องการ)
            float enemyOffset = 1.9f; // เพิ่มจาก 0.5f → 1.9f เพื่อให้ไม่จมดิน

            Vector3 groundPosition = new Vector3(hit.point.x, targetY + enemyOffset, hit.point.z);

            if (showDebugInfo)
            {
                Debug.Log($"✅ Enemy spawn Y={groundPosition.y:F2} (Player Y={targetY:F2} + Offset={enemyOffset})");
            }

            return groundPosition;
        }

        // 2. ถ้าไม่เจอพื้นด้วย Raycast ลองใช้ SphereCast
        if (Physics.SphereCast(startPos, 0.5f, Vector3.down, out hit, 20f, groundLayers))
        {
            List<Transform> players = GetAllPlayerTransforms();
            float targetY = players.Count > 0 ? players[0].position.y : hit.point.y;
            float enemyOffset = 1.9f;

            Vector3 groundPosition = new Vector3(hit.point.x, targetY + enemyOffset, hit.point.z);

            if (showDebugInfo)
            {
                Debug.Log($"✅ Found ground (sphere), Y={groundPosition.y:F2}");
            }

            return groundPosition;
        }

        // 3. Fallback: ใช้ตำแหน่ง Y ของผู้เล่น
        List<Transform> fallbackPlayers = GetAllPlayerTransforms();
        if (fallbackPlayers.Count > 0)
        {
            float enemyOffset = 1.9f;
            Vector3 playerGroundPos = new Vector3(position.x, fallbackPlayers[0].position.y + enemyOffset, position.z);

            if (showDebugInfo)
            {
                Debug.LogWarning($"⚠️ No ground found, using player Y + offset: {playerGroundPos.y:F2}");
            }

            return playerGroundPos;
        }

        if (showDebugInfo)
        {
            Debug.LogError($"❌ No ground found at all! Using original position");
        }

        return position;
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

        // ✅ เพิ่มบรรทัดนี้: เช็คว่าศัตรูตายหมดหรือยัง
        CheckAllEnemiesDefeated();

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
    private void CheckAllEnemiesDefeated()
    {
        // นับจำนวนศัตรูที่ยังมีชีวิต
        int aliveEnemies = 0;

        foreach (var enemy in activeEnemies)
        {
            // ✅ แก้ไข: ใช้ IsEnemyAlive() แทน IsDead
            if (enemy != null && IsEnemyAlive(enemy))
            {
                aliveEnemies++;
            }
        }

        Debug.Log($"[EnemySpawner] Alive enemies: {aliveEnemies}");

        // ✅ ถ้าไม่มีศัตรูเหลือแล้ว และไม่มี wave ที่จะ spawn แล้ว
        if (aliveEnemies == 0 && !HasMoreWaves())
        {
            Debug.Log("[EnemySpawner] 🏆 ALL ENEMIES DEFEATED!");

            // ✅ แจ้ง GameStateManager
            GameStateManager gameState = FindObjectOfType<GameStateManager>();

            if (gameState != null)
            {
                gameState.OnAllEnemiesDefeated();
            }
            else
            {
                Debug.LogWarning("[EnemySpawner] GameStateManager not found!");

                // Fallback - แสดง StageCompleteUI โดยตรง
                StageCompleteUI stageCompleteUI = FindObjectOfType<StageCompleteUI>();
                if (stageCompleteUI != null)
                {
                    stageCompleteUI.ShowStageComplete(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                }
            }
        }
    }

    /// <summary>
    /// ✅ เช็คว่ายังมี wave ที่จะ spawn หรือไม่
    /// </summary>
    private bool HasMoreWaves()
    {
        // ถ้าไม่มีระบบ wave - return false
        return false;
    }

    /// <summary>
    /// ✅ Helper method: เช็คว่า Enemy ยังมีชีวิตหรือไม่
    /// </summary>
    private bool IsEnemyAlive(NetworkEnemy enemy)
    {
        if (enemy == null) return false;

        // NetworkEnemy มี IsDead property
        return !enemy.IsDead;
    }
}