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
        PlayerPrefs.SetInt("PlayerArmor", currentCharacter.totalArmor);
        PlayerPrefs.SetFloat("PlayerCritChance", currentCharacter.totalCriticalChance);
        PlayerPrefs.SetFloat("PlayerMoveSpeed", currentCharacter.totalMoveSpeed);
        PlayerPrefs.SetFloat("PlayerHitRate", currentCharacter.totalHitRate);
        PlayerPrefs.SetFloat("PlayerEvasionRate", currentCharacter.totalEvasionRate);
        PlayerPrefs.SetFloat("PlayerAttackSpeed", currentCharacter.totalAttackSpeed);
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
        int attackDamage, int armor, float critChance, float moveSpeed,
        float hitRate, float evasionRate, float attackSpeed)
    {
        if (multiCharacterData == null) return;

        multiCharacterData.UpdateCharacterStats(
            multiCharacterData.currentActiveCharacter,
            level, exp, expToNext, maxHp, maxMana, attackDamage, armor,
            critChance, moveSpeed, hitRate, evasionRate, attackSpeed);

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

    public void ForceSave() => SavePlayerDataAsync();

    // ========== Debug Methods ==========
   

   
}