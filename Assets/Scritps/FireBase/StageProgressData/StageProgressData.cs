using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public class StageProgressData
{
    [Header("Stage Progress")]
    public Dictionary<string, int> stageEnemyKills = new Dictionary<string, int>();
    public List<string> completedStages = new List<string>();
    public string lastPlayedStage = "";
    public string lastUpdateDate = "";

    public StageProgressData()
    {
        stageEnemyKills = new Dictionary<string, int>();
        completedStages = new List<string>();
        lastPlayedStage = "";
        lastUpdateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    // ✅ เพิ่ม Enemy Kill
    public void AddEnemyKill(string stageName)
    {
        if (!stageEnemyKills.ContainsKey(stageName))
            stageEnemyKills[stageName] = 0;

        stageEnemyKills[stageName]++;
        lastPlayedStage = stageName;
        lastUpdateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

      /*  Debug.Log($"✅ [StageProgress] {stageName}: {stageEnemyKills[stageName]} enemies killed");*/
    }

    // ✅ ผ่านด่าน
    public void CompleteStage(string stageName)
    {
        if (!completedStages.Contains(stageName))
        {
            completedStages.Add(stageName);
            lastUpdateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Debug.Log($"🎉 [StageProgress] Stage {stageName} COMPLETED!");
        }
    }

    // ✅ เช็คว่าผ่านด่านแล้วหรือยัง
    public bool IsStageCompleted(string stageName)
    {
        return completedStages.Contains(stageName);
    }

    // ✅ ดึงจำนวน Enemy ที่กำจัดแล้ว
    public int GetEnemyKills(string stageName)
    {
        return stageEnemyKills.ContainsKey(stageName) ? stageEnemyKills[stageName] : 0;
    }

    // ✅ รีเซ็ต Progress
    public void ResetProgress()
    {
        stageEnemyKills.Clear();
        completedStages.Clear();
        lastPlayedStage = "";
        lastUpdateDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    // ✅ Debug method
    public void LogProgress()
    {
        Debug.Log("=== STAGE PROGRESS ===");
        Debug.Log($"Enemy Kills: {stageEnemyKills.Count} stages");
        foreach (var kvp in stageEnemyKills)
        {
            Debug.Log($"  {kvp.Key}: {kvp.Value} kills");
        }
        Debug.Log($"Completed Stages: {completedStages.Count}");
        foreach (string stage in completedStages)
        {
            Debug.Log($"  ✓ {stage}");
        }
        Debug.Log($"Last Played: {lastPlayedStage}");
        Debug.Log($"Last Update: {lastUpdateDate}");
        Debug.Log("====================");
    }
}