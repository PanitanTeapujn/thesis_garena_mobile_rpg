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
  
    #endregion
}