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
    public Image tierBackground;    // พื้นหลังแสดงสี tier (อยู่ข้างหลัง itemIcon)

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
        // 🔧 เช็คว่าลาก components ครบหรือยัง
        if (slotBackground == null)
        {
            slotBackground = GetComponent<Image>();
            if (slotBackground == null)
                Debug.LogError($"[InventorySlot] Slot {slotIndex}: Please assign SlotBackground in Inspector!");
        }

        if (slotButton == null)
        {
            slotButton = GetComponent<Button>();
            if (slotButton == null)
                Debug.LogError($"[InventorySlot] Slot {slotIndex}: Please assign SlotButton in Inspector!");
        }

        // 🚨 ไม่สร้างอัตโนมัติ - ให้ลากใส่ใน Inspector เท่านั้น
        if (tierBackground == null)
            Debug.LogWarning($"[InventorySlot] Slot {slotIndex}: TierBackground not assigned! Please drag it from Inspector.");

        if (itemIcon == null)
            Debug.LogWarning($"[InventorySlot] Slot {slotIndex}: ItemIcon not assigned! Please drag it from Inspector.");

        if (stackText == null)
            Debug.LogWarning($"[InventorySlot] Slot {slotIndex}: StackText not assigned! Please drag it from Inspector.");
    }

    private void SetupButton()
    {
        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(OnSlotClicked);
        }
        else
        {
            Debug.LogError($"[InventorySlot] Slot {slotIndex}: No SlotButton assigned!");
        }
    }
    #endregion

    #region Slot State Management
    public void SetEmptyState()
    {
        Debug.Log($"[InventorySlot] 🧹 Setting empty state for slot {slotIndex}");

        isEmpty = true;
        isSelected = false;

        // เซ็ต slot background
        if (slotBackground != null)
        {
            slotBackground.color = emptySlotColor;
        }

        // ซ่อน item icon
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.color = Color.white;
            itemIcon.gameObject.SetActive(false);
            Debug.Log($"[InventorySlot] 🖼️ Slot {slotIndex}: ItemIcon hidden");
        }

        // ปิด tier background
        if (tierBackground != null)
        {
            tierBackground.enabled = false;
            Debug.Log($"[InventorySlot] 🎨 Slot {slotIndex}: Tier background disabled");
        }

        // ซ่อน stack text
        if (stackText != null)
        {
            stackText.text = "";
            stackText.gameObject.SetActive(false);
            Debug.Log($"[InventorySlot] 📊 Slot {slotIndex}: Stack text hidden");
        }

        Debug.Log($"[InventorySlot] ✅ Slot {slotIndex} empty state complete");
    }
    public void SetFilledState(Sprite itemSprite, int stackCount = 0)
    {
        if (itemSprite == null)
        {
            Debug.LogWarning($"[InventorySlot] Trying to fill slot {slotIndex} with null sprite!");
            SetEmptyState();
            return;
        }

        Debug.Log($"[InventorySlot] 🎨 Setting filled state for slot {slotIndex}: {itemSprite.name}");

        isEmpty = false;
        isSelected = false;

        // เซ็ต slot background
        if (slotBackground != null)
        {
            slotBackground.color = filledSlotColor;
        }

        // แสดง item icon
        if (itemIcon != null)
        {
            itemIcon.sprite = itemSprite;
            itemIcon.color = Color.white; // สีขาวเสมอ
            itemIcon.gameObject.SetActive(true);
            Debug.Log($"[InventorySlot] 🖼️ Slot {slotIndex}: ItemIcon activated");
        }
        else
        {
            Debug.LogError($"[InventorySlot] Slot {slotIndex}: ItemIcon not assigned in Inspector!");
        }

        // จัดการ stack text
        if (stackText != null)
        {
            if (stackCount > 1)
            {
                stackText.text = stackCount.ToString();
                stackText.gameObject.SetActive(true);
                Debug.Log($"[InventorySlot] 📊 Slot {slotIndex}: Stack text = {stackCount}");
            }
            else
            {
                stackText.text = "";
                stackText.gameObject.SetActive(false);
            }
        }

        Debug.Log($"[InventorySlot] ✅ Slot {slotIndex} filled state complete");
    }

    public void SetTierBorder(Color tierColor)
    {
        if (itemIcon != null)
        {
            var outline = itemIcon.GetComponent<UnityEngine.UI.Outline>();
            if (outline != null)
            {
                outline.effectColor = tierColor;
                outline.enabled = true; // เปิดใช้งาน outline
                Debug.Log($"[InventorySlot] 🌈 Slot {slotIndex}: Set tier border color to {tierColor}");
            }
            else
            {
                Debug.LogWarning($"[InventorySlot] Slot {slotIndex}: No Outline component found on itemIcon");
            }
        }
    }
    public void DisableTierBorder()
    {
        if (itemIcon != null)
        {
            var outline = itemIcon.GetComponent<UnityEngine.UI.Outline>();
            if (outline != null)
            {
                outline.enabled = false;
                Debug.Log($"[InventorySlot] 🚫 Slot {slotIndex}: Disabled tier border");
            }
        }
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
        bool allGood = true;

        if (slotBackground == null)
        {
            Debug.LogError($"[InventorySlot] Slot {slotIndex}: slotBackground missing!");
            allGood = false;
        }

        if (tierBackground == null)
        {
            Debug.LogWarning($"[InventorySlot] Slot {slotIndex}: tierBackground missing!");
            allGood = false;
        }

        if (itemIcon == null)
        {
            Debug.LogWarning($"[InventorySlot] Slot {slotIndex}: itemIcon missing!");
            allGood = false;
        }

        if (slotButton == null)
        {
            Debug.LogError($"[InventorySlot] Slot {slotIndex}: slotButton missing!");
            allGood = false;
        }

        if (allGood)
        {
            Debug.Log($"[InventorySlot] ✅ Slot {slotIndex}: All components ready");
        }
        else
        {
            Debug.LogWarning($"[InventorySlot] ⚠️ Slot {slotIndex}: Some components missing - please assign in Inspector");
        }
    }
    #region Button Events
    private void OnSlotClicked()
    {
        // ✅ ตรวจสอบสถานะจริงจาก character inventory ก่อน
        SyncWithCharacterInventory();

        Debug.Log($"[InventorySlot] Slot {slotIndex} clicked - Empty: {isEmpty}, Selected: {isSelected}");

        // แจ้ง InventoryGridManager ว่า slot นี้ถูกกด
        OnSlotSelected?.Invoke(slotIndex);

        // 🆕 ถ้า slot นี้มีไอเทม ให้แสดงรายละเอียด
        if (!isEmpty)
        {
            ShowItemDetailForThisSlot();
        }
    }
    private void ShowItemDetailForThisSlot()
    {
        // หา Character จาก InventoryGridManager
        InventoryGridManager gridManager = GetComponentInParent<InventoryGridManager>();
        if (gridManager?.OwnerCharacter == null) return;

        Inventory inventory = gridManager.OwnerCharacter.GetInventory();
        if (inventory == null) return;

        InventoryItem item = inventory.GetItem(slotIndex);
        if (item == null || item.IsEmpty) return;

        // หา CombatUIManager เพื่อแสดง item detail
        CombatUIManager uiManager = FindObjectOfType<CombatUIManager>();
        if (uiManager?.itemDetailManager != null)
        {
            uiManager.itemDetailManager.ShowItemDetail(item);
            Debug.Log($"[InventorySlot] Requested item detail for: {item.itemData.ItemName}");
        }
    }

    private void SyncWithCharacterInventory()
    {
        // หา Character จาก InventoryGridManager
        InventoryGridManager gridManager = GetComponentInParent<InventoryGridManager>();
        if (gridManager?.OwnerCharacter == null) return;

        Inventory inventory = gridManager.OwnerCharacter.GetInventory();
        if (inventory == null) return;

        InventoryItem item = inventory.GetItem(slotIndex);

        // 🆕 Debug ข้อมูลก่อน sync
        string currentUIState = isEmpty ? "EMPTY" : "FILLED";
        string characterData = item?.IsEmpty != false ? "EMPTY" : $"{item.itemData.ItemName} x{item.stackCount}";

        Debug.Log($"[InventorySlot] Sync slot {slotIndex} - UI: {currentUIState}, Character: {characterData}");

        // อัปเดตสถานะตาม inventory จริง
        if (item == null || item.IsEmpty)
        {
            if (!isEmpty) // ถ้าเดิมไม่ empty แต่ตอนนี้ empty แล้ว
            {
                Debug.Log($"[InventorySlot] 🔄 Slot {slotIndex}: Changing from FILLED to EMPTY");
                SetEmptyState();

                // 🆕 Force refresh หลัง set empty
                Canvas.ForceUpdateCanvases();
            }
        }
        else
        {
            // 🆕 ตรวจสอบว่าต้องอัปเดต UI หรือไม่
            bool needsUpdate = false;

            if (isEmpty)
            {
                // ถ้าเดิม empty แต่ตอนนี้มี item แล้ว
                needsUpdate = true;
                Debug.Log($"[InventorySlot] 🔄 Slot {slotIndex}: Changing from EMPTY to FILLED");
            }
            else
            {
                // ถ้ามี item อยู่แล้ว แต่ stack count อาจเปลี่ยน
                if (stackText != null && stackText.gameObject.activeSelf)
                {
                    string currentStackText = stackText.text;
                    string newStackText = item.stackCount > 1 ? item.stackCount.ToString() : "";

                    if (currentStackText != newStackText)
                    {
                        needsUpdate = true;
                        Debug.Log($"[InventorySlot] 🔄 Slot {slotIndex}: Stack count changed from '{currentStackText}' to '{newStackText}'");
                    }
                }
            }
            if (needsUpdate)
            {
                Sprite itemIcon = item.itemData.ItemIcon;
                int stackCount = item.itemData.CanStack() && item.stackCount > 1 ? item.stackCount : 0;

                SetFilledState(itemIcon, stackCount);

                // 🆕 เปลี่ยนจาก SetRarityColor เป็น SetTierBackground
                Color tierColor = item.itemData.GetTierColor();
                SetTierBackground(tierColor);

                Debug.Log($"[InventorySlot] ✅ Updated slot {slotIndex}: {item.itemData.ItemName} x{item.stackCount}");

                // Force refresh หลัง update
                Canvas.ForceUpdateCanvases();
            }
        }
    }
    public void SetTierBackground(Color tierColor)
    {
        if (tierBackground != null)
        {
            tierBackground.color = tierColor;
            tierBackground.enabled = true;
            Debug.Log($"[InventorySlot] 🌈 Slot {slotIndex}: Tier background = {tierColor}");
        }
        else
        {
            Debug.LogWarning($"[InventorySlot] Slot {slotIndex}: TierBackground not assigned in Inspector!");
        }
    }

    // 🆕 ปิด tier background
    public void DisableTierBackground()
    {
        if (tierBackground != null)
        {
            tierBackground.enabled = false;
            Debug.Log($"[InventorySlot] 🚫 Slot {slotIndex}: Tier background disabled");
        }
    }
    public void ForceSync()
    {
        Debug.Log($"[InventorySlot] 🔄 Force syncing slot {slotIndex}...");
        SyncWithCharacterInventory();
    }
    #endregion

    #region Public Methods for Testing

    #endregion
}