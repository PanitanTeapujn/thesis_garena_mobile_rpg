using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryUIManager : MonoBehaviour
{
    [Header("Toggle Reference")]
    public InventoryToggleButton toggleButton;

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

    [Header("Audio")]
    public AudioClip clickSound;
    public AudioClip equipSound;
    public AudioClip useItemSound;

    private InventoryManager inventoryManager;
    private ItemDatabase itemDatabase;
    private List<InventorySlot> inventorySlots = new List<InventorySlot>();
    private Dictionary<EquipmentType, InventorySlot> equipmentSlots = new Dictionary<EquipmentType, InventorySlot>();
    private ItemData selectedItem;
    private int selectedSlotIndex = -1;
    private bool isInventoryOpen = false;
    private AudioSource audioSource;

    void Start()
    {
        InitializeAudio();
        InitializeUI();
        FindInventoryManager();
    }

    void Update()
    {
        // ปิด Inventory เมื่อกด ESC
        if (Input.GetKeyDown(KeyCode.Escape) && gameObject.activeInHierarchy)
        {
            if (toggleButton != null)
            {
                toggleButton.CloseInventory();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        // ตรวจสอบการกดปุ่มเพื่อเปิดปิด Inventory
        if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }
    }

    void InitializeAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = 0.7f;
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
                return;
            }
        }

        // Fallback: หาจาก InventoryManager ทั่วไป
        if (inventoryManager == null)
        {
            var allManagers = FindObjectsOfType<InventoryManager>();
            foreach (var manager in allManagers)
            {
                if (manager.HasInputAuthority)
                {
                    inventoryManager = manager;
                    if (inventoryManager != null)
                    {
                        itemDatabase = inventoryManager.itemDatabase;
                        SubscribeToInventoryEvents();
                        RefreshInventoryUI();
                    }
                    break;
                }
            }
        }

        // ถ้ายังไม่เจอ ให้ลองหาใหม่ในอีก 1 วินาที
        if (inventoryManager == null)
        {
            Invoke(nameof(FindInventoryManager), 1f);
        }
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
    }

    void SubscribeToInventoryEvents()
    {
        if (inventoryManager == null) return;

        inventoryManager.OnInventoryChanged += RefreshInventoryUI;
        inventoryManager.OnItemEquipped += OnItemEquipped;
        inventoryManager.OnItemUnequipped += OnItemUnequipped;
    }

    void UnsubscribeFromInventoryEvents()
    {
        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryChanged -= RefreshInventoryUI;
            inventoryManager.OnItemEquipped -= OnItemEquipped;
            inventoryManager.OnItemUnequipped -= OnItemUnequipped;
        }
    }

    void OnInventorySlotClicked(InventorySlotData slotData)
    {
        PlayClickSound();
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
        PlayClickSound();
        if (inventoryManager == null) return;

        var equippedItem = inventoryManager.GetEquippedItem(equipmentType);
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

        // Set item icon
        if (itemInfoIcon != null)
            itemInfoIcon.sprite = item.icon;

        // Set item name with rarity color
        if (itemInfoName != null)
        {
            itemInfoName.text = item.itemName;
            itemInfoName.color = item.GetRarityColor();
        }

        // Set description
        if (itemInfoDescription != null)
            itemInfoDescription.text = item.description;

        // Build stats text
        if (itemInfoStats != null)
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
                if (item.stats.attackSpeed > 0)
                    statsText += $"<color=#DDA0DD>⚡ Attack Speed: +{item.stats.attackSpeed:F2}</color>\n";
                if (item.stats.criticalChance > 0)
                    statsText += $"<color=#FFFF00>⚡ Critical Chance: +{item.stats.criticalChance:F1}%</color>\n";
                if (item.stats.criticalDamage > 0)
                    statsText += $"<color=#FFA500>💥 Critical Damage: +{item.stats.criticalDamage:F1}%</color>\n";
            }
            else if (item.itemType == ItemType.Consumable)
            {
                statsText += "<color=#00FF00><b>💊 Consumable Effects:</b></color>\n";
                if (item.healAmount > 0)
                    statsText += $"<color=#FF69B4>❤️ Heal: +{item.healAmount} HP</color>\n";
                if (item.manaAmount > 0)
                    statsText += $"<color=#87CEEB>💙 Restore: +{item.manaAmount} MP</color>\n";
                if (item.buffDuration > 0)
                    statsText += $"<color=#DDA0DD>⏰ Duration: {item.buffDuration}s</color>\n";
            }

            // Additional info
            statsText += "\n<color=#CCCCCC><b>📋 Item Info:</b></color>\n";
            if (quantity > 1)
                statsText += $"<color=#FFFFFF>📦 Quantity: {quantity}</color>\n";
            statsText += $"<color={GetRarityColorHex(item.rarity)}>💎 Rarity: {item.rarity}</color>\n";
            statsText += $"<color=#FFD700>💰 Value: {item.sellPrice} Gold</color>";

            itemInfoStats.text = statsText;
        }

        // Setup action buttons
        if (useButton != null)
            useButton.gameObject.SetActive(item.itemType == ItemType.Consumable && !isEquipped);
        if (equipButton != null)
            equipButton.gameObject.SetActive(item.itemType == ItemType.Equipment && !isEquipped);
        if (unequipButton != null)
            unequipButton.gameObject.SetActive(isEquipped);
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
        if (selectedItem != null && selectedItem.itemType == ItemType.Consumable && inventoryManager != null)
        {
            PlayUseItemSound();
            inventoryManager.UseItem(selectedItem.itemId);
            HideItemInfo();
        }
    }

    void EquipSelectedItem()
    {
        if (selectedItem != null && selectedItem.itemType == ItemType.Equipment && inventoryManager != null)
        {
            PlayEquipSound();
            inventoryManager.EquipItem(selectedItem.itemId);
            HideItemInfo();
        }
    }

    void UnequipSelectedItem()
    {
        if (selectedItem != null && selectedItem.itemType == ItemType.Equipment && inventoryManager != null)
        {
            PlayClickSound();
            inventoryManager.UnequipItem(selectedItem.equipmentType);
            HideItemInfo();
        }
    }

    public void RefreshInventoryUI()
    {
        if (inventoryManager == null) return;

        RefreshInventorySlots();
        RefreshEquipmentSlots();
        UpdatePlayerStatsDisplay();
    }

    void RefreshInventorySlots()
    {
        if (inventoryManager == null || inventorySlots == null) return;

        // Clear all slots first
        foreach (var slot in inventorySlots)
        {
            if (slot != null)
                slot.ClearSlot();
        }

        // Update inventory slots
        var inventoryItems = inventoryManager.GetInventoryItems();
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
        if (inventoryManager == null) return;

        // Update equipment slots
        foreach (var kvp in equipmentSlots)
        {
            EquipmentType equipType = kvp.Key;
            InventorySlot slot = kvp.Value;

            if (slot != null)
            {
                var equippedItem = inventoryManager.GetEquippedItem(equipType);
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
        if (inventoryManager == null || playerStatsText == null) return;

        ItemStats totalStats = inventoryManager.GetTotalEquipmentStats();

        string statsText = "<color=#FFD700><size=16><b>📊 Equipment Bonuses</b></size></color>\n\n";

        // Combat Stats
        statsText += "<color=#FF6B6B><b>⚔️ Combat:</b></color>\n";
        statsText += $"   Attack Damage: <color=#FFFFFF>+{totalStats.attackDamage}</color>\n";
        statsText += $"   Critical Chance: <color=#FFFF00>+{totalStats.criticalChance:F1}%</color>\n";
        statsText += $"   Critical Damage: <color=#FFA500>+{totalStats.criticalDamage:F1}%</color>\n";
        statsText += $"   Attack Speed: <color=#DDA0DD>+{totalStats.attackSpeed:F2}</color>\n\n";

        // Defense Stats
        statsText += "<color=#4ECDC4><b>🛡️ Defense:</b></color>\n";
        statsText += $"   Armor: <color=#FFFFFF>+{totalStats.armor}</color>\n";
        statsText += $"   Max HP: <color=#FF69B4>+{totalStats.maxHp}</color>\n\n";

        // Utility Stats
        statsText += "<color=#98FB98><b>🏃 Utility:</b></color>\n";
        statsText += $"   Move Speed: <color=#FFFFFF>+{totalStats.moveSpeed:F1}</color>\n";
        statsText += $"   Max Mana: <color=#87CEEB>+{totalStats.maxMana}</color>\n\n";

        // Calculate and show total power rating
        int combatPower = totalStats.attackDamage + (int)(totalStats.criticalChance * 2) + (int)(totalStats.criticalDamage);
        int defensePower = totalStats.armor + (totalStats.maxHp / 10);
        int utilityPower = (int)(totalStats.moveSpeed * 5) + (totalStats.maxMana / 20);
        int totalPower = combatPower + defensePower + utilityPower;

        statsText += "<color=#FFD700><size=14><b>🔥 Power Rating</b></size></color>\n";
        statsText += $"<color=#FF6B6B>Combat: {combatPower}</color> | ";
        statsText += $"<color=#4ECDC4>Defense: {defensePower}</color> | ";
        statsText += $"<color=#98FB98>Utility: {utilityPower}</color>\n";
        statsText += $"<color=#FFD700><b>Total: {totalPower}</b></color>";

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
        PlayClickSound();

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
        PlayClickSound();

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

    // Audio methods
    void PlayClickSound()
    {
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }

    void PlayEquipSound()
    {
        if (audioSource != null && equipSound != null)
        {
            audioSource.PlayOneShot(equipSound);
        }
    }

    void PlayUseItemSound()
    {
        if (audioSource != null && useItemSound != null)
        {
            audioSource.PlayOneShot(useItemSound);
        }
    }

    // Public methods for external use
    public void ToggleInventory()
    {
        if (toggleButton != null)
        {
            toggleButton.ToggleInventory();
        }
        else
        {
            // Fallback method
            bool isActive = gameObject.activeInHierarchy;
            SetInventoryState(!isActive);
        }
    }

    public void OpenInventory()
    {
        if (toggleButton != null)
        {
            toggleButton.OpenInventory();
        }
        else
        {
            SetInventoryState(true);
        }
    }

    public void CloseInventory()
    {
        if (toggleButton != null)
        {
            toggleButton.CloseInventory();
        }
        else
        {
            SetInventoryState(false);
        }
    }

    private void SetInventoryState(bool isOpen)
    {
        isInventoryOpen = isOpen;
        gameObject.SetActive(isOpen);

        if (isOpen)
        {
            RefreshInventoryUI();
        }

        // จัดการ Cursor และ Input
        HandleGameplayInput(isOpen);
    }

    private void HandleGameplayInput(bool isUIOpen)
    {
        // ปิด/เปิด gameplay input เมื่อเปิด UI
        var inputController = FindObjectOfType<SingleInputController>();
        if (inputController != null)
        {
            inputController.enabled = !isUIOpen;
        }

        // จัดการ Cursor (สำหรับ PC)
#if !UNITY_ANDROID && !UNITY_IOS
        if (isUIOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
#endif
    }

    // Testing and utility methods
    public void AddTestItems()
    {
        if (inventoryManager != null && inventoryManager.HasInputAuthority)
        {
            // Add various test items
            inventoryManager.AddItem("health_potion_small", 10);
           // inventoryManager.AddItem("mana_potion_small", 5);
            inventoryManager.AddItem("iron_sword", 1);
            inventoryManager.AddItem("leather_armor", 1);
            inventoryManager.AddItem("leather_helmet", 1);
            inventoryManager.AddItem("leather_pants", 1);
            inventoryManager.AddItem("leather_boots", 1);
           // inventoryManager.AddItem("steel_sword", 1);
          //  inventoryManager.AddItem("chainmail", 1);

            Debug.Log("Test items added to inventory!");
            RefreshInventoryUI();
        }
        else
        {
            Debug.LogWarning("InventoryManager not found or no input authority!");
        }
    }

    [ContextMenu("Add Test Items")]
    public void ContextMenuAddTestItems()
    {
        AddTestItems();
    }

    [ContextMenu("Refresh UI")]
    public void ContextMenuRefreshUI()
    {
        RefreshInventoryUI();
    }

    [ContextMenu("Clear All Slots")]
    public void ContextMenuClearSlots()
    {
        foreach (var slot in inventorySlots)
        {
            if (slot != null)
                slot.ClearSlot();
        }

        foreach (var slot in equipmentSlots.Values)
        {
            if (slot != null)
                slot.ClearSlot();
        }
    }

    public bool IsInventoryOpen()
    {
        return isInventoryOpen;
    }

    public InventoryManager GetInventoryManager()
    {
        return inventoryManager;
    }

    public ItemDatabase GetItemDatabase()
    {
        return itemDatabase;
    }

    void OnDestroy()
    {
        UnsubscribeFromInventoryEvents();
    }

    void OnDisable()
    {
        isInventoryOpen = false;
        HideItemInfo();
    }

    void OnEnable()
    {
        if (inventoryManager == null)
        {
            FindInventoryManager();
        }

        RefreshInventoryUI();
        isInventoryOpen = true;
    }
}
