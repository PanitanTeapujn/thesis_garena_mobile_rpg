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

            // 🆕 Subscribe to scene loading event
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
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
    private void OnDestroy()
    {
        // Unsubscribe from scene loading event
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    /// <summary>
    /// เริ่มการติดตามสำหรับด่านใหม่
    /// </summary>
    public void StartStageTracking(string stageName = "")
    {
        // 🆕 บังคับ reset ทุกครั้งเมื่อเริ่มด่านใหม่
        currentStageRewards = new StageRewardData(); // สร้างใหม่แทนการ Reset()

        stageStartTime = Time.time;
        isTrackingStage = true;

        if (!string.IsNullOrEmpty(stageName))
        {
            currentStageRewards.SetStageName(stageName);
        }
        else
        {
            currentStageRewards.SetStageName(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        if (showDebugLogs)
        {
            Debug.Log($"[StageRewardTracker] 🎯 Started tracking rewards for: {currentStageRewards.stageName}");
            Debug.Log($"[StageRewardTracker] 🔄 Reset - Gold: {currentStageRewards.totalGoldEarned}, Items: {currentStageRewards.itemsEarned.Count}");
        }
    }

    public static void ForceResetRewards()
    {
        if (Instance != null)
        {
            Instance.currentStageRewards = new StageRewardData();
            Instance.stageStartTime = Time.time;
            Instance.isTrackingStage = false;

            if (Instance.showDebugLogs)
                Debug.Log("[StageRewardTracker] 🧹 Forced reset of all rewards");
        }
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
            }
        }
    }
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // ถ้าเป็นการโหลด scene ใหม่ที่ไม่ใช่ Lobby หรือ Menu ให้ reset
        if (scene.name != "Lobby" && scene.name != "MainMenu" && !scene.name.Contains("Menu"))
        {
            if (showDebugLogs)
                Debug.Log($"[StageRewardTracker] 🔄 Scene changed to: {scene.name} - Resetting rewards");

            StartStageTracking(scene.name);
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
   

    #region Debug Methods
   
   
    #endregion
}