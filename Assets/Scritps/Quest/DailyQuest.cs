using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

[System.Serializable]
public class DailyQuestData
{
    public string questId;
    public string questName;
    public string questDescription;
    public QuestType questType;
    public int targetValue; // จำนวนที่ต้องทำให้ครบ
    public int currentProgress; // ความก้าวหน้าปัจจุบัน
    public bool isCompleted;
    public bool isRewardClaimed;
    public QuestReward reward;
    public DateTime questDate; // วันที่ของ quest
    public string lastUpdateTime;

    public bool IsExpired()
    {
        return questDate.Date < DateTime.Now.Date;
    }

    public bool CanClaimReward()
    {
        return isCompleted && !isRewardClaimed && !IsExpired();
    }

    public float GetProgressPercentage()
    {
        if (targetValue <= 0) return 0f;
        return Mathf.Clamp01((float)currentProgress / targetValue);
    }
}

[System.Serializable]
public class QuestReward
{
    public int goldReward;
    public int gemsReward;
    public int expReward;

    public bool HasReward()
    {
        return goldReward > 0 || gemsReward > 0 || expReward > 0;
    }
}

public enum QuestType
{
    DailyLogin,    // เข้าเกมครั้งแรกของวัน
    KillEnemies,   // ฆ่าศัตรู
    CompleteStages, // ผ่านด่าน
    CollectItems   // เก็บไอเทม
}

