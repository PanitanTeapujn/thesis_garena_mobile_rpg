using UnityEngine;
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

        combatUIManager = GetComponentInParent<CombatUIManager>();

        // 🆕 Setup tier background for itemIconImage (ใช้ background แทน outline)
        SetupItemIconTierBackground();

        // ซ่อน panel ตั้งแต่เริ่มต้น
        HideItemDetail();
    }

    private void SetupItemIconTierBackground()
    {
        // หา tier background ถ้าไม่ได้ assign ใน inspector
        if (itemTierBackground == null && itemIconImage != null)
        {
            // หาใน children ของ itemIconImage
            Transform tierBgTransform = itemIconImage.transform.Find("TierBackground");
            if (tierBgTransform != null)
            {
                itemTierBackground = tierBgTransform.GetComponent<Image>();
            }
            else
            {
                // สร้างใหม่ถ้าไม่มี
                GameObject tierBgObj = new GameObject("TierBackground");
                tierBgObj.transform.SetParent(itemIconImage.transform, false);
                itemTierBackground = tierBgObj.AddComponent<Image>();

                // ตั้งค่า RectTransform ให้อยู่ข้างหลัง itemIconImage
                RectTransform tierRect = itemTierBackground.GetComponent<RectTransform>();
                tierRect.anchorMin = Vector2.zero;
                tierRect.anchorMax = Vector2.one;
                tierRect.offsetMin = Vector2.one * -5f;  // ขยายออกมาเล็กน้อย
                tierRect.offsetMax = Vector2.one * 5f;

                // ตั้งค่าให้เป็น background layer
                tierBgObj.transform.SetSiblingIndex(0); // อยู่ด้านหลัง itemIconImage
                itemTierBackground.raycastTarget = false;
                itemTierBackground.color = Color.white;
                itemTierBackground.enabled = false; // ปิดไว้ก่อน

                Debug.Log("[ItemDetailPanel] Created TierBackground for itemIconImage");
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
                "No description available." : itemData.Description;
        }

        // แสดงสถิติ
        if (itemStatsText != null)
        {
            string statsText = itemData.Stats.GetStatsDescription();
            itemStatsText.text = string.IsNullOrEmpty(statsText) ?
                "No bonus stats" : statsText;
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

    private void SetItemIconTierBorder(Color tierColor)
    {
        if (itemIconImage != null)
        {
            var outline = itemIconImage.GetComponent<UnityEngine.UI.Outline>();
            if (outline != null)
            {
                outline.effectColor = tierColor;
                outline.enabled = true;
                Debug.Log($"[ItemDetailPanel] Set item icon tier border to {tierColor}");
            }
            else
            {
                Debug.LogWarning("[ItemDetailPanel] No Outline component found on itemIconImage");
            }
        }
    }

    // 🆕 method สำหรับปิด tier border
    private void DisableItemIconTierBorder()
    {
        if (itemIconImage != null)
        {
            var outline = itemIconImage.GetComponent<UnityEngine.UI.Outline>();
            if (outline != null)
            {
                outline.enabled = false;
                Debug.Log("[ItemDetailPanel] Disabled item icon tier border");
            }
        }
    }


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

                // ถ้า stack หมดแล้ว หรือลบทั้งหมด ให้ปิด detail panel
                InventoryItem updatedItem = inventory.GetItem(itemSlotIndex);
                if (updatedItem == null || updatedItem.IsEmpty || removeCount >= oldStackCount)
                {
                    Debug.Log("[ItemDetailPanel] Item stack depleted or fully removed, closing detail panel");
                    // ไม่ต้อง update currentItem เพราะจะ hide panel แล้ว
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
        StartCoroutine(DelayedCanvasUpdates());
    }

    private System.Collections.IEnumerator DelayedCanvasUpdates()
    {
        // อัปเดต canvas หลายครั้งเพื่อให้แน่ใจว่า UI refresh
        for (int i = 0; i < 3; i++)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            Debug.Log($"[ItemDetailPanel] Canvas update #{i + 1}");
        }
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
        bool isCurrentlyEquipped = IsItemCurrentlyEquipped(itemData);

        Debug.Log($"[ItemDetailPanel] Updating buttons for {itemData.ItemName}:");
        Debug.Log($"  - Item Type: {itemData.ItemType}");
        Debug.Log($"  - Currently Equipped: {isCurrentlyEquipped}");

        if (equipButton != null)
        {
            bool showEquipButton = !isCurrentlyEquipped;
            equipButton.gameObject.SetActive(showEquipButton);
            if (equipButtonText != null)
            {
                // 🆕 เปลี่ยนข้อความตาม item type
                if (itemData.ItemType == ItemType.Potion)
                    equipButtonText.text = "Add to Quick Slot";
                else
                    equipButtonText.text = "Equip";
            }

            Debug.Log($"  - Equip Button: {(showEquipButton ? "VISIBLE" : "HIDDEN")}");
        }

        if (unequipButton != null)
        {
            bool showUnequipButton = isCurrentlyEquipped;
            unequipButton.gameObject.SetActive(showUnequipButton);
            if (unequipButtonText != null)
            {
                // 🆕 เปลี่ยนข้อความตาม item type
                if (itemData.ItemType == ItemType.Potion)
                    unequipButtonText.text = "Remove from Slot";
                else
                    unequipButtonText.text = "Unequip";
            }

            Debug.Log($"  - Unequip Button: {(showUnequipButton ? "VISIBLE" : "HIDDEN")}");
        }
    }

    private void DebugCurrentCharacterSlots()
    {
        if (currentCharacter == null)
        {
            Debug.LogWarning("[ItemDetailPanel] No current character to debug");
            return;
        }

        Debug.Log("=== CHARACTER SLOTS DEBUG ===");

        // Debug equipment slots
        Debug.Log("--- Equipment Slots ---");
        ItemType[] equipmentTypes = { ItemType.Head, ItemType.Armor, ItemType.Weapon, ItemType.Pants, ItemType.Shoes, ItemType.Rune };
        foreach (ItemType type in equipmentTypes)
        {
            ItemData equipped = currentCharacter.GetEquippedItem(type);
            Debug.Log($"{type}: {(equipped?.ItemName ?? "EMPTY")}");
        }

        // Debug potion slots
        Debug.Log("--- Potion Slots ---");
        for (int i = 0; i < 5; i++)
        {
            ItemData potion = currentCharacter.GetPotionInSlot(i);
            Debug.Log($"Potion {i}: {(potion?.ItemName ?? "EMPTY")}");
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
    private void SetupItemIconBorder()
    {
        if (itemIconImage != null)
        {
            // ตรวจสอบว่ามี Outline component หรือยัง
            var outline = itemIconImage.GetComponent<UnityEngine.UI.Outline>();
            if (outline == null)
            {
                // เพิ่ม Outline component
                outline = itemIconImage.gameObject.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = Color.white;
                outline.effectDistance = new Vector2(3f, -3f); // ขนาดใหญ่กว่าใน inventory slot เล็กน้อย
                outline.enabled = false; // ปิดไว้ก่อน
                Debug.Log("[ItemDetailPanel] Added Outline component to itemIconImage");
            }
        }
    }
    // 🆕 สร้าง EquipmentData จาก ItemData
    private EquipmentData CreateEquipmentDataFromItem(ItemData itemData)
    {
        if (itemData == null)
        {
            Debug.LogError("[ItemDetailPanel] Cannot create EquipmentData: ItemData is null");
            return null;
        }

        // ตรวจสอบว่าเป็นไอเทมประเภทที่สวมใส่ได้
        if (!IsEquippableItem(itemData))
        {
            Debug.LogError($"[ItemDetailPanel] Item {itemData.ItemName} is not equippable.");
            return null;
        }

        // สร้าง EquipmentStats จาก ItemStats
        EquipmentStats equipmentStats = itemData.Stats.ToEquipmentStats();

        // สร้าง EquipmentData
        EquipmentData newEquipment = new EquipmentData
        {
            itemName = itemData.ItemName,
            stats = equipmentStats,
            itemIcon = itemData.ItemIcon
        };

        return newEquipment;
    }
}