using UnityEngine;
using Fusion;

public class AutoFarmController : NetworkBehaviour
{
    [Header("⚙️ Auto Farm Settings")]
    [Tooltip("กี่วินาทีที่ไม่มี input จะเริ่มฟาร์มอัตโนมัติ")]
    public float idleTimeToStart = 5f;

    [Tooltip("รัศมีการค้นหา Dummy")]
    public float detectionRange = 20f;

    [Tooltip("ระยะห่างเพิ่มเติมจาก AttackRange ที่ยังถือว่าอยู่ในระยะ")]
    public float attackRangeBuffer = 2.5f;

    [Header("⚔️ Combat Settings")]
    [Tooltip("ช่วงเวลาระหว่างการโจมตี (วินาที)")]
    public float autoAttackInterval = 1.5f;

    [Tooltip("ช่วงเวลาระหว่างการเช็ค Skill (วินาที)")]
    public float skillCheckInterval = 2f;

    [Header("🎯 Skill Settings")]
    [Tooltip("ใช้ Skill อัตโนมัติ")]
    public bool useSkillsAutomatically = true;

    [Tooltip("ลำดับ Skill ที่จะใช้ (1-4)")]
    public int[] skillPriority = new int[] { 1, 2, 3, 4 };

    [Header("🔧 Auto Range Adjustment")]
    [Tooltip("เวลาที่รอก่อนปรับ buffer (วินาที)")]
    public float timeBeforeAdjustBuffer = 3f;

    [Tooltip("ระยะถอยหลังเมื่อไม่สามารถโจมตีได้")]
    public float retreatDistance = 3f;
    [Header("🎯 Target Settings")]
    [Tooltip("โจมตี Dummy")]
    public bool targetDummies = true;

    [Tooltip("โจมตี Enemy ทั่วไป")]
    public bool targetEnemies = true;

    [Tooltip("ความสำคัญ: Dummy ก่อน หรือ Enemy ก่อน")]
    public TargetPriority targetPriority = TargetPriority.NearestFirst;

    public enum TargetPriority
    {
        NearestFirst,      // ใกล้สุดก่อน
        DummyFirst,        // Dummy ก่อน
        EnemyFirst         // Enemy ก่อน
    }
    [Header("📊 Debug")]
    public bool showDebugLogs = true;

    // === Private Variables ===
    private Hero hero;
    private NetworkEnemy currentTarget;
    private bool isAutoFarmActive = false;
    private bool isLobbyScene = false;

    // Timing
    private float lastInputTime;
    private float lastAutoAttackTime;
    private float lastSkillCheckTime;

    // ✅ Auto Range Adjustment Variables
    private float autoFarmStartTime;
    private float lastAttackAttemptTime; // เวลาที่พยายามโจมตีครั้งล่าสุด
    private bool hasSuccessfulAttack;
    private bool isRangeAdjustmentLocked;
    private float currentAttackRangeBuffer;
    private int adjustmentAttempts;

    // ✅ Retreat System Variables
    private bool isRetreating = false;
    private Vector3 retreatTargetPosition;
    private float retreatStartTime;
    private const float RETREAT_DURATION = 1.5f; // เวลาในการถอยหลัง

    // Constants
    private const float BUFFER_MIN = 1f;
    private const float BUFFER_MAX = 2.5f;
    private const int MAX_ADJUSTMENT_ATTEMPTS = 8; // จำกัดการปรับ

    // ========================================
    // INITIALIZATION
    // ========================================

    public override void Spawned()
    {
        base.Spawned();

        hero = GetComponent<Hero>();
        if (hero == null)
        {
            LogError("No Hero component found!");
            enabled = false;
            return;
        }

        // ลบหรือ comment ส่วนนี้ถ้าต้องการใช้ auto farm ในทุก scene
        /*
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        isLobbyScene = sceneName.ToLower().Contains("lobby");

        if (!isLobbyScene)
        {
            Log("Not in Lobby scene, disabling auto farm");
            enabled = false;
            return;
        }
        */

        // เปลี่ยนเป็น:
        isLobbyScene = true; // หรือลบ isLobbyScene check ออกทั้งหมด

        lastInputTime = Time.time;
        lastAutoAttackTime = Time.time;
        lastSkillCheckTime = Time.time;
        lastAttackAttemptTime = Time.time;

        currentAttackRangeBuffer = attackRangeBuffer;
        ResetRangeAdjustment();

        Log($"✅ Auto Farm initialized for {hero.CharacterName}");
    }

    // ========================================
    // UPDATE LOOPS
    // ========================================

