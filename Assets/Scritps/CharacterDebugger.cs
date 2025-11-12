using UnityEngine;
using Fusion;
using System.Text;

/// <summary>
/// 🔧 Debug Tool สำหรับตรวจสอบ Stats และ Status Effects
/// ใช้งาน: คลิกขวาที่ Component ใน Inspector → เลือก Context Menu
/// </summary>
public class CharacterDebugger : NetworkBehaviour
{
    private Character character;
    private StatusEffectManager statusEffectManager;
    private EquipmentManager equipmentManager;

    private void Awake()
    {
        character = GetComponent<Character>();
        statusEffectManager = GetComponent<StatusEffectManager>();
        equipmentManager = GetComponent<EquipmentManager>();
    }

    #region Context Menu - Stats Display

    /// <summary>
    /// แสดง Stats ปัจจุบัน (รวม Buffs/Equipment)
    /// </summary>
    [ContextMenu("📊 Show Current Stats (With Buffs)")]
    public void ShowCurrentStats()
    {
        if (character == null)
        {
            Debug.LogError("❌ Character component not found!");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("═══════════════════════════════════════");
        sb.AppendLine($"📊 CURRENT STATS - {character.CharacterName}");
        sb.AppendLine("═══════════════════════════════════════");
        sb.AppendLine();

        // HP & Mana
        sb.AppendLine("💚 HP & MANA:");
        sb.AppendLine($"  HP: {character.CurrentHp}/{character.MaxHp} ({character.GetHealthPercentage() * 100f:F1}%)");
        sb.AppendLine($"  Mana: {character.CurrentMana}/{character.MaxMana}");
        sb.AppendLine();

        // Offensive Stats
        sb.AppendLine("⚔️ OFFENSIVE STATS:");
        sb.AppendLine($"  Attack Damage: {character.AttackDamage}");
        sb.AppendLine($"  Magic Damage: {character.MagicDamage}");
        sb.AppendLine($"  Critical Chance: {character.GetEffectiveCriticalChance():F1}%");
        sb.AppendLine($"  Critical Damage: {character.GetEffectiveCriticalDamageBonus() * 100f:F1}%");
        sb.AppendLine($"  Life Steal: {character.GetEffectiveLifeSteal():F1}%");
        sb.AppendLine($"  Amp Damage: {character.GetEffectiveAmpDamage():F1}%");
        sb.AppendLine();

        // Defensive Stats
        sb.AppendLine("🛡️ DEFENSIVE STATS:");
        sb.AppendLine($"  Armor: {character.GetEffectiveArmor()}");
        sb.AppendLine($"  Magic Armor: {character.GetEffectiveMagicArmor()}");
        sb.AppendLine($"  Evasion Rate: {character.EvasionRate:F1}%");
        sb.AppendLine();

        // Speed & Utility
        sb.AppendLine("⚡ SPEED & UTILITY:");
        sb.AppendLine($"  Move Speed: {character.GetEffectiveMoveSpeed():F1}");
        sb.AppendLine($"  Attack Speed: {character.GetAttackSpeedMultiplierForUI():F2}x");
        sb.AppendLine($"  Hit Rate: {character.HitRate:F1}%");
        sb.AppendLine($"  Cooldown Reduction: {character.GetEffectiveReductionCoolDown():F1}%");
        sb.AppendLine();

        // Regeneration
        sb.AppendLine("💚 REGENERATION:");
        sb.AppendLine($"  Health Regen: {character.GetTotalHealthRegenRate():F1}/s");
        sb.AppendLine($"  Mana Regen: {character.GetTotalManaRegenRate():F1}/s");
        sb.AppendLine();

        sb.AppendLine("═══════════════════════════════════════");

        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// แสดง Base Stats (ไม่รวม Buffs/Equipment)
    /// </summary>
    [ContextMenu("📈 Show Base Stats (No Buffs)")]
    public void ShowBaseStats()
    {
        if (character == null || character.characterStats == null)
        {
            Debug.LogError("❌ Character or CharacterStats not found!");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("═══════════════════════════════════════");
        sb.AppendLine($"📈 BASE STATS - {character.CharacterName}");
        sb.AppendLine("═══════════════════════════════════════");
        sb.AppendLine();

        var stats = character.characterStats;

        sb.AppendLine("💚 BASE HP & MANA:");
        sb.AppendLine($"  Max HP: {stats.maxHp}");
        sb.AppendLine($"  Max Mana: {stats.maxMana}");
        sb.AppendLine();

        sb.AppendLine("⚔️ BASE OFFENSIVE:");
        sb.AppendLine($"  Attack Damage: {stats.attackDamage}");
        sb.AppendLine($"  Magic Damage: {stats.magicDamage}");
        sb.AppendLine($"  Critical Chance: {stats.criticalChance:F1}%");
        sb.AppendLine($"  Critical Damage: {stats.criticalDamageBonus:F1}%");
        sb.AppendLine($"  Life Steal: {stats.lifeSteal:F1}%");
        sb.AppendLine($"  Amp Damage: {stats.ampdamage:F1}%");
        sb.AppendLine();

        sb.AppendLine("🛡️ BASE DEFENSIVE:");
        sb.AppendLine($"  Armor: {stats.arrmor}");
        sb.AppendLine($"  Magic Armor: {stats.magicArmor}");
        sb.AppendLine($"  Evasion Rate: {stats.evasionRate:F1}%");
        sb.AppendLine();

        sb.AppendLine("⚡ BASE SPEED:");
        sb.AppendLine($"  Move Speed: {stats.moveSpeed:F1}");
        sb.AppendLine($"  Attack Speed: {stats.attackSpeed:F1}%");
        sb.AppendLine($"  Hit Rate: {stats.hitRate:F1}%");
        sb.AppendLine($"  CDR: {stats.reductionCoolDown:F1}%");
        sb.AppendLine();

        sb.AppendLine("💚 BASE REGEN:");
        sb.AppendLine($"  Health Regen: {stats.healthRegen:F1}/s");
        sb.AppendLine($"  Mana Regen: {stats.manaRegen:F1}/s");
        sb.AppendLine();

        sb.AppendLine("═══════════════════════════════════════");

        Debug.Log(sb.ToString());
    }

    #endregion

    #region Context Menu - Status Effects Display

    /// <summary>
    /// แสดง Active Buffs ทั้งหมด
    /// </summary>
    [ContextMenu("✨ Show Active Buffs")]
    public void ShowActiveBuffs()
    {
        if (statusEffectManager == null)
        {
            Debug.LogError("❌ StatusEffectManager not found!");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("═══════════════════════════════════════");
        sb.AppendLine($"✨ ACTIVE BUFFS - {character.CharacterName}");
        sb.AppendLine("═══════════════════════════════════════");
        sb.AppendLine();

        int buffCount = 0;

        // Check Aura Buffs
        sb.AppendLine("🌟 AURA BUFFS (Providing to Team):");
        if (statusEffectManager.IsProvidingAttackSpeedAura)
        {
            sb.AppendLine($"  ⚡ Attack Speed Aura: +{statusEffectManager.AttackSpeedAuraAmount * 100f:F0}%, {statusEffectManager.AttackSpeedAuraDuration:F1}s left");
            buffCount++;
        }
        if (statusEffectManager.IsProvidingDamageAura)
        {
            sb.AppendLine($"  💥 Damage Aura: +{statusEffectManager.DamageAuraAmount * 100f:F0}%, {statusEffectManager.DamageAuraDuration:F1}s left");
            buffCount++;
        }
        if (statusEffectManager.IsProvidingMoveSpeedAura)
        {
            sb.AppendLine($"  🏃 Move Speed Aura: +{statusEffectManager.MoveSpeedAuraAmount * 100f:F0}%, {statusEffectManager.MoveSpeedAuraDuration:F1}s left");
            buffCount++;
        }
        if (statusEffectManager.IsProvidingProtectionAura)
        {
            sb.AppendLine($"  🛡️ Protection Aura: -{statusEffectManager.ProtectionAuraAmount * 100f:F0}% damage taken, {statusEffectManager.ProtectionAuraDuration:F1}s left");
            buffCount++;
        }
        if (statusEffectManager.IsProvidingArmorAura)
        {
            sb.AppendLine($"  🛡️ Armor Aura: +{statusEffectManager.ArmorAuraAmount * 100f:F0}%, {statusEffectManager.ArmorAuraDuration:F1}s left");
            buffCount++;
        }
        if (statusEffectManager.IsProvidingCriticalAura)
        {
            sb.AppendLine($"  ✨ Critical Aura: +{statusEffectManager.CriticalAuraAmount * 100f:F0}%, {statusEffectManager.CriticalAuraDuration:F1}s left");
            buffCount++;
        }
        if (statusEffectManager.IsProvidingMagicDamageAura)
        {
            sb.AppendLine($"  🔮 Magic Damage Aura: +{statusEffectManager.MagicDamageAuraAmount * 100f:F0}%, {statusEffectManager.MagicDamageAuraDuration:F1}s left");
            buffCount++;
        }
        if (statusEffectManager.IsProvidingCooldownReductionAura)
        {
            sb.AppendLine($"  ⏱️ CDR Aura: +{statusEffectManager.CooldownReductionAuraAmount * 100f:F0}%, {statusEffectManager.CooldownReductionAuraDuration:F1}s left");
            buffCount++;
        }
        if (statusEffectManager.IsProvidingLifeStealAura)
        {
            sb.AppendLine($"  ❤️ Life Steal Aura: +{statusEffectManager.LifeStealAuraAmount * 100f:F0}%, {statusEffectManager.LifeStealAuraDuration:F1}s left");
            buffCount++;
        }
        if (statusEffectManager.IsProvidingHealthRegenAura)
        {
            sb.AppendLine($"  💚 Health Regen Aura: +{statusEffectManager.HealthRegenAuraAmount * 100f:F0}%, {statusEffectManager.HealthRegenAuraDuration:F1}s left");
            buffCount++;
        }
        if (statusEffectManager.IsProvidingManaRegenAura)
        {
            sb.AppendLine($"  💙 Mana Regen Aura: +{statusEffectManager.ManaRegenAuraAmount * 100f:F0}%, {statusEffectManager.ManaRegenAuraDuration:F1}s left");
            buffCount++;
        }
        sb.AppendLine();

        // Check Received Buffs
        sb.AppendLine("📥 RECEIVED BUFFS (From Others):");
        if (statusEffectManager.ReceivedAttackSpeedBonus > 0)
        {
            sb.AppendLine($"  ⚡ Attack Speed: +{statusEffectManager.ReceivedAttackSpeedBonus * 100f:F0}%");
            buffCount++;
        }
        if (statusEffectManager.ReceivedDamageBonus > 0)
        {
            sb.AppendLine($"  💥 Damage: +{statusEffectManager.ReceivedDamageBonus * 100f:F0}%");
            buffCount++;
        }
        if (statusEffectManager.ReceivedMoveSpeedBonus > 0)
        {
            sb.AppendLine($"  🏃 Move Speed: +{statusEffectManager.ReceivedMoveSpeedBonus * 100f:F0}%");
            buffCount++;
        }
        if (statusEffectManager.ReceivedProtectionBonus > 0)
        {
            sb.AppendLine($"  🛡️ Protection: -{statusEffectManager.ReceivedProtectionBonus * 100f:F0}% damage taken");
            buffCount++;
        }
        if (statusEffectManager.ReceivedArmorBonus > 0)
        {
            sb.AppendLine($"  🛡️ Armor: +{statusEffectManager.ReceivedArmorBonus * 100f:F0}%");
            buffCount++;
        }
        if (statusEffectManager.ReceivedCriticalBonus > 0)
        {
            sb.AppendLine($"  ✨ Critical: +{statusEffectManager.ReceivedCriticalBonus * 100f:F0}%");
            buffCount++;
        }
        if (statusEffectManager.ReceivedLifeStealBonus > 0)
        {
            sb.AppendLine($"  ❤️ Life Steal: +{statusEffectManager.ReceivedLifeStealBonus * 100f:F0}%");
            buffCount++;
        }
        if (statusEffectManager.ReceivedHealthRegenBonus > 0)
        {
            sb.AppendLine($"  💚 Health Regen: +{statusEffectManager.ReceivedHealthRegenBonus * 100f:F0}%");
            buffCount++;
        }
        if (statusEffectManager.ReceivedManaRegenBonus > 0)
        {
            sb.AppendLine($"  💙 Mana Regen: +{statusEffectManager.ReceivedManaRegenBonus * 100f:F0}%");
            buffCount++;
        }
        sb.AppendLine();

        if (buffCount == 0)
        {
            sb.AppendLine("  ❌ No active buffs");
        }
        else
        {
            sb.AppendLine($"Total Active Buffs: {buffCount}");
        }

        sb.AppendLine("═══════════════════════════════════════");

        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// แสดง Active Debuffs ทั้งหมด
    /// </summary>
    [ContextMenu("💀 Show Active Debuffs")]
    public void ShowActiveDebuffs()
    {
        if (statusEffectManager == null)
        {
            Debug.LogError("❌ StatusEffectManager not found!");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("═══════════════════════════════════════");
        sb.AppendLine($"💀 ACTIVE DEBUFFS - {character.CharacterName}");
        sb.AppendLine("═══════════════════════════════════════");
        sb.AppendLine();

        int debuffCount = 0;

        // Damage Over Time
        sb.AppendLine("🩸 DAMAGE OVER TIME:");
        if (statusEffectManager.IsPoisoned)
        {
            sb.AppendLine($"  🧪 Poisoned: {statusEffectManager.PoisonDamagePerTick} dmg/tick, {statusEffectManager.PoisonDuration:F1}s left");
            debuffCount++;
        }
        if (statusEffectManager.IsBurning)
        {
            sb.AppendLine($"  🔥 Burning: {statusEffectManager.BurnDamagePerTick} dmg/tick, {statusEffectManager.BurnDuration:F1}s left");
            debuffCount++;
        }
        if (statusEffectManager.IsBleeding)
        {
            sb.AppendLine($"  🩸 Bleeding: {statusEffectManager.BleedDamagePerTick} dmg/tick, {statusEffectManager.BleedDuration:F1}s left");
            debuffCount++;
        }
        sb.AppendLine();

        // Crowd Control
        sb.AppendLine("😵 CROWD CONTROL:");
        if (statusEffectManager.IsStunned)
        {
            sb.AppendLine($"  ⚡ Stunned: {statusEffectManager.StunDuration:F1}s left");
            debuffCount++;
        }
        if (statusEffectManager.IsFrozen)
        {
            sb.AppendLine($"  ❄️ Frozen: {statusEffectManager.FreezeDuration:F1}s left");
            debuffCount++;
        }
        sb.AppendLine();

        // Stat Reduction
        sb.AppendLine("📉 STAT REDUCTION:");
        if (statusEffectManager.IsArmorBreak)
        {
            sb.AppendLine($"  💔 Armor Break: -{statusEffectManager.ArmorBreakAmount * 100f:F0}%, {statusEffectManager.ArmorBreakDuration:F1}s left");
            debuffCount++;
        }
        if (statusEffectManager.IsBlind)
        {
            sb.AppendLine($"  👁️ Blinded: -{statusEffectManager.BlindAmount * 100f:F0}% hit rate, {statusEffectManager.BlindDuration:F1}s left");
            debuffCount++;
        }
        if (statusEffectManager.IsWeak)
        {
            sb.AppendLine($"  💪 Weakened: -{statusEffectManager.WeaknessAmount * 100f:F0}% damage, {statusEffectManager.WeaknessDuration:F1}s left");
            debuffCount++;
        }
        sb.AppendLine();

        if (debuffCount == 0)
        {
            sb.AppendLine("  ✅ No active debuffs");
        }
        else
        {
            sb.AppendLine($"Total Active Debuffs: {debuffCount}");
        }

        sb.AppendLine("═══════════════════════════════════════");

        Debug.Log(sb.ToString());
    }

    #endregion

    #region Context Menu - Test Functions

    /// <summary>
    /// ทดสอบใส่ Buff
    /// </summary>
    [ContextMenu("🧪 Test Apply Buff (Damage Aura)")]
    public void TestApplyBuff()
    {
        if (statusEffectManager == null)
        {
            Debug.LogError("❌ StatusEffectManager not found!");
            return;
        }

        statusEffectManager.ApplyDamageAura(5f, 0.2f, 10f);
        Debug.Log("✅ Applied Damage Aura: +20% damage, 5m radius, 10s duration");
    }

    /// <summary>
    /// ทดสอบใส่ Debuff
    /// </summary>
    [ContextMenu("🧪 Test Apply Debuff (Poison)")]
    public void TestApplyDebuff()
    {
        if (statusEffectManager == null)
        {
            Debug.LogError("❌ StatusEffectManager not found!");
            return;
        }

        statusEffectManager.ApplyPoison(10, 5f);
        Debug.Log("✅ Applied Poison: 10 dmg/tick, 5s duration");
    }

    /// <summary>
    /// ล้าง Buffs และ Debuffs ทั้งหมด
    /// </summary>
    [ContextMenu("🧹 Clear All Buffs & Debuffs")]
    public void ClearAllEffects()
    {
        if (statusEffectManager == null)
        {
            Debug.LogError("❌ StatusEffectManager not found!");
            return;
        }

        statusEffectManager.ClearAllStatusEffects();
        statusEffectManager.ClearAllAuras();
        Debug.Log("✅ Cleared all buffs and debuffs!");
    }

    #endregion

    #region Context Menu - Comparison

    /// <summary>
    /// เปรียบเทียบ Stats (Base vs Current)
    /// </summary>
    [ContextMenu("📊 Compare Base vs Current Stats")]
    public void CompareStats()
    {
        if (character == null || character.characterStats == null)
        {
            Debug.LogError("❌ Character or CharacterStats not found!");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("═══════════════════════════════════════");
        sb.AppendLine($"📊 STATS COMPARISON - {character.CharacterName}");
        sb.AppendLine("═══════════════════════════════════════");
        sb.AppendLine();

        var stats = character.characterStats;

        // HP
        int hpDiff = character.MaxHp - stats.maxHp;
        sb.AppendLine($"💚 Max HP: {stats.maxHp} → {character.MaxHp} ({FormatDiff(hpDiff)})");

        // Attack Damage
        int atkDiff = character.AttackDamage - stats.attackDamage;
        sb.AppendLine($"⚔️ Attack Damage: {stats.attackDamage} → {character.AttackDamage} ({FormatDiff(atkDiff)})");

        // Armor
        int armorDiff = character.Armor - stats.arrmor;
        sb.AppendLine($"🛡️ Armor: {stats.arrmor} → {character.Armor} ({FormatDiff(armorDiff)})");

        // Magic Armor
        int magicArmorDiff = character.MagicArmor - stats.magicArmor;
        sb.AppendLine($"🔮 Magic Armor: {stats.magicArmor} → {character.MagicArmor} ({FormatDiff(magicArmorDiff)})");

        // Move Speed
        float speedDiff = character.GetEffectiveMoveSpeed() - stats.moveSpeed;
        sb.AppendLine($"🏃 Move Speed: {stats.moveSpeed:F1} → {character.GetEffectiveMoveSpeed():F1} ({FormatDiff(speedDiff)})");

        // Critical Chance
        float critDiff = character.GetEffectiveCriticalChance() - stats.criticalChance;
        sb.AppendLine($"✨ Critical Chance: {stats.criticalChance:F1}% → {character.GetEffectiveCriticalChance():F1}% ({FormatDiff(critDiff)}%)");

        // Life Steal
        float lsDiff = character.GetEffectiveLifeSteal() - stats.lifeSteal;
        sb.AppendLine($"❤️ Life Steal: {stats.lifeSteal:F1}% → {character.GetEffectiveLifeSteal():F1}% ({FormatDiff(lsDiff)}%)");

        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════");

        Debug.Log(sb.ToString());
    }

    private string FormatDiff(float diff)
    {
        if (diff > 0)
            return $"+{diff:F1}";
        else if (diff < 0)
            return $"{diff:F1}";
        else
            return "±0";
    }

    private string FormatDiff(int diff)
    {
        if (diff > 0)
            return $"+{diff}";
        else if (diff < 0)
            return $"{diff}";
        else
            return "±0";
    }

    #endregion
}