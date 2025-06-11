using UnityEngine;
using Firebase.Database;
using System.Collections;
using System.Collections.Generic;

public class HybridFirebaseSync : MonoBehaviour
{
    private DatabaseReference databaseReference;
    private HybridInventoryManager hybridInventoryManager;
    private string playerId;
    private bool isNetworkMode = false;
    
    public void Initialize(HybridInventoryManager manager, bool networkMode)
    {
        hybridInventoryManager = manager;
        isNetworkMode = networkMode;
        databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
        playerId = PlayerPrefs.GetString("PlayerId");
        
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogWarning("No PlayerId found! HybridInventoryManager will use PlayerPrefs only.");
        }
        
        Debug.Log($"HybridFirebaseSync initialized - Network Mode: {isNetworkMode}");
    }
    
    #region Local Mode Data Loading/Saving
    
    public IEnumerator LoadLocalInventoryData()
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogWarning("No PlayerId - skipping Firebase load for local mode");
            yield break;
        }
        
        Debug.Log("Loading local inventory data from Firebase...");
        
        // Load inventory
        var inventoryTask = databaseReference.Child("players").Child(playerId).Child("inventory").GetValueAsync();
        yield return new WaitUntil(() => inventoryTask.IsCompleted);
        
        if (inventoryTask.Exception != null)
        {
            Debug.LogError($"Failed to load inventory: {inventoryTask.Exception}");
        }
        else if (inventoryTask.Result.Exists)
        {
            LoadLocalInventoryFromSnapshot(inventoryTask.Result);
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
            LoadLocalEquipmentFromSnapshot(equipmentTask.Result);
        }
        
        Debug.Log("Local inventory data loaded from Firebase successfully");
    }
    
    private void LoadLocalInventoryFromSnapshot(DataSnapshot snapshot)
    {
        foreach (var child in snapshot.Children)
        {
            var itemData = child.Value as Dictionary<string, object>;
            if (itemData != null)
            {
                string itemId = itemData["itemId"].ToString();
                int quantity = System.Convert.ToInt32(itemData["quantity"]);
                
                // Add to local inventory via HybridInventoryManager
                hybridInventoryManager.AddItem(itemId, quantity);
            }
        }
    }
    
    private void LoadLocalEquipmentFromSnapshot(DataSnapshot snapshot)
    {
        foreach (var child in snapshot.Children)
        {
            var equipData = child.Value as Dictionary<string, object>;
            if (equipData != null)
            {
                string itemId = equipData["itemId"].ToString();
                EquipmentType equipType = (EquipmentType)System.Convert.ToInt32(equipData["equipmentType"]);
                
                // Equip item via HybridInventoryManager
                hybridInventoryManager.EquipItem(itemId);
            }
        }
    }
    
    public void SaveLocalInventoryData()
    {
        if (string.IsNullOrEmpty(playerId) || isNetworkMode) return;
        
        StartCoroutine(SaveLocalInventoryCoroutine());
    }
    
    private IEnumerator SaveLocalInventoryCoroutine()
    {
        var inventoryData = new Dictionary<string, object>();
        
        // Save inventory items
        var inventoryItems = hybridInventoryManager.GetInventoryItems();
        foreach (var kvp in inventoryItems)
        {
            var itemData = new Dictionary<string, object>
            {
                ["itemId"] = kvp.Value.itemId,
                ["quantity"] = kvp.Value.quantity,
                ["slotIndex"] = kvp.Value.slotIndex
            };
            inventoryData[$"slot_{kvp.Key}"] = itemData;
        }
        
        var inventoryTask = databaseReference.Child("players").Child(playerId).Child("inventory")
            .SetValueAsync(inventoryData);
        yield return new WaitUntil(() => inventoryTask.IsCompleted);
        
        if (inventoryTask.Exception != null)
        {
            Debug.LogError($"Failed to save local inventory: {inventoryTask.Exception}");
        }
        
        // Save equipment
        var equipmentData = new Dictionary<string, object>();
        
        for (int i = 0; i < 5; i++)
        {
            EquipmentType equipType = (EquipmentType)i;
            var equippedItem = hybridInventoryManager.GetEquippedItem(equipType);
            
            if (equippedItem != null)
            {
                var equipData = new Dictionary<string, object>
                {
                    ["itemId"] = equippedItem.itemId,
                    ["equipmentType"] = (int)equippedItem.equipmentType
                };
                equipmentData[$"slot_{i}"] = equipData;
            }
        }
        
        var equipmentTask = databaseReference.Child("players").Child(playerId).Child("equipment")
            .SetValueAsync(equipmentData);
        yield return new WaitUntil(() => equipmentTask.IsCompleted);
        
        if (equipmentTask.Exception != null)
        {
            Debug.LogError($"Failed to save local equipment: {equipmentTask.Exception}");
        }
        else
        {
            Debug.Log("Local inventory saved to Firebase successfully");
        }
    }
    
    #endregion
    
    #region Network Mode Data Loading/Saving
    
    public IEnumerator LoadNetworkInventoryData()
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogWarning("No PlayerId - skipping Firebase load for network mode");
            yield break;
        }
        
        Debug.Log("Loading network inventory data from Firebase...");
        
        // Load inventory
        var inventoryTask = databaseReference.Child("players").Child(playerId).Child("inventory").GetValueAsync();
        yield return new WaitUntil(() => inventoryTask.IsCompleted);
        
        if (inventoryTask.Exception != null)
        {
            Debug.LogError($"Failed to load network inventory: {inventoryTask.Exception}");
        }
        else if (inventoryTask.Result.Exists)
        {
            LoadNetworkInventoryFromSnapshot(inventoryTask.Result);
        }
        
        // Load equipment
        var equipmentTask = databaseReference.Child("players").Child(playerId).Child("equipment").GetValueAsync();
        yield return new WaitUntil(() => equipmentTask.IsCompleted);
        
        if (equipmentTask.Exception != null)
        {
            Debug.LogError($"Failed to load network equipment: {equipmentTask.Exception}");
        }
        else if (equipmentTask.Result.Exists)
        {
            LoadNetworkEquipmentFromSnapshot(equipmentTask.Result);
        }
        
        Debug.Log("Network inventory data loaded from Firebase successfully");
    }
    
    private void LoadNetworkInventoryFromSnapshot(DataSnapshot snapshot)
    {
        foreach (var child in snapshot.Children)
        {
            var itemData = child.Value as Dictionary<string, object>;
            if (itemData != null)
            {
                string itemId = itemData["itemId"].ToString();
                int quantity = System.Convert.ToInt32(itemData["quantity"]);
                
                // Add to network inventory via HybridInventoryManager
                hybridInventoryManager.AddItem(itemId, quantity);
            }
        }
    }
    
    private void LoadNetworkEquipmentFromSnapshot(DataSnapshot snapshot)
    {
        foreach (var child in snapshot.Children)
        {
            var equipData = child.Value as Dictionary<string, object>;
            if (equipData != null)
            {
                string itemId = equipData["itemId"].ToString();
                EquipmentType equipType = (EquipmentType)System.Convert.ToInt32(equipData["equipmentType"]);
                
                // Equip item via HybridInventoryManager
                hybridInventoryManager.EquipItem(itemId);
            }
        }
    }
    
    public void SaveNetworkInventoryData()
    {
        if (string.IsNullOrEmpty(playerId) || !isNetworkMode) return;
        
        StartCoroutine(SaveNetworkInventoryCoroutine());
    }
    
    private IEnumerator SaveNetworkInventoryCoroutine()
    {
        var inventoryData = new Dictionary<string, object>();
        
        // Save inventory items from NetworkArray
        var inventoryItems = hybridInventoryManager.GetInventoryItems();
        foreach (var kvp in inventoryItems)
        {
            var itemData = new Dictionary<string, object>
            {
                ["itemId"] = kvp.Value.itemId,
                ["quantity"] = kvp.Value.quantity,
                ["slotIndex"] = kvp.Value.slotIndex
            };
            inventoryData[$"slot_{kvp.Key}"] = itemData;
        }
        
        var inventoryTask = databaseReference.Child("players").Child(playerId).Child("inventory")
            .SetValueAsync(inventoryData);
        yield return new WaitUntil(() => inventoryTask.IsCompleted);
        
        if (inventoryTask.Exception != null)
        {
            Debug.LogError($"Failed to save network inventory: {inventoryTask.Exception}");
        }
        
        // Save equipment from NetworkArray
        var equipmentData = new Dictionary<string, object>();
        
        for (int i = 0; i < 5; i++)
        {
            EquipmentType equipType = (EquipmentType)i;
            var equippedItem = hybridInventoryManager.GetEquippedItem(equipType);
            
            if (equippedItem != null)
            {
                var equipData = new Dictionary<string, object>
                {
                    ["itemId"] = equippedItem.itemId,
                    ["equipmentType"] = (int)equippedItem.equipmentType
                };
                equipmentData[$"slot_{i}"] = equipData;
            }
        }
        
        var equipmentTask = databaseReference.Child("players").Child(playerId).Child("equipment")
            .SetValueAsync(equipmentData);
        yield return new WaitUntil(() => equipmentTask.IsCompleted);
        
        if (equipmentTask.Exception != null)
        {
            Debug.LogError($"Failed to save network equipment: {equipmentTask.Exception}");
        }
        else
        {
            Debug.Log("Network inventory saved to Firebase successfully");
        }
    }
    
    #endregion
    
    #region Stats Saving
    
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
            ["lastUpdated"] = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            ["mode"] = isNetworkMode ? "network" : "local"
        };
        
        databaseReference.Child("players").Child(playerId).Child("equipmentStats")
            .SetValueAsync(statsData);
    }
    
    #endregion
    
    #region Auto Save Events
    
    void Start()
    {
        // Subscribe to inventory changes for auto-save
        if (hybridInventoryManager != null)
        {
            hybridInventoryManager.OnInventoryChanged += OnInventoryChanged;
            hybridInventoryManager.OnItemEquipped += OnItemEquipped;
            hybridInventoryManager.OnItemUnequipped += OnItemUnequipped;
        }
    }
    
    private void OnInventoryChanged()
    {
        // Auto-save when inventory changes
        if (isNetworkMode)
        {
            SaveNetworkInventoryData();
        }
        else
        {
            SaveLocalInventoryData();
        }
    }
    
    private void OnItemEquipped(EquippedItem item)
    {
        // Save immediately when equipment changes
        OnInventoryChanged();
        
        // Also save stats
        var totalStats = hybridInventoryManager.GetTotalEquipmentStats();
        SavePlayerStats(totalStats);
    }
    
    private void OnItemUnequipped(EquipmentType equipmentType)
    {
        // Save immediately when equipment changes
        OnInventoryChanged();
        
        // Also save stats
        var totalStats = hybridInventoryManager.GetTotalEquipmentStats();
        SavePlayerStats(totalStats);
    }
    
    #endregion
    
    #region Manual Save/Load Methods
    
    [ContextMenu("Manual Save")]
    public void ManualSave()
    {
        if (isNetworkMode)
        {
            SaveNetworkInventoryData();
        }
        else
        {
            SaveLocalInventoryData();
        }
        Debug.Log("Manual save triggered");
    }
    
    [ContextMenu("Manual Load")]
    public void ManualLoad()
    {
        if (isNetworkMode)
        {
            StartCoroutine(LoadNetworkInventoryData());
        }
        else
        {
            StartCoroutine(LoadLocalInventoryData());
        }
        Debug.Log("Manual load triggered");
    }
    
    [ContextMenu("Check Firebase Connection")]
    public void CheckFirebaseConnection()
    {
        Debug.Log($"Firebase Database URL: {databaseReference.Database.App.Options.DatabaseUrl}");
        Debug.Log($"Player ID: {playerId}");
        Debug.Log($"Network Mode: {isNetworkMode}");
    }
    
    #endregion
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (hybridInventoryManager != null)
        {
            hybridInventoryManager.OnInventoryChanged -= OnInventoryChanged;
            hybridInventoryManager.OnItemEquipped -= OnItemEquipped;
            hybridInventoryManager.OnItemUnequipped -= OnItemUnequipped;
        }
    }
}