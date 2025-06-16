using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System;

[System.Serializable]
public class StatusResistance
{
    [Header("Physical Resistance (Stun, Armor Break, Blind, Weakness)")]
    [Range(0f, 80f)] public float physicalResistance = 5f;

    [Header("Magical Resistance (Poison, Burn, Bleed, Freeze)")]
    [Range(0f, 80f)] public float magicalResistance = 5f;

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

[System.Serializable]
public class EquipmentStats
{
    [Header("Combat Stats")]
    public int attackDamageBonus = 0;
    public int armorBonus = 0;
    public float criticalChanceBonus = 0f;
    public float criticalMultiplierBonus = 0f;

    [Header("Survival Stats")]
    public int maxHpBonus = 0;
    public int maxManaBonus = 0;
    public float moveSpeedBonus = 0f;
    public float attackSpeedBonus = 0f;
    public float hitRateBonus = 0f;
    public float evasionRateBonus = 0f;
    [Header("Status Resistance")]
    public float physicalResistanceBonus = 0f;
    public float magicalResistanceBonus = 0f;
}

[System.Serializable]
public class EquipmentData
{
    public string itemName;
    public EquipmentStats stats;
    public Sprite itemIcon;
    // เพิ่ม properties อื่นๆ ตามต้องการ เช่น rarity, level requirement
}

public class EquipmentManager : NetworkBehaviour
{
    [Header("Equipment Settings")]
    public StatusResistance baseResistance = new StatusResistance();

    [Header("Current Equipment Stats")]
    public EquipmentStats currentEquipmentStats = new EquipmentStats();
    public EquipmentStats currentRuneStats = new EquipmentStats();

    public static event Action<Character, EquipmentStats> OnEquipmentChanged;
    public static event Action<Character, StatusResistance> OnResistanceChanged;

    // ========== Component References ==========
    private Character character;

    // ========== Network Properties สำหรับ Equipment Stats ==========
    [Networked] public int NetworkedAttackDamageBonus { get; set; }
    [Networked] public int NetworkedArmorBonus { get; set; }
    [Networked] public float NetworkedCriticalChanceBonus { get; set; }
    [Networked] public int NetworkedMaxHpBonus { get; set; }
    [Networked] public int NetworkedMaxManaBonus { get; set; }
    [Networked] public float NetworkedMoveSpeedBonus { get; set; }
    [Networked] public float NetworkedPhysicalResistanceBonus { get; set; }
    [Networked] public float NetworkedMagicalResistanceBonus { get; set; }
    [Networked] public float NetworkedHitRateBonus { get; set; }
    [Networked] public float NetworkedEvasionRateBonus { get; set; }
    [Networked] public float NetworkedAttackSpeedBonus { get; set; }
    protected virtual void Awake()
    {
        character = GetComponent<Character>();
    }

    protected virtual void Start()
    {
        // Initialize base resistance
        baseResistance.physicalResistance = 5f;
        baseResistance.magicalResistance = 5f;

        // Apply initial stats
        UpdateAllStats();
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            SyncEquipmentStats();
        }
    }

    // ========== Equipment Methods ==========
    public virtual void EquipItem(EquipmentData equipment)
    {
        if (!HasStateAuthority && !HasInputAuthority) return;

        Debug.Log($"[Equipment] {character.CharacterName} equipped {equipment.itemName}");

        // Remove old equipment stats (if any)
        RemoveEquipmentStats();

        // Apply new equipment stats
        currentEquipmentStats = equipment.stats;
        ApplyEquipmentStats();

        // Update resistance bonuses
        UpdateResistanceBonuses();

        // Sync to network
        SyncEquipmentStats();

        // Notify other systems
        OnEquipmentChanged?.Invoke(character, GetTotalStats());
    }

