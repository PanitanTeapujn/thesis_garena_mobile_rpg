using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyKillTracker : MonoBehaviour
{
    // ✅ เรียกเมื่อ Enemy ถูกกำจัด
    public static void OnEnemyKilled()
    {
        string currentStageName = SceneManager.GetActiveScene().name;

        Debug.Log($"🎯 [EnemyKillTracker] Enemy killed in {currentStageName}");

        // ✅ เพิ่ม kill count
        StageProgressManager.AddEnemyKill(currentStageName);
    }

    // ✅ ทำให้เป็น public method สำหรับใช้ใน StageProgressManager
    public static int GetRequiredKillsForStage(string stageName)
    {
        // ตัวอย่างการกำหนดเงื่อนไข
        switch (stageName)
        {
            case "PlayRoom1_1": return 10;
            case "PlayRoom1_2": return 15;
            case "PlayRoom1_3": return 20;
            case "PlayRoom2_1": return 25;
            case "PlayRoom2_2": return 30;
            case "PlayRoom2_3": return 35;
            case "PlayRoom3_1": return 40;
            case "PlayRoom3_2": return 45;
            case "PlayRoom3_3": return 50;
            default: return 10;
        }
    }

    // ✅ เพิ่ม method สำหรับดู progress ในเกม
    [ContextMenu("Show Stage Progress")]
    public void ShowStageProgress()
    {
        string currentStage = SceneManager.GetActiveScene().name;
        int currentKills = StageProgressManager.GetEnemyKills(currentStage);
        int requiredKills = GetRequiredKillsForStage(currentStage);
        bool isCompleted = StageProgressManager.IsStageCompleted(currentStage);

        Debug.Log($"📊 Stage Progress for {currentStage}:");
        Debug.Log($"   Kills: {currentKills}/{requiredKills}");
        Debug.Log($"   Status: {(isCompleted ? "✅ COMPLETED" : "❌ Not completed")}");
    }
}