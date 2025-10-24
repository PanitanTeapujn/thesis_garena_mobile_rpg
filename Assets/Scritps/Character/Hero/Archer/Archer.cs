using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Archer : Hero
{
    [Header("🏹 Archer Skill Settings")]
    [SerializeField] private int skill1ManaCost = 15;
    [SerializeField] private int skill2ManaCost = 20;
    [SerializeField] private int skill3ManaCost = 30;
    [SerializeField] private int skill4ManaCost = 50;

    protected override int GetSkill1ManaCost() { return skill1ManaCost; }
    protected override int GetSkill2ManaCost() { return skill2ManaCost; }
    protected override int GetSkill3ManaCost() { return skill3ManaCost; }
    protected override int GetSkill4ManaCost() { return skill4ManaCost; }

    [Header("🎯 Skill Parameters")]
    // Skill 1: Flame Arrow
    public float flameArrowRange = 15f;
    public float flameArrowDamageMultiplier = 1.5f;
    public float flameArrowBurnChance = 0.8f;
    public int flameArrowBurnDamage = 20;
    public float flameArrowBurnDuration = 6f;
    public float flameArrowHitRateBonus = 0.15f;
    public float flameArrowHitRateDuration = 5f;

    // Skill 2: Shadow Step
    public float shadowStepDistance = 8f;
    public float shadowStepSpeed = 20f;
    public float shadowStepInvulnerableDuration = 0.3f;
    public float shadowStepEvasionBonus = 0.25f;
    public float shadowStepEvasionDuration = 4f;
    public float shadowStepMoveSpeedBonus = 0.2f;
    public float shadowStepMoveSpeedDuration = 3f;
    public float smokeBombRadius = 3f;
    public float smokeBombBlindAmount = 0.5f;
    public float smokeBombBlindDuration = 3f;

    // Skill 3: Fire Volley
    public float fireVolleyRange = 15f;
    public float fireVolleyAOERadius = 5f;
    public int fireVolleyMaxTargets = 5;
    public float fireVolleyDamageMultiplier = 1.3f;
    public float fireVolleyBurnChance = 0.7f;
    public int fireVolleyBurnDamage = 18;
    public float fireVolleyBurnDuration = 5f;
    public float fireVolleyAttackSpeedBonus = 0.15f;
    public float fireVolleyAttackSpeedDuration = 6f;

    // Skill 4: Inferno Rain (Ultimate)
    public float infernoRainRange = 18f;
    public float infernoRainAOERadius = 8f;
    public int infernoRainMaxTargets = 10;
    public float infernoRainDamageMultiplier = 2.5f;
    public int infernoRainEnhancedBurnDamage = 45;
    public float infernoRainEnhancedBurnDuration = 10f;
    public float infernoRainFireRingDuration = 8f;
    public int infernoRainFireRingDamage = 20;
    public float infernoRainEvasionBonus = 0.2f;
    public float infernoRainEvasionDuration = 8f;

    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform arrowSpawnPoint;
    [SerializeField] private float arrowSpeed = 20f;
    [Header("🎨 Visual Effects")]
    [SerializeField] private ParticleSystem flameArrowEffect;
    [SerializeField] private ParticleSystem shadowStepEffect;
    [SerializeField] private ParticleSystem smokeBombEffect;
    [SerializeField] private ParticleSystem fireVolleyEffect;
    [SerializeField] private ParticleSystem infernoRainEffect;
    [SerializeField] private ParticleSystem basicAttackEffect;
    [SerializeField] private ParticleSystem burnEffect;
    [SerializeField] private ParticleSystem fireRingEffect;
    private GameObject currentFireRingEffect;
    [Header("🎯 Skill Indicators")]
    [SerializeField] private Material skillRangeIndicatorMaterial;
    private GameObject fireVolleyIndicator;
    private GameObject infernoRainIndicator;
    private LineRenderer rangeCircle;

    // ========== Network Properties ==========
    [Networked] public bool IsFlameArrowActive { get; set; }
    [Networked] public bool IsShadowStepping { get; set; }
    [Networked] public bool IsInvulnerable { get; set; }
    [Networked] public float InvulnerableEndTime { get; set; }
    [Networked] public bool HasHitRateBonus { get; set; }
    [Networked] public float HitRateBonusEndTime { get; set; }
    [Networked] public bool HasEvasionBonus { get; set; }
    [Networked] public float EvasionBonusEndTime { get; set; }
    [Networked] public bool HasMoveSpeedBonus { get; set; }
    [Networked] public float MoveSpeedBonusEndTime { get; set; }
    [Networked] public bool HasAttackSpeedBonus { get; set; }
    [Networked] public float AttackSpeedBonusEndTime { get; set; }
    [Networked] public NetworkObject CurrentFireRing { get; set; }

    // Passive tracking
    [Networked] public int EagleEyeAttackSpeedStacks { get; set; }
    [Networked] public float EagleEyeStacksEndTime { get; set; }
    private bool hasEagleEye = true;

    protected override void Start()
    {
        base.Start();
        AttackType = AttackType.Physical;

        // ปรับ cooldown สำหรับ Archer
        skill1Cooldown = 10f;
        skill2Cooldown = 15f;
        skill3Cooldown = 18f;
        skill4Cooldown = 40f;

        // เพิ่ม Hit Rate และ Evasion จาก Passive
        if (hasEagleEye)
        {
            HitRate += 0.2f; // +20%
            EvasionRate += 0.15f; // +15%
        }

        // สร้าง range indicators
        CreateRangeIndicators();

        // Subscribe to combat events สำหรับ passive
        CombatManager.OnDamageTaken += HandleEagleEyeOnHit;
        CombatManager.OnCharacterDeath += HandleEagleEyeOnKill;

        Debug.Log($"🏹 Archer {CharacterName} initialized with Eagle Eye passive!");
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (HasStateAuthority)
        {
            ProcessTemporaryBuffs();
            ProcessEagleEyeStacks();
        }
    }

    // ========== 💚 Skill 1: Flame Arrow ==========

    public override void TryUseSkill1()
    {
        if (!CanUseSkill(skill1ManaCost)) return;

        // คำนวณ cooldown reduction
        float effectiveReduction = GetEffectiveReductionCoolDown();
        float reductionMultiplier = 1f - (effectiveReduction / 100f);
        reductionMultiplier = Mathf.Clamp(reductionMultiplier, 0.1f, 1f);

        float finalCooldown = skill1Cooldown * reductionMultiplier;
        nextSkill1Time = Time.time + finalCooldown;

        UseMana(skill1ManaCost);

        // หาเป้าหมาย
        Character target = FindNearestEnemy(flameArrowRange);
        if (target != null)
        {
            RPC_ExecuteFlameArrow(target.Object);
        }
        else
        {
            Debug.Log($"🏹 No target found for Flame Arrow");
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_ExecuteFlameArrow(NetworkObject targetObject)
    {
        if (targetObject == null) return;

        Character target = targetObject.GetComponent<Character>();
        if (target == null ) return;

        StartCoroutine(ExecuteFlameArrow(target));
    }

    private IEnumerator ExecuteFlameArrow(Character target)
    {
        Debug.Log($"🏹 [Flame Arrow] Firing at {target.CharacterName}!");

        // ยิงลูกศรเพลิง (visual)
        if (arrowPrefab != null && target != null)
        {
            Vector3 startPos = arrowSpawnPoint != null ? arrowSpawnPoint.position : transform.position + Vector3.up;
            Vector3 direction = (target.transform.position + Vector3.up - startPos).normalized;

            GameObject arrow = Instantiate(arrowPrefab, startPos, Quaternion.LookRotation(direction));

            // ✅ เปลี่ยนสีลูกศรเป็นสีส้มแดง (ถ้าเป็น Sprite)
            SpriteRenderer arrowSprite = arrow.GetComponent<SpriteRenderer>();
            if (arrowSprite != null)
            {
                arrowSprite.color = new Color(1f, 0.5f, 0f); // สีส้มแดง
            }

            float distance = Vector3.Distance(startPos, target.transform.position);
            float travelTime = distance / arrowSpeed;
            float elapsed = 0f;
            Vector3 arrowStartPos = arrow.transform.position;

            while (elapsed < travelTime && target != null )
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / travelTime;

                Vector3 currentTarget = target.transform.position + Vector3.up;
                arrow.transform.position = Vector3.Lerp(arrowStartPos, currentTarget, progress);
                arrow.transform.LookAt(currentTarget);

                yield return null;
            }

            Destroy(arrow, 0.1f);

            // แสดง flame hit effect
            if (flameArrowEffect != null && target != null)
            {
                GameObject hitFX = Instantiate(flameArrowEffect.gameObject, target.transform.position + Vector3.up, Quaternion.identity);
                Destroy(hitFX, 2f);
            }
        }

        // สร้างดาเมจ (เฉพาะ server)
        if (HasStateAuthority && target != null )
        {
            int damage = Mathf.RoundToInt(AttackDamage * flameArrowDamageMultiplier);

            // ✅ Skill attack - ใช้ Amp Damage
            target.TakeDamageFromAttacker(damage, 0, this, DamageType.Normal, false);

            // มีโอกาส 80% ติด Burn
            if (Random.Range(0f, 1f) <= flameArrowBurnChance)
            {
                StatusEffectManager targetStatus = target.GetComponent<StatusEffectManager>();
                if (targetStatus != null)
                {
                    int burnDamagePerTick = flameArrowBurnDamage + Mathf.RoundToInt(AttackDamage * 0.15f);
                    targetStatus.ApplyBurn(burnDamagePerTick, flameArrowBurnDuration);
                    Debug.Log($"🔥 Applied Burn to {target.CharacterName}!");
                }
            }

            // เพิ่ม Hit Rate ให้ตัวเอง
            HasHitRateBonus = true;
            HitRateBonusEndTime = Time.time + flameArrowHitRateDuration;
            HitRate += flameArrowHitRateBonus;
            Debug.Log($"🎯 Gained +15% Hit Rate!");
        }
    }


    // ========== 🌪️ Skill 2: Shadow Step (Reverse Dash) ==========
    public override void TryUseSkill2()
    {
        if (!CanUseSkill(skill2ManaCost)) return;
        if (HasStatusEffect(StatusEffectType.Stun)) return;

        float effectiveReduction = GetEffectiveReductionCoolDown();
        float reductionMultiplier = 1f - (effectiveReduction / 100f);
        reductionMultiplier = Mathf.Clamp(reductionMultiplier, 0.1f, 1f);

        float finalCooldown = skill2Cooldown * reductionMultiplier;
        nextSkill2Time = Time.time + finalCooldown;

        UseMana(skill2ManaCost);

        // คำนวณทิศทางตรงข้าม
        Vector3 dashDirection = GetReverseDashDirection();
        Vector3 smokeBombPosition = transform.position;

        RPC_PerformShadowStep(dashDirection, smokeBombPosition);
    }

    private Vector3 GetReverseDashDirection()
    {
        // อ่าน input จาก moveInputX และ moveInputZ
        Vector3 inputDirection = new Vector3(moveInputX, 0, moveInputZ);

        // ถ้าไม่มี input ให้ dash ถอยหลัง (default)
        if (inputDirection.magnitude < 0.1f)
        {
            // ใช้ตำแหน่งกล้องเป็น reference
            if (cameraTransform != null)
            {
                Vector3 cameraForward = cameraTransform.forward;
                cameraForward.y = 0;
                cameraForward.Normalize();
                return -cameraForward; // ถอยหลัง
            }
            return -transform.forward; // fallback
        }

        // กลับทิศทาง input (reverse)
        Vector3 reversedDirection = -inputDirection;

        // แปลงเป็นทิศทางที่สัมพันธ์กับกล้อง
        if (cameraTransform != null)
        {
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 worldDirection = (cameraForward * reversedDirection.z) + (cameraRight * reversedDirection.x);
            return worldDirection.normalized;
        }

        return reversedDirection.normalized;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_PerformShadowStep(Vector3 direction, Vector3 smokeBombPosition)
    {
        StartCoroutine(ExecuteShadowStep(direction, smokeBombPosition));
    }

    private IEnumerator ExecuteShadowStep(Vector3 direction, Vector3 smokeBombPosition)
    {
        Vector3 startPos = transform.position;

        // ตรวจสอบกำแพงก่อน dash
        float actualDashDistance = shadowStepDistance;
        RaycastHit wallHit;

        if (Physics.Raycast(startPos, direction, out wallHit, shadowStepDistance,
            LayerMask.GetMask("Wall", "Obstacle", "Default")))
        {
            actualDashDistance = Mathf.Max(0.5f, wallHit.distance - 0.5f);
            Debug.Log($"⚠️ Wall detected, reducing dash to {actualDashDistance}m");
        }

        Vector3 endPos = startPos + direction * actualDashDistance;
        float dashTime = actualDashDistance / shadowStepSpeed;
        float elapsed = 0f;

        IsShadowStepping = true;
        IsInvulnerable = true;
        InvulnerableEndTime = Time.time + shadowStepInvulnerableDuration;

        // สร้าง dash effect
        if (shadowStepEffect != null)
        {
            GameObject dashFX = Instantiate(shadowStepEffect.gameObject, startPos, Quaternion.identity);
            Destroy(dashFX, 2f);
        }

        // Dash animation
        while (elapsed < dashTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / dashTime;

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);

            // Safety check กำแพง
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

            yield return null;
        }

        IsShadowStepping = false;

        // ทิ้ง Smoke Bomb ที่จุดเดิม
        if (HasStateAuthority)
        {
            CreateSmokeBomb(smokeBombPosition);
        }

        // Apply buffs
        if (HasStateAuthority)
        {
            HasEvasionBonus = true;
            EvasionBonusEndTime = Time.time + shadowStepEvasionDuration;
            EvasionRate += shadowStepEvasionBonus;

            HasMoveSpeedBonus = true;
            MoveSpeedBonusEndTime = Time.time + shadowStepMoveSpeedDuration;
            MoveSpeed *= (1f + shadowStepMoveSpeedBonus);

            Debug.Log($"🌪️ Shadow Step complete! +25% Evasion, +20% Move Speed");
        }
    }

    private void CreateSmokeBomb(Vector3 position)
    {
        // สร้าง smoke bomb effect
        if (smokeBombEffect != null)
        {
            GameObject smokeFX = Instantiate(smokeBombEffect.gameObject, position, Quaternion.identity);
            Destroy(smokeFX, smokeBombBlindDuration);
        }

        // หาศัตรูใน AOE
        Collider[] hitColliders = Physics.OverlapSphere(position, smokeBombRadius);
        foreach (Collider col in hitColliders)
        {
            Character enemy = col.GetComponent<Character>();
            if (enemy != null && enemy != this )
            {
                // ตรวจสอบว่าเป็นศัตรูหรือไม่
                bool isEnemy = false;
                if (this is Hero && enemy is NetworkEnemy) isEnemy = true;
                if (this is NetworkEnemy && enemy is Hero) isEnemy = true;

                if (isEnemy)
                {
                    StatusEffectManager enemyStatus = enemy.GetComponent<StatusEffectManager>();
                    if (enemyStatus != null)
                    {
                        enemyStatus.ApplyBlind(smokeBombBlindAmount, smokeBombBlindDuration);
                        Debug.Log($"💨 Applied Blind to {enemy.CharacterName}");
                    }
                }
            }
        }
    }

    // ========== 🎯 Skill 3: Fire Volley ==========
    public override void TryUseSkill3()
    {
        if (!CanUseSkill(skill3ManaCost)) return;

        float effectiveReduction = GetEffectiveReductionCoolDown();
        float reductionMultiplier = 1f - (effectiveReduction / 100f);
        reductionMultiplier = Mathf.Clamp(reductionMultiplier, 0.1f, 1f);

        float finalCooldown = skill3Cooldown * reductionMultiplier;
        nextSkill3Time = Time.time + finalCooldown;

        UseMana(skill3ManaCost);

        // หาตำแหน่งเป้าหมาย (ศัตรูที่ใกล้ที่สุด)
        Character nearestEnemy = FindNearestEnemy(fireVolleyRange);
        Vector3 targetPosition = nearestEnemy != null ? nearestEnemy.transform.position : transform.position + transform.forward * 10f;

        RPC_ExecuteFireVolley(targetPosition);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_ExecuteFireVolley(Vector3 targetPosition)
    {
        StartCoroutine(ExecuteFireVolley(targetPosition));
    }

    private IEnumerator ExecuteFireVolley(Vector3 targetPosition)
    {
        Debug.Log($"🎯 [Fire Volley] Firing at position {targetPosition}!");

        // สร้าง visual effect
        if (fireVolleyEffect != null)
        {
            GameObject volleyFX = Instantiate(fireVolleyEffect.gameObject, targetPosition + Vector3.up * 1f, Quaternion.identity);
            Destroy(volleyFX, 3f);
        }

        yield return new WaitForSeconds(0.3f);

        // สร้างดาเมจ (เฉพาะ server)
        if (HasStateAuthority)
        {
            List<Character> hitEnemies = FindEnemiesInArea(targetPosition, fireVolleyAOERadius, fireVolleyMaxTargets);

            foreach (Character enemy in hitEnemies)
            {
                if (enemy != null )
                {
                    int damage = Mathf.RoundToInt(AttackDamage * fireVolleyDamageMultiplier);
                    enemy.TakeDamageFromAttacker(damage, 0, this, DamageType.Normal, false);

                    // มีโอกาส 70% ติด Burn
                    if (Random.Range(0f, 1f) <= fireVolleyBurnChance)
                    {
                        StatusEffectManager enemyStatus = enemy.GetComponent<StatusEffectManager>();
                        if (enemyStatus != null)
                        {
                            int burnDamagePerTick = fireVolleyBurnDamage + Mathf.RoundToInt(AttackDamage * 0.12f);
                            enemyStatus.ApplyBurn(burnDamagePerTick, fireVolleyBurnDuration);
                            Debug.Log($"🔥 Applied Burn to {enemy.CharacterName}!");
                        }
                    }
                }
            }

            // เพิ่ม Attack Speed ให้ตัวเอง
            HasAttackSpeedBonus = true;
            AttackSpeedBonusEndTime = Time.time + fireVolleyAttackSpeedDuration;
            AttackSpeed += fireVolleyAttackSpeedBonus;
            Debug.Log($"⚡ Gained +15% Attack Speed!");
        }
    }

    // ========== 🔥 Skill 4: Inferno Rain (Ultimate) ==========
    public override void TryUseSkill4()
    {
        if (!CanUseSkill(skill4ManaCost)) return;

        float effectiveReduction = GetEffectiveReductionCoolDown();
        float reductionMultiplier = 1f - (effectiveReduction / 100f);
        reductionMultiplier = Mathf.Clamp(reductionMultiplier, 0.1f, 1f);

        float finalCooldown = skill4Cooldown * reductionMultiplier;
        nextSkill4Time = Time.time + finalCooldown;

        UseMana(skill4ManaCost);

        // หาตำแหน่งเป้าหมาย
        Character nearestEnemy = FindNearestEnemy(infernoRainRange);
        Vector3 targetPosition = nearestEnemy != null ? nearestEnemy.transform.position : transform.position + transform.forward * 12f;

        RPC_ExecuteInfernoRain(targetPosition);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_ExecuteInfernoRain(Vector3 targetPosition)
    {
        StartCoroutine(ExecuteInfernoRain(targetPosition));
    }

    private IEnumerator ExecuteInfernoRain(Vector3 targetPosition)
    {
        Debug.Log($"🔥 [INFERNO RAIN] Ultimate activated at {targetPosition}!");

        // สร้าง visual effect
        if (infernoRainEffect != null)
        {
            GameObject infernoFX = Instantiate(infernoRainEffect.gameObject, targetPosition + Vector3.up * 1f, Quaternion.identity);
            Destroy(infernoFX, 5f);
        }

        yield return new WaitForSeconds(0.5f);

        // สร้างดาเมจ (เฉพาะ server)
        if (HasStateAuthority)
        {
            List<Character> hitEnemies = FindEnemiesInArea(targetPosition, infernoRainAOERadius, infernoRainMaxTargets);

            foreach (Character enemy in hitEnemies)
            {
                if (enemy != null )
                {
                    int damage = Mathf.RoundToInt(AttackDamage * infernoRainDamageMultiplier);

                    // Critical chance
                    bool isCritical = Random.Range(0f, 1f) <= CriticalChance;
                    DamageType damageType = isCritical ? DamageType.Critical : DamageType.Normal;

                    if (isCritical)
                    {
                        damage = Mathf.RoundToInt(damage * (1f + CriticalDamageBonus));
                        Debug.Log($"💥 CRITICAL HIT!");
                    }

                    enemy.TakeDamageFromAttacker(damage, 0, this, damageType, false);

                    // 100% ติด Enhanced Burn
                    StatusEffectManager enemyStatus = enemy.GetComponent<StatusEffectManager>();
                    if (enemyStatus != null)
                    {
                        int burnDamagePerTick = infernoRainEnhancedBurnDamage + Mathf.RoundToInt(AttackDamage * 0.3f);
                        enemyStatus.ApplyBurn(burnDamagePerTick, infernoRainEnhancedBurnDuration);
                        Debug.Log($"🔥🔥 Applied Enhanced Burn to {enemy.CharacterName}!");
                    }
                }
            }

            // สร้าง Fire Ring
            CreateFireRing(targetPosition);

            // เพิ่ม Evasion ให้ตัวเอง
            HasEvasionBonus = true;
            EvasionBonusEndTime = Time.time + infernoRainEvasionDuration;
            EvasionRate += infernoRainEvasionBonus;
            Debug.Log($"🌪️ Gained +20% Evasion from Ultimate!");
        }
    }

    private void CreateFireRing(Vector3 position)
    {
        if (fireRingEffect == null) return;

        // ลบ Fire Ring เก่าถ้ามี
        if (currentFireRingEffect != null)
        {
            Destroy(currentFireRingEffect);
        }

        // สร้าง Fire Ring Particle ใหม่
        currentFireRingEffect = Instantiate(fireRingEffect.gameObject, position, Quaternion.identity);

        // ตั้งค่า Particle System
        ParticleSystem ps = currentFireRingEffect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = infernoRainFireRingDuration;
            main.loop = false; // เปลี่ยนเป็น false เพื่อให้หยุดเอง

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = infernoRainAOERadius;

            ps.Play();

            // ลบ GameObject หลังจาก duration หมด
            Destroy(currentFireRingEffect, infernoRainFireRingDuration);
        }

        // เริ่ม coroutine สำหรับดาเมจต่อเนื่อง
        StartCoroutine(FireRingDamageCoroutine(position));
    }
    private IEnumerator FireRingDamageCoroutine(Vector3 position)
    {
        float endTime = Time.time + infernoRainFireRingDuration;

        while (Time.time < endTime)
        {
            if (HasStateAuthority)
            {
                // หาศัตรูใน Fire Ring
                Collider[] hitColliders = Physics.OverlapSphere(position, infernoRainAOERadius);
                foreach (Collider col in hitColliders)
                {
                    Character enemy = col.GetComponent<Character>();
                    if (enemy != null && enemy != this )
                    {
                        bool isEnemy = false;
                        if (this is Hero && enemy is NetworkEnemy) isEnemy = true;
                        if (this is NetworkEnemy && enemy is Hero) isEnemy = true;

                        if (isEnemy)
                        {
                            // สร้างดาเมจต่อเนื่อง
                            enemy.TakeDamageFromAttacker(infernoRainFireRingDamage, 0, this, DamageType.Burn, false);

                            // Refresh Burn
                            StatusEffectManager enemyStatus = enemy.GetComponent<StatusEffectManager>();
                            if (enemyStatus != null && enemyStatus.IsBurning)
                            {
                                int burnDamagePerTick = infernoRainEnhancedBurnDamage + Mathf.RoundToInt(AttackDamage * 0.3f);
                                enemyStatus.ApplyBurn(burnDamagePerTick, infernoRainEnhancedBurnDuration);
                            }
                        }
                    }
                }
            }

            yield return new WaitForSeconds(1f); // ดาเมจทุก 1 วินาที
        }

        // ลบ Fire Ring
        if (HasStateAuthority && CurrentFireRing != null)
        {
            Runner.Despawn(CurrentFireRing);
            CurrentFireRing = null;
        }
    }

    // ========== 🌟 Passive: Eagle Eye ==========
   

    protected override void OnDestroy()
    {
        base.OnDestroy();

        // Unsubscribe events
        CombatManager.OnDamageTaken -= HandleEagleEyeOnHit;
        CombatManager.OnCharacterDeath -= HandleEagleEyeOnKill;
    }

    private void HandleEagleEyeOnHit(Character target, int damage, DamageType damageType, bool isCritical)
    {
        // ตรวจสอบว่าเป็นการโจมตีจากตัวเอง
        if (!hasEagleEye || !HasStateAuthority) return;
        if (target == this) return; // ไม่ใช่เราโดนโจมตี

        // 25% โอกาสยิงลูกศรเพิ่ม (เฉพาะ basic attack)
        if (damageType == DamageType.Normal && Random.Range(0f, 1f) <= 0.25f)
        {
            Debug.Log($"🦅 Eagle Eye! Double Shot activated!");
            StartCoroutine(DelayedExtraShot(target, 0.2f));
        }

        // เช็ค Critical Hit
        if (isCritical)
        {
            EagleEyeAttackSpeedStacks = Mathf.Min(EagleEyeAttackSpeedStacks + 1, 3);
            EagleEyeStacksEndTime = Time.time + 3f;

            float totalBonus = EagleEyeAttackSpeedStacks * 0.1f;
            Debug.Log($"🦅 Eagle Eye! Attack Speed +{totalBonus * 100}% ({EagleEyeAttackSpeedStacks} stacks)");
        }
    }

    private IEnumerator DelayedExtraShot(Character target, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (target != null && HasStateAuthority)
        {
            int damage = AttackDamage;
            target.TakeDamageFromAttacker(damage, 0, this, DamageType.Normal, true);
            Debug.Log($"🏹 Extra shot hit {target.CharacterName}!");
        }
    }

    private void HandleEagleEyeOnKill(Character killedEnemy)
    {
        if (!hasEagleEye || !HasStateAuthority) return;

        // ถ้าศัตรูมี Burn จะได้ Mana คืน 10 จุด
        StatusEffectManager enemyStatus = killedEnemy.GetComponent<StatusEffectManager>();
        if (enemyStatus != null && enemyStatus.IsBurning)
        {
            int manaRestore = 10;
            CurrentMana = Mathf.Min(CurrentMana + manaRestore, MaxMana);
            Debug.Log($"🦅 Eagle Eye! Restored {manaRestore} mana from burning enemy!");
        }
    }

    // ========== Helper Functions ==========
    private void ProcessTemporaryBuffs()
    {
        // Process Hit Rate Bonus
        if (HasHitRateBonus && Time.time >= HitRateBonusEndTime)
        {
            HitRate -= flameArrowHitRateBonus;
            HasHitRateBonus = false;
            Debug.Log($"🎯 Hit Rate bonus expired");
        }

        // Process Evasion Bonus
        if (HasEvasionBonus && Time.time >= EvasionBonusEndTime)
        {
            float totalEvasionBonus = shadowStepEvasionBonus;
            if (EvasionBonusEndTime == Time.time + infernoRainEvasionDuration)
            {
                totalEvasionBonus = infernoRainEvasionBonus;
            }
            EvasionRate -= totalEvasionBonus;
            HasEvasionBonus = false;
            Debug.Log($"🌪️ Evasion bonus expired");
        }

        // Process Move Speed Bonus
        if (HasMoveSpeedBonus && Time.time >= MoveSpeedBonusEndTime)
        {
            MoveSpeed /= (1f + shadowStepMoveSpeedBonus);
            HasMoveSpeedBonus = false;
            Debug.Log($"🌪️ Move Speed bonus expired");
        }

        // Process Attack Speed Bonus
        if (HasAttackSpeedBonus && Time.time >= AttackSpeedBonusEndTime)
        {
            AttackSpeed -= fireVolleyAttackSpeedBonus;
            HasAttackSpeedBonus = false;
            Debug.Log($"⚡ Attack Speed bonus expired");
        }

        // Process Invulnerable
        if (IsInvulnerable && Time.time >= InvulnerableEndTime)
        {
            IsInvulnerable = false;
        }
    }

    private void ProcessEagleEyeStacks()
    {
        if (EagleEyeAttackSpeedStacks > 0 && Time.time >= EagleEyeStacksEndTime)
        {
            EagleEyeAttackSpeedStacks = 0;
            Debug.Log($"🦅 Eagle Eye stacks expired");
        }
    }

    private Character FindNearestEnemy(float maxRange)
    {
        Character nearestEnemy = null;
        float nearestDistance = maxRange;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, maxRange);
        foreach (Collider col in hitColliders)
        {
            Character enemy = col.GetComponent<Character>();
            if (enemy != null && enemy != this )
            {
                bool isEnemy = false;
                if (this is Hero && enemy is NetworkEnemy) isEnemy = true;
                if (this is NetworkEnemy && enemy is Hero) isEnemy = true;

                if (isEnemy)
                {
                    float distance = Vector3.Distance(transform.position, enemy.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = enemy;
                    }
                }
            }
        }

        return nearestEnemy;
    }

    private List<Character> FindEnemiesInArea(Vector3 center, float radius, int maxTargets)
    {
        List<Character> enemies = new List<Character>();

        Collider[] hitColliders = Physics.OverlapSphere(center, radius);
        foreach (Collider col in hitColliders)
        {
            if (enemies.Count >= maxTargets) break;

            Character enemy = col.GetComponent<Character>();
            if (enemy != null && enemy != this )
            {
                bool isEnemy = false;
                if (this is Hero && enemy is NetworkEnemy) isEnemy = true;
                if (this is NetworkEnemy && enemy is Hero) isEnemy = true;

                if (isEnemy)
                {
                    enemies.Add(enemy);
                }
            }
        }

        return enemies;
    }

    private new bool CanUseSkill(int manaCost)
    {
        

        if (CurrentMana < manaCost)
        {
            Debug.Log($"❌ Not enough mana! Need {manaCost}, have {CurrentMana}");
            return false;
        }

        return true;
    }
    public override void TryAttack()
    {
        if (!HasInputAuthority || !IsSpawned) return;
        if (Time.time < nextAttackTime) return;

        // หาศัตรูในระยะโจมตี
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
                // ยิงลูกศรไปหาเป้าหมาย
                RPC_PerformArcherAttack(nearestEnemy.Object);

                // คำนวณ cooldown reduction จาก Attack Speed
                float cooldownReduction = GetEffectiveAttackSpeed(); // 0-0.9 (0%-90%)
                float finalAttackCooldown = AttackCooldown * (1f - cooldownReduction);

                nextAttackTime = Time.time + finalAttackCooldown;

                Debug.Log($"🏹 Archer attack! Cooldown Reduction: {cooldownReduction * 100f}%, Final Cooldown: {finalAttackCooldown:F2}s");
            }
        }
    }


    // ========================================
    // 3. RPC_PerformArcherAttack - สร้างดาเมจและ Effect
    // ========================================

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_PerformArcherAttack(NetworkObject enemyObject)
    {
        if (enemyObject != null)
        {
            Character enemy = enemyObject.GetComponent<Character>();
            if (enemy != null )
            {
                // ✅ Basic attack - ไม่ใช้ Amp Damage
                bool isBasicAttack = true;

                // สร้าง Physical Damage
                enemy.TakeDamageFromAttacker(AttackDamage, 0, this, DamageType.Normal, isBasicAttack);

                Debug.Log($"🏹 [Archer Attack] Dealt {AttackDamage} physical damage");

                // เรียก visual effect
                RPC_OnArcherAttackHit(enemyObject);
            }
        }
    }


    // ========================================
    // 4. RPC_OnArcherAttackHit - แสดง Visual Effect
    // ========================================

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnArcherAttackHit(NetworkObject enemyObject)
    {
        Debug.Log($"🏹 {CharacterName} arrow hit enemy!");

        if (enemyObject != null)
        {
            Vector3 hitPosition = enemyObject.transform.position + Vector3.up;

            // แสดง basic attack effect
            if (basicAttackEffect != null)
            {
                GameObject hitFX = Instantiate(basicAttackEffect.gameObject, hitPosition, Quaternion.identity);
                Destroy(hitFX, 1f);
            }

            // ยิงลูกศรจากตัวละครไปหาเป้าหมาย (visual only)
            StartCoroutine(ShootArrowVisual(enemyObject.transform));
        }
    }


    // ========================================
    // 5. ShootArrowVisual - ลูกศรบินไปหาเป้าหมาย (Visual)
    // ========================================

    private IEnumerator ShootArrowVisual(Transform target)
    {
        if (arrowPrefab == null || target == null) yield break;

        Vector3 startPos = arrowSpawnPoint != null ? arrowSpawnPoint.position : transform.position + Vector3.up;
        Vector3 direction = (target.position + Vector3.up - startPos).normalized;

        // สร้างลูกศร
        GameObject arrow = Instantiate(arrowPrefab, startPos, Quaternion.LookRotation(direction));

        // คำนวณเวลาบิน
        float distance = Vector3.Distance(startPos, target.position);
        float travelTime = distance / arrowSpeed;
        float elapsed = 0f;

        Vector3 arrowStartPos = arrow.transform.position;

        while (elapsed < travelTime && target != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / travelTime;

            // อัพเดทตำแหน่งลูกศร
            Vector3 currentTarget = target.position + Vector3.up;
            arrow.transform.position = Vector3.Lerp(arrowStartPos, currentTarget, progress);
            arrow.transform.LookAt(currentTarget);

            yield return null;
        }

        // ทำลายลูกศรเมื่อถึงเป้าหมาย
        Destroy(arrow, 0.1f);
    }

    private void CreateRangeIndicators()
    {
        // TODO: สร้าง visual indicators สำหรับ skill range
        // คล้าย Assassin แต่ปรับสำหรับ Archer
    }

    private void RPC_ShowSkillEffect(string effectName)
    {
        // TODO: แสดง visual effects
    }

    public override float GetEffectiveAttackSpeed()
    {
        float baseSpeed = base.GetEffectiveAttackSpeed();

        // เพิ่ม Eagle Eye stacks
        if (hasEagleEye && EagleEyeAttackSpeedStacks > 0)
        {
            baseSpeed += EagleEyeAttackSpeedStacks * 0.1f;
        }

        return baseSpeed;
    }
}