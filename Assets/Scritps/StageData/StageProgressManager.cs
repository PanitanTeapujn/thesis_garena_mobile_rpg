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
                Debug.LogError("[StageProgress] ❌ stageProgress is NULL in PersistentPlayerData!");

                if (PersistentPlayerData.Instance?.multiCharacterData != null)
                {
                    PersistentPlayerData.Instance.multiCharacterData.stageProgress = new StageProgressData();
                    Debug.Log("[StageProgress] Created new StageProgressData");
                }
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

        if (instance.StageProgress == null)
        {
            Debug.LogError($"[IsStageCompleted] StageProgress is NULL for {stageName}");
            return false;
        }

        bool isCompleted = instance.StageProgress.IsStageCompleted(stageName);

        // ✅ Debug เพื่อดูว่าเช็คถูกต้องหรือไม่
        Debug.Log($"[IsStageCompleted] Checking '{stageName}': {isCompleted}");

        if (instance.StageProgress.completedStages.Count > 0)
        {
            Debug.Log($"[IsStageCompleted] Available completed stages:");
            foreach (var stage in instance.StageProgress.completedStages)
            {
                Debug.Log($"  - '{stage}'");
            }
        }

        return isCompleted;
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
            // ✅ ใช้ข้อมูลจาก PersistentPlayerData โดยตรง (ไม่สร้างใหม่)
            Debug.Log($"✅ [StageProgress] Loaded from PersistentPlayerData");

            var stageProgressData = PersistentPlayerData.Instance.multiCharacterData.stageProgress;

            Debug.Log($"   Completed stages count: {stageProgressData.completedStages.Count}");

            // แสดงรายการด่านที่ผ่านแล้ว
            if (stageProgressData.completedStages.Count > 0)
            {
                Debug.Log("   Completed stages:");
                foreach (var stage in stageProgressData.completedStages)
                {
                    Debug.Log($"   ✅ {stage}");
                }
            }
            else
            {
                Debug.LogWarning("   ⚠️ No completed stages found in Firebase data");
            }
        }
        else
        {
            Debug.LogWarning("[StageProgress] No stage progress data in PersistentPlayerData");

            if (PersistentPlayerData.Instance?.multiCharacterData != null)
            {
                PersistentPlayerData.Instance.multiCharacterData.stageProgress = new StageProgressData();
                Debug.Log("[StageProgress] Created default progress");
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

                    // ✅ Sync กับ PlayerPrefs
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
    }

    // ========== PlayerPrefs Methods (Backup) ==========

  

   

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