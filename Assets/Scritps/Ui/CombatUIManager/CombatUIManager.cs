using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Fusion;
using TMPro;

public class CombatUIManager : MonoBehaviour
{
    [Header("UI Prefab")]
    public GameObject combatUIPrefab;

    [Header("UI References")]
    public Button attackButton;
    public Button skill1Button;
    public Button skill2Button;
    public Button skill3Button;
    public Button skill4Button;
    public Button inventoryButton;
    public Slider healthBar;
    public Slider manaBar;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI manaText;
    public FixedJoystick movementJoystick;
    public FixedJoystick cameraJoystick;

    [Header("🎯 Skill Cooldown UI")]
    public Image skill1CooldownOverlay;
    public Image skill2CooldownOverlay;
    public Image skill3CooldownOverlay;
    public Image skill4CooldownOverlay;
    public TextMeshProUGUI skill1CooldownText;
    public TextMeshProUGUI skill2CooldownText;
    public TextMeshProUGUI skill3CooldownText;
    public TextMeshProUGUI skill4CooldownText;


    [Header("🧪 Potion Buttons")]
    public Button potion1Button;
    public Button potion2Button;
    public Button potion3Button;
    public Button potion4Button;
    public Button potion5Button;

    [Header("🧪 Potion Button Visuals")]
    public Image potion1Icon;
    public Image potion2Icon;
    public Image potion3Icon;
    public Image potion4Icon;
    public Image potion5Icon;

    public TextMeshProUGUI potion1Count;
    public TextMeshProUGUI potion2Count;
    public TextMeshProUGUI potion3Count;
    public TextMeshProUGUI potion4Count;
    public TextMeshProUGUI potion5Count;
    [Header("🧪 Potion Cooldown Overlays (Optional)")]
    public Image potion1Cooldown;
    public Image potion2Cooldown;
    public Image potion3Cooldown;
    public Image potion4Cooldown;
    public Image potion5Cooldown;
    [Header("Inventory Panel")]
    public GameObject inventoryPanel;
    public Button inventoryCloseButton;

    [Header("🎯 NEW: Inventory Grid System")]
    public Transform inventoryGridParent;           // Parent สำหรับ inventory grid
    public GameObject inventorySlotPrefab;          // Prefab สำหรับ inventory slot (optional)
    public ScrollRect inventoryScrollRect;          // Scroll Rect สำหรับ inventory (optional)
    [Header("🔥 Set Bonus UI")]
    public Button setBonusButton;              // ปุ่มในใน Inventory Panel สำหรับเปิด Set Bonus
    public GameObject setBonusPanel;           // Panel แสดง Set Bonus
    public SetBonusUI setBonusUI;             // Component SetBonusUI

    [Header("🔍 Inventory Filter UI")]
    public TMP_Dropdown inventoryCategoryFilter;    // Dropdown สำหรับ filter category
    public TMP_Dropdown inventoryTierFilter;        // Dropdown สำหรับ filter tier
    public Button inventoryClearFiltersButton;      // ปุ่มล้าง filters
    public TextMeshProUGUI inventoryFilterStatusText; // แสดงสถานะ filter
    [Header("Character Stats in Inventory")]
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI characterLevelText;
    public Image characterPortraitImage; // 🆕 เพิ่มบรรทัดนี้

    public Slider inventoryHealthBar;
    public Slider inventoryManaBar;
    public TextMeshProUGUI inventoryHealthText;
    public TextMeshProUGUI inventoryManaText;
    public TextMeshProUGUI attackDamageText;
    public TextMeshProUGUI magicDamageText;
    public TextMeshProUGUI armorText;
    public TextMeshProUGUI moveSpeedText;
    public TextMeshProUGUI criticalChanceText;
    public TextMeshProUGUI criticalDamageText;
    public TextMeshProUGUI hitRateText;
    public TextMeshProUGUI evasionRateText;
    public TextMeshProUGUI attackSpeedText;
    public TextMeshProUGUI reductionCoolDownText;
    public TextMeshProUGUI liftSteal;
    public TextMeshProUGUI ampDamage;
    public TextMeshProUGUI magicArmor;
    public TextMeshProUGUI HealthRegen;
    public TextMeshProUGUI ManaRegen;
    [Header("🆕 Item Detail Panel")]
    public GameObject itemDetailPanel;              // Panel สำหรับแสดงรายละเอียดไอเทม
    public ItemDetailPanel itemDetailManager;      // Manager สำหรับจัดการ panel
   
    [Header("🆕 Equipment Slots (ลากจาก UI มาใส่)")]
    public List<EquipmentSlot> equipmentSlots = new List<EquipmentSlot>(); // Head, Armor, Weapon, Pants, Shoes, Rune
    public List<EquipmentSlot> potionSlots = new List<EquipmentSlot>();    // Potion quick slots (5 slots)
    public EquipmentSlotManager equipmentSlotManager; // Manager สำหรับจัดการ equipment slots
    [Header("🗑️ Delete System")]
    public Button deleteButton;                    // ปุ่มถังขยะ
    public ItemDeleteManager itemDeleteManager;   // Manager สำหรับระบบลบ
    private bool isDeleteModeActive = false;

    public Hero localHero { get; private set; }
    private SingleInputController inputController;
    private GameObject uiInstance;

    // เพิ่มการรอหา InputController
    private bool inputControllerFound = false;

    // ตัวแปรสถานะ Inventory
    private bool isInventoryOpen = false;
    private int selectedSlotIndex = -1;

    // 🎯 NEW: Inventory Grid Manager
    private InventoryGridManager inventoryGridManager;
    public static CombatUIManager Instance { get; private set; }
    private void Awake()
    {
        // ✅ Singleton check
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate CombatUIManager found! Destroying...");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // ถ้าต้องการให้คงอยู่ข้าม scene
    }
    private void Start()
    {
        Debug.Log("CombatUIManager Start");

        // ใช้ Coroutine เพื่อหา InputController
        StartCoroutine(FindInputControllerRoutine());
        LoadSkillIconsForCurrentHero();

    }

    private void Update()
    {
        // หา InputController ถ้ายังไม่เจอ
        if (!inputControllerFound && inputController == null)
        {
            inputController = FindObjectOfType<SingleInputController>();
            if (inputController != null)
            {
                inputControllerFound = true;
                inputController.UpdateJoystickReferences(movementJoystick, cameraJoystick);
                SetupButtonEvents();
                Debug.Log("InputController found in Update!");
            }
        }

        if (localHero == null)
        {
            Hero[] heroes = FindObjectsOfType<Hero>();
            foreach (Hero hero in heroes)
            {
                if (hero.HasInputAuthority && hero.IsSpawned)
                {
                    SetLocalHero(hero);
                    Debug.Log($"Found local hero in Update: {hero.CharacterName}");
                    break;
                }
            }
        }

        // อัพเดท UI
        if (localHero != null)
        {
            UpdateUI();

            // อัพเดท Character Stats ใน Inventory Panel ถ้าเปิดอยู่
            if (isInventoryOpen)
            {
                UpdateInventoryCharacterStats();
            }
        }

        // ✅ เพิ่มการตรวจสอบ ItemDeleteManager connection
        CheckItemDeleteManagerConnection();
    }
    // ✅ เพิ่ม method ใหม่สำหรับตรวจสอบ ItemDeleteManager
    private void CheckItemDeleteManagerConnection()
    {
        // ตรวจสอบทุก 2 วินาที
        if (Time.time - lastDeleteManagerCheck > 2f)
        {
            lastDeleteManagerCheck = Time.time;

            if (itemDeleteManager != null && localHero != null)
            {
                // ตรวจสอบว่า ItemDeleteManager มี reference ที่ถูกต้องหรือไม่
                if (itemDeleteManager.GetComponent<ItemDeleteManager>() != null)
                {
                    // Force refresh references
                    itemDeleteManager.ForceRefreshReferences();
                }
            }
        }
    }
    /// <summary>
    /// ตั้งค่ารูปตัวละครใน Inventory Panel
    /// </summary>
    private void UpdateCharacterPortrait()
    {
        if (characterPortraitImage == null)
        {
            Debug.LogWarning("[CombatUI] Character Portrait Image not assigned!");
            return;
        }

        if (localHero == null)
        {
            Debug.LogWarning("[CombatUI] Local hero is null, cannot update portrait");
            return;
        }

        // ดึงชื่อตัวละครจาก PersistentPlayerData
        string currentHero = PersistentPlayerData.Instance?.GetCurrentActiveCharacter() ?? "Assassin";

        Debug.Log($"[CombatUI] 🎨 Loading portrait for: {currentHero}");

        // ใช้ CharacterPortraitManager เพื่อตั้งค่ารูป
        CharacterPortraitManager.Instance.SetCharacterPortrait(characterPortraitImage, currentHero);
    }
    private float lastDeleteManagerCheck = 0f; // เพิ่มตัวแปรนี้ใน class level

