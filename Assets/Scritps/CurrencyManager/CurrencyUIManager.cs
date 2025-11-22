using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// จัดการ UI แสดงผลเงินและเพชร
/// ทำงานร่วมกับ CurrencyManager
/// </summary>
public class CurrencyUIManager : MonoBehaviour
{
    [Header("Currency UI Elements")]
    public List<TextMeshProUGUI> goldTextList = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> gemsTextList = new List<TextMeshProUGUI>();

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
        foreach (var goldText in goldTextList)
        {
            if (goldText != null)
                goldText.text = FormatCurrency(1000, CurrencyType.Gold);
        }

        foreach (var gemsText in gemsTextList)
        {
            if (gemsText != null)
                gemsText.text = FormatCurrency(50, CurrencyType.Gems);
        }
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
        if (goldTextList.Count == 0) return;

        if (enableCounterAnimation)
        {
            if (goldAnimationCoroutine != null)
            {
                StopCoroutine(goldAnimationCoroutine);
            }
            goldAnimationCoroutine = StartCoroutine(AnimateGoldCounter(currentDisplayGold, newAmount));
        }
        else
        {
            currentDisplayGold = newAmount;
            string formattedText = FormatCurrency(newAmount, CurrencyType.Gold);
            foreach (var goldText in goldTextList)
            {
                if (goldText != null)
                    goldText.text = formattedText;
            }
        }
    }


    private void UpdateGemsDisplay(int newAmount)
    {
        if (gemsTextList.Count == 0) return;

        if (enableCounterAnimation)
        {
            if (gemsAnimationCoroutine != null)
            {
                StopCoroutine(gemsAnimationCoroutine);
            }
            gemsAnimationCoroutine = StartCoroutine(AnimateGemsCounter(currentDisplayGems, newAmount));
        }
        else
        {
            currentDisplayGems = newAmount;
            string formattedText = FormatCurrency(newAmount, CurrencyType.Gems);
            foreach (var gemsText in gemsTextList)
            {
                if (gemsText != null)
                    gemsText.text = formattedText;
            }
        }
    }

    private IEnumerator AnimateGoldCounter(long startValue, long endValue)
    {
        float elapsed = 0f;
        float duration = Mathf.Abs(endValue - startValue) / animationSpeed / 1000f;
        duration = Mathf.Clamp(duration, 0.1f, 2f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            long currentValue = (long)Mathf.Lerp(startValue, endValue, progress);
            string formattedText = FormatCurrency(currentValue, CurrencyType.Gold);

            foreach (var goldText in goldTextList)
            {
                if (goldText != null)
                    goldText.text = formattedText;
            }

            yield return null;
        }

        currentDisplayGold = endValue;
        string finalText = FormatCurrency(endValue, CurrencyType.Gold);
        foreach (var goldText in goldTextList)
        {
            if (goldText != null)
                goldText.text = finalText;
        }
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
            string formattedText = FormatCurrency(currentValue, CurrencyType.Gems);

            foreach (var gemsText in gemsTextList)
            {
                if (gemsText != null)
                    gemsText.text = formattedText;
            }

            yield return null;
        }

        currentDisplayGems = endValue;
        string finalText = FormatCurrency(endValue, CurrencyType.Gems);
        foreach (var gemsText in gemsTextList)
        {
            if (gemsText != null)
                gemsText.text = finalText;
        }
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
                    return $"{formattedAmount}";
                case CurrencyType.Gems:
                    return $"{formattedAmount}";
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

            string goldFormatted = FormatCurrency(currentGold, CurrencyType.Gold);
            string gemsFormatted = FormatCurrency(currentGems, CurrencyType.Gems);

            foreach (var goldText in goldTextList)
            {
                if (goldText != null)
                    goldText.text = goldFormatted;
            }

            foreach (var gemsText in gemsTextList)
            {
                if (gemsText != null)
                    gemsText.text = gemsFormatted;
            }

            Debug.Log($"[CurrencyUIManager] Refreshed display - Gold: {currentGold}, Gems: {currentGems}");
        }
    }
    /// <summary>
    /// เพิ่ม Gold Text เข้า List
    /// </summary>
    public void AddGoldText(TextMeshProUGUI goldText)
    {
        if (goldText != null && !goldTextList.Contains(goldText))
        {
            goldTextList.Add(goldText);
            goldText.text = FormatCurrency(currentDisplayGold, CurrencyType.Gold);
        }
    }

    /// <summary>
    /// เพิ่ม Gems Text เข้า List
    /// </summary>
    public void AddGemsText(TextMeshProUGUI gemsText)
    {
        if (gemsText != null && !gemsTextList.Contains(gemsText))
        {
            gemsTextList.Add(gemsText);
            gemsText.text = FormatCurrency(currentDisplayGems, CurrencyType.Gems);
        }
    }

    /// <summary>
    /// ลบ Text ออกจาก List
    /// </summary>
    public void RemoveGoldText(TextMeshProUGUI goldText)
    {
        goldTextList.Remove(goldText);
    }

    public void RemoveGemsText(TextMeshProUGUI gemsText)
    {
        gemsTextList.Remove(gemsText);
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