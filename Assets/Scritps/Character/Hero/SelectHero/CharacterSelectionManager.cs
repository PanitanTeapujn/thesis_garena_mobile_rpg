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

    // เพิ่ม Loading Panel
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
       

        // ซ่อน loading panel
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    // เปลี่ยนเป็น Coroutine เพื่อรอการบันทึกข้อมูล
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

        // บันทึกข้อมูลใน Firebase และรอให้เสร็จ
        yield return StartCoroutine(SaveCharacterAndPlayerDataToFirebase(playerName));

        // ซ่อน loading
        ShowLoading(false);

        // ไปหน้า Lobby
        SceneManager.LoadScene("Lobby");
    }

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

        // ปิด/เปิด UI elements
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

            // ปิดส่วนประกอบที่ไม่จำเป็น (เช่น scripts, colliders)
            DisableComponents(currentPreview);
        }

        // อัพเดทข้อมูลตัวละคร
        UpdateCharacterInfo(character);

        // เอาการบันทึก Firebase ออกจากตรงนี้ เพราะจะทำตอน Confirm แทน
        // SaveCharacterToFirebase(character);
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
            if (!(component is Animator))  // ให้ Animator ทำงานต่อไป
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
                characterDescriptionText.text = "Blood Siphon grew up in an aristocratic family of the insect tribe, but rejected the luxurious life to join the army.With his remarkable ability to absorb the blood and life force of his enemies, he was appointed as one of the insect's elite soldiers. ";
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

    // ฟังก์ชันใหม่ที่บันทึกทั้งชื่อและตัวละครพร้อมกัน
    private IEnumerator SaveCharacterAndPlayerDataToFirebase(string playerName)
    {
        if (auth.CurrentUser != null)
        {
            string userId = auth.CurrentUser.UserId;

            var updates = new Dictionary<string, object>
            {
                { "playerName", playerName },
                { "lastCharacterSelected", selectedCharacter.ToString() },
                { "lastLoginDate", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
            };

            var task = databaseReference.Child("players").Child(userId).UpdateChildrenAsync(updates);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.Exception != null)
            {
                Debug.LogError($"Failed to save character and player data: {task.Exception}");
                ShowError("Failed to save data. Please try again.");
            }
            else
            {
                Debug.Log($"Character and player data saved successfully: {playerName}, {selectedCharacter}");
            }
        }
        else
        {
            Debug.LogError("User not authenticated");
            ShowError("User not authenticated. Please login again.");
        }
    }

    private void StartGame()
    {
        // โหลดฉากเล่นเกม
        SceneManager.LoadScene("PlayRoom1");
    }

    // เก็บฟังก์ชันเก่าไว้ในกรณีที่ต้องการใช้
    void SaveCharacterToFirebase(PlayerSelectionData.CharacterType character)
    {
        if (auth.CurrentUser != null)
        {
            string userId = auth.CurrentUser.UserId;

            var updates = new Dictionary<string, object>
            {
                { "lastCharacterSelected", character.ToString() }
            };

            databaseReference.Child("players").Child(userId).UpdateChildrenAsync(updates)
                .ContinueWith(task =>
                {
                    if (task.Exception != null)
                    {
                        Debug.LogError($"Failed to save character selection: {task.Exception}");
                    }
                    else
                    {
                        Debug.Log($"Character selection saved: {character}");
                    }
                });
        }
    }
}