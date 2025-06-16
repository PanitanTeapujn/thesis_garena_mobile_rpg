using UnityEngine;
using Firebase.Auth;
using Firebase.Database;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ✅ Fixed PersistentPlayerData - ใช้ MultiCharacterPlayerData เป็นหลัก
/// </summary>
public class PersistentPlayerData : MonoBehaviour
{
    [Header("Multi-Character Data")]
    public MultiCharacterPlayerData multiCharacterData;
    public bool isDataLoaded = false;

    // ✅ เก็บไว้เพื่อ backward compatibility แต่จะถูกอัพเดทอัตโนมัติ
    [Header("Legacy Support (Auto-Updated)")]
    public PlayerProgressData currentPlayerData;

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

    private FirebaseAuth auth;
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

    // ========== NON-BLOCKING Load ==========
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

            // Try to load as MultiCharacterPlayerData first
            try
            {
                multiCharacterData = JsonUtility.FromJson<MultiCharacterPlayerData>(json);
                if (multiCharacterData != null && multiCharacterData.IsValid())
                {
                    // ✅ อัพเดท currentPlayerData อัตโนมัติ
                    SyncCurrentPlayerData();

                    isDataLoaded = true;
                    loaded = true;

                    Debug.Log($"✅ Loaded multi-character data: {multiCharacterData.playerName}, Active: {multiCharacterData.currentActiveCharacter}");
                    SaveToPlayerPrefs();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[PersistentPlayerData] Failed to parse as MultiCharacterPlayerData: {e.Message}");
            }

            // If not loaded, try old format conversion
            if (!loaded)
            {
                yield return StartCoroutine(ConvertOldDataToMultiCharacter(json));
            }
        }
        else
        {
            Debug.Log("[PersistentPlayerData] No data found or timeout. Creating default multi-character data...");
            CreateDefaultMultiCharacterData();
        }
    }

    private IEnumerator ConvertOldDataToMultiCharacter(string json)
    {
        try
        {
            // Try to convert from old PlayerProgressData
            PlayerProgressData oldData = JsonUtility.FromJson<PlayerProgressData>(json);

            if (oldData != null && oldData.IsValid())
            {
                Debug.Log($"[PersistentPlayerData] Converting old single-character data to multi-character format");

                // Create new multi-character data
                multiCharacterData = new MultiCharacterPlayerData();
                multiCharacterData.playerName = oldData.playerName;
                multiCharacterData.registrationDate = oldData.registrationDate;
                multiCharacterData.lastLoginDate = oldData.lastLoginDate;

                // Set active character from old data
                string oldCharacterType = oldData.lastCharacterSelected;
                if (string.IsNullOrEmpty(oldCharacterType)) oldCharacterType = "Assassin";

                multiCharacterData.currentActiveCharacter = oldCharacterType;

                // Convert old character data
                CharacterProgressData convertedCharacter = new CharacterProgressData();
                convertedCharacter.characterType = oldCharacterType;
                convertedCharacter.currentLevel = oldData.currentLevel;
                convertedCharacter.currentExp = oldData.currentExp;
                convertedCharacter.expToNextLevel = oldData.expToNextLevel;
                convertedCharacter.totalMaxHp = oldData.totalMaxHp;
                convertedCharacter.totalMaxMana = oldData.totalMaxMana;
                convertedCharacter.totalAttackDamage = oldData.totalAttackDamage;
                convertedCharacter.totalArmor = oldData.totalArmor;
                convertedCharacter.totalCriticalChance = oldData.totalCriticalChance;
                convertedCharacter.totalCriticalMultiplier = oldData.totalCriticalMultiplier;
                convertedCharacter.totalMoveSpeed = oldData.totalMoveSpeed;
                convertedCharacter.totalAttackRange = oldData.totalAttackRange;
                convertedCharacter.totalAttackCooldown = oldData.totalAttackCooldown;
                convertedCharacter.totalHitRate = oldData.totalHitRate;
                convertedCharacter.totalEvasionRate = oldData.totalEvasionRate;
                convertedCharacter.totalAttackSpeed = oldData.totalAttackSpeed;

                // ✅ ถ้า old data ไม่มี accuracy stats ให้โหลดจาก ScriptableObject
                if (convertedCharacter.totalHitRate == 0 || convertedCharacter.totalEvasionRate == 0 || convertedCharacter.totalAttackSpeed == 0)
                {
                    CharacterStats characterStats = GetCharacterStatsForCharacterType(oldCharacterType);
                    if (characterStats != null)
                    {
                        convertedCharacter.totalHitRate = characterStats.hitRate;
                        convertedCharacter.totalEvasionRate = characterStats.evasionRate;
                        convertedCharacter.totalAttackSpeed = characterStats.attackSpeed;
                        Debug.Log($"✅ Applied missing accuracy stats from ScriptableObject for {oldCharacterType}");
                    }
                }

                // Remove default Assassin if we're converting different character
                if (oldCharacterType != "Assassin")
                {
                    multiCharacterData.characters.Clear();
                    multiCharacterData.characters.Add(convertedCharacter);

                    // Add default Assassin as well
                    CharacterProgressData defaultAssassin = multiCharacterData.CreateDefaultCharacterData("Assassin");
                    multiCharacterData.characters.Add(defaultAssassin);
                }
                else
                {
                    // Replace default Assassin with converted data
                    multiCharacterData.characters[0] = convertedCharacter;
                }

                // ✅ อัพเดท currentPlayerData
                SyncCurrentPlayerData();

                isDataLoaded = true;
                SaveToPlayerPrefs();
                SavePlayerDataAsync(); // Save new format to Firebase

                Debug.Log($"✅ Successfully converted old data to multi-character format");
                multiCharacterData.LogAllCharacters();

                yield break;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[PersistentPlayerData] Failed to convert old data: {e.Message}");
        }

        // If conversion failed, create default
        CreateDefaultMultiCharacterData();
    }

    // ✅ Helper method to get CharacterStats
    private CharacterStats GetCharacterStatsForCharacterType(string characterType)
    {
        switch (characterType)
        {
            case "BloodKnight":
                return Resources.Load<CharacterStats>("Characters/BloodKnightStats");
            case "Archer":
                return Resources.Load<CharacterStats>("Characters/ArcherStats");
            case "Assassin":
                return Resources.Load<CharacterStats>("Characters/AssassinStats");
            case "IronJuggernaut":
                return Resources.Load<CharacterStats>("Characters/IronJuggernautStats");
            default:
                return null;
        }
    }

    private void CreateDefaultMultiCharacterData()
    {
        multiCharacterData = new MultiCharacterPlayerData();
        multiCharacterData.playerName = PlayerPrefs.GetString("PlayerName", "Player");
        multiCharacterData.currentActiveCharacter = "Assassin";

        // ✅ อัพเดท currentPlayerData
        SyncCurrentPlayerData();

        isDataLoaded = true;
        SaveToPlayerPrefs();

        Debug.Log($"✅ Created default multi-character data with Assassin for {multiCharacterData.playerName}");
    }

    // ✅ Sync currentPlayerData จาก multiCharacterData
    private void SyncCurrentPlayerData()
    {
        if (multiCharacterData == null) return;

        CharacterProgressData activeCharacter = multiCharacterData.GetActiveCharacterData();
        if (activeCharacter != null)
        {
            currentPlayerData = activeCharacter.ToPlayerProgressData(multiCharacterData.playerName);
            Debug.Log($"🔄 Synced currentPlayerData for {multiCharacterData.currentActiveCharacter}");
        }
    }

    // ========== NON-BLOCKING Save ==========
    public void SavePlayerDataAsync()
    {
        // ✅ เปลี่ยนเป็นเช็ค multiCharacterData แทน
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

        // Save current character progress
        SaveCurrentCharacterProgress();

        // Update timestamps
        multiCharacterData.lastLoginDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Save multi-character data to Firebase
        string json = JsonUtility.ToJson(multiCharacterData, true);
        var task = databaseReference.Child("players").Child(auth.CurrentUser.UserId).SetRawJsonValueAsync(json);

        // Wait a bit to ensure save starts
        yield return new WaitForSeconds(0.5f);

        SaveToPlayerPrefs();
        Debug.Log($"💾 Saving multi-character data for {multiCharacterData.playerName} (async)");

        // Check if save completed
        if (task.IsCompleted)
        {
            if (task.Exception != null)
            {
                Debug.LogError($"❌ Failed to save to Firebase: {task.Exception.Message}");
            }
            else
            {
                Debug.Log($"✅ Successfully saved to Firebase");
            }
        }
    }

    // ========== Quick Update ==========
    public void UpdateLevelAndStats(int level, int exp, int expToNext, int maxHp, int maxMana,
        int attackDamage, int armor, float critChance, float moveSpeed,
        float hitRate, float evasionRate, float attackSpeed)
    {
        if (multiCharacterData == null) return;

        // Update ตัวละครปัจจุบันใน MultiCharacterPlayerData
        multiCharacterData.UpdateCharacterStats(
            multiCharacterData.currentActiveCharacter,
            level, exp, expToNext, maxHp, maxMana, attackDamage, armor,
            critChance, moveSpeed, hitRate, evasionRate, attackSpeed);

        // ✅ อัพเดท currentPlayerData ด้วย
        SyncCurrentPlayerData();

        SaveToPlayerPrefs();
        SavePlayerDataAsync();

        Debug.Log($"📊 Updated stats for {multiCharacterData.currentActiveCharacter}: Level {level}");
    }

    // ========== PlayerPrefs Cache ==========
    private void SaveToPlayerPrefs()
    {
        if (multiCharacterData == null) return;

        var currentCharacter = GetCurrentCharacterData();
        if (currentCharacter == null)
        {
            Debug.LogWarning("[PersistentPlayerData] No current character data to save to PlayerPrefs");
            return;
        }

        PlayerPrefs.SetString("PlayerName", multiCharacterData.playerName);
        PlayerPrefs.SetString("LastCharacterSelected", multiCharacterData.currentActiveCharacter);
        PlayerPrefs.SetInt("PlayerLevel", currentCharacter.currentLevel);
        PlayerPrefs.SetInt("PlayerExp", currentCharacter.currentExp);
        PlayerPrefs.SetInt("PlayerExpToNext", currentCharacter.expToNextLevel);
        PlayerPrefs.SetInt("PlayerMaxHp", currentCharacter.totalMaxHp);
        PlayerPrefs.SetInt("PlayerMaxMana", currentCharacter.totalMaxMana);
        PlayerPrefs.SetInt("PlayerAttackDamage", currentCharacter.totalAttackDamage);
        PlayerPrefs.SetInt("PlayerArmor", currentCharacter.totalArmor);
        PlayerPrefs.SetFloat("PlayerCritChance", currentCharacter.totalCriticalChance);
        PlayerPrefs.SetFloat("PlayerMoveSpeed", currentCharacter.totalMoveSpeed);
        PlayerPrefs.SetFloat("PlayerHitRate", currentCharacter.totalHitRate);
        PlayerPrefs.SetFloat("PlayerEvasionRate", currentCharacter.totalEvasionRate);
        PlayerPrefs.SetFloat("PlayerAttackSpeed", currentCharacter.totalAttackSpeed);
        PlayerPrefs.Save();

        Debug.Log($"💾 Saved to PlayerPrefs: {multiCharacterData.currentActiveCharacter} Level {currentCharacter.currentLevel}");
    }

    // ========== Public Methods ==========
    public bool HasValidData()
    {
        return isDataLoaded &&
               multiCharacterData?.IsValid() == true &&
               GetCurrentCharacterData() != null;
    }

    public PlayerProgressData GetPlayerData() => currentPlayerData;
    public void ForceSave() => SavePlayerDataAsync();

    // ========== Character Methods ==========
    public string GetCurrentCharacter()
    {
        return multiCharacterData?.currentActiveCharacter ?? "Assassin";
    }

    public void SwitchCharacter(string characterType)
    {
        if (multiCharacterData == null) return;

        // Save current character progress before switching
        if (!string.IsNullOrEmpty(multiCharacterData.currentActiveCharacter))
        {
            SaveCurrentCharacterProgress();
        }

        // Switch to new character
        multiCharacterData.SwitchActiveCharacter(characterType);

        // ✅ อัพเดท currentPlayerData
        SyncCurrentPlayerData();

        // Update PlayerPrefs
        SaveToPlayerPrefs();

        // Save to Firebase
        SavePlayerDataAsync();

        Debug.Log($"✅ Switched to character: {characterType}");
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

    public string GetCurrentActiveCharacter()
    {
        if (multiCharacterData == null) return "Assassin";
        return multiCharacterData.currentActiveCharacter;
    }

    private void SaveCurrentCharacterProgress()
    {
        if (multiCharacterData == null || currentPlayerData == null) return;

        string currentCharacterType = multiCharacterData.currentActiveCharacter;

        multiCharacterData.UpdateCharacterStats(
            currentCharacterType,
            currentPlayerData.currentLevel,
            currentPlayerData.currentExp,
            currentPlayerData.expToNextLevel,
            currentPlayerData.totalMaxHp,
            currentPlayerData.totalMaxMana,
            currentPlayerData.totalAttackDamage,
            currentPlayerData.totalArmor,
            currentPlayerData.totalCriticalChance,
            currentPlayerData.totalMoveSpeed,
            currentPlayerData.totalHitRate,
            currentPlayerData.totalEvasionRate,
            currentPlayerData.totalAttackSpeed
        );

        Debug.Log($"💾 Saved current character progress for {currentCharacterType}");
    }

    // ========== Context Menu ==========
    [ContextMenu("Quick Load")]
    public void Debug_QuickLoad() => LoadPlayerDataAsync();

    [ContextMenu("Quick Save")]
    public void Debug_QuickSave() => SavePlayerDataAsync();

    [ContextMenu("Force Sync CurrentPlayerData")]
    public void Debug_ForceSyncCurrentPlayerData()
    {
        SyncCurrentPlayerData();
        Debug.Log("🔄 Force synced currentPlayerData");
    }

    [ContextMenu("Log Current Data")]
    public void Debug_LogCurrentData()
    {
        if (multiCharacterData != null)
        {
            Debug.Log("=== Multi-Character Data ===");
            multiCharacterData.LogAllCharacters();
        }

        if (currentPlayerData != null)
        {
            Debug.Log("=== Current Player Data ===");
            currentPlayerData.LogProgressInfo();
        }
        else
        {
            Debug.Log("[PersistentPlayerData] No current data");
        }
    }

    [ContextMenu("Reset Data")]
    public void Debug_ResetData()
    {
        isDataLoaded = false;
        currentPlayerData = null;
        multiCharacterData = null;
        CreateDefaultMultiCharacterData();
    }

    [ContextMenu("Check Data Integrity")]
    public void Debug_CheckDataIntegrity()
    {
        Debug.Log("=== Data Integrity Check ===");
        Debug.Log($"🔍 isDataLoaded: {isDataLoaded}");
        Debug.Log($"🔍 multiCharacterData: {(multiCharacterData != null ? "✅" : "❌")}");
        Debug.Log($"🔍 currentPlayerData: {(currentPlayerData != null ? "✅" : "❌")}");
        Debug.Log($"🔍 HasValidData(): {HasValidData()}");

        if (multiCharacterData != null)
        {
            Debug.Log($"🔍 Active Character: {multiCharacterData.currentActiveCharacter}");
            Debug.Log($"🔍 Character Count: {multiCharacterData.characters.Count}");
        }

        var currentCharacter = GetCurrentCharacterData();
        if (currentCharacter != null)
        {
            Debug.Log($"🔍 Current Character Level: {currentCharacter.currentLevel}");
            Debug.Log($"🔍 Current Character HP: {currentCharacter.totalMaxHp}");
            Debug.Log($"🔍 Current Character Attack: {currentCharacter.totalAttackDamage}");
        }
    }
}