using UnityEngine;
using System.Collections.Generic;
using Fusion;

public class HybridInventoryManager : NetworkBehaviour
{
    [Header("Hybrid Settings")]
    public bool useNetworkMode = true;
    public bool autoDetectNetworkMode = true;
    public int maxInventorySlots = 30;
    public ItemDatabase itemDatabase;

    [Header("Data Persistence")]
    public bool useFirebaseSync = true;
    public bool usePlayerPrefsBackup = true;

    [Header("Equipment Integration")]
    public bool useEquipmentManagerForUse = true; // เปิด/ปิดการใช้ EquipmentManager
    public bool consumeEquipmentOnUse = false; // ใช้แล้วหายหรือไม่

    // Network Variables
    [Networked, Capacity(30)]
    public NetworkArray<NetworkInventoryItem> NetworkInventory => default;

    [Networked, Capacity(5)]
    public NetworkArray<NetworkEquippedItem> NetworkEquipment => default;

    // Local Variables
    private Dictionary<int, InventoryItem> localInventory = new Dictionary<int, InventoryItem>();
    private Dictionary<EquipmentType, EquippedItem> localEquipment = new Dictionary<EquipmentType, EquippedItem>();

    // Components
    private EquipmentManager equipmentManager;

    // Runtime state
    private bool isNetworkActive = false;
    private bool isDataLoaded = false;

    // Events
    public System.Action<InventoryItem> OnItemAdded;
    public System.Action<InventoryItem> OnItemRemoved;
    public System.Action<InventoryItem> OnItemUsed; // เพิ่ม event สำหรับ use
    public System.Action<EquippedItem> OnItemEquipped;
    public System.Action<EquipmentType> OnItemUnequipped;
    public System.Action OnInventoryChanged;

    private HybridFirebaseSync firebaseSync;

    #region Unity Lifecycle

    void Start()
    {
        // Find EquipmentManager
        FindEquipmentManager();

        if (!IsNetworkContextAvailable())
        {
            InitializeLocalMode();
        }
    }

    public override void Spawned()
    {
        InitializeNetworkMode();
    }

    private void FindEquipmentManager()
    {
        equipmentManager = GetComponent<EquipmentManager>();
        if (equipmentManager == null)
        {
            equipmentManager = FindObjectOfType<EquipmentManager>();
        }

        if (equipmentManager == null)
        {
            Debug.LogWarning("EquipmentManager not found! Equipment use will fallback to normal equip.");
            useEquipmentManagerForUse = false;
        }
        else
        {
            Debug.Log("✅ EquipmentManager found and connected for enhanced use system");
        }
    }

    #endregion

    #region Initialization (Same as before)

    bool IsNetworkContextAvailable()
    {
        if (!useNetworkMode) return false;
        if (!autoDetectNetworkMode) return useNetworkMode;

        var runner = FindObjectOfType<NetworkRunner>();
        bool hasNetwork = runner != null && runner.IsRunning;
        bool hasNetworkObject = Object != null;

        Debug.Log($"Network Detection - Runner: {hasNetwork}, NetworkObject: {hasNetworkObject}");

        return hasNetwork && hasNetworkObject;
    }

    void InitializeLocalMode()
    {
        isNetworkActive = false;
        Debug.Log("🏠 InventoryManager: Local Mode");

        SetupFirebaseSync();
        LoadLocalData();
    }

    void InitializeNetworkMode()
    {
        isNetworkActive = true;
        Debug.Log("🌐 InventoryManager: Network Mode");

        InitializeNetworkArrays();

        if (HasInputAuthority)
        {
            SetupFirebaseSync();
            StartCoroutine(LoadNetworkData());
        }
    }

    void InitializeNetworkArrays()
    {
        if (!HasStateAuthority) return;

        for (int i = 0; i < 5; i++)
        {
            if (string.IsNullOrEmpty(NetworkEquipment[i].itemId.ToString()))
            {
                NetworkEquipment.Set(i, new NetworkEquippedItem
                {
                    itemId = "",
                    equipmentType = (EquipmentType)i
                });
            }
        }
    }

