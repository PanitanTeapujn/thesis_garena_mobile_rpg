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
    public TextMeshProUGUI characterDescriptionText;
    public TextMeshProUGUI characterNameText;

    [Header("Loading")]
    public GameObject loadingPanel;

    private GameObject currentPreview;
    private PlayerSelectionData.CharacterType selectedCharacter;

    private FirebaseAuth auth;
    private DatabaseReference databaseReference;

    private void Start()
    {
        // เชื่อมปุ่มกับฟังก์ชันเลือกตัวละคร
        bloodKnightButton.onClick.AddListener(() => SelectCharacter(PlayerSelectionData.CharacterType.BloodKnight));
        archerButton.onClick.AddListener(() => SelectCharacter(PlayerSelectionData.CharacterType.Archer));
        assassinButton.onClick.AddListener(() => SelectCharacter(PlayerSelectionData.CharacterType.Assassin));
        ironJuggernautButton.onClick.AddListener(() => SelectCharacter(PlayerSelectionData.CharacterType.IronJuggernaut));

        confirmButton.onClick.AddListener(() => StartCoroutine(ConfirmSelectionCoroutine()));

        // แสดงตัวละครเริ่มต้น
        SelectCharacter(PlayerSelectionData.GetSelectedCharacter());

        auth = FirebaseAuth.DefaultInstance;
        databaseReference = FirebaseDatabase.DefaultInstance.RootReference;

        // โหลดชื่อผู้เล่นจาก PlayerPrefs (ถ้ามี)
        string savedPlayerName = PlayerPrefs.GetString("PlayerName", "");
        if (!string.IsNullOrEmpty(savedPlayerName))
        {
            playerNameInput.text = savedPlayerName;
        }

        // ซ่อน loading panel
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    private IEnumerator ConfirmSelectionCoroutine()
    {
        // ตรวจสอบชื่อ
        string playerName = playerNameInput.text.Trim();

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

        // แสดง loading
        ShowLoading(true);

        // บันทึกข้อมูลใน PlayerPrefs
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerSelectionData.SaveCharacterSelection(selectedCharacter);

        // สร้าง PlayerProgressData แบบเต็มและบันทึกผ่าน PersistentPlayerData
        yield return StartCoroutine(CreateAndSaveCompletePlayerData(playerName));

        // ซ่อน loading
        ShowLoading(false);

        // ไปหน้า Lobby
        SceneManager.LoadScene("Lobby");
    }

    // ========== NEW: สร้าง PlayerProgressData แบบเต็ม ==========
    private IEnumerator CreateAndSaveCompletePlayerData(string playerName)
    {
        Debug.Log($"[CharacterSelection] Creating complete player data for {playerName}, Character: {selectedCharacter}");

        // สร้าง PlayerProgressData ใหม่แบบเต็ม
        PlayerProgressData newPlayerData = new PlayerProgressData();

        // ข้อมูลพื้นฐาน
        newPlayerData.playerName = playerName;
        newPlayerData.lastCharacterSelected = selectedCharacter.ToString(); // ✅ ตรงนี้สำคัญ!
        newPlayerData.registrationDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        newPlayerData.lastLoginDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // โหลด base stats จาก CharacterStats ScriptableObject
        CharacterStats characterStats = GetCharacterStatsForCharacter(selectedCharacter);
        if (characterStats != null)
        {
            newPlayerData.InitializeFromCharacterStats(characterStats, 1);
            Debug.Log($"✅ Applied stats from ScriptableObject: {characterStats.characterName}");
        }
        else
        {
            Debug.LogWarning($"Could not find CharacterStats for {selectedCharacter}. Using default stats.");
            ApplyDefaultStats(newPlayerData);
        }

        // ใส่ข้อมูลใน PersistentPlayerData
        PersistentPlayerData.Instance.currentPlayerData = newPlayerData;
        PersistentPlayerData.Instance.isDataLoaded = true;

        // บันทึกลง Firebase ผ่าน PersistentPlayerData
        PersistentPlayerData.Instance.SavePlayerDataAsync();

        // รอให้ save เสร็จ
        yield return new WaitForSeconds(1f);

        Debug.Log($"✅ Complete player data created and saved: {playerName}, {selectedCharacter}");
        newPlayerData.LogProgressInfo();
    }

    // ========== NEW: หา CharacterStats ScriptableObject ==========
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

    // ========== NEW: Default Stats Fallback ==========
    private void ApplyDefaultStats(PlayerProgressData playerData)
    {
        // Default stats สำหรับแต่ละตัวละคร
        switch (selectedCharacter)
        {
            case PlayerSelectionData.CharacterType.BloodKnight:
                playerData.totalMaxHp = 120;
                playerData.totalMaxMana = 60;
                playerData.totalAttackDamage = 25;
                playerData.totalArmor = 8;
                break;
            case PlayerSelectionData.CharacterType.Archer:
                playerData.totalMaxHp = 80;
                playerData.totalMaxMana = 80;
                playerData.totalAttackDamage = 30;
                playerData.totalArmor = 3;
                break;
            case PlayerSelectionData.CharacterType.Assassin:
                playerData.totalMaxHp = 70;
                playerData.totalMaxMana = 40;
                playerData.totalAttackDamage = 35;
                playerData.totalArmor = 2;
                break;
            case PlayerSelectionData.CharacterType.IronJuggernaut:
            default:
                playerData.totalMaxHp = 150;
                playerData.totalMaxMana = 40;
                playerData.totalAttackDamage = 20;
                playerData.totalArmor = 12;
                break;
        }

        // Common stats
        playerData.currentLevel = 1;
        playerData.currentExp = 0;
        playerData.expToNextLevel = 100;
        playerData.totalCriticalChance = 5f;
        playerData.totalCriticalMultiplier = 2f;
        playerData.totalMoveSpeed = 5f;
        playerData.totalAttackRange = 2f;
        playerData.totalAttackCooldown = 1f;

        Debug.Log($"Applied default stats for {selectedCharacter}");
    }

    // ========== UI Methods (เหมือนเดิม) ==========
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
        playerNameInput.interactable = !show;
        bloodKnightButton.interactable = !show;
        archerButton.interactable = !show;
        assassinButton.interactable = !show;
        ironJuggernautButton.interactable = !show;
    }

    public void SelectCharacter(PlayerSelectionData.CharacterType character)
    {
        // บันทึกตัวละครที่เลือก
        selectedCharacter = character;
        PlayerSelectionData.SaveCharacterSelection(character);

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

        // อัพเดทข้อมูลตัวละคร
        UpdateCharacterInfo(character);

        Debug.Log($"[CharacterSelection] Selected character: {character}");
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
        switch (character)
        {
            case PlayerSelectionData.CharacterType.BloodKnight:
                characterNameText.text = "Blood Knight";
                characterDescriptionText.text = "Blood Siphon grew up in an aristocratic family of the insect tribe, but rejected the luxurious life to join the army. With his remarkable ability to absorb the blood and life force of his enemies, he was appointed as one of the insect's elite soldiers.";
                break;
            case PlayerSelectionData.CharacterType.Archer:
                characterNameText.text = "Archer";
                characterDescriptionText.text = "Talon was born in the kingdom of Aviana, a land high above the clouds that the Bird Clan had ruled for thousands of years. From a young age, he displayed remarkable talent for archery, able to hit the target with his arrows every time, even at the age of 1.";
                break;
            case PlayerSelectionData.CharacterType.Assassin:
                characterNameText.text = "Assassin";
                characterDescriptionText.text = "Shadow Prowler lost her family at a young age. She was adopted by the Shadow Claw Assassins Association and trained to become the Cat Clan's most skilled assassin. She specializes in poison and silent movement, earning the nickname Invisible Shadow.";
                break;
            case PlayerSelectionData.CharacterType.IronJuggernaut:
                characterNameText.text = "Iron Juggernaut";
                characterDescriptionText.text = "Legend has it that the Iron Rhino tribe was born from ancient warriors who inhaled fumes from forging mystical metal over many years, causing their bodies to develop steel-like properties. From a young age, Iron Rhinos are trained to master the use of their body weight and raw strength to their fullest advantage. Their primary weapons are a sword and shield forged from special volcanic steel, making them exceptionally durable. The rhino horn on their heads can also be used as a weapon in times of dire need.";
                break;
        }
    }

    // ========== Context Menu สำหรับ Debug ==========
    [ContextMenu("Test Create Player Data")]
    public void Debug_TestCreatePlayerData()
    {
        StartCoroutine(CreateAndSaveCompletePlayerData("TestPlayer"));
    }

    [ContextMenu("Check Selected Character")]
    public void Debug_CheckSelectedCharacter()
    {
        Debug.Log($"Current Selected Character: {selectedCharacter}");
        Debug.Log($"PlayerSelectionData: {PlayerSelectionData.GetSelectedCharacter()}");
    }
}