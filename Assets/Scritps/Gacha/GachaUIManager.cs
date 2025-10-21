using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;

/// <summary>
/// จัดการ UI ของระบบกาชา - Single Player Version
/// </summary>
public class GachaUIManager : MonoBehaviour
{
    #region UI Panels
    [Header("🎰 Main UI Panels")]
    public GameObject gachaMainPanel;
    public GameObject gachaResultPanel;
    public GameObject machineSelectionPanel;
    public GameObject gachaHistoryPanel;

    [Header("🎛️ Machine Selection")]
    public Transform machineButtonContainer;
    public Button machineButtonPrefab;
    public Button backToLobbyButton;

    [Header("🎯 Current Machine UI")]
    public TextMeshProUGUI machineNameText;
    public TextMeshProUGUI machineDescriptionText;
    public Image machineIconImage;
    public TextMeshProUGUI costSingleText;
    public TextMeshProUGUI costTenText;
    public Button rollSingleButton;
    public Button rollTenButton;
    public Button viewHistoryButton;

    [Header("💰 Currency Display")]
    public TextMeshProUGUI currentGoldText;
    public TextMeshProUGUI currentGemsText;
    public GameObject insufficientCurrencyWarning;

    [Header("🎁 Results Display")]
    public Transform resultItemContainer;
    [SerializeField] private GameObject resultItemPrefab; // ทำเป็น private
    public Button closeResultsButton;
    public Button addToInventoryButton;

    [Header("📊 History Panel")]
    public Transform historyItemContainer;
    public GameObject historyItemPrefab;
    public Button closeHistoryButton;
    public ScrollRect historyScrollRect;

    [Header("🎵 Effects & Audio")]
    public ParticleSystem gachaOpenEffect;
    public AudioSource uiAudioSource;
    public AudioClip buttonClickSound;
    public AudioClip gachaOpenSound;
    public AudioClip rareItemSound;
    public AudioClip insufficientFundsSound;

    [Header("⚠️ Error Display")]
    public GameObject errorPanel;
    public TextMeshProUGUI errorMessageText;
    public Button closeErrorButton;

    [Header("🔗 System References")]
    public LobbyManager lobbyManager;
    #endregion

    #region Private Variables
    private GachaMachine currentMachine;
    private List<Button> machineButtons = new List<Button>();
    private CurrencyManager currencyManager;
    private List<GachaReward> pendingRewards = new List<GachaReward>();
    private List<GachaHistoryEntry> gachaHistory = new List<GachaHistoryEntry>();
    private Character playerCharacter;
    private Inventory playerInventory;

    // Single player state
    public bool IsGachaInProgress { get; private set; } = false;
    #endregion

    #region Initialization
    void Start()
    {
        InitializeUI();
        SetupButtonEvents();
        SetupSystemReferences();
        RefreshMachineSelection();

        // Subscribe to currency events
        CurrencyManager.OnGoldChanged += OnCurrencyChanged;
        CurrencyManager.OnGemsChanged += OnCurrencyChanged;

        // Subscribe to gacha events
        GachaSystem.OnGachaCompleted += OnGachaCompleted;
        GachaSystem.OnRewardsAddedToInventory += OnRewardsAddedToInventory;
        GachaSystem.OnInventoryFull += OnInventoryFull;

        // Single player setup
        SetupForLocalPlayer();
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        CurrencyManager.OnGoldChanged -= OnCurrencyChanged;
        CurrencyManager.OnGemsChanged -= OnCurrencyChanged;
        GachaSystem.OnGachaCompleted -= OnGachaCompleted;
        GachaSystem.OnRewardsAddedToInventory -= OnRewardsAddedToInventory;
        GachaSystem.OnInventoryFull -= OnInventoryFull;
    }

    private void InitializeUI()
    {
        HideAllPanels();
        Debug.Log("🎨 [GachaUIManager] UI initialized");
    }

