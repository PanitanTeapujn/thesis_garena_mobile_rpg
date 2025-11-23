using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Firebase.Auth;
using Firebase.Database;
using System.Collections;
using System.Collections.Generic;

public class CharacterSelectionManager : MonoBehaviour
{
    [Header("Character Prefabs")]
    public GameObject bloodKnightPrefab;
    public GameObject archerPrefab;
    public GameObject assassinPrefab;
    public GameObject ironJuggernautPrefab;

    [Header("Character Preview")]
    public Transform characterPreviewParent;
    public Vector3 previewPosition;
    public Vector3 previewRotation;

    [Header("UI Elements")]
    public Button bloodKnightButton;
    public Button archerButton;
    public Button assassinButton;
    public Button ironJuggernautButton;
    public Button confirmButton;

    [Header("Player Name Input")]
    public TMP_InputField playerNameInput;
    public TextMeshProUGUI errorMessageText;

    [Header("Character Info")]
    public TextMeshProUGUI characterDescriptionText;  // ใช้สำหรับ Story
    public TextMeshProUGUI characterNameText;

    [Header("Loading")]
    public GameObject loadingPanel;

    [Header("Navigation")]
    public Button backToLobbyButton;
    public TextMeshProUGUI characterLevelsText;

    // ========== 🆕 NEW: Skill System ==========
    [Header("🎯 Skill Icon Buttons")]
    public Button passiveSkillButton;
    public Button skill1Button;
    public Button skill2Button;
    public Button skill3Button;
    public Button skill4Button;
    public Button showCharacterInfoButton; // ปุ่มสำหรับกลับมาแสดงคำอธิบายตัวละคร (optional)

    [Header("🎨 Skill Icon Images (ลากจาก Button > Image)")]
    public Image passiveSkillIcon;
    public Image skill1Icon;
    public Image skill2Icon;
    public Image skill3Icon;
    public Image skill4Icon;

    [Header("📖 Skill Descriptions - Blood Knight")]
    public string bloodKnightPassive = "Blood Siphon: Heals for a percentage of damage dealt.";
    public string bloodKnightSkill1 = "Blood Strike: Powerful melee attack that deals bonus damage.";
    public string bloodKnightSkill2 = "Life Drain: Drains life from enemies in an area.";
    public string bloodKnightSkill3 = "Crimson Shield: Creates a blood shield that absorbs damage.";
    public string bloodKnightSkill4 = "Blood Rage: Increases attack speed and damage temporarily.";

    [Header("📖 Skill Descriptions - Archer")]
    public string archerPassive = "Eagle Eye: Increased critical chance and range.";
    public string archerSkill1 = "Multi Shot: Fires multiple arrows at once.";
    public string archerSkill2 = "Poison Arrow: Shoots a poisoned arrow that deals damage over time.";
    public string archerSkill3 = "Rain of Arrows: Rains arrows in a large area.";
    public string archerSkill4 = "Snipe: Powerful long-range shot with high damage.";

    [Header("📖 Skill Descriptions - Assassin")]
    public string assassinPassive = "Shadow Step: Increased movement speed and evasion.";
    public string assassinSkill1 = "Backstab: Critical damage from behind.";
    public string assassinSkill2 = "Smoke Bomb: Creates smoke that blinds enemies.";
    public string assassinSkill3 = "Poison Blade: Applies deadly poison to weapons.";
    public string assassinSkill4 = "Shadow Clone: Creates clones to confuse enemies.";

    [Header("📖 Skill Descriptions - Iron Juggernaut")]
    public string ironJuggernautPassive = "Iron Skin: Increased armor and damage reduction.";
    public string ironJuggernautSkill1 = "Shield Bash: Stuns enemies with shield.";
    public string ironJuggernautSkill2 = "Ground Slam: Creates shockwave that damages nearby enemies.";
    public string ironJuggernautSkill3 = "Fortify: Temporarily increases defense greatly.";
    public string ironJuggernautSkill4 = "Charge: Rushes forward, knocking back enemies.";

