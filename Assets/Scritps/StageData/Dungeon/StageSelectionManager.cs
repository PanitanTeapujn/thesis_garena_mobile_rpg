using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
[System.Serializable]
public class SubStagePanel
{
    public GameObject panel;
    public string subStageName;
    public string sceneToLoad;
    public Button selectButton;

    [Header("🎯 Progression Settings")]
    public int requiredEnemyKills;
    public string[] requiredPreviousStages;
    public bool isFirstStage = false;

    [Header("📋 Stage Details (For Game Designer)")]
    [TextArea(3, 6)]
    public string stageDescription = ""; // คำอธิบายด่าน
    public string stageDifficulty = "Normal"; // ความยาก: Easy, Normal, Hard, Very Hard
    public string[] recommendedLevel; // เลเวลที่แนะนำ เช่น "Level 5-10"
    public string[] enemyTypes; // ประเภทศัตรูในด่าน
    public string[] stageRewards; // รางวัลที่ได้รับ
    [TextArea(2, 4)]
    public string designerNotes = ""; // บันทึกสำหรับ Game Designer
}

public class StageSelectionManager : MonoBehaviour
{
    [System.Serializable]
    public class SubStagePanel
    {
        public GameObject panel;
        public string subStageName;
        public string sceneToLoad;
        public Button selectButton;

        [Header("🎯 Progression Settings")]
        public int requiredEnemyKills;
        public string[] requiredPreviousStages;
        public bool isFirstStage = false;

        [Header("📋 Stage Detail")]
        [TextArea(3, 6)]
        public string stageDescription = ""; // คำอธิบายด่าน
    }

    [System.Serializable]
    public class MainStagePanel
    {
        public GameObject panel;
        public string stageName;
        public SubStagePanel[] subStagePanels;
    }

    [Header("Main Stage Selection")]
    public GameObject stageSelectionPanel;
    public Button leftArrowButton;
    public Button rightArrowButton;
    public Button confirmStageButton;
    public Button backToLobbyButton;

    [Header("Sub-Stage Selection")]
    public GameObject playModePanel;
    public Transform subStageContainer;
    public TextMeshProUGUI selectedMapText;
    public TextMeshProUGUI selectedSubStageText;
    public Button soloButton;
    public Button partyButton;
    public Button backToStageButton;

    [Header("Stage Status Display (Always Visible)")]
    public GameObject stageStatusPanel; // Panel แสดงสถานะด่านที่เลือก (แสดงตลอด)
    public TextMeshProUGUI statusStageNameText;
    public TextMeshProUGUI statusDescriptionText;
    public TextMeshProUGUI statusText; // ✅ Completed / 🔒 Locked / ⚠️ Available
    public UnityEngine.UI.Image statusIcon; // สีแสดงสถานะ



    [Header("Stage Configuration")]
    public MainStagePanel[] mainStagePanels;
    public StageData[] availableStages; // Game Designer กำหนดที่นี่

   

   


   
    // Events
    public static event Action<string> OnStageSelected; // ส่ง scene name
    public static event Action<string> OnSoloGameSelected; // ส่ง scene name
    public static event Action OnPartyGameSelected;
    public static event Action OnBackToLobby;

    private int currentMainStageIndex = 0;
    private int selectedSubStageIndex = -1;

    void Start()
    {
        SetupButtons();
        SetupSubStageButtons();
        HideAllPanels();

        // ✅ รอให้ข้อมูลโหลดเสร็จก่อนแสดงด่าน
        StartCoroutine(WaitForDataThenUpdateStages());

        // แสดง Status Panel
        if (stageStatusPanel != null)
        {
            stageStatusPanel.SetActive(true);
            ClearStatusDisplay();
        }
    }

    void SetupButtons()
    {
        leftArrowButton.onClick.AddListener(PreviousMainStage);
        rightArrowButton.onClick.AddListener(NextMainStage);
        confirmStageButton.onClick.AddListener(ConfirmMainStage);
        backToLobbyButton.onClick.AddListener(BackToLobby);

        soloButton.onClick.AddListener(StartSoloGame);
        partyButton.onClick.AddListener(StartPartyGame);
        backToStageButton.onClick.AddListener(BackToMainStageSelection);

        soloButton.interactable = false;
        partyButton.interactable = false;
    }

