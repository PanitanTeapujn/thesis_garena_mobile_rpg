using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
/// <summary>
/// Singleton Manager สำหรับจัดการ Tier Background Sprites
/// ลดการใช้ Memory โดยใช้ sprites ร่วมกันทุก instance
/// </summary>
public class TierBackgroundManager : MonoBehaviour
{
    private static TierBackgroundManager _instance;
    public static TierBackgroundManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // สร้าง GameObject ใหม่สำหรับ Manager
                GameObject managerObject = new GameObject("TierBackgroundManager");
                _instance = managerObject.AddComponent<TierBackgroundManager>();
                DontDestroyOnLoad(managerObject);
                _instance.LoadAllTierSprites();
            }
            return _instance;
        }
    }

    [Header("📁 Tier Background Sprites (Cache)")]
    private Dictionary<ItemTier, Sprite> tierBackgroundSprites = new Dictionary<ItemTier, Sprite>();
    
    [Header("⚡ Performance Settings")]
    [SerializeField] private bool enableCaching = true;
    [SerializeField] private bool preloadOnStart = true;
    [SerializeField] private bool unloadUnusedSprites = false; // สำหรับ memory sensitive projects

    #region Initialization
    private void Awake()
    {
        // Singleton pattern
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (preloadOnStart)
            {
                LoadAllTierSprites();
            }
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// โหลด Tier Sprites ทั้งหมดครั้งเดียว
    /// </summary>
    private void LoadAllTierSprites()
    {
        if (!enableCaching) return;

        Debug.Log("[TierBackgroundManager] 🔄 Loading tier background sprites...");

        try
        {
            // โหลดจาก Resources ครั้งเดียว
            tierBackgroundSprites[ItemTier.Common] = Resources.Load<Sprite>("TierBackgrounds/Common_Background");
            tierBackgroundSprites[ItemTier.Uncommon] = Resources.Load<Sprite>("TierBackgrounds/Uncommon_Background");
            tierBackgroundSprites[ItemTier.Rare] = Resources.Load<Sprite>("TierBackgrounds/Rare_Background");
            tierBackgroundSprites[ItemTier.Epic] = Resources.Load<Sprite>("TierBackgrounds/Epic_Background");
            tierBackgroundSprites[ItemTier.Legendary] = Resources.Load<Sprite>("TierBackgrounds/Legendary_Background");

            // นับ sprites ที่โหลดสำเร็จ
            int loadedCount = 0;
            foreach (var kvp in tierBackgroundSprites)
            {
                if (kvp.Value != null) loadedCount++;
            }

            Debug.Log($"[TierBackgroundManager] ✅ Loaded {loadedCount}/5 tier background sprites");

            // ✅ Debug memory usage
            long estimatedMemory = CalculateEstimatedMemoryUsage();
            Debug.Log($"[TierBackgroundManager] 📊 Estimated memory usage: ~{estimatedMemory / 1024}KB");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TierBackgroundManager] ❌ Failed to load tier sprites: {e.Message}");
        }
    }
    #endregion

    #region Public API
    /// <summary>
    /// ดึง Tier Background Sprite
    /// </summary>
    public Sprite GetTierBackgroundSprite(ItemTier tier)
    {
        if (!enableCaching)
        {
            // โหลดแบบ real-time ถ้าไม่ใช้ cache
            return Resources.Load<Sprite>($"TierBackgrounds/{tier}_Background");
        }

        if (tierBackgroundSprites.ContainsKey(tier))
        {
            return tierBackgroundSprites[tier];
        }

        Debug.LogWarning($"[TierBackgroundManager] No sprite found for tier: {tier}");
        return null;
    }

    /// <summary>
    /// ตรวจสอบว่ามี Sprite สำหรับ Tier นี้หรือไม่
    /// </summary>
    public bool HasTierSprite(ItemTier tier)
    {
        if (!enableCaching)
        {
            return Resources.Load<Sprite>($"TierBackgrounds/{tier}_Background") != null;
        }

        return tierBackgroundSprites.ContainsKey(tier) && tierBackgroundSprites[tier] != null;
    }

    /// <summary>
    /// ดึงสี Fallback สำหรับ Tier
    /// </summary>
    public Color GetTierFallbackColor(ItemTier tier)
    {
        switch (tier)
        {
            case ItemTier.Common: return Color.white;
            case ItemTier.Uncommon: return Color.green;
            case ItemTier.Rare: return Color.cyan;
            case ItemTier.Epic: return Color.magenta;
            case ItemTier.Legendary: return Color.yellow;
            default: return Color.white;
        }
    }

    /// <summary>
    /// เซ็ต Tier Background ให้ Image component
    /// </summary>
    public void SetTierBackground(Image backgroundImage, ItemTier tier)
    {
        if (backgroundImage == null) return;

        Sprite tierSprite = GetTierBackgroundSprite(tier);

        if (tierSprite != null)
        {
            // ใช้ Sprite
            backgroundImage.sprite = tierSprite;
            backgroundImage.color = Color.white;
            backgroundImage.enabled = true;
        }
        else
        {
            // Fallback เป็นสี
            backgroundImage.sprite = null;
            backgroundImage.color = GetTierFallbackColor(tier);
            backgroundImage.enabled = true;
        }
    }

    /// <summary>
    /// ปิด Tier Background
    /// </summary>
    public void DisableTierBackground(Image backgroundImage)
    {
        if (backgroundImage != null)
        {
            backgroundImage.sprite = null;
            backgroundImage.enabled = false;
        }
    }
    #endregion

    #region Memory Management
    /// <summary>
    /// คำนวณการใช้ Memory โดยประมาณ
    /// </summary>
    private long CalculateEstimatedMemoryUsage()
    {
        long totalBytes = 0;

        foreach (var kvp in tierBackgroundSprites)
        {
            if (kvp.Value != null && kvp.Value.texture != null)
            {
                Texture2D texture = kvp.Value.texture;
                // ประมาณการ: width * height * 4 bytes (RGBA32)
                totalBytes += texture.width * texture.height * 4;
            }
        }

        return totalBytes;
    }

    /// <summary>
    /// ล้าง Cache (สำหรับ memory sensitive projects)
    /// </summary>
    public void ClearCache()
    {
        if (unloadUnusedSprites)
        {
            Debug.Log("[TierBackgroundManager] 🧹 Clearing tier sprite cache...");
            tierBackgroundSprites.Clear();
            Resources.UnloadUnusedAssets();
        }
    }

    /// <summary>
    /// รีโหลด Sprites ใหม่
    /// </summary>
    public void ReloadSprites()
    {
        tierBackgroundSprites.Clear();
        LoadAllTierSprites();
    }
    #endregion

    #region Debug & Statistics
    /// <summary>
    /// แสดงสถิติการใช้งาน
    /// </summary>
    [ContextMenu("Show Statistics")]
    public void ShowStatistics()
    {
        Debug.Log("=== Tier Background Manager Statistics ===");
        Debug.Log($"Cache Enabled: {enableCaching}");
        Debug.Log($"Loaded Sprites: {tierBackgroundSprites.Count}");
        Debug.Log($"Memory Usage: ~{CalculateEstimatedMemoryUsage() / 1024}KB");
        
        foreach (var kvp in tierBackgroundSprites)
        {
            string status = kvp.Value != null ? "✅ Loaded" : "❌ Missing";
            Debug.Log($"  {kvp.Key}: {status}");
        }
    }
    #endregion
}