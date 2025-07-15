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

    private string currentTip = "";
    private float tipChangeTimer = 0f;
    private float tipChangeInterval = 5f; // เปลี่ยน tip ทุก 2 วินาที
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

    private void Start()
    {
        ShowLoadingPanel();
        StartCoroutine(MonitorLoadingProgress());
        StartCoroutine(UpdateTipDisplay()); // เพิ่มบรรทัดนี้

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
            // สุ่มเลือก tip ใหม่
            if (loadingTips.Length > 0)
            {
                currentTip = loadingTips[Random.Range(0, loadingTips.Length)];

                // แสดง tip ใน tipsText
                if (tipsText != null)
                {
                    tipsText.text = currentTip;
                }
            }

            yield return new WaitForSeconds(tipChangeInterval);
        }
    }

    private IEnumerator MonitorLoadingProgress()
    {
        // Stage 1: รอ PersistentPlayerData Instance
        yield return StartCoroutine(WaitForPersistentData());
        UpdateProgress(0.2f, 1);

        // Stage 2: รอ Firebase Data Loading
        yield return StartCoroutine(WaitForFirebaseData());
        UpdateProgress(0.4f, 2);

        // Stage 3: รอ Character Spawn และ Stats
        yield return StartCoroutine(WaitForCharacterReady());
        UpdateProgress(0.6f, 3);

        // Stage 4: รอ Inventory และ Equipment
        yield return StartCoroutine(WaitForInventoryAndEquipment());
        UpdateProgress(0.8f, 4);

        // Stage 5: รอ UI Setup
        yield return StartCoroutine(WaitForUISetup());
        UpdateProgress(1f, 5);

        // เสร็จสิ้น
        yield return new WaitForSeconds(0.5f);
        HideLoadingPanel();
    }

    private IEnumerator WaitForPersistentData()
    {
        float timeout = 10f;
        float elapsed = 0f;

        while (PersistentPlayerData.Instance == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            float stageProgress = Mathf.Clamp01(elapsed / timeout);
            UpdateLoadingUI(stageProgress * 0.2f, loadingMessages[0],
                $"Initializing... {elapsed:F1}s");
            yield return null;
        }

        if (PersistentPlayerData.Instance == null)
        {
            ShowError("Failed to initialize PersistentPlayerData");
            yield break;
        }
    }

    private IEnumerator WaitForFirebaseData()
    {
        float timeout = 30f;
        float elapsed = 0f;

        // เริ่มโหลดข้อมูล
        if (!PersistentPlayerData.Instance.isDataLoaded)
        {
            PersistentPlayerData.Instance.LoadPlayerDataAsync();
        }

        while (!PersistentPlayerData.Instance.isDataLoaded && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            float stageProgress = Mathf.Clamp01(elapsed / timeout);
            UpdateLoadingUI(0.2f + (stageProgress * 0.2f), loadingMessages[1],
                $"Loading from Firebase... {elapsed:F1}s");
            yield return null;
        }

        if (!PersistentPlayerData.Instance.isDataLoaded)
        {
            ShowError("Failed to load player data from Firebase");
            yield break;
        }

        // ตรวจสอบว่ามีข้อมูลที่ถูกต้อง
        if (!PersistentPlayerData.Instance.HasValidData())
        {
            UpdateLoadingUI(0.4f, loadingMessages[1], "Creating new player data...");
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator WaitForCharacterReady()
    {
        float timeout = 15f;
        float elapsed = 0f;

        Character localCharacter = null;

        while (localCharacter == null && elapsed < timeout)
        {
            // หา Character ที่มี InputAuthority
            Character[] characters = FindObjectsOfType<Character>();
            foreach (Character character in characters)
            {
                if (character.HasInputAuthority && character.IsSpawned)
                {
                    localCharacter = character;
                    break;
                }
            }

            elapsed += Time.deltaTime;
            float stageProgress = Mathf.Clamp01(elapsed / timeout);
            UpdateLoadingUI(0.4f + (stageProgress * 0.2f), loadingMessages[2],
                $"Spawning character... {elapsed:F1}s");
            yield return null;
        }

        if (localCharacter == null)
        {
            ShowError("Character not found or failed to spawn");
            yield break;
        }

        // รอให้ Character Stats โหลดเสร็จ
        elapsed = 0f;
        while (!localCharacter.IsStatsLoadingComplete() && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            float stageProgress = Mathf.Clamp01(elapsed / timeout);
            UpdateLoadingUI(0.4f + (stageProgress * 0.2f), loadingMessages[2],
                $"Loading character stats... {elapsed:F1}s");
            yield return null;
        }

        if (!localCharacter.IsStatsLoadingComplete())
        {
            ShowError("Character stats failed to load");
        }
    }

    private IEnumerator WaitForInventoryAndEquipment()
    {
        float timeout = 10f;
        float elapsed = 0f;

        // หา Character
        Character localCharacter = GetLocalCharacter();
        if (localCharacter == null)
        {
            ShowError("Character not found for inventory loading");
            yield break;
        }

        // รอให้ Inventory พร้อม
        while (localCharacter.GetInventory() == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            float stageProgress = Mathf.Clamp01(elapsed / timeout);
            UpdateLoadingUI(0.6f + (stageProgress * 0.2f), loadingMessages[3],
                $"Setting up inventory... {elapsed:F1}s");
            yield return null;
        }

        if (localCharacter.GetInventory() == null)
        {
            ShowError("Inventory system failed to initialize");
            yield break;
        }

        // รอให้ Equipment โหลดเสร็จ (ถ้ามีข้อมูลใน Firebase)
        if (PersistentPlayerData.Instance.ShouldLoadFromFirebase())
        {
            elapsed = 0f;
            while (elapsed < 3f) // รอ 3 วินาทีให้ equipment โหลด
            {
                elapsed += Time.deltaTime;
                float stageProgress = Mathf.Clamp01(elapsed / 3f);
                UpdateLoadingUI(0.6f + (stageProgress * 0.2f), loadingMessages[3],
                    $"Loading equipment... {elapsed:F1}s");
                yield return null;
            }
        }
    }

    private IEnumerator WaitForUISetup()
    {
        float timeout = 5f;
        float elapsed = 0f;

        // รอให้ CombatUIManager พร้อม
        CombatUIManager combatUI = null;

        while (combatUI == null && elapsed < timeout)
        {
            combatUI = FindObjectOfType<CombatUIManager>();
            elapsed += Time.deltaTime;
            float stageProgress = Mathf.Clamp01(elapsed / timeout);
            UpdateLoadingUI(0.8f + (stageProgress * 0.2f), loadingMessages[4],
                $"Setting up UI... {elapsed:F1}s");
            yield return null;
        }

        if (combatUI == null)
        {
            ShowError("Combat UI failed to initialize");
            yield break;
        }

        // Force setup inventory grid
        combatUI.ForceSetupInventoryGrid();

        // รออีกหน่อยให้ UI ตั้งค่าเสร็จ
        yield return new WaitForSeconds(1f);
    }

    private Character GetLocalCharacter()
    {
        Character[] characters = FindObjectsOfType<Character>();
        foreach (Character character in characters)
        {
            if (character.HasInputAuthority && character.IsSpawned)
            {
                return character;
            }
        }
        return null;
    }

    private void UpdateProgress(float targetProgress, int stage)
    {
        currentProgress = targetProgress;
        currentStage = Mathf.Min(stage, loadingMessages.Length - 1);

        string message = loadingMessages[currentStage];
        string detail = GetDetailMessage();

        UpdateLoadingUI(currentProgress, message, detail);
    }

    private string GetDetailMessage()
    {
        return $"{(currentProgress * 100f):F0}%";
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
            // แสดงเปอร์เซ็นต์
            detailText.text = $"Loading{(progress * 100f):F0}%";
        }
    }
    private void ShowError(string errorMessage)
    {
        Debug.LogError($"[LoadingPanel] {errorMessage}");

        if (loadingText != null)
        {
            loadingText.text = "Loading Error";
            loadingText.color = Color.red;
        }

        if (detailText != null)
        {
            detailText.text = errorMessage;
            detailText.color = Color.red;
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

    // Public method สำหรับ manual hide (ใช้เมื่อต้องการปิด loading จากภายนอก)
    public void ForceHideLoadingPanel()
    {
        HideLoadingPanel();
    }

    // Public method สำหรับตรวจสอบสถานะ
    public bool IsLoadingComplete()
    {
        return currentProgress >= 1f;
    }

    public float GetCurrentProgress()
    {
        return currentProgress;
    }
}
