using UnityEngine;
using Firebase.Database;
using System.Collections;
using System.Collections.Generic;

public class HybridFirebaseSync : MonoBehaviour
{
    private DatabaseReference databaseReference;
    private HybridInventoryManager hybridInventoryManager;
    private string playerId;
    private string currentCharacterType;
    private bool isNetworkMode = false;

    public void Initialize(HybridInventoryManager manager, bool networkMode)
    {
        hybridInventoryManager = manager;
        isNetworkMode = networkMode;
        databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
        playerId = PlayerPrefs.GetString("PlayerId");
        currentCharacterType = GetCurrentCharacterType();

        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogWarning("No PlayerId found! HybridInventoryManager will use PlayerPrefs only.");
        }

        if (string.IsNullOrEmpty(currentCharacterType))
        {
            currentCharacterType = "Assassin"; // Default fallback
            Debug.LogWarning("No character type found, using default: Assassin");
        }

        Debug.Log($"HybridFirebaseSync initialized - Network Mode: {isNetworkMode}, Character: {currentCharacterType}");
    }

    private string GetCurrentCharacterType()
    {
        // ลองหาจาก PersistentPlayerData ก่อน
        if (PersistentPlayerData.Instance != null)
        {
            string activeCharacter = PersistentPlayerData.Instance.GetCurrentActiveCharacter();
            if (!string.IsNullOrEmpty(activeCharacter))
            {
                return activeCharacter;
            }
        }

        // ถ้าไม่มี ให้ใช้จาก PlayerPrefs
        return PlayerPrefs.GetString("LastCharacterSelected", "Assassin");
    }

    #region Local Mode Data Loading/Saving

    public IEnumerator LoadLocalInventoryData()
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogWarning("No PlayerId - skipping Firebase load for local mode");
            yield break;
        }

        Debug.Log($"Loading local inventory data from Firebase for character: {currentCharacterType}...");

        // Load shared inventory (ไม่แยกตัวละคร)
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

        // Load character-specific equipment
        var equipmentPath = $"players/{playerId}/characters/{currentCharacterType}/equipment";
        var equipmentTask = databaseReference.Child("players").Child(playerId)
            .Child("characters").Child(currentCharacterType).Child("equipment").GetValueAsync();
        yield return new WaitUntil(() => equipmentTask.IsCompleted);

        if (equipmentTask.Exception != null)
        {
            Debug.LogError($"Failed to load equipment for {currentCharacterType}: {equipmentTask.Exception}");
        }
        else if (equipmentTask.Result.Exists)
        {
            LoadLocalEquipmentFromSnapshot(equipmentTask.Result);
        }
        else
        {
            Debug.Log($"No equipment data found for {currentCharacterType} - starting fresh");
        }

        Debug.Log($"Local inventory data loaded from Firebase successfully for {currentCharacterType}");
    }

    private void LoadLocalInventoryFromSnapshot(DataSnapshot snapshot)
    {
        int itemsLoaded = 0;
        foreach (var child in snapshot.Children)
        {
            var itemData = child.Value as Dictionary<string, object>;
            if (itemData != null && itemData.ContainsKey("itemId") && itemData.ContainsKey("quantity"))
            {
                string itemId = itemData["itemId"].ToString();
                int quantity = System.Convert.ToInt32(itemData["quantity"]);

                if (!string.IsNullOrEmpty(itemId) && quantity > 0)
                {
                    // Add to local inventory via HybridInventoryManager
                    bool success = hybridInventoryManager.AddItem(itemId, quantity);
                    if (success)
                    {
                        itemsLoaded++;
                        Debug.Log($"Loaded inventory item: {itemId} x{quantity}");
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to load inventory item: {itemId} x{quantity}");
                    }
                }
            }
        }
        Debug.Log($"Successfully loaded {itemsLoaded} inventory items");
    }

    private void LoadLocalEquipmentFromSnapshot(DataSnapshot snapshot)
    {
        int equipmentLoaded = 0;
        foreach (var child in snapshot.Children)
        {
            var equipData = child.Value as Dictionary<string, object>;
            if (equipData != null && equipData.ContainsKey("itemId") && equipData.ContainsKey("equipmentType"))
            {
                string itemId = equipData["itemId"].ToString();
                int equipTypeInt = System.Convert.ToInt32(equipData["equipmentType"]);
                EquipmentType equipType = (EquipmentType)equipTypeInt;

                if (!string.IsNullOrEmpty(itemId))
                {
                    // เพิ่ม item ลง inventory ก่อน (ถ้ายังไม่มี)
                    if (!hybridInventoryManager.HasItem(itemId))
                    {
                        hybridInventoryManager.AddItem(itemId, 1);
                    }

                    // แล้วค่อย equip
                    bool success = hybridInventoryManager.EquipItem(itemId);
                    if (success)
                    {
                        equipmentLoaded++;
                        Debug.Log($"Loaded equipment: {itemId} on {equipType}");
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to equip item: {itemId} on {equipType}");
                    }
                }
            }
        }
        Debug.Log($"Successfully loaded {equipmentLoaded} equipment items for {currentCharacterType}");
    }

    public void SaveLocalInventoryData()
    {
        if (string.IsNullOrEmpty(playerId) || isNetworkMode) return;

        // อัพเดท current character type
        currentCharacterType = GetCurrentCharacterType();

        StartCoroutine(SaveLocalInventoryCoroutine());
    }

    private IEnumerator SaveLocalInventoryCoroutine()
    {
        Debug.Log($"Saving local inventory data for character: {currentCharacterType}");

        // Save shared inventory (ไม่แยกตัวละคร)
        var inventoryData = new Dictionary<string, object>();
        var inventoryItems = hybridInventoryManager.GetInventoryItems();

        int itemCount = 0;
        foreach (var kvp in inventoryItems)
        {
            var itemData = new Dictionary<string, object>
            {
                ["itemId"] = kvp.Value.itemId,
                ["quantity"] = kvp.Value.quantity,
                ["slotIndex"] = kvp.Value.slotIndex
            };
            inventoryData[$"slot_{kvp.Key}"] = itemData;
            itemCount++;
        }

        var inventoryTask = databaseReference.Child("players").Child(playerId).Child("inventory")
            .SetValueAsync(inventoryData);
        yield return new WaitUntil(() => inventoryTask.IsCompleted);

        if (inventoryTask.Exception != null)
        {
            Debug.LogError($"Failed to save local inventory: {inventoryTask.Exception}");
        }
        else
        {
            Debug.Log($"Successfully saved {itemCount} inventory items");
        }

        // Save character-specific equipment
        var equipmentData = new Dictionary<string, object>();
        int equipmentCount = 0;

        for (int i = 0; i < 5; i++)
        {
            EquipmentType equipType = (EquipmentType)i;
            var equippedItem = hybridInventoryManager.GetEquippedItem(equipType);

            if (equippedItem != null && !string.IsNullOrEmpty(equippedItem.itemId))
            {
                var equipData = new Dictionary<string, object>
                {
                    ["itemId"] = equippedItem.itemId,
                    ["equipmentType"] = (int)equippedItem.equipmentType,
                    ["characterType"] = currentCharacterType,
                    ["lastUpdated"] = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                equipmentData[$"slot_{i}"] = equipData;
                equipmentCount++;
            }
        }

        // บันทึก equipment เฉพาะตัวละคร
        var equipmentPath = $"players/{playerId}/characters/{currentCharacterType}/equipment";
        var equipmentTask = databaseReference.Child("players").Child(playerId)
            .Child("characters").Child(currentCharacterType).Child("equipment")
            .SetValueAsync(equipmentData);
        yield return new WaitUntil(() => equipmentTask.IsCompleted);

        if (equipmentTask.Exception != null)
        {
            Debug.LogError($"Failed to save local equipment for {currentCharacterType}: {equipmentTask.Exception}");
        }
        else
        {
            Debug.Log($"Successfully saved {equipmentCount} equipment items for {currentCharacterType}");
        }

        // บันทึก metadata ของตัวละคร
        yield return SaveCharacterMetadata();

        Debug.Log($"Local inventory save completed for {currentCharacterType}");
    }

    private IEnumerator SaveCharacterMetadata()
    {
        var metadata = new Dictionary<string, object>
        {
            ["characterType"] = currentCharacterType,
            ["lastPlayed"] = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            ["mode"] = "local"
        };

        var metadataTask = databaseReference.Child("players").Child(playerId)
            .Child("characters").Child(currentCharacterType).Child("metadata")
            .SetValueAsync(metadata);
        yield return new WaitUntil(() => metadataTask.IsCompleted);

        if (metadataTask.Exception != null)
        {
            Debug.LogError($"Failed to save character metadata: {metadataTask.Exception}");
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

        Debug.Log($"Loading network inventory data from Firebase for character: {currentCharacterType}...");

        // Load shared inventory
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

        // Load character-specific equipment
        var equipmentTask = databaseReference.Child("players").Child(playerId)
            .Child("characters").Child(currentCharacterType).Child("equipment").GetValueAsync();
        yield return new WaitUntil(() => equipmentTask.IsCompleted);

        if (equipmentTask.Exception != null)
        {
            Debug.LogError($"Failed to load network equipment for {currentCharacterType}: {equipmentTask.Exception}");
        }
        else if (equipmentTask.Result.Exists)
        {
            LoadNetworkEquipmentFromSnapshot(equipmentTask.Result);
        }

        Debug.Log($"Network inventory data loaded from Firebase successfully for {currentCharacterType}");
    }

    private void LoadNetworkInventoryFromSnapshot(DataSnapshot snapshot)
    {
        foreach (var child in snapshot.Children)
        {
            var itemData = child.Value as Dictionary<string, object>;
            if (itemData != null && itemData.ContainsKey("itemId") && itemData.ContainsKey("quantity"))
            {
                string itemId = itemData["itemId"].ToString();
                int quantity = System.Convert.ToInt32(itemData["quantity"]);

                if (!string.IsNullOrEmpty(itemId) && quantity > 0)
                {
                    hybridInventoryManager.AddItem(itemId, quantity);
                }
            }
        }
    }

    private void LoadNetworkEquipmentFromSnapshot(DataSnapshot snapshot)
    {
        foreach (var child in snapshot.Children)
        {
            var equipData = child.Value as Dictionary<string, object>;
            if (equipData != null && equipData.ContainsKey("itemId") && equipData.ContainsKey("equipmentType"))
            {
                string itemId = equipData["itemId"].ToString();

                if (!string.IsNullOrEmpty(itemId))
                {
                    // เพิ่ม item ลง inventory ก่อน (ถ้ายังไม่มี)
                    if (!hybridInventoryManager.HasItem(itemId))
                    {
                        hybridInventoryManager.AddItem(itemId, 1);
                    }

                    // แล้วค่อย equip
                    hybridInventoryManager.EquipItem(itemId);
                }
            }
        }
    }

    public void SaveNetworkInventoryData()
    {
        if (string.IsNullOrEmpty(playerId) || !isNetworkMode) return;

        // อัพเดท current character type
        currentCharacterType = GetCurrentCharacterType();

        StartCoroutine(SaveNetworkInventoryCoroutine());
    }

    private IEnumerator SaveNetworkInventoryCoroutine()
    {
        Debug.Log($"Saving network inventory data for character: {currentCharacterType}");

        // Save shared inventory
        var inventoryData = new Dictionary<string, object>();
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

        // Save character-specific equipment
        var equipmentData = new Dictionary<string, object>();

        for (int i = 0; i < 5; i++)
        {
            EquipmentType equipType = (EquipmentType)i;
            var equippedItem = hybridInventoryManager.GetEquippedItem(equipType);

            if (equippedItem != null && !string.IsNullOrEmpty(equippedItem.itemId))
            {
                var equipData = new Dictionary<string, object>
                {
                    ["itemId"] = equippedItem.itemId,
                    ["equipmentType"] = (int)equippedItem.equipmentType,
                    ["characterType"] = currentCharacterType,
                    ["lastUpdated"] = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                equipmentData[$"slot_{i}"] = equipData;
            }
        }

        var equipmentTask = databaseReference.Child("players").Child(playerId)
            .Child("characters").Child(currentCharacterType).Child("equipment")
            .SetValueAsync(equipmentData);
        yield return new WaitUntil(() => equipmentTask.IsCompleted);

        if (equipmentTask.Exception != null)
        {
            Debug.LogError($"Failed to save network equipment for {currentCharacterType}: {equipmentTask.Exception}");
        }
        else
        {
            Debug.Log($"Network inventory saved to Firebase successfully for {currentCharacterType}");
        }

        // บันทึก metadata
        yield return SaveCharacterMetadata();
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
            ["characterType"] = currentCharacterType,
            ["mode"] = isNetworkMode ? "network" : "local"
        };

        // บันทึก stats แยกตามตัวละคร
        databaseReference.Child("players").Child(playerId)
            .Child("characters").Child(currentCharacterType).Child("equipmentStats")
            .SetValueAsync(statsData);
    }

    #endregion

    #region Character Switching

    public void OnCharacterChanged(string newCharacterType)
    {
        if (string.IsNullOrEmpty(newCharacterType)) return;

        Debug.Log($"Character changed from {currentCharacterType} to {newCharacterType}");

        // บันทึกข้อมูลตัวละครเก่าก่อน
        if (!string.IsNullOrEmpty(currentCharacterType))
        {
            if (isNetworkMode)
            {
                SaveNetworkInventoryData();
            }
            else
            {
                SaveLocalInventoryData();
            }
        }

        // เปลี่ยนตัวละคร
        currentCharacterType = newCharacterType;

        // โหลดข้อมูลตัวละครใหม่
        StartCoroutine(LoadCharacterData());
    }

    private IEnumerator LoadCharacterData()
    {
        Debug.Log($"Loading data for new character: {currentCharacterType}");

        // Clear current equipment (แต่ไม่ clear inventory)
        ClearCurrentEquipment();

        // รอหน่อยให้ UI อัพเดท
        yield return new WaitForSeconds(0.1f);

        // โหลดข้อมูลใหม่
        if (isNetworkMode)
        {
            yield return LoadNetworkInventoryData();
        }
        else
        {
            yield return LoadLocalInventoryData();
        }

        Debug.Log($"Character data loaded successfully for {currentCharacterType}");
    }

    private void ClearCurrentEquipment()
    {
        // Unequip ทุกชิ้นก่อน
        for (int i = 0; i < 5; i++)
        {
            EquipmentType equipType = (EquipmentType)i;
            var equippedItem = hybridInventoryManager.GetEquippedItem(equipType);
            if (equippedItem != null)
            {
                hybridInventoryManager.UnequipItem(equipType);
            }
        }
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
        Debug.Log($"Item equipped: {item.itemId} on {item.equipmentType} for {currentCharacterType}");

        // Save immediately when equipment changes
        OnInventoryChanged();

        // Also save stats
        var totalStats = hybridInventoryManager.GetTotalEquipmentStats();
        SavePlayerStats(totalStats);
    }

    private void OnItemUnequipped(EquipmentType equipmentType)
    {
        Debug.Log($"Item unequipped from {equipmentType} for {currentCharacterType}");

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
        currentCharacterType = GetCurrentCharacterType();

        if (isNetworkMode)
        {
            SaveNetworkInventoryData();
        }
        else
        {
            SaveLocalInventoryData();
        }
        Debug.Log($"Manual save triggered for {currentCharacterType}");
    }

    [ContextMenu("Manual Load")]
    public void ManualLoad()
    {
        currentCharacterType = GetCurrentCharacterType();

        if (isNetworkMode)
        {
            StartCoroutine(LoadNetworkInventoryData());
        }
        else
        {
            StartCoroutine(LoadLocalInventoryData());
        }
        Debug.Log($"Manual load triggered for {currentCharacterType}");
    }

    [ContextMenu("Check Firebase Connection")]
    public void CheckFirebaseConnection()
    {
        Debug.Log($"Firebase Database URL: {databaseReference.Database.App.Options.DatabaseUrl}");
        Debug.Log($"Player ID: {playerId}");
        Debug.Log($"Current Character: {currentCharacterType}");
        Debug.Log($"Network Mode: {isNetworkMode}");
    }

    [ContextMenu("Debug Character Equipment")]
    public void DebugCharacterEquipment()
    {
        Debug.Log($"=== Current Equipment for {currentCharacterType} ===");
        for (int i = 0; i < 5; i++)
        {
            EquipmentType equipType = (EquipmentType)i;
            var equippedItem = hybridInventoryManager.GetEquippedItem(equipType);
            if (equippedItem != null)
            {
                Debug.Log($"{equipType}: {equippedItem.itemId}");
            }
            else
            {
                Debug.Log($"{equipType}: (empty)");
            }
        }
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