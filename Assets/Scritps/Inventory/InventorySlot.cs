using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlot : MonoBehaviour
{
    [Header("UI Components")]
    public Image slotBackground;     // พื้นหลัง slot
    public Image itemIcon;          // ไอคอน item (ซ่อนถ้าไม่มี item)
    public Button slotButton;       // ปุ่มสำหรับ touch events
    public TextMeshProUGUI stackText; // จำนวน item (optional สำหรับอนาคต)

    [Header("Visual Settings")]
    public Color emptySlotColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);      // สีเทาเมื่อไม่มี item
    public Color filledSlotColor = new Color(0.3f, 0.3f, 0.3f, 1f);       // สีเมื่อมี item
    public Color selectedSlotColor = new Color(0.8f, 0.8f, 0.2f, 1f);     // สีเหลืองเมื่อถูกเลือก

    [Header("Slot Info")]
    [SerializeField] private int slotIndex = -1;
    [SerializeField] private bool isEmpty = true;
    [SerializeField] private bool isSelected = false;

    // Events
    public System.Action<int> OnSlotSelected;

    // Properties
    public int SlotIndex
    {
        get { return slotIndex; }
        set { slotIndex = value; }
    }

    public bool IsEmpty
    {
        get { return isEmpty; }
    }

    public bool IsSelected
    {
        get { return isSelected; }
    }

    #region Unity Lifecycle
    private void Awake()
    {
        SetupComponents();
        SetupButton();
    }

    private void Start()
    {
        SetEmptyState();
    }
    #endregion

    #region Component Setup
    private void SetupComponents()
    {
        // หา components อัตโนมัติถ้าไม่ได้ assign
        if (slotBackground == null)
            slotBackground = GetComponent<Image>();

        if (itemIcon == null)
            itemIcon = transform.Find("ItemIcon")?.GetComponent<Image>();

        if (slotButton == null)
            slotButton = GetComponent<Button>();

        if (stackText == null)
            stackText = GetComponentInChildren<TextMeshProUGUI>();

        // สร้าง ItemIcon ถ้าไม่มี
        if (itemIcon == null)
        {
            GameObject iconObj = new GameObject("ItemIcon");
            iconObj.transform.SetParent(transform, false);
            itemIcon = iconObj.AddComponent<Image>();

            // ตั้งค่า RectTransform ให้เต็ม slot แต่เล็กลงนิดหน่อย
            RectTransform iconRect = itemIcon.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.one * 5f;  // margin 5 pixel
            iconRect.offsetMax = Vector2.one * -5f;

            itemIcon.raycastTarget = false; // ไม่ให้ block การกดปุ่ม
            itemIcon.preserveAspect = true; // รักษา aspect ratio ของ sprite
        }

        // สร้าง Stack Text ถ้าไม่มี
        if (stackText == null)
        {
            GameObject textObj = new GameObject("StackText");
            textObj.transform.SetParent(transform, false);
            stackText = textObj.AddComponent<TextMeshProUGUI>();

            // ตั้งค่า RectTransform ให้อยู่มุมล่างขวา
            RectTransform textRect = stackText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.6f, 0f);
            textRect.anchorMax = new Vector2(1f, 0.4f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // ตั้งค่า text properties
            stackText.text = "";
            stackText.fontSize = 12f;
            stackText.color = Color.white;
            stackText.fontStyle = FontStyles.Bold;
            stackText.alignment = TextAlignmentOptions.BottomRight;
            stackText.raycastTarget = false;

            // เพิ่ม outline สำหรับอ่านง่าย
            var outline = textObj.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1, -1);
        }

        // ซ่อน stack text ไว้ก่อน
        if (stackText != null)
            stackText.gameObject.SetActive(false);
    }

    private void SetupButton()
    {
        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(OnSlotClicked);
        }
    }
    #endregion

    #region Slot State Management
    public void SetEmptyState()
    {
        // ✅ ตั้งค่า isEmpty ก่อนเป็นอันดับแรก
        isEmpty = true;
        isSelected = false;

        if (slotBackground != null)
            slotBackground.color = emptySlotColor;

        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.color = Color.white; // รีเซ็ต color
            itemIcon.gameObject.SetActive(false);
        }

        if (stackText != null)
            stackText.gameObject.SetActive(false);

        Debug.Log($"[InventorySlot] Slot {slotIndex} set to empty state, isEmpty now: {isEmpty}");
    }
    public void SetFilledState(Sprite itemSprite, int stackCount = 0)
    {
        if (itemSprite == null)
        {
            Debug.LogWarning($"[InventorySlot] Trying to fill slot {slotIndex} with null sprite!");
            SetEmptyState();
            return;
        }

        Debug.Log($"[InventorySlot] Setting sprite: {itemSprite.name} for slot {slotIndex}");

        // ✅ ตั้งค่า isEmpty ก่อนเป็นอันดับแรก
        isEmpty = false;
        isSelected = false;

        if (slotBackground != null)
            slotBackground.color = filledSlotColor;
        else
            Debug.LogError($"[InventorySlot] Slot {slotIndex}: slotBackground is null!");

        if (itemIcon != null)
        {
            itemIcon.sprite = itemSprite;
            itemIcon.gameObject.SetActive(true);

            Debug.Log($"[InventorySlot] Slot {slotIndex}: ItemIcon active={itemIcon.gameObject.activeSelf}, Sprite={itemIcon.sprite?.name}");

            // ✅ Force refresh canvas
            Canvas.ForceUpdateCanvases();
        }
        else
        {
            Debug.LogError($"[InventorySlot] Slot {slotIndex}: itemIcon is null!");
        }

        // แสดง Stack text เฉพาะเมื่อ stackCount > 1
        if (stackText != null)
        {
            if (stackCount > 1)
            {
                stackText.text = stackCount.ToString();
                stackText.gameObject.SetActive(true);
            }
            else
            {
                stackText.gameObject.SetActive(false);
            }
        }

        string stackInfo = stackCount > 1 ? $" x{stackCount}" : "";
        Debug.Log($"[InventorySlot] Slot {slotIndex} filled with item: {itemSprite.name}{stackInfo}, isEmpty now: {isEmpty}");
    }
    public void SetRarityColor(Color rarityColor)
    {
        if (itemIcon != null)
        {
            itemIcon.color = rarityColor;
        }
    }


    public void SetSelectedState(bool selected)
    {
        isSelected = selected;

        // ✅ ดึงข้อมูล item ที่อยู่ใน slot นี้จาก character inventory
        string itemInfo = GetCurrentItemInfo();

        if (slotBackground != null)
        {
            if (isSelected)
            {
                slotBackground.color = selectedSlotColor;
                Debug.Log($"[InventorySlot] Slot {slotIndex} selected - Item: {itemInfo}");
            }
            else
            {
                // กลับไปสีปกติตามสถานะ empty/filled
                slotBackground.color = isEmpty ? emptySlotColor : filledSlotColor;
                Debug.Log($"[InventorySlot] Slot {slotIndex} deselected - Item: {itemInfo}");
            }
        }
    }

    // ✅ เพิ่ม method นี้เพื่อดึงข้อมูล item
    private string GetCurrentItemInfo()
    {
        // หา Character จาก InventoryGridManager
        InventoryGridManager gridManager = GetComponentInParent<InventoryGridManager>();
        if (gridManager?.OwnerCharacter == null)
        {
            return "No Character";
        }

        Inventory inventory = gridManager.OwnerCharacter.GetInventory();
        if (inventory == null)
        {
            return "No Inventory";
        }

        InventoryItem item = inventory.GetItem(slotIndex);
        if (item == null || item.IsEmpty)
        {
            return "Empty";
        }

        string stackInfo = item.stackCount > 1 ? $" x{item.stackCount}" : "";
        return $"{item.itemData.ItemName}{stackInfo} ({item.itemData.ItemType})";
    }
    #endregion
    public void ForceSetupComponents()
    {
        Debug.Log($"[InventorySlot] Force setup components for slot {slotIndex}");

        SetupComponents();
        SetupButton();

        // ตรวจสอบว่า components พร้อมหรือยัง
        if (slotBackground == null)
            Debug.LogError($"[InventorySlot] Slot {slotIndex}: slotBackground is null after setup!");

        if (itemIcon == null)
            Debug.LogError($"[InventorySlot] Slot {slotIndex}: itemIcon is null after setup!");
        else
            Debug.Log($"[InventorySlot] Slot {slotIndex}: itemIcon setup complete - {itemIcon.gameObject.name}");

        if (slotButton == null)
            Debug.LogError($"[InventorySlot] Slot {slotIndex}: slotButton is null after setup!");
    }
    #region Button Events
    private void OnSlotClicked()
    {
        // ✅ ตรวจสอบสถานะจริงจาก character inventory ก่อน
        SyncWithCharacterInventory();

        Debug.Log($"[InventorySlot] Slot {slotIndex} clicked - Empty: {isEmpty}, Selected: {isSelected}");

        // แจ้ง InventoryGridManager ว่า slot นี้ถูกกด
        OnSlotSelected?.Invoke(slotIndex);
    }

    private void SyncWithCharacterInventory()
    {
        // หา Character จาก InventoryGridManager
        InventoryGridManager gridManager = GetComponentInParent<InventoryGridManager>();
        if (gridManager?.OwnerCharacter == null) return;

        Inventory inventory = gridManager.OwnerCharacter.GetInventory();
        if (inventory == null) return;

        InventoryItem item = inventory.GetItem(slotIndex);

        // อัปเดตสถานะตาม inventory จริง
        if (item == null || item.IsEmpty)
        {
            if (!isEmpty) // ถ้าเดิมไม่ empty แต่ตอนนี้ empty แล้ว
            {
                SetEmptyState();
                Debug.Log($"[InventorySlot] Synced slot {slotIndex} to empty state");
            }
        }
        else
        {
            if (isEmpty) // ถ้าเดิม empty แต่ตอนนี้มี item แล้ว
            {
                Sprite itemIcon = item.itemData.ItemIcon;
                int stackCount = item.itemData.CanStack() && item.stackCount > 1 ? item.stackCount : 0;

                SetFilledState(itemIcon, stackCount);

                // เพิ่ม tier color
                Color tierColor = item.itemData.GetTierColor();
                SetRarityColor(tierColor);

                Debug.Log($"[InventorySlot] Synced slot {slotIndex} to filled state: {item.itemData.ItemName}");
            }
        }
    }
    #endregion

    #region Public Methods for Testing

    #endregion
}