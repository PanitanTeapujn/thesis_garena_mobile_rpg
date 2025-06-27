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

    protected virtual void Start()
    {
        InitializeStats();
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
    public bool EquipItemData(ItemData itemData)
    {
        if (itemData == null)
        {
            Debug.LogWarning($"[Character] Cannot equip null item");
            return false;
        }

        // หา slot index ที่เหมาะสม
        int slotIndex = GetSlotIndexForItemType(itemData.ItemType);
        if (slotIndex == -1)
        {
            Debug.LogWarning($"[Character] No slot available for item type: {itemData.ItemType}");
            return false;
        }

        // Unequip item เก่าถ้ามี
        if (itemData.ItemType == ItemType.Potion)
        {
            // สำหรับ potion หา slot ว่าง
            int potionSlotIndex = FindEmptyPotionSlot();
            if (potionSlotIndex == -1)
            {
                Debug.LogWarning($"[Character] All potion slots are full");
                return false;
            }

            potionSlots[potionSlotIndex] = itemData;
            Debug.Log($"[Character] Equipped {itemData.ItemName} to potion slot {potionSlotIndex}");
        }
        else
        {
            // Unequip item เก่าถ้ามี
            if (characterEquippedItems[slotIndex] != null)
            {
                UnequipItemData(itemData.ItemType);
            }

            // Equip item ใหม่
            characterEquippedItems[slotIndex] = itemData;

            // ใช้ EquipmentManager เดิมด้วย (ถ้าต้องการ)
            if (equipmentManager != null)
            {
                EquipmentData equipData = ConvertItemDataToEquipmentData(itemData);
                if (equipData != null)
                {
                    equipmentManager.EquipItem(equipData);
                }
            }

            Debug.Log($"[Character] Equipped {itemData.ItemName} to {itemData.ItemType} slot");
        }

        // แจ้ง Event สำหรับ UI
        OnItemEquippedToSlot?.Invoke(this, itemData.ItemType, itemData);

        return true;
    }

    // เพิ่ม method สำหรับ unequip
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

        // ใช้ EquipmentManager เดิมด้วย
        if (equipmentManager != null)
        {
            equipmentManager.UnequipItem();
        }

        // เพิ่มกลับไป inventory
        if (inventory != null)
        {
            inventory.AddItem(unequippedItem, 1);
        }

        // แจ้ง Event สำหรับ UI
        OnItemUnequippedFromSlot?.Invoke(this, itemType);

        Debug.Log($"[Character] Unequipped {unequippedItem.ItemName} from {itemType} slot");
        return true;
    }

    // เพิ่ม method สำหรับ potion
    public bool UnequipPotion(int potionSlotIndex)
    {
        if (potionSlotIndex < 0 || potionSlotIndex >= potionSlots.Count || potionSlots[potionSlotIndex] == null)
        {
            Debug.LogWarning($"[Character] No potion in slot {potionSlotIndex}");
            return false;
        }

        ItemData unequippedPotion = potionSlots[potionSlotIndex];
        potionSlots[potionSlotIndex] = null;

        // เพิ่มกลับไป inventory
        if (inventory != null)
        {
            inventory.AddItem(unequippedPotion, 1);
        }

        Debug.Log($"[Character] Unequipped {unequippedPotion.ItemName} from potion slot {potionSlotIndex}");
        return true;
    }

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
            case ItemType.Potion: return -1; // ใช้ potionSlots แทน
            default: return -1;
        }
    }

    private int FindEmptyPotionSlot()
    {
        for (int i = 0; i < potionSlots.Count; i++)
        {
            if (potionSlots[i] == null)
                return i;
        }
        return -1; // เต็มหมด
    }

    private EquipmentData ConvertItemDataToEquipmentData(ItemData itemData)
    {
        // TODO: แปลง ItemData เป็น EquipmentData สำหรับใช้กับ EquipmentManager เดิม
        // ตอนนี้ return null ไว้ก่อน
        return null;
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
        if (slotIndex >= 0 && slotIndex < characterEquippedItems.Count)
            return characterEquippedItems[slotIndex];

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
        for (int i = 0; i < 5; i++)
        {
            potionSlots.Add(null);
        }

        Debug.Log($"[Character] Equipment slots initialized: 6 equipment + 5 potion slots");
    }
    public EquipmentSlotManager GetEquipmentSlotManager()
    {
        return equipmentSlotManager;
    }

    #endregion
}