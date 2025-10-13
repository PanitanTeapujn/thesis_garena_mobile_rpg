using UnityEngine;
using Fusion;

/// <summary>
/// ระบบฟาร์มอัตโนมัติสำหรับ Hero ใน Lobby
/// เมื่อไม่มี input นาน 5 วินาที จะเริ่มฟาร์ม Dummy อัตโนมัติ
/// </summary>
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

    // ========================================
    // INITIALIZATION
    // ========================================

    public override void Spawned()
    {
        base.Spawned();

        // ตรวจสอบว่ามี Hero component
        hero = GetComponent<Hero>();
        if (hero == null)
        {
            LogError("No Hero component found!");
            enabled = false;
            return;
        }

        // ตรวจสอบว่าอยู่ใน Lobby scene
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        isLobbyScene = sceneName.ToLower().Contains("lobby");

        if (!isLobbyScene)
        {
            Log("Not in Lobby scene, disabling auto farm");
            enabled = false;
            return;
        }

        // Initialize timers
        lastInputTime = Time.time;
        lastAutoAttackTime = Time.time;
        lastSkillCheckTime = Time.time;

        Log($"✅ Auto Farm initialized for {hero.CharacterName}");
    }

    // ========================================
    // UPDATE LOOPS
    // ========================================

    private void Update()
    {
        // เฉพาะ local player เท่านั้น
        if (!HasInputAuthority || !isLobbyScene) return;

        // ตรวจสอบ input
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
            // ไม่มี input - เช็คว่าควรเปิด auto farm หรือไม่
            float idleTime = Time.time - lastInputTime;

            if (!isAutoFarmActive && idleTime >= idleTimeToStart)
            {
                ActivateAutoFarm();
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        // เฉพาะ local player และต้องเปิด auto farm
        if (!HasInputAuthority || !isAutoFarmActive) return;

        // หาเป้าหมายใหม่ถ้าไม่มีหรือตายแล้ว
        if (currentTarget == null || currentTarget.IsDead)
        {
            FindNearestDummy();
        }

        // ถ้าไม่มีเป้าหมาย ออกเลย
        if (currentTarget == null || currentTarget.IsDead) return;

        // คำนวณระยะห่าง
        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        float effectiveAttackRange = hero.AttackRange + attackRangeBuffer;

        // ========== เดินเข้าหาเป้าหมาย ==========
        if (distance > effectiveAttackRange)
        {
            MoveTowardsTarget(distance, effectiveAttackRange);
        }
        // ========== อยู่ในระยะแล้ว - โจมตีและใช้ skill ==========
        else
        {
            // หยุดเดิน
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
            }
        }
    }

    // ========================================
    // INPUT DETECTION
    // ========================================

    private bool HasAnyInput()
    {
        // Keyboard/Mouse
        bool hasKeyboardInput = Input.anyKey;
        bool hasMouseMovement = Mathf.Abs(Input.GetAxis("Mouse X")) > 0.1f ||
                                Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.1f;
        bool hasMouseClick = Input.GetMouseButton(0) || Input.GetMouseButton(1);

        // Controller
        bool hasJoystickInput = Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f ||
                                Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f;

        // Touch
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
        Log("🤖 Auto Farm ACTIVATED");
    }

    private void DeactivateAutoFarm()
    {
        isAutoFarmActive = false;
        currentTarget = null;
        Log("❌ Auto Farm DEACTIVATED");
    }

    // ========================================
    // TARGET SELECTION
    // ========================================

    private void FindNearestDummy()
    {
        NetworkEnemy[] allEnemies = FindObjectsOfType<NetworkEnemy>();

        NetworkEnemy nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (NetworkEnemy enemy in allEnemies)
        {
            // ต้องเป็น Dummy และยังไม่ตาย
            if (!enemy.IsDummy || enemy.IsDead) continue;

            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            // อยู่ในระยะและใกล้ที่สุด
            if (distance < detectionRange && distance < nearestDistance)
            {
                nearest = enemy;
                nearestDistance = distance;
            }
        }

        currentTarget = nearest;

        if (currentTarget != null)
        {
            Log($"🎯 Target locked: Dummy at {nearestDistance:F1}m");
        }
    }

    // ========================================
    // MOVEMENT
    // ========================================

    private void MoveTowardsTarget(float currentDistance, float targetRange)
    {
        Vector3 direction = (currentTarget.transform.position - transform.position).normalized;
        direction.y = 0; // เดินแนวราบเท่านั้น

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
            rb.linearVelocity = new Vector3(0, currentVel.y, 0); // คง gravity
        }
    }

    // ========================================
    // COMBAT - SKILLS
    // ========================================

    private void TryUseSkill()
    {
        Log("🌟 Checking available skills...");

        // ลองใช้ skill ตามลำดับความสำคัญ
        foreach (int skillNum in skillPriority)
        {
            if (CanUseSkill(skillNum))
            {
                UseSkill(skillNum);
                Log($"✅ Used Skill {skillNum}!");
                return; // ใช้ได้แล้ว ออกเลย
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
        Log("⚔️ Auto attacking...");
        hero.TryAttack();
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
            Gizmos.DrawWireSphere(transform.position, hero.AttackRange + attackRangeBuffer);
        }

        // เส้นไปหาเป้าหมาย (สีแดง)
        if (currentTarget != null && isAutoFarmActive)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
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

    [ContextMenu("Find Nearest Dummy")]
    public void DebugFindDummy()
    {
        FindNearestDummy();
    }

    [ContextMenu("Try Use Skill")]
    public void DebugUseSkill()
    {
        TryUseSkill();
    }
}