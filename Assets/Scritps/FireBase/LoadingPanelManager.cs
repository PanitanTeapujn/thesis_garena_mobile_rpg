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

    [Header("Fake Loading Settings")]
    public float totalLoadingTime = 5f; // ⏱️ 5 วินาที

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

    private float currentProgress = 0f;
    private int currentStage = 0;
    private float tipChangeInterval = 2f; // เปลี่ยน tip ทุก 2 วินาที

    private void Start()
    {
        ShowLoadingPanel();
        StartCoroutine(FakeLoadingProgress());
        StartCoroutine(UpdateTipDisplay());
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

    // ✅ Loading แบบหลอกๆ 5 วินาที
    private IEnumerator FakeLoadingProgress()
    {
        float elapsed = 0f;
        int totalStages = loadingMessages.Length;

        while (elapsed < totalLoadingTime)
        {
            elapsed += Time.deltaTime;

            // คำนวณ progress (0 → 1)
            currentProgress = Mathf.Clamp01(elapsed / totalLoadingTime);

            // คำนวณ stage ปัจจุบัน
            currentStage = Mathf.FloorToInt(currentProgress * (totalStages - 1));
            currentStage = Mathf.Clamp(currentStage, 0, totalStages - 1);

            // อัพเดท UI
            string message = loadingMessages[currentStage];
            string detail = $"{(currentProgress * 100f):F0}%";

            UpdateLoadingUI(currentProgress, message, detail);

            yield return null; // รอ 1 frame
        }

        // เสร็จสิ้น - แสดงข้อความสุดท้าย
        currentProgress = 1f;
        currentStage = totalStages - 1;
        UpdateLoadingUI(1f, loadingMessages[currentStage], "100%");

        yield return new WaitForSeconds(0.5f);

        // ซ่อน loading panel
        HideLoadingPanel();
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

        Debug.Log("[LoadingPanel] Loading complete! Game ready to play.");
    }

    // Public methods
    public void ForceHideLoadingPanel()
    {
        StopAllCoroutines();
        HideLoadingPanel();
    }

    public bool IsLoadingComplete()
    {
        return currentProgress >= 1f;
    }

    public float GetCurrentProgress()
    {
        return currentProgress;
    }

    // ✅ เพิ่ม method สำหรับเปลี่ยนเวลา loading จากภายนอก
    public void SetLoadingTime(float seconds)
    {
        totalLoadingTime = Mathf.Max(1f, seconds);
    }
}