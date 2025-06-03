using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Fusion;
using System.Collections.Generic;

public class WaitingRoomManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI roomCodeText;
    public Transform playerListParent;
    public GameObject playerItemPrefab;

    [Header("Buttons")]
    public Button startButton;
    public Button leaveButton;

    [Header("Player Ready Status")]
    public Toggle readyToggle;

    private Dictionary<PlayerRef, PlayerRoomInfo> players = new Dictionary<PlayerRef, PlayerRoomInfo>();
    private bool isHost;
    private string roomCode;

    // Network Runner reference
    private NetworkRunner runner;

    void Start()
    {
        isHost = PlayerPrefs.GetString("IsHost", "false") == "true";

        // Setup UI
        startButton.gameObject.SetActive(isHost);
        startButton.onClick.AddListener(StartGame);
        leaveButton.onClick.AddListener(LeaveRoom);
        readyToggle.onValueChanged.AddListener(OnReadyToggleChanged);

        // Generate or get room code
        if (isHost)
        {
            roomCode = GenerateRoomCode();
            PlayerPrefs.SetString("RoomCode", roomCode);
        }
        else
        {
            roomCode = PlayerPrefs.GetString("RoomCode", "");
        }

        roomCodeText.text = $"Room Code: {roomCode}";

        // Start networking
        StartNetworking();
    }

    async void StartNetworking()
    {
        runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;

        var startGameArgs = new StartGameArgs()
        {
            GameMode = isHost ? GameMode.Host : GameMode.Client,
            SessionName = roomCode,
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        await runner.StartGame(startGameArgs);
    }

    string GenerateRoomCode()
    {
        return Random.Range(100000, 999999).ToString();
    }

    void OnReadyToggleChanged(bool isReady)
    {
        // Send ready status to network
        if (runner != null && runner.IsRunning)
        {
            RPC_UpdateReadyStatus(isReady);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    void RPC_UpdateReadyStatus(bool isReady, RpcInfo info = default)
    {
        if (players.ContainsKey(info.Source))
        {
            players[info.Source].isReady = isReady;
            UpdatePlayerList();

            // Host checks if all ready
            if (isHost)
            {
                CheckAllPlayersReady();
            }
        }
    }

    void CheckAllPlayersReady()
    {
        bool allReady = true;
        foreach (var player in players.Values)
        {
            if (!player.isReady)
            {
                allReady = false;
                break;
            }
        }

        startButton.interactable = allReady && players.Count > 0;
    }

    void UpdatePlayerList()
    {
        // Clear existing list
        foreach (Transform child in playerListParent)
        {
            Destroy(child.gameObject);
        }

        // Create player items
        foreach (var kvp in players)
        {
            GameObject item = Instantiate(playerItemPrefab, playerListParent);
            PlayerRoomItem itemScript = item.GetComponent<PlayerRoomItem>();

            if (itemScript != null)
            {
                itemScript.Setup(kvp.Value, isHost, kvp.Key);
                itemScript.OnKickClicked += KickPlayer;
            }
        }
    }

    void KickPlayer(PlayerRef player)
    {
        if (isHost && runner != null)
        {
            RPC_KickPlayer(player);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_KickPlayer(PlayerRef player)
    {
        if (player == runner.LocalPlayer)
        {
            // ถูกเตะ
            LeaveRoom();
        }
    }

    void StartGame()
    {
        if (isHost && runner != null)
        {
            RPC_StartGame();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_StartGame()
    {
        runner.LoadScene("PlayRoom1");
    }

    void LeaveRoom()
    {
        if (runner != null)
        {
            runner.Shutdown();
        }

        SceneManager.LoadScene("Lobby");
    }
}

[System.Serializable]
public class PlayerRoomInfo
{
    public string playerName;
    public PlayerSelectionData.CharacterType characterType;
    public bool isReady;
    public bool isHost;
}