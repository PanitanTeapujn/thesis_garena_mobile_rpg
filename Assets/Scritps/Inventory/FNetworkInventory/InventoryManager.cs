using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Fusion;

public class InventoryManager : NetworkBehaviour
{
    [Header("Inventory Settings")]
    public int maxInventorySlots = 30;

    [Header("Item Database")]
    public ItemDatabase itemDatabase;

    // Network Variables
    [Networked, Capacity(30)]
    public NetworkArray<NetworkInventoryItem> NetworkInventory => default;

    [Networked, Capacity(5)]
    public NetworkArray<NetworkEquippedItem> NetworkEquipment => default;

    // Local cache
    private Dictionary<int, InventoryItem> localInventory = new Dictionary<int, InventoryItem>();
    private Dictionary<EquipmentType, EquippedItem> localEquipment = new Dictionary<EquipmentType, EquippedItem>();

    // Events
    public System.Action<InventoryItem> OnItemAdded;
    public System.Action<InventoryItem> OnItemRemoved;
    public System.Action<EquippedItem> OnItemEquipped;
    public System.Action<EquipmentType> OnItemUnequipped;
    public System.Action OnInventoryChanged;

    private FirebaseInventorySync firebaseSync;

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            // Setup Firebase sync for local player only
            firebaseSync = gameObject.AddComponent<FirebaseInventorySync>();
            firebaseSync.Initialize(this);

