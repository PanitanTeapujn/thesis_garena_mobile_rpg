using UnityEngine;
using System;
using System.Collections.Generic;

#region Enums
[System.Serializable]
public enum ItemType
{
    Weapon = 0,     // อาวุธ
    Head = 1,       // หมวก/หน้ากาก
    Armor = 2,      // เสื้อเกราะ
    Pants = 3,      // กางเกง
    Shoes = 4,      // รองเท้า
    Rune = 5        // รูน (3 ช่อง)
}

[System.Serializable]
public enum ItemTier
{
    Common = 1,     // ธรรมดา - สีขาว
    Uncommon = 2,   // ไม่ธรรมดา - สีเขียว
    Rare = 3,       // หายาก - สีฟ้า
    Epic = 4,       // มหากาพย์ - สีม่วง
    Legendary = 5   // ตำนาน - สีทอง
}
#endregion

#region Item Stats Structure
[System.Serializable]
public class ItemStats
{
    #region Combat Stats
    [Header("Combat Stats")]
    public int attackDamageBonus = 0;
    public int magicDamageBonus = 0;
    public int armorBonus = 0;
    public float criticalChanceBonus = 0f;      // % (0.1f = 10%)
    public float criticalDamageBonus = 0f;      // multiplier (0.5f = +50%)
    #endregion

    #region Survival Stats
    [Header("Survival Stats")]
    public int maxHpBonus = 0;
    public int maxManaBonus = 0;
    public float moveSpeedBonus = 0f;
    public float attackSpeedBonus = 0f;         // % (0.2f = +20%)
    public float hitRateBonus = 0f;             // % (0.05f = +5%)
    public float evasionRateBonus = 0f;         // % (0.03f = +3%)
    #endregion

    #region Special Stats
    [Header("Special Stats")]
    public float reductionCoolDownBonus = 0f;   // % (0.1f = -10% cooldown)
    public float physicalResistanceBonus = 0f;  // % (0.15f = +15% resistance)
    public float magicalResistanceBonus = 0f;   // % (0.15f = +15% resistance)
    #endregion

    #region Utility Methods
    public EquipmentStats ToEquipmentStats()
    {
        EquipmentStats equipStats = new EquipmentStats();
        equipStats.attackDamageBonus = attackDamageBonus;
        equipStats.magicDamageBonus = magicDamageBonus;
        equipStats.armorBonus = armorBonus;
        equipStats.criticalChanceBonus = criticalChanceBonus;
        equipStats.criticalMultiplierBonus = criticalDamageBonus;
        equipStats.maxHpBonus = maxHpBonus;
        equipStats.maxManaBonus = maxManaBonus;
        equipStats.moveSpeedBonus = moveSpeedBonus;
        equipStats.attackSpeedBonus = attackSpeedBonus;
        equipStats.hitRateBonus = hitRateBonus;
        equipStats.evasionRateBonus = evasionRateBonus;
        equipStats.reductionCoolDownBonus = reductionCoolDownBonus;
        equipStats.physicalResistanceBonus = physicalResistanceBonus;
        equipStats.magicalResistanceBonus = magicalResistanceBonus;
        return equipStats;
    }

    public bool HasAnyStats()
    {
        return attackDamageBonus != 0 || magicDamageBonus != 0 || armorBonus != 0 ||
               criticalChanceBonus != 0f || criticalDamageBonus != 0f ||
               maxHpBonus != 0 || maxManaBonus != 0 || moveSpeedBonus != 0f ||
               attackSpeedBonus != 0f || hitRateBonus != 0f || evasionRateBonus != 0f ||
               reductionCoolDownBonus != 0f || physicalResistanceBonus != 0f || magicalResistanceBonus != 0f;
    }

