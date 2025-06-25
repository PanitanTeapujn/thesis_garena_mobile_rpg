using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System;

#region Data Classes
[System.Serializable]
public class StatusResistance
{
    #region  Base Resistance
    [Header("Physical Resistance (Stun, Armor Break, Blind, Weakness)")]
    [Range(0f, 80f)] public float physicalResistance = 5f;

    [Header("Magical Resistance (Poison, Burn, Bleed, Freeze)")]
    [Range(0f, 80f)] public float magicalResistance = 5f;
    #endregion Base Resistance, Equipment & Rune Bonuses, และ Calculation Methods

    #region Equipment & Rune Bonuses
    [Header("Equipment & Rune Bonuses")]
    public float equipmentPhysicalBonus = 0f;
    public float equipmentMagicalBonus = 0f;
    public float runePhysicalBonus = 0f;
    public float runeMagicalBonus = 0f;
    #endregion

    #region Calculation Methods
    public float GetTotalPhysicalResistance()
    {
        return Mathf.Clamp(physicalResistance + equipmentPhysicalBonus + runePhysicalBonus, 0f, 80f);
    }

    public float GetTotalMagicalResistance()
    {
        return Mathf.Clamp(magicalResistance + equipmentMagicalBonus + runeMagicalBonus, 0f, 80f);
    }
    #endregion
}

[System.Serializable]
public class EquipmentStats
{
    #region Combat Stats
    [Header("Combat Stats")]
    public int attackDamageBonus = 0;
    public int magicDamageBonus = 0;
    public int armorBonus = 0;
    public float criticalChanceBonus = 0f;
    public float criticalMultiplierBonus = 0f;
    public float reductionCoolDownBonus = 0f;
    #endregion

    #region Survival Stats
    [Header("Survival Stats")]
    public int maxHpBonus = 0;
    public int maxManaBonus = 0;
    public float moveSpeedBonus = 0f;
    public float attackSpeedBonus = 0f;
    public float hitRateBonus = 0f;
    public float evasionRateBonus = 0f;
    #endregion

    #region Status Resistance
    [Header("Status Resistance")]
    public float physicalResistanceBonus = 0f;
    public float magicalResistanceBonus = 0f;
    #endregion
}

[System.Serializable]
public class EquipmentData
{
    #region Equipment Properties
    public string itemName;
    public EquipmentStats stats;
    public Sprite itemIcon;
    // เพิ่ม properties อื่นๆ ตามต้องการ เช่น rarity, level requirement
    #endregion
}
#endregion

public class EquipmentManager : NetworkBehaviour
{
    #region - Event system สำหรับแจ้งเตือนการเปลี่ยนแปลง equipment และ resistance
    public static event Action<Character, EquipmentStats> OnEquipmentChanged;
    public static event Action<Character, StatusResistance> OnResistanceChanged;
    #endregion Event system สำหรับแจ้งเตือนการเปลี่ยนแปลง equipment และ resistance

    #region  การตั้งค่า base resistance และ current stats
    [Header("Equipment Settings")]
    public StatusResistance baseResistance = new StatusResistance();

    [Header("Current Equipment Stats")]
    public EquipmentStats currentEquipmentStats = new EquipmentStats();
    public EquipmentStats currentRuneStats = new EquipmentStats();
    #endregion

    #region  อ้างอิงถึง Character component
    private Character character;
    #endregion

    #region Networked properties ทั้งหมดสำหรับ Fusion networking
    [Networked] public int NetworkedAttackDamageBonus { get; set; }
    [Networked] public int NetworkedMagicDamageBonus { get; set; }
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
    [Networked] public float NetworkedReductionCoolDown { get; set; }
    [Networked] public float NetworkedCriticalMultiplierBonus { get; set; }
    #endregion

    #region Unity Lifecycle & Initialization  Awake, Start, Spawned และการเริ่มต้นระบบ
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
    #endregion

