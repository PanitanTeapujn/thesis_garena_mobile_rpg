using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

[System.Serializable]
public class CharacterEntry
{
    public CharacterData characterData;
    public int obtainedCount;
    public DateTime firstObtainedDate;
    public bool isNew;

    public CharacterEntry(CharacterData data)
    {
        characterData = data;
        obtainedCount = 1;
        firstObtainedDate = DateTime.Now;
        isNew = true;
    }
}

public class CharacterCollection : MonoBehaviour
{
    [Header("Collection Settings")]
    [SerializeField] private List<CharacterEntry> ownedCharacters = new List<CharacterEntry>();
    [SerializeField] private CharacterData currentActiveCharacter;

   
    public static event Action<CharacterData> OnNewCharacterObtained;
    public static event Action<CharacterData, int> OnDuplicateCharacterObtained;
    public static event Action<CharacterData> OnActiveCharacterChanged;

    #region Properties
    public List<CharacterEntry> OwnedCharacters => ownedCharacters;
    public int TotalCharactersCount => ownedCharacters.Count;
    public CharacterData ActiveCharacter => currentActiveCharacter;
    public bool HasAnyCharacter => ownedCharacters.Count > 0;
    #endregion

    #region Character Management
    public bool AddCharacter(CharacterData character)
    {
        if (character == null) return false;

        CharacterEntry existingEntry = ownedCharacters.FirstOrDefault(c => c.characterData == character);

        if (existingEntry != null)
        {
            // ตัวละครซ้ำ - เพิ่ม count
            existingEntry.obtainedCount++;
            existingEntry.isNew = false;
            OnDuplicateCharacterObtained?.Invoke(character, existingEntry.obtainedCount);

            Debug.Log($"[CharacterCollection] Duplicate character obtained: {character.characterName} x{existingEntry.obtainedCount}");
            return true;
        }
        else
        {
            // ตัวละครใหม่
            CharacterEntry newEntry = new CharacterEntry(character);
            ownedCharacters.Add(newEntry);
            OnNewCharacterObtained?.Invoke(character);

            // ถ้ายังไม่มี active character ให้ตั้งเป็น active
            if (currentActiveCharacter == null)
            {
                SetActiveCharacter(character);
            }

            Debug.Log($"[CharacterCollection] New character obtained: {character.characterName}");
            SaveCollectionData();
            return true;
        }
    }

    public bool HasCharacter(CharacterData character)
    {
        return ownedCharacters.Any(c => c.characterData == character);
    }

    public CharacterEntry GetCharacterEntry(CharacterData character)
    {
        return ownedCharacters.FirstOrDefault(c => c.characterData == character);
    }

    public int GetCharacterCount(CharacterData character)
    {
        CharacterEntry entry = GetCharacterEntry(character);
        return entry?.obtainedCount ?? 0;
    }

    public void SetActiveCharacter(CharacterData character)
    {
        if (character == null || !HasCharacter(character)) return;

        CharacterData previousActive = currentActiveCharacter;
        currentActiveCharacter = character;

        OnActiveCharacterChanged?.Invoke(character);

        Debug.Log($"[CharacterCollection] Active character changed: {previousActive?.characterName ?? "None"} → {character.characterName}");
        SaveCollectionData();
    }

    public void MarkCharacterAsViewed(CharacterData character)
    {
        CharacterEntry entry = GetCharacterEntry(character);
        if (entry != null)
        {
            entry.isNew = false;
            SaveCollectionData();
        }
    }

    public void MarkAllAsViewed()
    {
        foreach (var entry in ownedCharacters)
        {
            entry.isNew = false;
        }
        SaveCollectionData();
    }
    #endregion

    #region Collection Queries
    public List<CharacterEntry> GetCharactersByRarity(CharacterRarity rarity)
    {
        return ownedCharacters.Where(c => c.characterData.rarity == rarity).ToList();
    }

    public List<CharacterEntry> GetNewCharacters()
    {
        return ownedCharacters.Where(c => c.isNew).ToList();
    }

    public List<CharacterEntry> GetSortedByRarity()
    {
        return ownedCharacters.OrderByDescending(c => (int)c.characterData.rarity)
                            .ThenBy(c => c.characterData.characterName)
                            .ToList();
    }

    public List<CharacterEntry> GetSortedByName()
    {
        return ownedCharacters.OrderBy(c => c.characterData.characterName).ToList();
    }

    public List<CharacterEntry> GetSortedByObtainedDate()
    {
        return ownedCharacters.OrderByDescending(c => c.firstObtainedDate).ToList();
    }

