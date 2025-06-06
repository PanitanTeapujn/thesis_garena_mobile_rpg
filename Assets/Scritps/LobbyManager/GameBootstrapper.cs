using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System;

public class GameBootstrapper : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner runner;

    private void Start()
    {
        InitNetworkRunner();
    }

    private async void InitNetworkRunner()
    {
        // อย่าสร้างซ้ำหากมี NetworkRunner อยู่แล้ว
        if (FindObjectOfType<NetworkRunner>() != null)
            return;

        string gameMode = PlayerPrefs.GetString("GameMode", "Solo");
        bool isHost = PlayerPrefs.GetString("IsHost", "true") == "true";
        string roomCode = PlayerPrefs.GetString("RoomCode", "DefaultRoom");

        runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;

        GameMode selectedMode = gameMode == "Solo" ? GameMode.Single :
                                isHost ? GameMode.Host : GameMode.Client;

        Debug.Log($"[GameBootstrapper] Starting Runner | Mode: {selectedMode} | Room: {roomCode}");

        var sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        var result = await runner.StartGame(new StartGameArgs
        {
            GameMode = selectedMode,
            SessionName = roomCode,
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex), // ✅ แก้ตรงนี้
            SceneManager = sceneManager
        });

        if (result.Ok)
        {
            Debug.Log($"[GameBootstrapper] NetworkRunner started in {selectedMode} mode.");
        }
        else
        {
            Debug.LogError($"[GameBootstrapper] Failed to start runner: {result.ShutdownReason}");
        }
    }

    // INetworkRunnerCallbacks
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
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

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }
}
