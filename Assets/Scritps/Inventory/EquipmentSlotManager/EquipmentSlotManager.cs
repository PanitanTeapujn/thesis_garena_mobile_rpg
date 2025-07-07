using UnityEngine;
using System.Collections.Generic;

public class EquipmentSlotManager : MonoBehaviour
{
    [Header("📋 Equipment Slots (จะถูกเชื่อมต่อจาก CombatUI)")]
    [SerializeField] private List<EquipmentSlot> equipmentSlots = new List<EquipmentSlot>();
    [SerializeField] private List<EquipmentSlot> potionSlots = new List<EquipmentSlot>();
    [Header("📋 Connected Slots (จาก CombatUI)")]
    [SerializeField] private List<EquipmentSlot> connectedEquipmentSlots = new List<EquipmentSlot>();
    [SerializeField] private List<EquipmentSlot> connectedPotionSlots = new List<EquipmentSlot>();

    // Character Reference
    private Character ownerCharacter;

    // Properties
    public Character OwnerCharacter { get { return ownerCharacter; } }

    #region Unity Lifecycle
    private void Awake()
    {
        // หา Character component ในตัวเอง
        ownerCharacter = GetComponent<Character>();
        if (ownerCharacter == null)
        {
            Debug.LogError($"[EquipmentSlotManager] No Character component found on {gameObject.name}!");
        }
    }

    private void Start()
    {
        // รอให้ CombatUI เชื่อมต่อ slots ก่อน
        Debug.Log($"[EquipmentSlotManager] Ready for slot connections on {ownerCharacter?.CharacterName}");
    }
    #endregion

    #region Slot Registration (จาก CombatUI)
    /// <summary>
    /// เชื่อมต่อ Equipment Slots จาก CombatUI
    /// </summary>
    
    public void ConnectPotionSlots(List<EquipmentSlot> slots)
    {
        connectedPotionSlots.Clear();

        foreach (EquipmentSlot slot in slots)
        {
            if (slot != null && slot.SlotType == ItemType.Potion)
            {
                connectedPotionSlots.Add(slot);
                slot.SetManager(this);
                slot.OnSlotClicked += HandleSlotClicked;
            }
        }

        Debug.Log($"[EquipmentSlotManager] Connected {connectedPotionSlots.Count} potion slots");
        LoadEquippedItemsToSlots();
    }
    /// <summary>
    /// เชื่อมต่อ Potion Slots จาก CombatUI  
    /// </summary>
   
    #endregion
    public void ConnectEquipmentSlots(List<EquipmentSlot> slots)
    {
        connectedEquipmentSlots.Clear();

        foreach (EquipmentSlot slot in slots)
        {
            if (slot != null && slot.SlotType != ItemType.Potion)
            {
                connectedEquipmentSlots.Add(slot);
                slot.SetManager(this);
                slot.OnSlotClicked += HandleSlotClicked;
            }
        }

        Debug.Log($"[EquipmentSlotManager] Connected {connectedEquipmentSlots.Count} equipment slots");
        LoadEquippedItemsToSlots();
    }
    #region Slot Management
    private void LoadEquippedItemsToSlots()
    {
        if (ownerCharacter == null) return;

        Debug.Log($"[EquipmentSlotManager] Loading equipped items for {ownerCharacter.CharacterName}...");

        // 🆕 Debug character equipment ก่อน update slots

        int equipmentUpdated = 0;
        int potionUpdated = 0;

        // อัปเดต Equipment Slots
        foreach (EquipmentSlot slot in connectedEquipmentSlots)
        {
            if (slot != null)
            {
                UpdateSlotFromCharacter(slot);
                equipmentUpdated++;
            }
        }

        // อัปเดต Potion Slots ด้วย debug
        Debug.Log($"[EquipmentSlotManager] Updating {connectedPotionSlots.Count} potion slots...");
        foreach (EquipmentSlot slot in connectedPotionSlots)
        {
            if (slot != null)
            {
                Debug.Log($"[EquipmentSlotManager] Processing potion slot {slot.PotionSlotIndex}...");
                UpdateSlotFromCharacter(slot);
                potionUpdated++;
            }
            else
            {
                Debug.LogWarning("[EquipmentSlotManager] Found null potion slot!");
            }
        }

        // Force update canvas เพื่อให้แน่ใจว่า UI update
        Canvas.ForceUpdateCanvases();

        Debug.Log($"[EquipmentSlotManager] ✅ Updated {equipmentUpdated} equipment slots and {potionUpdated} potion slots");
    }
  

