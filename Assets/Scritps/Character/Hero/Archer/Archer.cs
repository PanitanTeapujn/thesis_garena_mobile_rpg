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
    [Header("🎬 Animation")]
    private Animator animator;
    private const string ANIM_ATTACK = "Attack";
    [Header("🎯 Attack Range Indicator")]
    [SerializeField] private GameObject attackRangeIndicatorPrefab;
    [SerializeField] private Material attackRangeMaterial;
    private GameObject attackRangeIndicator;
    private bool isShowingAttackRange = false;
    [Header("🌟 Ferris Point Mode")]
    [Networked] public bool IsInFerrisMode { get; set; }
    private float ferrisModeDrainRate = 10f;
    private float accumulatedDrain = 0f;
    private float ferrisAttackRangeMultiplier = 1.5f;
    private float ferrisDoubleShot = 0.40f;
    public float flameArrowAOERadius = 4f;
    [Header("🌟 Attack Speed Stack System")]
    [Networked] public int AttackSpeedStacks { get; set; }
    [Networked] public float AttackSpeedStacksEndTime { get; set; }
    private const int MAX_ATTACK_SPEED_STACKS = 100;
    private const float STACK_DURATION = 7f;
    private const float STACK_BONUS_PER_STACK = 0.01f; // 1% per stack

    [Networked] public bool HasAttackSpeedBonusFromSkill { get; set; }
    [Networked] public float AttackSpeedBonusFromSkillEndTime { get; set; }
    [Networked] public float AttackSpeedBonusFromSkillAmount { get; set; }
    [Networked] public int EagleEyeAttackSpeedStacks { get; set; }
    [Networked] public float EagleEyeStacksEndTime { get; set; }
    private bool hasEagleEye = true;
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
  

    protected override void Start()
    {
        base.Start();
        AttackType = AttackType.Physical;

        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning($"⚠️ Animator not found on {CharacterName}!");
        }

        // ปรับ cooldown สำหรับ Archer
        skill1Cooldown = 10f;
        skill2Cooldown = 15f;
        skill3Cooldown = 18f;
        skill4Cooldown = 40f;
        IsInFerrisMode = false;
        accumulatedDrain = 0f;

        if (hasEagleEye)
        {
            HitRate += 0.2f;
            EvasionRate += 0.15f;
        }

        // ✅ สร้าง Attack Range Indicator
        CreateRangeIndicators();

        CombatManager.OnDamageTaken += HandleEagleEyeOnHit;
        CombatManager.OnCharacterDeath += HandleEagleEyeOnKill;

        Debug.Log($"🏹 Archer {CharacterName} initialized with Eagle Eye passive!");
    }

    // ========== แก้ไข FixedUpdateNetwork - อัพเดทตำแหน่งวงกลม ==========
    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (HasStateAuthority)
        {
            ProcessTemporaryBuffs();
            ProcessEagleEyeStacks();
            ProcessAttackSpeedStacks(); // 🆕 เพิ่มใหม่
        }

        if (IsInFerrisMode)
        {
            DrainFerrisPoints(Runner.DeltaTime);
        }

        // อัพเดทตำแหน่งวงกลม
      
    }

    // ========== 💚 Skill 1: Flame Arrow ==========

    public override void TryUseSkill1()
    {
        if (!CanUseSkill(skill1ManaCost)) return;

        float effectiveReduction = GetEffectiveReductionCoolDown();
        float reductionMultiplier = 1f - (effectiveReduction / 100f);
        reductionMultiplier = Mathf.Clamp(reductionMultiplier, 0.1f, 1f);

        float finalCooldown = skill1Cooldown * reductionMultiplier;
        nextSkill1Time = Time.time + finalCooldown;

        UseMana(skill1ManaCost);

        if (!IsInFerrisMode)
        {
            GainFerrisPoint(15);
        }

        if (IsInFerrisMode)
        {
            ExecuteFerrisModeFlameArrow();
        }
        else
        {
            Character target = FindNearestEnemy(flameArrowRange);
            if (target != null)
            {
                FaceTarget(target.transform);
                RPC_ExecuteFlameArrow(target.Object);
            }
            else
            {
                Debug.Log($"🏹 No target found for Flame Arrow");
            }
        }
    }

    private void ExecuteFerrisModeFlameArrow()
    {
        List<Character> nearestEnemies = FindNearestEnemies(flameArrowRange, 2);

        if (nearestEnemies.Count == 0)
        {
            Debug.Log($"🌟 No targets found for Ferris Flame Arrow");
            return;
        }

        if (nearestEnemies.Count == 1)
        {
            FaceTarget(nearestEnemies[0].transform);
            RPC_ExecuteFerrisFlameArrow(nearestEnemies[0].Object, nearestEnemies[0].Object);
        }
        else
        {
            FaceTarget(nearestEnemies[0].transform);
            RPC_ExecuteFerrisFlameArrow(nearestEnemies[0].Object, nearestEnemies[1].Object);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_ExecuteFerrisFlameArrow(NetworkObject target1Object, NetworkObject target2Object)
    {
        if (target1Object == null) return;

        Character target1 = target1Object.GetComponent<Character>();
        Character target2 = target2Object?.GetComponent<Character>();

        if (target1 != null)
        {
            StartCoroutine(ExecuteFerrisFlameArrowSequence(target1, target2));
        }
    }

    private IEnumerator ExecuteFerrisFlameArrowSequence(Character target1, Character target2)
    {
        Debug.Log($"🌟 [Ferris Flame Arrow] Firing 2 arrows!");
        AudioManager.instance.PlaySFX(11, 0.2f);

        bool sameTarget = (target2 == null || target1 == target2);

        StartCoroutine(ShootFerrisArrow(target1, 0f, sameTarget ? 1 : 0));

        if (!sameTarget && target2 != null)
        {
            StartCoroutine(ShootFerrisArrow(target2, 0.1f, 1));
        }
        else if (sameTarget)
        {
            StartCoroutine(ShootFerrisArrow(target1, 0.1f, 2));
        }

        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator ShootFerrisArrow(Character target, float delay, int arrowIndex)
    {
        yield return new WaitForSeconds(delay);

        if (arrowPrefab != null && target != null)
        {
            Vector3 startPos = arrowSpawnPoint != null ? arrowSpawnPoint.position : transform.position + Vector3.up;
            Vector3 targetPos = target.transform.position + Vector3.up;
            Vector3 direction = (targetPos - startPos).normalized;

            GameObject arrow = Instantiate(arrowPrefab, startPos, Quaternion.identity);

            SpriteRenderer arrowSprite = arrow.GetComponent<SpriteRenderer>();
            if (arrowSprite != null)
            {
                arrowSprite.color = new Color(1f, 0.3f, 0f);
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

                if (direction.x < 0)
                {
                    arrowSprite.flipY = true;
                    arrow.transform.rotation = Quaternion.Euler(0, 0, angle + 180f);
                }
                else
                {
                    arrowSprite.flipY = false;
                    arrow.transform.rotation = Quaternion.Euler(0, 0, angle);
                }
            }

            float distance = Vector3.Distance(startPos, targetPos);
            float travelTime = distance / arrowSpeed;
            float elapsed = 0f;

            while (elapsed < travelTime && target != null)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / travelTime;

                Vector3 currentTarget = target.transform.position + Vector3.up;
                arrow.transform.position = Vector3.Lerp(startPos, currentTarget, progress);

                Vector3 newDirection = (currentTarget - arrow.transform.position).normalized;
                float newAngle = Mathf.Atan2(newDirection.y, newDirection.x) * Mathf.Rad2Deg;

                if (arrowSprite != null)
                {
                    if (newDirection.x < 0)
                    {
                        arrowSprite.flipY = true;
                        arrow.transform.rotation = Quaternion.Euler(0, 0, newAngle + 180f);
                    }
                    else
                    {
                        arrowSprite.flipY = false;
                        arrow.transform.rotation = Quaternion.Euler(0, 0, newAngle);
                    }
                }

                yield return null;
            }

            Destroy(arrow, 0.1f);

            if (flameArrowEffect != null && target != null)
            {
                GameObject hitFX = Instantiate(flameArrowEffect.gameObject, target.transform.position + Vector3.up, Quaternion.identity);
                Destroy(hitFX, 2f);
            }
        }

        if (HasStateAuthority && target != null)
        {
            int damage = GetScaledSkillDamageWithAmp(flameArrowDamageMultiplier);

            Collider[] hits = Physics.OverlapSphere(target.transform.position, flameArrowAOERadius);
            foreach (Collider col in hits)
            {
                Character enemy = col.GetComponent<Character>();
                if (enemy != null && enemy != this)
                {
                    bool isEnemy = false;
                    if (this is Hero && enemy is NetworkEnemy) isEnemy = true;
                    if (this is NetworkEnemy && enemy is Hero) isEnemy = true;

                    if (isEnemy)
                    {
                        enemy.TakeDamageFromAttacker(0, damage, this, DamageType.Magic, false);

                        if (Random.Range(0f, 1f) <= flameArrowBurnChance)
                        {
                            StatusEffectManager enemyStatus = enemy.GetComponent<StatusEffectManager>();
                            if (enemyStatus != null)
                            {
                                int burnDamagePerTick = flameArrowBurnDamage + Mathf.RoundToInt(MagicDamage * 0.15f);
                                enemyStatus.ApplyBurn(burnDamagePerTick, flameArrowBurnDuration);
                            }
                        }
                    }
                }
            }

            if (arrowIndex == 0)
            {
                HasHitRateBonus = true;
                HitRateBonusEndTime = Time.time + flameArrowHitRateDuration;
                HitRate += flameArrowHitRateBonus;
            }
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
        AudioManager.instance.PlaySFX(11, 0.2f);

        if (arrowPrefab != null && target != null)
        {
            Vector3 startPos = arrowSpawnPoint != null ? arrowSpawnPoint.position : transform.position + Vector3.up;
            Vector3 targetPos = target.transform.position + Vector3.up;
            Vector3 direction = (targetPos - startPos).normalized;

            GameObject arrow = Instantiate(arrowPrefab, startPos, Quaternion.identity);

            SpriteRenderer arrowSprite = arrow.GetComponent<SpriteRenderer>();
            if (arrowSprite != null)
            {
                arrowSprite.color = new Color(1f, 0.5f, 0f);
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

                if (direction.x < 0)
                {
                    arrowSprite.flipY = true;
                    arrow.transform.rotation = Quaternion.Euler(0, 0, angle + 180f);
                }
                else
                {
                    arrowSprite.flipY = false;
                    arrow.transform.rotation = Quaternion.Euler(0, 0, angle);
                }
            }

            float distance = Vector3.Distance(startPos, targetPos);
            float travelTime = distance / arrowSpeed;
            float elapsed = 0f;

            while (elapsed < travelTime && target != null)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / travelTime;

                Vector3 currentTarget = target.transform.position + Vector3.up;
                arrow.transform.position = Vector3.Lerp(startPos, currentTarget, progress);

                Vector3 newDirection = (currentTarget - arrow.transform.position).normalized;
                float newAngle = Mathf.Atan2(newDirection.y, newDirection.x) * Mathf.Rad2Deg;

                if (arrowSprite != null)
                {
                    if (newDirection.x < 0)
                    {
                        arrowSprite.flipY = true;
                        arrow.transform.rotation = Quaternion.Euler(0, 0, newAngle + 180f);
                    }
                    else
                    {
                        arrowSprite.flipY = false;
                        arrow.transform.rotation = Quaternion.Euler(0, 0, newAngle);
                    }
                }

                yield return null;
            }

            Destroy(arrow, 0.1f);

            if (flameArrowEffect != null && target != null)
            {
                GameObject hitFX = Instantiate(flameArrowEffect.gameObject, target.transform.position + Vector3.up, Quaternion.identity);
                Destroy(hitFX, 2f);
            }
        }

        // ✅ เปลี่ยนจาก AttackDamage เป็น MagicDamage
        if (HasStateAuthority && target != null)
        {
            int damage = GetScaledSkillDamageWithAmp(flameArrowDamageMultiplier);
            target.TakeDamageFromAttacker(0, damage, this, DamageType.Magic, false);

            if (Random.Range(0f, 1f) <= flameArrowBurnChance)
            {
                StatusEffectManager targetStatus = target.GetComponent<StatusEffectManager>();
                if (targetStatus != null)
                {
                    int burnDamagePerTick = flameArrowBurnDamage + Mathf.RoundToInt(MagicDamage * 0.15f);
                    targetStatus.ApplyBurn(burnDamagePerTick, flameArrowBurnDuration);
                    Debug.Log($"🔥 Applied Burn to {target.CharacterName}!");
                }
            }

            HasHitRateBonus = true;
            HitRateBonusEndTime = Time.time + flameArrowHitRateDuration;
            HitRate += flameArrowHitRateBonus;
            Debug.Log($"🎯 Gained +15% Hit Rate!");
        }
    }
    private List<Character> FindNearestEnemies(float maxRange, int maxCount)
    {
        List<Character> enemies = new List<Character>();
        List<Character> allEnemies = new List<Character>();

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, maxRange);
        foreach (Collider col in hitColliders)
        {
            Character enemy = col.GetComponent<Character>();
            if (enemy != null && enemy != this)
            {
                bool isEnemy = false;
                if (this is Hero && enemy is NetworkEnemy) isEnemy = true;
                if (this is NetworkEnemy && enemy is Hero) isEnemy = true;

                if (isEnemy)
                {
                    allEnemies.Add(enemy);
                }
            }
        }

        allEnemies.Sort((a, b) =>
            Vector3.Distance(transform.position, a.transform.position)
            .CompareTo(Vector3.Distance(transform.position, b.transform.position))
        );

        for (int i = 0; i < Mathf.Min(maxCount, allEnemies.Count); i++)
        {
            enemies.Add(allEnemies[i]);
        }

        return enemies;
    }
    private void FaceTarget(Transform target)
    {
        if (target == null) return;

        // คำนวณทิศทางไปหาเป้าหมาย
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0; // เอาแค่แนวนอน

        // Flip character ตามทิศทาง X
        if (direction.x > 0.1f)
        {
            // ศัตรูอยู่ทางขวา - หันขวา
            Vector3 newScale = transform.localScale;
            newScale.x = Mathf.Abs(newScale.x);
            transform.localScale = newScale;
            NetworkedFlipX = false;
        }
        else if (direction.x < -0.1f)
        {
            // ศัตรูอยู่ทางซ้าย - หันซ้าย
            Vector3 newScale = transform.localScale;
            newScale.x = -Mathf.Abs(newScale.x);
            transform.localScale = newScale;
            NetworkedFlipX = true;
        }

        Debug.Log($"🎯 Archer facing target: direction.x = {direction.x:F2}, flipX = {NetworkedFlipX}");
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

        if (!IsInFerrisMode)
        {
            GainFerrisPoint(20);
        }
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
        AudioManager.instance.PlaySFX(12, 0.5f);
        statusEffectManager.ApplyArmorBreak(6f, 10f);

        // 🏹 ยิงธนูออกไปข้างหน้าก่อน Dash
        if (HasStateAuthority)
        {
            Vector3 forwardDirection = -direction; // ยิงไปทางตรงข้ามกับทิศทาง dash
            ShootShadowStepArrow(forwardDirection);
        }

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

    // ========== เพิ่มฟังก์ชันใหม่ทั้งหมดหลัง CreateSmokeBomb ==========

    // 🏹 ฟังก์ชันยิงธนูสำหรับ Shadow Step
    private void ShootShadowStepArrow(Vector3 direction)
    {
        if (IsInFerrisMode)
        {
            // 🔥 ยิงธนูไฟแบบ Flame Arrow
            ShootFlameArrowForward(direction);
        }
        else
        {
            // ยิงธนูธรรมดา
            ShootNormalArrowForward(direction);
        }
    }

    private void ShootFlameArrowForward(Vector3 direction)
    {
        float maxDistance = flameArrowRange;
        Character targetInPath = FindEnemyInDirection(direction, maxDistance);

        if (targetInPath != null)
        {
            RPC_ShootShadowStepFlameArrow(targetInPath.Object);
        }
        else
        {
            Vector3 targetPos = transform.position + direction * maxDistance;
            RPC_ShootShadowStepFlameArrow(null, targetPos);
        }
    }

    private void ShootNormalArrowForward(Vector3 direction)
    {
        float maxDistance = AttackRange * 1.5f;
        Character targetInPath = FindEnemyInDirection(direction, maxDistance);

        if (targetInPath != null)
        {
            RPC_ShootShadowStepNormalArrow(targetInPath.Object);
        }
        else
        {
            Vector3 targetPos = transform.position + direction * maxDistance;
            RPC_ShootShadowStepNormalArrow(null, targetPos);
        }
    }

    private Character FindEnemyInDirection(Vector3 direction, float maxDistance)
    {
        RaycastHit[] hits = Physics.RaycastAll(transform.position, direction, maxDistance);
        Character nearestEnemy = null;
        float nearestDistance = maxDistance;

        foreach (RaycastHit hit in hits)
        {
            Character enemy = hit.collider.GetComponent<Character>();
            if (enemy != null && enemy != this)
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

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShootShadowStepFlameArrow(NetworkObject targetObject, Vector3 fallbackPosition = default)
    {
        Character target = targetObject?.GetComponent<Character>();
        Vector3 targetPos = target != null ? target.transform.position : fallbackPosition;

        if (targetPos != default)
        {
            StartCoroutine(ExecuteShadowStepFlameArrow(target, targetPos));
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShootShadowStepNormalArrow(NetworkObject targetObject, Vector3 fallbackPosition = default)
    {
        Character target = targetObject?.GetComponent<Character>();
        Vector3 targetPos = target != null ? target.transform.position : fallbackPosition;

        if (targetPos != default)
        {
            StartCoroutine(ExecuteShadowStepNormalArrow(target, targetPos));
        }
    }

    private IEnumerator ExecuteShadowStepFlameArrow(Character target, Vector3 targetPos)
    {
        if (arrowPrefab == null) yield break;

        AudioManager.instance.PlaySFX(11, 0.2f);

        Vector3 startPos = arrowSpawnPoint != null ? arrowSpawnPoint.position : transform.position + Vector3.up;
        targetPos += Vector3.up;
        Vector3 direction = (targetPos - startPos).normalized;

        GameObject arrow = Instantiate(arrowPrefab, startPos, Quaternion.identity);

        SpriteRenderer arrowSprite = arrow.GetComponent<SpriteRenderer>();
        if (arrowSprite != null)
        {
            arrowSprite.color = new Color(1f, 0.5f, 0f); // สีส้มแดง
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            if (direction.x < 0)
            {
                arrowSprite.flipY = true;
                arrow.transform.rotation = Quaternion.Euler(0, 0, angle + 180f);
            }
            else
            {
                arrowSprite.flipY = false;
                arrow.transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        float distance = Vector3.Distance(startPos, targetPos);
        float travelTime = distance / arrowSpeed;
        float elapsed = 0f;

        while (elapsed < travelTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / travelTime;

            Vector3 currentTarget = target != null ? target.transform.position + Vector3.up : targetPos;
            arrow.transform.position = Vector3.Lerp(startPos, currentTarget, progress);

            Vector3 newDirection = (currentTarget - arrow.transform.position).normalized;
            float newAngle = Mathf.Atan2(newDirection.y, newDirection.x) * Mathf.Rad2Deg;

            if (arrowSprite != null)
            {
                if (newDirection.x < 0)
                {
                    arrowSprite.flipY = true;
                    arrow.transform.rotation = Quaternion.Euler(0, 0, newAngle + 180f);
                }
                else
                {
                    arrowSprite.flipY = false;
                    arrow.transform.rotation = Quaternion.Euler(0, 0, newAngle);
                }
            }

            yield return null;
        }

        Destroy(arrow, 0.1f);

        if (flameArrowEffect != null)
        {
            GameObject hitFX = Instantiate(flameArrowEffect.gameObject, targetPos, Quaternion.identity);
            Destroy(hitFX, 2f);
        }

        if (HasStateAuthority && target != null)
        {
            int damage = Mathf.RoundToInt(AttackDamage * flameArrowDamageMultiplier * 0.7f); // 70% ของดาเมจ Flame Arrow

            Collider[] hits = Physics.OverlapSphere(target.transform.position, flameArrowAOERadius);
            foreach (Collider col in hits)
            {
                Character enemy = col.GetComponent<Character>();
                if (enemy != null && enemy != this)
                {
                    bool isEnemy = false;
                    if (this is Hero && enemy is NetworkEnemy) isEnemy = true;
                    if (this is NetworkEnemy && enemy is Hero) isEnemy = true;

                    if (isEnemy)
                    {
                        enemy.TakeDamageFromAttacker(damage, 0, this, DamageType.Normal, false);

                        if (Random.Range(0f, 1f) <= flameArrowBurnChance * 0.5f)
                        {
                            StatusEffectManager enemyStatus = enemy.GetComponent<StatusEffectManager>();
                            if (enemyStatus != null)
                            {
                                int burnDamagePerTick = Mathf.RoundToInt(flameArrowBurnDamage * 0.7f);
                                enemyStatus.ApplyBurn(burnDamagePerTick, flameArrowBurnDuration * 0.7f);
                            }
                        }
                    }
                }
            }
        }
    }

    private IEnumerator ExecuteShadowStepNormalArrow(Character target, Vector3 targetPos)
    {
        if (arrowPrefab == null) yield break;

        AudioManager.instance.PlaySFX(10, 0.3f);

        Vector3 startPos = arrowSpawnPoint != null ? arrowSpawnPoint.position : transform.position + Vector3.up;
        targetPos += Vector3.up;
        Vector3 direction = (targetPos - startPos).normalized;

        GameObject arrow = Instantiate(arrowPrefab, startPos, Quaternion.identity);

        SpriteRenderer arrowSprite = arrow.GetComponent<SpriteRenderer>();
        if (arrowSprite != null)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            if (direction.x < 0)
            {
                arrowSprite.flipY = true;
                arrow.transform.rotation = Quaternion.Euler(0, 0, angle + 180f);
            }
            else
            {
                arrowSprite.flipY = false;
                arrow.transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        float distance = Vector3.Distance(startPos, targetPos);
        float travelTime = distance / arrowSpeed;
        float elapsed = 0f;

        while (elapsed < travelTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / travelTime;

            Vector3 currentTarget = target != null ? target.transform.position + Vector3.up : targetPos;
            arrow.transform.position = Vector3.Lerp(startPos, currentTarget, progress);

            Vector3 newDirection = (currentTarget - arrow.transform.position).normalized;
            float newAngle = Mathf.Atan2(newDirection.y, newDirection.x) * Mathf.Rad2Deg;

            if (arrowSprite != null)
            {
                if (newDirection.x < 0)
                {
                    arrowSprite.flipY = true;
                    arrow.transform.rotation = Quaternion.Euler(0, 0, newAngle + 180f);
                }
                else
                {
                    arrowSprite.flipY = false;
                    arrow.transform.rotation = Quaternion.Euler(0, 0, newAngle);
                }
            }

            yield return null;
        }

        Destroy(arrow, 0.1f);

        if (basicAttackEffect != null)
        {
            GameObject hitFX = Instantiate(basicAttackEffect.gameObject, targetPos, Quaternion.identity);
            Destroy(hitFX, 2f);
        }

        if (HasStateAuthority && target != null)
        {
            int damage = Mathf.RoundToInt(AttackDamage * 1.2f); // 120% ของดาเมจปกติ
            target.TakeDamageFromAttacker(damage, 0, this, DamageType.Normal, false);
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

        if (!IsInFerrisMode)
        {
            GainFerrisPoint(30);
        }
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
        AudioManager.instance.PlaySFX(13, 0.5f);

        if (fireVolleyEffect != null)
        {
            GameObject volleyFX = Instantiate(fireVolleyEffect.gameObject, targetPosition + Vector3.up * 1f, Quaternion.identity);
            Destroy(volleyFX, 3f);
        }

        yield return new WaitForSeconds(0.3f);

        if (HasStateAuthority)
        {
            List<Character> hitEnemies = FindEnemiesInArea(targetPosition, fireVolleyAOERadius, fireVolleyMaxTargets);

            foreach (Character enemy in hitEnemies)
            {
                if (enemy != null)
                {
                    int damage = GetScaledSkillDamageWithAmp(fireVolleyDamageMultiplier);
                    enemy.TakeDamageFromAttacker(0, damage, this, DamageType.Magic, false);

                    if (Random.Range(0f, 1f) <= fireVolleyBurnChance)
                    {
                        StatusEffectManager enemyStatus = enemy.GetComponent<StatusEffectManager>();
                        if (enemyStatus != null)
                        {
                            int burnDamagePerTick = fireVolleyBurnDamage + Mathf.RoundToInt(MagicDamage * 0.12f);
                            enemyStatus.ApplyBurn(burnDamagePerTick, fireVolleyBurnDuration);
                            Debug.Log($"🔥 Applied Burn to {enemy.CharacterName}!");
                        }
                    }
                }
            }

            // 🆕 ใช้ Skill Attack Speed Bonus แยกออกจาก Stack System
            HasAttackSpeedBonusFromSkill = true;
            AttackSpeedBonusFromSkillEndTime = Time.time + fireVolleyAttackSpeedDuration;
            AttackSpeedBonusFromSkillAmount = fireVolleyAttackSpeedBonus;
            Debug.Log($"⚡ Gained +{fireVolleyAttackSpeedBonus * 100f:F0}% Attack Speed from Fire Volley!");
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

        if (!IsInFerrisMode)
        {
            GainFerrisPoint(40);
        }
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
    AudioManager.instance.PlaySFX(14, 0.5f);

    if (infernoRainEffect != null)
    {
        GameObject infernoFX = Instantiate(infernoRainEffect.gameObject, targetPosition + Vector3.up * 1f, Quaternion.identity);
        Destroy(infernoFX, 5f);
    }

    yield return new WaitForSeconds(0.5f);

    // ✅ เปลี่ยนเป็น MagicDamage
    if (HasStateAuthority)
    {
        List<Character> hitEnemies = FindEnemiesInArea(targetPosition, infernoRainAOERadius, infernoRainMaxTargets);

        foreach (Character enemy in hitEnemies)
        {
            if (enemy != null)
            {
                    int damage = GetScaledSkillDamageWithAmp(infernoRainDamageMultiplier);

                    bool isCritical = Random.Range(0f, 1f) <= CriticalChance;
                DamageType damageType = isCritical ? DamageType.Critical : DamageType.Magic;

                if (isCritical)
                {
                    damage = Mathf.RoundToInt(damage * (1f + CriticalDamageBonus));
                    Debug.Log($"💥 CRITICAL HIT!");
                }

                enemy.TakeDamageFromAttacker(0, damage, this, damageType, false);

                StatusEffectManager enemyStatus = enemy.GetComponent<StatusEffectManager>();
                if (enemyStatus != null)
                {
                    int burnDamagePerTick = infernoRainEnhancedBurnDamage + Mathf.RoundToInt(MagicDamage * 0.3f);
                    enemyStatus.ApplyBurn(burnDamagePerTick, infernoRainEnhancedBurnDuration);
                    Debug.Log($"🔥🔥 Applied Enhanced Burn to {enemy.CharacterName}!");
                }
            }
        }

        CreateFireRing(targetPosition);

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
        if (!hasEagleEye || !HasStateAuthority) return;
        if (target == this) return;

        float doubleShootChance = IsInFerrisMode ? ferrisDoubleShot : 0.25f;

        if (damageType == DamageType.Normal && Random.Range(0f, 1f) <= doubleShootChance)
        {
            Debug.Log($"🦅 Eagle Eye! Double Shot activated! (Chance: {doubleShootChance * 100}%)");
            StartCoroutine(DelayedExtraShot(target, 0.2f));
        }

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
    // ใน Archer.cs - แทนที่ ProcessTemporaryBuffs

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

        // 🆕 Process Attack Speed Bonus From Skill (แยกจาก Stack)
        if (HasAttackSpeedBonusFromSkill && Time.time >= AttackSpeedBonusFromSkillEndTime)
        {
            HasAttackSpeedBonusFromSkill = false;
            AttackSpeedBonusFromSkillAmount = 0f;
            Debug.Log($"⚡ Attack Speed bonus from skill expired");
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
    // ========== แก้ไข TryAttack - แสดง Attack Range ==========
    public override void TryAttack()
    {
        if (!HasInputAuthority || !IsSpawned) return;
        if (Time.time < nextAttackTime) return;

        float effectiveRange = IsInFerrisMode ? AttackRange * ferrisAttackRangeMultiplier : AttackRange;

        // ✅ แสดง Attack Range Indicator

        Collider[] enemies = Physics.OverlapSphere(transform.position, effectiveRange, LayerMask.GetMask("Enemy"));

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
                FaceTarget(nearestEnemy.transform);

                if (IsInFerrisMode)
                {
                    RPC_PerformFerrisModeAttack(nearestEnemy.Object);
                }
                else
                {
                    RPC_PerformArcherAttack(nearestEnemy.Object);
                }

                float cooldownReduction = GetEffectiveAttackSpeed();
                float finalAttackCooldown = AttackCooldown * (1f - cooldownReduction);
                nextAttackTime = Time.time + finalAttackCooldown;
            }
        }

        // ✅ ซ่อน Attack Range หลัง 0.5 วินาที
        StartCoroutine(HideAttackRangeAfterDelay(0.5f));
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_PerformFerrisModeAttack(NetworkObject enemyObject)
    {
        if (enemyObject != null)
        {
            Character enemy = enemyObject.GetComponent<Character>();
            if (enemy != null)
            {
                currentAttackTarget = enemyObject;
                float attackSpeedMultiplier = GetAttackSpeedMultiplierForUI();
                RPC_PlayAttackAnimation(attackSpeedMultiplier);
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
                // ✅ เก็บ reference ของเป้าหมาย
                currentAttackTarget = enemyObject;

                // ✅ คำนวณ Attack Speed multiplier
                float attackSpeedMultiplier = GetAttackSpeedMultiplierForUI();

                // ✅ เล่น Attack Animation (Animation Event จะเรียก OnArrowRelease() เอง)
                RPC_PlayAttackAnimation(attackSpeedMultiplier);
            }
        }
    }
    // ========================================
    // ✅ เพิ่ม RPC สำหรับเล่น Animation บนทุก Client
    // ========================================

    public void OnArrowRelease()
    {
        if (!HasStateAuthority) return;

        Debug.Log("🏹 [Animation Event] Arrow Released!");

        if (currentAttackTarget != null)
        {
            Character enemy = currentAttackTarget.GetComponent<Character>();
            if (enemy != null)
            {
                if (IsInFerrisMode)
                {
                    RPC_ShootFerrisModeArrows(currentAttackTarget);
                    StartCoroutine(DelayedFerrisModeDamage(currentAttackTarget));
                }
                else
                {
                    RPC_ShootArrowVisual(currentAttackTarget);
                    StartCoroutine(DelayedDamageAfterArrowHit(currentAttackTarget));
                }
            }

            currentAttackTarget = null;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShootFerrisModeArrows(NetworkObject targetObject)
    {
        if (targetObject != null)
        {
            Character target = targetObject.GetComponent<Character>();
            if (target != null)
            {
                List<Character> targets = FindNearestEnemies(AttackRange * ferrisAttackRangeMultiplier, 2);

                if (targets.Count == 0)
                {
                    targets.Add(target);
                }

                if (targets.Count == 1)
                {
                    StartCoroutine(ShootArrowVisual(targets[0].transform));
                    StartCoroutine(ShootArrowVisual(targets[0].transform, 0.1f));
                }
                else
                {
                    StartCoroutine(ShootArrowVisual(targets[0].transform));
                    StartCoroutine(ShootArrowVisual(targets[1].transform, 0.1f));
                }
            }
        }
    }

    // ใน Archer.cs - แทนที่ method เดิม

    private IEnumerator DelayedFerrisModeDamage(NetworkObject enemyObject)
    {
        if (enemyObject == null) yield break;

        Character enemy = enemyObject.GetComponent<Character>();
        if (enemy == null) yield break;

        float distance = Vector3.Distance(transform.position, enemy.transform.position);
        float arrowTravelTime = distance / arrowSpeed;

        yield return new WaitForSeconds(arrowTravelTime);

        if (enemy != null)
        {
            List<Character> targets = FindNearestEnemies(AttackRange * ferrisAttackRangeMultiplier, 2);

            if (targets.Count == 0)
            {
                targets.Add(enemy);
            }

            if (targets.Count == 1)
            {
                enemy.TakeDamageFromAttacker(AttackDamage, 0, this, DamageType.Normal, true);
                enemy.TakeDamageFromAttacker(AttackDamage, 0, this, DamageType.Normal, true);

                // 🆕 เพิ่ม Stack 2 ครั้ง (เพราะยิง 2 นัด)
                AddAttackSpeedStack();
                AddAttackSpeedStack();

                if (!IsInFerrisMode)
                {
                    GainFerrisPoint(3);
                }
            }
            else
            {
                targets[0].TakeDamageFromAttacker(AttackDamage, 0, this, DamageType.Normal, true);
                targets[1].TakeDamageFromAttacker(AttackDamage, 0, this, DamageType.Normal, true);

                // 🆕 เพิ่ม Stack 2 ครั้ง
                AddAttackSpeedStack();
                AddAttackSpeedStack();

                if (!IsInFerrisMode)
                {
                    GainFerrisPoint(4);
                }
            }
        }
    }
    private IEnumerator ShootArrowVisual(Transform target, float delay = 0f)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (arrowPrefab == null || target == null) yield break;

        AudioManager.instance.PlaySFX(10, 0.5f);

        Vector3 startPos = arrowSpawnPoint != null ? arrowSpawnPoint.position : transform.position + Vector3.up;
        Vector3 targetPos = target.position + Vector3.up;
        Vector3 direction = (targetPos - startPos).normalized;

        GameObject arrow = Instantiate(arrowPrefab, startPos, Quaternion.identity);

        SpriteRenderer arrowSprite = arrow.GetComponent<SpriteRenderer>();
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        if (arrowSprite != null)
        {
            if (direction.x < 0)
            {
                arrowSprite.flipY = true;
                arrow.transform.rotation = Quaternion.Euler(0, 0, angle + 180f);
            }
            else
            {
                arrowSprite.flipY = false;
                arrow.transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        float distance = Vector3.Distance(startPos, targetPos);
        float travelTime = distance / arrowSpeed;
        float elapsed = 0f;

        while (elapsed < travelTime && target != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / travelTime;

            Vector3 currentTarget = target.position + Vector3.up;
            arrow.transform.position = Vector3.Lerp(startPos, currentTarget, progress);

            Vector3 newDirection = (currentTarget - arrow.transform.position).normalized;
            float newAngle = Mathf.Atan2(newDirection.y, newDirection.x) * Mathf.Rad2Deg;

            if (arrowSprite != null)
            {
                if (newDirection.x < 0)
                {
                    arrowSprite.flipY = true;
                    arrow.transform.rotation = Quaternion.Euler(0, 0, newAngle + 180f);
                }
                else
                {
                    arrowSprite.flipY = false;
                    arrow.transform.rotation = Quaternion.Euler(0, 0, newAngle);
                }
            }

            yield return null;
        }

        Destroy(arrow, 0.1f);
    }

    private NetworkObject currentAttackTarget; // เก็บ reference ของเป้าหมายปัจจุบัน
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShootArrowVisual(NetworkObject targetObject)
    {
        if (targetObject != null)
        {
            Character target = targetObject.GetComponent<Character>();
            if (target != null)
            {
                StartCoroutine(ShootArrowVisual(target.transform));
            }
        }
    }
    private IEnumerator DelayedDamageAfterArrowHit(NetworkObject enemyObject)
    {
        if (enemyObject == null) yield break;

        Character enemy = enemyObject.GetComponent<Character>();
        if (enemy == null) yield break;

        float distance = Vector3.Distance(transform.position, enemy.transform.position);
        float arrowTravelTime = distance / arrowSpeed;

        yield return new WaitForSeconds(arrowTravelTime);

        if (enemy != null)
        {
            enemy.TakeDamageFromAttacker(AttackDamage, 0, this, DamageType.Normal, true);

            // 🆕 เพิ่ม Attack Speed Stack เมื่อโจมตีปกติโดน
            AddAttackSpeedStack();

            Debug.Log($"🏹 Dealt {AttackDamage} damage");
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayAttackAnimation(float speedMultiplier)
    {
        if (animator != null)
        {
            // ✅ ตั้งค่า Animation Speed ตาม Attack Speed
            animator.speed = speedMultiplier;

            // เล่น Attack Animation
            animator.SetTrigger(ANIM_ATTACK);

            Debug.Log($"🎬 Playing Attack animation for {CharacterName} at {speedMultiplier:F2}x speed");

            // ✅ รีเซ็ต animation speed หลังจาก animation เสร็จ
            StartCoroutine(ResetAnimationSpeed());
        }
    }
    private IEnumerator ResetAnimationSpeed()
    {
        // รอให้ animation เล่นเสร็จ (ประมาณ 1 วินาที)
        yield return new WaitForSeconds(1f);

        if (animator != null)
        {
            animator.speed = 1f; // รีเซ็ตกลับเป็นความเร็วปกติ
        }
    }


    // ========================================
    // 4. RPC_OnArcherAttackHit - แสดง Visual Effect
    // ========================================




    // ========================================
    // 5. ShootArrowVisual - ลูกศรบินไปหาเป้าหมาย (Visual)
    // ========================================

    private IEnumerator ShootArrowVisual(Transform target)
    {
        if (arrowPrefab == null || target == null) yield break;

        AudioManager.instance.PlaySFX(10, 0.5f);

        Vector3 startPos = arrowSpawnPoint != null ? arrowSpawnPoint.position : transform.position + Vector3.up;
        Vector3 targetPos = target.position + Vector3.up;
        Vector3 direction = (targetPos - startPos).normalized;

        // ✅ สร้างลูกธนูโดยไม่ใช้ Quaternion.LookRotation ก่อน
        GameObject arrow = Instantiate(arrowPrefab, startPos, Quaternion.identity);

        // ✅ เช็ค Sprite Renderer
        SpriteRenderer arrowSprite = arrow.GetComponent<SpriteRenderer>();

        // ✅ คำนวณมุมการหมุน (2D Sprite)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        if (arrowSprite != null)
        {
            // ✅ Flip ถ้าลูกธนูยิงไปทางซ้าย
            if (direction.x < 0)
            {
                arrowSprite.flipY = true;
                arrow.transform.rotation = Quaternion.Euler(0, 0, angle + 180f);
            }
            else
            {
                arrowSprite.flipY = false;
                arrow.transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
        else
        {
            // ✅ ถ้าเป็น 3D Model ใช้ LookRotation ปกติ
            arrow.transform.rotation = Quaternion.LookRotation(direction);
        }

        float distance = Vector3.Distance(startPos, targetPos);
        float travelTime = distance / arrowSpeed;
        float elapsed = 0f;

        while (elapsed < travelTime && target != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / travelTime;

            Vector3 currentTarget = target.position + Vector3.up;
            arrow.transform.position = Vector3.Lerp(startPos, currentTarget, progress);

            // ✅ อัพเดททิศทางและมุมระหว่างบิน
            Vector3 newDirection = (currentTarget - arrow.transform.position).normalized;
            float newAngle = Mathf.Atan2(newDirection.y, newDirection.x) * Mathf.Rad2Deg;

            if (arrowSprite != null)
            {
                if (newDirection.x < 0)
                {
                    arrowSprite.flipY = true;
                    arrow.transform.rotation = Quaternion.Euler(0, 0, newAngle + 180f);
                }
                else
                {
                    arrowSprite.flipY = false;
                    arrow.transform.rotation = Quaternion.Euler(0, 0, newAngle);
                }
            }
            else
            {
                arrow.transform.rotation = Quaternion.LookRotation(newDirection);
            }

            yield return null;
        }

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

    public override bool UsesFerrisPoint()
    {
        return true;
    }

    protected override void OnFerrisPointFull()
    {
        base.OnFerrisPointFull();
        if (!IsInFerrisMode)
        {
            EnterFerrisMode();
        }
    }

    private void EnterFerrisMode()
    {
        if (!HasStateAuthority) return;

        IsInFerrisMode = true;
        accumulatedDrain = 0f;

        // 🆕 ใช้ Attack Speed Aura แทน (ไม่ทับ Skill Bonus)
        statusEffectManager.ApplyAttackSpeedAura(6f, 0.30f, 10f);
        statusEffectManager.ApplyHitRateAura(6f, 0.15f, 10f);
        ResetAllSkillCooldowns();
        RPC_ShowFerrisModeText();
        Debug.Log($"🌟 [FERRIS MODE ACTIVATED] {CharacterName} - Double Arrows + 1.5x Range!");

        if (HasInputAuthority)
        {
            CombatUIManager uiManager = FindObjectOfType<CombatUIManager>();
            if (uiManager != null)
            {
                SkillIconManager.Instance.SetSkillIconUpgraded(uiManager, "Archer", 1, true);
                SkillIconManager.Instance.SetSkillIconUpgraded(uiManager, "Archer", 2, true);
            }
        }
    }
    private void ExitFerrisMode()
    {
        if (!HasStateAuthority) return;

        IsInFerrisMode = false;
        CurrentFerrisPoint = 0;
        accumulatedDrain = 0f;

        RPC_ShowFerrisModeEndText();
        Debug.Log($"🌟 [Ferris Mode Ended] {CharacterName}");

        if (HasInputAuthority)
        {
            CombatUIManager uiManager = FindObjectOfType<CombatUIManager>();
            if (uiManager != null)
            {
                SkillIconManager.Instance.SetSkillIconUpgraded(uiManager, "Archer", 1, false);
                SkillIconManager.Instance.SetSkillIconUpgraded(uiManager, "Archer", 2, false);
            }
        }
    }

    private void DrainFerrisPoints(float deltaTime)
    {
        if (!IsInFerrisMode) return;

        accumulatedDrain += ferrisModeDrainRate * deltaTime;

        if (accumulatedDrain >= 1f)
        {
            int drainAmount = Mathf.FloorToInt(accumulatedDrain);
            CurrentFerrisPoint = Mathf.Max(0, CurrentFerrisPoint - drainAmount);
            accumulatedDrain -= drainAmount;

            if (CurrentFerrisPoint <= 0)
            {
                accumulatedDrain = 0f;
                ExitFerrisMode();
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowFerrisModeText()
    {
        Vector3 textPosition = transform.position + Vector3.up * 1f;
        Color ferrisColor = new Color(1f, 0.8f, 0f, 1f);
        DamageTextManager.ShowCustomText(textPosition, "Ferris MODE", ferrisColor, 1f);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowFerrisModeEndText()
    {
        Vector3 textPosition = transform.position + Vector3.up * 1f;
        Color endColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        DamageTextManager.ShowCustomText(textPosition, "Ferris MODE End", endColor, 1.0f);
    }
    // ========== เพิ่ม Method สร้าง Attack Range Indicator ==========
    // ========== แก้ไข CreateAttackRangeIndicator - ไม่สร้าง Auto ==========
    // ========== แก้ไข CreateAttackRangeIndicator - สร้างวงกลมใน Code ==========
    

    private IEnumerator HideAttackRangeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideAttackRangeIndicator();
    }

    private void HideAttackRangeIndicator()
    {
        if (attackRangeIndicator != null && isShowingAttackRange)
        {
            attackRangeIndicator.SetActive(false);
            isShowingAttackRange = false;
        }
    }
    private void ProcessAttackSpeedStacks()
    {
        if (AttackSpeedStacks > 0 && Time.time >= AttackSpeedStacksEndTime)
        {
            AttackSpeedStacks = 0;
            Debug.Log($"⚡ [Attack Speed Stacks] {CharacterName} stacks expired");
        }
    }

    /// <summary>
    /// 🆕 เพิ่ม Attack Speed Stack เมื่อโจมตีปกติโดนเป้าหมาย
    /// </summary>
    public void AddAttackSpeedStack()
    {
        if (!HasStateAuthority) return;

        AttackSpeedStacks = Mathf.Min(AttackSpeedStacks + 1, MAX_ATTACK_SPEED_STACKS);
        AttackSpeedStacksEndTime = Time.time + STACK_DURATION;

        float totalBonus = AttackSpeedStacks * STACK_BONUS_PER_STACK;
        Debug.Log($"⚡ [Attack Speed Stack] {CharacterName}: +1 stack → {AttackSpeedStacks}/{MAX_ATTACK_SPEED_STACKS} ({totalBonus * 100f:F0}%)");
    }

    /// <summary>
    /// 🆕 คำนวณ Attack Speed จาก Stacks (สำหรับ GetEffectiveAttackSpeed)
    /// </summary>
    public float GetAttackSpeedBonusFromStacks()
    {
        float stackBonus = AttackSpeedStacks * STACK_BONUS_PER_STACK * 100f; // แปลงเป็น %
        return stackBonus;
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
    private int CalculateArcherBaseDamage()
    {
        int levelBonus = GetCurrentLevel() * 4; // +4 per level
        int baseDamage = MagicDamage + levelBonus;

        Debug.Log($"🏹 [Damage Calc] MAGIC={MagicDamage} + Lv{GetCurrentLevel()}×4 = {baseDamage}");

        return baseDamage;
    }

    /// <summary>
    /// คำนวณ Skill Damage พร้อม Amp Damage
    /// </summary>
    private int GetScaledSkillDamageWithAmp(float multiplier)
    {
        int baseDamage = CalculateArcherBaseDamage();
        int skillDamage = Mathf.RoundToInt(baseDamage * multiplier);

        // Apply Amp Damage
        int finalDamage = ApplyAmpDamageToSkill(skillDamage);


        return finalDamage;
    }
}