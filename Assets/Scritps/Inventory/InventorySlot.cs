using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class InventorySlot : MonoBehaviour, IPointerClickHandler
{
    #region Events
    public static event Action<InventorySlot> OnSlotSelected;
    public static event Action<InventorySlot, ItemData> OnItemChanged; // เพิ่มสำหรับ Step 2
    #endregion

    #region UI Components
    [Header("UI References")]
    public Image backgroundImage;      // พื้นหลัง slot
    public Image itemIconImage;        // รูป item
    public Image highlightImage;       // highlight เมื่อ selected
    public Image tierBorderImage;      // border สำหรับแสดง tier สี
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

    [Header("Item Data")] // ✅ Step 2: เพิ่ม ItemData
    public ItemData currentItem;        // Item ที่อยู่ใน slot นี้
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

        if (tierBorderImage == null)
            tierBorderImage = transform.Find("TierBorder")?.GetComponent<Image>();

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
            iconRect.offsetMin = Vector2.one * 8f;  // padding 8px
            iconRect.offsetMax = Vector2.one * -8f;
        }

        // สร้าง TierBorder child object ถ้าไม่มี (สำหรับแสดงสี tier)
        if (tierBorderImage == null)
        {
            GameObject tierBorderObj = new GameObject("TierBorder");
            tierBorderObj.transform.SetParent(transform);
            tierBorderObj.transform.localPosition = Vector3.zero;
            tierBorderObj.transform.localScale = Vector3.one;

            tierBorderImage = tierBorderObj.AddComponent<Image>();
            tierBorderImage.raycastTarget = false;

            // ตั้งขนาดให้เต็ม slot (border)
            RectTransform borderRect = tierBorderImage.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;

            // ใช้ border sprite ถ้ามี หรือใช้สีแบบธรรมดา
            tierBorderImage.type = Image.Type.Sliced;
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

        // ซ่อน tier border ตอนเริ่มต้น
        if (tierBorderImage != null)
        {
            tierBorderImage.gameObject.SetActive(false);
        }
    }
    #endregion

    #region Touch Events (Mobile Optimized)
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"🖱️ OnPointerClick triggered on slot {slotIndex}");
        SelectSlot();
    }
    public void SelectSlot()
    {
        Debug.Log($"🎯 SelectSlot called for slot {slotIndex} (isEmpty: {isEmpty}, hasItem: {HasItem()})");

        // แจ้งให้ InventoryGridManager รู้ว่า slot นี้ถูกเลือก
        OnSlotSelected?.Invoke(this);

        // Debug: แสดงข้อมูล item
        if (HasItem() && currentItem != null)
        {
            Debug.Log($"📦 Selected item: {currentItem.ItemName} ({currentItem.ItemType})");
        }
        else
        {
            Debug.Log($"📭 Selected empty slot {slotIndex}");
        }
    }
    #endregion

    #region Visual State Management
    public void SetEmptyState()
    {
        isEmpty = true;
        isSelected = false;
        currentItem = null;

        // ตั้งสีพื้นหลังเป็นสีเทา
        if (backgroundImage != null)
            backgroundImage.color = emptyColor;

        // ซ่อน item icon
        if (itemIconImage != null)
            itemIconImage.gameObject.SetActive(false);

        // ซ่อน tier border
        if (tierBorderImage != null)
            tierBorderImage.gameObject.SetActive(false);

        // ซ่อน highlight
        if (highlightImage != null)
            highlightImage.gameObject.SetActive(false);

        // แจ้งให้ระบบอื่นรู้ว่า item เปลี่ยน
        OnItemChanged?.Invoke(this, null);

       // Debug.Log($"📦 Slot {slotIndex} set to empty state");
    }

    public void SetFilledState(ItemData item)
    {
        if (item == null)
        {
            SetEmptyState();
            return;
        }

        isEmpty = false;
        currentItem = item;

        // ตั้งสีพื้นหลังเป็นสีเขียว
        if (backgroundImage != null)
            backgroundImage.color = filledColor;

        // แสดง item icon
        if (itemIconImage != null && item.ItemIcon != null)
        {
            itemIconImage.sprite = item.ItemIcon;
            itemIconImage.gameObject.SetActive(true);
        }

        // แสดง tier border ด้วยสีของ tier
        if (tierBorderImage != null)
        {
            tierBorderImage.color = item.GetTierColor();
            tierBorderImage.gameObject.SetActive(true);
        }

        // แจ้งให้ระบบอื่นรู้ว่า item เปลี่ยน
        OnItemChanged?.Invoke(this, item);

        Debug.Log($"📦 Slot {slotIndex} set to filled state with {item.ItemName} ({item.GetTierText()})");
    }

    // Backward compatibility สำหรับ Step 1
    public void SetFilledState(Sprite itemIcon)
    {
        isEmpty = false;

        if (backgroundImage != null)
            backgroundImage.color = filledColor;

        if (itemIconImage != null && itemIcon != null)
        {
            itemIconImage.sprite = itemIcon;
            itemIconImage.gameObject.SetActive(true);
        }

        if (tierBorderImage != null)
            tierBorderImage.gameObject.SetActive(false); // ไม่มี tier สำหรับ test sprite

        Debug.Log($"📦 Slot {slotIndex} set to filled state (legacy mode)");
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

    #region Item Management Methods (Step 2)
    public void SetItem(ItemData item)
    {
        Debug.Log($"🔄 SetItem called for slot {slotIndex}: {item?.ItemName ?? "NULL"}");

        if (item == null)
        {
            SetEmptyState();
        }
        else
        {
            SetFilledState(item);
        }

        Debug.Log($"✅ Slot {slotIndex} after SetItem: isEmpty={isEmpty}, hasItem={HasItem()}");
    }

    public ItemData GetItem()
    {
        return currentItem;
    }

    public void ClearItem()
    {
        SetEmptyState();
    }

    public bool HasItem()
    {
        bool hasItem = currentItem != null && !isEmpty;
        Debug.Log($"🔍 Slot {slotIndex} HasItem(): {hasItem} (currentItem: {currentItem?.ItemName ?? "NULL"}, isEmpty: {isEmpty})");
        return hasItem;
    }

    public bool CanAcceptItem(ItemData item)
    {
        // สำหรับ inventory slots ธรรมดา สามารถใส่ item อะไรก็ได้
        // (Equipment slots จะมีการจำกัดประเภท)
        return item != null;
    }

    public string GetItemId()
    {
        return currentItem?.ItemId ?? "";
    }

    public string GetItemName()
    {
        return currentItem?.ItemName ?? "";
    }

    public ItemType GetItemType()
    {
        return currentItem?.ItemType ?? ItemType.Weapon;
    }

    public ItemTier GetItemTier()
    {
        return currentItem?.Tier ?? ItemTier.Common;
    }
    #endregion

    #region Test Methods (Step 1 & Step 2)
    public void SetTestItem()
    {
        // สำหรับ Step 1 - ใช้ test sprite
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

    public void SetRandomTestItem()
    {
        // สำหรับ Step 2 - ใช้ item จาก database
        if (ItemDatabase.Instance != null)
        {
            ItemData randomItem = ItemDatabase.Instance.GetRandomItem();
            if (randomItem != null)
            {
                SetItem(randomItem);
                return;
            }
        }

        // Fallback เป็น test sprite ถ้าไม่มี database
        SetTestItem();
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
        SetRandomTestItem();
    }

    [ContextMenu("Test - Toggle Selected")]
    public void TestToggleSelected()
    {
        SetSelectedState(!isSelected);
    }

    [ContextMenu("Debug Item Info")]
    public void DebugItemInfo()
    {
        if (currentItem != null)
        {
            Debug.Log($"📦 Slot {slotIndex}: {currentItem.ItemName}");
            Debug.Log($"   Type: {currentItem.ItemType}, Tier: {currentItem.GetTierText()}");
            Debug.Log($"   Stats: {currentItem.Stats.GetStatsDescription()}");
        }
        else
        {
            Debug.Log($"📦 Slot {slotIndex}: Empty");
        }
    }

    [ContextMenu("Test - Fire Selection Event")]
    public void TestFireSelectionEvent()
    {
        Debug.Log($"🧪 Testing selection event for slot {slotIndex}");
        SelectSlot();
    }

    [ContextMenu("Test - Debug Slot State")]
    public void TestDebugSlotState()
    {
        Debug.Log($"📦 Slot {slotIndex} Debug:");
        Debug.Log($"   isEmpty: {isEmpty}");
        Debug.Log($"   isSelected: {isSelected}");
        Debug.Log($"   currentItem: {currentItem?.ItemName ?? "NULL"}");
        Debug.Log($"   HasItem(): {HasItem()}");
        Debug.Log($"   Button interactable: {(slotButton != null ? slotButton.interactable.ToString() : "N/A")}");
        Debug.Log($"   GameObject active: {gameObject.activeSelf}");
    }
    #endregion
}