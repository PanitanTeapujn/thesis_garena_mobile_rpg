using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
public enum EnemyState
{
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

    [Header("🎯 Improved Movement Settings")]
    public float minDistanceToPlayer = 1.0f; // ระยะห่างขั้นต่ำจากผู้เล่น
    public float enemySpacing = 2.0f;        // ระยะห่างระหว่างศัตรูด้วยกัน
    public LayerMask enemyLayer;             // Layer ของศัตรู
    public bool useCircling = true;          // เปิดใช้การเคลื่อนที่แบบวนรอบผู้เล่น
    public float circlingSpeed = 0.5f;
    public bool isCollidingWithPlayer;
    [Header("🔧 Debug Settings")]
    public bool showDebugInfo = false;

    [Header("💥 Proximity Damage")]
    public float collisionDamageCooldown = 2.0f;
    public float collisionDamageMultiplier = 0.5f;
    private float nextCollisionDamageTime = 0f;

    private float nextTargetCheckTime = 0f;
    protected Transform targetTransform;
    private float nextAttackTime = 0f;


    // Check if properly spawned
    public bool IsSpawned => Object != null && Object.IsValid;

    // ========== Unity Lifecycle ==========
    protected override void Start()
    {
        base.Start();
        Debug.Log($"Enemy Start - HasStateAuthority: {HasStateAuthority}");


        // ตั้งค่า enemy layer
        if (enemyLayer == 0)
        {
            enemyLayer = LayerMask.GetMask("Enemy");
        }

        
        // ========== Debug ==========

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
            Debug.Log($"===============================");
        }
        LevelManager enemyLevel = GetComponent<LevelManager>();
        if (enemyLevel != null && HasStateAuthority)
        {
            // Set random level 1-5 for enemy
            int randomLevel = Random.Range(1, 6);
            while (enemyLevel.CurrentLevel < randomLevel)
            {
                enemyLevel.GainExp(enemyLevel.ExpToNextLevel);
            }
            Debug.Log($"Enemy {CharacterName} spawned at level {randomLevel}");
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
            CurrentState = EnemyState.Chasing; // เริ่มต้นด้วยการไล่ตาม
            StateTimer = 0f;

            // กำหนดค่าเริ่มต้นของ position และ scale
            NetworkedPosition = transform.position;
            NetworkedScale = transform.localScale;
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

    // ========== 🎯 Improved Movement System ==========
    protected virtual void ImprovedMoveTowardsTarget()
    {
        if (targetTransform == null || rb == null) return;

        Vector3 directionToPlayer = (targetTransform.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, targetTransform.position);
        Vector3 moveDirection = Vector3.zero;

        // อัพเดท State Timer
        StateTimer += Runner.DeltaTime;

        switch (CurrentState)
        {
            case EnemyState.Chasing:
                // ไล่ตามผู้เล่นปกติ
                if (distanceToPlayer <= AttackRange && distanceToPlayer > minDistanceToPlayer * 0.7f)
                {
                    // อยู่ในระยะโจมตีที่เหมาะสม เปลี่ยนเป็น Attacking
                    CurrentState = EnemyState.Attacking;
                    StateTimer = 0f;
                    moveDirection = Vector3.zero; // หยุดเคลื่อนที่
                    if (showDebugInfo)
                        Debug.Log($"{CharacterName}: Entering attack range! Distance: {distanceToPlayer:F2}");
                }
                else if (distanceToPlayer <= minDistanceToPlayer * 0.7f)
                {
                    // เข้าใกล้เกินไป เปลี่ยนเป็น BackingOff
                    CurrentState = EnemyState.BackingOff;
                    StateTimer = 0f;
                    if (showDebugInfo)
                        Debug.Log($"{CharacterName}: Too close! Backing off... Distance: {distanceToPlayer:F2}");
                }
                else
                {
                    // ไล่ตามผู้เล่นปกติ
                    moveDirection = directionToPlayer;
                }
                break;

            case EnemyState.BackingOff:
                // ถอยออกจากผู้เล่น
                moveDirection = -directionToPlayer * 1.2f; // ถอยเร็วหน่อย

                if (StateTimer >= backOffTime || distanceToPlayer >= backOffDistance)
                {
                    // ถอยพอแล้ว เปลี่ยนเป็น Positioning
                    CurrentState = EnemyState.Positioning;
                    StateTimer = 0f;
                    if (showDebugInfo)
                        Debug.Log($"{CharacterName}: Finished backing off, positioning... Distance: {distanceToPlayer:F2}");
                }
                break;

            case EnemyState.Positioning:
                // หยุดชั่วคราวก่อนเข้าโจมตี
                moveDirection = Vector3.zero;

                if (StateTimer >= positionTime)
                {
                    // พร้อมเข้าโจมตี
                    CurrentState = EnemyState.Chasing;
                    StateTimer = 0f;
                    if (showDebugInfo)
                        Debug.Log($"{CharacterName}: Ready to chase again! Distance: {distanceToPlayer:F2}");
                }
                break;

            case EnemyState.Attacking:
                // อยู่ในระยะโจมตี - ให้โจมตีได้เต็มที่
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
                        moveDirection = circleDirection * circlingSpeed * 0.3f;
                    }
                    else
                    {
                        moveDirection = Vector3.zero;
                    }

                    if (showDebugInfo && StateTimer > 1f) // แสดงทุก 1 วินาที
                    {
                        Debug.Log($"{CharacterName}: In attack state, ready to strike! Distance: {distanceToPlayer:F2}");
                    }
                }
                break;
        }

