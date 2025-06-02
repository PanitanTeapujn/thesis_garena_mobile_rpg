using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    public GameObject playerPrefab;
    private NetworkRunner _runner;

    // เพิ่มเมธอดนี้เพื่อเชื่อมต่อกับ NetworkRunner
    private void OnEnable()
    {
        // หา NetworkRunner ในฉาก
        _runner = FindObjectOfType<NetworkRunner>();
        if (_runner != null)
        {
            // ลงทะเบียน callback
            _runner.AddCallbacks(this);
            Debug.Log("PlayerSpawner registered with NetworkRunner");
        }
        else
        {
            Debug.LogError("NetworkRunner not found in the scene!");
        }
    }

    // เพิ่มเมธอดนี้เพื่อตรวจสอบสถานะ NetworkRunner
    private void Update()
    {
        if (_runner == null)
        {
            _runner = FindObjectOfType<NetworkRunner>();
            if (_runner != null)
            {
                _runner.AddCallbacks(this);
                Debug.Log("PlayerSpawner registered with NetworkRunner");
            }
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"OnPlayerJoined called for player {player}. IsServer: {runner.IsServer}");

        // *** สำคัญมาก: เฉพาะ Host/Server เท่านั้นที่สามารถ spawn ได้ ***
        if (!runner.IsServer)
        {
            Debug.Log("Not server, skipping spawn");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("playerPrefab is not assigned!");
            return;
        }

        // สร้างตำแหน่งสุ่มสำหรับ spawn
        Vector3 spawnPosition = new Vector3(UnityEngine.Random.Range(-5f, 5f), 0, UnityEngine.Random.Range(-5f, 5f));

        // Spawn ผู้เล่น (ทำได้เฉพาะ Host/Server)
        NetworkObject playerObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player, (runner, obj) =>
        {
            var bloodKnight = obj.GetComponent<BloodKnight>();

            if (bloodKnight == null)
            {
                Debug.LogError("BloodKnight component not found on spawned player!");
                return;
            }

            // ตั้งค่าเฉพาะสำหรับ Local Player
            if (player == runner.LocalPlayer)
            {
                // Camera จะถูกจัดการใน BloodKnight.Start() แล้ว
                Debug.Log("Local player spawned");

                // Tag player as local (optional)
                obj.gameObject.tag = "LocalPlayer";
            }
            else
            {
                Debug.Log("Remote player spawned");
                // Tag as remote player (optional)
                obj.gameObject.tag = "RemotePlayer";
            }

            // ไม่ต้องกำหนด Joystick แล้ว เพราะ InputController จะจัดการให้
            // Joystick จะถูกอ่านโดย InputController และส่งผ่าน Network Input System
        });

        if (playerObject != null)
        {
            Debug.Log($"Player spawned successfully at {spawnPosition}");

            // Optional: เพิ่มการแสดงชื่อผู้เล่น
            if (player == runner.LocalPlayer)
            {
                playerObject.gameObject.name = "LocalPlayer";
            }
            else
            {
                playerObject.gameObject.name = $"RemotePlayer_{player}";
            }
        }
        else
        {
            Debug.LogError("Failed to spawn player!");
        }
    }

    // เพิ่ม callback สำหรับเมื่อผู้เล่นออกจากเกม
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player} left the game");
        // Fusion จะจัดการ despawn ให้อัตโนมัติ
    }

    // ตรวจสอบว่า prefab มี NetworkObject หรือไม่
    private void Start()
    {
        if (playerPrefab != null)
        {
            NetworkObject networkObject = playerPrefab.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Debug.LogError("playerPrefab does not have NetworkObject component!");
            }
            else
            {
                Debug.Log("playerPrefab has NetworkObject component");
            }

            // ตรวจสอบ BloodKnight component
            BloodKnight bloodKnight = playerPrefab.GetComponent<BloodKnight>();
            if (bloodKnight == null)
            {
                Debug.LogError("playerPrefab does not have BloodKnight component!");
            }
        }
        else
        {
            Debug.LogError("playerPrefab is not assigned!");
        }
    }

    // เมธอดที่จำเป็นต้องมีสำหรับ INetworkRunnerCallbacks
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken token) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}