using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
public enum AttackType
{
    Physical,    // โจมตีด้วย AttackDamage อย่างเดียว
    Magic,       // โจมตีด้วย MagicDamage อย่างเดียว
    Mixed        // โจมตีทั้งสองแบบ (ระบบเดิม)
}
public class Character : NetworkBehaviour
{
    #region Base Stats

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
    [SerializeField] private float criticalChance = 5f;
    public float CriticalChance { get { return criticalChance; } set { criticalChance = value; } }

    [SerializeField] private float criticalMultiplier = 2f;
    public float CriticalMultiplier { get { return criticalMultiplier; } set { criticalMultiplier = value; } }

    [SerializeField] private float hitRate;
    public float HitRate { get { return hitRate; } set { hitRate = value; } }

    [SerializeField] private float evasionRate;
    public float EvasionRate { get { return evasionRate; } set { evasionRate = value; } }

    [SerializeField] private float attackSpeed;
    public float AttackSpeed { get { return attackSpeed; } set { attackSpeed = value; } }

    [SerializeField] private float reductionCoolDown;
    public float ReductionCoolDown {get{ return reductionCoolDown; }set { reductionCoolDown = value; } }

    [Header("Attack Settings")]
    [SerializeField] private AttackType attackType = AttackType.Physical; // Default เป็น Physical
    public AttackType AttackType { get { return attackType; } set { attackType = value; } }


    #endregion


    [Header("Regeneration Settings")]
    [SerializeField] private float healthRegenPerSecond = 0.5f;
    [SerializeField] private float manaRegenPerSecond = 1f;
    private float healthRegenTimer = 0f;
    private float manaRegenTimer = 0f;
    private float regenTickInterval = 3f; // regen ทุก 1 วินาที
    // ========== Network Properties ==========
    [Networked] public int NetworkedCurrentHp { get; set; }
    [Networked] public int NetworkedMaxHp { get; set; }
    [Networked] public int NetworkedCurrentMana { get; set; }
    [Networked] public int NetworkedMaxMana { get; set; }
    [Networked] public bool IsNetworkStateReady { get; set; }

    [Header("Physics")]
    public Rigidbody rb;

    // ========== Component References ==========
    protected StatusEffectManager statusEffectManager;
    protected CombatManager combatManager;
    protected EquipmentManager equipmentManager;
    protected CharacterVisualManager visualManager;
    protected LevelManager levelManager;

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
            // ✅ ใช้ ScriptableObject เป็นหลักสำหรับทุกตัวละคร รวมทั้ง Assassin
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
            criticalMultiplier = characterStats.criticalMultiplier;
            hitRate = characterStats.hitRate;
            evasionRate = characterStats.evasionRate;
            attackSpeed = characterStats.attackSpeed;
            reductionCoolDown = characterStats.reductionCoolDown;
            attackType = characterStats.attackType;

