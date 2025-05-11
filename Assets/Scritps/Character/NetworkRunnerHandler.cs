using Fusion;
using Fusion.Sockets;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class NetworkRunnerHandler : MonoBehaviour
{
    public NetworkRunner runnerPrefab;
    private PlayerSpawner _spawner;

    private async void Start()
    {
        _spawner = FindObjectOfType<PlayerSpawner>();
        if (_spawner == null)
        {
            Debug.LogError("PlayerSpawner not found! Please add PlayerSpawner to scene.");
            return;
        }

        var runner = Instantiate(runnerPrefab);
        runner.name = "NetworkRunner_Main"; // ตั้งชื่อเพื่อให้ง่ายต่อการตรวจสอบ

        // ลงทะเบียน PlayerSpawner กับ NetworkRunner
        runner.AddCallbacks(_spawner);
        Debug.Log($"Added {_spawner.name} as callback to {runner.name}");

        runner.ProvideInput = true;
        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = "MyRoom",
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        Debug.Log($"Starting game with mode: {startGameArgs.GameMode}, session: {startGameArgs.SessionName}");

        try
        {
            await runner.StartGame(startGameArgs);
            Debug.Log("Game started successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error starting game: {e.Message}");
        }
    }

    public void LoadScene(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