    /// <summary>
    /// 🆕 Helper method
    /// </summary>
    private ItemType GetItemTypeFromSlotIndex(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0: return ItemType.Head;
            case 1: return ItemType.Armor;
            case 2: return ItemType.Weapon;
            case 3: return ItemType.Pants;
            case 4: return ItemType.Shoes;
            case 5: return ItemType.Rune;
            default: return ItemType.Weapon;
        }
    }

    public void UpdateSlotFromCharacter(EquipmentSlot slot)
    {
        if (slot == null || ownerCharacter == null) return;

        Debug.Log($"[EquipmentSlotManager] 🔄 Updating {slot.SlotType} slot...");

        // 🆕 ใช้ RefreshFromCharacterData แทน
        slot.RefreshFromCharacterData();
    }
    public void ForceRefreshAfterPotionUse(int potionSlotIndex)
    {
        Debug.Log($"[EquipmentSlotManager] 🧪 Force refreshing after potion use in slot {potionSlotIndex}...");

        // หา potion slot ที่ตรงกับ index
        foreach (EquipmentSlot slot in connectedPotionSlots)
        {
            if (slot != null && slot.PotionSlotIndex == potionSlotIndex)
            {
                Debug.Log($"[EquipmentSlotManager] Found potion slot {potionSlotIndex}, refreshing...");
                slot.RefreshFromCharacterData();

                // Force update ทันที
                Canvas.ForceUpdateCanvases();

                Debug.Log($"[EquipmentSlotManager] ✅ Refreshed potion slot {potionSlotIndex}");
                break;
            }
        }
    }
    public void RefreshAllSlots()
    {
        Debug.Log($"[EquipmentSlotManager] 🔄 Refreshing all slots for {ownerCharacter?.CharacterName}...");

        int equipmentRefreshed = 0;
        int potionRefreshed = 0;

        // Refresh Equipment Slots
        foreach (EquipmentSlot slot in connectedEquipmentSlots)
        {
            if (slot != null)
            {
                slot.RefreshFromCharacterData();
                equipmentRefreshed++;
            }
        }

        // 🆕 Refresh Potion Slots พร้อม debug
        foreach (EquipmentSlot slot in connectedPotionSlots)
        {
            if (slot != null)
            {
                Debug.Log($"[EquipmentSlotManager] 🧪 Refreshing potion slot {slot.PotionSlotIndex}...");
                slot.RefreshFromCharacterData();
                potionRefreshed++;
            }
        }

        // 🆕 Force update Canvas หลัง refresh ทั้งหมด
        Canvas.ForceUpdateCanvases();

        Debug.Log($"[EquipmentSlotManager] ✅ Refreshed {equipmentRefreshed} equipment slots and {potionRefreshed} potion slots");
    }
    public void RefreshPotionSlots()
    {
        Debug.Log($"[EquipmentSlotManager] 🧪 Refreshing potion slots only...");

        int refreshed = 0;
        foreach (EquipmentSlot slot in connectedPotionSlots)
        {
            if (slot != null)
            {
                slot.RefreshFromCharacterData();
                refreshed++;
            }
        }

        Canvas.ForceUpdateCanvases();
        Debug.Log($"[EquipmentSlotManager] ✅ Refreshed {refreshed} potion slots");
    }


    /// <summary>
    /// ตรวจสอบว่า slots ถูกเชื่อมต่อแล้วหรือยัง
    /// </summary>
    public bool IsConnected()
    {
        return connectedEquipmentSlots.Count > 0 || connectedPotionSlots.Count > 0;
    }
    #endregion

    #region Slot Events
    private void HandleSlotClicked(EquipmentSlot slot)
    {
        if (slot == null || ownerCharacter == null) return;

        Debug.Log($"[EquipmentSlotManager] {slot.SlotType} slot clicked");

        // หา equipped item
        ItemData equippedItem = null;
        if (slot.SlotType == ItemType.Potion)
        {
            equippedItem = ownerCharacter.GetPotionInSlot(slot.PotionSlotIndex);
        }
        else
        {
            equippedItem = ownerCharacter.GetEquippedItem(slot.SlotType);
        }

        // แสดง item detail ถ้ามี item
        if (equippedItem != null)
        {
            ShowItemDetail(equippedItem);
        }
    }

    private void ShowItemDetail(ItemData itemData)
    {
        // หา CombatUIManager เพื่อแสดง item detail
        CombatUIManager uiManager = FindObjectOfType<CombatUIManager>();
        if (uiManager?.itemDetailManager != null)
        {
            // สร้าง temporary InventoryItem เพื่อแสดง
            InventoryItem tempItem = new InventoryItem(itemData, 1, -1);
            uiManager.itemDetailManager.ShowItemDetail(tempItem);

            Debug.Log($"[EquipmentSlotManager] Showing detail for: {itemData.ItemName}");
        }
    }
    #endregion

    #region Public Methods
    public ItemData GetEquippedItem(ItemType itemType)
    {
        if (ownerCharacter == null) return null;
        return ownerCharacter.GetEquippedItem(itemType);
    }

    public ItemData GetPotionInSlot(int slotIndex)
    {
        if (ownerCharacter == null) return null;
        return ownerCharacter.GetPotionInSlot(slotIndex);
    }

    public bool EquipItem(ItemData itemData)
    {
        if (ownerCharacter == null) return false;

        bool success = ownerCharacter.EquipItemData(itemData);
        if (success)
        {
            // Refresh slots ที่เกี่ยวข้อง
            RefreshAllSlots();
        }
        return success;
    }

    public bool UnequipItem(ItemType itemType)
    {
        if (ownerCharacter == null) return false;

        bool success = ownerCharacter.UnequipItemData(itemType);
        if (success)
        {
            // Refresh slots ที่เกี่ยวข้อง
            RefreshAllSlots();
        }
        return success;
    }

    public bool UnequipPotion(int slotIndex)
    {
        if (ownerCharacter == null) return false;

        bool success = ownerCharacter.UnequipPotion(slotIndex);
        if (success)
        {
            // Refresh slots ที่เกี่ยวข้อง
            RefreshAllSlots();
        }
        return success;
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// ตรวจสอบว่า slots ถูกเชื่อมต่อแล้วหรือยัง
    /// </summary>


    /// <summary>
    /// ข้อมูลสำหรับ Debug
    /// </summary>
    /// 
    public void ForceRefreshFromCharacter()
    {
        if (ownerCharacter == null)
        {
            Debug.LogWarning("[EquipmentSlotManager] No owner character for refresh");
            return;
        }

        Debug.Log($"[EquipmentSlotManager] 🔄 Force refreshing from character {ownerCharacter.CharacterName}...");

        if (!IsConnected())
        {
            Debug.LogWarning("[EquipmentSlotManager] ⚠️ Equipment slots not connected! Retrying...");

            // ลองรอแล้ว retry
            StartCoroutine(RetryRefresh());
            return;
        }

        // Debug character equipment ก่อน refresh

        // Refresh all slots
        RefreshAllSlots();

        Debug.Log("[EquipmentSlotManager] ✅ Force refresh completed");
    }
  
    private System.Collections.IEnumerator RetryRefresh()
    {
        yield return new WaitForSeconds(0.5f);

        if (IsConnected())
        {
            RefreshAllSlots();
            Debug.Log("[EquipmentSlotManager] ✅ Retry refresh successful");
        }
        else
        {
            Debug.LogError("[EquipmentSlotManager] ❌ Retry refresh failed - slots still not connected");
        }
    }
   
    #endregion

    #region Context Menu for Testing
    
    #endregion
}