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
    Weakness
}

public class StatusEffectManager : NetworkBehaviour
{
    [Header("Status Effect Settings")]
    private float poisonTickInterval = 1f;
    private float burnTickInterval = 0.5f;   // ทุก 0.5 วินาที
    private float bleedTickInterval = 0.7f;  // ทุก 0.7 วินาที

    // ========== Network Status Properties ==========

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
        }
    }

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