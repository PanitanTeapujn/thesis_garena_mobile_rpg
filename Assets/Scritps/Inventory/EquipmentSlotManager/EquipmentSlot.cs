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

        // สร้าง SlotTypeText ถ้าต้องการและไม่มี
        if (slotTypeText == null && ShouldShowSlotTypeText())
        {
            CreateSlotTypeText();
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

        if (slotBackground != null)
            slotBackground.color = emptySlotColor;

        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.color = Color.white;
            itemIcon.gameObject.SetActive(false);
        }

        Debug.Log($"[EquipmentSlot] {slotType} slot set to empty");
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
        }

        Debug.Log($"[EquipmentSlot] {slotType} slot filled with item: {itemSprite.name}");
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

        // แจ้ง Manager
        OnSlotClicked?.Invoke(this);
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
    [ContextMenu("🔄 Force Refresh This Slot")]
    private void TestForceRefresh()
    {
        ForceRefresh();
    }

    [ContextMenu("🔍 Debug This Slot")]
    private void DebugThisSlot()
    {
        Debug.Log($"=== SLOT DEBUG ===");
        Debug.Log($"Slot Type: {slotType}");
        Debug.Log($"Potion Index: {potionSlotIndex}");
        Debug.Log($"Is Empty: {isEmpty}");
        Debug.Log($"Has Manager: {manager != null}");
        Debug.Log($"Item Icon Active: {itemIcon?.gameObject.activeSelf ?? false}");
    }
    #endregion
}