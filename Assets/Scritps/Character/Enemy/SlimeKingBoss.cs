using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;

public enum SlimeKingPhase
{
    Phase1_Normal,      // เฟส 1: โจมตีปกติ
    Phase2_Enraged,     // เฟส 2: โกรธ (75% HP)
    Phase3_Desperate    // เฟส 3: สุดท้าย (25% HP)
}

public class SlimeKingBoss : NetworkEnemy
{
    [Header("👑 Slime King Boss Settings")]
    [SerializeField] private SlimeKingPhase currentPhase = SlimeKingPhase.Phase1_Normal;
    [SerializeField] private float phaseTransitionDelay = 2f;
    [SerializeField] private bool isInPhaseTransition = false;

    [Header("🎯 Boss Stats")]
    [SerializeField] private int bossLevel = 15;               // Level สูง
    [SerializeField] private float bossScale = 2.5f;           // ขนาดใหญ่กว่าปกติ
    [SerializeField] private float bossSpeedMultiplier = 1.2f; // เร็วกว่า slime ปกติ

    [Header("💥 Multi-Attack System")]
    [SerializeField] private float multiAttackRadius = 8f;     // รัศมีโจมตีพื้นที่
    [SerializeField] private int maxMultiTargets = 3;          // โจมตีได้สูงสุด 3 เป้าหมาย
    [SerializeField] private float slamDamageMultiplier = 1.5f; // ดาเมจการกระแทก

    [Header("🌪️ Special Abilities")]
    [SerializeField] private bool canSummonMinions = true;     // เรียก slime น้อย
    [SerializeField] private GameObject[] minionPrefabs;       // Prefabs ของ slime น้อย
    [SerializeField] private int maxMinions = 6;               // จำนวนสูงสุดของ minions
    [SerializeField] private float summonCooldown = 15f;       // คูลดาวน์การเรียก

    [Header("🔮 Elemental Powers")]
    [SerializeField] private bool hasElementalPowers = true;   // มีพลังธาตุ
    [SerializeField] private float elementalAttackChance = 40f; // โอกาสใช้โจมตีธาตุ
    [SerializeField] private float auraDamageMultiplier = 1.3f; // ดาเมจออร่า

    [Header("🛡️ Boss Mechanics")]
    [SerializeField] private float rageThreshold = 0.75f;      // HP ที่เข้าเฟส 2
    [SerializeField] private float desperateThreshold = 0.25f; // HP ที่เข้าเฟส 3
    [SerializeField] private float immunityDuration = 3f;       // ช่วงเวลา immune หลังเปลี่ยนเฟส
    [SerializeField] private bool isImmune = false;            // สถานะ immune

    [Header("🎨 Boss Visual Effects")]
    [SerializeField] private ParticleSystem crownEffect;       // เอฟเฟกต์มงกุฎ
    [SerializeField] private ParticleSystem phaseTransitionFX; // เอฟเฟกต์เปลี่ยนเฟส
    [SerializeField] private ParticleSystem rageAura;          // ออร่าโกรธ
    [SerializeField] private ParticleSystem elementalAura;     // ออร่าธาตุ
    [SerializeField] private Light bossLight;                  // แสงพิเศษ

    [Header("📊 Boss UI")]
    [SerializeField] private bool showBossHealthBar = true;    // แสดง Boss Health Bar

    // Boss State Management
    [Networked] public SlimeKingPhase NetworkedPhase { get; set; }
    [Networked] public int ActiveMinionsCount { get; set; } = 0;
    [Networked] public bool IsPerformingSpecialAttack { get; set; } = false;

    private float nextSummonTime = 0f;
    private float nextSpecialAttackTime = 0f;
    private float immunityEndTime = 0f;
    private List<GameObject> activeMinions = new List<GameObject>();
    private Coroutine bossAICoroutine;

    // Phase transition tracking
    private bool hasTriggeredPhase2 = false;
    private bool hasTriggeredPhase3 = false;

    protected override void Start()
    {
        base.Start();

        SetupBossStats();
        InitializeBossEffects();

        if (HasStateAuthority)
        {
            // เริ่ม Boss AI
            bossAICoroutine = StartCoroutine(BossAIRoutine());

            // แสดง Boss Health Bar
            if (showBossHealthBar)
            {
                ShowBossHealthBar();
            }
        }
    }

    private void SetupBossStats()
    {
        CharacterName = "Slime King";
        AttackType = AttackType.Mixed; // ใช้ทั้ง Physical และ Magic

        // เพิ่มขนาด

        // เพิ่ม stats ให้เหมาะกับ boss
        MaxHp *= 8; // HP มากกว่าปกติ 8 เท่า
        CurrentHp = MaxHp;
        AttackDamage = Mathf.RoundToInt(AttackDamage * 2.5f);
        MagicDamage = Mathf.RoundToInt(MagicDamage * 2.5f);
        Armor += 15;
        MoveSpeed *= bossSpeedMultiplier;
        AttackRange *= 1.5f;

        // ปรับ detection range
        detectRange *= 2f;

        Debug.Log($"👑 {CharacterName} spawned as BOSS! HP: {MaxHp}, ATK: {AttackDamage}, MAG: {MagicDamage}");
    }

