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

    #region UI Components (Simplified - เหลือแค่ 2 อย่าง)
    [Header("UI References")]
    public Image backgroundImage;      // พื้นหลัง slot
    public Image equippedItemIcon;     // รูป item ที่สวมใส่
    public Button slotButton;          // button สำหรับ touch
    #endregion

    #region Slot Configuration
    [Header("Slot Configuration")]
    public ItemType slotType;              // ประเภท slot (Weapon, Head, etc.)
    public string slotName;                // ชื่อ slot สำหรับ display
    #endregion

    #region Colors and Visual States (Simplified)
    [Header("Visual Settings")]
    public Color emptySlotColor = new Color(0.4f, 0.4f, 0.4f, 1f);        // สีเมื่อว่าง
    public Color filledSlotColor = new Color(0.3f, 0.6f, 0.3f, 1f);       // สีเมื่อมี item
    public Color selectedColor = new Color(1f, 1f, 0f, 0.8f);             // สีเมื่อ selected
    #endregion

    #region Current State
    [Header("Current State")]
    public ItemData equippedItem;          // Item ที่สวมใส่อยู่
    public bool isEmpty = true;            // ว่างหรือไม่
    public bool isSelected = false;        // ถูกเลือกหรือไม่
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        InitializeComponents();
        SetupSlot();

        // ✅ เพิ่ม: Force active ตัวเอง
        gameObject.SetActive(true);
    }

    void Start()
    {
        SetEmptyState();

        // ✅ เพิ่ม: Double check activation
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }

    void OnEnable()
    {
        // ✅ เพิ่ม: Log เมื่อ active
        Debug.Log($"🎽 EquipmentSlot {slotName} enabled");
    }

    void OnDisable()
    {
        // ✅ เพิ่ม: Log เมื่อ inactive
        Debug.Log($"⚠️ EquipmentSlot {slotName} disabled");
    }
    #endregion

    #region Initialization (Simplified)
    void InitializeComponents()
    {
        // Auto-find components if not assigned
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (equippedItemIcon == null)
            equippedItemIcon = transform.Find("ItemIcon")?.GetComponent<Image>();

        if (slotButton == null)
            slotButton = GetComponent<Button>();

        // Create missing components (แค่ ItemIcon เท่านั้น)
        CreateMissingComponents();
    }

    void CreateMissingComponents()
    {
        // สร้าง ItemIcon child object ถ้าไม่มี
        if (equippedItemIcon == null)
        {
            GameObject itemIconObj = new GameObject("ItemIcon");
            itemIconObj.transform.SetParent(transform);
            itemIconObj.transform.localPosition = Vector3.zero;
            itemIconObj.transform.localScale = Vector3.one;

            equippedItemIcon = itemIconObj.AddComponent<Image>();
            equippedItemIcon.raycastTarget = false; // ไม่ให้รบกวน touch events

            // ตั้งขนาดให้พอดีใน slot
            RectTransform iconRect = equippedItemIcon.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.one * 8f;  // padding 8px
            iconRect.offsetMax = Vector2.one * -8f;
        }

        // สร้าง Button component ถ้าไม่มี
        if (slotButton == null)
        {
            slotButton = gameObject.AddComponent<Button>();
        }
    }

    void SetupSlot()
    {
        // ตั้งค่า button
        if (slotButton != null)
        {
            slotButton.transition = Selectable.Transition.ColorTint;

            // ตั้งค่าสี highlight สำหรับ button
            ColorBlock colors = slotButton.colors;
            colors.highlightedColor = selectedColor;
            colors.pressedColor = selectedColor;
            slotButton.colors = colors;
        }

        // ซ่อน item icon ตอนเริ่มต้น
        if (equippedItemIcon != null)
        {
            equippedItemIcon.gameObject.SetActive(false);
        }

        // ตั้งชื่อ GameObject ตาม slot type
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

    #region Visual State Management (Simplified)
    public void SetEmptyState()
    {
        isEmpty = true;
        equippedItem = null;

        // ตั้งสีพื้นหลัง
        if (backgroundImage != null)
            backgroundImage.color = emptySlotColor;

        // ซ่อน item icon
        if (equippedItemIcon != null)
            equippedItemIcon.gameObject.SetActive(false);

        // แจ้งให้ระบบอื่นรู้
        OnEquipmentChanged?.Invoke(this, null);

        Debug.Log($"🔧 Equipment slot {slotName} set to empty");
    }

    public void SetEquippedState(ItemData item)
    {
        if (item == null)
        {
            SetEmptyState();
            return;
        }

        // ตรวจสอบว่า item type ตรงกับ slot type หรือไม่
        if (!CanAcceptItem(item))
        {
            Debug.LogWarning($"❌ Cannot equip {item.ItemName} in {slotName} slot (wrong type)");
            return;
        }

        // ✅ Set state atomically เพื่อป้องกัน race condition
        isEmpty = false;
        equippedItem = item;

        // ตั้งสีพื้นหลัง
        if (backgroundImage != null)
            backgroundImage.color = filledSlotColor;

        // แสดง item icon
        if (equippedItemIcon != null && item.ItemIcon != null)
        {
            equippedItemIcon.sprite = item.ItemIcon;
            equippedItemIcon.gameObject.SetActive(true);
        }

        // ✅ เพิ่ม: Debug เพื่อยืนยันว่า item ถูกเก็บ
        Debug.Log($"✅ Equipped {item.ItemName} (ID: {item.ItemId}) in {slotName} slot");
        Debug.Log($"   Final state: isEmpty={isEmpty}, HasEquippedItem={HasEquippedItem()}");

        // แจ้งให้ระบบอื่นรู้
        OnEquipmentChanged?.Invoke(this, item);
    }

    public void SetSelectedState(bool selected)
    {
        isSelected = selected;

        // ใช้ Button highlight แทน highlight image
        if (slotButton != null)
        {
            if (selected)
            {
                // Force highlight state
                slotButton.OnSelect(null);
            }
            else
            {
                // Clear highlight state
                slotButton.OnDeselect(null);
            }
        }

        Debug.Log($"🌟 Equipment slot {slotName} selected: {selected}");
    }
    #endregion

    #region Equipment Logic
    public bool CanAcceptItem(ItemData item)
    {
        if (item == null) return false;

        // ตรวจสอบว่า item type ตรงกับ slot type
        bool typeMatch = item.ItemType == slotType;

        Debug.Log($"🔍 CanAcceptItem: {item.ItemName} ({item.ItemType}) → {slotName} ({slotType}) = {typeMatch}");

        return typeMatch;
    }

    public bool TryEquipItem(ItemData item)
    {
        // ตรวจสอบว่าสามารถใส่ item ได้หรือไม่
        if (!CanAcceptItem(item))
        {
            Debug.LogWarning($"❌ Cannot equip {item.ItemName}: wrong item type for {slotName}");
            return false;
        }

        // ✅ ตรวจสอบว่า slot ว่างหรือไม่อย่างเข้มงวด
        if (!isEmpty || HasEquippedItem() || equippedItem != null)
        {
            string occupiedBy = equippedItem?.ItemName ?? "Unknown";
            Debug.LogWarning($"❌ Cannot equip {item.ItemName}: {slotName} slot is already occupied by {occupiedBy}");
            Debug.LogWarning($"   Slot state: isEmpty={isEmpty}, HasEquippedItem={HasEquippedItem()}, equippedItem={equippedItem?.ItemName ?? "NULL"}");
            return false;
        }

        // ✅ เพิ่ม debug เพื่อ track การ equip
        Debug.Log($"🔄 Attempting to equip {item.ItemName} in {slotName}...");

        // Equip item
        SetEquippedState(item);

        // ✅ Verify ว่า equip สำเร็จ
        if (HasEquippedItem() && equippedItem == item)
        {
            Debug.Log($"✅ Successfully equipped {item.ItemName} in {slotName}");
            return true;
        }
        else
        {
            Debug.LogError($"❌ Equip verification failed for {item.ItemName} in {slotName}");
            // ✅ Rollback ถ้า equip ไม่สำเร็จ
            SetEmptyState();
            return false;
        }
    }

    public ItemData UnequipItem()
    {
        ItemData unequippedItem = equippedItem;

        if (unequippedItem != null)
        {
            SetEmptyState();
            Debug.Log($"🔧 Unequipped {unequippedItem.ItemName} from {slotName}");
        }

        return unequippedItem;
    }

    public ItemData GetEquippedItem()
    {
        return equippedItem;
    }

    public bool HasEquippedItem()
    {
        // ✅ ตรวจสอบให้แน่ใจว่า consistent
        bool hasItem = equippedItem != null && !isEmpty;

        // ✅ Debug inconsistency
        if ((equippedItem != null) != (!isEmpty))
        {
            Debug.LogWarning($"⚠️ Inconsistent state in {slotName}: equippedItem={(equippedItem != null)}, isEmpty={isEmpty}");

            // ✅ Auto-fix inconsistency
            if (equippedItem != null)
            {
                isEmpty = false;
            }
            else
            {
                isEmpty = true;
            }
        }

        return hasItem;
    }

    // ✅ เพิ่ม: Method สำหรับ force persistent activation
    public void ForceActivation()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            Debug.Log($"🔄 Force activated {slotName} slot");
        }
    }

    // ✅ เพิ่ม: Method สำหรับตรวจสอบสถานะ
    public void DebugSlotStatus()
    {
        Debug.Log($"🔍 {slotName} Status:");
        Debug.Log($"   GameObject Active: {gameObject.activeSelf}");
        Debug.Log($"   Is Empty: {isEmpty}");
        Debug.Log($"   Has Item: {HasEquippedItem()}");
        Debug.Log($"   Item: {equippedItem?.ItemName ?? "None"}");
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

    [ContextMenu("Test - Set Empty")]
    public void TestSetEmpty()
    {
        SetEmptyState();
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