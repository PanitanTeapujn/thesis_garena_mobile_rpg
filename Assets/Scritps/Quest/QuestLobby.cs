using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Fusion;


public class QuestLobby : MonoBehaviour
{
    [Header("Quest Panel")]
    public GameObject questPanel;
    public Button closeQuestButton;

    [Header("Quest Content")]
    public Transform questContent;           // Content สำหรับแสดงรายการเควส
    public GameObject questUIPrefab;         // Prefab ของ Quest UI
    public ScrollRect questScrollRect;       // ScrollRect สำหรับเลื่อนดูเควส

    [Header("Quest Detail Panel")]
    public GameObject questDetailPanel;      // Panel แสดงรายละเอียดเควส
    public TextMeshProUGUI detailQuestName;
    public TextMeshProUGUI detailQuestType;
    public TextMeshProUGUI detailDescription;
    public TextMeshProUGUI detailObjectives;
    public TextMeshProUGUI detailRewards;
    public Button closeDetailButton;

    [Header("Quest Limits")]
    public int maxActiveQuests = 3;          // จำนวนเควสที่รับได้สูงสุด
    public TextMeshProUGUI activeQuestCountText; // แสดงจำนวนเควสที่กำลังทำ

    // Private Variables
    private List<QuestUI> currentQuestUIs = new List<QuestUI>();
    private List<RuntimeQuest> activeQuests = new List<RuntimeQuest>();
    private List<string> completedQuestIDs = new List<string>();
    private int currentPlayerLevel = 1;

    void Start()
    {
        SetupEventListeners();
        LoadPlayerQuestData();
        RefreshQuestList();
    }

    private void SetupEventListeners()
    {
        if (closeQuestButton != null)
            closeQuestButton.onClick.AddListener(CloseQuestPanel);
        if (closeDetailButton != null)
            closeDetailButton.onClick.AddListener(CloseDetailPanel);
    }

    public void RefreshQuestList()
    {
        Debug.Log("[QuestLobby] Refreshing quest list...");

        // ลบ Quest UI เก่า
        ClearQuestUIs();

        // โหลดเควสที่พร้อมรับได้
        LoadAvailableQuests();

        // อัปเดตจำนวนเควสที่กำลังทำ
        UpdateActiveQuestCount();
    }

    private void ClearQuestUIs()
    {
        foreach (var questUI in currentQuestUIs)
        {
            if (questUI != null)
                Destroy(questUI.gameObject);
        }
        currentQuestUIs.Clear();
    }

    private void LoadAvailableQuests()
    {
        if (QuestDatabase.Instance == null)
        {
            Debug.LogError("[QuestLobby] QuestDatabase not found!");
            return;
        }

        // โหลดเควสทั้งหมดที่พร้อมรับได้
        var availableQuests = QuestDatabase.Instance.GetAllAvailableQuests(currentPlayerLevel, completedQuestIDs);

        Debug.Log($"[QuestLobby] Found {availableQuests.Count} available quests");

        // แยก Main Quest และ Side Quest
        var mainQuests = new List<QuestData>();
        var sideQuests = new List<QuestData>();

        foreach (var quest in availableQuests)
        {
            if (quest.questType == QuestType.MainQuest)
                mainQuests.Add(quest);
            else
                sideQuests.Add(quest);
        }

        // แสดง Main Quest ก่อน
        foreach (var quest in mainQuests)
        {
            CreateQuestUI(quest, GetQuestStatus(quest));
        }

        // ตามด้วย Side Quest
        foreach (var quest in sideQuests)
        {
            CreateQuestUI(quest, GetQuestStatus(quest));
        }

        // แสดงเควสที่กำลังทำ
        foreach (var runtimeQuest in activeQuests)
        {
            CreateQuestUI(runtimeQuest);
        }
    }

