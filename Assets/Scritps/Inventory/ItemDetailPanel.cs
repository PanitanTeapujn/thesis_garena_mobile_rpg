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
    private void UpdateEquipButtons(ItemData itemData)
    {
        bool canEquip = CanEquipItem(itemData);
        bool isEquippable = IsEquippableItem(itemData);

        // แสดง/ซ่อนปุ่ม equip
        if (equipButton != null)
        {
            equipButton.gameObject.SetActive(isEquippable && canEquip);
            if (equipButtonText != null)
                equipButtonText.text = "Equip";
        }

        // แสดง/ซ่อนปุ่ม unequip
        if (unequipButton != null)
        {
            unequipButton.gameObject.SetActive(isEquippable && !canEquip);
            if (unequipButtonText != null)
                unequipButtonText.text = "Unequip";
        }

        Debug.Log($"[ItemDetailPanel] Item: {itemData.ItemName}, IsEquippable: {isEquippable}, CanEquip: {canEquip}");
    }

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

        Debug.Log($"[ItemDetailPanel] Equipping item: {currentItem.itemData.ItemName}");

        // สร้าง EquipmentData จาก ItemData
        EquipmentData equipmentData = CreateEquipmentDataFromItem(currentItem.itemData);

        if (equipmentData != null)
        {
            // Equip item
            currentCharacter.EquipItem(equipmentData);

            // อัปเดตปุ่ม
            UpdateEquipButtons(currentItem.itemData);

            Debug.Log($"[ItemDetailPanel] Successfully equipped: {currentItem.itemData.ItemName}");
        }
        else
        {
            Debug.LogError($"[ItemDetailPanel] Failed to create equipment data for: {currentItem.itemData.ItemName}");
        }
    }

    // 🆕 เมื่อกดปุ่ม Unequip
    private void OnUnequipButtonClicked()
    {
        if (currentItem == null || currentCharacter == null)
        {
            Debug.LogWarning("[ItemDetailPanel] Cannot unequip: missing item or character");
            return;
        }

        Debug.Log($"[ItemDetailPanel] Unequipping item: {currentItem.itemData.ItemName}");

        // Unequip item
        currentCharacter.UnequipItem();

        // อัปเดตปุ่ม
        UpdateEquipButtons(currentItem.itemData);

        Debug.Log($"[ItemDetailPanel] Successfully unequipped: {currentItem.itemData.ItemName}");
    }

    // 🆕 สร้าง EquipmentData จาก ItemData
    private EquipmentData CreateEquipmentDataFromItem(ItemData itemData)
    {
        // TODO: ต้องสร้าง EquipmentData จาก ItemData
        // ตอนนี้ return null ไว้ก่อน เพราะต้องดู structure ของ EquipmentData

        Debug.LogWarning("[ItemDetailPanel] CreateEquipmentDataFromItem not implemented yet");
        return null;
    }
}