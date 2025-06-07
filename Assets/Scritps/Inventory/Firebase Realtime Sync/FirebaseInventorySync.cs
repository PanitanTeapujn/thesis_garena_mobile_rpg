using UnityEngine;
using Firebase.Database;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class FirebaseInventorySync : MonoBehaviour
{
    private DatabaseReference databaseReference;
    private InventoryManager inventoryManager;
    private string playerId;

    public void Initialize(InventoryManager manager)
    {
        inventoryManager = manager;
        databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
        playerId = PlayerPrefs.GetString("PlayerId");

        // Subscribe to inventory changes
        inventoryManager.OnInventoryChanged += SaveInventoryToFirebase;
        inventoryManager.OnItemEquipped += OnItemEquipped;
        inventoryManager.OnItemUnequipped += OnItemUnequipped;
    }

    public IEnumerator LoadInventoryData()
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("Player ID not found!");
            yield break;
        }

        Debug.Log("Loading inventory data from Firebase...");

        // Load inventory
        var inventoryTask = databaseReference.Child("players").Child(playerId).Child("inventory").GetValueAsync();
        yield return new WaitUntil(() => inventoryTask.IsCompleted);

        if (inventoryTask.Exception != null)
        {
            Debug.LogError($"Failed to load inventory: {inventoryTask.Exception}");
        }
        else if (inventoryTask.Result.Exists)
        {
            LoadInventoryFromSnapshot(inventoryTask.Result);
        }

        // Load equipment
        var equipmentTask = databaseReference.Child("players").Child(playerId).Child("equipment").GetValueAsync();
        yield return new WaitUntil(() => equipmentTask.IsCompleted);

        if (equipmentTask.Exception != null)
        {
            Debug.LogError($"Failed to load equipment: {equipmentTask.Exception}");
        }
        else if (equipmentTask.Result.Exists)
        {
            LoadEquipmentFromSnapshot(equipmentTask.Result);
        }

        Debug.Log("Inventory data loaded successfully");
    }

    private void LoadInventoryFromSnapshot(DataSnapshot snapshot)
    {
        foreach (var child in snapshot.Children)
        {
            var itemData = child.Value as Dictionary<string, object>;
            if (itemData != null)
            {
                string itemId = itemData["itemId"].ToString();
                int quantity = System.Convert.ToInt32(itemData["quantity"]);
                int slotIndex = System.Convert.ToInt32(itemData["slotIndex"]);

                // Add item to network inventory
                if (slotIndex < inventoryManager.maxInventorySlots)
                {
                    var networkItem = new NetworkInventoryItem(itemId, quantity);
                    inventoryManager.NetworkInventory.Set(slotIndex, networkItem);
                }
            }
        }
    }

    private void LoadEquipmentFromSnapshot(DataSnapshot snapshot)
    {
        foreach (var child in snapshot.Children)
        {
            var equipData = child.Value as Dictionary<string, object>;
            if (equipData != null)
            {
                string itemId = equipData["itemId"].ToString();
                EquipmentType equipType = (EquipmentType)System.Convert.ToInt32(equipData["equipmentType"]);

                var networkEquip = new NetworkEquippedItem(itemId, equipType);
                inventoryManager.NetworkEquipment.Set((int)equipType, networkEquip);
            }
        }
    }

    public void SaveInventoryToFirebase()
    {
        if (string.IsNullOrEmpty(playerId)) return;

        StartCoroutine(SaveInventoryCoroutine());
    }

    private IEnumerator SaveInventoryCoroutine()
    {
        var inventoryData = new Dictionary<string, object>();

        // Save inventory items
        for (int i = 0; i < inventoryManager.maxInventorySlots; i++)
        {
            var slot = inventoryManager.NetworkInventory[i];
            if (!string.IsNullOrEmpty(slot.itemId.ToString()))
            {
                var itemData = new Dictionary<string, object>
                {
                    ["itemId"] = slot.itemId.ToString(),
                    ["quantity"] = slot.quantity,
                    ["slotIndex"] = i
                };
                inventoryData[$"slot_{i}"] = itemData;
            }
        }

        var inventoryTask = databaseReference.Child("players").Child(playerId).Child("inventory")
            .SetValueAsync(inventoryData);
        yield return new WaitUntil(() => inventoryTask.IsCompleted);

        if (inventoryTask.Exception != null)
        {
            Debug.LogError($"Failed to save inventory: {inventoryTask.Exception}");
        }

        // Save equipment
        var equipmentData = new Dictionary<string, object>();

        for (int i = 0; i < 5; i++)
        {
            var equipped = inventoryManager.NetworkEquipment[i];
            if (!string.IsNullOrEmpty(equipped.itemId.ToString()))
            {
                var equipData = new Dictionary<string, object>
                {
                    ["itemId"] = equipped.itemId.ToString(),
                    ["equipmentType"] = (int)equipped.equipmentType
                };
                equipmentData[$"slot_{i}"] = equipData;
            }
        }

        var equipmentTask = databaseReference.Child("players").Child(playerId).Child("equipment")
            .SetValueAsync(equipmentData);
        yield return new WaitUntil(() => equipmentTask.IsCompleted);

        if (equipmentTask.Exception != null)
        {
            Debug.LogError($"Failed to save equipment: {equipmentTask.Exception}");
        }
    }

    private void OnItemEquipped(EquippedItem item)
    {
        // Save immediately when equipment changes
        SaveInventoryToFirebase();
    }

    private void OnItemUnequipped(EquipmentType equipmentType)
    {
        // Save immediately when equipment changes
        SaveInventoryToFirebase();
    }

    public void SavePlayerStats(ItemStats totalStats)
    {
        if (string.IsNullOrEmpty(playerId)) return;

        var statsData = new Dictionary<string, object>
        {
            ["totalAttackDamage"] = totalStats.attackDamage,
            ["totalArmor"] = totalStats.armor,
            ["totalMaxHp"] = totalStats.maxHp,
            ["totalMaxMana"] = totalStats.maxMana,
            ["totalMoveSpeed"] = totalStats.moveSpeed,
            ["totalAttackSpeed"] = totalStats.attackSpeed,
            ["totalCriticalChance"] = totalStats.criticalChance,
            ["totalCriticalDamage"] = totalStats.criticalDamage,
            ["lastUpdated"] = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        databaseReference.Child("players").Child(playerId).Child("equipmentStats")
            .SetValueAsync(statsData);
    }

    private void OnDestroy()
    {
        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryChanged -= SaveInventoryToFirebase;
            inventoryManager.OnItemEquipped -= OnItemEquipped;
            inventoryManager.OnItemUnequipped -= OnItemUnequipped;
        }
    }
}