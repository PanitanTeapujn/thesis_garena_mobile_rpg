using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;

// ✅ ย้าย enum มาไว้นอก class
public enum SortOption
{
    NameAZ,
    NameZA,
    PriceLowHigh,
    PriceHighLow,
    TierLowHigh,
    TierHighLow
}

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

    [Header("🆕 Phase 3: Enhanced Shop Features")]
    public Button refreshShopButton;
    public TMP_InputField searchInputField;
    public TMP_Dropdown categoryFilter;
    public TMP_Dropdown tierFilter;
    public TextMeshProUGUI itemCountText;
    public Button clearFiltersButton;

    [Header("🆕 Pagination")]
    public int itemsPerPage = 12;
    public Button previousPageButton;
    public Button nextPageButton;
    public TextMeshProUGUI pageInfoText;
    [Header("💎 Currency Display")]
    public TextMeshProUGUI currentGoldText;    // แสดงเงินปัจจุบัน
    public TextMeshProUGUI currentGemsText;    // แสดงเพชรปัจจุบัน
    [Header("🆕 Sorting")]
    public TMP_Dropdown sortDropdown;

    [Header("Shop Data")]
    public List<ShopItemData> beginJourneyItems = new List<ShopItemData>();
    private ShopItemData selectedShopItem;
    private CurrencyManager currencyManager;
    private Character playerCharacter;
    // ✅ Phase 3: Enhanced shop variables
    private List<ShopItemData> allShopItems = new List<ShopItemData>();
    private List<ShopItemData> filteredItems = new List<ShopItemData>();
    private int currentPage = 0;
    private string currentSearchText = "";
    private SortOption currentSortOption = SortOption.NameAZ;

    // ✅ เพิ่มตัวแปรสำหรับ Quantity System
    private int selectedQuantity = 1;
    private int maxAffordableQuantity = 1;

    #region Unity Lifecycle
    void Start()
    {
        UpdateCurrencyDisplay();

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

        // ✅ หา Player Character หลายวิธี
        FindPlayerCharacter();
        // ตั้งค่า UI เริ่มต้น
        ShowBeginJourneyPanel();
        HideItemDetailPanel();
        HidePurchaseQuantityPanel(); // ✅ เพิ่มบรรทัดนี้
    }
    public void UpdateCurrencyDisplay()
    {
        try
        {
            var currencyManager = CurrencyManager.FindCurrencyManager();

            if (currencyManager != null)
            {
                if (currentGoldText != null)
                {
                    currentGoldText.text = $"{currencyManager.GetCurrentGold():N0}";
                }

                if (currentGemsText != null)
                {
                    currentGemsText.text = $"{currencyManager.GetCurrentGems():N0}";
                }
            }
            else
            {
                if (currentGoldText != null)
                {
                    currentGoldText.text = "";
                }

                if (currentGemsText != null)
                {
                    currentGemsText.text = "";
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UpgradeLobby] ❌ Error updating currency display: {e.Message}");
        }
    }
    // ✅ เพิ่ม method ใหม่สำหรับหา Player Character
    private void FindPlayerCharacter()
    {
        // วิธีที่ 1: หาจาก tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerCharacter = playerObj.GetComponent<Character>();
            if (playerCharacter != null)
            {
                Debug.Log($"[ShopLooby] ✅ Found player character via tag: {playerCharacter.CharacterName}");
                return;
            }
        }

        // วิธีที่ 2: หาจาก HasInputAuthority (สำหรับ Multiplayer)
        Character[] allCharacters = FindObjectsOfType<Character>();
        foreach (Character character in allCharacters)
        {
            if (character.HasInputAuthority)
            {
                playerCharacter = character;
                Debug.Log($"[ShopLooby] ✅ Found player character via InputAuthority: {playerCharacter.CharacterName}");
                return;
            }
        }

        // วิธีที่ 3: หาจากชื่อ GameObject
        playerObj = GameObject.Find("LocalHero");
        if (playerObj == null) playerObj = GameObject.Find("Player");
        if (playerObj == null) playerObj = GameObject.Find("Character");

        if (playerObj != null)
        {
            playerCharacter = playerObj.GetComponent<Character>();
            if (playerCharacter != null)
            {
                Debug.Log($"[ShopLooby] ✅ Found player character via name: {playerCharacter.CharacterName}");
                return;
            }
        }

        // วิธีที่ 4: เอาตัวแรกที่เจอ (fallback)
        if (allCharacters.Length > 0)
        {
            playerCharacter = allCharacters[0];
            Debug.Log($"[ShopLooby] ⚠️ Using first character found: {playerCharacter.CharacterName}");
            return;
        }

        Debug.LogWarning("[ShopLooby] ❌ Player Character not found with any method!");
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
        if (confirmPurchaseButton != null)
            confirmPurchaseButton.onClick.AddListener(OnConfirmPurchaseClicked);
        if (cancelPurchaseButton != null)
            cancelPurchaseButton.onClick.AddListener(OnCancelPurchaseClicked);
        if (quantitySlider != null)
            quantitySlider.onValueChanged.AddListener(OnQuantitySliderChanged);

        // ✅ Phase 3: Enhanced shop listeners
        if (refreshShopButton != null)
            refreshShopButton.onClick.AddListener(RefreshShopItems);
        if (searchInputField != null)
            searchInputField.onValueChanged.AddListener(OnSearchTextChanged);
        if (categoryFilter != null)
            categoryFilter.onValueChanged.AddListener(OnCategoryFilterChanged);
        if (tierFilter != null)
            tierFilter.onValueChanged.AddListener(OnTierFilterChanged);
        if (sortDropdown != null)
            sortDropdown.onValueChanged.AddListener(OnSortOptionChanged);
        if (clearFiltersButton != null)
            clearFiltersButton.onClick.AddListener(ClearAllFilters);
        if (previousPageButton != null)
            previousPageButton.onClick.AddListener(PreviousPage);
        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(NextPage);
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

        // ✅ Phase 3: Setup enhanced shop features
        SetupFiltersAndSorting();

        // ถ้าไม่มีข้อมูลใน Inspector ให้สร้างจาก ItemDatabase
        if (beginJourneyItems.Count == 0)
        {
            LoadBeginJourneyItemsFromDatabase();
        }

        // ✅ Phase 3: Copy to allShopItems และ apply filters
        allShopItems = new List<ShopItemData>(beginJourneyItems);
        ApplyFiltersAndPagination();
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
        gridLayout.spacing = new Vector2(10, 5);
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.UpperLeft;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 8; // 3 คอลัมน์

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
        beginJourneyItems.Clear();

        var allItems = database.GetAllItems();
        Debug.Log($"[ShopLooby] Found {allItems.Count} total items in database");

        foreach (var item in allItems)
        {
            if (item == null)
            {
                Debug.LogWarning("[ShopLooby] Found null item in database!");
                continue;
            }

            // กรองเฉพาะไอเทมที่เหมาะสำหรับ Begin Journey
            if (IsBeginJourneyItem(item))
            {
                // ✅ ใช้ Constructor ใหม่ที่อ่านข้อมูลจาก ItemData
                ShopItemData newShopItem = new ShopItemData(item);

                if (newShopItem.itemData == null)
                {
                    Debug.LogError($"[ShopLooby] ShopItemData creation failed for item: {item.ItemName}");
                    continue;
                }

                beginJourneyItems.Add(newShopItem);

                string currencyType = newShopItem.currencyType == CurrencyType.Gold ? "Gold" : "Gems";
                Debug.Log($"[ShopLooby] ✅ Added {item.ItemName} ({item.ItemType}) - Price: {newShopItem.price} {currencyType}");
            }
        }

        Debug.Log($"[ShopLooby] ✅ Loaded {beginJourneyItems.Count} items for Begin Journey shop");
    }
    private CurrencyType DetermineItemCurrency(ItemData item)
    {
        // Legend และ Epic ใช้ Gem
        if (item.Tier == ItemTier.Legendary || item.Tier == ItemTier.Epic)
            return CurrencyType.Gems;

        // Rare tier บางอย่างใช้ Gem (weapon, armor)
        if (item.Tier == ItemTier.Rare &&
            (item.ItemType == ItemType.Weapon || item.ItemType == ItemType.Armor))
            return CurrencyType.Gems;

        // อื่นๆ ใช้ Gold
        return CurrencyType.Gold;
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
        CurrencyType currency = DetermineItemCurrency(item);

        if (currency == CurrencyType.Gems)
        {
            // ราคา Gem (ถูกกว่า Gold)
            switch (item.ItemType)
            {
                case ItemType.Weapon:
                    basePrice = item.Tier == ItemTier.Rare ? 15 :
                               item.Tier == ItemTier.Epic ? 35 : 75; // Legendary
                    break;
                case ItemType.Head:
                case ItemType.Armor:
                case ItemType.Pants:
                case ItemType.Shoes:
                    basePrice = item.Tier == ItemTier.Rare ? 10 :
                               item.Tier == ItemTier.Epic ? 25 : 50; // Legendary
                    break;
                case ItemType.Rune:
                    basePrice = item.Tier == ItemTier.Epic ? 20 : 40; // Legendary
                    break;
                default:
                    basePrice = 5;
                    break;
            }
        }
        else
        {
            // ราคา Gold (เหมือนเดิม)
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
        }

        return basePrice;
    }

    // ✅ สร้างไอเทมสำรองถ้าไม่มีใน database


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
        // ✅ เพิ่มการตรวจสอบก่อนสร้าง UI
        if (shopItemData == null)
        {
            Debug.LogError("[ShopLooby] shopItemData is null, cannot create UI!");
            return;
        }

        if (shopItemData.itemData == null)
        {
            Debug.LogError("[ShopLooby] shopItemData.itemData is null, cannot create UI!");
            return;
        }

        Debug.Log($"[ShopLooby] 🔨 Creating shop item UI for: {shopItemData.itemData.ItemName}");

        GameObject shopItemObj = Instantiate(shopItemPrefab, beginJourneyContainer);
        ShopItemUI shopItemUI = shopItemObj.GetComponent<ShopItemUI>();

        if (shopItemUI == null)
        {
            shopItemUI = shopItemObj.AddComponent<ShopItemUI>();
        }

        // ✅ ตรวจสอบอีกครั้งก่อนส่งไป setup
        if (shopItemData.itemData == null)
        {
            Debug.LogError($"[ShopLooby] ItemData became null during UI creation!");
            Destroy(shopItemObj);
            return;
        }

        shopItemUI.SetupShopItem(shopItemData, this);
        Debug.Log($"[ShopLooby] ✅ Shop item UI created successfully for: {shopItemData.itemData.ItemName}");
    }
    #endregion

    #region Panel Management
    private void ShowBeginJourneyPanel()
    {
        SetActivePanel(beginJourneyPanel);
        // ✅ ล้างข้อมูลเมื่อเปลี่ยน tab
        ClearSelectedItem();
        Debug.Log("[ShopLooby] Showing Begin Journey panel");
    }

    private void ShowLegendShopPanel()
    {
        SetActivePanel(legendShopPanel);
        // ✅ ล้างข้อมูลเมื่อเปลี่ยน tab
        ClearSelectedItem();
        Debug.Log("[ShopLooby] Showing Legend Shop panel");
    }

    private void ShowSellPanel()
    {
        SetActivePanel(sellPanel);
        // ✅ ล้างข้อมูลเมื่อเปลี่ยน tab
        ClearSelectedItem();
        Debug.Log("[ShopLooby] Showing Sell panel");
    }

    // ✅ เพิ่ม helper method
    private void ClearSelectedItem()
    {
        selectedShopItem = null;
        selectedQuantity = 1;

        // ปิด panels ที่เปิดอยู่
        if (shopItemDetailPanel != null && shopItemDetailPanel.activeSelf)
        {
            shopItemDetailPanel.SetActive(false);
        }

        if (purchaseQuantityPanel != null && purchaseQuantityPanel.activeSelf)
        {
            purchaseQuantityPanel.SetActive(false);
        }
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
        if (shopItemData == null)
        {
            Debug.LogError("[ShopLooby] shopItemData is null!");
            return;
        }

        if (shopItemData.itemData == null)
        {
            Debug.LogError("[ShopLooby] shopItemData.itemData is null!");
            return;
        }

        // ✅ เก็บ reference และ debug
        selectedShopItem = shopItemData;
        Debug.Log($"[ShopLooby] 📋 Selected shop item: {selectedShopItem.itemData.ItemName}, Price: {selectedShopItem.price}");
        Debug.Log($"[ShopLooby] 🔍 ItemData null check: {(selectedShopItem.itemData == null ? "NULL" : "OK")}");

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
            string currencyText = shopItemData.currencyType == CurrencyType.Gold ? "Gold" : "Gems";
            detailItemPrice.text = $"{shopItemData.price:N0} {currencyText}";
        }


        // ตรวจสอบว่าซื้อได้หรือไม่
        UpdateBuyButton();

        Debug.Log($"[ShopLooby] ✅ Item detail shown successfully for: {itemData.ItemName}");
        Debug.Log($"[ShopLooby] 🔍 Buy button reference: {(buyButton != null ? "OK" : "NULL")}");
        Debug.Log($"[ShopLooby] 🔍 Buy button interactable: {(buyButton != null ? buyButton.interactable.ToString() : "NULL")}");
    }

    private void UpdateBuyButton()
    {
        if (buyButton == null || selectedShopItem == null)
        {
            if (buyButton != null) buyButton.interactable = false;
            return;
        }

        if (currencyManager == null)
        {
            Debug.LogError("[ShopLooby] CurrencyManager is null! Trying to find again...");
            currencyManager = CurrencyManager.FindCurrencyManager();

            if (currencyManager == null)
            {
                buyButton.interactable = false;
                return;
            }
        }

        // ตรวจสอบเงินตามสกุลเงินที่ใช้
        bool canAfford = false;
        long playerCurrency = 0;

        if (selectedShopItem.currencyType == CurrencyType.Gold)
        {
            playerCurrency = currencyManager.GetCurrentGold();
            canAfford = playerCurrency >= selectedShopItem.price;
        }
        else // Gems
        {
            playerCurrency = currencyManager.GetCurrentGems();
            canAfford = playerCurrency >= selectedShopItem.price;
        }

        Debug.Log($"[ShopLooby] 💰 Buy button check - Player {selectedShopItem.currencyType}: {playerCurrency}, Item price: {selectedShopItem.price}, Can afford: {canAfford}");

        buyButton.interactable = true;

        // เปลี่ยนสีปุ่มตามสถานะ
        ColorBlock colors = buyButton.colors;
        colors.normalColor = canAfford ? Color.green : Color.yellow;
        colors.highlightedColor = canAfford ? Color.white : Color.blue;
        buyButton.colors = colors;

        // ✅ เปลี่ยนข้อความในปุ่มเป็นข้อความ Gold/Gems
        TextMeshProUGUI buyButtonText = buyButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buyButtonText != null)
        {
            if (canAfford)
            {
                buyButtonText.text = "BUY";
            }
            else
            {
                string currencyName = selectedShopItem.currencyType == CurrencyType.Gold ? "GOLD" : "GEMS";
                buyButtonText.text = $"NOT ENOUGH {currencyName}";
            }
        }
    }


    public void HideItemDetailPanel()
    {
        if (shopItemDetailPanel != null)
        {
            shopItemDetailPanel.SetActive(false);
        }
        // ✅ ไม่ล้าง selectedShopItem ตรงนี้เพื่อให้ Quantity Panel ใช้ได้
        // selectedShopItem = null; // ← ลบบรรทัดนี้ออก
        Debug.Log("[ShopLooby] Item detail panel hidden");
    }
    #endregion

    #region Purchase System
    private void OnBuyButtonClicked()
    {
        Debug.Log("[ShopLooby] 🖱️ Buy button clicked!");

        if (selectedShopItem == null)
        {
            ShowMessage("No item selected!");
            return;
        }

        if (currencyManager == null)
        {
            ShowMessage("Error: Currency system not found!");
            return;
        }

        // ตรวจสอบเงินตามสกุลเงินที่ใช้
        bool hasEnoughCurrency = false;
        long playerCurrency = 0;

        if (selectedShopItem.currencyType == CurrencyType.Gold)
        {
            playerCurrency = currencyManager.GetCurrentGold();
            hasEnoughCurrency = playerCurrency >= selectedShopItem.price;
        }
        else // Gems
        {
            playerCurrency = currencyManager.GetCurrentGems();
            hasEnoughCurrency = playerCurrency >= selectedShopItem.price;
        }

        if (!hasEnoughCurrency)
        {
            // ✅ แสดงข้อความ error เป็นข้อความ Gold/Gems
            string currencyName = selectedShopItem.currencyType == CurrencyType.Gold ? "gold" : "gems";
            Debug.LogWarning($"[ShopLooby] Not enough {currencyName}. Need: {selectedShopItem.price}, Have: {playerCurrency}");
            ShowMessage($"Not enough {currencyName}! Need {selectedShopItem.price:N0}, have {playerCurrency:N0}");
            return;
        }

        Debug.Log($"[ShopLooby] 🛒 Attempting to buy: {selectedShopItem.itemData.ItemName}");

        // เปิด Quantity Panel
        ShowPurchaseQuantityPanel();
    }
    // ✅ เพิ่ม Purchase Quantity System
    private void ShowPurchaseQuantityPanel()
    {
        if (selectedShopItem == null)
        {
            Debug.LogError("[ShopLooby] Cannot show quantity panel - selectedShopItem is null!");
            return;
        }

        if (selectedShopItem.itemData == null)
        {
            Debug.LogError("[ShopLooby] Cannot show quantity panel - selectedShopItem.itemData is null!");
            return;
        }

        if (purchaseQuantityPanel == null)
        {
            Debug.LogError("[ShopLooby] Cannot show quantity panel - purchaseQuantityPanel is null! Please assign it in Inspector");
            return;
        }

        Debug.Log($"[ShopLooby] 📋 Showing quantity panel for: {selectedShopItem.itemData.ItemName}");

        // คำนวณจำนวนสูงสุดที่ซื้อได้
        CalculateMaxAffordableQuantity();

        // ตรวจสอบว่าซื้อได้หรือไม่
        if (maxAffordableQuantity <= 0)
        {
            Debug.LogWarning("[ShopLooby] Cannot afford any items or no inventory space");
            ShowMessage("Cannot afford any items or no inventory space!");
            return;
        }

        // ซ่อน detail panel
        HideItemDetailPanel();

        // แสดง quantity panel
        purchaseQuantityPanel.SetActive(true);

        // Setup UI
        SetupQuantityPanelUI();

        Debug.Log($"[ShopLooby] ✅ Quantity panel shown successfully");
    }

    private void CalculateMaxAffordableQuantity()
    {
        if (currencyManager == null || selectedShopItem == null)
        {
            maxAffordableQuantity = 1;
            return;
        }

        // ✅ คำนวณจำนวนที่ซื้อได้ตามสกุลเงิน
        long playerCurrency = 0;
        if (selectedShopItem.currencyType == CurrencyType.Gold)
        {
            playerCurrency = currencyManager.GetCurrentGold();
        }
        else // Gems
        {
            playerCurrency = currencyManager.GetCurrentGems();
        }

        maxAffordableQuantity = (int)(playerCurrency / selectedShopItem.price);

        // จำกัดด้วย inventory space (เหมือนเดิม)
        if (playerCharacter != null)
        {
            Inventory inventory = playerCharacter.GetInventory();
            if (inventory != null)
            {
                int freeSlots = inventory.FreeSlots;

                if (selectedShopItem.itemData.CanStack())
                {
                    int maxStackableQuantity = CalculateMaxStackableQuantity(inventory);
                    maxAffordableQuantity = Mathf.Min(maxAffordableQuantity, maxStackableQuantity);
                }
                else
                {
                    maxAffordableQuantity = Mathf.Min(maxAffordableQuantity, freeSlots);
                }
            }
        }

        maxAffordableQuantity = Mathf.Max(1, maxAffordableQuantity);
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
        if (selectedShopItem?.itemData == null)
        {
            Debug.LogError("[ShopLooby] Cannot setup quantity panel UI - invalid item data");
            return;
        }

        ItemData itemData = selectedShopItem.itemData;
        Debug.Log($"[ShopLooby] 🎨 Setting up quantity panel UI for: {itemData.ItemName}");

        // แสดงรูปและชื่อไอเทม (เหมือนเดิม)
        if (quantityItemIcon != null)
        {
            quantityItemIcon.sprite = itemData.ItemIcon;
        }

        if (quantityItemName != null)
        {
            quantityItemName.text = itemData.ItemName;
            quantityItemName.color = itemData.GetTierColor();
        }

        // ✅ แสดงราคาพร้อมสกุลเงิน
        if (quantityItemPrice != null)
        {
            string currencyText = selectedShopItem.currencyType == CurrencyType.Gold ? "Gold" : "Gems";
            quantityItemPrice.text = $"{selectedShopItem.price:N0} {currencyText} each";
            Debug.Log($"[ShopLooby] ✅ Set quantity item price: {selectedShopItem.price} {currencyText}");
        }

        // ตั้งค่า Slider (เหมือนเดิม)
        if (quantitySlider != null)
        {
            quantitySlider.minValue = 1;
            quantitySlider.maxValue = maxAffordableQuantity;
            quantitySlider.wholeNumbers = true;
            quantitySlider.value = 1;
        }

        // ✅ แสดงข้อมูลเพิ่มเติมตามสกุลเงิน
        if (maxAffordableText != null)
        {
            long playerCurrency = 0;
            string currencyText = "";

            if (selectedShopItem.currencyType == CurrencyType.Gold)
            {
                playerCurrency = currencyManager?.GetCurrentGold() ?? 0;
                currencyText = "Gold";
            }
            else
            {
                playerCurrency = currencyManager?.GetCurrentGems() ?? 0;
                currencyText = "Gems";
            }

            maxAffordableText.text = $"You have: {playerCurrency:N0} {currencyText}\nMax: {maxAffordableQuantity}";
            Debug.Log($"[ShopLooby] ✅ Set max affordable text: {playerCurrency} {currencyText}, max {maxAffordableQuantity}");
        }

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
        if (selectedShopItem == null)
        {
            Debug.LogError("[ShopLooby] Cannot update quantity display - selectedShopItem is null!");
            return;
        }

        if (quantityValueText != null)
        {
            quantityValueText.text = selectedQuantity.ToString();
        }

        if (totalPriceText != null)
        {
            long totalPrice = selectedShopItem.price * selectedQuantity;
            string currencyText = selectedShopItem.currencyType == CurrencyType.Gold ? "Gold" : "Gems";
            totalPriceText.text = $"Total: {totalPrice:N0} {currencyText}";
            Debug.Log($"[ShopLooby] ✅ Updated total price text to: {totalPrice} {currencyText}");
        }

        // อัปเดตปุ่ม Confirm
        if (confirmPurchaseButton != null)
        {
            long totalPrice = selectedShopItem.price * selectedQuantity;
            bool canAfford = false;

            if (selectedShopItem.currencyType == CurrencyType.Gold)
            {
                canAfford = currencyManager != null && currencyManager.GetCurrentGold() >= totalPrice;
            }
            else
            {
                canAfford = currencyManager != null && currencyManager.GetCurrentGems() >= totalPrice;
            }

            confirmPurchaseButton.interactable = canAfford;

            ColorBlock colors = confirmPurchaseButton.colors;
            colors.normalColor = canAfford ? Color.green : Color.red;
            confirmPurchaseButton.colors = colors;
        }
    }

    private void OnConfirmPurchaseClicked()
    {
        if (selectedShopItem == null || selectedShopItem.itemData == null || selectedQuantity <= 0)
        {
            ShowMessage("Error: Invalid purchase data!");
            return;
        }

        if (currencyManager == null)
        {
            ShowMessage("Error: Currency system not found!");
            return;
        }

        // ✅ ตรวจสอบเงินตามสกุลเงิน
        long totalPrice = selectedShopItem.price * selectedQuantity;
        bool hasEnoughCurrency = false;

        if (selectedShopItem.currencyType == CurrencyType.Gold)
        {
            hasEnoughCurrency = currencyManager.HasEnoughGold(totalPrice);
        }
        else // Gems
        {
            hasEnoughCurrency = currencyManager.HasEnoughGems((int)totalPrice);
        }

        if (!hasEnoughCurrency)
        {
            string currencyName = selectedShopItem.currencyType == CurrencyType.Gold ? "gold" : "gems";
            ShowMessage($"Not enough {currencyName}!");
            return;
        }

        // ตรวจสอบ Character และ Inventory (เหมือนเดิม)
        if (playerCharacter == null)
        {
            FindPlayerCharacter();
            if (playerCharacter == null)
            {
                ShowMessage("Error: Player character not found!");
                return;
            }
        }

        Inventory playerInventory = playerCharacter.GetInventory();
        if (playerInventory == null)
        {
            playerInventory = playerCharacter.GetComponent<Inventory>();
            if (playerInventory == null)
            {
                playerInventory = playerCharacter.GetComponentInChildren<Inventory>();
            }

            if (playerInventory == null)
            {
                ShowMessage("Error: Player inventory not found!");
                return;
            }
        }

        // ซื้อไอเทม
        bool purchaseSuccess = ProcessQuantityPurchase(selectedShopItem, playerInventory, selectedQuantity);

        if (purchaseSuccess)
        {
            string currencyName = selectedShopItem.currencyType == CurrencyType.Gold ? "gold" : "gems";
            Debug.Log($"[ShopLooby] ✅ Successfully purchased: {selectedQuantity}x {selectedShopItem.itemData.ItemName} for {totalPrice} {currencyName}");
            ShowMessage($"Purchased {selectedQuantity}x {selectedShopItem.itemData.ItemName}!");
            HidePurchaseQuantityPanel();
        }
        else
        {
            ShowMessage("Purchase failed!");
        }
        UpdateCurrencyDisplay();
    }
    private bool ProcessQuantityPurchase(ShopItemData shopItem, Inventory inventory, int quantity)
    {
        try
        {
            long totalPrice = shopItem.price * quantity;

            // ตรวจสอบ inventory space
            if (!CanInventoryHoldItems(inventory, shopItem.itemData, quantity))
            {
                Debug.LogWarning("[ShopLooby] Not enough inventory space");
                ShowMessage("Not enough inventory space!");
                return false;
            }

            // ✅ หักเงินตามสกุลเงิน
            bool spendSuccess = false;
            if (shopItem.currencyType == CurrencyType.Gold)
            {
                spendSuccess = currencyManager.SpendGold(totalPrice);
            }
            else // Gems
            {
                spendSuccess = currencyManager.SpendGems((int)totalPrice);
            }

            if (!spendSuccess)
            {
                Debug.LogError("[ShopLooby] Failed to spend currency");
                return false;
            }

            // เพิ่มไอเทมใน inventory
            bool addSuccess = inventory.AddItem(shopItem.itemData, quantity);
            if (!addSuccess)
            {
                Debug.LogError("[ShopLooby] Failed to add items to inventory");

                // ✅ คืนเงินถ้าเพิ่มไอเทมไม่สำเร็จ
                if (shopItem.currencyType == CurrencyType.Gold)
                {
                    currencyManager.AddGold(totalPrice);
                }
                else
                {
                    currencyManager.AddGems((int)totalPrice);
                }
                return false;
            }

            string currencyName = shopItem.currencyType == CurrencyType.Gold ? "gold" : "gems";
            Debug.Log($"[ShopLooby] Purchase completed - {quantity}x {shopItem.itemData.ItemName}, Total: {totalPrice} {currencyName}");
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
        // ✅ ล้าง selectedShopItem ตรงนี้แทน เมื่อปิด quantity panel เรียบร้อยแล้ว
        selectedShopItem = null;
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
    // ✅ เพิ่ม method สำหรับ refresh character reference
    public void RefreshCharacterReference()
    {
        FindPlayerCharacter();
        if (playerCharacter != null)
        {
            Debug.Log($"[ShopLooby] ✅ Character reference refreshed: {playerCharacter.CharacterName}");
        }
    }

    // ✅ เพิ่ม method สำหรับ debug
    [ContextMenu("Debug Character & Inventory")]
    private void DebugCharacterAndInventory()
    {
        Debug.Log("=== CHARACTER & INVENTORY DEBUG ===");

        Character[] allCharacters = FindObjectsOfType<Character>();
        Debug.Log($"Total Characters in scene: {allCharacters.Length}");

        for (int i = 0; i < allCharacters.Length; i++)
        {
            Character character = allCharacters[i];
            Inventory inventory = character.GetInventory();
            Debug.Log($"Character {i}: {character.CharacterName}");
            Debug.Log($"  - GameObject: {character.gameObject.name}");
            Debug.Log($"  - HasInputAuthority: {character.HasInputAuthority}");
            Debug.Log($"  - Inventory: {(inventory != null ? "Found" : "NULL")}");
            Debug.Log($"  - Tag: {character.gameObject.tag}");
        }

        Debug.Log($"Current playerCharacter: {(playerCharacter?.CharacterName ?? "NULL")}");
        Debug.Log($"Current playerInventory: {(playerCharacter?.GetInventory() != null ? "Found" : "NULL")}");
    }
    #region Phase 3: Enhanced Shop Features

    private void SetupFiltersAndSorting()
    {
        // Setup category filter dropdown
        if (categoryFilter != null)
        {
            categoryFilter.ClearOptions();
            List<string> categoryOptions = new List<string> { "All" };
            foreach (ItemType itemType in System.Enum.GetValues(typeof(ItemType)))
            {
                categoryOptions.Add(itemType.ToString());
            }
            categoryFilter.AddOptions(categoryOptions);
            categoryFilter.value = 0; // All
        }

        // ✅ **FIX: Setup tier filter dropdown - แก้ไขการสร้าง dropdown options**
        if (tierFilter != null)
        {
            tierFilter.ClearOptions();
            List<string> tierOptions = new List<string> { "All" }; // index 0 = All

            // เพิ่ม tier options โดยเรียงตาม enum values
            foreach (ItemTier tier in System.Enum.GetValues(typeof(ItemTier)))
            {
                tierOptions.Add(tier.ToString());
            }
            // ตอนนี้ dropdown จะมี: All(0), Common(1), Uncommon(2), Rare(3), Epic(4), Legendary(5)
            // ซึ่งตรงกับ enum values: Common=1, Uncommon=2, Rare=3, Epic=4, Legendary=5

            tierFilter.AddOptions(tierOptions);
            tierFilter.value = 0; // All

            Debug.Log($"[ShopLooby] Tier filter setup completed. Options: {string.Join(", ", tierOptions)}");
        }

        // Setup sort dropdown
        if (sortDropdown != null)
        {
            sortDropdown.ClearOptions();
            List<string> sortOptions = new List<string>
        {
            "Name A-Z", "Name Z-A",
            "Price Low-High", "Price High-Low",
            "Tier Low-High", "Tier High-Low"
        };
            sortDropdown.AddOptions(sortOptions);
            sortDropdown.value = 0; // Name A-Z
        }
    }

    private void ApplyFiltersAndPagination()
    {
        // Start with all items
        filteredItems = new List<ShopItemData>(allShopItems);

        // Apply search filter
        if (!string.IsNullOrEmpty(currentSearchText))
        {
            filteredItems = filteredItems.Where(item =>
                item.itemData.ItemName.ToLower().Contains(currentSearchText.ToLower())).ToList();
        }

        // Apply category filter
        if (categoryFilter != null && categoryFilter.value > 0)
        {
            ItemType selectedType = (ItemType)(categoryFilter.value - 1);
            filteredItems = filteredItems.Where(item => item.itemData.ItemType == selectedType).ToList();
        }

        // ✅ **FIX: Apply tier filter - แก้ไขการแปลงค่า enum**
        if (tierFilter != null && tierFilter.value > 0)
        {
            // เนื่องจาก ItemTier เริ่มต้นที่ 1 (Common=1, Uncommon=2, ...)
            // และ dropdown เริ่มต้นที่ 0 (All=0, Common=1, Uncommon=2, ...)
            // เราต้องแปลงค่า dropdown ให้ตรงกับ enum
            ItemTier selectedTier = (ItemTier)tierFilter.value; // ไม่ต้อง -1 เพราะ enum เริ่มต้นที่ 1

            filteredItems = filteredItems.Where(item => item.itemData.Tier == selectedTier).ToList();

            Debug.Log($"[ShopLooby] Tier filter applied: {selectedTier} (dropdown value: {tierFilter.value})");
            Debug.Log($"[ShopLooby] Filtered items count: {filteredItems.Count}");
        }

        // Apply sorting
        ApplySorting();

        // Update pagination
        UpdatePagination();

        // Update display
        DisplayCurrentPage();
    }
    private void ApplySorting()
    {
        switch (currentSortOption)
        {
            case SortOption.NameAZ:
                filteredItems = filteredItems.OrderBy(item => item.itemData.ItemName).ToList();
                break;
            case SortOption.NameZA:
                filteredItems = filteredItems.OrderByDescending(item => item.itemData.ItemName).ToList();
                break;
            case SortOption.PriceLowHigh:
                filteredItems = filteredItems.OrderBy(item => item.price).ToList();
                break;
            case SortOption.PriceHighLow:
                filteredItems = filteredItems.OrderByDescending(item => item.price).ToList();
                break;
            case SortOption.TierLowHigh:
                filteredItems = filteredItems.OrderBy(item => (int)item.itemData.Tier).ToList();
                break;
            case SortOption.TierHighLow:
                filteredItems = filteredItems.OrderByDescending(item => (int)item.itemData.Tier).ToList();
                break;
        }
    }

    private void UpdatePagination()
    {
        int totalPages = Mathf.CeilToInt((float)filteredItems.Count / itemsPerPage);
        currentPage = Mathf.Clamp(currentPage, 0, Mathf.Max(0, totalPages - 1));

        // Update pagination buttons
        if (previousPageButton != null)
            previousPageButton.interactable = currentPage > 0;
        if (nextPageButton != null)
            nextPageButton.interactable = currentPage < totalPages - 1;

        // Update page info
        if (pageInfoText != null)
            pageInfoText.text = $"Page {currentPage + 1} of {Mathf.Max(1, totalPages)}";

        // Update item count
        if (itemCountText != null)
            itemCountText.text = $"Showing {filteredItems.Count} items";
    }

    private void DisplayCurrentPage()
    {
        // Clear existing items
        foreach (Transform child in beginJourneyContainer)
        {
            Destroy(child.gameObject);
        }

        // Calculate items for current page
        int startIndex = currentPage * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, filteredItems.Count);

        // Create UI for current page items
        for (int i = startIndex; i < endIndex; i++)
        {
            CreateShopItemUI(filteredItems[i]);
        }
    }

    // Event handlers
    private void OnSearchTextChanged(string searchText)
    {
        currentSearchText = searchText;
        currentPage = 0; // Reset to first page
        ApplyFiltersAndPagination();
    }

    private void OnCategoryFilterChanged(int filterIndex)
    {
        currentPage = 0;
        ApplyFiltersAndPagination();
    }

    private void OnTierFilterChanged(int filterIndex)
    {
        Debug.Log($"[ShopLooby] Tier filter changed to index: {filterIndex}");

        if (filterIndex == 0)
        {
            Debug.Log("[ShopLooby] Showing all tiers");
        }
        else
        {
            ItemTier selectedTier = (ItemTier)filterIndex;
            Debug.Log($"[ShopLooby] Selected tier: {selectedTier} (enum value: {(int)selectedTier})");
        }

        currentPage = 0;
        ApplyFiltersAndPagination();
    }

    private void OnSortOptionChanged(int sortIndex)
    {
        currentSortOption = (SortOption)sortIndex;
        ApplyFiltersAndPagination();
    }

    private void ClearAllFilters()
    {
        if (searchInputField != null) searchInputField.text = "";
        if (categoryFilter != null) categoryFilter.value = 0;
        if (tierFilter != null) tierFilter.value = 0;
        if (sortDropdown != null) sortDropdown.value = 0;

        currentSearchText = "";
        currentSortOption = SortOption.NameAZ;
        currentPage = 0;

        ApplyFiltersAndPagination();
    }

    private void RefreshShopItems()
    {
        LoadBeginJourneyItemsFromDatabase();
        allShopItems = new List<ShopItemData>(beginJourneyItems);
        ApplyFiltersAndPagination();
    }

    private void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            DisplayCurrentPage();
            UpdatePagination();
        }
    }

    private void NextPage()
    {
        int totalPages = Mathf.CeilToInt((float)filteredItems.Count / itemsPerPage);
        if (currentPage < totalPages - 1)
        {
            currentPage++;
            DisplayCurrentPage();
            UpdatePagination();
        }
    }

    #endregion
}

