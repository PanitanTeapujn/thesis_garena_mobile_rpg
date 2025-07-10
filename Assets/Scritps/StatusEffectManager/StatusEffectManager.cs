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

         // ========== 🌟 TEAM AURA BUFFS ==========
    AttackSpeedAura,    // +30% attack speed รัศมี 5m
    DamageAura,         // +20% damage รัศมี 5m  
    MoveSpeedAura,      // +15% move speed รัศมี 5m
    ProtectionAura,     // -15% damage taken รัศมี 6m
    ArmorAura,          // +20% armor รัศมี 6m
    CriticalAura        // +15% critical chance รัศมี 6m
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
    [Networked] public float OriginalMoveSpeed { get; set; }
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

    [Networked] public bool IsWeak { get; set; }
    [Networked] public float WeaknessDuration { get; set; }
    [Networked] public float WeaknessAmount { get; set; } // 0.4 = 40% reduction
    #endregion
#region AuraBuff

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
    [Networked] public float ReceivedAttackSpeedBonus { get; set; }
    [Networked] public float ReceivedDamageBonus { get; set; }
    [Networked] public float ReceivedMoveSpeedBonus { get; set; }
    [Networked] public float ReceivedProtectionBonus { get; set; }
    [Networked] public float ReceivedArmorBonus { get; set; }
    [Networked] public float ReceivedCriticalBonus { get; set; }

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
    public virtual void ApplyPoison(int damagePerTick, float duration)
    {
        if (!HasStateAuthority) return;

        // 🧪 คำนวณ resistance from equipment
        float totalResistance = GetMagicalResistance();

        // ตรวจสอบโอกาสป้องกัน
        float chanceReduction = totalResistance * 0.6f;
        if (UnityEngine.Random.Range(0f, 100f) < chanceReduction)
        {
            Debug.Log($"[Poison Resisted] {character.CharacterName} resisted poison! ({chanceReduction:F1}% chance)");
            return;
        }

        // ลดระยะเวลาและดาเมจตาม resistance
        float durationReduction = totalResistance / 100f;
        float damageReduction = (totalResistance * 0.5f) / 100f;

        duration = duration * (1f - durationReduction);
        damagePerTick = Mathf.RoundToInt(damagePerTick * (1f - damageReduction));

        bool wasAlreadyPoisoned = IsPoisoned;

        // Set poison status
        IsPoisoned = true;
        PoisonDamagePerTick = Mathf.Max(1, damagePerTick);
        PoisonDuration = Mathf.Max(0.5f, duration);

        float currentTime = (float)Runner.SimulationTime;
        if (!wasAlreadyPoisoned)
        {
            PoisonNextTickTime = currentTime + 0.1f;
        }

        Debug.Log($"[ApplyPoison] {character.CharacterName} is poisoned! {PoisonDamagePerTick} damage per {poisonTickInterval}s for {PoisonDuration:F1}s");

        // แจ้ง visual manager
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
        if (!HasStateAuthority) return;

        // คำนวณ resistance from equipment
        float totalResistance = GetPhysicalResistance();

        // ตรวจสอบโอกาสป้องกัน
        float chanceReduction = totalResistance * 0.6f;
        if (UnityEngine.Random.Range(0f, 100f) < chanceReduction)
        {
            Debug.Log($"[Stun Resisted] {character.CharacterName} resisted stun! ({chanceReduction:F1}% chance)");
            return;
        }

        // ลดระยะเวลาตาม resistance
        float durationReduction = totalResistance / 100f;
        duration = duration * (1f - durationReduction);

        IsStunned = true;
        StunDuration = Mathf.Max(0.5f, duration);

        // หยุดการเคลื่อนไหวทั้งหมด
        if (character.rb != null)
        {
            character.rb.velocity = Vector3.zero;
        }

        Debug.Log($"[ApplyStun] {character.CharacterName} is stunned for {StunDuration:F1}s!");
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
    public virtual void ApplyFreeze(float duration)
    {
        if (!HasStateAuthority) return;

        // คำนวณ resistance from equipment
        float totalResistance = GetMagicalResistance();

        // ตรวจสอบโอกาสป้องกัน
        float chanceReduction = totalResistance * 0.6f;
        if (UnityEngine.Random.Range(0f, 100f) < chanceReduction)
        {
            Debug.Log($"[Freeze Resisted] {character.CharacterName} resisted freeze! ({chanceReduction:F1}% chance)");
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
            OriginalMoveSpeed = character.MoveSpeed;
            character.MoveSpeed *= 0.3f; // ลดความเร็วเหลือ 30%

            if (character.rb != null)
            {
                character.rb.velocity = Vector3.zero;
            }
        }

        Debug.Log($"[ApplyFreeze] {character.CharacterName} is frozen for {FreezeDuration:F1}s!");
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

        IsFrozen = false;
        FreezeDuration = 0f;

        // คืนค่าความเร็ว
        character.MoveSpeed = OriginalMoveSpeed;

        Debug.Log($"{character.CharacterName} is no longer frozen. Move speed restored: {character.MoveSpeed}");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.Freeze, false);
    }

    // ========== 🔥 Burn System ==========
    public virtual void ApplyBurn(int damagePerTick, float duration)
    {
        if (!HasStateAuthority) return;

        // 🔥 คำนวณ resistance (Magical)
        float totalResistance = GetMagicalResistance();

        // ตรวจสอบโอกาสป้องกัน
        float chanceReduction = totalResistance * 0.6f;
        if (UnityEngine.Random.Range(0f, 100f) < chanceReduction)
        {
            Debug.Log($"[Burn Resisted] {character.CharacterName} resisted burn! ({chanceReduction:F1}% chance)");
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

        Debug.Log($"[ApplyBurn] {character.CharacterName} is burning! {BurnDamagePerTick} damage per {burnTickInterval}s for {BurnDuration:F1}s");
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
        if (!HasStateAuthority) return;

        // 🩸 คำนวณ resistance (Magical)
        float totalResistance = GetMagicalResistance();

        // ตรวจสอบโอกาสป้องกัน
        float chanceReduction = totalResistance * 0.6f;
        if (UnityEngine.Random.Range(0f, 100f) < chanceReduction)
        {
            Debug.Log($"[Bleed Resisted] {character.CharacterName} resisted bleed! ({chanceReduction:F1}% chance)");
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

        Debug.Log($"[ApplyBleed] {character.CharacterName} is bleeding! {BleedDamagePerTick} damage per {bleedTickInterval}s for {BleedDuration:F1}s");
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
        if (!HasStateAuthority) return;

        // 🛡️ คำนวณ resistance (Physical)
        float totalResistance = GetPhysicalResistance();

        // ตรวจสอบโอกาสป้องกัน
        float chanceReduction = totalResistance * 0.6f;
        if (UnityEngine.Random.Range(0f, 100f) < chanceReduction)
        {
            Debug.Log($"[Armor Break Resisted] {character.CharacterName} resisted armor break! ({chanceReduction:F1}% chance)");
            return;
        }

        // ลดระยะเวลาตาม resistance
        float durationReduction = totalResistance / 100f;
        duration = duration * (1f - durationReduction);

        IsArmorBreak = true;
        ArmorBreakDuration = Mathf.Max(0.5f, duration);
        ArmorBreakAmount = reduction;

        Debug.Log($"[ApplyArmorBreak] {character.CharacterName} armor broken! Reduction: {reduction * 100}% for {ArmorBreakDuration:F1}s");
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
        if (!HasStateAuthority) return;

        // 👁️ คำนวณ resistance (Physical)
        float totalResistance = GetPhysicalResistance();

        // ตรวจสอบโอกาสป้องกัน
        float chanceReduction = totalResistance * 0.6f;
        if (UnityEngine.Random.Range(0f, 100f) < chanceReduction)
        {
            Debug.Log($"[Blind Resisted] {character.CharacterName} resisted blind! ({chanceReduction:F1}% chance)");
            return;
        }

        // ลดระยะเวลาตาม resistance
        float durationReduction = totalResistance / 100f;
        duration = duration * (1f - durationReduction);

        IsBlind = true;
        BlindDuration = Mathf.Max(0.5f, duration);
        BlindAmount = reduction;

        Debug.Log($"[ApplyBlind] {character.CharacterName} is blinded! Critical reduction: {reduction * 100}% for {BlindDuration:F1}s");
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
        if (!HasStateAuthority) return;

        // 💪 คำนวณ resistance (Physical)
        float totalResistance = GetPhysicalResistance();

        // ตรวจสอบโอกาสป้องกัน
        float chanceReduction = totalResistance * 0.6f;
        if (UnityEngine.Random.Range(0f, 100f) < chanceReduction)
        {
            Debug.Log($"[Weakness Resisted] {character.CharacterName} resisted weakness! ({chanceReduction:F1}% chance)");
            return;
        }

        // ลดระยะเวลาตาม resistance
        float durationReduction = totalResistance / 100f;
        duration = duration * (1f - durationReduction);

        IsWeak = true;
        WeaknessDuration = Mathf.Max(0.5f, duration);
        WeaknessAmount = reduction;

        Debug.Log($"[ApplyWeakness] {character.CharacterName} is weakened! Attack reduction: {reduction * 100}% for {WeaknessDuration:F1}s");
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

    public virtual void ApplyAttackSpeedAura(float radius = 5f, float bonus = 0.3f, float duration = 15f)
    {
        if (!HasStateAuthority) return;

        IsProvidingAttackSpeedAura = true;
        AttackSpeedAuraRadius = radius;
        AttackSpeedAuraAmount = bonus;
        AttackSpeedAuraDuration = duration;

        Debug.Log($"[AttackSpeedAura] {character.CharacterName} providing +{bonus * 100}% attack speed in {radius}m for {duration}s");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.AttackSpeedAura, true);
    }

    public virtual void ApplyDamageAura(float radius = 5f, float bonus = 0.2f, float duration = 15f)
    {
        if (!HasStateAuthority) return;

        IsProvidingDamageAura = true;
        DamageAuraRadius = radius;
        DamageAuraAmount = bonus;
        DamageAuraDuration = duration;

        Debug.Log($"[DamageAura] {character.CharacterName} providing +{bonus * 100}% damage in {radius}m for {duration}s");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.DamageAura, true);
    }

    public virtual void ApplyMoveSpeedAura(float radius = 5f, float bonus = 0.15f, float duration = 15f)
    {
        if (!HasStateAuthority) return;

        IsProvidingMoveSpeedAura = true;
        MoveSpeedAuraRadius = radius;
        MoveSpeedAuraAmount = bonus;
        MoveSpeedAuraDuration = duration;

        Debug.Log($"[MoveSpeedAura] {character.CharacterName} providing +{bonus * 100}% move speed in {radius}m for {duration}s");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.MoveSpeedAura, true);
    }

    public virtual void ApplyProtectionAura(float radius = 6f, float reduction = 0.15f, float duration = 20f)
    {
        if (!HasStateAuthority) return;

        IsProvidingProtectionAura = true;
        ProtectionAuraRadius = radius;
        ProtectionAuraAmount = reduction;
        ProtectionAuraDuration = duration;

        Debug.Log($"[ProtectionAura] {character.CharacterName} providing -{reduction * 100}% damage taken in {radius}m for {duration}s");
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

        IsProvidingCriticalAura = true;
        CriticalAuraRadius = radius;
        CriticalAuraAmount = bonus;
        CriticalAuraDuration = duration;

        Debug.Log($"[CriticalAura] {character.CharacterName} providing +{bonus * 100}% critical in {radius}m for {duration}s");
        OnStatusEffectChanged?.Invoke(character, StatusEffectType.CriticalAura, true);
    }

    // ========== Process Aura Durations ==========
    private void ProcessAllAuras()
    {
        // Attack Speed Aura
        if (IsProvidingAttackSpeedAura)
        {
            AttackSpeedAuraDuration -= Runner.DeltaTime;
            if (AttackSpeedAuraDuration <= 0)
            {
                RemoveAttackSpeedAura();
            }
        }

        // Damage Aura
        if (IsProvidingDamageAura)
        {
            DamageAuraDuration -= Runner.DeltaTime;
            if (DamageAuraDuration <= 0)
            {
                RemoveDamageAura();
            }
        }

        // Move Speed Aura
        if (IsProvidingMoveSpeedAura)
        {
            MoveSpeedAuraDuration -= Runner.DeltaTime;
            if (MoveSpeedAuraDuration <= 0)
            {
                RemoveMoveSpeedAura();
            }
        }

        // Protection Aura
        if (IsProvidingProtectionAura)
        {
            ProtectionAuraDuration -= Runner.DeltaTime;
            if (ProtectionAuraDuration <= 0)
            {
                RemoveProtectionAura();
            }
        }

        // Armor Aura
        if (IsProvidingArmorAura)
        {
            ArmorAuraDuration -= Runner.DeltaTime;
            if (ArmorAuraDuration <= 0)
            {
                RemoveArmorAura();
            }
        }

        // Critical Aura
        if (IsProvidingCriticalAura)
        {
            CriticalAuraDuration -= Runner.DeltaTime;
            if (CriticalAuraDuration <= 0)
            {
                RemoveCriticalAura();
            }
        }
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

    private void CheckAndApplyAuraBonus(StatusEffectManager provider, float distance)
    {
        // Attack Speed Aura
        if (provider.IsProvidingAttackSpeedAura && distance <= provider.AttackSpeedAuraRadius)
        {
            ReceivedAttackSpeedBonus = Mathf.Max(ReceivedAttackSpeedBonus, provider.AttackSpeedAuraAmount);
        }

        // Damage Aura
        if (provider.IsProvidingDamageAura && distance <= provider.DamageAuraRadius)
        {
            ReceivedDamageBonus = Mathf.Max(ReceivedDamageBonus, provider.DamageAuraAmount);
        }

        // Move Speed Aura
        if (provider.IsProvidingMoveSpeedAura && distance <= provider.MoveSpeedAuraRadius)
        {
            ReceivedMoveSpeedBonus = Mathf.Max(ReceivedMoveSpeedBonus, provider.MoveSpeedAuraAmount);
        }

        // Protection Aura
        if (provider.IsProvidingProtectionAura && distance <= provider.ProtectionAuraRadius)
        {
            ReceivedProtectionBonus = Mathf.Max(ReceivedProtectionBonus, provider.ProtectionAuraAmount);
        }

        // Armor Aura
        if (provider.IsProvidingArmorAura && distance <= provider.ArmorAuraRadius)
        {
            ReceivedArmorBonus = Mathf.Max(ReceivedArmorBonus, provider.ArmorAuraAmount);
        }

        // Critical Aura
        if (provider.IsProvidingCriticalAura && distance <= provider.CriticalAuraRadius)
        {
            ReceivedCriticalBonus = Mathf.Max(ReceivedCriticalBonus, provider.CriticalAuraAmount);
        }
    }

    private void ResetReceivedAuraBonuses()
    {
        ReceivedAttackSpeedBonus = 0f;
        ReceivedDamageBonus = 0f;
        ReceivedMoveSpeedBonus = 0f;
        ReceivedProtectionBonus = 0f;
        ReceivedArmorBonus = 0f;
        ReceivedCriticalBonus = 0f;
    }

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
               IsProvidingProtectionAura || IsProvidingArmorAura || IsProvidingCriticalAura;
    }

    public bool IsReceivingAnyAura()
    {
        return ReceivedAttackSpeedBonus > 0 || ReceivedDamageBonus > 0 || ReceivedMoveSpeedBonus > 0 ||
               ReceivedProtectionBonus > 0 || ReceivedArmorBonus > 0 || ReceivedCriticalBonus > 0;
    }

    public void ClearAllAuras()
    {
        if (!HasStateAuthority) return;

        if (IsProvidingAttackSpeedAura) RemoveAttackSpeedAura();
        if (IsProvidingDamageAura) RemoveDamageAura();
        if (IsProvidingMoveSpeedAura) RemoveMoveSpeedAura();
        if (IsProvidingProtectionAura) RemoveProtectionAura();
        if (IsProvidingArmorAura) RemoveArmorAura();
        if (IsProvidingCriticalAura) RemoveCriticalAura();
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
    private float GetMagicalResistance()
    {
        if (equipmentManager != null)
        {
            return equipmentManager.GetTotalMagicalResistance();
        }
        return 5f; // default base resistance
    }

    private float GetPhysicalResistance()
    {
        if (equipmentManager != null)
        {
            return equipmentManager.GetTotalPhysicalResistance();
        }
        return 5f; // default base resistance
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
    }

    // ========== Status Effect Getters ==========
    public bool HasMagicalEffect()
    {
        return IsPoisoned || IsBurning || IsBleeding || IsFrozen;
    }

    public bool HasPhysicalEffect()
    {
        return IsStunned || IsArmorBreak || IsBlind || IsWeak;
    }

    public bool HasDebuff()
    {
        return IsArmorBreak || IsBlind || IsWeak || IsFrozen || IsStunned;
    }

    public bool HasDamageOverTime()
    {
        return IsPoisoned || IsBurning || IsBleeding;
    }

   
}