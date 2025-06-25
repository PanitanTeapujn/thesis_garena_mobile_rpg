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
        }

        // ซ่อน stack text ไว้ก่อน (ใช้ในอนาคต)
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
        isEmpty = true;
        isSelected = false;

        if (slotBackground != null)
            slotBackground.color = emptySlotColor;

        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.gameObject.SetActive(false);
        }

        if (stackText != null)
            stackText.gameObject.SetActive(false);

        Debug.Log($"[InventorySlot] Slot {slotIndex} set to empty state");
    }

    public void SetFilledState(Sprite itemSprite, int stackCount = 1)
    {
        if (itemSprite == null)
        {
            Debug.LogWarning($"[InventorySlot] Trying to fill slot {slotIndex} with null sprite!");
            SetEmptyState();
            return;
        }

        isEmpty = false;
        isSelected = false;

        if (slotBackground != null)
            slotBackground.color = filledSlotColor;

        if (itemIcon != null)
        {
            itemIcon.sprite = itemSprite;
            itemIcon.gameObject.SetActive(true);
        }

        // Stack text (สำหรับอนาคต)
        if (stackText != null && stackCount > 1)
        {
            stackText.text = stackCount.ToString();
            stackText.gameObject.SetActive(true);
        }
        else if (stackText != null)
        {
            stackText.gameObject.SetActive(false);
        }

        Debug.Log($"[InventorySlot] Slot {slotIndex} filled with item: {itemSprite.name}");
    }

    public void SetSelectedState(bool selected)
    {
        isSelected = selected;

        if (slotBackground != null)
        {
            if (isSelected)
            {
                slotBackground.color = selectedSlotColor;
                Debug.Log($"[InventorySlot] Slot {slotIndex} selected");
            }
            else
            {
                // กลับไปสีปกติตามสถานะ empty/filled
                slotBackground.color = isEmpty ? emptySlotColor : filledSlotColor;
                Debug.Log($"[InventorySlot] Slot {slotIndex} deselected");
            }
        }
    }
    #endregion

    #region Button Events
    private void OnSlotClicked()
    {
        Debug.Log($"[InventorySlot] Slot {slotIndex} clicked - Empty: {isEmpty}, Selected: {isSelected}");

        // แจ้ง InventoryGridManager ว่า slot นี้ถูกกด
        OnSlotSelected?.Invoke(slotIndex);
    }
    #endregion

    #region Public Methods for Testing
    [ContextMenu("Test: Set Empty")]
    private void TestSetEmpty()
    {
        SetEmptyState();
    }

    [ContextMenu("Test: Set Filled")]
    private void TestSetFilled()
    {
        // ใช้ sprite ปัจจุบันของ itemIcon หรือสร้างสี solid
        if (itemIcon != null)
        {
            // สร้าง texture สี่เหลี่ยมสีแดงทดสอบ
            Texture2D testTexture = new Texture2D(64, 64);
            Color[] colors = new Color[64 * 64];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = Color.red;
            testTexture.SetPixels(colors);
            testTexture.Apply();

            Sprite testSprite = Sprite.Create(testTexture, new Rect(0, 0, 64, 64), Vector2.one * 0.5f);
            SetFilledState(testSprite);
        }
    }

    [ContextMenu("Test: Toggle Selected")]
    private void TestToggleSelected()
    {
        SetSelectedState(!isSelected);
    }
    #endregion
}