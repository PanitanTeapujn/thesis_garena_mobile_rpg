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
        // Main stage buttons
        leftArrowButton.onClick.AddListener(PreviousMainStage);
        rightArrowButton.onClick.AddListener(NextMainStage);
        confirmStageButton.onClick.AddListener(ConfirmMainStage);
        backToLobbyButton.onClick.AddListener(BackToLobby);

        // Sub-stage mode buttons
        soloButton.onClick.AddListener(StartSoloGame);
        partyButton.onClick.AddListener(StartPartyGame);
        backToStageButton.onClick.AddListener(BackToMainStageSelection);

        // ปิดใช้งานปุ่ม Solo/Party ตอนเริ่มต้น
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

    // ========== Main Stage Selection ==========
    public void ShowMainStageSelection()
    {
        HideAllPanels();
        stageSelectionPanel.SetActive(true);
        UpdateMainStageDisplay();
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
        // ตรวจสอบว่าเป็น main stage ที่ถูกต้องหรือไม่
        if (mainIndex != currentMainStageIndex) return;

        selectedSubStageIndex = subIndex;

        // อัพเดท UI และเซฟข้อมูล
        UpdateSubStageUI();
        UpdateGameModeButtons();

        // เซฟ scene ที่จะโหลด
        string sceneToLoad = mainStagePanels[mainIndex].subStagePanels[subIndex].sceneToLoad;
        PlayerPrefs.SetString("SelectedStage", sceneToLoad);

        Debug.Log($"Selected sub-stage: {mainStagePanels[mainIndex].subStagePanels[subIndex].subStageName} -> {sceneToLoad}");

        // ส่ง event
        OnStageSelected?.Invoke(sceneToLoad);
    }

    void UpdateSubStageUI()
    {
        if (selectedSubStageText != null)
        {
            if (selectedSubStageIndex >= 0 &&
                currentMainStageIndex >= 0 && currentMainStageIndex < mainStagePanels.Length &&
                selectedSubStageIndex < mainStagePanels[currentMainStageIndex].subStagePanels.Length)
            {
                string subStageName = mainStagePanels[currentMainStageIndex].subStagePanels[selectedSubStageIndex].subStageName;
                selectedSubStageText.text = $"Selected: {subStageName}";
            }
            else
            {
                selectedSubStageText.text = "Please select a sub-stage";
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
    [ContextMenu("Log Current Selection")]
    void Debug_LogCurrentSelection()
    {
        Debug.Log($"Main Stage: {(currentMainStageIndex >= 0 ? mainStagePanels[currentMainStageIndex].stageName : "None")}");
        Debug.Log($"Sub Stage: {GetSelectedStageName()}");
        Debug.Log($"Scene: {GetSelectedScene()}");
    }
}