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

    [Header("Visual Settings")]
    public Color emptySlotColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color filledSlotColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Header("🔍 Debug Info")]
    [SerializeField] private bool isEmpty = true;

    // References
    private EquipmentSlotManager manager;

    // Events
    public System.Action<EquipmentSlot> OnSlotClicked;

    // Properties
    public ItemType SlotType { get { return slotType; } }
    public int PotionSlotIndex { get { return potionSlotIndex; } }
    public bool IsEmpty { get { return isEmpty; } }

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
            // 🆕 ถ้ามี itemIcon อยู่แล้ว ให้แน่ใจว่า raycastTarget = false
            itemIcon.raycastTarget = false;
        }

        // สร้าง SlotTypeText ถ้าต้องการและไม่มี
        if (slotTypeText == null && ShouldShowSlotTypeText())
        {
            CreateSlotTypeText();
        }

        // 🆕 แน่ใจว่า slotTypeText ไม่ block button
        if (slotTypeText != null)
        {
            slotTypeText.raycastTarget = false;
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
        isEmpty = true; // 🆕 ตั้งค่า isEmpty = true

        if (slotBackground != null)
            slotBackground.color = emptySlotColor;

        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.color = Color.white;
            itemIcon.gameObject.SetActive(false);
        }

        Debug.Log($"[EquipmentSlot] {slotType} slot set to empty, isEmpty now: {isEmpty}");
    }

    public void SetFilledState(Sprite itemSprite, Color tierColor = default)
    {
        if (itemSprite == null)
        {
            Debug.LogWarning($"[EquipmentSlot] Trying to fill {slotType} slot with null sprite!");
            SetEmptyState();
            return;
        }

        isEmpty = false;

        if (slotBackground != null)
            slotBackground.color = filledSlotColor;

        if (itemIcon != null)
        {
            itemIcon.sprite = itemSprite;
            itemIcon.color = tierColor != default ? tierColor : Color.white;
            itemIcon.gameObject.SetActive(true);

            // 🆕 แน่ใจว่า raycastTarget = false เสมอ
            itemIcon.raycastTarget = false;
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
        UpdateSlotTypeDisplay();

        Debug.Log($"[EquipmentSlot] Changed slot type to: {slotType}");
    }
    #endregion

    #region Context Menu for Testing
   
    #endregion
}