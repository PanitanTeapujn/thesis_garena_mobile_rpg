using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System;
using System.Linq;

// ✅ เพิ่ม StarterItemEntry class สำหรับจัดการ starter items
[System.Serializable]
public class StarterItemEntry
{
    [Header("Starter Item Configuration")]
    public ItemData itemData;
    [Range(1, 999)]
    public int quantity = 1;
    [Tooltip("Optional description for this starter item")]
    public string note = "";

    public StarterItemEntry()
    {
        quantity = 1;
    }

    public StarterItemEntry(ItemData item, int count = 1, string itemNote = "")
    {
        itemData = item;
        quantity = count;
        note = itemNote;
    }

    public bool IsValid()
    {
        return itemData != null && quantity > 0;
    }
}

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

    [Header("🎁 Starter Items Configuration")]
    [SerializeField] private bool giveStarterItems = true;
    [SerializeField] private bool starterItemsGiven = false; // ป้องกันการให้ซ้ำ
    [SerializeField] private List<StarterItemEntry> starterItemsList = new List<StarterItemEntry>();

    [Header("🔄 Fallback Options")]
    [SerializeField] private bool useFallbackItems = true; // เปิด/ปิดการใช้ fallback items
    [SerializeField] private bool fallbackToTestItems = true; // ใช้ test items ถ้า database ไม่มี

    [Header("🎯 Test Items (ScriptableObjects) - For Fallback")]
    [SerializeField] private ItemData testSword;
    [SerializeField] private ItemData testStaff;
    [SerializeField] private ItemData testArmor;
    [SerializeField] private ItemData testBoots;
    [SerializeField] private ItemData testRune;
    [SerializeField] private List<ItemData> testItems = new List<ItemData>();

    [Header("🎯 Item Database")]
    [SerializeField] private bool useItemDatabase = true; // เปิด/ปิดการใช้ database

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
            // 🆕 รอให้ PersistentPlayerData พร้อมก่อน
            StartCoroutine(DelayedStartup());
        }

        // Subscribe to equipment events
        if (character != null)
        {
            Character.OnStatsChanged += OnCharacterStatsChanged;
        }
    }

    // 🆕 เพิ่ม method ใหม่สำหรับ delayed startup
    private IEnumerator DelayedStartup()
    {
        // รอ 2 frames เพื่อให้ PersistentPlayerData เริ่มต้นเสร็จ
        yield return null;
        yield return null;

        // ตรวจสอบและโหลดข้อมูลก่อนให้ starter items
        if (PersistentPlayerData.Instance != null)
        {
            if (PersistentPlayerData.Instance.ShouldLoadFromFirebase())
            {
                Debug.Log("[Inventory] Loading saved inventory data...");
                // ข้อมูลจะถูกโหลดโดย Character.LoadPlayerDataIfAvailable() แล้ว
                starterItemsGiven = true; // กันไม่ให้ให้ starter items
            }
            else
            {
                Debug.Log("[Inventory] No saved data, will give starter items");
                GiveStarterItems();
            }
        }
        else
        {
            Debug.LogWarning("[Inventory] PersistentPlayerData not ready, giving starter items");
            GiveStarterItems();
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

        // เก็บสถานะก่อนเพิ่ม item สำหรับ validation
        int usedSlotsBefore = UsedSlots;

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

                    if (count <= 0)
                    {
                        // 🆕 Auto-save หลังจากเพิ่ม item สำเร็จ
                        AutoSaveInventoryData("AddItem - Stack");
                        return true; // เพิ่มครบแล้ว
                    }
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

        // 🆕 ตรวจสอบว่าเพิ่ม item สำเร็จหรือไม่
        int usedSlotsAfter = UsedSlots;
        bool addSuccess = usedSlotsAfter > usedSlotsBefore;

        if (addSuccess)
        {
            Debug.Log($"[Inventory] ✅ Successfully added {itemData.ItemName}. Slots: {usedSlotsBefore} → {usedSlotsAfter}");

            // 🆕 Auto-save หลังจากเพิ่ม item สำเร็จ
            AutoSaveInventoryData("AddItem - New Slot");
        }
        else
        {
            Debug.LogWarning($"[Inventory] ⚠️ AddItem may have failed for {itemData.ItemName}");
        }

        return addSuccess;
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

        // เก็บข้อมูลก่อนลบ
        string itemName = slot.itemData.ItemName;
        int stackBefore = slot.stackCount;

        slot.stackCount -= count;

        bool itemRemoved = false;
        if (slot.stackCount <= 0)
        {
            // ลบ item ออกจาก slot
            slot.itemData = null;
            slot.stackCount = 0;
            itemRemoved = true;
            Debug.Log($"[Inventory] Removed item from slot {slotIndex}");
        }
        else
        {
            Debug.Log($"[Inventory] Removed {count} items from slot {slotIndex}. Remaining: {slot.stackCount}");
        }

        // แจ้ง UI
        OnInventoryItemChanged?.Invoke(character, slotIndex, slot);

        if (HasStateAuthority)
        {
            RPC_NotifyInventoryChanged(slotIndex, !slot.IsEmpty, slot.stackCount);
        }

        // 🆕 Auto-save หลังจากลบ item
        AutoSaveInventoryData($"RemoveItem - {itemName} from slot {slotIndex}");

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

        // แจ้ง UI
        if (HasStateAuthority)
        {
            RPC_NotifyInventoryChanged(fromSlot, !items[fromSlot].IsEmpty, items[fromSlot].stackCount);
            RPC_NotifyInventoryChanged(toSlot, !items[toSlot].IsEmpty, items[toSlot].stackCount);
        }

        // 🆕 Auto-save หลังจากย้าย item
        AutoSaveInventoryData($"MoveItem - slot {fromSlot} to {toSlot}");

        return true;
    }

    private void AutoSaveInventoryData(string action)
    {
        try
        {
            if (PersistentPlayerData.Instance != null && character != null)
            {
                Debug.Log($"[Inventory] 💾 Auto-saving after: {action}");

                // ใช้ Coroutine เพื่อไม่ให้ block การทำงาน
                StartCoroutine(DelayedAutoSave(action));
            }
            else
            {
                Debug.LogWarning($"[Inventory] Cannot auto-save: PersistentPlayerData={PersistentPlayerData.Instance != null}, Character={character != null}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Inventory] ❌ Auto-save error: {e.Message}");
        }
    }

    // 🆕 เพิ่ม Delayed Auto-Save
    private IEnumerator DelayedAutoSave(string action)
    {
        // รอ 0.5 วินาที เพื่อให้ UI update เสร็จก่อน
        yield return new WaitForSeconds(0.5f);

        try
        {
            PersistentPlayerData.Instance?.SafeAutoSaveInventory(character, "AddItem");
            Debug.Log($"[Inventory] ✅ Auto-save completed for: {action}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Inventory] ❌ Delayed auto-save error: {e.Message}");
        }
    }

    // ✅ แก้ไข GiveStarterItems ให้ใช้ระบบใหม่
    private void GiveStarterItems()
    {
        if (!giveStarterItems || starterItemsGiven) return;

        // 🆕 ตรวจสอบว่ามีข้อมูลจาก Firebase หรือไม่
        if (PersistentPlayerData.Instance != null && PersistentPlayerData.Instance.ShouldLoadFromFirebase())
        {
            Debug.Log("🔄 [Inventory] Found saved data from Firebase, skipping starter items");
            starterItemsGiven = true; // กันไม่ให้ให้ starter items
            return;
        }

        Debug.Log("🎁 Giving configurable starter items...");

        // รอ 1 frame เพื่อให้ระบบ setup เสร็จ
        ItemDatabase database = GetDatabase(); // อาจจะ null ก็ได้
        StartCoroutine(GiveStarterItemsCoroutine(database));
    }

    // ✅ แก้ไข GiveStarterItemsCoroutine ให้ใช้ระบบใหม่
    private IEnumerator GiveStarterItemsCoroutine(ItemDatabase database)
    {
        yield return null; // รอ 1 frame

        int totalItemsGiven = 0;
        int totalEntries = 0;

        Debug.Log("🎁 === GIVING CONFIGURABLE STARTER ITEMS ===");

        // ✅ วิธีที่ 1: ใช้ starter items list ที่กำหนดเอง
        if (starterItemsList != null && starterItemsList.Count > 0)
        {
            Debug.Log($"📝 Found {starterItemsList.Count} starter item entries");

            foreach (StarterItemEntry entry in starterItemsList)
            {
                if (!entry.IsValid())
                {
                    Debug.LogWarning($"⚠️ Invalid starter item entry: {entry.note}");
                    continue;
                }

                bool success = AddItem(entry.itemData, entry.quantity);
                if (success)
                {
                    totalItemsGiven += entry.quantity;
                    totalEntries++;

                    string logMessage = $"  ✅ Added: {entry.itemData.ItemName} x{entry.quantity} ({entry.itemData.GetTierText()})";
                    if (!string.IsNullOrEmpty(entry.note))
                    {
                        logMessage += $" - {entry.note}";
                    }

                    Debug.Log(logMessage);

                    // แสดง potion effects ถ้าเป็น potion
                    if (entry.itemData.ItemType == ItemType.Potion && entry.itemData.Stats.IsPotion())
                    {
                        string effects = "";
                        if (entry.itemData.Stats.healAmount > 0) effects += $"+{entry.itemData.Stats.healAmount}HP ";
                        if (entry.itemData.Stats.manaAmount > 0) effects += $"+{entry.itemData.Stats.manaAmount}MP ";
                        if (entry.itemData.Stats.healPercentage > 0) effects += $"+{entry.itemData.Stats.healPercentage:P0}HP ";
                        if (entry.itemData.Stats.manaPercentage > 0) effects += $"+{entry.itemData.Stats.manaPercentage:P0}MP ";
                        Debug.Log($"    💊 Effects: {effects.Trim()}");
                    }
                }
                else
                {
                    Debug.LogWarning($"❌ Failed to add: {entry.itemData.ItemName} x{entry.quantity}");
                }
            }
        }
        // ✅ วิธีที่ 2: Fallback ไปใช้ test items ถ้าไม่มี starter items list
        else if (useFallbackItems && testItems != null && testItems.Count > 0)
        {
            Debug.Log("📝 No starter items configured, using fallback test items");

            foreach (ItemData testItem in testItems)
            {
                if (testItem == null) continue;

                int quantity = testItem.ItemType == ItemType.Potion ? 10 : 1; // potion ให้ 10 ชิ้น
                bool success = AddItem(testItem, quantity);

                if (success)
                {
                    totalItemsGiven += quantity;
                    totalEntries++;
                    Debug.Log($"  ✅ Added (fallback): {testItem.ItemName} x{quantity} ({testItem.GetTierText()})");
                }
            }

            // เพิ่ม individual test items
            if (testSword != null) { AddItem(testSword, 1); totalItemsGiven++; totalEntries++; }
            if (testStaff != null) { AddItem(testStaff, 1); totalItemsGiven++; totalEntries++; }
            if (testArmor != null) { AddItem(testArmor, 1); totalItemsGiven++; totalEntries++; }
            if (testBoots != null) { AddItem(testBoots, 1); totalItemsGiven++; totalEntries++; }
            if (testRune != null) { AddItem(testRune, 5); totalItemsGiven += 5; totalEntries++; }
        }
        // ✅ วิธีที่ 3: Fallback ไปใช้ database แบบเก่า
        else if (fallbackToTestItems && useItemDatabase && database != null)
        {
            Debug.Log("📝 No starter items or fallback items, using database fallback");

            // ให้ของแต่ละประเภทนิดหน่อย
            var weapons = database.GetItemsByType(ItemType.Weapon);
            if (weapons.Count > 0) { AddItem(weapons[0], 1); totalItemsGiven++; totalEntries++; }

            var potions = database.GetItemsByType(ItemType.Potion);
            if (potions.Count > 0) { AddItem(potions[0], 10); totalItemsGiven += 10; totalEntries++; }

            var armors = database.GetItemsByType(ItemType.Armor);
            if (armors.Count > 0) { AddItem(armors[0], 1); totalItemsGiven++; totalEntries++; }
        }
        else
        {
            Debug.LogWarning("⚠️ No starter items configured and fallback disabled!");
        }

        starterItemsGiven = true;

        Debug.Log($"🎉 === CONFIGURABLE STARTER ITEMS COMPLETE ===");
        Debug.Log($"📊 Total Entries: {totalEntries}");
        Debug.Log($"📊 Total Items Given: {totalItemsGiven}");
        Debug.Log($"💼 Inventory Status: {UsedSlots}/{CurrentSlots} slots used");

        // แสดงสรุป inventory
        LogInventorySummary();
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

    private void LogInventorySummary()
    {
        Debug.Log("=== INVENTORY SUMMARY ===");

        for (int i = 0; i < CurrentSlots; i++)
        {
            InventoryItem item = GetItem(i);
            if (item != null && !item.IsEmpty)
            {
                string itemInfo = $"Slot {i}: {item.itemData.ItemName}";
                if (item.stackCount > 1) itemInfo += $" x{item.stackCount}";
                itemInfo += $" ({item.itemData.ItemType}, {item.itemData.GetTierText()})";

                // แสดง potion effects ถ้าเป็น potion
                if (item.itemData.ItemType == ItemType.Potion && item.itemData.Stats.IsPotion())
                {
                    string effects = "";
                    if (item.itemData.Stats.healAmount > 0) effects += $"+{item.itemData.Stats.healAmount}HP ";
                    if (item.itemData.Stats.manaAmount > 0) effects += $"+{item.itemData.Stats.manaAmount}MP ";
                    if (item.itemData.Stats.healPercentage > 0) effects += $"+{item.itemData.Stats.healPercentage:P0}HP ";
                    if (item.itemData.Stats.manaPercentage > 0) effects += $"+{item.itemData.Stats.manaPercentage:P0}MP ";
                    itemInfo += $" [💊 {effects.Trim()}]";
                }

                Debug.Log(itemInfo);
            }
        }
    }

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
    // เพิ่ม Context Menu functions ได้ที่นี่ถ้าต้องการ
    #endregion
}