    void SetupSubStageButtons()
    {
        for (int mainIndex = 0; mainIndex < mainStagePanels.Length; mainIndex++)
        {
            for (int subIndex = 0; subIndex < mainStagePanels[mainIndex].subStagePanels.Length; subIndex++)
            {
                SubStagePanel subStagePanel = mainStagePanels[mainIndex].subStagePanels[subIndex];

                if (subStagePanel.selectButton != null)
                {
                    int capturedMainIndex = mainIndex;
                    int capturedSubIndex = subIndex;

                    subStagePanel.selectButton.onClick.RemoveAllListeners();
                    subStagePanel.selectButton.onClick.AddListener(() => SelectSubStage(capturedMainIndex, capturedSubIndex));
                }
            }
        }
    }

    void UpdateAllSubStageStatus()
    {
        for (int mainIndex = 0; mainIndex < mainStagePanels.Length; mainIndex++)
        {
            for (int subIndex = 0; subIndex < mainStagePanels[mainIndex].subStagePanels.Length; subIndex++)
            {
                SubStagePanel subStage = mainStagePanels[mainIndex].subStagePanels[subIndex];
                UpdateSubStageButtonStatus(subStage);
            }
        }
    }

    void UpdateSubStageButtonStatus(SubStagePanel subStage)
    {
        if (subStage.panel == null) return;

        bool isUnlocked = IsSubStageUnlocked(subStage);
        bool isCompleted = StageProgressManager.IsStageCompleted(subStage.sceneToLoad);

        // ✅ เพิ่ม Debug เพื่อตรวจสอบ
        Debug.Log($"[UpdateStatus] Stage: {subStage.sceneToLoad}, Completed: {isCompleted}, Unlocked: {isUnlocked}");

        // หา Image component ใน panel
        UnityEngine.UI.Image panelImage = subStage.panel.GetComponent<UnityEngine.UI.Image>();

        if (panelImage != null)
        {
            if (isCompleted)
            {
                // ✅ ด่านที่ผ่านแล้ว - สีเขียว
                panelImage.color = Color.white;
                Debug.Log($"[UpdateStatus] ✅ {subStage.sceneToLoad} set to GREEN");
            }
            else if (isUnlocked)
            {
                // ⚠️ ด่านที่ยังไม่ผ่าน - สีเทา
                panelImage.color = Color.white;
                Debug.Log($"[UpdateStatus] ⚠️ {subStage.sceneToLoad} set to GRAY");
            }
            else
            {
                // 🔒 ด่านที่ล็อก - สีดำ
                panelImage.color = Color.black;
                Debug.Log($"[UpdateStatus] 🔒 {subStage.sceneToLoad} set to BLACK");
            }
        }

        // รูปภาพอื่นๆ ใน panel
        

        // อัปเดตปุ่ม
        if (subStage.selectButton != null)
        {
            subStage.selectButton.interactable = isUnlocked;
        }

        // อัปเดตข้อความ
        TextMeshProUGUI buttonText = subStage.panel.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            if (isCompleted)
            {
                buttonText.text = subStage.subStageName + "";
                buttonText.color = Color.white;
            }
            else if (!isUnlocked)
            {
                buttonText.text = subStage.subStageName + "";
                buttonText.color = Color.white;
            }
            else
            {
                buttonText.color = Color.white;
            }
        }
    }

    // ✅ เช็คว่า substage ปลดล็อกแล้วหรือยัง
    bool IsSubStageUnlocked(SubStagePanel subStage)
    {
        // ด่านแรกของแต่ละ Main Stage ปลดล็อกอัตโนมัติ
        if (subStage.isFirstStage) return true;

        // เช็คว่าผ่าน substage ที่ต้องการมาก่อนหรือยัง
        if (subStage.requiredPreviousStages != null)
        {
            foreach (string requiredStage in subStage.requiredPreviousStages)
            {
                if (!StageProgressManager.IsStageCompleted(requiredStage))
                {
                    return false;
                }
            }
        }

        return true;
    }

    // ========== Main Stage Selection ==========
    public void ShowMainStageSelection()
    {
        HideAllPanels();
        stageSelectionPanel.SetActive(true);
        UpdateMainStageDisplay();
        UpdateAllSubStageStatus();
    }

    void PreviousMainStage()
    {
        currentMainStageIndex--;
        if (currentMainStageIndex < 0)
            currentMainStageIndex = mainStagePanels.Length - 1;
        UpdateMainStageDisplay();
    }

    void NextMainStage()
    {
        currentMainStageIndex++;
        if (currentMainStageIndex >= mainStagePanels.Length)
            currentMainStageIndex = 0;
        UpdateMainStageDisplay();
    }

    void UpdateMainStageDisplay()
    {
        // ซ่อน main stage panels ทั้งหมด
        for (int i = 0; i < mainStagePanels.Length; i++)
        {
            if (mainStagePanels[i].panel != null)
                mainStagePanels[i].panel.SetActive(false);
        }

        // แสดง main stage panel ปัจจุบัน
        if (currentMainStageIndex >= 0 && currentMainStageIndex < mainStagePanels.Length)
        {
            if (mainStagePanels[currentMainStageIndex].panel != null)
                mainStagePanels[currentMainStageIndex].panel.SetActive(true);
        }
    }

    void ConfirmMainStage()
    {
        if (currentMainStageIndex >= 0 && currentMainStageIndex < mainStagePanels.Length)
        {
            PlayerPrefs.SetString("SelectedMainStage", mainStagePanels[currentMainStageIndex].stageName);
            Debug.Log($"Selected main stage: {mainStagePanels[currentMainStageIndex].stageName}");

            // รีเซ็ต sub-stage selection
            selectedSubStageIndex = -1;

            ShowSubStageSelection();
        }
    }

    // ========== Sub-Stage Selection ==========
    void ShowSubStageSelection()
    {
        HideAllPanels();
        playModePanel.SetActive(true);

        // แสดง Status Panel
        if (stageStatusPanel != null)
        {
            stageStatusPanel.SetActive(true);
            ClearStatusDisplay();
        }

        if (selectedMapText != null && currentMainStageIndex >= 0 && currentMainStageIndex < mainStagePanels.Length)
        {
            selectedMapText.text = mainStagePanels[currentMainStageIndex].stageName;
        }

        ShowSubStagePanels();

        // ✅ อัปเดตสถานะทุกครั้งที่เปิด panel
        UpdateAllSubStageStatus();

        selectedSubStageIndex = -1;
        UpdateSubStageUI();
        UpdateGameModeButtons();
    }

    void ShowSubStagePanels()
    {
        // ซ่อน sub-stage panels ทั้งหมด
        HideAllSubStagePanels();

        // แสดงเฉพาะ sub-stage panels ของ main stage ที่เลือก
        if (currentMainStageIndex >= 0 && currentMainStageIndex < mainStagePanels.Length)
        {
            SubStagePanel[] currentSubStages = mainStagePanels[currentMainStageIndex].subStagePanels;

            for (int i = 0; i < currentSubStages.Length; i++)
            {
                if (currentSubStages[i].panel != null)
                    currentSubStages[i].panel.SetActive(true);
            }
        }
    }

    void HideAllSubStagePanels()
    {
        for (int mainIndex = 0; mainIndex < mainStagePanels.Length; mainIndex++)
        {
            for (int subIndex = 0; subIndex < mainStagePanels[mainIndex].subStagePanels.Length; subIndex++)
            {
                if (mainStagePanels[mainIndex].subStagePanels[subIndex].panel != null)
                    mainStagePanels[mainIndex].subStagePanels[subIndex].panel.SetActive(false);
            }
        }
    }

    void SelectSubStage(int mainIndex, int subIndex)
    {
        if (mainIndex != currentMainStageIndex) return;

        SubStagePanel selectedSubStage = mainStagePanels[mainIndex].subStagePanels[subIndex];

        // อัปเดต Status Panel
        UpdateStatusDisplay(selectedSubStage);

        // ✅ เช็คว่าปลดล็อกแล้วหรือยัง
        if (!IsSubStageUnlocked(selectedSubStage))
        {
            Debug.LogWarning($"SubStage {selectedSubStage.sceneToLoad} is still locked!");
            return;
        }

        selectedSubStageIndex = subIndex;
        UpdateSubStageUI();
        UpdateGameModeButtons();

        PlayerPrefs.SetString("SelectedStage", selectedSubStage.sceneToLoad);

        string normalizedSceneName = selectedSubStage.sceneToLoad.ToLower();
        PlayerPrefs.SetInt($"RequiredKills_{normalizedSceneName}", selectedSubStage.requiredEnemyKills);
        PlayerPrefs.Save();

        Debug.Log($"Selected sub-stage: {selectedSubStage.subStageName} -> {selectedSubStage.sceneToLoad}");

        OnStageSelected?.Invoke(selectedSubStage.sceneToLoad);
    }
    void UpdateStatusDisplay(SubStagePanel subStage)
    {
        if (stageStatusPanel == null) return;

        bool isUnlocked = IsSubStageUnlocked(subStage);
        bool isCompleted = StageProgressManager.IsStageCompleted(subStage.sceneToLoad);

        // ✅ Debug
        Debug.Log($"[StatusDisplay] Stage: {subStage.sceneToLoad}, Completed: {isCompleted}, Unlocked: {isUnlocked}");

        // ชื่อด่าน
        if (statusStageNameText != null)
        {
            statusStageNameText.text = subStage.subStageName;
        }

        // คำอธิบายด่าน
        if (statusDescriptionText != null)
        {
            if (!string.IsNullOrEmpty(subStage.stageDescription))
                statusDescriptionText.text = subStage.stageDescription;
            else
                statusDescriptionText.text = "No description available.";
        }

        // สถานะด่าน
        if (statusText != null)
        {
            if (isCompleted)
            {
                statusText.text = "COMPLETED";
                statusText.color = Color.white;
            }
            else if (isUnlocked)
            {
                statusText.text = "AVAILABLE";
                statusText.color = Color.black;
            }
            else
            {
                statusText.text = "LOCKED";
                statusText.color = Color.black;
            }
        }

        // ไอคอนสถานะ
        if (statusIcon != null)
        {
            if (isCompleted)
                statusIcon.color = Color.green;
            else if (isUnlocked)
                statusIcon.color = Color.black;
            else
                statusIcon.color = Color.black;
        }
    }
    // ✅ เพิ่ม Coroutine ใหม่
    private System.Collections.IEnumerator WaitForDataThenUpdateStages()
    {
        Debug.Log("[StageSelection] 🔄 Waiting for PersistentPlayerData...");

        float timeout = 10f;
        float elapsed = 0f;

        // รอจนกว่าข้อมูลจะโหลดเสร็จ
        while (elapsed < timeout)
        {
            if (PersistentPlayerData.Instance != null &&
                PersistentPlayerData.Instance.isDataLoaded &&
                PersistentPlayerData.Instance.HasValidData())
            {
                Debug.Log("[StageSelection] ✅ Data loaded successfully");
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (elapsed >= timeout)
        {
            Debug.LogError("[StageSelection] ❌ Timeout waiting for data");
        }

        // ✅ อัปเดตสถานะด่านทั้งหมดหลังโหลดข้อมูลเสร็จ
        UpdateAllSubStageStatus();
        UpdateMainStageDisplay();

        Debug.Log("[StageSelection] 🎯 Stage status updated from Firebase data");
    }
    void ClearStatusDisplay()
    {
        if (statusStageNameText != null)
            statusStageNameText.text = "Select a Stage";

        if (statusDescriptionText != null)
            statusDescriptionText.text = "Click on a stage to view details.";

        if (statusText != null)
        {
            statusText.text = "-";
            statusText.color = Color.white;
        }

        if (statusIcon != null)
        {
            statusIcon.color = Color.white;
        }
    }
    void UpdateSubStageUI()
    {
        if (selectedSubStageText != null)
        {
            if (selectedSubStageIndex >= 0 &&
                currentMainStageIndex >= 0 && currentMainStageIndex < mainStagePanels.Length &&
                selectedSubStageIndex < mainStagePanels[currentMainStageIndex].subStagePanels.Length)
            {
                SubStagePanel subStage = mainStagePanels[currentMainStageIndex].subStagePanels[selectedSubStageIndex];
                string subStageName = subStage.subStageName;

                // ✅ ใช้ StageProgressManager ที่แก้ไขแล้ว
                bool isCompleted = StageProgressManager.IsStageCompleted(subStage.sceneToLoad);
                int currentKills = StageProgressManager.GetEnemyKills(subStage.sceneToLoad);
                int requiredKills = EnemyKillTracker.GetRequiredKillsForStage(subStage.sceneToLoad);

               
            }
            else
            {
                selectedSubStageText.text = "Please select a sub-stage";
                selectedSubStageText.color = Color.white;
            }
        }
    }
    void UpdateGameModeButtons()
    {
        bool hasSelectedSubStage = selectedSubStageIndex >= 0;
        soloButton.interactable = hasSelectedSubStage;
        partyButton.interactable = hasSelectedSubStage;
    }

    // ========== Game Mode Selection ==========
    void StartSoloGame()
    {
        if (selectedSubStageIndex < 0)
        {
            Debug.LogWarning("Please select a sub-stage first!");
            return;
        }

        string sceneToLoad = PlayerPrefs.GetString("SelectedStage", "PlayRoom1_1");

        // 🆕 Debug ตรวจสอบ PlayerPrefs
        SubStagePanel selectedSubStage = mainStagePanels[currentMainStageIndex].subStagePanels[selectedSubStageIndex];
        Debug.Log($"🎮 [StartSoloGame] Scene: {sceneToLoad}");
        Debug.Log($"🎮 [StartSoloGame] Required kills: {selectedSubStage.requiredEnemyKills}");
        Debug.Log($"🎮 [StartSoloGame] PlayerPrefs key: RequiredKills_{sceneToLoad}");
        Debug.Log($"🎮 [StartSoloGame] PlayerPrefs value: {PlayerPrefs.GetInt($"RequiredKills_{sceneToLoad}", -999)}");

        OnSoloGameSelected?.Invoke(sceneToLoad);
    }

    void StartPartyGame()
    {
        if (selectedSubStageIndex < 0)
        {
            Debug.LogWarning("Please select a sub-stage first!");
            return;
        }

        OnPartyGameSelected?.Invoke();
    }

    // ========== Navigation ==========
    void BackToMainStageSelection()
    {
        ShowMainStageSelection();
    }

    void BackToLobby()
    {
        HideAllPanels();
        OnBackToLobby?.Invoke();
    }

    void HideAllPanels()
    {
        stageSelectionPanel.SetActive(false);
        playModePanel.SetActive(false);

        // ซ่อน main stage panels
        for (int i = 0; i < mainStagePanels.Length; i++)
        {
            if (mainStagePanels[i].panel != null)
                mainStagePanels[i].panel.SetActive(false);
        }

        // ซ่อน sub-stage panels
        HideAllSubStagePanels();
    }

    // ========== Public Methods ==========
    public void SetMainStageIndex(int index)
    {
        if (index >= 0 && index < mainStagePanels.Length)
        {
            currentMainStageIndex = index;
            UpdateMainStageDisplay();
        }
    }

    public string GetSelectedStageName()
    {
        if (currentMainStageIndex >= 0 && currentMainStageIndex < mainStagePanels.Length &&
            selectedSubStageIndex >= 0 && selectedSubStageIndex < mainStagePanels[currentMainStageIndex].subStagePanels.Length)
        {
            return mainStagePanels[currentMainStageIndex].subStagePanels[selectedSubStageIndex].subStageName;
        }
        return "";
    }

    public string GetSelectedScene()
    {
        return PlayerPrefs.GetString("SelectedStage", "");
    }

    // ========== Debug Methods ==========
    // เพิ่มใน StageSelectionManager
    
}