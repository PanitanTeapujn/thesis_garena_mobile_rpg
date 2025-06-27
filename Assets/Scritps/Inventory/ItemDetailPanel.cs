using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemDetailPanel : MonoBehaviour
{
    [Header("UI References")]
    public Image itemIconImage;             // รูปไอเทม
    public TextMeshProUGUI itemNameText;    // ชื่อไอเทม
    public TextMeshProUGUI itemTypeText;    // ประเภทไอเทม
    public TextMeshProUGUI itemTierText;    // ระดับความหายาก
    public TextMeshProUGUI itemDescText;    // คำอธิบายไอเทม
    public TextMeshProUGUI itemStatsText;   // สถิติไอเทม
    public TextMeshProUGUI stackCountText;  // จำนวนไอเทม
    public Button closeButton;              // ปุ่มปิด panel

    private void Awake()
    {
        // Setup close button
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HideItemDetail);
        }

        // ซ่อน panel ตั้งแต่เริ่มต้น
        HideItemDetail();
    }

    public void ShowItemDetail(InventoryItem item)
    {
        if (item == null || item.IsEmpty)
        {
            HideItemDetail();
            return;
        }

        ItemData itemData = item.itemData;

        // แสดง panel
        gameObject.SetActive(true);

        // แสดงรูปไอเทม
        if (itemIconImage != null)
        {
            itemIconImage.sprite = itemData.ItemIcon;
            itemIconImage.color = itemData.GetTierColor();
        }

        // แสดงชื่อไอเทม
        if (itemNameText != null)
        {
            itemNameText.text = itemData.ItemName;
            itemNameText.color = itemData.GetTierColor();
        }

        // แสดงประเภทไอเทม
        if (itemTypeText != null)
        {
            itemTypeText.text = $"Type: {itemData.ItemType}";
        }

        // แสดงระดับความหายาก
        if (itemTierText != null)
        {
            itemTierText.text = itemData.GetTierText();
            itemTierText.color = itemData.GetTierColor();
        }

        // แสดงคำอธิบาย
        if (itemDescText != null)
        {
            itemDescText.text = string.IsNullOrEmpty(itemData.Description) ?
                "No description available." : itemData.Description;
        }

        // แสดงสถิติ
        if (itemStatsText != null)
        {
            string statsText = itemData.Stats.GetStatsDescription();
            itemStatsText.text = string.IsNullOrEmpty(statsText) ?
                "No bonus stats" : statsText;
        }

        // แสดงจำนวนไอเทม (สำหรับไอเทมที่ stack ได้)
        if (stackCountText != null)
        {
            if (item.stackCount > 1)
            {
                stackCountText.text = $"Quantity: {item.stackCount}";
                stackCountText.gameObject.SetActive(true);
            }
            else
            {
                stackCountText.gameObject.SetActive(false);
            }
        }

        Debug.Log($"[ItemDetailPanel] Showing details for: {itemData.ItemName}");
    }

    public void HideItemDetail()
    {
        gameObject.SetActive(false);
        Debug.Log("[ItemDetailPanel] Item detail panel hidden");
    }

    public bool IsVisible()
    {
        return gameObject.activeSelf;
    }
}