using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Fusion;
[System.Serializable]
public struct StageCompletionResult
{
    public int expGained;
    public int playerLevelBefore;
    public int playerLevelAfter;
    public int playerExpBefore;
    public int playerExpAfter;
    public int playerExpToNextBefore;
    public int playerExpToNextAfter;
    public bool isFirstClear;
    public int firstClearGemsBonus;
    public PlayerLevelUpResult levelUpResult;
}
public class StageCompleteUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject stageCompletePanel;
    public TextMeshProUGUI stageNameText;
    public TextMeshProUGUI congratsText;
    public Button backToLobbyButton;

    [Header("🏆 Rewards Display")]
    public TextMeshProUGUI goldRewardText;
    public TextMeshProUGUI gemsRewardText;
    public Transform itemRewardsContainer;
    public GameObject itemRewardPrefab;
    public TextMeshProUGUI completionTimeText;
    public TextMeshProUGUI enemiesKilledText;
    [Header("⏱️ Display Settings")]
    public float delayBeforeShow = 2f; // หน่วงเวลาก่อนแสดง UI
    public float fadeInDuration = 1f;   // ระยะเวลา fade in
    [Header("Audio")]
    public AudioSource victoryAudioSource;
    public AudioClip victorySound;
    [Header("📈 Player EXP Display")]
    public Slider playerExpSlider;
    public TextMeshProUGUI playerLevelText;
    public TextMeshProUGUI playerExpText;
    public TextMeshProUGUI playerExpGainedText;
    public AudioClip playerLevelUpSound;


    private void Awake()
    {
        // ซ่อน panel ตอนเริ่มต้น
        if (stageCompletePanel != null)
            stageCompletePanel.SetActive(false);
    }

    private void Start()
    {
        // Setup ปุ่ม Back to Lobby
        if (backToLobbyButton != null)
        {
            backToLobbyButton.onClick.AddListener(BackToLobby);
        }
    }
    private void Update()
    {
        // ✅ Debug timeScale (ลบออกได้เมื่อแก้เสร็จแล้ว)
        if (Time.timeScale != 1f && !stageCompletePanel.activeInHierarchy)
        {
            Debug.LogWarning($"[StageCompleteUI] TimeScale is {Time.timeScale} but UI is not showing! Resetting...");
            Time.timeScale = 1f;
        }
    }
    /// <summary>
    /// แสดง Stage Complete UI พร้อมข้อมูล rewards - เรียกจาก RPC
    /// </summary>
    public void ShowStageComplete(string stageName)
    {
        Debug.Log($"🔍 [StageCompleteUI] ShowStageComplete called for: {stageName}");

        if (stageCompletePanel == null)
        {
            Debug.LogWarning("[StageCompleteUI] Stage complete panel is not assigned!");
            return;
        }

        // เรียกแค่ coroutine เท่านั้น ไม่แสดง UI ทันที
        StartCoroutine(ShowStageCompleteWithDelay(stageName));
    }

    /// <summary>
    /// แสดงข้อมูล rewards ที่ได้รับ
    /// </summary>
    /// 
    /// <summary>
    /// แสดงข้อมูล rewards
    /// </summary>
    private void DisplayStageRewards()
    {
        var rewards = StageRewardTracker.GetCurrentRewards();

        if (rewards == null)
        {
            Debug.LogWarning("[StageCompleteUI] No reward data available!");
            return;
        }

        // แสดงเงินและเพชร
        DisplayCurrencyRewards(rewards);

        // แสดงไอเทม
        DisplayItemRewards(rewards);

        // แสดงสถิติ
        DisplayStatistics(rewards);

        Debug.Log($"[StageCompleteUI] 🏆 Displayed rewards: Gold={rewards.totalGoldEarned:N0}, Gems={rewards.totalGemsEarned}, Items={rewards.itemsEarned.Count}");
    }

    /// <summary>
    /// แสดงเงินและเพชรรวม First Clear Bonus
    /// </summary>
    /// <summary>
    /// แสดงเงินและเพชรรวม First Clear Bonus
    /// </summary>
    private void DisplayCurrencyRewardsWithFirstClear(StageRewardData rewards)
    {
        if (goldRewardText != null)
        {
            goldRewardText.text = $"💰 {rewards.totalGoldEarned:N0}";
            goldRewardText.color = Color.yellow;
        }

        if (gemsRewardText != null)
        {
            // ✅ แค่แสดงเพชรปกติ ไม่ต้องตรวจสอบ First Clear ใน UI
            gemsRewardText.text = $"💎 {rewards.totalGemsEarned}";
            gemsRewardText.color = Color.cyan;
        }
    }
    private IEnumerator ShowStageCompleteWithDelay(string stageName)
    {
        Debug.Log($"🕒 [StageCompleteUI] Waiting {delayBeforeShow} seconds before showing UI...");

        yield return new WaitForSeconds(delayBeforeShow);
        DailyQuestTracker.AddStageCompletion();

        // หยุดการติดตาม rewards และคำนวณเวลา
        StageRewardTracker.Instance.StopStageTracking();

        // ✅ ประมวลผล Player EXP และ First Clear ก่อนแสดง UI
        var stageResult = ProcessStageCompletionData(stageName);

        // ตั้งค่า panel ให้โปร่งใสก่อน (สำหรับ fade in effect)
        if (stageCompletePanel != null)
        {
            CanvasGroup canvasGroup = stageCompletePanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = stageCompletePanel.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            stageCompletePanel.SetActive(true);
        }

        // ตั้งค่าข้อความพื้นฐาน
        if (stageNameText != null)
            stageNameText.text = stageName;

        if (congratsText != null)
            congratsText.text = "🎉 STAGE COMPLETED! 🎉";

        // ✅ แสดงข้อมูลแบบ Static ก่อน (ไม่มี Animation)
        DisplayAllRewardsStatic(stageResult);

        // เล่นเสียง
        PlayVictorySound();

        // เริ่ม fade in effect
        yield return StartCoroutine(FadeInPanel());

        // ✅ หลังจาก UI แสดงเสร็จแล้ว ค่อยเล่น EXP Animation
        yield return new WaitForSecondsRealtime(0.5f);

        if (stageResult.expGained > 0)
        {
            yield return StartCoroutine(AnimatePlayerExpOnly(stageResult));
        }

        // ❌ ลบส่วนนี้ออก - ไม่ต้องปรับ timeScale
        // Time.timeScale = 0.1f;
        // StartCoroutine(RestoreTimeScale());

        Debug.Log($"🏆 [StageCompleteUI] Stage complete UI fully displayed for: {stageName}");
    }
    private void DisplayAllRewardsStatic(StageCompletionResult stageResult)
    {
        var stageRewards = StageRewardTracker.GetCurrentRewards();

        if (stageRewards == null)
        {
            Debug.LogWarning("[StageCompleteUI] No reward data available!");
            return;
        }

        // 1. แสดงข้อมูลพื้นฐาน (Gold, Items, Stats)
        DisplayBasicRewards(stageRewards);
        DisplayStatistics(stageRewards);
        DisplayItemRewards(stageRewards);

        // 2. แสดง Gems รวม (ไม่มี Animation)
        DisplayFinalGemsReward(stageRewards, stageResult);

        // 3. Setup Player EXP เริ่มต้น (ค่าเก่าก่อน Animation)
        SetupPlayerExpDisplayInitial(stageResult.playerLevelBefore, stageResult.playerExpBefore, stageResult.playerExpToNextBefore);

        Debug.Log($"[StageCompleteUI] 🏆 All rewards displayed (static)");
    }
    private void SetupPlayerExpDisplayInitial(int level, int exp, int expToNext)
    {
        if (playerExpSlider != null)
        {
            float progress = (float)exp / expToNext;
            playerExpSlider.value = progress;
        }

        if (playerLevelText != null)
            playerLevelText.text = $"PLAYER LEVEL {level}";

        if (playerExpText != null)
            playerExpText.text = $"{exp}/{expToNext} EXP";

        if (playerExpGainedText != null)
            playerExpGainedText.text = ""; // เคลียร์ก่อน
    }
    private StageCompletionResult ProcessStageCompletionData(string stageName)
    {
        var result = new StageCompletionResult();

        try
        {
            var persistentData = PersistentPlayerData.Instance;
            if (persistentData?.multiCharacterData == null) return result;

            // บันทึกข้อมูล Player ก่อนเพิ่ม EXP
            result.playerLevelBefore = persistentData.GetPlayerLevel();
            result.playerExpBefore = persistentData.GetPlayerExp();
            result.playerExpToNextBefore = persistentData.GetPlayerExpToNext();

            // ✅ ตรวจสอบ First Clear Bonus
            result.isFirstClear = persistentData.multiCharacterData.IsFirstClearStage(stageName);

            if (result.isFirstClear)
            {
                result.firstClearGemsBonus = persistentData.multiCharacterData.ProcessFirstClearBonus(stageName);
                Debug.Log($"💎 [FirstClear] Processing first clear bonus: {result.firstClearGemsBonus} gems");

                // เพิ่มเพชร First Clear Bonus ทันที
                if (result.firstClearGemsBonus > 0)
                {
                    var currencyManager = FindObjectOfType<CurrencyManager>();
                    if (currencyManager != null)
                    {
                        currencyManager.AddGems(result.firstClearGemsBonus, true);
                    }
                }
            }

            // คำนวณ EXP ที่ได้จากการจบด่าน
            result.expGained = CalculateStageCompletionExp(stageName);

            if (result.expGained > 0)
            {
                result.levelUpResult = persistentData.multiCharacterData.GainPlayerExpFromStageCompletion(stageName, result.expGained);

                // เพิ่มรางวัลจาก Player Level Up
                if (result.levelUpResult.didLevelUp && result.levelUpResult.HasRewards())
                {
                    var currencyManager = FindObjectOfType<CurrencyManager>();
                    if (currencyManager != null)
                    {
                        if (result.levelUpResult.goldReward > 0)
                            currencyManager.AddGold(result.levelUpResult.goldReward, false);
                        if (result.levelUpResult.gemsReward > 0)
                            currencyManager.AddGems(result.levelUpResult.gemsReward, false);
                    }
                }

                // บันทึกข้อมูล
                persistentData.SavePlayerDataAsync();
            }

            // บันทึกข้อมูล Player หลังเพิ่ม EXP
            result.playerLevelAfter = persistentData.GetPlayerLevel();
            result.playerExpAfter = persistentData.GetPlayerExp();
            result.playerExpToNextAfter = persistentData.GetPlayerExpToNext();

            Debug.Log($"[StageComplete] 📈 Player gained {result.expGained} EXP from completing {stageName}");
            Debug.Log($"[StageComplete] First clear: {result.isFirstClear}, Bonus gems: {result.firstClearGemsBonus}");

        }
        catch (System.Exception e)
        {
            Debug.LogError($"[StageComplete] ❌ Error in ProcessStageCompletionData: {e.Message}");
        }

        return result;
    }
    /// <summary>
    /// ✅ แสดงข้อมูล rewards ทั้งหมดแบบ animated
    /// </summary>
    /// <summary>
    /// ✅ แสดงข้อมูล rewards ทั้งหมดแบบ animated (แบบง่าย)
    /// </summary>
  

    /// <summary>
    /// แสดง Gems รวม (ไม่มี Animation)
    /// </summary>
    private void DisplayFinalGemsReward(StageRewardData stageRewards, StageCompletionResult stageResult)
    {
        if (gemsRewardText == null) return;

        // คำนวณเพชรรวมทั้งหมด
        int totalGems = stageRewards.totalGemsEarned;
        if (stageResult.isFirstClear) totalGems += stageResult.firstClearGemsBonus;
        if (stageResult.levelUpResult.didLevelUp) totalGems += stageResult.levelUpResult.gemsReward;

        // แสดงผลรวม
        string gemText = $"💎 {totalGems}";

        // เพิ่มข้อความ breakdown ถ้ามี bonus
        List<string> bonusInfo = new List<string>();

        if (stageResult.isFirstClear && stageResult.firstClearGemsBonus > 0)
        {
            bonusInfo.Add($"First Clear +{stageResult.firstClearGemsBonus}");
            gemsRewardText.color = Color.magenta; // สีพิเศษสำหรับ first clear
        }
        else
        {
            gemsRewardText.color = Color.cyan;
        }

        if (stageResult.levelUpResult.didLevelUp && stageResult.levelUpResult.gemsReward > 0)
        {
            bonusInfo.Add($"Level Up +{stageResult.levelUpResult.gemsReward}");
        }

        // เพิ่มข้อความ bonus (ถ้ามี)
        if (bonusInfo.Count > 0)
        {
            gemText += $" ({string.Join(", ", bonusInfo)})";
        }

        gemsRewardText.text = gemText;

        Debug.Log($"[StageCompleteUI] Final gems display: {totalGems} (Base: {stageRewards.totalGemsEarned}, First Clear: {stageResult.firstClearGemsBonus}, Level Up: {stageResult.levelUpResult.gemsReward})");
    }

    /// <summary>
    /// Animate เฉพาะ Player EXP Slider
    /// </summary>
    /// <summary>
    /// Animate เฉพาะ Player EXP Slider (หลังจาก UI แสดงแล้ว)
    /// </summary>
    private IEnumerator AnimatePlayerExpOnly(StageCompletionResult stageResult)
    {
        Debug.Log($"[StageCompleteUI] 🎬 Starting EXP animation...");

        // ✅ แสดง EXP gained text ก่อน
        if (playerExpGainedText != null)
        {
            playerExpGainedText.text = $"+{stageResult.expGained} EXP";
            playerExpGainedText.color = Color.green;
        }

        yield return new WaitForSecondsRealtime(0.3f);

        bool hasLeveledUp = stageResult.levelUpResult.didLevelUp;

        if (hasLeveledUp)
        {
            // ✅ กรณี Level Up: แยกเป็น 2 phase
            yield return StartCoroutine(AnimateExpWithLevelUp(stageResult));
        }
        else
        {
            // ✅ กรณีไม่ Level Up: animate ปกติ
            yield return StartCoroutine(AnimateExpNormal(stageResult));
        }

        Debug.Log($"✅ [StageCompleteUI] Player EXP animation completed");
    }

    /// <summary>
    /// Animate EXP แบบปกติ (ไม่ Level Up)
    /// </summary>
    private IEnumerator AnimateExpNormal(StageCompletionResult stageResult)
    {
        float animationDuration = 1.5f;
        float elapsed = 0f;

        int startExp = stageResult.playerExpBefore;
        int endExp = stageResult.playerExpAfter;
        int expToNext = stageResult.playerExpToNextBefore;

        Debug.Log($"[StageCompleteUI] Normal EXP animation: {startExp} → {endExp}/{expToNext}");

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / animationDuration;

            int currentExp = Mathf.RoundToInt(Mathf.Lerp(startExp, endExp, progress));

            // Update UI
            if (playerExpSlider != null)
            {
                float sliderValue = (float)currentExp / expToNext;
                playerExpSlider.value = sliderValue;
            }

            if (playerExpText != null)
                playerExpText.text = $"{currentExp}/{expToNext} EXP";

            yield return null;
        }

        // Final values
        if (playerExpSlider != null)
            playerExpSlider.value = (float)endExp / expToNext;

        if (playerExpText != null)
            playerExpText.text = $"{endExp}/{expToNext} EXP";
    }

    /// <summary>
    /// Animate EXP พร้อม Level Up (2 Phase Animation)
    /// </summary>
    private IEnumerator AnimateExpWithLevelUp(StageCompletionResult stageResult)
    {
        int startExp = stageResult.playerExpBefore;
        int expToNext = stageResult.playerExpToNextBefore;
        int startLevel = stageResult.playerLevelBefore;
        int endLevel = stageResult.playerLevelAfter;
        int finalExp = stageResult.playerExpAfter;
        int finalExpToNext = stageResult.playerExpToNextAfter;

        Debug.Log($"[StageCompleteUI] Level Up EXP animation: Level {startLevel} → {endLevel}");
        Debug.Log($"[StageCompleteUI] EXP: {startExp}/{expToNext} → {finalExp}/{finalExpToNext}");

        // ✅ Phase 1: เติม EXP จนเต็ม (ถึง expToNext)
        Debug.Log($"[StageCompleteUI] Phase 1: Filling EXP bar to full...");

        float phase1Duration = 1.2f;
        float elapsed = 0f;

        while (elapsed < phase1Duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / phase1Duration;

            int currentExp = Mathf.RoundToInt(Mathf.Lerp(startExp, expToNext, progress));

            // Update UI
            if (playerExpSlider != null)
            {
                float sliderValue = (float)currentExp / expToNext;
                playerExpSlider.value = sliderValue;
            }

            if (playerExpText != null)
                playerExpText.text = $"{currentExp}/{expToNext} EXP";

            yield return null;
        }

        // ✅ เติมให้เต็มพอดี 100%
        if (playerExpSlider != null)
            playerExpSlider.value = 1f;

        if (playerExpText != null)
            playerExpText.text = $"{expToNext}/{expToNext} EXP";

        yield return new WaitForSecondsRealtime(0.3f); // รอให้เห็นแถบเต็ม

        // ✅ Phase 2: Level Up Effect + Reset Bar
        Debug.Log($"[StageCompleteUI] Phase 2: Level up effect and reset bar...");

        StartCoroutine(ShowLevelUpEffect());

        // อัพเดต Level text
        if (playerLevelText != null)
            playerLevelText.text = $"PLAYER LEVEL {endLevel}";

        yield return new WaitForSecondsRealtime(0.5f); // รอให้ level up effect เล่น

        // ✅ Phase 3: Reset bar และ animate ไปยัง EXP ใหม่
        Debug.Log($"[StageCompleteUI] Phase 3: Animating new level EXP...");

        float phase3Duration = 0.8f;
        elapsed = 0f;

        while (elapsed < phase3Duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / phase3Duration;

            int currentExp = Mathf.RoundToInt(Mathf.Lerp(0, finalExp, progress));

            // Update UI
            if (playerExpSlider != null)
            {
                float sliderValue = (float)currentExp / finalExpToNext;
                playerExpSlider.value = sliderValue;
            }

            if (playerExpText != null)
                playerExpText.text = $"{currentExp}/{finalExpToNext} EXP";

            yield return null;
        }

        // ✅ Final values
        if (playerExpSlider != null)
            playerExpSlider.value = (float)finalExp / finalExpToNext;

        if (playerLevelText != null)
            playerLevelText.text = $"PLAYER LEVEL {endLevel}";

        if (playerExpText != null)
            playerExpText.text = $"{finalExp}/{finalExpToNext} EXP";

        // Log level up rewards
        if (stageResult.levelUpResult.goldReward > 0)
            Debug.Log($"💰 [StageCompleteUI] Level up reward: {stageResult.levelUpResult.goldReward:N0} gold");
        if (stageResult.levelUpResult.gemsReward > 0)
            Debug.Log($"💎 [StageCompleteUI] Level up reward: {stageResult.levelUpResult.gemsReward} gems");

        Debug.Log($"🎉 [StageCompleteUI] Level up animation completed!");
    }

    /// <summary>
    /// แสดง Level Up Effect แบบง่าย
    /// </summary>
    private IEnumerator ShowLevelUpEffect()
    {
        // เล่นเสียง level up
        if (victoryAudioSource != null && playerLevelUpSound != null)
        {
            victoryAudioSource.PlayOneShot(playerLevelUpSound);
        }

        // Pulse effect บน level text
        if (playerLevelText != null)
        {
            Vector3 originalScale = playerLevelText.transform.localScale;
            Color originalColor = playerLevelText.color;

            float pulseDuration = 1f;
            float elapsed = 0f;

            while (elapsed < pulseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / pulseDuration;

                // Pulse scale
                float scale = 1f + (Mathf.Sin(progress * Mathf.PI * 6) * 0.15f);
                playerLevelText.transform.localScale = originalScale * scale;

                // Color flash
                Color flashColor = Color.Lerp(originalColor, Color.yellow, Mathf.Sin(progress * Mathf.PI * 6) * 0.5f + 0.5f);
                playerLevelText.color = flashColor;

                yield return null;
            }

            // Restore original
            playerLevelText.transform.localScale = originalScale;
            playerLevelText.color = originalColor;
        }
    }

    /// <summary>
    /// แสดงข้อมูลพื้นฐาน (ไม่รวม Bonus ต่างๆ)
    /// </summary>
    private void DisplayBasicRewards(StageRewardData rewards)
    {
        if (goldRewardText != null)
        {
            goldRewardText.text = $"💰 {rewards.totalGoldEarned:N0}";
            goldRewardText.color = Color.yellow;
        }

        if (gemsRewardText != null)
        {
            gemsRewardText.text = $"💎 0"; // เริ่มจาก 0
            gemsRewardText.color = Color.cyan;
        }
    }

    /// <summary>
    /// Setup Player EXP Display
    /// </summary>
   

    /// <summary>
    /// Animate Player EXP Gain (แบบรวมใน UI หลัก)
    /// </summary>
    private IEnumerator AnimatePlayerExpGainIntegrated(StageCompletionResult stageResult)
    {
        if (playerExpGainedText != null)
        {
            playerExpGainedText.text = $"+{stageResult.expGained} EXP";
            playerExpGainedText.color = Color.green;
        }

        float animationDuration = 1.5f;
        float elapsed = 0f;

        int currentExp = stageResult.playerExpBefore;
        int targetExp = stageResult.playerExpAfter;
        int currentExpToNext = stageResult.playerExpToNextBefore;
        int currentLevel = stageResult.playerLevelBefore;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / animationDuration;

            // Calculate animated values
            int animatedExp = Mathf.RoundToInt(Mathf.Lerp(currentExp, targetExp, progress));

            // Handle level up during animation
            if (progress >= 0.7f && stageResult.levelUpResult.didLevelUp)
            {
                currentLevel = stageResult.playerLevelAfter;
                currentExpToNext = stageResult.playerExpToNextAfter;
                animatedExp = stageResult.playerExpAfter;
            }

            // Update UI
            if (playerExpSlider != null)
            {
                float sliderProgress = (float)animatedExp / currentExpToNext;
                playerExpSlider.value = sliderProgress;
            }

            if (playerLevelText != null)
                playerLevelText.text = $"PLAYER LEVEL {currentLevel}";

            if (playerExpText != null)
                playerExpText.text = $"{animatedExp}/{currentExpToNext} EXP";

            // Level up effect
            if (progress >= 0.7f && stageResult.levelUpResult.didLevelUp && playerLevelText != null)
            {
                float pulse = 1f + (Mathf.Sin(Time.unscaledTime * 8f) * 0.1f);
                playerLevelText.transform.localScale = Vector3.one * pulse;
                playerLevelText.color = Color.Lerp(Color.white, Color.yellow, Mathf.PingPong(Time.unscaledTime * 2f, 1f));
            }

            yield return null;
        }

        // Reset level text effects
        if (playerLevelText != null)
        {
            playerLevelText.transform.localScale = Vector3.one;
            playerLevelText.color = Color.white;
        }
    }

    /// <summary>
    /// Animate First Clear Bonus
    /// </summary>
   

    /// <summary>
    /// อัปเดต Gems ขั้นสุดท้าย (รวมทุก bonus)
    /// </summary>
   
    

    /// <summary>
    /// ✅ แสดง Player EXP Animation
    /// </summary>
   

    /// <summary>
    /// ✅ แสดง Level Up Effect
    /// </summary>
    private System.Collections.IEnumerator ShowPlayerLevelUpEffect(int newLevel)
    {
        // เล่นเสียง level up
        if (victoryAudioSource != null && playerLevelUpSound != null)
        {
            victoryAudioSource.PlayOneShot(playerLevelUpSound);
        }

        // แสดง level up text effect
        if (playerLevelText != null)
        {
            Vector3 originalScale = playerLevelText.transform.localScale;
            Color originalColor = playerLevelText.color;

            // Pulse effect
            float pulseDuration = 0.5f;
            float elapsed = 0f;

            while (elapsed < pulseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / pulseDuration;

                float scale = 1f + (Mathf.Sin(progress * Mathf.PI * 4) * 0.2f);
                playerLevelText.transform.localScale = originalScale * scale;

                float alpha = 1f + (Mathf.Sin(progress * Mathf.PI * 4) * 0.3f);
                Color newColor = originalColor;
                newColor.a = alpha;
                playerLevelText.color = newColor;

                yield return null;
            }

            playerLevelText.transform.localScale = originalScale;
            playerLevelText.color = originalColor;
        }

        Debug.Log($"🎉 [StageCompleteUI] Player reached level {newLevel}!");
    }

    /// <summary>
    /// ✅ แสดง Player Level Up Rewards
    /// </summary>



    private int CalculateStageCompletionExp(string stageName)
    {
        

        // Fallback to default logic
        int baseExp = 50;

        if (stageName.Contains("PlayRoom1"))
            baseExp += 20;
        else if (stageName.Contains("PlayRoom2"))
            baseExp += 40;
        else if (stageName.Contains("PlayRoom3"))
            baseExp += 60;
        else if (stageName.Contains("PlayRoom4"))
            baseExp += 80;

        if (stageName.EndsWith("_1"))
            baseExp += 5;
        else if (stageName.EndsWith("_2"))
            baseExp += 10;
        else if (stageName.EndsWith("_3"))
            baseExp += 15;

        return baseExp;
    }

    /// <summary>
    /// เพิ่มรางวัลจาก Player Level Up (Stage Completion)
    /// </summary>
    private void AddStageCompletionPlayerLevelRewards(PlayerLevelUpResult result)
    {
        var currencyManager = FindObjectOfType<CurrencyManager>();
        if (currencyManager == null) return;

        if (result.goldReward > 0)
        {
            currencyManager.AddGold(result.goldReward, false);
            Debug.Log($"💰 [StageComplete] Added {result.goldReward} gold from player level up");
        }

        if (result.gemsReward > 0)
        {
            currencyManager.AddGems(result.gemsReward, false);
            Debug.Log($"💎 [StageComplete] Added {result.gemsReward} gems from player level up");
        }
    }

    /// <summary>
    /// แสดง notification ของ Player Level Up (Stage Completion)
    /// </summary>
    private void ShowStageCompletionPlayerLevelUpNotification(PlayerLevelUpResult result)
    {
        Debug.Log($"📢 [StageComplete] Player Level Up from Stage Completion!");
        Debug.Log($"   New Player Level: {result.newPlayerLevel}");
        Debug.Log($"   Stage Completion Rewards: {result.goldReward} gold, {result.gemsReward} gems");
    }
    private IEnumerator FadeInPanel()
    {
        CanvasGroup canvasGroup = stageCompletePanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) yield break;

        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeInDuration;

            // Ease in curve
            float alpha = Mathf.Lerp(0f, 1f, progress * progress);
            canvasGroup.alpha = alpha;

            yield return null;
        }

        canvasGroup.alpha = 1f;
        Debug.Log("✨ [StageCompleteUI] Fade in complete!");
    }
    /// <summary>
    /// แสดงเงินและเพชรที่ได้รับ
    /// </summary>
    private void DisplayCurrencyRewards(StageRewardData rewards)
    {
        if (goldRewardText != null)
        {
            goldRewardText.text = $"💰 {rewards.totalGoldEarned:N0}";
            goldRewardText.color = Color.yellow;
        }

        if (gemsRewardText != null)
        {
            gemsRewardText.text = $"💎 {rewards.totalGemsEarned}";
            gemsRewardText.color = Color.cyan;
        }
    }

    /// <summary>
    /// แสดงไอเทมที่ได้รับ
    /// </summary>
    private void DisplayItemRewards(StageRewardData rewards)
    {
        if (itemRewardsContainer == null || itemRewardPrefab == null)
        {
            Debug.LogWarning("[StageCompleteUI] Item rewards container or prefab not assigned!");
            return;
        }

        // เคลียร์ไอเทมเก่า
        ClearItemRewards();

        // สร้างไอเทมใหม่
        foreach (var itemReward in rewards.itemsEarned)
        {
            CreateItemRewardUI(itemReward);
        }

        Debug.Log($"[StageCompleteUI] Created {rewards.itemsEarned.Count} item reward UI elements");
    }

    /// <summary>
    /// แสดงสถิติการเล่น
    /// </summary>
    private void DisplayStatistics(StageRewardData rewards)
    {
        if (completionTimeText != null)
        {
            string timeFormat = FormatTime(rewards.stageCompletionTime);
            completionTimeText.text = $"⏱️ Time: {timeFormat}";
        }

        if (enemiesKilledText != null)
        {
            enemiesKilledText.text = $"⚔️ Enemies: {rewards.totalEnemiesKilled}";
        }
    }

    /// <summary>
    /// สร้าง UI element สำหรับแสดงไอเทม
    /// </summary>
    private void CreateItemRewardUI(ItemRewardInfo itemReward)
    {
        GameObject itemUI = Instantiate(itemRewardPrefab, itemRewardsContainer);

        // ใช้ ItemRewardUI script ถ้ามี
        ItemRewardUI rewardUI = itemUI.GetComponent<ItemRewardUI>();
        if (rewardUI != null)
        {
            rewardUI.SetItemData(itemReward);
            rewardUI.PlayAppearAnimation();
        }
        else
        {
            // Fallback: ใช้ component ธรรมดา
            SetupItemRewardBasic(itemUI, itemReward);
            StartCoroutine(AnimateItemReward(itemUI));
        }
    }

    /// <summary>
    /// Setup item reward แบบพื้นฐาน (ถ้าไม่มี ItemRewardUI script)
    /// </summary>
    private void SetupItemRewardBasic(GameObject itemUI, ItemRewardInfo itemReward)
    {
        // หา components ใน prefab
        Image itemIcon = itemUI.GetComponentInChildren<Image>();
        TextMeshProUGUI itemNameText = itemUI.GetComponentInChildren<TextMeshProUGUI>();

        // ตั้งค่าข้อมูล
        if (itemIcon != null && itemReward.itemIcon != null)
        {
            itemIcon.sprite = itemReward.itemIcon;
            itemIcon.color = itemReward.GetTierColor();
        }

        if (itemNameText != null)
        {
            string quantityText = itemReward.quantity > 1 ? $" x{itemReward.quantity}" : "";
            itemNameText.text = $"{itemReward.itemName}{quantityText}";
            itemNameText.color = itemReward.GetTierColor();
        }
    }

    /// <summary>
    /// Animation สำหรับไอเทม rewards
    /// </summary>
    private IEnumerator AnimateItemReward(GameObject itemUI)
    {
        Vector3 originalScale = itemUI.transform.localScale;
        itemUI.transform.localScale = Vector3.zero;

        // หน่วงเวลาสุ่มเพื่อให้ไอเทมปรากฏทีละตัว
        float randomDelay = Random.Range(0f, 0.3f);
        yield return new WaitForSecondsRealtime(randomDelay);

        float duration = 0.5f; // เพิ่มระยะเวลา animation
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // ใช้ unscaled เพราะ timeScale ถูกปรับ
            float progress = elapsed / duration;

            // Bounce effect
            float scale = Mathf.Lerp(0f, 1f, Mathf.Sqrt(progress));
            if (progress > 0.8f)
            {
                // เพิ่ม bounce เล็กน้อยที่ท้าย
                scale += Mathf.Sin((progress - 0.8f) * Mathf.PI * 5f) * 0.1f;
            }

            itemUI.transform.localScale = originalScale * scale;
            yield return null;
        }

        itemUI.transform.localScale = originalScale;
    }

    /// <summary>
    /// ลบไอเทม reward เก่าทั้งหมด
    /// </summary>
    private void ClearItemRewards()
    {
        if (itemRewardsContainer == null) return;

        for (int i = itemRewardsContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = itemRewardsContainer.GetChild(i);
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }
    }

    /// <summary>
    /// แปลงเวลาเป็นรูปแบบที่อ่านง่าย
    /// </summary>
    private string FormatTime(float totalSeconds)
    {
        int minutes = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);

        if (minutes > 0)
            return $"{minutes}m {seconds}s";
        else
            return $"{seconds}s";
    }

    /// <summary>
    /// ซ่อน Stage Complete UI
    /// </summary>
    public void HideStageComplete()
    {
        if (stageCompletePanel != null)
        {
            StartCoroutine(FadeOutPanel());
        }
    }
    private IEnumerator FadeOutPanel()
    {
        CanvasGroup canvasGroup = stageCompletePanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            stageCompletePanel.SetActive(false);
            yield break;
        }

        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < fadeInDuration * 0.5f) // Fade out เร็วกว่า fade in
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / (fadeInDuration * 0.5f);

            float alpha = Mathf.Lerp(startAlpha, 0f, progress);
            canvasGroup.alpha = alpha;

            yield return null;
        }

        canvasGroup.alpha = 0f;
        stageCompletePanel.SetActive(false);

        Debug.Log("🌙 [StageCompleteUI] Fade out complete!");
    }

    /// <summary>
    /// กลับไป Lobby - ใช้โค้ดเหมือน LoseScene
    /// </summary>
    private void BackToLobby()
    {
        Debug.Log("[StageCompleteUI] Going back to lobby...");

        // ✅ แน่ใจว่า timeScale กลับเป็น 1 ก่อน
        Time.timeScale = 1f;
        DailyQuestTracker.FlushProgressToQuests();

        StartCoroutine(BackToLobbyWithTransition());

        // 🆕 Force reset rewards ก่อนกลับ Lobby
        StageRewardTracker.ForceResetRewards();

        // ทำความสะอาด Network Components เหมือน LoseScene
        CleanupNetworkComponents();

        // โหลด Lobby Scene
        SceneManager.LoadScene("Lobby");
    }
   private IEnumerator BackToLobbyWithTransition()
{
    // ✅ แน่ใจว่า timeScale เป็น 1 ตั้งแต่เริ่มต้น
    Time.timeScale = 1f;
    
    // Fade out panel ก่อน
    yield return StartCoroutine(FadeOutPanel());

    // 🆕 Force reset rewards ก่อนกลับ Lobby
    StageRewardTracker.ForceResetRewards();

    // ทำความสะอาด Network Components เหมือน LoseScene
    CleanupNetworkComponents();

    // รอหน่วงเวลานิดหนึ่งก่อนโหลด scene
    yield return new WaitForSeconds(0.5f);

    // โหลด Lobby Scene
    SceneManager.LoadScene("Lobby");
}

    /// <summary>
    /// ทำความสะอาด Network Components - คัดลอกจาก LoseScene
    /// </summary>
    private void CleanupNetworkComponents()
    {
        // Shutdown NetworkRunner
        NetworkRunner runner = FindObjectOfType<NetworkRunner>();
        if (runner != null)
        {
            Debug.Log("Shutting down NetworkRunner from StageCompleteUI");
            runner.Shutdown();
        }

        // Cleanup PlayerSpawner
        PlayerSpawner spawner = FindObjectOfType<PlayerSpawner>();
        if (spawner != null)
        {
            spawner.CleanupOnGameExit();
        }

        // ลบ NetworkObjects ที่เหลืออยู่
        NetworkObject[] networkObjects = FindObjectsOfType<NetworkObject>();
        foreach (var obj in networkObjects)
        {
            if (obj != null)
            {
                Destroy(obj.gameObject);
            }
        }
    }

    /// <summary>
    /// เล่นเสียงชัยชนะ
    /// </summary>
    private void PlayVictorySound()
    {
        AudioManager.instance.PlaySFX(28, 1f);
    }

    /// <summary>
    /// คืนค่า Time Scale กลับเป็นปกติ
    /// </summary>
    private IEnumerator RestoreTimeScale()
    {
        // ✅ ปรับให้คืนค่าเร็วขึ้น หรือลบ method นี้ออกไปเลย
        yield return new WaitForSecondsRealtime(0.5f); // ลดจาก 3 วินาที

        // Smooth transition กลับเป็น normal time (เร็วขึ้น)
        float duration = 0.2f; // ลดจาก 1 วินาที
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;

            Time.timeScale = Mathf.Lerp(0.1f, 1f, progress);
            yield return null;
        }

        Time.timeScale = 1f;
        Debug.Log("⏰ [StageCompleteUI] Time scale restored to normal");
    }
    private void OnDestroy()
    {
        // ✅ แน่ใจว่า timeScale กลับเป็น 1 เมื่อ destroy object
        Time.timeScale = 1f;
        Debug.Log("[StageCompleteUI] OnDestroy: Time.timeScale reset to 1");
    }

    #region Debug Methods

    #endregion
}