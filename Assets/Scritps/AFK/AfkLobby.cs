using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class AfkLobby : MonoBehaviour
{
    [Header("UI References")]
    public Button afkButton;
    public Button closeAfkPanelButton;
    public Button claimRewardButton;
    public GameObject afkPanel;

    [Header("Display UI")]
    public TextMeshProUGUI afkTimeText;
    public TextMeshProUGUI baseGoldText;
    public TextMeshProUGUI dummyBonusText;
    public TextMeshProUGUI playerBonusText;
    public TextMeshProUGUI totalGoldText;

    [Header("Progress Bar")]
    public Slider afkProgressSlider;
    public TextMeshProUGUI progressText; // แสดง "5/8 hours"

    [Header("Status")]
    public TextMeshProUGUI statusText;

    [Header("Optional: Notification Badge")]
    public GameObject notificationBadge;

    private AFKRewardResult currentReward;
    private bool hasClaimedReward = false;
    private bool hasUpdatedLoginTime = false; // 🆕 เพิ่มตัวแปรนี้
    private string savedLastLoginTime = ""; // เก็บเวลาเก่าไว้ก่อนอัปเดต
    private bool hasCalculatedReward = false;

    void Start()
    {
        SetupButtons();

        // ปิด Panel ตอนเริ่มต้น
        if (afkPanel != null)
            afkPanel.SetActive(false);

        // ✅ รอให้ข้อมูลโหลดเสร็จก่อนเช็ครางวัล
        StartCoroutine(WaitForDataThenCheckReward());
    }

    // 🆕 Coroutine ใหม่สำหรับรอข้อมูล
    private System.Collections.IEnumerator WaitForDataThenCheckReward()
    {
        Debug.Log("[AfkLobby] 🔄 Waiting for PersistentPlayerData...");

        float timeout = 30f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (PersistentPlayerData.Instance != null &&
                PersistentPlayerData.Instance.isDataLoaded &&
                PersistentPlayerData.Instance.HasValidData())
            {
                Debug.Log("[AfkLobby] ✅ Data loaded successfully");
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (elapsed >= timeout)
        {
            Debug.LogError("[AfkLobby] ❌ Timeout waiting for data");
            yield break;
        }

        // ✅ ใช้ AFKData จาก Firebase แทน PlayerPrefs
        if (!hasCalculatedReward)
        {
            var afkData = PersistentPlayerData.Instance.multiCharacterData.afkData;

            // เช็คว่ามีเวลาที่ pending อยู่หรือไม่
            if (!string.IsNullOrEmpty(afkData.pendingLoginTime))
            {
                // ถ้ามีเวลาที่ยังไม่ได้รับรางวัล ให้ใช้เวลานั้น
                savedLastLoginTime = afkData.pendingLoginTime;
                Debug.Log($"[AfkLobby] 📥 Using pending login time from Firebase: {savedLastLoginTime}");
            }
            else
            {
                // ถ้าไม่มี ให้เก็บเวลาปัจจุบันจาก lastLoginDate
                savedLastLoginTime = PersistentPlayerData.Instance.multiCharacterData.lastLoginDate;

                // บันทึกเป็น pending time
                afkData.pendingLoginTime = savedLastLoginTime;
                PersistentPlayerData.Instance.SavePlayerDataAsync();

                Debug.Log($"[AfkLobby] 💾 Saved new pending login time to Firebase: {savedLastLoginTime}");
            }

            // เช็ครางวัลก่อนอัปเดตเวลา
            CheckForPendingReward();
            hasCalculatedReward = true;

            // อัปเดตเวลาใหม่หลังเช็ครางวัลแล้ว
            UpdateCurrentLoginTime();
        }
    }
    // 🆕 Method สำหรับอัปเดตเวลา Login ปัจจุบัน
    private void UpdateCurrentLoginTime()
    {
        try
        {
            var persistentData = PersistentPlayerData.Instance;

            if (persistentData?.multiCharacterData == null)
            {
                Debug.LogWarning("[AfkLobby] Cannot update login time - no data");
                return;
            }

            string now = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // ✅ บันทึกเวลา Login ปัจจุบันลง Firebase
            persistentData.multiCharacterData.lastLoginDate = now;
            persistentData.SavePlayerDataAsync();

            Debug.Log($"[AfkLobby] ✅ Updated login time: {now}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AfkLobby] Error updating login time: {e.Message}");
        }
    }

    void SetupButtons()
    {
        if (afkButton != null)
            afkButton.onClick.AddListener(OpenAfkPanel);

        if (closeAfkPanelButton != null)
            closeAfkPanelButton.onClick.AddListener(CloseAfkPanel);

        if (claimRewardButton != null)
            claimRewardButton.onClick.AddListener(ClaimReward);
    }

    void OpenAfkPanel()
    {
        if (afkPanel == null) return;

        // คำนวณรางวัล AFK
        CalculateAfkReward();

        // แสดง Panel
        afkPanel.SetActive(true);

        Debug.Log("[AfkLobby] Opened AFK Panel");
    }

    void CloseAfkPanel()
    {
        if (afkPanel != null)
            afkPanel.SetActive(false);

        Debug.Log("[AfkLobby] Closed AFK Panel");
    }

    void CheckForPendingReward()
    {
        try
        {
            var persistentData = PersistentPlayerData.Instance;

            if (persistentData?.multiCharacterData == null)
            {
                Debug.Log("[AfkLobby] No player data available yet");
                UpdateNotificationBadge(false);
                return;
            }

            // ✅ ใช้เวลาเก่าที่เก็บไว้
            string timeToCheck = !string.IsNullOrEmpty(savedLastLoginTime)
                ? savedLastLoginTime
                : persistentData.multiCharacterData.lastLoginDate;

            Debug.Log($"[AfkLobby] 🔍 Checking reward with time: {timeToCheck}");

            bool hasPendingReward = AFKRewardCalculator.HasPendingReward(timeToCheck);

            UpdateNotificationBadge(hasPendingReward);

            if (hasPendingReward)
            {
                Debug.Log($"[AfkLobby] 🎁 Player has pending AFK reward! Last login: {timeToCheck}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[AfkLobby] Error checking pending reward: {e.Message}");
        }
    }

    void CalculateAfkReward()
    {
        try
        {
            var persistentData = PersistentPlayerData.Instance;

            if (persistentData?.multiCharacterData == null)
            {
                ShowNoReward("ไม่พบข้อมูลผู้เล่น");
                return;
            }

            // ✅ ใช้เวลาเก่าที่เก็บไว้
            string timeToCalculate = !string.IsNullOrEmpty(savedLastLoginTime)
                ? savedLastLoginTime
                : persistentData.multiCharacterData.lastLoginDate;

            Debug.Log($"[AfkLobby] 🔍 Calculating reward with time: {timeToCalculate}");

            int dummyLevel = persistentData.multiCharacterData.trainingDummy?.currentLevel ?? 1;
            int playerLevel = persistentData.multiCharacterData.playerLevel;

            currentReward = AFKRewardCalculator.CalculateReward(timeToCalculate, dummyLevel, playerLevel);

            if (currentReward.hasReward)
            {
                DisplayReward(currentReward);
                hasClaimedReward = false;
            }
            else
            {
                ShowNoReward("คุณต้อง AFK อย่างน้อย 1 นาที");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[AfkLobby] Error calculating reward: {e.Message}");
            ShowNoReward("เกิดข้อผิดพลาดในการคำนวณรางวัล");
        }
    }
    void DisplayReward(AFKRewardResult reward)
    {
        // แสดงเวลาที่ AFK
        if (afkTimeText != null)
            afkTimeText.text = $"Time AFK: {reward.GetAfkTimeDisplayShort()}";

        // แสดงเงินพื้นฐาน
        if (baseGoldText != null)
            baseGoldText.text = $"Base: {reward.baseGold:N0} Gold";

        // ✅ แสดงโบนัสจาก Dummy (คูณ Level)
        if (dummyBonusText != null)
        {
            dummyBonusText.text = $"Bonus Dummy Lv.{reward.dummyLevel} (x{reward.dummyLevel}): +{reward.dummyBonus:N0} Gold";
        }

        // แสดงโบนัสจาก Player Level (1% ต่อ level)
        if (playerBonusText != null)
        {
            float playerPercent = reward.playerLevel * 1f;
            playerBonusText.text = $"Bonus Player Lv.{reward.playerLevel} (+{playerPercent:F0}%): +{reward.playerLevelBonus:N0} Gold";
        }

        // แสดงเงินรวม
        if (totalGoldText != null)
            totalGoldText.text = $"Total: {reward.totalGold:N0} Gold";

        // อัปเดต Progress Bar
        UpdateProgressBar(reward.afkHours);

        // แสดงสถานะ
        if (statusText != null)
        {
            statusText.text = "กดปุ่มด้านล่างเพื่อรับรางวัล";
            statusText.color = Color.green;
        }

        // เปิดใช้ปุ่มรับรางวัล
        if (claimRewardButton != null)
            claimRewardButton.interactable = true;

        Debug.Log($"[AfkLobby] Displayed reward: {reward.totalGold} gold for {reward.GetAfkTimeDisplayShort()}");
    }
    private void UpdateProgressBar(float afkHours)
    {
        const int MAX_AFK_HOURS = 8;

        // อัปเดต Slider
        if (afkProgressSlider != null)
        {
            afkProgressSlider.maxValue = MAX_AFK_HOURS;
            afkProgressSlider.value = Mathf.Min(afkHours, MAX_AFK_HOURS);
        }

        // อัปเดต Progress Text
        if (progressText != null)
        {
            float cappedHours = Mathf.Min(afkHours, MAX_AFK_HOURS);
            progressText.text = $"{cappedHours:F1}/{MAX_AFK_HOURS} Hrs";

            // เปลี่ยนสีถ้าถึง limit
            if (afkHours >= MAX_AFK_HOURS)
            {
                progressText.color = Color.yellow; // สีเหลือง = ถึง limit
            }
            else
            {
                progressText.color = Color.white;
            }
        }
    }

    void ShowNoReward(string message)
    {
        // ซ่อนข้อมูลรางวัล
        if (afkTimeText != null) afkTimeText.text = "Time AFK: -";
        if (baseGoldText != null) baseGoldText.text = "Base: 0 Gold";
        if (dummyBonusText != null) dummyBonusText.text = "Bonus Dummy: 0 Gold";
        if (playerBonusText != null) playerBonusText.text = "Bonus Player: 0 Gold";
        if (totalGoldText != null) totalGoldText.text = "Total: 0 Gold";

        // ✅ รีเซ็ต Progress Bar
        if (afkProgressSlider != null)
            afkProgressSlider.value = 0;

        if (progressText != null)
        {
            progressText.text = "0/8 Hrs";
            progressText.color = Color.white;
        }

        // แสดงสถานะ
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = Color.gray;
        }

        // ปิดปุ่มรับรางวัล
        if (claimRewardButton != null)
            claimRewardButton.interactable = false;

        currentReward = null;

        Debug.Log($"[AfkLobby] No reward available: {message}");
    }

    void ClaimReward()
    {
        if (currentReward == null || !currentReward.hasReward || hasClaimedReward)
        {
            Debug.LogWarning("[AfkLobby] No reward to claim or already claimed");
            return;
        }

        try
        {
            var currencyManager = CurrencyManager.FindCurrencyManager();

            if (currencyManager != null)
            {
                bool success = currencyManager.AddGold(currentReward.totalGold, true);

                if (success)
                {
                    Debug.Log($"[AfkLobby] ✅ Successfully claimed {currentReward.totalGold} gold from AFK");

                    var persistentData = PersistentPlayerData.Instance;
                    if (persistentData?.multiCharacterData?.afkData != null)
                    {
                        Debug.Log($"[ClaimReward] 📝 Before claim:");
                        Debug.Log($"  pendingLoginTime: '{persistentData.multiCharacterData.afkData.pendingLoginTime}'");
                        Debug.Log($"  lastLoginDate: '{persistentData.multiCharacterData.lastLoginDate}'");

                        // บันทึกข้อมูลการรับรางวัล
                        persistentData.multiCharacterData.afkData.RecordClaim(currentReward.totalGold);

                        // ✅ เคลียร์ pending login time
                        persistentData.multiCharacterData.afkData.pendingLoginTime = "";

                        // ✅ อัปเดต lastLoginDate
                        persistentData.multiCharacterData.lastLoginDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                        Debug.Log($"[ClaimReward] 📝 After claim (before save):");
                        Debug.Log($"  pendingLoginTime: '{persistentData.multiCharacterData.afkData.pendingLoginTime}'");
                        Debug.Log($"  lastLoginDate: '{persistentData.multiCharacterData.lastLoginDate}'");

                        // ✅ บังคับ Save แบบ synchronous พร้อม verify
                        StartCoroutine(ForceSaveAndVerifyAfkClaim());
                    }

                    // เคลียร์เวลาเก่าใน memory
                    savedLastLoginTime = "";

                    hasClaimedReward = true;

                    if (statusText != null)
                    {
                        statusText.text = $"รับรางวัล {currentReward.totalGold:N0} Gold แล้ว!";
                        statusText.color = Color.yellow;
                    }

                    if (claimRewardButton != null)
                        claimRewardButton.interactable = false;

                    UpdateNotificationBadge(false);

                    CloseAfkPanel();
                }
                else
                {
                    Debug.LogError("[AfkLobby] Failed to add gold to player");

                    if (statusText != null)
                    {
                        statusText.text = "เกิดข้อผิดพลาด ไม่สามารถรับรางวัลได้";
                        statusText.color = Color.red;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[AfkLobby] Error claiming reward: {e.Message}");
        }
    }

    // 🆕 Coroutine ใหม่สำหรับ save และ verify แบบเข้มงวด
    private System.Collections.IEnumerator ForceSaveAndVerifyAfkClaim()
    {
        var persistentData = PersistentPlayerData.Instance;

        Debug.Log("[AfkLobby] 💾 Force saving AFK claim data...");

        // Save ครั้งที่ 1
        persistentData.ForceSaveAFKData();

        // รอให้ Firebase save เสร็จ (2 วินาที)
        yield return new WaitForSeconds(2f);

        // ✅ Verify ว่า save สำเร็จ โดยโหลดข้อมูลกลับมาจาก Firebase
        Debug.Log("[AfkLobby] 🔍 Verifying AFK data was saved...");

        yield return StartCoroutine(VerifyAfkDataSaved());

        Debug.Log("[AfkLobby] ✅ AFK claim save and verify completed");
    }

    // 🆕 Verify โดยการโหลดข้อมูลจาก Firebase กลับมาเช็ค
    // 🆕 Verify โดยการโหลดข้อมูลจาก Firebase กลับมาเช็ค
    // 🆕 Verify โดยการโหลดข้อมูลจาก Firebase กลับมาเช็ค
    private System.Collections.IEnumerator VerifyAfkDataSaved()
    {
        Debug.Log("[VerifyAfkDataSaved] 🔍 Starting verification...");

        var persistentData = PersistentPlayerData.Instance;

        // ✅ แก้ไข: ใช้ Firebase reference จาก PersistentPlayerData
        if (persistentData?.auth?.CurrentUser == null)
        {
            Debug.LogError("[VerifyAfkDataSaved] ❌ Cannot verify - no auth user");
            yield break;
        }

        Debug.Log($"[VerifyAfkDataSaved] Reading from Firebase for user: {persistentData.auth.CurrentUser.UserId}");

        // อ่านข้อมูลจาก Firebase
        var task = persistentData.GetDatabaseReference()
            .Child("players")
            .Child(persistentData.auth.CurrentUser.UserId)
            .GetValueAsync();

        // รอให้ Firebase response
        float timeout = 10f;
        float elapsed = 0f;

        while (!task.IsCompleted && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (elapsed >= timeout)
        {
            Debug.LogError("[VerifyAfkDataSaved] ❌ Timeout waiting for Firebase response");
            yield break;
        }

        if (task.Exception != null)
        {
            Debug.LogError($"[VerifyAfkDataSaved] ❌ Firebase error: {task.Exception.Message}");
            yield break;
        }

        if (!task.Result.Exists)
        {
            Debug.LogError("[VerifyAfkDataSaved] ❌ No data found in Firebase");
            yield break;
        }

        Debug.Log("[VerifyAfkDataSaved] ✅ Data received from Firebase, parsing...");

        // ✅ แก้ไข: ย้าย try-catch ออกมาเป็น method แยก
        yield return StartCoroutine(ParseAndCheckAfkData(task.Result));
    }

    // 🆕 แยก method สำหรับ parse data (ไม่มี yield return ใน try-catch)
    private System.Collections.IEnumerator ParseAndCheckAfkData(Firebase.Database.DataSnapshot snapshot)
    {
        string json = "";
        MultiCharacterPlayerData reloadedData = null;
        bool parseSuccess = false;

        // Parse ข้อมูล (ไม่มี yield return ใน try-catch)
        try
        {
            json = snapshot.GetRawJsonValue();

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("[ParseAndCheckAfkData] ❌ Empty JSON from Firebase");
                yield break;
            }

            Debug.Log($"[ParseAndCheckAfkData] JSON length: {json.Length} characters");

            reloadedData = JsonUtility.FromJson<MultiCharacterPlayerData>(json);
            parseSuccess = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ParseAndCheckAfkData] ❌ Parse error: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            yield break;
        }

        // ถ้า parse สำเร็จ ให้เช็คข้อมูล
        if (parseSuccess && reloadedData?.afkData != null)
        {
            string pendingTime = reloadedData.afkData.pendingLoginTime;
            string lastLogin = reloadedData.lastLoginDate;

            Debug.Log($"[ParseAndCheckAfkData] 🔍 Firebase values:");
            Debug.Log($"  pendingLoginTime: '{pendingTime}'");
            Debug.Log($"  lastLoginDate: '{lastLogin}'");

            if (string.IsNullOrEmpty(pendingTime))
            {
                Debug.Log("[ParseAndCheckAfkData] ✅ Verified: Pending time cleared successfully in Firebase");
            }
            else
            {
                Debug.LogError($"[ParseAndCheckAfkData] ❌ CRITICAL: Pending time still exists in Firebase: '{pendingTime}'");
                Debug.LogError("[ParseAndCheckAfkData] Forcing save again...");

                // บังคับ save อีกครั้งแบบ direct update
                yield return StartCoroutine(ForceDirectUpdateFirebase());
            }
        }
        else
        {
            Debug.LogError("[ParseAndCheckAfkData] ❌ afkData is null in reloaded data");
        }
    }

    // 🆕 Force update Firebase โดยตรง (ถ้า verify ล้มเหลว)
    private System.Collections.IEnumerator ForceDirectUpdateFirebase()
    {
        Debug.Log("[ForceDirectUpdateFirebase] 🚀 Starting direct Firebase update...");

        var persistentData = PersistentPlayerData.Instance;

        if (persistentData?.auth?.CurrentUser == null)
        {
            Debug.LogError("[ForceDirectUpdateFirebase] ❌ No auth user");
            yield break;
        }

        // อัปเดตเฉพาะ field ที่จำเป็น
        var updates = new System.Collections.Generic.Dictionary<string, object>
    {
        { "afkData/pendingLoginTime", "" },
        { "lastLoginDate", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
    };

        Debug.Log("[ForceDirectUpdateFirebase] 📤 Sending direct update to Firebase...");

        var task = persistentData.GetDatabaseReference()
            .Child("players")
            .Child(persistentData.auth.CurrentUser.UserId)
            .UpdateChildrenAsync(updates);

        // รอให้ update เสร็จ
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError($"[ForceDirectUpdateFirebase] ❌ Update failed: {task.Exception.Message}");
        }
        else
        {
            Debug.Log("[ForceDirectUpdateFirebase] ✅ Direct update successful");

            // อัปเดต local data ด้วย
            persistentData.multiCharacterData.afkData.pendingLoginTime = "";
            persistentData.multiCharacterData.lastLoginDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Verify อีกครั้งหลัง update
            yield return new WaitForSeconds(2f);

            Debug.Log("[ForceDirectUpdateFirebase] 🔍 Re-verifying after direct update...");
            yield return StartCoroutine(VerifyAfkDataSaved());
        }
    }

    // 🆕 Force update Firebase โดยตรง (ถ้า verify ล้มเหลว)

    // 🆕 Verify ว่า pending time ถูกเคลียร์แล้ว
    private System.Collections.IEnumerator VerifyPendingTimeCleared()
    {
        // รอให้ save เสร็จ
        yield return new WaitForSeconds(1.5f);

        var persistentData = PersistentPlayerData.Instance;
        if (persistentData?.multiCharacterData?.afkData != null)
        {
            string pendingTime = persistentData.multiCharacterData.afkData.pendingLoginTime;

            if (string.IsNullOrEmpty(pendingTime))
            {
                Debug.Log("[AfkLobby] ✅ Verified: Pending time cleared in Firebase");
            }
            else
            {
                Debug.LogWarning($"[AfkLobby] ⚠️ Pending time still exists: '{pendingTime}'");
                Debug.LogWarning("[AfkLobby] Attempting to clear again...");

                // บังคับเคลียร์อีกครั้ง
                persistentData.multiCharacterData.afkData.pendingLoginTime = "";
                persistentData.ForceSaveAFKData();
            }
        }
    }
    // 🆕 Coroutine สำหรับบังคับ save และ verify
    private System.Collections.IEnumerator SaveAndVerifyPendingTime(PersistentPlayerData persistentData)
    {
        // Save ครั้งแรก
        persistentData.SavePlayerDataAsync();
        Debug.Log("[AfkLobby] 💾 Saving cleared pending time to Firebase...");

        // รอให้ save เสร็จ
        yield return new WaitForSeconds(1f);

        // Verify ว่า save สำเร็จ
        if (string.IsNullOrEmpty(persistentData.multiCharacterData.afkData.pendingLoginTime))
        {
            Debug.Log("[AfkLobby] ✅ Verified: Pending time cleared successfully");
        }
        else
        {
            Debug.LogWarning("[AfkLobby] ⚠️ Pending time still exists, forcing save again...");

            // บังคับเคลียร์อีกครั้ง
            persistentData.multiCharacterData.afkData.pendingLoginTime = "";
            persistentData.SavePlayerDataAsync();

            yield return new WaitForSeconds(1f);

            if (string.IsNullOrEmpty(persistentData.multiCharacterData.afkData.pendingLoginTime))
            {
                Debug.Log("[AfkLobby] ✅ Retry successful: Pending time cleared");
            }
            else
            {
                Debug.LogError("[AfkLobby] ❌ Failed to clear pending time after retry");
            }
        }
    }
    void UpdateNotificationBadge(bool show)
    {
        if (notificationBadge != null)
            notificationBadge.SetActive(show);
    }

    // Debug methods (เหมือนเดิม)
    [ContextMenu("Test AFK Reward (1 hour)")]
    void TestAfkReward1Hour()
    {
        var persistentData = PersistentPlayerData.Instance;
        if (persistentData?.multiCharacterData != null)
        {
            DateTime oneHourAgo = DateTime.Now.AddHours(-1);
            persistentData.multiCharacterData.lastLoginDate = oneHourAgo.ToString("yyyy-MM-dd HH:mm:ss");

            Debug.Log($"[AfkLobby] Test: Set last login to 1 hour ago");

            CheckForPendingReward();
        }
    }

    [ContextMenu("Test AFK Reward (8 hours)")]
    void TestAfkReward8Hours()
    {
        var persistentData = PersistentPlayerData.Instance;
        if (persistentData?.multiCharacterData != null)
        {
            DateTime eightHoursAgo = DateTime.Now.AddHours(-8);
            persistentData.multiCharacterData.lastLoginDate = eightHoursAgo.ToString("yyyy-MM-dd HH:mm:ss");

            Debug.Log($"[AfkLobby] Test: Set last login to 8 hours ago");

            CheckForPendingReward();
        }
    }
}