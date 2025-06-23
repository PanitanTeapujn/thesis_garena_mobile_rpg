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
        float attackSpeedMultiplier = attacker.AttackSpeed;

        if (attacker.GetComponent<EquipmentManager>() != null)
        {
            attackSpeedMultiplier += attacker.GetComponent<EquipmentManager>().GetAttackSpeedBonus();
        }

        if (attacker.GetComponent<StatusEffectManager>() != null)
        {
            StatusEffectManager attackerStatus = attacker.GetComponent<StatusEffectManager>();
            float auraMultiplier = attackerStatus.GetTotalAttackSpeedMultiplier();
            attackSpeedMultiplier *= auraMultiplier;

            if (auraMultiplier > 1f)
            {
                Debug.Log($"[Attack Speed Aura] Attack speed boosted by {(auraMultiplier - 1f) * 100:F0}%");
            }
        }

        float finalCooldown = baseAttackCooldown / Mathf.Max(0.1f, attackSpeedMultiplier);
        return finalCooldown;
    }
    #endregion

    #region Main Damage System - ระบบรับดาเมจหลักที่รองรับทั้ง Physical และ Magic Damage
    public virtual void TakeDamageFromAttacker(int physicalDamage, int magicDamage, Character attacker, DamageType damageType = DamageType.Normal)
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
            isCritical = CalculateCriticalHit(attacker);
        }

        // คำนวณ damage แยกตาม type
        int finalPhysicalDamage = CalculateFinalDamage(physicalDamage, isCritical, DamageType.Normal);
        int finalMagicDamage = CalculateFinalDamage(magicDamage, isCritical, DamageType.Magic);
        int totalDamage = finalPhysicalDamage + finalMagicDamage;

        // Apply damage
        int oldHp = character.CurrentHp;
        character.CurrentHp -= totalDamage;
        character.CurrentHp = Mathf.Clamp(character.CurrentHp, 0, character.MaxHp);

        // Sync network state
        SyncHealthUpdate();

        // ✅ แสดง Damage Text ผ่าน RPC
        if (HasStateAuthority)
        {
            Vector3 textPosition = character.transform.position + Vector3.up * 2f;
            RPC_ShowDamageText(textPosition, totalDamage, damageType, isCritical, false, false);
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

        int finalDamage = CalculateFinalDamage(damage, isCritical, damageType);

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

        // Check death
        if (character.CurrentHp <= 0)
        {
            HandleDeath();
        }
    }

    public virtual void TakeDamageFromAttacker(int damage, Character attacker, DamageType damageType = DamageType.Normal)
    {
        var (physicalDamage, magicDamage) = attacker.GetAttackDamages();
        TakeDamageFromAttacker(physicalDamage, magicDamage, attacker, damageType);
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
    #endregion

    #region Damage Calculations - การคำนวณดาเมจสุดท้าย รวม Critical, Armor, Resistance และ Auras
    private int CalculateFinalDamage(int baseDamage, bool isCritical, DamageType damageType)
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
            float criticalDamageBonus = character.GetEffectiveCriticalDamageBonus();
            int criticalDamage = Mathf.RoundToInt(finalDamage * (1f + criticalDamageBonus));

            Debug.Log($"[Critical Hit] Base: {finalDamage} × (1 + {criticalDamageBonus:F2}) = {criticalDamage}");
            Debug.Log($"[Critical Stats] CriticalDamageBonus: {character.CriticalDamageBonus}, Equipment Bonus: {(equipmentManager?.GetCriticalMultiplierBonus() ?? 0f)}, Total: {criticalDamageBonus}");

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

        // Apply armor (only for physical damage)
        if (damageType != DamageType.Magic)
        {
            int currentArmor = GetCurrentArmor();
            finalDamage = finalDamage - currentArmor;
            Debug.Log($"[Armor] Reduced by {currentArmor}: {finalDamage}");
        }

        // Prevent negative damage
        finalDamage = Mathf.Max(1, finalDamage);

        Debug.Log($"[Final Damage] {baseDamage} -> {finalDamage} (type: {damageType}, critical: {isCritical})");
        return finalDamage;
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
                Debug.Log($"[Armor Aura] Armor boosted by {(armorMultiplier - 1f) * 100:F0}%");
            }
        }

        // Apply Armor Break effect
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

    #region Debug Methods - Methods สำหรับ Debug และทดสอบระบบ Combat
    public void DebugCriticalStats()
    {
        if (character == null) return;

        float baseCrit = character.CriticalDamageBonus;
        float equipmentBonus = equipmentManager?.GetCriticalMultiplierBonus() ?? 0f;
        float totalCrit = character.GetEffectiveCriticalDamageBonus();

        Debug.Log($"=== {character.CharacterName} Critical Stats Debug (New System) ===");
        Debug.Log($"Base Critical Damage Bonus: {baseCrit}");
        Debug.Log($"Equipment Bonus: {equipmentBonus}");
        Debug.Log($"Total Effective: {totalCrit}");
        Debug.Log($"Expected Damage with Crit: Base × (1 + {totalCrit}) = Base × {1f + totalCrit:F2}");
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestCriticalDamage(int testBaseDamage = 55)
    {
        Debug.Log($"=== Testing Critical Damage Calculation ===");
        Debug.Log($"Test Base Damage: {testBaseDamage}");

        // Test normal damage
        int normalDamage = CalculateFinalDamage(testBaseDamage, false, DamageType.Normal);
        Debug.Log($"Normal Damage Result: {normalDamage}");

        // Test critical damage
        int criticalDamage = CalculateFinalDamage(testBaseDamage, true, DamageType.Normal);
        Debug.Log($"Critical Damage Result: {criticalDamage}");

        // Show stats
        DebugCriticalStats();
    }
    #endregion
}