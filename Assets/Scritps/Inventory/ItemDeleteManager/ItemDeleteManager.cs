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
    // ✅ แทนที่ method เดิมทั้งหมด
    private void RefreshInventoryGridManagerReference()
    {
        Debug.Log("[ItemDeleteManager] 🔍 Attempting to find InventoryGridManager...");

        // วิธีที่ 1: หาจาก CombatUIManager
        if (inventoryGridManager == null && combatUIManager != null)
        {
            inventoryGridManager = combatUIManager.GetInventoryGridManager();
            if (inventoryGridManager != null)
            {
                Debug.Log("[ItemDeleteManager] ✅ Found via CombatUIManager.GetInventoryGridManager()");
                return;
            }
        }

        // วิธีที่ 2: หาจาก scene ทั้งหมด
        if (inventoryGridManager == null)
        {
            inventoryGridManager = FindObjectOfType<InventoryGridManager>(true); // true = include inactive
            if (inventoryGridManager != null)
            {
                Debug.Log("[ItemDeleteManager] ✅ Found via FindObjectOfType (including inactive)");
                return;
            }
        }

        // วิธีที่ 3: หาจาก Inventory Panel
        if (inventoryGridManager == null && combatUIManager != null)
        {
            // หา inventory panel จาก CombatUIManager
            if (combatUIManager.inventoryPanel != null)
            {
                inventoryGridManager = combatUIManager.inventoryPanel.GetComponentInChildren<InventoryGridManager>(true);
                if (inventoryGridManager != null)
                {
                    Debug.Log("[ItemDeleteManager] ✅ Found in CombatUIManager.inventoryPanel");
                    return;
                }
            }
        }

        // วิธีที่ 4: หาจาก inventoryGridParent
        if (inventoryGridManager == null && combatUIManager != null)
        {
            if (combatUIManager.inventoryGridParent != null)
            {
                inventoryGridManager = combatUIManager.inventoryGridParent.GetComponent<InventoryGridManager>();
                if (inventoryGridManager != null)
                {
                    Debug.Log("[ItemDeleteManager] ✅ Found in CombatUIManager.inventoryGridParent");
                    return;
                }
            }
        }

        // วิธีที่ 5: หาทุก InventoryGridManager ใน scene
        if (inventoryGridManager == null)
        {
            InventoryGridManager[] allGridManagers = FindObjectsOfType<InventoryGridManager>(true);
            if (allGridManagers.Length > 0)
            {
                inventoryGridManager = allGridManagers[0];
                Debug.Log($"[ItemDeleteManager] ✅ Found {allGridManagers.Length} InventoryGridManager(s), using first one");
                return;
            }
        }

        Debug.LogError("[ItemDeleteManager] ❌ InventoryGridManager NOT FOUND after all attempts!");
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

    // ✅ แทนที่ method เดิม
    public void EnterDeleteMode()
    {
        if (isDeleteMode)
        {
            Debug.Log("[ItemDeleteManager] Already in delete mode");
            return;
        }

        Debug.Log("[ItemDeleteManager] 🗑️ Entering delete mode...");
        Debug.Log($"[ItemDeleteManager] combatUIManager: {(combatUIManager != null ? "OK" : "NULL")}");
        Debug.Log($"[ItemDeleteManager] inventoryGridManager: {(inventoryGridManager != null ? "OK" : "NULL")}");
        Debug.Log($"[ItemDeleteManager] targetCharacter: {(targetCharacter != null ? targetCharacter.CharacterName : "NULL")}");

        // ✅ ถ้าไม่มี InventoryGridManager ให้ force refresh
        if (inventoryGridManager == null)
        {
            Debug.LogWarning("[ItemDeleteManager] InventoryGridManager not found, attempting to find...");

            // ลองหาอีกครั้ง
            RefreshInventoryGridManagerReference();

            // ถ้ายังหาไม่เจอ ให้ขอจาก CombatUIManager
            if (inventoryGridManager == null && combatUIManager != null)
            {
                Debug.Log("[ItemDeleteManager] Requesting InventoryGridManager from CombatUIManager...");
                inventoryGridManager = combatUIManager.GetInventoryGridManager();
            }

            // ถ้ายังหาไม่เจอ แสดง error
            if (inventoryGridManager == null)
            {
                Debug.LogError("[ItemDeleteManager] ❌ Cannot enter delete mode: InventoryGridManager not found!");
                Debug.LogError("[ItemDeleteManager] Please make sure:");
                Debug.LogError("  1. InventoryPanel is active in hierarchy");
                Debug.LogError("  2. InventoryGridManager component exists");
                Debug.LogError("  3. CombatUIManager.inventoryGridManager is assigned");
                return;
            }
            else
            {
                Debug.Log("[ItemDeleteManager] ✅ Successfully found InventoryGridManager!");
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

        // เล่นเสียง
        AudioManager.instance?.PlaySFX(7, 0.8f);

        Debug.Log("[ItemDeleteManager] ✅ Delete mode activated - Click any item to delete");
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
        Debug.Log($"[ItemDeleteManager] Selecting slot {slotIndex} for delete");

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

        Debug.Log($"[ItemDeleteManager] Selected {item.itemData.ItemName} x{item.stackCount} for delete");

        // ✅ แสดง delete confirmation panel ทันที
        ShowDeleteConfirmation();

        // ✅ เล่นเสียง
        AudioManager.instance?.PlaySFX(7, 0.8f);
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
    // ✅ แทนที่ method เดิม
    public void ShowDeleteConfirmation()
    {
        Debug.Log("[ItemDeleteManager] ShowDeleteConfirmation called");
        Debug.Log($"[ItemDeleteManager] HasSelectedItemToDelete: {HasSelectedItemToDelete}");
        Debug.Log($"[ItemDeleteManager] deleteConfirmPanel: {deleteConfirmPanel != null}");

        if (!HasSelectedItemToDelete)
        {
            Debug.LogWarning("[ItemDeleteManager] ❌ No item selected for delete");
            return;
        }

        if (deleteConfirmPanel == null)
        {
            Debug.LogError("[ItemDeleteManager] ❌ deleteConfirmPanel is NULL!");
            return;
        }

        // แสดง confirmation panel
        deleteConfirmPanel.SetActive(true);
        Debug.Log($"[ItemDeleteManager] ✅ Panel activated: {deleteConfirmPanel.activeSelf}");

        // อัปเดตข้อมูล item ใน panel
        UpdateDeletePanelItemInfo();

        // อัปเดตข้อความยืนยัน
        UpdateConfirmationMessage();

        // เล่นเสียง
        AudioManager.instance?.PlaySFX(7, 1f);

        Debug.Log($"[ItemDeleteManager] ✅ Showing delete confirmation for {itemToDelete.itemData.ItemName}");
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

    // ✅ แทนที่ method เดิม
    private void CancelDelete()
    {
        Debug.Log("[ItemDeleteManager] ❌ Delete cancelled");

        // ซ่อน confirmation panel
        if (deleteConfirmPanel != null)
        {
            deleteConfirmPanel.SetActive(false);
        }

        // Clear selection แต่ยังอยู่ในโหมดลบ
        ClearCurrentSelection();

        // เล่นเสียง
        AudioManager.instance?.PlaySFX(7, 0.5f);
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

    // ✅ แทนที่ method เดิม
    private void ConfirmDelete()
    {
        if (!HasSelectedItemToDelete)
        {
            Debug.LogWarning("[ItemDeleteManager] ❌ No item to delete");
            return;
        }

        Debug.Log($"[ItemDeleteManager] 🗑️ Confirming delete of {itemToDelete.itemData.ItemName} x{itemToDelete.stackCount}");

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

            // เล่นเสียงลบ
            AudioManager.instance?.PlaySFX(8, 1f);
        }
        else
        {
            Debug.LogError("[ItemDeleteManager] ❌ Failed to delete item");

            // เล่นเสียง error
            AudioManager.instance?.PlaySFX(9, 1f);
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

    // ✅ แทนที่ method เดิมทั้งหมด
    // ✅ แทนที่ method เดิม - เวอร์ชันเบา
    private void UpdateUIAfterDelete()
    {
        Debug.Log("[ItemDeleteManager] 🔄 Updating UI after delete...");

        // ตรวจสอบ reference
        if (inventoryGridManager == null)
        {
            RefreshInventoryGridManagerReference();
        }

        if (inventoryGridManager == null)
        {
            Debug.LogWarning("[ItemDeleteManager] Cannot update UI: InventoryGridManager not found");
            return;
        }

        // ✅ อัพเดทเฉพาะ slot ที่ลบ (ไม่ต้อง refresh ทั้งหมด)
        if (selectedSlotForDelete >= 0)
        {
            inventoryGridManager.UpdateSlotFromCharacter(selectedSlotForDelete);
        }

        // ✅ Force hide extra slots เท่านั้น (ไม่ต้อง force refresh all)
        inventoryGridManager.ForceHideExtraSlots();

        // ✅ Canvas update ครั้งเดียว
        Canvas.ForceUpdateCanvases();

        Debug.Log("[ItemDeleteManager] ✅ UI updated after delete");
    }

    // ✅ เพิ่ม Coroutine สำหรับ delayed refresh
    private IEnumerator DelayedCanvasRefresh()
    {
        // รอ 3 frames แล้ว refresh ซ้ำ
        yield return null;
        yield return null;
        yield return null;

        if (inventoryGridManager != null)
        {
            inventoryGridManager.ForceRefreshOnOpen();
            Canvas.ForceUpdateCanvases();
            Debug.Log("[ItemDeleteManager] Delayed canvas refresh complete");
        }
    }
    #endregion

    #region Public Methods
    public void SetTargetCharacter(Character character)
    {
        targetCharacter = character;
        Debug.Log($"[ItemDeleteManager] Target character set: {character?.CharacterName ?? "None"}");

        // ✅ เมื่อมี character ใหม่ ให้ force refresh ทุกอย่าง
        if (character != null)
        {
            ForceRefreshReferences();
        }
    }

    public bool ShouldBlockItemDetail()
    {
        // ถ้าอยู่ในโหมดลบ ให้ block การแสดง item detail panel
        return isDeleteMode;
    }

    public void OnDeleteButtonClicked()
    {
        Debug.Log("[ItemDeleteManager] 🗑️ Delete button clicked!");
        Debug.Log($"[ItemDeleteManager] Current state - isDeleteMode: {isDeleteMode}, hasSelection: {HasSelectedItemToDelete}");

        if (!isDeleteMode)
        {
            // เข้าโหมดลบ
            EnterDeleteMode();
        }
        else
        {
            // ออกจากโหมดลบ (กดถังขยะอีกครั้งเพื่อยกเลิก)
            ExitDeleteMode();
        }
    }

    // ✅ เพิ่ม method สำหรับ force refresh reference (เรียกจาก CombatUIManager)
    // ✅ แทนที่ method เดิม
    public void ForceRefreshReferences()
    {
        Debug.Log("[ItemDeleteManager] 🔄 Force refreshing all references...");

        // 1. หา CombatUIManager
        if (combatUIManager == null)
        {
            combatUIManager = FindObjectOfType<CombatUIManager>();
            Debug.Log($"[ItemDeleteManager] CombatUIManager: {(combatUIManager != null ? "Found" : "NOT FOUND")}");
        }

        // 2. หา InventoryGridManager
        RefreshInventoryGridManagerReference();
        Debug.Log($"[ItemDeleteManager] InventoryGridManager: {(inventoryGridManager != null ? "Found" : "NOT FOUND")}");

        // 3. หา Target Character
        if (targetCharacter == null && combatUIManager?.localHero != null)
        {
            targetCharacter = combatUIManager.localHero;
            Debug.Log($"[ItemDeleteManager] Target Character: {targetCharacter.CharacterName}");
        }

        // 4. ดึง slots จาก InventoryGridManager
        if (inventoryGridManager != null)
        {
            allInventorySlots = inventoryGridManager.AllSlots;
            Debug.Log($"[ItemDeleteManager] Found {allInventorySlots?.Count ?? 0} inventory slots");
        }

        Debug.Log("[ItemDeleteManager] ✅ Force refresh complete");
    }
    #endregion
}