    private void SetupButtonEvents()
    {
        // Main gacha buttons
        if (rollSingleButton != null)
            rollSingleButton.onClick.AddListener(() => RequestGachaRoll(1));

        if (rollTenButton != null)
            rollTenButton.onClick.AddListener(() => RequestGachaRoll(10));

        // Panel navigation buttons
        if (closeResultsButton != null)
            closeResultsButton.onClick.AddListener(CloseResults);

        if (closeErrorButton != null)
            closeErrorButton.onClick.AddListener(CloseError);

        if (backToLobbyButton != null)
            backToLobbyButton.onClick.AddListener(BackToLobby);

        if (viewHistoryButton != null)
            viewHistoryButton.onClick.AddListener(ShowHistory);

        if (closeHistoryButton != null)
            closeHistoryButton.onClick.AddListener(CloseHistory);

        // Inventory integration
        if (addToInventoryButton != null)
            addToInventoryButton.onClick.AddListener(AddPendingRewardsToInventory);
    }

    private void SetupSystemReferences()
    {
        // หา CurrencyManager
        currencyManager = CurrencyManager.FindCurrencyManager();
        if (currencyManager == null)
        {
            Debug.LogWarning("🎰 [GachaUIManager] CurrencyManager not found!");
        }

        // หา player character และ inventory
        playerCharacter = FindPlayerCharacter();
        if (playerCharacter != null)
        {
            playerInventory = playerCharacter.GetComponent<Inventory>();
            if (playerInventory == null)
            {
                Debug.LogWarning("🎰 [GachaUIManager] Player inventory not found!");
            }
        }
        else
        {
            Debug.LogWarning("🎰 [GachaUIManager] Player character not found!");
        }

        // หา LobbyManager ถ้ายังไม่ได้ set
        if (lobbyManager == null)
        {
            lobbyManager = FindObjectOfType<LobbyManager>();
        }
    }

    private void SetupForLocalPlayer()
    {
        // Load gacha history
        LoadGachaHistoryFromPlayerPrefs();

        // Update currency display
        UpdateCurrencyDisplay();
    }
    #endregion

    #region Panel Management
    private void HideAllPanels()
    {
        if (gachaMainPanel != null) gachaMainPanel.SetActive(false);
        if (gachaResultPanel != null) gachaResultPanel.SetActive(false);
        if (machineSelectionPanel != null) machineSelectionPanel.SetActive(false);
        if (gachaHistoryPanel != null) gachaHistoryPanel.SetActive(false);
        if (errorPanel != null) errorPanel.SetActive(false);
        if (insufficientCurrencyWarning != null) insufficientCurrencyWarning.SetActive(false);
    }

    public void ShowMachineSelection()
    {
        HideAllPanels();
        if (machineSelectionPanel != null)
        {
            machineSelectionPanel.SetActive(true);
            RefreshMachineSelection();
            UpdateCurrencyDisplay();
        }
        PlayButtonSound();
    }

    public void ShowGachaPanel()
    {
        HideAllPanels();
        if (gachaMainPanel != null)
        {
            gachaMainPanel.SetActive(true);
            UpdateCurrencyDisplay();
            UpdateMachineUI();
        }
        PlayButtonSound();
    }

    private void ShowHistory()
    {
        HideAllPanels();
        if (gachaHistoryPanel != null)
        {
            gachaHistoryPanel.SetActive(true);
            UpdateHistoryDisplay();
        }
        PlayButtonSound();
    }

    private void CloseHistory()
    {
        ShowGachaPanel();
        PlayButtonSound();
    }

    private void BackToLobby()
    {
        HideAllPanels();

        // ใช้ LobbyManager เพื่อกลับไป lobby
        if (lobbyManager != null)
        {
            lobbyManager.gachaPanel.SetActive(false);
        }

        PlayButtonSound();
    }
    #endregion