    [Header("📜 Info Display Panel")]
    public GameObject infoPanel;               // Panel รวม (มี Stats + Story/Skill Description)
    public GameObject statsSection;            // Section แสดง Stats (แสดงตลอด)
    public GameObject storySection;            // Section แสดงเรื่องราว (toggle)
    public GameObject skillDescriptionSection; // Section แสดงรายละเอียด skill (toggle)
    public TextMeshProUGUI skillDescriptionText; // Text ใน Skill Description Section

    // ========== 🆕 NEW: Detailed Stats Display (17 Fields) ==========
    [Header("📊 Character Stats Display")]
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI manaText;
    public TextMeshProUGUI attackDamageText;
    public TextMeshProUGUI magicDamageText;
    public TextMeshProUGUI armorText;
    public TextMeshProUGUI magicArmorText;
    public TextMeshProUGUI moveSpeedText;
    public TextMeshProUGUI criticalChanceText;
    public TextMeshProUGUI criticalDamageText;
    public TextMeshProUGUI hitRateText;
    public TextMeshProUGUI evasionRateText;
    public TextMeshProUGUI attackSpeedText;
    public TextMeshProUGUI reductionCoolDownText;
    public TextMeshProUGUI lifeStealText;
    public TextMeshProUGUI ampDamageText;
    public TextMeshProUGUI healthRegenText;
    public TextMeshProUGUI manaRegenText;

    // ========== Private Variables ==========
    private GameObject currentPreview;
    private PlayerSelectionData.CharacterType selectedCharacter;
    private FirebaseAuth auth;
    private DatabaseReference databaseReference;
    private bool comingFromLobby = false;
    private int currentSelectedSkillIndex = -1; // -1 = ไม่มีการเลือก, 0 = passive, 1-4 = skills
    private string currentCharacterBaseDescription = ""; // เก็บคำอธิบายเดิมของตัวละคร

    private void Start()
    {
        // Setup character selection buttons
        bloodKnightButton.onClick.AddListener(() => SelectCharacter(PlayerSelectionData.CharacterType.BloodKnight));
        archerButton.onClick.AddListener(() => SelectCharacter(PlayerSelectionData.CharacterType.Archer));
        assassinButton.onClick.AddListener(() => SelectCharacter(PlayerSelectionData.CharacterType.Assassin));
        ironJuggernautButton.onClick.AddListener(() => SelectCharacter(PlayerSelectionData.CharacterType.IronJuggernaut));

        confirmButton.onClick.AddListener(() => StartCoroutine(ConfirmSelectionCoroutine()));

        // ✅ Setup skill buttons
        SetupSkillButtons();

        // ✅ Setup back to lobby button
        if (backToLobbyButton != null)
            backToLobbyButton.onClick.AddListener(BackToLobby);

        // ✅ Check if coming from Lobby
        comingFromLobby = (PlayerPrefs.GetString("LastScene", "") == "Lobby");

        // ✅ Show back button if from lobby
        if (backToLobbyButton != null)
            backToLobbyButton.gameObject.SetActive(comingFromLobby);

        auth = FirebaseAuth.DefaultInstance;
        databaseReference = FirebaseDatabase.DefaultInstance.RootReference;

        // Load saved player name
        string savedPlayerName = PlayerPrefs.GetString("PlayerName", "");
        if (!string.IsNullOrEmpty(savedPlayerName) && playerNameInput != null)
        {
            playerNameInput.text = savedPlayerName;
        }

        // ✅ Initialize character selection
        StartCoroutine(InitializeCharacterSelection());

        // ✅ Hide panels initially
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        if (storySection != null)
            storySection.SetActive(false);

        if (skillDescriptionSection != null)
            skillDescriptionSection.SetActive(true);
    }

