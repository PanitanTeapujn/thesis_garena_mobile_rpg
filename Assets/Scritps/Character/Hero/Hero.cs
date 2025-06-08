using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;

public class Hero : Character
{
    // ========== Network Properties ==========
    [Networked] public Vector3 NetworkedPosition { get; set; }
    [Networked] public Vector3 NetworkedVelocity { get; set; }
    [Networked] public bool NetworkedFlipX { get; set; }
    [Networked] public float NetworkedYRotation { get; set; }
    [Networked] public Vector3 NetworkedScale { get; set; }

    [Networked] public int NetworkedCurrentHp { get; set; }
    [Networked] public int NetworkedMaxHp { get; set; }
    [Networked] public int NetworkedCurrentMana { get; set; }
    [Networked] public int NetworkedMaxMana { get; set; }
    [Networked] public bool IsNetworkStateReady { get; set; }

    [Networked] public TickTimer AttackCooldownTimer { get; set; }
    float nextAttackTime = 0f;

    private float AttackRange = 2f; // Override from Character
    private LayerMask enemyLayer;
    // Network input data
    protected NetworkInputData networkInputData;
    protected float currentCameraAngle = 0f;
    private NetworkInputData prevInputData;
    private bool skill1Consumed = false;
    private bool skill2Consumed = false;
    private bool skill3Consumed = false;
    private bool skill4Consumed = false;

    // ========== Camera Properties ==========
    [Header("Camera")]
    public Vector3 moveDirection;
    public Transform cameraTransform;
    public float cameraRotationSpeed = 50f;
    public Vector3 cameraOffset = new Vector3(0, 12, -13);

    public float cameraFollowSpeed = 8f;
    public float cameraRotationSmoothTime = 0.1f;
    private Vector3 cameraVelocity;
    private Vector3 lastPlayerPosition;

    [Header("Move")]
    public float moveInputX;
    public float moveInputZ;

    [Header("Movement Settings")]
    private float movementDeadZone = 0.2f; // Dead zone สำหรับ movement input
    private Vector2 lastMovementInput = Vector2.zero;
    private Vector3 lastVelocity = Vector3.zero;
    private float movementSmoothTime = 0.1f;

    [Header("Rotation Settings")]
    private float lastYRotation = 0f;
    private float rotationThreshold = 2f; // threshold สำหรับการเปลี่ยน rotation
    [Header("Skill Cooldowns")]
    public float skill1Cooldown = 1f;
    public float skill2Cooldown = 1f;
    public float skill3Cooldown = 1f;
    public float skill4Cooldown = 1f;

    private float nextSkill1Time = 0f;
    private float nextSkill2Time = 0f;
    private float nextSkill3Time = 0f;
    private float nextSkill4Time = 0f;


