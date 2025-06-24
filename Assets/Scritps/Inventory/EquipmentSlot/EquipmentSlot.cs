using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class EquipmentSlot : MonoBehaviour, IPointerClickHandler
{
    #region Events
    public static event Action<EquipmentSlot> OnEquipmentSlotSelected;
    public static event Action<EquipmentSlot, ItemData> OnEquipmentChanged;
    #endregion

    #region UI Components
    [Header("UI References")]
    public Image backgroundImage;
    public Image equippedItemIcon;
    public Button slotButton;
    #endregion

    #region Slot Configuration
    [Header("Slot Configuration")]
    public ItemType slotType;
    public string slotName;
    #endregion

    #region Colors and Visual States
    [Header("Visual Settings")]
    public Color emptySlotColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    public Color filledSlotColor = new Color(0.3f, 0.6f, 0.3f, 1f);
    public Color selectedColor = new Color(1f, 1f, 0f, 0.8f);
    #endregion

    #region Current State
    [Header("Current State")]
    public ItemData equippedItem;
    public bool isEmpty = true;
    public bool isSelected = false;

    // ✅ เพิ่ม: Thread safety flags
    private bool isUpdating = false;
    private readonly object stateLock = new object();
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        InitializeComponents();
        SetupSlot();
        gameObject.SetActive(true);
    }

    void Start()
    {
        ForceSetEmptyState();

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }

    void OnEnable()
    {
        Debug.Log($"🎽 EquipmentSlot {slotName} enabled");
    }

    void OnDisable()
    {
        Debug.Log($"⚠️ EquipmentSlot {slotName} disabled");
    }
    #endregion

    #region Initialization
    void InitializeComponents()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (equippedItemIcon == null)
            equippedItemIcon = transform.Find("ItemIcon")?.GetComponent<Image>();

        if (slotButton == null)
            slotButton = GetComponent<Button>();

        CreateMissingComponents();
    }

    void CreateMissingComponents()
    {
        if (equippedItemIcon == null)
        {
            GameObject itemIconObj = new GameObject("ItemIcon");
            itemIconObj.transform.SetParent(transform);
            itemIconObj.transform.localPosition = Vector3.zero;
            itemIconObj.transform.localScale = Vector3.one;

            equippedItemIcon = itemIconObj.AddComponent<Image>();
            equippedItemIcon.raycastTarget = false;

            RectTransform iconRect = equippedItemIcon.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.one * 8f;
            iconRect.offsetMax = Vector2.one * -8f;
        }

        if (slotButton == null)
        {
            slotButton = gameObject.AddComponent<Button>();
        }
    }

    void SetupSlot()
    {
        if (slotButton != null)
        {
            slotButton.transition = Selectable.Transition.ColorTint;

            ColorBlock colors = slotButton.colors;
            colors.highlightedColor = selectedColor;
            colors.pressedColor = selectedColor;
            slotButton.colors = colors;
        }

        if (equippedItemIcon != null)
        {
            equippedItemIcon.gameObject.SetActive(false);
        }

        if (string.IsNullOrEmpty(slotName))
            slotName = slotType.ToString();

        gameObject.name = $"EquipmentSlot_{slotName}";
    }
    #endregion

    #region Touch Events
    public void OnPointerClick(PointerEventData eventData)
    {
        SelectSlot();
    }

    public void SelectSlot()
    {
        OnEquipmentSlotSelected?.Invoke(this);
        Debug.Log($"🎽 Equipment slot selected: {slotName} ({slotType})");
    }
    #endregion

    #region Visual State Management - ✅ ปรับปรุงแล้ว
    public void SetEmptyState()
    {
        if (isUpdating) return;
        ForceSetEmptyState();
    }

    public void ForceSetEmptyState()
    {
        lock (stateLock)
        {
            ItemData previousItem = equippedItem;

            // ✅ Atomic state change
            isEmpty = true;
            equippedItem = null;

            // Update visuals
            if (backgroundImage != null)
                backgroundImage.color = emptySlotColor;

            if (equippedItemIcon != null)
                equippedItemIcon.gameObject.SetActive(false);

            // Fire event
            OnEquipmentChanged?.Invoke(this, null);

            Debug.Log($"🔧 Equipment slot {slotName} set to empty (was: {previousItem?.ItemName ?? "empty"})");
        }
    }

    public void SetEquippedState(ItemData item)
    {
        if (item == null)
        {
            ForceSetEmptyState();
            return;
        }

        if (!CanAcceptItem(item))
        {
            Debug.LogWarning($"❌ Cannot equip {item.ItemName} in {slotName} slot (wrong type)");
            return;
        }

        lock (stateLock)
        {
            // ✅ Atomic state change
            equippedItem = item;
            isEmpty = false;

            // Update visuals
            if (backgroundImage != null)
                backgroundImage.color = filledSlotColor;

            if (equippedItemIcon != null && item.ItemIcon != null)
            {
                equippedItemIcon.sprite = item.ItemIcon;
                equippedItemIcon.gameObject.SetActive(true);
            }

            // Fire event
            OnEquipmentChanged?.Invoke(this, item);

            Debug.Log($"✅ Equipped {item.ItemName} in {slotName} slot");
            Debug.Log($"   State: isEmpty={isEmpty}, HasEquippedItem={HasEquippedItem()}");
        }
    }

    public void SetSelectedState(bool selected)
    {
        isSelected = selected;

        if (slotButton != null)
        {
            if (selected)
            {
                slotButton.OnSelect(null);
            }
            else
            {
                slotButton.OnDeselect(null);
            }
        }

        Debug.Log($"🌟 Equipment slot {slotName} selected: {selected}");
    }
    #endregion

    #region Equipment Logic - ✅ ปรับปรุงแล้ว
    public bool CanAcceptItem(ItemData item)
    {
        if (item == null) return false;

        bool typeMatch = item.ItemType == slotType;
        Debug.Log($"🔍 CanAcceptItem: {item.ItemName} ({item.ItemType}) → {slotName} ({slotType}) = {typeMatch}");

        return typeMatch;
    }

    public bool TryEquipItem(ItemData item)
    {
        // ✅ ป้องกัน concurrent access
        if (isUpdating)
        {
            Debug.LogWarning($"⚠️ {slotName} is updating, cannot equip {item?.ItemName}");
            return false;
        }

        isUpdating = true;

        try
        {
            // Pre-checks
            if (!CanAcceptItem(item))
            {
                Debug.LogWarning($"❌ Cannot equip {item.ItemName}: wrong item type for {slotName}");
                return false;
            }

            // ✅ Comprehensive emptiness check
            lock (stateLock)
            {
                if (!isEmpty || HasEquippedItem() || equippedItem != null)
                {
                    string occupiedBy = equippedItem?.ItemName ?? "Unknown";
                    Debug.LogWarning($"❌ Cannot equip {item.ItemName}: {slotName} already occupied by {occupiedBy}");
                    Debug.LogWarning($"   State: isEmpty={isEmpty}, HasEquippedItem={HasEquippedItem()}, equippedItem={equippedItem?.ItemName ?? "NULL"}");
                    return false;
                }
            }

            Debug.Log($"🔄 Attempting to equip {item.ItemName} in {slotName}...");

            // Perform equip
            SetEquippedState(item);

            // ✅ Verify success
            bool success = HasEquippedItem() && ReferenceEquals(equippedItem, item);

            if (!success)
            {
                Debug.LogError($"❌ Equip verification failed for {item.ItemName} in {slotName}");
                ForceSetEmptyState(); // Rollback
                return false;
            }

            Debug.Log($"✅ Successfully equipped {item.ItemName} in {slotName}");
            return true;
        }
        finally
        {
            isUpdating = false;
        }
    }

    public ItemData UnequipItem()
    {
        if (isUpdating)
        {
            Debug.LogWarning($"⚠️ Cannot unequip from {slotName} - slot is updating");
            return null;
        }

        ItemData unequippedItem = equippedItem;

        if (unequippedItem != null)
        {
            ForceSetEmptyState();
            Debug.Log($"🔧 Unequipped {unequippedItem.ItemName} from {slotName}");
        }

        return unequippedItem;
    }

    public ItemData GetEquippedItem()
    {
        lock (stateLock)
        {
            return equippedItem;
        }
    }

    public bool HasEquippedItem()
    {
        lock (stateLock)
        {
            return equippedItem != null && !isEmpty;
        }
    }

    // ✅ เพิ่ม: State validation และ recovery
    public void ValidateAndFixState()
    {
        lock (stateLock)
        {
            bool hasItem = equippedItem != null;
            bool isEmptyFlag = isEmpty;

            // Check for inconsistencies และแก้ไข
            if (hasItem && isEmptyFlag)
            {
                Debug.LogWarning($"🔧 Fixing {slotName}: has item but marked as empty");
                isEmpty = false;
            }
            else if (!hasItem && !isEmptyFlag)
            {
                Debug.LogWarning($"🔧 Fixing {slotName}: no item but marked as filled");
                isEmpty = true;

                // Update visual
                if (backgroundImage != null)
                    backgroundImage.color = emptySlotColor;
                if (equippedItemIcon != null)
                    equippedItemIcon.gameObject.SetActive(false);
            }

            Debug.Log($"✅ {slotName} state validated: isEmpty={isEmpty}, hasItem={hasItem}");
        }
    }

    public void ForceActivation()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            Debug.Log($"🔄 Force activated {slotName} slot");
        }
    }

    public void DebugSlotStatus()
    {
        lock (stateLock)
        {
            Debug.Log($"🔍 {slotName} Status:");
            Debug.Log($"   GameObject Active: {gameObject.activeSelf}");
            Debug.Log($"   Is Empty: {isEmpty}");
            Debug.Log($"   Has Item: {HasEquippedItem()}");
            Debug.Log($"   Item: {equippedItem?.ItemName ?? "None"}");
            Debug.Log($"   Is Updating: {isUpdating}");
        }
    }
    #endregion

    #region Debug Methods
    [ContextMenu("Test - Force Activation")]
    public void TestForceActivation()
    {
        ForceActivation();
    }

    [ContextMenu("Test - Debug Status")]
    public void TestDebugStatus()
    {
        DebugSlotStatus();
    }

    [ContextMenu("Test - Validate State")]
    public void TestValidateState()
    {
        ValidateAndFixState();
    }

    [ContextMenu("Test - Set Empty")]
    public void TestSetEmpty()
    {
        ForceSetEmptyState();
    }

    [ContextMenu("Test - Equip Random Item")]
    public void TestEquipRandomItem()
    {
        if (ItemDatabase.Instance != null)
        {
            var itemsOfType = ItemDatabase.Instance.GetItemsByType(slotType);
            if (itemsOfType.Count > 0)
            {
                ItemData randomItem = itemsOfType[UnityEngine.Random.Range(0, itemsOfType.Count)];
                TryEquipItem(randomItem);
            }
            else
            {
                Debug.LogWarning($"❌ No items of type {slotType} found in database");
            }
        }
    }
    #endregion
}