using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// Quest UI Component สำหรับแสดงผลเควสแต่ละอัน
public class QuestUI : MonoBehaviour
{
    [Header("Quest Info Display")]
    public TextMeshProUGUI questTypeText;        // Main Quest / Side Quest
    public TextMeshProUGUI questNameText;        // ชื่อเควส
    public TextMeshProUGUI questDescriptionText; // คำอธิบายสั้น
    public TextMeshProUGUI requiredLevelText;    // Level ที่ต้องการ
    public TextMeshProUGUI rewardsText;          // รางวัลที่ได้
    public Image questTypeIcon;                  // ไอคอน Main/Side Quest

    [Header("Quest Progress Display")]
    public GameObject progressContainer;         // Container สำหรับแสดง progress
    public Transform objectivesParent;           // Parent สำหรับ objectives
    public GameObject objectivePrefab;           // Prefab สำหรับแสดง objective แต่ละอัน
    public Slider overallProgressBar;            // Progress bar รวม
    public TextMeshProUGUI progressPercentText;  // เปอร์เซ็นต์ความคืบหน้า

    [Header("Quest Action Buttons")]
    public Button acceptButton;     // ปุ่มรับเควส
    public Button detailButton;     // ปุ่มดูรายละเอียด
    public Button cancelButton;     // ปุ่มยกเลิกเควส
    public Button rewardButton;     // ปุ่มรับรางวัล

    [Header("Quest Status")]
    public GameObject availableState;    // แสดงเมื่อเควสพร้อมรับได้
    public GameObject acceptedState;     // แสดงเมื่อรับเควสแล้ว
    public GameObject completedState;    // แสดงเมื่อทำเสร็จแล้ว (รอรับรางวัล)
    public GameObject lockedState;       // แสดงเมื่อยังรับเควสไม่ได้

    // Private Variables
    private QuestData currentQuestData;
    private RuntimeQuest currentRuntimeQuest;
    private QuestLobby questLobby;
    private QuestStatus currentStatus;

    // Events
    public System.Action<QuestData> OnQuestAccepted;
    public System.Action<QuestData> OnQuestCanceled;
    public System.Action<QuestData> OnQuestRewardClaimed;
    public System.Action<QuestData> OnQuestDetailRequested;

    #region Setup Methods

    public void Initialize(QuestLobby lobby)
    {
        questLobby = lobby;
        SetupButtonListeners();
    }

