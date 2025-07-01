using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

using UnityEngine.UI;

public class EquipmentSlot : MonoBehaviour
{
    [Header("🎯 Slot Settings (เปลี่ยนได้ใน Inspector)")]
    [SerializeField] private ItemType slotType = ItemType.Head;
    [SerializeField] private int potionSlotIndex = 0; // สำหรับ potion slots เท่านั้น (0-4)

    [Header("UI Components")]
    public Image slotBackground;     // พื้นหลัง slot
    public Image itemIcon;          // ไอคอน item
    public Button slotButton;       // ปุ่มสำหรับ click
    public TextMeshProUGUI slotTypeText; // ข้อความบอกประเภท slot (optional)
    [Header("🧪 Potion Display")]
    public TextMeshProUGUI stackCountText; // สำหรับแสดงจำนวน potion
    [Header("Visual Settings")]
    public Color emptySlotColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color filledSlotColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    public Color selectedSlotColor = new Color(0.8f, 0.8f, 0.2f, 1f);     // สีเหลืองเมื่อถูกเลือก

    [Header("🔍 Debug Info")]
    [SerializeField] private bool isEmpty = true;
    [SerializeField] private bool isSelected = false;

    // References
    private EquipmentSlotManager manager;

    // Events
    public System.Action<EquipmentSlot> OnSlotClicked;

    // Properties
    public ItemType SlotType { get { return slotType; } }
    public int PotionSlotIndex { get { return potionSlotIndex; } }
    public bool IsEmpty { get { return isEmpty; } }
    public bool IsSelected { get { return isSelected; } }

    #region Unity Lifecycle
    private void Awake()
    {
        SetupComponents();
        SetupButton();
        UpdateSlotTypeDisplay();
    }

    private void OnValidate()
    {
        // อัปเดต slot type text เมื่อเปลี่ยนใน inspector
        UpdateSlotTypeDisplay();
    }
    #endregion

    #region Setup
    public void SetManager(EquipmentSlotManager slotManager)
    {
        manager = slotManager;
        Debug.Log($"[EquipmentSlot] {slotType} slot connected to manager");
    }

    private void SetupComponents()
    {
        // หา components อัตโนมัติถ้าไม่ได้ assign
        if (slotBackground == null)
            slotBackground = GetComponent<Image>();

        if (itemIcon == null)
            itemIcon = transform.Find("ItemIcon")?.GetComponent<Image>();

        if (slotButton == null)
            slotButton = GetComponent<Button>();

        if (slotTypeText == null)
            slotTypeText = GetComponentInChildren<TextMeshProUGUI>();

        // สร้าง ItemIcon ถ้าไม่มี
        if (itemIcon == null)
        {
            CreateItemIcon();
        }
        else
        {
            itemIcon.raycastTarget = false;
        }

        // สร้าง SlotTypeText ถ้าต้องการและไม่มี
        if (slotTypeText == null && ShouldShowSlotTypeText())
        {
            CreateSlotTypeText();
        }

        // 🆕 สร้าง StackCountText สำหรับ potion ถ้าไม่มี
        if (stackCountText == null && slotType == ItemType.Potion)
        {
            CreateStackCountText();
        }

        // แน่ใจว่า components ไม่ block button
        if (slotTypeText != null)
            slotTypeText.raycastTarget = false;

        if (stackCountText != null)
            stackCountText.raycastTarget = false;
    }

    private void CreateStackCountText()
    {
        GameObject textObj = new GameObject("StackCountText");
        textObj.transform.SetParent(transform, false);
        stackCountText = textObj.AddComponent<TextMeshProUGUI>();

        RectTransform textRect = stackCountText.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.6f, 0f);
        textRect.anchorMax = new Vector2(1f, 0.4f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        stackCountText.text = "";
        stackCountText.fontSize = 12f;
        stackCountText.color = Color.white;
        stackCountText.fontStyle = FontStyles.Bold;
        stackCountText.alignment = TextAlignmentOptions.BottomRight;
        stackCountText.raycastTarget = false;

        // เพิ่ม outline สำหรับอ่านง่าย
        var outline = textObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, -1);

        // ซ่อนไว้ก่อน
        stackCountText.gameObject.SetActive(false);

