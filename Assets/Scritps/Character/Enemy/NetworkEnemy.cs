﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
[System.Serializable]
public class SpecificItemDrop
{
    [Header("📦 Item Settings")]
    public ItemData itemData;

    [Header("🎯 Drop Settings")]
    [Tooltip("โอกาสที่จะ drop item นี้ (0-100%)")]
    [Range(0f, 100f)]
    public float dropChance = 15f;

    [Tooltip("จำนวนขั้นต่ำที่จะ drop")]
    [Range(1, 10)]
    public int minQuantity = 1;

    [Tooltip("จำนวนสูงสุดที่จะ drop")]
    [Range(1, 10)]
    public int maxQuantity = 1;

    [Header("🎯 Level Requirements")]
    [Tooltip("Level ขั้นต่ำของ enemy ที่จะ drop item นี้")]
    [Range(1, 50)]
    public int minEnemyLevel = 1;

    [Tooltip("Level สูงสุดของ enemy ที่จะ drop item นี้ (0 = ไม่จำกัด)")]
    [Range(0, 50)]
    public int maxEnemyLevel = 0;

    public SpecificItemDrop()
    {
        dropChance = 15f;
        minQuantity = 1;
        maxQuantity = 1;
        minEnemyLevel = 1;
        maxEnemyLevel = 0;
    }

    public SpecificItemDrop(ItemData item, float chance, int minQty = 1, int maxQty = 1)
    {
        itemData = item;
        dropChance = chance;
        minQuantity = minQty;
        maxQuantity = maxQty;
        minEnemyLevel = 1;
        maxEnemyLevel = 0;
    }

    public bool CanDropAtLevel(int enemyLevel)
    {
        if (enemyLevel < minEnemyLevel) return false;
        if (maxEnemyLevel > 0 && enemyLevel > maxEnemyLevel) return false;
        return true;
    }

    public bool IsValid()
    {
        return itemData != null && dropChance > 0f;
    }
}
public enum EnemyState
{
    Patrolling,    // 🆕 เพิ่ม state การสุ่มเดิน
    Chasing,
    BackingOff,
    Attacking,
    Positioning
}

public class NetworkEnemy : Character
{
    // ========== Network Properties ==========
    [Networked] public NetworkBool IsDead { get; set; }
    [Networked] public PlayerRef CurrentTarget { get; set; }

    // เพิ่ม Network Position Properties เหมือน Hero
    [Networked] public Vector3 NetworkedPosition { get; set; }
    [Networked] public Vector3 NetworkedVelocity { get; set; }
    [Networked] public Vector3 NetworkedScale { get; set; } // ใช้สำหรับ flip เท่านั้น

    // 🆕 Network Properties สำหรับ Patrol System
    [Networked] public Vector3 PatrolCenter { get; set; }
    [Networked] public Vector3 PatrolTarget { get; set; }
    [Networked] public float PatrolWaitTimer { get; set; }

    // ========== Enemy Properties ==========
    [Header("Enemy Settings")]
    public float detectRange = 10f;
    public float attackCheckInterval = 0.5f;

    [Networked] public EnemyState CurrentState { get; set; }
    [Networked] public float StateTimer { get; set; }

    [Header("🎯 Attack Pattern Settings")]
    public float backOffDistance = 3f;      // ระยะที่ต้องถอยออกไป
    public float backOffTime = 1f;          // เวลาที่ใช้ในการถอย
    public float positionTime = 0.5f;       // เวลาหยุดก่อนเข้าโจมตี
    public float rushSpeed = 3f;          // ความเร็วตอนเข้าโจมตี

    [Header("🚶 Patrol Settings")]
    public float patrolRange = 8f;           // ระยะการ patrol จากจุดเริ่มต้น
    public float patrolSpeed = 0.5f;         // ความเร็วการ patrol (คูณกับ MoveSpeed)
    [Header("🎲 Random Wait Time Settings")]
    public float minPatrolWaitTime = 1f;     // เวลาหยุดขั้นต่ำที่จุดหมาย
    public float maxPatrolWaitTime = 4f;     // เวลาหยุดสูงสุดที่จุดหมาย
    [Space]
    public float patrolTargetRadius = 1f;    // ระยะที่ถือว่าถึงจุดหมายแล้ว
    public bool returnToCenter = true;       // กลับไปจุดกลางหลังจาก patrol นานๆ
    public float maxPatrolTime = 30f;        // เวลาสูงสุดก่อนกลับจุดกลาง

    [Header("🎯 Individual Enemy Settings")]
    [Tooltip("เวลาหยุดที่สุ่มแล้วสำหรับตัวนี้ (Auto-generated)")]
    public float individualPatrolWaitTime; // เวลาหยุดที่สุ่มแล้วสำหรับตัวนี้

