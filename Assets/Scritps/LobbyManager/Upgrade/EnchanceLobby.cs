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
    public Transform enhanceSlot;
    public Image enhanceSlotIcon;
    public Image nextLevelPreviewIcon;              // 🆕 รูปไอเทมระดับถัดไป
    public TextMeshProUGUI itemStatsText;
    public TextMeshProUGUI itemStatsTextlevel;
    public TextMeshProUGUI nextLevelBonusText;      // 🆕 แสดง stat bonus ที่จะได้
    public TextMeshProUGUI enchantLevelText;        // 🆕 แสดง "+1" หรือระดับปัจจุบัน
    public TextMeshProUGUI enchantCostText;         // 🆕 แสดงราคาแยกต่างหาก
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
    [Header("🧪 Phase 2: Enchantment Materials")]
    public GameObject materialModePanel;            // Panel สำหรับโหมดเลือกวัตถุดิบ
    public Transform materialSlotsContainer;        // Container สำหรับ 10 ช่องวัตถุดิบ
    public GameObject materialSlotPrefab;           // Prefab สำหรับช่องวัตถุดิบ
    public int maxMaterialSlots = 10;               // จำนวนช่องวัตถุดิบสูงสุด

    // 🧪 เพิ่มใน Private variables
    // Material system variables
    private bool isMaterialMode = false;              // โหมดเลือกวัตถุดิบ
    private List<InventoryItem> materialSlots = new List<InventoryItem>(); // วัตถุดิบในช่อง
    private List<GameObject> materialSlotUIs = new List<GameObject>();     // UI ของช่องวัตถุดิบ
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
        SetupMaterialSlots();

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
    private void SetupMaterialSlots()
    {
        // สร้างช่องวัตถุดิบ 10 ช่อง
        materialSlots.Clear();
        materialSlotUIs.Clear();

        if (materialSlotsContainer == null || materialSlotPrefab == null)
        {
            Debug.LogWarning("[EnchanceLobby] Material slots container or prefab not assigned!");
            return;
        }

        for (int i = 0; i < maxMaterialSlots; i++)
        {
            // สร้าง slot data
            materialSlots.Add(null);

            // สร้าง UI
            GameObject slotUI = Instantiate(materialSlotPrefab, materialSlotsContainer);
            materialSlotUIs.Add(slotUI);

            // Setup button สำหรับ slot
            Button slotButton = slotUI.GetComponent<Button>();
            if (slotButton == null)
                slotButton = slotUI.GetComponentInChildren<Button>();

            if (slotButton != null)
            {
                int slotIndex = i; // Local copy สำหรับ closure
                slotButton.onClick.RemoveAllListeners();
                slotButton.onClick.AddListener(() => OnMaterialSlotClicked(slotIndex));
            }
        }

        // เริ่มต้นซ่อนโหมดวัตถุดิบ
        SetMaterialMode(false);
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
        if (isMaterialMode)
        {
            AddMaterialToSlot(inventoryItem);
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

            // 🧪 เข้าสู่โหมดวัตถุดิบ
            SetMaterialMode(true);
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
            // แสดงรูปไอเทมปัจจุบัน
            if (enhanceSlotIcon != null)
            {
                enhanceSlotIcon.sprite = currentEnhanceItem.itemData.ItemIcon;
                enhanceSlotIcon.color = Color.white;
            }

            // 🆕 แสดงรูปไอเทมระดับถัดไป (เหมือนกัน แต่อาจจะใส่ effect ได้)
            if (nextLevelPreviewIcon != null)
            {
                nextLevelPreviewIcon.sprite = currentEnhanceItem.itemData.ItemIcon;
                nextLevelPreviewIcon.color = Color.white;
                nextLevelPreviewIcon.gameObject.SetActive(true);
            }

            // แสดง stats ปัจจุบัน
            if (itemStatsText != null)
            {
                string statsText = currentEnhanceItem.itemData.Stats.GetStatsDescription();

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

            // 🆕 แสดงระดับ Enchant ปัจจุบัน และถัดไป
            int currentLevel = GetCurrentItemEnchantLevel();
            int nextLevel = currentLevel + 1;

            if (enchantLevelText != null)
            {
                if (currentLevel <= 0)
                {
                    enchantLevelText.text = $"+{nextLevel}";
                }
                else
                {
                    enchantLevelText.text = $"+{nextLevel}";
                }
                enchantLevelText.color = Color.cyan;
            }

            // 🆕 แสดง stat bonus ที่จะได้จากการ enchant ระดับถัดไป
            if (nextLevelBonusText != null)
            {
                string bonusText = GetNextLevelBonusPreview(nextLevel);
                nextLevelBonusText.text = bonusText;
                nextLevelBonusText.color = Color.red;
            }

            // 🆕 แสดงราคาแยกต่างหาก
            if (enchantCostText != null)
            {
                long enchantCost = GetEnchantCost(nextLevel);
                enchantCostText.text = $"💰 Cost: {enchantCost:N0} Gold";
                enchantCostText.color = Color.yellow;
            }
        }
        else
        {
            // ไม่มีไอเทม - ซ่อน/ล้าง UI ทั้งหมด
            if (enhanceSlotIcon != null)
            {
                enhanceSlotIcon.sprite = null;
                enhanceSlotIcon.color = Color.clear;
            }

            if (nextLevelPreviewIcon != null)
            {
                nextLevelPreviewIcon.sprite = null;
                nextLevelPreviewIcon.color = Color.clear;
                nextLevelPreviewIcon.gameObject.SetActive(false);
            }

            if (itemStatsText != null)
            {
                itemStatsText.text = "";
            }

            if (itemStatsTextlevel != null)
            {
                itemStatsTextlevel.text = "";
            }

            if (nextLevelBonusText != null)
            {
                nextLevelBonusText.text = "";
            }

            if (enchantLevelText != null)
            {
                enchantLevelText.text = "";
            }

            if (enchantCostText != null)
            {
                enchantCostText.text = "";
            }
        }

        Debug.Log($"[EnchanceLobby] Updated enhance slot display - Has item: {hasItem}");
    }

    // ======== เพิ่ม Helper Methods ========

    /// <summary>
    /// ดึงระดับ Enchant ปัจจุบันของไอเทมในช่อง enhance
    /// </summary>
    private int GetCurrentItemEnchantLevel()
    {
        if (currentEnhanceItem?.enchantmentData != null)
        {
            return currentEnhanceItem.enchantmentData.currentEnchantLevel;
        }
        return 0;
    }

    /// <summary>
    /// แสดง preview ของ stat bonus ที่จะได้จากระดับถัดไป
    /// </summary>
    private string GetNextLevelBonusPreview(int nextLevel)
    {
        if (currentEnhanceItem?.itemData?.EnchantmentSettings == null)
            return "No bonus data available";

        var enchantSettings = currentEnhanceItem.itemData.EnchantmentSettings;

        // ตรวจสอบว่าสามารถ enchant ไประดับถัดไปได้หรือไม่
        if (!enchantSettings.CanEnchantToLevel(nextLevel))
        {
            return $"Max enchant level reached ({enchantSettings.maxEnchantLevel})";
        }

        // ดึงข้อมูล bonus สำหรับระดับถัดไป
        var bonusData = enchantSettings.GetBonusDataForLevel(nextLevel);
        if (bonusData == null || !bonusData.HasAnyBonus())
        {
            // Debug เพื่อดูว่ามี bonus data หรือไม่
            Debug.LogWarning($"[EnchanceLobby] No bonus data found for level {nextLevel}. Available bonuses: {enchantSettings.enchantmentBonuses.Count}");

            // แสดงรายการ bonus levels ที่มี
            string availableLevels = "";
            foreach (var bonus in enchantSettings.enchantmentBonuses)
            {
                availableLevels += bonus.enchantLevel + ", ";
            }
            Debug.Log($"[EnchanceLobby] Available bonus levels: {availableLevels}");

            return $"No bonus configured for level {nextLevel}";
        }

        // สร้างข้อความแสดง bonus (ไม่รวมราคา)
        string previewText = $"\n";
        previewText += bonusData.GetBonusDescription();

        return previewText;
    }

    private long GetEnchantCost(int nextLevel)
    {
        if (currentEnhanceItem?.itemData?.EnchantmentSettings == null)
            return 0;

        var enchantSettings = currentEnhanceItem.itemData.EnchantmentSettings;
        int currentLevel = GetCurrentItemEnchantLevel();

        return enchantSettings.GetEnchantCost(currentLevel, nextLevel);
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

            // 🧪 ออกจากโหมดวัตถุดิบ
            SetMaterialMode(false);
            ClearAllMaterialSlots();
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
    #region Material System Methods

    /// <summary>
    /// เปลี่ยนโหมดระหว่างการเลือกไอเทม enchant และการเลือกวัตถุดิบ
    /// </summary>
    private void SetMaterialMode(bool enableMaterialMode)
    {
        isMaterialMode = enableMaterialMode;

        // แสดง/ซ่อน panel วัตถุดิบ
        if (materialModePanel != null)
        {
            materialModePanel.SetActive(enableMaterialMode);
        }

        Debug.Log($"[EnchanceLobby] Material mode: {(enableMaterialMode ? "ON" : "OFF")}");
    }

    /// <summary>
    /// เพิ่มวัตถุดิบลงในช่องว่าง
    /// </summary>
    private void AddMaterialToSlot(InventoryItem inventoryItem)
    {
        // หาช่องว่าง
        int emptySlot = FindEmptyMaterialSlot();
        if (emptySlot == -1)
        {
            Debug.LogWarning("[EnchanceLobby] No empty material slots available!");
            return;
        }

        // ตรวจสอบว่า item ยังอยู่ใน inventory หรือไม่
        if (playerCharacter?.GetInventory() != null)
        {
            Inventory inventory = playerCharacter.GetInventory();
            InventoryItem currentItem = inventory.GetItem(inventoryItem.slotIndex);

            if (currentItem?.itemData?.ItemId != inventoryItem.itemData.ItemId)
            {
                Debug.LogWarning("[EnchanceLobby] Material item is no longer available!");
                RefreshEnchanceableItems();
                return;
            }

            // ลบ item จาก inventory (เฉพาะ 1 ชิ้น)
            if (inventory.RemoveItem(inventoryItem.slotIndex, 1))
            {
                // เก็บ item ในช่องวัตถุดิบ
                materialSlots[emptySlot] = new InventoryItem(inventoryItem.itemData, 1, -1);

                Debug.Log($"[EnchanceLobby] Added {inventoryItem.itemData.ItemName} to material slot {emptySlot}");

                // อัพเดท UI
                UpdateMaterialSlotDisplay(emptySlot);
                RefreshEnchanceableItems();
            }
            else
            {
                Debug.LogError("[EnchanceLobby] Failed to remove material from inventory!");
            }
        }
    }

    /// <summary>
    /// หาช่องวัตถุดิบที่ว่าง
    /// </summary>
    private int FindEmptyMaterialSlot()
    {
        for (int i = 0; i < materialSlots.Count; i++)
        {
            if (materialSlots[i] == null || materialSlots[i].IsEmpty)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// เมื่อกดช่องวัตถุดิบ (เพื่อถอดออก)
    /// </summary>
    private void OnMaterialSlotClicked(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= materialSlots.Count)
            return;

        InventoryItem materialItem = materialSlots[slotIndex];
        if (materialItem == null || materialItem.IsEmpty)
            return;

        // คืน item กลับ inventory
        if (playerCharacter?.GetInventory() != null)
        {
            Inventory inventory = playerCharacter.GetInventory();

            if (inventory.AddItem(materialItem.itemData, materialItem.stackCount))
            {
                Debug.Log($"[EnchanceLobby] Returned {materialItem.itemData.ItemName} from material slot {slotIndex}");

                // ล้างช่อง
                materialSlots[slotIndex] = null;

                // อัพเดท UI ช่องวัตถุดิบ
                UpdateMaterialSlotDisplay(slotIndex);

                // 🔄 Refresh inventory list เหมือนตอนเปิดหน้าหรือใส่ item
                RefreshEnchanceableItems();

                Debug.Log("[EnchanceLobby] Refreshed inventory after removing material");
            }
            else
            {
                Debug.LogError("[EnchanceLobby] Failed to return material to inventory - no space!");
            }
        }
    }

    /// <summary>
    /// อัพเดท UI ของช่องวัตถุดิบ
    /// </summary>
    private void UpdateMaterialSlotDisplay(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= materialSlotUIs.Count)
            return;

        GameObject slotUI = materialSlotUIs[slotIndex];
        InventoryItem materialItem = materialSlots[slotIndex];

        // หา Image component สำหรับแสดงไอคอน
        Image slotIcon = slotUI.GetComponent<Image>();
        if (slotIcon == null)
            slotIcon = slotUI.GetComponentInChildren<Image>();

        if (materialItem != null && !materialItem.IsEmpty)
        {
            // มีวัตถุดิบ - แสดงไอคอน
            if (slotIcon != null)
            {
                slotIcon.sprite = materialItem.itemData.ItemIcon;
                slotIcon.color = Color.white;
            }
        }
        else
        {
            // ไม่มีวัตถุดิบ - ล้างไอคอน
            if (slotIcon != null)
            {
                slotIcon.sprite = null;
                slotIcon.color = Color.clear;
            }
        }
    }

    /// <summary>
    /// ล้างวัตถุดิบทั้งหมด
    /// </summary>
    private void ClearAllMaterialSlots()
    {
        bool anyItemReturned = false;

        for (int i = 0; i < materialSlots.Count; i++)
        {
            if (materialSlots[i] != null && !materialSlots[i].IsEmpty)
            {
                // คืน item กลับ inventory
                if (playerCharacter?.GetInventory() != null)
                {
                    Inventory inventory = playerCharacter.GetInventory();
                    if (inventory.AddItem(materialSlots[i].itemData, materialSlots[i].stackCount))
                    {
                        anyItemReturned = true;
                        Debug.Log($"[EnchanceLobby] Returned {materialSlots[i].itemData.ItemName} from slot {i}");
                    }
                }
            }
            materialSlots[i] = null;
            UpdateMaterialSlotDisplay(i);
        }

        // 🔄 Refresh inventory หากมีการคืน item
        if (anyItemReturned)
        {
            RefreshEnchanceableItems();
            Debug.Log("[EnchanceLobby] Refreshed inventory after clearing all materials");
        }

        Debug.Log("[EnchanceLobby] Cleared all material slots");
    }

    #endregion

}