    #region Currency Integration
    private void UpdateCurrencyDisplay()
    {
        if (currencyManager == null) return;

        // แสดงจำนวนเงินปัจจุบัน
        if (currentGoldText != null)
        {
            currentGoldText.text = $"Gold: {currencyManager.GetCurrentGold():N0}";
        }

        if (currentGemsText != null)
        {
            currentGemsText.text = $"Gems: {currencyManager.GetCurrentGems():N0}";
        }

        // อัปเดตสถานะปุ่ม roll
        UpdateRollButtonStates();
    }

    private void UpdateRollButtonStates()
    {
        if (currentMachine == null || currentMachine.Pool == null) return;

        // ตรวจสอบ currency สำหรับ single roll
        bool canAffordSingle = CanAffordRoll(1);
        if (rollSingleButton != null)
        {
            rollSingleButton.interactable = canAffordSingle && !IsGachaInProgress;
        }

        // ตรวจสอบ currency สำหรับ ten rolls
        bool canAffordTen = CanAffordRoll(10);
        if (rollTenButton != null)
        {
            rollTenButton.interactable = canAffordTen && !IsGachaInProgress;
        }
    }

    private bool CanAffordRoll(int rollCount)
    {
        if (currentMachine == null || currentMachine.Pool == null || currencyManager == null)
            return false;

        long cost = rollCount == 1 ?
            currentMachine.Pool.costPerRoll :
            currentMachine.Pool.costPerTenRolls;

        // ตรวจสอบตาม currency type
        if (currentMachine.Pool.costCurrency == "Gems")
        {
            return currencyManager.HasEnoughGems((int)cost);
        }
        else if (currentMachine.Pool.costCurrency == "Gold")
        {
            return currencyManager.HasEnoughGold(cost);
        }

        return false;
    }

    private bool SpendCurrency(int rollCount)
    {
        if (currentMachine == null || currentMachine.Pool == null || currencyManager == null)
            return false;

        long cost = rollCount == 1 ?
            currentMachine.Pool.costPerRoll :
            currentMachine.Pool.costPerTenRolls;

        // ใช้จ่ายตาม currency type
        if (currentMachine.Pool.costCurrency == "Gems")
        {
            return currencyManager.SpendGems((int)cost);
        }
        else if (currentMachine.Pool.costCurrency == "Gold")
        {
            return currencyManager.SpendGold(cost);
        }

        return false;
    }

    private void OnCurrencyChanged(long oldAmount, long newAmount)
    {
        UpdateCurrencyDisplay();
    }

    private void OnCurrencyChanged(int oldAmount, int newAmount)
    {
        UpdateCurrencyDisplay();
    }
    #endregion

