using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class InventorySlot : MonoBehaviour, IPointerClickHandler
{
    #region Events
    public static event Action<InventorySlot> OnSlotSelected;
    #endregion

    #region UI Components
    [Header("UI References")]
    public Image backgroundImage;      // พื้นหลัง slot
    public Image itemIconImage;        // รูป item
    public Image highlightImage;       // highlight เมื่อ selected
    public Button slotButton;          // button สำหรับ touch
    #endregion

    #region Colors for Different States
    [Header("Slot Colors")]
    public Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 1f);     // สีเทาเมื่อว่าง
    public Color filledColor = new Color(0.2f, 0.7f, 0.2f, 1f);    // สีเขียวเมื่อมี item
    public Color selectedColor = new Color(1f, 1f, 0f, 0.5f);       // สีเหลืองเมื่อ selected
    #endregion

    #region Slot Properties
    [Header("Slot Info")]
    public int slotIndex;              // ตำแหน่งใน grid
    public bool isEmpty = true;         // ว่างหรือไม่
    public bool isSelected = false;     // ถูกเลือกหรือไม่

    // TODO: Step 2 จะเพิ่ม ItemData ตรงนี้
    // public ItemData currentItem;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        InitializeComponents();
        SetupSlot();
    }

    void Start()
    {
        SetEmptyState();
    }
    #endregion

    #region Initialization
    void InitializeComponents()
    {
        // Auto-find components if not assigned
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (itemIconImage == null)
            itemIconImage = transform.Find("ItemIcon")?.GetComponent<Image>();

        if (highlightImage == null)
            highlightImage = transform.Find("Highlight")?.GetComponent<Image>();

        if (slotButton == null)
            slotButton = GetComponent<Button>();

        // Create missing components if needed
        CreateMissingComponents();
    }

    void CreateMissingComponents()
    {
        // สร้าง ItemIcon child object ถ้าไม่มี
        if (itemIconImage == null)
        {
            GameObject itemIconObj = new GameObject("ItemIcon");
            itemIconObj.transform.SetParent(transform);
            itemIconObj.transform.localPosition = Vector3.zero;
            itemIconObj.transform.localScale = Vector3.one;

            itemIconImage = itemIconObj.AddComponent<Image>();
            itemIconImage.raycastTarget = false; // ไม่ให้รบกวน touch events

            // ตั้งขนาดให้พอดีใน slot
            RectTransform iconRect = itemIconImage.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.one * 5f;  // padding 5px
            iconRect.offsetMax = Vector2.one * -5f;
        }

        // สร้าง Highlight child object ถ้าไม่มี
        if (highlightImage == null)
        {
            GameObject highlightObj = new GameObject("Highlight");
            highlightObj.transform.SetParent(transform);
            highlightObj.transform.localPosition = Vector3.zero;
            highlightObj.transform.localScale = Vector3.one;

            highlightImage = highlightObj.AddComponent<Image>();
            highlightImage.raycastTarget = false;

            // ตั้งขนาดให้เต็ม slot
            RectTransform highlightRect = highlightImage.GetComponent<RectTransform>();
            highlightRect.anchorMin = Vector2.zero;
            highlightRect.anchorMax = Vector2.one;
            highlightRect.offsetMin = Vector2.zero;
            highlightRect.offsetMax = Vector2.zero;
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
            slotButton.transition = Selectable.Transition.None; // ไม่ใช้ built-in transition
        }

        // ซ่อน highlight ตอนเริ่มต้น
        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(false);
        }

        // ซ่อน item icon ตอนเริ่มต้น
        if (itemIconImage != null)
        {
            itemIconImage.gameObject.SetActive(false);
        }
    }
    #endregion

    #region Touch Events (Mobile Optimized)
    public void OnPointerClick(PointerEventData eventData)
    {
        SelectSlot();
    }

    public void SelectSlot()
    {
        // แจ้งให้ InventoryGridManager รู้ว่า slot นี้ถูกเลือก
        OnSlotSelected?.Invoke(this);

        Debug.Log($"🎯 Slot {slotIndex} selected (isEmpty: {isEmpty})");
    }
    #endregion

    #region Visual State Management
    public void SetEmptyState()
    {
        isEmpty = true;
        isSelected = false;

        // ตั้งสีพื้นหลังเป็นสีเทา
        if (backgroundImage != null)
            backgroundImage.color = emptyColor;

        // ซ่อน item icon
        if (itemIconImage != null)
            itemIconImage.gameObject.SetActive(false);

        // ซ่อน highlight
        if (highlightImage != null)
            highlightImage.gameObject.SetActive(false);

        Debug.Log($"📦 Slot {slotIndex} set to empty state");
    }

    public void SetFilledState(Sprite itemIcon)
    {
        isEmpty = false;

        // ตั้งสีพื้นหลังเป็นสีเขียว
        if (backgroundImage != null)
            backgroundImage.color = filledColor;

        // แสดง item icon
        if (itemIconImage != null && itemIcon != null)
        {
            itemIconImage.sprite = itemIcon;
            itemIconImage.gameObject.SetActive(true);
        }

        Debug.Log($"📦 Slot {slotIndex} set to filled state");
    }

    public void SetSelectedState(bool selected)
    {
        isSelected = selected;

        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(selected);
            if (selected)
            {
                highlightImage.color = selectedColor;
            }
        }

        Debug.Log($"✨ Slot {slotIndex} selected: {selected}");
    }

    public void SetSlotIndex(int index)
    {
        slotIndex = index;
        gameObject.name = $"InventorySlot_{index:D2}"; // เช่น InventorySlot_01, InventorySlot_23
    }
    #endregion

    #region Public Methods for Future Steps
    // TODO: Step 2 จะเพิ่ม methods สำหรับ ItemData
    // public void SetItem(ItemData item) { }
    // public ItemData GetItem() { return currentItem; }
    // public void ClearItem() { }

    // สำหรับการทดสอบใน Step 1
    public void SetTestItem()
    {
        // สร้าง test sprite แบบง่ายๆ สำหรับทดสอบ
        Texture2D testTexture = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.red; // สีแดงสำหรับทดสอบ
        }
        testTexture.SetPixels(pixels);
        testTexture.Apply();

        Sprite testSprite = Sprite.Create(testTexture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        SetFilledState(testSprite);
    }
    #endregion

    #region Debug Methods
    [ContextMenu("Test - Set Empty")]
    public void TestSetEmpty()
    {
        SetEmptyState();
    }

    [ContextMenu("Test - Set Filled")]
    public void TestSetFilled()
    {
        SetTestItem();
    }

    [ContextMenu("Test - Toggle Selected")]
    public void TestToggleSelected()
    {
        SetSelectedState(!isSelected);
    }
    #endregion
}