using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[System.Serializable]
public class SlotDebugInfo
{
    public int slotIndex;
    public bool hasButton;
    public bool buttonInteractable;
    public bool backgroundRaycastTarget;
    public bool iconRaycastTarget;
    public bool isGameObjectActive;
    public Vector3 worldPosition;
    public Vector2 sizeDelta;
    public List<string> blockingElements = new List<string>();
}

public class InventorySlotDebugger : MonoBehaviour
{
    [Header("Debug Results")]
    [SerializeField] private List<SlotDebugInfo> debugInfos = new List<SlotDebugInfo>();

    [Header("Auto Debug Settings")]
    [SerializeField] private bool autoDebugOnAwake = true;
    [SerializeField] private bool showDetailedLog = true;

    private InventoryGridManager gridManager;

    private void Awake()
    {
        if (autoDebugOnAwake)
        {
            StartCoroutine(DelayedDebug());
        }
    }

    private System.Collections.IEnumerator DelayedDebug()
    {
        yield return new WaitForSeconds(1f); // รอให้ grid setup เสร็จ
        PerformDetailedSlotDebug();
    }

    [ContextMenu("🔍 Debug All Slots")]
    public void PerformDetailedSlotDebug()
    {
        gridManager = FindObjectOfType<InventoryGridManager>();
        if (gridManager == null)
        {
            Debug.LogError("[SlotDebugger] No InventoryGridManager found!");
            return;
        }

        debugInfos.Clear();

        Debug.Log("=== INVENTORY SLOT CLICK DEBUG ===");

        for (int i = 0; i < gridManager.AllSlots.Count && i < 10; i++) // ตรวจ 10 slot แรก
        {
            InventorySlot slot = gridManager.AllSlots[i];
            SlotDebugInfo info = AnalyzeSlot(slot, i);
            debugInfos.Add(info);

            if (showDetailedLog)
            {
                LogSlotInfo(info);
            }
        }

        // สรุปปัญหา
        SummarizeIssues();
    }

    private SlotDebugInfo AnalyzeSlot(InventorySlot slot, int index)
    {
        SlotDebugInfo info = new SlotDebugInfo
        {
            slotIndex = index
        };

        if (slot == null)
        {
            Debug.LogError($"[SlotDebugger] Slot {index} is NULL!");
            return info;
        }

        // ตรวจสอบ GameObject
        info.isGameObjectActive = slot.gameObject.activeSelf;

        // ตรวจสอบ Button Component
        info.hasButton = slot.slotButton != null;
        if (info.hasButton)
        {
            info.buttonInteractable = slot.slotButton.interactable;
        }

        // ตรวจสอบ Raycast Target
        if (slot.slotBackground != null)
        {
            info.backgroundRaycastTarget = slot.slotBackground.raycastTarget;
        }

        if (slot.itemIcon != null)
        {
            info.iconRaycastTarget = slot.itemIcon.raycastTarget;
        }

        // ตรวจสอบ Position และ Size
        RectTransform rectTransform = slot.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            info.worldPosition = rectTransform.position;
            info.sizeDelta = rectTransform.sizeDelta;
        }

        // ตรวจสอบ Blocking Elements
        CheckForBlockingElements(slot, info);

