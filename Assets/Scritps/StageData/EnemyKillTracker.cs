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
        Debug.Log($"🔍 [EnemyKillTracker] Checking required kills for: '{stageName}'");

        // 🆕 แปลงเป็น lowercase เพื่อหลีกเลี่ยงปัญหา case sensitive
        string normalizedStageName = stageName.ToLower();
        string playerPrefsKey = $"RequiredKills_{normalizedStageName}";
        int savedRequiredKills = PlayerPrefs.GetInt(playerPrefsKey, -1);

        Debug.Log($"🔍 [EnemyKillTracker] Normalized stage: '{normalizedStageName}'");
        Debug.Log($"🔍 [EnemyKillTracker] PlayerPrefs key: '{playerPrefsKey}'");
        Debug.Log($"🔍 [EnemyKillTracker] PlayerPrefs value: {savedRequiredKills}");

        if (savedRequiredKills > 0)
        {
            Debug.Log($"✅ [EnemyKillTracker] Using saved value: {savedRequiredKills}");
            return savedRequiredKills;
        }

        // ถ้าไม่มีข้อมูลใน PlayerPrefs ให้ใช้ค่า default (ใช้ normalized name)
        Debug.LogWarning($"⚠️ [EnemyKillTracker] No valid saved data, using default for: '{normalizedStageName}'");

        int defaultValue;
        switch (normalizedStageName)
        {
            case "playroom1_1": defaultValue = 10; break;
            case "playroom1_2": defaultValue = 15; break;
            case "playroom1_3": defaultValue = 20; break;
            case "playroom2_1": defaultValue = 25; break;
            case "playroom2_2": defaultValue = 30; break;
            case "playroom2_3": defaultValue = 35; break;
            case "playroom3_1": defaultValue = 40; break;
            case "playroom3_2": defaultValue = 45; break;
            case "playroom3_3": defaultValue = 50; break;
            // 🆝 เพิ่มกรณีพิเศษ
            case "playroom1": defaultValue = 20; break; // สำหรับ scene ที่ชื่อแค่ PlayRoom1
            default: defaultValue = 10; break;
        }

        Debug.Log($"🔄 [EnemyKillTracker] Using default value: {defaultValue}");
        return defaultValue;
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