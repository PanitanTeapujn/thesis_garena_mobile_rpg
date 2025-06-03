using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
public class PlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Character Prefabs")]
    public GameObject bloodKnightPrefab;
    public GameObject archerPrefab;
    public GameObject assassinPrefab;
    public GameObject ironJuggernautPrefab;

    [Header("Network Manager Prefab")]
    public GameObject networkPlayerManagerPrefab; // ต้องสร้าง prefab นี้

    private NetworkRunner _runner;

    private HashSet<string> spawnRequests = new HashSet<string>();


    // Dictionary เพื่อเก็บข้อมูลตัวละครของแต่ละ player
    private Dictionary<PlayerRef, PlayerSelectionData.CharacterType> playerCharacters = new Dictionary<PlayerRef, PlayerSelectionData.CharacterType>();

    // Dictionary เพื่อเก็บ NetworkPlayerManager ของแต่ละ player
    private Dictionary<PlayerRef, NetworkPlayerManager> playerManagers = new Dictionary<PlayerRef, NetworkPlayerManager>();

    // Dictionary เพื่อเก็บ spawned characters ของแต่ละ player
    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    private void OnEnable()
    {
        _runner = FindObjectOfType<NetworkRunner>();
        if (_runner != null)
        {
            _runner.AddCallbacks(this);
            Debug.Log($"PlayerSpawner registered with NetworkRunner. IsServer: {_runner.IsServer}");
        }
        else
        {
            Debug.LogError("NetworkRunner not found in the scene!");
        }
    }

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
        Debug.Log($"[SPAWNER] OnPlayerJoined - Player: {player}, IsServer: {runner.IsServer}, LocalPlayer: {runner.LocalPlayer}");

        _runner = runner;

        // เฉพาะ Host/Server เท่านั้นที่สามารถ spawn ได้
        if (!runner.IsServer)
        {
            Debug.Log("Not server, skipping spawn");
            return;
        }

        // Spawn NetworkPlayerManager สำหรับ player นี้ก่อน
        if (networkPlayerManagerPrefab != null)
        {
            NetworkObject managerObject = runner.Spawn(
                networkPlayerManagerPrefab,
                Vector3.zero,
                Quaternion.identity,
                player
            );

            if (managerObject != null)
            {
                NetworkPlayerManager manager = managerObject.GetComponent<NetworkPlayerManager>();
                if (manager != null)
                {
                    playerManagers[player] = manager;
                    Debug.Log($"NetworkPlayerManager spawned for player {player}");
                }
            }
        }

        // *** แทนที่ code เดิม ด้วย code นี้ ***
        if (player != runner.LocalPlayer)
        {
            // Spawn player ที่ join ใหม่
            StartCoroutine(DelayedSpawn(player));

            // และ re-spawn local player ถ้ายังไม่มีใน spawnedCharacters
            if (!spawnedCharacters.ContainsKey(runner.LocalPlayer))
            {
                Debug.Log($"[SPAWNER] Also spawning host player");
                StartCoroutine(DelayedSpawn(runner.LocalPlayer));
            }
        }
        else
        {
            // ถ้าเป็น Host spawn ตัวเอง
            StartCoroutine(DelayedSpawn(player));
        }
    }
    IEnumerator DelayedSpawn(PlayerRef player)
    {
        string spawnKey = $"{player}_{Time.time}";

        Debug.Log($"[DELAYED SPAWN] Starting for {player}, Key: {spawnKey}");

        // ตรวจสอบว่ามี request ซ้ำหรือไม่
        if (spawnRequests.Contains(player.ToString()))
        {
            Debug.LogWarning($"[DUPLICATE REQUEST] Already spawning {player}");
            yield break;
        }

        spawnRequests.Add(player.ToString());

        yield return new WaitForSeconds(0.5f);

        Debug.Log($"[DELAYED SPAWN] Now spawning {player}");
        PlayerSelectionData.CharacterType selectedCharacter = PlayerSelectionData.GetSelectedCharacter();
        SpawnCharacterForPlayer(player, selectedCharacter);

        // ลบออกหลัง spawn เสร็จ
        spawnRequests.Remove(player.ToString());
    }
    // เมธอดใหม่สำหรับ spawn ตัวละคร
    public void SpawnCharacterForPlayer(PlayerRef player, PlayerSelectionData.CharacterType characterType)
    {
        // ใช้ runner ที่หาได้ล่าสุด
        NetworkRunner currentRunner = _runner;
        Debug.Log($"[SPAWN] Attempting to spawn for {player}, Already spawned: {spawnedCharacters.ContainsKey(player)}");
        Debug.Log($"[SPAWN] Called for {player}, Stack: {System.Environment.StackTrace}");

        if (spawnedCharacters.ContainsKey(player))
        {
            Debug.LogWarning($"Player {player} already has a character spawned!");
            return;
        }
      //  Debug.Log($"[SPAWNER] Spawning for {player}, Runner.LocalPlayer: {_runner.LocalPlayer}, IsServer: {_runner.IsServer}");

        if (currentRunner == null)
        {
            currentRunner = FindObjectOfType<NetworkRunner>();
            if (currentRunner == null)
            {
                Debug.LogError("NetworkRunner not found!");
                return;
            }
        }

        if (!currentRunner.IsServer)
        {
           // Debug.LogError($"Only server can spawn characters! IsServer: {currentRunner.IsServer}");
            return;
        }

        // ตรวจสอบว่า player นี้ spawn ตัวละครไปแล้วหรือยัง
        if (spawnedCharacters.ContainsKey(player))
        {
           // Debug.LogWarning($"Player {player} already has a character spawned!");
            return;
        }

        // บันทึกตัวละครที่ player เลือก
        playerCharacters[player] = characterType;

        // เลือก prefab ตามตัวละครที่เลือก
        GameObject prefabToSpawn = GetPrefabForCharacter(characterType);

        if (prefabToSpawn == null)
        {
            Debug.LogError($"No prefab found for character: {characterType}");
            return;
        }

        // สร้างตำแหน่งสุ่มสำหรับ spawn
        Vector3 spawnPosition = new Vector3(UnityEngine.Random.Range(-5f, 5f), 0, UnityEngine.Random.Range(-5f, 5f));

        // Spawn ตัวละคร
        NetworkObject playerObject = currentRunner.Spawn(prefabToSpawn, spawnPosition, Quaternion.identity, player, (runner, obj) =>
        {
            // ตั้งค่าเฉพาะสำหรับ Local Player
            if (player == runner.LocalPlayer)
            {
                Debug.Log($"Local player spawned as {characterType}");
                obj.gameObject.tag = "LocalPlayer";
            }
            else
            {
                Debug.Log($"Remote player spawned as {characterType}");
                obj.gameObject.tag = "RemotePlayer";
            }
        });

        if (playerObject != null)
        {
            // บันทึกว่า spawn แล้ว
            spawnedCharacters[player] = playerObject;

            Debug.Log($"Player {player} spawned successfully as {characterType} at {spawnPosition}");

            // ตั้งชื่อ GameObject ตามตัวละคร
            if (player == currentRunner.LocalPlayer)
            {
                playerObject.gameObject.name = $"LocalPlayer_{characterType}";
            }
            else
            {
                playerObject.gameObject.name = $"RemotePlayer_{player}_{characterType}";
            }
        }
        else
        {
            Debug.LogError($"Failed to spawn player as {characterType}!");
        }

    }

    private GameObject GetPrefabForCharacter(PlayerSelectionData.CharacterType character)
    {
        switch (character)
        {
            case PlayerSelectionData.CharacterType.BloodKnight:
                return bloodKnightPrefab;
            case PlayerSelectionData.CharacterType.Archer:
                return archerPrefab;
            case PlayerSelectionData.CharacterType.Assassin:
                return assassinPrefab;
            case PlayerSelectionData.CharacterType.IronJuggernaut:
                return ironJuggernautPrefab;
            default:
                Debug.LogWarning($"Unknown character type: {character}, using default");
                return bloodKnightPrefab;
        }
    }

    // เพิ่ม callback สำหรับเมื่อผู้เล่นออกจากเกม
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player {player} left the game");

        // ลบข้อมูลตัวละครของ player ที่ออกไป
        if (playerCharacters.ContainsKey(player))
        {
            playerCharacters.Remove(player);
        }

        // ลบ spawned character reference
        if (spawnedCharacters.ContainsKey(player))
        {
            spawnedCharacters.Remove(player);
        }

        // ลบ NetworkPlayerManager reference
        if (playerManagers.ContainsKey(player))
        {
            playerManagers.Remove(player);
        }
    }

    // ตรวจสอบว่า prefabs มี NetworkObject หรือไม่
    private void Start()
    {
        // ตรวจสอบ Blood Knight
        if (bloodKnightPrefab != null)
        {
            CheckPrefabComponents(bloodKnightPrefab, "Blood Knight");
        }
        else
        {
            Debug.LogError("Blood Knight prefab is not assigned!");
        }

        // ตรวจสอบ Archer
        if (archerPrefab != null)
        {
            CheckPrefabComponents(archerPrefab, "Archer");
        }
        else
        {
            Debug.LogError("Archer prefab is not assigned!");
        }

        // ตรวจสอบ Assassin
        if (assassinPrefab != null)
        {
            CheckPrefabComponents(assassinPrefab, "Assassin");
        }
        else
        {
            Debug.LogError("Assassin prefab is not assigned!");
        }

        // ตรวจสอบ Iron Juggernaut
        if (ironJuggernautPrefab != null)
        {
            CheckPrefabComponents(ironJuggernautPrefab, "Iron Juggernaut");
        }
        else
        {
            Debug.LogError("Iron Juggernaut prefab is not assigned!");
        }

        // ตรวจสอบ NetworkPlayerManager prefab
        if (networkPlayerManagerPrefab == null)
        {
            Debug.LogError("NetworkPlayerManager prefab is not assigned!");
        }
    }

    private void CheckPrefabComponents(GameObject prefab, string characterName)
    {
        NetworkObject networkObject = prefab.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError($"{characterName} prefab does not have NetworkObject component!");
        }
        else
        {
            Debug.Log($"{characterName} prefab has NetworkObject component");
        }

        // ตรวจสอบ Character component (Hero หรือ sub-class)
        Character character = prefab.GetComponent<Character>();
        if (character == null)
        {
            Debug.LogError($"{characterName} prefab does not have Character component!");
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