    // ========== 🆕 NEW: Setup Skill Buttons ==========
    private void SetupSkillButtons()
    {
        Debug.Log("=== Setting up Skill Buttons ===");

        if (passiveSkillButton != null)
        {
            passiveSkillButton.onClick.RemoveAllListeners();
            passiveSkillButton.onClick.AddListener(() => ShowSkillDescription(0));
            Debug.Log("✅ Passive button setup complete");
        }

        if (skill1Button != null)
        {
            skill1Button.onClick.RemoveAllListeners();
            skill1Button.onClick.AddListener(() => ShowSkillDescription(1));
            Debug.Log("✅ Skill 1 button setup complete");
        }

        if (skill2Button != null)
        {
            skill2Button.onClick.RemoveAllListeners();
            skill2Button.onClick.AddListener(() => ShowSkillDescription(2));
            Debug.Log("✅ Skill 2 button setup complete");
        }

        if (skill3Button != null)
        {
            skill3Button.onClick.RemoveAllListeners();
            skill3Button.onClick.AddListener(() => ShowSkillDescription(3));
            Debug.Log("✅ Skill 3 button setup complete");
        }

        if (skill4Button != null)
        {
            skill4Button.onClick.RemoveAllListeners();
            skill4Button.onClick.AddListener(() => ShowSkillDescription(4));
            Debug.Log("✅ Skill 4 button setup complete");
        }


        // ✅ Setup Show Character Info Button (Optional)
        if (showCharacterInfoButton != null)
        {
            showCharacterInfoButton.onClick.RemoveAllListeners();
            showCharacterInfoButton.onClick.AddListener(ShowCharacterDescription);
            Debug.Log("✅ Show Character Info button setup complete");
        }
        Debug.Log("=== Skill Buttons Setup Complete ===");
    }

    // ========== 🆕 NEW: Show Skill Description ==========
    private void ShowSkillDescription(int skillIndex)
    {
        currentSelectedSkillIndex = skillIndex;


        // ✅ ดึง description ตามตัวละครที่เลือก 
        if (characterDescriptionText != null)
        {
            string description = GetSkillDescriptionForCharacter(selectedCharacter, skillIndex);
            string skillName = GetSkillName(skillIndex);

            // แสดงคำอธิบายสกิลใน characterDescriptionText
            characterDescriptionText.text = $"<color=yellow><b>{skillName}:</b></color> \n{description}";
        }

        Debug.Log($"[CharacterSelection] Showing {selectedCharacter} skill {skillIndex} description");
    }

    // ========== 🆕 NEW: Show Character Description ==========
    private void ShowCharacterDescription()
    {
        currentSelectedSkillIndex = -1; // Reset skill selection

        // ✅ แสดงคำอธิบายตัวละครกลับ
        if (characterDescriptionText != null && !string.IsNullOrEmpty(currentCharacterBaseDescription))
        {
            characterDescriptionText.text = currentCharacterBaseDescription;
        }

        Debug.Log($"[CharacterSelection] Showing {selectedCharacter} character description");
    }

    // ========== 🆕 NEW: Get Skill Description for Character ==========
    private string GetSkillDescriptionForCharacter(PlayerSelectionData.CharacterType character, int skillIndex)
    {
        switch (character)
        {
            case PlayerSelectionData.CharacterType.BloodKnight:
                switch (skillIndex)
                {
                    case 0: return bloodKnightPassive;
                    case 1: return bloodKnightSkill1;
                    case 2: return bloodKnightSkill2;
                    case 3: return bloodKnightSkill3;
                    case 4: return bloodKnightSkill4;
                }
                break;

            case PlayerSelectionData.CharacterType.Archer:
                switch (skillIndex)
                {
                    case 0: return archerPassive;
                    case 1: return archerSkill1;
                    case 2: return archerSkill2;
                    case 3: return archerSkill3;
                    case 4: return archerSkill4;
                }
                break;

            case PlayerSelectionData.CharacterType.Assassin:
                switch (skillIndex)
                {
                    case 0: return assassinPassive;
                    case 1: return assassinSkill1;
                    case 2: return assassinSkill2;
                    case 3: return assassinSkill3;
                    case 4: return assassinSkill4;
                }
                break;

            case PlayerSelectionData.CharacterType.IronJuggernaut:
                switch (skillIndex)
                {
                    case 0: return ironJuggernautPassive;
                    case 1: return ironJuggernautSkill1;
                    case 2: return ironJuggernautSkill2;
                    case 3: return ironJuggernautSkill3;
                    case 4: return ironJuggernautSkill4;
                }
                break;
        }

        return "Skill description not available.";
    }

    // ========== 🆕 NEW: Get Skill Name ==========
    private string GetSkillName(int skillIndex)
    {
        switch (skillIndex)
        {
            case 0: return "Passive Skill";
            case 1: return "Skill 1";
            case 2: return "Skill 2";
            case 3: return "Skill 3";
            case 4: return "Skill 4";
            default: return "Unknown Skill";
        }
    }

