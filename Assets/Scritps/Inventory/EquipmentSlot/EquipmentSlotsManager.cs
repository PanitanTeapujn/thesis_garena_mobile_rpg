using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;

public class EquipmentSlotsManager : MonoBehaviour
{
    #region Events
    public static event Action<ItemData, ItemType> OnItemEquipped;
    public static event Action<ItemData, ItemType> OnItemUnequipped;
    public static event Action OnEquipmentChanged;
    #endregion

    #region UI References
    [Header("Equipment Slots Panel")]
    public GameObject equipmentSlotsPanel;

    [Header("Equipment Slots")]
    public EquipmentSlot weaponSlot;
    public EquipmentSlot headSlot;
    public EquipmentSlot armorSlot;
    public EquipmentSlot pantsSlot;
    public EquipmentSlot shoesSlot;
    public EquipmentSlot[] runeSlots = new EquipmentSlot[3]; // 3 rune slots
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

    // ✅ เพิ่ม: Sequential processing flags
    private bool isProcessingEquipment = false;
    private Dictionary<ItemData, EquipmentSlot> itemToSlotMap = new Dictionary<ItemData, EquipmentSlot>();
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        InitializeSlotsList();
        SetupSlots();
    }

    void Start()
    {
        SubscribeToEvents();
        HideEquipmentSlots();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    #endregion

    #region Show/Hide Methods
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

    #region Initialization
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
        // ตั้งค่า slot types
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

    #region Event Management
    void SubscribeToEvents()
    {
        EquipmentSlot.OnEquipmentSlotSelected += HandleSlotSelected;
        EquipmentSlot.OnEquipmentChanged += HandleEquipmentChanged;
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
        if (currentSelectedSlot != null)
            currentSelectedSlot.SetSelectedState(false);

        currentSelectedSlot = slot;
        currentSelectedSlot.SetSelectedState(true);

        Debug.Log($"🎯 Equipment slot selected: {slot.slotName}");

        if (slot.HasEquippedItem())
        {
            ShowEquippedItemDetail(slot);
        }
    }

    void HandleEquipmentChanged(EquipmentSlot slot, ItemData item)
    {
        Debug.Log($"🔄 Equipment changed in {slot.slotName}: {item?.ItemName ?? "Empty"}");

        OnEquipmentChanged?.Invoke();

        if (equipmentSlotsPanel != null && !equipmentSlotsPanel.activeSelf)
        {
            equipmentSlotsPanel.SetActive(true);
            Debug.Log("🔄 Re-activated equipment slots panel after change");
        }

        UpdateCharacterStats();
    }
    #endregion

    #region Equipment Operations - ✅ ปรับปรุงเพื่อป้องกัน rune duplication
    void HandleEquipRequest(ItemData item, int inventorySlotIndex)
    {
        if (item == null) return;

        // ✅ ป้องกัน concurrent processing
        if (isProcessingEquipment)
        {
            Debug.LogWarning($"⚠️ Equipment system busy, queuing {item.ItemName}...");
            StartCoroutine(WaitAndRetryEquip(item, inventorySlotIndex));
            return;
        }

        StartCoroutine(ProcessEquipRequest(item, inventorySlotIndex));
    }

    private IEnumerator WaitAndRetryEquip(ItemData item, int inventorySlotIndex)
    {
        // รอให้ระบบว่าง
        while (isProcessingEquipment)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // ลองอีกครั้ง
        HandleEquipRequest(item, inventorySlotIndex);
    }

    private IEnumerator ProcessEquipRequest(ItemData item, int inventorySlotIndex)
    {
        isProcessingEquipment = true;

        Debug.Log($"🎽 Processing equip request: {item.ItemName} ({item.ItemType}) from slot {inventorySlotIndex}");

        // ✅ สำหรับ rune: เช็ค duplication ก่อน
        if (item.ItemType == ItemType.Rune)
        {
            if (IsRuneIdAlreadyEquipped(item.ItemId))
            {
                Debug.LogError($"❌ Cannot equip: Rune {item.ItemName} (ID: {item.ItemId}) is already equipped!");
                isProcessingEquipment = false;
                yield break;
            }
        }

        // หา target slot
        EquipmentSlot targetSlot = null;

        if (item.ItemType == ItemType.Rune)
        {
            targetSlot = GetNextAvailableRuneSlot(); // ✅ ใช้ sequential method ใหม่
        }
        else
        {
            targetSlot = GetSlotForItemType(item.ItemType);
        }

        if (targetSlot == null)
        {
            Debug.LogError($"❌ No available slot for {item.ItemType}");
            isProcessingEquipment = false;
            yield break;
        }

        // ✅ Validate slot availability อีกครั้ง
        if (!IsSlotTrulyEmpty(targetSlot))
        {
            Debug.LogWarning($"❌ Target slot {targetSlot.slotName} is not empty!");
            isProcessingEquipment = false;
            yield break;
        }

        // รอ frame เดียวเพื่อให้ UI update
        yield return null;

        // ลอง equip
        bool success = AtomicEquipItem(item, targetSlot);

        if (success)
        {
            // ลบจาก inventory
            RemoveItemFromInventory(inventorySlotIndex);

            // แจ้งให้ระบบรู้
            OnItemEquipped?.Invoke(item, item.ItemType);

            Debug.Log($"✅ Successfully equipped {item.ItemName} in {targetSlot.slotName}");
        }
        else
        {
            Debug.LogError($"❌ Failed to equip {item.ItemName}");
        }

        // ✅ รออีก frame ก่อนปลดล็อก
        yield return null;
        isProcessingEquipment = false;
    }

    // ✅ ใหม่: Method สำหรับเช็ค rune ID duplication
    public bool IsRuneIdAlreadyEquipped(string runeId)
    {
        Debug.Log($"🔍 Checking rune ID '{runeId}' duplication...");

        for (int i = 0; i < runeSlots.Length; i++)
        {
            var runeSlot = runeSlots[i];
            if (runeSlot != null && runeSlot.HasEquippedItem())
            {
                var equippedRune = runeSlot.GetEquippedItem();
                if (equippedRune != null && equippedRune.ItemId == runeId)
                {
                    Debug.Log($"❌ Found duplicate: Rune ID '{runeId}' in {runeSlot.slotName}");
                    return true;
                }
            }
        }

        Debug.Log($"✅ No duplicate found for rune ID '{runeId}'");
        return false;
    }

    // ✅ ใหม่: Sequential rune slot assignment
    private EquipmentSlot GetNextAvailableRuneSlot()
    {
        Debug.Log("🔍 Finding next available rune slot sequentially...");

        // ใช้ sequential assignment: Rune1 → Rune2 → Rune3
        for (int i = 0; i < runeSlots.Length; i++)
        {
            var slot = runeSlots[i];
            if (slot != null && IsSlotTrulyEmpty(slot))
            {
                Debug.Log($"✅ Found available rune slot: {slot.slotName}");
                return slot;
            }
            else if (slot != null)
            {
                Debug.Log($"   {slot.slotName}: occupied by {slot.GetEquippedItem()?.ItemName ?? "Unknown"}");
            }
        }

        Debug.LogWarning("❌ No available rune slots found!");
        return null;
    }

    // ✅ ใหม่: Comprehensive slot validation
    private bool IsSlotTrulyEmpty(EquipmentSlot slot)
    {
        if (slot == null) return false;

        bool isEmpty = slot.isEmpty;
        bool hasItem = slot.HasEquippedItem();
        var equippedItem = slot.GetEquippedItem();

        // ทุกเงื่อนไขต้องเป็น "ว่าง"
        bool isReallyEmpty = isEmpty && !hasItem && equippedItem == null;

        if (!isReallyEmpty)
        {
            Debug.Log($"🔍 {slot.slotName} not empty: isEmpty={isEmpty}, hasItem={hasItem}, item={equippedItem?.ItemName ?? "null"}");
        }

        return isReallyEmpty;
    }

    // ✅ ใหม่: Atomic equip operation
    private bool AtomicEquipItem(ItemData item, EquipmentSlot targetSlot)
    {
        // Double-check before atomic operation
        if (!IsSlotTrulyEmpty(targetSlot))
        {
            Debug.LogError($"❌ Atomic equip failed: {targetSlot.slotName} not empty");
            return false;
        }

        // ✅ สำหรับ rune: เช็ค duplication อีกครั้งก่อน equip
        if (item.ItemType == ItemType.Rune && IsRuneIdAlreadyEquipped(item.ItemId))
        {
            Debug.LogError($"❌ Atomic equip failed: Rune ID '{item.ItemId}' duplicate detected");
            return false;
        }

        // Perform atomic equip
        bool success = targetSlot.TryEquipItem(item);

        if (success)
        {
            // Update mapping
            itemToSlotMap[item] = targetSlot;

            // Update equipment manager
            UpdateEquipmentManager(item, true);

            Debug.Log($"✅ Atomic equip successful: {item.ItemName} → {targetSlot.slotName}");
        }
        else
        {
            Debug.LogError($"❌ Atomic equip failed: {item.ItemName} → {targetSlot.slotName}");
        }

        return success;
    }

    public bool EquipItem(ItemData item, EquipmentSlot targetSlot = null)
    {
        if (item == null) return false;

        // ✅ เช็ค rune duplication
        if (item.ItemType == ItemType.Rune && IsRuneIdAlreadyEquipped(item.ItemId))
        {
            Debug.LogError($"❌ Cannot equip: Rune ID '{item.ItemId}' already equipped");
            return false;
        }

        if (targetSlot == null)
        {
            targetSlot = GetSlotForItemType(item.ItemType);
        }

        if (targetSlot == null)
        {
            Debug.LogError($"❌ No available slot for {item.ItemName}");
            return false;
        }

        return AtomicEquipItem(item, targetSlot);
    }

    public bool UnequipItem(EquipmentSlot slot)
    {
        if (slot == null || slot.isEmpty) return false;

        ItemData item = slot.GetEquippedItem();
        ItemData unequippedItem = slot.UnequipItem();

        if (unequippedItem != null)
        {
            // ลบออกจาก mapping
            if (itemToSlotMap.ContainsKey(unequippedItem))
            {
                itemToSlotMap.Remove(unequippedItem);
                Debug.Log($"🔄 Removed {unequippedItem.ItemName} from slot mapping");
            }

            // อัปเดต EquipmentManager เดิม
            UpdateEquipmentManager(unequippedItem, false);
            return true;
        }

        return false;
    }

    void HandleUnequipRequest(ItemData item, int slotIndex)
    {
        if (item == null) return;

        Debug.Log($"🔧 HandleUnequipRequest: {item.ItemName}");

        EquipmentSlot equipSlot = FindSlotWithItem(item);

        if (equipSlot == null)
        {
            Debug.LogError($"❌ Cannot find {item.ItemName} in any equipment slot");
            DebugAllEquippedItems();
            return;
        }

        bool unequipped = UnequipItem(equipSlot);
        if (unequipped)
        {
            AddItemToInventory(item);
            OnItemUnequipped?.Invoke(item, item.ItemType);
            Debug.Log($"✅ Successfully unequipped {item.ItemName}");
        }
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
            case ItemType.Rune: return GetNextAvailableRuneSlot(); // ✅ ใช้ sequential method
            default: return null;
        }
    }

    // ✅ ปรับปรุง: FindSlotWithItem ให้รองรับ rune โดยใช้ ItemId
    public EquipmentSlot FindSlotWithItem(ItemData item)
    {
        Debug.Log($"🔍 FindSlotWithItem: Looking for {item.ItemName} (ID: {item.ItemId})");

        foreach (var slot in allSlots)
        {
            if (slot != null && slot.HasEquippedItem())
            {
                var equippedItem = slot.GetEquippedItem();

                if (equippedItem != null)
                {
                    // ✅ สำหรับ rune: ใช้ ReferenceEquals เท่านั้น (เพราะเราต้องการหา instance เดียวกันพอดี)
                    // สำหรับ equipment อื่น: ใช้ ItemId เป็นหลัก
                    bool isMatch = false;

                    if (item.ItemType == ItemType.Rune)
                    {
                        // สำหรับ rune: ใช้ ReferenceEquals เพื่อหา instance เดียวกันที่ต้องการ unequip
                        isMatch = ReferenceEquals(equippedItem, item);
                        Debug.Log($"   {slot.slotName}: Rune reference check = {isMatch}");
                    }
                    else
                    {
                        // สำหรับ equipment อื่น: ใช้ ItemId
                        isMatch = equippedItem.ItemId == item.ItemId;
                        Debug.Log($"   {slot.slotName}: ID match = {isMatch}");
                    }

                    if (isMatch)
                    {
                        Debug.Log($"✅ Found {item.ItemName} in {slot.slotName}");
                        return slot;
                    }
                }
            }
        }

        Debug.Log($"❌ {item.ItemName} not found in any slot");
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

    // ✅ ใหม่: Method สำหรับหา rune ที่มี ID เดียวกัน
    public EquipmentSlot FindSlotWithRuneId(string runeId)
    {
        Debug.Log($"🔍 FindSlotWithRuneId: Looking for rune ID '{runeId}'");

        for (int i = 0; i < runeSlots.Length; i++)
        {
            var slot = runeSlots[i];
            if (slot != null && slot.HasEquippedItem())
            {
                var equippedRune = slot.GetEquippedItem();
                if (equippedRune != null && equippedRune.ItemId == runeId)
                {
                    Debug.Log($"✅ Found rune ID '{runeId}' in {slot.slotName}");
                    return slot;
                }
            }
        }

        Debug.Log($"❌ Rune ID '{runeId}' not found");
        return null;
    }
    #endregion

    #region Integration Methods
    void UpdateEquipmentManager(ItemData item, bool equipping)
    {
        if (equipmentManager == null) return;

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
            }
        }
        else
        {
            Debug.LogError("❌ InventoryGrid is null! Cannot return item to inventory");

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
        var inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager != null && slot.HasEquippedItem())
        {
            inventoryManager.ShowItemDetail(slot.GetEquippedItem(), -1);
        }
    }
    #endregion

    #region Debug Methods
    [ContextMenu("Debug Equipment Slots")]
    public void DebugEquipmentSlots()
    {
        Debug.Log($"🎽 Equipment Slots Debug:");
        Debug.Log($"   equipmentSlotsPanel: {(equipmentSlotsPanel != null ? "✅ Assigned" : "❌ NULL")}");
        Debug.Log($"   isVisible: {isVisible}");
        Debug.Log($"   Total Slots: {allSlots.Count}");
        Debug.Log($"   Processing: {isProcessingEquipment}");

        foreach (var slot in allSlots)
        {
            if (slot != null)
            {
                string itemInfo = slot.HasEquippedItem() ? $"{slot.GetEquippedItem().ItemName} (ID: {slot.GetEquippedItem().ItemId})" : "Empty";
                Debug.Log($"   - {slot.slotName}: {itemInfo}");
            }
        }
    }

    [ContextMenu("Debug Rune Slots")]
    public void DebugRuneSlots()
    {
        Debug.Log("🔍 === RUNE SLOTS DEBUG ===");

        for (int i = 0; i < runeSlots.Length; i++)
        {
            var slot = runeSlots[i];
            if (slot != null)
            {
                var item = slot.GetEquippedItem();
                bool isEmpty = slot.isEmpty;
                bool hasItem = slot.HasEquippedItem();

                Debug.Log($"Rune{i + 1}: isEmpty={isEmpty}, hasItem={hasItem}, item={item?.ItemName ?? "None"}");
                if (item != null)
                {
                    Debug.Log($"   Item ID: {item.ItemId}");
                }
                Debug.Log($"   GameObject active: {slot.gameObject.activeSelf}");
                Debug.Log($"   Is truly empty: {IsSlotTrulyEmpty(slot)}");
            }
            else
            {
                Debug.Log($"Rune{i + 1}: NULL SLOT!");
            }
        }

        Debug.Log($"Item-to-Slot mappings: {itemToSlotMap.Count}");
        foreach (var mapping in itemToSlotMap)
        {
            Debug.Log($"   {mapping.Key?.ItemName} (ID: {mapping.Key?.ItemId}) → {mapping.Value?.slotName}");
        }

        Debug.Log("🔍 === END RUNE SLOTS DEBUG ===");
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

    // ✅ ใหม่: Debug method สำหรับ check rune duplication
    [ContextMenu("Debug - Check Rune Duplicates")]
    public void DebugCheckRuneDuplicates()
    {
        Debug.Log("🔍 === CHECKING RUNE DUPLICATES ===");

        var runeIds = new Dictionary<string, List<string>>();

        for (int i = 0; i < runeSlots.Length; i++)
        {
            var slot = runeSlots[i];
            if (slot != null && slot.HasEquippedItem())
            {
                var rune = slot.GetEquippedItem();
                if (rune != null)
                {
                    string runeId = rune.ItemId;
                    if (!runeIds.ContainsKey(runeId))
                    {
                        runeIds[runeId] = new List<string>();
                    }
                    runeIds[runeId].Add(slot.slotName);
                }
            }
        }

        Debug.Log("Rune ID Summary:");
        foreach (var kvp in runeIds)
        {
            string status = kvp.Value.Count > 1 ? "❌ DUPLICATE" : "✅ Unique";
            Debug.Log($"   ID '{kvp.Key}': {string.Join(", ", kvp.Value)} - {status}");
        }

        Debug.Log("🔍 === END DUPLICATE CHECK ===");
    }

    // ✅ Recovery methods
    [ContextMenu("Recovery - Clear All Rune Slots")]
    public void RecoveryClearAllRuneSlots()
    {
        Debug.Log("🔧 === RECOVERY: CLEARING ALL RUNE SLOTS ===");

        isProcessingEquipment = true;

        try
        {
            foreach (var runeSlot in runeSlots)
            {
                if (runeSlot != null)
                {
                    var item = runeSlot.GetEquippedItem();
                    if (item != null)
                    {
                        Debug.Log($"Returning {item.ItemName} to inventory...");
                        AddItemToInventory(item);
                    }

                    runeSlot.ForceSetEmptyState();
                }
            }

            // Clear mapping
            var itemsToRemove = new List<ItemData>();
            foreach (var mapping in itemToSlotMap)
            {
                if (mapping.Value != null && mapping.Value.slotType == ItemType.Rune)
                {
                    itemsToRemove.Add(mapping.Key);
                }
            }

            foreach (var item in itemsToRemove)
            {
                itemToSlotMap.Remove(item);
            }

            Debug.Log("✅ All rune slots cleared and items returned to inventory");
        }
        finally
        {
            isProcessingEquipment = false;
        }
    }

    [ContextMenu("Recovery - Fix All States")]
    public void RecoveryFixAllStates()
    {
        Debug.Log("🔧 === RECOVERY: FIXING ALL STATES ===");

        foreach (var slot in allSlots)
        {
            if (slot != null)
            {
                slot.ValidateAndFixState();
            }
        }

        Debug.Log("✅ All slot states fixed");
        DebugEquipmentSlots();
    }

    [ContextMenu("Recovery - Remove Duplicate Runes")]
    public void RecoveryRemoveDuplicateRunes()
    {
        Debug.Log("🔧 === RECOVERY: REMOVING DUPLICATE RUNES ===");

        var seenRuneIds = new HashSet<string>();

        for (int i = 0; i < runeSlots.Length; i++)
        {
            var slot = runeSlots[i];
            if (slot != null && slot.HasEquippedItem())
            {
                var rune = slot.GetEquippedItem();
                if (rune != null)
                {
                    if (seenRuneIds.Contains(rune.ItemId))
                    {
                        Debug.Log($"🔧 Removing duplicate rune: {rune.ItemName} (ID: {rune.ItemId}) from {slot.slotName}");
                        AddItemToInventory(rune);
                        slot.ForceSetEmptyState();

                        if (itemToSlotMap.ContainsKey(rune))
                        {
                            itemToSlotMap.Remove(rune);
                        }
                    }
                    else
                    {
                        seenRuneIds.Add(rune.ItemId);
                        Debug.Log($"✅ Keeping unique rune: {rune.ItemName} (ID: {rune.ItemId}) in {slot.slotName}");
                    }
                }
            }
        }

        Debug.Log("✅ Duplicate rune removal completed");
        DebugRuneSlots();
    }
    #endregion
}