    public virtual void UnequipItem()
    {
        if (!HasStateAuthority && !HasInputAuthority) return;

        Debug.Log($"[Equipment] {character.CharacterName} unequipped item");

        RemoveEquipmentStats();
        currentEquipmentStats = new EquipmentStats();
        UpdateResistanceBonuses();
        SyncEquipmentStats();

        OnEquipmentChanged?.Invoke(character, GetTotalStats());
    }

    public virtual void ApplyRuneBonus(EquipmentStats runeStats)
    {
        if (!HasStateAuthority && !HasInputAuthority) return;

        Debug.Log($"[Rune] {character.CharacterName} applied rune bonus");

        RemoveRuneStats();
        currentRuneStats = runeStats;
        ApplyRuneStats();
        UpdateResistanceBonuses();
        SyncEquipmentStats();

        OnEquipmentChanged?.Invoke(character, GetTotalStats());
    }

    // ========== Stats Application ==========
    private void ApplyEquipmentStats()
    {
        character.AttackDamage += currentEquipmentStats.attackDamageBonus;
        character.Armor += currentEquipmentStats.armorBonus;
        character.CriticalChance += currentEquipmentStats.criticalChanceBonus;
        character.CriticalMultiplier += currentEquipmentStats.criticalMultiplierBonus;

        character.MaxHp += currentEquipmentStats.maxHpBonus;
        character.MaxMana += currentEquipmentStats.maxManaBonus;
        character.MoveSpeed += currentEquipmentStats.moveSpeedBonus;
        character.HitRate += currentEquipmentStats.hitRateBonus;
        character.EvasionRate += currentEquipmentStats.evasionRateBonus;
        character.AttackSpeed += currentEquipmentStats.attackSpeedBonus;

        // Ensure current HP/Mana don't exceed new max values
        character.CurrentHp = Mathf.Min(character.CurrentHp, character.MaxHp);
        character.CurrentMana = Mathf.Min(character.CurrentMana, character.MaxMana);

        Debug.Log($"[Equipment Applied] {character.CharacterName} - ATK: +{currentEquipmentStats.attackDamageBonus}, ARM: +{currentEquipmentStats.armorBonus}");
    }

    private void RemoveEquipmentStats()
    {
        character.AttackDamage -= currentEquipmentStats.attackDamageBonus;
        character.Armor -= currentEquipmentStats.armorBonus;
        character.CriticalChance -= currentEquipmentStats.criticalChanceBonus;
        character.CriticalMultiplier -= currentEquipmentStats.criticalMultiplierBonus;

        character.MaxHp -= currentEquipmentStats.maxHpBonus;
        character.MaxMana -= currentEquipmentStats.maxManaBonus;
        character.MoveSpeed -= currentEquipmentStats.moveSpeedBonus;
        character.HitRate -= currentEquipmentStats.hitRateBonus;
        character.EvasionRate -= currentEquipmentStats.evasionRateBonus;
        character.AttackSpeed -= currentEquipmentStats.attackSpeedBonus;
        // Ensure stats don't go negative
        character.AttackDamage = Mathf.Max(1, character.AttackDamage);
        character.Armor = Mathf.Max(0, character.Armor);
        character.MaxHp = Mathf.Max(1, character.MaxHp);
        character.MaxMana = Mathf.Max(0, character.MaxMana);
        character.MoveSpeed = Mathf.Max(0.1f, character.MoveSpeed);
        character.HitRate = Mathf.Max(5f, character.HitRate);           // ต่ำสุด 5%
        character.EvasionRate = Mathf.Max(0f, character.EvasionRate);   // ต่ำสุด 0%
        character.AttackSpeed = Mathf.Max(0.1f, character.AttackSpeed); // ต่ำสุด 0.1x
        // Adjust current HP/Mana if needed
        character.CurrentHp = Mathf.Min(character.CurrentHp, character.MaxHp);
        character.CurrentMana = Mathf.Min(character.CurrentMana, character.MaxMana);
    }

