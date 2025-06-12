using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

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

    [Header("Buttons")]
    public Button playButton;
    public Button shopButton;
    public Button inventoryButton;
    public Button settingsButton;
    public Button logoutButton;

    // ========== เพิ่มใหม่: Stage Selection Panel ==========
    [Header("Stage Selection Panel")]
    public GameObject stageSelectionPanel;
    public Button leftArrowButton;
    public Button rightArrowButton;
    public Button confirmStageButton;
    public Button backToLobbyButton;

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

    // ========== เพิ่มใหม่: Stage Panels ==========
    [System.Serializable]
    public class StagePanel
    {
        public GameObject panel;
        public string stageName;
        public string sceneToLoad;
    }

    [Header("Stage Configuration")]
    public StagePanel[] stagePanels = new StagePanel[]
    {
        new StagePanel { stageName = "Stage 1: Forest", sceneToLoad = "PlayRoom1" },
        new StagePanel { stageName = "Stage 2: Desert", sceneToLoad = "PlayRoom2" },
        new StagePanel { stageName = "Stage 3: Ice Cave", sceneToLoad = "PlayRoom3" }
    };

    private int currentStageIndex = 0;
    private string selectedGameMode = ""; // เก็บโหมดที่เลือก

    // ข้อมูลเดิม
    private PlayerProgressData playerData;
    private bool isPlayerDataLoaded = false;

    void Start()
    {
        ShowBasicPlayerInfo();
        StartCoroutine(LoadAndShowPlayerStats());

        // ========== แก้ไข: Setup buttons ==========
        playButton.onClick.AddListener(ShowStageSelectionPanel); // เปลี่ยนจากเดิม
        logoutButton.onClick.AddListener(Logout);

        // Stage selection buttons (ใหม่)
        leftArrowButton.onClick.AddListener(PreviousStage);
        rightArrowButton.onClick.AddListener(NextStage);
        confirmStageButton.onClick.AddListener(ConfirmStageSelection);
        backToLobbyButton.onClick.AddListener(BackToLobby);

        // Play mode buttons
        soloButton.onClick.AddListener(() => StartGameWithMode("Solo"));
        partyButton.onClick.AddListener(() => StartGameWithMode("Party"));
        closePanelButton.onClick.AddListener(ShowStageSelectionPanel); // กลับไปหน้าเลือกด่าน

        // Party buttons
        createRoomButton.onClick.AddListener(CreateRoom);
        joinRoomButton.onClick.AddListener(ShowJoinPanel);
        backToModeButton.onClick.AddListener(ShowPlayModePanel);

        // Join room buttons
        joinButton.onClick.AddListener(JoinRoom);
        backToPartyButton.onClick.AddListener(ShowPartyOptions);

        HideAllPanels();
        UpdateStageDisplay();

        InvokeRepeating("UpdatePlayerStatsUI", 2f, 2f);
    }

    // ========== ใหม่: Stage Selection Methods ==========
    void ShowStageSelectionPanel()
    {
        HideAllPanels();
        stageSelectionPanel.SetActive(true);
        UpdateStageDisplay();
    }

    void PreviousStage()
    {
        currentStageIndex--;
        if (currentStageIndex < 0)
            currentStageIndex = stagePanels.Length - 1;
        UpdateStageDisplay();
    }

    void NextStage()
    {
        currentStageIndex++;
        if (currentStageIndex >= stagePanels.Length)
            currentStageIndex = 0;
        UpdateStageDisplay();
    }

    void UpdateStageDisplay()
    {
        // ซ่อน panel ทั้งหมดก่อน
        for (int i = 0; i < stagePanels.Length; i++)
        {
            if (stagePanels[i].panel != null)
            {
                stagePanels[i].panel.SetActive(false);
            }
        }

        // แสดง panel ปัจจุบัน
        if (stagePanels.Length > 0 && currentStageIndex >= 0 && currentStageIndex < stagePanels.Length)
        {
            if (stagePanels[currentStageIndex].panel != null)
            {
                stagePanels[currentStageIndex].panel.SetActive(true);
            }
        }
    }

    void ConfirmStageSelection()
    {
        // บันทึกด่านที่เลือก
        if (stagePanels.Length > 0 && currentStageIndex >= 0 && currentStageIndex < stagePanels.Length)
        {
            PlayerPrefs.SetString("SelectedStage", stagePanels[currentStageIndex].sceneToLoad);
            Debug.Log($"Selected stage: {stagePanels[currentStageIndex].stageName} -> {stagePanels[currentStageIndex].sceneToLoad}");
        }

        // ไปหน้าเลือกโหมดการเล่น
        ShowPlayModePanel();
    }

    void BackToLobby()
    {
        HideAllPanels();
    }

    // ========== แก้ไข: Game Mode Methods ==========
    void StartGameWithMode(string gameMode)
    {
        selectedGameMode = gameMode;

        if (gameMode == "Solo")
        {
            StartSoloGame();
        }
        else if (gameMode == "Party")
        {
            ShowPartyOptions();
        }
    }

    // ========== แก้ไข: Start Game Methods ==========
    void StartSoloGame()
    {
        PlayerPrefs.SetString("GameMode", "Solo");

        // ใช้ด่านที่เลือก
        string selectedStage = PlayerPrefs.GetString("SelectedStage", "PlayRoom1");
        SceneManager.LoadScene(selectedStage);
    }

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

    // ========== แก้ไข: Panel Management ==========
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
        stageSelectionPanel.SetActive(false);
        playModePanel.SetActive(false);
        partyOptionsPanel.SetActive(false);
        joinRoomPanel.SetActive(false);

        // ซ่อน stage panels ทั้งหมดด้วย
        HideAllStagePanels();
    }

    void HideAllStagePanels()
    {
        for (int i = 0; i < stagePanels.Length; i++)
        {
            if (stagePanels[i].panel != null)
            {
                stagePanels[i].panel.SetActive(false);
            }
        }
    }

    void Logout()
    {
        SceneManager.LoadScene("CharacterSelection");
    }

    // ========== Methods เดิมที่ไม่เปลี่ยน ==========
    private void ShowBasicPlayerInfo()
    {
        playerNameText.text = PlayerPrefs.GetString("PlayerName", "Unknown");
        characterTypeText.text = "Loading...";

        if (playerLevelText != null)
        {
            int savedLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
            playerLevelText.text = $"Level {savedLevel}";
        }

        Debug.Log("[LobbyManager] Basic player info displayed (without character)");
    }

    private IEnumerator LoadAndShowPlayerStats()
    {
        Debug.Log("[LobbyManager] Loading player stats...");

        float timeout = 5f;
        float elapsed = 0f;

        while (!PersistentPlayerData.Instance.HasValidData() && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return new WaitForSeconds(0.1f);
        }

        if (PersistentPlayerData.Instance.HasValidData())
        {
            playerData = PersistentPlayerData.Instance.GetPlayerData();
            isPlayerDataLoaded = true;

            Debug.Log($"✅ [LobbyManager] Player stats loaded: {playerData.playerName}, Level {playerData.currentLevel}, Character: {playerData.lastCharacterSelected}");

            UpdatePlayerSelectionDataFromFirebase();
            UpdatePlayerStatsUI();
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
        playerData.totalMaxHp = PlayerPrefs.GetInt("PlayerMaxHp", 100);
        playerData.totalMaxMana = PlayerPrefs.GetInt("PlayerMaxMana", 50);
        playerData.totalAttackDamage = PlayerPrefs.GetInt("PlayerAttackDamage", 20);
        playerData.totalArmor = PlayerPrefs.GetInt("PlayerArmor", 5);

        string savedCharacter = PlayerPrefs.GetString("LastCharacterSelected", "");
        if (!string.IsNullOrEmpty(savedCharacter))
        {
            playerData.lastCharacterSelected = savedCharacter;
        }
        else
        {
            playerData.lastCharacterSelected = PlayerSelectionData.GetSelectedCharacter().ToString();
        }

        isPlayerDataLoaded = true;
        UpdatePlayerStatsUI();

        Debug.Log($"[LobbyManager] Stats loaded from PlayerPrefs fallback, Character: {playerData.lastCharacterSelected}");
    }

    private void UpdatePlayerStatsUI()
    {
        if (!isPlayerDataLoaded || playerData == null) return;

        try
        {
            if (!string.IsNullOrEmpty(playerData.lastCharacterSelected))
            {
                characterTypeText.text = playerData.lastCharacterSelected;
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

            Debug.Log($"[LobbyManager] UI updated - Level {playerData.currentLevel}, Character: {playerData.lastCharacterSelected}, HP {playerData.totalMaxHp}");
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
            playerData = PersistentPlayerData.Instance.GetPlayerData();
            isPlayerDataLoaded = true;
            UpdatePlayerSelectionDataFromFirebase();
            UpdatePlayerStatsUI();
        }
    }

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

    [ContextMenu("Check Character Consistency")]
    public void Debug_CheckCharacterConsistency()
    {
        string firebaseChar = playerData?.lastCharacterSelected ?? "null";
        string playerPrefsChar = PlayerSelectionData.GetSelectedCharacter().ToString();
        string savedInPrefs = PlayerPrefs.GetString("LastCharacterSelected", "not set");

        Debug.Log($"=== Character Consistency Check ===");
        Debug.Log($"Firebase Character: {firebaseChar}");
        Debug.Log($"PlayerSelectionData: {playerPrefsChar}");
        Debug.Log($"PlayerPrefs LastCharacterSelected: {savedInPrefs}");
        Debug.Log($"Match: {firebaseChar == playerPrefsChar}");
    }
}