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
    public Button useButton;
    public Button equipButton;
    public Button unequipButton;
    
    [Header("Player Stats")]
    public TextMeshProUGUI playerStatsText;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    private HybridInventoryManager hybridInventoryManager;
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
        StartCoroutine(FindAndSetupHybridInventoryManager());
    }
    
    void Update()
    {
        // ปิด Inventory เมื่อกด ESC
        if (Input.GetKeyDown(KeyCode.Escape) && gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
        }
        
        // ตรวจสอบการกดปุ่มเพื่อเปิดปิด Inventory
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
        
        // Setup item info buttons
        if (useButton != null)
            useButton.onClick.AddListener(UseSelectedItem);
        if (equipButton != null)
            equipButton.onClick.AddListener(EquipSelectedItem);
        if (unequipButton != null)
            unequipButton.onClick.AddListener(UnequipSelectedItem);
        
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
        
        // Create inventory slots
        CreateInventorySlots();
        
        // Hide info panel initially
        if (itemInfoPanel != null)
            itemInfoPanel.SetActive(false);
        
        // Show inventory tab by default
        ShowInventoryTab();
    }
    
    System.Collections.IEnumerator FindAndSetupHybridInventoryManager()
    {
        Debug.Log("🔍 Searching for HybridInventoryManager...");
        
        while (hybridInventoryManager == null && retryCount < maxRetries)
        {
            retryCount++;
            Debug.Log($"Attempt {retryCount}/{maxRetries} - Searching for HybridInventoryManager...");
            
            // หา HybridInventoryManager
            hybridInventoryManager = FindObjectOfType<HybridInventoryManager>();
            
            if (hybridInventoryManager != null)
            {
                Debug.Log($"✅ Found HybridInventoryManager on: {hybridInventoryManager.name}");
                Debug.Log($"Network Mode: {hybridInventoryManager.IsNetworkMode()}");
                Debug.Log($"Data Loaded: {hybridInventoryManager.IsDataLoaded()}");
                
                // Setup การเชื่อมต่อ
                SetupInventoryConnection();
                break;
            }
            
            yield return new WaitForSeconds(1f);
        }
        
        if (hybridInventoryManager == null)
        {
            Debug.LogError("❌ Failed to find HybridInventoryManager after maximum retries!");
            Debug.LogError("Make sure HybridInventoryManager is attached to a Character GameObject");
        }
    }
    
    void SetupInventoryConnection()
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
        
        Debug.Log("✅ HybridInventoryUIManager connected successfully!");
        
        // Initial UI refresh
        RefreshInventoryUI();
    }
    
    void CreateInventorySlots()
    {
        if (inventoryGrid == null || inventorySlotPrefab == null) return;
        
        // Clear existing slots
        foreach (Transform child in inventoryGrid)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        inventorySlots.Clear();
        
        // Create new slots
        for (int i = 0; i < 30; i++) // Max inventory slots
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
            selectedSlotIndex = -1; // Equipment slot
            ShowItemInfo(selectedItem, 1, true);
        }
        else
        {
            HideItemInfo();
        }
    }
    
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
        
        // Show stats
        if (itemInfoStats != null)
        {
            string statsText = BuildItemStatsText(item, quantity);
            itemInfoStats.text = statsText;
        }
        
        // Setup buttons
        if (useButton != null)
            useButton.gameObject.SetActive(item.itemType == ItemType.Consumable && !isEquipped);
        if (equipButton != null)
            equipButton.gameObject.SetActive(item.itemType == ItemType.Equipment && !isEquipped);
        if (unequipButton != null)
            unequipButton.gameObject.SetActive(isEquipped);
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
    
    void UseSelectedItem()
    {
        if (selectedItem != null && selectedItem.itemType == ItemType.Consumable && hybridInventoryManager != null)
        {
            hybridInventoryManager.UseItem(selectedItem.itemId);
            HideItemInfo();
        }
    }
    
    void EquipSelectedItem()
    {
        if (selectedItem != null && selectedItem.itemType == ItemType.Equipment && hybridInventoryManager != null)
        {
            hybridInventoryManager.EquipItem(selectedItem.itemId);
            HideItemInfo();
        }
    }
    
    void UnequipSelectedItem()
    {
        if (selectedItem != null && selectedItem.itemType == ItemType.Equipment && hybridInventoryManager != null)
        {
            hybridInventoryManager.UnequipItem(selectedItem.equipmentType);
            HideItemInfo();
        }
    }
    
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
        
        // Clear all slots first
        foreach (var slot in inventorySlots)
        {
            if (slot != null)
                slot.ClearSlot();
        }
        
        // Update inventory slots
        var inventoryItems = hybridInventoryManager.GetInventoryItems();
        if (inventoryItems != null)
        {
            if (showDebugInfo)
            {
                Debug.Log($"Updating {inventoryItems.Count} inventory items in UI");
            }
            
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
                        if (showDebugInfo)
                        {
                            Debug.Log($"Set slot {slotIndex}: {itemData.itemName} x{item.quantity}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"ItemData not found for: {item.itemId}");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("GetInventoryItems returned null");
        }
    }
    
    void RefreshEquipmentSlots()
    {
        if (hybridInventoryManager == null) return;
        
        // Update equipment slots
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
        
        // Show mode info
        statsText += $"<color=#CCCCCC><size=10>Mode: {(hybridInventoryManager.IsNetworkMode() ? "Network" : "Local")}</size></color>";
        
        playerStatsText.text = statsText;
    }
    
    void OnItemEquipped(EquippedItem equippedItem)
    {
        RefreshInventoryUI();
    }
    
    void OnItemUnequipped(EquipmentType equipmentType)
    {
        RefreshInventoryUI();
    }
    
    void ShowInventoryTab()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(true);
        if (equipmentPanel != null)
            equipmentPanel.SetActive(false);
        
        // Update button colors
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
        
        // Update button colors
        if (inventoryTabButton != null)
            inventoryTabButton.GetComponent<Image>().color = Color.white;
        if (equipmentTabButton != null)
            equipmentTabButton.GetComponent<Image>().color = Color.yellow;
    }
    
    // Public methods for external use
    public void ToggleInventory()
    {
        bool isActive = gameObject.activeInHierarchy;
        gameObject.SetActive(!isActive);
        
        if (gameObject.activeInHierarchy)
        {
            RefreshInventoryUI();
        }
    }
    
    public void AddTestItems()
    {
        if (hybridInventoryManager != null)
        {
            hybridInventoryManager.AddItem("health_potion_small", 10);
            hybridInventoryManager.AddItem("mana_potion_small", 5);
            hybridInventoryManager.AddItem("iron_sword", 1);
            hybridInventoryManager.AddItem("leather_armor", 1);
            hybridInventoryManager.AddItem("leather_helmet", 1);
            Debug.Log("Test items added!");
        }
    }
    
    [ContextMenu("Force Refresh UI")]
    public void ForceRefreshUI()
    {
        RefreshInventoryUI();
    }
    
    [ContextMenu("Add Test Items")]
    public void ContextAddTestItems()
    {
        AddTestItems();
    }
    
    [ContextMenu("Debug Inventory State")]
    public void DebugInventoryState()
    {
        if (hybridInventoryManager != null)
        {
            Debug.Log($"=== Inventory Debug Info ===");
            Debug.Log($"Network Mode: {hybridInventoryManager.IsNetworkMode()}");
            Debug.Log($"Data Loaded: {hybridInventoryManager.IsDataLoaded()}");
            
            var items = hybridInventoryManager.GetInventoryItems();
            Debug.Log($"Total Items: {items.Count}");
            
            foreach (var kvp in items)
            {
                Debug.Log($"Slot {kvp.Key}: {kvp.Value.itemId} x{kvp.Value.quantity}");
            }
        }
        else
        {
            Debug.LogError("HybridInventoryManager is null!");
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (hybridInventoryManager != null)
        {
            hybridInventoryManager.OnInventoryChanged -= RefreshInventoryUI;
            hybridInventoryManager.OnItemEquipped -= OnItemEquipped;
            hybridInventoryManager.OnItemUnequipped -= OnItemUnequipped;
        }
    }
}