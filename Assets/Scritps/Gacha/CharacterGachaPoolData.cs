using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class CharacterGachaEntry
{
    [Header("Character & Drop Rate")]
    public CharacterData characterData;
    [Range(0.1f, 100f)]
    public float dropRate = 1f;

    [Header("Visual Settings")]
    public bool isFeaturedCharacter = false;
    public bool isLimitedTime = false;

    [Header("Duplicate Handling")]
    public bool convertDuplicates = true; // แปลงตัวละครซ้ำเป็น currency หรือ item
    public ItemData duplicateRewardItem; // item ที่ได้เมื่อสุ่มได้ซ้ำ
    public int duplicateRewardAmount = 10; // จำนวน currency/item ที่ได้จากการซ้ำ

    [TextArea(2, 3)]
    public string description = "";

    public bool IsValid()
    {
        return characterData != null && dropRate > 0;
    }
}

[CreateAssetMenu(fileName = "New Character Gacha Pool", menuName = "Gacha System/Character Gacha Pool")]
public class CharacterGachaPoolData : ScriptableObject
{
    [Header("Pool Information")]
    public string poolId = "";
    public string poolName = "";
    public Sprite poolIcon;

    [TextArea(3, 5)]
    public string description = "";

    [Header("Cost Settings")]
    public int costPerRoll = 150;
    public string costCurrency = "Gems";
    public int costPerTenRolls = 1350;

    [Header("Character Pool")]
    [SerializeField] private List<CharacterGachaEntry> characterEntries = new List<CharacterGachaEntry>();

    [Header("Guarantee System")]
    public bool hasGuarantee = true;
    public int guaranteeCount = 90; // pity system - หลังจาก 90 pull แล้วได้ 5 star แน่นอน
    public CharacterRarity guaranteeRarity = CharacterRarity.Legendary;

    [Header("Rate Up System")]
    public bool hasRateUp = false;
    public List<CharacterData> rateUpCharacters = new List<CharacterData>();
    public float rateUpMultiplier = 2f;

    [Header("Banner Settings")]
    public bool isFeaturedBanner = false;
    public Sprite bannerImage;
    public System.DateTime bannerStartDate;
    public System.DateTime bannerEndDate;

    #region Properties
    public List<CharacterGachaEntry> CharacterEntries => characterEntries;
    public float TotalDropRate
    {
        get
        {
            return characterEntries.Where(entry => entry.IsValid()).Sum(entry => GetEffectiveDropRate(entry));
        }
    }

    public bool IsBannerActive
    {
        get
        {
            var now = System.DateTime.Now;
            return now >= bannerStartDate && now <= bannerEndDate;
        }
    }
    #endregion

    #region Validation
    void OnValidate()
    {
        // Auto-generate pool ID
        if (string.IsNullOrEmpty(poolId))
        {
            poolId = $"char_pool_{name.Replace(" ", "_").ToLower()}_{Random.Range(1000, 9999)}";
        }

        // Auto-generate pool name
        if (string.IsNullOrEmpty(poolName))
        {
            poolName = name;
        }

        // Validate entries
        for (int i = characterEntries.Count - 1; i >= 0; i--)
        {
            if (characterEntries[i] == null || !characterEntries[i].IsValid())
            {
                Debug.LogWarning($"Invalid character entry at index {i} in pool {poolName}");
            }
        }
    }
    #endregion

    #region Gacha Logic
    public CharacterGachaEntry GetRandomCharacter()
    {
        if (characterEntries.Count == 0) return null;

        var validEntries = characterEntries.Where(entry => entry.IsValid()).ToList();
        if (validEntries.Count == 0) return null;

        float totalRate = validEntries.Sum(entry => GetEffectiveDropRate(entry));
        float randomValue = Random.Range(0f, totalRate);
        float currentRate = 0f;

        foreach (var entry in validEntries)
        {
            currentRate += GetEffectiveDropRate(entry);
            if (randomValue <= currentRate)
            {
                return entry;
            }
        }

        return validEntries.Last(); // fallback
    }

    public CharacterGachaEntry GetGuaranteedCharacter(CharacterRarity minRarity)
    {
        var validEntries = characterEntries.Where(entry =>
            entry.IsValid() &&
            entry.characterData.rarity >= minRarity
        ).ToList();

        if (validEntries.Count == 0)
        {
            // fallback ให้ตัวละคร rarity สูงสุดที่มี
            validEntries = characterEntries.Where(entry => entry.IsValid())
                .OrderByDescending(entry => (int)entry.characterData.rarity)
                .Take(1)
                .ToList();
        }

        if (validEntries.Count == 0) return null;

        return validEntries[Random.Range(0, validEntries.Count)];
    }

