using UnityEngine;
using Firebase.Auth;
using Firebase.Database;
using System.Collections;
using System.Collections.Generic;

public class PersistentPlayerData : MonoBehaviour
{
    #region Variables and Properties  ตัวแปร, Singleton pattern และ Properties
    [Header("Multi-Character Data")]
    public MultiCharacterPlayerData multiCharacterData;
    public bool isDataLoaded = false;

    // Singleton
    private static PersistentPlayerData _instance;
    public static PersistentPlayerData Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PersistentPlayerData>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("PersistentPlayerData");
                    _instance = go.AddComponent<PersistentPlayerData>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    public FirebaseAuth auth;
    private DatabaseReference databaseReference;
    #endregion

    #region Unity Lifecycle & Initialization Awake, Firebase initialization
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFirebase();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void InitializeFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
        databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
    }

    private bool IsFirebaseReady()
    {
        if (auth == null)
        {
            Debug.LogError("❌ Firebase Auth is null");
            return false;
        }

        if (databaseReference == null)
        {
            Debug.LogError("❌ Firebase Database Reference is null");
            return false;
        }

        Debug.Log("✅ Firebase is ready");
        return true;
    }
    #endregion
    [ContextMenu("🔍 Debug Character Stats")]
    private void DebugCharacterStats()
    {
        if (multiCharacterData?.characters == null) return;

        foreach (var character in multiCharacterData.characters)
        {
            Debug.Log($"=== {character.characterType} STATS ===");
            Debug.Log($"Level: {character.currentLevel}");
            Debug.Log($"Base HP: {character.baseMaxHp} | Total HP: {character.totalMaxHp}");
            Debug.Log($"Base ATK: {character.baseAttackDamage} | Total ATK: {character.totalAttackDamage}");
            Debug.Log($"Base ARM: {character.baseArmor} | Total ARM: {character.totalArmor}");
            Debug.Log("============================");
        }
    }
    #region Helper Methods & Getters ฟังก์ชันช่วยต่างๆ สำหรับดึงข้อมูล
    public CharacterProgressData GetCurrentCharacterData()
    {
        if (multiCharacterData == null) return null;
        return multiCharacterData.GetActiveCharacterData();
    }

    public string GetPlayerName()
    {
        return multiCharacterData?.playerName ?? "Player";
    }
    [System.Serializable]
    private class InventoryBackupData
    {
        public int totalItems = 0;
        public int currentSlots = 0;
        public System.DateTime timestamp;
        public List<string> itemNames = new List<string>();
    }
    public int GetCurrentLevel()
    {
        var characterData = GetCurrentCharacterData();
        return characterData?.currentLevel ?? 1;
    }

    public int GetCurrentExp()
    {
        var characterData = GetCurrentCharacterData();
        return characterData?.currentExp ?? 0;
    }

    public string GetCurrentActiveCharacter()
    {
        if (multiCharacterData == null) return "Assassin";
        return multiCharacterData.currentActiveCharacter;
    }
    public CharacterProgressData GetCharacterData(string characterType)
    {
        return multiCharacterData.characters.Find(c => c.characterType == characterType);
    }

    public List<CharacterProgressData> GetAllCharacterData()
    {
        if (multiCharacterData == null) return new List<CharacterProgressData>();
        return multiCharacterData.characters;
    }


    public bool HasValidData()
    {
        return isDataLoaded &&
               multiCharacterData != null &&
               !string.IsNullOrEmpty(multiCharacterData.playerName) &&
               GetCurrentCharacterData() != null;
    }
    #endregion

    #region Data Loading & Saving การโหลดและบันทึกข้อมูล
    public void LoadPlayerDataAsync()
    {
        if (isDataLoaded) return;
        StartCoroutine(LoadDataCoroutine());
    }

    public IEnumerator LoadDataCoroutine()
    {
        if (auth?.CurrentUser == null)
        {
            CreateDefaultMultiCharacterData();
            yield break;
        }

        Debug.Log("[PersistentPlayerData] Loading multi-character data from Firebase...");

        var task = databaseReference.Child("players").Child(auth.CurrentUser.UserId).GetValueAsync();

        float timeout = 3f;
        float elapsed = 0f;

        while (!task.IsCompleted && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (task.IsCompleted && task.Exception == null && task.Result.Exists)
        {
            string json = task.Result.GetRawJsonValue();
            bool loaded = false;

            try
            {
                multiCharacterData = JsonUtility.FromJson<MultiCharacterPlayerData>(json);
                if (multiCharacterData != null && multiCharacterData.IsValid())
                {
                    isDataLoaded = true;
                    loaded = true;
                    SaveToPlayerPrefs();
                    Debug.Log($"✅ Loaded multi-character data: {multiCharacterData.playerName}, Active: {multiCharacterData.currentActiveCharacter}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[PersistentPlayerData] Failed to parse data: {e.Message}");
            }

            if (!loaded)
            {
                CreateDefaultMultiCharacterData();
            }
            else
            {
                // 🆕 โหลดข้อมูลเงินหลังจากโหลดข้อมูลหลักเสร็จ
                LoadCurrencyData();
            }
        }
        else
        {
            Debug.Log("[PersistentPlayerData] No data found. Creating default data...");
            CreateDefaultMultiCharacterData();
        }

        RegisterPlayerInDirectory();
    }

    private void CreateDefaultMultiCharacterData()
    {
        multiCharacterData = new MultiCharacterPlayerData();
        multiCharacterData.playerName = PlayerPrefs.GetString("PlayerName", "Player");
        multiCharacterData.currentActiveCharacter = "Assassin";
        InitializeCurrencyForNewPlayer();
        isDataLoaded = true;
        SaveToPlayerPrefs();

        Debug.Log($"✅ Created default multi-character data with Assassin for {multiCharacterData.playerName}");
    }
    private void InitializeCurrencyForNewPlayer()
    {
        if (multiCharacterData != null)
        {
            multiCharacterData.sharedCurrency = new SharedCurrencyData();
            multiCharacterData.hasCurrencyData = true;
            multiCharacterData.currencyLastSaveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            Debug.Log("✅ Currency system initialized for new player");
        }
    }
    public void SavePlayerDataAsync()
    {
        if (multiCharacterData == null || auth?.CurrentUser == null)
        {
            Debug.LogWarning("[PersistentPlayerData] Cannot save - no data or not authenticated");
            return;
        }

        StartCoroutine(SaveDataCoroutine());
    }

    private IEnumerator SaveDataCoroutine()
    {
        if (multiCharacterData == null || auth?.CurrentUser == null) yield break;

        multiCharacterData.lastLoginDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        string json = JsonUtility.ToJson(multiCharacterData, true);
        var task = databaseReference.Child("players").Child(auth.CurrentUser.UserId).SetRawJsonValueAsync(json);

        yield return new WaitForSeconds(0.5f);

        SaveToPlayerPrefs();
        Debug.Log($"💾 Saved multi-character data for {multiCharacterData.playerName}");

        if (task.IsCompleted)
        {
            if (task.Exception != null)
            {
                Debug.LogError($"❌ Failed to save: {task.Exception.Message}");
            }
            else
            {
                Debug.Log($"✅ Successfully saved to Firebase");
            }
        }
    }

    private void SaveToPlayerPrefs()
    {
        if (multiCharacterData == null) return;

        var currentCharacter = GetCurrentCharacterData();
        if (currentCharacter == null) return;

        PlayerPrefs.SetString("PlayerName", multiCharacterData.playerName);
        PlayerPrefs.SetString("LastCharacterSelected", multiCharacterData.currentActiveCharacter);
        PlayerPrefs.SetInt("PlayerLevel", currentCharacter.currentLevel);
        PlayerPrefs.SetInt("PlayerExp", currentCharacter.currentExp);
        PlayerPrefs.SetInt("PlayerExpToNext", currentCharacter.expToNextLevel);
        PlayerPrefs.SetInt("PlayerMaxHp", currentCharacter.totalMaxHp);
        PlayerPrefs.SetInt("PlayerMaxMana", currentCharacter.totalMaxMana);
        PlayerPrefs.SetInt("PlayerAttackDamage", currentCharacter.totalAttackDamage);
        PlayerPrefs.SetInt("PlayerMagicDamage", currentCharacter.totalMagicDamage);
        PlayerPrefs.SetInt("PlayerArmor", currentCharacter.totalArmor);
        PlayerPrefs.SetFloat("PlayerCritChance", currentCharacter.totalCriticalChance);
        PlayerPrefs.SetFloat("PlayerCriticalDamageBonus", currentCharacter.totalCriticalDamageBonus);
        PlayerPrefs.SetFloat("PlayerMoveSpeed", currentCharacter.totalMoveSpeed);
        PlayerPrefs.SetFloat("PlayerHitRate", currentCharacter.totalHitRate);
        PlayerPrefs.SetFloat("PlayerEvasionRate", currentCharacter.totalEvasionRate);
        PlayerPrefs.SetFloat("PlayerAttackSpeed", currentCharacter.totalAttackSpeed);
        PlayerPrefs.SetFloat("PlayerReductionCoolDown", currentCharacter.totalReductionCoolDown);
        PlayerPrefs.Save();
    }

    public void ForceSave() => SavePlayerDataAsync();
    #endregion

    #region Character Management การจัดการตัวละคร
    public void SwitchCharacter(string characterType)
    {
        if (multiCharacterData == null)
        {
            Debug.LogError("[PersistentPlayerData] MultiCharacterData is null!");
            return;
        }

        multiCharacterData.SwitchActiveCharacter(characterType);
        SaveToPlayerPrefs();
        SavePlayerDataAsync();

        Debug.Log($"✅ Switched to character: {characterType}");
    }

    public void UpdateLevelAndStats(int level, int exp, int expToNext, int maxHp, int maxMana,
       int attackDamage, int magicDamage, int armor, float critChance, float critDamageBonus,
       float moveSpeed, float hitRate, float evasionRate, float attackSpeed, float reductionCoolDown)
    {
        if (multiCharacterData == null)
        {
            Debug.LogError("[PersistentPlayerData] MultiCharacterData is null!");
            return;
        }

        string characterType = multiCharacterData.currentActiveCharacter;
        CharacterProgressData character = GetOrCreateCharacterData(characterType);

        if (character != null)
        {
            // เก็บค่าเก่าเพื่อ debug
            int oldHp = character.totalMaxHp;
            int oldAtk = character.totalAttackDamage;
            int oldArm = character.totalArmor;

            character.currentLevel = level;
            character.currentExp = exp;
            character.expToNextLevel = expToNext;
            character.totalMaxHp = maxHp;
            character.totalMaxMana = maxMana;
            character.totalAttackDamage = attackDamage;
            character.totalMagicDamage = magicDamage;
            character.totalArmor = armor;
            character.totalCriticalChance = critChance;
            character.totalCriticalDamageBonus = critDamageBonus;
            character.totalMoveSpeed = moveSpeed;
            character.totalHitRate = hitRate;
            character.totalEvasionRate = evasionRate;
            character.totalAttackSpeed = attackSpeed;
            character.totalReductionCoolDown = reductionCoolDown;

            // Mark ว่ามี total stats
            character.hasTotalStats = true;
            character.statsLastUpdateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            Debug.Log($"[PersistentPlayerData] 💾 Updated total stats for {characterType}:");
            Debug.Log($"  HP: {oldHp} → {maxHp} (change: {maxHp - oldHp:+#;-#;0})");
            Debug.Log($"  ATK: {oldAtk} → {attackDamage} (change: {attackDamage - oldAtk:+#;-#;0})");
            Debug.Log($"  ARM: {oldArm} → {armor} (change: {armor - oldArm:+#;-#;0})");
            Debug.Log($"  Level: {level}, CRIT: {critChance:F1}%");

            // 🆕 บันทึกลง Firebase ทันที
            SavePlayerDataAsync();
        }
    }


    // Note: This method appears to be a duplicate with different parameters - consider removing or renaming
    internal void UpdateLevelAndStats(int currentLevel, int currentExp, int expToNextLevel, int maxHp, int maxMana,
        int attackDamage, int magicDamage, int armor, float criticalChance, float criticalmulti, float criticalMultiplier,
        float moveSpeed, float hitRate, float evasionRate, float attackSpeed, float reductionCoolDown)
    {
        throw new System.NotImplementedException();
    }
    #endregion

    #region Player Directory & Registration การลงทะเบียนผู้เล่น
    public void RegisterPlayerInDirectory()
    {
        if (auth?.CurrentUser == null || multiCharacterData == null) return;
        StartCoroutine(RegisterPlayerInDirectoryCoroutine());
    }

    private IEnumerator RegisterPlayerInDirectoryCoroutine()
    {
        Debug.Log($"📝 Registering {multiCharacterData.playerName} in player directory...");

        var task = databaseReference
            .Child("playerDirectory")
            .Child(multiCharacterData.playerName)
            .SetValueAsync(auth.CurrentUser.UserId);

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception == null)
        {
            Debug.Log($"✅ Player registered in directory successfully");
        }
        else
        {
            Debug.LogError($"❌ Failed to register in directory: {task.Exception.Message}");
        }
    }
    #endregion

    #region Friends System ระบบเพื่อนทั้งหมด
    public void SendFriendRequest(string targetPlayerName)
    {
        // ตรวจสอบการเชื่อมต่อ Firebase ก่อน
        if (!IsFirebaseReady())
        {
            Debug.LogError("❌ Firebase is not ready! Cannot send friend request.");
            return;
        }

        if (auth?.CurrentUser == null)
        {
            Debug.LogError("❌ User not authenticated! Cannot send friend request.");
            return;
        }

        if (multiCharacterData == null)
        {
            Debug.LogError("❌ Player data not loaded! Cannot send friend request.");
            return;
        }

        StartCoroutine(SendFriendRequestCoroutine(targetPlayerName));
    }

    private IEnumerator SendFriendRequestCoroutine(string targetPlayerName)
    {
        Debug.Log($"🔍 Starting friend request for: {targetPlayerName}");

        // วิธีง่ายที่สุด: อ่านข้อมูลทุกคนและหาชื่อ
        var task = databaseReference.Child("players").GetValueAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError($"❌ Firebase error: {task.Exception.Message}");
            yield break;
        }

        if (!task.Result.Exists)
        {
            Debug.Log("❌ No players found in Firebase!");
            yield break;
        }

        Debug.Log($"📊 Searching through {task.Result.ChildrenCount} players...");

        string targetUserId = null;
        foreach (var player in task.Result.Children)
        {
            var playerData = player;

            // ลองทุกวิธีที่เป็นไปได้
            string playerName = null;

            // วิธีที่ 1: มี field playerName
            if (playerData.HasChild("playerName"))
            {
                playerName = playerData.Child("playerName").Value?.ToString();
            }
            // วิธีที่ 2: เป็น string ธรรมดา
            else if (playerData.Value is string)
            {
                playerName = playerData.Value.ToString();
            }

            Debug.Log($"🔍 Player {player.Key}: '{playerName}'");

            if (playerName == targetPlayerName)
            {
                targetUserId = player.Key;
                Debug.Log($"✅ Found {targetPlayerName} with ID: {targetUserId}");
                break;
            }
        }

        if (string.IsNullOrEmpty(targetUserId))
        {
            Debug.Log($"❌ Player '{targetPlayerName}' not found!");

            // Debug: แสดงรายชื่อผู้เล่นทั้งหมด
            Debug.Log("📋 Available players:");
            foreach (var player in task.Result.Children)
            {
                var playerData = player;
                string playerName = "NO_NAME";

                if (playerData.HasChild("playerName"))
                    playerName = playerData.Child("playerName").Value?.ToString();

                Debug.Log($"   - {player.Key}: '{playerName}'");
            }
            yield break;
        }

        // ส่ง friend request
        Debug.Log($"📤 Sending friend request...");
        var requestTask = databaseReference
            .Child("players")
            .Child(targetUserId)
            .Child("pendingFriendRequests")
            .Child(auth.CurrentUser.UserId)
            .SetValueAsync(multiCharacterData.playerName);

        yield return new WaitUntil(() => requestTask.IsCompleted);

        if (requestTask.Exception == null)
        {
            Debug.Log($"✅ Friend request sent to {targetPlayerName}!");
        }
        else
        {
            Debug.LogError($"❌ Failed to send: {requestTask.Exception.Message}");
        }
    }

    public void SendFriendRequestByUserId(string targetUserId)
    {
        if (!IsFirebaseReady()) return;
        StartCoroutine(SendFriendRequestByUserIdCoroutine(targetUserId));
    }

    private IEnumerator SendFriendRequestByUserIdCoroutine(string targetUserId)
    {
        Debug.Log($"🔍 Sending friend request to UserId: {targetUserId}");

        // ตรวจสอบว่า userId นี้มีอยู่จริงหรือไม่
        var checkTask = databaseReference.Child("players").Child(targetUserId).Child("playerName").GetValueAsync();
        yield return new WaitUntil(() => checkTask.IsCompleted);

        if (checkTask.Exception != null)
        {
            Debug.LogError($"❌ Error checking user: {checkTask.Exception.Message}");
            yield break;
        }

        if (!checkTask.Result.Exists)
        {
            Debug.Log($"❌ User ID '{targetUserId}' not found!");
            yield break;
        }

        string targetPlayerName = checkTask.Result.Value?.ToString();
        Debug.Log($"✅ Found player: {targetPlayerName}");

        // ตรวจสอบว่าเป็นตัวเองหรือไม่
        if (targetUserId == auth.CurrentUser.UserId)
        {
            Debug.Log("❌ Cannot send friend request to yourself!");
            yield break;
        }

        Debug.Log($"📤 Sending friend request to {targetPlayerName} (UserId: {targetUserId})");

        // ส่ง friend request
        var requestTask = databaseReference
            .Child("players")
            .Child(targetUserId)
            .Child("pendingFriendRequests")
            .Child(auth.CurrentUser.UserId)
            .SetValueAsync(multiCharacterData.playerName);

        yield return new WaitUntil(() => requestTask.IsCompleted);

        if (requestTask.Exception == null)
        {
            Debug.Log($"✅ Friend request sent to {targetPlayerName}");
        }
        else
        {
            Debug.LogError($"❌ Failed to send friend request: {requestTask.Exception.Message}");
        }
    }

    public void AcceptFriendRequest(string requesterName)
    {
        if (auth?.CurrentUser == null || multiCharacterData == null) return;
        StartCoroutine(AcceptFriendRequestCoroutine(requesterName));
    }

    private IEnumerator AcceptFriendRequestCoroutine(string requesterName)
    {
        Debug.Log($"🔍 Accepting friend request from: {requesterName}");

        // ค้นหา requester UserId
        var task = databaseReference.Child("players").GetValueAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError($"❌ Error reading players: {task.Exception.Message}");
            yield break;
        }

        string requesterUserId = null;
        foreach (var player in task.Result.Children)
        {
            try
            {
                var playerData = player;
                if (playerData.HasChild("playerName"))
                {
                    string playerName = playerData.Child("playerName").Value?.ToString();
                    if (playerName == requesterName)
                    {
                        requesterUserId = player.Key;
                        break;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Error processing player {player.Key}: {e.Message}");
            }
        }

        if (string.IsNullOrEmpty(requesterUserId))
        {
            Debug.LogError($"❌ Could not find UserId for {requesterName}");
            yield break;
        }

        Debug.Log($"✅ Found requester UserId: {requesterUserId}");

        // เพิ่มเป็นเพื่อนทั้งสองฝ่าย
        var currentUserId = auth.CurrentUser.UserId;

        // เพิ่มเพื่อนให้ตัวเอง
        multiCharacterData.friends.Add(requesterName);
        var task1 = databaseReference.Child("players").Child(currentUserId).Child("friends").Child(requesterUserId).SetValueAsync(requesterName);

        // เพิ่มเพื่อนให้อีกฝ่าย
        var task2 = databaseReference.Child("players").Child(requesterUserId).Child("friends").Child(currentUserId).SetValueAsync(multiCharacterData.playerName);

        // ลบ friend request
        multiCharacterData.pendingFriendRequests.Remove(requesterName);
        var task3 = databaseReference.Child("players").Child(currentUserId).Child("pendingFriendRequests").Child(requesterUserId).RemoveValueAsync();

        // รอให้ทุก task เสร็จ
        yield return new WaitUntil(() => task1.IsCompleted && task2.IsCompleted && task3.IsCompleted);

        // ตรวจสอบผลลัพธ์
        try
        {
            if (task1.Exception != null)
            {
                Debug.LogError($"❌ Failed to add friend to self: {task1.Exception.Message}");
            }
            else if (task2.Exception != null)
            {
                Debug.LogError($"❌ Failed to add friend to requester: {task2.Exception.Message}");
            }
            else if (task3.Exception != null)
            {
                Debug.LogError($"❌ Failed to remove friend request: {task3.Exception.Message}");
            }
            else
            {
                Debug.Log($"✅ {requesterName} is now your friend!");
                SavePlayerDataAsync();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Exception while checking accept friend results: {e.Message}");
        }
    }

    public void RejectFriendRequest(string requesterName)
    {
        if (multiCharacterData == null) return;

        multiCharacterData.pendingFriendRequests.Remove(requesterName);
        SavePlayerDataAsync();

        Debug.Log($"❌ Rejected friend request from {requesterName}");
    }

    public void RemoveFriend(string friendName)
    {
        if (multiCharacterData == null) return;

        multiCharacterData.friends.Remove(friendName);
        SavePlayerDataAsync();

        Debug.Log($"❌ Removed {friendName} from friends list");
    }

    public List<string> GetFriendsList()
    {
        return multiCharacterData?.friends ?? new List<string>();
    }

    public List<string> GetPendingFriendRequests()
    {
        return multiCharacterData?.pendingFriendRequests ?? new List<string>();
    }

    public void LoadFriendRequestsFromFirebase()
    {
        if (auth?.CurrentUser == null) return;
        StartCoroutine(LoadFriendRequestsCoroutine());
    }

    private IEnumerator LoadFriendRequestsCoroutine()
    {
        if (!IsFirebaseReady())
        {
            Debug.LogError("❌ Firebase not ready for loading friend requests");
            yield break;
        }

        Debug.Log("🔍 Loading friend requests from Firebase...");

        var task = databaseReference.Child("players").Child(auth.CurrentUser.UserId).Child("pendingFriendRequests").GetValueAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        try
        {
            if (task.Exception != null)
            {
                Debug.LogError($"❌ Error loading friend requests: {task.Exception.Message}");
                yield break;
            }

            if (task.Result.Exists)
            {
                if (multiCharacterData == null)
                {
                    Debug.LogError("❌ MultiCharacterData is null when loading friend requests!");
                    yield break;
                }

                multiCharacterData.pendingFriendRequests.Clear();

                foreach (var request in task.Result.Children)
                {
                    string requesterName = request.Value.ToString();
                    if (!multiCharacterData.pendingFriendRequests.Contains(requesterName))
                    {
                        multiCharacterData.pendingFriendRequests.Add(requesterName);
                    }
                }

                Debug.Log($"✅ Loaded {multiCharacterData.pendingFriendRequests.Count} friend requests");
            }
            else
            {
                Debug.Log("📭 No pending friend requests found");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Exception while loading friend requests: {e.Message}");
        }
    }

    public IEnumerator RefreshFriendRequestsCoroutine()
    {
        if (auth?.CurrentUser == null)
        {
            Debug.LogError("❌ Not authenticated for refresh");
            yield break;
        }

        Debug.Log("📡 Refreshing friend requests from Firebase...");

        var task = databaseReference.Child("players").Child(auth.CurrentUser.UserId).Child("pendingFriendRequests").GetValueAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError($"❌ Error refreshing friend requests: {task.Exception.Message}");
            yield break;
        }

        if (multiCharacterData == null)
        {
            Debug.LogError("❌ MultiCharacterData is null during refresh!");
            yield break;
        }

        // เคลียร์และโหลดใหม่
        int oldCount = multiCharacterData.pendingFriendRequests.Count;
        multiCharacterData.pendingFriendRequests.Clear();

        if (task.Result.Exists)
        {
            foreach (var request in task.Result.Children)
            {
                string requesterName = request.Value.ToString();
                if (!multiCharacterData.pendingFriendRequests.Contains(requesterName))
                {
                    multiCharacterData.pendingFriendRequests.Add(requesterName);
                }
            }
        }

        int newCount = multiCharacterData.pendingFriendRequests.Count;
        Debug.Log($"📨 Friend requests: {oldCount} → {newCount}");

        if (newCount > oldCount)
        {
            Debug.Log($"🎉 You have {newCount - oldCount} new friend request(s)!");
        }
    }

    public IEnumerator RefreshFriendsListCoroutine()
    {
        if (auth?.CurrentUser == null) yield break;

        Debug.Log("📡 Refreshing friends list from Firebase...");

        var task = databaseReference.Child("players").Child(auth.CurrentUser.UserId).Child("friends").GetValueAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError($"❌ Error refreshing friends: {task.Exception.Message}");
            yield break;
        }

        if (multiCharacterData == null) yield break;

        // เคลียร์และโหลดใหม่
        int oldCount = multiCharacterData.friends.Count;
        multiCharacterData.friends.Clear();

        if (task.Result.Exists)
        {
            foreach (var friend in task.Result.Children)
            {
                string friendName = friend.Value.ToString();
                if (!multiCharacterData.friends.Contains(friendName))
                {
                    multiCharacterData.friends.Add(friendName);
                }
            }
        }

        int newCount = multiCharacterData.friends.Count;
        Debug.Log($"👥 Friends: {oldCount} → {newCount}");
    }
    #endregion
    #region 🆕 Inventory Save Methods

    /// <summary>
    /// บันทึกข้อมูล inventory และ equipment ทั้งหมดของ character
    /// </summary>
    public void SaveInventoryData(Character character)
    {
        if (character == null)
        {
            Debug.LogError("[SaveInventoryData] Character is null!");
            return;
        }

        if (multiCharacterData == null)
        {
            Debug.LogError("[SaveInventoryData] MultiCharacterData is null!");
            return;
        }

        try
        {
            Debug.Log($"[SaveInventoryData] 💾 Starting inventory save for {character.CharacterName}");

            // 🔧 แก้ไข: ตรวจสอบ inventory ก่อน save
            var inventory = character.GetInventory();
            if (inventory == null)
            {
                Debug.LogError("[SaveInventoryData] ❌ Character has no inventory to save!");
                return;
            }

            // 🆕 เพิ่ม validation check
            int itemsInInventory = inventory.UsedSlots;
            Debug.Log($"[SaveInventoryData] Current inventory: {itemsInInventory}/{inventory.CurrentSlots} slots used");

            // 🔧 แก้ไข: ถ้าไม่มี items ใน inventory เลย ให้ warning
            if (itemsInInventory == 0)
            {
                Debug.LogWarning("[SaveInventoryData] ⚠️ No items in inventory to save!");
                // ไม่ return false เพื่อให้ยังคง save equipment
            }

            // 1. Save Shared Inventory (แม้ว่าจะว่างก็ตาม)
            bool inventorySaved = SaveSharedInventoryDataSafe(character);

            // 2. Save Character Equipment
            bool equipmentSaved = SaveCharacterEquipmentData(character);

            // 3. Update debug info
            multiCharacterData.UpdateAllInventoryDebugInfo();

            // 🔧 แก้ไข: Save แม้ว่าจะไม่มี inventory items
            if (inventorySaved || equipmentSaved)
            {
                Debug.Log($"[SaveInventoryData] ✅ Save completed for {character.CharacterName}");
                Debug.Log($"  - Inventory saved: {inventorySaved}");
                Debug.Log($"  - Equipment saved: {equipmentSaved}");

                // Auto save to Firebase
                SavePlayerDataAsync();
            }
            else
            {
                Debug.LogWarning("[SaveInventoryData] ⚠️ No data was saved");

                // 🆕 ลอง save อีกครั้งถ้าไม่สำเร็จ
                Debug.Log("[SaveInventoryData] 🔄 Retrying save...");
                StartCoroutine(RetrySaveInventoryData(character));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveInventoryData] ❌ Error saving inventory: {e.Message}");
            Debug.LogError($"[SaveInventoryData] Stack trace: {e.StackTrace}");
        }
    }

    private bool SaveSharedInventoryDataSafe(Character character)
    {
        try
        {
            var inventory = character.GetInventory();
            if (inventory == null)
            {
                Debug.LogWarning("[SaveSharedInventoryDataSafe] Character has no inventory");
                return false;
            }

            Debug.Log($"[SaveSharedInventoryDataSafe] 📦 Saving inventory: {inventory.UsedSlots}/{inventory.CurrentSlots} slots");

            // แปลง Inventory เป็น SharedInventoryData
            var sharedData = InventoryDataConverter.ToSharedInventoryData(inventory);
            if (sharedData == null)
            {
                Debug.LogError("[SaveSharedInventoryDataSafe] Failed to convert inventory data");
                return false;
            }

            // บันทึกลง multiCharacterData
            multiCharacterData.sharedInventory = sharedData;

            // 🆕 เพิ่ม validation หลัง save
            bool saveSuccess = ValidateSharedInventorySave(inventory, sharedData);

            if (saveSuccess)
            {
                Debug.Log($"[SaveSharedInventoryDataSafe] ✅ Saved {sharedData.items.Count} items to shared inventory");
                LogSavedInventoryItems(sharedData);
            }
            else
            {
                Debug.LogError("[SaveSharedInventoryDataSafe] ❌ Save validation failed!");
            }

            return saveSuccess;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSharedInventoryDataSafe] ❌ Error: {e.Message}");
            return false;
        }
    }

    // 🆕 เพิ่ม validation หลัง save
    private bool ValidateSharedInventorySave(Inventory inventory, SharedInventoryData sharedData)
    {
        try
        {
            int inventoryItemCount = inventory.UsedSlots;
            int savedItemCount = sharedData?.items?.Count ?? 0;

            Debug.Log($"[ValidateSharedInventorySave] Inventory items: {inventoryItemCount}, Saved items: {savedItemCount}");

            // ตรวจสอบว่าจำนวน items ตรงกันหรือไม่
            if (inventoryItemCount != savedItemCount)
            {
                Debug.LogWarning($"[ValidateSharedInventorySave] ⚠️ Item count mismatch: {inventoryItemCount} vs {savedItemCount}");

                // ถ้า inventory ว่างแต่มี saved data แปลว่าอาจมีปัญหา
                if (inventoryItemCount == 0 && savedItemCount > 0)
                {
                    Debug.LogError("[ValidateSharedInventorySave] ❌ Inventory is empty but saved data exists!");
                    return false;
                }
            }

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ValidateSharedInventorySave] ❌ Error: {e.Message}");
            return false;
        }
    }

    // 🆕 เพิ่ม retry mechanism
    private System.Collections.IEnumerator RetrySaveInventoryData(Character character)
    {
        yield return new WaitForSeconds(1f);

        Debug.Log("[RetrySaveInventoryData] 🔄 Retrying inventory save...");

        try
        {
            bool inventorySaved = SaveSharedInventoryDataSafe(character);
            bool equipmentSaved = SaveCharacterEquipmentData(character);

            if (inventorySaved || equipmentSaved)
            {
                Debug.Log("[RetrySaveInventoryData] ✅ Retry save successful");
                SavePlayerDataAsync();
            }
            else
            {
                Debug.LogError("[RetrySaveInventoryData] ❌ Retry save failed");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RetrySaveInventoryData] ❌ Error: {e.Message}");
        }
    }

    /// <summary>
    /// บันทึกข้อมูล shared inventory
    /// </summary>
    private bool SaveSharedInventoryData(Character character)
    {
        try
        {
            var inventory = character.GetInventory();
            if (inventory == null)
            {
                Debug.LogWarning("[SaveSharedInventoryData] Character has no inventory");
                return false;
            }

            Debug.Log($"[SaveSharedInventoryData] 📦 Saving shared inventory: {inventory.UsedSlots}/{inventory.CurrentSlots} slots");

            // แปลง Inventory เป็น SharedInventoryData
            var sharedData = InventoryDataConverter.ToSharedInventoryData(inventory);
            if (sharedData == null)
            {
                Debug.LogError("[SaveSharedInventoryData] Failed to convert inventory data");
                return false;
            }

            // บันทึกลง multiCharacterData
            multiCharacterData.sharedInventory = sharedData;

            Debug.Log($"[SaveSharedInventoryData] ✅ Saved {sharedData.items.Count} items to shared inventory");

            // Debug: แสดงรายการไอเทมที่บันทึก
            LogSavedInventoryItems(sharedData);

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSharedInventoryData] ❌ Error: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// บันทึกข้อมูล equipment และ potion ของ character
    /// </summary>
    private bool SaveCharacterEquipmentData(Character character)
    {
        try
        {
            // 🔧 แก้ไข: ใช้ currentActiveCharacter แทน CharacterName
            string characterType = multiCharacterData.currentActiveCharacter;
            Debug.Log($"[SaveCharacterEquipmentData] ⚔️ Saving equipment for {characterType} (Character: {character.CharacterName})");

            // แปลง Character equipment เป็น CharacterEquipmentData
            var equipmentData = InventoryDataConverter.ToCharacterEquipmentData(character);
            if (equipmentData == null)
            {
                Debug.LogError("[SaveCharacterEquipmentData] Failed to convert equipment data");
                return false;
            }

            // 🔧 แก้ไข: ตั้งค่า characterType ให้ถูกต้อง
            equipmentData.characterType = characterType;

            // หาหรือสร้าง character data - ใช้ characterType ที่ถูกต้อง
            var characterProgressData = multiCharacterData.GetOrCreateCharacterData(characterType);
            if (characterProgressData == null)
            {
                Debug.LogError($"[SaveCharacterEquipmentData] Failed to get character data for {characterType}");
                return false;
            }

            // 🔧 แก้ไข: ตรวจสอบว่า characterProgressData มี characterType ถูกต้อง
            if (string.IsNullOrEmpty(characterProgressData.characterType))
            {
                characterProgressData.characterType = characterType;
                Debug.Log($"[SaveCharacterEquipmentData] Set character type to {characterType}");
            }

            // บันทึกข้อมูล equipment
            characterProgressData.characterEquipment = equipmentData;

            Debug.Log($"[SaveCharacterEquipmentData] ✅ Saved equipment for {characterType}");
            Debug.Log($"[SaveCharacterEquipmentData] Equipment count: {equipmentData.equipment.equippedCount}");
            Debug.Log($"[SaveCharacterEquipmentData] Potion count: {equipmentData.totalPotionCount}");

            // 🆕 Debug: แสดงข้อมูล character ทั้งหมด
            LogCharacterDataStatus();

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveCharacterEquipmentData] ❌ Error: {e.Message}");
            return false;
        }
    }

    private void LogCharacterDataStatus()
    {
        if (multiCharacterData?.characters == null)
        {
            Debug.Log("[LogCharacterDataStatus] No character data");
            return;
        }

        Debug.Log("=== CHARACTER DATA STATUS ===");
        Debug.Log($"Active Character: {multiCharacterData.currentActiveCharacter}");
        Debug.Log($"Total Characters: {multiCharacterData.characters.Count}");

        for (int i = 0; i < multiCharacterData.characters.Count; i++)
        {
            var character = multiCharacterData.characters[i];
            if (character != null)
            {
                bool hasStats = character.totalMaxHp > 0;
                bool hasEquipment = character.HasEquipmentData();

                Debug.Log($"Character {i}: Type='{character.characterType}', HasStats={hasStats}, HasEquipment={hasEquipment}");
                Debug.Log($"  Stats: HP={character.totalMaxHp}, ATK={character.totalAttackDamage}");
                Debug.Log($"  Equipment: Eq={character.totalEquippedItems}, Pot={character.totalPotions}");
            }
            else
            {
                Debug.Log($"Character {i}: NULL");
            }
        }
        Debug.Log("=============================");
    }
    /// <summary>
    /// บันทึกเฉพาะ potion slots ของ character
    /// </summary>
    public void SaveCharacterPotionData(Character character)
    {
        if (character == null || multiCharacterData == null)
        {
            Debug.LogError("[SaveCharacterPotionData] Character or MultiCharacterData is null!");
            return;
        }

        try
        {
            // 🔧 ใช้ currentActiveCharacter แทน CharacterName
            string characterType = multiCharacterData.currentActiveCharacter;
            Debug.Log($"[SaveCharacterPotionData] 🧪 Saving potion data for {characterType} (Character: {character.CharacterName})");

            var characterProgressData = multiCharacterData.GetOrCreateCharacterData(characterType);
            if (characterProgressData?.characterEquipment == null)
            {
                Debug.LogError($"[SaveCharacterPotionData] Character equipment data is null for {characterType}");
                return;
            }

            // 🆕 Debug potion data ก่อน save
            Debug.Log($"[SaveCharacterPotionData] === POTION DATA BEFORE SAVE ===");
            for (int i = 0; i < 5; i++)
            {
                var potionItem = character.GetPotionInSlot(i);
                int stackCount = character.GetPotionStackCount(i);
                Debug.Log($"  Slot {i}: {(potionItem?.ItemName ?? "EMPTY")} x{stackCount}");
            }

            // อัปเดตเฉพาะ potion slots
            bool hasChanges = false;
            for (int i = 0; i < 5; i++)
            {
                var potionItem = character.GetPotionInSlot(i);
                int stackCount = character.GetPotionStackCount(i);

                // ดึงข้อมูลเก่าเพื่อเปรียบเทียบ
                var oldPotionSlot = characterProgressData.characterEquipment.GetPotionSlot(i);
                string oldItemId = oldPotionSlot?.itemId ?? "";
                int oldStackCount = oldPotionSlot?.stackCount ?? 0;

                if (potionItem != null && stackCount > 0)
                {
                    // มี potion ใน slot นี้
                    string newItemId = potionItem.ItemId;

                    if (oldItemId != newItemId || oldStackCount != stackCount)
                    {
                        characterProgressData.characterEquipment.SetPotionSlot(i, newItemId, stackCount, potionItem.ItemName);
                        Debug.Log($"[SaveCharacterPotionData] 🔄 Updated slot {i}: {potionItem.ItemName} x{stackCount} (was: {oldItemId} x{oldStackCount})");
                        hasChanges = true;
                    }
                    else
                    {
                        Debug.Log($"[SaveCharacterPotionData] ✓ Slot {i}: No changes - {potionItem.ItemName} x{stackCount}");
                    }
                }
                else
                {
                    // ไม่มี potion ใน slot นี้
                    if (!string.IsNullOrEmpty(oldItemId) || oldStackCount > 0)
                    {
                        characterProgressData.characterEquipment.ClearPotionSlot(i);
                        Debug.Log($"[SaveCharacterPotionData] 🧹 Cleared slot {i} (was: {oldItemId} x{oldStackCount})");
                        hasChanges = true;
                    }
                    else
                    {
                        Debug.Log($"[SaveCharacterPotionData] ✓ Slot {i}: Already empty");
                    }
                }
            }

            if (hasChanges)
            {
                characterProgressData.UpdateEquipmentDebugInfo();
                Debug.Log($"[SaveCharacterPotionData] ✅ Potion data saved for {characterType} with changes");

                // 🆕 Debug potion data หลัง save
                Debug.Log($"[SaveCharacterPotionData] === POTION DATA AFTER SAVE ===");
                for (int i = 0; i < characterProgressData.characterEquipment.potionSlots.Count; i++)
                {
                    var savedSlot = characterProgressData.characterEquipment.potionSlots[i];
                    if (!savedSlot.IsEmpty())
                    {
                        Debug.Log($"  Saved Slot {i}: {savedSlot.itemName} x{savedSlot.stackCount} (ID: {savedSlot.itemId})");
                    }
                    else
                    {
                        Debug.Log($"  Saved Slot {i}: EMPTY");
                    }
                }

                // Auto save to Firebase
                SavePlayerDataAsync();
            }
            else
            {
                Debug.Log($"[SaveCharacterPotionData] ✓ No changes detected for {characterType}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveCharacterPotionData] ❌ Error: {e.Message}");
            Debug.LogError($"[SaveCharacterPotionData] Stack trace: {e.StackTrace}");
        }
    }
    /// <summary>
    /// บังคับบันทึก inventory ทันที (สำหรับกรณีเร่งด่วน)
    /// </summary>
    public void ForceImmediateSaveInventory(Character character)
    {
        Debug.Log("[ForceImmediateSaveInventory] 🚀 Force saving inventory data...");

        SaveInventoryData(character);

        // บันทึกลง PlayerPrefs ด้วย (เป็น backup)
        SaveInventoryToPlayerPrefs(character);
    }

    /// <summary>
    /// บันทึกข้อมูล inventory พื้นฐานลง PlayerPrefs (เป็น backup)
    /// </summary>
    private void SaveInventoryToPlayerPrefs(Character character)
    {
        try
        {
            var inventory = character?.GetInventory();
            if (inventory != null)
            {
                PlayerPrefs.SetInt("InventoryCurrentSlots", inventory.CurrentSlots);
                PlayerPrefs.SetInt("InventoryUsedSlots", inventory.UsedSlots);
                PlayerPrefs.SetString("InventoryLastSave", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                PlayerPrefs.Save();

                Debug.Log("[SaveInventoryToPlayerPrefs] ✅ Inventory backup saved to PlayerPrefs");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveInventoryToPlayerPrefs] ❌ Error: {e.Message}");
        }
    }

    #endregion

    #region 🆕 Debug & Logging Methods

    /// <summary>
    /// แสดงรายการไอเทมที่บันทึกลง shared inventory
    /// </summary>
    private void LogSavedInventoryItems(SharedInventoryData sharedData)
    {
        if (sharedData?.items == null || sharedData.items.Count == 0)
        {
            Debug.Log("[LogSavedInventoryItems] No items to log");
            return;
        }

        Debug.Log("=== SAVED INVENTORY ITEMS ===");
        foreach (var item in sharedData.items)
        {
            if (item != null && item.IsValid())
            {
                string stackInfo = item.stackCount > 1 ? $" x{item.stackCount}" : "";
                Debug.Log($"Slot {item.slotIndex}: {item.itemName}{stackInfo} ({item.itemType}) ID: {item.itemId}");
            }
        }
        Debug.Log($"Total: {sharedData.items.Count} items saved");
        Debug.Log("============================");
    }

    /// <summary>
    /// แสดงรายการอุปกรณ์ที่บันทึก
    /// </summary>
  


    /// <summary>
    /// แสดงสถิติการบันทึกทั้งหมด
    /// </summary>
    public void LogSaveStats()
    {
        if (multiCharacterData == null)
        {
            Debug.Log("[LogSaveStats] No data to show");
            return;
        }

        Debug.Log("=== INVENTORY SAVE STATS ===");
        Debug.Log($"Player: {multiCharacterData.playerName}");
        Debug.Log($"Last Save: {multiCharacterData.inventoryLastSaveTime}");
        Debug.Log($"Shared Items: {multiCharacterData.totalSharedItems}");
        Debug.Log($"Has Data: {multiCharacterData.HasAnyInventoryOrEquipmentData()}");

        foreach (var character in multiCharacterData.characters)
        {
            if (character?.characterEquipment != null)
            {
                Debug.Log($"{character.characterType}: Eq={character.totalEquippedItems}, Pot={character.totalPotions}");
            }
        }
        Debug.Log("===========================");
    }

    #endregion
    #region 🆕 Inventory Load Methods

    /// <summary>
    /// โหลดข้อมูล inventory และ equipment ทั้งหมดของ character
    /// </summary>
    public void LoadInventoryData(Character character)
    {
        if (character == null)
        {
            Debug.LogError("[LoadInventoryData] Character is null!");
            return;
        }

        if (multiCharacterData == null)
        {
            Debug.LogError("[LoadInventoryData] MultiCharacterData is null!");
            return;
        }

        try
        {
            Debug.Log($"[LoadInventoryData] 📥 Starting inventory load for {character.CharacterName}");

            // 1. Load Shared Inventory
            bool inventoryLoaded = LoadSharedInventoryData(character);

            // 2. Load Character Equipment (ใช้ระบบเดิมที่สมบูรณ์)
            bool equipmentLoaded = LoadCharacterEquipmentData(character);

            if (inventoryLoaded || equipmentLoaded)
            {
                Debug.Log($"[LoadInventoryData] ✅ Inventory load completed for {character.CharacterName}");

                // 3. Force refresh หลัง load เสร็จ
                ForceRefreshInventoryUI(character);

                // 🆕 4. แจ้ง LevelManager ให้คำนวณ stats ใหม่หลังโหลด equipment
                NotifyLevelManagerEquipmentLoaded(character);
            }
            else
            {
                Debug.LogWarning("[LoadInventoryData] ⚠️ No data was loaded - giving starter items");
                GiveStarterItemsIfNeeded(character);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LoadInventoryData] ❌ Error loading inventory: {e.Message}");
            Debug.LogError($"[LoadInventoryData] Stack trace: {e.StackTrace}");

            // ถ้า load ไม่ได้ ให้ starter items
            GiveStarterItemsIfNeeded(character);
        }
    }
  
    private System.Collections.IEnumerator DelayedUIRefresh(Character character)
    {
        yield return new WaitForSeconds(0.2f);

        // Force refresh EquipmentSlotManager
        var equipmentManager = character.GetComponent<EquipmentSlotManager>();
        if (equipmentManager?.IsConnected() == true)
        {
            equipmentManager.ForceRefreshFromCharacter();
        }

        // Force refresh CombatUIManager
        var combatUI = FindObjectOfType<CombatUIManager>();
        if (combatUI?.equipmentSlotManager?.IsConnected() == true)
        {
            combatUI.equipmentSlotManager.ForceRefreshFromCharacter();
        }

        // แจ้ง stats changed
        Character.RaiseOnStatsChanged();
        Canvas.ForceUpdateCanvases();

        Debug.Log("[DelayedUIRefresh] ✅ UI refreshed after equipment load");
    }

    private void ForceRefreshInventoryUIOnly(Character character)
    {
        try
        {
            Debug.Log("[ForceRefreshInventoryUIOnly] 🔄 Refreshing UI only (no stats changes)...");

            // 1. Force refresh equipment slots UI
            var equipmentSlotManager = character.GetComponent<EquipmentSlotManager>();
            if (equipmentSlotManager?.IsConnected() == true)
            {
                equipmentSlotManager.ForceRefreshFromCharacter();
            }

            // 2. Force refresh CombatUI equipment slots
            var combatUIManager = FindObjectOfType<CombatUIManager>();
            if (combatUIManager?.equipmentSlotManager?.IsConnected() == true)
            {
                combatUIManager.equipmentSlotManager.ForceRefreshFromCharacter();
            }

            // 3. Force refresh inventory grid
            var inventoryGrid = FindObjectOfType<InventoryGridManager>();
            if (inventoryGrid != null)
            {
                inventoryGrid.ForceUpdateFromCharacter();
                inventoryGrid.ForceSyncAllSlots();
            }

            // 4. Force update Canvas
            Canvas.ForceUpdateCanvases();

            Debug.Log("[ForceRefreshInventoryUIOnly] ✅ UI refresh completed (stats unchanged)");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ForceRefreshInventoryUIOnly] ❌ Error: {e.Message}");
        }
    }
    private void NotifyCharacterEquipmentLoaded(Character character)
    {
        Debug.Log("[NotifyCharacterEquipmentLoaded] ⚠️ DISABLED - no longer notifying Character to recalculate stats");
        Debug.Log("[NotifyCharacterEquipmentLoaded] ✅ Equipment loaded without stats interference");

        // ไม่เรียก character.ApplyLoadedEquipmentStats() เพื่อไม่ให้ stats บัค
        // เฉพาะ refresh UI
        ForceRefreshInventoryUIOnly(character);
    }
  
    private bool LoadEquipmentSlotsSimple(Character character, CharacterEquipmentData equipmentData)
    {
        try
        {
            int loadedCount = 0;
            var equipment = equipmentData.equipment;

            Debug.Log("[LoadEquipmentSlotsSimple] Loading 6 equipment slots (no stats application)...");

            // เคลียร์ equipment ก่อน
            character.ClearAllEquipmentForLoad();

            // โหลดแต่ละ slot โดยไม่ apply stats
            if (LoadSingleEquipmentSlotSimple(character, ItemType.Head, equipment.headItemId))
                loadedCount++;
            if (LoadSingleEquipmentSlotSimple(character, ItemType.Armor, equipment.armorItemId))
                loadedCount++;
            if (LoadSingleEquipmentSlotSimple(character, ItemType.Weapon, equipment.weaponItemId))
                loadedCount++;
            if (LoadSingleEquipmentSlotSimple(character, ItemType.Pants, equipment.pantsItemId))
                loadedCount++;
            if (LoadSingleEquipmentSlotSimple(character, ItemType.Shoes, equipment.shoesItemId))
                loadedCount++;
            if (LoadSingleEquipmentSlotSimple(character, ItemType.Rune, equipment.runeItemId))
                loadedCount++;

            Debug.Log($"[LoadEquipmentSlotsSimple] ✅ Loaded {loadedCount}/6 equipment items (no stats changes)");
            return loadedCount > 0;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LoadEquipmentSlotsSimple] ❌ Error: {e.Message}");
            return false;
        }
    }
    private bool LoadSingleEquipmentSlotSimple(Character character, ItemType itemType, string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return false;

        try
        {
            // หา ItemData จาก ID
            ItemData itemData = GetItemDataById(itemId);
            if (itemData == null)
            {
                Debug.LogError($"[LoadSingleEquipmentSlotSimple] Item not found: {itemId} for {itemType}");
                return false;
            }

            // ตรวจสอบ item type
            if (itemData.ItemType != itemType)
            {
                Debug.LogError($"[LoadSingleEquipmentSlotSimple] Item type mismatch: {itemData.ItemType} != {itemType}");
                return false;
            }

            // โหลด item ลง character โดยไม่ apply stats
            bool success = character.LoadEquipmentDirectly(itemData);
            if (success)
            {
                Debug.Log($"[LoadSingleEquipmentSlotSimple] ✅ Loaded {itemData.ItemName} (no stats applied)");
                return true;
            }

            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LoadSingleEquipmentSlotSimple] ❌ Error: {e.Message}");
            return false;
        }
    }

  
    private bool LoadPotionSlotsSimple(Character character, CharacterEquipmentData equipmentData)
    {
        try
        {
            int loadedCount = 0;

            for (int i = 0; i < 5; i++)
            {
                var potionSlot = equipmentData.GetPotionSlot(i);
                if (potionSlot?.IsValid() == true)
                {
                    ItemData potionData = GetItemDataById(potionSlot.itemId);
                    if (potionData?.ItemType == ItemType.Potion)
                    {
                        bool loaded = character.LoadPotionDirectly(potionData, i, potionSlot.stackCount);
                        if (loaded) loadedCount++;
                    }
                }
            }

            Debug.Log($"[LoadPotionSlotsSimple] ✅ Loaded {loadedCount}/5 potion slots");
            return loadedCount > 0;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LoadPotionSlotsSimple] ❌ Error: {e.Message}");
            return false;
        }
    }

    /// </summary>
    private void NotifyLevelManagerEquipmentLoaded(Character character)
    {
        try
        {
            Debug.Log("[NotifyLevelManagerEquipmentLoaded] 📢 Notifying LevelManager that equipment is loaded...");

            var levelManager = character.GetComponent<LevelManager>();
            if (levelManager != null)
            {
                // เรียก method ของ LevelManager เพื่อคำนวณ stats ใหม่
                levelManager.OnEquipmentLoadedRecalculateStats();
                Debug.Log("[NotifyLevelManagerEquipmentLoaded] ✅ LevelManager notified successfully");
            }
            else
            {
                Debug.LogWarning("[NotifyLevelManagerEquipmentLoaded] ⚠️ No LevelManager found on character");

                // ถ้าไม่มี LevelManager ให้เรียก Character.ApplyLoadedEquipmentStats() โดยตรง
                character.ApplyLoadedEquipmentStats();
                Debug.Log("[NotifyLevelManagerEquipmentLoaded] ✅ Applied equipment stats directly to character");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NotifyLevelManagerEquipmentLoaded] ❌ Error notifying LevelManager: {e.Message}");
        }
    }

    /// <summary>
    /// โหลดข้อมูล shared inventory
    /// </summary>
    private bool LoadSharedInventoryData(Character character)
    {
        try
        {
            var inventory = character.GetInventory();
            if (inventory == null)
            {
                Debug.LogWarning("[LoadSharedInventoryData] Character has no inventory");
                return false;
            }

            var sharedData = multiCharacterData.sharedInventory;
            if (sharedData == null || !sharedData.IsValid() || sharedData.items.Count == 0)
            {
                Debug.LogWarning("[LoadSharedInventoryData] No shared inventory data to load");
                return false;
            }

            Debug.Log($"[LoadSharedInventoryData] 📦 Loading {sharedData.items.Count} items from shared inventory");

            // 🆕 ตรวจสอบว่ามี items ใน inventory ปัจจุบันหรือไม่
            int currentItems = inventory.UsedSlots;
            Debug.Log($"[LoadSharedInventoryData] Current inventory has {currentItems} items");

            // 🆕 ถ้ามี items อยู่แล้วและจำนวนตรงกับ Firebase ให้ skip การ load
            if (currentItems > 0 && currentItems == sharedData.items.Count)
            {
                Debug.Log("[LoadSharedInventoryData] ✅ Inventory already has correct items, skipping load");
                return true;
            }

            // 🆕 ถ้ามี items อยู่แล้วแต่จำนวนไม่ตรง ให้เก็บ backup ก่อน
            List<InventoryItem> backupItems = null;
            if (currentItems > 0)
            {
                Debug.LogWarning($"[LoadSharedInventoryData] ⚠️ Item count mismatch: Current={currentItems}, Firebase={sharedData.items.Count}");
                backupItems = BackupCurrentInventory(inventory);
            }

            // 🔧 แก้ไข: ตรวจสอบข้อมูล Firebase อย่างละเอียด
            if (!ValidateFirebaseInventoryData(sharedData))
            {
                Debug.LogError("[LoadSharedInventoryData] ❌ Firebase data validation failed!");

                if (backupItems != null)
                {
                    Debug.Log("[LoadSharedInventoryData] 🔄 Keeping current inventory due to invalid Firebase data");
                    return false; // ไม่ clear inventory ถ้าข้อมูล Firebase ไม่ถูกต้อง
                }
                return false;
            }

            // เคลียร์ inventory เฉพาะเมื่อแน่ใจว่าข้อมูล Firebase ถูกต้อง
            Debug.Log("[LoadSharedInventoryData] 🧹 Clearing current inventory...");
            inventory.ClearInventory();

            // ตั้งค่า grid settings
            if (sharedData.currentSlots > 0)
            {
                int expandSlots = sharedData.currentSlots - inventory.CurrentSlots;
                if (expandSlots > 0 && inventory.CanExpandInventory(expandSlots))
                {
                    inventory.ExpandInventory(expandSlots);
                    Debug.Log($"[LoadSharedInventoryData] Expanded inventory to {sharedData.currentSlots} slots");
                }
            }

            // โหลดไอเทมทีละตัว
            int successCount = 0;
            int failCount = 0;

            foreach (var savedItem in sharedData.items)
            {
                if (savedItem == null || !savedItem.IsValid())
                {
                    failCount++;
                    continue;
                }

                bool loaded = LoadSingleInventoryItemSafe(inventory, savedItem);
                if (loaded)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                }
            }

            Debug.Log($"[LoadSharedInventoryData] Load result: {successCount} success, {failCount} failed");

            // 🆕 ตรวจสอบผลลัพธ์และ restore backup ถ้าจำเป็น
            if (successCount == 0)
            {
                Debug.LogError("[LoadSharedInventoryData] ❌ Failed to load any items!");

                if (backupItems != null && backupItems.Count > 0)
                {
                    Debug.Log("[LoadSharedInventoryData] 🔄 Restoring backup inventory...");
                    RestoreInventoryBackup(inventory, backupItems);
                    return false;
                }
                else
                {
                    Debug.LogWarning("[LoadSharedInventoryData] ⚠️ No backup available, inventory is now empty");
                    return false;
                }
            }

            // 🆕 ตรวจสอบว่าโหลดครบถ้วนหรือไม่
            if (successCount < sharedData.items.Count)
            {
                Debug.LogWarning($"[LoadSharedInventoryData] ⚠️ Partial load: {successCount}/{sharedData.items.Count} items");
            }

            Debug.Log($"[LoadSharedInventoryData] ✅ Successfully loaded {successCount} items to inventory");
            Debug.Log($"[LoadSharedInventoryData] Final inventory usage: {inventory.UsedSlots}/{inventory.CurrentSlots} slots");

            return successCount > 0;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LoadSharedInventoryData] ❌ Error: {e.Message}");
            Debug.LogError($"[LoadSharedInventoryData] Stack trace: {e.StackTrace}");
            return false;
        }
    }
    private bool ValidateFirebaseInventoryData(SharedInventoryData sharedData)
    {
        try
        {
            if (sharedData?.items == null)
            {
                Debug.LogError("[ValidateFirebaseInventoryData] SharedData or items is null");
                return false;
            }

            if (sharedData.items.Count == 0)
            {
                Debug.LogWarning("[ValidateFirebaseInventoryData] No items in Firebase data");
                return false;
            }

            int validItems = 0;
            int invalidItems = 0;

            foreach (var item in sharedData.items)
            {
                if (item?.IsValid() == true)
                {
                    // ตรวจสอบว่าหา ItemData ได้หรือไม่
                    ItemData itemData = GetItemDataById(item.itemId);
                    if (itemData != null)
                    {
                        validItems++;
                    }
                    else
                    {
                        Debug.LogWarning($"[ValidateFirebaseInventoryData] Item not found in database: {item.itemId} ({item.itemName})");
                        invalidItems++;
                    }
                }
                else
                {
                    invalidItems++;
                }
            }

            Debug.Log($"[ValidateFirebaseInventoryData] Validation result: {validItems} valid, {invalidItems} invalid");

            // ต้องมี valid items อย่างน้อย 50% ถึงจะ load
            bool isValid = validItems > 0 && (validItems >= invalidItems);

            if (!isValid)
            {
                Debug.LogError($"[ValidateFirebaseInventoryData] ❌ Too many invalid items: {invalidItems}/{validItems + invalidItems}");
            }

            return isValid;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ValidateFirebaseInventoryData] ❌ Error: {e.Message}");
            return false;
        }
    }
    // ใน PersistentPlayerData.cs - แก้ไข SafeAutoSaveInventory method
    public void SafeAutoSaveInventory(Character character, string action = "Auto-Save")
    {
        if (character == null || multiCharacterData == null)
        {
            Debug.LogWarning($"[SafeAutoSaveInventory] Cannot save - missing components");
            return;
        }

        try
        {
            var inventory = character.GetInventory();
            if (inventory == null)
            {
                Debug.LogWarning($"[SafeAutoSaveInventory] Character has no inventory");
                return;
            }

            int currentItems = inventory.UsedSlots;

            // 🔧 แก้ไข: ไม่ต้อง reload จาก Firebase ใน auto-save
            // การ auto-save ควรจะเป็นการบันทึกเท่านั้น ไม่ใช่การโหลด
            if (currentItems == 0 && action != "Clear Inventory")
            {
                bool hasFirebaseData = multiCharacterData.sharedInventory?.items?.Count > 0;
                if (hasFirebaseData)
                {
                    Debug.LogWarning($"[SafeAutoSaveInventory] ⚠️ Preventing save of empty inventory when Firebase has data!");

                    // 🔧 แก้ไข: ไม่เรียก LoadInventoryData ที่นี่
                    // LoadInventoryData(character); // ลบบรรทัดนี้

                    Debug.LogWarning($"[SafeAutoSaveInventory] ❌ BLOCKED: Attempt to save empty inventory when Firebase has data!");
                    return;
                }
            }

            Debug.Log($"[SafeAutoSaveInventory] 💾 Safe saving inventory with {currentItems} items (Action: {action})");

            // เก็บ backup ก่อน save
            var backupData = CreateInventoryBackupData(inventory);

            // Save ปกติ (ไม่มีการ load)
            SaveInventoryData(character);

            // Validate ว่า save สำเร็จ
            StartCoroutine(ValidateSaveSuccess(character, backupData, currentItems));

        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SafeAutoSaveInventory] ❌ Error: {e.Message}");
        }
    }

    // 🆕 สร้าง backup data สำหรับ validation
    private InventoryBackupData CreateInventoryBackupData(Inventory inventory)
    {
        var backup = new InventoryBackupData();
        backup.totalItems = inventory.UsedSlots;
        backup.currentSlots = inventory.CurrentSlots;
        backup.timestamp = System.DateTime.Now;

        for (int i = 0; i < inventory.CurrentSlots; i++)
        {
            var item = inventory.GetItem(i);
            if (item != null && !item.IsEmpty)
            {
                backup.itemNames.Add($"{item.itemData.ItemName} x{item.stackCount}");
            }
        }

        return backup;
    }

    // 🆕 Validate save success
    private System.Collections.IEnumerator ValidateSaveSuccess(Character character, InventoryBackupData backup, int expectedItems)
    {
        yield return new WaitForSeconds(1f); // รอให้ save เสร็จ

        try
        {
            Debug.Log($"[ValidateSaveSuccess] Validating save success...");

            // ตรวจสอบข้อมูลใน multiCharacterData
            int savedItems = multiCharacterData.sharedInventory?.items?.Count ?? 0;

            Debug.Log($"[ValidateSaveSuccess] Expected: {expectedItems}, Saved: {savedItems}");

            if (savedItems != expectedItems)
            {
                Debug.LogError($"[ValidateSaveSuccess] ❌ SAVE VALIDATION FAILED!");
                Debug.LogError($"  Expected items: {expectedItems}");
                Debug.LogError($"  Saved items: {savedItems}");
                Debug.LogError($"  Backup had: {backup.totalItems} items");

                // แสดง backup info เพื่อ debug
                Debug.Log($"[ValidateSaveSuccess] Backup items were:");
                foreach (var itemName in backup.itemNames)
                {
                    Debug.Log($"  - {itemName}");
                }

                // ลอง save อีกครั้ง
                Debug.Log($"[ValidateSaveSuccess] 🔄 Retrying save...");
                SaveInventoryData(character);
            }
            else
            {
                Debug.Log($"[ValidateSaveSuccess] ✅ Save validation successful");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ValidateSaveSuccess] ❌ Validation error: {e.Message}");
        }
    }
    public void SaveInventoryDataSafe(Character character, string action = "Manual Save")
    {
        if (character == null)
        {
            Debug.LogError("[SaveInventoryDataSafe] Character is null!");
            return;
        }

        if (multiCharacterData == null)
        {
            Debug.LogError("[SaveInventoryDataSafe] MultiCharacterData is null!");
            return;
        }

        try
        {
            var inventory = character.GetInventory();
            if (inventory == null)
            {
                Debug.LogError("[SaveInventoryDataSafe] Character has no inventory to save!");
                return;
            }

            int itemsInInventory = inventory.UsedSlots;
            Debug.Log($"[SaveInventoryDataSafe] 💾 Starting safe save for {character.CharacterName}");
            Debug.Log($"[SaveInventoryDataSafe] Current inventory: {itemsInInventory}/{inventory.CurrentSlots} slots (Action: {action})");

            // 🛡️ Double-check ป้องกันการ save inventory ว่าง
            if (itemsInInventory == 0 && action != "Clear Inventory" && action != "Emergency Clear")
            {
                bool hasFirebaseData = multiCharacterData.sharedInventory?.items?.Count > 0;
                if (hasFirebaseData)
                {
                    Debug.LogError("[SaveInventoryDataSafe] ❌ CRITICAL: Blocking save of empty inventory when Firebase has data!");
                    Debug.LogError($"[SaveInventoryDataSafe] Firebase has {multiCharacterData.sharedInventory.items.Count} items");

                    // แสดง Firebase items
                    Debug.Log("[SaveInventoryDataSafe] Firebase items:");
                    foreach (var item in multiCharacterData.sharedInventory.items)
                    {
                        Debug.Log($"  - {item.itemName} x{item.stackCount}");
                    }

                    return; // ไม่ save
                }
            }

            // Save ปกติ
            bool inventorySaved = SaveSharedInventoryDataSafe(character);
            bool equipmentSaved = SaveCharacterEquipmentData(character);

            if (inventorySaved || equipmentSaved)
            {
                multiCharacterData.UpdateAllInventoryDebugInfo();
                SavePlayerDataAsync();

                Debug.Log($"[SaveInventoryDataSafe] ✅ Safe save completed");
                Debug.Log($"  - Inventory saved: {inventorySaved}");
                Debug.Log($"  - Equipment saved: {equipmentSaved}");
            }
            else
            {
                Debug.LogWarning("[SaveInventoryDataSafe] ⚠️ No data was saved");
            }

        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveInventoryDataSafe] ❌ Error: {e.Message}");
        }
    }

    // 🆕 Safe version ของ LoadSingleInventoryItem
    private bool LoadSingleInventoryItemSafe(Inventory inventory, SavedInventoryItem savedItem)
    {
        try
        {
            // หา ItemData จาก ID
            ItemData itemData = GetItemDataById(savedItem.itemId);
            if (itemData == null)
            {
                Debug.LogError($"[LoadSingleInventoryItemSafe] Item not found: {savedItem.itemId} ({savedItem.itemName})");
                return false;
            }

            // ใช้ AddItem แทนการ set ตำแหน่งโดยตรง (ปลอดภัยกว่า)
            bool added = inventory.AddItem(itemData, savedItem.stackCount);

            if (added)
            {
                Debug.Log($"[LoadSingleInventoryItemSafe] ✅ Added {itemData.ItemName} x{savedItem.stackCount}");
            }
            else
            {
                Debug.LogWarning($"[LoadSingleInventoryItemSafe] ⚠️ Failed to add {itemData.ItemName}");
            }

            return added;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LoadSingleInventoryItemSafe] ❌ Error loading {savedItem.itemName}: {e.Message}");
            return false;
        }
    }

    // 🆕 เพิ่ม method สำหรับ backup inventory
    private List<InventoryItem> BackupCurrentInventory(Inventory inventory)
    {
        var backup = new List<InventoryItem>();

        for (int i = 0; i < inventory.CurrentSlots; i++)
        {
            var item = inventory.GetItem(i);
            if (item != null && !item.IsEmpty)
            {
                backup.Add(new InventoryItem(item.itemData, item.stackCount, item.slotIndex));
            }
        }

        Debug.Log($"[BackupCurrentInventory] Backed up {backup.Count} items");
        return backup;
    }

    // 🆕 เพิ่ม method สำหรับ restore inventory
    private void RestoreInventoryBackup(Inventory inventory, List<InventoryItem> backup)
    {
        if (backup == null || backup.Count == 0) return;

        Debug.Log($"[RestoreInventoryBackup] Restoring {backup.Count} items...");

        foreach (var item in backup)
        {
            if (item != null && !item.IsEmpty)
            {
                inventory.AddItem(item.itemData, item.stackCount);
            }
        }

        Debug.Log($"[RestoreInventoryBackup] ✅ Restored backup inventory");
    }

    /// <summary>
    /// โหลดไอเทมเดียวลง inventory
    /// </summary>
   


    /// <summary>
    /// โหลดข้อมูล equipment และ potion ของ character
    /// </summary>
    private bool LoadCharacterEquipmentData(Character character)
    {
        try
        {
            string characterType = multiCharacterData.currentActiveCharacter;
            Debug.Log($"[LoadCharacterEquipmentData] ⚔️ Loading equipment for {characterType} (Character: {character.CharacterName})");

            var characterProgressData = multiCharacterData.GetCharacterData(characterType);
            if (characterProgressData?.characterEquipment == null)
            {
                Debug.LogWarning($"[LoadCharacterEquipmentData] No equipment data for {characterType}");
                DebugShowAllCharacterEquipmentData();
                return false;
            }

            var equipmentData = characterProgressData.characterEquipment;
            if (!equipmentData.IsValid())
            {
                Debug.LogWarning($"[LoadCharacterEquipmentData] Invalid equipment data for {characterType}");
                return false;
            }

            // Debug ข้อมูลก่อน load
            Debug.Log($"[LoadCharacterEquipmentData] Equipment data found:");
            Debug.Log($"  - Head: {equipmentData.equipment.headItemId}");
            Debug.Log($"  - Armor: {equipmentData.equipment.armorItemId}");
            Debug.Log($"  - Weapon: {equipmentData.equipment.weaponItemId}");
            Debug.Log($"  - Pants: {equipmentData.equipment.pantsItemId}");
            Debug.Log($"  - Shoes: {equipmentData.equipment.shoesItemId}");
            Debug.Log($"  - Rune: {equipmentData.equipment.runeItemId}");
            Debug.Log($"  - Total Equipment: {equipmentData.equipment.equippedCount}");
            Debug.Log($"  - Total Potions: {equipmentData.totalPotionCount}");

            // 1. โหลด Equipment Slots (6 ช่อง)
            bool equipmentLoaded = LoadEquipmentSlots(character, equipmentData);

            // 2. โหลด Potion Slots (5 ช่อง)
            bool potionsLoaded = LoadPotionSlots(character, equipmentData);

            if (equipmentLoaded || potionsLoaded)
            {
                Debug.Log($"[LoadCharacterEquipmentData] ✅ Equipment loaded for {characterType}");
                Debug.Log($"[LoadCharacterEquipmentData] Equipment loaded: {equipmentLoaded}, Potions loaded: {potionsLoaded}");

                return true;
            }

            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LoadCharacterEquipmentData] ❌ Error: {e.Message}");
            return false;
        }
    }
  


    private void DebugShowAllCharacterEquipmentData()
    {
        Debug.Log("=== ALL CHARACTER EQUIPMENT DATA ===");

        if (multiCharacterData?.characters == null)
        {
            Debug.Log("No characters found");
            return;
        }

        foreach (var character in multiCharacterData.characters)
        {
            if (character != null)
            {
                bool hasEquipment = character.HasEquipmentData();
                Debug.Log($"Character: {character.characterType}");
                Debug.Log($"  - Has Equipment: {hasEquipment}");

                if (hasEquipment && character.characterEquipment != null)
                {
                    Debug.Log($"  - Equipment Count: {character.characterEquipment.equipment.equippedCount}");
                    Debug.Log($"  - Potion Count: {character.characterEquipment.totalPotionCount}");

                    // แสดงรายละเอียด equipment
                    var eq = character.characterEquipment.equipment;
                    if (!string.IsNullOrEmpty(eq.headItemId)) Debug.Log($"    Head: {eq.headItemId}");
                    if (!string.IsNullOrEmpty(eq.armorItemId)) Debug.Log($"    Armor: {eq.armorItemId}");
                    if (!string.IsNullOrEmpty(eq.weaponItemId)) Debug.Log($"    Weapon: {eq.weaponItemId}");
                    if (!string.IsNullOrEmpty(eq.pantsItemId)) Debug.Log($"    Pants: {eq.pantsItemId}");
                    if (!string.IsNullOrEmpty(eq.shoesItemId)) Debug.Log($"    Shoes: {eq.shoesItemId}");
                    if (!string.IsNullOrEmpty(eq.runeItemId)) Debug.Log($"    Rune: {eq.runeItemId}");
                }
            }
        }
        Debug.Log("===================================");
    }

    private System.Collections.IEnumerator ForceRefreshEquipmentUICoroutine(Character character)
    {
        Debug.Log("[ForceRefreshEquipmentUICoroutine] 🔄 Starting equipment UI refresh...");

        // รอ 2 frames เพื่อให้ equipment data settle
        yield return null;
        yield return null;

        try
        {
            int refreshCount = 0;

            // 1. หา และ refresh EquipmentSlotManager จาก character
            var equipmentSlotManager = character.GetComponent<EquipmentSlotManager>();
            if (equipmentSlotManager != null)
            {
                if (equipmentSlotManager.IsConnected())
                {
                    equipmentSlotManager.ForceRefreshFromCharacter();
                    refreshCount++;
                    Debug.Log("[ForceRefreshEquipmentUICoroutine] ✅ Character equipment slots refreshed");
                }
                else
                {
                    Debug.LogWarning("[ForceRefreshEquipmentUICoroutine] ⚠️ Character EquipmentSlotManager not connected");
                }
            }
            else
            {
                Debug.LogWarning("[ForceRefreshEquipmentUICoroutine] ⚠️ No EquipmentSlotManager on character");
            }

            // 2. หา และ refresh CombatUIManager equipment slots
            var combatUIManager = FindObjectOfType<CombatUIManager>();
            if (combatUIManager?.equipmentSlotManager != null)
            {
                if (combatUIManager.equipmentSlotManager.IsConnected())
                {
                    combatUIManager.equipmentSlotManager.ForceRefreshFromCharacter();
                    refreshCount++;
                    Debug.Log("[ForceRefreshEquipmentUICoroutine] ✅ CombatUI equipment slots refreshed");
                }
                else
                {
                    Debug.LogWarning("[ForceRefreshEquipmentUICoroutine] ⚠️ CombatUI EquipmentSlotManager not connected");
                }
            }
            else
            {
                Debug.LogWarning("[ForceRefreshEquipmentUICoroutine] ⚠️ No CombatUIManager or equipment slot manager found");
            }

            // 3. แจ้ง Character.OnStatsChanged event
            Character.RaiseOnStatsChanged();

            // 4. Force update Canvas
            Canvas.ForceUpdateCanvases();
            if (refreshCount > 0)
            {
                // ทำซ้ำอีกครั้งเพื่อให้แน่ใจ
                if (equipmentSlotManager != null && equipmentSlotManager.IsConnected())
                {
                    equipmentSlotManager.RefreshAllSlots();
                }

                if (combatUIManager?.equipmentSlotManager != null && combatUIManager.equipmentSlotManager.IsConnected())
                {
                    combatUIManager.equipmentSlotManager.RefreshAllSlots();
                }

                Canvas.ForceUpdateCanvases();

                Debug.Log($"[ForceRefreshEquipmentUICoroutine] ✅ Equipment UI refresh complete ({refreshCount} managers refreshed)");
            }
            else
            {
                Debug.LogError("[ForceRefreshEquipmentUICoroutine] ❌ No equipment managers were refreshed!");

                // ลอง debug สถานะ managers
                DebugEquipmentManagerStatus(character);
            }

        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ForceRefreshEquipmentUICoroutine] ❌ Error: {e.Message}");
        }// รออีก 1 frame แล้วทำซ้ำเพื่อให้แน่ใจ
        yield return null;



    }

    private void DebugEquipmentManagerStatus(Character character)
    {
        Debug.Log("=== EQUIPMENT MANAGER STATUS DEBUG ===");

        // Character EquipmentSlotManager
        var charEquipmentManager = character.GetComponent<EquipmentSlotManager>();
        Debug.Log($"Character EquipmentSlotManager: {(charEquipmentManager != null ? "Found" : "Not Found")}");
        if (charEquipmentManager != null)
        {
            Debug.Log($"  - Is Connected: {charEquipmentManager.IsConnected()}");
        }

        // CombatUIManager
        var combatUIManager = FindObjectOfType<CombatUIManager>();
        Debug.Log($"CombatUIManager: {(combatUIManager != null ? "Found" : "Not Found")}");
        if (combatUIManager != null)
        {
            Debug.Log($"  - Has Equipment Manager: {(combatUIManager.equipmentSlotManager != null)}");
            if (combatUIManager.equipmentSlotManager != null)
            {
                Debug.Log($"  - Equipment Manager Connected: {combatUIManager.equipmentSlotManager.IsConnected()}");
            }
        }

        // ทุก EquipmentSlotManager ใน scene
        var allEquipmentManagers = FindObjectsOfType<EquipmentSlotManager>();
        Debug.Log($"Total EquipmentSlotManagers in scene: {allEquipmentManagers.Length}");
        for (int i = 0; i < allEquipmentManagers.Length; i++)
        {
            var manager = allEquipmentManagers[i];
            Debug.Log($"  Manager {i}: {manager.gameObject.name} - Connected: {manager.IsConnected()}");
        }

        Debug.Log("=====================================");
    }
    
    // 🆕 เพิ่ม Debug & Logging Methods สำหรับ Load ใน PersistentPlayerData class
    public void FixSplitCharacterData()
    {
        if (multiCharacterData?.characters == null || multiCharacterData.characters.Count < 2)
        {
            Debug.Log("[FixSplitCharacterData] No split data to fix");
            return;
        }

        Debug.Log("[FixSplitCharacterData] 🔧 Attempting to fix split character data...");

        CharacterProgressData statsChar = null;
        CharacterProgressData equipmentChar = null;

        // หา character ที่มี stats และ character ที่มี equipment
        foreach (var character in multiCharacterData.characters)
        {
            if (character != null)
            {
                bool hasStats = character.totalMaxHp > 0;
                bool hasEquipment = character.HasEquipmentData();

                if (hasStats && !hasEquipment)
                {
                    statsChar = character;
                    Debug.Log($"[FixSplitCharacterData] Found stats character: {character.characterType}");
                }
                else if (!hasStats && hasEquipment)
                {
                    equipmentChar = character;
                    Debug.Log($"[FixSplitCharacterData] Found equipment character: {character.characterType}");
                }
            }
        }

        // รวมข้อมูล
        if (statsChar != null && equipmentChar != null)
        {
            Debug.Log("[FixSplitCharacterData] 🔧 Merging character data...");

            // คัดลอกข้อมูล equipment จาก equipmentChar ไป statsChar
            statsChar.characterEquipment = equipmentChar.characterEquipment;
            statsChar.hasEquipmentData = equipmentChar.hasEquipmentData;
            statsChar.equipmentLastSaveTime = equipmentChar.equipmentLastSaveTime;
            statsChar.totalEquippedItems = equipmentChar.totalEquippedItems;
            statsChar.totalPotions = equipmentChar.totalPotions;

            // ตั้งค่า characterType ให้ถูกต้อง
            string correctCharacterType = multiCharacterData.currentActiveCharacter;
            statsChar.characterType = correctCharacterType;
            statsChar.characterEquipment.characterType = correctCharacterType;

            // ลบ equipmentChar ออก
            multiCharacterData.characters.Remove(equipmentChar);

            Debug.Log($"[FixSplitCharacterData] ✅ Merged data successfully! Active character: {correctCharacterType}");

            // บันทึกข้อมูลที่แก้ไขแล้ว
            SavePlayerDataAsync();

            LogCharacterDataStatus();
        }
        else
        {
            Debug.Log("[FixSplitCharacterData] ❌ Could not find split data to merge");
        }
    }
    #region 🆕 Load Debug & Logging Methods

    /// <summary>
    /// แสดงรายการไอเทมที่โหลดใน inventory
    /// </summary>
  

    /// <summary>
    /// แสดงรายการอุปกรณ์และยาที่โหลด
    /// </summary>
    private void LogLoadedEquipmentData(CharacterEquipmentData equipmentData)
    {
        if (equipmentData == null)
        {
            Debug.Log("[LogLoadedEquipmentData] No equipment data to log");
            return;
        }

        Debug.Log($"=== LOADED EQUIPMENT DATA ({equipmentData.characterType}) ===");

        // Equipment slots
        int equippedCount = 0;
        if (!string.IsNullOrEmpty(equipmentData.equipment.headItemId))
        {
            Debug.Log($"Head: {equipmentData.equipment.headItemId}");
            equippedCount++;
        }
        if (!string.IsNullOrEmpty(equipmentData.equipment.armorItemId))
        {
            Debug.Log($"Armor: {equipmentData.equipment.armorItemId}");
            equippedCount++;
        }
        if (!string.IsNullOrEmpty(equipmentData.equipment.weaponItemId))
        {
            Debug.Log($"Weapon: {equipmentData.equipment.weaponItemId}");
            equippedCount++;
        }
        if (!string.IsNullOrEmpty(equipmentData.equipment.pantsItemId))
        {
            Debug.Log($"Pants: {equipmentData.equipment.pantsItemId}");
            equippedCount++;
        }
        if (!string.IsNullOrEmpty(equipmentData.equipment.shoesItemId))
        {
            Debug.Log($"Shoes: {equipmentData.equipment.shoesItemId}");
            equippedCount++;
        }
        if (!string.IsNullOrEmpty(equipmentData.equipment.runeItemId))
        {
            Debug.Log($"Rune: {equipmentData.equipment.runeItemId}");
            equippedCount++;
        }

        // Potion slots
        int potionCount = 0;
        for (int i = 0; i < equipmentData.potionSlots.Count; i++)
        {
            var potion = equipmentData.potionSlots[i];
            if (!potion.IsEmpty())
            {
                Debug.Log($"Potion {i}: {potion.itemName} x{potion.stackCount} - ID: {potion.itemId}");
                potionCount++;
            }
        }

        Debug.Log($"Equipment loaded: {equippedCount}/6, Potions loaded: {potionCount}/5");
        Debug.Log("===============================================");
    }

    /// <summary>
    /// แสดงสถิติการโหลดทั้งหมด
    /// </summary>
    public void LogLoadStats()
    {
        if (multiCharacterData == null)
        {
            Debug.Log("[LogLoadStats] No data to show");
            return;
        }

        Debug.Log("=== INVENTORY LOAD STATS ===");
        Debug.Log($"Player: {multiCharacterData.playerName}");
        Debug.Log($"Has Inventory Data: {multiCharacterData.HasInventoryData()}");
        Debug.Log($"Has Any Data: {multiCharacterData.HasAnyInventoryOrEquipmentData()}");
        Debug.Log($"Shared Items: {multiCharacterData.sharedInventory?.items?.Count ?? 0}");

        foreach (var character in multiCharacterData.characters)
        {
            if (character?.characterEquipment != null)
            {
                bool hasEquipment = character.HasEquipmentData();
                Debug.Log($"{character.characterType}: Has Data={hasEquipment}, Eq={character.totalEquippedItems}, Pot={character.totalPotions}");
            }
        }
        Debug.Log("===========================");
    }

    /// <summary>
    /// ตรวจสอบสถานะ ItemDatabase
    /// </summary>
    public void LogItemDatabaseStatus()
    {
        Debug.Log("=== ITEM DATABASE STATUS ===");

        try
        {
            var database = ItemDatabase.Instance;
            if (database == null)
            {
                Debug.LogError("❌ ItemDatabase.Instance is null!");
                return;
            }

            var allItems = database.GetAllItems();
            if (allItems == null)
            {
                Debug.LogError("❌ GetAllItems() returned null!");
                return;
            }

            Debug.Log($"✅ ItemDatabase found with {allItems.Count} items");

            // แสดงสถิติตาม item type
            var typeCounts = new Dictionary<ItemType, int>();
            foreach (var item in allItems)
            {
                if (item != null)
                {
                    if (!typeCounts.ContainsKey(item.ItemType))
                        typeCounts[item.ItemType] = 0;
                    typeCounts[item.ItemType]++;
                }
            }

            foreach (var kvp in typeCounts)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value} items");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error checking ItemDatabase: {e.Message}");
        }

        Debug.Log("============================");
    }

    /// <summary>
    /// ทดสอบการหา item โดยใช้ ID
    /// </summary>
    public void TestItemLookup(string itemId)
    {
        Debug.Log($"=== TESTING ITEM LOOKUP: {itemId} ===");

        var itemData = GetItemDataById(itemId);
        if (itemData != null)
        {
            Debug.Log($"✅ Found: {itemData.ItemName} ({itemData.ItemType}, {itemData.GetTierText()})");
            Debug.Log($"   ID: {itemData.ItemId}");
            Debug.Log($"   Stackable: {itemData.CanStack()}, Max Stack: {itemData.MaxStackSize}");
        }
        else
        {
            Debug.LogError($"❌ Item not found: {itemId}");

            // Debug: แสดงรายการ ID ทั้งหมดใน database
            LogAllItemIds();
        }

        Debug.Log("=======================================");
    }

    /// <summary>
    /// แสดงรายการ ItemID ทั้งหมดใน database (สำหรับ debug)
    /// </summary>
    private void LogAllItemIds()
    {
        try
        {
            var database = ItemDatabase.Instance;
            if (database?.GetAllItems() == null) return;

            Debug.Log("=== ALL ITEM IDs IN DATABASE ===");
            int count = 0;
            foreach (var item in database.GetAllItems())
            {
                if (item != null)
                {
                    Debug.Log($"{count}: {item.ItemId} - {item.ItemName} ({item.ItemType})");
                    count++;

                    // จำกัดแสดงแค่ 20 ตัวแรก เพื่อไม่ให้ log ยาวเกินไป
                    if (count >= 20)
                    {
                        Debug.Log($"... and {database.GetAllItems().Count - count} more items");
                        break;
                    }
                }
            }
            Debug.Log("================================");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error logging item IDs: {e.Message}");
        }
    }

    /// <summary>
    /// แสดงข้อมูลการโหลดสำหรับ character ปัจจุบัน
    /// </summary>
    public void LogCurrentCharacterLoadStatus(Character character)
    {
        if (character == null)
        {
            Debug.Log("[LogCurrentCharacterLoadStatus] Character is null");
            return;
        }

        Debug.Log($"=== LOAD STATUS: {character.CharacterName} ===");

        // Inventory status
        var inventory = character.GetInventory();
        if (inventory != null)
        {
            Debug.Log($"Inventory: {inventory.UsedSlots}/{inventory.CurrentSlots} slots used");
            Debug.Log($"Grid: {inventory.GridWidth}x{inventory.GridHeight}");
        }
        else
        {
            Debug.Log("❌ No inventory found");
        }

        // Equipment status
        var equippedItems = character.GetAllEquippedItems();
        Debug.Log($"Equipped items: {equippedItems.Count}");

        // Potion status
        int potionCount = 0;
        for (int i = 0; i < 5; i++)
        {
            var potion = character.GetPotionInSlot(i);
            if (potion != null)
            {
                int stackCount = character.GetPotionStackCount(i);
                Debug.Log($"Potion {i}: {potion.ItemName} x{stackCount}");
                potionCount++;
            }
        }

        if (potionCount == 0)
        {
            Debug.Log("No potions equipped");
        }

        Debug.Log("=====================================");
    }

    #endregion
    /// <summary>
    /// โหลด equipment slots (6 ช่อง)
    /// </summary>
    private bool LoadEquipmentSlots(Character character, CharacterEquipmentData equipmentData)
    {
        try
        {
            int loadedCount = 0;
            var equipment = equipmentData.equipment;

            Debug.Log("[LoadEquipmentSlots] Loading 6 equipment slots...");

            // เคลียร์ equipment ก่อน
            character.ClearAllEquipmentForLoad();

            // โหลดแต่ละ slot
            if (LoadSingleEquipmentSlot(character, ItemType.Head, equipment.headItemId))
                loadedCount++;
            if (LoadSingleEquipmentSlot(character, ItemType.Armor, equipment.armorItemId))
                loadedCount++;
            if (LoadSingleEquipmentSlot(character, ItemType.Weapon, equipment.weaponItemId))
                loadedCount++;
            if (LoadSingleEquipmentSlot(character, ItemType.Pants, equipment.pantsItemId))
                loadedCount++;
            if (LoadSingleEquipmentSlot(character, ItemType.Shoes, equipment.shoesItemId))
                loadedCount++;
            if (LoadSingleEquipmentSlot(character, ItemType.Rune, equipment.runeItemId))
                loadedCount++;

            // ✅ Apply stats และ refresh UI หลัง load equipment ทั้งหมด
            if (loadedCount > 0)
            {
                character.ApplyLoadedEquipmentStats();
                character.ForceRefreshEquipmentAfterLoad();
            }

            Debug.Log($"[LoadEquipmentSlots] ✅ Loaded {loadedCount}/6 equipment pieces");
            return loadedCount > 0;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LoadEquipmentSlots] ❌ Error: {e.Message}");
            return false;
        }
    }

    private bool LoadSingleEquipmentSlot(Character character, ItemType itemType, string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            Debug.Log($"[LoadSingleEquipmentSlot] No item to load for {itemType}");
            return false;
        }

        try
        {
            // หา ItemData จาก ID
            ItemData itemData = GetItemDataById(itemId);
            if (itemData == null)
            {
                Debug.LogError($"[LoadSingleEquipmentSlot] Item not found: {itemId} for {itemType}");
                return false;
            }

            // ตรวจสอบว่า item type ตรงกันหรือไม่
            if (itemData.ItemType != itemType)
            {
                Debug.LogError($"[LoadSingleEquipmentSlot] Item type mismatch: {itemData.ItemType} != {itemType} for {itemData.ItemName}");
                return false;
            }

            // โหลด item ลง character
            bool success = character.LoadEquipmentDirectly(itemData);
            if (success)
            {
                Debug.Log($"[LoadSingleEquipmentSlot] ✅ Loaded {itemData.ItemName} to {itemType} slot");
                return true;
            }
            else
            {
                Debug.LogError($"[LoadSingleEquipmentSlot] Failed to load {itemData.ItemName} to {itemType} slot");
                return false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LoadSingleEquipmentSlot] ❌ Error loading {itemType} item {itemId}: {e.Message}");
            return false;
        }
    }

   
    /// <summary>
    /// โหลด equipment slot เดียว
    /// </summary>
   

   

    private int GetSlotIndexForItemType(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Head: return 0;
            case ItemType.Armor: return 1;
            case ItemType.Weapon: return 2;
            case ItemType.Pants: return 3;
            case ItemType.Shoes: return 4;
            case ItemType.Rune: return 5;
            default: return -1;
        }
    }
    /// </summary>
    private bool LoadPotionSlots(Character character, CharacterEquipmentData equipmentData)
    {
        try
        {
            int loadedCount = 0;

            Debug.Log("[LoadPotionSlots] Loading 5 potion slots...");

            // 🔧 แก้ไข: ตรวจสอบ potion ที่มีอยู่แล้วอย่างละเอียด
            for (int i = 0; i < 5; i++)
            {
                var potionSlot = equipmentData.GetPotionSlot(i);
                if (potionSlot == null || potionSlot.IsEmpty())
                {
                    Debug.Log($"[LoadPotionSlots] Slot {i}: No potion data to load");
                    continue;
                }

                // ตรวจสอบว่ามี potion ใน character แล้วหรือไม่
                ItemData existingPotion = character.GetPotionInSlot(i);
                if (existingPotion != null)
                {
                    int existingStack = character.GetPotionStackCount(i);

                    if (existingPotion.ItemId == potionSlot.itemId)
                    {
                        Debug.LogWarning($"[LoadPotionSlots] ⚠️ DUPLICATE DETECTED!");
                        Debug.LogWarning($"[LoadPotionSlots] Slot {i} already has {existingPotion.ItemName} x{existingStack}");
                        Debug.LogWarning($"[LoadPotionSlots] Firebase wants to load {potionSlot.itemName} x{potionSlot.stackCount}");

                        // 🔧 ถ้าจำนวนใน character น้อยกว่า Firebase ให้ใช้ค่าจาก Firebase
                        if (existingStack < potionSlot.stackCount)
                        {
                            Debug.LogWarning($"[LoadPotionSlots] 🔄 Updating stack count: {existingStack} → {potionSlot.stackCount}");
                            character.SetPotionStackCount(i, potionSlot.stackCount);
                            loadedCount++;
                        }
                        else
                        {
                            Debug.LogWarning($"[LoadPotionSlots] ❌ Skipping load to prevent duplication");
                        }
                        continue;
                    }
                    else
                    {
                        Debug.LogWarning($"[LoadPotionSlots] Different potion in slot {i}: {existingPotion.ItemName} vs {potionSlot.itemName}");
                        // เคลียร์ slot เก่าก่อน
                        character.potionSlots[i] = null;
                        character.SetPotionStackCount(i, 0);
                    }
                }

                // โหลด potion ใหม่
                bool loaded = LoadSinglePotionSlot(character, i, potionSlot);
                if (loaded)
                    loadedCount++;
            }

            Debug.Log($"[LoadPotionSlots] ✅ Loaded {loadedCount}/5 potion slots (prevented duplicates)");
            return loadedCount > 0;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LoadPotionSlots] ❌ Error: {e.Message}");
            return false;
        }
    }
   

    /// <summary>
    /// โหลด potion slots (5 ช่อง)
    /// </summary>


    /// <summary>
    /// โหลด potion slot เดียว
    /// </summary>
    private bool LoadSinglePotionSlot(Character character, int slotIndex, SavedPotionSlot potionSlot)
    {
        try
        {
            // หา ItemData จาก ID
            ItemData itemData = GetItemDataById(potionSlot.itemId);
            if (itemData == null)
            {
                Debug.LogError($"[LoadSinglePotionSlot] Potion not found: {potionSlot.itemId} ({potionSlot.itemName})");
                return false;
            }

            // ตรวจสอบว่าเป็น potion หรือไม่
            if (itemData.ItemType != ItemType.Potion)
            {
                Debug.LogError($"[LoadSinglePotionSlot] Item is not a potion: {itemData.ItemName} ({itemData.ItemType})");
                return false;
            }

            // โหลด potion ลง character
            bool success = character.LoadPotionDirectly(itemData, slotIndex, potionSlot.stackCount);

            if (success)
            {
                Debug.Log($"[LoadSinglePotionSlot] ✅ Loaded {itemData.ItemName} x{potionSlot.stackCount} to potion slot {slotIndex}");
                return true;
            }
            else
            {
                Debug.LogError($"[LoadSinglePotionSlot] Failed to load {itemData.ItemName}");
                return false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LoadSinglePotionSlot] ❌ Error loading potion slot {slotIndex}: {e.Message}");
            return false;
        }
    }
    private void UpdatePotionStackCountsAfterLoad(Character character, CharacterEquipmentData equipmentData)
    {
        try
        {
            Debug.Log("[UpdatePotionStackCountsAfterLoad] Updating potion stack counts...");

            for (int i = 0; i < 5; i++)
            {
                var savedPotionSlot = equipmentData.GetPotionSlot(i);
                if (savedPotionSlot != null && !savedPotionSlot.IsEmpty())
                {
                    // หา potion ที่ equip แล้วใน character และ update stack count
                    var currentPotion = character.GetPotionInSlot(i);
                    if (currentPotion != null && currentPotion.ItemId == savedPotionSlot.itemId)
                    {
                        character.SetPotionStackCount(i, savedPotionSlot.stackCount);
                        Debug.Log($"[UpdatePotionStackCountsAfterLoad] Updated slot {i} stack count to {savedPotionSlot.stackCount}");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UpdatePotionStackCountsAfterLoad] ❌ Error: {e.Message}");
        }
    }

    /// <summary>
    /// ใส่ potion ลง slot โดยตรง (สำหรับ load data)
    /// </summary>
    private bool SetPotionSlotDirectly(Character character, int slotIndex, ItemData potionData, int stackCount)
    {
        try
        {
            // วิธีง่ายๆ: ใช้ EquipItemData แต่ต้องทำหลังจากที่เคลียร์ potion slots ทั้งหมดแล้ว
            // แล้วใส่ potion ตาม order ที่ต้องการ

            Debug.Log($"[SetPotionSlotDirectly] Trying to set potion {potionData.ItemName} x{stackCount} to slot {slotIndex}");

            // ใช้ method ที่มีอยู่แล้ว
            bool success = character.EquipItemData(potionData);

            if (success)
            {
                // ตั้งค่า stack count โดยใช้ method ที่มีอยู่
                character.SetPotionStackCount(slotIndex, stackCount);
                Debug.Log($"[SetPotionSlotDirectly] ✅ Set potion slot {slotIndex}: {potionData.ItemName} x{stackCount}");
                return true;
            }

            Debug.LogWarning($"[SetPotionSlotDirectly] Failed to equip {potionData.ItemName}");
            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SetPotionSlotDirectly] ❌ Error: {e.Message}");
            return false;
        }
    }

    #endregion

    #region 🆕 Item Database Helper Methods

    /// <summary>
    /// หา ItemData จาก ItemID โดยใช้ ItemDatabase
    /// </summary>
    private ItemData GetItemDataById(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return null;

        try
        {
            // ใช้ ItemDatabase.Instance ค้นหา item
            var database = ItemDatabase.Instance;
            if (database == null)
            {
                Debug.LogError("[GetItemDataById] ItemDatabase.Instance is null!");
                return null;
            }

            var allItems = database.GetAllItems();
            if (allItems == null || allItems.Count == 0)
            {
                Debug.LogError("[GetItemDataById] ItemDatabase has no items!");
                return null;
            }

            // ค้นหา item ที่มี ID ตรงกัน
            foreach (var item in allItems)
            {
                if (item != null && item.ItemId == itemId)
                {
                    return item;
                }
            }

            Debug.LogWarning($"[GetItemDataById] Item not found in database: {itemId}");
            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GetItemDataById] ❌ Error searching for item {itemId}: {e.Message}");
            return null;
        }
    }


    /// <summary>
    /// ตรวจสอบว่ามีข้อมูล inventory ใน Firebase หรือไม่
    /// </summary>
    public bool HasInventoryDataInFirebase()
    {
        return multiCharacterData != null &&
               multiCharacterData.HasAnyInventoryOrEquipmentData();
    }
    public bool ShouldLoadFromFirebase()
    {
        // ตรวจสอบว่ามี data ใน multiCharacterData หรือไม่
        if (multiCharacterData == null) return false;

        // ตรวจสอบ shared inventory
        bool hasSharedInventory = multiCharacterData.sharedInventory != null &&
                                 multiCharacterData.sharedInventory.items != null &&
                                 multiCharacterData.sharedInventory.items.Count > 0;

        // ตรวจสอบ character equipment
        bool hasAnyEquipment = false;
        if (multiCharacterData.characters != null)
        {
            foreach (var character in multiCharacterData.characters)
            {
                if (character?.HasEquipmentData() == true)
                {
                    hasAnyEquipment = true;
                    break;
                }
            }
        }

        bool shouldLoad = hasSharedInventory || hasAnyEquipment;

        Debug.Log($"[ShouldLoadFromFirebase] SharedInventory: {hasSharedInventory}, Equipment: {hasAnyEquipment}, Result: {shouldLoad}");

        return shouldLoad;
    }

    #endregion

    #region 🆕 UI Refresh Methods

    /// <summary>
    /// Force refresh ทุก UI เกี่ยวกับ inventory หลัง load เสร็จ
    /// </summary>
    private void ForceRefreshInventoryUI(Character character)
    {
        try
        {
            Debug.Log("[ForceRefreshInventoryUI] 🔄 Force refreshing all inventory UI...");

            // 1. หา InventoryGridManager และ refresh
            var inventoryGridManager = FindObjectOfType<InventoryGridManager>();
            if (inventoryGridManager != null)
            {
                inventoryGridManager.ForceUpdateFromCharacter();
                Debug.Log("[ForceRefreshInventoryUI] ✅ Inventory grid refreshed");
            }

            // 2. หา EquipmentSlotManager และ refresh  
            var equipmentSlotManager = character.GetComponent<EquipmentSlotManager>();
            if (equipmentSlotManager != null && equipmentSlotManager.IsConnected())
            {
                equipmentSlotManager.ForceRefreshFromCharacter();
                Debug.Log("[ForceRefreshInventoryUI] ✅ Equipment slots refreshed");
            }

            // 3. หา CombatUIManager และ refresh equipment slots
            var combatUIManager = FindObjectOfType<CombatUIManager>();
            if (combatUIManager?.equipmentSlotManager != null)
            {
                combatUIManager.equipmentSlotManager.ForceRefreshFromCharacter();
                Debug.Log("[ForceRefreshInventoryUI] ✅ CombatUI equipment slots refreshed");
            }

            // 4. แจ้ง Character.OnStatsChanged event
            Character.RaiseOnStatsChanged();

            // 5. Force update Canvas
            Canvas.ForceUpdateCanvases();

            Debug.Log("[ForceRefreshInventoryUI] ✅ All inventory UI refreshed");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ForceRefreshInventoryUI] ❌ Error: {e.Message}");
        }
    }

    private void ForceRefreshUISimple(Character character)
    {
        try
        {
            // 1. หา InventoryGridManager และ refresh
            var inventoryGridManager = FindObjectOfType<InventoryGridManager>();
            if (inventoryGridManager != null)
            {
                inventoryGridManager.ForceUpdateFromCharacter();
                Debug.Log("[ForceRefreshUISimple] ✅ Inventory grid refreshed");
            }

            // 2. หา EquipmentSlotManager และ refresh  
            var equipmentSlotManager = character.GetComponent<EquipmentSlotManager>();
            if (equipmentSlotManager != null)
            {
                equipmentSlotManager.ForceRefreshFromCharacter();
                Debug.Log("[ForceRefreshUISimple] ✅ Equipment slots refreshed");
            }

            // 3. แจ้ง Character.OnStatsChanged event
            Character.RaiseOnStatsChanged();

            Debug.Log("[ForceRefreshUISimple] ✅ All inventory UI refreshed");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ForceRefreshUISimple] ❌ Error: {e.Message}");
        }
    }

    /// <summary>
    /// Coroutine สำหรับ refresh UI หลัง load
    /// </summary>
    private IEnumerator ForceRefreshUICoroutine(Character character)
    {
        // รอ 2 frames เพื่อให้ระบบต่างๆ พร้อม
        yield return null;
        yield return null;

        try
        {
            // 1. หา InventoryGridManager และ refresh
            var inventoryGridManager = FindObjectOfType<InventoryGridManager>();
            if (inventoryGridManager != null)
            {
                inventoryGridManager.ForceUpdateFromCharacter();
                inventoryGridManager.ForceSyncAllSlots();
                Debug.Log("[ForceRefreshUICoroutine] ✅ Inventory grid refreshed");
            }

            // 2. หา EquipmentSlotManager และ refresh  
            var equipmentSlotManager = character.GetComponent<EquipmentSlotManager>();
            if (equipmentSlotManager != null)
            {
                equipmentSlotManager.ForceRefreshFromCharacter();
                Debug.Log("[ForceRefreshUICoroutine] ✅ Equipment slots refreshed");
            }
            else
            {
                // หาจาก CombatUIManager
                var combatUIManager = FindObjectOfType<CombatUIManager>();
                if (combatUIManager?.equipmentSlotManager != null)
                {
                    combatUIManager.equipmentSlotManager.ForceRefreshFromCharacter();
                    Debug.Log("[ForceRefreshUICoroutine] ✅ Equipment slots refreshed (via CombatUIManager)");
                }
            }

            // 3. แจ้ง Character.OnStatsChanged event
            Character.RaiseOnStatsChanged();

            // 4. Force update Canvas
            Canvas.ForceUpdateCanvases();

            Debug.Log("[ForceRefreshUICoroutine] ✅ All inventory UI refreshed");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ForceRefreshUICoroutine] ❌ Error: {e.Message}");
        }
    }

    /// <summary>
    /// ให้ starter items ถ้าจำเป็น (ไม่มีข้อมูลใน Firebase)
    /// </summary>
    private void GiveStarterItemsIfNeeded(Character character)
    {
        try
        {
            Debug.Log("[GiveStarterItemsIfNeeded] 🎁 No saved data found, will use default starter items system");

            // ไม่ต้องเรียก reflection แค่ log ไว้
            // starter items จะถูกให้โดย Inventory.Start() method ตามปกติ
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GiveStarterItemsIfNeeded] ❌ Error: {e.Message}");
        }
    }

    #endregion

    #region 🆕 Reflection Helper Methods (สำหรับเข้าถึง private fields)

    /// <summary>
    /// ดึง potion slots list จาก Character (private field)
    /// </summary>
    private List<ItemData> GetCharacterPotionSlots(Character character)
    {
        try
        {
            var field = character.GetType().GetField("potionSlots",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(character) as List<ItemData>;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// ดึง potion stack counts list จาก Character (private field)
    /// </summary>
    private List<int> GetCharacterPotionStackCounts(Character character)
    {
        try
        {
            var field = character.GetType().GetField("potionStackCounts",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(character) as List<int>;
        }
        catch
        {
            return null;
        }
    }

    #endregion
    public CharacterProgressData GetOrCreateCharacterData(string characterType)
    {
        CharacterProgressData existing = GetCharacterData(characterType);
        if (existing != null)
            return existing;

        CharacterProgressData newCharacter = CreateDefaultCharacterData(characterType);
        multiCharacterData.characters.Add(newCharacter);
        return newCharacter;
    }

    public CharacterProgressData CreateDefaultCharacterData(string characterType)
    {
        CharacterProgressData newCharacter = new CharacterProgressData(characterType);
        newCharacter.currentLevel = 1;
        newCharacter.currentExp = 0;
        newCharacter.expToNextLevel = 100;

        CharacterStats characterStats = null;

        switch (characterType)
        {
            case "BloodKnight":
                characterStats = Resources.Load<CharacterStats>("Characters/BloodKnightStats");
                break;
            case "Archer":
                characterStats = Resources.Load<CharacterStats>("Characters/ArcherStats");
                break;
            case "Assassin":
                characterStats = Resources.Load<CharacterStats>("Characters/AssassinStats");
                break;
            case "IronJuggernaut":
                characterStats = Resources.Load<CharacterStats>("Characters/IronJuggernautStats");
                break;
        }

        if (characterStats != null)
        {
            newCharacter.totalMaxHp = characterStats.maxHp;
            newCharacter.totalMaxMana = characterStats.maxMana;
            newCharacter.totalAttackDamage = characterStats.attackDamage;
            newCharacter.totalMagicDamage = characterStats.magicDamage;
            newCharacter.totalArmor = characterStats.arrmor;
            newCharacter.totalCriticalChance = characterStats.criticalChance;
            newCharacter.totalCriticalDamageBonus = characterStats.criticalDamageBonus;
            newCharacter.totalMoveSpeed = characterStats.moveSpeed;
            newCharacter.totalAttackRange = characterStats.attackRange;
            newCharacter.totalAttackCooldown = characterStats.attackCoolDown;
            newCharacter.totalHitRate = characterStats.hitRate;
            newCharacter.totalEvasionRate = characterStats.evasionRate;
            newCharacter.totalAttackSpeed = characterStats.attackSpeed;
            newCharacter.totalReductionCoolDown = characterStats.reductionCoolDown;

            Debug.Log($"✅ Created {characterType} with stats from ScriptableObject");
        }
        return newCharacter;
    }
    #region 🆕 Currency Save/Load Methods

    /// <summary>
    /// บันทึกข้อมูลเงินและเพชรทั้งหมด
    /// </summary>
    /// 



    public void SaveCurrencyData()
    {
        if (multiCharacterData == null)
        {
            Debug.LogError("[SaveCurrencyData] MultiCharacterData is null!");
            return;
        }

        try
        {
            Debug.Log($"[SaveCurrencyData] 💰 Saving currency data...");
            Debug.Log($"  Gold: {multiCharacterData.sharedCurrency.gold}");
            Debug.Log($"  Gems: {multiCharacterData.sharedCurrency.gems}");

            // อัปเดตข้อมูล debug
            multiCharacterData.UpdateCurrencyDebugInfo();

            // Auto save to Firebase
            SavePlayerDataAsync();

            Debug.Log("[SaveCurrencyData] ✅ Currency data saved successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveCurrencyData] ❌ Error: {e.Message}");
        }
    }

    /// <summary>
    /// โหลดข้อมูลเงินและเพชร
    /// </summary>
    public void LoadCurrencyData()
    {
        if (multiCharacterData == null)
        {
            Debug.LogError("[LoadCurrencyData] MultiCharacterData is null!");
            return;
        }

        try
        {
            Debug.Log("[LoadCurrencyData] 💰 Loading currency data...");

            if (multiCharacterData.sharedCurrency == null)
            {
                Debug.LogWarning("[LoadCurrencyData] No currency data found, creating default");
                multiCharacterData.sharedCurrency = new SharedCurrencyData();
            }

            if (multiCharacterData.sharedCurrency.IsValid())
            {
                Debug.Log($"[LoadCurrencyData] ✅ Currency loaded - Gold: {multiCharacterData.sharedCurrency.gold}, Gems: {multiCharacterData.sharedCurrency.gems}");

                // บันทึกลง PlayerPrefs เป็น backup
                SaveCurrencyToPlayerPrefs();
            }
            else
            {
                Debug.LogWarning("[LoadCurrencyData] Invalid currency data, using defaults");
                multiCharacterData.sharedCurrency = new SharedCurrencyData();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LoadCurrencyData] ❌ Error: {e.Message}");
            // ใช้ default ถ้า error
            multiCharacterData.sharedCurrency = new SharedCurrencyData();
        }
    }

    /// <summary>
    /// บันทึกข้อมูลเงินลง PlayerPrefs (เป็น backup)
    /// </summary>
    private void SaveCurrencyToPlayerPrefs()
    {
        try
        {
            if (multiCharacterData?.sharedCurrency != null)
            {
                PlayerPrefs.SetString("PlayerGold", multiCharacterData.sharedCurrency.gold.ToString());
                PlayerPrefs.SetInt("PlayerGems", multiCharacterData.sharedCurrency.gems);
                PlayerPrefs.SetString("CurrencyLastSave", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                PlayerPrefs.Save();

                Debug.Log("[SaveCurrencyToPlayerPrefs] ✅ Currency backup saved to PlayerPrefs");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveCurrencyToPlayerPrefs] ❌ Error: {e.Message}");
        }
    }
    public void SaveBaseStats(Character character, LevelManager levelManager)
    {
        if (multiCharacterData == null || character == null || levelManager == null)
        {
            Debug.LogError("[PersistentPlayerData] Cannot save base stats - missing components");
            return;
        }

        try
        {
            string characterType = multiCharacterData.currentActiveCharacter;
            var characterData = GetOrCreateCharacterData(characterType);

            // บันทึก level และ exp
            characterData.currentLevel = levelManager.CurrentLevel;
            characterData.currentExp = levelManager.CurrentExp;
            characterData.expToNextLevel = levelManager.ExpToNextLevel;

            Debug.Log($"[PersistentPlayerData] 💾 Saving base stats for {characterType} Level {levelManager.CurrentLevel}");

            // 🆕 คำนวณ base stats (ScriptableObject + Level bonuses)
            if (character.characterStats != null)
            {
                int levelBonusHp = (levelManager.CurrentLevel - 1) * levelManager.levelUpStats.hpBonusPerLevel;
                int levelBonusMana = (levelManager.CurrentLevel - 1) * levelManager.levelUpStats.manaBonusPerLevel;
                int levelBonusAttack = (levelManager.CurrentLevel - 1) * levelManager.levelUpStats.attackDamageBonusPerLevel;
                int levelBonusMagic = (levelManager.CurrentLevel - 1) * levelManager.levelUpStats.magicDamageBonusPerLevel;
                int levelBonusArmor = (levelManager.CurrentLevel - 1) * levelManager.levelUpStats.armorBonusPerLevel;
                float levelBonusCrit = (levelManager.CurrentLevel - 1) * levelManager.levelUpStats.criticalChanceBonusPerLevel;
                float levelBonusSpeed = (levelManager.CurrentLevel - 1) * levelManager.levelUpStats.moveSpeedBonusPerLevel;

                // 🆕 เก็บ base stats ใน characterData (ชื่อใหม่เพื่อแยกจาก total stats)
                characterData.baseMaxHp = character.characterStats.maxHp + levelBonusHp;
                characterData.baseMaxMana = character.characterStats.maxMana + levelBonusMana;
                characterData.baseAttackDamage = character.characterStats.attackDamage + levelBonusAttack;
                characterData.baseMagicDamage = character.characterStats.magicDamage + levelBonusMagic;
                characterData.baseArmor = character.characterStats.arrmor + levelBonusArmor;
                characterData.baseCriticalChance = character.characterStats.criticalChance + levelBonusCrit;
                characterData.baseCriticalDamageBonus = character.characterStats.criticalDamageBonus;
                characterData.baseMoveSpeed = character.characterStats.moveSpeed + levelBonusSpeed;
                characterData.baseHitRate = character.characterStats.hitRate;
                characterData.baseEvasionRate = character.characterStats.evasionRate;
                characterData.baseAttackSpeed = character.characterStats.attackSpeed;
                characterData.baseReductionCoolDown = character.characterStats.reductionCoolDown;

                Debug.Log($"[PersistentPlayerData] Base stats calculated: HP={characterData.baseMaxHp}, ATK={characterData.baseAttackDamage}");
            }

            // 🆕 เก็บ total stats (base + equipment) สำหรับแสดงใน UI
            characterData.totalMaxHp = character.MaxHp;
            characterData.totalMaxMana = character.MaxMana;
            characterData.totalAttackDamage = character.AttackDamage;
            characterData.totalMagicDamage = character.MagicDamage;
            characterData.totalArmor = character.Armor;
            characterData.totalCriticalChance = character.CriticalChance;
            characterData.totalCriticalDamageBonus = character.CriticalDamageBonus;
            characterData.totalMoveSpeed = character.MoveSpeed;
            characterData.totalHitRate = character.HitRate;
            characterData.totalEvasionRate = character.EvasionRate;
            characterData.totalAttackSpeed = character.AttackSpeed;
            characterData.totalReductionCoolDown = character.ReductionCoolDown;

            Debug.Log($"[PersistentPlayerData] Total stats saved: HP={characterData.totalMaxHp}, ATK={characterData.totalAttackDamage}");

            // บันทึกลง Firebase
            SavePlayerDataAsync();

            Debug.Log($"[PersistentPlayerData] ✅ Saved both base and total stats for {characterType}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PersistentPlayerData] ❌ Error saving base stats: {e.Message}");
        }
    }
    public void LoadStatsForCharacter(Character character, LevelManager levelManager)
    {
        if (multiCharacterData == null || character == null || levelManager == null)
        {
            Debug.LogError("[PersistentPlayerData] Cannot load stats - missing components");
            return;
        }

        try
        {
            string characterType = multiCharacterData.currentActiveCharacter;
            var characterData = GetCharacterData(characterType);

            if (characterData == null)
            {
                Debug.LogWarning($"[PersistentPlayerData] No data found for {characterType}, creating default");
                characterData = CreateDefaultCharacterData(characterType);
                multiCharacterData.characters.Add(characterData);
            }

            Debug.Log($"[PersistentPlayerData] 📥 Loading stats for {characterType}...");

            // โหลด level และ exp
            levelManager.CurrentLevel = characterData.currentLevel;
            levelManager.CurrentExp = characterData.currentExp;
            levelManager.ExpToNextLevel = characterData.expToNextLevel;

            // 🆕 โหลด base stats เท่านั้น (ไม่รวม equipment bonuses)
            LoadBaseStatsOnly(character, levelManager, characterData);

            // Mark as initialized
            levelManager.IsInitialized = true;

            Debug.Log($"[PersistentPlayerData] ✅ Base stats loaded for {characterType}: Level {levelManager.CurrentLevel}, HP={character.MaxHp}, ATK={character.AttackDamage}");

            // 🆕 บันทึก base stats เพื่อใช้ในอนาคต
            SaveBaseStatsToCharacterData(character, levelManager, characterData);

        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PersistentPlayerData] ❌ Error loading stats: {e.Message}");
        }
    }
    private void SaveBaseStatsToCharacterData(Character character, LevelManager levelManager, CharacterProgressData characterData)
    {
        try
        {
            Debug.Log($"[SaveBaseStatsToCharacterData] Saving base stats for {characterData.characterType}...");

            characterData.UpdateBaseStats(
                character.MaxHp, character.MaxMana, character.AttackDamage, character.MagicDamage, character.Armor,
                character.CriticalChance, character.CriticalDamageBonus, character.MoveSpeed,
                character.HitRate, character.EvasionRate, character.AttackSpeed, character.ReductionCoolDown
            );

            Debug.Log($"[SaveBaseStatsToCharacterData] ✅ Base stats saved: HP={character.MaxHp}, ATK={character.AttackDamage}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveBaseStatsToCharacterData] ❌ Error: {e.Message}");
        }
    }
    public bool ValidateAndFixEquipmentLoading(Character character)
    {
        if (character == null || multiCharacterData == null) return false;

        try
        {
            Debug.Log($"[ValidateAndFixEquipmentLoading] Validating equipment for {character.CharacterName}...");

            string characterType = multiCharacterData.currentActiveCharacter;
            var characterData = GetCharacterData(characterType);

            if (characterData?.HasEquipmentData() != true)
            {
                Debug.Log($"[ValidateAndFixEquipmentLoading] No equipment data in Firebase for {characterType}");
                return false;
            }

            // ตรวจสอบ equipment ใน character
            int currentEquipmentCount = character.GetAllEquippedItems().Count;
            int expectedEquipmentCount = characterData.totalEquippedItems;

            Debug.Log($"[ValidateAndFixEquipmentLoading] Equipment count: Current={currentEquipmentCount}, Expected={expectedEquipmentCount}");

            if (currentEquipmentCount < expectedEquipmentCount)
            {
                Debug.LogWarning($"[ValidateAndFixEquipmentLoading] Equipment mismatch detected! Attempting auto-fix...");

                // ลอง reload equipment
                LoadInventoryData(character);

                // ตรวจสอบอีกครั้ง
                int newEquipmentCount = character.GetAllEquippedItems().Count;
                Debug.Log($"[ValidateAndFixEquipmentLoading] After reload: {newEquipmentCount} equipment items");

                return newEquipmentCount > currentEquipmentCount;
            }

            Debug.Log($"[ValidateAndFixEquipmentLoading] ✅ Equipment validation passed");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ValidateAndFixEquipmentLoading] ❌ Error: {e.Message}");
            return false;
        }
    }
    public bool ForceReloadEquipment(Character character)
    {
        if (character == null || multiCharacterData == null) return false;

        try
        {
            Debug.Log($"[ForceReloadEquipment] Force reloading equipment for {character.CharacterName}...");

            // เคลียร์ equipment ปัจจุบัน
            character.ClearAllEquipmentForLoad();

            // รอ 1 frame (จำลอง)
            System.Threading.Thread.Sleep(50);

            // โหลด equipment ใหม่
            LoadInventoryData(character);

            // ตรวจสอบผลลัพธ์
            int equipmentCount = character.GetAllEquippedItems().Count;
            Debug.Log($"[ForceReloadEquipment] ✅ Force reload result: {equipmentCount} equipment items");

            return equipmentCount > 0;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ForceReloadEquipment] ❌ Error: {e.Message}");
            return false;
        }
    }
    private void LoadBaseStatsOnly(Character character, LevelManager levelManager, CharacterProgressData characterData)
    {
        Debug.Log($"[LoadBaseStatsOnly] Loading base stats for {characterData.characterType}...");

        // 🆕 ถ้ามี base stats ที่บันทึกไว้ ให้ใช้ตัวนั้น
        if (characterData.HasValidBaseStats())
        {
            Debug.Log($"[LoadBaseStatsOnly] Using saved base stats");

            character.MaxHp = characterData.baseMaxHp;
            character.CurrentHp = characterData.baseMaxHp;
            character.MaxMana = characterData.baseMaxMana;
            character.CurrentMana = characterData.baseMaxMana;
            character.AttackDamage = characterData.baseAttackDamage;
            character.MagicDamage = characterData.baseMagicDamage;
            character.Armor = characterData.baseArmor;
            character.CriticalChance = characterData.baseCriticalChance;
            character.CriticalDamageBonus = characterData.baseCriticalDamageBonus;
            character.MoveSpeed = characterData.baseMoveSpeed;
            character.HitRate = characterData.baseHitRate;
            character.EvasionRate = characterData.baseEvasionRate;
            character.AttackSpeed = characterData.baseAttackSpeed;
            character.ReductionCoolDown = characterData.baseReductionCoolDown;
        }
        else
        {
            Debug.Log($"[LoadBaseStatsOnly] Calculating base stats from ScriptableObject + level");

            // คำนวณจาก ScriptableObject + level bonuses
            CalculateBaseStatsFromScriptableObject(character, levelManager);

            // บันทึก base stats ที่คำนวณได้
            SaveBaseStatsToCharacterData(character, levelManager, characterData);
        }

        // Force update network state
        character.ForceUpdateNetworkState();

        Debug.Log($"[LoadBaseStatsOnly] ✅ Base stats loaded: HP={character.MaxHp}, ATK={character.AttackDamage}, ARM={character.Armor}");
    }

    private void CalculateBaseStatsFromScriptableObject(Character character, LevelManager levelManager)
    {
        if (character.characterStats == null)
        {
            Debug.LogError($"[CalculateBaseStatsFromScriptableObject] No characterStats ScriptableObject!");
            return;
        }

        // ใช้ stats จาก ScriptableObject เป็น base
        int baseHp = character.characterStats.maxHp;
        int baseMana = character.characterStats.maxMana;
        int baseAttack = character.characterStats.attackDamage;
        int baseMagic = character.characterStats.magicDamage;
        int baseArmor = character.characterStats.arrmor;
        float baseCrit = character.characterStats.criticalChance;
        float baseCritDamage = character.characterStats.criticalDamageBonus;
        float baseSpeed = character.characterStats.moveSpeed;
        float baseHit = character.characterStats.hitRate;
        float baseEvasion = character.characterStats.evasionRate;
        float baseAttackSpeed = character.characterStats.attackSpeed;
        float baseCDR = character.characterStats.reductionCoolDown;

        // เพิ่ม level bonuses
        if (levelManager?.levelUpStats != null)
        {
            int levelBonus = levelManager.CurrentLevel - 1;

            baseHp += levelBonus * levelManager.levelUpStats.hpBonusPerLevel;
            baseMana += levelBonus * levelManager.levelUpStats.manaBonusPerLevel;
            baseAttack += levelBonus * levelManager.levelUpStats.attackDamageBonusPerLevel;
            baseMagic += levelBonus * levelManager.levelUpStats.magicDamageBonusPerLevel;
            baseArmor += levelBonus * levelManager.levelUpStats.armorBonusPerLevel;
            baseCrit += levelBonus * levelManager.levelUpStats.criticalChanceBonusPerLevel;
            baseSpeed += levelBonus * levelManager.levelUpStats.moveSpeedBonusPerLevel;
        }

        // Apply ลง character
        character.MaxHp = baseHp;
        character.CurrentHp = baseHp;
        character.MaxMana = baseMana;
        character.CurrentMana = baseMana;
        character.AttackDamage = baseAttack;
        character.MagicDamage = baseMagic;
        character.Armor = baseArmor;
        character.CriticalChance = baseCrit;
        character.CriticalDamageBonus = baseCritDamage;
        character.MoveSpeed = baseSpeed;
        character.HitRate = baseHit;
        character.EvasionRate = baseEvasion;
        character.AttackSpeed = baseAttackSpeed;
        character.ReductionCoolDown = baseCDR;

        Debug.Log($"[CalculateBaseStatsFromScriptableObject] ✅ Calculated base stats: Level {levelManager.CurrentLevel}, HP={baseHp}, ATK={baseAttack}, ARM={baseArmor}");
    }


    private System.Collections.IEnumerator DelayedApplyEquipmentBonuses(Character character)
    {
        Debug.Log("[PersistentPlayerData] 🔄 Applying equipment bonuses after loading base stats...");

        // รอ 3 frames เพื่อให้ระบบพร้อม
        yield return null;
        yield return null;
        yield return null;

        // โหลด equipment
        LoadInventoryData(character);

        // รออีก 2 frames เพื่อให้ equipment load เสร็จ
        yield return null;
        yield return null;

        // Apply equipment bonuses
        character.ApplyLoadedEquipmentStats();

        Debug.Log($"[PersistentPlayerData] ✅ Equipment bonuses applied: HP={character.MaxHp}, ATK={character.AttackDamage}");

        // บันทึก total stats ใหม่
        SaveCurrentStatsAsTotal(character);
    }
    /// <summary>
    /// โหลดข้อมูลเงินจาก PlayerPrefs (fallback)
    /// </summary>
    private void LoadCurrencyFromPlayerPrefs()
    {
        try
        {
            if (multiCharacterData?.sharedCurrency == null)
                multiCharacterData.sharedCurrency = new SharedCurrencyData();

            string goldStr = PlayerPrefs.GetString("PlayerGold", "1000");
            if (long.TryParse(goldStr, out long gold))
            {
                multiCharacterData.sharedCurrency.gold = gold;
            }

            multiCharacterData.sharedCurrency.gems = PlayerPrefs.GetInt("PlayerGems", 50);

            Debug.Log("[LoadCurrencyFromPlayerPrefs] ✅ Currency loaded from PlayerPrefs backup");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LoadCurrencyFromPlayerPrefs] ❌ Error: {e.Message}");
        }
    }
    public void SaveEquippedItemsOnly(Character character)
    {
        if (character == null || multiCharacterData == null) return;

        try
        {
            string characterType = multiCharacterData.currentActiveCharacter;
            var characterData = GetOrCreateCharacterData(characterType);

            Debug.Log($"[SaveEquippedItemsOnly] 💾 Saving equipped items for {characterType}...");

            // บันทึก equipped items โดยตรง
            var equipmentData = InventoryDataConverter.ToCharacterEquipmentData(character);
            characterData.characterEquipment = equipmentData;
            characterData.UpdateEquipmentDebugInfo();

            // บันทึกลง Firebase ทันที
            SavePlayerDataAsync();

            Debug.Log($"[SaveEquippedItemsOnly] ✅ Equipped items saved successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveEquippedItemsOnly] ❌ Error: {e.Message}");
        }
    }

    private void SaveCurrentStatsAsTotal(Character character)
    {
        try
        {
            string characterType = multiCharacterData.currentActiveCharacter;
            var characterData = GetOrCreateCharacterData(characterType);

            characterData.totalMaxHp = character.MaxHp;
            characterData.totalMaxMana = character.MaxMana;
            characterData.totalAttackDamage = character.AttackDamage;
            characterData.totalMagicDamage = character.MagicDamage;
            characterData.totalArmor = character.Armor;
            characterData.totalCriticalChance = character.CriticalChance;
            characterData.totalCriticalDamageBonus = character.CriticalDamageBonus;
            characterData.totalMoveSpeed = character.MoveSpeed;
            characterData.totalHitRate = character.HitRate;
            characterData.totalEvasionRate = character.EvasionRate;
            characterData.totalAttackSpeed = character.AttackSpeed;
            characterData.totalReductionCoolDown = character.ReductionCoolDown;

            SavePlayerDataAsync();

            Debug.Log($"[PersistentPlayerData] 💾 Updated total stats: HP={character.MaxHp}, ATK={character.AttackDamage}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PersistentPlayerData] ❌ Error saving total stats: {e.Message}");
        }
    }
    /// <summary>
    /// บังคับบันทึกเงินทันที
    /// </summary>
    public void ForceImmediateSaveCurrency()
    {
        Debug.Log("[ForceImmediateSaveCurrency] 🚀 Force saving currency data...");
        SaveCurrencyData();
        SaveCurrencyToPlayerPrefs();
    }

    /// <summary>
    /// ตรวจสอบว่าควรโหลดข้อมูลเงินจาก Firebase หรือไม่
    /// </summary>
    public bool ShouldLoadCurrencyFromFirebase()
    {
        return multiCharacterData != null &&
               multiCharacterData.sharedCurrency != null &&
               multiCharacterData.hasCurrencyData;
    }

    #endregion
    #region Debug Methods ฟังก์ชันสำหรับ debug
    #region 🆕 Debug Context Menus

    [ContextMenu("Debug: Show Character Data Status")]

    [ContextMenu("Test: Check Should Load From Firebase")]
    private void TestShouldLoadFromFirebase()
    {
        bool shouldLoad = ShouldLoadFromFirebase();
        bool hasData = HasInventoryDataInFirebase();

        Debug.Log("=== LOAD CHECK RESULT ===");
        Debug.Log($"Should Load From Firebase: {shouldLoad}");
        Debug.Log($"Has Inventory Data: {hasData}");
        Debug.Log($"MultiCharacterData: {multiCharacterData != null}");

        if (multiCharacterData != null)
        {
            var sharedItems = multiCharacterData.sharedInventory?.items?.Count ?? 0;
            Debug.Log($"Shared Inventory Items: {sharedItems}");
            Debug.Log($"Characters Count: {multiCharacterData.characters?.Count ?? 0}");
        }
        Debug.Log("========================");
    }
    private void DebugShowCharacterDataStatus()
    {
        LogCharacterDataStatus();

        if (multiCharacterData != null)
        {
            Debug.Log($"Current Active Character: {multiCharacterData.currentActiveCharacter}");
            Debug.Log($"Has Inventory Data: {multiCharacterData.HasInventoryData()}");
            Debug.Log($"Has Any Data: {multiCharacterData.HasAnyInventoryOrEquipmentData()}");
        }
    }

    [ContextMenu("Debug: Fix Split Character Data")]
    private void DebugFixSplitCharacterData()
    {
        FixSplitCharacterData();
    }

    [ContextMenu("Debug: Force Save Current Character")]
    private void DebugForceSaveCurrentCharacter()
    {
        var character = FindObjectOfType<Character>();
        if (character != null)
        {
            Debug.Log($"[DebugForceSave] Saving {character.CharacterName}...");
            SaveInventoryData(character);
            LogCharacterDataStatus();
        }
        else
        {
            Debug.LogError("No Character found in scene!");
        }
    }

    [ContextMenu("Debug: Force Load Current Character")]
    private void DebugForceLoadCurrentCharacter()
    {
        var character = FindObjectOfType<Character>();
        if (character != null)
        {
            Debug.Log($"[DebugForceLoad] Loading {character.CharacterName}...");
            LoadInventoryData(character);
        }
        else
        {
            Debug.LogError("No Character found in scene!");
        }
    }

    [ContextMenu("Debug: Clear All Character Equipment Data")]
    private void DebugClearAllCharacterEquipmentData()
    {
        if (multiCharacterData?.characters == null) return;

        Debug.Log("[DebugClearEquipment] Clearing all character equipment data...");

        foreach (var character in multiCharacterData.characters)
        {
            if (character?.characterEquipment != null)
            {
                // เคลียร์เฉพาะ equipment ไม่ใช่ stats
                character.characterEquipment = new CharacterEquipmentData(character.characterType);
                character.hasEquipmentData = false;
                character.totalEquippedItems = 0;
                character.totalPotions = 0;
            }
        }

        SavePlayerDataAsync();
        Debug.Log("[DebugClearEquipment] ✅ Cleared all equipment data");
    }

    [ContextMenu("Debug: Show Firebase Raw Data")]
    private void DebugShowFirebaseRawData()
    {
        if (multiCharacterData == null)
        {
            Debug.Log("No multiCharacterData");
            return;
        }

        string json = JsonUtility.ToJson(multiCharacterData, true);
        Debug.Log("=== FIREBASE RAW DATA ===");
        Debug.Log(json);
        Debug.Log("========================");
    }

    [ContextMenu("Debug: Test Character Identification")]
    private void DebugTestCharacterIdentification()
    {
        var character = FindObjectOfType<Character>();
        if (character == null)
        {
            Debug.LogError("No Character found!");
            return;
        }

        Debug.Log("=== CHARACTER IDENTIFICATION TEST ===");
        Debug.Log($"Character.CharacterName: '{character.CharacterName}'");
        Debug.Log($"PersistentPlayerData.currentActiveCharacter: '{multiCharacterData?.currentActiveCharacter}'");
        Debug.Log($"PersistentPlayerData.GetCurrentActiveCharacter(): '{GetCurrentActiveCharacter()}'");

        // ทดสอบการหา character data
        string characterType = multiCharacterData?.currentActiveCharacter ?? "Unknown";
        var characterData = multiCharacterData?.GetCharacterData(characterType);

        Debug.Log($"Character data found: {characterData != null}");
        if (characterData != null)
        {
            Debug.Log($"Character data type: '{characterData.characterType}'");
            Debug.Log($"Has stats: {characterData.totalMaxHp > 0}");
            Debug.Log($"Has equipment: {characterData.HasEquipmentData()}");
        }

        Debug.Log("====================================");
    }

    [ContextMenu("🔍 Debug: Check Load Status")]
    private void DebugCheckLoadStatus()
    {
        Debug.Log("=== LOAD STATUS CHECK ===");
        Debug.Log($"PersistentPlayerData Ready: {Instance != null}");
        Debug.Log($"MultiCharacterData Ready: {multiCharacterData != null}");
        Debug.Log($"Should Load From Firebase: {ShouldLoadFromFirebase()}");
        Debug.Log($"Has Inventory Data: {HasInventoryDataInFirebase()}");

        if (multiCharacterData != null)
        {
            Debug.Log($"Shared Items Count: {multiCharacterData.sharedInventory?.items?.Count ?? 0}");
            Debug.Log($"Current Active Character: {multiCharacterData.currentActiveCharacter}");

            var currentChar = multiCharacterData.GetActiveCharacterData();
            if (currentChar != null)
            {
                Debug.Log($"Character Equipment: {currentChar.HasEquipmentData()}");
                Debug.Log($"Equipped Items: {currentChar.totalEquippedItems}");
                Debug.Log($"Potions: {currentChar.totalPotions}");
            }
        }
        Debug.Log("========================");
    }
    [ContextMenu("🔍 Debug: Test Equipment Load")]
    private void TestEquipmentLoad()
    {
        var character = FindObjectOfType<Character>();
        if (character == null)
        {
            Debug.LogError("No Character found in scene!");
            return;
        }

        Debug.Log("=== TESTING EQUIPMENT LOAD ===");

        // ตรวจสอบข้อมูลใน Firebase
        DebugShowAllCharacterEquipmentData();

        // ลองโหลดข้อมูล
        LoadInventoryData(character);

        // ตรวจสอบผลลัพธ์
    }

    [ContextMenu("🔍 Debug: Equipment vs UI Status")]
    private void DebugEquipmentVsUIStatus()
    {
        var character = FindObjectOfType<Character>();
        if (character == null)
        {
            Debug.LogError("No Character found!");
            return;
        }

        Debug.Log("=== EQUIPMENT vs UI STATUS ===");

        // 1. ตรวจสอบข้อมูลใน Character
        Debug.Log("📊 CHARACTER DATA:");
        for (int i = 0; i < 6; i++)
        {
            ItemType itemType = GetItemTypeFromSlotIndex(i);
            ItemData equippedItem = character.GetEquippedItem(itemType);
            Debug.Log($"  {itemType}: {(equippedItem?.ItemName ?? "EMPTY")}");
        }

        // 2. ตรวจสอบ EquipmentSlotManager
        var equipmentManager = character.GetComponent<EquipmentSlotManager>();
        Debug.Log($"📱 EQUIPMENT MANAGER: {(equipmentManager != null ? "Found" : "Not Found")}");
        if (equipmentManager != null)
        {
            Debug.Log($"  - Connected: {equipmentManager.IsConnected()}");
        }

        // 3. ตรวจสอบ CombatUIManager
        var combatUI = FindObjectOfType<CombatUIManager>();
        Debug.Log($"🖥️ COMBAT UI: {(combatUI != null ? "Found" : "Not Found")}");
        if (combatUI?.equipmentSlotManager != null)
        {
            Debug.Log($"  - Equipment Manager: Found");
            Debug.Log($"  - Connected: {combatUI.equipmentSlotManager.IsConnected()}");
        }

        Debug.Log("==============================");
    }

    [ContextMenu("🔧 Force Fix Equipment Load")]
    private void ForceFixEquipmentLoad()
    {
        var character = FindObjectOfType<Character>();
        if (character == null)
        {
            Debug.LogError("No Character found!");
            return;
        }

        Debug.Log("🔧 FORCE FIXING EQUIPMENT LOAD...");

        // 1. ตรวจสอบและ fix character type matching
        if (multiCharacterData != null)
        {
            string currentActive = multiCharacterData.currentActiveCharacter;
            string characterName = character.CharacterName;

            Debug.Log($"Active Character: {currentActive}");
            Debug.Log($"Character Name: {characterName}");

            // ถ้าไม่ตรงกัน ให้แก้ไข
            if (currentActive != characterName)
            {
                Debug.LogWarning($"Character type mismatch! Fixing: {characterName}");
                multiCharacterData.currentActiveCharacter = characterName;
            }
        }

        // 2. Force โหลดข้อมูล equipment
        LoadInventoryData(character);

        // 3. รอแล้ว force refresh UI
        StartCoroutine(DelayedForceRefresh(character));
    }

    private System.Collections.IEnumerator DelayedForceRefresh(Character character)
    {
        yield return new WaitForSeconds(1f);

        Debug.Log("🔄 Force refreshing equipment UI...");

        // Force refresh ทุกอย่าง
        var equipmentManager = character.GetComponent<EquipmentSlotManager>();
        if (equipmentManager != null && equipmentManager.IsConnected())
        {
            equipmentManager.ForceRefreshFromCharacter();
            Debug.Log("✅ Character EquipmentSlotManager refreshed");
        }

        var combatUI = FindObjectOfType<CombatUIManager>();
        if (combatUI?.equipmentSlotManager != null)
        {
            combatUI.equipmentSlotManager.ForceRefreshFromCharacter();
            Debug.Log("✅ CombatUI EquipmentSlotManager refreshed");
        }

        Character.RaiseOnStatsChanged();
        Canvas.ForceUpdateCanvases();

        Debug.Log("🎉 Force fix complete!");

        // ตรวจสอบผลลัพธ์
        yield return new WaitForSeconds(0.5f);
    }

    // 🆕 เพิ่ม helper method
    private ItemType GetItemTypeFromSlotIndex(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0: return ItemType.Head;
            case 1: return ItemType.Armor;
            case 2: return ItemType.Weapon;
            case 3: return ItemType.Pants;
            case 4: return ItemType.Shoes;
            case 5: return ItemType.Rune;
            default: return ItemType.Weapon;
        }
    }


    #endregion
    // Note: This method is not implemented - consider implementing or removing

   
    // 🆕 เพิ่ม method สำหรับ restore จาก PlayerPrefs
  

    // 🆕 เพิ่ม method สำหรับตรวจสอบ data consistency
  

    // 🆕 Emergency load method ที่ไม่ clear inventory
   

    private System.Collections.IEnumerator DelayedCanvasUpdate()
    {
        yield return new WaitForSeconds(0.1f);
        Canvas.ForceUpdateCanvases();

        yield return new WaitForSeconds(0.1f);
        Canvas.ForceUpdateCanvases();

        Debug.Log("[DelayedCanvasUpdate] ✅ Delayed canvas updates completed");
    }

    // 🆕 Alternative fix method
    

    // 🆕 Emergency starter items
  

    private System.Collections.IEnumerator DelayedEmergencySave(Character character)
    {
        yield return new WaitForSeconds(2f);

        Debug.Log("💾 Emergency saving after starter items...");
        SaveInventoryData(character);
    }
    internal void CheckFirebaseStatus()
    {
        throw new System.NotImplementedException();
    }
    #endregion
}