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
        CreateInventoryGrid();
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
        // ลบ slots เก่าถ้ามี
        ClearExistingSlots();

        // คำนวณจำนวน slots
        totalSlots = gridWidth * gridHeight;

        // สร้าง slots ใหม่
        for (int i = 0; i < totalSlots; i++)
        {
            CreateSlot(i);
        }

        Debug.Log($"[InventoryGrid] Created {allSlots.Count} inventory slots");
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

        allSlots.Add(slot);
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

        Debug.Log($"[InventoryGrid] Created slot from scratch");
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

    #region Public Methods for Testing
    [ContextMenu("Test: Create Grid")]
    private void TestCreateGrid()
    {
        CreateInventoryGrid();
    }

    [ContextMenu("Test: Fill Random Slots")]
    private void TestFillRandomSlots()
    {
        // สร้าง test sprite
        Texture2D testTexture = new Texture2D(64, 64);
        Color[] colors = new Color[64 * 64];

        for (int i = 0; i < 5; i++) // เติม 5 slots แบบสุ่ม
        {
            int randomSlot = Random.Range(0, allSlots.Count);

            // สุ่มสี
            Color randomColor = new Color(Random.value, Random.value, Random.value, 1f);
            for (int j = 0; j < colors.Length; j++)
                colors[j] = randomColor;

            testTexture.SetPixels(colors);
            testTexture.Apply();

            Sprite testSprite = Sprite.Create(testTexture, new Rect(0, 0, 64, 64), Vector2.one * 0.5f);
            testSprite.name = $"TestItem_{i}";

            allSlots[randomSlot].SetFilledState(testSprite);
        }

        Debug.Log("[InventoryGrid] Filled 5 random slots with test items");
    }

    [ContextMenu("Test: Clear All Slots")]
    private void TestClearAllSlots()
    {
        foreach (InventorySlot slot in allSlots)
        {
            slot.SetEmptyState();
        }
        DeselectAllSlots();
        Debug.Log("[InventoryGrid] Cleared all slots");
    }

    [ContextMenu("Test: Auto-Fit Grid")]
    private void TestAutoFitGrid()
    {
        autoFitToParent = true;
        RefreshGridLayout();
        Debug.Log($"[InventoryGrid] Auto-fit applied - New cell size: {cellSize}");
    }

    [ContextMenu("Test: Set Small Cells (40px)")]
    private void TestSetSmallCells()
    {
        autoFitToParent = false;
        cellSize = 40f;
        spacing = 2f;
        RefreshGridLayout();
    }

    [ContextMenu("Test: Set Medium Cells (60px)")]
    private void TestSetMediumCells()
    {
        autoFitToParent = false;
        cellSize = 60f;
        spacing = 3f;
        RefreshGridLayout();
    }

    [ContextMenu("Test: Set Large Cells (80px)")]
    private void TestSetLargeCells()
    {
        autoFitToParent = false;
        cellSize = 80f;
        spacing = 5f;
        RefreshGridLayout();
    }

    [ContextMenu("Test: Show Grid Info")]
    private void TestShowGridInfo()
    {
        Debug.Log("=== INVENTORY GRID INFO ===");
        Debug.Log($"Grid Size: {gridWidth}x{gridHeight}");
        Debug.Log($"Total Slots: {totalSlots}");
        Debug.Log($"Created Slots: {allSlots.Count}");
        Debug.Log($"Selected Slot: {selectedSlotIndex}");
        Debug.Log($"Cell Size: {cellSize}x{cellSize}");
        Debug.Log($"Spacing: {spacing}");
        Debug.Log($"Auto Fit: {autoFitToParent}");

        int filledSlots = 0;
        foreach (InventorySlot slot in allSlots)
        {
            if (!slot.IsEmpty) filledSlots++;
        }
        Debug.Log($"Filled Slots: {filledSlots}");
        Debug.Log($"Empty Slots: {allSlots.Count - filledSlots}");

        // แสดงขนาด parent
        RectTransform parentRect = GetComponentInParent<RectTransform>();
        if (parentRect != null)
        {
            Debug.Log($"Parent Panel Size: {parentRect.rect.size}");
        }

        Debug.Log("===========================");
    }
    #endregion

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
}