        return info;
    }

    private void CheckForBlockingElements(InventorySlot slot, SlotDebugInfo info)
    {
        // ใช้ Raycast เพื่อตรวจสอบว่ามี element อื่นบัง slot หรือไม่
        Canvas parentCanvas = slot.GetComponentInParent<Canvas>();
        if (parentCanvas == null) return;

        GraphicRaycaster raycaster = parentCanvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null) return;

        // สร้าง PointerEventData สำหรับตรวจสอบ
        PointerEventData eventData = new PointerEventData(EventSystem.current);

        // ใช้ตำแหน่งกลาง slot
        RectTransform slotRect = slot.GetComponent<RectTransform>();
        Vector3 worldPos = slotRect.position;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(parentCanvas.worldCamera, worldPos);

        eventData.position = screenPos;

        // ทำ Raycast
        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(eventData, results);

        // ตรวจสอบผลลัพธ์
        bool foundSlotButton = false;
        foreach (RaycastResult result in results)
        {
            if (result.gameObject == slot.slotButton?.gameObject)
            {
                foundSlotButton = true;
                break;
            }
            else if (result.gameObject != slot.gameObject &&
                     result.gameObject != slot.slotBackground?.gameObject)
            {
                // พบ element ที่บัง
                info.blockingElements.Add($"{result.gameObject.name} ({result.gameObject.GetType().Name})");
            }
        }

        if (!foundSlotButton && info.hasButton)
        {
            info.blockingElements.Add("⚠️ SLOT BUTTON NOT REACHABLE BY RAYCAST!");
        }
    }

    private void LogSlotInfo(SlotDebugInfo info)
    {
        string status = "✅";
        if (!info.hasButton || !info.buttonInteractable || !info.backgroundRaycastTarget || !info.isGameObjectActive)
        {
            status = "❌";
        }
        else if (info.blockingElements.Count > 0)
        {
            status = "⚠️";
        }

        Debug.Log($"{status} SLOT {info.slotIndex}:");
        Debug.Log($"  Active: {info.isGameObjectActive}");
        Debug.Log($"  Button: {info.hasButton} (Interactable: {info.buttonInteractable})");
        Debug.Log($"  Background Raycast: {info.backgroundRaycastTarget}");
        Debug.Log($"  Icon Raycast: {info.iconRaycastTarget}");
        Debug.Log($"  Position: {info.worldPosition}");
        Debug.Log($"  Size: {info.sizeDelta}");

        if (info.blockingElements.Count > 0)
        {
            Debug.Log($"  🚫 Blocking Elements: {string.Join(", ", info.blockingElements)}");
        }

        Debug.Log(""); // บรรทัดว่าง
    }

    private void SummarizeIssues()
    {
        Debug.Log("=== SUMMARY ===");

        List<int> problematicSlots = new List<int>();
        List<int> blockedSlots = new List<int>();

        foreach (SlotDebugInfo info in debugInfos)
        {
            bool hasIssue = !info.hasButton || !info.buttonInteractable ||
                           !info.backgroundRaycastTarget || !info.isGameObjectActive;

            if (hasIssue)
            {
                problematicSlots.Add(info.slotIndex);
            }

            if (info.blockingElements.Count > 0)
            {
                blockedSlots.Add(info.slotIndex);
            }
        }

        if (problematicSlots.Count > 0)
        {
            Debug.LogError($"❌ Slots with Component Issues: {string.Join(", ", problematicSlots)}");
        }

        if (blockedSlots.Count > 0)
        {
            Debug.LogWarning($"⚠️ Slots with Blocking Elements: {string.Join(", ", blockedSlots)}");
        }

        if (problematicSlots.Count == 0 && blockedSlots.Count == 0)
        {
            Debug.Log("✅ All tested slots appear to be working correctly!");
        }

        // เพิ่มคำแนะนำ
        Debug.Log("\n=== RECOMMENDATIONS ===");
        if (blockedSlots.Count > 0)
        {
            Debug.Log("🔧 Try the following fixes:");
            Debug.Log("  1. Check if there's an invisible UI element blocking slots 0-3");
            Debug.Log("  2. Verify Canvas render order and sorting layers");
            Debug.Log("  3. Check if ItemDetailPanel or other panels are blocking the slots");
            Debug.Log("  4. Try calling Canvas.ForceUpdateCanvases() after grid creation");
        }
    }

    [ContextMenu("🎯 Test Click Simulation")]
    public void SimulateClickOnProblematicSlots()
    {
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<InventoryGridManager>();
        }

        Debug.Log("=== CLICK SIMULATION TEST ===");

        for (int i = 0; i < 5; i++) // ทดสอบ slot 0-4
        {
            if (i < gridManager.AllSlots.Count)
            {
                InventorySlot slot = gridManager.AllSlots[i];

                Debug.Log($"🖱️ Simulating click on slot {i}...");

                if (slot.slotButton != null)
                {
                    // จำลองการกดปุ่ม
                    slot.slotButton.onClick.Invoke();
                    Debug.Log($"  ✅ Button click invoked for slot {i}");
                }
                else
                {
                    Debug.LogError($"  ❌ No button found for slot {i}");
                }
            }
        }
    }

    [ContextMenu("🔧 Quick Fix: Force Refresh Raycast")]
    public void ForceRefreshRaycast()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.enabled = false;
                raycaster.enabled = true;
            }
        }

        Canvas.ForceUpdateCanvases();
        Debug.Log("🔧 Forced raycast refresh on all canvases");
    }

    [ContextMenu("🔍 Check Event System")]
    public void CheckEventSystem()
    {
        EventSystem eventSystem = FindObjectOfType<EventSystem>();

        Debug.Log("=== EVENT SYSTEM CHECK ===");
        Debug.Log($"EventSystem exists: {eventSystem != null}");

        if (eventSystem != null)
        {
            Debug.Log($"EventSystem enabled: {eventSystem.enabled}");
            Debug.Log($"Current selected object: {eventSystem.currentSelectedGameObject?.name ?? "None"}");

            StandaloneInputModule inputModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (inputModule != null)
            {
                Debug.Log($"Input Module enabled: {inputModule.enabled}");
            }
        }
        else
        {
            Debug.LogError("❌ No EventSystem found! This is required for UI interactions.");
        }
    }
}