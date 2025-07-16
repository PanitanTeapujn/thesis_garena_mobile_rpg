using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;

public class ShopLooby : MonoBehaviour
{
    [Header("Main Controls")]
    public Button closeShop;
    public GameObject shopPanel;

    [Header("Navigation")]
    public Button beginJourneyTab;
    public Button legendShopTab;
    public Button sellTab;

    [Header("Shop Panels")]
    public GameObject beginJourneyPanel;
    public GameObject legendShopPanel;
    public GameObject sellPanel;

    [Header("Begin Journey Shop")]
    public Transform beginJourneyContainer;
    public GameObject shopItemPrefab;
    public ScrollRect beginJourneyScrollRect;

    [Header("Shop Item Detail Panel")]
    public GameObject shopItemDetailPanel;
    public Image detailItemIcon;
    public TextMeshProUGUI detailItemName;
    public TextMeshProUGUI detailItemType;
    public TextMeshProUGUI detailItemTier;
    public TextMeshProUGUI detailItemDescription;
    public TextMeshProUGUI detailItemStats;
    public TextMeshProUGUI detailItemPrice;
    public Button buyButton;
    public Button closeDetailButton;

    [Header("🆕 Purchase Quantity Panel")]
    public GameObject purchaseQuantityPanel;
    public Image quantityItemIcon;
    public TextMeshProUGUI quantityItemName;
    public TextMeshProUGUI quantityItemPrice;
    public Slider quantitySlider;
    public TextMeshProUGUI quantityValueText;
    public TextMeshProUGUI totalPriceText;
    public Button confirmPurchaseButton;
    public Button cancelPurchaseButton;
    public TextMeshProUGUI maxAffordableText;

    [Header("Shop Data")]
    public List<ShopItemData> beginJourneyItems = new List<ShopItemData>();

    private ShopItemData selectedShopItem;
    private CurrencyManager currencyManager;
    private Character playerCharacter;

    // ✅ เพิ่มตัวแปรสำหรับ Quantity System
    private int selectedQuantity = 1;
    private int maxAffordableQuantity = 1;

    #region Unity Lifecycle
    void Start()
    {
        InitializeShop();
        SetupEventListeners();
        SetupShopData();
    }

    void OnDestroy()
    {
        RemoveEventListeners();
    }
    #endregion

    #region Initialization
    private void InitializeShop()
    {
        // หา CurrencyManager
        currencyManager = CurrencyManager.FindCurrencyManager();
        if (currencyManager == null)
        {
            Debug.LogWarning("[ShopLooby] CurrencyManager not found");
        }

        // หา Player Character
        playerCharacter = FindObjectOfType<Character>();
        if (playerCharacter == null)
        {
            Debug.LogWarning("[ShopLooby] Player Character not found");
        }

        // ตั้งค่า UI เริ่มต้น
        ShowBeginJourneyPanel();
        HideItemDetailPanel();
        HidePurchaseQuantityPanel(); // ✅ เพิ่มบรรทัดนี้
    }

    private void SetupEventListeners()
    {
        // Main controls
        if (closeShop != null)
            closeShop.onClick.AddListener(CloseShop);

        // Navigation tabs
        if (beginJourneyTab != null)
            beginJourneyTab.onClick.AddListener(ShowBeginJourneyPanel);
        if (legendShopTab != null)
            legendShopTab.onClick.AddListener(ShowLegendShopPanel);
        if (sellTab != null)
            sellTab.onClick.AddListener(ShowSellPanel);

        // Detail panel
        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyButtonClicked);
        if (closeDetailButton != null)
            closeDetailButton.onClick.AddListener(HideItemDetailPanel);

