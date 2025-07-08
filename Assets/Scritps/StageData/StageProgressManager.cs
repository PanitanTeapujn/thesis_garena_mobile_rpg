using UnityEngine;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Database;
using System.Collections;

public class StageProgressManager : MonoBehaviour
{
    private static StageProgressManager _instance;
    public static StageProgressManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<StageProgressManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("StageProgressManager");
                    _instance = go.AddComponent<StageProgressManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    private StageProgressData StageProgress
    {
        get
        {
            if (PersistentPlayerData.Instance?.multiCharacterData?.stageProgress == null)
            {
                PersistentPlayerData.Instance.multiCharacterData.stageProgress = new StageProgressData();
            }
            return PersistentPlayerData.Instance.multiCharacterData.stageProgress;
        }
    }

    [Header("Firebase Integration")]
    public bool useFirebase = true;
    public bool autoSave = true;

    private StageProgressData stageProgress;
    private FirebaseAuth auth;
    private DatabaseReference databaseReference;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFirebase();
            LoadProgress();
            Debug.Log("[StageProgress] StageProgressManager initialized with Firebase");
        }
        else if (_instance != this)
        {
            Debug.Log("[StageProgress] Destroying duplicate StageProgressManager");
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    void InitializeFirebase()
    {
        if (useFirebase)
        {
            auth = FirebaseAuth.DefaultInstance;
            databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
        }
    }

    // ✅ เรียกจาก EnemyKillTracker เมื่อ Enemy ถูกกำจัด
    public static void AddEnemyKill(string stageName)
    {
        StageProgressManager instance = Instance;

        // ใช้ StageProgress property แทน
        instance.StageProgress.AddEnemyKill(stageName);

        instance.CheckStageCompletion(stageName);

        if (instance.autoSave)
        {
            // เปลี่ยนจาก SaveProgressToFirebase เป็น SaveThroughPersistentPlayerData
            instance.SaveThroughPersistentPlayerData();
        }
    }

    // ✅ เช็คว่าผ่าน substage แล้วหรือยัง
    private void CheckStageCompletion(string stageName)
    {
        int currentKills = GetEnemyKills(stageName);
        int requiredKills = EnemyKillTracker.GetRequiredKillsForStage(stageName);

        if (currentKills >= requiredKills && !IsStageCompleted(stageName))
        {
            CompleteStage(stageName);
        }
    }

    // ✅ ผ่านด่าน
    public static void CompleteStage(string stageName)
    {
        StageProgressManager instance = Instance;

        instance.StageProgress.CompleteStage(stageName);

        if (instance.autoSave)
        {
            instance.SaveThroughPersistentPlayerData();
        }
    }
    // ✅ เช็คว่าผ่านด่านแล้วหรือยัง
    public static bool IsStageCompleted(string stageName)
    {
        StageProgressManager instance = Instance;
        return instance.StageProgress?.IsStageCompleted(stageName) ?? false;
    }

    // ✅ ดึงจำนวน Enemy ที่กำจัดแล้ว
    // แก้ไข GetEnemyKills
    public static int GetEnemyKills(string stageName)
    {
        StageProgressManager instance = Instance;
        return instance.StageProgress?.GetEnemyKills(stageName) ?? 0;
    }
    private void SaveThroughPersistentPlayerData()
    {
        if (PersistentPlayerData.Instance?.multiCharacterData != null)
        {
            PersistentPlayerData.Instance.multiCharacterData.UpdateStageProgressDebugInfo();
            PersistentPlayerData.Instance.SavePlayerDataAsync();
            Debug.Log("💾 [StageProgress] Saved through PersistentPlayerData");
        }
    }

    // ========== Firebase Save/Load Methods ==========

    // ✅ โหลด Progress จาก Firebase
    public void LoadProgress()
    {
        if (PersistentPlayerData.Instance?.multiCharacterData?.stageProgress != null)
        {
            // ข้อมูลมีอยู่แล้วจาก PersistentPlayerData
            Debug.Log($"✅ [StageProgress] Loaded from PersistentPlayerData - {StageProgress.completedStages.Count} completed stages");
            StageProgress.LogProgress();
        }
        else
        {
            // สร้าง default progress
            if (PersistentPlayerData.Instance?.multiCharacterData != null)
            {
                PersistentPlayerData.Instance.multiCharacterData.stageProgress = new StageProgressData();
                Debug.Log("[StageProgress] Created default progress in PersistentPlayerData");
            }
        }
    }


    private IEnumerator LoadProgressFromFirebase()
    {
        Debug.Log("[StageProgress] Loading progress from Firebase...");

        var task = databaseReference.Child("players").Child(auth.CurrentUser.UserId).Child("stageProgress").GetValueAsync();

        float timeout = 5f;
        float elapsed = 0f;

        while (!task.IsCompleted && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (task.IsCompleted && task.Exception == null && task.Result.Exists)
        {
            try
            {
                string json = task.Result.GetRawJsonValue();
                stageProgress = JsonUtility.FromJson<StageProgressData>(json);

                if (stageProgress != null)
                {
                    Debug.Log($"✅ [StageProgress] Loaded from Firebase - {stageProgress.completedStages.Count} completed stages");
                    stageProgress.LogProgress();

                    // ✅ Sync กับ PlayerPrefs
                    SaveToPlayerPrefs();
                }
                else
                {
                    CreateDefaultProgress();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[StageProgress] Failed to parse Firebase data: {e.Message}");
                CreateDefaultProgress();
            }
        }
        else
        {
            Debug.Log("[StageProgress] No Firebase data found, creating default progress");
            CreateDefaultProgress();
        }
    }

    // ✅ บันทึก Progress ลง Firebase
    public void SaveProgressToFirebase()
    {
        SaveThroughPersistentPlayerData();
    }

    private IEnumerator SaveProgressToFirebaseCoroutine()
    {
        // ✅ อัปเดตเวลา
        stageProgress.lastUpdateDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        string json = JsonUtility.ToJson(stageProgress, true);

        var task = databaseReference.Child("players").Child(auth.CurrentUser.UserId).Child("stageProgress").SetRawJsonValueAsync(json);

        // ✅ รอไม่เกิน 2 วินาที
        float timeout = 2f;
        float elapsed = 0f;

        while (!task.IsCompleted && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (task.IsCompleted)
        {
            if (task.Exception != null)
            {
                Debug.LogError($"❌ [StageProgress] Failed to save to Firebase: {task.Exception.Message}");
            }
            else
            {
                Debug.Log($"💾 [StageProgress] Saved to Firebase successfully");
            }
        }

        // ✅ Sync กับ PlayerPrefs
        SaveToPlayerPrefs();
    }

    // ========== PlayerPrefs Methods (Backup) ==========

    private void LoadFromPlayerPrefs()
    {
        Debug.Log("[StageProgress] Loading from PlayerPrefs...");

        stageProgress = new StageProgressData();

        // โหลดด่านที่ผ่านแล้ว
        string completedStagesStr = PlayerPrefs.GetString("CompletedStages", "");
        if (!string.IsNullOrEmpty(completedStagesStr))
        {
            string[] stages = completedStagesStr.Split(',');
            foreach (string stage in stages)
            {
                if (!string.IsNullOrEmpty(stage))
                    stageProgress.completedStages.Add(stage);
            }
        }

        // โหลด Enemy kills ทุกตัว
        string[] allPossibleStages = {
            "PlayRoom1_1", "PlayRoom1_2", "PlayRoom1_3",
            "PlayRoom2_1", "PlayRoom2_2", "PlayRoom2_3",
            "PlayRoom3_1", "PlayRoom3_2", "PlayRoom3_3"
        };

        foreach (string stage in allPossibleStages)
        {
            int kills = PlayerPrefs.GetInt($"EnemyKills_{stage}", 0);
            if (kills > 0)
            {
                stageProgress.stageEnemyKills[stage] = kills;
            }
        }

        stageProgress.lastPlayedStage = PlayerPrefs.GetString("LastPlayedStage", "");

        Debug.Log($"✅ [StageProgress] Loaded from PlayerPrefs - {stageProgress.completedStages.Count} completed stages");
    }

    private void SaveToPlayerPrefs()
    {
        if (stageProgress == null) return;

        // บันทึก Enemy kills
        foreach (var kvp in stageProgress.stageEnemyKills)
        {
            PlayerPrefs.SetInt($"EnemyKills_{kvp.Key}", kvp.Value);
        }

        // บันทึกด่านที่ผ่านแล้ว
        string completedStagesStr = string.Join(",", stageProgress.completedStages);
        PlayerPrefs.SetString("CompletedStages", completedStagesStr);
        PlayerPrefs.SetString("LastPlayedStage", stageProgress.lastPlayedStage);

        PlayerPrefs.Save();

        Debug.Log($"💾 [StageProgress] Synced to PlayerPrefs");
    }

    private void CreateDefaultProgress()
    {
        stageProgress = new StageProgressData();
        Debug.Log("[StageProgress] Created default progress");
    }

    // ========== Public Methods ==========

    // ✅ บังคับ Save ทันที
    public static void ForceSave()
    {
        Instance.SaveThroughPersistentPlayerData();
    }

    // ✅ รีเซ็ต Progress
    [ContextMenu("Reset All Progress")]
    public void ResetProgress()
    {
        if (stageProgress != null)
            stageProgress.ResetProgress();

        // ลบ PlayerPrefs
        string[] allPossibleStages = {
            "PlayRoom1_1", "PlayRoom1_2", "PlayRoom1_3",
            "PlayRoom2_1", "PlayRoom2_2", "PlayRoom2_3",
            "PlayRoom3_1", "PlayRoom3_2", "PlayRoom3_3"
        };

        foreach (string stage in allPossibleStages)
        {
            PlayerPrefs.DeleteKey($"EnemyKills_{stage}");
        }
        PlayerPrefs.DeleteKey("CompletedStages");
        PlayerPrefs.DeleteKey("LastPlayedStage");
        PlayerPrefs.Save();

        // ลบใน Firebase
        if (useFirebase)
        {
            SaveProgressToFirebase();
        }

        Debug.Log("🔄 [StageProgress] All progress reset!");
    }

    // ✅ แสดง Progress ปัจจุบัน
    [ContextMenu("Show Current Progress")]
    public void ShowCurrentProgress()
    {
        if (stageProgress != null)
        {
            stageProgress.LogProgress();
        }
        else
        {
            Debug.Log("[StageProgress] No progress data available");
        }
    }

    // ✅ เปิด/ปิด Auto Save
    public void SetAutoSave(bool enabled)
    {
        autoSave = enabled;
        Debug.Log($"[StageProgress] Auto Save: {(enabled ? "Enabled" : "Disabled")}");
    }

    // ✅ เปิด/ปิด Firebase
    public void SetUseFirebase(bool enabled)
    {
        useFirebase = enabled;
        Debug.Log($"[StageProgress] Firebase: {(enabled ? "Enabled" : "Disabled")}");
    }
}