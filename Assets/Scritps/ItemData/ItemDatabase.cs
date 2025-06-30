using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory System/Item Database")]
public class ItemDatabase : ScriptableObject
{
    #region Database
    [Header("All Items")]
    [SerializeField] private List<ItemData> allItems = new List<ItemData>();

    [Header("Organized by Type")]
    [SerializeField] private List<ItemData> weapons = new List<ItemData>();
    [SerializeField] private List<ItemData> headItems = new List<ItemData>();
    [SerializeField] private List<ItemData> armorItems = new List<ItemData>();
    [SerializeField] private List<ItemData> pantsItems = new List<ItemData>();
    [SerializeField] private List<ItemData> shoesItems = new List<ItemData>();
    [SerializeField] private List<ItemData> runes = new List<ItemData>();
    [SerializeField] private List<ItemData> potions = new List<ItemData>(); // เพิ่มบรรทัดนี้

    #endregion

    #region Singleton Access
    private static ItemDatabase _instance;
    public static ItemDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<ItemDatabase>("ItemDatabase");
                if (_instance == null)
                {
                    Debug.LogError("❌ ItemDatabase not found in Resources folder!");
                }
            }
            return _instance;
        }
    }
    #endregion

    #region Initialization
    void OnValidate()
    {
        if (Application.isEditor)
        {
            RefreshItemLists();
        }
    }

    [ContextMenu("Refresh Item Lists")]
    public void RefreshItemLists()
    {
        // เคลียร์ lists
        weapons.Clear();
        headItems.Clear();
        armorItems.Clear();
        pantsItems.Clear();
        shoesItems.Clear();
        runes.Clear();
        potions.Clear(); // เพิ่มบรรทัดนี้


        // จัดกลุ่มตาม type
        foreach (var item in allItems)
        {
            if (item == null) continue;

            switch (item.ItemType)
            {
                case ItemType.Weapon:
                    weapons.Add(item);
                    break;
                case ItemType.Head:
                    headItems.Add(item);
                    break;
                case ItemType.Armor:
                    armorItems.Add(item);
                    break;
                case ItemType.Pants:
                    pantsItems.Add(item);
                    break;
                case ItemType.Shoes:
                    shoesItems.Add(item);
                    break;
                case ItemType.Rune:
                    runes.Add(item);
                    break;
                case ItemType.Potion: // เพิ่ม case นี้
                    potions.Add(item);
                    break;
            }
        }

        Debug.Log($"📦 ItemDatabase refreshed: {allItems.Count} total items");
        Debug.Log($"   🗡️ Weapons: {weapons.Count}");
        Debug.Log($"   ⛑️ Head: {headItems.Count}");
        Debug.Log($"   🛡️ Armor: {armorItems.Count}");
        Debug.Log($"   👖 Pants: {pantsItems.Count}");
        Debug.Log($"   👟 Shoes: {shoesItems.Count}");
        Debug.Log($"   🔮 Runes: {runes.Count}");
    }
    #endregion

    #region Query Methods
    public ItemData GetItemById(string itemId)
    {
        return allItems.FirstOrDefault(item => item.ItemId == itemId);
    }

    public ItemData GetItemByName(string itemName)
    {
        return allItems.FirstOrDefault(item => item.ItemName == itemName);
    }

    public List<ItemData> GetItemsByType(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Weapon: return new List<ItemData>(weapons);
            case ItemType.Head: return new List<ItemData>(headItems);
            case ItemType.Armor: return new List<ItemData>(armorItems);
            case ItemType.Pants: return new List<ItemData>(pantsItems);
            case ItemType.Shoes: return new List<ItemData>(shoesItems);
            case ItemType.Rune: return new List<ItemData>(runes);
            case ItemType.Potion: return new List<ItemData>(potions); // เพิ่มบรรทัดนี้

            default: return new List<ItemData>();
        }
    }

    public List<ItemData> GetItemsByTier(ItemTier tier)
    {
        return allItems.Where(item => item.Tier == tier).ToList();
    }

    public List<ItemData> GetAllItems()
    {
        return new List<ItemData>(allItems);
    }

    public ItemData GetRandomItem()
    {
        if (allItems.Count == 0) return null;
        int randomIndex = Random.Range(0, allItems.Count);
        return allItems[randomIndex];
    }

    public ItemData GetRandomItemByType(ItemType itemType)
    {
        var itemsOfType = GetItemsByType(itemType);
        if (itemsOfType.Count == 0) return null;
        int randomIndex = Random.Range(0, itemsOfType.Count);
        return itemsOfType[randomIndex];
    }

    public ItemData GetRandomItemByTier(ItemTier tier)
    {
        var itemsOfTier = GetItemsByTier(tier);
        if (itemsOfTier.Count == 0) return null;
        int randomIndex = Random.Range(0, itemsOfTier.Count);
        return itemsOfTier[randomIndex];
    }
    #endregion

    #region Management Methods
    public void AddItem(ItemData item)
    {
        if (item == null) return;

        if (!allItems.Contains(item))
        {
            allItems.Add(item);
            RefreshItemLists();
            Debug.Log($"✅ Added item: {item.ItemName}");
        }
    }

    public void RemoveItem(ItemData item)
    {
        if (item == null) return;

        if (allItems.Contains(item))
        {
            allItems.Remove(item);
            RefreshItemLists();
            Debug.Log($"❌ Removed item: {item.ItemName}");
        }
    }

    public void RemoveItemById(string itemId)
    {
        var item = GetItemById(itemId);
        if (item != null)
        {
            RemoveItem(item);
        }
    }
    #endregion

    #region Test Data Generation

    [ContextMenu("🧪 Create Sample Health Potion")]
    private void CreateSampleHealthPotion()
    {
        ItemData healthPotion = CreateInstance<ItemData>();

        // Basic Info
        healthPotion.name = "HealthPotion_Common";
        healthPotion.itemName = "Health Potion";
        healthPotion.itemType = ItemType.Potion;
        healthPotion.tier = ItemTier.Common;
        healthPotion.description = "Restores 50 HP instantly. Essential for survival.";
        healthPotion.maxStackSize = 99;
        healthPotion.isStackable = true;

        // Potion Stats
        healthPotion.Stats.healAmount = 50;  // ฟื้นฟู 50 HP

        // เพิ่มลง database
        AddItem(healthPotion);

        Debug.Log("✅ Created Health Potion: +50 HP");
    }

    [ContextMenu("🧪 Create Sample Mana Potion")]
    private void CreateSampleManaPotion()
    {
        ItemData manaPotion = CreateInstance<ItemData>();

        // Basic Info
        manaPotion.name = "ManaPotion_Common";
        manaPotion.itemName = "Mana Potion";
        manaPotion.itemType = ItemType.Potion;
        manaPotion.tier = ItemTier.Common;
        manaPotion.description = "Restores 30 MP instantly. Keep your magic flowing.";
        manaPotion.maxStackSize = 99;
        manaPotion.isStackable = true;

        // Potion Stats
        manaPotion.Stats.manaAmount = 30;  // ฟื้นฟู 30 MP

        // เพิ่มลง database
        AddItem(manaPotion);

        Debug.Log("✅ Created Mana Potion: +30 MP");
    }

    [ContextMenu("🧪 Create Sample Mixed Potion")]
    private void CreateSampleMixedPotion()
    {
        ItemData mixedPotion = CreateInstance<ItemData>();

        // Basic Info
        mixedPotion.name = "MixedPotion_Uncommon";
        mixedPotion.itemName = "Greater Healing Potion";
        mixedPotion.itemType = ItemType.Potion;
        mixedPotion.tier = ItemTier.Uncommon;
        mixedPotion.description = "Restores both HP and MP. A versatile recovery solution.";
        mixedPotion.maxStackSize = 50;
        mixedPotion.isStackable = true;

        // Potion Stats
        mixedPotion.Stats.healAmount = 75;   // ฟื้นฟู 75 HP
        mixedPotion.Stats.manaAmount = 45;   // ฟื้นฟู 45 MP

        // เพิ่มลง database
        AddItem(mixedPotion);

        Debug.Log("✅ Created Mixed Potion: +75 HP, +45 MP");
    }

    [ContextMenu("🧪 Create Sample Percentage Potion")]
    private void CreateSamplePercentagePotion()
    {
        ItemData percentPotion = CreateInstance<ItemData>();

        // Basic Info
        percentPotion.name = "PercentPotion_Rare";
        percentPotion.itemName = "Supreme Recovery Elixir";
        percentPotion.itemType = ItemType.Potion;
        percentPotion.tier = ItemTier.Rare;
        percentPotion.description = "Restores 25% of max HP and MP. For the wealthy adventurer.";
        percentPotion.maxStackSize = 10;
        percentPotion.isStackable = true;

        // Potion Stats (percentage-based)
        percentPotion.Stats.healPercentage = 0.25f;  // ฟื้นฟู 25% ของ Max HP
        percentPotion.Stats.manaPercentage = 0.25f;  // ฟื้นฟู 25% ของ Max MP

        // เพิ่มลง database
        AddItem(percentPotion);

        Debug.Log("✅ Created Percentage Potion: +25% Max HP/MP");
    }

    [ContextMenu("🧪 Create All Sample Potions")]
    private void CreateAllSamplePotions()
    {
        CreateSampleHealthPotion();
        CreateSampleManaPotion();
        CreateSampleMixedPotion();
        CreateSamplePercentagePotion();

        Debug.Log("🎉 Created all sample potions! Check your database.");
    }
    #endregion
}