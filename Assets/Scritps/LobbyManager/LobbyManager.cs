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
    public GameObject characterSelectionPanel; // Optional: In-lobby character selection
    public Button bloodKnightSelectButton;
    public Button archerSelectButton;
    public Button assassinSelectButton;
    public Button ironJuggernautSelectButton;
    public TextMeshProUGUI availableCharactersText;


    // Player data
    private PlayerProgressData playerData;
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
            PlayerPrefs.DeleteKey("LastScene"); // Clear the flag
        }
    }

    void SetupEvents()
    {
        // Subscribe to stage selection events
        StageSelectionManager.OnSoloGameSelected += HandleSoloGameSelected;
        StageSelectionManager.OnPartyGameSelected += HandlePartyGameSelected;
        StageSelectionManager.OnBackToLobby += HandleBackToLobby;
    }

    void SetupButtons()
    {
        // Main lobby buttons
        playButton.onClick.AddListener(ShowStageSelection);
        logoutButton.onClick.AddListener(Logout);

        // Party buttons
        createRoomButton.onClick.AddListener(CreateRoom);
        joinRoomButton.onClick.AddListener(ShowJoinPanel);
        backToLobbyFromPartyButton.onClick.AddListener(BackToMainLobby);

        // Join room buttons
        joinButton.onClick.AddListener(JoinRoom);
        backToPartyButton.onClick.AddListener(ShowPartyOptions);

        if (characterSelectionButton != null)
            characterSelectionButton.onClick.AddListener(OpenCharacterSelection);

        // In-lobby character selection buttons (if using panel instead of scene)
       
    }
    private void OpenCharacterSelection()
    {
        // ✅ Set flag ว่ามาจาก Lobby และไป Scene เสมอ
        PlayerPrefs.SetString("LastScene", "Lobby");
        SceneManager.LoadScene("CharacterSelection");
    }

    private void ShowCharacterSelectionPanel()
    {
        HideAllPanels();
        characterSelectionPanel.SetActive(true);
        UpdateAvailableCharactersList();
    }

    private void SwitchCharacter(string characterType)
    {
        Debug.Log($"[LobbyManager] Switching to character: {characterType}");

        // Switch character in PersistentPlayerData
        PersistentPlayerData.Instance.SwitchCharacter(characterType);

        // Update UI
        RefreshPlayerStats();

        // Update PlayerSelectionData
        if (System.Enum.TryParse<PlayerSelectionData.CharacterType>(characterType, out var characterEnum))
        {
            PlayerSelectionData.SaveCharacterSelection(characterEnum);
        }

        // Hide character selection panel
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

        // Show all 4 character types with their levels
        string[] allCharacterTypes = { "BloodKnight", "Archer", "Assassin", "IronJuggernaut" };

        foreach (string characterType in allCharacterTypes)
        {
            CharacterProgressData characterData = allCharacters.Find(c => c.characterType == characterType);

            if (characterData != null)
            {
                // Character exists - show level and stats
                string color = (characterType == currentActive) ? "yellow" : "white";
                charactersList += $"<color={color}>• {characterType} - Level {characterData.currentLevel}</color>\n";
                charactersList += $"   HP: {characterData.totalMaxHp}, ATK: {characterData.totalAttackDamage}\n";
            }
            else
            {
                // Character not created yet - show as available
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

        // Add character selection panel
        
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

    // ========== Player Data Management (เหมือนเดิม) ==========
    private void ShowBasicPlayerInfo()
    {
        playerNameText.text = PlayerPrefs.GetString("PlayerName", "Unknown");

        // ✅ แสดง default Assassin character
        string savedCharacter = PlayerPrefs.GetString("LastCharacterSelected", "Assassin");
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
            // ✅ Force refresh to get latest character data
            RefreshPlayerStats();

            Debug.Log($"✅ [LobbyManager] Player stats loaded successfully");
        }
        else
        {
            Debug.LogWarning("[LobbyManager] Timeout loading player data. Using PlayerPrefs fallback.");
            LoadStatsFromPlayerPrefs();
        }
    }

    private void UpdatePlayerSelectionDataFromFirebase()
    {
        if (playerData == null || string.IsNullOrEmpty(playerData.lastCharacterSelected)) return;

        if (System.Enum.TryParse<PlayerSelectionData.CharacterType>(playerData.lastCharacterSelected, out var characterType))
        {
            PlayerSelectionData.SaveCharacterSelection(characterType);
            Debug.Log($"[LobbyManager] ✅ Updated PlayerSelectionData to: {characterType} (from Firebase: {playerData.lastCharacterSelected})");
        }
        else
        {
            Debug.LogWarning($"[LobbyManager] Cannot parse character: {playerData.lastCharacterSelected}");
        }
    }

    private void LoadStatsFromPlayerPrefs()
    {
        playerData = new PlayerProgressData();
        playerData.playerName = PlayerPrefs.GetString("PlayerName", "Player");
        playerData.currentLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
        playerData.currentExp = PlayerPrefs.GetInt("PlayerExp", 0);
        playerData.expToNextLevel = PlayerPrefs.GetInt("PlayerExpToNext", 100);
        playerData.totalMaxHp = PlayerPrefs.GetInt("PlayerMaxHp", 70);      // Assassin default
        playerData.totalMaxMana = PlayerPrefs.GetInt("PlayerMaxMana", 40);  // Assassin default
        playerData.totalAttackDamage = PlayerPrefs.GetInt("PlayerAttackDamage", 35); // Assassin default
        playerData.totalArmor = PlayerPrefs.GetInt("PlayerArmor", 2);       // Assassin default

        // ✅ Default character เป็น Assassin
        string savedCharacter = PlayerPrefs.GetString("LastCharacterSelected", "Assassin");
        playerData.lastCharacterSelected = savedCharacter;

        isPlayerDataLoaded = true;
        UpdatePlayerStatsUI();

    }


    private void UpdatePlayerStatsUI()
    {
        if (!isPlayerDataLoaded || playerData == null) return;

        try
        {
            // Get current active character name
            string activeCharacter = PersistentPlayerData.Instance.GetCurrentActiveCharacter();

            if (!string.IsNullOrEmpty(activeCharacter))
            {
                characterTypeText.text = activeCharacter;
            }

            if (playerLevelText != null)
                playerLevelText.text = $"Level {playerData.currentLevel}";

            if (playerExpText != null)
                playerExpText.text = $"EXP: {playerData.currentExp}/{playerData.expToNextLevel}";

            if (expProgressSlider != null)
            {
                float progress = playerData.expToNextLevel > 0 ?
                    (float)playerData.currentExp / playerData.expToNextLevel : 1f;
                expProgressSlider.value = progress;
            }

            if (hpText != null)
                hpText.text = $"HP: {playerData.totalMaxHp}";

            if (manaText != null)
                manaText.text = $"Mana: {playerData.totalMaxMana}";

            if (attackText != null)
                attackText.text = $"Attack: {playerData.totalAttackDamage}";

            if (armorText != null)
                armorText.text = $"Armor: {playerData.totalArmor}";

            Debug.Log($"[LobbyManager] UI updated - Level {playerData.currentLevel}, Character: {activeCharacter}, HP {playerData.totalMaxHp}");
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
            if (PersistentPlayerData.Instance.multiCharacterData != null)
            {
                // ✅ ใช้ Multi-Character System
                string activeCharacter = PersistentPlayerData.Instance.GetCurrentActiveCharacter();
                CharacterProgressData activeCharacterData = PersistentPlayerData.Instance.GetCharacterData(activeCharacter);

                if (activeCharacterData != null)
                {
                    // Convert to PlayerProgressData for compatibility
                    playerData = activeCharacterData.ToPlayerProgressData(PersistentPlayerData.Instance.multiCharacterData.playerName);
                    isPlayerDataLoaded = true;

                    // ✅ Update PlayerSelectionData as well
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
                // Fallback to old system
                playerData = PersistentPlayerData.Instance.GetPlayerData();
                if (playerData != null)
                {
                    isPlayerDataLoaded = true;
                    UpdatePlayerSelectionDataFromFirebase();
                    UpdatePlayerStatsUI();
                }
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
            // ✅ Refresh stats when returning to lobby
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
        if (playerData != null)
        {
            playerData.LogProgressInfo();
            Debug.Log($"PlayerSelectionData says: {PlayerSelectionData.GetSelectedCharacter()}");
        }
        else
        {
            Debug.Log("[LobbyManager] No player data available");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        StageSelectionManager.OnSoloGameSelected -= HandleSoloGameSelected;
        StageSelectionManager.OnPartyGameSelected -= HandlePartyGameSelected;
        StageSelectionManager.OnBackToLobby -= HandleBackToLobby;
    }
}