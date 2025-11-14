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
    private NetworkRunner _cachedRunner;
    private Hero[] _cachedHeroes;
    private float _lastHeroUpdateTime = 0f;

    private async void Start()
    {
        Debug.Log($"[NetworkRunnerHandler] Start - Platform: {Application.platform}");

        // หา PlayerSpawner
        _spawner = FindObjectOfType<PlayerSpawner>();
        if (_spawner == null)
        {
            Debug.LogError("PlayerSpawner not found!");
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

        // ✅ บังคับใช้ Single Mode บน Mobile
        GameMode gameMode = GameMode.Single;
        string sessionName = "Solo_" + Random.Range(100000, 999999);

        Debug.Log($"[NetworkRunnerHandler] Starting Single Player Mode");

        var startGameArgs = new StartGameArgs()
        {
            GameMode = gameMode,
            SessionName = sessionName,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        try
        {
            await runner.StartGame(startGameArgs);
            _cachedRunner = runner;

            Debug.Log($"✅ Game started - IsServer: {runner.IsServer}, LocalPlayer: {runner.LocalPlayer}");

            // ✅ Single mode = เป็น Server เสมอ
            if (runner.IsServer)
            {
                Debug.Log("Setting up EnemySpawner...");
                SetupEnemySpawner(runner);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error starting game: {e.Message}\n{e.StackTrace}");
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
    private void Update()
    {
       
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