    private void InitializeBossEffects()
    {
        // เปิดเอฟเฟกต์มงกุฎ
        if (crownEffect != null)
            crownEffect.Play();

        if (bossLight != null)
        {
            bossLight.color = Color.yellow;
            bossLight.intensity = 2f;
            bossLight.range = multiAttackRadius;
        }

        // ตั้งค่าสีพิเศษสำหรับ boss
        Renderer bossRenderer = GetComponent<Renderer>();
        if (bossRenderer != null)
        {
            bossRenderer.material.color = Color.yellow;
            bossRenderer.material.SetColor("_EmissionColor", Color.yellow * 0.6f);
        }

        // เริ่มเฟส 1
        currentPhase = SlimeKingPhase.Phase1_Normal;
        NetworkedPhase = currentPhase;
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (HasStateAuthority && !IsDead)
        {
            CheckPhaseTransitions();
            UpdateImmunityStatus();
        }
    }

    private void CheckPhaseTransitions()
    {
        float healthPercentage = (float)CurrentHp / MaxHp;

        // เช็คเฟส 2 (75% HP)
        if (!hasTriggeredPhase2 && healthPercentage <= rageThreshold)
        {
            hasTriggeredPhase2 = true;
            TriggerPhaseTransition(SlimeKingPhase.Phase2_Enraged);
        }
        // เช็คเฟส 3 (25% HP)
        else if (!hasTriggeredPhase3 && healthPercentage <= desperateThreshold)
        {
            hasTriggeredPhase3 = true;
            TriggerPhaseTransition(SlimeKingPhase.Phase3_Desperate);
        }
    }

    private void TriggerPhaseTransition(SlimeKingPhase newPhase)
    {
        if (isInPhaseTransition) return;

        StartCoroutine(PerformPhaseTransition(newPhase));
    }

    private IEnumerator PerformPhaseTransition(SlimeKingPhase newPhase)
    {
        isInPhaseTransition = true;
        isImmune = true;
        immunityEndTime = Runner.SimulationTime + immunityDuration;

        Debug.Log($"👑 {CharacterName} entering {newPhase}!");

        // แสดงเอฟเฟกต์เปลี่ยนเฟส
        RPC_ShowPhaseTransition(newPhase);

        yield return new WaitForSeconds(phaseTransitionDelay);

        // เปลี่ยนเฟสและปรับ stats
        currentPhase = newPhase;
        NetworkedPhase = newPhase;
        ApplyPhaseChanges(newPhase);

        isInPhaseTransition = false;

        Debug.Log($"👑 {CharacterName} phase transition complete: {newPhase}");
    }

    private void ApplyPhaseChanges(SlimeKingPhase phase)
    {
        switch (phase)
        {
            case SlimeKingPhase.Phase2_Enraged:
                // เฟส 2: เพิ่มความเร็วและ attack speed
                MoveSpeed *= 1.3f;
                AttackSpeed *= 1.4f;
                AttackDamage = Mathf.RoundToInt(AttackDamage * 1.2f);

                // เปิดออร่าโกรธ
                if (rageAura != null)
                    rageAura.Play();

                // เปลี่ยนสี
                ChangeColor(Color.red);

                Debug.Log($"👑 Phase 2: ENRAGED! Speed↑ AttackSpeed↑ Damage↑");
                break;

            case SlimeKingPhase.Phase3_Desperate:
                // เฟส 3: เพิ่มความแรงอย่างมาก แต่ลดความเร็ว
                MoveSpeed *= 0.8f; // ช้าลง
                AttackSpeed *= 1.6f; // เร็วขึ้นมาก
                AttackDamage = Mathf.RoundToInt(AttackDamage * 1.5f);
                MagicDamage = Mathf.RoundToInt(MagicDamage * 1.5f);

                // เปิดออร่าธาตุ
                if (elementalAura != null)
                    elementalAura.Play();

                // เปลี่ยนสี
                ChangeColor(Color.magenta);

                Debug.Log($"👑 Phase 3: DESPERATE! Massive damage boost!");
                break;
        }

        // อัพเดท network stats
        ForceUpdateNetworkState();
    }

    private void ChangeColor(Color newColor)
    {
        Renderer bossRenderer = GetComponent<Renderer>();
        if (bossRenderer != null)
        {
            bossRenderer.material.color = newColor;
            bossRenderer.material.SetColor("_EmissionColor", newColor * 0.8f);
        }

        if (bossLight != null)
        {
            bossLight.color = newColor;
        }
    }

    private void UpdateImmunityStatus()
    {
        if (isImmune && Runner.SimulationTime >= immunityEndTime)
        {
            isImmune = false;
            Debug.Log($"👑 {CharacterName} immunity ended");
        }
    }

    // Boss AI Routine
    private IEnumerator BossAIRoutine()
    {
        while (!IsDead)
        {
            yield return new WaitForSeconds(0.5f);

            if (isInPhaseTransition || isImmune) continue;

            // ตัดสินใจการกระทำของ Boss
            PerformBossAction();
        }
    }