    private void CreateQuestUI(QuestData questData, QuestStatus status)
    {
        if (questUIPrefab == null || questContent == null)
        {
            Debug.LogError("[QuestLobby] Quest UI Prefab or Content not assigned!");
            return;
        }

        GameObject questUIObj = Instantiate(questUIPrefab, questContent);
        QuestUI questUI = questUIObj.GetComponent<QuestUI>();

        if (questUI == null)
        {
            Debug.LogError("[QuestLobby] QuestUI component not found on prefab!");
            Destroy(questUIObj);
            return;
        }

        // Initialize และ setup quest UI
        questUI.Initialize(this);
        questUI.SetupQuest(questData, status);

        // Subscribe events
        questUI.OnQuestAccepted += OnQuestAccepted;
        questUI.OnQuestCanceled += OnQuestCanceled;
        questUI.OnQuestRewardClaimed += OnQuestRewardClaimed;
        questUI.OnQuestDetailRequested += OnQuestDetailRequested;

        currentQuestUIs.Add(questUI);

        Debug.Log($"[QuestLobby] Created quest UI for: {questData.questName}");
    }

    private void CreateQuestUI(RuntimeQuest runtimeQuest)
    {
        if (questUIPrefab == null || questContent == null) return;

        GameObject questUIObj = Instantiate(questUIPrefab, questContent);
        QuestUI questUI = questUIObj.GetComponent<QuestUI>();

        if (questUI == null)
        {
            Destroy(questUIObj);
            return;
        }

        // Setup runtime quest
        questUI.Initialize(this);
        questUI.SetupRuntimeQuest(runtimeQuest);

        // Subscribe events
        questUI.OnQuestAccepted += OnQuestAccepted;
        questUI.OnQuestCanceled += OnQuestCanceled;
        questUI.OnQuestRewardClaimed += OnQuestRewardClaimed;
        questUI.OnQuestDetailRequested += OnQuestDetailRequested;

        currentQuestUIs.Add(questUI);
    }

    private QuestStatus GetQuestStatus(QuestData questData)
    {
        // ตรวจสอบว่าเควสนี้กำลังทำอยู่หรือไม่
        foreach (var activeQuest in activeQuests)
        {
            if (activeQuest.questData.questID == questData.questID)
            {
                return activeQuest.currentStatus;
            }
        }

        // ตรวจสอบว่าทำเสร็จแล้วหรือไม่
        if (completedQuestIDs.Contains(questData.questID))
        {
            return QuestStatus.Finished;
        }

        // ตรวจสอบว่ารับได้หรือไม่
        if (questData.CanPlayerAcceptQuest(currentPlayerLevel, completedQuestIDs))
        {
            return QuestStatus.Available;
        }

        return QuestStatus.Locked;
    }

    #region Quest Event Handlers

    private void OnQuestAccepted(QuestData questData)
    {
        Debug.Log($"[QuestLobby] Quest accepted: {questData.questName}");

        // ตรวจสอบจำนวนเควสที่กำลังทำ
        if (activeQuests.Count >= maxActiveQuests)
        {
            ShowMessage($"Cannot accept more quests! Maximum {maxActiveQuests} active quests allowed.");
            return;
        }

        // สร้าง Runtime Quest
        RuntimeQuest newRuntimeQuest = new RuntimeQuest(questData);
        newRuntimeQuest.currentStatus = QuestStatus.Accepted;

        activeQuests.Add(newRuntimeQuest);

        // บันทึกข้อมูล
        SavePlayerQuestData();

        // รีเฟรช UI
        RefreshQuestList();

        ShowMessage($"Quest accepted: {questData.questName}");
    }

    private void OnQuestCanceled(QuestData questData)
    {
        Debug.Log($"[QuestLobby] Quest canceled: {questData.questName}");

        // หาและลบเควสที่ยกเลิก
        for (int i = activeQuests.Count - 1; i >= 0; i--)
        {
            if (activeQuests[i].questData.questID == questData.questID)
            {
                activeQuests.RemoveAt(i);
                break;
            }
        }

        // บันทึกข้อมูล
        SavePlayerQuestData();

        // รีเฟรช UI
        RefreshQuestList();

        ShowMessage($"Quest canceled: {questData.questName}");
    }

