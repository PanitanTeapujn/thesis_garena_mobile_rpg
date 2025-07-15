using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemDeleteManager : MonoBehaviour
{
    [Header("Delete Mode UI")]
    public GameObject deleteConfirmPanel;          // Panel สำหรับยืนยันการลบ
    public Button cancelButton;                    // ปุ่ม Cancel
    public Button confirmButton;                   // ปุ่ม Confirm
    public TextMeshProUGUI confirmMessageText;     // ข้อความยืนยัน

    [Header("Visual Settings")]
    public Color deleteModeBorderColor = Color.red;           // สีขอบเมื่ออยู่ในโหมดลบ
    public Color selectedForDeleteColor = new Color(1f, 0.3f, 0.3f, 0.8f); // สีเมื่อเลือกไอเท็มที่จะลบ
    public Color normalSlotColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);       // สีปกติของ slot

    [Header("References")]
    [SerializeField] private CombatUIManager combatUIManager;
    [SerializeField] private InventoryGridManager inventoryGridManager;
    [SerializeField] private Character targetCharacter;

    // State variables
    private bool isDeleteMode = false;
    private int selectedSlotForDelete = -1;
    private InventoryItem itemToDelete = null;
    private List<InventorySlot> allInventorySlots = new List<InventorySlot>();

    // Events
    public System.Action<bool> OnDeleteModeChanged;

    // Properties
    public bool IsDeleteMode => isDeleteMode;
    public bool HasSelectedItemToDelete => selectedSlotForDelete >= 0 && itemToDelete != null;

    #region Unity Lifecycle
    private void Awake()
    {
        SetupUI();
        SetupReferences();
    }

    private void Start()
    {
        // ซ่อน confirm panel ตั้งแต่เริ่มต้น
        if (deleteConfirmPanel != null)
        {
            deleteConfirmPanel.SetActive(false);
        }

        // ✅ เพิ่ม retry mechanism สำหรับหา InventoryGridManager
        StartCoroutine(RetryFindInventoryGridManager());
    }
    #endregion

    #region Setup Methods
    private void SetupUI()
    {
        // Setup Cancel Button
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(CancelDelete);
        }
        else
        {
            Debug.LogError("[ItemDeleteManager] Cancel Button not assigned!");
        }

        // Setup Confirm Button
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(ConfirmDelete);
        }
        else
        {
            Debug.LogError("[ItemDeleteManager] Confirm Button not assigned!");
        }
    }

    private void SetupReferences()
    {
        // หา CombatUIManager ถ้าไม่ได้ assign
        if (combatUIManager == null)
        {
            combatUIManager = FindObjectOfType<CombatUIManager>();
            if (combatUIManager == null)
            {
                Debug.LogError("[ItemDeleteManager] CombatUIManager not found!");
                return;
            }
        }

        // ✅ ปรับปรุงการหา InventoryGridManager
        RefreshInventoryGridManagerReference();

        // Subscribe to inventory events
        InventorySlot.OnSlotClickedForDelete += HandleInventorySlotClicked;

        Debug.Log("[ItemDeleteManager] Setup complete");
    }

    // ✅ เพิ่ม method สำหรับ refresh reference
    private void RefreshInventoryGridManagerReference()
    {
        // หา InventoryGridManager ถ้าไม่ได้ assign หรือเป็น null
        if (inventoryGridManager == null)
        {
            // วิธีที่ 1: หาจาก CombatUIManager
            if (combatUIManager != null)
            {
                inventoryGridManager = combatUIManager.GetInventoryGridManager();
            }

            // วิธีที่ 2: หาจาก scene ทั้งหมด
            if (inventoryGridManager == null)
            {
                inventoryGridManager = FindObjectOfType<InventoryGridManager>();
            }

            // วิธีที่ 3: หาจาก parent objects
            if (inventoryGridManager == null)
            {
                Transform inventoryPanel = FindInventoryPanel();
                if (inventoryPanel != null)
                {
                    inventoryGridManager = inventoryPanel.GetComponentInChildren<InventoryGridManager>();
                }
            }
        }

        if (inventoryGridManager != null)
        {
            Debug.Log($"[ItemDeleteManager] ✅ Found InventoryGridManager: {inventoryGridManager.name}");
        }
        else
        {
            Debug.LogWarning("[ItemDeleteManager] ⚠️ InventoryGridManager still not found!");
        }
    }

    // ✅ เพิ่ม helper method สำหรับหา inventory panel
    private Transform FindInventoryPanel()
    {
        GameObject[] allGameObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allGameObjects)
        {
            if (obj.name.ToLower().Contains("inventory") && obj.name.ToLower().Contains("panel"))
            {
                return obj.transform;
            }
        }
        return null;
    }

    // ✅ เพิ่ม Coroutine สำหรับ retry หา InventoryGridManager
    private IEnumerator RetryFindInventoryGridManager()
    {
        float timeout = 10f;
        float elapsed = 0f;

        while (inventoryGridManager == null && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;

            Debug.Log($"[ItemDeleteManager] Retry finding InventoryGridManager... ({elapsed:F1}s)");
            RefreshInventoryGridManagerReference();

            if (inventoryGridManager != null)
            {
                Debug.Log("[ItemDeleteManager] ✅ Successfully found InventoryGridManager on retry!");
                break;
            }
        }

        if (inventoryGridManager == null)
        {
            Debug.LogError("[ItemDeleteManager] ❌ Failed to find InventoryGridManager after timeout!");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        InventorySlot.OnSlotClickedForDelete -= HandleInventorySlotClicked;
    }
    #endregion

    #region Delete Mode Management
    public void ToggleDeleteMode()
    {
        if (isDeleteMode)
        {
            ExitDeleteMode();
        }
        else
        {
            EnterDeleteMode();
        }
    }

    public void EnterDeleteMode()
    {
        if (isDeleteMode) return;

        Debug.Log("[ItemDeleteManager] 🗑️ Entering delete mode...");

        // ✅ ตรวจสอบและ refresh reference ก่อนเข้าโหมดลบ
        if (inventoryGridManager == null)
        {
            Debug.LogWarning("[ItemDeleteManager] InventoryGridManager not found, attempting to find...");
            RefreshInventoryGridManagerReference();

            if (inventoryGridManager == null)
            {
                Debug.LogError("[ItemDeleteManager] ❌ Cannot enter delete mode: InventoryGridManager not found!");
                return;
            }
        }

        isDeleteMode = true;
        selectedSlotForDelete = -1;
        itemToDelete = null;

        // อัปเดตการแสดงผลของ inventory slots
        UpdateInventorySlotsVisualForDeleteMode();

        // แจ้ง CombatUIManager ว่าเข้าโหมดลบแล้ว
        if (combatUIManager != null)
        {
            combatUIManager.SetDeleteModeActive(true);
        }

        // แจ้ง events
        OnDeleteModeChanged?.Invoke(true);

        Debug.Log("[ItemDeleteManager] ✅ Delete mode activated");
    }

    public void ExitDeleteMode()
    {
        if (!isDeleteMode) return;

        Debug.Log("[ItemDeleteManager] 🚪 Exiting delete mode...");

        isDeleteMode = false;
        selectedSlotForDelete = -1;
        itemToDelete = null;

        // คืนสีปกติให้ inventory slots
        RestoreInventorySlotsNormalVisual();

        // ✅ ซ่อน confirm panel เมื่อออกจากโหมดลบ
        if (deleteConfirmPanel != null)
        {
            deleteConfirmPanel.SetActive(false);
        }

        // แจ้ง CombatUIManager ว่าออกจากโหมดลบแล้ว
        if (combatUIManager != null)
        {
            combatUIManager.SetDeleteModeActive(false);
        }

        // แจ้ง events
        OnDeleteModeChanged?.Invoke(false);

        Debug.Log("[ItemDeleteManager] ✅ Delete mode deactivated");
    }

    private void UpdateInventorySlotsVisualForDeleteMode()
    {
        // ✅ ตรวจสอบและ refresh reference อีกครั้ง
        if (inventoryGridManager == null)
        {
            RefreshInventoryGridManagerReference();
        }

        if (inventoryGridManager == null)
        {
            Debug.LogError("[ItemDeleteManager] ❌ Cannot update slots visual: InventoryGridManager not found!");
            return;
        }

        // ดึง slots ทั้งหมดจาก InventoryGridManager
        allInventorySlots = inventoryGridManager.AllSlots;

        if (allInventorySlots == null || allInventorySlots.Count == 0)
        {
            Debug.LogWarning("[ItemDeleteManager] ⚠️ No inventory slots found in InventoryGridManager!");
            return;
        }

        foreach (InventorySlot slot in allInventorySlots)
        {
            if (slot != null && slot.slotBackground != null)
            {
                // เพิ่มขอบสีแดงเพื่อบอกว่าอยู่ในโหมดลบ
                AddDeleteModeBorder(slot);
            }
        }

        Debug.Log($"[ItemDeleteManager] Updated {allInventorySlots.Count} slots for delete mode");
    }

    private void RestoreInventorySlotsNormalVisual()
    {
        foreach (InventorySlot slot in allInventorySlots)
        {
            if (slot != null && slot.slotBackground != null)
            {
                // คืนสีปกติและลบขอบ
                RemoveDeleteModeBorder(slot);
            }
        }

        Debug.Log("[ItemDeleteManager] Restored normal visual for all slots");
    }

    private void AddDeleteModeBorder(InventorySlot slot)
    {
        // เพิ่ม Outline component สำหรับขอบสีแดง
        var outline = slot.slotBackground.GetComponent<UnityEngine.UI.Outline>();
        if (outline == null)
        {
            outline = slot.slotBackground.gameObject.AddComponent<UnityEngine.UI.Outline>();
        }

        outline.effectColor = deleteModeBorderColor;
        outline.effectDistance = new Vector2(2f, 2f);
        outline.enabled = true;
    }

    private void RemoveDeleteModeBorder(InventorySlot slot)
    {
        var outline = slot.slotBackground.GetComponent<UnityEngine.UI.Outline>();
        if (outline != null)
        {
            outline.enabled = false;
        }

        // คืนสีปกติของ slot
        if (slot.IsEmpty)
        {
            slot.slotBackground.color = slot.emptySlotColor;
        }
        else
        {
            slot.slotBackground.color = slot.filledSlotColor;
        }
    }
    #endregion

    #region Slot Selection for Delete
    private void HandleInventorySlotClicked(int slotIndex)
    {
        // ถ้าไม่ได้อยู่ในโหมดลบ ไม่ต้องทำอะไร
        if (!isDeleteMode) return;

        Debug.Log($"[ItemDeleteManager] Slot {slotIndex} clicked in delete mode");

        // ตรวจสอบว่า slot มีไอเทมหรือไม่
        if (targetCharacter?.GetInventory() == null) return;

        InventoryItem item = targetCharacter.GetInventory().GetItem(slotIndex);
        if (item == null || item.IsEmpty)
        {
            Debug.Log($"[ItemDeleteManager] Slot {slotIndex} is empty, cannot select for delete");
            return;
        }

        // เลือก slot สำหรับลบ
        SelectSlotForDelete(slotIndex, item);
    }

    private void SelectSlotForDelete(int slotIndex, InventoryItem item)
    {
        // ยกเลิกการเลือก slot เก่า
        if (selectedSlotForDelete >= 0)
        {
            DeselectPreviousSlot();
        }

        // เลือก slot ใหม่
        selectedSlotForDelete = slotIndex;
        itemToDelete = item;

        // เปลี่ยนสีของ slot ที่เลือก
        InventorySlot selectedSlot = GetSlotByIndex(slotIndex);
        if (selectedSlot != null && selectedSlot.slotBackground != null)
        {
            selectedSlot.slotBackground.color = selectedForDeleteColor;
        }

        Debug.Log($"[ItemDeleteManager] Selected {item.itemData.ItemName} x{item.stackCount} for delete from slot {slotIndex}");

        // ✅ เพิ่มบรรทัดนี้ - แสดง delete panel ทันทีเมื่อเลือก item
        ShowDeleteConfirmation();
    }


    private void DeselectPreviousSlot()
    {
        if (selectedSlotForDelete < 0) return;

        InventorySlot previousSlot = GetSlotByIndex(selectedSlotForDelete);
        if (previousSlot != null && previousSlot.slotBackground != null)
        {
            // คืนสีปกติ (แต่ยังคงขอบโหมดลบ)
            previousSlot.slotBackground.color = previousSlot.IsEmpty ? previousSlot.emptySlotColor : previousSlot.filledSlotColor;
        }
    }

    private InventorySlot GetSlotByIndex(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= allInventorySlots.Count) return null;
        return allInventorySlots[slotIndex];
    }
    #endregion

    #region Delete Confirmation
    public void ShowDeleteConfirmation()
    {
        if (!HasSelectedItemToDelete)
        {
            Debug.LogWarning("[ItemDeleteManager] No item selected for delete");
            return;
        }

        // แสดง confirmation panel
        if (deleteConfirmPanel != null)
        {
            deleteConfirmPanel.SetActive(true);
        }

        // อัปเดตข้อมูล item ใน panel
        UpdateDeletePanelItemInfo();

        // อัปเดตข้อความยืนยัน
        UpdateConfirmationMessage();

        Debug.Log($"[ItemDeleteManager] Showing delete confirmation for {itemToDelete.itemData.ItemName}");
    }
    private void UpdateDeletePanelItemInfo()
    {
        if (itemToDelete == null || itemToDelete.itemData == null) return;

        ItemData itemData = itemToDelete.itemData;

        // อัปเดตรูป item (ถ้ามี deleteItemIcon ใน Inspector)
        Image itemIcon = deleteConfirmPanel.transform.Find("ItemIcon")?.GetComponent<Image>();
        if (itemIcon != null)
        {
            itemIcon.sprite = itemData.ItemIcon;
            itemIcon.color = Color.white;
            itemIcon.gameObject.SetActive(true);
        }

        // อัปเดตชื่อ item
        TextMeshProUGUI itemNameText = deleteConfirmPanel.transform.Find("ItemNameText")?.GetComponent<TextMeshProUGUI>();
        if (itemNameText != null)
        {
            itemNameText.text = itemData.ItemName;
            itemNameText.color = itemData.GetTierColor();
        }

        // อัปเดตประเภท item
        TextMeshProUGUI itemTypeText = deleteConfirmPanel.transform.Find("ItemTypeText")?.GetComponent<TextMeshProUGUI>();
        if (itemTypeText != null)
        {
            itemTypeText.text = $"{itemData.ItemType} ({itemData.GetTierText()})";
            itemTypeText.color = itemData.GetTierColor();
        }

        // อัปเดตจำนวน item
        TextMeshProUGUI stackText = deleteConfirmPanel.transform.Find("StackText")?.GetComponent<TextMeshProUGUI>();
        if (stackText != null)
        {
            if (itemToDelete.stackCount > 1)
            {
                stackText.text = $"x{itemToDelete.stackCount}";
                stackText.gameObject.SetActive(true);
            }
            else
            {
                stackText.gameObject.SetActive(false);
            }
        }

        // อัปเดต tier background (ถ้ามี)
        Image tierBg = deleteConfirmPanel.transform.Find("TierBackground")?.GetComponent<Image>();
        if (tierBg != null)
        {
            tierBg.color = itemData.GetTierColor();
            tierBg.enabled = true;
        }

        Debug.Log($"[ItemDeleteManager] Updated delete panel info for: {itemData.ItemName}");
    }
    private void UpdateConfirmationMessage()
    {
        if (confirmMessageText != null && itemToDelete != null)
        {
            string message = $"Are you sure you want to delete {itemToDelete.itemData.ItemName}";
            if (itemToDelete.stackCount > 1)
            {
                message += $" x{itemToDelete.stackCount}";
            }
            message += "?";

            confirmMessageText.text = message;
        }
    }

    private void CancelDelete()
    {
        Debug.Log("[ItemDeleteManager] Delete cancelled");

        // ซ่อน confirmation panel
        if (deleteConfirmPanel != null)
        {
            deleteConfirmPanel.SetActive(false);
        }

        // ✅ เปลี่ยนจากการออกโหมดลบ เป็นแค่ clear selection
        ClearCurrentSelection();
    }
    private void ClearCurrentSelection()
    {
        // คืนสีปกติให้ slot ที่เลือกอยู่
        if (selectedSlotForDelete >= 0)
        {
            DeselectPreviousSlot();
        }

        // รีเซ็ตการเลือก
        selectedSlotForDelete = -1;
        itemToDelete = null;

        Debug.Log("[ItemDeleteManager] Cleared current selection, still in delete mode");
    }

    private void ConfirmDelete()
    {
        if (!HasSelectedItemToDelete)
        {
            Debug.LogWarning("[ItemDeleteManager] No item to delete");
            return;
        }

        Debug.Log($"[ItemDeleteManager] Confirming delete of {itemToDelete.itemData.ItemName} x{itemToDelete.stackCount}");

        // ทำการลบไอเท็ม
        bool deleteSuccess = DeleteSelectedItem();

        if (deleteSuccess)
        {
            Debug.Log("[ItemDeleteManager] ✅ Item deleted successfully");

            // ซ่อน confirmation panel
            if (deleteConfirmPanel != null)
            {
                deleteConfirmPanel.SetActive(false);
            }

            // ออกจากโหมดลบ
            ExitDeleteMode();

            // อัปเดต UI
            UpdateUIAfterDelete();
        }
        else
        {
            Debug.LogError("[ItemDeleteManager] ❌ Failed to delete item");
        }
    }

    private bool DeleteSelectedItem()
    {
        if (targetCharacter?.GetInventory() == null) return false;

        Inventory inventory = targetCharacter.GetInventory();

        // ลบไอเท็มทั้งหมดใน slot
        bool success = inventory.RemoveItem(selectedSlotForDelete, itemToDelete.stackCount);

        if (success)
        {
            // บันทึกการเปลี่ยนแปลง
            inventory.AutoSaveInventoryData($"DeleteItems - {itemToDelete.itemData.ItemName} x{itemToDelete.stackCount}");
        }

        return success;
    }

    private void UpdateUIAfterDelete()
    {
        // ✅ ตรวจสอบ reference อีกครั้งก่อน update
        if (inventoryGridManager == null)
        {
            RefreshInventoryGridManagerReference();
        }

        // อัปเดต inventory grid
        if (inventoryGridManager != null)
        {
            inventoryGridManager.UpdateSlotFromCharacter(selectedSlotForDelete);
        }
        else
        {
            Debug.LogWarning("[ItemDeleteManager] Cannot update UI: InventoryGridManager not found");
        }

        // Force update canvas
        Canvas.ForceUpdateCanvases();

        Debug.Log("[ItemDeleteManager] UI updated after delete");
    }
    #endregion

    #region Public Methods
    public void SetTargetCharacter(Character character)
    {
        targetCharacter = character;
        Debug.Log($"[ItemDeleteManager] Target character set: {character?.CharacterName ?? "None"}");

        // ✅ เมื่อมี character ใหม่ ให้ refresh reference ด้วย
        if (character != null)
        {
            RefreshInventoryGridManagerReference();
        }
    }

    public bool ShouldBlockItemDetail()
    {
        // ถ้าอยู่ในโหมดลบ ให้ block การแสดง item detail panel
        return isDeleteMode;
    }

    public void OnDeleteButtonClicked()
    {
        if (!isDeleteMode)
        {
            // เข้าโหมดลบ
            EnterDeleteMode();
        }
        else if (HasSelectedItemToDelete)
        {
            // ✅ เปลี่ยนจาก ShowDeleteConfirmation เป็น ConfirmDelete ทันที
            // เพราะ panel แสดงอยู่แล้ว
            ConfirmDelete();
        }
        else
        {
            // ออกจากโหมดลบถ้าไม่ได้เลือกอะไร
            ExitDeleteMode();
        }
    }

    // ✅ เพิ่ม method สำหรับ force refresh reference (เรียกจาก CombatUIManager)
    public void ForceRefreshReferences()
    {
        Debug.Log("[ItemDeleteManager] 🔄 Force refreshing references...");
        RefreshInventoryGridManagerReference();
    }
    #endregion
}