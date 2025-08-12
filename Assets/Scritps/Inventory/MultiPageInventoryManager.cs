using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class InventoryPageConfig
{
    [Header("Page Configuration")]
    public string pageName = "All Items";
    public ItemType filterType = ItemType.Weapon;
    public bool showAllItems = true;  // ถ้า true จะแสดงทุก ItemType
    public Sprite pageIcon;
    public Color pageColor = Color.white;

    [Header("Grid Settings")]
    public int slotsPerPage = 30;
    public int gridWidth = 6;
    public int gridHeight = 5;

    public InventoryPageConfig()
    {
        pageName = "All Items";
        showAllItems = true;
        slotsPerPage = 30;
        gridWidth = 6;
        gridHeight = 5;
    }

    public InventoryPageConfig(string name, ItemType type, int slots = 30)
    {
        pageName = name;
        filterType = type;
        showAllItems = false;
        slotsPerPage = slots;
        gridWidth = 6;
        gridHeight = 5;
    }
}

public class MultiPageInventoryManager : MonoBehaviour
{
    [Header("🔧 Multi-Page Configuration")]
    [SerializeField] private List<InventoryPageConfig> inventoryPages = new List<InventoryPageConfig>();
    [SerializeField] private int currentPageIndex = 0;

    [Header("🎯 UI References")]
    [SerializeField] private Transform pageButtonContainer;
    [SerializeField] private GameObject pageButtonPrefab;
    [SerializeField] private Transform gridContainer;
    [SerializeField] private TextMeshProUGUI pageTitle;
    [SerializeField] private TextMeshProUGUI slotCountText;

    [Header("🔗 Dependencies")]
    [SerializeField] private Character ownerCharacter;
    [SerializeField] private bool autoDetectCharacter = true;

    // Private Variables
    private List<Button> pageButtons = new List<Button>();
    private List<InventoryGridManager> gridManagers = new List<InventoryGridManager>();
    private InventoryGridManager currentGridManager;

    // Events
    public System.Action<int> OnPageChanged;
    public System.Action<int, int> OnSlotSelectionChanged; // pageIndex, slotIndex

    // Properties
    public int CurrentPageIndex { get { return currentPageIndex; } }
    public InventoryPageConfig CurrentPage { get { return GetCurrentPageConfig(); } }
    public InventoryGridManager CurrentGridManager { get { return currentGridManager; } }
    public Character OwnerCharacter { get { return ownerCharacter; } }

    #region Unity Lifecycle
    private void Awake()
    {
        SetupDefaultPages();
    }

    private void Start()
    {
        SetupCharacterConnection();
        StartCoroutine(DelayedSetup());
    }

    private IEnumerator DelayedSetup()
    {
        yield return null; // รอ 1 frame

        CreatePageButtons();
        CreateInventoryGrids();
        SwitchToPage(0);

        Debug.Log($"[MultiPageInventory] Setup completed with {inventoryPages.Count} pages");
    }
    #endregion

    #region Setup Methods
    private void SetupDefaultPages()
    {
        if (inventoryPages.Count == 0)
        {
            Debug.Log("[MultiPageInventory] Creating default page configuration...");

            // หน้าแรก: All Items
            inventoryPages.Add(new InventoryPageConfig()
            {
                pageName = "All Items",
                showAllItems = true,
                slotsPerPage = 30,
                gridWidth = 6,
                gridHeight = 5,
                pageColor = Color.white
            });

            // หน้าอื่นๆ แบ่งตาม ItemType
            inventoryPages.Add(new InventoryPageConfig("Weapons", ItemType.Weapon, 30));
            inventoryPages.Add(new InventoryPageConfig("Armor", ItemType.Armor, 30));
            inventoryPages.Add(new InventoryPageConfig("Head", ItemType.Head, 30));
            inventoryPages.Add(new InventoryPageConfig("Pants", ItemType.Pants, 30));
            inventoryPages.Add(new InventoryPageConfig("Shoes", ItemType.Shoes, 30));
            inventoryPages.Add(new InventoryPageConfig("Runes", ItemType.Rune, 30));
            inventoryPages.Add(new InventoryPageConfig("Potions", ItemType.Potion, 30));
            inventoryPages.Add(new InventoryPageConfig("Materials", ItemType.Material, 30));

            Debug.Log($"[MultiPageInventory] Created {inventoryPages.Count} default pages");
        }
    }

