using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LoadingPanelManager : MonoBehaviour
{
    [Header("Loading Panel UI")]
    public GameObject loadingPanel;
    public Slider progressBar;
    public TextMeshProUGUI loadingText;
    public TextMeshProUGUI detailText;
    public TextMeshProUGUI tipsText;

    [Header("Fade Manager Reference")]
    public FadeManager fadeManager;

    [Header("Fake Loading Settings")]
    public float totalLoadingTime = 5f;

    [Header("Loading Stages")]
    public string[] loadingMessages = {
        "Initializing Firebase...",
        "Loading Player Data...",
        "Loading Character Stats...",
        "Loading Inventory & Equipment...",
        "Setting up UI...",
        "Ready to Play!"
    };

    [Header("Loading Tips")]
    public string[] loadingTips = {
        "Tip: Check your inventory regularly for better equipment!",
        "Tip: Different weapons have unique combat styles.",
        "Tip: Your character stats affect combat performance.",
        "Tip: Equipment can be upgraded for better stats.",
        "Tip: Save your progress frequently!",
        "Tip: Explore different areas for rare items.",
        "Tip: Character level affects available skills."
    };

    [Header("Background Images")]
    public Image backgroundImage;               // ✅ UI Image
    public Sprite[] backgroundSprites;          // ✅ รูปพื้นหลังหลายรูป
    public float backgroundChangeInterval = 3f; // ✅ เปลี่ยนทุก 3 วิ
    private Coroutine bgCoroutine;

    private float currentProgress = 0f;
    private int currentStage = 0;
    private float tipChangeInterval = 2f;
    private Coroutine loadingCoroutine;
    private Coroutine tipCoroutine;

    private void Start()
    {
        ShowLoadingPanel();

        loadingCoroutine = StartCoroutine(FakeLoadingProgress());
        tipCoroutine = StartCoroutine(UpdateTipDisplay());

        // ✅ เริ่มเปลี่ยน BG แบบสุ่มเรื่อยๆ
        if (backgroundSprites.Length > 0 && backgroundImage != null)
        {
            bgCoroutine = StartCoroutine(ChangeBackgroundLoop());
        }
    }

    private void ShowLoadingPanel()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }
        UpdateLoadingUI(0f, loadingMessages[0], "Please wait...");
    }

    private IEnumerator UpdateTipDisplay()
    {
        while (currentProgress < 1f)
        {
            if (loadingTips.Length > 0)
            {
                string currentTip = loadingTips[Random.Range(0, loadingTips.Length)];
                if (tipsText != null)
                {
                    tipsText.text = currentTip;
                }
            }
            yield return new WaitForSeconds(tipChangeInterval);
        }
    }

    // ✅ เปลี่ยนรูปพื้นหลังเรื่อยๆพร้อม Fade
    private IEnumerator ChangeBackgroundLoop()
    {
        while (currentProgress < 1f)
        {
            Sprite nextSprite = backgroundSprites[Random.Range(0, backgroundSprites.Length)];
            yield return StartCoroutine(FadeBackgroundTo(nextSprite));
            yield return new WaitForSeconds(backgroundChangeInterval);
        }
    }

    // ✅ Fade ภาพนุ่มๆ
    private IEnumerator FadeBackgroundTo(Sprite newSprite)
    {
        if (backgroundImage == null) yield break;

        float duration = 0.6f;
        float t = 0f;

        Color startColor = backgroundImage.color;
        Color fadeOutColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        // Fade Out
        while (t < duration)
        {
            t += Time.deltaTime;
            backgroundImage.color = Color.Lerp(startColor, fadeOutColor, t / duration);
            yield return null;
        }

        backgroundImage.sprite = newSprite;

        // Fade In
        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            backgroundImage.color = Color.Lerp(fadeOutColor, startColor, t / duration);
            yield return null;
        }
    }

    // ✅ Loading ปลอมแบบค่อยๆขึ้น
    private IEnumerator FakeLoadingProgress()
    {
        float elapsed = 0f;
        int totalStages = loadingMessages.Length;

        while (elapsed < totalLoadingTime)
        {
            elapsed += Time.deltaTime;

            currentProgress = Mathf.Clamp01(elapsed / totalLoadingTime);

            currentStage = Mathf.FloorToInt(currentProgress * (totalStages - 1));
            currentStage = Mathf.Clamp(currentStage, 0, totalStages - 1);

            string message = loadingMessages[currentStage];
            string detail = $"{(currentProgress * 100f):F0}%";

            UpdateLoadingUI(currentProgress, message, detail);

            yield return null;
        }

        currentProgress = 1f;
        currentStage = totalStages - 1;
        UpdateLoadingUI(1f, loadingMessages[currentStage], "100%");

        yield return new WaitForSeconds(0.5f);

        // ✅ Fade Out ก่อนเข้าเกม
        if (fadeManager != null)
        {
            yield return fadeManager.StartCoroutine(fadeManager.FadeToBlack());
        }

        HideLoadingPanel();

        // ✅ Fade In
        if (fadeManager != null)
        {
            yield return fadeManager.StartCoroutine(fadeManager.FadeFromBlack());
        }

        Debug.Log("[LoadingPanel] All complete! Ready to play.");
    }

    private void UpdateLoadingUI(float progress, string message, string detail)
    {
        if (progressBar != null)
        {
            progressBar.value = progress;
        }

        if (loadingText != null)
        {
            loadingText.text = message;
        }

        if (detailText != null)
        {
            detailText.text = $"Loading {detail}";
        }
    }

    private void HideLoadingPanel()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        Debug.Log("[LoadingPanel] Loading complete!");
    }

    // ✅ ใช้สำหรับบังคับปิด Loading
    public void ForceHideLoadingPanel()
    {
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
            loadingCoroutine = null;
        }

        if (tipCoroutine != null)
        {
            StopCoroutine(tipCoroutine);
            tipCoroutine = null;
        }

        if (bgCoroutine != null)
        {
            StopCoroutine(bgCoroutine);
            bgCoroutine = null;
        }

        HideLoadingPanel();

        if (fadeManager != null)
        {
            fadeManager.SetTransparentInstant();
        }
    }

    public bool IsLoadingComplete()
    {
        return currentProgress >= 1f;
    }

    public float GetCurrentProgress()
    {
        return currentProgress;
    }

    public void SetLoadingTime(float seconds)
    {
        totalLoadingTime = Mathf.Max(1f, seconds);
    }
}
