using UnityEngine;
using Firebase.Auth;
using Firebase.Database;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PersistentPlayerData : MonoBehaviour
{
    private static Dictionary<string, CharacterStats> _characterStatsCache = new Dictionary<string, CharacterStats>();

    #region Variables and Properties  ตัวแปร, Singleton pattern และ Properties
    [Header("Multi-Character Data")]
    public MultiCharacterPlayerData multiCharacterData;
    public bool isDataLoaded = false;
    [Header("⚙️ Settings")]
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
        if (!isDataLoaded || multiCharacterData == null)
            return false;

        // ตรวจสอบว่ามีข้อมูลผู้เล่นจริงๆ
        bool hasPlayerData = !string.IsNullOrEmpty(multiCharacterData.playerName) &&
                            multiCharacterData.characters != null &&
                            multiCharacterData.characters.Count > 0;

        // ตรวจสอบว่ามีข้อมูลตัวละครปัจจุบัน
        bool hasCurrentCharacter = GetCurrentCharacterData() != null;

        Debug.Log($"[HasValidData] PlayerData: {hasPlayerData}, CurrentChar: {hasCurrentCharacter}");

        return hasPlayerData && hasCurrentCharacter;
    }
    #endregion
    public void UpdateDailyQuestProgress(QuestType questType, int amount = 1)
    {
        if (multiCharacterData == null)
        {
            Debug.LogWarning("[PersistentPlayerData] Cannot update quest - MultiCharacterData is null");
            return;
        }

        Debug.Log($"[PersistentPlayerData] Updating quest progress: {questType} +{amount}");

        multiCharacterData.UpdateQuestProgress(questType, amount);
        multiCharacterData.UpdateDailyQuestDebugInfo();

        // บันทึกลง Firebase ทันที
        SavePlayerDataAsync();

        Debug.Log($"[PersistentPlayerData] Quest progress saved to Firebase");
    }
    #region Data Loading & Saving การโหลดและบันทึกข้อมูล
    public void LoadPlayerDataAsync()
    {
        if (isDataLoaded) return;
        StartCoroutine(LoadDataCoroutine());
    }

    // แก้ไขใน PersistentPlayerData.cs - LoadDataCoroutine()
    // แก้ไขใน PersistentPlayerData.cs - LoadDataCoroutine()
    public IEnumerator LoadDataCoroutine()
    {
        if (auth?.CurrentUser == null)
        {
            yield break;
        }

        Debug.Log("[PersistentPlayerData] Loading multi-character data from Firebase...");

        var task = databaseReference.Child("players").Child(auth.CurrentUser.UserId).GetValueAsync();

        float timeout = 30f;
        float elapsed = 0f;

        while (!task.IsCompleted && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;

            if (Mathf.RoundToInt(elapsed) % 5 == 0 && elapsed % 1f < 0.1f)
            {
                Debug.Log($"[PersistentPlayerData] Still loading... {elapsed:F1}s");
            }
        }

        if (task.IsCompleted && task.Exception == null && task.Result.Exists)
        {
            string json = task.Result.GetRawJsonValue();
            bool loaded = false;

            if (!string.IsNullOrEmpty(json) && json.Length > 100)
            {
                try
                {
                    var tempData = JsonUtility.FromJson<MultiCharacterPlayerData>(json);

                    if (IsValidPlayerData(tempData))
                    {
                        multiCharacterData = tempData;
                       
                        // ✅ โหลด character stats หลังจาก deserialize JSON
                        multiCharacterData.LoadCharacterStatsFromResources();

                        isDataLoaded = true;
                        loaded = true;

                        // ✅ เคลียร์ PlayerPrefs เก่าเฉพาะเมื่อโหลดข้อมูลใหม่จาก Firebase สำเร็จ

                        SaveCompleteBackupToPlayerPrefs();

                       
                    }
                    else
                    {
                        Debug.LogWarning("[PersistentPlayerData] Invalid data structure from Firebase");
                        loaded = false;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[PersistentPlayerData] Failed to parse Firebase data: {e.Message}");
                    loaded = false;
                }
            }
            else
            {
                Debug.LogWarning($"[PersistentPlayerData] JSON too small ({json?.Length ?? 0} chars) from Firebase");
                loaded = false;
            }

            if (loaded)
            {
                LoadCurrencyData();
            }
        }
        else if (elapsed >= timeout)
        {
            Debug.LogError("[PersistentPlayerData] ⚠️ TIMEOUT! Trying backup before creating new...");
        }
        else
        {
            Debug.Log("[PersistentPlayerData] No Firebase data found, checking backup...");
        }

        RegisterPlayerInDirectory();
    }

    // 🆕 เพิ่ม method สำหรับเคลียร์ PlayerPrefs เก่า
    
    // 🆕 เพิ่ม method สำหรับ validate ข้อมูล
    private bool IsValidPlayerData(MultiCharacterPlayerData data)
    {
        if (data == null) return false;

        // ตรวจสอบข้อมูลพื้นฐาน
        bool hasBasicData = !string.IsNullOrEmpty(data.playerName) &&
                           !string.IsNullOrEmpty(data.currentActiveCharacter) &&
                           data.characters != null &&
                           data.characters.Count > 0;

        if (!hasBasicData) return false;

        // ตรวจสอบว่ามี character ที่ active อยู่จริง
        bool hasActiveCharacter = data.characters.Any(c => c.characterType == data.currentActiveCharacter);

        if (!hasActiveCharacter) return false;

        Debug.Log($"[IsValidPlayerData] Data validation passed for {data.playerName}");
        return true;
    }

 

    // 🆕 เพิ่ม method สำหรับบันทึก backup
    private void SaveCompleteBackupToPlayerPrefs()
    {
        try
        {
            if (multiCharacterData != null)
            {
                string json = JsonUtility.ToJson(multiCharacterData, true);
               

            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveCompleteBackupToPlayerPrefs] Error: {e.Message}");
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

    private bool isSaving = false;

    private IEnumerator SaveDataCoroutine()
    {
        if (multiCharacterData == null || auth?.CurrentUser == null)
        {
            yield break;
        }

        // ป้องกัน concurrent save
        if (isSaving)
        {
            Debug.LogWarning("[PersistentPlayerData] Save already in progress, skipping...");
            yield break;
        }

        isSaving = true;

        // อัปเดต debug info ทั้งหมด
        multiCharacterData.lastLoginDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        multiCharacterData.UpdateStageProgressDebugInfo();
        multiCharacterData.UpdateAllInventoryDebugInfo();
        multiCharacterData.UpdateCurrencyDebugInfo();

        // 🆕 บันทึก backup ก่อน save ไป Firebase
        SaveCompleteBackupToPlayerPrefs();

        string json = JsonUtility.ToJson(multiCharacterData, true);
        var task = databaseReference.Child("players").Child(auth.CurrentUser.UserId).SetRawJsonValueAsync(json);

        yield return new WaitForSeconds(0.5f);

        Debug.Log($"💾 Saved complete player data including stage progress for {multiCharacterData.playerName}");

        if (task.IsCompleted)
        {
            if (task.Exception != null)
            {
                Debug.LogError($"❌ Failed to save: {task.Exception.Message}");
            }
            else
            {
                Debug.Log($"✅ Successfully saved complete data to Firebase");

                // 🆕 อัปเดต backup timestamp หลัง save สำเร็จ
               
            }
        }

        isSaving = false;
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
        SavePlayerDataAsync();

        Debug.Log($"✅ Switched to character: {characterType}");
    }

    public void UpdateLevelAndStats(int level, int exp, int expToNext, int maxHp, int maxMana,
    int attackDamage, int magicDamage, int armor, float critChance, float critDamageBonus,
    float moveSpeed, float hitRate, float evasionRate, float attackSpeed, float reductionCoolDown, float lifeSteal,int magicArmor,float healthRegen,float manaRegen,float amp)
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
            float oldLifeSteal = character.totalLifeSteal; // 🆕 เพิ่ม debug LifeSteal

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
            character.totalLifeSteal = lifeSteal; // ✅ บันทึก LifeSteal
            character.totalAmpdamage = amp; // ✅ บันทึก LifeSteal
            character.totalMagicArmor = magicArmor; // ✅ บันทึก LifeSteal
            character.totalHealthRegen = healthRegen; // ✅ บันทึก LifeSteal
            character.totalManaRegen = manaRegen; // ✅ บันทึก LifeSteal

            // Mark ว่ามี total stats
            character.hasTotalStats = true;
            character.statsLastUpdateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

          

            // 🆕 บันทึกลง Firebase ทันที
            SavePlayerDataAsync();
        }
    }


    // Note: This method appears to be a duplicate with different parameters - consider removing or renaming

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
           

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSharedInventoryDataSafe] ❌ Error: {e.Message}");
            return false;
        }
    }

    // 🆕 เพิ่ม validation หลัง save
  

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

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveCharacterEquipmentData] ❌ Error: {e.Message}");
            return false;
        }
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
    }

    /// <summary>
    /// บันทึกข้อมูล inventory พื้นฐานลง PlayerPrefs (เป็น backup)
    /// </summary>
   

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


    #endregion
    #region 🆕 Inventory Load Methods

    /// <summary>
    /// โหลดข้อมูล inventory และ equipment ทั้งหมดของ character
    /// </summary>
    public void LoadInventoryData(Character character)
    {
        if (character == null || multiCharacterData == null)
        {
            Debug.LogError("[LoadInventoryData] Character or MultiCharacterData is null!");
            return;
        }

        try
        {
            Debug.Log($"[LoadInventoryData] 📥 Starting inventory load for {character.CharacterName}");

            // 1. Load Character Equipment FIRST (แบบเดิม)
            bool equipmentLoaded = LoadCharacterEquipmentDataFirst(character);

            // 2. Load Shared Inventory AFTER equipment (แบบเดิม)  
            bool inventoryLoaded = LoadSharedInventoryDataSafe(character);

            if (inventoryLoaded || equipmentLoaded)
            {
                Debug.Log($"[LoadInventoryData] ✅ Load completed - Equipment: {equipmentLoaded}, Inventory: {inventoryLoaded}");
                ForceRefreshInventoryUI(character);
                NotifyLevelManagerEquipmentLoaded(character);

                // ✅ เพิ่มแค่บรรทัดนี้ - force update grid หลังโหลดเสร็จ
                StartCoroutine(DelayedGridRefresh(character));
            }
            else
            {
                Debug.LogWarning("[LoadInventoryData] ⚠️ No data loaded");
                GiveStarterItemsIfNeeded(character);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LoadInventoryData] ❌ Error: {e.Message}");
            GiveStarterItemsIfNeeded(character);
        }
    }

    // ✅ เพิ่ม method ใหม่สำหรับ delayed grid refresh เท่านั้น
    private System.Collections.IEnumerator DelayedGridRefresh(Character character)
    {
        // รอ 1 วินาทีเพื่อให้ stats และ equipment settle เสร็จ
        yield return new WaitForSeconds(1f);

        try
        {
            Debug.Log("[DelayedGridRefresh] 🔄 Refreshing inventory grid display...");

            // หา InventoryGridManager และ refresh display เท่านั้น
            var gridManager = FindObjectOfType<InventoryGridManager>();

            if (gridManager != null)
            {
                if (gridManager.OwnerCharacter == null)
                {
                    gridManager.SetOwnerCharacter(character);
                }

                // ✅ แค่ refresh display - ไม่โหลดข้อมูลซ้ำ
                gridManager.ForceRefreshAllSlots();

                Debug.Log("[DelayedGridRefresh] ✅ Grid display refreshed");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DelayedGridRefresh] ❌ Error: {e.Message}");
        }
    }
    private bool LoadSharedInventoryDataSafe(Character character)
    {
        try
        {
            var inventory = character.GetInventory();
            if (inventory == null)
            {
                Debug.LogWarning("[LoadSharedInventoryDataSafe] Character has no inventory");
                return false;
            }

            var sharedData = multiCharacterData.sharedInventory;
            if (sharedData == null || !sharedData.IsValid() || sharedData.items.Count == 0)
            {
                Debug.LogWarning("[LoadSharedInventoryDataSafe] No shared inventory data to load");
                return false;
            }

            Debug.Log($"[LoadSharedInventoryDataSafe] 📦 Loading {sharedData.items.Count} items (excluding equipped items)");

            // 🔧 รายการ ItemID ที่ equipped แล้ว (ไม่ให้โหลดเข้า inventory)
            var equippedItemIds = GetCurrentlyEquippedItemIds(character);

            // เคลียร์ inventory เฉพาะที่ไม่ใช่ equipped items
            ClearInventoryExcludingEquipped(inventory, equippedItemIds);

            // โหลดไอเทมที่ไม่ได้ equipped
            int successCount = 0;
            int skippedCount = 0;

            foreach (var savedItem in sharedData.items)
            {
                if (savedItem == null || !savedItem.IsValid())
                    continue;

                // 🔧 ข้าม items ที่ equipped แล้ว
                if (equippedItemIds.Contains(savedItem.itemId))
                {
                    Debug.Log($"[LoadSharedInventoryDataSafe] ⏭️ Skipping equipped item: {savedItem.itemName}");
                    skippedCount++;
                    continue;
                }

                bool loaded = LoadSingleInventoryItemSafe(inventory, savedItem);
                if (loaded) successCount++;
            }

            Debug.Log($"[LoadSharedInventoryDataSafe] ✅ Loaded {successCount} items, skipped {skippedCount} equipped items");
            return successCount > 0;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LoadSharedInventoryDataSafe] ❌ Error: {e.Message}");
            return false;
        }
    }
    private HashSet<string> GetCurrentlyEquippedItemIds(Character character)
    {
        var equippedIds = new HashSet<string>();

        try
        {
            // Equipment items (6 ช่อง)
            for (int i = 0; i < 6; i++)
            {
                ItemType itemType = GetItemTypeFromSlotIndex(i);
                ItemData equippedItem = character.GetEquippedItem(itemType);

                if (equippedItem != null && !string.IsNullOrEmpty(equippedItem.ItemId))
                {
                    equippedIds.Add(equippedItem.ItemId);
                    Debug.Log($"[GetCurrentlyEquippedItemIds] Found equipped: {equippedItem.ItemName} (ID: {equippedItem.ItemId})");
                }
            }

            // Potion items (5 ช่อง)
            for (int i = 0; i < 5; i++)
            {
                ItemData potionItem = character.GetPotionInSlot(i);

                if (potionItem != null && !string.IsNullOrEmpty(potionItem.ItemId))
                {
                    equippedIds.Add(potionItem.ItemId);
                    Debug.Log($"[GetCurrentlyEquippedItemIds] Found equipped potion: {potionItem.ItemName} (ID: {potionItem.ItemId})");
                }
            }

            Debug.Log($"[GetCurrentlyEquippedItemIds] Total equipped items: {equippedIds.Count}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GetCurrentlyEquippedItemIds] ❌ Error: {e.Message}");
        }

        return equippedIds;
    }

    // 🆕 เคลียร์ inventory แต่ไม่แตะ equipped items
    private void ClearInventoryExcludingEquipped(Inventory inventory, HashSet<string> equippedItemIds)
    {
        try
        {
            Debug.Log("[ClearInventoryExcludingEquipped] 🧹 Clearing inventory (preserving equipped items)...");

            // ดู items ใน inventory ปัจจุบัน
            for (int i = inventory.CurrentSlots - 1; i >= 0; i--)
            {
                var item = inventory.GetItem(i);
                if (item != null && !item.IsEmpty)
                {
                    // ถ้าเป็น equipped item ให้ข้าม
                    if (equippedItemIds.Contains(item.itemData.ItemId))
                    {
                        Debug.Log($"[ClearInventoryExcludingEquipped] ⏭️ Preserving equipped item: {item.itemData.ItemName}");
                        continue;
                    }

                    // ลบ items ที่ไม่ได้ equipped
                    inventory.RemoveItem(i, item.stackCount);
                }
            }

            Debug.Log("[ClearInventoryExcludingEquipped] ✅ Inventory cleared (equipped items preserved)");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ClearInventoryExcludingEquipped] ❌ Error: {e.Message}");
        }
    }
    private bool LoadCharacterEquipmentDataFirst(Character character)
    {
        try
        {
            string characterType = multiCharacterData.currentActiveCharacter;
            Debug.Log($"[LoadCharacterEquipmentDataFirst] ⚔️ Loading equipment FIRST for {characterType}");

            var characterProgressData = multiCharacterData.GetCharacterData(characterType);
            if (characterProgressData?.characterEquipment == null)
            {
                Debug.LogWarning($"[LoadCharacterEquipmentDataFirst] No equipment data for {characterType}");
                return false;
            }

            var equipmentData = characterProgressData.characterEquipment;
            if (!equipmentData.IsValid())
            {
                Debug.LogWarning($"[LoadCharacterEquipmentDataFirst] Invalid equipment data");
                return false;
            }

            // 🔧 เคลียร์ equipment slots ก่อน (แต่ไม่แตะ inventory)
            character.ClearEquipmentSlotsForReload();

            // โหลด Equipment Slots (6 ช่อง) โดยไม่ให้ไปใน inventory
            bool equipmentLoaded = LoadEquipmentSlotsDirectly(character, equipmentData);

            // โหลด Potion Slots (5 ช่อง)
            bool potionsLoaded = LoadPotionSlotsDirectly(character, equipmentData);

            if (equipmentLoaded || potionsLoaded)
            {
                Debug.Log($"[LoadCharacterEquipmentDataFirst] ✅ Equipment loaded FIRST - Equipment: {equipmentLoaded}, Potions: {potionsLoaded}");

                // Apply stats หลังโหลด equipment
                character.ApplyLoadedEquipmentStats();
                character.ForceRefreshEquipmentAfterLoad();

                return true;
            }

            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LoadCharacterEquipmentDataFirst] ❌ Error: {e.Message}");
            return false;
        }
    }
    private bool LoadEquipmentSlotsDirectly(Character character, CharacterEquipmentData equipmentData)
    {
        try
        {
            int loadedCount = 0;
            var equipment = equipmentData.equipment;

            Debug.Log("[LoadEquipmentSlotsDirectly] Loading equipment slots directly...");

            // โหลดแต่ละ slot โดยไม่ให้ไปใน inventory
            if (LoadSingleEquipmentSlotDirectly(character, ItemType.Head, equipment.headItemId))
                loadedCount++;
            if (LoadSingleEquipmentSlotDirectly(character, ItemType.Armor, equipment.armorItemId))
                loadedCount++;
            if (LoadSingleEquipmentSlotDirectly(character, ItemType.Weapon, equipment.weaponItemId))
                loadedCount++;
            if (LoadSingleEquipmentSlotDirectly(character, ItemType.Pants, equipment.pantsItemId))
                loadedCount++;
            if (LoadSingleEquipmentSlotDirectly(character, ItemType.Shoes, equipment.shoesItemId))
                loadedCount++;
            if (LoadSingleEquipmentSlotDirectly(character, ItemType.Rune, equipment.runeItemId))
                loadedCount++;

            Debug.Log($"[LoadEquipmentSlotsDirectly] ✅ Loaded {loadedCount}/6 equipment pieces");
            return loadedCount > 0;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LoadEquipmentSlotsDirectly] ❌ Error: {e.Message}");
            return false;
        }
    }

    private bool LoadSingleEquipmentSlotDirectly(Character character, ItemType itemType, string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return false;

        try
        {
            ItemData itemData = GetItemDataById(itemId);
            if (itemData == null)
            {
                Debug.LogError($"[LoadSingleEquipmentSlotDirectly] Item not found: {itemId} for {itemType}");
                return false;
            }

            if (itemData.ItemType != itemType)
            {
                Debug.LogError($"[LoadSingleEquipmentSlotDirectly] Item type mismatch: {itemData.ItemType} != {itemType}");
                return false;
            }

            // โหลดโดยตรงไปยัง equipment slot (ไม่ผ่าน inventory)
            bool success = character.LoadEquipmentDirectly(itemData);
            if (success)
            {
                Debug.Log($"[LoadSingleEquipmentSlotDirectly] ✅ Loaded {itemData.ItemName} directly to {itemType} slot");
                return true;
            }

            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LoadSingleEquipmentSlotDirectly] ❌ Error: {e.Message}");
            return false;
        }
    }

    private bool LoadPotionSlotsDirectly(Character character, CharacterEquipmentData equipmentData)
    {
        try
        {
            int loadedCount = 0;

            Debug.Log("[LoadPotionSlotsDirectly] Loading potion slots directly...");

            for (int i = 0; i < 5; i++)
            {
                var potionSlot = equipmentData.GetPotionSlot(i);
                if (potionSlot?.IsValid() == true)
                {
                    ItemData potionData = GetItemDataById(potionSlot.itemId);
                    if (potionData?.ItemType == ItemType.Potion)
                    {
                        // โหลดโดยตรงไปยัง potion slot (ไม่ผ่าน inventory)
                        bool loaded = character.LoadPotionDirectly(potionData, i, potionSlot.stackCount);
                        if (loaded)
                        {
                            Debug.Log($"[LoadPotionSlotsDirectly] ✅ Loaded {potionData.ItemName} x{potionSlot.stackCount} to slot {i}");
                            loadedCount++;
                        }
                    }
                }
            }

            Debug.Log($"[LoadPotionSlotsDirectly] ✅ Loaded {loadedCount}/5 potion slots");
            return loadedCount > 0;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LoadPotionSlotsDirectly] ❌ Error: {e.Message}");
            return false;
        }
    }

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

            // 🆕 **FIX**: อนุญาตให้ save เมื่อเป็นการ equip item
            bool isEquipAction = action.Contains("Equip") || action.Contains("Equipment");

            if (currentItems == 0 && !isEquipAction && action != "Clear Inventory")
            {
                bool hasFirebaseData = multiCharacterData.sharedInventory?.items?.Count > 0;
                if (hasFirebaseData)
                {
                    Debug.LogWarning($"[SafeAutoSaveInventory] ⚠️ Preventing save of empty inventory when Firebase has data!");
                    Debug.LogWarning($"[SafeAutoSaveInventory] ❌ BLOCKED: Attempt to save empty inventory when Firebase has data!");
                    return;
                }
            }

            Debug.Log($"[SafeAutoSaveInventory] 💾 Safe saving inventory with {currentItems} items (Action: {action})");

            // เก็บ backup ก่อน save
            var backupData = CreateInventoryBackupData(inventory);

            // Save ปกติ
            SaveInventoryData(character);

            // Validate ว่า save สำเร็จ

        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SafeAutoSaveInventory] ❌ Error: {e.Message}");
        }
    }
    public void ForceSaveAfterEquipItem(Character character)
    {
        if (character == null || multiCharacterData == null)
        {
            Debug.LogError("[ForceSaveAfterEquipItem] Cannot save - missing components");
            return;
        }

        try
        {
            Debug.Log("[ForceSaveAfterEquipItem] 🚀 Force saving after equipment change...");

            // บันทึก inventory และ equipment ทันที (ไม่ผ่าน safety check)
            SaveInventoryData(character);
            SaveEquippedItemsOnly(character);

            Debug.Log("[ForceSaveAfterEquipItem] ✅ Force save completed");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ForceSaveAfterEquipItem] ❌ Error: {e.Message}");
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

    public void SaveInventoryDataSafe(Character character, string action = "Manual Save")
    {
        if (character == null || multiCharacterData == null)
        {
            Debug.LogWarning($"[SaveInventoryDataSafe] Cannot save - missing components");
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

            // 🆕 **ปรับปรุง safety check - อนุญาต save เมื่อ equip item**
            bool isEquipAction = action.Contains("Equip") || action.Contains("Equipment") || action.Contains("Force Save");

            if (itemsInInventory == 0 && !isEquipAction && action != "Clear Inventory" && action != "Emergency Clear")
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
    public void ForceSaveInventoryAfterEquip(Character character, string action = "Force Save After Equip")
    {
        if (character == null || multiCharacterData == null)
        {
            Debug.LogWarning($"[ForceSaveInventoryAfterEquip] Cannot save - missing components");
            return;
        }

        try
        {
            var inventory = character.GetInventory();
            if (inventory == null)
            {
                Debug.LogWarning($"[ForceSaveInventoryAfterEquip] Character has no inventory");
                return;
            }

            int currentItems = inventory.UsedSlots;

            Debug.Log($"[ForceSaveInventoryAfterEquip] 🚀 FORCE saving inventory with {currentItems} items (Action: {action})");
            Debug.Log($"[ForceSaveInventoryAfterEquip] ⚠️ BYPASSING all safety checks for equipment action");

            // 🔑 **ไม่มี safety check - บังคับ save เลย**
            SaveInventoryData(character);

            Debug.Log($"[ForceSaveInventoryAfterEquip] ✅ Force save completed without safety checks");

        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ForceSaveInventoryAfterEquip] ❌ Error: {e.Message}");
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
   

    // 🆕 เพิ่ม method สำหรับ restore inventory
   

    /// <summary>
    /// โหลดไอเทมเดียวลง inventory
    /// </summary>
   


    /// <summary>
    /// โหลดข้อมูล equipment และ potion ของ character
    /// </summary>
   


  

   

  
    
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
            return;
        }


        // Equipment slots
        int equippedCount = 0;
        if (!string.IsNullOrEmpty(equipmentData.equipment.headItemId))
        {
            equippedCount++;
        }
        if (!string.IsNullOrEmpty(equipmentData.equipment.armorItemId))
        {
            equippedCount++;
        }
        if (!string.IsNullOrEmpty(equipmentData.equipment.weaponItemId))
        {
            equippedCount++;
        }
        if (!string.IsNullOrEmpty(equipmentData.equipment.pantsItemId))
        {
            equippedCount++;
        }
        if (!string.IsNullOrEmpty(equipmentData.equipment.shoesItemId))
        {
            equippedCount++;
        }
        if (!string.IsNullOrEmpty(equipmentData.equipment.runeItemId))
        {
            equippedCount++;
        }

        // Potion slots
        int potionCount = 0;
        for (int i = 0; i < equipmentData.potionSlots.Count; i++)
        {
            var potion = equipmentData.potionSlots[i];
            if (!potion.IsEmpty())
            {
                potionCount++;
            }
        }

      
    }

    /// <summary>
    /// แสดงสถิติการโหลดทั้งหมด
    /// </summary>
   

    /// <summary>
    /// ตรวจสอบสถานะ ItemDatabase
    /// </summary>
 
    /// <summary>
    /// ทดสอบการหา item โดยใช้ ID
    /// </summary>
   
    /// <summary>
    /// แสดงรายการ ItemID ทั้งหมดใน database (สำหรับ debug)
    /// </summary>
 

    /// <summary>
    /// แสดงข้อมูลการโหลดสำหรับ character ปัจจุบัน
    /// </summary>
    

    #endregion
  

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

    /// <summary>
    /// ดึง Player Level ปัจจุบัน
    /// </summary>
    public int GetPlayerLevel()
    {
        return multiCharacterData?.playerLevel ?? 1;
    }

    /// <summary>
    /// ดึง Player EXP ปัจจุบัน
    /// </summary>
    public int GetPlayerExp()
    {
        return multiCharacterData?.playerExp ?? 0;
    }

    /// <summary>
    /// ดึง Player EXP ที่ต้องการเพื่อ Level ถัดไป
    /// </summary>
    public int GetPlayerExpToNext()
    {
        return multiCharacterData?.playerExpToNext ?? 100;
    }

    /// <summary>
    /// ตรวจสอบว่ามีข้อมูล Player Level หรือไม่
    /// </summary>
    public bool HasPlayerLevelData()
    {
        return multiCharacterData?.HasPlayerLevelData() ?? false;
    }
    /// <summary>
    /// ดึง potion stack counts list จาก Character (private field)
    /// </summary>
    /// <summary>
    /// ตรวจสอบว่าด่านนี้ผ่านครั้งแรกหรือไม่
    /// </summary>
    public bool IsFirstClearStage(string stageName)
    {
        return multiCharacterData?.IsFirstClearStage(stageName) ?? true;
    }

    /// <summary>
    /// ดึงจำนวนด่านที่ First Clear แล้ว
    /// </summary>
    public int GetTotalFirstClearStages()
    {
        return multiCharacterData?.totalStageFirstClears ?? 0;
    }

    /// <summary>
    /// ดึงจำนวนเพชรรวมจาก First Clear
    /// </summary>
    public int GetTotalFirstClearGemsEarned()
    {
        return multiCharacterData?.totalFirstClearBonusGems ?? 0;
    }

    #endregion
    public CharacterProgressData GetOrCreateCharacterData(string characterType)
    {
        // ✅ ตรวจสอบว่ามี character อยู่แล้วหรือไม่
        CharacterProgressData existing = GetCharacterData(characterType);
        if (existing != null)
        {
            Debug.Log($"[GetOrCreateCharacterData] Character {characterType} already exists");
            return existing;
        }

        // ✅ สร้างตัวละครใหม่
        Debug.Log($"[GetOrCreateCharacterData] Creating new character: {characterType}");
        CharacterProgressData newCharacter = CreateDefaultCharacterData(characterType);

        if (multiCharacterData == null)
        {
            Debug.LogError("[GetOrCreateCharacterData] multiCharacterData is null!");
            return null;
        }

        if (multiCharacterData.characters == null)
        {
            multiCharacterData.characters = new List<CharacterProgressData>();
        }

        multiCharacterData.characters.Add(newCharacter);

        Debug.Log($"✅ Created new character {characterType}. Total characters: {multiCharacterData.characters.Count}");

        return newCharacter;
    }

    public CharacterProgressData CreateDefaultCharacterData(string characterType)
    {
        Debug.Log($"[CreateDefaultCharacterData] 🔨 Creating character: {characterType}");

        CharacterProgressData newCharacter = new CharacterProgressData(characterType);
        newCharacter.currentLevel = 1;
        newCharacter.currentExp = 0;
        newCharacter.expToNextLevel = 100;
        newCharacter.AddStatPoints(1);

        CharacterStats characterStats = null;

        // ✅ เพิ่ม debug path
        string resourcePath = "";

        switch (characterType)
        {
            case "BloodKnight":
                resourcePath = "Characters/BloodKnightStats";
                characterStats = Resources.Load<CharacterStats>(resourcePath);
                Debug.Log($"[CreateDefaultCharacterData] 🔍 Loading BloodKnight from: {resourcePath}");
                Debug.Log($"[CreateDefaultCharacterData] Result: {(characterStats != null ? "SUCCESS ✅" : "FAILED ❌")}");
                break;
            case "Archer":
                resourcePath = "Characters/ArcherStats";
                characterStats = Resources.Load<CharacterStats>(resourcePath);
                Debug.Log($"[CreateDefaultCharacterData] 🔍 Loading Archer from: {resourcePath}");
                Debug.Log($"[CreateDefaultCharacterData] Result: {(characterStats != null ? "SUCCESS ✅" : "FAILED ❌")}");
                break;
            case "Assassin":
                resourcePath = "Characters/AssassinStats";
                characterStats = Resources.Load<CharacterStats>(resourcePath);
                Debug.Log($"[CreateDefaultCharacterData] 🔍 Loading Assassin from: {resourcePath}");
                Debug.Log($"[CreateDefaultCharacterData] Result: {(characterStats != null ? "SUCCESS ✅" : "FAILED ❌")}");
                break;
            case "IronJuggernaut":
                resourcePath = "Characters/IronJuggernautStats";
                characterStats = Resources.Load<CharacterStats>(resourcePath);
                Debug.Log($"[CreateDefaultCharacterData] 🔍 Loading IronJuggernaut from: {resourcePath}");
                Debug.Log($"[CreateDefaultCharacterData] Result: {(characterStats != null ? "SUCCESS ✅" : "FAILED ❌")}");
                break;
        }

        // ✅ ถ้าโหลด ScriptableObject ได้ ให้ copy stats
        if (characterStats != null)
        {
            newCharacter.baseMaxHp = characterStats.maxHp;
            newCharacter.baseMaxMana = characterStats.maxMana;
            newCharacter.baseAttackDamage = characterStats.attackDamage;
            newCharacter.baseMagicDamage = characterStats.magicDamage;
            newCharacter.baseArmor = characterStats.arrmor;
            newCharacter.baseCriticalChance = characterStats.criticalChance;
            newCharacter.baseCriticalDamageBonus = characterStats.criticalDamageBonus;
            newCharacter.baseMoveSpeed = characterStats.moveSpeed;
            newCharacter.baseHitRate = characterStats.hitRate;
            newCharacter.baseEvasionRate = characterStats.evasionRate;
            newCharacter.baseAttackSpeed = characterStats.attackSpeed;
            newCharacter.baseReductionCoolDown = characterStats.reductionCoolDown;
            newCharacter.baseLifeSteal = characterStats.lifeSteal;
            newCharacter.baseAmpDamage = characterStats.ampdamage;
            newCharacter.baseMagicArmor = characterStats.magicArmor;
            newCharacter.baseHealthRegen = characterStats.healthRegen;
            newCharacter.baseManaRegen = characterStats.manaRegen;
            newCharacter.baseAttackCoolDown = characterStats.attackCoolDown;
            newCharacter.baseAttackRange = characterStats.attackRange;
            newCharacter.attackType = characterStats.attackType;

            // ✅ อัปเดต total stats ด้วย
            newCharacter.UpdateTotalStats();

            Debug.Log($"[CreateDefaultCharacterData] ✅ Stats loaded from ScriptableObject: HP={newCharacter.totalMaxHp}, ATK={newCharacter.totalAttackDamage}");
        }
        else
        {
            // ✅ ถ้าโหลดไม่ได้ ใช้ default stats
            Debug.LogError($"[CreateDefaultCharacterData] ❌ ScriptableObject not found at path: {resourcePath}");
            Debug.LogWarning($"[CreateDefaultCharacterData] ⚠️ Using default stats for {characterType}");
            SetDefaultStatsForCharacter(newCharacter, characterType);
        }

        return newCharacter;
    }

    // ✅ เพิ่ม method สำหรับ set default stats (กรณี ScriptableObject โหลดไม่ได้)
    private void SetDefaultStatsForCharacter(CharacterProgressData character, string characterType)
    {
        switch (characterType)
        {
            case "BloodKnight":
                character.baseMaxHp = 150;
                character.baseMaxMana = 80;
                character.baseAttackDamage = 25;
                character.baseMagicDamage = 15;
                character.baseArmor = 10;
                character.baseMoveSpeed = 3.0f;
                break;

            case "Archer":
                character.baseMaxHp = 90;
                character.baseMaxMana = 60;
                character.baseAttackDamage = 30;
                character.baseMagicDamage = 10;
                character.baseArmor = 5;
                character.baseMoveSpeed = 3.5f;
                break;

            case "Assassin":
                character.baseMaxHp = 100;
                character.baseMaxMana = 50;
                character.baseAttackDamage = 35;
                character.baseMagicDamage = 10;
                character.baseArmor = 5;
                character.baseMoveSpeed = 4.0f;
                break;

            case "IronJuggernaut":
                character.baseMaxHp = 200;
                character.baseMaxMana = 70;
                character.baseAttackDamage = 20;
                character.baseMagicDamage = 12;
                character.baseArmor = 15;
                character.baseMoveSpeed = 2.5f;
                break;
        }

        // Stats ที่เหมือนกันทุกตัว
        character.baseCriticalChance = 5f;
        character.baseCriticalDamageBonus = 150f;
        character.baseHitRate = 85f;
        character.baseEvasionRate = 5f;
        character.baseAttackSpeed = 1f;
        character.baseReductionCoolDown = 0f;
        character.baseLifeSteal = 0f;
        character.baseMagicArmor = 0;
        character.baseHealthRegen = 0.5f;
        character.baseManaRegen = 1f;
        character.attackType = AttackType.Physical;

        character.UpdateTotalStats();

        Debug.Log($"[SetDefaultStatsForCharacter] ✅ Default stats set: HP={character.totalMaxHp}, ATK={character.totalAttackDamage}");
    }

    // ✅ เพิ่ม method ใหม่สำหรับโหลด character stats อย่างปลอดภัย
    private void TryLoadCharacterStats(CharacterProgressData character, string characterType)
    {
        try
        {
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
                // ✅ Copy stats จาก ScriptableObject
                character.baseMaxHp = characterStats.maxHp;
                character.baseMaxMana = characterStats.maxMana;
                character.baseAttackDamage = characterStats.attackDamage;
                character.baseMagicDamage = characterStats.magicDamage;
                character.baseArmor = characterStats.arrmor;
                character.baseCriticalChance = characterStats.criticalChance;
                character.baseCriticalDamageBonus = characterStats.criticalDamageBonus;
                character.baseMoveSpeed = characterStats.moveSpeed;
                character.baseHitRate = characterStats.hitRate;
                character.baseEvasionRate = characterStats.evasionRate;
                character.baseAttackSpeed = characterStats.attackSpeed;
                character.baseReductionCoolDown = characterStats.reductionCoolDown;
                character.baseLifeSteal = characterStats.lifeSteal;
                character.baseAmpDamage = characterStats.ampdamage;
                character.baseMagicArmor = characterStats.magicArmor;
                character.baseHealthRegen = characterStats.healthRegen;
                character.baseManaRegen = characterStats.manaRegen;
                character.attackType = characterStats.attackType;

                // Copy to total stats
                character.totalMaxHp = characterStats.maxHp;
                character.totalMaxMana = characterStats.maxMana;
                character.totalAttackDamage = characterStats.attackDamage;
                character.totalMagicDamage = characterStats.magicDamage;
                character.totalArmor = characterStats.arrmor;
                character.totalCriticalChance = characterStats.criticalChance;
                character.totalCriticalDamageBonus = characterStats.criticalDamageBonus;
                character.totalMoveSpeed = characterStats.moveSpeed;
                character.totalHitRate = characterStats.hitRate;
                character.totalEvasionRate = characterStats.evasionRate;
                character.totalAttackSpeed = characterStats.attackSpeed;
                character.totalReductionCoolDown = characterStats.reductionCoolDown;
                character.totalLifeSteal = characterStats.lifeSteal;
                character.totalAmpdamage = characterStats.ampdamage;
                character.totalMagicArmor = characterStats.magicArmor;
                character.totalHealthRegen = characterStats.healthRegen;
                character.totalManaRegen = characterStats.manaRegen;

                Debug.Log($"✅ Loaded {characterType} stats from ScriptableObject");
            }
            else
            {
                Debug.LogWarning($"⚠️ Could not load {characterType} stats, using defaults");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error loading {characterType} stats: {e.Message}");
        }
    }

    // ✅ เพิ่ม method สำหรับตั้ง default stats
   
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

            // ✅ คำนวณ base stats (ScriptableObject + Level bonuses + Stat upgrades)
            CalculateAndSaveBaseStats(character, levelManager, characterData);

            // ✅ บันทึก total stats (base + equipment bonuses) แยกต่างหาก
            SaveCurrentTotalStats(character, characterData);

            // บันทึกลง Firebase
            SavePlayerDataAsync();

            Debug.Log($"[PersistentPlayerData] ✅ Saved both base and total stats for {characterType}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PersistentPlayerData] ❌ Error saving base stats: {e.Message}");
        }
    }
    private void CalculateAndSaveBaseStats(Character character, LevelManager levelManager, CharacterProgressData characterData)
    {
        if (character.characterStats == null)
        {
            Debug.LogError("[PersistentPlayerData] No characterStats ScriptableObject!");
            return;
        }

        // ✅ คำนวณ base stats จาก ScriptableObject + Level bonuses เท่านั้น
        int levelBonus = levelManager.CurrentLevel - 1;

        int baseHp = character.characterStats.maxHp + (levelBonus * levelManager.levelUpStats.hpBonusPerLevel);
        int baseMana = character.characterStats.maxMana + (levelBonus * levelManager.levelUpStats.manaBonusPerLevel);
        int baseAttack = character.characterStats.attackDamage + (levelBonus * levelManager.levelUpStats.attackDamageBonusPerLevel);
        int baseMagic = character.characterStats.magicDamage + (levelBonus * levelManager.levelUpStats.magicDamageBonusPerLevel);
        int baseArmor = character.characterStats.arrmor + (levelBonus * levelManager.levelUpStats.armorBonusPerLevel);
        float baseCrit = character.characterStats.criticalChance + (levelBonus * levelManager.levelUpStats.criticalChanceBonusPerLevel);
        float baseCritDmg = character.characterStats.criticalDamageBonus;
        float baseSpeed = character.characterStats.moveSpeed + (levelBonus * levelManager.levelUpStats.moveSpeedBonusPerLevel);
        float baseHit = character.characterStats.hitRate;
        float baseEvasion = character.characterStats.evasionRate;
        float baseAtkSpeed = character.characterStats.attackSpeed;
        float baseCdr = character.characterStats.reductionCoolDown;
        float baseLifeSteal = character.characterStats.lifeSteal ;
        float baseAmpDamage = character.characterStats.ampdamage ;

        // ✅ เพิ่ม 3 stats ใหม่ จาก ScriptableObject
        int baseMagicArmor = character.characterStats.magicArmor + (levelBonus * levelManager.levelUpStats.magicArmorBonusPerLevel);
        float baseHealthRegen = character.characterStats.healthRegen;
        float baseManaRegen = character.characterStats.manaRegen;

        // ✅ เพิ่ม stat bonuses จาก upgrades
        characterData.GetStatBonuses(out int hpBonus, out int atkBonus, out float critDmgBonus,
                                   out float atkSpeedBonus, out float evaBonus, out float critChanceBonus,
                                   out int magicBonus, out int manaBonus, out float cdrBonus,
                                   out float hitBonus, out float lifeStealBonus, out float speedBonus);

        // รวม bonuses เข้ากับ base stats
        baseHp += hpBonus;
        baseMana += manaBonus;
        baseAttack += atkBonus;
        baseMagic += magicBonus;
        baseCrit += critChanceBonus;
        baseCritDmg += critDmgBonus;
        baseSpeed += speedBonus;
        baseAtkSpeed += atkSpeedBonus;
        baseEvasion += evaBonus;
        baseCdr += cdrBonus;
        baseHit += hitBonus;
        baseLifeSteal += lifeStealBonus;

        // ✅ บันทึก base stats ลง characterData (รวม 3 stats ใหม่)
        characterData.UpdateBaseStats(
            baseHp, baseMana, baseAttack, baseMagic, baseArmor,
            baseCrit, baseCritDmg, baseSpeed, baseHit, baseEvasion,
            baseAtkSpeed, baseCdr, baseLifeSteal, baseMagicArmor, baseHealthRegen, baseManaRegen,baseAmpDamage
        );

        
    }
    private void SaveCurrentTotalStats(Character character, CharacterProgressData characterData)
    {
        // บันทึก total stats (base + equipment bonuses)
        characterData.UpdateTotalStats(
            character.MaxHp, character.MaxMana, character.AttackDamage, character.MagicDamage, character.Armor,
            character.CriticalChance, character.CriticalDamageBonus, character.MoveSpeed,
            character.HitRate, character.EvasionRate, character.AttackSpeed, character.ReductionCoolDown,
            character.LifeSteal, character.MagicArmor, character.HealthRegen, character.ManaRegen,character.AmpDamage // ✅ เพิ่ม 3 stats ใหม่
        );

       
    }

    // ✅ เพิ่ม method สำหรับบันทึก total stats แยกต่างหาก
   
    
  
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
   


    #endregion

   

    
    internal void CheckFirebaseStatus()
    {
        throw new System.NotImplementedException();
    }
    #endregion
}