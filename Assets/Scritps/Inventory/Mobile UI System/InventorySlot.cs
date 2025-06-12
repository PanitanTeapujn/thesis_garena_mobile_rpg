using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[System.Serializable]
public class InventorySlotData
{
    public string itemId;
    public int quantity;
    public int slotIndex;
    public EquipmentType equipmentType;

    public InventorySlotData(string id, int qty, int slot)
    {
        itemId = id;
        quantity = qty;
        slotIndex = slot;
    }
}

public class InventorySlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Components")]
    public Image slotBackground;
    public Image itemIcon;
    public TextMeshProUGUI quantityText;
    public Image rarityBorder;

    [Header("Visual Settings")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;
    public Color selectedColor = Color.green;

    // Data
    private InventorySlotData slotData;
    private ItemData currentItem;
    private bool isSelected = false;
    private EquipmentType equipmentType;

    // Events
    public System.Action<InventorySlotData> OnSlotClicked;
    public System.Action<InventorySlotData> OnSlotHover;

    void Start()
    {
        // รอให้ UI Components ถูกสร้างเสร็จก่อน
        InitializeSlot();
    }

    private void InitializeSlot()
    {
        // ตรวจสอบว่า UI Components ถูก assign แล้วหรือไม่
        if (slotBackground == null)
        {
            Debug.LogError($"SlotBackground is not assigned in {gameObject.name}");
            return;
        }

        if (itemIcon == null)
        {
            Debug.LogError($"ItemIcon is not assigned in {gameObject.name}");
            return;
        }

        if (quantityText == null)
        {
            Debug.LogError($"QuantityText is not assigned in {gameObject.name}");
            return;
        }

        ClearSlot();
    }

    public void SetSlotIndex(int index)
    {
        if (slotData == null)
            slotData = new InventorySlotData("", 0, index);
        else
            slotData.slotIndex = index;
    }

    public void SetEquipmentType(EquipmentType type)
    {
        equipmentType = type;
        if (slotData != null)
            slotData.equipmentType = type;
    }

    public void SetItem(ItemData item, int quantity)
    {
        if (item == null)
        {
            Debug.LogWarning("Trying to set null item in inventory slot");
            ClearSlot();
            return;
        }

        currentItem = item;

        if (slotData == null)
            slotData = new InventorySlotData(item.itemId, quantity, -1);
        else
        {
            slotData.itemId = item.itemId;
            slotData.quantity = quantity;
        }

        // Update visual with null checks
        if (itemIcon != null)
        {
            itemIcon.sprite = item.icon;
            itemIcon.color = Color.white;
            itemIcon.gameObject.SetActive(true);
        }

        // Update quantity text
        if (quantityText != null)
        {
            if (quantity > 1)
            {
                quantityText.text = quantity.ToString();
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
        }

        // Update rarity border
        if (rarityBorder != null)
        {
            rarityBorder.color = item.GetRarityColor();
            rarityBorder.gameObject.SetActive(true);
        }

        // Update background
        if (slotBackground != null)
        {
            slotBackground.color = normalColor;
        }
    }

    public void ClearSlot()
    {
        currentItem = null;

        if (slotData == null)
            slotData = new InventorySlotData("", 0, -1);
        else
        {
            slotData.itemId = "";
            slotData.quantity = 0;
        }

        // Clear visual with null checks
        if (itemIcon != null)
        {
            itemIcon.gameObject.SetActive(false);
        }

        if (quantityText != null)
        {
            quantityText.gameObject.SetActive(false);
        }

        if (rarityBorder != null)
        {
            rarityBorder.gameObject.SetActive(false);
        }

        if (slotBackground != null)
        {
            slotBackground.color = normalColor;
        }

        isSelected = false;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (slotBackground != null)
        {
            slotBackground.color = selected ? selectedColor : normalColor;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnSlotClicked?.Invoke(slotData);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSelected && slotBackground != null)
        {
            slotBackground.color = highlightColor;
        }

        OnSlotHover?.Invoke(slotData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelected && slotBackground != null)
        {
            slotBackground.color = normalColor;
        }
    }

    public bool IsEmpty()
    {
        return currentItem == null || string.IsNullOrEmpty(slotData?.itemId);
    }

    public ItemData GetItem()
    {
        return currentItem;
    }

    public int GetQuantity()
    {
        return slotData?.quantity ?? 0;
    }

    public string GetItemId()
    {
        return slotData?.itemId ?? "";
    }

    // เพิ่มฟังก์ชันตรวจสอบว่า slot พร้อมใช้งานหรือไม่
    public bool IsSlotReady()
    {
        return slotBackground != null && itemIcon != null && quantityText != null;
    }
}