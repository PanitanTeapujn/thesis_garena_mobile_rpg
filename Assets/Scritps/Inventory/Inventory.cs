using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System;
using System.Linq;
[System.Serializable]
public class InventoryItem
{
    public ItemData itemData;
    public int stackCount;
    public int slotIndex; // ตำแหน่งใน inventory

    public InventoryItem(ItemData item = null, int count = 0, int slot = -1)
    {
        itemData = item;
        stackCount = count;
        slotIndex = slot;
    }

    public bool IsEmpty => itemData == null || stackCount <= 0;
    public bool CanStack => itemData != null && itemData.CanStack();
    public bool IsMaxStack => itemData != null && stackCount >= itemData.MaxStackSize;
    public int GetMaxStackSize() => itemData?.MaxStackSize ?? 1;
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
    [Header("🎯 Test Items (ScriptableObjects)")]
    [SerializeField] private ItemData testSword;
    [SerializeField] private ItemData testStaff;
    [SerializeField] private ItemData testArmor;
    [SerializeField] private ItemData testBoots;
    [SerializeField] private ItemData testRune;
    [SerializeField] private List<ItemData> testItems = new List<ItemData>();
    [Header("🎁 Starter Items")]
    [SerializeField] private bool giveStarterItems = true;
    [SerializeField] private bool starterItemsGiven = false; // ป้องกันการให้ซ้ำ
    [Header("🎯 Item Database")]
    [SerializeField] private bool useItemDatabase = true; // เปิด/ปิดการใช้ database
    [SerializeField] private bool fallbackToTestItems = true; // ใช้ test items ถ้า database ไม่มี

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
        if (HasStateAuthority) // เฉพาะ host/authority เท่านั้น
        {
            GiveStarterItems();
        }
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

        // ✅ Force สร้าง inventory grid ก่อนเพิ่ม item
        ForceCreateInventoryGridIfNeeded();