            // Load inventory from Firebase
            StartCoroutine(LoadInventoryFromFirebase());
        }

        // Initialize equipment slots
        InitializeEquipmentSlots();
    }

    private void InitializeEquipmentSlots()
    {
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

    private System.Collections.IEnumerator LoadInventoryFromFirebase()
    {
        yield return firebaseSync.LoadInventoryData();
        OnInventoryChanged?.Invoke();
    }

    #region Add/Remove Items

    public bool AddItem(string itemId, int quantity = 1)
    {
        if (!HasInputAuthority) return false;

        ItemData itemData = itemDatabase.GetItem(itemId);
        if (itemData == null)
        {
            Debug.LogError($"Item not found: {itemId}");
            return false;
        }

        // Try to stack with existing items first
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
                        // Fill this stack and continue with remaining
                        int remaining = newQuantity - itemData.maxStackSize;
                        RPC_UpdateInventorySlot(i, itemId, itemData.maxStackSize);
                        return AddItem(itemId, remaining);
                    }
                }
            }
        }

        // Find empty slot
        for (int i = 0; i < maxInventorySlots; i++)
        {
            if (string.IsNullOrEmpty(NetworkInventory[i].itemId.ToString()))
            {
                RPC_UpdateInventorySlot(i, itemId, quantity);
                return true;
            }
        }

        Debug.LogWarning("Inventory is full!");
        return false;
    }

    public bool RemoveItem(string itemId, int quantity = 1)
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

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_UpdateInventorySlot(int slotIndex, string itemId, int quantity)
    {
        NetworkInventory.Set(slotIndex, new NetworkInventoryItem
        {
            itemId = itemId,
            quantity = quantity
        });

        UpdateLocalCache();
        OnInventoryChanged?.Invoke();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_ClearInventorySlot(int slotIndex)
    {
        NetworkInventory.Set(slotIndex, new NetworkInventoryItem());
        UpdateLocalCache();
        OnInventoryChanged?.Invoke();
    }

    #endregion

    #region Equipment

    public bool EquipItem(string itemId)
    {
        if (!HasInputAuthority) return false;

        ItemData itemData = itemDatabase.GetItem(itemId);
        if (itemData == null || itemData.itemType != ItemType.Equipment)
        {
            Debug.LogError($"Cannot equip item: {itemId}");
            return false;
        }

        // Check if player has the item
        if (!HasItem(itemId))
        {
            Debug.LogWarning($"Player doesn't have item: {itemId}");
            return false;
        }

        // Unequip current item in this slot if exists
        int equipSlot = (int)itemData.equipmentType;
        var currentEquipped = NetworkEquipment[equipSlot];

        if (!string.IsNullOrEmpty(currentEquipped.itemId.ToString()))
        {
            // Add current equipped item back to inventory
            AddItem(currentEquipped.itemId.ToString(), 1);
        }

        // Remove from inventory
        RemoveItem(itemId, 1);

        // Equip new item
        RPC_EquipItem(equipSlot, itemId);

        return true;
    }

    public bool UnequipItem(EquipmentType equipmentType)
    {
        if (!HasInputAuthority) return false;

        int equipSlot = (int)equipmentType;
        var equipped = NetworkEquipment[equipSlot];

        if (string.IsNullOrEmpty(equipped.itemId.ToString())) return false;

        // Add back to inventory
        if (!AddItem(equipped.itemId.ToString(), 1))
        {
            Debug.LogWarning("Cannot unequip - inventory full!");
            return false;
        }

        // Clear equipment slot
        RPC_UnequipItem(equipSlot);
        return true;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_EquipItem(int equipSlot, string itemId)
    {
        ItemData itemData = itemDatabase.GetItem(itemId);
        NetworkEquipment.Set(equipSlot, new NetworkEquippedItem
        {
            itemId = itemId,
            equipmentType = itemData.equipmentType
        });

        UpdateLocalCache();
        OnItemEquipped?.Invoke(new EquippedItem(itemId, itemData.equipmentType));
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_UnequipItem(int equipSlot)
    {
        EquipmentType equipType = (EquipmentType)equipSlot;
        NetworkEquipment.Set(equipSlot, new NetworkEquippedItem
        {
            itemId = "",
            equipmentType = equipType
        });

        UpdateLocalCache();
        OnItemUnequipped?.Invoke(equipType);
    }

    #endregion

    #region Use Items

    public bool UseItem(string itemId)
    {
        if (!HasInputAuthority) return false;

        ItemData itemData = itemDatabase.GetItem(itemId);
        if (itemData == null || itemData.itemType != ItemType.Consumable)
        {
            Debug.LogError($"Cannot use item: {itemId}");
            return false;
        }

        if (!HasItem(itemId)) return false;

        // Apply item effects
        Character character = GetComponent<Character>();
        if (character != null)
        {
            // Heal
            if (itemData.healAmount > 0)
            {
                character.CurrentHp = Mathf.Min(character.MaxHp,
                    character.CurrentHp + itemData.healAmount);
            }

            // Restore mana
            if (itemData.manaAmount > 0)
            {
                character.CurrentMana = Mathf.Min(character.MaxMana,
                    character.CurrentMana + itemData.manaAmount);
            }
        }

        // Remove item from inventory
        RemoveItem(itemId, 1);

        Debug.Log($"Used item: {itemData.itemName}");
        return true;
    }

    #endregion

    #region Utility Methods

    public bool HasItem(string itemId, int requiredQuantity = 1)
    {
        int totalQuantity = 0;
        for (int i = 0; i < maxInventorySlots; i++)
        {
            var slot = NetworkInventory[i];
            if (!string.IsNullOrEmpty(slot.itemId.ToString()) && slot.itemId.ToString() == itemId)
            {
                totalQuantity += slot.quantity;
                if (totalQuantity >= requiredQuantity)
                    return true;
            }
        }
        return false;
    }

    public int GetItemQuantity(string itemId)
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

    public EquippedItem GetEquippedItem(EquipmentType equipmentType)
    {
        int slot = (int)equipmentType;
        var equipped = NetworkEquipment[slot];

        if (string.IsNullOrEmpty(equipped.itemId.ToString())) return null;

        return new EquippedItem(equipped.itemId.ToString(), equipped.equipmentType);
    }

    public ItemStats GetTotalEquipmentStats()
    {
        ItemStats totalStats = new ItemStats();

        for (int i = 0; i < 5; i++)
        {
            var equipped = NetworkEquipment[i];
            if (!string.IsNullOrEmpty(equipped.itemId.ToString()))
            {
                ItemData itemData = itemDatabase.GetItem(equipped.itemId.ToString());
                if (itemData != null)
                {
                    totalStats.attackDamage += itemData.stats.attackDamage;
                    totalStats.armor += itemData.stats.armor;
                    totalStats.maxHp += itemData.stats.maxHp;
                    totalStats.maxMana += itemData.stats.maxMana;
                    totalStats.moveSpeed += itemData.stats.moveSpeed;
                    totalStats.attackSpeed += itemData.stats.attackSpeed;
                    totalStats.criticalChance += itemData.stats.criticalChance;
                    totalStats.criticalDamage += itemData.stats.criticalDamage;
                }
            }
        }

        return totalStats;
    }

    private void UpdateLocalCache()
    {
        // Update local inventory cache
        localInventory.Clear();
        for (int i = 0; i < maxInventorySlots; i++)
        {
            var slot = NetworkInventory[i];
            if (!string.IsNullOrEmpty(slot.itemId.ToString()))
            {
                localInventory[i] = new InventoryItem(slot.itemId.ToString(), slot.quantity, i);
            }
        }

        // Update local equipment cache
        localEquipment.Clear();
        for (int i = 0; i < 5; i++)
        {
            var equipped = NetworkEquipment[i];
            if (!string.IsNullOrEmpty(equipped.itemId.ToString()))
            {
                localEquipment[equipped.equipmentType] = new EquippedItem(
                    equipped.itemId.ToString(), equipped.equipmentType);
            }
        }
    }

    public Dictionary<int, InventoryItem> GetInventoryItems()
    {
        return localInventory;
    }

    #endregion
}