    #region Equipment Management การใส่/ถอด equipment และการใช้ runes
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
        if (character != null)
        {
            character.OnEquipmentStatsChanged(); // แจ้งให้ Inspector อัพเดท
        }
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
        if (character != null)
        {
            character.OnEquipmentStatsChanged(); // แจ้งให้ Inspector อัพเดท
        }
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
        if (character != null)
        {
            character.OnEquipmentStatsChanged(); // แจ้งให้ Inspector อัพเดท
        }
    }
    #endregion

    #region Stats Application Methods การใช้ stats, การลบ stats, และการอัปเดต stats
    private void ApplyEquipmentStats()
    {
        character.AttackDamage += currentEquipmentStats.attackDamageBonus;
        character.MagicDamage += currentEquipmentStats.magicDamageBonus;
        character.Armor += currentEquipmentStats.armorBonus;
        character.CriticalChance += currentEquipmentStats.criticalChanceBonus;

        // 🔧 สำหรับวิธีที่ 2: ไม่แตะ character.CriticalMultiplier
        // ให้ GetEffectiveCriticalMultiplier() คำนวณเอง

        character.MaxHp += currentEquipmentStats.maxHpBonus;
        character.MaxMana += currentEquipmentStats.maxManaBonus;
        character.MoveSpeed += currentEquipmentStats.moveSpeedBonus;
        character.HitRate += currentEquipmentStats.hitRateBonus;
        character.EvasionRate += currentEquipmentStats.evasionRateBonus;
        character.AttackSpeed += currentEquipmentStats.attackSpeedBonus;
        character.ReductionCoolDown += currentEquipmentStats.reductionCoolDownBonus;

        // Ensure current HP/Mana don't exceed new max values
        character.CurrentHp = Mathf.Min(character.CurrentHp, character.MaxHp);
        character.CurrentMana = Mathf.Min(character.CurrentMana, character.MaxMana);

        Debug.Log($"[Equipment Applied] {character.CharacterName} - ATK: +{currentEquipmentStats.attackDamageBonus}, ARM: +{currentEquipmentStats.armorBonus}");
        Debug.Log($"Critical Multiplier Bonus: {currentEquipmentStats.criticalMultiplierBonus} (not applied to character, will be calculated in GetEffectiveCriticalMultiplier)");
    }

    private void RemoveEquipmentStats()
    {
        character.AttackDamage -= currentEquipmentStats.attackDamageBonus;
        character.MagicDamage -= currentEquipmentStats.magicDamageBonus;
        character.Armor -= currentEquipmentStats.armorBonus;
        character.CriticalChance -= currentEquipmentStats.criticalChanceBonus;

        // 🔧 แก้ไข: ลบ critical multiplier bonus
        float critMultBonus = currentEquipmentStats.criticalMultiplierBonus;
        if (critMultBonus != 0f)
        {
            character.CriticalDamageBonus -= critMultBonus;
            Debug.Log($"[Equipment Removed] {character.CharacterName}: Critical Multiplier -{critMultBonus} (new total: {character.CriticalDamageBonus})");
        }

        character.MaxHp -= currentEquipmentStats.maxHpBonus;
        character.MaxMana -= currentEquipmentStats.maxManaBonus;
        character.MoveSpeed -= currentEquipmentStats.moveSpeedBonus;
        character.HitRate -= currentEquipmentStats.hitRateBonus;
        character.EvasionRate -= currentEquipmentStats.evasionRateBonus;
        character.AttackSpeed -= currentEquipmentStats.attackSpeedBonus;
        character.ReductionCoolDown -= currentEquipmentStats.reductionCoolDownBonus;

        // Ensure stats don't go negative
        character.AttackDamage = Mathf.Max(1, character.AttackDamage);
        character.MagicDamage = Mathf.Max(1, character.MagicDamage);
        character.Armor = Mathf.Max(0, character.Armor);
        character.MaxHp = Mathf.Max(1, character.MaxHp);
        character.MaxMana = Mathf.Max(0, character.MaxMana);
        character.MoveSpeed = Mathf.Max(0.1f, character.MoveSpeed);
        character.HitRate = Mathf.Max(5f, character.HitRate);
        character.EvasionRate = Mathf.Max(0f, character.EvasionRate);
        character.AttackSpeed = Mathf.Max(0.1f, character.AttackSpeed);
        character.ReductionCoolDown = Mathf.Max(0f, character.ReductionCoolDown);

        // 🔧 เพิ่ม: ป้องกัน critical multiplier ติดลบ
        character.CriticalDamageBonus = Mathf.Max(0f, character.CriticalDamageBonus);

        // Adjust current HP/Mana if needed
        character.CurrentHp = Mathf.Min(character.CurrentHp, character.MaxHp);
        character.CurrentMana = Mathf.Min(character.CurrentMana, character.MaxMana);
    }

    private void ApplyRuneStats()
    {
        character.AttackDamage += currentRuneStats.attackDamageBonus;
        character.MagicDamage += currentRuneStats.magicDamageBonus;
        character.Armor += currentRuneStats.armorBonus;
        character.CriticalChance += currentRuneStats.criticalChanceBonus;
        character.MaxHp += currentRuneStats.maxHpBonus;
        character.MaxMana += currentRuneStats.maxManaBonus;
        character.MoveSpeed += currentRuneStats.moveSpeedBonus;
        character.HitRate += currentRuneStats.hitRateBonus;
        character.EvasionRate += currentRuneStats.evasionRateBonus;
        character.AttackSpeed += currentRuneStats.attackSpeedBonus;
        character.ReductionCoolDown += currentRuneStats.reductionCoolDownBonus;
        Debug.Log($"[Rune Applied] {character.CharacterName} - ATK: +{currentRuneStats.attackDamageBonus}");
    }

    private void RemoveRuneStats()
    {
        character.AttackDamage -= currentRuneStats.attackDamageBonus;
        character.MagicDamage -= currentRuneStats.magicDamageBonus;
        character.Armor -= currentRuneStats.armorBonus;
        character.CriticalChance -= currentRuneStats.criticalChanceBonus;
        character.MaxHp -= currentRuneStats.maxHpBonus;
        character.MaxMana -= currentRuneStats.maxManaBonus;
        character.MoveSpeed -= currentRuneStats.moveSpeedBonus;
        character.HitRate -= currentRuneStats.hitRateBonus;
        character.EvasionRate -= currentRuneStats.evasionRateBonus;
        character.AttackSpeed -= currentRuneStats.attackSpeedBonus;
        character.ReductionCoolDown -= currentRuneStats.reductionCoolDownBonus;

        // Ensure stats don't go negative
        character.AttackDamage = Mathf.Max(1, character.AttackDamage);
        character.MagicDamage = Mathf.Max(1, character.MagicDamage);
        character.Armor = Mathf.Max(0, character.Armor);
        character.MaxHp = Mathf.Max(1, character.MaxHp);
        character.MaxMana = Mathf.Max(0, character.MaxMana);
        character.MoveSpeed = Mathf.Max(0.1f, character.MoveSpeed);
        character.HitRate = Mathf.Max(5f, character.HitRate);
        character.EvasionRate = Mathf.Max(0f, character.EvasionRate);
        character.AttackSpeed = Mathf.Max(0.1f, character.AttackSpeed);
        character.ReductionCoolDown = Mathf.Max(0f, character.ReductionCoolDown);
    }

    private void UpdateAllStats()
    {
        ApplyEquipmentStats();
        ApplyRuneStats();
        UpdateResistanceBonuses();
    }
    #endregion

    #region Resistance Management การจัดการ status resistance และการคำนวณ
    private void UpdateResistanceBonuses()
    {
        baseResistance.equipmentPhysicalBonus = currentEquipmentStats.physicalResistanceBonus;
        baseResistance.equipmentMagicalBonus = currentEquipmentStats.magicalResistanceBonus;
        baseResistance.runePhysicalBonus = currentRuneStats.physicalResistanceBonus;
        baseResistance.runeMagicalBonus = currentRuneStats.magicalResistanceBonus;

        OnResistanceChanged?.Invoke(character, baseResistance);

        Debug.Log($"[Resistance Updated] Physical: {GetTotalPhysicalResistance():F1}%, Magical: {GetTotalMagicalResistance():F1}%");
    }

    public float GetTotalPhysicalResistance()
    {
        return baseResistance.GetTotalPhysicalResistance();
    }

    public float GetTotalMagicalResistance()
    {
        return baseResistance.GetTotalMagicalResistance();
    }
    #endregion

    #region Network Synchronization  การ sync equipment stats ผ่าน network
    private void SyncEquipmentStats()
    {
        if (HasStateAuthority)
        {
            EquipmentStats totalStats = GetTotalStats();
            NetworkedAttackDamageBonus = totalStats.attackDamageBonus;
            NetworkedMagicDamageBonus = totalStats.magicDamageBonus;
            NetworkedArmorBonus = totalStats.armorBonus;
            NetworkedCriticalChanceBonus = totalStats.criticalChanceBonus;

            // 🔧 เพิ่ม: Sync Critical Multiplier Bonus
            NetworkedCriticalMultiplierBonus = totalStats.criticalMultiplierBonus;

            NetworkedMaxHpBonus = totalStats.maxHpBonus;
            NetworkedMaxManaBonus = totalStats.maxManaBonus;
            NetworkedMoveSpeedBonus = totalStats.moveSpeedBonus;
            NetworkedPhysicalResistanceBonus = totalStats.physicalResistanceBonus;
            NetworkedMagicalResistanceBonus = totalStats.magicalResistanceBonus;
            NetworkedHitRateBonus = totalStats.hitRateBonus;
            NetworkedEvasionRateBonus = totalStats.evasionRateBonus;
            NetworkedAttackSpeedBonus = totalStats.attackSpeedBonus;
            NetworkedReductionCoolDown = totalStats.reductionCoolDownBonus;

            Debug.Log($"[Equipment Sync] {character.CharacterName}: Critical Multiplier Bonus = {totalStats.criticalMultiplierBonus}");
        }
    }
    #endregion

    #region Public Query Methods  methods สำหรับดูข้อมูล stats ต่างๆ
    public int GetArmorBonus()
    {
        return currentEquipmentStats.armorBonus + currentRuneStats.armorBonus;
    }

    public int GetAttackDamageBonus()
    {
        return currentEquipmentStats.attackDamageBonus + currentRuneStats.attackDamageBonus;
    }

    public int GetMagicDamageBonus()
    {
        return currentEquipmentStats.magicDamageBonus + currentRuneStats.magicDamageBonus;
    }

    public float GetCriticalChanceBonus()
    {
        return currentEquipmentStats.criticalChanceBonus + currentRuneStats.criticalChanceBonus;
    }

    public float GetHitRateBonus()
    {
        return currentEquipmentStats.hitRateBonus + currentRuneStats.hitRateBonus;
    }

    public float GetEvasionRateBonus()
    {
        return currentEquipmentStats.evasionRateBonus + currentRuneStats.evasionRateBonus;
    }

    public float GetCriticalMultiplierBonus()
    {
        float equipmentBonus = currentEquipmentStats.criticalMultiplierBonus;
        float runeBonus = currentRuneStats.criticalMultiplierBonus;
        float totalBonus = equipmentBonus + runeBonus;

        // 🔧 Debug: แสดงรายละเอียดการคำนวณ
        if (Application.isEditor && totalBonus > 0f)
        {
            Debug.Log($"[GetCriticalMultiplierBonus] {character.CharacterName}: Equipment={equipmentBonus}, Rune={runeBonus}, Total={totalBonus}");
        }

        return totalBonus;
    }

    public float GetAttackSpeedBonus()
    {
        return currentEquipmentStats.attackSpeedBonus + currentRuneStats.attackSpeedBonus;
    }

    public float GetReductionCoolDownBonus()
    {
        return currentEquipmentStats.reductionCoolDownBonus + currentRuneStats.reductionCoolDownBonus;
    }

    public EquipmentStats GetTotalStats()
    {
        EquipmentStats total = new EquipmentStats();
        total.attackDamageBonus = currentEquipmentStats.attackDamageBonus + currentRuneStats.attackDamageBonus;
        total.magicDamageBonus = currentEquipmentStats.magicDamageBonus + currentRuneStats.magicDamageBonus;
        total.armorBonus = currentEquipmentStats.armorBonus + currentRuneStats.armorBonus;
        total.criticalChanceBonus = currentEquipmentStats.criticalChanceBonus + currentRuneStats.criticalChanceBonus;
        total.criticalMultiplierBonus = currentEquipmentStats.criticalMultiplierBonus + currentRuneStats.criticalMultiplierBonus;
        total.maxHpBonus = currentEquipmentStats.maxHpBonus + currentRuneStats.maxHpBonus;
        total.maxManaBonus = currentEquipmentStats.maxManaBonus + currentRuneStats.maxManaBonus;
        total.moveSpeedBonus = currentEquipmentStats.moveSpeedBonus + currentRuneStats.moveSpeedBonus;
        total.physicalResistanceBonus = currentEquipmentStats.physicalResistanceBonus + currentRuneStats.physicalResistanceBonus;
        total.magicalResistanceBonus = currentEquipmentStats.magicalResistanceBonus + currentRuneStats.magicalResistanceBonus;
        total.hitRateBonus = currentEquipmentStats.hitRateBonus + currentRuneStats.hitRateBonus;
        total.evasionRateBonus = currentEquipmentStats.evasionRateBonus + currentRuneStats.evasionRateBonus;
        total.attackSpeedBonus = currentEquipmentStats.attackSpeedBonus + currentRuneStats.attackSpeedBonus;
        total.reductionCoolDownBonus = currentEquipmentStats.reductionCoolDownBonus + currentRuneStats.reductionCoolDownBonus;
        return total;
    }
    #endregion

