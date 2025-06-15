using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System;
using UnityEngine.SceneManagement;

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

public class CombatManager : NetworkBehaviour
{
    public static event Action<Character, int, DamageType, bool> OnDamageTaken;
    public static event Action<Character> OnCharacterDeath;
    public static event Action<Character, int> OnCharacterHealed;
    // ========== Component References ==========
    private Character character;
    private StatusEffectManager statusEffectManager;
    private EquipmentManager equipmentManager;

    protected virtual void Awake()
    {
        character = GetComponent<Character>();
        statusEffectManager = GetComponent<StatusEffectManager>();
        equipmentManager = GetComponent<EquipmentManager>();
    }

    protected virtual void Start()
    {
        // Subscribe to status effect damage events
        StatusEffectManager.OnStatusDamage += HandleStatusDamage;
    }

    protected virtual void OnDestroy()
    {
        // Unsubscribe events
        StatusEffectManager.OnStatusDamage -= HandleStatusDamage;
    }

    // ========== Main Damage System ==========
    public virtual void TakeDamageFromAttacker(int damage, Character attacker, DamageType damageType = DamageType.Normal)
    {
        if (!HasStateAuthority && !HasInputAuthority) return;

        // 🎯 คำนวณ critical จาก attacker's stats
        bool isCritical = false;
        int finalDamage = damage;

        if (attacker != null)
        {
            // ตรวจสอบ status effects ของ attacker (จะเพิ่มใน StatusEffectManager ต่อไป)
            finalDamage = ApplyAttackerStatusEffects(damage, attacker);

            // คำนวณ critical
            isCritical = CalculateCriticalHit(attacker);
        }

        // เรียก TakeDamage หลัก
        TakeDamage(finalDamage, damageType, isCritical);
    }

    public virtual void TakeDamage(int damage, DamageType damageType = DamageType.Normal, bool isCritical = false)
    {
        if (!HasStateAuthority && !HasInputAuthority) return;

        int finalDamage = CalculateFinalDamage(damage, isCritical);

        int oldHp = character.CurrentHp;
        character.CurrentHp -= finalDamage;
        character.CurrentHp = Mathf.Clamp(character.CurrentHp, 0, character.MaxHp);

        Debug.Log($"[TakeDamage] {character.CharacterName}: {oldHp} -> {character.CurrentHp} (damage: {finalDamage}, type: {damageType}, critical: {isCritical})");

        // Sync network state
        SyncHealthUpdate();

        // 🎯 แจ้ง damage event (จะทำให้ DamageTextManager แสดง damage text)
        OnDamageTaken?.Invoke(character, finalDamage, damageType, isCritical);

        // 🎯 แสดง damage text ทันที (สำหรับ local client)
        ShowDamageText(finalDamage, damageType, isCritical);

        // Check death
        if (character.CurrentHp <= 0)
        {
            HandleDeath();
        }
    }

    // ========== Status Effect Damage Handler ==========
    private void HandleStatusDamage(Character targetCharacter, int damage, DamageType damageType)
    {
        // เช็คว่าเป็น character ของเราหรือไม่
        if (targetCharacter == character)
        {
            // ใช้ TakeDamage แต่แสดง damage text แบบ status effect
            int oldHp = character.CurrentHp;
            character.CurrentHp -= damage;
            character.CurrentHp = Mathf.Clamp(character.CurrentHp, 0, character.MaxHp);

            Debug.Log($"[StatusDamage] {character.CharacterName}: {oldHp} -> {character.CurrentHp} (damage: {damage}, type: {damageType})");

            // Sync network state
            SyncHealthUpdate();

            // 🎯 แสดง status damage text
            ShowStatusDamageText(damage, damageType);

            // Check death
            if (character.CurrentHp <= 0)
            {
                HandleDeath();
            }
        }
    }
    private void ShowDamageText(int damage, DamageType damageType, bool isCritical)
    {
        // แสดง damage text บนหัวตัวละคร
        Vector3 textPosition = character.transform.position + Vector3.up * 2f;

        // เรียกใช้ DamageTextManager
        if (character is Hero)
        {
            DamageTextManager.ShowHeroDamage(textPosition, damage, damageType, isCritical);
        }
        else if (character is NetworkEnemy)
        {
            DamageTextManager.ShowEnemyDamage(textPosition, damage, damageType, isCritical);
        }
        else
        {
            // สำหรับ character ทั่วไป
            DamageTextManager.Instance?.ShowDamageText(textPosition, damage, damageType, isCritical, false);
        }
    }
    private void ShowStatusDamageText(int damage, DamageType damageType)
    {
        // แสดง status effect damage text
        Vector3 textPosition = character.transform.position + Vector3.up * 2.5f; // สูงกว่า normal damage เล็กน้อย

        // แปลง DamageType เป็น StatusEffectType
        StatusEffectType effectType = damageType switch
        {
            DamageType.Poison => StatusEffectType.Poison,
            DamageType.Burn => StatusEffectType.Burn,
            DamageType.Bleed => StatusEffectType.Bleed,
            _ => StatusEffectType.None
        };

        if (effectType != StatusEffectType.None)
        {
            DamageTextManager.ShowStatusDamage(textPosition, damage, effectType);
        }
    }


