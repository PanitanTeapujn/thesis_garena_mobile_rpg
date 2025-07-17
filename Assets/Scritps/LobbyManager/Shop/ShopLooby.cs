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
    TierHighLow,
    TypeAZ,         // ✅ เพิ่มที่หายไป
    TypeZA          // ✅ เพิ่มที่หายไป
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
    [Header("🏆 Legend Shop")]
    public Transform legendShopContainer;
    public List<ShopItemData> legendShopItems = new List<ShopItemData>();
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
    private List<ShopItemData> currentShopItems = new List<ShopItemData>();
    private Transform currentShopContainer;
    private string currentShopName = "Begin Journey";
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
    [Header("🛒 Sell Panel")]
    public Transform sellInventoryContainer;        // Container สำหรับแสดง inventory ใน sell panel
    public GameObject sellItemPrefab;              // Prefab สำหรับแสดง item ที่ขายได้ (ใช้ shopItemPrefab ได้)
    public ScrollRect sellScrollRect;
    [Header("🛒 Sell Item Detail")]
    public GameObject sellItemDetailPanel;         // Panel แสดงรายละเอียดไอเทมที่จะขาย
    public Image sellDetailItemIcon;
    public TextMeshProUGUI sellDetailItemName;
    public TextMeshProUGUI sellDetailItemType;
    public TextMeshProUGUI sellDetailItemTier;
    public TextMeshProUGUI sellDetailItemDescription;
    public TextMeshProUGUI sellDetailItemStats;
    public TextMeshProUGUI sellDetailSellPrice;    // ราคาขาย
    public Button sellButton;                      // ปุ่มขาย
    public Button closeSellDetailButton;
    public Button sellAllButton;                    // ✅ เพิ่มปุ่ม Sell All
    private List<InventoryItem> selectedSellAllItems = new List<InventoryItem>();
    private bool isSellAllMode = false;
    private List<InventoryItem> sellableItems = new List<InventoryItem>();
    private InventoryItem selectedSellItem;
    private int selectedSellSlotIndex = -1;
    // Scroll rect สำหรับ sell panel
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
        StartCoroutine(CurrencyUpdateLoop());

        InitializeShop();
        SetupEventListeners();
        SetupShopData();
        StartCoroutine(DelayedShopSetup());

    }
    #region Delayed Setup Methods
    // ✅ **เพิ่ม Coroutine สำหรับ setup ที่ต้องรอ**
    private IEnumerator DelayedShopSetup()
    {
        // รอ 2 frames ให้ UI components พร้อม
        yield return null;
        yield return null;

        Debug.Log("[ShopLooby] 🔄 Starting delayed shop setup...");

        SetupShopData();

        Debug.Log("[ShopLooby] ✅ Delayed shop setup completed");
    }

    private IEnumerator DelayedDropdownSetup()
    {
        // รอ 2 frames ให้ UI setup เสร็จ
        yield return null;
        yield return null;

        Debug.Log("[ShopLooby] 🔄 Starting delayed dropdown setup...");

        // Force setup dropdowns อีกครั้ง
        SetupFiltersAndSorting();

        // รอ 1 frame แล้ว debug state
        yield return null;

        Debug.Log("[ShopLooby] ✅ Delayed dropdown setup completed");
    }
    #endregion
    
    void OnEnable()
    {
        UpdateCurrencyDisplay();

        // ✅ ลองหา character ใหม่เมื่อเปิด panel
        if (playerCharacter == null)
        {
            playerCharacter = FindPlayerCharacterForSell();
            if (playerCharacter != null)
            {
                Debug.Log($"[ShopLooby] Found character on panel enable: {playerCharacter.CharacterName}");
            }
        }

        // Setup dropdown อีกครั้งเมื่อเปิด shop
        StartCoroutine(DelayedDropdownSetup());
    }

    #region Debug Methods

    #endregion
    void OnDestroy()
    {
        RemoveEventListeners();
        StopAllCoroutines();

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
    private IEnumerator CurrencyUpdateLoop()
    {
        while (true)
        {
            UpdateCurrencyDisplay();
            yield return new WaitForSeconds(0.5f); // อัพเดททุก 0.5 วินาที
        }
    }
    private void UpdateCurrencyDisplay()
    {
        try
        {
            // ✅ **หา CurrencyManager ใหม่ทุกครั้งถ้าไม่มี**
            if (currencyManager == null)
            {
                currencyManager = CurrencyManager.FindCurrencyManager();
            }

            if (currencyManager != null)
            {
                if (currentGoldText != null)
                {
                    long currentGold = currencyManager.GetCurrentGold();
                    currentGoldText.text = $"{currentGold:N0}";
                }

                if (currentGemsText != null)
                {
                    int currentGems = currencyManager.GetCurrentGems();
                    currentGemsText.text = $"{currentGems:N0}";
                }
            }
            else
            {
                // ✅ **แสดง "Loading..." แทน empty string**
                if (currentGoldText != null)
                {
                    currentGoldText.text = "Loading...";
                }

                if (currentGemsText != null)
                {
                    currentGemsText.text = "Loading...";
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ShopLooby] ❌ Error updating currency display: {e.Message}");
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

        if (sellButton != null)
            sellButton.onClick.AddListener(OnSellButtonClicked);
        if (closeSellDetailButton != null)
            closeSellDetailButton.onClick.AddListener(HideSellItemDetailPanel);
        if (sellAllButton != null)
            sellAllButton.onClick.AddListener(OnSellAllButtonClicked);
        // Enhanced shop listeners
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

        if (sellButton != null)
            sellButton.onClick.RemoveAllListeners();
        if (closeSellDetailButton != null)
            closeSellDetailButton.onClick.RemoveAllListeners();
        if (sellAllButton != null)
            sellAllButton.onClick.RemoveAllListeners();
    }

    private void SetupShopData()
    {
        // ✅ Auto setup layout สำหรับทั้งสอง container
        AutoSetupLayout();

        // ✅ **ไม่ setup dropdown ตรงนี้ เพราะจะทำใน DelayedDropdownSetup แล้ว**
        // SetupFiltersAndSorting(); // ← ลบบรรทัดนี้

        // ถ้าไม่มีข้อมูลใน Inspector ให้สร้างจาก ItemDatabase
        if (beginJourneyItems.Count == 0)
        {
            LoadBeginJourneyItemsFromDatabase();
        }

        // ✅ เริ่มต้นด้วย Begin Journey
        SwitchToBeginJourneyShop();
    }
    private void SwitchToBeginJourneyShop()
    {
        currentShopItems = beginJourneyItems;
        currentShopContainer = beginJourneyContainer;
        currentShopName = "Begin Journey";

        // ✅ **อัพเดทระบบ filter และ pagination**
        allShopItems = new List<ShopItemData>(beginJourneyItems);
        ApplyFiltersAndPagination();

        Debug.Log($"[ShopLooby] ✅ Switched to Begin Journey Shop - {beginJourneyItems.Count} items");
    }

    // ✅ **9. เพิ่ม method สำหรับเปลี่ยนไป Legend Shop**
    private void SwitchToLegendShop()
    {
        currentShopItems = legendShopItems;
        currentShopContainer = legendShopContainer;
        currentShopName = "Legend Shop";

        // ✅ **อัพเดทระบบ filter และ pagination**
        allShopItems = new List<ShopItemData>(legendShopItems);
        ApplyFiltersAndPagination();

        Debug.Log($"[ShopLooby] ✅ Switched to Legend Shop - {legendShopItems.Count} items");
    }

    // ✅ เพิ่ม method สำหรับตั้งค่า Layout อัตโนมัติ
    private void AutoSetupLayout()
    {
        // ✅ **Setup Begin Journey Container**
        SetupContainer(beginJourneyContainer, "Begin Journey", 8, new Vector2(150, 200));

        // ✅ **Setup Legend Shop Container**
        SetupContainer(legendShopContainer, "Legend Shop", 8, new Vector2(150, 200));
    }

    // ✅ **12. เพิ่ม helper method สำหรับ setup container**
    private void SetupContainer(Transform container, string containerName, int columns, Vector2 cellSize)
    {
        if (container == null)
        {
            Debug.LogError($"[ShopLooby] {containerName} container is null! Please assign it in Inspector");
            return;
        }

        // ลบ Layout Group เก่า
        var existingLayouts = container.GetComponents<LayoutGroup>();
        for (int i = 0; i < existingLayouts.Length; i++)
        {
            if (Application.isPlaying)
                DestroyImmediate(existingLayouts[i]);
            else
                DestroyImmediate(existingLayouts[i]);
        }

        // เพิ่ม Grid Layout Group
        GridLayoutGroup gridLayout = beginJourneyContainer.gameObject.AddComponent<GridLayoutGroup>();
        GridLayoutGroup gridLayoutlegen = legendShopContainer.gameObject.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(150, 200);
        gridLayout.spacing = new Vector2(10, 5);
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.UpperLeft;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 8; // 3 คอลัมน์gridLayout.cellSize = new Vector2(150, 200);

        gridLayoutlegen.cellSize = new Vector2(150, 200);
        gridLayoutlegen.spacing = new Vector2(10, 5);
        gridLayoutlegen.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayoutlegen.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayoutlegen.childAlignment = TextAnchor.UpperLeft;
        gridLayoutlegen.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayoutlegen.constraintCount = 8; // 3 คอลัมน์

        // เพิ่ม Content Size Fitter
        ContentSizeFitter fitter = container.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = container.gameObject.AddComponent<ContentSizeFitter>();
        }
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Debug.Log($"[ShopLooby] ✅ {containerName} container setup completed - {columns} columns with {cellSize} cells");
    }
    private void OnSellButtonClicked()
    {
        if (selectedSellItem != null)
        {
            ShowSellQuantityPanel();
        }
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

        // ✅ **ใช้ currentShopContainer แทน beginJourneyContainer**
        if (currentShopContainer == null)
        {
            Debug.LogError("[ShopLooby] currentShopContainer is null!");
            return;
        }

        Debug.Log($"[ShopLooby] 🔨 Creating {currentShopName} item UI for: {shopItemData.itemData.ItemName}");

        GameObject shopItemObj = Instantiate(shopItemPrefab, currentShopContainer);
        ShopItemUI shopItemUI = shopItemObj.GetComponent<ShopItemUI>();

        if (shopItemUI == null)
        {
            shopItemUI = shopItemObj.AddComponent<ShopItemUI>();
        }

        shopItemUI.SetupShopItem(shopItemData, this);
        Debug.Log($"[ShopLooby] ✅ {currentShopName} item UI created successfully for: {shopItemData.itemData.ItemName}");
    }

    #endregion

    #region Panel Management
    private void ShowBeginJourneyPanel()
    {
        SetActivePanel(beginJourneyPanel);
        ClearSelectedItem();
        SwitchToBeginJourneyShop();
        Debug.Log("[ShopLooby] Showing Begin Journey panel");
    }

    private void ShowLegendShopPanel()
    {
        SetActivePanel(legendShopPanel);
        ClearSelectedItem();
        SwitchToLegendShop();
        Debug.Log("[ShopLooby] Showing Legend Shop panel");
    }


    // ✅ **6. เพิ่ม method สำหรับโหลด Legend Shop items**
    private void LoadLegendShopItems()
    {
        // ✅ **ถ้ายังไม่ได้โหลด หรือต้องการ refresh**
        if (legendShopItems.Count == 0)
        {
            LoadLegendShopItemsFromDatabase();
        }

        // ✅ **ใช้ระบบเดิมในการแสดงผล**
        DisplayLegendShopItems();
    }

    // ✅ **7. เพิ่ม method สำหรับโหลด Legend items จาก database**
    private void LoadLegendShopItemsFromDatabase()
    {
        ItemDatabase database = ItemDatabase.Instance;
        legendShopItems.Clear();

        var allItems = database.GetAllItems();
        Debug.Log($"[ShopLooby] Loading legend shop items from {allItems.Count} total items");

        foreach (var item in allItems)
        {
            if (item == null)
            {
                Debug.LogWarning("[ShopLooby] Found null item in database!");
                continue;
            }

            // ✅ **กรองเฉพาะไอเทม Legend**
            if (IsLegendShopItem(item))
            {
                ShopItemData newShopItem = new ShopItemData(item);

                if (newShopItem.itemData == null)
                {
                    Debug.LogError($"[ShopLooby] ShopItemData creation failed for item: {item.ItemName}");
                    continue;
                }

                legendShopItems.Add(newShopItem);

                string currencyType = newShopItem.currencyType == CurrencyType.Gold ? "Gold" : "Gems";
                Debug.Log($"[ShopLooby] ✅ Added legend item: {item.ItemName} ({item.ItemType}) - Price: {newShopItem.price} {currencyType}");
            }
        }

        Debug.Log($"[ShopLooby] ✅ Loaded {legendShopItems.Count} items for Legend shop");
    }

    // ✅ **8. เพิ่ม method สำหรับกำหนดว่าไอเทมไหนเป็น Legend**
    private bool IsLegendShopItem(ItemData item)
    {
        // ✅ **เอาเฉพาะ Epic และ Legendary**
        if (item.Tier == ItemTier.Epic || item.Tier == ItemTier.Legendary)
            return true;

        // ✅ **เอา Rare ที่เป็น Weapon หรือ Armor**
        if (item.Tier == ItemTier.Rare &&
            (item.ItemType == ItemType.Weapon || item.ItemType == ItemType.Armor))
            return true;

        // ✅ **เอา Rune ทุกระดับ**
        if (item.ItemType == ItemType.Rune)
            return true;

        return false;
    }

    // ✅ **9. เพิ่ม method สำหรับแสดง Legend Shop items**
    private void DisplayLegendShopItems()
    {
        if (legendShopContainer == null)
        {
            Debug.LogError("[ShopLooby] legendShopContainer is null! Please assign it in Inspector");
            return;
        }

        // ✅ **ลบ items เก่า**
        foreach (Transform child in legendShopContainer)
        {
            Destroy(child.gameObject);
        }

        // ✅ **สร้าง items ใหม่**
        foreach (var shopItemData in legendShopItems)
        {
            CreateLegendShopItemUI(shopItemData);
        }

        Debug.Log($"[ShopLooby] Displayed {legendShopItems.Count} legend shop items");
    }

    // ✅ **10. เพิ่ม method สำหรับสร้าง Legend Shop item UI**
    private void CreateLegendShopItemUI(ShopItemData shopItemData)
    {
        if (shopItemData == null || shopItemData.itemData == null)
        {
            Debug.LogError("[ShopLooby] Invalid legend shop item data!");
            return;
        }

        Debug.Log($"[ShopLooby] 🏆 Creating legend shop item UI for: {shopItemData.itemData.ItemName}");

        // ✅ **ใช้ prefab เดียวกัน**
        GameObject shopItemObj = Instantiate(shopItemPrefab, legendShopContainer);
        ShopItemUI shopItemUI = shopItemObj.GetComponent<ShopItemUI>();

        if (shopItemUI == null)
        {
            shopItemUI = shopItemObj.AddComponent<ShopItemUI>();
        }

        // ✅ **ใช้ method เดียวกัน**
        shopItemUI.SetupShopItem(shopItemData, this);

        Debug.Log($"[ShopLooby] ✅ Legend shop item UI created: {shopItemData.itemData.ItemName}");
    }

    private void ShowSellPanel()
    {
        Debug.Log("[ShopLooby] 📋 Showing Sell panel...");

        SetActivePanel(sellPanel);
        ClearSelectedItem();

        // ✅ ล้างข้อมูล Sell All
        CleanupSellAllMode();

        // Find character
        if (playerCharacter == null)
            playerCharacter = FindPlayerCharacterForSell();

        if (playerCharacter?.GetInventory() == null)
        {
            ShowMessage("Player inventory not found!");
            UpdateSellAllButton();
            return;
        }

        // ✅ ใช้ ForceRefreshSellPanel แทน
        ForceRefreshSellPanel();
    }
    private void LoadSellableItems()
    {
        sellableItems.Clear();

        if (playerCharacter?.GetInventory() == null)
        {
            ShowMessage("No inventory found!");
            return;
        }

        Inventory playerInventory = playerCharacter.GetInventory();

        for (int i = 0; i < playerInventory.CurrentSlots; i++)
        {
            InventoryItem item = playerInventory.GetItem(i);
            if (item != null && !item.IsEmpty && item.itemData != null &&
                item.itemData.IsSellable && item.itemData.SellPrice > 0)
            {
                sellableItems.Add(item);
            }
        }

        DisplaySellableItems();
    }

    private Character FindPlayerCharacterForSell()
    {
        Character foundCharacter = null;

        // วิธีที่ 1: ใช้ method ที่มีอยู่แล้ว
        if (playerCharacter != null)
        {
            return playerCharacter;
        }

        // วิธีที่ 2: หาจาก CombatUIManager
        CombatUIManager uiManager = FindObjectOfType<CombatUIManager>();
        if (uiManager != null && uiManager.localHero != null)
        {
            foundCharacter = uiManager.localHero;
            Debug.Log("[ShopLooby] Found character from CombatUIManager");
            return foundCharacter;
        }

        // วิธีที่ 3: หาจาก Hero ที่มี InputAuthority
        Hero[] heroes = FindObjectsOfType<Hero>();
        foreach (Hero hero in heroes)
        {
            if (hero.HasInputAuthority && hero.IsSpawned)
            {
                foundCharacter = hero;
                Debug.Log("[ShopLooby] Found character from Hero with InputAuthority");
                return foundCharacter;
            }
        }

        // วิธีที่ 4: หาจาก tag "Player"
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            foundCharacter = playerObj.GetComponent<Character>();
            if (foundCharacter != null)
            {
                Debug.Log("[ShopLooby] Found character from Player tag");
                return foundCharacter;
            }
        }

        // วิธีที่ 5: หา Character ใดๆ ที่มี Inventory
        Character[] characters = FindObjectsOfType<Character>();
        foreach (Character character in characters)
        {
            if (character.GetInventory() != null)
            {
                foundCharacter = character;
                Debug.Log("[ShopLooby] Found character with Inventory component");
                return foundCharacter;
            }
        }

        Debug.LogError("[ShopLooby] No character found with any method!");
        return null;
    }
    private void DisplaySellableItems()
    {
        if (sellInventoryContainer == null) return;

        if (sellableItems.Count == 0)
        {
            ShowMessage("No sellable items in your inventory!");
            UpdateSellAllButton(); // ✅ เพิ่มบรรทัดนี้
            return;
        }

        foreach (InventoryItem item in sellableItems)
        {
            if (item?.itemData != null)
                CreateSellItemUI(item);
        }

        UpdateSellAllButton(); // ✅ เพิ่มบรรทัดนี้
    }
    private void CreateSellItemUI(InventoryItem inventoryItem)
    {
        if (inventoryItem?.itemData == null || !inventoryItem.itemData.IsSellable) return;

        GameObject prefabToUse = sellItemPrefab ?? shopItemPrefab;
        if (prefabToUse == null) return;

        try
        {
            GameObject sellItemObj = Instantiate(prefabToUse, sellInventoryContainer);
            ShopItemUI sellItemUI = sellItemObj.GetComponent<ShopItemUI>() ?? sellItemObj.AddComponent<ShopItemUI>();

            ShopItemData sellItemData = new ShopItemData(inventoryItem.itemData);
            sellItemData.price = inventoryItem.itemData.SellPrice;
            sellItemData.currencyType = CurrencyType.Gold;

            sellItemUI.SetupShopItem(sellItemData, this);

            // Create local copy to avoid reference issues
            InventoryItem localItem = new InventoryItem(inventoryItem.itemData, inventoryItem.stackCount, inventoryItem.slotIndex);

            Button itemButton = sellItemObj.GetComponent<Button>() ?? sellItemObj.GetComponentInChildren<Button>();
            if (itemButton != null)
            {
                itemButton.onClick.RemoveAllListeners();
                itemButton.onClick.AddListener(() => OnSellItemClicked(localItem));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ShopLooby] Error creating sell item UI: {e.Message}");
        }
    }

    private void OnSellItemClicked(InventoryItem inventoryItem)
    {
        if (inventoryItem?.itemData == null)
        {
            ShowMessage("Error: Invalid item!");
            return;
        }

        // Check if item still exists in inventory
        if (playerCharacter?.GetInventory() != null)
        {
            Inventory inventory = playerCharacter.GetInventory();
            InventoryItem currentItem = inventory.GetItem(inventoryItem.slotIndex);

            if (currentItem?.itemData != inventoryItem.itemData)
            {
                ShowMessage("Item is no longer available!");
                RefreshSellPanel();
                return;
            }
        }

        if (!inventoryItem.itemData.IsSellable || inventoryItem.itemData.SellPrice <= 0)
        {
            ShowMessage("This item cannot be sold!");
            return;
        }

        selectedSellItem = inventoryItem;
        selectedSellSlotIndex = inventoryItem.slotIndex;

        ShowSellQuantityPanel();
    }

    private void ShowSellQuantityPanel()
    {
        if (selectedSellItem?.itemData == null)
        {
            Debug.LogError("[ShopLooby] No sell item selected");
            return;
        }

        if (purchaseQuantityPanel == null)
        {
            Debug.LogError("[ShopLooby] purchaseQuantityPanel not assigned!");
            return;
        }

        Debug.Log($"[ShopLooby] Showing sell quantity panel for: {selectedSellItem.itemData.ItemName}");

        // ซ่อน sell detail panel ถ้าเปิดอยู่
        HideSellItemDetailPanel();

        // แสดง quantity panel
        purchaseQuantityPanel.SetActive(true);

        // Setup UI สำหรับ sell mode
        SetupSellQuantityPanelUI();
    }
    private void SetupSellQuantityPanelUI()
    {
        if (selectedSellItem?.itemData == null) return;

        ItemData itemData = selectedSellItem.itemData;
        int maxSellQuantity = selectedSellItem.stackCount;

        if (quantityItemIcon != null)
            quantityItemIcon.sprite = itemData.ItemIcon;

        if (quantityItemName != null)
        {
            quantityItemName.text = $"SELL: {itemData.ItemName}";
            quantityItemName.color = itemData.GetTierColor();
        }

        if (quantityItemPrice != null)
            quantityItemPrice.text = $"💰 {itemData.SellPrice:N0} Gold each";

        if (quantitySlider != null)
        {
            quantitySlider.minValue = 1;
            quantitySlider.maxValue = maxSellQuantity;
            quantitySlider.value = 1;
        }

        if (maxAffordableText != null)
        {
            long currentGold = currencyManager?.GetCurrentGold() ?? 0;
            maxAffordableText.text = $"Current Gold: {currentGold:N0}\nMax to sell: {maxSellQuantity}";
        }

        if (confirmPurchaseButton != null)
        {
            TextMeshProUGUI buttonText = confirmPurchaseButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = "SELL";

            confirmPurchaseButton.interactable = true;
            ColorBlock colors = confirmPurchaseButton.colors;
            colors.normalColor = Color.green;
            confirmPurchaseButton.colors = colors;
        }

        selectedQuantity = 1;
        maxAffordableQuantity = maxSellQuantity;
        UpdateSellQuantityDisplay();
    }
    private void UpdateSellQuantityDisplay()
    {
        if (selectedSellItem?.itemData == null) return;

        if (quantityValueText != null)
            quantityValueText.text = selectedQuantity.ToString();

        if (totalPriceText != null)
        {
            long totalSellPrice = selectedSellItem.itemData.SellPrice * selectedQuantity;
            totalPriceText.text = $"Total: {totalSellPrice:N0} Gold";
        }
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
                buyButtonText.text = "BUY";

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

        if (selectedSellItem != null)
            UpdateSellQuantityDisplay();
        else
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
        if (isSellAllMode && selectedSellAllItems.Count > 0)
        {
            ProcessSellAllTransaction();
        }
        else if (selectedSellItem != null)
        {
            ProcessSellTransaction();
        }
        else if (selectedShopItem != null)
        {
            ProcessPurchaseTransaction();
        }
        else
        {
            ShowMessage("No item selected!");
        }
    }
    private void ProcessSellAllTransaction()
    {
        try
        {
            // ตรวจสอบระบบ
            if (currencyManager == null)
                currencyManager = CurrencyManager.FindCurrencyManager();
            if (playerCharacter == null)
                playerCharacter = FindPlayerCharacterForSell();

            if (currencyManager == null || playerCharacter == null)
            {
                ShowMessage("Error: System not found!");
                return;
            }

            Inventory playerInventory = playerCharacter.GetInventory();
            if (playerInventory == null)
            {
                ShowMessage("Error: Inventory not found!");
                return;
            }

            Debug.Log("[ShopLooby] 🔄 Starting Sell All transaction...");

            // ✅ รีเฟรช sellableItems ให้ล่าสุดก่อน
            RefreshSellableItemsList();

            if (sellableItems.Count == 0)
            {
                ShowMessage("No sellable items found!");
                return;
            }

            // ✅ คำนวณรายการและราคารวม
            long totalValue = 0;
            int totalItemsSold = 0;
            var soldItems = new List<(string name, int count, long price)>();

            Debug.Log($"[ShopLooby] Processing {sellableItems.Count} sellable items...");

            // ✅ ลบและขายทีละชิ้น พร้อมเก็บบันทึก
            foreach (var sellItem in sellableItems.ToList()) // ToList() เพื่อหลีกเลี่ยง modification during iteration
            {
                if (sellItem?.itemData == null) continue;

                // ตรวจสอบว่า item ยังอยู่ใน slot หรือไม่
                InventoryItem currentItem = playerInventory.GetItem(sellItem.slotIndex);
                if (currentItem?.itemData?.ItemId != sellItem.itemData.ItemId)
                {
                    Debug.LogWarning($"[ShopLooby] Item {sellItem.itemData.ItemName} no longer in slot {sellItem.slotIndex}");
                    continue;
                }

                // ลบ item ทั้งหมดใน slot
                int quantityToSell = currentItem.stackCount;
                long itemValue = sellItem.itemData.SellPrice * quantityToSell;

                Debug.Log($"[ShopLooby] Selling {quantityToSell}x {sellItem.itemData.ItemName} for {itemValue} gold");

                // ✅ ลบ item ทั้งหมดใน slot
                if (playerInventory.RemoveItem(sellItem.slotIndex, quantityToSell))
                {
                    totalValue += itemValue;
                    totalItemsSold += quantityToSell;
                    soldItems.Add((sellItem.itemData.ItemName, quantityToSell, itemValue));
                    Debug.Log($"[ShopLooby] ✅ Sold {quantityToSell}x {sellItem.itemData.ItemName}");
                }
                else
                {
                    Debug.LogWarning($"[ShopLooby] ❌ Failed to sell {sellItem.itemData.ItemName}");
                }
            }

            if (totalItemsSold == 0)
            {
                ShowMessage("Failed to sell any items!");
                return;
            }

            // ✅ เพิ่มเงิน
            Debug.Log($"[ShopLooby] Adding {totalValue} gold to player...");
            if (!currencyManager.AddGold(totalValue))
            {
                ShowMessage("Failed to add gold!");
                Debug.LogError("[ShopLooby] ❌ Failed to add gold - transaction may be incomplete!");
                return;
            }

            // ✅ สำเร็จ - แสดงผลลัพธ์
            Debug.Log($"[ShopLooby] ✅ Sell All completed: {totalItemsSold} items for {totalValue} gold");
            ShowMessage($"Sold {totalItemsSold} items for {totalValue:N0} gold!");

            // ✅ ปิด panels และล้างข้อมูล
            CleanupSellAllMode();

            // ✅ รีเฟรช UI ทันที
            ForceRefreshSellPanel();

            // ✅ อัปเดท currency display
            UpdateCurrencyDisplay();

            Debug.Log("[ShopLooby] 🎉 Sell All transaction completed successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ShopLooby] ❌ Sell All transaction error: {e.Message}");
            ShowMessage("Sell All transaction failed!");
            CleanupSellAllMode();
        }
    }
    private void RefreshSellableItemsList()
    {
        Debug.Log("[ShopLooby] 🔄 Refreshing sellable items list...");

        sellableItems.Clear();

        if (playerCharacter?.GetInventory() == null)
            return;

        Inventory playerInventory = playerCharacter.GetInventory();

        // หาของทุกชิ้นที่ขายได้ใหม่
        for (int i = 0; i < playerInventory.CurrentSlots; i++)
        {
            InventoryItem item = playerInventory.GetItem(i);

            if (item != null && !item.IsEmpty && item.itemData != null &&
                item.itemData.IsSellable && item.itemData.SellPrice > 0)
            {
                sellableItems.Add(item);
            }
        }

        Debug.Log($"[ShopLooby] Found {sellableItems.Count} sellable items");
    }

    // ===== เพิ่ม method ล้างข้อมูล Sell All =====

    private void CleanupSellAllMode()
    {
        Debug.Log("[ShopLooby] 🧹 Cleaning up Sell All mode...");

        selectedSellAllItems.Clear();
        isSellAllMode = false;
        selectedQuantity = 1;
        selectedShopItem = null;
        selectedSellItem = null;
        selectedSellSlotIndex = -1;

        // ปิด quantity panel
        if (purchaseQuantityPanel != null)
        {
            purchaseQuantityPanel.SetActive(false);
        }

        // แสดง slider และ quantity text กลับมา
        if (quantitySlider != null)
            quantitySlider.gameObject.SetActive(true);
        if (quantityValueText != null)
            quantityValueText.gameObject.SetActive(true);
    }

    // ===== เพิ่ม method Force Refresh UI =====

    private void ForceRefreshSellPanel()
    {
        Debug.Log("[ShopLooby] 🔄 Force refreshing sell panel UI...");

        try
        {
            if (sellInventoryContainer == null)
            {
                Debug.LogWarning("[ShopLooby] sellInventoryContainer is null!");
                return;
            }

            // ✅ ลบ UI เก่าออกทั้งหมด
            int childCount = sellInventoryContainer.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Transform child = sellInventoryContainer.GetChild(i);
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }

            Debug.Log($"[ShopLooby] Destroyed {childCount} old sell item UIs");

            // ✅ รอ 1 frame ให้ UI ลบเสร็จ
            StartCoroutine(DelayedLoadSellItems());
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ShopLooby] Error force refreshing sell panel: {e.Message}");
        }
    }

    // ===== เพิ่ม Coroutine สำหรับ delayed load =====

    private System.Collections.IEnumerator DelayedLoadSellItems()
    {
        // รอ 1 frame ให้ UI ลบเสร็จ
        yield return null;

        Debug.Log("[ShopLooby] 🔄 Loading sell items after delay...");

        // รีเฟรช sellableItems อีกครั้ง
        RefreshSellableItemsList();

        // แสดง UI ใหม่
        DisplaySellableItems();

        Debug.Log("[ShopLooby] ✅ Sell panel refreshed completely");
    }
    private void ProcessPurchaseTransaction()
    {
        Debug.Log("[ShopLooby] 🔄 Starting purchase transaction...");

        if (selectedShopItem == null || selectedShopItem.itemData == null || selectedQuantity <= 0)
        {
            Debug.LogError("[ShopLooby] ❌ Invalid purchase data!");
            ShowMessage("Error: Invalid purchase data!");
            return;
        }

        if (currencyManager == null)
        {
            Debug.LogError("[ShopLooby] ❌ Currency manager not found!");
            ShowMessage("Error: Currency system not found!");
            return;
        }

        // ตรวจสอบเงินตามสกุลเงิน
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
            Debug.LogWarning($"[ShopLooby] ❌ Not enough {currencyName} for purchase");
            ShowMessage($"Not enough {currencyName}!");
            return;
        }

        // ตรวจสอบ Character และ Inventory
        if (playerCharacter == null)
        {
            FindPlayerCharacter();
            if (playerCharacter == null)
            {
                Debug.LogError("[ShopLooby] ❌ Player character not found!");
                ShowMessage("Error: Player character not found!");
                return;
            }
        }

        Inventory playerInventory = playerCharacter.GetInventory();
        if (playerInventory == null)
        {
            Debug.LogError("[ShopLooby] ❌ Player inventory not found!");
            ShowMessage("Error: Player inventory not found!");
            return;
        }

        // ซื้อไอเทม
        bool purchaseSuccess = ProcessQuantityPurchase(selectedShopItem, playerInventory, selectedQuantity);

        if (purchaseSuccess)
        {
            string currencyName = selectedShopItem.currencyType == CurrencyType.Gold ? "gold" : "gems";
            Debug.Log($"[ShopLooby] ✅ Successfully purchased: {selectedQuantity}x {selectedShopItem.itemData.ItemName} for {totalPrice} {currencyName}");
            ShowMessage($"Purchased {selectedQuantity}x {selectedShopItem.itemData.ItemName}!");
            HidePurchaseQuantityPanel();

            // อัพเดท currency ทันที
            UpdateCurrencyDisplay();
        }
        else
        {
            Debug.LogError("[ShopLooby] ❌ Purchase failed!");
            ShowMessage("Purchase failed!");
        }
    }
    private void ProcessSellTransaction()
    {
        try
        {
            // Basic validation
            if (selectedSellItem?.itemData == null || selectedQuantity <= 0)
            {
                ShowMessage("Error: Invalid sell data!");
                return;
            }

            // Find systems
            if (currencyManager == null)
                currencyManager = CurrencyManager.FindCurrencyManager();
            if (playerCharacter == null)
                playerCharacter = FindPlayerCharacterForSell();

            if (currencyManager == null || playerCharacter == null)
            {
                ShowMessage("Error: System not found!");
                return;
            }

            Inventory playerInventory = playerCharacter.GetInventory();
            if (playerInventory == null)
            {
                ShowMessage("Error: Inventory not found!");
                return;
            }

            // Validate slot and item
            if (selectedSellSlotIndex < 0 || selectedSellSlotIndex >= playerInventory.CurrentSlots)
            {
                ShowMessage("Error: Invalid slot!");
                return;
            }

            InventoryItem slotItem = playerInventory.GetItem(selectedSellSlotIndex);
            if (slotItem?.itemData == null || slotItem.itemData.ItemId != selectedSellItem.itemData.ItemId)
            {
                ShowMessage("Error: Item mismatch!");
                RefreshSellPanel();
                return;
            }

            if (selectedQuantity > slotItem.stackCount)
            {
                ShowMessage($"Error: Not enough items! Available: {slotItem.stackCount}");
                return;
            }

            // Process transaction
            long totalSellPrice = selectedSellItem.itemData.SellPrice * selectedQuantity;

            // Remove item first
            if (!playerInventory.RemoveItem(selectedSellSlotIndex, selectedQuantity))
            {
                ShowMessage("Failed to remove items!");
                return;
            }

            // Add gold
            if (!currencyManager.AddGold(totalSellPrice))
            {
                ShowMessage("Failed to add gold!");
                playerInventory.AddItem(selectedSellItem.itemData, selectedQuantity); // Revert
                return;
            }

            // Success
            ShowMessage($"Sold {selectedQuantity}x {selectedSellItem.itemData.ItemName} for {totalSellPrice:N0} gold!");

            // Cleanup
            selectedSellItem = null;
            selectedSellSlotIndex = -1;
            selectedQuantity = 1;

            if (purchaseQuantityPanel != null)
                purchaseQuantityPanel.SetActive(false);

            if (sellPanel != null && sellPanel.activeSelf)
                ForceRefreshSellPanel();

            UpdateCurrencyDisplay();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ShopLooby] Sell transaction error: {e.Message}");
            ShowMessage("Sell transaction failed!");
        }
    }
    private void RefreshSellPanel()
    {
        Debug.Log("[ShopLooby] 🔄 Refreshing sell panel...");

        if (sellInventoryContainer == null) return;

        // ใช้ ForceRefreshSellPanel แทน
        ForceRefreshSellPanel();
    }
    private void OnSellAllButtonClicked()
    {
        Debug.Log("[ShopLooby] 🛒 Sell All button clicked!");

        // ✅ รีเฟรช sellableItems ก่อน
        RefreshSellableItemsList();

        // ตรวจสอบว่ามีของขายหรือไม่
        if (sellableItems.Count == 0)
        {
            ShowMessage("No sellable items found!");
            return;
        }

        // ✅ คำนวณรายการที่จะขายแบบ realtime
        var sellList = CalculateSellAllItems();

        if (sellList.Count == 0)
        {
            ShowMessage("No valid items to sell!");
            return;
        }

        long totalValue = sellList.Sum(item => item.itemData.SellPrice * item.stackCount);
        int totalItems = sellList.Sum(item => item.stackCount);

        Debug.Log($"[ShopLooby] Sell All: {totalItems} items worth {totalValue} gold");

        // แสดง confirmation dialog
        ShowSellAllConfirmation(sellList, totalItems, totalValue);
    }

    // ===== 5. เพิ่ม method คำนวณรายการขาย =====

    private List<InventoryItem> CalculateSellAllItems()
    {
        Debug.Log("[ShopLooby] 🔢 Calculating sell all items...");

        List<InventoryItem> sellList = new List<InventoryItem>();

        if (playerCharacter?.GetInventory() == null)
            return sellList;

        Inventory playerInventory = playerCharacter.GetInventory();

        // ✅ หาของทุกชิ้นที่ขายได้ใหม่ (ไม่ใช้ cache)
        for (int i = 0; i < playerInventory.CurrentSlots; i++)
        {
            InventoryItem item = playerInventory.GetItem(i);

            if (item != null && !item.IsEmpty && item.itemData != null &&
                item.itemData.IsSellable && item.itemData.SellPrice > 0)
            {
                sellList.Add(item);
            }
        }

        Debug.Log($"[ShopLooby] Found {sellList.Count} items to sell");
        return sellList;
    }

    // ===== 6. เพิ่ม method แสดง confirmation =====

    private void ShowSellAllConfirmation(List<InventoryItem> sellList, int totalItems, long totalValue)
    {
        // ใช้ purchaseQuantityPanel แต่ปรับเป็น confirmation mode
        if (purchaseQuantityPanel == null)
        {
            Debug.LogError("[ShopLooby] purchaseQuantityPanel not assigned!");
            return;
        }

        purchaseQuantityPanel.SetActive(true);

        // Setup UI สำหรับ Sell All confirmation
        SetupSellAllConfirmationUI(sellList, totalItems, totalValue);
    }

    // ===== 7. เพิ่ม method setup UI confirmation =====

    private void SetupSellAllConfirmationUI(List<InventoryItem> sellList, int totalItems, long totalValue)
    {
        // แสดงไอคอน (ใช้ไอคอนทั่วไป)
        if (quantityItemIcon != null)
        {
            quantityItemIcon.gameObject.SetActive(false);
        }

        // แสดงชื่อ
        if (quantityItemName != null)
        {
            quantityItemName.text = "SELL ALL ITEMS";
            quantityItemName.color = Color.yellow;
        }

        // แสดงรายละเอียด
        if (quantityItemPrice != null)
        {
            quantityItemPrice.text = $" Total: {totalValue:N0} Gold";
        }

        // ซ่อน slider (ไม่ต้องเลือกจำนวน)
        if (quantitySlider != null)
        {
            quantitySlider.gameObject.SetActive(false);
        }

        // ซ่อน quantity text
        if (quantityValueText != null)
        {
            quantityValueText.gameObject.SetActive(false);
        }

        // แสดงข้อมูลรายการ
        if (maxAffordableText != null)
        {
            string itemList = "";
            for (int i = 0; i < Mathf.Min(5, sellList.Count); i++)
            {
                var item = sellList[i];
                itemList += $"• {item.itemData.ItemName} x{item.stackCount}\n";
            }

            if (sellList.Count > 5)
            {
                itemList += $"... and {sellList.Count - 5} more items";
            }

            maxAffordableText.text = $"Items to sell ({totalItems} total):\n{itemList}";
        }

        // แสดงราคารวม
        if (totalPriceText != null)
        {
            totalPriceText.text = $"Total Value: {totalValue:N0} Gold";
        }

        // ตั้งค่าปุ่ม Confirm
        if (confirmPurchaseButton != null)
        {
            TextMeshProUGUI buttonText = confirmPurchaseButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = "SELL ALL";

            confirmPurchaseButton.interactable = true;
            ColorBlock colors = confirmPurchaseButton.colors;
            colors.normalColor = Color.red; // สีแดงเพื่อให้เห็นว่าเป็นการขายทั้งหมด
            confirmPurchaseButton.colors = colors;
        }

        // เก็บข้อมูลสำหรับประมวลผล
        selectedSellAllItems = sellList;
        isSellAllMode = true;
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
   

    private void HideSellItemDetailPanel()
    {
        if (sellItemDetailPanel != null)
        {
            sellItemDetailPanel.SetActive(false);
        }
    }
    private void OnCancelPurchaseClicked()
    {
        HidePurchaseQuantityPanel();
        Debug.Log("[ShopLooby] Purchase/Sell/Sell All cancelled");
    }

    public void HidePurchaseQuantityPanel()
    {
        if (purchaseQuantityPanel != null)
            purchaseQuantityPanel.SetActive(false);

        // Reset all modes
        selectedQuantity = 1;
        selectedShopItem = null;
        selectedSellItem = null;
        selectedSellSlotIndex = -1;
        selectedSellAllItems.Clear();
        isSellAllMode = false;

        // แสดง slider และ quantity text กลับมา
        if (quantitySlider != null)
            quantitySlider.gameObject.SetActive(true);
        if (quantityValueText != null)
            quantityValueText.gameObject.SetActive(true);
    }
    private void UpdateSellAllButton()
    {
        if (sellAllButton == null) return;

        // ✅ รีเฟรช sellableItems ก่อน
        RefreshSellableItemsList();

        // เปิด/ปิดปุ่มตามจำนวนของที่ขายได้
        bool hasSellableItems = sellableItems.Count > 0;
        sellAllButton.interactable = hasSellableItems;

        // เปลี่ยนสีปุ่ม
        ColorBlock colors = sellAllButton.colors;
        colors.normalColor = hasSellableItems ? Color.white : Color.gray;
        sellAllButton.colors = colors;

        // อัปเดตข้อความ
        TextMeshProUGUI buttonText = sellAllButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            if (hasSellableItems)
            {
                long totalValue = sellableItems.Sum(item => item.itemData.SellPrice * item.stackCount);
                buttonText.text = $"SELL ALL ({totalValue:N0} Gold)";
            }
            else
            {
                buttonText.text = "SELL ALL";
            }
        }

        Debug.Log($"[ShopLooby] Updated Sell All button: {hasSellableItems} ({sellableItems.Count} items)");
    }

    // ===== 14. แก้ไข DisplaySellableItems เพื่อให้ update ปุ่ม =====



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

    #region Phase 3: Enhanced Shop Features

    private void SetupFiltersAndSorting()
    {
        Debug.Log("[ShopLooby] 🔧 Setting up filters and sorting...");

        // ✅ **Setup category filter dropdown with safety checks**
        if (categoryFilter != null)
        {
            try
            {
                categoryFilter.ClearOptions();
                List<string> categoryOptions = new List<string> { "All" };
                foreach (ItemType itemType in System.Enum.GetValues(typeof(ItemType)))
                {
                    categoryOptions.Add(itemType.ToString());
                }
                categoryFilter.AddOptions(categoryOptions);
                categoryFilter.value = 0;
                categoryFilter.RefreshShownValue();

                Debug.Log($"[ShopLooby] ✅ Category filter setup: {categoryOptions.Count} options");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ShopLooby] ❌ Category filter setup failed: {e.Message}");
            }
        }

        // ✅ **Setup tier filter dropdown with safety checks**
        if (tierFilter != null)
        {
            try
            {
                tierFilter.ClearOptions();
                List<string> tierOptions = new List<string> { "All" };

                foreach (ItemTier tier in System.Enum.GetValues(typeof(ItemTier)))
                {
                    tierOptions.Add(tier.ToString());
                }

                tierFilter.AddOptions(tierOptions);
                tierFilter.value = 0;
                tierFilter.RefreshShownValue();

                Debug.Log($"[ShopLooby] ✅ Tier filter setup: {tierOptions.Count} options");
                Debug.Log($"[ShopLooby] Tier options: {string.Join(", ", tierOptions)}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ShopLooby] ❌ Tier filter setup failed: {e.Message}");
            }
        }

        // ✅ **Setup sort dropdown with safety checks**
        if (sortDropdown != null)
        {
            try
            {
                sortDropdown.ClearOptions();
                List<string> sortOptions = new List<string>
            {
                "Name A-Z", "Name Z-A",
                "Price Low-High", "Price High-Low",
                "Tier Low-High", "Tier High-Low"
            };
                sortDropdown.AddOptions(sortOptions);
                sortDropdown.value = 0;
                sortDropdown.RefreshShownValue();

                Debug.Log($"[ShopLooby] ✅ Sort dropdown setup: {sortOptions.Count} options");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ShopLooby] ❌ Sort dropdown setup failed: {e.Message}");
            }
        }

        Debug.Log("[ShopLooby] 🎉 Filters and sorting setup completed!");
    }
    private bool AreDropdownsReady()
    {
        bool categoryReady = categoryFilter == null || categoryFilter.options.Count > 0;
        bool tierReady = tierFilter == null || tierFilter.options.Count > 0;
        bool sortReady = sortDropdown == null || sortDropdown.options.Count > 0;

        bool allReady = categoryReady && tierReady && sortReady;

      

        return allReady;
    }

    private void ApplyFiltersAndPagination()
    {
        // ✅ **ตรวจสอบว่า dropdown พร้อมหรือยัง**
        if (!AreDropdownsReady())
        {
            Debug.LogWarning("[ShopLooby] ⚠️ Dropdowns not ready, skipping filter application");
            return;
        }

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

        // Apply tier filter
        if (tierFilter != null && tierFilter.value > 0)
        {
            ItemTier selectedTier = (ItemTier)tierFilter.value;
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
        // ✅ **ใช้ currentShopContainer แทน beginJourneyContainer**
        if (currentShopContainer != null)
        {
            // ลบ items เก่า
            foreach (Transform child in currentShopContainer)
            {
                Destroy(child.gameObject);
            }
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
        // ✅ **โหลดข้อมูลตาม shop ปัจจุบัน**
        if (currentShopName == "Begin Journey")
        {
            LoadBeginJourneyItemsFromDatabase();
            allShopItems = new List<ShopItemData>(beginJourneyItems);
        }
        else if (currentShopName == "Legend Shop")
        {
            LoadLegendShopItemsFromDatabase();
            allShopItems = new List<ShopItemData>(legendShopItems);
        }

        ApplyFiltersAndPagination();
        Debug.Log($"[ShopLooby] ✅ {currentShopName} items refreshed");
    }
    public void RefreshLegendShop()
    {
        LoadLegendShopItemsFromDatabase();

        // ✅ **แสดงผลใหม่ถ้าอยู่ใน legend panel**
        if (legendShopPanel != null && legendShopPanel.activeSelf)
        {
            DisplayLegendShopItems();
        }
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
        price = item.BuyPrice;
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
