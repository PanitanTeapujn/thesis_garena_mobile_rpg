using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using static ShopLooby;

#region Enums
[System.Serializable]
public enum ItemType
{
    Weapon = 0,     // อาวุธ
    Head = 1,       // หมวก/หน้ากาก
    Armor = 2,      // เสื้อเกราะ
    Pants = 3,      // กางเกง
    Shoes = 4,      // รองเท้า
    Rune = 5,
  Potion = 6  ,
    Material = 7,// รูน (3 ช่อง)
    Misc = 8        // 🆕 ของเบ็ดเตล็ด

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
    public float magicalResistanceBonus = 0f;
    public float lifeStealBonus = 0f;           // % (1.5f = +1.5% lifesteal)
                                                // % (0.15f = +15% resistance)
    private bool isStackable;
    #endregion
    #region Potion Stats
    [Header("Potion Effects")]
    public int healAmount = 0;          // จำนวน HP ที่ฟื้นฟู (เช่น 50 HP)
    public int manaAmount = 0;          // จำนวน Mana ที่ฟื้นฟู (เช่น 30 MP)
    public float healPercentage = 0f;   // เปอร์เซ็นต์ HP ที่ฟื้นฟู (0.1f = 10%)
    public float manaPercentage = 0f;   // เปอร์เซ็นต์ Mana ที่ฟื้นฟู (0.1f = 10%)
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
        equipStats.lifeStealBonus = lifeStealBonus;

        return equipStats;
    }

    public bool HasAnyStats()
    {
        return attackDamageBonus != 0 || magicDamageBonus != 0 || armorBonus != 0 ||
               criticalChanceBonus != 0f || criticalDamageBonus != 0f ||
               maxHpBonus != 0 || maxManaBonus != 0 || moveSpeedBonus != 0f ||
               attackSpeedBonus != 0f || hitRateBonus != 0f || evasionRateBonus != 0f ||
               reductionCoolDownBonus != 0f || physicalResistanceBonus != 0f || magicalResistanceBonus != 0f || lifeStealBonus != 0f ;
    }

    // แทนที่ GetStatsDescription() method เดิมด้วยโค้ดนี้
    // แทนที่ GetStatsDescription() method เดิมด้วยโค้ดนี้
    public string GetStatsDescription()
    {
        List<string> statsList = new List<string>();

        // Equipment Stats
        if (attackDamageBonus != 0)
            statsList.Add($"Attack: +{attackDamageBonus}");
        if (magicDamageBonus != 0)
            statsList.Add($"Magic: +{magicDamageBonus}");
        if (armorBonus != 0)
            statsList.Add($"Armor: +{armorBonus}");

        // 🔧 แสดงเป็น % โดยไม่ให้ :P1 คูณ 100 อีก
        if (criticalChanceBonus != 0f)
            statsList.Add($"Crit Chance: +{criticalChanceBonus:F1}%");
        if (criticalDamageBonus != 0f)
            statsList.Add($"Crit Damage: +{criticalDamageBonus:F1}%");

        if (maxHpBonus != 0)
            statsList.Add($"HP: +{maxHpBonus}");
        if (maxManaBonus != 0)
            statsList.Add($"Mana: +{maxManaBonus}");
        if (moveSpeedBonus != 0f)
            statsList.Add($"Move Speed: +{moveSpeedBonus:F1}");

        // 🔧 แสดงเป็น % โดยไม่ให้ :P1 คูณ 100 อีก
        if (attackSpeedBonus != 0f)
            statsList.Add($"Attack Speed: +{attackSpeedBonus:F1}%");
        if (hitRateBonus != 0f)
            statsList.Add($"Hit Rate: +{hitRateBonus:F1}%");
        if (evasionRateBonus != 0f)
            statsList.Add($"Evasion: +{evasionRateBonus:F1}%");
        if (reductionCoolDownBonus != 0f)
            statsList.Add($"Cooldown: -{reductionCoolDownBonus:F1}%");
        if (physicalResistanceBonus != 0f)
            statsList.Add($"Physical Res: +{physicalResistanceBonus:F1}%");
        if (magicalResistanceBonus != 0f)
            statsList.Add($"Magical Res: +{magicalResistanceBonus:F1}%");
        if (lifeStealBonus != 0f)
            statsList.Add($"Lifesteal: +{lifeStealBonus:F1}%");

        // Potion Effects
        if (healAmount > 0)
            statsList.Add($"🔴 Heal: +{healAmount} HP");
        if (manaAmount > 0)
            statsList.Add($"🔵 Mana: +{manaAmount} MP");

        // 🔧 แสดงเป็น % โดยไม่ให้ :P1 คูณ 100 อีก
        if (healPercentage > 0f)
            statsList.Add($"🔴 Heal: +{healPercentage:F1}% Max HP");
        if (manaPercentage > 0f)
            statsList.Add($"🔵 Mana: +{manaPercentage:F1}% Max MP");

        return statsList.Count > 0 ? string.Join("\n", statsList) : "No bonus stats";
    }

    public bool IsPotion()
    {
        return healAmount > 0 || manaAmount > 0 || healPercentage > 0f || manaPercentage > 0f;
    }

    public bool IsHealthPotion()
    {
        return healAmount > 0 || healPercentage > 0f;
    }

    public bool IsManaPotion()
    {
        return manaAmount > 0 || manaPercentage > 0f;
    }

    public bool IsMixedPotion()
    {
        return IsHealthPotion() && IsManaPotion();
    }
    #endregion
}
#endregion

