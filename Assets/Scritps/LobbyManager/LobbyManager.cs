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

    [Header("Level & Stats UI (เพิ่มใหม่)")]
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

    // ========== เพิ่มใหม่ ==========
    private PlayerProgressData playerData;
    private bool isPlayerDataLoaded = false;

    void Start()
    {
        // แสดงข้อมูลผู้เล่นพื้นฐาน (เร็ว)
        ShowBasicPlayerInfo();

        // โหลดข้อมูล level และ stats (ช้าหน่อย)
        StartCoroutine(LoadAndShowPlayerStats());

        // Setup buttons (เหมือนเดิม)
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

        // อัพเดท UI ทุก 2 วินาที
        InvokeRepeating("UpdatePlayerStatsUI", 2f, 2f);
    }

    // ========== แก้ไข: แสดงข้อมูลพื้นฐาน ==========
    private void ShowBasicPlayerInfo()
    {
        // แสดงชื่อจาก PlayerPrefs ก่อน (เร็ว)
        playerNameText.text = PlayerPrefs.GetString("PlayerName", "Unknown");

        // ✅ Fix: ไม่แสดงตัวละครจาก PlayerSelectionData ทันที
        // รอให้ข้อมูลจาก Firebase มาก่อน
        characterTypeText.text = "Loading...";

        // แสดง level จาก PlayerPrefs ถ้ามี
        if (playerLevelText != null)
        {
            int savedLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
            playerLevelText.text = $"Level {savedLevel}";
        }

        Debug.Log("[LobbyManager] Basic player info displayed (without character)");
    }

    // ========== แก้ไข: โหลดข้อมูล Stats ==========
    private IEnumerator LoadAndShowPlayerStats()
    {
        Debug.Log("[LobbyManager] Loading player stats...");

        // รอให้ PersistentPlayerData โหลดข้อมูล
        float timeout = 5f; // รอสูงสุด 5 วินาที
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

            // ✅ Fix: อัพเดท PlayerSelectionData ให้ตรงกับข้อมูลจาก Firebase
            UpdatePlayerSelectionDataFromFirebase();

            // อัพเดท UI ทันที
            UpdatePlayerStatsUI();
        }
        else
        {
            Debug.LogWarning("[LobbyManager] Timeout loading player data. Using PlayerPrefs fallback.");
            LoadStatsFromPlayerPrefs();
        }
    }

    // ========== เพิ่มใหม่: อัพเดท PlayerSelectionData จาก Firebase ==========
    private void UpdatePlayerSelectionDataFromFirebase()
    {
        if (playerData == null || string.IsNullOrEmpty(playerData.lastCharacterSelected)) return;

        // แปลง string เป็น CharacterType
        if (System.Enum.TryParse<PlayerSelectionData.CharacterType>(playerData.lastCharacterSelected, out var characterType))
        {
            // ✅ อัพเดท PlayerSelectionData ให้ตรงกับข้อมูลจาก Firebase
            PlayerSelectionData.SaveCharacterSelection(characterType);

            Debug.Log($"[LobbyManager] ✅ Updated PlayerSelectionData to: {characterType} (from Firebase: {playerData.lastCharacterSelected})");
        }
        else
        {
            Debug.LogWarning($"[LobbyManager] Cannot parse character: {playerData.lastCharacterSelected}");
        }
    }

    // ========== แก้ไข: Fallback จาก PlayerPrefs ==========
    private void LoadStatsFromPlayerPrefs()
    {
        // สร้าง PlayerProgressData จาก PlayerPrefs
        playerData = new PlayerProgressData();
        playerData.playerName = PlayerPrefs.GetString("PlayerName", "Player");
        playerData.currentLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
        playerData.currentExp = PlayerPrefs.GetInt("PlayerExp", 0);
        playerData.expToNextLevel = PlayerPrefs.GetInt("PlayerExpToNext", 100);
        playerData.totalMaxHp = PlayerPrefs.GetInt("PlayerMaxHp", 100);
        playerData.totalMaxMana = PlayerPrefs.GetInt("PlayerMaxMana", 50);
        playerData.totalAttackDamage = PlayerPrefs.GetInt("PlayerAttackDamage", 20);
        playerData.totalArmor = PlayerPrefs.GetInt("PlayerArmor", 5);

        // ✅ Fix: ใช้ข้อมูลตัวละครจาก PlayerPrefs เฉพาะกรณี fallback เท่านั้น
        string savedCharacter = PlayerPrefs.GetString("LastCharacterSelected", "");
        if (!string.IsNullOrEmpty(savedCharacter))
        {
            playerData.lastCharacterSelected = savedCharacter;
        }
        else
        {
            // ถ้าไม่มีข้อมูลใน PlayerPrefs ใช้ default
            playerData.lastCharacterSelected = PlayerSelectionData.GetSelectedCharacter().ToString();
        }

        isPlayerDataLoaded = true;
        UpdatePlayerStatsUI();

        Debug.Log($"[LobbyManager] Stats loaded from PlayerPrefs fallback, Character: {playerData.lastCharacterSelected}");
    }

    // ========== แก้ไข: อัพเดท UI ==========
    private void UpdatePlayerStatsUI()
    {
        if (!isPlayerDataLoaded || playerData == null) return;

        try
        {
            // ✅ Fix: อัพเดทชื่อตัวละครจากข้อมูลที่โหลดมา
            if (!string.IsNullOrEmpty(playerData.lastCharacterSelected))
            {
                characterTypeText.text = playerData.lastCharacterSelected;
            }

            // อัพเดท level และ exp
            if (playerLevelText != null)
                playerLevelText.text = $"Level {playerData.currentLevel}";

            if (playerExpText != null)
                playerExpText.text = $"EXP: {playerData.currentExp}/{playerData.expToNextLevel}";

            // อัพเดท exp progress bar
            if (expProgressSlider != null)
            {
                float progress = playerData.expToNextLevel > 0 ?
                    (float)playerData.currentExp / playerData.expToNextLevel : 1f;
                expProgressSlider.value = progress;
            }

            // อัพเดท stats
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

    // ========== เพิ่มใหม่: Force Refresh ==========
    public void RefreshPlayerStats()
    {
        // บังคับให้โหลดข้อมูลใหม่
        if (PersistentPlayerData.Instance.HasValidData())
        {
            playerData = PersistentPlayerData.Instance.GetPlayerData();
            isPlayerDataLoaded = true;

            // ✅ Fix: อัพเดท PlayerSelectionData ด้วย
            UpdatePlayerSelectionDataFromFirebase();

            UpdatePlayerStatsUI();
        }
    }

    // ========== เพิ่มใหม่: Context Menu สำหรับ Debug ==========
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

    // ========== Methods เดิม (ไม่เปลี่ยน) ==========
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
        SceneManager.LoadScene("PlayRoom1");
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

    void Logout()
    {
        SceneManager.LoadScene("CharacterSelection");
    }
}