    [Header("🎯 Improved Movement Settings")]
    public float minDistanceToPlayer = 1.0f; // ระยะห่างขั้นต่ำจากผู้เล่น
    public float enemySpacing = 2.0f;        // ระยะห่างระหว่างศัตรูด้วยกัน
    public LayerMask enemyLayer;             // Layer ของศัตรู
    public bool useCircling = true;          // เปิดใช้การเคลื่อนที่แบบวนรอบผู้เล่น
    public float circlingSpeed = 0.5f;
    public bool isCollidingWithPlayer;

    [Header("🔧 Debug Settings")]
    public bool showDebugInfo = false;
    public bool showPatrolGizmos = true;     // 🆕 แสดง patrol area ใน Scene view


   

    

    [Header("💥 Proximity Damage")]
    public float collisionDamageCooldown = 2.0f;
    public float collisionDamageMultiplier = 0.5f;
    private float nextCollisionDamageTime = 0f;

    private float nextTargetCheckTime = 0f;
    protected Transform targetTransform;
    protected float nextAttackTime = 0f;
    private float totalPatrolTime = 0f;      // 🆕 เวลา patrol รวม

    [Header("💰 Drop System")]
    public EnemyDropManager dropManager;
    public ItemDropManager ItemDrop;
    // Check if properly spawned
    public bool IsSpawned => Object != null && Object.IsValid;

    // ========== Unity Lifecycle ==========
    protected override void Start()
    {
        base.Start();
        Debug.Log($"Enemy Start - HasStateAuthority: {HasStateAuthority}");
        if (dropManager == null)
            dropManager = GetComponent<EnemyDropManager>();
        if (ItemDrop == null)
            ItemDrop = GetComponent<ItemDropManager>();
        // ตั้งค่า enemy layer
        if (enemyLayer == 0)
        {
            enemyLayer = LayerMask.GetMask("Enemy");
        }

        // ตั้งค่า physics สำหรับ 2D movement
        if (rb != null)
        {
            rb.freezeRotation = true; // ห้ามหมุนทุกแกน - ใช้แค่ flip
            rb.useGravity = true;
            rb.drag = 2.0f; // เพิ่ม drag เพื่อให้หยุดได้เร็วขึ้น
            rb.mass = 5f;   // ลด mass เพื่อให้เคลื่อนที่ได้ง่ายขึ้น
        }

        if (showDebugInfo)
        {
            Debug.Log($"=== Enemy Movement Settings ===");
            Debug.Log($"minDistanceToPlayer: {minDistanceToPlayer}");
            Debug.Log($"enemySpacing: {enemySpacing}");
            Debug.Log($"useCircling: {useCircling}");
            Debug.Log($"circlingSpeed: {circlingSpeed}");
            Debug.Log($"patrolRange: {patrolRange}");
            Debug.Log($"patrolSpeed: {patrolSpeed}");
            Debug.Log($"patrolWaitTime: {minPatrolWaitTime}-{maxPatrolWaitTime}s (random)");
            Debug.Log($"===============================");
        }

        LevelManager enemyLevel = GetComponent<LevelManager>();
        if (enemyLevel != null && HasStateAuthority)
        {
            // Set random level 1-5 for enemy
            int randomLevel = Random.Range(1, 10);
            while (enemyLevel.CurrentLevel < randomLevel)
            {
                enemyLevel.GainExp(enemyLevel.ExpToNextLevel);
            }
            Debug.Log($"Enemy {CharacterName} spawned at level {randomLevel}");
        }
    }

    // ใน NetworkEnemy.cs - override InitializeStats เพื่อไม่ให้เรียก equipment methods

    protected override void InitializeStats()
    {
        if (characterStats != null)
        {
            // ✅ ใช้ ScriptableObject เป็นหลักสำหรับ enemy (ไม่ต้องใช้ equipment)
            CharacterName = characterStats.characterName;
            MaxHp = characterStats.maxHp;
            CurrentHp = MaxHp;
            MaxMana = characterStats.maxMana;
            CurrentMana = MaxMana;
            AttackDamage = characterStats.attackDamage;
            MagicDamage = characterStats.magicDamage;
            Armor = characterStats.arrmor;
            MoveSpeed = characterStats.moveSpeed;
            AttackRange = characterStats.attackRange;
            AttackCooldown = characterStats.attackCoolDown;
            CriticalChance = characterStats.criticalChance;
            CriticalDamageBonus = characterStats.criticalDamageBonus;
            HitRate = characterStats.hitRate;
            EvasionRate = characterStats.evasionRate;
            AttackSpeed = characterStats.attackSpeed;
            ReductionCoolDown = characterStats.reductionCoolDown;
            AttackType = characterStats.attackType;

            // 🆕 Enemy ไม่ต้องใช้ InitializeEquipmentSlots()
            Debug.Log($"[NetworkEnemy] Stats initialized for {CharacterName} (no equipment)");
        }
    }