        // ✅ Purchase Quantity Panel
        if (confirmPurchaseButton != null)
            confirmPurchaseButton.onClick.AddListener(OnConfirmPurchaseClicked);
        if (cancelPurchaseButton != null)
            cancelPurchaseButton.onClick.AddListener(OnCancelPurchaseClicked);
        if (quantitySlider != null)
            quantitySlider.onValueChanged.AddListener(OnQuantitySliderChanged);
    }

    private void RemoveEventListeners()
    {
        if (closeShop != null)
            closeShop.onClick.RemoveAllListeners();
        if (beginJourneyTab != null)
            beginJourneyTab.onClick.RemoveAllListeners();
        if (legendShopTab != null)
            legendShopTab.onClick.RemoveAllListeners();
        if (sellTab != null)
            sellTab.onClick.RemoveAllListeners();
        if (buyButton != null)
            buyButton.onClick.RemoveAllListeners();
        if (closeDetailButton != null)
            closeDetailButton.onClick.RemoveAllListeners();

        // ✅ Purchase Quantity Panel
        if (confirmPurchaseButton != null)
            confirmPurchaseButton.onClick.RemoveAllListeners();
        if (cancelPurchaseButton != null)
            cancelPurchaseButton.onClick.RemoveAllListeners();
        if (quantitySlider != null)
            quantitySlider.onValueChanged.RemoveAllListeners();
    }

    private void SetupShopData()
    {
        // ✅ Auto setup layout ก่อนสร้างไอเท็ม
        AutoSetupLayout();

        // ถ้าไม่มีข้อมูลใน Inspector ให้สร้างจาก ItemDatabase
        if (beginJourneyItems.Count == 0)
        {
            LoadBeginJourneyItemsFromDatabase();
        }

        // สร้าง UI สำหรับ Begin Journey items
        CreateBeginJourneyShopItems();
    }

    // ✅ เพิ่ม method สำหรับตั้งค่า Layout อัตโนมัติ
    private void AutoSetupLayout()
    {
        if (beginJourneyContainer == null)
        {
            Debug.LogError("[ShopLooby] beginJourneyContainer is null! Please assign it in Inspector");
            return;
        }

        // ลบ Layout Group เก่า (ถ้ามี)
        var existingLayouts = beginJourneyContainer.GetComponents<LayoutGroup>();
        for (int i = 0; i < existingLayouts.Length; i++)
        {
            if (Application.isPlaying)
                DestroyImmediate(existingLayouts[i]);
            else
                DestroyImmediate(existingLayouts[i]);
        }

        // เพิ่ม Grid Layout Group
        GridLayoutGroup gridLayout = beginJourneyContainer.gameObject.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(150, 200);
        gridLayout.spacing = new Vector2(10, 10);
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.UpperCenter;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 3; // 3 คอลัมน์

        // เพิ่ม Content Size Fitter
        ContentSizeFitter fitter = beginJourneyContainer.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = beginJourneyContainer.gameObject.AddComponent<ContentSizeFitter>();
        }
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Debug.Log("[ShopLooby] ✅ Auto setup grid layout completed - 3 columns with 150x200 cells");
    }

    private void LoadBeginJourneyItemsFromDatabase()
    {
        ItemDatabase database = ItemDatabase.Instance;
        if (database == null)
        {
            Debug.LogError("[ShopLooby] ItemDatabase not found");
            CreateFallbackItems(); // สร้างไอเทมสำรองถ้าไม่มี database
            return;
        }

        beginJourneyItems.Clear();

        // ✅ วิธีที่ 1: โหลดทุกไอเทมจาก database แล้วกรอง
        var allItems = database.GetAllItems();
        Debug.Log($"[ShopLooby] Found {allItems.Count} total items in database");

        if (allItems.Count == 0)
        {
            Debug.LogWarning("[ShopLooby] No items found in database, creating fallback items");
            CreateFallbackItems();
            return;
        }

        // เพิ่มไอเทมแต่ละประเภทพร้อมกำหนดราคา
        foreach (var item in allItems)
        {
            if (item == null) continue;

            long itemPrice = DetermineItemPrice(item);

            // กรองเฉพาะไอเทมที่เหมาะสำหรับ Begin Journey
            if (IsBeginJourneyItem(item))
            {
                beginJourneyItems.Add(new ShopItemData(item, itemPrice));
                Debug.Log($"[ShopLooby] Added {item.ItemName} ({item.ItemType}) - Price: {itemPrice}");
            }
        }

        // ✅ ถ้ายังไม่มีไอเทมใน shop ให้สร้างสำรอง
        if (beginJourneyItems.Count == 0)
        {
            Debug.LogWarning("[ShopLooby] No suitable items found, creating fallback items");
            CreateFallbackItems();
        }

        Debug.Log($"[ShopLooby] ✅ Loaded {beginJourneyItems.Count} items for Begin Journey shop");
    }

    // ✅ กำหนดเงื่อนไขว่าไอเทมไหนเหมาะสำหรับ Begin Journey
    private bool IsBeginJourneyItem(ItemData item)
    {
        // เอาไอเทม Common และ Uncommon
        if (item.Tier == ItemTier.Common || item.Tier == ItemTier.Uncommon)
            return true;

        // เอา Potion ทุกระดับ
        if (item.ItemType == ItemType.Potion)
            return true;

        // เอา Material สำหรับ crafting
        if (item.ItemType == ItemType.Material)
            return true;

        return false;
    }

    // ✅ กำหนดราคาตาม tier และ type
    private long DetermineItemPrice(ItemData item)
    {
        // ใช้ราคาจาก ItemData ก่อน
        if (item.BuyPrice > 0)
            return item.BuyPrice;

        // กำหนดราคาตาม tier และ type
        long basePrice = 0;

        switch (item.ItemType)
        {
            case ItemType.Weapon:
                basePrice = item.Tier == ItemTier.Common ? 150 : 350;
                break;
            case ItemType.Head:
            case ItemType.Armor:
            case ItemType.Pants:
            case ItemType.Shoes:
                basePrice = item.Tier == ItemTier.Common ? 100 : 250;
                break;
            case ItemType.Potion:
                basePrice = 25;
                break;
            case ItemType.Rune:
                basePrice = item.Tier == ItemTier.Common ? 200 : 500;
                break;
            case ItemType.Material:
                basePrice = 10;
                break;
            case ItemType.Misc:
                basePrice = 50;
                break;
            default:
                basePrice = 100;
                break;
        }

        return basePrice;
    }

    // ✅ สร้างไอเทมสำรองถ้าไม่มีใน database
    private void CreateFallbackItems()
    {
        Debug.Log("[ShopLooby] 🔧 Creating fallback shop items...");

        beginJourneyItems.Clear();

        // สร้าง ItemData พื้นฐานหรือใช้ testItems จาก Inventory
        var inventory = FindObjectOfType<Inventory>();
        
        Debug.Log($"[ShopLooby] Created {beginJourneyItems.Count} fallback items");
    }

    private void CreateBeginJourneyShopItems()
    {
        if (beginJourneyContainer == null || shopItemPrefab == null)
        {
            Debug.LogError("[ShopLooby] Missing container or prefab for shop items");
            return;
        }

        // ลบ shop items เก่า
        foreach (Transform child in beginJourneyContainer)
        {
            DestroyImmediate(child.gameObject);
        }

        // สร้าง shop items ใหม่
        foreach (var shopItemData in beginJourneyItems)
        {
            CreateShopItemUI(shopItemData);
        }

        Debug.Log($"[ShopLooby] Created {beginJourneyItems.Count} shop item UIs");
    }

    private void CreateShopItemUI(ShopItemData shopItemData)
    {
        GameObject shopItemObj = Instantiate(shopItemPrefab, beginJourneyContainer);
        ShopItemUI shopItemUI = shopItemObj.GetComponent<ShopItemUI>();

        if (shopItemUI == null)
        {
            shopItemUI = shopItemObj.AddComponent<ShopItemUI>();
        }

        shopItemUI.SetupShopItem(shopItemData, this);
    }
    #endregion

    #region Panel Management
    private void ShowBeginJourneyPanel()
    {
        SetActivePanel(beginJourneyPanel);
        Debug.Log("[ShopLooby] Showing Begin Journey panel");
    }

    private void ShowLegendShopPanel()
    {
        SetActivePanel(legendShopPanel);
        Debug.Log("[ShopLooby] Showing Legend Shop panel");
    }

    private void ShowSellPanel()
    {
        SetActivePanel(sellPanel);
        Debug.Log("[ShopLooby] Showing Sell panel");
    }

    private void SetActivePanel(GameObject targetPanel)
    {
        if (beginJourneyPanel != null) beginJourneyPanel.SetActive(false);
        if (legendShopPanel != null) legendShopPanel.SetActive(false);
        if (sellPanel != null) sellPanel.SetActive(false);

        if (targetPanel != null) targetPanel.SetActive(true);
    }

    void CloseShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
        Debug.Log("[ShopLooby] Shop closed");
    }
    #endregion

    #region Item Detail Panel
    public void ShowItemDetailPanel(ShopItemData shopItemData)
    {
        if (shopItemData == null || shopItemData.itemData == null)
        {
            Debug.LogWarning("[ShopLooby] Invalid shop item data");
            return;
        }

        selectedShopItem = shopItemData;
        ItemData itemData = shopItemData.itemData;

        // แสดง panel
        if (shopItemDetailPanel != null)
        {
            shopItemDetailPanel.SetActive(true);
        }

        // แสดงรูปไอเทม
        if (detailItemIcon != null)
        {
            detailItemIcon.sprite = itemData.ItemIcon;
            detailItemIcon.color = Color.white;
        }

        // แสดงชื่อไอเทม
        if (detailItemName != null)
        {
            detailItemName.text = itemData.ItemName;
            detailItemName.color = itemData.GetTierColor();
        }

        // แสดงประเภท
        if (detailItemType != null)
        {
            detailItemType.text = $"Type: {itemData.ItemType}";
        }

        // แสดง tier
        if (detailItemTier != null)
        {
            detailItemTier.text = itemData.GetTierText();
            detailItemTier.color = itemData.GetTierColor();
        }

        // แสดงคำอธิบาย
        if (detailItemDescription != null)
        {
            detailItemDescription.text = string.IsNullOrEmpty(itemData.Description) ?
                "" : itemData.Description;
        }

        // แสดงสถิติ
        if (detailItemStats != null)
        {
            string statsText = itemData.Stats.GetStatsDescription();
            detailItemStats.text = string.IsNullOrEmpty(statsText) ?
                "" : statsText;
        }

        // แสดงราคา
        if (detailItemPrice != null)
        {
            detailItemPrice.text = $"💰 {shopItemData.price:N0}";
        }

        // ตรวจสอบว่าซื้อได้หรือไม่
        UpdateBuyButton();

        Debug.Log($"[ShopLooby] Showing item detail: {itemData.ItemName} - Price: {shopItemData.price}");
    }

    private void UpdateBuyButton()
    {
        if (buyButton == null || currencyManager == null || selectedShopItem == null)
        {
            if (buyButton != null) buyButton.interactable = false;
            return;
        }

        long playerGold = currencyManager.GetCurrentGold();
        bool canAfford = playerGold >= selectedShopItem.price;

        buyButton.interactable = canAfford;

        // เปลี่ยนสีปุ่มตามสถานะ
        ColorBlock colors = buyButton.colors;
        colors.normalColor = canAfford ? Color.green : Color.red;
        buyButton.colors = colors;

        Debug.Log($"[ShopLooby] Buy button updated - Can afford: {canAfford} (Player: {playerGold}, Price: {selectedShopItem.price})");
    }

    public void HideItemDetailPanel()
    {
        if (shopItemDetailPanel != null)
        {
            shopItemDetailPanel.SetActive(false);
        }
        selectedShopItem = null;
        Debug.Log("[ShopLooby] Item detail panel hidden");
    }
    #endregion

    #region Purchase System
    private void OnBuyButtonClicked()
    {
        if (selectedShopItem == null)
        {
            Debug.LogWarning("[ShopLooby] No item selected for purchase");
            return;
        }

        // ✅ แทนที่จะซื้อทันที ให้เปิด Quantity Panel
        ShowPurchaseQuantityPanel();
    }

    // ✅ เพิ่ม Purchase Quantity System
    private void ShowPurchaseQuantityPanel()
    {
        if (selectedShopItem == null || purchaseQuantityPanel == null)
        {
            Debug.LogWarning("[ShopLooby] Cannot show quantity panel - missing item or panel");
            return;
        }

        // คำนวณจำนวนสูงสุดที่ซื้อได้
        CalculateMaxAffordableQuantity();

        // ซ่อน detail panel
        HideItemDetailPanel();

        // แสดง quantity panel
        purchaseQuantityPanel.SetActive(true);

        // Setup UI
        SetupQuantityPanelUI();

        Debug.Log($"[ShopLooby] Showing quantity panel for {selectedShopItem.itemData.ItemName}");
    }

    private void CalculateMaxAffordableQuantity()
    {
        if (currencyManager == null || selectedShopItem == null)
        {
            maxAffordableQuantity = 1;
            return;
        }

        long playerGold = currencyManager.GetCurrentGold();
        maxAffordableQuantity = (int)(playerGold / selectedShopItem.price);

        // จำกัดด้วย inventory space
        if (playerCharacter != null)
        {
            Inventory inventory = playerCharacter.GetInventory();
            if (inventory != null)
            {
                int freeSlots = inventory.FreeSlots;

                // ถ้าไอเทม stack ได้
                if (selectedShopItem.itemData.CanStack())
                {
                    // คำนวณจำนวนที่เพิ่มได้ใน inventory
                    int maxStackableQuantity = CalculateMaxStackableQuantity(inventory);
                    maxAffordableQuantity = Mathf.Min(maxAffordableQuantity, maxStackableQuantity);
                }
                else
                {
                    // ไอเทมไม่ stack ได้ จำกัดด้วยจำนวน free slots
                    maxAffordableQuantity = Mathf.Min(maxAffordableQuantity, freeSlots);
                }
            }
        }

        maxAffordableQuantity = Mathf.Max(1, maxAffordableQuantity); // อย่างน้อย 1
        Debug.Log($"[ShopLooby] Max affordable quantity: {maxAffordableQuantity}");
    }

    private int CalculateMaxStackableQuantity(Inventory inventory)
    {
        int totalCanAdd = 0;

        // หาช่องที่มีไอเทมเดียวกันแล้วยังไม่เต็ม
        for (int i = 0; i < inventory.CurrentSlots; i++)
        {
            InventoryItem slot = inventory.GetItem(i);
            if (slot != null && !slot.IsEmpty && slot.itemData == selectedShopItem.itemData)
            {
                int canAddToThisSlot = selectedShopItem.itemData.MaxStackSize - slot.stackCount;
                totalCanAdd += canAddToThisSlot;
            }
        }

        // เพิ่มช่องว่างที่สามารถใส่ไอเทมใหม่ได้
        int emptySlots = inventory.FreeSlots;
        totalCanAdd += emptySlots * selectedShopItem.itemData.MaxStackSize;

        return totalCanAdd;
    }

    private void SetupQuantityPanelUI()
    {
        ItemData itemData = selectedShopItem.itemData;

        // แสดงรูปและชื่อไอเทม
        if (quantityItemIcon != null)
        {
            quantityItemIcon.sprite = itemData.ItemIcon;
        }

        if (quantityItemName != null)
        {
            quantityItemName.text = itemData.ItemName;
            quantityItemName.color = itemData.GetTierColor();
        }

        if (quantityItemPrice != null)
        {
            quantityItemPrice.text = $"💰 {selectedShopItem.price:N0} each";
        }

        // ตั้งค่า Slider
        if (quantitySlider != null)
        {
            quantitySlider.minValue = 1;
            quantitySlider.maxValue = maxAffordableQuantity;
            quantitySlider.wholeNumbers = true;
            quantitySlider.value = 1;
        }

        // แสดงข้อมูลเพิ่มเติม
        if (maxAffordableText != null)
        {
            long playerGold = currencyManager?.GetCurrentGold() ?? 0;
            maxAffordableText.text = $"You have: 💰 {playerGold:N0}\nMax: {maxAffordableQuantity}";
        }

        // อัปเดตข้อมูลจำนวนและราคารวม
        selectedQuantity = 1;
        UpdateQuantityDisplay();
    }

    private void OnQuantitySliderChanged(float value)
    {
        selectedQuantity = (int)value;
        UpdateQuantityDisplay();
    }

    private void UpdateQuantityDisplay()
    {
        if (quantityValueText != null)
        {
            quantityValueText.text = selectedQuantity.ToString();
        }

        if (totalPriceText != null)
        {
            long totalPrice = selectedShopItem.price * selectedQuantity;
            totalPriceText.text = $"Total: 💰 {totalPrice:N0}";
        }

        // อัปเดตปุ่ม Confirm
        if (confirmPurchaseButton != null)
        {
            bool canAfford = currencyManager != null &&
                           currencyManager.GetCurrentGold() >= (selectedShopItem.price * selectedQuantity);

            confirmPurchaseButton.interactable = canAfford;

            // เปลี่ยนสีปุ่ม
            ColorBlock colors = confirmPurchaseButton.colors;
            colors.normalColor = canAfford ? Color.green : Color.red;
            confirmPurchaseButton.colors = colors;
        }
    }

    private void OnConfirmPurchaseClicked()
    {
        if (selectedShopItem == null || selectedQuantity <= 0)
        {
            Debug.LogWarning("[ShopLooby] Invalid purchase data");
            return;
        }

        // ตรวจสอบเงิน
        long totalPrice = selectedShopItem.price * selectedQuantity;

        if (currencyManager == null || !currencyManager.HasEnoughGold(totalPrice))
        {
            Debug.LogWarning($"[ShopLooby] Not enough gold. Need: {totalPrice}, Have: {currencyManager?.GetCurrentGold() ?? 0}");
            ShowMessage("Not enough gold!");
            return;
        }

        // ตรวจสอบ inventory
        if (playerCharacter == null)
        {
            Debug.LogError("[ShopLooby] Player character not found");
            return;
        }

        Inventory playerInventory = playerCharacter.GetInventory();
        if (playerInventory == null)
        {
            Debug.LogError("[ShopLooby] Player inventory not found");
            return;
        }

        // ซื้อไอเทม
        bool purchaseSuccess = ProcessQuantityPurchase(selectedShopItem, playerInventory, selectedQuantity);

        if (purchaseSuccess)
        {
            Debug.Log($"[ShopLooby] ✅ Successfully purchased: {selectedQuantity}x {selectedShopItem.itemData.ItemName}");
            ShowMessage($"Purchased {selectedQuantity}x {selectedShopItem.itemData.ItemName}!");
            HidePurchaseQuantityPanel();
        }
        else
        {
            Debug.LogError($"[ShopLooby] ❌ Failed to purchase: {selectedQuantity}x {selectedShopItem.itemData.ItemName}");
            ShowMessage("Purchase failed!");
        }
    }

    private bool ProcessQuantityPurchase(ShopItemData shopItem, Inventory inventory, int quantity)
    {
        try
        {
            long totalPrice = shopItem.price * quantity;

            // ตรวจสอบ inventory space สำหรับจำนวนที่ต้องการซื้อ
            if (!CanInventoryHoldItems(inventory, shopItem.itemData, quantity))
            {
                Debug.LogWarning("[ShopLooby] Not enough inventory space");
                ShowMessage("Not enough inventory space!");
                return false;
            }

            // หักเงิน
            bool spendSuccess = currencyManager.SpendGold(totalPrice);
            if (!spendSuccess)
            {
                Debug.LogError("[ShopLooby] Failed to spend gold");
                return false;
            }

            // เพิ่มไอเทมใน inventory
            bool addSuccess = inventory.AddItem(shopItem.itemData, quantity);
            if (!addSuccess)
            {
                Debug.LogError("[ShopLooby] Failed to add items to inventory");

                // คืนเงินถ้าเพิ่มไอเทมไม่สำเร็จ
                currencyManager.AddGold(totalPrice);
                return false;
            }

            Debug.Log($"[ShopLooby] Purchase completed - {quantity}x {shopItem.itemData.ItemName}, Total: {totalPrice}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ShopLooby] Purchase error: {e.Message}");
            return false;
        }
    }

    private bool CanInventoryHoldItems(Inventory inventory, ItemData itemData, int quantity)
    {
        if (itemData.CanStack())
        {
            // คำนวณว่า inventory สามารถเก็บจำนวนนี้ได้หรือไม่
            int canHold = CalculateMaxStackableQuantity(inventory);
            return canHold >= quantity;
        }
        else
        {
            // ไอเทมไม่ stack ได้ ต้องมี free slots เพียงพอ
            return inventory.FreeSlots >= quantity;
        }
    }

    private void OnCancelPurchaseClicked()
    {
        HidePurchaseQuantityPanel();
        Debug.Log("[ShopLooby] Purchase cancelled");
    }

    public void HidePurchaseQuantityPanel()
    {
        if (purchaseQuantityPanel != null)
        {
            purchaseQuantityPanel.SetActive(false);
        }
        selectedQuantity = 1;
        Debug.Log("[ShopLooby] Purchase quantity panel hidden");
    }

    // ✅ เก็บ method เก่าไว้สำหรับ backward compatibility
    private bool ProcessPurchase(ShopItemData shopItem, Inventory inventory)
    {
        return ProcessQuantityPurchase(shopItem, inventory, 1);
    }

    private void ShowMessage(string message)
    {
        Debug.Log($"[ShopLooby] 💬 {message}");
        // TODO: แสดง popup message ใน UI (อาจจะทำในอนาคต)
    }
    #endregion

    #region Public Methods
    public void RefreshShop()
    {
        SetupShopData();
        UpdateBuyButton();
        Debug.Log("[ShopLooby] Shop refreshed");
    }

    public void RefreshCurrency()
    {
        UpdateBuyButton();
        Debug.Log("[ShopLooby] Currency refreshed");
    }
    #endregion
}

// ✅ Shop Item Data Class
[System.Serializable]
public class ShopItemData
{
    public ItemData itemData;
    public long price;
    public bool isAvailable = true;
    public int stockLimit = -1; // -1 = unlimited

    public ShopItemData(ItemData item, long itemPrice)
    {
        itemData = item;
        price = itemPrice;
        isAvailable = true;
        stockLimit = -1;
    }

    public ShopItemData(ItemData item, long itemPrice, int stock)
    {
        itemData = item;
        price = itemPrice;
        isAvailable = true;
        stockLimit = stock;
    }

    public bool IsValid()
    {
        return itemData != null && price > 0 && isAvailable;
    }
}

// ✅ Shop Item UI Component
