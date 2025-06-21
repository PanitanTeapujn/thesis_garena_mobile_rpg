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
    public int magicDamageBonus = 0;
    public int armorBonus = 0;
    public float criticalChanceBonus = 0f;
    public float criticalMultiplierBonus = 0f;
    public float reductionCoolDownBonus = 0f;

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

    // ========== Stats Application ==========
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

    // ========== Public Query Methods ==========
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

#if UNITY_EDITOR
    // ========== Context Menu for Testing ==========

    [ContextMenu("Test Equipment/Equip Iron Sword")]
    private void TestEquipIronSword()
    {
        EquipmentData ironSword = CreateTestWeapon("Iron Sword", 15, 5,0, 5f, 0f, 0f, 2f,0f);
        EquipItem(ironSword);
        LogCurrentStats();
    }

    [ContextMenu("Test Equipment/Equip Steel Sword")]
    private void TestEquipSteelSword()
    {
        EquipmentData steelSword = CreateTestWeapon("Steel Sword", 25, 10,0, 8f, 0f, 0f, 3f,1f);
        EquipItem(steelSword);
        LogCurrentStats();
    }

    [ContextMenu("Test Equipment/Equip Legendary Blade")]
    private void TestEquipLegendaryBlade()
    {
        EquipmentData legendaryBlade = CreateTestWeapon("Legendary Blade", 50,30 ,0, 15f, 0.5f, 5f, 8f,3f);
        EquipItem(legendaryBlade);
        LogCurrentStats();
    }

    [ContextMenu("Test Equipment/Equip Leather Armor")]
    private void TestEquipLeatherArmor()
    {
        EquipmentData leatherArmor = CreateTestArmor("Leather Armor", 0, 10,5 ,0f, 50, 0, 0f,0f);
        EquipItem(leatherArmor);
        LogCurrentStats();
    }

    [ContextMenu("Test Equipment/Equip Plate Armor")]
    private void TestEquipPlateArmor()
    {
        EquipmentData plateArmor = CreateTestArmor("Plate Armor", 0, 25, 15,0f, 100, 0, -1f,2f);
        EquipItem(plateArmor);
        LogCurrentStats();
    }

    [ContextMenu("Test Equipment/Equip Mage Robe")]
    private void TestEquipMageRobe()
    {
        EquipmentData mageRobe = CreateTestArmor("Mage Robe", 0,10 ,5, 0f, 0, 100, 2f,5f);
        EquipItem(mageRobe);
        LogCurrentStats();
    }

    [ContextMenu("Test Equipment/Apply Attack Rune")]
    private void TestApplyAttackRune()
    {
        EquipmentStats attackRune = new EquipmentStats
        {
            attackDamageBonus = 20,
            criticalChanceBonus = 10f,
            hitRateBonus = 5f
        };
        ApplyRuneBonus(attackRune);
        LogCurrentStats();
    }

    [ContextMenu("Test Equipment/Apply Defense Rune")]
    private void TestApplyDefenseRune()
    {
        EquipmentStats defenseRune = new EquipmentStats
        {
            armorBonus = 15,
            maxHpBonus = 200,
            physicalResistanceBonus = 10f,
            magicalResistanceBonus = 10f
        };
        ApplyRuneBonus(defenseRune);
        LogCurrentStats();
    }

    [ContextMenu("Test Equipment/Apply Speed Rune")]
    private void TestApplySpeedRune()
    {
        EquipmentStats speedRune = new EquipmentStats
        {
            moveSpeedBonus = 3f,
            attackSpeedBonus = 0.5f,
            evasionRateBonus = 8f
        };
        ApplyRuneBonus(speedRune);
        LogCurrentStats();
    }

    [ContextMenu("Test Equipment/Unequip All")]
    private void TestUnequipAll()
    {
        UnequipItem();
        // Clear rune stats
        currentRuneStats = new EquipmentStats();
        RemoveRuneStats();
        Debug.Log("=== All Equipment Removed ===");
        LogCurrentStats();
    }

    [ContextMenu("Test Equipment/Show Current Stats")]
    private void TestShowCurrentStats()
    {
        LogCurrentStats();
        LogBaseCharacterStats();
    }

    [ContextMenu("Test Equipment/Show Network Stats")]
    private void TestShowNetworkStats()
    {
        if (HasStateAuthority)
        {
            Debug.Log("=== Networked Equipment Stats ===");
            Debug.Log($"⚔️ Networked Attack Damage Bonus: {NetworkedAttackDamageBonus}");
            Debug.Log($"🛡️ Networked Armor Bonus: {NetworkedArmorBonus}");
            Debug.Log($"💥 Networked Critical Chance Bonus: {NetworkedCriticalChanceBonus:F1}%");
            Debug.Log($"❤️ Networked Max HP Bonus: {NetworkedMaxHpBonus}");
            Debug.Log($"💙 Networked Max Mana Bonus: {NetworkedMaxManaBonus}");
            Debug.Log($"🏃 Networked Move Speed Bonus: {NetworkedMoveSpeedBonus:F1}");
            Debug.Log($"🎯 Networked Hit Rate Bonus: {NetworkedHitRateBonus:F1}%");
            Debug.Log($"💨 Networked Evasion Rate Bonus: {NetworkedEvasionRateBonus:F1}%");
            Debug.Log($"⚡ Networked Attack Speed Bonus: {NetworkedAttackSpeedBonus:F1}x");
        }
        else
        {
            Debug.Log("This client doesn't have StateAuthority - cannot show networked stats");
        }
    }

    // Helper methods สำหรับสร้าง test equipment
    private EquipmentData CreateTestWeapon(string name, int attackBonus,int magicBonus ,int armorBonus, float critChance,
                                         float critMultiplier, float hitRate, float attackSpeed,float reductionCoolDown)
    {
        EquipmentData weapon = new EquipmentData
        {
            itemName = name,
            stats = new EquipmentStats
            {
                attackDamageBonus = attackBonus,
                magicDamageBonus = magicBonus,
                armorBonus = armorBonus,
                criticalChanceBonus = critChance,
                criticalMultiplierBonus = critMultiplier,
                hitRateBonus = hitRate,
                attackSpeedBonus = attackSpeed,
                reductionCoolDownBonus = reductionCoolDown
            }
        };
        return weapon;
    }

    private EquipmentData CreateTestArmor(string name, int attackBonus,int magicBonus ,int armorBonus, float critChance,
                                        int hpBonus, int manaBonus, float moveSpeed,float reductionCoolDown)
    {
        EquipmentData armor = new EquipmentData
        {
            itemName = name,
            stats = new EquipmentStats
            {
                attackDamageBonus = attackBonus,
                armorBonus = armorBonus,
                magicDamageBonus = magicBonus,
                criticalChanceBonus = critChance,
                maxHpBonus = hpBonus,
                maxManaBonus = manaBonus,
                moveSpeedBonus = moveSpeed,
                physicalResistanceBonus = armorBonus > 15 ? 5f : 2f, // Plate armor ให้ resistance มากกว่า
                magicalResistanceBonus = manaBonus > 0 ? 8f : 1f,                // Mage robe ให้ magic resistance มากกว่า
                reductionCoolDownBonus = reductionCoolDown
            }
        };
        return armor;
    }
    public float GetCriticalMultiplierBonusRaw()
    {
        float equipmentBonus = currentEquipmentStats.criticalMultiplierBonus;
        float runeBonus = currentRuneStats.criticalMultiplierBonus;
        float totalBonus = equipmentBonus + runeBonus;

        Debug.Log($"[GetCriticalMultiplierBonusRaw] {character.CharacterName}: Equipment={equipmentBonus}, Rune={runeBonus}, Total={totalBonus}");

        return totalBonus;
    }
    private void LogBaseCharacterStats()
    {
        Debug.Log($"=== {character.CharacterName} Base Character Stats ===");
        Debug.Log($"⚔️ Current Attack Damage: {character.AttackDamage}");
        Debug.Log($"🛡️ Current Armor: {character.Armor}");
        Debug.Log($"💥 Current Critical Chance: {character.CriticalChance:F1}%");
        Debug.Log($"🔥 Current Critical Multiplier: {character.CriticalDamageBonus:F1}x");
        Debug.Log($"❤️ Current Max HP: {character.MaxHp} (Current: {character.CurrentHp})");
        Debug.Log($"💙 Current Max Mana: {character.MaxMana} (Current: {character.CurrentMana})");
        Debug.Log($"🏃 Current Move Speed: {character.MoveSpeed:F1}");
        Debug.Log($"🎯 Current Hit Rate: {character.HitRate:F1}%");
        Debug.Log($"💨 Current Evasion Rate: {character.EvasionRate:F1}%");
        Debug.Log($"⚡ Current Attack Speed: {character.AttackSpeed:F1}x");

        // แสดง effective speeds ด้วย
        if (character is Hero hero)
        {
            Debug.Log($"🌟 Effective Move Speed: {hero.GetEffectiveMoveSpeed():F1}");
            Debug.Log($"🌟 Effective Attack Speed: {hero.GetEffectiveAttackSpeed():F1}");
        }
    }

    [ContextMenu("Test Equipment/Test Full Warrior Set")]
    private void TestFullWarriorSet()
    {
        Debug.Log("=== Testing Full Warrior Equipment Set ===");

        // Equip Steel Sword
        EquipmentData steelSword = CreateTestWeapon("Steel Sword", 25,10 ,0, 8f, 0f, 5f, 3f,5f);
        EquipItem(steelSword);

        // Apply Attack Rune
        EquipmentStats attackRune = new EquipmentStats
        {
            attackDamageBonus = 20,
            criticalChanceBonus = 10f,
            hitRateBonus = 5f,
            attackSpeedBonus = 0.3f
        };
        ApplyRuneBonus(attackRune);

        LogCurrentStats();
        LogBaseCharacterStats();
    }

    [ContextMenu("Test Equipment/Test Full Tank Set")]
    private void TestFullTankSet()
    {
        Debug.Log("=== Testing Full Tank Equipment Set ===");

        // Equip Plate Armor
        EquipmentData plateArmor = CreateTestArmor("Plate Armor", 0, 30,20, 0f, 150, 0, -1f,1f);
        EquipItem(plateArmor);

        // Apply Defense Rune
        EquipmentStats defenseRune = new EquipmentStats
        {
            armorBonus = 20,
            maxHpBonus = 250,
            physicalResistanceBonus = 15f,
            magicalResistanceBonus = 10f
        };
        ApplyRuneBonus(defenseRune);

        LogCurrentStats();
        LogBaseCharacterStats();
    }
