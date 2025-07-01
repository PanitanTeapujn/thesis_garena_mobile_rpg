using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System;

public enum AttackType
{
    Physical,    // โจมตีด้วย AttackDamage อย่างเดียว
    Magic,       // โจมตีด้วย MagicDamage อย่างเดียว
    Mixed        // โจมตีทั้งสองแบบ (ระบบเดิม)
}

public class Character : NetworkBehaviour
{
    #region Event system สำหรับแจ้งเตือนการเปลี่ยนแปลง stats
    public static event Action OnStatsChanged;
    #endregion

    #region สถานะพื้นฐานทั้งหมด (HP, Mana, Attack, Critical, etc.)
    [Header("Base Stats")]
    public CharacterStats characterStats;

    [SerializeField] private string characterName;
    public string CharacterName { get { return characterName; } }

    [SerializeField] private int currentHp;
    public int CurrentHp { get { return currentHp; } set { currentHp = value; } }

    [SerializeField] private int maxHp;
    public int MaxHp { get { return maxHp; } set { maxHp = value; } }

    [SerializeField] private int currentMana;
    public int CurrentMana { get { return currentMana; } set { currentMana = value; } }

    [SerializeField] private int maxMana;
    public int MaxMana { get { return maxMana; } set { maxMana = value; } }

    [SerializeField] private int attackDamage;
    public int AttackDamage { get { return attackDamage; } set { attackDamage = value; } }

    [SerializeField] private int magicDamage;
    public int MagicDamage { get { return magicDamage; } set { magicDamage = value; } }

    [SerializeField] private int armor;
    public int Armor { get { return armor; } set { armor = value; } }

    [SerializeField] private float moveSpeed;
    public float MoveSpeed { get { return moveSpeed; } set { moveSpeed = value; } }

    [SerializeField] private float attackRange;
    public float AttackRange { get { return attackRange; } set { attackRange = value; } }

    [SerializeField] private float attackCooldown;
    public float AttackCooldown { get { return attackCooldown; } set { attackCooldown = value; } }

    [Header("Critical Stats")]
    [SerializeField] private float criticalChance;
    public float CriticalChance { get { return criticalChance; } set { criticalChance = value; } }

    [SerializeField] private float criticalDamageBonus;
    public float CriticalDamageBonus { get { return criticalDamageBonus; } set { criticalDamageBonus = value; } }

    [SerializeField] private float hitRate;
    public float HitRate { get { return hitRate; } set { hitRate = value; } }

    [SerializeField] private float evasionRate;
    public float EvasionRate { get { return evasionRate; } set { evasionRate = value; } }

    [SerializeField] private float attackSpeed;
    public float AttackSpeed { get { return attackSpeed; } set { attackSpeed = value; } }

    [SerializeField] private float reductionCoolDown;
    public float ReductionCoolDown { get { return reductionCoolDown; } set { reductionCoolDown = value; } }

    [Header("Attack Settings")]
    [SerializeField] private AttackType attackType = AttackType.Physical; // Default เป็น Physical
    public AttackType AttackType { get { return attackType; } set { attackType = value; } }
    #endregion

    #region Network Properties  สำหรับ Fusion networking
    [Networked] public int NetworkedCurrentHp { get; set; }
    [Networked] public int NetworkedMaxHp { get; set; }
    [Networked] public int NetworkedCurrentMana { get; set; }
    [Networked] public int NetworkedMaxMana { get; set; }
    [Networked] public bool IsNetworkStateReady { get; set; }
    #endregion

    #region Regeneration Settings การตั้งค่าการฟื้นฟู HP/Mana
    [Header("Regeneration Settings")]
    [SerializeField] private float healthRegenPerSecond = 0.5f;
    [SerializeField] private float manaRegenPerSecond = 1f;
    private float healthRegenTimer = 0f;
    private float manaRegenTimer = 0f;
    private float regenTickInterval = 3f; // regen ทุก 3 วินาที
    #endregion

    #region Component References การอ้างอิงถึง managers และ components อื่นๆ
    [Header("Physics")]
    public Rigidbody rb;

    protected StatusEffectManager statusEffectManager;
    protected CombatManager combatManager;
    protected EquipmentManager equipmentManager;
    protected CharacterVisualManager visualManager;
    protected LevelManager levelManager;
    protected Inventory inventory;
    protected EquipmentSlotManager equipmentSlotManager; // 🆕 เพิ่มใหม่
    [Header("🆕 Equipment Slots")]
    [SerializeField] private List<ItemData> characterEquippedItems = new List<ItemData>(6); // 6 slots: Head, Armor, Weapon, Pants, Shoes, Rune
    [SerializeField] private List<ItemData> potionSlots = new List<ItemData>(5);   // 5 potion quick slots
    [Header("🧪 Potion Stack Counts")]
    [SerializeField] private List<int> potionStackCounts = new List<int>(5); // เก็บจำนวนของแต่ละ potion slot
    [Header("🧪 Potion Usage")]
    [SerializeField] public float potionCooldown = 1f; // cooldown 1 วินาที
    private float[] lastPotionUseTime = new float[5]; // cooldown แยกแต่ละ slot
    public float PotionCooldown { get { return potionCooldown; } }

    // Events สำหรับแจ้ง UI
    public static event System.Action<Character, ItemType, ItemData> OnItemEquippedToSlot;
    public static event System.Action<Character, ItemType> OnItemUnequippedFromSlot;
    #endregion

    #region Unity Lifecycle & Initialization Awake, Start และการเริ่มต้นระบบต่างๆ
    protected virtual void Awake()
    {
        InitializeComponents();
        InitializePhysics();
    }
    public static void RaiseOnStatsChanged()
    {
        OnStatsChanged?.Invoke();
    }
    protected virtual void Start()
    {
        StartCoroutine(DelayedLoadPlayerDataStart());
        InitializeStats();
    }
    private System.Collections.IEnumerator DelayedLoadPlayerDataStart()
    {
        Debug.Log($"[Character] Starting delayed player data load for {CharacterName}...");

        // รอ 5 frames เพื่อให้ UI systems และ managers setup เสร็จ
        for (int i = 0; i < 5; i++)
        {
            yield return null;
        }

        // ตรวจสอบว่า PersistentPlayerData พร้อมหรือยัง
        int waitCount = 0;
        while (PersistentPlayerData.Instance == null && waitCount < 30) // รอสูงสุด 30 frames
        {
            yield return null;
            waitCount++;
        }

        if (PersistentPlayerData.Instance == null)
        {
            Debug.LogWarning($"[Character] PersistentPlayerData not ready after {waitCount} frames");
            yield break;
        }

        Debug.Log($"[Character] PersistentPlayerData ready after {waitCount} frames");

        // ตรวจสอบว่ามีข้อมูลใน Firebase หรือไม่
        if (PersistentPlayerData.Instance.ShouldLoadFromFirebase())
        {
            Debug.Log($"[Character] Found saved data, loading inventory for {CharacterName}...");

            // โหลดข้อมูล
            yield return StartCoroutine(DelayedLoadPlayerData());
        }
        else
        {
            Debug.Log($"[Character] No saved data found for {CharacterName}");
        }
    }
    private void LoadPlayerDataIfAvailable()
    {
        if (PersistentPlayerData.Instance == null)
        {
            Debug.LogWarning("[Character] PersistentPlayerData not available yet");
            return;
        }

        // ตรวจสอบว่ามีข้อมูลใน Firebase หรือไม่
        if (PersistentPlayerData.Instance.ShouldLoadFromFirebase())
        {
            Debug.Log($"[Character] Found saved data, loading inventory for {CharacterName}...");

            // 🆕 ใช้ Coroutine เพื่อ delay การโหลด
            StartCoroutine(DelayedLoadPlayerData());
        }
        else
        {
            Debug.Log($"[Character] No saved data found for {CharacterName}");
        }
    }
    private System.Collections.IEnumerator DelayedLoadPlayerData()
    {
        Debug.Log("[Character] Starting detailed load of player data...");

        // รอ 3 frames เพื่อให้ UI systems พร้อม
        yield return null;
        yield return null;
        yield return null;

        // โหลดข้อมูล inventory และ equipment
        try
        {
            Debug.Log("[Character] Loading inventory data...");
            PersistentPlayerData.Instance.LoadInventoryData(this);

            Debug.Log("[Character] ✅ Player data loaded successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Character] ❌ Error loading player data: {e.Message}");
        }

        // รออีก 3 frames แล้ว force refresh equipment
        yield return null;
        yield return null;
        yield return null;

        Debug.Log("[Character] Force refreshing equipment UI...");
        ForceRefreshAllEquipmentUI();

        // รออีก 2 frames แล้ว verify ผลลัพธ์
        yield return null;
        yield return null;

        Debug.Log("[Character] Verifying loaded equipment...");
        DebugLoadedEquipment();
    }

