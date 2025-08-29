using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;
using System.Reflection;

public class GachaSystem : MonoBehaviour
{
    [Header("System Settings")]
    public bool enableDebugLog = true;
    public bool autoAddRewardsToInventory = true;
    public float playerSearchRetryDelay = 0.5f;
    public int maxPlayerSearchRetries = 10;

    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string playerName = "Player";
    [SerializeField] private Character manualPlayerReference;

    [Header("Gacha Machines")]
    [SerializeField] private List<GachaMachine> gachaMachines = new List<GachaMachine>();

    [Header("UI References")]
    public GachaUIManager uiManager;

    // Cache สำหรับ player character
    private Character cachedPlayerCharacter;
    private Inventory cachedInventory;
    private CharacterCollection cachedCharacterCollection;
    private bool isSearchingForPlayer = false;

    #region Singleton
    private static GachaSystem _instance;
    public static GachaSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GachaSystem>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GachaSystem");
                    _instance = go.AddComponent<GachaSystem>();
                }
            }
            return _instance;
        }
    }
    #endregion

    #region Events
    public static event Action<List<GachaReward>> OnRewardsAddedToInventory;
    public static event Action<GachaReward> OnInventoryFull;
    public static event Action<GachaMachine, List<GachaReward>> OnGachaCompleted;
    public static event Action<Character> OnPlayerCharacterFound;
    public static event Action OnPlayerCharacterLost;
    #endregion

    #region Properties
    public List<GachaMachine> AllMachines => gachaMachines;
    public int MachineCount => gachaMachines.Count;
    public Character PlayerCharacter => cachedPlayerCharacter;
    public CharacterCollection PlayerCharacterCollection => cachedCharacterCollection;
    public bool HasValidPlayer => cachedPlayerCharacter != null && cachedInventory != null;
    public bool HasCharacterCollection => cachedCharacterCollection != null;
    #endregion

    #region Initialization
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InitializeSystem();
    }

    private void InitializeSystem()
    {
        // ค้นหา gacha machines ใน scene
        RefreshMachineList();

        // Subscribe to events
        GachaMachine.OnGachaRolled += HandleGachaRolled;
        GachaMachine.OnRareItemObtained += HandleRareItemObtained;
        GachaMachine.OnGachaError += HandleGachaError;

        // เริ่มค้นหา player character
        StartCoroutine(FindPlayerWithRetry());

        Debug.Log($" GachaSystem initialized with {gachaMachines.Count} machines");
    }

    void OnDestroy()
    {
        GachaMachine.OnGachaRolled -= HandleGachaRolled;
        GachaMachine.OnRareItemObtained -= HandleRareItemObtained;
        GachaMachine.OnGachaError -= HandleGachaError;
    }

    void Start()
    {
        // หาก player ยังไม่เจอใน Awake ให้ลองอีกครั้งใน Start
        if (!HasValidPlayer)
        {
            StartCoroutine(FindPlayerWithRetry());
        }
    }
    #endregion

    #region Player Character Management
    /// <summary>
    /// ค้นหา player character และ inventory ด้วยหลายวิธี
    /// </summary>
    private IEnumerator FindPlayerWithRetry()
    {
        if (isSearchingForPlayer) yield break;
        isSearchingForPlayer = true;

        int retryCount = 0;

        while (retryCount < maxPlayerSearchRetries && !HasValidPlayer)
        {
            Character foundPlayer = FindPlayerCharacter();

            if (foundPlayer != null)
            {
                SetPlayerCharacter(foundPlayer);
                break;
            }

            retryCount++;

            if (enableDebugLog)
            {
                Debug.Log($" Searching for player character... Attempt {retryCount}/{maxPlayerSearchRetries}");
            }

            yield return new WaitForSeconds(playerSearchRetryDelay);
        }

        if (!HasValidPlayer)
        {
            Debug.LogWarning(" Could not find player character after all retries. Gacha rewards will be queued.");
        }

        isSearchingForPlayer = false;
    }

    private Character FindPlayerCharacter()
    {
        // วิธีที่ 1: ใช้ manual reference ถ้ามี
        if (manualPlayerReference != null)
        {
            if (enableDebugLog) Debug.Log(" Found player via manual reference");
            return manualPlayerReference;
        }

        // วิธีที่ 2: หาจาก PlayerManager หรือ GameManager (ถ้ามี)
        Character playerFromManager = FindPlayerFromManager();
        if (playerFromManager != null)
        {
            if (enableDebugLog) Debug.Log(" Found player via manager");
            return playerFromManager;
        }

        // วิธีที่ 3: หาจาก Tag
        GameObject playerByTag = GameObject.FindGameObjectWithTag(playerTag);
        if (playerByTag != null)
        {
            Character character = playerByTag.GetComponent<Character>();
            if (character != null)
            {
                if (enableDebugLog) Debug.Log(" Found player via tag");
                return character;
            }
        }

        // วิธีที่ 4: หาจาก Name
        GameObject playerByName = GameObject.Find(playerName);
        if (playerByName != null)
        {
            Character character = playerByName.GetComponent<Character>();
            if (character != null)
            {
                if (enableDebugLog) Debug.Log(" Found player via name");
                return character;
            }
        }

        // วิธีที่ 5: หาจาก Network Authority (สำหรับ multiplayer)
        Character[] characters = FindObjectsOfType<Character>();
        foreach (Character character in characters)
        {
            if (character.HasStateAuthority)
            {
                if (enableDebugLog) Debug.Log(" Found player via network authority");
                return character;
            }
        }

        // วิธีที่ 6: หาจาก Local Player flag (ถ้ามี)
        foreach (Character character in characters)
        {
            // สมมติว่ามี property IsLocalPlayer
            if (HasLocalPlayerFlag(character))
            {
                if (enableDebugLog) Debug.Log(" Found player via local player flag");
                return character;
            }
        }

        // วิธีที่ 7: Fallback - เอาตัวแรกที่มี Inventory
        foreach (Character character in characters)
        {
            if (character.GetComponent<Inventory>() != null)
            {
                if (enableDebugLog) Debug.Log(" Found player via inventory fallback");
                return character;
            }
        }

        return null;
    }

    private Character FindPlayerFromManager()
    {
        // ลองหาจาก PlayerManager, GameManager, หรือ NetworkManager ด้วย reflection

        // หา PlayerManager
        MonoBehaviour[] allComponents = FindObjectsOfType<MonoBehaviour>();

        foreach (var component in allComponents)
        {
            var type = component.GetType();

            // ตรวจสอบ PlayerManager
            if (type.Name == "PlayerManager")
            {
                Character player = GetPlayerFromComponent(component, new string[] { "LocalPlayer", "Player", "CurrentPlayer" });
                if (player != null) return player;
            }

            // ตรวจสอบ GameManager
            if (type.Name == "GameManager")
            {
                Character player = GetPlayerFromComponent(component, new string[] { "Player", "LocalPlayer", "MainPlayer" });
                if (player != null) return player;
            }

            // ตรวจสอบ NetworkManager
            if (type.Name == "NetworkManager" || type.Name.Contains("Network"))
            {
                Character player = GetPlayerFromComponent(component, new string[] { "LocalPlayer", "Player" });
                if (player != null) return player;
            }
        }

        return null;
    }

    private Character GetPlayerFromComponent(MonoBehaviour component, string[] propertyNames)
    {
        var type = component.GetType();

        foreach (string propertyName in propertyNames)
        {
            try
            {
                // ลอง property ก่อน
                var property = type.GetProperty(propertyName);
                if (property != null)
                {
                    var value = property.GetValue(component);
                    if (value is Character character)
                        return character;
                    if (value is GameObject go && go != null)
                    {
                        var charComponent = go.GetComponent<Character>();
                        if (charComponent != null) return charComponent;
                    }
                }

                // ลอง field
                var field = type.GetField(propertyName);
                if (field != null)
                {
                    var value = field.GetValue(component);
                    if (value is Character character)
                        return character;
                    if (value is GameObject go && go != null)
                    {
                        var charComponent = go.GetComponent<Character>();
                        if (charComponent != null) return charComponent;
                    }
                }
            }
            catch
            {
                // Continue to next property/field
            }
        }

        return null;
    }

    private bool HasLocalPlayerFlag(Character character)
    {
        // ตรวจสอบว่ามี property หรือ field ที่บอกว่าเป็น local player
        var type = character.GetType();

        try
        {
            // ตรวจสอบ field
            var isLocalField = type.GetField("isLocalPlayer");
            if (isLocalField != null && isLocalField.FieldType == typeof(bool))
            {
                return (bool)isLocalField.GetValue(character);
            }

            // ตรวจสอบ property
            var isLocalProperty = type.GetProperty("IsLocalPlayer");
            if (isLocalProperty != null && isLocalProperty.PropertyType == typeof(bool))
            {
                return (bool)isLocalProperty.GetValue(character);
            }

            // ตรวจสอบ property อื่นๆ ที่อาจจะมี
            string[] possibleNames = { "isLocal", "IsLocal", "isPlayer", "IsPlayer", "isOwner", "IsOwner" };

            foreach (string name in possibleNames)
            {
                var field = type.GetField(name);
                if (field != null && field.FieldType == typeof(bool))
                {
                    return (bool)field.GetValue(character);
                }

                var property = type.GetProperty(name);
                if (property != null && property.PropertyType == typeof(bool))
                {
                    return (bool)property.GetValue(character);
                }
            }
        }
        catch (Exception ex)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($" Error checking local player flag: {ex.Message}");
            }
        }

        return false;
    }

    private void SetPlayerCharacter(Character player)
    {
        cachedPlayerCharacter = player;
        cachedInventory = player.GetComponent<Inventory>();
        cachedCharacterCollection = player.GetComponent<CharacterCollection>();

        if (cachedInventory == null)
        {
            Debug.LogError($" Player character '{player.name}' does not have an Inventory component!");
            cachedPlayerCharacter = null;
            return;
        }

        // CharacterCollection is optional but recommended for character gacha
        if (cachedCharacterCollection == null)
        {
            Debug.LogWarning($" Player character '{player.name}' does not have a CharacterCollection component. Character gacha will not work properly.");
        }

        if (enableDebugLog)
        {
            Debug.Log($" Player character set: {player.name} (Inventory: /, CharacterCollection: {(cachedCharacterCollection != null ? "/" : "x")})");
        }

        OnPlayerCharacterFound?.Invoke(player);
    }

    /// <summary>
    /// Manual method สำหรับ set player character จากภายนอก
    /// </summary>
    public void SetPlayerCharacterManually(Character player)
    {
        if (player == null)
        {
            Debug.LogError(" Cannot set null player character");
            return;
        }

        SetPlayerCharacter(player);
    }

    /// <summary>
    /// เรียกเมื่อ player character หายไป (เช่น scene change)
    /// </summary>
    public void ClearPlayerCharacter()
    {
        if (cachedPlayerCharacter != null)
        {
            OnPlayerCharacterLost?.Invoke();
        }

        cachedPlayerCharacter = null;
        cachedInventory = null;
        cachedCharacterCollection = null;

        if (enableDebugLog)
        {
            Debug.Log(" Player character cleared");
        }
    }

    /// <summary>
    /// Refresh การหา player character
    /// </summary>
    public void RefreshPlayerCharacter()
    {
        ClearPlayerCharacter();
        StartCoroutine(FindPlayerWithRetry());
    }
    #endregion

    #region Machine Management
    public void RefreshMachineList()
    {
        gachaMachines.Clear();
        gachaMachines.AddRange(FindObjectsOfType<GachaMachine>());

        if (enableDebugLog)
        {
            Debug.Log($" Refreshed gacha machines: {gachaMachines.Count} found");
        }
    }

    public GachaMachine GetMachine(string machineId)
    {
        return gachaMachines.FirstOrDefault(m => m.machineId == machineId);
    }

    public GachaMachine GetMachineByName(string machineName)
    {
        return gachaMachines.FirstOrDefault(m => m.machineName == machineName);
    }

    public List<GachaMachine> GetMachinesByPool(GachaPoolData pool)
    {
        return gachaMachines.Where(m => m.Pool == pool).ToList();
    }

    public List<GachaMachine> GetCharacterMachines()
    {
        return gachaMachines.Where(m => m.isCharacterMachine).ToList();
    }

    public List<GachaMachine> GetItemMachines()
    {
        return gachaMachines.Where(m => !m.isCharacterMachine).ToList();
    }
    #endregion

    #region Gacha Operations
    public List<GachaReward> RollGacha(string machineId, int rollCount = 1)
    {
        GachaMachine machine = GetMachine(machineId);
        if (machine == null)
        {
            Debug.LogError($" Machine '{machineId}' not found!");
            return new List<GachaReward>();
        }

        return machine.Roll(rollCount);
    }

    public List<GachaReward> RollSingle(string machineId)
    {
        return RollGacha(machineId, 1);
    }

    public List<GachaReward> RollTen(string machineId)
    {
        return RollGacha(machineId, 10);
    }
    #endregion

    #region Event Handlers
    private void HandleGachaRolled(GachaMachine machine, List<GachaReward> rewards)
    {
        if (enableDebugLog)
        {
            Debug.Log($" Gacha rolled on '{machine.machineName}': {rewards.Count} rewards");
        }

        // เพิ่ม rewards เข้า inventory/collection
        if (autoAddRewardsToInventory)
        {
            StartCoroutine(AddRewardsToInventory(rewards));
        }

        // แจ้ง UI
        if (uiManager != null)
        {
            uiManager.ShowGachaResults(machine, rewards);
        }

        OnGachaCompleted?.Invoke(machine, rewards);
    }

    private void HandleRareItemObtained(GachaMachine machine, GachaReward reward)
    {
        if (enableDebugLog)
        {
            string type = reward.rewardType == RewardType.Character ? "character" : "item";
            Debug.Log($" Rare {type} obtained: {reward.GetRewardText()} from '{machine.machineName}'");
        }

        // แจ้ง UI แสดง rare item effect
        if (uiManager != null)
        {
            uiManager.ShowRareItemEffect(reward);
        }
    }

    private void HandleGachaError(GachaMachine machine, string error)
    {
        Debug.LogError($" Gacha error on '{machine.machineName}': {error}");

        if (uiManager != null)
        {
            uiManager.ShowErrorMessage(error);
        }
    }
    #endregion

    #region Inventory Integration
    private Queue<List<GachaReward>> pendingRewards = new Queue<List<GachaReward>>();

    private IEnumerator AddRewardsToInventory(List<GachaReward> rewards)
    {
        // ถ้ายังไม่มี player ให้เก็บไว้ใน queue
        if (!HasValidPlayer)
        {
            pendingRewards.Enqueue(rewards);

            if (enableDebugLog)
            {
                Debug.Log($" Queued {rewards.Count} rewards (no player found yet)");
            }

            // ลองหา player อีกครั้ง
            if (!isSearchingForPlayer)
            {
                StartCoroutine(FindPlayerWithRetry());
            }

            // รอให้หา player เจอ หรือ timeout
            yield return StartCoroutine(WaitForPlayerWithTimeout(5f));
        }

        // ถ้ายังไม่เจอ player ให้เก็บไว้ใน queue และ return
        if (!HasValidPlayer)
        {
            if (!pendingRewards.Contains(rewards))
            {
                pendingRewards.Enqueue(rewards);
            }

            Debug.LogWarning(" Cannot add rewards to inventory: Player not found. Rewards queued.");
            yield break;
        }

        // Process pending rewards ก่อน
        ProcessPendingRewards();

        // Process current rewards
        yield return ProcessRewardList(rewards);
    }

    private IEnumerator WaitForPlayerWithTimeout(float timeout)
    {
        float timer = 0f;
        while (timer < timeout && !HasValidPlayer)
        {
            timer += Time.deltaTime;
            yield return null;
        }
    }

    private void ProcessPendingRewards()
    {
        while (pendingRewards.Count > 0 && HasValidPlayer)
        {
            var rewards = pendingRewards.Dequeue();
            StartCoroutine(ProcessRewardList(rewards));

            if (enableDebugLog)
            {
                Debug.Log($" Processing queued rewards: {rewards.Count} items");
            }
        }
    }

    private IEnumerator ProcessRewardList(List<GachaReward> rewards)
    {
        if (!HasValidPlayer) yield break;

        List<GachaReward> addedRewards = new List<GachaReward>();
        List<GachaReward> failedRewards = new List<GachaReward>();

        foreach (GachaReward reward in rewards)
        {
            if (reward == null || !reward.IsValid()) continue;

            bool success = false;

            switch (reward.rewardType)
            {
                case RewardType.Item:
                    success = ProcessItemReward(reward);
                    break;
                case RewardType.Character:
                    success = ProcessCharacterReward(reward);
                    break;
                case RewardType.Currency:
                    success = ProcessCurrencyReward(reward);
                    break;
            }

            if (success)
            {
                addedRewards.Add(reward);
                if (enableDebugLog)
                {
                    Debug.Log($" Added to player: {reward.GetRewardText()}");
                }
            }
            else
            {
                failedRewards.Add(reward);
                Debug.LogWarning($" Failed to add: {reward.GetRewardText()}");
                OnInventoryFull?.Invoke(reward);
            }

            yield return null; // รอ 1 frame ระหว่างการเพิ่ม item
        }

        if (addedRewards.Count > 0)
        {
            OnRewardsAddedToInventory?.Invoke(addedRewards);
        }

        if (failedRewards.Count > 0)
        {
            Debug.LogWarning($" {failedRewards.Count} rewards could not be processed");
        }
    }

    private bool ProcessItemReward(GachaReward reward)
    {
        // ตรวจสอบว่า inventory ยังใช้ได้
        if (cachedInventory == null)
        {
            Debug.LogError(" Inventory reference lost during processing");
            RefreshPlayerCharacter();
            return false;
        }

        // ลองเพิ่มเข้า inventory
        return cachedInventory.AddItem(reward.itemData, reward.quantity);
    }

    private bool ProcessCharacterReward(GachaReward reward)
    {
        // ตรวจสอบว่ามี CharacterCollection
        if (cachedCharacterCollection == null)
        {
            Debug.LogError(" CharacterCollection not found - cannot add character reward");
            return false;
        }

        // เพิ่มตัวละครเข้า collection
        return cachedCharacterCollection.AddCharacter(reward.characterData);
    }

    private bool ProcessCurrencyReward(GachaReward reward)
    {
        // หา CurrencyManager และเพิ่ม currency
        CurrencyManager currencyManager = CurrencyManager.FindCurrencyManager();
        if (currencyManager == null)
        {
            Debug.LogError(" CurrencyManager not found - cannot add currency reward");
            return false;
        }

        if (reward.currencyType == "Gold")
        {
            return currencyManager.AddGold(reward.quantity);
        }
        else if (reward.currencyType == "Gems")
        {
            return currencyManager.AddGems(reward.quantity);
        }
        else
        {
            Debug.LogWarning($" Unsupported currency type: {reward.currencyType}");
            return false;
        }
    }
    #endregion

    #region Scene Management
    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (enableDebugLog)
        {
            Debug.Log($" Scene loaded: {scene.name}");
        }

        // Clear cache และหา player ใหม่
        ClearPlayerCharacter();
        RefreshMachineList();

        // รอ 1 frame แล้วค้นหา player
        StartCoroutine(DelayedPlayerSearch());
    }

    private IEnumerator DelayedPlayerSearch()
    {
        yield return null; // รอ 1 frame
        yield return StartCoroutine(FindPlayerWithRetry());
    }
    #endregion

    #region Statistics & Analytics
    public Dictionary<string, int> GetMachineUsageStats()
    {
        // TODO: implement usage tracking
        return new Dictionary<string, int>();
    }

    public List<GachaReward> GetRecentRewards(int count = 10)
    {
        // TODO: implement recent rewards tracking
        return new List<GachaReward>();
    }

    public int GetPendingRewardsCount()
    {
        return pendingRewards.Sum(rewards => rewards.Count);
    }

    public Dictionary<string, object> GetSystemStats()
    {
        var stats = new Dictionary<string, object>
        {
            ["TotalMachines"] = MachineCount,
            ["CharacterMachines"] = GetCharacterMachines().Count,
            ["ItemMachines"] = GetItemMachines().Count,
            ["HasValidPlayer"] = HasValidPlayer,
            ["HasCharacterCollection"] = HasCharacterCollection,
            ["PendingRewards"] = GetPendingRewardsCount(),
            ["AutoAddRewards"] = autoAddRewardsToInventory,
            ["DebugLogging"] = enableDebugLog
        };

        return stats;
    }
    #endregion

    #region Debug & Testing
    [ContextMenu("Test All Machines")]
    public void TestAllMachines()
    {
        foreach (var machine in gachaMachines)
        {
            Debug.Log($" Testing machine: {machine.machineName} ({(machine.isCharacterMachine ? "Character" : "Item")})");
            machine.TestSingleRoll();
        }
    }

    [ContextMenu("Debug System Info")]
    public void DebugSystemInfo()
    {
        Debug.Log("=== GACHA SYSTEM INFO ===");
        Debug.Log($"Machines: {gachaMachines.Count} (Character: {GetCharacterMachines().Count}, Item: {GetItemMachines().Count})");
        Debug.Log($"Auto-add to inventory: {autoAddRewardsToInventory}");
        Debug.Log($"Debug logging: {enableDebugLog}");
        Debug.Log($"Has valid player: {HasValidPlayer}");
        Debug.Log($"Has character collection: {HasCharacterCollection}");
        Debug.Log($"Pending rewards: {GetPendingRewardsCount()}");

        if (HasValidPlayer)
        {
            Debug.Log($"Player: {cachedPlayerCharacter.name}");
            Debug.Log($"  - Inventory: {(cachedInventory != null ? "✓" : "✗")}");
            Debug.Log($"  - Character Collection: {(cachedCharacterCollection != null ? "✓" : "✗")}");
        }

        Debug.Log("=== MACHINES ===");
        foreach (var machine in gachaMachines.OrderBy(m => m.isCharacterMachine))
        {
            string type = machine.isCharacterMachine ? "Character" : "Item";
            string poolName = machine.isCharacterMachine ?
                machine.CharacterPool?.poolName ?? "None" :
                machine.Pool?.poolName ?? "None";

            Debug.Log($"  {machine.machineName} ({type}) - Pool: {poolName}");
        }
    }

    [ContextMenu("Force Find Player")]
    public void ForceRefreshPlayer()
    {
        RefreshPlayerCharacter();
    }

    [ContextMenu("Clear Pending Rewards")]
    public void ClearPendingRewards()
    {
        int count = GetPendingRewardsCount();
        pendingRewards.Clear();
        Debug.Log($" Cleared {count} pending rewards");
    }

    [ContextMenu("Test Character Gacha")]
    public void TestCharacterGacha()
    {
        var characterMachines = GetCharacterMachines();
        if (characterMachines.Count > 0)
        {
            var machine = characterMachines[0];
            Debug.Log($" Testing character gacha: {machine.machineName}");
            var rewards = machine.Roll(1);

            foreach (var reward in rewards)
            {
                Debug.Log($"  Result: {reward.GetFullDisplayText()}");
            }
        }
        else
        {
            Debug.LogWarning(" No character gacha machines found for testing");
        }
    }
    #endregion
}