    // Called when spawned by Fusion
    public override void Spawned()
    {
        Debug.Log($"Enemy Spawned - HasStateAuthority: {HasStateAuthority}");

        // Initialize network properties only on server
        if (HasStateAuthority)
        {
            NetworkedMaxHp = MaxHp;
            NetworkedCurrentHp = CurrentHp;
            IsDead = false;
            CurrentState = EnemyState.Patrolling; // 🆕 เริ่มต้นด้วย Patrolling แทน Chasing
            StateTimer = 0f;
            totalPatrolTime = 0f;

            // 🆕 ตั้งค่า patrol center และ target
            PatrolCenter = transform.position;
            GenerateNewPatrolTarget();

            // 🎲 สุ่ม patrol wait time สำหรับตัวนี้
            individualPatrolWaitTime = Random.Range(minPatrolWaitTime, maxPatrolWaitTime);

            // กำหนดค่าเริ่มต้นของ position และ scale
            NetworkedPosition = transform.position;
            NetworkedScale = transform.localScale;

            if (showDebugInfo)
            {
                Debug.Log($"{CharacterName}: Initialized patrol system at {PatrolCenter}");
                Debug.Log($"{CharacterName}: Individual patrol wait time: {individualPatrolWaitTime:F1}s (range: {minPatrolWaitTime}-{maxPatrolWaitTime})");
            }
        }
    }

    // ========== Network Update ==========
    public override void FixedUpdateNetwork()
    {
        // เรียก base.FixedUpdateNetwork() ก่อนเพื่อให้ระบบ poison ทำงาน
        base.FixedUpdateNetwork();

        // Safety check
        if (!IsSpawned) return;

        // Server/Host controls enemies
        if (HasStateAuthority)
        {
            if (!IsDead)
            {
                // 🔧 ใช้ระบบ AI ใหม่ที่ปรับปรุงแล้ว
                FindNearestPlayer();
                ImprovedMoveTowardsTarget();
                TryAttackTarget();

                // Check death
                if (CurrentHp <= 0 && !IsDead)
                {
                    IsDead = true;
                    RPC_OnDeath();
                }
            }

            // อัพเดท Network Properties
            NetworkedPosition = transform.position;
            NetworkedScale = transform.localScale; // sync scale สำหรับ flip
            if (rb != null)
            {
                NetworkedVelocity = rb.velocity;
            }
        }
        // Remote clients - apply network state
        else
        {
            ApplyNetworkState();
        }
    }

    protected virtual void OnDestroy()
    {
    }

    // ========== 🆕 Patrol System ==========
    private void GenerateNewPatrolTarget()
    {
        // สุ่มจุดใหม่ในรัศมี patrol
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        float randomDistance = Random.Range(patrolRange * 0.3f, patrolRange);

        Vector3 targetPosition = PatrolCenter + new Vector3(randomDirection.x, 0, randomDirection.y) * randomDistance;

        // ตรวจสอบว่าจุดหมายอยู่ในพื้นที่ที่เดินได้
        PatrolTarget = ValidatePatrolTarget(targetPosition);
        PatrolWaitTimer = 0f;

        if (showDebugInfo)
        {
            Debug.Log($"{CharacterName}: New patrol target: {PatrolTarget} (distance: {Vector3.Distance(PatrolCenter, PatrolTarget):F1})");
        }
    }

    private Vector3 ValidatePatrolTarget(Vector3 target)
    {
        // ตรวจสอบว่าจุดหมายไม่ติดสิ่งกีดขวาง
        RaycastHit hit;
        Vector3 directionToTarget = (target - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, target);

        if (Physics.Raycast(transform.position, directionToTarget, out hit, distanceToTarget, LayerMask.GetMask("Wall", "Obstacle")))
        {
            // ถ้าติดสิ่งกีดขวาง ให้เลือกจุดใกล้ๆ patrol center
            Vector2 safeDirection = Random.insideUnitCircle.normalized;
            return PatrolCenter + new Vector3(safeDirection.x, 0, safeDirection.y) * (patrolRange * 0.5f);
        }

        return target;
    }

    // ========== 🎯 Improved Movement System ==========
    protected virtual void ImprovedMoveTowardsTarget()
    {
        // อัพเดท State Timer
        StateTimer += Runner.DeltaTime;

        // 🆕 ตรวจสอบว่าควรเปลี่ยนจาก Patrolling เป็น Chasing หรือไม่
        if (CurrentState == EnemyState.Patrolling && targetTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, targetTransform.position);
            if (distanceToPlayer <= detectRange)
            {
                CurrentState = EnemyState.Chasing;
                StateTimer = 0f;
                if (showDebugInfo)
                {
                    Debug.Log($"{CharacterName}: Player detected! Switching to Chasing. Distance: {distanceToPlayer:F1}");
                }
            }
        }

        // 🆕 ตรวจสอบว่าควรกลับไป Patrolling หรือไม่
        if (CurrentState != EnemyState.Patrolling && targetTransform == null)
        {
            CurrentState = EnemyState.Patrolling;
            StateTimer = 0f;
            totalPatrolTime = 0f;
            GenerateNewPatrolTarget();
            if (showDebugInfo)
            {
                Debug.Log($"{CharacterName}: No target found, returning to Patrol");
            }
        }

