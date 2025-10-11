using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class BloodKnight : Hero
{
    #region Blood Knight Properties
    [Header("🩸 Blood Knight Settings")]
    [SerializeField] private int skill1ManaCost = 15;
    [SerializeField] private int skill2ManaCost = 20;
    [SerializeField] private int skill3ManaCost = 15;
    [SerializeField] private int skill4ManaCost = 40;

    [Header("🩸 Blood Points System")]
    [Networked] public float BloodPoints { get; set; }
    [Networked] public bool IsInBloodTrance { get; set; }
    private const float MAX_BLOOD_POINTS = 100f;
    private const float BLOOD_TRANCE_DRAIN_RATE = 10f; // 10 points per second

    [Header("🩸 Blood Edge (Skill 1) Settings")]
    [Networked] public int BloodEdgeStacks { get; set; }
    [Networked] public float BloodEdgeAttackBonus { get; set; }
    [Networked] public float BloodEdgeLifeSteal { get; set; }
    private int bloodEdgeHitCount = 0;

    [Header("🩸 Blood Pool (Skill 3) Settings")]
    private GameObject currentBloodPool;
    public GameObject bloodPoolPrefab;
    public GameObject cursedPoolPrefab;

    [Header("🩸 Blood Storm (Skill 4) Settings")]
    [Networked] public bool IsInBloodStorm { get; set; }
    [Networked] public float BloodStormEndTime { get; set; }
    public GameObject bloodStormPrefab;
    private GameObject currentBloodStorm;

    [Header("🎨 Blood Knight Visual Effects")]
    [SerializeField] private ParticleSystem bloodAuraEffect;           // Blood Points aura
    [SerializeField] private ParticleSystem bloodTranceBurstEffect;    // Blood Trance activation
    [SerializeField] private ParticleSystem bloodDripEffect;           // Passive blood drip
    [SerializeField] private ParticleSystem bloodEdgeEffect;           // Skill 1 effect
    [SerializeField] private ParticleSystem crimsonLanceEffect;        // Skill 2 effect
    [SerializeField] private ParticleSystem bloodExplosionEffect;      // Skill 4 finale

    [Header("🎨 Blood Aura Color Settings")]
    [SerializeField] private Color lightRedColor = new Color(1f, 0.6f, 0.6f, 0.5f);      // 0-33
    [SerializeField] private Color mediumRedColor = new Color(1f, 0.3f, 0.3f, 0.7f);     // 34-66
    [SerializeField] private Color darkRedColor = new Color(0.8f, 0.1f, 0.1f, 0.9f);     // 67-99
    [SerializeField] private Color crimsonColor = new Color(0.6f, 0f, 0f, 1f);           // 100

    [Header("🔱 Build Settings")]
    [SerializeField] private bool isPhysicalBuild = true; // true = Physical, false = Magic

    private float minHpPercent = 0.01f; // 1% HP minimum
    #endregion

    protected override void Start()
    {
        base.Start();

        // Set attack type based on build
        AttackType = isPhysicalBuild ? AttackType.Physical : AttackType.Magic;

        // Set cooldowns
        skill1Cooldown = 10f;
        skill2Cooldown = 7f;
        skill3Cooldown = 12f;
        skill4Cooldown = 20f;

        // Initialize Blood Points
        BloodPoints = 0f;
        IsInBloodTrance = false;

        Debug.Log($"🩸 Blood Knight {CharacterName} initialized! Build: {(isPhysicalBuild ? "Physical" : "Magic")}");
    }

    #region Blood Points System

    /// <summary>
    /// เพิ่ม Blood Points
    /// </summary>
    private void GainBloodPoints(int amount)
    {
        if (IsInBloodTrance) return; // ไม่เพิ่มระหว่าง Blood Trance

        float oldPoints = BloodPoints;
        BloodPoints = Mathf.Min(BloodPoints + amount, MAX_BLOOD_POINTS);

        Debug.Log($"🩸 [Blood Points] {CharacterName}: {oldPoints:F0} → {BloodPoints:F0} (+{amount})");

        // Update visual
        UpdateBloodAuraVisual();

        // Check if should enter Blood Trance
        if (BloodPoints >= MAX_BLOOD_POINTS && !IsInBloodTrance)
        {
            EnterBloodTrance();
        }
    }

    /// <summary>
    /// ลด Blood Points (ระหว่าง Blood Trance)
    /// </summary>
    private void DrainBloodPoints(float deltaTime)
    {
        if (!IsInBloodTrance) return;

        BloodPoints = Mathf.Max(0f, BloodPoints - (BLOOD_TRANCE_DRAIN_RATE * deltaTime));

        Debug.Log($"🩸 [Blood Trance Drain] {CharacterName}: {BloodPoints:F0} points remaining");

        // Update visual
        UpdateBloodAuraVisual();

        // Check if should exit Blood Trance
        if (BloodPoints <= 0f)
        {
            ExitBloodTrance();
        }
    }

    /// <summary>
    /// เข้าสู่โหมด Blood Trance
    /// </summary>
    private void EnterBloodTrance()
    {
        if (!HasStateAuthority) return;

        IsInBloodTrance = true;
        BloodPoints = MAX_BLOOD_POINTS;

        Debug.Log($"🩸 [BLOOD TRANCE ACTIVATED] {CharacterName}");

        // Visual effect
        RPC_ShowBloodTranceBurst();
        RPC_ShowBloodTranceAura(true);
    }

    /// <summary>
    /// ออกจากโหมด Blood Trance
    /// </summary>
    private void ExitBloodTrance()
    {
        if (!HasStateAuthority) return;

        IsInBloodTrance = false;
        BloodPoints = 0f;

        Debug.Log($"🩸 [Blood Trance Ended] {CharacterName}");

        // Visual effect
        RPC_ShowBloodTranceAura(false);
    }

    /// <summary>
    /// อัปเดต Blood Aura visual ตาม Blood Points
    /// </summary>
    private void UpdateBloodAuraVisual()
    {
        RPC_UpdateBloodAuraColor(BloodPoints);
    }

    #endregion

    #region HP Cost System

    /// <summary>
    /// ตรวจสอบและใช้ HP เพื่อ cast skill
    /// </summary>
    private bool TryUseSkillWithHPCost(float hpCostPercent, bool useCurrentHP = true)
    {
        int hpCost;

        if (useCurrentHP)
        {
            hpCost = Mathf.RoundToInt(CurrentHp * hpCostPercent);
        }
        else
        {
            hpCost = Mathf.RoundToInt(MaxHp * hpCostPercent);
        }

        // Safety check: can't go below 1% HP
        int minHp = Mathf.Max(1, Mathf.RoundToInt(MaxHp * minHpPercent));

        if (CurrentHp - hpCost < minHp)
        {
            Debug.LogWarning($"❌ {CharacterName} not enough HP! Need {hpCost}, have {CurrentHp}, min is {minHp}");
            return false;
        }

        // Apply HP cost
        CurrentHp -= hpCost;
        CurrentHp = Mathf.Max(minHp, CurrentHp);

        if (HasStateAuthority)
        {
            NetworkedCurrentHp = CurrentHp;
        }

        Debug.Log($"🩸 [HP Cost] {CharacterName}: -{hpCost} HP ({CurrentHp}/{MaxHp} remaining)");

        return true;
    }

    /// <summary>
    /// คำนวณ HP Cost โดยพิจารณา build bonus
    /// </summary>
    private float GetAdjustedHPCost(float baseHPCost)
    {
        if (isPhysicalBuild)
        {
            return baseHPCost * 0.7f; // Physical Build: -30% HP Cost
        }
        return baseHPCost;
    }

    #endregion

    #region Damage Calculation

    /// <summary>
    /// คำนวณ Base Damage สำหรับ Blood Knight
    /// </summary>
    private int CalculateBloodKnightBaseDamage()
    {
        // Formula: ((AttackDamage * 0.8) + (MagicDamage * 0.2) + (Level * 10)) * (1 + (1 - CurrentHP / MaxHP))

        float attackPortion = AttackDamage * 0.8f;
        float magicPortion = MagicDamage * 0.2f;
        int levelBonus = GetCurrentLevel() * 10;

        float baseDamage = attackPortion + magicPortion + levelBonus;

        // Low HP multiplier
        float hpPercent = (float)CurrentHp / MaxHp;
        float lowHpMultiplier = 1f + (1f - hpPercent);

        int finalDamage = Mathf.RoundToInt(baseDamage * lowHpMultiplier);

        Debug.Log($"🩸 [Damage Calc] Base: {baseDamage:F0} × {lowHpMultiplier:F2} (HP: {hpPercent:P0}) = {finalDamage}");

        return finalDamage;
    }

    /// <summary>
    /// คำนวณ Bleed Damage
    /// </summary>
    private int CalculateBleedDamage(float multiplier)
    {
        int baseDamage = CalculateBloodKnightBaseDamage();
        int bleedDamage = Mathf.RoundToInt(baseDamage * multiplier);

        // Magic Build: Bleed scales with Magic Damage
        if (!isPhysicalBuild)
        {
            float magicScaling = MagicDamage * 0.1f; // +10% of Magic Damage
            bleedDamage += Mathf.RoundToInt(magicScaling);
        }

        return Mathf.Max(1, bleedDamage);
    }

    /// <summary>
    /// คำนวณ Skill Damage พร้อม Amp Damage
    /// </summary>
    private int GetScaledSkillDamageWithAmp(float multiplier)
    {
        int baseDamage = CalculateBloodKnightBaseDamage();
        int scaledDamage = Mathf.RoundToInt(baseDamage * multiplier);

        // Apply Amp Damage (skill attacks)
        int finalDamage = ApplyAmpDamageToSkill(scaledDamage);

        return finalDamage;
    }

    #endregion

    #region Blood Trance Bonuses

    /// <summary>
    /// ดึงค่า Attack Damage Bonus ใน Blood Trance
    /// </summary>
    private float GetBloodTranceDamageBonus()
    {
        if (!IsInBloodTrance) return 0f;

        if (isPhysicalBuild)
        {
            return 0.4f; // +40% Attack Damage (Physical Build)
        }
        else
        {
            return 0.15f; // +15% Attack Damage (base)
        }
    }

    /// <summary>
    /// ดึงค่า Magic Damage Bonus ใน Blood Trance
    /// </summary>
    private float GetBloodTranceMagicBonus()
    {
        if (!IsInBloodTrance) return 0f;

        if (!isPhysicalBuild)
        {
            return 0.4f; // +40% Magic Damage (Magic Build)
        }
        else
        {
            return 0.15f; // +15% Magic Damage (base)
        }
    }

    #endregion

    #region Skill 1: Blood Edge

    protected override void TryUseSkill1()
    {
        if (!CanUseSkill(skill1ManaCost)) return;

        // Cooldown check
        if (Time.time < nextSkill1Time)
        {
            Debug.Log($"❌ Skill 1 on cooldown! {nextSkill1Time - Time.time:F1}s remaining");
            return;
        }

        float effectiveReduction = GetEffectiveReductionCoolDown();
        float reductionMultiplier = 1f - (effectiveReduction / 100f);
        reductionMultiplier = Mathf.Clamp(reductionMultiplier, 0.1f, 1f);
        float finalCooldown = skill1Cooldown * reductionMultiplier;

        if (!IsInBloodTrance)
        {
            // ✅ Normal Mode
            float adjustedHPCost = GetAdjustedHPCost(0.15f);
            if (!TryUseSkillWithHPCost(adjustedHPCost, true)) return;

            UseMana(GetAdjustedManaCost(skill1ManaCost));
            GainBloodPoints(25);

            // Apply Blood Edge buff: 3 hits, +15% Attack Damage, 3rd hit heals 10%
            ApplyBloodEdgeBuff(3, 0.15f, 0.10f, false);

            Debug.Log($"🩸 [Skill 1 Normal] {CharacterName}: 3 hits with +15% Attack, 3rd hit heals 10%");
        }
        else
        {
            // ✅ Blood Trance Mode - ไม่เสีย Mana, ไม่เสีย HP, ไม่เพิ่ม Blood Points

            // Apply Blood Edge buff: 5 hits, +40% Attack Damage, every hit heals 15%
            ApplyBloodEdgeBuff(5, 0.40f, 0.15f, true);

            Debug.Log($"🩸 [Skill 1 Blood Trance] {CharacterName}: 5 hits with +40% Attack, every hit heals 15%");
        }

        nextSkill1Time = Time.time + finalCooldown;
        RPC_ShowSkillEffect("BloodEdge");
    }

    /// <summary>
    /// Apply Blood Edge Buff
    /// </summary>
    private void ApplyBloodEdgeBuff(int hitCount, float attackBonus, float lifeStealPercent, bool healEveryHit)
    {
        BloodEdgeStacks = hitCount;
        BloodEdgeAttackBonus = attackBonus;
        BloodEdgeLifeSteal = lifeStealPercent;
        bloodEdgeHitCount = 0;

        Debug.Log($"🩸 [Blood Edge Buff] Stacks: {hitCount}, Attack Bonus: +{attackBonus * 100}%, Life Steal: {lifeStealPercent * 100}%");
    }

    #endregion

    #region Skill 2: Crimson Lance

    protected override void TryUseSkill2()
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

        if (!IsInBloodTrance)
        {
            // ✅ Normal Mode
            float adjustedHPCost = GetAdjustedHPCost(0.20f);
            if (!TryUseSkillWithHPCost(adjustedHPCost, true)) return;

            UseMana(GetAdjustedManaCost(skill2ManaCost));
            GainBloodPoints(30);

            // Single spear forward
            Vector3 direction = GetDashDirection();
            RPC_ThrowCrimsonLance(direction, false);

            Debug.Log($"🩸 [Skill 2 Normal] {CharacterName}: Single spear");
        }
        else
        {
            // ✅ Blood Trance Mode - ไม่เสีย Mana, ไม่เสีย HP

            // 6 spears in all directions
            RPC_ThrowCrimsonBurst();

            Debug.Log($"🩸 [Skill 2 Blood Trance] {CharacterName}: Crimson Burst (6 spears)");
        }

        nextSkill2Time = Time.time + finalCooldown;
        RPC_ShowSkillEffect("CrimsonLance");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ThrowCrimsonLance(Vector3 direction, bool isBurst)
    {
        StartCoroutine(ExecuteCrimsonLance(direction, isBurst));
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ThrowCrimsonBurst()
    {
        StartCoroutine(ExecuteCrimsonBurst());
    }

    private IEnumerator ExecuteCrimsonLance(Vector3 direction, bool isBurst)
    {
        // Create projectile
        GameObject spear = CreateCrimsonSpear(transform.position, direction);

        float travelDistance = isBurst ? 8f : 12f;
        float travelSpeed = 15f;
        float travelTime = travelDistance / travelSpeed;
        float elapsed = 0f;

        Vector3 startPos = transform.position;
        List<Character> hitEnemies = new List<Character>();

        while (elapsed < travelTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / travelTime;

            Vector3 currentPos = startPos + direction * (travelDistance * progress);
            spear.transform.position = currentPos;

            // Check enemies hit
            Collider[] enemies = Physics.OverlapSphere(currentPos, 1.5f, LayerMask.GetMask("Enemy"));
            foreach (Collider col in enemies)
            {
                Character enemy = col.GetComponent<Character>();
                if (enemy != null && !hitEnemies.Contains(enemy))
                {
                    hitEnemies.Add(enemy);
                    ApplyCrimsonLanceDamage(enemy, isBurst);
                }
            }

            yield return null;
        }

        Destroy(spear);
        Debug.Log($"🩸 Crimson Lance hit {hitEnemies.Count} enemies");
    }

    private IEnumerator ExecuteCrimsonBurst()
    {
        Vector3[] directions = new Vector3[6];
        float angleStep = 360f / 6f;

        for (int i = 0; i < 6; i++)
        {
            float angle = i * angleStep;
            float rad = angle * Mathf.Deg2Rad;
            directions[i] = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)).normalized;
        }

        // Launch all 6 spears
        List<Coroutine> spearCoroutines = new List<Coroutine>();
        foreach (Vector3 dir in directions)
        {
            spearCoroutines.Add(StartCoroutine(ExecuteCrimsonLance(dir, true)));
        }

        // Wait for all spears to finish
        foreach (Coroutine coroutine in spearCoroutines)
        {
            yield return coroutine;
        }
    }

    private GameObject CreateCrimsonSpear(Vector3 position, Vector3 direction)
    {
        GameObject spear = new GameObject("CrimsonSpear");
        spear.transform.position = position;
        spear.transform.rotation = Quaternion.LookRotation(direction);

        // Add visual effect
        if (crimsonLanceEffect != null)
        {
            ParticleSystem ps = Instantiate(crimsonLanceEffect, spear.transform);
            ps.Play();
        }

        return spear;
    }

    private void ApplyCrimsonLanceDamage(Character enemy, bool isBurst)
    {
        int damage;
        int bleedDamage;

        if (!isBurst)
        {
            // Normal mode: Base × 0.8
            damage = GetScaledSkillDamageWithAmp(0.8f);
            bleedDamage = CalculateBleedDamage(0.3f);
        }
        else
        {
            // Burst mode: Base × 0.6
            damage = GetScaledSkillDamageWithAmp(0.6f);
            bleedDamage = CalculateBleedDamage(0.4f);

            // Reduce cooldown by 1s per enemy hit
            nextSkill2Time = Mathf.Max(Time.time, nextSkill2Time - 1f);
        }

        // Apply damage (skill attack - no life steal)
        enemy.TakeDamageFromAttacker(0, damage, this, DamageType.Magic, false);

        // Apply Bleed
        enemy.ApplyStatusEffect(StatusEffectType.Bleed, bleedDamage, 3f);

        Debug.Log($"🩸 Crimson Lance hit {enemy.CharacterName}: {damage} damage + {bleedDamage} Bleed/s");
    }

    #endregion

    #region Skill 3: Blood Pool

    protected override void TryUseSkill3()
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

        if (!IsInBloodTrance)
        {
            // ✅ Normal Mode
            float adjustedHPCost = GetAdjustedHPCost(0.20f);
            if (!TryUseSkillWithHPCost(adjustedHPCost, true)) return;

            UseMana(GetAdjustedManaCost(skill3ManaCost));
            GainBloodPoints(35);

            // Create Blood Pool
            RPC_CreateBloodPool(transform.position, false);

            Debug.Log($"🩸 [Skill 3 Normal] {CharacterName}: Blood Pool created");
        }
        else
        {
            // ✅ Blood Trance Mode

            // Create Cursed Pool
            RPC_CreateBloodPool(transform.position, true);

            Debug.Log($"🩸 [Skill 3 Blood Trance] {CharacterName}: Cursed Pool created");
        }

        nextSkill3Time = Time.time + finalCooldown;
        RPC_ShowSkillEffect("BloodPool");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_CreateBloodPool(Vector3 position, bool isCursed)
    {
        StartCoroutine(ExecuteBloodPool(position, isCursed));
    }

    private IEnumerator ExecuteBloodPool(Vector3 position, bool isCursed)
    {
        // Destroy old pool if exists
        if (currentBloodPool != null)
        {
            Destroy(currentBloodPool);
        }

        // Create pool
        GameObject poolPrefab = isCursed ? cursedPoolPrefab : bloodPoolPrefab;
        if (poolPrefab != null)
        {
            currentBloodPool = Instantiate(poolPrefab, position, Quaternion.identity);
        }
        else
        {
            // Fallback: create simple pool visual
            currentBloodPool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            currentBloodPool.transform.position = position;
            currentBloodPool.transform.localScale = new Vector3(6f, 0.1f, 6f); // 3m radius
            currentBloodPool.GetComponent<Renderer>().material.color = isCursed ? Color.black : Color.red;
        }

        float duration = 5f;
        float tickInterval = 1f;
        float nextTick = 0f;
        float elapsed = 0f;

        float poolRadius = 3f;

        Debug.Log($"🩸 [Blood Pool] Created at {position}, isCursed: {isCursed}");

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= nextTick)
            {
                // Check if player is in pool
                if (Vector3.Distance(transform.position, position) <= poolRadius)
                {
                    if (!isCursed)
                    {
                        // Normal Pool: Heal 1% HP/s, +15% Attack Speed
                        int healAmount = Mathf.RoundToInt(MaxHp * 0.01f);
                        Heal(healAmount);

                        // Attack Speed buff handled by status effect (could add here if needed)
                    }
                    else
                    {
                        // Cursed Pool: Heal 3% HP/s, +20% Magic Damage
                        int healAmount = Mathf.RoundToInt(MaxHp * 0.03f);
                        Heal(healAmount);
                    }
                }

                // Check enemies in pool (Cursed Pool only)
                if (isCursed)
                {
                    Collider[] enemies = Physics.OverlapSphere(position, poolRadius, LayerMask.GetMask("Enemy"));
                    foreach (Collider col in enemies)
                    {
                        Character enemy = col.GetComponent<Character>();
                        if (enemy != null)
                        {
                            int bleedDamage = CalculateBleedDamage(0.3f);
                            enemy.ApplyStatusEffect(StatusEffectType.Bleed, bleedDamage, 3f);
                        }
                    }
                }

                nextTick += tickInterval;
            }

            yield return null;
        }

        // Destroy pool
        if (currentBloodPool != null)
        {
            Destroy(currentBloodPool);
        }

        Debug.Log($"🩸 [Blood Pool] Ended");
    }

    #endregion

    #region Skill 4: Rite of the Blood Moon (Ultimate)

    protected override void TryUseSkill4()
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

        if (!IsInBloodTrance)
        {
            // ✅ Normal Mode - Uses Max HP (not Current HP)
            float adjustedHPCost = GetAdjustedHPCost(0.25f);
            if (!TryUseSkillWithHPCost(adjustedHPCost, false)) return; // false = use Max HP

            UseMana(GetAdjustedManaCost(skill4ManaCost));
            GainBloodPoints(40);

            // Blood Wave AOE
            RPC_CreateBloodWave(transform.position);

            Debug.Log($"🩸 [Skill 4 Normal] {CharacterName}: Blood Wave");
        }
        else
        {
            // ✅ Blood Trance Mode

            // Blood Storm
            RPC_CreateBloodStorm();

            Debug.Log($"🩸 [Skill 4 Blood Trance] {CharacterName}: Blood Storm");
        }

        nextSkill4Time = Time.time + finalCooldown;
        RPC_ShowSkillEffect("BloodMoon");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_CreateBloodWave(Vector3 position)
    {
        StartCoroutine(ExecuteBloodWave(position));
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_CreateBloodStorm()
    {
        StartCoroutine(ExecuteBloodStorm());
    }

    private IEnumerator ExecuteBloodWave(Vector3 position)
    {
        float radius = 6f;

        // Visual effect
        if (bloodExplosionEffect != null)
        {
            GameObject effect = Instantiate(bloodExplosionEffect.gameObject, position, Quaternion.identity);
            effect.transform.localScale = Vector3.one * radius;
            Destroy(effect, 3f);
        }

        // Damage enemies
        Collider[] enemies = Physics.OverlapSphere(position, radius, LayerMask.GetMask("Enemy"));

        foreach (Collider col in enemies)
        {
            Character enemy = col.GetComponent<Character>();
            if (enemy != null)
            {
                // Damage: Base × 1.0
                int damage = GetScaledSkillDamageWithAmp(1.0f);
                enemy.TakeDamageFromAttacker(0, damage, this, DamageType.Magic, false);

                // Apply Bleed: Base × 0.5/s for 5s
                int bleedDamage = CalculateBleedDamage(0.5f);
                enemy.ApplyStatusEffect(StatusEffectType.Bleed, bleedDamage, 5f);

                Debug.Log($"🩸 Blood Wave hit {enemy.CharacterName}: {damage} damage + {bleedDamage} Bleed/s");
            }
        }

        yield return null;
    }

    private IEnumerator ExecuteBloodStorm()
    {
        IsInBloodStorm = true;
        BloodStormEndTime = Time.time + 8f;

        Vector3 stormPosition = transform.position;

        // Create Blood Storm visual
        if (bloodStormPrefab != null)
        {
            currentBloodStorm = Instantiate(bloodStormPrefab, transform);
            currentBloodStorm.transform.localPosition = Vector3.zero;
        }
        else
        {
            // Fallback visual
            currentBloodStorm = new GameObject("BloodStorm");
            currentBloodStorm.transform.SetParent(transform);
            currentBloodStorm.transform.localPosition = Vector3.zero;
        }

        float duration = 8f;
        float tickInterval = 1f;
        float nextTick = 0f;
        float elapsed = 0f;
        float stormRadius = 6f;

        int totalEnemiesHit = 0;
        List<Character> hitEnemies = new List<Character>();

        Debug.Log($"🩸 [Blood Storm] Started! Duration: {duration}s");

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Update storm position to follow character
            stormPosition = transform.position;
            if (currentBloodStorm != null)
            {
                currentBloodStorm.transform.position = stormPosition;
            }

            if (elapsed >= nextTick)
            {
                // Damage enemies in storm every second
                Collider[] enemies = Physics.OverlapSphere(stormPosition, stormRadius, LayerMask.GetMask("Enemy"));

                foreach (Collider col in enemies)
                {
                    Character enemy = col.GetComponent<Character>();
                    if (enemy != null)
                    {
                        // Damage: Base × 0.6/s
                        int damage = GetScaledSkillDamageWithAmp(0.6f);
                        enemy.TakeDamageFromAttacker(0, damage, this, DamageType.Magic, false);

                        // Heal self 1% of damage dealt
                        int healAmount = Mathf.RoundToInt(damage * 0.01f);
                        Heal(healAmount);

                        // Check if enemy has Bleed → gain Blood Points (but won't increase during Blood Trance)
                        if (enemy.HasStatusEffect(StatusEffectType.Bleed))
                        {
                            // Double Bleed damage (Blood Resonance) - handled by status effect manager
                            Debug.Log($"🩸 [Blood Resonance] {enemy.CharacterName} Bleed damage doubled!");
                        }

                        if (!hitEnemies.Contains(enemy))
                        {
                            hitEnemies.Add(enemy);
                            totalEnemiesHit++;
                        }
                    }
                }

                nextTick += tickInterval;
            }

            yield return null;
        }

        // Blood Storm Finale: Explosion
        Debug.Log($"🩸 [Blood Storm] Finale! Explosion!");

        // Final explosion damage: Base × 1.5
        Collider[] finalEnemies = Physics.OverlapSphere(stormPosition, 8f, LayerMask.GetMask("Enemy"));

        foreach (Collider col in finalEnemies)
        {
            Character enemy = col.GetComponent<Character>();
            if (enemy != null)
            {
                int explosionDamage = GetScaledSkillDamageWithAmp(1.5f);
                enemy.TakeDamageFromAttacker(0, explosionDamage, this, DamageType.Critical, false);

                Debug.Log($"🩸 Blood Storm Explosion hit {enemy.CharacterName}: {explosionDamage} damage");
            }
        }

        // Explosion visual effect
        if (bloodExplosionEffect != null)
        {
            GameObject explosion = Instantiate(bloodExplosionEffect.gameObject, stormPosition, Quaternion.identity);
            explosion.transform.localScale = Vector3.one * 8f;
            Destroy(explosion, 3f);
        }

        // Destroy storm
        if (currentBloodStorm != null)
        {
            Destroy(currentBloodStorm);
        }

        IsInBloodStorm = false;
        Debug.Log($"🩸 [Blood Storm] Ended! Total enemies hit: {totalEnemiesHit}");
    }

    #endregion

    #region Override Attack (Blood Edge Integration)

    public override void TryAttack()
    {
        if (!HasInputAuthority || !IsSpawned) return;
        if (Time.time < nextAttackTime) return;

        Collider[] enemies = Physics.OverlapSphere(transform.position, AttackRange, LayerMask.GetMask("Enemy"));

        if (enemies.Length > 0)
        {
            NetworkEnemy nearestEnemy = null;
            float nearestDistance = float.MaxValue;

            foreach (Collider col in enemies)
            {
                NetworkEnemy enemy = col.GetComponent<NetworkEnemy>();
                if (enemy != null && enemy.IsSpawned && !enemy.IsDead)
                {
                    float distance = Vector3.Distance(transform.position, enemy.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = enemy;
                    }
                }
            }

            if (nearestEnemy != null)
            {
                bool hasBloodEdge = BloodEdgeStacks > 0;
                RPC_PerformBloodKnightAttack(nearestEnemy.Object, hasBloodEdge);

                // Cooldown reduction
                float cooldownReduction = GetEffectiveAttackSpeed();
                float finalAttackCooldown = AttackCooldown * (1f - cooldownReduction);
                nextAttackTime = Time.time + finalAttackCooldown;

                Debug.Log($"🩸 Blood Knight attack! Cooldown Reduction: {cooldownReduction * 100f}%, Final Cooldown: {finalAttackCooldown:F2}s");
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_PerformBloodKnightAttack(NetworkObject enemyObject, bool hasBloodEdge)
    {
        if (enemyObject != null)
        {
            Character enemy = enemyObject.GetComponent<Character>();
            if (enemy != null)
            {
                bloodEdgeHitCount++;
                bool isThirdHit = (bloodEdgeHitCount == 3 && BloodEdgeStacks >= 3);
                bool shouldHealEveryHit = (BloodEdgeStacks == 5); // Blood Trance mode

                int baseDamage = AttackDamage;
                int finalDamage = baseDamage;

                // Apply Blood Edge bonus
                if (hasBloodEdge)
                {
                    float bonus = 1f + BloodEdgeAttackBonus;
                    finalDamage = Mathf.RoundToInt(baseDamage * bonus);

                    Debug.Log($"🩸 [Blood Edge] Attack bonus: +{BloodEdgeAttackBonus * 100}%, Damage: {baseDamage} → {finalDamage}");
                }

                // Basic attack
                enemy.TakeDamageFromAttacker(finalDamage, 0, this, DamageType.Normal, true);

                // Apply Bleed if Blood Edge is active
                if (hasBloodEdge)
                {
                    int bleedDamage = CalculateBleedDamage(0.3f);
                    enemy.ApplyStatusEffect(StatusEffectType.Bleed, bleedDamage, 3f);

                    // Life Steal
                    if (shouldHealEveryHit || isThirdHit)
                    {
                        int healAmount = Mathf.RoundToInt(finalDamage * BloodEdgeLifeSteal);
                        Heal(healAmount);
                        Debug.Log($"🩸 [Blood Edge Heal] {CharacterName}: +{healAmount} HP");
                    }

                    // Reduce stack
                    BloodEdgeStacks--;

                    if (BloodEdgeStacks <= 0)
                    {
                        BloodEdgeStacks = 0;
                        BloodEdgeAttackBonus = 0f;
                        BloodEdgeLifeSteal = 0f;
                        bloodEdgeHitCount = 0;
                        Debug.Log($"🩸 [Blood Edge] Buff ended");
                    }
                }

                RPC_OnAttackHit(enemyObject);
            }
        }
    }

    #endregion

    #region Helper Methods

    private bool CanUseSkill(int manaCost)
    {
        if (HasStatusEffect(StatusEffectType.Stun))
        {
            Debug.Log($"❌ Cannot use skill while stunned!");
            return false;
        }

        // In Blood Trance, don't need mana
        if (IsInBloodTrance) return true;

        // Check mana
        if (CurrentMana < manaCost)
        {
            Debug.Log($"❌ Not enough mana! Need {manaCost}, have {CurrentMana}");
            return false;
        }

        return true;
    }

    private int GetAdjustedManaCost(int baseCost)
    {
        if (!isPhysicalBuild)
        {
            return Mathf.RoundToInt(baseCost * 0.7f); // Magic Build: -30% Mana Cost
        }
        return baseCost;
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

    #region Visual Effects RPCs

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateBloodAuraColor(float bloodPoints)
    {
        if (bloodAuraEffect == null) return;

        ParticleSystem.MainModule main = bloodAuraEffect.main;
        Color targetColor;

        if (bloodPoints < 33f)
        {
            targetColor = lightRedColor;
        }
        else if (bloodPoints < 66f)
        {
            targetColor = mediumRedColor;
        }
        else if (bloodPoints < 100f)
        {
            targetColor = darkRedColor;
        }
        else
        {
            targetColor = crimsonColor;
        }

        main.startColor = targetColor;

        // Adjust particle emission based on blood points
        ParticleSystem.EmissionModule emission = bloodAuraEffect.emission;
        emission.rateOverTime = bloodPoints * 0.5f; // 0-50 particles/s
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowBloodTranceBurst()
    {
        if (bloodTranceBurstEffect != null)
        {
            GameObject burst = Instantiate(bloodTranceBurstEffect.gameObject, transform.position + Vector3.up * 1f, Quaternion.identity);

            ParticleSystem ps = burst.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = crimsonColor;
                main.startLifetime = 1f;
            }

            Destroy(burst, 2f);
        }

        Debug.Log($"🩸 [Visual] Blood Trance Burst!");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowBloodTranceAura(bool show)
    {
        if (bloodAuraEffect != null)
        {
            if (show)
            {
                bloodAuraEffect.Play();

                ParticleSystem.MainModule main = bloodAuraEffect.main;
                main.startColor = crimsonColor;

                ParticleSystem.EmissionModule emission = bloodAuraEffect.emission;
                emission.rateOverTime = 50f; // Max emission
            }
            else
            {
                bloodAuraEffect.Stop();
            }
        }

        if (bloodDripEffect != null)
        {
            if (show)
            {
                bloodDripEffect.Play();
            }
            else
            {
                bloodDripEffect.Stop();
            }
        }

        Debug.Log($"🩸 [Visual] Blood Trance Aura: {(show ? "ON" : "OFF")}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowSkillEffect(string effectName)
    {
        Debug.Log($"✨ [Skill Effect] {CharacterName} - {effectName}");

        switch (effectName)
        {
            case "BloodEdge":
                ShowBloodEdgeEffect();
                break;
            case "CrimsonLance":
                ShowCrimsonLanceEffect();
                break;
            case "BloodPool":
                ShowBloodPoolEffect();
                break;
            case "BloodMoon":
                ShowBloodMoonEffect();
                break;
        }
    }

    private void ShowBloodEdgeEffect()
    {
        if (bloodEdgeEffect != null)
        {
            GameObject effect = Instantiate(bloodEdgeEffect.gameObject, transform.position + Vector3.up * 1f, Quaternion.identity);

            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = Color.red;
                main.startLifetime = 1f;
            }

            Destroy(effect, 2f);
        }
    }

    private void ShowCrimsonLanceEffect()
    {
        if (crimsonLanceEffect != null)
        {
            GameObject effect = Instantiate(crimsonLanceEffect.gameObject, transform.position + Vector3.up * 1.5f, Quaternion.identity);

            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = new Color(0.8f, 0f, 0f, 1f);
                main.startLifetime = 1.5f;
            }

            Destroy(effect, 2.5f);
        }
    }

    private void ShowBloodPoolEffect()
    {
        // Pool effect is handled in ExecuteBloodPool
    }

    private void ShowBloodMoonEffect()
    {
        if (bloodExplosionEffect != null)
        {
            GameObject effect = Instantiate(bloodExplosionEffect.gameObject, transform.position, Quaternion.identity);
            effect.transform.localScale = Vector3.one * 6f;

            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = new Color(0.5f, 0f, 0f, 0.8f);
                main.startLifetime = 2f;
            }

            Destroy(effect, 3f);
        }
    }

    #endregion

    #region Network Update Override

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        // Drain Blood Points during Blood Trance
        if (HasStateAuthority && IsInBloodTrance)
        {
            DrainBloodPoints(Runner.DeltaTime);
        }
    }

    #endregion

    #region Override Methods

    protected override void OnDestroy()
    {
        base.OnDestroy();

        // Clean up pools and storms
        if (currentBloodPool != null)
        {
            Destroy(currentBloodPool);
        }

        if (currentBloodStorm != null)
        {
            Destroy(currentBloodStorm);
        }
    }

    #endregion
}