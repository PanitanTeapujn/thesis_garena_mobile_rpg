using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class IceBoss : NetworkEnemy
{
    [Header("?? Ice Boss Settings")]
    [SerializeField] private float phase2HPThreshold = 0.6f; // 60% HP
    private bool hasEnteredPhase2 = false;

    [Header("?? Skill Cooldowns")]
    [SerializeField] private float halfCircleAttackCooldown = 3f;
    [SerializeField] private float jumpSlamCooldown = 5f;
    [SerializeField] private float dashWithTrailCooldown = 6f;
    [SerializeField] private float iceProjectileCooldown = 4f;
    [SerializeField] private float iceMirrorComboCooldown = 20f;
    [SerializeField] private float enhancedJumpSlamCooldown = 7f;
    [SerializeField] private float cloneDashCooldown = 8f;
    [SerializeField] private float cloneProjectileCooldown = 8f;
    [SerializeField] private ParticleSystem halfCircleEffect; // ✅ เพิ่มบรรทัดนี้

    [Header("?? Skill 1: Half-Circle Attack")]
    [SerializeField] private float halfCircleRadius = 4f;
    [SerializeField] private float halfCircleAngle = 180f;
    [SerializeField] private float halfCircleTelegraphDuration = 1.5f;
    [SerializeField] private GameObject halfCircleTelegraphPrefab;

    [Header("?? Skill 2: Jump Slam")]
    [SerializeField] private float jumpHeight = 5f;
    [SerializeField] private float jumpDuration = 0.6f;
    [SerializeField] private float slamRadius = 5f;
    [SerializeField] private float slamTelegraphDuration = 1.2f;
    [SerializeField] private float freezeDuration = 3f;
    [SerializeField] private GameObject slamTelegraphPrefab;
    [SerializeField] private ParticleSystem jumpSlamEffect;

    [Header("💥 Skill 3: Dash With Trail")]
    [SerializeField] private float dashDistance = 10f;
    [SerializeField] private float dashDuration = 0.4f;
    [SerializeField] private float dashWidth = 2.5f;
    [SerializeField] private float dashTelegraphDuration = 1.2f;
    [SerializeField] private GameObject dashTelegraphPrefab;
    [SerializeField] private GameObject iceTrailPrefab;
    [SerializeField] private float iceTrailLifetime = 6f;
    [SerializeField] private float slowAmount = 0.5f; // 50% slow
    [SerializeField] private float slowDuration = 2f;
    [SerializeField] private ParticleSystem dashEffect;
    [Header("💥 Skill 3: Dash End Slow AoE")]
    [SerializeField] private float dashEndSlowAoERadius = 5f; // รัศมี AoE
    [SerializeField] private float dashEndSlowAoEDuration = 3f; // ระยะเวลา AoE อยู่
    [SerializeField] private float dashEndSlowAmount = 0.4f; // 40% slow
    [SerializeField] private float dashEndSlowEffectDuration = 2f; // ระยะเวลา slow บนผู้เล่น
    [SerializeField] private GameObject dashEndSlowAoEPrefab; // Prefab วงกลม Slow AoE
    [SerializeField] private ParticleSystem dashEndSlowEffect; // Particle Effect
    [Header("?? Skill 4: Ice Projectile")]
    [SerializeField] private float projectileTelegraphDuration = 1.5f;
    [SerializeField] private float projectileTravelTime = 0.8f;
    [SerializeField] private float projectileRadius = 2f;
    [SerializeField] private GameObject projectileTelegraphPrefab;
    [SerializeField] private GameObject iceProjectilePrefab;
    [SerializeField] private ParticleSystem projectileEffect;

    [Header("🧊 Skill 5: Ice Mirror Combo")]
    [SerializeField] private GameObject iceBossClonePrefab;
    [SerializeField] private float cloneSpawnRadius = 8f;
    [SerializeField] private float cloneSpawnDelay = 0.15f; // ✅ ลดจาก 0.3f → 0.15f
    [SerializeField] private float cloneDashDelay = 0.3f; // ✅ ลดจาก 0.8f → 0.3f
    [SerializeField] private GameObject cloneSpawnEffect;
    [SerializeField] private int eightDirectionSpikes = 8;
    [SerializeField] private float spikeDistance = 12f;
    [SerializeField] private float spikeWidth = 2f;
    [SerializeField] private GameObject spikeTelegraphPrefab;

    // ✅ เพิ่มตัวแปรสำหรับ Clone Dash Speed
    [Header("🧊 Clone Dash Settings")]
    [SerializeField] private float cloneDashSpeed = 0.25f; // ✅ เร็วกว่า Boss (0.4f)
    [SerializeField] private float cloneDashDistance = 12f; // ✅ ไกลกว่า Boss นิดหน่อย

    [Header("?? Skill 6: Enhanced Jump Slam")]
    [SerializeField] private int minRandomSpikes = 2;
    [SerializeField] private int maxRandomSpikes = 6;

    [Header("?? Skill 7 & 8: Clone Skills")]
    [SerializeField] private int minCloneCount = 1;
    [SerializeField] private int maxCloneCount = 2;

    [Header("?? Visual Effects")]
    [SerializeField] private float telegraphYOffset = -1.16f;
    [SerializeField] private Color iceTelegraphColor = new Color(0.5f, 0.7f, 1f, 0.5f);

    [Header("?? Animation")]
    private Animator animator;
    private const string ANIM_ATTACK = "attack";
    private const string ANIM_SKILL1 = "skill1";
    private const string ANIM_SKILL2 = "skill2";

    [Header("?? Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    // Private variables
    private bool isPerformingSkill = false;
    private GameObject currentTelegraph;
    private List<GameObject> activeTelegraphs = new List<GameObject>();
    private List<GameObject> activeIceTrails = new List<GameObject>();
    private List<IceBossClone> activeClones = new List<IceBossClone>();
    private Vector3 originalPosition;
    private List<Hero> hitHeroes = new List<Hero>();

    private enum BossPhase
    {
        Phase1,
        Phase2
    }

    private enum SkillType
    {
        HalfCircleAttack = 1,
        JumpSlam = 2,
        DashWithTrail = 3,
        IceProjectile = 4,
        IceMirrorCombo = 5,
        EnhancedJumpSlam = 6,
        CloneDash = 7,
        CloneProjectile = 8
    }

    [Networked] private BossPhase CurrentPhase { get; set; }
    [Networked] private float NextSkillTime { get; set; }

    protected override void Start()
    {
        base.Start();
        SetupIceBossStats();
        CurrentPhase = BossPhase.Phase1;
        originalPosition = transform.position;
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogWarning($"?? Animator not found on {CharacterName}!");
        }

        Debug.Log($"?? {CharacterName} Boss spawned!");
    }

    private void SetupIceBossStats()
    {
        CharacterName = "Ice Boss";
     
      
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (HasStateAuthority && !IsDead)
        {
            CheckPhaseTransition();

            // ✅ เพิ่มการเช็คนี้ - ถ้ากำลังใช้สกิล ไม่ให้ทำอะไร
            if (!isPerformingSkill)
            {
                ProcessBossAI();
            }

            NetworkedPosition = transform.position;

            if (rb != null)
            {
                NetworkedVelocity = rb.linearVelocity;
            }
        }
        else
        {
            ApplyNetworkState();
        }
    }
    protected override void TryAttackTarget()
    {
        // ✅ Ice Boss ไม่ใช้การโจมตีปกติ - ใช้สกิลเท่านั้น
        // ปิดการทำงานของ NetworkEnemy.TryAttackTarget()

        // ไม่ทำอะไร - Boss ใช้ระบบสกิลจาก ProcessBossAI() แทน
        Debug.Log($"🧊 Ice Boss: Basic attack disabled (skill-based combat only)");
    }
    // ========== Phase Management ==========
    private void CheckPhaseTransition()
    {
        float hpPercentage = (float)CurrentHp / MaxHp;

        if (!hasEnteredPhase2 && hpPercentage <= phase2HPThreshold)
        {
            hasEnteredPhase2 = true;
            CurrentPhase = BossPhase.Phase2;
            Debug.Log($"🧊 {CharacterName}: PHASE 2 ACTIVATED!");

            // ✅ บังคับใช้ Ice Mirror Combo ทันที (ไม่สนใจว่ากำลังทำอะไรอยู่)
            StartCoroutine(ForceIceMirrorCombo());
        }
    }

    // ✅ Method สำหรับบังคับใช้ท่า Phase 2
    private IEnumerator ForceIceMirrorCombo()
    {
        // ✅ รอให้สกิลปัจจุบันจบก่อน (ถ้ามี)
        while (isPerformingSkill)
        {
            Debug.Log($"🧊 Waiting for current skill to finish...");
            yield return new WaitForSeconds(0.5f);
        }

        // ✅ บังคับใช้ Ice Mirror Combo
        Debug.Log($"🧊🧊🧊 FORCING ICE MIRROR COMBO NOW!");
        yield return StartCoroutine(PerformSkill(SkillType.IceMirrorCombo));
    }

    // ========== AI System ==========
    private void ProcessBossAI()
    {
        if (isPerformingSkill) return;

        if (targetTransform == null)
        {
            FindNearestPlayer();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, targetTransform.position);

        if (Runner.SimulationTime >= NextSkillTime && distanceToPlayer <= 15f)
        {
            SkillType selectedSkill = SelectRandomSkill();
            StartCoroutine(PerformSkill(selectedSkill));
        }
    }

    // ========== Skill Selection ==========
    private SkillType SelectRandomSkill()
    {
        if (CurrentPhase == BossPhase.Phase1)
        {
            // Phase 1: Skill 1-4
            int[] skills = { 1, 2, 3, 4 };
            int randomIndex = Random.Range(0, skills.Length);
            return (SkillType)skills[randomIndex];
        }
        else
        {
            // Phase 2: Skill 1-4, 6, 7, 8
            int[] skills = { 1, 2, 3, 4, 6, 7, 8 };
            int randomIndex = Random.Range(0, skills.Length);
            return (SkillType)skills[randomIndex];
        }
    }

    private SkillType SelectBossSkillDuringClone()
    {
        if (CurrentPhase == BossPhase.Phase1)
        {
            // Skill 1-4
            int[] skills = { 1, 2, 3, 4 };
            return (SkillType)skills[Random.Range(0, skills.Length)];
        }
        else
        {
            // Skill 1-4, 6
            int[] skills = { 1, 2, 3, 4, 6 };
            return (SkillType)skills[Random.Range(0, skills.Length)];
        }
    }

    // ========== Main Skill Executor ==========
    private IEnumerator PerformSkill(SkillType skill)
    {
        isPerformingSkill = true;
        StopMovement();
        originalPosition = transform.position;

        Debug.Log($"?????? {CharacterName}: *** STARTING SKILL {skill} ***");

        switch (skill)
        {
            case SkillType.HalfCircleAttack:
                Debug.Log(">>> Executing HalfCircleAttack");
                yield return StartCoroutine(Skill1_HalfCircleAttack());
                NextSkillTime = Runner.SimulationTime + halfCircleAttackCooldown;
                break;

            case SkillType.JumpSlam:
                Debug.Log(">>> Executing JumpSlam");
                yield return StartCoroutine(Skill2_JumpSlam());
                NextSkillTime = Runner.SimulationTime + jumpSlamCooldown;
                break;

            case SkillType.DashWithTrail:
                Debug.Log(">>> Executing DashWithTrail");
                yield return StartCoroutine(Skill3_DashWithTrail());
                NextSkillTime = Runner.SimulationTime + dashWithTrailCooldown;
                break;

            case SkillType.IceProjectile:
                Debug.Log(">>> Executing IceProjectile");
                yield return StartCoroutine(Skill4_IceProjectile());
                NextSkillTime = Runner.SimulationTime + iceProjectileCooldown;
                break;

            case SkillType.IceMirrorCombo:
                Debug.Log(">>> Executing IceMirrorCombo");
                yield return StartCoroutine(Skill5_IceMirrorCombo());
                NextSkillTime = Runner.SimulationTime + iceMirrorComboCooldown;
                break;

            case SkillType.EnhancedJumpSlam:
                Debug.Log(">>> Executing EnhancedJumpSlam");
                yield return StartCoroutine(Skill6_EnhancedJumpSlam());
                NextSkillTime = Runner.SimulationTime + enhancedJumpSlamCooldown;
                break;

            case SkillType.CloneDash:
                Debug.Log(">>> Executing CloneDash");
                yield return StartCoroutine(Skill7_CloneDash());
                NextSkillTime = Runner.SimulationTime + cloneDashCooldown;
                break;

            case SkillType.CloneProjectile:
                Debug.Log(">>> Executing CloneProjectile");
                yield return StartCoroutine(Skill8_CloneProjectile());
                NextSkillTime = Runner.SimulationTime + cloneProjectileCooldown;
                break;
        }

        ResumeMovement();
        isPerformingSkill = false;

        Debug.Log($"??? {CharacterName}: SKILL {skill} COMPLETE!");
    }

    // ========== SKILL 1: Half-Circle Attack ==========
    private IEnumerator Skill1_HalfCircleAttack()
    {
        Debug.Log($"🧊 Skill 1: Half-Circle Attack");

        // ✅ Reset hit list
        hitHeroes.Clear();

        // ✅ เริ่ม Slow Aura รอบตัว
        StartCoroutine(ApplySlowAura());

        // Telegraph
        yield return StartCoroutine(ShowHalfCircleTelegraph());

        // Play animation
        RPC_PlayAnimation(ANIM_ATTACK);

        yield return new WaitForSeconds(0.3f);

        // Execute attack
        ExecuteHalfCircleAttack();

        yield return new WaitForSeconds(1f);
    }
    // ========== เพิ่ม Method สำหรับ Slow Aura ==========
    [Header("💥 Skill 1: Slow Aura")]
    [SerializeField] private float slowAuraRadius = 10f;
    [SerializeField] private float slowAuraAmount = 0.3f; // 30% slow
    [SerializeField] private float slowAuraDuration = 2f;
    [SerializeField] private float slowAuraTickInterval = 0.5f; // ทุก 0.5 วินาที

    private IEnumerator ApplySlowAura()
    {
        Debug.Log($"🧊 Slow Aura activated! Radius: {slowAuraRadius}m, Slow: {slowAuraAmount * 100}%");

        float elapsed = 0f;
        float nextTickTime = 0f;

        while (elapsed < halfCircleTelegraphDuration + 0.3f) // ช่วงเวลา telegraph + execute
        {
            elapsed += Time.deltaTime;

            if (elapsed >= nextTickTime)
            {
                // หาผู้เล่นในรัศมี
                Collider[] targets = Physics.OverlapSphere(transform.position, slowAuraRadius, LayerMask.GetMask("Player"));

                foreach (Collider target in targets)
                {
                    Hero hero = target.GetComponent<Hero>();
                    if (hero != null)
                    {
                        hero.ApplyStatusEffect(StatusEffectType.Slow, 0, slowAuraDuration, slowAuraAmount);
                        Debug.Log($"🧊 Slow Aura affected {hero.CharacterName}!");
                    }
                }

                nextTickTime += slowAuraTickInterval;
            }

            yield return null;
        }

        Debug.Log($"🧊 Slow Aura ended");
    }
    private IEnumerator ShowHalfCircleTelegraph()
    {
        Vector3 directionToPlayer = targetTransform != null ?
            (targetTransform.position - transform.position).normalized : transform.forward;
        directionToPlayer.y = 0;

        transform.rotation = Quaternion.LookRotation(directionToPlayer);

        Vector3 telegraphPosition = new Vector3(transform.position.x, telegraphYOffset, transform.position.z);
        Quaternion telegraphRotation = transform.rotation;

        if (halfCircleTelegraphPrefab != null)
        {
            currentTelegraph = Instantiate(halfCircleTelegraphPrefab, telegraphPosition, telegraphRotation);
        }
        else
        {
            currentTelegraph = CreateProceduralHalfCircleTelegraph(telegraphPosition, transform.forward);
        }

        RPC_ShowTelegraph(telegraphPosition);

        float elapsed = 0f;
        Vector3 startScale = new Vector3(0.1f, 0.05f, 0.1f);
        Vector3 targetScale = new Vector3(halfCircleRadius * 0.5f, 0.05f, halfCircleRadius * 0.5f);

        while (elapsed < halfCircleTelegraphDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / halfCircleTelegraphDuration;
            float smoothProgress = 1f - Mathf.Pow(1f - progress, 3f);

            if (currentTelegraph != null)
            {
                currentTelegraph.transform.position = telegraphPosition;
                currentTelegraph.transform.rotation = telegraphRotation;
                currentTelegraph.transform.localScale = Vector3.Lerp(startScale, targetScale, smoothProgress);

                float pulse = Mathf.Sin(elapsed * 8f) * 0.1f + 0.9f;
                Renderer renderer = currentTelegraph.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color color = iceTelegraphColor;
                    color.a = iceTelegraphColor.a * pulse;
                    renderer.material.color = color;
                }
            }

            yield return null;
        }

        if (currentTelegraph != null)
        {
            Destroy(currentTelegraph);
            currentTelegraph = null;
        }
    }

    private void ExecuteHalfCircleAttack()
    {
        Vector3 forward = transform.forward;
        Collider[] targets = Physics.OverlapSphere(transform.position, halfCircleRadius, LayerMask.GetMask("Player"));

        foreach (Collider target in targets)
        {
            Hero hero = target.GetComponent<Hero>();
            if (hero != null && IsInHalfCircle(hero.transform.position, transform.position, forward) && !hitHeroes.Contains(hero))
            {
                hitHeroes.Add(hero);

                hero.TakeDamageFromAttacker(0, MagicDamage, this, DamageType.Magic);
                Debug.Log($"🧊 Half-Circle hit {hero.CharacterName}!");
            }
        }

        // ✅ แก้ไข: ให้ Effect หายไป
        if (halfCircleEffect != null)
        {
            ParticleSystem fx = Instantiate(halfCircleEffect, transform.position, Quaternion.identity);
            Destroy(fx.gameObject, 2f);
        }

        RPC_ShowHalfCircleEffect(transform.position);
    }
    private bool IsInHalfCircle(Vector3 targetPos, Vector3 center, Vector3 forward)
    {
        Vector3 directionToTarget = (targetPos - center).normalized;
        directionToTarget.y = 0;
        forward.y = 0;

        float angle = Vector3.Angle(forward, directionToTarget);
        return angle <= halfCircleAngle / 2f;
    }

    // ========== SKILL 2: Jump Slam ==========
    private IEnumerator Skill2_JumpSlam()
    {
        Debug.Log($"🧊 Skill 2: Jump Slam");

        // ✅ Lock player position ตอนเริ่ม telegraph
        Vector3 initialTargetPosition = targetTransform != null ? targetTransform.position : transform.position;

        // Telegraph
        yield return StartCoroutine(ShowSlamTelegraph(initialTargetPosition));

        // ✅ อัพเดทตำแหน่งผู้เล่นก่อนกระโดด (หลัง telegraph)
        Vector3 finalTargetPosition = targetTransform != null ? targetTransform.position : initialTargetPosition;

        Debug.Log($"🧊 Jump Slam: Initial target={initialTargetPosition}, Final target={finalTargetPosition}");

        // Jump up
        yield return StartCoroutine(JumpUp());

        // Play animation
        RPC_PlayAnimation(ANIM_SKILL1);

        // ✅ Slam down ตรงตำแหน่งผู้เล่น
        yield return StartCoroutine(SlamDown(finalTargetPosition));

        // Execute damage
        ExecuteSlamDamage(finalTargetPosition, false);

        yield return new WaitForSeconds(0.8f); // ✅ ลดจาก 1f
    }

    private IEnumerator ShowSlamTelegraph(Vector3 position)
    {
        Vector3 telegraphPosition = new Vector3(position.x, telegraphYOffset, position.z);

        if (slamTelegraphPrefab != null)
        {
            currentTelegraph = Instantiate(slamTelegraphPrefab, telegraphPosition, Quaternion.identity);
        }
        else
        {
            currentTelegraph = CreateProceduralCircleTelegraph(telegraphPosition, slamRadius);
        }

        RPC_ShowTelegraph(telegraphPosition);

        float elapsed = 0f;
        Vector3 startScale = new Vector3(0.1f, 0.05f, 0.1f);
        Vector3 targetScale = new Vector3(slamRadius * 2f, 0.05f, slamRadius * 2f);

        while (elapsed < slamTelegraphDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / slamTelegraphDuration;

            if (currentTelegraph != null)
            {
                currentTelegraph.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);

                // ✅ เพิ่ม Pulse ให้ชัดเจนขึ้น
                float pulse = Mathf.Sin(elapsed * 10f) * 0.2f + 0.8f; // ✅ เปลี่ยนความเร็ว pulse
                Renderer renderer = currentTelegraph.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color color = iceTelegraphColor;
                    color.a = iceTelegraphColor.a * pulse;
                    renderer.material.color = color;
                }
            }

            yield return null;
        }

        if (currentTelegraph != null)
        {
            Destroy(currentTelegraph);
            currentTelegraph = null;
        }
    }

    private IEnumerator JumpUp()
    {
        Debug.Log($"🧊 Jumping up!");
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.up * jumpHeight;

        float elapsed = 0f;
        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / jumpDuration;

            // ✅ ใช้ Ease Out Quad เพื่อให้กระโดดดูเร็วขึ้น
            float smoothProgress = 1f - Mathf.Pow(1f - progress, 2f);

            transform.position = Vector3.Lerp(startPos, targetPos, smoothProgress);
            yield return null;
        }
    }
    private IEnumerator SlamDown(Vector3 targetPosition)
    {
        Debug.Log($"🧊 Slamming down to {targetPosition}!");
        Vector3 startPos = transform.position;

        // ✅ ลงตรงตำแหน่งผู้เล่น (ใช้ Y ของ originalPosition)
        Vector3 endPos = new Vector3(targetPosition.x, originalPosition.y, targetPosition.z);

        float elapsed = 0f;
        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / jumpDuration;

            // ✅ ใช้ Ease In Quad เพื่อให้ลงเร็วขึ้น
            float smoothProgress = Mathf.Pow(progress, 2f);

            transform.position = Vector3.Lerp(startPos, endPos, smoothProgress);

            // ✅ Sync network
            if (HasStateAuthority)
            {
                NetworkedPosition = transform.position;
            }

            yield return null;
        }

        // ✅ บังคับให้อยู่ตำแหน่งสุดท้าย
        transform.position = endPos;

        if (HasStateAuthority)
        {
            NetworkedPosition = endPos;
        }

        Debug.Log($"🧊 Slam complete at {endPos}");
    }

    private void ExecuteSlamDamage(Vector3 center, bool isEnhanced, int spikeCount = 0)
    {
        // ✅ แก้ไข: ให้ Effect หายไป
        if (jumpSlamEffect != null)
        {
            ParticleSystem fx = Instantiate(jumpSlamEffect, center, Quaternion.identity);
            Destroy(fx.gameObject, 2f); // ✅ เพิ่มบรรทัดนี้
        }

        RPC_ShowSlamEffect(center);

        // Deal damage
        Collider[] targets = Physics.OverlapSphere(center, slamRadius, LayerMask.GetMask("Player"));

        foreach (Collider target in targets)
        {
            Hero hero = target.GetComponent<Hero>();
            if (hero != null)
            {
                hero.TakeDamageFromAttacker(0, MagicDamage, this, DamageType.Magic);
                hero.ApplyStatusEffect(StatusEffectType.Freeze, 0, freezeDuration);
                Debug.Log($"🧊 Slam hit {hero.CharacterName}!");
            }
        }

        // Enhanced: Fire random spikes
        if (isEnhanced && spikeCount > 0)
        {
            StartCoroutine(FireRandomSpikes(center, spikeCount));
        }
    }
    // ========== SKILL 3: Dash With Trail ==========
    private IEnumerator Skill3_DashWithTrail()
    {
        Debug.Log($"🧊 Skill 3: Dash With Trail");

        hitHeroes.Clear();

        if (targetTransform == null)
        {
            FindNearestPlayer();
        }

        if (targetTransform == null)
        {
            Debug.LogWarning("🧊 No target for dash!");
            yield break;
        }

        Vector3 dashStartPos = transform.position;
        Vector3 directionToPlayer = (targetTransform.position - dashStartPos).normalized;
        directionToPlayer.y = 0;

        // Telegraph
        yield return StartCoroutine(ShowDashTelegraph(dashStartPos, directionToPlayer));

        // Play animation
        RPC_PlayAnimation(ANIM_SKILL1);

        yield return new WaitForSeconds(0.2f);

        // Execute dash
        Vector3 dashEndPosition = Vector3.zero; // ✅ เก็บตำแหน่งที่ Dash จบ
        yield return StartCoroutine(ExecuteDashWithTrail(dashStartPos, directionToPlayer, (endPos) => dashEndPosition = endPos));

        // ✅ สร้าง Slow AoE ตรงจุดที่ Dash จบ
        yield return StartCoroutine(CreateDashEndSlowAoE(dashEndPosition));

        yield return new WaitForSeconds(0.5f);
    }


    private IEnumerator ShowDashTelegraph(Vector3 startPos, Vector3 direction)
    {
        Vector3 telegraphPos = new Vector3(startPos.x, telegraphYOffset, startPos.z);
        Quaternion rotation = Quaternion.LookRotation(direction);

        if (dashTelegraphPrefab != null)
        {
            currentTelegraph = Instantiate(dashTelegraphPrefab, telegraphPos, rotation);
        }
        else
        {
            Debug.LogWarning("?? Dash Telegraph Prefab is missing!");
            yield break;
        }

        RPC_ShowTelegraph(telegraphPos);

        float elapsed = 0f;
        Vector3 startScale = new Vector3(dashWidth, 0.05f, 0.5f);
        Vector3 targetScale = new Vector3(dashWidth, 0.05f, dashDistance);

        while (elapsed < dashTelegraphDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / dashTelegraphDuration;

            if (currentTelegraph != null)
            {
                float currentLength = dashDistance * progress;
                Vector3 offset = direction * (currentLength * 0.5f);

                currentTelegraph.transform.position = new Vector3(
                    startPos.x + offset.x,
                    telegraphYOffset,
                    startPos.z + offset.z
                );

                currentTelegraph.transform.rotation = rotation;
                currentTelegraph.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);

                float pulse = Mathf.Sin(elapsed * 10f) * 0.15f + 0.85f;
                Renderer renderer = currentTelegraph.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color color = iceTelegraphColor;
                    color.a = iceTelegraphColor.a * pulse;
                    renderer.material.color = color;
                }
            }

            yield return null;
        }

        if (currentTelegraph != null)
        {
            Destroy(currentTelegraph);
            currentTelegraph = null;
        }
    }

    private IEnumerator ExecuteDashWithTrail(Vector3 startPos, Vector3 direction, System.Action<Vector3> onDashComplete)
    {
        Debug.Log($"🧊 [Dash Start] From: {startPos}, Direction: {direction}");

        // Check wall
        float actualDashDistance = dashDistance;
        RaycastHit wallHit;

        if (Physics.Raycast(
            startPos + Vector3.up * 0.5f,
            direction,
            out wallHit,
            dashDistance,
            LayerMask.GetMask("Wall", "Obstacle", "Default")))
        {
            actualDashDistance = Mathf.Max(0.5f, wallHit.distance - 0.5f);
            Debug.Log($"⚠️ Wall detected at {wallHit.distance}m, reducing dash to {actualDashDistance}m");
        }

        Vector3 endPos = startPos + direction * actualDashDistance;

        Debug.Log($"🧊 [Dash] Target position: {endPos} (Distance: {actualDashDistance}m)");

        // Play effect
        if (dashEffect != null)
        {
            dashEffect.Play();
        }

        float elapsed = 0f;
        Vector3 lastTrailPos = startPos;

        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / dashDuration;
            float smoothProgress = Mathf.Sin(progress * Mathf.PI * 0.5f);

            Vector3 targetPos = Vector3.Lerp(startPos, endPos, smoothProgress);

            // Move
            if (rb != null)
            {
                rb.MovePosition(targetPos);
            }
            else
            {
                transform.position = targetPos;
            }

            if (HasStateAuthority)
            {
                NetworkedPosition = targetPos;
            }

            // Create ice trail every 0.5m
            if (Vector3.Distance(lastTrailPos, targetPos) >= 0.5f)
            {
                CreateIceTrail(targetPos);
                lastTrailPos = targetPos;
            }

            // Check collision
            CheckDashCollision(targetPos, direction);

            yield return null;
        }

        // Final position
        if (rb != null)
        {
            rb.MovePosition(endPos);
            rb.linearVelocity = Vector3.zero;
        }
        else
        {
            transform.position = endPos;
        }

        if (HasStateAuthority)
        {
            NetworkedPosition = endPos;
        }

        Debug.Log($"🧊 [Dash Complete] Final position: {transform.position}");

        if (dashEffect != null)
        {
            dashEffect.Stop();
        }

        // ✅ ส่งตำแหน่งจบกลับไป
        onDashComplete?.Invoke(endPos);

        yield return new WaitForSeconds(0.1f);
    }

    private void CreateIceTrail(Vector3 position)
    {
        if (iceTrailPrefab != null)
        {
            Vector3 trailPos = new Vector3(position.x, telegraphYOffset, position.z);
            GameObject trail = Instantiate(iceTrailPrefab, trailPos, Quaternion.identity);

            // Setup trail
            IceTrail iceTrail = trail.GetComponent<IceTrail>();
            if (iceTrail != null)
            {
                iceTrail.Initialize(slowAmount, slowDuration, iceTrailLifetime);
            }

            activeIceTrails.Add(trail);
            Destroy(trail, iceTrailLifetime);

            Debug.Log($"?? Ice Trail created at {trailPos}");
        }
    }

    private void CheckDashCollision(Vector3 currentPos, Vector3 direction)
    {
        Vector3 boxSize = new Vector3(dashWidth, 2f, 1f);
        Collider[] hits = Physics.OverlapBox(
            currentPos,
            boxSize * 0.5f,
            Quaternion.LookRotation(direction),
            LayerMask.GetMask("Player")
        );

        foreach (Collider hit in hits)
        {
            Hero hero = hit.GetComponent<Hero>();
            if (hero != null && !hitHeroes.Contains(hero))
            {
                hitHeroes.Add(hero);

                hero.TakeDamageFromAttacker(AttackDamage, 0, this, DamageType.Normal);

                Vector3 knockbackDirection = (hero.transform.position - transform.position).normalized;
                hero.ApplyKnockback(knockbackDirection, 40f, 0.8f);

                Debug.Log($"?? Dash hit {hero.CharacterName}!");
            }
        }
    }

    // ========== SKILL 4: Ice Projectile ==========
    private IEnumerator Skill4_IceProjectile()
    {
        Debug.Log($"?? Skill 4: Ice Projectile");

        if (targetTransform == null)
        {
            FindNearestPlayer();
        }

        if (targetTransform == null)
        {
            Debug.LogWarning("?? No target for projectile!");
            yield break;
        }

        // Lock target position
        Vector3 lockedTargetPos = targetTransform.position;

        // Telegraph
        yield return StartCoroutine(ShowProjectileTelegraph(lockedTargetPos));

        // Play animation
        RPC_PlayAnimation(ANIM_ATTACK);

        yield return new WaitForSeconds(0.3f);

        // Fire projectile
        yield return StartCoroutine(FireIceProjectile(lockedTargetPos));

        yield return new WaitForSeconds(1f);
    }

    private IEnumerator ShowProjectileTelegraph(Vector3 targetPosition)
    {
        Vector3 telegraphPosition = new Vector3(targetPosition.x, telegraphYOffset, targetPosition.z);

        if (projectileTelegraphPrefab != null)
        {
            currentTelegraph = Instantiate(projectileTelegraphPrefab, telegraphPosition, Quaternion.identity);
        }
        else
        {
            currentTelegraph = CreateProceduralCircleTelegraph(telegraphPosition, projectileRadius);
        }

        RPC_ShowTelegraph(telegraphPosition);

        float elapsed = 0f;
        Vector3 startScale = new Vector3(0.1f, 0.05f, 0.1f);
        Vector3 targetScale = new Vector3(projectileRadius * 2f, 0.05f, projectileRadius * 2f);

        while (elapsed < projectileTelegraphDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / projectileTelegraphDuration;

            if (currentTelegraph != null)
            {
                currentTelegraph.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);

                float pulse = Mathf.Sin(elapsed * 8f) * 0.15f + 0.85f;
                Renderer renderer = currentTelegraph.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color color = iceTelegraphColor;
                    color.a = iceTelegraphColor.a * pulse;
                    renderer.material.color = color;
                }
            }

            yield return null;
        }

        if (currentTelegraph != null)
        {
            Destroy(currentTelegraph);
            currentTelegraph = null;
        }
    }

    private IEnumerator FireIceProjectile(Vector3 targetPosition)
    {
        Vector3 startPos = transform.position + Vector3.up * 1f;
        Vector3 endPos = new Vector3(targetPosition.x, targetPosition.y, targetPosition.z);

        GameObject projectile = null;
        if (iceProjectilePrefab != null)
        {
            projectile = Instantiate(iceProjectilePrefab, startPos, Quaternion.identity);
        }

        float elapsed = 0f;

        while (elapsed < projectileTravelTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / projectileTravelTime;

            if (projectile != null)
            {
                projectile.transform.position = Vector3.Lerp(startPos, endPos, progress);
            }

            yield return null;
        }

        if (projectile != null)
        {
            Destroy(projectile);
        }

        // Execute damage
        ExecuteProjectileDamage(targetPosition);
    }

    private void ExecuteProjectileDamage(Vector3 center)
    {
        // ✅ แก้ไข: ให้ Effect หายไป
        if (projectileEffect != null)
        {
            ParticleSystem fx = Instantiate(projectileEffect, center, Quaternion.identity);
            Destroy(fx.gameObject, 2f); // ✅ เพิ่มบรรทัดนี้
        }

        RPC_ShowProjectileEffect(center);

        Collider[] targets = Physics.OverlapSphere(center, projectileRadius, LayerMask.GetMask("Player"));

        foreach (Collider target in targets)
        {
            Hero hero = target.GetComponent<Hero>();
            if (hero != null)
            {
                hero.TakeDamageFromAttacker(0, MagicDamage, this, DamageType.Magic);
                hero.ApplyStatusEffect(StatusEffectType.Freeze, 0, freezeDuration);
                Debug.Log($"🧊 Ice Projectile hit {hero.CharacterName}!");
            }
        }
    }

    // ========== SKILL 5: Ice Mirror Combo ==========
    private IEnumerator Skill5_IceMirrorCombo()
    {
        Debug.Log($"?? Skill 5: Ice Mirror Combo!");

        RPC_PlayAnimation(ANIM_SKILL2);

        // Phase A: Spawn 6 Clones + Boss
        yield return StartCoroutine(SpawnClonesAroundPlayer());

        yield return new WaitForSeconds(1f);

        // Phase B: Sequential Clone Dash
        yield return StartCoroutine(ExecuteSequentialCloneDash());

        yield return new WaitForSeconds(0.5f);

        // Phase C: Boss Jump Slam + 8 Direction Ice Spikes
        yield return StartCoroutine(ExecuteFinalCombo());

        // Cleanup
        ClearAllClones();

        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator SpawnClonesAroundPlayer()
    {
        if (targetTransform == null)
        {
            FindNearestPlayer();
        }

        Vector3 playerPos = targetTransform != null ? targetTransform.position : transform.position;

        List<Vector3> clonePositions = new List<Vector3>();

        // Calculate 6 positions around player
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f * Mathf.Deg2Rad;
            float x = playerPos.x + Mathf.Cos(angle) * cloneSpawnRadius;
            float z = playerPos.z + Mathf.Sin(angle) * cloneSpawnRadius;
            clonePositions.Add(new Vector3(x, playerPos.y, z));
        }

        // Random position for boss
        int bossIndex = Random.Range(0, 7); // 0-6

        for (int i = 0; i < 6; i++)
        {
            if (i == bossIndex)
            {
                // Move boss to this position
                transform.position = clonePositions[i];
                if (HasStateAuthority)
                {
                    NetworkedPosition = clonePositions[i];
                }
                Debug.Log($"🧊 Boss moved to clone position {i}");
            }
            else
            {
                // Spawn clone
                if (iceBossClonePrefab != null)
                {
                    GameObject cloneObj = Instantiate(iceBossClonePrefab, clonePositions[i], Quaternion.identity);
                    IceBossClone clone = cloneObj.GetComponent<IceBossClone>();
                    if (clone != null)
                    {
                        activeClones.Add(clone);
                    }

                    // ✅ แก้ไข: ให้ Effect หายไป
                    if (cloneSpawnEffect != null)
                    {
                        GameObject fx = Instantiate(cloneSpawnEffect, clonePositions[i], Quaternion.identity);
                        Destroy(fx, 2f); // ✅ เพิ่มบรรทัดนี้
                    }

                    Debug.Log($"🧊 Clone {i} spawned at {clonePositions[i]}");
                }
            }

            yield return new WaitForSeconds(cloneSpawnDelay);
        }

        RPC_ShowCloneSpawnEffect(playerPos);
    }

    private IEnumerator ExecuteSequentialCloneDash()
    {
        Debug.Log($"🧊 Starting Sequential Clone Dash");

        // ✅ Shuffle clone order
        List<IceBossClone> allClones = new List<IceBossClone>(activeClones);

        for (int i = 0; i < allClones.Count; i++)
        {
            IceBossClone temp = allClones[i];
            int randomIndex = Random.Range(i, allClones.Count);
            allClones[i] = allClones[randomIndex];
            allClones[randomIndex] = temp;
        }

        // ✅ Clone Dash ทีละตัว - แต่ไม่รอให้ตาย
        foreach (IceBossClone clone in allClones)
        {
            if (clone != null)
            {
                if (targetTransform == null)
                {
                    FindNearestPlayer();
                }

                Vector3 currentPlayerPos = targetTransform != null ? targetTransform.position : transform.position;
                Vector3 direction = (currentPlayerPos - clone.transform.position).normalized;

                Debug.Log($"🧊 Clone dashing FAST to player at {currentPlayerPos}");

                // ✅ เริ่ม Dash แบบ Fire-and-Forget (ไม่รอให้จบ)
                StartCoroutine(clone.PerformDash(
                    direction,
                    cloneDashDistance,
                    cloneDashSpeed,
                    dashWidth,
                    this
                ));
            }

            // ✅ รอสั้นๆ ก่อนให้ตัวถัดไป Dash (ไม่รอให้ Dash เสร็จ)
            yield return new WaitForSeconds(cloneDashDelay);
        }

        // ✅ รอให้ Clone ทั้งหมด Dash เสร็จ (ประมาณ)
        yield return new WaitForSeconds(cloneDashSpeed + 0.2f);

        // ✅ Boss Dash
        if (targetTransform == null)
        {
            FindNearestPlayer();
        }

        Vector3 bossTargetPos = targetTransform != null ? targetTransform.position : transform.position;
        Vector3 bossDirection = (bossTargetPos - transform.position).normalized;
        bossDirection.y = 0;

        Debug.Log($"🧊 Boss dashing to CURRENT player position: {bossTargetPos}");

        Vector3 dashEndPos = Vector3.zero;
        yield return StartCoroutine(ExecuteDashWithTrail(transform.position, bossDirection, (endPos) => dashEndPos = endPos));

        Debug.Log($"🧊 All clones dashed rapidly and died!");
    }
    private IEnumerator CreateDashEndSlowAoE(Vector3 position)
    {
        Debug.Log($"🧊 Creating Dash End Slow AoE at {position}");

        Vector3 aoePosition = new Vector3(position.x, telegraphYOffset, position.z);

        // ✅ สร้าง Visual AoE (ถ้ามี Prefab)
        GameObject slowAoE = null;
        if (dashEndSlowAoEPrefab != null)
        {
            slowAoE = Instantiate(dashEndSlowAoEPrefab, aoePosition, Quaternion.identity);
            slowAoE.transform.localScale = new Vector3(dashEndSlowAoERadius * 2f, 0.05f, dashEndSlowAoERadius * 2f);
        }
        else
        {
            // สร้างแบบ procedural
            slowAoE = CreateProceduralCircleTelegraph(aoePosition, dashEndSlowAoERadius);

            // เปลี่ยนสีเป็นสีน้ำเงินเข้ม
            Renderer renderer = slowAoE.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.3f, 0.5f, 1f, 0.4f);
            }
        }

        // ✅ สร้าง Particle Effect
        GameObject particleFX = null;
        if (dashEndSlowEffect != null)
        {
            particleFX = Instantiate(dashEndSlowEffect.gameObject, position, Quaternion.identity);

            ParticleSystem ps = particleFX.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = new Color(0.3f, 0.6f, 1f, 0.7f);
                main.startLifetime = dashEndSlowAoEDuration;
                main.loop = true;

                ParticleSystem.ShapeModule shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = dashEndSlowAoERadius;
            }
        }

        RPC_ShowDashEndSlowAoE(position);

        // ✅ ทำ Slow ต่อเนื่อง
        float elapsed = 0f;
        float nextTickTime = 0f;
        float tickInterval = 0.5f; // ตรวจสอบทุก 0.5 วินาที

        while (elapsed < dashEndSlowAoEDuration)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= nextTickTime)
            {
                // หาผู้เล่นในรัศมี
                Collider[] targets = Physics.OverlapSphere(position, dashEndSlowAoERadius, LayerMask.GetMask("Player"));

                foreach (Collider target in targets)
                {
                    Hero hero = target.GetComponent<Hero>();
                    if (hero != null)
                    {
                        hero.ApplyStatusEffect(StatusEffectType.Slow, 0, dashEndSlowEffectDuration, dashEndSlowAmount);
                        Debug.Log($"🧊 Dash End Slow AoE affected {hero.CharacterName}!");
                    }
                }

                nextTickTime += tickInterval;
            }

            yield return null;
        }

        // ✅ ทำลาย AoE และ Particle
        if (slowAoE != null)
        {
            Destroy(slowAoE);
        }

        if (particleFX != null)
        {
            Destroy(particleFX);
        }

        Debug.Log($"🧊 Dash End Slow AoE ended");
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowDashEndSlowAoE(Vector3 position)
    {
        // ✅ แสดง Particle Effect ฝั่ง Client
        if (dashEndSlowEffect != null)
        {
            GameObject particleFX = Instantiate(dashEndSlowEffect.gameObject, position, Quaternion.identity);
            Destroy(particleFX, dashEndSlowAoEDuration);
        }

        Debug.Log($"[Client] Dash End Slow AoE at {position}");
    }
    private IEnumerator ExecuteFinalCombo()
    {
        if (targetTransform == null)
        {
            FindNearestPlayer();
        }

        Vector3 targetPosition = targetTransform != null ? targetTransform.position : transform.position;

        // Jump Slam
        yield return StartCoroutine(ShowSlamTelegraph(targetPosition));
        yield return StartCoroutine(JumpUp());
        yield return StartCoroutine(SlamDown(targetPosition));

        // Execute slam
        ExecuteSlamDamage(targetPosition, false);

        yield return new WaitForSeconds(0.5f);

        // Fire 8 direction spikes
        yield return StartCoroutine(Fire8DirectionSpikes(targetPosition));
    }

    private IEnumerator Fire8DirectionSpikes(Vector3 center)
    {
        Debug.Log($"?? Firing 8-direction spikes from {center}");

        List<Vector3> directions = new List<Vector3>
        {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right,
            new Vector3(1, 0, 1).normalized,
            new Vector3(-1, 0, 1).normalized,
            new Vector3(1, 0, -1).normalized,
            new Vector3(-1, 0, -1).normalized
        };

        // Show all telegraphs
        foreach (Vector3 dir in directions)
        {
            ShowSpikeTelegraph(center, dir);
        }

        yield return new WaitForSeconds(1f);

        // Fire all spikes simultaneously
        foreach (Vector3 dir in directions)
        {
            FireIceSpike(center, dir);
        }

        ClearAllTelegraphs();
    }

    private void ShowSpikeTelegraph(Vector3 startPos, Vector3 direction)
    {
        Vector3 telegraphPos = new Vector3(startPos.x, telegraphYOffset, startPos.z);
        Quaternion rotation = Quaternion.LookRotation(direction);

        GameObject telegraph = null;
        if (spikeTelegraphPrefab != null)
        {
            telegraph = Instantiate(spikeTelegraphPrefab, telegraphPos, rotation);
            telegraph.transform.localScale = new Vector3(spikeWidth, 0.05f, spikeDistance);
        }

        if (telegraph != null)
        {
            activeTelegraphs.Add(telegraph);
        }
    }

    private void FireIceSpike(Vector3 startPos, Vector3 direction)
    {
        Vector3 endPos = startPos + direction * spikeDistance;

        // Create spike line
        StartCoroutine(AnimateIceSpike(startPos, endPos, direction));
    }

    private IEnumerator AnimateIceSpike(Vector3 start, Vector3 end, Vector3 direction)
    {
        GameObject spike = null;
        if (iceProjectilePrefab != null)
        {
            spike = Instantiate(iceProjectilePrefab, start, Quaternion.LookRotation(direction));
        }

        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            if (spike != null)
            {
                spike.transform.position = Vector3.Lerp(start, end, progress);

                // Check collision along the way
                CheckSpikeCollision(spike.transform.position);
            }

            yield return null;
        }

        if (spike != null)
        {
            Destroy(spike, 1f);
        }
    }

    private void CheckSpikeCollision(Vector3 position)
    {
        Collider[] targets = Physics.OverlapSphere(position, spikeWidth / 2f, LayerMask.GetMask("Player"));

        foreach (Collider target in targets)
        {
            Hero hero = target.GetComponent<Hero>();
            if (hero != null)
            {
                hero.TakeDamageFromAttacker(0, MagicDamage, this, DamageType.Magic);
                hero.ApplyStatusEffect(StatusEffectType.Freeze, 0, freezeDuration);
                Debug.Log($"?? Ice Spike hit {hero.CharacterName}!");
            }
        }
    }

    // ========== SKILL 6: Enhanced Jump Slam ==========
    // ========== SKILL 6: Enhanced Jump Slam ==========
    private IEnumerator Skill6_EnhancedJumpSlam()
    {
        Debug.Log($"🧊 Skill 6: Enhanced Jump Slam");

        if (targetTransform == null)
        {
            FindNearestPlayer();
        }

        // ✅ Lock player position ตอนเริ่ม telegraph
        Vector3 initialTargetPosition = targetTransform != null ? targetTransform.position : transform.position;

        // Telegraph
        yield return StartCoroutine(ShowSlamTelegraph(initialTargetPosition));

        // ✅ อัพเดทตำแหน่งผู้เล่นก่อนกระโดด
        Vector3 finalTargetPosition = targetTransform != null ? targetTransform.position : initialTargetPosition;

        // Jump up
        yield return StartCoroutine(JumpUp());

        // Play animation
        RPC_PlayAnimation(ANIM_SKILL1);

        // Slam down
        yield return StartCoroutine(SlamDown(finalTargetPosition));

        // Random spike count
        int spikeCount = Random.Range(minRandomSpikes, maxRandomSpikes + 1);

        // Execute damage + spikes
        ExecuteSlamDamage(finalTargetPosition, true, spikeCount);

        yield return new WaitForSeconds(1.5f); // ✅ ลดจาก 2f
    }

    private IEnumerator FireRandomSpikes(Vector3 center, int count)
    {
        Debug.Log($"?? Firing {count} random spikes from {center}");

        List<Vector3> allDirections = new List<Vector3>
        {
            Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
            new Vector3(1, 0, 1).normalized, new Vector3(-1, 0, 1).normalized,
            new Vector3(1, 0, -1).normalized, new Vector3(-1, 0, -1).normalized
        };

        // Shuffle
        for (int i = 0; i < allDirections.Count; i++)
        {
            Vector3 temp = allDirections[i];
            int randomIndex = Random.Range(i, allDirections.Count);
            allDirections[i] = allDirections[randomIndex];
            allDirections[randomIndex] = temp;
        }

        // Take first 'count' directions
        for (int i = 0; i < count; i++)
        {
            ShowSpikeTelegraph(center, allDirections[i]);
        }

        yield return new WaitForSeconds(1f);

        for (int i = 0; i < count; i++)
        {
            FireIceSpike(center, allDirections[i]);
        }

        yield return new WaitForSeconds(0.5f);

        ClearAllTelegraphs();
    }

    // ========== SKILL 7: Clone Dash ==========
    private IEnumerator Skill7_CloneDash()
    {
        Debug.Log($"🧊 Skill 7: Clone Dash");

        int cloneCount = Random.Range(minCloneCount, maxCloneCount + 1);

        // Spawn clones
        List<IceBossClone> clones = new List<IceBossClone>();

        for (int i = 0; i < cloneCount; i++)
        {
            Vector3 randomOffset = Random.insideUnitCircle.normalized * 5f;
            Vector3 spawnPos = transform.position + new Vector3(randomOffset.x, 0f, randomOffset.y);

            if (iceBossClonePrefab != null)
            {
                GameObject cloneObj = Instantiate(iceBossClonePrefab, spawnPos, Quaternion.identity);
                IceBossClone clone = cloneObj.GetComponent<IceBossClone>();
                if (clone != null)
                {
                    clones.Add(clone);
                }

                if (cloneSpawnEffect != null)
                {
                    GameObject fx = Instantiate(cloneSpawnEffect, spawnPos, Quaternion.identity);
                    Destroy(fx, 2f);
                }
            }
        }

        yield return new WaitForSeconds(0.3f);

        // ✅ Clones dash แบบ Fire-and-Forget (ไม่รอกัน)
        foreach (IceBossClone clone in clones)
        {
            if (clone != null && targetTransform != null)
            {
                Vector3 direction = (targetTransform.position - clone.transform.position).normalized;

                // ✅ เริ่ม Dash แต่ไม่รอให้จบ
                StartCoroutine(clone.PerformDash(
                    direction,
                    cloneDashDistance,
                    cloneDashSpeed,
                    dashWidth,
                    this
                ));
            }

            // ✅ รอสั้นๆ ก่อนให้ตัวถัดไป Dash
            yield return new WaitForSeconds(cloneDashDelay);
        }

        // ✅ รอให้ Clone Dash เสร็จทั้งหมด
        yield return new WaitForSeconds(cloneDashSpeed + 0.2f);

        // Boss performs another skill
        SkillType bossSkill = SelectBossSkillDuringClone();
        Debug.Log($"🧊 Boss using skill {bossSkill} while clones finish dashing");

        yield return StartCoroutine(PerformSkill(bossSkill));

        // ✅ ไม่ต้อง Cleanup เพราะ Clone ตายเองแล้ว
        Debug.Log($"🧊 All clones already destroyed themselves");
    }


    // ========== SKILL 8: Clone Projectile ==========
    private IEnumerator Skill8_CloneProjectile()
    {
        Debug.Log($"🧊 Skill 8: Clone Projectile");

        int cloneCount = Random.Range(minCloneCount, maxCloneCount + 1);

        // Spawn clones
        List<IceBossClone> clones = new List<IceBossClone>();

        for (int i = 0; i < cloneCount; i++)
        {
            Vector3 randomOffset = Random.insideUnitCircle.normalized * 5f;
            Vector3 spawnPos = transform.position + new Vector3(randomOffset.x, 0f, randomOffset.y);

            if (iceBossClonePrefab != null)
            {
                GameObject cloneObj = Instantiate(iceBossClonePrefab, spawnPos, Quaternion.identity);
                IceBossClone clone = cloneObj.GetComponent<IceBossClone>();
                if (clone != null)
                {
                    clones.Add(clone);
                }

                // ✅ แก้ไข: ให้ Effect หายไป
                if (cloneSpawnEffect != null)
                {
                    GameObject fx = Instantiate(cloneSpawnEffect, spawnPos, Quaternion.identity);
                    Destroy(fx, 2f); // ✅ เพิ่มบรรทัดนี้
                }
            }
        }

        yield return new WaitForSeconds(0.5f);

        // Clones shoot
        foreach (IceBossClone clone in clones)
        {
            if (clone != null && targetTransform != null)
            {
                StartCoroutine(clone.PerformIceProjectile(
                    targetTransform.position,
                    projectileTravelTime,
                    projectileRadius,
                    iceProjectilePrefab,
                    projectileEffect,
                    this,
                    freezeDuration
                ));
            }
        }

        // Boss performs another skill
        SkillType bossSkill = SelectBossSkillDuringClone();
        Debug.Log($"🧊 Boss using skill {bossSkill} while clones shoot");

        yield return StartCoroutine(PerformSkill(bossSkill));

        // Cleanup clones
        foreach (IceBossClone clone in clones)
        {
            if (clone != null)
            {
                Destroy(clone.gameObject);
            }
        }
    }

    public IEnumerator PerformIceProjectile(
    Vector3 targetPosition,
    float travelTime,
    float radius,
    GameObject projectilePrefab,
    ParticleSystem effect,
    IceBoss boss,
    float freezeDuration)
    {
        Debug.Log($"🧊 Clone shooting ice projectile at {targetPosition}");

        // Play animation
        if (animator != null)
        {
            animator.SetTrigger("attack");
        }

        yield return new WaitForSeconds(0.3f);

        Vector3 startPos = transform.position + Vector3.up * 1f;
        Vector3 endPos = new Vector3(targetPosition.x, targetPosition.y, targetPosition.z);

        GameObject projectile = null;
        if (projectilePrefab != null)
        {
            projectile = Instantiate(projectilePrefab, startPos, Quaternion.identity);
        }

        float elapsed = 0f;

        while (elapsed < travelTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / travelTime;

            if (projectile != null)
            {
                projectile.transform.position = Vector3.Lerp(startPos, endPos, progress);
            }

            yield return null;
        }

        if (projectile != null)
        {
            Destroy(projectile);
        }

        // ✅ แก้ไข: ให้ Effect หายไป
        if (effect != null)
        {
            ParticleSystem fx = Instantiate(effect, targetPosition, Quaternion.identity);
            Destroy(fx.gameObject, 2f); // ✅ เพิ่มบรรทัดนี้
        }

        Collider[] targets = Physics.OverlapSphere(targetPosition, radius, LayerMask.GetMask("Player"));

        foreach (Collider target in targets)
        {
            Hero hero = target.GetComponent<Hero>();
            if (hero != null)
            {
                hero.TakeDamageFromAttacker(0, boss.MagicDamage, boss, DamageType.Magic);
                hero.ApplyStatusEffect(StatusEffectType.Freeze, 0, freezeDuration);
                Debug.Log($"🧊 Clone projectile hit {hero.CharacterName}!");
            }
        }

        Debug.Log($"🧊 Clone projectile complete");

        // Clone disappears after shooting
        Destroy(gameObject, 0.5f);
    }
    // ========== Helper Methods ==========
    private GameObject CreateProceduralHalfCircleTelegraph(Vector3 position, Vector3 direction)
    {
        GameObject telegraph = new GameObject("IceBoss_HalfCircleTelegraph");
        telegraph.transform.position = position;
        telegraph.transform.rotation = Quaternion.LookRotation(direction);
        telegraph.transform.localScale = new Vector3(0.1f, 0.05f, 0.1f);

        MeshFilter meshFilter = telegraph.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = telegraph.AddComponent<MeshRenderer>();

        meshFilter.mesh = CreateHalfCircleMesh(halfCircleRadius, 32);

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = iceTelegraphColor;
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        meshRenderer.material = mat;

        return telegraph;
    }

    private Mesh CreateHalfCircleMesh(float radius, int segments)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        vertices.Add(Vector3.zero);

        for (int i = 0; i <= segments / 2; i++)
        {
            float angle = Mathf.Deg2Rad * (i * 360f / segments - 90f);
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            vertices.Add(new Vector3(x, 0, z));
        }

        for (int i = 0; i < segments / 2; i++)
        {
            triangles.Add(0);
            triangles.Add(i + 1);
            triangles.Add(i + 2);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

    private GameObject CreateProceduralCircleTelegraph(Vector3 position, float radius)
    {
        GameObject telegraph = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        telegraph.name = "IceBoss_CircleTelegraph";
        telegraph.transform.position = position;
        telegraph.transform.localScale = new Vector3(0.1f, 0.05f, 0.1f);

        Destroy(telegraph.GetComponent<Collider>());

        Renderer renderer = telegraph.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = iceTelegraphColor;
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            renderer.material = mat;
        }

        return telegraph;
    }

    private void ClearAllTelegraphs()
    {
        foreach (GameObject telegraph in activeTelegraphs)
        {
            if (telegraph != null)
            {
                Destroy(telegraph);
            }
        }
        activeTelegraphs.Clear();
    }

    private void ClearAllClones()
    {
        foreach (IceBossClone clone in activeClones)
        {
            if (clone != null)
            {
                Destroy(clone.gameObject);
            }
        }
        activeClones.Clear();
    }

    // ========== Movement Control ==========
    private void StopMovement()
    {
        MoveSpeed = 0f;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
        targetTransform = null;
    }

    private void ResumeMovement()
    {
        if (characterStats != null)
        {
            MoveSpeed = characterStats.moveSpeed;
        }
        FindNearestPlayer();
    }

    // ========== RPCs ==========
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayAnimation(string animationName)
    {
        if (animator == null) return;

        bool hasParameter = false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == animationName && param.type == AnimatorControllerParameterType.Trigger)
            {
                hasParameter = true;
                break;
            }
        }

        if (!hasParameter)
        {
            Debug.LogError($"? Animator has no Trigger parameter: {animationName}");
            return;
        }

        Debug.Log($"? [RPC] Playing animation: {animationName} on {CharacterName}");
        animator.SetTrigger(animationName);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowTelegraph(Vector3 position)
    {
        Debug.Log($"[Client] Showing ice telegraph at {position}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowHalfCircleEffect(Vector3 position)
    {
        if (halfCircleEffect != null)
        {
            ParticleSystem fx = Instantiate(halfCircleEffect, position, Quaternion.identity);
            Destroy(fx.gameObject, 2f);
        }
        // ✅ เพิ่มโค้ดที่หายไปกลับมา
        if (projectileEffect != null) // ✅ หรือใช้ effect ที่เหมาะสม
        {
            ParticleSystem fx = Instantiate(projectileEffect, position, Quaternion.identity);
            Destroy(fx.gameObject, 2f);
        }
        Debug.Log($"[Client] Half-circle effect at {position}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowSlamEffect(Vector3 position)
    {
        // ✅ แก้ไข: ให้ Effect หายไปฝั่ง Client ด้วย
        if (jumpSlamEffect != null)
        {
            ParticleSystem fx = Instantiate(jumpSlamEffect, position, Quaternion.identity);
            Destroy(fx.gameObject, 2f);
        }
        Debug.Log($"[Client] Slam effect at {position}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowProjectileEffect(Vector3 position)
    {
        // ✅ แก้ไข: ให้ Effect หายไปฝั่ง Client ด้วย
        if (projectileEffect != null)
        {
            ParticleSystem fx = Instantiate(projectileEffect, position, Quaternion.identity);
            Destroy(fx.gameObject, 2f);
        }
        Debug.Log($"[Client] Projectile effect at {position}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowCloneSpawnEffect(Vector3 position)
    {
        // ✅ แก้ไข: ให้ Effect หายไปฝั่ง Client ด้วย
        if (cloneSpawnEffect != null)
        {
            GameObject fx = Instantiate(cloneSpawnEffect, position, Quaternion.identity);
            Destroy(fx, 2f);
        }
        Debug.Log($"[Client] Clone spawn effect at {position}");
    }

    protected override void RPC_OnDeath()
    {
        ClearAllTelegraphs();
        ClearAllClones();

        foreach (GameObject trail in activeIceTrails)
        {
            if (trail != null)
            {
                Destroy(trail);
            }
        }
        activeIceTrails.Clear();

        if (currentTelegraph != null)
        {
            Destroy(currentTelegraph);
        }

        base.RPC_OnDeath();
    }

    // ========== Debug Gizmos ==========
   

}