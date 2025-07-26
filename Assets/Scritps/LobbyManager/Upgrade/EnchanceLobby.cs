using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class EnchanceLobby : MonoBehaviour
{
    [Header("Main Controls")]
    public Button closeEnchanceLobby;
    public Button upgradeStatButton;

    [Header("Panels")]
    public GameObject enchancePanel;
    public GameObject upgradeStatPanel;

    [Header("💎 Currency Display")]
    public TextMeshProUGUI currentGoldText;
    public TextMeshProUGUI currentGemsText;

    [Header("🔨 Phase 1: Enchance Item Display")]
    public Transform enchanceInventoryContainer;    // Container สำหรับแสดง items ที่ enchance ได้
    public GameObject enchanceItemPrefab;           // Prefab สำหรับแสดง item (ใช้ shopItemPrefab ได้)
    public ScrollRect enchanceScrollRect;           // Scroll rect สำหรับ inventory

    [Header("🔨 Pagination")]
    public int itemsPerPage = 12;                   // จำนวน items ต่อหน้า
    public Button previousPageButton;               // ปุ่มหน้าก่อน
    public Button nextPageButton;                   // ปุ่มหน้าถัดไป
    public TextMeshProUGUI pageInfoText;            // แสดงข้อมูลหน้า

    [Header("🔨 Enhance Slot")]
    public Transform enhanceSlot;                   // ช่องสำหรับ item ที่จะ enhance
    public Image enhanceSlotIcon;
    public TextMeshProUGUI itemStatsText;
    public TextMeshProUGUI itemStatsTextlevel;
    // ไอคอนของ item ในช่อง enhance
    public Button enhanceSlotButton;                // ปุ่มของช่อง enhance (สำหรับถอดออก)
            // UI เมื่อมี item ในช่อง

    [Header("🔨 Remove Item Panel")]
    public GameObject removeItemPanel;              // Panel สำหรับถอด item จากช่อง enhance
    public Image removeItemIcon;                    // ไอคอนของ item ที่จะถอด
    public TextMeshProUGUI removeItemName;          // ชื่อ item ที่จะถอด
    public Button confirmRemoveButton;              // ปุ่มยืนยันการถอด
    public Button cancelRemoveButton;               // ปุ่มยกเลิก

    // Private variables
    private Character playerCharacter;
    private CurrencyManager currencyManager;
    private List<InventoryItem> enchanceableItems = new List<InventoryItem>();
    private int currentPage = 0;
    private InventoryItem currentEnhanceItem = null;  // Item ที่อยู่ในช่อง enhance
    private int currentEnhanceSlotIndex = -1;         // Slot index ของ item ในช่อง enhance

    #region Unity Lifecycle
    void Start()
    {
        // เชื่อม events
        SetupEventListeners();

        // หา systems
        FindPlayerCharacter();
        currencyManager = CurrencyManager.FindCurrencyManager();

        // Setup UI เริ่มต้น
        UpdateCurrencyDisplay();
        StartCoroutine(CurrencyUpdateLoop());

        // Setup enhance slot
        SetupEnhanceSlot();

        // โหลด enchanceable items
        RefreshEnchanceableItems();
    }

    void OnEnable()
    {
        UpdateCurrencyDisplay();

        // หา character ใหม่เมื่อเปิด panel
        if (playerCharacter == null)
        {
            FindPlayerCharacter();
        }

        // รีเฟรช items เมื่อเปิด panel
        RefreshEnchanceableItems();
    }

    void OnDestroy()
    {
        RemoveEventListeners();
        StopAllCoroutines();
    }
    #endregion

    #region Initialization
    private void SetupEventListeners()
    {
        // Main controls
        if (closeEnchanceLobby != null)
            closeEnchanceLobby.onClick.AddListener(CloseEnchanceLobby);
        if (upgradeStatButton != null)
            upgradeStatButton.onClick.AddListener(OpenUpgradeStatLobby);

        // Pagination
        if (previousPageButton != null)
            previousPageButton.onClick.AddListener(PreviousPage);
        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(NextPage);

        // Enhance slot
        if (enhanceSlotButton != null)
            enhanceSlotButton.onClick.AddListener(OnEnhanceSlotClicked);

        // Remove item panel
        if (confirmRemoveButton != null)
            confirmRemoveButton.onClick.AddListener(OnConfirmRemoveClicked);
        if (cancelRemoveButton != null)
            cancelRemoveButton.onClick.AddListener(OnCancelRemoveClicked);
    }

    private void RemoveEventListeners()
    {
        if (closeEnchanceLobby != null)
            closeEnchanceLobby.onClick.RemoveAllListeners();
        if (upgradeStatButton != null)
            upgradeStatButton.onClick.RemoveAllListeners();
        if (previousPageButton != null)
            previousPageButton.onClick.RemoveAllListeners();
        if (nextPageButton != null)
            nextPageButton.onClick.RemoveAllListeners();
        if (enhanceSlotButton != null)
            enhanceSlotButton.onClick.RemoveAllListeners();
        if (confirmRemoveButton != null)
            confirmRemoveButton.onClick.RemoveAllListeners();
        if (cancelRemoveButton != null)
            cancelRemoveButton.onClick.RemoveAllListeners();
    }

    private void FindPlayerCharacter()
    {
        // หาจาก tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerCharacter = playerObj.GetComponent<Character>();
            if (playerCharacter != null)
            {
                Debug.Log($"[EnchanceLobby] Found player character via tag: {playerCharacter.CharacterName}");
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
                Debug.Log($"[EnchanceLobby] Found player character via InputAuthority: {playerCharacter.CharacterName}");
                return;
            }
        }

        // หาจาก CombatUIManager
        CombatUIManager uiManager = FindObjectOfType<CombatUIManager>();
        if (uiManager != null && uiManager.localHero != null)
        {
            playerCharacter = uiManager.localHero;
            Debug.Log("[EnchanceLobby] Found character from CombatUIManager");
            return;
        }

        Debug.LogWarning("[EnchanceLobby] Player Character not found!");
    }

    private void SetupEnhanceSlot()
    {
        // เริ่มต้นเป็นช่องว่าง
        UpdateEnhanceSlotDisplay();
    }
    #endregion

    #region Currency Display
    private IEnumerator CurrencyUpdateLoop()
    {
        while (true)
        {
            UpdateCurrencyDisplay();
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void UpdateCurrencyDisplay()
    {
        try
        {
            if (currencyManager == null)
                currencyManager = CurrencyManager.FindCurrencyManager();

            if (currencyManager != null)
            {
                if (currentGoldText != null)
                    currentGoldText.text = $"{currencyManager.GetCurrentGold():N0}";
                if (currentGemsText != null)
                    currentGemsText.text = $"{currencyManager.GetCurrentGems():N0}";
            }
            else
            {
                if (currentGoldText != null)
                    currentGoldText.text = "Loading...";
                if (currentGemsText != null)
                    currentGemsText.text = "Loading...";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnchanceLobby] Error updating currency display: {e.Message}");
        }
    }
    #endregion

    #region Panel Management
    void CloseEnchanceLobby()
    {
        enchancePanel.SetActive(false);
        if (upgradeStatPanel != null)
            upgradeStatPanel.SetActive(true);
    }

    void OpenUpgradeStatLobby()
    {
        if (upgradeStatPanel != null)
            upgradeStatPanel.SetActive(true);
        CloseEnchanceLobby();
    }
    #endregion

    #region Enchanceable Items Management
    private void RefreshEnchanceableItems()
    {
        Debug.Log("[EnchanceLobby] Refreshing enchanceable items...");

        enchanceableItems.Clear();

        if (playerCharacter?.GetInventory() == null)
        {
            Debug.LogWarning("[EnchanceLobby] No inventory found!");
            DisplayEnchanceableItems();
            return;
        }

        Inventory playerInventory = playerCharacter.GetInventory();

        // หา items ที่สามารถ enhance ได้
        for (int i = 0; i < playerInventory.CurrentSlots; i++)
        {
            InventoryItem item = playerInventory.GetItem(i);

            // ตรวจสอบว่า item สามารถ enhance ได้หรือไม่
            if (item != null && !item.IsEmpty && item.itemData != null && CanEnhanceItem(item.itemData))
            {
                enchanceableItems.Add(item);
            }
        }

        Debug.Log($"[EnchanceLobby] Found {enchanceableItems.Count} enchanceable items");

        // Reset หน้าและแสดงผล
        currentPage = 0;
        UpdatePagination();
        DisplayEnchanceableItems();
    }

    private bool CanEnhanceItem(ItemData itemData)
    {
        // เฉพาะอุปกรณ์ที่สามารถ enhance ได้
        switch (itemData.ItemType)
        {
            case ItemType.Weapon:
            case ItemType.Head:
            case ItemType.Armor:
            case ItemType.Pants:
            case ItemType.Shoes:
            case ItemType.Rune:
            case ItemType.Potion:
            case ItemType.Material:
            case ItemType.Misc:
                return true;
            default:
                return false;
        }
    }

    private void DisplayEnchanceableItems()
    {
        if (enchanceInventoryContainer == null)
        {
            Debug.LogError("[EnchanceLobby] enchanceInventoryContainer is null!");
            return;
        }

        // ลบ items เก่า
        foreach (Transform child in enchanceInventoryContainer)
        {
            Destroy(child.gameObject);
        }

        if (enchanceableItems.Count == 0)
        {
            Debug.Log("[EnchanceLobby] No enchanceable items to display");
            return;
        }

        // คำนวณ items สำหรับหน้าปัจจุบัน
        int startIndex = currentPage * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, enchanceableItems.Count);

        // สร้าง UI สำหรับ items ในหน้าปัจจุบัน
        for (int i = startIndex; i < endIndex; i++)
        {
            CreateEnchanceItemUI(enchanceableItems[i]);
        }

        Debug.Log($"[EnchanceLobby] Displayed {endIndex - startIndex} items (page {currentPage + 1})");
    }

    private void CreateEnchanceItemUI(InventoryItem inventoryItem)
    {
        if (inventoryItem?.itemData == null)
        {
            Debug.LogError("[EnchanceLobby] Invalid inventory item!");
            return;
        }

        // ใช้ prefab ที่กำหนด หรือถ้าไม่มีให้ลองหา shopItemPrefab
        GameObject prefabToUse = enchanceItemPrefab;
        if (prefabToUse == null)
        {
            // ลองหา ShopLooby และใช้ shopItemPrefab ของมัน
            ShopLooby shopLooby = FindObjectOfType<ShopLooby>();
            if (shopLooby != null && shopLooby.shopItemPrefab != null)
            {
                prefabToUse = shopLooby.shopItemPrefab;
            }
        }

        if (prefabToUse == null)
        {
            Debug.LogError("[EnchanceLobby] No prefab available for enchance item UI!");
            return;
        }

        try
        {
            GameObject enchanceItemObj = Instantiate(prefabToUse, enchanceInventoryContainer);

            // ตั้งค่า UI ผ่าน ShopItemUI (ถ้ามี)
            ShopItemUI itemUI = enchanceItemObj.GetComponent<ShopItemUI>();
            if (itemUI != null)
            {
                // สร้าง ShopItemData สำหรับแสดงผล
                ShopItemData displayData = new ShopItemData(inventoryItem.itemData, 0, CurrencyType.Gold);
                itemUI.SetupShopItem(displayData, null);
            }
          

            // เพิ่ม Button listener สำหรับเลือก item
            Button itemButton = enchanceItemObj.GetComponent<Button>();
            if (itemButton == null)
                itemButton = enchanceItemObj.GetComponentInChildren<Button>();

            if (itemButton != null)
            {
                // สร้าง local copy เพื่อหลีกเลี่ยง reference issues
                InventoryItem localItem = new InventoryItem(inventoryItem.itemData, inventoryItem.stackCount, inventoryItem.slotIndex);

                itemButton.onClick.RemoveAllListeners();
                itemButton.onClick.AddListener(() => OnEnchanceItemClicked(localItem));
            }

            Debug.Log($"[EnchanceLobby] Created UI for {inventoryItem.itemData.ItemName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnchanceLobby] Error creating enchance item UI: {e.Message}");
        }
    }

   
    #endregion

    #region Enhance Slot Management
    public void OnEnchanceItemClicked(InventoryItem inventoryItem)
    {
        if (inventoryItem?.itemData == null)
        {
            Debug.LogError("[EnchanceLobby] Invalid item clicked!");
            return;
        }

        // ✅ ตรวจสอบว่าไอเทมสามารถ Enchant ได้จริงไหม
        if (!CanEnhanceItem(inventoryItem.itemData) || !inventoryItem.itemData.EnchantmentSettings?.canBeEnchanted == true)
        {
            Debug.Log("[EnchanceLobby] Item cannot be enchanted.");
            return;
        }

        // ตรวจสอบว่า item ยังอยู่ใน inventory หรือไม่
        if (playerCharacter?.GetInventory() != null)
        {
            Inventory inventory = playerCharacter.GetInventory();
            InventoryItem currentItem = inventory.GetItem(inventoryItem.slotIndex);

            if (currentItem?.itemData?.ItemId != inventoryItem.itemData.ItemId)
            {
                Debug.LogWarning("[EnchanceLobby] Item is no longer available!");
                RefreshEnchanceableItems();
                return;
            }
        }

        // ถ้ามี item ในช่อง enhance อยู่แล้ว ให้คืนกลับ inventory ก่อน
        if (currentEnhanceItem != null)
        {
            ReturnItemToInventory();
        }

        // ย้าย item ไปช่อง enhance
        MoveItemToEnhanceSlot(inventoryItem);
    }


    private void MoveItemToEnhanceSlot(InventoryItem inventoryItem)
    {
        if (playerCharacter?.GetInventory() == null)
        {
            Debug.LogError("[EnchanceLobby] No inventory found!");
            return;
        }

        Inventory inventory = playerCharacter.GetInventory();

        // ลบ item จาก inventory (เฉพาะ 1 ชิ้น)
        if (inventory.RemoveItem(inventoryItem.slotIndex, 1))
        {
            // เก็บข้อมูล item ที่ย้ายมา
            currentEnhanceItem = new InventoryItem(inventoryItem.itemData, 1, -1);
            currentEnhanceSlotIndex = inventoryItem.slotIndex;

            Debug.Log($"[EnchanceLobby] Moved {inventoryItem.itemData.ItemName} to enhance slot");

            // อัพเดท UI
            UpdateEnhanceSlotDisplay();
            RefreshEnchanceableItems(); // รีเฟรช list เพื่อ update จำนวน
        }
        else
        {
            Debug.LogError("[EnchanceLobby] Failed to remove item from inventory!");
        }
    }
    private void UpdateEnhanceSlotDisplay()
    {
        bool hasItem = currentEnhanceItem != null && !currentEnhanceItem.IsEmpty;

        if (hasItem)
        {
            if (enhanceSlotIcon != null)
            {
                enhanceSlotIcon.sprite = currentEnhanceItem.itemData.ItemIcon;
                enhanceSlotIcon.color = Color.white;
            }

            if (itemStatsText != null)
            {
                // ✅ แก้ให้ดึงจาก Stats
                string statsText = currentEnhanceItem.itemData.Stats.GetStatsDescription();

                // ✅ แก้ชื่อให้ตรงกับที่คุณใช้จริง
                var enchantData = currentEnhanceItem.enchantmentData;
                if (enchantData != null && enchantData.currentEnchantLevel > 0)
                {
                    statsText += $"\n\n🔨 Enchant Level: +{enchantData.currentEnchantLevel}";
                    if (enchantData.isDamaged)
                    {
                        statsText += " (Damaged)";
                    }
                }

                itemStatsText.text = string.IsNullOrEmpty(statsText) ? "" : statsText;
            }
            
        }
        else
        {
            if (enhanceSlotIcon != null)
            {
                enhanceSlotIcon.sprite = null;
                enhanceSlotIcon.color = Color.clear;
            }

            if (itemStatsText != null)
            {
                itemStatsText.text = "";
            } 
            if (itemStatsTextlevel != null)
            {
                itemStatsText.text = "";
            }
        }

        Debug.Log($"[EnchanceLobby] Updated enhance slot display - Has item: {hasItem}");
    }


    private void OnEnhanceSlotClicked()
    {
        if (currentEnhanceItem != null && !currentEnhanceItem.IsEmpty)
        {
            // แสดง remove panel
            ShowRemoveItemPanel();
        }
        else
        {
            Debug.Log("[EnchanceLobby] Enhance slot is empty");
        }
    }
    #endregion

    #region Remove Item Panel
    private void ShowRemoveItemPanel()
    {
        if (removeItemPanel == null || currentEnhanceItem == null)
        {
            Debug.LogError("[EnchanceLobby] Cannot show remove panel!");
            return;
        }

        removeItemPanel.SetActive(true);

        // ตั้งค่า UI
        if (removeItemIcon != null)
        {
            removeItemIcon.sprite = currentEnhanceItem.itemData.ItemIcon;
        }

        if (removeItemName != null)
        {
            removeItemName.text = $"Remove {currentEnhanceItem.itemData.ItemName}?";
            removeItemName.color = currentEnhanceItem.itemData.GetTierColor();
        }

        Debug.Log($"[EnchanceLobby] Showing remove panel for {currentEnhanceItem.itemData.ItemName}");
    }

    private void OnConfirmRemoveClicked()
    {
        if (currentEnhanceItem != null)
        {
            ReturnItemToInventory();
        }

        HideRemoveItemPanel();
    }

    private void OnCancelRemoveClicked()
    {
        HideRemoveItemPanel();
    }

    private void HideRemoveItemPanel()
    {
        if (removeItemPanel != null)
            removeItemPanel.SetActive(false);
    }

    private void ReturnItemToInventory()
    {
        if (currentEnhanceItem == null || playerCharacter?.GetInventory() == null)
        {
            Debug.LogError("[EnchanceLobby] Cannot return item to inventory!");
            return;
        }

        Inventory inventory = playerCharacter.GetInventory();

        // พยายามเพิ่ม item กลับ inventory
        if (inventory.AddItem(currentEnhanceItem.itemData, currentEnhanceItem.stackCount))
        {
            Debug.Log($"[EnchanceLobby] Returned {currentEnhanceItem.itemData.ItemName} to inventory");

            // ล้างข้อมูล enhance slot
            currentEnhanceItem = null;
            currentEnhanceSlotIndex = -1;

            // อัพเดท UI
            UpdateEnhanceSlotDisplay();
            RefreshEnchanceableItems();
        }
        else
        {
            Debug.LogError("[EnchanceLobby] Failed to return item to inventory - no space!");
            // TODO: แสดงข้อความ error ให้ผู้เล่น
        }
    }
    #endregion

    #region Pagination
    private void UpdatePagination()
    {
        int totalPages = Mathf.CeilToInt((float)enchanceableItems.Count / itemsPerPage);
        currentPage = Mathf.Clamp(currentPage, 0, Mathf.Max(0, totalPages - 1));

        // อัพเดทปุ่ม pagination
        if (previousPageButton != null)
            previousPageButton.interactable = currentPage > 0;
        if (nextPageButton != null)
            nextPageButton.interactable = currentPage < totalPages - 1;

        // อัพเดทข้อมูลหน้า
        if (pageInfoText != null)
            pageInfoText.text = $"Page {currentPage + 1} of {Mathf.Max(1, totalPages)}";
    }

    private void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            UpdatePagination();
            DisplayEnchanceableItems();
        }
    }

    private void NextPage()
    {
        int totalPages = Mathf.CeilToInt((float)enchanceableItems.Count / itemsPerPage);
        if (currentPage < totalPages - 1)
        {
            currentPage++;
            UpdatePagination();
            DisplayEnchanceableItems();
        }
    }
    #endregion

    #region Public Methods
    public void ForceRefreshItems()
    {
        RefreshEnchanceableItems();
    }

    public bool HasItemInEnhanceSlot()
    {
        return currentEnhanceItem != null && !currentEnhanceItem.IsEmpty;
    }

    public InventoryItem GetCurrentEnhanceItem()
    {
        return currentEnhanceItem;
    }
    #endregion
}