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

    [Header("Equipment System")]
    public EquipmentSlotsManager equipmentSlots;

    [Header("Item Detail Panel")]
    public ItemDetailPanel itemDetailPanel;

    [Header("Inventory Grid")]
    public InventoryGridManager inventoryGrid;

    [Header("Item Database")]
    public ItemDatabase itemDatabase;

    private CharacterProgressData currentCharacterData;
    private bool isInventoryOpen = false;
    private GameObject currentCharacterPreview;

    // ✅ เพิ่ม: Event setup tracking
    private bool eventsSetupComplete = false;

    void Start()
    {
        SetupButtons();
        HideInventory();
        LoadItemSystem();

        // ✅ Setup systems ตามลำดับ
        StartCoroutine(SetupSystemsSequentially());
    }

    // ✅ ใหม่: Sequential system setup
    private IEnumerator SetupSystemsSequentially()
    {
        // รอให้ทุกอย่าง load เสร็จก่อน
        yield return new WaitForSeconds(0.1f);

        SetupEquipmentSystem();
        yield return new WaitForSeconds(0.1f);

        SetupItemDetailPanel();
        yield return new WaitForSeconds(0.1f);

        // ✅ Verify setup
        VerifyAllSystems();

        eventsSetupComplete = true;
        Debug.Log("✅ All systems setup completed");
    }

    void SetupButtons()
    {
        if (backToLobbyButton != null)
            backToLobbyButton.onClick.AddListener(HideInventory);
    }

    void SetupEquipmentSystem()
    {
        if (equipmentSlots == null)
            equipmentSlots = FindObjectOfType<EquipmentSlotsManager>();

        if (equipmentSlots != null)
        {
            // ✅ Unsubscribe first เพื่อป้องกัน duplicates
            EquipmentSlotsManager.OnItemEquipped -= HandleItemEquipped;
            EquipmentSlotsManager.OnItemUnequipped -= HandleItemUnequipped;
            EquipmentSlotsManager.OnEquipmentChanged -= HandleEquipmentChanged;

            // Subscribe to equipment events
            EquipmentSlotsManager.OnItemEquipped += HandleItemEquipped;
            EquipmentSlotsManager.OnItemUnequipped += HandleItemUnequipped;
            EquipmentSlotsManager.OnEquipmentChanged += HandleEquipmentChanged;

            Debug.Log("✅ Equipment system connected");
        }
        else
        {
            Debug.LogWarning("⚠️ EquipmentSlotsManager not found!");
        }
    }

    void HandleItemEquipped(ItemData item, ItemType slotType)
    {
        Debug.Log($"🎽 Item equipped: {item.ItemName} in {slotType} slot");

        // ✅ Delayed refresh เพื่อให้ระบบ update เสร็จก่อน
        StartCoroutine(DelayedUIRefresh($"item equipped: {item.ItemName}"));

        HideItemDetail();
        RefreshInventoryVisuals();
    }

    void HandleItemUnequipped(ItemData item, ItemType slotType)
    {
        Debug.Log($"🔧 Item unequipped: {item.ItemName} from {slotType} slot");

        // ✅ Delayed refresh เพื่อให้ระบบ update เสร็จก่อน
        StartCoroutine(DelayedUIRefresh($"item unequipped: {item.ItemName}"));

        HideItemDetail();
        RefreshInventoryVisuals();

        if (inventoryGrid != null)
        {
            inventoryGrid.RefreshAllSlots();
            Debug.Log("🔄 Forced inventory refresh after unequip");
        }

        if (equipmentSlots != null && !equipmentSlots.IsVisible())
        {
            equipmentSlots.ShowEquipmentSlots();
            Debug.Log("🔄 Re-showed equipment slots after unequip");
        }
    }

    void HandleEquipmentChanged()
    {
        Debug.Log("🔄 Equipment changed - refreshing UI");
        if (isInventoryOpen)
        {
            // ✅ Delayed refresh เพื่อให้ระบบ update เสร็จก่อน
            StartCoroutine(DelayedUIRefresh("equipment changed"));
        }
    }

    // ✅ ใหม่: Delayed UI refresh coroutine
    private IEnumerator DelayedUIRefresh(string reason)
    {
        Debug.Log($"🔄 Starting delayed UI refresh for: {reason}");

        // รอ 2 frames เพื่อให้ระบบ update เสร็จ
        yield return null;
        yield return null;

        RefreshCharacterInfo();

        // ✅ ถ้า ItemDetailPanel เปิดอยู่ ให้ force refresh buttons
        if (itemDetailPanel != null && itemDetailPanel.IsVisible())
        {
            itemDetailPanel.RefreshButtonState();
            Debug.Log("🔄 Refreshed ItemDetailPanel buttons");
        }

        Debug.Log($"✅ Delayed UI refresh completed for: {reason}");
    }

    public void ShowInventory()
    {
        Debug.Log("📦 ShowInventory called");

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
            isInventoryOpen = true;
            RefreshCharacterInfo();

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

            if (equipmentSlots != null)
            {
                equipmentSlots.ShowEquipmentSlots();
                Debug.Log("✅ Equipment slots shown");
            }
            else
            {
                Debug.LogWarning("⚠️ EquipmentSlotsManager not found!");
            }

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

            HideItemDetail();

            if (inventoryGrid != null)
                inventoryGrid.gameObject.SetActive(false);

            if (equipmentSlots != null)
            {
                equipmentSlots.HideEquipmentSlots();
                Debug.Log("✅ Equipment slots hidden");
            }

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

        if (characterNameText != null)
        {
            string displayName = GetCharacterDisplayName(currentCharacterData.characterType);
            characterNameText.text = displayName;
        }

        if (characterLevelText != null)
            characterLevelText.text = $"Level {currentCharacterData.currentLevel}";

        if (characterExpText != null)
            characterExpText.text = $"EXP: {currentCharacterData.currentExp}/{currentCharacterData.expToNextLevel}";

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

        if (critChanceStatText != null)
            critChanceStatText.text = $"Critacal:{currentCharacterData.totalCriticalChance.ToString()}%";

        if (critDamageStatText != null)
            critDamageStatText.text = $"CritDamage:{(currentCharacterData.totalCriticalDamageBonus)}";

        if (hitRateStatText != null)
            hitRateStatText.text = $"HitRate:{(currentCharacterData.totalHitRate)}%";

        if (evasionRateStatText != null)
            evasionRateStatText.text = $"Dodge:{(currentCharacterData.totalEvasionRate)}%";

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
                return assassinImage;
        }
    }

    void OnDestroy()
    {
        if (itemDetailPanel != null)
        {
            ItemDetailPanel.OnEquipRequested -= HandleEquipRequest;
            ItemDetailPanel.OnUnequipRequested -= HandleUnequipRequest;
        }

        if (equipmentSlots != null)
        {
            EquipmentSlotsManager.OnItemEquipped -= HandleItemEquipped;
            EquipmentSlotsManager.OnItemUnequipped -= HandleItemUnequipped;
            EquipmentSlotsManager.OnEquipmentChanged -= HandleEquipmentChanged;
        }
    }

    void LoadItemSystem()
    {
        if (itemDatabase == null)
            itemDatabase = ItemDatabase.Instance;

        if (inventoryGrid != null)
        {
            inventoryGrid.itemDatabase = itemDatabase;
            inventoryGrid.LoadItemDatabase();
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

    #region Item Detail Panel - ✅ ปรับปรุงแล้ว
    void SetupItemDetailPanel()
    {
        Debug.Log("🔧 Setting up ItemDetailPanel...");

        if (itemDetailPanel == null)
        {
            itemDetailPanel = FindObjectOfType<ItemDetailPanel>();
            Debug.Log($"🔍 FindObjectOfType result: {(itemDetailPanel != null ? "Found" : "Not Found")}");
        }

        if (itemDetailPanel != null)
        {
            // ✅ Robust event cleanup and setup
            CleanupItemDetailEvents();
            SetupItemDetailEvents();

            Debug.Log("✅ Item detail panel connected");
            VerifyEventSubscription();
        }
        else
        {
            Debug.LogError("❌ ItemDetailPanel not found! Make sure it exists in the scene.");
        }
    }

    // ✅ ใหม่: Robust event cleanup
    private void CleanupItemDetailEvents()
    {
        try
        {
            if (ItemDetailPanel.OnEquipRequested != null)
            {
                var delegates = ItemDetailPanel.OnEquipRequested.GetInvocationList();
                foreach (var del in delegates)
                {
                    ItemDetailPanel.OnEquipRequested -= (System.Action<ItemData, int>)del;
                }
                Debug.Log($"🧹 Cleaned up {delegates.Length} OnEquipRequested subscribers");
            }

            if (ItemDetailPanel.OnUnequipRequested != null)
            {
                var delegates = ItemDetailPanel.OnUnequipRequested.GetInvocationList();
                foreach (var del in delegates)
                {
                    ItemDetailPanel.OnUnequipRequested -= (System.Action<ItemData, int>)del;
                }
                Debug.Log($"🧹 Cleaned up {delegates.Length} OnUnequipRequested subscribers");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ Event cleanup warning: {e.Message}");
        }
    }

    // ✅ ใหม่: Robust event setup
    private void SetupItemDetailEvents()
    {
        try
        {
            ItemDetailPanel.OnEquipRequested += HandleEquipRequest;
            ItemDetailPanel.OnUnequipRequested += HandleUnequipRequest;
            Debug.Log("✅ Item detail events subscribed");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Event setup failed: {e.Message}");
        }
    }

    void VerifyEventSubscription()
    {
        Debug.Log("🔍 === VERIFYING EVENT SUBSCRIPTION ===");

        if (ItemDetailPanel.OnEquipRequested != null)
        {
            var subscribers = ItemDetailPanel.OnEquipRequested.GetInvocationList();
            Debug.Log($"✅ OnEquipRequested has {subscribers.Length} subscriber(s):");
            foreach (var subscriber in subscribers)
            {
                Debug.Log($"   - {subscriber.Target?.GetType().Name}.{subscriber.Method.Name}");
            }
        }
        else
        {
            Debug.LogError("❌ OnEquipRequested is NULL after subscription!");
        }

        if (ItemDetailPanel.OnUnequipRequested != null)
        {
            var subscribers = ItemDetailPanel.OnUnequipRequested.GetInvocationList();
            Debug.Log($"✅ OnUnequipRequested has {subscribers.Length} subscriber(s):");
            foreach (var subscriber in subscribers)
            {
                Debug.Log($"   - {subscriber.Target?.GetType().Name}.{subscriber.Method.Name}");
            }
        }
        else
        {
            Debug.LogError("❌ OnUnequipRequested is NULL after subscription!");
        }

        Debug.Log("🔍 === END VERIFICATION ===");
    }

    // ✅ เพิ่ม method สำหรับ re-setup events
    [ContextMenu("Debug - Re-setup Events")]
    public void DebugResetupEvents()
    {
        Debug.Log("🔧 Re-setting up all events...");

        SetupEquipmentSystem();
        SetupItemDetailPanel();

        Debug.Log("✅ Event re-setup completed");
    }

    void HandleEquipRequest(ItemData item, int slotIndex)
    {
        if (item == null) return;

        Debug.Log($"🎽 Equip request: {item.ItemName} from slot {slotIndex}");

        if (equipmentSlots != null)
        {
            // ✅ สำหรับ rune: เช็ค duplication ก่อน
            if (item.ItemType == ItemType.Rune)
            {
                if (equipmentSlots.IsRuneIdAlreadyEquipped(item.ItemId))
                {
                    Debug.LogError($"❌ Cannot equip: Rune {item.ItemName} (ID: {item.ItemId}) is already equipped!");
                    ShowEquipErrorMessage($"Rune '{item.ItemName}' is already equipped!");
                    return;
                }
            }

            var targetSlot = equipmentSlots.GetSlotForItemType(item.ItemType);
            if (targetSlot == null)
            {
                Debug.LogError($"❌ No slot available for {item.ItemType}");
                return;
            }

            // ✅ สำหรับ non-rune items: เช็คว่าช่องว่างหรือไม่
            if (item.ItemType != ItemType.Rune)
            {
                bool slotEmpty = targetSlot.isEmpty && !targetSlot.HasEquippedItem() && targetSlot.GetEquippedItem() == null;

                if (!slotEmpty)
                {
                    Debug.LogWarning($"❌ {targetSlot.slotName} slot is occupied");
                    return;
                }
            }

            // Equip item ผ่าน Equipment System
            bool success = equipmentSlots.EquipItem(item, targetSlot);
            if (success)
            {
                RemoveItemFromInventory(slotIndex);
                Debug.Log($"✅ Successfully equipped {item.ItemName}");

                // ✅ เพิ่ม: Force update ItemDetailPanel ทันที
                StartCoroutine(ForceUpdateItemDetailPanelAfterEquip(item));
            }
            else
            {
                Debug.LogError($"❌ Failed to equip {item.ItemName}");
            }
        }
        else
        {
            Debug.LogError("❌ Equipment system not available!");
        }
    }

    // ✅ ใหม่: Force update ItemDetailPanel หลัง equip
    private IEnumerator ForceUpdateItemDetailPanelAfterEquip(ItemData equippedItem)
    {
        // รอให้ระบบ update เสร็จ
        yield return null;
        yield return null;

        if (itemDetailPanel != null && itemDetailPanel.IsVisible())
        {
            var currentItem = itemDetailPanel.GetCurrentItem();

            // ถ้าเป็น item เดียวกันที่เพิ่ง equip
            if (currentItem != null &&
                currentItem.ItemId == equippedItem.ItemId &&
                currentItem.ItemName == equippedItem.ItemName)
            {
                Debug.Log($"🔄 Force hiding ItemDetailPanel after equipping {equippedItem.ItemName}");
                itemDetailPanel.HidePanel();
            }
        }
    }

    // ✅ ใหม่: แสดง error message
    private void ShowEquipErrorMessage(string message)
    {
        Debug.LogWarning($"⚠️ {message}");
        // TODO: แสดง UI notification หรือ popup ถ้าต้องการ
    }

    void HandleUnequipRequest(ItemData item, int slotIndex)
    {
        if (item == null) return;

        Debug.Log($"🔧 Unequip request: {item.ItemName}");

        if (equipmentSlots != null)
        {
            var equipSlot = equipmentSlots.FindSlotWithItem(item);
            if (equipSlot != null)
            {
                bool success = equipmentSlots.UnequipItem(equipSlot);
                if (success)
                {
                    AddItemToInventory(item);
                    Debug.Log($"✅ Successfully unequipped {item.ItemName}");

                    // ✅ เพิ่ม: Force update ItemDetailPanel ทันที
                    StartCoroutine(ForceUpdateItemDetailPanelAfterUnequip(item));
                }
                else
                {
                    Debug.LogError($"❌ Failed to unequip {item.ItemName}");
                }
            }
            else
            {
                Debug.LogError($"❌ {item.ItemName} not found in equipment slots");
            }
        }
        else
        {
            Debug.LogError("❌ Equipment system not available!");
        }
    }

    // ✅ ใหม่: Force update ItemDetailPanel หลัง unequip
    private IEnumerator ForceUpdateItemDetailPanelAfterUnequip(ItemData unequippedItem)
    {
        // รอให้ระบบ update เสร็จ
        yield return null;
        yield return null;

        if (itemDetailPanel != null && itemDetailPanel.IsVisible())
        {
            var currentItem = itemDetailPanel.GetCurrentItem();

            // ถ้าเป็น item เดียวกันที่เพิ่ง unequip
            if (currentItem != null &&
                currentItem.ItemId == unequippedItem.ItemId &&
                currentItem.ItemName == unequippedItem.ItemName)
            {
                Debug.Log($"🔄 Force refreshing ItemDetailPanel buttons after unequipping {unequippedItem.ItemName}");
                itemDetailPanel.RefreshButtonState();
            }
        }
    }

    public void ShowItemDetail(ItemData item, int slotIndex)
    {
        if (itemDetailPanel != null && item != null)
        {
            // ✅ เพิ่ม: บอก ItemDetailPanel ว่า item มาจาก inventory (slotIndex >= 0) หรือ equipment (slotIndex = -1)
            itemDetailPanel.ShowItemDetail(item, slotIndex);

            Debug.Log($"📋 Showing item detail: {item.ItemName} from {(slotIndex >= 0 ? $"inventory slot {slotIndex}" : "equipment")}");
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

    #region Debug Methods - ✅ ปรับปรุงแล้ว
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

    [ContextMenu("Debug - Full System Check")]
    public void DebugFullSystemCheck()
    {
        Debug.Log("🔍 === FULL SYSTEM CHECK ===");

        // 1. Check ItemDetailPanel events
        Debug.Log("1. ItemDetailPanel Events:");
        if (ItemDetailPanel.OnEquipRequested != null)
        {
            Debug.Log($"   OnEquipRequested: {ItemDetailPanel.OnEquipRequested.GetInvocationList().Length} subscribers");
        }
        else
        {
            Debug.LogError("   OnEquipRequested: NULL!");
        }

        // 2. Check EquipmentSlotsManager
        Debug.Log("2. EquipmentSlotsManager:");
        if (equipmentSlots != null)
        {
            Debug.Log($"   Found: {equipmentSlots.name}");
            Debug.Log($"   Is Visible: {equipmentSlots.IsVisible()}");
        }
        else
        {
            Debug.LogError("   EquipmentSlotsManager: NULL!");
        }

        // 3. Check InventoryGrid
        Debug.Log("3. InventoryGrid:");
        if (inventoryGrid != null)
        {
            Debug.Log($"   Empty slots: {inventoryGrid.GetEmptySlotCount()}");
            Debug.Log($"   Filled slots: {inventoryGrid.GetFilledSlotCount()}");
        }
        else
        {
            Debug.LogError("   InventoryGrid: NULL!");
        }

        // 4. Check setup status
        Debug.Log($"4. Setup Status:");
        Debug.Log($"   Events setup complete: {eventsSetupComplete}");
        Debug.Log($"   Inventory open: {isInventoryOpen}");

        Debug.Log("🔍 === END FULL SYSTEM CHECK ===");
    }

    // ✅ ใหม่: System verification
    private void VerifyAllSystems()
    {
        Debug.Log("🔍 Verifying all systems...");

        bool allGood = true;

        if (itemDetailPanel == null)
        {
            Debug.LogError("❌ ItemDetailPanel missing!");
            allGood = false;
        }

        if (equipmentSlots == null)
        {
            Debug.LogError("❌ EquipmentSlotsManager missing!");
            allGood = false;
        }

        if (inventoryGrid == null)
        {
            Debug.LogError("❌ InventoryGrid missing!");
            allGood = false;
        }

        if (ItemDetailPanel.OnEquipRequested == null)
        {
            Debug.LogError("❌ OnEquipRequested not subscribed!");
            allGood = false;
        }

        if (allGood)
        {
            Debug.Log("✅ All systems verified successfully");
        }
        else
        {
            Debug.LogError("❌ System verification failed - some components missing");
        }
    }

    [ContextMenu("Recovery - Force System Reconnect")]
    public void RecoveryForceSystemReconnect()
    {
        Debug.Log("🔧 === RECOVERY: FORCE SYSTEM RECONNECT ===");

        try
        {
            // Re-find all components
            if (itemDetailPanel == null)
                itemDetailPanel = FindObjectOfType<ItemDetailPanel>();

            if (equipmentSlots == null)
                equipmentSlots = FindObjectOfType<EquipmentSlotsManager>();

            if (inventoryGrid == null)
                inventoryGrid = FindObjectOfType<InventoryGridManager>();

            // Re-setup all systems
            SetupEquipmentSystem();
            SetupItemDetailPanel();

            // Verify
            VerifyAllSystems();

            Debug.Log("✅ System reconnect completed");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Recovery failed: {e.Message}");
        }
    }

    [ContextMenu("Test - Check Rune System")]
    public void TestCheckRuneSystem()
    {
        Debug.Log("🔍 === TESTING RUNE SYSTEM ===");

        if (equipmentSlots != null)
        {
            equipmentSlots.DebugRuneSlots();
            equipmentSlots.DebugCheckRuneDuplicates();
        }
        else
        {
            Debug.LogError("❌ EquipmentSlotsManager not found!");
        }

        Debug.Log("🔍 === END RUNE SYSTEM TEST ===");
    }
    #endregion
}