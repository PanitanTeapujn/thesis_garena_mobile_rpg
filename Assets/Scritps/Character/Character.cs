using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public enum StatusEffectType
{
    None,
    Poison,
    Burn,
    Freeze,
    Stun,
    Bleed
}

public enum DamageType
{
    Normal,
    Critical,
    Magic,
    Poison,
    Burn,
    Freeze,
    Stun,
    Bleed
}
[System.Serializable]
public class StatusResistance
{
    [Header("Physical Resistance (Stun, Armor Break, Blind, Weakness)")]
    [Range(0f, 80f)] public float physicalResistance = 0f;

    [Header("Magical Resistance (Poison, Burn, Bleed, Freeze)")]
    [Range(0f, 80f)] public float magicalResistance = 0f;

    [Header("Equipment & Rune Bonuses")]
    public float equipmentPhysicalBonus = 0f;
    public float equipmentMagicalBonus = 0f;
    public float runePhysicalBonus = 0f;
    public float runeMagicalBonus = 0f;

    // คำนวณค่า resistance รวม
    public float GetTotalPhysicalResistance()
    {
        return Mathf.Clamp(physicalResistance + equipmentPhysicalBonus + runePhysicalBonus, 0f, 80f);
    }

    public float GetTotalMagicalResistance()
    {
        return Mathf.Clamp(magicalResistance + equipmentMagicalBonus + runeMagicalBonus, 0f, 80f);
    }
}
public class Character : NetworkBehaviour
{
    [Header("Base Stats")]
    public CharacterStats characterStats;

    [SerializeField]
    private string characterName;
    public string CharacterName { get { return characterName; } }

    [SerializeField]
    private int currentHp;
    public int CurrentHp { get { return currentHp; } set { currentHp = value; } }

    [SerializeField]
    private int maxHp;
    public int MaxHp { get { return maxHp; } set { maxHp = value; } }

    [SerializeField]
    private int currentMana;
    public int CurrentMana { get { return currentMana; } set { currentMana = value; } }

    [SerializeField]
    private int maxMana;
    public int MaxMana { get { return maxMana; } set { maxMana = value; } }

    [SerializeField]
    private int attackDamage;
    public int AttackDamage { get { return attackDamage; } set { attackDamage = value; } }

    [SerializeField]
    private int armor;
    public int Armor { get { return armor; } set { armor = value; } }

    [SerializeField]
    private float moveSpeed;
    public float MoveSpeed { get { return moveSpeed; } set { moveSpeed = value; } }

    [SerializeField]
    private float attackRange;
    public float AttackRange { get { return attackRange; } set { attackRange = value; } }

    [SerializeField]
    private float attackCooldown;
    public float AttackCooldown { get { return attackCooldown; } set { attackCooldown = value; } }

    [Header("Critical Stats")]
    [SerializeField]
    private float criticalChance = 5f;
    public float CriticalChance { get { return criticalChance; } set { criticalChance = value; } }

    [SerializeField]
    private float criticalMultiplier = 2f;
    public float CriticalMultiplier { get { return criticalMultiplier; } set { criticalMultiplier = value; } }


    

    // ========== Network Properties ==========
    [Networked] public int NetworkedCurrentHp { get; set; }
    [Networked] public int NetworkedMaxHp { get; set; }
    [Networked] public int NetworkedCurrentMana { get; set; }
    [Networked] public int NetworkedMaxMana { get; set; }
    [Networked] public bool IsNetworkStateReady { get; set; }


    [Header("Status Resistance")]
    public StatusResistance statusResistance = new StatusResistance();
    [Networked] public bool IsArmorBreak { get; set; }
    [Networked] public float ArmorBreakDuration { get; set; }
    [Networked] public float ArmorBreakAmount { get; set; } // 0.5 = 50% reduction

    // Blind Status  
    [Networked] public bool IsBlind { get; set; }
    [Networked] public float BlindDuration { get; set; }
    [Networked] public float BlindAmount { get; set; } // 0.8 = 80% reduction

    // Weakness Status
    [Networked] public bool IsWeak { get; set; }
    [Networked] public float WeaknessDuration { get; set; }
    [Networked] public float WeaknessAmount { get; set; } // 0.4 = 40% reduction

    // ========== เพิ่มใน Visual Headers ==========
    [Header("Physical Status Effects Colors")]
    public Color armorBreakColor = new Color(0.8f, 0.8f, 0.3f); // สีน้ำตาลทอง
    public Color blindColor = new Color(0.2f, 0.2f, 0.2f);      // สีเทาเข้ม
    public Color weaknessColor = new Color(0.6f, 0.4f, 0.8f);   // สีม่วงอ่อน

    public GameObject armorBreakVFX;
    public GameObject blindVFX;
    public GameObject weaknessVFX;

    // เพิ่ม flags สำหรับ flash effects
    private bool isFlashingFromArmorBreak = false;
    private bool isFlashingFromBlind = false;
    private bool isFlashingFromWeakness = false;


    // ========== Network Status Effects ==========
    [Networked] public bool IsPoisoned { get; set; }
    [Networked] public float PoisonDuration { get; set; }
    [Networked] public int PoisonDamagePerTick { get; set; }
    [Networked] public TickTimer PoisonTickTimer { get; set; }

    [Networked] public bool IsBurning { get; set; }
    [Networked] public float BurnDuration { get; set; }
    [Networked] public int BurnDamagePerTick { get; set; }
    [Networked] public float BurnNextTickTime { get; set; }

    // Freeze Status
    [Networked] public bool IsFrozen { get; set; }
    [Networked] public float FreezeDuration { get; set; }
    [Networked] public float OriginalMoveSpeed { get; set; }

    // Stun Status
    [Networked] public bool IsStunned { get; set; }
    [Networked] public float StunDuration { get; set; }

