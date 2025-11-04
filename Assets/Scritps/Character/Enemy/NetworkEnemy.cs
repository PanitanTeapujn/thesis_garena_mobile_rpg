using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEngine.UI;

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
    public float detectRange = 50f;
    public float attackCheckInterval = 0.5f;

    [Header("🎯 Dummy System (Firebase-based)")]
    public bool IsDummy = false;
    public int DummyLevel = 1;                    // อ่านจาก Firebase
    private PersistentPlayerData persistentData;  // Reference to Firebase data

    public bool isLobbyDummy = false; // ตั้งค่าใน Inspector

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
    public float minDistanceToPlayer = 1.0f;
    public float enemySpacing = 2.0f;
    public LayerMask enemyLayer;
    public LayerMask wallLayer;              // 🆕 เพิ่ม Layer สำหรับกำแพง
    public float wallDetectionDistance = 1f; // 🆕 ระยะตรวจจับกำแพง
    public bool useCircling = true;
    public float circlingSpeed = 0.5f;
    public bool isCollidingWithPlayer;

    [Header("🔧 Debug Settings")]
    public bool showDebugInfo = false;
    public bool showPatrolGizmos = true;     // 🆕 แสดง patrol area ใน Scene view



    [Header("🎯 Dummy UI")]
    public GameObject dummyUIPrefab; // ลาก WorldSpaceUI prefab มาใส่
    private GameObject dummyUIInstance;

    [Header("🎯 Dummy Debuff System")]
    public float dummyDebuffCooldown = 8f;  // cooldown การใส่ debuff
    private float nextDummyDebuffTime = 0f;
    private float dummyDebuffCheckInterval = 2f;  // ตรวจสอบทุก 2 วินาที
    private float lastDummyDebuffCheckTime = 0f;

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
    [Header("🧹 Pickup Text Cleanup")]
    private float nextPickupCleanupTime = 0f;
    private const float PICKUP_CLEANUP_INTERVAL = 10f;

    [Header("🤖 AI Behavior Patterns")]
    [SerializeField] private bool useAdvancedAI = true;          // เปิด/ปิด AI ซับซ้อน
    [SerializeField] private float behaviorChangeInterval = 3f;  // เปลี่ยนพฤติกรรมทุก 3 วินาที
    [SerializeField] private float circleRadius = 3f;            // รัศมีวนรอบ
    [SerializeField] private float strafeSpeed = 0.7f;           // ความเร็วเดินข้าง
    [SerializeField] private float retreatChance = 20f;          // โอกาสถอย (%)
    [SerializeField] private float circleChance = 40f;           // โอกาสวนรอบ (%)
    [SerializeField] private float rushChance = 40f;
    public bool IsSpawned => Object != null && Object.IsValid;
    private enum AIBehavior
    {
        DirectRush,     // เดินตรงเข้าไป
        CircleLeft,     // วนซ้าย
        CircleRight,    // วนขวา
        StrafeLeft,     // เดินข้างซ้าย
        StrafeRight,    // เดินข้างขวา
        Retreat         // ถอยหลัง
    }

    [Networked] private AIBehavior CurrentBehavior { get; set; }
    [Networked] private float NextBehaviorChangeTime { get; set; }
    private float circleAngle = 0f; // มุมสำหรับวนรอบ
    // ========== Unity Lifecycle ==========
    private void Awake()
    {
        base.Awake();

        // เช็คว่าเป็น Dummy หรือไม่
        if (IsDummy)
        {
            persistentData = PersistentPlayerData.Instance;
        }
    }
    protected override void Start()
    {
        base.Start();
        if (IsDummy)
        {
            persistentData = PersistentPlayerData.Instance;

            // โหลด level จาก Firebase ถ้ายังไม่มี
            if (DummyLevel <= 0)
            {
                LoadDummyLevelFromFirebase();
            }

            InitializeDummySystem();

            Debug.Log($"[Dummy] Started at Level {DummyLevel}");
        }

        if (isLobbyDummy && HasStateAuthority)
        {
            IsDummy = true;
            InitializeDummySystem();
        }
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
        if (wallLayer == 0)
        {
            wallLayer = LayerMask.GetMask("Wall", "Obstacle");
        }

        // ตั้งค่า physics สำหรับ 2D movement
        if (rb != null)
        {
            rb.freezeRotation = true; // ห้ามหมุนทุกแกน - ใช้แค่ flip
            rb.useGravity = true;
            rb.linearDamping = 2.0f; // เพิ่ม drag เพื่อให้หยุดได้เร็วขึ้น
            rb.mass = 5f;   // ลด mass เพื่อให้เคลื่อนที่ได้ง่ายขึ้น
        }



        LevelManager enemyLevel = GetComponent<LevelManager>();
        if (enemyLevel != null && !IsDummy && !isLobbyDummy)
        {
            int randomLevel = Random.Range(1, 10);
            StartCoroutine(SetEnemyLevelCoroutine(enemyLevel, randomLevel));
        }
    }
    private IEnumerator SetEnemyLevelCoroutine(LevelManager enemyLevel, int targetLevel)
    {
        int attempts = 0;
        int maxAttempts = 50; // ป้องกัน infinite loop

        while (enemyLevel.CurrentLevel < targetLevel && attempts < maxAttempts)
        {
            enemyLevel.GainExp(enemyLevel.ExpToNextLevel);
            attempts++;

            // ✅ รอ 1 frame เพื่อไม่ให้ค้าง
            yield return null;
        }

        if (attempts >= maxAttempts)
        {
            Debug.LogWarning($"[NetworkEnemy] Failed to reach level {targetLevel} after {maxAttempts} attempts");
        }
        else
        {
            Debug.Log($"Enemy {CharacterName} spawned at level {enemyLevel.CurrentLevel}");
        }
    }
    protected void Update()
    {
        // อัพเดท Dummy UI
        if (IsDummy && dummyUIInstance != null)
        {
            UpdateDummyUIPositionAndRotation();
            //UpdateDummyLevelText();
        }

        // ✅ Cleanup pickup texts ทุก 10 วินาที
        if (IsDummy && Time.time >= nextPickupCleanupTime)
        {
            nextPickupCleanupTime = Time.time + PICKUP_CLEANUP_INTERVAL;
        }
    }
  
    // ใน NetworkEnemy.cs - override InitializeStats เพื่อไม่ให้เรียก equipment methods
    private void LoadDummyLevelFromFirebase()
    {
        if (persistentData?.multiCharacterData == null)
        {
            Debug.LogWarning("[Dummy] Firebase data not ready, using default level 1");
            DummyLevel = 1;
            return;
        }

        // เช็คและรีเซ็ตถ้าเป็นวันใหม่
        persistentData.multiCharacterData.CheckAndResetDummyIfNewDay();

        // โหลด level จาก Firebase
        DummyLevel = persistentData.multiCharacterData.trainingDummy.currentLevel;

        Debug.Log($"[Dummy] 📥 Loaded level {DummyLevel} from Firebase (Last reset: {persistentData.multiCharacterData.trainingDummy.lastResetDate})");
    }
    public void LevelUpDummy()
    {
        if (!IsDummy) return;

        if (persistentData?.multiCharacterData?.trainingDummy == null)
        {
            Debug.LogError("[Dummy] Cannot level up - Firebase data not available!");
            return;
        }

        var dummyData = persistentData.multiCharacterData.trainingDummy;

        if (!dummyData.CanLevelUp())
        {
            Debug.LogWarning($"[Dummy] Already at max level ({dummyData.maxLevel})");
            return;
        }

        // Level up
        int oldLevel = DummyLevel;
        dummyData.LevelUp();
        DummyLevel = dummyData.currentLevel;

        // อัปเดต stats
        InitializeDummySystem();

        // บันทึกลง Firebase
        persistentData.multiCharacterData.UpdateTrainingDummyDebugInfo();
        persistentData.SavePlayerDataAsync();

        // แสดง UI notification
        RPC_ShowLevelUpNotification(oldLevel, DummyLevel);

        Debug.Log($"[Dummy] ⬆️ Leveled up from {oldLevel} to {DummyLevel}");
        Debug.Log($"[Dummy] New stats - HP: {MaxHp}, Armor: {Armor}, Magic Armor: {MagicArmor}");
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowLevelUpNotification(int oldLevel, int newLevel)
    {
        // แสดง floating text
        Vector3 textPosition = transform.position + Vector3.up * 3f;
        string message = $"DUMMY LEVEL UP!\n{oldLevel} → {newLevel}";

        // ใช้ DamageTextManager หรือ Notification system
        Debug.Log($"💥 {message}");
    }


    private void InitializeDummySystem()
    {
        if (!IsDummy) return;

        // ✅ โหลด level จาก Firebase ถ้ายังไม่มี
        if (persistentData?.multiCharacterData?.trainingDummy != null)
        {
            persistentData.multiCharacterData.CheckAndResetDummyIfNewDay();
            DummyLevel = persistentData.multiCharacterData.trainingDummy.currentLevel;
            Debug.Log($"[Dummy] Loaded level {DummyLevel} from Firebase");
        }

        int levelMultiplier = DummyLevel;

        // 🎯 Base Stats (ขึ้นตาม level)
        MaxHp = 500 + (levelMultiplier * 1000);
        CurrentHp = MaxHp;

        // 🛡️ Armor Stats
        Armor = 10 + (levelMultiplier * 10);
        MagicArmor = 5 + (levelMultiplier * 8);

        // 🎯 Combat Stats
        EvasionRate = Mathf.Min(5f + (levelMultiplier * 2f), 40f);
        HitRate = 90f;

        // ❤️ Regeneration Stats
        HealthRegen = 0.2f + (levelMultiplier * 0.5f);

        // 🚫 Dummy ไม่เคลื่อนที่
        MoveSpeed = 0f;
        detectRange = 0f;

        // 🔧 ปิดการชนกับ Player
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // ✅ สร้าง UI ใหม่เสมอ (ลบของเก่าก่อน)
        if (dummyUIInstance != null)
        {
            Destroy(dummyUIInstance);
            dummyUIInstance = null;
        }

        CreateDummyUI();

        Debug.Log($"╔═══════════════════════════════════════╗");
        Debug.Log($"║  🎯 Level {DummyLevel} Training Dummy Stats  ║");
        Debug.Log($"╠═══════════════════════════════════════╣");
        Debug.Log($"║  HP: {MaxHp:N0}                       ");
        Debug.Log($"║  Physical Armor: {Armor}              ");
        Debug.Log($"║  Magic Armor: {MagicArmor}            ");
        Debug.Log($"║  Evasion Rate: {EvasionRate}%         ");
        Debug.Log($"║  HP Regen: {HealthRegen}/s            ");
        Debug.Log($"╚═══════════════════════════════════════╝");
    }
    private void CreateDummyUI()
    {
        if (!IsDummy)
        {
            Debug.LogWarning("[Dummy] Not a dummy, skipping UI creation");
            return;
        }

        // ลบ UI เก่าถ้ามี
        if (dummyUIInstance != null)
        {
            Destroy(dummyUIInstance);
            dummyUIInstance = null;
        }

        // ถ้าไม่มี prefab ให้สร้างแบบ manual
        if (dummyUIPrefab == null)
        {
            Debug.LogWarning("[Dummy] No UI Prefab! Creating simple UI...");
            return;
        }

        try
        {
            // สร้าง UI instance
            dummyUIInstance = Instantiate(dummyUIPrefab, transform.position + Vector3.up * 3.5f, Quaternion.identity);
            dummyUIInstance.transform.SetParent(null); // ไม่ parent กับ Dummy

            // หา WorldSpaceUI component
            WorldSpaceUI uiComponent = dummyUIInstance.GetComponent<WorldSpaceUI>();

            if (uiComponent != null)
            {
                uiComponent.offset = new Vector3(0, 3.5f, 0);
                uiComponent.showLevelInName = true;
                InitializeDummyWorldSpaceUI(uiComponent);
            }
            else
            {
                Debug.LogWarning("[Dummy] WorldSpaceUI component not found, creating simple UI...");
                Destroy(dummyUIInstance);
                
            }

            Debug.Log($"[Dummy] ✅ UI created for Level {DummyLevel} Dummy");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Dummy] Error creating UI: {e.Message}");
            
        }
    }

    // ✅ เพิ่ม method สำหรับ initialize UI ของ Dummy
    private void InitializeDummyWorldSpaceUI(WorldSpaceUI ui)
    {
        // เก็บ reference ไว้
        dummyUIInstance = ui.gameObject;

        // ตั้งค่า UI elements
        if (ui.playerNameText3D != null)
        {
            ui.playerNameText3D.text = $"[Lv.{DummyLevel}] Training Dummy";
            ui.playerNameText3D.color = Color.cyan; // สีพิเศษสำหรับ Dummy
            ui.playerNameText3D.fontSize = 8; // ขนาดใหญ่ขึ้น
        }

        if (ui.levelText3D != null)
        {
            ui.levelText3D.text = $"Level {DummyLevel}";
            ui.levelText3D.color = Color.yellow;
        }

        // ซ่อน HP/Mana bars (Dummy ไม่ต้องแสดง)
        if (ui.healthBar != null)
            ui.healthBar.gameObject.SetActive(false);

        if (ui.manaBar != null)
            ui.manaBar.gameObject.SetActive(false);

        Debug.Log($"[Dummy] UI created for Level {DummyLevel} Dummy");
    }
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
   
    private void UpdateDummyUIPositionAndRotation()
    {
        if (dummyUIInstance == null) return;

        try
        {
            // ตั้งตำแหน่ง UI ให้อยู่เหนือ Dummy
            Vector3 uiPosition = transform.position + Vector3.up * 3.5f;
            dummyUIInstance.transform.position = uiPosition;

            // หมุนหน้า UI ไปทางกล้อง
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                dummyUIInstance.transform.LookAt(
                    dummyUIInstance.transform.position + mainCamera.transform.rotation * Vector3.forward,
                    mainCamera.transform.rotation * Vector3.up
                );
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Dummy] Error updating UI: {e.Message}");
        }
    }

    // ✅ อัพเดทตำแหน่ง UI ให้ตาม Dummy
    private void UpdateDummyUIPosition()
    {
        if (dummyUIInstance == null) return;

        // ตั้งตำแหน่ง UI ให้อยู่เหนือ Dummy
        Vector3 uiPosition = transform.position + new Vector3(0, 3.5f, 0);
        dummyUIInstance.transform.position = uiPosition;

        // หมุนหน้า UI ไปทางกล้อง
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            dummyUIInstance.transform.LookAt(
                dummyUIInstance.transform.position + mainCamera.transform.rotation * Vector3.forward,
                mainCamera.transform.rotation * Vector3.up
            );
        }
    }
    // Called when spawned by Fusion
    public override void Spawned()
    {
        Debug.Log($"Enemy Spawned - HasStateAuthority: {HasStateAuthority}");

        if (HasStateAuthority)
        {
            NetworkedMaxHp = MaxHp;
            NetworkedCurrentHp = CurrentHp;
            IsDead = false;

            // ✅ เปลี่ยนจาก Patrolling เป็น Chasing ทันที
            CurrentState = EnemyState.Chasing;
            StateTimer = 0f;
            totalPatrolTime = 0f;

            PatrolCenter = transform.position;
            GenerateNewPatrolTarget();
            individualPatrolWaitTime = Random.Range(minPatrolWaitTime, maxPatrolWaitTime);

            NetworkedPosition = transform.position;
            NetworkedScale = transform.localScale;

            // เริ่ม AI behavior
            CurrentBehavior = AIBehavior.DirectRush;
            NextBehaviorChangeTime = Runner.SimulationTime + Random.Range(1f, 3f);

            // ✅ หาผู้เล่นทันที
            FindNearestPlayer();

            if (showDebugInfo)
            {
                Debug.Log($"{CharacterName}: Spawned and immediately chasing player!");
            }
        }
    }
    // ========== Network Update ==========
    public override void FixedUpdateNetwork()
    {
        if (Setting.IsPaused) return;

        base.FixedUpdateNetwork();

        if (!IsSpawned) return;

        if (HasStateAuthority)
        {
            if (!IsDead)
            {
                // 🎯 Dummy ปล่อย Debuff
                if (IsDummy)
                {
                    CheckAndApplyDummyDebuffs();
                }
                else
                {
                    // AI ปกติ
                    FindNearestPlayer();
                    ImprovedMoveTowardsTarget();
                    TryAttackTarget();
                }

                // Check death
                if (CurrentHp <= 0 && !IsDead)
                {
                    IsDead = true;
                    RPC_OnDeath();
                }
            }

            // อัพเดท Network Properties
            NetworkedPosition = transform.position;
            NetworkedScale = transform.localScale;
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
    private void CheckAndApplyDummyDebuffs()
    {
        // ตรวจสอบทุก 2 วินาที
        if (Time.time - lastDummyDebuffCheckTime < dummyDebuffCheckInterval)
            return;

        lastDummyDebuffCheckTime = Time.time;

        // เช็ค cooldown
        if (Time.time < nextDummyDebuffTime)
            return;

        // หาผู้เล่นในระยะ 10 เมตร
        Collider[] heroColliders = Physics.OverlapSphere(transform.position, 10f, LayerMask.GetMask("Player"));

        if (heroColliders.Length == 0)
            return;

        // เลือกผู้เล่นสุ่ม
        Collider targetCollider = heroColliders[Random.Range(0, heroColliders.Length)];
        Hero targetHero = targetCollider.GetComponent<Hero>();

        if (targetHero != null && targetHero.IsSpawned)
        {
            // ใส่ Debuff ตาม Dummy Level
            ApplyDummyDebuff(targetHero);
            nextDummyDebuffTime = Time.time + dummyDebuffCooldown;
        }
    }

    // ✅ เพิ่ม method ใหม่: ใส่ Debuff ตาม Level
    private void ApplyDummyDebuff(Hero targetHero)
    {
        // คำนวณ debuff stats ตาม level
        int level = DummyLevel;

        // โอกาสใส่ debuff (เพิ่มตาม level)
        float debuffChance = Mathf.Min(30f + (level * 3f), 70f); // 30-70%

        if (Random.Range(0f, 100f) > debuffChance)
        {
            Debug.Log($"[Dummy] Level {level} debuff missed! (Chance: {debuffChance}%)");
            return;
        }

        // สุ่ม debuff แบบถ่วงน้ำหนักตาม level
        StatusEffectType debuffType = SelectDummyDebuff(level);

        // คำนวณค่า debuff
        float duration = CalculateDebuffDuration(level);
        float amount = CalculateDebuffAmount(level, debuffType);

        // ใส่ debuff
        ApplyDebuffToHero(targetHero, debuffType, duration, amount);
    }

    // ✅ เลือก Debuff ตาม Level
    private StatusEffectType SelectDummyDebuff(int level)
    {
        // Level 1-5: Blind เท่านั้น
        if (level <= 5)
        {
            return StatusEffectType.Blind;
        }

        // Level 6-10: Blind (60%) หรือ Weakness (40%)
        if (level <= 10)
        {
            return Random.Range(0f, 100f) < 60f ? StatusEffectType.Blind : StatusEffectType.Weakness;
        }

        // Level 11-20: Blind (40%), Weakness (30%), Stun (30%)
        if (level <= 20)
        {
            float roll = Random.Range(0f, 100f);
            if (roll < 40f) return StatusEffectType.Blind;
            if (roll < 70f) return StatusEffectType.Weakness;
            return StatusEffectType.Stun;
        }

        // Level 21-30: เท่ากันหมด + Armor Break
        if (level <= 30)
        {
            float roll = Random.Range(0f, 100f);
            if (roll < 25f) return StatusEffectType.Blind;
            if (roll < 50f) return StatusEffectType.Weakness;
            if (roll < 75f) return StatusEffectType.Stun;
            return StatusEffectType.ArmorBreak;
        }

        // Level 31+: ทุก debuff (weighted random)
        float highRoll = Random.Range(0f, 100f);
        if (highRoll < 20f) return StatusEffectType.Blind;
        if (highRoll < 40f) return StatusEffectType.Weakness;
        if (highRoll < 60f) return StatusEffectType.Stun;
        if (highRoll < 80f) return StatusEffectType.ArmorBreak;
        return StatusEffectType.Freeze; // Level สูงมาก ใส่ Freeze ได้
    }

    // ✅ คำนวณระยะเวลา Debuff
    private float CalculateDebuffDuration(int level)
    {
        // Base duration: 2-6 วินาที ขึ้นตาม level
        float baseDuration = 2f + (level * 0.15f);

        // Cap ที่ 8 วินาที
        return Mathf.Min(baseDuration, 8f);
    }

    // ✅ คำนวณความแรงของ Debuff
    private float CalculateDebuffAmount(int level, StatusEffectType debuffType)
    {
        float baseAmount = 0f;

        switch (debuffType)
        {
            case StatusEffectType.Blind:
                // 40-80% hit/crit reduction
                baseAmount = 0.4f + (level * 0.015f);
                return Mathf.Min(baseAmount, 0.85f);

            case StatusEffectType.Weakness:
                // 20-60% attack reduction
                baseAmount = 0.2f + (level * 0.015f);
                return Mathf.Min(baseAmount, 0.65f);

            case StatusEffectType.ArmorBreak:
                // 30-70% armor reduction
                baseAmount = 0.3f + (level * 0.015f);
                return Mathf.Min(baseAmount, 0.75f);

            case StatusEffectType.Stun:
            case StatusEffectType.Freeze:
                // Duration เท่านั้น ไม่มี amount
                return 0f;

            default:
                return 0.5f;
        }
    }

    // ✅ ใส่ Debuff ให้ Hero
    private void ApplyDebuffToHero(Hero hero, StatusEffectType debuffType, float duration, float amount)
    {
        StatusEffectManager statusManager = hero.GetComponent<StatusEffectManager>();
        if (statusManager == null) return;

        switch (debuffType)
        {
            case StatusEffectType.Blind:
                statusManager.ApplyBlind(duration, amount);
                Debug.Log($"[Dummy Lv.{DummyLevel}] ⚫ Blinded {hero.CharacterName} for {duration:F1}s ({amount * 100}% reduction)");
                break;

            case StatusEffectType.Weakness:
                statusManager.ApplyWeakness(duration, amount);
                Debug.Log($"[Dummy Lv.{DummyLevel}] 💪 Weakened {hero.CharacterName} for {duration:F1}s ({amount * 100}% reduction)");
                break;

            case StatusEffectType.Stun:
                statusManager.ApplyStun(duration);
                Debug.Log($"[Dummy Lv.{DummyLevel}] ⚡ Stunned {hero.CharacterName} for {duration:F1}s");
                break;

            case StatusEffectType.ArmorBreak:
                statusManager.ApplyArmorBreak(duration, amount);
                Debug.Log($"[Dummy Lv.{DummyLevel}] 🛡️ Broke {hero.CharacterName}'s armor for {duration:F1}s ({amount * 100}% reduction)");
                break;

            case StatusEffectType.Freeze:
                statusManager.ApplyFreeze(duration);
                Debug.Log($"[Dummy Lv.{DummyLevel}] ❄️ Froze {hero.CharacterName} for {duration:F1}s");
                break;
        }

        // แสดง floating text
        RPC_ShowDebuffText(hero.transform.position, debuffType, duration);
    }

    // ✅ แสดง Debuff Text
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowDebuffText(Vector3 position, StatusEffectType debuffType, float duration)
    {
        string debuffText = GetDebuffDisplayText(debuffType, duration);
        Color debuffColor = GetDebuffColor(debuffType);

        // ใช้ DamageTextManager หรือสร้าง text แยก
        // DamageTextManager.ShowCustomText(position + Vector3.up * 3f, debuffText, debuffColor);

        Debug.Log($"💥 {debuffText}");
    }
    private string GetDebuffDisplayText(StatusEffectType debuffType, float duration)
    {
        switch (debuffType)
        {
            case StatusEffectType.Blind: return $"⚫ BLIND! ({duration:F1}s)";
            case StatusEffectType.Weakness: return $"💪 WEAK! ({duration:F1}s)";
            case StatusEffectType.Stun: return $"⚡ STUNNED! ({duration:F1}s)";
            case StatusEffectType.ArmorBreak: return $"🛡️ ARMOR BREAK! ({duration:F1}s)";
            case StatusEffectType.Freeze: return $"❄️ FROZEN! ({duration:F1}s)";
            default: return "DEBUFF!";
        }
    }

    // ✅ Helper: สี Debuff
    private Color GetDebuffColor(StatusEffectType debuffType)
    {
        switch (debuffType)
        {
            case StatusEffectType.Blind: return new Color(0.3f, 0.3f, 0.3f); // เทา
            case StatusEffectType.Weakness: return new Color(0.6f, 0.3f, 0.6f); // ม่วง
            case StatusEffectType.Stun: return Color.yellow;
            case StatusEffectType.ArmorBreak: return new Color(1f, 0.5f, 0f); // ส้ม
            case StatusEffectType.Freeze: return Color.cyan;
            default: return Color.white;
        }
    }
    protected virtual void OnDestroy()
    {
        if (IsDummy)
        {
            // ทำลาย UI
            if (dummyUIInstance != null)
            {
                try
                {
                    Destroy(dummyUIInstance);
                    dummyUIInstance = null;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Dummy] Error destroying UI: {e.Message}");
                }
            }

            // ✅ Cleanup pickup texts
            CleanupAllPickupTexts();
        }
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
        Vector3 directionToTarget = (target - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, target);

        RaycastHit hit;

        // ✅ เช็คแบบเดียวกับ Toxic Dash
        if (Physics.Raycast(
            transform.position + Vector3.up * 0.5f,  // เริ่มสูงขึ้นนิด
            directionToTarget,
            out hit,
            distanceToTarget,
            wallLayer))
        {
            if (showDebugInfo)
            {
                Debug.Log($"{CharacterName}: Patrol blocked by {hit.collider.name} at {hit.distance:F1}m");
                Debug.DrawLine(transform.position, hit.point, Color.red, 2f);
            }

            // ลองหาจุดใหม่ที่ปลอดภัย
            for (int attempt = 0; attempt < 5; attempt++)
            {
                Vector2 safeDirection = Random.insideUnitCircle.normalized;
                Vector3 newTarget = PatrolCenter + new Vector3(safeDirection.x, 0, safeDirection.y) * (patrolRange * 0.5f);

                Vector3 checkDir = (newTarget - transform.position).normalized;
                float checkDist = Vector3.Distance(transform.position, newTarget);

                // เช็คว่าทางนี้ปลอดภัยไหม
                if (!Physics.Raycast(
                    transform.position + Vector3.up * 0.5f,
                    checkDir,
                    checkDist,
                    wallLayer))
                {
                    if (showDebugInfo)
                        Debug.Log($"{CharacterName}: Found safe patrol target at attempt {attempt + 1}");

                    return newTarget;
                }
            }

            // หาไม่เจอ กลับไป patrol center
            return PatrolCenter;
        }

        return target;
    }

    // ========== 🎯 Improved Movement System ==========
    protected virtual void ImprovedMoveTowardsTarget()
    {
        if (IsDummy) return;

        StateTimer += Runner.DeltaTime;

        // ✅ ลบการเช็ค detectRange ออก - ไม่ต้องกลับไป Patrolling
        // if (CurrentState == EnemyState.Patrolling && targetTransform != null)
        // {
        //     float distanceToPlayer = Vector3.Distance(transform.position, targetTransform.position);
        //     if (distanceToPlayer <= detectRange)
        //     {
        //         CurrentState = EnemyState.Chasing;
        //         ...
        //     }
        // }

        // ✅ ลบการเช็คว่าควรกลับไป Patrolling - ให้ไล่ผู้เล่นตลอด
        // if (CurrentState != EnemyState.Patrolling && targetTransform == null)
        // {
        //     CurrentState = EnemyState.Patrolling;
        //     ...
        // }

        // ✅ ถ้าไม่มี target ให้หาใหม่
        if (targetTransform == null)
        {
            FindNearestPlayer();
            if (targetTransform == null)
            {
                // ถ้ายังหาไม่เจอ ให้หยุดชั่วคราว
                return;
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

        // หลีกเลี่ยงศัตรูตัวอื่น
        if (CurrentState != EnemyState.Patrolling)
        {
            Vector3 avoidanceForce = CalculateAvoidanceForce();
            moveDirection += avoidanceForce;
        }

        moveDirection.y = 0;
        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }

        ApplyMovement(moveDirection);

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
    // Handler สำหรับ Chasing State - เวอร์ชันใหม่
    private Vector3 HandleChasing()
    {
        if (targetTransform == null) return Vector3.zero;

        Vector3 directionToPlayer = (targetTransform.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, targetTransform.position);

        // เช็คว่าควรเปลี่ยน state หรือไม่
        if (distanceToPlayer <= AttackRange && distanceToPlayer > minDistanceToPlayer * 0.7f)
        {
            CurrentState = EnemyState.Attacking;
            StateTimer = 0f;
            if (showDebugInfo)
                Debug.Log($"{CharacterName}: Entering attack range! Distance: {distanceToPlayer:F2}");
            return Vector3.zero;
        }
        else if (distanceToPlayer <= minDistanceToPlayer * 0.7f)
        {
            CurrentState = EnemyState.BackingOff;
            StateTimer = 0f;
            if (showDebugInfo)
                Debug.Log($"{CharacterName}: Too close! Backing off... Distance: {distanceToPlayer:F2}");
            return Vector3.zero;
        }

        // ✅ ใช้ Advanced AI ถ้าเปิดใช้งาน
        if (useAdvancedAI)
        {
            return HandleAdvancedChasing(directionToPlayer, distanceToPlayer);
        }
        else
        {
            // AI แบบเดิม - เดินตรงเข้าไป
            return directionToPlayer;
        }
    }

    // ✅ Advanced AI Chasing
    private Vector3 HandleAdvancedChasing(Vector3 directionToPlayer, float distanceToPlayer)
    {
        // เปลี่ยนพฤติกรรมทุกๆ interval
        if (Runner.SimulationTime >= NextBehaviorChangeTime)
        {
            SelectNewBehavior(distanceToPlayer);
            NextBehaviorChangeTime = Runner.SimulationTime + behaviorChangeInterval;
        }

        Vector3 moveDirection = Vector3.zero;

        switch (CurrentBehavior)
        {
            case AIBehavior.DirectRush:
                moveDirection = directionToPlayer;
                break;

            case AIBehavior.CircleLeft:
                moveDirection = CalculateCircleMovement(directionToPlayer, -1f, distanceToPlayer);
                break;

            case AIBehavior.CircleRight:
                moveDirection = CalculateCircleMovement(directionToPlayer, 1f, distanceToPlayer);
                break;

            case AIBehavior.StrafeLeft:
                Vector3 leftDir = new Vector3(-directionToPlayer.z, 0, directionToPlayer.x);
                moveDirection = (directionToPlayer + leftDir * strafeSpeed).normalized;
                break;

            case AIBehavior.StrafeRight:
                Vector3 rightDir = new Vector3(directionToPlayer.z, 0, -directionToPlayer.x);
                moveDirection = (directionToPlayer + rightDir * strafeSpeed).normalized;
                break;

            case AIBehavior.Retreat:
                moveDirection = -directionToPlayer * 0.5f;
                break;
        }

        // ✅ เช็คกำแพงก่อนคืนค่า moveDirection
        if (moveDirection.magnitude > 0.1f)
        {
            RaycastHit wallCheck;
            if (Physics.Raycast(
                transform.position + Vector3.up * 0.5f,
                moveDirection.normalized,
                out wallCheck,
                wallDetectionDistance,
                wallLayer))
            {
                // เจอกำแพง - เปลี่ยน behavior
                if (showDebugInfo)
                {
                    Debug.Log($"{CharacterName}: Wall ahead! Changing behavior from {CurrentBehavior}");
                    Debug.DrawLine(transform.position, wallCheck.point, Color.yellow, 0.5f);
                }

                // บังคับเปลี่ยน behavior ทันที
                SelectNewBehavior(distanceToPlayer);
                NextBehaviorChangeTime = Runner.SimulationTime + 0.5f; // เปลี่ยนเร็วขึ้น

                // ใช้ทิศทางไปหาผู้เล่นแทน (fallback)
                moveDirection = directionToPlayer;
            }
        }

        if (showDebugInfo && moveDirection.magnitude > 0.1f)
        {
            Color debugColor = CurrentBehavior switch
            {
                AIBehavior.DirectRush => Color.red,
                AIBehavior.CircleLeft => Color.blue,
                AIBehavior.CircleRight => Color.green,
                AIBehavior.StrafeLeft => Color.cyan,
                AIBehavior.StrafeRight => Color.yellow,
                AIBehavior.Retreat => Color.magenta,
                _ => Color.white
            };
            Debug.DrawRay(transform.position, moveDirection * 2f, debugColor, 0.1f);
        }

        return moveDirection;
    }

    // ✅ เลือกพฤติกรรมใหม่แบบถ่วงน้ำหนัก
    private void SelectNewBehavior(float distanceToPlayer)
    {
        // ถ้าใกล้มาก → ถอยบ่อยขึ้น
        // ถ้าไกลมาก → เดินตรงบ่อยขึ้น
        float adjustedRetreatChance = retreatChance;
        float adjustedRushChance = rushChance;

        if (distanceToPlayer < AttackRange * 0.8f)
        {
            // ใกล้มาก → เพิ่มโอกาสถอย/วนรอบ
            adjustedRetreatChance *= 2f;
            adjustedRushChance *= 0.5f;
        }
        else if (distanceToPlayer > AttackRange * 1.5f)
        {
            // ไกลมาก → เพิ่มโอกาสเดินตรง
            adjustedRushChance *= 1.5f;
            adjustedRetreatChance *= 0.5f;
        }

        // สุ่มพฤติกรรม
        float roll = Random.Range(0f, 100f);

        if (roll < adjustedRetreatChance)
        {
            CurrentBehavior = AIBehavior.Retreat;
        }
        else if (roll < adjustedRetreatChance + circleChance)
        {
            CurrentBehavior = Random.Range(0f, 1f) > 0.5f ? AIBehavior.CircleLeft : AIBehavior.CircleRight;
        }
        else if (roll < adjustedRetreatChance + circleChance + 20f) // 20% strafe
        {
            CurrentBehavior = Random.Range(0f, 1f) > 0.5f ? AIBehavior.StrafeLeft : AIBehavior.StrafeRight;
        }
        else
        {
            CurrentBehavior = AIBehavior.DirectRush;
        }

        // รีเซ็ตมุมวนรอบ
        circleAngle = 0f;

        if (showDebugInfo)
        {
            Debug.Log($"{CharacterName}: New behavior: {CurrentBehavior} (distance: {distanceToPlayer:F1})");
        }
    }

    // ✅ คำนวณการเคลื่อนที่แบบวนรอบ
    private Vector3 CalculateCircleMovement(Vector3 directionToPlayer, float direction, float distanceToPlayer)
    {
        // direction: -1 = ซ้าย, 1 = ขวา
        circleAngle += Runner.DeltaTime * 2f * direction; // หมุนรอบ

        // คำนวณตำแหน่งเป้าหมายบนวงกลม
        Vector3 tangent = new Vector3(-directionToPlayer.z, 0, directionToPlayer.x) * direction;
        Vector3 circleOffset = tangent * Mathf.Sin(circleAngle) + directionToPlayer * Mathf.Cos(circleAngle);

        // ถ้าไกลเกินไป ให้เข้าใกล้มากขึ้น
        if (distanceToPlayer > circleRadius * 1.5f)
        {
            circleOffset = Vector3.Lerp(circleOffset, directionToPlayer, 0.5f);
        }

        return circleOffset.normalized;
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
                    currentMoveSpeed *= patrolSpeed;
                    break;
                case EnemyState.BackingOff:
                    currentMoveSpeed *= 1.3f;
                    break;
            }

            // ✅ คำนวณระยะที่จะเคลื่อนที่
            float moveDistance = currentMoveSpeed * Runner.DeltaTime;

            // ✅ เช็คกำแพงแบบเดียวกับ Toxic Dash
            RaycastHit wallHit;
            if (Physics.Raycast(
                transform.position + Vector3.up * 0.5f,  // เริ่มสูงขึ้นนิดหน่อย
                moveDirection,
                out wallHit,
                moveDistance + wallDetectionDistance,
                wallLayer))
            {
                // ✅ เจอกำแพง - ลดระยะการเคลื่อนที่
                float safeDistance = Mathf.Max(0.1f, wallHit.distance - 0.5f);

                if (safeDistance < moveDistance)
                {
                    moveDistance = safeDistance;

                    if (showDebugInfo)
                    {
                        Debug.Log($"{CharacterName}: Wall at {wallHit.distance:F2}m, reducing move to {safeDistance:F2}m");
                        Debug.DrawLine(transform.position, wallHit.point, Color.red, 0.5f);
                    }
                }

                // ✅ ถ้าใกล้กำแพงมาก ให้หาทางเลี่ยว
                if (safeDistance < 0.3f)
                {
                    Vector3 alternativeDirection = FindAlternativeDirection(moveDirection);

                    if (alternativeDirection != Vector3.zero)
                    {
                        moveDirection = alternativeDirection;
                        moveDistance = currentMoveSpeed * Runner.DeltaTime;

                        if (showDebugInfo)
                        {
                            Debug.Log($"{CharacterName}: Taking alternative path");
                            Debug.DrawRay(transform.position, alternativeDirection * 2f, Color.green, 0.5f);
                        }
                    }
                    else
                    {
                        // หาทางไม่เจอ หยุดเลย
                        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);

                        if (showDebugInfo)
                            Debug.Log($"{CharacterName}: Completely blocked! Stopping.");

                        return;
                    }
                }
            }

            // ✅ เคลื่อนที่ด้วยระยะที่ปลอดภัย
            Vector3 newPosition = transform.position + moveDirection * moveDistance;

            // ✅ ตรวจสอบอีกครั้งขณะเคลื่อนที่ (safety check เหมือน Toxic Dash)
            Vector3 moveVector = newPosition - transform.position;
            if (moveVector.magnitude > 0.1f)
            {
                if (Physics.Raycast(
                    transform.position + Vector3.up * 0.5f,
                    moveVector.normalized,
                    moveVector.magnitude,
                    wallLayer))
                {
                    if (showDebugInfo)
                        Debug.LogWarning($"🛑 {CharacterName}: Wall collision during movement! Stopping.");

                    rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                    return;
                }
            }

            // ✅ ใช้ MovePosition แทน direct position (เหมือน Toxic Dash)
            rb.MovePosition(newPosition);
            FlipCharacterTowardsMovement(moveDirection);
        }
        else
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    // ✅ แก้ไข FindAlternativeDirection ให้ละเอียดขึ้น
    private Vector3 FindAlternativeDirection(Vector3 blockedDirection)
    {
        // ลองหาทางเลี่ยวหลายมุม (เหมือน Toxic Dash)
        float[] angles = { 30f, -30f, 45f, -45f, 60f, -60f, 90f, -90f };

        foreach (float angle in angles)
        {
            Vector3 altDir = Quaternion.Euler(0, angle, 0) * blockedDirection;

            // เช็คว่าทางนี้ปลอดภัยไหม
            RaycastHit hit;
            if (!Physics.Raycast(
                transform.position + Vector3.up * 0.5f,
                altDir.normalized,
                out hit,
                wallDetectionDistance * 2f,
                wallLayer))
            {
                if (showDebugInfo)
                {
                    Debug.DrawRay(transform.position, altDir.normalized * 2f, Color.green, 0.5f);
                    Debug.Log($"{CharacterName}: Found alternative at {angle}°");
                }
                return altDir.normalized;
            }
        }

        // หาทางไม่เจอ
        if (showDebugInfo)
            Debug.Log($"{CharacterName}: No alternative path found!");

        return Vector3.zero;
    }

    // ✅ ตรวจสอบว่าทางข้างหน้ามีกำแพงไหม
    private bool IsPathBlocked(Vector3 direction, float distance)
    {
        // ใช้ SphereCast เพื่อตรวจจับกำแพง (ดีกว่า Raycast เพราะครอบคลุมพื้นที่กว้างขึ้น)
        float capsuleRadius = 0.5f; // รัศมีของตัวละคร

        RaycastHit hit;
        bool blocked = Physics.SphereCast(
            transform.position + Vector3.up * 0.5f, // จุดเริ่มต้น (สูงขึ้นนิดหน่อย)
            capsuleRadius,                           // รัศมี
            direction,                               // ทิศทาง
            out hit,
            distance + wallDetectionDistance,        // ระยะ
            wallLayer                                // เช็คเฉพาะ Wall layer
        );

        if (blocked && showDebugInfo)
        {
            Debug.DrawRay(transform.position, direction * distance, Color.red, 0.1f);
            Debug.Log($"{CharacterName}: Wall detected at {hit.point}, distance: {hit.distance:F2}");
        }

        return blocked;
    }

    // ✅ หาทางเลี่ยงเมื่อเจอกำแพง
    
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
                rb.linearVelocity = NetworkedVelocity;
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
        // ✅ ลบการเช็คเวลา - หาได้ทุกเมื่อ
        // if (Time.time < nextTargetCheckTime) return;
        // nextTargetCheckTime = Time.time + attackCheckInterval;

        float nearestDistance = float.MaxValue;
        Hero nearestHero = null;

        Hero[] heroes = FindObjectsOfType<Hero>();

        foreach (Hero hero in heroes)
        {
            if (hero == null || !hero.IsSpawned) continue;

            float distance = Vector3.Distance(transform.position, hero.transform.position);

            // ✅ ลบการเช็ค detectRange - หาได้ทุกระยะ
            if (distance < nearestDistance)
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
        if (IsDummy) return;

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

        if (HasStateAuthority)
        {
            // 🆕 ถ้าเป็น Dummy ให้จัดการแบบพิเศษ
            if (IsDummy)
            {
                HandleDummyDeath();
                return; // ออกก่อน ไม่ทำ drop อื่นๆ
            }

            // Enemy ปกติ - โค้ดเดิม...
            DailyQuestTracker.AddEnemyKill();

            // Currency drops
            EnemyDropManager currencyDropManager = GetComponent<EnemyDropManager>();
            if (currencyDropManager != null)
            {
                currencyDropManager.TriggerDrops();
            }

            // Item drops
            ItemDropManager itemDropManager = GetComponent<ItemDropManager>();
            if (itemDropManager != null)
            {
                itemDropManager.TriggerItemDrops();
            }

            // Experience drops
            DropExpToNearbyHeroes();

            // Notify spawner
            NotifySpawnerOfDeath();
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
    // แก้ไขใน NetworkEnemy.cs

    private void HandleDummyDeath()
    {
        Debug.Log($"[Dummy] Level {DummyLevel} Dummy defeated!");

        // ✅ 1. ให้ Gold
        int goldReward = CalculateDummyGoldReward();
        GiveDummyGoldToNearbyHeroes(goldReward);

        // ✅ 2. Level Up และบันทึกลง Firebase
        if (persistentData?.multiCharacterData?.trainingDummy != null)
        {
            var dummyData = persistentData.multiCharacterData.trainingDummy;

            if (dummyData.CanLevelUp())
            {
                int oldLevel = DummyLevel;
                dummyData.LevelUp();
                DummyLevel = dummyData.currentLevel;

                Debug.Log($"[Dummy] ⬆️ Leveled up from {oldLevel} to {DummyLevel}");

                // ✅ บันทึกและ reload ข้อมูล
                persistentData.SaveAndReloadDummyData();
            }
        }

        // ✅ 3. ลบ UI
        if (dummyUIInstance != null)
        {
            Destroy(dummyUIInstance);
            dummyUIInstance = null;
        }

        // ✅ 4. Despawn (EnemySpawner จะ spawn ใหม่เอง)
        if (HasStateAuthority && Object != null)
        {
            Debug.Log("[Dummy] 💀 Despawning...");
            Runner.Despawn(Object);
        }
    }

    // ✅ เพิ่ม coroutine สำหรับ force save
    private IEnumerator ForceSaveDummyLevel()
    {
        Debug.Log("[Dummy] 💾 Force saving level to Firebase...");

        // บันทึกลง Firebase
        persistentData.SavePlayerDataAsync();

        // รอ 1 วินาทีให้ Firebase save เสร็จ
        yield return new WaitForSeconds(1f);

        Debug.Log($"[Dummy] ✅ Level {DummyLevel} saved to Firebase");

        // ✅ Verify save โดยอ่านกลับมา
        if (persistentData?.multiCharacterData?.trainingDummy != null)
        {
            int savedLevel = persistentData.multiCharacterData.trainingDummy.currentLevel;
            Debug.Log($"[Dummy] 🔍 Verified Firebase level: {savedLevel}");
        }
    }

    // ✅ เพิ่ม method สำหรับ respawn ทันที
    private IEnumerator ImmediateRespawnDummy()
    {
        Debug.Log("[Dummy] 🔄 Starting immediate respawn...");

        // รอ 2 วินาทีให้ save เสร็จ
        yield return new WaitForSeconds(2f);

        if (!HasStateAuthority || Object == null)
        {
            Debug.LogWarning("[Dummy] Cannot respawn - no authority");
            yield break;
        }

        // เก็บตำแหน่งเดิม
        Vector3 spawnPosition = transform.position;

        Debug.Log($"[Dummy] 💀 Despawning old dummy...");

        // Despawn Dummy เก่า
        Runner.Despawn(Object);

        // รอให้ despawn เสร็จ
        yield return new WaitForSeconds(0.5f);

        // ✅ หา EnemySpawner และ spawn ใหม่ทันที
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null)
        {
            Debug.Log($"[Dummy] 🎯 Spawning new Level {DummyLevel} dummy immediately...");
            spawner.SpawnDummyAtPosition(spawnPosition);
        }
        else
        {
            Debug.LogError("[Dummy] ❌ EnemySpawner not found!");
        }
    }
    private IEnumerator RespawnDummyAfterDeath()
    {
        Debug.Log("[Dummy] ⏳ Waiting 3 seconds before respawn...");
        yield return new WaitForSeconds(3f);

        if (!HasStateAuthority || Object == null)
        {
            Debug.LogWarning("[Dummy] Cannot respawn - no authority or object is null");
            yield break;
        }

        // เก็บตำแหน่งเดิม
        Vector3 spawnPosition = transform.position;

        Debug.Log($"[Dummy] 🔄 Despawning old dummy at {spawnPosition}...");

        // Despawn Dummy เก่า
        Runner.Despawn(Object);

        // รอให้ despawn เสร็จ
        yield return new WaitForSeconds(0.5f);

        // หา EnemySpawner และสั่ง spawn Dummy ใหม่
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null)
        {
            Debug.Log($"[Dummy] 🎯 Spawning new Level {DummyLevel} dummy at {spawnPosition}...");
            spawner.SpawnDummyAtPosition(spawnPosition);
        }
        else
        {
            Debug.LogError("[Dummy] ❌ EnemySpawner not found!");
        }
    }


    // 🆕 คำนวณ Gold ที่ Dummy ให้
    private int CalculateDummyGoldReward()
    {
        int baseReward = 50;
        int levelBonus = DummyLevel * 50;
        return baseReward + levelBonus;
    }

    // 🆕 ให้ Gold แก่ Hero ที่อยู่ใกล้
    // ✅ แก้ไข: ให้ Gold แก่ Hero ที่อยู่ใกล้พร้อมแสดง pickup text
    private void GiveDummyGoldToNearbyHeroes(int goldAmount)
    {
        Collider[] heroColliders = Physics.OverlapSphere(transform.position, 15f, LayerMask.GetMask("Player"));

        foreach (Collider col in heroColliders)
        {
            Hero hero = col.GetComponent<Hero>();
            if (hero != null && hero.IsSpawned)
            {
                CurrencyManager currencyManager = hero.GetComponent<CurrencyManager>();
                if (currencyManager != null)
                {
                    currencyManager.AddGold(goldAmount, true);
                    Debug.Log($"[Dummy] Gave {goldAmount} gold to {hero.CharacterName}");
                }
            }
        }

        // ✅ เรียก RPC แบบ broadcast ให้ทุกคนแสดง UI
        RPC_ShowDummyGoldPickupBroadcast(transform.position, goldAmount);
    }
    // ✅ RPC แบบ broadcast - ทุกคนจะเห็น
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowDummyGoldPickupBroadcast(Vector3 position, int amount)
    {
        Debug.Log($"[RPC_ShowDummyGoldPickupBroadcast] Showing pickup at {position} for {amount} gold");

        // ✅ ใช้ DamageTextManager แทน
        DamageTextManager.ShowGoldPickup(position, amount);
    }
  

    // ✅ Animation แบบง่าย
   
  

    // ✅ เพิ่ม RPC method ใหม่ (เหมือน EnemyDropManager แต่ไม่ต้องใช้ dropManager)
   

    // ✅ เพิ่ม method แสดง pickup text (คัดลอกมาจาก EnemyDropManager)
  
    public static void CleanupAllPickupTexts()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();

        foreach (Canvas canvas in canvases)
        {
            if (canvas.name == "PickupCanvas")
            {
                // หา pickup texts ทั้งหมด
                Transform[] children = canvas.GetComponentsInChildren<Transform>();

                foreach (Transform child in children)
                {
                    if (child.name.Contains("PickupText"))
                    {
                        Destroy(child.gameObject);
                    }
                }

                Debug.Log($"[CleanupAllPickupTexts] Cleaned up pickup texts from {canvas.name}");
            }
        }
    }

    // ✅ Animation coroutine (คัดลอกมาจาก EnemyDropManager)
   

    // ✅ Helper methods (คัดลอกมาจาก EnemyDropManager)
   
   

   
    // 🆕 Respawn Dummy ใหม่
    private IEnumerator RespawnDummy()
    {
        yield return new WaitForSeconds(2f);

        if (HasStateAuthority && Object != null)
        {
            // เก็บตำแหน่งเดิม
            Vector3 spawnPosition = transform.position;

            // Despawn Dummy เก่า
            Runner.Despawn(Object);

            // หา EnemySpawner และสั่ง spawn Dummy ใหม่
            yield return new WaitForSeconds(0.5f);

            EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
            if (spawner != null)
            {
                spawner.SpawnDummyAtPosition(spawnPosition);
            }
        }
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

    // เพิ่มใน NetworkEnemy.cs

   
}