            Debug.Log($"✅ [Character] Initialized {characterName} with ScriptableObject stats: HP={maxHp}, ATK={attackDamage}");
        }
        else
        {
            Debug.LogWarning($"[Character] No CharacterStats ScriptableObject found for {gameObject.name}!");
        }
    }
    // ========== Fusion Network Methods ==========
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

    // ✅ Method สำหรับ skill ที่ผสม ratio + flat
    public virtual (int physicalDamage, int magicDamage) GetAdvancedSkillDamages(
        float physicalRatio = 0f, float magicRatio = 0f,
        int flatPhysical = 0, int flatMagic = 0)
    {
        int physDamage = Mathf.RoundToInt(AttackDamage * physicalRatio) + flatPhysical;
        int magDamage = Mathf.RoundToInt(MagicDamage * magicRatio) + flatMagic;

        return (physDamage, magDamage);
    }

    public virtual void UseSkillOnTarget(Character target, AttackType skillType, float physicalRatio = 1f, float magicRatio = 1f, DamageType damageType = DamageType.Normal)
    {
        if (combatManager == null) return;

        var (physicalDamage, magicDamage) = GetSkillDamages(skillType, physicalRatio, magicRatio);

        Debug.Log($"[Skill Attack] {CharacterName} uses {skillType} skill: Physical={physicalDamage}, Magic={magicDamage}");

        combatManager.TakeDamageFromAttacker(physicalDamage, magicDamage, this, damageType);
    }

    /// <summary>
    /// ใช้สำหรับ Skills ที่ใช้ flat damage
    /// </summary>
    public virtual void UseSkillOnTarget(Character target, AttackType skillType, int flatPhysical, int flatMagic, DamageType damageType = DamageType.Normal)
    {
        if (combatManager == null) return;

        var (physicalDamage, magicDamage) = GetSkillDamages(skillType, flatPhysical, flatMagic);

        Debug.Log($"[Skill Attack] {CharacterName} uses {skillType} skill: Physical={physicalDamage}, Magic={magicDamage}");

        target.TakeDamageFromAttacker(physicalDamage, magicDamage, this, damageType);
    }

    /// <summary>
    /// ใช้สำหรับ Skills ขั้นสูงที่ผสม ratio + flat
    /// </summary>
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
    // ========== Public Interface Methods ==========

    /// <summary>
    /// ใช้ status effect ผ่าน StatusEffectManager
    /// </summary>
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

    /// <summary>
    /// รับดาเมจผ่าน CombatManager
    /// </summary>
    public virtual void TakeDamage(int damage, DamageType damageType = DamageType.Normal, bool isCritical = false)
    {
        if (combatManager == null) return;
        combatManager.TakeDamage(damage, damageType, isCritical);
    }

    /// <summary>
    /// รับดาเมจจากผู้โจมตี ผ่าน CombatManager
    /// </summary>
    public virtual void TakeDamageFromAttacker(int damage, Character attacker, DamageType damageType = DamageType.Normal)
    {
        if (combatManager == null) return;
        combatManager.TakeDamageFromAttacker(damage, attacker, damageType);
    }

    /// <summary>
    /// ใส่ของผ่าน EquipmentManager
    /// </summary>
    public void EquipItem(EquipmentData equipment)
    {
        if (equipmentManager == null) return;
        equipmentManager.EquipItem(equipment);
    }

    /// <summary>
    /// ถอดของผ่าน EquipmentManager
    /// </summary>
    public void UnequipItem()
    {
        if (equipmentManager == null) return;
        equipmentManager.UnequipItem();
    }


    /// <summary>
    /// ใส่ rune ผ่าน EquipmentManager
    /// </summary>
    public void ApplyRune(EquipmentStats runeStats)
    {
        if (equipmentManager == null) return;
        equipmentManager.ApplyRuneBonus(runeStats);
    }

    /// <summary>
    /// รักษาผ่าน CombatManager
    /// </summary>
    public void Heal(int amount)
    {
        if (combatManager == null) return;
        combatManager.Heal(amount);
    }
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
           // Debug.Log($"[Health Regen] {CharacterName}: {oldHp} -> {currentHp} (+{regenAmount})");
        }
    }

    private void RegenerateMana()
    {
        int regenAmount = 1;
        // 🔧 รวม equipment bonus
        float totalManaRegen = manaRegenPerSecond;

        // เพิ่ม bonus จาก equipment (ถ้ามี)
        if (equipmentManager != null)
        {
            // totalManaRegen += equipmentManager.GetManaRegenBonus();
        }


        int oldMana = currentMana;
        currentMana = Mathf.Min(currentMana + regenAmount, maxMana);

        if (currentMana > oldMana)
        {
            NetworkedCurrentMana = currentMana;
            //Debug.Log($"[Mana Regen] {CharacterName}: {oldMana} -> {currentMana} (+{regenAmount}) [Base: {manaRegenPerSecond}, Total: {totalManaRegen}]");
        }
    }
    // ========== Query Methods ==========

    /// <summary>
    /// เช็คว่ามี status effect หรือไม่
    /// </summary>
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

    /// <summary>
    /// ดู stats รวมจาก equipment
    /// </summary>
    public EquipmentStats GetTotalEquipmentStats()
    {
        if (equipmentManager == null) return new EquipmentStats();
        return equipmentManager.GetTotalStats();
    }

    /// <summary>
    /// ดูเปอร์เซ็นต์เลือด
    /// </summary>
    public float GetHealthPercentage()
    {
        if (combatManager == null) return (float)currentHp / maxHp;
        return combatManager.GetHealthPercentage();
    }

    // ========== Utility Methods ==========
    public bool IsSpawned => Object != null && Object.IsValid;

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
    public float GetEffectiveMoveSpeed()
    {
        float baseMoveSpeed = moveSpeed;

        // ✅ รวม Move Speed Aura
        if (statusEffectManager != null)
        {
            float moveSpeedMultiplier = statusEffectManager.GetTotalMoveSpeedMultiplier();
            baseMoveSpeed *= moveSpeedMultiplier;

            if (moveSpeedMultiplier > 1f)
            {
                Debug.Log($"[Move Speed Aura] {CharacterName} speed boosted by {(moveSpeedMultiplier - 1f) * 100:F0}%");
            }
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

    /// <summary>
    /// ✅ 🌟 เพิ่ม: ดู Attack Speed รวม Aura
    /// </summary>
    public float GetEffectiveAttackSpeed()
    {
        float baseAttackSpeed = attackSpeed;

        // ✅ รวม Attack Speed Aura
        if (statusEffectManager != null)
        {
            float attackSpeedMultiplier = statusEffectManager.GetTotalAttackSpeedMultiplier();
            baseAttackSpeed *= attackSpeedMultiplier;

            if (attackSpeedMultiplier > 1f)
            {
                Debug.Log($"[Attack Speed Aura] {CharacterName} attack speed boosted by {(attackSpeedMultiplier - 1f) * 100:F0}%");
            }
        }

        return baseAttackSpeed;
    }

    /// <summary>
    /// รับดาเมจจากผู้โจมตี แยก Physical และ Magic damage
    /// </summary>
    public virtual void TakeDamageFromAttacker(int physicalDamage, int magicDamage, Character attacker, DamageType damageType = DamageType.Normal)
    {
        if (combatManager == null) return;
        combatManager.TakeDamageFromAttacker(physicalDamage, magicDamage, attacker, damageType);
    }
    protected virtual bool CanDie()
    {
        return NetworkedCurrentHp <= 0;
    }
    /// ดู level ปัจจุบัน
    public float GetEffectiveCriticalMultiplier()
    {
        float baseCritMultiplier = criticalMultiplier;

        // เพิ่ม bonus จาก equipment
        if (equipmentManager != null)
        {
            baseCritMultiplier += equipmentManager.GetCriticalMultiplierBonus();
        }

        return baseCritMultiplier;
    }
    public int GetCurrentLevel()
    {
        if (levelManager == null) return 1;
        return levelManager.CurrentLevel;
    }
    /// ดู exp ปัจจุบัน
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
    /// เช็คว่าถึง max level แล้วหรือยัง

    public bool IsMaxLevel()
    {
        if (levelManager == null) return false;
        return levelManager.IsMaxLevel();
    }
    // ========== Debug Methods ==========
  

}