        Vector3 moveDirection = Vector3.zero;

        switch (CurrentState)
        {
            case EnemyState.Patrolling:
                moveDirection = HandlePatrolling();
                break;

            case EnemyState.Chasing:
                moveDirection = HandleChasing();
                break;

            case EnemyState.BackingOff:
                moveDirection = HandleBackingOff();
                break;

            case EnemyState.Positioning:
                moveDirection = HandlePositioning();
                break;

            case EnemyState.Attacking:
                moveDirection = HandleAttacking();
                break;
        }

        // หลีกเลี่ยงศัตรูตัวอื่น (ยกเว้นตอน patrol)
        if (CurrentState != EnemyState.Patrolling)
        {
            Vector3 avoidanceForce = CalculateAvoidanceForce();
            moveDirection += avoidanceForce;
        }

        // ทำให้เป็นทิศทางที่ถูกต้อง
        moveDirection.y = 0;
        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }

        // เคลื่อนที่
        ApplyMovement(moveDirection);

        // Debug info
        if (showDebugInfo)
        {
            string targetInfo = targetTransform != null ? $"Target: {targetTransform.name}" : "No Target";
            Debug.Log($"{CharacterName}: State={CurrentState}, Timer={StateTimer:F1}, {targetInfo}");
        }
    }

    // 🆕 Handler สำหรับ Patrolling State
    private Vector3 HandlePatrolling()
    {
        totalPatrolTime += Runner.DeltaTime;

        float distanceToTarget = Vector3.Distance(transform.position, PatrolTarget);

        // ถ้าถึงจุดหมายแล้ว
        if (distanceToTarget <= patrolTargetRadius)
        {
            PatrolWaitTimer += Runner.DeltaTime;

            // 🎲 ใช้ individualPatrolWaitTime ที่สุ่มแล้วแทน patrolWaitTime
            if (PatrolWaitTimer >= individualPatrolWaitTime)
            {
                // หยุดพอแล้ว สร้างจุดหมายใหม่
                if (returnToCenter && totalPatrolTime >= maxPatrolTime)
                {
                    // กลับไปจุดกลางถ้า patrol นานเกินไป
                    PatrolTarget = PatrolCenter;
                    totalPatrolTime = 0f;
                    if (showDebugInfo)
                    {
                        Debug.Log($"{CharacterName}: Returning to patrol center");
                    }
                }
                else
                {
                    GenerateNewPatrolTarget();
                    // 🎲 สุ่ม wait time ใหม่สำหรับจุดหมายต่อไป
                    individualPatrolWaitTime = Random.Range(minPatrolWaitTime, maxPatrolWaitTime);
                    if (showDebugInfo)
                    {
                        Debug.Log($"{CharacterName}: New wait time: {individualPatrolWaitTime:F1}s");
                    }
                }
            }
            else if (showDebugInfo && PatrolWaitTimer % 1f < Runner.DeltaTime) // แสดงทุกวินาที
            {
                Debug.Log($"{CharacterName}: Waiting at patrol point... {PatrolWaitTimer:F1}/{individualPatrolWaitTime:F1}s");
            }

            return Vector3.zero; // หยุดเคลื่อนที่ขณะรอ
        }

        // เดินไปหาจุดหมาย
        Vector3 directionToTarget = (PatrolTarget - transform.position).normalized;
        return directionToTarget;
    }

    // Handler สำหรับ Chasing State
    private Vector3 HandleChasing()
    {
        if (targetTransform == null) return Vector3.zero;

        Vector3 directionToPlayer = (targetTransform.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, targetTransform.position);

        if (distanceToPlayer <= AttackRange && distanceToPlayer > minDistanceToPlayer * 0.7f)
        {
            // อยู่ในระยะโจมตีที่เหมาะสม เปลี่ยนเป็น Attacking
            CurrentState = EnemyState.Attacking;
            StateTimer = 0f;
            if (showDebugInfo)
                Debug.Log($"{CharacterName}: Entering attack range! Distance: {distanceToPlayer:F2}");
            return Vector3.zero;
        }
        else if (distanceToPlayer <= minDistanceToPlayer * 0.7f)
        {
            // เข้าใกล้เกินไป เปลี่ยนเป็น BackingOff
            CurrentState = EnemyState.BackingOff;
            StateTimer = 0f;
            if (showDebugInfo)
                Debug.Log($"{CharacterName}: Too close! Backing off... Distance: {distanceToPlayer:F2}");
        }

        return directionToPlayer;
    }

    // Handler สำหรับ BackingOff State
    private Vector3 HandleBackingOff()
    {
        if (targetTransform == null) return Vector3.zero;

        Vector3 directionToPlayer = (targetTransform.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, targetTransform.position);
        Vector3 moveDirection = -directionToPlayer * 1.2f; // ถอยเร็วหน่อย

        if (StateTimer >= backOffTime || distanceToPlayer >= backOffDistance)
        {
            // ถอยพอแล้ว เปลี่ยนเป็น Positioning
            CurrentState = EnemyState.Positioning;
            StateTimer = 0f;
            if (showDebugInfo)
                Debug.Log($"{CharacterName}: Finished backing off, positioning... Distance: {distanceToPlayer:F2}");
        }

        return moveDirection;
    }

    // Handler สำหรับ Positioning State
    private Vector3 HandlePositioning()
    {
        if (StateTimer >= positionTime)
        {
            // พร้อมเข้าโจมตี
            CurrentState = EnemyState.Chasing;
            StateTimer = 0f;
            if (showDebugInfo)
                Debug.Log($"{CharacterName}: Ready to chase again!");
        }

        return Vector3.zero; // หยุดชั่วคราว
    }

    // Handler สำหรับ Attacking State
    private Vector3 HandleAttacking()
    {
        if (targetTransform == null) return Vector3.zero;

        Vector3 directionToPlayer = (targetTransform.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, targetTransform.position);

        if (distanceToPlayer > AttackRange * 1.3f)
        {
            // ผู้เล่นออกไปไกลเกินไป กลับไปไล่ตาม
            CurrentState = EnemyState.Chasing;
            StateTimer = 0f;
            if (showDebugInfo)
                Debug.Log($"{CharacterName}: Player too far, chasing... Distance: {distanceToPlayer:F2}");
        }
        else if (distanceToPlayer < minDistanceToPlayer * 0.5f)
        {
            // ใกล้เกินไปมาก ถอยออกมา
            CurrentState = EnemyState.BackingOff;
            StateTimer = 0f;
            if (showDebugInfo)
                Debug.Log($"{CharacterName}: Player very close, backing off... Distance: {distanceToPlayer:F2}");
        }
        else
        {
            // อยู่ในระยะที่ดี - โจมตีได้! เคลื่อนที่เล็กน้อยหรือหยุด
            if (useCircling && distanceToPlayer > minDistanceToPlayer)
            {
                Vector3 circleDirection = new Vector3(-directionToPlayer.z, 0, directionToPlayer.x);
                return circleDirection * circlingSpeed * 0.3f;
            }
        }

        return Vector3.zero;
    }

    // ปรับปรุง ApplyMovement เพื่อรองรับ patrol speed
    private void ApplyMovement(Vector3 moveDirection)
    {
        if (moveDirection.magnitude > 0.1f)
        {
            float currentMoveSpeed = GetEffectiveMoveSpeed();

            // ปรับความเร็วตาม state
            switch (CurrentState)
            {
                case EnemyState.Patrolling:
                    currentMoveSpeed *= patrolSpeed; // ช้าลงเวลา patrol
                    break;
                case EnemyState.BackingOff:
                    currentMoveSpeed *= 1.3f; // ถอยเร็วขึ้น
                    break;
            }

            Vector3 newPosition = transform.position + moveDirection * currentMoveSpeed * Runner.DeltaTime;
            rb.MovePosition(newPosition);

            // การ flip
            FlipCharacterTowardsMovement(moveDirection);
        }
        else
        {
            // หยุดการเคลื่อนที่
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }
    }

    // ========== 🔧 ระบบหลีกเลี่ยงศัตรูตัวอื่น ==========
    protected Vector3 CalculateAvoidanceForce()
    {
        Vector3 avoidanceForce = Vector3.zero;

        // ตรวจสอบศัตรูใกล้เคียง
        Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, enemySpacing, enemyLayer);

        // หาแรงผลักออกจากศัตรูแต่ละตัว
        foreach (Collider enemyCollider in nearbyEnemies)
        {
            // ข้ามตัวเอง
            if (enemyCollider.gameObject == gameObject)
                continue;

            // คำนวณทิศทางหลีกเลี่ยง
            Vector3 directionAway = transform.position - enemyCollider.transform.position;
            float distance = directionAway.magnitude;

            // ถ้าระยะห่างน้อยกว่าที่กำหนด ให้คำนวณแรงผลัก
            if (distance < enemySpacing && distance > 0.1f)
            {
                // ยิ่งใกล้ยิ่งผลักแรง
                float strength = 1.0f - (distance / enemySpacing);
                directionAway.y = 0; // ไม่ผลักในแกน Y
                directionAway.Normalize();
                avoidanceForce += directionAway * strength;

                if (showDebugInfo)
                {
                    Debug.Log($"{CharacterName}: Avoiding enemy at distance {distance:F1}");
                }
            }
        }

        return avoidanceForce;
    }

    // 🔧 ระบบการ flip แบบ 2D เท่านั้น (แก้ oscillation)
    protected void FlipCharacterTowardsMovement(Vector3 moveDirection)
    {
        if (moveDirection.magnitude < 0.3f) return; // เพิ่ม threshold เพื่อป้องกัน flip บ่อย

        // flip เฉพาะ scale.x ตามทิศทางการเคลื่อนที่
        Vector3 newScale = transform.localScale;
        if (moveDirection.x > 0.3f) // เพิ่ม threshold
        {
            newScale.x = Mathf.Abs(newScale.x); // หันขวา
        }
        else if (moveDirection.x < -0.3f) // เพิ่ม threshold
        {
            newScale.x = -Mathf.Abs(newScale.x); // หันซ้าย
        }
        // ถ้าอยู่ระหว่าง -0.3 ถึง 0.3 ไม่เปลี่ยน flip

        transform.localScale = newScale;
    }

    // ========== Network State Application ==========
    protected virtual void ApplyNetworkState()
    {
        float positionDistance = Vector3.Distance(transform.position, NetworkedPosition);

        if (positionDistance > 0.1f)
        {
            if (rb != null)
            {
                rb.velocity = NetworkedVelocity;
            }

            float lerpRate = positionDistance > 2f ? 50f : 20f;
            transform.position = Vector3.Lerp(
                transform.position,
                NetworkedPosition,
                Runner.DeltaTime * lerpRate
            );
        }

        // Scale synchronization สำหรับ flip
        float scaleDistance = Vector3.Distance(transform.localScale, NetworkedScale);
        if (scaleDistance > 0.01f)
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                NetworkedScale,
                Runner.DeltaTime * 15f
            );
        }
    }

    // ========== Render Method for Visual Interpolation ==========
    public override void Render()
    {
        // Visual interpolation สำหรับ remote clients
        if (!HasStateAuthority)
        {
            float alpha = Runner.DeltaTime * 20f;

            if (Vector3.Distance(transform.position, NetworkedPosition) > 0.01f)
            {
                transform.position = Vector3.Lerp(transform.position, NetworkedPosition, alpha);
            }

            if (Vector3.Distance(transform.localScale, NetworkedScale) > 0.001f)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, NetworkedScale, alpha);
            }
        }
    }

    // ========== Enemy AI ==========
    protected void FindNearestPlayer()
    {
        if (Time.time < nextTargetCheckTime) return;
        nextTargetCheckTime = Time.time + attackCheckInterval;

        float nearestDistance = float.MaxValue;
        Hero nearestHero = null;

        // Find all heroes in scene
        Hero[] heroes = FindObjectsOfType<Hero>();

        foreach (Hero hero in heroes)
        {
            if (hero == null || !hero.IsSpawned) continue;

            float distance = Vector3.Distance(transform.position, hero.transform.position);

            // 🆕 ปรับการตรวจจับ: ใช้ detectRange สำหรับ patrol, แต่ยังติดตามต่อถ้าเคย detect แล้ว
            float effectiveDetectRange = detectRange;
            if (CurrentState != EnemyState.Patrolling)
            {
                // ถ้าไม่ได้อยู่ใน patrol state ให้ติดตามในระยะไกลขึ้น
                effectiveDetectRange = detectRange * 1.5f;
            }

            if (distance < effectiveDetectRange && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestHero = hero;
            }
        }

        if (nearestHero != null)
        {
            targetTransform = nearestHero.transform;
            CurrentTarget = nearestHero.Object.InputAuthority;

            if (showDebugInfo)
            {
                Debug.Log($"{CharacterName} found target: {nearestHero.CharacterName} at distance {nearestDistance:F1}");
            }
        }
        else
        {
            targetTransform = null;
            CurrentTarget = PlayerRef.None;
        }
    }

    protected virtual void TryAttackTarget()
    {
        if (targetTransform == null) return;

        float distance = Vector3.Distance(transform.position, targetTransform.position);

        // ปรับเงื่อนไขการโจมตีให้ง่ายขึ้น
        bool canAttack = CurrentState == EnemyState.Attacking &&
                         distance <= AttackRange &&
                         distance >= minDistanceToPlayer * 0.5f && // ไม่ให้ใกล้เกินไป
                         Runner.SimulationTime >= nextAttackTime;

        if (canAttack)
        {
            // ✅ 🌟 เปลี่ยน: ใช้ GetEffectiveAttackSpeed() แทน AttackSpeed
            float effectiveAttackSpeed = GetEffectiveAttackSpeed();
            float finalAttackCooldown = AttackCooldown / Mathf.Max(0.1f, effectiveAttackSpeed);
            nextAttackTime = Runner.SimulationTime + finalAttackCooldown;

            RPC_PerformAttack(CurrentTarget);

            if (showDebugInfo)
            {
                Debug.Log($"{CharacterName}: *** ATTACK EXECUTED! *** Distance: {distance:F2}, State: {CurrentState}, Speed: {effectiveAttackSpeed:F1}x (base: {AttackSpeed:F1}x)");
            }
        }
        else if (showDebugInfo && CurrentState == EnemyState.Attacking)
        {
            // Debug ว่าทำไมไม่โจมตี
            string reason = "";
            if (distance > AttackRange) reason += "Too far ";
            if (distance < minDistanceToPlayer * 0.5f) reason += "Too close ";
            if (Runner.SimulationTime < nextAttackTime) reason += $"Cooldown ({nextAttackTime - Runner.SimulationTime:F1}s) ";

            Debug.Log($"{CharacterName}: Cannot attack - {reason} | Distance: {distance:F2}");
        }
    }

    // ========== 🎨 Debug Visualization ==========
    protected void OnDrawGizmosSelected()
    {
        if (!showPatrolGizmos) return;

        // วาดพื้นที่ patrol
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(HasStateAuthority ? PatrolCenter : transform.position, patrolRange);

        // วาดจุดกลาง patrol
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(HasStateAuthority ? PatrolCenter : transform.position, 0.5f);

        // วาดจุดหมาย patrol ปัจจุบัน
        if (HasStateAuthority && CurrentState == EnemyState.Patrolling)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(PatrolTarget, patrolTargetRadius);

            // วาดเส้นไปยังจุดหมาย
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, PatrolTarget);
        }

        // วาดระยะ detect
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        // วาดระยะโจมตี
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, AttackRange);
    }

    #region // ========== Combat RPCs ==========
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PerformAttack(PlayerRef targetPlayer)
    {
        // Find the target hero
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
            Debug.Log($"Enemy {name} attempts to attack {targetHero.CharacterName}!");

            // 🎯 ใช้ TakeDamageFromAttacker() แทน TakeDamage() เพื่อให้มีการเช็ค Hit/Miss
            targetHero.TakeDamageFromAttacker(AttackDamage, MagicDamage, this, DamageType.Normal);

            // 🎯 Status effects จะใส่เฉพาะเมื่อโจมตีโดนเท่านั้น (ย้ายไปใส่หลังจาก hit success)
            // ลบส่วนนี้ออกจากที่นี่ แล้วย้ายไปใส่ใน OnSuccessfulAttack
        }
    }

    public void OnSuccessfulAttack(Character target)
    {
        if (!HasStateAuthority) return;

        // 30% โอกาสติด status effects เฉพาะเมื่อโจมตีโดน
        if (Random.Range(0f, 100f) <= 30f)
        {
            Debug.Log($"Enemy applies status effects to {target.CharacterName}!");

            // สุ่ม status effect ที่จะใส่
            float effectRoll = Random.Range(0f, 100f);

            if (effectRoll < 15f)
            {
                target.ApplyStatusEffect(StatusEffectType.Poison, 3, 5f);
                Debug.Log("Applied Poison!");
            }
            else if (effectRoll < 30f)
            {
                target.ApplyStatusEffect(StatusEffectType.Burn, 4, 4f);
                Debug.Log("Applied Burn!");
            }
            else if (effectRoll < 45f)
            {
                target.ApplyStatusEffect(StatusEffectType.Bleed, 2, 8f);
                Debug.Log("Applied Bleed!");
            }
            else if (effectRoll < 60f)
            {
                target.ApplyStatusEffect(StatusEffectType.Stun, 0, 2f);
                Debug.Log("Applied Stun!");
            }
            else if (effectRoll < 75f)
            {
                target.ApplyStatusEffect(StatusEffectType.Freeze, 0, 3f);
                Debug.Log("Applied Freeze!");
            }
            else if (effectRoll < 85f)
            {
                target.ApplyStatusEffect(StatusEffectType.ArmorBreak, 0, 8f, 0.5f);
                Debug.Log("Applied Armor Break!");
            }
            else if (effectRoll < 95f)
            {
                target.ApplyStatusEffect(StatusEffectType.Blind, 0, 6f, 0.8f);
                Debug.Log("Applied Blind!");
            }
            else
            {
                target.ApplyStatusEffect(StatusEffectType.Weakness, 0, 10f, 0.4f);
                Debug.Log("Applied Weakness!");
            }
        }
    }

    // ใน NetworkEnemy.cs - แก้ไข RPC_OnDeath()
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnDeath()
    {
        Debug.Log($"Enemy {name} died!");
        IsDead = true;

        // 🆕 ทำ drops และ session kill counting ทันทีเมื่อตาย
        if (HasStateAuthority)
        {
            // 1. Currency drops (เงิน + เพชร)
            EnemyDropManager currencyDropManager = GetComponent<EnemyDropManager>();
            if (currencyDropManager != null)
            {
                currencyDropManager.TriggerDrops();
            }

            // 2. Item drops
            ItemDropManager itemDropManager = GetComponent<ItemDropManager>();
            if (itemDropManager != null)
            {
                itemDropManager.TriggerItemDrops();
            }

            // 3. Experience drops
            DropExpToNearbyHeroes();

            // 🆕 4. บอก EnemySpawner ว่า enemy ตัวนี้ตายแล้ว (สำหรับ session kills)
            NotifySpawnerOfDeath();

            // 5. Global kill tracking
        }

        // Death visual effects...
        Renderer enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = Color.gray;
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        StatusEffectManager statusManager = GetComponent<StatusEffectManager>();
        if (statusManager != null)
        {
            statusManager.ClearAllStatusEffects();
        }
        StageRewardTracker.AddEnemyKill();

        StartCoroutine(DestroyAfterDelay());
    }

    // 🆕 Method ใหม่สำหรับแจ้ง EnemySpawner
    private void NotifySpawnerOfDeath()
    {
        // หา EnemySpawner ในเกม
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();

        if (spawner != null)
        {
            // ได้ enemy type name จาก character stats หรือ object name
            string enemyTypeName = GetEnemyTypeName();

            Debug.Log($"🔥 Notifying spawner of death: {enemyTypeName}");

            // เรียก method ใหม่ใน EnemySpawner
            spawner.OnEnemyDeath(this, enemyTypeName);
        }
        else
        {
            Debug.LogWarning($"🔥 No EnemySpawner found! Cannot update session kills for {name}");
        }
    }

    // 🆕 Method หา enemy type name
    private string GetEnemyTypeName()
    {
        // ลองหาจาก CharacterName ก่อน
        if (!string.IsNullOrEmpty(CharacterName))
        {
            return CharacterName;
        }

        // ถ้าไม่มี ใช้ชื่อ object แทน (ตัด (Clone) ออก)
        string objectName = name.Replace("(Clone)", "").Trim();
        return objectName;
    }

    // 🆕 เพิ่ม method ใหม่สำหรับ drop items











    private int GetEnemyLevel()
    {
        LevelManager enemyLevel = GetComponent<LevelManager>();
        return enemyLevel?.CurrentLevel ?? 1;
    }

   

   

    private void DropExpToNearbyHeroes()
    {
        // หา Characters ในระยะ 15 เมตร
        Collider[] heroColliders = Physics.OverlapSphere(transform.position, 15f, LayerMask.GetMask("Player"));
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
            // คำนวณ exp ที่จะให้
            int baseExp = 25;

            // Bonus exp จาก level ของ enemy
            LevelManager enemyLevel = GetComponent<LevelManager>();
            if (enemyLevel != null)
            {
                baseExp += (enemyLevel.CurrentLevel - 1) * 10;
            }

            // แบ่ง exp ให้ characters
            int expPerCharacter = Mathf.Max(1, baseExp / nearbyCharacters.Count);

            foreach (Character character in nearbyCharacters)
            {
                // 🔧 ใช้ method จาก Character base class
                character.GainExp(expPerCharacter);
                Debug.Log($"💰 {name} dropped {expPerCharacter} exp to {character.CharacterName}");
            }
        }
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(2f);

        if (HasStateAuthority && Object != null)
        {
            Runner.Despawn(Object);
        }
    }

    #endregion

    // ========== 💥 Collision Damage System ==========
    public virtual void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            if (showDebugInfo)
                Debug.Log($"{CharacterName}: Collision with player detected - using new attack pattern");

            isCollidingWithPlayer = true;
            // ไม่ทำ damage - ให้ state machine จัดการ
        }
    }

    public virtual void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            // ไม่ทำ damage ต่อเนื่อง - ให้ state machine จัดการ
            if (showDebugInfo && StateTimer > 2f) // แสดงทุก 2 วินาที
            {
                Debug.Log($"{CharacterName}: Still near player - State: {CurrentState}");
            }
        }
    }

    public virtual void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            isCollidingWithPlayer = false;
            if (showDebugInfo)
                Debug.Log($"{CharacterName}: Stopped colliding with player");
        }
    }

    private void TryDealCollisionDamage(GameObject playerObject)
    {
        // ตรวจสอบว่าพ้นช่วงเวลาคูลดาวน์หรือยัง
        if (Time.time >= nextCollisionDamageTime)
        {
            Hero hero = playerObject.GetComponent<Hero>();
            if (hero != null)
            {
                // คำนวณดาเมจจากการชน
                int collisionDamage = Mathf.RoundToInt(AttackDamage * collisionDamageMultiplier);
                if (collisionDamage < 1) collisionDamage = 1;

                // ทำดาเมจผ่าน RPC
                RPC_DealCollisionDamage(hero.Object, collisionDamage);

                // ตั้งค่าคูลดาวน์ใหม่
                nextCollisionDamageTime = Time.time + collisionDamageCooldown;

                if (showDebugInfo)
                {
                    Debug.Log($"{CharacterName}: Collision damage {collisionDamage} to {hero.CharacterName}");
                }
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_DealCollisionDamage(NetworkObject heroObject, int damage)
    {
        if (heroObject != null)
        {
            Hero hero = heroObject.GetComponent<Hero>();
            if (hero != null && hero.HasInputAuthority)
            {
                hero.TakeDamage(damage, DamageType.Normal, false);
                Debug.Log($"Enemy collision damage: {damage} to {hero.CharacterName}");
            }
        }
    }
}