    private void OnQuestRewardClaimed(QuestData questData)
    {
        Debug.Log($"[QuestLobby] Quest reward claimed: {questData.questName}");

        // หา RuntimeQuest
        RuntimeQuest questToComplete = null;
        for (int i = activeQuests.Count - 1; i >= 0; i--)
        {
            if (activeQuests[i].questData.questID == questData.questID)
            {
                questToComplete = activeQuests[i];
                activeQuests.RemoveAt(i);
                break;
            }
        }

        if (questToComplete == null)
        {
            Debug.LogError("[QuestLobby] Quest to complete not found!");
            return;
        }

        // เพิ่มเข้าใน completed quests
        if (!completedQuestIDs.Contains(questData.questID))
        {
            completedQuestIDs.Add(questData.questID);
        }

        // ให้รางวัล (จะทำในเฟส 2)
        GiveQuestRewards(questData);

        // บันทึกข้อมูล
        SavePlayerQuestData();

        // รีเฟรช UI
        RefreshQuestList();

        ShowMessage($"Quest completed: {questData.questName}");
    }

    private void OnQuestDetailRequested(QuestData questData)
    {
        ShowQuestDetailPanel(questData);
    }

    #endregion

    #region Quest Detail Panel

    private void ShowQuestDetailPanel(QuestData questData)
    {
        if (questDetailPanel == null) return;

        questDetailPanel.SetActive(true);

        if (detailQuestName != null)
            detailQuestName.text = questData.questName;

        if (detailQuestType != null)
        {
            detailQuestType.text = questData.GetQuestTypeText();
            detailQuestType.color = questData.GetQuestTypeColor();
        }

        if (detailDescription != null)
            detailDescription.text = questData.questDetailDescription;

        if (detailObjectives != null)
        {
            string objectivesText = "Objectives:\n";
            foreach (var objective in questData.objectives)
            {
                objectivesText += $"• {objective.description}\n";
            }
            detailObjectives.text = objectivesText;
        }

        if (detailRewards != null)
            detailRewards.text = $"Rewards: {questData.GetAllRewardsText()}";
    }

    private void CloseDetailPanel()
    {
        if (questDetailPanel != null)
            questDetailPanel.SetActive(false);
    }

    #endregion

    #region Helper Methods

    private void UpdateActiveQuestCount()
    {
        if (activeQuestCountText != null)
        {
            activeQuestCountText.text = $"Active Quests: {activeQuests.Count}/{maxActiveQuests}";
        }
    }

    private void GiveQuestRewards(QuestData questData)
    {
        // TODO: ใช้ในเฟส 2 - ให้รางวัลจริง
        Debug.Log($"[QuestLobby] Giving rewards for quest: {questData.questName}");
        foreach (var reward in questData.rewards)
        {
            Debug.Log($"[QuestLobby] Reward: {reward.GetRewardText()}");
        }
    }

    private void ShowMessage(string message)
    {
        Debug.Log($"[QuestLobby] {message}");
        // TODO: แสดง UI message
    }

    private void LoadPlayerQuestData()
    {
        // TODO: โหลดข้อมูลจาก Save System
        currentPlayerLevel = 1;
        activeQuests.Clear();
        completedQuestIDs.Clear();

        Debug.Log("[QuestLobby] Player quest data loaded");
    }

    private void SavePlayerQuestData()
    {
        // TODO: บันทึกข้อมูลลง Save System
        Debug.Log("[QuestLobby] Player quest data saved");
    }

    #endregion

    private void CloseQuestPanel()
    {
        if (questPanel != null)
            questPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (closeQuestButton != null)
            closeQuestButton.onClick.RemoveAllListeners();
        if (closeDetailButton != null)
            closeDetailButton.onClick.RemoveAllListeners();
    }
}
