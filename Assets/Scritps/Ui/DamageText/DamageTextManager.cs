using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance { get; private set; }

    [Header("Prefab Setup")]
    public DamageText damageTextPrefab;

    [Header("Pool Settings")]
    public int poolSize = 50;
    public int maxActiveTexts = 30; // จำกัดจำนวนที่แสดงพร้อมกัน
    public float cullingDistance = 25f; // ระยะที่จะไม่แสดง damage text

    [Header("Performance Settings")]
    public bool enablePooling = true;
    public bool enableCulling = true;
    public float cleanupInterval = 2f; // ทำความสะอาดทุก 2 วินาที

    private Queue<DamageText> damageTextPool = new Queue<DamageText>();
    private List<DamageText> activeDamageTexts = new List<DamageText>();
    private Camera mainCamera;
    private int textDisplayedThisFrame = 0;
    private int maxTextsPerFrame = 3; // จำกัดการแสดงต่อ frame

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;
        StartCoroutine(PerformanceCleanup());

        // Subscribe to damage events
        CombatManager.OnDamageTaken += HandleDamageTaken;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            // Unsubscribe events
            CombatManager.OnDamageTaken -= HandleDamageTaken;
        }
    }

    private void LateUpdate()
    {
        // Reset frame counter
        textDisplayedThisFrame = 0;
    }

    #region Pool Management

    private void InitializePool()
    {
        if (!enablePooling || damageTextPrefab == null) return;

        // Create pool
        for (int i = 0; i < poolSize; i++)
        {
            CreateNewDamageText();
        }

        Debug.Log($"DamageTextManager: Initialized pool with {poolSize} objects");
    }

    private DamageText CreateNewDamageText()
    {
        if (damageTextPrefab == null)
        {
            Debug.LogError("DamageTextManager: damageTextPrefab is null!");
            return null;
        }

        DamageText newText = Instantiate(damageTextPrefab, transform);
        newText.gameObject.SetActive(false);
        damageTextPool.Enqueue(newText);
        return newText;
    }

    private DamageText GetDamageText()
    {
        if (!enablePooling)
        {
            // Create new instance without pooling
            return Instantiate(damageTextPrefab, transform);
        }

        // Get from pool or create new
        DamageText damageText = null;

        if (damageTextPool.Count > 0)
        {
            damageText = damageTextPool.Dequeue();
        }
        else
        {
            damageText = CreateNewDamageText();
            if (damageText != null)
            {
                damageTextPool.Dequeue(); // Remove it since we just added it
            }
        }

        return damageText;
    }

    public void ReturnDamageText(DamageText damageText)
    {
        if (damageText == null) return;

        // Remove from active list
        activeDamageTexts.Remove(damageText);

        if (enablePooling)
        {
            // Return to pool
            damageText.gameObject.SetActive(false);
            damageTextPool.Enqueue(damageText);
        }
        else
        {
            // Destroy if not using pooling
            Destroy(damageText.gameObject);
        }
    }

    #endregion

    #region Damage Display

    private void HandleDamageTaken(Character target, int damage, DamageType damageType, bool isCritical)
    {
        // แสดง damage text เฉพาะใน local client เท่านั้น
        ShowDamageText(target.transform.position, damage, damageType, isCritical, false);
    }

    public void ShowDamageText(Vector3 position, int damage, DamageType damageType, bool isCritical = false, bool isHeal = false)
    {
        // Performance checks
        if (!ShouldShowDamageText(position)) return;

        DamageText damageText = GetDamageText();
        if (damageText == null) return;

        // Add to active list
        activeDamageTexts.Add(damageText);

        // Initialize and show
        Vector3 adjustedPosition = position + Vector3.up * 1.5f; // แสดงเหนือหัว
        damageText.Initialize(adjustedPosition, damage, damageType, isCritical, isHeal);

        textDisplayedThisFrame++;

        // Debug log for critical hits
        if (isCritical)
        {
            Debug.Log($"💥 Critical damage text shown: {damage} at {position}");
        }
    }

    public void ShowHealText(Vector3 position, int healAmount)
    {
        ShowDamageText(position, healAmount, DamageType.Normal, false, true);
    }

    #endregion

    #region Performance Management

    private bool ShouldShowDamageText(Vector3 position)
    {
        // Frame limit check
        if (textDisplayedThisFrame >= maxTextsPerFrame)
        {
            return false;
        }

        // Active text limit check
        if (activeDamageTexts.Count >= maxActiveTexts)
        {
            // Remove oldest text to make room
            if (activeDamageTexts.Count > 0)
            {
                activeDamageTexts[0].StopAnimation();
            }
        }

        // Distance culling
        if (enableCulling && mainCamera != null)
        {
            float distance = Vector3.Distance(position, mainCamera.transform.position);
            if (distance > cullingDistance)
            {
                return false;
            }
        }

        return true;
    }

    private IEnumerator PerformanceCleanup()
    {
        while (true)
        {
            yield return new WaitForSeconds(cleanupInterval);
            CleanupActiveDamageTexts();
        }
    }

    private void CleanupActiveDamageTexts()
    {
        // Remove null references
        activeDamageTexts.RemoveAll(text => text == null || !text.gameObject.activeInHierarchy);

        // Force cleanup if too many active
        while (activeDamageTexts.Count > maxActiveTexts)
        {
            if (activeDamageTexts.Count > 0)
            {
                activeDamageTexts[0].StopAnimation();
            }
            else
            {
                break;
            }
        }

        // Log performance info
        if (Application.isEditor)
        {
            Debug.Log($"DamageTextManager: Active texts: {activeDamageTexts.Count}, Pool size: {damageTextPool.Count}");
        }
    }

    #endregion

    #region Public Methods for Heroes and Enemies

    /// <summary>
    /// แสดง damage text สำหรับ Hero
    /// </summary>
    public static void ShowHeroDamage(Vector3 position, int damage, DamageType damageType, bool isCritical = false)
    {
        Instance?.ShowDamageText(position, damage, damageType, isCritical, false);
    }

    /// <summary>
    /// แสดง damage text สำหรับ Enemy
    /// </summary>
    public static void ShowEnemyDamage(Vector3 position, int damage, DamageType damageType, bool isCritical = false)
    {
        Instance?.ShowDamageText(position, damage, damageType, isCritical, false);
    }

    /// <summary>
    /// แสดง healing text
    /// </summary>
    public static void ShowHealing(Vector3 position, int healAmount)
    {
        Instance?.ShowHealText(position, healAmount);
    }
    public static void ShowMissText(Vector3 position)
    {
        if (Instance != null)
        {
            Instance.CreateMissText(position);
        }
    }



    private void CreateMissText(Vector3 worldPosition)
    {
        // Performance checks เหมือนกับ ShowDamageText
        if (!ShouldShowDamageText(worldPosition)) return;

        // ใช้ GetDamageText() แทน GetPooledDamageText()
        DamageText damageText = GetDamageText();
        if (damageText == null) return;

        // Add to active list
        activeDamageTexts.Add(damageText);

        // ใช้ world position โดยตรง แทนที่จะแปลงเป็น screen space
        Vector3 adjustedPosition = worldPosition + Vector3.up * 1.5f; // แสดงเหนือหัว
        damageText.ShowMiss(adjustedPosition);

        damageText.gameObject.SetActive(true);
        textDisplayedThisFrame++;

        Debug.Log($"💨 Miss text shown at {worldPosition}");
    }
    /// <summary>
    /// แสดง status effect damage
    /// </summary>
    public static void ShowStatusDamage(Vector3 position, int damage, StatusEffectType effectType)
    {
        DamageType damageType = effectType switch
        {
            StatusEffectType.Poison => DamageType.Poison,
            StatusEffectType.Burn => DamageType.Burn,
            StatusEffectType.Bleed => DamageType.Bleed,
            _ => DamageType.Magic
        };

        Instance?.ShowDamageText(position, damage, damageType, false, false);
    }

    #endregion

    #region Settings

    public void SetPoolSize(int newSize)
    {
        poolSize = Mathf.Clamp(newSize, 10, 200);
    }

    public void SetMaxActiveTexts(int newMax)
    {
        maxActiveTexts = Mathf.Clamp(newMax, 5, 100);
    }

    public void SetCullingDistance(float newDistance)
    {
        cullingDistance = Mathf.Clamp(newDistance, 5f, 100f);
    }

    public void EnablePooling(bool enable)
    {
        enablePooling = enable;
    }

    public void EnableCulling(bool enable)
    {
        enableCulling = enable;
    }

    #endregion

    #region Debug Methods

    [ContextMenu("Test Damage Text")]
    public void TestDamageText()
    {
        if (mainCamera != null)
        {
            Vector3 testPos = mainCamera.transform.position + mainCamera.transform.forward * 5f;
            ShowDamageText(testPos, 999, DamageType.Critical, true, false);
        }
    }

    [ContextMenu("Test Heal Text")]
    public void TestHealText()
    {
        if (mainCamera != null)
        {
            Vector3 testPos = mainCamera.transform.position + mainCamera.transform.forward * 5f;
            ShowHealText(testPos, 50);
        }
    }

    [ContextMenu("Clear All Damage Texts")]
    public void ClearAllDamageTexts()
    {
        foreach (var text in activeDamageTexts.ToArray())
        {
            if (text != null)
            {
                text.StopAnimation();
            }
        }
    }

    [ContextMenu("Log Pool Status")]
    public void LogPoolStatus()
    {
        Debug.Log($"=== DamageTextManager Status ===");
        Debug.Log($"Pool Size: {damageTextPool.Count}");
        Debug.Log($"Active Texts: {activeDamageTexts.Count}");
        Debug.Log($"Max Active Texts: {maxActiveTexts}");
        Debug.Log($"Pooling Enabled: {enablePooling}");
        Debug.Log($"Culling Enabled: {enableCulling}");
        Debug.Log($"Culling Distance: {cullingDistance}");
    }

    #endregion
}