using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class EquipmentSlotsManager : MonoBehaviour
{
    #region Events
    public static event Action<ItemData, ItemType> OnItemEquipped;
    public static event Action<ItemData, ItemType> OnItemUnequipped;
    public static event Action OnEquipmentChanged;
    #endregion

    #region UI References (เหมือน ItemDetailPanel)
    [Header("Equipment Slots Panel")]
    public GameObject equipmentSlotsPanel;  // Panel หลักที่ใส่ slots ทั้งหมด

    [Header("Equipment Slots")]
    public EquipmentSlot weaponSlot;
    public EquipmentSlot headSlot;
    public EquipmentSlot armorSlot;
    public EquipmentSlot pantsSlot;
    public EquipmentSlot shoesSlot;
    public EquipmentSlot[] runeSlots = new EquipmentSlot[3]; // 3 rune slots
    private bool isProcessingEquip = false;

    #endregion

    #region System References
    [Header("System References")]
    public InventoryGridManager inventoryGrid;
    public EquipmentManager equipmentManager;
    #endregion

    #region Current State
    [Header("Current State")]
    public List<EquipmentSlot> allSlots = new List<EquipmentSlot>();
    public EquipmentSlot currentSelectedSlot;
    public bool isVisible = false;
    #endregion

    #region Unity Lifecycle (เรียบง่ายเหมือน ItemDetailPanel)
    void Awake()
    {
        InitializeSlotsList();
        SetupSlots();
    }

    void Start()
    {
        SubscribeToEvents();
        HideEquipmentSlots(); // เริ่มต้นด้วยการซ่อน
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    #endregion

    #region Show/Hide Methods (เหมือน ItemDetailPanel)
    public void ShowEquipmentSlots()
    {
        Debug.Log("🎽 ShowEquipmentSlots called");

        if (equipmentSlotsPanel != null)
        {
            equipmentSlotsPanel.SetActive(true);
            isVisible = true;
            Debug.Log("✅ Equipment slots panel activated");
        }
        else
        {
            Debug.LogError("❌ equipmentSlotsPanel GameObject is null!");
        }
    }

    public void HideEquipmentSlots()
    {
        if (equipmentSlotsPanel != null)
        {
            equipmentSlotsPanel.SetActive(false);
            isVisible = false;
            Debug.Log("📋 Equipment slots panel hidden");
        }
    }

    public bool IsVisible()
    {
        return isVisible;
    }
    #endregion

    #region Initialization (เรียบง่าย ไม่มี complex loops)
    void InitializeSlotsList()
    {
        allSlots.Clear();

        // เพิ่ม slots หลัก
        if (weaponSlot != null) allSlots.Add(weaponSlot);
        if (headSlot != null) allSlots.Add(headSlot);
        if (armorSlot != null) allSlots.Add(armorSlot);
        if (pantsSlot != null) allSlots.Add(pantsSlot);
        if (shoesSlot != null) allSlots.Add(shoesSlot);

        // เพิ่ม rune slots
        foreach (var runeSlot in runeSlots)
        {
            if (runeSlot != null) allSlots.Add(runeSlot);
        }

        Debug.Log($"🎽 Initialized {allSlots.Count} equipment slots");
    }

    void SetupSlots()
    {
        // ตั้งค่า slot types เฉยๆ ไม่ยุ่งกับ GameObject
        if (weaponSlot != null)
        {
            weaponSlot.slotType = ItemType.Weapon;
            weaponSlot.slotName = "Weapon";
        }
        if (headSlot != null)
        {
            headSlot.slotType = ItemType.Head;
            headSlot.slotName = "Head";
        }
        if (armorSlot != null)
        {
            armorSlot.slotType = ItemType.Armor;
            armorSlot.slotName = "Armor";
        }
        if (pantsSlot != null)
        {
            pantsSlot.slotType = ItemType.Pants;
            pantsSlot.slotName = "Pants";
        }
        if (shoesSlot != null)
        {
            shoesSlot.slotType = ItemType.Shoes;
            shoesSlot.slotName = "Shoes";
        }

        // ตั้งค่า rune slots
        for (int i = 0; i < runeSlots.Length; i++)
        {
            if (runeSlots[i] != null)
            {
                runeSlots[i].slotType = ItemType.Rune;
                runeSlots[i].slotName = $"Rune{i + 1}";
            }
        }

        // หา references ถ้าไม่ได้ assign
        if (inventoryGrid == null)
            inventoryGrid = FindObjectOfType<InventoryGridManager>();

        if (equipmentManager == null)
            equipmentManager = FindObjectOfType<EquipmentManager>();

        Debug.Log("🔧 Equipment slots setup completed");
    }
    #endregion

    #region Event Management (เหมือน ItemDetailPanel)
    void SubscribeToEvents()
    {
        // Subscribe to slot events
        EquipmentSlot.OnEquipmentSlotSelected += HandleSlotSelected;
        EquipmentSlot.OnEquipmentChanged += HandleEquipmentChanged;

        // Subscribe to item detail panel events
        ItemDetailPanel.OnEquipRequested += HandleEquipRequest;

        Debug.Log("✅ Equipment slots events subscribed");
    }

    void UnsubscribeFromEvents()
    {
        EquipmentSlot.OnEquipmentSlotSelected -= HandleSlotSelected;
        EquipmentSlot.OnEquipmentChanged -= HandleEquipmentChanged;
        ItemDetailPanel.OnEquipRequested -= HandleEquipRequest;
    }

    void HandleSlotSelected(EquipmentSlot slot)
    {
        // ยกเลิกการเลือก slot เก่า
        if (currentSelectedSlot != null)
            currentSelectedSlot.SetSelectedState(false);

        // เลือก slot ใหม่
        currentSelectedSlot = slot;
        currentSelectedSlot.SetSelectedState(true);

        Debug.Log($"🎯 Equipment slot selected: {slot.slotName}");

        // ถ้า slot มี item แสดง detail panel
        if (slot.HasEquippedItem())
        {
            ShowEquippedItemDetail(slot);
        }
    }

    void HandleEquipmentChanged(EquipmentSlot slot, ItemData item)
    {
        Debug.Log($"🔄 Equipment changed in {slot.slotName}: {item?.ItemName ?? "Empty"}");

        // ✅ เพิ่ม: แจ้งให้ระบบอื่นรู้ทันที
        OnEquipmentChanged?.Invoke();

        // ✅ เพิ่ม: Force refresh panel ให้แน่ใจว่า slot ยังคงแสดงอยู่
        if (equipmentSlotsPanel != null && !equipmentSlotsPanel.activeSelf)
        {
            equipmentSlotsPanel.SetActive(true);
            Debug.Log("🔄 Re-activated equipment slots panel after change");
        }

        // อัปเดต character stats
        UpdateCharacterStats();
    }
    #endregion

    #region Equipment Operations
    void HandleEquipRequest(ItemData item, int inventorySlotIndex)
    {
        if (item == null) return;

        // ✅ ป้องกัน double processing
        if (isProcessingEquip)
        {
            Debug.LogWarning($"⚠️ Already processing equip request, ignoring duplicate for {item.ItemName}");
            return;
        }

        isProcessingEquip = true;

        Debug.Log($"🎽 HandleEquipRequest: {item.ItemName} ({item.ItemType}) from slot {inventorySlotIndex}");

        try
        {
            // หา slot ที่เหมาะสมสำหรับ item นี้
            EquipmentSlot targetSlot = GetSlotForItemType(item.ItemType);

            if (targetSlot == null)
            {
                Debug.LogError($"❌ No slot found for item type: {item.ItemType}");
                return;
            }

            // ✅ Double check ว่า slot ยังว่างอยู่
            if (!targetSlot.isEmpty || targetSlot.HasEquippedItem())
            {
                Debug.LogWarning($"❌ Cannot equip {item.ItemName}: {targetSlot.slotName} slot is already occupied by {targetSlot.GetEquippedItem()?.ItemName}");
                return;
            }

            // Equip item
            bool equipped = EquipItem(item, targetSlot);
            if (equipped)
            {
                // ลบ item จาก inventory
                RemoveItemFromInventory(inventorySlotIndex);

                // แจ้งให้ระบบรู้
                OnItemEquipped?.Invoke(item, item.ItemType);

                Debug.Log($"✅ Successfully equipped {item.ItemName} in {targetSlot.slotName}");
            }
            else
            {
                Debug.LogError($"❌ Failed to equip {item.ItemName}");
            }
        }
        finally
        {
            // ✅ Reset flag ไม่ว่าจะสำเร็จหรือไม่
            isProcessingEquip = false;
        }
    }

    void HandleUnequipRequest(ItemData item, int slotIndex)
    {
        if (item == null) return;

        Debug.Log($"🔧 HandleUnequipRequest: {item.ItemName}");

        // ✅ ลองหา slot ด้วยวิธีอื่นก่อน
        EquipmentSlot equipSlot = null;

        // วิธีที่ 1: ใช้ FindSlotWithItem
        equipSlot = FindSlotWithItem(item);

        // วิธีที่ 2: ถ้าไม่เจอ ลองดูทุก slot ที่มี item
        if (equipSlot == null)
        {
            Debug.Log("🔍 Method 1 failed, trying method 2...");
            foreach (var slot in allSlots)
            {
                if (slot != null && slot.HasEquippedItem())
                {
                    var equippedItem = slot.GetEquippedItem();
                    if (equippedItem != null && equippedItem.ItemName == item.ItemName)
                    {
                        equipSlot = slot;
                        Debug.Log($"✅ Found {item.ItemName} in {slot.slotName} using method 2");
                        break;
                    }
                }
            }
        }

        // วิธีที่ 3: ถ้ายังไม่เจอ ลองหาจาก ItemType
        if (equipSlot == null)
        {
            Debug.Log("🔍 Method 2 failed, trying method 3...");
            equipSlot = GetSlotForItemType(item.ItemType);
            if (equipSlot != null && equipSlot.HasEquippedItem())
            {
                Debug.Log($"✅ Found slot by ItemType: {equipSlot.slotName}");
            }
            else
            {
                equipSlot = null;
            }
        }

        if (equipSlot == null)
        {
            Debug.LogError($"❌ Cannot find {item.ItemName} in any equipment slot");
            DebugAllEquippedItems(); // Debug ทุก slot
            return;
        }

        // Unequip item
        bool unequipped = UnequipItem(equipSlot);
        if (unequipped)
        {
            // เพิ่ม item กลับไป inventory
            AddItemToInventory(item);

            // แจ้งให้ระบบรู้
            OnItemUnequipped?.Invoke(item, item.ItemType);

            Debug.Log($"✅ Successfully unequipped {item.ItemName}");
        }
    }

    public bool EquipItem(ItemData item, EquipmentSlot targetSlot = null)
    {
        if (item == null) return false;

        // หา slot ถ้าไม่ได้ระบุ
        if (targetSlot == null)
        {
            targetSlot = GetSlotForItemType(item.ItemType);
        }

        if (targetSlot == null)
        {
            Debug.LogError($"❌ No available slot for {item.ItemName}");
            return false;
        }

        // ✅ Final check ก่อน equip
        if (!targetSlot.isEmpty || targetSlot.HasEquippedItem())
        {
            Debug.LogError($"❌ Target slot {targetSlot.slotName} is not empty! Contains: {targetSlot.GetEquippedItem()?.ItemName}");
            return false;
        }

        // ลอง equip
        bool success = targetSlot.TryEquipItem(item);

        if (success)
        {
            // อัปเดต EquipmentManager เดิม
            UpdateEquipmentManager(item, true);
            Debug.Log($"✅ Successfully equipped {item.ItemName} in {targetSlot.slotName}");
        }
        else
        {
            Debug.LogError($"❌ TryEquipItem failed for {item.ItemName} in {targetSlot.slotName}");
        }

        return success;
    }

    public bool UnequipItem(EquipmentSlot slot)
    {
        if (slot == null || slot.isEmpty) return false;

        ItemData item = slot.GetEquippedItem();
        ItemData unequippedItem = slot.UnequipItem();

        if (unequippedItem != null)
        {
            // อัปเดต EquipmentManager เดิม
            UpdateEquipmentManager(unequippedItem, false);
            return true;
        }

        return false;
    }
    #endregion

    #region Slot Management
    public EquipmentSlot GetSlotForItemType(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Weapon: return weaponSlot;
            case ItemType.Head: return headSlot;
            case ItemType.Armor: return armorSlot;
            case ItemType.Pants: return pantsSlot;
            case ItemType.Shoes: return shoesSlot;
            case ItemType.Rune: return GetAvailableRuneSlot();
            default: return null;
        }
    }

    public EquipmentSlot GetAvailableRuneSlot()
    {
        Debug.Log("🔍 Looking for available rune slot...");

        // หา rune slot ว่างแรก
        for (int i = 0; i < runeSlots.Length; i++)
        {
            var runeSlot = runeSlots[i];
            if (runeSlot != null)
            {
                bool isEmpty = runeSlot.isEmpty;
                bool hasItem = runeSlot.HasEquippedItem();

                Debug.Log($"   Rune{i + 1}: isEmpty={isEmpty}, hasItem={hasItem}, item={runeSlot.GetEquippedItem()?.ItemName ?? "None"}");

                // ✅ ตรวจสอบทั้ง isEmpty และ HasEquippedItem
                if (isEmpty && !hasItem)
                {
                    Debug.Log($"✅ Found available rune slot: Rune{i + 1}");
                    return runeSlot;
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ Rune slot {i} is null!");
            }
        }

        Debug.LogWarning("❌ No available rune slots found!");
        return null; // ไม่มี slot ว่าง
    }


    public EquipmentSlot FindSlotWithItem(ItemData item)
    {
        Debug.Log($"🔍 FindSlotWithItem: Looking for {item.ItemName} (ID: {item.ItemId})");

        foreach (var slot in allSlots)
        {
            if (slot != null && slot.HasEquippedItem())
            {
                var equippedItem = slot.GetEquippedItem();
                Debug.Log($"   Checking slot {slot.slotName}: {equippedItem?.ItemName} (ID: {equippedItem?.ItemId})");

                // ✅ เปรียบเทียบทั้ง ItemId และ ItemName เผื่อ ItemId ไม่ตรง
                if (equippedItem != null &&
                    (equippedItem.ItemId == item.ItemId || equippedItem.ItemName == item.ItemName))
                {
                    Debug.Log($"✅ Found {item.ItemName} in {slot.slotName}");
                    return slot;
                }
            }
        }

        Debug.LogError($"❌ Item {item.ItemName} not found in any slot!");
        return null;
    }
    public bool IsItemEquipped(ItemData item)
    {
        return FindSlotWithItem(item) != null;
    }

    public ItemData GetEquippedItem(ItemType itemType)
    {
        EquipmentSlot slot = GetSlotForItemType(itemType);
        return slot?.GetEquippedItem();
    }
    #endregion

    #region Integration Methods
    void UpdateEquipmentManager(ItemData item, bool equipping)
    {
        if (equipmentManager == null) return;

        // สร้าง EquipmentData จาก ItemData
        EquipmentData equipmentData = new EquipmentData
        {
            itemName = item.ItemName,
            stats = item.Stats.ToEquipmentStats()
        };

        if (equipping)
        {
            equipmentManager.EquipItem(equipmentData);
        }
        else
        {
            equipmentManager.UnequipItem();
        }

        Debug.Log($"🔄 Updated EquipmentManager: {item.ItemName} ({(equipping ? "equipped" : "unequipped")})");
    }

    void UpdateCharacterStats()
    {
        Debug.Log("📊 Character stats updated based on equipment");
    }

    void RemoveItemFromInventory(int slotIndex)
    {
        if (inventoryGrid != null)
        {
            inventoryGrid.RemoveItem(slotIndex);
        }
    }

    void AddItemToInventory(ItemData item)
    {
        Debug.Log($"🔄 Trying to add {item.ItemName} back to inventory");

        if (inventoryGrid != null)
        {
            bool added = inventoryGrid.AddItem(item);
            if (added)
            {
                Debug.Log($"✅ Successfully returned {item.ItemName} to inventory");
            }
            else
            {
                Debug.LogWarning($"⚠️ Inventory full! Cannot return {item.ItemName} to inventory");
                // TODO: Handle inventory full case - maybe drop on ground or show message
            }
        }
        else
        {
            Debug.LogError("❌ InventoryGrid is null! Cannot return item to inventory");

            // ✅ เพิ่ม: หา InventoryGrid ใหม่ถ้าหายไป
            inventoryGrid = FindObjectOfType<InventoryGridManager>();
            if (inventoryGrid != null)
            {
                Debug.Log("🔄 Found InventoryGrid, retrying...");
                bool added = inventoryGrid.AddItem(item);
                if (added)
                {
                    Debug.Log($"✅ Successfully returned {item.ItemName} to inventory (retry)");
                }
            }
        }
    }

    void ShowEquippedItemDetail(EquipmentSlot slot)
    {
        // แสดง item detail panel สำหรับ equipped item
        var inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager != null && slot.HasEquippedItem())
        {
            inventoryManager.ShowItemDetail(slot.GetEquippedItem(), -1); // -1 = equipped item
        }
    }
    #endregion

    #region Debug Methods (เหมือน ItemDetailPanel)
    [ContextMenu("Debug Equipment Slots")]
    public void DebugEquipmentSlots()
    {
        Debug.Log($"🎽 Equipment Slots Debug:");
        Debug.Log($"   equipmentSlotsPanel: {(equipmentSlotsPanel != null ? "✅ Assigned" : "❌ NULL")}");
        Debug.Log($"   isVisible: {isVisible}");
        Debug.Log($"   Total Slots: {allSlots.Count}");

        if (equipmentSlotsPanel != null)
            Debug.Log($"   Panel Active: {equipmentSlotsPanel.activeSelf}");

        // Debug แต่ละ slot
        foreach (var slot in allSlots)
        {
            if (slot != null)
            {
                string itemInfo = slot.HasEquippedItem() ? slot.GetEquippedItem().ItemName : "Empty";
                Debug.Log($"   - {slot.slotName}: {itemInfo}");
            }
        }
    }

    [ContextMenu("Test - Show Equipment Slots")]
    public void TestShowEquipmentSlots()
    {
        ShowEquipmentSlots();
    }

    [ContextMenu("Test - Hide Equipment Slots")]
    public void TestHideEquipmentSlots()
    {
        HideEquipmentSlots();
    }

    [ContextMenu("Test - Equip Random Items")]
    public void TestEquipRandomItems()
    {
        if (ItemDatabase.Instance == null) return;

        foreach (var slot in allSlots)
        {
            if (slot != null && slot.isEmpty)
            {
                var itemsOfType = ItemDatabase.Instance.GetItemsByType(slot.slotType);
                if (itemsOfType.Count > 0)
                {
                    ItemData randomItem = itemsOfType[UnityEngine.Random.Range(0, itemsOfType.Count)];
                    slot.TryEquipItem(randomItem);
                }
            }
        }

        Debug.Log("🎲 Equipped random items to all empty slots");
    }
    #endregion
    [ContextMenu("Test - Debug Unequip Issue")]
    public void TestDebugUnequipIssue()
    {
        Debug.Log("🔍 === UNEQUIP DEBUG ===");
        Debug.Log($"Equipment Slots Panel: {(equipmentSlotsPanel != null ? equipmentSlotsPanel.activeSelf.ToString() : "NULL")}");
        Debug.Log($"Inventory Grid: {(inventoryGrid != null ? "Found" : "NULL")}");

        if (inventoryGrid != null)
        {
            Debug.Log($"Inventory Empty Slots: {inventoryGrid.GetEmptySlotCount()}");
            Debug.Log($"Inventory Filled Slots: {inventoryGrid.GetFilledSlotCount()}");
        }

        foreach (var slot in allSlots)
        {
            if (slot != null)
            {
                Debug.Log($"Slot {slot.slotName}: Active={slot.gameObject.activeSelf}, HasItem={slot.HasEquippedItem()}");
            }
        }
        Debug.Log("🔍 === END UNEQUIP DEBUG ===");
    }

    [ContextMenu("Debug All Equipped Items")]
    public void DebugAllEquippedItems()
    {
        Debug.Log("🔍 === ALL EQUIPPED ITEMS ===");
        foreach (var slot in allSlots)
        {
            if (slot != null)
            {
                if (slot.HasEquippedItem())
                {
                    var item = slot.GetEquippedItem();
                    Debug.Log($"✅ {slot.slotName}: {item?.ItemName} (ID: {item?.ItemId})");
                }
                else
                {
                    Debug.Log($"⚪ {slot.slotName}: Empty");
                }
            }
            else
            {
                Debug.Log($"❌ NULL SLOT!");
            }
        }
        Debug.Log("🔍 === END EQUIPPED ITEMS ===");
    }
}