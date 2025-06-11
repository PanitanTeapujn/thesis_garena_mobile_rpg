using UnityEngine;
using Firebase.Auth;
using Firebase.Database;
using System.Collections;

/// <summary>
/// Ultra Lightweight PersistentPlayerData - ไม่มี blocking operations + Better Character Handling
/// </summary>
public class PersistentPlayerData : MonoBehaviour
{
    [Header("Player Data")]
    public PlayerProgressData currentPlayerData;
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

    private IEnumerator LoadDataCoroutine()
    {
        if (auth?.CurrentUser == null)
        {
            CreateDefaultData();
            yield break;
        }

        Debug.Log("[PersistentPlayerData] Loading from Firebase...");

        var task = databaseReference.Child("players").Child(auth.CurrentUser.UserId).GetValueAsync();

        // รอ max 3 วินาที ถ้าเกินใช้ default
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

            PlayerProgressData loadedData = null;
            bool valid = false;

            try
            {
                loadedData = JsonUtility.FromJson<PlayerProgressData>(json);
                valid = (loadedData != null && loadedData.IsValid());
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[PersistentPlayerData] Parse error: {e.Message}. Trying fallback...");
            }

            if (valid)
            {
                currentPlayerData = loadedData;
                isDataLoaded = true;
                Debug.Log($"✅ Loaded PlayerProgressData: {currentPlayerData.playerName}, Character: {currentPlayerData.lastCharacterSelected}, Level {currentPlayerData.currentLevel}");
                SaveToPlayerPrefs();
            }
            else
            {
                Debug.Log("[PersistentPlayerData] Trying to convert from old data format...");
                // ✨ ไม่มี try/catch ตรงนี้ ใช้ yield return ได้
                yield return StartCoroutine(ConvertOldDataFormat(json));
            }

            yield break;
        }
        else
        {
            Debug.Log("[PersistentPlayerData] No data found or timeout. Creating default data...");
        }

        // Fallback to default
        CreateDefaultData();
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

    // ========== NEW: Apply Character Stats ==========
 /*   private void ApplyCharacterStats(string characterName)
    {
        // Parse character type
        PlayerSelectionData.CharacterType characterType = PlayerSelectionData.CharacterType.IronJuggernaut;
        if (System.Enum.TryParse<PlayerSelectionData.CharacterType>(characterName, out var parsedType))
        {
            characterType = parsedType;
        }

        Debug.Log($"[PersistentPlayerData] Applying stats for character: {characterType}");

        // Apply stats ตามตัวละคร
        switch (characterType)
        {
            case PlayerSelectionData.CharacterType.BloodKnight:
                currentPlayerData.totalMaxHp = 120;
                currentPlayerData.totalMaxMana = 60;
                currentPlayerData.totalAttackDamage = 25;
                currentPlayerData.totalArmor = 8;
                currentPlayerData.totalMoveSpeed = 5.2f;
                break;

            case PlayerSelectionData.CharacterType.Archer:
                currentPlayerData.totalMaxHp = 80;
                currentPlayerData.totalMaxMana = 80;
                currentPlayerData.totalAttackDamage = 30;
                currentPlayerData.totalArmor = 3;
                currentPlayerData.totalMoveSpeed = 5.8f;
                break;

            case PlayerSelectionData.CharacterType.Assassin:
                currentPlayerData.totalMaxHp = 70;
                currentPlayerData.totalMaxMana = 40;
                currentPlayerData.totalAttackDamage = 35;
                currentPlayerData.totalArmor = 2;
                currentPlayerData.totalMoveSpeed = 6.5f;
                break;

            case PlayerSelectionData.CharacterType.IronJuggernaut:
            default:
                currentPlayerData.totalMaxHp = 150;
                currentPlayerData.totalMaxMana = 40;
                currentPlayerData.totalAttackDamage = 20;
                currentPlayerData.totalArmor = 12;
                currentPlayerData.totalMoveSpeed = 4.5f;
                break;
        }

        // Common stats
        currentPlayerData.currentLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
        currentPlayerData.currentExp = PlayerPrefs.GetInt("PlayerExp", 0);
        currentPlayerData.expToNextLevel = PlayerPrefs.GetInt("PlayerExpToNext", 100);
        currentPlayerData.totalCriticalChance = 5f;
        currentPlayerData.totalCriticalMultiplier = 2f;
        currentPlayerData.totalAttackRange = 2f;
        currentPlayerData.totalAttackCooldown = 1f;

        Debug.Log($"[PersistentPlayerData] Applied {characterType} stats: HP={currentPlayerData.totalMaxHp}, ATK={currentPlayerData.totalAttackDamage}");
    }*/

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
        currentPlayerData.lastLoginDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        string json = JsonUtility.ToJson(currentPlayerData, true);
        var task = databaseReference.Child("players").Child(auth.CurrentUser.UserId).SetRawJsonValueAsync(json);

        // Fire and forget - ไม่รอให้เสร็จ
        yield return new WaitForSeconds(0.1f);

        SaveToPlayerPrefs();
        Debug.Log($"💾 Saving {currentPlayerData.playerName}, Character: {currentPlayerData.lastCharacterSelected} (async)");
    }

    // ========== Quick Update ==========
    public void UpdateLevelAndStats(int level, int exp, int expToNext, int maxHp, int maxMana, int attackDamage, int armor, float critChance, float moveSpeed)
    {
        if (currentPlayerData == null) return;

        currentPlayerData.currentLevel = level;
        currentPlayerData.currentExp = exp;
        currentPlayerData.expToNextLevel = expToNext;
        currentPlayerData.totalMaxHp = maxHp;
        currentPlayerData.totalMaxMana = maxMana;
        currentPlayerData.totalAttackDamage = attackDamage;
        currentPlayerData.totalArmor = armor;
        currentPlayerData.totalCriticalChance = critChance;
        currentPlayerData.totalMoveSpeed = moveSpeed;

        SaveToPlayerPrefs(); // Instant local save
        SavePlayerDataAsync(); // Async Firebase save
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