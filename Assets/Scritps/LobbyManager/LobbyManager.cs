using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI characterTypeText;

    [Header("Buttons")]
    public Button playButton;
    public Button shopButton;
    public Button inventoryButton;
    public Button settingsButton;
    public Button logoutButton;

    [Header("Play Mode Panel")]
    public GameObject playModePanel;
    public Button soloButton;
    public Button partyButton;
    public Button closePanelButton;

    [Header("Party Options Panel")]
    public GameObject partyOptionsPanel;
    public Button createRoomButton;
    public Button joinRoomButton;
    public Button backToModeButton;

    [Header("Join Room Panel")]
    public GameObject joinRoomPanel;
    public TMP_InputField roomCodeInput;
    public Button joinButton;
    public Button backToPartyButton;

    void Start()
    {
        // แสดงข้อมูลผู้เล่น
        playerNameText.text = PlayerPrefs.GetString("PlayerName", "Unknown");
        characterTypeText.text = PlayerSelectionData.GetSelectedCharacter().ToString();

        // Setup buttons
        playButton.onClick.AddListener(ShowPlayModePanel);
        logoutButton.onClick.AddListener(Logout);

        // Play mode buttons
        soloButton.onClick.AddListener(StartSoloGame);
        partyButton.onClick.AddListener(ShowPartyOptions);
        closePanelButton.onClick.AddListener(HideAllPanels);

        // Party buttons
        createRoomButton.onClick.AddListener(CreateRoom);
        joinRoomButton.onClick.AddListener(ShowJoinPanel);
        backToModeButton.onClick.AddListener(ShowPlayModePanel);

        // Join room buttons
        joinButton.onClick.AddListener(JoinRoom);
        backToPartyButton.onClick.AddListener(ShowPartyOptions);

        HideAllPanels();
    }

    void ShowPlayModePanel()
    {
        HideAllPanels();
        playModePanel.SetActive(true);
    }

    void ShowPartyOptions()
    {
        HideAllPanels();
        partyOptionsPanel.SetActive(true);
    }

    void ShowJoinPanel()
    {
        HideAllPanels();
        joinRoomPanel.SetActive(true);
    }

    void HideAllPanels()
    {
        playModePanel.SetActive(false);
        partyOptionsPanel.SetActive(false);
        joinRoomPanel.SetActive(false);
    }

    void StartSoloGame()
    {
        PlayerPrefs.SetString("GameMode", "Solo");
        PlayerPrefs.SetString("IsHost", "true");
        SceneManager.LoadScene("PlayRoom1");
    }

    void CreateRoom()
    {
        PlayerPrefs.SetString("GameMode", "Party");
        PlayerPrefs.SetString("IsHost", "true");
        SceneManager.LoadScene("WaitingRoom");
    }

    void JoinRoom()
    {
        string roomCode = roomCodeInput.text.Trim();

        if (string.IsNullOrEmpty(roomCode))
        {
            Debug.Log("Please enter room code!");
            return;
        }

        PlayerPrefs.SetString("GameMode", "Party");
        PlayerPrefs.SetString("IsHost", "false");
        PlayerPrefs.SetString("RoomCode", roomCode);
        SceneManager.LoadScene("WaitingRoom");
    }

    void Logout()
    {
        SceneManager.LoadScene("CharacterSelection");
    }
}