    #region Machine Management
    public void RefreshMachineSelection()
    {
        ClearMachineButtons();

        if (GachaSystem.Instance == null) return;

        foreach (GachaMachine machine in GachaSystem.Instance.AllMachines)
        {
            CreateMachineButton(machine);
        }

        Debug.Log($"🔄 [GachaUIManager] Refreshed machine selection: {GachaSystem.Instance.MachineCount} machines");
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

        // Setup button display
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

        // Setup cost display on button
        TextMeshProUGUI costText = newButton.transform.Find("CostText")?.GetComponent<TextMeshProUGUI>();
        if (costText != null && machine.Pool != null)
        {
            costText.text = $"{machine.Pool.costPerRoll} {machine.Pool.costCurrency}";
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

        Debug.Log($"🎯 [GachaUIManager] Selected machine: {machine.machineName}");
    }

    private void UpdateMachineUI()
    {
        if (currentMachine == null) return;

        // Update machine info
        if (machineNameText != null)
            machineNameText.text = currentMachine.machineName;

        string description = "";
        Sprite icon = null;
        int costSingle = 0;
        int costTen = 0;
        string currency = "";

        // Get data from appropriate pool
        if (currentMachine.isCharacterMachine && currentMachine.CharacterPool != null)
        {
            description = currentMachine.CharacterPool.description;
            icon = currentMachine.CharacterPool.poolIcon;
            costSingle = currentMachine.CharacterPool.costPerRoll;
            costTen = currentMachine.CharacterPool.costPerTenRolls;
            currency = currentMachine.CharacterPool.costCurrency;
        }
        else if (currentMachine.Pool != null)
        {
            description = currentMachine.Pool.description;
            icon = currentMachine.Pool.poolIcon;
            costSingle = currentMachine.Pool.costPerRoll;
            costTen = currentMachine.Pool.costPerTenRolls;
            currency = currentMachine.Pool.costCurrency;
        }

        if (machineDescriptionText != null)
            machineDescriptionText.text = description;

        if (machineIconImage != null && icon != null)
            machineIconImage.sprite = icon;

        // Update cost display
        if (costSingleText != null)
            costSingleText.text = $"{costSingle} {currency}";

        if (costTenText != null)
            costTenText.text = $"{costTen} {currency}";

        // Update button states
        UpdateRollButtonStates();
    }
    #endregion

    #region Gacha Operations - Single Player Version
    private void RequestGachaRoll(int rollCount)
    {
        if (currentMachine == null)
        {
            ShowErrorMessage("No machine selected!");
            return;
        }

        if (IsGachaInProgress)
        {
            ShowErrorMessage("Gacha roll already in progress!");
            return;
        }

        if (!CanAffordRoll(rollCount))
        {
            ShowInsufficientCurrencyWarning();
            return;
        }

        // เริ่ม gacha roll ทันที (single player)
        StartGachaRoll(currentMachine.machineId, rollCount);
        PlayButtonSound();
    }

    private void StartGachaRoll(string machineId, int rollCount)
    {
        IsGachaInProgress = true;
        StartCoroutine(PerformGachaRoll(machineId, rollCount));
    }

    private IEnumerator PerformGachaRoll(string machineId, int rollCount)
    {
        GachaMachine machine = GachaSystem.Instance.GetMachine(machineId);
        if (machine == null)
        {
            IsGachaInProgress = false;
            ShowErrorMessage("Machine not found!");
            yield break;
        }

        // ใช้จ่าย currency
        bool paymentSuccess = SpendCurrency(rollCount);
        if (!paymentSuccess)
        {
            IsGachaInProgress = false;
            ShowErrorMessage("Payment failed!");
            yield break;
        }

        Debug.Log($"🎲 [GachaUIManager] Rolling {rollCount} times on {machine.machineName}");

        // เล่น effects
        PlayGachaEffects();

        // รอให้ effect เล่นจบ
        yield return new WaitForSeconds(1.5f);

        // ทำการสุ่ม
        List<GachaReward> rewards = machine.Roll(rollCount);

        // เก็บ rewards ไว้เพื่อเพิ่มเข้า inventory ภายหลัง
        pendingRewards = rewards;

        // บันทึกลง history
        SaveGachaToHistory(machine, rewards);

        // แสดงผลลัพธ์
        ShowGachaResults(machine, rewards);

        IsGachaInProgress = false;
    }

    private void PlayGachaEffects()
    {
        // เล่น particle effect
        if (gachaOpenEffect != null)
        {
            gachaOpenEffect.Play();
        }

        // เล่นเสียง
        if (uiAudioSource != null && gachaOpenSound != null)
        {
            uiAudioSource.PlayOneShot(gachaOpenSound);
        }

        // ปิดปุ่มชั่วคราว
        UpdateRollButtonStates();
    }

    private void ShowInsufficientCurrencyWarning()
    {
        if (insufficientCurrencyWarning != null)
        {
            insufficientCurrencyWarning.SetActive(true);

            // ซ่อนหลัง 3 วินาที
            StartCoroutine(HideWarningAfterDelay(3f));
        }

        if (uiAudioSource != null && insufficientFundsSound != null)
        {
            uiAudioSource.PlayOneShot(insufficientFundsSound);
        }

        Debug.LogWarning("💸 [GachaUIManager] Insufficient currency for gacha roll");
    }

    private IEnumerator HideWarningAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (insufficientCurrencyWarning != null)
        {
            insufficientCurrencyWarning.SetActive(false);
        }
    }
    #endregion

