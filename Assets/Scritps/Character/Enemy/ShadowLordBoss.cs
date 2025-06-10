using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public enum BossPhase
{
    Phase1_Normal,      // เฟสปกติ - โจมตีพื้นฐาน
    Phase2_Enraged,     // เฟส 2 - เร็วขึ้น, เรียก minions
    Phase3_Desperate,   // เฟส 3 - AOE attacks, teleport
    Phase4_Final        // เฟสสุดท้าย - ultimate abilities
}

public enum BossAttackType
{
    BasicAttack,
    ChargeAttack,
    AOEBlast,
    ShadowStrike,
    MinionSummon,
    TeleportStrike,
    ShadowWave,
    UltimateDestruction
}

public class ShadowLordBoss : NetworkEnemy
{
    [Header("🔥 Boss Settings")]
    [SerializeField] private BossPhase currentPhase = BossPhase.Phase1_Normal;
    [SerializeField] private float phaseTransitionDuration = 2f;
    [SerializeField] private bool isInPhaseTransition = false;

    [Header("⚡ Boss Abilities")]
    [SerializeField] private float chargeAttackSpeed = 8f;
    [SerializeField] private float chargeAttackDamageMultiplier = 2f;
    [SerializeField] private float aoeBlastRadius = 5f;
    [SerializeField] private int aoeBlastDamage = 30;
    [SerializeField] private float teleportRange = 8f;
    [SerializeField] private int shadowStrikeDamage = 25;

    [Header("👾 Minion System")]
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private int maxMinions = 3;
    [SerializeField] private float minionSpawnRadius = 4f;
    [SerializeField] private List<NetworkObject> activeMinions = new List<NetworkObject>();

    [Header("🌪️ Ultimate Abilities")]
    [SerializeField] private float shadowWaveSpeed = 6f;
    [SerializeField] private int shadowWaveDamage = 20;
    [SerializeField] private float ultimateChannelTime = 3f;
    [SerializeField] private int ultimateDamage = 100;
    [SerializeField] private float ultimateRadius = 10f;

    [Header("🛡️ Boss Protection")]
    [SerializeField] private bool hasShield = false;
    [SerializeField] private float shieldDuration = 5f;
    [SerializeField] private int shieldHealth = 100;

    [Header("🎯 Boss Combat")]
    [SerializeField] private float nextSpecialAttackTime = 0f;
    [SerializeField] private float specialAttackCooldown = 3f;
    [SerializeField] private BossAttackType currentAttackType;
    [SerializeField] private bool isPerformingSpecialAttack = false;

    [Header("📊 Boss Stats")]
    [SerializeField] private int originalMaxHp;
    [SerializeField] private float phase2HpThreshold = 0.75f;
    [SerializeField] private float phase3HpThreshold = 0.5f;
    [SerializeField] private float phase4HpThreshold = 0.25f;

    // Network Properties
    [Networked] public BossPhase NetworkedCurrentPhase { get; set; }
    [Networked] public bool IsChannelingUltimate { get; set; }
    [Networked] public float UltimateChannelTimer { get; set; }
    [Networked] public bool HasActiveShield { get; set; }
    [Networked] public int CurrentShieldHealth { get; set; }

    protected override void Start()
    {
        base.Start();

        // บอสจะมี stats ที่แข็งแกร่งกว่าศัตรูทั่วไป
        originalMaxHp = MaxHp;

        // ตั้งค่า Boss เฉพาะ
        detectRange = 15f; // มองเห็นไกลกว่า
        AttackRange = 3f;
        MoveSpeed *= 0.8f; // เคลื่อนที่ช้ากว่าแต่แข็งแกร่งกว่า

        if (HasStateAuthority)
        {
            currentPhase = BossPhase.Phase1_Normal;
            NetworkedCurrentPhase = currentPhase;

            Debug.Log($"🔥 Shadow Lord Boss spawned with {MaxHp} HP!");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!IsSpawned || IsDead) return;

        if (HasStateAuthority)
        {
            // เช็คการเปลี่ยนเฟส
            CheckPhaseTransition();

            // ระบบ AI ของบอส
            if (!isInPhaseTransition)
            {
                BossAIUpdate();
            }

            // อัพเดท Ultimate channeling
            if (IsChannelingUltimate)
            {
                UltimateChannelTimer += Runner.DeltaTime;
                if (UltimateChannelTimer >= ultimateChannelTime)
                {
                    ExecuteUltimateAttack();
                }
            }

            // ทำความสะอาด minions ที่ตาย
            CleanupDeadMinions();
        }

        // เรียก base class
        base.FixedUpdateNetwork();
    }

