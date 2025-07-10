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

  
    [Header("Character Stats in Inventory")]
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI characterLevelText;
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

    [Header("🆕 Item Detail Panel")]
    public GameObject itemDetailPanel;              // Panel สำหรับแสดงรายละเอียดไอเทม
    public ItemDetailPanel itemDetailManager;      // Manager สำหรับจัดการ panel

    [Header("🆕 Equipment Slots (ลากจาก UI มาใส่)")]
    public List<EquipmentSlot> equipmentSlots = new List<EquipmentSlot>(); // Head, Armor, Weapon, Pants, Shoes, Rune
    public List<EquipmentSlot> potionSlots = new List<EquipmentSlot>();    // Potion quick slots (5 slots)
    public EquipmentSlotManager equipmentSlotManager; // Manager สำหรับจัดการ equipment slots

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

    private void Start()
    {
        Debug.Log("CombatUIManager Start");
        CreateCombatUIFromPrefab();

        // ใช้ Coroutine เพื่อหา InputController
        StartCoroutine(FindInputControllerRoutine());
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
    }

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

    private void CreateCombatUIFromPrefab()
    {
        if (combatUIPrefab == null)
        {
            Debug.LogError("Combat UI Prefab not assigned!");
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas not found!");
            return;
        }

        Transform safeArea = canvas.transform.Find("SafeArea");
        if (safeArea == null)
        {
            GameObject safeAreaObj = new GameObject("SafeArea");
            safeAreaObj.transform.SetParent(canvas.transform, false);

            RectTransform safeAreaRect = safeAreaObj.AddComponent<RectTransform>();
            safeAreaRect.anchorMin = Vector2.zero;
            safeAreaRect.anchorMax = Vector2.one;
            safeAreaRect.offsetMin = Vector2.zero;
            safeAreaRect.offsetMax = Vector2.zero;

            safeArea = safeAreaRect;
        }

        uiInstance = Instantiate(combatUIPrefab, safeArea);
        Debug.Log($"UI Instance created: {uiInstance.name}");

        SetupInventoryPanel();
    }

    // 🎯 UPDATED: Setup Inventory Panel with Grid System
    private void SetupInventoryPanel()
    {
        Debug.Log("=== Setting up Inventory Panel with Grid System ===");

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            Debug.Log("✅ Inventory Panel initialized (hidden)");

            // 🎯 Setup Inventory Grid
            SetupInventoryGrid();

            // Setup Item Info Panel
            SetupItemDetailPanel();

        }
        else
        {
            Debug.LogError("❌ Inventory Panel not assigned in Inspector!");
        }
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
        }
        else
        {
            Debug.Log("[CombatUI] Inventory grid already exists");
        }
    }

    // ✅ แก้ไข SetupInventoryGrid ให้รอนานกว่า
    private void SetupInventoryGrid()
    {
        Debug.Log("=== Setting up Inventory Grid ===");

        // ✅ ลบการ force activate panel ออก
        // ให้ grid สร้างได้โดยไม่ต้อง activate

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

        Debug.Log("✅ Inventory Grid setup complete");
    }
    private void ConnectInventoryToHero(Hero hero)
    {
        if (inventoryGridManager != null)
        {
            inventoryGridManager.SetOwnerCharacter(hero);

            // ✅ ใช้ StartCoroutine แทน direct call
            StartCoroutine(DelayedInventorySetup());

            Debug.Log($"[CombatUI] Connected inventory grid to {hero.CharacterName}");
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

            // 🆕 แสดง item detail ถ้ามี item ใน slot
            ShowItemDetailForSlot(slotIndex);
        }
        else
        {
            Debug.Log("[CombatUI] No slot selected");

            // 🆕 ซ่อน item detail panel
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

    private void SetupButtonEvents()
    {
        if (inputController == null)
        {
            Debug.LogError("InputController still not found when setting up buttons!");
            return;
        }

        Debug.Log("=== Setting up UI Button Events ===");
        SetupPotionButtons();

        if (attackButton != null)
        {
            attackButton.onClick.RemoveAllListeners();
            attackButton.onClick.AddListener(() => {
                Debug.Log("Attack button pressed");
                inputController.SetAttackPressed();
            });
            Debug.Log("✅ Attack button event setup complete");
        }
        else Debug.LogWarning("❌ Attack button not assigned in Inspector!");

        if (skill1Button != null)
        {
            skill1Button.onClick.RemoveAllListeners();
            skill1Button.onClick.AddListener(() => {
                Debug.Log("Skill1 button pressed");
                inputController.SetSkill1Pressed();
            });
            Debug.Log("✅ Skill1 button event setup complete");
        }
        else Debug.LogWarning("❌ Skill1 button not assigned in Inspector!");

        if (skill2Button != null)
        {
            skill2Button.onClick.RemoveAllListeners();
            skill2Button.onClick.AddListener(() => {
                Debug.Log("Skill2 button pressed");
                inputController.SetSkill2Pressed();
            });
            Debug.Log("✅ Skill2 button event setup complete");
        }
        else Debug.LogWarning("❌ Skill2 button not assigned in Inspector!");

        if (skill3Button != null)
        {
            skill3Button.onClick.RemoveAllListeners();
            skill3Button.onClick.AddListener(() => {
                Debug.Log("Skill3 button pressed");
                inputController.SetSkill3Pressed();
            });
            Debug.Log("✅ Skill3 button event setup complete");
        }
        else Debug.LogWarning("❌ Skill3 button not assigned in Inspector!");

        if (skill4Button != null)
        {
            skill4Button.onClick.RemoveAllListeners();
            skill4Button.onClick.AddListener(() => {
                Debug.Log("Skill4 button pressed");
                inputController.SetSkill4Pressed();
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

        Debug.Log("=== UI Button Events Setup Complete ===");

      
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
            inventoryPanel.SetActive(true);
            isInventoryOpen = true;

            // อัพเดท Character Stats ทันทีที่เปิด Panel
            if (localHero != null)
            {
                UpdateInventoryCharacterStats();
            }

            // ✅ เพิ่มบรรทัดนี้เพื่อ force update inventory grid
            if (inventoryGridManager != null)
            {
                inventoryGridManager.ForceUpdateFromCharacter();
                Debug.Log("[CombatUI] Forced inventory grid update");
            }

            Debug.Log("Inventory panel opened");
        }
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

            // 🆕 ซ่อน item detail panel ด้วย
            HideItemDetail();

            Debug.Log("Inventory panel closed");
        }
    }

    public void SetLocalHero(Hero hero)
    {
        localHero = hero;
        Debug.Log($"Local hero set: {hero.CharacterName} - HP: {hero.CurrentHp}/{hero.MaxHp}");

        // ✅ เชื่อมต่อ Inventory Grid
        ConnectInventoryToHero(hero);

        // 🆕 เชื่อมต่อ Equipment Slots ผ่าน EquipmentSlotManager
        ConnectEquipmentSlotsToHero(hero);

        UpdateUI();
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
        if (localHero == null) return;
        UpdatePotionButtons();

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
        if (localHero == null) return;

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

        // Combat Stats
        if (attackDamageText != null)
        {
            attackDamageText.text = $"ATK: {localHero.AttackDamage}";
        }

        if (magicDamageText != null)
        {
            magicDamageText.text = $"MAG: {localHero.MagicDamage}";
        }

        if (armorText != null)
        {
            armorText.text = $"ARM: {localHero.Armor}";
        }

        if (moveSpeedText != null)
        {
            moveSpeedText.text = $"SPD: {localHero.GetEffectiveMoveSpeed():F1}";
        }

        if (criticalChanceText != null)
        {
            criticalChanceText.text = $"CRIT: {localHero.CriticalChance:F1}%";
        }

        if (criticalDamageText != null)
        {
            criticalDamageText.text = $"CRIT DMG: {localHero.GetEffectiveCriticalDamageBonus() * 100f:F1}%";
        }


        if (hitRateText != null)
        {
            hitRateText.text = $"HIT: {localHero.HitRate:F1}%";
        }

        if (evasionRateText != null)
        {
            evasionRateText.text = $"EVA: {localHero.EvasionRate:F1}%";
        }

        if (attackSpeedText != null)
        {
            // ใช้ฟังก์ชันใหม่สำหรับ UI
            float multiplier = localHero.GetAttackSpeedMultiplierForUI();
            attackSpeedText.text = $"AS: x{multiplier:F2}";
        }

    }

    public void ShowEquipmentItemDetail(ItemData itemData)
    {
        if (itemDetailManager != null && itemData != null)
        {
            // สร้าง InventoryItem temporary
            InventoryItem displayItem = new InventoryItem(itemData, 1, -1);
            itemDetailManager.ShowItemDetail(displayItem);

            Debug.Log($"[CombatUI] Showing equipment item detail: {itemData.ItemName}");
        }
        else
        {
            Debug.LogWarning("[CombatUI] Cannot show item detail - missing manager or item data");
        }
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