#region ScriptableObject ItemData
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory System/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("🔄 Trade Recipe")]
    [SerializeField] private TradeRecipeData tradeRecipe = new TradeRecipeData();
    #region Basic Info
    [Header("Basic Information")]
    [SerializeField] public string itemId;
    [SerializeField] public string itemName;
    [SerializeField] public Sprite itemIcon;
    [SerializeField] public ItemType itemType;
    [SerializeField] public ItemTier tier;
    [TextArea(3, 5)]
    [SerializeField] public string description;
    #endregion

    [Header("💰 Economy")]
    [SerializeField] public long sellPrice = 0;      // ราคาขาย (เงินที่ได้เมื่อขายให้ NPC)
    [SerializeField] public long buyPrice = 0;       // ราคาซื้อ (เงินที่ต้องจ่ายเมื่อซื้อจาก shop)
    [SerializeField] public bool isSellable = true;  // สามารถขายได้หรือไม่
    [SerializeField] public bool isTradeable = true; // สามารถแลกเปลี่ยนได้หรือไม่
    [SerializeField] public CurrencyType buyCurrencyType = CurrencyType.Gold; // ✅ เพิ่มบรรทัดนี้
    #region Trade Properties
    public TradeRecipeData TradeRecipe => tradeRecipe;
    public bool IsTradeableItem => tradeRecipe.isTradeableItem;
    public bool HasValidTradeRecipe => tradeRecipe.IsValid();
    #endregion

    [Header("Stack Settings")]
    [SerializeField] public int maxStackSize = 1;
    [SerializeField] public bool isStackable = false;

    [Header("🔥 Set Bonus")]
    [SerializeField] private EquipmentSet belongsToSet = null;
    [SerializeField] private bool isSetItem = false;

    // Properties
    public EquipmentSet BelongsToSet => belongsToSet;
    public bool IsSetItem => isSetItem;

    #region Stats
    [Header("Item Stats")]
    [SerializeField] private ItemStats stats = new ItemStats();
    #endregion

    #region Properties
    public int MaxStackSize => maxStackSize;
    public bool IsStackable => isStackable;
    public string ItemId => itemId;
    public string ItemName => itemName;
    public Sprite ItemIcon => itemIcon;
    public ItemType ItemType => itemType;
    public ItemTier Tier => tier;
    public string Description => description;
    public ItemStats Stats => stats;
    public long SellPrice => sellPrice;
    public long BuyPrice => buyPrice;
    public bool IsSellable => isSellable;
    public bool IsTradeable => isTradeable;
    public CurrencyType BuyCurrencyType => buyCurrencyType; // ✅ เพิ่มบรรทัดนี้

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

        // ตั้งค่า stack ตาม item type
        SetDefaultStackSettings();

        // 🆕 ตรวจสอบ set item
        ValidateSetItem();
    }
    private void ValidateSetItem()
    {
        if (belongsToSet != null)
        {
            isSetItem = true;

            // ตรวจสอบว่า item นี้อยู่ใน set items list หรือไม่
            if (!belongsToSet.setItems.Contains(this))
            {
                Debug.LogWarning($"[ItemData] {itemName} belongs to set {belongsToSet.setName} but is not in the set's item list!");
            }
        }
        else
        {
            isSetItem = false;
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
    public string GetBuyPriceDisplayText()
    {
        if (buyPrice <= 0) return "FREE";

        string currencyIcon = buyCurrencyType == CurrencyType.Gold ? "💰" : "💎";
        return $"{currencyIcon} {buyPrice:N0}";
    }
    public string GetSellPriceDisplayText()
    {
        if (sellPrice <= 0) return "Cannot Sell";

        // ขายได้เฉพาะ Gold เท่านั้น
        return $"💰 {sellPrice:N0}";
    }
    public bool CanAfford(long playerGold, int playerGems)
    {
        if (buyPrice <= 0) return true; // ฟรี

        if (buyCurrencyType == CurrencyType.Gold)
        {
            return playerGold >= buyPrice;
        }
        else // Gems
        {
            return playerGems >= buyPrice;
        }
    }


    public bool IsPartOfSet()
    {
        return isSetItem && belongsToSet != null;
    }

    /// <summary>
    /// ดึงข้อมูล set ที่เป็นสมาชิก
    /// </summary>
    public EquipmentSet GetEquipmentSet()
    {
        return belongsToSet;
    }

    /// <summary>
    /// ดึงชื่อ set
    /// </summary>
    public string GetSetName()
    {
        if (IsPartOfSet())
            return belongsToSet.setName;

        return "No Set";
    }

    /// <summary>
    /// ดึงสี set
    /// </summary>
    public Color GetSetColor()
    {
        if (IsPartOfSet())
            return belongsToSet.setColor;

        return Color.white;
    }

    /// <summary>
    /// ดึงคำอธิบาย set bonus
    /// </summary>
    public string GetSetBonusDescription(List<ItemData> equippedItems = null)
    {
        if (!IsPartOfSet())
            return "";

        int equippedPieces = 0;
        if (equippedItems != null)
        {
            equippedPieces = belongsToSet.GetEquippedPiecesCount(equippedItems);
        }

        return belongsToSet.GetSetBonusInfo(equippedPieces);
    }

    /// <summary>
    /// ตรวจสอบว่าเป็น set เดียวกันหรือไม่
    /// </summary>
    public bool IsSameSet(ItemData otherItem)
    {
        if (!IsPartOfSet() || !otherItem.IsPartOfSet())
            return false;

        return belongsToSet == otherItem.belongsToSet;
    }
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
    public bool CanStack()
    {
        return isStackable && maxStackSize > 1;
    }

    public bool CanStackWith(ItemData other)
    {
        if (other == null || !CanStack() || !other.CanStack())
            return false;

        return ItemId == other.ItemId;
    }
    private void SetDefaultStackSettings()
    {
        switch (itemType)
        {
            case ItemType.Potion:
                isStackable = true;
                maxStackSize = Mathf.Max(maxStackSize, 99); // Potion stack ได้ 99
                break;
            case ItemType.Rune:
                isStackable = true;
                maxStackSize = Mathf.Max(maxStackSize, 10); // Rune stack ได้ 10
                break;
            case ItemType.Material:  // 🆕 วัสดุ stack ได้เยอะ
                isStackable = true;
                maxStackSize = Mathf.Max(maxStackSize, 999);
                break;
            case ItemType.Misc:      // 🆕 ของเบ็ดเตล็ด stack ได้ปานกลาง
                isStackable = true;
                maxStackSize = Mathf.Max(maxStackSize, 50);
                break;
            case ItemType.Weapon:
            case ItemType.Head:
            case ItemType.Armor:
            case ItemType.Pants:
            case ItemType.Shoes:
                isStackable = false;
                maxStackSize = 1; // อุปกรณ์ไม่ stack
                break;
        }
    }
    public bool IsEquippableItem()
    {
        switch (itemType)
        {
            case ItemType.Weapon:
            case ItemType.Head:
            case ItemType.Armor:
            case ItemType.Pants:
            case ItemType.Shoes:
                return true;
            case ItemType.Rune:
            case ItemType.Potion:
            case ItemType.Material:
            case ItemType.Misc:
                return false;
            default:
                return false;
        }
    }

    // 🆕 เพิ่ม method ตรวจสอบว่าเป็นวัสดุหรือไม่
    public bool IsMaterial()
    {
        return itemType == ItemType.Material || itemType == ItemType.Misc;
    }

    // 🆕 เพิ่ม method คำนวณราคา
    public long GetSellValue(int quantity = 1)
    {
        return sellPrice * quantity;
    }

    public long GetBuyValue(int quantity = 1)
    {
        return buyPrice * quantity;
    }
    public string GetTierText()
    {
        string tierText = tier switch
        {
            ItemTier.Common => "Common",
            ItemTier.Uncommon => "Uncommon",
            ItemTier.Rare => "Rare",
            ItemTier.Epic => "Epic",
            ItemTier.Legendary => "Legendary",
            _ => "Unknown"
        };

        // 🆕 เพิ่ม set info ถ้าเป็น set item
        if (IsPartOfSet())
        {
            tierText += $" ({belongsToSet.setName})";
        }

        return tierText;
    }
    public string GetDetailedTooltip(List<ItemData> equippedItems = null)
    {
        List<string> tooltipLines = new List<string>();

        // Basic info
        tooltipLines.Add($"<color=#{ColorUtility.ToHtmlStringRGB(GetTierColor())}>{ItemName}</color>");
        tooltipLines.Add($"<color=grey>{GetTierText()}</color>");

        if (!string.IsNullOrEmpty(Description))
        {
            tooltipLines.Add($"<color=white>{Description}</color>");
        }

        // Stats
        if (Stats.HasAnyStats())
        {
            tooltipLines.Add("");
            tooltipLines.Add("<color=lightblue>Stats:</color>");
            tooltipLines.Add($"<color=white>{Stats.GetStatsDescription()}</color>");
        }

        // Set bonus info
        if (IsPartOfSet())
        {
            tooltipLines.Add("");
            tooltipLines.Add("<color=orange>Set Bonus:</color>");
            tooltipLines.Add(GetSetBonusDescription(equippedItems));
        }

        // Price info
        if (SellPrice > 0)
        {
            tooltipLines.Add("");
            tooltipLines.Add($"<color=yellow>Sell Price: {GetSellPriceDisplayText()}</color>");
        }

        return string.Join("\n", tooltipLines);
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
        firebaseData.sellPrice = sellPrice;
        firebaseData.buyPrice = buyPrice;
        firebaseData.buyCurrencyType = (int)buyCurrencyType;
        firebaseData.isSellable = isSellable;
        firebaseData.isTradeable = isTradeable;

        // 🆕 Set info
        firebaseData.isSetItem = isSetItem;
        firebaseData.setName = IsPartOfSet() ? belongsToSet.setName : "";

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
        firebaseData.lifeStealBonus = stats.lifeStealBonus;
        firebaseData.healAmount = stats.healAmount;
        firebaseData.manaAmount = stats.manaAmount;
        firebaseData.healPercentage = stats.healPercentage;
        firebaseData.manaPercentage = stats.manaPercentage;

        return firebaseData;
    }

    public static ItemData FromFirebaseData(FirebaseItemData firebaseData, Sprite icon = null)
    {
        ItemData item = CreateInstance<ItemData>();
        item.itemId = firebaseData.itemId;
        item.itemName = firebaseData.itemName;
        item.itemIcon = icon;
        item.itemType = (ItemType)firebaseData.itemType;
        item.tier = (ItemTier)firebaseData.tier;
        item.description = firebaseData.description;
        item.sellPrice = firebaseData.sellPrice;
        item.buyPrice = firebaseData.buyPrice;
        item.buyCurrencyType = (CurrencyType)firebaseData.buyCurrencyType;
        item.isSellable = firebaseData.isSellable;
        item.isTradeable = firebaseData.isTradeable;

        // 🆕 Set info
        item.isSetItem = firebaseData.isSetItem;
        if (item.isSetItem && !string.IsNullOrEmpty(firebaseData.setName))
        {
            // ต้องหา EquipmentSet จาก setName
            item.belongsToSet = FindEquipmentSetByName(firebaseData.setName);
        }

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
        item.stats.lifeStealBonus = firebaseData.lifeStealBonus;
        item.stats.healAmount = firebaseData.healAmount;
        item.stats.manaAmount = firebaseData.manaAmount;
        item.stats.healPercentage = firebaseData.healPercentage;
        item.stats.manaPercentage = firebaseData.manaPercentage;

        return item;
    }
    private static EquipmentSet FindEquipmentSetByName(string setName)
    {
        EquipmentSet[] allSets = Resources.LoadAll<EquipmentSet>("EquipmentSets");

        foreach (var set in allSets)
        {
            if (set.setName == setName)
            {
                return set;
            }
        }

        Debug.LogWarning($"[ItemData] Could not find EquipmentSet with name: {setName}");
        return null;
    }
    #endregion
    #region Trade Methods
    public TradeItemData ToTradeItemData()
    {
        if (!HasValidTradeRecipe)
        {
            Debug.LogWarning($"[ItemData] {ItemName} does not have a valid trade recipe!");
            return null;
        }

        var tradeData = new TradeItemData(this, tradeRecipe.requiredGold);
        tradeData.tradeName = !string.IsNullOrEmpty(tradeRecipe.tradeName) ? tradeRecipe.tradeName : ItemName;
        tradeData.tradeDescription = tradeRecipe.tradeDescription;

        foreach (var requirement in tradeRecipe.requiredItems)
        {
            if (requirement.IsValid())
            {
                tradeData.AddRequirement(requirement.requiredItem, requirement.requiredQuantity);
            }
        }

        return tradeData;
    }

    public bool CanPlayerTrade(Inventory playerInventory, CurrencyManager currencyManager)
    {
        if (!HasValidTradeRecipe) return false;

        // ตรวจสอบเงิน
        if (tradeRecipe.requiredGold > 0 && currencyManager != null)
        {
            if (!currencyManager.HasEnoughGold(tradeRecipe.requiredGold))
                return false;
        }

        // ตรวจสอบ items
        if (playerInventory != null)
        {
            foreach (var requirement in tradeRecipe.requiredItems)
            {
                if (!requirement.IsValid()) continue;

                int playerHas = GetItemCountInInventory(playerInventory, requirement.requiredItem);
                if (playerHas < requirement.requiredQuantity)
                    return false;
            }
        }

        return true;
    }

    private int GetItemCountInInventory(Inventory inventory, ItemData itemData)
    {
        int totalCount = 0;

        for (int i = 0; i < inventory.CurrentSlots; i++)
        {
            InventoryItem item = inventory.GetItem(i);
            if (item != null && !item.IsEmpty && item.itemData.ItemId == itemData.ItemId)
            {
                totalCount += item.stackCount;
            }
        }

        return totalCount;
    }

    public string GetTradeRequirementsStatusText(Inventory playerInventory, CurrencyManager currencyManager)
    {
        if (!HasValidTradeRecipe)
            return "Not tradeable";

        List<string> statusLines = new List<string>();

        // ✅ ตรวจสอบ items requirements
        if (tradeRecipe.requiredItems != null && tradeRecipe.requiredItems.Count > 0)
        {
            foreach (var requirement in tradeRecipe.requiredItems)
            {
                if (!requirement.IsValid()) continue;

                int playerHas = 0;
                if (playerInventory != null)
                {
                    playerHas = GetItemCountInInventory(playerInventory, requirement.requiredItem);
                }

                string status = playerHas >= requirement.requiredQuantity ? "✅" : "❌";
                statusLines.Add($"{status} {requirement.requiredItem.ItemName}: {playerHas}/{requirement.requiredQuantity}");
            }
        }

        // ✅ ตรวจสอบ gold requirements
        if (tradeRecipe.requiredGold > 0)
        {
            long playerGold = 0;
            if (currencyManager != null)
            {
                playerGold = currencyManager.GetCurrentGold();
            }

            string goldStatus = playerGold >= tradeRecipe.requiredGold ? "✅" : "❌";
            statusLines.Add($"{goldStatus} Gold: {playerGold:N0}/{tradeRecipe.requiredGold:N0}");
        }

        // ✅ ถ้าไม่มี requirements ใดๆ
        if (statusLines.Count == 0)
        {
            return "No requirements needed";
        }

        return string.Join("\n", statusLines);
    }
    #endregion
    #region Debug

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
    #region Economy
    public long sellPrice = 0;
    public long buyPrice = 0;
    public bool isSellable = true;
    public bool isTradeable = true;
    public int buyCurrencyType = 0; // CurrencyType as int
    #endregion
    #region Set Info
    public bool isSetItem = false;
    public string setName = "";
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
    public float lifeStealBonus = 0f;
    #endregion
    
    #region Potion Stats (เพิ่มใหม่)
    public int healAmount = 0;
    public int manaAmount = 0;
    public float healPercentage = 0f;
    public float manaPercentage = 0f;
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
    public CurrencyType GetBuyCurrencyType()
    {
        return (CurrencyType)buyCurrencyType;
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
        stats.lifeStealBonus = lifeStealBonus;
        stats.healAmount = healAmount;
        stats.manaAmount = manaAmount;
        stats.healPercentage = healPercentage;
        stats.manaPercentage = manaPercentage;
        return stats;
    }
    #endregion

    #endregion
}
[System.Serializable]
public class TradeRequirementData
{
    [Header("Required Item")]
    public ItemData requiredItem;

    [Header("Required Quantity")]
    [Range(1, 999)]
    public int requiredQuantity = 1;

    [Header("Optional Description")]
    public string description = "";

    public bool IsValid()
    {
        return requiredItem != null && requiredQuantity > 0;
    }
}

// ✅ 2. เพิ่ม Trade Recipe Class ใน ItemData.cs
[System.Serializable]
public class TradeRecipeData
{
    [Header("🔄 Trade Recipe Settings")]
    [Tooltip("Can this item be obtained through trading?")]
    public bool isTradeableItem = false;

    [Header("💰 Gold Cost")]
    [Tooltip("Gold required for this trade")]
    public long requiredGold = 0;

    [Header("📦 Required Items")]
    [Tooltip("Items needed for this trade")]
    public List<TradeRequirementData> requiredItems = new List<TradeRequirementData>();

    [Header("📝 Trade Info")]
    [Tooltip("Display name for this trade")]
    public string tradeName = "";

    [TextArea(2, 4)]
    [Tooltip("Description of this trade")]
    public string tradeDescription = "";

    [Header("🎯 Trade Category")]
    [Tooltip("Category for organizing trades")]
    public TradeCategory tradeCategory = TradeCategory.Equipment;

    public bool IsValid()
    {
        return isTradeableItem && requiredItems.Count > 0 && requiredItems.All(req => req.IsValid());
    }

    public string GetRequirementsText()
    {
        if (requiredItems.Count == 0) return "No requirements";

        List<string> requirements = new List<string>();

        foreach (var req in requiredItems)
        {
            if (req.IsValid())
            {
                requirements.Add($"{req.requiredItem.ItemName} x{req.requiredQuantity}");
            }
        }

        if (requiredGold > 0)
        {
            requirements.Add($"{requiredGold:N0} Gold");
        }

        return string.Join("\n", requirements);
    }
}

// ✅ 3. เพิ่ม Trade Category Enum
[System.Serializable]
public enum TradeCategory
{
    Equipment,      // อุปกรณ์
    Consumables,    // ของใช้
    Materials,      // วัสดุ
    Rare,          // ของหายาก
    Legendary,     // ของตำนาน
    Special        // พิเศษ
}