    // ========== 🆕 NEW: Show Story Panel ==========
    private void ShowStoryPanel()
    {
        currentSelectedSkillIndex = -1;

        // แสดง Story Section
        if (storySection != null)
            storySection.SetActive(true);

        // ซ่อน Skill Description Section
        if (skillDescriptionSection != null)
            skillDescriptionSection.SetActive(false);

        Debug.Log("[CharacterSelection] Showing story panel");
    }

    // ========== 🆕 NEW: Load Skill Icons ==========
    private void LoadSkillIconsForCharacter(string characterName)
    {
        if (SkillIconManager.Instance == null)
        {
            Debug.LogWarning("[CharacterSelection] SkillIconManager not found!");
            return;
        }

        Debug.Log($"[CharacterSelection] 🎨 Loading skill icons for: {characterName}");

        // โหลด skill icons สำหรับ skill 1-4
        SkillIconManager.Instance.SetSkillIconsForSelection(
            characterName,
            skill1Icon,
            skill2Icon,
            skill3Icon,
            skill4Icon
        );

        // Passive Icon ไม่ต้องโหลด - คุณจะใส่รูปเองใน Inspector
        Debug.Log($"✅ Skill icons loaded for {characterName}");
    }

    // ========== 🆕 NEW: Update Character Stats (17 Fields) ==========
    private void UpdateCharacterStats(PlayerSelectionData.CharacterType character)
    {
        // หา CharacterStats ScriptableObject
        CharacterStats stats = GetCharacterStatsForCharacter(character);

        if (stats == null)
        {
            Debug.LogWarning($"[CharacterSelection] Could not load stats for {character}");
            return;
        }

        // ดึงข้อมูล character progress ถ้ามี
        CharacterProgressData progressData = PersistentPlayerData.Instance.GetCharacterData(character.ToString());

        // ใช้ total stats ถ้ามี progress data, ไม่งั้นใช้ base stats
        int currentLevel = progressData?.currentLevel ?? 1;
        int maxHp = progressData?.totalMaxHp ?? stats.maxHp;
        int maxMana = progressData?.totalMaxMana ?? stats.maxMana;
        int attackDamage = progressData?.totalAttackDamage ?? stats.attackDamage;
        int magicDamage = progressData?.totalMagicDamage ?? stats.magicDamage;
        int armor = progressData?.totalArmor ?? stats.arrmor;
        int magicArmor = progressData?.totalMagicArmor ?? stats.magicArmor;
        float moveSpeed = progressData?.totalMoveSpeed ?? stats.moveSpeed;
        float critChance = progressData?.totalCriticalChance ?? stats.criticalChance;
        float critDamage = progressData?.totalCriticalDamageBonus ?? stats.criticalDamageBonus;
        float hitRate = progressData?.totalHitRate ?? stats.hitRate;
        float evasionRate = progressData?.totalEvasionRate ?? stats.evasionRate;
        float attackSpeed = progressData?.totalAttackSpeed ?? stats.attackSpeed;
        float cdr = progressData?.totalReductionCoolDown ?? stats.reductionCoolDown;
        float lifeSteal = progressData?.totalLifeSteal ?? stats.lifeSteal;
        float ampDamage = progressData?.totalAmpdamage ?? stats.ampdamage;
        float healthRegen = progressData?.totalHealthRegen ?? stats.healthRegen;
        float manaRegen = progressData?.totalManaRegen ?? stats.manaRegen;

        // แสดง Stats แยก 17 fields
        if (hpText != null)
            hpText.text = $"HP: {maxHp}";

        if (manaText != null)
            manaText.text = $"Mana: {maxMana}";

        if (attackDamageText != null)
            attackDamageText.text = $"ATK: {attackDamage}";

        if (magicDamageText != null)
            magicDamageText.text = $"MAG: {magicDamage}";

        if (armorText != null)
            armorText.text = $"ARM: {armor}";

        if (magicArmorText != null)
            magicArmorText.text = $"MGA: {magicArmor}";

        if (moveSpeedText != null)
            moveSpeedText.text = $"SPD: {moveSpeed:F1}";

        if (criticalChanceText != null)
            criticalChanceText.text = $"CRIT: {critChance:F1}%";

        if (criticalDamageText != null)
            criticalDamageText.text = $"CRIT DMG: {critDamage * 100f:F1}%";

        if (hitRateText != null)
            hitRateText.text = $"HIT: {hitRate:F1}%";

        if (evasionRateText != null)
            evasionRateText.text = $"EVA: {evasionRate:F1}%";

        if (attackSpeedText != null)
            attackSpeedText.text = $"AS: x{attackSpeed:F2}";

        if (reductionCoolDownText != null)
            reductionCoolDownText.text = $"CDR: {cdr:F1}%";

        if (lifeStealText != null)
            lifeStealText.text = $"LST: {lifeSteal:F1}%";

        if (ampDamageText != null)
            ampDamageText.text = $"AMP: {ampDamage:F1}%";

        if (healthRegenText != null)
            healthRegenText.text = $"HP+: {healthRegen:F1}/s";

        if (manaRegenText != null)
            manaRegenText.text = $"MP+: {manaRegen:F1}/s";

        Debug.Log($"[CharacterSelection] Updated stats for {character} (Level {currentLevel})");
    }