    private void SetupButtonListeners()
    {
        if (acceptButton != null)
            acceptButton.onClick.AddListener(OnAcceptButtonClicked);
        if (detailButton != null)
            detailButton.onClick.AddListener(OnDetailButtonClicked);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelButtonClicked);
        if (rewardButton != null)
            rewardButton.onClick.AddListener(OnRewardButtonClicked);
    }

    public void SetupQuest(QuestData questData, QuestStatus status = QuestStatus.Available)
    {
        currentQuestData = questData;
        currentStatus = status;

        if (questData == null)
        {
            Debug.LogError("[QuestUI] QuestData is null!");
            return;
        }

        // แสดงข้อมูลเควสพื้นฐาน
        DisplayQuestInfo();

        // แสดงสถานะของเควส
        UpdateQuestStatus(status);
    }

    public void SetupRuntimeQuest(RuntimeQuest runtimeQuest)
    {
        currentRuntimeQuest = runtimeQuest;
        currentQuestData = runtimeQuest.questData;
        currentStatus = runtimeQuest.currentStatus;

        // แสดงข้อมูลเควส
        DisplayQuestInfo();

        // แสดง progress
        DisplayQuestProgress();

        // อัปเดตสถานะ
        UpdateQuestStatus(runtimeQuest.currentStatus);
    }

    #endregion

    #region Display Methods

    private void DisplayQuestInfo()
    {
        if (currentQuestData == null) return;

        // แสดงประเภทเควส
        if (questTypeText != null)
        {
            questTypeText.text = currentQuestData.GetQuestTypeText();
            questTypeText.color = currentQuestData.GetQuestTypeColor();
        }

        // แสดงชื่อเควส
        if (questNameText != null)
        {
            questNameText.text = currentQuestData.questName;
        }

        // แสดงคำอธิบาย
        if (questDescriptionText != null)
        {
            questDescriptionText.text = currentQuestData.questDescription;
        }

        // แสดง Level ที่ต้องการ
        if (requiredLevelText != null)
        {
            requiredLevelText.text = $"Required Level: {currentQuestData.requiredPlayerLevel}";
        }

        // แสดงรางวัล
        if (rewardsText != null)
        {
            rewardsText.text = $"Rewards: {currentQuestData.GetAllRewardsText()}";
        }

        // แสดงไอคอนประเภทเควส
        if (questTypeIcon != null)
        {
            // สามารถเพิ่ม sprite ไอคอนได้ตามต้องการ
            questTypeIcon.color = currentQuestData.GetQuestTypeColor();
        }
    }

    private void DisplayQuestProgress()
    {
        if (currentRuntimeQuest == null || progressContainer == null) return;

        // แสดง progress container
        progressContainer.SetActive(true);

        // ลบ objective UI เก่า
        if (objectivesParent != null)
        {
            foreach (Transform child in objectivesParent)
            {
                Destroy(child.gameObject);
            }
        }

        // สร้าง objective UI ใหม่
        foreach (var objective in currentRuntimeQuest.runtimeObjectives)
        {
            CreateObjectiveUI(objective);
        }

        // อัปเดต progress bar รวม
        if (overallProgressBar != null)
        {
            float progressPercentage = currentQuestData.GetOverallProgressPercentage();
            overallProgressBar.value = progressPercentage;
        }

        // อัปเดตข้อความเปอร์เซ็นต์
        if (progressPercentText != null)
        {
            float progressPercentage = currentQuestData.GetOverallProgressPercentage();
            progressPercentText.text = $"{Mathf.RoundToInt(progressPercentage * 100)}%";
        }
    }

    private void CreateObjectiveUI(QuestObjective objective)
    {
        if (objectivePrefab == null || objectivesParent == null) return;

        GameObject objUI = Instantiate(objectivePrefab, objectivesParent);

        // ค้นหา components ใน objective UI
        TextMeshProUGUI descText = objUI.GetComponentInChildren<TextMeshProUGUI>();
        Slider progressSlider = objUI.GetComponentInChildren<Slider>();

        if (descText != null)
        {
            descText.text = $"{objective.description} ({objective.GetProgressText()})";
        }

        if (progressSlider != null)
        {
            progressSlider.value = objective.GetProgressPercentage();
        }
    }

    private void UpdateQuestStatus(QuestStatus status)
    {
        currentStatus = status;

        // ซ่อน state ทั้งหมดก่อน
        SetAllStatesActive(false);

        // แสดง state ที่ถูกต้อง
        switch (status)
        {
            case QuestStatus.Available:
                if (availableState != null) availableState.SetActive(true);
                break;
            case QuestStatus.Accepted:
                if (acceptedState != null) acceptedState.SetActive(true);
                DisplayQuestProgress(); // แสดง progress เมื่อรับเควสแล้ว
                break;
            case QuestStatus.Completed:
                if (completedState != null) completedState.SetActive(true);
                DisplayQuestProgress(); // แสดง progress เมื่อเสร็จแล้ว
                break;
            case QuestStatus.Locked:
                if (lockedState != null) lockedState.SetActive(true);
                break;
        }

        // อัปเดตสถานะปุ่ม
        UpdateButtonStates(status);
    }

    private void SetAllStatesActive(bool active)
    {
        if (availableState != null) availableState.SetActive(active);
        if (acceptedState != null) acceptedState.SetActive(active);
        if (completedState != null) completedState.SetActive(active);
        if (lockedState != null) lockedState.SetActive(active);
    }

    private void UpdateButtonStates(QuestStatus status)
    {
        // Accept Button
        if (acceptButton != null)
        {
            acceptButton.gameObject.SetActive(status == QuestStatus.Available);
            acceptButton.interactable = status == QuestStatus.Available;
        }

        // Cancel Button
        if (cancelButton != null)
        {
            cancelButton.gameObject.SetActive(status == QuestStatus.Accepted);
        }

        // Reward Button
        if (rewardButton != null)
        {
            bool showReward = status == QuestStatus.Completed;
            rewardButton.gameObject.SetActive(showReward);

            if (showReward && currentRuntimeQuest != null)
            {
                rewardButton.interactable = currentRuntimeQuest.IsReadyForReward();
            }
        }

        // Detail Button - แสดงตลอด
        if (detailButton != null)
        {
            detailButton.gameObject.SetActive(true);
        }
    }

    #endregion

    #region Button Event Handlers

    private void OnAcceptButtonClicked()
    {
        Debug.Log($"[QuestUI] Accept quest clicked: {currentQuestData.questName}");
        OnQuestAccepted?.Invoke(currentQuestData);
    }

    private void OnDetailButtonClicked()
    {
        Debug.Log($"[QuestUI] Detail quest clicked: {currentQuestData.questName}");
        OnQuestDetailRequested?.Invoke(currentQuestData);
    }

    private void OnCancelButtonClicked()
    {
        Debug.Log($"[QuestUI] Cancel quest clicked: {currentQuestData.questName}");
        OnQuestCanceled?.Invoke(currentQuestData);
    }

    private void OnRewardButtonClicked()
    {
        Debug.Log($"[QuestUI] Reward quest clicked: {currentQuestData.questName}");
        OnQuestRewardClaimed?.Invoke(currentQuestData);
    }

    #endregion

    #region Public Update Methods

    public void RefreshProgress()
    {
        if (currentRuntimeQuest != null && currentStatus == QuestStatus.Accepted)
        {
            DisplayQuestProgress();

            // ตรวจสอบว่าเควสเสร็จแล้วหรือยัง
            if (currentRuntimeQuest.IsReadyForReward() && currentStatus != QuestStatus.Completed)
            {
                UpdateQuestStatus(QuestStatus.Completed);
            }
        }
    }

    public void UpdateObjectiveProgress(QuestObjectiveType objectiveType, string targetID, int amount = 1)
    {
        if (currentRuntimeQuest != null)
        {
            currentRuntimeQuest.UpdateObjectiveProgress(objectiveType, targetID, amount);
            RefreshProgress();
        }
    }

    #endregion

    private void OnDestroy()
    {
        // ลบ event listeners
        if (acceptButton != null)
            acceptButton.onClick.RemoveAllListeners();
        if (detailButton != null)
            detailButton.onClick.RemoveAllListeners();
        if (cancelButton != null)
            cancelButton.onClick.RemoveAllListeners();
        if (rewardButton != null)
            rewardButton.onClick.RemoveAllListeners();
    }
}