    void SetupFirebaseSync()
    {
        if (!useFirebaseSync) return;

        firebaseSync = GetComponent<HybridFirebaseSync>();
        if (firebaseSync == null)
        {
            firebaseSync = gameObject.AddComponent<HybridFirebaseSync>();
        }

        firebaseSync.Initialize(this, isNetworkActive);
    }

    #endregion

    #region Data Loading (Same as before)

    void LoadLocalData()
    {
        if (useFirebaseSync && firebaseSync != null)
        {
            StartCoroutine(LoadLocalDataCoroutine());
        }
        else if (usePlayerPrefsBackup)
        {
            LoadFromPlayerPrefs();
        }

        isDataLoaded = true;
    }

    System.Collections.IEnumerator LoadLocalDataCoroutine()
    {
        yield return firebaseSync.LoadLocalInventoryData();
        isDataLoaded = true;
        OnInventoryChanged?.Invoke();
    }

    System.Collections.IEnumerator LoadNetworkData()
    {
        yield return firebaseSync.LoadNetworkInventoryData();
        isDataLoaded = true;
        OnInventoryChanged?.Invoke();
    }

    void LoadFromPlayerPrefs()
    {
        string inventoryJson = PlayerPrefs.GetString("HybridInventory", "");
        if (!string.IsNullOrEmpty(inventoryJson))
        {
            try
            {
                var data = JsonUtility.FromJson<SerializableInventoryData>(inventoryJson);
                localInventory = data.GetInventoryDictionary();
                localEquipment = data.GetEquipmentDictionary();
                Debug.Log("Loaded inventory from PlayerPrefs");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load from PlayerPrefs: {e.Message}");
            }
        }
    }

    #endregion

    #region Enhanced Use Item System

    public bool UseItem(string itemId)
    {
        ItemData itemData = itemDatabase.GetItem(itemId);
        if (itemData == null)
        {
            Debug.LogError($"Item not found: {itemId}");
            return false;
        }

        if (!HasItem(itemId))
        {
            Debug.LogError($"Item not in inventory: {itemId}");
            return false;
        }

        bool success = false;

        switch (itemData.itemType)
        {
            case ItemType.Consumable:
                success = UseConsumableItem(itemData);
                break;

            case ItemType.Equipment:
                success = UseEquipmentItem(itemData);
                break;

            default:
                Debug.LogWarning($"Item type {itemData.itemType} cannot be used: {itemId}");
                return false;
        }

        if (success)
        {
            Debug.Log($"✅ Used item: {itemData.itemName}");

            // Create inventory item for event
            var inventoryItem = new InventoryItem(itemId, 1, -1);
            OnItemUsed?.Invoke(inventoryItem);
            OnInventoryChanged?.Invoke();
        }

        return success;
    }

    private bool UseConsumableItem(ItemData itemData)
    {
        // Apply consumable effects
        ApplyConsumableEffects(itemData);

        // Remove item from inventory
        return RemoveItem(itemData.itemId, 1);
    }

    private bool UseEquipmentItem(ItemData itemData)
    {
        if (useEquipmentManagerForUse && equipmentManager != null)
        {
            return UseEquipmentWithManager(itemData);
        }
        else
        {
            // Fallback to normal equip
            return EquipItem(itemData.itemId);
        }
    }

    private bool UseEquipmentWithManager(ItemData itemData)
    {
        Debug.Log($"🔧 Using equipment with EquipmentManager: {itemData.itemName}");

        // Create EquipmentData for EquipmentManager
        var equipmentData = new EquipmentData
        {
            itemName = itemData.itemName,
            stats = ConvertItemStatsToEquipmentStats(itemData.stats),
            itemIcon = itemData.icon
        };

        // Apply stats through EquipmentManager
        bool success = ApplyEquipmentStatsToManager(equipmentData);

        if (success)
        {
            // Remove item from inventory if set to consume
            if (consumeEquipmentOnUse)
            {
                RemoveItem(itemData.itemId, 1);
                Debug.Log($"🗑️ Equipment consumed: {itemData.itemName}");
            }
            else
            {
                Debug.Log($"🔄 Equipment used (not consumed): {itemData.itemName}");
            }

            // Show effects
            ShowEquipmentUseEffects(itemData);
        }

        return success;
    }