    private IEnumerator InitializeCharacterSelection()
    {
        // รอให้ PersistentPlayerData โหลดเสร็จก่อน
        float timeout = 3f;
        float elapsed = 0f;

        while (!PersistentPlayerData.Instance.HasValidData() && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return new WaitForSeconds(0.1f);
        }

        // ✅ แสดงรายการตัวละครทั้งหมด
        ShowCharacterLevels();

        if (comingFromLobby)
        {
            // ✅ ถ้ามาจาก Lobby ให้เลือก current active character
            string activeCharacter = PersistentPlayerData.Instance.GetCurrentActiveCharacter();
            if (System.Enum.TryParse<PlayerSelectionData.CharacterType>(activeCharacter, out var activeCharacterType))
            {
                SelectCharacter(activeCharacterType);
            }
            else
            {
                SelectCharacter(PlayerSelectionData.CharacterType.Assassin); // Fallback
            }

            // ซ่อน name input ถ้ามาจาก Lobby
            if (playerNameInput != null)
                playerNameInput.gameObject.SetActive(false);
        }
        else
        {
            // ✅ ผู้เล่นใหม่ - แสดงตัวละครเริ่มต้น (Assassin)
            SelectCharacter(PlayerSelectionData.CharacterType.Assassin);
        }

        Debug.Log($"[CharacterSelection] Initialized - Coming from Lobby: {comingFromLobby}");
    }

    private void BackToLobby()
    {
        PlayerPrefs.SetString("LastScene", "CharacterSelection");
        SceneManager.LoadScene("Lobby");
    }

    private IEnumerator ConfirmSelectionCoroutine()
    {
        string playerName;

        if (comingFromLobby)
        {
            playerName = PersistentPlayerData.Instance.GetPlayerName();
        }
        else
        {
            playerName = playerNameInput.text.Trim();

            if (string.IsNullOrEmpty(playerName))
            {
                ShowError("Please enter your name!");
                yield break;
            }

            if (playerName.Length < 3 || playerName.Length > 16)
            {
                ShowError("Name must be 3-16 characters!");
                yield break;
            }
        }

        ShowLoading(true);

        PlayerPrefs.SetString("PlayerName", playerName);

        Debug.Log($"[CharacterSelection] ✅ Confirmed selection: {selectedCharacter}");

        if (comingFromLobby)
        {
            yield return StartCoroutine(HandleCharacterSwitchFromLobby(playerName));
        }
        else
        {
            yield return StartCoroutine(CreateNewMultiCharacterPlayer(playerName));
        }

        ShowLoading(false);
    }

