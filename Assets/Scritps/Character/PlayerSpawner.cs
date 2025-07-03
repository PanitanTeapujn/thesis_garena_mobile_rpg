using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
public class PlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Character Prefabs")]
    public GameObject bloodKnightPrefab;
    public GameObject archerPrefab;
    public GameObject assassinPrefab;
    public GameObject ironJuggernautPrefab;

    [Header("Network Manager Prefab")]
    public GameObject networkPlayerManagerPrefab; // ต้องสร้าง prefab นี้
    [Header("Spawn Settings")]
    public Transform[] spawnPoints; // สำหรับกำหนดจุด spawn แน่นอน
    public float spawnHeight = 2f; // ความสูงในการ spawn
    public LayerMask groundLayerMask = 1; // Layer ของพื้น
    [SerializeField] private bool autoCreateSpawnPoints = true; // ✅ สร้างอัตโนมัติ
    [SerializeField] private bool showSpawnPointDebug = true; // ✅ แสดง debug
    private Dictionary<Hero, bool> heroStatsReady = new Dictionary<Hero, bool>();

    private NetworkRunner _runner;

    private HashSet<string> spawnRequests = new HashSet<string>();

    [Header("Combat UI")]
    public CombatUIManager combatUIManagerPrefab;
    private Dictionary<Hero, GameObject> heroWorldUIs = new Dictionary<Hero, GameObject>();

    [Header("World UI")]
    public GameObject worldSpaceUIPrefab;
    // Dictionary เพื่อเก็บข้อมูลตัวละครของแต่ละ player
    private Dictionary<PlayerRef, PlayerSelectionData.CharacterType> playerCharacters = new Dictionary<PlayerRef, PlayerSelectionData.CharacterType>();

    // Dictionary เพื่อเก็บ NetworkPlayerManager ของแต่ละ player
    private Dictionary<PlayerRef, NetworkPlayerManager> playerManagers = new Dictionary<PlayerRef, NetworkPlayerManager>();

    // Dictionary เพื่อเก็บ spawned characters ของแต่ละ player
    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    private void Awake()
    {
        _runner = FindObjectOfType<NetworkRunner>();

        // ✅ สร้าง spawn points อัตโนมัติถ้าไม่มี
        if (autoCreateSpawnPoints && (spawnPoints == null || spawnPoints.Length == 0))
        {
            CreateDefaultSpawnPointsRuntime();
        }

        if (_runner != null)
        {
            _runner.AddCallbacks(this);
            Debug.Log($"PlayerSpawner registered with NetworkRunner. IsServer: {_runner.IsServer}");
        }
        else
        {
            Debug.LogError("NetworkRunner not found in the scene!");
        }
    }
    private void OnEnable()
    {
        _runner = FindObjectOfType<NetworkRunner>();
        if (_runner != null)
        {
            _runner.AddCallbacks(this);
            Debug.Log($"PlayerSpawner registered with NetworkRunner. IsServer: {_runner.IsServer}");
        }
        else
        {
            Debug.LogError("NetworkRunner not found in the scene!");
        }
    }
    private void SetupCombatUI(NetworkObject playerObject)
    {
        if (playerObject == null) return;

        Hero hero = playerObject.GetComponent<Hero>();
        if (hero == null) return;

        // 🆕 รอ stats โหลดเสร็จก่อน setup UI
        StartCoroutine(SetupCombatUIWithStatsWait(hero, playerObject));
    }

    private IEnumerator SetupCombatUIWithStatsWait(Hero hero, NetworkObject playerObject)
    {
        Debug.Log($"[PlayerSpawner] 🔄 Setting up combat UI for {hero.CharacterName}, waiting for stats...");

        yield return new WaitForSeconds(0.5f);

        // รอให้ hero spawn เสร็จ
        while (!hero.IsSpawned)
        {
            yield return null;
        }

        // 🆕 รอให้ stats โหลดเสร็จ (เฉพาะ local player)
        if (hero.HasInputAuthority)
        {
            yield return StartCoroutine(WaitForHeroStatsReady(hero));
        }

        // Setup Screen Space UI (เฉพาะ local player)
        if (hero.HasInputAuthority)
        {
            Debug.Log($"[PlayerSpawner] 🖥️ Setting up Combat UI for local player: {hero.CharacterName}");

            CombatUIManager combatUI = FindObjectOfType<CombatUIManager>();

            if (combatUI == null && combatUIManagerPrefab != null)
            {
                combatUI = Instantiate(combatUIManagerPrefab);
                Debug.Log("[PlayerSpawner] Created new CombatUIManager from prefab");
            }

            if (combatUI != null)
            {
                yield return new WaitForEndOfFrame();
                combatUI.SetLocalHero(hero);
                Debug.Log($"[PlayerSpawner] ✅ Combat UI setup complete for {hero.CharacterName}");
            }
        }

        // 🆕 แจ้งว่า hero พร้อมใช้งานแล้ว (หลัง stats โหลดเสร็จ)
        OnHeroSpawnComplete(hero);
    }
    private IEnumerator WaitForHeroStatsReady(Hero hero)
    {
        Debug.Log($"[PlayerSpawner] ⏳ Waiting for {hero.CharacterName} stats to be ready...");

        Character character = hero.GetComponent<Character>();
        if (character == null)
        {
            Debug.LogError($"[PlayerSpawner] No Character component found on {hero.CharacterName}!");
            yield break;
        }

        // รอให้ Character โหลด stats เสร็จ
        int maxWaitTime = 30; // 30 วินาที
        float waitTime = 0f;

        while (!character.IsStatsLoadingComplete() && waitTime < maxWaitTime)
        {
            yield return new WaitForSeconds(0.1f);
            waitTime += 0.1f;

            // Debug ทุก 3 วินาที
            if (Mathf.RoundToInt(waitTime) % 3 == 0 && waitTime % 1f < 0.1f)
            {
                Debug.Log($"[PlayerSpawner] Still waiting for {hero.CharacterName} stats... ({waitTime:F1}s)");
            }
        }

        if (character.IsStatsLoadingComplete())
        {
            heroStatsReady[hero] = true;
            Debug.Log($"[PlayerSpawner] ✅ {hero.CharacterName} stats ready! Final stats: HP={character.MaxHp}, ATK={character.AttackDamage}");
        }
        else
        {
            Debug.LogWarning($"[PlayerSpawner] ⚠️ Timeout waiting for {hero.CharacterName} stats after {maxWaitTime}s");
            heroStatsReady[hero] = false;
        }
    }


    private IEnumerator SetupCombatUIWithDelay(Hero hero, NetworkObject playerObject)
    {
        yield return new WaitForSeconds(0.5f);

        while (!hero.IsSpawned)
        {
            yield return null;
        }

        // Setup Screen Space UI (เฉพาะ local player)
        if (hero.HasInputAuthority)
        {
            Debug.Log($"Setting up Combat UI for local player: {hero.CharacterName}");

            CombatUIManager combatUI = FindObjectOfType<CombatUIManager>();

            if (combatUI == null && combatUIManagerPrefab != null)
            {
                combatUI = Instantiate(combatUIManagerPrefab);
                Debug.Log("Created new CombatUIManager from prefab");
            }

            if (combatUI != null)
            {
                yield return new WaitForEndOfFrame();
                combatUI.SetLocalHero(hero);
            }
        }

        // ไม่สร้าง WorldSpaceUI ที่นี่ เพราะจะสร้างผ่าน RPC แทน
    }
    private IEnumerator SetHeroAfterDelay(CombatUIManager combatUI, Hero hero)
    {
        // รอให้ CombatUIManager setup เสร็จ
        yield return new WaitForSeconds(0.5f);

        combatUI.SetLocalHero(hero);
        Debug.Log($"Hero set to CombatUIManager after delay: {hero.CharacterName}");
    }
    private void Update()
    {
        if (_runner == null)
        {
            _runner = FindObjectOfType<NetworkRunner>();
            if (_runner != null)
            {
                _runner.AddCallbacks(this);
                Debug.Log("PlayerSpawner registered with NetworkRunner");
            }
        }
    }
    public void OnHeroSpawnComplete(Hero hero)
    {
        Debug.Log($"[PlayerSpawner] 🎉 Hero spawn complete: {hero.CharacterName}, HasInput: {hero.HasInputAuthority}");

        // ตรวจสอบว่า stats พร้อมหรือไม่
        bool statsReady = heroStatsReady.ContainsKey(hero) ? heroStatsReady[hero] : false;
        Debug.Log($"[PlayerSpawner] Stats ready for {hero.CharacterName}: {statsReady}");

        // Setup UI สำหรับ local player (ถ้า stats พร้อมแล้ว)
        if (hero.HasInputAuthority && statsReady)
        {
            StartCoroutine(EnsureUISetup(hero));
        }

        // สร้าง WorldSpaceUI สำหรับ hero นี้
        StartCoroutine(DelayedWorldUISetup(hero));

        // 🆕 Debug final stats
        Character character = hero.GetComponent<Character>();
        if (character != null)
        {
            Debug.Log($"[PlayerSpawner] 📊 Final spawned stats for {hero.CharacterName}: HP={character.MaxHp}, ATK={character.AttackDamage}, ARM={character.Armor}");
        }
    }
    private IEnumerator DelayedWorldUISetup(Hero hero)
    {
        // รอให้ network state พร้อม
        while (!hero.IsNetworkStateReady)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // 🆕 รอให้ stats พร้อม (สำหรับ UI display)
        Character character = hero.GetComponent<Character>();
        if (character != null)
        {
            yield return new WaitUntil(() => character.IsStatsLoadingComplete());
        }

        // รอเพิ่มอีกนิดเพื่อให้แน่ใจ
        yield return new WaitForSeconds(0.2f);

        CreateWorldSpaceUIForHero(hero);
    }
    private IEnumerator EnsureUISetup(Hero hero)
    {
        yield return new WaitForSeconds(0.1f);

        CombatUIManager combatUI = FindObjectOfType<CombatUIManager>();
        if (combatUI != null && combatUI.localHero == null)
        {
            combatUI.SetLocalHero(hero);
            Debug.Log("UI setup completed for late-joining player");
        }
    }
    public void CreateWorldSpaceUIForHero(Hero hero)
    {
        if (hero == null || worldSpaceUIPrefab == null) return;

        // ตรวจสอบว่าสร้าง UI ไปแล้วหรือยัง
        if (heroWorldUIs.ContainsKey(hero)) return;

        Debug.Log($"Creating WorldSpaceUI for {hero.CharacterName}");

        GameObject worldUI = Instantiate(worldSpaceUIPrefab);
        WorldSpaceUI worldSpaceUI = worldUI.GetComponent<WorldSpaceUI>();

        if (worldSpaceUI != null)
        {
            worldSpaceUI.Initialize(hero);
            heroWorldUIs[hero] = worldUI;
            Debug.Log($"WorldSpaceUI created and initialized for {hero.CharacterName}");
        }
    }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[SPAWNER] OnPlayerJoined - Player: {player}, IsServer: {runner.IsServer}, LocalPlayer: {runner.LocalPlayer}");

        _runner = runner;

        if (!runner.IsServer)
        {
            Debug.Log("Not server, skipping spawn");
            return;
        }

        // ตรวจสอบและลบข้อมูลเก่าของผู้เล่นนี้ (ถ้ามี)
        if (spawnedCharacters.ContainsKey(player))
        {
            Debug.Log($"Removing old character data for player {player}");
            spawnedCharacters.Remove(player);
        }
        if (playerCharacters.ContainsKey(player))
        {
            playerCharacters.Remove(player);
        }
        if (playerManagers.ContainsKey(player))
        {
            playerManagers.Remove(player);
        }

        // Spawn NetworkPlayerManager สำหรับ player นี้ก่อน
        if (networkPlayerManagerPrefab != null)
        {
            NetworkObject managerObject = runner.Spawn(
                networkPlayerManagerPrefab,
                Vector3.zero,
                Quaternion.identity,
                player
            );

            if (managerObject != null)
            {
                NetworkPlayerManager manager = managerObject.GetComponent<NetworkPlayerManager>();
                if (manager != null)
                {
                    playerManagers[player] = manager;
                    Debug.Log($"NetworkPlayerManager spawned for player {player}");
                }
            }
        }

        // Spawn character
        if (player != runner.LocalPlayer)
        {
            StartCoroutine(DelayedSpawn(player));

            if (!spawnedCharacters.ContainsKey(runner.LocalPlayer))
            {
                Debug.Log($"[SPAWNER] Also spawning host player");
                StartCoroutine(DelayedSpawn(runner.LocalPlayer));
            }
        }
        else
        {
            StartCoroutine(DelayedSpawn(player));
        }
    }
    IEnumerator DelayedSpawn(PlayerRef player)
    {
        string spawnKey = $"{player}_{Time.time}";

      //  Debug.Log($"[DELAYED SPAWN] Starting for {player}, Key: {spawnKey}");

        // ตรวจสอบว่ามี request ซ้ำหรือไม่
        if (spawnRequests.Contains(player.ToString()))
        {
            Debug.LogWarning($"[DUPLICATE REQUEST] Already spawning {player}");
            yield break;
        }

        spawnRequests.Add(player.ToString());

        yield return new WaitForSeconds(0.5f);

        Debug.Log($"[DELAYED SPAWN] Now spawning {player}");
        PlayerSelectionData.CharacterType selectedCharacter = PlayerSelectionData.GetSelectedCharacter();
        SpawnCharacterForPlayer(player, selectedCharacter);

        // ลบออกหลัง spawn เสร็จ
        spawnRequests.Remove(player.ToString());
    }
    // เมธอดใหม่สำหรับ spawn ตัวละคร
    public void SpawnCharacterForPlayer(PlayerRef player, PlayerSelectionData.CharacterType characterType)
    {
        // ใช้ runner ที่หาได้ล่าสุด
        NetworkRunner currentRunner = _runner;
     //   Debug.Log($"[SPAWN] Attempting to spawn for {player}, Already spawned: {spawnedCharacters.ContainsKey(player)}");
       // Debug.Log($"[SPAWN] Called for {player}, Stack: {System.Environment.StackTrace}");

        if (spawnedCharacters.ContainsKey(player))
        {
            Debug.LogWarning($"Player {player} already has a character spawned!");
            return;
        }
      //  Debug.Log($"[SPAWNER] Spawning for {player}, Runner.LocalPlayer: {_runner.LocalPlayer}, IsServer: {_runner.IsServer}");

        if (currentRunner == null)
        {
            currentRunner = FindObjectOfType<NetworkRunner>();
            if (currentRunner == null)
            {
                Debug.LogError("NetworkRunner not found!");
                return;
            }
        }

        if (!currentRunner.IsServer)
        {
           // Debug.LogError($"Only server can spawn characters! IsServer: {currentRunner.IsServer}");
            return;
        }

        // ตรวจสอบว่า player นี้ spawn ตัวละครไปแล้วหรือยัง
        if (spawnedCharacters.ContainsKey(player))
        {
           // Debug.LogWarning($"Player {player} already has a character spawned!");
            return;
        }

        // บันทึกตัวละครที่ player เลือก
        playerCharacters[player] = characterType;

        // เลือก prefab ตามตัวละครที่เลือก
        GameObject prefabToSpawn = GetPrefabForCharacter(characterType);

        if (prefabToSpawn == null)
        {
            Debug.LogError($"No prefab found for character: {characterType}");
            return;
        }

        // สร้างตำแหน่งสุ่มสำหรับ spawn
        Vector3 spawnPosition = new Vector3(UnityEngine.Random.Range(-5f, 5f), 0, UnityEngine.Random.Range(-5f, 5f));

        // Spawn ตัวละคร
        NetworkObject playerObject = currentRunner.Spawn(prefabToSpawn, spawnPosition, Quaternion.identity, player, (runner, obj) =>
        {
            // ตั้งค่าเฉพาะสำหรับ Local Player
            if (player == runner.LocalPlayer)
            {
                Debug.Log($"Local player spawned as {characterType}");
                obj.gameObject.tag = "LocalPlayer";
            }
            else
            {
                Debug.Log($"Remote player spawned as {characterType}");
                obj.gameObject.tag = "RemotePlayer";
            }
        });

        if (playerObject != null)
        {
            // บันทึกว่า spawn แล้ว
            spawnedCharacters[player] = playerObject;

            Debug.Log($"Player {player} spawned successfully as {characterType} at {spawnPosition}");

            // ตั้งชื่อ GameObject ตามตัวละคร
            if (player == currentRunner.LocalPlayer)
            {
                playerObject.gameObject.name = $"LocalPlayer_{characterType}";
            }
            else
            {
                playerObject.gameObject.name = $"RemotePlayer_{player}_{characterType}";
            }
        }
        else
        {
            Debug.LogError($"Failed to spawn player as {characterType}!");
        }
        SetupCombatUI(playerObject);

    }

    private GameObject GetPrefabForCharacter(PlayerSelectionData.CharacterType character)
    {
        switch (character)
        {
            case PlayerSelectionData.CharacterType.BloodKnight:
                return bloodKnightPrefab;
            case PlayerSelectionData.CharacterType.Archer:
                return archerPrefab;
            case PlayerSelectionData.CharacterType.Assassin:
                return assassinPrefab;
            case PlayerSelectionData.CharacterType.IronJuggernaut:
                return ironJuggernautPrefab;
            default:
                Debug.LogWarning($"Unknown character type: {character}, using default");
                return bloodKnightPrefab;
        }
    }

    // เพิ่ม callback สำหรับเมื่อผู้เล่นออกจากเกม
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[PlayerSpawner] Player {player} left the game");

        if (spawnedCharacters.ContainsKey(player))
        {
            NetworkObject playerObj = spawnedCharacters[player];
            if (playerObj != null)
            {
                Hero hero = playerObj.GetComponent<Hero>();
                if (hero != null)
                {
                    // 🆕 ลบ hero stats tracking
                    if (heroStatsReady.ContainsKey(hero))
                    {
                        heroStatsReady.Remove(hero);
                    }

                    // ลบ WorldUI
                    if (heroWorldUIs.ContainsKey(hero))
                    {
                        Destroy(heroWorldUIs[hero]);
                        heroWorldUIs.Remove(hero);
                    }
                }
            }
            spawnedCharacters.Remove(player);
        }

        // ลบข้อมูลอื่นๆ
        if (playerCharacters.ContainsKey(player))
        {
            playerCharacters.Remove(player);
        }

        if (playerManagers.ContainsKey(player))
        {
            playerManagers.Remove(player);
        }
    }
    public void CleanupOnGameExit()
    {
        Debug.Log("[PlayerSpawner] Cleaning up PlayerSpawner data");

        // Clear ข้อมูลทั้งหมด
        playerCharacters.Clear();
        playerManagers.Clear();
        spawnedCharacters.Clear();
        spawnRequests.Clear();
        heroStatsReady.Clear(); // 🆕 เพิ่มบรรทัดนี้

        // Remove callbacks
        if (_runner != null)
        {
            _runner.RemoveCallbacks(this);
        }
    }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"NetworkRunner shutdown: {shutdownReason}");
        CleanupOnGameExit();
    }
    private Vector3 GetSafeSpawnPosition(PlayerRef player)
    {
        Debug.Log($"🎯 [SPAWN] Getting safe position for Player {player}...");

        // วิธีที่ 1: ใช้ spawn points ที่กำหนดไว้
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Debug.Log($"🎯 [SPAWN] Found {spawnPoints.Length} spawn points");

            // กรอง spawn points ที่ไม่เป็น null
            List<Transform> validSpawnPoints = new List<Transform>();
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    validSpawnPoints.Add(spawnPoints[i]);
                    Debug.Log($"🎯 [SPAWN] Valid spawn point {i}: {spawnPoints[i].name} at {spawnPoints[i].position}");
                }
                else
                {
                    Debug.LogWarning($"⚠️ [SPAWN] Spawn point {i} is NULL!");
                }
            }

            if (validSpawnPoints.Count > 0)
            {
                // เลือก spawn point ตาม player index
                int spawnIndex = player.PlayerId % validSpawnPoints.Count;
                Transform selectedSpawnPoint = validSpawnPoints[spawnIndex];

                Vector3 spawnPosition = selectedSpawnPoint.position;
                Debug.Log($"✅ [SPAWN] Using spawn point {spawnIndex}: {selectedSpawnPoint.name} at {spawnPosition}");
                return spawnPosition;
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ [SPAWN] No spawn points available! Array length: {(spawnPoints?.Length ?? 0)}");
        }

        // วิธีที่ 2: หาตำแหน่งที่ปลอดภัยด้วย Raycast
        Vector3 safePosition = FindSafeGroundPosition();

        if (safePosition != Vector3.zero)
        {
            Debug.Log($"✅ [SPAWN] Found safe ground position: {safePosition}");
            return safePosition;
        }

        // วิธีที่ 3: Fallback - ใช้ตำแหน่งเริ่มต้นที่ปลอดภัย
        Vector3 fallbackPosition = GetFallbackSpawnPosition(player);

        Debug.Log($"✅ [SPAWN] Using fallback position: {fallbackPosition}");
        return fallbackPosition;
    }

    // ✅ เพิ่ม method สำหรับสร้าง spawn points อัตโนมัติตอน runtime
    private void CreateDefaultSpawnPointsRuntime()
    {
        Debug.Log("🎯 [SPAWN] Creating default spawn points at runtime...");

        GameObject spawnParent = GameObject.Find("SpawnPoints");
        if (spawnParent == null)
        {
            spawnParent = new GameObject("SpawnPoints");
            Debug.Log("🎯 [SPAWN] Created SpawnPoints parent object");
        }

        Vector3[] defaultPositions = {
        new Vector3(0, spawnHeight, 0),      // กลาง
        new Vector3(3, spawnHeight, 0),      // ขวา
        new Vector3(-3, spawnHeight, 0),     // ซ้าย
        new Vector3(0, spawnHeight, 3),      // หน้า
        new Vector3(0, spawnHeight, -3),     // หลัง
        new Vector3(3, spawnHeight, 3),      // มุมขวาหน้า
        new Vector3(-3, spawnHeight, 3),     // มุมซ้ายหน้า
        new Vector3(3, spawnHeight, -3),     // มุมขวาหลัง
        new Vector3(-3, spawnHeight, -3)     // มุมซ้ายหลัง
    };

        spawnPoints = new Transform[defaultPositions.Length];

        for (int i = 0; i < defaultPositions.Length; i++)
        {
            GameObject spawnPoint = new GameObject($"SpawnPoint_{i}");
            spawnPoint.transform.parent = spawnParent.transform;
            spawnPoint.transform.position = defaultPositions[i];
            spawnPoints[i] = spawnPoint.transform;

            Debug.Log($"🎯 [SPAWN] Created spawn point {i} at {defaultPositions[i]}");
        }

        Debug.Log($"✅ [SPAWN] Created {defaultPositions.Length} runtime spawn points");
    }

    // ✅ ปรับปรุง fallback position ให้ดีกว่า
    private Vector3 GetFallbackSpawnPosition(PlayerRef player)
    {
        Debug.Log($"🎯 [SPAWN] Calculating fallback position for Player {player}...");

        // สร้างตำแหน่งแบบกระจาย 3x3 grid
        int playerIndex = player.PlayerId;
        int gridSize = 3;
        int x = playerIndex % gridSize;
        int z = (playerIndex / gridSize) % gridSize;

        Vector3 fallbackPosition = new Vector3(
            (x - 1) * 4f, // -4, 0, 4
            spawnHeight,
            (z - 1) * 4f  // -4, 0, 4
        );

        Debug.Log($"🎯 [SPAWN] Fallback position for Player {playerIndex}: Grid({x},{z}) = {fallbackPosition}");
        return fallbackPosition;
    }

    // ✅ ปรับปรุง FindSafeGroundPosition ให้มี debug
    private Vector3 FindSafeGroundPosition()
    {
        Debug.Log($"🎯 [SPAWN] Searching for safe ground position...");

        int maxAttempts = 10;

        for (int i = 0; i < maxAttempts; i++)
        {
            // สุ่มตำแหน่ง X, Z
            float x = UnityEngine.Random.Range(-8f, 8f);
            float z = UnityEngine.Random.Range(-8f, 8f);

            // เริ่มต้นจากจุดสูง
            Vector3 rayStart = new Vector3(x, 20f, z);

            Debug.Log($"🎯 [SPAWN] Attempt {i + 1}: Raycasting from {rayStart}...");

            // ยิง Raycast ลงไปหาพื้น
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 25f, groundLayerMask))
            {
                // พบพื้น - spawn สูงกว่าพื้นนิดหน่อย
                Vector3 groundPosition = hit.point;
                groundPosition.y += 1f; // เพิ่มความสูง 1 เมตร

                Debug.Log($"✅ [SPAWN] Found ground at attempt {i + 1}: {groundPosition}");
                return groundPosition;
            }
            else
            {
                Debug.Log($"❌ [SPAWN] Attempt {i + 1}: No ground found");
            }
        }

        // ไม่พบพื้นที่ปลอดภัย
        Debug.LogWarning("⚠️ [SPAWN] Could not find safe ground position after all attempts");
        return Vector3.zero;
    }

    // ✅ เพิ่ม method สำหรับ debug spawn points ใน Scene View
    private void OnDrawGizmos()
    {
        if (!showSpawnPointDebug) return;

        if (spawnPoints != null)
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    // วาด spawn point เป็นทรงกลมสีเขียว
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(spawnPoints[i].position, 0.5f);

                    // วาดเลขเพื่อบอก index
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawRay(spawnPoints[i].position, Vector3.up * 2f);

                    // วาดชื่อ (ถ้าต้องการ)
#if UNITY_EDITOR
                    UnityEditor.Handles.Label(spawnPoints[i].position + Vector3.up * 2.5f, $"Spawn {i}");
#endif
                }
            }
        }

        // วาดพื้นที่ที่ใช้ Raycast หาพื้น
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(16f, 0.1f, 16f));
    }

    // ✅ เพิ่ม Inspector Button สำหรับ debug
    [ContextMenu("🔍 Debug Spawn Points")]
    private void DebugSpawnPoints()
    {
        Debug.Log("=== SPAWN POINTS DEBUG ===");
        Debug.Log($"Array length: {(spawnPoints?.Length ?? 0)}");
        Debug.Log($"Auto create: {autoCreateSpawnPoints}");
        Debug.Log($"Spawn height: {spawnHeight}");

        if (spawnPoints != null)
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    Debug.Log($"Spawn Point {i}: {spawnPoints[i].name} at {spawnPoints[i].position}");
                }
                else
                {
                    Debug.LogWarning($"Spawn Point {i}: NULL");
                }
            }
        }

        Debug.Log("========================");
    }

    [ContextMenu("🎯 Test Spawn Positions")]
    private void TestSpawnPositions()
    {
        Debug.Log("=== TESTING SPAWN POSITIONS ===");

        for (int i = 0; i < 5; i++)
        {
            // สร้าง fake PlayerRef เพื่อทดสอบ
            var fakePlayer = PlayerRef.FromIndex(i);
            Vector3 testPosition = GetSafeSpawnPosition(fakePlayer);
            Debug.Log($"Player {i} would spawn at: {testPosition}");
        }

        Debug.Log("==============================");
    }

    // เมธอดที่จำเป็นต้องมีสำหรับ INetworkRunnerCallbacks
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken token) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}