    private void Update()
    {
        if (!HasInputAuthority || !isLobbyScene) return;

        if (HasAnyInput())
        {
            lastInputTime = Time.time;

            if (isAutoFarmActive)
            {
                DeactivateAutoFarm();
            }
        }
        else
        {
            float idleTime = Time.time - lastInputTime;

            if (!isAutoFarmActive && idleTime >= idleTimeToStart)
            {
                ActivateAutoFarm();
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority || !isAutoFarmActive) return;

        // ถ้ากำลังถอยหลัง จัดการ retreat
        if (isRetreating)
        {
            HandleRetreating();
            return;
        }

        if (currentTarget == null || currentTarget.IsDead)
        {
            FindNearestTarget();  // ← เปลี่ยนจาก FindNearestDummy()
        }

        if (currentTarget == null || currentTarget.IsDead) return;

        // ✅ ตรวจสอบและปรับ attack range
        CheckAndAdjustAttackRange();

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        float effectiveAttackRange = hero.AttackRange + currentAttackRangeBuffer;

        // ========== เดินเข้าหาเป้าหมาย ==========
        if (distance > effectiveAttackRange)
        {
            MoveTowardsTarget(distance, effectiveAttackRange);
        }
        // ========== อยู่ในระยะแล้ว - โจมตีและใช้ skill ==========
        else
        {
            StopMovement();

            // ลองใช้ Skill
            if (useSkillsAutomatically && Time.time >= lastSkillCheckTime + skillCheckInterval)
            {
                TryUseSkill();
                lastSkillCheckTime = Time.time;
            }

            // โจมตีปกติ
            if (Time.time >= lastAutoAttackTime + autoAttackInterval)
            {
                PerformAutoAttack();
                lastAutoAttackTime = Time.time;
                lastAttackAttemptTime = Time.time; // ✅ บันทึกเวลาที่พยายามโจมตี
            }
        }
    }

    // ========================================
    // INPUT DETECTION
    // ========================================
   
    private bool HasAnyInput()
    {
        bool hasKeyboardInput = Input.anyKey;
        bool hasMouseMovement = Mathf.Abs(Input.GetAxis("Mouse X")) > 0.1f ||
                                Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.1f;
        bool hasMouseClick = Input.GetMouseButton(0) || Input.GetMouseButton(1);
        bool hasJoystickInput = Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f ||
                                Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f;
        bool hasTouchInput = Input.touchCount > 0;

        return hasKeyboardInput || hasMouseMovement || hasMouseClick ||
               hasJoystickInput || hasTouchInput;
    }

    // ========================================
    // AUTO FARM CONTROL
    // ========================================

    private void ActivateAutoFarm()
    {
        isAutoFarmActive = true;
        lastAutoAttackTime = Time.time;
        lastSkillCheckTime = Time.time;

        autoFarmStartTime = Time.time;
        lastAttackAttemptTime = Time.time;
        ResetRangeAdjustment();

        Log($"🤖 Auto Farm ACTIVATED (Buffer: {currentAttackRangeBuffer})");
    }

    private void DeactivateAutoFarm()
    {
        isAutoFarmActive = false;
        isRetreating = false;
        currentTarget = null;
        Log("❌ Auto Farm DEACTIVATED");
    }

    // ========================================
    // TARGET SELECTION
    // ========================================

    private void FindNearestTarget()
    {
        NetworkEnemy[] allEnemies = FindObjectsOfType<NetworkEnemy>();

        NetworkEnemy nearest = null;
        float nearestDistance = float.MaxValue;

        // สำหรับ priority system
        NetworkEnemy nearestDummy = null;
        float nearestDummyDistance = float.MaxValue;
        NetworkEnemy nearestEnemy = null;
        float nearestEnemyDistance = float.MaxValue;

        foreach (NetworkEnemy enemy in allEnemies)
        {
            if (enemy.IsDead) continue;

            // ตรวจสอบว่าเป็น Dummy หรือ Enemy
            bool isDummy = enemy.IsDummy;

            // ข้ามถ้าไม่ต้องการ target ประเภทนี้
            if (isDummy && !targetDummies) continue;
            if (!isDummy && !targetEnemies) continue;

            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            if (distance > detectionRange) continue;

            // เก็บแยกตามประเภท
            if (isDummy)
            {
                if (distance < nearestDummyDistance)
                {
                    nearestDummy = enemy;
                    nearestDummyDistance = distance;
                }
            }
            else
            {
                if (distance < nearestEnemyDistance)
                {
                    nearestEnemy = enemy;
                    nearestEnemyDistance = distance;
                }
            }

            // เก็บใกล้สุดโดยรวม
            if (distance < nearestDistance)
            {
                nearest = enemy;
                nearestDistance = distance;
            }
        }

        // เลือก target ตาม priority
        switch (targetPriority)
        {
            case TargetPriority.DummyFirst:
                currentTarget = nearestDummy ?? nearestEnemy;
                break;

            case TargetPriority.EnemyFirst:
                currentTarget = nearestEnemy ?? nearestDummy;
                break;

            case TargetPriority.NearestFirst:
            default:
                currentTarget = nearest;
                break;
        }

        if (currentTarget != null)
        {
            string targetType = currentTarget.IsDummy ? "Dummy" : "Enemy";
            float dist = Vector3.Distance(transform.position, currentTarget.transform.position);
            Log($"🎯 Target locked: {targetType} ({currentTarget.CharacterName}) at {dist:F1}m");
        }
    }

    // ========================================
    // MOVEMENT
    // ========================================

    private void MoveTowardsTarget(float currentDistance, float targetRange)
    {
        Vector3 direction = (currentTarget.transform.position - transform.position).normalized;
        direction.y = 0;

        Rigidbody rb = hero.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 newPosition = transform.position + direction * hero.MoveSpeed * Runner.DeltaTime;
            rb.MovePosition(newPosition);
        }

        Log($"🚶 Moving to target: {currentDistance:F1}m / {targetRange:F1}m");
    }

