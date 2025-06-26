using UnityEngine;

#region Item Enums
[System.Serializable]
public enum ItemType
{
    Weapon,     // อาวุธ (ดาบ, ไม้กายสิทธิ์, คันธนู)
    Head,       // หมวก/หมวกนักรบ
    Armor,      // เสื้อเกราะ/เสื้อผ้า
    Pants,      // กางเกง/กระโปรง
    Shoes,      // รองเท้า/บู๊ท
    Rune        // รูน/พลอย (เพิ่มสเตตพิเศษ)
}

[System.Serializable]
public enum ItemRarity
{
    Common,     // ขาว - ไอเทมธรรมดา
    Uncommon,   // เขียว - ไอเทมหายาก
    Rare,       // น้ำเงิน - ไอเทมหายากมาก
    Epic,       // ม่วง - ไอเทมระดับสูง
    Legendary   // ส้ม/ทอง - ไอเทมระดับตำนาน
}
#endregion

[CreateAssetMenu(fileName = "New Item", menuName = "RPG/Item Data")]
public class ItemData : ScriptableObject
{
    #region Basic Item Info
    [Header("🎯 Basic Item Information")]
    [SerializeField] private string itemName = "New Item";
    [SerializeField] private ItemType itemType = ItemType.Weapon;
    [SerializeField] private ItemRarity itemRarity = ItemRarity.Common;

    [Space(5)]
    [TextArea(3, 5)]
    [SerializeField] private string description = "A mysterious item with unknown powers...";

    [Space(5)]
    [SerializeField] private Sprite itemIcon;
    [SerializeField] private int maxStackSize = 1; // สำหรับไอเทมที่ stack ได้ (รูน/ยา)
    #endregion

    #region Equipment Stats
    [Header("📊 Item Stats")]
    [SerializeField] private EquipmentStats itemStats = new EquipmentStats();
    #endregion

    #region Level Requirements & Value
    [Header("⚖️ Requirements & Value")]
    [SerializeField] private int levelRequirement = 1;
    [SerializeField] private int sellValue = 10;
    [SerializeField] private int buyValue = 50;
    #endregion

    #region Public Properties
    public string ItemName { get { return itemName; } }
    public ItemType ItemType { get { return itemType; } }
    public ItemRarity ItemRarity { get { return itemRarity; } }
    public string Description { get { return description; } }
    public Sprite ItemIcon { get { return itemIcon; } }
    public int MaxStackSize { get { return maxStackSize; } }
    public EquipmentStats ItemStats { get { return itemStats; } }
    public int LevelRequirement { get { return levelRequirement; } }
    public int SellValue { get { return sellValue; } }
    public int BuyValue { get { return buyValue; } }
    #endregion

    #region Utility Methods
    public bool CanStack()
    {
        return maxStackSize > 1;
    }

    public bool CanEquip(int characterLevel)
    {
        return characterLevel >= levelRequirement;
    }

    public Color GetRarityColor()
    {
        switch (itemRarity)
        {
            case ItemRarity.Common:
                return Color.white;
            case ItemRarity.Uncommon:
                return Color.green;
            case ItemRarity.Rare:
                return Color.blue;
            case ItemRarity.Epic:
                return Color.magenta;
            case ItemRarity.Legendary:
                return Color.yellow;
            default:
                return Color.white;
        }
    }

    public string GetRarityText()
    {
        switch (itemRarity)
        {
            case ItemRarity.Common:
                return "Common";
            case ItemRarity.Uncommon:
                return "Uncommon";
            case ItemRarity.Rare:
                return "Rare";
            case ItemRarity.Epic:
                return "Epic";
            case ItemRarity.Legendary:
                return "Legendary";
            default:
                return "Unknown";
        }
    }

    public string GetItemTypeText()
    {
        switch (itemType)
        {
            case ItemType.Weapon:
                return "Weapon";
            case ItemType.Head:
                return "Helmet";
            case ItemType.Armor:
                return "Armor";
            case ItemType.Pants:
                return "Leggings";
            case ItemType.Shoes:
                return "Boots";
            case ItemType.Rune:
                return "Rune";
            default:
                return "Unknown";
        }
    }

    // สำหรับแปลง ItemData เป็น EquipmentData เพื่อใช้กับระบบเดิม
    public EquipmentData ToEquipmentData()
    {
        EquipmentData equipData = new EquipmentData();
        equipData.itemName = this.itemName;
        equipData.stats = this.itemStats;
        equipData.itemIcon = this.itemIcon;
        return equipData;
    }
    #endregion

    #region Context Menu for Testing
    [ContextMenu("📊 Show Item Info")]
    private void ShowItemInfo()
    {
        Debug.Log("=== ITEM INFORMATION ===");
        Debug.Log($"📛 Name: {itemName}");
        Debug.Log($"🏷️ Type: {GetItemTypeText()}");
        Debug.Log($"⭐ Rarity: {GetRarityText()}");
        Debug.Log($"📖 Description: {description}");
        Debug.Log($"🎯 Level Req: {levelRequirement}");
        Debug.Log($"💰 Value: Buy {buyValue}g, Sell {sellValue}g");
        Debug.Log($"📦 Max Stack: {maxStackSize}");

        Debug.Log("\n--- STATS ---");
        if (itemStats.attackDamageBonus > 0)
            Debug.Log($"⚔️ Attack Damage: +{itemStats.attackDamageBonus}");
        if (itemStats.magicDamageBonus > 0)
            Debug.Log($"🪄 Magic Damage: +{itemStats.magicDamageBonus}");
        if (itemStats.armorBonus > 0)
            Debug.Log($"🛡️ Armor: +{itemStats.armorBonus}");
        if (itemStats.maxHpBonus > 0)
            Debug.Log($"❤️ HP: +{itemStats.maxHpBonus}");
        if (itemStats.maxManaBonus > 0)
            Debug.Log($"💙 Mana: +{itemStats.maxManaBonus}");
        if (itemStats.criticalChanceBonus > 0)
            Debug.Log($"💥 Critical Chance: +{itemStats.criticalChanceBonus}%");
        if (itemStats.criticalMultiplierBonus > 0)
            Debug.Log($"🔥 Critical Damage: +{itemStats.criticalMultiplierBonus}%");
        if (itemStats.moveSpeedBonus > 0)
            Debug.Log($"💨 Move Speed: +{itemStats.moveSpeedBonus}");
        if (itemStats.attackSpeedBonus > 0)
            Debug.Log($"⚡ Attack Speed: +{itemStats.attackSpeedBonus}");

        Debug.Log("========================");
    }

    [ContextMenu("🎨 Test Rarity Color")]
    private void TestRarityColor()
    {
        Color color = GetRarityColor();
        Debug.Log($"Rarity: {GetRarityText()}, Color: {color}");
    }
    #endregion

    #region Validation
    private void OnValidate()
    {
        // ป้องกันค่าติดลบ
        levelRequirement = Mathf.Max(1, levelRequirement);
        sellValue = Mathf.Max(0, sellValue);
        buyValue = Mathf.Max(sellValue, buyValue); // ราคาซื้อต้องมากกว่าราคาขาย
        maxStackSize = Mathf.Max(1, maxStackSize);

        // ตั้งค่า stack size ตาม item type
        if (itemType == ItemType.Rune)
        {
            maxStackSize = Mathf.Max(maxStackSize, 10); // รูนควร stack ได้
        }
        else if (itemType != ItemType.Rune)
        {
            maxStackSize = 1; // อุปกรณ์อื่นไม่ stack
        }
    }
    #endregion
}