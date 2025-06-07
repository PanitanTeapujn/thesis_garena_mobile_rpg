using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;

public class SingleInputController : MonoBehaviour, INetworkRunnerCallbacks
{
    private static SingleInputController instance;

    private FixedJoystick movementJoystick;
    private FixedJoystick cameraJoystick;

    private NetworkInputData localInput;
    private NetworkRunner runner;

    private bool attackPressed = false;
    private bool skill1Pressed = false;
    private bool skill2Pressed = false; 
    private bool skill3Pressed = false;
    private bool skill4Pressed = false;

    private float attackPressedTime = 0f;
    private float skill1PressedTime = 0f;
    private float skill2PressedTime = 0f;
    private float skill3PressedTime = 0f;
    private float skill4PressedTime = 0f;

    private const float buttonPressDuration = 0.1f;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.Log("SingleInputController already exists, destroying duplicate");
            Destroy(this);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("SingleInputController created and set as singleton");

    }

    private void Start()
    {
        Debug.Log("SingleInputController Start");

        runner = FindObjectOfType<NetworkRunner>();
        FindJoysticks();
    }

    private void FindJoysticks()
    {
        // หา Joystick จาก scene เก่า (fallback)
        FixedJoystick[] joysticks = GameObject.FindObjectsOfType<FixedJoystick>();

        foreach (var js in joysticks)
        {
            if (js.gameObject.name == "JoystickCharacter" && movementJoystick == null)
            {
                movementJoystick = js;
                Debug.Log("Found Movement Joystick in scene");
            }
            else if (js.gameObject.name == "CameraJoystick" && cameraJoystick == null)
            {
                cameraJoystick = js;
                Debug.Log("Found Camera Joystick in scene");
            }
        }

        if (movementJoystick == null || cameraJoystick == null)
        {
            Debug.LogWarning("Some joysticks not found! Waiting for CombatUIManager to provide references.");
        }
    }

    // เพิ่ม method สำหรับ CombatUIManager อัพเดท joystick references
    public void UpdateJoystickReferences(FixedJoystick moveJoystick, FixedJoystick camJoystick)
    {
        if (moveJoystick != null)
        {
            movementJoystick = moveJoystick;
            Debug.Log("Movement joystick reference updated");
        }

        if (camJoystick != null)
        {
            cameraJoystick = camJoystick;
            Debug.Log("Camera joystick reference updated");
        }
    }

    private void Update()
    {
        if (runner == null)
        {
            runner = FindObjectOfType<NetworkRunner>();
            return;
        }

        if (!runner.IsRunning)
            return;

        if (movementJoystick == null || cameraJoystick == null)
        {
            FindJoysticks();
            return;
        }

        // อ่านค่า input จาก Joystick
        localInput.movementInput = new Vector2(
            movementJoystick.Horizontal,
            movementJoystick.Vertical
        );

        localInput.cameraRotationInput = cameraJoystick.Horizontal;

        // คำนวณทิศทางการมอง
        if (localInput.movementInput.magnitude > 0.1f)
        {
            Vector3 moveDir = new Vector3(localInput.movementInput.x, 0, localInput.movementInput.y);
            localInput.lookDirection = moveDir.normalized;
        }

        // Reset button states after duration
        if (attackPressed && Time.time - attackPressedTime > buttonPressDuration)
        {
            attackPressed = false;
        }
        if (skill1Pressed && Time.time - skill1PressedTime > buttonPressDuration)
        {
            skill1Pressed = false;
        }
        if (skill2Pressed && Time.time - skill2PressedTime > buttonPressDuration)
        {
            skill2Pressed = false;
        }
        if (skill3Pressed && Time.time - skill3PressedTime > buttonPressDuration)
        {
            skill3Pressed = false;
        }
        if (skill4Pressed && Time.time - skill4PressedTime > buttonPressDuration)
        {
            skill4Pressed = false;
        }

        // Set input states - แต่ละสกิลทำงานอิสระ
        localInput.attack = attackPressed;
        localInput.skill1 = skill1Pressed;
        localInput.skill2 = skill2Pressed;
        localInput.skill3 = skill3Pressed;
        localInput.skill4 = skill4Pressed;
    }

   
    public void SetAttackPressed()
    {
        attackPressed = true;
        attackPressedTime = Time.time;
        Debug.Log("Attack input set");
    }

    public void SetSkill1Pressed()
    {
        skill1Pressed = true;
        skill1PressedTime = Time.time;
        Debug.Log("Skill1 input set");
    }

    public void SetSkill2Pressed()
    {
        skill2Pressed = true;
        skill2PressedTime = Time.time;
        Debug.Log("Skill2 input set");
    }  
    public void SetSkill3Pressed()
    {
        skill3Pressed = true;
        skill3PressedTime = Time.time;
        Debug.Log("Skill3 input set");
    } 
    public void SetSkill4Pressed()
    {
        skill4Pressed = true;
        skill4PressedTime = Time.time;
        Debug.Log("Skill4 input set");
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        input.Set(localInput);
    }

    // Debug display
    void OnGUI()
    {
        if (runner != null && runner.IsRunning)
        {
            GUI.Label(new Rect(10, 70, 400, 20), $"Input System Active");
            GUI.Label(new Rect(10, 90, 400, 20), $"Movement: {localInput.movementInput}");
            GUI.Label(new Rect(10, 110, 400, 20), $"Camera: {localInput.cameraRotationInput}");
            GUI.Label(new Rect(10, 130, 400, 20), $"Attack: {localInput.attack}");
            GUI.Label(new Rect(10, 150, 400, 20), $"Skills: S1={localInput.skill1} S2={localInput.skill2} S3={localInput.skill3} S4={localInput.skill4}");

            // แสดงปุ่มที่กดค้างอยู่
            string pressedButtons = "";
            if (attackPressed) pressedButtons += "ATK ";
            if (skill1Pressed) pressedButtons += "S1 ";
            if (skill2Pressed) pressedButtons += "S2 ";
            if (skill3Pressed) pressedButtons += "S3 ";
            if (skill4Pressed) pressedButtons += "S4 ";
            GUI.Label(new Rect(10, 170, 400, 20), $"Pressed: {pressedButtons}");
        }
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        throw new NotImplementedException();
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        throw new NotImplementedException();
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        throw new NotImplementedException();
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        throw new NotImplementedException();
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        throw new NotImplementedException();
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        throw new NotImplementedException();
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        throw new NotImplementedException();
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        throw new NotImplementedException();
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        throw new NotImplementedException();
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        throw new NotImplementedException();
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        throw new NotImplementedException();
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }

    // ... rest of callback methods ...
}