    // Bleed Status
    [Networked] public bool IsBleeding { get; set; }
    [Networked] public float BleedDuration { get; set; }
    [Networked] public int BleedDamagePerTick { get; set; }
    [Networked] public float BleedNextTickTime { get; set; }
    // 🆕 เพิ่ม manual timer สำหรับ poison
    [Networked] public float PoisonNextTickTime { get; set; }

    [Header("Physics")]
    public Rigidbody rb;

    [Header("Visual")]
    public Renderer characterRenderer;
    public Color originalColor;

    [Header("Status Effects ")]
    public Color poisonColor = new Color(0.5f, 1f, 0.5f); // สีเขียวอ่อน
    public GameObject poisonVFX;
    private float poisonTickInterval = 1f; // ทุก 1 วินาที
    public Color burnColor = new Color(1f, 0.3f, 0f);     // สีส้มแดง
    public Color freezeColor = new Color(0.3f, 0.8f, 1f); // สีน้ำเงินอ่อน
    public Color stunColor = new Color(1f, 1f, 0f);       // สีเหลือง
    public Color bleedColor = new Color(0.7f, 0f, 0f);    // สีแดงเข้ม
    public GameObject burnVFX;
    public GameObject freezeVFX;
    public GameObject stunVFX;
    public GameObject bleedVFX;
    private float burnTickInterval = 0.5f;   // ทุก 0.5 วินาที
    private float bleedTickInterval = 0.7f;  // ทุก 0.7 วินาที

