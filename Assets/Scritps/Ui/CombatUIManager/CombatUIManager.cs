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
    public Button inventoryButton; // ✅ เพิ่มปุ่ม Inventory
    public Slider healthBar;
    public Slider manaBar;
    public TextMeshProUGUI healthText; // ✅ เปลี่ยนเป็น TextMeshPro
    public TextMeshProUGUI manaText; // ✅ เปลี่ยนเป็น TextMeshPro
    public FixedJoystick movementJoystick;
    public FixedJoystick cameraJoystick;

    [Header("Inventory Panel")]
    public GameObject inventoryPanel; // ✅ เพิ่ม Inventory Panel
    public Button inventoryCloseButton; // ✅ ปุ่มปิด Inventory

    [Header("Character Stats in Inventory")]
    public TextMeshProUGUI characterNameText; // ✅ เปลี่ยนเป็น TextMeshPro
    public TextMeshProUGUI characterLevelText; // ✅ เปลี่ยนเป็น TextMeshPro
    public Slider inventoryHealthBar;
    public Slider inventoryManaBar;
    public TextMeshProUGUI inventoryHealthText; // ✅ เปลี่ยนเป็น TextMeshPro
    public TextMeshProUGUI inventoryManaText; // ✅ เปลี่ยนเป็น TextMeshPro
    public TextMeshProUGUI attackDamageText; // ✅ เปลี่ยนเป็น TextMeshPro
    public TextMeshProUGUI magicDamageText; // ✅ เปลี่ยนเป็น TextMeshPro
    public TextMeshProUGUI armorText; // ✅ เปลี่ยนเป็น TextMeshPro
    public TextMeshProUGUI moveSpeedText; // ✅ เปลี่ยนเป็น TextMeshPro
    public TextMeshProUGUI criticalChanceText; // ✅ เปลี่ยนเป็น TextMeshPro
    public TextMeshProUGUI criticalDamageText; // ✅ เปลี่ยนเป็น TextMeshPro
    public TextMeshProUGUI hitRateText; // ✅ เปลี่ยนเป็น TextMeshPro
    public TextMeshProUGUI evasionRateText; // ✅ เปลี่ยนเป็น TextMeshPro
    public TextMeshProUGUI attackSpeedText; // ✅ เปลี่ยนเป็น TextMeshPro

    public Hero localHero { get; private set; }
    private SingleInputController inputController;
    private GameObject uiInstance;

    // เพิ่มการรอหา InputController
    private bool inputControllerFound = false;

    // ✅ เพิ่มตัวแปรสถานะ Inventory
    private bool isInventoryOpen = false;

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

            // ✅ อัพเดท Character Stats ใน Inventory Panel ถ้าเปิดอยู่
            if (isInventoryOpen)
            {
                UpdateInventoryCharacterStats();
            }
        }
    }

    // เพิ่ม Coroutine สำหรับหา InputController
    private IEnumerator FindInputControllerRoutine()
    {
        float timeout = 5f; // รอสูงสุด 5 วินาที
        float elapsed = 0f;

        while (inputController == null && elapsed < timeout)
        {
            inputController = FindObjectOfType<SingleInputController>();

            if (inputController != null)
            {
                Debug.Log("InputController found!");
                inputControllerFound = true;

                // อัพเดท joystick references
                inputController.UpdateJoystickReferences(movementJoystick, cameraJoystick);

                // Setup button events
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

        SetupUIReferences();
        SetupInventoryPanel(); // ✅ Setup Inventory Panel
        // ไม่เรียก SetupButtonEvents() ที่นี่ เพราะจะเรียกใน Coroutine
    }

    private void SetupUIReferences()
    {
        if (uiInstance == null) return;

        // หา UI elements
        attackButton = FindUIComponent<Button>("AttackButton");
        skill1Button = FindUIComponent<Button>("Skill1Button");
        skill2Button = FindUIComponent<Button>("Skill2Button");
        skill3Button = FindUIComponent<Button>("Skill3Button");
        skill4Button = FindUIComponent<Button>("Skill4Button");
        inventoryButton = FindUIComponent<Button>("InventoryButton"); // ✅ หาปุ่ม Inventory

        healthBar = FindUIComponent<Slider>("HealthBar");
        manaBar = FindUIComponent<Slider>("ManaBar");

        healthText = FindUIComponent<TextMeshProUGUI>("HealthText"); // ✅ เปลี่ยนเป็น TextMeshPro
        manaText = FindUIComponent<TextMeshProUGUI>("ManaText"); // ✅ เปลี่ยนเป็น TextMeshPro

        movementJoystick = FindUIComponent<FixedJoystick>("JoystickCharacter");
        cameraJoystick = FindUIComponent<FixedJoystick>("CameraJoystick");

        // ✅ หา Inventory Panel
        inventoryPanel = FindUIComponent<RectTransform>("InventoryPanel").gameObject;
        inventoryCloseButton = FindUIComponent<Button>("InventoryCloseButton");

        // ✅ หา Character Stats UI ใน Inventory Panel
        characterNameText = FindUIComponent<TextMeshProUGUI>("CharacterNameText"); // ✅ เปลี่ยนเป็น TextMeshPro
        characterLevelText = FindUIComponent<TextMeshProUGUI>("CharacterLevelText"); // ✅ เปลี่ยนเป็น TextMeshPro
        inventoryHealthBar = FindUIComponent<Slider>("InventoryHealthBar");
        inventoryManaBar = FindUIComponent<Slider>("InventoryManaBar");
        inventoryHealthText = FindUIComponent<TextMeshProUGUI>("InventoryHealthText"); // ✅ เปลี่ยนเป็น TextMeshPro
        inventoryManaText = FindUIComponent<TextMeshProUGUI>("InventoryManaText"); // ✅ เปลี่ยนเป็น TextMeshPro
        attackDamageText = FindUIComponent<TextMeshProUGUI>("AttackDamageText"); // ✅ เปลี่ยนเป็น TextMeshPro
        magicDamageText = FindUIComponent<TextMeshProUGUI>("MagicDamageText"); // ✅ เปลี่ยนเป็น TextMeshPro
        armorText = FindUIComponent<TextMeshProUGUI>("ArmorText"); // ✅ เปลี่ยนเป็น TextMeshPro
        moveSpeedText = FindUIComponent<TextMeshProUGUI>("MoveSpeedText"); // ✅ เปลี่ยนเป็น TextMeshPro
        criticalChanceText = FindUIComponent<TextMeshProUGUI>("CriticalChanceText"); // ✅ เปลี่ยนเป็น TextMeshPro
        criticalDamageText = FindUIComponent<TextMeshProUGUI>("CriticalDamageText"); // ✅ เปลี่ยนเป็น TextMeshPro
        hitRateText = FindUIComponent<TextMeshProUGUI>("HitRateText"); // ✅ เปลี่ยนเป็น TextMeshPro
        evasionRateText = FindUIComponent<TextMeshProUGUI>("EvasionRateText"); // ✅ เปลี่ยนเป็น TextMeshPro
        attackSpeedText = FindUIComponent<TextMeshProUGUI>("AttackSpeedText"); // ✅ เปลี่ยนเป็น TextMeshPro

        Debug.Log($"UI Setup Results:");
        Debug.Log($"- Attack Button: {attackButton != null}");
        Debug.Log($"- Inventory Button: {inventoryButton != null}"); // ✅ เพิ่ม debug
        Debug.Log($"- Health Bar: {healthBar != null}");
        Debug.Log($"- Mana Bar: {manaBar != null}");
        Debug.Log($"- Movement Joystick: {movementJoystick != null}");
        Debug.Log($"- Camera Joystick: {cameraJoystick != null}");
        Debug.Log($"- Inventory Panel: {inventoryPanel != null}"); // ✅ เพิ่ม debug
        Debug.Log($"- Character Stats UI Found: {characterNameText != null && attackDamageText != null}"); // ✅ เพิ่ม debug
    }

    // ✅ เพิ่มฟังก์ชัน Setup Inventory Panel
    private void SetupInventoryPanel()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false); // เริ่มต้นซ่อน Panel
            Debug.Log("Inventory Panel initialized (hidden)");
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

        if (attackButton != null)
        {
            // ลบ listener เก่าก่อน (ป้องกันการ add ซ้ำ)
            attackButton.onClick.RemoveAllListeners();
            attackButton.onClick.AddListener(() => {
                Debug.Log("Attack button pressed");
                inputController.SetAttackPressed();
            });
            Debug.Log("Attack button event setup complete");
        }

        if (skill1Button != null)
        {
            skill1Button.onClick.RemoveAllListeners();
            skill1Button.onClick.AddListener(() => {
                Debug.Log("Skill1 button pressed");
                inputController.SetSkill1Pressed();
            });
            Debug.Log("Skill1 button event setup complete");
        }

        if (skill2Button != null)
        {
            skill2Button.onClick.RemoveAllListeners();
            skill2Button.onClick.AddListener(() => {
                Debug.Log("Skill2 button pressed");
                inputController.SetSkill2Pressed();
            });
            Debug.Log("Skill2 button event setup complete");
        }

        if (skill3Button != null)
        {
            skill3Button.onClick.RemoveAllListeners();
            skill3Button.onClick.AddListener(() => {
                Debug.Log("Skill3 button pressed");
                inputController.SetSkill3Pressed();
            });
            Debug.Log("Skill3 button event setup complete");
        }

        if (skill4Button != null)
        {
            skill4Button.onClick.RemoveAllListeners();
            skill4Button.onClick.AddListener(() => {
                Debug.Log("Skill4 button pressed");
                inputController.SetSkill4Pressed();
            });
            Debug.Log("Skill4 button event setup complete");
        }

        // ✅ Setup Inventory Button
        if (inventoryButton != null)
        {
            inventoryButton.onClick.RemoveAllListeners();
            inventoryButton.onClick.AddListener(() => {
                Debug.Log("Inventory button pressed");
                ToggleInventory();
            });
            Debug.Log("Inventory button event setup complete");
        }

        // ✅ Setup Inventory Close Button
        if (inventoryCloseButton != null)
        {
            inventoryCloseButton.onClick.RemoveAllListeners();
            inventoryCloseButton.onClick.AddListener(() => {
                Debug.Log("Inventory close button pressed");
                CloseInventory();
            });
            Debug.Log("Inventory close button event setup complete");
        }
    }

    // ✅ เพิ่มฟังก์ชัน Inventory
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

    public void OpenInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
            isInventoryOpen = true;

            // ✅ อัพเดท Character Stats ทันทีที่เปิด Panel
            if (localHero != null)
            {
                UpdateInventoryCharacterStats();
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
            Debug.Log("Inventory panel closed");
        }
    }

    public void SetLocalHero(Hero hero)
    {
        localHero = hero;
        Debug.Log($"Local hero set: {hero.CharacterName} - HP: {hero.CurrentHp}/{hero.MaxHp}");
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (localHero == null) return;

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

    // ✅ เพิ่มฟังก์ชันอัพเดท Character Stats ใน Inventory Panel
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
            criticalDamageText.text = $"CRIT DMG: {localHero.GetEffectiveCriticalDamageBonus():F1}%";
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
            attackSpeedText.text = $"AS: {localHero.GetEffectiveAttackSpeed():F2}";
        }
    }
}