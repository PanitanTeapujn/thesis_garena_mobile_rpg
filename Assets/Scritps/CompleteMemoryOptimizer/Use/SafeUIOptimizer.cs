using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SafeUIOptimizer : MonoBehaviour
{
    [Header("Safe UI Optimization")]
    public bool enableOptimization = true;
    public bool onlyAnalyze = true; // เริ่มต้นแค่วิเคราะห์ ไม่ทำลาย UI

    [Header("Analysis Only (Safe Mode)")]
    public bool showDetailedReport = true;

    void Start()
    {
        if (enableOptimization)
        {
            AnalyzeUIMemory();
        }
    }

    public void AnalyzeUIMemory()
    {
        Debug.Log("🔍 Analyzing UI Memory Usage (Safe Mode)...");

        // นับ UI Components
        Text[] allTexts = FindObjectsOfType<Text>(true);
        Image[] allImages = FindObjectsOfType<Image>(true);
        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);
        Button[] allButtons = FindObjectsOfType<Button>(true);

        // แยกระหว่าง Active และ Inactive
        int activeTexts = 0, inactiveTexts = 0;
        int activeImages = 0, inactiveImages = 0;
        int activeCanvases = 0, inactiveCanvases = 0;

        // วิเคราะห์ Text
        List<string> emptyTexts = new List<string>();
        List<string> duplicateTexts = new List<string>();
        Dictionary<string, int> textContent = new Dictionary<string, int>();

        foreach (Text text in allTexts)
        {
            if (text == null) continue;

            if (text.gameObject.activeInHierarchy)
                activeTexts++;
            else
                inactiveTexts++;

            // ตรวจสอบ Text ที่ว่าง
            if (string.IsNullOrEmpty(text.text) || text.text.Trim() == "")
            {
                emptyTexts.Add(text.gameObject.name);
            }

            // ตรวจสอบ Text ที่ซ้ำ
            if (textContent.ContainsKey(text.text))
            {
                textContent[text.text]++;
                if (!duplicateTexts.Contains(text.text))
                    duplicateTexts.Add(text.text);
            }
            else
            {
                textContent[text.text] = 1;
            }
        }

        // วิเคราะห์ Image
        List<string> noSpriteImages = new List<string>();
        List<string> invisibleImages = new List<string>();

        foreach (Image image in allImages)
        {
            if (image == null) continue;

            if (image.gameObject.activeInHierarchy)
                activeImages++;
            else
                inactiveImages++;

            // ตรวจสอบ Image ที่ไม่มี sprite
            if (image.sprite == null)
            {
                noSpriteImages.Add(image.gameObject.name);
            }

            // ตรวจสอบ Image ที่โปร่งใส
            if (image.color.a < 0.01f)
            {
                invisibleImages.Add(image.gameObject.name);
            }
        }

        // วิเคราะห์ Canvas
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas == null) continue;

            if (canvas.gameObject.activeInHierarchy)
                activeCanvases++;
            else
                inactiveCanvases++;
        }

        // แสดงผลการวิเคราะห์
        Debug.Log("📊 UI Analysis Report:");
        Debug.Log($"📝 Text Components: {activeTexts} active / {allTexts.Length} total");
        Debug.Log($"🖼️ Image Components: {activeImages} active / {allImages.Length} total");
        Debug.Log($"🎨 Canvas Components: {activeCanvases} active / {allCanvases.Length} total");
        Debug.Log($"🔘 Button Components: {allButtons.Length} total");

        // แสดงปัญหาที่พบ
        Debug.Log("⚠️ Issues Found:");
        Debug.Log($"   Empty Text objects: {emptyTexts.Count}");
        Debug.Log($"   Duplicate Text content: {duplicateTexts.Count}");
        Debug.Log($"   Images without sprite: {noSpriteImages.Count}");
        Debug.Log($"   Invisible Images: {invisibleImages.Count}");

        if (showDetailedReport)
        {
            ShowDetailedReport(emptyTexts, duplicateTexts, noSpriteImages, invisibleImages);
        }

        // แนะนำการแก้ไข
        SuggestOptimizations(emptyTexts.Count, duplicateTexts.Count, noSpriteImages.Count, invisibleImages.Count);

        if (!onlyAnalyze)
        {
            Debug.LogWarning("⚠️ onlyAnalyze is OFF! Will perform actual optimization.");
            Debug.LogWarning("⚠️ Make sure to backup your scene first!");
        }
        else
        {
            Debug.Log("✅ Analysis complete. No changes made (Safe Mode).");
        }
    }

    private void ShowDetailedReport(List<string> emptyTexts, List<string> duplicateTexts,
                                   List<string> noSpriteImages, List<string> invisibleImages)
    {
        Debug.Log("📋 Detailed Report:");

        if (emptyTexts.Count > 0 && emptyTexts.Count <= 10)
        {
            Debug.Log("📝 Empty Text Objects:");
            foreach (string name in emptyTexts)
            {
                Debug.Log($"   - {name}");
            }
        }
        else if (emptyTexts.Count > 10)
        {
            Debug.Log($"📝 Empty Text Objects: {emptyTexts.Count} (too many to list)");
        }

        if (duplicateTexts.Count > 0 && duplicateTexts.Count <= 10)
        {
            Debug.Log("🔄 Duplicate Text Content:");
            foreach (string text in duplicateTexts)
            {
                Debug.Log($"   - \"{text}\"");
            }
        }

        if (noSpriteImages.Count > 0 && noSpriteImages.Count <= 10)
        {
            Debug.Log("🖼️ Images without Sprite:");
            foreach (string name in noSpriteImages)
            {
                Debug.Log($"   - {name}");
            }
        }

        if (invisibleImages.Count > 0 && invisibleImages.Count <= 10)
        {
            Debug.Log("👻 Invisible Images:");
            foreach (string name in invisibleImages)
            {
                Debug.Log($"   - {name}");
            }
        }
    }

    private void SuggestOptimizations(int emptyTexts, int duplicateTexts, int noSpriteImages, int invisibleImages)
    {
        int totalIssues = emptyTexts + duplicateTexts + noSpriteImages + invisibleImages;

        Debug.Log("💡 Optimization Suggestions:");

        if (totalIssues == 0)
        {
            Debug.Log("✅ Great! No major UI issues found.");
            return;
        }

        if (emptyTexts > 0)
        {
            Debug.Log($"📝 Consider removing {emptyTexts} empty Text objects");
        }

        if (duplicateTexts > 0)
        {
            Debug.Log($"🔄 Consider consolidating {duplicateTexts} duplicate Text contents");
        }

        if (noSpriteImages > 0)
        {
            Debug.Log($"🖼️ Consider removing {noSpriteImages} Images without sprites");
        }

        if (invisibleImages > 0)
        {
            Debug.Log($"👻 Consider removing {invisibleImages} invisible Images");
        }

        Debug.Log($"📊 Total potential memory savings: ~{totalIssues * 2} MB");

        if (totalIssues > 20)
        {
            Debug.LogWarning("🚨 Many UI issues found! Consider manual cleanup.");
        }

        Debug.Log("💡 To enable safe optimization, set 'onlyAnalyze' to false");
        Debug.Log("💡 But make sure to backup your scene first!");
    }

    [ContextMenu("Analyze UI Memory")]
    public void ForceAnalyze()
    {
        AnalyzeUIMemory();
    }

    [ContextMenu("Safe Optimization (Backup Scene First!)")]
    public void SafeOptimization()
    {
        if (onlyAnalyze)
        {
            Debug.LogWarning("❌ Safe Optimization disabled. Set 'onlyAnalyze' to false first.");
            Debug.LogWarning("❌ And make sure to backup your scene!");
            return;
        }

        Debug.Log("🛠️ Performing SAFE UI optimization...");

        // แค่ปิด raycast target ที่ไม่จำเป็น (ไม่ทำลาย UI)
        OptimizeRaycastTargets();

        // ปิด Rich Text ที่ไม่จำเป็น
        OptimizeRichText();

        // แค่แจ้งเตือน ไม่ลบจริง
        LogProblematicElements();

        Debug.Log("✅ Safe optimization complete. No UI elements were destroyed.");
    }

    private void OptimizeRaycastTargets()
    {
        Image[] allImages = FindObjectsOfType<Image>();
        Text[] allTexts = FindObjectsOfType<Text>();

        int optimizedImages = 0;
        int optimizedTexts = 0;

        foreach (Image image in allImages)
        {
            if (image != null && image.raycastTarget && !HasUIInteraction(image.gameObject))
            {
                image.raycastTarget = false;
                optimizedImages++;
            }
        }

        foreach (Text text in allTexts)
        {
            if (text != null && text.raycastTarget)
            {
                text.raycastTarget = false;
                optimizedTexts++;
            }
        }

        Debug.Log($"🎯 Optimized raycast targets: {optimizedImages} images, {optimizedTexts} texts");
    }

    private void OptimizeRichText()
    {
        Text[] allTexts = FindObjectsOfType<Text>();
        int optimizedCount = 0;

        foreach (Text text in allTexts)
        {
            if (text != null && text.supportRichText && !text.text.Contains("<"))
            {
                text.supportRichText = false;
                optimizedCount++;
            }
        }

        Debug.Log($"📝 Disabled unnecessary Rich Text: {optimizedCount} components");
    }

    private void LogProblematicElements()
    {
        Text[] allTexts = FindObjectsOfType<Text>(true);
        Image[] allImages = FindObjectsOfType<Image>(true);

        List<string> problematicObjects = new List<string>();

        foreach (Text text in allTexts)
        {
            if (text != null && (string.IsNullOrEmpty(text.text) || text.text == "Text"))
            {
                problematicObjects.Add($"Text: {text.gameObject.name}");
            }
        }

        foreach (Image image in allImages)
        {
            if (image != null && (image.sprite == null || image.color.a < 0.01f))
            {
                problematicObjects.Add($"Image: {image.gameObject.name}");
            }
        }

        if (problematicObjects.Count > 0)
        {
            Debug.Log("🔍 Objects that could be manually removed:");
            for (int i = 0; i < Mathf.Min(10, problematicObjects.Count); i++)
            {
                Debug.Log($"   - {problematicObjects[i]}");
            }

            if (problematicObjects.Count > 10)
            {
                Debug.Log($"   ... and {problematicObjects.Count - 10} more");
            }
        }
    }

    private bool HasUIInteraction(GameObject obj)
    {
        return obj.GetComponent<Button>() != null ||
               obj.GetComponent<Toggle>() != null ||
               obj.GetComponent<Slider>() != null ||
               obj.GetComponent<Dropdown>() != null ||
               obj.GetComponent<InputField>() != null ||
               obj.GetComponent<Scrollbar>() != null;
    }

    [ContextMenu("Count Current UI")]
    public void CountCurrentUI()
    {
        Text[] texts = FindObjectsOfType<Text>();
        Image[] images = FindObjectsOfType<Image>();
        Canvas[] canvases = FindObjectsOfType<Canvas>();

        Debug.Log($"📊 Current UI Count:");
        Debug.Log($"   Active Text: {texts.Length}");
        Debug.Log($"   Active Image: {images.Length}");
        Debug.Log($"   Active Canvas: {canvases.Length}");

        // เช็ค memory โดยประมาณ
        long estimatedMemory = (texts.Length * 100) + (images.Length * 500); // KB
        Debug.Log($"💾 Estimated UI Memory: ~{estimatedMemory / 1024} MB");
    }
}