    private void PerformBossAction()
    {
        if (targetTransform == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, targetTransform.position);

        // ตัดสินใจใช้ special attack
        if (ShouldUseSpecialAttack())
        {
            PerformSpecialAttack();
        }
        // เรียก minions
        else if (ShouldSummonMinions())
        {
            SummonMinions();
        }
        // โจมตีปกติแต่แรงกว่า
        else if (distanceToTarget <= AttackRange)
        {
            PerformBossAttack();
        }
    }

    private bool ShouldUseSpecialAttack()
    {
        return Runner.SimulationTime >= nextSpecialAttackTime &&
               !IsPerformingSpecialAttack &&
               Random.Range(0f, 100f) <= GetSpecialAttackChance();
    }

    private bool ShouldSummonMinions()
    {
        return canSummonMinions &&
               Runner.SimulationTime >= nextSummonTime &&
               ActiveMinionsCount < GetMaxMinionsForPhase() &&
               Random.Range(0f, 100f) <= GetSummonChance();
    }

    private float GetSpecialAttackChance()
    {
        return currentPhase switch
        {
            SlimeKingPhase.Phase1_Normal => 25f,
            SlimeKingPhase.Phase2_Enraged => 40f,
            SlimeKingPhase.Phase3_Desperate => 60f,
            _ => 25f
        };
    }

    private float GetSummonChance()
    {
        return currentPhase switch
        {
            SlimeKingPhase.Phase1_Normal => 30f,
            SlimeKingPhase.Phase2_Enraged => 45f,
            SlimeKingPhase.Phase3_Desperate => 20f, // เฟส 3 เน้นโจมตีมากกว่า
            _ => 30f
        };
    }

    private int GetMaxMinionsForPhase()
    {
        return currentPhase switch
        {
            SlimeKingPhase.Phase1_Normal => 2,
            SlimeKingPhase.Phase2_Enraged => 4,
            SlimeKingPhase.Phase3_Desperate => 6,
            _ => 2
        };
    }

    // Special Attack System
    private void PerformSpecialAttack()
    {
        IsPerformingSpecialAttack = true;
        nextSpecialAttackTime = Runner.SimulationTime + GetSpecialAttackCooldown();

        // สุ่มประเภทของ special attack ตามเฟส
        SpecialAttackType attackType = GetRandomSpecialAttack();

        switch (attackType)
        {
            case SpecialAttackType.GroundSlam:
                StartCoroutine(PerformGroundSlam());
                break;
            case SpecialAttackType.ElementalBurst:
                StartCoroutine(PerformElementalBurst());
                break;
            case SpecialAttackType.SlimeRain:
                StartCoroutine(PerformSlimeRain());
                break;
        }
    }

    private SpecialAttackType GetRandomSpecialAttack()
    {
        var availableAttacks = new List<SpecialAttackType> { SpecialAttackType.GroundSlam };

        if (currentPhase >= SlimeKingPhase.Phase2_Enraged)
        {
            availableAttacks.Add(SpecialAttackType.ElementalBurst);
        }

        if (currentPhase == SlimeKingPhase.Phase3_Desperate)
        {
            availableAttacks.Add(SpecialAttackType.SlimeRain);
        }

        return availableAttacks[Random.Range(0, availableAttacks.Count)];
    }

    private float GetSpecialAttackCooldown()
    {
        return currentPhase switch
        {
            SlimeKingPhase.Phase1_Normal => 12f,
            SlimeKingPhase.Phase2_Enraged => 8f,
            SlimeKingPhase.Phase3_Desperate => 5f,
            _ => 12f
        };
    }

    // Ground Slam Attack
    private IEnumerator PerformGroundSlam()
    {
        Debug.Log($"👑 {CharacterName} performing GROUND SLAM!");

        RPC_ShowGroundSlamWarning(transform.position);

        // Warning phase - 2 วินาที
        yield return new WaitForSeconds(2f);

        // Execute slam
        RPC_ExecuteGroundSlam(transform.position);

        // หา targets ในรัศมี
        Collider[] targets = Physics.OverlapSphere(transform.position, multiAttackRadius, LayerMask.GetMask("Player"));

        foreach (Collider target in targets)
        {
            Hero hero = target.GetComponent<Hero>();
            if (hero != null)
            {
                int slamDamage = Mathf.RoundToInt(AttackDamage * slamDamageMultiplier);
                hero.TakeDamage(slamDamage, DamageType.Normal);

                // เพิ่ม stun effect
                hero.ApplyStatusEffect(StatusEffectType.Stun, 0, 3f);

                Debug.Log($"👑 Ground Slam hit {hero.CharacterName} for {slamDamage} damage!");
            }
        }

        IsPerformingSpecialAttack = false;
    }

    // Elemental Burst Attack
    private IEnumerator PerformElementalBurst()
    {
        Debug.Log($"👑 {CharacterName} performing ELEMENTAL BURST!");

        RPC_ShowElementalBurstStart(transform.position);

        yield return new WaitForSeconds(1.5f);

        // สร้าง elemental waves
        for (int i = 0; i < 3; i++)
        {
            CreateElementalWave(i);
            yield return new WaitForSeconds(0.5f);
        }

        IsPerformingSpecialAttack = false;
    }