    #region Results Display
    public void ShowGachaResults(GachaMachine machine, List<GachaReward> rewards)
    {
        if (gachaResultPanel == null) return;

        HideAllPanels();
        gachaResultPanel.SetActive(true);

        // เคลียร์ผลลัพธ์เก่า
        ClearResultItems();

        // แสดงผลลัพธ์ใหม่
        StartCoroutine(ShowResultsAnimated(rewards));

        // เปิดปุ่มเพิ่มเข้า inventory
        if (addToInventoryButton != null)
        {
            addToInventoryButton.gameObject.SetActive(true);
            addToInventoryButton.interactable = true;
        }

        Debug.Log($"🎁 [GachaUIManager] Showing {rewards.Count} gacha results");
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
        bool hasRareItem = false;

        foreach (GachaReward reward in rewards)
        {
            CreateResultItem(reward);

            // เช็คว่ามี rare item หรือไม่
            if (reward.IsRareReward() || reward.isGuaranteed)
            {
                hasRareItem = true;
            }

            // ✅ หน่วงเวลานานขึ้นสำหรับ rare items
            float delay = (reward.IsRareReward() || reward.isGuaranteed) ? 0.5f : 0.2f;
            yield return new WaitForSeconds(delay);
        }

        // เล่นเสียง rare item ถ้ามี
        if (hasRareItem && uiAudioSource != null && rareItemSound != null)
        {
            uiAudioSource.PlayOneShot(rareItemSound);
        }
    }
    private void CreateResultItem(GachaReward reward)
    {
        if (resultItemPrefab == null || resultItemContainer == null) return;

        GameObject itemObj = Instantiate(resultItemPrefab, resultItemContainer);

        // ✅ ใช้ ItemRewardUI script แทนการ setup แบบเดิม
        ItemRewardUI rewardUI = itemObj.GetComponent<ItemRewardUI>();
        if (rewardUI != null)
        {
            // สร้าง ItemRewardInfo จาก GachaReward
            ItemRewardInfo itemRewardInfo = ConvertGachaRewardToItemRewardInfo(reward);

            // ตั้งค่าข้อมูล
            rewardUI.SetItemData(itemRewardInfo);

            // เล่น animation (หน่วงเวลาถ้าเป็น rare item)
            if (reward.IsRareReward() || reward.isGuaranteed)
            {
                StartCoroutine(PlayRewardAnimationDelayed(rewardUI, 0.2f));
            }
            else
            {
                rewardUI.PlayAppearAnimation();
            }
        }
        else
        {
            // Fallback: ใช้วิธีเดิมถ้าไม่มี ItemRewardUI component
            SetupResultItemFallback(itemObj, reward);
        }
    }
    /// <summary>
    /// แปลง GachaReward เป็น ItemRewardInfo สำหรับใช้กับ ItemRewardUI
    /// </summary>
    private ItemRewardInfo ConvertGachaRewardToItemRewardInfo(GachaReward reward)
    {
        var itemRewardInfo = new ItemRewardInfo();

        switch (reward.rewardType)
        {
            case RewardType.Item:
                if (reward.itemData != null)
                {
                    itemRewardInfo.itemName = reward.itemData.ItemName;
                    itemRewardInfo.itemIcon = reward.itemData.ItemIcon;
                    itemRewardInfo.itemTier = reward.itemData.Tier;
                    itemRewardInfo.quantity = reward.quantity;
                }
                break;

            case RewardType.Character:
                if (reward.characterData != null)
                {
                    itemRewardInfo.itemName = reward.characterData.characterName;
                    itemRewardInfo.itemIcon = reward.characterData.characterIcon;
                    // แปลง CharacterRarity เป็น ItemTier
                    itemRewardInfo.itemTier = ConvertCharacterRarityToItemTier(reward.characterData.rarity);
                    itemRewardInfo.quantity = 1;
                }
                break;

            case RewardType.Currency:
                itemRewardInfo.itemName = reward.currencyType;
                // TODO: ใส่ icon สำหรับ currency (Gold/Gems icon)
                itemRewardInfo.itemTier = ItemTier.Common; // Currency ใช้ tier common
                itemRewardInfo.quantity = reward.quantity;
                break;
        }

        return itemRewardInfo;
    }

