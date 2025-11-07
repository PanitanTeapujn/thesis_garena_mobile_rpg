using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// Singleton Manager สำหรับจัดการรูป Portrait ของตัวละคร
/// ใช้สำหรับแสดงใน Inventory Panel และที่อื่นๆ
/// </summary>
public class CharacterPortraitManager : MonoBehaviour
{
    private static CharacterPortraitManager _instance;
    public static CharacterPortraitManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject managerObject = new GameObject("CharacterPortraitManager");
                _instance = managerObject.AddComponent<CharacterPortraitManager>();
                DontDestroyOnLoad(managerObject);
                _instance.LoadAllPortraits();
            }
            return _instance;
        }
    }

    [Header("📁 Character Portrait Sprites (Cache)")]
    private Dictionary<string, Sprite> portraitCache = new Dictionary<string, Sprite>();

    [Header("⚡ Performance Settings")]
    [SerializeField] private bool enableCaching = true;
    [SerializeField] private bool preloadOnStart = true;

    #region Initialization
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (preloadOnStart)
            {
                LoadAllPortraits();
            }
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// โหลด Portrait ของทุกตัวละครครั้งเดียว
    /// </summary>
    private void LoadAllPortraits()
    {
        if (!enableCaching) return;

        Debug.Log("[CharacterPortraitManager] 🔄 Loading character portraits...");

        try
        {
            // โหลดรูปตัวละครจาก Resources
            LoadPortrait("Assassin", "CharacterPortraits/Assassin_Portrait");
            LoadPortrait("BloodKnight", "CharacterPortraits/BloodKnight_Portrait");
            LoadPortrait("Berserker", "CharacterPortraits/Berserker_Portrait");
            LoadPortrait("Archer", "CharacterPortraits/Archer_Portrait");
            LoadPortrait("IronJuggernaut", "CharacterPortraits/IronJuggernaut_Portrait");

            Debug.Log($"[CharacterPortraitManager] ✅ Loaded {portraitCache.Count} character portraits");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CharacterPortraitManager] ❌ Failed to load portraits: {e.Message}");
        }
    }

    /// <summary>
    /// โหลด Portrait ของตัวละครหนึ่งตัว
    /// </summary>
    private void LoadPortrait(string characterName, string resourcePath)
    {
        Sprite portrait = Resources.Load<Sprite>(resourcePath);

        if (portrait != null)
        {
            portraitCache[characterName] = portrait;
            Debug.Log($"[CharacterPortraitManager] ✅ Loaded portrait for {characterName}");
        }
        else
        {
            Debug.LogWarning($"[CharacterPortraitManager] ⚠️ Portrait not found for {characterName} at {resourcePath}");
        }
    }
    #endregion

    #region Public API
    /// <summary>
    /// ดึง Portrait Sprite ของตัวละคร
    /// </summary>
    public Sprite GetCharacterPortrait(string characterName)
    {
        if (!enableCaching)
        {
            // โหลดแบบ real-time ถ้าไม่ใช้ cache
            return Resources.Load<Sprite>($"CharacterPortraits/{characterName}_Portrait");
        }

        if (portraitCache.ContainsKey(characterName))
        {
            return portraitCache[characterName];
        }

        Debug.LogWarning($"[CharacterPortraitManager] No portrait found for: {characterName}");
        return null;
    }

    /// <summary>
    /// ตรวจสอบว่ามี Portrait หรือไม่
    /// </summary>
    public bool HasPortrait(string characterName)
    {
        if (!enableCaching)
        {
            return Resources.Load<Sprite>($"CharacterPortraits/{characterName}_Portrait") != null;
        }

        return portraitCache.ContainsKey(characterName);
    }

    /// <summary>
    /// ตั้งค่า Portrait ให้ Image component
    /// </summary>
    public void SetCharacterPortrait(Image portraitImage, string characterName)
    {
        if (portraitImage == null)
        {
            Debug.LogError("[CharacterPortraitManager] Portrait Image is null!");
            return;
        }

        Sprite portrait = GetCharacterPortrait(characterName);

        if (portrait != null)
        {
            portraitImage.sprite = portrait;
            portraitImage.enabled = true;
            portraitImage.color = Color.white;
            Debug.Log($"[CharacterPortraitManager] ✅ Set portrait for {characterName}");
        }
        else
        {
            // Fallback - แสดงรูป default หรือซ่อน
            portraitImage.sprite = null;
            portraitImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // สีเทาจาง
            portraitImage.enabled = true;
            Debug.LogWarning($"[CharacterPortraitManager] ⚠️ Using fallback for {characterName}");
        }
    }

    /// <summary>
    /// ซ่อน Portrait
    /// </summary>
    public void HidePortrait(Image portraitImage)
    {
        if (portraitImage != null)
        {
            portraitImage.sprite = null;
            portraitImage.enabled = false;
        }
    }

    /// <summary>
    /// ล้าง Cache
    /// </summary>
    public void ClearCache()
    {
        Debug.Log("[CharacterPortraitManager] 🧹 Clearing portrait cache...");
        portraitCache.Clear();
        Resources.UnloadUnusedAssets();
    }

    /// <summary>
    /// รีโหลด Portraits ใหม่
    /// </summary>
    public void ReloadPortraits()
    {
        portraitCache.Clear();
        LoadAllPortraits();
    }
    #endregion

    #region Debug Methods
    [ContextMenu("Show Statistics")]
    public void ShowStatistics()
    {
        Debug.Log("=== CHARACTER PORTRAIT MANAGER STATS ===");
        Debug.Log($"Cached Portraits: {portraitCache.Count}");
        Debug.Log($"Caching Enabled: {enableCaching}");

        foreach (var kvp in portraitCache)
        {
            Debug.Log($"  {kvp.Key}: {(kvp.Value != null ? "Loaded ✅" : "Missing ❌")}");
        }
        Debug.Log("=======================================");
    }
    #endregion
}