    public Dictionary<CharacterRarity, int> GetRarityDistribution()
    {
        var distribution = new Dictionary<CharacterRarity, int>();

        foreach (CharacterRarity rarity in System.Enum.GetValues(typeof(CharacterRarity)))
        {
            distribution[rarity] = ownedCharacters.Count(c => c.characterData.rarity == rarity);
        }

        return distribution;
    }

    public int GetNewCharactersCount()
    {
        return ownedCharacters.Count(c => c.isNew);
    }
    #endregion

    #region Data Persistence
    private void SaveCollectionData()
    {
        try
        {
            // Save active character
            if (currentActiveCharacter != null)
            {
                PlayerPrefs.SetString("ActiveCharacter", currentActiveCharacter.characterId);
            }

            // Save owned characters (simplified - in real implementation, use JSON)
            List<string> characterIds = new List<string>();
            List<int> characterCounts = new List<int>();

            foreach (var entry in ownedCharacters)
            {
                characterIds.Add(entry.characterData.characterId);
                characterCounts.Add(entry.obtainedCount);
            }

            string idsString = string.Join(",", characterIds);
            string countsString = string.Join(",", characterCounts.Select(c => c.ToString()));

            PlayerPrefs.SetString("OwnedCharacterIds", idsString);
            PlayerPrefs.SetString("OwnedCharacterCounts", countsString);

            Debug.Log("[CharacterCollection] Collection data saved");
        }
        catch (Exception e)
        {
            Debug.LogError($"[CharacterCollection] Failed to save collection data: {e.Message}");
        }
    }

    private void LoadCollectionData()
    {
        try
        {
            // Load active character
            string activeCharId = PlayerPrefs.GetString("ActiveCharacter", "");
            if (!string.IsNullOrEmpty(activeCharId))
            {
                // TODO: Find character by ID from available characters
                Debug.Log($"[CharacterCollection] Loading active character: {activeCharId}");
            }

            // Load owned characters
            string idsString = PlayerPrefs.GetString("OwnedCharacterIds", "");
            string countsString = PlayerPrefs.GetString("OwnedCharacterCounts", "");

            if (!string.IsNullOrEmpty(idsString))
            {
                string[] ids = idsString.Split(',');
                string[] counts = countsString.Split(',');

                // TODO: Reconstruct collection from saved data
                Debug.Log($"[CharacterCollection] Loading {ids.Length} owned characters");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[CharacterCollection] Failed to load collection data: {e.Message}");
        }
    }
    #endregion

    #region Unity Events
    void Start()
    {
        LoadCollectionData();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveCollectionData();
        }
    }
    #endregion

    #region Debug
    [ContextMenu("Debug Collection Info")]
    public void DebugCollectionInfo()
    {
        Debug.Log($"=== CHARACTER COLLECTION INFO ===");
        Debug.Log($"Total Characters: {TotalCharactersCount}");
        Debug.Log($"Active Character: {currentActiveCharacter?.characterName ?? "None"}");
        Debug.Log($"New Characters: {GetNewCharactersCount()}");

        var distribution = GetRarityDistribution();
        foreach (var kvp in distribution)
        {
            Debug.Log($"  {kvp.Key}: {kvp.Value} characters");
        }

        Debug.Log("=== OWNED CHARACTERS ===");
        foreach (var entry in GetSortedByRarity())
        {
            Debug.Log($"  {entry.characterData.characterName} ({entry.characterData.GetRarityText()}) x{entry.obtainedCount} {(entry.isNew ? "[NEW]" : "")}");
        }
    }

    [ContextMenu("Add Test Character")]
    public void AddTestCharacter()
    {
        // สำหรับ testing - ต้องมี CharacterData ใน project
        CharacterData[] allCharacters = Resources.LoadAll<CharacterData>("");
        if (allCharacters.Length > 0)
        {
            CharacterData randomChar = allCharacters[UnityEngine.Random.Range(0, allCharacters.Length)];
            AddCharacter(randomChar);
            Debug.Log($"Added test character: {randomChar.characterName}");
        }
        else
        {
            Debug.LogWarning("No CharacterData found in Resources folder");
        }
    }

    [ContextMenu("Clear All Characters")]
    public void ClearAllCharacters()
    {
        ownedCharacters.Clear();
        currentActiveCharacter = null;
        SaveCollectionData();
        Debug.Log("Cleared all characters from collection");
    }
    #endregion
}