using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System;

[System.Serializable]
public class InventoryItem
{
    public ItemData itemData;
    public int stackCount;
    public int slotIndex; // ตำแหน่งใน inventory

    public InventoryItem(ItemData item, int count = 1, int slot = -1)
    {
        itemData = item;
        stackCount = count;
        slotIndex = slot;
    }

    public bool IsEmpty => itemData == null;
    public bool CanStack => itemData != null && itemData.CanStack();
    public bool IsMaxStack => stackCount >= itemData.MaxStackSize;
}

public class Inventory : NetworkBehaviour
{
    #region Events
    public static event Action<Character, int> OnInventorySlotCountChanged;
    public static event Action<Character, int, InventoryItem> OnInventoryItemChanged;
    public static event Action<Character> OnInventoryCleared;
    #endregion

    #region Inventory Settings
    [Header("📦 Inventory Settings")]
    [SerializeField] private int maxSlots = 48; // 8x6 = 48 slots เริ่มต้น
    [SerializeField] private int currentSlots = 24; // เริ่มต้นที่ 24 ช่อง (6x4)
    [SerializeField] private List<InventoryItem> items = new List<InventoryItem>();

    [Header("🎯 Grid Layout")]
    [SerializeField] private int gridWidth = 6;   // จำนวน columns
    [SerializeField] private int gridHeight = 4;  // จำนวน rows
    #endregion

    #region Character Reference
    private Character character;
    #endregion

    #region Networked Properties
    [Networked] public int NetworkedCurrentSlots { get; set; }
    [Networked] public int NetworkedMaxSlots { get; set; }
    #endregion

    #region Properties

    public int GridWidth { get { return gridWidth; } }
    public int GridHeight { get { return gridHeight; } }
    public int MaxSlots { get { return maxSlots; } }
    public int CurrentSlots { get { return currentSlots; } }
    public List<InventoryItem> Items { get { return items; } }
    public int UsedSlots
    {
        get
        {
            int count = 0;
            foreach (var item in items)
            {
                if (!item.IsEmpty) count++;
            }
            return count;
        }
    }
    public int FreeSlots { get { return currentSlots - UsedSlots; } }
    #endregion

    #region Unity Lifecycle & Initialization
    protected virtual void Awake()
    {
        character = GetComponent<Character>();
        InitializeInventory();
    }

    protected virtual void Start()
    {
        // Subscribe to equipment events
        if (character != null)
        {
            Character.OnStatsChanged += OnCharacterStatsChanged;
        }
    }

    private void OnDestroy()
    {
        Character.OnStatsChanged -= OnCharacterStatsChanged;
    }

    private void InitializeInventory()
    {
        // คำนวณ grid dimensions ก่อน
        CalculateGridDimensions();

        // สร้าง empty slots
        items.Clear();
        for (int i = 0; i < maxSlots; i++)
        {
            items.Add(new InventoryItem(null, 0, i));
        }

        Debug.Log($"[Inventory] Initialized for {character?.CharacterName} - Slots: {currentSlots}/{maxSlots} ({gridWidth}x{gridHeight})");
    }
    #endregion

