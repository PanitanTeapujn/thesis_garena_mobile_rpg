using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        // ✅ แทนที่จะ force activate panel ให้ใช้วิธีที่ clean กว่า
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

        // ✅ ใช้ Canvas.ForceUpdateCanvases แทน panel activation
        if (needsCanvasRefresh)
        {
            StartCoroutine(ForceCanvasRefreshRoutine());
        }

        // โหลด items จาก character inventory
        LoadItemsFromCharacterInventory();

        Debug.Log($"[InventoryGrid] Created {allSlots.Count} inventory slots");
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
        slot.SetEmptyState();

        allSlots.Add(slot);

        // ✅ เพิ่มการ ensure ว่า slot พร้อมใช้งาน
        EnsureSlotIsReady(slot);

        Debug.Log($"[InventoryGrid] Created slot {slotIndex} with components ready");
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

            Debug.Log($"[InventoryGrid] Successfully connected to {ownerCharacter.CharacterName}'s inventory");
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

        for (int i = 0; i < totalSlots && i < characterInventory.CurrentSlots; i++)
        {
            InventoryItem item = characterInventory.GetItem(i);
            UpdateSlotFromInventoryItem(i, item);
        }
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
            Debug.LogError($"[InventoryGrid] Slot {slotIndex} is null!");
            return;
        }

        Debug.Log($"[InventoryGrid] Updating slot {slotIndex}, Current isEmpty: {slot.IsEmpty}");

        if (item == null || item.IsEmpty)
        {
            slot.SetEmptyState();
            Debug.Log($"[InventoryGrid] Set slot {slotIndex} to empty");
        }
        else
        {
            // ใช้ icon จาก ItemData และแสดง stack count
            Sprite itemIcon = item.itemData.ItemIcon;
            int stackCount = item.stackCount;

            if (itemIcon == null)
            {
                Debug.LogError($"[InventoryGrid] Item {item.itemData.ItemName} has null icon!");
                return;
            }

            Debug.Log($"[InventoryGrid] Setting slot {slotIndex} with item: {item.itemData.ItemName} x{stackCount}");

            // แสดง stack count เฉพาะตอนที่ item สามารถ stack ได้ และมีมากกว่า 1
            bool showStackCount = item.itemData.CanStack() && stackCount > 1;

            slot.SetFilledState(itemIcon, showStackCount ? stackCount : 0);

            // เพิ่ม tier color
            Color tierColor = item.itemData.GetTierColor();
            slot.SetRarityColor(tierColor);

            Debug.Log($"[InventoryGrid] Updated slot {slotIndex}: {item.itemData.ItemName} x{stackCount}, isEmpty after update: {slot.IsEmpty}");
        }

        // ✅ Force refresh canvas หลังจาก update
        StartCoroutine(DelayedCanvasRefresh());
    }
    private IEnumerator DelayedCanvasRefresh()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
    }

    public void ForceSyncAllSlots()
    {
        Debug.Log("[InventoryGrid] Force syncing all slots...");

        if (ownerCharacter?.GetInventory() == null) return;

        Inventory inventory = ownerCharacter.GetInventory();

        for (int i = 0; i < allSlots.Count && i < inventory.CurrentSlots; i++)
        {
            InventoryItem item = inventory.GetItem(i);
            InventorySlot slot = allSlots[i];

            if (slot != null)
            {
                Debug.Log($"[InventoryGrid] Syncing slot {i}: {(item?.IsEmpty != false ? "Empty" : item.itemData.ItemName)}");
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
            Debug.LogWarning($"[InventoryGrid] No character or inventory to update slot {slotIndex}");
            return;
        }

        if (slotIndex < 0 || slotIndex >= allSlots.Count)
        {
            Debug.LogWarning($"[InventoryGrid] Slot index {slotIndex} out of range");
            return;
        }

        Inventory inventory = ownerCharacter.GetInventory();
        InventoryItem item = inventory.GetItem(slotIndex);

        UpdateSlotFromInventoryItem(slotIndex, item);

        Debug.Log($"[InventoryGrid] Updated slot {slotIndex} from character inventory");
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
    [ContextMenu("🔄 Force Sync All Slots")]
    private void TestForceSyncAllSlots()
    {
        ForceSyncAllSlots();
    }

    [ContextMenu("📋 Compare Slot vs Inventory")]
    private void CompareSlotVsInventory()
    {
        // ลองหา character อีกครั้งถ้ายังไม่มี
        if (ownerCharacter == null)
        {
            ownerCharacter = FindCharacterMultipleWays();
        }

        if (ownerCharacter?.GetInventory() == null)
        {
            Debug.LogWarning($"[InventoryGrid] No character ({ownerCharacter?.CharacterName ?? "null"}) or inventory found");

            // แสดงข้อมูล debug
            Debug.Log("=== CHARACTER DEBUG INFO ===");
            Character[] allCharacters = FindObjectsOfType<Character>();
            Debug.Log($"Total Characters found: {allCharacters.Length}");

            foreach (Character character in allCharacters)
            {
                Inventory inv = character.GetInventory();
                Debug.Log($"  - {character.CharacterName}: HasInventory={inv != null}, Items={inv?.UsedSlots ?? 0}");
            }

            Hero[] allHeroes = FindObjectsOfType<Hero>();
            Debug.Log($"Total Heroes found: {allHeroes.Length}");

            foreach (Hero hero in allHeroes)
            {
                Debug.Log($"  - {hero.CharacterName}: HasInputAuthority={hero.HasInputAuthority}, IsSpawned={hero.IsSpawned}");
            }

            return;
        }

        Inventory inventory = ownerCharacter.GetInventory();
        Debug.Log($"=== SLOT vs INVENTORY COMPARISON ({ownerCharacter.CharacterName}) ===");

        for (int i = 0; i < Mathf.Min(allSlots.Count, inventory.CurrentSlots); i++)
        {
            InventorySlot slot = allSlots[i];
            InventoryItem item = inventory.GetItem(i);

            string slotState = slot?.IsEmpty == true ? "Empty" : "Filled";
            string invState = item?.IsEmpty != false ? "Empty" : $"{item.itemData.ItemName} x{item.stackCount}";

            bool matches = (slot?.IsEmpty == true) == (item?.IsEmpty != false);
            string status = matches ? "✅ MATCH" : "❌ MISMATCH";

            Debug.Log($"Slot {i}: {status} - Slot={slotState}, Inventory={invState}");
        }
    }
    [ContextMenu("🔗 Debug Character Connection")]
    private void DebugCharacterConnection()
    {
        Debug.Log("=== CHARACTER CONNECTION DEBUG ===");
        Debug.Log($"Current Owner Character: {ownerCharacter?.CharacterName ?? "None"}");
        Debug.Log($"Auto Detect Character: {autoDetectCharacter}");
        Debug.Log($"All Slots Count: {allSlots.Count}");

        // ลองหา character ด้วยวิธีต่างๆ
        Debug.Log("\n--- Finding Characters ---");

        // 1. จาก parent
        Character parentChar = GetComponentInParent<Character>();
        Debug.Log($"From Parent: {parentChar?.CharacterName ?? "None"}");

        // 2. จาก UI Manager
        CombatUIManager uiManager = FindObjectOfType<CombatUIManager>();
        Debug.Log($"UI Manager found: {uiManager != null}");
        Debug.Log($"UI Manager Local Hero: {uiManager?.localHero?.CharacterName ?? "None"}");

        // 3. จาก Heroes
        Hero[] heroes = FindObjectsOfType<Hero>();
        Debug.Log($"Total Heroes: {heroes.Length}");
        foreach (Hero hero in heroes)
        {
            Debug.Log($"  Hero: {hero.CharacterName}, HasInput: {hero.HasInputAuthority}, Spawned: {hero.IsSpawned}, HasInventory: {hero.GetInventory() != null}");
        }

        // 4. จาก Characters
        Character[] characters = FindObjectsOfType<Character>();
        Debug.Log($"Total Characters: {characters.Length}");
        foreach (Character character in characters)
        {
            Debug.Log($"  Character: {character.CharacterName}, HasInventory: {character.GetInventory() != null}");
        }
    }

    [ContextMenu("🔧 Force Find and Connect Character")]
    private void ForceFindAndConnectCharacter()
    {
        Debug.Log("[InventoryGrid] Force finding and connecting character...");

        Character foundCharacter = FindCharacterMultipleWays();

        if (foundCharacter != null)
        {
            SetOwnerCharacter(foundCharacter);
            Debug.Log($"✅ Successfully connected to: {foundCharacter.CharacterName}");
        }
        else
        {
            Debug.LogError("❌ No character found to connect!");
        }
    }

    [ContextMenu("🏗️ Force Recreate Grid")]
    private void ForceRecreateGrid()
    {
        Debug.Log("[InventoryGrid] Force recreating grid...");

        ClearExistingSlots();
        CreateInventoryGrid();

        Debug.Log($"✅ Grid recreated with {allSlots.Count} slots");
    }

    [ContextMenu("🏗️ Test: Force Create Slots Now")]
    private void TestForceCreateSlotsNow()
    {
        Debug.Log("[InventoryGrid] Testing force create slots...");

        // หา inventory panel
        GameObject panel = GetInventoryPanel();
        bool wasActive = panel != null ? panel.activeSelf : true;

        Debug.Log($"Inventory Panel: {(panel != null ? "Found" : "Not Found")}, Active: {wasActive}");

        if (panel != null && !wasActive)
        {
            panel.SetActive(true);
            Debug.Log("Force activated panel for testing");
        }

        // ลบ slots เก่า
        ClearExistingSlots();

        // สร้าง slots ใหม่
        int slotsToCreate = ownerCharacter != null ? ownerCharacter.GetInventorySlotCount() : 24;
        for (int i = 0; i < slotsToCreate; i++)
        {
            CreateSlot(i);
        }

        Debug.Log($"Created {allSlots.Count} slots");

        // คืนสถานะ panel
        if (panel != null && !wasActive)
        {
            StartCoroutine(DelayedDeactivatePanel(panel));
        }
    }

    [ContextMenu("👁️ Test: Check Slots Existence")]
    private void TestCheckSlotsExistence()
    {
        Debug.Log($"=== SLOTS EXISTENCE CHECK ===");
        Debug.Log($"Total Slots: {allSlots.Count}");
        Debug.Log($"Owner Character: {ownerCharacter?.CharacterName ?? "None"}");

        GameObject panel = GetInventoryPanel();
        Debug.Log($"Inventory Panel: {(panel != null ? panel.name : "Not Found")}");
        Debug.Log($"Panel Active: {panel?.activeSelf ?? false}");

        for (int i = 0; i < Mathf.Min(5, allSlots.Count); i++)
        {
            InventorySlot slot = allSlots[i];
            if (slot != null)
            {
                Debug.Log($"Slot {i}: GameObject={slot.gameObject.name}, Active={slot.gameObject.activeSelf}, Empty={slot.IsEmpty}");
            }
            else
            {
                Debug.Log($"Slot {i}: NULL");
            }
        }
    }
}