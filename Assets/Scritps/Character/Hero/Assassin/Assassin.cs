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

        if (statusEffectManager != null)
        {
            statusEffectManager.ApplyAttackSpeedAura(6f, 0.3f, 12f);
            Debug.Log($"✅ Applied Attack Speed Aura (+30% for 12s in 6m radius)");
        }

        Debug.Log($"🐍 [Poison Infusion] {CharacterName} gains 3 poison-infused attacks!");
        RPC_ShowSkillEffect("PoisonInfusion");
    }

    // ========== 🌫️ Skill 2: Toxic Dash - FIXED ==========
    public override void TryUseSkill2()
    {
        if (!CanUseSkill(skill2ManaCost)) return;
        if (HasStatusEffect(StatusEffectType.Stun)) return;

        // ✅ คำนวณ cooldown reduction ที่ถูกต้อง
        float effectiveReduction = GetEffectiveReductionCoolDown();
        float reductionMultiplier = 1f - (effectiveReduction / 100f);
        reductionMultiplier = Mathf.Clamp(reductionMultiplier, 0.1f, 1f);

        float finalCooldown = skill2Cooldown * reductionMultiplier;
        nextSkill2Time = Time.time + finalCooldown;

        UseMana(skill2ManaCost);

        if (statusEffectManager != null)
        {
            statusEffectManager.ApplyMoveSpeedAura(5f, 0.25f, 10f);
            Debug.Log($"✅ Applied Move Speed Aura (+25% for 10s)");
        }

        Vector3 dashDirection = GetDashDirection();

        RPC_PerformToxicDashAll(dashDirection, transform.position);
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

    private void ApplyToxicDashEffects(Character enemy)
    {
        // ✅ FIXED: ใช้ GetScaledSkillDamage ที่มี Amp Damage แล้ว
        int directDamage = GetScaledSkillDamage(0.6f);
        int poisonDamage = GetScaledPoisonDamage(0.3f);

        // ✅ ทำ direct magic damage - เป็น skill attack
        enemy.TakeDamageFromAttacker(0, directDamage, this, DamageType.Magic, false);

        // 🆕 Apply Knockback - พุ่งศัตรูไปข้างหน้า
        Vector3 dashDirection = GetDashDirection();
        enemy.ApplyKnockback(dashDirection, 4f, 0.3f);
        Debug.Log($"🌫️ [Toxic Dash Knockback] {enemy.CharacterName} knocked back!");

        // ใส่ status effects
        enemy.ApplyStatusEffect(StatusEffectType.Poison, poisonDamage, 8f);
        enemy.ApplyStatusEffect(StatusEffectType.Weakness, 0, 6f, 0.3f);
        enemy.ApplyStatusEffect(StatusEffectType.Blind, 0, 4f, 0.6f);

        // Passive: โอกาส spread poison
        if (hasVenomMastery && Random.Range(0f, 100f) < 25f)
        {
            SpreadPoisonToNearby(enemy, 4f);
        }

        Debug.Log($"🌫️ Toxic Dash hit {enemy.CharacterName} (with Amp)! Direct: {directDamage}, Poison: {poisonDamage}/s");
    }


    // ========== 💣 Skill 3: Shadow Assassination - FIXED ==========
    public override void TryUseSkill3()
    {
        if (!CanUseSkill(skill3ManaCost)) return;

        // ตรวจสอบเป้าหมายก่อน
        Collider[] enemies = Physics.OverlapSphere(transform.position, 8f, LayerMask.GetMask("Enemy"));
        Character targetEnemy = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider col in enemies)
        {
            Character enemy = col.GetComponent<Character>();
            if (enemy != null)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    targetEnemy = enemy;
                }
            }
        }

        if (targetEnemy == null)
        {
            Debug.Log("❌ No target found for Shadow Assassination!");
            return;
        }

        // ✅ ตั้ง cooldown เฉพาะเมื่อมีเป้าหมายและใช้สกิลสำเร็จแล้ว
        float effectiveReduction = GetEffectiveReductionCoolDown();
        float reductionMultiplier = 1f - (effectiveReduction / 100f);
        reductionMultiplier = Mathf.Clamp(reductionMultiplier, 0.1f, 1f);

        float finalCooldown = skill3Cooldown * reductionMultiplier;
        nextSkill3Time = Time.time + finalCooldown;

        UseMana(skill3ManaCost);

        if (statusEffectManager != null)
        {
            statusEffectManager.ApplyCriticalAura(6f, 0.4f, 12f);
            Debug.Log($"✅ Applied Critical Aura (+40% for 12s)");
        }

        Vector3 originalPos = transform.position;
        NetworkObject targetNetworkObject = targetEnemy.GetComponent<NetworkObject>();
        if (targetNetworkObject != null)
        {
            RPC_PerformShadowAssassinationAll(targetEnemy.transform.position, targetNetworkObject, originalPos);
        }
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

    private void PerformExecutionAttack(Character target)
    {
        // คำนวณ base damage
        int baseDamage = CalculateExecutionDamage();

        // เช็ค execution bonus
        bool isExecutionRange = target.GetHealthPercentage() < 0.3f;

        if (isExecutionRange)
        {
            baseDamage = Mathf.RoundToInt(baseDamage * 1.5f);
            Debug.Log($"🩸 [Execution] Target HP < 30%! Damage increased by 50%!");
        }

        // ✅ Shadow Assassination เป็น skill attack - ไม่มี life steal
        int physicalDamage = Mathf.RoundToInt(baseDamage * 0.3f);
        int magicDamage = Mathf.RoundToInt(baseDamage * 0.7f);

        target.TakeDamageFromAttacker(physicalDamage, magicDamage, this, DamageType.Critical, false);

        // เพิ่ม effect ตาม build
        if (MagicDamage > AttackDamage) // Magic Build
        {
            int poisonDamage = GetScaledPoisonDamage(0.6f);
            target.ApplyStatusEffect(StatusEffectType.Poison, poisonDamage, 10f);

            // Venom Mastery: spread poison
            if (hasVenomMastery)
            {
                SpreadPoisonToNearby(target, 6f);
            }

            Debug.Log($"🐍 [Magic Build] Applied strong poison to {target.CharacterName}!");
        }
        else // Physical Build
        {
            // ใส่ bleeding effect
            target.ApplyStatusEffect(StatusEffectType.Bleed, 8, 8f);
            target.ApplyStatusEffect(StatusEffectType.ArmorBreak, 0, 10f, 0.5f);

            Debug.Log($"⚔️ [Physical Build] Applied bleeding + armor break to {target.CharacterName}!");
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

        // สร้าง visual effect ด้วย Particle System
        GameObject plagueEffect = null;
        if (plagueOutbreakEffect != null)
        {
            plagueEffect = Instantiate(plagueOutbreakEffect.gameObject, position, Quaternion.identity);
            plagueEffect.transform.localScale = Vector3.one * 5f; // ขยายให้ใหญ่มาก

            ParticleSystem ps = plagueEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = new Color(0.2f, 0.8f, 0.2f, 0.8f); // เขียวพิษ
                main.startLifetime = 20f; // ยาวเท่ากับ duration
                main.loop = true;

                // ปรับ shape ให้เป็นวงกลมใหญ่
                ParticleSystem.ShapeModule shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = 12f;
            }
        }
        else
        {
            Debug.LogWarning("PlagueOutbreakEffect is not assigned!");
        }

        float duration = 20f;
        float tickInterval = 1f;
        float nextTick = 0f;
        float elapsed = 0f;

        // ✅ Super Poison damage ใหม่
        int superPoisonDamage = GetScaledPoisonDamage(0.8f);

        IsInPlagueCloud = true;
        PlagueCloudEndTime = Time.time + duration;

        Debug.Log($"☠️ [Plague Outbreak] {CharacterName} creates massive poison cloud! ({superPoisonDamage} damage/s)");

        // ✅ ให้ team auras
        if (statusEffectManager != null)
        {
            statusEffectManager.ApplyAttackSpeedAura(12f, 0.5f, 20f);
            statusEffectManager.ApplyDamageAura(12f, 0.4f, 20f);
            statusEffectManager.ApplyProtectionAura(12f, 0.25f, 20f);
            statusEffectManager.ApplyCriticalAura(12f, 0.3f, 20f);

            Debug.Log($"💚 [Plague Outbreak] Team Auras activated!");
        }

        // เพิ่ม secondary effects ทุก 3 วินาที
        StartCoroutine(CreatePlagueWaves(position, duration));

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= nextTick)
            {
                // ผลกระทบต่อศัตรู
                Collider[] enemies = Physics.OverlapSphere(position, 12f, LayerMask.GetMask("Enemy"));
                foreach (Collider col in enemies)
                {
                    Character enemy = col.GetComponent<Character>();
                    if (enemy != null)
                    {
                        // Super Poison
                        enemy.ApplyStatusEffect(StatusEffectType.Poison, superPoisonDamage, 5f);
                        enemy.ApplyStatusEffect(StatusEffectType.Blind, 0, 8f, 0.8f);
                        enemy.ApplyStatusEffect(StatusEffectType.Weakness, 0, 8f, 0.5f);
                        enemy.ApplyStatusEffect(StatusEffectType.ArmorBreak, 0, 8f, 0.6f);

                        // ✅ FIX: Direct magic damage - skill damage ไม่มี life steal
                        int directDamage = GetScaledSkillDamage(0.3f);
                        enemy.TakeDamageFromAttacker(0, directDamage, this, DamageType.Magic, false);

                        // สร้าง hit effect
                        CreatePoisonHitEffect(enemy.transform.position);

                        // โอกาส stun
                        if (Random.Range(0f, 100f) < 20f)
                        {
                            enemy.ApplyStatusEffect(StatusEffectType.Stun, 0, 2f);
                            Debug.Log($"☠️ {enemy.CharacterName} stunned by plague cloud!");
                        }
                    }
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
    private IEnumerator CreatePlagueWaves(Vector3 center, float duration)
    {
        float elapsed = 0f;
        int waveCount = 0;

        while (elapsed < duration)
        {
            yield return new WaitForSeconds(3f);
            elapsed += 3f;
            waveCount++;

            // สร้างคลื่นพิษ
            if (toxicDashEffect != null)
            {
                GameObject wave = Instantiate(toxicDashEffect.gameObject, center, Quaternion.identity);
                wave.transform.localScale = Vector3.one * (2f + waveCount * 0.5f);

                ParticleSystem ps = wave.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ParticleSystem.MainModule main = ps.main;
                    main.startColor = new Color(0.8f, 0.2f, 0.8f, 0.6f); // สีม่วงพิษ
                    main.startLifetime = 2f;
                }

                Destroy(wave, 3f);
            }

            Debug.Log($"🌊 Plague wave {waveCount} created!");
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
                // ✅ ใช้สูตรใหม่
                int spreadDamage = GetScaledPoisonDamage(0.2f); // 20% ของ base formula
                enemy.ApplyStatusEffect(StatusEffectType.Poison, spreadDamage, 5f);

                // โอกาสใส่ weakness
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
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_PerformAssassinAttack(NetworkObject enemyObject, bool shouldPoison, bool forceCritical)
    {
        if (enemyObject != null)
        {
            Character enemy = enemyObject.GetComponent<Character>();
            if (enemy != null)
            {
                // ✅ FIXED: การโจมตีปกติของ Assassin ยังคงเป็น basic attack (ไม่ใช้ Amp Damage)
                bool isBasicAttack = true;

                Debug.Log($"🐍 [Assassin Attack] shouldPoison: {shouldPoison}, forceCritical: {forceCritical}, isBasicAttack: {isBasicAttack}");
                bool shouldKnockback = Random.Range(0f, 100f) < 100f;

                // Basic attack ไม่ใช้ Amp Damage
                if (shouldKnockback)
                {
                    Vector3 knockbackDir = (enemy.transform.position - transform.position).normalized;
                    enemy.ApplyKnockback(knockbackDir, 10f, 0.7f); // แรงเบา ระยะสั้น
                    Debug.Log($"🐍 [Knockback] Basic attack knocked back {enemy.CharacterName}!");
                }

                // Basic attack ไม่ใช้ Amp Damage
                enemy.TakeDamageFromAttacker(AttackDamage, this, DamageType.Normal, isBasicAttack);

                // ✅ Poison effect จาก skill - ใช้ Amp Damage
                if (shouldPoison)
                {
                    int poisonDamage = GetScaledPoisonDamage(0.4f); // จะมี Amp Damage อยู่แล้ว
                    enemy.ApplyStatusEffect(StatusEffectType.Poison, poisonDamage, 8f);

                    // ✅ Venom Mastery bonus - เป็น skill damage ใช้ Amp Damage
                    if (hasVenomMastery)
                    {
                        int bonusDamage = GetScaledSkillDamage(0.2f); // จะมี Amp Damage อยู่แล้ว
                        enemy.TakeDamageFromAttacker(0, bonusDamage, this, DamageType.Magic, false); // skill attack
                        Debug.Log($"🐍 [Venom Mastery] poison bonus (with Amp): {bonusDamage}");
                    }

                    // Passive: spread poison
                    if (hasVenomMastery && Random.Range(0f, 100f) < 30f)
                    {
                        SpreadPoisonToNearby(enemy, 4f);
                    }

                    Debug.Log($"🐍 Applied poison: {poisonDamage} damage/s for 8s");
                }

                // ✅ Force critical - เป็น skill damage ใช้ Amp Damage
                if (forceCritical)
                {
                    int critDamage = GetScaledSkillDamage(0.8f); // จะมี Amp Damage อยู่แล้ว
                    enemy.TakeDamageFromAttacker(0, critDamage, this, DamageType.Critical, false); // skill attack
                    Debug.Log($"🐍 [Poison Infusion] Final strike (with Amp): {critDamage} damage");
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
        // ใช้ Particle System แทน LineRenderer
        if (plagueOutbreakEffect != null)
        {
            plagueRangeIndicator = Instantiate(plagueOutbreakEffect.gameObject, transform);
            plagueRangeIndicator.name = $"{CharacterName}_PlagueRangeIndicator";

            // ปรับขนาดและสี
            ParticleSystem ps = plagueRangeIndicator.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = new Color(0.5f, 0f, 1f, 0.6f); // สีม่วงโปร่งใส
                main.startLifetime = 1f;
                main.loop = true;

                ParticleSystem.ShapeModule shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = 12f; // รัศมี ultimate
            }

            plagueRangeIndicator.SetActive(false);
        }
        else
        {
            Debug.LogWarning("PlagueOutbreakEffect is not assigned!");
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

        // ปรับขนาดตามรัศมี
        ParticleSystem ps = plagueRangeIndicator.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.radius = radius;
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
        // คำนวณ base damage
        int magicPortion = Mathf.RoundToInt(MagicDamage * 0.7f);
        int physicalPortion = Mathf.RoundToInt(AttackDamage * 0.3f);
        int levelBonus = GetCurrentLevel() * 10;

        int baseDamage = magicPortion + physicalPortion + levelBonus;
        int scaledDamage = Mathf.RoundToInt(baseDamage * multiplier);

        // ✅ FIXED: Apply Amp Damage ให้ poison damage (เป็น skill effect)
        int finalDamage = ApplyAmpDamageToSkill(scaledDamage);

        Debug.Log($"🐍 [Poison Damage] Base: {baseDamage} × {multiplier} = {scaledDamage} → With Amp: {finalDamage}");
        Debug.Log($"    (Magic: {magicPortion}, Physical: {physicalPortion}, Level: {levelBonus})");

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
            case "Invisibility":
                ShowInvisibilityEffect();
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

    private void ShowInvisibilityEffect()
    {
        if (invisibilityEffect != null)
        {
            GameObject effect = Instantiate(invisibilityEffect.gameObject, transform.position, Quaternion.identity);

            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = new Color(0.8f, 0.8f, 0.8f, 0.3f); // เทาโปร่งใส
                main.startLifetime = 1f;
            }

            Destroy(effect, 2f);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SyncPosition(Vector3 newPosition)
    {
        NetworkedPosition = newPosition;
        transform.position = newPosition;
    }

    // ========== Network Update Override ==========
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
    }
}