    private void SetupCharacterConnection()
    {
        if (ownerCharacter == null && autoDetectCharacter)
        {
            ownerCharacter = FindCharacterMultipleWays();
            Debug.Log($"[MultiPageInventory] Auto-detected character: {ownerCharacter?.CharacterName ?? "None"}");
        }

        if (ownerCharacter == null)
        {
            StartCoroutine(RetryFindCharacter());
        }
    }

    private Character FindCharacterMultipleWays()
    {
        // วิธีที่ 1: หาจาก parent
        Character foundCharacter = GetComponentInParent<Character>();
        if (foundCharacter != null) return foundCharacter;

        // วิธีที่ 2: หาจาก CombatUIManager
        CombatUIManager uiManager = FindObjectOfType<CombatUIManager>();
        if (uiManager != null && uiManager.localHero != null)
        {
            return uiManager.localHero;
        }

        // วิธีที่ 3: หาจาก Hero ที่มี InputAuthority
        Hero[] heroes = FindObjectsOfType<Hero>();
        foreach (Hero hero in heroes)
        {
            if (hero.HasInputAuthority && hero.IsSpawned)
            {
                return hero;
            }
        }

        return null;
    }

    private IEnumerator RetryFindCharacter()
    {
        float timeout = 10f;
        float elapsed = 0f;

        while (ownerCharacter == null && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;

            ownerCharacter = FindCharacterMultipleWays();

            if (ownerCharacter != null)
            {
                Debug.Log($"[MultiPageInventory] Found character on retry: {ownerCharacter.CharacterName}");
                RefreshAllGrids();
                break;
            }
        }

        if (ownerCharacter == null)
        {
            Debug.LogError("[MultiPageInventory] Failed to find character after retry!");
        }
    }
    #endregion

    #region Page Button Creation
    private void CreatePageButtons()
    {
        if (pageButtonContainer == null)
        {
            Debug.LogError("[MultiPageInventory] Page button container not assigned!");
            return;
        }

        // ลบปุ่มเก่า
        ClearPageButtons();

        Debug.Log($"[MultiPageInventory] Creating {inventoryPages.Count} page buttons...");

        for (int i = 0; i < inventoryPages.Count; i++)
        {
            CreateSinglePageButton(i);
        }

        Debug.Log($"[MultiPageInventory] Created {pageButtons.Count} page buttons");
    }

    private void CreateSinglePageButton(int pageIndex)
    {
        InventoryPageConfig pageConfig = inventoryPages[pageIndex];

        GameObject buttonObj;

        if (pageButtonPrefab != null)
        {
            buttonObj = Instantiate(pageButtonPrefab, pageButtonContainer);
        }
        else
        {
            buttonObj = CreatePageButtonFromScratch();
        }

        buttonObj.name = $"PageButton_{pageConfig.pageName}";

        // Setup Button component
        Button button = buttonObj.GetComponent<Button>();
        if (button == null)
        {
            button = buttonObj.AddComponent<Button>();
        }

        // Setup button event
        int capturedIndex = pageIndex; // สำคัญ: capture ค่า index
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SwitchToPage(capturedIndex));

        // Setup button visuals
        SetupPageButtonVisuals(button, pageConfig, pageIndex);

        pageButtons.Add(button);