    private float GetEffectiveDropRate(CharacterGachaEntry entry)
    {
        float baseRate = entry.dropRate;

        // ตรวจสอบ rate up
        if (hasRateUp && rateUpCharacters.Contains(entry.characterData))
        {
            baseRate *= rateUpMultiplier;
        }

        return baseRate;
    }

    public List<CharacterGachaEntry> GetCharactersByRarity(CharacterRarity rarity)
    {
        return characterEntries.Where(entry =>
            entry.IsValid() &&
            entry.characterData.rarity == rarity
        ).ToList();
    }

    public List<CharacterGachaEntry> GetFeaturedCharacters()
    {
        return characterEntries.Where(entry =>
            entry.IsValid() &&
            entry.isFeaturedCharacter
        ).ToList();
    }

    public List<CharacterGachaEntry> GetLimitedCharacters()
    {
        return characterEntries.Where(entry =>
            entry.IsValid() &&
            entry.isLimitedTime
        ).ToList();
    }
    #endregion

    #region Banner Management
    public void SetBannerPeriod(System.DateTime start, System.DateTime end)
    {
        bannerStartDate = start;
        bannerEndDate = end;
        Debug.Log($"[CharacterGachaPool] Banner period set: {start:yyyy-MM-dd} to {end:yyyy-MM-dd}");
    }

    public bool IsCharacterRateUp(CharacterData character)
    {
        return hasRateUp && rateUpCharacters.Contains(character);
    }

    public void AddRateUpCharacter(CharacterData character)
    {
        if (!rateUpCharacters.Contains(character))
        {
            rateUpCharacters.Add(character);
        }
    }

    public void RemoveRateUpCharacter(CharacterData character)
    {
        rateUpCharacters.Remove(character);
    }

    public void ClearRateUp()
    {
        rateUpCharacters.Clear();
        hasRateUp = false;
    }
    #endregion

    #region Statistics
    public Dictionary<CharacterRarity, float> GetRarityRates()
    {
        var rates = new Dictionary<CharacterRarity, float>();

        foreach (CharacterRarity rarity in System.Enum.GetValues(typeof(CharacterRarity)))
        {
            var entries = GetCharactersByRarity(rarity);
            float totalRate = entries.Sum(entry => GetEffectiveDropRate(entry));
            rates[rarity] = totalRate;
        }

        return rates;
    }

    public float GetCharacterRate(CharacterData character)
    {
        var entry = characterEntries.FirstOrDefault(e => e.characterData == character);
        return entry != null ? GetEffectiveDropRate(entry) : 0f;
    }
    #endregion

    #region Debug
    [ContextMenu("Debug Pool Info")]
    public void DebugPoolInfo()
    {
        Debug.Log($"Character Gacha Pool: {poolName} ({poolId})");
        Debug.Log($"Cost: {costPerRoll} {costCurrency} (10x: {costPerTenRolls})");
        Debug.Log($"Description: {description}");
        Debug.Log($"Characters: {characterEntries.Count}, Total Rate: {TotalDropRate:F2}%");
        Debug.Log($"Banner Active: {IsBannerActive}");
        Debug.Log($"Rate Up Active: {hasRateUp} (x{rateUpMultiplier})");

        if (hasRateUp)
        {
            Debug.Log($"Rate Up Characters: {string.Join(", ", rateUpCharacters.Select(c => c.characterName))}");
        }

        var rarityRates = GetRarityRates();
        foreach (var kvp in rarityRates)
        {
            var entries = GetCharactersByRarity(kvp.Key);
            Debug.Log($"  {kvp.Key}: {entries.Count} characters, {kvp.Value:F2}% total rate");
        }

        Debug.Log("=== CHARACTER LIST ===");
        foreach (var entry in characterEntries.OrderByDescending(e => (int)e.characterData.rarity))
        {
            string flags = "";
            if (entry.isFeaturedCharacter) flags += "[FEATURED] ";
            if (entry.isLimitedTime) flags += "[LIMITED] ";
            if (IsCharacterRateUp(entry.characterData)) flags += "[RATE UP] ";

            Debug.Log($"  {entry.characterData.characterName} ({entry.characterData.GetRarityText()}) - {GetEffectiveDropRate(entry):F2}% {flags}");
        }
    }

    [ContextMenu("Test Character Roll")]
    public void TestCharacterRoll()
    {
        var result = GetRandomCharacter();
        if (result != null)
        {
            Debug.Log($"Test Roll Result: {result.characterData.characterName} ({result.characterData.GetRarityText()})");
        }
        else
        {
            Debug.Log("Test Roll Failed: No character obtained");
        }
    }
    #endregion
}