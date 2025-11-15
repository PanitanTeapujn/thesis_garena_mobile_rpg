using Fusion;
using Fusion.Sockets;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// 🔄 HostMigrationManager - จัดการการย้าย Host เมื่อ Host ตาย/ออก
/// </summary>
public class HostMigrationManager : MonoBehaviour, INetworkRunnerCallbacks
{
    private static HostMigrationManager instance;
    public static HostMigrationManager Instance => instance;

    private NetworkRunner runner;
    private bool isMigrating = false;

    [Header("Settings")]
    [SerializeField] private float migrationTimeout = 10f;
    [SerializeField] private bool autoMigrationEnabled = true;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        runner = FindObjectOfType<NetworkRunner>();

        if (runner != null)
        {
            runner.AddCallbacks(this);
            Debug.Log("[HostMigration] ✅ Manager initialized and registered");
        }
        else
        {
            Debug.LogWarning("[HostMigration] No NetworkRunner found on start");
        }
    }

    public void RegisterRunner(NetworkRunner newRunner)
    {
        if (runner != null && runner != newRunner)
        {
            runner.RemoveCallbacks(this);
        }

        runner = newRunner;

        if (runner != null)
        {
            runner.AddCallbacks(this);
            Debug.Log("[HostMigration] Runner registered");
        }
    }

    /// <summary>
    /// ✅ Fusion callback สำหรับ Host Migration
    /// </summary>
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.Log("[HostMigration] ========================================");
        Debug.Log("[HostMigration] 🔄 HOST MIGRATION EVENT!");
        Debug.Log("[HostMigration] ========================================");

        if (isMigrating)
        {
            Debug.LogWarning("[HostMigration] Already migrating - ignoring duplicate event");
            return;
        }

        // ✅ เรียก async method แทน coroutine
        PerformHostMigration(runner, hostMigrationToken);
    }
    /// <summary>
    /// ✅ ทำการ Host Migration
    /// </summary>
    private async void PerformHostMigration(NetworkRunner runner, HostMigrationToken token)
    {
        isMigrating = true;

        Debug.Log("[HostMigration] 1️⃣ Starting migration process...");

        // แสดง UI notification
        ShowMigrationUI(true);

        await System.Threading.Tasks.Task.Delay(500); // แทน yield return

        Debug.Log("[HostMigration] 2️⃣ Shutting down current session...");

        // Shutdown session เดิม
        runner.Shutdown(shutdownReason: ShutdownReason.HostMigration);

        await System.Threading.Tasks.Task.Delay(500);

        Debug.Log("[HostMigration] 3️⃣ Starting new session with migration token...");

        // สร้าง session ใหม่ด้วย migration token
        var result = await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Shared, // เปลี่ยนจาก Host เป็น Shared
            HostMigrationToken = token,
            HostMigrationResume = HostMigrationResume,
            SceneManager = runner.GetComponent<INetworkSceneManager>()
        });

        if (result.Ok)
        {
            Debug.Log("[HostMigration] ✅ Migration successful!");
            Debug.Log($"[HostMigration] New server: {runner.IsServer}");
            Debug.Log($"[HostMigration] Local player: {runner.LocalPlayer}");
        }
        else
        {
            Debug.LogError($"[HostMigration] ❌ Migration failed: {result.ShutdownReason}");
        }

        isMigrating = false;
        ShowMigrationUI(false);
    }

    /// <summary>
    /// ✅ Resume callback หลัง migration
    /// </summary>
    private void HostMigrationResume(NetworkRunner runner)
    {
        Debug.Log("[HostMigration] ========================================");
        Debug.Log("[HostMigration] 🎯 HOST MIGRATION RESUMED");
        Debug.Log($"  - IsServer: {runner.IsServer}");
        Debug.Log($"  - IsClient: {runner.IsClient}");
        Debug.Log($"  - LocalPlayer: {runner.LocalPlayer}");
        Debug.Log("[HostMigration] ========================================");

        // ✅ ลบการเช็ค HasCallbacks ออก - register ทับได้เลย
        runner.AddCallbacks(this);

        // Notify PlayerSpawner
        PlayerSpawner spawner = FindObjectOfType<PlayerSpawner>();
        if (spawner != null)
        {
            spawner.OnHostMigration(runner, null);
        }

        // อัพเดท UI
        UpdateUIAfterMigration(runner);
    }

    /// <summary>
    /// ✅ แสดง/ซ่อน Migration UI
    /// </summary>
    private void ShowMigrationUI(bool show)
    {
        if (show)
        {
            Debug.Log("[HostMigration] 📢 MIGRATING HOST - PLEASE WAIT...");
            // TODO: แสดง loading screen หรือ notification
        }
        else
        {
            Debug.Log("[HostMigration] ✅ Migration UI hidden");
            // TODO: ซ่อน loading screen
        }
    }

    /// <summary>
    /// ✅ อัพเดท UI หลัง migration
    /// </summary>
    private void UpdateUIAfterMigration(NetworkRunner runner)
    {
        Debug.Log("[HostMigration] Updating UI after migration...");

        // อัพเดท CombatUI
        CombatUIManager combatUI = FindObjectOfType<CombatUIManager>();
        if (combatUI != null)
        {
            combatUI.UpdateUI();
        }

        // อัพเดท WorldSpaceUI
       
    }

    #region INetworkRunnerCallbacks Implementation

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[HostMigration] Player joined after migration: {player}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[HostMigration] Shutdown: {shutdownReason}");
    }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    #endregion

    private void OnDestroy()
    {
        if (runner != null)
        {
            runner.RemoveCallbacks(this);
        }

        if (instance == this)
        {
            instance = null;
        }
    }
}