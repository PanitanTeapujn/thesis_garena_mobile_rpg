using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System;
using UnityEngine.SceneManagement;

#region Enums & Events - การกำหนดประเภทดาเมจและ Event System สำหรับแจ้งเตือนเหตุการณ์ต่างๆ
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
    // Combat Events
    public System.Action<Character, int, DamageType, bool> OnLocalDamageTaken;

    public static event Action<Character, int, DamageType, bool> OnDamageTaken;
    public static event Action<Character> OnCharacterDeath;
    public static event Action<Character, int> OnCharacterHealed;
    #endregion

    #region Component References - การอ้างอิงถึง Component และ Manager อื่นๆ ที่จำเป็นสำหรับระบบต่อสู้
    private Character character;
    private StatusEffectManager statusEffectManager;
    private EquipmentManager equipmentManager;
    #endregion

    #region Unity Lifecycle & Initialization - การเริ่มต้นและจัดการ Component เมื่อระบบทำงาน
    protected virtual void Awake()
    {
        character = GetComponent<Character>();
        statusEffectManager = GetComponent<StatusEffectManager>();
        equipmentManager = GetComponent<EquipmentManager>();
    }

    protected virtual void Start()
    {
        StatusEffectManager.OnStatusDamage += HandleStatusDamage;
    }

    protected virtual void OnDestroy()
    {
        StatusEffectManager.OnStatusDamage -= HandleStatusDamage;
    }
    #endregion

    #region Network RPC Methods - การส่งข้อมูลผ่าน Network สำหรับแสดง Damage Text และ Effect
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowDamageText(Vector3 position, int damage, DamageType damageType, bool isCritical, bool isHeal = false, bool isMiss = false)
    {
        if (isMiss)
        {
            DamageTextManager.ShowMissText(position);
            return;
        }

        if (character is Hero)
        {
            if (isHeal)
                DamageTextManager.ShowHealing(position, damage);
            else
                DamageTextManager.ShowHeroDamage(position, damage, damageType, isCritical);
        }
        else if (character is NetworkEnemy)
        {
            if (isHeal)
                DamageTextManager.ShowHealing(position, damage);
            else
                DamageTextManager.ShowEnemyDamage(position, damage, damageType, isCritical);
        }
        else
        {
            DamageTextManager.Instance?.ShowDamageText(position, damage, damageType, isCritical, isHeal);
        }
    }
    public virtual void TakeDamageFromDummyAttack(int physicalDamage, int magicDamage, Character attacker)
    {
        if (!HasStateAuthority && !HasInputAuthority) return;

        // Dummy ไม่มี armor - รับดาเมจเต็ม
        int totalDamage = physicalDamage + magicDamage;

        Debug.Log($"[Dummy Attack] {attacker?.CharacterName ?? "Unknown"} -> {character.CharacterName}: {totalDamage}");

        // Apply damage
        int oldHp = character.CurrentHp;
        character.CurrentHp -= totalDamage;
        character.CurrentHp = Mathf.Clamp(character.CurrentHp, 0, character.MaxHp);

        // Sync network state
        SyncHealthUpdate();

        // แสดง Damage Text
        if (HasStateAuthority)
        {
            Vector3 textPosition = character.transform.position + Vector3.up * 2f;
            RPC_ShowDamageText(textPosition, totalDamage, DamageType.Normal, false, false, false);
        }

        OnDamageTaken?.Invoke(character, totalDamage, DamageType.Normal, false);

        // Check death
        if (character.CurrentHp <= 0)
        {
            HandleDeath();
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowStatusDamageText(Vector3 position, int damage, StatusEffectType effectType)
    {
        DamageTextManager.ShowStatusDamage(position, damage, effectType);
    }
    #endregion

    #region Hit Calculation & Combat Mechanics - การคำนวณความแม่นยำในการโจมตีและ Attack Speed
    private bool CalculateHitSuccess(Character attacker, Character target)
    {
        float attackerHitRate = attacker.HitRate;
        float targetEvasion = target.EvasionRate;

        if (attacker.GetComponent<EquipmentManager>() != null)
        {
            attackerHitRate += attacker.GetComponent<EquipmentManager>().GetHitRateBonus();
        }

        if (target.GetComponent<EquipmentManager>() != null)
        {
            targetEvasion += target.GetComponent<EquipmentManager>().GetEvasionRateBonus();
        }

        if (attacker.GetComponent<StatusEffectManager>() != null)
        {
            StatusEffectManager attackerStatus = attacker.GetComponent<StatusEffectManager>();
            if (attackerStatus.IsBlind)
            {
                float blindReduction = attackerStatus.BlindAmount;
                attackerHitRate *= (1f - blindReduction);
                Debug.Log($"[Blind Effect] Hit rate reduced by {blindReduction * 100}%");
            }
        }

        float finalHitChance = attackerHitRate - targetEvasion;
        finalHitChance = Mathf.Clamp(finalHitChance, 5f, 95f);

        float roll = UnityEngine.Random.Range(0f, 100f);
        bool isHit = roll < finalHitChance;

        return isHit;
    }

    private float CalculateAttackCooldownWithSpeed(Character attacker)
    {
        float baseAttackCooldown = attacker.AttackCooldown;

        // ✅ ใช้ระบบใหม่: cooldown reduction
        float cooldownReduction = attacker.GetEffectiveAttackSpeed(); // ได้ค่า 0-0.9
        float finalCooldown = baseAttackCooldown * (1f - cooldownReduction);

        Debug.Log($"[Attack Cooldown] {attacker.CharacterName}: Base={baseAttackCooldown}s, Reduction={cooldownReduction * 100f}%, Final={finalCooldown:F2}s");

        return finalCooldown;
    }
    #endregion

    #region Main Damage System - ระบบรับดาเมจหลักที่รองรับทั้ง Physical และ Magic Damage
    public virtual void TakeDamageFromAttacker(int physicalDamage, int magicDamage, Character attacker, DamageType damageType = DamageType.Normal, bool isBasicAttack = false)
    {
        if (!HasStateAuthority && !HasInputAuthority) return;

        // เช็ค Hit/Miss ก่อน
        if (!CalculateHitSuccess(attacker, character))
        {
            if (HasStateAuthority)
            {
                Vector3 textPosition = character.transform.position + Vector3.up * 2f;
                RPC_ShowDamageText(textPosition, 0, DamageType.Normal, false, false, true);
            }
            Debug.Log($"[MISS] {attacker.CharacterName} missed {character.CharacterName}!");
            return;
        }

        // คำนวณ critical และ apply status effects
        bool isCritical = false;
        if (attacker != null)
        {
            physicalDamage = ApplyAttackerStatusEffects(physicalDamage, attacker);
            magicDamage = ApplyAttackerStatusEffects(magicDamage, attacker);
            isCritical = CalculateCriticalHit(attacker);
        }

        // ✅ FIXED: Apply Amp Damage เฉพาะ skill attacks เท่านั้น
        if (!isBasicAttack && attacker != null)
        {
            // Apply Amp Damage ให้กับ skill damage
            int originalTotal = physicalDamage + magicDamage;

            // คำนวณ Amp Damage
            int ampedPhysical = attacker.ApplyAmpDamageToSkill(physicalDamage);
            int ampedMagic = attacker.ApplyAmpDamageToSkill(magicDamage);

            physicalDamage = ampedPhysical;
            magicDamage = ampedMagic;

            int newTotal = physicalDamage + magicDamage;
            Debug.Log($"[Skill Amp Damage] {attacker.CharacterName}: {originalTotal} → {newTotal} (+{newTotal - originalTotal})");
        }
        else if (isBasicAttack)
        {
            Debug.Log($"[Basic Attack] {attacker?.CharacterName}: No Amp Damage applied (basic attack)");
        }

        // ✅ คำนวณดาเมจแยกตาม damage type
        int finalPhysicalDamage = CalculateFinalDamage(physicalDamage, isCritical, DamageType.Normal, attacker);
        int finalMagicDamage = CalculateFinalDamage(magicDamage, isCritical, DamageType.Magic, attacker);
        int totalDamage = finalPhysicalDamage + finalMagicDamage;

        Debug.Log($"[Damage Breakdown] {attacker?.CharacterName ?? "Unknown"} -> {character.CharacterName}:");
        Debug.Log($"  Physical: {physicalDamage} -> {finalPhysicalDamage} (after Physical Armor: {GetCurrentArmor()})");
        Debug.Log($"  Magic: {magicDamage} -> {finalMagicDamage} (after Magic Armor: {GetCurrentMagicArmor()})");
        Debug.Log($"  Total: {totalDamage}, Critical: {isCritical}, IsBasicAttack: {isBasicAttack}");

        // Apply damage
        int oldHp = character.CurrentHp;
        character.CurrentHp -= totalDamage;
        character.CurrentHp = Mathf.Clamp(character.CurrentHp, 0, character.MaxHp);
        if (character is NetworkEnemy && AudioManager.instance != null)
        {
            AudioManager.instance.PlayEnemyHitSound(character);
        }
        // ✅ Apply lifesteal เฉพาะการโจมตีธรรมดา
        if (attacker != null && totalDamage > 0 && isBasicAttack)
        {
            ApplyLifesteal(attacker, totalDamage);
            Debug.Log($"[Life Steal] Applied for basic attack: {totalDamage} damage");
        }
        else if (attacker != null && totalDamage > 0 && !isBasicAttack)
        {
            Debug.Log($"[Life Steal] Skipped for skill attack (not basic attack)");
        }

        // Sync network state
        SyncHealthUpdate();

        // ✅ แสดง Damage Text ผ่าน RPC
        if (HasStateAuthority)
        {
            Vector3 textPosition = character.transform.position + Vector3.up * 2f;
            DamageType displayType = finalMagicDamage > finalPhysicalDamage ? DamageType.Magic : DamageType.Normal;
            RPC_ShowDamageText(textPosition, totalDamage, displayType, isCritical, false, false);
        }

        // Fire damage event สำหรับ visual flash
        OnDamageTaken?.Invoke(character, totalDamage, damageType, isCritical);
        if (attacker != null)
        {
            TryDropGoldFromDummy(totalDamage, attacker);
        }
        // Check death
        if (character.CurrentHp <= 0)
        {
            HandleDeath();
        }
    }

    public virtual void TakeDamage(int damage, DamageType damageType = DamageType.Normal, bool isCritical = false)
    {
        if (!HasStateAuthority && !HasInputAuthority) return;
        int finalDamage = CalculateFinalDamage(damage, isCritical, damageType, null);
        Debug.Log($"[TakeDamage] {character.CharacterName}: {damage} -> {finalDamage} ({damageType})");
        int oldHp = character.CurrentHp;
        character.CurrentHp -= finalDamage;
        character.CurrentHp = Mathf.Clamp(character.CurrentHp, 0, character.MaxHp);
        // Sync network state
        SyncHealthUpdate();
        // ✅ แสดง Damage Text ผ่าน RPC
        if (HasStateAuthority)
        {
            Vector3 textPosition = character.transform.position + Vector3.up * 2f;
            RPC_ShowDamageText(textPosition, finalDamage, damageType, isCritical, false, false);
        }

        OnDamageTaken?.Invoke(character, finalDamage, damageType, isCritical);

        // ลบ 2 บรรทัดนี้ออก:
        // 🆕 ✅ Fire damage event สำหรับ visual flash
        // OnDamageTaken?.Invoke(character, finalDamage, damageType, isCritical);

        // Check death
        if (character.CurrentHp <= 0)
        {
            HandleDeath();
        }
    }
    public virtual void TakeDamageFromAttacker(int damage, Character attacker, DamageType damageType = DamageType.Normal, bool isBasicAttack = false)
    {
        // ✅ ปรับปรุงให้ส่ง physical และ magic damage แยก
        var (physicalDamage, magicDamage) = attacker.GetAttackDamages();

        Debug.Log($"[TakeDamageFromAttacker] {attacker.CharacterName}: Physical={physicalDamage}, Magic={magicDamage}, Type={damageType}, IsBasicAttack={isBasicAttack}");

        TakeDamageFromAttacker(physicalDamage, magicDamage, attacker, damageType, isBasicAttack);
    }
    #endregion

    #region Status Effect Damage Handler - การจัดการดาเมจจาก Status Effects เช่น Poison, Burn, Bleed
    private void HandleStatusDamage(Character targetCharacter, int damage, DamageType damageType)
    {
        if (targetCharacter == character)
        {
            int oldHp = character.CurrentHp;
            character.CurrentHp -= damage;
            character.CurrentHp = Mathf.Clamp(character.CurrentHp, 0, character.MaxHp);

            // Sync network state
            SyncHealthUpdate();

            // ✅ แสดง Status Damage Text ผ่าน RPC
            if (HasStateAuthority)
            {
                Vector3 textPosition = character.transform.position + Vector3.up * 2.5f;
                StatusEffectType effectType = damageType switch
                {
                    DamageType.Poison => StatusEffectType.Poison,
                    DamageType.Burn => StatusEffectType.Burn,
                    DamageType.Bleed => StatusEffectType.Bleed,
                    _ => StatusEffectType.None
                };

                if (effectType != StatusEffectType.None)
                {
                    RPC_ShowStatusDamageText(textPosition, damage, effectType);
                }
            }

            // Check death
            if (character.CurrentHp <= 0)
            {
                HandleDeath();
            }
        }
    }
    private void ApplyLifesteal(Character attacker, int damageDealt)
    {
        // ✅ ใช้ GetEffectiveLifeSteal() ที่จะรวม Equipment bonus
        float lifeStealPercent = attacker.GetEffectiveLifeSteal();

        Debug.Log($"[ApplyLifesteal] {attacker.CharacterName}: Effective Lifesteal = {lifeStealPercent:F1}%");

        if (lifeStealPercent <= 0f) return;

        // Calculate lifesteal amount
        int lifeStealAmount = Mathf.RoundToInt(damageDealt * (lifeStealPercent / 100f));

        if (lifeStealAmount <= 0) return;

        // Apply healing to attacker
        int oldHp = attacker.CurrentHp;
        attacker.CurrentHp = Mathf.Min(attacker.CurrentHp + lifeStealAmount, attacker.MaxHp);
        int actualHeal = attacker.CurrentHp - oldHp;

        if (actualHeal > 0)
        {
            Debug.Log($"[Lifesteal] {attacker.CharacterName}: {lifeStealPercent:F1}% of {damageDealt} = {lifeStealAmount} HP healed ({actualHeal} actual)");

            // Show lifesteal text
            if (HasStateAuthority)
            {
                Vector3 textPosition = attacker.transform.position + Vector3.up * 2.5f;
                RPC_ShowLifestealText(textPosition, actualHeal);
            }

            // Sync attacker's health
            SyncAttackerHealth(attacker);

            // Fire heal event
            OnCharacterHealed?.Invoke(attacker, actualHeal);
        }
    }

    // ✅ เพิ่ม RPC สำหรับ Lifesteal text
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowLifestealText(Vector3 position, int healAmount)
    {
    }

    // เพิ่ม RPC สำหรับ Lifesteal text


    // เพิ่ม method สำหรับ sync attacker health
    private void SyncAttackerHealth(Character attacker)
    {
        if (HasStateAuthority)
        {
            attacker.NetworkedCurrentHp = attacker.CurrentHp;
            RPC_BroadcastAttackerHealthUpdate(attacker.NetworkedCurrentHp);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastAttackerHealthUpdate(int newHp)
    {
        // This will be handled by the attacker's own CombatManager
    }
    #endregion

    #region Damage Calculations - การคำนวณดาเมจสุดท้าย รวม Critical, Armor, Resistance และ Auras
    private int CalculateFinalDamage(int baseDamage, bool isCritical, DamageType damageType, Character attacker = null)
    {
        if (baseDamage <= 0) return 0;

        int finalDamage = baseDamage;

        // Apply Damage Aura bonus
        if (statusEffectManager != null)
        {
            float damageMultiplier = statusEffectManager.GetTotalDamageMultiplier();
            finalDamage = Mathf.RoundToInt(baseDamage * damageMultiplier);
            Debug.Log($"[Damage Aura] Base: {baseDamage} -> With Aura: {finalDamage} (x{damageMultiplier:F2})");
        }

        // Critical damage calculation
        if (isCritical)
        {
            // ✅ แก้ไข: ใช้ attacker แทน character
            float criticalDamageBonus = 0f;
            if (attacker != null)
            {
                criticalDamageBonus = attacker.GetEffectiveCriticalDamageBonus();
            }
            else
            {
                criticalDamageBonus = character.GetEffectiveCriticalDamageBonus();
            }

            int criticalDamage = Mathf.RoundToInt(finalDamage * (1f + criticalDamageBonus));

            Debug.Log($"[Critical Hit] {attacker?.CharacterName ?? character.CharacterName}: Base: {finalDamage} × (1 + {criticalDamageBonus:F2}) = {criticalDamage}");

            return criticalDamage;
        }

        // Apply resistance based on damage type
        float resistance = 0f;
        if (equipmentManager != null)
        {
            if (damageType == DamageType.Magic)
            {
                resistance = equipmentManager.GetTotalMagicalResistance();
            }
            else
            {
                resistance = equipmentManager.GetTotalPhysicalResistance();
            }
        }

        // Convert resistance to damage reduction
        if (resistance > 0f)
        {
            float damageReduction = resistance / 100f;
            finalDamage = Mathf.RoundToInt(finalDamage * (1f - damageReduction));
            Debug.Log($"[Resistance] Reduced by {resistance:F1}%: {finalDamage}");
        }

        // Apply protection aura
        if (statusEffectManager != null)
        {
            float protectionReduction = statusEffectManager.GetTotalDamageReduction();
            if (protectionReduction > 0f)
            {
                finalDamage = Mathf.RoundToInt(finalDamage * (1f - protectionReduction));
                Debug.Log($"[Protection Aura] Reduced by {protectionReduction * 100f:F1}%: {finalDamage}");
            }
        }

        // ✅ Apply armor based on damage type
        if (damageType == DamageType.Magic)
        {
            // Magic damage ใช้ Magic Armor
            int currentMagicArmor = GetCurrentMagicArmor();
            finalDamage = finalDamage - currentMagicArmor;
            Debug.Log($"[Magic Armor] Reduced by {currentMagicArmor}: {finalDamage}");
        }
        else
        {
            // Physical damage ใช้ Armor ปกติ
            int currentArmor = GetCurrentArmor();
            finalDamage = finalDamage - currentArmor;
            Debug.Log($"[Physical Armor] Reduced by {currentArmor}: {finalDamage}");
        }

        // Prevent negative damage
        finalDamage = Mathf.Max(1, finalDamage);

        Debug.Log($"[Final Damage] {baseDamage} -> {finalDamage} (type: {damageType}, critical: {isCritical})");
        return finalDamage;
    }
    private int GetCurrentMagicArmor()
    {
        int baseMagicArmor = character.MagicArmor;

        // Add magic armor from equipment
        if (equipmentManager != null)
        {
            baseMagicArmor += equipmentManager.GetMagicArmorBonus();
        }

        // Apply Armor Aura (ใช้กับ Magic Armor ด้วย)
        if (statusEffectManager != null)
        {
            float armorMultiplier = statusEffectManager.GetTotalArmorMultiplier();
            baseMagicArmor = Mathf.RoundToInt(baseMagicArmor * armorMultiplier);

            if (armorMultiplier > 1f)
            {
                Debug.Log($"[Magic Armor Aura] Magic Armor boosted by {(armorMultiplier - 1f) * 100:F0}%");
            }
        }

        // Apply Armor Break effect (ใช้กับ Magic Armor ด้วย)
        if (statusEffectManager != null && statusEffectManager.IsArmorBreak)
        {
            float reduction = statusEffectManager.ArmorBreakAmount;
            baseMagicArmor = Mathf.RoundToInt(baseMagicArmor * (1f - reduction));
            Debug.Log($"[Magic Armor Break] Magic Armor reduced by {reduction * 100}%: {baseMagicArmor}");
        }

        Debug.Log($"[GetCurrentMagicArmor] {character.CharacterName}: Base={character.MagicArmor}, Equipment={equipmentManager?.GetMagicArmorBonus()}, Final={baseMagicArmor}");

        return baseMagicArmor;
    }

    private int GetCurrentArmor()
    {
        int baseArmor = character.Armor;

        // Add armor from equipment
        if (equipmentManager != null)
        {
            baseArmor += equipmentManager.GetArmorBonus();
        }

        // Apply Armor Aura
        if (statusEffectManager != null)
        {
            float armorMultiplier = statusEffectManager.GetTotalArmorMultiplier();
            baseArmor = Mathf.RoundToInt(baseArmor * armorMultiplier);

            if (armorMultiplier > 1f)
            {
                Debug.Log($"[Physical Armor Aura] Armor boosted by {(armorMultiplier - 1f) * 100:F0}%");
            }
        }

        // Apply Armor Break effect
        if (statusEffectManager != null && statusEffectManager.IsArmorBreak)
        {
            float reduction = statusEffectManager.ArmorBreakAmount;
            baseArmor = Mathf.RoundToInt(baseArmor * (1f - reduction));
            Debug.Log($"[Physical Armor Break] Armor reduced by {reduction * 100}%: {baseArmor}");
        }

        Debug.Log($"[GetCurrentArmor] {character.CharacterName}: Base={character.Armor}, Equipment={equipmentManager?.GetArmorBonus()}, Final={baseArmor}");

        return baseArmor;
    }

    private bool CalculateCriticalHit(Character attacker)
    {
        float critRoll = UnityEngine.Random.Range(0f, 100f);
        float attackerCritChance = attacker.CriticalChance;

        // Add critical chance from equipment
        if (attacker.GetComponent<EquipmentManager>() != null)
        {
            attackerCritChance += attacker.GetComponent<EquipmentManager>().GetCriticalChanceBonus();
        }

        // Apply Critical Aura
        if (attacker.GetComponent<StatusEffectManager>() != null)
        {
            StatusEffectManager attackerStatus = attacker.GetComponent<StatusEffectManager>();
            float criticalBonus = attackerStatus.GetTotalCriticalBonus();
            attackerCritChance += criticalBonus * 100f;

            if (criticalBonus > 0f)
            {
                Debug.Log($"[Critical Aura] Critical chance boosted by {criticalBonus * 100:F0}%");
            }
        }

        // Apply Blind effect
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

        // ✅ เพิ่ม debug
        //  Debug.Log($"[Critical Check] {attacker.CharacterName}: Roll={critRoll:F1}, Chance={attackerCritChance:F1}%, Result={isCritical}");

        return isCritical;
    }

    private int ApplyAttackerStatusEffects(int damage, Character attacker)
    {
        int modifiedDamage = damage;

        // Apply Weakness effect
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
    #endregion

    #region Network Synchronization - การ Sync ข้อมูล Health ผ่าน Network สำหรับ Multiplayer
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
    #endregion

    #region Death System - ระบบการตายและการจัดการเมื่อตัวละครมี HP เหลือ 0
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
    #endregion

    #region Healing System - ระบบการรักษาและการฟื้นฟู HP
    public void Heal(int amount)
    {
        if (!HasStateAuthority && !HasInputAuthority) return;

        int oldHp = character.CurrentHp;
        character.CurrentHp = Mathf.Min(character.CurrentHp + amount, character.MaxHp);
        int actualHeal = character.CurrentHp - oldHp;

        Debug.Log($"[Heal] {character.CharacterName}: {oldHp} -> {character.CurrentHp} (+{actualHeal})");

        // ✅ แสดง Heal Text ผ่าน RPC
        if (actualHeal > 0 && HasStateAuthority)
        {
            Vector3 textPosition = character.transform.position + Vector3.up * 2f;
            RPC_ShowDamageText(textPosition, actualHeal, DamageType.Normal, false, true, false);

            OnCharacterHealed?.Invoke(character, actualHeal);
        }

        SyncHealthUpdate();
    }
    #endregion

    #region Public Query Methods - Methods สำหรับดูข้อมูลและสถานะต่างๆ ของตัวละคร
    public void FireDamageEvent(int damage, DamageType damageType, bool isCritical)
    {
        OnDamageTaken?.Invoke(character, damage, damageType, isCritical);
    }

    public float GetHealthPercentage()
    {
        return (float)character.CurrentHp / character.MaxHp;
    }
    #endregion
    #region Dummy Gold Drop System

    [Header("🎯 Dummy Gold Drop Settings")]
    [Tooltip("โอกาสพื้นฐานในการดรอปเงินจาก Dummy (%)")]
    public float dummyBaseDropChance = 15f; // 15% base chance

    [Tooltip("โอกาสเพิ่มต่อดาเมจ 1000 (%)")]
    public float dummyDropChancePerKDamage = 0.5f; // +0.5% ต่อ 1000 damage

    [Tooltip("โอกาสดรอปสูงสุด (%)")]
    public float dummyMaxDropChance = 50f; // สูงสุด 50%

    [Tooltip("จำนวนเงินพื้นฐาน")]
    public int dummyBaseGoldAmount = 10;

    [Tooltip("เงินเพิ่มต่อดาเมจ 10000")]
    public float dummyGoldPerTenKDamage = 5f; // +5 gold ต่อ 10k damage

    [Tooltip("จำนวนเงินสูงสุดต่อครั้ง")]
    public int dummyMaxGoldPerHit = 500; // สูงสุด 500 gold ต่อครั้ง

    [Tooltip("เวลาคูลดาวน์ระหว่างการดรอปเงิน (วินาที)")]
    public float dummyGoldDropCooldown = 2f; // ดรอปได้ทุก 2 วินาที

    private float lastDummyGoldDropTime = 0f;

    /// <summary>
    /// ตรวจสอบและดรอปเงินจาก Dummy
    /// เรียกจาก TakeDamageFromAttacker เมื่อโจมตี Dummy
    /// </summary>
    private void TryDropGoldFromDummy(int totalDamage, Character attacker)
    {
        // เช็คว่าเป็น Dummy หรือไม่
        NetworkEnemy enemy = character as NetworkEnemy;
        if (enemy == null || !enemy.IsDummy) return;

        // เช็ค cooldown
        if (Time.time - lastDummyGoldDropTime < dummyGoldDropCooldown) return;

        // คำนวณโอกาสดรอป (ยิ่งดาเมจเยอะ โอกาสยิ่งสูง)
        float dropChance = CalculateDummyDropChance(totalDamage);

        // สุ่มว่าจะดรอปหรือไม่
        float roll = UnityEngine.Random.Range(0f, 100f);

        if (roll <= dropChance)
        {
            // คำนวณจำนวนเงินที่ดรอป
            int goldAmount = CalculateDummyGoldAmount(totalDamage);

            // ดรอปเงินให้ attacker
            DropGoldToDummyAttacker(attacker, goldAmount, totalDamage, dropChance);

            // อัพเดทเวลา
            lastDummyGoldDropTime = Time.time;

            Debug.Log($"[Dummy Gold Drop] ✅ Dropped {goldAmount} gold! (Damage: {totalDamage}, Chance: {dropChance:F1}%)");
        }
        else
        {
            Debug.Log($"[Dummy Gold Drop] ❌ No drop (Roll: {roll:F1}% > {dropChance:F1}%)");
        }
    }

    /// <summary>
    /// คำนวณโอกาสดรอปตามดาเมจ
    /// </summary>
    private float CalculateDummyDropChance(int damage)
    {
        // Base chance
        float chance = dummyBaseDropChance;

        // เพิ่มโอกาสตามดาเมจ (แบบ linear scaling)
        float damageInK = damage / 1000f;
        chance += damageInK * dummyDropChancePerKDamage;

        // จำกัดไม่ให้เกิน max
        chance = Mathf.Min(chance, dummyMaxDropChance);

        return chance;
    }

    /// <summary>
    /// คำนวณจำนวนเงินที่ดรอปตามดาเมจ
    /// ใช้ Logarithmic scaling เพื่อป้องกันเงินเฟ้อ
    /// </summary>
    private int CalculateDummyGoldAmount(int damage)
    {
        // Base amount
        float goldAmount = dummyBaseGoldAmount;

        // เพิ่มเงินแบบ logarithmic (ยิ่งดาเมจสูง เงินเพิ่มช้าลง)
        if (damage > 1000)
        {
            float damageInTenK = damage / 10000f;

            // ใช้ log scaling เพื่อลดการเติบโตของเงิน
            float scaledGold = Mathf.Log10(1 + damageInTenK) * dummyGoldPerTenKDamage * 10f;
            goldAmount += scaledGold;
        }

        // จำกัดไม่ให้เกิน max
        goldAmount = Mathf.Min(goldAmount, dummyMaxGoldPerHit);

        // เพิ่มความสุ่ม ±10%
        float randomFactor = UnityEngine.Random.Range(0.9f, 1.1f);
        goldAmount *= randomFactor;

        return Mathf.RoundToInt(goldAmount);
    }

    /// <summary>
    /// ดรอปเงินให้ผู้โจมตี Dummy
    /// </summary>
    private void DropGoldToDummyAttacker(Character attacker, int goldAmount, int damage, float dropChance)
    {
        // หา CurrencyManager
        CurrencyManager currencyManager = attacker.GetComponent<CurrencyManager>();
        if (currencyManager != null)
        {
            // เพิ่มเงิน
            currencyManager.AddGold(goldAmount, false); // ไม่ save ทันที เพื่อ performance

            // แสดง text
            if (HasStateAuthority)
            {
                RPC_ShowDummyGoldDrop(attacker.transform.position, goldAmount, damage, dropChance);
            }

            Debug.Log($"[Dummy Gold] 💰 {attacker.CharacterName} gained {goldAmount} gold from {damage} damage!");
        }
    }

    /// <summary>
    /// แสดง Gold Drop text
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowDummyGoldDrop(Vector3 position, int goldAmount, int damage, float dropChance)
    {
        // ใช้ DamageTextManager แสดงเงิน
        DamageTextManager.ShowGoldPickup(position, goldAmount);

        // Optional: แสดง particle effect หรือ sound
        Debug.Log($"💰 Gold Drop: {goldAmount} (from {damage} dmg, {dropChance:F1}% chance)");
    }

    #endregion
    #region Debug Methods - Methods สำหรับ Debug และทดสอบระบบ Combat

    #endregion
}