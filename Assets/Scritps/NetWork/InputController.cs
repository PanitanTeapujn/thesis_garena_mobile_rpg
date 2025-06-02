using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;

public class InputController : MonoBehaviour, INetworkRunnerCallbacks
{
    // Reference ไปยัง Joysticks
    private FixedJoystick movementJoystick;
    private FixedJoystick cameraJoystick;

    // เก็บ input data ไว้ส่ง
    private NetworkInputData localInput;

    // เก็บ reference ของ NetworkRunner
    private NetworkRunner runner;

    private void Start()
    {
        // หา NetworkRunner
        runner = FindObjectOfType<NetworkRunner>();

        // หา Joysticks ใน scene
        FindJoysticks();
    }

    private void FindJoysticks()
    {
        // ค้นหา Joystick ทั้งหมดใน scene
        FixedJoystick[] joysticks = GameObject.FindObjectsOfType<FixedJoystick>();

        foreach (var js in joysticks)
        {
            if (js.gameObject.name == "JoystickCharacter")
            {
                movementJoystick = js;
                Debug.Log("Found Movement Joystick");
            }
            else if (js.gameObject.name == "CameraJoystick")
            {
                cameraJoystick = js;
                Debug.Log("Found Camera Joystick");
            }
        }

        if (movementJoystick == null || cameraJoystick == null)
        {
            Debug.LogError("Joysticks not found! Make sure they exist in the scene.");
        }
    }

    private void Update()
    {
        // ตรวจสอบว่า runner พร้อมและเป็น local player
        if (runner == null || !runner.IsRunning)
            return;

        // อ่านค่า input จาก Joystick ทุก frame
        if (movementJoystick != null && cameraJoystick != null)
        {
            localInput.movementInput = new Vector2(
                movementJoystick.Horizontal,
                movementJoystick.Vertical
            );

            localInput.cameraRotationInput = cameraJoystick.Horizontal;

            // คำนวณทิศทางการมอง (สำหรับการซิงค์การหัน)
            if (localInput.movementInput.magnitude > 0.1f)
            {
                Vector3 moveDir = new Vector3(localInput.movementInput.x, 0, localInput.movementInput.y);
                localInput.lookDirection = moveDir.normalized;
            }
        }
    }

    // *** สำคัญมาก: OnInput จะถูกเรียกโดย Fusion เพื่อส่ง input ***
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // ส่ง input data ที่เก็บไว้ไปให้ Fusion
        input.Set(localInput);
    }

    // Callbacks อื่นๆ ที่จำเป็น (ไม่ต้องทำอะไร)
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
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