    private IEnumerator HandleCharacterSwitchFromLobby(string playerName)
    {
        Debug.Log($"[CharacterSelection] Switching character to {selectedCharacter} for existing player");

        // ✅ ใช้ Multi-Character System
        if (PersistentPlayerData.Instance.multiCharacterData != null)
        {
            // Switch character
            PersistentPlayerData.Instance.SwitchCharacter(selectedCharacter.ToString());

            // Ensure character data exists
            yield return StartCoroutine(EnsureCharacterDataExists(playerName, selectedCharacter.ToString()));

            // รอให้ save เสร็จ
            yield return new WaitForSeconds(0.5f);

            Debug.Log($"✅ Character switched to {selectedCharacter}");
        }
        else
        {
            Debug.LogError("[CharacterSelection] No multiCharacterData found!");
            // Create new multi-character data
            yield return StartCoroutine(CreateNewMultiCharacterPlayer(playerName));
        }

        // กลับไป Lobby
        PlayerPrefs.SetString("LastScene", "CharacterSelection");
        SceneManager.LoadScene("Lobby");
    }

    private IEnumerator CreateNewMultiCharacterPlayer(string playerName)
    {
        Debug.Log($"[CharacterSelection] Creating new multi-character player: {playerName}");

        // ✅ สร้าง MultiCharacterPlayerData ใหม่
        MultiCharacterPlayerData newMultiCharacterData = new MultiCharacterPlayerData();
        newMultiCharacterData.playerName = playerName;
        newMultiCharacterData.currentActiveCharacter = selectedCharacter.ToString();
        newMultiCharacterData.registrationDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        newMultiCharacterData.lastLoginDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // ✅ สร้างเฉพาะตัวละครที่เลือก
        Debug.Log($"[CharacterSelection] Creating character: {selectedCharacter}");

        CharacterProgressData newCharacterData = newMultiCharacterData.GetOrCreateCharacterData(selectedCharacter.ToString());

        // Apply stats from ScriptableObject
        CharacterStats characterStats = GetCharacterStatsForCharacter(selectedCharacter);
        if (characterStats != null)
        {
            ApplyStatsFromScriptableObject(newCharacterData, characterStats);
            Debug.Log($"✅ Applied ScriptableObject stats for {selectedCharacter}");
        }
        else
        {
            Debug.LogWarning($"⚠️ Could not load stats for {selectedCharacter}, using defaults");
        }

        // Set ใน PersistentPlayerData
        PersistentPlayerData.Instance.multiCharacterData = newMultiCharacterData;
        PersistentPlayerData.Instance.isDataLoaded = true;

        // Save to Firebase
        PersistentPlayerData.Instance.SavePlayerDataAsync();

        // รอให้ save เสร็จ
        yield return new WaitForSeconds(1f);

        Debug.Log($"✅ New multi-character player created: {playerName}, Active: {selectedCharacter}");
        newMultiCharacterData.LogAllCharacters();

        // ไป Lobby
        PlayerPrefs.SetString("LastScene", "CharacterSelection");
        SceneManager.LoadScene("Lobby");
    }

    private IEnumerator EnsureCharacterDataExists(string playerName, string characterType)
    {
        Debug.Log($"[CharacterSelection] Ensuring character data exists for {characterType}");

        // Check if character already exists
        CharacterProgressData existingData = PersistentPlayerData.Instance.GetCharacterData(characterType);

        if (existingData != null)
        {
            Debug.Log($"✅ Character {characterType} already exists at level {existingData.currentLevel}");
            yield break;
        }

        // Create new character data
        Debug.Log($"[CharacterSelection] Creating new character data for {characterType}");

        CharacterProgressData newCharacterData = PersistentPlayerData.Instance.multiCharacterData.GetOrCreateCharacterData(characterType);

        // Apply stats from ScriptableObject if available
        CharacterStats characterStats = GetCharacterStatsForCharacter(GetCharacterTypeEnum(characterType));
        if (characterStats != null)
        {
            ApplyStatsFromScriptableObject(newCharacterData, characterStats);
            Debug.Log($"✅ Applied ScriptableObject stats for {characterType}");
        }

        // Save the data
        PersistentPlayerData.Instance.SavePlayerDataAsync();

        yield return new WaitForSeconds(0.5f);
    }

    private PlayerSelectionData.CharacterType GetCharacterTypeEnum(string characterType)
    {
        if (System.Enum.TryParse<PlayerSelectionData.CharacterType>(characterType, out var result))
            return result;
        return PlayerSelectionData.CharacterType.Assassin;
    }