    private bool ApplyEquipmentStatsToManager(EquipmentData equipmentData)
    {
        try
        {
            // Apply as equipment bonus (not replacement)
            equipmentManager.ApplyRuneBonus(equipmentData.stats);

            Debug.Log($"✨ Applied equipment stats via EquipmentManager:");
            Debug.Log($"   🗡️ ATK: +{equipmentData.stats.attackDamageBonus}");
            Debug.Log($"   🛡️ ARM: +{equipmentData.stats.armorBonus}");
            Debug.Log($"   ❤️ HP: +{equipmentData.stats.maxHpBonus}");
            Debug.Log($"   💙 MP: +{equipmentData.stats.maxManaBonus}");
            Debug.Log($"   ⚡ Crit: +{equipmentData.stats.criticalChanceBonus:F1}%");

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to apply equipment stats: {e.Message}");
            return false;
        }
    }

    private EquipmentStats ConvertItemStatsToEquipmentStats(ItemStats itemStats)
    {
        return new EquipmentStats
        {
            attackDamageBonus = itemStats.attackDamage,
            armorBonus = itemStats.armor,
            maxHpBonus = itemStats.maxHp,
            maxManaBonus = itemStats.maxMana,
            moveSpeedBonus = itemStats.moveSpeed,
            attackSpeedBonus = itemStats.attackSpeed,
            criticalChanceBonus = itemStats.criticalChance,
            criticalMultiplierBonus = itemStats.criticalDamage,
            hitRateBonus = 0f, // ItemStats doesn't have these
            evasionRateBonus = 0f,
            physicalResistanceBonus = 0f,
            magicalResistanceBonus = 0f
        };
    }

    private void ApplyConsumableEffects(ItemData itemData)
    {
        Debug.Log($"💊 Using consumable: {itemData.itemName}");

        // Find Character component to apply effects
        var character = GetComponent<Character>();
        if (character == null)
        {
            character = FindObjectOfType<Character>();
        }

        if (character != null)
        {
            // Apply healing
            if (itemData.healAmount > 0)
            {
                int newHp = Mathf.Min(character.CurrentHp + itemData.healAmount, character.MaxHp);
                int actualHeal = newHp - character.CurrentHp;
                character.CurrentHp = newHp;

                Debug.Log($"❤️ Healed: +{actualHeal} HP (Current: {character.CurrentHp}/{character.MaxHp})");
                ShowHealingEffect(actualHeal);
            }

            // Apply mana restoration
            if (itemData.manaAmount > 0)
            {
                int newMana = Mathf.Min(character.CurrentMana + itemData.manaAmount, character.MaxMana);
                int actualMana = newMana - character.CurrentMana;
                character.CurrentMana = newMana;

                Debug.Log($"💙 Mana restored: +{actualMana} MP (Current: {character.CurrentMana}/{character.MaxMana})");
                ShowManaEffect(actualMana);
            }
        }
        else
        {
            Debug.LogWarning("Character component not found - cannot apply consumable effects");
        }
    }

    private void ShowEquipmentUseEffects(ItemData itemData)
    {
        // Visual effects for equipment use
        Debug.Log($"✨ Equipment use effects for {itemData.itemName}");

        // You can add particle effects, sound effects, etc. here
        // Example:
        // PlayEquipmentUseSound();
        // ShowStatsBoostParticles();
    }

    private void ShowHealingEffect(int healAmount)
    {
        // Visual effects for healing
        Debug.Log($"💚 Healing effect: +{healAmount}");

        // You can add healing particles, floating text, etc.
        // Example:
        // ShowFloatingText($"+{healAmount} HP", Color.green);
        // PlayHealingSound();
    }

