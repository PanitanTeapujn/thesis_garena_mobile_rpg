using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class BossSlime : NetworkEnemy
{
    [Header("🐲 Boss Slime Settings")]
    [SerializeField] private float phase2HPThreshold = 0.5f; // 50% HP
    private bool hasEnteredPhase2 = false;

    [Header("🎯 Skill Cooldowns")]
    [SerializeField] private float basicAttackCooldown = 2f;
    [SerializeField] private float poisonAoECooldown = 4f;
    [SerializeField] private float singleDashCooldown = 5f;
    [SerializeField] private float doubleDashCooldown = 7f;
    [SerializeField] private float ultimateCooldown = 15f;
    [SerializeField] private float verticalSpikeCooldown = 6f;
    [SerializeField] private float horizontalSpikeCooldown = 6f;
    [Header("💥 Skill 2: Poison AoE")]
    [SerializeField] private float poisonAoERadius = 4f;
    [SerializeField] private float poisonAoEAngle = 180f; // ครึ่งวงกลม
    [SerializeField] private float poisonAoETelegraphDuration = 1.2f;
    [SerializeField] private int poisonDamagePerTick = 5;
    [SerializeField] private float poisonDuration = 6f;
    [SerializeField] private GameObject poisonAoETelegraphPrefab; // 🆕 Prefab สำหรับ Poison AoE Telegraph

    [Header("🚀 Skill 3 & 4: Dash Attack")]
    [SerializeField] private float dashDistance = 10f;
    [SerializeField] private float dashDuration = 0.4f;
    [SerializeField] private float dashWidth = 2.5f;
    [SerializeField] private float dashTelegraphDuration = 1.2f;
    [SerializeField] private GameObject dashTelegraphPrefab; // 🆕 Prefab สำหรับ Dash Telegraph

    // ========== เพิ่ม Alternative: ใช้ Prefab แยกสำหรับแนวนอน ==========
    [Header("🌟 Skill 5: Ultimate Spike Pattern")]
    [SerializeField] private float ultimateFlyHeight = 5f;
    [SerializeField] private float ultimateFlyDuration = 1.5f;
    [SerializeField] private float spikeWidth = 2f;
    [SerializeField] private float spikeLength = 15f;
    [SerializeField] private float spikeTelegraphDuration = 1f;
    [SerializeField] private float spikeDelayBetweenWaves = 1.5f;
    [SerializeField] private GameObject spikeTelegraphPrefab; // แนวตั้ง
    [SerializeField] private GameObject spikeTelegraphHorizontalPrefab; // 🆕 แนวนอน (แยกต่างหาก)
    [SerializeField] private GameObject spikePrefab;
    [SerializeField] private int spikeDamage = 15;
    [SerializeField] private float spikeVisibleDuration = 2f;

    [Header("🎯 Skill 6 & 7: Phase 2 Spikes")]
    [SerializeField] private float phase2SpikeCooldown = 5f;

    [Header("🎨 Visual Effects")]
    [SerializeField] private ParticleSystem poisonEffect;
    [SerializeField] private ParticleSystem dashEffect;
    [SerializeField] private ParticleSystem spikeEffect;
    [SerializeField] private float telegraphYOffset = -1.16f;


    [Header("🔧 Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [Header("🎬 Animation")]
    private Animator animator;
    private const string ANIM_ATTACK = "attack";
    private const string ANIM_SKILL1 = "skill1";
    private const string ANIM_SKILL2 = "skill2";
    // Private variables
    private bool isPerformingSkill = false;
    private GameObject currentTelegraph;
    private List<GameObject> activeTelegraphs = new List<GameObject>();
    private List<GameObject> activeSpikes = new List<GameObject>(); // 🆕 เก็บ spike objects
    private Vector3 originalPosition;
    private List<Hero> hitHeroes = new List<Hero>();
    [Header("👻 Minion Summoning System")]
    [Tooltip("ลิสต์ลูกน้องที่สามารถเสกได้")]
    [SerializeField] private NetworkEnemy[] minionPrefabs; // ✅ เปลี่ยนเป็น Array

    [Tooltip("จำนวนลูกน้องสูงสุดที่มีได้พร้อมกัน")]
    [Range(1, 10)]
    [SerializeField] private int maxMinions = 3;

    [Tooltip("ระยะห่างจาก Boss ที่ลูกน้องจะถูกเสก")]
    [Range(3f, 10f)]
    [SerializeField] private float summonRadius = 5f;

    [Tooltip("เสกครั้งแรกตอน Phase 2 หรือไม่")]
    [SerializeField] private bool summonOnPhase2 = true;

    [Tooltip("จำนวนลูกน้องที่จะเสกตอน Phase 2")]
    [Range(1, 5)]
    [SerializeField] private int phase2SummonCount = 2;

    [Header("🎨 Summoning Effects")]
    [SerializeField] private GameObject summonEffectPrefab;
    [SerializeField] private ParticleSystem summonParticles;

    // Private variables
    private EnemySpawner enemySpawner;
    private List<NetworkEnemy> summonedMinions = new List<NetworkEnemy>(); // ✅ เปลี่ยนชื่อ
    private bool hasSpawnedPhase2Minions = false;
    private enum BossPhase
    {
        Phase1,
        Phase2
    }

    private enum SkillType
    {
        BasicAttack = 1,
        PoisonAoE = 2,
        SingleDash = 3,
        DoubleDash = 4,
        Ultimate = 5,
        VerticalSpike = 6,
        HorizontalSpike = 7,
        UltimateMix = 8
    }

    [Networked] private BossPhase CurrentPhase { get; set; }
    [Networked] private float NextSkillTime { get; set; }

    protected override void Start()
    {
        base.Start();
        SetupBossSlimeStats();
        CurrentPhase = BossPhase.Phase1;
        originalPosition = transform.position;
        animator = GetComponent<Animator>();
        enemySpawner = FindObjectOfType<EnemySpawner>();

        if (animator == null)
        {
            Debug.LogWarning($"⚠️ Animator not found on {CharacterName}!");
        }
        Debug.Log($"🐲 {CharacterName} Boss spawned!");
    }

    private void SetupBossSlimeStats()
    {
        CharacterName = "Boss Slime";
        AttackType = AttackType.Magic;

        // Boss stats - ปรับตามต้องการ
        MaxHp = 5000;
        CurrentHp = MaxHp;
        MagicDamage = 30;
        AttackDamage = 25;
        MoveSpeed = 2f;
        AttackRange = 3f;

        // ปรับสี
       
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (HasStateAuthority && !IsDead)
        {
            CheckPhaseTransition();
            CleanupDeadMinions();

            ProcessBossAI();
        }
    }

    // ========== Phase Management ==========
    private void CheckPhaseTransition()
    {
        float hpPercentage = (float)CurrentHp / MaxHp;

        if (!hasEnteredPhase2 && hpPercentage <= phase2HPThreshold)
        {
            hasEnteredPhase2 = true;
            CurrentPhase = BossPhase.Phase2;
            Debug.Log($"🐲 {CharacterName}: PHASE 2 ACTIVATED!");

            // ✅ เสกลูกน้องทันที
            if (summonOnPhase2 && !hasSpawnedPhase2Minions)
            {
                hasSpawnedPhase2Minions = true;
                StartCoroutine(SummonMinions(phase2SummonCount));
            }

            // บังคับใช้ Ultimate ทันที
            if (!isPerformingSkill)
            {
                StartCoroutine(PerformSkill(SkillType.Ultimate));
            }
        }
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

        // ✅ เพิ่ม: เช็คว่าต้องเสกลูกน้องเพิ่มหรือไม่ (Phase 2 เท่านั้น)
        if (CurrentPhase == BossPhase.Phase2)
        {
            int currentMinions = GetActiveMinionCount();
            if (currentMinions < maxMinions)
            {
                // สุ่มโอกาส 20% ที่จะเสกลูกน้องแทนการใช้สกิล
                if (Random.Range(0f, 100f) < 20f && Runner.SimulationTime >= NextSkillTime)
                {
                    Debug.Log($"[BossSlime] Summoning more minions ({currentMinions}/{maxMinions})");
                    StartCoroutine(SummonMinions(1)); // เสกทีละ 1 ตัว
                    NextSkillTime = Runner.SimulationTime + 8f; // Cooldown 8 วินาที
                    return;
                }
            }
        }

        // ตรวจสอบว่าพร้อมใช้สกิลหรือยัง
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
            // Phase 1: สุ่มสกิล 1-4
            int[] skills = { 1, 2, 3, 4 };
            int randomIndex = Random.Range(0, skills.Length);
            return (SkillType)skills[randomIndex];
        }
        else
        {
            // Phase 2: สุ่มสกิล 1-8
            int[] skills = { 1, 2, 3, 4, 5, 6, 7, 8 };
            int randomIndex = Random.Range(0, skills.Length);
            return (SkillType)skills[randomIndex];
        }
    }

    // ========== Main Skill Executor ==========
    // ========== เพิ่ม Debug ใน Skill Executor ==========
    private IEnumerator PerformSkill(SkillType skill)
    {
        isPerformingSkill = true;
        StopMovement();
        originalPosition = transform.position;

        Debug.Log($"🐲🐲🐲 {CharacterName}: *** STARTING SKILL {skill} ***"); // ✅ Debug ชัดเจน

        switch (skill)
        {
            case SkillType.BasicAttack:
                Debug.Log(">>> Executing BasicAttack"); // ✅ Debug
                yield return StartCoroutine(Skill1_BasicAttack());
                NextSkillTime = Runner.SimulationTime + basicAttackCooldown;
                break;

            case SkillType.PoisonAoE:
                Debug.Log(">>> Executing PoisonAoE"); // ✅ Debug
                yield return StartCoroutine(Skill2_PoisonAoE());
                NextSkillTime = Runner.SimulationTime + poisonAoECooldown;
                break;

            case SkillType.SingleDash:
                Debug.Log(">>> Executing SingleDash"); // ✅ Debug
                yield return StartCoroutine(Skill3_SingleDash());
                NextSkillTime = Runner.SimulationTime + singleDashCooldown;
                break;

            case SkillType.DoubleDash:
                Debug.Log(">>> Executing DoubleDash"); // ✅ Debug
                yield return StartCoroutine(Skill4_DoubleDash());
                NextSkillTime = Runner.SimulationTime + doubleDashCooldown;
                break;

            case SkillType.Ultimate:
                Debug.Log(">>> Executing Ultimate"); // ✅ Debug
                yield return StartCoroutine(Skill5_Ultimate());
                NextSkillTime = Runner.SimulationTime + ultimateCooldown;
                break;

            case SkillType.VerticalSpike:
                Debug.Log(">>> Executing VerticalSpike"); // ✅ Debug
                yield return StartCoroutine(Skill6_VerticalSpike());
                NextSkillTime = Runner.SimulationTime + verticalSpikeCooldown;
                break;

            case SkillType.HorizontalSpike:
                Debug.Log(">>> Executing HorizontalSpike"); // ✅ Debug
                yield return StartCoroutine(Skill7_HorizontalSpike());
                NextSkillTime = Runner.SimulationTime + horizontalSpikeCooldown;
                break;

            case SkillType.UltimateMix:
                Debug.Log(">>> Executing UltimateMix"); // ✅ Debug
                yield return StartCoroutine(Skill8_UltimateMix());
                NextSkillTime = Runner.SimulationTime + ultimateCooldown;
                break;
        }

        ResumeMovement();
        isPerformingSkill = false;

        Debug.Log($"🐲✅ {CharacterName}: SKILL {skill} COMPLETE! Next skill at: {NextSkillTime}"); // ✅ Debug
    }

    // ========== SKILL 1: Basic Attack ==========
    private IEnumerator Skill1_BasicAttack()
    {
        Debug.Log($"🐲 Skill 1: Basic Attack");

        // Play attack animation
        RPC_PlayAnimation(ANIM_ATTACK);

        yield return new WaitForSeconds(0.5f);

        // Deal damage to nearby player
        if (targetTransform != null)
        {
            float distance = Vector3.Distance(transform.position, targetTransform.position);
            if (distance <= AttackRange)
            {
                Hero hero = targetTransform.GetComponent<Hero>();
                if (hero != null)
                {
                    hero.TakeDamageFromAttacker(AttackDamage, 0, this, DamageType.Normal);
                    Debug.Log($"🐲 Basic Attack hit {hero.CharacterName}!");
                }
            }
        }

        yield return new WaitForSeconds(1f);
    }

    // ========== SKILL 2: Poison AoE ==========
    private IEnumerator Skill2_PoisonAoE()
    {
        Debug.Log($"🐲 Skill 2: Poison AoE");

        // Telegraph
        yield return StartCoroutine(ShowPoisonAoETelegraph());

        // Play attack animation
        RPC_PlayAnimation("attack");

        yield return new WaitForSeconds(0.3f);

        // ✅ Execute poison AoE ต่อเนื่อง
        yield return StartCoroutine(ExecutePoisonAoE());

        yield return new WaitForSeconds(0.5f);
    }
    private IEnumerator ExecutePoisonAoE()
    {
        Vector3 aoeCenter = transform.position;
        aoeCenter.y += -1f; // ✅ ปรับความสูงของจุดเกิด Particle (เช่น ลอยจากพื้น 1 หน่วย)

        float aoeDuration = 3f; // ระยะเวลาที่ particle อยู่
        float tickInterval = 0.5f; // ทำดาเมจทุก 0.5 วินาที
        float nextTickTime = 0f;
        float elapsed = 0f;

        // ✅ สร้าง Poison Particle Effect
        GameObject poisonFX = null;
        if (poisonEffect != null)
        {
            poisonFX = Instantiate(poisonEffect.gameObject, aoeCenter, Quaternion.identity);

            poisonFX.transform.localScale = Vector3.one * (poisonAoERadius * 0.5f);

            ParticleSystem ps = poisonFX.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = new Color(0.7f, 0.2f, 1f, 0.8f); // ✅ สีม่วง (purple glow)
                main.startLifetime = aoeDuration;
                main.loop = true;

                ParticleSystem.ShapeModule shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = poisonAoERadius;
                shape.position = new Vector3(0f, 0.3f, 0f); // ✅ ปรับความสูงภายในตัว particle ด้วย

                ps.Play();
            }

            // ทำลายหลังจาก duration
            Destroy(poisonFX, aoeDuration);
        }

        Debug.Log($"🐲 Poison AoE started! Duration: {aoeDuration}s, Tick: {tickInterval}s");

        // ✅ ลูปทำดาเมจต่อเนื่อง
        while (elapsed < aoeDuration)
        {
            elapsed += Time.deltaTime;

            // ถึงเวลา tick แล้ว
            if (elapsed >= nextTickTime)
            {
                // ตรวจสอบผู้เล่นในพื้นที่
                Collider[] targets = Physics.OverlapSphere(aoeCenter, poisonAoERadius, LayerMask.GetMask("Player"));

                Debug.Log($"🐲 Poison AoE tick! Found {targets.Length} players in area");

                foreach (Collider target in targets)
                {
                    Hero hero = target.GetComponent<Hero>();
                    if (hero != null)
                    {
                        hero.TakeDamageFromAttacker(0, MagicDamage, this, DamageType.Magic);
                        hero.ApplyStatusEffect(StatusEffectType.Poison, poisonDamagePerTick, poisonDuration);

                        Debug.Log($"🐲 Poison AoE damaged {hero.CharacterName}! Damage: {MagicDamage}");
                    }
                }

                nextTickTime += tickInterval;
            }

            yield return null;
        }

        Debug.Log($"🐲 Poison AoE ended!");
        RPC_ShowPoisonEffect(aoeCenter);
    }

    // ========== SKILL 2: Poison AoE ========== (แก้ไขให้เห็น Telegraph)
    private IEnumerator ShowPoisonAoETelegraph()
    {
        Vector3 telegraphPos = new Vector3(transform.position.x, telegraphYOffset, transform.position.z);
        Vector3 directionToPlayer = targetTransform != null ?
            (targetTransform.position - transform.position).normalized : transform.forward;
        directionToPlayer.y = 0;

        if (poisonAoETelegraphPrefab != null)
        {
            currentTelegraph = Instantiate(poisonAoETelegraphPrefab, telegraphPos, Quaternion.LookRotation(directionToPlayer));
            Debug.Log($"🐲 Poison AoE Telegraph created at {telegraphPos}");
        }
        else
        {
            Debug.LogWarning("🐲 Poison AoE Telegraph Prefab is missing!");
            yield break;
        }

        RPC_ShowTelegraph(telegraphPos);

        float elapsed = 0f;
        Vector3 startScale = new Vector3(0.1f, currentTelegraph.transform.localScale.y, 0.1f);
        Vector3 targetScale = new Vector3(poisonAoERadius * 2f, currentTelegraph.transform.localScale.y, poisonAoERadius * 2f);

        // ✅ เพิ่มเวลา telegraph จาก 1.2s → 2.0s
        float telegraphDuration = 2.0f;

        Debug.Log($"🐲 Poison AoE animating - Duration: {telegraphDuration}s");

        while (elapsed < telegraphDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / telegraphDuration;

            if (currentTelegraph != null)
            {
                currentTelegraph.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
                currentTelegraph.transform.position = telegraphPos;
                currentTelegraph.transform.rotation = Quaternion.LookRotation(directionToPlayer);

                // Pulse effect
                float pulse = Mathf.Sin(elapsed * 8f) * 0.15f + 0.85f;
                Renderer renderer = currentTelegraph.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color color = renderer.material.color;
                    Color originalColor = color;
                    color.a = originalColor.a * pulse;
                    renderer.material.color = color;
                }
            }
            else
            {
                Debug.LogWarning("🐲 Poison AoE Telegraph destroyed during animation!");
                yield break;
            }

            yield return null;
        }

        Debug.Log("🐲 Poison AoE Telegraph animation complete");

        if (currentTelegraph != null)
        {
            Destroy(currentTelegraph);
            currentTelegraph = null;
        }
    }
    
    // ========== SKILL 3: Single Dash ==========
    private IEnumerator Skill3_SingleDash()
    {
        Debug.Log($"🐲 Skill 3: Single Dash");
        RPC_PlayAnimation(ANIM_SKILL1);

        yield return StartCoroutine(PerformDashAttack(1));
    }

    // ========== SKILL 4: Double Dash ==========
    private IEnumerator Skill4_DoubleDash()
    {
        Debug.Log($"🐲 Skill 4: Double Dash");
        RPC_PlayAnimation(ANIM_SKILL2);

        yield return StartCoroutine(PerformDashAttack(2));
    }

    private IEnumerator PerformDashAttack(int dashCount)
    {
        for (int i = 0; i < dashCount; i++)
        {
            hitHeroes.Clear();

            // หา target ใหม่ทุกครั้ง
            if (targetTransform == null)
            {
                FindNearestPlayer();
            }

            if (targetTransform == null)
            {
                Debug.LogWarning("🐲 No target for dash!");
                yield break;
            }

            Vector3 dashStartPos = transform.position;
            Vector3 directionToPlayer = (targetTransform.position - dashStartPos).normalized;
            directionToPlayer.y = 0;

            Debug.Log($"🐲 Dash {i + 1}/{dashCount} - Direction: {directionToPlayer}");

            // Telegraph
            yield return StartCoroutine(ShowDashTelegraph(dashStartPos, directionToPlayer));

            // Play skill1 animation
            RPC_PlayAnimation("skill1");

            yield return new WaitForSeconds(0.2f);

            // Execute dash
            yield return StartCoroutine(ExecuteDash(dashStartPos, directionToPlayer));

            // Small delay between dashes
            if (i < dashCount - 1)
            {
                yield return new WaitForSeconds(0.3f);
            }
        }

        yield return new WaitForSeconds(1f);
    }

    // ========== Dash Telegraph ========== (แก้ไขส่วนนี้)
    // ========== Dash Telegraph ========== (แก้ไขใหม่)
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
            Debug.LogWarning("🐲 Dash Telegraph Prefab is missing!");
            yield break;
        }

        RPC_ShowTelegraph(telegraphPos);

        float elapsed = 0f;
        Vector3 startScale = new Vector3(dashWidth, 0.05f, 0.5f);
        // ✅ ใช้ dashDistance เต็มที่ (เหมือน Ice Bird)
        Vector3 targetScale = new Vector3(dashWidth, 0.05f, dashDistance);

        Debug.Log($"🐲 Dash Telegraph - Distance: {dashDistance}m");

        while (elapsed < dashTelegraphDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / dashTelegraphDuration;

            if (currentTelegraph != null)
            {
                // ✅ ขยายไปข้างหน้า เหมือน Ice Bird
                float currentLength = dashDistance * progress;
                Vector3 offset = direction * (currentLength * 0.5f);

                currentTelegraph.transform.position = new Vector3(
                    startPos.x + offset.x,
                    telegraphYOffset,
                    startPos.z + offset.z
                );

                currentTelegraph.transform.rotation = rotation;
                currentTelegraph.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);

                // Pulse
                float pulse = Mathf.Sin(elapsed * 10f) * 0.15f + 0.85f;
                Renderer renderer = currentTelegraph.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color color = renderer.material.color;
                    Color originalColor = color;
                    color.a = originalColor.a * pulse;
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
    private IEnumerator ExecuteDash(Vector3 startPos, Vector3 direction)
    {
        if (dashEffect != null)
        {
            dashEffect.Play();
        }

        float elapsed = 0f;
        Vector3 endPos = startPos + direction * dashDistance;

        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / dashDuration;
            float smoothProgress = Mathf.Sin(progress * Mathf.PI * 0.5f);

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, smoothProgress);
            transform.position = currentPos;

            CheckDashCollision(currentPos, direction);

            yield return null;
        }

        if (dashEffect != null)
        {
            dashEffect.Stop();
        }
    }

    // ========== (ถ้าต้องการ) เพิ่ม Stun หลัง Knockback ==========
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

                // ทำดาเมจ
                hero.TakeDamageFromAttacker(AttackDamage, 0, this, DamageType.Normal);

                // ✅ Knockback
                Vector3 knockbackDirection = (hero.transform.position - transform.position).normalized;
                hero.ApplyKnockback(knockbackDirection, 50f, 1f);

                // ✅ (Optional) เพิ่ม Stun หลัง knockback 1 วินาที
                 hero.ApplyStatusEffect(StatusEffectType.Stun, 0, 1.5f);

                Debug.Log($"🐲 Dash hit {hero.CharacterName}! Knockback applied!");
            }
        }
    }

    // ========== SKILL 5: Ultimate Spike Pattern ==========
    private IEnumerator Skill5_Ultimate()
    {
        Debug.Log($"🐲 Skill 5: ULTIMATE!");
        RPC_PlayAnimation(ANIM_SKILL2);

        // Fly up
        yield return StartCoroutine(FlyUp());

        // Play skill2 animation

        // Wave 1: Vertical spikes (4 columns)
        yield return StartCoroutine(FireVerticalSpikeWave());

        yield return new WaitForSeconds(spikeDelayBetweenWaves);

        // Wave 2: Horizontal spikes (3 rows)
        yield return StartCoroutine(FireHorizontalSpikeWave());

        yield return new WaitForSeconds(0.5f);

        // Fly down
        yield return StartCoroutine(FlyDown());

        yield return new WaitForSeconds(1.5f);
    }

    private IEnumerator FlyUp()
    {
        Debug.Log($"🐲 Flying up!");
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.up * ultimateFlyHeight;

        float elapsed = 0f;
        while (elapsed < ultimateFlyDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / ultimateFlyDuration;
            transform.position = Vector3.Lerp(startPos, targetPos, progress);
            yield return null;
        }

        RPC_ShowFlyEffect(transform.position);
    }

    private IEnumerator FlyDown()
    {
        Debug.Log($"🐲 Flying down!");
        Vector3 startPos = transform.position;
        Vector3 targetPos = new Vector3(originalPosition.x, originalPosition.y, originalPosition.z);

        float elapsed = 0f;
        while (elapsed < ultimateFlyDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / ultimateFlyDuration;
            transform.position = Vector3.Lerp(startPos, targetPos, progress);
            yield return null;
        }

        transform.position = targetPos;
    }

    // ========== Spike Pattern Waves ========== (แก้ไขให้สร้างหลาย prefab)

    // ========== แก้ไข Ultimate Spike Pattern - เพิ่มความแม่นยำ ==========
    // ========== แก้ไข Ultimate Pattern - ใช้ระยะคงที่ ==========
    private IEnumerator FireVerticalSpikeWave()
    {
        Debug.Log($"🐲 Firing vertical spike wave!");

        if (targetTransform == null)
        {
            FindNearestPlayer();
        }

        Vector3 playerPos = targetTransform != null ? targetTransform.position : transform.position;
        Vector3 bossPos = new Vector3(transform.position.x, telegraphYOffset, transform.position.z);

        // คำนวณตำแหน่งเริ่มต้นให้คลอบผู้เล่น
        float totalWidth = spikeWidth * 7;
        float startX = playerPos.x - totalWidth / 2f;

        List<SpikeData> spikeDataList = new List<SpikeData>();

        for (int i = 0; i < 4; i++)
        {
            float xPos = startX + (i * spikeWidth * 2);

            // ✅ เข็มเริ่มจากด้านหลัง Boss
            Vector3 spikeStartPos = new Vector3(xPos, telegraphYOffset, bossPos.z - 2f); // เริ่มหลัง Boss 2 เมตร

            spikeDataList.Add(new SpikeData(spikeStartPos, Vector3.forward, spikeWidth, spikeLength));
        }

        // Show telegraph
        foreach (SpikeData data in spikeDataList)
        {
            ShowSpikeTelegraph(data.position, data.direction, data.width, data.length);
        }

        yield return new WaitForSeconds(spikeTelegraphDuration);

        // Fire spikes
        foreach (SpikeData data in spikeDataList)
        {
            FireSpike(data.position, data.direction, data.width, data.length);
        }

        ClearAllTelegraphs();
    }

    private IEnumerator FireHorizontalSpikeWave()
    {
        Debug.Log($"🐲 Firing horizontal spike wave!");

        if (targetTransform == null)
        {
            FindNearestPlayer();
        }

        Vector3 playerPos = targetTransform != null ? targetTransform.position : transform.position;
        Vector3 bossPos = new Vector3(transform.position.x, telegraphYOffset, transform.position.z);

        float totalHeight = spikeWidth * 5;
        float startZ = playerPos.z - totalHeight / 2f;

        List<SpikeData> spikeDataList = new List<SpikeData>();

        for (int i = 0; i < 3; i++)
        {
            float zPos = startZ + (i * spikeWidth * 2);

            // ✅ เข็มเริ่มจากด้านซ้าย Boss
            Vector3 spikeStartPos = new Vector3(bossPos.x - 2f, telegraphYOffset, zPos); // เริ่มซ้าย Boss 2 เมตร

            spikeDataList.Add(new SpikeData(spikeStartPos, Vector3.right, spikeWidth, spikeLength));
        }

        // Show telegraph
        foreach (SpikeData data in spikeDataList)
        {
            ShowSpikeTelegraph(data.position, data.direction, data.width, data.length);
        }

        yield return new WaitForSeconds(spikeTelegraphDuration);

        // Fire spikes
        foreach (SpikeData data in spikeDataList)
        {
            FireSpike(data.position, data.direction, data.width, data.length);
        }

        ClearAllTelegraphs();
    }

    // ========== SKILL 6: Vertical Spike ==========
    private IEnumerator Skill6_VerticalSpike()
    {
        Debug.Log($"🐲 Skill 6: Vertical Spike");

        yield return StartCoroutine(FlyUp());
        RPC_PlayAnimation(ANIM_SKILL2);

        yield return StartCoroutine(FireSingleVerticalSpike());

        yield return StartCoroutine(FlyDown());
        yield return new WaitForSeconds(1f);
    }

    private IEnumerator FireSingleVerticalSpike()
    {
        if (targetTransform == null)
        {
            FindNearestPlayer();
        }

        Vector3 playerPos = targetTransform != null ? targetTransform.position : transform.position;
        Vector3 spikePos = new Vector3(playerPos.x, telegraphYOffset, transform.position.z);

        ShowSpikeTelegraph(spikePos, Vector3.forward, spikeWidth, spikeLength);
        yield return new WaitForSeconds(spikeTelegraphDuration);

        FireSpike(spikePos, Vector3.forward, spikeWidth, spikeLength);
        ClearAllTelegraphs();
    }

    // ========== SKILL 7: Horizontal Spike ==========
    private IEnumerator Skill7_HorizontalSpike()
    {
        Debug.Log($"🐲 Skill 7: Horizontal Spike");

        yield return StartCoroutine(FlyUp());
        RPC_PlayAnimation("skill2");

        yield return StartCoroutine(FireSingleHorizontalSpike());

        yield return StartCoroutine(FlyDown());
        yield return new WaitForSeconds(1f);
    }

    // ========== แก้ไข Skill 7 ให้ส่งค่าถูกต้อง ==========
    private IEnumerator FireSingleHorizontalSpike()
    {
        if (targetTransform == null)
        {
            FindNearestPlayer();
        }

        Vector3 playerPos = targetTransform != null ? targetTransform.position : transform.position;
        Vector3 spikePos = new Vector3(transform.position.x, telegraphYOffset, playerPos.z);

        // ✅ สำหรับแนวนอน: width = spikeWidth, length = spikeLength
        ShowSpikeTelegraph(spikePos, Vector3.right, spikeWidth, spikeLength);
        yield return new WaitForSeconds(spikeTelegraphDuration);

        FireSpike(spikePos, Vector3.right, spikeWidth, spikeLength);
        ClearAllTelegraphs();
    }

    // ========== SKILL 8: Ultimate Mix ==========
    private IEnumerator Skill8_UltimateMix()
    {
        Debug.Log($"🐲 Skill 8: Ultimate Mix!");
        RPC_PlayAnimation(ANIM_SKILL2);

        // ใช้ Ultimate
        yield return StartCoroutine(Skill5_Ultimate());

        // สุ่มสกิล Phase 1 หรือ Phase 2
        yield return new WaitForSeconds(1f);

        SkillType randomSkill = SelectRandomSkill();
        yield return StartCoroutine(PerformSkill(randomSkill));
    }

    // ========== Spike Helpers ==========
    // ========== Spike Helpers ========== (แก้ไขส่วนนี้)
    // ========== Spike Helpers ========== (แก้ไขให้แนวนอนหมุนถูก)
    // ========== Spike Helpers ========== (แก้ไขให้แนวนอนหมุนถูก)
    // ========== วิธีที่ 2: ใช้ Prefab แยก ==========
    private void ShowSpikeTelegraph(Vector3 position, Vector3 direction, float width, float length)
    {
        GameObject telegraph;
        Quaternion rotation = Quaternion.LookRotation(direction);

        // ✅ เลือก prefab ตามทิศทาง
        GameObject prefabToUse;
        if (direction == Vector3.right || direction == -Vector3.right)
        {
            // แนวนอน - ใช้ prefab พิเศษ
            prefabToUse = spikeTelegraphHorizontalPrefab != null ? spikeTelegraphHorizontalPrefab : spikeTelegraphPrefab;
            Debug.Log($"🐲 Using Horizontal Telegraph Prefab");
        }
        else
        {
            // แนวตั้ง - ใช้ prefab ปกติ
            prefabToUse = spikeTelegraphPrefab;
            Debug.Log($"🐲 Using Vertical Telegraph Prefab");
        }

        if (prefabToUse != null)
        {
            telegraph = Instantiate(prefabToUse, position, rotation);
            telegraph.transform.localScale = new Vector3(width, telegraph.transform.localScale.y, length);
        }
        else
        {
            Debug.LogWarning("🐲 Spike Telegraph Prefab is missing!");
            return;
        }

        telegraph.name = "SpikeTelegraph";
        activeTelegraphs.Add(telegraph);
    }

    // ========== แก้ไข FireSpike - เพิ่มความแม่นยำของ Hitbox ==========
    // ========== แก้ไข FireSpike - ปรับ Hitbox ให้แม่นยำ ==========
    // ========== แก้ไข FireSpike - ให้เข็มเคลื่อนที่แทนการยืด ==========
    // ========== แก้ไข FireSpike - สลับการหมุนเข็ม ==========
    // ========== แก้ไข FireSpike - ใช้มุมที่ถูกต้อง ==========
    private void FireSpike(Vector3 position, Vector3 direction, float width, float length)
    {
        GameObject spike = null;
        if (spikePrefab != null)
        {
            Vector3 spawnPos = position;
            Quaternion rotation = Quaternion.LookRotation(direction);

            spike = Instantiate(spikePrefab, spawnPos, rotation);

            // ✅ ใช้มุมที่ถูกต้อง
            if (direction == Vector3.forward || direction == -Vector3.forward)
            {
                // แนวตั้ง - หมุน (-90, 0, 90)
                spike.transform.localScale = new Vector3(width, spike.transform.localScale.y, width);
                spike.transform.rotation = Quaternion.Euler(-90f, 0f, 270f);

                Debug.Log($"🐲 Vertical Spike created - Rotation: (-90, 0, 270)");
            }
            else if (direction == Vector3.right || direction == -Vector3.right)
            {
                // แนวนอน - ไม่หมุน (0, 0, 0)
                spike.transform.localScale = new Vector3(width, spike.transform.localScale.y, width);
                spike.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

                Debug.Log($"🐲 Horizontal Spike created - Rotation: (0, 0, 0)");
            }
            else
            {
                spike.transform.localScale = new Vector3(width, spike.transform.localScale.y, width);
            }

            spike.name = "PoisonSpike";
            activeSpikes.Add(spike);

            // Initialize handler
            SpikeCollisionHandler handler = spike.GetComponent<SpikeCollisionHandler>();
            if (handler != null)
            {
                handler.Initialize(this, spikeDamage, poisonDamagePerTick, poisonDuration);
            }
            else
            {
                Debug.LogWarning("⚠️ SpikeCollisionHandler not found on Spike Prefab!");
            }

            Vector3 targetPos = position + direction * length;
            StartCoroutine(MoveSpikeForward(spike, targetPos, 0.5f));

            Destroy(spike, spikeVisibleDuration);
        }

        if (spikeEffect != null)
        {
            ParticleSystem fx = Instantiate(spikeEffect, position, Quaternion.LookRotation(direction));
            Destroy(fx.gameObject, 2f);
        }

        RPC_ShowSpikeEffect(position, direction);
    }

    // ✅ เพิ่ม method ใหม่: ตั้งค่า Collider
    private void SetupSpikeCollider(GameObject spike, float width)
    {
        // เช็คว่ามี Collider อยู่แล้วหรือไม่
        Collider existingCollider = spike.GetComponent<Collider>();
        if (existingCollider != null)
        {
            // ถ้ามีแล้ว ให้ตั้งเป็น Trigger
            existingCollider.isTrigger = true;
            Debug.Log($"🐲 Spike already has Collider: {existingCollider.GetType().Name}");
        }
        else
        {
            // ถ้าไม่มี ให้สร้างใหม่
            BoxCollider collider = spike.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(width, 2f, width); // ปรับขนาดตามต้องการ

            Debug.Log($"🐲 Added BoxCollider to Spike");
        }

        // ✅ เพิ่ม SpikeCollisionHandler component
        SpikeCollisionHandler handler = spike.GetComponent<SpikeCollisionHandler>();
        if (handler == null)
        {
            handler = spike.AddComponent<SpikeCollisionHandler>();
        }

        // ส่งข้อมูล damage ให้ handler
        handler.Initialize(this, spikeDamage, poisonDamagePerTick, poisonDuration);
    }

    // ✅ แก้ไข DelayedSpikeCollisionCheck
   
    // ========== เพิ่ม Debug Gizmos เพื่อดู Hitbox ==========

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

    // 🆕 เพิ่ม method สำหรับลบ spikes
    private void ClearAllSpikes()
    {
        foreach (GameObject spike in activeSpikes)
        {
            if (spike != null)
            {
                Destroy(spike);
            }
        }
        activeSpikes.Clear();
    }
    // ========== แก้ไข MoveSpikeForward - ให้เคลื่อนที่ถูกต้อง ==========
    // ========== แก้ไข MoveSpikeForward - ให้เคลื่อนที่เป็นเส้นตรง ==========
    private IEnumerator MoveSpikeForward(GameObject spike, Vector3 targetPosition, float duration)
    {
        if (spike == null) yield break;

        Vector3 startPosition = spike.transform.position;
        float elapsed = 0f;

        // ✅ คำนวณความเร็ว
        float distance = Vector3.Distance(startPosition, targetPosition);
        float speed = distance / duration;

        Debug.Log($"🐲 Spike moving from {startPosition} to {targetPosition} (Distance: {distance:F2}m, Speed: {speed:F2}m/s)");

        while (elapsed < duration)
        {
            if (spike == null) yield break;

            elapsed += Time.deltaTime;

            // ✅ เคลื่อนที่เป็นเส้นตรงด้วยความเร็วคงที่
            spike.transform.position = Vector3.MoveTowards(
                spike.transform.position,
                targetPosition,
                speed * Time.deltaTime
            );

            // ถ้าถึงแล้วให้หยุด
            if (Vector3.Distance(spike.transform.position, targetPosition) < 0.1f)
            {
                break;
            }

            yield return null;
        }

        if (spike != null)
        {
            spike.transform.position = targetPosition;
        }
    }
    // ========== Telegraph Creation Helpers ==========

    // ========== เพิ่ม Debug Visualization ==========
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Poison AoE - วงกลมรอบตัว
        Gizmos.color = new Color(0.5f, 1f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, poisonAoERadius);

        // แสดง spike pattern (ถ้ามี target)
        if (targetTransform != null && Application.isPlaying)
        {
            Vector3 playerPos = targetTransform.position;

            // Vertical spikes
            Gizmos.color = Color.yellow;
            float totalWidth = spikeWidth * 7;
            float startX = playerPos.x - totalWidth / 2f;

            for (int i = 0; i < 4; i++)
            {
                float xPos = startX + (i * spikeWidth * 2);
                Vector3 start = new Vector3(xPos, 0, transform.position.z);
                Vector3 end = start + Vector3.forward * spikeLength;
                Gizmos.DrawLine(start, end);
            }

            // Horizontal spikes
            Gizmos.color = Color.cyan;
            float totalHeight = spikeWidth * 5;
            float startZ = playerPos.z - totalHeight / 2f;

            for (int i = 0; i < 3; i++)
            {
                float zPos = startZ + (i * spikeWidth * 2);
                Vector3 start = new Vector3(transform.position.x, 0, zPos);
                Vector3 end = start + Vector3.right * spikeLength;
                Gizmos.DrawLine(start, end);
            }
        }
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
    }

    // ========== RPCs ==========
    // ========== แก้ไข RPC_PlayAnimation ให้เหมือน Archer ==========
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayAnimation(string animationName)
    {
        if (animator == null)
        {
            Debug.LogError($"❌ No Animator on {name}!");
            return;
        }

        // ✅ Debug: เช็คว่า parameter มีไหม
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
            Debug.LogError($"❌ Animator has no Trigger parameter: {animationName}");
            Debug.Log($"Available parameters:");
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                Debug.Log($"  - {param.name} ({param.type})");
            }
            return;
        }

        // ✅ เล่น animation
        Debug.Log($"✅ [RPC] Playing animation: {animationName} on {CharacterName}");
        animator.SetTrigger(animationName);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowTelegraph(Vector3 position)
    {
        Debug.Log($"[Client] Showing telegraph at {position}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowPoisonEffect(Vector3 position)
    {
        Debug.Log($"[Client] Poison effect at {position}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowSpikeEffect(Vector3 position, Vector3 direction)
    {
        Debug.Log($"[Client] Spike effect at {position}, direction: {direction}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowFlyEffect(Vector3 position)
    {
        Debug.Log($"[Client] Boss flying at {position}");
    }

    // แก้ไข RPC_OnDeath
    protected override void RPC_OnDeath()
    {
        ClearAllTelegraphs();
        ClearAllSpikes(); // 🆕 เพิ่มบรรทัดนี้
        DestroyAllMinions();

        if (currentTelegraph != null)
        {
            Destroy(currentTelegraph);
        }
        base.RPC_OnDeath();
    }
    private void DestroyAllMinions()
    {
        if (!HasStateAuthority) return;

        Debug.Log($"[BossSlime] Boss died - Destroying {summonedMinions.Count} minions");

        foreach (NetworkEnemy minion in summonedMinions)
        {
            if (minion != null && minion.Object != null)
            {
                Runner.Despawn(minion.Object);
            }
        }

        summonedMinions.Clear();
    }
    // ========== Debug Gizmos ==========
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        // Poison AoE range
        Gizmos.color = new Color(0.5f, 1f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, poisonAoERadius);

        // Dash distance
        if (targetTransform != null)
        {
            Vector3 direction = (targetTransform.position - transform.position).normalized;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + direction * dashDistance);
        }
    }
    private IEnumerator SummonMinions(int count)
    {
        if (minionPrefabs == null || minionPrefabs.Length == 0)
        {
            Debug.LogError("[BossSlime] ❌ No minion prefabs assigned!");
            yield break;
        }

        if (enemySpawner == null)
        {
            Debug.LogError("[BossSlime] ❌ EnemySpawner not found!");
            yield break;
        }

        Debug.Log($"👻 [BossSlime] Summoning {count} minions...");

        // Play summoning animation
        if (animator != null)
        {
            animator.SetTrigger("attack");
        }

        // Show summoning effect at Boss position
        if (summonEffectPrefab != null)
        {
            GameObject summonFX = Instantiate(summonEffectPrefab, transform.position, Quaternion.identity);
            Destroy(summonFX, 2f);
        }

        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < count; i++)
        {
            // ✅ สุ่มเลือก minion prefab
            NetworkEnemy randomMinionPrefab = minionPrefabs[Random.Range(0, minionPrefabs.Length)];

            if (randomMinionPrefab == null)
            {
                Debug.LogWarning($"[BossSlime] ⚠️ Minion prefab at index is null!");
                continue;
            }

            // สุ่มตำแหน่งรอบๆ Boss
            Vector3 randomOffset = Random.insideUnitCircle.normalized * summonRadius;
            Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, 0f, randomOffset.y);

            // ✅ ใช้ SpawnSummonedEnemy() จาก EnemySpawner
            NetworkEnemy minion = enemySpawner.SpawnSummonedEnemy(randomMinionPrefab, spawnPosition);

            if (minion != null)
            {
                summonedMinions.Add(minion);

                // Play spawn particle
                if (summonParticles != null)
                {
                    ParticleSystem particles = Instantiate(summonParticles, spawnPosition, Quaternion.identity);
                    Destroy(particles.gameObject, 2f);
                }

                Debug.Log($"✅ [BossSlime] Summoned {minion.CharacterName} ({i + 1}/{count}) at {spawnPosition}");
            }

            // Delay ระหว่างการเสก
            if (i < count - 1)
            {
                yield return new WaitForSeconds(0.3f);
            }
        }

        Debug.Log($"🎉 [BossSlime] Summoning complete! Total minions: {summonedMinions.Count}");

        // Show message to all clients
        RPC_ShowSummonMessage(count);
    }

    /// <summary>
    /// ลบลูกน้องที่ตายออกจาก List
    /// </summary>
    private void CleanupDeadMinions()
    {
        for (int i = summonedMinions.Count - 1; i >= 0; i--)
        {
            if (summonedMinions[i] == null || summonedMinions[i].IsDead)
            {
                Debug.Log($"[BossSlime] Minion died - Remaining: {summonedMinions.Count - 1}");
                summonedMinions.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// เช็คว่ามีลูกน้องเหลืออยู่กี่ตัว
    /// </summary>
    private int GetActiveMinionCount()
    {
        CleanupDeadMinions();
        return summonedMinions.Count;
    }

    /// <summary>
    /// เสกลูกน้องเพิ่มถ้ามีน้อยกว่าจำนวนสูงสุด (สามารถเรียกใช้ใน AI Loop)
    /// </summary>
    private void TrySummonMoreMinions()
    {
        if (!HasStateAuthority) return;
        if (minionPrefabs == null || minionPrefabs.Length == 0) return;
        if (enemySpawner == null) return;

        int currentCount = GetActiveMinionCount();
        int neededCount = maxMinions - currentCount;

        if (neededCount > 0 && !isPerformingSkill)
        {
            Debug.Log($"[BossSlime] Need to summon {neededCount} more minions");
            StartCoroutine(SummonMinions(neededCount));
        }
    }

    /// <summary>
    /// RPC แสดงข้อความเสกลูกน้อง
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowSummonMessage(int count)
    {
        Debug.Log($"💥 [BossSlime] Boss summoned {count} minions!");
        // TODO: แสดง UI notification ให้ผู้เล่น
    }

}
// ✅ เพิ่ม Helper Class สำหรับเก็บข้อมูล Spike
[System.Serializable]
public class SpikeData
{
    public Vector3 position;
    public Vector3 direction;
    public float width;
    public float length;

    public SpikeData(Vector3 pos, Vector3 dir, float w, float l)
    {
        position = pos;
        direction = dir;
        width = w;
        length = l;
    }
}