    public string GetStatsDescription()
    {
        List<string> statsList = new List<string>();

        if (attackDamageBonus != 0)
            statsList.Add($"Attack: +{attackDamageBonus}");
        if (magicDamageBonus != 0)
            statsList.Add($"Magic: +{magicDamageBonus}");
        if (armorBonus != 0)
            statsList.Add($"Armor: +{armorBonus}");
        if (criticalChanceBonus != 0f)
            statsList.Add($"Crit Chance: +{criticalChanceBonus:P1}");
        if (criticalDamageBonus != 0f)
            statsList.Add($"Crit Damage: +{criticalDamageBonus:P1}");
        if (maxHpBonus != 0)
            statsList.Add($"HP: +{maxHpBonus}");
        if (maxManaBonus != 0)
            statsList.Add($"Mana: +{maxManaBonus}");
        if (moveSpeedBonus != 0f)
            statsList.Add($"Move Speed: +{moveSpeedBonus:F1}");
        if (attackSpeedBonus != 0f)
            statsList.Add($"Attack Speed: +{attackSpeedBonus:P1}");
        if (hitRateBonus != 0f)
            statsList.Add($"Hit Rate: +{hitRateBonus:P1}");
        if (evasionRateBonus != 0f)
            statsList.Add($"Evasion: +{evasionRateBonus:P1}");
        if (reductionCoolDownBonus != 0f)
            statsList.Add($"Cooldown: -{reductionCoolDownBonus:P1}");
        if (physicalResistanceBonus != 0f)
            statsList.Add($"Physical Res: +{physicalResistanceBonus:P1}");
        if (magicalResistanceBonus != 0f)
            statsList.Add($"Magical Res: +{magicalResistanceBonus:P1}");

        return statsList.Count > 0 ? string.Join("\n", statsList) : "No bonus stats";
    }
    #endregion
}
#endregion

#region ScriptableObject ItemData
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory System/Item Data")]
public class ItemData : ScriptableObject
{
    #region Basic Info
    [Header("Basic Information")]
    [SerializeField] private string itemId;
    [SerializeField] private string itemName;
    [SerializeField] private Sprite itemIcon;
    [SerializeField] private ItemType itemType;
    [SerializeField] private ItemTier tier;
    [TextArea(3, 5)]
    [SerializeField] private string description;
    #endregion

    #region Stats
    [Header("Item Stats")]
    [SerializeField] private ItemStats stats = new ItemStats();
    #endregion

    #region Properties
    public string ItemId => itemId;
    public string ItemName => itemName;
    public Sprite ItemIcon => itemIcon;
    public ItemType ItemType => itemType;
    public ItemTier Tier => tier;
    public string Description => description;
    public ItemStats Stats => stats;
    #endregion

    #region Validation
    void OnValidate()
    {
        // Auto-generate ID ถ้าว่าง
        if (string.IsNullOrEmpty(itemId))
        {
            itemId = GenerateItemId();
        }

        // Auto-generate name ถ้าว่าง
        if (string.IsNullOrEmpty(itemName))
        {
            itemName = name; // ใช้ชื่อไฟล์
        }
    }

    private string GenerateItemId()
    {
        // สร้าง ID แบบง่ายๆ: Type_Name_RandomNumber
        string typePrefix = itemType.ToString().ToLower();
        string cleanName = itemName.Replace(" ", "_").ToLower();
        string randomSuffix = UnityEngine.Random.Range(1000, 9999).ToString();

        return $"{typePrefix}_{cleanName}_{randomSuffix}";
    }
    #endregion

    #region Utility Methods
    public Color GetTierColor()
    {
        switch (tier)
        {
            case ItemTier.Common: return Color.white;
            case ItemTier.Uncommon: return Color.green;
            case ItemTier.Rare: return Color.cyan;
            case ItemTier.Epic: return Color.magenta;
            case ItemTier.Legendary: return Color.yellow;
            default: return Color.white;
        }
    }

    public string GetTierText()
    {
        switch (tier)
        {
            case ItemTier.Common: return "Common";
            case ItemTier.Uncommon: return "Uncommon";
            case ItemTier.Rare: return "Rare";
            case ItemTier.Epic: return "Epic";
            case ItemTier.Legendary: return "Legendary";
            default: return "Unknown";
        }
    }

    public bool CanEquipToSlot(ItemType slotType)
    {
        return itemType == slotType;
    }

    public FirebaseItemData ToFirebaseData()
    {
        FirebaseItemData firebaseData = new FirebaseItemData();
        firebaseData.itemId = itemId;
        firebaseData.itemName = itemName;
        firebaseData.itemType = (int)itemType;
        firebaseData.tier = (int)tier;
        firebaseData.description = description;

        // Copy stats
        firebaseData.attackDamageBonus = stats.attackDamageBonus;
        firebaseData.magicDamageBonus = stats.magicDamageBonus;
        firebaseData.armorBonus = stats.armorBonus;
        firebaseData.criticalChanceBonus = stats.criticalChanceBonus;
        firebaseData.criticalDamageBonus = stats.criticalDamageBonus;
        firebaseData.maxHpBonus = stats.maxHpBonus;
        firebaseData.maxManaBonus = stats.maxManaBonus;
        firebaseData.moveSpeedBonus = stats.moveSpeedBonus;
        firebaseData.attackSpeedBonus = stats.attackSpeedBonus;
        firebaseData.hitRateBonus = stats.hitRateBonus;
        firebaseData.evasionRateBonus = stats.evasionRateBonus;
        firebaseData.reductionCoolDownBonus = stats.reductionCoolDownBonus;
        firebaseData.physicalResistanceBonus = stats.physicalResistanceBonus;
        firebaseData.magicalResistanceBonus = stats.magicalResistanceBonus;

        return firebaseData;
    }