    #region Phase Management
    private void CheckPhaseTransition()
    {
        if (isInPhaseTransition) return;

        float hpPercentage = (float)CurrentHp / originalMaxHp;
        BossPhase newPhase = currentPhase;

        if (hpPercentage <= phase4HpThreshold && currentPhase != BossPhase.Phase4_Final)
        {
            newPhase = BossPhase.Phase4_Final;
        }
        else if (hpPercentage <= phase3HpThreshold && currentPhase != BossPhase.Phase3_Desperate)
        {
            newPhase = BossPhase.Phase3_Desperate;
        }
        else if (hpPercentage <= phase2HpThreshold && currentPhase != BossPhase.Phase2_Enraged)
        {
            newPhase = BossPhase.Phase2_Enraged;
        }

        if (newPhase != currentPhase)
        {
            StartPhaseTransition(newPhase);
        }
    }

    private void StartPhaseTransition(BossPhase newPhase)
    {
        isInPhaseTransition = true;
        currentPhase = newPhase;
        NetworkedCurrentPhase = newPhase;

        RPC_OnPhaseTransition(newPhase);

        // ทำการเปลี่ยนแปลง stats ตามเฟส
        ApplyPhaseStats(newPhase);

        // หยุดการโจมตีชั่วคราว
        StartCoroutine(PhaseTransitionCoroutine());
    }

    private IEnumerator PhaseTransitionCoroutine()
    {
        yield return new WaitForSeconds(phaseTransitionDuration);
        isInPhaseTransition = false;

        // ใช้ ability พิเศษหลังเปลี่ยนเฟส
        if (currentPhase == BossPhase.Phase2_Enraged)
        {
            SummonMinions();
        }
        else if (currentPhase == BossPhase.Phase3_Desperate)
        {
            ActivateShield();
        }
    }

    private void ApplyPhaseStats(BossPhase phase)
    {
        switch (phase)
        {
            case BossPhase.Phase2_Enraged:
                MoveSpeed *= 1.3f;
                AttackCooldown *= 0.8f;
                specialAttackCooldown *= 0.7f;
                break;

            case BossPhase.Phase3_Desperate:
                MoveSpeed *= 1.5f;
                AttackCooldown *= 0.6f;
                specialAttackCooldown *= 0.5f;
                CriticalChance += 15f;
                break;

            case BossPhase.Phase4_Final:
                MoveSpeed *= 1.8f;
                AttackCooldown *= 0.4f;
                specialAttackCooldown *= 0.3f;
                CriticalChance += 25f;
                AttackDamage = Mathf.RoundToInt(AttackDamage * 1.5f);
                break;
        }
    }
    #endregion

    #region Boss AI
    private void BossAIUpdate()
    {
        // ใช้ระบบ AI พื้นฐานจาก NetworkEnemy
        FindNearestPlayer();

        if (targetTransform != null)
        {
            // ตัดสินใจใช้ special attack
            if (Runner.SimulationTime >= nextSpecialAttackTime && !isPerformingSpecialAttack)
            {
                ChooseAndExecuteSpecialAttack();
            }
            else
            {
                // ใช้การเคลื่อนที่ปกติ
                BossMovement();
            }

            // โจมตีปกติ
            TryBasicAttack();
        }
    }

