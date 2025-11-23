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
    [Networked] public bool IsInBerserkMode { get; set; } // ✅ เพิ่มกลับมา
    [SerializeField] private bool isBerserkActive = false; // ใช้ใน client-side
    private float accumulatedDrain = 0f; // สะสม drain amount ที่ยังไม่ได้ปัดเศษ

    [Header("🔥 Rage Gain Settings")]
    private const float RAGE_FROM_DAMAGE_TAKEN = 3f; // +3 Rage per 100 damage
    private const int RAGE_FROM_KILL = 20;
    private const int RAGE_FROM_SKILL = 10;
    private const int RAGE_FROM_CRITICAL = 5;

    [Header("🪓 Savage Strike (Skill 1) Settings")]
    [SerializeField] private float savageStrikeRange = 3f;
    [SerializeField] private float savageStrikeAngle = 90f; // 90° cone
    [SerializeField] private float ferrisXPatternRadius = 8f; // ✅ X-Pattern รัศมี

    [Header("🌪️ Whirlwind (Skill 2) Settings")]
    [Networked] public bool IsWhirlwinding { get; set; }
    [SerializeField] private float normalDashDistance = 5f; // ✅ Normal: 5m
    [SerializeField] private float ferrisDashDistance = 7f; // ✅ Ferris: 7m
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
    [SerializeField] private ParticleSystem basicAttackEffect;        // Basic attack effect

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
        CurrentFerrisPoint = 0;
        UpdateReflectDamage();

        Debug.Log($"🪓 Berserker {CharacterName} initialized! Build: {(isPhysicalBuild ? "Physical" : "Hybrid")}");
    }

    #region Rage System

    /// <summary>
    /// เพิ่ม Rage Points
    /// </summary>
    /// 

    private void GainRagePoints(int amount)
    {
        if (IsInBerserkMode) return; // ไม่เพิ่มระหว่าง Berserk Mode

        float oldRage = CurrentFerrisPoint;
        GainFerrisPoint(amount); // ใช้ method จาก Hero.cs

        Debug.Log($"🔥 [Ferris Points] {CharacterName}: {oldRage:F0} → {CurrentFerrisPoint:F0} (+{amount})");

        // Update visual
        UpdateRageAuraVisual();
    }

    /// <summary>
    /// ลด Rage Points (ระหว่าง Berserk Mode)
    /// </summary>
    /// <summary>
    /// ลด Ferris Points (ระหว่าง Berserk Mode)
    /// </summary>
    /// <summary>
    /// ลด Ferris Points (ระหว่าง Berserk Mode)
    /// </summary>
    private void DrainRagePoints(float deltaTime)
    {
        if (!IsInBerserkMode) return;

        float drainRate = 10f; // 10 points per second

        // ✅ สะสม drain amount
        accumulatedDrain += drainRate * deltaTime;

        // ✅ เมื่อสะสมได้ >= 1 point ให้ลดทีละ 1 point
        if (accumulatedDrain >= 1f)
        {
            int drainAmount = Mathf.FloorToInt(accumulatedDrain);
            CurrentFerrisPoint = Mathf.Max(0, CurrentFerrisPoint - drainAmount);

            accumulatedDrain -= drainAmount; // เก็บเศษไว้

            Debug.Log($"🔥 [Berserk Drain] {CharacterName}: -{drainAmount} points, {CurrentFerrisPoint} remaining (accumulated: {accumulatedDrain:F2})");

            // Update visual
            UpdateRageAuraVisual();

            // Check if should exit Berserk Mode
            if (CurrentFerrisPoint <= 0)
            {
                accumulatedDrain = 0f; // รีเซ็ตตัวสะสม
                ExitBerserkMode();
            }
        }
    }
    protected override void OnFerrisPointFull()
    {
        base.OnFerrisPointFull();

        // ✅ เช็คว่ายังไม่ได้อยู่ใน Berserk Mode
        if (!IsInBerserkMode)
        {
            EnterBerserkMode();
        }
    }
    /// <summary>
    /// เข้าสู่โหมด Berserk Mode
    /// </summary>
    /// <summary>
    /// เข้าสู่โหมด Berserk Mode
    /// </summary>

    private void EnterBerserkMode()
    {
        if (!HasStateAuthority) return;

        IsInBerserkMode = true;
        isBerserkActive = true;
        accumulatedDrain = 0f;

        Debug.Log($"🔥 [BERSERK MODE ACTIVATED] {CharacterName}");

        // ✅ Apply Ferris Mode Auras
        if (statusEffectManager != null)
        {
            // Protection Aura: -15% damage taken, 6m radius, 20s
            statusEffectManager.ApplyProtectionAura(6f, 0.15f, 20f);

            // Health Regen Aura: +50% health regen, 5m radius, 20s
            statusEffectManager.ApplyHealthRegenAura(5f, 0.5f, 20f);

            Debug.Log($"🛡️ [Ferris Mode] Applied Protection Aura (-15% damage) + Health Regen Aura (+50%)");
        }

        // แสดงข้อความ "FERRIS MODE"
        RPC_ShowBerserkModeText();

        // Visual effect
        RPC_ShowBerserkBurst();
        RPC_ShowBerserkAura(true);
        ResetAllSkillCooldowns();

        // ✅ เปลี่ยน skill icons ทั้ง 4 ท่า
        if (HasInputAuthority)
        {
            CombatUIManager uiManager = FindObjectOfType<CombatUIManager>();
            if (uiManager != null && SkillIconManager.Instance != null)
            {
                SkillIconManager.Instance.SetSkillIconUpgraded(uiManager, "IronJuggernaut", 1, true);
                SkillIconManager.Instance.SetSkillIconUpgraded(uiManager, "IronJuggernaut", 2, true);
                SkillIconManager.Instance.SetSkillIconUpgraded(uiManager, "IronJuggernaut", 3, true);
                SkillIconManager.Instance.SetSkillIconUpgraded(uiManager, "IronJuggernaut", 4, true);
            }
        }
    }

    // ✅ เพิ่ม RPC สำหรับแสดงข้อความ (เหมือน Assassin)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowBerserkModeText()
    {
        Vector3 textPosition = transform.position + Vector3.up * 1f;

        // ✅ สีแดงเข้ม (แทนสีม่วงของ Assassin)
        Color berserkColor = new Color(1f, 0.2f, 0f, 1f); // แดงสว่าง
        DamageTextManager.ShowCustomText(textPosition, "FERRIS MODE", berserkColor, 1f);

        Debug.Log($"🔥 [Visual] Berserk Mode text displayed for {CharacterName}");
    }
    private void ExitBerserkMode()
    {
        if (!HasStateAuthority) return;

        IsInBerserkMode = false;
        isBerserkActive = false;
        CurrentFerrisPoint = 0;

        Debug.Log($"🔥 [Berserk Mode Ended] {CharacterName}");

        // ✅ แสดงข้อความ "FERRIS MODE End" เหมือน Assassin
        RPC_ShowBerserkModeEndText();

        // Visual effect
        RPC_ShowBerserkAura(false);

        // ✅ รีเซ็ต skill icons ทั้ง 4 ท่า
        if (HasInputAuthority)
        {
            CombatUIManager uiManager = FindObjectOfType<CombatUIManager>();
            if (uiManager != null && SkillIconManager.Instance != null)
            {
                SkillIconManager.Instance.SetSkillIconUpgraded(uiManager, "IronJuggernaut", 1, false);
                SkillIconManager.Instance.SetSkillIconUpgraded(uiManager, "IronJuggernaut", 2, false);
                SkillIconManager.Instance.SetSkillIconUpgraded(uiManager, "IronJuggernaut", 3, false);
                SkillIconManager.Instance.SetSkillIconUpgraded(uiManager, "IronJuggernaut", 4, false);
            }
        }
    }

    // ✅ เพิ่ม RPC สำหรับข้อความ End
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowBerserkModeEndText()
    {
        Vector3 textPosition = transform.position + Vector3.up * 1f;

        // ข้อความสีเทา
        Color endColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        DamageTextManager.ShowCustomText(textPosition, "FERRIS MODE End", endColor, 1.0f);

        Debug.Log($"🔥 [Visual] Berserk Mode end text displayed for {CharacterName}");
    }

    private void UpdateRageAuraVisual()
    {
        RPC_UpdateRageAuraColor(CurrentFerrisPoint);
    }

    /// <summary>
    /// คำนวณ Rage bonus ตามระดับ Rage
    /// </summary>
    private (float attackBonus, float armorBonus) GetRageBonus()
    {
        float attackBonus = 0f;
        float armorBonus = 0f;

        if (CurrentFerrisPoint < 33f)
        {
            attackBonus = 0.05f;  // +5%
            armorBonus = 0.10f;   // +10%
        }
        else if (CurrentFerrisPoint < 66f)
        {
            attackBonus = 0.15f;  // +15%
            armorBonus = 0.20f;   // +20%
        }
        else if (CurrentFerrisPoint < 100f)
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
    // ใน Berserker.cs - แก้ไข CalculateBerserkerBaseDamage

    private int CalculateBerserkerBaseDamage()
    {
        // ✅ เพิ่ม Level Scaling (เหมือน Assassin)
        int levelBonus = GetCurrentLevel() * 5; // +5 per level (เพราะ Berserker เน้น Physical)
        int baseDamage = AttackDamage + levelBonus;

        // Apply Rage bonus
        var (attackBonus, _) = GetRageBonus();
        baseDamage = Mathf.RoundToInt(baseDamage * (1f + attackBonus));

        // Berserk Mode bonuses
        if (IsInBerserkMode)
        {
            baseDamage = Mathf.RoundToInt(baseDamage * 1.5f); // +50% in Berserk
        }

        Debug.Log($"🪓 [Damage Calc] ATK={AttackDamage} + Lv{GetCurrentLevel()}×5 = {baseDamage} (Rage: {attackBonus:P0}, Berserk: {IsInBerserkMode})");

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

        // ✅ Apply Damage Aura
        if (statusEffectManager != null)
        {
            statusEffectManager.ApplyDamageAura(5f, 0.2f, 15f); // +20% damage, 5m radius, 15s
            Debug.Log($"🔥 [Skill 1] Applied Damage Aura: +20% damage");
        }

        if (!IsInBerserkMode)
        {
            // Normal Mode: ทุบครั้งเดียว
            RPC_ExecuteSavageStrike(transform.position, transform.forward);
        }
        else
        {
            // Ferris Mode: X-Pattern Slash
            RPC_ExecuteXPatternSlash(transform.position);
        }

        GainRagePoints(15);

        nextSkill1Time = Time.time + finalCooldown;
        RPC_ShowSkillEffect("SavageStrike");
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ExecuteXPatternSlash(Vector3 position)
    {
        StartCoroutine(ExecuteXPatternSlash(position));
    }

    // ✅ Execute X-Pattern Slash (4 ทิศทาง)
    private IEnumerator ExecuteXPatternSlash(Vector3 position)
    {
        float damageMultiplier = 2.5f; // Ferris Mode damage
        int waveCount = 4; // 4 ทิศทาง (X pattern)
        float waveInterval = 0.15f; // ความเร็วในการยิงแต่ละทิศ

        Debug.Log($"⚔️ [X-Pattern Slash] Starting 4-directional attack!");

        // 4 ทิศทางของ X (45°, 135°, 225°, 315°)
        Vector3[] directions = new Vector3[]
        {
        new Vector3(1, 0, 1).normalized,   // ขวาบน (45°)
        new Vector3(-1, 0, 1).normalized,  // ซ้ายบน (135°)
        new Vector3(-1, 0, -1).normalized, // ซ้ายล่าง (225°)
        new Vector3(1, 0, -1).normalized   // ขวาล่าง (315°)
        };

        for (int i = 0; i < waveCount; i++)
        {
            Vector3 slashDirection = directions[i];

            // 🎨 แสดง particle effect สำหรับแต่ละทิศทาง
            RPC_ShowXSlashEffect(position, slashDirection, i + 1);

            // Hit enemies ในทิศทางนั้นๆ
            StartCoroutine(ExecuteExpandingSlash(position, slashDirection, damageMultiplier));

            // รอก่อนยิงทิศต่อไป
            yield return new WaitForSeconds(waveInterval);
        }

        Debug.Log($"⚔️ [X-Pattern Slash] Complete!");
    }

    // ✅ ทำดาเมจแบบ expanding (บาวออกไป)
    private IEnumerator ExecuteExpandingSlash(Vector3 startPos, Vector3 direction, float damageMultiplier)
    {
        float maxDistance = ferrisXPatternRadius; // 8m
        float currentDistance = 0f;
        float expandSpeed = 15f; // ความเร็วในการขยาย
        float checkRadius = 2f; // รัศมีการเช็คศัตรู

        List<Character> hitEnemies = new List<Character>(); // เก็บศัตรูที่โดนแล้ว

        while (currentDistance < maxDistance)
        {
            currentDistance += expandSpeed * Time.deltaTime;
            currentDistance = Mathf.Min(currentDistance, maxDistance);

            Vector3 currentPos = startPos + direction * currentDistance;

            // เช็คศัตรู ณ ตำแหน่งปัจจุบัน
            Collider[] enemies = Physics.OverlapSphere(currentPos, checkRadius, LayerMask.GetMask("Enemy"));

            foreach (Collider col in enemies)
            {
                Character enemy = col.GetComponent<Character>();
                if (enemy != null && !hitEnemies.Contains(enemy))
                {
                    hitEnemies.Add(enemy);

                    int damage = GetScaledSkillDamageWithAmp(damageMultiplier);
                    enemy.TakeDamageFromAttacker(0, damage, this, DamageType.Normal, false);

                    Debug.Log($"⚔️ X-Slash hit {enemy.CharacterName}: {damage} damage");
                }
            }

            yield return null;
        }

        Debug.Log($"⚔️ Expanding slash finished! Hit {hitEnemies.Count} enemies");
    }

    // ✅ RPC สำหรับแสดง particle X-Pattern
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowXSlashEffect(Vector3 position, Vector3 direction, int slashNumber)
    {
        if (savageStrikeEffect == null) return;

        // สร้าง trail effect ที่บาวออกไป
        StartCoroutine(AnimateExpandingXSlash(position, direction, slashNumber));
    }

    // ✅ Animate expanding X-Slash particle
    private IEnumerator AnimateExpandingXSlash(Vector3 startPos, Vector3 direction, int slashNumber)
    {
        float maxDistance = ferrisXPatternRadius;
        float currentDistance = 0f;
        float expandSpeed = 15f;

        // สร้าง particle object
        GameObject slashEffect = Instantiate(savageStrikeEffect.gameObject, startPos, Quaternion.identity);

        ParticleSystem ps = slashEffect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ParticleSystem.MainModule main = ps.main;

            // สีตามลำดับ
            Color[] colors = new Color[]
            {
            new Color(1f, 0.9f, 0.3f, 0.9f),  // เหลือง
            new Color(1f, 0.5f, 0f, 0.95f),   // ส้ม
            new Color(1f, 0.1f, 0f, 1f),      // แดง
            new Color(1f, 0f, 0.5f, 1f)       // แดงชมพู
            };

            main.startColor = colors[slashNumber - 1];
            main.startSize = 2f;
            main.startLifetime = 1.5f;

            // ทำให้ particle ยิงออกไปตามทิศทาง
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f; // มุมแคบ
            shape.rotation = Quaternion.LookRotation(direction).eulerAngles;
        }

        // เคลื่อนที่ particle ออกไปตามทิศทาง
        while (currentDistance < maxDistance)
        {
            currentDistance += expandSpeed * Time.deltaTime;
            currentDistance = Mathf.Min(currentDistance, maxDistance);

            Vector3 currentPos = startPos + direction * currentDistance;
            slashEffect.transform.position = currentPos;
            slashEffect.transform.LookAt(currentPos + direction);

            yield return null;
        }

        // หยุด particle และทำลาย
        if (ps != null)
        {
            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
        }

        Destroy(slashEffect, 1f);

        Debug.Log($"⚔️ X-Slash particle [{slashNumber}] animated!");
    }
    // ใน Berserker.cs - แทนที่ RPC_ExecuteSavageStrike

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ExecuteSavageStrike(Vector3 position, Vector3 direction)
    {
        if (IsInBerserkMode)
        {
            // Berserk Mode: ทุบ 3 ครั้งต่อเนื่อง
            StartCoroutine(ExecuteSavageStrikeCombo(position, direction));
        }
        else
        {
            // Normal Mode: ทุบครั้งเดียว (โค้ดเดิม)
            ExecuteSingleSavageStrike(position, direction);
        }
    }

    private IEnumerator ExecuteSavageStrikeCombo(Vector3 position, Vector3 direction)
    {
        float damageMultiplier = 2.0f;
        int hitCount = 3;
        float hitInterval = 0.3f;

        Debug.Log($"⚔️ [Savage Strike Combo] Starting 3-hit combo!");

        for (int i = 0; i < hitCount; i++)
        {
            // 🎨 แสดง particle effect สำหรับแต่ละครั้งที่ทุบ
            RPC_ShowComboHitEffect(i + 1);

            // หา main target
            Character mainTarget = FindNearestEnemy(position, 3f);

            if (mainTarget != null)
            {
                int damage = GetScaledSkillDamageWithAmp(damageMultiplier);
                mainTarget.TakeDamageFromAttacker(0, damage, this, DamageType.Normal, false);

                // Knockback เฉพาะครั้งสุดท้าย
                if (i == hitCount - 1)
                {
                    Vector3 knockbackDir = (mainTarget.transform.position - position).normalized;
                    mainTarget.ApplyKnockback(knockbackDir, 3f);
                }

                Debug.Log($"⚔️ Savage Strike [{i + 1}/3] hit {mainTarget.CharacterName}: {damage} damage");
            }

            // Cleave AOE
            ExecuteCleaveAOE(position, direction, damageMultiplier * 0.6f, mainTarget);

            // รอก่อนทุบครั้งต่อไป
            if (i < hitCount - 1)
            {
                yield return new WaitForSeconds(hitInterval);
            }
        }

        Debug.Log($"⚔️ Savage Strike combo complete!");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowComboHitEffect(int hitNumber)
    {
        if (savageStrikeEffect == null) return;

        // สร้าง particle effect หน้าตัวละคร
        Vector3 effectPosition = transform.position + transform.forward * 1.5f + Vector3.up * 1f;
        GameObject effect = Instantiate(savageStrikeEffect.gameObject, effectPosition, Quaternion.identity);

        ParticleSystem ps = effect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ParticleSystem.MainModule main = ps.main;

            // สีเปลี่ยนตามลำดับการทุบ
            switch (hitNumber)
            {
                case 1:
                    main.startColor = new Color(1f, 0.9f, 0.3f, 0.9f); // เหลือง
                    main.startSize = 1.5f;
                    break;
                case 2:
                    main.startColor = new Color(1f, 0.5f, 0f, 0.95f); // ส้ม
                    main.startSize = 2f;
                    break;
                case 3:
                    main.startColor = new Color(1f, 0.1f, 0f, 1f); // แดง
                    main.startSize = 2.5f; // ใหญ่ที่สุด
                    break;
            }

            main.startLifetime = 0.8f;

            // เพิ่มความเร็วให้ particle
            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.speedModifier = 2f + hitNumber; // เร็วขึ้นทุกครั้ง
        }

        // Play sound
        AudioManager.instance?.PlaySFX(21, 0.3f + (hitNumber * 0.1f)); // เสียงดังขึ้นทุกครั้ง

        Destroy(effect, 1.5f);

        Debug.Log($"⚔️ [Combo Hit {hitNumber}] Particle effect spawned!");
    }
    private void ExecuteSingleSavageStrike(Vector3 position, Vector3 direction)
    {
        // โค้ดเดิม (Normal Mode)
        float damageMultiplier = 1.2f;
        float cleaveMultiplier = 0.6f;

        // Main target
        Character mainTarget = FindNearestEnemy(position, 3f);

        if (mainTarget != null)
        {
            int damage = GetScaledSkillDamageWithAmp(damageMultiplier);
            mainTarget.TakeDamageFromAttacker(0, damage, this, DamageType.Normal, false);

            Debug.Log($"🪓 Savage Strike hit {mainTarget.CharacterName}: {damage} damage");
        }

        // Cleave AOE
        ExecuteCleaveAOE(position, direction, damageMultiplier * cleaveMultiplier, mainTarget);
    }

    private Character FindNearestEnemy(Vector3 position, float range)
    {
        Collider[] targets = Physics.OverlapSphere(position, range, LayerMask.GetMask("Enemy"));
        Character nearestEnemy = null;
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
                    nearestEnemy = enemy;
                }
            }
        }

        return nearestEnemy;
    }

    private void ExecuteCleaveAOE(Vector3 position, Vector3 direction, float damageMultiplier, Character mainTarget)
    {
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
                    int cleaveDamage = GetScaledSkillDamageWithAmp(damageMultiplier);
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

    // ใน Berserker.cs - แทนที่ TryUseSkill2



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

        // Execute Dash
        Vector3 dashDirection = GetDashDirection();
        RPC_ExecuteDash(transform.position, dashDirection);

        GainRagePoints(12);

        nextSkill2Time = Time.time + finalCooldown;
        RPC_ShowSkillEffect("Whirlwind");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ExecuteDash(Vector3 startPosition, Vector3 direction)
    {
        StartCoroutine(ExecuteDash(startPosition, direction));
    }

    private IEnumerator ExecuteDash(Vector3 startPosition, Vector3 direction)
    {
        // ✅ ใช้ระยะใหม่
        float actualDashDistance = IsInBerserkMode ? ferrisDashDistance : normalDashDistance; // 5m / 7m
        float damageRadius = IsInBerserkMode ? 6f : 4f;
        float damageDuration = IsInBerserkMode ? 2f : 1f;
        float damageMultiplier = IsInBerserkMode ? 1.5f : 1.0f;
        float dashSpeed = 20f;

        Debug.Log($"🌪️ [Dash] Distance: {actualDashDistance}m (Ferris: {IsInBerserkMode})");

        // ตรวจสอบกำแพง
        RaycastHit wallHit;
        if (Physics.Raycast(startPosition, direction, out wallHit, actualDashDistance,
            LayerMask.GetMask("Wall", "Obstacle", "Default")))
        {
            actualDashDistance = Mathf.Max(0.5f, wallHit.distance - 0.5f);
            Debug.Log($"⚠️ Wall detected! Reducing dash distance to {actualDashDistance}m");
        }

        Vector3 endPosition = startPosition + direction * actualDashDistance;
        float dashTime = actualDashDistance / dashSpeed;

        // 🔥 Dash phase
        float elapsed = 0f;

        // 🆕 สร้าง trail particles (Front + Back) สำหรับ Ferris Mode
        GameObject frontTrail = null;
        GameObject backTrail = null;

        if (IsInBerserkMode && whirlwindEffect != null)
        {
            frontTrail = Instantiate(whirlwindEffect.gameObject, transform.position, Quaternion.identity);
            ConfigureTrailParticle(frontTrail, new Color(1f, 0.3f, 0f, 0.9f), 2f);

            backTrail = Instantiate(whirlwindEffect.gameObject, transform.position, Quaternion.identity);
            ConfigureTrailParticle(backTrail, new Color(1f, 0.8f, 0f, 0.7f), 1.5f);

            Debug.Log($"🔥 [Ferris Dash] Created front and back trail particles!");
        }

        while (elapsed < dashTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / dashTime;

            Vector3 currentPosition = Vector3.Lerp(startPosition, endPosition, progress);

            // ตรวจสอบกำแพงระหว่าง dash
            Vector3 moveVector = currentPosition - transform.position;
            if (moveVector.magnitude > 0.1f)
            {
                if (Physics.Raycast(transform.position, moveVector.normalized, moveVector.magnitude,
                    LayerMask.GetMask("Wall", "Obstacle", "Default")))
                {
                    Debug.LogWarning("🛑 Wall collision during dash! Stopping.");
                    break;
                }
            }

            // Move
            if (HasInputAuthority && rb != null)
            {
                rb.MovePosition(currentPosition);
                NetworkedPosition = currentPosition;
            }
            else
            {
                transform.position = currentPosition;
            }

            // อัพเดทตำแหน่ง trail particles
            if (IsInBerserkMode)
            {
                if (frontTrail != null)
                {
                    Vector3 frontPos = currentPosition + direction * 1.5f;
                    frontTrail.transform.position = frontPos;
                }

                if (backTrail != null)
                {
                    Vector3 backPos = currentPosition - direction * 1.5f;
                    backTrail.transform.position = backPos;
                }
            }

            yield return null;
        }

        // ตั้งตำแหน่งสุดท้าย
        if (HasInputAuthority && rb != null)
        {
            rb.MovePosition(endPosition);
            NetworkedPosition = endPosition;
        }
        else
        {
            transform.position = endPosition;
        }

        // ทำลาย trail particles
        if (frontTrail != null)
        {
            StopAndDestroyParticle(frontTrail, 1f);
        }

        if (backTrail != null)
        {
            StopAndDestroyParticle(backTrail, 1f);
        }

        // 🔥 AOE Damage phase
        float damageElapsed = 0f;
        float damageInterval = 0.5f;
        float nextDamageTime = 0f;


        while (damageElapsed < damageDuration)
        {
            damageElapsed += Time.deltaTime;

            if (damageElapsed >= nextDamageTime)
            {
                Collider[] enemies = Physics.OverlapSphere(transform.position, damageRadius, LayerMask.GetMask("Enemy"));

                foreach (Collider col in enemies)
                {
                    Character enemy = col.GetComponent<Character>();
                    if (enemy != null)
                    {
                        int damage = GetScaledSkillDamageWithAmp(damageMultiplier);
                        enemy.TakeDamageFromAttacker(0, damage, this, DamageType.Normal, false);

                        Debug.Log($"🌪️ Dash AOE hit {enemy.CharacterName}: {damage} damage");
                    }
                }

                nextDamageTime = damageElapsed + damageInterval;
            }

            yield return null;
        }

        Debug.Log($"🌪️ Dash complete!");
    }
    // 🆕 Helper method สำหรับตั้งค่า trail particle
    private void ConfigureTrailParticle(GameObject trailObject, Color color, float size)
    {
        if (trailObject == null) return;

        ParticleSystem ps = trailObject.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ParticleSystem.MainModule main = ps.main;
            main.startColor = color;
            main.startLifetime = 1.5f;
            main.startSize = size;
            main.loop = true;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 50f; // หนาแน่น

            // ทำให้ particle ไหลตามการเคลื่อนไหว
            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.speedModifier = 3f;
        }
    }

    // 🆕 Helper method สำหรับหยุดและทำลาย particle
    private void StopAndDestroyParticle(GameObject particleObject, float destroyDelay)
    {
        if (particleObject == null) return;

        ParticleSystem ps = particleObject.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ParticleSystem.MainModule main = ps.main;
            main.loop = false; // หยุด loop
        }

        Destroy(particleObject, destroyDelay);
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

    // ใน Berserker.cs - แทนที่ TryUseSkill3 และ RPC_ExecuteWarCry

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

        // Execute Cleanse
        RPC_ExecuteCleanse(transform.position);

        GainRagePoints(20);

        nextSkill3Time = Time.time + finalCooldown;
        RPC_ShowSkillEffect("WarCry"); // ใช้ effect เดิม
    }

    // ใน Berserker.cs - แก้ไข RPC_ExecuteCleanse

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ExecuteCleanse(Vector3 position)
    {
        float immunityDuration = IsInBerserkMode ? 10f : 7f;
        float healPercent = IsInBerserkMode ? 0.20f : 0.10f;
        float knockbackRadius = IsInBerserkMode ? 8f : 6f; // รัศมี knockback
        float knockbackForce = IsInBerserkMode ? 15f : 10f; // แรง knockback

        // ล้าง debuff ทั้งหมด
        StatusEffectManager statusMgr = GetComponent<StatusEffectManager>();
        if (statusMgr != null)
        {
            statusMgr.ClearAllStatusEffects();
            Debug.Log($"🧹 [Cleanse] {CharacterName} cleared all debuffs!");
        }

        // ให้ immunity
        if (statusMgr != null)
        {
            statusMgr.ApplyDebuffImmunity(immunityDuration);
            Debug.Log($"🛡️ [Immunity] {CharacterName} immune for {immunityDuration}s");
        }

        // Heal
        int healAmount = Mathf.RoundToInt(MaxHp * healPercent);
        Heal(healAmount);

        // ✅ Knockback enemies ใกล้ๆ
        Collider[] enemies = Physics.OverlapSphere(position, knockbackRadius, LayerMask.GetMask("Enemy"));

        foreach (Collider col in enemies)
        {
            Character enemy = col.GetComponent<Character>();
            if (enemy != null)
            {
                Vector3 knockbackDir = (enemy.transform.position - position).normalized;
                enemy.ApplyKnockback(knockbackDir, knockbackForce, 1.5f);
                Debug.Log($"🛡️ [War Cry Knockback] {enemy.CharacterName} knocked back!");
            }
        }

        Debug.Log($"🛡️ [Cleanse] {CharacterName}: Cleared debuffs + {immunityDuration}s immunity + {healAmount} HP + Knockback {enemies.Length} enemies");
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
            // Berserk Mode: Titan Rampage with Teleport
            RPC_ExecuteTitanRampageWithTeleport();

            Debug.Log($"⚡ [Skill 4 Berserk] {CharacterName}: Titan Rampage with Teleport");
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

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ExecuteTitanRampageWithTeleport()
    {
        StartCoroutine(ExecuteTitanRampageWithTeleport());
    }

    private IEnumerator ExecuteGroundSlam(Vector3 position)
    {
        // 📹 Camera Shake เมื่อทุบพื้น
        RPC_TriggerCameraShake();

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

    /// <summary>
    /// Titan Rampage พร้อมระบบ Teleport เพื่อแก้ปัญหาการติดพื้น
    /// </summary>
    /// <summary>
    /// Titan Rampage แบบขยายร่าง ไม่กระโดด
    /// </summary>
    /// <summary>
    /// Titan Rampage แบบไม่ขยายตัว ไม่กระโดด - เน้นกล้องสั่น + Knockback
    /// </summary>
    private IEnumerator ExecuteTitanRampageWithTeleport()
    {
        IsInTitanForm = true;

        float duration = isPhysicalBuild ? 10f : 8f;
        TitanFormEndTime = Time.time + duration;

        Debug.Log($"⚡ [Titan Rampage] Started! Duration: {duration}s");

        // 💥 Ground Slam Impact + Camera Shake + Knockback
        RPC_TriggerCameraShake();

        float impactRadius = 10f;
        float knockbackForce = 15f; // แรง knockback เท่า War Cry
        Collider[] impactEnemies = Physics.OverlapSphere(transform.position, impactRadius, LayerMask.GetMask("Enemy"));

        foreach (Collider col in impactEnemies)
        {
            Character enemy = col.GetComponent<Character>();
            if (enemy != null)
            {
                int impactDamage = GetScaledSkillDamageWithAmp(3.5f);
                enemy.TakeDamageFromAttacker(0, impactDamage, this, DamageType.Critical, false);
                enemy.ApplyStatusEffect(StatusEffectType.Stun, 0, 1.5f);

                // Knockback เหมือนท่า 3
                Vector3 knockbackDir = (enemy.transform.position - transform.position).normalized;
                enemy.ApplyKnockback(knockbackDir, knockbackForce, 1.5f);

                Debug.Log($"⚡ Ground Slam Impact hit {enemy.CharacterName}: {impactDamage} damage + Knockback");
            }
        }

        // 🌋 Earthquake Zone
        Debug.Log($"⚡ [Titan Rampage] Earthquake Zone for {duration}s");

        float tickInterval = 1f;
        float nextTick = 0f;
        float elapsed = 0f;
        int totalEnemiesHit = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= nextTick)
            {
                Collider[] enemies = Physics.OverlapSphere(transform.position, earthquakeZoneRadius, LayerMask.GetMask("Enemy"));

                foreach (Collider col in enemies)
                {
                    Character enemy = col.GetComponent<Character>();
                    if (enemy != null)
                    {
                        int damage = GetScaledSkillDamageWithAmp(0.8f);
                        enemy.TakeDamageFromAttacker(0, damage, this, DamageType.Normal, false);
                        enemy.ApplyStatusEffect(StatusEffectType.Freeze, 0, tickInterval + 0.5f);

                        if (!isPhysicalBuild)
                        {
                            int healAmount = Mathf.RoundToInt(damage * 0.05f);
                            Heal(healAmount);
                        }

                        totalEnemiesHit++;
                    }
                }

                nextTick += tickInterval;
            }

            yield return null;
        }

        // 💥 Finale: Explosion
        Debug.Log($"⚡ [Titan Rampage] Finale! Explosion!");

        float explosionRadius = 10f;
        float explosionMultiplier = isPhysicalBuild ? 4.5f : 3.0f;

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

        IsInTitanForm = false;
        Debug.Log($"⚡ [Titan Rampage] Ended! Total enemies hit: {totalEnemiesHit}");
    }

    #endregion

    #region Visual Effects RPCs

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateRageAuraColor(float ferrisPoints) // เปลี่ยนชื่อ parameter
    {
        if (rageAuraEffect == null) return;

        ParticleSystem.MainModule main = rageAuraEffect.main;
        Color targetColor;

        if (ferrisPoints < 33f)
        {
            targetColor = lightYellowColor;
        }
        else if (ferrisPoints < 66f)
        {
            targetColor = orangeColor;
        }
        else if (ferrisPoints < 100f)
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
        emission.rateOverTime = ferrisPoints * 0.5f; // 0-50 particles/s
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
    private void RPC_TriggerCameraShake()
    {
        // เฉพาะ local player เท่านั้นที่จะเห็นกล้องสั่น
        if (HasInputAuthority)
        {
            StartCoroutine(CameraShake());
        }
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
            case "BasicAttack":
                ShowBasicAttackEffect();
                break;
        }
    }

    private void ShowSavageStrikeEffect()
    {
        AudioManager.instance.PlaySFX(21, 0.5f);

        if (savageStrikeEffect != null)
        {
            GameObject effect = Instantiate(savageStrikeEffect.gameObject, transform.position + transform.forward * 1.5f, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    private void ShowWhirlwindEffect()
    {
        AudioManager.instance.PlaySFX(22, 0.5f);

        if (whirlwindEffect != null)
        {
            GameObject effect = Instantiate(whirlwindEffect.gameObject, transform.position, Quaternion.identity);
            effect.transform.SetParent(transform); // Follow character
            Destroy(effect, 3f);
        }
    }

    private void ShowWarCryEffect()
    {
        AudioManager.instance.PlaySFX(23, 0.5f);

        if (warCryEffect != null)
        {
            GameObject effect = Instantiate(warCryEffect.gameObject, transform.position + Vector3.up * 1f, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    private void ShowTitanWrathEffect()
    {
        AudioManager.instance.PlaySFX(24, 0.5f);

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

            // when the berserker lands on the ground
        }
    }

    // ใน Berserker.cs - แทนที่ ShowBasicAttackEffect()

    private void ShowBasicAttackEffect()
    {
        AudioManager.instance.PlaySFX(20, 0.5f);

        if (basicAttackEffect != null)
        {
            // ✅ แสดง particle ที่ตำแหน่งด้านหน้าตัวละคร (เหมือน Assassin)
            Vector3 effectPosition = transform.position + Vector3.up * 0.5f;
            GameObject effect = Instantiate(basicAttackEffect.gameObject, effectPosition, Quaternion.identity);

            // ✅ Flip effect ตามทิศทางของตัวละคร (2D Sprite)
            Vector3 effectScale = effect.transform.localScale;
            if (transform.localScale.x < 0)
            {
                // ถ้าตัวละครหันซ้าย (scale.x เป็นลบ)
                effectScale.x = -Mathf.Abs(effectScale.x);
            }
            else
            {
                // ถ้าตัวละครหันขวา (scale.x เป็นบวก)
                effectScale.x = Mathf.Abs(effectScale.x);
            }
            effect.transform.localScale = effectScale;

            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;

                // ✅ สีตาม Ferris Points (แดง-ส้ม-เหลือง)
                if (CurrentFerrisPoint < 33f)
                {
                    main.startColor = new Color(1f, 0.9f, 0.7f, 0.8f); // เหลืองอ่อน
                }
                else if (CurrentFerrisPoint < 66f)
                {
                    main.startColor = new Color(1f, 0.6f, 0.2f, 0.9f); // ส้ม
                }
                else if (CurrentFerrisPoint < 100f)
                {
                    main.startColor = new Color(1f, 0.3f, 0.1f, 1f); // แดงส้ม
                }
                else
                {
                    main.startColor = new Color(0.8f, 0f, 0f, 1f); // แดงเข้ม
                }

                main.startLifetime = 0.5f;
            }

            Destroy(effect, 1f);
        }
    }

    #endregion

    #region Override Methods

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        // Drain Ferris Points during Berserk Mode
        if (HasStateAuthority && IsInBerserkMode)
        {
            DrainRagePoints(Runner.DeltaTime);
        }
        if (HasStateAuthority)
        {
            UpdateReflectDamage();
        }
    }
    private void UpdateReflectDamage()
    {
        if (IsInBerserkMode)
        {
            ReflectDamagePercent = 0.30f * 2f; // 60%
        }
        else
        {
            ReflectDamagePercent = 0.30f * 0.5f; // 15%
        }

        // Debug.Log($"⚡ [Reflect] {CharacterName}: {ReflectDamagePercent * 100f:F0}% (Berserk: {IsInBerserkMode})");
    }
    protected override void RPC_OnAttackHit(NetworkObject enemyObject)
    {
        base.RPC_OnAttackHit(enemyObject);

        // Show basic attack effect
        RPC_ShowSkillEffect("BasicAttack");

        // Check for critical hit to gain extra Rage
        // TODO: Implement critical detection and give +5 Rage
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_PerformAttack(NetworkObject enemyObject)
    {
        if (enemyObject != null)
        {
            Character enemy = enemyObject.GetComponent<Character>();
            if (enemy != null)
            {
                // ✅ เพิ่ม Knockback 100% เหมือน Assassin
                Vector3 knockbackDir = (enemy.transform.position - transform.position).normalized;
                enemy.ApplyKnockback(knockbackDir, 10f, 0.7f); // 10 force, 0.7s duration
                Debug.Log($"🪓 [Basic Attack Knockback] {enemy.CharacterName} knocked back!");

                // ✅ FIXED: Basic attack ไม่ใช้ Amp Damage - ระบุ isBasicAttack = true
                enemy.TakeDamageFromAttacker(AttackDamage, MagicDamage, this, DamageType.Normal, true);
                RPC_OnAttackHit(enemyObject);
                Debug.Log($"[Basic Attack] {CharacterName} performed basic attack (no Amp Damage)");
            }
            else
            {
                // fallback สำหรับ NetworkEnemy เก่า
                NetworkEnemy networkEnemy = enemyObject.GetComponent<NetworkEnemy>();
                if (networkEnemy != null && !networkEnemy.IsDead)
                {
                    // ✅ Knockback สำหรับ NetworkEnemy ด้วย
                    Vector3 knockbackDir = (networkEnemy.transform.position - transform.position).normalized;
                    networkEnemy.GetComponent<Character>()?.ApplyKnockback(knockbackDir, 10f, 0.7f);

                    networkEnemy.TakeDamageFromAttacker(AttackDamage, this, DamageType.Normal);
                    RPC_OnAttackHit(enemyObject);
                }
            }
        }
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

    /// <summary>
    /// Camera Shake Effect สำหรับ Ground Slam Impact
    /// </summary>
    private IEnumerator CameraShake()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) yield break;

        Vector3 originalPos = mainCam.transform.position;
        float shakeDuration = 0.5f; // ระยะเวลาสั่น
        float shakeIntensity = 0.3f; // ความแรงของการสั่น
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;

            // ลดความแรงของการสั่นตามเวลา (ease-out)
            float strength = shakeIntensity * (1f - (elapsed / shakeDuration));

            Vector3 shakeOffset = Random.insideUnitSphere * strength;
            shakeOffset.y *= 0.5f; // ลดการสั่นในแนวตั้ง

            mainCam.transform.position = originalPos + shakeOffset;

            yield return null;
        }

        // คืนตำแหน่งกล้องกลับเดิม
        mainCam.transform.position = originalPos;
    }
    private void ResetAllSkillCooldowns()
    {
        if (!HasStateAuthority) return;

        nextSkill1Time = 0f;
        nextSkill2Time = 0f;
        nextSkill3Time = 0f;
        nextSkill4Time = 0f;

        Debug.Log($"🌙 [Shadow Mode] All skill cooldowns RESET! Ready for combo!");

        // แจ้ง UI


    }


    #endregion
}