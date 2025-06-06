using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class CombatUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject combatUIPrefab; // Prefab ที่มี UI ทั้งหมด
    public Transform safeAreaTransform; // Reference ไปยัง Safe Area ใน Canvas

    [Header("Combat UI Elements")]
    public Button attackButton;
    public Slider healthBar;
    public Text healthText;
    public GameObject skillButtonsContainer;

    private Hero localHero;
    private SingleInputController inputController;

    private void Start()
    {
        // หา Safe Area ถ้ายังไม่ได้ assign
        if (safeAreaTransform == null)
        {
            GameObject safeArea = GameObject.Find("SafeArea");
            if (safeArea != null)
            {
                safeAreaTransform = safeArea.transform;
            }
            else
            {
                Debug.LogError("SafeArea not found! Please create SafeArea in Canvas.");
                return;
            }
        }

        // สร้าง Combat UI ใน Safe Area
        CreateCombatUI();

        // หา Input Controller
        inputController = FindObjectOfType<SingleInputController>();
    }

    private void CreateCombatUI()
    {
        if (combatUIPrefab != null && safeAreaTransform != null)
        {
            // สร้าง UI instance ใน Safe Area
            GameObject uiInstance = Instantiate(combatUIPrefab, safeAreaTransform);

            // ตั้งค่า RectTransform ให้เต็ม Safe Area
            RectTransform rectTransform = uiInstance.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
            }

            // Get references from instantiated UI
            attackButton = uiInstance.GetComponentInChildren<Button>();
            healthBar = uiInstance.GetComponentInChildren<Slider>();
            healthText = uiInstance.GetComponentInChildren<Text>();

            // Setup attack button
            if (attackButton != null)
            {
                attackButton.onClick.AddListener(OnAttackButtonPressed);
            }
        }
        else
        {
            // ถ้าไม่มี prefab ให้สร้าง UI แบบ manual
            CreateManualCombatUI();
        }
    }

    private void CreateManualCombatUI()
    {
        // สร้าง Container สำหรับ Combat UI
        GameObject combatUIContainer = new GameObject("CombatUI");
        combatUIContainer.transform.SetParent(safeAreaTransform, false);

        RectTransform containerRect = combatUIContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        // สร้างปุ่มโจมตี
        CreateAttackButton(combatUIContainer.transform);

        // สร้าง Health Bar
        CreateHealthBar(combatUIContainer.transform);
    }

    private void CreateAttackButton(Transform parent)
    {
        // สร้าง Attack Button
        GameObject attackButtonObj = new GameObject("AttackButton");
        attackButtonObj.transform.SetParent(parent, false);

        RectTransform rectTransform = attackButtonObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.8f, 0.1f);
        rectTransform.anchorMax = new Vector2(0.95f, 0.25f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        // เพิ่ม Image component
        Image buttonImage = attackButtonObj.AddComponent<Image>();
        buttonImage.color = new Color(1f, 0.2f, 0.2f, 0.8f);

        // เพิ่ม Button component
        attackButton = attackButtonObj.AddComponent<Button>();
        attackButton.onClick.AddListener(OnAttackButtonPressed);

        // เพิ่ม Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(attackButtonObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = "Attack";
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 24;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = Color.white;
    }

    private void CreateHealthBar(Transform parent)
    {
        // สร้าง Health Bar Container
        GameObject healthBarContainer = new GameObject("HealthBar");
        healthBarContainer.transform.SetParent(parent, false);

        RectTransform containerRect = healthBarContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.05f, 0.85f);
        containerRect.anchorMax = new Vector2(0.35f, 0.95f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        // Background
        Image bgImage = healthBarContainer.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // Health Bar Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(healthBarContainer.transform, false);

        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = new Vector2(5, 5);
        fillRect.offsetMax = new Vector2(-5, -5);

        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);

        // Create Slider
        healthBar = healthBarContainer.AddComponent<Slider>();
        healthBar.fillRect = fillRect;
        healthBar.targetGraphic = fillImage;
        healthBar.minValue = 0;
        healthBar.maxValue = 100;
        healthBar.value = 100;

        // Health Text
        GameObject textObj = new GameObject("HealthText");
        textObj.transform.SetParent(healthBarContainer.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        healthText = textObj.AddComponent<Text>();
        healthText.text = "100/100";
        healthText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        healthText.fontSize = 18;
        healthText.alignment = TextAnchor.MiddleCenter;
        healthText.color = Color.white;
    }

    public void SetLocalHero(Hero hero)
    {
        localHero = hero;
        UpdateHealthBar();
    }

    private void OnAttackButtonPressed()
    {
        // Set attack flag in input controller
        if (inputController != null)
        {
            inputController.SetAttackPressed();
        }
    }

    private void Update()
    {
        // Update health bar
        if (localHero != null)
        {
            UpdateHealthBar();
        }
    }

    private void UpdateHealthBar()
    {
        if (localHero == null || healthBar == null) return;

        float healthPercentage = (float)localHero.CurrentHp / localHero.MaxHp;
        healthBar.value = healthPercentage * 100f;

        if (healthText != null)
        {
            healthText.text = $"{localHero.CurrentHp}/{localHero.MaxHp}";
        }

        // Change color based on health
        if (healthBar.fillRect != null)
        {
            Image fillImage = healthBar.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                if (healthPercentage > 0.6f)
                    fillImage.color = Color.green;
                else if (healthPercentage > 0.3f)
                    fillImage.color = Color.yellow;
                else
                    fillImage.color = Color.red;
            }
        }
    }
}