        // ถ้า item สามารถ stack ได้ ลองหา slot ที่มี item เดียวกันแล้วยังไม่เต็ม
        if (itemData.CanStack())
        {
            for (int i = 0; i < currentSlots; i++)
            {
                InventoryItem slot = items[i];
                if (!slot.IsEmpty && slot.itemData.CanStackWith(itemData) && !slot.IsMaxStack)
                {
                    int canAdd = Mathf.Min(count, itemData.MaxStackSize - slot.stackCount);
                    slot.stackCount += canAdd;
                    count -= canAdd;

                    Debug.Log($"[Inventory] Stacked {canAdd} {itemData.ItemName} in slot {i}. Total: {slot.stackCount}");

                    // ✅ แจ้ง UI ทันที
                    OnInventoryItemChanged?.Invoke(character, i, slot);

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

            // ✅ แจ้ง UI ทันที
            OnInventoryItemChanged?.Invoke(character, emptySlot, items[emptySlot]);

            if (HasStateAuthority)
            {
                RPC_NotifyInventoryChanged(emptySlot, true, addCount);
            }
        }

        return true;
    }
    private void ForceCreateInventoryGridIfNeeded()
    {
        // หา InventoryGridManager
        InventoryGridManager gridManager = FindObjectOfType<InventoryGridManager>();

        if (gridManager != null)
        {
            // ถ้า gridManager มีแต่ยังไม่มี slots
            if (gridManager.AllSlots.Count == 0)
            {
                Debug.Log("[Inventory] Requesting inventory grid creation...");

                // Set character ถ้ายังไม่ได้ set
                if (gridManager.OwnerCharacter == null)
                {
                    gridManager.SetOwnerCharacter(character);
                }

                // ✅ เพิ่มการ force update ทันที
                gridManager.ForceUpdateFromCharacter();

                // รอ 1 frame แล้วตรวจสอบอีกครั้ง
                StartCoroutine(VerifyGridCreation(gridManager));
            }
            else
            {
                Debug.Log("[Inventory] Grid already exists, updating...");
                gridManager.ForceUpdateFromCharacter();
            }
        }
        else
        {
            Debug.LogWarning("[Inventory] No InventoryGridManager found! Requesting setup...");

            // หา CombatUIManager และ setup grid
            CombatUIManager uiManager = FindObjectOfType<CombatUIManager>();
            if (uiManager != null)
            {
                uiManager.ForceSetupInventoryGrid();
                // รอแล้วลองอีกครั้ง
                StartCoroutine(RetryForceCreateGrid());
            }
            else
            {
                Debug.LogError("[Inventory] No CombatUIManager found!");
            }
        }
    }
    private IEnumerator VerifyGridCreation(InventoryGridManager gridManager)
    {
        yield return null; // รอ 1 frame

        if (gridManager.AllSlots.Count > 0)
        {
            Debug.Log("[Inventory] Grid creation verified successfully");

            // ✅ Sync ทุก slots ทันที
            gridManager.ForceSyncAllSlots();
        }
        else
        {
            Debug.LogWarning("[Inventory] Grid creation failed, forcing again...");
            gridManager.ForceUpdateFromCharacter();

            yield return null;
            gridManager.ForceSyncAllSlots();
        }
    }

    private IEnumerator RequestGridCreation(InventoryGridManager gridManager)
    {
        yield return null; // รอ 1 frame

        gridManager.ForceUpdateFromCharacter();

        yield return null; // รออีก 1 frame

        // ตรวจสอบว่าสร้างแล้วหรือยัง
        if (gridManager.AllSlots.Count > 0)
        {
            Debug.Log("[Inventory] Grid creation successful");
        }
        else
        {
            Debug.LogWarning("[Inventory] Grid creation failed, retrying...");
            gridManager.ForceUpdateFromCharacter();
        }
    }
    private IEnumerator RetryForceCreateGrid()
    {
        yield return null; // รอ 1 frame

        InventoryGridManager gridManager = FindObjectOfType<InventoryGridManager>();
        if (gridManager != null && gridManager.OwnerCharacter == null)
        {
            gridManager.SetOwnerCharacter(character);
            gridManager.ForceUpdateFromCharacter();
            Debug.Log("[Inventory] Retry grid creation successful");
        }
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
    private void GiveStarterItems()
    {
        if (!giveStarterItems || starterItemsGiven) return;

        ItemDatabase database = GetDatabase();
        if (database == null)
        {
            Debug.LogWarning("[Inventory] No ItemDatabase found for starter items");
            return;
        }

        Debug.Log("🎁 Giving starter items...");

        // รอ 1 frame เพื่อให้ระบบ setup เสร็จ
        StartCoroutine(GiveStarterItemsCoroutine(database));
    }

    private IEnumerator GiveStarterItemsCoroutine(ItemDatabase database)
    {
        yield return null; // รอ 1 frame

        int itemsGiven = 0;

        // ให้ weapon 1 ชิ้น
        var weapons = database.GetItemsByType(ItemType.Weapon);
        if (weapons.Count > 0)
        {
            ItemData weapon = weapons[UnityEngine.Random.Range(0, weapons.Count)];
            if (AddItem(weapon, 1))
            {
                itemsGiven++;
                Debug.Log($"🗡️ Starter weapon: {weapon.ItemName}");
            }
        }

        // ให้ potion 5-10 ขวด
        var potions = database.GetItemsByType(ItemType.Potion);
        if (potions.Count > 0)
        {
            ItemData potion = potions[0]; // เอาอันแรก
            int count = UnityEngine.Random.Range(5, 11);
            if (AddItem(potion, count))
            {
                itemsGiven++;
                Debug.Log($"🧪 Starter potions: {potion.ItemName} x{count}");
            }
        }

        // ให้ armor 1 ชิ้น
        var armors = database.GetItemsByType(ItemType.Armor);
        if (armors.Count > 0)
        {
            ItemData armor = armors[UnityEngine.Random.Range(0, armors.Count)];
            if (AddItem(armor, 1))
            {
                itemsGiven++;
                Debug.Log($"🛡️ Starter armor: {armor.ItemName}");
            }
        }
        var plants = database.GetItemsByType(ItemType.Pants);
        if (plants.Count > 0)
        {
            ItemData plant = plants[UnityEngine.Random.Range(0, plants.Count)];
            if (AddItem(plant, 1))
            {
                itemsGiven++;
                Debug.Log($"🛡️ Starter armor: {plant.ItemName}");
            }
        }
        var heads = database.GetItemsByType(ItemType.Head);
        if (heads.Count > 0)
        {
            ItemData head = heads[UnityEngine.Random.Range(0, heads.Count)];
            if (AddItem(head, 1))
            {
                itemsGiven++;
                Debug.Log($"🛡️ Starter armor: {head.ItemName}");
            }
        }
        var shoses = database.GetItemsByType(ItemType.Shoes);
        if (shoses.Count > 0)
        {
            ItemData shose = shoses[UnityEngine.Random.Range(0, shoses.Count)];
            if (AddItem(shose, 1))
            {
                itemsGiven++;
                Debug.Log($"🛡️ Starter armor: {shose.ItemName}");
            }
        }

        // ให้ rune 3-5 ชิ้น
        var runes = database.GetItemsByType(ItemType.Rune);
        if (runes.Count > 0)
        {
            ItemData rune = runes[UnityEngine.Random.Range(0, runes.Count)];
            int count = UnityEngine.Random.Range(3, 6);
            if (AddItem(rune, count))
            {
                itemsGiven++;
                Debug.Log($"💎 Starter runes: {rune.ItemName} x{count}");
            }
        }

        starterItemsGiven = true;
        Debug.Log($"🎁 Gave {itemsGiven} types of starter items!");
    }
    public ItemDatabase GetDatabase()
    {
        if (!useItemDatabase) return null;

        // ใช้ Resources folder (วิธีที่ 1)
        return ItemDatabase.Instance;
    }

    // เพิ่ม method ตรวจสอบ database
    private bool HasDatabase()
    {
        return GetDatabase() != null && GetDatabase().GetAllItems().Count > 0;
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

    public bool UsePotion(int slotIndex, int count = 1)
    {
        if (slotIndex < 0 || slotIndex >= currentSlots || count <= 0)
        {
            Debug.LogWarning($"[Inventory] Invalid use parameters: slot {slotIndex}, count {count}");
            return false;
        }

        InventoryItem slot = items[slotIndex];
        if (slot.IsEmpty)
        {
            Debug.LogWarning($"[Inventory] Slot {slotIndex} is empty");
            return false;
        }

        if (slot.itemData.ItemType != ItemType.Potion)
        {
            Debug.LogWarning($"[Inventory] Item '{slot.itemData.ItemName}' is not a potion");
            return false;
        }

        if (slot.stackCount < count)
        {
            Debug.LogWarning($"[Inventory] Not enough potions in slot {slotIndex}. Has: {slot.stackCount}, Requested: {count}");
            return false;
        }

        // ใช้ potion (ลดจำนวน)
        bool success = RemoveItem(slotIndex, count);

        if (success)
        {
            Debug.Log($"[Inventory] Used {count} {slot.itemData.ItemName}");

            // TODO: Apply potion effects ที่นี่
            // ApplyPotionEffect(slot.itemData, count);
        }

        return success;
    }

    #region Context Menu for Testing


    // เพิ่ม method สำหรับใช้ database


    // เพิ่ม method สำหรับ test items แบบเก่า

    // เพิ่ม Context Menu ใหม่ๆ สำหรับ database








    [ContextMenu("🧪 Test: Add Items from Database")]
    private void TestAddItemsFromDatabase()
    {
        ItemDatabase database = GetDatabase();
        if (database == null)
        {
            Debug.LogWarning("❌ ItemDatabase not found!");
            return;
        }

        Debug.Log("🧪 Testing items from database...");

        // ลอง add อย่างละชนิด
        var allItems = database.GetAllItems();
        if (allItems.Count == 0)
        {
            Debug.LogWarning("❌ No items found in database!");
            return;
        }

        // เอา weapon 1 ชิ้น
        var weapons = database.GetItemsByType(ItemType.Weapon);
        if (weapons.Count > 0)
        {
            bool success = AddItem(weapons[0], 1);
            Debug.Log($"🗡️ Weapon: {weapons[0].ItemName} - {(success ? "✅ Added" : "❌ Failed")}");
        }

        // เอา potion 5 ชิ้น
        var potions = database.GetItemsByType(ItemType.Potion);
        if (potions.Count > 0)
        {
            bool success = AddItem(potions[0], 5);
            Debug.Log($"🧪 Potion: {potions[0].ItemName} x5 - {(success ? "✅ Added" : "❌ Failed")}");
        }

        // เอา armor 1 ชิ้น
        var armors = database.GetItemsByType(ItemType.Armor);
        if (armors.Count > 0)
        {
            bool success = AddItem(armors[0], 1);
            Debug.Log($"🛡️ Armor: {armors[0].ItemName} - {(success ? "✅ Added" : "❌ Failed")}");
        }

        Debug.Log($"🧪 Test complete! Check inventory slots for items and icons.");
    }




    #endregion
}