    // 🔧 เพิ่มตัวแปรสำหรับจัดการสี
    private bool isTakingDamage = false;
    private bool isFlashingFromPoison = false; // 🆕 ป้องกัน UpdateVisualEffects override
    private bool isFlashingFromBurn = false;
    private bool isFlashingFromFreeze = false;
    private bool isFlashingFromStun = false;
    private bool isFlashingFromBleed = false;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        characterRenderer = GetComponent<Renderer>();
        if (characterRenderer != null)
        {
            originalColor = characterRenderer.material.color;
        }
    }

    protected virtual void Start()
    {
        characterName = characterStats.characterName;
        maxHp = characterStats.maxHp;
        currentHp = maxHp;
        maxMana = characterStats.maxMana;
        currentMana = maxMana;
        attackDamage = characterStats.attackDamage;
        armor = characterStats.arrmor;
        moveSpeed = characterStats.moveSpeed;
        attackRange = characterStats.attackRange;
        attackCooldown = characterStats.attackCoolDown;
        criticalChance = characterStats.criticalChance;
        criticalMultiplier = characterStats.criticalMultiplier;
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

        // Initialize VFX
        if (poisonVFX != null)
            poisonVFX.SetActive(false);
        if (poisonVFX != null) poisonVFX.SetActive(false);
        if (burnVFX != null) burnVFX.SetActive(false);
        if (freezeVFX != null) freezeVFX.SetActive(false);
        if (stunVFX != null) stunVFX.SetActive(false);
        if (bleedVFX != null) bleedVFX.SetActive(false);
        if (armorBreakVFX != null) armorBreakVFX.SetActive(false);
        if (blindVFX != null) blindVFX.SetActive(false);
        if (weaknessVFX != null) weaknessVFX.SetActive(false);

        // Set default resistance values
        statusResistance.physicalResistance = 100f; // 5% base
        statusResistance.magicalResistance = 5f;  // 5% base
    }

    protected virtual void Update()
    {
        HandleStatusEffects();
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

            // 🔧 ใช้ manual timer แทน TickTimer
            if (IsPoisoned)
            {
                float currentTime = (float)Runner.SimulationTime;
                //Debug.Log($"[POISON DEBUG] {CharacterName} - Duration: {PoisonDuration:F2}, Current Time: {currentTime:F2}, Next Tick Time: {PoisonNextTickTime:F2}");

                if (currentTime >= PoisonNextTickTime)
                {
                  //  Debug.Log($"[Poison Tick] {CharacterName} applying poison damage!");
                    ApplyPoisonDamage();
                    // ตั้งเวลาสำหรับ tick ถัดไป
                    PoisonNextTickTime = currentTime + poisonTickInterval;
                   // Debug.Log($"[Poison Tick] {CharacterName} next poison tick at: {PoisonNextTickTime:F2}");
                }
            }

            // Check if poison duration expired
            if (IsPoisoned && PoisonDuration > 0)
            {
                PoisonDuration -= Runner.DeltaTime;
                if (PoisonDuration <= 0)
                {
                   // Debug.Log($"[Poison] {CharacterName} poison duration expired, removing poison");
                    RemovePoison();
                }
            }
            if (IsBurning)
            {
                float currentTime = (float)Runner.SimulationTime;

                if (currentTime >= BurnNextTickTime)
                {
                    ApplyBurnDamage();
                    BurnNextTickTime = currentTime + burnTickInterval;
                }

                BurnDuration -= Runner.DeltaTime;
                if (BurnDuration <= 0)
                {
                    RemoveBurn();
                }
            }

            // ❄️ Handle Freeze Status
            if (IsFrozen)
            {
                FreezeDuration -= Runner.DeltaTime;
                if (FreezeDuration <= 0)
                {
                    RemoveFreeze();
                }
            }

            // ⚡ Handle Stun Status
            if (IsStunned)
            {
                StunDuration -= Runner.DeltaTime;
                if (StunDuration <= 0)
                {
                    RemoveStun();
                }
            }

            // 🩸 Handle Bleed Status
            if (IsBleeding)
            {
                float currentTime = (float)Runner.SimulationTime;

                if (currentTime >= BleedNextTickTime)
                {
                    ApplyBleedDamage();
                    BleedNextTickTime = currentTime + bleedTickInterval;
                }

                BleedDuration -= Runner.DeltaTime;
                if (BleedDuration <= 0)
                {
                    RemoveBleed();
                }
            }
            if (IsArmorBreak)
            {
                ArmorBreakDuration -= Runner.DeltaTime;
                if (ArmorBreakDuration <= 0)
                {
                    RemoveArmorBreak();
                }
            }

            if (IsBlind)
            {
                BlindDuration -= Runner.DeltaTime;
                if (BlindDuration <= 0)
                {
                    RemoveBlind();
                }
            }

            if (IsWeak)
            {
                WeaknessDuration -= Runner.DeltaTime;
                if (WeaknessDuration <= 0)
                {
                    RemoveWeakness();
                }
            }
        }
    }

    #region  // ========== Network Damage System ==========
    public virtual void TakeDamageFromAttacker(int damage, Character attacker, DamageType damageType = DamageType.Normal)
    {
        if (!HasStateAuthority && !HasInputAuthority) return;

        // 🎯 คำนวณ critical จาก attacker's stats
        bool isCritical = false;
        int finalDamage = damage;

        if (attacker != null)
        {
            // 💪 ตรวจสอบ Weakness effect ของ attacker
            if (attacker.IsWeak)
            {
                finalDamage = Mathf.RoundToInt(damage * (1f - attacker.WeaknessAmount));
                Debug.Log($"[Weakness Effect] {attacker.CharacterName} damage reduced from {damage} to {finalDamage} ({attacker.WeaknessAmount * 100}% reduction)");
            }

            float critRoll = Random.Range(0f, 100f);
            float attackerCritChance = attacker.CriticalChance;

            // 👁️ ตรวจสอบ Blind effect ของ attacker
            if (attacker.IsBlind)
            {
                attackerCritChance *= (1f - attacker.BlindAmount);
                Debug.Log($"[Blind Effect] {attacker.CharacterName} critical chance reduced to {attackerCritChance:F1}%");
            }

            isCritical = critRoll < attackerCritChance;
            Debug.Log($"[Critical Check] {attacker.CharacterName} rolls {critRoll:F1}% vs {attackerCritChance:F1}% = {(isCritical ? "CRITICAL!" : "Normal")}");
        }

        // เรียก TakeDamage เดิมที่มีการคำนวณ damage แล้ว
        TakeDamage(finalDamage, damageType, isCritical);
    }
    public virtual void TakeDamage(int damage, DamageType damageType = DamageType.Normal, bool isCritical = false)
    {
        if (!HasStateAuthority && !HasInputAuthority) return;

        int finalDamage = CalculateDamage(damage, isCritical);

        int oldHp = currentHp;
        currentHp -= finalDamage;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);

        Debug.Log($"[TakeDamage] {CharacterName}: {oldHp} -> {currentHp} (damage: {finalDamage}, type: {damageType}, critical: {isCritical})");

        // Sync based on authority
        if (HasStateAuthority)
        {
            NetworkedCurrentHp = currentHp;
            RPC_BroadcastHealthUpdate(currentHp);

            // 🔧 เฉพาะ StateAuthority เท่านั้นที่เรียก damage flash
            RPC_TriggerDamageFlash(damageType, isCritical);

            // 🔧 เฉพาะ StateAuthority เท่านั้นที่เรียก death
            if (currentHp <= 0)
            {
                RPC_OnDeath();
            }
        }
        else if (HasInputAuthority)
        {
            RPC_UpdateHealth(currentHp);

            // 🔧 InputAuthority ส่งไปให้ StateAuthority handle
            RPC_RequestDamageFlash(damageType, isCritical);

            // 🔧 InputAuthority ส่งไปให้ StateAuthority handle death
            if (currentHp <= 0)
            {
                RPC_RequestDeath();
            }
        }
    }
    protected virtual int CalculateDamage(int damage, bool isCritical)
    {
        // คำนวณ critical และ armor เหมือนเดิม
        if (isCritical)
        {
            // 👁️ ตรวจสอบ Blind effect ก่อนทำ critical
            if (IsBlind)
            {
                // ลดโอกาส critical ตาม blind amount
                float blindReduction = BlindAmount; // 0.8 = 80%
                float newCritChance = criticalChance * (1f - blindReduction);

                // Roll ใหม่ด้วย critical chance ที่ลดลง
                if (Random.Range(0f, 100f) >= newCritChance)
                {
                    isCritical = false; // ไม่ critical เพราะ blind
                    Debug.Log($"[Blind Effect] Critical blocked by blind! ({blindReduction * 100}% reduction)");
                }
            }

            if (isCritical)
            {
                int criticalDamage = Mathf.RoundToInt(damage * criticalMultiplier);
                Debug.Log($"[CalculateDamage] Critical Hit! {damage} * {criticalMultiplier} = {criticalDamage} (ignoring armor)");
                return criticalDamage;
            }
        }

        // คำนวณ armor ปกติ + armor break effect
        int currentArmor = armor;

        // 🛡️ ตรวจสอบ Armor Break effect
        if (IsArmorBreak)
        {
            currentArmor = Mathf.RoundToInt(currentArmor * (1f - ArmorBreakAmount));
            Debug.Log($"[Armor Break Effect] Armor reduced from {armor} to {currentArmor} ({ArmorBreakAmount * 100}% reduction)");
        }

        int damageAfterArmor = damage - currentArmor;
        int finalDamage = Mathf.Max(1, damageAfterArmor);
        Debug.Log($"[CalculateDamage] Normal Hit: {damage} - {currentArmor} armor = {finalDamage}");
        return finalDamage;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_UpdateHealth(int newHp)
    {
        currentHp = newHp;
        NetworkedCurrentHp = newHp;
        Debug.Log($"[RPC_UpdateHealth] Health updated: {newHp} for {CharacterName}");

        RPC_BroadcastHealthUpdate(newHp);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastHealthUpdate(int newHp)
    {
        NetworkedCurrentHp = newHp;
        Debug.Log($"[RPC_BroadcastHealthUpdate] Health broadcasted: {newHp} for {CharacterName}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]  // 🔧 เปลี่ยนเป็น StateAuthority
    private void RPC_TriggerDamageFlash(DamageType damageType, bool isCritical)
    {
        StartCoroutine(NetworkDamageFlash(damageType, isCritical));
    }

    // 🆕 เพิ่ม RPC สำหรับ InputAuthority ขอให้ StateAuthority ทำ damage flash
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestDamageFlash(DamageType damageType, bool isCritical)
    {
        // StateAuthority รับคำขอแล้วส่ง flash ให้ทุกคน
        RPC_TriggerDamageFlash(damageType, isCritical);
    }

    // 🆕 เพิ่ม RPC สำหรับ InputAuthority ขอให้ StateAuthority handle death
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestDeath()
    {
        // StateAuthority รับคำขอแล้วส่ง death ให้ทุกคน
        RPC_OnDeath();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]  // 🔧 เปลี่ยนเป็น StateAuthority
    private void RPC_OnDeath()
    {
        Debug.Log($"{CharacterName} died!");
        // Handle death logic here
    }
    #endregion
    // ========== Network Damage Flash - Everyone Can See ==========
    protected virtual IEnumerator NetworkDamageFlash(DamageType damageType, bool isCritical)
    {
        if (characterRenderer == null) yield break;

        isTakingDamage = true;
        Color flashColor = GetFlashColorByDamageType(damageType, isCritical);

        // Store current color
        Color currentColor = characterRenderer.material.color;

        // Apply flash color
        characterRenderer.material.color = flashColor;

        // Flash duration
        float flashDuration = isCritical ? 0.3f : 0.2f;
        yield return new WaitForSeconds(flashDuration);

        // Return to appropriate color
        Color returnColor = GetReturnColor();
        characterRenderer.material.color = returnColor;

        isTakingDamage = false;
    }

    private Color GetFlashColorByDamageType(DamageType damageType, bool isCritical)
    {
        switch (damageType)
        {
            case DamageType.Poison:
                return new Color(0.8f, 0f, 0.8f); // สีม่วง
            case DamageType.Burn:
                return new Color(1f, 0.3f, 0f); // สีส้มแดง
            case DamageType.Freeze:
                return new Color(0.3f, 0.8f, 1f); // สีน้ำเงินอ่อน
            case DamageType.Stun:
                return new Color(1f, 1f, 0f); // สีเหลือง
            case DamageType.Bleed:
                return new Color(0.7f, 0f, 0f); // สีแดงเข้ม
            case DamageType.Critical:
                return new Color(1f, 0.8f, 0f); // สีทอง
            default:
                return isCritical ? new Color(1f, 0.5f, 0f) : Color.red;
        }
    }

    private Color GetReturnColor()
    {
        // ลำดับความสำคัญของสี (สถานะที่สำคัญกว่าจะแสดงก่อน)
        if (IsStunned) return stunColor;           // ⚡ Stun - สำคัญที่สุด
        if (IsFrozen) return freezeColor;          // ❄️ Freeze
        if (IsArmorBreak) return armorBreakColor;  // 🛡️ Armor Break
        if (IsBlind) return blindColor;            // 👁️ Blind
        if (IsWeak) return weaknessColor;          // 💪 Weakness
        if (IsPoisoned) return poisonColor;        // 🧪 Poison
        if (IsBurning) return burnColor;           // 🔥 Burn
        if (IsBleeding) return bleedColor;         // 🩸 Bleed

        return originalColor;
    }

    // ========== Poison Status Effect System ==========
    #region Status Effect Magic
    public virtual void ApplyPoison(int damagePerTick, float duration)
    {
        if (!HasStateAuthority) return;

        // 🧪 คำนวณ resistance (Magical)
        float totalResistance = statusResistance.GetTotalMagicalResistance();

        // ตรวจสอบโอกาสป้องกัน
        float chanceReduction = totalResistance * 0.6f; // 60% ของ resistance
        if (Random.Range(0f, 100f) < chanceReduction)
        {
            Debug.Log($"[Poison Resisted] {CharacterName} resisted poison! ({chanceReduction:F1}% chance)");
            return;
        }

        // ลดระยะเวลาและดาเมจตาม resistance
        float durationReduction = totalResistance / 100f;
        float damageReduction = (totalResistance * 0.5f) / 100f; // 50% ของ resistance

        duration = duration * (1f - durationReduction);
        damagePerTick = Mathf.RoundToInt(damagePerTick * (1f - damageReduction));

        bool wasAlreadyPoisoned = IsPoisoned;

        IsPoisoned = true;
        PoisonDamagePerTick = Mathf.Max(1, damagePerTick);
        PoisonDuration = Mathf.Max(0.5f, duration);

        float currentTime = (float)Runner.SimulationTime;
        if (!wasAlreadyPoisoned)
        {
            PoisonNextTickTime = currentTime + 0.1f;
        }

        Debug.Log($"[ApplyPoison] {CharacterName} is poisoned! {PoisonDamagePerTick} damage per {poisonTickInterval}s for {PoisonDuration:F1}s (Resistance: {totalResistance:F1}%)");
        RPC_ShowPoisonEffect(true);
    }

    private void ApplyPoisonDamage()
    {
        if (!IsPoisoned) return;

        Debug.Log($"[ApplyPoisonDamage] {CharacterName} about to take {PoisonDamagePerTick} poison damage");

        // ลดเลือดโดยตรงเหมือนโค้ดเก่า แล้ว sync network
        int oldHp = currentHp;
        currentHp -= PoisonDamagePerTick;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);

        // Update network properties
        NetworkedCurrentHp = currentHp;

        Debug.Log($"[Poison] {CharacterName} takes {PoisonDamagePerTick} poison damage. HP: {currentHp}/{maxHp} (was {oldHp})");

        // 🔧 ใช้ RPC สำหรับ poison damage flash แทน coroutine local
        RPC_TriggerPoisonFlash();

        // Broadcast health change to all clients
        RPC_BroadcastHealthUpdate(currentHp);

        if (currentHp <= 0)
        {
            RPC_OnDeath();
        }
    }

    // 🆕 เพิ่ม RPC สำหรับ poison flash
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TriggerPoisonFlash()
    {
        StartCoroutine(PoisonDamageFlash());
    }

    // 🔧 ปรับปรุง PoisonDamageFlash ให้ใช้ flag
    protected virtual IEnumerator PoisonDamageFlash()
    {
        if (characterRenderer == null) yield break;

        // 🔧 ตั้งค่า flag เพื่อป้องกัน UpdateVisualEffects() override
        isFlashingFromPoison = true;

        // บันทึกสีปัจจุบัน
        Color currentColor = characterRenderer.material.color;

        // เปลี่ยนเป็นสีแดงชั่วคราว
        characterRenderer.material.color = Color.red;

        // รอเวลาสั้นๆ
        yield return new WaitForSeconds(0.2f);

        // เปลี่ยนกลับเป็นสีเดิม (สีพิษ หรือสีปกติ)
        if (IsPoisoned)
        {
            characterRenderer.material.color = poisonColor;
        }
        else
        {
            characterRenderer.material.color = originalColor;
        }

        // 🔧 ปิด flag
        isFlashingFromPoison = false;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowPoisonEffect(bool show)
    {
        if (poisonVFX != null)
        {
            poisonVFX.SetActive(show);
        }

        // 🔧 เปลี่ยนสีทันทีเหมือนโค้ดเก่า แต่เช็ค isFlashingFromPoison ก่อน
        if (characterRenderer != null && !isFlashingFromPoison)
        {
            if (show)
            {
                characterRenderer.material.color = poisonColor;
            }
            else
            {
                characterRenderer.material.color = originalColor;
            }
        }
    }
    public virtual void RemovePoison()
    {
        if (!HasStateAuthority) return;

        bool wasPoisoned = IsPoisoned;
        IsPoisoned = false;
        PoisonDuration = 0f;
        PoisonDamagePerTick = 0;
        PoisonNextTickTime = 0f; // 🔧 รีเซ็ต manual timer

        Debug.Log($"{CharacterName} is no longer poisoned (was poisoned: {wasPoisoned})");

        // Remove visual effects for all clients
        RPC_ShowPoisonEffect(false);
    }
    public virtual void ApplyBurn(int damagePerTick, float duration)
    {
        if (!HasStateAuthority) return;

        // 🔥 คำนวณ resistance (Magical)
        float totalResistance = statusResistance.GetTotalMagicalResistance();

        // ตรวจสอบโอกาสป้องกัน
        float chanceReduction = totalResistance * 0.6f;
        if (Random.Range(0f, 100f) < chanceReduction)
        {
            Debug.Log($"[Burn Resisted] {CharacterName} resisted burn! ({chanceReduction:F1}% chance)");
            return;
        }

        // ลดระยะเวลาและดาเมจตาม resistance
        float durationReduction = totalResistance / 100f;
        float damageReduction = (totalResistance * 0.5f) / 100f;

        duration = duration * (1f - durationReduction);
        damagePerTick = Mathf.RoundToInt(damagePerTick * (1f - damageReduction));

        bool wasAlreadyBurning = IsBurning;

        IsBurning = true;
        BurnDamagePerTick = Mathf.Max(1, damagePerTick);
        BurnDuration = Mathf.Max(0.5f, duration);

        float currentTime = (float)Runner.SimulationTime;
        if (!wasAlreadyBurning)
        {
            BurnNextTickTime = currentTime + 0.1f;
        }

        Debug.Log($"[ApplyBurn] {CharacterName} is burning! {BurnDamagePerTick} damage per {burnTickInterval}s for {BurnDuration:F1}s (Resistance: {totalResistance:F1}%)");
        RPC_ShowBurnEffect(true);
    }

    private void ApplyBurnDamage()
    {
        if (!IsBurning) return;

        int oldHp = currentHp;
        currentHp -= BurnDamagePerTick;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);

        NetworkedCurrentHp = currentHp;
        Debug.Log($"[Burn] {CharacterName} takes {BurnDamagePerTick} burn damage. HP: {currentHp}/{maxHp}");

        RPC_TriggerBurnFlash();
        RPC_BroadcastHealthUpdate(currentHp);

        if (currentHp <= 0)
        {
            RPC_OnDeath();
        }
    }

    public virtual void RemoveBurn()
    {
        if (!HasStateAuthority) return;

        IsBurning = false;
        BurnDuration = 0f;
        BurnDamagePerTick = 0;
        BurnNextTickTime = 0f;

        Debug.Log($"{CharacterName} is no longer burning");
        RPC_ShowBurnEffect(false);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowBurnEffect(bool show)
    {
        if (burnVFX != null)
        {
            burnVFX.SetActive(show);
        }

        if (characterRenderer != null && !isFlashingFromBurn && !isTakingDamage)
        {
            if (show)
            {
                characterRenderer.material.color = burnColor;
            }
            else
            {
                characterRenderer.material.color = GetReturnColor();
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TriggerBurnFlash()
    {
        StartCoroutine(BurnDamageFlash());
    }

    protected virtual IEnumerator BurnDamageFlash()
    {
        if (characterRenderer == null) yield break;

        isFlashingFromBurn = true;
        characterRenderer.material.color = Color.red;
        yield return new WaitForSeconds(0.2f);

        if (IsBurning)
        {
            characterRenderer.material.color = burnColor;
        }
        else
        {
            characterRenderer.material.color = GetReturnColor();
        }

        isFlashingFromBurn = false;
    }

    // ========== ❄️ Freeze Status Effect System ==========
    public virtual void ApplyFreeze(float duration)
    {
        if (!HasStateAuthority) return;

        // ❄️ คำนวณ resistance (Magical)
        float totalResistance = statusResistance.GetTotalMagicalResistance();

        // ตรวจสอบโอกาสป้องกัน
        float chanceReduction = totalResistance * 0.6f;
        if (Random.Range(0f, 100f) < chanceReduction)
        {
            Debug.Log($"[Freeze Resisted] {CharacterName} resisted freeze! ({chanceReduction:F1}% chance)");
            return;
        }

        // ลดระยะเวลาตาม resistance
        float durationReduction = totalResistance / 100f;
        duration = duration * (1f - durationReduction);

        bool wasAlreadyFrozen = IsFrozen;

        IsFrozen = true;
        FreezeDuration = Mathf.Max(0.5f, duration);

        if (!wasAlreadyFrozen)
        {
            OriginalMoveSpeed = moveSpeed;
            moveSpeed *= 0.3f; // ลดความเร็วเหลือ 30%

            if (rb != null)
            {
                rb.velocity = Vector3.zero;
            }
        }

        Debug.Log($"[ApplyFreeze] {CharacterName} is frozen for {FreezeDuration:F1}s! Move speed: {moveSpeed} (Resistance: {totalResistance:F1}%)");
        RPC_ShowFreezeEffect(true);
    }

    public virtual void RemoveFreeze()
    {
        if (!HasStateAuthority) return;

        IsFrozen = false;
        FreezeDuration = 0f;

        // คืนค่าความเร็ว
        moveSpeed = OriginalMoveSpeed;

        Debug.Log($"{CharacterName} is no longer frozen. Move speed restored: {moveSpeed}");
        RPC_ShowFreezeEffect(false);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowFreezeEffect(bool show)
    {
        if (freezeVFX != null)
        {
            freezeVFX.SetActive(show);
        }

        if (characterRenderer != null && !isFlashingFromFreeze && !isTakingDamage)
        {
            if (show)
            {
                characterRenderer.material.color = freezeColor;
            }
            else
            {
                characterRenderer.material.color = GetReturnColor();
            }
        }
    }

    // ========== ⚡ Stun Status Effect System ==========
   

    // ========== 🩸 Bleed Status Effect System ==========
    public virtual void ApplyBleed(int damagePerTick, float duration)
    {
        if (!HasStateAuthority) return;

        // 🩸 คำนวณ resistance (Magical)
        float totalResistance = statusResistance.GetTotalMagicalResistance();

        // ตรวจสอบโอกาสป้องกัน
        float chanceReduction = totalResistance * 0.6f;
        if (Random.Range(0f, 100f) < chanceReduction)
        {
            Debug.Log($"[Bleed Resisted] {CharacterName} resisted bleed! ({chanceReduction:F1}% chance)");
            return;
        }

        // ลดระยะเวลาและดาเมจตาม resistance
        float durationReduction = totalResistance / 100f;
        float damageReduction = (totalResistance * 0.5f) / 100f;

        duration = duration * (1f - durationReduction);
        damagePerTick = Mathf.RoundToInt(damagePerTick * (1f - damageReduction));

        bool wasAlreadyBleeding = IsBleeding;

        IsBleeding = true;
        BleedDamagePerTick = Mathf.Max(1, damagePerTick);
        BleedDuration = Mathf.Max(0.5f, duration);

        float currentTime = (float)Runner.SimulationTime;
        if (!wasAlreadyBleeding)
        {
            BleedNextTickTime = currentTime + 0.1f;
        }

        Debug.Log($"[ApplyBleed] {CharacterName} is bleeding! {BleedDamagePerTick} damage per {bleedTickInterval}s for {BleedDuration:F1}s (Resistance: {totalResistance:F1}%)");
        RPC_ShowBleedEffect(true);
    }

    private void ApplyBleedDamage()
    {
        if (!IsBleeding) return;

        // 🩸 Bleed ทำดาเมจมากขึ้นเมื่อเลือดน้อย
        float healthPercentage = (float)currentHp / maxHp;
        float bleedMultiplier = Mathf.Lerp(2.0f, 1.0f, healthPercentage); // 1x-2x damage
        int finalDamage = Mathf.RoundToInt(BleedDamagePerTick * bleedMultiplier);

        int oldHp = currentHp;
        currentHp -= finalDamage;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);

        NetworkedCurrentHp = currentHp;
        Debug.Log($"[Bleed] {CharacterName} takes {finalDamage} bleed damage (base: {BleedDamagePerTick} x {bleedMultiplier:F1}). HP: {currentHp}/{maxHp}");

        RPC_TriggerBleedFlash();
        RPC_BroadcastHealthUpdate(currentHp);

        if (currentHp <= 0)
        {
            RPC_OnDeath();
        }
    }

    public virtual void RemoveBleed()
    {
        if (!HasStateAuthority) return;

        IsBleeding = false;
        BleedDuration = 0f;
        BleedDamagePerTick = 0;
        BleedNextTickTime = 0f;

        Debug.Log($"{CharacterName} is no longer bleeding");
        RPC_ShowBleedEffect(false);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowBleedEffect(bool show)
    {
        if (bleedVFX != null)
        {
            bleedVFX.SetActive(show);
        }

        if (characterRenderer != null && !isFlashingFromBleed && !isTakingDamage)
        {
            if (show)
            {
                characterRenderer.material.color = bleedColor;
            }
            else
            {
                characterRenderer.material.color = GetReturnColor();
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TriggerBleedFlash()
    {
        StartCoroutine(BleedDamageFlash());
    }

    protected virtual IEnumerator BleedDamageFlash()
    {
        if (characterRenderer == null) yield break;

        isFlashingFromBleed = true;
        characterRenderer.material.color = Color.red;
        yield return new WaitForSeconds(0.2f);

        if (IsBleeding)
        {
            characterRenderer.material.color = bleedColor;
        }
        else
        {
            characterRenderer.material.color = GetReturnColor();
        }

        isFlashingFromBleed = false;
    }
    // ========== Status Effects Handler ==========
    protected virtual void HandleStatusEffects()
    {
        // Client-side visual updates based on network state
        if (!HasStateAuthority)
        {
            UpdateVisualEffects();
        }
    }

    // 🔧 ปรับปรุง UpdateVisualEffects ให้ไม่ override การ flash
    private void UpdateVisualEffects()
    {
        // Update all VFX based on network state
        if (poisonVFX != null && poisonVFX.activeSelf != IsPoisoned)
            poisonVFX.SetActive(IsPoisoned);

        if (burnVFX != null && burnVFX.activeSelf != IsBurning)
            burnVFX.SetActive(IsBurning);

        if (freezeVFX != null && freezeVFX.activeSelf != IsFrozen)
            freezeVFX.SetActive(IsFrozen);

        if (stunVFX != null && stunVFX.activeSelf != IsStunned)
            stunVFX.SetActive(IsStunned);

        if (bleedVFX != null && bleedVFX.activeSelf != IsBleeding)
            bleedVFX.SetActive(IsBleeding);

        if (armorBreakVFX != null && armorBreakVFX.activeSelf != IsArmorBreak)
            armorBreakVFX.SetActive(IsArmorBreak);

        if (blindVFX != null && blindVFX.activeSelf != IsBlind)
            blindVFX.SetActive(IsBlind);

        if (weaknessVFX != null && weaknessVFX.activeSelf != IsWeak)
            weaknessVFX.SetActive(IsWeak);

        // อัพเดทสีเฉพาะเมื่อไม่มีการ flash
        if (characterRenderer != null && !isTakingDamage &&
            !isFlashingFromPoison && !isFlashingFromBurn &&
            !isFlashingFromFreeze && !isFlashingFromStun && !isFlashingFromBleed &&
            !isFlashingFromArmorBreak && !isFlashingFromBlind && !isFlashingFromWeakness)
        {
            Color targetColor = GetReturnColor();
            float colorDifference = Vector4.Distance(characterRenderer.material.color, targetColor);

            if (colorDifference > 0.1f)
            {
                characterRenderer.material.color = targetColor;
            }
        }
    }
    #endregion


    #region Status Effect Physic

    public virtual void ApplyStun(float duration)
    {
        if (!HasStateAuthority) return;

        // ⚡ คำนวณ resistance (Physical)
        float totalResistance = statusResistance.GetTotalPhysicalResistance();

        // ตรวจสอบโอกาสป้องกัน
        float chanceReduction = totalResistance * 0.6f;
        if (Random.Range(0f, 100f) < chanceReduction)
        {
            Debug.Log($"[Stun Resisted] {CharacterName} resisted stun! ({chanceReduction:F1}% chance)");
            return;
        }

        // ลดระยะเวลาตาม resistance
        float durationReduction = totalResistance / 100f;
        duration = duration * (1f - durationReduction);

        IsStunned = true;
        StunDuration = Mathf.Max(0.5f, duration);

        // หยุดการเคลื่อนไหวทั้งหมด
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
        }

        Debug.Log($"[ApplyStun] {CharacterName} is stunned for {StunDuration:F1}s! (Resistance: {totalResistance:F1}%)");
        RPC_ShowStunEffect(true);
    }

    public virtual void RemoveStun()
    {
        if (!HasStateAuthority) return;

        IsStunned = false;
        StunDuration = 0f;

        Debug.Log($"{CharacterName} is no longer stunned");
        RPC_ShowStunEffect(false);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowStunEffect(bool show)
    {
        if (stunVFX != null)
        {
            stunVFX.SetActive(show);
        }

        if (characterRenderer != null && !isFlashingFromStun && !isTakingDamage)
        {
            if (show)
            {
                characterRenderer.material.color = stunColor;
            }
            else
            {
                characterRenderer.material.color = GetReturnColor();
            }
        }
    }
    public virtual void ApplyArmorBreak(float duration, float reduction = 0.5f)
    {
        if (!HasStateAuthority) return;

        // 🛡️ คำนวณ resistance (Physical)
        float totalResistance = statusResistance.GetTotalPhysicalResistance();

        // ตรวจสอบโอกาสป้องกัน
        float chanceReduction = totalResistance * 0.6f; // 60% ของ resistance
        if (Random.Range(0f, 100f) < chanceReduction)
        {
            Debug.Log($"[Armor Break Resisted] {CharacterName} resisted armor break! ({chanceReduction:F1}% chance)");
            return;
        }

        // ลดระยะเวลาตาม resistance
        float durationReduction = totalResistance / 100f;
        duration = duration * (1f - durationReduction);

        IsArmorBreak = true;
        ArmorBreakDuration = Mathf.Max(0.5f, duration);
        ArmorBreakAmount = reduction;

        Debug.Log($"[ApplyArmorBreak] {CharacterName} armor broken! Reduction: {reduction * 100}% for {ArmorBreakDuration:F1}s (Resistance: {totalResistance:F1}%)");
        RPC_ShowArmorBreakEffect(true);
    }

    public virtual void RemoveArmorBreak()
    {
        if (!HasStateAuthority) return;

        IsArmorBreak = false;
        ArmorBreakDuration = 0f;
        ArmorBreakAmount = 0f;

        Debug.Log($"{CharacterName} armor break removed");
        RPC_ShowArmorBreakEffect(false);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowArmorBreakEffect(bool show)
    {
        if (armorBreakVFX != null)
        {
            armorBreakVFX.SetActive(show);
        }

        if (characterRenderer != null && !isFlashingFromArmorBreak && !isTakingDamage)
        {
            if (show)
            {
                characterRenderer.material.color = armorBreakColor;
            }
            else
            {
                characterRenderer.material.color = GetReturnColor();
            }
        }
    }

    // ========== 👁️ Blind Status Effect System ==========
    public virtual void ApplyBlind(float duration, float reduction = 0.8f)
    {
        if (!HasStateAuthority) return;

        // 👁️ คำนวณ resistance (Physical)
        float totalResistance = statusResistance.GetTotalPhysicalResistance();

        // ตรวจสอบโอกาสป้องกัน
        float chanceReduction = totalResistance * 0.6f;
        if (Random.Range(0f, 100f) < chanceReduction)
        {
            Debug.Log($"[Blind Resisted] {CharacterName} resisted blind! ({chanceReduction:F1}% chance)");
            return;
        }

        // ลดระยะเวลาตาม resistance
        float durationReduction = totalResistance / 100f;
        duration = duration * (1f - durationReduction);

        IsBlind = true;
        BlindDuration = Mathf.Max(0.5f, duration);
        BlindAmount = reduction;

        Debug.Log($"[ApplyBlind] {CharacterName} is blinded! Critical reduction: {reduction * 100}% for {BlindDuration:F1}s");
        RPC_ShowBlindEffect(true);
    }

    public virtual void RemoveBlind()
    {
        if (!HasStateAuthority) return;

        IsBlind = false;
        BlindDuration = 0f;
        BlindAmount = 0f;

        Debug.Log($"{CharacterName} blind removed");
        RPC_ShowBlindEffect(false);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowBlindEffect(bool show)
    {
        if (blindVFX != null)
        {
            blindVFX.SetActive(show);
        }

        if (characterRenderer != null && !isFlashingFromBlind && !isTakingDamage)
        {
            if (show)
            {
                characterRenderer.material.color = blindColor;
            }
            else
            {
                characterRenderer.material.color = GetReturnColor();
            }
        }
    }

    // ========== 💪 Weakness Status Effect System ==========
    public virtual void ApplyWeakness(float duration, float reduction = 0.4f)
    {
        if (!HasStateAuthority) return;

        // 💪 คำนวณ resistance (Physical)
        float totalResistance = statusResistance.GetTotalPhysicalResistance();

        // ตรวจสอบโอกาสป้องกัน
        float chanceReduction = totalResistance * 0.6f;
        if (Random.Range(0f, 100f) < chanceReduction)
        {
            Debug.Log($"[Weakness Resisted] {CharacterName} resisted weakness! ({chanceReduction:F1}% chance)");
            return;
        }

        // ลดระยะเวลาตาม resistance
        float durationReduction = totalResistance / 100f;
        duration = duration * (1f - durationReduction);

        IsWeak = true;
        WeaknessDuration = Mathf.Max(0.5f, duration);
        WeaknessAmount = reduction;

        Debug.Log($"[ApplyWeakness] {CharacterName} is weakened! Attack reduction: {reduction * 100}% for {WeaknessDuration:F1}s");
        RPC_ShowWeaknessEffect(true);
    }

    public virtual void RemoveWeakness()
    {
        if (!HasStateAuthority) return;

        IsWeak = false;
        WeaknessDuration = 0f;
        WeaknessAmount = 0f;

        Debug.Log($"{CharacterName} weakness removed");
        RPC_ShowWeaknessEffect(false);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowWeaknessEffect(bool show)
    {
        if (weaknessVFX != null)
        {
            weaknessVFX.SetActive(show);
        }

        if (characterRenderer != null && !isFlashingFromWeakness && !isTakingDamage)
        {
            if (show)
            {
                characterRenderer.material.color = weaknessColor;
            }
            else
            {
                characterRenderer.material.color = GetReturnColor();
            }
        }
    }
    #endregion
    // ========== Public Methods for External Use ==========
    #region // ========== Equipment/Rune Integration ==========

    public virtual void UpdateResistanceFromEquipment(float physicalBonus, float magicalBonus)
    {
        statusResistance.equipmentPhysicalBonus = physicalBonus;
        statusResistance.equipmentMagicalBonus = magicalBonus;

        Debug.Log($"[Equipment] Updated resistance: Physical +{physicalBonus}%, Magical +{magicalBonus}%");
    }

    public virtual void UpdateResistanceFromRunes(float physicalBonus, float magicalBonus)
    {
        statusResistance.runePhysicalBonus = physicalBonus;
        statusResistance.runeMagicalBonus = magicalBonus;

        Debug.Log($"[Runes] Updated resistance: Physical +{physicalBonus}%, Magical +{magicalBonus}%");
    }
    public virtual void GetCurrentResistanceStats()
    {
        float totalPhysical = statusResistance.GetTotalPhysicalResistance();
        float totalMagical = statusResistance.GetTotalMagicalResistance();

        Debug.Log($"=== {CharacterName} Resistance Stats ===");
        Debug.Log($"🛡️ Physical Resistance: {totalPhysical:F1}% (Base: {statusResistance.physicalResistance}% + Equipment: {statusResistance.equipmentPhysicalBonus}% + Runes: {statusResistance.runePhysicalBonus}%)");
        Debug.Log($"🔮 Magical Resistance: {totalMagical:F1}% (Base: {statusResistance.magicalResistance}% + Equipment: {statusResistance.equipmentMagicalBonus}% + Runes: {statusResistance.runeMagicalBonus}%)");
        Debug.Log($"Physical protects: Stun, Armor Break, Blind, Weakness");
        Debug.Log($"Magical protects: Poison, Burn, Bleed, Freeze");
    }
    #endregion
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

    // ========== Debug Methods ==========
   

    // ========== Testing Methods ==========
    

   
   
}