    public static ItemData FromFirebaseData(FirebaseItemData firebaseData, Sprite icon = null)
    {
        ItemData item = CreateInstance<ItemData>();
        item.itemId = firebaseData.itemId;
        item.itemName = firebaseData.itemName;
        item.itemIcon = icon; // จะต้อง load จาก Resources หรือ Addressables
        item.itemType = (ItemType)firebaseData.itemType;
        item.tier = (ItemTier)firebaseData.tier;
        item.description = firebaseData.description;

        // Copy stats
        item.stats.attackDamageBonus = firebaseData.attackDamageBonus;
        item.stats.magicDamageBonus = firebaseData.magicDamageBonus;
        item.stats.armorBonus = firebaseData.armorBonus;
        item.stats.criticalChanceBonus = firebaseData.criticalChanceBonus;
        item.stats.criticalDamageBonus = firebaseData.criticalDamageBonus;
        item.stats.maxHpBonus = firebaseData.maxHpBonus;
        item.stats.maxManaBonus = firebaseData.maxManaBonus;
        item.stats.moveSpeedBonus = firebaseData.moveSpeedBonus;
        item.stats.attackSpeedBonus = firebaseData.attackSpeedBonus;
        item.stats.hitRateBonus = firebaseData.hitRateBonus;
        item.stats.evasionRateBonus = firebaseData.evasionRateBonus;
        item.stats.reductionCoolDownBonus = firebaseData.reductionCoolDownBonus;
        item.stats.physicalResistanceBonus = firebaseData.physicalResistanceBonus;
        item.stats.magicalResistanceBonus = firebaseData.magicalResistanceBonus;

        return item;
    }
    #endregion

    #region Debug
    [ContextMenu("Debug Item Info")]
    public void DebugItemInfo()
    {
        Debug.Log($"📦 Item: {itemName} ({itemId})");
        Debug.Log($"🏷️ Type: {itemType}, Tier: {GetTierText()}");
        Debug.Log($"📜 Description: {description}");
        Debug.Log($"⚡ Stats:\n{stats.GetStatsDescription()}");
    }
    #endregion
}
#endregion

#region Firebase Data Structure
[System.Serializable]
public class FirebaseItemData
{
    #region Basic Info
    public string itemId;
    public string itemName;
    public int itemType;        // ItemType as int
    public int tier;           // ItemTier as int
    public string description;
    #endregion

    #region Stats (Flattened for Firebase)
    public int attackDamageBonus = 0;
    public int magicDamageBonus = 0;
    public int armorBonus = 0;
    public float criticalChanceBonus = 0f;
    public float criticalDamageBonus = 0f;
    public int maxHpBonus = 0;
    public int maxManaBonus = 0;
    public float moveSpeedBonus = 0f;
    public float attackSpeedBonus = 0f;
    public float hitRateBonus = 0f;
    public float evasionRateBonus = 0f;
    public float reductionCoolDownBonus = 0f;
    public float physicalResistanceBonus = 0f;
    public float magicalResistanceBonus = 0f;
    #endregion

    #region Utility
    public ItemType GetItemType()
    {
        return (ItemType)itemType;
    }

    public ItemTier GetTier()
    {
        return (ItemTier)tier;
    }

    public ItemStats ToItemStats()
    {
        ItemStats stats = new ItemStats();
        stats.attackDamageBonus = attackDamageBonus;
        stats.magicDamageBonus = magicDamageBonus;
        stats.armorBonus = armorBonus;
        stats.criticalChanceBonus = criticalChanceBonus;
        stats.criticalDamageBonus = criticalDamageBonus;
        stats.maxHpBonus = maxHpBonus;
        stats.maxManaBonus = maxManaBonus;
        stats.moveSpeedBonus = moveSpeedBonus;
        stats.attackSpeedBonus = attackSpeedBonus;
        stats.hitRateBonus = hitRateBonus;
        stats.evasionRateBonus = evasionRateBonus;
        stats.reductionCoolDownBonus = reductionCoolDownBonus;
        stats.physicalResistanceBonus = physicalResistanceBonus;
        stats.magicalResistanceBonus = magicalResistanceBonus;
        return stats;
    }
    #endregion
    #endregion
}

