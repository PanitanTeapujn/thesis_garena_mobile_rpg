using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class ItemDetailPanel : MonoBehaviour
{
    #region UI References
    [Header("Panel References")]
    public GameObject detailPanel;
    public Button closeButton;
    public Button equipButton;
    public Button unequipButton;

    [Header("Item Display")]
    public Image itemIconImage;
    public Image itemTierBorder;

    [Header("Item Info Text")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI tierText;
    public TextMeshProUGUI typeText;
    public TextMeshProUGUI statText;
    public TextMeshProUGUI descriptionsText;
    #endregion

    #region Current State
    [Header("Current State")]
    public ItemData currentItem;
    public int currentSlotIndex = -1;
    public bool isVisible = false;

    // ✅ เพิ่ม: Button interaction protection
    private bool isProcessingButton = false;
    #endregion

    #region Events
    public static System.Action<ItemData, int> OnEquipRequested;
    public static System.Action<ItemData, int> OnUnequipRequested;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        InitializeComponents();
        SetupButtons();
    }

    void Start()
    {
        HidePanel();
    }

    void OnEnable()
    {
        SubscribeToEvents();
    }

    void OnDisable()
    {
        UnsubscribeFromEvents();
    }
    #endregion

    #region Initialization
    void InitializeComponents()
    {
        Debug.Log("✅ ItemDetailPanel initialized");
    }

    void SetupButtons()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(HidePanel);

        if (equipButton != null)
            equipButton.onClick.AddListener(OnEquipButtonClicked);

        if (unequipButton != null)
            unequipButton.onClick.AddListener(OnUnequipButtonClicked);
    }

    void OnEquipmentSystemChanged()
    {
        // ถ้า panel เปิดอยู่และมี item ให้ refresh buttons
        if (isVisible && currentItem != null)
        {
            Debug.Log($"🔄 Equipment system changed, refreshing buttons for {currentItem.ItemName}");
            ForceUpdateButtons(); // ✅ เปลี่ยนเป็น ForceUpdateButtons
        }
    }

    void SubscribeToEvents()
    {
        // Unsubscribe ก่อนเผื่อมี duplicate
        InventorySlot.OnSlotSelected -= OnInventorySlotSelected;
        EquipmentSlotsManager.OnEquipmentChanged -= OnEquipmentSystemChanged;

        // Subscribe ใหม่
        InventorySlot.OnSlotSelected += OnInventorySlotSelected;
        EquipmentSlotsManager.OnEquipmentChanged += OnEquipmentSystemChanged;

        Debug.Log("✅ ItemDetailPanel subscribed to slot events");
    }

    void UnsubscribeFromEvents()
    {
        InventorySlot.OnSlotSelected -= OnInventorySlotSelected;
        EquipmentSlotsManager.OnEquipmentChanged -= OnEquipmentSystemChanged;

        Debug.Log("📝 ItemDetailPanel unsubscribed from slot events");
    }
    #endregion

    #region Event Handlers - ✅ ปรับปรุงแล้ว
    void OnInventorySlotSelected(InventorySlot slot)
    {
        Debug.Log($"🎯 ItemDetailPanel received slot selection: {slot?.slotIndex}");

        if (slot != null && slot.HasItem())
        {
            ItemData item = slot.GetItem();
            Debug.Log($"📦 Slot has item: {item?.ItemName}");

            // ✅ เปลี่ยน: Force refresh panel ทั้งหมดแทนการ update incremental
            RefreshItemDetailPanel(item, slot.slotIndex);
        }
        else
        {
            Debug.Log("📭 Slot is empty, hiding panel");
            HidePanel();
        }
    }

    private void RefreshItemDetailPanel(ItemData item, int slotIndex)
    {
        Debug.Log($"🔄 Force refreshing ItemDetailPanel for {item.ItemName}");

        // Clear current state ก่อน
        currentItem = null;
        currentSlotIndex = -1;

        // รอ 1 frame เพื่อให้ system clear state
        StartCoroutine(DelayedShowItemDetail(item, slotIndex));
    }

    private System.Collections.IEnumerator DelayedShowItemDetail(ItemData item, int slotIndex)
    {
        // รอ 1 frame เพื่อให้ state clear
        yield return null;

        // แสดง item detail ใหม่
        ShowItemDetail(item, slotIndex);

        Debug.Log($"✅ Refreshed ItemDetailPanel for {item.ItemName}");
    }

    void OnEquipButtonClicked()
    {
        // ✅ Prevent rapid clicking
        if (isProcessingButton)
        {
            Debug.LogWarning("⚠️ Button already processing, ignoring click");
            return;
        }

        StartCoroutine(ProcessEquipButtonClick());
    }

    private IEnumerator ProcessEquipButtonClick()
    {
        isProcessingButton = true;

        try
        {
            Debug.Log($"🎽 OnEquipButtonClicked: {currentItem?.ItemName ?? "NULL"}");

            // ✅ Comprehensive validation
            if (currentItem == null)
            {
                Debug.LogError("❌ Cannot equip: currentItem is null!");
                yield break;
            }

            // ✅ สำหรับ rune: เช็คว่ามี ID เดียวกันใน equipment แล้วหรือยัง
            if (currentItem.ItemType == ItemType.Rune)
            {
                if (IsRuneIdAlreadyEquipped(currentItem.ItemId))
                {
                    Debug.LogWarning($"❌ Rune {currentItem.ItemName} (ID: {currentItem.ItemId}) is already equipped!");
                    ShowEquipError("This rune is already equipped!");
                    yield break;
                }
            }

            // ✅ Check event subscribers
            if (OnEquipRequested == null)
            {
                Debug.LogError("❌ OnEquipRequested has no subscribers!");

                // ✅ Try to reconnect
                var inventoryManager = FindObjectOfType<InventoryManager>();
                if (inventoryManager != null)
                {
                    Debug.Log("🔧 Found InventoryManager, trying to reconnect...");
                    inventoryManager.DebugResetupEvents();

                    // Wait a frame for reconnection
                    yield return null;

                    // Check again
                    if (OnEquipRequested == null)
                    {
                        Debug.LogError("❌ Failed to reconnect events!");
                        yield break;
                    }
                }
                else
                {
                    Debug.LogError("❌ InventoryManager not found!");
                    yield break;
                }
            }

            // ✅ Disable button temporarily
            if (equipButton != null)
            {
                equipButton.interactable = false;
            }

            // ✅ Invoke event with error handling
            try
            {
                OnEquipRequested.Invoke(currentItem, currentSlotIndex);
                Debug.Log($"✅ Equip event fired: {currentItem.ItemName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Error invoking OnEquipRequested: {e.Message}\n{e.StackTrace}");
            }

            // ✅ Wait before re-enabling button
            yield return new WaitForSeconds(0.5f);

            // ✅ Force refresh buttons after equip attempt
            yield return new WaitForSeconds(0.1f);
            ForceUpdateButtons();
        }
        finally
        {
            // ✅ Re-enable button
            if (equipButton != null)
            {
                equipButton.interactable = true;
            }

            isProcessingButton = false;
        }
    }

    void OnUnequipButtonClicked()
    {
        // ✅ Prevent rapid clicking
        if (isProcessingButton)
        {
            Debug.LogWarning("⚠️ Button already processing, ignoring click");
            return;
        }

        StartCoroutine(ProcessUnequipButtonClick());
    }

    private IEnumerator ProcessUnequipButtonClick()
    {
        isProcessingButton = true;

        try
        {
            Debug.Log($"🔧 OnUnequipButtonClicked: {currentItem?.ItemName ?? "NULL"}");

            if (currentItem == null)
            {
                Debug.LogError("❌ Cannot unequip: currentItem is null!");
                yield break;
            }

            if (OnUnequipRequested == null)
            {
                Debug.LogError("❌ OnUnequipRequested has no subscribers!");
                yield break;
            }

            // ✅ Disable button temporarily
            if (unequipButton != null)
            {
                unequipButton.interactable = false;
            }

            try
            {
                OnUnequipRequested.Invoke(currentItem, currentSlotIndex);
                Debug.Log($"✅ Unequip event fired: {currentItem.ItemName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Error invoking OnUnequipRequested: {e.Message}\n{e.StackTrace}");
            }

            yield return new WaitForSeconds(0.5f);

            // ✅ Force refresh buttons after unequip attempt
            yield return new WaitForSeconds(0.1f);
            ForceUpdateButtons();
        }
        finally
        {
            // ✅ Re-enable button
            if (unequipButton != null)
            {
                unequipButton.interactable = false;
            }

            isProcessingButton = false;
        }
    }
    #endregion

    #region Panel Control
    public void ShowItemDetail(ItemData item, int slotIndex)
    {
        Debug.Log($"📋 ShowItemDetail: {item?.ItemName}, slot={slotIndex}");

        if (item == null)
        {
            Debug.LogWarning("❌ Item is null, hiding panel");
            HidePanel();
            return;
        }

        // ✅ เพิ่ม: Force clear previous state ถ้าเป็น item เดียวกัน
        if (currentItem != null && currentItem.ItemName == item.ItemName)
        {
            Debug.Log($"🔄 Same item {item.ItemName}, force refreshing...");
            currentItem = null;
            currentSlotIndex = -1;
        }

        currentItem = item;
        currentSlotIndex = slotIndex;
        isVisible = true;

        UpdateItemDisplay();
        UpdateItemInfo();

        // ✅ เพิ่ม: Force re-evaluate buttons แทนการใช้ cached state
        ForceUpdateButtons();

        if (detailPanel != null)
        {
            detailPanel.SetActive(true);
            Debug.Log($"✅ Detail panel activated for: {item.ItemName}");
        }
        else
        {
            Debug.LogError("❌ detailPanel GameObject is null!");
        }
    }

    // ✅ ใหม่: สำหรับ check rune ID duplication
    private bool IsRuneIdAlreadyEquipped(string runeId)
    {
        var slotsManager = FindObjectOfType<EquipmentSlotsManager>();
        if (slotsManager == null) return false;

        Debug.Log($"🔍 Checking if rune ID '{runeId}' is already equipped...");

        for (int i = 0; i < slotsManager.runeSlots.Length; i++)
        {
            var runeSlot = slotsManager.runeSlots[i];
            if (runeSlot != null && runeSlot.HasEquippedItem())
            {
                var equippedRune = runeSlot.GetEquippedItem();
                if (equippedRune != null && equippedRune.ItemId == runeId)
                {
                    Debug.Log($"❌ Rune ID '{runeId}' already equipped in {runeSlot.slotName}");
                    return true;
                }
            }
        }

        Debug.Log($"✅ Rune ID '{runeId}' not found in any equipment slot");
        return false;
    }

    // ✅ ใหม่: แสดง error message
    private void ShowEquipError(string message)
    {
        Debug.LogWarning($"⚠️ {message}");
        // TODO: แสดง UI popup หรือ notification ถ้าต้องการ
    }

    private void ForceUpdateButtons()
    {
        if (currentItem == null) return;

        Debug.Log($"🔄 Force updating buttons for {currentItem.ItemName}");

        // ✅ Force re-check equipment status แทนการใช้ cached
        bool isEquipped = ForceCheckEquipmentStatus(currentItem);
        bool canEquip = !isEquipped && ForceCheckCanEquip(currentItem) && !isProcessingButton;

        // ✅ สำหรับ rune: เช็คเพิ่มเติมว่ามี ID เดียวกันใน equipment แล้วหรือยัง
        if (currentItem.ItemType == ItemType.Rune && !isEquipped)
        {
            bool runeIdExists = IsRuneIdAlreadyEquipped(currentItem.ItemId);
            if (runeIdExists)
            {
                canEquip = false;
                Debug.Log($"🚫 Cannot equip: Rune ID '{currentItem.ItemId}' already equipped");
            }
        }

        if (equipButton != null)
        {
            equipButton.gameObject.SetActive(!isEquipped);
            equipButton.interactable = canEquip;

            var buttonColors = equipButton.colors;
            buttonColors.normalColor = canEquip ? Color.green : Color.gray;
            equipButton.colors = buttonColors;
        }

        if (unequipButton != null)
        {
            unequipButton.gameObject.SetActive(isEquipped);
            unequipButton.interactable = !isProcessingButton;
        }

        Debug.Log($"🔘 Force updated buttons: isEquipped={isEquipped}, canEquip={canEquip}");
    }

    // ✅ ปรับปรุง: สำหรับ force check (ไม่ใช้ cache)
    private bool ForceCheckEquipmentStatus(ItemData item)
    {
        var slotsManager = FindObjectOfType<EquipmentSlotsManager>();
        if (slotsManager != null)
        {
            // ✅ สำหรับ rune: เช็คจาก ItemId
            if (item.ItemType == ItemType.Rune)
            {
                // เช็คว่า rune นี้ (instance นี้โดยเฉพาะ) อยู่ใน equipment หรือไม่
                // โดยดูจาก currentSlotIndex: ถ้า = -1 แสดงว่าเปิดจาก equipment slot
                if (currentSlotIndex == -1)
                {
                    Debug.Log($"✅ Rune {item.ItemName} shown from equipment slot - IS EQUIPPED");
                    return true;
                }
                else
                {
                    Debug.Log($"✅ Rune {item.ItemName} shown from inventory slot {currentSlotIndex} - NOT EQUIPPED");
                    return false;
                }
            }
            else
            {
                // สำหรับ equipment อื่น: ใช้วิธีเดิม
                foreach (var slot in slotsManager.allSlots)
                {
                    if (slot != null && slot.HasEquippedItem())
                    {
                        var equippedItem = slot.GetEquippedItem();
                        if (equippedItem != null &&
                            equippedItem.ItemId == item.ItemId &&
                            equippedItem.ItemName == item.ItemName)
                        {
                            Debug.Log($"✅ Force check: {item.ItemName} found in {slot.slotName}");
                            return true;
                        }
                    }
                }
            }
        }

        Debug.Log($"✅ Force check: {item.ItemName} not equipped anywhere");
        return false;
    }

    // ✅ ปรับปรุง method สำหรับ force check can equip
    private bool ForceCheckCanEquip(ItemData item)
    {
        var slotsManager = FindObjectOfType<EquipmentSlotsManager>();
        if (slotsManager != null)
        {
            if (item.ItemType == ItemType.Rune)
            {
                // ✅ เช็คว่ามีช่องรูนว่างหรือไม่ AND ไม่มี rune ID เดียวกันใน equipment
                for (int i = 0; i < slotsManager.runeSlots.Length; i++)
                {
                    var runeSlot = slotsManager.runeSlots[i];
                    if (runeSlot != null && runeSlot.isEmpty && !runeSlot.HasEquippedItem())
                    {
                        Debug.Log($"✅ Force check: Found empty rune slot {i + 1}");

                        // ✅ เช็คเพิ่มเติมว่าไม่มี rune ID เดียวกันใน equipment
                        bool runeIdExists = IsRuneIdAlreadyEquipped(item.ItemId);
                        if (!runeIdExists)
                        {
                            return true;
                        }
                        else
                        {
                            Debug.Log($"🚫 Rune ID '{item.ItemId}' already equipped, cannot equip duplicate");
                            return false;
                        }
                    }
                }
                return false;
            }
            else
            {
                var targetSlot = slotsManager.GetSlotForItemType(item.ItemType);
                return targetSlot != null && targetSlot.isEmpty && !targetSlot.HasEquippedItem();
            }
        }
        return false;
    }

    public void HidePanel()
    {
        currentItem = null;
        currentSlotIndex = -1;
        isVisible = false;

        // ✅ Reset processing flag
        isProcessingButton = false;

        if (detailPanel != null)
            detailPanel.SetActive(false);

        Debug.Log("📋 Item detail panel hidden");
    }
    #endregion

    #region UI Update Methods
    void UpdateItemDisplay()
    {
        if (currentItem == null) return;

        // Update item icon
        if (itemIconImage != null && currentItem.ItemIcon != null)
        {
            itemIconImage.sprite = currentItem.ItemIcon;
        }

        // Update tier border color
        if (itemTierBorder != null)
        {
            itemTierBorder.color = currentItem.GetTierColor();
        }
    }

    void UpdateItemInfo()
    {
        if (currentItem == null) return;

        // Update Name
        if (nameText != null)
            nameText.text = $"{currentItem.ItemName}";

        // Update Tier
        if (tierText != null)
        {
            tierText.text = $"{currentItem.GetTierText()}";
            tierText.color = currentItem.GetTierColor();
        }

        // Update Type
        if (typeText != null)
            typeText.text = $"{GetItemTypeDisplayName(currentItem.ItemType)}";

        // Update Stats
        if (statText != null)
        {
            string statsInfo = GetStatsDisplayText(currentItem.Stats);
            statText.text = $"{statsInfo}";
        }

        // Update Descriptions
        if (descriptionsText != null)
            descriptionsText.text = $"{currentItem.Description}";
    }

    public void RefreshButtonState()
    {
        if (currentItem == null) return;

        Debug.Log($"🔄 RefreshButtonState for {currentItem.ItemName}");
        ForceUpdateButtons(); // ✅ เปลี่ยนเป็น ForceUpdateButtons
    }

    string GetStatsDisplayText(ItemStats stats)
    {
        if (!stats.HasAnyStats())
            return "No bonus stats";

        var statsList = new List<string>();

        // Combat Stats
        if (stats.attackDamageBonus != 0)
            statsList.Add($"Attack: +{stats.attackDamageBonus}");
        if (stats.magicDamageBonus != 0)
            statsList.Add($"Magic: +{stats.magicDamageBonus}");
        if (stats.armorBonus != 0)
            statsList.Add($"Armor: +{stats.armorBonus}");
        if (stats.criticalChanceBonus != 0f)
            statsList.Add($"Crit Chance: +{stats.criticalChanceBonus:P1}");
        if (stats.criticalDamageBonus != 0f)
            statsList.Add($"Crit Damage: +{stats.criticalDamageBonus:P1}");

        // Survival Stats
        if (stats.maxHpBonus != 0)
            statsList.Add($"HP: +{stats.maxHpBonus}");
        if (stats.maxManaBonus != 0)
            statsList.Add($"Mana: +{stats.maxManaBonus}");
        if (stats.moveSpeedBonus != 0f)
            statsList.Add($"Move Speed: +{stats.moveSpeedBonus:F1}");
        if (stats.attackSpeedBonus != 0f)
            statsList.Add($"Attack Speed: +{stats.attackSpeedBonus:P1}");
        if (stats.hitRateBonus != 0f)
            statsList.Add($"Hit Rate: +{stats.hitRateBonus:P1}");
        if (stats.evasionRateBonus != 0f)
            statsList.Add($"Evasion: +{stats.evasionRateBonus:P1}");

        // Special Stats
        if (stats.reductionCoolDownBonus != 0f)
            statsList.Add($"Cooldown: -{stats.reductionCoolDownBonus:P1}");
        if (stats.physicalResistanceBonus != 0f)
            statsList.Add($"Physical Res: +{stats.physicalResistanceBonus:P1}");
        if (stats.magicalResistanceBonus != 0f)
            statsList.Add($"Magical Res: +{stats.magicalResistanceBonus:P1}");

        return string.Join("\n", statsList);
    }

    // ✅ ลบ method เก่าที่ไม่ใช้แล้ว - UpdateButtons, CanEquipItem, IsItemCurrentlyEquipped

    string GetItemTypeDisplayName(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Weapon: return "Weapon";
            case ItemType.Head: return "Head Armor";
            case ItemType.Armor: return "Body Armor";
            case ItemType.Pants: return "Leg Armor";
            case ItemType.Shoes: return "Boots";
            case ItemType.Rune: return "Rune";
            default: return itemType.ToString();
        }
    }
    #endregion

    #region Public Methods
    public bool IsVisible()
    {
        return isVisible;
    }

    public ItemData GetCurrentItem()
    {
        return currentItem;
    }

    public void ForceHide()
    {
        HidePanel();
    }
    #endregion

    #region Debug Methods
    [ContextMenu("Debug Panel State")]
    public void DebugPanelState()
    {
        Debug.Log($"📋 ItemDetailPanel Debug:");
        Debug.Log($"   detailPanel: {(detailPanel != null ? "✅ Assigned" : "❌ NULL")}");
        Debug.Log($"   isVisible: {isVisible}");
        Debug.Log($"   currentItem: {currentItem?.ItemName ?? "NULL"}");
        Debug.Log($"   currentSlotIndex: {currentSlotIndex}");
        Debug.Log($"   isProcessingButton: {isProcessingButton}");
        Debug.Log($"   Panel Active: {(detailPanel != null ? detailPanel.activeSelf.ToString() : "N/A")}");

        // Debug event subscribers
        Debug.Log($"   OnEquipRequested subscribers: {(OnEquipRequested?.GetInvocationList()?.Length ?? 0)}");
        Debug.Log($"   OnUnequipRequested subscribers: {(OnUnequipRequested?.GetInvocationList()?.Length ?? 0)}");
    }

    [ContextMenu("Test - Check Event Subscribers")]
    public void TestCheckEventSubscribers()
    {
        Debug.Log("🔍 === EVENT SUBSCRIBERS CHECK ===");

        if (OnEquipRequested != null)
        {
            var subscribers = OnEquipRequested.GetInvocationList();
            Debug.Log($"✅ OnEquipRequested has {subscribers.Length} subscriber(s):");
            foreach (var subscriber in subscribers)
            {
                Debug.Log($"   - {subscriber.Target?.GetType().Name}.{subscriber.Method.Name}");
            }
        }
        else
        {
            Debug.Log("❌ OnEquipRequested has NO subscribers!");
        }

        if (OnUnequipRequested != null)
        {
            var subscribers = OnUnequipRequested.GetInvocationList();
            Debug.Log($"✅ OnUnequipRequested has {subscribers.Length} subscriber(s):");
            foreach (var subscriber in subscribers)
            {
                Debug.Log($"   - {subscriber.Target?.GetType().Name}.{subscriber.Method.Name}");
            }
        }
        else
        {
            Debug.Log("❌ OnUnequipRequested has NO subscribers!");
        }

        Debug.Log("🔍 === END EVENT SUBSCRIBERS CHECK ===");
    }

    [ContextMenu("Test - Force Reconnect Events")]
    public void TestForceReconnectEvents()
    {
        var inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager != null)
        {
            inventoryManager.DebugResetupEvents();
            Debug.Log("🔧 Forced event reconnection");
        }
        else
        {
            Debug.LogError("❌ InventoryManager not found!");
        }
    }

    [ContextMenu("Test - Check Rune Duplicates")]
    public void TestCheckRuneDuplicates()
    {
        if (currentItem == null || currentItem.ItemType != ItemType.Rune)
        {
            Debug.Log("❌ No rune item selected");
            return;
        }

        bool isDuplicate = IsRuneIdAlreadyEquipped(currentItem.ItemId);
        Debug.Log($"🔍 Rune {currentItem.ItemName} (ID: {currentItem.ItemId}) duplicate check: {isDuplicate}");
    }
    #endregion
}