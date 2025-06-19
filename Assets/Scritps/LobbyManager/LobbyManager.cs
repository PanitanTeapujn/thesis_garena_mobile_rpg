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

    [Header("Friends System")]
    public Button friendsButton;
    public GameObject friendsPanel;
    public TMP_InputField searchFriendInput;
    public Button searchFriendButton;
    public Transform friendRequestsList;
    public Transform friendsList;
    public Button backFromFriendsButton;
    [Header("Friends Auto Refresh")]
    public Button refreshFriendsButton;
    public TextMeshProUGUI lastRefreshTimeText;
    public GameObject friendsLoadingIndicator;

    private Coroutine autoRefreshCoroutine;
    private bool isRefreshing = false;
    private System.DateTime lastRefreshTime;
    // Prefabs สำหรับ UI
    public GameObject friendRequestItemPrefab;  // ต้องสร้าง prefab สำหรับ friend request
    public GameObject friendItemPrefab;         // ต้องสร้าง prefab สำหรับ friend list


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
        StartAutoRefreshSystem();

        StartCoroutine(DelayedLoadFriendRequests());
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
        if (friendsButton != null)
            friendsButton.onClick.AddListener(ShowFriendsPanel);
        if (searchFriendButton != null)
            searchFriendButton.onClick.AddListener(SearchFriend);
        if (backFromFriendsButton != null)
            backFromFriendsButton.onClick.AddListener(BackToMainLobby);
        if (characterSelectionButton != null)
            characterSelectionButton.onClick.AddListener(OpenCharacterSelection);
        if (refreshFriendsButton != null)
            refreshFriendsButton.onClick.AddListener(ManualRefreshFriends);
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
        if (friendsPanel != null)
            friendsPanel.SetActive(false);
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

    #region Friends


    void ShowFriendsPanel()
    {
        Debug.Log("👥 ShowFriendsPanel() called");

        HideAllPanels();

        if (friendsPanel != null)
        {
            friendsPanel.SetActive(true);
            Debug.Log("✅ Friends panel activated");

            // ⭐ Auto refresh เมื่อเปิด panel
            StartCoroutine(RefreshWhenPanelOpened());
        }
        else
        {
            Debug.LogError("❌ friendsPanel is NULL!");
        }
    }
    private IEnumerator RefreshWhenPanelOpened()
    {
        yield return new WaitForEndOfFrame();

        // โหลดข้อมูลปัจจุบันก่อน
        RefreshFriendsList();
        RefreshFriendRequests();

        // ถ้าไม่เคย refresh หรือ refresh นานแล้ว (เกิน 1 นาที) ให้ refresh ใหม่
        bool shouldRefresh = lastRefreshTime == default(System.DateTime) ||
                            (System.DateTime.Now - lastRefreshTime).TotalMinutes > 1;

        if (shouldRefresh && !isRefreshing)
        {
            Debug.Log("🔄 Auto refreshing on panel open...");
            yield return StartCoroutine(RefreshFriendsDataCoroutine(true));
        }
    }

    void SearchFriend()
    {
        string friendName = searchFriendInput.text.Trim();
        if (string.IsNullOrEmpty(friendName))
        {
            Debug.Log("Please enter friend name!");
            return;
        }

        if (friendName == PersistentPlayerData.Instance.GetPlayerName())
        {
            Debug.Log("Cannot add yourself as friend!");
            return;
        }

        Debug.Log($"🔍 Searching for: '{friendName}'");
        PersistentPlayerData.Instance.SendFriendRequest(friendName);
        searchFriendInput.text = "";
    }


    void RefreshFriendsList()
    {
        if (friendsList == null) return;

        // Clear existing items
        foreach (Transform child in friendsList)
        {
            Destroy(child.gameObject);
        }

        // Load friends from PersistentPlayerData
        List<string> friends = PersistentPlayerData.Instance.GetFriendsList();

        foreach (string friendName in friends)
        {
            CreateFriendItem(friendName);
        }
    }

    void RefreshFriendRequests()
    {
        if (friendRequestsList == null) return;

        // Clear existing items
        foreach (Transform child in friendRequestsList)
        {
            Destroy(child.gameObject);
        }

        // Load friend requests from PersistentPlayerData
        List<string> requests = PersistentPlayerData.Instance.GetPendingFriendRequests();

        foreach (string requesterName in requests)
        {
            CreateFriendRequestItem(requesterName);
        }
    }

    void CreateFriendItem(string friendName)
    {
        if (friendItemPrefab == null || friendsList == null) return;

        GameObject friendItem = Instantiate(friendItemPrefab, friendsList);

        // ตั้งชื่อเพื่อน
        TextMeshProUGUI nameText = friendItem.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null)
            nameText.text = friendName;

        // ปุ่มลบเพื่อน (ถ้ามี)
        Button removeButton = friendItem.GetComponentInChildren<Button>();
        if (removeButton != null)
        {
            removeButton.onClick.AddListener(() => RemoveFriend(friendName));
        }
    }

    void CreateFriendRequestItem(string requesterName)
    {
        Debug.Log($"🔨 Creating friend request item for: {requesterName}");

        if (friendRequestItemPrefab == null || friendRequestsList == null)
        {
            Debug.LogError("❌ Missing prefab or list!");
            return;
        }

        GameObject requestItem = Instantiate(friendRequestItemPrefab, friendRequestsList);

        // ⭐ ตั้งขนาดที่เหมาสม
        RectTransform rectTransform = requestItem.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // ตั้งขนาดความสูงคงที่
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, 80f);

            // หรือใช้ Layout Element แทน
            LayoutElement layoutElement = requestItem.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = requestItem.AddComponent<LayoutElement>();
            }
            layoutElement.preferredHeight = 80f; // ความสูง 80 pixels
            layoutElement.flexibleWidth = 1f;    // ให้ขยายตามความกว้าง

            Debug.Log($"✅ Set item size: {rectTransform.sizeDelta}");
        }

        // ตั้งชื่อผู้ส่ง request
        TextMeshProUGUI nameText = requestItem.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = $"{requesterName} wants to be your friend";

            // ตั้งค่า Text ให้พอดี
            nameText.fontSize = 16f;
            nameText.autoSizeTextContainer = true;
        }

        // ปุ่ม Accept และ Reject
        Button[] buttons = requestItem.GetComponentsInChildren<Button>();
        if (buttons.Length >= 2)
        {
            // ตั้งขนาดปุ่ม
            foreach (Button button in buttons)
            {
                RectTransform buttonRect = button.GetComponent<RectTransform>();
                if (buttonRect != null)
                {
                    buttonRect.sizeDelta = new Vector2(80f, 30f); // กว้าง 80, สูง 30
                }
            }

            buttons[0].onClick.AddListener(() => AcceptFriendRequest(requesterName));
            buttons[1].onClick.AddListener(() => RejectFriendRequest(requesterName));
        }
    }

    void AcceptFriendRequest(string requesterName)
    {
        PersistentPlayerData.Instance.AcceptFriendRequest(requesterName);
        RefreshFriendRequests();
        RefreshFriendsList();
    }

    void RejectFriendRequest(string requesterName)
    {
        PersistentPlayerData.Instance.RejectFriendRequest(requesterName);
        RefreshFriendRequests();
    }

    void RemoveFriend(string friendName)
    {
        PersistentPlayerData.Instance.RemoveFriend(friendName);
        RefreshFriendsList();
    }

    private IEnumerator DelayedLoadFriendRequests()
    {
        // รอให้ PersistentPlayerData โหลดเสร็จก่อน
        yield return new WaitForSeconds(2f);

        int maxRetries = 5;
        int retryCount = 0;

        while (retryCount < maxRetries)
        {
            if (PersistentPlayerData.Instance.HasValidData())
            {
                Debug.Log("✅ Loading friend requests...");
                PersistentPlayerData.Instance.LoadFriendRequestsFromFirebase();
                break;
            }

            retryCount++;
            Debug.Log($"⏳ Waiting for player data to load... (Retry {retryCount}/{maxRetries})");
            yield return new WaitForSeconds(1f);
        }

        if (retryCount >= maxRetries)
        {
            Debug.LogWarning("⚠️ Player data not loaded after maximum retries");
        }
    }

    void StartAutoRefreshSystem()
    {
        Debug.Log("🔄 Starting auto refresh system for friends...");

        // รอให้ระบบโหลดเสร็จก่อน
        StartCoroutine(DelayedStartAutoRefresh());
    }

    private IEnumerator DelayedStartAutoRefresh()
    {
        // รอให้ player data โหลดเสร็จ
        yield return new WaitForSeconds(5f);

        // เริ่ม auto refresh
        if (autoRefreshCoroutine != null)
        {
            StopCoroutine(autoRefreshCoroutine);
        }

        autoRefreshCoroutine = StartCoroutine(AutoRefreshFriendsCoroutine());
        Debug.Log("✅ Auto refresh system started - refreshing every 30 seconds");
    }

    private IEnumerator AutoRefreshFriendsCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(30f); // รอ 30 วินาที

            // Auto refresh เฉพาะเมื่อ friends panel เปิดอยู่
            if (friendsPanel != null && friendsPanel.activeSelf && !isRefreshing)
            {
                Debug.Log("🔄 Auto refreshing friends data...");
                yield return StartCoroutine(RefreshFriendsDataCoroutine(false)); // false = ไม่แสดง loading
            }
        }
    }

    // Manual refresh เมื่อกดปุ่ม
    public void ManualRefreshFriends()
    {
        if (isRefreshing)
        {
            Debug.Log("⏳ Already refreshing, please wait...");
            return;
        }

        Debug.Log("🔄 Manual refresh triggered");
        StartCoroutine(RefreshFriendsDataCoroutine(true)); // true = แสดง loading
    }

    // Main refresh coroutine
    private IEnumerator RefreshFriendsDataCoroutine(bool showLoading)
    {
        if (isRefreshing) yield break;

        isRefreshing = true;

        if (showLoading && friendsLoadingIndicator != null)
        {
            friendsLoadingIndicator.SetActive(true);
        }

        Debug.Log("📡 Refreshing friends data from Firebase...");

        // โหลดข้อมูล friend requests ใหม่
        yield return StartCoroutine(PersistentPlayerData.Instance.RefreshFriendRequestsCoroutine());

        // โหลดข้อมูล friends list ใหม่ (ถ้าต้องการ)
        yield return StartCoroutine(PersistentPlayerData.Instance.RefreshFriendsListCoroutine());

        // อัพเดต UI
        if (friendsPanel != null && friendsPanel.activeSelf)
        {
            RefreshFriendRequests();
            RefreshFriendsList();
            Debug.Log("✅ Friends UI refreshed");
        }

        // อัพเดตเวลาที่ refresh ล่าสุด
        lastRefreshTime = System.DateTime.Now;
        UpdateLastRefreshTimeDisplay();

        if (showLoading && friendsLoadingIndicator != null)
        {
            friendsLoadingIndicator.SetActive(false);
        }

        isRefreshing = false;
        Debug.Log($"✅ Friends data refresh completed at {lastRefreshTime:HH:mm:ss}");
    }

    // แสดงเวลาที่ refresh ล่าสุด
    private void UpdateLastRefreshTimeDisplay()
    {
        if (lastRefreshTimeText != null)
        {
            lastRefreshTimeText.text = $"Last updated: {lastRefreshTime:HH:mm:ss}";
        }
    }

    #endregion
    // ========== Debug Methods ==========


    void OnDestroy()
    {
        StageSelectionManager.OnSoloGameSelected -= HandleSoloGameSelected;
        StageSelectionManager.OnPartyGameSelected -= HandlePartyGameSelected;
        StageSelectionManager.OnBackToLobby -= HandleBackToLobby;
    }
    [ContextMenu("Show My User ID")]
    public void ShowMyUserId()
    {
        if (PersistentPlayerData.Instance.auth?.CurrentUser != null)
        {
            string userId = PersistentPlayerData.Instance.auth.CurrentUser.UserId;
            Debug.Log($"📋 Your User ID: {userId}");
            Debug.Log($"📋 Your Player Name: {PersistentPlayerData.Instance.GetPlayerName()}");
            Debug.Log($"💡 Share your User ID with friends to add each other!");
        }
        else
        {
            Debug.Log("❌ Not authenticated");
        }
    }

    [ContextMenu("Test Friend System")]
    public void TestFriendSystem()
    {
        Debug.Log("=== Testing Friend System ===");

        // 1. ตรวจสอบสถานะ
        PersistentPlayerData.Instance.CheckFirebaseStatus();

        // 2. ดูข้อมูลทั้งหมด
        PersistentPlayerData.Instance.DebugAllPlayers();
    }

    [ContextMenu("Fix Existing Items Size")]
    public void FixExistingItemsSize()
    {
        if (friendRequestsList == null) return;

        Debug.Log("🔧 Fixing existing items size...");

        for (int i = 0; i < friendRequestsList.childCount; i++)
        {
            Transform child = friendRequestsList.GetChild(i);

            // แก้ไขขนาด
            RectTransform rectTransform = child.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, 80f);
            }

            // เพิ่ม Layout Element ถ้าไม่มี
            LayoutElement layoutElement = child.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = child.gameObject.AddComponent<LayoutElement>();
            }
            layoutElement.preferredHeight = 80f;
            layoutElement.flexibleWidth = 1f;
        }

        // Force rebuild layout
        LayoutRebuilder.ForceRebuildLayoutImmediate(friendRequestsList.GetComponent<RectTransform>());

        Debug.Log($"✅ Fixed {friendRequestsList.childCount} items");
    }
}