    private void ShowManaEffect(int manaAmount)
    {
        // Visual effects for mana restoration
        Debug.Log($"💙 Mana effect: +{manaAmount}");

        // You can add mana particles, floating text, etc.
        // Example:
        // ShowFloatingText($"+{manaAmount} MP", Color.blue);
        // PlayManaSound();
    }

    #endregion

    #region Add/Remove Items (Same as before but with some enhancements)

    public bool AddItem(string itemId, int quantity = 1)
    {
        if (isNetworkActive)
        {
            return AddItemNetwork(itemId, quantity);
        }
        else
        {
            return AddItemLocal(itemId, quantity);
        }
    }

    bool AddItemNetwork(string itemId, int quantity)
    {
        if (!HasInputAuthority) return false;

        ItemData itemData = itemDatabase.GetItem(itemId);
        if (itemData == null) return false;

        if (itemData.isStackable)
        {
            for (int i = 0; i < maxInventorySlots; i++)
            {
                var slot = NetworkInventory[i];
                if (!string.IsNullOrEmpty(slot.itemId.ToString()) && slot.itemId.ToString() == itemId)
                {
                    int newQuantity = slot.quantity + quantity;
                    if (newQuantity <= itemData.maxStackSize)
                    {
                        RPC_UpdateInventorySlot(i, itemId, newQuantity);
                        return true;
                    }
                    else
                    {
                        int remaining = newQuantity - itemData.maxStackSize;
                        RPC_UpdateInventorySlot(i, itemId, itemData.maxStackSize);
                        return AddItem(itemId, remaining);
                    }
                }
            }
        }

        for (int i = 0; i < maxInventorySlots; i++)
        {
            if (string.IsNullOrEmpty(NetworkInventory[i].itemId.ToString()))
            {
                RPC_UpdateInventorySlot(i, itemId, quantity);
                return true;
            }
        }

        return false;
    }