    protected override void Start()
    {
        base.Start();
        InitializeCombat();
        // Debug.Log($"[SPAWNED] {gameObject.name} - Input: {HasInputAuthority}, State: {HasStateAuthority}");
        SetRotationThreshold(3f);
        SetMovementDeadZone(0.1f);
        // กำหนดค่าเริ่มต้นของ scale
        NetworkedScale = transform.localScale;

        // Setup camera เฉพาะ local player
        if (HasInputAuthority)
        {
            cameraTransform = Camera.main?.transform;
        }
    }
    private void LateUpdate()
    {
        // Camera logic เฉพาะ local player
        if (HasInputAuthority && cameraTransform != null)
        {
            UpdateCameraSmooth();
        }
    }
    private void UpdateCameraSmooth()
    {
        // ใช้ position ที่ smooth แล้ว
        Vector3 targetPosition = transform.position;

        // Smooth position changes เพื่อลด jitter
        if (Vector3.Distance(lastPlayerPosition, targetPosition) > 0.01f)
        {
            lastPlayerPosition = Vector3.Lerp(lastPlayerPosition, targetPosition, Time.deltaTime * cameraFollowSpeed);
        }

        // คำนวณ camera position ที่ต้องการ
        Quaternion cameraRotation = Quaternion.AngleAxis(currentCameraAngle, Vector3.up);
        Vector3 desiredPosition = lastPlayerPosition + cameraRotation * cameraOffset;

        // Smooth camera movement
        cameraTransform.position = Vector3.SmoothDamp(
            cameraTransform.position,
            desiredPosition,
            ref cameraVelocity,
            cameraRotationSmoothTime
        );

        // Smooth camera look at
        Vector3 lookTarget = lastPlayerPosition + Vector3.up * 1.5f; // เพิ่มความสูงเล็กน้อย
        Vector3 targetDirection = (lookTarget - cameraTransform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

        cameraTransform.rotation = Quaternion.Slerp(
            cameraTransform.rotation,
            targetRotation,
            Time.deltaTime * cameraFollowSpeed
        );
    }
    private void InitializeCombat()
    {
        enemyLayer = LayerMask.GetMask("Enemy");

        if (HasStateAuthority)
        {
            NetworkedMaxHp = MaxHp;
            NetworkedCurrentHp = CurrentHp;
            NetworkedMaxMana = MaxMana;
            NetworkedCurrentMana = CurrentMana;
        }
    }
    // ========== Network Update ==========
    public override void FixedUpdateNetwork()
    {
        // Client ที่มี InputAuthority แต่ไม่มี StateAuthority
        if (HasInputAuthority && !HasStateAuthority)
        {
            if (GetInput(out networkInputData))
            {
                ProcessMovement();
                ProcessCameraRotation();
                ProcessCharacterFacing();

                // ส่ง RPC ไปให้ Host อัพเดท position
                RPC_UpdatePosition(transform.position, rb.velocity, transform.localScale, transform.eulerAngles.y);
            }
        }
        // Host หรือ Server ที่มี StateAuthority
        else if (HasStateAuthority)
        {
            // อัพเดท Network Properties
            NetworkedPosition = transform.position;
            NetworkedScale = transform.localScale;
            NetworkedYRotation = transform.eulerAngles.y;
            NetworkedFlipX = transform.localScale.x < 0;

            if (rb != null)
            {
                NetworkedVelocity = rb.velocity;
            }

            // Process input ถ้าเป็น local player
            if (HasInputAuthority)
            {
                if (GetInput(out networkInputData))
                {
                    ProcessMovement();
                    ProcessCameraRotation();
                    ProcessCharacterFacing();
                }
            }
        }
        // Remote player - apply network state
        else
        {
            ApplyNetworkState();
        }

        // เรียก virtual method สำหรับ abilities เฉพาะของแต่ละ class
        ProcessClassSpecificAbilities();
    }

    // ========== Virtual Methods for Inheritance ==========
   

    // ========== RPC Methods ==========
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_UpdatePosition(Vector3 position, Vector3 velocity, Vector3 scale, float yRotation)
    {
        transform.position = position;
        if (rb != null) rb.velocity = velocity;
        transform.localScale = scale;
        transform.eulerAngles = new Vector3(0, yRotation, 0);
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

        // Scale synchronization
        float scaleDistance = Vector3.Distance(transform.localScale, NetworkedScale);
        if (scaleDistance > 0.01f)
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                NetworkedScale,
                Runner.DeltaTime * 15f
            );
        }

        // Rotation synchronization
        float targetYRotation = NetworkedYRotation;
        float currentYRotation = transform.eulerAngles.y;
        float rotationDifference = Mathf.DeltaAngle(currentYRotation, targetYRotation);

