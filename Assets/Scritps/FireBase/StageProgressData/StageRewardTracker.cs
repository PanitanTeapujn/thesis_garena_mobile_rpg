using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// เก็บสถิติ rewards ที่ได้รับระหว่างเล่นด่าน
/// </summary>
public class StageRewardTracker : MonoBehaviour
{
    #region Singleton
    private static StageRewardTracker _instance;
    public static StageRewardTracker Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<StageRewardTracker>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("StageRewardTracker");
                    _instance = go.AddComponent<StageRewardTracker>();
                }
            }
            return _instance;
        }
    }
    #endregion

    [Header("🏆 Current Stage Rewards")]
    public StageRewardData currentStageRewards = new StageRewardData();

    [Header("⏱️ Stage Timer")]
    private float stageStartTime = 0f;
    private bool isTrackingStage = false;

    [Header("🔧 Debug")]
    public bool showDebugLogs = true;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartStageTracking();
    }

    /// <summary>
    /// เริ่มการติดตามสำหรับด่านใหม่
    /// </summary>
    public void StartStageTracking(string stageName = "")
    {
        currentStageRewards.Reset();
        stageStartTime = Time.time;
        isTrackingStage = true;

        if (!string.IsNullOrEmpty(stageName))
        {
            currentStageRewards.SetStageName(stageName);
        }
        else
        {
            // ใช้ชื่อ scene ปัจจุบัน
            currentStageRewards.SetStageName(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        if (showDebugLogs)
            Debug.Log($"[StageRewardTracker] 🎯 Started tracking rewards for: {currentStageRewards.stageName}");
    }

    /// <summary>
    /// หยุดการติดตามและคำนวณเวลาที่ใช้
    /// </summary>
    public void StopStageTracking()
    {
        if (isTrackingStage)
        {
            float completionTime = Time.time - stageStartTime;
            currentStageRewards.SetCompletionTime(completionTime);
            isTrackingStage = false;

            if (showDebugLogs)
            {
                Debug.Log($"[StageRewardTracker] ⏹️ Stopped tracking. Completion time: {completionTime:F1}s");
                LogCurrentRewards();
            }
        }
    }

    /// <summary>
    /// เพิ่มเงินที่ได้รับ
    /// </summary>
    public static void AddGoldReward(long amount)
    {
        if (Instance.isTrackingStage)
        {
            Instance.currentStageRewards.AddGold(amount);

            if (Instance.showDebugLogs)
                Debug.Log($"[StageRewardTracker] 💰 Added {amount:N0} gold (Total: {Instance.currentStageRewards.totalGoldEarned:N0})");
        }
    }

    /// <summary>
    /// เพิ่มเพชรที่ได้รับ
    /// </summary>
    public static void AddGemsReward(int amount)
    {
        if (Instance.isTrackingStage)
        {
            Instance.currentStageRewards.AddGems(amount);

            if (Instance.showDebugLogs)
                Debug.Log($"[StageRewardTracker] 💎 Added {amount} gems (Total: {Instance.currentStageRewards.totalGemsEarned})");
        }
    }

    /// <summary>
    /// เพิ่มไอเทมที่ได้รับ
    /// </summary>
    public static void AddItemReward(ItemData itemData, int quantity = 1)
    {
        if (Instance.isTrackingStage && itemData != null)
        {
            Instance.currentStageRewards.AddItem(itemData, quantity);

            if (Instance.showDebugLogs)
            {
                string quantityText = quantity > 1 ? $" x{quantity}" : "";
                Debug.Log($"[StageRewardTracker] 🎁 Added {itemData.ItemName}{quantityText} ({itemData.GetTierText()})");
            }
        }
    }

    /// <summary>
    /// เพิ่มจำนวน enemy ที่ฆ่า
    /// </summary>
    public static void AddEnemyKill()
    {
        if (Instance.isTrackingStage)
        {
            Instance.currentStageRewards.AddEnemyKill();
        }
    }

    /// <summary>
    /// ดึงข้อมูล rewards ปัจจุบัน
    /// </summary>
    public static StageRewardData GetCurrentRewards()
    {
        return Instance.currentStageRewards;
    }

    /// <summary>
    /// ตั้งค่าจำนวน enemy ที่ถูกต้อง (เรียกจาก EnemySpawner)
    /// </summary>
    public void SetCorrectEnemyCount(int correctCount)
    {
        currentStageRewards.totalEnemiesKilled = correctCount;

        if (showDebugLogs)
            Debug.Log($"[StageRewardTracker] 🔧 Corrected enemy count to: {correctCount}");
    }

    /// <summary>
    /// ดึงจำนวน enemy ที่ฆ่าปัจจุบัน
    /// </summary>
    public static int GetCurrentEnemyKillCount()
    {
        return Instance.currentStageRewards.totalEnemiesKilled;
    }

    /// <summary>
    /// แสดงสถิติ rewards ปัจจุบัน
    /// </summary>
    private void LogCurrentRewards()
    {
        Debug.Log("=== STAGE REWARDS SUMMARY ===");
        Debug.Log($"Stage: {currentStageRewards.stageName}");
        Debug.Log($"💰 Gold: {currentStageRewards.totalGoldEarned:N0}");
        Debug.Log($"💎 Gems: {currentStageRewards.totalGemsEarned}");
        Debug.Log($"🎁 Items: {currentStageRewards.itemsEarned.Count}");
        Debug.Log($"⚔️ Enemies killed: {currentStageRewards.totalEnemiesKilled}");
        Debug.Log($"⏱️ Time: {currentStageRewards.stageCompletionTime:F1}s");

        if (currentStageRewards.itemsEarned.Count > 0)
        {
            Debug.Log("Items earned:");
            foreach (var item in currentStageRewards.itemsEarned)
            {
                string quantityText = item.quantity > 1 ? $" x{item.quantity}" : "";
                Debug.Log($"  - {item.itemName}{quantityText} ({item.GetTierText()})");
            }
        }
        Debug.Log("=============================");
    }

    #region Debug Methods
    [ContextMenu("🧪 Test: Add Sample Rewards")]
    public void TestAddSampleRewards()
    {
        if (Application.isPlaying)
        {
            AddGoldReward(1500);
            AddGemsReward(25);

            // จำลอง item rewards (ต้องมี ItemDatabase)
            Debug.Log("🧪 Added sample rewards for testing");
            LogCurrentRewards();
        }
    }

    [ContextMenu("🧪 Test: Reset Rewards")]
    public void TestResetRewards()
    {
        if (Application.isPlaying)
        {
            currentStageRewards.Reset();
            Debug.Log("🧪 Reset all rewards");
        }
    }

    [ContextMenu("📊 Show Current Rewards")]
    public void ShowCurrentRewards()
    {
        if (Application.isPlaying)
        {
            LogCurrentRewards();
        }
    }

    [ContextMenu("🔧 Debug: Compare EnemySpawner Count")]
    public void DebugCompareEnemyCount()
    {
        if (Application.isPlaying)
        {
            EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
            if (spawner != null)
            {
                Debug.Log("=== ENEMY COUNT COMPARISON ===");
                Debug.Log($"🎯 EnemySpawner.currentSessionKills: {spawner.currentSessionKills}");
                Debug.Log($"📊 StageRewardTracker.totalEnemiesKilled: {currentStageRewards.totalEnemiesKilled}");
                Debug.Log($"🎯 Required kills for stage: {spawner.GetRequiredKills()}");
                Debug.Log($"✅ Stage completed: {spawner.IsCurrentStageCompleted()}");
                Debug.Log("==============================");
            }
            else
            {
                Debug.LogWarning("🔍 No EnemySpawner found in scene!");
            }
        }
    }

    [ContextMenu("🔧 Force Sync Enemy Count")]
    public void ForceSyncEnemyCount()
    {
        if (Application.isPlaying)
        {
            EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
            if (spawner != null)
            {
                SetCorrectEnemyCount(spawner.currentSessionKills);
                Debug.Log($"🔧 Force synced enemy count to: {spawner.currentSessionKills}");
            }
        }
    }
    #endregion
}