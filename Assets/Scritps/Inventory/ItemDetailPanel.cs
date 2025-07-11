﻿using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemDetailPanel : MonoBehaviour
{
    [Header("UI References")]
    public Image itemIconImage;             // รูปไอเทม
    public TextMeshProUGUI itemNameText;    // ชื่อไอเทม
    public TextMeshProUGUI itemTypeText;    // ประเภทไอเทม
    public TextMeshProUGUI itemTierText;    // ระดับความหายาก
    public TextMeshProUGUI itemDescText;    // คำอธิบายไอเทม
    public TextMeshProUGUI itemStatsText;   // สถิติไอเทม
    public TextMeshProUGUI stackCountText;  // จำนวนไอเทม
    public Button closeButton;              // ปุ่มปิด panel
    public Image itemTierBackground;        // พื้นหลังแสดงสี tier ของ item icon
    [Header("🆕 Value Panel")]
    public Button showValueButton;          // ปุ่มแสดง value panel
    public TextMeshProUGUI showValueButtonText; // ข้อความในปุ่ม
    public ItemValuePanel itemValuePanel;   // Reference ไปยัง ItemValuePanel
    [Header("🆕 Equip/Unequip Buttons")]
    public Button equipButton;              // ปุ่ม Equip
    public Button unequipButton;            // ปุ่ม Unequip
    public TextMeshProUGUI equipButtonText; // ข้อความในปุ่ม Equip
    public TextMeshProUGUI unequipButtonText; // ข้อความในปุ่ม Unequip

    private InventoryItem currentItem;      // Item ที่กำลังแสดงอยู่
    private Character currentCharacter;     // Character ที่เป็นเจ้าของ item
    [SerializeField]
    private CombatUIManager combatUIManager;
    private void Awake()
    {
        // Setup close button
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HideItemDetail);
        }

        // Setup equip button
        if (equipButton != null)
        {
            equipButton.onClick.RemoveAllListeners();
            equipButton.onClick.AddListener(OnEquipButtonClicked);
        }

        // Setup unequip button
        if (unequipButton != null)
        {
            unequipButton.onClick.RemoveAllListeners();
            unequipButton.onClick.AddListener(OnUnequipButtonClicked);
        }
        if (showValueButton != null)
        {
            showValueButton.onClick.RemoveAllListeners();
            showValueButton.onClick.AddListener(OnShowValueButtonClicked);
        }
        combatUIManager = GetComponentInParent<CombatUIManager>();

        // 🆕 Setup tier background for itemIconImage (ใช้ background แทน outline)

        // ซ่อน panel ตั้งแต่เริ่มต้น
        HideItemDetail();
    }


    private void OnShowValueButtonClicked()
    {
        if (currentItem == null)
        {
            Debug.LogWarning("[ItemDetailPanel] No current item to show value");
            return;
        }

        // ปิด detail panel
        HideItemDetail();

        // เปิด value panel
        if (itemValuePanel != null)
        {
            itemValuePanel.ShowValuePanel(currentItem);
        }
        else
        {
            // หา ItemValuePanel ใน scene ถ้าไม่ได้ assign
            ItemValuePanel valuePanel = FindObjectOfType<ItemValuePanel>();
            if (valuePanel != null)
            {
                valuePanel.ShowValuePanel(currentItem);
            }
            else
            {
                Debug.LogWarning("[ItemDetailPanel] ItemValuePanel not found!");
            }
        }
    }
    public void ShowItemDetail(InventoryItem item)
    {
        if (item == null || item.IsEmpty)
        {
            HideItemDetail();
            return;
        }

        ItemData itemData = item.itemData;
        currentItem = item;

        // หา Character จาก CombatUIManager
        CombatUIManager uiManager = FindObjectOfType<CombatUIManager>();
        currentCharacter = uiManager?.localHero;

        // แสดง panel
        gameObject.SetActive(true);

        // แสดงรูปไอเทม
        if (itemIconImage != null)
        {
            itemIconImage.sprite = itemData.ItemIcon;
            itemIconImage.color = Color.white; // 🆕 ใช้สีขาวแทนการเปลี่ยนสี

            // 🆕 เซ็ต tier border แทนการเปลี่ยนสี
            SetItemIconTierBackground(itemData.GetTierColor());
        }

        // แสดงชื่อไอเทม
        if (itemNameText != null)
        {
            itemNameText.text = itemData.ItemName;
            itemNameText.color = itemData.GetTierColor(); // ชื่อยังคงใช้สี tier
        }

        // แสดงประเภทไอเทม
        if (itemTypeText != null)
        {
            itemTypeText.text = $"Type: {itemData.ItemType}";
        }

        // แสดงระดับความหายาก
        if (itemTierText != null)
        {
            itemTierText.text = itemData.GetTierText();
            itemTierText.color = itemData.GetTierColor();
        }

        // แสดงคำอธิบาย
        if (itemDescText != null)
        {
            itemDescText.text = string.IsNullOrEmpty(itemData.Description) ?
                "" : itemData.Description;
        }

        // แสดงสถิติ
        if (itemStatsText != null)
        {
            string statsText = itemData.Stats.GetStatsDescription();
            itemStatsText.text = string.IsNullOrEmpty(statsText) ?
                "": statsText;
        }

        // แสดงจำนวนไอเทม (สำหรับไอเทมที่ stack ได้)
        if (stackCountText != null)
        {
            if (item.stackCount > 1)
            {
                stackCountText.text = $"{item.stackCount}x";
                stackCountText.gameObject.SetActive(true);
            }
            else
            {
                stackCountText.gameObject.SetActive(false);
            }
        }
        if (itemDescText != null)
        {
            string descText = string.IsNullOrEmpty(itemData.Description) ?
                "" : itemData.Description;

            // เพิ่มข้อมูลราคาถ้ามี
            

            itemDescText.text = descText;
        }


        // จัดการปุ่ม Equip/Unequip
        UpdateEquipButtons(itemData);

        Debug.Log($"[ItemDetailPanel] Showing details for: {itemData.ItemName}");
    }
    private void SetItemIconTierBackground(Color tierColor)
    {
        if (itemTierBackground != null)
        {
            itemTierBackground.color = tierColor;
            itemTierBackground.enabled = true;
            Debug.Log($"[ItemDetailPanel] Set item icon tier background to {tierColor}");
        }
        else
        {
            Debug.LogWarning("[ItemDetailPanel] itemTierBackground is null! Please assign it in Inspector");
        }
    }

    // 🆕 method สำหรับปิด tier background
    private void DisableItemIconTierBackground()
    {
        if (itemTierBackground != null)
        {
            itemTierBackground.enabled = false;
            Debug.Log("[ItemDetailPanel] Disabled item icon tier background");
        }
    }

   
    // 🆕 method สำหรับปิด tier border
   

    public void HideItemDetail()
    {
        DisableItemIconTierBackground();

        gameObject.SetActive(false);
        Debug.Log("[ItemDetailPanel] Item detail panel hidden");
    }

    public bool IsVisible()
    {
        return gameObject.activeSelf;
    }

    // 🆕 จัดการการแสดงปุ่ม Equip/Unequip
    

    // 🆕 ตรวจสอบว่า item สามารถ equip ได้หรือไม่
    private bool CanEquipItem(ItemData itemData)
    {
        if (currentCharacter?.GetComponent<EquipmentManager>() == null)
            return false;

        // ตรวจสอบว่า item นี้ไม่ได้ equipped อยู่แล้ว
        EquipmentManager equipManager = currentCharacter.GetComponent<EquipmentManager>();
        // TODO: ต้องเพิ่ม method ใน EquipmentManager เพื่อตรวจสอบ equipped items
        // return !equipManager.IsItemEquipped(itemData);

        return true; // ชั่วคราวให้ equip ได้เสมอ
    }

    // 🆕 ตรวจสอบว่า item เป็นของที่สามารถ equip ได้หรือไม่
    private bool IsEquippableItem(ItemData itemData)
    {
        switch (itemData.ItemType)
        {
            case ItemType.Weapon:
            case ItemType.Head:
            case ItemType.Armor:
            case ItemType.Pants:
            case ItemType.Shoes:
                return true;
            case ItemType.Rune:
            case ItemType.Potion:
                return false;
            default:
                return false;
        }
    }

    // 🆕 เมื่อกดปุ่ม Equip
    private void OnEquipButtonClicked()
    {
        if (currentItem == null || currentCharacter == null)
        {
            Debug.LogWarning("[ItemDetailPanel] Cannot equip: missing item or character");
            return;
        }

        ItemType itemType = currentItem.itemData.ItemType;
        Debug.Log($"[ItemDetailPanel] Trying to equip: {currentItem.itemData.ItemName} ({itemType})");

        // แยก logic สำหรับ potion และ equipment
        EquipmentSlot targetSlot = null;

        if (itemType == ItemType.Potion)
        {
            // สำหรับ potion: หา empty potion slot
            targetSlot = FindEmptyPotionSlot();
            if (targetSlot == null)
            {
                Debug.LogWarning("[ItemDetailPanel] All potion slots are full!");
                ShowMessage("All potion slots are full!");
                return;
            }
        }
        else
        {
            // สำหรับ equipment: หา slot ที่ตรงกับ type
            targetSlot = FindEquipmentSlotByType(itemType);
            if (targetSlot == null)
            {
                Debug.LogWarning($"[ItemDetailPanel] No slot found for ItemType: {itemType}");
                return;
            }
        }

        Debug.Log($"[ItemDetailPanel] Found target slot: {targetSlot.SlotType} (Potion Index: {targetSlot.PotionSlotIndex})");

        // สำหรับ potion: ใส่ทั้ง stack
        if (itemType == ItemType.Potion)
        {
            bool success = EquipFullPotionStack();
            if (success)
            {
                HideItemDetail();
            }
        }
        else
        {
            // สำหรับ equipment: ใช้วิธีเดิม
            bool equipSuccess = currentCharacter.EquipItemData(currentItem.itemData);

            if (equipSuccess)
            {
                // 🆕 เปลี่ยนจาก SetFilledState ให้ใช้ SetTierBorder
                targetSlot.SetFilledState(currentItem.itemData.ItemIcon, currentItem.itemData.GetTierColor());
                // หรือถ้า EquipmentSlot มี method SetTierBorder แล้ว:
                // targetSlot.SetTierBorder(currentItem.itemData.GetTierColor());

                RemoveItemFromInventory(1); // ลบ 1 ชิ้น
                UpdateEquipButtons(currentItem.itemData);
                HideItemDetail();
            }
        }
    }

    private bool EquipFullPotionStack()
    {
        if (currentItem == null || currentItem.itemData?.ItemType != ItemType.Potion)
        {
            Debug.LogError("[ItemDetailPanel] EquipFullPotionStack called with invalid item!");
            return false;
        }

        if (currentCharacter == null)
        {
            Debug.LogError("[ItemDetailPanel] No current character for potion equip!");
            return false;
        }

        Debug.Log($"[ItemDetailPanel] 🧪 Equipping full potion stack: {currentItem.itemData.ItemName} x{currentItem.stackCount}");

        // หา inventory
        Inventory inventory = currentCharacter.GetInventory();
        if (inventory == null)
        {
            Debug.LogError("[ItemDetailPanel] No inventory found!");
            return false;
        }

        // หา inventory slot ที่มี potion นี้
        int inventorySlotIndex = FindItemSlotIndex(inventory, currentItem.itemData);
        if (inventorySlotIndex == -1)
        {
            Debug.LogError($"[ItemDetailPanel] Cannot find {currentItem.itemData.ItemName} in inventory!");
            return false;
        }

        // ตรวจสอบว่ามี CombatUIManager
        if (combatUIManager == null)
        {
            combatUIManager = FindObjectOfType<CombatUIManager>();
            if (combatUIManager == null)
            {
                Debug.LogError("[ItemDetailPanel] CombatUIManager not found!");
                return false;
            }
        }

        // หา empty potion slot
        EquipmentSlot targetSlot = FindEmptyPotionSlot();
        if (targetSlot == null)
        {
            Debug.LogWarning("[ItemDetailPanel] All potion slots are full!");
            ShowMessage("All potion slots are full!");
            return false;
        }

        // Equip ใน character
        bool equipSuccess = currentCharacter.EquipItemData(currentItem.itemData);
        if (!equipSuccess)
        {
            Debug.LogError("[ItemDetailPanel] Failed to equip potion to character!");
            return false;
        }

        // ลบ potion ทั้งหมดจาก inventory slot
        int totalStackCount = currentItem.stackCount;
        bool removeSuccess = inventory.RemoveItem(inventorySlotIndex, totalStackCount);

        if (removeSuccess)
        {
            Debug.Log($"[ItemDetailPanel] ✅ Removed entire potion stack ({totalStackCount} items) from inventory slot {inventorySlotIndex}");

            // อัปเดต equipment slot visual
            targetSlot.SetFilledState(currentItem.itemData.ItemIcon, currentItem.itemData.GetTierColor());

            // Force sync inventory UI
            ForceUpdateInventorySlot(inventorySlotIndex);

            // 🆕 **FIX**: ใช้ method ใหม่ที่ bypass safety check**
            if (PersistentPlayerData.Instance != null)
            {
                PersistentPlayerData.Instance.ForceSaveInventoryAfterEquip(currentCharacter, "Equip Full Potion Stack");
            }

            Debug.Log($"[ItemDetailPanel] 🎉 Successfully equipped full potion stack!");
            return true;
        }
        else
        {
            Debug.LogError("[ItemDetailPanel] Failed to remove potion from inventory!");
            return false;
        }
    }

    private void UpdatePotionSlotStackCount(EquipmentSlot potionSlot, int stackCount)
    {
        if (potionSlot.stackCountText != null && stackCount > 1)
        {
            potionSlot.stackCountText.text = stackCount.ToString();
            potionSlot.stackCountText.gameObject.SetActive(true);
            Debug.Log($"[ItemDetailPanel] 📊 Updated equipment slot stack count: {stackCount}");
        }
    }
    private EquipmentSlot FindEmptyPotionSlot()
    {
        if (combatUIManager?.potionSlots == null) return null;

        Debug.Log($"[ItemDetailPanel] Searching {combatUIManager.potionSlots.Count} potion slots for empty slot");

        foreach (var slot in combatUIManager.potionSlots)
        {
            if (slot != null && slot.SlotType == ItemType.Potion && slot.IsEmpty)
            {
                Debug.Log($"[ItemDetailPanel] Found empty potion slot at index {slot.PotionSlotIndex}");
                return slot;
            }
        }

        Debug.LogWarning("[ItemDetailPanel] No empty potion slot found");
        return null;
    }

    // 🆕 เพิ่ม method สำหรับหา equipment slot by type
    private EquipmentSlot FindEquipmentSlotByType(ItemType itemType)
    {
        if (combatUIManager?.equipmentSlots == null) return null;

        foreach (var slot in combatUIManager.equipmentSlots)
        {
            if (slot != null && slot.SlotType == itemType)
            {
                return slot;
            }
        }

        return null;
    }

    // 🆕 เพิ่ม method สำหรับแสดงข้อความ (optional)
    private void ShowMessage(string message)
    {
        Debug.Log($"[ItemDetailPanel] 💬 {message}");
        // TODO: แสดง popup message ใน UI (อาจจะทำในอนาคต)
    }

    private void RemoveItemFromInventory(int removeCount = 1)
    {
        if (currentItem == null || currentCharacter == null)
            return;

        Inventory inventory = currentCharacter.GetInventory();
        if (inventory == null)
        {
            Debug.LogWarning("[ItemDetailPanel] Character has no inventory");
            return;
        }

        // หา slot ที่มี item นี้
        int itemSlotIndex = FindItemSlotIndex(inventory, currentItem.itemData);

        if (itemSlotIndex != -1)
        {
            // ดึงข้อมูล item ก่อนลบ
            InventoryItem inventoryItem = inventory.GetItem(itemSlotIndex);
            int oldStackCount = inventoryItem?.stackCount ?? 0;

            // ลบจำนวนที่ระบุจาก inventory
            bool success = inventory.RemoveItem(itemSlotIndex, removeCount);

            if (success)
            {
                Debug.Log($"[ItemDetailPanel] Removed {removeCount} {currentItem.itemData.ItemName} from inventory slot {itemSlotIndex}");
                Debug.Log($"[ItemDetailPanel] Stack count: {oldStackCount} -> {oldStackCount - removeCount}");

                // Force update inventory UI
                ForceUpdateInventorySlot(itemSlotIndex);

                // 🆕 **FIX**: Force save ทันทีหลัง equip item
                ForceSaveAfterEquip(removeCount >= oldStackCount);

                // ถ้า stack หมดแล้ว หรือลบทั้งหมด ให้ปิด detail panel
                InventoryItem updatedItem = inventory.GetItem(itemSlotIndex);
                if (updatedItem == null || updatedItem.IsEmpty || removeCount >= oldStackCount)
                {
                    Debug.Log("[ItemDetailPanel] Item stack depleted or fully removed, closing detail panel");
                }
                else
                {
                    // อัปเดต current item กับ stack count ใหม่
                    currentItem = updatedItem;
                    Debug.Log($"[ItemDetailPanel] Updated current item stack: {currentItem.stackCount}");
                }
            }
            else
            {
                Debug.LogError($"[ItemDetailPanel] Failed to remove {currentItem.itemData.ItemName} from inventory");
            }
        }
        else
        {
            Debug.LogWarning($"[ItemDetailPanel] Could not find {currentItem.itemData.ItemName} in inventory");
        }
    }

    // 🆕 **เพิ่ม method ใหม่**
    private void ForceSaveAfterEquip(bool inventoryNowEmpty)
    {
        try
        {
            if (PersistentPlayerData.Instance != null && currentCharacter != null)
            {
                string saveAction = inventoryNowEmpty ? "Equip Last Item - Empty Inventory" : "Equip Item";

                Debug.Log($"[ItemDetailPanel] 💾 Force saving after equip: {saveAction}");

                // 🔑 **ใช้ method ใหม่ที่ bypass safety check ทั้งหมด**
                PersistentPlayerData.Instance.ForceSaveInventoryAfterEquip(currentCharacter, saveAction);

                Debug.Log($"[ItemDetailPanel] ✅ Force save completed for: {saveAction}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ItemDetailPanel] ❌ Force save error: {e.Message}");
        }
    }
    // 🆕 เพิ่ม method ใหม่สำหรับ force update inventory slot เฉพาะ
    private void ForceUpdateInventorySlot(int slotIndex)
    {
        Debug.Log($"[ItemDetailPanel] Force updating inventory slot {slotIndex}");

        // หา InventoryGridManager
        InventoryGridManager gridManager = FindObjectOfType<InventoryGridManager>();
        if (gridManager != null)
        {
            // อัปเดต slot เฉพาะที่เปลี่ยนแปลง
            gridManager.UpdateSlotFromCharacter(slotIndex);
            Debug.Log($"[ItemDetailPanel] ✅ Updated inventory slot {slotIndex} from character data");
        }
        else
        {
            Debug.LogWarning("[ItemDetailPanel] InventoryGridManager not found for slot update");
        }

        // Force refresh canvas
        Canvas.ForceUpdateCanvases();
    }

    private int FindItemSlotIndex(Inventory inventory, ItemData itemData)
    {
        for (int i = 0; i < inventory.CurrentSlots; i++)
        {
            InventoryItem item = inventory.GetItem(i);
            if (item != null && !item.IsEmpty && item.itemData == itemData)
            {
                return i;
            }
        }
        return -1; // ไม่เจอ
    }

    // 🆕 อัปเดต inventory UI
    private void UpdateInventoryUI(int slotIndex)
    {
        Debug.Log($"[ItemDetailPanel] Updating inventory UI...");

        // 🆕 อัปเดต slot เฉพาะก่อน
        if (slotIndex >= 0)
        {
            ForceUpdateInventorySlot(slotIndex);
        }

        // หา InventoryGridManager และ sync ทั้งหมด
        InventoryGridManager gridManager = FindObjectOfType<InventoryGridManager>();
        if (gridManager != null)
        {
            gridManager.ForceSyncAllSlots();
            Debug.Log($"[ItemDetailPanel] ✅ Force synced all inventory slots");
        }

        // อัปเดต equipment slots ผ่าง CombatUIManager
        CombatUIManager uiManager = FindObjectOfType<CombatUIManager>();
        if (uiManager?.equipmentSlotManager != null)
        {
            uiManager.equipmentSlotManager.RefreshAllSlots();
            Debug.Log("[ItemDetailPanel] ✅ Refreshed all equipment slots");
        }

        // 🆕 Force update canvas หลายครั้งเพื่อให้แน่ใจ
    }

   
    // 🆕 เมื่อกดปุ่ม Unequip
    // 🆕 เมื่อกดปุ่ม Unequip
    private void OnUnequipButtonClicked()
    {
        if (currentItem == null || currentCharacter == null)
        {
            Debug.LogWarning("[ItemDetailPanel] Cannot unequip: missing item or character");
            return;
        }

        ItemType itemType = currentItem.itemData.ItemType;
        Debug.Log($"[ItemDetailPanel] Unequipping item: {currentItem.itemData.ItemName} ({itemType})");

        // 🆕 แยก logic สำหรับ potion และ equipment
        if (itemType == ItemType.Potion)
        {
            bool success = UnequipPotionToInventory();
            if (success)
            {
                HideItemDetail();
            }
        }
        else
        {
            bool success = UnequipEquipmentToInventory();
            if (success)
            {
                HideItemDetail();
            }
        }
    }
    private bool UnequipPotionToInventory()
    {
        Debug.Log($"[ItemDetailPanel] 🧪 Unequipping potion: {currentItem.itemData.ItemName}");

        // หา potion slot ที่มี potion นี้
        EquipmentSlot targetSlot = FindPotionSlotWithItem(currentItem.itemData);
        if (targetSlot == null)
        {
            Debug.LogWarning($"[ItemDetailPanel] Cannot find potion slot with {currentItem.itemData.ItemName}");
            return false;
        }

        // 🆕 ใช้ method ใหม่จาก Character ที่จัดการทั้งหมดให้เรียบร้อย
        bool success = currentCharacter.UnequipPotionAndReturnToInventory(targetSlot.PotionSlotIndex);

        if (success)
        {
            Debug.Log($"[ItemDetailPanel] ✅ Successfully unequipped potion to inventory");

            // อัปเดต equipment slot UI
            targetSlot.SetEmptyState();

            // Force sync inventory UI
            ForceUpdateAllInventorySlots();

            return true;
        }
        else
        {
            Debug.LogError($"[ItemDetailPanel] ❌ Failed to unequip potion to inventory");
            return false;
        }
    }

    private EquipmentSlot FindPotionSlotWithItem(ItemData itemData)
    {
        if (combatUIManager?.potionSlots == null) return null;

        for (int i = 0; i < combatUIManager.potionSlots.Count; i++)
        {
            EquipmentSlot slot = combatUIManager.potionSlots[i];
            if (slot != null && slot.SlotType == ItemType.Potion)
            {
                // ตรวจสอบว่า character มี potion นี้ใน slot หรือไม่
                ItemData potionInSlot = currentCharacter.GetPotionInSlot(slot.PotionSlotIndex);
                if (potionInSlot == itemData)
                {
                    Debug.Log($"[ItemDetailPanel] Found {itemData.ItemName} in potion slot {slot.PotionSlotIndex}");
                    return slot;
                }
            }
        }

        Debug.LogWarning($"[ItemDetailPanel] No potion slot found with {itemData.ItemName}");
        return null;
    }

    // 🆕 เพิ่ม method สำหรับ unequip equipment ปกติ
    private bool UnequipEquipmentToInventory()
    {
        ItemType itemType = currentItem.itemData.ItemType;
        Debug.Log($"[ItemDetailPanel] ⚔️ Unequipping equipment: {currentItem.itemData.ItemName} ({itemType})");

        // หา equipment slot
        EquipmentSlot targetSlot = FindEquipmentSlotByType(itemType);
        if (targetSlot == null)
        {
            Debug.LogWarning($"[ItemDetailPanel] No slot found for ItemType: {itemType}");
            return false;
        }

        // 🆕 ใช้ method ใหม่จาก Character ที่จัดการทั้งหมดให้เรียบร้อย
        bool success = currentCharacter.UnequipAndReturnToInventory(itemType);

        if (success)
        {
            Debug.Log($"[ItemDetailPanel] ✅ Successfully unequipped equipment to inventory");

            // อัปเดต equipment slot UI
            targetSlot.SetEmptyState();

            // Force sync inventory UI
            ForceUpdateAllInventorySlots();

            return true;
        }
        else
        {
            Debug.LogError($"[ItemDetailPanel] ❌ Failed to unequip equipment to inventory");
            return false;
        }
    }
    // 🆕 เพิ่ม method สำหรับหา potion slot ที่มี item ระบุ

    // 🆕 เพิ่ม method สำหรับนับ potion ทั้งหมดใน inventory


    // 🆕 เพิ่ม method สำหรับ force update inventory ทั้งหมด
    private void ForceUpdateAllInventorySlots()
    {
        Debug.Log("[ItemDetailPanel] Force updating all inventory slots...");

        InventoryGridManager gridManager = FindObjectOfType<InventoryGridManager>();
        if (gridManager != null)
        {
            gridManager.ForceSyncAllSlots();
            Debug.Log("[ItemDetailPanel] ✅ Force synced all inventory slots");
        }

        // อัปเดต equipment slots ด้วย
        CombatUIManager uiManager = FindObjectOfType<CombatUIManager>();
        if (uiManager?.equipmentSlotManager != null)
        {
            uiManager.equipmentSlotManager.RefreshAllSlots();
            Debug.Log("[ItemDetailPanel] ✅ Refreshed all equipment slots");
        }

        // Force canvas update
        Canvas.ForceUpdateCanvases();
    }
    private void UpdateEquipButtons(ItemData itemData)
    {
        // 🆕 ซ่อนปุ่ม equip/unequip สำหรับ materials และ misc
        if (itemData.ItemType == ItemType.Material || itemData.ItemType == ItemType.Misc)
        {
            if (equipButton != null) equipButton.gameObject.SetActive(false);
            if (unequipButton != null) unequipButton.gameObject.SetActive(false);
            Debug.Log($"[ItemDetailPanel] Hidden equip buttons for {itemData.ItemType}");
            return;
        }

        // สำหรับไอเทมอื่น ๆ ใช้ logic เดิม
        bool isCurrentlyEquipped = IsItemCurrentlyEquipped(itemData);

        if (equipButton != null)
        {
            bool showEquipButton = !isCurrentlyEquipped;
            equipButton.gameObject.SetActive(showEquipButton);
            if (equipButtonText != null)
            {
                if (itemData.ItemType == ItemType.Potion)
                    equipButtonText.text = "Add to Quick Slot";
                else
                    equipButtonText.text = "Equip";
            }
        }

        if (unequipButton != null)
        {
            bool showUnequipButton = isCurrentlyEquipped;
            unequipButton.gameObject.SetActive(showUnequipButton);
            if (unequipButtonText != null)
            {
                if (itemData.ItemType == ItemType.Potion)
                    unequipButtonText.text = "Remove from Slot";
                else
                    unequipButtonText.text = "Unequip";
            }
        }
    }

    private bool IsItemCurrentlyEquipped(ItemData itemData)
    {
        if (currentCharacter == null || itemData == null) return false;

        if (itemData.ItemType == ItemType.Potion)
        {
            Debug.Log($"[ItemDetailPanel] Checking if potion {itemData.ItemName} is equipped...");

            // ตรวจสอบ potion slots ทั้ง 5 ช่อง
            for (int i = 0; i < 5; i++)
            {
                ItemData potionInSlot = currentCharacter.GetPotionInSlot(i);
                Debug.Log($"[ItemDetailPanel] Potion slot {i}: {(potionInSlot?.ItemName ?? "EMPTY")}");

                if (potionInSlot == itemData)
                {
                    Debug.Log($"[ItemDetailPanel] ✅ Found {itemData.ItemName} in potion slot {i}");
                    return true;
                }
            }

            Debug.Log($"[ItemDetailPanel] ❌ {itemData.ItemName} not found in any potion slot");
            return false;
        }
        else
        {
            // ตรวจสอบ equipment slots
            ItemData equippedItem = currentCharacter.GetEquippedItem(itemData.ItemType);
            bool isEquipped = equippedItem == itemData;

            Debug.Log($"[ItemDetailPanel] Equipment check: {itemData.ItemName} is {(isEquipped ? "EQUIPPED" : "NOT EQUIPPED")}");
            return isEquipped;
        }
    }

    // 🆕 เพิ่ม method สำหรับหา Equipment Slot
    private EquipmentSlot FindEquipmentSlot(ItemType itemType)
    {
        if (combatUIManager == null) return null;

        // หาใน equipment slots
        foreach (var slot in combatUIManager.equipmentSlots)
        {
            if (slot.SlotType == itemType)
            {
                return slot;
            }
        }

        // หาใน potion slots
        if (itemType == ItemType.Potion)
        {
            foreach (var slot in combatUIManager.potionSlots)
            {
                if (slot.SlotType == itemType && !slot.IsEmpty)
                {
                    return slot;
                }
            }
        }

        return null;
    }
    
    // 🆕 สร้าง EquipmentData จาก ItemData
   
}