using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// จัดการ UI แสดงผลเงินและเพชร
/// ทำงานร่วมกับ CurrencyManager
/// </summary>
public class CurrencyUIManager : MonoBehaviour
{
    [Header("Currency UI Elements")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI gemsText;

    [Header("Currency Icons")]
    public Image goldIcon;
    public Image gemsIcon;

    [Header("Animation Settings")]
    public bool enableCounterAnimation = true;
    public float animationSpeed = 2f;

    [Header("Format Settings")]
    public bool useShortFormat = true;  // 1.2K แทน 1200
    public bool showCurrencySymbols = true;  // แสดง $ และ 💎

    private CurrencyManager currencyManager;
    private long currentDisplayGold = 0;
    private int currentDisplayGems = 0;

    // Animation
    private Coroutine goldAnimationCoroutine;
    private Coroutine gemsAnimationCoroutine;

    #region Unity Lifecycle
    void Start()
    {
        InitializeUI();
        SubscribeToEvents();

        // หา CurrencyManager
        StartCoroutine(FindCurrencyManagerDelayed());
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    #endregion

    #region Initialization
    private void InitializeUI()
    {
        // ตั้งค่าเริ่มต้น
        if (goldText != null)
            goldText.text = FormatCurrency(1000, CurrencyType.Gold);

        if (gemsText != null)
            gemsText.text = FormatCurrency(50, CurrencyType.Gems);
    }

    private IEnumerator FindCurrencyManagerDelayed()
    {
        // รอหา CurrencyManager ใน scene
        int attempts = 0;
        while (currencyManager == null && attempts < 10)
        {
            currencyManager = CurrencyManager.FindCurrencyManager();
            if (currencyManager != null)
            {
                RefreshCurrencyDisplay();
                break;
            }

            attempts++;
            yield return new WaitForSeconds(0.5f);
        }

        if (currencyManager == null)
        {
            Debug.LogWarning("[CurrencyUIManager] CurrencyManager not found in scene");
        }
        else
        {
            Debug.Log("[CurrencyUIManager] ✅ Connected to CurrencyManager");
        }
    }
    #endregion

    #region Event Handling
    private void SubscribeToEvents()
    {
        CurrencyManager.OnGoldChanged += HandleGoldChanged;
        CurrencyManager.OnGemsChanged += HandleGemsChanged;
    }

    private void UnsubscribeFromEvents()
    {
        CurrencyManager.OnGoldChanged -= HandleGoldChanged;
        CurrencyManager.OnGemsChanged -= HandleGemsChanged;
    }

    private void HandleGoldChanged(long oldAmount, long newAmount)
    {
        UpdateGoldDisplay(newAmount);
    }

    private void HandleGemsChanged(int oldAmount, int newAmount)
    {
        UpdateGemsDisplay(newAmount);
    }
    #endregion

    #region UI Update Methods
    private void UpdateGoldDisplay(long newAmount)
    {
        if (goldText == null) return;

        if (enableCounterAnimation)
        {
            // หยุด animation เก่า
            if (goldAnimationCoroutine != null)
            {
                StopCoroutine(goldAnimationCoroutine);
            }

            goldAnimationCoroutine = StartCoroutine(AnimateGoldCounter(currentDisplayGold, newAmount));
        }
        else
        {
            currentDisplayGold = newAmount;
            goldText.text = FormatCurrency(newAmount, CurrencyType.Gold);
        }
    }

    private void UpdateGemsDisplay(int newAmount)
    {
        if (gemsText == null) return;

        if (enableCounterAnimation)
        {
            // หยุด animation เก่า
            if (gemsAnimationCoroutine != null)
            {
                StopCoroutine(gemsAnimationCoroutine);
            }

            gemsAnimationCoroutine = StartCoroutine(AnimateGemsCounter(currentDisplayGems, newAmount));
        }
        else
        {
            currentDisplayGems = newAmount;
            gemsText.text = FormatCurrency(newAmount, CurrencyType.Gems);
        }
    }

    private IEnumerator AnimateGoldCounter(long startValue, long endValue)
    {
        float elapsed = 0f;
        float duration = Mathf.Abs(endValue - startValue) / animationSpeed / 1000f; // ปรับ duration ตามจำนวน
        duration = Mathf.Clamp(duration, 0.1f, 2f); // จำกัด duration

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            long currentValue = (long)Mathf.Lerp(startValue, endValue, progress);
            goldText.text = FormatCurrency(currentValue, CurrencyType.Gold);

            yield return null;
        }

        // ตั้งค่าสุดท้าย
        currentDisplayGold = endValue;
        goldText.text = FormatCurrency(endValue, CurrencyType.Gold);
        goldAnimationCoroutine = null;
    }

    private IEnumerator AnimateGemsCounter(int startValue, int endValue)
    {
        float elapsed = 0f;
        float duration = Mathf.Abs(endValue - startValue) / animationSpeed / 100f;
        duration = Mathf.Clamp(duration, 0.1f, 2f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            int currentValue = (int)Mathf.Lerp(startValue, endValue, progress);
            gemsText.text = FormatCurrency(currentValue, CurrencyType.Gems);

            yield return null;
        }

        // ตั้งค่าสุดท้าย
        currentDisplayGems = endValue;
        gemsText.text = FormatCurrency(endValue, CurrencyType.Gems);
        gemsAnimationCoroutine = null;
    }
    #endregion

    #region Currency Formatting
    private string FormatCurrency(long amount, CurrencyType currencyType)
    {
        string formattedAmount = useShortFormat ? FormatShort(amount) : amount.ToString("N0");

        if (showCurrencySymbols)
        {
            switch (currencyType)
            {
                case CurrencyType.Gold:
                    return $"💰 {formattedAmount}";
                case CurrencyType.Gems:
                    return $"💎 {formattedAmount}";
                default:
                    return formattedAmount;
            }
        }

        return formattedAmount;
    }

    private string FormatShort(long amount)
    {
        if (amount >= 1000000000) // 1B+
            return $"{amount / 1000000000f:F1}B";
        else if (amount >= 1000000) // 1M+
            return $"{amount / 1000000f:F1}M";
        else if (amount >= 1000) // 1K+
            return $"{amount / 1000f:F1}K";
        else
            return amount.ToString();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Force refresh display จาก CurrencyManager
    /// </summary>
    public void RefreshCurrencyDisplay()
    {
        if (currencyManager != null)
        {
            long currentGold = currencyManager.GetCurrentGold();
            int currentGems = currencyManager.GetCurrentGems();

            currentDisplayGold = currentGold;
            currentDisplayGems = currentGems;

            if (goldText != null)
                goldText.text = FormatCurrency(currentGold, CurrencyType.Gold);

            if (gemsText != null)
                gemsText.text = FormatCurrency(currentGems, CurrencyType.Gems);

            Debug.Log($"[CurrencyUIManager] Refreshed display - Gold: {currentGold}, Gems: {currentGems}");
        }
    }

    /// <summary>
    /// ตั้งค่า CurrencyManager manually
    /// </summary>
    public void SetCurrencyManager(CurrencyManager manager)
    {
        currencyManager = manager;
        RefreshCurrencyDisplay();
    }

    /// <summary>
    /// ตั้งค่าการแสดงผล
    /// </summary>
    public void SetDisplaySettings(bool shortFormat, bool showSymbols, bool enableAnimation)
    {
        useShortFormat = shortFormat;
        showCurrencySymbols = showSymbols;
        enableCounterAnimation = enableAnimation;

        RefreshCurrencyDisplay();
    }
    #endregion

    #region Debug Methods
    [ContextMenu("Test Refresh Display")]
    private void TestRefreshDisplay()
    {
        RefreshCurrencyDisplay();
    }

    [ContextMenu("Test Add 1000 Gold")]
    private void TestAddGold()
    {
        if (currencyManager != null)
        {
            currencyManager.AddGold(1000);
        }
    }

    [ContextMenu("Test Add 100 Gems")]
    private void TestAddGems()
    {
        if (currencyManager != null)
        {
            currencyManager.AddGems(100);
        }
    }
    #endregion
}