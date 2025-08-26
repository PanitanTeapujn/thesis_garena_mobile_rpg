// แก้ไข DailyQuestTracker.cs
using System;

public static class DailyQuestTracker
{
    private static int sessionEnemiesKilled = 0;
    private static int sessionStagesCompleted = 0;

    public static event Action<int> OnEnemiesKilledUpdate;
    public static event Action<int> OnStagesCompletedUpdate;

    public static void AddEnemyKill()
    {
        sessionEnemiesKilled++;

        // เพิ่ม: อัปเดต Firebase ทันทีผ่าน PersistentPlayerData
        var persistentData = PersistentPlayerData.Instance;
        if (persistentData != null)
        {
            persistentData.UpdateDailyQuestProgress(QuestType.KillEnemies, 1);
        }
    }

    public static void AddStageCompletion()
    {
        sessionStagesCompleted++;

        // เพิ่ม: อัปเดต Firebase ทันทีผ่าน PersistentPlayerData
        var persistentData = PersistentPlayerData.Instance;
        if (persistentData != null)
        {
            persistentData.UpdateDailyQuestProgress(QuestType.CompleteStages, 1);
        }
    }

    // เก็บ method เดิมไว้สำหรับ bulk update เมื่อกลับ lobby
    public static void FlushProgressToQuests()
    {
        if (sessionEnemiesKilled > 0)
        {
            OnEnemiesKilledUpdate?.Invoke(sessionEnemiesKilled);
        }

        if (sessionStagesCompleted > 0)
        {
            OnStagesCompletedUpdate?.Invoke(sessionStagesCompleted);
        }

        ResetSession();
    }

    public static void ResetSession()
    {
        sessionEnemiesKilled = 0;
        sessionStagesCompleted = 0;
    }
}