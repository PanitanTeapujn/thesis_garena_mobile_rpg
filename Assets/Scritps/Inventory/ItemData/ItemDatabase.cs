using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [Header("All Items")]
    public ItemData[] allItems;

    private Dictionary<string, ItemData> itemLookup;

    private void OnEnable()
    {
        BuildLookupTable();
    }

    private void BuildLookupTable()
    {
        itemLookup = new Dictionary<string, ItemData>();

        foreach (var item in allItems)
        {
            if (item != null && !string.IsNullOrEmpty(item.itemId))
            {
                if (itemLookup.ContainsKey(item.itemId))
                {
                    Debug.LogWarning($"Duplicate item ID found: {item.itemId}");
                }
                else
                {
                    itemLookup[item.itemId] = item;
                }
            }
        }

        Debug.Log($"Item Database loaded with {itemLookup.Count} items");
    }

    public ItemData GetItem(string itemId)
    {
        if (itemLookup == null)
            BuildLookupTable();

        itemLookup.TryGetValue(itemId, out ItemData item);
        return item;
    }

    public ItemData[] GetItemsByType(ItemType itemType)
    {
        if (itemLookup == null)
            BuildLookupTable();

        return allItems.Where(item => item != null && item.itemType == itemType).ToArray();
    }

    public ItemData[] GetEquipmentByType(EquipmentType equipmentType)
    {
        if (itemLookup == null)
            BuildLookupTable();

        return allItems.Where(item => item != null &&
                             item.itemType == ItemType.Equipment &&
                             item.equipmentType == equipmentType).ToArray();
    }

    public ItemData[] GetItemsByRarity(ItemRarity rarity)
    {
        if (itemLookup == null)
            BuildLookupTable();

        return allItems.Where(item => item != null && item.rarity == rarity).ToArray();
    }

    public ItemData GetRandomItem(ItemType itemType = ItemType.Consumable)
    {
        var items = GetItemsByType(itemType);
        if (items.Length > 0)
        {
            return items[Random.Range(0, items.Length)];
        }
        return null;
    }

    public ItemData GetRandomEquipment(EquipmentType equipmentType)
    {
        var equipment = GetEquipmentByType(equipmentType);
        if (equipment.Length > 0)
        {
            return equipment[Random.Range(0, equipment.Length)];
        }
        return null;
    }

    // Validate database integrity
    [ContextMenu("Validate Database")]
    public void ValidateDatabase()
    {
        var duplicateIds = new List<string>();
        var itemIds = new HashSet<string>();

        foreach (var item in allItems)
        {
            if (item == null) continue;

            if (string.IsNullOrEmpty(item.itemId))
            {
                Debug.LogError($"Item {item.name} has empty itemId!");
                continue;
            }

            if (!itemIds.Add(item.itemId))
            {
                duplicateIds.Add(item.itemId);
            }

            // Validate equipment stats
            if (item.itemType == ItemType.Equipment)
            {
                if (item.stats.attackDamage < 0 || item.stats.armor < 0)
                {
                    Debug.LogWarning($"Item {item.itemName} has negative stats!");
                }
            }

            // Validate stack size
            if (item.isStackable && item.maxStackSize <= 0)
            {
                Debug.LogError($"Stackable item {item.itemName} has invalid max stack size!");
            }
        }

        if (duplicateIds.Count > 0)
        {
            Debug.LogError($"Found duplicate item IDs: {string.Join(", ", duplicateIds)}");
        }
        else
        {
            Debug.Log("Database validation completed successfully!");
        }
    }
}