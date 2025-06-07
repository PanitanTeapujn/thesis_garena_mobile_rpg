using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

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
    public Slider healthBar;
    public Slider manaBar;
    public Text healthText;
    public Text manaText;
    public FixedJoystick movementJoystick;
    public FixedJoystick cameraJoystick;

    private Hero localHero;
    private SingleInputController inputController;
    private GameObject uiInstance;

    // เพิ่มการรอหา InputController
    private bool inputControllerFound = false;

    private void Start()
    {
        Debug.Log("CombatUIManager Start");
        CreateCombatUIFromPrefab();

        // ใช้ Coroutine เพื่อหา InputController
        StartCoroutine(FindInputControllerRoutine());
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

        healthBar = FindUIComponent<Slider>("HealthBar");
        manaBar = FindUIComponent<Slider>("ManaBar");

        healthText = FindUIComponent<Text>("HealthText");
        manaText = FindUIComponent<Text>("ManaText");

        movementJoystick = FindUIComponent<FixedJoystick>("JoystickCharacter");
        cameraJoystick = FindUIComponent<FixedJoystick>("CameraJoystick");

        Debug.Log($"UI Setup Results:");
        Debug.Log($"- Attack Button: {attackButton != null}");
        Debug.Log($"- Health Bar: {healthBar != null}");
        Debug.Log($"- Mana Bar: {manaBar != null}");
        Debug.Log($"- Movement Joystick: {movementJoystick != null}");
        Debug.Log($"- Camera Joystick: {cameraJoystick != null}");
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
    }

    public void SetLocalHero(Hero hero)
    {
        localHero = hero;
        Debug.Log($"Local hero set: {hero.CharacterName} - HP: {hero.CurrentHp}/{hero.MaxHp}");
        UpdateUI();
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

        // อัพเดท UI
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