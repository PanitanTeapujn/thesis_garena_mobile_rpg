using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

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
        if (completedStages == null)
        {
            Debug.LogError("[StageProgressData] completedStages list is NULL!");
            completedStages = new List<string>();
            return false;
        }

        // ✅ ทำ case-insensitive comparison เพราะอาจมีปัญหาตัวพิมพ์
        bool found = completedStages.Any(s => s.Equals(stageName, System.StringComparison.OrdinalIgnoreCase));

        Debug.Log($"[StageProgressData.IsStageCompleted] Checking '{stageName}': {found}");
        Debug.Log($"[StageProgressData.IsStageCompleted] Total completed: {completedStages.Count}");

        return found;
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
    
}