    bool AddItemLocal(string itemId, int quantity)
    {
        ItemData itemData = itemDatabase.GetItem(itemId);
        if (itemData == null) return false;

        if (itemData.isStackable)
        {
            foreach (var kvp in localInventory)
            {
                if (kvp.Value.itemId == itemId)
                {
                    int newQuantity = kvp.Value.quantity + quantity;
                    if (newQuantity <= itemData.maxStackSize)
                    {
                        kvp.Value.quantity = newQuantity;
                        SaveLocalData();
                        OnItemAdded?.Invoke(kvp.Value);
                        OnInventoryChanged?.Invoke();
                        return true;
                    }
                    else
                    {
                        int remaining = newQuantity - itemData.maxStackSize;
                        kvp.Value.quantity = itemData.maxStackSize;
                        SaveLocalData();
                        return AddItem(itemId, remaining);
                    }
                }
            }
        }

        for (int i = 0; i < maxInventorySlots; i++)
        {
            if (!localInventory.ContainsKey(i))
            {
                var newItem = new InventoryItem(itemId, quantity, i);
                localInventory[i] = newItem;
                SaveLocalData();
                OnItemAdded?.Invoke(newItem);
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        return false;
    }

    public bool RemoveItem(string itemId, int quantity = 1)
    {
        if (isNetworkActive)
        {
            return RemoveItemNetwork(itemId, quantity);
        }
        else
        {
            return RemoveItemLocal(itemId, quantity);
        }
    }

    bool RemoveItemNetwork(string itemId, int quantity)
    {
        if (!HasInputAuthority) return false;

        int remainingToRemove = quantity;

        for (int i = 0; i < maxInventorySlots; i++)
        {
            var slot = NetworkInventory[i];
            if (!string.IsNullOrEmpty(slot.itemId.ToString()) && slot.itemId.ToString() == itemId)
            {
                if (slot.quantity >= remainingToRemove)
                {
                    int newQuantity = slot.quantity - remainingToRemove;
                    if (newQuantity <= 0)
                    {
                        RPC_ClearInventorySlot(i);
                    }
                    else
                    {
                        RPC_UpdateInventorySlot(i, itemId, newQuantity);
                    }
                    return true;
                }
                else
                {
                    remainingToRemove -= slot.quantity;
                    RPC_ClearInventorySlot(i);
                }
            }
        }

        return remainingToRemove <= 0;
    }

    bool RemoveItemLocal(string itemId, int quantity)
    {
        int remainingToRemove = quantity;
        var itemsToRemove = new List<int>();

        foreach (var kvp in localInventory)
        {
            if (kvp.Value.itemId == itemId)
            {
                if (kvp.Value.quantity >= remainingToRemove)
                {
                    kvp.Value.quantity -= remainingToRemove;
                    if (kvp.Value.quantity <= 0)
                    {
                        itemsToRemove.Add(kvp.Key);
                    }
                    remainingToRemove = 0;
                    break;
                }
                else
                {
                    remainingToRemove -= kvp.Value.quantity;
                    itemsToRemove.Add(kvp.Key);
                }
            }
        }

        foreach (int key in itemsToRemove)
        {
            localInventory.Remove(key);
        }

        if (remainingToRemove < quantity)
        {
            SaveLocalData();
            OnInventoryChanged?.Invoke();
        }

        return remainingToRemove <= 0;
    }

    #endregion

    #region Equipment System (Same as before)

    public bool EquipItem(string itemId)
    {
        if (isNetworkActive)
        {
            return EquipItemNetwork(itemId);
        }
        else
        {
            return EquipItemLocal(itemId);
        }
    }

    bool EquipItemNetwork(string itemId)
    {
        if (!HasInputAuthority) return false;

        ItemData itemData = itemDatabase.GetItem(itemId);
        if (itemData == null || itemData.itemType != ItemType.Equipment) return false;

        if (!HasItem(itemId)) return false;

        int equipSlot = (int)itemData.equipmentType;
        var currentEquipped = NetworkEquipment[equipSlot];

        if (!string.IsNullOrEmpty(currentEquipped.itemId.ToString()))
        {
            AddItem(currentEquipped.itemId.ToString(), 1);
        }

        RemoveItem(itemId, 1);
        RPC_EquipItem(equipSlot, itemId);

        return true;
    }

    bool EquipItemLocal(string itemId)
    {
        ItemData itemData = itemDatabase.GetItem(itemId);
        if (itemData == null || itemData.itemType != ItemType.Equipment) return false;

        if (!HasItem(itemId)) return false;

        EquipmentType equipSlot = itemData.equipmentType;

        if (localEquipment.ContainsKey(equipSlot))
        {
            AddItem(localEquipment[equipSlot].itemId, 1);
        }

        RemoveItem(itemId, 1);

        localEquipment[equipSlot] = new EquippedItem(itemId, equipSlot);
        SaveLocalData();
        OnItemEquipped?.Invoke(localEquipment[equipSlot]);
        OnInventoryChanged?.Invoke();

        return true;
    }

    public bool UnequipItem(EquipmentType equipmentType)
    {
        if (isNetworkActive)
        {
            return UnequipItemNetwork(equipmentType);
        }
        else
        {
            return UnequipItemLocal(equipmentType);
        }
    }

    bool UnequipItemNetwork(EquipmentType equipmentType)
    {
        if (!HasInputAuthority) return false;

        int equipSlot = (int)equipmentType;
        var equipped = NetworkEquipment[equipSlot];

        if (string.IsNullOrEmpty(equipped.itemId.ToString())) return false;

        if (!AddItem(equipped.itemId.ToString(), 1)) return false;

        RPC_UnequipItem(equipSlot);
        return true;
    }

    bool UnequipItemLocal(EquipmentType equipmentType)
    {
        if (!localEquipment.ContainsKey(equipmentType)) return false;

        var equippedItem = localEquipment[equipmentType];

        if (!AddItem(equippedItem.itemId, 1)) return false;

        localEquipment.Remove(equipmentType);
        SaveLocalData();
        OnItemUnequipped?.Invoke(equipmentType);
        OnInventoryChanged?.Invoke();

        return true;
    }

    #endregion

    #region Network RPCs (Same as before)

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_UpdateInventorySlot(int slotIndex, string itemId, int quantity)
    {
        NetworkInventory.Set(slotIndex, new NetworkInventoryItem(itemId, quantity));
        OnInventoryChanged?.Invoke();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_ClearInventorySlot(int slotIndex)
    {
        NetworkInventory.Set(slotIndex, new NetworkInventoryItem("", 0));
        OnInventoryChanged?.Invoke();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_EquipItem(int equipSlot, string itemId)
    {
        ItemData itemData = itemDatabase.GetItem(itemId);
        NetworkEquipment.Set(equipSlot, new NetworkEquippedItem(itemId, itemData.equipmentType));
        OnItemEquipped?.Invoke(new EquippedItem(itemId, itemData.equipmentType));
        OnInventoryChanged?.Invoke();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_UnequipItem(int equipSlot)
    {
        EquipmentType equipType = (EquipmentType)equipSlot;
        NetworkEquipment.Set(equipSlot, new NetworkEquippedItem("", equipType));
        OnItemUnequipped?.Invoke(equipType);
        OnInventoryChanged?.Invoke();
    }

    #endregion

    #region Utility Methods (Same as before)

    public bool HasItem(string itemId, int requiredQuantity = 1)
    {
        int totalQuantity = GetItemQuantity(itemId);
        return totalQuantity >= requiredQuantity;
    }

    public int GetItemQuantity(string itemId)
    {
        if (isNetworkActive)
        {
            int totalQuantity = 0;
            for (int i = 0; i < maxInventorySlots; i++)
            {
                var slot = NetworkInventory[i];
                if (!string.IsNullOrEmpty(slot.itemId.ToString()) && slot.itemId.ToString() == itemId)
                {
                    totalQuantity += slot.quantity;
                }
            }
            return totalQuantity;
        }
        else
        {
            int totalQuantity = 0;
            foreach (var item in localInventory.Values)
            {
                if (item.itemId == itemId)
                {
                    totalQuantity += item.quantity;
                }
            }
            return totalQuantity;
        }
    }

    public EquippedItem GetEquippedItem(EquipmentType equipmentType)
    {
        if (isNetworkActive)
        {
            int slot = (int)equipmentType;
            var equipped = NetworkEquipment[slot];

            if (string.IsNullOrEmpty(equipped.itemId.ToString())) return null;

            return new EquippedItem(equipped.itemId.ToString(), equipped.equipmentType);
        }
        else
        {
            return localEquipment.ContainsKey(equipmentType) ? localEquipment[equipmentType] : null;
        }
    }

    public ItemStats GetTotalEquipmentStats()
    {
        ItemStats totalStats = new ItemStats();

        if (isNetworkActive)
        {
            for (int i = 0; i < 5; i++)
            {
                var equipped = NetworkEquipment[i];
                if (!string.IsNullOrEmpty(equipped.itemId.ToString()))
                {
                    ItemData itemData = itemDatabase.GetItem(equipped.itemId.ToString());
                    if (itemData != null)
                    {
                        AddStatsToTotal(ref totalStats, itemData.stats);
                    }
                }
            }
        }
        else
        {
            foreach (var equippedItem in localEquipment.Values)
            {
                ItemData itemData = itemDatabase.GetItem(equippedItem.itemId);
                if (itemData != null)
                {
                    AddStatsToTotal(ref totalStats, itemData.stats);
                }
            }
        }

        return totalStats;
    }

    void AddStatsToTotal(ref ItemStats totalStats, ItemStats itemStats)
    {
        totalStats.attackDamage += itemStats.attackDamage;
        totalStats.armor += itemStats.armor;
        totalStats.maxHp += itemStats.maxHp;
        totalStats.maxMana += itemStats.maxMana;
        totalStats.moveSpeed += itemStats.moveSpeed;
        totalStats.attackSpeed += itemStats.attackSpeed;
        totalStats.criticalChance += itemStats.criticalChance;
        totalStats.criticalDamage += itemStats.criticalDamage;
    }

    public Dictionary<int, InventoryItem> GetInventoryItems()
    {
        if (isNetworkActive)
        {
            var items = new Dictionary<int, InventoryItem>();
            for (int i = 0; i < maxInventorySlots; i++)
            {
                var slot = NetworkInventory[i];
                if (!string.IsNullOrEmpty(slot.itemId.ToString()))
                {
                    items[i] = new InventoryItem(slot.itemId.ToString(), slot.quantity, i);
                }
            }
            return items;
        }
        else
        {
            return new Dictionary<int, InventoryItem>(localInventory);
        }
    }

    #endregion

    #region Data Persistence (Same as before)

    void SaveLocalData()
    {
        if (isNetworkActive) return;

        if (useFirebaseSync && firebaseSync != null)
        {
            firebaseSync.SaveLocalInventoryData();
        }

        if (usePlayerPrefsBackup)
        {
            SaveToPlayerPrefs();
        }
    }

    void SaveToPlayerPrefs()
    {
        var data = new SerializableInventoryData(localInventory, localEquipment);
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("HybridInventory", json);
        PlayerPrefs.Save();
    }

    #endregion

    #region Public Interface

    public bool IsNetworkMode()
    {
        return isNetworkActive;
    }

    public bool IsLocalMode()
    {
        return !isNetworkActive;
    }

    public bool IsDataLoaded()
    {
        return isDataLoaded;
    }

    #endregion

    #region Context Menu (Enhanced Debug Tools)

    [ContextMenu("Add Test Items")]
    void AddTestItems()
    {
        AddItem("health_potion_small", 10);
        AddItem("iron_sword", 1);
        AddItem("leather_armor", 1);
        AddItem("steel_helmet", 1);
        AddItem("mana_potion_small", 5);
    }

    [ContextMenu("Test Use Consumable")]
    void TestUseConsumable()
    {
        UseItem("health_potion_small");
    }

    [ContextMenu("Test Use Equipment")]
    void TestUseEquipment()
    {
        UseItem("iron_sword");
    }

    [ContextMenu("Toggle Equipment Manager Use")]
    void ToggleEquipmentManagerUse()
    {
        useEquipmentManagerForUse = !useEquipmentManagerForUse;
        Debug.Log($"Equipment Manager Use: {useEquipmentManagerForUse}");
    }

    [ContextMenu("Toggle Equipment Consumption")]
    void ToggleEquipmentConsumption()
    {
        consumeEquipmentOnUse = !consumeEquipmentOnUse;
        Debug.Log($"Consume Equipment on Use: {consumeEquipmentOnUse}");
    }

    [ContextMenu("Debug Equipment Manager Stats")]
    void DebugEquipmentManagerStats()
    {
        if (equipmentManager != null)
        {
            equipmentManager.LogCurrentStats();
        }
        else
        {
            Debug.LogWarning("EquipmentManager not found");
        }
    }

    #endregion
}

// Rest of the classes remain the same...
[System.Serializable]
public class SerializableInventoryData
{
    public InventoryItem[] inventoryItems;
    public EquippedItem[] equippedItems;

    public SerializableInventoryData(Dictionary<int, InventoryItem> inventory, Dictionary<EquipmentType, EquippedItem> equipment)
    {
        inventoryItems = new InventoryItem[inventory.Count];
        int i = 0;
        foreach (var item in inventory.Values)
        {
            inventoryItems[i++] = item;
        }

        equippedItems = new EquippedItem[equipment.Count];
        i = 0;
        foreach (var item in equipment.Values)
        {
            equippedItems[i++] = item;
        }
    }

    public Dictionary<int, InventoryItem> GetInventoryDictionary()
    {
        var dict = new Dictionary<int, InventoryItem>();
        foreach (var item in inventoryItems)
        {
            dict[item.slotIndex] = item;
        }
        return dict;
    }

    public Dictionary<EquipmentType, EquippedItem> GetEquipmentDictionary()
    {
        var dict = new Dictionary<EquipmentType, EquippedItem>();
        foreach (var item in equippedItems)
        {
            dict[item.equipmentType] = item;
        }
        return dict;
    }
}