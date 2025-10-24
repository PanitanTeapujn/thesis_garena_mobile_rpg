using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Berserker : Hero
{
    #region Berserker Properties
    [Header("🪓 Berserker Settings")]
    [SerializeField] private int skill1ManaCost = 20;
    [SerializeField] private int skill2ManaCost = 25;
    [SerializeField] private int skill3ManaCost = 15;
    [SerializeField] private int skill4ManaCost = 50;

    [Header("🔥 Rage System")]
    [Networked] public float RagePoints { get; set; }
    [Networked] public bool IsInBerserkMode { get; set; }
    private const float MAX_RAGE_POINTS = 100f;
    private const float BERSERK_DRAIN_RATE = 10f; // 10 points per second

    [Header("🔥 Rage Gain Settings")]
    private const float RAGE_FROM_DAMAGE_TAKEN = 3f; // +3 Rage per 100 damage
    private const int RAGE_FROM_KILL = 20;
    private const int RAGE_FROM_SKILL = 10;
    private const int RAGE_FROM_CRITICAL = 5;

    [Header("🪓 Savage Strike (Skill 1) Settings")]
    [SerializeField] private float savageStrikeRange = 3f;
    [SerializeField] private float savageStrikeAngle = 90f; // 90° cone

    [Header("🌪️ Whirlwind (Skill 2) Settings")]
    [Networked] public bool IsWhirlwinding { get; set; }
    [SerializeField] private float whirlwindDashDistance = 8f;
    [SerializeField] private float whirlwindRadius = 4f;

    [Header("🛡️ War Cry (Skill 3) Settings")]
    [SerializeField] private float warCryRadius = 6f;

    [Header("⚡ Titan's Wrath (Skill 4) Settings")]
    [Networked] public bool IsInTitanForm { get; set; }
    [Networked] public float TitanFormEndTime { get; set; }
    [SerializeField] private float groundSlamRadius = 7f;
    [SerializeField] private float earthquakeZoneRadius = 8f;

    [Header("🎨 Berserker Visual Effects")]
    [SerializeField] private ParticleSystem rageAuraEffect;           // Rage aura
    [SerializeField] private ParticleSystem berserkBurstEffect;       // Berserk activation
    [SerializeField] private ParticleSystem savageStrikeEffect;       // Skill 1 effect
    [SerializeField] private ParticleSystem whirlwindEffect;          // Skill 2 effect
    [SerializeField] private ParticleSystem warCryEffect;             // Skill 3 effect
    [SerializeField] private ParticleSystem groundSlamEffect;         // Skill 4 normal
    [SerializeField] private ParticleSystem titanFormEffect;          // Skill 4 berserk
    [SerializeField] private ParticleSystem earthquakeZoneEffect;     // Skill 4 zone

    [Header("🎨 Rage Aura Color Settings")]
    [SerializeField] private Color lightYellowColor = new Color(1f, 1f, 0.6f, 0.5f);      // 0-33
    [SerializeField] private Color orangeColor = new Color(1f, 0.5f, 0f, 0.7f);           // 34-66
    [SerializeField] private Color darkRedColor = new Color(0.8f, 0.1f, 0.1f, 0.9f);      // 67-99
    [SerializeField] private Color crimsonLightningColor = new Color(0.6f, 0f, 0f, 1f);   // 100

    [Header("🔱 Build Settings")]
    [SerializeField] private bool isPhysicalBuild = true; // true = Physical, false = Hybrid

    private ParticleSystem currentEarthquakeZone;
    #endregion

    protected override void Start()
    {
        base.Start();

        // Set attack type based on build
        AttackType = isPhysicalBuild ? AttackType.Physical : AttackType.Mixed;

        // Set cooldowns
        skill1Cooldown = 8f;
        skill2Cooldown = 10f;
        skill3Cooldown = 15f;
        skill4Cooldown = 30f;

        // Initialize Rage Points
        RagePoints = 0f;
        IsInBerserkMode = false;

        Debug.Log($"🪓 Berserker {CharacterName} initialized! Build: {(isPhysicalBuild ? "Physical" : "Hybrid")}");
    }

    #region Rage System

    /// <summary>
    /// เพิ่ม Rage Points
    /// </summary>
    private void GainRagePoints(int amount)
    {
        if (IsInBerserkMode) return; // ไม่เพิ่มระหว่าง Berserk Mode

        float oldRage = RagePoints;
        RagePoints = Mathf.Min(RagePoints + amount, MAX_RAGE_POINTS);

        Debug.Log($"🔥 [Rage] {CharacterName}: {oldRage:F0} → {RagePoints:F0} (+{amount})");

        // Update visual
        UpdateRageAuraVisual();

        // Check if should enter Berserk Mode
        if (RagePoints >= MAX_RAGE_POINTS && !IsInBerserkMode)
        {
            EnterBerserkMode();
        }
    }

    /// <summary>
    /// ลด Rage Points (ระหว่าง Berserk Mode)
    /// </summary>
    private void DrainRagePoints(float deltaTime)
    {
        if (!IsInBerserkMode) return;

        RagePoints = Mathf.Max(0f, RagePoints - (BERSERK_DRAIN_RATE * deltaTime));

        Debug.Log($"🔥 [Berserk Drain] {CharacterName}: {RagePoints:F0} rage remaining");

        // Update visual
        UpdateRageAuraVisual();

        // Check if should exit Berserk Mode
        if (RagePoints <= 0f)
        {
            ExitBerserkMode();
        }
    }

    /// <summary>
    /// เข้าสู่โหมด Berserk Mode
    /// </summary>
    private void EnterBerserkMode()
    {
        if (!HasStateAuthority) return;

        IsInBerserkMode = true;
        RagePoints = MAX_RAGE_POINTS;

        Debug.Log($"🔥 [BERSERK MODE ACTIVATED] {CharacterName}");

        // Visual effect
        RPC_ShowBerserkBurst();
        RPC_ShowBerserkAura(true);
    }

    /// <summary>
    /// ออกจากโหมด Berserk Mode
    /// </summary>
    private void ExitBerserkMode()
    {
        if (!HasStateAuthority) return;

        IsInBerserkMode = false;
        RagePoints = 0f;

        Debug.Log($"🔥 [Berserk Mode Ended] {CharacterName}");

        // Visual effect
        RPC_ShowBerserkAura(false);
    }

    /// <summary>
    /// อัปเดต Rage Aura visual ตาม Rage Points
    /// </summary>
    private void UpdateRageAuraVisual()
    {
        RPC_UpdateRageAuraColor(RagePoints);
    }

    /// <summary>
    /// คำนวณ Rage bonus ตามระดับ Rage
    /// </summary>
    private (float attackBonus, float armorBonus) GetRageBonus()
    {
        float attackBonus = 0f;
        float armorBonus = 0f;

        if (RagePoints < 33f)
        {
            attackBonus = 0.05f;  // +5%
            armorBonus = 0.10f;   // +10%
        }
        else if (RagePoints < 66f)
        {
            attackBonus = 0.15f;  // +15%
            armorBonus = 0.20f;   // +20%
        }
        else if (RagePoints < 100f)
        {
            attackBonus = 0.30f;  // +30%
            armorBonus = 0.35f;   // +35%
        }
        else // 100 Rage (Berserk Mode)
        {
            attackBonus = 0.50f;  // +50%
            armorBonus = 0.50f;   // +50%
        }

        return (attackBonus, armorBonus);
    }

    /// <summary>
    /// รับดาเมจแล้วได้ Rage
    /// </summary>
    public override void TakeDamageFromAttacker(int physicalDamage, int magicDamage, Character attacker, DamageType damageType, bool isBasicAttack = false)
    {
        base.TakeDamageFromAttacker(physicalDamage, magicDamage, attacker, damageType, isBasicAttack);

        // Calculate Rage gain from damage
        int totalDamage = physicalDamage + magicDamage;
        int rageGain = Mathf.RoundToInt((totalDamage / 100f) * RAGE_FROM_DAMAGE_TAKEN);

        if (rageGain > 0)
        {
            GainRagePoints(rageGain);
        }
    }

    #endregion

    #region Damage Calculation

    /// <summary>
    /// คำนวณ Base Damage สำหรับ Berserker พร้อม Rage Bonus
    /// </summary>
    private int CalculateBerserkerBaseDamage()
    {
        int baseDamage = AttackDamage;

        // Apply Rage bonus
        var (attackBonus, _) = GetRageBonus();
        baseDamage = Mathf.RoundToInt(baseDamage * (1f + attackBonus));

        // Berserk Mode bonuses
        if (IsInBerserkMode)
        {
            baseDamage = Mathf.RoundToInt(baseDamage * 1.5f); // +50% in Berserk
        }

        return baseDamage;
    }

    /// <summary>
    /// คำนวณ Skill Damage พร้อม Amp Damage และ Rage Bonus
    /// </summary>
    private int GetScaledSkillDamageWithAmp(float multiplier)
    {
        int baseDamage = CalculateBerserkerBaseDamage();
        int skillDamage = Mathf.RoundToInt(baseDamage * multiplier);

        // Amp Damage (ไม่ใช้ใน basic attack)
        float ampMultiplier = 1f + (AmpDamage / 100f);
        skillDamage = Mathf.RoundToInt(skillDamage * ampMultiplier);

        return Mathf.Max(1, skillDamage);
    }

    /// <summary>
    /// Override GetAttackDamage เพื่อรวม Rage Bonus
    /// </summary>
    public new int GetAttackDamage()
    {
        return CalculateBerserkerBaseDamage();
    }

    /// <summary>
    /// Override GetArmor เพื่อรวม Rage Bonus
    /// </summary>
    public new int GetArmor()
    {
        var (_, armorBonus) = GetRageBonus();
        int totalArmor = Mathf.RoundToInt(Armor * (1f + armorBonus));

        // Berserk Mode bonus
        if (IsInBerserkMode)
        {
            totalArmor = Mathf.RoundToInt(totalArmor * 1.5f);
        }

        return totalArmor;
    }

    #endregion

    #region Skill 1: Savage Strike

    public override void TryUseSkill1()
    {
        if (!CanUseSkill(skill1ManaCost)) return;

        if (Time.time < nextSkill1Time)
        {
            Debug.Log($"❌ Skill 1 on cooldown! {nextSkill1Time - Time.time:F1}s remaining");
            return;
        }

        float effectiveReduction = GetEffectiveReductionCoolDown();
        float reductionMultiplier = 1f - (effectiveReduction / 100f);
        reductionMultiplier = Mathf.Clamp(reductionMultiplier, 0.1f, 1f);
        float finalCooldown = skill1Cooldown * reductionMultiplier;

        // Physical Build: -20% cooldown
        if (isPhysicalBuild)
        {
            finalCooldown *= 0.8f;
        }

        if (!IsInBerserkMode)
        {
            UseMana(skill1ManaCost);
        }

        // Execute Savage Strike
        RPC_ExecuteSavageStrike(transform.position, transform.forward);

        GainRagePoints(15);

        nextSkill1Time = Time.time + finalCooldown;
        RPC_ShowSkillEffect("SavageStrike");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ExecuteSavageStrike(Vector3 position, Vector3 direction)
    {
        float damageMultiplier = IsInBerserkMode ? 2.0f : 1.2f;
        float cleaveMultiplier = 0.6f;
        float knockbackDistance = IsInBerserkMode ? 3f : 0f;

        // Main target
        Collider[] targets = Physics.OverlapSphere(position, 3f, LayerMask.GetMask("Enemy"));
        Character mainTarget = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider col in targets)
        {
            Character enemy = col.GetComponent<Character>();
            if (enemy != null)
            {
                float distance = Vector3.Distance(position, enemy.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    mainTarget = enemy;
                }
            }
        }

        // Hit main target
        if (mainTarget != null)
        {
            int damage = GetScaledSkillDamageWithAmp(damageMultiplier);
            mainTarget.TakeDamageFromAttacker(0, damage, this, DamageType.Normal, false);

            // Knockback in Berserk Mode
            if (knockbackDistance > 0f)
            {
                Vector3 knockbackDir = (mainTarget.transform.position - position).normalized;
                // TODO: Apply knockback force
            }

            Debug.Log($"🪓 Savage Strike hit {mainTarget.CharacterName}: {damage} damage");
        }

        // Cleave AOE
        float cleaveRange = IsInBerserkMode ? 5f : 3f;
        Collider[] cleaveTargets = Physics.OverlapSphere(position + direction * 1.5f, cleaveRange, LayerMask.GetMask("Enemy"));

        foreach (Collider col in cleaveTargets)
        {
            Character enemy = col.GetComponent<Character>();
            if (enemy != null && enemy != mainTarget)
            {
                // Check if in cone
                Vector3 toEnemy = (enemy.transform.position - position).normalized;
                float angle = Vector3.Angle(direction, toEnemy);

                if (angle <= savageStrikeAngle / 2f)
                {
                    int cleaveDamage = GetScaledSkillDamageWithAmp(damageMultiplier * cleaveMultiplier);
                    enemy.TakeDamageFromAttacker(0, cleaveDamage, this, DamageType.Normal, false);

                    Debug.Log($"🪓 Savage Strike cleave hit {enemy.CharacterName}: {cleaveDamage} damage");
                }
            }
        }
    }

    private bool CanUseSkill(int manaCost)
    {
        if (HasStatusEffect(StatusEffectType.Stun))
        {
            Debug.Log($"❌ Cannot use skill while stunned!");
            return false;
        }

        // In Berserk Mode, don't need mana
        if (IsInBerserkMode) return true;

        // Check mana
        if (CurrentMana < manaCost)
        {
            Debug.Log($"❌ Not enough mana! Need {manaCost}, have {CurrentMana}");
            return false;
        }

        return true;
    }

    #endregion

    #region Skill 2: Whirlwind

    public override void TryUseSkill2()
    {
        if (!CanUseSkill(skill2ManaCost)) return;

        if (Time.time < nextSkill2Time)
        {
            Debug.Log($"❌ Skill 2 on cooldown! {nextSkill2Time - Time.time:F1}s remaining");
            return;
        }

        float effectiveReduction = GetEffectiveReductionCoolDown();
        float reductionMultiplier = 1f - (effectiveReduction / 100f);
        reductionMultiplier = Mathf.Clamp(reductionMultiplier, 0.1f, 1f);
        float finalCooldown = skill2Cooldown * reductionMultiplier;

        if (!IsInBerserkMode)
        {
            UseMana(skill2ManaCost);
        }

        // Execute Whirlwind
        Vector3 dashDirection = GetDashDirection();
        RPC_ExecuteWhirlwind(transform.position, dashDirection);

        GainRagePoints(12);

        nextSkill2Time = Time.time + finalCooldown;
        RPC_ShowSkillEffect("Whirlwind");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ExecuteWhirlwind(Vector3 startPosition, Vector3 direction)
    {
        StartCoroutine(ExecuteWhirlwind(startPosition, direction));
    }

    private IEnumerator ExecuteWhirlwind(Vector3 startPosition, Vector3 direction)
    {
        IsWhirlwinding = true;

        float dashDistance = IsInBerserkMode ? 12f : 8f;
        float spinRadius = IsInBerserkMode ? 6f : 4f;
        int hitCount = IsInBerserkMode ? 5 : 3;
        float damageMultiplier = IsInBerserkMode ? 1.2f : 0.8f;

        // ✅ ตรวจสอบกำแพงก่อน dash
        RaycastHit wallHit;
        if (Physics.Raycast(startPosition, direction, out wallHit, dashDistance,
            LayerMask.GetMask("Wall", "Obstacle", "Default")))
        {
            dashDistance = Mathf.Max(0.5f, wallHit.distance - 0.5f);
            Debug.Log($"⚠️ Wall detected! Reducing whirlwind distance to {dashDistance}m");
        }

        Vector3 endPosition = startPosition + direction * dashDistance;
        float dashTime = dashDistance / 15f; // 15f = dashSpeed
        float elapsed = 0f;

        int currentHit = 0;
        float hitInterval = dashTime / hitCount;
        float nextHitTime = 0f;

        while (elapsed < dashTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / dashTime;

            Vector3 currentPosition = Vector3.Lerp(startPosition, endPosition, progress);

            // ✅ ตรวจสอบกำแพงระหว่าง dash (safety check)
            Vector3 moveVector = currentPosition - transform.position;
            if (moveVector.magnitude > 0.1f)
            {
                if (Physics.Raycast(transform.position, moveVector.normalized, moveVector.magnitude,
                    LayerMask.GetMask("Wall", "Obstacle", "Default")))
                {
                    Debug.LogWarning("🛑 Wall collision during whirlwind! Stopping.");
                    break;
                }
            }

            // ✅ ใช้ MovePosition สำหรับ physics-based movement
            if (HasInputAuthority && rb != null)
            {
                rb.MovePosition(currentPosition);
                NetworkedPosition = currentPosition;
            }
            else
            {
                transform.position = currentPosition;
            }

            if (elapsed >= nextHitTime && currentHit < hitCount)
            {
                // Hit enemies in radius
                Collider[] enemies = Physics.OverlapSphere(currentPosition, spinRadius, LayerMask.GetMask("Enemy"));

                foreach (Collider col in enemies)
                {
                    Character enemy = col.GetComponent<Character>();
                    if (enemy != null)
                    {
                        int damage = GetScaledSkillDamageWithAmp(damageMultiplier);
                        enemy.TakeDamageFromAttacker(0, damage, this, DamageType.Normal, false);

                        // Hybrid Build: Life steal
                        if (!isPhysicalBuild)
                        {
                            int healAmount = Mathf.RoundToInt(damage * 0.02f); // 2% life steal
                            Heal(healAmount);
                        }

                        // Apply Bleed in Berserk Mode
                        if (IsInBerserkMode)
                        {
                            int bleedDamage = GetScaledSkillDamageWithAmp(0.3f);
                            enemy.ApplyStatusEffect(StatusEffectType.Bleed, bleedDamage, 4f);
                        }

                        Debug.Log($"🌪️ Whirlwind hit {enemy.CharacterName}: {damage} damage");
                    }
                }

                currentHit++;
                nextHitTime = elapsed + hitInterval;
            }

            yield return null;
        }

        IsWhirlwinding = false;
        Debug.Log($"🌪️ Whirlwind complete! Hit {currentHit} times");
    }

    private Vector3 GetDashDirection()
    {
        if (cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            forward.y = 0;
            return forward.normalized;
        }
        return transform.forward;
    }

    #endregion

    #region Skill 3: War Cry

    public override void TryUseSkill3()
    {
        if (!CanUseSkill(skill3ManaCost)) return;

        if (Time.time < nextSkill3Time)
        {
            Debug.Log($"❌ Skill 3 on cooldown! {nextSkill3Time - Time.time:F1}s remaining");
            return;
        }

        float effectiveReduction = GetEffectiveReductionCoolDown();
        float reductionMultiplier = 1f - (effectiveReduction / 100f);
        reductionMultiplier = Mathf.Clamp(reductionMultiplier, 0.1f, 1f);
        float finalCooldown = skill3Cooldown * reductionMultiplier;

        if (!IsInBerserkMode)
        {
            UseMana(skill3ManaCost);
        }

        // Execute War Cry
        RPC_ExecuteWarCry(transform.position);

        GainRagePoints(20);

        nextSkill3Time = Time.time + finalCooldown;
        RPC_ShowSkillEffect("WarCry");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ExecuteWarCry(Vector3 position)
    {
        float armorBonus = IsInBerserkMode ? 0.50f : 0.30f;
        float magicArmorBonus = IsInBerserkMode ? 0.30f : 0f;
        float healPercent = IsInBerserkMode ? 0.20f : 0.10f;
        float weaknessAmount = IsInBerserkMode ? 0.40f : 0.25f;
        float slowAmount = IsInBerserkMode ? 0.30f : 0f;
        float debuffDuration = IsInBerserkMode ? 6f : 4f;
        float buffDuration = 5f;
        float aoeRadius = IsInBerserkMode ? 8f : 6f;

        // Self buff
        int healAmount = Mathf.RoundToInt(MaxHp * healPercent);
        Heal(healAmount);

        // Apply buffs (temporary - could use status effect system)
        // TODO: Implement armor buff system
        Debug.Log($"🛡️ War Cry: +{armorBonus * 100}% Armor, healed {healAmount} HP");

        // Debuff enemies
        Collider[] enemies = Physics.OverlapSphere(position, aoeRadius, LayerMask.GetMask("Enemy"));

        foreach (Collider col in enemies)
        {
            Character enemy = col.GetComponent<Character>();
            if (enemy != null)
            {
                enemy.ApplyStatusEffect(StatusEffectType.Weakness, 0, debuffDuration);

                if (slowAmount > 0f)
                {
                    enemy.ApplyStatusEffect(StatusEffectType.Freeze, 0, debuffDuration); // Using Freeze for slow
                }

                Debug.Log($"🛡️ War Cry debuffed {enemy.CharacterName}");
            }
        }

        // Team Aura (based on build)
        if (isPhysicalBuild)
        {
            // Physical Build: +20% Attack Speed Aura
            ApplyStatusEffect(StatusEffectType.AttackSpeedAura, 0, buffDuration);
        }
        else
        {
            // Hybrid Build: +15% Damage Aura
            ApplyStatusEffect(StatusEffectType.DamageAura, 0, buffDuration);
        }
    }

    #endregion

    #region Skill 4: Titan's Wrath

    public override void TryUseSkill4()
    {
        if (!CanUseSkill(skill4ManaCost)) return;

        if (Time.time < nextSkill4Time)
        {
            Debug.Log($"❌ Skill 4 on cooldown! {nextSkill4Time - Time.time:F1}s remaining");
            return;
        }

        float effectiveReduction = GetEffectiveReductionCoolDown();
        float reductionMultiplier = 1f - (effectiveReduction / 100f);
        reductionMultiplier = Mathf.Clamp(reductionMultiplier, 0.1f, 1f);
        float finalCooldown = skill4Cooldown * reductionMultiplier;

        if (!IsInBerserkMode)
        {
            // Normal Mode: Ground Slam
            UseMana(skill4ManaCost);
            RPC_ExecuteGroundSlam(transform.position);
            GainRagePoints(30);

            Debug.Log($"⚡ [Skill 4 Normal] {CharacterName}: Ground Slam");
        }
        else
        {
            // Berserk Mode: Titan Rampage
            RPC_ExecuteTitanRampage();

            Debug.Log($"⚡ [Skill 4 Berserk] {CharacterName}: Titan Rampage");
        }

        nextSkill4Time = Time.time + finalCooldown;
        RPC_ShowSkillEffect("TitanWrath");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ExecuteGroundSlam(Vector3 position)
    {
        StartCoroutine(ExecuteGroundSlam(position));
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ExecuteTitanRampage()
    {
        StartCoroutine(ExecuteTitanRampage());
    }

    private IEnumerator ExecuteGroundSlam(Vector3 position)
    {
        // Initial impact damage and stun
        Collider[] enemies = Physics.OverlapSphere(position, groundSlamRadius, LayerMask.GetMask("Enemy"));

        foreach (Collider col in enemies)
        {
            Character enemy = col.GetComponent<Character>();
            if (enemy != null)
            {
                int damage = GetScaledSkillDamageWithAmp(2.5f);
                enemy.TakeDamageFromAttacker(0, damage, this, DamageType.Normal, false);
                enemy.ApplyStatusEffect(StatusEffectType.Stun, 0, 2f);

                Debug.Log($"⚡ Ground Slam hit {enemy.CharacterName}: {damage} damage + 2s stun");
            }
        }

        // Create Shockwave Zone
        float zoneDuration = 5f;
        float tickInterval = 1f;
        float nextTick = 0f;
        float elapsed = 0f;

        while (elapsed < zoneDuration)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= nextTick)
            {
                // Damage enemies in zone
                Collider[] zoneEnemies = Physics.OverlapSphere(position, groundSlamRadius, LayerMask.GetMask("Enemy"));

                foreach (Collider col in zoneEnemies)
                {
                    Character enemy = col.GetComponent<Character>();
                    if (enemy != null)
                    {
                        int zoneDamage = GetScaledSkillDamageWithAmp(0.4f);
                        enemy.TakeDamageFromAttacker(0, zoneDamage, this, DamageType.Normal, false);
                    }
                }

                // Buff self in zone
                // TODO: Apply +20% Physical Armor buff

                nextTick += tickInterval;
            }

            yield return null;
        }

        Debug.Log($"⚡ Ground Slam zone ended");
    }

    private IEnumerator ExecuteTitanRampage()
    {
        IsInTitanForm = true;

        float duration = isPhysicalBuild ? 10f : 8f;
        TitanFormEndTime = Time.time + duration;

        // ✅ Apply Titan Form buffs
        // TODO: Apply stat multipliers (+100% Attack, +70% Armor, etc.)

        // ✅ FIX: ขยายตัวโดยไม่ทะลุพื้น
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.3f; // +30% size

        // ปรับ collider ให้เหมาะสมกับขนาดใหม่
        CapsuleCollider capsuleCol = GetComponent<CapsuleCollider>();
        BoxCollider boxCol = GetComponent<BoxCollider>();

        Vector3 originalColliderCenter = Vector3.zero;
        float originalColliderHeight = 0f;

        if (capsuleCol != null)
        {
            originalColliderCenter = capsuleCol.center;
            originalColliderHeight = capsuleCol.height;

            // ✅ ยกศูนย์กลาง collider ขึ้นเมื่อขยายตัว
            capsuleCol.center = new Vector3(
                originalColliderCenter.x,
                originalColliderCenter.y * 1.3f, // ยกขึ้น
                originalColliderCenter.z
            );
            capsuleCol.height = originalColliderHeight * 1.3f;
        }
        else if (boxCol != null)
        {
            originalColliderCenter = boxCol.center;

            // ✅ ยกศูนย์กลาง collider ขึ้นเมื่อขยายตัว
            boxCol.center = new Vector3(
                originalColliderCenter.x,
                originalColliderCenter.y * 1.3f, // ยกขึ้น
                originalColliderCenter.z
            );
        }

        transform.localScale = targetScale;

        float tickInterval = 1f;
        float nextTick = 0f;
        float elapsed = 0f;

        int totalEnemiesHit = 0;

        Debug.Log($"⚡ [Titan Rampage] Started! Duration: {duration}s");

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= nextTick)
            {
                // Damage and slow enemies in earthquake zone
                Collider[] enemies = Physics.OverlapSphere(transform.position, earthquakeZoneRadius, LayerMask.GetMask("Enemy"));

                foreach (Collider col in enemies)
                {
                    Character enemy = col.GetComponent<Character>();
                    if (enemy != null)
                    {
                        int damage = GetScaledSkillDamageWithAmp(0.8f);
                        enemy.TakeDamageFromAttacker(0, damage, this, DamageType.Normal, false);

                        // Apply slow
                        enemy.ApplyStatusEffect(StatusEffectType.Freeze, 0, tickInterval + 0.5f);

                        // Hybrid Build: Life steal
                        if (!isPhysicalBuild)
                        {
                            int healAmount = Mathf.RoundToInt(damage * 0.05f); // 5%
                            Heal(healAmount);
                        }

                        totalEnemiesHit++;
                    }
                }

                nextTick += tickInterval;
            }

            yield return null;
        }

        // Finale: Explosion
        Debug.Log($"⚡ [Titan Rampage] Finale! Explosion!");

        float explosionRadius = 10f;
        float explosionMultiplier = isPhysicalBuild ? 4.5f : 3.0f; // Physical Build: +50% finale damage

        Collider[] finalEnemies = Physics.OverlapSphere(transform.position, explosionRadius, LayerMask.GetMask("Enemy"));

        foreach (Collider col in finalEnemies)
        {
            Character enemy = col.GetComponent<Character>();
            if (enemy != null)
            {
                int explosionDamage = GetScaledSkillDamageWithAmp(explosionMultiplier);
                enemy.TakeDamageFromAttacker(0, explosionDamage, this, DamageType.Critical, false);

                Debug.Log($"⚡ Titan Rampage Finale hit {enemy.CharacterName}: {explosionDamage} damage");
            }
        }

        // Reset size and collider
        transform.localScale = originalScale;

        if (capsuleCol != null)
        {
            capsuleCol.center = originalColliderCenter;
            capsuleCol.height = originalColliderHeight;
        }
        else if (boxCol != null)
        {
            boxCol.center = originalColliderCenter;
        }

        IsInTitanForm = false;
        Debug.Log($"⚡ [Titan Rampage] Ended! Total enemies hit: {totalEnemiesHit}");
    }

    #endregion

    #region Visual Effects RPCs

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateRageAuraColor(float ragePoints)
    {
        if (rageAuraEffect == null) return;

        ParticleSystem.MainModule main = rageAuraEffect.main;
        Color targetColor;

        if (ragePoints < 33f)
        {
            targetColor = lightYellowColor;
        }
        else if (ragePoints < 66f)
        {
            targetColor = orangeColor;
        }
        else if (ragePoints < 100f)
        {
            targetColor = darkRedColor;
        }
        else
        {
            targetColor = crimsonLightningColor;
        }

        main.startColor = targetColor;

        // Adjust particle emission based on rage
        ParticleSystem.EmissionModule emission = rageAuraEffect.emission;
        emission.rateOverTime = ragePoints * 0.5f; // 0-50 particles/s
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowBerserkBurst()
    {
        if (berserkBurstEffect != null)
        {
            GameObject burst = Instantiate(berserkBurstEffect.gameObject, transform.position + Vector3.up * 1f, Quaternion.identity);

            ParticleSystem ps = burst.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = crimsonLightningColor;
                main.startLifetime = 1f;
            }

            Destroy(burst, 2f);
        }

        Debug.Log($"🔥 [Visual] Berserk Burst!");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowBerserkAura(bool show)
    {
        if (rageAuraEffect != null)
        {
            if (show)
            {
                rageAuraEffect.Play();

                ParticleSystem.MainModule main = rageAuraEffect.main;
                main.startColor = crimsonLightningColor;

                ParticleSystem.EmissionModule emission = rageAuraEffect.emission;
                emission.rateOverTime = 50f; // Max emission
            }
            else
            {
                rageAuraEffect.Stop();
            }
        }

        Debug.Log($"🔥 [Visual] Berserk Aura: {(show ? "ON" : "OFF")}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowSkillEffect(string effectName)
    {
        Debug.Log($"✨ [Skill Effect] {CharacterName} - {effectName}");

        switch (effectName)
        {
            case "SavageStrike":
                ShowSavageStrikeEffect();
                break;
            case "Whirlwind":
                ShowWhirlwindEffect();
                break;
            case "WarCry":
                ShowWarCryEffect();
                break;
            case "TitanWrath":
                ShowTitanWrathEffect();
                break;
        }
    }

    private void ShowSavageStrikeEffect()
    {
        if (savageStrikeEffect != null)
        {
            GameObject effect = Instantiate(savageStrikeEffect.gameObject, transform.position + transform.forward * 1.5f, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    private void ShowWhirlwindEffect()
    {
        if (whirlwindEffect != null)
        {
            GameObject effect = Instantiate(whirlwindEffect.gameObject, transform.position, Quaternion.identity);
            effect.transform.SetParent(transform); // Follow character
            Destroy(effect, 3f);
        }
    }

    private void ShowWarCryEffect()
    {
        if (warCryEffect != null)
        {
            GameObject effect = Instantiate(warCryEffect.gameObject, transform.position + Vector3.up * 1f, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    private void ShowTitanWrathEffect()
    {
        if (!IsInBerserkMode && groundSlamEffect != null)
        {
            GameObject effect = Instantiate(groundSlamEffect.gameObject, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }
        else if (IsInBerserkMode && titanFormEffect != null)
        {
            GameObject effect = Instantiate(titanFormEffect.gameObject, transform.position, Quaternion.identity);
            effect.transform.SetParent(transform);
            Destroy(effect, 10f);
        }
    }

    #endregion

    #region Override Methods

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        // Drain Rage during Berserk Mode
        if (HasStateAuthority && IsInBerserkMode)
        {
            DrainRagePoints(Runner.DeltaTime);
        }
    }

    protected override void RPC_OnAttackHit(NetworkObject enemyObject)
    {
        base.RPC_OnAttackHit(enemyObject);

        // Check for critical hit to gain extra Rage
        // TODO: Implement critical detection and give +5 Rage
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        // Clean up effects
        if (currentEarthquakeZone != null)
        {
            Destroy(currentEarthquakeZone.gameObject);
        }
    }

    #endregion
}