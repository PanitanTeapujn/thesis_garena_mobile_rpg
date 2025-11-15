using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System;

public enum StatusEffectType
{
    None,
    // Magical Effects
    Poison,
    Burn,
    Bleed,
    Freeze,
    // Physical Effects  
    Stun,
    ArmorBreak,
    Blind,
    Weakness,
    Slow,
    // ========== 🌟 TEAM AURA BUFFS ==========
    AttackSpeedAura,    // +30% attack speed รัศมี 5m
    DamageAura,         // +20% damage รัศมี 5m  
    MoveSpeedAura,      // +15% move speed รัศมี 5m
    ProtectionAura,     // -15% damage taken รัศมี 6m
    ArmorAura,          // +20% armor รัศมี 6m
    CriticalAura,        // +15% critical chance รัศมี 6m
         MagicDamageAura,        // +20% magic damage
    CooldownReductionAura,  // +15% CDR
    LifeStealAura,          // +10% life steal
    AmpDamageAura,          // +15% amp damage
    MagicArmorAura,         // +20% magic armor
    HealthRegenAura,        // +50% health regen
    ManaRegenAura,          // +50% mana regen
    HitRateAura,            // +15% hit rate
    EvasionAura
}

public class StatusEffectManager : NetworkBehaviour
{
    [Header("Status Effect Settings")]
    private float poisonTickInterval = 1f;
    private float burnTickInterval = 0.5f;   // ทุก 0.5 วินาที
    private float bleedTickInterval = 0.7f;  // ทุก 0.7 วินาที

    [Header("🌟 Team Aura Settings")]
    public float auraCheckInterval = 0.5f;          // ตรวจสอบทุก 0.5 วินาที
    public LayerMask teamLayerMask = -1;

    // ========== Network Status Properties ==========
#region Magical Effects
    // 🧪 Magical Effects
    [Networked] public bool IsPoisoned { get; set; }
    [Networked] public float PoisonDuration { get; set; }
    [Networked] public int PoisonDamagePerTick { get; set; }
    [Networked] public float PoisonNextTickTime { get; set; }

    [Networked] public bool IsBurning { get; set; }
    [Networked] public float BurnDuration { get; set; }
    [Networked] public int BurnDamagePerTick { get; set; }
    [Networked] public float BurnNextTickTime { get; set; }

    [Networked] public bool IsBleeding { get; set; }
    [Networked] public float BleedDuration { get; set; }
    [Networked] public int BleedDamagePerTick { get; set; }
    [Networked] public float BleedNextTickTime { get; set; }

    [Networked] public bool IsFrozen { get; set; }
    [Networked] public float FreezeDuration { get; set; }
    [Networked] public float OriginalMoveSpeedForFreeze  { get; set; }
    #endregion

#region Physical Effects
    // ⚡ Physical Effects
    [Networked] public bool IsStunned { get; set; }
    [Networked] public float StunDuration { get; set; }

    [Networked] public bool IsArmorBreak { get; set; }
    [Networked] public float ArmorBreakDuration { get; set; }
    [Networked] public float ArmorBreakAmount { get; set; } // 0.5 = 50% reduction

    [Networked] public bool IsBlind { get; set; }
    [Networked] public float BlindDuration { get; set; }
    [Networked] public float BlindAmount { get; set; } // 0.8 = 80% reduction
    [Networked] public bool IsSlowed { get; set; }
    [Networked] public float SlowDuration { get; set; }
    [Networked] public float SlowAmount { get; set; } // 0.3 = 30% slow
    [Networked] public float OriginalMoveSpeedForSlow { get; set; } // 🆕 เก็บ speed เดิม
    [Networked] public bool IsWeak { get; set; }
    [Networked] public float WeaknessDuration { get; set; }
    [Networked] public float WeaknessAmount { get; set; } // 0.4 = 40% reduction
    #endregion
    #region AuraBuff

    [Networked] public bool IsProvidingMagicDamageAura { get; set; }
    [Networked] public float MagicDamageAuraDuration { get; set; }
    [Networked] public float MagicDamageAuraRadius { get; set; }
    [Networked] public float MagicDamageAuraAmount { get; set; }

    // ========== 🆕 Cooldown Reduction Aura ==========
    [Networked] public bool IsProvidingCooldownReductionAura { get; set; }
    [Networked] public float CooldownReductionAuraDuration { get; set; }
    [Networked] public float CooldownReductionAuraRadius { get; set; }
    [Networked] public float CooldownReductionAuraAmount { get; set; }

    // ========== 🆕 Life Steal Aura ==========
    [Networked] public bool IsProvidingLifeStealAura { get; set; }
    [Networked] public float LifeStealAuraDuration { get; set; }
    [Networked] public float LifeStealAuraRadius { get; set; }
    [Networked] public float LifeStealAuraAmount { get; set; }

    // ========== 🆕 Amp Damage Aura ==========
    [Networked] public bool IsProvidingAmpDamageAura { get; set; }
    [Networked] public float AmpDamageAuraDuration { get; set; }
    [Networked] public float AmpDamageAuraRadius { get; set; }
    [Networked] public float AmpDamageAuraAmount { get; set; }

    // ========== 🆕 Magic Armor Aura ==========
    [Networked] public bool IsProvidingMagicArmorAura { get; set; }
    [Networked] public float MagicArmorAuraDuration { get; set; }
    [Networked] public float MagicArmorAuraRadius { get; set; }
    [Networked] public float MagicArmorAuraAmount { get; set; }

    // ========== 🆕 Health Regen Aura ==========
    [Networked] public bool IsProvidingHealthRegenAura { get; set; }
    [Networked] public float HealthRegenAuraDuration { get; set; }
    [Networked] public float HealthRegenAuraRadius { get; set; }
    [Networked] public float HealthRegenAuraAmount { get; set; }

    // ========== 🆕 Mana Regen Aura ==========
    [Networked] public bool IsProvidingManaRegenAura { get; set; }
    [Networked] public float ManaRegenAuraDuration { get; set; }
    [Networked] public float ManaRegenAuraRadius { get; set; }
    [Networked] public float ManaRegenAuraAmount { get; set; }

    // ========== 🆕 Hit Rate Aura ==========
    [Networked] public bool IsProvidingHitRateAura { get; set; }
    [Networked] public float HitRateAuraDuration { get; set; }
    [Networked] public float HitRateAuraRadius { get; set; }
    [Networked] public float HitRateAuraAmount { get; set; }

    // ========== 🆕 Evasion Aura ==========
    [Networked] public bool IsProvidingEvasionAura { get; set; }
    [Networked] public float EvasionAuraDuration { get; set; }
    [Networked] public float EvasionAuraRadius { get; set; }
    [Networked] public float EvasionAuraAmount { get; set; }

    // ========== Receiving Properties (เดิม + ใหม่) ==========
    [Networked] public float ReceivedAttackSpeedBonus { get; set; }
    [Networked] public float ReceivedDamageBonus { get; set; }
    [Networked] public float ReceivedMoveSpeedBonus { get; set; }
    [Networked] public float ReceivedProtectionBonus { get; set; }
    [Networked] public float ReceivedArmorBonus { get; set; }
    [Networked] public float ReceivedCriticalBonus { get; set; }

    // 🆕 Receiving Properties ใหม่
    [Networked] public float ReceivedMagicDamageBonus { get; set; }
    [Networked] public float ReceivedCooldownReductionBonus { get; set; }
    [Networked] public float ReceivedLifeStealBonus { get; set; }
    [Networked] public float ReceivedAmpDamageBonus { get; set; }
    [Networked] public float ReceivedMagicArmorBonus { get; set; }
    [Networked] public float ReceivedHealthRegenBonus { get; set; }
    [Networked] public float ReceivedManaRegenBonus { get; set; }
    [Networked] public float ReceivedHitRateBonus { get; set; }
    [Networked] public float ReceivedEvasionBonus { get; set; }
    // Attack Speed Aura
    [Networked] public bool IsProvidingAttackSpeedAura { get; set; }
    [Networked] public float AttackSpeedAuraDuration { get; set; }
    [Networked] public float AttackSpeedAuraRadius { get; set; }
    [Networked] public float AttackSpeedAuraAmount { get; set; }

    // Damage Aura  
    [Networked] public bool IsProvidingDamageAura { get; set; }
    [Networked] public float DamageAuraDuration { get; set; }
    [Networked] public float DamageAuraRadius { get; set; }
    [Networked] public float DamageAuraAmount { get; set; }

    // Move Speed Aura
    [Networked] public bool IsProvidingMoveSpeedAura { get; set; }
    [Networked] public float MoveSpeedAuraDuration { get; set; }
    [Networked] public float MoveSpeedAuraRadius { get; set; }
    [Networked] public float MoveSpeedAuraAmount { get; set; }

    // Protection Aura
    [Networked] public bool IsProvidingProtectionAura { get; set; }
    [Networked] public float ProtectionAuraDuration { get; set; }
    [Networked] public float ProtectionAuraRadius { get; set; }
    [Networked] public float ProtectionAuraAmount { get; set; }

    // Armor Aura
    [Networked] public bool IsProvidingArmorAura { get; set; }
    [Networked] public float ArmorAuraDuration { get; set; }
    [Networked] public float ArmorAuraRadius { get; set; }
    [Networked] public float ArmorAuraAmount { get; set; }