    private void CreateElementalWave(int waveIndex)
    {
        float radius = (waveIndex + 1) * 3f; // รัศมีเพิ่มขึ้นทีละ wave

        RPC_ShowElementalWave(transform.position, radius);

        Collider[] targets = Physics.OverlapSphere(transform.position, radius, LayerMask.GetMask("Player"));

        foreach (Collider target in targets)
        {
            Hero hero = target.GetComponent<Hero>();
            if (hero != null)
            {
                // สุ่มธาตุ
                ApplyRandomElementalEffect(hero);
            }
        }
    }

    private void ApplyRandomElementalEffect(Hero hero)
    {
        int elementType = Random.Range(0, 3);
        int elementalDamage = Mathf.RoundToInt(MagicDamage * 0.8f);

        switch (elementType)
        {
            case 0: // Fire
                hero.TakeDamage(elementalDamage, DamageType.Magic);
                hero.ApplyStatusEffect(StatusEffectType.Burn, 8, 6f);
                Debug.Log($"🔥 Elemental Fire hit {hero.CharacterName}!");
                break;

            case 1: // Ice
                hero.TakeDamage(elementalDamage, DamageType.Magic);
                hero.ApplyStatusEffect(StatusEffectType.Freeze, 0, 4f);
                Debug.Log($"❄️ Elemental Ice hit {hero.CharacterName}!");
                break;

            case 2: // Poison
                hero.TakeDamage(elementalDamage, DamageType.Magic);
                hero.ApplyStatusEffect(StatusEffectType.Poison, 6, 8f);
                Debug.Log($"☠️ Elemental Poison hit {hero.CharacterName}!");
                break;
        }
    }

    // Slime Rain Attack (Phase 3 only)
    private IEnumerator PerformSlimeRain()
    {
        Debug.Log($"👑 {CharacterName} performing SLIME RAIN!");

        RPC_ShowSlimeRainStart();

        // สร้าง slime ตกจากฟ้า 8 ครั้ง
        for (int i = 0; i < 8; i++)
        {
            Vector3 randomPos = transform.position + Random.insideUnitSphere * 10f;
            randomPos.y = transform.position.y + 15f; // สูงขึ้น

            CreateFallingSlime(randomPos);

            yield return new WaitForSeconds(0.3f);
        }

        yield return new WaitForSeconds(3f); // รอให้ slime ตกหมด

        IsPerformingSpecialAttack = false;
    }

    private void CreateFallingSlime(Vector3 startPos)
    {
        // สร้าง slime projectile
        GameObject fallingSlime = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fallingSlime.transform.position = startPos;
        fallingSlime.transform.localScale = Vector3.one * 0.8f;
        fallingSlime.GetComponent<Renderer>().material.color = Color.green;

        // เพิ่ม Rigidbody เพื่อให้ตก
        Rigidbody slimeRb = fallingSlime.AddComponent<Rigidbody>();
        slimeRb.useGravity = true;

        // เพิ่ม script จัดการการกระทบ
        FallingSlimeProjectile projectile = fallingSlime.AddComponent<FallingSlimeProjectile>();
        projectile.Initialize(MagicDamage);

        // ทำลายหลัง 10 วินาทีถ้ายังไม่กระทบ
        Destroy(fallingSlime, 10f);
    }

    // Minion Summoning System
    private void SummonMinions()
    {
        if (minionPrefabs == null || minionPrefabs.Length == 0) return;

        nextSummonTime = Runner.SimulationTime + summonCooldown;

        int minionsToSummon = GetMinionsToSummon();

        for (int i = 0; i < minionsToSummon; i++)
        {
            SummonSingleMinion();
        }

        Debug.Log($"👑 {CharacterName} summoned {minionsToSummon} minions!");
        RPC_ShowSummonEffect(transform.position);
    }

    private int GetMinionsToSummon()
    {
        int maxNew = GetMaxMinionsForPhase() - ActiveMinionsCount;
        return Random.Range(1, Mathf.Min(3, maxNew + 1));
    }

    private void SummonSingleMinion()
    {
        // สุ่มประเภท minion
        GameObject minionPrefab = minionPrefabs[Random.Range(0, minionPrefabs.Length)];

        // สุ่มตำแหน่งรอบๆ boss
        Vector2 offset2D = Random.insideUnitCircle.normalized * 5f;
        Vector3 summonPos = transform.position + new Vector3(offset2D.x, 0f, offset2D.y);
        summonPos.y = transform.position.y;

        // Spawn minion
        if (HasStateAuthority)
        {
            GameObject minion = Instantiate(minionPrefab, summonPos, Quaternion.identity);
            NetworkEnemy minionEnemy = minion.GetComponent<NetworkEnemy>();

            if (minionEnemy != null)
            {
                // ทำให้ minion มี stats ต่ำกว่าปกติ
                minionEnemy.MaxHp = Mathf.RoundToInt(minionEnemy.MaxHp * 0.7f);
                minionEnemy.CurrentHp = minionEnemy.MaxHp;
                minionEnemy.transform.localScale *= 0.8f; // เล็กกว่าปกติ

                activeMinions.Add(minion);
                ActiveMinionsCount++;

                // เพิ่ม callback เมื่อ minion ตาย
                MinionDeathTracker tracker = minion.AddComponent<MinionDeathTracker>();
                tracker.Initialize(this);
            }
        }
    }