    private void BossMovement()
    {
        if (isPerformingSpecialAttack) return;

        float distanceToPlayer = Vector3.Distance(transform.position, targetTransform.position);
        Vector3 directionToPlayer = (targetTransform.position - transform.position).normalized;
        Vector3 moveDirection = Vector3.zero;

        // การเคลื่อนที่ของบอสแตกต่างตามเฟส
        switch (currentPhase)
        {
            case BossPhase.Phase1_Normal:
                // เคลื่อนที่ปกติ
                if (distanceToPlayer > AttackRange)
                {
                    moveDirection = directionToPlayer;
                }
                break;

            case BossPhase.Phase2_Enraged:
            case BossPhase.Phase3_Desperate:
                // เคลื่อนที่แบบ aggressive
                if (distanceToPlayer > AttackRange * 0.8f)
                {
                    moveDirection = directionToPlayer;
                }
                else if (distanceToPlayer < AttackRange * 0.5f)
                {
                    // ถอยออกเล็กน้อย
                    moveDirection = -directionToPlayer * 0.5f;
                }
                break;

            case BossPhase.Phase4_Final:
                // เคลื่อนที่แบบคาดเดาไม่ได้
                if (distanceToPlayer > AttackRange)
                {
                    moveDirection = directionToPlayer;
                }
                // เพิ่มการเคลื่อนที่ด้านข้าง
                Vector3 sideDirection = new Vector3(-directionToPlayer.z, 0, directionToPlayer.x);
                moveDirection += sideDirection * 0.3f * Mathf.Sin(Runner.SimulationTime * 2f);
                break;
        }

        // ใช้ Rigidbody เคลื่อนที่
        if (moveDirection.magnitude > 0.1f && rb != null)
        {
            moveDirection.y = 0;
            moveDirection.Normalize();

            Vector3 newPosition = transform.position + moveDirection * MoveSpeed * Runner.DeltaTime;
            rb.MovePosition(newPosition);

            // Flip character
            FlipCharacterTowardsMovement(moveDirection);
        }
    }

    private void ChooseAndExecuteSpecialAttack()
    {
        if (targetTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, targetTransform.position);
        BossAttackType attackType = BossAttackType.BasicAttack;

        // เลือก attack ตามเฟสและระยะทาง
        switch (currentPhase)
        {
            case BossPhase.Phase1_Normal:
                attackType = Random.Range(0f, 1f) < 0.7f ? BossAttackType.ChargeAttack : BossAttackType.ShadowStrike;
                break;

            case BossPhase.Phase2_Enraged:
                if (activeMinions.Count < maxMinions && Random.Range(0f, 1f) < 0.3f)
                {
                    attackType = BossAttackType.MinionSummon;
                }
                else if (distanceToPlayer <= aoeBlastRadius && Random.Range(0f, 1f) < 0.4f)
                {
                    attackType = BossAttackType.AOEBlast;
                }
                else
                {
                    attackType = Random.Range(0f, 1f) < 0.6f ? BossAttackType.ChargeAttack : BossAttackType.ShadowStrike;
                }
                break;

            case BossPhase.Phase3_Desperate:
                if (Random.Range(0f, 1f) < 0.3f)
                {
                    attackType = BossAttackType.TeleportStrike;
                }
                else if (Random.Range(0f, 1f) < 0.4f)
                {
                    attackType = BossAttackType.ShadowWave;
                }
                else
                {
                    attackType = BossAttackType.AOEBlast;
                }
                break;

            case BossPhase.Phase4_Final:
                if (Random.Range(0f, 1f) < 0.2f)
                {
                    attackType = BossAttackType.UltimateDestruction;
                }
                else if (Random.Range(0f, 1f) < 0.4f)
                {
                    attackType = BossAttackType.TeleportStrike;
                }
                else
                {
                    attackType = BossAttackType.ShadowWave;
                }
                break;
        }

        ExecuteSpecialAttack(attackType);
    }
    #endregion

    #region Special Attacks
    private void ExecuteSpecialAttack(BossAttackType attackType)
    {
        currentAttackType = attackType;
        isPerformingSpecialAttack = true;
        nextSpecialAttackTime = Runner.SimulationTime + specialAttackCooldown;

        switch (attackType)
        {
            case BossAttackType.ChargeAttack:
                StartCoroutine(ChargeAttackCoroutine());
                break;
            case BossAttackType.AOEBlast:
                StartCoroutine(AOEBlastCoroutine());
                break;
            case BossAttackType.ShadowStrike:
                StartCoroutine(ShadowStrikeCoroutine());
                break;
            case BossAttackType.MinionSummon:
                SummonMinions();
                break;
            case BossAttackType.TeleportStrike:
                StartCoroutine(TeleportStrikeCoroutine());
                break;
            case BossAttackType.ShadowWave:
                StartCoroutine(ShadowWaveCoroutine());
                break;
            case BossAttackType.UltimateDestruction:
                StartChannelUltimate();
                break;
        }

        RPC_OnSpecialAttack(attackType);
    }