        Debug.Log($"[EquipmentSlot] Created StackCountText for {slotType} slot");
    }
    public void SetSelectedState(bool selected)
    {
        isSelected = selected;

        if (slotBackground != null)
        {
            if (isSelected)
            {
                slotBackground.color = selectedSlotColor;
                Debug.Log($"[EquipmentSlot] {slotType} slot selected");
            }
            else
            {
                // กลับไปสีปกติตามสถานะ empty/filled
                slotBackground.color = isEmpty ? emptySlotColor : filledSlotColor;
                Debug.Log($"[EquipmentSlot] {slotType} slot deselected");
            }
        }
    }
    private void CreateItemIcon()
    {
        GameObject iconObj = new GameObject("ItemIcon");
        iconObj.transform.SetParent(transform, false);
        itemIcon = iconObj.AddComponent<Image>();

        RectTransform iconRect = itemIcon.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = Vector2.one * 5f;
        iconRect.offsetMax = Vector2.one * -5f;

        // 🆕 สำคัญมาก: ป้องกันการ block button click
        itemIcon.raycastTarget = false;
        itemIcon.preserveAspect = true;
        itemIcon.gameObject.SetActive(false);
    }

    private void CreateSlotTypeText()
    {
        GameObject textObj = new GameObject("SlotTypeText");
        textObj.transform.SetParent(transform, false);
        slotTypeText = textObj.AddComponent<TextMeshProUGUI>();

        RectTransform textRect = slotTypeText.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 0.3f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        slotTypeText.fontSize = 8f;
        slotTypeText.color = Color.white;
        slotTypeText.fontStyle = FontStyles.Bold;
        slotTypeText.alignment = TextAlignmentOptions.Bottom;
        slotTypeText.raycastTarget = false;

        UpdateSlotTypeDisplay();
    }

    private void SetupButton()
    {
        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(OnSlotButtonClicked);
        }
    }
    #endregion

    #region Visual Management
    public void SetEmptyState()
    {
        isEmpty = true;
        isSelected = false;

        if (slotBackground != null)
            slotBackground.color = emptySlotColor;

        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.color = Color.white;
            itemIcon.gameObject.SetActive(false);
        }

        // 🆕 ซ่อน stack count text ด้วย
        if (stackCountText != null)
        {
            stackCountText.gameObject.SetActive(false);
        }

        Debug.Log($"[EquipmentSlot] {slotType} slot set to empty, isEmpty now: {isEmpty}");
    }

    private void UpdatePotionStackCount()
    {
        if (slotType != ItemType.Potion || stackCountText == null)
            return;

        // หา Character จาก manager
        if (manager?.OwnerCharacter == null)
            return;

        // ดึง potion จาก character
        ItemData potionData = manager.OwnerCharacter.GetPotionInSlot(potionSlotIndex);
        if (potionData == null)
        {
            // 🆕 ถ้าไม่มี potion แสดงว่า slot ว่างแล้ว
            Debug.Log($"[EquipmentSlot] Potion slot {potionSlotIndex} is now empty");
            stackCountText.gameObject.SetActive(false);

            // 🆕 เปลี่ยน slot เป็น empty state
            SetEmptyState();
            return;
        }

        // ดึง stack count จาก character
        int stackCount = manager.OwnerCharacter.GetPotionStackCount(potionSlotIndex);

        Debug.Log($"[EquipmentSlot] 🧪 Updating potion slot {potionSlotIndex}: {potionData.ItemName} x{stackCount}");

        if (stackCount > 1)
        {
            stackCountText.text = stackCount.ToString();
            stackCountText.gameObject.SetActive(true);
            Debug.Log($"[EquipmentSlot] 📊 Updated potion stack count: {stackCount}");
        }
        else if (stackCount == 1)
        {
            // ไม่แสดงเลข 1
            stackCountText.gameObject.SetActive(false);
            Debug.Log($"[EquipmentSlot] 📊 Hidden stack count for single potion");
        }
        else
        {
            // ถ้า stack count = 0 แสดงว่าหมดแล้ว
            stackCountText.gameObject.SetActive(false);
            Debug.Log($"[EquipmentSlot] ⚠️ Potion stack depleted! Setting slot to empty");

            // 🆕 เปลี่ยน slot เป็น empty state
            SetEmptyState();
        }
    }

    // 🆕 เพิ่ม method สำหรับหาจำนวน potion ทั้งหมด
    private int GetTotalPotionCount(ItemData potionData)
    {
        // 🆕 ใช้ stack count จาก character แทน
        if (manager?.OwnerCharacter == null)
            return 1;

        return manager.OwnerCharacter.GetPotionStackCount(potionSlotIndex);
    }
    public void ForceUpdatePotionInfo()
    {
        if (slotType == ItemType.Potion)
        {
            Debug.Log($"[EquipmentSlot] 🔄 Force updating potion info for slot {potionSlotIndex}...");

            UpdatePotionStackCount();

            // Force update Canvas ทันที
            Canvas.ForceUpdateCanvases();

            Debug.Log($"[EquipmentSlot] ✅ Force updated potion info for slot {potionSlotIndex}");
        }
    }

    public void RefreshFromCharacterData()
    {
        if (manager?.OwnerCharacter == null) return;

        if (slotType == ItemType.Potion)
        {
            // ดึงข้อมูล potion ใหม่จาก character
            ItemData potionData = manager.OwnerCharacter.GetPotionInSlot(potionSlotIndex);

            if (potionData != null)
            {
                // อัปเดต UI
                SetFilledState(potionData.ItemIcon, potionData.GetTierColor());
                Debug.Log($"[EquipmentSlot] 🔄 Refreshed potion slot {potionSlotIndex}: {potionData.ItemName}");
            }
            else
            {
                // Slot ว่าง
                SetEmptyState();
                Debug.Log($"[EquipmentSlot] 🔄 Refreshed potion slot {potionSlotIndex}: EMPTY");
            }
        }
        else
        {
            // สำหรับ equipment อื่นๆ
            ItemData equippedItem = manager.OwnerCharacter.GetEquippedItem(slotType);

            if (equippedItem != null)
            {
                SetFilledState(equippedItem.ItemIcon, equippedItem.GetTierColor());
                Debug.Log($"[EquipmentSlot] 🔄 Refreshed {slotType} slot: {equippedItem.ItemName}");
            }
            else
            {
                SetEmptyState();
                Debug.Log($"[EquipmentSlot] 🔄 Refreshed {slotType} slot: EMPTY");
            }
        }
    }
    public void SetFilledState(Sprite itemSprite, Color tierColor = default)
    {
        if (itemSprite == null)
        {
            Debug.LogWarning($"[EquipmentSlot] Trying to fill {slotType} slot with null sprite!");
            SetEmptyState();
            return;
        }

        isSelected = false;
        isEmpty = false;

        if (slotBackground != null)
            slotBackground.color = filledSlotColor;

        if (itemIcon != null)
        {
            itemIcon.sprite = itemSprite;
            itemIcon.color = tierColor != default ? tierColor : Color.white;
            itemIcon.gameObject.SetActive(true);
            itemIcon.raycastTarget = false;
        }

        // 🆕 สำหรับ potion: แสดง stack count จาก character และ force update
        if (slotType == ItemType.Potion && stackCountText != null)
        {
            UpdatePotionStackCount();

            // 🆕 Force update ทันทีหลัง set filled state
            Canvas.ForceUpdateCanvases();
        }

        Debug.Log($"[EquipmentSlot] {slotType} slot filled with item: {itemSprite.name}, isEmpty now: {isEmpty}");
    }
    private bool ShouldShowSlotTypeText()
    {
        // แสดงข้อความประเภท slot เฉพาะ equipment slots (ไม่ใช่ potion slots)
        return slotType != ItemType.Potion;
    }

    private void UpdateSlotTypeDisplay()
    {
        if (slotTypeText != null)
        {
            slotTypeText.text = GetSlotTypeDisplayName();
        }
    }

    private string GetSlotTypeDisplayName()
    {
        switch (slotType)
        {
            case ItemType.Head: return "HEAD";
            case ItemType.Armor: return "ARMOR";
            case ItemType.Weapon: return "WEAPON";
            case ItemType.Pants: return "PANTS";
            case ItemType.Shoes: return "SHOES";
            case ItemType.Rune: return "RUNE";
            case ItemType.Potion: return $"P{potionSlotIndex + 1}"; // P1, P2, P3, P4, P5
            default: return slotType.ToString().ToUpper();
        }
    }
    #endregion

    #region Button Events
    private void OnSlotButtonClicked()
    {
        SetSelectedState(!isSelected); // Toggle selected state

        Debug.Log($"[EquipmentSlot] {slotType} slot clicked - Empty: {isEmpty}");
        Debug.Log($"[EquipmentSlot] ItemIcon raycastTarget: {itemIcon?.raycastTarget ?? false}");
        Debug.Log($"[EquipmentSlot] ItemIcon active: {itemIcon?.gameObject.activeSelf ?? false}");

        // แจ้ง Manager
        OnSlotClicked?.Invoke(this);

        // 🆕 ถ้า slot นี้มีไอเทม ให้แสดงรายละเอียด
        if (!isEmpty)
        {
            ShowItemDetailForThisSlot();
        }
        else
        {
            Debug.Log($"[EquipmentSlot] {slotType} slot is empty, not showing item detail");
        }
    }
    private void ShowItemDetailForThisSlot()
    {
        Debug.Log($"[EquipmentSlot] Attempting to show item detail for {slotType} slot, isEmpty: {isEmpty}");

        // หา CombatUIManager เพื่อแสดง item detail
        CombatUIManager uiManager = FindObjectOfType<CombatUIManager>();
        if (uiManager?.itemDetailManager == null)
        {
            Debug.LogWarning("[EquipmentSlot] CombatUIManager or ItemDetailManager not found");
            return;
        }

        // หา Character ผ่าน CombatUIManager
        Character character = uiManager.localHero;
        if (character == null)
        {
            Debug.LogWarning("[EquipmentSlot] No character found from CombatUIManager");
            return;
        }

        // 🆕 Debug character's equipped items
        DebugCharacterEquippedItems(character);

        ItemData itemData = null;

        // ดึง item data ตาม slot type
        if (slotType == ItemType.Potion)
        {
            itemData = character.GetPotionInSlot(potionSlotIndex);
        }
        else
        {
            itemData = character.GetEquippedItem(slotType);
        }

        if (itemData == null)
        {
            Debug.LogWarning($"[EquipmentSlot] No item found in {slotType} slot");

            // 🆕 Debug เพิ่มเติม
            Debug.LogWarning($"[EquipmentSlot] Character equipped items count: {character.GetAllEquippedItems().Count}");
            return;
        }

        // สร้าง InventoryItem สำหรับ ItemDetailPanel (เหมือน InventorySlot)
        InventoryItem displayItem = new InventoryItem(itemData, 1, -1);

        // แสดง item detail ผ่าง CombatUIManager (เหมือน InventorySlot)
        uiManager.itemDetailManager.ShowItemDetail(displayItem);
        Debug.Log($"[EquipmentSlot] Requested item detail for: {itemData.ItemName}");
    }

    // 🆕 เพิ่ม method สำหรับ debug equipped items
    private void DebugCharacterEquippedItems(Character character)
    {
        Debug.Log($"=== DEBUG CHARACTER EQUIPPED ITEMS ({character.CharacterName}) ===");

        // ตรวจสอบแต่ละ equipment slot
        for (int i = 0; i < 6; i++)
        {
            ItemType itemType = GetItemTypeFromSlotIndex(i);
            ItemData equippedItem = character.GetEquippedItem(itemType);
            Debug.Log($"Slot {i} ({itemType}): {(equippedItem?.ItemName ?? "NULL")}");
        }

        // ตรวจสอบ potion slots
        for (int i = 0; i < 5; i++)
        {
            ItemData potionItem = character.GetPotionInSlot(i);
            Debug.Log($"Potion {i}: {(potionItem?.ItemName ?? "NULL")}");
        }
    }

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
    #endregion

    #region Public Methods
    public void ForceRefresh()
    {
        if (manager != null)
        {
            manager.UpdateSlotFromCharacter(this);
        }
    }

    // เปลี่ยน slot type (สำหรับใช้ใน runtime ถ้าต้องการ)
    public void SetSlotType(ItemType newType, int newPotionIndex = 0)
    {
        slotType = newType;
        potionSlotIndex = newPotionIndex;

        Debug.Log($"[EquipmentSlot] Changed slot type to: {slotType}, potion index: {potionSlotIndex}");

        // สร้าง StackCountText ถ้าเป็น potion และยังไม่มี
        if (slotType == ItemType.Potion && stackCountText == null)
        {
            CreateStackCountText();
        }

        UpdateSlotTypeDisplay();
    }

    #endregion

    #region Context Menu for Testing
#if UNITY_EDITOR
    
#endif
    #endregion
}