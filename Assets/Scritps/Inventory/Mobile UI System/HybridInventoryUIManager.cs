using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class HybridInventoryUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject inventoryPanel;
    public GameObject equipmentPanel;
    public Button inventoryTabButton;
    public Button equipmentTabButton;

    [Header("Inventory Grid")]
    public Transform inventoryGrid;
    public GameObject inventorySlotPrefab;
    public int gridColumns = 6;

    [Header("Equipment Slots")]
    public InventorySlot weaponSlot;
    public InventorySlot helmetSlot;
    public InventorySlot armorSlot;
    public InventorySlot pantsSlot;
    public InventorySlot bootsSlot;

    [Header("Item Info Panel")]
    public GameObject itemInfoPanel;
    public Image itemInfoIcon;
    public TextMeshProUGUI itemInfoName;
    public TextMeshProUGUI itemInfoDescription;
    public TextMeshProUGUI itemInfoStats;

    [Header("Action Buttons")]
    public Button useButton;
    public Button equipButton;
    public Button unequipButton;
    public Button useAsBoostButton; // ใหม่: ปุ่มสำหรับใช้เป็น boost

    [Header("Player Stats")]
    public TextMeshProUGUI playerStatsText;

    [Header("Use Mode Settings")]
    public bool showUseForEquipment = true; // แสดงปุ่ม Use สำหรับ equipment
    public bool showEquipOption = true; // แสดงปุ่ม Equip แบบปกติ
    public bool prioritizeUseOverEquip = true; // ให้ Use เป็น priority

    [Header("Debug")]
    public bool showDebugInfo = true;

    private HybridInventoryManager hybridInventoryManager;
    private EquipmentManager equipmentManager;
    private ItemDatabase itemDatabase;
    private List<InventorySlot> inventorySlots = new List<InventorySlot>();
    private Dictionary<EquipmentType, InventorySlot> equipmentSlots = new Dictionary<EquipmentType, InventorySlot>();
    private ItemData selectedItem;
    private int selectedSlotIndex = -1;
    private bool isInventoryOpen = false;
    private int retryCount = 0;
    private const int maxRetries = 10;

    void Start()
    {
        InitializeUI();
        StartCoroutine(FindAndSetupManagers());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }
    }

    void InitializeUI()
    {
        // Setup tab buttons
        if (inventoryTabButton != null)
            inventoryTabButton.onClick.AddListener(() => ShowInventoryTab());
        if (equipmentTabButton != null)
            equipmentTabButton.onClick.AddListener(() => ShowEquipmentTab());

        // Setup action buttons
        if (useButton != null)
            useButton.onClick.AddListener(UseSelectedItem);
        if (equipButton != null)
            equipButton.onClick.AddListener(EquipSelectedItem);
        if (unequipButton != null)
            unequipButton.onClick.AddListener(UnequipSelectedItem);
        if (useAsBoostButton != null)
            useAsBoostButton.onClick.AddListener(UseSelectedItemAsBoost);

        // Initialize equipment slots dictionary
        if (weaponSlot != null) equipmentSlots[EquipmentType.Weapon] = weaponSlot;
        if (helmetSlot != null) equipmentSlots[EquipmentType.Helmet] = helmetSlot;
        if (armorSlot != null) equipmentSlots[EquipmentType.Armor] = armorSlot;
        if (pantsSlot != null) equipmentSlots[EquipmentType.Pants] = pantsSlot;
        if (bootsSlot != null) equipmentSlots[EquipmentType.Boots] = bootsSlot;

        // Setup equipment slot callbacks
        foreach (var kvp in equipmentSlots)
        {
            var equipType = kvp.Key;
            var slot = kvp.Value;
            if (slot != null)
            {
                slot.SetEquipmentType(equipType);
                slot.OnSlotClicked += (slotData) => OnEquipmentSlotClicked(equipType);
            }
        }

        CreateInventorySlots();

        if (itemInfoPanel != null)
            itemInfoPanel.SetActive(false);

        ShowInventoryTab();
    }

    System.Collections.IEnumerator FindAndSetupManagers()
    {
        Debug.Log("🔍 Searching for managers...");

        while ((hybridInventoryManager == null || equipmentManager == null) && retryCount < maxRetries)
        {
            retryCount++;
            Debug.Log($"Attempt {retryCount}/{maxRetries} - Searching for managers...");

            // Find HybridInventoryManager
            if (hybridInventoryManager == null)
            {
                hybridInventoryManager = FindObjectOfType<HybridInventoryManager>();
            }

            // Find EquipmentManager
            if (equipmentManager == null)
            {
                equipmentManager = FindObjectOfType<EquipmentManager>();
            }

            if (hybridInventoryManager != null && equipmentManager != null)
            {
                Debug.Log($"✅ Found both managers");
                SetupManagerConnections();
                break;
            }
            else if (hybridInventoryManager != null)
            {
                Debug.Log($"✅ Found HybridInventoryManager, EquipmentManager not required");
                SetupManagerConnections();
                break;
            }

            yield return new WaitForSeconds(1f);
        }

        if (hybridInventoryManager == null)
        {
            Debug.LogError("❌ Failed to find HybridInventoryManager!");
        }
    }

    void SetupManagerConnections()
    {
        if (hybridInventoryManager == null) return;

        // Get ItemDatabase
        itemDatabase = hybridInventoryManager.itemDatabase;
        if (itemDatabase == null)
        {
            Debug.LogError("ItemDatabase not found in HybridInventoryManager!");
            return;
        }

        // Subscribe to events
        hybridInventoryManager.OnInventoryChanged += RefreshInventoryUI;
        hybridInventoryManager.OnItemEquipped += OnItemEquipped;
        hybridInventoryManager.OnItemUnequipped += OnItemUnequipped;
        hybridInventoryManager.OnItemUsed += OnItemUsed; // เพิ่ม event ใหม่

        Debug.Log("✅ HybridInventoryUIManager connected successfully!");

        // Initial UI refresh
        RefreshInventoryUI();
    }

    #region Inventory Slots Management

    void CreateInventorySlots()
    {
        if (inventoryGrid == null || inventorySlotPrefab == null) return;

        foreach (Transform child in inventoryGrid)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        inventorySlots.Clear();

        for (int i = 0; i < 30; i++)
        {
            GameObject slotObj = Instantiate(inventorySlotPrefab, inventoryGrid);
            InventorySlot slot = slotObj.GetComponent<InventorySlot>();
            if (slot != null)
            {
                slot.SetSlotIndex(i);
                slot.OnSlotClicked += OnInventorySlotClicked;
                inventorySlots.Add(slot);
            }
        }

        Debug.Log($"Created {inventorySlots.Count} inventory slots");
    }

    #endregion

    #region Slot Click Handlers

    void OnInventorySlotClicked(InventorySlotData slotData)
    {
        selectedSlotIndex = slotData.slotIndex;

        if (!string.IsNullOrEmpty(slotData.itemId))
        {
            selectedItem = itemDatabase?.GetItem(slotData.itemId);
            ShowItemInfo(selectedItem, slotData.quantity);
        }
        else
        {
            HideItemInfo();
        }
    }

    void OnEquipmentSlotClicked(EquipmentType equipmentType)
    {
        if (hybridInventoryManager == null) return;

        var equippedItem = hybridInventoryManager.GetEquippedItem(equipmentType);
        if (equippedItem != null)
        {
            selectedItem = itemDatabase?.GetItem(equippedItem.itemId);
            selectedSlotIndex = -1;
            ShowItemInfo(selectedItem, 1, true);
        }
        else
        {
            HideItemInfo();
        }
    }

    #endregion

    #region Enhanced Item Info Display

    void ShowItemInfo(ItemData item, int quantity, bool isEquipped = false)
    {
        if (item == null || itemInfoPanel == null) return;

        itemInfoPanel.SetActive(true);

        if (itemInfoIcon != null)
            itemInfoIcon.sprite = item.icon;

        if (itemInfoName != null)
        {
            itemInfoName.text = item.itemName;
            itemInfoName.color = item.GetRarityColor();
        }

        if (itemInfoDescription != null)
            itemInfoDescription.text = item.description;

        if (itemInfoStats != null)
        {
            string statsText = BuildItemStatsText(item, quantity);
            itemInfoStats.text = statsText;
        }

        // Setup buttons based on item type and settings
        SetupActionButtons(item, isEquipped);
    }

    void SetupActionButtons(ItemData item, bool isEquipped)
    {
        if (isEquipped)
        {
            // เมื่อ item ถูก equip แล้ว
            SetButtonState(useButton, false);
            SetButtonState(equipButton, false);
            SetButtonState(unequipButton, true);
            SetButtonState(useAsBoostButton, false);
        }
        else
        {
            switch (item.itemType)
            {
                case ItemType.Consumable:
                    SetupConsumableButtons();
                    break;

                case ItemType.Equipment:
                    SetupEquipmentButtons(item);
                    break;

                default:
                    SetupDefaultButtons();
                    break;
            }
        }
    }

    void SetupConsumableButtons()
    {
        SetButtonState(useButton, true, "Use", "💊 Consume this item");
        SetButtonState(equipButton, false);
        SetButtonState(unequipButton, false);
        SetButtonState(useAsBoostButton, false);
    }

    void SetupEquipmentButtons(ItemData item)
    {
        bool hasEquipmentManager = equipmentManager != null && hybridInventoryManager.useEquipmentManagerForUse;

        if (showUseForEquipment && hasEquipmentManager)
        {
            if (prioritizeUseOverEquip)
            {
                // Use เป็น primary, Equip เป็น secondary
                SetButtonState(useButton, true, "Use", "⚡ Apply stats as bonus");
                SetButtonState(equipButton, showEquipOption, "Equip", "🎽 Equip normally");
                SetButtonState(useAsBoostButton, true, "Boost", "💪 Boost stats permanently");
            }
            else
            {
                // Equip เป็น primary, Use เป็น secondary
                SetButtonState(useButton, true, "Use", "⚡ Apply as bonus");
                SetButtonState(equipButton, true, "Equip", "🎽 Equip this item");
                SetButtonState(useAsBoostButton, showUseForEquipment, "Boost", "💪 Permanent boost");
            }
        }
        else
        {
            // แค่ Equip ปกติ
            SetButtonState(useButton, false);
            SetButtonState(equipButton, true, "Equip", "🎽 Equip this item");
            SetButtonState(useAsBoostButton, false);
        }

        SetButtonState(unequipButton, false);
    }

    void SetupDefaultButtons()
    {
        SetButtonState(useButton, false);
        SetButtonState(equipButton, false);
        SetButtonState(unequipButton, false);
        SetButtonState(useAsBoostButton, false);
    }

    void SetButtonState(Button button, bool active, string text = "", string tooltip = "")
    {
        if (button == null) return;

        button.gameObject.SetActive(active);

        if (active && !string.IsNullOrEmpty(text))
        {
            var buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = text;
            }

            // Add tooltip if available
            if (!string.IsNullOrEmpty(tooltip))
            {
                // You can implement tooltip system here
                button.name = tooltip; // Simple tooltip via name for now
            }
        }
    }

    string BuildItemStatsText(ItemData item, int quantity)
    {
        string statsText = "";

        if (item.itemType == ItemType.Equipment)
        {
            statsText += "<color=#FFD700><b>⚔️ Equipment Stats:</b></color>\n";
            if (item.stats.attackDamage > 0)
                statsText += $"<color=#FF6B6B>⚔️ Attack Damage: +{item.stats.attackDamage}</color>\n";
            if (item.stats.armor > 0)
                statsText += $"<color=#4ECDC4>🛡️ Armor: +{item.stats.armor}</color>\n";
            if (item.stats.maxHp > 0)
                statsText += $"<color=#FF69B4>❤️ Max HP: +{item.stats.maxHp}</color>\n";
            if (item.stats.maxMana > 0)
                statsText += $"<color=#87CEEB>💙 Max Mana: +{item.stats.maxMana}</color>\n";
            if (item.stats.moveSpeed > 0)
                statsText += $"<color=#98FB98>🏃 Move Speed: +{item.stats.moveSpeed:F1}</color>\n";
            if (item.stats.criticalChance > 0)
                statsText += $"<color=#FFFF00>⚡ Critical Chance: +{item.stats.criticalChance:F1}%</color>\n";

            // แสดงข้อมูลเพิ่มเติมถ้ามี EquipmentManager
            if (equipmentManager != null && hybridInventoryManager.useEquipmentManagerForUse)
            {
                statsText += "\n<color=#00FF7F><b>💪 Use Effects:</b></color>\n";
                statsText += "<color=#FFD700>✨ Will be applied as stat bonus</color>\n";
                if (!hybridInventoryManager.consumeEquipmentOnUse)
                {
                    statsText += "<color=#90EE90>♻️ Item will not be consumed</color>\n";
                }
                else
                {
                    statsText += "<color=#FFA500>⚠️ Item will be consumed</color>\n";
                }
            }
        }
        else if (item.itemType == ItemType.Consumable)
        {
            statsText += "<color=#00FF00><b>💊 Consumable Effects:</b></color>\n";
            if (item.healAmount > 0)
                statsText += $"<color=#FF69B4>❤️ Heal: +{item.healAmount} HP</color>\n";
            if (item.manaAmount > 0)
                statsText += $"<color=#87CEEB>💙 Restore: +{item.manaAmount} MP</color>\n";
        }

        // Additional info
        statsText += "\n<color=#CCCCCC><b>📋 Item Info:</b></color>\n";
        if (quantity > 1)
            statsText += $"<color=#FFFFFF>📦 Quantity: {quantity}</color>\n";
        statsText += $"<color={GetRarityColorHex(item.rarity)}>💎 Rarity: {item.rarity}</color>\n";
        statsText += $"<color=#FFD700>💰 Value: {item.sellPrice} Gold</color>";

        return statsText;
    }

    string GetRarityColorHex(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return "#FFFFFF";
            case ItemRarity.Uncommon: return "#00FF00";
            case ItemRarity.Rare: return "#0080FF";
            case ItemRarity.Epic: return "#FF00FF";
            case ItemRarity.Legendary: return "#FFD700";
            default: return "#FFFFFF";
        }
    }

    void HideItemInfo()
    {
        if (itemInfoPanel != null)
            itemInfoPanel.SetActive(false);
        selectedItem = null;
        selectedSlotIndex = -1;
    }

    #endregion

    #region Enhanced Action Methods

    void UseSelectedItem()
    {
        if (selectedItem != null && hybridInventoryManager != null)
        {
            bool success = hybridInventoryManager.UseItem(selectedItem.itemId);
            if (success)
            {
                Debug.Log($"✅ Used item: {selectedItem.itemName}");
                ShowUseAnimation(selectedItem);
                HideItemInfo();
            }
            else
            {
                Debug.LogWarning($"❌ Failed to use item: {selectedItem.itemName}");
                ShowErrorMessage("Failed to use item!");
            }
        }
    }

    void EquipSelectedItem()
    {
        if (selectedItem != null && selectedItem.itemType == ItemType.Equipment && hybridInventoryManager != null)
        {
            bool success = hybridInventoryManager.EquipItem(selectedItem.itemId);
            if (success)
            {
                Debug.Log($"🎽 Equipped item: {selectedItem.itemName}");
                ShowEquipAnimation(selectedItem);
                HideItemInfo();
            }
            else
            {
                Debug.LogWarning($"❌ Failed to equip item: {selectedItem.itemName}");
                ShowErrorMessage("Failed to equip item!");
            }
        }
    }

    void UnequipSelectedItem()
    {
        if (selectedItem != null && selectedItem.itemType == ItemType.Equipment && hybridInventoryManager != null)
        {
            bool success = hybridInventoryManager.UnequipItem(selectedItem.equipmentType);
            if (success)
            {
                Debug.Log($"🎽 Unequipped item: {selectedItem.itemName}");
                HideItemInfo();
            }
            else
            {
                Debug.LogWarning($"❌ Failed to unequip item: {selectedItem.itemName}");
                ShowErrorMessage("Failed to unequip item!");
            }
        }
    }

    void UseSelectedItemAsBoost()
    {
        if (selectedItem != null && selectedItem.itemType == ItemType.Equipment && hybridInventoryManager != null)
        {
            // Save current setting
            bool originalConsumeSetting = hybridInventoryManager.consumeEquipmentOnUse;

            // Force consume for boost
            hybridInventoryManager.consumeEquipmentOnUse = true;

            bool success = hybridInventoryManager.UseItem(selectedItem.itemId);

            // Restore original setting
            hybridInventoryManager.consumeEquipmentOnUse = originalConsumeSetting;

            if (success)
            {
                Debug.Log($"💪 Boosted with item: {selectedItem.itemName}");
                ShowBoostAnimation(selectedItem);
                HideItemInfo();
            }
            else
            {
                Debug.LogWarning($"❌ Failed to boost with item: {selectedItem.itemName}");
                ShowErrorMessage("Failed to boost stats!");
            }
        }
    }

    #endregion

    #region Visual Effects

    void ShowUseAnimation(ItemData item)
    {
        // เอฟเฟคการใช้ item
        Debug.Log($"✨ Use animation for {item.itemName}");
        // TODO: Add particle effects, sound, etc.
    }

    void ShowEquipAnimation(ItemData item)
    {
        // เอฟเฟคการ equip
        Debug.Log($"🎽 Equip animation for {item.itemName}");
        // TODO: Add particle effects, sound, etc.
    }

    void ShowBoostAnimation(ItemData item)
    {
        // เอฟเฟคการ boost
        Debug.Log($"💪 Boost animation for {item.itemName}");
        // TODO: Add special boost effects
    }

    void ShowErrorMessage(string message)
    {
        Debug.LogWarning($"⚠️ {message}");
        // TODO: Show UI error message
    }

    #endregion

    #region Event Handlers

    void OnItemUsed(InventoryItem item)
    {
        Debug.Log($"📦 Item used event: {item.itemId}");
        RefreshInventoryUI();

        // Show feedback
        var itemData = itemDatabase?.GetItem(item.itemId);
        if (itemData != null)
        {
            ShowUseAnimation(itemData);
        }
    }

    void OnItemEquipped(EquippedItem item)
    {
        Debug.Log($"⚔️ Item equipped event: {item.itemId}");
        RefreshInventoryUI();
    }

    void OnItemUnequipped(EquipmentType equipmentType)
    {
        Debug.Log($"🛡️ Item unequipped event: {equipmentType}");
        RefreshInventoryUI();
    }

    #endregion

    #region UI Refresh (Enhanced)

    public void RefreshInventoryUI()
    {
        if (hybridInventoryManager == null)
        {
            Debug.LogWarning("Cannot refresh UI - HybridInventoryManager is null");
            return;
        }

        if (showDebugInfo)
        {
            Debug.Log("🔄 Refreshing Inventory UI...");
        }

        RefreshInventorySlots();
        RefreshEquipmentSlots();
        UpdatePlayerStatsDisplay();

        if (showDebugInfo)
        {
            Debug.Log("✅ UI Refresh completed");
        }
    }

    void RefreshInventorySlots()
    {
        if (hybridInventoryManager == null || inventorySlots == null) return;

        foreach (var slot in inventorySlots)
        {
            if (slot != null)
                slot.ClearSlot();
        }

        var inventoryItems = hybridInventoryManager.GetInventoryItems();
        if (inventoryItems != null)
        {
            foreach (var kvp in inventoryItems)
            {
                int slotIndex = kvp.Key;
                var item = kvp.Value;

                if (slotIndex < inventorySlots.Count && inventorySlots[slotIndex] != null)
                {
                    ItemData itemData = itemDatabase?.GetItem(item.itemId);
                    if (itemData != null)
                    {
                        inventorySlots[slotIndex].SetItem(itemData, item.quantity);
                    }
                }
            }
        }
    }

    void RefreshEquipmentSlots()
    {
        if (hybridInventoryManager == null) return;

        foreach (var kvp in equipmentSlots)
        {
            EquipmentType equipType = kvp.Key;
            InventorySlot slot = kvp.Value;

            if (slot != null)
            {
                var equippedItem = hybridInventoryManager.GetEquippedItem(equipType);
                if (equippedItem != null)
                {
                    ItemData itemData = itemDatabase?.GetItem(equippedItem.itemId);
                    if (itemData != null)
                    {
                        slot.SetItem(itemData, 1);
                    }
                }
                else
                {
                    slot.ClearSlot();
                }
            }
        }
    }

    void UpdatePlayerStatsDisplay()
    {
        if (hybridInventoryManager == null || playerStatsText == null) return;

        ItemStats totalStats = hybridInventoryManager.GetTotalEquipmentStats();

        string statsText = "<color=#FFD700><size=16><b>📊 Equipment Bonuses</b></size></color>\n\n";

        // Combat Stats
        statsText += "<color=#FF6B6B><b>⚔️ Combat:</b></color>\n";
        statsText += $"   Attack Damage: <color=#FFFFFF>+{totalStats.attackDamage}</color>\n";
        statsText += $"   Critical Chance: <color=#FFFF00>+{totalStats.criticalChance:F1}%</color>\n";
        statsText += $"   Critical Damage: <color=#FFA500>+{totalStats.criticalDamage:F1}%</color>\n\n";

        // Defense Stats
        statsText += "<color=#4ECDC4><b>🛡️ Defense:</b></color>\n";
        statsText += $"   Armor: <color=#FFFFFF>+{totalStats.armor}</color>\n";
        statsText += $"   Max HP: <color=#FF69B4>+{totalStats.maxHp}</color>\n\n";

        // Utility Stats
        statsText += "<color=#98FB98><b>🏃 Utility:</b></color>\n";
        statsText += $"   Move Speed: <color=#FFFFFF>+{totalStats.moveSpeed:F1}</color>\n";
        statsText += $"   Max Mana: <color=#87CEEB>+{totalStats.maxMana}</color>\n\n";

        // Show equipment manager stats if available
        if (equipmentManager != null)
        {
            var equipStats = equipmentManager.GetTotalStats();
            statsText += "<color=#FFD700><b>💪 Additional Bonuses:</b></color>\n";
            statsText += $"   Rune ATK: <color=#FF6B6B>+{equipStats.attackDamageBonus}</color>\n";
            statsText += $"   Rune ARM: <color=#4ECDC4>+{equipStats.armorBonus}</color>\n";
            statsText += $"   Physical Res: <color=#FFA500>{equipmentManager.GetTotalPhysicalResistance():F1}%</color>\n";
            statsText += $"   Magical Res: <color=#9370DB>{equipmentManager.GetTotalMagicalResistance():F1}%</color>\n\n";
        }

        // Show mode info
        statsText += $"<color=#CCCCCC><size=10>Mode: {(hybridInventoryManager.IsNetworkMode() ? "Network" : "Local")}</size></color>";

        playerStatsText.text = statsText;
    }

    #endregion

    #region Tab Management

    void ShowInventoryTab()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(true);
        if (equipmentPanel != null)
            equipmentPanel.SetActive(false);

        if (inventoryTabButton != null)
            inventoryTabButton.GetComponent<Image>().color = Color.yellow;
        if (equipmentTabButton != null)
            equipmentTabButton.GetComponent<Image>().color = Color.white;
    }

    void ShowEquipmentTab()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
        if (equipmentPanel != null)
            equipmentPanel.SetActive(true);

        if (inventoryTabButton != null)
            inventoryTabButton.GetComponent<Image>().color = Color.white;
        if (equipmentTabButton != null)
            equipmentTabButton.GetComponent<Image>().color = Color.yellow;
    }

    public void ToggleInventory()
    {
        bool isActive = gameObject.activeInHierarchy;
        gameObject.SetActive(!isActive);

        if (gameObject.activeInHierarchy)
        {
            RefreshInventoryUI();
        }
    }

    #endregion

    #region Context Menu (Enhanced Debug Tools)

    [ContextMenu("Add Test Items")]
    public void AddTestItems()
    {
        if (hybridInventoryManager != null)
        {
            hybridInventoryManager.AddItem("health_potion_small", 10);
            hybridInventoryManager.AddItem("mana_potion_small", 5);
            hybridInventoryManager.AddItem("iron_sword", 3);
            hybridInventoryManager.AddItem("leather_armor", 2);
            hybridInventoryManager.AddItem("steel_helmet", 1);
            Debug.Log("Test items added!");
        }
    }

    [ContextMenu("Test Use Equipment")]
    public void TestUseEquipment()
    {
        if (hybridInventoryManager != null)
        {
            hybridInventoryManager.UseItem("iron_sword");
            Debug.Log("Tested equipment use");
        }
    }

    [ContextMenu("Toggle Use Mode")]
    public void ToggleUseMode()
    {
        showUseForEquipment = !showUseForEquipment;
        Debug.Log($"Show Use for Equipment: {showUseForEquipment}");

        // Refresh item info if something is selected
        if (selectedItem != null)
        {
            ShowItemInfo(selectedItem, 1);
        }
    }

    [ContextMenu("Toggle Equipment Manager Integration")]
    public void ToggleEquipmentManagerIntegration()
    {
        if (hybridInventoryManager != null)
        {
            hybridInventoryManager.useEquipmentManagerForUse = !hybridInventoryManager.useEquipmentManagerForUse;
            Debug.Log($"Equipment Manager Use: {hybridInventoryManager.useEquipmentManagerForUse}");
        }
    }

    [ContextMenu("Force Refresh UI")]
    public void ForceRefreshUI()
    {
        RefreshInventoryUI();
    }

    #endregion

    void OnDestroy()
    {
        if (hybridInventoryManager != null)
        {
            hybridInventoryManager.OnInventoryChanged -= RefreshInventoryUI;
            hybridInventoryManager.OnItemEquipped -= OnItemEquipped;
            hybridInventoryManager.OnItemUnequipped -= OnItemUnequipped;
            hybridInventoryManager.OnItemUsed -= OnItemUsed;
        }
    }
}