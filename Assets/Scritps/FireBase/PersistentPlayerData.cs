using UnityEngine;
using Firebase.Auth;
using Firebase.Database;
using System.Collections;
using System.Collections.Generic;

public class PersistentPlayerData : MonoBehaviour
{
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

    // ========== Helper Methods ==========
    public CharacterProgressData GetCurrentCharacterData()
    {
        if (multiCharacterData == null) return null;
        return multiCharacterData.GetActiveCharacterData();
    }

    public string GetPlayerName()
    {
        return multiCharacterData?.playerName ?? "Player";
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
        if (multiCharacterData == null) return null;
        return multiCharacterData.GetCharacterData(characterType);
    }

    public List<CharacterProgressData> GetAllCharacterData()
    {
        if (multiCharacterData == null) return new List<CharacterProgressData>();
        return multiCharacterData.characters;
    }

    // ========== Load/Save Methods ==========
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
        }
        else
        {
            Debug.Log("[PersistentPlayerData] No data found. Creating default data...");
            CreateDefaultMultiCharacterData();
        }

        RegisterPlayerInDirectory();
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
    public void SendFriendRequestByUserId(string targetUserId)
    {
        if (!IsFirebaseReady()) return;
        StartCoroutine(SendFriendRequestByUserIdCoroutine(targetUserId));
    }
    private void CreateDefaultMultiCharacterData()
    {
        multiCharacterData = new MultiCharacterPlayerData();
        multiCharacterData.playerName = PlayerPrefs.GetString("PlayerName", "Player");
        multiCharacterData.currentActiveCharacter = "Assassin";

        isDataLoaded = true;
        SaveToPlayerPrefs();

        Debug.Log($"✅ Created default multi-character data with Assassin for {multiCharacterData.playerName}");
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

    // ========== Character Management ==========
    public void SwitchCharacter(string characterType)
    {
        if (multiCharacterData == null) return;

        multiCharacterData.SwitchActiveCharacter(characterType);
        SaveToPlayerPrefs();
        SavePlayerDataAsync();

        Debug.Log($"✅ Switched to character: {characterType}");
    }

    public void UpdateLevelAndStats(int level, int exp, int expToNext, int maxHp, int maxMana,
     int attackDamage, int magicDamage, int armor, float critChance, float critDamageBonus, float moveSpeed,
     float hitRate, float evasionRate, float attackSpeed, float reductionCoolDown)
    {
        if (multiCharacterData == null) return;

        multiCharacterData.UpdateCharacterStats(
            multiCharacterData.currentActiveCharacter,
            level, exp, expToNext, maxHp, maxMana, attackDamage, magicDamage, armor,
            critChance, critDamageBonus, moveSpeed, hitRate, evasionRate, attackSpeed, reductionCoolDown);

        SaveToPlayerPrefs();
        SavePlayerDataAsync();
    }

    // ========== Public Methods ==========
    public bool HasValidData()
    {
        return isDataLoaded &&
               multiCharacterData != null &&
               !string.IsNullOrEmpty(multiCharacterData.playerName) &&
               GetCurrentCharacterData() != null;
    }


    #region Friends
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

    internal void UpdateLevelAndStats(int currentLevel, int currentExp, int expToNextLevel, int maxHp, int maxMana, int attackDamage, int magicDamage, int armor, float criticalChance, float criticalmulti , float criticalMultiplier, float moveSpeed, float hitRate, float evasionRate, float attackSpeed, float reductionCoolDown)
    {
        throw new System.NotImplementedException();
    }

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

    // ========== Load Friend Requests from Firebase ==========
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
    #endregion
    public void ForceSave() => SavePlayerDataAsync();

    // ========== Debug Methods ==========

    [ContextMenu("Debug All Players")]
    public void DebugAllPlayers()
    {
        StartCoroutine(DebugAllPlayersCoroutine());
    }

    private IEnumerator DebugAllPlayersCoroutine()
    {
        Debug.Log("🔍 Fetching all players from Firebase...");

        var task = databaseReference.Child("players").GetValueAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError($"❌ Error: {task.Exception.Message}");
            yield break;
        }

        if (!task.Result.Exists)
        {
            Debug.Log("❌ No players found!");
            yield break;
        }

        Debug.Log($"📊 Found {task.Result.ChildrenCount} players:");

        foreach (var player in task.Result.Children)
        {
            var playerData = player;
            string userId = player.Key;

            // แสดงข้อมูลแบบละเอียด
            Debug.Log($"\n👤 Player: {userId}");

            if (playerData.HasChild("playerName"))
            {
                string playerName = playerData.Child("playerName").Value?.ToString();
                Debug.Log($"   📝 Name: '{playerName}'");
            }
            else
            {
                Debug.Log($"   ❌ No playerName field");
            }

            // แสดง JSON structure
            string json = playerData.GetRawJsonValue();
            if (!string.IsNullOrEmpty(json) && json.Length < 500)
            {
                Debug.Log($"   📄 Data: {json}");
            }
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

    internal void CheckFirebaseStatus()
    {
        throw new System.NotImplementedException();
    }
}