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
        skill1Cooldown = 8f;
        skill2Cooldown = 12f;
        skill3Cooldown = 15f;
        skill4Cooldown = 20f;

        // สร้าง range indicator สำหรับ ultimate
        CreateRangeIndicator();

        Debug.Log($"🐍 Assassin {CharacterName} initialized with Venom Mastery!");
    }

    // ========== 💚 Skill 1: Poison Infusion ==========
    protected override void TryUseSkill1()
    {
        if (!CanUseSkill(skill1ManaCost)) return;

        UseMana(skill1ManaCost);

        // เริ่ม Poison Infusion - ให้ 3 charges
        PoisonInfusionStacks = 3;

        // ✅ 🌟 เปลี่ยน: ให้ Attack Speed Aura แทนการ buff ตัวเอง
        if (statusEffectManager != null)
        {
            statusEffectManager.ApplyAttackSpeedAura(6f, 0.3f, 12f); // +30% attack speed, 6m radius, 12s
            Debug.Log($"✅ Applied Attack Speed Aura (+30% for 12s in 6m radius)");
        }

        Debug.Log($"🐍 [Poison Infusion] {CharacterName} gains 3 poison-infused attacks!");

        // Visual effect
        RPC_ShowSkillEffect("PoisonInfusion");
    }

    // ========== 🌫️ Skill 2: Toxic Dash - FIXED ==========
    protected override void TryUseSkill2()
    {
        if (!CanUseSkill(skill2ManaCost)) return;
        if (HasStatusEffect(StatusEffectType.Stun)) return;

        UseMana(skill2ManaCost);

        // ✅ 🌟 เปลี่ยน: ให้ Move Speed Aura ก่อน dash
        if (statusEffectManager != null)
        {
            statusEffectManager.ApplyMoveSpeedAura(5f, 0.25f, 10f); // +25% move speed, 5m radius, 10s
            Debug.Log($"✅ Applied Move Speed Aura (+25% for 10s)");
        }

        // หาทิศทางการ dash (ตามกล้อง)
        Vector3 dashDirection = GetDashDirection();

        // ✅ FIX: ส่ง RPC ไปทุกคนเพื่อ sync visual และ damage
        RPC_PerformToxicDashAll(dashDirection, transform.position);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_PerformToxicDashAll(Vector3 direction, Vector3 startPosition)
    {
        StartCoroutine(ExecuteToxicDashFixed(direction, startPosition));
    }

    private IEnumerator ExecuteToxicDashFixed(Vector3 direction, Vector3 startPosition)
    {
        Vector3 startPos = startPosition;
        Vector3 endPos = startPos + direction * dashDistance;

        float dashTime = dashDistance / dashSpeed;
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
                main.startColor = new Color(0.5f, 1f, 0f, 0.8f); // เขียวพิษ
                main.startLifetime = dashTime + 1f;
                main.loop = true;

                // ปรับ shape ให้เป็นเส้นตรง
                ParticleSystem.ShapeModule shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(1f, 1f, dashDistance);
            }
        }

        while (elapsed < dashTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / dashTime;

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);

            // อัพเดทตำแหน่ง dash trail
            if (dashTrail != null)
            {
                dashTrail.transform.position = currentPos;
                dashTrail.transform.LookAt(endPos);
            }

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

                        // สร้าง hit effect
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
            // หยุด loop แล้วปล่อยให้ particle หายไปเอง
            ParticleSystem ps = dashTrail.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.loop = false;
            }

            Destroy(dashTrail, 2f);
        }

        IsDashing = false;
        Debug.Log($"🌫️ [Toxic Dash] {CharacterName} dashed through {hitEnemies.Count} enemies!");
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
        // ✅ คำนวณ damage ใหม่
        int directDamage = GetScaledSkillDamage(0.6f); // 60% ของ base formula
        int poisonDamage = GetScaledPoisonDamage(0.3f); // 30% ของ base formula

        // ทำ direct magic damage ก่อน
        enemy.TakeDamageFromAttacker(0, directDamage, this, DamageType.Magic);

        // ✅ ใส่ status effects
        enemy.ApplyStatusEffect(StatusEffectType.Poison, poisonDamage, 8f);
        enemy.ApplyStatusEffect(StatusEffectType.Weakness, 0, 6f, 0.3f);
        enemy.ApplyStatusEffect(StatusEffectType.Blind, 0, 4f, 0.6f);

        // Passive: โอกาส spread poison
        if (hasVenomMastery && Random.Range(0f, 100f) < 25f)
        {
            SpreadPoisonToNearby(enemy, 4f);
        }

        Debug.Log($"🌫️ Toxic Dash hit {enemy.CharacterName}! Direct: {directDamage}, Poison: {poisonDamage}/s");
    }

    // ========== 💣 Skill 3: Shadow Assassination - FIXED ==========
    protected override void TryUseSkill3()
    {
        if (!CanUseSkill(skill3ManaCost)) return;

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

        UseMana(skill3ManaCost);

        // ✅ ให้ Critical Aura เพื่อรองรับ Physical Build
        if (statusEffectManager != null)
        {
            statusEffectManager.ApplyCriticalAura(6f, 0.4f, 12f);
            Debug.Log($"✅ Applied Critical Aura (+40% for 12s)");
        }

        // ✅ FIX: ส่ง RPC ไปทุกคนพร้อมกับ sync position
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
        IsAssassinating = true;

        // คำนวณตำแหน่งหลังเป้าหมาย
        Vector3 backPosition = targetPosition + (targetPosition - originalPosition).normalized * -2f;
        backPosition.y = targetPosition.y;

        // Phase 1: เทเลพอร์ตไปหลังเป้าหมาย
        Debug.Log($"🗡️ [Shadow Assassination] {CharacterName} teleporting behind target!");

        // ✅ FIX: ซ่อนตัวและ sync กับทุกคน
        SetVisibility(false);
        yield return new WaitForSeconds(0.2f);

        // ✅ FIX: เทเลพอร์ตและ sync position
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

        // หันหน้าไปหาเป้าหมาย
        Vector3 lookDirection = (targetPosition - backPosition).normalized;
        transform.rotation = Quaternion.LookRotation(lookDirection);

        // ปรากฏตัว
        SetVisibility(true);
        yield return new WaitForSeconds(0.1f);

        // ✅ Phase 2: โจมตี Execution (เฉพาะ StateAuthority)
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

        // ✅ เปลี่ยนจาก UseAdvancedSkillOnTarget เป็น TakeDamageFromAttacker โดยตรง
        int physicalDamage = Mathf.RoundToInt(baseDamage * 0.3f);
        int magicDamage = Mathf.RoundToInt(baseDamage * 0.7f);

        target.TakeDamageFromAttacker(physicalDamage, magicDamage, this, DamageType.Critical);

        // เพิ่ม effect ตาม build
        if (MagicDamage > AttackDamage) // Magic Build
        {
            int poisonDamage = GetScaledPoisonDamage(0.6f); // ✅ เพิ่ม f suffix
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

        Debug.Log($"🗡️ Shadow Assassination dealt {baseDamage} damage to {target.CharacterName} (Execution: {isExecutionRange})");
    }

    // ========== ☠️ Skill 4: Plague Outbreak (Ultimate) ==========
    protected override void TryUseSkill4()
    {
        if (!CanUseSkill(skill4ManaCost)) return;

        UseMana(skill4ManaCost);

        Vector3 cloudPosition = transform.position;

        RPC_CreatePlagueOutbreak(cloudPosition);
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

                        // Direct magic damage
                        int directDamage = GetScaledSkillDamage(0.3f);
                        enemy.TakeDamageFromAttacker(0, directDamage, this, DamageType.Magic);

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
                // ✅ ง่ายขึ้น: ใช้ RPC เดียว
                bool hasPoisonInfusion = PoisonInfusionStacks > 0;
                bool guaranteedCrit = PoisonInfusionStacks == 1; // ครั้งสุดท้าย

                RPC_PerformAssassinAttack(nearestEnemy.Object, hasPoisonInfusion, guaranteedCrit);

                // ลด stack
                if (PoisonInfusionStacks > 0)
                {
                    PoisonInfusionStacks--;
                    Debug.Log($"🐍 Poison Infusion: {PoisonInfusionStacks} stacks remaining");
                }

                // ✅ ใช้ GetEffectiveAttackSpeed() แทน AttackSpeed
                float effectiveAttackSpeed = GetEffectiveAttackSpeed();
                float finalAttackCooldown = AttackCooldown / Mathf.Max(0.1f, effectiveAttackSpeed);
                nextAttackTime = Time.time + finalAttackCooldown;

                Debug.Log($"🐍 Assassin attack! Speed: {effectiveAttackSpeed:F1}x, Cooldown: {finalAttackCooldown:F1}s");
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
                // ✅ ทำ damage ปกติ (ใช้ระบบเดิม)
                enemy.TakeDamageFromAttacker(AttackDamage, this, DamageType.Normal);

                // ✅ ใส่ poison ถ้ามี Poison Infusion - ใช้สูตรใหม่
                if (shouldPoison)
                {
                    int poisonDamage = GetScaledPoisonDamage(0.4f); // 40% ของ base formula
                    enemy.ApplyStatusEffect(StatusEffectType.Poison, poisonDamage, 8f);

                    // ✅ Venom Mastery: bonus damage ใช้สูตรใหม่
                    if (hasVenomMastery)
                    {
                        int bonusDamage = GetScaledSkillDamage(0.2f); // 20% ของ base formula
                        enemy.TakeDamageFromAttacker(0, bonusDamage, this, DamageType.Magic);
                        Debug.Log($"🐍 [Venom Mastery] poison bonus: {bonusDamage}");
                    }

                    // ✅ Passive: spread poison ใช้สูตรใหม่
                    if (hasVenomMastery && Random.Range(0f, 100f) < 30f)
                    {
                        SpreadPoisonToNearby(enemy, 4f);
                    }

                    Debug.Log($"🐍 Applied poison: {poisonDamage} damage/s for 8s");
                }

                // ✅ Force critical ใช้สูตรใหม่
                if (forceCritical)
                {
                    int critDamage = GetScaledSkillDamage(0.8f); // 80% ของ base formula
                    enemy.TakeDamageFromAttacker(0, critDamage, this, DamageType.Critical);
                    Debug.Log($"🐍 [Poison Infusion] Final strike - Guaranteed Critical! ({critDamage} damage)");
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
        // ✅ ใช้สูตรเดียวกับ Shadow Assassination
        int magicPortion = Mathf.RoundToInt(MagicDamage * 0.7f);
        int physicalPortion = Mathf.RoundToInt(AttackDamage * 0.3f);
        int levelBonus = GetCurrentLevel() * 10;

        int baseDamage = magicPortion + physicalPortion + levelBonus;
        int finalDamage = Mathf.RoundToInt(baseDamage * multiplier);

        Debug.Log($"🐍 [Poison Damage] Base: {baseDamage} × {multiplier} = {finalDamage} (Magic: {magicPortion}, Physical: {physicalPortion}, Level: {levelBonus})");

        return Mathf.Max(1, finalDamage);
    }

    private int GetScaledSkillDamage(float multiplier)
    {
        int magicPortion = Mathf.RoundToInt(MagicDamage * 0.7f);
        int physicalPortion = Mathf.RoundToInt(AttackDamage * 0.3f);
        int levelBonus = GetCurrentLevel() * 10;

        int baseDamage = magicPortion + physicalPortion + levelBonus;
        int finalDamage = Mathf.RoundToInt(baseDamage * multiplier);

        return Mathf.Max(1, finalDamage);
    }

    private Vector3 GetDashDirection()
    {
        // ใช้ทิศทางกล้องเป็นหลัก
        if (cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            forward.y = 0;
            return forward.normalized;
        }

        // fallback ใช้ forward ของตัวละคร
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
            GameObject effect = Instantiate(basicAttackEffect.gameObject, transform.position + Vector3.up * 1.5f, Quaternion.identity);

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