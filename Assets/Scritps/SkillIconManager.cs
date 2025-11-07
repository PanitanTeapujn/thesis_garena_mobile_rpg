using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// Singleton Manager สำหรับจัดการ Skill Icons ของแต่ละ Hero
/// ลดการใช้ Memory โดยใช้ sprites ร่วมกันทุก instance
/// </summary>
public class SkillIconManager : MonoBehaviour
{
    private static SkillIconManager _instance;
    public static SkillIconManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject managerObject = new GameObject("SkillIconManager");
                _instance = managerObject.AddComponent<SkillIconManager>();
                DontDestroyOnLoad(managerObject);
                _instance.LoadAllSkillIcons();
            }
            return _instance;
        }
    }

    [Header("📁 Skill Icon Sprites (Cache)")]
    private Dictionary<string, HeroSkillIcons> skillIconCache = new Dictionary<string, HeroSkillIcons>();

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
                LoadAllSkillIcons();
            }
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// โหลด Skill Icons ของทุก Hero ครั้งเดียว
    /// </summary>
    private void LoadAllSkillIcons()
    {
        if (!enableCaching) return;

        Debug.Log("[SkillIconManager] 🔄 Loading skill icons...");

        try
        {
            // โหลด Assassin Skills
            LoadHeroSkills("Assassin", "SkillIcons/Assassin");

            // โหลด BloodKnight Skills
            LoadHeroSkills("BloodKnight", "SkillIcons/BloodKnight");

            // โหลด Berserker Skills
            LoadHeroSkills("Berserker", "SkillIcons/Berserker");

            // โหลด Archer Skills (ถ้ามี)
            LoadHeroSkills("Archer", "SkillIcons/Archer");

            // โหลด IronJuggernaut Skills (ถ้ามี)
            LoadHeroSkills("IronJuggernaut", "SkillIcons/IronJuggernaut");

            Debug.Log($"[SkillIconManager] ✅ Loaded skill icons for {skillIconCache.Count} heroes");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SkillIconManager] ❌ Failed to load skill icons: {e.Message}");
        }
    }

    /// <summary>
    /// โหลด Skill Icons ของ Hero หนึ่งตัว
    /// </summary>
    private void LoadHeroSkills(string heroName, string resourcePath)
    {
        HeroSkillIcons icons = new HeroSkillIcons();

        // โหลดรูป Skill 1-4
        icons.skill1Icon = Resources.Load<Sprite>($"{resourcePath}/Skill1");
        icons.skill2Icon = Resources.Load<Sprite>($"{resourcePath}/Skill2");
        icons.skill3Icon = Resources.Load<Sprite>($"{resourcePath}/Skill3");
        icons.skill4Icon = Resources.Load<Sprite>($"{resourcePath}/Skill4");

        // นับจำนวนที่โหลดสำเร็จ
        int loadedCount = 0;
        if (icons.skill1Icon != null) loadedCount++;
        if (icons.skill2Icon != null) loadedCount++;
        if (icons.skill3Icon != null) loadedCount++;
        if (icons.skill4Icon != null) loadedCount++;

        if (loadedCount > 0)
        {
            skillIconCache[heroName] = icons;
            Debug.Log($"[SkillIconManager] ✅ Loaded {loadedCount}/4 icons for {heroName}");
        }
        else
        {
            Debug.LogWarning($"[SkillIconManager] ⚠️ No icons found for {heroName} at {resourcePath}");
        }
    }
    #endregion

    #region Public API
    /// <summary>
    /// ดึง Skill Icons ของ Hero
    /// </summary>
    public HeroSkillIcons GetHeroSkillIcons(string heroName)
    {
        if (!enableCaching)
        {
            // โหลดแบบ real-time ถ้าไม่ใช้ cache
            HeroSkillIcons icons = new HeroSkillIcons();
            icons.skill1Icon = Resources.Load<Sprite>($"SkillIcons/{heroName}/Skill1");
            icons.skill2Icon = Resources.Load<Sprite>($"SkillIcons/{heroName}/Skill2");
            icons.skill3Icon = Resources.Load<Sprite>($"SkillIcons/{heroName}/Skill3");
            icons.skill4Icon = Resources.Load<Sprite>($"SkillIcons/{heroName}/Skill4");
            return icons;
        }

        if (skillIconCache.ContainsKey(heroName))
        {
            return skillIconCache[heroName];
        }

        Debug.LogWarning($"[SkillIconManager] No icons found for hero: {heroName}");
        return null;
    }

    /// <summary>
    /// ตรวจสอบว่ามี Skill Icons สำหรับ Hero นี้หรือไม่
    /// </summary>
    public bool HasHeroIcons(string heroName)
    {
        if (!enableCaching)
        {
            return Resources.Load<Sprite>($"SkillIcons/{heroName}/Skill1") != null;
        }

        return skillIconCache.ContainsKey(heroName);
    }

    /// <summary>
    /// ตั้งค่า Skill Icons ให้กับ CombatUIManager
    /// </summary>
    public void SetSkillIcons(CombatUIManager combatUI, string heroName)
    {
        if (combatUI == null)
        {
            Debug.LogError("[SkillIconManager] CombatUIManager is null!");
            return;
        }

        HeroSkillIcons icons = GetHeroSkillIcons(heroName);
        if (icons == null)
        {
            Debug.LogWarning($"[SkillIconManager] No icons found for {heroName}, using fallback");
            return;
        }

        // ตั้งค่าไอคอนให้กับแต่ละปุ่ม
        SetButtonIcon(combatUI.skill1Button, icons.skill1Icon, "Skill1");
        SetButtonIcon(combatUI.skill2Button, icons.skill2Icon, "Skill2");
        SetButtonIcon(combatUI.skill3Button, icons.skill3Icon, "Skill3");
        SetButtonIcon(combatUI.skill4Button, icons.skill4Icon, "Skill4");

        Debug.Log($"[SkillIconManager] ✅ Set skill icons for {heroName}");
    }

    /// <summary>
    /// ตั้งค่าไอคอนให้กับปุ่มเดียว
    /// </summary>
    private void SetButtonIcon(Button button, Sprite icon, string skillName)
    {
        if (button == null)
        {
            Debug.LogWarning($"[SkillIconManager] Button for {skillName} is null!");
            return;
        }

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage == null)
        {
            // ลองหา Image ใน children
            buttonImage = button.GetComponentInChildren<Image>();
        }

        if (buttonImage != null && icon != null)
        {
            buttonImage.sprite = icon;
            buttonImage.enabled = true;
            Debug.Log($"[SkillIconManager] ✅ Set icon for {skillName}");
        }
        else
        {
            Debug.LogWarning($"[SkillIconManager] ⚠️ Missing Image or Sprite for {skillName}");
        }
    }

    /// <summary>
    /// ล้าง Cache (สำหรับ memory sensitive projects)
    /// </summary>
    public void ClearCache()
    {
        Debug.Log("[SkillIconManager] 🧹 Clearing skill icon cache...");
        skillIconCache.Clear();
        Resources.UnloadUnusedAssets();
    }

    /// <summary>
    /// รีโหลด Sprites ใหม่
    /// </summary>
    public void ReloadSprites()
    {
        skillIconCache.Clear();
        LoadAllSkillIcons();
    }
    #endregion

    #region Debug Methods
    [ContextMenu("Show Statistics")]
    public void ShowStatistics()
    {
        Debug.Log("=== SKILL ICON MANAGER STATS ===");
        Debug.Log($"Cached Heroes: {skillIconCache.Count}");
        Debug.Log($"Caching Enabled: {enableCaching}");

        foreach (var kvp in skillIconCache)
        {
            int iconCount = 0;
            if (kvp.Value.skill1Icon != null) iconCount++;
            if (kvp.Value.skill2Icon != null) iconCount++;
            if (kvp.Value.skill3Icon != null) iconCount++;
            if (kvp.Value.skill4Icon != null) iconCount++;

            Debug.Log($"  {kvp.Key}: {iconCount}/4 icons loaded");
        }
        Debug.Log("===============================");
    }
    #endregion
}

/// <summary>
/// Class สำหรับเก็บ Skill Icons ของ Hero
/// </summary>
[System.Serializable]
public class HeroSkillIcons
{
    public Sprite skill1Icon;
    public Sprite skill2Icon;
    public Sprite skill3Icon;
    public Sprite skill4Icon;

    public bool IsValid()
    {
        return skill1Icon != null && skill2Icon != null &&
               skill3Icon != null && skill4Icon != null;
    }

    public int GetLoadedIconCount()
    {
        int count = 0;
        if (skill1Icon != null) count++;
        if (skill2Icon != null) count++;
        if (skill3Icon != null) count++;
        if (skill4Icon != null) count++;
        return count;
    }
}