    /// <summary>
    /// แปลง CharacterRarity เป็น ItemTier
    /// </summary>
    private ItemTier ConvertCharacterRarityToItemTier(CharacterRarity rarity)
    {
        switch (rarity)
        {
            case CharacterRarity.Common: return ItemTier.Common;
            case CharacterRarity.Uncommon: return ItemTier.Uncommon;
            case CharacterRarity.Rare: return ItemTier.Rare;
            case CharacterRarity.Epic: return ItemTier.Epic;
            case CharacterRarity.Legendary: return ItemTier.Legendary;
            default: return ItemTier.Common;
        }
    }
    /// <summary>
    /// เล่น animation หลังจากหน่วงเวลา (สำหรับ rare items)
    /// </summary>
    private IEnumerator PlayRewardAnimationDelayed(ItemRewardUI rewardUI, float delay)
    {
        yield return new WaitForSeconds(delay);
        rewardUI.PlayAppearAnimation();
    }
    /// <summary>
    /// Setup item แบบเดิม (fallback) ถ้าไม่มี ItemRewardUI component
    /// </summary>
    private void SetupResultItemFallback(GameObject itemObj, GachaReward reward)
    {
        // Setup item display (โค้ดเดิมของคุณ)
        Image itemIcon = itemObj.transform.Find("Icon")?.GetComponent<Image>();
        if (itemIcon != null && reward.GetRewardIcon() != null)
        {
            itemIcon.sprite = reward.GetRewardIcon();
        }

        TextMeshProUGUI itemName = itemObj.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
        if (itemName != null)
        {
            itemName.text = reward.GetDisplayName();
            itemName.color = reward.GetRewardColor();
        }

        TextMeshProUGUI itemQuantity = itemObj.transform.Find("Quantity")?.GetComponent<TextMeshProUGUI>();
        if (itemQuantity != null && reward.quantity > 1)
        {
            itemQuantity.text = $"x{reward.quantity}";
        }

        // แสดง tier/rarity
        TextMeshProUGUI tierText = itemObj.transform.Find("Tier")?.GetComponent<TextMeshProUGUI>();
        if (tierText != null)
        {
            tierText.text = reward.GetRewardTierText();
            tierText.color = reward.GetRewardColor();
        }

        // Special indicators
        GameObject newLabel = itemObj.transform.Find("NewLabel")?.gameObject;
        if (newLabel != null) newLabel.SetActive(reward.isNewItem);

        GameObject guaranteedLabel = itemObj.transform.Find("GuaranteedLabel")?.gameObject;
        if (guaranteedLabel != null) guaranteedLabel.SetActive(reward.isGuaranteed);
    }

    public void ShowRareItemEffect(GachaReward reward)
    {
        // แสดง special effect สำหรับ rare item
        Debug.Log($"⭐ [GachaUIManager] RARE ITEM EFFECT: {reward.GetRewardText()}");

        // เล่น particle effect พิเศษ
        // เล่นเสียงพิเศษ
        // แสดง popup พิเศษ
    }

    private void CloseResults()
    {
        ShowGachaPanel();
        PlayButtonSound();
    }
    #endregion

    #region Inventory Integration
    private void AddPendingRewardsToInventory()
    {
        if (pendingRewards.Count == 0)
        {
            ShowErrorMessage("No rewards to add!");
            return;
        }

        if (playerInventory == null)
        {
            ShowErrorMessage("Player inventory not found!");
            return;
        }

        StartCoroutine(AddRewardsToInventoryCoroutine());
    }

