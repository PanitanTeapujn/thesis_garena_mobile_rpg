using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class InventoryGridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 8;     // 8 คอลัมน์
    [SerializeField] private int gridHeight = 6;    // 6 แถว
    [SerializeField] private float cellSize = 60f;   // ขนาดแต่ละ cell (ลดลงจาก 80f)
    [SerializeField] private float spacing = 3f;     // ระยะห่างระหว่าง cells (ลดลงจาก 5f)
    [SerializeField] private bool autoFitToParent = true;  // ปรับขนาดอัตโนมัติตาม parent
    [Header("Character Integration")]
    [SerializeField] private Character ownerCharacter;          // Character ที่เป็นเจ้าของ inventory นี้
    [SerializeField] private bool autoDetectCharacter = true;   // หา Character อัตโนมัติ
    [Header("UI References")]
    [SerializeField] private Transform gridParent;          // Parent สำหรับ grid
    [SerializeField] private GameObject slotPrefab;         // Prefab สำหรับแต่ละ slot
    [SerializeField] private GridLayoutGroup gridLayout;   // Grid Layout Group component

    [Header("Grid Info")]
    [SerializeField] private List<InventorySlot> allSlots = new List<InventorySlot>();
    [SerializeField] private int selectedSlotIndex = -1;    // slot ที่เลือกอยู่ปัจจุบัน
    [SerializeField] private int totalSlots = 48;           // รวม 48 slots (6x8)

    // Events
    public System.Action<int> OnSlotSelectionChanged;   // เมื่อเลือก slot ใหม่
    public System.Action<int> OnSlotDoubleClicked;      // เมื่อ double click (สำหรับ equip ในอนาคต)
    [Header("🔍 Inventory Filter System")]
    public TMP_Dropdown categoryFilterDropdown;     // Dropdown สำหรับ filter ตาม ItemType
    public TMP_Dropdown tierFilterDropdown;        // Dropdown สำหรับ filter ตาม ItemTier
    public Button clearFiltersButton;              // ปุ่มล้าง filters
    public TextMeshProUGUI filteredItemCountText;  // แสดงจำนวน items หลัง filter

    [Header("🔍 Filter Variables")]
    private ItemType selectedItemType = ItemType.Weapon; // เก็บ type ที่เลือก (-1 = All)
    private ItemTier selectedItemTier = ItemTier.Common;  // เก็บ tier ที่เลือก (-1 = All)
    private bool isFilteringByType = false;              // สถานะการ filter ตาม type
    private bool isFilteringByTier = false;              // สถานะการ filter ตาม tier
    // Properties
    public Character OwnerCharacter { get { return ownerCharacter; } }
    public int TotalSlots { get { return totalSlots; } }
    public int SelectedSlotIndex { get { return selectedSlotIndex; } }
    public List<InventorySlot> AllSlots { get { return allSlots; } }

    #region Unity Lifecycle
    private void Awake()
    {
        SetupGridLayout();
    }

    private void Start()
    {
        SetupCharacterConnection();
        SetupInventoryFilters();

        // ✅ เปลี่ยนจาก CreateInventoryGrid(); เป็น
        // สร้าง grid เสมอ แม้ว่าจะยังไม่มี character
        if (allSlots.Count == 0)
        {
            CreateInventoryGrid();
            Debug.Log("[InventoryGrid] Created initial grid");
        }

        // ถ้ามี character แล้วให้ load items
        if (ownerCharacter != null)
        {
            LoadItemsFromCharacterInventory();
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        // ปรับขนาดอัตโนมัติเมื่อ parent panel เปลี่ยนขนาด
        if (autoFitToParent && gridLayout != null)
        {
            StartCoroutine(DelayedRefreshLayout());
        }
    }

    private IEnumerator DelayedRefreshLayout()
    {
        // รอ 1 frame เพื่อให้ layout update เสร็จก่อน
        yield return null;
        RefreshGridLayout();
    }
    #endregion

    #region Auto-Fit Calculation Methods
    private void CalculateOptimalCellSize()
    {
        RectTransform parentRect = GetComponentInParent<RectTransform>();
        if (parentRect == null)
        {
            Debug.LogWarning("[InventoryGrid] No parent RectTransform found for auto-fit calculation");
            return;
        }

        // ดึงขนาด parent panel
        Vector2 parentSize = parentRect.rect.size;

        // หัก margin สำหรับพื้นที่ว่าง
        float marginX = 40f; // margin ซ้าย-ขวา
        float marginY = 40f; // margin บน-ล่าง

        float availableWidth = parentSize.x - marginX;
        float availableHeight = parentSize.y - marginY;

        // คำนวณขนาดที่เหมาะสมสำหรับ cell
        float maxCellWidth = (availableWidth - (spacing * (gridWidth - 1))) / gridWidth;
        float maxCellHeight = (availableHeight - (spacing * (gridHeight - 1))) / gridHeight;

        // ใช้ขนาดที่เล็กกว่าเพื่อให้เป็นสี่เหลี่ยมจัตุรัส
        float optimalSize = Mathf.Min(maxCellWidth, maxCellHeight);

        // จำกัดขนาดไม่ให้เล็กเกินไปหรือใหญ่เกินไป
        optimalSize = Mathf.Clamp(optimalSize, 30f, 80f);

        cellSize = optimalSize;

        Debug.Log($"[InventoryGrid] Auto-fit calculated: Parent size={parentSize}, Available={availableWidth}x{availableHeight}, Optimal cell size={cellSize}");
    }

    public void RefreshGridLayout()
    {
        if (autoFitToParent)
        {
            CalculateOptimalCellSize();
        }

        if (gridLayout != null)
        {
            gridLayout.cellSize = new Vector2(cellSize, cellSize);
            gridLayout.spacing = new Vector2(spacing, spacing);
        }

        Debug.Log($"[InventoryGrid] Grid layout refreshed: Cell={cellSize}, Spacing={spacing}");
    }
    #endregion

    #region Grid Setup
    private void SetupGridLayout()
    {
        // หา GridLayoutGroup ถ้าไม่ได้ assign
        if (gridLayout == null)
            gridLayout = GetComponent<GridLayoutGroup>();

        // สร้าง GridLayoutGroup ถ้าไม่มี
        if (gridLayout == null)
        {
            gridLayout = gameObject.AddComponent<GridLayoutGroup>();
        }

        // 🎯 NEW: Auto-fit to parent panel
        if (autoFitToParent)
        {
            CalculateOptimalCellSize();
        }

        // ตั้งค่า Grid Layout
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = gridWidth;
        gridLayout.cellSize = new Vector2(cellSize, cellSize);
        gridLayout.spacing = new Vector2(spacing, spacing);
        gridLayout.childAlignment = TextAnchor.MiddleCenter;
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;

        // ตั้งค่า Content Size Fitter สำหรับ scroll
        ContentSizeFitter sizeFitter = GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = gameObject.AddComponent<ContentSizeFitter>();
        }
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Debug.Log($"[InventoryGrid] Grid Layout setup complete: {gridWidth}x{gridHeight} = {totalSlots} slots, Cell Size: {cellSize}x{cellSize}");
    }

    private void CreateInventoryGrid()
    {
        bool needsCanvasRefresh = !IsParentCanvasActive();

        // ลบ slots เก่าถ้ามี
        ClearExistingSlots();

        // ดึงจำนวน slots จาก Character ถ้ามี
        if (ownerCharacter != null)
        {
            int characterSlots = ownerCharacter.GetInventorySlotCount();
            totalSlots = characterSlots;

            // คำนวณ grid dimensions ใหม่
            CalculateGridDimensions(characterSlots);

            Debug.Log($"[InventoryGrid] Using character's inventory: {characterSlots} slots ({gridWidth}x{gridHeight})");
        }
        else
        {
            // ใช้ค่าเริ่มต้นถ้าไม่มี character
            totalSlots = gridWidth * gridHeight;
            Debug.LogWarning("[InventoryGrid] No character found, using default slot count");
        }

        // สร้าง slots ใหม่
        for (int i = 0; i < totalSlots; i++)
        {
            CreateSlot(i);
        }

        if (needsCanvasRefresh)
        {
            StartCoroutine(ForceCanvasRefreshRoutine());
        }

        // ✅ เพิ่ม immediate sync หลังสร้าง slots เสร็จ
        if (ownerCharacter != null)
        {
            StartCoroutine(ImmediateSyncAllSlotsAfterCreate());
        }

        Debug.Log($"[InventoryGrid] Created {allSlots.Count} inventory slots");
    }
    private System.Collections.IEnumerator ImmediateSyncAllSlotsAfterCreate()
    {
        // รอ 2 frames เพื่อให้ slots สร้างเสร็จ
        yield return null;
        yield return null;

        Debug.Log("[InventoryGrid] 🔄 Immediate sync all slots after creation...");

        if (ownerCharacter?.GetInventory() != null)
        {
            Inventory inventory = ownerCharacter.GetInventory();

            for (int i = 0; i < allSlots.Count && i < inventory.CurrentSlots; i++)
            {
                InventorySlot slot = allSlots[i];
                InventoryItem item = inventory.GetItem(i);

                if (slot != null)
                {
                    if (item == null || item.IsEmpty)
                    {
                        slot.SetEmptyState();
                    }
                    else
                    {
                        Sprite itemIcon = item.itemData.ItemIcon;
                        int stackCount = item.itemData.CanStack() && item.stackCount > 1 ? item.stackCount : 0;

                        slot.SetFilledState(itemIcon, stackCount);

                       

                        Debug.Log($"[InventoryGrid] Immediate sync slot {i}: {item.itemData.ItemName}");
                    }
                }
            }

            // Force canvas update
            Canvas.ForceUpdateCanvases();

            Debug.Log("[InventoryGrid] ✅ Immediate sync completed for all slots");
        }
    }
    private bool IsParentCanvasActive()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        return parentCanvas != null && parentCanvas.gameObject.activeInHierarchy;
    }

    // ✅ เพิ่ม Coroutine สำหรับ refresh canvas
    private IEnumerator ForceCanvasRefreshRoutine()
    {
        yield return null; // รอ 1 frame
        Canvas.ForceUpdateCanvases();

        yield return null; // รออีก 1 frame
        Canvas.ForceUpdateCanvases();

        Debug.Log("[InventoryGrid] Force refreshed canvas for slot creation");
    }

    private GameObject GetInventoryPanel()
    {
        // หาจาก CombatUIManager
        CombatUIManager uiManager = FindObjectOfType<CombatUIManager>();
        if (uiManager != null && uiManager.inventoryPanel != null)
        {
            return uiManager.inventoryPanel;
        }

        // หาจาก parent hierarchy
        Transform current = transform.parent;
        while (current != null)
        {
            if (current.name.ToLower().Contains("inventory"))
            {
                return current.gameObject;
            }
            current = current.parent;
        }

        return null;
    }

    private IEnumerator DelayedDeactivatePanel(GameObject panel)
    {
        // รอ 2 frames เพื่อให้ slots สร้างเสร็จ
        yield return null;
        yield return null;

        // ตรวจสอบว่า user ยังไม่ได้เปิด panel เอง
        CombatUIManager uiManager = FindObjectOfType<CombatUIManager>();
        bool userOpenedPanel = uiManager != null && uiManager.IsInventoryOpen();

        if (!userOpenedPanel)
        {
            panel.SetActive(false);
            Debug.Log("[InventoryGrid] Deactivated inventory panel after grid creation");
        }
    }
    private void CreateSlot(int slotIndex)
    {
        GameObject slotObj;

        // ใช้ prefab ถ้ามี ไม่งั้นสร้างใหม่
        if (slotPrefab != null)
        {
            slotObj = Instantiate(slotPrefab, transform);
        }
        else
        {
            slotObj = CreateSlotFromScratch();
        }

        slotObj.name = $"InventorySlot_{slotIndex}";

        // Setup InventorySlot component
        InventorySlot slot = slotObj.GetComponent<InventorySlot>();
        if (slot == null)
        {
            slot = slotObj.AddComponent<InventorySlot>();
        }

        slot.SlotIndex = slotIndex;
        slot.OnSlotSelected += HandleSlotSelected;

        // ✅ Setup components ทันทีที่สร้าง
        slot.ForceSetupComponents();

        // ✅ ลบบรรทัดนี้ออก - ไม่ให้ SetEmptyState ทันที
        // slot.SetEmptyState();

        allSlots.Add(slot);

        // ✅ เพิ่มบรรทัดนี้ - ถ้ามี character แล้วให้ sync ทันที
        if (ownerCharacter != null)
        {
            StartCoroutine(DelayedSyncSlotFromCharacter(slot, slotIndex));
        }

        Debug.Log($"[InventoryGrid] Created slot {slotIndex} without forcing empty state");
    }
    private System.Collections.IEnumerator DelayedSyncSlotFromCharacter(InventorySlot slot, int slotIndex)
    {
        // รอ 1 frame เพื่อให้ slot setup เสร็จ
        yield return null;

        if (ownerCharacter?.GetInventory() != null)
        {
            InventoryItem item = ownerCharacter.GetInventory().GetItem(slotIndex);

            if (item == null || item.IsEmpty)
            {
                slot.SetEmptyState();
            }
            else
            {
                Sprite itemIcon = item.itemData.ItemIcon;
                int stackCount = item.itemData.CanStack() && item.stackCount > 1 ? item.stackCount : 0;

                slot.SetFilledState(itemIcon, stackCount);

               

                Debug.Log($"[InventoryGrid] Slot {slotIndex} synced with item: {item.itemData.ItemName}");
            }
        }
        else
        {
            slot.SetEmptyState();
        }
    }
    private void EnsureSlotIsReady(InventorySlot slot)
    {
        // ตรวจสอบว่า components ครบ
        if (slot.slotBackground == null || slot.itemIcon == null || slot.slotButton == null)
        {
            Debug.LogWarning($"[InventoryGrid] Slot {slot.SlotIndex} missing components, re-setup");
            slot.ForceSetupComponents();
        }

        // Force layout update
        LayoutRebuilder.ForceRebuildLayoutImmediate(slot.GetComponent<RectTransform>());
    }
    private GameObject CreateSlotFromScratch()
    {
        // สร้าง slot object
        GameObject slotObj = new GameObject("InventorySlot");
        slotObj.transform.SetParent(transform, false);

        // เพิ่ม RectTransform
        RectTransform rectTransform = slotObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(cellSize, cellSize);

        // เพิ่ม Image สำหรับพื้นหลัง
        Image slotImage = slotObj.AddComponent<Image>();
        slotImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // เพิ่ม Button
        Button slotButton = slotObj.AddComponent<Button>();

        // ตั้งค่า Button transitions
        ColorBlock colorBlock = slotButton.colors;
        colorBlock.normalColor = Color.white;
        colorBlock.highlightedColor = new Color(1f, 1f, 1f, 0.8f);
        colorBlock.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colorBlock.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        slotButton.colors = colorBlock;

        // ✅ สร้าง ItemIcon GameObject
        GameObject iconObj = new GameObject("ItemIcon");
        iconObj.transform.SetParent(slotObj.transform, false);
        Image itemIcon = iconObj.AddComponent<Image>();

        // ตั้งค่า RectTransform ให้เต็ม slot แต่เล็กลงนิดหน่อย
        RectTransform iconRect = itemIcon.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = Vector2.one * 5f;  // margin 5 pixel
        iconRect.offsetMax = Vector2.one * -5f;

        itemIcon.raycastTarget = false; // ไม่ให้ block การกดปุ่ม
        itemIcon.preserveAspect = true; // รักษา aspect ratio ของ sprite
        itemIcon.gameObject.SetActive(false); // ซ่อนไว้ก่อน

        Debug.Log($"[InventoryGrid] Created slot from scratch with ItemIcon");
        return slotObj;
    }


    private void ClearExistingSlots()
    {
        // ยกเลิก event subscriptions
        foreach (InventorySlot slot in allSlots)
        {
            if (slot != null)
                slot.OnSlotSelected -= HandleSlotSelected;
        }

        // ลบ slot objects
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(transform.GetChild(i).gameObject);
            else
                DestroyImmediate(transform.GetChild(i).gameObject);
        }

        allSlots.Clear();
        selectedSlotIndex = -1;
    }
    #endregion

    #region Slot Selection Management
    private void HandleSlotSelected(int slotIndex)
    {
        Debug.Log($"[InventoryGrid] Slot {slotIndex} selected");

        // ยกเลิกการเลือก slot เก่า
        if (selectedSlotIndex >= 0 && selectedSlotIndex < allSlots.Count)
        {
            allSlots[selectedSlotIndex].SetSelectedState(false);
        }

        // เลือก slot ใหม่
        selectedSlotIndex = slotIndex;
        if (selectedSlotIndex >= 0 && selectedSlotIndex < allSlots.Count)
        {
            allSlots[selectedSlotIndex].SetSelectedState(true);
        }

        // แจ้ง event
        OnSlotSelectionChanged?.Invoke(selectedSlotIndex);
    }

    public void DeselectAllSlots()
    {
        if (selectedSlotIndex >= 0 && selectedSlotIndex < allSlots.Count)
        {
            allSlots[selectedSlotIndex].SetSelectedState(false);
        }
        selectedSlotIndex = -1;

        OnSlotSelectionChanged?.Invoke(-1);
        Debug.Log("[InventoryGrid] All slots deselected");
    }

    public InventorySlot GetSelectedSlot()
    {
        if (selectedSlotIndex >= 0 && selectedSlotIndex < allSlots.Count)
        {
            return allSlots[selectedSlotIndex];
        }
        return null;
    }

    public InventorySlot GetSlot(int index)
    {
        if (index >= 0 && index < allSlots.Count)
        {
            return allSlots[index];
        }
        return null;
    }
    #endregion
    private void SetupCharacterConnection()
    {
        Debug.Log("[InventoryGrid] Setting up character connection...");

        // หา Character อัตโนมัติถ้าไม่ได้ assign
        if (ownerCharacter == null && autoDetectCharacter)
        {
            ownerCharacter = FindCharacterMultipleWays();
            Debug.Log($"[InventoryGrid] Auto-detected character: {ownerCharacter?.CharacterName ?? "None"}");
        }

        // Subscribe to inventory events
        if (ownerCharacter != null)
        {
            Inventory.OnInventorySlotCountChanged += OnCharacterInventoryChanged;
            Inventory.OnInventoryItemChanged += OnCharacterItemChanged;

            // ตรวจสอบ Inventory component
            Inventory inventory = ownerCharacter.GetInventory();
            if (inventory == null)
            {
                Debug.LogError($"[InventoryGrid] Character {ownerCharacter.CharacterName} has no Inventory component!");
                return;
            }

            // เพิ่มการสร้าง grid ทันทีที่เจอ character
            if (allSlots.Count == 0)
            {
                Debug.Log("[InventoryGrid] Creating grid early for character connection");
                CreateInventoryGrid();
            }

            Debug.Log($"[InventoryGrid] Connected to {ownerCharacter.CharacterName}'s inventory ({inventory.UsedSlots}/{inventory.CurrentSlots} slots)");
        }
        else
        {
            Debug.LogWarning("[InventoryGrid] No character found during setup!");

            // ลองหาอีกครั้งใน coroutine
            StartCoroutine(RetryFindCharacter());
        }
    }
    private Character FindCharacterMultipleWays()
    {
        Character foundCharacter = null;

        // วิธีที่ 1: หาจาก parent objects
        foundCharacter = GetComponentInParent<Character>();
        if (foundCharacter != null)
        {
            Debug.Log("[InventoryGrid] Found character from parent");
            return foundCharacter;
        }

        // วิธีที่ 2: หาจาก UI Manager
        CombatUIManager uiManager = FindObjectOfType<CombatUIManager>();
        if (uiManager != null && uiManager.localHero != null)
        {
            foundCharacter = uiManager.localHero;
            Debug.Log("[InventoryGrid] Found character from UI Manager");
            return foundCharacter;
        }

        // วิธีที่ 3: หาจาก Hero ที่มี InputAuthority
        Hero[] heroes = FindObjectsOfType<Hero>();
        foreach (Hero hero in heroes)
        {
            if (hero.HasInputAuthority && hero.IsSpawned)
            {
                foundCharacter = hero;
                Debug.Log("[InventoryGrid] Found character from Hero with InputAuthority");
                return foundCharacter;
            }
        }

        // วิธีที่ 4: หา Character ใดๆ ที่มี Inventory
        Character[] characters = FindObjectsOfType<Character>();
        foreach (Character character in characters)
        {
            if (character.GetInventory() != null)
            {
                foundCharacter = character;
                Debug.Log("[InventoryGrid] Found character with Inventory component");
                return foundCharacter;
            }
        }

        Debug.LogWarning("[InventoryGrid] No character found with any method!");
        return null;
    }

    // ✅ เพิ่ม coroutine เพื่อลองหา character อีกครั้ง
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
                Debug.Log($"[InventoryGrid] Found character on retry: {ownerCharacter.CharacterName}");

                // Setup connection ใหม่
                Inventory.OnInventorySlotCountChanged += OnCharacterInventoryChanged;
                Inventory.OnInventoryItemChanged += OnCharacterItemChanged;

                // สร้าง grid ถ้ายังไม่มี
                if (allSlots.Count == 0)
                {
                    CreateInventoryGrid();
                }
                else
                {
                    LoadItemsFromCharacterInventory();
                }

                break;
            }
        }

        if (ownerCharacter == null)
        {
            Debug.LogError("[InventoryGrid] Failed to find character after retry!");
        }
    }

    // ✅ เพิ่ม method สำหรับ manual set character (เรียกจาก CombatUIManager)
    public void SetOwnerCharacter(Character character)
    {
        // ยกเลิก subscription เก่า
        if (ownerCharacter != null)
        {
            Inventory.OnInventorySlotCountChanged -= OnCharacterInventoryChanged;
            Inventory.OnInventoryItemChanged -= OnCharacterItemChanged;
        }

        ownerCharacter = character;

        Debug.Log($"[InventoryGrid] Manually set owner character: {ownerCharacter?.CharacterName ?? "None"}");

        // Subscribe ใหม่
        if (ownerCharacter != null)
        {
            // ตรวจสอบ Inventory component
            Inventory inventory = ownerCharacter.GetInventory();
            if (inventory == null)
            {
                Debug.LogError($"[InventoryGrid] Character {ownerCharacter.CharacterName} has no Inventory component!");
                return;
            }

            Inventory.OnInventorySlotCountChanged += OnCharacterInventoryChanged;
            Inventory.OnInventoryItemChanged += OnCharacterItemChanged;

            // อัปเดต grid ทันที
            if (allSlots.Count == 0)
            {
                CreateInventoryGrid();
            }
            else
            {
                UpdateGridFromCharacterInventory();
            }
            ForceLoadFromCharacterAfterConnection();

            Debug.Log($"[InventoryGrid] Successfully connected to {ownerCharacter.CharacterName}'s inventory");

            // ✅ เพิ่มบรรทัดนี้ - แจ้ง ItemDeleteManager
            NotifyItemDeleteManagerForRefresh();
        }
    }
    // เพิ่ม method สำหรับ handle character inventory events
    private void OnCharacterInventoryChanged(Character character, int newSlotCount)
    {
        if (character == ownerCharacter)
        {
            Debug.Log($"[InventoryGrid] Character inventory slots changed: {newSlotCount}");
            UpdateGridFromCharacterInventory();
        }
    }

    private void OnCharacterItemChanged(Character character, int slotIndex, InventoryItem item)
    {
        if (character == ownerCharacter)
        {
            Debug.Log($"[InventoryGrid] Item changed at slot {slotIndex}: {(item?.itemData?.ItemName ?? "Empty")}");

            // ✅ สร้าง grid ถ้ายังไม่มี slots
            if (allSlots.Count == 0)
            {
                Debug.Log("[InventoryGrid] No slots exist, force creating grid...");
                CreateInventoryGrid();
                return;
            }

            // อัปเดต slot ทันที
            if (slotIndex < allSlots.Count)
            {
                UpdateSlotFromInventoryItem(slotIndex, item);
            }
            else
            {
                Debug.LogWarning($"[InventoryGrid] Slot index {slotIndex} out of range. Recreating grid...");
                CreateInventoryGrid();
            }
        }
    }
    public void ForceUpdateFromCharacter()
    {
        if (ownerCharacter == null) return;

        Debug.Log("[InventoryGrid] Force updating from character inventory...");

        // สร้าง grid ถ้ายังไม่มี
        if (allSlots.Count == 0)
        {
            CreateInventoryGrid();
        }
        else
        {
            // อัปเดต items ที่มีอยู่
            LoadItemsFromCharacterInventory();
        }
    }
    private void CalculateGridDimensions(int slotCount)
    {
        // หา dimensions ที่เหมาะสมที่สุด
        if (slotCount <= 24) // 6x4
        {
            gridWidth = 6;
            gridHeight = 4;
        }
        else if (slotCount <= 30) // 6x5
        {
            gridWidth = 6;
            gridHeight = 5;
        }
        else if (slotCount <= 36) // 6x6
        {
            gridWidth = 6;
            gridHeight = 6;
        }
        else if (slotCount <= 42) // 7x6
        {
            gridWidth = 7;
            gridHeight = 6;
        }
        else // 8x6 หรือมากกว่า
        {
            gridWidth = 8;
            gridHeight = Mathf.CeilToInt((float)slotCount / gridWidth);
        }

        // อัปเดต GridLayoutGroup
        if (gridLayout != null)
        {
            gridLayout.constraintCount = gridWidth;
        }

        totalSlots = gridWidth * gridHeight; // อาจจะมากกว่า slotCount เล็กน้อย
    }

    // เพิ่ม method สำหรับโหลด items จาก character
    private void LoadItemsFromCharacterInventory()
    {
        if (ownerCharacter == null || ownerCharacter.GetInventory() == null) return;

        Inventory characterInventory = ownerCharacter.GetInventory();

        Debug.Log($"[InventoryGrid] Loading {characterInventory.UsedSlots} items from character inventory...");

        for (int i = 0; i < totalSlots && i < characterInventory.CurrentSlots; i++)
        {
            InventoryItem item = characterInventory.GetItem(i);
            UpdateSlotFromInventoryItem(i, item);

            // ✅ เพิ่มบรรทัดนี้ - Force sync ทุก slot ทันที
            if (i < allSlots.Count && allSlots[i] != null)
            {
                allSlots[i].ForceSyncFromCharacterNow();
            }
        }

        // ✅ เพิ่ม force refresh ทั้ง grid
        ForceRefreshAllSlots();
        ApplyInventoryFilters();

        Debug.Log("[InventoryGrid] ✅ Completed loading all items with force sync");
    }
    public void ForceRefreshAllSlots()
    {
        Debug.Log("[InventoryGrid] 🔄 Force refreshing all slots...");

        if (ownerCharacter?.GetInventory() == null)
        {
            Debug.LogWarning("[InventoryGrid] No character inventory to refresh from");
            return;
        }

        Inventory inventory = ownerCharacter.GetInventory();

        // ✅ Debug inventory ก่อน refresh
        Debug.Log($"[InventoryGrid] Inventory has {inventory.UsedSlots}/{inventory.CurrentSlots} items");

        for (int i = 0; i < 5 && i < inventory.CurrentSlots; i++)
        {
            var item = inventory.GetItem(i);
            string itemInfo = item?.IsEmpty != false ? "EMPTY" : $"{item.itemData.ItemName} x{item.stackCount}";
            Debug.Log($"[InventoryGrid] Slot {i}: {itemInfo}");
        }

        // Refresh ทุก slot พร้อม debug
        int refreshedCount = 0;
        foreach (InventorySlot slot in allSlots)
        {
            if (slot != null)
            {
                // ดึงข้อมูลจาก character inventory
                int slotIndex = slot.SlotIndex;
                if (slotIndex >= 0 && slotIndex < inventory.CurrentSlots)
                {
                    var item = inventory.GetItem(slotIndex);

                    // Debug ก่อน sync
                    string beforeState = slot.IsEmpty ? "EMPTY" : "FILLED";
                    string inventoryState = item?.IsEmpty != false ? "EMPTY" : $"{item.itemData.ItemName} x{item.stackCount}";

                    // Force sync
                    slot.ForceSyncFromCharacterNow();

                    // Debug หลัง sync
                    string afterState = slot.IsEmpty ? "EMPTY" : "FILLED";

                    if (beforeState != afterState)
                    {
                        Debug.Log($"[InventoryGrid] Slot {slotIndex}: {beforeState} → {afterState} (Inventory: {inventoryState})");
                    }

                    refreshedCount++;
                }
            }
        }

        // Force canvas update
        Canvas.ForceUpdateCanvases();
        ApplyInventoryFilters();

        Debug.Log($"[InventoryGrid] ✅ Refreshed {refreshedCount} slots");
    }
    // เพิ่ม method สำหรับอัปเดต slot จาก inventory item
    private void UpdateSlotFromInventoryItem(int slotIndex, InventoryItem item)
    {
        if (slotIndex < 0 || slotIndex >= allSlots.Count)
        {
            Debug.LogWarning($"[InventoryGrid] Slot index {slotIndex} out of range (0-{allSlots.Count - 1})");
            return;
        }

        InventorySlot slot = allSlots[slotIndex];

        if (slot == null)
        {
            return;
        }

        if (item == null || item.IsEmpty)
        {
            slot.SetEmptyState();
        }
        else
        {
            // ใช้ icon จาก ItemData และแสดง stack count พร้อม tier
            Sprite itemIcon = item.itemData.ItemIcon;
            int stackCount = item.stackCount;
            ItemTier tier = item.itemData.Tier; // 🆕 เพิ่มการดึง tier

            if (itemIcon == null)
            {
                return;
            }

            // แสดง stack count เฉพาะตอนที่ item สามารถ stack ได้ และมีมากกว่า 1
            bool showStackCount = item.itemData.CanStack() && stackCount > 1;

            // 🔧 เรียกใช้ method ใหม่ที่รับ tier ด้วย
            slot.SetFilledState(itemIcon, showStackCount ? stackCount : 0, tier);
        }

        // Force sync slot ทันที
        slot.ForceSyncFromCharacterNow();
    }
    private IEnumerator DelayedCanvasRefresh()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
    }

    public void ForceSyncAllSlots()
    {
       // Debug.Log("[InventoryGrid] Force syncing all slots...");

        if (ownerCharacter?.GetInventory() == null) return;

        Inventory inventory = ownerCharacter.GetInventory();

        for (int i = 0; i < allSlots.Count && i < inventory.CurrentSlots; i++)
        {
            InventoryItem item = inventory.GetItem(i);
            InventorySlot slot = allSlots[i];

            if (slot != null)
            {
             //   Debug.Log($"[InventoryGrid] Syncing slot {i}: {(item?.IsEmpty != false ? "Empty" : item.itemData.ItemName)}");
                UpdateSlotFromInventoryItem(i, item);
            }
        }

        // Clear remaining slots
        for (int i = inventory.CurrentSlots; i < allSlots.Count; i++)
        {
            if (allSlots[i] != null)
            {
                allSlots[i].SetEmptyState();
            }
        }
    }
    // เพิ่ม method สำหรับอัปเดต grid จาก character inventory
    public void UpdateGridFromCharacterInventory()
    {
        if (ownerCharacter == null) return;

        int newSlotCount = ownerCharacter.GetInventorySlotCount();

        if (newSlotCount != totalSlots)
        {
            // สร้าง grid ใหม่ถ้าจำนวน slots เปลี่ยน
            CreateInventoryGrid();
        }
        else
        {
            // อัปเดต items ถ้าจำนวน slots เท่าเดิม
            LoadItemsFromCharacterInventory();
        }
    }

    // เพิ่ม method สำหรับ set character manually
    public void ForceLoadFromCharacterAfterConnection()
    {
        if (ownerCharacter == null) return;

       // Debug.Log("[InventoryGrid] Force loading items after character connection...");

        // ตรวจสอบว่ามี slots แล้วหรือยัง
        if (allSlots.Count == 0)
        {
            CreateInventoryGrid();
        }

        // โหลด items ทันที
        LoadItemsFromCharacterInventory();

        // ✅ Force refresh ทุก slot
        ForceRefreshAllSlots();

        // ✅ เพิ่มบรรทัดนี้ - แจ้ง ItemDeleteManager หลัง load เสร็จ
        NotifyItemDeleteManagerForRefresh();

       // Debug.Log("[InventoryGrid] ✅ Force load completed with all slots refreshed");
    }
    public bool IsProperlyConnected()
    {
        bool hasCharacter = ownerCharacter != null;
        bool hasInventory = ownerCharacter?.GetInventory() != null;
        bool hasSlots = allSlots != null && allSlots.Count > 0;

       // Debug.Log($"[InventoryGrid] Connection Status - Character: {hasCharacter}, Inventory: {hasInventory}, Slots: {hasSlots}");

        return hasCharacter && hasInventory && hasSlots;
    }

    private void OnDestroy()
    {
        if (ownerCharacter != null)
        {
            Inventory.OnInventorySlotCountChanged -= OnCharacterInventoryChanged;
            Inventory.OnInventoryItemChanged -= OnCharacterItemChanged;
        }
    }
    #region Public Methods for Testing





    #endregion
    public void UpdateSlotFromCharacter(int slotIndex)
    {
        if (ownerCharacter?.GetInventory() == null)
        {
         //   Debug.LogWarning($"[InventoryGrid] No character or inventory to update slot {slotIndex}");
            return;
        }

        if (slotIndex < 0 || slotIndex >= allSlots.Count)
        {
         //   Debug.LogWarning($"[InventoryGrid] Slot index {slotIndex} out of range (0-{allSlots.Count - 1})");
            return;
        }

        Inventory inventory = ownerCharacter.GetInventory();
        InventoryItem item = inventory.GetItem(slotIndex);
        InventorySlot slot = allSlots[slotIndex];

        if (slot == null)
        {
         //   Debug.LogError($"[InventoryGrid] Slot {slotIndex} is null!");
            return;
        }

      //  Debug.Log($"[InventoryGrid] 🔄 Updating slot {slotIndex} from character data...");

        // 🆕 แสดงข้อมูลก่อนและหลัง update
        string beforeState = slot.IsEmpty ? "EMPTY" : "FILLED";
        string itemInfo = item?.IsEmpty != false ? "EMPTY" : $"{item.itemData.ItemName} x{item.stackCount}";

       // Debug.Log($"[InventoryGrid] Slot {slotIndex} - Before: {beforeState}, Character Data: {itemInfo}");

        // อัปเดต slot ตามข้อมูลจาก character
        UpdateSlotFromInventoryItem(slotIndex, item);

        string afterState = slot.IsEmpty ? "EMPTY" : "FILLED";
     //   Debug.Log($"[InventoryGrid] Slot {slotIndex} - After: {afterState}");

        // 🆕 Force refresh เฉพาะ slot นี้
        if (slot.slotButton != null)
        {
            // Trigger layout rebuild สำหรับ slot นี้
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(slot.GetComponent<RectTransform>());
        }
    }
    public void NotifyItemDeleteManagerForRefresh()
    {
      //  Debug.Log("[InventoryGrid] 📢 Notifying ItemDeleteManager to refresh references...");

        try
        {
            ItemDeleteManager deleteManager = FindObjectOfType<ItemDeleteManager>();
            if (deleteManager != null)
            {
                deleteManager.ForceRefreshReferences();
             //   Debug.Log("[InventoryGrid] ✅ Successfully notified ItemDeleteManager");
            }
            else
            {
               // Debug.LogWarning("[InventoryGrid] ItemDeleteManager not found for notification");
            }
        }
        catch (System.Exception e)
        {
          //  Debug.LogError($"[InventoryGrid] Error notifying ItemDeleteManager: {e.Message}");
        }
    }
    #region Runtime Grid Modification
    public void ResizeGrid(int newWidth, int newHeight)
    {
        gridWidth = newWidth;
        gridHeight = newHeight;
        gridLayout.constraintCount = gridWidth;

        CreateInventoryGrid();
        Debug.Log($"[InventoryGrid] Resized to {gridWidth}x{gridHeight}");
    }

    public void SetCellSize(float newSize)
    {
        cellSize = newSize;
        gridLayout.cellSize = new Vector2(cellSize, cellSize);
        Debug.Log($"[InventoryGrid] Cell size changed to {cellSize}");
    }

    public void SetSpacing(float newSpacing)
    {
        spacing = newSpacing;
        gridLayout.spacing = new Vector2(spacing, spacing);
        Debug.Log($"[InventoryGrid] Spacing changed to {spacing}");
    }
    #endregion

    private void SetupInventoryFilters()
    {
      //  Debug.Log("[InventoryGrid] 🔧 Setting up inventory filters...");

        // ✅ Setup Category Filter Dropdown
        if (categoryFilterDropdown != null)
        {
            try
            {
                categoryFilterDropdown.ClearOptions();
                List<string> categoryOptions = new List<string> { "All Types" };

                foreach (ItemType itemType in System.Enum.GetValues(typeof(ItemType)))
                {
                    categoryOptions.Add(itemType.ToString());
                }

                categoryFilterDropdown.AddOptions(categoryOptions);
                categoryFilterDropdown.value = 0;
                categoryFilterDropdown.RefreshShownValue();

                // ✅ Setup dropdown content layout พร้อม optimization
                SetupDropdownContentLayout(categoryFilterDropdown, "Category");
                OptimizeDropdownUsability(categoryFilterDropdown, "Category");

                // เพิ่ม event listener
                categoryFilterDropdown.onValueChanged.AddListener(OnCategoryFilterChanged);

              //  Debug.Log($"[InventoryGrid] ✅ Category filter setup: {categoryOptions.Count} options");
            }
            catch (System.Exception e)
            {
             //   Debug.LogError($"[InventoryGrid] ❌ Category filter setup failed: {e.Message}");
            }
        }

        // ✅ Setup Tier Filter Dropdown
        if (tierFilterDropdown != null)
        {
            try
            {
                tierFilterDropdown.ClearOptions();
                List<string> tierOptions = new List<string> { "All Tiers" };

                foreach (ItemTier tier in System.Enum.GetValues(typeof(ItemTier)))
                {
                    tierOptions.Add(tier.ToString());
                }

                tierFilterDropdown.AddOptions(tierOptions);
                tierFilterDropdown.value = 0;
                tierFilterDropdown.RefreshShownValue();

                // ✅ Setup dropdown content layout พร้อม optimization
                SetupDropdownContentLayout(tierFilterDropdown, "Tier");
                OptimizeDropdownUsability(tierFilterDropdown, "Tier");

                // เพิ่ม event listener
                tierFilterDropdown.onValueChanged.AddListener(OnTierFilterChanged);

             //   Debug.Log($"[InventoryGrid] ✅ Tier filter setup: {tierOptions.Count} options");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[InventoryGrid] ❌ Tier filter setup failed: {e.Message}");
            }
        }

        // ✅ Setup Clear Filters Button
        if (clearFiltersButton != null)
        {
            clearFiltersButton.onClick.RemoveAllListeners();
            clearFiltersButton.onClick.AddListener(ClearAllFilters);
          //  Debug.Log("[InventoryGrid] ✅ Clear filters button setup");
        }

       // Debug.Log("[InventoryGrid] 🎉 Inventory filters setup completed with improved usability!");
    }
    private void SetupDropdownContentLayout(TMP_Dropdown dropdown, string dropdownName)
    {
        if (dropdown == null) return;

        try
        {
            // หา dropdown content (Template)
            Transform template = dropdown.transform.Find("Template");
            if (template == null)
            {
              //  Debug.LogWarning($"[InventoryGrid] No Template found in {dropdownName} dropdown");
                return;
            }

            // หา Viewport และ Content
            Transform viewport = template.Find("Viewport");
            if (viewport == null)
            {
           //     Debug.LogWarning($"[InventoryGrid] No Viewport found in {dropdownName} dropdown template");
                return;
            }

            Transform content = viewport.Find("Content");
            if (content == null)
            {
            //    Debug.LogWarning($"[InventoryGrid] No Content found in {dropdownName} dropdown viewport");
                return;
            }

            // ✅ เพิ่ม Vertical Layout Group ใน Content
            VerticalLayoutGroup verticalLayout = content.GetComponent<VerticalLayoutGroup>();
            if (verticalLayout == null)
            {
                verticalLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            //    Debug.Log($"[InventoryGrid] ✅ Added VerticalLayoutGroup to {dropdownName} dropdown content");
            }

            // ✅ ตั้งค่า Vertical Layout Group ให้กดง่าย (เหมือน ShopLooby)
            verticalLayout.padding = new RectOffset(2, 2, 15, 2);       // padding น้อยลง
            verticalLayout.spacing = 25f;                               // ไม่มีระยะห่างระหว่างตัวเลือก
            verticalLayout.childAlignment = TextAnchor.UpperLeft;       // จัดเรียงซ้ายบน
            verticalLayout.childControlWidth = true;                   // ควบคุมความกว้าง
            verticalLayout.childControlHeight = true;                  // ควบคุมความสูง
            verticalLayout.childForceExpandWidth = true;               // บังคับขยายความกว้าง
            verticalLayout.childForceExpandHeight = false;             // ไม่บังคับขยายความสูง

            // ✅ เพิ่ม Content Size Fitter
            ContentSizeFitter sizeFitter = content.GetComponent<ContentSizeFitter>();
            if (sizeFitter == null)
            {
                sizeFitter = content.gameObject.AddComponent<ContentSizeFitter>();
            }
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // ✅ ปรับ Template size ให้เหมาะสมกับการกด
            RectTransform templateRect = template.GetComponent<RectTransform>();
            if (templateRect != null)
            {
                // กำหนดความสูงสูงสุดของ dropdown
                float maxHeight = 300; // สูงสุด 200 pixels
                Vector2 templateSize = templateRect.sizeDelta;
                templateSize.y = maxHeight;
                templateRect.sizeDelta = templateSize;

             //   Debug.Log($"[InventoryGrid] Set {dropdownName} dropdown template max height to {maxHeight}");
            }

            // ✅ เพิ่ม ScrollRect ถ้าไม่มี (สำหรับกรณีตัวเลือกเยอะ)
            ScrollRect scrollRect = template.GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                scrollRect = template.gameObject.AddComponent<ScrollRect>();
                scrollRect.content = content.GetComponent<RectTransform>();
                scrollRect.viewport = viewport.GetComponent<RectTransform>();
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.scrollSensitivity = 20f;

              //  Debug.Log($"[InventoryGrid] ✅ Added ScrollRect to {dropdownName} dropdown");
            }

            // ✅ ปรับแต่ง dropdown items ให้กดง่าย
            SetupDropdownItemInteraction(dropdown, dropdownName);

            Debug.Log($"[InventoryGrid] ✅ {dropdownName} dropdown content layout setup completed");

        }
        catch (System.Exception e)
        {
            Debug.LogError($"[InventoryGrid] ❌ Error setting up {dropdownName} dropdown layout: {e.Message}");
        }
    }
    private void SetupDropdownItemInteraction(TMP_Dropdown dropdown, string dropdownName)
    {
        if (dropdown == null) return;

        try
        {
            // หา dropdown item prefab
            Transform template = dropdown.transform.Find("Template");
            if (template == null) return;

            Transform viewport = template.Find("Viewport");
            if (viewport == null) return;

            Transform content = viewport.Find("Content");
            if (content == null) return;

            Transform item = content.Find("Item");
            if (item == null) return;

            // ✅ ปรับแต่ง item toggle component
            Toggle itemToggle = item.GetComponent<Toggle>();
            if (itemToggle != null)
            {
                // ปรับสี transition ให้เห็นชัดเจนขึ้น
                ColorBlock colors = itemToggle.colors;
                colors.normalColor = new Color(1f, 1f, 1f, 0.8f);      // ปกติ - ขาวโปร่งใส
                colors.highlightedColor = new Color(0.9f, 0.9f, 1f, 1f); // hover - ฟ้าอ่อน
                colors.pressedColor = new Color(0.7f, 0.7f, 1f, 1f);     // กด - ฟ้าเข้ม
                colors.selectedColor = new Color(0.8f, 1f, 0.8f, 1f);    // เลือก - เขียวอ่อน
                colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // ปิดใช้งาน - เทา
                colors.colorMultiplier = 1f;
                colors.fadeDuration = 0.1f;
                itemToggle.colors = colors;

                // ตั้งค่า transition เป็น ColorTint เพื่อให้เห็นการเปลี่ยนแปลงชัด
                itemToggle.transition = Selectable.Transition.ColorTint;
            }

            // ✅ ปรับขนาด item ให้กดง่าย
            RectTransform itemRect = item.GetComponent<RectTransform>();
            if (itemRect != null)
            {
                // เพิ่มความสูงของแต่ละ item ให้กดง่าย
                Vector2 itemSize = itemRect.sizeDelta;
                itemSize.y = Mathf.Max(itemSize.y, 25f); // ความสูงขั้นต่ำ 25 pixels
                itemRect.sizeDelta = itemSize;
            }

            // ✅ ปรับแต่ง text component ใน item
            TextMeshProUGUI itemText = item.GetComponentInChildren<TextMeshProUGUI>();
            if (itemText != null)
            {
                // ตั้งค่า text alignment
                itemText.alignment = TextAlignmentOptions.Left;
                itemText.margin = new Vector4(10, 4, 10, 4); // margin ซ้าย, บน, ขวา, ล่าง

                // ปรับขนาดตัวอักษรให้อ่านง่าย
                if (itemText.fontSize < 14f)
                {
                    itemText.fontSize = 14f;
                }
            }

         //   Debug.Log($"[InventoryGrid] ✅ {dropdownName} dropdown item interaction setup completed");

        }
        catch (System.Exception e)
        {
           // Debug.LogError($"[InventoryGrid] ❌ Error setting up {dropdownName} dropdown item interaction: {e.Message}");
        }
    }

    // ✅ เพิ่ม method สำหรับปรับแต่ง dropdown ทั้งตัว
    private void OptimizeDropdownUsability(TMP_Dropdown dropdown, string dropdownName)
    {
        if (dropdown == null) return;

        try
        {
            // ✅ ปรับแต่ง dropdown button เอง
            Button dropdownButton = dropdown.GetComponent<Button>();
            if (dropdownButton != null)
            {
                // ปรับสี transition
                ColorBlock colors = dropdownButton.colors;
                colors.normalColor = new Color(0.9f, 0.9f, 0.9f, 1f);
                colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
                colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
                colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
                colors.colorMultiplier = 1f;
                colors.fadeDuration = 0.1f;
                dropdownButton.colors = colors;
            }

            // ✅ ปรับขนาด dropdown ให้กดง่าย
            RectTransform dropdownRect = dropdown.GetComponent<RectTransform>();
            if (dropdownRect != null)
            {
                Vector2 dropdownSize = dropdownRect.sizeDelta;
                dropdownSize.y = Mathf.Max(dropdownSize.y, 30f); // ความสูงขั้นต่ำ 30 pixels
                dropdownRect.sizeDelta = dropdownSize;
            }

            // ✅ ปรับแต่ง dropdown arrow
            Transform arrow = dropdown.transform.Find("Arrow");
            if (arrow != null)
            {
                Image arrowImage = arrow.GetComponent<Image>();
                if (arrowImage != null)
                {
                    arrowImage.color = new Color(0.3f, 0.3f, 0.3f, 1f); // สีเทาเข้มให้เห็นชัด
                }
            }

           // Debug.Log($"[InventoryGrid] ✅ {dropdownName} dropdown usability optimized");

        }
        catch (System.Exception e)
        {
          //  Debug.LogError($"[InventoryGrid] ❌ Error optimizing {dropdownName} dropdown usability: {e.Message}");
        }
    }

    private void SetupFilterContainer()
    {
        // หา filter container หรือสร้างใหม่
        Transform filterContainer = transform.parent?.Find("FilterContainer");

        if (filterContainer == null && categoryFilterDropdown != null)
        {
            // หา parent ของ dropdown แล้วเพิ่ม Vertical Layout Group
            Transform dropdownParent = categoryFilterDropdown.transform.parent;

            if (dropdownParent != null)
            {
                // เพิ่ม Vertical Layout Group ถ้ายังไม่มี
                VerticalLayoutGroup verticalLayout = dropdownParent.GetComponent<VerticalLayoutGroup>();
                if (verticalLayout == null)
                {
                    verticalLayout = dropdownParent.gameObject.AddComponent<VerticalLayoutGroup>();

                    // ตั้งค่า Vertical Layout Group
                    verticalLayout.spacing = 10f;
                    verticalLayout.childAlignment = TextAnchor.UpperCenter;
                    verticalLayout.childControlWidth = true;
                    verticalLayout.childControlHeight = false;
                    verticalLayout.childForceExpandWidth = true;
                    verticalLayout.childForceExpandHeight = false;

                    // เพิ่ม Content Size Fitter
                    ContentSizeFitter sizeFitter = dropdownParent.GetComponent<ContentSizeFitter>();
                    if (sizeFitter == null)
                    {
                        sizeFitter = dropdownParent.gameObject.AddComponent<ContentSizeFitter>();
                    }
                    sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                    Debug.Log("[InventoryGrid] ✅ Added Vertical Layout Group to filter container");
                }
            }
        }
    }
    // ✅ เพิ่ม Event Handlers
    private void OnCategoryFilterChanged(int filterIndex)
    {
        Debug.Log($"[InventoryGrid] Category filter changed to index: {filterIndex}");

        if (filterIndex == 0)
        {
            // "All Types" selected
            isFilteringByType = false;
            Debug.Log("[InventoryGrid] Showing all item types");
        }
        else
        {
            // Specific type selected
            isFilteringByType = true;
            selectedItemType = (ItemType)(filterIndex - 1);
            Debug.Log($"[InventoryGrid] Filtering by type: {selectedItemType}");
        }

        ApplyInventoryFilters();
    }

    private void OnTierFilterChanged(int filterIndex)
    {
        Debug.Log($"[InventoryGrid] Tier filter changed to index: {filterIndex}");

        if (filterIndex == 0)
        {
            // "All Tiers" selected
            isFilteringByTier = false;
            Debug.Log("[InventoryGrid] Showing all tiers");
        }
        else
        {
            // Specific tier selected
            isFilteringByTier = true;
            selectedItemTier = (ItemTier)filterIndex;
            Debug.Log($"[InventoryGrid] Filtering by tier: {selectedItemTier}");
        }

        ApplyInventoryFilters();
    }

    private void ClearAllFilters()
    {
        Debug.Log("[InventoryGrid] 🧹 Clearing all filters...");

        // Reset dropdowns
        if (categoryFilterDropdown != null)
        {
            categoryFilterDropdown.value = 0;
            categoryFilterDropdown.RefreshShownValue();
        }

        if (tierFilterDropdown != null)
        {
            tierFilterDropdown.value = 0;
            tierFilterDropdown.RefreshShownValue();
        }

        // Reset filter states
        isFilteringByType = false;
        isFilteringByTier = false;

        // Apply filters (จะแสดง items ทั้งหมด)
        ApplyInventoryFilters();
    }

    private void ApplyInventoryFilters()
    {
        if (ownerCharacter?.GetInventory() == null)
        {
            Debug.LogWarning("[InventoryGrid] No inventory to filter");
            return;
        }

        Debug.Log($"[InventoryGrid] 🔍 Applying filters - Type: {(isFilteringByType ? selectedItemType.ToString() : "All")}, Tier: {(isFilteringByTier ? selectedItemTier.ToString() : "All")}");

        Inventory inventory = ownerCharacter.GetInventory();
        int visibleItems = 0;
        int totalItems = 0;

        // ✅ วนลูปทุก slot แต่ไม่ซ่อน slot เอง
        for (int i = 0; i < allSlots.Count && i < inventory.CurrentSlots; i++)
        {
            InventorySlot slot = allSlots[i];
            InventoryItem item = inventory.GetItem(i);

            if (slot == null) continue;

            // ✅ แสดง slot เสมอ (ไม่ซ่อน)
            slot.gameObject.SetActive(true);

            bool shouldShowItem = ShouldShowItem(item);

            if (item != null && !item.IsEmpty)
            {
                totalItems++;

                if (shouldShowItem)
                {
                    // แสดง item ปกติ
                    visibleItems++;
                    SetSlotVisualState(slot, true);
                }
                else
                {
                    // ซ่อน item แต่แสดง slot แบบ dimmed
                    SetSlotVisualState(slot, false);
                }
            }
            else
            {
                // Empty slot - แสดงปกติเสมอ
                SetSlotVisualState(slot, true);
            }
        }

        // อัพเดท item count text
        UpdateFilteredItemCount(visibleItems, totalItems);

        Debug.Log($"[InventoryGrid] ✅ Filter applied - Visible: {visibleItems}/{totalItems} items");
    }
    private void SetSlotVisualState(InventorySlot slot, bool showItem)
    {
        if (slot == null) return;

        if (showItem)
        {
            // แสดง item ปกติ
            if (slot.itemIcon != null)
            {
                slot.itemIcon.color = Color.white;
            }
           
            if (slot.stackText != null)
            {
                slot.stackText.color = Color.white;
            }
        }
        else
        {
            // ทำให้ item มอง dimmed/gray
            if (slot.itemIcon != null)
            {
                slot.itemIcon.color = new Color(0.5f, 0.5f, 0.5f, 0.3f); // สีเทาจาง
            }
            
            if (slot.stackText != null)
            {
                slot.stackText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // ตัวเลขเทาจาง
            }
        }
    }
    // ✅ เพิ่ม method สำหรับตรวจสอบว่า item ควรแสดงหรือไม่
    private bool ShouldShowItem(InventoryItem item)
    {
        // ถ้า slot ว่าง - แสดงเสมอ
        if (item == null || item.IsEmpty)
        {
            return true; // ✅ เปลี่ยนให้แสดง empty slots เสมอ
        }

        // ตรวจสอบ type filter
        if (isFilteringByType && item.itemData.ItemType != selectedItemType)
        {
            return false;
        }

        // ตรวจสอบ tier filter
        if (isFilteringByTier && item.itemData.Tier != selectedItemTier)
        {
            return false;
        }

        return true;
    }
    private void UpdateFilteredItemCount(int visibleItems, int totalItems)
    {
        if (filteredItemCountText != null)
        {
            if (isFilteringByType || isFilteringByTier)
            {
                filteredItemCountText.text = $"Showing: {visibleItems}/{totalItems} items";
                filteredItemCountText.gameObject.SetActive(true);
            }
            else
            {
                filteredItemCountText.text = $"Total: {totalItems} items";
                filteredItemCountText.gameObject.SetActive(totalItems > 0);
            }
        }
    }
    // ✅ เพิ่ม method สำหรับอัพเดท item count
    private void UpdateFilteredItemCount(int visibleCount)
    {
        if (filteredItemCountText != null)
        {
            if (isFilteringByType || isFilteringByTier)
            {
                filteredItemCountText.text = $"Showing: {visibleCount} items";
                filteredItemCountText.gameObject.SetActive(true);
            }
            else
            {
                filteredItemCountText.gameObject.SetActive(false);
            }
        }
    }
    public void ResetFilters()
    {
        ClearAllFilters();
    }

    public bool HasActiveFilters()
    {
        return isFilteringByType || isFilteringByTier;
    }

    public string GetCurrentFilterStatus()
    {
        if (!HasActiveFilters())
        {
            return "No filters active";
        }

        List<string> activeFilters = new List<string>();

        if (isFilteringByType)
        {
            activeFilters.Add($"Type: {selectedItemType}");
        }

        if (isFilteringByTier)
        {
            activeFilters.Add($"Tier: {selectedItemTier}");
        }

        return "Filters: " + string.Join(", ", activeFilters);
    }
}