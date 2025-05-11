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
        Debug.Log($"OnPlayerJoined called for player {player}");

        if (playerPrefab == null)
        {
            Debug.LogError("playerPrefab is not assigned!");
            return;
        }

        // สร้างตำแหน่งสุ่มสำหรับ spawn
        Vector3 spawnPosition = new Vector3(UnityEngine.Random.Range(-5f, 5f), 0, UnityEngine.Random.Range(-5f, 5f));

        // Spawn ผู้เล่น
        NetworkObject playerObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player, (runner, obj) =>
        {
            var bloodKnight = obj.GetComponent<BloodKnight>();
            BloodKnight knight = obj.GetComponent<BloodKnight>();

            if (player == runner.LocalPlayer)
            {
                knight.cameraTransform = Camera.main.transform;
                Debug.Log("Camera assigned to BloodKnight from PlayerSpawner");
            }
            // ค้นหา Joystick ทั้งหมดใน scene
            FixedJoystick[] joysticks = GameObject.FindObjectsOfType<FixedJoystick>();

            foreach (var js in joysticks)
            {
                if (js.gameObject.name == "JoystickCharacter") // หรือชื่อที่ใช้สำหรับการเดิน
                {
                    bloodKnight.joystick = js;
                }
                else if (js.gameObject.name == "CameraJoystick") // หรือชื่อที่ใช้สำหรับหมุนกล้อง
                {
                    bloodKnight.joystickCamera = js;
                }
            }

            if (bloodKnight.joystick == null || bloodKnight.joystickCamera == null)
            {
                Debug.LogWarning("Joystick or CameraJoystick not assigned correctly!");
            }
        });


        if (playerObject != null)
        {
            Debug.Log($"Player spawned successfully at {spawnPosition}");
        }
        else
        {
            Debug.LogError("Failed to spawn player!");
        }
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
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
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