    private void ForceRefreshAllEquipmentUI()
    {
        try
        {
            Debug.Log("[Character] 🔄 Force refreshing equipment UI...");

            int refreshedManagers = 0;

            // 1. Force refresh EquipmentSlotManager
            var equipmentSlotManager = GetComponent<EquipmentSlotManager>();
            if (equipmentSlotManager != null)
            {
                if (equipmentSlotManager.IsConnected())
                {
                    equipmentSlotManager.ForceRefreshFromCharacter();
                    refreshedManagers++;
                    Debug.Log("[Character] ✅ EquipmentSlotManager refreshed");
                }
                else
                {
                    Debug.LogWarning("[Character] ⚠️ EquipmentSlotManager not connected, will retry...");

                    // ลองใหม่หลัง 1 วินาที
                    StartCoroutine(RetryRefreshEquipmentUI());
                }
            }
            else
            {
                Debug.LogWarning("[Character] ⚠️ No EquipmentSlotManager found on character");
            }

            // 2. Force refresh CombatUIManager equipment
            var combatUIManager = FindObjectOfType<CombatUIManager>();
            if (combatUIManager?.equipmentSlotManager != null)
            {
                if (combatUIManager.equipmentSlotManager.IsConnected())
                {
                    combatUIManager.equipmentSlotManager.ForceRefreshFromCharacter();
                    refreshedManagers++;
                    Debug.Log("[Character] ✅ CombatUIManager equipment refreshed");
                }
                else
                {
                    Debug.LogWarning("[Character] ⚠️ CombatUIManager equipment not connected");
                }
            }
            else
            {
                Debug.LogWarning("[Character] ⚠️ No CombatUIManager equipment manager found");
            }

            // 3. แจ้ง stats changed
            RaiseOnStatsChanged();

            // 4. Force update Canvas
            Canvas.ForceUpdateCanvases();

            Debug.Log($"[Character] ✅ Equipment UI refresh complete ({refreshedManagers} managers refreshed)");

            if (refreshedManagers == 0)
            {
                Debug.LogError("[Character] ❌ No equipment managers were refreshed!");
                DebugEquipmentManagersStatus();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Character] ❌ Error refreshing equipment UI: {e.Message}");
        }
    }

    // 🆕 เพิ่ม method สำหรับ debug equipment managers
    private void DebugEquipmentManagersStatus()
    {
        Debug.Log("=== EQUIPMENT MANAGERS STATUS ===");

        var equipmentSlotManager = GetComponent<EquipmentSlotManager>();
        Debug.Log($"Character EquipmentSlotManager: {(equipmentSlotManager != null ? "Found" : "Not Found")}");
        if (equipmentSlotManager != null)
        {
            Debug.Log($"  - Is Connected: {equipmentSlotManager.IsConnected()}");
        }

        var combatUIManager = FindObjectOfType<CombatUIManager>();
        Debug.Log($"CombatUIManager: {(combatUIManager != null ? "Found" : "Not Found")}");
        if (combatUIManager != null)
        {
            Debug.Log($"  - Has Equipment Manager: {(combatUIManager.equipmentSlotManager != null)}");
            if (combatUIManager.equipmentSlotManager != null)
            {
                Debug.Log($"  - Equipment Manager Connected: {combatUIManager.equipmentSlotManager.IsConnected()}");
            }
        }

        Debug.Log("================================");
    }
    private System.Collections.IEnumerator RetryRefreshEquipmentUI()
    {
        int maxRetries = 10;
        int retryCount = 0;

        while (retryCount < maxRetries)
        {
            yield return new WaitForSeconds(0.5f); // รอ 0.5 วินาที
            retryCount++;

            var equipmentSlotManager = GetComponent<EquipmentSlotManager>();
            if (equipmentSlotManager != null && equipmentSlotManager.IsConnected())
            {
                equipmentSlotManager.ForceRefreshFromCharacter();
                Debug.Log($"[Character] ✅ Equipment UI refreshed after {retryCount} retries");
                break;
            }

            Debug.Log($"[Character] Retry {retryCount}/{maxRetries} - Equipment manager still not ready");
        }

        if (retryCount >= maxRetries)
        {
            Debug.LogWarning("[Character] ⚠️ Failed to refresh equipment UI after max retries");
        }
    }
    private void InitializeComponents()
    {
        // Get or add components
        statusEffectManager = GetComponent<StatusEffectManager>();
        if (statusEffectManager == null)
            statusEffectManager = gameObject.AddComponent<StatusEffectManager>();

        combatManager = GetComponent<CombatManager>();
        if (combatManager == null)
            combatManager = gameObject.AddComponent<CombatManager>();

        equipmentManager = GetComponent<EquipmentManager>();
        if (equipmentManager == null)
            equipmentManager = gameObject.AddComponent<EquipmentManager>();

        visualManager = GetComponent<CharacterVisualManager>();
        if (visualManager == null)
            visualManager = gameObject.AddComponent<CharacterVisualManager>();

        levelManager = GetComponent<LevelManager>();
        if (levelManager == null)
            levelManager = gameObject.AddComponent<LevelManager>();
        inventory = GetComponent<Inventory>();
        if (inventory == null)
            inventory = gameObject.AddComponent<Inventory>();
        equipmentSlotManager = GetComponent<EquipmentSlotManager>();
        if (equipmentSlotManager == null)
            equipmentSlotManager = gameObject.AddComponent<EquipmentSlotManager>();
    }