    public void OnMinionDestroyed()
    {
        ActiveMinionsCount = Mathf.Max(0, ActiveMinionsCount - 1);

        // ทำความสะอาด list
        activeMinions.RemoveAll(minion => minion == null);

        Debug.Log($"👑 Minion destroyed. Remaining: {ActiveMinionsCount}");
    }

    // Override การโจมตีปกติ - Boss Attack
    private void PerformBossAttack()
    {
        if (targetTransform == null || Runner.SimulationTime < nextAttackTime) return;

        float effectiveAttackSpeed = GetEffectiveAttackSpeed();
        float finalAttackCooldown = AttackCooldown / Mathf.Max(0.1f, effectiveAttackSpeed);
        nextAttackTime = Runner.SimulationTime + finalAttackCooldown;

        // Boss มีโอกาสโจมตีหลายเป้าหมาย
        if (ShouldPerformMultiAttack())
        {
            PerformMultiTargetAttack();
        }
        else
        {
            PerformSingleTargetAttack();
        }
    }

    private bool ShouldPerformMultiAttack()
    {
        return currentPhase >= SlimeKingPhase.Phase2_Enraged &&
               Random.Range(0f, 100f) <= 50f;
    }

    private void PerformMultiTargetAttack()
    {
        // หาเป้าหมายทั้งหมดในรัศมี
        Collider[] targets = Physics.OverlapSphere(transform.position, multiAttackRadius, LayerMask.GetMask("Player"));
        List<Hero> validTargets = new List<Hero>();

        foreach (Collider target in targets)
        {
            Hero hero = target.GetComponent<Hero>();
            if (hero != null)
            {
                validTargets.Add(hero);
            }
        }

        // จำกัดจำนวนเป้าหมาย
        int targetCount = Mathf.Min(validTargets.Count, maxMultiTargets);

        if (targetCount > 0)
        {
            Debug.Log($"👑 {CharacterName} multi-attack hitting {targetCount} targets!");
            RPC_BossMultiAttack(validTargets.Take(targetCount).ToArray());
        }
    }

    private void PerformSingleTargetAttack()
    {
        Hero targetHero = targetTransform.GetComponent<Hero>();
        if (targetHero != null)
        {
            RPC_BossSingleAttack(targetHero.Object.InputAuthority);
        }
    }

    // Override การรับดาเมจ - Boss มี immunity
    public override void TakeDamageFromAttacker(int physicalDamage, int magicDamage, Character attacker, DamageType damageType = DamageType.Normal)
    {
        if (isImmune)
        {
            Debug.Log($"👑 {CharacterName} is IMMUNE to damage!");
            RPC_ShowImmunityEffect(transform.position);
            return;
        }

        // Boss รับดาเมจลดลง 30%
        int reducedPhysical = Mathf.RoundToInt(physicalDamage * 0.7f);
        int reducedMagic = Mathf.RoundToInt(magicDamage * 0.7f);

        base.TakeDamageFromAttacker(reducedPhysical, reducedMagic, attacker, damageType);

        // Boss มีโอกาส counter-attack
        if (Random.Range(0f, 100f) <= GetCounterAttackChance())
        {
            PerformCounterAttack(attacker);
        }
    }

    private float GetCounterAttackChance()
    {
        return currentPhase switch
        {
            SlimeKingPhase.Phase1_Normal => 15f,
            SlimeKingPhase.Phase2_Enraged => 25f,
            SlimeKingPhase.Phase3_Desperate => 40f,
            _ => 15f
        };
    }

    private void PerformCounterAttack(Character attacker)
    {
        if (attacker is Hero hero)
        {
            int counterDamage = Mathf.RoundToInt(AttackDamage * 0.6f);
            hero.TakeDamage(counterDamage, DamageType.Normal);

            Debug.Log($"👑 {CharacterName} counter-attacked {hero.CharacterName} for {counterDamage}!");
            RPC_ShowCounterAttackEffect(hero.transform.position);
        }
    }

    // Override การตาย - Boss Death
    protected override void RPC_OnDeath()
    {
        Debug.Log($"👑 THE SLIME KING HAS FALLEN!");

        if (HasStateAuthority)
        {
            PerformBossDeath();
        }

        // ทำลาย minions ทั้งหมด
        foreach (GameObject minion in activeMinions)
        {
            if (minion != null)
            {
                Destroy(minion);
            }
        }
        activeMinions.Clear();
        ActiveMinionsCount = 0;

        base.RPC_OnDeath();
    }

    private void PerformBossDeath()
    {
        // Boss death explosion ใหญ่มาก
        Collider[] targets = Physics.OverlapSphere(transform.position, multiAttackRadius * 2f, LayerMask.GetMask("Player"));

        foreach (Collider target in targets)
        {
            Hero hero = target.GetComponent<Hero>();
            if (hero != null)
            {
                int deathDamage = Mathf.RoundToInt(AttackDamage * 2f);
                hero.TakeDamage(deathDamage, DamageType.Magic);

                Debug.Log($"💀👑 Boss death explosion hit {hero.CharacterName}!");
            }
        }

        // สร้าง special drops
        CreateBossDrops();

        RPC_ShowBossDeathExplosion(transform.position);
    }