    // Critical Aura
    [Networked] public bool IsProvidingCriticalAura { get; set; }
    [Networked] public float CriticalAuraDuration { get; set; }
    [Networked] public float CriticalAuraRadius { get; set; }
    [Networked] public float CriticalAuraAmount { get; set; }

    // ========== Team Aura Receiving Properties (สิ่งที่ได้รับจากคนอื่น) ==========
  

    // Timers
    private float nextAuraCheckTime = 0f;

    #endregion
    // ========== Events for Communication ==========
    public static event Action<Character, int, DamageType> OnStatusDamage;
    public static event Action<Character, StatusEffectType, bool> OnStatusEffectChanged;
    public static event Action<Character, StatusEffectType> OnStatusDamageFlash;

    // ========== Component References ==========
    private Character character;
    private EquipmentManager equipmentManager;
    [Header("🛡️ Debuff Immunity")]
    [Networked] public bool IsDebuffImmune { get; set; }
    [Networked] public float DebuffImmunityDuration { get; set; }
    protected virtual void Awake()
    {
        character = GetComponent<Character>();
        equipmentManager = GetComponent<EquipmentManager>();
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority)
        {
            // Magical Effects
            ProcessPoisonEffect();
            ProcessBurnEffect();
            ProcessBleedEffect();
            ProcessFreezeEffect();

            // Physical Effects
            ProcessStunEffect();
            ProcessArmorBreakEffect();
            ProcessBlindEffect();
            ProcessWeaknessEffect();
            ProcessSlowEffect();
            ProcessAllAuras();
            float currentTime = (float)Runner.SimulationTime;
            if (currentTime >= nextAuraCheckTime)
            {
                UpdateAuraEffects();
                nextAuraCheckTime = currentTime + auraCheckInterval;
            }

        }
    }
    #region DeBuff
    // ========== 🧪 Poison System ==========
    public virtual void ApplySlow(float duration, float slowAmount = 0.3f)
    {
        if (!HasStateAuthority || IsDebuffImmune) return;

        float totalResistance = GetPhysicalResistance();
        float chanceReduction = totalResistance * 0.6f;

        if (UnityEngine.Random.Range(0f, 100f) < chanceReduction)
        {
            Debug.Log($"[Slow Resisted] {character.CharacterName} resisted slow with {character.Armor} Armor!");
            RPC_ShowStatusResistText(StatusEffectType.Slow);
            return;
        }

        float durationReduction = totalResistance / 100f;
        duration = duration * (1f - durationReduction);

        bool wasAlreadySlowed = IsSlowed;

        IsSlowed = true;
        SlowDuration = Mathf.Max(0.5f, duration);
        SlowAmount = slowAmount;

        if (!wasAlreadySlowed)
        {
            // 🆕 เก็บ original speed ก่อน slow (ไม่แก้ไข character.MoveSpeed)
            OriginalMoveSpeedForSlow = character.MoveSpeed;

            Debug.Log($"[ApplySlow] {character.CharacterName} slowed! " +
                     $"Original Speed: {OriginalMoveSpeedForSlow:F1}, " +
                     $"Slow: {slowAmount * 100f:F0}%, Duration: {SlowDuration:F1}s");

            // ✅ แสดงข้อความ
            RPC_ShowStatusEffectText(StatusEffectType.Slow, 0, duration, slowAmount);
        }

        Debug.Log($"[ApplySlow] {character.CharacterName} slowed by {slowAmount * 100f:F0}% for {SlowDuration:F1}s!");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.Slow, true);
    }

    private void ProcessSlowEffect()
    {
        if (!IsSlowed) return;

        if (SlowDuration > 0)
        {
            SlowDuration -= Runner.DeltaTime;
            if (SlowDuration <= 0)
            {
                RemoveSlow();
            }
        }
    }

    public virtual void RemoveSlow()
    {
        if (!HasStateAuthority) return;

        if (IsSlowed)
        {
            IsSlowed = false;
            SlowDuration = 0f;
            SlowAmount = 0f;

            Debug.Log($"[RemoveSlow] {character.CharacterName} is no longer slowed! Speed: {character.GetEffectiveMoveSpeed():F1}");
            OnStatusEffectChanged?.Invoke(character, StatusEffectType.Slow, false);
        }
    }
    public virtual void ApplyPoison(int damagePerTick, float duration)
    {
        if (!HasStateAuthority || IsDebuffImmune) return; // 🆕 เพิ่มการเช็ค

        float totalResistance = GetMagicalResistance();
        float chanceReduction = totalResistance * 0.6f;

        if (UnityEngine.Random.Range(0f, 100f) < chanceReduction)
        {
            Debug.Log($"[Poison Resisted] {character.CharacterName} resisted poison with {character.MagicArmor} Magic Armor! ({chanceReduction:F1}% chance)");

            // ✅ แสดงข้อความ "RESISTED"
            RPC_ShowStatusResistText(StatusEffectType.Poison);
            return;
        }

        float durationReduction = totalResistance / 100f;
        float damageReduction = (totalResistance * 0.5f) / 100f;

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

            // ✅ แสดงข้อความเฉพาะครั้งแรกที่ได้รับ Poison
            RPC_ShowStatusEffectText(StatusEffectType.Poison, damagePerTick, duration);
        }

        Debug.Log($"[ApplyPoison] {character.CharacterName} poisoned! Damage: {PoisonDamagePerTick}/tick for {PoisonDuration:F1}s");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.Poison, true);
    }
    private void ProcessPoisonEffect()
    {
        if (!IsPoisoned) return;

        float currentTime = (float)Runner.SimulationTime;

        // Check poison tick
        if (currentTime >= PoisonNextTickTime)
        {
            ApplyPoisonDamage();
            PoisonNextTickTime = currentTime + poisonTickInterval;
        }

        // Check poison duration
        if (PoisonDuration > 0)
        {
            PoisonDuration -= Runner.DeltaTime;
            if (PoisonDuration <= 0)
            {
                RemovePoison();
            }
        }
    }

    private void ApplyPoisonDamage()
    {
        if (!IsPoisoned) return;

        Debug.Log($"[PoisonTick] {character.CharacterName} takes {PoisonDamagePerTick} poison damage");

        // ส่งสัญญาณให้ CombatManager ทำ damage (CombatManager จะจัดการ damage text เอง)
        OnStatusDamage?.Invoke(character, PoisonDamagePerTick, DamageType.Poison);

        // ส่งสัญญาณให้ VisualManager ทำ flash effect
        OnStatusDamageFlash?.Invoke(character, StatusEffectType.Poison);
    }

    public virtual void RemovePoison()
    {
        if (!HasStateAuthority) return;

        bool wasPoisoned = IsPoisoned;
        IsPoisoned = false;
        PoisonDuration = 0f;
        PoisonDamagePerTick = 0;
        PoisonNextTickTime = 0f;

        Debug.Log($"{character.CharacterName} is no longer poisoned");

        // แจ้ง visual manager
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.Poison, false);
    }

    // ========== ⚡ Stun System ==========
    public virtual void ApplyStun(float duration)
    {
        if (!HasStateAuthority || IsDebuffImmune) return; // 🆕 เพิ่มการเช็ค

        float totalResistance = GetPhysicalResistance();
        float chanceReduction = totalResistance * 0.6f;

        if (UnityEngine.Random.Range(0f, 100f) < chanceReduction)
        {
            RPC_ShowStatusResistText(StatusEffectType.Stun);
            return;
        }

        float durationReduction = totalResistance / 100f;
        duration = duration * (1f - durationReduction);

        bool wasAlreadyStunned = IsStunned;

        IsStunned = true;
        StunDuration = Mathf.Max(0.5f, duration);

        if (character.rb != null)
        {
            character.rb.linearVelocity = Vector3.zero;
        }

        if (!wasAlreadyStunned)
        {
            // ✅ แสดงข้อความ
            RPC_ShowStatusEffectText(StatusEffectType.Stun, 0, duration);
        }

        OnStatusEffectChanged?.Invoke(character, StatusEffectType.Stun, true);
    }

    private void ProcessStunEffect()
    {
        if (!IsStunned) return;

        if (StunDuration > 0)
        {
            StunDuration -= Runner.DeltaTime;
            if (StunDuration <= 0)
            {
                RemoveStun();
            }
        }
    }

    public virtual void RemoveStun()
    {
        if (!HasStateAuthority) return;

        IsStunned = false;
        StunDuration = 0f;

        Debug.Log($"{character.CharacterName} is no longer stunned");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.Stun, false);
    }

    // ========== ❄️ Freeze System ==========
    // ========== ❄️ Freeze System ==========
    public virtual void ApplyFreeze(float duration)
    {
        if (!HasStateAuthority || IsDebuffImmune) return;

        // ❄️ ใช้ Magic Armor
        float totalResistance = GetMagicalResistance();

        // ตรวจสอบโอกาสป้องกัน
        float chanceReduction = totalResistance * 0.6f;
        if (UnityEngine.Random.Range(0f, 100f) < chanceReduction)
        {
            Debug.Log($"[Freeze Resisted] {character.CharacterName} resisted freeze with {character.MagicArmor} Magic Armor!");
            RPC_ShowStatusResistText(StatusEffectType.Freeze);
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
            // ✅ เก็บ original speed แบบเดียวกับ Slow (ไม่แก้ไข character.MoveSpeed)
            OriginalMoveSpeedForFreeze = character.MoveSpeed;

            // ✅ หยุด velocity
            if (character.rb != null)
            {
                character.rb.linearVelocity = Vector3.zero;
            }

            Debug.Log($"[ApplyFreeze] {character.CharacterName} frozen! " +
                     $"Original Speed: {OriginalMoveSpeedForFreeze:F1}, " +
                     $"Duration: {FreezeDuration:F1}s");

            // ✅ แสดงข้อความ
            RPC_ShowStatusEffectText(StatusEffectType.Freeze, 0, duration);
        }

        OnStatusEffectChanged?.Invoke(character, StatusEffectType.Freeze, true);
    }
    private void ProcessFreezeEffect()
    {
        if (!IsFrozen) return;

        if (FreezeDuration > 0)
        {
            FreezeDuration -= Runner.DeltaTime;
            if (FreezeDuration <= 0)
            {
                RemoveFreeze();
            }
        }
    }

    public virtual void RemoveFreeze()
    {
        if (!HasStateAuthority) return;

        if (IsFrozen)
        {
            IsFrozen = false;
            FreezeDuration = 0f;

            // ❌ ลบบรรทัดนี้ออก (ไม่ต้อง restore เพราะไม่ได้แก้ไข):
            // character.MoveSpeed = OriginalMoveSpeed;

            Debug.Log($"[RemoveFreeze] {character.CharacterName} is no longer frozen! " +
                     $"Speed: {character.GetEffectiveMoveSpeed():F1}");

            OnStatusEffectChanged?.Invoke(character, StatusEffectType.Freeze, false);
        }
    }

    // ========== 🔥 Burn System ==========
    public virtual void ApplyBurn(int damagePerTick, float duration)
    {
        if (!HasStateAuthority || IsDebuffImmune) return; // 🆕 เพิ่มการเช็ค

        float totalResistance = GetMagicalResistance();
        float chanceReduction = totalResistance * 0.6f;

        if (UnityEngine.Random.Range(0f, 100f) < chanceReduction)
        {
            RPC_ShowStatusResistText(StatusEffectType.Burn);
            return;
        }

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

            // ✅ แสดงข้อความเฉพาะครั้งแรก
            RPC_ShowStatusEffectText(StatusEffectType.Burn, damagePerTick, duration);
        }

        OnStatusEffectChanged?.Invoke(character, StatusEffectType.Burn, true);
    }

    private void ProcessBurnEffect()
    {
        if (!IsBurning) return;

        float currentTime = (float)Runner.SimulationTime;

        // Check burn tick
        if (currentTime >= BurnNextTickTime)
        {
            ApplyBurnDamage();
            BurnNextTickTime = currentTime + burnTickInterval;
        }

        // Check burn duration
        if (BurnDuration > 0)
        {
            BurnDuration -= Runner.DeltaTime;
            if (BurnDuration <= 0)
            {
                RemoveBurn();
            }
        }
    }


    private void ApplyBurnDamage()
    {
        if (!IsBurning) return;

        Debug.Log($"[BurnTick] {character.CharacterName} takes {BurnDamagePerTick} burn damage");

        // ส่งสัญญาณให้ CombatManager ทำ damage (CombatManager จะจัดการ damage text เอง)
        OnStatusDamage?.Invoke(character, BurnDamagePerTick, DamageType.Burn);

        OnStatusDamageFlash?.Invoke(character, StatusEffectType.Burn);
    }


    public virtual void RemoveBurn()
    {
        if (!HasStateAuthority) return;

        IsBurning = false;
        BurnDuration = 0f;
        BurnDamagePerTick = 0;
        BurnNextTickTime = 0f;

        Debug.Log($"{character.CharacterName} is no longer burning");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.Burn, false);
    }

    // ========== 🩸 Bleed System ==========
    public virtual void ApplyBleed(int damagePerTick, float duration)
    {
        if (!HasStateAuthority || IsDebuffImmune) return; // 🆕 เพิ่มการเช็ค

        float totalResistance = GetMagicalResistance();
        float chanceReduction = totalResistance * 0.6f;

        if (UnityEngine.Random.Range(0f, 100f) < chanceReduction)
        {
            RPC_ShowStatusResistText(StatusEffectType.Bleed);
            return;
        }

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

            // ✅ แสดงข้อความเฉพาะครั้งแรก
            RPC_ShowStatusEffectText(StatusEffectType.Bleed, damagePerTick, duration);
        }

        OnStatusEffectChanged?.Invoke(character, StatusEffectType.Bleed, true);
    }

    private void ProcessBleedEffect()
    {
        if (!IsBleeding) return;

        float currentTime = (float)Runner.SimulationTime;

        // Check bleed tick
        if (currentTime >= BleedNextTickTime)
        {
            ApplyBleedDamage();
            BleedNextTickTime = currentTime + bleedTickInterval;
        }

        // Check bleed duration
        if (BleedDuration > 0)
        {
            BleedDuration -= Runner.DeltaTime;
            if (BleedDuration <= 0)
            {
                RemoveBleed();
            }
        }
    }

    private void ApplyBleedDamage()
    {
        if (!IsBleeding) return;

        // 🩸 Bleed ทำดาเมจมากขึ้นเมื่อเลือดน้อย
        float healthPercentage = (float)character.CurrentHp / character.MaxHp;
        float bleedMultiplier = Mathf.Lerp(2.0f, 1.0f, healthPercentage); // 1x-2x damage
        int finalDamage = Mathf.RoundToInt(BleedDamagePerTick * bleedMultiplier);

        Debug.Log($"[BleedTick] {character.CharacterName} takes {finalDamage} bleed damage (base: {BleedDamagePerTick} x {bleedMultiplier:F1})");

        // ส่งสัญญาณให้ CombatManager ทำ damage (CombatManager จะจัดการ damage text เอง)
        OnStatusDamage?.Invoke(character, finalDamage, DamageType.Bleed);

        OnStatusDamageFlash?.Invoke(character, StatusEffectType.Bleed);
    }
    public virtual void RemoveBleed()
    {
        if (!HasStateAuthority) return;

        IsBleeding = false;
        BleedDuration = 0f;
        BleedDamagePerTick = 0;
        BleedNextTickTime = 0f;

        Debug.Log($"{character.CharacterName} is no longer bleeding");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.Bleed, false);
    }

    // ========== 🛡️ Armor Break System ==========
    public virtual void ApplyArmorBreak(float duration, float reduction = 0.5f)
    {
        if (!HasStateAuthority || IsDebuffImmune) return; // 🆕 เพิ่มการเช็ค

        // 🛡️ ใช้ Physical Armor แทน physicalResistance
        float totalResistance = GetPhysicalResistance();

        // ตรวจสอบโอกาสป้องกัน
        float chanceReduction = totalResistance * 0.6f;
        if (UnityEngine.Random.Range(0f, 100f) < chanceReduction)
        {
            Debug.Log($"[Armor Break Resisted] {character.CharacterName} resisted armor break with {character.Armor} Armor! ({chanceReduction:F1}% chance)");
            return;
        }

        // ลดระยะเวลาตาม resistance
        float durationReduction = totalResistance / 100f;
        duration = duration * (1f - durationReduction);

        IsArmorBreak = true;
        ArmorBreakDuration = Mathf.Max(0.5f, duration);
        ArmorBreakAmount = reduction;

        Debug.Log($"[ApplyArmorBreak] {character.CharacterName} armor broken! Armor: {character.Armor} reduced duration to {ArmorBreakDuration:F1}s with {reduction * 100}% reduction");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.ArmorBreak, true);
    }

    private void ProcessArmorBreakEffect()
    {
        if (!IsArmorBreak) return;

        if (ArmorBreakDuration > 0)
        {
            ArmorBreakDuration -= Runner.DeltaTime;
            if (ArmorBreakDuration <= 0)
            {
                RemoveArmorBreak();
            }
        }
    }

    public virtual void RemoveArmorBreak()
    {
        if (!HasStateAuthority) return;

        IsArmorBreak = false;
        ArmorBreakDuration = 0f;
        ArmorBreakAmount = 0f;

        Debug.Log($"{character.CharacterName} armor break removed");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.ArmorBreak, false);
    }

    // ========== 👁️ Blind System ==========
    public virtual void ApplyBlind(float duration, float reduction = 0.8f)
    {
        if (!HasStateAuthority || IsDebuffImmune) return; // 🆕 เพิ่มการเช็ค

        float totalResistance = GetPhysicalResistance();
        float chanceReduction = totalResistance * 0.6f;

        if (UnityEngine.Random.Range(0f, 100f) < chanceReduction)
        {
            RPC_ShowStatusResistText(StatusEffectType.Blind);
            return;
        }

        float durationReduction = totalResistance / 100f;
        duration = duration * (1f - durationReduction);

        bool wasAlreadyBlind = IsBlind;

        IsBlind = true;
        BlindDuration = Mathf.Max(0.5f, duration);
        BlindAmount = reduction;

        if (!wasAlreadyBlind)
        {
            // ✅ แสดงข้อความ
            RPC_ShowStatusEffectText(StatusEffectType.Blind, 0, duration, reduction);
        }

        OnStatusEffectChanged?.Invoke(character, StatusEffectType.Blind, true);
    }


    private void ProcessBlindEffect()
    {
        if (!IsBlind) return;

        if (BlindDuration > 0)
        {
            BlindDuration -= Runner.DeltaTime;
            if (BlindDuration <= 0)
            {
                RemoveBlind();
            }
        }
    }

    public virtual void RemoveBlind()
    {
        if (!HasStateAuthority) return;

        IsBlind = false;
        BlindDuration = 0f;
        BlindAmount = 0f;

        Debug.Log($"{character.CharacterName} blind removed");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.Blind, false);
    }

    // ========== 💪 Weakness System ==========
    public virtual void ApplyWeakness(float duration, float reduction = 0.4f)
    {
        if (!HasStateAuthority || IsDebuffImmune) return; // 🆕 เพิ่มการเช็ค

        float totalResistance = GetPhysicalResistance();
        float chanceReduction = totalResistance * 0.6f;

        if (UnityEngine.Random.Range(0f, 100f) < chanceReduction)
        {
            RPC_ShowStatusResistText(StatusEffectType.Weakness);
            return;
        }

        float durationReduction = totalResistance / 100f;
        duration = duration * (1f - durationReduction);

        bool wasAlreadyWeak = IsWeak;

        IsWeak = true;
        WeaknessDuration = Mathf.Max(0.5f, duration);
        WeaknessAmount = reduction;

        if (!wasAlreadyWeak)
        {
            // ✅ แสดงข้อความ
            RPC_ShowStatusEffectText(StatusEffectType.Weakness, 0, duration, reduction);
        }

        OnStatusEffectChanged?.Invoke(character, StatusEffectType.Weakness, true);
    }


    private void ProcessWeaknessEffect()
    {
        if (!IsWeak) return;

        if (WeaknessDuration > 0)
        {
            WeaknessDuration -= Runner.DeltaTime;
            if (WeaknessDuration <= 0)
            {
                RemoveWeakness();
            }
        }
    }

    public virtual void RemoveWeakness()
    {
        if (!HasStateAuthority) return;

        IsWeak = false;
        WeaknessDuration = 0f;
        WeaknessAmount = 0f;

        Debug.Log($"{character.CharacterName} weakness removed");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.Weakness, false);
    }
    #endregion


    #region Buff
    public virtual void ApplyMagicDamageAura(float radius = 5f, float bonus = 0.2f, float duration = 15f)
    {
        if (!HasStateAuthority) return;

        bool wasAlreadyProviding = IsProvidingMagicDamageAura;

        IsProvidingMagicDamageAura = true;
        MagicDamageAuraRadius = radius;
        MagicDamageAuraAmount = bonus;
        MagicDamageAuraDuration = duration;

        if (!wasAlreadyProviding)
        {
            RPC_ShowAuraBuffText(StatusEffectType.MagicDamageAura, bonus, duration);
        }

        OnStatusEffectChanged?.Invoke(character, StatusEffectType.MagicDamageAura, true);
    }

    // ========== 🆕 Cooldown Reduction Aura ==========
    public virtual void ApplyCooldownReductionAura(float radius = 5f, float bonus = 0.15f, float duration = 15f)
    {
        if (!HasStateAuthority) return;

        bool wasAlreadyProviding = IsProvidingCooldownReductionAura;

        IsProvidingCooldownReductionAura = true;
        CooldownReductionAuraRadius = radius;
        CooldownReductionAuraAmount = bonus;
        CooldownReductionAuraDuration = duration;

        if (!wasAlreadyProviding)
        {
            RPC_ShowAuraBuffText(StatusEffectType.CooldownReductionAura, bonus, duration);
        }

        OnStatusEffectChanged?.Invoke(character, StatusEffectType.CooldownReductionAura, true);
    }

    // ========== 🆕 Life Steal Aura ==========
    public virtual void ApplyLifeStealAura(float radius = 5f, float bonus = 0.1f, float duration = 15f)
    {
        if (!HasStateAuthority) return;

        bool wasAlreadyProviding = IsProvidingLifeStealAura;

        IsProvidingLifeStealAura = true;
        LifeStealAuraRadius = radius;
        LifeStealAuraAmount = bonus;
        LifeStealAuraDuration = duration;

        if (!wasAlreadyProviding)
        {
            RPC_ShowAuraBuffText(StatusEffectType.LifeStealAura, bonus, duration);
        }

        OnStatusEffectChanged?.Invoke(character, StatusEffectType.LifeStealAura, true);
    }

    // ========== 🆕 Amp Damage Aura ==========
    public virtual void ApplyAmpDamageAura(float radius = 5f, float bonus = 0.15f, float duration = 15f)
    {
        if (!HasStateAuthority) return;

        bool wasAlreadyProviding = IsProvidingAmpDamageAura;

        IsProvidingAmpDamageAura = true;
        AmpDamageAuraRadius = radius;
        AmpDamageAuraAmount = bonus;
        AmpDamageAuraDuration = duration;

        if (!wasAlreadyProviding)
        {
            RPC_ShowAuraBuffText(StatusEffectType.AmpDamageAura, bonus, duration);
        }

        OnStatusEffectChanged?.Invoke(character, StatusEffectType.AmpDamageAura, true);
    }

    // ========== 🆕 Magic Armor Aura ==========
    public virtual void ApplyMagicArmorAura(float radius = 6f, float bonus = 0.2f, float duration = 20f)
    {
        if (!HasStateAuthority) return;

        bool wasAlreadyProviding = IsProvidingMagicArmorAura;

        IsProvidingMagicArmorAura = true;
        MagicArmorAuraRadius = radius;
        MagicArmorAuraAmount = bonus;
        MagicArmorAuraDuration = duration;

        if (!wasAlreadyProviding)
        {
            RPC_ShowAuraBuffText(StatusEffectType.MagicArmorAura, bonus, duration);
        }

        OnStatusEffectChanged?.Invoke(character, StatusEffectType.MagicArmorAura, true);
    }

    // ========== 🆕 Health Regen Aura ==========
    public virtual void ApplyHealthRegenAura(float radius = 5f, float bonus = 0.5f, float duration = 20f)
    {
        if (!HasStateAuthority) return;

        bool wasAlreadyProviding = IsProvidingHealthRegenAura;

        IsProvidingHealthRegenAura = true;
        HealthRegenAuraRadius = radius;
        HealthRegenAuraAmount = bonus;
        HealthRegenAuraDuration = duration;

        if (!wasAlreadyProviding)
        {
            RPC_ShowAuraBuffText(StatusEffectType.HealthRegenAura, bonus, duration);
        }

        OnStatusEffectChanged?.Invoke(character, StatusEffectType.HealthRegenAura, true);
    }

    // ========== 🆕 Mana Regen Aura ==========
    public virtual void ApplyManaRegenAura(float radius = 5f, float bonus = 0.5f, float duration = 20f)
    {
        if (!HasStateAuthority) return;

        bool wasAlreadyProviding = IsProvidingManaRegenAura;

        IsProvidingManaRegenAura = true;
        ManaRegenAuraRadius = radius;
        ManaRegenAuraAmount = bonus;
        ManaRegenAuraDuration = duration;

        if (!wasAlreadyProviding)
        {
            RPC_ShowAuraBuffText(StatusEffectType.ManaRegenAura, bonus, duration);
        }

        OnStatusEffectChanged?.Invoke(character, StatusEffectType.ManaRegenAura, true);
    }

    // ========== 🆕 Hit Rate Aura ==========
    public virtual void ApplyHitRateAura(float radius = 5f, float bonus = 0.15f, float duration = 15f)
    {
        if (!HasStateAuthority) return;

        bool wasAlreadyProviding = IsProvidingHitRateAura;

        IsProvidingHitRateAura = true;
        HitRateAuraRadius = radius;
        HitRateAuraAmount = bonus;
        HitRateAuraDuration = duration;

        if (!wasAlreadyProviding)
        {
            RPC_ShowAuraBuffText(StatusEffectType.HitRateAura, bonus, duration);
        }

        OnStatusEffectChanged?.Invoke(character, StatusEffectType.HitRateAura, true);
    }

    // ========== 🆕 Evasion Aura ==========
    public virtual void ApplyEvasionAura(float radius = 5f, float bonus = 0.1f, float duration = 15f)
    {
        if (!HasStateAuthority) return;

        bool wasAlreadyProviding = IsProvidingEvasionAura;

        IsProvidingEvasionAura = true;
        EvasionAuraRadius = radius;
        EvasionAuraAmount = bonus;
        EvasionAuraDuration = duration;

        if (!wasAlreadyProviding)
        {
            RPC_ShowAuraBuffText(StatusEffectType.EvasionAura, bonus, duration);
        }

        OnStatusEffectChanged?.Invoke(character, StatusEffectType.EvasionAura, true);
    }

    public virtual void ApplyAttackSpeedAura(float radius = 5f, float bonus = 0.3f, float duration = 15f)
    {
        if (!HasStateAuthority) return;

        bool wasAlreadyProviding = IsProvidingAttackSpeedAura;

        IsProvidingAttackSpeedAura = true;
        AttackSpeedAuraRadius = radius;
        AttackSpeedAuraAmount = bonus;
        AttackSpeedAuraDuration = duration;

        if (!wasAlreadyProviding)
        {
            // ✅ แสดงข้อความเฉพาะครั้งแรก
            RPC_ShowAuraBuffText(StatusEffectType.AttackSpeedAura, bonus, duration);
        }

        Debug.Log($"[AttackSpeedAura] {character.CharacterName} providing +{bonus * 100}% attack speed in {radius}m for {duration}s");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.AttackSpeedAura, true);
    }

    public virtual void ApplyDamageAura(float radius = 5f, float bonus = 0.2f, float duration = 15f)
    {
        if (!HasStateAuthority) return;

        bool wasAlreadyProviding = IsProvidingDamageAura;

        IsProvidingDamageAura = true;
        DamageAuraRadius = radius;
        DamageAuraAmount = bonus;
        DamageAuraDuration = duration;

        if (!wasAlreadyProviding)
        {
            RPC_ShowAuraBuffText(StatusEffectType.DamageAura, bonus, duration);
        }

        OnStatusEffectChanged?.Invoke(character, StatusEffectType.DamageAura, true);
    }


    public virtual void ApplyMoveSpeedAura(float radius = 5f, float bonus = 0.15f, float duration = 15f)
    {
        if (!HasStateAuthority) return;

        bool wasAlreadyProviding = IsProvidingMoveSpeedAura;

        IsProvidingMoveSpeedAura = true;
        MoveSpeedAuraRadius = radius;
        MoveSpeedAuraAmount = bonus;
        MoveSpeedAuraDuration = duration;

        if (!wasAlreadyProviding)
        {
            RPC_ShowAuraBuffText(StatusEffectType.MoveSpeedAura, bonus, duration);
        }

        OnStatusEffectChanged?.Invoke(character, StatusEffectType.MoveSpeedAura, true);
    }

    public virtual void ApplyProtectionAura(float radius = 6f, float reduction = 0.15f, float duration = 20f)
    {
        if (!HasStateAuthority) return;

        bool wasAlreadyProviding = IsProvidingProtectionAura;

        IsProvidingProtectionAura = true;
        ProtectionAuraRadius = radius;
        ProtectionAuraAmount = reduction;
        ProtectionAuraDuration = duration;

        if (!wasAlreadyProviding)
        {
            RPC_ShowAuraBuffText(StatusEffectType.ProtectionAura, reduction, duration);
        }

        OnStatusEffectChanged?.Invoke(character, StatusEffectType.ProtectionAura, true);
    }

    public virtual void ApplyArmorAura(float radius = 6f, float bonus = 0.2f, float duration = 20f)
    {
        if (!HasStateAuthority) return;

        IsProvidingArmorAura = true;
        ArmorAuraRadius = radius;
        ArmorAuraAmount = bonus;
        ArmorAuraDuration = duration;

        Debug.Log($"[ArmorAura] {character.CharacterName} providing +{bonus * 100}% armor in {radius}m for {duration}s");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.ArmorAura, true);
    }

    public virtual void ApplyCriticalAura(float radius = 6f, float bonus = 0.15f, float duration = 20f)
    {
        if (!HasStateAuthority) return;

        bool wasAlreadyProviding = IsProvidingCriticalAura;

        IsProvidingCriticalAura = true;
        CriticalAuraRadius = radius;
        CriticalAuraAmount = bonus;
        CriticalAuraDuration = duration;

        if (!wasAlreadyProviding)
        {
            RPC_ShowAuraBuffText(StatusEffectType.CriticalAura, bonus, duration);
        }

        OnStatusEffectChanged?.Invoke(character, StatusEffectType.CriticalAura, true);
    }

    // ========== Process Aura Durations ==========
    private void ProcessAllAuras()
    {
        // ========== Aura เดิม ==========
        if (IsProvidingAttackSpeedAura)
        {
            AttackSpeedAuraDuration -= Runner.DeltaTime;
            if (AttackSpeedAuraDuration <= 0) RemoveAttackSpeedAura();
        }

        if (IsProvidingDamageAura)
        {
            DamageAuraDuration -= Runner.DeltaTime;
            if (DamageAuraDuration <= 0) RemoveDamageAura();
        }

        if (IsProvidingMoveSpeedAura)
        {
            MoveSpeedAuraDuration -= Runner.DeltaTime;
            if (MoveSpeedAuraDuration <= 0) RemoveMoveSpeedAura();
        }

        if (IsProvidingProtectionAura)
        {
            ProtectionAuraDuration -= Runner.DeltaTime;
            if (ProtectionAuraDuration <= 0) RemoveProtectionAura();
        }

        if (IsProvidingArmorAura)
        {
            ArmorAuraDuration -= Runner.DeltaTime;
            if (ArmorAuraDuration <= 0) RemoveArmorAura();
        }

        if (IsProvidingCriticalAura)
        {
            CriticalAuraDuration -= Runner.DeltaTime;
            if (CriticalAuraDuration <= 0) RemoveCriticalAura();
        }

        // ========== 🆕 Aura ใหม่ ==========
        if (IsProvidingMagicDamageAura)
        {
            MagicDamageAuraDuration -= Runner.DeltaTime;
            if (MagicDamageAuraDuration <= 0) RemoveMagicDamageAura();
        }

        if (IsProvidingCooldownReductionAura)
        {
            CooldownReductionAuraDuration -= Runner.DeltaTime;
            if (CooldownReductionAuraDuration <= 0) RemoveCooldownReductionAura();
        }

        if (IsProvidingLifeStealAura)
        {
            LifeStealAuraDuration -= Runner.DeltaTime;
            if (LifeStealAuraDuration <= 0) RemoveLifeStealAura();
        }

        if (IsProvidingAmpDamageAura)
        {
            AmpDamageAuraDuration -= Runner.DeltaTime;
            if (AmpDamageAuraDuration <= 0) RemoveAmpDamageAura();
        }

        if (IsProvidingMagicArmorAura)
        {
            MagicArmorAuraDuration -= Runner.DeltaTime;
            if (MagicArmorAuraDuration <= 0) RemoveMagicArmorAura();
        }

        if (IsProvidingHealthRegenAura)
        {
            HealthRegenAuraDuration -= Runner.DeltaTime;
            if (HealthRegenAuraDuration <= 0) RemoveHealthRegenAura();
        }

        if (IsProvidingManaRegenAura)
        {
            ManaRegenAuraDuration -= Runner.DeltaTime;
            if (ManaRegenAuraDuration <= 0) RemoveManaRegenAura();
        }

        if (IsProvidingHitRateAura)
        {
            HitRateAuraDuration -= Runner.DeltaTime;
            if (HitRateAuraDuration <= 0) RemoveHitRateAura();
        }

        if (IsProvidingEvasionAura)
        {
            EvasionAuraDuration -= Runner.DeltaTime;
            if (EvasionAuraDuration <= 0) RemoveEvasionAura();
        }
        if (IsDebuffImmune)
        {
            DebuffImmunityDuration -= Runner.DeltaTime;
            if (DebuffImmunityDuration <= 0)
            {
                RemoveDebuffImmunity();
            }
        }
    }
    public virtual void ApplyDebuffImmunity(float duration)
    {
        if (!HasStateAuthority) return;

        IsDebuffImmune = true;
        DebuffImmunityDuration = duration;

        Debug.Log($"🛡️ [Debuff Immunity] {character.CharacterName} is immune for {duration}s");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.Stun, true); // ใช้ Stun เป็น visual indicator
    }

    public virtual void RemoveDebuffImmunity()
    {
        if (!HasStateAuthority) return;

        IsDebuffImmune = false;
        DebuffImmunityDuration = 0f;

        Debug.Log($"🛡️ [Debuff Immunity] {character.CharacterName} immunity ended");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.Stun, false);
    }
    // ========== Update Aura Effects (ตรวจสอบใครอยู่ในรัศมี) ==========
    private void UpdateAuraEffects()
    {
        // Reset ค่าที่ได้รับจาก aura
        ResetReceivedAuraBonuses();

        // ค้นหา Character ที่ใกล้เคียงผ่าน Physics.OverlapSphere
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, 10f, teamLayerMask);

        foreach (Collider col in nearbyColliders)
        {
            Character auraProvider = col.GetComponent<Character>();
            if (auraProvider == null || !auraProvider.IsSpawned) continue;

            StatusEffectManager providerStatus = auraProvider.GetComponent<StatusEffectManager>();
            if (providerStatus == null) continue;

            float distance = Vector3.Distance(transform.position, auraProvider.transform.position);

            // ตรวจสอบ aura แต่ละประเภท
            CheckAndApplyAuraBonus(providerStatus, distance);
        }

        // Debug info ถ้ามี aura
        if (IsReceivingAnyAura())
        {
          /*  Debug.Log($"[Aura Effects] {character.CharacterName} receiving: " +
                     $"ATK+{ReceivedAttackSpeedBonus:F2}, DMG+{ReceivedDamageBonus:F2}, " +
                     $"SPD+{ReceivedMoveSpeedBonus:F2}, PROT+{ReceivedProtectionBonus:F2}");*/
        }
    }

    // แทนที่ method เดิมด้วยโค้ดนี้
    private void CheckAndApplyAuraBonus(StatusEffectManager provider, float distance)
    {
        // ========== Aura เดิม ==========
        if (provider.IsProvidingAttackSpeedAura && distance <= provider.AttackSpeedAuraRadius)
        {
            ReceivedAttackSpeedBonus = Mathf.Max(ReceivedAttackSpeedBonus, provider.AttackSpeedAuraAmount);
        }

        if (provider.IsProvidingDamageAura && distance <= provider.DamageAuraRadius)
        {
            ReceivedDamageBonus = Mathf.Max(ReceivedDamageBonus, provider.DamageAuraAmount);
        }

        if (provider.IsProvidingMoveSpeedAura && distance <= provider.MoveSpeedAuraRadius)
        {
            ReceivedMoveSpeedBonus = Mathf.Max(ReceivedMoveSpeedBonus, provider.MoveSpeedAuraAmount);
        }

        if (provider.IsProvidingProtectionAura && distance <= provider.ProtectionAuraRadius)
        {
            ReceivedProtectionBonus = Mathf.Max(ReceivedProtectionBonus, provider.ProtectionAuraAmount);
        }

        if (provider.IsProvidingArmorAura && distance <= provider.ArmorAuraRadius)
        {
            ReceivedArmorBonus = Mathf.Max(ReceivedArmorBonus, provider.ArmorAuraAmount);
        }

        if (provider.IsProvidingCriticalAura && distance <= provider.CriticalAuraRadius)
        {
            ReceivedCriticalBonus = Mathf.Max(ReceivedCriticalBonus, provider.CriticalAuraAmount);
        }

        // ========== 🆕 Aura ใหม่ ==========
        if (provider.IsProvidingMagicDamageAura && distance <= provider.MagicDamageAuraRadius)
        {
            ReceivedMagicDamageBonus = Mathf.Max(ReceivedMagicDamageBonus, provider.MagicDamageAuraAmount);
        }

        if (provider.IsProvidingCooldownReductionAura && distance <= provider.CooldownReductionAuraRadius)
        {
            ReceivedCooldownReductionBonus = Mathf.Max(ReceivedCooldownReductionBonus, provider.CooldownReductionAuraAmount);
        }

        if (provider.IsProvidingLifeStealAura && distance <= provider.LifeStealAuraRadius)
        {
            ReceivedLifeStealBonus = Mathf.Max(ReceivedLifeStealBonus, provider.LifeStealAuraAmount);
        }

        if (provider.IsProvidingAmpDamageAura && distance <= provider.AmpDamageAuraRadius)
        {
            ReceivedAmpDamageBonus = Mathf.Max(ReceivedAmpDamageBonus, provider.AmpDamageAuraAmount);
        }

        if (provider.IsProvidingMagicArmorAura && distance <= provider.MagicArmorAuraRadius)
        {
            ReceivedMagicArmorBonus = Mathf.Max(ReceivedMagicArmorBonus, provider.MagicArmorAuraAmount);
        }

        if (provider.IsProvidingHealthRegenAura && distance <= provider.HealthRegenAuraRadius)
        {
            ReceivedHealthRegenBonus = Mathf.Max(ReceivedHealthRegenBonus, provider.HealthRegenAuraAmount);
        }

        if (provider.IsProvidingManaRegenAura && distance <= provider.ManaRegenAuraRadius)
        {
            ReceivedManaRegenBonus = Mathf.Max(ReceivedManaRegenBonus, provider.ManaRegenAuraAmount);
        }

        if (provider.IsProvidingHitRateAura && distance <= provider.HitRateAuraRadius)
        {
            ReceivedHitRateBonus = Mathf.Max(ReceivedHitRateBonus, provider.HitRateAuraAmount);
        }

        if (provider.IsProvidingEvasionAura && distance <= provider.EvasionAuraRadius)
        {
            ReceivedEvasionBonus = Mathf.Max(ReceivedEvasionBonus, provider.EvasionAuraAmount);
        }
    }

    // แทนที่ method เดิมด้วยโค้ดนี้
    private void ResetReceivedAuraBonuses()
    {
        // ========== Aura เดิม ==========
        ReceivedAttackSpeedBonus = 0f;
        ReceivedDamageBonus = 0f;
        ReceivedMoveSpeedBonus = 0f;
        ReceivedProtectionBonus = 0f;
        ReceivedArmorBonus = 0f;
        ReceivedCriticalBonus = 0f;

        // ========== 🆕 Aura ใหม่ ==========
        ReceivedMagicDamageBonus = 0f;
        ReceivedCooldownReductionBonus = 0f;
        ReceivedLifeStealBonus = 0f;
        ReceivedAmpDamageBonus = 0f;
        ReceivedMagicArmorBonus = 0f;
        ReceivedHealthRegenBonus = 0f;
        ReceivedManaRegenBonus = 0f;
        ReceivedHitRateBonus = 0f;
        ReceivedEvasionBonus = 0f;
    }
    #region Buff - เพิ่มต่อจาก Remove Aura Methods เดิม

    // ========== 🆕 Remove Magic Damage Aura ==========
    public virtual void RemoveMagicDamageAura()
    {
        if (!HasStateAuthority) return;
        IsProvidingMagicDamageAura = false;
        MagicDamageAuraDuration = 0f;
        Debug.Log($"{character.CharacterName} magic damage aura ended");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.MagicDamageAura, false);
    }

    // ========== 🆕 Remove Cooldown Reduction Aura ==========
    public virtual void RemoveCooldownReductionAura()
    {
        if (!HasStateAuthority) return;
        IsProvidingCooldownReductionAura = false;
        CooldownReductionAuraDuration = 0f;
        Debug.Log($"{character.CharacterName} cooldown reduction aura ended");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.CooldownReductionAura, false);
    }

    // ========== 🆕 Remove Life Steal Aura ==========
    public virtual void RemoveLifeStealAura()
    {
        if (!HasStateAuthority) return;
        IsProvidingLifeStealAura = false;
        LifeStealAuraDuration = 0f;
        Debug.Log($"{character.CharacterName} life steal aura ended");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.LifeStealAura, false);
    }

    // ========== 🆕 Remove Amp Damage Aura ==========
    public virtual void RemoveAmpDamageAura()
    {
        if (!HasStateAuthority) return;
        IsProvidingAmpDamageAura = false;
        AmpDamageAuraDuration = 0f;
        Debug.Log($"{character.CharacterName} amp damage aura ended");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.AmpDamageAura, false);
    }

    // ========== 🆕 Remove Magic Armor Aura ==========
    public virtual void RemoveMagicArmorAura()
    {
        if (!HasStateAuthority) return;
        IsProvidingMagicArmorAura = false;
        MagicArmorAuraDuration = 0f;
        Debug.Log($"{character.CharacterName} magic armor aura ended");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.MagicArmorAura, false);
    }

    // ========== 🆕 Remove Health Regen Aura ==========
    public virtual void RemoveHealthRegenAura()
    {
        if (!HasStateAuthority) return;
        IsProvidingHealthRegenAura = false;
        HealthRegenAuraDuration = 0f;
        Debug.Log($"{character.CharacterName} health regen aura ended");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.HealthRegenAura, false);
    }

    // ========== 🆕 Remove Mana Regen Aura ==========
    public virtual void RemoveManaRegenAura()
    {
        if (!HasStateAuthority) return;
        IsProvidingManaRegenAura = false;
        ManaRegenAuraDuration = 0f;
        Debug.Log($"{character.CharacterName} mana regen aura ended");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.ManaRegenAura, false);
    }

    // ========== 🆕 Remove Hit Rate Aura ==========
    public virtual void RemoveHitRateAura()
    {
        if (!HasStateAuthority) return;
        IsProvidingHitRateAura = false;
        HitRateAuraDuration = 0f;
        Debug.Log($"{character.CharacterName} hit rate aura ended");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.HitRateAura, false);
    }

    // ========== 🆕 Remove Evasion Aura ==========
    public virtual void RemoveEvasionAura()
    {
        if (!HasStateAuthority) return;
        IsProvidingEvasionAura = false;
        EvasionAuraDuration = 0f;
        Debug.Log($"{character.CharacterName} evasion aura ended");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.EvasionAura, false);
    }

    #endregion
    // ========== Remove Aura Methods ==========
    public virtual void RemoveAttackSpeedAura()
    {
        if (!HasStateAuthority) return;
        IsProvidingAttackSpeedAura = false;
        AttackSpeedAuraDuration = 0f;
        Debug.Log($"{character.CharacterName} attack speed aura ended");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.AttackSpeedAura, false);
    }

    public virtual void RemoveDamageAura()
    {
        if (!HasStateAuthority) return;
        IsProvidingDamageAura = false;
        DamageAuraDuration = 0f;
        Debug.Log($"{character.CharacterName} damage aura ended");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.DamageAura, false);
    }

    public virtual void RemoveMoveSpeedAura()
    {
        if (!HasStateAuthority) return;
        IsProvidingMoveSpeedAura = false;
        MoveSpeedAuraDuration = 0f;
        Debug.Log($"{character.CharacterName} move speed aura ended");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.MoveSpeedAura, false);
    }

    public virtual void RemoveProtectionAura()
    {
        if (!HasStateAuthority) return;
        IsProvidingProtectionAura = false;
        ProtectionAuraDuration = 0f;
        Debug.Log($"{character.CharacterName} protection aura ended");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.ProtectionAura, false);
    }

    public virtual void RemoveArmorAura()
    {
        if (!HasStateAuthority) return;
        IsProvidingArmorAura = false;
        ArmorAuraDuration = 0f;
        Debug.Log($"{character.CharacterName} armor aura ended");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.ArmorAura, false);
    }

    public virtual void RemoveCriticalAura()
    {
        if (!HasStateAuthority) return;
        IsProvidingCriticalAura = false;
        CriticalAuraDuration = 0f;
        Debug.Log($"{character.CharacterName} critical aura ended");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.CriticalAura, false);
    }

    #endregion


    // ========== Public Query Methods ==========
    public bool IsProvidingAnyAura()
    {
        return IsProvidingAttackSpeedAura || IsProvidingDamageAura || IsProvidingMoveSpeedAura ||
               IsProvidingProtectionAura || IsProvidingArmorAura || IsProvidingCriticalAura ||
               // 🆕 เพิ่ม aura ใหม่
               IsProvidingMagicDamageAura || IsProvidingCooldownReductionAura || IsProvidingLifeStealAura ||
               IsProvidingAmpDamageAura || IsProvidingMagicArmorAura || IsProvidingHealthRegenAura ||
               IsProvidingManaRegenAura || IsProvidingHitRateAura || IsProvidingEvasionAura;
    }

    public bool IsReceivingAnyAura()
    {
        return ReceivedAttackSpeedBonus > 0 || ReceivedDamageBonus > 0 || ReceivedMoveSpeedBonus > 0 ||
               ReceivedProtectionBonus > 0 || ReceivedArmorBonus > 0 || ReceivedCriticalBonus > 0 ||
               // 🆕 เพิ่ม aura ใหม่
               ReceivedMagicDamageBonus > 0 || ReceivedCooldownReductionBonus > 0 || ReceivedLifeStealBonus > 0 ||
               ReceivedAmpDamageBonus > 0 || ReceivedMagicArmorBonus > 0 || ReceivedHealthRegenBonus > 0 ||
               ReceivedManaRegenBonus > 0 || ReceivedHitRateBonus > 0 || ReceivedEvasionBonus > 0;
    }

    public void ClearAllAuras()
    {
        if (!HasStateAuthority) return;

        // ========== Aura เดิม ==========
        if (IsProvidingAttackSpeedAura) RemoveAttackSpeedAura();
        if (IsProvidingDamageAura) RemoveDamageAura();
        if (IsProvidingMoveSpeedAura) RemoveMoveSpeedAura();
        if (IsProvidingProtectionAura) RemoveProtectionAura();
        if (IsProvidingArmorAura) RemoveArmorAura();
        if (IsProvidingCriticalAura) RemoveCriticalAura();

        // ========== 🆕 Aura ใหม่ ==========
        if (IsProvidingMagicDamageAura) RemoveMagicDamageAura();
        if (IsProvidingCooldownReductionAura) RemoveCooldownReductionAura();
        if (IsProvidingLifeStealAura) RemoveLifeStealAura();
        if (IsProvidingAmpDamageAura) RemoveAmpDamageAura();
        if (IsProvidingMagicArmorAura) RemoveMagicArmorAura();
        if (IsProvidingHealthRegenAura) RemoveHealthRegenAura();
        if (IsProvidingManaRegenAura) RemoveManaRegenAura();
        if (IsProvidingHitRateAura) RemoveHitRateAura();
        if (IsProvidingEvasionAura) RemoveEvasionAura();
    }

    // 🆕 เพิ่ม Getter Methods ใหม่
    public float GetTotalMagicDamageMultiplier()
    {
        float multiplier = 1f + ReceivedMagicDamageBonus;

        // ✅ เพิ่ม debug log
        if (ReceivedMagicDamageBonus > 0)
        {
            Debug.Log($"[Magic Damage Aura] {character.CharacterName}: " +
                     $"Received Bonus = {ReceivedMagicDamageBonus:F2} ({ReceivedMagicDamageBonus * 100f:F1}%), " +
                     $"Multiplier = {multiplier:F2}");
        }

        return multiplier;
    }


    public float GetTotalCooldownReductionBonus()
    {
        return ReceivedCooldownReductionBonus;
    }

    public float GetTotalLifeStealBonus()
    {
        return ReceivedLifeStealBonus;
    }

    public float GetTotalAmpDamageBonus()
    {
        return ReceivedAmpDamageBonus;
    }

    public float GetTotalMagicArmorMultiplier()
    {
        return 1f + ReceivedMagicArmorBonus;
    }

    public float GetTotalHealthRegenMultiplier()
    {
        return 1f + ReceivedHealthRegenBonus;
    }

    public float GetTotalManaRegenMultiplier()
    {
        return 1f + ReceivedManaRegenBonus;
    }

    public float GetTotalHitRateBonus()
    {
        return ReceivedHitRateBonus;
    }

    public float GetTotalEvasionBonus()
    {
        return ReceivedEvasionBonus;
    }
    public float GetTotalDamageMultiplier()
    {
        return 1f + ReceivedDamageBonus;
    }

    /// <summary>
    /// ใช้โดย Character เพื่อคำนวณ attack speed
    /// </summary>
    public float GetTotalAttackSpeedMultiplier()
    {
        return 1f + ReceivedAttackSpeedBonus;
    }

    /// <summary>
    /// ใช้โดย Character เพื่อคำนวณ move speed
    /// </summary>
    public float GetTotalMoveSpeedMultiplier()
    {
        return 1f + ReceivedMoveSpeedBonus;
    }

    /// <summary>
    /// ใช้โดย CombatManager เพื่อคำนวณ damage reduction
    /// </summary>
    public float GetTotalDamageReduction()
    {
        return ReceivedProtectionBonus;
    }

    /// <summary>
    /// ใช้โดย CombatManager เพื่อคำนวณ armor bonus
    /// </summary>
    public float GetTotalArmorMultiplier()
    {
        return 1f + ReceivedArmorBonus;
    }

    /// <summary>
    /// ใช้โดย CombatManager เพื่อคำนวณ critical bonus
    /// </summary>
    public float GetTotalCriticalBonus()
    {
        return ReceivedCriticalBonus;
    }
    // ========== Helper Methods ==========
    // ใน StatusEffectManager.cs - แทนที่ method เดิม
    private float GetMagicalResistance()
    {
        // ✅ ใช้ GetEffectiveMagicArmor() แทน MagicArmor โดยตรง
        int totalMagicArmor = character.GetEffectiveMagicArmor();

        // ✅ สูตรเดียวกับเดิม
        float resistancePercent;

        if (totalMagicArmor <= 100)
        {
            resistancePercent = totalMagicArmor * 0.5f;
        }
        else if (totalMagicArmor <= 500)
        {
            float excess = totalMagicArmor - 100f;
            resistancePercent = 50f + (excess * 0.0625f);
        }
        else
        {
            float excess = totalMagicArmor - 500f;
            resistancePercent = 75f + (excess * 0.01f);
            resistancePercent = Mathf.Min(resistancePercent, 90f);
        }

        Debug.Log($"[Magical Resistance] {character.CharacterName}: Magic Armor {totalMagicArmor} = {resistancePercent:F1}% resistance");

        return resistancePercent;
    }

    // ใน StatusEffectManager.cs - แทนที่ method เดิม
    private float GetPhysicalResistance()
    {
        // ✅ ใช้ GetEffectiveArmor() แทน Armor โดยตรง
        int totalArmor = character.GetEffectiveArmor();

        // ✅ สูตรเดียวกับเดิม
        float resistancePercent;

        if (totalArmor <= 100)
        {
            resistancePercent = totalArmor * 0.5f;
        }
        else if (totalArmor <= 500)
        {
            float excess = totalArmor - 100f;
            resistancePercent = 50f + (excess * 0.0625f);
        }
        else
        {
            float excess = totalArmor - 500f;
            resistancePercent = 75f + (excess * 0.01f);
            resistancePercent = Mathf.Min(resistancePercent, 90f);
        }

        Debug.Log($"[Physical Resistance] {character.CharacterName}: Armor {totalArmor} = {resistancePercent:F1}% resistance");

        return resistancePercent;
    }


    // ========== Public Query Methods ==========
    public bool HasAnyStatusEffect()
    {
        return IsPoisoned || IsBurning || IsBleeding || IsFrozen ||
               IsStunned || IsArmorBreak || IsBlind || IsWeak;
    }

    public void ClearAllStatusEffects()
    {
        if (!HasStateAuthority) return;

        // Magical Effects
        if (IsPoisoned) RemovePoison();
        if (IsBurning) RemoveBurn();
        if (IsBleeding) RemoveBleed();
        if (IsFrozen) RemoveFreeze();

        // Physical Effects
        if (IsStunned) RemoveStun();
        if (IsArmorBreak) RemoveArmorBreak();
        if (IsBlind) RemoveBlind();
        if (IsWeak) RemoveWeakness();
        if (IsSlowed) RemoveSlow();  // 🆕 เพิ่มบรรทัดนี้

    }

    // ========== Status Effect Getters ==========
    public bool HasMagicalEffect()
    {
        return IsPoisoned || IsBurning || IsBleeding || IsFrozen;
    }

    public bool HasPhysicalEffect()
    {
        return IsStunned || IsArmorBreak || IsBlind || IsWeak || IsSlowed;
    }

    public bool HasDebuff()
    {
        return IsArmorBreak || IsBlind || IsWeak || IsFrozen || IsStunned || IsSlowed;
    }

    public bool HasDamageOverTime()
    {
        return IsPoisoned || IsBurning || IsBleeding;
    }
    public bool IsMagicalStatusEffect(StatusEffectType effectType)
    {
        return effectType == StatusEffectType.Poison ||
               effectType == StatusEffectType.Burn ||
               effectType == StatusEffectType.Bleed ||
               effectType == StatusEffectType.Freeze;
    }

    /// <summary>
    /// Physical Status Effects ที่ใช้ Physical Armor ป้องกัน
    /// </summary>
    public bool IsPhysicalStatusEffect(StatusEffectType effectType)
    {
        return effectType == StatusEffectType.Stun ||
               effectType == StatusEffectType.ArmorBreak ||
               effectType == StatusEffectType.Blind ||

               effectType == StatusEffectType.Weakness ||
           effectType == StatusEffectType.Slow; 

    }

    // ========== Debug Methods ==========
    public float GetStatusResistanceChance(StatusEffectType effectType)
    {
        float resistance;

        if (IsMagicalStatusEffect(effectType))
        {
            resistance = GetMagicalResistance();
        }
        else if (IsPhysicalStatusEffect(effectType))
        {
            resistance = GetPhysicalResistance();
        }
        else
        {
            return 0f; // Aura effects ไม่มี resistance
        }

        return resistance * 0.6f; // 60% ของ resistance เป็นโอกาสป้องกัน
    }

    /// <summary>
    /// คำนวณ duration reduction สำหรับ Status Effect
    /// </summary>
    /// <param name="effectType">ประเภท Status Effect</param>
    /// <param name="originalDuration">ระยะเวลาเดิม</param>
    /// <returns>ระยะเวลาหลังลด</returns>
    public float GetReducedDuration(StatusEffectType effectType, float originalDuration)
    {
        float resistance;

        if (IsMagicalStatusEffect(effectType))
        {
            resistance = GetMagicalResistance();
        }
        else if (IsPhysicalStatusEffect(effectType))
        {
            resistance = GetPhysicalResistance();
        }
        else
        {
            return originalDuration; // Aura effects ไม่มี resistance
        }

        float durationReduction = resistance / 100f;
        float reducedDuration = originalDuration * (1f - durationReduction);

        return Mathf.Max(0.5f, reducedDuration); // อย่างน้อย 0.5 วินาที
    }

    /// <summary>
    /// แสดงข้อมูล Status Effect Resistance แบบสั้น
    /// </summary>
    public string GetStatusResistanceInfo()
    {
        float physicalRes = GetPhysicalResistance();
        float magicalRes = GetMagicalResistance();

        return $"Physical Resistance: {physicalRes:F1}% (Armor: {character.Armor})\n" +
               $"Magical Resistance: {magicalRes:F1}% (Magic Armor: {character.MagicArmor})";
    }
    /// <summary>
    /// แสดงข้อมูล resistance ทั้งหมด
    /// </summary>
    // เพิ่มในส่วนท้ายของ class (ก่อน closing brace)

    #region Status Effect Text Display

    /// <summary>
    /// แสดงข้อความเมื่อได้รับ Status Effect (Debuff)
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowStatusEffectText(StatusEffectType effectType, int damagePerTick, float duration, float reduction = 0f)
    {
        Vector3 textPosition = character.transform.position + Vector3.up * 1f;
        string message = "";
        Color color = Color.white;

        switch (effectType)
        {
            case StatusEffectType.Poison:
                message = $"POISONED";
                color = new Color(0.2f, 0.8f, 0.2f, 1f); // เขียวพิษ
                break;

            case StatusEffectType.Burn:
                message = $"BURNING";
                color = new Color(1f, 0.3f, 0f, 1f); // ส้มไฟ
                break;

            case StatusEffectType.Bleed:
                message = $"BLEEDING ";
                color = new Color(0.8f, 0f, 0f, 1f); // แดงเลือด
                break;

            case StatusEffectType.Stun:
                message = "STUNNED";
                color = new Color(1f, 1f, 0f, 1f); // เหลือง
                break;

            case StatusEffectType.Weakness:
                message = $"WEAKENED (-{reduction * 100:F0}%)";
                color = new Color(0.6f, 0.4f, 0.8f, 1f); // ม่วงอ่อน
                break;

            case StatusEffectType.Blind:
                message = $"BLINDED (-{reduction * 100:F0}%)";
                color = new Color(0.3f, 0.3f, 0.3f, 1f); // เทาเข้ม
                break;

            case StatusEffectType.ArmorBreak:
                message = $"ARMOR BREAK (-{reduction * 100:F0}%)";
                color = new Color(0.8f, 0.5f, 0f, 1f); // ส้มเข้ม
                break;

            case StatusEffectType.Freeze:
                message = "FROZEN";
                color = new Color(0.5f, 0.7f, 1f, 1f); // ฟ้าน้ำแข็ง
                break;
            case StatusEffectType.Slow:
                message = $"SLOWED (-{reduction * 100:F0}%)";
                color = new Color(0.6f, 0.8f, 1f, 1f); // ฟ้าอ่อน
                break;
        }

        // แสดงข้อความ
        DamageTextManager.ShowCustomText(textPosition, message, color, 0.7f);

        Debug.Log($"[Status Text] {character.CharacterName}: {message}");
    }

    /// <summary>
    /// แสดงข้อความเมื่อ Resist Status Effect สำเร็จ
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowStatusResistText(StatusEffectType effectType)
    {
        Vector3 textPosition = character.transform.position + Vector3.up * 1f;
        string message = "RESISTED";
        Color color = new Color(0.5f, 0.9f, 1f, 1f); // ฟ้าสว่าง

        DamageTextManager.ShowCustomText(textPosition, message, color, 0.8f);

        Debug.Log($"[Resist Text] {character.CharacterName} resisted {effectType}");
    }

    /// <summary>
    /// แสดงข้อความเมื่อได้รับ Aura Buff
    /// </summary>
    // แทนที่ RPC method เดิม - เพิ่ม cases ใหม่
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowAuraBuffText(StatusEffectType auraType, float bonus, float duration)
    {
        Vector3 textPosition = character.transform.position + Vector3.up * 1f;
        string message = "";
        Color color = Color.white;

        switch (auraType)
        {
            // ========== Aura เดิม ==========
            case StatusEffectType.AttackSpeedAura:
                message = $"ATK SPEED +{bonus * 100:F0}%";
                color = new Color(1f, 1f, 0.3f, 1f);
                break;

            case StatusEffectType.DamageAura:
                message = $"DAMAGE +{bonus * 100:F0}%";
                color = new Color(1f, 0.3f, 0.3f, 1f);
                break;

            case StatusEffectType.MoveSpeedAura:
                message = $"SPEED +{bonus * 100:F0}%";
                color = new Color(0.3f, 1f, 0.3f, 1f);
                break;

            case StatusEffectType.ProtectionAura:
                message = $"PROTECTION -{bonus * 100:F0}%";
                color = new Color(0.5f, 0.7f, 1f, 1f);
                break;

            case StatusEffectType.ArmorAura:
                message = $"ARMOR +{bonus * 100:F0}%";
                color = new Color(0.7f, 0.7f, 0.7f, 1f);
                break;

            case StatusEffectType.CriticalAura:
                message = $"CRIT +{bonus * 100:F0}%";
                color = new Color(1f, 0.7f, 0f, 1f);
                break;

            // ========== 🆕 Aura ใหม่ ==========
            case StatusEffectType.MagicDamageAura:
                message = $"MAGIC DMG +{bonus * 100:F0}%";
                color = new Color(0.6f, 0.3f, 1f, 1f); // ม่วง
                break;

            case StatusEffectType.CooldownReductionAura:
                message = $"CDR +{bonus * 100:F0}%";
                color = new Color(0.3f, 0.9f, 1f, 1f); // ฟ้าสว่าง
                break;

            case StatusEffectType.LifeStealAura:
                message = $"LIFE STEAL +{bonus * 100:F0}%";
                color = new Color(1f, 0.2f, 0.2f, 1f); // แดงเลือด
                break;

            case StatusEffectType.AmpDamageAura:
                message = $"AMP DMG +{bonus * 100:F0}%";
                color = new Color(1f, 0.5f, 0f, 1f); // ส้มสว่าง
                break;

            case StatusEffectType.MagicArmorAura:
                message = $"MAGIC ARM +{bonus * 100:F0}%";
                color = new Color(0.5f, 0.5f, 1f, 1f); // ม่วงอ่อน
                break;

            case StatusEffectType.HealthRegenAura:
                message = $"HP REGEN +{bonus * 100:F0}%";
                color = new Color(0.2f, 1f, 0.2f, 1f); // เขียวสว่าง
                break;

            case StatusEffectType.ManaRegenAura:
                message = $"MP REGEN +{bonus * 100:F0}%";
                color = new Color(0.3f, 0.6f, 1f, 1f); // ฟ้าน้ำเงิน
                break;

            case StatusEffectType.HitRateAura:
                message = $"HIT RATE +{bonus * 100:F0}%";
                color = new Color(1f, 1f, 0.5f, 1f); // เหลืองอ่อน
                break;

            case StatusEffectType.EvasionAura:
                message = $"EVASION +{bonus * 100:F0}%";
                color = new Color(0.8f, 0.8f, 0.8f, 1f); // เทาสว่าง
                break;
        }

        // แสดงข้อความ
        DamageTextManager.ShowCustomText(textPosition, message, color, 0.8f);

        Debug.Log($"[Aura Buff] {character.CharacterName}: {message}");
    }

    #endregion

}