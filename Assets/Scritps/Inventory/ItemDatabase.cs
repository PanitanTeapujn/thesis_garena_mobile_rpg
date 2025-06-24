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
    [ContextMenu("Generate Test Items")]
    public void GenerateTestItems()
    {
        Debug.Log("🔧 Generating test items...");

        // สร้าง test items สำหรับแต่ละประเภท
        GenerateTestWeapons();
        GenerateTestHeadItems();
        GenerateTestArmorItems();
        GenerateTestPantsItems();
        GenerateTestShoesItems();
        GenerateTestRunes();

        RefreshItemLists();
        Debug.Log($"✅ Generated {allItems.Count} test items");
    }

    private void GenerateTestWeapons()
    {
        // Test Weapons
        var ironSword = CreateTestItem("iron_sword_001", "Iron Sword", ItemType.Weapon, ItemTier.Common,
            "A basic iron sword for beginners.");
        ironSword.Stats.attackDamageBonus = 10;
        allItems.Add(ironSword);

        var steelSword = CreateTestItem("steel_sword_001", "Steel Sword", ItemType.Weapon, ItemTier.Uncommon,
            "A sturdy steel sword with improved damage.");
        steelSword.Stats.attackDamageBonus = 20;
        steelSword.Stats.criticalChanceBonus = 0.05f;
        allItems.Add(steelSword);
    }

    private void GenerateTestHeadItems()
    {
        // Test Head Items
        var leatherHelmet = CreateTestItem("leather_helmet_001", "Leather Helmet", ItemType.Head, ItemTier.Common,
            "Basic protection for your head.");
        leatherHelmet.Stats.armorBonus = 5;
        leatherHelmet.Stats.maxHpBonus = 20;
        allItems.Add(leatherHelmet);
    }

    private void GenerateTestArmorItems()
    {
        // Test Armor Items
        var chainmail = CreateTestItem("chainmail_001", "Chainmail Armor", ItemType.Armor, ItemTier.Uncommon,
            "Flexible metal armor providing good protection.");
        chainmail.Stats.armorBonus = 15;
        chainmail.Stats.maxHpBonus = 40;
        chainmail.Stats.physicalResistanceBonus = 0.1f;
        allItems.Add(chainmail);
    }

    private void GenerateTestPantsItems()
    {
        // Test Pants Items
        var leatherPants = CreateTestItem("leather_pants_001", "Leather Pants", ItemType.Pants, ItemTier.Common,
            "Comfortable leather pants for mobility.");
        leatherPants.Stats.armorBonus = 3;
        leatherPants.Stats.moveSpeedBonus = 0.5f;
        allItems.Add(leatherPants);
    }

    private void GenerateTestShoesItems()
    {
        // Test Shoes Items
        var speedBoots = CreateTestItem("speed_boots_001", "Speed Boots", ItemType.Shoes, ItemTier.Rare,
            "Boots that enhance movement speed and agility.");
        speedBoots.Stats.moveSpeedBonus = 1.5f;
        speedBoots.Stats.evasionRateBonus = 0.08f;
        allItems.Add(speedBoots);
    }

    private void GenerateTestRunes()
    {
        // Test Runes
        var strengthRune = CreateTestItem("strength_rune_001", "Rune of Strength", ItemType.Rune, ItemTier.Epic,
            "A mystical rune that enhances physical power.");
        strengthRune.Stats.attackDamageBonus = 15;
        strengthRune.Stats.criticalDamageBonus = 0.25f;
        allItems.Add(strengthRune);

        var wisdomRune = CreateTestItem("wisdom_rune_001", "Rune of Wisdom", ItemType.Rune, ItemTier.Epic,
            "A mystical rune that enhances magical abilities.");
        wisdomRune.Stats.magicDamageBonus = 25;
        wisdomRune.Stats.maxManaBonus = 50;
        allItems.Add(wisdomRune);

        var vitalityRune = CreateTestItem("vitality_rune_001", "Rune of Vitality", ItemType.Rune, ItemTier.Rare,
            "A mystical rune that enhances life force.");
        vitalityRune.Stats.maxHpBonus = 100;
        vitalityRune.Stats.reductionCoolDownBonus = 0.1f;
        allItems.Add(vitalityRune);
    }

    private ItemData CreateTestItem(string id, string name, ItemType type, ItemTier tier, string description)
    {
        ItemData item = CreateInstance<ItemData>();

        // Use reflection to set private fields (for testing only)
        var itemIdField = typeof(ItemData).GetField("itemId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var itemNameField = typeof(ItemData).GetField("itemName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var itemTypeField = typeof(ItemData).GetField("itemType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var tierField = typeof(ItemData).GetField("tier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var descriptionField = typeof(ItemData).GetField("description", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var statsField = typeof(ItemData).GetField("stats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        itemIdField?.SetValue(item, id);
        itemNameField?.SetValue(item, name);
        itemTypeField?.SetValue(item, type);
        tierField?.SetValue(item, tier);
        descriptionField?.SetValue(item, description);
        statsField?.SetValue(item, new ItemStats());

        return item;
    }
    #endregion

    #region Debug Methods
    [ContextMenu("Debug All Items")]
    public void DebugAllItems()
    {
        Debug.Log($"📦 ItemDatabase Contents ({allItems.Count} items):");

        foreach (var item in allItems)
        {
            if (item != null)
            {
                Debug.Log($"   {item.GetTierText()} {item.ItemType}: {item.ItemName} ({item.ItemId})");
            }
        }
    }

    [ContextMenu("Debug Items by Type")]
    public void DebugItemsByType()
    {
        Debug.Log("📦 Items organized by type:");
        Debug.Log($"🗡️ Weapons ({weapons.Count}): {string.Join(", ", weapons.Select(w => w.ItemName))}");
        Debug.Log($"⛑️ Head ({headItems.Count}): {string.Join(", ", headItems.Select(h => h.ItemName))}");
        Debug.Log($"🛡️ Armor ({armorItems.Count}): {string.Join(", ", armorItems.Select(a => a.ItemName))}");
        Debug.Log($"👖 Pants ({pantsItems.Count}): {string.Join(", ", pantsItems.Select(p => p.ItemName))}");
        Debug.Log($"👟 Shoes ({shoesItems.Count}): {string.Join(", ", shoesItems.Select(s => s.ItemName))}");
        Debug.Log($"🔮 Runes ({runes.Count}): {string.Join(", ", runes.Select(r => r.ItemName))}");
    }
    #endregion
}