    private void ApplyRuneStats()
    {
        character.AttackDamage += currentRuneStats.attackDamageBonus;
        character.Armor += currentRuneStats.armorBonus;
        character.CriticalChance += currentRuneStats.criticalChanceBonus;
        character.MaxHp += currentRuneStats.maxHpBonus;
        character.MaxMana += currentRuneStats.maxManaBonus;
        character.MoveSpeed += currentRuneStats.moveSpeedBonus;
        character.HitRate += currentRuneStats.hitRateBonus;
        character.EvasionRate += currentRuneStats.evasionRateBonus;
        character.AttackSpeed += currentRuneStats.attackSpeedBonus;
        Debug.Log($"[Rune Applied] {character.CharacterName} - ATK: +{currentRuneStats.attackDamageBonus}");
    }

    private void RemoveRuneStats()
    {
        character.AttackDamage -= currentRuneStats.attackDamageBonus;
        character.Armor -= currentRuneStats.armorBonus;
        character.CriticalChance -= currentRuneStats.criticalChanceBonus;
        character.MaxHp -= currentRuneStats.maxHpBonus;
        character.MaxMana -= currentRuneStats.maxManaBonus;
        character.MoveSpeed -= currentRuneStats.moveSpeedBonus;
        character.HitRate -= currentRuneStats.hitRateBonus;
        character.EvasionRate -= currentRuneStats.evasionRateBonus;
        character.AttackSpeed -= currentRuneStats.attackSpeedBonus;

        // Ensure stats don't go negative
        character.AttackDamage = Mathf.Max(1, character.AttackDamage);
        character.Armor = Mathf.Max(0, character.Armor);
        character.MaxHp = Mathf.Max(1, character.MaxHp);
        character.MaxMana = Mathf.Max(0, character.MaxMana);
        character.MoveSpeed = Mathf.Max(0.1f, character.MoveSpeed);
        character.HitRate = Mathf.Max(5f, character.HitRate);
        character.EvasionRate = Mathf.Max(0f, character.EvasionRate);
        character.AttackSpeed = Mathf.Max(0.1f, character.AttackSpeed);

    }

    private void UpdateAllStats()
    {
        ApplyEquipmentStats();
        ApplyRuneStats();
        UpdateResistanceBonuses();
    }

    // ========== Resistance Management ==========
    private void UpdateResistanceBonuses()
    {
        baseResistance.equipmentPhysicalBonus = currentEquipmentStats.physicalResistanceBonus;
        baseResistance.equipmentMagicalBonus = currentEquipmentStats.magicalResistanceBonus;
        baseResistance.runePhysicalBonus = currentRuneStats.physicalResistanceBonus;
        baseResistance.runeMagicalBonus = currentRuneStats.magicalResistanceBonus;

        OnResistanceChanged?.Invoke(character, baseResistance);

        Debug.Log($"[Resistance Updated] Physical: {GetTotalPhysicalResistance():F1}%, Magical: {GetTotalMagicalResistance():F1}%");
    }

    // ========== Network Synchronization ==========
    private void SyncEquipmentStats()
    {
        if (HasStateAuthority)
        {
            EquipmentStats totalStats = GetTotalStats();
            NetworkedAttackDamageBonus = totalStats.attackDamageBonus;
            NetworkedArmorBonus = totalStats.armorBonus;
            NetworkedCriticalChanceBonus = totalStats.criticalChanceBonus;
            NetworkedMaxHpBonus = totalStats.maxHpBonus;
            NetworkedMaxManaBonus = totalStats.maxManaBonus;
            NetworkedMoveSpeedBonus = totalStats.moveSpeedBonus;
            NetworkedPhysicalResistanceBonus = totalStats.physicalResistanceBonus;
            NetworkedMagicalResistanceBonus = totalStats.magicalResistanceBonus;
            NetworkedHitRateBonus = totalStats.hitRateBonus;
            NetworkedEvasionRateBonus = totalStats.evasionRateBonus;
            NetworkedAttackSpeedBonus = totalStats.attackSpeedBonus;

        }
    }