    // เพิ่ม Coroutine สำหรับหา InputController
    private IEnumerator FindInputControllerRoutine()
    {
        float timeout = 5f;
        float elapsed = 0f;

        while (inputController == null && elapsed < timeout)
        {
            inputController = FindObjectOfType<SingleInputController>();

            if (inputController != null)
            {
                Debug.Log("InputController found!");
                inputControllerFound = true;
                inputController.UpdateJoystickReferences(movementJoystick, cameraJoystick);
                SetupButtonEvents();
                break;
            }

            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (inputController == null)
        {
            Debug.LogError("InputController not found after timeout!");
        }
    }



    // 🎯 UPDATED: Setup Inventory Panel with Grid System
    private void SetupInventoryPanel()
    {
        Debug.Log("=== Setting up Inventory Panel with Pagination System ===");

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);

            // ❌ ลบการตรวจสอบ ScrollRect ออก

            Debug.Log("✅ Inventory Panel initialized (hidden) with pagination");

            // Setup Inventory Grid
            SetupInventoryGrid();

            // Setup Item Info Panel
            SetupItemDetailPanel();

            SetupInventoryFilters();
        }
        else
        {
            Debug.LogError("❌ Inventory Panel not assigned in Inspector!");
        }
    }
    private void SetupInventoryFilters()
    {
        Debug.Log("=== Setting up Inventory Filter UI ===");

        // หา filter components ถ้าไม่ได้ assign ใน Inspector
        if (inventoryCategoryFilter == null)
        {
            inventoryCategoryFilter = FindUIComponentInInventory<TMP_Dropdown>("CategoryFilter");
        }

        if (inventoryTierFilter == null)
        {
            inventoryTierFilter = FindUIComponentInInventory<TMP_Dropdown>("TierFilter");
        }

        if (inventoryClearFiltersButton == null)
        {
            inventoryClearFiltersButton = FindUIComponentInInventory<Button>("ClearFiltersButton");
        }

        if (inventoryFilterStatusText == null)
        {
            inventoryFilterStatusText = FindUIComponentInInventory<TextMeshProUGUI>("FilterStatusText");
        }

        // เชื่อมต่อกับ InventoryGridManager
        if (inventoryGridManager != null)
        {
            ConnectFiltersToGridManager();
            Debug.Log("✅ Inventory filters connected to grid manager");
        }
        else
        {
            Debug.LogWarning("❌ InventoryGridManager not found, will connect filters later");
        }

        Debug.Log("✅ Inventory Filter UI setup complete");
    }

    // ✅ เพิ่ม method สำหรับเชื่อมต่อ filters กับ grid manager
    private void ConnectFiltersToGridManager()
    {
        if (inventoryGridManager == null) return;

        // เชื่อมต่อ filter components กับ grid manager
        if (inventoryCategoryFilter != null)
        {
            inventoryGridManager.categoryFilterDropdown = inventoryCategoryFilter;
        }

        if (inventoryTierFilter != null)
        {
            inventoryGridManager.tierFilterDropdown = inventoryTierFilter;
        }

        if (inventoryClearFiltersButton != null)
        {
            inventoryGridManager.clearFiltersButton = inventoryClearFiltersButton;
        }

        if (inventoryFilterStatusText != null)
        {
            inventoryGridManager.filteredItemCountText = inventoryFilterStatusText;
        }

        // เรียก setup filters ใน grid manager
        StartCoroutine(DelayedFilterSetup());
    }

    // ✅ เพิ่ม coroutine สำหรับ delayed filter setup
    private IEnumerator DelayedFilterSetup()
    {
        // รอ 2 frames เพื่อให้ grid พร้อม
        yield return null;
        yield return null;

        if (inventoryGridManager != null)
        {
            // เรียก private method ผ่าน reflection หรือทำ public
            inventoryGridManager.GetType().GetMethod("SetupInventoryFilters",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(inventoryGridManager, null);

            Debug.Log("[CombatUI] ✅ Delayed filter setup completed");
        }
    }

    // ✅ เพิ่ม helper method สำหรับหา UI components ใน inventory panel
    private T FindUIComponentInInventory<T>(string componentName) where T : Component
    {
        if (inventoryPanel == null) return null;

        // หาใน inventory panel และ children
        T[] components = inventoryPanel.GetComponentsInChildren<T>(true);

        foreach (T component in components)
        {
            if (component.gameObject.name.Contains(componentName))
            {
                Debug.Log($"[CombatUI] Found {componentName}: {component.gameObject.name}");
                return component;
            }
        }

        Debug.LogWarning($"[CombatUI] {componentName} of type {typeof(T).Name} not found in inventory panel");
        return null;
    }
    private void SetupItemDetailPanel()
    {
        Debug.Log("=== Setting up Item Detail Panel ===");

        // ถ้ายังไม่มี ItemDetailPanel component ให้เพิ่ม
        if (itemDetailManager == null && itemDetailPanel != null)
        {
            itemDetailManager = itemDetailPanel.GetComponent<ItemDetailPanel>();
            if (itemDetailManager == null)
            {
                itemDetailManager = itemDetailPanel.AddComponent<ItemDetailPanel>();
            }
        }

        // ซ่อน panel ไว้ก่อน
        if (itemDetailPanel != null)
        {
            itemDetailPanel.SetActive(false);
        }

        Debug.Log("✅ Item Detail Panel setup complete");
    }
    private void ShowItemDetailForSlot(int slotIndex)
    {
        if (localHero?.GetInventory() == null || itemDetailManager == null)
            return;

        InventoryItem item = localHero.GetInventory().GetItem(slotIndex);

        if (item != null && !item.IsEmpty)
        {
            itemDetailManager.ShowItemDetail(item);
            Debug.Log($"[CombatUI] Showing item detail for slot {slotIndex}: {item.itemData.ItemName}");
        }
        else
        {
            HideItemDetail();
        }
    }

    // เพิ่ม method ใหม่สำหรับซ่อนรายละเอียดไอเทม
    private void HideItemDetail()
    {
        if (itemDetailManager != null)
        {
            itemDetailManager.HideItemDetail();
        }
    }
    // 🎯 NEW: Setup Inventory Grid System
    public void ForceSetupInventoryGrid()
    {
        if (inventoryGridManager == null)
        {
            SetupInventoryGrid();
            Debug.Log("[CombatUI] Force setup inventory grid completed");

            // ✅ Refresh ItemDeleteManager หลัง force setup
            RefreshItemDeleteManagerReferences();
        }
        else
        {
            Debug.Log("[CombatUI] Inventory grid already exists");

            // ✅ แม้ grid มีอยู่แล้ว ก็ยัง refresh ItemDeleteManager
            RefreshItemDeleteManagerReferences();

            // ✅ Force update grid ด้วย
            if (localHero != null)
            {
                inventoryGridManager.ForceUpdateFromCharacter();
            }
        }
    }


    // ✅ แก้ไข SetupInventoryGrid ให้รอนานกว่า
    private void SetupInventoryGrid()
    {
        Debug.Log("=== Setting up Inventory Grid ===");

        // หา Inventory Grid Parent ถ้าไม่ได้ assign
        if (inventoryGridParent == null)
        {
            inventoryGridParent = FindInventoryGridParent();
        }

        if (inventoryGridParent == null)
        {
            Debug.LogError("❌ Inventory Grid Parent not found! Creating new one...");
            CreateInventoryGridParent();
        }

        // สร้าง InventoryGridManager
        inventoryGridManager = inventoryGridParent.GetComponent<InventoryGridManager>();
        if (inventoryGridManager == null)
        {
            inventoryGridManager = inventoryGridParent.gameObject.AddComponent<InventoryGridManager>();
            Debug.Log("✅ Created InventoryGridManager component");
        }

        // Subscribe to grid events
        inventoryGridManager.OnSlotSelectionChanged += HandleSlotSelectionChanged;
        ConnectFiltersToGridManager();

        // 🎯 เชื่อมต่อกับ local hero ถ้ามีแล้ว
        if (localHero != null)
        {
            ConnectInventoryToHero(localHero);
        }
        else
        {
            // เพิ่มส่วนนี้เพื่อหา hero ใน coroutine
            StartCoroutine(WaitForHeroAndSetupGrid());
        }

        // ✅ Refresh ItemDeleteManager references
        RefreshItemDeleteManagerReferences();

        Debug.Log("✅ Inventory Grid setup complete");
    }
    public void ResetInventoryFilters()
    {
        if (inventoryGridManager != null)
        {
            inventoryGridManager.ResetFilters();
            Debug.Log("[CombatUI] Reset inventory filters");
        }
    }

    public bool HasActiveInventoryFilters()
    {
        if (inventoryGridManager != null)
        {
            return inventoryGridManager.HasActiveFilters();
        }
        return false;
    }

    public string GetInventoryFilterStatus()
    {
        if (inventoryGridManager != null)
        {
            return inventoryGridManager.GetCurrentFilterStatus();
        }
        return "Filters not available";
    }
    // ✅ เพิ่ม method ใหม่สำหรับ refresh ItemDeleteManager
    private void RefreshItemDeleteManagerReferences()
    {
        if (itemDeleteManager != null)
        {
            Debug.Log("[CombatUI] 🔄 Refreshing ItemDeleteManager references...");
            itemDeleteManager.ForceRefreshReferences();

            // ตั้งค่า target character ใหม่
            if (localHero != null)
            {
                itemDeleteManager.SetTargetCharacter(localHero);
            }
        }
        else
        {
            Debug.LogWarning("[CombatUI] ItemDeleteManager not assigned in Inspector!");

            // ลองหา ItemDeleteManager อัตโนมัติ
            itemDeleteManager = FindObjectOfType<ItemDeleteManager>();
            if (itemDeleteManager != null)
            {
                Debug.Log("[CombatUI] ✅ Found ItemDeleteManager automatically");
                itemDeleteManager.ForceRefreshReferences();

                if (localHero != null)
                {
                    itemDeleteManager.SetTargetCharacter(localHero);
                }
            }
        }
    }

    // ✅ เพิ่ม method ใหม่สำหรับ refresh ItemDeleteManager


    private void ConnectInventoryToHero(Hero hero)
    {
        if (inventoryGridManager != null)
        {
            inventoryGridManager.SetOwnerCharacter(hero);
            // ✅ เพิ่มการ force load items ทันที
            inventoryGridManager.ForceLoadFromCharacterAfterConnection();

            // ✅ เพิ่มการ force update ทันที
            inventoryGridManager.ForceUpdateFromCharacter();

            Debug.Log($"[CombatUI] Connected inventory grid to {hero.CharacterName}");

            // ✅ Refresh ItemDeleteManager หลัง connect hero
            RefreshItemDeleteManagerReferences();
        }
    }

    // ✅ เพิ่ม delayed setup
    private IEnumerator DelayedInventorySetup()
    {
        yield return null; // รอ 1 frame ให้ connection เสร็จ

        if (inventoryGridManager != null)
        {
            inventoryGridManager.ForceUpdateFromCharacter();
            Debug.Log("[CombatUI] Delayed inventory setup completed");
        }
    }

    private IEnumerator DelayedDeactivatePanelLonger()
{
    // รอ 3 frames เพื่อให้แน่ใจว่า slots สร้างเสร็จ
    yield return null;
    yield return null;
    yield return null;

    if (!isInventoryOpen) // ถ้า user ยังไม่ได้เปิด panel เอง
    {
        inventoryPanel.SetActive(false);
        Debug.Log("[CombatUI] Deactivated inventory panel after extended grid setup");
    }
}

    // เพิ่ม coroutine นี้
    private IEnumerator DelayedDeactivatePanel()
    {
        yield return null; // รอ 1 frame ให้ grid setup เสร็จ
        yield return null; // รออีก 1 frame เพื่อให้แน่ใจ
        yield return null; // รออีก 1 frame สำหรับ slots

        if (!isInventoryOpen) // ถ้า user ยังไม่ได้เปิด panel เอง
        {
            inventoryPanel.SetActive(false);
            Debug.Log("[CombatUI] Deactivated inventory panel after grid setup");
        }
    }

   
    private IEnumerator WaitForHeroAndSetupGrid()
    {
        float timeout = 10f;
        float elapsed = 0f;

        while (localHero == null && elapsed < timeout)
        {
            // ลองหา hero
            Hero[] heroes = FindObjectsOfType<Hero>();
            foreach (Hero hero in heroes)
            {
                if (hero.HasInputAuthority && hero.IsSpawned)
                {
                    SetLocalHero(hero);
                    Debug.Log($"[CombatUI] Found hero in coroutine: {hero.CharacterName}");
                    yield break;
                }
            }

            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (localHero == null)
        {
            Debug.LogWarning("[CombatUI] No hero found within timeout for grid setup");
        }
    }

    private Transform FindInventoryGridParent()
    {
        // ลองหาใน hierarchy ต่างๆ
        string[] possiblePaths = {
            "InventoryGrid",
            "Grid",
            "Content",
            "Scroll View/Viewport/Content",
            "InventoryScrollRect/Viewport/Content"
        };

        foreach (string path in possiblePaths)
        {
            Transform found = inventoryPanel.transform.Find(path);
            if (found != null)
            {
                Debug.Log($"✅ Found inventory grid parent at: {path}");
                return found;
            }
        }

        return null;
    }

    private void CreateInventoryGridParent()
    {
        // สร้าง Grid Parent ใหม่
        GameObject gridParentObj = new GameObject("InventoryGrid");
        gridParentObj.transform.SetParent(inventoryPanel.transform, false);

        RectTransform gridRect = gridParentObj.AddComponent<RectTransform>();
        gridRect.anchorMin = Vector2.zero;
        gridRect.anchorMax = Vector2.one;
        gridRect.offsetMin = new Vector2(20, 20);  // margin
        gridRect.offsetMax = new Vector2(-20, -20);

        inventoryGridParent = gridRect;

        Debug.Log("✅ Created new Inventory Grid Parent");
    }

    // 🎯 NEW: Handle slot selection events
    private void HandleSlotSelectionChanged(int slotIndex)
    {
        selectedSlotIndex = slotIndex;

        if (slotIndex >= 0)
        {
            Debug.Log($"[CombatUI] Selected inventory slot: {slotIndex}");

            // ✅ ปรับปรุงการตรวจสอบโหมดลบ
            if (itemDeleteManager != null && itemDeleteManager.IsDeleteMode)
            {
                Debug.Log("[CombatUI] In delete mode - blocking ItemDetailPanel");

                // ซ่อน ItemDetailPanel ถ้าเปิดอยู่
                HideItemDetail();

                // ไม่แสดง ItemDetailPanel ในโหมดลบ
                return;
            }

            // แสดง item detail ตามปกติถ้าไม่ได้อยู่ในโหมดลบ
            ShowItemDetailForSlot(slotIndex);
        }
        else
        {
            Debug.Log("[CombatUI] No slot selected");
            HideItemDetail();
        }
    }

    private T FindUIComponent<T>(string name) where T : Component
    {
        Transform directChild = uiInstance.transform.Find(name);
        if (directChild != null)
        {
            T component = directChild.GetComponent<T>();
            if (component != null) return component;
        }

        T[] allComponents = uiInstance.GetComponentsInChildren<T>(true);
        foreach (T comp in allComponents)
        {
            if (comp.gameObject.name == name)
            {
                return comp;
            }
        }

        Debug.LogWarning($"Could not find {name} of type {typeof(T).Name}");
        return null;
    }

    // ✅ แก้ไข SetupButtonEvents() ใน CombatUIManager.cs
    // แทนที่ method เดิมด้วย code นี้:

    private void SetupButtonEvents()
    {
        Debug.Log("=== Setting up UI Button Events ===");

        // ✅ ตรวจสอบ inputController ให้แน่ใจว่ามีจริง
        if (inputController == null)
        {
            Debug.LogWarning("❌ InputController is NULL! Trying to find it...");
            inputController = FindObjectOfType<SingleInputController>();

            if (inputController == null)
            {
                // ✅ สร้างใหม่ถ้ายังไม่มี (safety fallback)
                Debug.LogWarning("⚠️ Creating new SingleInputController...");
                GameObject obj = new GameObject("SingleInputController");
                inputController = obj.AddComponent<SingleInputController>();
                DontDestroyOnLoad(obj);
            }
        }

        // ✅ รอให้ InputController พร้อม
        if (inputController != null && movementJoystick != null && cameraJoystick != null)
        {
            inputController.UpdateJoystickReferences(movementJoystick, cameraJoystick);
            Debug.Log("✅ InputController joystick references updated");
        }
        else
        {
            Debug.LogError($"❌ Missing references! IC:{inputController != null}, MJ:{movementJoystick != null}, CJ:{cameraJoystick != null}");
        }

        SetupPotionButtons();
        SetupDeleteButton();

        // Attack Button
        if (attackButton != null)
        {
            attackButton.onClick.RemoveAllListeners();
            attackButton.onClick.AddListener(() => {
                Debug.Log("Attack button pressed");
                if (inputController != null)
                    inputController.SetAttackPressed();
                else
                    Debug.LogError("❌ Cannot attack - InputController is NULL!");
            });
            Debug.Log("✅ Attack button event setup complete");
        }
        else Debug.LogWarning("❌ Attack button not assigned in Inspector!");

        // Skill 1 Button
        if (skill1Button != null)
        {
            skill1Button.onClick.RemoveAllListeners();
            skill1Button.onClick.AddListener(() => {
                Debug.Log("Skill1 button pressed");
                if (inputController != null)
                    inputController.SetSkill1Pressed();
                else
                    Debug.LogError("❌ Cannot use skill1 - InputController is NULL!");
            });
            Debug.Log("✅ Skill1 button event setup complete");
        }
        else Debug.LogWarning("❌ Skill1 button not assigned in Inspector!");

        // Skill 2 Button
        if (skill2Button != null)
        {
            skill2Button.onClick.RemoveAllListeners();
            skill2Button.onClick.AddListener(() => {
                Debug.Log("Skill2 button pressed");
                if (inputController != null)
                    inputController.SetSkill2Pressed();
                else
                    Debug.LogError("❌ Cannot use skill2 - InputController is NULL!");
            });
            Debug.Log("✅ Skill2 button event setup complete");
        }
        else Debug.LogWarning("❌ Skill2 button not assigned in Inspector!");

        // Skill 3 Button
        if (skill3Button != null)
        {
            skill3Button.onClick.RemoveAllListeners();
            skill3Button.onClick.AddListener(() => {
                Debug.Log("Skill3 button pressed");
                if (inputController != null)
                    inputController.SetSkill3Pressed();
                else
                    Debug.LogError("❌ Cannot use skill3 - InputController is NULL!");
            });
            Debug.Log("✅ Skill3 button event setup complete");
        }
        else Debug.LogWarning("❌ Skill3 button not assigned in Inspector!");

        // Skill 4 Button
        if (skill4Button != null)
        {
            skill4Button.onClick.RemoveAllListeners();
            skill4Button.onClick.AddListener(() => {
                Debug.Log("Skill4 button pressed");
                if (inputController != null)
                    inputController.SetSkill4Pressed();
                else
                    Debug.LogError("❌ Cannot use skill4 - InputController is NULL!");
            });
            Debug.Log("✅ Skill4 button event setup complete");
        }
        else Debug.LogWarning("❌ Skill4 button not assigned in Inspector!");

        // Setup Inventory Button
        if (inventoryButton != null)
        {
            inventoryButton.onClick.RemoveAllListeners();
            inventoryButton.onClick.AddListener(() => {
                Debug.Log("Inventory button pressed");
                ToggleInventory();
            });
            Debug.Log("✅ Inventory button event setup complete");
        }
        else Debug.LogWarning("❌ Inventory button not assigned in Inspector!");

        // Setup Inventory Close Button
        if (inventoryCloseButton != null)
        {
            inventoryCloseButton.onClick.RemoveAllListeners();
            inventoryCloseButton.onClick.AddListener(() => {
                Debug.Log("Inventory close button pressed");
                CloseInventory();
            });
            Debug.Log("✅ Inventory close button event setup complete");
        }
        else Debug.LogWarning("❌ Inventory close button not assigned in Inspector!");

        if (setBonusButton != null)
        {
            setBonusButton.onClick.RemoveAllListeners();
            setBonusButton.onClick.AddListener(() => {
                Debug.Log("Set Bonus button pressed");
                ToggleSetBonusPanel();
            });
            Debug.Log("✅ Set Bonus button event setup complete");
        }

        Debug.Log("=== UI Button Events Setup Complete ===");
    }
    private void SetupDeleteButton()
    {
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(() => {
                Debug.Log("Delete button pressed");
                OnDeleteButtonClicked();
            });
            Debug.Log("✅ Delete button event setup complete");
        }
        else
        {
            Debug.LogWarning("❌ Delete button not assigned in Inspector!");
        }
    }
    private void OnDeleteButtonClicked()
    {
        if (itemDeleteManager != null)
        {
            itemDeleteManager.OnDeleteButtonClicked();
        }
        else
        {
            Debug.LogError("[CombatUI] ItemDeleteManager not assigned!");
        }
    }

    // เพิ่ม method นี้สำหรับ ItemDeleteManager เรียกใช้
    public void SetDeleteModeActive(bool active)
    {
        isDeleteModeActive = active;

        // อัปเดตสีของปุ่มลบ
        if (deleteButton != null)
        {
            var buttonImage = deleteButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = active ? Color.red : Color.white;
            }
        }

        // ✅ ซ่อน ItemDetailPanel เมื่อเข้าโหมดลบ
        if (active)
        {
            HideItemDetail();
            Debug.Log("[CombatUI] Delete mode ACTIVE - ItemDetailPanel hidden");
        }
        else
        {
            Debug.Log("[CombatUI] Delete mode INACTIVE");
        }

        Debug.Log($"[CombatUI] Delete mode: {(active ? "ACTIVE" : "INACTIVE")}");
    }
    public void UpdatePotionButtons()
    {
        if (localHero == null) return;

        UpdateSinglePotionButton(0, potion1Icon, potion1Count, potion1Button);
        UpdateSinglePotionButton(1, potion2Icon, potion2Count, potion2Button);
        UpdateSinglePotionButton(2, potion3Icon, potion3Count, potion3Button);
        UpdateSinglePotionButton(3, potion4Icon, potion4Count, potion4Button);
        UpdateSinglePotionButton(4, potion5Icon, potion5Count, potion5Button);
    }
    private void SetupPotionButtons()
    {
        Debug.Log("=== Setting up Potion Button Events ===");

        // Potion 1
        if (potion1Button != null)
        {
            potion1Button.onClick.RemoveAllListeners();
            potion1Button.onClick.AddListener(() => {
                Debug.Log("Potion1 button pressed");
                inputController.SetPotion1Pressed();
            });
            Debug.Log("✅ Potion1 button event setup complete");
        }

        // Potion 2
        if (potion2Button != null)
        {
            potion2Button.onClick.RemoveAllListeners();
            potion2Button.onClick.AddListener(() => {
                Debug.Log("Potion2 button pressed");
                inputController.SetPotion2Pressed();
            });
            Debug.Log("✅ Potion2 button event setup complete");
        }

        // Potion 3
        if (potion3Button != null)
        {
            potion3Button.onClick.RemoveAllListeners();
            potion3Button.onClick.AddListener(() => {
                Debug.Log("Potion3 button pressed");
                inputController.SetPotion3Pressed();
            });
            Debug.Log("✅ Potion3 button event setup complete");
        }

        // Potion 4
        if (potion4Button != null)
        {
            potion4Button.onClick.RemoveAllListeners();
            potion4Button.onClick.AddListener(() => {
                Debug.Log("Potion4 button pressed");
                inputController.SetPotion4Pressed();
            });
            Debug.Log("✅ Potion4 button event setup complete");
        }

        // Potion 5
        if (potion5Button != null)
        {
            potion5Button.onClick.RemoveAllListeners();
            potion5Button.onClick.AddListener(() => {
                Debug.Log("Potion5 button pressed");
                inputController.SetPotion5Pressed();
            });
            Debug.Log("✅ Potion5 button event setup complete");
        }

        Debug.Log("=== Potion Button Events Setup Complete ===");
    }

    // เพิ่มฟังก์ชัน Inventory
    public void ToggleInventory()
    {
        if (isInventoryOpen)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }
    private void UpdateSkillCooldowns()
    {
        if (localHero == null) return;

        // อัพเดท Skill 1
        UpdateSingleSkillCooldown(
            localHero.GetSkillCooldownRemaining(1),
            localHero.skill1Cooldown,
            skill1Button,
            skill1CooldownOverlay,
            skill1CooldownText
        );

        // อัพเดท Skill 2
        UpdateSingleSkillCooldown(
            localHero.GetSkillCooldownRemaining(2),
            localHero.skill2Cooldown,
            skill2Button,
            skill2CooldownOverlay,
            skill2CooldownText
        );

        // อัพเดท Skill 3
        UpdateSingleSkillCooldown(
            localHero.GetSkillCooldownRemaining(3),
            localHero.skill3Cooldown,
            skill3Button,
            skill3CooldownOverlay,
            skill3CooldownText
        );

        // อัพเดท Skill 4
        UpdateSingleSkillCooldown(
            localHero.GetSkillCooldownRemaining(4),
            localHero.skill4Cooldown,
            skill4Button,
            skill4CooldownOverlay,
            skill4CooldownText
        );
    }
    private void UpdateSingleSkillCooldown(float cooldownRemaining, float maxCooldown, Button skillButton, Image cooldownOverlay, TextMeshProUGUI cooldownText)
    {
        bool isOnCooldown = cooldownRemaining > 0;

        // ✅ ปุ่มกดได้ตลอด - ไม่แก้ interactable
        // if (skillButton != null)
        // {
        //     skillButton.interactable = !isOnCooldown;
        // }

        // อัพเดท cooldown overlay
        if (cooldownOverlay != null)
        {
            if (isOnCooldown)
            {
                cooldownOverlay.gameObject.SetActive(true);
                cooldownOverlay.fillAmount = cooldownRemaining / maxCooldown;

                // ✅ เพิ่มสี overlay เพื่อให้เห็นชัดว่าติด cooldown
                cooldownOverlay.color = new Color(0f, 0f, 0f, 0.7f); // สีดำโปร่งใส
            }
            else
            {
                cooldownOverlay.gameObject.SetActive(false);
            }
        }

        // อัพเดท cooldown text
        if (cooldownText != null)
        {
            if (isOnCooldown)
            {
                cooldownText.gameObject.SetActive(true);
                cooldownText.text = Mathf.Ceil(cooldownRemaining).ToString();
                cooldownText.color = Color.white; // สีขาวให้เห็นชัด
            }
            else
            {
                cooldownText.gameObject.SetActive(false);
            }
        }
    }
    private void UpdateSinglePotionButton(int slotIndex, Image iconImage, TextMeshProUGUI countText, Button button, Image cooldownOverlay = null)
    {
        // ดึง potion data จาก character
        ItemData potionData = localHero.GetPotionInSlot(slotIndex);
        int stackCount = localHero.GetPotionStackCount(slotIndex);
        bool canUse = localHero.CanUsePotion(slotIndex);
        float cooldownRemaining = localHero.GetPotionCooldownRemaining(slotIndex);

        // แสดงปุ่มเสมอ
        if (button != null)
        {
            button.gameObject.SetActive(true);
            button.interactable = canUse && stackCount > 0;
        }

        if (potionData != null && stackCount > 0)
        {
            // มี potion - แสดงรูปและจำนวน
            if (iconImage != null)
            {
                iconImage.sprite = potionData.ItemIcon;
                iconImage.color = canUse ? Color.white : new Color(1f, 1f, 1f, 0.5f); // ทำให้จางถ้าใช้ไม่ได้
                iconImage.gameObject.SetActive(true);
            }

            if (countText != null)
            {
                countText.text = stackCount > 1 ? stackCount.ToString() : "";
                countText.gameObject.SetActive(stackCount > 1);
            }
        }
        else
        {
            // ไม่มี potion - แสดงปุ่มว่าง
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.color = new Color(1f, 1f, 1f, 0.3f); // สีจางเป็น placeholder
                iconImage.gameObject.SetActive(false);
            }

            if (countText != null)
            {
                countText.gameObject.SetActive(false);
            }
        }

        // แสดง cooldown overlay
        if (cooldownOverlay != null)
        {
            if (cooldownRemaining > 0)
            {
                cooldownOverlay.gameObject.SetActive(true);
                cooldownOverlay.fillAmount = cooldownRemaining / localHero.potionCooldown;
            }
            else
            {
                cooldownOverlay.gameObject.SetActive(false);
            }
        }
    }
    public void OpenInventory()
    {
        if (inventoryPanel != null)
        {
            AudioManager.instance.PlaySFX(7, 1f);

            inventoryPanel.SetActive(true);
            isInventoryOpen = true;

            // อัพเดท Character Stats ทันทีที่เปิด Panel
            if (localHero != null)
            {
                UpdateInventoryCharacterStats();

                // Force load จาก Firebase
                var inventory = localHero.GetInventory();
                if (inventory != null)
                {
                    inventory.ForceLoadFromFirebase();
                }
            }

            // ✅ เปลี่ยนจาก OnInventoryPanelOpened() เป็น ForceRefreshOnOpen()
            if (inventoryGridManager != null)
            {
                inventoryGridManager.ForceRefreshOnOpen();

                // รอแล้ว force อีกครั้ง
                StartCoroutine(DelayedInventoryFix());
            }

            Debug.Log("Inventory panel opened");
        }
    }

    // ✅ เพิ่ม Coroutine ใหม่นี้
    private IEnumerator DelayedInventoryFix()
    {
        // รอ 3 frames
        yield return null;
        yield return null;
        yield return null;

        if (inventoryGridManager != null)
        {
            inventoryGridManager.ForceRefreshOnOpen();
            Debug.Log("[CombatUI] ✅ Delayed inventory fix complete");
        }
    }
    public bool IsDeleteModeActive()
    {
        return isDeleteModeActive;
    }
    public void CloseInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            isInventoryOpen = false;

            // ยกเลิกการเลือก slot
            if (inventoryGridManager != null)
            {
                inventoryGridManager.DeselectAllSlots();
            }

            // ซ่อน item detail panel ด้วย
            HideItemDetail();

            // ✅ เพิ่มบรรทัดนี้ - ปิด delete mode เมื่อปิด inventory

            if (setBonusUI != null && setBonusUI.IsPanelOpen())
            {
                setBonusUI.CloseSetBonusPanel();
            }
            Debug.Log("Inventory panel closed");
        }
    }
    public bool IsSetBonusPanelOpen()
    {
        return setBonusUI != null && setBonusUI.IsPanelOpen();
    }
    public void SetLocalHero(Hero hero)
    {
        localHero = hero;
        Debug.Log($"Local hero set: {hero.CharacterName} - HP: {hero.CurrentHp}/{hero.MaxHp}");

        // เชื่อมต่อ Inventory Grid
        ConnectInventoryToHero(hero);

        // เชื่อมต่อ Equipment Slots ผ่าง EquipmentSlotManager
        ConnectEquipmentSlotsToHero(hero);

        // 🆕 เชื่อมต่อ ItemDeleteManager
        ConnectDeleteManagerToHero(hero);

        // ✅ เพิ่มบรรทัดนี้ - เชื่อมต่อ Set Bonus UI
        ConnectSetBonusUIToHero(hero);

        // ✅ เพิ่มการ force refresh references
        RefreshItemDeleteManagerReferences();
        LoadSkillIconsForCurrentHero();

        UpdateUI();
    }
    private void ConnectDeleteManagerToHero(Hero hero)
    {
        if (itemDeleteManager != null)
        {
            itemDeleteManager.SetTargetCharacter(hero);
            itemDeleteManager.ForceRefreshReferences();
            Debug.Log($"[CombatUI] Connected ItemDeleteManager to {hero.CharacterName}");
        }
        else
        {
            Debug.LogWarning("[CombatUI] ItemDeleteManager not assigned in Inspector!");

            // ลองหาอัตโนมัติ
            itemDeleteManager = FindObjectOfType<ItemDeleteManager>();
            if (itemDeleteManager != null)
            {
                itemDeleteManager.SetTargetCharacter(hero);
                itemDeleteManager.ForceRefreshReferences();
                Debug.Log($"[CombatUI] Auto-found and connected ItemDeleteManager to {hero.CharacterName}");
            }
        }
    }


    private void ConnectEquipmentSlotsToHero(Hero hero)
    {
        // หา EquipmentSlotManager จาก Character
        EquipmentSlotManager equipmentSlotManager = hero.GetComponent<EquipmentSlotManager>();

        if (equipmentSlotManager != null)
        {
            Debug.Log($"[CombatUI] Connecting slots to {hero.CharacterName}...");

            // เชื่อมต่อ Equipment Slots ที่ลากมาใน Inspector
            if (equipmentSlots.Count > 0)
            {
                equipmentSlotManager.ConnectEquipmentSlots(equipmentSlots);

                // Setup events สำหรับแต่ละ slot
                foreach (EquipmentSlot slot in equipmentSlots)
                {
                    if (slot != null)
                    {
                        slot.OnSlotClicked += HandleEquipmentSlotClicked;
                    }
                }

                Debug.Log($"[CombatUI] ✅ Connected {equipmentSlots.Count} equipment slots to {hero.CharacterName}");
            }

            // 🆕 เชื่อมต่อ Potion Slots ที่ลากมาใน Inspector
            if (potionSlots.Count > 0)
            {
                Debug.Log($"[CombatUI] Found {potionSlots.Count} potion slots to connect");

                // 🆕 ตรวจสอบและ setup potion slots ก่อน connect
                SetupPotionSlots();

                equipmentSlotManager.ConnectPotionSlots(potionSlots);

                // Setup events สำหรับแต่ละ potion slot
                foreach (EquipmentSlot slot in potionSlots)
                {
                    if (slot != null)
                    {
                        slot.OnSlotClicked += HandleEquipmentSlotClicked;
                        Debug.Log($"[CombatUI] Setup event for potion slot {slot.PotionSlotIndex}");
                    }
                }

                Debug.Log($"[CombatUI] ✅ Connected {potionSlots.Count} potion slots to {hero.CharacterName}");
            }
            else
            {
                Debug.LogWarning($"[CombatUI] ❌ No potion slots found in Inspector! Please assign potion slots.");
            }

            // เก็บ reference
            this.equipmentSlotManager = equipmentSlotManager;
        }
        else
        {
            Debug.LogWarning($"[CombatUI] No EquipmentSlotManager found in {hero.CharacterName}! Please add EquipmentSlotManager component to Character.");
        }
    }

    // 🆕 เพิ่ม method สำหรับ setup potion slots
    private void SetupPotionSlots()
    {
        Debug.Log("[CombatUI] Setting up potion slots...");

        for (int i = 0; i < potionSlots.Count; i++)
        {
            EquipmentSlot slot = potionSlots[i];
            if (slot != null)
            {
                // ตั้งค่า slot type และ potion index
                if (slot.SlotType != ItemType.Potion)
                {
                    Debug.LogWarning($"[CombatUI] Potion slot {i} has wrong SlotType: {slot.SlotType}, fixing...");
                    slot.SetSlotType(ItemType.Potion, i);
                }

                // ตรวจสอบ potion slot index
                if (slot.PotionSlotIndex != i)
                {
                    Debug.LogWarning($"[CombatUI] Potion slot {i} has wrong PotionSlotIndex: {slot.PotionSlotIndex}, fixing...");
                    slot.SetSlotType(ItemType.Potion, i);
                }

                Debug.Log($"[CombatUI] Potion slot {i}: SlotType={slot.SlotType}, PotionSlotIndex={slot.PotionSlotIndex}");
            }
            else
            {
                Debug.LogError($"[CombatUI] Potion slot {i} is null!");
            }
        }
    }

    private void ConnectSetBonusUIToHero(Hero hero)
    {
        if (setBonusUI != null)
        {
            setBonusUI.SetOwnerCharacter(hero);
            Debug.Log($"[CombatUI] Connected SetBonusUI to {hero.CharacterName}");
        }
        else
        {
            Debug.LogWarning("[CombatUI] SetBonusUI not assigned in Inspector!");

            // ลองหาอัตโนมัติ
            setBonusUI = FindObjectOfType<SetBonusUI>();
            if (setBonusUI != null)
            {
                setBonusUI.SetOwnerCharacter(hero);
                Debug.Log($"[CombatUI] Auto-found and connected SetBonusUI to {hero.CharacterName}");
            }
            else
            {
                Debug.LogWarning("[CombatUI] SetBonusUI component not found!");
            }
        }
    }

    // ✅ เพิ่มฟังก์ชันใหม่นี้
    public void ToggleSetBonusPanel()
    {
        if (setBonusUI != null)
        {
            setBonusUI.ToggleSetBonusPanel();
        }
        else
        {
            Debug.LogError("[CombatUI] SetBonusUI not assigned!");
        }
    }

    // ✅ เพิ่มฟังก์ชันใหม่นี้
    public void OpenSetBonusPanel()
    {
        if (setBonusUI != null)
        {
            setBonusUI.OpenSetBonusPanel();
        }
    }

    // ✅ เพิ่มฟังก์ชันใหม่นี้
    public void CloseSetBonusPanel()
    {
        if (setBonusUI != null)
        {
            setBonusUI.CloseSetBonusPanel();
        }
    }
    private void HandleEquipmentSlotClicked(EquipmentSlot slot)
    {
        if (slot == null || localHero == null) return;

        Debug.Log($"[CombatUI] Equipment slot clicked: {slot.SlotType}");

        // หา item ใน slot
        ItemData itemData = null;

        if (slot.SlotType == ItemType.Potion)
        {
            itemData = localHero.GetPotionInSlot(slot.PotionSlotIndex);
        }
        else
        {
            itemData = localHero.GetEquippedItem(slot.SlotType);
        }

        // แสดง item detail ถ้ามี item
        if (itemData != null)
        {
            ShowEquipmentItemDetail(itemData);
        }
        else
        {
            Debug.Log($"[CombatUI] No item in {slot.SlotType} slot");
        }
    }

    public void UpdateUI()
    {
        if (localHero == null || !localHero.IsSpawned) return; // เพิ่มการตรวจสอบ IsSpawned

        UpdatePotionButtons();
        UpdateSkillCooldowns();

        // ใช้ NetworkedCurrentHp/NetworkedMaxHp แทน
        if (healthBar != null && localHero.NetworkedMaxHp > 0)
        {
            float healthPercentage = (float)localHero.NetworkedCurrentHp / localHero.NetworkedMaxHp;
            healthBar.value = Mathf.Clamp01(healthPercentage);
        }

        if (healthText != null)
        {
            healthText.text = $"{localHero.NetworkedCurrentHp}/{localHero.NetworkedMaxHp}";
        }

        // ใช้ NetworkedCurrentMana/NetworkedMaxMana แทน
        if (manaBar != null && localHero.NetworkedMaxMana > 0)
        {
            float manaPercentage = (float)localHero.NetworkedCurrentMana / localHero.NetworkedMaxMana;
            manaBar.value = Mathf.Clamp01(manaPercentage);
        }

        if (manaText != null)
        {
            manaText.text = $"{localHero.NetworkedCurrentMana}/{localHero.NetworkedMaxMana}";
        }
    }
    // เพิ่มฟังก์ชันอัพเดท Character Stats ใน Inventory Panel
    public void UpdateInventoryCharacterStats()
    {
        if (localHero == null || !localHero.IsSpawned) return; // เพิ่มการตรวจสอบ IsSpawned
        UpdateCharacterPortrait();

        // Character Name & Level
        if (characterNameText != null)
        {
            characterNameText.text = localHero.CharacterName;
        }

        if (characterLevelText != null)
        {
            characterLevelText.text = $"Level {localHero.GetCurrentLevel()}";
        }

        // Health & Mana ใน Inventory (เหมือน Combat UI)
        if (inventoryHealthBar != null && localHero.NetworkedMaxHp > 0)
        {
            float healthPercentage = (float)localHero.NetworkedCurrentHp / localHero.NetworkedMaxHp;
            inventoryHealthBar.value = Mathf.Clamp01(healthPercentage);
        }

        if (inventoryHealthText != null)
        {
            inventoryHealthText.text = $"{localHero.NetworkedCurrentHp}/{localHero.NetworkedMaxHp}";
        }

        if (inventoryManaBar != null && localHero.NetworkedMaxMana > 0)
        {
            float manaPercentage = (float)localHero.NetworkedCurrentMana / localHero.NetworkedMaxMana;
            inventoryManaBar.value = Mathf.Clamp01(manaPercentage);
        }

        if (inventoryManaText != null)
        {
            inventoryManaText.text = $"{localHero.NetworkedCurrentMana}/{localHero.NetworkedMaxMana}";
        }
        if (HealthRegen != null)
        {
            HealthRegen.text = $"Heal+: {localHero.HealthRegen}/s";
        } if (ManaRegen != null)
        {
            ManaRegen.text = $"Mana+:{localHero.ManaRegen}/s";
        }
        // Combat Stats
        var statusEffectManager = localHero.GetComponent<StatusEffectManager>();
        if (statusEffectManager != null && statusEffectManager.IsReceivingAnyAura())
        {
            // แสดง Attack Speed
            if (attackSpeedText != null)
            {
                float multiplier = localHero.GetAttackSpeedMultiplierForUI();
                float auraBonus = statusEffectManager.ReceivedAttackSpeedBonus * 100f;

                if (auraBonus > 0)
                {
                    attackSpeedText.text = $"AS: x{multiplier:F2} <color=#FFD700>(+{auraBonus:F0}%)</color>";
                }
                else
                {
                    attackSpeedText.text = $"AS: x{multiplier:F2}";
                }
            }

            // แสดง Physical Damage
            if (attackDamageText != null)
            {
                // ✅ ใช้ GetAttackDamages() แทน AttackDamage โดยตรง
                var (physicalDamage, _) = localHero.GetAttackDamages();
                float damageBonus = statusEffectManager.ReceivedDamageBonus * 100f;

                if (damageBonus > 0)
                {
                    attackDamageText.text = $"ATK: {physicalDamage} <color=#FFD700>(+{damageBonus:F0}%)</color>";
                }
                else
                {
                    attackDamageText.text = $"ATK: {physicalDamage}";
                }
            }

            // ใน CombatUIManager.cs - UpdateInventoryCharacterStats()
            // แทนที่โค้ดส่วน Magic Damage (บรรทัด ~800-810)

            // ========== ✅ Magic Damage (แสดงแบบเดียวกับ ATK) ==========
            if (magicDamageText != null)
            {
                // ✅ ดึงค่า base magic damage (ไม่รวม aura)
                int baseMagicDamage = localHero.MagicDamage;

                // ✅ ดึง aura bonus
                float magicBonus = statusEffectManager.ReceivedMagicDamageBonus * 100f;

                // ✅ คำนวณ effective magic damage (รวม aura)
                int effectiveMagicDamage = Mathf.RoundToInt(baseMagicDamage * (1f + statusEffectManager.ReceivedMagicDamageBonus));

                if (magicBonus > 0)
                {
                    // แสดงแบบเดียวกับ ATK: effective (bonus)
                    magicDamageText.text = $"MAG: {effectiveMagicDamage} <color=#9D7FFF>(+{magicBonus:F0}%)</color>";
                }
                else
                {
                    magicDamageText.text = $"MAG: {baseMagicDamage}";
                }

                Debug.Log($"[Magic Damage UI] Base={baseMagicDamage}, Bonus={magicBonus}%, Effective={effectiveMagicDamage}");
            }

            // แสดง Move Speed
            if (moveSpeedText != null)
            {
                float speedBonus = statusEffectManager.ReceivedMoveSpeedBonus * 100f;
                float effectiveSpeed = localHero.GetEffectiveMoveSpeed();

                if (speedBonus > 0)
                {
                    moveSpeedText.text = $"SPD: {effectiveSpeed:F1} <color=#90EE90>(+{speedBonus:F0}%)</color>";
                }
                else
                {
                    moveSpeedText.text = $"SPD: {effectiveSpeed:F1}";
                }
            }

            // แสดง Armor
            // ========== ✅ Physical Armor (แก้ไข) ==========
            if (armorText != null)
            {
                // ✅ ใช้ GetEffectiveArmor() แทน Armor โดยตรง
                int effectiveArmor = localHero.GetEffectiveArmor();
                float armorBonus = statusEffectManager.ReceivedArmorBonus * 100f;

                if (armorBonus > 0)
                {
                    armorText.text = $"ARM: {effectiveArmor} <color=#C0C0C0>(+{armorBonus:F0}%)</color>";
                }
                else
                {
                    armorText.text = $"ARM: {effectiveArmor}";
                }
            }

            // ========== ✅ Magic Armor (แก้ไข) ==========
            if (magicArmor != null)
            {
                // ✅ ใช้ GetEffectiveMagicArmor() แทน MagicArmor โดยตรง
                int effectiveMagicArmor = localHero.GetEffectiveMagicArmor();
                float magicArmorBonus = statusEffectManager.ReceivedMagicArmorBonus * 100f;

                if (magicArmorBonus > 0)
                {
                    magicArmor.text = $"MGA: {effectiveMagicArmor} <color=#8080FF>(+{magicArmorBonus:F0}%)</color>";
                }
                else
                {
                    magicArmor.text = $"MGA: {effectiveMagicArmor}";
                }
            }

            // แสดง Critical Chance
            if (criticalChanceText != null)
            {
                // ✅ ใช้ GetEffectiveCriticalChance() แทน CriticalChance โดยตรง
                float effectiveCrit = localHero.GetEffectiveCriticalChance();
                float critBonus = statusEffectManager.ReceivedCriticalBonus * 100f;

                if (critBonus > 0)
                {
                    criticalChanceText.text = $"CRIT: {effectiveCrit:F1}% <color=#FFD700>(+{critBonus:F0}%)</color>";
                }
                else
                {
                    criticalChanceText.text = $"CRIT: {effectiveCrit:F1}%";
                }
            }

            // 🆕 แสดง Cooldown Reduction
            if (reductionCoolDownText != null)
            {
                float cdrBonus = statusEffectManager.ReceivedCooldownReductionBonus * 100f;
                float totalCDR = localHero.ReductionCoolDown + cdrBonus;

                if (cdrBonus > 0)
                {
                    reductionCoolDownText.text = $"CDR: {totalCDR:F1}% <color=#87CEEB>(+{cdrBonus:F0}%)</color>";
                }
                else
                {
                    reductionCoolDownText.text = $"CDR: {localHero.ReductionCoolDown:F1}%";
                }
            }

            // 🆕 แสดง Life Steal
            if (liftSteal != null)
            {
                float lifeStealBonus = statusEffectManager.ReceivedLifeStealBonus * 100f;
                float effectiveLifesteal = localHero.GetEffectiveLifeSteal() + lifeStealBonus;

                if (lifeStealBonus > 0)
                {
                    liftSteal.text = $"LST: {effectiveLifesteal:F1}% <color=#FF3333>(+{lifeStealBonus:F0}%)</color>";
                }
                else
                {
                    liftSteal.text = $"LST: {effectiveLifesteal:F1}%";
                }
            }

            // 🆕 แสดง Amp Damage
            if (ampDamage != null)
            {
                float ampBonus = statusEffectManager.ReceivedAmpDamageBonus * 100f;
                float totalAmp = localHero.AmpDamage + ampBonus;

                if (ampBonus > 0)
                {
                    ampDamage.text = $"AMP: {totalAmp:F1}% <color=#FF8000>(+{ampBonus:F0}%)</color>";
                }
                else
                {
                    ampDamage.text = $"AMP: {localHero.AmpDamage:F1}%";
                }
            }

            // 🆕 แสดง Health Regen
            if (HealthRegen != null)
            {
                float regenBonus = statusEffectManager.ReceivedHealthRegenBonus * 100f;
                float effectiveRegen = localHero.HealthRegen;

                if (regenBonus > 0)
                {
                    HealthRegen.text = $"HP+: {effectiveRegen:F1}/s <color=#32CD32>(+{regenBonus:F0}%)</color>";
                }
                else
                {
                    HealthRegen.text = $"HP+: {effectiveRegen:F1}/s";
                }
            }

            // 🆕 แสดง Mana Regen
            if (ManaRegen != null)
            {
                float manaRegenBonus = statusEffectManager.ReceivedManaRegenBonus * 100f;
                float effectiveManaRegen = localHero.ManaRegen;

                if (manaRegenBonus > 0)
                {
                    ManaRegen.text = $"MP+: {effectiveManaRegen:F1}/s <color=#4D9BFF>(+{manaRegenBonus:F0}%)</color>";
                }
                else
                {
                    ManaRegen.text = $"MP+: {effectiveManaRegen:F1}/s";
                }
            }

            // 🆕 แสดง Hit Rate
            if (hitRateText != null)
            {
                float hitBonus = statusEffectManager.ReceivedHitRateBonus * 100f;
                float totalHitRate = localHero.HitRate + hitBonus;

                if (hitBonus > 0)
                {
                    hitRateText.text = $"HIT: {totalHitRate:F1}% <color=#FFFF80>(+{hitBonus:F0}%)</color>";
                }
                else
                {
                    hitRateText.text = $"HIT: {localHero.HitRate:F1}%";
                }
            }

            // 🆕 แสดง Evasion Rate
            if (evasionRateText != null)
            {
                float evasionBonus = statusEffectManager.ReceivedEvasionBonus * 100f;
                float totalEvasion = localHero.EvasionRate + evasionBonus;

                if (evasionBonus > 0)
                {
                    evasionRateText.text = $"EVA: {totalEvasion:F1}% <color=#CCCCCC>(+{evasionBonus:F0}%)</color>";
                }
                else
                {
                    evasionRateText.text = $"EVA: {localHero.EvasionRate:F1}%";
                }
            }

            // แสดง Critical Damage (ไม่มี aura buff)
            if (criticalDamageText != null)
            {
                criticalDamageText.text = $"CRIT DMG: {localHero.GetEffectiveCriticalDamageBonus() * 100f:F1}%";
            }
        }
        else
        {
            // ไม่มี aura - แสดงแบบปกติ (โค้ดเดิม)
            if (attackDamageText != null)
                attackDamageText.text = $"ATK: {localHero.AttackDamage}";

            if (magicDamageText != null)
                magicDamageText.text = $"MAG: {localHero.MagicDamage}";

            if (armorText != null)
                armorText.text = $"ARM: {localHero.Armor}";

            if (magicArmor != null)
                magicArmor.text = $"MGA: {localHero.MagicArmor}";

            if (moveSpeedText != null)
                moveSpeedText.text = $"SPD: {localHero.GetEffectiveMoveSpeed():F1}";

            if (criticalChanceText != null)
                criticalChanceText.text = $"CRIT: {localHero.CriticalChance:F1}%";

            if (criticalDamageText != null)
                criticalDamageText.text = $"CRIT DMG: {localHero.GetEffectiveCriticalDamageBonus() * 100f:F1}%";

            if (hitRateText != null)
                hitRateText.text = $"HIT: {localHero.HitRate:F1}%";

            if (evasionRateText != null)
                evasionRateText.text = $"EVA: {localHero.EvasionRate:F1}%";

            if (reductionCoolDownText != null)
                reductionCoolDownText.text = $"CDR: {localHero.ReductionCoolDown:F1}%";

            if (ampDamage != null)
                ampDamage.text = $"AMP: {localHero.AmpDamage:F1}%";

            if (attackSpeedText != null)
            {
                float multiplier = localHero.GetAttackSpeedMultiplierForUI();
                attackSpeedText.text = $"AS: x{multiplier:F2}";
            }

            if (liftSteal != null)
            {
                float effectiveLifesteal = localHero.GetEffectiveLifeSteal();
                liftSteal.text = $"LST: {effectiveLifesteal:F1}%";
            }

            if (HealthRegen != null)
                HealthRegen.text = $"HP+: {localHero.HealthRegen:F1}/s";

            if (ManaRegen != null)
                ManaRegen.text = $"MP+: {localHero.ManaRegen:F1}/s";
        }

    }

    public void ShowEquipmentItemDetail(ItemData itemData)
    {
        if (itemDetailManager != null && itemData != null)
        {
            // สร้าง InventoryItem temporary
            InventoryItem displayItem = new InventoryItem(itemData, 1, -1);
            itemDetailManager.ShowItemDetail(displayItem);

            //Debug.Log($"[CombatUI] Showing equipment item detail: {itemData.ItemName}");
        }
        else
        {
            Debug.LogWarning("[CombatUI] Cannot show item detail - missing manager or item data");
        }
    }
    public void LoadSkillIconsForCurrentHero()
    {
        if (PersistentPlayerData.Instance == null)
        {
            Debug.LogWarning("[CombatUI] PersistentPlayerData not found!");
            return;
        }

        string currentHero = PersistentPlayerData.Instance.GetCurrentActiveCharacter();

        Debug.Log($"[CombatUI] 🎨 Loading skill icons for: {currentHero}");

        SkillIconManager.Instance.SetSkillIcons(this, currentHero);
    }
    // 🆕 เพิ่ม method สำหรับซ่อน item detail
    public void HideEquipmentItemDetail()
    {
        if (itemDetailManager != null)
        {
            itemDetailManager.HideItemDetail();
        }
    }
    public bool IsInventoryOpen()
    {
        return isInventoryOpen;
    }
    // 🎯 NEW: Public methods for accessing inventory grid
    public InventoryGridManager GetInventoryGridManager()
    {
        return inventoryGridManager;
    }

    public int GetSelectedSlotIndex()
    {
        return selectedSlotIndex;
    }


    // 🎯 NEW: Context Menu สำหรับทดสอบ
   
}