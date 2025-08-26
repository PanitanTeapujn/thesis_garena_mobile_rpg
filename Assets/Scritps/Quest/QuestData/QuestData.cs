using UnityEngine;
using System.Collections.Generic;

// Enums สำหรับระบบ Quest
public enum QuestType
{
    MainQuest,
    SideQuest
}

public enum QuestStatus
{
    Available,    // พร้อมรับได้
    Accepted,     // รับแล้ว
    Completed,    // ทำเสร็จแล้ว (รอรับรางวัล)
    Finished,     // รับรางวัลแล้ว (เควสจบ)
    Locked        // ยังไม่สามารถรับได้
}

public enum QuestObjectiveType
{
    KillMonster,   // ฆ่ามอนสเตอร์
    CollectItem,   // เก็บไอเทม
    DeliverItem    // ส่งไอเทม
}

public enum RewardType
{
    Gold,
    Gems,
    PlayerExp,
    Item
}

// Objective สำหรับเควส
[System.Serializable]
public class QuestObjective
{
    [Header("Objective Settings")]
    public QuestObjectiveType objectiveType;
    public string description;

    [Header("Kill Monster Objective")]
    public string monsterID;  // ID ของมอนสเตอร์ที่ต้องฆ่า
    public int killCount;     // จำนวนที่ต้องฆ่า

    [Header("Collect/Deliver Item Objective")]
    public ItemData requiredItem;  // ไอเทมที่ต้องเก็บ/ส่ง
    public int requiredQuantity;   // จำนวนที่ต้องการ

    [Header("Progress Tracking")]
    public int currentProgress = 0;  // ความคืบหน้าปัจจุบัน

    public bool IsCompleted()
    {
        switch (objectiveType)
        {
            case QuestObjectiveType.KillMonster:
                return currentProgress >= killCount;
            case QuestObjectiveType.CollectItem:
            case QuestObjectiveType.DeliverItem:
                return currentProgress >= requiredQuantity;
            default:
                return false;
        }
    }

    public string GetProgressText()
    {
        switch (objectiveType)
        {
            case QuestObjectiveType.KillMonster:
                return $"{currentProgress}/{killCount}";
            case QuestObjectiveType.CollectItem:
            case QuestObjectiveType.DeliverItem:
                return $"{currentProgress}/{requiredQuantity}";
            default:
                return "0/0";
        }
    }

    public float GetProgressPercentage()
    {
        switch (objectiveType)
        {
            case QuestObjectiveType.KillMonster:
                return killCount > 0 ? (float)currentProgress / killCount : 0f;
            case QuestObjectiveType.CollectItem:
            case QuestObjectiveType.DeliverItem:
                return requiredQuantity > 0 ? (float)currentProgress / requiredQuantity : 0f;
            default:
                return 0f;
        }
    }
}

// รางวัลของเควส
[System.Serializable]
public class QuestReward
{
    public RewardType rewardType;
    public long goldAmount;      // สำหรับ Gold
    public int gemAmount;        // สำหรับ Gems
    public long playerExpAmount; // สำหรับ Player EXP
    public ItemData rewardItem;  // สำหรับ Item
    public int itemQuantity = 1; // จำนวนไอเทม

    public bool IsValid()
    {
        switch (rewardType)
        {
            case RewardType.Gold:
                return goldAmount > 0;
            case RewardType.Gems:
                return gemAmount > 0;
            case RewardType.PlayerExp:
                return playerExpAmount > 0;
            case RewardType.Item:
                return rewardItem != null && itemQuantity > 0;
            default:
                return false;
        }
    }

    public string GetRewardText()
    {
        switch (rewardType)
        {
            case RewardType.Gold:
                return $"{goldAmount:N0} Gold";
            case RewardType.Gems:
                return $"{gemAmount:N0} Gems";
            case RewardType.PlayerExp:
                return $"{playerExpAmount:N0} EXP";
            case RewardType.Item:
                return $"{rewardItem.ItemName} x{itemQuantity}";
            default:
                return "Unknown Reward";
        }
    }
}

