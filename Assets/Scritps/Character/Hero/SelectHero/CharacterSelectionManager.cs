using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
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
    private GameObject currentPreview;
    private PlayerSelectionData.CharacterType selectedCharacter;

    private void Start()
    {
        // เชื่อมปุ่มกับฟังก์ชันเลือกตัวละคร
        bloodKnightButton.onClick.AddListener(() => SelectCharacter(PlayerSelectionData.CharacterType.BloodKnight));
        archerButton.onClick.AddListener(() => SelectCharacter(PlayerSelectionData.CharacterType.Archer));
        assassinButton.onClick.AddListener(() => SelectCharacter(PlayerSelectionData.CharacterType.Assassin));
        ironJuggernautButton.onClick.AddListener(() => SelectCharacter(PlayerSelectionData.CharacterType.IronJuggernaut));

        confirmButton.onClick.AddListener(ConfirmSelection);

        // แสดงตัวละครเริ่มต้น
        SelectCharacter(PlayerSelectionData.GetSelectedCharacter());
    }
    private void ConfirmSelection()
    {
        // ตรวจสอบชื่อ
        string playerName = playerNameInput.text.Trim();

        if (string.IsNullOrEmpty(playerName))
        {
            ShowError("Please enter your name!");
            return;
        }

        if (playerName.Length < 3 || playerName.Length > 16)
        {
            ShowError("Name must be 3-16 characters!");
            return;
        }

        // บันทึกข้อมูล
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerSelectionData.SaveCharacterSelection(selectedCharacter);

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
                characterDescriptionText.text = "Blood Siphon เติบโตในตระกูลชนชั้นสูงของเผ่าแมลง แต่ปฏิเสธชีวิตหรูหราเพื่อเข้าร่วมกองทัพ ด้วยความสามารถที่โดดเด่นในการดูดซับเลือดและพลังชีวิตของศัตรู เขาได้รับการแต่งตั้งให้เป็นหนึ่งในยแมลงทหารชั้นยอด ";
                break;
            case PlayerSelectionData.CharacterType.Archer:
                characterNameText.text = "Archer";
                characterDescriptionText.text = "Talon เกิดในอาณาจักร Aviana ดินแดนสูงเหนือเมฆที่เผ่านกครอบครองมานานนับพันปี ตั้งแต่เด็ก เขาแสดงพรสวรรค์อันโดดเด่นในการใช้ธนู สามารถยิงธนูเข้าเป้าได้ทุกครั้งแม้ในอายุเพียง 1 ขวบ";
                break;
            case PlayerSelectionData.CharacterType.Assassin:
                characterNameText.text = "Assassin";
                characterDescriptionText.text = "Shadow Prowler สูญเสียครอบครัวในวัยเด็กเธอถูกรับเลี้ยงโดยสมาคมนักฆ่า Shadow Claw และฝึกฝนจนกลายเป็นนักฆ่าที่ชำนาญที่สุดของเผ่าแมว เธอเชี่ยวชาญการใช้พิษและการเคลื่อนไหวเงียบกริบ โดยได้รับฉายาว่า เงาที่มองไม่เห็น";
                break;
            case PlayerSelectionData.CharacterType.IronJuggernaut:
                characterNameText.text = "Iron Juggernaut";
                characterDescriptionText.text = "ตำนานเล่าว่า เผ่าแรดเหล็กเกิดจากการที่นักรบโบราณได้สูดดมไอจากการหลอมโลหะวิเศษเป็นเวลานาน จนร่างกายพัฒนาคุณสมบัติคล้ายเหล็กกล้า Iron Rhino ได้รับการฝึกฝนตั้งแต่เด็กให้เรียนรู้การใช้น้ำหนักตัวและพละกำลังให้เกิดประโยชน์สูงสุดอาวุธหลักคือดาบและโล่ที่ตีขึ้นจากเหล็กพิเศษจากภูเขาไฟ ทำให้แข็งแกร่งเป็นพิเศษ นอแรดบนศีรษะสามารถใช้เป็นอาวุธได้เช่นกันในยามคับขัน";
                break;
        }
    }

    private void StartGame()
    {
        // โหลดฉากเล่นเกม
        SceneManager.LoadScene("PlayRoom1");
    }
}