    private void CreateBossDrops()
    {
        // Boss drops special items และ currency มากมาย
        

        if (ItemDrop != null)
        {
            // Boss มีโอกาส drop rare items สูง
            ItemDrop.TriggerItemDrops();
        }

        // ให้ exp มากพิเศษ
        DropBossExpToNearbyHeroes();
    }

    private void DropBossExpToNearbyHeroes()
    {
        Collider[] heroColliders = Physics.OverlapSphere(transform.position, 20f, LayerMask.GetMask("Player"));
        List<Character> nearbyCharacters = new List<Character>();

        foreach (Collider col in heroColliders)
        {
            Character character = col.GetComponent<Character>();
            if (character != null && character.IsSpawned)
            {
                nearbyCharacters.Add(character);
            }
        }

        if (nearbyCharacters.Count > 0)
        {
            // Boss ให้ exp มาก
            int bossExp = 500; // Base exp สูง
            int expPerCharacter = Mathf.Max(100, bossExp / nearbyCharacters.Count);

            foreach (Character character in nearbyCharacters)
            {
                character.GainExp(expPerCharacter);
                Debug.Log($"💰👑 Boss {name} dropped {expPerCharacter} exp to {character.CharacterName}");
            }
        }
    }

    // RPC Methods สำหรับ Visual Effects
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowPhaseTransition(SlimeKingPhase newPhase)
    {
        if (phaseTransitionFX != null)
        {
            phaseTransitionFX.Play();
        }

        // แสงพิเศษสำหรับเปลี่ยนเฟส
        StartCoroutine(PhaseTransitionLightEffect(newPhase));
    }

