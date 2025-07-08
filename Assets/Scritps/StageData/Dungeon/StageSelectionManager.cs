using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

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
        public int requiredEnemyKills;        // จำนวน Enemy ที่ต้องกำจัด
        public string[] requiredPreviousStages;   // substage ที่ต้องผ่านมาก่อน
        public bool isFirstStage = false;         // ด่านแรกของแต่ละ Main Stage

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




   
    [Header("Stage Configuration")]
    public MainStagePanel[] mainStagePanels;
    public StageData[] availableStages; // Game Designer กำหนดที่นี่

    [Header("Stage Info Panel")]
    public GameObject stageInfoPanel;
    public TextMeshProUGUI stageNameText;
    public TextMeshProUGUI stageDescriptionText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI unlockConditionText;
    public Button selectStageButton;




    [Header("UI References")]
    public Transform stageButtonParent;
    public GameObject stageButtonPrefab;
    public Button backButton;
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
        UpdateMainStageDisplay();
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
        if (subStage.selectButton == null) return;

        bool isUnlocked = IsSubStageUnlocked(subStage);
        bool isCompleted = StageProgressManager.IsStageCompleted(subStage.sceneToLoad);

        // เปลี่ยนสีปุ่มตามสถานะ
        ColorBlock colors = subStage.selectButton.colors;

        if (isCompleted)
        {
            colors.normalColor = Color.green;      // เขียว = ผ่านแล้ว
            colors.highlightedColor = Color.green * 1.2f;
        }
        else if (isUnlocked)
        {
            colors.normalColor = Color.white;      // ขาว = ปลดล็อกแล้ว
            colors.highlightedColor = Color.cyan;
        }
        else
        {
            colors.normalColor = Color.gray;       // เทา = ล็อกอยู่
            colors.highlightedColor = Color.gray;
        }

        subStage.selectButton.colors = colors;
        subStage.selectButton.interactable = isUnlocked;

        // เปลี่ยนข้อความในปุ่ม (ถ้ามี Text component)
        TextMeshProUGUI buttonText = subStage.selectButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            if (isCompleted)
                buttonText.text = subStage.subStageName + " ✓";
            else if (isUnlocked)
                buttonText.text = subStage.subStageName;
            else
                buttonText.text = subStage.subStageName + " 🔒";
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

        // อัพเดทชื่อ map ที่เลือก
        if (selectedMapText != null && currentMainStageIndex >= 0 && currentMainStageIndex < mainStagePanels.Length)
        {
            selectedMapText.text = mainStagePanels[currentMainStageIndex].stageName;
        }

        // แสดง sub-stage panels
        ShowSubStagePanels();

        // รีเซ็ตการเลือก sub-stage
        selectedSubStageIndex = -1;
        UpdateSubStageUI();
        UpdateGameModeButtons();
        UpdateAllSubStageStatus(); // ✅ อัปเดตสถานะ

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

        // ✅ เช็คว่าปลดล็อกแล้วหรือยัง
        if (!IsSubStageUnlocked(selectedSubStage))
        {
            Debug.LogWarning($"SubStage {selectedSubStage.sceneToLoad} is still locked!");

            if (selectedSubStageText != null)
            {
                string lockMessage = "🔒 Locked";
                if (selectedSubStage.requiredPreviousStages != null && selectedSubStage.requiredPreviousStages.Length > 0)
                {
                    lockMessage += $" - Complete previous stages first";
                }

                selectedSubStageText.text = lockMessage;
                selectedSubStageText.color = Color.red;
            }
            return;
        }

        selectedSubStageIndex = subIndex;
        UpdateSubStageUI();
        UpdateGameModeButtons();

        PlayerPrefs.SetString("SelectedStage", selectedSubStage.sceneToLoad);

        // 🆕 แปลงเป็น lowercase ก่อนบันทึก
        string normalizedSceneName = selectedSubStage.sceneToLoad.ToLower();
        PlayerPrefs.SetInt($"RequiredKills_{normalizedSceneName}", selectedSubStage.requiredEnemyKills);
        PlayerPrefs.Save();

        Debug.Log($"Selected sub-stage: {selectedSubStage.subStageName} -> {selectedSubStage.sceneToLoad}");
        Debug.Log($"🎯 Saved required kills: RequiredKills_{normalizedSceneName} = {selectedSubStage.requiredEnemyKills}");

        OnStageSelected?.Invoke(selectedSubStage.sceneToLoad);
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
 
}