        // หลีกเลี่ยงศัตรูตัวอื่น
        Vector3 avoidanceForce = CalculateAvoidanceForce();
        moveDirection += avoidanceForce;

        // ทำให้เป็นทิศทางที่ถูกต้อง
        moveDirection.y = 0;
        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }

        // เคลื่อนที่
        if (moveDirection.magnitude > 0.1f)
        {
            float currentMoveSpeed = MoveSpeed;

            // เพิ่มความเร็วถ้าอยู่ในสถานะ BackingOff
            if (CurrentState == EnemyState.BackingOff)
            {
                currentMoveSpeed *= 1.3f; // ถอยเร็วขึ้น
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

        // Debug info
        if (showDebugInfo)
        {
            Debug.Log($"{CharacterName}: State={CurrentState}, Timer={StateTimer:F1}, Distance={distanceToPlayer:F1}, CanAttack={CurrentState == EnemyState.Attacking}");
        }
    }

    // 🔧 ระบบหลีกเลี่ยงศัตรูตัวอื่น
    private Vector3 CalculateAvoidanceForce()
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
    private void FlipCharacterTowardsMovement(Vector3 moveDirection)
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
    private void FindNearestPlayer()
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
            if (distance < detectRange && distance < nearestDistance)
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

    private void TryAttackTarget()
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
            nextAttackTime = Runner.SimulationTime + AttackCooldown;
            RPC_PerformAttack(CurrentTarget);

            if (showDebugInfo)
            {
                Debug.Log($"{CharacterName}: *** ATTACK EXECUTED! *** Distance: {distance:F2}, State: {CurrentState}");
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
            Debug.Log($"Enemy {name} attacks {targetHero.CharacterName} for {AttackDamage} damage!");

            // ทำ damage ปกติก่อน
            if (HasStateAuthority && Random.Range(0f, 100f) <= 30f) // 30% โอกาสติด status effects
            {
                Debug.Log($"Enemy applies status effects to {targetHero.CharacterName}!");

                // สุ่ม status effect ที่จะใส่
                float effectRoll = Random.Range(0f, 100f);

                if (effectRoll < 15f)
                {
                    targetHero.ApplyStatusEffect(StatusEffectType.Poison, 3, 5f);
                    Debug.Log("Applied Poison!");
                }
                else if (effectRoll < 30f)
                {
                    targetHero.ApplyStatusEffect(StatusEffectType.Burn, 4, 4f);
                    Debug.Log("Applied Burn!");
                }
                else if (effectRoll < 45f)
                {
                    targetHero.ApplyStatusEffect(StatusEffectType.Bleed, 2, 8f);
                    Debug.Log("Applied Bleed!");
                }
                else if (effectRoll < 60f)
                {
                    targetHero.ApplyStatusEffect(StatusEffectType.Stun, 0, 2f);
                    Debug.Log("Applied Stun!");
                }
                else if (effectRoll < 75f)
                {
                    targetHero.ApplyStatusEffect(StatusEffectType.Freeze, 0, 3f);
                    Debug.Log("Applied Freeze!");
                }
                else if (effectRoll < 85f)
                {
                    targetHero.ApplyStatusEffect(StatusEffectType.ArmorBreak, 0, 8f, 0.5f);
                    Debug.Log("Applied Armor Break!");
                }
                else if (effectRoll < 95f)
                {
                    targetHero.ApplyStatusEffect(StatusEffectType.Blind, 0, 6f, 0.8f);
                    Debug.Log("Applied Blind!");
                }
                else
                {
                    targetHero.ApplyStatusEffect(StatusEffectType.Weakness, 0, 10f, 0.4f);
                    Debug.Log("Applied Weakness!");
                }
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnDeath()
    {
        Debug.Log($"Enemy {name} died!");

        IsDead = true;

        // 🆕 Enemy drop exp ให้ heroes ใกล้เคียงก่อนตาย
        if (HasStateAuthority)
        {
            DropExpToNearbyHeroes();
        }

        // Death visual effects
        Renderer enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = Color.gray;
        }

        // Disable collider
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Clear all status effects when dead
        StatusEffectManager statusManager = GetComponent<StatusEffectManager>();
        if (statusManager != null)
        {
            statusManager.ClearAllStatusEffects();
        }

        // Destroy after delay
        StartCoroutine(DestroyAfterDelay());
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
    

    // ========== Context Menu Debug ==========
    
}