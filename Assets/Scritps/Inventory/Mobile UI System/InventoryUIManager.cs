using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryUIManager : MonoBehaviour
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

    private InventoryManager inventoryManager;
    private ItemDatabase itemDatabase;
    private List<InventorySlot> inventorySlots = new List<InventorySlot>();
    private Dictionary<EquipmentType, InventorySlot> equipmentSlots = new Dictionary<EquipmentType, InventorySlot>();
    private ItemData selectedItem;
    private int selectedSlotIndex = -1;

    void Start()
    {
        InitializeUI();
        FindInventoryManager();
    }

    void InitializeUI()
    {
        // Setup tab buttons
        inventoryTabButton.onClick.AddListener(() => ShowInventoryTab());
        equipmentTabButton.onClick.AddListener(() => ShowEquipmentTab());

        // Setup item info buttons
        useButton.onClick.AddListener(UseSelectedItem);
        equipButton.onClick.AddListener(EquipSelectedItem);
        unequipButton.onClick.AddListener(UnequipSelectedItem);

        // Initialize equipment slots dictionary
        equipmentSlots[EquipmentType.Weapon] = weaponSlot;
        equipmentSlots[EquipmentType.Helmet] = helmetSlot;
        equipmentSlots[EquipmentType.Armor] = armorSlot;
        equipmentSlots[EquipmentType.Pants] = pantsSlot;
        equipmentSlots[EquipmentType.Boots] = bootsSlot;

        // Setup equipment slot callbacks
        foreach (var kvp in equipmentSlots)
        {
            var equipType = kvp.Key;
            var slot = kvp.Value;
            slot.SetEquipmentType(equipType);
            slot.OnSlotClicked += (slotData) => OnEquipmentSlotClicked(equipType);
        }

        // Create inventory slots
        CreateInventorySlots();

        // Hide info panel initially
        itemInfoPanel.SetActive(false);

        // Show inventory tab by default
        ShowInventoryTab();
    }

    void FindInventoryManager()
    {
        // Find inventory manager from local player
        var localPlayer = FindObjectOfType<NetworkPlayerManager>();
        if (localPlayer != null && localPlayer.HasInputAuthority)
        {
            inventoryManager = localPlayer.GetComponent<InventoryManager>();
            if (inventoryManager != null)
            {
                itemDatabase = inventoryManager.itemDatabase;
                SubscribeToInventoryEvents();
                RefreshInventoryUI();
            }
        }
    }

    void CreateInventorySlots()
    {
        for (int i = 0; i < 30; i++) // Max inventory slots
        {
            GameObject slotObj = Instantiate(inventorySlotPrefab, inventoryGrid);
            InventorySlot slot = slotObj.GetComponent<InventorySlot>();
            slot.SetSlotIndex(i);
            slot.OnSlotClicked += OnInventorySlotClicked;
            inventorySlots.Add(slot);
        }
    }

    void SubscribeToInventoryEvents()
    {
        if (inventoryManager == null) return;

        inventoryManager.OnInventoryChanged += RefreshInventoryUI;
        inventoryManager.OnItemEquipped += OnItemEquipped;
        inventoryManager.OnItemUnequipped += OnItemUnequipped;
    }

    void OnInventorySlotClicked(InventorySlotData slotData)
    {
        selectedSlotIndex = slotData.slotIndex;

        if (!string.IsNullOrEmpty(slotData.itemId))
        {
            selectedItem = itemDatabase.GetItem(slotData.itemId);
            ShowItemInfo(selectedItem, slotData.quantity);
        }
        else
        {
            HideItemInfo();
        }
    }

    void OnEquipmentSlotClicked(EquipmentType equipmentType)
    {
        var equippedItem = inventoryManager.GetEquippedItem(equipmentType);
        if (equippedItem != null)
        {
            selectedItem = itemDatabase.GetItem(equippedItem.itemId);
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
        if (item == null) return;

        itemInfoPanel.SetActive(true);
        itemInfoIcon.sprite = item.icon;
        itemInfoName.text = item.itemName;
        itemInfoName.color = item.GetRarityColor();
        itemInfoDescription.text = item.description;

        // Show stats
        string statsText = "";
        if (item.itemType == ItemType.Equipment)
        {
            if (item.stats.attackDamage > 0)
                statsText += $"Attack Damage: +{item.stats.attackDamage}\n";
            if (item.stats.armor > 0)
                statsText += $"Armor: +{item.stats.armor}\n";
            if (item.stats.maxHp > 0)
                statsText += $"Max HP: +{item.stats.maxHp}\n";
            if (item.stats.maxMana > 0)
                statsText += $"Max Mana: +{item.stats.maxMana}\n";
            if (item.stats.moveSpeed > 0)
                statsText += $"Move Speed: +{item.stats.moveSpeed:F1}\n";
            if (item.stats.criticalChance > 0)
                statsText += $"Critical Chance: +{item.stats.criticalChance:F1}%\n";
        }
        else if (item.itemType == ItemType.Consumable)
        {
            if (item.healAmount > 0)
                statsText += $"Heal: +{item.healAmount} HP\n";
            if (item.manaAmount > 0)
                statsText += $"Mana: +{item.manaAmount} MP\n";
        }

        itemInfoStats.text = statsText;

        // Setup buttons
        useButton.gameObject.SetActive(item.itemType == ItemType.Consumable && !isEquipped);
        equipButton.gameObject.SetActive(item.itemType == ItemType.Equipment && !isEquipped);
        unequipButton.gameObject.SetActive(isEquipped);
    }

    void HideItemInfo()
    {
        itemInfoPanel.SetActive(false);
        selectedItem = null;
        selectedSlotIndex = -1;
    }

    void UseSelectedItem()
    {
        if (selectedItem != null && selectedItem.itemType == ItemType.Consumable)
        {
            inventoryManager.UseItem(selectedItem.itemId);
            HideItemInfo();
        }
    }

    void EquipSelectedItem()
    {
        if (selectedItem != null && selectedItem.itemType == ItemType.Equipment)
        {
            inventoryManager.EquipItem(selectedItem.itemId);
            HideItemInfo();
        }
    }

    void UnequipSelectedItem()
    {
        if (selectedItem != null && selectedItem.itemType == ItemType.Equipment)
        {
            inventoryManager.UnequipItem(selectedItem.equipmentType);
            HideItemInfo();
        }
    }

    void RefreshInventoryUI()
    {
        if (inventoryManager == null) return;

        // Clear all slots first
        foreach (var slot in inventorySlots)
        {
            slot.ClearSlot();
        }

        // Update inventory slots
        var inventoryItems = inventoryManager.GetInventoryItems();
        foreach (var kvp in inventoryItems)
        {
            int slotIndex = kvp.Key;
            var item = kvp.Value;

            if (slotIndex < inventorySlots.Count)
            {
                ItemData itemData = itemDatabase.GetItem(item.itemId);
                if (itemData != null)
                {
                    inventorySlots[slotIndex].SetItem(itemData, item.quantity);
                }
            }
        }

        // Update equipment slots
        foreach (var kvp in equipmentSlots)
        {
            EquipmentType equipType = kvp.Key;
            InventorySlot slot = kvp.Value;

            var equippedItem = inventoryManager.GetEquippedItem(equipType);
            if (equippedItem != null)
            {
                ItemData itemData = itemDatabase.GetItem(equippedItem.itemId);
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

        // Update player stats
        UpdatePlayerStatsDisplay();
    }

    void UpdatePlayerStatsDisplay()
    {
        if (inventoryManager == null) return;

        ItemStats totalStats = inventoryManager.GetTotalEquipmentStats();

        string statsText = "Equipment Bonuses:\n";
        statsText += $"Attack Damage: +{totalStats.attackDamage}\n";
        statsText += $"Armor: +{totalStats.armor}\n";
        statsText += $"Max HP: +{totalStats.maxHp}\n";
        statsText += $"Max Mana: +{totalStats.maxMana}\n";
        statsText += $"Move Speed: +{totalStats.moveSpeed:F1}\n";
        statsText += $"Critical Chance: +{totalStats.criticalChance:F1}%\n";
        statsText += $"Critical Damage: +{totalStats.criticalDamage:F1}%";

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
        inventoryPanel.SetActive(true);
        equipmentPanel.SetActive(false);

        // Update button colors
        inventoryTabButton.GetComponent<Image>().color = Color.yellow;
        equipmentTabButton.GetComponent<Image>().color = Color.white;
    }

    void ShowEquipmentTab()
    {
        inventoryPanel.SetActive(false);
        equipmentPanel.SetActive(true);

        // Update button colors
        inventoryTabButton.GetComponent<Image>().color = Color.white;
        equipmentTabButton.GetComponent<Image>().color = Color.yellow;
    }

    // Public methods for external use
    public void ToggleInventory()
    {
        bool isActive = inventoryPanel.activeInHierarchy || equipmentPanel.activeInHierarchy;
        gameObject.SetActive(!isActive);

        if (gameObject.activeInHierarchy)
        {
            RefreshInventoryUI();
        }
    }

    public void AddTestItems()
    {
        if (inventoryManager != null)
        {
            // Add some test items
            inventoryManager.AddItem("health_potion", 5);
            inventoryManager.AddItem("mana_potion", 3);
            inventoryManager.AddItem("iron_sword", 1);
            inventoryManager.AddItem("leather_armor", 1);
        }
    }

    void OnDestroy()
    {
        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryChanged -= RefreshInventoryUI;
            inventoryManager.OnItemEquipped -= OnItemEquipped;
            inventoryManager.OnItemUnequipped -= OnItemUnequipped;
        }
    }
}
