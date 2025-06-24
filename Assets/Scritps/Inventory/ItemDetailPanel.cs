using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ItemDetailPanel : MonoBehaviour
{
    #region UI References
    [Header("Panel References")]
    public GameObject detailPanel;           // Panel หลัก
    public Button closeButton;               // ปุ่มปิด (ถ้ามี)
    public Button equipButton;               // ปุ่ม Equip (สีเขียว)
    public Button unequipButton;             // ปุ่ม Unequip (สีแดง/ส้ม)

    [Header("Item Display")]
    public Image itemIconImage;              // รูป item ใหญ่
    public Image itemTierBorder;             // border สี tier รอบรูป

    [Header("Item Info Text")]
    public TextMeshProUGUI nameText;         // "Name" + ชื่อ item
    public TextMeshProUGUI tierText;         // "Tier" + tier level  
    public TextMeshProUGUI typeText;         // "Type" + ประเภท item
    public TextMeshProUGUI statText;         // "Stat" + รายการ stats
    public TextMeshProUGUI descriptionsText; // "Descriptions" + คำอธิบาย
    #endregion

    #region Current State
    [Header("Current State")]
    public ItemData currentItem;
    public int currentSlotIndex = -1;
    public bool isVisible = false;
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
        // ไม่ต้องสร้าง prefabs เพราะใช้ text เดียว
        Debug.Log("✅ Simple ItemDetailPanel initialized");
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

    void SubscribeToEvents()
    {
        // Unsubscribe ก่อนเผื่อมี duplicate
        InventorySlot.OnSlotSelected -= OnInventorySlotSelected;

        // Subscribe ใหม่
        InventorySlot.OnSlotSelected += OnInventorySlotSelected;

        Debug.Log("✅ ItemDetailPanel subscribed to slot events");
    }

    void UnsubscribeFromEvents()
    {
        InventorySlot.OnSlotSelected -= OnInventorySlotSelected;
        Debug.Log("📝 ItemDetailPanel unsubscribed from slot events");
    }
    #endregion

    #region Event Handlers
    void OnInventorySlotSelected(InventorySlot slot)
    {
        Debug.Log($"🎯 ItemDetailPanel received slot selection: {slot?.slotIndex}");

        if (slot != null && slot.HasItem())
        {
            ItemData item = slot.GetItem();
            Debug.Log($"📦 Slot has item: {item?.ItemName}");
            ShowItemDetail(item, slot.slotIndex);
        }
        else
        {
            Debug.Log("📭 Slot is empty, hiding panel");
            HidePanel();
        }
    }

    void OnEquipButtonClicked()
    {
        if (currentItem != null)
        {
            OnEquipRequested?.Invoke(currentItem, currentSlotIndex);
            Debug.Log($"🎽 Equip requested: {currentItem.ItemName}");
        }
    }

    void OnUnequipButtonClicked()
    {
        if (currentItem != null)
        {
            OnUnequipRequested?.Invoke(currentItem, currentSlotIndex);
            Debug.Log($"🎽 Unequip requested: {currentItem.ItemName}");
        }
    }
    #endregion

    #region Panel Control
    public void ShowItemDetail(ItemData item, int slotIndex)
    {
        Debug.Log($"📋 ShowItemDetail called: item={item?.ItemName}, slot={slotIndex}");

        if (item == null)
        {
            Debug.LogWarning("❌ Item is null, hiding panel");
            HidePanel();
            return;
        }

        currentItem = item;
        currentSlotIndex = slotIndex;
        isVisible = true;

        UpdateItemDisplay();
        UpdateItemInfo();
        UpdateButtons();

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

    public void HidePanel()
    {
        currentItem = null;
        currentSlotIndex = -1;
        isVisible = false;

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
            // เปลี่ยนสีตาม tier (optional)
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

    string GetStatsDisplayText(ItemStats stats)
    {
        if (!stats.HasAnyStats())
            return "No bonus stats";

        var statsList = new System.Collections.Generic.List<string>();

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

    void UpdateButtons()
    {
        if (currentItem == null) return;

        // ตรวจสอบว่า item นี้ equipped อยู่ไหม
        bool isEquipped = IsItemCurrentlyEquipped(currentItem);

        if (equipButton != null)
        {
            equipButton.gameObject.SetActive(!isEquipped);
        }

        if (unequipButton != null)
        {
            unequipButton.gameObject.SetActive(isEquipped);
        }
    }

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

    // TODO: Step 6 จะ implement methods เหล่านี้
    bool IsItemCurrentlyEquipped(ItemData item)
    {
        // ตรงนี้จะเชื่อมต่อกับ EquipmentManager ใน Step 6
        return false;
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

    [ContextMenu("Debug Panel State")]
    public void DebugPanelState()
    {
        Debug.Log($"📋 ItemDetailPanel Debug:");
        Debug.Log($"   detailPanel: {(detailPanel != null ? "✅ Assigned" : "❌ NULL")}");
        Debug.Log($"   isVisible: {isVisible}");
        Debug.Log($"   currentItem: {currentItem?.ItemName ?? "NULL"}");
        Debug.Log($"   currentSlotIndex: {currentSlotIndex}");
        Debug.Log($"   Panel Active: {(detailPanel != null ? detailPanel.activeSelf.ToString() : "N/A")}");
    }
}