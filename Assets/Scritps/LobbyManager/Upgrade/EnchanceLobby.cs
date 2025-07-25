using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;

public class EnchanceLobby : MonoBehaviour
{
    [Header("Button")]
    public Button closeEnchanceLobby;
    public Button upgradeStatButton;

    [Header("Panel")]
    public GameObject enchancePanel;
    public GameObject upgradeStatPanel;

    [Header("💎 Currency Display")]
    public TextMeshProUGUI currentGoldText;
    public TextMeshProUGUI currentGemsText;

    [Header("🎒 Inventory Display")]
    public Transform inventoryContainer;        // Container สำหรับแสดง inventory items
    public GameObject inventoryItemPrefab;      // Prefab สำหรับแสดง inventory item (ใช้ ShopItemUI ได้)
    public ScrollRect inventoryScrollRect;      // Scroll rect สำหรับ inventory

    [Header("📄 Pagination")]
    public int itemsPerPage = 12;              // จำนวนไอเทมต่อหน้า
    public Button previousPageButton;          // ปุ่มหน้าก่อน
    public Button nextPageButton;              // ปุ่มหน้าถัดไป
    public TextMeshProUGUI pageInfoText;       // แสดงข้อมูลหน้า
    public TextMeshProUGUI itemCountText;      // แสดงจำนวนไอเทม

    [Header("🔍 Filter Options")]
    public TMP_Dropdown categoryFilter;        // Filter ตามประเภทไอเทม
    public Button refreshInventoryButton;      // ปุ่ม refresh inventory

    // Private variables
    private Character playerCharacter;
    private List<InventoryItem> allInventoryItems = new List<InventoryItem>();
    private List<InventoryItem> filteredItems = new List<InventoryItem>();
    private int currentPage = 0;

    void Start()
    {
        // Setup event listeners
        closeEnchanceLobby.onClick.AddListener(CloseEnchanceLobby);
        upgradeStatButton.onClick.AddListener(OpenUpgradeStatLobby);

        if (previousPageButton != null)
            previousPageButton.onClick.AddListener(PreviousPage);
        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(NextPage);
        if (refreshInventoryButton != null)
            refreshInventoryButton.onClick.AddListener(RefreshInventory);
        if (categoryFilter != null)
            categoryFilter.onValueChanged.AddListener(OnCategoryFilterChanged);

        // Setup initial state
        UpdateCurrencyDisplay();
        SetupCategoryFilter();

        // Start currency update loop
        StartCoroutine(CurrencyUpdateLoop());
    }

    void OnEnable()
    {
        // Refresh inventory when panel opens
        StartCoroutine(DelayedInventoryLoad());
    }

    private IEnumerator DelayedInventoryLoad()
    {
        // รอ 1 frame ให้ UI setup เสร็จ
        yield return null;

        FindPlayerCharacter();
        LoadInventoryItems();
    }

    private IEnumerator CurrencyUpdateLoop()
    {
        while (true)
        {
            UpdateCurrencyDisplay();
            yield return new WaitForSeconds(0.5f);
        }
    }

    #region Character & Inventory Management

    private void FindPlayerCharacter()
    {
        // ใช้วิธีเดียวกับ ShopLooby
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerCharacter = playerObj.GetComponent<Character>();
            if (playerCharacter != null)
            {
                Debug.Log($"[EnchanceLobby] ✅ Found player character: {playerCharacter.CharacterName}");
                return;
            }
        }

        // หาจาก HasInputAuthority
        Character[] allCharacters = FindObjectsOfType<Character>();
        foreach (Character character in allCharacters)
        {
            if (character.HasInputAuthority)
            {
                playerCharacter = character;
                Debug.Log($"[EnchanceLobby] ✅ Found player via InputAuthority: {playerCharacter.CharacterName}");
                return;
            }
        }

        // หาจาก CombatUIManager
        CombatUIManager uiManager = FindObjectOfType<CombatUIManager>();
        if (uiManager != null && uiManager.localHero != null)
        {
            playerCharacter = uiManager.localHero;
            Debug.Log("[EnchanceLobby] ✅ Found character from CombatUIManager");
            return;
        }