    private IEnumerator PhaseTransitionLightEffect(SlimeKingPhase phase)
    {
        if (bossLight == null) yield break;

        Color phaseColor = phase switch
        {
            SlimeKingPhase.Phase2_Enraged => Color.red,
            SlimeKingPhase.Phase3_Desperate => Color.magenta,
            _ => Color.yellow
        };

        float originalIntensity = bossLight.intensity;

        // Flash effect
        for (int i = 0; i < 5; i++)
        {
            bossLight.intensity = originalIntensity * 3f;
            bossLight.color = Color.white;
            yield return new WaitForSeconds(0.1f);

            bossLight.intensity = originalIntensity;
            bossLight.color = phaseColor;
            yield return new WaitForSeconds(0.1f);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BossMultiAttack(Hero[] targets)
    {
        foreach (Hero hero in targets)
        {
            if (hero != null)
            {
                // ดาเมจลดลงเมื่อโจมตีหลายเป้าหมาย
                int multiAttackDamage = Mathf.RoundToInt(AttackDamage * 0.8f);
                int multiMagicDamage = Mathf.RoundToInt(MagicDamage * 0.8f);

                hero.TakeDamageFromAttacker(multiAttackDamage, multiMagicDamage, this, DamageType.Normal);

                // แสดงเอฟเฟกต์
                RPC_ShowBossAttackEffect(hero.transform.position);
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BossSingleAttack(PlayerRef targetPlayer)
    {
        Hero targetHero = null;
        Hero[] heroes = FindObjectsOfType<Hero>();

        foreach (Hero hero in heroes)
        {
            if (hero.Object != null && hero.Object.InputAuthority == targetPlayer)
            {
                targetHero = hero;
                break;
            }
        }

        if (targetHero != null)
        {
            // Boss attack ปกติแต่แรงกว่า
            int bossAttackDamage = Mathf.RoundToInt(AttackDamage * GetPhaseDamageMultiplier());
            int bossMagicDamage = Mathf.RoundToInt(MagicDamage * GetPhaseDamageMultiplier());

            targetHero.TakeDamageFromAttacker(bossAttackDamage, bossMagicDamage, this, DamageType.Normal);

            Debug.Log($"👑 {CharacterName} boss attack hit {targetHero.CharacterName}!");
            RPC_ShowBossAttackEffect(targetHero.transform.position);

            // Boss มีโอกาสใส่ random status effect
            if (Random.Range(0f, 100f) <= 30f)
            {
                ApplyRandomStatusEffect(targetHero);
            }
        }
    }

    private float GetPhaseDamageMultiplier()
    {
        return currentPhase switch
        {
            SlimeKingPhase.Phase1_Normal => 1.0f,
            SlimeKingPhase.Phase2_Enraged => 1.2f,
            SlimeKingPhase.Phase3_Desperate => 1.5f,
            _ => 1.0f
        };
    }

    private void ApplyRandomStatusEffect(Hero target)
    {
        var possibleEffects = new[]
        {
            StatusEffectType.Stun,
            StatusEffectType.Weakness,
            StatusEffectType.ArmorBreak,
            StatusEffectType.Poison,
            StatusEffectType.Burn
        };

        StatusEffectType selectedEffect = possibleEffects[Random.Range(0, possibleEffects.Length)];

        switch (selectedEffect)
        {
            case StatusEffectType.Stun:
                target.ApplyStatusEffect(selectedEffect, 0, 2f);
                break;
            case StatusEffectType.Weakness:
                target.ApplyStatusEffect(selectedEffect, 0, 8f, 0.3f);
                break;
            case StatusEffectType.ArmorBreak:
                target.ApplyStatusEffect(selectedEffect, 0, 10f, 0.4f);
                break;
            case StatusEffectType.Poison:
                target.ApplyStatusEffect(selectedEffect, 8, 8f);
                break;
            case StatusEffectType.Burn:
                target.ApplyStatusEffect(selectedEffect, 10, 6f);
                break;
        }

        Debug.Log($"👑 Boss applied {selectedEffect} to {target.CharacterName}!");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowGroundSlamWarning(Vector3 position)
    {
        // แสดงเอฟเฟกต์เตือนก่อน ground slam
        GameObject warningFX = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        warningFX.transform.position = position;
        warningFX.transform.localScale = new Vector3(multiAttackRadius * 2f, 0.1f, multiAttackRadius * 2f);
        warningFX.GetComponent<Renderer>().material.color = Color.red;

        // กระพริบ
        StartCoroutine(FlashWarning(warningFX));

        Destroy(warningFX, 2f);
    }

    private IEnumerator FlashWarning(GameObject warning)
    {
        Renderer renderer = warning.GetComponent<Renderer>();

        for (int i = 0; i < 10; i++)
        {
            renderer.enabled = !renderer.enabled;
            yield return new WaitForSeconds(0.2f);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ExecuteGroundSlam(Vector3 position)
    {
        // เอฟเฟกต์การกระแทกพื้น
        if (phaseTransitionFX != null)
        {
            GameObject slamFX = Instantiate(phaseTransitionFX.gameObject, position, Quaternion.identity);
            slamFX.transform.localScale *= 3f;
            Destroy(slamFX, 3f);
        }

        // สั่นกล้อง
        StartCoroutine(CameraShake());
    }

    private IEnumerator CameraShake()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) yield break;

        Vector3 originalPos = mainCam.transform.position;

        for (float t = 0; t < 1f; t += Time.deltaTime)
        {
            Vector3 shakeOffset = Random.insideUnitSphere * 0.5f;
            mainCam.transform.position = originalPos + shakeOffset;
            yield return null;
        }

        mainCam.transform.position = originalPos;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowElementalBurstStart(Vector3 position)
    {
        if (elementalAura != null)
        {
            GameObject burstFX = Instantiate(elementalAura.gameObject, position, Quaternion.identity);
            burstFX.transform.localScale *= 2f;
            Destroy(burstFX, 5f);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowElementalWave(Vector3 center, float radius)
    {
        // สร้างคลื่นธาตุ
        GameObject wave = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wave.transform.position = center;
        wave.transform.localScale = new Vector3(radius * 2f, 0.2f, radius * 2f);

        // สีสุ่มตามธาตุ
        Color[] elementColors = { Color.red, Color.cyan, Color.green };
        wave.GetComponent<Renderer>().material.color = elementColors[Random.Range(0, elementColors.Length)];

        // ขยายขนาด
        StartCoroutine(ExpandWave(wave, radius));

        Destroy(wave, 2f);
    }

    private IEnumerator ExpandWave(GameObject wave, float targetRadius)
    {
        Vector3 startScale = Vector3.one * 0.1f;
        Vector3 endScale = new Vector3(targetRadius * 2f, 0.2f, targetRadius * 2f);

        for (float t = 0; t < 1f; t += Time.deltaTime * 2f)
        {
            if (wave != null)
            {
                wave.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            }
            yield return null;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowSlimeRainStart()
    {
        // เอฟเฟกต์ฟ้าร้อง
        if (bossLight != null)
        {
            StartCoroutine(LightningEffect());
        }
    }

    private IEnumerator LightningEffect()
    {
        Color originalColor = bossLight.color;
        float originalIntensity = bossLight.intensity;

        for (int i = 0; i < 3; i++)
        {
            bossLight.color = Color.white;
            bossLight.intensity = originalIntensity * 5f;
            yield return new WaitForSeconds(0.1f);

            bossLight.color = originalColor;
            bossLight.intensity = originalIntensity;
            yield return new WaitForSeconds(0.3f);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowSummonEffect(Vector3 position)
    {
        if (crownEffect != null)
        {
            GameObject summonFX = Instantiate(crownEffect.gameObject, position, Quaternion.identity);
            Destroy(summonFX, 3f);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowBossAttackEffect(Vector3 position)
    {
        // เอฟเฟกต์การโจมตีของ boss
        GameObject attackFX = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        attackFX.transform.position = position + Vector3.up * 1f;
        attackFX.transform.localScale = Vector3.one * 2f;
        attackFX.GetComponent<Renderer>().material.color = Color.yellow;

        Destroy(attackFX, 1f);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowCounterAttackEffect(Vector3 position)
    {
        // เอฟเฟกต์ counter attack
        GameObject counterFX = GameObject.CreatePrimitive(PrimitiveType.Cube);
        counterFX.transform.position = position + Vector3.up * 2f;
        counterFX.transform.localScale = Vector3.one * 1.5f;
        counterFX.GetComponent<Renderer>().material.color = Color.red;

        Destroy(counterFX, 0.8f);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowImmunityEffect(Vector3 position)
    {
        // เอฟเฟกต์ immunity
        GameObject immuneFX = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        immuneFX.transform.position = position + Vector3.up * 2f;
        immuneFX.transform.localScale = Vector3.one * 3f;
        immuneFX.GetComponent<Renderer>().material.color = Color.white;

        // ทำให้โปร่งใส
        Material mat = immuneFX.GetComponent<Renderer>().material;
        mat.color = new Color(1, 1, 1, 0.3f);

        Destroy(immuneFX, 1f);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowBossDeathExplosion(Vector3 position)
    {
        // เอฟเฟกต์การตายของ boss ที่ยิ่งใหญ่
        if (phaseTransitionFX != null)
        {
            for (int i = 0; i < 5; i++)
            {
                Vector3 explosionPos = position + Random.insideUnitSphere * 5f;
                GameObject explosionFX = Instantiate(phaseTransitionFX.gameObject, explosionPos, Quaternion.identity);
                explosionFX.transform.localScale *= 4f;
                Destroy(explosionFX, 5f);
            }
        }

        // แสงระเบิดใหญ่
        GameObject lightObj = new GameObject("BossDeathLight");
        lightObj.transform.position = position;
        Light deathLight = lightObj.AddComponent<Light>();
        deathLight.color = Color.white;
        deathLight.intensity = 10f;
        deathLight.range = 50f;

        StartCoroutine(FadeBossDeathLight(deathLight, lightObj));
    }

    private IEnumerator FadeBossDeathLight(Light light, GameObject lightObj)
    {
        float duration = 5f;
        float startIntensity = light.intensity;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            if (light != null)
            {
                light.intensity = Mathf.Lerp(startIntensity, 0f, t / duration);
            }
            yield return null;
        }

        if (lightObj != null)
            Destroy(lightObj);
    }

    private void ShowBossHealthBar()
    {
        // แสดง Boss Health Bar (ต้องมี UI system รองรับ)
        Debug.Log($"👑 Showing Boss Health Bar for {CharacterName}");
        // TODO: Implement Boss Health Bar UI
    }

    protected override void OnDestroy()
    {
        // ทำลาย minions ทั้งหมด
        foreach (GameObject minion in activeMinions)
        {
            if (minion != null)
                Destroy(minion);
        }
        activeMinions.Clear();

        if (bossAICoroutine != null)
            StopCoroutine(bossAICoroutine);

        base.OnDestroy();
    }

    // Debug Gizmos
    private void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // วาดรัศมีโจมตีพื้นที่
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, multiAttackRadius);

        // วาดรัศมี ground slam
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, multiAttackRadius);
    }
}

// ===== Supporting Classes =====

public enum SpecialAttackType
{
    GroundSlam,
    ElementalBurst,
    SlimeRain
}

// Component สำหรับติดตาม minion ที่ตาย
public class MinionDeathTracker : MonoBehaviour
{
    private SlimeKingBoss slimeKing;

    public void Initialize(SlimeKingBoss king)
    {
        slimeKing = king;
    }

    private void OnDestroy()
    {
        if (slimeKing != null)
        {
            slimeKing.OnMinionDestroyed();
        }
    }
}

// Component สำหรับ Falling Slime Projectile
public class FallingSlimeProjectile : MonoBehaviour
{
    private int damage;
    private bool hasHit = false;

    public void Initialize(int projectileDamage)
    {
        damage = projectileDamage;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;

        // ตรวจสอบว่าชนผู้เล่นหรือพื้น
        Hero hero = collision.gameObject.GetComponent<Hero>();
        if (hero != null)
        {
            hero.TakeDamage(damage, DamageType.Magic);
            hero.ApplyStatusEffect(StatusEffectType.Poison, 5, 6f);

            Debug.Log($"🌧️ Falling Slime hit {hero.CharacterName} for {damage} damage!");
        }

        // สร้างเอฟเฟกต์การกระเด็น
        CreateImpactEffect();

        // ทำลายตัวเอง
        Destroy(gameObject);
    }

    private void CreateImpactEffect()
    {
        // สร้างเอฟเฟกต์การกระแทก
        GameObject impactFX = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        impactFX.transform.position = transform.position;
        impactFX.transform.localScale = Vector3.one * 2f;
        impactFX.GetComponent<Renderer>().material.color = Color.green;

        Destroy(impactFX, 1f);

        // สร้างสเศษกระเด็น
        for (int i = 0; i < 3; i++)
        {
            GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shard.transform.position = transform.position + Random.insideUnitSphere * 0.5f;
            shard.transform.localScale = Vector3.one * 0.1f;
            shard.GetComponent<Renderer>().material.color = Color.green;

            Rigidbody shardRb = shard.AddComponent<Rigidbody>();
            shardRb.AddForce(Random.insideUnitSphere * 3f, ForceMode.Impulse);

            Destroy(shard, 2f);
        }
    }
}