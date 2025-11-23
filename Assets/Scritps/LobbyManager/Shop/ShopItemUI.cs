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

    [Header("🔨 Enchance Mode Components")]
    public GameObject enchantableBadge;          // ✅ Badge "Enchantable"
    public TextMeshProUGUI enchantableText;      // ✅ Text บน badge
    public GameObject materialBadge;             // ✅ Badge "Material"

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

        // ✅ แสดงราคา - ตรวจสอบว่าเป็น Trade mode หรือ Enchance mode
        if (itemPrice != null)
        {
            if (IsTradeMode())
            {
                itemPrice.text = "TRADE";
            }
            else if (IsEnchanceMode())
            {
                // ✅ ในโหมด Enchance ไม่แสดงราคา
                itemPrice.text = "";
            }
            else
            {
                string currencyText = data.currencyType == CurrencyType.Gold ? "Gold" : "Gems";
                itemPrice.text = $"{data.price:N0} {currencyText}";
            }
        }

        // ตั้งค่า tier background
        if (tierBackground != null)
        {
            tierBackground.color = itemData.GetTierColor();
        }

        // ✅ แสดง Enchance badges
        SetupEnchanceBadges(itemData);

        // ตั้งค่าปุ่ม
        if (itemButton != null)
        {
            itemButton.onClick.RemoveAllListeners();
            itemButton.onClick.AddListener(OnItemClicked);
        }
    }

    // ✅ เพิ่ม method ตรวจสอบ Enchance mode
    private bool IsEnchanceMode()
    {
        // ตรวจสอบว่าอยู่ใน Enchance panel หรือไม่
        EnchanceLobby enchanceLobby = FindObjectOfType<EnchanceLobby>();
        return enchanceLobby != null &&
               enchanceLobby.enchancePanel != null &&
               enchanceLobby.enchancePanel.activeSelf;
    }

    private bool IsTradeMode()
    {
        return shopLooby != null &&
               shopLooby.tradePanel != null &&
               shopLooby.tradePanel.activeSelf;
    }

    // ✅ Setup Enchance Badges
    private void SetupEnchanceBadges(ItemData itemData)
    {
        // ซ่อน badges ทั้งหมดก่อน
        if (enchantableBadge != null) enchantableBadge.SetActive(false);
        if (materialBadge != null) materialBadge.SetActive(false);

        // ถ้าไม่ได้อยู่ใน Enchance mode ก็ไม่ต้องแสดง
        if (!IsEnchanceMode()) return;

        // ตรวจสอบว่า item นี้ enchant ได้หรือไม่
        bool canBeEnchanted = itemData.CanBeEnchanted && itemData.IsEquippableItem();
        bool canBeMaterial = CanBeUsedAsMaterial(itemData);

        if (canBeEnchanted)
        {
            // แสดง badge "Enchantable"
            if (enchantableBadge != null)
            {
                enchantableBadge.SetActive(true);

                if (enchantableText != null)
                {
                    enchantableText.text = "Enchantable";
                    enchantableText.color = Color.yellow;
                }
            }
        }
        else if (canBeMaterial)
        {
            // แสดง badge "Material"
            if (materialBadge != null)
            {
                materialBadge.SetActive(true);
            }
            else if (enchantableBadge != null)
            {
                // ถ้าไม่มี materialBadge ใช้ enchantableBadge แทน
                enchantableBadge.SetActive(true);

                if (enchantableText != null)
                {
                    enchantableText.text = "Material";
                    enchantableText.color = Color.cyan;
                }
            }
        }
    }

    // ✅ Helper method
    private bool CanBeUsedAsMaterial(ItemData itemData)
    {
        if (itemData == null) return false;

        switch (itemData.ItemType)
        {
            case ItemType.Weapon:
            case ItemType.Head:
            case ItemType.Armor:
            case ItemType.Pants:
            case ItemType.Shoes:
            case ItemType.Material:
            case ItemType.Misc:
                return true;
            default:
                return false;
        }
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