        Debug.LogWarning("[EnchanceLobby] ❌ Player Character not found!");
    }

    private void LoadInventoryItems()
    {
        allInventoryItems.Clear();

        if (playerCharacter?.GetInventory() == null)
        {
            Debug.LogWarning("[EnchanceLobby] No inventory found!");
            UpdateDisplay();
            return;
        }

        Inventory playerInventory = playerCharacter.GetInventory();

        // โหลดไอเทมทั้งหมดจาก inventory
        for (int i = 0; i < playerInventory.CurrentSlots; i++)
        {
            InventoryItem item = playerInventory.GetItem(i);
            if (item != null && !item.IsEmpty && item.itemData != null)
            {
                allInventoryItems.Add(item);
            }
        }

        Debug.Log($"[EnchanceLobby] Loaded {allInventoryItems.Count} items from inventory");

        // Apply filters และแสดงผล
        ApplyFiltersAndPagination();
    }

    #endregion

    #region UI Setup

    private void SetupCategoryFilter()
    {
        if (categoryFilter == null) return;

        categoryFilter.ClearOptions();
        List<string> categoryOptions = new List<string> { "All Items" };

        // เพิ่มประเภทไอเทมต่างๆ
        foreach (ItemType itemType in System.Enum.GetValues(typeof(ItemType)))
        {
            categoryOptions.Add(itemType.ToString());
        }

        categoryOptions.Add("Enchantable"); // เพิ่มตัวเลือกสำหรับไอเทมที่ enchant ได้

        categoryFilter.AddOptions(categoryOptions);
        categoryFilter.value = 0;

        Debug.Log($"[EnchanceLobby] Category filter setup with {categoryOptions.Count} options");
    }

    

    #endregion

    #region Filter & Pagination

    private void ApplyFiltersAndPagination()
    {
        // เริ่มจากไอเทมทั้งหมด
        filteredItems = new List<InventoryItem>(allInventoryItems);

        // Apply category filter
        if (categoryFilter != null && categoryFilter.value > 0)
        {
            if (categoryFilter.value <= System.Enum.GetValues(typeof(ItemType)).Length)
            {
                // Filter by ItemType
                ItemType selectedType = (ItemType)(categoryFilter.value - 1);
                filteredItems = filteredItems.Where(item => item.itemData.ItemType == selectedType).ToList();
            }
            else
            {
                // Filter by "Enchantable" items
                filteredItems = filteredItems.Where(item => CanItemBeEnchanted(item.itemData)).ToList();
            }
        }

        // Update pagination
        UpdatePagination();

        // Display current page
        DisplayCurrentPage();

        Debug.Log($"[EnchanceLobby] Filtered to {filteredItems.Count} items");
    }

    private bool CanItemBeEnchanted(ItemData itemData)
    {
        // กำหนดเงื่อนไขว่าไอเทมไหน enchant ได้
        return itemData.ItemType == ItemType.Weapon ||
               itemData.ItemType == ItemType.Armor ||
               itemData.ItemType == ItemType.Head ||
               itemData.ItemType == ItemType.Pants ||
               itemData.ItemType == ItemType.Shoes;
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
            itemCountText.text = $"Items: {filteredItems.Count}";
    }

    private void DisplayCurrentPage()
    {
        // Clear existing items
        if (inventoryContainer != null)
        {
            foreach (Transform child in inventoryContainer)
            {
                Destroy(child.gameObject);
            }
        }

        // Setup container if needed

        // Calculate items for current page
        int startIndex = currentPage * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, filteredItems.Count);

        // Create UI for current page items
        for (int i = startIndex; i < endIndex; i++)
        {
            CreateInventoryItemUI(filteredItems[i]);
        }

        Debug.Log($"[EnchanceLobby] Displayed items {startIndex}-{endIndex} of {filteredItems.Count}");
    }

    private void CreateInventoryItemUI(InventoryItem inventoryItem)
    {
        if (inventoryItem?.itemData == null || inventoryContainer == null) return;

        // ใช้ prefab (ถ้าไม่มีให้ใช้ ShopItemPrefab)
        GameObject prefabToUse = inventoryItemPrefab;
        if (prefabToUse == null)
        {
            Debug.LogWarning("[EnchanceLobby] No inventory item prefab assigned!");
            return;
        }

        GameObject itemObj = Instantiate(prefabToUse, inventoryContainer);

        // Setup ShopItemUI component
        ShopItemUI itemUI = itemObj.GetComponent<ShopItemUI>();
        if (itemUI == null)
        {
            itemUI = itemObj.AddComponent<ShopItemUI>();
        }

        // Create ShopItemData for display
        ShopItemData displayData = new ShopItemData(inventoryItem.itemData);
        displayData.price = 0; // ไม่แสดงราคาใน enchant lobby

        // Setup with null shop reference (เพราะไม่ใช่ shop)
        itemUI.SetupShopItem(displayData, null);

        // Override button click for enchant functionality
        Button itemButton = itemObj.GetComponent<Button>();
        if (itemButton == null)
        {
            itemButton = itemObj.GetComponentInChildren<Button>();
        }

        if (itemButton != null)
        {
            itemButton.onClick.RemoveAllListeners();
            itemButton.onClick.AddListener(() => OnInventoryItemClicked(inventoryItem));
        }

        // Hide price text since this is enchant lobby
        TextMeshProUGUI priceText = itemObj.GetComponentInChildren<TextMeshProUGUI>();
        Transform priceTransform = itemObj.transform.Find("PriceText");
        if (priceTransform != null)
        {
            priceTransform.gameObject.SetActive(false);
        }
    }

    private void OnInventoryItemClicked(InventoryItem inventoryItem)
    {
        if (inventoryItem?.itemData == null) return;

        Debug.Log($"[EnchanceLobby] Item clicked: {inventoryItem.itemData.ItemName}");

        // TODO: เปิด enchant detail panel หรือทำงานที่ต้องการ
        ShowEnchantOptions(inventoryItem);
    }

    private void ShowEnchantOptions(InventoryItem item)
    {
        // Placeholder for enchant functionality
        Debug.Log($"[EnchanceLobby] Showing enchant options for: {item.itemData.ItemName}");
        // TODO: เปิด panel สำหรับ enchant
    }

    #endregion

    #region Event Handlers

    private void OnCategoryFilterChanged(int filterIndex)
    {
        currentPage = 0; // Reset to first page
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

    private void RefreshInventory()
    {
        Debug.Log("[EnchanceLobby] Refreshing inventory...");
        FindPlayerCharacter();
        LoadInventoryItems();
    }

    private void UpdateDisplay()
    {
        ApplyFiltersAndPagination();
    }

    #endregion

    #region Original Methods

    void CloseEnchanceLobby()
    {
        enchancePanel.SetActive(false);
        upgradeStatPanel.SetActive(true);
    }

    void OpenUpgradeStatLobby()
    {
        upgradeStatPanel.SetActive(true);
        CloseEnchanceLobby();
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
            Debug.LogError($"[EnchanceLobby] ❌ Error updating currency display: {e.Message}");
        }
    }

    #endregion
}