        Debug.Log($"[MultiPageInventory] Created button for page: {pageConfig.pageName}");
    }

    private GameObject CreatePageButtonFromScratch()
    {
        GameObject buttonObj = new GameObject("PageButton");
        buttonObj.transform.SetParent(pageButtonContainer, false);

        // Add RectTransform
        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(80, 80);

        // Add Image
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = Color.white;

        // Add Button
        Button button = buttonObj.AddComponent<Button>();

        // Create Text child
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Page";
        text.fontSize = 12;
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.Center;

        return buttonObj;
    }

    private void SetupPageButtonVisuals(Button button, InventoryPageConfig pageConfig, int pageIndex)
    {
        // Set button text
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = pageConfig.pageName;
        }

        // Set button icon if available
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null && pageConfig.pageIcon != null)
        {
            buttonImage.sprite = pageConfig.pageIcon;
        }

        // Set button color
        if (buttonImage != null)
        {
            buttonImage.color = pageConfig.pageColor;
        }

        // Set initial selection state
        UpdatePageButtonSelection(pageIndex, pageIndex == currentPageIndex);
    }

    private void ClearPageButtons()
    {
        foreach (Button button in pageButtons)
        {
            if (button != null)
            {
                DestroyImmediate(button.gameObject);
            }
        }
        pageButtons.Clear();
    }
    #endregion

    #region Grid Creation
    private void CreateInventoryGrids()
    {
        if (gridContainer == null)
        {
            Debug.LogError("[MultiPageInventory] Grid container not assigned!");
            return;
        }

        // ลบ grids เก่า
        ClearInventoryGrids();

        Debug.Log($"[MultiPageInventory] Creating {inventoryPages.Count} inventory grids...");

        for (int i = 0; i < inventoryPages.Count; i++)
        {
            CreateSingleInventoryGrid(i);
        }

        Debug.Log($"[MultiPageInventory] Created {gridManagers.Count} inventory grids");
    }

    private void CreateSingleInventoryGrid(int pageIndex)
    {
        InventoryPageConfig pageConfig = inventoryPages[pageIndex];

        // สร้าง Grid GameObject
        GameObject gridObj = new GameObject($"InventoryGrid_{pageConfig.pageName}");
        gridObj.transform.SetParent(gridContainer, false);

        // Setup RectTransform
        RectTransform gridRect = gridObj.AddComponent<RectTransform>();
        gridRect.anchorMin = Vector2.zero;
        gridRect.anchorMax = Vector2.one;
        gridRect.offsetMin = Vector2.zero;
        gridRect.offsetMax = Vector2.zero;

        // Add InventoryGridManager component
        InventoryGridManager gridManager = gridObj.AddComponent<InventoryGridManager>();

        // Configure grid manager (ใช้ reflection หรือ public methods)
        ConfigureGridManager(gridManager, pageConfig);

        // Subscribe to grid events
        gridManager.OnSlotSelectionChanged += (slotIndex) => HandleSlotSelectionChanged(pageIndex, slotIndex);

        gridManagers.Add(gridManager);

        // ซ่อน grid ไว้ก่อน (จะแสดงเฉพาะ current page)
        gridObj.SetActive(pageIndex == currentPageIndex);

        Debug.Log($"[MultiPageInventory] Created grid for page: {pageConfig.pageName} ({pageConfig.slotsPerPage} slots)");
    }

    private void ConfigureGridManager(InventoryGridManager gridManager, InventoryPageConfig pageConfig)
    {
        // ต้องใช้ public methods หรือ properties ของ InventoryGridManager
        // เนื่องจากตัวแปรส่วนใหญ่เป็น private

        // Set character connection
        if (ownerCharacter != null)
        {
            gridManager.SetOwnerCharacter(ownerCharacter);
        }

        // NOTE: การตั้งค่า grid dimensions อาจต้องทำผ่าน public methods
        // หรือเพิ่ม public methods ใน InventoryGridManager

        Debug.Log($"[MultiPageInventory] Configured grid manager for {pageConfig.pageName}");
    }

    private void ClearInventoryGrids()
    {
        foreach (InventoryGridManager gridManager in gridManagers)
        {
            if (gridManager != null)
            {
                DestroyImmediate(gridManager.gameObject);
            }
        }
        gridManagers.Clear();
    }
    #endregion

    #region Page Switching
    public void SwitchToPage(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= inventoryPages.Count)
        {
            Debug.LogWarning($"[MultiPageInventory] Invalid page index: {pageIndex}");
            return;
        }

        if (pageIndex == currentPageIndex)
        {
            Debug.Log($"[MultiPageInventory] Already on page {pageIndex}");
            return;
        }

        // ซ่อน current grid
        if (currentGridManager != null && currentGridManager.gameObject != null)
        {
            currentGridManager.gameObject.SetActive(false);
        }

        // เปลี่ยน page
        int previousPageIndex = currentPageIndex;
        currentPageIndex = pageIndex;

        // แสดง new grid
        if (pageIndex < gridManagers.Count)
        {
            currentGridManager = gridManagers[pageIndex];
            if (currentGridManager != null && currentGridManager.gameObject != null)
            {
                currentGridManager.gameObject.SetActive(true);

                // Force refresh grid
                RefreshCurrentGrid();
            }
        }

        // อัพเดท UI
        UpdatePageButtonSelections();
        UpdatePageUI();

        // แจ้ง event
        OnPageChanged?.Invoke(pageIndex);

        Debug.Log($"[MultiPageInventory] Switched from page {previousPageIndex} to page {pageIndex} ({inventoryPages[pageIndex].pageName})");
    }

    private void UpdatePageButtonSelections()
    {
        for (int i = 0; i < pageButtons.Count; i++)
        {
            UpdatePageButtonSelection(i, i == currentPageIndex);
        }
    }

    private void UpdatePageButtonSelection(int buttonIndex, bool selected)
    {
        if (buttonIndex >= 0 && buttonIndex < pageButtons.Count)
        {
            Button button = pageButtons[buttonIndex];
            if (button != null)
            {
                Image buttonImage = button.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = selected ? Color.yellow : inventoryPages[buttonIndex].pageColor;
                }
            }
        }
    }

    private void UpdatePageUI()
    {
        InventoryPageConfig currentPage = GetCurrentPageConfig();

        if (pageTitle != null)
        {
            pageTitle.text = currentPage.pageName;
        }

        if (slotCountText != null)
        {
            int usedSlots = GetUsedSlotsInCurrentPage();
            slotCountText.text = $"{usedSlots}/{currentPage.slotsPerPage}";
        }
    }
    #endregion

    #region Event Handlers
    private void HandleSlotSelectionChanged(int pageIndex, int slotIndex)
    {
        if (pageIndex == currentPageIndex)
        {
            OnSlotSelectionChanged?.Invoke(pageIndex, slotIndex);
            Debug.Log($"[MultiPageInventory] Slot selected: Page {pageIndex}, Slot {slotIndex}");
        }
    }
    #endregion

    #region Helper Methods
    private InventoryPageConfig GetCurrentPageConfig()
    {
        if (currentPageIndex >= 0 && currentPageIndex < inventoryPages.Count)
        {
            return inventoryPages[currentPageIndex];
        }
        return new InventoryPageConfig(); // fallback
    }

    private int GetUsedSlotsInCurrentPage()
    {
        if (currentGridManager != null && ownerCharacter?.GetInventory() != null)
        {
            // TODO: นับเฉพาะ items ที่ตรงกับ filter ของ page นี้
            return ownerCharacter.GetInventory().UsedSlots;
        }
        return 0;
    }

    private void RefreshCurrentGrid()
    {
        if (currentGridManager != null && ownerCharacter != null)
        {
            currentGridManager.ForceUpdateFromCharacter();
            Debug.Log($"[MultiPageInventory] Refreshed current grid: {GetCurrentPageConfig().pageName}");
        }
    }

    private void RefreshAllGrids()
    {
        foreach (InventoryGridManager gridManager in gridManagers)
        {
            if (gridManager != null && ownerCharacter != null)
            {
                gridManager.ForceUpdateFromCharacter();
            }
        }
        Debug.Log("[MultiPageInventory] Refreshed all grids");
    }
    #endregion

    #region Public Interface
    public void SetOwnerCharacter(Character character)
    {
        ownerCharacter = character;

        // เชื่อมต่อทุก grid managers
        foreach (InventoryGridManager gridManager in gridManagers)
        {
            if (gridManager != null)
            {
                gridManager.SetOwnerCharacter(character);
            }
        }

        RefreshCurrentGrid();
        UpdatePageUI();

        Debug.Log($"[MultiPageInventory] Set owner character: {character?.CharacterName ?? "None"}");
    }

    public void ForceRefreshAllPages()
    {
        RefreshAllGrids();
        UpdatePageUI();
        Debug.Log("[MultiPageInventory] Force refreshed all pages");
    }

    public void AddCustomPage(InventoryPageConfig pageConfig)
    {
        inventoryPages.Add(pageConfig);

        // สร้าง UI elements ใหม่
        CreateSinglePageButton(inventoryPages.Count - 1);
        CreateSingleInventoryGrid(inventoryPages.Count - 1);

        Debug.Log($"[MultiPageInventory] Added custom page: {pageConfig.pageName}");
    }

    public InventoryGridManager GetGridManager(int pageIndex)
    {
        if (pageIndex >= 0 && pageIndex < gridManagers.Count)
        {
            return gridManagers[pageIndex];
        }
        return null;
    }

    public List<string> GetPageNames()
    {
        List<string> names = new List<string>();
        foreach (InventoryPageConfig page in inventoryPages)
        {
            names.Add(page.pageName);
        }
        return names;
    }
    #endregion

    #region Context Menu (เฉพาะ Editor)
#if UNITY_EDITOR
    [ContextMenu("Recreate All Pages")]
    private void RecreateAllPages()
    {
        inventoryPages.Clear();
        SetupDefaultPages();

        if (Application.isPlaying)
        {
            CreatePageButtons();
            CreateInventoryGrids();
            SwitchToPage(0);
        }

        Debug.Log("[MultiPageInventory] Recreated all pages");
    }

    [ContextMenu("Switch to Next Page")]
    private void SwitchToNextPage()
    {
        if (Application.isPlaying)
        {
            int nextPage = (currentPageIndex + 1) % inventoryPages.Count;
            SwitchToPage(nextPage);
        }
    }
#endif
    #endregion
}