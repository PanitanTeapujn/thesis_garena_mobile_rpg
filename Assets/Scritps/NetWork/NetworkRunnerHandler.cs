using Fusion;
using Fusion.Sockets;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class NetworkRunnerHandler : MonoBehaviour
{
    public NetworkRunner runnerPrefab;
    private PlayerSpawner _spawner;
    private SingleInputController _inputController;
    public GameObject enemySpawnerPrefab;

    private async void Start()
    {
        // หา PlayerSpawner
        _spawner = FindObjectOfType<PlayerSpawner>();
        if (_spawner == null)
        {
            Debug.LogError("PlayerSpawner not found! Please add PlayerSpawner to scene.");
            return;
        }

        // สร้าง NetworkRunner
        var runner = Instantiate(runnerPrefab);
        runner.name = "NetworkRunner_Main";
        runner.ProvideInput = true;

        // หาหรือสร้าง SingleInputController
        _inputController = FindObjectOfType<SingleInputController>();
        if (_inputController == null)
        {
            GameObject inputControllerGO = new GameObject("SingleInputController");
            _inputController = inputControllerGO.AddComponent<SingleInputController>();
            Debug.Log("Created SingleInputController");
        }

        // ลงทะเบียน callbacks
        runner.AddCallbacks(_spawner);
        runner.AddCallbacks(_inputController);

        // 🔄 กำหนด SessionName ตามโหมด
        string gameModeSetting = PlayerPrefs.GetString("GameMode", "Coop");
        bool isSolo = gameModeSetting == "Solo";
        string sessionName = isSolo ? "Solo_" + Random.Range(100000, 999999) : "MainRoom";

        Debug.Log($"Game mode: {(isSolo ? "Solo" : "Co-op")}, Session name: {sessionName}");

        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = sessionName,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        Debug.Log($"Starting game with mode: {startGameArgs.GameMode}, session: {startGameArgs.SessionName}");

        try
        {
            await runner.StartGame(startGameArgs);
            Debug.Log($"Game started successfully - IsServer: {runner.IsServer}, LocalPlayer: {runner.LocalPlayer}");

            // ✅ Spawn EnemySpawner เฉพาะฝั่ง Server
            if (runner.IsServer)
            {
                Debug.Log("Running as Server - Setting up EnemySpawner");
                SetupEnemySpawner(runner);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error starting game: {e.Message}");
        }
    }

    private void SetupEnemySpawner(NetworkRunner runner)
    {
        if (enemySpawnerPrefab != null)
        {
            Debug.Log("Spawning EnemySpawner from prefab");
            runner.Spawn(enemySpawnerPrefab);
        }
        else
        {
            Debug.LogError("EnemySpawner prefab not assigned!");
        }
    }

    private void OnDestroy()
    {
        if (_spawner != null)
        {
            _spawner.CleanupOnGameExit();
        }
    }

    public void LoadScene(string sceneName)
    {
        CleanupNetworkComponents();
        SceneManager.LoadScene(sceneName);
    }

    private void CleanupNetworkComponents()
    {
        NetworkRunner runner = FindObjectOfType<NetworkRunner>();
        if (runner != null)
        {
            Debug.Log("Shutting down NetworkRunner");
            runner.Shutdown();
        }

        if (_spawner != null)
        {
            _spawner.CleanupOnGameExit();
        }
    }
}