    // ========== หา CharacterStats ScriptableObject ==========
    private CharacterStats GetCharacterStatsForCharacter(PlayerSelectionData.CharacterType characterType)
    {
        string characterName = characterType.ToString();

        // ลองหาจาก Resources folder
        CharacterStats[] allCharacterStats = Resources.LoadAll<CharacterStats>("Characters");

        foreach (CharacterStats stats in allCharacterStats)
        {
            if (stats.name.Contains(characterName) ||
                stats.characterName.Equals(characterName, System.StringComparison.OrdinalIgnoreCase))
            {
                return stats;
            }
        }

        // ลองหาจากชื่อไฟล์โดยตรง
        switch (characterType)
        {
            case PlayerSelectionData.CharacterType.BloodKnight:
                return Resources.Load<CharacterStats>("Characters/BloodKnightStats");
            case PlayerSelectionData.CharacterType.Archer:
                return Resources.Load<CharacterStats>("Characters/ArcherStats");
            case PlayerSelectionData.CharacterType.Assassin:
                return Resources.Load<CharacterStats>("Characters/AssassinStats");
            case PlayerSelectionData.CharacterType.IronJuggernaut:
                return Resources.Load<CharacterStats>("Characters/IronJuggernautStats");
            default:
                Debug.LogWarning($"Unknown character type: {characterType}");
                return null;
        }
    }

    // ========== Apply Stats from ScriptableObject ==========
    private void ApplyStatsFromScriptableObject(CharacterProgressData characterData, CharacterStats stats)
    {
        characterData.totalMaxHp = stats.maxHp;
        characterData.totalMaxMana = stats.maxMana;
        characterData.totalAttackDamage = stats.attackDamage;
        characterData.totalMagicDamage = stats.magicDamage;
        characterData.totalArmor = stats.arrmor;
        characterData.totalMagicArmor = stats.magicArmor;
        characterData.totalCriticalChance = stats.criticalChance;
        characterData.totalCriticalDamageBonus = stats.criticalDamageBonus;
        characterData.totalMoveSpeed = stats.moveSpeed;
        characterData.totalAttackRange = stats.attackRange;
        characterData.totalAttackCooldown = stats.attackCoolDown;
        characterData.totalHitRate = stats.hitRate;
        characterData.totalEvasionRate = stats.evasionRate;
        characterData.totalAttackSpeed = stats.attackSpeed;
        characterData.totalReductionCoolDown = stats.reductionCoolDown;
        characterData.totalLifeSteal = stats.lifeSteal;
        characterData.totalAmpdamage = stats.ampdamage;
        characterData.totalHealthRegen = stats.healthRegen;
        characterData.totalManaRegen = stats.manaRegen;
    }

    // ========== UI Methods ==========
    private void ShowError(string message)
    {
        if (errorMessageText != null)
        {
            errorMessageText.text = message;
            errorMessageText.gameObject.SetActive(true);
            Invoke("HideError", 3f);
        }
    }

    private void HideError()
    {
        if (errorMessageText != null)
        {
            errorMessageText.gameObject.SetActive(false);
        }
    }

    private void ShowLoading(bool show)
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(show);

