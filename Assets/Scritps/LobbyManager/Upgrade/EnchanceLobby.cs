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

    [Header("💎 Currency Display")]
    public TextMeshProUGUI currentGoldText;
    public TextMeshProUGUI currentGemsText;

    [Header("🔨 Phase 1: Enchance Item Display")]
    public Transform enchanceInventoryContainer;    // Container สำหรับแสดง items ที่ enchance ได้
    public GameObject enchanceItemPrefab;           // Prefab สำหรับแสดง item (ใช้ shopItemPrefab ได้)
    public ScrollRect enchanceScrollRect;           // Scroll rect สำหรับ inventoryf

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

    [Header("📊 Success Rate Display")]
    public Slider successRateSlider;                // Slider แสดงเปอร์เซ็นต์
    public TextMeshProUGUI successRateText;         // Text แสดงเปอร์เซ็นต์ตัวเลข
    public TextMeshProUGUI materialCountText;       // Text แสดงจำนวนวัตถุดิบ
    [Header("🔨 Enchant Process")]
    public Button performEnchantButton;          // ปุ่มกด Enchant
    public TextMeshProUGUI enchantButtonText;    // Text บนปุ่ม Enchant

    [Header("🎉 Success Panel")]
    public GameObject enchantSuccessPanel;       // Panel แสดงผลสำเร็จ
    public Image successItemIcon;               // ไอคอนไอเทมที่สำเร็จ
    public TextMeshProUGUI successItemName;     // ชื่อไอเทมที่สำเร็จ
    public TextMeshProUGUI successItemStats;    // Stats ของไอเทมใหม่
    public Button confirmSuccessButton;         // ปุ่ม Confirm ใส่กระเป๋า

    [Header("💥 Failure Panel")]
    public GameObject enchantFailurePanel;      // Panel แสดงผลล้มเหลว
    public Image failureItemIcon;              // ไอคอนไอเทมที่เสียหาย
    public TextMeshProUGUI failureItemName;    // ชื่อไอเทมที่เสียหาย
    public TextMeshProUGUI failureDescription; // คำอธิบายความเสียหาย
    public Button repairButton;                // ปุ่ม Repair
    public Button cancelFailureButton;         // ปุ่ม Cancel
    public TextMeshProUGUI repairCostText;     // ราคาซ่อม

    // Private variables for enchant result
    private ItemData enchantResultItem = null;     // ไอเทมผลลัพธ์
    private ItemData failedItem = null;           // ไอเทมที่ล้มเหลว
    private int originalEnchantLevel = 0;         // ระดับเดิมก่อนล้มเหลว
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
       

        // Pagination
        if (previousPageButton != null)
            previousPageButton.onClick.AddListener(PreviousPage);
        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(NextPage);

        // Enhance slot
        if (enhanceSlotButton != null)
            enhanceSlotButton.onClick.AddListener(OnEnhanceSlotClicked);
        if (performEnchantButton != null)
            performEnchantButton.onClick.AddListener(OnPerformEnchantClicked);
        if (confirmSuccessButton != null)
            confirmSuccessButton.onClick.AddListener(OnConfirmSuccessClicked);
        if (repairButton != null)
            repairButton.onClick.AddListener(OnRepairClicked);
        if (cancelFailureButton != null)
            cancelFailureButton.onClick.AddListener(OnCancelFailureClicked);
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
        if (performEnchantButton != null)
            performEnchantButton.onClick.RemoveAllListeners();
        if (confirmSuccessButton != null)
            confirmSuccessButton.onClick.RemoveAllListeners();
        if (repairButton != null)
            repairButton.onClick.RemoveAllListeners();
        if (cancelFailureButton != null)
            cancelFailureButton.onClick.RemoveAllListeners();
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
    private void CloseEnchanceLobby()
    {
        // ⭐ ถ้ามี item ในช่อง enhance ให้เอากลับเข้ากระเป๋าก่อน
        if (currentEnhanceItem != null)
        {
            ReturnItemToInventory();
        }

        // ปิด panel
        if (enchancePanel != null)
        {
            enchancePanel.SetActive(false);
        }
    }


    #endregion

    #region Enchanceable Items Management
    private void RefreshEnchanceableItems()
    {
        Debug.Log("[EnchanceLobby] === REFRESHING ENCHANCEABLE ITEMS ===");

        int previousCount = enchanceableItems.Count;
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

        Debug.Log($"[EnchanceLobby] Found {enchanceableItems.Count} enchanceable items (was {previousCount})");

        // แสดงรายการ items ที่หาได้
        for (int i = 0; i < enchanceableItems.Count && i < 5; i++) // แสดงแค่ 5 อันแรก
        {
            var item = enchanceableItems[i];
            Debug.Log($"  {i + 1}. {item.itemData.ItemName} x{item.stackCount} (slot {item.slotIndex})");
        }

        if (enchanceableItems.Count > 5)
        {
            Debug.Log($"  ... and {enchanceableItems.Count - 5} more items");
        }

        // Reset หน้าและแสดงผล
        currentPage = 0;
        UpdatePagination();
        DisplayEnchanceableItems();

        Debug.Log("[EnchanceLobby] ✅ Refresh complete");
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

    }

    // ======== เพิ่ม Helper Methods ========

    /// <summary>
    /// ดึงระดับ Enchant ปัจจุบันของไอเทมในช่อง enhance
    /// </summary>
    private int GetCurrentItemEnchantLevel()
    {
        if (currentEnhanceItem?.itemData == null) return 0;

        return GetEnchantLevelFromId(currentEnhanceItem.itemData.ItemId);
    }

    /// <summary>
    /// แสดง preview ของ stat bonus ที่จะได้จากระดับถัดไป
    /// </summary>
    private string GetNextLevelBonusPreview(int nextLevel)
    {
        if (currentEnhanceItem?.itemData?.EnchantmentSettings == null)
            return "No data available";

        var enchantSettings = currentEnhanceItem.itemData.EnchantmentSettings;

        // ตรวจสอบว่าสามารถ enchant ไประดับถัดไปได้หรือไม่
        if (!enchantSettings.CanEnchantToLevel(nextLevel))
        {
            return $"Max enchant level reached ({enchantSettings.maxEnchantLevel})";
        }

        // สร้าง Item ID ของ item ที่จะได้หลังจาก enchant
        string targetItemId = GetEnchantedItemId(currentEnhanceItem.itemData.ItemId, nextLevel);

        // หา ItemData ของ item ระดับถัดไป
        ItemData targetItemData = GetItemDataById(targetItemId);

        if (targetItemData == null)
        {
            return $"Item +{nextLevel} not found in database";
        }

        // แสดง stats ของ item ใหม่ตรงๆ
        if (!targetItemData.Stats.HasAnyStats())
        {
            return "No stats available";
        }

        // คืนค่า stats description ของ item ใหม่
        return targetItemData.Stats.GetStatsDescription();
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
        }
    }
    #endregion

    #region Remove Item Panel
    private void ShowRemoveItemPanel()
    {
        if (removeItemPanel == null || currentEnhanceItem == null)
        {
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
            removeItemName.color = currentEnhanceItem.itemData.GetTierColor();
        }

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
            return;
        }


        Inventory inventory = playerCharacter.GetInventory();

        // พยายามเพิ่ม item กลับ inventory
        if (inventory.AddItem(currentEnhanceItem.itemData, currentEnhanceItem.stackCount))
        {

            // ล้างข้อมูล enhance slot
            string itemName = currentEnhanceItem.itemData.ItemName;
            currentEnhanceItem = null;
            currentEnhanceSlotIndex = -1;

            // อัพเดท UI
            UpdateEnhanceSlotDisplay();

            // ✅ Force refresh inventory ทันที
            RefreshEnchanceableItems();

            // 🧪 ออกจากโหมดวัตถุดิบและคืนวัตถุดิบกลับกระเป๋า
            SetMaterialMode(false);
            ReturnMaterialSlotsToInventory(); // ✅ เปลี่ยนจาก ClearAllMaterialSlots()

            // ✅ Force save inventory ทันที
            inventory.AutoSaveInventoryData($"ReturnItem-{itemName}");
        }
        else
        {
            // TODO: แสดงข้อความ error ให้ผู้เล่น
        }
    }
    #endregion
   

  

    // ============= เพิ่ม Context Menu สำหรับ Debug =============
    

    
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


        // 📊 อัพเดท success rate เมื่อเปลี่ยนโหมด
        UpdateSuccessRateDisplay();
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
            return;
        }

        // ตรวจสอบว่า item ยังอยู่ใน inventory หรือไม่
        if (playerCharacter?.GetInventory() != null)
        {
            Inventory inventory = playerCharacter.GetInventory();
            InventoryItem currentItem = inventory.GetItem(inventoryItem.slotIndex);

            if (currentItem?.itemData?.ItemId != inventoryItem.itemData.ItemId)
            {
                RefreshEnchanceableItems();
                return;
            }


            // ลบ item จาก inventory (เฉพาะ 1 ชิ้น)
            if (inventory.RemoveItem(inventoryItem.slotIndex, 1))
            {
                // เก็บ item ในช่องวัตถุดิบ
                materialSlots[emptySlot] = new InventoryItem(inventoryItem.itemData, 1, -1);


                // อัพเดท UI
                UpdateMaterialSlotDisplay(emptySlot);

                // ✅ Force refresh inventory list ทันที
                RefreshEnchanceableItems();

                // ✅ Force save inventory ทันที
                inventory.AutoSaveInventoryData($"AddMaterial-{inventoryItem.itemData.ItemName}");

                // 📊 อัพเดท success rate
                UpdateSuccessRateDisplay();
            }
            else
            {
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
        {
            return;
        }


        // คืน item กลับ inventory
        if (playerCharacter?.GetInventory() != null)
        {
            Inventory inventory = playerCharacter.GetInventory();

            if (inventory.AddItem(materialItem.itemData, materialItem.stackCount))
            {

                // ล้างช่อง
                materialSlots[slotIndex] = null;

                // อัพเดท UI ช่องวัตถุดิบ
                UpdateMaterialSlotDisplay(slotIndex);

                // ✅ Force refresh inventory list ทันที
                RefreshEnchanceableItems();

                // ✅ Force save inventory ทันที
                inventory.AutoSaveInventoryData($"RemoveMaterial-{materialItem.itemData.ItemName}");

                // 📊 อัพเดท success rate
                UpdateSuccessRateDisplay();
            }
            else
            {
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
        // ✅ เปลี่ยนพฤติกรรม: คืนของกลับกระเป๋าแทนการทำลาย
        ReturnMaterialSlotsToInventory();
    }
    private void ConsumeMaterialSlots()
    {

        List<string> consumedItems = new List<string>();
        int totalConsumed = 0;

        for (int i = 0; i < materialSlots.Count; i++)
        {
            if (materialSlots[i] != null && !materialSlots[i].IsEmpty)
            {
                // เก็บข้อมูลสำหรับ log
                consumedItems.Add($"{materialSlots[i].itemData.ItemName} x{materialSlots[i].stackCount}");
                totalConsumed++;

            }

            // ล้างช่องวัตถุดิบ (ไม่คืนกลับกระเป๋า)
            materialSlots[i] = null;
            UpdateMaterialSlotDisplay(i);
        }


        // 📊 อัพเดท success rate (จะเป็น 0 เพราะไม่มีวัตถุดิบแล้ว)
        UpdateSuccessRateDisplay();
    }
    private void ReturnMaterialSlotsToInventory()
    {

        bool anyItemReturned = false;
        List<string> returnedItems = new List<string>();

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
                        returnedItems.Add($"{materialSlots[i].itemData.ItemName} x{materialSlots[i].stackCount}");
                    }
                    else
                    {
                    }
                }
            }
            materialSlots[i] = null;
            UpdateMaterialSlotDisplay(i);
        }

        // ✅ Force refresh inventory หากมีการคืน item
        if (anyItemReturned)
        {
            RefreshEnchanceableItems();

            // ✅ Force save inventory ทันที
            if (playerCharacter?.GetInventory() != null)
            {
                playerCharacter.GetInventory().AutoSaveInventoryData($"ReturnMaterials-{returnedItems.Count}items");
            }
        }

        // 📊 อัพเดท success rate
        UpdateSuccessRateDisplay();

    }
    /// <summary>
    /// 📊 อัพเดท Success Rate Display
    /// </summary>
    private void UpdateSuccessRateDisplay()
    {
        UpdateEnchantButton();

        // ถ้าไม่มีไอเทมใน enhance slot หรือไม่ได้อยู่ในโหมดวัตถุดิบ
        if (currentEnhanceItem == null || !isMaterialMode)
        {
            // ซ่อน/ล้าง success rate UI
            if (successRateSlider != null)
            {
                successRateSlider.value = 0f;
                successRateSlider.gameObject.SetActive(false);
            }
            if (successRateText != null)
            {
                successRateText.text = "";
            }
            if (materialCountText != null)
            {
                materialCountText.text = "";
            }
            return;
        }

        // แสดง success rate UI
        if (successRateSlider != null)
        {
            successRateSlider.gameObject.SetActive(true);
        }

        // นับจำนวนวัตถุดิบ
        int materialCount = GetMaterialCount();

        // คำนวณ success rate
        float successRate = CalculateSuccessRate();

        // อัพเดท Slider (0-100)
        if (successRateSlider != null)
        {
            successRateSlider.minValue = 0f;
            successRateSlider.maxValue = 100f;
            successRateSlider.value = successRate;
        }

        // อัพเดท Text เปอร์เซ็นต์
        if (successRateText != null)
        {
            successRateText.text = $"{successRate:F1}%";

            // เปลี่ยนสีตามเปอร์เซ็นต์
            if (successRate >= 75f)
                successRateText.color = Color.green;
            else if (successRate >= 50f)
                successRateText.color = Color.yellow;
            else if (successRate >= 25f)
                successRateText.color = Color.cyan;
            else
                successRateText.color = Color.red;
        }

        // อัพเดท Text จำนวนวัตถุดิบ
        if (materialCountText != null)
        {
            materialCountText.text = $"Materials: {materialCount}/{maxMaterialSlots}";
            materialCountText.color = materialCount > 0 ? Color.white : Color.gray;
        }

        Debug.Log($"[EnchanceLobby] Success rate updated: {successRate:F1}% with {materialCount} materials");
    }

    /// <summary>
    /// 📊 นับจำนวนวัตถุดิบที่มี
    /// </summary>
    private int GetMaterialCount()
    {
        int count = 0;
        foreach (var material in materialSlots)
        {
            if (material != null && !material.IsEmpty)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// 📊 คำนวณ Success Rate จากวัตถุดิบ
    /// </summary>
    private float CalculateSuccessRate()
    {
        if (currentEnhanceItem?.itemData == null)
            return 0f;

        // รวบรวมวัตถุดิบที่ไม่ว่าง
        List<ItemData> materials = new List<ItemData>();
        foreach (var materialSlot in materialSlots)
        {
            if (materialSlot != null && !materialSlot.IsEmpty && materialSlot.itemData != null)
            {
                materials.Add(materialSlot.itemData);
            }
        }

        // ใช้ method ที่มีอยู่แล้วใน ItemData
        float totalRate = currentEnhanceItem.itemData.GetEnchantSuccessRate(materials);

        return totalRate;
    }

    #endregion
    private void UpdateEnchantButton()
    {
        bool hasItemInSlot = currentEnhanceItem != null && !currentEnhanceItem.IsEmpty;
        bool canEnchant = CanPerformEnchant();

        if (performEnchantButton != null)
        {
            // แสดงปุ่มเมื่อมีไอเทมในช่อง enhance
            performEnchantButton.gameObject.SetActive(hasItemInSlot);
            performEnchantButton.interactable = canEnchant;
        }

        if (enchantButtonText != null && hasItemInSlot)
        {
            if (canEnchant)
            {
                int nextLevel = GetCurrentItemEnchantLevel() + 1;
                long cost = GetEnchantCost(nextLevel);
                float successRate = CalculateSuccessRate();

                enchantButtonText.text = $"Enchant to +{nextLevel}\n{successRate:F1}% Success\n💰 {cost:N0}";
                enchantButtonText.color = Color.white;
            }
            else
            {
                // แสดงสาเหตุที่กดไม่ได้
                string reason = GetCannotEnchantReason();
                enchantButtonText.text = reason;
                enchantButtonText.color = Color.red;
            }
        }
    }
    private string GetCannotEnchantReason()
    {
        if (currentEnhanceItem == null || currentEnhanceItem.IsEmpty)
            return "No item in slot";

        int nextLevel = GetCurrentItemEnchantLevel() + 1;
        long cost = GetEnchantCost(nextLevel);

        // ตรวจสอบเงิน
        if (currencyManager == null || !currencyManager.HasEnoughGold(cost))
            return $"Need 💰 {cost:N0} Gold";

        // ตรวจสอบระดับสูงสุด
        if (currentEnhanceItem?.itemData?.EnchantmentSettings != null)
        {
            if (!currentEnhanceItem.itemData.EnchantmentSettings.CanEnchantToLevel(nextLevel))
                return $"Max level reached";
        }

        // ถ้าต้องการบังคับใส่วัตถุดิบ
        if (GetMaterialCount() == 0)
            return "Add materials for\nbetter success rate";

        return "Ready to Enchant!";
    }

    private bool CanPerformEnchant()
    {
        if (currentEnhanceItem == null || !isMaterialMode) return false;

        // ต้องมีวัตถุดิบอย่างน้อย 1 ชิ้น
        if (GetMaterialCount() == 0) return false;

        // ตรวจสอบเงิน
        int nextLevel = GetCurrentItemEnchantLevel() + 1;
        long cost = GetEnchantCost(nextLevel);

        if (currencyManager == null || !currencyManager.HasEnoughGold(cost))
            return false;

        // ตรวจสอบระดับสูงสุด
        if (currentEnhanceItem?.itemData?.EnchantmentSettings != null)
        {
            return currentEnhanceItem.itemData.EnchantmentSettings.CanEnchantToLevel(nextLevel);
        }

        return false;
    }

    private void OnPerformEnchantClicked()
    {
        Debug.Log("[EnchanceLobby] Perform enchant clicked");

        if (!CanPerformEnchant())
        {
            DebugWhyCannotEnchant();
            return;
        }

        int currentLevel = GetCurrentItemEnchantLevel();
        int nextLevel = currentLevel + 1;
        long cost = GetEnchantCost(nextLevel);
        float successRate = CalculateSuccessRate();

        // ตรวจสอบ target ItemData ก่อน enchant
        string targetItemId = GetEnchantedItemId(currentEnhanceItem.itemData.ItemId, nextLevel);
        ItemData targetItemData = GetItemDataById(targetItemId);

        if (targetItemData == null)
        {
            Debug.LogError($"[EnchanceLobby] Target item not found: {targetItemId}");
            return;
        }

        // เก็บรายการวัตถุดิบก่อนใช้
        List<string> usedMaterials = new List<string>();
        int materialCount = 0;

        for (int i = 0; i < materialSlots.Count; i++)
        {
            if (materialSlots[i] != null && !materialSlots[i].IsEmpty)
            {
                usedMaterials.Add($"{materialSlots[i].itemData.ItemName} x{materialSlots[i].stackCount}");
                materialCount++;
            }
        }

        Debug.Log($"[EnchanceLobby] Attempting enchant: {currentLevel} -> {nextLevel}, Cost: {cost}, Success Rate: {successRate:F1}%");

        // หักเงิน
        if (!currencyManager.SpendGold(cost))
        {
            Debug.LogError("[EnchanceLobby] Failed to spend gold");
            return;
        }

        Debug.Log($"[EnchanceLobby] Successfully spent {cost} gold");

        // ✅ Force อัพเดท UI เงินทันทีหลังจากหักเงิน
        UpdateCurrencyDisplay();

        // ✅ รอสักครู่เพื่อให้ UI อัพเดท
        StartCoroutine(DelayedEnchantProcess(nextLevel, currentLevel, usedMaterials, materialCount, successRate));
    }
    private IEnumerator DelayedEnchantProcess(int nextLevel, int currentLevel, List<string> usedMaterials, int materialCount, float successRate)
    {
        // รอ 1 frame เพื่อให้ UI อัพเดท
        yield return null;

        // ✅ Force อัพเดท UI อีกครั้ง
        UpdateCurrencyDisplay();

        // บริโภควัตถุดิบ
        ConsumeMaterialSlots();

        // ทำการสุ่มผลลัพธ์
        float randomValue = UnityEngine.Random.Range(0f, 100f);
        bool isSuccess = randomValue <= successRate;

        Debug.Log($"[EnchanceLobby] Enchant result: {(isSuccess ? "SUCCESS" : "FAILED")} (rolled {randomValue:F1} vs {successRate:F1}%)");

        if (isSuccess)
        {
            OnEnchantSuccess(nextLevel);
        }
        else
        {
            OnEnchantFailure(currentLevel);
        }

        // Force save ทันทีหลังจาก enchant
        if (playerCharacter?.GetInventory() != null)
        {
            playerCharacter.GetInventory().AutoSaveInventoryData($"EnchantComplete-{(isSuccess ? "Success" : "Failure")}-Level{nextLevel}");
        }

        // ✅ Force อัพเดท UI อีกครั้งหลังจากเสร็จ
        UpdateCurrencyDisplay();
    }

    private void DebugWhyCannotEnchant()
    {

        if (currentEnhanceItem == null)
        {
            return;
        }

        if (!isMaterialMode)
        {
            return;
        }

        int materialCount = GetMaterialCount();
        if (materialCount == 0)
        {
            return;
        }

        int nextLevel = GetCurrentItemEnchantLevel() + 1;
        long cost = GetEnchantCost(nextLevel);

        if (currencyManager == null)
        {
            return;
        }

        if (!currencyManager.HasEnoughGold(cost))
        {
            return;
        }

        // ตรวจสอบ EnchantmentSettings
        if (currentEnhanceItem.itemData.EnchantmentSettings != null)
        {
            if (!currentEnhanceItem.itemData.EnchantmentSettings.CanEnchantToLevel(nextLevel))
            {
                return;
            }
        }

        // ตรวจสอบ ItemData
        string targetItemId = GetEnchantedItemId(currentEnhanceItem.itemData.ItemId, nextLevel);
        ItemData targetItemData = GetItemDataById(targetItemId);

        if (targetItemData == null)
        {
            return;
        }

    }

    private void OnEnchantSuccess(int newLevel)
    {

        if (currentEnhanceItem == null)
        {
            return;
        }


        // สร้างไอเทมใหม่จาก ID เดิม + ระดับใหม่
        string newItemId = GetEnchantedItemId(currentEnhanceItem.itemData.ItemId, newLevel);

        ItemData newItemData = GetItemDataById(newItemId);

        if (newItemData == null)
        {

            // ✅ คืนเงินให้ผู้เล่น
            long cost = GetEnchantCost(newLevel);
            if (currencyManager != null)
            {
                currencyManager.AddGold(cost);
            }

            return;
        }


        enchantResultItem = newItemData;

        // แสดง Success Panel
        ShowEnchantSuccessPanel();

        // ล้างช่อง enhance
        currentEnhanceItem = null;
        currentEnhanceSlotIndex = -1;
        UpdateEnhanceSlotDisplay();
        SetMaterialMode(false);

    }

    private void OnEnchantFailure(int currentLevel)
    {
        Debug.Log($"[EnchanceLobby] Enchant failed for level {currentLevel}");

        // เก็บข้อมูลเดิมไว้สำหรับ repair
        originalEnchantLevel = currentLevel;

        // ✅ เปลี่ยน: ทุกครั้งที่ fail จะลดระดับแน่นอน (ไม่มีโอกาส)
        int newLevel = currentLevel;

        if (currentLevel > 0)
        {
            // ลดระดับ 1-3 ระดับ (สุ่ม)
            int degradeAmount = UnityEngine.Random.Range(1, Mathf.Min(4, currentLevel + 1));
            newLevel = Mathf.Max(0, currentLevel - degradeAmount);
            Debug.Log($"[EnchanceLobby] Item degraded from +{currentLevel} to +{newLevel} (reduced by {degradeAmount})");
        }
        else
        {
            // ถ้าเป็น +0 อยู่แล้ว ก็ยังคงเป็น +0
            newLevel = 0;
            Debug.Log($"[EnchanceLobby] Item was already +0, remains at +0");
        }

        // สร้างไอเทมที่เสียหาย
        string damagedItemId = GetEnchantedItemId(GetBaseItemId(currentEnhanceItem.itemData.ItemId), newLevel);
        ItemData damagedItemData = GetItemDataById(damagedItemId);

        if (damagedItemData == null)
        {
            Debug.LogError($"[EnchanceLobby] Cannot find damaged item data for ID: {damagedItemId}");
            return;
        }

        failedItem = damagedItemData;

        // แสดง Failure Panel
        ShowEnchantFailurePanel();

        // ล้างช่อง enhance
        currentEnhanceItem = null;
        currentEnhanceSlotIndex = -1;
        UpdateEnhanceSlotDisplay();
        SetMaterialMode(false);
    }

    private void ShowEnchantSuccessPanel()
    {
        if (enchantSuccessPanel == null || enchantResultItem == null) return;

        enchantSuccessPanel.SetActive(true);

        // ตั้งค่า UI
        if (successItemIcon != null)
            successItemIcon.sprite = enchantResultItem.ItemIcon;

        if (successItemName != null)
        {
            successItemName.text = enchantResultItem.ItemName;
            successItemName.color = enchantResultItem.GetTierColor();
        }

        if (successItemStats != null)
        {
            successItemStats.text = enchantResultItem.Stats.GetStatsDescription();
        }

    }

    private void ShowEnchantFailurePanel()
    {
        if (enchantFailurePanel == null || failedItem == null) return;

        Debug.Log($"[EnchanceLobby] Showing failure panel for {failedItem.ItemName}");

        enchantFailurePanel.SetActive(true);

        // ตั้งค่า UI
        if (failureItemIcon != null)
            failureItemIcon.sprite = failedItem.ItemIcon;

        if (failureItemName != null)
        {
            failureItemName.text = failedItem.ItemName;
            failureItemName.color = failedItem.GetTierColor();
        }

        int currentLevel = GetEnchantLevelFromId(failedItem.ItemId);

        if (failureDescription != null)
        {
            failureDescription.text = $"Enchantment failed!\nItem degraded to +{currentLevel}";

            // ✅ เปลี่ยน: แสดงข้อความใหม่สำหรับ repair แบบสุ่มครั้งเดียว
            if (currentLevel < originalEnchantLevel)
            {
                int maxPossibleRepair = originalEnchantLevel - currentLevel;
                failureDescription.text += $"\n(Repair once: randomly get +1 to +{maxPossibleRepair} levels)";
                failureDescription.text += $"\n(One chance only! Max: +{originalEnchantLevel})";
            }
        }

        // ตรวจสอบ repair ได้หรือไม่ (สามารถ repair ได้ถ้าระดับปัจจุบัน < ระดับเดิม)
        bool canRepair = currentLevel < originalEnchantLevel;

        // ดึง repair cost พื้นฐาน
        long repairCost = 0;
        bool canAfford = false;

        if (canRepair)
        {
            repairCost = GetRandomRepairCost(currentLevel);
            canAfford = currencyManager != null && currencyManager.HasEnoughGold(repairCost);
            Debug.Log($"[EnchanceLobby] Repair cost for +{currentLevel}: {repairCost}, Can afford: {canAfford}");
        }

        // แสดงราคาซ่อม
        if (repairCostText != null)
        {
            if (canRepair)
            {
                int maxPossibleLevel = originalEnchantLevel;
                repairCostText.text = $"Repair Once (Random result): 💰 {repairCost:N0}";
                repairCostText.color = canAfford ? Color.white : Color.red;
            }
            else
            {
                repairCostText.text = "Item fully repaired";
                repairCostText.color = Color.gray;
            }
        }

        // ✅ แก้ไข: ตั้งค่าปุ่ม repair (ไม่เปลี่ยนสี)
        if (repairButton != null)
        {
            repairButton.gameObject.SetActive(true);
            repairButton.interactable = canRepair && canAfford;

            // ✅ ไม่เปลี่ยนสีปุ่ม

            // เปลี่ยนข้อความบนปุ่ม
            var buttonText = repairButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (buttonText != null)
            {
                if (canRepair && canAfford)
                {
                    buttonText.text = $"Repair Once (💰 {repairCost:N0})";
                    buttonText.color = Color.white;
                }
                else if (canRepair && !canAfford)
                {
                    buttonText.text = "Not enough gold";
                    buttonText.color = Color.red;
                }
                else
                {
                    buttonText.text = "Fully repaired";
                    buttonText.color = Color.gray;
                }
            }
        }
    }

    private void OnConfirmSuccessClicked()
    {
        if (enchantResultItem != null && playerCharacter?.GetInventory() != null)
        {
            Inventory inventory = playerCharacter.GetInventory();

            if (inventory.AddItem(enchantResultItem, 1))
            {

                // ซ่อน panel
                if (enchantSuccessPanel != null)
                    enchantSuccessPanel.SetActive(false);

                // ล้างข้อมูล
                enchantResultItem = null;

                // รีเฟรช inventory
                RefreshEnchanceableItems();
            }
            else
            {
                // TODO: แสดงข้อความ error
            }
        }
    }

    private void OnRepairClicked()
    {
        Debug.Log("[EnchanceLobby] Repair button clicked");

        if (failedItem == null)
        {
            Debug.LogError("[EnchanceLobby] No failed item to repair");
            return;
        }

        int currentLevel = GetEnchantLevelFromId(failedItem.ItemId);
        Debug.Log($"[EnchanceLobby] Current item level: +{currentLevel}, Original level: +{originalEnchantLevel}");

        // ตรวจสอบว่าสามารถ repair ได้หรือไม่
        if (currentLevel >= originalEnchantLevel)
        {
            Debug.Log("[EnchanceLobby] Item already at original level or higher");
            return;
        }

        // คำนวณ cost สำหรับ repair
        long repairCost = GetRandomRepairCost(currentLevel);

        if (currencyManager == null)
        {
            Debug.LogError("[EnchanceLobby] Currency manager not found");
            return;
        }

        long currentGold = currencyManager.GetCurrentGold();
        Debug.Log($"[EnchanceLobby] Player gold: {currentGold}, Repair cost: {repairCost}");

        if (!currencyManager.HasEnoughGold(repairCost))
        {
            Debug.Log("[EnchanceLobby] Not enough gold for repair");
            return;
        }

        if (!currencyManager.SpendGold(repairCost))
        {
            Debug.LogError("[EnchanceLobby] Failed to spend gold");
            return;
        }

        Debug.Log($"[EnchanceLobby] Spent {repairCost} gold for repair");

        // ✅ Force อัพเดท UI เงินทันทีหลังจากหักเงิน
        UpdateCurrencyDisplay();

        // สุ่มระดับสุดท้ายที่จะได้ (ครั้งเดียวเท่านั้น)
        int maxPossibleIncrease = originalEnchantLevel - currentLevel;
        int randomIncrease = UnityEngine.Random.Range(1, maxPossibleIncrease + 1);
        int finalRepairedLevel = Mathf.Min(currentLevel + randomIncrease, originalEnchantLevel);

        Debug.Log($"[EnchanceLobby] One-time repair: +{randomIncrease} levels (from +{currentLevel} to +{finalRepairedLevel})");

        string repairedItemId = GetEnchantedItemId(GetBaseItemId(failedItem.ItemId), finalRepairedLevel);
        ItemData repairedItemData = GetItemDataById(repairedItemId);

        Debug.Log($"[EnchanceLobby] Looking for repaired item ID: {repairedItemId}");

        if (repairedItemData == null)
        {
            Debug.LogError($"[EnchanceLobby] Cannot find repaired item data for ID: {repairedItemId}");
            // คืนเงินถ้าหาไอเทมไม่เจอ
            currencyManager.AddGold(repairCost);
            // ✅ อัพเดท UI หลังคืนเงิน
            UpdateCurrencyDisplay();
            return;
        }

        if (playerCharacter?.GetInventory() == null)
        {
            Debug.LogError("[EnchanceLobby] Player inventory not found");
            // คืนเงินถ้า inventory ไม่มี
            currencyManager.AddGold(repairCost);
            // ✅ อัพเดท UI หลังคืนเงิน
            UpdateCurrencyDisplay();
            return;
        }

        Inventory inventory = playerCharacter.GetInventory();

        if (inventory.AddItem(repairedItemData, 1))
        {
            Debug.Log($"[EnchanceLobby] Successfully repaired item to +{finalRepairedLevel} (increased by +{randomIncrease})");

            // ซ่อน panel ทันที (ไม่ว่าจะซ่อมครบหรือไม่ก็ตาม)
            if (enchantFailurePanel != null)
                enchantFailurePanel.SetActive(false);

            // ล้างข้อมูล
            failedItem = null;
            originalEnchantLevel = 0;

            Debug.Log("[EnchanceLobby] Repair completed! (One-time only)");

            // รีเฟรช inventory
            RefreshEnchanceableItems();

            // Force save inventory ทันที
            inventory.AutoSaveInventoryData($"RepairComplete-Level{finalRepairedLevel}");

            // ✅ Force อัพเดท UI อีกครั้งหลังจากเสร็จ
            UpdateCurrencyDisplay();
        }
        else
        {
            Debug.LogError("[EnchanceLobby] Failed to add repaired item to inventory");
            // คืนเงินถ้า inventory เต็ม
            currencyManager.AddGold(repairCost);
            // ✅ อัพเดท UI หลังคืนเงิน
            UpdateCurrencyDisplay();

            // TODO: แสดงข้อความ error ให้ผู้เล่น
        }
    }
    public void ForceUpdateCurrencyDisplay()
    {
        UpdateCurrencyDisplay();
    }
    // ✅ เพิ่ม method ใหม่สำหรับคำนวณ repair cost แบบสุ่ม
    private long GetRandomRepairCost(int currentLevel)
    {
        if (failedItem == null || originalEnchantLevel <= 0)
        {
            return 0;
        }

        // ใช้ enchantment settings ถ้ามี
        if (failedItem.EnchantmentSettings != null && failedItem.EnchantmentSettings.canBeEnchanted)
        {
            // คำนวณ cost พื้นฐานจากการ enchant ระดับเดิม
            long baseEnchantCost = failedItem.EnchantmentSettings.GetEnchantCost(0, originalEnchantLevel);
            long repairCost = (long)(baseEnchantCost * failedItem.EnchantmentSettings.repairCostMultiplier);

            return repairCost;
        }

        // ใช้ formula พื้นฐาน (ถ้าไม่มี enchantment settings)
        long baseCost = 1000; // ราคาพื้นฐาน
        long totalCost = 0;

        // คำนวณ cost จากระดับปัจจุบันไปยังระดับเดิม
        for (int i = currentLevel + 1; i <= originalEnchantLevel; i++)
        {
            totalCost += (long)(baseCost * Mathf.Pow(1.5f, i - 1));
        }

        long finalRepairCost = (long)(totalCost * 0.8f); // 80% ของราคา enchant

        return finalRepairCost;
    }


    private void OnCancelFailureClicked()
    {
        if (failedItem != null && playerCharacter?.GetInventory() != null)
        {
            Inventory inventory = playerCharacter.GetInventory();

            if (inventory.AddItem(failedItem, 1))
            {

                // ซ่อน panel
                if (enchantFailurePanel != null)
                    enchantFailurePanel.SetActive(false);

                // ล้างข้อมูล
                failedItem = null;
                originalEnchantLevel = 0;

                // รีเฟรช inventory
                RefreshEnchanceableItems();
            }
            else
            {
            }
        }
    }

    #region Helper Methods for Item ID Management

    /// <summary>
    /// สร้าง Item ID ที่มีระดับ enchant
    /// </summary>
    private string GetEnchantedItemId(string baseItemId, int enchantLevel)
    {
        string baseId = GetBaseItemId(baseItemId);

        if (enchantLevel <= 0)
        {
            return baseId;
        }

        string enchantedId = $"{baseId}+{enchantLevel}";

        return enchantedId;
    }

    /// <summary>
    /// ดึง Base Item ID (ไม่มีระดับ enchant)
    /// </summary>
    private string GetBaseItemId(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return itemId;

        int plusIndex = itemId.LastIndexOf('+');
        if (plusIndex > 0)
        {
            string afterPlus = itemId.Substring(plusIndex + 1);
            if (int.TryParse(afterPlus, out _)) // ถ้าหลัง + เป็นตัวเลข
            {
                return itemId.Substring(0, plusIndex);
            }
        }

        return itemId; // ไม่มี enchant level
    }

    /// <summary>
    /// ดึงระดับ enchant จาก Item ID
    /// </summary>
    private int GetEnchantLevelFromId(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return 0;

        int plusIndex = itemId.LastIndexOf('+');
        if (plusIndex > 0 && plusIndex < itemId.Length - 1)
        {
            string levelString = itemId.Substring(plusIndex + 1);
            if (int.TryParse(levelString, out int level))
            {
                return level;
            }
        }

        return 0;
    }

    /// <summary>
    /// หา ItemData จาก ID (ใช้ database)
    /// </summary>
    private ItemData GetItemDataById(string itemId)
    {

        if (string.IsNullOrEmpty(itemId))
        {
            return null;
        }

        var database = ItemDatabase.Instance;
        if (database?.GetAllItems() == null)
        {
            return null;
        }

        var allItems = database.GetAllItems();

        // ✅ Debug รายการ items ที่คล้ายกัน
        var similarItems = new List<string>();

        foreach (var item in allItems)
        {
            if (item?.ItemId == itemId)
            {
                return item;
            }

            // เก็บ items ที่มีชื่อคล้ายกัน
            if (item != null && item.ItemId.Contains(itemId.Split('+')[0]))
            {
                similarItems.Add(item.ItemId);
            }
        }

      

        return null;
    }
   
    /// <summary>
    /// คำนวณราคาซ่อม
    /// </summary>
    private long GetRepairCost()
    {
        if (failedItem == null || originalEnchantLevel <= 0)
        {
            return 0;
        }

      

        // ✅ วิธีที่ 1: ใช้ enchantment settings ถ้ามี
        if (failedItem.EnchantmentSettings != null && failedItem.EnchantmentSettings.canBeEnchanted)
        {
            long enchantCost = failedItem.EnchantmentSettings.GetEnchantCost(0, originalEnchantLevel);
            long repairCost = (long)(enchantCost * failedItem.EnchantmentSettings.repairCostMultiplier);

            return repairCost;
        }

        // ✅ วิธีที่ 2: ใช้ formula พื้นฐาน (ถ้าไม่มี enchantment settings)
        long baseCost = 1000; // ราคาพื้นฐาน
        long totalCost = 0;

        for (int i = 1; i <= originalEnchantLevel; i++)
        {
            totalCost += (long)(baseCost * Mathf.Pow(1.5f, i - 1));
        }

        long finalRepairCost = (long)(totalCost * 0.7f); // 70% ของราคา enchant

        return finalRepairCost;
    }
  
    /// <summary>
    /// แก้ไข GetCurrentItemEnchantLevel() ให้ดูจาก Item ID
    /// </summary>
  #endregion
}