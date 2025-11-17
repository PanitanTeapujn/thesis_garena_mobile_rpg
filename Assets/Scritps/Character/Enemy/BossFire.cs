using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class BossFire : NetworkEnemy
{
    [Header("🔥 Fire Boss Settings")]
    [SerializeField] private float phase2HPThreshold = 0.7f; // 70% HP
    [SerializeField] private float phase3HPThreshold = 0.5f; // 50% HP
   
    private bool hasEnteredPhase2 = false;
    private bool hasEnteredPhase3 = false;

    [Header("🔥 Heat Gauge System")]
    [Networked] private int CurrentHeat { get; set; }
    private const int MAX_HEAT = 5;
    [SerializeField] private float heatEnhancedAoEMultiplier = 1.5f; // +50% AoE
    [SerializeField] private float heatEnhancedBurnMultiplier = 2f; // x2 Burn damage
    private bool isNextSkillEnhanced = false;

    [Header("⚡ Skill Cooldowns")]
    [SerializeField] private float fireSlamCooldown = 3f;
    [SerializeField] private float fireDashCooldown = 5f;
    [SerializeField] private float fireJumpSlamCooldown = 6f;
    [SerializeField] private float fireProjectileCooldown = 4f;
    [SerializeField] private float meteorRainCooldown = 20f;
    [SerializeField] private float fireShieldCooldown = 15f;
    [SerializeField] private float shockwaveCooldown = 6f;
    [SerializeField] private float enhancedThrowCooldown = 8f;
    [SerializeField] private float parryCounterCooldown = 10f;
    [SerializeField] private float combo1Cooldown = 25f; // Skill 11
    [SerializeField] private float combo2Cooldown = 30f; // Skill 12
    [SerializeField] private float combo3Cooldown = 25f; // Skill 13

    [Header("🔥 Skill 1: Fire Slam")]
    [SerializeField] private float fireSlamRadius = 5f;
    [SerializeField] private float fireSlamTelegraphDuration = 1.2f;
    [SerializeField] private int fireSlamDamage = 30;
    [SerializeField] private int fireSlamBurnDamage = 5;
    [SerializeField] private float fireSlamBurnDuration = 4f;
    [SerializeField] private GameObject fireSlamTelegraphPrefab;
    [SerializeField] private ParticleSystem fireSlamEffect;

    [Header("🔥 Skill 2: Fire Dash")]
    [SerializeField] private float dashDistance = 10f;
    [SerializeField] private float dashDuration = 0.4f;
    [SerializeField] private float dashWidth = 2.5f;
    [SerializeField] private float dashTelegraphDuration = 1.2f;
    [SerializeField] private GameObject dashTelegraphPrefab;
    [SerializeField] private GameObject fireTrailPrefab;
    [SerializeField] private float fireTrailLifetime = 6f;
    [SerializeField] private int fireTrailBurnDamage = 3;
    [SerializeField] private float fireTrailBurnDuration = 3f;
    [SerializeField] private ParticleSystem dashEffect;

    [Header("🔥 Skill 3: Fire Jump Slam")]
    [SerializeField] private float jumpHeight = 5f;
    [SerializeField] private float jumpDuration = 0.6f;
    [SerializeField] private float jumpSlamRadius = 5f;
    [SerializeField] private float jumpSlamTelegraphDuration = 1.2f;
    [SerializeField] private int jumpSlamDamage = 35;
    [SerializeField] private int jumpSlamBurnDamage = 6;
    [SerializeField] private float jumpSlamBurnDuration = 5f;
    [SerializeField] private GameObject jumpSlamTelegraphPrefab;
    [SerializeField] private ParticleSystem jumpSlamEffect;

    [Header("🔥 Skill 4: Fire Projectile x3")]
    [SerializeField] private float projectileTelegraphDuration = 1.5f;
    [SerializeField] private float projectileTravelTime = 0.8f;
    [SerializeField] private float projectileRadius = 2f;
    [SerializeField] private int projectileDamage = 25;
    [SerializeField] private int projectileBurnDamage = 5;
    [SerializeField] private float projectileBurnDuration = 4f;
    [SerializeField] private GameObject projectileTelegraphPrefab;
    [SerializeField] private GameObject fireProjectilePrefab;
    [SerializeField] private ParticleSystem projectileEffect;

    [Header("☄️ Skill 5: Meteor Rain")]
    [SerializeField] private int minMeteorCount = 10;
    [SerializeField] private int maxMeteorCount = 15;
    [SerializeField] private float meteorRainDuration = 10f;
    [SerializeField] private float meteorSpawnInterval = 0.8f; // Average interval
    [SerializeField] private float meteorRadius = 2f; // Same as Skill 4
    [SerializeField] private int meteorDamage = 25; // Same as Skill 4
    [SerializeField] private int meteorBurnDamage = 5;
    [SerializeField] private float meteorBurnDuration = 4f;
    [SerializeField] private GameObject meteorTelegraphPrefab;
    [SerializeField] private GameObject meteorPrefab;
    [SerializeField] private ParticleSystem meteorEffect;

    [Header("🛡️ Skill 6: Fire Shield")]
    [SerializeField] private float fireShieldDuration = 5f;
    [SerializeField] private float fireShieldRadius = 3f;
    [SerializeField] private float fireShieldTickInterval = 0.5f;
    [SerializeField] private int fireShieldDamage = 10;
    [SerializeField] private int fireShieldBurnDamage = 3;
    [SerializeField] private float fireShieldBurnDuration = 3f;
    [SerializeField] private ParticleSystem fireShieldEffect;

    [Header("⚡ Skill 7: Shockwave")]
    [SerializeField] private float shockwaveLength = 15f;
    [SerializeField] private float shockwaveWidth = 2f;
    [SerializeField] private float shockwaveTelegraphDuration = 1f;
    [SerializeField] private int shockwaveDamage = 15; // 50% of normal skills
    [SerializeField] private float shockwaveStunDuration = 1f;
    [SerializeField] private GameObject shockwaveTelegraphPrefab;
    [SerializeField] private GameObject shockwavePrefab;
    [SerializeField] private ParticleSystem shockwaveEffect;

    [Header("🔥 Skill 8: Enhanced Throw + Teleport")]
    [SerializeField] private int minThrowCount = 1;
    [SerializeField] private int maxThrowCount = 3;
    [SerializeField] private float throwDelay = 0.5f;
    [SerializeField] private float enhancedSlamRadius = 7f; // Larger than normal Slam
    [SerializeField] private int enhancedSlamDamage = 40;
    [SerializeField] private int enhancedSlamBurnDamage = 8;

    [Header("🛡️ Skill 10: Parry Counter")]
    [SerializeField] private float parryDuration = 2f;
    [SerializeField] private float parryCounterDashSpeed = 0.2f; // Placeholder
    [SerializeField] private int parryCounterDamage = 40; // Placeholder
    [SerializeField] private ParticleSystem parryEffect;

   

    [Header("🎨 Visual Effects")]
    [SerializeField] private float telegraphYOffset = -1.16f;
    [SerializeField] private Color fireTelegraphColor = new Color(1f, 0.3f, 0f, 0.5f);

    [Header("🎬 Animation")]
    private Animator animator;
    private const string ANIM_ATTACK = "attack";
    private const string ANIM_CRASH = "crash";
    private const string ANIM_JUMP = "jump";
    private const string ANIM_THROW = "throw";
    private const string ANIM_HEAVY = "heavy";
    private const string ANIM_SHIELD = "shield";
    private const string ANIM_GROUND = "ground";
    private const string ANIM_PARRY = "parry";

    [Header("🔧 Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    // Private variables
    private bool isPerformingSkill = false;
    private bool isInParryState = false;
    private bool isMeteorRaining = false;
    private GameObject currentTelegraph;
    private List<GameObject> activeTelegraphs = new List<GameObject>();
    private List<GameObject> activeFireTrails = new List<GameObject>();
    private Vector3 originalPosition;
    private List<Hero> hitHeroes = new List<Hero>();

    private enum BossPhase
    {
        Phase1,
        Phase2,
        Phase3,
    }

    private enum SkillType
    {
        FireSlam = 1,
        FireDash = 2,
        FireJumpSlam = 3,
        FireProjectile = 4,
        MeteorRain = 5,
        FireShield = 6,
        Shockwave = 7,
        EnhancedThrow = 8,
        ParryCounter = 10,
        ThrowDashThrowCombo = 11,
        MeteorShockwaveThrowCombo = 12,
        ThrowTripleJumpCombo = 13
    }

    [Networked] private BossPhase CurrentPhase { get; set; }
    [Networked] private float NextFireSlamTime { get; set; }
    [Networked] private float NextFireDashTime { get; set; }
    [Networked] private float NextFireJumpSlamTime { get; set; }
    [Networked] private float NextFireProjectileTime { get; set; }
    [Networked] private float NextMeteorRainTime { get; set; }
    [Networked] private float NextFireShieldTime { get; set; }
    [Networked] private float NextShockwaveTime { get; set; }
    [Networked] private float NextEnhancedThrowTime { get; set; }
    [Networked] private float NextParryCounterTime { get; set; }
    [Networked] private float NextCombo1Time { get; set; }
    [Networked] private float NextCombo2Time { get; set; }
    [Networked] private float NextCombo3Time { get; set; }

    protected override void Start()
    {
        base.Start();
        SetupBossFireStats();
        CurrentPhase = BossPhase.Phase1;
        CurrentHeat = 0;
        originalPosition = transform.position;
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogWarning($"🔥 Animator not found on {CharacterName}!");
        }

        Debug.Log($"🔥 {CharacterName} Boss spawned!");
    }

    private void SetupBossFireStats()
    {
        CharacterName = "Fire Boss";
        // Stats will be set via CharacterStats ScriptableObject
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (HasStateAuthority && !IsDead)
        {
            CheckPhaseTransition();

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
        // Fire Boss doesn't use basic attacks - skill-based combat only
        Debug.Log($"🔥 Fire Boss: Basic attack disabled (skill-based combat only)");
    }

    // ========== Phase Management ==========
    private void CheckPhaseTransition()
    {
        float hpPercentage = (float)CurrentHp / MaxHp;

        // Phase 2: 70% HP
        if (!hasEnteredPhase2 && hpPercentage <= phase2HPThreshold)
        {
            hasEnteredPhase2 = true;
            CurrentPhase = BossPhase.Phase2;
            Debug.Log($"🔥 {CharacterName}: PHASE 2 ACTIVATED!");
            StartCoroutine(ForceMeteorRain());
        }

        // Phase 3: 50% HP
        if (!hasEnteredPhase3 && hpPercentage <= phase3HPThreshold)
        {
            hasEnteredPhase3 = true;
            CurrentPhase = BossPhase.Phase3;
            Debug.Log($"🔥 {CharacterName}: PHASE 3 ACTIVATED!");
            StartCoroutine(ForceMeteorRain());
        }

        // Last Stand: 10% HP
       
    }

    private IEnumerator ForceMeteorRain()
    {
        while (isPerformingSkill)
        {
            Debug.Log($"🔥 Waiting for current skill to finish...");
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log($"🔥🔥🔥 FORCING METEOR RAIN NOW!");
        yield return StartCoroutine(PerformSkill(SkillType.MeteorRain));
    }

    // ========== AI System ==========
    private void ProcessBossAI()
    {

        if (targetTransform == null)
        {
            FindNearestPlayer();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, targetTransform.position);

        // Select skill based on cooldowns
        if (distanceToPlayer <= 15f)
        {
            SkillType? selectedSkill = SelectRandomSkill();
            if (selectedSkill.HasValue)
            {
                StartCoroutine(PerformSkill(selectedSkill.Value));
            }
        }
    }

    private SkillType? SelectRandomSkill()
    {
        float currentTime = (float)Runner.SimulationTime;
        List<SkillType> availableSkills = new List<SkillType>();

        if (CurrentPhase == BossPhase.Phase1)
        {
            // Phase 1: Skills 1-4
            if (currentTime >= NextFireSlamTime) availableSkills.Add(SkillType.FireSlam);
            if (currentTime >= NextFireDashTime) availableSkills.Add(SkillType.FireDash);
            if (currentTime >= NextFireJumpSlamTime) availableSkills.Add(SkillType.FireJumpSlam);
            if (currentTime >= NextFireProjectileTime) availableSkills.Add(SkillType.FireProjectile);
        }
        else if (CurrentPhase == BossPhase.Phase2)
        {
            // Phase 2: Skills 1-4, 5, 6, 7, 8
            if (currentTime >= NextFireSlamTime) availableSkills.Add(SkillType.FireSlam);
            if (currentTime >= NextFireDashTime) availableSkills.Add(SkillType.FireDash);
            if (currentTime >= NextFireJumpSlamTime) availableSkills.Add(SkillType.FireJumpSlam);
            if (currentTime >= NextFireProjectileTime) availableSkills.Add(SkillType.FireProjectile);
            if (currentTime >= NextMeteorRainTime) availableSkills.Add(SkillType.MeteorRain);
            if (currentTime >= NextFireShieldTime) availableSkills.Add(SkillType.FireShield);
            if (currentTime >= NextShockwaveTime) availableSkills.Add(SkillType.Shockwave);
            if (currentTime >= NextEnhancedThrowTime) availableSkills.Add(SkillType.EnhancedThrow);
        }
        else if (CurrentPhase == BossPhase.Phase3)
        {
            // Phase 3: All skills + Combos
            if (currentTime >= NextFireSlamTime) availableSkills.Add(SkillType.FireSlam);
            if (currentTime >= NextFireDashTime) availableSkills.Add(SkillType.FireDash);
            if (currentTime >= NextFireJumpSlamTime) availableSkills.Add(SkillType.FireJumpSlam);
            if (currentTime >= NextFireProjectileTime) availableSkills.Add(SkillType.FireProjectile);
            if (currentTime >= NextMeteorRainTime) availableSkills.Add(SkillType.MeteorRain);
            if (currentTime >= NextFireShieldTime) availableSkills.Add(SkillType.FireShield);
            if (currentTime >= NextShockwaveTime) availableSkills.Add(SkillType.Shockwave);
            if (currentTime >= NextEnhancedThrowTime) availableSkills.Add(SkillType.EnhancedThrow);
            if (currentTime >= NextParryCounterTime) availableSkills.Add(SkillType.ParryCounter);
            if (currentTime >= NextCombo1Time) availableSkills.Add(SkillType.ThrowDashThrowCombo);
            if (currentTime >= NextCombo2Time) availableSkills.Add(SkillType.MeteorShockwaveThrowCombo);
            if (currentTime >= NextCombo3Time) availableSkills.Add(SkillType.ThrowTripleJumpCombo);
        }

        if (availableSkills.Count == 0) return null;

        // Random selection with weighted probability
        // TODO: Implement weighted selection based on skill difficulty
        return availableSkills[Random.Range(0, availableSkills.Count)];
    }

    // ========== Main Skill Executor ==========
    private IEnumerator PerformSkill(SkillType skill)
    {
        isPerformingSkill = true;
        StopMovement();
        originalPosition = transform.position;

        Debug.Log($"🔥🔥🔥 {CharacterName}: *** STARTING SKILL {skill} ***");

        // Check Heat Enhancement
        bool isEnhanced = (CurrentHeat >= MAX_HEAT);
        if (isEnhanced)
        {
            isNextSkillEnhanced = true;
            Debug.Log($"🔥 HEAT MAXED! Next fire skill will be ENHANCED!");
        }

        switch (skill)
        {
            case SkillType.FireSlam:
                Debug.Log(">>> Executing FireSlam");
                yield return StartCoroutine(Skill1_FireSlam());
                NextFireSlamTime = (float)Runner.SimulationTime + fireSlamCooldown;
                break;

            case SkillType.FireDash:
                Debug.Log(">>> Executing FireDash");
                yield return StartCoroutine(Skill2_FireDash());
                NextFireDashTime = (float)Runner.SimulationTime + fireDashCooldown;
                break;

            case SkillType.FireJumpSlam:
                Debug.Log(">>> Executing FireJumpSlam");
                yield return StartCoroutine(Skill3_FireJumpSlam());
                NextFireJumpSlamTime = (float)Runner.SimulationTime + fireJumpSlamCooldown;
                break;

            case SkillType.FireProjectile:
                Debug.Log(">>> Executing FireProjectile");
                yield return StartCoroutine(Skill4_FireProjectile());
                NextFireProjectileTime = (float)Runner.SimulationTime + fireProjectileCooldown;
                break;

            case SkillType.MeteorRain:
                Debug.Log(">>> Executing MeteorRain");
                yield return StartCoroutine(Skill5_MeteorRain());
                NextMeteorRainTime = (float)Runner.SimulationTime + meteorRainCooldown;
                break;

            case SkillType.FireShield:
                Debug.Log(">>> Executing FireShield");
                yield return StartCoroutine(Skill6_FireShield());
                NextFireShieldTime = (float)Runner.SimulationTime + fireShieldCooldown;
                break;

            case SkillType.Shockwave:
                Debug.Log(">>> Executing Shockwave");
                yield return StartCoroutine(Skill7_Shockwave());
                NextShockwaveTime = (float)Runner.SimulationTime + shockwaveCooldown;
                break;

            case SkillType.EnhancedThrow:
                Debug.Log(">>> Executing EnhancedThrow");
                yield return StartCoroutine(Skill8_EnhancedThrow());
                NextEnhancedThrowTime = (float)Runner.SimulationTime + enhancedThrowCooldown;
                break;

            case SkillType.ParryCounter:
                Debug.Log(">>> Executing ParryCounter");
                yield return StartCoroutine(Skill10_ParryCounter());
                NextParryCounterTime = (float)Runner.SimulationTime + parryCounterCooldown;
                break;

            case SkillType.ThrowDashThrowCombo:
                Debug.Log(">>> Executing ThrowDashThrowCombo");
                yield return StartCoroutine(Skill11_ThrowDashThrowCombo());
                NextCombo1Time = (float)Runner.SimulationTime + combo1Cooldown;
                break;

            case SkillType.MeteorShockwaveThrowCombo:
                Debug.Log(">>> Executing MeteorShockwaveThrowCombo");
                yield return StartCoroutine(Skill12_MeteorShockwaveThrowCombo());
                NextCombo2Time = (float)Runner.SimulationTime + combo2Cooldown;
                break;

            case SkillType.ThrowTripleJumpCombo:
                Debug.Log(">>> Executing ThrowTripleJumpCombo");
                yield return StartCoroutine(Skill13_ThrowTripleJumpCombo());
                NextCombo3Time = (float)Runner.SimulationTime + combo3Cooldown;
                break;
        }

        ResumeMovement();
        isPerformingSkill = false;

        Debug.Log($"🔥✅ {CharacterName}: SKILL {skill} COMPLETE!");
    }

    // ========== SKILL 1: Fire Slam ==========
    private IEnumerator Skill1_FireSlam()
    {
        Debug.Log($"🔥 Skill 1: Fire Slam");

        hitHeroes.Clear();

        // Telegraph
        yield return StartCoroutine(ShowCircleTelegraph(transform.position, GetEffectiveRadius(fireSlamRadius), fireSlamTelegraphDuration));

        // Play animation
        RPC_PlayAnimation(ANIM_ATTACK);

        yield return new WaitForSeconds(0.3f);

        // Execute attack
        ExecuteFireSlam();

        // Add Heat
        AddHeat(1);

        yield return new WaitForSeconds(1f);
    }

    private void ExecuteFireSlam()
    {
        float effectiveRadius = GetEffectiveRadius(fireSlamRadius);
        int effectiveBurnDamage = GetEffectiveBurnDamage(fireSlamBurnDamage);

        Collider[] targets = Physics.OverlapSphere(transform.position, effectiveRadius, LayerMask.GetMask("Player"));

        foreach (Collider target in targets)
        {
            Hero hero = target.GetComponent<Hero>();
            if (hero != null && !hitHeroes.Contains(hero))
            {
                hitHeroes.Add(hero);
                hero.TakeDamageFromAttacker(0, fireSlamDamage, this, DamageType.Magic);
                hero.ApplyStatusEffect(StatusEffectType.Burn, effectiveBurnDamage, fireSlamBurnDuration);
                Debug.Log($"🔥 Fire Slam hit {hero.CharacterName}!");
            }
        }

        if (fireSlamEffect != null)
        {
            ParticleSystem fx = Instantiate(fireSlamEffect, transform.position, Quaternion.identity);
            Destroy(fx.gameObject, 2f);
        }

        RPC_ShowEffect(transform.position, 1);
    }

    // ========== SKILL 2: Fire Dash ==========
    private IEnumerator Skill2_FireDash()
    {
        Debug.Log($"🔥 Skill 2: Fire Dash");

        hitHeroes.Clear();

        if (targetTransform == null)
        {
            FindNearestPlayer();
        }

        if (targetTransform == null)
        {
            Debug.LogWarning("🔥 No target for dash!");
            yield break;
        }

        Vector3 dashStartPos = transform.position;
        Vector3 directionToPlayer = (targetTransform.position - dashStartPos).normalized;
        directionToPlayer.y = 0;

        // Telegraph
        yield return StartCoroutine(ShowDashTelegraph(dashStartPos, directionToPlayer));

        // Play animation
        RPC_PlayAnimation(ANIM_CRASH);

        yield return new WaitForSeconds(0.2f);

        // Execute dash
        yield return StartCoroutine(ExecuteFireDash(dashStartPos, directionToPlayer));

        // Add Heat
        AddHeat(1);

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
            Debug.LogWarning("🔥 Dash Telegraph Prefab is missing!");
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

    private IEnumerator ExecuteFireDash(Vector3 startPos, Vector3 direction)
    {
        Debug.Log($"🔥 [Dash Start] From: {startPos}, Direction: {direction}");

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

        Debug.Log($"🔥 [Dash] Target position: {endPos} (Distance: {actualDashDistance}m)");

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

            // Create fire trail every 0.5m
            if (Vector3.Distance(lastTrailPos, targetPos) >= 0.5f)
            {
                CreateFireTrail(targetPos);
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

        Debug.Log($"🔥 [Dash Complete] Final position: {transform.position}");

        if (dashEffect != null)
        {
            dashEffect.Stop();
        }

        yield return new WaitForSeconds(0.1f);
    }

    private void CreateFireTrail(Vector3 position)
    {
        if (fireTrailPrefab != null)
        {
            Vector3 trailPos = new Vector3(position.x, telegraphYOffset, position.z);
            GameObject trail = Instantiate(fireTrailPrefab, trailPos, Quaternion.identity);

            // Initialize FireTrail component
            FireTrail fireTrail = trail.GetComponent<FireTrail>();
            if (fireTrail != null)
            {
                fireTrail.Initialize(fireTrailBurnDamage, fireTrailBurnDuration, fireTrailLifetime);
            }

            activeFireTrails.Add(trail);
            Destroy(trail, fireTrailLifetime);

            Debug.Log($"🔥 Fire Trail created at {trailPos}");
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

                Debug.Log($"🔥 Dash hit {hero.CharacterName}!");
            }
        }
    }

    // ========== SKILL 3: Fire Jump Slam ==========
    private IEnumerator Skill3_FireJumpSlam()
    {
        Debug.Log($"🔥 Skill 3: Fire Jump Slam");

        Vector3 initialTargetPosition = targetTransform != null ? targetTransform.position : transform.position;

        // Telegraph
        yield return StartCoroutine(ShowCircleTelegraph(initialTargetPosition, GetEffectiveRadius(jumpSlamRadius), jumpSlamTelegraphDuration));

        Vector3 finalTargetPosition = targetTransform != null ? targetTransform.position : initialTargetPosition;

        Debug.Log($"🔥 Jump Slam: Initial target={initialTargetPosition}, Final target={finalTargetPosition}");

        // Jump up
        yield return StartCoroutine(JumpUp());

        // Play animation
        RPC_PlayAnimation(ANIM_JUMP);

        // Slam down
        yield return StartCoroutine(SlamDown(finalTargetPosition));

        // Execute damage
        ExecuteJumpSlamDamage(finalTargetPosition);

        // Add Heat
        AddHeat(1);

        yield return new WaitForSeconds(0.8f);
    }

    private IEnumerator JumpUp()
    {
        Debug.Log($"🔥 Jumping up!");
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.up * jumpHeight;

        float elapsed = 0f;
        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / jumpDuration;
            float smoothProgress = 1f - Mathf.Pow(1f - progress, 2f);

            transform.position = Vector3.Lerp(startPos, targetPos, smoothProgress);
            yield return null;
        }
    }

    private IEnumerator SlamDown(Vector3 targetPosition)
    {
        Debug.Log($"🔥 Slamming down to {targetPosition}!");
        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(targetPosition.x, originalPosition.y, targetPosition.z);

        float elapsed = 0f;
        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / jumpDuration;
            float smoothProgress = Mathf.Pow(progress, 2f);

            transform.position = Vector3.Lerp(startPos, endPos, smoothProgress);

            if (HasStateAuthority)
            {
                NetworkedPosition = transform.position;
            }

            yield return null;
        }

        transform.position = endPos;

        if (HasStateAuthority)
        {
            NetworkedPosition = endPos;
        }

        Debug.Log($"🔥 Slam complete at {endPos}");
    }

    private void ExecuteJumpSlamDamage(Vector3 center)
    {
        float effectiveRadius = GetEffectiveRadius(jumpSlamRadius);
        int effectiveBurnDamage = GetEffectiveBurnDamage(jumpSlamBurnDamage);

        if (jumpSlamEffect != null)
        {
            ParticleSystem fx = Instantiate(jumpSlamEffect, center, Quaternion.identity);
            Destroy(fx.gameObject, 2f);
        }

        RPC_ShowEffect(center, 3);

        Collider[] targets = Physics.OverlapSphere(center, effectiveRadius, LayerMask.GetMask("Player"));

        foreach (Collider target in targets)
        {
            Hero hero = target.GetComponent<Hero>();
            if (hero != null)
            {
                hero.TakeDamageFromAttacker(0, jumpSlamDamage, this, DamageType.Magic);
                hero.ApplyStatusEffect(StatusEffectType.Burn, effectiveBurnDamage, jumpSlamBurnDuration);
                Debug.Log($"🔥 Jump Slam hit {hero.CharacterName}!");
            }
        }
    }

    // ========== SKILL 4: Fire Projectile x3 ==========
    private IEnumerator Skill4_FireProjectile()
    {
        Debug.Log($"🔥 Skill 4: Fire Projectile x3");

        if (targetTransform == null)
        {
            FindNearestPlayer();
        }

        if (targetTransform == null)
        {
            Debug.LogWarning("🔥 No target for projectile!");
            yield break;
        }

        Vector3 playerPosition = targetTransform.position;

        // Show 3 telegraphs
        List<Vector3> projectilePositions = new List<Vector3>
        {
            playerPosition,
            playerPosition + new Vector3(2f, 0f, 0f),
            playerPosition + new Vector3(-2f, 0f, 0f)
        };

        foreach (Vector3 pos in projectilePositions)
        {
            ShowProjectileTelegraph(pos);
        }

        yield return new WaitForSeconds(projectileTelegraphDuration);

        // Play animation
        RPC_PlayAnimation(ANIM_THROW);

        yield return new WaitForSeconds(0.3f);

        // Fire 3 projectiles
        foreach (Vector3 pos in projectilePositions)
        {
            StartCoroutine(FireProjectile(pos));
        }

        // Add Heat
        AddHeat(1);

        yield return new WaitForSeconds(projectileTravelTime + 1f);

        ClearAllTelegraphs();
    }

    private void ShowProjectileTelegraph(Vector3 targetPosition)
    {
        Vector3 telegraphPosition = new Vector3(targetPosition.x, telegraphYOffset, targetPosition.z);
        float effectiveRadius = GetEffectiveRadius(projectileRadius);

        GameObject telegraph = null;
        if (projectileTelegraphPrefab != null)
        {
            telegraph = Instantiate(projectileTelegraphPrefab, telegraphPosition, Quaternion.identity);
            telegraph.transform.localScale = new Vector3(effectiveRadius * 2f, 0.05f, effectiveRadius * 2f);
        }

        if (telegraph != null)
        {
            activeTelegraphs.Add(telegraph);
        }
    }

    private IEnumerator FireProjectile(Vector3 targetPosition)
    {
        Vector3 startPos = transform.position + Vector3.up * 1f;
        Vector3 endPos = new Vector3(targetPosition.x, targetPosition.y, targetPosition.z);

        GameObject projectile = null;
        if (fireProjectilePrefab != null)
        {
            projectile = Instantiate(fireProjectilePrefab, startPos, Quaternion.identity);
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
        float effectiveRadius = GetEffectiveRadius(projectileRadius);
        int effectiveBurnDamage = GetEffectiveBurnDamage(projectileBurnDamage);

        if (projectileEffect != null)
        {
            ParticleSystem fx = Instantiate(projectileEffect, center, Quaternion.identity);
            Destroy(fx.gameObject, 2f);
        }

        RPC_ShowEffect(center, 4);

        Collider[] targets = Physics.OverlapSphere(center, effectiveRadius, LayerMask.GetMask("Player"));

        foreach (Collider target in targets)
        {
            Hero hero = target.GetComponent<Hero>();
            if (hero != null)
            {
                hero.TakeDamageFromAttacker(0, projectileDamage, this, DamageType.Magic);
                hero.ApplyStatusEffect(StatusEffectType.Burn, effectiveBurnDamage, projectileBurnDuration);
                Debug.Log($"🔥 Fire Projectile hit {hero.CharacterName}!");
            }
        }
    }

    // ========== SKILL 5: Meteor Rain ==========
    private IEnumerator Skill5_MeteorRain()
    {
        Debug.Log($"🔥 Skill 5: Meteor Rain!");

        RPC_PlayAnimation(ANIM_HEAVY);

        yield return new WaitForSeconds(1f);

        // Start meteor rain coroutine (runs in background)
        StartCoroutine(MeteorRainLoop());

        // Add Heat
        AddHeat(2);

        // Boss can continue using other skills
        Debug.Log($"🔥 Meteor Rain started - Boss can use other skills now!");
    }

    private IEnumerator MeteorRainLoop()
    {
        isMeteorRaining = true;
        int meteorCount = Random.Range(minMeteorCount, maxMeteorCount + 1);
        float timePerMeteor = meteorRainDuration / meteorCount;

        Debug.Log($"🔥 Spawning {meteorCount} meteors over {meteorRainDuration} seconds");

        for (int i = 0; i < meteorCount; i++)
        {
            // Random position around arena
            Vector3 randomPos = transform.position + new Vector3(
                Random.Range(-10f, 10f),
                0f,
                Random.Range(-10f, 10f)
            );

            StartCoroutine(SpawnMeteor(randomPos));

            yield return new WaitForSeconds(timePerMeteor + Random.Range(-0.2f, 0.2f));
        }

        isMeteorRaining = false;
        Debug.Log($"🔥 Meteor Rain complete!");
    }

    private IEnumerator SpawnMeteor(Vector3 targetPosition)
    {
        // Telegraph
        Vector3 telegraphPosition = new Vector3(targetPosition.x, telegraphYOffset, targetPosition.z);
        GameObject telegraph = null;

        if (meteorTelegraphPrefab != null)
        {
            telegraph = Instantiate(meteorTelegraphPrefab, telegraphPosition, Quaternion.identity);
            telegraph.transform.localScale = new Vector3(meteorRadius * 2f, 0.05f, meteorRadius * 2f);
        }

        yield return new WaitForSeconds(1f);

        if (telegraph != null)
        {
            Destroy(telegraph);
        }

        // Spawn meteor from sky
        Vector3 meteorStartPos = targetPosition + Vector3.up * 15f;
        GameObject meteor = null;

        if (meteorPrefab != null)
        {
            meteor = Instantiate(meteorPrefab, meteorStartPos, Quaternion.identity);
        }

        // Fall down
        float fallDuration = 0.5f;
        float elapsed = 0f;

        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fallDuration;

            if (meteor != null)
            {
                meteor.transform.position = Vector3.Lerp(meteorStartPos, targetPosition, progress);
            }

            yield return null;
        }

        if (meteor != null)
        {
            Destroy(meteor);
        }

        // Impact damage
        ExecuteMeteorDamage(targetPosition);
    }

    private void ExecuteMeteorDamage(Vector3 center)
    {
        if (meteorEffect != null)
        {
            ParticleSystem fx = Instantiate(meteorEffect, center, Quaternion.identity);
            Destroy(fx.gameObject, 2f);
        }

        RPC_ShowEffect(center, 5);

        Collider[] targets = Physics.OverlapSphere(center, meteorRadius, LayerMask.GetMask("Player"));

        foreach (Collider target in targets)
        {
            Hero hero = target.GetComponent<Hero>();
            if (hero != null)
            {
                hero.TakeDamageFromAttacker(0, meteorDamage, this, DamageType.Magic);
                hero.ApplyStatusEffect(StatusEffectType.Burn, meteorBurnDamage, meteorBurnDuration);
                Debug.Log($"🔥 Meteor hit {hero.CharacterName}!");
            }
        }
    }

    // ========== SKILL 6: Fire Shield ==========
    private IEnumerator Skill6_FireShield()
    {
        Debug.Log($"🔥 Skill 6: Fire Shield!");

        RPC_PlayAnimation(ANIM_SHIELD);

        // Cleanse all debuffs
        statusEffectManager?.ClearAllStatusEffects();

        // Grant Debuff Immunity
        statusEffectManager?.ApplyDebuffImmunity(fireShieldDuration);

        // Start shield effect
        GameObject shieldFX = null;
        if (fireShieldEffect != null)
        {
            shieldFX = Instantiate(fireShieldEffect.gameObject, transform.position, Quaternion.identity);
            shieldFX.transform.SetParent(transform);
        }

        float elapsed = 0f;
        float nextTickTime = 0f;

        while (elapsed < fireShieldDuration)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= nextTickTime)
            {
                // Damage nearby players
                Collider[] targets = Physics.OverlapSphere(transform.position, fireShieldRadius, LayerMask.GetMask("Player"));

                foreach (Collider target in targets)
                {
                    Hero hero = target.GetComponent<Hero>();
                    if (hero != null)
                    {
                        hero.TakeDamageFromAttacker(0, fireShieldDamage, this, DamageType.Magic);
                        hero.ApplyStatusEffect(StatusEffectType.Burn, fireShieldBurnDamage, fireShieldBurnDuration);
                        Debug.Log($"🔥 Fire Shield damaged {hero.CharacterName}!");
                    }
                }

                nextTickTime += fireShieldTickInterval;
            }

            yield return null;
        }

        if (shieldFX != null)
        {
            Destroy(shieldFX);
        }

        Debug.Log($"🔥 Fire Shield ended");
        yield return new WaitForSeconds(0.5f);
    }

    // ========== SKILL 7: Shockwave ==========
    private IEnumerator Skill7_Shockwave()
    {
        Debug.Log($"🔥 Skill 7: Shockwave!");

        // Random direction (horizontal or vertical)
        bool isHorizontal = Random.Range(0, 2) == 0;
        Vector3 direction = isHorizontal ? Vector3.right : Vector3.forward;

        // Telegraph
        yield return StartCoroutine(ShowShockwaveTelegraph(transform.position, direction));

        // Play animation
        RPC_PlayAnimation(ANIM_GROUND);

        yield return new WaitForSeconds(0.3f);

        // Fire shockwave
        FireShockwave(transform.position, direction);

        // Add Heat
        AddHeat(1);

        yield return new WaitForSeconds(1f);

        ClearAllTelegraphs();
    }

    private IEnumerator ShowShockwaveTelegraph(Vector3 startPos, Vector3 direction)
    {
        Vector3 telegraphPos = new Vector3(startPos.x, telegraphYOffset, startPos.z);
        Quaternion rotation = Quaternion.LookRotation(direction);

        GameObject telegraph = null;
        if (shockwaveTelegraphPrefab != null)
        {
            telegraph = Instantiate(shockwaveTelegraphPrefab, telegraphPos, rotation);
            telegraph.transform.localScale = new Vector3(shockwaveWidth, 0.05f, shockwaveLength);
        }

        activeTelegraphs.Add(telegraph);

        yield return new WaitForSeconds(shockwaveTelegraphDuration);
    }

    private void FireShockwave(Vector3 position, Vector3 direction)
    {
        GameObject shockwave = null;
        if (shockwavePrefab != null)
        {
            Vector3 spawnPos = position;
            Quaternion rotation = Quaternion.LookRotation(direction);

            shockwave = Instantiate(shockwavePrefab, spawnPos, rotation);
            shockwave.transform.localScale = new Vector3(shockwaveWidth, shockwave.transform.localScale.y, shockwaveWidth);
        }

        Vector3 targetPos = position + direction * shockwaveLength;
        StartCoroutine(MoveShockwaveForward(shockwave, targetPos, 0.5f));

        if (shockwaveEffect != null)
        {
            ParticleSystem fx = Instantiate(shockwaveEffect, position, Quaternion.LookRotation(direction));
            Destroy(fx.gameObject, 2f);
        }

        RPC_ShowEffect(position, 7);

        Destroy(shockwave, 2f);
    }

    private IEnumerator MoveShockwaveForward(GameObject shockwave, Vector3 targetPosition, float duration)
    {
        if (shockwave == null) yield break;

        Vector3 startPosition = shockwave.transform.position;
        float elapsed = 0f;
        float distance = Vector3.Distance(startPosition, targetPosition);
        float speed = distance / duration;

        while (elapsed < duration)
        {
            if (shockwave == null) yield break;

            elapsed += Time.deltaTime;

            shockwave.transform.position = Vector3.MoveTowards(
                shockwave.transform.position,
                targetPosition,
                speed * Time.deltaTime
            );

            // Check collision
            CheckShockwaveCollision(shockwave.transform.position);

            if (Vector3.Distance(shockwave.transform.position, targetPosition) < 0.1f)
            {
                break;
            }

            yield return null;
        }

        if (shockwave != null)
        {
            shockwave.transform.position = targetPosition;
        }
    }

    private void CheckShockwaveCollision(Vector3 position)
    {
        Collider[] targets = Physics.OverlapSphere(position, shockwaveWidth / 2f, LayerMask.GetMask("Player"));

        foreach (Collider target in targets)
        {
            Hero hero = target.GetComponent<Hero>();
            if (hero != null && !hitHeroes.Contains(hero))
            {
                hitHeroes.Add(hero);
                hero.TakeDamageFromAttacker(0, shockwaveDamage, this, DamageType.Magic);
                hero.ApplyStatusEffect(StatusEffectType.Stun, 0, shockwaveStunDuration);
                Debug.Log($"🔥 Shockwave hit {hero.CharacterName}!");
            }
        }
    }

    // ========== SKILL 8: Enhanced Throw + Teleport ==========
    private IEnumerator Skill8_EnhancedThrow()
    {
        Debug.Log($"🔥 Skill 8: Enhanced Throw + Teleport!");

        if (targetTransform == null)
        {
            FindNearestPlayer();
        }

        if (targetTransform == null)
        {
            Debug.LogWarning("🔥 No target for enhanced throw!");
            yield break;
        }

        Vector3 playerPosition = targetTransform.position;
        int throwCount = Random.Range(minThrowCount, maxThrowCount + 1);

        List<Vector3> throwPositions = new List<Vector3>();
        for (int i = 0; i < throwCount; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));
            throwPositions.Add(playerPosition + offset);
        }

        // Show telegraphs
        foreach (Vector3 pos in throwPositions)
        {
            ShowProjectileTelegraph(pos);
        }

        yield return new WaitForSeconds(projectileTelegraphDuration);

        // Throw projectiles
        RPC_PlayAnimation(ANIM_THROW);

        Vector3 lastProjectilePos = Vector3.zero;

        for (int i = 0; i < throwPositions.Count; i++)
        {
            StartCoroutine(FireProjectile(throwPositions[i]));
            lastProjectilePos = throwPositions[i];

            if (i < throwPositions.Count - 1)
            {
                yield return new WaitForSeconds(throwDelay);
            }
        }

        yield return new WaitForSeconds(projectileTravelTime);

        // Teleport to last projectile
        Debug.Log($"🔥 Teleporting to {lastProjectilePos}");
        transform.position = new Vector3(lastProjectilePos.x, originalPosition.y, lastProjectilePos.z);

        if (HasStateAuthority)
        {
            NetworkedPosition = transform.position;
        }

        RPC_ShowEffect(transform.position, 8);

        yield return new WaitForSeconds(0.3f);

        // Execute enhanced slam
        RPC_PlayAnimation(ANIM_ATTACK);

        yield return new WaitForSeconds(0.3f);

        ExecuteEnhancedSlam();

        // Add Heat
        AddHeat(2);

        yield return new WaitForSeconds(1f);

        ClearAllTelegraphs();
    }

    private void ExecuteEnhancedSlam()
    {
        Collider[] targets = Physics.OverlapSphere(transform.position, enhancedSlamRadius, LayerMask.GetMask("Player"));

        foreach (Collider target in targets)
        {
            Hero hero = target.GetComponent<Hero>();
            if (hero != null)
            {
                hero.TakeDamageFromAttacker(0, enhancedSlamDamage, this, DamageType.Magic);
                hero.ApplyStatusEffect(StatusEffectType.Burn, enhancedSlamBurnDamage, fireSlamBurnDuration);
                Debug.Log($"🔥 Enhanced Slam hit {hero.CharacterName}!");
            }
        }

        if (fireSlamEffect != null)
        {
            ParticleSystem fx = Instantiate(fireSlamEffect, transform.position, Quaternion.identity);
            Destroy(fx.gameObject, 2f);
        }
    }

    // ========== SKILL 10: Parry Counter ==========
    private IEnumerator Skill10_ParryCounter()
    {
        Debug.Log($"🔥 Skill 10: Parry Counter!");

        RPC_PlayAnimation(ANIM_PARRY);

        if (parryEffect != null)
        {
            parryEffect.Play();
        }

        isInParryState = true;
        float elapsed = 0f;

        while (elapsed < parryDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        isInParryState = false;

        if (parryEffect != null)
        {
            parryEffect.Stop();
        }

        Debug.Log($"🔥 Parry window ended");
        yield return new WaitForSeconds(0.5f);
    }

    public  void TakeDamageFromAttacker(int attackDamage, int magicDamage, Character attacker, DamageType damageType)
    {
        // Check parry state
        if (isInParryState && HasStateAuthority)
        {
            Debug.Log($"🔥 PARRY SUCCESSFUL! Counter attacking {attacker.CharacterName}!");

            // Counter dash
            Vector3 direction = (attacker.transform.position - transform.position).normalized;
            direction.y = 0;

            StartCoroutine(ParryCounterDash(direction, attacker));
            return;
        }

        // Normal damage
        base.TakeDamageFromAttacker(attackDamage, magicDamage, attacker, damageType);
    }

    private IEnumerator ParryCounterDash(Vector3 direction, Character target)
    {
        hitHeroes.Clear();

        Vector3 startPos = transform.position;
        Vector3 targetPos = target.transform.position;
        float distance = Vector3.Distance(startPos, targetPos);

        RPC_PlayAnimation(ANIM_CRASH);

        if (dashEffect != null)
        {
            dashEffect.Play();
        }

        float elapsed = 0f;

        while (elapsed < parryCounterDashSpeed)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / parryCounterDashSpeed;

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, progress);

            if (rb != null)
            {
                rb.MovePosition(currentPos);
            }
            else
            {
                transform.position = currentPos;
            }

            if (HasStateAuthority)
            {
                NetworkedPosition = currentPos;
            }

            // Check collision
            CheckDashCollision(currentPos, direction);

            yield return null;
        }

        // Deal damage to target
        Hero hero = target as Hero;
        if (hero != null)
        {
            hero.TakeDamageFromAttacker(parryCounterDamage, 0, this, DamageType.Normal);
            Debug.Log($"🔥 Parry Counter hit {hero.CharacterName}!");
        }

        if (dashEffect != null)
        {
            dashEffect.Stop();
        }

        isInParryState = false;
    }

    // ========== SKILL 11: Throw-Dash-Throw Combo ==========
    private IEnumerator Skill11_ThrowDashThrowCombo()
    {
        Debug.Log($"🔥 Skill 11: Throw-Dash-Throw Combo!");

        // Part 1: Enhanced Throw
        yield return StartCoroutine(Skill8_EnhancedThrow());

        yield return new WaitForSeconds(0.5f);

        // Part 2: Fire Dash
        yield return StartCoroutine(Skill2_FireDash());

        yield return new WaitForSeconds(0.5f);

        // Part 3: Enhanced Throw again
        yield return StartCoroutine(Skill8_EnhancedThrow());

        Debug.Log($"🔥 Combo 11 complete!");
    }

    // ========== SKILL 12: Meteor-Shockwave-Throw Combo ==========
    private IEnumerator Skill12_MeteorShockwaveThrowCombo()
    {
        Debug.Log($"🔥 Skill 12: Meteor-Shockwave-Throw Combo!");

        // Part 1: Meteor Rain
        yield return StartCoroutine(Skill5_MeteorRain());

        yield return new WaitForSeconds(0.5f);

        // Part 2: Shockwave
        yield return StartCoroutine(Skill7_Shockwave());

        yield return new WaitForSeconds(0.5f);

        // Part 3: Enhanced Throw
        yield return StartCoroutine(Skill8_EnhancedThrow());

        Debug.Log($"🔥 Combo 12 complete!");
    }

    // ========== SKILL 13: Throw-Triple Jump Combo ==========
    private IEnumerator Skill13_ThrowTripleJumpCombo()
    {
        Debug.Log($"🔥 Skill 13: Throw-Triple Jump Combo!");

        // Part 1: Enhanced Throw
        yield return StartCoroutine(Skill8_EnhancedThrow());

        yield return new WaitForSeconds(0.5f);

        // Part 2: Triple Jump Slam
        for (int i = 0; i < 3; i++)
        {
            Debug.Log($"🔥 Jump Slam {i + 1}/3");
            yield return StartCoroutine(Skill3_FireJumpSlam());
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log($"🔥 Combo 13 complete!");
    }

    // ========== LAST STAND: Fake Death Ultimate Slam ==========
  

    

    // ========== Heat Gauge System ==========
    private void AddHeat(int amount)
    {
        if (CurrentHeat >= MAX_HEAT)
        {
            Debug.Log($"🔥 Heat already maxed at {MAX_HEAT}!");
            return;
        }

        CurrentHeat = Mathf.Min(CurrentHeat + amount, MAX_HEAT);
        Debug.Log($"🔥 Heat: {CurrentHeat}/{MAX_HEAT}");

        if (CurrentHeat >= MAX_HEAT)
        {
            Debug.Log($"🔥🔥🔥 HEAT MAXED! Next fire skill will be ENHANCED!");
        }
    }

    private void ResetHeat()
    {
        CurrentHeat = 0;
        isNextSkillEnhanced = false;
        Debug.Log($"🔥 Heat reset to 0");
    }

    private float GetEffectiveRadius(float baseRadius)
    {
        if (isNextSkillEnhanced)
        {
            float enhanced = baseRadius * heatEnhancedAoEMultiplier;
            Debug.Log($"🔥 ENHANCED AoE: {baseRadius} → {enhanced}");
            ResetHeat();
            return enhanced;
        }
        return baseRadius;
    }

    private int GetEffectiveBurnDamage(int baseBurnDamage)
    {
        if (isNextSkillEnhanced)
        {
            int enhanced = Mathf.RoundToInt(baseBurnDamage * heatEnhancedBurnMultiplier);
            Debug.Log($"🔥 ENHANCED Burn: {baseBurnDamage} → {enhanced}");
            return enhanced;
        }
        return baseBurnDamage;
    }

    // ========== Helper Methods ==========
    private IEnumerator ShowCircleTelegraph(Vector3 position, float radius, float duration)
    {
        Vector3 telegraphPosition = new Vector3(position.x, telegraphYOffset, position.z);

        if (fireSlamTelegraphPrefab != null)
        {
            currentTelegraph = Instantiate(fireSlamTelegraphPrefab, telegraphPosition, Quaternion.identity);
        }
        else
        {
            currentTelegraph = CreateProceduralCircleTelegraph(telegraphPosition, radius);
        }

        RPC_ShowTelegraph(telegraphPosition);

        float elapsed = 0f;
        Vector3 startScale = new Vector3(0.1f, 0.05f, 0.1f);
        Vector3 targetScale = new Vector3(radius * 2f, 0.05f, radius * 2f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            if (currentTelegraph != null)
            {
                currentTelegraph.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);

                float pulse = Mathf.Sin(elapsed * 10f) * 0.2f + 0.8f;
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

    private GameObject CreateProceduralCircleTelegraph(Vector3 position, float radius)
    {
        GameObject telegraph = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        telegraph.name = "FireBoss_CircleTelegraph";
        telegraph.transform.position = position;
        telegraph.transform.localScale = new Vector3(0.1f, 0.05f, 0.1f);

        Destroy(telegraph.GetComponent<Collider>());

        Renderer renderer = telegraph.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = fireTelegraphColor;
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
            Debug.LogError($"🔥 Animator has no Trigger parameter: {animationName}");
            return;
        }

        Debug.Log($"🔥 [RPC] Playing animation: {animationName} on {CharacterName}");
        animator.SetTrigger(animationName);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowTelegraph(Vector3 position)
    {
        Debug.Log($"[Client] Showing fire telegraph at {position}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowEffect(Vector3 position, int skillId)
    {
        Debug.Log($"[Client] Fire effect at {position} for skill {skillId}");
    }

    protected override void RPC_OnDeath()
    {
        ClearAllTelegraphs();

        foreach (GameObject trail in activeFireTrails)
        {
            if (trail != null)
            {
                Destroy(trail);
            }
        }
        activeFireTrails.Clear();

        if (currentTelegraph != null)
        {
            Destroy(currentTelegraph);
        }

        base.RPC_OnDeath();
    }

    // ========== Debug Gizmos ==========
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        // Fire Slam range
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, fireSlamRadius);

        // Jump Slam range
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        if (targetTransform != null)
        {
            Gizmos.DrawWireSphere(targetTransform.position, jumpSlamRadius);
        }
    }
}