// ✅ Shop Item Data Class
[System.Serializable]
public class ShopItemData
{
    public ItemData itemData;
    public long price;
    public CurrencyType currencyType;
    public bool isAvailable = true;
    public int stockLimit = -1;

    // ✅ Constructor ใหม่ที่ใช้ข้อมูลจาก ItemData
    public ShopItemData(ItemData item)
    {
        itemData = item;
        price = item.BuyPrice > 0 ? item.BuyPrice : GetDefaultPrice(item);
        currencyType = item.BuyCurrencyType;
        isAvailable = true;
        stockLimit = -1;
    }

    // ✅ Constructor เดิมสำหรับ backward compatibility
    public ShopItemData(ItemData item, long itemPrice)
    {
        itemData = item;
        price = itemPrice;
        currencyType = item.BuyCurrencyType; // ใช้จาก ItemData
        isAvailable = true;
        stockLimit = -1;
    }

    public ShopItemData(ItemData item, long itemPrice, CurrencyType currency)
    {
        itemData = item;
        price = itemPrice;
        currencyType = currency;
        isAvailable = true;
        stockLimit = -1;
    }

    // ✅ กำหนดราคาเริ่มต้นถ้าไม่ได้ set ใน ItemData
    private long GetDefaultPrice(ItemData item)
    {
        if (item.BuyCurrencyType == CurrencyType.Gems)
        {
            // ราคา Gem (ถูกกว่า Gold)
            return item.Tier switch
            {
                ItemTier.Common => 5,
                ItemTier.Uncommon => 10,
                ItemTier.Rare => 25,
                ItemTier.Epic => 50,
                ItemTier.Legendary => 100,
                _ => 10
            };
        }
        else
        {
            // ราคา Gold
            return item.Tier switch
            {
                ItemTier.Common => 100,
                ItemTier.Uncommon => 250,
                ItemTier.Rare => 500,
                ItemTier.Epic => 1500,
                ItemTier.Legendary => 3000,
                _ => 250
            };
        }
    }

    public bool IsValid()
    {
        return itemData != null && price > 0 && isAvailable;
    }

    // ✅ เพิ่ม method สำหรับแสดงสกุลเงิน
    public string GetPriceDisplayText()
    {
        string currencyIcon = currencyType == CurrencyType.Gold ? "💰" : "💎";
        return $"{currencyIcon} {price:N0}";
    }
}

#endregion
// ✅ Shop Item UI Component