    // ========== Public Query Methods ==========
    public int GetArmorBonus()
    {
        return currentEquipmentStats.armorBonus + currentRuneStats.armorBonus;
    }

    public int GetAttackDamageBonus()
    {
        return currentEquipmentStats.attackDamageBonus + currentRuneStats.attackDamageBonus;
    }

    public float GetCriticalChanceBonus()
    {
        return currentEquipmentStats.criticalChanceBonus + currentRuneStats.criticalChanceBonus;
    }

    public float GetTotalPhysicalResistance()
    {
        return baseResistance.GetTotalPhysicalResistance();
    }

    public float GetTotalMagicalResistance()
    {
        return baseResistance.GetTotalMagicalResistance();
    }

    public float GetHitRateBonus()
    {
        return currentEquipmentStats.hitRateBonus + currentRuneStats.hitRateBonus;
    }

    public float GetEvasionRateBonus()
    {
        return currentEquipmentStats.evasionRateBonus + currentRuneStats.evasionRateBonus;
    }

    public float GetAttackSpeedBonus()
    {
        return currentEquipmentStats.attackSpeedBonus + currentRuneStats.attackSpeedBonus;
    }
    public EquipmentStats GetTotalStats()
    {
        EquipmentStats total = new EquipmentStats();
        total.attackDamageBonus = currentEquipmentStats.attackDamageBonus + currentRuneStats.attackDamageBonus;
        total.armorBonus = currentEquipmentStats.armorBonus + currentRuneStats.armorBonus;
        total.criticalChanceBonus = currentEquipmentStats.criticalChanceBonus + currentRuneStats.criticalChanceBonus;
        total.maxHpBonus = currentEquipmentStats.maxHpBonus + currentRuneStats.maxHpBonus;
        total.maxManaBonus = currentEquipmentStats.maxManaBonus + currentRuneStats.maxManaBonus;
        total.moveSpeedBonus = currentEquipmentStats.moveSpeedBonus + currentRuneStats.moveSpeedBonus;
        total.physicalResistanceBonus = currentEquipmentStats.physicalResistanceBonus + currentRuneStats.physicalResistanceBonus;
        total.magicalResistanceBonus = currentEquipmentStats.magicalResistanceBonus + currentRuneStats.magicalResistanceBonus;
        total.hitRateBonus = currentEquipmentStats.hitRateBonus + currentRuneStats.hitRateBonus;
        total.evasionRateBonus = currentEquipmentStats.evasionRateBonus + currentRuneStats.evasionRateBonus;
        total.attackSpeedBonus = currentEquipmentStats.attackSpeedBonus + currentRuneStats.attackSpeedBonus;
        return total;
    }

    // ========== Debug Methods ==========
    public void LogCurrentStats()
    {
        EquipmentStats total = GetTotalStats();
        Debug.Log($"=== {character.CharacterName} Equipment Stats ===");
        Debug.Log($"⚔️ Attack Damage: +{total.attackDamageBonus}");
        Debug.Log($"🛡️ Armor: +{total.armorBonus}");
        Debug.Log($"💥 Critical Chance: +{total.criticalChanceBonus:F1}%");
        Debug.Log($"❤️ Max HP: +{total.maxHpBonus}");
        Debug.Log($"💙 Max Mana: +{total.maxManaBonus}");
        Debug.Log($"🏃 Move Speed: +{total.moveSpeedBonus:F1}");
        Debug.Log($"🛡️ Physical Resistance: {GetTotalPhysicalResistance():F1}%");
        Debug.Log($"🔮 Magical Resistance: {GetTotalMagicalResistance():F1}%");
        Debug.Log($"🎯 Hit Rate: +{total.hitRateBonus:F1}%");
        Debug.Log($"💨 Evasion Rate: +{total.evasionRateBonus:F1}%");
        Debug.Log($"⚡ Attack Speed: +{total.attackSpeedBonus:F1}x");

    }
}