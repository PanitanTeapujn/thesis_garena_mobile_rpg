using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class InventoryGridManager : MonoBehaviour
{
    #region Grid Settings
    [Header("Grid Configuration")]
    public int gridWidth = 8;   // 8 คอลัมน์
    public int gridHeight = 6;  // 6 แถว
    public int totalSlots => gridWidth * gridHeight; // 48 slots total
    #endregion

    #region Prefab References
    [Header("Prefab References")]
    public GameObject inventorySlotPrefab;  // prefab ของ slot
    #endregion

    #region UI References
    [Header("UI References")]
    public Transform gridParent;            // parent object สำหรับ grid
    public GridLayoutGroup gridLayoutGroup; // grid layout component
    #endregion

    #region Grid Data
    [Header("Grid State")]
    public List<InventorySlot> allSlots = new List<InventorySlot>();
    public InventorySlot currentSelectedSlot = null;
    public int selectedSlotIndex = -1;
    #endregion

    #region Events
    public static event Action<InventorySlot> OnSlotSelectionChanged;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        InitializeGridParent();
        SetupGridLayout();
    }

    void Start()
    {
        CreateInventoryGrid();
        SubscribeToEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    #endregion

    #region Initialization
    void InitializeGridParent()
    {
        // หา grid parent ถ้าไม่ได้ assign
        if (gridParent == null)
        {
            GameObject gridObj = GameObject.Find("InventoryGrid");
            if (gridObj == null)
            {
                // สร้างใหม่ถ้าไม่มี
                gridObj = new GameObject("InventoryGrid");
                gridObj.transform.SetParent(transform);
            }
            gridParent = gridObj.transform;
        }

        // ตรวจสอบ GridLayoutGroup
        if (gridLayoutGroup == null)
        {
            gridLayoutGroup = gridParent.GetComponent<GridLayoutGroup>();
            if (gridLayoutGroup == null)
            {
                gridLayoutGroup = gridParent.gameObject.AddComponent<GridLayoutGroup>();
            }
        }
    }

    void SetupGridLayout()
    {
        if (gridLayoutGroup == null) return;

        // ตั้งค่า Grid Layout สำหรับ mobile
        gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayoutGroup.constraintCount = gridWidth; // 8 คอลัมน์

        // ขนาด cell (ปรับตามขนาดหน้าจอ mobile)
        gridLayoutGroup.cellSize = new Vector2(80f, 80f);

        // ระยะห่าง
        gridLayoutGroup.spacing = new Vector2(5f, 5f);

        // padding
        gridLayoutGroup.padding = new RectOffset(10, 18, 50, 10);

        // การจัดเรียง
        gridLayoutGroup.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayoutGroup.childAlignment = TextAnchor.UpperLeft;

        Debug.Log($"✅ Grid Layout setup complete: {gridWidth}x{gridHeight} = {totalSlots} slots");
    }

    void CreateInventoryGrid()
    {
        // เคลียร์ slots เก่า (ถ้ามี)
        ClearExistingSlots();

        // สร้าง slot prefab ถ้าไม่มี
        if (inventorySlotPrefab == null)
        {
            CreateSlotPrefab();
        }

        // สร้าง slots ทั้งหมด
        for (int i = 0; i < totalSlots; i++)
        {
            CreateSlot(i);
        }

        Debug.Log($"📦 Created {allSlots.Count} inventory slots");
    }

    void CreateSlotPrefab()
    {
        // สร้าง prefab แบบง่ายๆ ถ้าไม่มี
        GameObject slotObj = new GameObject("InventorySlot");

        // เพิ่ม RectTransform
        RectTransform rectTransform = slotObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(80f, 80f);

        // เพิ่ม Image component (background)
        Image backgroundImage = slotObj.AddComponent<Image>();
        backgroundImage.color = new Color(0.3f, 0.3f, 0.3f, 1f); // สีเทา

        // เพิ่ม InventorySlot script
        InventorySlot slotScript = slotObj.AddComponent<InventorySlot>();
        slotScript.backgroundImage = backgroundImage;

        // สร้างเป็น prefab ใน memory
        inventorySlotPrefab = slotObj;

        Debug.Log("🔧 Created slot prefab automatically");
    }

    void CreateSlot(int index)
    {
        // สร้าง slot instance
        GameObject slotInstance = Instantiate(inventorySlotPrefab, gridParent);

        // ตั้งค่า slot
        InventorySlot slot = slotInstance.GetComponent<InventorySlot>();
        if (slot != null)
        {
            slot.SetSlotIndex(index);
            allSlots.Add(slot);
        }
        else
        {
            Debug.LogError($"❌ Slot at index {index} doesn't have InventorySlot component!");
        }
    }

    void ClearExistingSlots()
    {
        // ลบ slots เก่าออกจาก UI
        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(gridParent.GetChild(i).gameObject);
        }

        // เคลียร์ list
        allSlots.Clear();
        currentSelectedSlot = null;
        selectedSlotIndex = -1;
    }
    #endregion

    #region Event Management
    void SubscribeToEvents()
    {
        InventorySlot.OnSlotSelected += HandleSlotSelected;
    }

    void UnsubscribeFromEvents()
    {
        InventorySlot.OnSlotSelected -= HandleSlotSelected;
    }

    void HandleSlotSelected(InventorySlot selectedSlot)
    {
        // ยกเลิกการเลือก slot เก่า
        if (currentSelectedSlot != null)
        {
            currentSelectedSlot.SetSelectedState(false);
        }

        // เลือก slot ใหม่
        currentSelectedSlot = selectedSlot;
        selectedSlotIndex = selectedSlot.slotIndex;
        currentSelectedSlot.SetSelectedState(true);

        // แจ้งระบบอื่นๆ
        OnSlotSelectionChanged?.Invoke(selectedSlot);

        Debug.Log($"🎯 Selected slot {selectedSlotIndex}");
    }
    #endregion

    #region Public Methods for Testing (Step 1)
    [ContextMenu("Test - Fill Random Slots")]
    public void TestFillRandomSlots()
    {
        // เติม items สุ่มใน 10 slots สำหรับทดสอบ
        for (int i = 0; i < 10; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, totalSlots);
            if (allSlots[randomIndex].isEmpty)
            {
                allSlots[randomIndex].SetTestItem();
            }
        }

        Debug.Log("🎲 Filled random slots for testing");
    }

    [ContextMenu("Test - Clear All Slots")]
    public void TestClearAllSlots()
    {
        foreach (var slot in allSlots)
        {
            slot.SetEmptyState();
        }

        currentSelectedSlot = null;
        selectedSlotIndex = -1;

        Debug.Log("🧹 Cleared all inventory slots");
    }

    [ContextMenu("Test - Fill All Slots")]
    public void TestFillAllSlots()
    {
        foreach (var slot in allSlots)
        {
            slot.SetTestItem();
        }

        Debug.Log("📦 Filled all inventory slots");
    }
    #endregion

    #region Query Methods
    public InventorySlot GetSlot(int index)
    {
        if (index >= 0 && index < allSlots.Count)
        {
            return allSlots[index];
        }
        return null;
    }

    public InventorySlot GetSelectedSlot()
    {
        return currentSelectedSlot;
    }

    public int GetSelectedSlotIndex()
    {
        return selectedSlotIndex;
    }

    public List<InventorySlot> GetEmptySlots()
    {
        List<InventorySlot> emptySlots = new List<InventorySlot>();
        foreach (var slot in allSlots)
        {
            if (slot.isEmpty)
            {
                emptySlots.Add(slot);
            }
        }
        return emptySlots;
    }

    public List<InventorySlot> GetFilledSlots()
    {
        List<InventorySlot> filledSlots = new List<InventorySlot>();
        foreach (var slot in allSlots)
        {
            if (!slot.isEmpty)
            {
                filledSlots.Add(slot);
            }
        }
        return filledSlots;
    }

    public int GetEmptySlotCount()
    {
        return GetEmptySlots().Count;
    }

    public int GetFilledSlotCount()
    {
        return GetFilledSlots().Count;
    }
    #endregion

    #region Auto-Setup for Scene
    [ContextMenu("Auto Setup Grid in Scene")]
    public void AutoSetupGridInScene()
    {
        // หา Canvas ในฉาก
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("❌ No Canvas found in scene!");
            return;
        }

        // สร้าง UI structure
        GameObject inventoryPanel = new GameObject("InventoryPanel");
        inventoryPanel.transform.SetParent(canvas.transform);

        RectTransform panelRect = inventoryPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.offsetMin = new Vector2(0, 0);
        panelRect.offsetMax = new Vector2(-50, -50);

        // เพิ่ม background
        Image panelBg = inventoryPanel.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        // ตั้งให้ grid parent เป็น panel นี้
        gridParent = inventoryPanel.transform;
        InitializeGridParent();
        SetupGridLayout();
        CreateInventoryGrid();

        Debug.Log("✅ Auto-setup complete! Inventory grid created in scene.");
    }
    #endregion
}