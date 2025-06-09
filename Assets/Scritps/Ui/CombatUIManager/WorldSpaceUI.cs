using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WorldSpaceUI : MonoBehaviour
{
    [Header("UI Elements (Canvas)")]
    public Slider healthBar;
    public Slider manaBar;
    public Canvas worldCanvas;

    [Header("3D TextMeshPro Elements")]
    public TextMeshPro playerNameText3D;
    public TextMeshPro levelText3D;

    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 2.5f, 0);
    public bool showLevelInName = false; // แสดง level ใน name หรือแยก

    private Hero targetHero;
    private Camera mainCamera;
    private bool isInitialized = false;

    public void Initialize(Hero hero)
    {
        targetHero = hero;
        mainCamera = Camera.main;

        // 🔧 ตั้งค่า Canvas สำหรับ HP/Mana bars (ใช้ระบบเดิม)
        SetupWorldSpaceCanvas();

        // 🔧 ตั้งค่า 3D TextMeshPro (ถ้ามีการลากมาใส่ Inspector)
        

        // ตั้งค่าข้อความเริ่มต้น
        UpdateTextContent();

        isInitialized = true;
        Debug.Log($"WorldSpaceUI initialized for {hero.CharacterName} (Level {hero.GetCurrentLevel()})");

        // อัปเดต UI ทันทีหลังจาก initialize
        UpdateBars();
    }

    private void Update()
    {
        if (!isInitialized || targetHero == null || mainCamera == null)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
            return;
        }

        // ตรวจสอบว่า Hero ยังมีอยู่
        if (!targetHero.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        // Follow target position
        transform.position = targetHero.transform.position + offset;

        // Face camera
        FaceCamera();

        // Update bars และ text
        UpdateBars();
        UpdateTextContent();
    }

    private void FaceCamera()
    {
        if (mainCamera == null) return;

        Vector3 lookDirection = mainCamera.transform.rotation * Vector3.forward;
        Vector3 upDirection = mainCamera.transform.rotation * Vector3.up;

        // หมุน Canvas (สำหรับ health/mana bars) - ระบบเดิม
        if (worldCanvas != null)
        {
            worldCanvas.transform.LookAt(transform.position + lookDirection, upDirection);
        }

        // หมุน 3D TextMeshPro elements เฉพาะ name กับ level
        if (playerNameText3D != null)
        {
            playerNameText3D.transform.LookAt(playerNameText3D.transform.position + lookDirection, upDirection);
        }

        if (levelText3D != null)
        {
            levelText3D.transform.LookAt(levelText3D.transform.position + lookDirection, upDirection);
        }
    }

    private void UpdateBars()
    {
        if (targetHero == null) return;

        // รอให้ network state พร้อม
        if (!targetHero.IsNetworkStateReady) return;

        // Update Health Bar (ระบบเดิมที่ทำงานได้ดี)
        if (healthBar != null && targetHero.NetworkedMaxHp > 0)
        {
            float healthPercent = (float)targetHero.NetworkedCurrentHp / targetHero.NetworkedMaxHp;
            healthBar.value = Mathf.Clamp01(healthPercent);
        }

        // Update Mana Bar (ระบบเดิมที่ทำงานได้ดี)
        if (manaBar != null && targetHero.NetworkedMaxMana > 0)
        {
            float manaPercent = (float)targetHero.NetworkedCurrentMana / targetHero.NetworkedMaxMana;
            manaBar.value = Mathf.Clamp01(manaPercent);
        }
    }

    private void UpdateTextContent()
    {
        if (targetHero == null) return;

        int currentLevel = targetHero.GetCurrentLevel();

        // Update Player Name (3D TextMeshPro)
        if (playerNameText3D != null)
        {
            if (showLevelInName)
            {
                playerNameText3D.text = $"[Lv.{currentLevel}] {targetHero.CharacterName}";
            }
            else
            {
                playerNameText3D.text = targetHero.CharacterName;
            }
        }

        // Update Level Text แยก (3D TextMeshPro)
        if (levelText3D != null && !showLevelInName)
        {
            float expProgress = targetHero.GetExpProgress();
            if (expProgress > 0 && !targetHero.IsMaxLevel())
            {
                levelText3D.text = $"Lv.{currentLevel}";
            }
            else
            {
                levelText3D.text = $"Lv.{currentLevel}";
            }
        }
    }

    // =========== Setup Methods ===========

    private void SetupWorldSpaceCanvas()
    {
        // หา Canvas ถ้าไม่ได้กำหนด (สำหรับ health/mana bars เดิม)
        if (worldCanvas == null)
        {
            worldCanvas = GetComponentInChildren<Canvas>();
        }

        if (worldCanvas != null)
        {
            // ตั้งค่า Canvas สำหรับ World Space (ระบบเดิม)
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.worldCamera = mainCamera;

            // ปรับขนาด Canvas
            RectTransform canvasRect = worldCanvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                canvasRect.sizeDelta = new Vector2(200, 50); // ขนาดสำหรับ bars
                canvasRect.localScale = Vector3.one * 0.02f; // Scale เล็กลง
            }

            Debug.Log("Canvas setup complete for HP/Mana bars");
        }
        else
        {
            Debug.LogWarning("No Canvas found for HP/Mana bars!");
        }
    }

    private void Setup3DTextMeshPro()
    {
        // ตั้งค่า PlayerName Text (3D) - ถ้ามีการลากมาใส่ Inspector
        

        // ตั้งค่า Level Text (3D) - ถ้ามีการลากมาใส่ Inspector
       
    }

    // =========== Public Methods สำหรับ Customization ===========

    /// <summary>
    /// เปลี่ยนสีของ health bar ตาม % (ระบบเดิม)
    /// </summary>
    public void UpdateHealthBarColor()
    {
        if (healthBar == null || targetHero == null) return;

        float healthPercent = (float)targetHero.NetworkedCurrentHp / targetHero.NetworkedMaxHp;
        Image fillImage = healthBar.fillRect?.GetComponent<Image>();

        if (fillImage != null)
        {
            if (healthPercent > 0.6f)
                fillImage.color = Color.green;
            else if (healthPercent > 0.3f)
                fillImage.color = Color.yellow;
            else
                fillImage.color = Color.red;
        }
    }

    /// <summary>
    /// เปลี่ยนสีของ mana bar (ระบบเดิม)
    /// </summary>
    public void UpdateManaBarColor()
    {
        if (manaBar == null) return;

        Image fillImage = manaBar.fillRect?.GetComponent<Image>();
        if (fillImage != null)
        {
            fillImage.color = Color.blue;
        }
    }

    /// <summary>
    /// แสดง/ซ่อน UI elements
    /// </summary>
    public void SetUIVisibility(bool showHealthBar, bool showManaBar, bool showName, bool showLevel)
    {
        // HP/Mana bars (ระบบเดิม)
        if (healthBar != null)
            healthBar.gameObject.SetActive(showHealthBar);

        if (manaBar != null)
            manaBar.gameObject.SetActive(showManaBar);

        // 3D Text elements
        if (playerNameText3D != null)
            playerNameText3D.gameObject.SetActive(showName);

        if (levelText3D != null)
            levelText3D.gameObject.SetActive(showLevel);
    }

    /// <summary>
    /// ปรับขนาด 3D text ตามระยะห่างจากกล้อง
    /// </summary>
    public void UpdateTextScaleByDistance()
    {
        if (mainCamera == null || targetHero == null) return;

        float distance = Vector3.Distance(mainCamera.transform.position, transform.position);
        float scale = Mathf.Clamp(distance * 0.1f, 0.5f, 2f);

        if (playerNameText3D != null)
            playerNameText3D.transform.localScale = Vector3.one * scale;

        if (levelText3D != null)
            levelText3D.transform.localScale = Vector3.one * scale;
    }

    /// <summary>
    /// เปลี่ยนสี text ตามสถานะ
    /// </summary>
    public void UpdateTextColors()
    {
        if (playerNameText3D != null)
        {
            // เปลี่ยนสีชื่อตาม HP
            float healthPercent = (float)targetHero.NetworkedCurrentHp / targetHero.NetworkedMaxHp;
            if (healthPercent <= 0.2f)
                playerNameText3D.color = Color.red;
            else if (healthPercent <= 0.5f)
                playerNameText3D.color = Color.yellow;
            else
                playerNameText3D.color = Color.white;
        }
    }

    // =========== Debug Methods ===========

    [ContextMenu("Debug UI Status")]
    public void DebugUIStatus()
    {
        Debug.Log("=== WorldSpace UI Debug (Manual 3D Text Setup) ===");
        Debug.Log($"Initialized: {isInitialized}");
        Debug.Log($"Target Hero: {(targetHero != null ? targetHero.CharacterName : "NULL")}");
        Debug.Log($"Main Camera: {(mainCamera != null ? mainCamera.name : "NULL")}");

        // Debug Canvas (HP/Mana bars)
        if (worldCanvas != null)
        {
            Debug.Log($"Canvas Mode: {worldCanvas.renderMode}");
            Debug.Log($"Canvas Active: {worldCanvas.gameObject.activeInHierarchy}");
            Debug.Log($"Health Bar: {(healthBar != null ? "OK" : "NULL")}");
            Debug.Log($"Mana Bar: {(manaBar != null ? "OK" : "NULL")}");
        }

        // Debug 3D TextMeshPro
        if (playerNameText3D != null)
        {
            Debug.Log($"PlayerName 3D Text: '{playerNameText3D.text}'");
            Debug.Log($"PlayerName Active: {playerNameText3D.gameObject.activeInHierarchy}");
            Debug.Log($"PlayerName Position: {playerNameText3D.transform.position}");
        }
        else
        {
            Debug.LogWarning("PlayerName 3D Text is NULL! Please drag TextMeshPro (3D) to Inspector.");
        }

        if (levelText3D != null)
        {
            Debug.Log($"Level 3D Text: '{levelText3D.text}'");
            Debug.Log($"Level Active: {levelText3D.gameObject.activeInHierarchy}");
        }
        else
        {
            Debug.Log("Level 3D Text is NULL (check if showLevelInName is enabled or drag TextMeshPro to Inspector)");
        }
    }

    [ContextMenu("Force Text Update")]
    public void ForceTextUpdate()
    {
        if (playerNameText3D != null)
        {
            playerNameText3D.text = "TEST NAME 3D";
            Debug.Log("Forced PlayerName 3D text update");
        }

        if (levelText3D != null)
        {
            levelText3D.text = "TEST LEVEL 3D";
            Debug.Log("Forced Level 3D text update");
        }

        UpdateTextContent();
    }

    [ContextMenu("Reset 3D Text Settings")]
    public void Reset3DTextSettings()
    {
        // ตั้งค่า 3D TextMeshPro ใหม่
        Setup3DTextMeshPro();
        UpdateTextContent();

        Debug.Log("Reset 3D TextMeshPro settings");
    }

    [ContextMenu("Test All UI")]
    public void TestAllUI()
    {
        if (targetHero != null)
        {
            Debug.Log($"=== UI Test ===");
            Debug.Log($"HP: {targetHero.NetworkedCurrentHp}/{targetHero.NetworkedMaxHp}");
            Debug.Log($"MP: {targetHero.NetworkedCurrentMana}/{targetHero.NetworkedMaxMana}");
            Debug.Log($"Level: {targetHero.GetCurrentLevel()}");
            Debug.Log($"Name: {targetHero.CharacterName}");

            // Test bars
            UpdateHealthBarColor();
            UpdateManaBarColor();

            // Test text
            UpdateTextColors();
        }
    }
}