    private void InitializePhysics()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationY;
            rb.useGravity = true;
            rb.drag = 1.0f;
            rb.mass = 10f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }

    protected virtual void InitializeStats()
    {
        if (characterStats != null)
        {
            // ✅ ใช้ ScriptableObject เป็นหลักสำหรับทุกตัวละคร
            characterName = characterStats.characterName;
            maxHp = characterStats.maxHp;
            currentHp = maxHp;
            maxMana = characterStats.maxMana;
            currentMana = maxMana;
            attackDamage = characterStats.attackDamage;
            magicDamage = characterStats.magicDamage;
            armor = characterStats.arrmor;
            moveSpeed = characterStats.moveSpeed;
            attackRange = characterStats.attackRange;
            attackCooldown = characterStats.attackCoolDown;
            criticalChance = characterStats.criticalChance;
            criticalDamageBonus = characterStats.criticalDamageBonus;
            hitRate = characterStats.hitRate;
            evasionRate = characterStats.evasionRate;
            attackSpeed = characterStats.attackSpeed;
            reductionCoolDown = characterStats.reductionCoolDown;
            attackType = characterStats.attackType;
            InitializeEquipmentSlots();

        }
    }
    #endregion

    #region Fusion Network Methods RPC, Spawned, FixedUpdateNetwork
    public override void Spawned()
    {
        base.Spawned();

        if (HasStateAuthority)
        {
            NetworkedMaxHp = maxHp;
            NetworkedCurrentHp = currentHp;
            NetworkedMaxMana = maxMana;
            NetworkedCurrentMana = currentMana;
            IsNetworkStateReady = true;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority)
        {
            // Sync HP/Mana ทุก frame
            NetworkedCurrentHp = currentHp;
            NetworkedCurrentMana = currentMana;
            NetworkedMaxHp = maxHp;
            NetworkedMaxMana = maxMana;
            ProcessRegeneration();
        }

        if (GetInput<NetworkInputData>(out var input))
        {
            HandlePotionInputs(input);
        }
    }
    protected virtual void HandlePotionInputs(NetworkInputData input)
    {
        // ตรวจสอบว่าเป็น InputAuthority หรือไม่
        if (!HasInputAuthority)
            return;

        // จัดการ potion inputs (ใช้ implicit bool conversion)
        if (input.potion1 && CanUsePotion(0))
        {
            UsePotion(0);
        }

        if (input.potion2 && CanUsePotion(1))
        {
            UsePotion(1);
        }

        if (input.potion3 && CanUsePotion(2))
        {
            UsePotion(2);
        }

        if (input.potion4 && CanUsePotion(3))
        {
            UsePotion(3);
        }

        if (input.potion5 && CanUsePotion(4))
        {
            UsePotion(4);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    protected virtual void RPC_RequestDeath()
    {
        // StateAuthority ตรวจสอบและตัดสินใจ
        if (CanDie()) // เพิ่ม validation
        {
            RPC_OnDeath();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    protected virtual void RPC_OnDeath()
    {
        Debug.Log($"{CharacterName} died!");
        // Handle death logic here
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SyncHealthRegen(int newHp)
    {
        currentHp = newHp;
        NetworkedCurrentHp = newHp;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SyncManaRegen(int newMana)
    {
        currentMana = newMana;
        NetworkedCurrentMana = newMana;
    }

    public void ForceUpdateNetworkState()
    {
        if (HasStateAuthority)
        {
            NetworkedMaxHp = maxHp;
            NetworkedCurrentHp = currentHp;
            NetworkedMaxMana = maxMana;
            NetworkedCurrentMana = currentMana;
            IsNetworkStateReady = true;
        }
    }
    #endregion

    #region Damage Calculation Methods  การคำนวณดาเมจสำหรับ Attack Types ต่างๆ
    public virtual (int physicalDamage, int magicDamage) GetAttackDamages()
    {
        switch (attackType)
        {
            case AttackType.Physical:
                return (AttackDamage, 0); // Physical อย่างเดียว

            case AttackType.Magic:
                return (0, MagicDamage); // Magic อย่างเดียว

            case AttackType.Mixed:
                return (AttackDamage, MagicDamage); // ทั้งสองแบบ

            default:
                return (AttackDamage, 0); // Default เป็น Physical
        }
    }

    public virtual (int physicalDamage, int magicDamage) GetSkillDamages(AttackType skillType, float physicalRatio = 1f, float magicRatio = 1f)
    {
        switch (skillType)
        {
            case AttackType.Physical:
                int physDamage = Mathf.RoundToInt(AttackDamage * physicalRatio);
                return (physDamage, 0);

            case AttackType.Magic:
                int magDamage = Mathf.RoundToInt(MagicDamage * magicRatio);
                return (0, magDamage);

            case AttackType.Mixed:
                int mixedPhys = Mathf.RoundToInt(AttackDamage * physicalRatio);
                int mixedMag = Mathf.RoundToInt(MagicDamage * magicRatio);
                return (mixedPhys, mixedMag);

            default:
                return (AttackDamage, 0);
        }
    }

    public virtual (int physicalDamage, int magicDamage) GetSkillDamages(AttackType skillType, int flatPhysical = 0, int flatMagic = 0)
    {
        switch (skillType)
        {
            case AttackType.Physical:
                return (flatPhysical, 0);

            case AttackType.Magic:
                return (0, flatMagic);

            case AttackType.Mixed:
                return (flatPhysical, flatMagic);

            default:
                return (flatPhysical, 0);
        }
    }

    public virtual (int physicalDamage, int magicDamage) GetAdvancedSkillDamages(
        float physicalRatio = 0f, float magicRatio = 0f,
        int flatPhysical = 0, int flatMagic = 0)
    {
        int physDamage = Mathf.RoundToInt(AttackDamage * physicalRatio) + flatPhysical;
        int magDamage = Mathf.RoundToInt(MagicDamage * magicRatio) + flatMagic;

        return (physDamage, magDamage);
    }
    #endregion

    #region Skill and Combat Methods Skills, การโจมตี, การรับดาเมจ, การรักษา
    public virtual void UseSkillOnTarget(Character target, AttackType skillType, float physicalRatio = 1f, float magicRatio = 1f, DamageType damageType = DamageType.Normal)
    {
        if (combatManager == null) return;

        var (physicalDamage, magicDamage) = GetSkillDamages(skillType, physicalRatio, magicRatio);

        Debug.Log($"[Skill Attack] {CharacterName} uses {skillType} skill: Physical={physicalDamage}, Magic={magicDamage}");

        combatManager.TakeDamageFromAttacker(physicalDamage, magicDamage, this, damageType);
    }

    public virtual void UseSkillOnTarget(Character target, AttackType skillType, int flatPhysical, int flatMagic, DamageType damageType = DamageType.Normal)
    {
        if (combatManager == null) return;

        var (physicalDamage, magicDamage) = GetSkillDamages(skillType, flatPhysical, flatMagic);

        Debug.Log($"[Skill Attack] {CharacterName} uses {skillType} skill: Physical={physicalDamage}, Magic={magicDamage}");

        target.TakeDamageFromAttacker(physicalDamage, magicDamage, this, damageType);
    }

    public virtual void UseAdvancedSkillOnTarget(Character target,
        float physicalRatio = 0f, float magicRatio = 0f,
        int flatPhysical = 0, int flatMagic = 0,
        DamageType damageType = DamageType.Normal)
    {
        if (combatManager == null) return;

        var (physicalDamage, magicDamage) = GetAdvancedSkillDamages(physicalRatio, magicRatio, flatPhysical, flatMagic);

        Debug.Log($"[Advanced Skill] {CharacterName}: Physical={physicalDamage} (ratio:{physicalRatio}, flat:{flatPhysical}), Magic={magicDamage} (ratio:{magicRatio}, flat:{flatMagic})");

        target.TakeDamageFromAttacker(physicalDamage, magicDamage, this, damageType);
    }

    public virtual void TakeDamage(int damage, DamageType damageType = DamageType.Normal, bool isCritical = false)
    {
        if (combatManager == null) return;
        combatManager.TakeDamage(damage, damageType, isCritical);
    }

    public virtual void TakeDamageFromAttacker(int damage, Character attacker, DamageType damageType = DamageType.Normal)
    {
        if (combatManager == null) return;
        combatManager.TakeDamageFromAttacker(damage, attacker, damageType);
    }

    public virtual void TakeDamageFromAttacker(int physicalDamage, int magicDamage, Character attacker, DamageType damageType = DamageType.Normal)
    {
        if (combatManager == null) return;
        combatManager.TakeDamageFromAttacker(physicalDamage, magicDamage, attacker, damageType);
    }

    public void Heal(int amount)
    {
        if (combatManager == null) return;
        combatManager.Heal(amount);
    }
    #endregion

    #region Status Effect Methods การใช้และตรวจสอบ status effects
    public void ApplyStatusEffect(StatusEffectType effectType, int damage = 0, float duration = 0f, float amount = 0f)
    {
        if (statusEffectManager == null) return;

        switch (effectType)
        {
            // Magical Effects
            case StatusEffectType.Poison:
                statusEffectManager.ApplyPoison(damage, duration);
                break;
            case StatusEffectType.Burn:
                statusEffectManager.ApplyBurn(damage, duration);
                break;
            case StatusEffectType.Bleed:
                statusEffectManager.ApplyBleed(damage, duration);
                break;
            case StatusEffectType.Freeze:
                statusEffectManager.ApplyFreeze(duration);
                break;

            // Physical Effects
            case StatusEffectType.Stun:
                statusEffectManager.ApplyStun(duration);
                break;
            case StatusEffectType.ArmorBreak:
                statusEffectManager.ApplyArmorBreak(duration, amount > 0 ? amount : 0.5f);
                break;
            case StatusEffectType.Blind:
                statusEffectManager.ApplyBlind(duration, amount > 0 ? amount : 0.8f);
                break;
            case StatusEffectType.Weakness:
                statusEffectManager.ApplyWeakness(duration, amount > 0 ? amount : 0.4f);
                break;
        }
    }

    public bool HasStatusEffect(StatusEffectType effectType)
    {
        if (statusEffectManager == null) return false;

        switch (effectType)
        {
            // Magical Effects
            case StatusEffectType.Poison:
                return statusEffectManager.IsPoisoned;
            case StatusEffectType.Burn:
                return statusEffectManager.IsBurning;
            case StatusEffectType.Bleed:
                return statusEffectManager.IsBleeding;
            case StatusEffectType.Freeze:
                return statusEffectManager.IsFrozen;

            // Physical Effects
            case StatusEffectType.Stun:
                return statusEffectManager.IsStunned;
            case StatusEffectType.ArmorBreak:
                return statusEffectManager.IsArmorBreak;
            case StatusEffectType.Blind:
                return statusEffectManager.IsBlind;
            case StatusEffectType.Weakness:
                return statusEffectManager.IsWeak;

            case StatusEffectType.AttackSpeedAura:
                return statusEffectManager.IsProvidingAttackSpeedAura;
            case StatusEffectType.DamageAura:
                return statusEffectManager.IsProvidingDamageAura;
            case StatusEffectType.MoveSpeedAura:
                return statusEffectManager.IsProvidingMoveSpeedAura;
            case StatusEffectType.ProtectionAura:
                return statusEffectManager.IsProvidingProtectionAura;
            case StatusEffectType.ArmorAura:
                return statusEffectManager.IsProvidingArmorAura;
            case StatusEffectType.CriticalAura:
                return statusEffectManager.IsProvidingCriticalAura;

            default:
                return false;
        }
    }
    #endregion

    #region Equipment Methods การจัดการอุปกรณ์และ runes
   
    #endregion
    #region Inventory Methods การจัดการ inventory และ items
    public Inventory GetInventory()
    {
        return inventory;
    }

    public int GetInventorySlotCount()
    {
        if (inventory == null) return 24; // default slots
        return inventory.CurrentSlots;
    }

    public int GetInventoryMaxSlots()
    {
        if (inventory == null) return 48; // default max slots
        return inventory.MaxSlots;
    }

    public bool AddItemToInventory(ItemData itemData, int count = 1)
    {
        if (inventory == null) return false;
        return inventory.AddItem(itemData, count);
    }

    public bool RemoveItemFromInventory(int slotIndex, int count = 1)
    {
        if (inventory == null) return false;
        return inventory.RemoveItem(slotIndex, count);
    }

    public InventoryItem GetInventoryItem(int slotIndex)
    {
        if (inventory == null) return null;
        return inventory.GetItem(slotIndex);
    }

    public bool IsInventorySlotEmpty(int slotIndex)
    {
        if (inventory == null) return true;
        return inventory.IsSlotEmpty(slotIndex);
    }

    public void ExpandInventory(int additionalSlots)
    {
        if (inventory == null) return;
        inventory.ExpandInventory(additionalSlots);
    }

    public bool CanExpandInventory(int additionalSlots)
    {
        if (inventory == null) return false;
        return inventory.CanExpandInventory(additionalSlots);
    }
    #endregion
    #region Regeneration System ระบบการฟื้นฟู HP/Mana อัตโนมัติ
    private void ProcessRegeneration()
    {
        // Health Regeneration
        if (currentHp < maxHp)
        {
            healthRegenTimer += Runner.DeltaTime;
            if (healthRegenTimer >= regenTickInterval)
            {
                RegenerateHealth();
                healthRegenTimer = 0f;
            }
        }

        // Mana Regeneration
        if (currentMana < maxMana)
        {
            manaRegenTimer += Runner.DeltaTime;
            if (manaRegenTimer >= regenTickInterval)
            {
                RegenerateMana();
                manaRegenTimer = 0f;
            }
        }
    }

    private void RegenerateHealth()
    {
        int regenAmount = Mathf.RoundToInt(healthRegenPerSecond);
        int oldHp = currentHp;

        currentHp = Mathf.Min(currentHp + regenAmount, maxHp);

        if (currentHp > oldHp)
        {
            NetworkedCurrentHp = currentHp;

            // ✅ Sync to all clients if this is StateAuthority
            if (HasStateAuthority)
            {
                RPC_SyncHealthRegen(currentHp);
            }
        }
    }

    private void RegenerateMana()
    {
        int regenAmount = 1;
        int oldMana = currentMana;

        currentMana = Mathf.Min(currentMana + regenAmount, maxMana);

        if (currentMana > oldMana)
        {
            NetworkedCurrentMana = currentMana;

            // ✅ Sync to all clients if this is StateAuthority
            if (HasStateAuthority)
            {
                RPC_SyncManaRegen(currentMana);
            }
        }
    }
    #endregion

    #region Effective Stats Methods การคำนวณ stats ที่รวม buffs/debuffs
    public float GetEffectiveMoveSpeed()
    {
        float baseMoveSpeed = moveSpeed;

        // ✅ รวม Move Speed Aura
        if (statusEffectManager != null)
        {
            float moveSpeedMultiplier = statusEffectManager.GetTotalMoveSpeedMultiplier();
            baseMoveSpeed *= moveSpeedMultiplier;
        }

        // ✅ รวม Freeze effect (ใช้ระบบเดิม)
        if (HasStatusEffect(StatusEffectType.Freeze))
        {
            baseMoveSpeed *= 0.3f; // ลดความเร็วเหลือ 30% เมื่อ freeze
        }

        return baseMoveSpeed;
    }

    public float GetEffectiveReductionCoolDown()
    {
        float baseReductionCoolDown = reductionCoolDown;
        if (equipmentManager != null)
        {
            baseReductionCoolDown += equipmentManager.GetReductionCoolDownBonus();
        }

        return baseReductionCoolDown;
    }

    public float GetEffectiveAttackSpeed()
    {
        float baseAttackSpeed = attackSpeed;

        // ✅ รวม Attack Speed Aura
        if (statusEffectManager != null)
        {
            float attackSpeedMultiplier = statusEffectManager.GetTotalAttackSpeedMultiplier();
            baseAttackSpeed *= attackSpeedMultiplier;
        }

        return baseAttackSpeed;
    }

    public float GetEffectiveCriticalDamageBonus()
    {
        float baseCritBonus = criticalDamageBonus;
        float equipmentBonus = 0f;

        if (equipmentManager != null)
        {
            equipmentBonus = equipmentManager.GetCriticalMultiplierBonus();
        }

        float totalBonus = baseCritBonus + equipmentBonus;

        Debug.Log($"🔍 Critical: Base={baseCritBonus}, Equipment={equipmentBonus}, Total={totalBonus}");

        return totalBonus;
    }

    public void UpdateCriticalDamageBonus(float newValue, bool forceNetworkSync = false)
    {
        float oldValue = criticalDamageBonus;
        criticalDamageBonus = newValue;

        Debug.Log($"[Critical Damage Bonus Updated] {CharacterName}: {oldValue} -> {newValue}");

        // Force network sync ถ้าจำเป็น (เฉพาะตอนที่มี authority)
        if (forceNetworkSync && HasStateAuthority)
        {
            ForceUpdateNetworkState();
        }

        // ✅ Trigger event เพื่อแจ้ง UI และระบบอื่นๆ
        OnStatsChanged?.Invoke();
    }
    #endregion

    #region Level and Experience Methods ระบบเลเวลและประสบการณ์
    public int GetCurrentLevel()
    {
        if (levelManager == null) return 1;
        return levelManager.CurrentLevel;
    }

    public int GetCurrentExp()
    {
        if (levelManager == null) return 0;
        return levelManager.CurrentExp;
    }

    public float GetExpProgress()
    {
        if (levelManager == null) return 0f;
        return levelManager.GetExpProgress();
    }

    public void GainExp(int expAmount)
    {
        if (levelManager == null) return;
        levelManager.GainExp(expAmount);
    }

    public bool IsMaxLevel()
    {
        if (levelManager == null) return false;
        return levelManager.IsMaxLevel();
    }
    #endregion

    #region Query and Utility Methods ฟังก์ชันยูทิลิตี้และการค้นหาข้อมูล
    public float GetHealthPercentage()
    {
        if (combatManager == null) return (float)currentHp / maxHp;
        return combatManager.GetHealthPercentage();
    }

    protected virtual bool CanDie()
    {
        return NetworkedCurrentHp <= 0;
    }

    public bool IsSpawned => Object != null && Object.IsValid;
    #endregion
    #region Equipment Methods (ปรับปรุงใหม่)
    // เพิ่ม method ใหม่สำหรับ equip ItemData

    public void EquipItem(EquipmentData equipment)
    {
        if (equipmentManager == null) return;
        equipmentManager.EquipItem(equipment);
    }

    public void UnequipItem()
    {
        if (equipmentManager == null) return;
        equipmentManager.UnequipItem();
    }

    public void ApplyRune(EquipmentStats runeStats)
    {
        if (equipmentManager == null) return;
        equipmentManager.ApplyRuneBonus(runeStats);
    }

    public EquipmentStats GetTotalEquipmentStats()
    {
        if (equipmentManager == null) return new EquipmentStats();
        return equipmentManager.GetTotalStats();
    }

    public void OnEquipmentStatsChanged()
    {
        // แจ้งให้ระบบอื่นๆ รู้ว่า stats เปลี่ยน (รวม Inspector)
        OnStatsChanged?.Invoke();

        Debug.Log($"[Equipment Changed] Critical Damage Bonus now: {GetEffectiveCriticalDamageBonus()}");
    }
    public bool EquipItemData(ItemData itemData)
    {
        if (itemData == null)
        {
            Debug.LogWarning($"[Character] Cannot equip null item");
            return false;
        }

        Debug.Log($"[Character] EquipItemData called: {itemData.ItemName} ({itemData.ItemType})");

        // 🆕 ตรวจสอบ characterEquippedItems และ potionSlots lists ก่อน
        if (characterEquippedItems.Count < 6)
        {
            Debug.LogWarning($"[Character] characterEquippedItems list too small: {characterEquippedItems.Count}");
            InitializeEquipmentSlots();
        }

        if (potionSlots.Count < 5)
        {
            Debug.LogWarning($"[Character] potionSlots list too small: {potionSlots.Count}");
            InitializeEquipmentSlots();
        }

        // สำหรับ potion: ใช้ logic แยกต่างหาก
        if (itemData.ItemType == ItemType.Potion)
        {
            return EquipPotionToSlot(itemData);
        }

        // สำหรับ equipment อื่นๆ: ใช้ logic เดิม
        int slotIndex = GetSlotIndexForItemType(itemData.ItemType);
        if (slotIndex == -1)
        {
            Debug.LogWarning($"[Character] No slot available for item type: {itemData.ItemType}");
            return false;
        }

        // Unequip item เก่าถ้ามี (และเพิ่มกลับไป inventory)
        if (characterEquippedItems[slotIndex] != null)
        {
            ItemData oldItem = characterEquippedItems[slotIndex];

            if (inventory != null)
            {
                inventory.AddItem(oldItem, 1);
                Debug.Log($"[Character] Added old item back to inventory: {oldItem.ItemName}");
            }
        }

        // Equip item ใหม่
        characterEquippedItems[slotIndex] = itemData;
        Debug.Log($"[Character] ✅ Equipped {itemData.ItemName} to slot {slotIndex} ({itemData.ItemType})");

        // คำนวณ total stats จาก equipment ทั้งหมด
        ApplyAllEquipmentStats();

        // แจ้ง Event สำหรับ UI
        OnItemEquippedToSlot?.Invoke(this, itemData.ItemType, itemData);

        // Force update equipment slots ทันที
        ForceUpdateEquipmentSlotsNow();
        PersistentPlayerData.Instance?.SaveInventoryData(this);

        return true;
    }

    // 🆕 เพิ่ม method ใหม่สำหรับ equip potion
    private bool EquipPotionToSlot(ItemData potionData)
    {
        Debug.Log($"[Character] 🧪 Attempting to equip potion: {potionData.ItemName}");

        // หาช่องว่างใน potion slots (0-4)
        int emptySlotIndex = FindEmptyPotionSlot();

        if (emptySlotIndex == -1)
        {
            Debug.LogWarning($"[Character] ❌ All potion slots are full (0-4)");
            DebugPotionSlots();
            return false;
        }

        // 🆕 หาจำนวน potion ทั้งหมดใน inventory
        int totalStackCount = GetPotionStackCountFromInventory(potionData);

        // แสดง potion effects ถ้ามี
        if (potionData.Stats.IsPotion())
        {
            string effects = "";
            if (potionData.Stats.healAmount > 0) effects += $"+{potionData.Stats.healAmount}HP ";
            if (potionData.Stats.manaAmount > 0) effects += $"+{potionData.Stats.manaAmount}MP ";
            if (potionData.Stats.healPercentage > 0) effects += $"+{potionData.Stats.healPercentage:P0}HP ";
            if (potionData.Stats.manaPercentage > 0) effects += $"+{potionData.Stats.manaPercentage:P0}MP ";
            Debug.Log($"[Character] 💊 Potion effects: {effects.Trim()}");
        }

        // ใส่ potion ลงช่องที่ว่าง
        potionSlots[emptySlotIndex] = potionData;
        potionStackCounts[emptySlotIndex] = totalStackCount; // 🆕 เก็บจำนวน stack

        Debug.Log($"[Character] ✅ Equipped {potionData.ItemName} x{totalStackCount} to potion slot {emptySlotIndex}");

        // แจ้ง Event สำหรับ UI (ใช้ ItemType.Potion)
        OnItemEquippedToSlot?.Invoke(this, ItemType.Potion, potionData);

        // Force update equipment slots ทันที
        ForceUpdateEquipmentSlotsNow();

        return true;
    }

    private int GetPotionStackCountFromInventory(ItemData potionData)
    {
        if (inventory == null) return 1;

        int totalCount = 0;
        for (int i = 0; i < inventory.CurrentSlots; i++)
        {
            InventoryItem item = inventory.GetItem(i);
            if (item != null && !item.IsEmpty && item.itemData == potionData)
            {
                totalCount += item.stackCount;
            }
        }

        Debug.Log($"[Character] Found {totalCount} {potionData.ItemName} in inventory");
        return totalCount;
    }

    // 🆕 เพิ่ม method สำหรับ get potion stack count
    public int GetPotionStackCount(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < potionStackCounts.Count)
        {
            return potionStackCounts[slotIndex];
        }
        return 0;
    }

    // 🆕 เพิ่ม method สำหรับ set potion stack count
    public void SetPotionStackCount(int slotIndex, int stackCount)
    {
        if (slotIndex >= 0 && slotIndex < potionStackCounts.Count)
        {
            potionStackCounts[slotIndex] = stackCount;
            Debug.Log($"[Character] Set potion slot {slotIndex} stack count to {stackCount}");
        }
    }
    private void DebugPotionSlots()
    {
        Debug.Log("=== POTION SLOTS STATUS ===");
        for (int i = 0; i < potionSlots.Count; i++)
        {
            ItemData potion = potionSlots[i];
            int stackCount = i < potionStackCounts.Count ? potionStackCounts[i] : 0;

            if (potion != null)
            {
                Debug.Log($"Slot {i}: {potion.ItemName} x{stackCount}");
            }
            else
            {
                Debug.Log($"Slot {i}: EMPTY");
            }
        }
    }

    private void ForceUpdateEquipmentSlotsNow()
    {
        EquipmentSlotManager equipmentSlotManager = GetComponent<EquipmentSlotManager>();
        if (equipmentSlotManager != null && equipmentSlotManager.IsConnected())
        {
            equipmentSlotManager.ForceRefreshFromCharacter();
            Debug.Log("[Character] ✅ Immediate equipment slots refresh");
        }

        // หา CombatUIManager แล้วลอง refresh ด้วย
        CombatUIManager uiManager = FindObjectOfType<CombatUIManager>();
        if (uiManager?.equipmentSlotManager != null)
        {
            uiManager.equipmentSlotManager.ForceRefreshFromCharacter();
            Debug.Log("[Character] ✅ Immediate UI manager refresh");
        }
    }

    // เพิ่ม method สำหรับ unequip
    public bool UnequipPotion(int potionSlotIndex)
    {
        if (potionSlotIndex < 0 || potionSlotIndex >= potionSlots.Count || potionSlots[potionSlotIndex] == null)
        {
            Debug.LogWarning($"[Character] No potion in slot {potionSlotIndex}");
            return false;
        }

        ItemData unequippedPotion = potionSlots[potionSlotIndex];
        int stackCount = potionStackCounts[potionSlotIndex]; // 🆕 ดึงจำนวน stack

        Debug.Log($"[Character] Unequipping {unequippedPotion.ItemName} x{stackCount} from potion slot {potionSlotIndex}");

        // เคลียร์ slot
        potionSlots[potionSlotIndex] = null;
        potionStackCounts[potionSlotIndex] = 0; // 🆕 รีเซ็ต stack count

        // 🆕 ไม่ต้องเพิ่มกลับ inventory ที่นี่ เพราะ ItemDetailPanel จะจัดการเอง

        Debug.Log($"[Character] ✅ Unequipped {unequippedPotion.ItemName} from potion slot {potionSlotIndex}");
        return true;
    }
    // 🆕 Method ใหม่: คำนวณ total stats จาก equipment ทั้งหมด
    private void ApplyAllEquipmentStats()
    {
        if (equipmentManager == null)
        {
            Debug.LogWarning("[Character] EquipmentManager not found - stats will not be applied!");
            return;
        }

        // 🆕 Debug stats ก่อน apply
        Debug.Log($"[Character] 📈 STATS BEFORE APPLY: ATK={AttackDamage}, ARM={Armor}, CRIT={CriticalChance:F1}%, CRIT_DMG={CriticalDamageBonus:F1}%");

        // คำนวณ total stats จาก characterEquippedItems ทั้งหมด
        EquipmentStats totalStats = CalculateTotalEquipmentStats();

        // สร้าง EquipmentData ที่มี total stats
        EquipmentData totalEquipmentData = new EquipmentData
        {
            itemName = "Total Equipment",
            stats = totalStats,
            itemIcon = null
        };

        // ส่ง total stats ไปให้ EquipmentManager ครั้งเดียว
        equipmentManager.EquipItem(totalEquipmentData);

        // 🆕 Debug stats หลัง apply
        Debug.Log($"[Character] 📈 STATS AFTER APPLY: ATK={AttackDamage}, ARM={Armor}, CRIT={CriticalChance:F1}%, CRIT_DMG={GetEffectiveCriticalDamageBonus():F1}%");

        Debug.Log($"[Character] ✅ Applied total equipment stats to EquipmentManager");
    }

    private EquipmentStats CalculateTotalEquipmentStats()
    {
        EquipmentStats totalStats = new EquipmentStats();

        // รวม stats จาก equipment ทุกชิ้น
        foreach (ItemData equippedItem in characterEquippedItems)
        {
            if (equippedItem != null)
            {
                EquipmentStats itemStats = equippedItem.Stats.ToEquipmentStats();

                totalStats.attackDamageBonus += itemStats.attackDamageBonus;
                totalStats.magicDamageBonus += itemStats.magicDamageBonus;
                totalStats.armorBonus += itemStats.armorBonus;
                totalStats.criticalChanceBonus += itemStats.criticalChanceBonus;
                totalStats.criticalMultiplierBonus += itemStats.criticalMultiplierBonus;
                totalStats.maxHpBonus += itemStats.maxHpBonus;
                totalStats.maxManaBonus += itemStats.maxManaBonus;
                totalStats.moveSpeedBonus += itemStats.moveSpeedBonus;
                totalStats.attackSpeedBonus += itemStats.attackSpeedBonus;
                totalStats.hitRateBonus += itemStats.hitRateBonus;
                totalStats.evasionRateBonus += itemStats.evasionRateBonus;
                totalStats.reductionCoolDownBonus += itemStats.reductionCoolDownBonus;
                totalStats.physicalResistanceBonus += itemStats.physicalResistanceBonus;
                totalStats.magicalResistanceBonus += itemStats.magicalResistanceBonus;

                Debug.Log($"[Character] Added stats from {equippedItem.ItemName}: ATK+{itemStats.attackDamageBonus}, ARM+{itemStats.armorBonus}");
            }
        }

        Debug.Log($"[Character] Total calculated stats: ATK+{totalStats.attackDamageBonus}, ARM+{totalStats.armorBonus}, HP+{totalStats.maxHpBonus}");

        // 🆕 แสดง total stats ทั้งหมดที่มีค่ามากกว่า 0
        List<string> totalStatsList = new List<string>();

        if (totalStats.attackDamageBonus != 0)
            totalStatsList.Add($"ATK+{totalStats.attackDamageBonus}");
        if (totalStats.magicDamageBonus != 0)
            totalStatsList.Add($"MAG+{totalStats.magicDamageBonus}");
        if (totalStats.armorBonus != 0)
            totalStatsList.Add($"ARM+{totalStats.armorBonus}");
        if (totalStats.criticalChanceBonus != 0f)
            totalStatsList.Add($"CRIT+{totalStats.criticalChanceBonus:F1}%");
        if (totalStats.criticalMultiplierBonus != 0f)
            totalStatsList.Add($"CRIT_DMG+{totalStats.criticalMultiplierBonus:F1}%");
        if (totalStats.maxHpBonus != 0)
            totalStatsList.Add($"HP+{totalStats.maxHpBonus}");
        if (totalStats.maxManaBonus != 0)
            totalStatsList.Add($"MP+{totalStats.maxManaBonus}");
        if (totalStats.moveSpeedBonus != 0f)
            totalStatsList.Add($"SPD+{totalStats.moveSpeedBonus:F1}");
        if (totalStats.attackSpeedBonus != 0f)
            totalStatsList.Add($"AS+{totalStats.attackSpeedBonus:F1}%");
        if (totalStats.hitRateBonus != 0f)
            totalStatsList.Add($"HIT+{totalStats.hitRateBonus:F1}%");
        if (totalStats.evasionRateBonus != 0f)
            totalStatsList.Add($"EVA+{totalStats.evasionRateBonus:F1}%");
        if (totalStats.reductionCoolDownBonus != 0f)
            totalStatsList.Add($"CDR+{totalStats.reductionCoolDownBonus:F1}%");
        if (totalStats.physicalResistanceBonus != 0f)
            totalStatsList.Add($"PHYS_RES+{totalStats.physicalResistanceBonus:F1}%");
        if (totalStats.magicalResistanceBonus != 0f)
            totalStatsList.Add($"MAG_RES+{totalStats.magicalResistanceBonus:F1}%");

        string totalStatsString = totalStatsList.Count > 0 ? string.Join(", ", totalStatsList) : "No total stats";
        Debug.Log($"[Character] 📊 TOTAL EQUIPMENT STATS: [{totalStatsString}]");

        return totalStats;
    }

    // เพิ่ม method สำหรับ potion
   
    // เพิ่ม helper methods
    private int GetSlotIndexForItemType(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Head: return 0;
            case ItemType.Armor: return 1;
            case ItemType.Weapon: return 2;
            case ItemType.Pants: return 3;
            case ItemType.Shoes: return 4;
            case ItemType.Rune: return 5;
            case ItemType.Potion:
                // 🆕 สำหรับ potion: หาช่องว่างใน potion slots (0-4)
                return FindEmptyPotionSlot();
            default: return -1;
        }
    }

    private int FindEmptyPotionSlot()
    {
        Debug.Log($"[Character] Searching for empty potion slot in {potionSlots.Count} slots...");

        for (int i = 0; i < potionSlots.Count; i++)
        {
            if (potionSlots[i] == null)
            {
                Debug.Log($"[Character] Found empty potion slot at index {i}");
                return i;
            }
            else
            {
                Debug.Log($"[Character] Potion slot {i}: {potionSlots[i].ItemName}");
            }
        }

        Debug.LogWarning("[Character] No empty potion slot found (all 5 slots full)");
        return -1; // เต็มหมด
    }
    private EquipmentData ConvertItemDataToEquipmentData(ItemData itemData)
    {
        if (itemData == null)
        {
            Debug.LogError("[Character] Cannot convert null ItemData to EquipmentData");
            return null;
        }

        // สร้าง EquipmentData จาก ItemData
        EquipmentData equipmentData = new EquipmentData
        {
            itemName = itemData.ItemName,
            stats = itemData.Stats.ToEquipmentStats(), // ใช้ ToEquipmentStats() ที่มีอยู่แล้ว
            itemIcon = itemData.ItemIcon
        };

        Debug.Log($"[Character] Converted {itemData.ItemName} to EquipmentData with stats");
        return equipmentData;
    }
    // เพิ่ม getter methods
    public ItemData GetEquippedItem(ItemType itemType)
    {
        if (itemType == ItemType.Potion)
        {
            Debug.LogWarning($"[Character] Use GetPotionInSlot() for potion items");
            return null;
        }

        int slotIndex = GetSlotIndexForItemType(itemType);
        Debug.Log($"[Character] GetEquippedItem({itemType}) -> slotIndex: {slotIndex}");

        if (slotIndex >= 0 && slotIndex < characterEquippedItems.Count)
        {
            ItemData item = characterEquippedItems[slotIndex];
            Debug.Log($"[Character] GetEquippedItem result: {(item?.ItemName ?? "NULL")}");
            return item;
        }

        Debug.LogWarning($"[Character] Invalid slot index {slotIndex} for {itemType}");
        return null;
    }

    public ItemData GetPotionInSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < potionSlots.Count)
            return potionSlots[slotIndex];

        return null;
    }

    public bool IsEquipmentSlotEmpty(ItemType itemType)
    {
        ItemData item = GetEquippedItem(itemType);
        return item == null;
    }

    public bool IsPotionSlotEmpty(int slotIndex)
    {
        ItemData potion = GetPotionInSlot(slotIndex);
        return potion == null;
    }

    public List<ItemData> GetAllEquippedItems()
    {
        List<ItemData> allEquipped = new List<ItemData>();

        // เพิ่ม equipment items
        foreach (ItemData item in characterEquippedItems)
        {
            if (item != null)
                allEquipped.Add(item);
        }

        // เพิ่ม potion items
        foreach (ItemData potion in potionSlots)
        {
            if (potion != null)
                allEquipped.Add(potion);
        }

        return allEquipped;
    }

    private void InitializeEquipmentSlots()
    {
        // Initialize equipped items list (6 slots)
        characterEquippedItems.Clear();
        for (int i = 0; i < 6; i++)
        {
            characterEquippedItems.Add(null);
        }

        // Initialize potion slots (5 slots)
        potionSlots.Clear();
        potionStackCounts.Clear();
        for (int i = 0; i < 5; i++)
        {
            potionSlots.Add(null);
            potionStackCounts.Add(0); // 🆕 เริ่มต้นด้วย 0
        }

        Debug.Log($"[Character] Equipment slots initialized: 6 equipment + 5 potion slots with stack counts");
    }

    public EquipmentSlotManager GetEquipmentSlotManager()
    {
        return equipmentSlotManager;
    }
   
    private ItemType GetItemTypeFromSlotIndex(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0: return ItemType.Head;
            case 1: return ItemType.Armor;
            case 2: return ItemType.Weapon;
            case 3: return ItemType.Pants;
            case 4: return ItemType.Shoes;
            case 5: return ItemType.Rune;
            default: return ItemType.Weapon;
        }
    }
    public bool UnequipItemData(ItemType itemType)
    {
        if (itemType == ItemType.Potion)
        {
            Debug.LogWarning($"[Character] Use UnequipPotion() for potion slots");
            return false;
        }

        int slotIndex = GetSlotIndexForItemType(itemType);
        if (slotIndex == -1 || characterEquippedItems[slotIndex] == null)
        {
            Debug.LogWarning($"[Character] No equipped item found for type: {itemType}");
            return false;
        }

        ItemData unequippedItem = characterEquippedItems[slotIndex];
        characterEquippedItems[slotIndex] = null;

        Debug.Log($"[Character] ✅ Unequipped {unequippedItem.ItemName} from {itemType} slot");

        // คำนวณ total stats ใหม่หลังจาก unequip
        ApplyAllEquipmentStats();

        // แจ้ง Event สำหรับ UI
        OnItemUnequippedFromSlot?.Invoke(this, itemType);

        // Force update equipment slots
        ForceUpdateEquipmentSlotsNow();
        PersistentPlayerData.Instance?.SaveInventoryData(this);

        return true;
    }
    public bool UnequipPotionAndReturnToInventory(int potionSlotIndex)
    {
        if (potionSlotIndex < 0 || potionSlotIndex >= potionSlots.Count || potionSlots[potionSlotIndex] == null)
        {
            Debug.LogWarning($"[Character] No potion in slot {potionSlotIndex} to unequip");
            return false;
        }

        ItemData potionToUnequip = potionSlots[potionSlotIndex];
        int stackCount = GetPotionStackCount(potionSlotIndex);

        // Unequip จาก character
        bool unequipSuccess = UnequipPotion(potionSlotIndex);
        if (!unequipSuccess)
        {
            Debug.LogError($"[Character] Failed to unequip potion from slot {potionSlotIndex}");
            return false;
        }

        // เพิ่มกลับไป inventory
        if (inventory != null)
        {
            bool addSuccess = inventory.AddItem(potionToUnequip, stackCount);
            if (addSuccess)
            {
                Debug.Log($"[Character] ✅ Added {potionToUnequip.ItemName} x{stackCount} back to inventory");
                PersistentPlayerData.Instance?.SaveInventoryData(this);
                return true;
            }
            else
            {
                Debug.LogError($"[Character] Failed to add {potionToUnequip.ItemName} back to inventory!");
                // ถ้าใส่ inventory ไม่ได้ ให้ equip กลับ
                potionSlots[potionSlotIndex] = potionToUnequip;
                SetPotionStackCount(potionSlotIndex, stackCount);
                return false;
            }
        }
        else
        {
            Debug.LogError("[Character] No inventory found!");
            // Equip กลับ
            potionSlots[potionSlotIndex] = potionToUnequip;
            SetPotionStackCount(potionSlotIndex, stackCount);
            return false;
        }
    }

    public bool UnequipAndReturnToInventory(ItemType itemType)
    {
        if (itemType == ItemType.Potion)
        {
            Debug.LogWarning($"[Character] Use UnequipPotionAndReturnToInventory() for potion");
            return false;
        }

        // หา equipped item ก่อน unequip
        ItemData equippedItem = GetEquippedItem(itemType);
        if (equippedItem == null)
        {
            Debug.LogWarning($"[Character] No {itemType} equipped to unequip");
            return false;
        }

        // Unequip จาก character
        bool unequipSuccess = UnequipItemData(itemType);
        if (!unequipSuccess)
        {
            Debug.LogError($"[Character] Failed to unequip {itemType}");
            return false;
        }

        // เพิ่มกลับไป inventory
        if (inventory != null)
        {
            bool addSuccess = inventory.AddItem(equippedItem, 1);
            if (addSuccess)
            {
                Debug.Log($"[Character] ✅ Added {equippedItem.ItemName} back to inventory");
                PersistentPlayerData.Instance?.SaveInventoryData(this);
                return true;
            }
            else
            {
                Debug.LogError($"[Character] Failed to add {equippedItem.ItemName} back to inventory!");
                // ถ้าใส่ inventory ไม่ได้ ให้ equip กลับ
                EquipItemData(equippedItem);
                return false;
            }
        }
        else
        {
            Debug.LogError("[Character] No inventory found!");
            // Equip กลับ
            EquipItemData(equippedItem);
            return false;
        }
    }

    public bool UsePotion(int potionSlotIndex)
    {
        // ตรวจสอบ slot index
        if (potionSlotIndex < 0 || potionSlotIndex >= potionSlots.Count || potionSlotIndex >= 5)
        {
            Debug.LogWarning($"[Character] Invalid potion slot index: {potionSlotIndex}");
            return false;
        }

        // ตรวจสอบ cooldown แยกแต่ละ slot
        if (Time.time - lastPotionUseTime[potionSlotIndex] < potionCooldown)
        {
            Debug.LogWarning($"[Character] Potion slot {potionSlotIndex} cooldown not ready! ({Time.time - lastPotionUseTime[potionSlotIndex]:F1}s / {potionCooldown}s)");
            return false;
        }

        // ตรวจสอบว่ามี potion ใน slot หรือไม่
        ItemData potionData = GetPotionInSlot(potionSlotIndex);
        if (potionData == null)
        {
            Debug.LogWarning($"[Character] No potion in slot {potionSlotIndex}");
            return false;
        }

        // ตรวจสอบ stack count
        int currentStackCount = GetPotionStackCount(potionSlotIndex);
        if (currentStackCount <= 0)
        {
            Debug.LogWarning($"[Character] Potion stack depleted in slot {potionSlotIndex}");
            // ล้าง slot ถ้า stack หมด
            potionSlots[potionSlotIndex] = null;
            SetPotionStackCount(potionSlotIndex, 0);
            return false;
        }

        // 🆕 Debug ก่อนใช้ potion
        Debug.Log($"[Character] 🧪 Using {potionData.ItemName} from slot {potionSlotIndex}. Current stack: {currentStackCount}");

        // ใช้ potion
        bool success = ApplyPotionEffects(potionData);
        if (success)
        {
            // ลด stack count
            int newStackCount = currentStackCount - 1;
            SetPotionStackCount(potionSlotIndex, newStackCount);

            // ถ้า stack หมดแล้ว ให้ล้าง slot
            if (newStackCount <= 0)
            {
                potionSlots[potionSlotIndex] = null;
                Debug.Log($"[Character] 🧪 Potion slot {potionSlotIndex} depleted and cleared");
            }

            // อัปเดต cooldown เฉพาะ slot นี้
            lastPotionUseTime[potionSlotIndex] = Time.time;

            Debug.Log($"[Character] ✅ Used {potionData.ItemName} from slot {potionSlotIndex}. Remaining: {newStackCount}");

            // 🆕 แจ้ง UI ให้อัปเดตทันที
            ForceUpdatePotionUI(potionSlotIndex);

            // 🆕 บันทึกข้อมูลทันที (ย้ายเข้ามาใน if success block)
            SavePotionDataAfterUse();

            return true;
        }

        Debug.LogWarning($"[Character] Failed to apply potion effects for {potionData.ItemName}");
        return false;
    }

    private void ForceUpdatePotionUI(int potionSlotIndex)
    {
        try
        {
            Debug.Log($"[Character] 🔄 Force updating potion UI for slot {potionSlotIndex}...");

            // 1. แจ้ง stats changed event
            OnStatsChanged?.Invoke();

            // 2. Force refresh EquipmentSlotManager - เฉพาะ potion slot ที่ใช้
            var equipmentSlotManager = GetComponent<EquipmentSlotManager>();
            if (equipmentSlotManager != null && equipmentSlotManager.IsConnected())
            {
                equipmentSlotManager.ForceRefreshAfterPotionUse(potionSlotIndex);
                Debug.Log($"[Character] ✅ Character EquipmentSlotManager potion slot {potionSlotIndex} refreshed");
            }

            // 3. Force refresh CombatUIManager equipment slots - เฉพาะ potion slot ที่ใช้
            var combatUIManager = FindObjectOfType<CombatUIManager>();
            if (combatUIManager?.equipmentSlotManager != null)
            {
                combatUIManager.equipmentSlotManager.ForceRefreshAfterPotionUse(potionSlotIndex);
                Debug.Log($"[Character] ✅ CombatUI EquipmentSlotManager potion slot {potionSlotIndex} refreshed");
            }

            // 4. Force update Canvas
            Canvas.ForceUpdateCanvases();

            Debug.Log($"[Character] ✅ Potion UI updated for slot {potionSlotIndex}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Character] ❌ Error updating potion UI: {e.Message}");
        }
    }

    private void SavePotionDataAfterUse()
    {
        try
        {
            Debug.Log($"[Character] 💾 Saving potion data after use...");

            // บันทึกข้อมูล potion
            PersistentPlayerData.Instance?.SaveCharacterPotionData(this);

            // บันทึกข้อมูล inventory ด้วย (ในกรณีที่ potion มาจาก inventory)
            PersistentPlayerData.Instance?.SaveInventoryData(this);

            Debug.Log($"[Character] ✅ Potion data saved successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Character] ❌ Error saving potion data: {e.Message}");
        }
    }

    /// <summary>
    /// ใช้ผลของ potion กับตัวละคร
    /// </summary>
    private bool ApplyPotionEffects(ItemData potionData)
    {
        if (potionData?.Stats == null)
        {
            Debug.LogWarning("[Character] Invalid potion data");
            return false;
        }

        ItemStats stats = potionData.Stats;
        bool appliedAnyEffect = false;

        // รักษาพลังชีวิตแบบคงที่
        if (stats.healAmount > 0)
        {
            int oldHp = currentHp;
            currentHp = Mathf.Min(currentHp + stats.healAmount, maxHp);
            Debug.Log($"[Character] 💖 Healed {stats.healAmount} HP: {oldHp} -> {currentHp}");
            appliedAnyEffect = true;
        }

        // รักษาพลังชีวิตแบบเปอร์เซ็นต์
        if (stats.healPercentage > 0)
        {
            int healAmount = Mathf.RoundToInt(maxHp * stats.healPercentage);
            int oldHp = currentHp;
            currentHp = Mathf.Min(currentHp + healAmount, maxHp);
            Debug.Log($"[Character] 💖 Healed {stats.healPercentage:P0} ({healAmount} HP): {oldHp} -> {currentHp}");
            appliedAnyEffect = true;
        }

        // ฟื้นฟูมานาแบบคงที่
        if (stats.manaAmount > 0)
        {
            int oldMana = currentMana;
            currentMana = Mathf.Min(currentMana + stats.manaAmount, maxMana);
            Debug.Log($"[Character] 💙 Restored {stats.manaAmount} MP: {oldMana} -> {currentMana}");
            appliedAnyEffect = true;
        }

        // ฟื้นฟูมานาแบบเปอร์เซ็นต์
        if (stats.manaPercentage > 0)
        {
            int manaAmount = Mathf.RoundToInt(maxMana * stats.manaPercentage);
            int oldMana = currentMana;
            currentMana = Mathf.Min(currentMana + manaAmount, maxMana);
            Debug.Log($"[Character] 💙 Restored {stats.manaPercentage:P0} ({manaAmount} MP): {oldMana} -> {currentMana}");
            appliedAnyEffect = true;
        }

        // ส่ง network update ถ้าเป็น authority
        if (HasStateAuthority && appliedAnyEffect)
        {
            NetworkedCurrentHp = currentHp;
            NetworkedCurrentMana = currentMana;
        }

        return appliedAnyEffect;
    }
    /// <summary>
    /// ตรวจสอบว่า potion สามารถใช้ได้หรือไม่
    /// </summary>
    public bool CanUsePotion(int potionSlotIndex)
    {
        // ตรวจสอบ slot index
        if (potionSlotIndex < 0 || potionSlotIndex >= potionSlots.Count || potionSlotIndex >= 5)
            return false;

        // ตรวจสอบ cooldown เฉพาะ slot นี้
        if (Time.time - lastPotionUseTime[potionSlotIndex] < potionCooldown)
            return false;

        // ตรวจสอบว่ามี potion และมี stack count > 0
        ItemData potionData = GetPotionInSlot(potionSlotIndex);
        if (potionData == null)
            return false;

        int stackCount = GetPotionStackCount(potionSlotIndex);
        return stackCount > 0;
    }

    /// <summary>
    /// ดูเวลา cooldown ที่เหลือสำหรับ slot ที่ระบุ
    /// </summary>
    public float GetPotionCooldownRemaining(int potionSlotIndex)
    {
        if (potionSlotIndex < 0 || potionSlotIndex >= 5)
            return 0f;

        float remaining = potionCooldown - (Time.time - lastPotionUseTime[potionSlotIndex]);
        return Mathf.Max(0f, remaining);
    }

    /// <summary>
    /// ดูเวลา cooldown ที่เหลือสำหรับ slot ที่ระบุ
    /// </summary>

    /// <summary>
    /// ดูเวลา cooldown ที่เหลือ
    /// </summary>
    public bool LoadEquipmentDirectly(ItemData itemData)
    {
        if (itemData == null)
        {
            Debug.LogWarning($"[Character] Cannot load null equipment");
            return false;
        }

        Debug.Log($"[Character] Loading equipment directly: {itemData.ItemName} ({itemData.ItemType})");

        // ตรวจสอบ characterEquippedItems list
        if (characterEquippedItems.Count < 6)
        {
            Debug.LogWarning($"[Character] characterEquippedItems list too small: {characterEquippedItems.Count}");
            InitializeEquipmentSlots();
        }

        int slotIndex = GetSlotIndexForItemType(itemData.ItemType);
        if (slotIndex == -1)
        {
            Debug.LogWarning($"[Character] No slot available for item type: {itemData.ItemType}");
            return false;
        }

        // ใส่ item ลง characterEquippedItems โดยตรง
        characterEquippedItems[slotIndex] = itemData;
        Debug.Log($"[Character] ✅ Loaded {itemData.ItemName} to slot {slotIndex} ({itemData.ItemType})");

        return true;
    }
    public bool LoadPotionDirectly(ItemData potionData, int slotIndex, int stackCount)
    {
        if (potionData == null || slotIndex < 0 || slotIndex >= 5)
        {
            Debug.LogWarning($"[Character] Invalid potion load parameters");
            return false;
        }

        Debug.Log($"[Character] Loading potion directly: {potionData.ItemName} x{stackCount} to slot {slotIndex}");

        // ตรวจสอบ potion lists
        if (potionSlots.Count < 5 || potionStackCounts.Count < 5)
        {
            Debug.LogWarning($"[Character] Potion slots not initialized properly");
            InitializeEquipmentSlots();
        }

        // ใส่ potion ลง slot
        potionSlots[slotIndex] = potionData;
        potionStackCounts[slotIndex] = stackCount;

        Debug.Log($"[Character] ✅ Loaded {potionData.ItemName} x{stackCount} to potion slot {slotIndex}");
        return true;
    }

    public void ClearAllEquipmentForLoad()
    {
        Debug.Log($"[Character] Clearing all equipment for load...");

        // เคลียร์ equipment slots
        if (characterEquippedItems != null)
        {
            for (int i = 0; i < characterEquippedItems.Count; i++)
            {
                characterEquippedItems[i] = null;
            }
        }

        // เคลียร์ potion slots
        if (potionSlots != null)
        {
            for (int i = 0; i < potionSlots.Count; i++)
            {
                potionSlots[i] = null;
            }
        }

        // เคลียร์ potion stack counts
        if (potionStackCounts != null)
        {
            for (int i = 0; i < potionStackCounts.Count; i++)
            {
                potionStackCounts[i] = 0;
            }
        }

        Debug.Log($"[Character] ✅ All equipment cleared for load");
    }

    public void ApplyLoadedEquipmentStats()
    {
        Debug.Log($"[Character] Applying loaded equipment stats...");

        // คำนวณ total stats จาก equipment ทั้งหมด
        ApplyAllEquipmentStats();

        // แจ้ง Event สำหรับ UI
        OnStatsChanged?.Invoke();

        Debug.Log($"[Character] ✅ Applied loaded equipment stats");
    }

    /// <summary>
    /// Force refresh equipment UI หลัง load
    /// </summary>
    public void ForceRefreshEquipmentAfterLoad()
    {
        Debug.Log($"[Character] Force refreshing equipment UI after load...");

        // Force update equipment slots ทันที
        ForceUpdateEquipmentSlotsNow();

        // แจ้ง stats changed
        OnStatsChanged?.Invoke();

        Debug.Log($"[Character] ✅ Equipment UI refreshed after load");
    }

    #endregion

    [ContextMenu("🔍 Debug Current Equipped Items")]
    public void DebugCurrentEquippedItems()
    {
        Debug.Log($"=== EQUIPPED ITEMS DEBUG ({CharacterName}) ===");

        // Equipment slots
        for (int i = 0; i < 6; i++)
        {
            ItemType itemType = GetItemTypeFromSlotIndex(i);
            ItemData equippedItem = GetEquippedItem(itemType);
            Debug.Log($"Slot {i} ({itemType}): {(equippedItem?.ItemName ?? "EMPTY")}");
        }

        // Potion slots
        for (int i = 0; i < 5; i++)
        {
            ItemData potionItem = GetPotionInSlot(i);
            int stackCount = GetPotionStackCount(i);
            Debug.Log($"Potion {i}: {(potionItem?.ItemName ?? "EMPTY")} x{stackCount}");
        }

        Debug.Log("==========================================");
    }
    [ContextMenu("🔍 Debug Loaded Equipment")]
    public void DebugLoadedEquipment()
    {
        Debug.Log($"=== LOADED EQUIPMENT DEBUG ({CharacterName}) ===");

        Debug.Log("📦 Equipment Slots:");
        for (int i = 0; i < 6; i++)
        {
            ItemType itemType = GetItemTypeFromSlotIndex(i);
            ItemData item = i < characterEquippedItems.Count ? characterEquippedItems[i] : null;
            Debug.Log($"  Slot {i} ({itemType}): {(item?.ItemName ?? "EMPTY")}");
        }

        Debug.Log("🧪 Potion Slots:");
        for (int i = 0; i < 5; i++)
        {
            ItemData potion = i < potionSlots.Count ? potionSlots[i] : null;
            int stackCount = i < potionStackCounts.Count ? potionStackCounts[i] : 0;
            Debug.Log($"  Slot {i}: {(potion?.ItemName ?? "EMPTY")} x{stackCount}");
        }

        Debug.Log("==========================================");
    }
}