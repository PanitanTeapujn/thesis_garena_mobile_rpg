using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Fusion;
public class ShopItemUI : MonoBehaviour
{
    [Header("UI Components")]
    public Image itemIcon;
    public TextMeshProUGUI itemName;
    public TextMeshProUGUI itemPrice;
    public Button itemButton;
    public Image tierBackground;

    private ShopItemData shopItemData;
    private ShopLooby shopLooby;

    public void SetupShopItem(ShopItemData data, ShopLooby shop)
    {
        shopItemData = data;
        shopLooby = shop;

        if (data == null || data.itemData == null)
        {
            Debug.LogError("[ShopItemUI] Invalid shop item data");
            return;
        }

        ItemData itemData = data.itemData;

        // แสดงรูปไอเทม
        if (itemIcon != null)
        {
            itemIcon.sprite = itemData.ItemIcon;
            itemIcon.color = Color.white;
        }

        // แสดงชื่อไอเทม
        if (itemName != null)
        {
            itemName.text = itemData.ItemName;
        }

        // ✅ แสดงราคา - ตรวจสอบว่าเป็น Trade mode หรือไม่
        if (itemPrice != null)
        {
            if (IsTradeMode())
            {
                // ถ้าเป็น Trade mode ให้แสดงข้อความ "TRADE"
                itemPrice.text = "TRADE";
            }
            else
            {
                // แสดงราคาปกติ
                string currencyText = data.currencyType == CurrencyType.Gold ? "Gold" : "Gems";
                itemPrice.text = $"{data.price:N0} {currencyText}";
            }
        }

        // ตั้งค่า tier background
        if (tierBackground != null)
        {
            tierBackground.color = itemData.GetTierColor();
        }

        // ตั้งค่าปุ่ม
        if (itemButton != null)
        {
            itemButton.onClick.RemoveAllListeners();
            itemButton.onClick.AddListener(OnItemClicked);
        }

        Debug.Log($"[ShopItemUI] Setup complete: {itemData.ItemName} - {(IsTradeMode() ? "TRADE" : data.price + " gold")}");
    }

    // ✅ เพิ่ม method ตรวจสอบ Trade mode
    private bool IsTradeMode()
    {
        // ตรวจสอบว่าอยู่ใน Trade panel หรือไม่
        return shopLooby != null &&
               shopLooby.tradePanel != null &&
               shopLooby.tradePanel.activeSelf;
    }

    private void OnItemClicked()
    {
        if (shopLooby != null && shopItemData != null)
        {
            shopLooby.ShowItemDetailPanel(shopItemData);
            Debug.Log($"[ShopItemUI] Item clicked: {shopItemData.itemData.ItemName}");
        }
    }
}
