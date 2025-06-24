using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class InventoryManager : MonoBehaviour
{
    [Header("Inventory Panel")]
    public GameObject inventoryPanel;
    public Button backToLobbyButton;

    [Header("Character Images")]
    public Sprite bloodKnightImage;
    public Sprite archerImage;
    public Sprite assassinImage;
    public Sprite ironJuggernautImage;

    [Header("Character Preview")]
    public Transform characterPreviewParent;
    public Vector3 previewPosition = Vector3.zero;
    public Vector3 previewRotation = new Vector3(0, 180, 0);

    [Header("Character Info Display")]
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI characterLevelText;
    public TextMeshProUGUI characterExpText;
    public Slider expProgressSlider;
    public Image characterDisplayImage;

    [Header("Character Stats Display")]
    public TextMeshProUGUI hpStatText;
    public TextMeshProUGUI manaStatText;
    public TextMeshProUGUI attackStatText;
    public TextMeshProUGUI magicAttackStatText;
    public TextMeshProUGUI armorStatText;
    public TextMeshProUGUI critChanceStatText;
    public TextMeshProUGUI critDamageStatText;
    public TextMeshProUGUI moveSpeedStatText;
    public TextMeshProUGUI hitRateStatText;
    public TextMeshProUGUI evasionRateStatText;
    public TextMeshProUGUI attackSpeedStatText;
    public TextMeshProUGUI reductionCooldownStatText;


    [Header("Item Detail Panel")]
    public ItemDetailPanel itemDetailPanel;


    [Header("Inventory Grid")]
    public InventoryGridManager inventoryGrid;
    [Header("Item Database")]
    public ItemDatabase itemDatabase;
    private CharacterProgressData currentCharacterData;
    private bool isInventoryOpen = false;
    private GameObject currentCharacterPreview;

    void Start()
    {
        SetupButtons();
        HideInventory();
        LoadItemSystem();
        SetupItemDetailPanel();

    }

    void SetupButtons()
    {
        if (backToLobbyButton != null)
            backToLobbyButton.onClick.AddListener(HideInventory);
    }

    public void ShowInventory()
    {
        Debug.Log("📦 ShowInventory called");

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
            isInventoryOpen = true;
            RefreshCharacterInfo();

            // แสดง inventory grid
            if (inventoryGrid != null)
            {
                inventoryGrid.gameObject.SetActive(true);
                inventoryGrid.RefreshAllSlots();
                Debug.Log($"✅ InventoryGrid activated with {inventoryGrid.GetFilledSlotCount()} items");
            }
            else
            {
                Debug.LogError("❌ InventoryGrid is null!");
            }

            // ตรวจสอบ ItemDetailPanel
            if (itemDetailPanel != null)
            {
                Debug.Log("✅ ItemDetailPanel ready for events");
            }
            else
            {
                Debug.LogError("❌ ItemDetailPanel is null!");
            }

            Debug.Log("📦 Inventory panel opened successfully");
        }
        else
        {
            Debug.LogError("❌ inventoryPanel is null!");
        }
    }

    public void HideInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            isInventoryOpen = false;

            // เพิ่ม: ซ่อน item detail panel
            HideItemDetail();

            if (inventoryGrid != null)
                inventoryGrid.gameObject.SetActive(false);

            if (characterDisplayImage != null)
            {
                characterDisplayImage.gameObject.SetActive(false);
            }

            Debug.Log("📦 Inventory panel closed");
        }
    }
    public void RefreshCharacterInfo()
    {
        if (!PersistentPlayerData.Instance.HasValidData())
        {
            Debug.LogWarning("[InventoryManager] Player data not available for refresh");
            LoadFromPlayerPrefs();
            return;
        }

        string activeCharacter = PersistentPlayerData.Instance.GetCurrentActiveCharacter();
        currentCharacterData = PersistentPlayerData.Instance.GetCharacterData(activeCharacter);

        if (currentCharacterData != null)
        {
            UpdateCharacterInfoDisplay();
            UpdateCharacterStatsDisplay();
            UpdateCharacterPreview();
            Debug.Log($"✅ [InventoryManager] Refreshed info for {activeCharacter} - Level {currentCharacterData.currentLevel}");
        }
        else
        {
            Debug.LogWarning($"[InventoryManager] No character data found for {activeCharacter}");
            LoadFromPlayerPrefs();
        }
    }

    private void LoadFromPlayerPrefs()
    {
        // Fallback to PlayerPrefs if PersistentPlayerData is not available
        currentCharacterData = new CharacterProgressData();
        currentCharacterData.characterType = PlayerPrefs.GetString("LastCharacterSelected", "Assassin");
        currentCharacterData.currentLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
        currentCharacterData.currentExp = PlayerPrefs.GetInt("PlayerExp", 0);
        currentCharacterData.expToNextLevel = PlayerPrefs.GetInt("PlayerExpToNext", 100);
        currentCharacterData.totalMaxHp = PlayerPrefs.GetInt("PlayerMaxHp", 70);
        currentCharacterData.totalMaxMana = PlayerPrefs.GetInt("PlayerMaxMana", 40);
        currentCharacterData.totalAttackDamage = PlayerPrefs.GetInt("PlayerAttackDamage", 35);
        currentCharacterData.totalMagicDamage = PlayerPrefs.GetInt("PlayerMagicDamage", 20);
        currentCharacterData.totalArmor = PlayerPrefs.GetInt("PlayerArmor", 2);
        currentCharacterData.totalCriticalChance = PlayerPrefs.GetFloat("PlayerCritChance", 0.1f);
        currentCharacterData.totalCriticalDamageBonus = PlayerPrefs.GetFloat("PlayerCriticalDamageBonus", 0.1f);
        currentCharacterData.totalMoveSpeed = PlayerPrefs.GetFloat("PlayerMoveSpeed", 5f);
        currentCharacterData.totalHitRate = PlayerPrefs.GetFloat("PlayerHitRate", 0.95f);
        currentCharacterData.totalEvasionRate = PlayerPrefs.GetFloat("PlayerEvasionRate", 0.05f);
        currentCharacterData.totalAttackSpeed = PlayerPrefs.GetFloat("PlayerAttackSpeed", 1f);
        currentCharacterData.totalReductionCoolDown = PlayerPrefs.GetFloat("PlayerReductionCoolDown", 0f);

        UpdateCharacterInfoDisplay();
        UpdateCharacterStatsDisplay();
        UpdateCharacterPreview();
        Debug.Log("[InventoryManager] Loaded data from PlayerPrefs as fallback");
    }

    private void UpdateCharacterInfoDisplay()
    {
        if (currentCharacterData == null) return;

        // Character name and type
        if (characterNameText != null)
        {
            string displayName = GetCharacterDisplayName(currentCharacterData.characterType);
            characterNameText.text = displayName;
        }

        // Level
        if (characterLevelText != null)
            characterLevelText.text = $"Level {currentCharacterData.currentLevel}";

        // Experience
        if (characterExpText != null)
            characterExpText.text = $"EXP: {currentCharacterData.currentExp}/{currentCharacterData.expToNextLevel}";

        // Experience progress bar
        if (expProgressSlider != null)
        {
            float progress = currentCharacterData.expToNextLevel > 0 ?
                (float)currentCharacterData.currentExp / currentCharacterData.expToNextLevel : 1f;
            expProgressSlider.value = progress;
        }
    }

    private void UpdateCharacterStatsDisplay()
    {
        if (currentCharacterData == null) return;

        // Basic Stats
        if (hpStatText != null)
            hpStatText.text = $"Hp:{currentCharacterData.totalMaxHp.ToString() }";

        if (manaStatText != null)
            manaStatText.text = $"Mana:{ currentCharacterData.totalMaxMana.ToString()}";

        if (attackStatText != null)
            attackStatText.text = $"Attack:{currentCharacterData.totalAttackDamage.ToString()}";

        if (magicAttackStatText != null)
            magicAttackStatText.text = $"Magic:{ currentCharacterData.totalMagicDamage.ToString()}";

        if (armorStatText != null)
            armorStatText.text = $"Armor:{currentCharacterData.totalArmor.ToString()}";

        // Percentage Stats
        if (critChanceStatText != null)
            critChanceStatText.text = $"Critacal:{currentCharacterData.totalCriticalChance.ToString()}%";

        if (critDamageStatText != null)
            critDamageStatText.text = $"CritDamage:{(currentCharacterData.totalCriticalDamageBonus)}";

        if (hitRateStatText != null)
            hitRateStatText.text = $"HitRate:{(currentCharacterData.totalHitRate)}%";

        if (evasionRateStatText != null)
            evasionRateStatText.text = $"Dodge:{(currentCharacterData.totalEvasionRate)}%";

        // Speed Stats
        if (moveSpeedStatText != null)
            moveSpeedStatText.text = $"MoveSpeed:{ currentCharacterData.totalMoveSpeed.ToString()}";

        if (attackSpeedStatText != null)
            attackSpeedStatText.text = $"AttackSpeed:{ currentCharacterData.totalAttackSpeed.ToString()}%";

        if (reductionCooldownStatText != null)
            reductionCooldownStatText.text = $"CoolDown:{(currentCharacterData.totalReductionCoolDown)}%";
    }

    private string GetCharacterDisplayName(string characterType)
    {
        switch (characterType)
        {
            case "BloodKnight": return "Blood Knight";
            case "Archer": return "Archer";
            case "Assassin": return "Assassin";
            case "IronJuggernaut": return "Iron Juggernaut";
            default: return characterType;
        }
    }

    public bool IsInventoryOpen()
    {
        return isInventoryOpen;
    }

    // Called when application regains focus to refresh data
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && isInventoryOpen)
        {
            StartCoroutine(DelayedRefresh());
        }
    }

    private IEnumerator DelayedRefresh()
    {
        yield return new WaitForSeconds(0.5f);
        RefreshCharacterInfo();
    }
    private void UpdateCharacterPreview()
    {
        if (currentCharacterData == null) return;

        // แสดงรูป Character
        Sprite characterSprite = GetSpriteForCharacter(currentCharacterData.characterType);
        if (characterSprite != null && characterDisplayImage != null)
        {
            characterDisplayImage.sprite = characterSprite;
            characterDisplayImage.gameObject.SetActive(true);
            Debug.Log($"✅ [InventoryManager] Character image updated: {currentCharacterData.characterType}");
        }
        else
        {
            Debug.LogWarning($"[InventoryManager] Missing sprite or image component for {currentCharacterData.characterType}");
            if (characterDisplayImage != null)
                characterDisplayImage.gameObject.SetActive(false);
        }
    }


    private Sprite GetSpriteForCharacter(string characterType)
    {
        switch (characterType)
        {
            case "BloodKnight":
                return bloodKnightImage;
            case "Archer":
                return archerImage;
            case "Assassin":
                return assassinImage;
            case "IronJuggernaut":
                return ironJuggernautImage;
            default:
                Debug.LogWarning($"[InventoryManager] Unknown character type: {characterType}");
                return assassinImage; // Fallback to Assassin
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (itemDetailPanel != null)
        {
            ItemDetailPanel.OnEquipRequested -= HandleEquipRequest;
            ItemDetailPanel.OnUnequipRequested -= HandleUnequipRequest;
        }
    }
    void LoadItemSystem()
    {
        // โหลด ItemDatabase
        if (itemDatabase == null)
            itemDatabase = ItemDatabase.Instance;

        // ตรวจสอบว่า InventoryGrid มี ItemDatabase
        if (inventoryGrid != null)
        {
            inventoryGrid.itemDatabase = itemDatabase;
            inventoryGrid.LoadItemDatabase(); // เรียก method ใหม่ที่เพิ่มไป
        }

        if (itemDatabase != null)
        {
            Debug.Log($"✅ Item system loaded with {itemDatabase.GetAllItems().Count} items");
        }
    }

    public void AddItemToInventory(ItemData item)
    {
        if (inventoryGrid != null && item != null)
        {
            bool added = inventoryGrid.AddItem(item);
            if (added)
            {
                Debug.Log($"✅ Added {item.ItemName} to inventory");

                // Refresh character info หลังจากเพิ่ม item
                if (isInventoryOpen)
                {
                    RefreshCharacterInfo();
                }
            }
            else
            {
                Debug.LogWarning($"❌ Failed to add {item.ItemName} - inventory full");
            }
        }
    }

    public void AddItemToInventory(string itemId)
    {
        if (itemDatabase != null)
        {
            ItemData item = itemDatabase.GetItemById(itemId);
            if (item != null)
            {
                AddItemToInventory(item);
            }
            else
            {
                Debug.LogWarning($"❌ Item with ID '{itemId}' not found in database");
            }
        }
    }
    #region Item Detail Panel
    void SetupItemDetailPanel()
    {
        Debug.Log("🔧 Setting up ItemDetailPanel...");

        // หา ItemDetailPanel ถ้าไม่ได้ assign
        if (itemDetailPanel == null)
        {
            itemDetailPanel = FindObjectOfType<ItemDetailPanel>();
            Debug.Log($"🔍 FindObjectOfType result: {(itemDetailPanel != null ? "Found" : "Not Found")}");
        }

        if (itemDetailPanel != null)
        {
            // Subscribe to events
            ItemDetailPanel.OnEquipRequested += HandleEquipRequest;
            ItemDetailPanel.OnUnequipRequested += HandleUnequipRequest;

            // เรียก debug method เพื่อตรวจสอบสถานะ
            itemDetailPanel.DebugPanelState();

            Debug.Log("✅ Item detail panel connected");
        }
        else
        {
            Debug.LogError("❌ ItemDetailPanel not found! Make sure it exists in the scene.");
        }
    }

    void HandleEquipRequest(ItemData item, int slotIndex)
    {
        if (item == null) return;

        Debug.Log($"🎽 Equip request: {item.ItemName} from slot {slotIndex}");

        // TODO: Step 6 จะ implement การ equip จริง
        // ตอนนี้แค่แสดง log
        Debug.Log($"✅ {item.ItemName} equipped! (Step 6 will implement actual equipping)");

        // ปิด detail panel หลัง equip
        if (itemDetailPanel != null)
        {
            itemDetailPanel.HidePanel();
        }

        // Refresh inventory display
        RefreshInventoryVisuals();
    }

    void HandleUnequipRequest(ItemData item, int slotIndex)
    {
        if (item == null) return;

        Debug.Log($"🎽 Unequip request: {item.ItemName}");

        // TODO: Step 6 จะ implement การ unequip จริง
        Debug.Log($"✅ {item.ItemName} unequipped! (Step 6 will implement actual unequipping)");

        // ปิด detail panel หลัง unequip
        if (itemDetailPanel != null)
        {
            itemDetailPanel.HidePanel();
        }

        // Refresh inventory display
        RefreshInventoryVisuals();
    }

    public void ShowItemDetail(ItemData item, int slotIndex)
    {
        if (itemDetailPanel != null && item != null)
        {
            itemDetailPanel.ShowItemDetail(item, slotIndex);
        }
    }

    public void HideItemDetail()
    {
        if (itemDetailPanel != null)
        {
            itemDetailPanel.HidePanel();
        }
    }
    public bool RemoveItemFromInventory(int slotIndex)
    {
        if (inventoryGrid != null)
        {
            bool removed = inventoryGrid.RemoveItem(slotIndex);
            if (removed && isInventoryOpen)
            {
                RefreshCharacterInfo();
            }
            return removed;
        }
        return false;
    }

    public ItemData GetItemInSlot(int slotIndex)
    {
        if (inventoryGrid != null)
        {
            return inventoryGrid.GetItemInSlot(slotIndex);
        }
        return null;
    }

    public void RefreshInventoryVisuals()
    {
        if (inventoryGrid != null)
        {
            inventoryGrid.RefreshAllSlots();
        }
    }

    #endregion

    [ContextMenu("Test - Add Random Items")]
    public void TestAddRandomItems()
    {
        if (inventoryGrid != null)
        {
            inventoryGrid.TestFillRandomItems();
        }
    }

    [ContextMenu("Test - Clear Inventory")]
    public void TestClearInventory()
    {
        if (inventoryGrid != null)
        {
            inventoryGrid.TestClearAllItems();
        }
    }

    [ContextMenu("Test - Add Legendary Item")]
    public void TestAddLegendaryItem()
    {
        if (itemDatabase != null)
        {
            var legendaryItems = itemDatabase.GetItemsByTier(ItemTier.Legendary);
            if (legendaryItems.Count > 0)
            {
                AddItemToInventory(legendaryItems[0]);
            }
            else
            {
                Debug.LogWarning("❌ No legendary items found in database");
            }
        }
    }

    [ContextMenu("Test - Debug Inventory State")]
    public void TestDebugInventoryState()
    {
        if (inventoryGrid != null && inventoryGrid.slotItemIds != null)
        {
            int filledSlots = 0;
            for (int i = 0; i < inventoryGrid.slotItemIds.Count; i++)
            {
                string itemId = inventoryGrid.slotItemIds[i];
                if (!string.IsNullOrEmpty(itemId))
                {
                    ItemData item = inventoryGrid.GetItemInSlot(i);
                    string itemName = item?.ItemName ?? "Unknown";
                    Debug.Log($"📦 Slot {i}: {itemName} ({itemId})");
                    filledSlots++;
                }
            }
            Debug.Log($"📊 Inventory Summary: {filledSlots}/{inventoryGrid.totalSlots} slots filled");
        }
    }

    [ContextMenu("Test - Show Detail Panel")]
    public void TestShowDetailPanel()
    {
        if (itemDatabase != null && itemDetailPanel != null)
        {
            var testItem = itemDatabase.GetRandomItem();
            if (testItem != null)
            {
                itemDetailPanel.ShowItemDetail(testItem, 0);
                Debug.Log($"📋 Showing detail for test item: {testItem.ItemName}");
            }
        }
    }

    [ContextMenu("Test - Hide Detail Panel")]
    public void TestHideDetailPanel()
    {
        HideItemDetail();
    }

    [ContextMenu("Test - Manual Show Detail Panel")]
    public void TestManualShowDetailPanel()
    {
        Debug.Log("🧪 Testing manual show detail panel...");

        if (itemDetailPanel == null)
        {
            Debug.LogError("❌ itemDetailPanel is null!");
            return;
        }

        if (itemDatabase == null)
        {
            Debug.LogError("❌ itemDatabase is null!");
            return;
        }

        var testItem = itemDatabase.GetRandomItem();
        if (testItem != null)
        {
            Debug.Log($"📋 Manually showing detail for: {testItem.ItemName}");
            itemDetailPanel.ShowItemDetail(testItem, 0);
        }
        else
        {
            Debug.LogError("❌ No test item available!");
        }
    }

    [ContextMenu("Test - Check All Systems")]
    public void TestCheckAllSystems()
    {
        Debug.Log("🔍 === SYSTEM CHECK ===");

        // Check ItemDetailPanel
        Debug.Log($"ItemDetailPanel: {(itemDetailPanel != null ? "✅ OK" : "❌ NULL")}");
        if (itemDetailPanel != null)
        {
            itemDetailPanel.DebugPanelState();
        }

        // Check InventoryGrid
        Debug.Log($"InventoryGrid: {(inventoryGrid != null ? "✅ OK" : "❌ NULL")}");
        if (inventoryGrid != null)
        {
            Debug.Log($"   Total slots: {inventoryGrid.allSlots?.Count ?? 0}");
            Debug.Log($"   Filled slots: {inventoryGrid.GetFilledSlotCount()}");
        }

        // Check ItemDatabase
        Debug.Log($"ItemDatabase: {(itemDatabase != null ? "✅ OK" : "❌ NULL")}");
        if (itemDatabase != null)
        {
            Debug.Log($"   Total items: {itemDatabase.GetAllItems().Count}");
        }

        // Check Events
        Debug.Log("Event subscriptions:");
        Debug.Log($"   OnEquipRequested: {(ItemDetailPanel.OnEquipRequested != null ? "✅ Subscribed" : "❌ No subscribers")}");
        Debug.Log($"   OnUnequipRequested: {(ItemDetailPanel.OnUnequipRequested != null ? "✅ Subscribed" : "❌ No subscribers")}");

        Debug.Log("🔍 === END SYSTEM CHECK ===");
    }

    // 🔧 เพิ่ม test สำหรับ event chain
    [ContextMenu("Test - Event Chain")]
    public void TestEventChain()
    {
        Debug.Log("🧪 Testing event chain...");

        if (inventoryGrid != null && inventoryGrid.allSlots != null)
        {
            // หา slot ที่มี item
            for (int i = 0; i < inventoryGrid.allSlots.Count; i++)
            {
                var slot = inventoryGrid.allSlots[i];
                if (slot != null && slot.HasItem())
                {
                    Debug.Log($"🎯 Testing slot {i} with item: {slot.GetItem()?.ItemName}");
                    slot.TestFireSelectionEvent(); // ใช้ method ที่เพิ่มใน InventorySlot
                    break;
                }
            }
        }
        else
        {
            Debug.LogError("❌ No inventory grid or slots available for testing!");
        }
    }
}