// ScriptableObject หลักสำหรับ Quest
[CreateAssetMenu(fileName = "New Quest", menuName = "Quest System/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("Basic Quest Info")]
    public string questID;           // ID ของเควส
    public string questName;         // ชื่อเควส
    public QuestType questType;      // ประเภทเควส (Main/Side)

    [Header("Requirements")]
    public int requiredPlayerLevel = 1;  // เลเวลผู้เล่นที่ต้องการ
    public List<string> prerequisiteQuestIDs = new List<string>(); // เควสที่ต้องทำก่อน (สำหรับ Main Quest)

    [Header("Quest Description")]
    [TextArea(3, 5)]
    public string questDescription;      // คำอธิบายเควสสั้น

    [TextArea(5, 10)]
    public string questDetailDescription; // คำอธิบายเควสแบบละเอียด

    [Header("Quest Objectives")]
    public List<QuestObjective> objectives = new List<QuestObjective>();

    [Header("Quest Rewards")]
    public List<QuestReward> rewards = new List<QuestReward>();

    [Header("Quest Status")]
    public QuestStatus initialStatus = QuestStatus.Available;

    // Helper Methods
    public bool CanPlayerAcceptQuest(int playerLevel, List<string> completedQuestIDs)
    {
        // ตรวจสอบเลเวลผู้เล่น
        if (playerLevel < requiredPlayerLevel)
            return false;

        // ตรวจสอบเควสที่ต้องทำก่อน (สำหรับ Main Quest)
        if (questType == QuestType.MainQuest && prerequisiteQuestIDs.Count > 0)
        {
            foreach (string prereqID in prerequisiteQuestIDs)
            {
                if (!completedQuestIDs.Contains(prereqID))
                    return false;
            }
        }

        return true;
    }

    public bool AreAllObjectivesCompleted()
    {
        foreach (var objective in objectives)
        {
            if (!objective.IsCompleted())
                return false;
        }
        return true;
    }

    public string GetQuestTypeText()
    {
        return questType == QuestType.MainQuest ? "Main Quest" : "Side Quest";
    }

    public Color GetQuestTypeColor()
    {
        return questType == QuestType.MainQuest ? Color.yellow : Color.cyan;
    }

    public string GetAllRewardsText()
    {
        List<string> rewardTexts = new List<string>();
        foreach (var reward in rewards)
        {
            if (reward.IsValid())
                rewardTexts.Add(reward.GetRewardText());
        }
        return string.Join(", ", rewardTexts);
    }

    public float GetOverallProgressPercentage()
    {
        if (objectives.Count == 0) return 0f;

        float totalProgress = 0f;
        foreach (var objective in objectives)
        {
            totalProgress += objective.GetProgressPercentage();
        }

        return totalProgress / objectives.Count;
    }
}

// Runtime Quest Instance (สำหรับเก็บข้อมูลเควสที่ผู้เล่นกำลังทำ)
[System.Serializable]
public class RuntimeQuest
{
    public QuestData questData;
    public QuestStatus currentStatus;
    public System.DateTime acceptedTime;
    public List<QuestObjective> runtimeObjectives;

    public RuntimeQuest(QuestData data)
    {
        questData = data;
        currentStatus = data.initialStatus;
        acceptedTime = System.DateTime.Now;

        // Copy objectives เพื่อให้ track progress ได้
        runtimeObjectives = new List<QuestObjective>();
        foreach (var obj in data.objectives)
        {
            runtimeObjectives.Add(new QuestObjective
            {
                objectiveType = obj.objectiveType,
                description = obj.description,
                monsterID = obj.monsterID,
                killCount = obj.killCount,
                requiredItem = obj.requiredItem,
                requiredQuantity = obj.requiredQuantity,
                currentProgress = 0
            });
        }
    }

    public bool IsReadyForReward()
    {
        if (currentStatus != QuestStatus.Accepted) return false;

        foreach (var objective in runtimeObjectives)
        {
            if (!objective.IsCompleted())
                return false;
        }

        return true;
    }

    public void UpdateObjectiveProgress(QuestObjectiveType objectiveType, string targetID, int amount = 1)
    {
        foreach (var objective in runtimeObjectives)
        {
            if (objective.objectiveType == objectiveType)
            {
                switch (objectiveType)
                {
                    case QuestObjectiveType.KillMonster:
                        if (objective.monsterID == targetID)
                        {
                            objective.currentProgress = Mathf.Min(objective.currentProgress + amount, objective.killCount);
                        }
                        break;
                    case QuestObjectiveType.CollectItem:
                    case QuestObjectiveType.DeliverItem:
                        if (objective.requiredItem.ItemId == targetID)
                        {
                            objective.currentProgress = Mathf.Min(objective.currentProgress + amount, objective.requiredQuantity);
                        }
                        break;
                }
            }
        }
    }
}

// Quest Database สำหรับจัดการเควสทั้งหมด
[CreateAssetMenu(fileName = "Quest Database", menuName = "Quest System/Quest Database")]
public class QuestDatabase : ScriptableObject
{
    [Header("All Quests")]
    public List<QuestData> allQuests = new List<QuestData>();

    [Header("Main Quest Chain")]
    [Tooltip("เรียงลำดับ Main Quest ตามลำดับที่ต้องทำ")]
    public List<QuestData> mainQuestChain = new List<QuestData>();

    [Header("Side Quests by Level")]
    [Tooltip("Side Quest แยกตาม Level ที่สามารถรับได้")]
    public List<QuestsByLevel> sideQuestsByLevel = new List<QuestsByLevel>();

    // Singleton pattern
    private static QuestDatabase instance;
    public static QuestDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<QuestDatabase>("QuestDatabase");
                if (instance == null)
                {
                    Debug.LogError("[QuestDatabase] QuestDatabase not found in Resources folder!");
                }
            }
            return instance;
        }
    }

    // Helper Methods
    public QuestData GetQuestByID(string questID)
    {
        foreach (var quest in allQuests)
        {
            if (quest.questID == questID)
                return quest;
        }
        return null;
    }

    public List<QuestData> GetAvailableMainQuests(List<string> completedQuestIDs)
    {
        List<QuestData> availableQuests = new List<QuestData>();

        foreach (var quest in mainQuestChain)
        {
            // ตรวจสอบว่าเควสนี้ทำเสร็จแล้วหรือยัง
            if (completedQuestIDs.Contains(quest.questID))
                continue;

            // ตรวจสอบ prerequisite
            bool canAccept = true;
            foreach (string prereqID in quest.prerequisiteQuestIDs)
            {
                if (!completedQuestIDs.Contains(prereqID))
                {
                    canAccept = false;
                    break;
                }
            }

            if (canAccept)
                availableQuests.Add(quest);
        }

        return availableQuests;
    }

    public List<QuestData> GetAvailableSideQuests(int playerLevel, List<string> completedQuestIDs)
    {
        List<QuestData> availableQuests = new List<QuestData>();

        foreach (var levelGroup in sideQuestsByLevel)
        {
            if (playerLevel >= levelGroup.requiredLevel)
            {
                foreach (var quest in levelGroup.quests)
                {
                    // ตรวจสอบว่าทำเสร็จแล้วหรือยัง
                    if (!completedQuestIDs.Contains(quest.questID))
                    {
                        availableQuests.Add(quest);
                    }
                }
            }
        }

        return availableQuests;
    }

    public List<QuestData> GetAllAvailableQuests(int playerLevel, List<string> completedQuestIDs)
    {
        List<QuestData> allAvailable = new List<QuestData>();

        // เพิ่ม Main Quests ก่อน
        allAvailable.AddRange(GetAvailableMainQuests(completedQuestIDs));

        // ตามด้วย Side Quests
        allAvailable.AddRange(GetAvailableSideQuests(playerLevel, completedQuestIDs));

        return allAvailable;
    }
}

// Helper class สำหรับจัดกลุ่ม Side Quest ตาม Level
[System.Serializable]
public class QuestsByLevel
{
    public int requiredLevel;
    public List<QuestData> quests = new List<QuestData>();
}