    private IEnumerator AddRewardsToInventoryCoroutine()
    {
        int successCount = 0;
        int failCount = 0;

        foreach (GachaReward reward in pendingRewards)
        {
            if (reward == null || !reward.IsValid()) continue;

            bool success = playerInventory.AddItem(reward.itemData, reward.quantity);

            if (success)
            {
                successCount += reward.quantity;
                Debug.Log($"✅ [GachaUIManager] Added to inventory: {reward.GetRewardText()}");
            }
            else
            {
                failCount += reward.quantity;
                Debug.LogWarning($"⚠️ [GachaUIManager] Failed to add: {reward.GetRewardText()}");
            }

            yield return new WaitForSeconds(0.1f); // หน่วงเวลาเล็กน้อย
        }

        // แสดงผลลัพธ์
        if (successCount > 0)
        {
            ShowSuccessMessage($"Added {successCount} items to inventory!");

            // ล้าง pending rewards
            pendingRewards.Clear();

            // ปิดปุ่มเพิ่มเข้า inventory
            if (addToInventoryButton != null)
            {
                addToInventoryButton.interactable = false;
            }
        }

        if (failCount > 0)
        {
            ShowErrorMessage($"{failCount} items could not be added (inventory full?)");
        }
    }

    private Character FindPlayerCharacter()
    {
        // หา player character
        Character[] characters = FindObjectsOfType<Character>();
        return characters.Length > 0 ? characters[0] : null;
    }

    private void OnGachaCompleted(GachaMachine machine, List<GachaReward> rewards)
    {
        // Event handler สำหรับเมื่อ gacha เสร็จสิ้น
        Debug.Log($"🎉 [GachaUIManager] Gacha completed: {rewards.Count} rewards from {machine.machineName}");
    }

    private void OnRewardsAddedToInventory(List<GachaReward> rewards)
    {
        // Event handler สำหรับเมื่อ rewards ถูกเพิ่มเข้า inventory สำเร็จ
        Debug.Log($"📦 [GachaUIManager] {rewards.Count} rewards added to inventory");
    }

    private void OnInventoryFull(GachaReward reward)
    {
        // Event handler สำหรับเมื่อ inventory เต็ม
        ShowErrorMessage($"Inventory full! Could not add {reward.GetRewardText()}");
    }
    #endregion

    #region History System - บันทึกประวัติ gacha ใน PlayerPrefs
    private void SaveGachaToHistory(GachaMachine machine, List<GachaReward> rewards)
    {
        var historyEntry = new GachaHistoryEntry
        {
            machineId = machine.machineId,
            machineName = machine.machineName,
            rollCount = rewards.Count,
            timestamp = System.DateTime.Now,
            rewards = rewards
        };

        gachaHistory.Add(historyEntry);

        // บันทึกลง PlayerPrefs
        SaveGachaHistoryToPlayerPrefs();

        Debug.Log($"📊 [GachaUIManager] Saved gacha history: {machine.machineName} x{rewards.Count}");
    }

