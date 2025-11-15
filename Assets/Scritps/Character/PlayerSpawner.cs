using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using TMPro;
using System.Linq;

public class PlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    #region Character Prefabs - Generic System
    [Header("🎮 Character Prefabs - Array System")]
    [SerializeField] private CharacterPrefabMapping[] characterPrefabsArray = new CharacterPrefabMapping[4];


    [System.Serializable]
    public class CharacterPrefabMapping
    {
        public PlayerSelectionData.CharacterType characterType;
        public GameObject prefab;
    }

    // Dictionary for fast lookup
    private Dictionary<PlayerSelectionData.CharacterType, GameObject> prefabDictionary;
    #endregion

    #region Network & Manager References
    [Header("Network Manager Prefab")]
    public GameObject networkPlayerManagerPrefab;

    private NetworkRunner _runner;
    private HashSet<string> spawnRequests = new HashSet<string>();
    #endregion

    #region Spawn Settings
    [Header("Spawn Settings")]
    public Transform[] spawnPoints;
    public float spawnHeight = 2f;
    public LayerMask groundLayerMask = 1;
    [SerializeField] private bool autoCreateSpawnPoints = true;
    [SerializeField] private bool showSpawnPointDebug = true;
    #endregion

    #region UI References
    [Header("Combat UI")]
    public CombatUIManager combatUIManagerPrefab;

    [Header("World UI")]
    public GameObject worldSpaceUIPrefab;

    private Dictionary<Hero, GameObject> heroWorldUIs = new Dictionary<Hero, GameObject>();
    private Dictionary<Hero, bool> heroStatsReady = new Dictionary<Hero, bool>();
    #endregion

    #region Player Data Tracking
    private Dictionary<PlayerRef, PlayerSelectionData.CharacterType> playerCharacters = new Dictionary<PlayerRef, PlayerSelectionData.CharacterType>();
    private Dictionary<PlayerRef, NetworkPlayerManager> playerManagers = new Dictionary<PlayerRef, NetworkPlayerManager>();
    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    #endregion

    #region Initialization
    private void Awake()
    {
        InitializePrefabDictionary();

        if (autoCreateSpawnPoints && (spawnPoints == null || spawnPoints.Length == 0))
        {
            CreateDefaultSpawnPointsRuntime();
        }

    }

    private void OnEnable()
    {
        // NetworkRunner will be found in Update
    }

    private void Update()
    {
        if (_runner == null)
        {
            _runner = FindObjectOfType<NetworkRunner>();
            if (_runner != null)
            {
                _runner.AddCallbacks(this);
                Debug.Log($"[PlayerSpawner] ✅ Registered - IsServer: {_runner.IsServer}, Players: {_runner.ActivePlayers.Count()}"); // ✅ เพิ่ม debug
            }
        }
    }
    private void InitializePrefabDictionary()
    {
        prefabDictionary = new Dictionary<PlayerSelectionData.CharacterType, GameObject>();

        if (characterPrefabsArray == null || characterPrefabsArray.Length == 0)
        {
            Debug.LogError("[InitializePrefabDictionary] ❌ characterPrefabsArray is empty!");
            return;
        }

        Debug.Log($"[InitializePrefabDictionary] 🔍 Processing {characterPrefabsArray.Length} character mappings...");

        for (int i = 0; i < characterPrefabsArray.Length; i++)
        {
            var mapping = characterPrefabsArray[i];

            if (mapping.prefab == null)
            {
                Debug.LogWarning($"[InitializePrefabDictionary] ⚠️ Slot {i} ({mapping.characterType}): Prefab is NULL!");
                continue;
            }

            // ✅ ตรวจสอบ prefab components
            NetworkObject networkObj = mapping.prefab.GetComponent<NetworkObject>();
            Hero heroComp = mapping.prefab.GetComponent<Hero>();
            Character charComp = mapping.prefab.GetComponent<Character>();

            if (networkObj == null || heroComp == null || charComp == null)
            {
                Debug.LogError($"[InitializePrefabDictionary] ❌ Slot {i} ({mapping.characterType}): Missing required components!");
                Debug.LogError($"  - NetworkObject: {(networkObj != null ? "✅" : "❌")}");
                Debug.LogError($"  - Hero: {(heroComp != null ? "✅" : "❌")}");
                Debug.LogError($"  - Character: {(charComp != null ? "✅" : "❌")}");
                continue;
            }

            // ✅ ตรวจสอบ CharacterStats
            if (charComp.characterStats == null)
            {
                Debug.LogError($"[InitializePrefabDictionary] ❌ Slot {i} ({mapping.characterType}): characterStats is NULL!");
                continue;
            }

            // ✅ เพิ่มลง dictionary
            if (prefabDictionary.ContainsKey(mapping.characterType))
            {
                Debug.LogWarning($"[InitializePrefabDictionary] ⚠️ Duplicate key: {mapping.characterType} (overwriting)");
            }

            prefabDictionary[mapping.characterType] = mapping.prefab;
            Debug.Log($"[InitializePrefabDictionary] ✅ Slot {i}: {mapping.characterType} → {mapping.prefab.name}");
        }

        Debug.Log($"[InitializePrefabDictionary] 🎉 Dictionary initialized with {prefabDictionary.Count} characters");
    }
    /// <summary>
    /// ✅ สร้าง Dictionary จาก list เพื่อ fast lookup
    /// </summary>

    #endregion

    #region Player Join/Leave Callbacks
    // ✅ แทนที่ OnPlayerJoined() method ใน PlayerSpawner.cs

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[PlayerSpawner] ========================================");
        Debug.Log($"[PlayerSpawner] 🎮 Player joined:");
        Debug.Log($"  - Player: {player}");
        Debug.Log($"  - IsServer: {runner.IsServer}");
        Debug.Log($"  - IsClient: {runner.IsClient}");
        Debug.Log($"  - LocalPlayer: {runner.LocalPlayer}");
        Debug.Log($"  - GameMode: {runner.GameMode}");
        Debug.Log($"[PlayerSpawner] ========================================");

        _runner = runner;

        // ✅ เฉพาะ Server/Host เท่านั้นที่ spawn
        if (!runner.IsServer)
        {
            Debug.Log($"[PlayerSpawner] ⏭️ Client detected - waiting for server to spawn");
            return;
        }

        Debug.Log($"[PlayerSpawner] ✅ Server confirmed - processing spawn for {player}");

        // Single player mode
        if (runner.GameMode == GameMode.Single)
        {
            Debug.Log($"[PlayerSpawner] Single mode - forcing spawn for local player");

            string activeCharacter = PersistentPlayerData.Instance.GetCurrentActiveCharacter();
            PlayerSelectionData.CharacterType selectedCharacter;

            if (System.Enum.TryParse<PlayerSelectionData.CharacterType>(activeCharacter, out selectedCharacter))
            {
                Debug.Log($"[PlayerSpawner] Using character: {selectedCharacter}");
            }
            else
            {
                selectedCharacter = PlayerSelectionData.CharacterType.Assassin;
                Debug.LogWarning($"[PlayerSpawner] Failed to parse, using default: {selectedCharacter}");
            }

            SpawnCharacterForPlayer(runner.LocalPlayer, selectedCharacter);
            return;
        }

        // ✅ Party Mode - Host spawn ให้ทุกคน
        Debug.Log($"[PlayerSpawner] 🌐 Party Mode - processing Player {player}");

        CleanupPlayerData(player);
        SpawnPlayerManager(runner, player);

        // ✅ ตรวจสอบว่าเป็น Local Player หรือ Remote Player
        if (player == runner.LocalPlayer)
        {
            Debug.Log($"[PlayerSpawner] 🏠 This is LOCAL PLAYER (Host) - spawning immediately");

            // ✅ Host spawn ทันที (ไม่ต้องรอ RPC)
            string activeCharacter = PersistentPlayerData.Instance.GetCurrentActiveCharacter();
            PlayerSelectionData.CharacterType selectedCharacter;

            if (System.Enum.TryParse<PlayerSelectionData.CharacterType>(activeCharacter, out selectedCharacter))
            {
                Debug.Log($"[PlayerSpawner] Host character: {selectedCharacter}");
                SpawnCharacterForPlayer(player, selectedCharacter);
            }
            else
            {
                Debug.LogError($"[PlayerSpawner] Failed to parse host character!");
            }
        }
        else
        {
            Debug.Log($"[PlayerSpawner] 🌐 This is REMOTE PLAYER - waiting for RPC...");

            // ✅ Remote player รอ RPC (จะถูกเรียกจาก NetworkPlayerManager.RPC_RequestCharacterSpawn)
            // ไม่ต้องทำอะไรตรงนี้ - RPC จะเรียก SpawnCharacterForPlayer เอง
        }
    }

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
                    if (heroStatsReady.ContainsKey(hero))
                    {
                        heroStatsReady.Remove(hero);
                    }

                    if (heroWorldUIs.ContainsKey(hero))
                    {
                        Destroy(heroWorldUIs[hero]);
                        heroWorldUIs.Remove(hero);
                    }
                }
            }
            spawnedCharacters.Remove(player);
        }

        CleanupPlayerData(player);
    }

    private void CleanupPlayerData(PlayerRef player)
    {
        if (spawnedCharacters.ContainsKey(player))
        {
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
    }

    private void SpawnPlayerManager(NetworkRunner runner, PlayerRef player)
    {
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
                    Debug.Log($"[PlayerSpawner] NetworkPlayerManager spawned for {player}");
                }
            }
        }
    }
    #endregion

    #region Character Spawning
    // ✅ แทนที่ DelayedSpawn() method ใน PlayerSpawner.cs

    IEnumerator DelayedSpawn(PlayerRef player)
    {
        Debug.Log($"[DelayedSpawn] ========================================");
        Debug.Log($"[DelayedSpawn] START for Player {player}");
        Debug.Log($"[DelayedSpawn] ========================================");

        // ✅ ป้องกัน duplicate spawn
        if (spawnRequests.Contains(player.ToString()))
        {
            Debug.LogWarning($"[DelayedSpawn] ❌ Already spawning {player}");
            yield break;
        }

        spawnRequests.Add(player.ToString());

        // ✅ รอให้ NetworkPlayerManager พร้อม
        float timeout = 5f;
        float elapsed = 0f;

        NetworkPlayerManager playerManager = null;

        while (elapsed < timeout)
        {
            // ✅ หา NetworkPlayerManager ของ player นี้
            NetworkPlayerManager[] managers = FindObjectsOfType<NetworkPlayerManager>();

            foreach (var manager in managers)
            {
                if (manager.Object != null && manager.Object.InputAuthority == player)
                {
                    playerManager = manager;
                    Debug.Log($"[DelayedSpawn] ✅ Found NetworkPlayerManager for {player}");
                    break;
                }
            }

            if (playerManager != null)
            {
                break;
            }

            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;

            if (elapsed >= timeout)
            {
                Debug.LogError($"[DelayedSpawn] ⏰ TIMEOUT waiting for NetworkPlayerManager!");
                spawnRequests.Remove(player.ToString());
                yield break;
            }
        }

        // ✅ ดึง character type จาก NetworkPlayerManager
        PlayerSelectionData.CharacterType selectedCharacter;

        if (playerManager != null && playerManager.HasSpawnedCharacter)
        {
            // ✅ ใช้ character type ที่ถูก sync มาแล้ว
            selectedCharacter = (PlayerSelectionData.CharacterType)playerManager.SelectedCharacterType;
            Debug.Log($"[DelayedSpawn] ✅ Using synced character: {selectedCharacter} for Player {player}");
        }
        else
        {
            // ✅ Fallback: ถ้าเป็น local player ให้ใช้จาก PersistentPlayerData
            if (player == _runner.LocalPlayer)
            {
                Debug.Log($"[DelayedSpawn] Using local player character from PersistentPlayerData");

                string activeCharacter = PersistentPlayerData.Instance?.GetCurrentActiveCharacter() ?? "Assassin";

                if (!System.Enum.TryParse<PlayerSelectionData.CharacterType>(activeCharacter, out selectedCharacter))
                {
                    Debug.LogError($"[DelayedSpawn] ❌ Failed to parse '{activeCharacter}' - using Assassin");
                    selectedCharacter = PlayerSelectionData.CharacterType.Assassin;
                }
            }
            else
            {
                // ✅ Remote player แต่ยังไม่มี manager - ใช้ default
                Debug.LogWarning($"[DelayedSpawn] ⚠️ No character type found for {player} - using Assassin");
                selectedCharacter = PlayerSelectionData.CharacterType.Assassin;
            }
        }

        Debug.Log($"[DelayedSpawn] 🎯 Final character for Player {player}: {selectedCharacter}");

        // ✅ ตรวจสอบ prefab
        if (!IsPrefabAvailable(selectedCharacter))
        {
            Debug.LogError($"[DelayedSpawn] ❌ Prefab not available for {selectedCharacter}!");
            Debug.Log($"[DelayedSpawn] 🔄 Falling back to Assassin...");

            selectedCharacter = PlayerSelectionData.CharacterType.Assassin;

            if (!IsPrefabAvailable(selectedCharacter))
            {
                Debug.LogError($"[DelayedSpawn] ❌ CRITICAL: Even Assassin not available!");
                spawnRequests.Remove(player.ToString());
                yield break;
            }
        }

        Debug.Log($"[DelayedSpawn] 6️⃣ About to spawn: {selectedCharacter} for Player {player}");

        // ✅ Spawn character
        SpawnCharacterForPlayer(player, selectedCharacter);

        Debug.Log($"[DelayedSpawn] 🏁 END");

        spawnRequests.Remove(player.ToString());
    }


    public void SpawnCharacterForPlayer(PlayerRef player, PlayerSelectionData.CharacterType characterType)
    {
        NetworkRunner currentRunner = _runner;

        if (spawnedCharacters.ContainsKey(player))
        {
            Debug.LogWarning($"[PlayerSpawner] Player {player} already has a character spawned!");
            return;
        }

        if (currentRunner == null)
        {
            currentRunner = FindObjectOfType<NetworkRunner>();
            if (currentRunner == null)
            {
                Debug.LogError("[PlayerSpawner] NetworkRunner not found!");
                return;
            }
        }

        if (!currentRunner.IsServer)
        {
            return;
        }

        playerCharacters[player] = characterType;

        // ✅ Generic prefab lookup
        GameObject prefabToSpawn = GetPrefabForCharacter(characterType);

        if (prefabToSpawn == null)
        {
            Debug.LogError($"[PlayerSpawner] ❌ No prefab found for: {characterType}");
            return;
        }

        Debug.Log($"[PlayerSpawner] ✅ Spawning {characterType} using prefab: {prefabToSpawn.name}");

        // Validate prefab components
        if (!ValidatePrefab(prefabToSpawn, characterType))
        {
            return;
        }

        Vector3 spawnPosition = GetSafeSpawnPosition(player);

        try
        {
            NetworkObject playerObject = currentRunner.Spawn(
                prefabToSpawn,
                spawnPosition,
                Quaternion.identity,
                player,
                (runner, obj) =>
                {
                    if (player == runner.LocalPlayer)
                    {
                        Debug.Log($"[PlayerSpawner] ✅ Local player spawned as {characterType}");
                        obj.gameObject.tag = "LocalPlayer";
                    }
                    else
                    {
                        Debug.Log($"[PlayerSpawner] ✅ Remote player spawned as {characterType}");
                        obj.gameObject.tag = "RemotePlayer";
                    }
                }
            );

            if (playerObject != null)
            {
                spawnedCharacters[player] = playerObject;
                Debug.Log($"[PlayerSpawner] ✅ Player {player} spawned as {characterType} at {spawnPosition}");

                if (player == currentRunner.LocalPlayer)
                {
                    playerObject.gameObject.name = $"LocalPlayer_{characterType}";
                }
                else
                {
                    playerObject.gameObject.name = $"RemotePlayer_{player}_{characterType}";
                }

                SetupCombatUI(playerObject);
            }
            else
            {
                Debug.LogError($"[PlayerSpawner] ❌ Failed to spawn {characterType}! NetworkObject is null!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerSpawner] ❌ EXCEPTION while spawning {characterType}: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
        }
    }

    /// <summary>
    /// ✅ Generic prefab getter - no type-specific logic
    /// </summary>
    private GameObject GetPrefabForCharacter(PlayerSelectionData.CharacterType characterType)
    {
        Debug.Log($"[GetPrefabForCharacter] ========================================");
        Debug.Log($"[GetPrefabForCharacter] Looking for: {characterType} (int: {(int)characterType})");
        Debug.Log($"[GetPrefabForCharacter] Dictionary count: {prefabDictionary.Count}");

        // ✅ แสดง keys ทั้งหมด
        Debug.Log($"[GetPrefabForCharacter] Dictionary contents:");
        foreach (var kvp in prefabDictionary)
        {
            Debug.Log($"  - {kvp.Key} (int: {(int)kvp.Key}) → {(kvp.Value != null ? kvp.Value.name : "NULL")}");
        }

        // ✅ ค้นหา prefab
        if (prefabDictionary.TryGetValue(characterType, out GameObject prefab))
        {
            if (prefab == null)
            {
                Debug.LogError($"[GetPrefabForCharacter] ❌ Prefab for {characterType} is NULL in dictionary!");
                return null;
            }

            Debug.Log($"[GetPrefabForCharacter] ✅ Found prefab: {prefab.name}");

            // ✅ Validate components
            NetworkObject netObj = prefab.GetComponent<NetworkObject>();
            Hero heroComp = prefab.GetComponent<Hero>();
            Character charComp = prefab.GetComponent<Character>();

            Debug.Log($"[GetPrefabForCharacter] Component validation:");
            Debug.Log($"  - NetworkObject: {(netObj != null ? "✅" : "❌")}");
            Debug.Log($"  - Hero: {(heroComp != null ? $"✅ ({heroComp.GetType().Name})" : "❌")}");
            Debug.Log($"  - Character: {(charComp != null ? "✅" : "❌")}");

            if (charComp != null)
            {
                Debug.Log($"  - CharacterStats: {(charComp.characterStats != null ? "✅" : "❌")}");
            }

            Debug.Log($"[GetPrefabForCharacter] ========================================");

            return prefab;
        }

        Debug.LogError($"[GetPrefabForCharacter] ❌ Character type {characterType} NOT FOUND in dictionary!");
        Debug.Log($"[GetPrefabForCharacter] ========================================");

        return null;
    }

    /// <summary>
    /// ✅ Validate prefab before spawning - generic validation
    /// </summary>
    private bool ValidatePrefab(GameObject prefab, PlayerSelectionData.CharacterType characterType)
    {
        NetworkObject networkObj = prefab.GetComponent<NetworkObject>();
        Hero heroComponent = prefab.GetComponent<Hero>();

        if (networkObj == null)
        {
            Debug.LogError($"[PlayerSpawner] ❌ {prefab.name} missing NetworkObject component!");
            return false;
        }

        if (heroComponent == null)
        {
            Debug.LogError($"[PlayerSpawner] ❌ {prefab.name} missing Hero component!");
            return false;
        }

        Debug.Log($"[PlayerSpawner] ✅ {characterType} prefab validation passed");
        return true;
    }

    private bool IsPrefabAvailable(PlayerSelectionData.CharacterType characterType)
    {
        GameObject prefab = GetPrefabForCharacter(characterType);

        if (prefab == null)
        {
            Debug.LogError($"[PlayerSpawner] ❌ Prefab not found for {characterType}!");
            return false;
        }

        return true;
    }
    #endregion

    #region UI Setup
    private void SetupCombatUI(NetworkObject playerObject)
    {
        if (playerObject == null) return;

        Hero hero = playerObject.GetComponent<Hero>();
        if (hero == null) return;

        StartCoroutine(SetupCombatUIWithStatsWait(hero, playerObject));
    }

    private IEnumerator SetupCombatUIWithStatsWait(Hero hero, NetworkObject playerObject)
    {
        Debug.Log($"[PlayerSpawner] 🔄 Setting up UI for {hero.CharacterName}...");

        yield return new WaitForSeconds(0.5f);

        while (!hero.IsSpawned)
        {
            yield return null;
        }

        if (hero.HasInputAuthority)
        {
            yield return StartCoroutine(WaitForHeroStatsReady(hero));
        }

        if (hero.HasInputAuthority)
        {
            Debug.Log($"[PlayerSpawner] 🖥️ Setting up Combat UI for local player");

            CombatUIManager combatUI = FindObjectOfType<CombatUIManager>();

            if (combatUI == null && combatUIManagerPrefab != null)
            {
                combatUI = Instantiate(combatUIManagerPrefab);
                Debug.Log("[PlayerSpawner] Created new CombatUIManager");
            }

            if (combatUI != null)
            {
                yield return new WaitForEndOfFrame();
                combatUI.SetLocalHero(hero);
                Debug.Log($"[PlayerSpawner] ✅ Combat UI setup complete");
            }
        }

        OnHeroSpawnComplete(hero);
    }

    private IEnumerator WaitForHeroStatsReady(Hero hero)
    {
        Debug.Log($"[PlayerSpawner] ⏳ Waiting for {hero.CharacterName} stats...");

        Character character = hero.GetComponent<Character>();
        if (character == null)
        {
            Debug.LogError($"[PlayerSpawner] No Character component!");
            yield break;
        }

        yield return new WaitUntil(() => PersistentPlayerData.Instance != null);

        float dataWaitTime = 0f;
        while (!PersistentPlayerData.Instance.isDataLoaded && dataWaitTime < 45f)
        {
            yield return new WaitForSeconds(1f);
            dataWaitTime += 1f;

            if (Mathf.RoundToInt(dataWaitTime) % 5 == 0)
            {
                Debug.Log($"[PlayerSpawner] Still waiting for data... {dataWaitTime}s");
            }
        }

        bool hasValidData = PersistentPlayerData.Instance.HasValidData();
        Debug.Log($"[PlayerSpawner] Data loaded: {PersistentPlayerData.Instance.isDataLoaded}, Valid: {hasValidData}");

        if (!PersistentPlayerData.Instance.isDataLoaded)
        {
            Debug.LogError($"[PlayerSpawner] ❌ TIMEOUT after {dataWaitTime}s!");
            yield return StartCoroutine(TryLoadBackupData());
        }

        int maxWaitTime = 45;
        float waitTime = 0f;

        while (!character.IsStatsLoadingComplete() && waitTime < maxWaitTime)
        {
            yield return new WaitForSeconds(0.5f);
            waitTime += 0.5f;

            if (Mathf.RoundToInt(waitTime) % 5 == 0 && waitTime % 1f < 0.5f)
            {
                Debug.Log($"[PlayerSpawner] Still waiting for stats... ({waitTime:F1}s)");
            }
        }

        if (character.IsStatsLoadingComplete())
        {
            heroStatsReady[hero] = true;
            Debug.Log($"[PlayerSpawner] ✅ Stats ready! HP={character.MaxHp}, ATK={character.AttackDamage}");
        }
        else
        {
            Debug.LogWarning($"[PlayerSpawner] ⚠️ Timeout after {maxWaitTime}s");
            heroStatsReady[hero] = false;
            yield return StartCoroutine(RetryLoadCharacterData(character));
        }
    }

    private IEnumerator TryLoadBackupData()
    {
        Debug.Log("[PlayerSpawner] 🔄 Trying to load backup data...");

        bool backupLoaded = false;

        var loadBackupMethod = typeof(PersistentPlayerData).GetMethod(
            "LoadFromPlayerPrefsBackup",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        try
        {
            if (loadBackupMethod != null)
            {
                backupLoaded = (bool)loadBackupMethod.Invoke(PersistentPlayerData.Instance, null);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerSpawner] Error loading backup: {e.Message}");
        }

        if (backupLoaded)
        {
            Debug.Log("[PlayerSpawner] ✅ Backup data loaded");
            yield return new WaitForSeconds(1f);
        }
        else
        {
            Debug.LogWarning("[PlayerSpawner] ❌ No backup data available");
        }
    }

    private IEnumerator RetryLoadCharacterData(Character character)
    {
        Debug.Log("[PlayerSpawner] 🔄 Retrying character data load...");

        yield return new WaitForSeconds(2f);

        try
        {
            if (PersistentPlayerData.Instance?.HasValidData() == true)
            {
                var levelManager = character.GetComponent<LevelManager>();
                if (levelManager != null)
                {
                    levelManager.RefreshCharacterData();
                }

                PersistentPlayerData.Instance.LoadInventoryData(character);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerSpawner] Retry failed: {e.Message}");
        }

        yield return new WaitForSeconds(2f);

        if (character.MaxHp > 0 && character.AttackDamage > 0)
        {
            heroStatsReady[character.GetComponent<Hero>()] = true;
            Debug.Log("[PlayerSpawner] ✅ Retry successful!");
        }
    }

    public void OnHeroSpawnComplete(Hero hero)
    {
        Debug.Log($"[PlayerSpawner] 🎉 Hero spawn complete: {hero.CharacterName}");

        bool statsReady = heroStatsReady.ContainsKey(hero) ? heroStatsReady[hero] : false;
        Debug.Log($"[PlayerSpawner] Stats ready: {statsReady}");

        if (hero.HasInputAuthority && statsReady)
        {
            StartCoroutine(EnsureUISetup(hero));
        }

        StartCoroutine(DelayedWorldUISetup(hero));

        Character character = hero.GetComponent<Character>();
        if (character != null)
        {
            Debug.Log($"[PlayerSpawner] 📊 Final stats: HP={character.MaxHp}, ATK={character.AttackDamage}");
        }
    }

    private IEnumerator DelayedWorldUISetup(Hero hero)
    {
        while (!hero.IsNetworkStateReady)
        {
            yield return new WaitForSeconds(0.1f);
        }

        Character character = hero.GetComponent<Character>();
        if (character != null)
        {
            yield return new WaitUntil(() => character.IsStatsLoadingComplete());
        }

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
            Debug.Log("[PlayerSpawner] UI setup completed");
        }
    }

    public void CreateWorldSpaceUIForHero(Hero hero)
    {
        if (hero == null || worldSpaceUIPrefab == null) return;

        if (heroWorldUIs.ContainsKey(hero)) return;

        Debug.Log($"[PlayerSpawner] Creating WorldSpaceUI for {hero.CharacterName}");

        GameObject worldUI = Instantiate(worldSpaceUIPrefab);
        WorldSpaceUI worldSpaceUI = worldUI.GetComponent<WorldSpaceUI>();

        if (worldSpaceUI != null)
        {
            worldSpaceUI.Initialize(hero);
            heroWorldUIs[hero] = worldUI;
            Debug.Log($"[PlayerSpawner] WorldSpaceUI created");
        }
    }
    #endregion

    #region Spawn Position Management
    private Vector3 GetSafeSpawnPosition(PlayerRef player)
    {
        Debug.Log($"[PlayerSpawner] 🎯 Getting spawn position for Player {player}");

        // Method 1: Use spawn points
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            List<Transform> validSpawnPoints = new List<Transform>();
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    validSpawnPoints.Add(spawnPoints[i]);
                }
            }

            if (validSpawnPoints.Count > 0)
            {
                int spawnIndex = player.PlayerId % validSpawnPoints.Count;
                Transform selectedSpawnPoint = validSpawnPoints[spawnIndex];
                Vector3 spawnPosition = selectedSpawnPoint.position;
                Debug.Log($"[PlayerSpawner] ✅ Using spawn point {spawnIndex} at {spawnPosition}");
                return spawnPosition;
            }
        }

        // Method 2: Find safe ground with Raycast
        Vector3 safePosition = FindSafeGroundPosition();
        if (safePosition != Vector3.zero)
        {
            Debug.Log($"[PlayerSpawner] ✅ Found safe ground: {safePosition}");
            return safePosition;
        }

        // Method 3: Fallback position
        Vector3 fallbackPosition = GetFallbackSpawnPosition(player);
        Debug.Log($"[PlayerSpawner] ✅ Using fallback: {fallbackPosition}");
        return fallbackPosition;
    }

    private void CreateDefaultSpawnPointsRuntime()
    {
        Debug.Log("[PlayerSpawner] 🎯 Creating default spawn points...");

        GameObject spawnParent = GameObject.Find("SpawnPoints");
        if (spawnParent == null)
        {
            spawnParent = new GameObject("SpawnPoints");
        }

        Vector3[] defaultPositions = {
            new Vector3(0, spawnHeight, 0),
            new Vector3(3, spawnHeight, 0),
            new Vector3(-3, spawnHeight, 0),
            new Vector3(0, spawnHeight, 3),
            new Vector3(0, spawnHeight, -3),
            new Vector3(3, spawnHeight, 3),
            new Vector3(-3, spawnHeight, 3),
            new Vector3(3, spawnHeight, -3),
            new Vector3(-3, spawnHeight, -3)
        };

        spawnPoints = new Transform[defaultPositions.Length];

        for (int i = 0; i < defaultPositions.Length; i++)
        {
            GameObject spawnPoint = new GameObject($"SpawnPoint_{i}");
            spawnPoint.transform.parent = spawnParent.transform;
            spawnPoint.transform.position = defaultPositions[i];
            spawnPoints[i] = spawnPoint.transform;
        }

        Debug.Log($"[PlayerSpawner] ✅ Created {defaultPositions.Length} spawn points");
    }

    private Vector3 GetFallbackSpawnPosition(PlayerRef player)
    {
        int playerIndex = player.PlayerId;
        int gridSize = 3;
        int x = playerIndex % gridSize;
        int z = (playerIndex / gridSize) % gridSize;

        Vector3 fallbackPosition = new Vector3(
            (x - 1) * 4f,
            spawnHeight,
            (z - 1) * 4f
        );

        return fallbackPosition;
    }

    private Vector3 FindSafeGroundPosition()
    {
        int maxAttempts = 10;

        for (int i = 0; i < maxAttempts; i++)
        {
            float x = UnityEngine.Random.Range(-8f, 8f);
            float z = UnityEngine.Random.Range(-8f, 8f);
            Vector3 rayStart = new Vector3(x, 20f, z);

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 25f, groundLayerMask))
            {
                Vector3 groundPosition = hit.point;
                groundPosition.y += 1f;
                return groundPosition;
            }
        }

        return Vector3.zero;
    }
    #endregion

    #region Cleanup
    public void CleanupOnGameExit()
    {
        Debug.Log("[PlayerSpawner] Cleaning up...");

        playerCharacters.Clear();
        playerManagers.Clear();
        spawnedCharacters.Clear();
        spawnRequests.Clear();
        heroStatsReady.Clear();

        if (_runner != null)
        {
            _runner.RemoveCallbacks(this);
        }
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[PlayerSpawner] Shutdown: {shutdownReason}");
        CleanupOnGameExit();
    }
    #endregion

    #region Debug Visualization
    private void OnDrawGizmos()
    {
        if (!showSpawnPointDebug) return;

        if (spawnPoints != null)
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(spawnPoints[i].position, 0.5f);

                    Gizmos.color = Color.yellow;
                    Gizmos.DrawRay(spawnPoints[i].position, Vector3.up * 2f);

#if UNITY_EDITOR
                    UnityEditor.Handles.Label(spawnPoints[i].position + Vector3.up * 2.5f, $"Spawn {i}");
#endif
                }
            }
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(16f, 0.1f, 16f));
    }
    #endregion


    #region INetworkRunnerCallbacks - Required Methods
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
    #endregion
}