using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemValuePanel : MonoBehaviour
{
    [Header("UI References")]
    public Image itemIconImage;             // รูปไอเทม
    public TextMeshProUGUI itemNameText;    // ชื่อไอเทม
    public TextMeshProUGUI itemTierText;    // ระดับความหายาก
    public Image itemTierBackground;        // พื้นหลังแสดงสี tier

    [Header("💰 Value Information")]
    public TextMeshProUGUI sellPriceText;   // ราคาขาย
    public TextMeshProUGUI buyPriceText;    // ราคาซื้อ
    public TextMeshProUGUI stackValueText;  // มูลค่ารวม (ถ้ามี stack)
    public TextMeshProUGUI sellableText;    // สถานะขายได้หรือไม่
    public TextMeshProUGUI tradeableText;   // สถานะแลกเปลี่ยนได้หรือไม่

    [Header("Buttons")]
    public Button closeButton;              // ปุ่มปิด panel

    private InventoryItem currentItem;      // Item ที่กำลังแสดงอยู่

    private void Awake()
    {
        // Setup close button
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HideValuePanel);
        }

        // ซ่อน panel ตั้งแต่เริ่มต้น
        HideValuePanel();
    }

    public void ShowValuePanel(InventoryItem item)
    {
        if (item == null || item.IsEmpty)
        {
            HideValuePanel();
            return;
        }

        ItemData itemData = item.itemData;
        currentItem = item;

        // แสดง panel
        gameObject.SetActive(true);

        // แสดงรูปไอเทม
        if (itemIconImage != null)
        {
            itemIconImage.sprite = itemData.ItemIcon;
            itemIconImage.color = Color.white;
            SetItemIconTierBackground(itemData.GetTierColor());
        }

        // แสดงชื่อไอเทม
        if (itemNameText != null)
        {
            itemNameText.text = itemData.ItemName;
            itemNameText.color = itemData.GetTierColor();
        }

        // แสดงระดับความหายาก
        if (itemTierText != null)
        {
            itemTierText.text = itemData.GetTierText();
            itemTierText.color = itemData.GetTierColor();
        }

        // แสดงราคาขาย
        if (sellPriceText != null)
        {
            if (itemData.SellPrice > 0)
            {
                sellPriceText.text = $"Sell:{itemData.SellPrice:N0} Gold";
            }
            else
            {
                sellPriceText.text = "Cannot Sell";
            }
        }

        // แสดงราคาซื้อ
        if (buyPriceText != null)
        {
            if (itemData.BuyPrice > 0)
            {
                buyPriceText.text = $"Buy:{itemData.BuyPrice:N0} Gold";
            }
            else
            {
                buyPriceText.text = "Not for Sale";
            }
        }

        // แสดงมูลค่ารวม (สำหรับ stack)
        if (stackValueText != null)
        {
            if (item.stackCount > 1 && itemData.SellPrice > 0)
            {
                long totalValue = itemData.GetSellValue(item.stackCount);
                stackValueText.text = $"Total Value: {totalValue:N0} Gold ({item.stackCount}x)";
                stackValueText.gameObject.SetActive(true);
            }
            else
            {
                stackValueText.gameObject.SetActive(false);
            }
        }

        // แสดงสถานะขายได้
        if (sellableText != null)
        {
            if (itemData.IsSellable)
            {
                sellableText.text = "✅ Sellable";
            }
            else
            {
                sellableText.text = "❌ Cannot Sell";
            }
        }

        // แสดงสถานะแลกเปลี่ยนได้
        if (tradeableText != null)
        {
            if (itemData.IsTradeable)
            {
                tradeableText.text = "✅ Tradeable";
            }
            else
            {
                tradeableText.text = "❌ Cannot Trade";
                tradeableText.color = Color.red;
            }
        }

        Debug.Log($"[ItemValuePanel] Showing value info for: {itemData.ItemName}");
    }

    private void SetItemIconTierBackground(Color tierColor)
    {
        if (itemTierBackground != null)
        {
            itemTierBackground.color = tierColor;
            itemTierBackground.enabled = true;
        }
    }

    public void HideValuePanel()
    {
        if (itemTierBackground != null)
        {
            itemTierBackground.enabled = false;
        }

        gameObject.SetActive(false);
        Debug.Log("[ItemValuePanel] Value panel hidden");
    }

    public bool IsVisible()
    {
        return gameObject.activeSelf;
    }
}