    private IEnumerator ChargeAttackCoroutine()
    {
        if (targetTransform == null) yield break;

        Vector3 startPos = transform.position;
        Vector3 targetPos = targetTransform.position;
        Vector3 chargeDirection = (targetPos - startPos).normalized;

        // Charge towards player
        float chargeTime = 0.8f;
        float elapsed = 0f;

        while (elapsed < chargeTime)
        {
            if (rb != null)
            {
                Vector3 newPos = transform.position + chargeDirection * chargeAttackSpeed * Runner.DeltaTime;
                rb.MovePosition(newPos);
            }

            elapsed += Runner.DeltaTime;
            yield return null;
        }

        // Deal damage to nearby players
        Collider[] players = Physics.OverlapSphere(transform.position, AttackRange * 1.5f, LayerMask.GetMask("Player"));
        foreach (Collider player in players)
        {
            Hero hero = player.GetComponent<Hero>();
            if (hero != null)
            {
                int chargeDamage = Mathf.RoundToInt(AttackDamage * chargeAttackDamageMultiplier);
                RPC_DealDamageToPlayer(hero.Object, chargeDamage, true);
            }
        }

        yield return new WaitForSeconds(0.5f);
        isPerformingSpecialAttack = false;
    }

    private IEnumerator AOEBlastCoroutine()
    {
        // Warning phase
        RPC_ShowAOEWarning(transform.position, aoeBlastRadius);
        yield return new WaitForSeconds(1.5f);

        // Execute blast
        Collider[] players = Physics.OverlapSphere(transform.position, aoeBlastRadius, LayerMask.GetMask("Player"));
        foreach (Collider player in players)
        {
            Hero hero = player.GetComponent<Hero>();
            if (hero != null)
            {
                RPC_DealDamageToPlayer(hero.Object, aoeBlastDamage, false);

                // เพิ่ม status effect
                hero.ApplyStatusEffect(StatusEffectType.Burn, 5, 6f);
            }
        }

        RPC_ExecuteAOEBlast(transform.position, aoeBlastRadius);

        yield return new WaitForSeconds(0.5f);
        isPerformingSpecialAttack = false;
    }

    private IEnumerator ShadowStrikeCoroutine()
    {
        if (targetTransform == null) yield break;

        // เป็นล่องหน
        RPC_BecomeInvisible(true);
        yield return new WaitForSeconds(0.8f);

        // Teleport behind player
        Vector3 behindPlayer = targetTransform.position - targetTransform.forward * 2f;
        transform.position = behindPlayer;

        // กลับมามองเห็นได้และโจมตี
        RPC_BecomeInvisible(false);

        Hero targetHero = targetTransform.GetComponent<Hero>();
        if (targetHero != null)
        {
            RPC_DealDamageToPlayer(targetHero.Object, shadowStrikeDamage, true);
            targetHero.ApplyStatusEffect(StatusEffectType.Bleed, 3, 8f);
        }

        yield return new WaitForSeconds(0.3f);
        isPerformingSpecialAttack = false;
    }

    private IEnumerator TeleportStrikeCoroutine()
    {
        if (targetTransform == null) yield break;

        // Teleport to random position around player
        Vector3 randomDirection = Random.insideUnitSphere;
        randomDirection.y = 0;
        randomDirection.Normalize();

        Vector3 teleportPos = targetTransform.position + randomDirection * teleportRange;

        RPC_TeleportEffect(transform.position, teleportPos);
        transform.position = teleportPos;

        yield return new WaitForSeconds(0.2f);

        // Immediate attack
        Hero targetHero = targetTransform.GetComponent<Hero>();
        if (targetHero != null && Vector3.Distance(transform.position, targetHero.transform.position) <= AttackRange * 1.5f)
        {
            RPC_DealDamageToPlayer(targetHero.Object, shadowStrikeDamage, true);
        }

        yield return new WaitForSeconds(0.3f);
        isPerformingSpecialAttack = false;
    }

    private IEnumerator ShadowWaveCoroutine()
    {
        // ส่งคลื่นพลังงานไปทุกทิศทาง
        RPC_CreateShadowWave(transform.position);

        // หาผู้เล่นทั้งหมดในรัศมี
        Collider[] players = Physics.OverlapSphere(transform.position, shadowWaveSpeed * 2f, LayerMask.GetMask("Player"));

        foreach (Collider player in players)
        {
            Hero hero = player.GetComponent<Hero>();
            if (hero != null)
            {
                float distance = Vector3.Distance(transform.position, hero.transform.position);
                float delay = distance / shadowWaveSpeed;

                StartCoroutine(DelayedShadowWaveDamage(hero, delay));
            }
        }

        yield return new WaitForSeconds(1f);
        isPerformingSpecialAttack = false;
    }

