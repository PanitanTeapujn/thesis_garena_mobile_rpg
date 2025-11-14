using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Assassin : Hero
{
    [Header("🐍 Assassin Skill Settings")]
    [SerializeField] private int skill1ManaCost = 15;
    [SerializeField] private int skill2ManaCost = 20;
    [SerializeField] private int skill3ManaCost = 25;
    [SerializeField] private int skill4ManaCost = 45;
    protected override int GetSkill1ManaCost() { return skill1ManaCost; }
    protected override int GetSkill2ManaCost() { return skill2ManaCost; }
    protected override int GetSkill3ManaCost() { return skill3ManaCost; }
    protected override int GetSkill4ManaCost() { return skill4ManaCost; }
    [Header("🎯 Skill Parameters")]
    public float dashDistance = 8f;
    public float dashSpeed = 20f;
    public GameObject toxicBombPrefab;
    public GameObject plagueCloudPrefab;

    // เพิ่มในส่วน Network Properties (หลัง line ~40)
    [Header("🌙 Shadow Mode (Ferris Point Transformation)")]
    [Networked] public bool IsInShadowMode { get; set; }
    [Networked] public int Skill3UsesRemaining { get; set; }
    [Networked] public bool CanDashAgain { get; set; }
    [Networked] public float DashGracePeriodEnd { get; set; }

    private float shadowModeDrainRate = 10f; // 10 points per second
    private float dashGracePeriod = 3.5f; // 1 second to press dash again
    private float accumulatedDrain = 0f;
    [Header("🎨 Visual Effects")]
    public Material skillRangeIndicatorMaterial;
    private GameObject plagueRangeIndicator;
    private LineRenderer rangeCircle;
    [Header("🎨 Assassin Visual Effects")]
    [SerializeField] private ParticleSystem poisonInfusionEffect;    // เอฟเฟกต์พิษ
    [SerializeField] private ParticleSystem toxicDashEffect;         // เอฟเฟกต์ dash
    [SerializeField] private ParticleSystem shadowAssassinEffect;    // เอฟเฟกต์ teleport
    [SerializeField] private ParticleSystem plagueOutbreakEffect;    // เอฟเฟกต์ ultimate
    [SerializeField] private ParticleSystem basicAttackEffect;       // เอฟเฟกต์โจมตีปกติ
    [SerializeField] private ParticleSystem invisibilityEffect;      // เอฟเฟกต์หายตัว
    // ========== Network Properties for Skills ==========
    [Networked] public int PoisonInfusionStacks { get; set; }
    [Networked] public bool IsInPlagueCloud { get; set; }
    [Networked] public float PlagueCloudEndTime { get; set; }
    [Networked] public bool IsDashing { get; set; }
    [Networked] public bool IsAssassinating { get; set; }
    [Networked] public bool IsInvisible { get; set; }

    // Passive tracking
    private bool hasVenomMastery = true; // Passive เปิดตลอด
    [Header("🌟 Skill 3 Chain Settings")]
    [SerializeField] private float chainInterval = 0.3f; // เวลาระหว่างการวาร์ปแต่ละครั้ง
    [SerializeField] private float chainTeleportSpeed = 30f; // ความเร็วการเคลื่อนที่
    [SerializeField] private int maxChainTargets = 6; // จำนวน chain สูงสุด

    [Header("🐍 Venom Mastery Settings")]
    [SerializeField] private int manaRestoreOnPoisonKill = 15; // Mana ที่คืนเมื่อฆ่าด้วยพิษ
    [SerializeField] private float poisonDurationExtension = 2f; // เวลาที่เพิ่มเมื่อติดพิษซ้ำ
    [SerializeField] private float maxPoisonDuration = 15f; // duration สูงสุด
    private List<Character> chainedTargets = new List<Character>(); // เก็บศัตรูที่โดน chain แล้ว

    protected override void Start()
    {
        base.Start();
        AttackType = AttackType.Physical;
        // ปรับ cooldown สำหรับ Assassin
        skill1Cooldown = 13f;
        skill2Cooldown = 18f;
        skill3Cooldown = 22f;
        skill4Cooldown = 32f;

        // สร้าง range indicator สำหรับ ultimate
        CreateRangeIndicator();
        IsInShadowMode = false;
        Skill3UsesRemaining = 0;
        CanDashAgain = false;
        accumulatedDrain = 0f;
        StatusEffectManager.OnStatusDamage += HandlePoisonKillForManaRestore;

        Debug.Log($"🐍 Assassin {CharacterName} initialized with Venom Mastery!");
    }

    // ========== 💚 Skill 1: Poison Infusion ==========
    public override void TryUseSkill1()
    {
        if (!CanUseSkill(skill1ManaCost)) return;

        float effectiveReduction = GetEffectiveReductionCoolDown();
        float reductionMultiplier = 1f - (effectiveReduction / 100f);
        reductionMultiplier = Mathf.Clamp(reductionMultiplier, 0.1f, 1f);

        float finalCooldown = skill1Cooldown * reductionMultiplier;
        nextSkill1Time = Time.time + finalCooldown;

        UseMana(skill1ManaCost);
        PoisonInfusionStacks = 3;

        statusEffectManager.ApplyCriticalAura(6f, 0.15f, 20f);

        // ✅ เก็บ Ferris Point
        if (!IsInShadowMode)
        {
            GainFerrisPoint(15); // +15 points
        }

        Debug.Log($"🐍 [Poison Infusion] {CharacterName} gains 3 poison-infused attacks!");
        RPC_ShowSkillEffect("PoisonInfusion");
    }

    // ========== 🌫️ Skill 2: Toxic Dash - FIXED ==========
    // แทนที่ TryUseSkill2() เดิมทั้งหมด
    public override void TryUseSkill2()
    {
        if (!CanUseSkill(skill2ManaCost)) return;
        if (HasStatusEffect(StatusEffectType.Stun)) return;

        // ✅ คำนวณ cooldown reduction
        float effectiveReduction = GetEffectiveReductionCoolDown();
        float reductionMultiplier = 1f - (effectiveReduction / 100f);
        reductionMultiplier = Mathf.Clamp(reductionMultiplier, 0.1f, 1f);
        float finalCooldown = skill2Cooldown * reductionMultiplier;
        /*statusEffectManager.ApplyAttackSpeedAura(6f, 0.3f, 12f);        // +30% Attack Speed
        statusEffectManager.ApplyDamageAura(5f, 0.2f, 15f);             // +20% Physical Damage
        statusEffectManager.ApplyMoveSpeedAura(5f, 0.15f, 15f);         // +15% Move Speed
        statusEffectManager.ApplyProtectionAura(6f, 0.15f, 20f);        // -15% Damage Taken
        statusEffectManager.ApplyArmorAura(6f, 0.2f, 20f);              // +20% Armor
        statusEffectManager.ApplyCriticalAura(6f, 0.15f, 20f);          // +15% Critical Chance

        // ========== 🆕 Aura Buffs ใหม่ (9 ตัว) ==========
        statusEffectManager.ApplyMagicDamageAura(5f, 0.2f, 15f);        // +20% Magic Damage
        statusEffectManager.ApplyCooldownReductionAura(5f, 0.15f, 15f); // +15% CDR
        statusEffectManager.ApplyLifeStealAura(5f, 0.1f, 15f);          // +10% Life Steal
        statusEffectManager.ApplyAmpDamageAura(5f, 0.15f, 15f);         // +15% Amp Damage
        statusEffectManager.ApplyMagicArmorAura(6f, 0.2f, 20f);         // +20% Magic Armor
        statusEffectManager.ApplyHealthRegenAura(5f, 0.5f, 20f);        // +50% Health Regen
        statusEffectManager.ApplyManaRegenAura(5f, 0.5f, 20f);          // +50% Mana Regen
        statusEffectManager.ApplyHitRateAura(5f, 0.15f, 15f);           // +15% Hit Rate
        statusEffectManager.ApplyEvasionAura(5f, 0.1f, 15f);            // +10% Eva*/
        if (!IsInShadowMode)
        {
            // ✅ Normal Mode - dash 1 ครั้ง
            nextSkill2Time = Time.time + finalCooldown;
            UseMana(skill2ManaCost);

            if (statusEffectManager != null)
            {
                statusEffectManager.ApplyMoveSpeedAura(5f, 0.25f, 10f);
            }

            Vector3 dashDirection = GetDashDirection();
            RPC_PerformToxicDashAll(dashDirection, transform.position);

            Debug.Log($"🌫️ [Toxic Dash Normal] {CharacterName} dashed once");
        }
        else
        {
            // ✅ Shadow Mode - Double Dash
            if (!CanDashAgain)
            {
                // ครั้งแรก - ยังไม่ติด cooldown
                UseMana(skill2ManaCost);

                if (statusEffectManager != null)
                {
                    statusEffectManager.ApplyMoveSpeedAura(5f, 0.20f, 10f);
                }

                Vector3 dashDirection = GetDashDirection();
                RPC_PerformToxicDashAll(dashDirection, transform.position);

                // เปิด grace period
                CanDashAgain = true;
                DashGracePeriodEnd = Time.time + dashGracePeriod;

                Debug.Log($"🌙 [Shadow Dash 1/2] {CharacterName} - Press again within {dashGracePeriod}s!");
            }
            else
            {
                // ครั้งที่สอง - ติด cooldown
                nextSkill2Time = Time.time + finalCooldown;
                UseMana(skill2ManaCost);

                Vector3 dashDirection = GetDashDirection();
                RPC_PerformToxicDashAll(dashDirection, transform.position);

                // ปิด double dash
                CanDashAgain = false;

                Debug.Log($"🌙 [Shadow Dash 2/2] {CharacterName} - Cooldown applied!");
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_PerformToxicDashAll(Vector3 direction, Vector3 startPosition)
    {
        StartCoroutine(ExecuteToxicDashFixed(direction, startPosition));
    }

    private IEnumerator ExecuteToxicDashFixed(Vector3 direction, Vector3 startPosition)
    {
        AudioManager.instance.PlaySFX(2, 1f);

        Vector3 startPos = startPosition;

        // ✅ ตรวจสอบกำแพงก่อน dash
        float actualDashDistance = dashDistance;
        RaycastHit wallHit;

        if (Physics.Raycast(startPos, direction, out wallHit, dashDistance,
            LayerMask.GetMask("Wall", "Obstacle", "Default")))
        {
            actualDashDistance = Mathf.Max(0.5f, wallHit.distance - 0.5f);
            Debug.Log($"⚠️ Wall detected at {wallHit.distance}m, reducing dash to {actualDashDistance}m");
        }

        Vector3 endPos = startPos + direction * actualDashDistance;
        float dashTime = actualDashDistance / dashSpeed;
        float elapsed = 0f;

        List<Character> hitEnemies = new List<Character>();
        IsDashing = true;

        // สร้าง dash trail effect
        GameObject dashTrail = null;
        if (toxicDashEffect != null)
        {
            dashTrail = Instantiate(toxicDashEffect.gameObject, startPos, Quaternion.identity);

            ParticleSystem ps = dashTrail.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = new Color(0.5f, 1f, 0f, 0.8f);
                main.startLifetime = dashTime + 1f;
                main.loop = true;

                ParticleSystem.ShapeModule shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(1f, 1f, actualDashDistance);
            }
        }

        while (elapsed < dashTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / dashTime;

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);

            // ✅ ตรวจสอบกำแพงระหว่าง dash (safety check)
            Vector3 moveVector = currentPos - transform.position;
            if (moveVector.magnitude > 0.1f)
            {
                if (Physics.Raycast(transform.position, moveVector.normalized, moveVector.magnitude,
                    LayerMask.GetMask("Wall", "Obstacle", "Default")))
                {
                    Debug.LogWarning("🛑 Wall collision during dash! Stopping.");
                    break;
                }
            }

            // อัพเดทตำแหน่ง dash trail
            if (dashTrail != null)
            {
                dashTrail.transform.position = currentPos;
                dashTrail.transform.LookAt(endPos);
            }

            // ✅ ใช้ MovePosition แทน direct position
            if (HasInputAuthority)
            {
                if (rb != null)
                {
                    rb.MovePosition(currentPos);
                }
                NetworkedPosition = currentPos;
            }
            else
            {
                transform.position = currentPos;
            }

            // ✅ ลบบรรทัดนี้ออก - ไม่ต้องหมุนตัวสำหรับ 2D
            // transform.forward = direction; ❌ ลบออก

            // เช็คศัตรู
            if (HasStateAuthority)
            {
                Collider[] enemies = Physics.OverlapSphere(currentPos, 2f, LayerMask.GetMask("Enemy"));
                foreach (Collider col in enemies)
                {
                    Character enemy = col.GetComponent<Character>();
                    if (enemy != null && !hitEnemies.Contains(enemy))
                    {
                        hitEnemies.Add(enemy);
                        ApplyToxicDashEffects(enemy);
                        CreateToxicHitEffect(enemy.transform.position);
                    }
                }
            }

            yield return null;
        }

        // ตั้งตำแหน่งสุดท้าย
        if (HasInputAuthority)
        {
            if (rb != null)
            {
                rb.MovePosition(endPos);
            }
            NetworkedPosition = endPos;
        }
        else
        {
            transform.position = endPos;
        }

        // ทำลาย dash trail
        if (dashTrail != null)
        {
            ParticleSystem ps = dashTrail.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.loop = false;
            }
            Destroy(dashTrail, 2f);
        }

        IsDashing = false;
        Debug.Log($"🌫️ [Toxic Dash] {CharacterName} dashed {actualDashDistance}m through {hitEnemies.Count} enemies!");
    }
    private void CreateToxicHitEffect(Vector3 position)
    {
        if (toxicDashEffect != null)
        {
            GameObject hitEffect = Instantiate(toxicDashEffect.gameObject, position + Vector3.up * 1f, Quaternion.identity);

            ParticleSystem ps = hitEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = new Color(0.8f, 0.2f, 0.8f, 0.9f); // สีม่วงพิษ
                main.startLifetime = 1f;
            }

            Destroy(hitEffect, 1.5f);
        }
    }

    // เพิ่มก่อน Debug.Log ท้ายสุด
    private void ApplyToxicDashEffects(Character enemy)
    {
        int directDamage = GetScaledSkillDamage(0.6f);

        // ✅ NERF: 0.3f → 0.21f (-30%)
        int poisonDamage = GetScaledPoisonDamage(0.21f);

        enemy.TakeDamageFromAttacker(0, directDamage, this, DamageType.Magic, false);

        Vector3 dashDirection = GetDashDirection();
        enemy.ApplyKnockback(dashDirection, 4f, 0.3f);

        enemy.ApplyStatusEffect(StatusEffectType.Poison, poisonDamage, 8f);

        // ✅ Extend poison duration
        ExtendPoisonDuration(enemy);

        if (hasVenomMastery && Random.Range(0f, 100f) < 25f)
        {
            SpreadPoisonToNearby(enemy, 4f);
        }

        if (!IsInShadowMode)
        {
            GainFerrisPoint(5);
        }

        Debug.Log($"🌫️ Toxic Dash hit {enemy.CharacterName}! Direct: {directDamage}, Poison: {poisonDamage}/s");
    }
    // ========== 💣 Skill 3: Shadow Assassination - FIXED ==========
    public override void TryUseSkill3()
    {
        if (!CanUseSkill(skill3ManaCost)) return;

        float effectiveReduction = GetEffectiveReductionCoolDown();
        float reductionMultiplier = 1f - (effectiveReduction / 100f);
        reductionMultiplier = Mathf.Clamp(reductionMultiplier, 0.1f, 1f);
        float finalCooldown = skill3Cooldown * reductionMultiplier;

        if (!IsInShadowMode)
        {
            // ✅ Normal Mode: Single Target
            ExecuteNormalModeShadowAssassination(finalCooldown);
        }
        else
        {
            // ✅ Ferris Mode: Chain 6x
            ExecuteFerrisModeShadowAssassination(finalCooldown);
        }
    }
    private void ExecuteNormalModeShadowAssassination(float finalCooldown)
    {
        // หาเป้าหมายใกล้ที่สุด
        Character target = FindNearestEnemy(8f);

        if (target == null)
        {
            Debug.Log("❌ No target found for Shadow Assassination!");
            return;
        }

        // ใช้ Mana
        UseMana(skill3ManaCost);

        // ✅ ไม่ติด cooldown ก่อน (จะติดหลังถ้าไม่ฆ่าศัตรูที่ติดพิษ)
        // nextSkill3Time จะถูกตั้งใน RPC_ExecuteNormalAssassination

        // Apply Critical Aura
        if (statusEffectManager != null)
        {
            statusEffectManager.ApplyCriticalAura(6f, 0.4f, 12f);
        }

        Vector3 originalPos = transform.position;
        NetworkObject targetNetworkObject = target.GetComponent<NetworkObject>();

        if (targetNetworkObject != null)
        {
            RPC_ExecuteNormalAssassination(targetNetworkObject, originalPos, finalCooldown);
        }

        Debug.Log($"🗡️ [Shadow Assassination Normal] {CharacterName} targeting {target.CharacterName}");
    }

    /// <summary>
    /// Ferris Mode: วาร์ปโจมตี 6 ครั้ง
    /// </summary>
    private void ExecuteFerrisModeShadowAssassination(float finalCooldown)
    {
        // หาศัตรูทั้งหมดในระยะ
        List<Character> targets = FindNearestEnemies(8f, maxChainTargets);

        if (targets.Count == 0)
        {
            Debug.Log("❌ No targets found for Ferris Shadow Assassination!");
            return;
        }

        // ใช้ Mana
        UseMana(skill3ManaCost);

        // ✅ ติด cooldown ทันที (Ferris Mode ไม่รีเซ็ต)
        nextSkill3Time = Time.time + finalCooldown;

        // สร้าง list ของ NetworkObject
        List<NetworkObject> targetObjects = new List<NetworkObject>();
        foreach (Character target in targets)
        {
            NetworkObject netObj = target.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                targetObjects.Add(netObj);
            }
        }

        Vector3 originalPos = transform.position;
        RPC_ExecuteChainAssassination(targetObjects.ToArray(), originalPos);

        Debug.Log($"🌙 [Ferris Shadow Assassination] {CharacterName} chaining {targets.Count} targets!");
    }


    // ========== 6. RPC: Normal Mode Assassination ==========

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_ExecuteNormalAssassination(NetworkObject targetObject, Vector3 originalPosition, float cooldown)
    {
        StartCoroutine(ExecuteNormalAssassinationSequence(targetObject, originalPosition, cooldown));
    }

    // ใน Assassin.cs - แก้ไข ExecuteNormalAssassinationSequence

    // ใน Assassin.cs - แก้ไข ExecuteNormalAssassinationSequence

    private IEnumerator ExecuteNormalAssassinationSequence(NetworkObject targetObject, Vector3 originalPosition, float cooldown)
    {
        if (targetObject == null) yield break;

        Character target = targetObject.GetComponent<Character>();
        if (target == null) yield break;

        AudioManager.instance.PlaySFX(3, 1f);
        IsAssassinating = true;

        // คำนวณตำแหน่งหลังเป้าหมาย
        Vector3 targetPos = target.transform.position;
        Vector3 directionToTarget = (targetPos - originalPosition).normalized;
        Vector3 backPosition = targetPos - directionToTarget * 2.5f;
        backPosition.y = targetPos.y;

        // ✅ ตรวจสอบกำแพง
        RaycastHit wallCheck;
        Vector3 teleportDirection = (backPosition - originalPosition).normalized;
        float teleportDistance = Vector3.Distance(originalPosition, backPosition);

        if (Physics.Raycast(originalPosition, teleportDirection, out wallCheck, teleportDistance,
            LayerMask.GetMask("Wall", "Obstacle", "Default")))
        {
            backPosition = originalPosition + teleportDirection * Mathf.Max(0.5f, wallCheck.distance - 0.5f);
        }

        // Phase 1: Teleport
        Debug.Log($"🗡️ [Shadow Assassination] {CharacterName} teleporting to {target.CharacterName}!");

        SetVisibility(false);
        yield return new WaitForSeconds(0.2f);

        if (HasInputAuthority)
        {
            if (rb != null) rb.MovePosition(backPosition);
            NetworkedPosition = backPosition;
        }
        else
        {
            transform.position = backPosition;
        }

        SetVisibility(true);
        yield return new WaitForSeconds(0.1f);

        // Phase 2: Attack
        if (HasStateAuthority && target != null)
        {
            // ✅ เช็คว่าเป้าหมายติดพิษหรือไม่ (ก่อนโจมตี)
            bool wasTargetPoisoned = CheckIfTargetPoisoned(target);

            // โจมตี
            PerformExecutionAttack(target);

            // ✅ ถ้าเป้าหมายติดพิษ → คืน Mana + ลด CD 50% (ไม่ต้องฆ่า)
            if (wasTargetPoisoned)
            {
                // คืน 50% Mana
                int manaRestore = Mathf.RoundToInt(skill3ManaCost * 0.5f);
                CurrentMana = Mathf.Min(CurrentMana + manaRestore, MaxMana);

                // ✅ ลด Cooldown 50% (ไม่ใช่รีเซ็ตเป็น 0)
                float reducedCooldown = cooldown * 0.5f; // เหลือแค่ 50%
                nextSkill3Time = Time.time + reducedCooldown;

                Debug.Log($"✅ [Execute Success] Hit poisoned enemy! +{manaRestore} Mana, CD reduced by 50% (now {reducedCooldown:F1}s)");
                RPC_ShowExecuteSuccessText(manaRestore, reducedCooldown);
            }
            else
            {
                // ✅ ถ้าไม่ติดพิษ → ติด cooldown เต็ม
                nextSkill3Time = Time.time + cooldown;
                Debug.Log($"⚠️ [Execute Normal] Target not poisoned. Full cooldown: {cooldown:F1}s");
            }
        }

        IsAssassinating = false;
    }

    // ✅ แก้ไข RPC แสดงข้อความ
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowExecuteSuccessText(int manaRestore, float cooldownRemaining)
    {
        Vector3 textPosition = transform.position + Vector3.up * 1.5f;
        Color successColor = new Color(1f, 0.8f, 0f, 1f); // ทอง
        DamageTextManager.ShowCustomText(textPosition, $"EXECUTE!\n+{manaRestore} MANA\n-50% CD ({cooldownRemaining:F1}s)", successColor, 1.2f);
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

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowExecuteSuccessText(int manaRestore)
    {
        Vector3 textPosition = transform.position + Vector3.up * 1.5f;
        Color successColor = new Color(1f, 0.8f, 0f, 1f); // ทอง
        DamageTextManager.ShowCustomText(textPosition, $"EXECUTE! +{manaRestore} MANA", successColor, 1f);
    }


    // ========== 7. RPC: Ferris Mode Chain Assassination ==========

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_ExecuteChainAssassination(NetworkObject[] targetObjects, Vector3 originalPosition)
    {
        StartCoroutine(ExecuteChainAssassinationSequence(targetObjects, originalPosition));
    }

    private IEnumerator ExecuteChainAssassinationSequence(NetworkObject[] targetObjects, Vector3 originalPosition)
    {
        if (targetObjects == null || targetObjects.Length == 0) yield break;

        // ✅ เล่นเสียงครั้งแรก (ดังที่สุด)
        AudioManager.instance.PlaySFX(3, 1f);

        IsAssassinating = true;
        chainedTargets.Clear();

        Debug.Log($"🌙 [Chain Start] Assassinating {targetObjects.Length} targets!");

        // ✅ ตรวจสอบว่าเป้าหมายเหลือตัวเดียวหรือไม่
        List<Character> aliveTargets = new List<Character>();
        foreach (NetworkObject netObj in targetObjects)
        {
            if (netObj != null)
            {
                Character target = netObj.GetComponent<Character>();
                if (target != null && target.CurrentHp > 0)
                {
                    aliveTargets.Add(target);
                }
            }
        }

        bool isSingleTarget = aliveTargets.Count == 1;

        // Chain loop - สูงสุด 6 ครั้ง
        for (int i = 0; i < maxChainTargets; i++)
        {
            Character currentTarget = null;

            if (isSingleTarget && aliveTargets.Count > 0)
            {
                // ✅ เป้าหมายเดียว (Boss) → ตีซ้ำที่ตัวเดียว
                currentTarget = aliveTargets[0];
            }
            else
            {
                // ✅ หลายเป้าหมาย → หาเป้าหมายที่ยังไม่โดน
                currentTarget = FindUnchainedTarget(aliveTargets);
            }

            if (currentTarget == null)
            {
                Debug.Log($"🌙 [Chain {i + 1}/6] No more targets!");
                break;
            }

            // บันทึกว่าโดนแล้ว (ถ้าไม่ใช่ single target)
            if (!isSingleTarget && !chainedTargets.Contains(currentTarget))
            {
                chainedTargets.Add(currentTarget);
            }

            // ✅ วาร์พไปหาเป้าหมาย (แบบหายตัว)
            yield return StartCoroutine(TeleportToTarget(currentTarget, i + 1));

            // โจมตี
            if (HasStateAuthority && currentTarget != null)
            {
                PerformChainAttack(currentTarget, i + 1);
            }

            // รอก่อน chain ครั้งต่อไป
            yield return new WaitForSeconds(chainInterval);

            // เช็คว่าเป้าหมายตายหรือยัง
            if (currentTarget.CurrentHp <= 0)
            {
                aliveTargets.Remove(currentTarget);

                if (aliveTargets.Count == 0)
                {
                    Debug.Log($"🌙 [Chain {i + 1}/6] All targets eliminated!");
                    break;
                }
            }
        }

        IsAssassinating = false;
        chainedTargets.Clear();

        Debug.Log($"🌙 [Chain Complete] Ferris Shadow Assassination finished!");
    }
    private IEnumerator TeleportToTarget(Character target, int chainNumber)
    {
        if (target == null) yield break;

        Vector3 startPos = transform.position;
        Vector3 targetPos = target.transform.position;

        // ✅ คำนวณตำแหน่งด้านหลัง + สูงขึ้น
        Vector3 direction = (startPos - targetPos).normalized; // ทิศทางจากศัตรูมาหาเรา
        Vector3 behindTarget = targetPos + direction * 2f;     // อยู่ด้านหลังศัตรู 2m
        behindTarget.y = targetPos.y + 1.5f;                   // ✅ สูงขึ้น 1.5m

        // ✅ Phase 1: หายตัว
        SetVisibility(false);

        // ✅ เล่นเสียงวาร์ป (ดังลดลงเรื่อยๆ)
        float volume = Mathf.Lerp(0.8f, 0.3f, (chainNumber - 1) / (float)maxChainTargets);
        AudioManager.instance.PlaySFX(3, volume);

        yield return new WaitForSeconds(0.15f);

        // ✅ Phase 2: Teleport ไปตำแหน่งใหม่
        if (HasInputAuthority)
        {
            if (rb != null) rb.MovePosition(behindTarget);
            NetworkedPosition = behindTarget;
        }
        else
        {
            transform.position = behindTarget;
        }

        yield return new WaitForSeconds(0.05f);

        // ✅ Phase 3: ปรากฏตัว
        SetVisibility(true);

        // แสดง teleport effect
        RPC_ShowTeleportEffect(behindTarget, chainNumber);

        Debug.Log($"🌙 [Teleport {chainNumber}/6] Warped to {target.CharacterName} at height {behindTarget.y - targetPos.y}m");
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowTeleportEffect(Vector3 position, int chainNumber)
    {
        if (shadowAssassinEffect != null)
        {
            GameObject effect = Instantiate(shadowAssassinEffect.gameObject, position, Quaternion.identity);

            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;

                // สีเปลี่ยนตามจำนวน chain (ม่วง → ชมพู → แดง)
                Color[] colors = new Color[]
                {
                new Color(0.8f, 0.3f, 1f, 1f),   // ม่วง
                new Color(0.7f, 0.2f, 1f, 1f),   // ม่วงเข้ม
                new Color(0.9f, 0.2f, 0.8f, 1f), // ชมพู
                new Color(1f, 0.2f, 0.6f, 1f),   // ชมพูแดง
                new Color(1f, 0.3f, 0.3f, 1f),   // แดง
                new Color(1f, 0.5f, 0f, 1f)      // ส้ม
                };

                main.startColor = colors[Mathf.Clamp(chainNumber - 1, 0, 5)];
                main.startLifetime = 0.6f;
                main.startSize = 1.5f + (chainNumber * 0.3f); // ใหญ่ขึ้นทุกครั้ง
            }

            Destroy(effect, 1.2f);
        }
    }

    /// <summary>
    /// เคลื่อนที่ไปหาเป้าหมาย (เห็นตัวตลอด)
    /// </summary>
    

    /// <summary>
    /// โจมตีในระหว่าง Chain
    /// </summary>
    private void PerformChainAttack(Character target, int chainNumber)
    {
        if (target == null) return;

        bool isPoisoned = CheckIfTargetPoisoned(target);

        // ✅ คำนวณดาเมจ
        int baseDamage = CalculateExecutionDamage();

        // ดาเมจตามสถานะพิษ
        float damageMultiplier = isPoisoned ? 1.25f : 1.0f; // 125% / 100%
        int finalDamage = Mathf.RoundToInt(baseDamage * damageMultiplier);

        int physicalDamage = Mathf.RoundToInt(finalDamage * 0.3f);
        int magicDamage = Mathf.RoundToInt(finalDamage * 0.7f);

        target.TakeDamageFromAttacker(physicalDamage, magicDamage, this, DamageType.Critical, false);

        // ✅ ใส่พิษ + Extend Duration
        if (MagicDamage > AttackDamage)
        {
            int poisonDamage = GetScaledPoisonDamage(0.4f);
            target.ApplyStatusEffect(StatusEffectType.Poison, poisonDamage, 10f);

            if (isPoisoned)
            {
                ExtendPoisonDuration(target);
            }
        }
        else
        {
            target.ApplyStatusEffect(StatusEffectType.Bleed, 8, 8f);
        }

        Debug.Log($"🌙 [Chain {chainNumber}/6] Hit {target.CharacterName}: {finalDamage} damage (Poisoned: {isPoisoned}, {damageMultiplier * 100}%)");

        // แสดง particle
        RPC_ShowChainHitEffect(target.transform.position, chainNumber);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowChainHitEffect(Vector3 position, int chainNumber)
    {
        if (shadowAssassinEffect != null)
        {
            GameObject effect = Instantiate(shadowAssassinEffect.gameObject, position + Vector3.up * 1f, Quaternion.identity);

            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;

                // สีเปลี่ยนตามจำนวน chain
                Color[] colors = new Color[]
                {
                new Color(0.8f, 0.3f, 1f, 1f),  // ม่วง
                new Color(0.6f, 0.2f, 1f, 1f),  // ม่วงเข้ม
                new Color(0.9f, 0.2f, 0.8f, 1f), // ชมพู
                new Color(1f, 0.2f, 0.5f, 1f),  // แดงชมพู
                new Color(1f, 0.3f, 0.3f, 1f),  // แดง
                new Color(1f, 0.5f, 0f, 1f)     // ส้ม
                };

                main.startColor = colors[Mathf.Clamp(chainNumber - 1, 0, 5)];
                main.startLifetime = 0.8f;
                main.startSize = 1f + (chainNumber * 0.2f); // ใหญ่ขึ้นทุกครั้ง
            }

            Destroy(effect, 1.5f);
        }
    }

    /// <summary>
    /// หาเป้าหมายที่ยังไม่โดน chain
    /// </summary>
    private Character FindUnchainedTarget(List<Character> targets)
    {
        foreach (Character target in targets)
        {
            if (target != null && target.CurrentHp > 0 && !chainedTargets.Contains(target))
            {
                return target;
            }
        }
        return null;
    }

    /// <summary>
    /// เช็คว่าเป้าหมายติดพิษหรือไม่
    /// </summary>
    private bool CheckIfTargetPoisoned(Character target)
    {
        if (target == null) return false;

        StatusEffectManager targetStatus = target.GetComponent<StatusEffectManager>();
        return targetStatus != null && targetStatus.IsPoisoned;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_PerformShadowAssassinationAll(Vector3 targetPosition, NetworkObject targetObject, Vector3 originalPosition)
    {
        StartCoroutine(ExecuteShadowAssassinationFixed(targetPosition, targetObject, originalPosition));
    }

    private IEnumerator ExecuteShadowAssassinationFixed(Vector3 targetPosition, NetworkObject targetObject, Vector3 originalPosition)
    {
        AudioManager.instance.PlaySFX(3, 1f);

        IsAssassinating = true;

        // คำนวณตำแหน่งหลังเป้าหมาย
        Vector3 directionToTarget = (targetPosition - originalPosition).normalized;
        Vector3 backPosition = targetPosition - directionToTarget * 2.5f;
        backPosition.y = targetPosition.y;

        // ✅ ตรวจสอบกำแพงก่อนเทเลพอร์ต
        RaycastHit wallCheck;
        Vector3 teleportDirection = (backPosition - originalPosition).normalized;
        float teleportDistance = Vector3.Distance(originalPosition, backPosition);

        if (Physics.Raycast(originalPosition, teleportDirection, out wallCheck, teleportDistance,
            LayerMask.GetMask("Wall", "Obstacle", "Default")))
        {
            // ถ้าเจอกำแพง ให้ teleport ไปหน้ากำแพงแทน
            backPosition = originalPosition + teleportDirection * Mathf.Max(0.5f, wallCheck.distance - 0.5f);
            Debug.LogWarning($"⚠️ Wall detected! Adjusting teleport position to {wallCheck.distance - 0.5f}m");
        }

        // Phase 1: เทเลพอร์ตไปหลังเป้าหมาย
        Debug.Log($"🗡️ [Shadow Assassination] {CharacterName} teleporting behind target!");

        // ซ่อนตัว
        SetVisibility(false);
        yield return new WaitForSeconds(0.2f);

        // ✅ เทเลพอร์ตโดยไม่หมุนตัว (2D Sprite ไม่ต้อง rotate)
        if (HasInputAuthority)
        {
            if (rb != null)
            {
                rb.MovePosition(backPosition);
            }
            NetworkedPosition = backPosition;
        }
        else
        {
            transform.position = backPosition;
        }

        // ✅ ไม่ต้องหมุนตัว เพราะเป็น 2D
        // transform.rotation ไม่ต้องแก้ไข

        // ปรากฏตัว
        SetVisibility(true);
        yield return new WaitForSeconds(0.1f);

        // Phase 2: โจมตี Execution
        if (HasStateAuthority)
        {
            Character targetCharacter = targetObject?.GetComponent<Character>();
            if (targetCharacter != null)
            {
                PerformExecutionAttack(targetCharacter);
            }
        }

        IsAssassinating = false;
        Debug.Log($"🗡️ [Shadow Assassination] {CharacterName} completed assassination!");
    }

    private int CalculateExecutionDamage()
    {
        // ✅ Base damage formula: (MagicDamage × 0.7) + (AttackDamage × 0.3) + (Level × 10)
        int magicPortion = Mathf.RoundToInt(MagicDamage * 0.7f);
        int physicalPortion = Mathf.RoundToInt(AttackDamage * 0.3f);
        int levelBonus = GetCurrentLevel() * 10;

        int totalDamage = magicPortion + physicalPortion + levelBonus;

        Debug.Log($"🗡️ [Execution Damage] Magic: {magicPortion} + Physical: {physicalPortion} + Level: {levelBonus} = {totalDamage}");

        return totalDamage;
    }

    // เพิ่มก่อน Debug.Log ท้ายสุด
    private void PerformExecutionAttack(Character target)
    {
        int baseDamage = CalculateExecutionDamage();
        bool isExecutionRange = target.GetHealthPercentage() < 0.3f;

        if (isExecutionRange)
        {
            baseDamage = Mathf.RoundToInt(baseDamage * 1.5f);
            Debug.Log($"🩸 [Execution] Target HP < 30%! Damage increased by 50%!");
        }

        int physicalDamage = Mathf.RoundToInt(baseDamage * 0.3f);
        int magicDamage = Mathf.RoundToInt(baseDamage * 0.7f);

        target.TakeDamageFromAttacker(physicalDamage, magicDamage, this, DamageType.Critical, false);

        if (MagicDamage > AttackDamage)
        {
            int poisonDamage = GetScaledPoisonDamage(0.6f);
            target.ApplyStatusEffect(StatusEffectType.Poison, poisonDamage, 10f);

            if (hasVenomMastery)
            {
                SpreadPoisonToNearby(target, 6f);
            }

            Debug.Log($"🐍 [Magic Build] Applied strong poison to {target.CharacterName}!");
        }
        else
        {
            target.ApplyStatusEffect(StatusEffectType.Bleed, 8, 8f);
            target.ApplyStatusEffect(StatusEffectType.ArmorBreak, 0, 10f, 0.5f);

            Debug.Log($"⚔️ [Physical Build] Applied bleeding + armor break to {target.CharacterName}!");
        }

        // ✅ เก็บ Ferris Point
        if (!IsInShadowMode)
        {
            int ferrisGain = isExecutionRange ? 25 : 20; // เพิ่มถ้าเป็น execution
            GainFerrisPoint(ferrisGain);
        }

        Debug.Log($"🗡️ Shadow Assassination dealt {baseDamage} damage (no life steal) to {target.CharacterName} (Execution: {isExecutionRange})");
    }

    // ========== ☠️ Skill 4: Plague Outbreak (Ultimate) ==========
    public override void TryUseSkill4()
    {
        if (!CanUseSkill(skill4ManaCost)) return;

        float effectiveReduction = GetEffectiveReductionCoolDown();

        float reductionMultiplier = 1f - (effectiveReduction / 100f);

        float finalCooldown = skill4Cooldown * reductionMultiplier;

        nextSkill4Time = Time.time + finalCooldown;
        UseMana(skill4ManaCost);

        Vector3 cloudPosition = transform.position;
        RPC_CreatePlagueOutbreak(cloudPosition);
        Debug.Log($"☠️ [Skill 4] Cooldown: {skill4Cooldown}s → {finalCooldown:F1}s (Reduction: {effectiveReduction}%)");
       

    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_CreatePlagueOutbreak(Vector3 position)
    {
        // แสดง range indicator สำหรับทุกคน
        RPC_ShowRangeIndicatorAll(position, 12f);

        StartCoroutine(ExecutePlagueOutbreak(position));
    }

    private IEnumerator ExecutePlagueOutbreak(Vector3 position)
    {
        AudioManager.instance.PlaySFX(4, 1f);

        // ✅ รัศมีจริง 6m
        float plagueRadius = 6f;
        float duration = 10f;

        GameObject plagueEffect = null;
        if (plagueOutbreakEffect != null)
        {
            plagueEffect = Instantiate(plagueOutbreakEffect.gameObject, position, Quaternion.identity);

            // ✅ ลดขนาด scale ลง - ปรับให้ตรงกับรัศมี 6m
            plagueEffect.transform.localScale = Vector3.one * 1.2f; // เปลี่ยนจาก 3f → 1.2f

            ParticleSystem ps = plagueEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);
                main.startLifetime = duration;
                main.loop = true;

                ParticleSystem.ShapeModule shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = plagueRadius; // ✅ ตั้งเป็น 6m ตามรัศมีจริง
            }
        }

        float tickInterval = 1f;
        float nextTick = 0f;
        float elapsed = 0f;

        int superPoisonDamage = GetScaledPoisonDamage(0.56f);

        IsInPlagueCloud = true;
        PlagueCloudEndTime = Time.time + duration;

        Debug.Log($"☠️ [Plague Outbreak] Radius: {plagueRadius}m, Duration: {duration}s, Poison: {superPoisonDamage}/s");

        if (statusEffectManager != null)
        {
            statusEffectManager.ApplyCriticalAura(plagueRadius, 0.4f, duration);
        }

        StartCoroutine(CreatePlagueWaves(position, duration, plagueRadius)); // ✅ ส่งรัศมีไปด้วย

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= nextTick)
            {
                Collider[] enemies = Physics.OverlapSphere(position, plagueRadius, LayerMask.GetMask("Enemy"));

                int enemiesHitThisTick = 0;

                foreach (Collider col in enemies)
                {
                    Character enemy = col.GetComponent<Character>();
                    if (enemy != null)
                    {
                        enemy.ApplyStatusEffect(StatusEffectType.Poison, superPoisonDamage, 5f);
                        enemy.ApplyStatusEffect(StatusEffectType.Weakness, 0, 8f, 0.5f);

                        int directDamage = GetScaledSkillDamage(0.21f);
                        enemy.TakeDamageFromAttacker(0, directDamage, this, DamageType.Magic, false);

                        CreatePoisonHitEffect(enemy.transform.position);

                        if (Random.Range(0f, 100f) < 20f)
                        {
                            enemy.ApplyStatusEffect(StatusEffectType.Stun, 0, 2f);
                        }

                        enemiesHitThisTick++;
                        ExtendPoisonDuration(enemy);
                    }
                }

                if (!IsInShadowMode && enemiesHitThisTick > 0)
                {
                    GainFerrisPoint(enemiesHitThisTick * 2);
                }

                nextTick += tickInterval;
            }

            yield return null;
        }

        IsInPlagueCloud = false;
        RPC_HideRangeIndicatorAll();

        if (plagueEffect != null)
        {
            Destroy(plagueEffect);
        }

        Debug.Log($"☠️ [Plague Outbreak] Effect ended");
    }
    private IEnumerator CreatePlagueWaves(Vector3 center, float duration, float radius)
    {
        float elapsed = 0f;
        int waveCount = 0;

        while (elapsed < duration)
        {
            yield return new WaitForSeconds(3f);
            elapsed += 3f;
            waveCount++;

            // ✅ ปรับขนาด wave ให้ตรงกับรัศมีจริง
            if (toxicDashEffect != null)
            {
                GameObject wave = Instantiate(toxicDashEffect.gameObject, center, Quaternion.identity);

                // ✅ คำนวณ scale จากรัศมี: radius * 2 / 10 (เพราะ default size คือ 10)
                float waveScale = (radius * 2f) / 10f; // 6m * 2 / 10 = 1.2
                wave.transform.localScale = Vector3.one * waveScale;

                ParticleSystem ps = wave.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ParticleSystem.MainModule main = ps.main;
                    main.startColor = new Color(0.8f, 0.2f, 0.8f, 0.6f); // สีม่วงพิษ
                    main.startLifetime = 2f;
                }

                Destroy(wave, 3f);
            }

            Debug.Log($"🌊 Plague wave {waveCount} created! Radius: {radius}m");
        }
    }

   
    private void CreatePoisonHitEffect(Vector3 position)
    {
        if (poisonInfusionEffect != null)
        {
            GameObject hitEffect = Instantiate(poisonInfusionEffect.gameObject, position + Vector3.up * 1f, Quaternion.identity);

            ParticleSystem ps = hitEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = Color.green;
                main.startLifetime = 0.8f;
            }

            Destroy(hitEffect, 1.5f);
        }
    }

    // ========== 🐍 Passive: Venom Mastery ==========
    private void SpreadPoisonToNearby(Character sourceEnemy, float range)
    {
        if (!hasVenomMastery) return;

        Collider[] nearbyEnemies = Physics.OverlapSphere(sourceEnemy.transform.position, range, LayerMask.GetMask("Enemy"));

        int spreadCount = 0;
        foreach (Collider col in nearbyEnemies)
        {
            Character enemy = col.GetComponent<Character>();
            if (enemy != null && enemy != sourceEnemy && spreadCount < 3)
            {
                // ✅ NERF: 0.2f → 0.14f (-30%)
                int spreadDamage = GetScaledPoisonDamage(0.14f);
                enemy.ApplyStatusEffect(StatusEffectType.Poison, spreadDamage, 5f);

                // ✅ Extend poison duration
                ExtendPoisonDuration(enemy);

                if (Random.Range(0f, 100f) < 50f)
                {
                    enemy.ApplyStatusEffect(StatusEffectType.Weakness, 0, 4f, 0.2f);
                }

                spreadCount++;
                Debug.Log($"🐍 [Venom Mastery] Poison spread: {spreadDamage}/s to {enemy.CharacterName}!");
            }
        }
    }


    // ========== Override Attack for Poison Infusion ==========
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
                bool hasPoisonInfusion = PoisonInfusionStacks > 0;
                bool guaranteedCrit = PoisonInfusionStacks == 1;

                RPC_PerformAssassinAttack(nearestEnemy.Object, hasPoisonInfusion, guaranteedCrit);

                // ลด stack
                if (PoisonInfusionStacks > 0)
                {
                    PoisonInfusionStacks--;
                    Debug.Log($"🐍 Poison Infusion: {PoisonInfusionStacks} stacks remaining");
                }

                // ✅ ใช้ระบบใหม่: cooldown reduction
                float cooldownReduction = GetEffectiveAttackSpeed(); // ได้ค่า 0-0.9 (0%-90%)
                float finalAttackCooldown = AttackCooldown * (1f - cooldownReduction);

                nextAttackTime = Time.time + finalAttackCooldown;

                Debug.Log($"🐍 Assassin attack! Cooldown Reduction: {cooldownReduction * 100f}%, Final Cooldown: {finalAttackCooldown:F2}s");
            }
        }
    }

    // ใน RPC_PerformAssassinAttack method
    // เพิ่มก่อน Debug.Log ท้ายสุด
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_PerformAssassinAttack(NetworkObject enemyObject, bool shouldPoison, bool forceCritical)
    {
        if (enemyObject != null)
        {
            Character enemy = enemyObject.GetComponent<Character>();
            if (enemy != null)
            {
                bool isBasicAttack = true;

                bool shouldKnockback = Random.Range(0f, 100f) < 100f;
                if (shouldKnockback)
                {
                    Vector3 knockbackDir = (enemy.transform.position - transform.position).normalized;
                    enemy.ApplyKnockback(knockbackDir, 10f, 0.7f);
                }

                enemy.TakeDamageFromAttacker(AttackDamage, this, DamageType.Normal, isBasicAttack);

                if (shouldPoison)
                {
                    // ✅ NERF: 0.4f → 0.28f (-30%)
                    int poisonDamage = GetScaledPoisonDamage(0.28f);
                    enemy.ApplyStatusEffect(StatusEffectType.Poison, poisonDamage, 8f);

                    // ✅ Extend poison duration
                    ExtendPoisonDuration(enemy);

                    if (hasVenomMastery)
                    {
                        int bonusDamage = GetScaledSkillDamage(0.2f);
                        enemy.TakeDamageFromAttacker(0, bonusDamage, this, DamageType.Magic, false);
                    }

                    if (hasVenomMastery && Random.Range(0f, 100f) < 30f)
                    {
                        SpreadPoisonToNearby(enemy, 4f);
                    }
                }

                if (forceCritical)
                {
                    int critDamage = GetScaledSkillDamage(0.8f);
                    enemy.TakeDamageFromAttacker(0, critDamage, this, DamageType.Critical, false);
                }

                if (!IsInShadowMode)
                {
                    int ferrisGain = shouldPoison ? 3 : 2;
                    if (forceCritical) ferrisGain += 2;
                    GainFerrisPoint(ferrisGain);
                }

                RPC_OnAttackHit(enemyObject);
            }
        }
    }

    // ========== 🎨 Visual Range Indicator System ==========
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    protected override void RPC_OnAttackHit(NetworkObject enemyObject)
    {
        Debug.Log($"{CharacterName} hit enemy!");

        if (enemyObject != null)
        {
            // แสดง basic attack effect
            RPC_ShowSkillEffect("BasicAttack");

            // ถ้ามี poison infusion ให้แสดงเอฟเฟกต์พิษ
            if (PoisonInfusionStacks > 0)
            {
                CreatePoisonAttackEffect(enemyObject.transform.position);
            }

            // ถ้าเป็น critical hit ให้แสดงเอฟเฟกต์พิเศษ
            if (PoisonInfusionStacks == 1) // final strike
            {
                CreateCriticalAttackEffect(enemyObject.transform.position);
            }
        }
    }
    private void CreatePoisonAttackEffect(Vector3 position)
    {
        if (poisonInfusionEffect != null)
        {
            GameObject effect = Instantiate(poisonInfusionEffect.gameObject, position + Vector3.up * 1f, Quaternion.identity);

            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = new Color(0.2f, 0.8f, 0.2f, 0.9f); // เขียวพิษ
                main.startLifetime = 1.2f;
            }

            Destroy(effect, 2f);
        }
    }

    private void CreateCriticalAttackEffect(Vector3 position)
    {
        if (shadowAssassinEffect != null)
        {
            GameObject effect = Instantiate(shadowAssassinEffect.gameObject, position + Vector3.up * 2f, Quaternion.identity);
            effect.transform.localScale = Vector3.one * 2f; // ขยายให้ใหญ่

            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = new Color(1f, 0.8f, 0f, 1f); // สีทอง
                main.startLifetime = 1.5f;
            }

            Destroy(effect, 2.5f);
        }
    }
    private void CreateRangeIndicator()
    {
        if (plagueOutbreakEffect != null)
        {
            plagueRangeIndicator = Instantiate(plagueOutbreakEffect.gameObject, transform);
            plagueRangeIndicator.name = $"{CharacterName}_PlagueRangeIndicator";

            ParticleSystem ps = plagueRangeIndicator.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = new Color(0.5f, 0f, 1f, 0.6f); // สีม่วงโปร่งใส
                main.startLifetime = 1f;
                main.loop = true;

                ParticleSystem.ShapeModule shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = 6f; // ✅ ตั้งเป็น 6m ตามรัศมีจริง
            }

            plagueRangeIndicator.SetActive(false);
        }
        else
        {
            Debug.LogWarning("PlagueOutbreakEffect is not assigned!");
        }
    }
    // เพิ่มหลัง Start()
    protected override void OnFerrisPointFull()
    {
        base.OnFerrisPointFull();

        if (!IsInShadowMode)
        {
            EnterShadowMode();
        }
    }

    /// <summary>
    /// เข้าสู่โหมด Shadow Mode (เมื่อ Ferris Point เต็ม)
    /// </summary>
    private void EnterShadowMode()
    {
        if (!HasStateAuthority) return;

        IsInShadowMode = true;
        Skill3UsesRemaining = 3;
        CanDashAgain = false;
        accumulatedDrain = 0f;
        statusEffectManager.ApplyMoveSpeedAura(6f, 0.50f, 10f);
        ResetAllSkillCooldowns();

        RPC_ShowShadowModeText();
        Debug.Log($"🌙 [SHADOW MODE ACTIVATED] {CharacterName} - Skill 3: 3 uses, Skill 2: Double Dash!");
        if (HasInputAuthority)
        {
            CombatUIManager uiManager = FindObjectOfType<CombatUIManager>();
            if (uiManager != null)
            {
                // เปลี่ยนเป็น upgraded icons
                SkillIconManager.Instance.SetSkillIconUpgraded(uiManager, "Assassin", 2, true);
                SkillIconManager.Instance.SetSkillIconUpgraded(uiManager, "Assassin", 3, true);
            }
        }
        // Visual effect
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowShadowModeText()
    {
        // แสดงข้อความเหนือหัวตัวละคร
        Vector3 textPosition = transform.position + Vector3.up * 1f; // สูงกว่าหัว

        // สร้างข้อความ "SHADOW MODE" สีม่วง
        Color shadowColor = new Color(0.7f, 0.3f, 1f, 1f); // สีม่วงสว่าง
        DamageTextManager.ShowCustomText(textPosition, "Ferris MODE", shadowColor, 1f);

        Debug.Log($"🌙 [Visual] Shadow Mode text displayed for {CharacterName}");
    }
    public override bool UsesFerrisPoint()
    {
        return true;
    }
    /// <summary>
    /// ออกจากโหมด Shadow Mode
    /// </summary>
    private void ExitShadowMode()
    {
        if (!HasStateAuthority) return;

        IsInShadowMode = false;
        Skill3UsesRemaining = 0;
        CanDashAgain = false;
        CurrentFerrisPoint = 0;
        accumulatedDrain = 0f;
        RPC_ShowShadowModeEndText();

        Debug.Log($"🌙 [Shadow Mode Ended] {CharacterName}");
        if (HasInputAuthority)
        {
            CombatUIManager uiManager = FindObjectOfType<CombatUIManager>();
            if (uiManager != null)
            {
                // เปลี่ยนกลับเป็น normal icons
                SkillIconManager.Instance.SetSkillIconUpgraded(uiManager, "Assassin", 2, false);
                SkillIconManager.Instance.SetSkillIconUpgraded(uiManager, "Assassin", 3, false);
            }
        }
        // Visual effect
        RPC_ShowShadowModeDeactivation();
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowShadowModeEndText()
    {
        Vector3 textPosition = transform.position + Vector3.up * 1f;

        // ข้อความสีเทา
        Color endColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        DamageTextManager.ShowCustomText(textPosition, "Ferris MODE End", endColor, 1.0f);

        Debug.Log($"🌙 [Visual] Shadow Mode end text displayed for {CharacterName}");
    }
    /// <summary>
    /// ลด Ferris Points ระหว่าง Shadow Mode
    /// </summary>
    private void DrainFerrisPoints(float deltaTime)
    {
        if (!IsInShadowMode) return;

        // สะสม drain amount
        accumulatedDrain += shadowModeDrainRate * deltaTime;

        // เมื่อสะสมได้ >= 1 point ให้ลดทีละ 1 point
        if (accumulatedDrain >= 1f)
        {
            int drainAmount = Mathf.FloorToInt(accumulatedDrain);
            CurrentFerrisPoint = Mathf.Max(0, CurrentFerrisPoint - drainAmount);

            accumulatedDrain -= drainAmount;

            Debug.Log($"🌙 [Shadow Mode Drain] {CharacterName}: -{drainAmount} points, {CurrentFerrisPoint} remaining");

            // Check if should exit Shadow Mode
            if (CurrentFerrisPoint <= 0)
            {
                accumulatedDrain = 0f;
                ExitShadowMode();
            }
        }
    }
    private Material CreateRangeIndicatorMaterial()
    {
        // สร้าง material สำหรับ range indicator
        if (skillRangeIndicatorMaterial != null)
        {
            return skillRangeIndicatorMaterial;
        }

        // สร้าง default material
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(0.5f, 0f, 1f, 0.6f); // สีม่วงโปร่งใส
        return mat;
    }

    private void ShowRangeIndicator(Vector3 center, float radius)
    {
        if (plagueRangeIndicator == null) return;

        plagueRangeIndicator.transform.position = center;

        // ✅ ปรับขนาดตามรัศมี (6m)
        ParticleSystem ps = plagueRangeIndicator.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.radius = radius; // 6m
        }

        plagueRangeIndicator.SetActive(true);

        // เริ่ม animation effect
        StartCoroutine(AnimateParticleRangeIndicator());

        Debug.Log($"🎨 [Particle Range Indicator] Showing plague range: {radius}m");
    }
    private IEnumerator AnimateParticleRangeIndicator()
    {
        if (plagueRangeIndicator == null) yield break;

        ParticleSystem ps = plagueRangeIndicator.GetComponent<ParticleSystem>();
        if (ps == null) yield break;

        ParticleSystem.MainModule main = ps.main;
        Color originalColor = main.startColor.color;

        while (plagueRangeIndicator.activeInHierarchy)
        {
            // สร้าง pulsing effect
            float alpha = 0.3f + 0.3f * Mathf.Sin(Time.time * 3f);
            Color newColor = originalColor;
            newColor.a = alpha;
            main.startColor = newColor;

            yield return null;
        }

        // รีเซ็ตสีเมื่อเสร็จสิ้น
        main.startColor = originalColor;
    }

    private void HideRangeIndicator()
    {
        if (plagueRangeIndicator != null)
        {
            plagueRangeIndicator.SetActive(false);
            Debug.Log($"🎨 [Range Indicator] Hidden");
        }
    }

    private IEnumerator AnimateRangeIndicator()
    {
        if (rangeCircle == null) yield break;

        float animationTime = 0f;
        Color originalColor = rangeCircle.endColor;

        while (plagueRangeIndicator.activeInHierarchy)
        {
            animationTime += Time.deltaTime;

            // สร้าง pulsing effect
            float alpha = 0.3f + 0.3f * Mathf.Sin(animationTime * 3f);
            Color newColor = originalColor;
            newColor.a = alpha;
            rangeCircle.endColor = newColor;

            yield return null;
        }

        // รีเซ็ตสีเมื่อเสร็จสิ้น
        rangeCircle.endColor = originalColor;
    }

    // ========== Helper Methods - FIXED ==========
    private void SetVisibility(bool visible)
    {
        // เอฟเฟกต์ก่อนเปลี่ยน visibility
        if (!visible)
        {
            RPC_ShowSkillEffect("Invisibility");
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = visible;
        }

        IsInvisible = !visible;

        // เอฟเฟกต์หลังจาก appear
        if (visible)
        {
            StartCoroutine(ShowAppearEffect());
        }
    }
    private IEnumerator ShowAppearEffect()
    {
        yield return new WaitForSeconds(0.1f);

        if (shadowAssassinEffect != null)
        {
            GameObject effect = Instantiate(shadowAssassinEffect.gameObject, transform.position, Quaternion.identity);

            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = Color.cyan;
                main.startLifetime = 0.8f;
            }

            Destroy(effect, 1.5f);
        }
    }
    private bool CanUseSkill(int manaCost)
    {
        // ✅ ลบการเช็ค authority - ให้ทุกคนเช็คได้
        if (CurrentMana < manaCost)
        {
            Debug.Log($"❌ Not enough mana! Need {manaCost}, have {CurrentMana}");
            return false;
        }

        if (HasStatusEffect(StatusEffectType.Stun))
        {
            Debug.Log($"❌ Cannot use skill while stunned!");
            return false;
        }

        return true;
    }

    private int GetScaledPoisonDamage(float multiplier)
    {
        int magicPortion = Mathf.RoundToInt(MagicDamage * 0.7f);
        int physicalPortion = Mathf.RoundToInt(AttackDamage * 0.3f);
        int levelBonus = GetCurrentLevel() * 10;

        int baseDamage = magicPortion + physicalPortion + levelBonus;

        // ✅ ลดดาเมจ 30% (multiplier ถูกลดไว้แล้วตอนเรียก)
        int scaledDamage = Mathf.RoundToInt(baseDamage * multiplier);
        int finalDamage = ApplyAmpDamageToSkill(scaledDamage);

        return Mathf.Max(1, finalDamage);
    }
    private int GetScaledSkillDamage(float multiplier)
    {
        // คำนวณ base damage
        int magicPortion = Mathf.RoundToInt(MagicDamage * 0.7f);
        int physicalPortion = Mathf.RoundToInt(AttackDamage * 0.3f);
        int levelBonus = GetCurrentLevel() * 10;

        int baseDamage = magicPortion + physicalPortion + levelBonus;
        int scaledDamage = Mathf.RoundToInt(baseDamage * multiplier);

        // ✅ FIXED: Apply Amp Damage ให้ skill damage
        int finalDamage = ApplyAmpDamageToSkill(scaledDamage);

        Debug.Log($"🐍 [Skill Damage] Base: {baseDamage} × {multiplier} = {scaledDamage} → With Amp: {finalDamage}");
        Debug.Log($"    (Magic: {magicPortion}, Physical: {physicalPortion}, Level: {levelBonus})");

        return Mathf.Max(1, finalDamage);
    }


    private Vector3 GetDashDirection()
    {
        // ✅ ใช้ input จาก Joystick เป็นหลัก
        if (GetInput(out NetworkInputData input))
        {
            // ถ้ามีการกด Joystick ให้พุ่งไปทางนั้น
            if (input.movementInput.magnitude > 0.1f)
            {
                Vector3 moveDir = new Vector3(
                    input.movementInput.x,
                    0,
                    input.movementInput.y
                );

                Debug.Log($"🎮 Dash direction from joystick: {moveDir.normalized}");
                return moveDir.normalized;
            }
        }

        // Fallback: ใช้ทิศทางกล้องหรือตัวละคร
        if (cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            forward.y = 0;
            return forward.normalized;
        }

        return transform.forward;
    }

    // ========== RPC Methods ==========
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowRangeIndicatorAll(Vector3 center, float radius)
    {
        ShowRangeIndicator(center, radius);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HideRangeIndicatorAll()
    {
        HideRangeIndicator();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowSkillEffect(string effectName)
    {
        Debug.Log($"✨ [Skill Effect] {CharacterName} - {effectName}");

        switch (effectName)
        {
            case "PoisonInfusion":
                ShowPoisonInfusionEffect();
                break;
            case "ToxicDash":
                ShowToxicDashEffect();
                break;
            case "ShadowAssassination":
                ShowShadowAssassinationEffect();
                break;
            case "PlagueOutbreak":
                ShowPlagueOutbreakEffect();
                break;
            case "BasicAttack":
                ShowBasicAttackEffect();
                break;
                
                
        }
    }
    private void ShowPoisonInfusionEffect()
    {
        AudioManager.instance.PlaySFX(0,0.3f);
        if (poisonInfusionEffect != null)
        {
            GameObject effect = Instantiate(poisonInfusionEffect.gameObject, transform.position + Vector3.up * 1f, Quaternion.identity);

            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = Color.green;
                main.startLifetime = 2f;
            }

            Destroy(effect, 3f);
        }
    }

    private void ShowToxicDashEffect()
    {
        AudioManager.instance.PlaySFX(0,0.2f);
        if (toxicDashEffect != null)
        {
            GameObject effect = Instantiate(toxicDashEffect.gameObject, transform.position, Quaternion.identity);

            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = new Color(0.5f, 1f, 0f, 0.8f); // เขียวพิษ
                main.startLifetime = 1.5f;
            }

            Destroy(effect, 2f);
        }
    }

    private void ShowShadowAssassinationEffect()
    {
        AudioManager.instance.PlaySFX(1,0.2f);
        if (shadowAssassinEffect != null)
        {
            GameObject effect = Instantiate(shadowAssassinEffect.gameObject, transform.position + Vector3.up * 2f, Quaternion.identity);

            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = new Color(0.3f, 0f, 0.8f, 0.9f); // สีม่วงเข้ม
                main.startLifetime = 1f;
            }

            Destroy(effect, 2f);
        }
    }

    private void ShowPlagueOutbreakEffect()
    {
        AudioManager.instance.PlaySFX(0,0.2f);
        if (plagueOutbreakEffect != null)
        {
            GameObject effect = Instantiate(plagueOutbreakEffect.gameObject, transform.position, Quaternion.identity);
            effect.transform.localScale = Vector3.one * 3f; // ขยายให้ใหญ่

            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = new Color(0.2f, 0.8f, 0.2f, 0.7f); // เขียวอ่อน
                main.startLifetime = 3f;
            }

            Destroy(effect, 5f);
        }
    }

    private void ShowBasicAttackEffect()
    {
        if (basicAttackEffect != null)
        {
            GameObject effect = Instantiate(basicAttackEffect.gameObject, transform.position + Vector3.up , Quaternion.identity);

            // ✅ Flip effect ตามทิศทางของตัวละคร
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

            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFX(0, 0.1f);
            }
            else
            {
                Debug.LogError("❌ AudioManager.instance is NULL!");
            }

            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = Color.white;
                main.startLifetime = 0.5f;
            }

            Destroy(effect, 1f);
        }
    }

  

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SyncPosition(Vector3 newPosition)
    {
        NetworkedPosition = newPosition;
        transform.position = newPosition;
    }

    // ========== Network Update Override ==========
    // แทนที่ FixedUpdateNetwork() เดิม
    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        // ✅ Sync position เมื่อมีการเปลี่ยนแปลง
        if (HasInputAuthority && !HasStateAuthority)
        {
            if (Vector3.Distance(transform.position, NetworkedPosition) > 0.1f)
            {
                RPC_SyncPosition(transform.position);
            }
        }

        // Update status
        if (IsInPlagueCloud && Time.time > PlagueCloudEndTime)
        {
            IsInPlagueCloud = false;
        }

        // ✅ Sync visibility
        if (!HasInputAuthority)
        {
            SetVisibility(!IsInvisible);
        }

        // ✅ Shadow Mode: Drain Ferris Points
        if (HasStateAuthority && IsInShadowMode)
        {
            DrainFerrisPoints(Runner.DeltaTime);
        }

        // ✅ Shadow Mode: Grace Period Check (Skill 2 Double Dash)
        if (HasStateAuthority && CanDashAgain && Time.time > DashGracePeriodEnd)
        {
            // หมดเวลา grace period - ติด cooldown
            float effectiveReduction = GetEffectiveReductionCoolDown();
            float reductionMultiplier = 1f - (effectiveReduction / 100f);
            reductionMultiplier = Mathf.Clamp(reductionMultiplier, 0.1f, 1f);
            float finalCooldown = skill2Cooldown * reductionMultiplier;

            nextSkill2Time = Time.time + finalCooldown;
            CanDashAgain = false;

            Debug.Log($"⏱️ [Grace Period Expired] Skill 2 cooldown applied: {finalCooldown:F1}s");
        }
    }
    

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowShadowModeDeactivation()
    {
        if (shadowAssassinEffect != null)
        {
            GameObject effect = Instantiate(shadowAssassinEffect.gameObject, transform.position + Vector3.up * 1f, Quaternion.identity);

            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); // สีเทา
                main.startLifetime = 1f;
            }

            Destroy(effect, 2f);
        }

        Debug.Log($"🌙 [Visual] Shadow Mode Deactivated!");
    }
    // ========== Event Handlers ==========
    protected override void OnDestroy()
    {
        base.OnDestroy();

        // ทำลาย range indicator
        if (plagueRangeIndicator != null)
        {
            Destroy(plagueRangeIndicator);
        }
        StatusEffectManager.OnStatusDamage -= HandlePoisonKillForManaRestore;

    }
    private void HandlePoisonKillForManaRestore(Character target, int damage, DamageType damageType)
    {
        if (!HasStateAuthority) return;
        if (target == null || target == this) return;

        // เช็คว่าศัตรูตายด้วยพิษหรือไม่
        if (damageType == DamageType.Poison && target.CurrentHp <= 0)
        {
            // คืน Mana
            CurrentMana = Mathf.Min(CurrentMana + manaRestoreOnPoisonKill, MaxMana);

            Debug.Log($"🐍 [Venom Mastery] {target.CharacterName} died from poison! Restored {manaRestoreOnPoisonKill} Mana → {CurrentMana}/{MaxMana}");

            // แสดงข้อความ
            RPC_ShowManaRestoreText(manaRestoreOnPoisonKill);
        }
    }

    /// <summary>
    /// ✅ ENHANCED: เมื่อใส่พิษซ้ำ → duration +2s (max 15s)
    /// </summary>
    private void ExtendPoisonDuration(Character target)
    {
        if (!HasStateAuthority) return;
        if (target == null) return;

        StatusEffectManager targetStatus = target.GetComponent<StatusEffectManager>();
        if (targetStatus != null && targetStatus.IsPoisoned)
        {
            // เพิ่ม duration แต่ไม่เกิน max
            float newDuration = Mathf.Min(
                targetStatus.PoisonDuration + poisonDurationExtension,
                maxPoisonDuration
            );

            targetStatus.PoisonDuration = newDuration;

            Debug.Log($"🐍 [Venom Mastery] Extended poison on {target.CharacterName}: +{poisonDurationExtension}s → {newDuration:F1}s (max: {maxPoisonDuration}s)");
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowManaRestoreText(int amount)
    {
        Vector3 textPosition = transform.position + Vector3.up * 1.5f;
        Color manaColor = new Color(0.3f, 0.6f, 1f, 1f); // น้ำเงิน
        DamageTextManager.ShowCustomText(textPosition, $"+{amount} MANA", manaColor, 0.8f);
    }
    private Character FindNearestEnemy(float range)
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, range, LayerMask.GetMask("Enemy"));

        Character nearestEnemy = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider col in enemies)
        {
            Character enemy = col.GetComponent<Character>();
            if (enemy != null && enemy.CurrentHp > 0)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = enemy;
                }
            }
        }

        return nearestEnemy;
    }

    /// <summary>
    /// หาศัตรูหลายตัวที่ใกล้ที่สุดภายในระยะที่กำหนด (สำหรับ Ferris Mode)
    /// </summary>
    private List<Character> FindNearestEnemies(float range, int maxCount)
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, range, LayerMask.GetMask("Enemy"));

        List<Character> validEnemies = new List<Character>();

        // เก็บศัตรูที่ยังมีชีวิต
        foreach (Collider col in enemies)
        {
            Character enemy = col.GetComponent<Character>();
            if (enemy != null && enemy.CurrentHp > 0)
            {
                validEnemies.Add(enemy);
            }
        }

        // เรียงตามระยะทาง (ใกล้ไปไกล)
        validEnemies.Sort((a, b) =>
        {
            float distA = Vector3.Distance(transform.position, a.transform.position);
            float distB = Vector3.Distance(transform.position, b.transform.position);
            return distA.CompareTo(distB);
        });

        // ตัดให้เหลือแค่ maxCount ตัว
        if (validEnemies.Count > maxCount)
        {
            validEnemies = validEnemies.GetRange(0, maxCount);
        }

        Debug.Log($"🎯 Found {validEnemies.Count} enemies within {range}m range");
        return validEnemies;
    }
}