#endif

#if UNITY_EDITOR
    [ContextMenu("Test Equipment/Test Critical Multiplier Equipment")]
    private void TestCriticalMultiplierEquipment()
    {
        EquipmentData testWeapon = new EquipmentData
        {
            itemName = "Critical Test Weapon",
            stats = new EquipmentStats
            {
                attackDamageBonus = 20,
                criticalChanceBonus = 15f,
                criticalMultiplierBonus = 2.0f, // ทดสอบด้วย 2.0
                hitRateBonus = 10f
            }
        };

        Debug.Log($"=== Testing Critical Multiplier Equipment ===");
        Debug.Log($"Before Equip - Critical Multiplier: {character.CriticalDamageBonus}");
        Debug.Log($"Equipment Critical Bonus: {testWeapon.stats.criticalMultiplierBonus}");

        EquipItem(testWeapon);

        Debug.Log($"After Equip - Critical Multiplier: {character.CriticalDamageBonus}");
        Debug.Log($"Expected: {character.characterStats.criticalDamageBonus + testWeapon.stats.criticalMultiplierBonus}");

        // ทดสอบการคำนวณ
       

        LogCurrentStats();
    }

    [ContextMenu("Test Equipment/Test Critical Rune")]
    private void TestCriticalRune()
    {
        EquipmentStats testRune = new EquipmentStats
        {
            criticalChanceBonus = 20f,
            criticalMultiplierBonus = 1.5f, // ทดสอบด้วย 1.5
            attackDamageBonus = 25
        };

        Debug.Log($"=== Testing Critical Multiplier Rune ===");
        Debug.Log($"Before Rune - Critical Multiplier: {character.CriticalDamageBonus}");
        Debug.Log($"Rune Critical Bonus: {testRune.criticalMultiplierBonus}");

        ApplyRuneBonus(testRune);

        Debug.Log($"After Rune - Critical Multiplier: {character.CriticalDamageBonus}");

        LogCurrentStats();
    }

    [ContextMenu("Debug/Show Critical Multiplier Debug Info")]
    private void ShowCriticalMultiplierDebug()
    {
        Debug.Log($"=== Critical Multiplier Debug Info ===");
        Debug.Log($"Character Base: {character.characterStats?.criticalDamageBonus ?? 0f}");
        Debug.Log($"Character Current: {character.CriticalDamageBonus}");
        Debug.Log($"Equipment Bonus: {currentEquipmentStats.criticalMultiplierBonus}");
        Debug.Log($"Rune Bonus: {currentRuneStats.criticalMultiplierBonus}");
        Debug.Log($"GetCriticalMultiplierBonus(): {GetCriticalMultiplierBonus()}");
        Debug.Log($"GetEffectiveCriticalMultiplier(): {character.GetEffectiveCriticalDamageBonus()}");

        // ตรวจสอบ network sync
        if (HasStateAuthority)
        {
            Debug.Log($"Network Synced Bonus: {NetworkedCriticalMultiplierBonus}");
        }
    }
#endif
}