    private void UpdateHistoryDisplay()
    {
        if (historyItemContainer == null) return;

        // เคลียร์ history items เก่า
        foreach (Transform child in historyItemContainer)
        {
            Destroy(child.gameObject);
        }

        // แสดง history ใหม่ (เรียงจากใหม่ไปเก่า)
        var sortedHistory = new List<GachaHistoryEntry>(gachaHistory);
        sortedHistory.Reverse();

        foreach (var entry in sortedHistory)
        {
            CreateHistoryItem(entry);
        }

        // Scroll ไปด้านบน
        if (historyScrollRect != null)
        {
            historyScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void CreateHistoryItem(GachaHistoryEntry entry)
    {
        if (historyItemPrefab == null || historyItemContainer == null) return;

        GameObject historyObj = Instantiate(historyItemPrefab, historyItemContainer);

        // Setup history display
        TextMeshProUGUI timestampText = historyObj.transform.Find("Timestamp")?.GetComponent<TextMeshProUGUI>();
        if (timestampText != null)
        {
            timestampText.text = entry.timestamp.ToString("MM/dd HH:mm");
        }

        TextMeshProUGUI machineText = historyObj.transform.Find("Machine")?.GetComponent<TextMeshProUGUI>();
        if (machineText != null)
        {
            machineText.text = entry.machineName;
        }

        TextMeshProUGUI rollCountText = historyObj.transform.Find("RollCount")?.GetComponent<TextMeshProUGUI>();
        if (rollCountText != null)
        {
            rollCountText.text = $"{entry.rollCount} rolls";
        }

        // แสดง rewards สำคัญ
        TextMeshProUGUI rewardsText = historyObj.transform.Find("Rewards")?.GetComponent<TextMeshProUGUI>();
        if (rewardsText != null && entry.rewards.Count > 0)
        {
            string rewardSummary = "";
            int rareCount = 0;

            foreach (var reward in entry.rewards)
            {
                if (reward.itemData.Tier >= ItemTier.Rare)
                {
                    rareCount++;
                    if (rewardSummary.Length > 0) rewardSummary += ", ";
                    rewardSummary += reward.itemData.ItemName;
                }
            }

            if (rareCount > 0)
            {
                rewardsText.text = $"⭐ {rewardSummary}";
                rewardsText.color = Color.yellow;
            }
            else
            {
                rewardsText.text = $"{entry.rewards.Count} items";
                rewardsText.color = Color.white;
            }
        }
    }

    private void LoadGachaHistoryFromPlayerPrefs()
    {
        // Load จาก PlayerPrefs (แทนที่ Firebase)
        string historyJson = PlayerPrefs.GetString("GachaHistory", "");

        if (!string.IsNullOrEmpty(historyJson))
        {
            try
            {
                // TODO: Implement JSON deserialization for history
                Debug.Log("🔄 [GachaUIManager] Loading gacha history from PlayerPrefs...");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ [GachaUIManager] Failed to load history: {e.Message}");
            }
        }
    }

    private void SaveGachaHistoryToPlayerPrefs()
    {
        // Save ไป PlayerPrefs (แทนที่ Firebase)
        try
        {
            // TODO: Implement JSON serialization for history
            // PlayerPrefs.SetString("GachaHistory", jsonString);
            Debug.Log("💾 [GachaUIManager] Saving gacha history to PlayerPrefs...");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ [GachaUIManager] Failed to save history: {e.Message}");
        }
    }
    #endregion

    #region Error Handling
    public void ShowErrorMessage(string message)
    {
        if (errorPanel == null || errorMessageText == null) return;

        errorMessageText.text = message;

        // แสดง error panel ไว้ด้านหน้า
        errorPanel.SetActive(true);
        errorPanel.transform.SetAsLastSibling();

        Debug.LogWarning($"⚠️ [GachaUIManager] Error: {message}");
    }

    private void ShowSuccessMessage(string message)
    {
        // แสดง success message
        Debug.Log($"✅ [GachaUIManager] Success: {message}");
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
        PlayButtonSound();

        Debug.Log("🎰 [GachaUIManager] Opened gacha UI");
    }

    public void CloseGachaUI()
    {
        HideAllPanels();
        PlayButtonSound();

        Debug.Log("🎰 [GachaUIManager] Closed gacha UI");
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

    public bool IsGachaUIOpen()
    {
        return (gachaMainPanel != null && gachaMainPanel.activeSelf) ||
               (machineSelectionPanel != null && machineSelectionPanel.activeSelf) ||
               (gachaResultPanel != null && gachaResultPanel.activeSelf);
    }
    #endregion


}

#region Data Classes
[System.Serializable]
public class GachaHistoryEntry
{
    public string machineId;
    public string machineName;
    public int rollCount;
    public System.DateTime timestamp;
    public List<GachaReward> rewards;

    public GachaHistoryEntry()
    {
        rewards = new List<GachaReward>();
        timestamp = System.DateTime.Now;
    }
}
#endregion