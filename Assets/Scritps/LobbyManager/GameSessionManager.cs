using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;

public class GameSessionManager : MonoBehaviour
{
    private NetworkRunner runner;
    private string gameMode;
    private string roomCode;
    private bool isHost;

    void Start()
    {
        // ปิด FusionBootstrapDebugGUI
        DisableFusionDebugGUI();

        // อ่านข้อมูลการตั้งค่า
        gameMode = PlayerPrefs.GetString("GameMode", "Solo");
        roomCode = PlayerPrefs.GetString("RoomCode", "");
        isHost = PlayerPrefs.GetString("IsHost", "true") == "true";

        Debug.Log($"[GameSession] Mode: {gameMode}, Room: {roomCode}, Host: {isHost}");

        // เริ่ม Networking
        StartNetworking();
    }

    void DisableFusionDebugGUI()
    {
        // ปิด FusionBootstrapDebugGUI ทุกตัวที่อาจมี
        FusionBootstrapDebugGUI[] debugGUIs = FindObjectsOfType<FusionBootstrapDebugGUI>();
        foreach (var gui in debugGUIs)
        {
            gui.gameObject.SetActive(false);
            Debug.Log("[GameSession] Disabled FusionBootstrapDebugGUI");
        }

        // ปิดจาก Bootstrap ถ้ามี
       
    }

    async void StartNetworking()
    {
        // สร้าง NetworkRunner
        runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;

        GameMode networkMode;
        string sessionName;

        if (gameMode == "Solo")
        {
            // ✅ โหมด Solo - ใช้ Host แต่ session ที่แตกต่างกัน
            networkMode = GameMode.Host;
            sessionName = roomCode; // ใช้ unique session ที่สร้างไว้

            Debug.Log($"[GameSession] Starting SOLO session: {sessionName}");
        }
        else
        {
            // โหมด Party - ใช้ระบบเดิม
            networkMode = isHost ? GameMode.Host : GameMode.Client;
            sessionName = roomCode;

            Debug.Log($"[GameSession] Starting PARTY session: {sessionName} as {networkMode}");
        }

        var startGameArgs = new StartGameArgs()
        {
            GameMode = networkMode,
            SessionName = sessionName,
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        try
        {
            var result = await runner.StartGame(startGameArgs);

            if (result.Ok)
            {
                Debug.Log($"✅ [GameSession] {gameMode} session started successfully!");

                // ปิด FusionBootstrapDebugGUI อีกครั้งหลังจากเริ่ม network
                Invoke("DisableFusionDebugGUI", 0.5f);
            }
            else
            {
                Debug.LogError($"❌ [GameSession] Failed to start session: {result.ShutdownReason}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ [GameSession] Exception starting session: {e.Message}");
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        // ปิด FusionBootstrapDebugGUI เมื่อกลับมา
        if (!pauseStatus)
        {
            Invoke("DisableFusionDebugGUI", 0.1f);
        }
    }

    void Update()
    {
        // ปิด FusionBootstrapDebugGUI ต่อเนื่อง (ทุก 2 วินาที)
        if (Time.frameCount % 120 == 0)
        {
            DisableFusionDebugGUI();
        }
    }

    void OnDestroy()
    {
        if (runner != null && runner.IsRunning)
        {
            runner.Shutdown();
        }
    }

    // สำหรับ Debug
    [ContextMenu("Force Disable Debug GUI")]
    void Debug_DisableGUI()
    {
        DisableFusionDebugGUI();
    }

    [ContextMenu("Log Session Info")]
    void Debug_LogSessionInfo()
    {
        Debug.Log($"=== Session Info ===");
        Debug.Log($"Game Mode: {gameMode}");
        Debug.Log($"Room Code: {roomCode}");
        Debug.Log($"Is Host: {isHost}");
        Debug.Log($"Runner Active: {runner != null && runner.IsRunning}");

        if (runner != null)
        {
            Debug.Log($"Session Name: {runner.SessionInfo?.Name}");
        }
    }
}