    private IEnumerator DelayedShadowWaveDamage(Hero hero, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (hero != null)
        {
            RPC_DealDamageToPlayer(hero.Object, shadowWaveDamage, false);
            hero.ApplyStatusEffect(StatusEffectType.Weakness, 0, 5f, 0.3f);
        }
    }

    private void StartChannelUltimate()
    {
        IsChannelingUltimate = true;
        UltimateChannelTimer = 0f;
        isPerformingSpecialAttack = true;

        RPC_StartUltimateChannel();
    }

    private void ExecuteUltimateAttack()
    {
        IsChannelingUltimate = false;

        // สร้างความเสียหายมหาศาลในพื้นที่กว้าง
        Collider[] players = Physics.OverlapSphere(transform.position, ultimateRadius, LayerMask.GetMask("Player"));

        foreach (Collider player in players)
        {
            Hero hero = player.GetComponent<Hero>();
            if (hero != null)
            {
                RPC_DealDamageToPlayer(hero.Object, ultimateDamage, true);

                // ใส่ status effects หลายอัน
                hero.ApplyStatusEffect(StatusEffectType.Burn, 10, 10f);
                hero.ApplyStatusEffect(StatusEffectType.Stun, 0, 3f);
            }
        }

        RPC_ExecuteUltimate(transform.position, ultimateRadius);

        StartCoroutine(UltimateRecovery());
    }

    private IEnumerator UltimateRecovery()
    {
        yield return new WaitForSeconds(2f);
        isPerformingSpecialAttack = false;
    }
    #endregion

    #region Minion System
    private void SummonMinions()
    {
        if (minionPrefab == null || !HasStateAuthority) return;

        int minionsToSpawn = Mathf.Min(3, maxMinions - activeMinions.Count);

        for (int i = 0; i < minionsToSpawn; i++)
        {
            Vector3 spawnPos = transform.position + Random.insideUnitSphere * minionSpawnRadius;
            spawnPos.y = transform.position.y;

            NetworkObject minion = Runner.Spawn(minionPrefab, spawnPos, Quaternion.identity);
            if (minion != null)
            {
                activeMinions.Add(minion);
            }
        }

        RPC_OnMinionSummon(minionsToSpawn);
        isPerformingSpecialAttack = false;
    }

    private void CleanupDeadMinions()
    {
        for (int i = activeMinions.Count - 1; i >= 0; i--)
        {
            if (activeMinions[i] == null || !activeMinions[i].IsValid)
            {
                activeMinions.RemoveAt(i);
            }
        }
    }
    #endregion

    #region Shield System
    private void ActivateShield()
    {
        HasActiveShield = true;
        CurrentShieldHealth = shieldHealth;

        RPC_ActivateShield();
        StartCoroutine(ShieldDurationCoroutine());
    }

    private IEnumerator ShieldDurationCoroutine()
    {
        yield return new WaitForSeconds(shieldDuration);

        HasActiveShield = false;
        CurrentShieldHealth = 0;
        RPC_DeactivateShield();
    }

    public override void TakeDamage(int damage, DamageType damageType = DamageType.Normal, bool isCritical = false)
    {
        if (HasActiveShield && HasStateAuthority)
        {
            // Shield absorbs damage first
            int damageToShield = Mathf.Min(damage, CurrentShieldHealth);
            CurrentShieldHealth -= damageToShield;
            damage -= damageToShield;

            RPC_ShieldDamage(damageToShield);

            if (CurrentShieldHealth <= 0)
            {
                HasActiveShield = false;
                RPC_DeactivateShield();
            }

            // Remaining damage goes to HP
            if (damage > 0)
            {
                base.TakeDamage(damage, damageType, isCritical);
            }
        }
        else
        {
            base.TakeDamage(damage, damageType, isCritical);
        }
    }
    #endregion