#if UNITY_EDITOR
    #region Debug Methods (Editor Only)  debug methods สำหรับใช้ใน editor เท่านั้น
    public float GetCriticalMultiplierBonusRaw()
    {
        float equipmentBonus = currentEquipmentStats.criticalMultiplierBonus;
        float runeBonus = currentRuneStats.criticalMultiplierBonus;
        float totalBonus = equipmentBonus + runeBonus;

        Debug.Log($"[GetCriticalMultiplierBonusRaw] {character.CharacterName}: Equipment={equipmentBonus}, Rune={runeBonus}, Total={totalBonus}");

        return totalBonus;
    }
    #endregion
#endif
    #region Context Menu สำหรับทดสอบ Equipment (เพิ่มเข้าไปในคลาส EquipmentManager)

    [ContextMenu("Test: Equip Iron Sword")]
    private void TestEquipIronSword()
    {
        EquipmentData ironSword = CreateTestWeapon(
            "Iron Sword",
            attackDamageBonus: 25,
            criticalChanceBonus: 5f,
            criticalMultiplierBonus: 15f
        );

        EquipItem(ironSword);
        Debug.Log("🗡️ Equipped Iron Sword: +25 ATK, +5% Crit, +15% Crit Dmg");
    }

    [ContextMenu("Test: Equip Magic Staff")]
    private void TestEquipMagicStaff()
    {
        EquipmentData magicStaff = CreateTestWeapon(
            "Magic Staff",
            magicDamageBonus: 40,
            maxManaBonus: 50,
            reductionCoolDownBonus: 10f
        );

        EquipItem(magicStaff);
        Debug.Log("🪄 Equipped Magic Staff: +40 MAG, +50 MP, +10% CDR");
    }

    [ContextMenu("Test: Equip Heavy Armor")]
    private void TestEquipHeavyArmor()
    {
        EquipmentData heavyArmor = CreateTestArmor(
            "Heavy Armor",
            armorBonus: 35,
            maxHpBonus: 100,
            physicalResistanceBonus: 15f
        );

        EquipItem(heavyArmor);
        Debug.Log("🛡️ Equipped Heavy Armor: +35 ARM, +100 HP, +15% Phys Resist");
    }

    [ContextMenu("Test: Equip Speed Boots")]
    private void TestEquipSpeedBoots()
    {
        EquipmentData speedBoots = CreateTestAccessory(
            "Speed Boots",
            moveSpeedBonus: 2f,
            attackSpeedBonus: 0.3f,
            evasionRateBonus: 10f
        );

        EquipItem(speedBoots);
        Debug.Log("👟 Equipped Speed Boots: +2 SPD, +0.3 AS, +10% EVA");
    }

    [ContextMenu("Test: Apply Power Rune")]
    private void TestApplyPowerRune()
    {
        EquipmentStats powerRune = new EquipmentStats
        {
            attackDamageBonus = 15,
            magicDamageBonus = 15,
            criticalMultiplierBonus = 10f,
            reductionCoolDownBonus = 5f
        };

        ApplyRuneBonus(powerRune);
        Debug.Log("💎 Applied Power Rune: +15 ATK, +15 MAG, +10% Crit Dmg, +5% CDR");
    }

    [ContextMenu("Test: Show Current Stats")]
    private void TestShowCurrentStats()
    {
        if (character == null)
        {
            Debug.LogError("❌ Character reference not found!");
            return;
        }

        Debug.Log("=== CURRENT CHARACTER STATS ===");
        Debug.Log($"📛 Name: {character.CharacterName}");
        Debug.Log($"❤️ HP: {character.CurrentHp}/{character.MaxHp}");
        Debug.Log($"💙 MP: {character.CurrentMana}/{character.MaxMana}");
        Debug.Log($"⚔️ ATK: {character.AttackDamage}");
        Debug.Log($"🪄 MAG: {character.MagicDamage}");
        Debug.Log($"🛡️ ARM: {character.Armor}");
        Debug.Log($"💨 SPD: {character.GetEffectiveMoveSpeed():F1}");
        Debug.Log($"💥 CRIT: {character.CriticalChance:F1}%");
        Debug.Log($"🔥 CRIT DMG: {character.GetEffectiveCriticalDamageBonus():F1}%");
        Debug.Log($"🎯 HIT: {character.HitRate:F1}%");
        Debug.Log($"🌪️ EVA: {character.EvasionRate:F1}%");
        Debug.Log($"⚡ AS: {character.GetEffectiveAttackSpeed():F2}");
        Debug.Log($"⏰ CDR: {character.GetEffectiveReductionCoolDown():F1}%");
        Debug.Log("===============================");
    }

    [ContextMenu("Test: Show Equipment Bonuses")]
    private void TestShowEquipmentBonuses()
    {
        EquipmentStats totalStats = GetTotalStats();

        Debug.Log("=== EQUIPMENT BONUSES ===");
        Debug.Log($"⚔️ ATK Bonus: +{totalStats.attackDamageBonus}");
        Debug.Log($"🪄 MAG Bonus: +{totalStats.magicDamageBonus}");
        Debug.Log($"🛡️ ARM Bonus: +{totalStats.armorBonus}");
        Debug.Log($"❤️ HP Bonus: +{totalStats.maxHpBonus}");
        Debug.Log($"💙 MP Bonus: +{totalStats.maxManaBonus}");
        Debug.Log($"💨 SPD Bonus: +{totalStats.moveSpeedBonus:F1}");
        Debug.Log($"💥 CRIT Bonus: +{totalStats.criticalChanceBonus:F1}%");
        Debug.Log($"🔥 CRIT DMG Bonus: +{totalStats.criticalMultiplierBonus:F1}%");
        Debug.Log($"🎯 HIT Bonus: +{totalStats.hitRateBonus:F1}%");
        Debug.Log($"🌪️ EVA Bonus: +{totalStats.evasionRateBonus:F1}%");
        Debug.Log($"⚡ AS Bonus: +{totalStats.attackSpeedBonus:F2}");
        Debug.Log($"⏰ CDR Bonus: +{totalStats.reductionCoolDownBonus:F1}%");
        Debug.Log("========================");
    }

    [ContextMenu("Test: Remove All Equipment")]
    private void TestRemoveAllEquipment()
    {
        UnequipItem();

        // ลบ Rune ด้วย
        EquipmentStats emptyRune = new EquipmentStats();
        ApplyRuneBonus(emptyRune);

        Debug.Log("🗑️ Removed all equipment and runes");
    }

    [ContextMenu("Test: Resistance Check")]
    private void TestResistanceCheck()
    {
        Debug.Log("=== STATUS RESISTANCE ===");
        Debug.Log($"🛡️ Physical Resistance: {GetTotalPhysicalResistance():F1}%");
        Debug.Log($"✨ Magical Resistance: {GetTotalMagicalResistance():F1}%");
        Debug.Log("========================");
    }

    // Helper Methods สำหรับสร้างไอเทมทดสอบ
    private EquipmentData CreateTestWeapon(string name, int attackDamageBonus = 0, int magicDamageBonus = 0,
        float criticalChanceBonus = 0f, float criticalMultiplierBonus = 0f, int maxManaBonus = 0, float reductionCoolDownBonus = 0f)
    {
        EquipmentData weapon = new EquipmentData
        {
            itemName = name,
            stats = new EquipmentStats
            {
                attackDamageBonus = attackDamageBonus,
                magicDamageBonus = magicDamageBonus,
                criticalChanceBonus = criticalChanceBonus,
                criticalMultiplierBonus = criticalMultiplierBonus,
                maxManaBonus = maxManaBonus,
                reductionCoolDownBonus = reductionCoolDownBonus
            }
        };
        return weapon;
    }

    private EquipmentData CreateTestArmor(string name, int armorBonus = 0, int maxHpBonus = 0,
        float physicalResistanceBonus = 0f, float magicalResistanceBonus = 0f)
    {
        EquipmentData armor = new EquipmentData
        {
            itemName = name,
            stats = new EquipmentStats
            {
                armorBonus = armorBonus,
                maxHpBonus = maxHpBonus,
                physicalResistanceBonus = physicalResistanceBonus,
                magicalResistanceBonus = magicalResistanceBonus
            }
        };
        return armor;
    }

    private EquipmentData CreateTestAccessory(string name, float moveSpeedBonus = 0f, float attackSpeedBonus = 0f,
        float evasionRateBonus = 0f, float hitRateBonus = 0f)
    {
        EquipmentData accessory = new EquipmentData
        {
            itemName = name,
            stats = new EquipmentStats
            {
                moveSpeedBonus = moveSpeedBonus,
                attackSpeedBonus = attackSpeedBonus,
                evasionRateBonus = evasionRateBonus,
                hitRateBonus = hitRateBonus
            }
        };
        return accessory;
    }

    #endregion
}