        confirmButton.interactable = !show;
        if (playerNameInput != null) playerNameInput.interactable = !show;
        bloodKnightButton.interactable = !show;
        archerButton.interactable = !show;
        assassinButton.interactable = !show;
        ironJuggernautButton.interactable = !show;
    }

    public void SelectCharacter(PlayerSelectionData.CharacterType character)
    {
        // บันทึกตัวละครที่เลือก
        selectedCharacter = character;

        // ลบตัวละครตัวอย่างเดิม (ถ้ามี)
        if (currentPreview != null)
        {
            Destroy(currentPreview);
        }

        // สร้างตัวละครตัวอย่าง
        GameObject prefabToSpawn = GetPrefabForCharacter(character);
        if (prefabToSpawn != null)
        {
            currentPreview = Instantiate(prefabToSpawn, previewPosition, Quaternion.Euler(previewRotation), characterPreviewParent);
            DisableComponents(currentPreview);
        }

        // ✅ อัพเดท Character Info (Story)
        UpdateCharacterInfo(character);

        // ✅ อัพเดท Character Stats (Detailed)
        UpdateCharacterStats(character);

        // ✅ โหลด Skill Icons
        LoadSkillIconsForCharacter(character.ToString());

        // ✅ แสดง Story Panel (Default)
        ShowStoryPanel();

        Debug.Log($"[CharacterSelection] Selected character: {character}");
    }

    private void ShowCharacterLevel(string characterType)
    {
        CharacterProgressData characterData = PersistentPlayerData.Instance.GetCharacterData(characterType);

        if (characterData != null)
        {
            // Update character name text to include level
            characterNameText.text = $"{GetDisplayName(characterType)} (Level {characterData.currentLevel})";
        }
        else
        {
            characterNameText.text = $"{GetDisplayName(characterType)} (New Character)";
        }
    }

    private void ShowCharacterLevels()
    {
        if (characterLevelsText == null) return;

        List<CharacterProgressData> allCharacters = PersistentPlayerData.Instance.GetAllCharacterData();
        string currentActive = PersistentPlayerData.Instance.GetCurrentActiveCharacter();

        string levelsText = "<color=yellow>Your Characters:</color>\n";

        string[] allCharacterTypes = { "BloodKnight", "Archer", "Assassin", "IronJuggernaut" };

        foreach (string characterType in allCharacterTypes)
        {
            CharacterProgressData characterData = allCharacters.Find(c => c.characterType == characterType);

            if (characterData != null)
            {
                string color = (characterType == currentActive) ? "yellow" : "white";
                levelsText += $"<color={color}>• {GetDisplayName(characterType)} - Level {characterData.currentLevel}</color>\n";
            }
            else
            {
                levelsText += $"<color=gray>• {GetDisplayName(characterType)} - New</color>\n";
            }
        }

        characterLevelsText.text = levelsText;
    }

    private string GetDisplayName(string characterType)
    {
        switch (characterType)
        {
            case "BloodKnight": return "Blood Knight";
            case "Archer": return "Archer";
            case "Assassin": return "Assassin";
            case "IronJuggernaut": return "Iron Juggernaut";
            default: return characterType;
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
                return ironJuggernautPrefab;
        }
    }

    private void DisableComponents(GameObject character)
    {
        // ปิด scripts ที่ไม่จำเป็นในหน้าเลือกตัวละคร
        MonoBehaviour[] components = character.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour component in components)
        {
            if (!(component is Animator))
            {
                component.enabled = false;
            }
        }

        // ปิด colliders
        Collider[] colliders = character.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }

        // ปิด rigidbody
        Rigidbody rb = character.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    private void UpdateCharacterInfo(PlayerSelectionData.CharacterType character)
    {
        // Story/Lore text (ใช้เดิม)
        string baseDescription = "";

        switch (character)
        {
            case PlayerSelectionData.CharacterType.BloodKnight:
                baseDescription = "Ferris Buff: Increased life steal\r\nPassive: Restores HP on hit\r\nSkill 2: Summons 6 blood spears orbiting the user\r\nSkill 3: Greatly increases healing power";
                break;
            case PlayerSelectionData.CharacterType.Archer:
                baseDescription = "Ferris Buff: Faster attack speed / Shoots 2 arrows per attack\r\nPassive: Double Hit chance increased to 40%\r\nSkill 1: Shoots 2 arrows forward\r\nSkill 2: Dash forward, leaving a trail of fire";
                break;
            case PlayerSelectionData.CharacterType.Assassin:
                baseDescription = "Ferris Buff: Increased movement speed\r\nSkill 2: Dash twice per cooldown\r\nSkill 3: Strikes 6 consecutive attacks";
                break;
            case PlayerSelectionData.CharacterType.IronJuggernaut:
                baseDescription = "Ferris Buff: Damage taken reduced\r\nPassive: Stronger damage reflection\r\nSkill 1: Slams the ground in an X-shaped attack\r\nSkill 4: Deals AoE damage around the user";
                break;
        }

        currentCharacterBaseDescription = baseDescription;
        characterDescriptionText.text = baseDescription;

        // Show character level
        ShowCharacterLevel(character.ToString());
    }
}