    #region Fusion Network Methods
    public override void Spawned()
    {
        base.Spawned();

        if (HasStateAuthority)
        {
            NetworkedCurrentSlots = currentSlots;
            NetworkedMaxSlots = maxSlots;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority)
        {
            // Sync inventory data
            NetworkedCurrentSlots = currentSlots;
            NetworkedMaxSlots = maxSlots;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyInventoryChanged(int slotIndex, bool hasItem, int stackCount)
    {
        // แจ้งการเปลี่ยนแปลงของ item ใน slot
        if (slotIndex >= 0 && slotIndex < items.Count)
        {
            if (hasItem)
            {
                items[slotIndex].stackCount = stackCount;
            }
            else
            {
                items[slotIndex].itemData = null;
                items[slotIndex].stackCount = 0;
            }

            OnInventoryItemChanged?.Invoke(character, slotIndex, items[slotIndex]);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifySlotCountChanged(int newSlotCount)
    {
        currentSlots = newSlotCount;
        OnInventorySlotCountChanged?.Invoke(character, newSlotCount);
    }
    #endregion
    private void CalculateGridDimensions()
    {
        // หา dimensions ที่เหมาะสมที่สุดตาม currentSlots
        if (currentSlots <= 24) // 6x4
        {
            gridWidth = 6;
            gridHeight = 4;
        }
        else if (currentSlots <= 30) // 6x5
        {
            gridWidth = 6;
            gridHeight = 5;
        }
        else if (currentSlots <= 36) // 6x6
        {
            gridWidth = 6;
            gridHeight = 6;
        }
        else if (currentSlots <= 42) // 7x6
        {
            gridWidth = 7;
            gridHeight = 6;
        }
        else // 8x6 หรือมากกว่า
        {
            gridWidth = 8;
            gridHeight = Mathf.CeilToInt((float)currentSlots / gridWidth);
        }

        Debug.Log($"[Inventory] Grid dimensions: {gridWidth}x{gridHeight} for {currentSlots} slots");
    }
    #region Inventory Management
    public bool AddItem(ItemData itemData, int count = 1)
    {
        if (itemData == null || count <= 0)
        {
            Debug.LogWarning($"[Inventory] Invalid item or count: {itemData?.ItemName}, Count: {count}");
            return false;
        }

        // ถ้า item สามารถ stack ได้ ลองหา slot ที่มี item เดียวกันแล้วยังไม่เต็ม
        if (itemData.CanStack())
        {
            for (int i = 0; i < currentSlots; i++)
            {
                InventoryItem slot = items[i];
                if (!slot.IsEmpty && slot.itemData.ItemName == itemData.ItemName && !slot.IsMaxStack)
                {
                    int canAdd = Mathf.Min(count, itemData.MaxStackSize - slot.stackCount);
                    slot.stackCount += canAdd;
                    count -= canAdd;

                    Debug.Log($"[Inventory] Stacked {canAdd} {itemData.ItemName} in slot {i}. Total: {slot.stackCount}");

                    if (HasStateAuthority)
                    {
                        RPC_NotifyInventoryChanged(i, true, slot.stackCount);
                    }

                    if (count <= 0) return true; // เพิ่มครบแล้ว
                }
            }
        }

        // หาช่องว่างสำหรับ item ที่เหลือ
        while (count > 0)
        {
            int emptySlot = FindFirstEmptySlot();
            if (emptySlot == -1)
            {
                Debug.LogWarning($"[Inventory] No empty slots available! Cannot add {count} {itemData.ItemName}");
                return false;
            }

            int addCount = Mathf.Min(count, itemData.MaxStackSize);
            items[emptySlot].itemData = itemData;
            items[emptySlot].stackCount = addCount;
            count -= addCount;

            Debug.Log($"[Inventory] Added {addCount} {itemData.ItemName} to slot {emptySlot}");

            if (HasStateAuthority)
            {
                RPC_NotifyInventoryChanged(emptySlot, true, addCount);
            }
        }

        return true;
    }

    public bool RemoveItem(int slotIndex, int count = 1)
    {
        if (slotIndex < 0 || slotIndex >= currentSlots || count <= 0)
        {
            Debug.LogWarning($"[Inventory] Invalid remove parameters: slot {slotIndex}, count {count}");
            return false;
        }

        InventoryItem slot = items[slotIndex];
        if (slot.IsEmpty)
        {
            Debug.LogWarning($"[Inventory] Slot {slotIndex} is already empty");
            return false;
        }

        if (slot.stackCount < count)
        {
            Debug.LogWarning($"[Inventory] Not enough items in slot {slotIndex}. Has: {slot.stackCount}, Requested: {count}");
            return false;
        }

        slot.stackCount -= count;

        if (slot.stackCount <= 0)
        {
            // ลบ item ออกจาก slot
            slot.itemData = null;
            slot.stackCount = 0;
            Debug.Log($"[Inventory] Removed item from slot {slotIndex}");
        }
        else
        {
            Debug.Log($"[Inventory] Removed {count} items from slot {slotIndex}. Remaining: {slot.stackCount}");
        }

        if (HasStateAuthority)
        {
            RPC_NotifyInventoryChanged(slotIndex, !slot.IsEmpty, slot.stackCount);
        }

        return true;
    }

    public InventoryItem GetItem(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < items.Count)
        {
            return items[slotIndex];
        }
        return null;
    }

    public bool IsSlotEmpty(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < currentSlots)
        {
            return items[slotIndex].IsEmpty;
        }
        return true;
    }

    public int FindFirstEmptySlot()
    {
        for (int i = 0; i < currentSlots; i++)
        {
            if (items[i].IsEmpty)
            {
                return i;
            }
        }
        return -1; // ไม่มีช่องว่าง
    }

    public void ClearInventory()
    {
        for (int i = 0; i < items.Count; i++)
        {
            items[i].itemData = null;
            items[i].stackCount = 0;
        }

        Debug.Log($"[Inventory] Cleared all items for {character?.CharacterName}");

        if (HasStateAuthority)
        {
            OnInventoryCleared?.Invoke(character);
        }
    }

    public bool MoveItem(int fromSlot, int toSlot)
    {
        if (fromSlot < 0 || fromSlot >= currentSlots || toSlot < 0 || toSlot >= currentSlots)
        {
            Debug.LogWarning($"[Inventory] Invalid move slots: {fromSlot} -> {toSlot}");
            return false;
        }

        if (fromSlot == toSlot) return true; // ย้ายไปที่เดียวกัน

        InventoryItem fromItem = items[fromSlot];
        InventoryItem toItem = items[toSlot];

        if (fromItem.IsEmpty)
        {
            Debug.LogWarning($"[Inventory] Source slot {fromSlot} is empty");
            return false;
        }

        // สลับตำแหน่ง items
        items[fromSlot] = toItem;
        items[toSlot] = fromItem;

        // อัพเดท slot indices
        items[fromSlot].slotIndex = fromSlot;
        items[toSlot].slotIndex = toSlot;

        Debug.Log($"[Inventory] Moved item from slot {fromSlot} to slot {toSlot}");

        if (HasStateAuthority)
        {
            RPC_NotifyInventoryChanged(fromSlot, !items[fromSlot].IsEmpty, items[fromSlot].stackCount);
            RPC_NotifyInventoryChanged(toSlot, !items[toSlot].IsEmpty, items[toSlot].stackCount);
        }

        return true;
    }
    #endregion

    #region Inventory Expansion
    public void ExpandInventory(int additionalSlots)
    {
        int newSlotCount = Mathf.Min(currentSlots + additionalSlots, maxSlots);

        if (newSlotCount > currentSlots)
        {
            currentSlots = newSlotCount;

            // คำนวณ grid dimensions ใหม่
            CalculateGridDimensions();

            Debug.Log($"[Inventory] Expanded inventory to {currentSlots} slots ({gridWidth}x{gridHeight})");

            if (HasStateAuthority)
            {
                NetworkedCurrentSlots = currentSlots;
                RPC_NotifySlotCountChanged(currentSlots);
            }
        }
        else
        {
            Debug.LogWarning($"[Inventory] Cannot expand beyond max slots ({maxSlots})");
        }
    }
    public (int row, int col) SlotIndexToRowCol(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= currentSlots)
            return (-1, -1);

        int row = slotIndex / gridWidth;
        int col = slotIndex % gridWidth;
        return (row, col);
    }

    // เพิ่ม method สำหรับแปลง row/column เป็น slot index
    public int RowColToSlotIndex(int row, int col)
    {
        if (row < 0 || row >= gridHeight || col < 0 || col >= gridWidth)
            return -1;

        int slotIndex = row * gridWidth + col;
        return slotIndex < currentSlots ? slotIndex : -1;
    }

    public bool CanExpandInventory(int additionalSlots)
    {
        return (currentSlots + additionalSlots) <= maxSlots;
    }
    #endregion

    #region Item Search & Query
    public List<InventoryItem> FindItemsByType(ItemType itemType)
    {
        List<InventoryItem> foundItems = new List<InventoryItem>();

        for (int i = 0; i < currentSlots; i++)
        {
            InventoryItem item = items[i];
            if (!item.IsEmpty && item.itemData.ItemType == itemType)
            {
                foundItems.Add(item);
            }
        }

        return foundItems;
    }

    public InventoryItem FindFirstItemByName(string itemName)
    {
        for (int i = 0; i < currentSlots; i++)
        {
            InventoryItem item = items[i];
            if (!item.IsEmpty && item.itemData.ItemName == itemName)
            {
                return item;
            }
        }

        return null;
    }

    public int GetItemCount(string itemName)
    {
        int total = 0;

        for (int i = 0; i < currentSlots; i++)
        {
            InventoryItem item = items[i];
            if (!item.IsEmpty && item.itemData.ItemName == itemName)
            {
                total += item.stackCount;
            }
        }

        return total;
    }
    #endregion

    #region Event Handlers
    private void OnCharacterStatsChanged()
    {
        // อาจจะมีการขยาย inventory ตาม level หรือ stats
        // ยังไม่ implement ในขั้นนี้
    }
    #endregion

    #region Context Menu for Testing
    [ContextMenu("📦 Show Inventory Info")]
    private void ShowInventoryInfo()
    {
        Debug.Log("=== INVENTORY INFORMATION ===");
        Debug.Log($"📛 Owner: {character?.CharacterName}");
        Debug.Log($"📦 Slots: {currentSlots}/{maxSlots}");
        Debug.Log($"📊 Used: {UsedSlots}, Free: {FreeSlots}");
        Debug.Log("=============================");

        for (int i = 0; i < currentSlots; i++)
        {
            InventoryItem item = items[i];
            if (!item.IsEmpty)
            {
                Debug.Log($"[{i:D2}] {item.itemData.ItemName} x{item.stackCount}");
            }
        }
    }

    [ContextMenu("🧪 Test: Add Test Items")]
    private void TestAddItems()
    {
        // ต้องมี ItemData test objects สำหรับทดสอบ
        Debug.Log("[Inventory] Test adding items - Need ItemData assets to test properly");
    }

    [ContextMenu("🗑️ Test: Clear Inventory")]
    private void TestClearInventory()
    {
        ClearInventory();
    }

    [ContextMenu("📈 Test: Expand Inventory (+12 slots)")]
    private void TestExpandInventory()
    {
        ExpandInventory(12);
    }
    #endregion
}