    #region Basic Attack Override
    private void TryBasicAttack()
    {
        if (targetTransform == null || isPerformingSpecialAttack) return;

        float distance = Vector3.Distance(transform.position, targetTransform.position);

        if (distance <= AttackRange && Runner.SimulationTime >= nextAttackTime)
        {
            nextAttackTime = Runner.SimulationTime + AttackCooldown;

            Hero targetHero = targetTransform.GetComponent<Hero>();
            if (targetHero != null)
            {
                RPC_DealDamageToPlayer(targetHero.Object, AttackDamage, false);

                // เพิ่มโอกาส status effects สำหรับบอส
                if (Random.Range(0f, 100f) <= 40f) // 40% chance
                {
                    StatusEffectType[] possibleEffects = {
                        StatusEffectType.Poison, StatusEffectType.Burn,
                        StatusEffectType.Bleed, StatusEffectType.Weakness
                    };

                    StatusEffectType randomEffect = possibleEffects[Random.Range(0, possibleEffects.Length)];
                    targetHero.ApplyStatusEffect(randomEffect, 5, 5f, 0.3f);
                }
            }
        }
    }
    #endregion

    #region RPC Methods
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnPhaseTransition(BossPhase newPhase)
    {
        Debug.Log($"🔥 Shadow Lord Boss entered {newPhase}!");

        // Visual effects สำหรับการเปลี่ยนเฟส
        // เพิ่ม particle effects, screen shake, etc.
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnSpecialAttack(BossAttackType attackType)
    {
        Debug.Log($"⚡ Boss uses {attackType}!");

        // Visual และ audio effects สำหรับ special attacks
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_DealDamageToPlayer(NetworkObject playerObject, int damage, bool isCritical)
    {
        if (playerObject != null)
        {
            Hero hero = playerObject.GetComponent<Hero>();
            if (hero != null && hero.HasInputAuthority)
            {
                hero.TakeDamage(damage, DamageType.Normal, isCritical);
                Debug.Log($"Boss deals {damage} damage to {hero.CharacterName}");
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowAOEWarning(Vector3 center, float radius)
    {
        Debug.Log($"⚠️ AOE Warning at {center} with radius {radius}");
        // สร้าง warning indicator
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ExecuteAOEBlast(Vector3 center, float radius)
    {
        Debug.Log($"💥 AOE Blast executed!");
        // สร้าง explosion effect
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BecomeInvisible(bool invisible)
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Color color = renderer.material.color;
            color.a = invisible ? 0.3f : 1f;
            renderer.material.color = color;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TeleportEffect(Vector3 fromPos, Vector3 toPos)
    {
        Debug.Log($"✨ Boss teleports from {fromPos} to {toPos}");
        // สร้าง teleport effect
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_CreateShadowWave(Vector3 center)
    {
        Debug.Log($"🌊 Shadow Wave from {center}");
        // สร้าง expanding wave effect
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_StartUltimateChannel()
    {
        Debug.Log($"🔴 ULTIMATE CHANNELING! TAKE COVER!");
        // สร้าง dramatic channeling effect
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ExecuteUltimate(Vector3 center, float radius)
    {
        Debug.Log($"💀 ULTIMATE DESTRUCTION!");
        // สร้าง massive explosion effect
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnMinionSummon(int count)
    {
        Debug.Log($"👾 Boss summoned {count} minions!");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ActivateShield()
    {
        Debug.Log($"🛡️ Boss activated shield!");
        // สร้าง shield visual effect
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_DeactivateShield()
    {
        Debug.Log($"💔 Boss shield destroyed!");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShieldDamage(int damage)
    {
        Debug.Log($"🛡️ Shield took {damage} damage! Remaining: {CurrentShieldHealth}");
    }
    #endregion

    #region Debug Methods
    [ContextMenu("Force Phase 2")]
    public void ForcePhase2()
    {
        if (HasStateAuthority)
        {
            StartPhaseTransition(BossPhase.Phase2_Enraged);
        }
    }

    [ContextMenu("Force Phase 3")]
    public void ForcePhase3()
    {
        if (HasStateAuthority)
        {
            StartPhaseTransition(BossPhase.Phase3_Desperate);
        }
    }

    [ContextMenu("Force Ultimate")]
    public void ForceUltimate()
    {
        if (HasStateAuthority)
        {
            ExecuteSpecialAttack(BossAttackType.UltimateDestruction);
        }
    }

    [ContextMenu("Test Charge Attack")]
    public void TestChargeAttack()
    {
        if (HasStateAuthority)
        {
            ExecuteSpecialAttack(BossAttackType.ChargeAttack);
        }
    }
    #endregion
}