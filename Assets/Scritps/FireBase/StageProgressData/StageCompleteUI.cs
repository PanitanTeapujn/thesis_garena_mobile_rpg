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
    /// แสดง Stage Complete UI - เรียกจาก RPC
    /// </summary>
    public void ShowStageComplete(string stageName)
    {
        if (stageCompletePanel == null)
        {
            Debug.LogWarning("[StageCompleteUI] Stage complete panel is not assigned!");
            return;
        }

        // แสดง panel
        stageCompletePanel.SetActive(true);

        // ตั้งค่าข้อความ
        if (stageNameText != null)
            stageNameText.text = stageName;

        if (congratsText != null)
            congratsText.text = "🎉 STAGE COMPLETED! 🎉";

        // เล่นเสียง
        PlayVictorySound();

        // หยุดเวลาชั่วขณะ (optional)
        Time.timeScale = 0.1f;
        StartCoroutine(RestoreTimeScale());

        Debug.Log($"🏆 [StageCompleteUI] Showing stage complete for: {stageName}");
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