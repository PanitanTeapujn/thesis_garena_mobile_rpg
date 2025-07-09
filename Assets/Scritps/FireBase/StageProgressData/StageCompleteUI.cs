using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Fusion;

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

    [Header("Audio")]
    public AudioSource victoryAudioSource;
    public AudioClip victorySound;

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

        // 🆕 Debug ข้อมูล rewards ก่อนแสดง
        var preDisplayRewards = StageRewardTracker.GetCurrentRewards();
        Debug.Log($"[StageCompleteUI] 🔍 Pre-display rewards: Gold={preDisplayRewards.totalGoldEarned}, Items={preDisplayRewards.itemsEarned.Count}");

        // หยุดการติดตาม rewards และคำนวณเวลา
        StageRewardTracker.Instance.StopStageTracking();

        // 🆕 Debug ข้อมูลหลัง StopTracking
        var postStopRewards = StageRewardTracker.GetCurrentRewards();
        Debug.Log($"[StageCompleteUI] 🔍 Post-stop rewards: Gold={postStopRewards.totalGoldEarned}, Items={postStopRewards.itemsEarned.Count}");

        // แสดง panel
        stageCompletePanel.SetActive(true);

        // ตั้งค่าข้อความพื้นฐาน
        if (stageNameText != null)
            stageNameText.text = stageName;

        if (congratsText != null)
            congratsText.text = "🎉 STAGE COMPLETED! 🎉";

        // แสดงข้อมูล rewards
        DisplayStageRewards();

        // เล่นเสียง
        PlayVictorySound();

        // หยุดเวลาชั่วขณะ (optional)
        Time.timeScale = 0.1f;
        StartCoroutine(RestoreTimeScale());

        Debug.Log($"🏆 [StageCompleteUI] Showing stage complete with rewards for: {stageName}");
    }

    /// <summary>
    /// แสดงข้อมูล rewards ที่ได้รับ
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

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // ใช้ unscaled เพราะ timeScale ถูกปรับ
            float progress = elapsed / duration;
            float scale = Mathf.Lerp(0f, 1f, Mathf.Sqrt(progress)); // Ease out curve

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
            stageCompletePanel.SetActive(false);
    }

    /// <summary>
    /// กลับไป Lobby - ใช้โค้ดเหมือน LoseScene
    /// </summary>
    private void BackToLobby()
    {
        Debug.Log("[StageCompleteUI] Going back to lobby...");

        // 🆕 Force reset rewards ก่อนกลับ Lobby
        StageRewardTracker.ForceResetRewards();

        // คืนค่า Time Scale ปกติก่อน
        Time.timeScale = 1f;

        // ทำความสะอาด Network Components เหมือน LoseScene
        CleanupNetworkComponents();

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
        if (victoryAudioSource != null && victorySound != null)
        {
            victoryAudioSource.PlayOneShot(victorySound);
        }
    }

    /// <summary>
    /// คืนค่า Time Scale กลับเป็นปกติ
    /// </summary>
    private IEnumerator RestoreTimeScale()
    {
        yield return new WaitForSecondsRealtime(2f); // รอ 2 วินาทีจริง (ไม่ได้รับผลจาก timeScale)
        Time.timeScale = 1f;
    }

    #region Debug Methods
    [ContextMenu("🧪 Test: Show Stage Complete")]
    public void TestShowStageComplete()
    {
        if (Application.isPlaying)
        {
            ShowStageComplete("Test Stage");
        }
    }

    [ContextMenu("🧪 Test: Hide Stage Complete")]
    public void TestHideStageComplete()
    {
        if (Application.isPlaying)
        {
            HideStageComplete();
        }
    }
    #endregion
}