        if (Mathf.Abs(rotationDifference) > 1f)
        {
            Quaternion targetRotation = Quaternion.Euler(0, targetYRotation, 0);
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                Runner.DeltaTime * 10f
            );
        }

        // Flip synchronization
        Vector3 currentScale = transform.localScale;
        bool shouldFlipX = NetworkedFlipX;
        bool isCurrentlyFlipped = currentScale.x < 0;

        if (shouldFlipX != isCurrentlyFlipped)
        {
            currentScale.x = shouldFlipX ? -Mathf.Abs(currentScale.x) : Mathf.Abs(currentScale.x);
            transform.localScale = currentScale;
        }
    }

    // ========== Movement Processing ==========
    protected virtual void ProcessMovement()
    {
        if (!HasInputAuthority) return;

        Vector3 moveDirection = Vector3.zero;
        Vector2 currentInput = networkInputData.movementInput;

        // ลด dead zone ให้เหมาะสม และใช้เฉพาะ magnitude
        float inputMagnitude = currentInput.magnitude;
        if (inputMagnitude < 0.15f) // ลดจาก 0.2 เป็น 0.15
        {
            currentInput = Vector2.zero; // ใส่ zero หากน้อยกว่า dead zone
        }

        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            moveDirection = camForward * currentInput.y + camRight * currentInput.x;
        }

        if (rb != null)
        {
            if (moveDirection.magnitude > 0.1f)
            {
                Vector3 targetVelocity = new Vector3(
                    moveDirection.x * MoveSpeed,
                    rb.velocity.y,
                    moveDirection.z * MoveSpeed
                );

                // ใช้ Lerp แทน SmoothDamp เพื่อความง่าย
                Vector3 newVelocity = Vector3.Lerp(
                    rb.velocity,
                    targetVelocity,
                    Time.fixedDeltaTime * 15f // ปรับความเร็วการ lerp
                );

                // เซ็ต velocity เสมอเมื่อมีการเคลื่อนไหว
                rb.velocity = newVelocity;
                FlipCharacterNetwork(currentInput.x);
            }
            else
            {
                // Smooth stop
                Vector3 currentVel = rb.velocity;
                Vector3 stoppedVelocity = new Vector3(
                    Mathf.Lerp(currentVel.x, 0, Time.fixedDeltaTime * 10f),
                    currentVel.y,
                    Mathf.Lerp(currentVel.z, 0, Time.fixedDeltaTime * 10f)
                );

                rb.velocity = stoppedVelocity;
            }
        }

        // เก็บค่า input ล่าสุด
        lastMovementInput = currentInput;
    }

    public void SetMovementDeadZone(float deadZone)
    {
        movementDeadZone = Mathf.Clamp(deadZone, 0.1f, 0.5f);
    }

    public void SetMovementSmoothTime(float smoothTime)
    {
        movementSmoothTime = Mathf.Clamp(smoothTime, 0.05f, 0.3f);
    }
    protected virtual void ProcessCameraRotation()
    {
        if (!HasInputAuthority || cameraTransform == null) return;

        if (Mathf.Abs(networkInputData.cameraRotationInput) > 0.1f)
        {
            // ลด sensitivity เล็กน้อยเพื่อให้ smooth
            float rotationSpeed = cameraRotationSpeed * 0.7f;
            currentCameraAngle += networkInputData.cameraRotationInput * rotationSpeed * Runner.DeltaTime;
        }
    }

    protected virtual void ProcessCharacterFacing()
    {
        if (!HasInputAuthority || cameraTransform == null) return;

        Vector3 lookDir = cameraTransform.forward;
        lookDir.y = 0;

        if (lookDir != Vector3.zero)
        {
            // คำนวณ target rotation
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            float targetYRotation = targetRotation.eulerAngles.y;

            // ตรวจสอบความแตกต่างของ rotation
            float rotationDifference = Mathf.DeltaAngle(lastYRotation, targetYRotation);

            // อัพเดท rotation เฉพาะเมื่อมีการเปลี่ยนแปลงที่ชัดเจน
            if (Mathf.Abs(rotationDifference) > rotationThreshold)
            {
                // ใช้ Lerp เพื่อ smooth rotation แทนการเซ็ตตรงๆ
                transform.rotation = Quaternion.Lerp(
                    transform.rotation,
                    targetRotation,
                    Time.fixedDeltaTime * 8f
                );

                NetworkedYRotation = transform.eulerAngles.y;
                lastYRotation = NetworkedYRotation;
            }
        }
    }

    protected void FlipCharacterNetwork(float horizontalInput)
    {
        // เก็บ rotation ปัจจุบันไว้
        Quaternion currentRotation = transform.rotation;
        Vector3 newScale = transform.localScale;

        if (horizontalInput > 0.1f)
        {
            newScale.x = Mathf.Abs(newScale.x);
            NetworkedFlipX = false;
        }
        else if (horizontalInput < -0.1f)
        {
            newScale.x = -Mathf.Abs(newScale.x);
            NetworkedFlipX = true;
        }

        transform.localScale = newScale;

        // คืนค่า rotation เพื่อป้องกันการเปลี่ยนแปลงจาก scale
        transform.rotation = currentRotation;
    }
    public void SetRotationThreshold(float threshold)
    {
        rotationThreshold = Mathf.Clamp(threshold, 1f, 10f);
    }
    public void DebugNetworkState()
    {
        Debug.Log($"[DEBUG] {CharacterName} Network State:");
        Debug.Log($"  - HasInputAuthority: {HasInputAuthority}");
        Debug.Log($"  - HasStateAuthority: {HasStateAuthority}");
        Debug.Log($"  - IsNetworkStateReady: {IsNetworkStateReady}");
        Debug.Log($"  - CurrentHp: {CurrentHp} | NetworkedCurrentHp: {NetworkedCurrentHp}");
        Debug.Log($"  - MaxHp: {MaxHp} | NetworkedMaxHp: {NetworkedMaxHp}");
        Debug.Log($"  - CurrentMana: {CurrentMana} | NetworkedCurrentMana: {NetworkedCurrentMana}");
        Debug.Log($"  - MaxMana: {MaxMana} | NetworkedMaxMana: {NetworkedMaxMana}");
    }
    // ========== Original Methods (Non-Network) ==========
    protected override void Update()
    {
        base.Update();
        if (Time.time % 2.0f < Time.deltaTime)
        {
            DebugNetworkState();
        }
        if (HasInputAuthority)
        {
            CombatUIManager combatUI = FindObjectOfType<CombatUIManager>();
            if (combatUI != null && combatUI.localHero == this)
            {
                // Force update ทุก frame เพื่อให้แน่ใจว่า UI ได้รับข้อมูลล่าสุด
                combatUI.UpdateUI();
            }
        }

        // Camera follow สำหรับ local player

    }

    public void Move(Vector3 moveDirection)
    {
        // เก็บไว้สำหรับ non-network movement ถ้าจำเป็น
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();
        Vector3 adjustedMoveDirection = camForward * moveInputZ + camRight * moveInputX;
        rb.velocity = new Vector3(adjustedMoveDirection.x * MoveSpeed, rb.velocity.y, adjustedMoveDirection.z * MoveSpeed);
    }

    protected virtual void FlipCharacter()
    {
        if (moveInputX > 0)
        {
            transform.localScale = new Vector3(1f, 1f, 1f);
        }
        else if (moveInputX < 0)
        {
            transform.localScale = new Vector3(-1f, 1f, 1f);
        }
    }

    public void FollowCamera()
    {
        if (cameraTransform == null)
            return;
        cameraTransform.position = transform.position + cameraOffset;
        cameraTransform.LookAt(transform.position);
    }

    protected void RotateCharacterToCamera()
    {
        if (cameraTransform == null)
            return;
        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0;
        transform.forward = cameraForward;
    }

    protected void RotateCamera(float input)
    {
        if (cameraTransform == null)
            return;
        if (input != 0)
        {
            Quaternion rotation = Quaternion.AngleAxis(input * cameraRotationSpeed * Time.deltaTime, Vector3.up);
            cameraOffset = rotation * cameraOffset;
            cameraTransform.position = transform.position + cameraOffset;
            cameraTransform.LookAt(transform.position);
        }
    }

    // ========== Fusion Methods ==========
    public override void Spawned()
    {
        base.Spawned();

        // เพิ่ม delay เพื่อให้ network state setup เสร็จ
        StartCoroutine(OnSpawnComplete());
    }

    public override void Render()
    {
        // Visual interpolation สำหรับ remote players
        if (!HasInputAuthority)
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

    #region Combat

    protected virtual void ProcessClassSpecificAbilities()
    {
        if (HasInputAuthority)
        {
            if (GetInput(out networkInputData))
            {
                // Reset consumed flags เมื่อ input เป็น false
                if (!networkInputData.skill1) skill1Consumed = false;
                if (!networkInputData.skill2) skill2Consumed = false;
                if (!networkInputData.skill3) skill3Consumed = false;
                if (!networkInputData.skill4) skill4Consumed = false;

                // เช็ค Attack
                if (networkInputData.attack && Time.time >= nextAttackTime)
                {
                    TryAttack();
                    nextAttackTime = Time.time + AttackCooldown;
                }

                // เช็ค Skills พร้อม consumed flag และ cooldown
                if (networkInputData.skill1 && !skill1Consumed)
                {
                    if (Time.time >= nextSkill1Time)
                    {
                        TryUseSkill1();
                        nextSkill1Time = Time.time + skill1Cooldown;
                        skill1Consumed = true;
                        Debug.Log($"Skill1 used! Next available at: {nextSkill1Time:F2} (cooldown: {skill1Cooldown}s)");
                    }
                    else
                    {
                        float remainingCooldown = nextSkill1Time - Time.time;
                        Debug.Log($"Skill1 on cooldown! {remainingCooldown:F1}s remaining");
                    }
                }

                if (networkInputData.skill2 && !skill2Consumed)
                {
                    if (Time.time >= nextSkill2Time)
                    {
                        TryUseSkill2();
                        nextSkill2Time = Time.time + skill2Cooldown;
                        skill2Consumed = true;
                        Debug.Log($"Skill2 used! Next available at: {nextSkill2Time:F2} (cooldown: {skill2Cooldown}s)");
                    }
                    else
                    {
                        float remainingCooldown = nextSkill2Time - Time.time;
                        Debug.Log($"Skill2 on cooldown! {remainingCooldown:F1}s remaining");
                    }
                }

                if (networkInputData.skill3 && !skill3Consumed)
                {
                    if (Time.time >= nextSkill3Time)
                    {
                        TryUseSkill3();
                        nextSkill3Time = Time.time + skill3Cooldown;
                        skill3Consumed = true;
                        Debug.Log($"Skill3 used! Next available at: {nextSkill3Time:F2} (cooldown: {skill3Cooldown}s)");
                    }
                    else
                    {
                        float remainingCooldown = nextSkill3Time - Time.time;
                        Debug.Log($"Skill3 on cooldown! {remainingCooldown:F1}s remaining");
                    }
                }

                if (networkInputData.skill4 && !skill4Consumed)
                {
                    if (Time.time >= nextSkill4Time)
                    {
                        TryUseSkill4();
                        nextSkill4Time = Time.time + skill4Cooldown;
                        skill4Consumed = true;
                        Debug.Log($"Skill4 used! Next available at: {nextSkill4Time:F2} (cooldown: {skill4Cooldown}s)");
                    }
                    else
                    {
                        float remainingCooldown = nextSkill4Time - Time.time;
                        Debug.Log($"Skill4 on cooldown! {remainingCooldown:F1}s remaining");
                    }
                }
            }
        }

        // *** เพิ่มการ sync health และ mana ทุก frame สำหรับทุก client ***
        if (HasStateAuthority)
        {
            NetworkedCurrentHp = CurrentHp;
            NetworkedCurrentMana = CurrentMana;
            NetworkedMaxHp = MaxHp;
            NetworkedMaxMana = MaxMana;
        }

        // *** เพิ่มการ sync สำหรับ client ที่มี InputAuthority ***
        if (HasInputAuthority && !HasStateAuthority)
        {
            // ส่งข้อมูล HP/Mana ไปยัง server หากมีการเปลี่ยนแปลง
            if (NetworkedCurrentHp != CurrentHp || NetworkedCurrentMana != CurrentMana)
            {
                RPC_SyncHealthMana(CurrentHp, CurrentMana);
            }
        }
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SyncHealthMana(int newHp, int newMana)
    {
        CurrentHp = newHp;
        CurrentMana = newMana;
        NetworkedCurrentHp = newHp;
        NetworkedCurrentMana = newMana;
        Debug.Log($"Health/Mana synced via RPC: HP={newHp}, Mana={newMana} for {CharacterName}");
    }
    protected virtual void TryUseSkill1()
    {
        Debug.Log($"=== SKILL 1 EXECUTED at {Time.time:F2} ===");
        UseMana(10); // ตัวอย่างการใช้ mana
    }

    protected virtual void TryUseSkill2()
    {
        Debug.Log($"=== SKILL 2 EXECUTED at {Time.time:F2} ===");
        UseMana(15);
    }

    protected virtual void TryUseSkill3()
    {
        Debug.Log($"=== SKILL 3 EXECUTED at {Time.time:F2} ===");
        UseMana(20);
    }

    protected virtual void TryUseSkill4()
    {
        Debug.Log($"=== SKILL 4 EXECUTED at {Time.time:F2} ===");
        UseMana(25);
    }
    public void UseMana(int amount)
    {
        if (!HasInputAuthority) return;

        CurrentMana -= amount;
        CurrentMana = Mathf.Clamp(CurrentMana, 0, MaxMana);

        // Send mana info to server
        RPC_UpdateMana(CurrentMana);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_UpdateMana(int newMana)
    {
        CurrentMana = newMana;
        NetworkedCurrentMana = newMana;
    }
    public bool IsSpawned => Object != null && Object.IsValid;

    public void TryAttack()
    {
        if (!HasInputAuthority || !IsSpawned) return;

        if (Time.time < nextAttackTime) return;

        Collider[] enemies = Physics.OverlapSphere(transform.position, AttackRange, enemyLayer);

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
                RPC_PerformAttack(nearestEnemy.Object);
                nextAttackTime = Time.time + AttackCooldown;
                Debug.Log($"Hero attacking enemy at distance: {Vector3.Distance(transform.position, nearestEnemy.transform.position)}");
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_PerformAttack(NetworkObject enemyObject)
    {
        if (enemyObject != null)
        {
            NetworkEnemy enemy = enemyObject.GetComponent<NetworkEnemy>();
            if (enemy != null && !enemy.IsDead)
            {
                enemy.TakeDamage(AttackDamage, Object.InputAuthority);
                RPC_OnAttackHit(enemyObject);
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnAttackHit(NetworkObject enemyObject)
    {
        Debug.Log($"{CharacterName} hit enemy!");
        // TODO: Add animation / VFX here
    }
    public void TakeDamage(int damage)
    {
        if (!HasInputAuthority) return;

        CurrentHp -= damage;
        CurrentHp = Mathf.Clamp(CurrentHp, 0, MaxHp);
        RPC_UpdateHealth(CurrentHp);
        StartCoroutine(DamageFlash());

        Debug.Log($"{CharacterName} takes {damage} damage. HP: {CurrentHp}/{MaxHp}");

        if (CurrentHp <= 0)
        {
            RPC_OnDeath();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_UpdateHealth(int newHp)
    {
        CurrentHp = newHp;
        NetworkedCurrentHp = newHp;
        Debug.Log($"Health updated via RPC: {newHp} for {CharacterName}");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_OnDeath()
    {
        Debug.Log($"{CharacterName} died!");
        SceneManager.LoadScene("LoseScene");
    }

    private IEnumerator DamageFlash()
    {
        if (characterRenderer != null)
        {
            characterRenderer.material.color = Color.red;
            yield return new WaitForSeconds(0.2f);
            characterRenderer.material.color = originalColor;
        }
    }
    #endregion

    #region Ui
    public void ForceUpdateUI()
    {
        if (HasStateAuthority)
        {
            NetworkedMaxHp = MaxHp;
            NetworkedCurrentHp = CurrentHp;
            NetworkedMaxMana = MaxMana;
            NetworkedCurrentMana = CurrentMana;
            IsNetworkStateReady = true;
        }
    }
    private IEnumerator OnSpawnComplete()
    {
        yield return new WaitForSeconds(0.2f);

        // Initialize network properties
        if (HasStateAuthority)
        {
            NetworkedMaxHp = MaxHp;
            NetworkedCurrentHp = CurrentHp;
            NetworkedMaxMana = MaxMana;
            NetworkedCurrentMana = CurrentMana;
            IsNetworkStateReady = true;
        }

        // รอให้ network state sync เสร็จ
        yield return new WaitForSeconds(0.1f);

        // แจ้งให้ PlayerSpawner ทราบว่า spawn เสร็จแล้ว
        PlayerSpawner spawner = FindObjectOfType<PlayerSpawner>();
        if (spawner != null)
        {
            spawner.OnHeroSpawnComplete(this);
        }

        // แจ้งให้สร้าง WorldSpaceUI สำหรับทุกคน
        RPC_NotifyUISpawn();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyUISpawn()
    {
        // หา PlayerSpawner และขอให้สร้าง WorldSpaceUI
        PlayerSpawner spawner = FindObjectOfType<PlayerSpawner>();
        if (spawner != null)
        {
            spawner.CreateWorldSpaceUIForHero(this);
        }
    }
    #endregion
}