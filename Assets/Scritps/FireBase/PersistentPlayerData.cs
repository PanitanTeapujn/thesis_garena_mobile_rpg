using UnityEngine;
using Firebase.Auth;
using Firebase.Database;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// Ultra Lightweight PersistentPlayerData - ไม่มี blocking operations + Better Character Handling
/// </summary>
public class PersistentPlayerData : MonoBehaviour
{
    [Header("Player Data")]
    public PlayerProgressData currentPlayerData;
    public bool isDataLoaded = false;


    [Header("Multi-Character Support")]
    public MultiCharacterPlayerData multiCharacterData;

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
                    // Set current character data
                    CharacterProgressData activeCharacter = multiCharacterData.GetActiveCharacterData();
                    currentPlayerData = activeCharacter.ToPlayerProgressData(multiCharacterData.playerName);

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

                // Set current player data
                currentPlayerData = convertedCharacter.ToPlayerProgressData(multiCharacterData.playerName);

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

    private void CreateDefaultMultiCharacterData()
    {
        multiCharacterData = new MultiCharacterPlayerData();
        multiCharacterData.playerName = PlayerPrefs.GetString("PlayerName", "Player");

        // Set current character data
        CharacterProgressData activeCharacter = multiCharacterData.GetActiveCharacterData();
        currentPlayerData = activeCharacter.ToPlayerProgressData(multiCharacterData.playerName);

        isDataLoaded = true;
        SaveToPlayerPrefs();

        Debug.Log($"✅ Created default multi-character data with Assassin");
    }
    // ========== NEW: Convert Old Data Format ==========
    private IEnumerator ConvertOldDataFormat(string json)
    {
        try
        {
            // ลองแปลงจาก SimplePlayerData format
            var oldData = JsonUtility.FromJson<FirebaseLoginManager.SimplePlayerData>(json);

            if (oldData != null && !string.IsNullOrEmpty(oldData.playerName))
            {
                Debug.Log($"[PersistentPlayerData] Converting old data for {oldData.playerName}, Character: {oldData.lastCharacterSelected}");

                // สร้าง PlayerProgressData ใหม่จากข้อมูลเก่า
                currentPlayerData = new PlayerProgressData();
                currentPlayerData.playerName = oldData.playerName;
                currentPlayerData.lastCharacterSelected = oldData.lastCharacterSelected; // ✅ ตรงนี้สำคัญ!
                currentPlayerData.registrationDate = oldData.registrationDate;
                currentPlayerData.lastLoginDate = oldData.lastLoginDate;

                // Apply character stats ตามตัวละครที่เลือก
/*                ApplyCharacterStats(currentPlayerData.lastCharacterSelected);
*/
                isDataLoaded = true;
                SaveToPlayerPrefs();

                // บันทึกข้อมูลใหม่กลับ Firebase
                SavePlayerDataAsync();

                Debug.Log($"✅ Converted and saved: {currentPlayerData.playerName}, Character: {currentPlayerData.lastCharacterSelected}");
                currentPlayerData.LogProgressInfo();
                yield break;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[PersistentPlayerData] Failed to convert old data: {e.Message}");
        }

        // ถ้าแปลงไม่ได้ ใช้ default
        CreateDefaultData();
    }

  

    // ========== Fast Default Creation ==========
    private void CreateDefaultData()
    {
        currentPlayerData = new PlayerProgressData();
        currentPlayerData.playerName = PlayerPrefs.GetString("PlayerName", "Player");

        // ✅ ใช้ character ที่เลือกล่าสุด
        string selectedCharacter = PlayerSelectionData.GetSelectedCharacter().ToString();
        currentPlayerData.lastCharacterSelected = selectedCharacter;

        Debug.Log($"[PersistentPlayerData] Creating default data for character: {selectedCharacter}");

        // ใช้ values จาก PlayerPrefs ถ้ามี (เร็วกว่า load ScriptableObject)
        currentPlayerData.currentLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
        currentPlayerData.currentExp = PlayerPrefs.GetInt("PlayerExp", 0);
        currentPlayerData.expToNextLevel = PlayerPrefs.GetInt("PlayerExpToNext", 100);
        currentPlayerData.totalMaxHp = PlayerPrefs.GetInt("PlayerMaxHp", 0);
        currentPlayerData.totalMaxMana = PlayerPrefs.GetInt("PlayerMaxMana", 0);
        currentPlayerData.totalAttackDamage = PlayerPrefs.GetInt("PlayerAttackDamage", 0);
        currentPlayerData.totalArmor = PlayerPrefs.GetInt("PlayerArmor", 0);
        currentPlayerData.totalCriticalChance = PlayerPrefs.GetFloat("PlayerCritChance", 5f);
        currentPlayerData.totalMoveSpeed = PlayerPrefs.GetFloat("PlayerMoveSpeed", 5f);

        // ถ้าไม่มีใน PlayerPrefs ใช้ character stats
        if (currentPlayerData.totalMaxHp <= 0)
        {
/*            ApplyCharacterStats(selectedCharacter);
*/        }

        isDataLoaded = true;
        Debug.Log($"✅ Created default data for {currentPlayerData.playerName}, Character: {currentPlayerData.lastCharacterSelected}");
    }

    // ========== NON-BLOCKING Save ==========
    public void SavePlayerDataAsync()
    {
        if (currentPlayerData == null || auth?.CurrentUser == null) return;

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

        // Fire and forget
        yield return new WaitForSeconds(0.1f);

        SaveToPlayerPrefs();
        Debug.Log($"💾 Saving multi-character data for {multiCharacterData.playerName} (async)");
    }


    // ========== Quick Update ==========
    public void UpdateLevelAndStats(int level, int exp, int expToNext, int maxHp, int maxMana, int attackDamage, int armor, float critChance, float moveSpeed)
    {
        if (currentPlayerData == null) return;

        // Update current PlayerProgressData
        currentPlayerData.currentLevel = level;
        currentPlayerData.currentExp = exp;
        currentPlayerData.expToNextLevel = expToNext;
        currentPlayerData.totalMaxHp = maxHp;
        currentPlayerData.totalMaxMana = maxMana;
        currentPlayerData.totalAttackDamage = attackDamage;
        currentPlayerData.totalArmor = armor;
        currentPlayerData.totalCriticalChance = critChance;
        currentPlayerData.totalMoveSpeed = moveSpeed;

        // Update multi-character data
        if (multiCharacterData != null)
        {
            multiCharacterData.UpdateCharacterStats(
                multiCharacterData.currentActiveCharacter,
                level, exp, expToNext, maxHp, maxMana, attackDamage, armor, critChance, moveSpeed
            );
        }

        SaveToPlayerPrefs();
        SavePlayerDataAsync();
    }

    // ========== NEW: Update Character Selection ==========
    public void UpdateCharacterSelection(PlayerSelectionData.CharacterType character)
    {
        if (currentPlayerData == null) return;

        string oldCharacter = currentPlayerData.lastCharacterSelected;
        currentPlayerData.lastCharacterSelected = character.ToString();

        Debug.Log($"[PersistentPlayerData] Character changed: {oldCharacter} → {character}");

        SaveToPlayerPrefs();
        SavePlayerDataAsync();
    }

    // ========== PlayerPrefs Cache ==========
    private void SaveToPlayerPrefs()
    {
        if (currentPlayerData == null) return;

        PlayerPrefs.SetString("PlayerName", currentPlayerData.playerName);
        PlayerPrefs.SetString("LastCharacterSelected", currentPlayerData.lastCharacterSelected); // ✅ บันทึก character ด้วย
        PlayerPrefs.SetInt("PlayerLevel", currentPlayerData.currentLevel);
        PlayerPrefs.SetInt("PlayerExp", currentPlayerData.currentExp);
        PlayerPrefs.SetInt("PlayerExpToNext", currentPlayerData.expToNextLevel);
        PlayerPrefs.SetInt("PlayerMaxHp", currentPlayerData.totalMaxHp);
        PlayerPrefs.SetInt("PlayerMaxMana", currentPlayerData.totalMaxMana);
        PlayerPrefs.SetInt("PlayerAttackDamage", currentPlayerData.totalAttackDamage);
        PlayerPrefs.SetInt("PlayerArmor", currentPlayerData.totalArmor);
        PlayerPrefs.SetFloat("PlayerCritChance", currentPlayerData.totalCriticalChance);
        PlayerPrefs.SetFloat("PlayerMoveSpeed", currentPlayerData.totalMoveSpeed);
        PlayerPrefs.Save();
    }

    // ========== Public Methods ==========
    public bool HasValidData() => isDataLoaded && currentPlayerData?.IsValid() == true;
    public PlayerProgressData GetPlayerData() => currentPlayerData;
    public void ForceSave() => SavePlayerDataAsync();

    // ========== NEW: Character Methods ==========
    public string GetCurrentCharacter()
    {
        return currentPlayerData?.lastCharacterSelected ?? "IronJuggernaut";
    }

    public void SetCurrentCharacter(PlayerSelectionData.CharacterType character)
    {
        UpdateCharacterSelection(character);
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

        // Update currentPlayerData to reflect new character
        CharacterProgressData newCharacterData = multiCharacterData.GetActiveCharacterData();
        currentPlayerData = newCharacterData.ToPlayerProgressData(multiCharacterData.playerName);

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
            currentPlayerData.totalMoveSpeed
        );
    }
    // ========== Context Menu ==========
    [ContextMenu("Quick Load")]
    public void Debug_QuickLoad() => LoadPlayerDataAsync();

    [ContextMenu("Quick Save")]
    public void Debug_QuickSave() => SavePlayerDataAsync();

    [ContextMenu("Log Current Data")]
    public void Debug_LogCurrentData()
    {
        if (currentPlayerData != null)
        {
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
        CreateDefaultData();
    }
}