    // ========== Damage Calculations ==========
    private int CalculateFinalDamage(int baseDamage, bool isCritical)
    {
        int finalDamage = baseDamage;

        // Critical damage calculation
        if (isCritical)
        {
            finalDamage = Mathf.RoundToInt(baseDamage * character.CriticalMultiplier);
            Debug.Log($"[Critical Hit] {baseDamage} * {character.CriticalMultiplier} = {finalDamage}");
            return finalDamage; // Critical ignores armor
        }

        // Normal damage with armor
        int currentArmor = GetCurrentArmor();
        int damageAfterArmor = baseDamage - currentArmor;
        finalDamage = Mathf.Max(1, damageAfterArmor); // ไม่ให้ damage ต่ำกว่า 1

        Debug.Log($"[Normal Hit] {baseDamage} - {currentArmor} armor = {finalDamage}");
        return finalDamage;
    }


    private int GetCurrentArmor()
    {
        int baseArmor = character.Armor;

        // เพิ่ม armor จาก equipment
        if (equipmentManager != null)
        {
            baseArmor += equipmentManager.GetArmorBonus();
        }

        // ตรวจสอบ Armor Break effect
        if (statusEffectManager != null && statusEffectManager.IsArmorBreak)
        {
            float reduction = statusEffectManager.ArmorBreakAmount;
            baseArmor = Mathf.RoundToInt(baseArmor * (1f - reduction));
            Debug.Log($"[Armor Break] Armor reduced by {reduction * 100}%: {baseArmor}");
        }

        return baseArmor;
    }

    private bool CalculateCriticalHit(Character attacker)
    {
        float critRoll = UnityEngine.Random.Range(0f, 100f);
        float attackerCritChance = attacker.CriticalChance;

        // เพิ่ม critical chance จาก equipment
        if (attacker.GetComponent<EquipmentManager>() != null)
        {
            attackerCritChance += attacker.GetComponent<EquipmentManager>().GetCriticalChanceBonus();
        }

        // ลด critical chance ถ้าโดน Blind
        if (attacker.GetComponent<StatusEffectManager>() != null)
        {
            StatusEffectManager attackerStatus = attacker.GetComponent<StatusEffectManager>();
            if (attackerStatus.IsBlind)
            {
                float blindReduction = attackerStatus.BlindAmount;
                attackerCritChance *= (1f - blindReduction);
                Debug.Log($"[Blind Effect] Critical chance reduced by {blindReduction * 100}%");
            }
        }

        bool isCritical = critRoll < attackerCritChance;
        //Debug.Log($"[Critical Check] rolls {critRoll:F1}% vs {attackerCritChance:F1}% = {(isCritical ? "CRITICAL!" : "Normal")}");

        return isCritical;
    }
    private int ApplyAttackerStatusEffects(int damage, Character attacker)
    {
        int modifiedDamage = damage;

        // ตรวจสอบ Weakness effect ของ attacker
        if (attacker.GetComponent<StatusEffectManager>() != null)
        {
            StatusEffectManager attackerStatus = attacker.GetComponent<StatusEffectManager>();
            if (attackerStatus.IsWeak)
            {
                float weaknessReduction = attackerStatus.WeaknessAmount;
                modifiedDamage = Mathf.RoundToInt(damage * (1f - weaknessReduction));
                Debug.Log($"[Weakness Effect] Damage reduced from {damage} to {modifiedDamage} ({weaknessReduction * 100}% reduction)");
            }
        }

        return modifiedDamage;
    }

    // ========== Network Synchronization ==========
    private void SyncHealthUpdate()
    {
        if (HasStateAuthority)
        {
            character.NetworkedCurrentHp = character.CurrentHp;
            RPC_BroadcastHealthUpdate(character.CurrentHp);
        }
        else if (HasInputAuthority)
        {
            RPC_UpdateHealth(character.CurrentHp);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_UpdateHealth(int newHp)
    {
        character.CurrentHp = newHp;
        character.NetworkedCurrentHp = newHp;
        RPC_BroadcastHealthUpdate(newHp);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastHealthUpdate(int newHp)
    {
        character.NetworkedCurrentHp = newHp;
    }

    // ========== Death System ==========
    private void HandleDeath()
    {
        if (HasStateAuthority)
        {
            RPC_OnDeath();
        }
        else if (HasInputAuthority)
        {
            RPC_RequestDeath();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestDeath()
    {
        if (CanDie())
        {
            RPC_OnDeath();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_OnDeath()
    {
        Debug.Log($"{character.CharacterName} died!");
        OnCharacterDeath?.Invoke(character);
        SceneManager.LoadScene("LoseScene");

    }

    private bool CanDie()
    {
        return character.NetworkedCurrentHp <= 0;
    }

    // ========== Public Methods ==========
    public void Heal(int amount)
    {
        if (!HasStateAuthority && !HasInputAuthority) return;

        int oldHp = character.CurrentHp;
        character.CurrentHp = Mathf.Min(character.CurrentHp + amount, character.MaxHp);

        Debug.Log($"[Heal] {character.CharacterName}: {oldHp} -> {character.CurrentHp} (+{amount})");

        SyncHealthUpdate();
    }

    public float GetHealthPercentage()
    {
        return (float)character.CurrentHp / character.MaxHp;
    }
}