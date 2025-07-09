using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class GachaUIManager : MonoBehaviour
{
    [Header("Main UI Panels")]
    public GameObject gachaMainPanel;
    public GameObject gachaResultPanel;
    public GameObject machineSelectionPanel;

    [Header("Machine Selection")]
    public Transform machineButtonContainer;
    public Button machineButtonPrefab;

    [Header("Current Machine UI")]
    public TextMeshProUGUI machineNameText;
    public TextMeshProUGUI machineDescriptionText;
    public Image machineIconImage;
    public TextMeshProUGUI costSingleText;
    public TextMeshProUGUI costTenText;
    public Button rollSingleButton;
    public Button rollTenButton;

    [Header("Results Display")]
    public Transform resultItemContainer;
    public GameObject resultItemPrefab;
    public Button closeResultsButton;

    [Header("Effects")]
    public ParticleSystem gachaOpenEffect;
    public AudioSource uiAudioSource;
    public AudioClip buttonClickSound;
    public AudioClip gachaOpenSound;
    public AudioClip rareItemSound;

    [Header("Error Display")]
    public GameObject errorPanel;
    public TextMeshProUGUI errorMessageText;
    public Button closeErrorButton;

    #region Private Variables
    private GachaMachine currentMachine;
    private List<Button> machineButtons = new List<Button>();
    #endregion

    #region Initialization
    void Start()
    {
        InitializeUI();
        SetupButtonEvents();
        RefreshMachineSelection();
    }

    private void InitializeUI()
    {
        // ซ่อน panels ทั้งหมดตอนเริ่มต้น
        if (gachaMainPanel != null) gachaMainPanel.SetActive(false);
        if (gachaResultPanel != null) gachaResultPanel.SetActive(false);
        if (machineSelectionPanel != null) machineSelectionPanel.SetActive(false);
        if (errorPanel != null) errorPanel.SetActive(false);

        Debug.Log(" GachaUIManager initialized");
    }

    private void SetupButtonEvents()
    {
        if (rollSingleButton != null)
            rollSingleButton.onClick.AddListener(() => RollGacha(1));

        if (rollTenButton != null)
            rollTenButton.onClick.AddListener(() => RollGacha(10));

        if (closeResultsButton != null)
            closeResultsButton.onClick.AddListener(CloseResults);

        if (closeErrorButton != null)
            closeErrorButton.onClick.AddListener(CloseError);
    }
    #endregion

    #region Machine Selection
    public void RefreshMachineSelection()
    {
        ClearMachineButtons();

        if (GachaSystem.Instance == null) return;

        foreach (GachaMachine machine in GachaSystem.Instance.AllMachines)
        {
            CreateMachineButton(machine);
        }

        Debug.Log($" Refreshed machine selection: {GachaSystem.Instance.MachineCount} machines");
    }

    private void ClearMachineButtons()
    {
        foreach (Button button in machineButtons)
        {
            if (button != null) Destroy(button.gameObject);
        }
        machineButtons.Clear();
    }

    private void CreateMachineButton(GachaMachine machine)
    {
        if (machineButtonPrefab == null || machineButtonContainer == null) return;

        Button newButton = Instantiate(machineButtonPrefab, machineButtonContainer);
        machineButtons.Add(newButton);

        // Setup button text and icon
        TextMeshProUGUI buttonText = newButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = machine.machineName;
        }

        Image buttonIcon = newButton.transform.Find("Icon")?.GetComponent<Image>();
        if (buttonIcon != null && machine.Pool != null && machine.Pool.poolIcon != null)
        {
            buttonIcon.sprite = machine.Pool.poolIcon;
        }

        // Setup button click event
        newButton.onClick.AddListener(() => SelectMachine(machine));
    }

    private void SelectMachine(GachaMachine machine)
    {
        currentMachine = machine;
        UpdateMachineUI();
        ShowGachaPanel();
        PlayButtonSound();

        Debug.Log($" Selected machine: {machine.machineName}");
    }
    #endregion

    #region UI Display
    public void ShowMachineSelection()
    {
        if (machineSelectionPanel != null)
        {
            machineSelectionPanel.SetActive(true);
            RefreshMachineSelection();
        }
    }

    public void ShowGachaPanel()
    {
        if (gachaMainPanel != null)
        {
            gachaMainPanel.SetActive(true);
        }
        if (machineSelectionPanel != null)
        {
            machineSelectionPanel.SetActive(false);
        }
    }

    private void UpdateMachineUI()
    {
        if (currentMachine == null || currentMachine.Pool == null) return;

        // Update machine info
        if (machineNameText != null)
            machineNameText.text = currentMachine.machineName;

        if (machineDescriptionText != null)
            machineDescriptionText.text = currentMachine.Pool.description;

        if (machineIconImage != null && currentMachine.Pool.poolIcon != null)
            machineIconImage.sprite = currentMachine.Pool.poolIcon;

        // Update cost display
        if (costSingleText != null)
            costSingleText.text = $"{currentMachine.Pool.costPerRoll} {currentMachine.Pool.costCurrency}";

        if (costTenText != null)
            costTenText.text = $"{currentMachine.Pool.costPerTenRolls} {currentMachine.Pool.costCurrency}";

        // Update guarantee display if needed
        UpdateGuaranteeDisplay();
    }

    private void UpdateGuaranteeDisplay()
    {
        // TODO: แสดงข้อมูล guarantee counter
    }
    #endregion

    #region Gacha Operations
    private void RollGacha(int rollCount)
    {
        if (currentMachine == null)
        {
            ShowErrorMessage("No machine selected!");
            return;
        }

        PlayButtonSound();

        // TODO: ตรวจสอบ currency ก่อนสุ่ม

        // เริ่มสุ่ม
        Debug.Log($" Rolling {rollCount} times on {currentMachine.machineName}");

        // เล่น effect
        if (gachaOpenEffect != null)
        {
            gachaOpenEffect.Play();
        }

        if (uiAudioSource != null && gachaOpenSound != null)
        {
            uiAudioSource.PlayOneShot(gachaOpenSound);
        }

        // ทำการสุ่ม
        List<GachaReward> rewards = currentMachine.Roll(rollCount);

        // แสดงผลลัพธ์
        StartCoroutine(ShowResultsDelayed(rewards));
    }

    private IEnumerator ShowResultsDelayed(List<GachaReward> rewards)
    {
        // รอให้ effect เล่นจบ
        yield return new WaitForSeconds(1f);

        ShowGachaResults(currentMachine, rewards);
    }
    #endregion

    #region Results Display
    public void ShowGachaResults(GachaMachine machine, List<GachaReward> rewards)
    {
        if (gachaResultPanel == null) return;

        // ซ่อน panel หลัก
        if (gachaMainPanel != null)
            gachaMainPanel.SetActive(false);

        // แสดง result panel
        gachaResultPanel.SetActive(true);

        // เคลียร์ผลลัพธ์เก่า
        ClearResultItems();

        // แสดงผลลัพธ์ใหม่
        StartCoroutine(ShowResultsAnimated(rewards));

        Debug.Log($" Showing {rewards.Count} gacha results");
    }

    private void ClearResultItems()
    {
        if (resultItemContainer == null) return;

        foreach (Transform child in resultItemContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private IEnumerator ShowResultsAnimated(List<GachaReward> rewards)
    {
        foreach (GachaReward reward in rewards)
        {
            CreateResultItem(reward);
            yield return new WaitForSeconds(0.3f); // แสดงทีละ item
        }
    }

    private void CreateResultItem(GachaReward reward)
    {
        if (resultItemPrefab == null || resultItemContainer == null) return;

        GameObject itemObj = Instantiate(resultItemPrefab, resultItemContainer);

        // Setup item display
        Image itemIcon = itemObj.transform.Find("Icon")?.GetComponent<Image>();
        if (itemIcon != null && reward.itemData.ItemIcon != null)
        {
            itemIcon.sprite = reward.itemData.ItemIcon;
        }

        TextMeshProUGUI itemName = itemObj.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
        if (itemName != null)
        {
            itemName.text = reward.itemData.ItemName;
            itemName.color = reward.itemData.GetTierColor();
        }

        TextMeshProUGUI itemQuantity = itemObj.transform.Find("Quantity")?.GetComponent<TextMeshProUGUI>();
        if (itemQuantity != null)
        {
            itemQuantity.text = $"x{reward.quantity}";
        }

        // แสดง special effects สำหรับ rare items
        if (reward.itemData.Tier >= ItemTier.Rare || reward.isGuaranteed)
        {
            // TODO: เพิ่ม glow effect หรือ animation
        }
    }

    public void ShowRareItemEffect(GachaReward reward)
    {
        // เล่นเสียง rare item
        if (uiAudioSource != null && rareItemSound != null)
        {
            uiAudioSource.PlayOneShot(rareItemSound);
        }

        // TODO: แสดง special effect สำหรับ rare item
        Debug.Log($"⭐ RARE ITEM EFFECT: {reward.GetRewardText()}");
    }

    private void CloseResults()
    {
        if (gachaResultPanel != null)
            gachaResultPanel.SetActive(false);

        if (gachaMainPanel != null)
            gachaMainPanel.SetActive(true);

        PlayButtonSound();
    }
    #endregion

    #region Error Handling
    public void ShowErrorMessage(string message)
    {
        if (errorPanel == null || errorMessageText == null) return;

        errorMessageText.text = message;
        errorPanel.SetActive(true);

        Debug.LogWarning($"⚠️ UI Error: {message}");
    }

    private void CloseError()
    {
        if (errorPanel != null)
            errorPanel.SetActive(false);

        PlayButtonSound();
    }
    #endregion

    #region Audio
    private void PlayButtonSound()
    {
        if (uiAudioSource != null && buttonClickSound != null)
        {
            uiAudioSource.PlayOneShot(buttonClickSound);
        }
    }
    #endregion

    #region Public Interface
    public void OpenGachaUI()
    {
        ShowMachineSelection();
    }

    public void CloseGachaUI()
    {
        if (gachaMainPanel != null) gachaMainPanel.SetActive(false);
        if (gachaResultPanel != null) gachaResultPanel.SetActive(false);
        if (machineSelectionPanel != null) machineSelectionPanel.SetActive(false);
        if (errorPanel != null) errorPanel.SetActive(false);
    }

    public void SetCurrentMachine(string machineId)
    {
        if (GachaSystem.Instance != null)
        {
            GachaMachine machine = GachaSystem.Instance.GetMachine(machineId);
            if (machine != null)
            {
                SelectMachine(machine);
            }
        }
    }
    #endregion
}