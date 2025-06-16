using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI characterTypeText;

    [Header("Level & Stats UI")]
    public TextMeshProUGUI playerLevelText;
    public TextMeshProUGUI playerExpText;
    public Slider expProgressSlider;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI manaText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI armorText;

    [Header("Main Lobby Buttons")]
    public Button playButton;
    public Button shopButton;
    public Button inventoryButton;
    public Button settingsButton;
    public Button logoutButton;

    [Header("Party Management")]
    public GameObject partyOptionsPanel;
    public Button createRoomButton;
    public Button joinRoomButton;
    public Button backToLobbyFromPartyButton;

    [Header("Join Room Panel")]
    public GameObject joinRoomPanel;
    public TMP_InputField roomCodeInput;
    public Button joinButton;
    public Button backToPartyButton;

    [Header("References")]
    public StageSelectionManager stageSelectionManager;

    [Header("Character Selection")]
    public Button characterSelectionButton;
    public GameObject characterSelectionPanel;
    public Button bloodKnightSelectButton;
    public Button archerSelectButton;
    public Button assassinSelectButton;
    public Button ironJuggernautSelectButton;
    public TextMeshProUGUI availableCharactersText;

    // Player data
    private CharacterProgressData currentCharacterData;
    private bool isPlayerDataLoaded = false;

    void Start()
    {
        SetupEvents();
        SetupButtons();

        ShowBasicPlayerInfo();
        StartCoroutine(LoadAndShowPlayerStats());

        HideAllPanels();
        InvokeRepeating("UpdatePlayerStatsUI", 2f, 2f);

        if (PlayerPrefs.GetString("LastScene", "") == "CharacterSelection")
        {
            StartCoroutine(DelayedRefresh());
            PlayerPrefs.DeleteKey("LastScene");
        }
    }

    void SetupEvents()
    {
        StageSelectionManager.OnSoloGameSelected += HandleSoloGameSelected;
        StageSelectionManager.OnPartyGameSelected += HandlePartyGameSelected;
        StageSelectionManager.OnBackToLobby += HandleBackToLobby;
    }

    void SetupButtons()
    {
        playButton.onClick.AddListener(ShowStageSelection);
        logoutButton.onClick.AddListener(Logout);

        createRoomButton.onClick.AddListener(CreateRoom);
        joinRoomButton.onClick.AddListener(ShowJoinPanel);
        backToLobbyFromPartyButton.onClick.AddListener(BackToMainLobby);

        joinButton.onClick.AddListener(JoinRoom);
        backToPartyButton.onClick.AddListener(ShowPartyOptions);

        if (characterSelectionButton != null)
            characterSelectionButton.onClick.AddListener(OpenCharacterSelection);

        // In-lobby character selection buttons
        if (bloodKnightSelectButton != null)
            bloodKnightSelectButton.onClick.AddListener(() => SwitchCharacter("BloodKnight"));
        if (archerSelectButton != null)
            archerSelectButton.onClick.AddListener(() => SwitchCharacter("Archer"));
        if (assassinSelectButton != null)
            assassinSelectButton.onClick.AddListener(() => SwitchCharacter("Assassin"));
        if (ironJuggernautSelectButton != null)
            ironJuggernautSelectButton.onClick.AddListener(() => SwitchCharacter("IronJuggernaut"));
    }

    private void OpenCharacterSelection()
    {
        PlayerPrefs.SetString("LastScene", "Lobby");
        SceneManager.LoadScene("CharacterSelection");
    }

    private void ShowCharacterSelectionPanel()
    {
        HideAllPanels();
        if (characterSelectionPanel != null)
        {
            characterSelectionPanel.SetActive(true);
            UpdateAvailableCharactersList();
        }
    }

    private void SwitchCharacter(string characterType)
    {
        Debug.Log($"[LobbyManager] Switching to character: {characterType}");

        PersistentPlayerData.Instance.SwitchCharacter(characterType);
        RefreshPlayerStats();

        if (System.Enum.TryParse<PlayerSelectionData.CharacterType>(characterType, out var characterEnum))
        {
            PlayerSelectionData.SaveCharacterSelection(characterEnum);
        }

        if (characterSelectionPanel != null)
            characterSelectionPanel.SetActive(false);

        Debug.Log($"✅ [LobbyManager] Successfully switched to {characterType}");
    }

    private void UpdateAvailableCharactersList()
    {
        if (availableCharactersText == null) return;

        List<CharacterProgressData> allCharacters = PersistentPlayerData.Instance.GetAllCharacterData();
        string currentActive = PersistentPlayerData.Instance.GetCurrentActiveCharacter();

        string charactersList = $"<color=yellow>Current Active: {currentActive}</color>\n\n";
        charactersList += "<color=white>Available Characters:</color>\n";

        string[] allCharacterTypes = { "BloodKnight", "Archer", "Assassin", "IronJuggernaut" };

        foreach (string characterType in allCharacterTypes)
        {
            CharacterProgressData characterData = allCharacters.Find(c => c.characterType == characterType);

            if (characterData != null)
            {
                string color = (characterType == currentActive) ? "yellow" : "white";
                charactersList += $"<color={color}>• {characterType} - Level {characterData.currentLevel}</color>\n";
                charactersList += $"   HP: {characterData.totalMaxHp}, ATK: {characterData.totalAttackDamage}\n";
            }
            else
            {
                charactersList += $"<color=gray>• {characterType} - New Character</color>\n";
            }
        }

        availableCharactersText.text = charactersList;
    }

    // ========== Stage Selection Events ==========
    void HandleSoloGameSelected(string sceneToLoad)
    {
        PlayerPrefs.SetString("GameMode", "Solo");
        SceneManager.LoadScene(sceneToLoad);
    }

    void HandlePartyGameSelected()
    {
        ShowPartyOptions();
    }

    void HandleBackToLobby()
    {
        BackToMainLobby();
    }

    // ========== UI Navigation ==========
    void ShowStageSelection()
    {
        HideAllPanels();
        if (stageSelectionManager != null)
        {
            stageSelectionManager.ShowMainStageSelection();
        }
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

    void BackToMainLobby()
    {
        HideAllPanels();
    }

    void HideAllPanels()
    {
        partyOptionsPanel.SetActive(false);
        joinRoomPanel.SetActive(false);

        if (characterSelectionPanel != null)
            characterSelectionPanel.SetActive(false);
    }

    // ========== Party Management ==========
    void CreateRoom()
    {
        PlayerPrefs.SetString("GameMode", "Party");
        PlayerPrefs.SetString("IsHost", "true");
        PlayerPrefs.SetString("GameMode", "Coop");
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
        PlayerPrefs.SetString("GameMode", "Coop");
        SceneManager.LoadScene("WaitingRoom");
    }

    void Logout()
    {
        SceneManager.LoadScene("CharacterSelection");
    }

    // ========== Player Data Management ==========
    private void ShowBasicPlayerInfo()
    {
        string playerName = PlayerPrefs.GetString("PlayerName", "Unknown");
        if (playerNameText != null)
            playerNameText.text = playerName;

        string savedCharacter = PlayerPrefs.GetString("LastCharacterSelected", "Assassin");
        if (characterTypeText != null)
            characterTypeText.text = savedCharacter;

        if (playerLevelText != null)
        {
            int savedLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
            playerLevelText.text = $"Level {savedLevel}";
        }

        Debug.Log($"[LobbyManager] Basic player info displayed: {savedCharacter}");
    }

    private IEnumerator LoadAndShowPlayerStats()
    {
        Debug.Log("[LobbyManager] Loading multi-character player stats...");

        float timeout = 5f;
        float elapsed = 0f;

        while (!PersistentPlayerData.Instance.HasValidData() && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return new WaitForSeconds(0.1f);
        }

        if (PersistentPlayerData.Instance.HasValidData())
        {
            RefreshPlayerStats();
            Debug.Log($"✅ [LobbyManager] Player stats loaded successfully");
        }
        else
        {
            Debug.LogWarning("[LobbyManager] Timeout loading player data. Using PlayerPrefs fallback.");
            LoadStatsFromPlayerPrefs();
        }
    }

    private void LoadStatsFromPlayerPrefs()
    {
        currentCharacterData = new CharacterProgressData();
        currentCharacterData.characterType = PlayerPrefs.GetString("LastCharacterSelected", "Assassin");
        currentCharacterData.currentLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
        currentCharacterData.currentExp = PlayerPrefs.GetInt("PlayerExp", 0);
        currentCharacterData.expToNextLevel = PlayerPrefs.GetInt("PlayerExpToNext", 100);
        currentCharacterData.totalMaxHp = PlayerPrefs.GetInt("PlayerMaxHp", 70);
        currentCharacterData.totalMaxMana = PlayerPrefs.GetInt("PlayerMaxMana", 40);
        currentCharacterData.totalAttackDamage = PlayerPrefs.GetInt("PlayerAttackDamage", 35);
        currentCharacterData.totalArmor = PlayerPrefs.GetInt("PlayerArmor", 2);

        isPlayerDataLoaded = true;
        UpdatePlayerStatsUI();
    }

    private void UpdatePlayerStatsUI()
    {
        if (!isPlayerDataLoaded || currentCharacterData == null) return;

        try
        {
            string activeCharacter = PersistentPlayerData.Instance.GetCurrentActiveCharacter();
            if (!string.IsNullOrEmpty(activeCharacter) && characterTypeText != null)
            {
                characterTypeText.text = activeCharacter;
            }

            if (playerLevelText != null)
                playerLevelText.text = $"Level {currentCharacterData.currentLevel}";

            if (playerExpText != null)
                playerExpText.text = $"EXP: {currentCharacterData.currentExp}/{currentCharacterData.expToNextLevel}";

            if (expProgressSlider != null)
            {
                float progress = currentCharacterData.expToNextLevel > 0 ?
                    (float)currentCharacterData.currentExp / currentCharacterData.expToNextLevel : 1f;
                expProgressSlider.value = progress;
            }

            if (hpText != null)
                hpText.text = $"HP: {currentCharacterData.totalMaxHp}";

            if (manaText != null)
                manaText.text = $"Mana: {currentCharacterData.totalMaxMana}";

            if (attackText != null)
                attackText.text = $"Attack: {currentCharacterData.totalAttackDamage}";

            if (armorText != null)
                armorText.text = $"Armor: {currentCharacterData.totalArmor}";

            Debug.Log($"[LobbyManager] UI updated - Level {currentCharacterData.currentLevel}, Character: {activeCharacter}, HP {currentCharacterData.totalMaxHp}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[LobbyManager] Error updating UI: {e.Message}");
        }
    }

    public void RefreshPlayerStats()
    {
        if (PersistentPlayerData.Instance.HasValidData())
        {
            string activeCharacter = PersistentPlayerData.Instance.GetCurrentActiveCharacter();
            CharacterProgressData activeCharacterData = PersistentPlayerData.Instance.GetCharacterData(activeCharacter);

            if (activeCharacterData != null)
            {
                currentCharacterData = activeCharacterData;
                isPlayerDataLoaded = true;

                if (System.Enum.TryParse<PlayerSelectionData.CharacterType>(activeCharacter, out var characterEnum))
                {
                    PlayerSelectionData.SaveCharacterSelection(characterEnum);
                }

                UpdatePlayerStatsUI();

                Debug.Log($"✅ [LobbyManager] Refreshed stats for {activeCharacter} - Level {activeCharacterData.currentLevel}");
            }
            else
            {
                Debug.LogWarning($"[LobbyManager] No character data found for {activeCharacter}");
            }
        }
        else
        {
            Debug.LogWarning("[LobbyManager] No valid data available for refresh");
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            StartCoroutine(DelayedRefresh());
        }
    }

    private IEnumerator DelayedRefresh()
    {
        yield return new WaitForSeconds(0.5f);
        RefreshPlayerStats();
        Debug.Log("[LobbyManager] Refreshed stats after returning to lobby");
    }

    // ========== Debug Methods ==========
    [ContextMenu("Refresh Player Stats")]
    public void Debug_RefreshStats()
    {
        RefreshPlayerStats();
    }

    [ContextMenu("Log Player Data")]
    public void Debug_LogPlayerData()
    {
        if (currentCharacterData != null)
        {
            Debug.Log($"Character: {currentCharacterData.characterType}, Level: {currentCharacterData.currentLevel}, HP: {currentCharacterData.totalMaxHp}");
            Debug.Log($"PlayerSelectionData says: {PlayerSelectionData.GetSelectedCharacter()}");
        }
        else
        {
            Debug.Log("[LobbyManager] No player data available");
        }
    }

    void OnDestroy()
    {
        StageSelectionManager.OnSoloGameSelected -= HandleSoloGameSelected;
        StageSelectionManager.OnPartyGameSelected -= HandlePartyGameSelected;
        StageSelectionManager.OnBackToLobby -= HandleBackToLobby;
    }
}