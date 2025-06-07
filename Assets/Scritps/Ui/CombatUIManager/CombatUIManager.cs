using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class CombatUIManager : MonoBehaviour
{
    [Header("UI Prefab")]
    public GameObject combatUIPrefab;

    [Header("UI References - จะถูกตั้งค่าอัตโนมัติจาก Prefab")]
    public Button attackButton;
    public Button skill1Button;
    public Button skill2Button;
    public Slider healthBar;
    public Slider manaBar;
    public Text healthText;
    public Text manaText;

    // เพิ่ม Joystick references
    public FixedJoystick movementJoystick;
    public FixedJoystick cameraJoystick;

    private Hero localHero;
    private SingleInputController inputController;
    private GameObject uiInstance;

    private void Start()
    {
        Debug.Log("CombatUIManager Start");
        CreateCombatUIFromPrefab();

        // หา InputController และอัพเดท joystick references
        inputController = FindObjectOfType<SingleInputController>();
        if (inputController != null)
        {
            inputController.UpdateJoystickReferences(movementJoystick, cameraJoystick);
            Debug.Log("Updated InputController with new joystick references");
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
            Debug.Log("Created SafeArea");
        }

        uiInstance = Instantiate(combatUIPrefab, safeArea);
        Debug.Log($"UI Instance created: {uiInstance.name}");

        SetupUIReferences();
        SetupButtonEvents();
    }

    private void SetupUIReferences()
    {
        if (uiInstance == null)
        {
            Debug.LogError("UI Instance is null!");
            return;
        }

        // หา UI elements โดยใช้วิธีที่มั่นใจกว่า
        attackButton = FindUIComponent<Button>("AttackButton");
        skill1Button = FindUIComponent<Button>("Skill1Button");
        skill2Button = FindUIComponent<Button>("Skill2Button");

        healthBar = FindUIComponent<Slider>("HealthBar");
        manaBar = FindUIComponent<Slider>("ManaBar");

        healthText = FindUIComponent<Text>("HealthText");
        manaText = FindUIComponent<Text>("ManaText");

        // หา Joysticks
        movementJoystick = FindUIComponent<FixedJoystick>("JoystickCharacter");
        cameraJoystick = FindUIComponent<FixedJoystick>("CameraJoystick");

        // Debug log
        Debug.Log($"UI Setup Results:");
        Debug.Log($"- Attack Button: {attackButton != null}");
        Debug.Log($"- Health Bar: {healthBar != null}");
        Debug.Log($"- Mana Bar: {manaBar != null}");
        Debug.Log($"- Movement Joystick: {movementJoystick != null}");
        Debug.Log($"- Camera Joystick: {cameraJoystick != null}");
    }

    private T FindUIComponent<T>(string name) where T : Component
    {
        // วิธีที่ 1: หาจาก direct child
        Transform directChild = uiInstance.transform.Find(name);
        if (directChild != null)
        {
            T component = directChild.GetComponent<T>();
            if (component != null)
            {
                Debug.Log($"Found {name} as direct child");
                return component;
            }
        }

        // วิธีที่ 2: หาจาก children ทั้งหมด
        T[] allComponents = uiInstance.GetComponentsInChildren<T>(true);
        foreach (T comp in allComponents)
        {
            if (comp.gameObject.name == name)
            {
                Debug.Log($"Found {name} in children");
                return comp;
            }
        }

        // วิธีที่ 3: หาจาก type เฉพาะกรณีที่มีตัวเดียว
        if (allComponents.Length == 1)
        {
            Debug.Log($"Found single {typeof(T).Name} component, assuming it's {name}");
            return allComponents[0];
        }

        Debug.LogWarning($"Could not find {name} of type {typeof(T).Name}");
        return null;
    }

    private void SetupButtonEvents()
    {
        if (inputController == null)
        {
            Debug.LogError("InputController not found!");
            return;
        }

        if (attackButton != null)
        {
            attackButton.onClick.AddListener(() => {
                Debug.Log("Attack button pressed");
                inputController.SetAttackPressed();
            });
            Debug.Log("Attack button event setup complete");
        }

        if (skill1Button != null)
        {
            skill1Button.onClick.AddListener(() => {
                Debug.Log("Skill1 button pressed");
                inputController.SetSkill1Pressed();
            });
        }

        if (skill2Button != null)
        {
            skill2Button.onClick.AddListener(() => {
                Debug.Log("Skill2 button pressed");
                inputController.SetSkill2Pressed();
            });
        }
    }

    public void SetLocalHero(Hero hero)
    {
        localHero = hero;
        Debug.Log($"Local hero set: {hero.CharacterName}");
        UpdateUI();
    }

    private void Update()
    {
        if (localHero != null)
        {
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (localHero == null) return;

        // Update Health Bar
        if (healthBar != null)
        {
            float healthPercentage = (float)localHero.CurrentHp / localHero.MaxHp;
            healthBar.value = healthPercentage;
        }

        if (healthText != null)
        {
            healthText.text = $"{localHero.CurrentHp}/{localHero.MaxHp}";
        }

        // Update Mana Bar
        if (manaBar != null)
        {
            float manaPercentage = (float)localHero.CurrentMana / localHero.MaxMana;
            manaBar.value = manaPercentage;
        }

        if (manaText != null)
        {
            manaText.text = $"{localHero.CurrentMana}/{localHero.MaxMana}";
        }
    }
}