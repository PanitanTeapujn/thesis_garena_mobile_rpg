using UnityEngine;

[System.Serializable]
public enum ItemType
{
    Consumable,
    Equipment,
    Material,
    Quest
}

[System.Serializable]
public enum EquipmentType
{
    Weapon,
    Helmet,
    Armor,
    Pants,
    Boots
}

[System.Serializable]
public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

[System.Serializable]
public class ItemStats
{
    public int attackDamage;
    public int armor;
    public int maxHp;
    public int maxMana;
    public float moveSpeed;
    public float attackSpeed;
    public float criticalChance;
    public float criticalDamage;
}

[CreateAssetMenu(fileName = "New Item", menuName = "Game/Item")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    public string itemId;
    public string description;
    public Sprite icon;
    public ItemType itemType;
    public ItemRarity rarity;

    [Header("Stack Info")]
    public bool isStackable = true;
    public int maxStackSize = 99;

    [Header("Equipment Info")]
    public EquipmentType equipmentType;
    public ItemStats stats;

    [Header("Consumable Info")]
    public int healAmount;
    public int manaAmount;
    public float buffDuration;

    [Header("Economy")]
    public int buyPrice;
    public int sellPrice;

    public Color GetRarityColor()
    {
        switch (rarity)
        {
            case ItemRarity.Common: return Color.white;
            case ItemRarity.Uncommon: return Color.green;
            case ItemRarity.Rare: return Color.blue;
            case ItemRarity.Epic: return Color.magenta;
            case ItemRarity.Legendary: return Color.yellow;
            default: return Color.white;
        }
    }
}

[System.Serializable]
public class InventoryItem
{
    public string itemId;
    public int quantity;
    public int slotIndex;

    public InventoryItem(string id, int qty, int slot)
    {
        itemId = id;
        quantity = qty;
        slotIndex = slot;
    }
}

[System.Serializable]
public class EquippedItem
{
    public string itemId;
    public EquipmentType equipmentType;

    public EquippedItem(string id, EquipmentType type)
    {
        itemId = id;
        equipmentType = type;
    }
}

[System.Serializable]
public class PlayerInventoryData
{
    public InventoryItem[] inventoryItems;
    public EquippedItem[] equippedItems;
    public int inventorySize = 30;

    public PlayerInventoryData()
    {
        inventoryItems = new InventoryItem[0];
        equippedItems = new EquippedItem[5]; // 5 equipment slots
    }
}
