using Fusion;
using Fusion.Sockets;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.Collections;

public class NetworkRunnerHandler : MonoBehaviour
{
    public NetworkRunner runnerPrefab;
    public GameObject enemySpawnerPrefab;

    [Header("🔄 Host Migration")]
    [SerializeField] private GameObject hostMigrationManagerPrefab;
    [SerializeField] private bool enableHostMigration = true;

    private PlayerSpawner _spawner;
    private SingleInputController _inputController;
    private HostMigrationManager _hostMigrationManager;

    private async void Start()
    {
        Debug.Log($"[NetworkRunnerHandler] ========================================");
        Debug.Log($"[NetworkRunnerHandler] Start - Scene: {SceneManager.GetActiveScene().name}");
        Debug.Log($"[NetworkRunnerHandler] ========================================");

        // ✅ Setup HostMigrationManager
        SetupHostMigrationManager();

        // ✅ หา PlayerSpawner
        _spawner = FindObjectOfType<PlayerSpawner>();
        if (_spawner == null)
        {
            Debug.LogError("[NetworkRunnerHandler] PlayerSpawner not found!");
            return;
        }

        // ✅ หา/สร้าง InputController
        _inputController = FindObjectOfType<SingleInputController>();
        if (_inputController == null)
        {
            Debug.Log("[NetworkRunnerHandler] Creating SingleInputController...");
            GameObject inputControllerGO = new GameObject("SingleInputController");
            _inputController = inputControllerGO.AddComponent<SingleInputController>();
        }

        // ✅ หา existing runner
        NetworkRunner existingRunner = FindObjectOfType<NetworkRunner>();

        if (existingRunner != null)
        {
            if (!existingRunner.IsRunning)
            {
                Debug.LogWarning("[NetworkRunnerHandler] ⚠️ Found NetworkRunner but NOT running! Destroying...");
                Destroy(existingRunner.gameObject);
                existingRunner = null;
            }
            else
            {
                Debug.Log("[NetworkRunnerHandler] ✅ Using existing NetworkRunner");
                Debug.Log($"  - IsServer: {existingRunner.IsServer}");
                Debug.Log($"  - IsRunning: {existingRunner.IsRunning}");
                Debug.Log($"  - LocalPlayer: {existingRunner.LocalPlayer}");

                existingRunner.AddCallbacks(_spawner);
                existingRunner.AddCallbacks(_inputController);

                // ✅ Register runner กับ HostMigrationManager
                if (_hostMigrationManager != null)
                {
                    _hostMigrationManager.RegisterRunner(existingRunner);
                }

                Debug.Log("[NetworkRunnerHandler] ✅ Callbacks registered");

                if (existingRunner.IsServer)
                {
                    StartCoroutine(DelayedSpawnHost(existingRunner));
                    StartCoroutine(DelayedSpawnExistingPlayers(existingRunner));
                    StartCoroutine(DelayedSetupEnemySpawner(existingRunner));
                }

                return;
            }
        }

        // ✅ สร้าง runner ใหม่
        Debug.Log("[NetworkRunnerHandler] 🆕 Creating new NetworkRunner");

        var runner = Instantiate(runnerPrefab);
        runner.name = "NetworkRunner_Main";
        runner.ProvideInput = true;

        runner.AddCallbacks(_spawner);
        runner.AddCallbacks(_inputController);

        // ✅ Register กับ HostMigrationManager
        if (_hostMigrationManager != null)
        {
            _hostMigrationManager.RegisterRunner(runner);
        }

        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.Single,
            SessionName = "Solo_" + Random.Range(100000, 999999),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        try
        {
            await runner.StartGame(startGameArgs);
            Debug.Log($"✅ Solo game started - IsServer: {runner.IsServer}, IsRunning: {runner.IsRunning}");

            if (runner.IsServer)
            {
                StartCoroutine(DelayedSetupEnemySpawner(runner));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error starting game: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// ✅ Setup HostMigrationManager
    /// </summary>
    private void SetupHostMigrationManager()
    {
        if (!enableHostMigration)
        {
            Debug.Log("[NetworkRunnerHandler] Host Migration disabled");
            return;
        }

        // หา existing manager
        _hostMigrationManager = FindObjectOfType<HostMigrationManager>();

        if (_hostMigrationManager == null)
        {
            if (hostMigrationManagerPrefab != null)
            {
                Debug.Log("[NetworkRunnerHandler] Creating HostMigrationManager from prefab");
                GameObject managerObj = Instantiate(hostMigrationManagerPrefab);
                _hostMigrationManager = managerObj.GetComponent<HostMigrationManager>();
            }
            else
            {
                Debug.Log("[NetworkRunnerHandler] Creating HostMigrationManager GameObject");
                GameObject managerObj = new GameObject("HostMigrationManager");
                _hostMigrationManager = managerObj.AddComponent<HostMigrationManager>();
            }

            DontDestroyOnLoad(_hostMigrationManager.gameObject);
            Debug.Log("[NetworkRunnerHandler] ✅ HostMigrationManager created");
        }
        else
        {
            Debug.Log("[NetworkRunnerHandler] ✅ Using existing HostMigrationManager");
        }
    }

    private IEnumerator DelayedSpawnHost(NetworkRunner runner)
    {
        Debug.Log("[NetworkRunnerHandler] 🏠 Spawning HOST player...");

        yield return new WaitForSeconds(0.3f);

        if (_spawner != null && runner != null && runner.IsRunning)
        {
            _spawner.OnPlayerJoined(runner, runner.LocalPlayer);
            Debug.Log($"[NetworkRunnerHandler] ✅ Host player spawn triggered: {runner.LocalPlayer}");
        }
        else
        {
            Debug.LogError("[NetworkRunnerHandler] ❌ Cannot spawn host - spawner or runner invalid!");
        }
    }

    private IEnumerator DelayedSpawnExistingPlayers(NetworkRunner runner)
    {
        Debug.Log("[NetworkRunnerHandler] 🔄 Checking for remote players to spawn...");

        yield return new WaitForSeconds(1f);

        if (_spawner == null)
        {
            Debug.LogError("[NetworkRunnerHandler] PlayerSpawner is null!");
            yield break;
        }

        if (runner == null || !runner.IsRunning)
        {
            Debug.LogError("[NetworkRunnerHandler] Runner is not running!");
            yield break;
        }

        foreach (var player in runner.ActivePlayers)
        {
            if (player != runner.LocalPlayer)
            {
                Debug.Log($"[NetworkRunnerHandler] 🎯 Spawning remote player: {player}");
                _spawner.OnPlayerJoined(runner, player);
            }
            else
            {
                Debug.Log($"[NetworkRunnerHandler] ⏭️ Skipping LocalPlayer: {player}");
            }
        }
    }

    private IEnumerator DelayedSetupEnemySpawner(NetworkRunner runner)
    {
        Debug.Log("[NetworkRunnerHandler] 🔄 Waiting before spawning EnemySpawner...");

        yield return new WaitForSeconds(1.5f);

        if (runner == null || !runner.IsRunning)
        {
            Debug.LogWarning("[NetworkRunnerHandler] Runner stopped - skipping EnemySpawner");
            yield break;
        }

        EnemySpawner existing = FindObjectOfType<EnemySpawner>();
        if (existing != null)
        {
            Debug.Log("[NetworkRunnerHandler] ⏭️ EnemySpawner already exists");
            yield break;
        }

        if (enemySpawnerPrefab != null)
        {
            try
            {
                Debug.Log("[NetworkRunnerHandler] 🎯 Spawning EnemySpawner prefab");

                var spawned = runner.Spawn(enemySpawnerPrefab);

                if (spawned != null)
                {
                    Debug.Log("[NetworkRunnerHandler] ✅ EnemySpawner spawned");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[NetworkRunnerHandler] ❌ Exception: {e.Message}");
            }
        }
    }

    private void OnDestroy()
    {
        Debug.Log("[NetworkRunnerHandler] OnDestroy called");

        if (_spawner != null)
        {
            _spawner.CleanupOnGameExit();
        }
    }
}