    private void StopMovement()
    {
        Rigidbody rb = hero.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 currentVel = rb.linearVelocity;
            rb.linearVelocity = new Vector3(0, currentVel.y, 0);
        }
    }

    // ========================================
    // RETREAT SYSTEM
    // ========================================

    /// <summary>
    /// เริ่มการถอยหลังจาก Dummy
    /// </summary>
    private void StartRetreat()
    {
        if (currentTarget == null) return;

        isRetreating = true;
        retreatStartTime = Time.time;

        // คำนวณทิศทางถอยหลัง (ตรงข้ามกับ Dummy)
        Vector3 directionFromTarget = (transform.position - currentTarget.transform.position).normalized;
        directionFromTarget.y = 0;

        // ตำแหน่งเป้าหมายที่จะถอยหลังไป
        retreatTargetPosition = transform.position + directionFromTarget * retreatDistance;

        Log($"🏃 Starting retreat! Target position: {retreatTargetPosition}");
    }

    /// <summary>
    /// จัดการการเคลื่อนที่ถอยหลัง
    /// </summary>
    private void HandleRetreating()
    {
        float elapsedTime = Time.time - retreatStartTime;

        // ถ้าถอยหลังครบเวลาแล้ว
        if (elapsedTime >= RETREAT_DURATION)
        {
            isRetreating = false;
            Log("✅ Retreat complete! Adjusting range buffer...");

            // ปรับ buffer หลังจากถอยหลังเสร็จ
            AdjustAttackRangeBuffer();
            return;
        }

        // เคลื่อนที่ไปยังตำแหน่งเป้าหมาย
        Vector3 direction = (retreatTargetPosition - transform.position).normalized;
        direction.y = 0;

        float distance = Vector3.Distance(transform.position, retreatTargetPosition);

        if (distance > 0.5f) // ยังไม่ถึงเป้าหมาย
        {
            Rigidbody rb = hero.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 newPosition = transform.position + direction * hero.MoveSpeed * Runner.DeltaTime;
                rb.MovePosition(newPosition);
            }

            Log($"🏃 Retreating... Distance: {distance:F1}m");
        }
    }

    // ========================================
    // RANGE ADJUSTMENT SYSTEM
    // ========================================

    /// <summary>
    /// ตรวจสอบและปรับ attack range buffer
    /// </summary>
    private void CheckAndAdjustAttackRange()
    {
        if (isRangeAdjustmentLocked || isRetreating) return;

        float timeSinceLastAttack = Time.time - lastAttackAttemptTime;

        // ถ้าพยายามโจมตีแล้วไม่สำเร็จเกิน timeBeforeAdjustBuffer
        if (!hasSuccessfulAttack && timeSinceLastAttack >= timeBeforeAdjustBuffer)
        {
            Log($"⚠️ No successful attack for {timeSinceLastAttack:F1}s - Starting retreat!");
            StartRetreat(); // ถอยหลังก่อน แล้วค่อยปรับ buffer
        }
    }

    /// <summary>
    /// ปรับ attack range buffer (เรียกหลังจากถอยหลังเสร็จ)
    /// </summary>
    private void AdjustAttackRangeBuffer()
    {
        adjustmentAttempts++;

        // สลับระหว่าง BUFFER_MIN และ BUFFER_MAX
        if (Mathf.Approximately(currentAttackRangeBuffer, BUFFER_MAX))
        {
            currentAttackRangeBuffer = BUFFER_MIN;
            Log($"🔧 Adjusting buffer: {BUFFER_MAX} → {BUFFER_MIN} (Attempt #{adjustmentAttempts})");
        }
        else
        {
            currentAttackRangeBuffer = BUFFER_MAX;
            Log($"🔧 Adjusting buffer: {BUFFER_MIN} → {BUFFER_MAX} (Attempt #{adjustmentAttempts})");
        }

        // Reset timers
        lastAttackAttemptTime = Time.time;

        // ป้องกันการปรับบ่อยเกินไป
        if (adjustmentAttempts >= MAX_ADJUSTMENT_ATTEMPTS)
        {
            isRangeAdjustmentLocked = true;
            LogError($"⛔ Range adjustment limit reached! Locked at {currentAttackRangeBuffer}");
        }
    }

    /// <summary>
    /// Reset ระบบปรับ range
    /// </summary>
    private void ResetRangeAdjustment()
    {
        hasSuccessfulAttack = false;
        isRangeAdjustmentLocked = false;
        adjustmentAttempts = 0;
        currentAttackRangeBuffer = attackRangeBuffer;
        isRetreating = false;
    }

    // ========================================
    // COMBAT - SKILLS
    // ========================================

    private void TryUseSkill()
    {
        Log("🌟 Checking available skills...");

        foreach (int skillNum in skillPriority)
        {
            if (CanUseSkill(skillNum))
            {
                UseSkill(skillNum);
                Log($"✅ Used Skill {skillNum}!");
                return;
            }
        }

        Log("⚠️ No skills available");
    }

    private bool CanUseSkill(int skillNumber)
    {
        switch (skillNumber)
        {
            case 1: return hero.CanUseSkill1();
            case 2: return hero.CanUseSkill2();
            case 3: return hero.CanUseSkill3();
            case 4: return hero.CanUseSkill4();
            default: return false;
        }
    }

    private void UseSkill(int skillNumber)
    {
        switch (skillNumber)
        {
            case 1:
                hero.UseSkill1();
                break;
            case 2:
                hero.UseSkill2();
                break;
            case 3:
                hero.UseSkill3();
                break;
            case 4:
                hero.UseSkill4();
                break;
        }
    }

    // ========================================
    // COMBAT - BASIC ATTACK
    // ========================================

    private void PerformAutoAttack()
    {
        Log($"⚔️ Auto attacking... (Buffer: {currentAttackRangeBuffer})");

        // เก็บ nextAttackTime ก่อนโจมตี
        float previousNextAttackTime = hero.GetNextAttackTime();

        hero.TryAttack();

        // เช็คว่าโจมตีสำเร็จหรือไม่ (nextAttackTime เปลี่ยนไหม)
        float currentNextAttackTime = hero.GetNextAttackTime();

        if (currentNextAttackTime > previousNextAttackTime)
        {
            // โจมตีสำเร็จ! (cooldown เปลี่ยน)
            if (!hasSuccessfulAttack)
            {
                hasSuccessfulAttack = true;
                isRangeAdjustmentLocked = true;
                Log($"✅ First attack successful! Range adjustment locked at {currentAttackRangeBuffer}");
            }
        }
        else
        {
            // โจมตีไม่สำเร็จ
            Log($"❌ Attack failed! (out of range or on cooldown)");
        }
    }

    // ========================================
    // DEBUG & VISUALIZATION
    // ========================================

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[AutoFarm] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[AutoFarm] ❌ {message}");
    }

    private void OnDrawGizmosSelected()
    {
        if (!enabled) return;

        // Detection range (สีเหลือง)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Attack range (สีเขียว)
        if (hero != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, hero.AttackRange + currentAttackRangeBuffer);
        }

        // เส้นไปหาเป้าหมาย (สีแดง)
        if (currentTarget != null && isAutoFarmActive)
        {
            Gizmos.color = isRetreating ? Color.blue : Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
        }

        // Retreat target (สีน้ำเงิน)
        if (isRetreating)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(retreatTargetPosition, 0.5f);
            Gizmos.DrawLine(transform.position, retreatTargetPosition);
        }
    }

    // ========================================
    // DEBUG COMMANDS
    // ========================================

    [ContextMenu("Force Activate Auto Farm")]
    public void ForceActivate()
    {
        ActivateAutoFarm();
    }

    [ContextMenu("Force Deactivate Auto Farm")]
    public void ForceDeactivate()
    {
        DeactivateAutoFarm();
    }

    [ContextMenu("Force Start Retreat")]
    public void ForceStartRetreat()
    {
        StartRetreat();
    }

    [ContextMenu("Reset Range Adjustment")]
    public void DebugResetRange()
    {
        ResetRangeAdjustment();
        Log($"Range adjustment reset! Current buffer: {currentAttackRangeBuffer}");
    }
}