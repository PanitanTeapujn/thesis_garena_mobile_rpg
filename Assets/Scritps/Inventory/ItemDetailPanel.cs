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

        // 🆕 Setup equip button
        if (equipButton != null)
        {
            equipButton.onClick.RemoveAllListeners();
            equipButton.onClick.AddListener(OnEquipButtonClicked);
        }

        // 🆕 Setup unequip button
        if (unequipButton != null)
        {
            unequipButton.onClick.RemoveAllListeners();
            unequipButton.onClick.AddListener(OnUnequipButtonClicked);
        }
        combatUIManager = GetComponentInParent<CombatUIManager>();

        // ซ่อน panel ตั้งแต่เริ่มต้น
        HideItemDetail();
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
            itemIconImage.color = itemData.GetTierColor();
        }

        // แสดงชื่อไอเทม
        if (itemNameText != null)
        {
            itemNameText.text = itemData.ItemName;
            itemNameText.color = itemData.GetTierColor();
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
                stackCountText.text = $"Quantity: {item.stackCount}";
                stackCountText.gameObject.SetActive(true);
            }
            else
            {
                stackCountText.gameObject.SetActive(false);
            }
        }

        // 🆕 จัดการปุ่ม Equip/Unequip
        UpdateEquipButtons(itemData);

        Debug.Log($"[ItemDetailPanel] Showing details for: {itemData.ItemName}");
    }

    public void HideItemDetail()
    {
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

        // หาช่องที่ตรงกับ ItemType
        EquipmentSlot targetSlot = null;

        if (combatUIManager != null)
        {
            // เช็ค equipment slots
            foreach (var slot in combatUIManager.equipmentSlots)
            {
                if (slot.SlotType == itemType)
                {
                    targetSlot = slot;
                    break;
                }
            }

            // ถ้ายังไม่เจอ ลองเช็ค potion slots
            if (targetSlot == null)
            {
                foreach (var slot in combatUIManager.potionSlots)
                {
                    if (slot.SlotType == itemType)
                    {
                        targetSlot = slot;
                        break;
                    }
                }
            }
        }

        if (targetSlot == null)
        {
            Debug.LogWarning($"[ItemDetailPanel] No slot found for ItemType: {itemType}");
            return;
        }

        Debug.Log($"[ItemDetailPanel] Equipping item: {currentItem.itemData.ItemName} into slot {targetSlot.SlotType}");

        // สร้าง EquipmentData จาก ItemData
        EquipmentData equipmentData = CreateEquipmentDataFromItem(currentItem.itemData);

        if (equipmentData != null)
        {
            // Equip ใน character
            currentCharacter.EquipItemData(currentItem.itemData);

            // แสดงไอคอนใน slot
            targetSlot.SetFilledState(currentItem.itemData.ItemIcon, currentItem.itemData.GetTierColor());

            // 🆕 ลบไอเทมออกจาก inventory
            RemoveItemFromInventory();

            UpdateEquipButtons(currentItem.itemData);

            Debug.Log($"[ItemDetailPanel] Successfully equipped: {currentItem.itemData.ItemName}");
        }
        else
        {
            Debug.LogError($"[ItemDetailPanel] Failed to create equipment data for: {currentItem.itemData.ItemName}");
        }
    }

    private void RemoveItemFromInventory()
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
            // ลบ 1 ชิ้นจาก inventory
            bool success = inventory.RemoveItem(itemSlotIndex, 1);

            if (success)
            {
                Debug.Log($"[ItemDetailPanel] Removed {currentItem.itemData.ItemName} from inventory slot {itemSlotIndex}");

                // อัปเดต inventory UI
                UpdateInventoryUI(itemSlotIndex);
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
        // หา InventoryGridManager
        InventoryGridManager gridManager = FindObjectOfType<InventoryGridManager>();
        if (gridManager != null)
        {
            // ใช้ ForceSyncAllSlots เพื่ออัปเดต inventory ทั้งหมด
            gridManager.ForceSyncAllSlots();
            Debug.Log($"[ItemDetailPanel] Updated inventory UI for all slots");
        }
        else
        {
            Debug.LogWarning("[ItemDetailPanel] InventoryGridManager not found for UI update");
        }

        // 🆕 อัปเดต equipment slots ผ่าง CombatUIManager
        CombatUIManager uiManager = FindObjectOfType<CombatUIManager>();
        if (uiManager?.equipmentSlotManager != null)
        {
            uiManager.equipmentSlotManager.RefreshAllSlots();
            Debug.Log("[ItemDetailPanel] Updated equipment slots UI");
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

        // หา slot ที่ต้อง unequip
        EquipmentSlot targetSlot = FindEquipmentSlot(itemType);
        if (targetSlot == null)
        {
            Debug.LogWarning($"[ItemDetailPanel] No slot found for ItemType: {itemType}");
            return;
        }

        // Unequip จาก character
        bool unequipSuccess = false;
        if (itemType == ItemType.Potion)
        {
            unequipSuccess = currentCharacter.UnequipPotion(targetSlot.PotionSlotIndex);
        }
        else
        {
            unequipSuccess = currentCharacter.UnequipItemData(itemType);
        }

        if (unequipSuccess)
        {
            // อัปเดต slot UI
            targetSlot.SetEmptyState();

            // Unequip จาก EquipmentManager เดิมด้วย (ถ้ามี)
           /* if (equipmentManager != null && itemType != ItemType.Potion)
            {
                equipmentManager.UnequipItem();
            }*/

            // อัปเดต inventory UI
            UpdateInventoryUI(-1);

            // ปิด detail panel
            HideItemDetail();

            Debug.Log($"[ItemDetailPanel] Successfully unequipped: {currentItem.itemData.ItemName}");
        }
        else
        {
            Debug.LogError($"[ItemDetailPanel] Failed to unequip: {currentItem.itemData.ItemName}");
        }
    }
    private void UpdateEquipButtons(ItemData itemData)
    {
        bool isCurrentlyEquipped = IsItemCurrentlyEquipped(itemData);

        if (equipButton != null)
        {
            equipButton.gameObject.SetActive(!isCurrentlyEquipped);
            if (equipButtonText != null)
                equipButtonText.text = "Equip";
        }

        if (unequipButton != null)
        {
            unequipButton.gameObject.SetActive(isCurrentlyEquipped);
            if (unequipButtonText != null)
                unequipButtonText.text = "Unequip";
        }

        Debug.Log($"[ItemDetailPanel] Updated buttons for {itemData.ItemName}: Equipped={isCurrentlyEquipped}");
    }

    private bool IsItemCurrentlyEquipped(ItemData itemData)
    {
        if (currentCharacter == null || itemData == null) return false;

        // ตรวจสอบตาม item type
        if (itemData.ItemType == ItemType.Potion)
        {
            // ตรวจสอบ potion slots
            for (int i = 0; i < 5; i++)
            {
                ItemData potionInSlot = currentCharacter.GetPotionInSlot(i);
                if (potionInSlot == itemData)
                {
                    return true;
                }
            }
        }
        else
        {
            // ตรวจสอบ equipment slots
            ItemData equippedItem = currentCharacter.GetEquippedItem(itemData.ItemType);
            return equippedItem == itemData;
        }

        return false;
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