public class DailyQuest : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject dailyQuestPanel;

    [Header("Buttons")]
    public Button openPanelButton;
    public Button closePanelButton;

    [Header("Quest UI")]
    public Transform questContainer;
    public GameObject questItemPrefab;

    [Header("Quest Settings")]
    public List<DailyQuestTemplate> questTemplates = new List<DailyQuestTemplate>();

    // Data
    private List<DailyQuestData> currentQuests = new List<DailyQuestData>();
    private DateTime lastQuestUpdate;

    // References
    private PersistentPlayerData persistentData;

    void Start()
    {
        InitializeSystem();
        SetupUI();
        LoadDailyQuests();
        CheckAndUpdateQuests();

        // เพิ่มการฟัง events จาก tracker
        SubscribeToQuestTracker();

        // เพิ่ม: Refresh ทุก 5 วินาทีเพื่อตรวจสอบ Firebase data
        InvokeRepeating("ForceRefreshQuestsFromFirebase", 5f, 5f);
    }
    private void SubscribeToQuestTracker()
    {
        DailyQuestTracker.OnEnemiesKilledUpdate += OnEnemiesKilledFromGame;
        DailyQuestTracker.OnStagesCompletedUpdate += OnStagesCompletedFromGame;
        Debug.Log("[DailyQuest] Subscribed to quest tracker events");
    }

    private void OnDestroy()
    {
        // Unsubscribe เมื่อ destroy
        DailyQuestTracker.OnEnemiesKilledUpdate -= OnEnemiesKilledFromGame;
        DailyQuestTracker.OnStagesCompletedUpdate -= OnStagesCompletedFromGame;
    }

    private void OnEnemiesKilledFromGame(int killCount)
    {
        Debug.Log($"[DailyQuest] Received {killCount} enemy kills from session");

        // อัปเดต quest progress (อาจจะอัปเดตแล้วจาก Firebase แต่ทำเพื่อให้แน่ใจ)
        UpdateQuestProgress(QuestType.KillEnemies, killCount);

        // Refresh UI เพื่อแสดงผลล่าสุด
        RefreshQuestUI();
    }

    private void OnStagesCompletedFromGame(int stageCount)
    {
        Debug.Log($"[DailyQuest] Received {stageCount} stage completions from session");

        // อัปเดต quest progress (อาจจะอัปเดตแล้วจาก Firebase แต่ทำเพื่อให้แน่ใจ)
        UpdateQuestProgress(QuestType.CompleteStages, stageCount);

        // Refresh UI เพื่อแสดงผลล่าสุด
        RefreshQuestUI();
    }

    // เพิ่ม method ใหม่สำหรับ force refresh จาก Firebase
    public void ForceRefreshQuestsFromFirebase()
    {
        if (persistentData?.multiCharacterData?.dailyQuests != null)
        {
            currentQuests = new List<DailyQuestData>(persistentData.multiCharacterData.dailyQuests);
            RefreshQuestUI();
            Debug.Log("[DailyQuest] Force refreshed quests from Firebase data");
        }
    }
    public void UpdateQuestProgress(QuestType questType, int amount = 1)
    {
        if (persistentData?.multiCharacterData == null)
        {
            Debug.LogWarning("[DailyQuest] PersistentPlayerData not available, cannot update quest progress");
            return;
        }

        bool questUpdated = false;

        foreach (var quest in currentQuests)
        {
            if (quest.questType == questType && !quest.isCompleted)
            {
                int oldProgress = quest.currentProgress;
                quest.currentProgress = Mathf.Min(quest.currentProgress + amount, quest.targetValue);
                quest.lastUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // ตรวจสอบว่า quest เสร็จแล้วหรือยัง
                if (quest.currentProgress >= quest.targetValue && !quest.isCompleted)
                {
                    quest.isCompleted = true;
                    Debug.Log($"[DailyQuest] ✅ Quest completed: {quest.questName}");

                    // แสดง notification (ถ้าต้องการ)
                    ShowQuestCompletionNotification(quest);
                }

                Debug.Log($"[DailyQuest] Updated {quest.questName}: {oldProgress} → {quest.currentProgress}/{quest.targetValue}");
                questUpdated = true;
                break;
            }
        }

        if (questUpdated)
        {
            SaveQuestData();
            RefreshQuestUI();
        }
        else
        {
            Debug.Log($"[DailyQuest] No active quest found for type: {questType}");
        }
    }

    // เพิ่ม method แสดง notification เมื่อ quest เสร็จ
    private void ShowQuestCompletionNotification(DailyQuestData quest)
    {
        // TODO: สร้าง popup หรือ notification UI สำหรับแจ้งว่า quest เสร็จแล้ว
        Debug.Log($"🎉 [DailyQuest] QUEST COMPLETED: {quest.questName}!");
        Debug.Log($"💰 Rewards available: {quest.reward.goldReward} gold, {quest.reward.gemsReward} gems, {quest.reward.expReward} exp");
    }
    // เพิ่ม method นี้ในส่วนท้าย
    public void OnReturnToLobby()
    {
        // เรียกเมื่อผู้เล่นกลับมา lobby
        DailyQuestTracker.FlushProgressToQuests();
    }

    void InitializeSystem()
    {
        persistentData = PersistentPlayerData.Instance;

        // สร้าง quest template เริ่มต้น
        if (questTemplates.Count == 0)
        {
            CreateDefaultQuestTemplates();
        }
    }

    void SetupUI()
    {
        openPanelButton.onClick.AddListener(OpenDailyQuest);
        closePanelButton.onClick.AddListener(CloseDailyQuest);
    }

    void CreateDefaultQuestTemplates()
    {
        // Daily Login Quest
        var loginQuest = new DailyQuestTemplate
        {
            questId = "daily_login",
            questName = "Daily Login",
            questDescription = "Login to the game",
            questType = QuestType.DailyLogin,
            targetValue = 1,
            reward = new QuestReward { goldReward = 500, gemsReward = 10, expReward = 100 }
        };

        questTemplates.Add(loginQuest);
    }

    void CheckAndUpdateQuests()
    {
        // ตรวจสอบให้แน่ใจว่า data พร้อมแล้ว
        if (persistentData?.isDataLoaded != true || persistentData?.multiCharacterData == null)
        {
            Debug.Log("[DailyQuest] Data not ready, skipping quest check");
            return;
        }

        DateTime today = DateTime.Now.Date;

        Debug.Log($"[DailyQuest] Checking quests - Today: {today:yyyy-MM-dd}, LastUpdate: {lastQuestUpdate:yyyy-MM-dd}");
        Debug.Log($"[DailyQuest] Current quests count: {currentQuests.Count}");

        // ตรวจสอบว่าเป็นวันใหม่หรือไม่
        if (lastQuestUpdate.Date < today)
        {
            Debug.Log("[DailyQuest] New day detected, generating new quests...");
            GenerateDailyQuests();
            lastQuestUpdate = DateTime.Now;
            SaveQuestData();
        }
        else
        {
            Debug.Log("[DailyQuest] Same day, keeping existing quests");

            // Debug ข้อมูล quest ปัจจุบัน
            foreach (var quest in currentQuests)
            {
                Debug.Log($"[DailyQuest] Quest: {quest.questName} - Completed: {quest.isCompleted} - Claimed: {quest.isRewardClaimed}");
            }
        }

        // ตรวจสอบ Daily Login Quest อัตโนมัติ
        CheckDailyLoginQuest();

        // อัปเดต UI
        RefreshQuestUI();
    }

    void GenerateDailyQuests()
    {
        Debug.Log("[DailyQuest] Generating new daily quests...");
        Debug.Log($"[DailyQuest] Before clear - Current quests: {currentQuests.Count}");

        currentQuests.Clear();
        DateTime today = DateTime.Now.Date;

        foreach (var template in questTemplates)
        {
            var questData = new DailyQuestData
            {
                questId = template.questId + "_" + today.ToString("yyyyMMdd"),
                questName = template.questName,
                questDescription = template.questDescription,
                questType = template.questType,
                targetValue = template.targetValue,
                currentProgress = 0,
                isCompleted = false,
                isRewardClaimed = false,
                reward = template.reward,
                questDate = today,
                lastUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            currentQuests.Add(questData);
            Debug.Log($"[DailyQuest] Created new quest: {questData.questName} - ID: {questData.questId}");
        }

        Debug.Log($"[DailyQuest] Generated {currentQuests.Count} new daily quests for {today:yyyy-MM-dd}");
    }

    void CheckDailyLoginQuest()
    {
        var loginQuest = currentQuests.Find(q => q.questType == QuestType.DailyLogin);
        if (loginQuest != null)
        {
            Debug.Log($"[DailyQuest] Login quest current status - Completed: {loginQuest.isCompleted}, Claimed: {loginQuest.isRewardClaimed}");

            // อัพเดทความก้าวหน้าของ Daily Login Quest อัตโนมัติ เฉพาะเมื่อยังไม่เสร็จ
            if (!loginQuest.isCompleted && !loginQuest.isRewardClaimed)
            {
                loginQuest.currentProgress = 1;
                loginQuest.isCompleted = true;
                loginQuest.lastUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                Debug.Log("[DailyQuest] Daily Login quest completed automatically!");
                SaveQuestData();
            }
            else
            {
                Debug.Log("[DailyQuest] Login quest already completed/claimed, skipping auto-complete");
            }
        }
        else
        {
            Debug.LogWarning("[DailyQuest] No Daily Login quest found!");
        }
    }

    public void ClaimQuestReward(string questId)
    {
        var quest = currentQuests.Find(q => q.questId == questId);
        if (quest == null)
        {
            Debug.LogError($"[DailyQuest] Quest not found: {questId}");
            return;
        }

        if (!quest.CanClaimReward())
        {
            Debug.LogWarning($"[DailyQuest] Cannot claim reward for quest: {questId}");
            return;
        }

        // ให้รางวัลผ่าน CurrencyManager
        var currencyManager = CurrencyManager.FindCurrencyManager();

        if (currencyManager != null)
        {
            if (quest.reward.goldReward > 0)
            {
                bool goldAdded = currencyManager.AddGold(quest.reward.goldReward, true);
                Debug.Log($"[DailyQuest] Awarded {quest.reward.goldReward} gold - Success: {goldAdded}");
            }

            if (quest.reward.gemsReward > 0)
            {
                bool gemsAdded = currencyManager.AddGems(quest.reward.gemsReward, true);
                Debug.Log($"[DailyQuest] Awarded {quest.reward.gemsReward} gems - Success: {gemsAdded}");
            }

            // บังคับ save ข้อมูลเงินลง Firebase ทันที
            currencyManager.ForceSaveCurrency();

            // รอให้ save เสร็จแล้ว save PersistentPlayerData ด้วย
            StartCoroutine(WaitAndSavePersistentData());
        }
        else
        {
            // Fallback: ใช้ PersistentPlayerData
            Debug.LogWarning("[DailyQuest] No CurrencyManager found, using PersistentPlayerData fallback");

            if (persistentData?.multiCharacterData?.sharedCurrency != null)
            {
                if (quest.reward.goldReward > 0)
                {
                    persistentData.multiCharacterData.sharedCurrency.gold += quest.reward.goldReward;
                    Debug.Log($"[DailyQuest] Awarded {quest.reward.goldReward} gold (fallback)");
                }

                if (quest.reward.gemsReward > 0)
                {
                    persistentData.multiCharacterData.sharedCurrency.gems += quest.reward.gemsReward;
                    Debug.Log($"[DailyQuest] Awarded {quest.reward.gemsReward} gems (fallback)");
                }

                persistentData.SaveCurrencyData();
            }
        }

        // ให้ EXP ผู้เล่น
        if (quest.reward.expReward > 0 && persistentData?.multiCharacterData != null)
        {
            var result = persistentData.multiCharacterData.GainPlayerExpFromStageCompletion("DailyQuest", quest.reward.expReward);
            if (result.didLevelUp)
            {
                Debug.Log($"[DailyQuest] Player leveled up to {result.newPlayerLevel}!");

                // เพิ่มรางวัลจาก level up ด้วย
                if (currencyManager != null)
                {
                    if (result.goldReward > 0)
                        currencyManager.AddGold(result.goldReward, true);
                    if (result.gemsReward > 0)
                        currencyManager.AddGems(result.gemsReward, true);
                }
            }
            Debug.Log($"[DailyQuest] Awarded {quest.reward.expReward} player EXP");
        }

        // ทำเครื่องหมายว่ารับรางวัลแล้ว
        quest.isRewardClaimed = true;
        quest.lastUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        Debug.Log($"[DailyQuest] Marked quest as claimed: {quest.questName}");
        Debug.Log($"[DailyQuest] Quest status - Completed: {quest.isCompleted}, Claimed: {quest.isRewardClaimed}");

        SaveQuestData();
        RefreshQuestUI();

        // Verify save
        Debug.Log($"[DailyQuest] After save - MultiCharacterData quest count: {persistentData?.multiCharacterData?.dailyQuests?.Count ?? 0}");

        // แสดงการแจ้งเตือนรางวัล (ถ้าต้องการ)
        ShowRewardNotification(quest);
    }

    void ShowRewardNotification(DailyQuestData quest)
    {
        string rewardText = "";

        if (quest.reward.goldReward > 0)
            rewardText += $"+{quest.reward.goldReward} Gold ";
        if (quest.reward.gemsReward > 0)
            rewardText += $"+{quest.reward.gemsReward} Gems ";
        if (quest.reward.expReward > 0)
            rewardText += $"+{quest.reward.expReward} EXP ";

        Debug.Log($"[DailyQuest] Reward Claimed: {rewardText}");

        // TODO: แสดง popup หรือ animation รางวัล
    }

    void RefreshQuestUI()
    {
        // ลบ quest items เก่า
        foreach (Transform child in questContainer)
        {
            Destroy(child.gameObject);
        }

        // สร้าง quest items ใหม่
        foreach (var quest in currentQuests)
        {
            CreateQuestItem(quest);
        }
    }

    void CreateQuestItem(DailyQuestData quest)
    {
        if (questItemPrefab == null) return;

        GameObject questItem = Instantiate(questItemPrefab, questContainer);
        var questItemUI = questItem.GetComponent<DailyQuestItemUI>();

        if (questItemUI != null)
        {
            questItemUI.SetupQuest(quest, this);
        }
    }

    void LoadQuestData()
    {
        Debug.Log("[DailyQuest] LoadQuestData called");

        if (persistentData?.multiCharacterData == null)
        {
            Debug.LogWarning("[DailyQuest] MultiCharacterPlayerData not available");
            currentQuests = new List<DailyQuestData>();
            lastQuestUpdate = DateTime.MinValue;
            return;
        }

        try
        {
            // โหลดจาก MultiCharacterPlayerData
            if (persistentData.multiCharacterData.dailyQuests != null && persistentData.multiCharacterData.dailyQuests.Count > 0)
            {
                currentQuests = new List<DailyQuestData>(persistentData.multiCharacterData.dailyQuests);

                if (DateTime.TryParse(persistentData.multiCharacterData.questLastUpdateTime, out DateTime savedDate))
                {
                    lastQuestUpdate = savedDate;
                }

                Debug.Log($"[DailyQuest] Loaded {currentQuests.Count} quests from MultiCharacterPlayerData");

                // Debug แต่ละ quest ที่โหลด
                foreach (var quest in currentQuests)
                {
                    Debug.Log($"[DailyQuest] Loaded quest from Firebase: {quest.questName} - Completed: {quest.isCompleted} - Claimed: {quest.isRewardClaimed} - Date: {quest.questDate:yyyy-MM-dd}");
                }
            }
            else
            {
                Debug.Log("[DailyQuest] No quest data found in MultiCharacterPlayerData");
                currentQuests = new List<DailyQuestData>();
                lastQuestUpdate = DateTime.MinValue;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DailyQuest] Error loading quest data: {e.Message}");
            currentQuests = new List<DailyQuestData>();
            lastQuestUpdate = DateTime.MinValue;
        }
    }

    void SaveQuestData()
    {
        if (persistentData?.multiCharacterData == null)
        {
            Debug.LogWarning("[DailyQuest] MultiCharacterPlayerData not available, cannot save");
            return;
        }

        try
        {
            // บันทึกลง MultiCharacterPlayerData
            persistentData.multiCharacterData.dailyQuests = new List<DailyQuestData>(currentQuests);
            persistentData.multiCharacterData.questLastUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            persistentData.multiCharacterData.UpdateDailyQuestDebugInfo();

            Debug.Log($"[DailyQuest] Saving {currentQuests.Count} quests to MultiCharacterPlayerData");

            // บันทึกลง Firebase ผ่าน PersistentPlayerData
            persistentData.SavePlayerDataAsync();

            // Validate การบันทึก
            var savedQuests = persistentData.multiCharacterData.dailyQuests;
            Debug.Log($"[DailyQuest] Validation - Saved {savedQuests?.Count ?? 0} quests");

            if (savedQuests != null && savedQuests.Count > 0)
            {
                var loginQuest = savedQuests.Find(q => q.questType == QuestType.DailyLogin);
                if (loginQuest != null)
                {
                    Debug.Log($"[DailyQuest] Login quest status after save - Completed: {loginQuest.isCompleted}, Claimed: {loginQuest.isRewardClaimed}");
                }
            }

            Debug.Log("[DailyQuest] Quest data saved to MultiCharacterPlayerData and Firebase");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DailyQuest] Error saving quest data: {e.Message}");
        }
    }

    void LoadDailyQuests()
    {
        // รอให้ PersistentPlayerData โหลดเสร็จก่อน
        if (persistentData?.isDataLoaded != true || persistentData?.multiCharacterData == null)
        {
            Debug.Log("[DailyQuest] Waiting for PersistentPlayerData to load...");
            Invoke("LoadDailyQuests", 2f); // เพิ่มเป็น 2 วินาที
            return;
        }

        Debug.Log("[DailyQuest] PersistentPlayerData is ready, loading quest data...");

        LoadQuestData();

        // ถ้าไม่มีข้อมูล quest หรือข้อมูลเก่า ให้สร้างใหม่
        if (currentQuests.Count == 0 || lastQuestUpdate.Date < DateTime.Now.Date)
        {
            Debug.Log("[DailyQuest] No valid quest data found, generating new quests");
            GenerateDailyQuests();
            lastQuestUpdate = DateTime.Now;
            SaveQuestData();
        }
        else
        {
            Debug.Log($"[DailyQuest] Loaded {currentQuests.Count} quests from saved data");

            // Debug แต่ละ quest
            foreach (var quest in currentQuests)
            {
                Debug.Log($"[DailyQuest] Loaded quest: {quest.questName} - Completed: {quest.isCompleted} - Claimed: {quest.isRewardClaimed}");
            }
        }
    }

    public System.Collections.IEnumerator WaitAndSavePersistentData()
    {
        yield return new WaitForSeconds(1f);
        persistentData.SavePlayerDataAsync();
        Debug.Log("[DailyQuest] Saved PersistentPlayerData after currency update");
    }

    void OpenDailyQuest()
    {
        dailyQuestPanel.SetActive(true);
        RefreshQuestUI();
    }

    public void CloseDailyQuest()
    {
        dailyQuestPanel.SetActive(false);
    }

    // Debug Methods
    [ContextMenu("Force Generate New Quests")]
    void ForceGenerateNewQuests()
    {
        GenerateDailyQuests();
        SaveQuestData();
        RefreshQuestUI();
        Debug.Log("[DailyQuest] Forced generation of new daily quests");
    }

    [ContextMenu("Complete All Quests")]
    void CompleteAllQuests()
    {
        foreach (var quest in currentQuests)
        {
            quest.currentProgress = quest.targetValue;
            quest.isCompleted = true;
        }
        SaveQuestData();
        RefreshQuestUI();
        Debug.Log("[DailyQuest] All quests marked as completed");
    }
}

[System.Serializable]
public class DailyQuestTemplate
{
    public string questId;
    public string questName;
    public string questDescription;
    public QuestType questType;
    public int targetValue;
    public QuestReward reward;
}

[System.Serializable]
public class DailyQuestDataWrapper
{
    public List<DailyQuestData> quests;
}

[System.Serializable]
public class DailyQuestFirebaseData
{
    public List<DailyQuestData> quests;
    public string lastUpdateTime;
    public int playerLevel; // เก็บ player level ตอนที่ save เพื่อ debug
    public int totalQuestsCompleted; // จำนวน quest ที่ทำเสร็จทั้งหมด

    public DailyQuestFirebaseData()
    {
        quests = new List<DailyQuestData>();
        lastUpdateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        playerLevel = 1;
        totalQuestsCompleted = 0;
    }
}