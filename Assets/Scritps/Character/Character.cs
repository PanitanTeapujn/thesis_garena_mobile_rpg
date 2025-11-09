using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System;

public enum AttackType
{
    Physical,    // โจมตีด้วย AttackDamage อย่างเดียว
    Magic,       // โจมตีด้วย MagicDamage อย่างเดียว
    Mixed        // โจมตีทั้งสองแบบ (ระบบเดิม)
}

public class Character : NetworkBehaviour
{
    #region Event system สำหรับแจ้งเตือนการเปลี่ยนแปลง stats
    public static event Action OnStatsChanged;
    #endregion

    #region สถานะพื้นฐานทั้งหมด (HP, Mana, Attack, Critical, etc.)
    [Header("Base Stats")]
    public CharacterStats characterStats;

    [SerializeField] private string characterName;
    public string CharacterName { get { return characterName; } set { characterName = value; } }

    [SerializeField] private int currentHp;
    public int CurrentHp { get { return currentHp; } set { currentHp = value; } }

    [SerializeField] private int maxHp;
    public int MaxHp { get { return maxHp; } set { maxHp = value; } }

    [SerializeField] private int currentMana;
    public int CurrentMana { get { return currentMana; } set { currentMana = value; } }

    [SerializeField] private int maxMana;
    public int MaxMana { get { return maxMana; } set { maxMana = value; } }

    [SerializeField] private int attackDamage;
    public int AttackDamage { get { return attackDamage; } set { attackDamage = value; } }

    [SerializeField] private int magicDamage;
    public int MagicDamage { get { return magicDamage; } set { magicDamage = value; } }

    [SerializeField] private int armor;
    public int Armor { get { return armor; } set { armor = value; } }

    [SerializeField] private float moveSpeed;
    public float MoveSpeed { get { return moveSpeed; } set { moveSpeed = value; } }

    [SerializeField] private float attackRange;
    public float AttackRange { get { return attackRange; } set { attackRange = value; } }

    [SerializeField] private float attackCooldown;
    public float AttackCooldown { get { return attackCooldown; } set { attackCooldown = value; } }

    [Header("Critical Stats")]
    [SerializeField] private float criticalChance;
    public float CriticalChance { get { return criticalChance; } set { criticalChance = value; } }

    [SerializeField] private float criticalDamageBonus;
    public float CriticalDamageBonus { get { return criticalDamageBonus; } set { criticalDamageBonus = value; } }

    [SerializeField] private float hitRate;
    public float HitRate { get { return hitRate; } set { hitRate = value; } }

    [SerializeField] private float evasionRate;
    public float EvasionRate { get { return evasionRate; } set { evasionRate = value; } }

    [SerializeField] private float attackSpeed;
    public float AttackSpeed { get { return attackSpeed; } set { attackSpeed = value; } }

    [SerializeField] private float reductionCoolDown;
    public float ReductionCoolDown { get { return reductionCoolDown; } set { reductionCoolDown = value; } }
    [SerializeField] private float lifeSteal;
    public float LifeSteal { get { return lifeSteal; } set { lifeSteal = value; } }
    
    [SerializeField] private float ampDamage;
    public float AmpDamage { get { return ampDamage; } set { ampDamage = value; } }
    #region สถานะพื้นฐานทั้งหมด - เพิ่ม 3 stats ใหม่
    // เพิ่มในส่วน stats declarations หลัง lifeSteal
    [SerializeField] private int magicArmor;
    public int MagicArmor { get { return magicArmor; } set { magicArmor = value; } }

    [SerializeField] private float manaRegen;
    public float ManaRegen { get { return manaRegen; } set { manaRegen = value; } }

    [SerializeField] private float healthRegen;
    public float HealthRegen { get { return healthRegen; } set { healthRegen = value; } }
    #endregion

    #region Network Properties - เพิ่ม Networked properties
    [Networked] public int NetworkedMagicArmor { get; set; }
    [Networked] public float NetworkedManaRegen { get; set; }
    [Networked] public float NetworkedHealthRegen { get; set; }
    #endregion

    [Header("Attack Settings")]
    [SerializeField] private AttackType attackType = AttackType.Physical; // Default เป็น Physical
    public AttackType AttackType { get { return attackType; } set { attackType = value; } }
    #endregion

    #region Network Properties  สำหรับ Fusion networking
    [Networked] public int NetworkedCurrentHp { get; set; }
    [Networked] public int NetworkedMaxHp { get; set; }
    [Networked] public int NetworkedCurrentMana { get; set; }
    [Networked] public int NetworkedMaxMana { get; set; }
    [Networked] public bool IsNetworkStateReady { get; set; }
    [Networked] public bool IsStatsReady { get; set; } = false;
    [Networked] public float NetworkedLifeSteal { get; set; }

    [Header("🆕 Spawn State Management")]
    [SerializeField] private bool isStatsLoaded = false;
    [SerializeField] private bool isSpawnComplete = false;
    private bool hasTriedLoadStats = false;

    #endregion


    // ใน Character.cs - แก้ไขส่วน Regeneration Settings
    #region Regeneration Settings การตั้งค่าการฟื้นฟู HP/Mana
    [Header("Regeneration Settings")]
    [SerializeField] private float baseHealthRegenPerSecond = 0.2f;
    [SerializeField] private float baseManaRegenPerSecond = 0.5f;
    private float healthRegenTimer = 0f;
    private float manaRegenTimer = 0f;
    private float regenTickInterval = 1f; // ✅ เปลี่ยนจาก 5 วินาที เป็น 1 วินาที (เร็วขึ้น 5 เท่า)

    [Header("Regen Caps")]
    [SerializeField] private float maxHealthRegenPerTick = 50f; // ✅ เพิ่ม cap ที่ปรับได้
    [SerializeField] private float maxManaRegenPerTick = 100f;  // ✅ เพิ่ม cap ที่ปรับได้
    #endregion

    #region Component References การอ้างอิงถึง managers และ components อื่นๆ
    [Header("Physics")]
    public Rigidbody rb;

    protected StatusEffectManager statusEffectManager;
    protected CombatManager combatManager;
    protected EquipmentManager equipmentManager;
    protected CharacterVisualManager visualManager;
    protected LevelManager levelManager;
    protected Inventory inventory;
    protected SetBonusManager setBonusManager;
    protected EquipmentSlotManager equipmentSlotManager; // 🆕 เพิ่มใหม่
    [Header("🆕 Equipment Slots")]
    [SerializeField] private List<ItemData> characterEquippedItems = new List<ItemData>(6); // 6 slots: Head, Armor, Weapon, Pants, Shoes, Rune
    [SerializeField] public List<ItemData> potionSlots = new List<ItemData>(5);   // 5 potion quick slots
    [Header("🧪 Potion Stack Counts")]
    [SerializeField] private List<int> potionStackCounts = new List<int>(5); // เก็บจำนวนของแต่ละ potion slot
    [Header("🧪 Potion Usage")]
    [SerializeField] public float potionCooldown = 1f; // cooldown 1 วินาที
    private float[] lastPotionUseTime = new float[5]; // cooldown แยกแต่ละ slot

    [Header("🔧 Auto-Fix Settings")]
    [SerializeField] private bool enableAutoFixInventory = true; // เปิด/ปิด auto-fix
    [SerializeField] private bool autoFixCompleted = false; // ป้องกันการ fix ซ้ำ
    [SerializeField] private float autoFixDelay = 3f; // รอ 3 วินาทีหลัง spawn
    public float PotionCooldown { get { return potionCooldown; } }

    // Events สำหรับแจ้ง UI
    public static event System.Action<Character, ItemType, ItemData> OnItemEquippedToSlot;
    public static event System.Action<Character, ItemType> OnItemUnequippedFromSlot;
    #endregion

    #region Unity Lifecycle & Initialization Awake, Start และการเริ่มต้นระบบต่างๆ
    protected virtual void Awake()
    {
        InitializeComponents();
        InitializePhysics();
    }
    public static void RaiseOnStatsChanged()
    {
        OnStatsChanged?.Invoke();
    }
    protected virtual void Start()
    {
        InitializeStats();
       
    }
   
    public bool IsStatsLoadingComplete()
    {
        return isStatsLoaded && isSpawnComplete;
    }

    public void WaitForStatsLoaded(System.Action callback)
    {
        if (IsStatsLoadingComplete())
        {
            callback?.Invoke();
        }
        else
        {
            StartCoroutine(WaitForStatsCoroutine(callback));
        }
    }

    private System.Collections.IEnumerator WaitForStatsCoroutine(System.Action callback)
    {
        yield return new WaitUntil(() => IsStatsLoadingComplete());
        callback?.Invoke();
    }


    public void LoadPlayerDataIfAvailable()
    {
        if (PersistentPlayerData.Instance == null)
        {
            Debug.LogWarning($"[Character] {CharacterName}: PersistentPlayerData not available");
            return;
        }

        Debug.Log($"[Character] {CharacterName}: Loading player data...");

        // โหลด stats ผ่าน LevelManager (แบบเดิม)
       /* var levelManager = GetComponent<LevelManager>();
        if (levelManager != null)
        {
            PersistentPlayerData.Instance.LoadStatsForCharacter(this, levelManager);
        }*/

        // โหลด inventory และ equipment (แบบเดิม)
        if (PersistentPlayerData.Instance.ShouldLoadFromFirebase())
        {
            PersistentPlayerData.Instance.LoadInventoryData(this);
        }

        Debug.Log($"[Character] {CharacterName}: Player data loaded");
    }
    private System.Collections.IEnumerator DelayedLoadPlayerData()
    {

        // รอ 3 frames เพื่อให้ UI systems พร้อม
        yield return null;
        yield return null;
        yield return null;

        // โหลดข้อมูล inventory และ equipment
        try
        {
            PersistentPlayerData.Instance.LoadInventoryData(this);

        }
        catch (System.Exception e)
        {
        }

        // รออีก 3 frames แล้ว force refresh equipment
        yield return null;
        yield return null;
        yield return null;

        ForceRefreshAllEquipmentUI();

        // รออีก 2 frames แล้ว verify ผลลัพธ์
        yield return null;
        yield return null;

    }

    private void ForceRefreshAllEquipmentUI()
    {
        try
        {

            int refreshedManagers = 0;

            // 1. Force refresh EquipmentSlotManager
            var equipmentSlotManager = GetComponent<EquipmentSlotManager>();
            if (equipmentSlotManager != null)
            {
                if (equipmentSlotManager.IsConnected())
                {
                    equipmentSlotManager.ForceRefreshFromCharacter();
                    refreshedManagers++;
                }
                else
                {

                    // ลองใหม่หลัง 1 วินาที
                    StartCoroutine(RetryRefreshEquipmentUI());
                }
            }
            else
            {
            }

            // 2. Force refresh CombatUIManager equipment
            var combatUIManager = FindObjectOfType<CombatUIManager>();
            if (combatUIManager?.equipmentSlotManager != null)
            {
                if (combatUIManager.equipmentSlotManager.IsConnected())
                {
                    combatUIManager.equipmentSlotManager.ForceRefreshFromCharacter();
                    refreshedManagers++;
                }
                else
                {
                }
            }
            else
            {
            }

            // 3. แจ้ง stats changed
            RaiseOnStatsChanged();

            // 4. Force update Canvas
            Canvas.ForceUpdateCanvases();


           
        }
        catch (System.Exception e)
        {
        }
    }

    // 🆕 เพิ่ม method สำหรับ debug equipment managers
   
    private System.Collections.IEnumerator RetryRefreshEquipmentUI()
    {
        int maxRetries = 10;
        int retryCount = 0;

        while (retryCount < maxRetries)
        {
            yield return new WaitForSeconds(0.5f); // รอ 0.5 วินาที
            retryCount++;

            var equipmentSlotManager = GetComponent<EquipmentSlotManager>();
            if (equipmentSlotManager != null && equipmentSlotManager.IsConnected())
            {
                equipmentSlotManager.ForceRefreshFromCharacter();
                break;
            }

        }

        if (retryCount >= maxRetries)
        {
        }
    }
    // ใน Character.cs - แก้ไข InitializeComponents()

    private void InitializeComponents()
    {
        // Get or add components
        statusEffectManager = GetComponent<StatusEffectManager>();
        if (statusEffectManager == null)
            statusEffectManager = gameObject.AddComponent<StatusEffectManager>();

        combatManager = GetComponent<CombatManager>();
        if (combatManager == null)
            combatManager = gameObject.AddComponent<CombatManager>();

        visualManager = GetComponent<CharacterVisualManager>();
        if (visualManager == null)
            visualManager = gameObject.AddComponent<CharacterVisualManager>();

        levelManager = GetComponent<LevelManager>();
        if (levelManager == null)
            levelManager = gameObject.AddComponent<LevelManager>();

        // 🆕 เพิ่มการตรวจสอบว่าเป็น Enemy หรือไม่
        bool isEnemy = GetComponent<NetworkEnemy>() != null;

        // ✅ Components ที่ Player เท่านั้นที่ต้องการ
        if (!isEnemy)
        {
            equipmentManager = GetComponent<EquipmentManager>();
            if (equipmentManager == null)
                equipmentManager = gameObject.AddComponent<EquipmentManager>();

            inventory = GetComponent<Inventory>();
            if (inventory == null)
                inventory = gameObject.AddComponent<Inventory>();

            equipmentSlotManager = GetComponent<EquipmentSlotManager>();
            if (equipmentSlotManager == null)
                equipmentSlotManager = gameObject.AddComponent<EquipmentSlotManager>();
            setBonusManager = GetComponent<SetBonusManager>();
            if (setBonusManager == null)
                setBonusManager = gameObject.AddComponent<SetBonusManager>();

            // 🆕 ✅ Auto-setup SetBonusManager
        }
        else
        {
        }
    }
   
    private void InitializePhysics()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationY;
            rb.useGravity = true;
            rb.linearDamping = 1.0f;
            rb.mass = 10f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }

    protected virtual void InitializeStats()
    {
        if (characterStats != null)
        {
            // ✅ ใช้ ScriptableObject เป็นหลักสำหรับทุกตัวละคร
            characterName = characterStats.characterName;
            maxHp = characterStats.maxHp;
            currentHp = maxHp;
            maxMana = characterStats.maxMana;
            currentMana = maxMana;
            attackDamage = characterStats.attackDamage;
            magicDamage = characterStats.magicDamage;
            armor = characterStats.arrmor; // ✅ ใช้ arrmor (ตัวแปรเดิมใน ScriptableObject)
            moveSpeed = characterStats.moveSpeed;
            attackRange = characterStats.attackRange;
            attackCooldown = characterStats.attackCoolDown;
            criticalChance = characterStats.criticalChance;
            criticalDamageBonus = characterStats.criticalDamageBonus;
            hitRate = characterStats.hitRate;
            evasionRate = characterStats.evasionRate;
            attackSpeed = characterStats.attackSpeed;
            reductionCoolDown = characterStats.reductionCoolDown;
            lifeSteal = characterStats.lifeSteal;
            ampDamage = characterStats.ampdamage;

            // ✅ แก้ไข Magic Armor - ใช้ค่าจาก ScriptableObject โดยตรง
            magicArmor = characterStats.magicArmor; // ไม่ใช้ arrmor ซ้ำ
            manaRegen = characterStats.manaRegen;
            healthRegen = characterStats.healthRegen;
            attackType = characterStats.attackType;

            InitializeEquipmentSlots();

            Debug.Log($"[Character.InitializeStats] {CharacterName} initialized:");
            Debug.Log($"  Armor: {armor}, Magic Armor: {magicArmor}");
            Debug.Log($"  Health Regen: {healthRegen}, Mana Regen: {manaRegen}");
        }
    }
    #endregion

    #region Fusion Network Methods RPC, Spawned, FixedUpdateNetwork
    public override void Spawned()
    {
        base.Spawned();


        if (HasStateAuthority)
        {
            NetworkedMaxHp = maxHp;
            NetworkedCurrentHp = currentHp;
            NetworkedMaxMana = maxMana;
            NetworkedCurrentMana = currentMana;
            IsNetworkStateReady = true;
        }

        // 🆕 เริ่มการโหลด stats หลัง spawn
        isSpawnComplete = true;

        // 🆕 เฉพาะ InputAuthority ที่จะโหลดข้อมูลจาก Firebase
        if (HasInputAuthority)
        {
            StartCoroutine(PostSpawnStatsLoading());

            // 🆕 เริ่ม auto-check equipment หลังจาก loading เสร็จ
            StartCoroutine(AutoFixEquipmentIfNeeded());
        }
    }

    private IEnumerator PostSpawnStatsLoading()
    {
        Debug.Log("[Character] 🔄 Post spawn stats loading...");

        // 🔧 รอให้ PersistentPlayerData โหลดเสร็จก่อน - เพิ่มเวลารอ
        yield return new WaitUntil(() => PersistentPlayerData.Instance != null);

        // 🔧 รอให้ data โหลดเสร็จจริงๆ
        float waitTime = 0f;
        while (!PersistentPlayerData.Instance.isDataLoaded && waitTime < 15f)
        {
            yield return new WaitForSeconds(0.5f);
            waitTime += 0.5f;
            Debug.Log($"[Character] Waiting for data... {waitTime}s");
        }

        if (!PersistentPlayerData.Instance.isDataLoaded)
        {
            Debug.LogError("[Character] Timeout waiting for PersistentPlayerData!");
            isStatsLoaded = true;
            IsStatsReady = true;
            yield break;
        }

        // 🔧 ตรวจสอบว่ามีข้อมูลจริงๆ ก่อนโหลด
        bool hasValidData = PersistentPlayerData.Instance.HasValidData();
        Debug.Log($"[Character] Has valid data: {hasValidData}");

        if (hasValidData)
        {
            var levelManager = GetComponent<LevelManager>();
            if (levelManager != null)
            {
                levelManager.RefreshCharacterData();
                yield return new WaitForSeconds(0.5f);
            }

            yield return StartCoroutine(SimpleLoadEquipmentOnly());
        }
        else
        {
            Debug.Log("[Character] No valid data found, using defaults");
        }

        isStatsLoaded = true;
        IsStatsReady = true;
    }
    private IEnumerator SimpleLoadEquipmentOnly()
    {
        Debug.Log("[Character] 🔄 Starting simple equipment load...");

        bool shouldHaveEquipment = false;
        try
        {
            // ตรวจสอบว่าควรมี equipment หรือไม่
            shouldHaveEquipment = PersistentPlayerData.Instance?.multiCharacterData
                ?.GetActiveCharacterData()?.HasEquipmentData() ?? false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Character] Error checking equipment data: {e.Message}");
            yield break;
        }

        if (shouldHaveEquipment)
        {
            Debug.Log("[Character] Equipment data found, loading...");

            // ✅ ขั้นตอนที่ 1: บันทึก current HP/Mana percentage ก่อนทำอะไร
            float initialHpPercentage = MaxHp > 0 ? (float)CurrentHp / MaxHp : 1f;
            float initialManaPercentage = MaxMana > 0 ? (float)CurrentMana / MaxMana : 1f;

            Debug.Log($"[Character] 📊 Initial HP/Mana state: HP={CurrentHp}/{MaxHp} ({initialHpPercentage:P0}), Mana={CurrentMana}/{MaxMana} ({initialManaPercentage:P0})");

            // ✅ ขั้นตอนที่ 2: Apply base stats ก่อน (รวม stat upgrades แล้ว)
            ApplyBaseStatsBeforeEquipment();

            yield return new WaitForSeconds(0.2f);

            // ✅ ขั้นตอนที่ 3: เคลียร์ equipment ปัจจุบัน
            ClearAllEquipmentForLoad();

            yield return new WaitForSeconds(0.2f);

            // ✅ ขั้นตอนที่ 4: โหลด equipment จาก Firebase
            try
            {
                PersistentPlayerData.Instance.LoadInventoryData(this);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Character] Error loading equipment: {e.Message}");
                yield break;
            }

            yield return new WaitForSeconds(0.5f);

            int equipmentCount = 0;
            try
            {
                // ตรวจสอบผลลัพธ์
                equipmentCount = GetAllEquippedItems().Count;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Character] Error counting equipment: {e.Message}");
                yield break;
            }

            Debug.Log($"[Character] Equipment loaded: {equipmentCount} items");

            if (equipmentCount > 0)
            {
                // ✅ ขั้นตอนที่ 5: Apply equipment bonuses ทับ base stats (โดยรักษา HP percentage)
                ApplyLoadedEquipmentStatsWithPreservedHP(initialHpPercentage, initialManaPercentage);
                ForceUpdateEquipmentSlotsNow();
            }
            else
            {
                Debug.Log("[Character] No equipment found after loading");

                // ✅ ถ้าไม่มี equipment ให้รักษา HP percentage เดิม
                CurrentHp = Mathf.RoundToInt(MaxHp * initialHpPercentage);
                CurrentMana = Mathf.RoundToInt(MaxMana * initialManaPercentage);
                CurrentHp = Mathf.Clamp(CurrentHp, 1, MaxHp);
                CurrentMana = Mathf.Clamp(CurrentMana, 0, MaxMana);

                ForceUpdateNetworkState();
                Debug.Log($"[Character] Preserved HP without equipment: {CurrentHp}/{MaxHp} ({(float)CurrentHp / MaxHp:P0})");
            }
        }
        else
        {
            Debug.Log("[Character] No equipment data to load");

            // ✅ ถ้าไม่มี equipment ให้ apply base stats เฉยๆ และรักษา HP percentage
            float hpPercentage = MaxHp > 0 ? (float)CurrentHp / MaxHp : 1f;
            float manaPercentage = MaxMana > 0 ? (float)CurrentMana / MaxMana : 1f;

            ApplyBaseStatsBeforeEquipment();

            // รักษา HP percentage
            CurrentHp = Mathf.RoundToInt(MaxHp * hpPercentage);
            CurrentMana = Mathf.RoundToInt(MaxMana * manaPercentage);
            CurrentHp = Mathf.Clamp(CurrentHp, 1, MaxHp);
            CurrentMana = Mathf.Clamp(CurrentMana, 0, MaxMana);

            ForceUpdateNetworkState();
        }
    }
    private void ApplyLoadedEquipmentStatsWithPreservedHP(float preservedHpPercentage, float preservedManaPercentage)
    {
        try
        {
            Debug.Log($"[Character] ⚔️ Applying equipment with preserved HP: {preservedHpPercentage:P0}");

            // เก็บ base stats ก่อน apply equipment
            int baseHp = MaxHp;
            int baseMana = MaxMana;
            int baseAttack = AttackDamage;
            float baseLifeSteal = LifeSteal;

            // ✅ Apply เฉพาะ equipment bonuses
            ApplyEquipmentBonusesOnly();

            // ✅ ใช้ preserved percentage แทนการคำนวณใหม่
            CurrentHp = Mathf.RoundToInt(MaxHp * preservedHpPercentage);
            CurrentMana = Mathf.RoundToInt(MaxMana * preservedManaPercentage);
            CurrentHp = Mathf.Clamp(CurrentHp, 1, MaxHp);
            CurrentMana = Mathf.Clamp(CurrentMana, 0, MaxMana);

            // Debug การเปลี่ยนแปลง
            int hpBonus = MaxHp - baseHp;
            int manaBonus = MaxMana - baseMana;
            int atkBonus = AttackDamage - baseAttack;
            float lifeStealBonus = LifeSteal - baseLifeSteal;

            

            // Force update network state
            ForceUpdateNetworkState();

            // แจ้ง stats changed
            OnStatsChanged?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Character] ❌ Error applying equipment with preserved HP: {e.Message}");
        }
    }


    // ใน Character.cs - เพิ่ม method ใหม่สำหรับ apply base stats ก่อน load equipment
    // แก้ไขใน Character.cs - ApplyBaseStatsBeforeEquipment method

    private void ApplyBaseStatsBeforeEquipment()
    {
        try
        {
            var characterData = PersistentPlayerData.Instance?.GetCurrentCharacterData();
            if (characterData == null)
            {
                Debug.LogWarning("[Character] No character data for base stats");
                return;
            }

            if (characterData.HasValidBaseStats())
            {
                Debug.Log("[Character] 📊 Applying base stats before equipment load...");

                // ✅ เก็บ HP/Mana percentage ก่อนเปลี่ยน stats - แต่ให้ default เป็น 100% ถ้ายังไม่มีข้อมูล
                float hpPercentage = MaxHp > 0 ? (float)CurrentHp / MaxHp : 1f;
                float manaPercentage = MaxMana > 0 ? (float)CurrentMana / MaxMana : 1f;

                // ✅ ถ้าเป็นการโหลดครั้งแรกหลัง login ให้ใช้ 100%
                bool isFirstLoad = (CurrentHp == 0 || MaxHp == 0);
                if (isFirstLoad)
                {
                    hpPercentage = 1f;
                    manaPercentage = 1f;
                    Debug.Log("[Character] First load detected, using 100% HP/Mana");
                }

                // Apply base stats
                MaxHp = characterData.baseMaxHp;
                MaxMana = characterData.baseMaxMana;
                AttackDamage = characterData.baseAttackDamage;
                MagicDamage = characterData.baseMagicDamage;
                Armor = characterData.baseArmor;
                CriticalChance = characterData.baseCriticalChance;
                UpdateCriticalDamageBonus(characterData.baseCriticalDamageBonus, false);
                MoveSpeed = characterData.baseMoveSpeed;
                HitRate = characterData.baseHitRate;
                EvasionRate = characterData.baseEvasionRate;
                AttackSpeed = characterData.baseAttackSpeed;
                ReductionCoolDown = characterData.baseReductionCoolDown;
                LifeSteal = characterData.baseLifeSteal;
                AmpDamage = characterData.baseAmpDamage;
                // ✅ แก้ไข 3 stats ใหม่ - ใช้ค่าจาก characterData โดยตรง
                MagicArmor = characterData.baseMagicArmor;
                HealthRegen = characterData.baseHealthRegen;
                ManaRegen = characterData.baseManaRegen;

                // ✅ ปรับ currentHp และ currentMana ตามเปอร์เซ็นต์
                CurrentHp = Mathf.RoundToInt(MaxHp * hpPercentage);
                CurrentMana = Mathf.RoundToInt(MaxMana * manaPercentage);
                CurrentHp = Mathf.Clamp(CurrentHp, 1, MaxHp);
                CurrentMana = Mathf.Clamp(CurrentMana, 0, MaxMana);

                ForceUpdateNetworkState();

              
            }
            else
            {
                Debug.LogWarning("[Character] No valid base stats found, keeping current stats");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Character] ❌ Error applying base stats: {e.Message}");
        }
    }






    // แก้ไขใน Character.cs - ApplyLoadedEquipmentStats method

    public void ApplyLoadedEquipmentStats()
    {
        try
        {

            // ✅ เก็บ HP/Mana percentage ก่อน apply equipment
            float hpPercentage = MaxHp > 0 ? (float)CurrentHp / MaxHp : 1f;
            float manaPercentage = MaxMana > 0 ? (float)CurrentMana / MaxMana : 1f;


            // เก็บ base stats ก่อน apply equipment
            int baseHp = MaxHp;
            int baseMana = MaxMana;
            int baseAttack = AttackDamage;
            float baseLifeSteal = LifeSteal;

            // ✅ Apply เฉพาะ equipment bonuses (ไม่รวม stat upgrades)
            ApplyEquipmentBonusesOnly();

            // ✅ ปรับ CurrentHp และ CurrentMana ตาม MaxHp/MaxMana ใหม่
            CurrentHp = Mathf.RoundToInt(MaxHp * hpPercentage);
            CurrentMana = Mathf.RoundToInt(MaxMana * manaPercentage);
            CurrentHp = Mathf.Clamp(CurrentHp, 1, MaxHp);
            CurrentMana = Mathf.Clamp(CurrentMana, 0, MaxMana);

            // Debug การเปลี่ยนแปลง
            int hpBonus = MaxHp - baseHp;
            int manaBonus = MaxMana - baseMana;
            int atkBonus = AttackDamage - baseAttack;
            float lifeStealBonus = LifeSteal - baseLifeSteal;

            

            // Force update network state
            ForceUpdateNetworkState();

            // แจ้ง stats changed
            OnStatsChanged?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Character] ❌ Error applying equipment stats: {e.Message}");
        }
    }

    private void ApplyEquipmentBonusesOnly()
    {
        try
        {
            if (equipmentManager == null) return;

            // ดึง equipment stats
            EquipmentStats equipmentStats = equipmentManager.GetTotalStats();

           

            // Apply equipment bonuses
            MaxHp += equipmentStats.maxHpBonus;
            MaxMana += equipmentStats.maxManaBonus;
            AttackDamage += equipmentStats.attackDamageBonus;
            MagicDamage += equipmentStats.magicDamageBonus;
            Armor += equipmentStats.armorBonus;
            CriticalChance += equipmentStats.criticalChanceBonus;
            UpdateCriticalDamageBonus(CriticalDamageBonus + equipmentStats.criticalMultiplierBonus, false);
            MoveSpeed += equipmentStats.moveSpeedBonus;
            HitRate += equipmentStats.hitRateBonus;
            EvasionRate += equipmentStats.evasionRateBonus;
            AttackSpeed += equipmentStats.attackSpeedBonus;
            ReductionCoolDown += equipmentStats.reductionCoolDownBonus;
            LifeSteal += equipmentStats.lifeStealBonus;
            AmpDamage += equipmentStats.ampDamageBonus;
            MagicArmor += equipmentStats.magicArmorBonus;
            HealthRegen += equipmentStats.healthRegenBonus;
            ManaRegen += equipmentStats.manaRegenBonus;

        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Character] ❌ Error applying equipment bonuses: {e.Message}");
        }
    }
    public void ApplyLoadedEquipmentStatsWithReset()
    {
        try
        {

            // 🆕 เก็บ HP/Mana percentage ก่อนเปลี่ยน stats
            

            // 1. Reset กลับเป็น base stats ก่อน

            // 2. เก็บ base stats สำหรับ comparison
            int baseMaxHp = MaxHp;
            int baseAttackDamage = AttackDamage;
            int baseArmor = Armor;
            float baseCriticalChance = CriticalChance;

            // 3. Apply equipment stats ใหม่
            ApplyAllEquipmentStats();

            // 🆕 ปรับ currentHp และ currentMana ตามเปอร์เซ็นต์เดิม (ทำซ้ำเพื่อความแน่ใจ)
            CurrentHp = MaxHp ;
            CurrentMana = MaxMana ;
            CurrentHp = Mathf.Clamp(CurrentHp, 1, MaxHp);
            CurrentMana = Mathf.Clamp(CurrentMana, 0, MaxMana);

            // 4. Debug การเปลี่ยนแปลง
            int hpBonus = MaxHp - baseMaxHp;
            int atkBonus = AttackDamage - baseAttackDamage;
            int armBonus = Armor - baseArmor;
            float critBonus = CriticalChance - baseCriticalChance;



            // 5. Force update network state
            ForceUpdateNetworkState();

            // 6. แจ้ง stats changed
            OnStatsChanged?.Invoke();
        }
        catch (System.Exception e)
        {
        }
    }














    #region 🆕 Auto-Fix System

    /// <summary>
    /// 🆕 ตรวจสอบและแก้ไขปัญหาอัตโนมัติ
    /// </summary>
    private IEnumerator AutoFixEquipmentIfNeeded()
    {
        yield return new WaitForSeconds(2f); // รอให้ loading เสร็จก่อน


        // ตรวจสอบว่ามี equipment หรือไม่
        int equipmentCount = GetAllEquippedItems().Count;
        bool hasEquipmentInFirebase = PersistentPlayerData.Instance?.multiCharacterData
            ?.GetCurrentCharacterData()?.HasEquipmentData() ?? false;


        if (hasEquipmentInFirebase && equipmentCount == 0)
        {

            // ทำ auto-fix
            yield return StartCoroutine(AutoFixEquipmentMismatch());
        }
        else
        {
        }
    }

    /// <summary>
    /// 🆕 Auto-fix equipment mismatch
    /// </summary>
    private IEnumerator AutoFixEquipmentMismatch()
    {

        // Clear และ reload
        ClearAllEquipmentForLoad();
        yield return new WaitForSeconds(0.2f);

        // Reload equipment
        PersistentPlayerData.Instance?.LoadInventoryData(this);
        yield return new WaitForSeconds(0.5f);

        // Apply stats
        ApplyLoadedEquipmentStats();
        yield return new WaitForSeconds(0.2f);

        // Refresh UI
        ForceUpdateEquipmentSlotsNow();
        OnStatsChanged?.Invoke();

        int equipmentCountAfterFix = GetAllEquippedItems().Count;

        if (equipmentCountAfterFix > 0)
        {
        }
        else
        {
        }
    }

    #endregion

   
 

   

   
   
    

    // 🆕 Network RPCs สำหรับ sync stats
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestStatsSync()
    {

        // StateAuthority ส่ง stats ไปให้ทุกคน
        RPC_SyncStatsToAll(
            maxHp, maxMana, attackDamage, magicDamage, armor,
            criticalChance, criticalDamageBonus, moveSpeed,
            hitRate, evasionRate, attackSpeed, reductionCoolDown
        );
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SyncStatsToAll(int hp, int mana, int atk, int magic, int arm,
        float crit, float critDmg, float speed, float hit, float eva, float atkSpeed, float cdr)
    {

        // Apply synced stats
        maxHp = hp;
        maxMana = mana;
        attackDamage = atk;
        magicDamage = magic;
        armor = arm;
        criticalChance = crit;
        criticalDamageBonus = critDmg;
        moveSpeed = speed;
        hitRate = hit;
        evasionRate = eva;
        attackSpeed = atkSpeed;
        reductionCoolDown = cdr;

        // Update current HP/Mana accordingly
        currentHp = Mathf.Min(currentHp, maxHp);
        currentMana = Mathf.Min(currentMana, maxMana);

        ForceUpdateNetworkState();
        OnStatsChanged?.Invoke();

    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyStatsLoaded()
    {
        isStatsLoaded = true;
    }


    public override void FixedUpdateNetwork()
    {
        if (Setting.IsPaused) return;

        if (HasStateAuthority)
        {
            // Sync HP/Mana ทุก frame
            NetworkedCurrentHp = currentHp;
            NetworkedCurrentMana = currentMana;
            NetworkedMaxHp = maxHp;
            NetworkedMaxMana = maxMana;
            ProcessRegeneration();
        }

        if (GetInput<NetworkInputData>(out var input))
        {
            HandlePotionInputs(input);
        }
    }
    protected virtual void HandlePotionInputs(NetworkInputData input)
    {
        // ตรวจสอบว่าเป็น InputAuthority หรือไม่
        if (!HasInputAuthority)
            return;

        // จัดการ potion inputs (ใช้ implicit bool conversion)
        if (input.potion1 && CanUsePotion(0))
        {
            UsePotion(0);
        }

        if (input.potion2 && CanUsePotion(1))
        {
            UsePotion(1);
        }

        if (input.potion3 && CanUsePotion(2))
        {
            UsePotion(2);
        }

        if (input.potion4 && CanUsePotion(3))
        {
            UsePotion(3);
        }

        if (input.potion5 && CanUsePotion(4))
        {
            UsePotion(4);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    protected virtual void RPC_RequestDeath()
    {
        // StateAuthority ตรวจสอบและตัดสินใจ
        if (CanDie()) // เพิ่ม validation
        {
            RPC_OnDeath();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    protected virtual void RPC_OnDeath()
    {
        Debug.Log($"{CharacterName} died!");
        // Handle death logic here
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SyncHealthRegen(int newHp)
    {
        currentHp = newHp;
        NetworkedCurrentHp = newHp;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SyncManaRegen(int newMana)
    {
        currentMana = newMana;
        NetworkedCurrentMana = newMana;
    }

    public void ForceUpdateNetworkState()
    {
        if (HasStateAuthority)
        {
            NetworkedMaxHp = maxHp;
            NetworkedCurrentHp = currentHp;
            NetworkedMaxMana = maxMana;
            NetworkedCurrentMana = currentMana;
            NetworkedLifeSteal = lifeSteal;
            NetworkedManaRegen = manaRegen;
            NetworkedHealthRegen = healthRegen;

            IsNetworkStateReady = true;
        }
    }
    #endregion

    #region Damage Calculation Methods  การคำนวณดาเมจสำหรับ Attack Types ต่างๆ
    // ใน Character.cs - แทนที่ method เดิม
    public virtual (int physicalDamage, int magicDamage) GetAttackDamages()
    {
        int finalPhysicalDamage = AttackDamage;
        int finalMagicDamage = MagicDamage;

        if (statusEffectManager != null)
        {
            // Physical Damage Aura
            float physicalMultiplier = statusEffectManager.GetTotalDamageMultiplier();
            finalPhysicalDamage = Mathf.RoundToInt(AttackDamage * physicalMultiplier);

            // ✅ Magic Damage Aura (เพิ่ม debug)
            float magicMultiplier = statusEffectManager.GetTotalMagicDamageMultiplier();
            finalMagicDamage = Mathf.RoundToInt(MagicDamage * magicMultiplier);

            // ✅ Debug log
            if (magicMultiplier > 1f)
            {
                Debug.Log($"[GetAttackDamages] {CharacterName}: " +
                         $"Base Magic = {MagicDamage}, Multiplier = {magicMultiplier:F2}, " +
                         $"Final Magic = {finalMagicDamage}");
            }
        }

        switch (attackType)
        {
            case AttackType.Physical:
                return (finalPhysicalDamage, 0);

            case AttackType.Magic:
                return (0, finalMagicDamage);

            case AttackType.Mixed:
                return (finalPhysicalDamage, finalMagicDamage);

            default:
                return (finalPhysicalDamage, 0);
        }
    }

    // ใน Character.cs - แทนที่ method เดิม
    public virtual (int physicalDamage, int magicDamage) GetSkillDamages(AttackType skillType, float physicalRatio = 1f, float magicRatio = 1f)
    {
        int basePhysical = Mathf.RoundToInt(AttackDamage * physicalRatio);
        int baseMagic = Mathf.RoundToInt(MagicDamage * magicRatio);

        // ✅ เพิ่ม Aura Bonus
        if (statusEffectManager != null)
        {
            float physicalMultiplier = statusEffectManager.GetTotalDamageMultiplier();
            basePhysical = Mathf.RoundToInt(basePhysical * physicalMultiplier);

            float magicMultiplier = statusEffectManager.GetTotalMagicDamageMultiplier();
            baseMagic = Mathf.RoundToInt(baseMagic * magicMultiplier);
        }

        switch (skillType)
        {
            case AttackType.Physical:
                return (basePhysical, 0);

            case AttackType.Magic:
                return (0, baseMagic);

            case AttackType.Mixed:
                return (basePhysical, baseMagic);

            default:
                return (basePhysical, 0);
        }
    }

    public virtual (int physicalDamage, int magicDamage) GetSkillDamages(AttackType skillType, int flatPhysical = 0, int flatMagic = 0)
    {
        switch (skillType)
        {
            case AttackType.Physical:
                return (flatPhysical, 0);

            case AttackType.Magic:
                return (0, flatMagic);

            case AttackType.Mixed:
                return (flatPhysical, flatMagic);

            default:
                return (flatPhysical, 0);
        }
    }

    public virtual (int physicalDamage, int magicDamage) GetAdvancedSkillDamages(
        float physicalRatio = 0f, float magicRatio = 0f,
        int flatPhysical = 0, int flatMagic = 0)
    {
        int physDamage = Mathf.RoundToInt(AttackDamage * physicalRatio) + flatPhysical;
        int magDamage = Mathf.RoundToInt(MagicDamage * magicRatio) + flatMagic;

        return (physDamage, magDamage);
    }
    #endregion

    #region Skill and Combat Methods Skills, การโจมตี, การรับดาเมจ, การรักษา
    public virtual void UseSkillOnTarget(Character target, AttackType skillType, float physicalRatio = 1f, float magicRatio = 1f, DamageType damageType = DamageType.Normal)
    {
        if (combatManager == null) return;

        var (physicalDamage, magicDamage) = GetSkillDamages(skillType, physicalRatio, magicRatio);

        Debug.Log($"[UseSkillOnTarget] {CharacterName} uses skill on {target.CharacterName} (no life steal)");

        // ✅ ระบุว่าเป็น skill attack - ไม่มี life steal
        combatManager.TakeDamageFromAttacker(physicalDamage, magicDamage, this, damageType, false);
    }

    public virtual void UseSkillOnTarget(Character target, AttackType skillType, int flatPhysical, int flatMagic, DamageType damageType = DamageType.Normal)
    {
        if (combatManager == null) return;

        var (physicalDamage, magicDamage) = GetSkillDamages(skillType, flatPhysical, flatMagic);

        Debug.Log($"[UseSkillOnTarget] {CharacterName} uses skill with flat damage on {target.CharacterName} (no life steal)");

        // ✅ ระบุว่าเป็น skill attack - ไม่มี life steal
        target.TakeDamageFromAttacker(physicalDamage, magicDamage, this, damageType, false);
    }

    public virtual void UseAdvancedSkillOnTarget(Character target,
     float physicalRatio = 0f, float magicRatio = 0f,
     int flatPhysical = 0, int flatMagic = 0,
     DamageType damageType = DamageType.Normal)
    {
        if (combatManager == null) return;

        var (physicalDamage, magicDamage) = GetAdvancedSkillDamages(physicalRatio, magicRatio, flatPhysical, flatMagic);

        Debug.Log($"[UseAdvancedSkillOnTarget] {CharacterName} uses advanced skill on {target.CharacterName} (no life steal)");

        // ✅ ระบุว่าเป็น skill attack - ไม่มี life steal
        target.TakeDamageFromAttacker(physicalDamage, magicDamage, this, damageType, false);
    }

    public virtual void TakeDamage(int damage, DamageType damageType = DamageType.Normal, bool isCritical = false)
    {
        if (combatManager == null) return;
        combatManager.TakeDamage(damage, damageType, isCritical);
    }

    public virtual void TakeDamageFromAttacker(int damage, Character attacker, DamageType damageType = DamageType.Normal, bool isBasicAttack = false)
    {
        if (combatManager == null) return;
        combatManager.TakeDamageFromAttacker(damage, attacker, damageType, isBasicAttack);
    }

    public virtual void TakeDamageFromAttacker(int physicalDamage, int magicDamage, Character attacker, DamageType damageType = DamageType.Normal, bool isBasicAttack = false)
    {
        if (combatManager == null) return;
        combatManager.TakeDamageFromAttacker(physicalDamage, magicDamage, attacker, damageType, isBasicAttack);
    }

    public void Heal(int amount)
    {
        if (combatManager == null) return;
        combatManager.Heal(amount);
    }
    #endregion

    #region Status Effect Methods การใช้และตรวจสอบ status effects
    public void ApplyStatusEffect(StatusEffectType effectType, int damage = 0, float duration = 0f, float amount = 0f)
    {
        if (statusEffectManager == null) return;

        switch (effectType)
        {
            // Magical Effects
            case StatusEffectType.Poison:
                statusEffectManager.ApplyPoison(damage, duration);
                break;
            case StatusEffectType.Burn:
                statusEffectManager.ApplyBurn(damage, duration);
                break;
            case StatusEffectType.Bleed:
                statusEffectManager.ApplyBleed(damage, duration);
                break;
            case StatusEffectType.Freeze:
                statusEffectManager.ApplyFreeze(duration);
                break;

            // Physical Effects
            case StatusEffectType.Stun:
                statusEffectManager.ApplyStun(duration);
                break;
            case StatusEffectType.ArmorBreak:
                statusEffectManager.ApplyArmorBreak(duration, amount > 0 ? amount : 0.5f);
                break;
            case StatusEffectType.Blind:
                statusEffectManager.ApplyBlind(duration, amount > 0 ? amount : 0.8f);
                break;
            case StatusEffectType.Weakness:
                statusEffectManager.ApplyWeakness(duration, amount > 0 ? amount : 0.4f);
                break;
        }
    }

    public bool HasStatusEffect(StatusEffectType effectType)
    {
        if (statusEffectManager == null) return false;

        switch (effectType)
        {
            // Magical Effects
            case StatusEffectType.Poison:
                return statusEffectManager.IsPoisoned;
            case StatusEffectType.Burn:
                return statusEffectManager.IsBurning;
            case StatusEffectType.Bleed:
                return statusEffectManager.IsBleeding;
            case StatusEffectType.Freeze:
                return statusEffectManager.IsFrozen;

            // Physical Effects
            case StatusEffectType.Stun:
                return statusEffectManager.IsStunned;
            case StatusEffectType.ArmorBreak:
                return statusEffectManager.IsArmorBreak;
            case StatusEffectType.Blind:
                return statusEffectManager.IsBlind;
            case StatusEffectType.Weakness:
                return statusEffectManager.IsWeak;

            case StatusEffectType.AttackSpeedAura:
                return statusEffectManager.IsProvidingAttackSpeedAura;
            case StatusEffectType.DamageAura:
                return statusEffectManager.IsProvidingDamageAura;
            case StatusEffectType.MoveSpeedAura:
                return statusEffectManager.IsProvidingMoveSpeedAura;
            case StatusEffectType.ProtectionAura:
                return statusEffectManager.IsProvidingProtectionAura;
            case StatusEffectType.ArmorAura:
                return statusEffectManager.IsProvidingArmorAura;
            case StatusEffectType.CriticalAura:
                return statusEffectManager.IsProvidingCriticalAura;

            default:
                return false;
        }
    }
    #endregion

    #region Equipment Methods การจัดการอุปกรณ์และ runes

    #endregion
    #region Inventory Methods การจัดการ inventory และ items
    public Inventory GetInventory()
    {
        return inventory;
    }

    public int GetInventorySlotCount()
    {
        if (inventory == null) return 24; // default slots
        return inventory.CurrentSlots;
    }

    public int GetInventoryMaxSlots()
    {
        if (inventory == null) return 48; // default max slots
        return inventory.MaxSlots;
    }

    public bool AddItemToInventory(ItemData itemData, int count = 1)
    {
        if (inventory == null) return false;
        return inventory.AddItem(itemData, count);
    }

    public bool RemoveItemFromInventory(int slotIndex, int count = 1)
    {
        if (inventory == null) return false;
        return inventory.RemoveItem(slotIndex, count);
    }

    public InventoryItem GetInventoryItem(int slotIndex)
    {
        if (inventory == null) return null;
        return inventory.GetItem(slotIndex);
    }

    public bool IsInventorySlotEmpty(int slotIndex)
    {
        if (inventory == null) return true;
        return inventory.IsSlotEmpty(slotIndex);
    }

    public void ExpandInventory(int additionalSlots)
    {
        if (inventory == null) return;
        inventory.ExpandInventory(additionalSlots);
    }

    public bool CanExpandInventory(int additionalSlots)
    {
        if (inventory == null) return false;
        return inventory.CanExpandInventory(additionalSlots);
    }
    #endregion
    #region Regeneration System ระบบการฟื้นฟู HP/Mana อัตโนมัติ
    private void ProcessRegeneration()
    {
        // Health Regeneration
        if (currentHp < maxHp)
        {
            healthRegenTimer += Runner.DeltaTime;
            if (healthRegenTimer >= regenTickInterval)
            {
                RegenerateHealth();
                healthRegenTimer = 0f;
            }
        }

        // Mana Regeneration
        if (currentMana < maxMana)
        {
            manaRegenTimer += Runner.DeltaTime;
            if (manaRegenTimer >= regenTickInterval)
            {
                RegenerateMana();
                manaRegenTimer = 0f;
            }
        }
    }
    private void RegenerateHealth()
    {
        // รวม base regen + equipment bonus + set bonus
        float totalHealthRegen = baseHealthRegenPerSecond + HealthRegen;

        if (equipmentManager != null)
        {
            totalHealthRegen += equipmentManager.GetHealthRegenBonus();
        }

        // ✅ ใช้ค่า cap ที่กำหนดเอง แทนที่จะคำนวณจาก %
        int regenAmount = Mathf.RoundToInt(totalHealthRegen * regenTickInterval);
        regenAmount = Mathf.Min(regenAmount, Mathf.RoundToInt(maxHealthRegenPerTick));

        int oldHp = currentHp;
        currentHp = Mathf.Min(currentHp + regenAmount, maxHp);

        if (currentHp > oldHp)
        {
            NetworkedCurrentHp = currentHp;

            float equipmentBonus = equipmentManager != null ? equipmentManager.GetHealthRegenBonus() : 0f;
            Debug.Log($"[Health Regen] {CharacterName}: +{currentHp - oldHp} HP (Cap: {maxHealthRegenPerTick})");
            Debug.Log($"  Base: {baseHealthRegenPerSecond:F1}/s + Character: {HealthRegen:F1}/s + Equipment: {equipmentBonus:F1}/s = {totalHealthRegen:F1}/s");
            Debug.Log($"  Calculated: {totalHealthRegen * regenTickInterval:F1}, Applied: {currentHp - oldHp}");

            if (HasStateAuthority)
            {
                RPC_SyncHealthRegen(currentHp);
            }
        }
    }

    private void RegenerateMana()
    {
        // รวม base regen + equipment bonus + set bonus
        float totalManaRegen = baseManaRegenPerSecond + ManaRegen;

        if (equipmentManager != null)
        {
            totalManaRegen += equipmentManager.GetManaRegenBonus();
        }

        // ✅ ใช้ค่า cap ที่กำหนดเอง
        int regenAmount = Mathf.RoundToInt(totalManaRegen * regenTickInterval);
        regenAmount = Mathf.Min(regenAmount, Mathf.RoundToInt(maxManaRegenPerTick));

        int oldMana = currentMana;
        currentMana = Mathf.Min(currentMana + regenAmount, maxMana);

        if (currentMana > oldMana)
        {
            NetworkedCurrentMana = currentMana;

            float equipmentBonus = equipmentManager != null ? equipmentManager.GetManaRegenBonus() : 0f;
            

            if (HasStateAuthority)
            {
                RPC_SyncManaRegen(currentMana);
            }
        }
    }
    public void SetHealthRegenCap(float newCap)
    {
        maxHealthRegenPerTick = newCap;
    }

    public void SetManaRegenCap(float newCap)
    {
        maxManaRegenPerTick = newCap;
    }

    // ✅ เพิ่ม methods สำหรับปรับ regen interval
    public void SetRegenInterval(float newInterval)
    {
        regenTickInterval = Mathf.Max(0.1f, newInterval); // อย่างน้อย 0.1 วินาที
    }

    // ✅ เพิ่ม method สำหรับดูสถานะ regen
    public void LogRegenStatus()
    {
        float totalHealthRegen = GetTotalHealthRegenRate();
        float totalManaRegen = GetTotalManaRegenRate();

       
    }
    public float GetTotalHealthRegenRate()
    {
        float total = baseHealthRegenPerSecond + HealthRegen;
        if (equipmentManager != null)
        {
            total += equipmentManager.GetHealthRegenBonus();
        }
        return total;
    }

    public float GetTotalManaRegenRate()
    {
        float total = baseManaRegenPerSecond + ManaRegen;
        if (equipmentManager != null)
        {
            total += equipmentManager.GetManaRegenBonus();
        }
        return total;
    }
    // ใน Character.cs - เพิ่ม method ใหม่ใน region Effective Stats Methods

    /// <summary>
    /// ✅ คำนวณ Armor ที่รวม Aura Bonus
    /// </summary>
    public virtual int GetEffectiveArmor()
    {
        int baseArmor = Armor;

        // ✅ รวม Armor Aura
        if (statusEffectManager != null)
        {
            float armorMultiplier = statusEffectManager.GetTotalArmorMultiplier();
            baseArmor = Mathf.RoundToInt(baseArmor * armorMultiplier);
        }

        Debug.Log($"[GetEffectiveArmor] {CharacterName}: Base={Armor}, Effective={baseArmor}");

        return baseArmor;
    }

    /// <summary>
    /// ✅ คำนวณ Magic Armor ที่รวม Aura Bonus
    /// </summary>
    public virtual int GetEffectiveMagicArmor()
    {
        int baseMagicArmor = MagicArmor;

        // ✅ รวม Magic Armor Aura
        if (statusEffectManager != null)
        {
            float magicArmorMultiplier = statusEffectManager.GetTotalMagicArmorMultiplier();
            baseMagicArmor = Mathf.RoundToInt(baseMagicArmor * magicArmorMultiplier);
        }

        Debug.Log($"[GetEffectiveMagicArmor] {CharacterName}: Base={MagicArmor}, Effective={baseMagicArmor}");

        return baseMagicArmor;
    }
    // ✅ เพิ่ม method ใน Character.cs สำหรับแจ้ง EquipmentManager ว่า stats เปลี่ยน

    public float GetCurrentHealthRegenRate()
    {
        return baseHealthRegenPerSecond + HealthRegen;
    }

    public float GetCurrentManaRegenRate()
    {
        return baseManaRegenPerSecond + ManaRegen;
    }
    #endregion

    #region Effective Stats Methods การคำนวณ stats ที่รวม buffs/debuffs
    public float GetEffectiveMoveSpeed()
    {
        float baseMoveSpeed = moveSpeed;

        // ✅ รวม Move Speed Aura
        if (statusEffectManager != null)
        {
            float moveSpeedMultiplier = statusEffectManager.GetTotalMoveSpeedMultiplier();
            baseMoveSpeed *= moveSpeedMultiplier;
        }

        // ✅ รวม Freeze effect (ใช้ระบบเดิม)
        if (HasStatusEffect(StatusEffectType.Freeze))
        {
            baseMoveSpeed *= 0.3f; // ลดความเร็วเหลือ 30% เมื่อ freeze
        }

        return baseMoveSpeed;
    }

    public virtual int ApplyAmpDamageToSkill(int baseDamage)
    {
        if (baseDamage <= 0) return baseDamage;

        // รวม Amp Damage จากทุกแหล่ง
        float totalAmpDamage = GetEffectiveAmpDamage();

        if (totalAmpDamage <= 0f) return baseDamage;

        // คำนวณ damage หลัง apply Amp Damage
        float multiplier = 1f + (totalAmpDamage / 100f);
        int finalDamage = Mathf.RoundToInt(baseDamage * multiplier);

        Debug.Log($"[Amp Damage] {CharacterName}: Base={baseDamage} × {multiplier:F2} = {finalDamage} (Amp: {totalAmpDamage:F1}%)");

        return finalDamage;
    }

    // เพิ่ม method สำหรับดึง effective Amp Damage
    public virtual float GetEffectiveAmpDamage()
    {
        // Base Amp Damage จาก character stats
        float totalAmpDamage = this.AmpDamage;

        // เพิ่ม Amp Damage จาก equipment
        if (equipmentManager != null)
        {
            totalAmpDamage += equipmentManager.GetAmpDamageBonus();
        }

        // เพิ่ม Amp Damage จาก status effects (ถ้ามี)
        if (statusEffectManager != null)
        {
            // สามารถเพิ่ม method GetAmpDamageBonus() ใน StatusEffectManager ได้ในอนาคต
            // totalAmpDamage += statusEffectManager.GetAmpDamageBonus();
        }

        Debug.Log($"[GetEffectiveAmpDamage] {CharacterName}: Character={this.AmpDamage}, Equipment={equipmentManager?.GetAmpDamageBonus() ?? 0f}, Total={totalAmpDamage:F1}%");

        return totalAmpDamage;
    }

    // ✅ โค้ดปัจจุบันถูกต้องแล้ว
    public float GetEffectiveReductionCoolDown()
    {
        // ใช้ this.ReductionCoolDown ที่รวม equipment bonus แล้ว (เหมือน LifeSteal)
        float totalCDR = this.ReductionCoolDown; // ค่านี้รวม equipment bonus แล้วจาก EquipmentManager


        float finalReduction = Mathf.Clamp(totalCDR, 0f, 75f);

        return finalReduction;
    }
   
    public virtual float GetEffectiveAttackSpeed()
    {
        // รวม attack speed bonus เป็น %
        float totalAttackSpeedBonus = 0f;

        // 🆕 ใช้ค่า AttackSpeed ปัจจุบัน (รวม upgrades แล้ว)
        if (characterStats != null)
        {
            float attackSpeedFromUpgrades = this.AttackSpeed - characterStats.attackSpeed;
            if (attackSpeedFromUpgrades > 0)
            {
                totalAttackSpeedBonus += attackSpeedFromUpgrades;
            }
        }

        // Equipment bonus (เป็น %)
        if (equipmentManager != null)
        {
            totalAttackSpeedBonus += equipmentManager.GetAttackSpeedBonus();
        }

        // Status effect bonus (เป็น %)
        if (statusEffectManager != null)
        {
            float auraMultiplier = statusEffectManager.GetTotalAttackSpeedMultiplier();
            float auraBonus = (auraMultiplier - 1f) * 100f;
            totalAttackSpeedBonus += auraBonus;
        }

        // ✅ คำนวณ cooldown reduction ด้วย Diminishing Returns
        float cooldownReduction = CalculateAttackSpeedReduction(totalAttackSpeedBonus);


        return cooldownReduction;
    }
    private float CalculateAttackSpeedReduction(float attackSpeedPercent)
    {
        // สูตร Diminishing Returns: reduction = attackSpeed / (attackSpeed + 100)
        // ตัวอย่าง:
        // 50% → 50/(50+100) = 0.333 (33.3% reduction)
        // 100% → 100/(100+100) = 0.5 (50% reduction) 
        // 300% → 300/(300+100) = 0.75 (75% reduction)
        // 900% → 900/(900+100) = 0.9 (90% reduction - ใกล้เคียง limit)

        float reduction = attackSpeedPercent / (attackSpeedPercent + 100f);

        // จำกัดไม่เกิน 95% (ป้องกัน cooldown เป็น 0)
        reduction = Mathf.Clamp(reduction, 0f, 0.99f);

        return reduction;
    }
    public float GetAttackSpeedMultiplierForUI()
    {
        // รวม attack speed bonus เป็น %
        float totalAttackSpeedBonus = 0f;

        // 🆕 เพิ่ม bonus จาก base AttackSpeed (DEX upgrades)
        if (characterStats != null)
        {
            float baseAttackSpeedFromStats = characterStats.attackSpeed;
            float currentAttackSpeed = this.AttackSpeed; // ค่าปัจจุบัน (รวม upgrades)
            float attackSpeedFromUpgrades = currentAttackSpeed - baseAttackSpeedFromStats;

            if (attackSpeedFromUpgrades > 0)
            {
                totalAttackSpeedBonus += attackSpeedFromUpgrades; // แปลงเป็น %
            }
        }

        // Equipment bonus (เป็น %)
        if (equipmentManager != null)
        {
            totalAttackSpeedBonus += equipmentManager.GetAttackSpeedBonus();
        }

        // Status effect bonus (เป็น %)
        if (statusEffectManager != null)
        {
            float auraMultiplier = statusEffectManager.GetTotalAttackSpeedMultiplier();
            float auraBonus = (auraMultiplier - 1f) * 100f;
            totalAttackSpeedBonus += auraBonus;
        }

        // ✅ แปลง % เป็น multiplier สำหรับ UI
        float multiplier = 1f + (totalAttackSpeedBonus / 100f);


        return multiplier;
    }

    // ใน Character.cs - แทนที่ method เดิม
    public virtual float GetEffectiveCriticalDamageBonus()
    {
        float baseCriticalBonus = CriticalDamageBonus / 100f;

        // Equipment bonus
        float equipmentBonus = 0f;
        if (equipmentManager != null)
        {
            equipmentBonus = equipmentManager.GetCriticalMultiplierBonus() / 100f;
        }

        float totalBonus = baseCriticalBonus + equipmentBonus;

        Debug.Log($"[GetEffectiveCriticalDamageBonus] {CharacterName}: Base={baseCriticalBonus:F2}, Equipment={equipmentBonus:F2}, Total={totalBonus:F2}");

        return totalBonus;
    }

    public virtual float GetEffectiveCriticalChance()
    {
        float baseCritical = CriticalChance;

        // ✅ รวม Critical Aura
        if (statusEffectManager != null)
        {
            float criticalBonus = statusEffectManager.GetTotalCriticalBonus() * 100f;
            baseCritical += criticalBonus;
        }

        // Cap ที่ 100%
        baseCritical = Mathf.Min(baseCritical, 100f);

        Debug.Log($"[GetEffectiveCriticalChance] {CharacterName}: Base={CriticalChance:F1}%, Effective={baseCritical:F1}%");

        return baseCritical;
    }
    // ใน Character.cs - แก้ไข GetEffectiveLifeSteal()
    public virtual float GetEffectiveLifeSteal()
    {
        // ✅ ใช้ this.LifeSteal ที่รวม equipment bonus แล้ว - ไม่ต้องบวกซ้ำ
        float totalLifeSteal = this.LifeSteal; // ค่านี้รวม equipment bonus แล้วจาก EquipmentManager


        return Mathf.Clamp(totalLifeSteal, 0f, 100f);
    }

   
    public void UpdateCriticalDamageBonus(float newValue, bool forceNetworkSync = false)
    {
        float oldValue = criticalDamageBonus;
        criticalDamageBonus = newValue;


        // Force network sync ถ้าจำเป็น (เฉพาะตอนที่มี authority)
        if (forceNetworkSync && HasStateAuthority)
        {
            ForceUpdateNetworkState();
        }

        // ✅ Trigger event เพื่อแจ้ง UI และระบบอื่นๆ
        OnStatsChanged?.Invoke();
    }
    #endregion
    public float GetEffectiveHealthRegen()
    {
        float total = baseHealthRegenPerSecond + HealthRegen;
        if (equipmentManager != null)
        {
            total += equipmentManager.GetHealthRegenBonus();
        }
        return total;
    }

    public float GetEffectiveManaRegen()
    {
        float total = baseManaRegenPerSecond + ManaRegen;
        if (equipmentManager != null)
        {
            total += equipmentManager.GetManaRegenBonus();
        }
        return total;
    }
    #region Level and Experience Methods ระบบเลเวลและประสบการณ์
    public int GetCurrentLevel()
    {
        if (levelManager == null) return 1;
        return levelManager.CurrentLevel;
    }

    public int GetCurrentExp()
    {
        if (levelManager == null) return 0;
        return levelManager.CurrentExp;
    }

    public float GetExpProgress()
    {
        if (levelManager == null) return 0f;
        return levelManager.GetExpProgress();
    }

    public void GainExp(int expAmount)
    {
        if (levelManager == null) return;
        levelManager.GainExp(expAmount);
    }

    public bool IsMaxLevel()
    {
        if (levelManager == null) return false;
        return levelManager.IsMaxLevel();
    }
    #endregion

    #region Query and Utility Methods ฟังก์ชันยูทิลิตี้และการค้นหาข้อมูล
    public float GetHealthPercentage()
    {
        if (combatManager == null) return (float)currentHp / maxHp;
        return combatManager.GetHealthPercentage();
    }

    protected virtual bool CanDie()
    {
        return NetworkedCurrentHp <= 0;
    }

    public bool IsSpawned => Object != null && Object.IsValid;
    #endregion
    public void EquipItem(EquipmentData equipment)
    {
        if (equipmentManager == null) return;
        equipmentManager.EquipItem(equipment);
    }

    public void UnequipItem()
    {
        if (equipmentManager == null) return;
        equipmentManager.UnequipItem();
    }

    public void ApplyRune(EquipmentStats runeStats)
    {
        if (equipmentManager == null) return;
        equipmentManager.ApplyRuneBonus(runeStats);
    }

    public EquipmentStats GetTotalEquipmentStats()
    {
        if (equipmentManager == null) return new EquipmentStats();
        return equipmentManager.GetTotalStats();
    }

    public void OnEquipmentStatsChanged()
    {
        // แจ้งให้ระบบอื่นๆ รู้ว่า stats เปลี่ยน (รวม Inspector)
        OnStatsChanged?.Invoke();

    }
    public bool EquipItemData(ItemData itemData)
    {
        if (itemData == null)
        {
            return false;
        }
        if (GetComponent<NetworkEnemy>() != null)
        {
            return false;
        }


        // ตรวจสอบ lists
        if (characterEquippedItems.Count < 6)
        {
            InitializeEquipmentSlots();
        }

        if (potionSlots.Count < 5)
        {
            InitializeEquipmentSlots();
        }

        bool equipSuccess = false;

        // สำหรับ potion: ใช้ logic แยกต่างหาก
        if (itemData.ItemType == ItemType.Potion)
        {
            equipSuccess = EquipPotionToSlot(itemData);
        }
        else
        {
            // สำหรับ equipment อื่นๆ
            int slotIndex = GetSlotIndexForItemType(itemData.ItemType);
            if (slotIndex == -1)
            {
                return false;
            }

            // Unequip item เก่าถ้ามี
            if (characterEquippedItems[slotIndex] != null)
            {
                ItemData oldItem = characterEquippedItems[slotIndex];
                if (inventory != null)
                {
                    inventory.AddItem(oldItem, 1);
                }
            }

            // Equip item ใหม่
            characterEquippedItems[slotIndex] = itemData;

            // คำนวณ total stats จาก equipment ทั้งหมด
            ApplyAllEquipmentStats();

            // แจ้ง Event สำหรับ UI
            OnItemEquippedToSlot?.Invoke(this, itemData.ItemType, itemData);

            equipSuccess = true;
        }

        if (equipSuccess)
        {
            // Force update equipment slots ทันที
            ForceUpdateEquipmentSlotsNow();
            SaveTotalStatsDirectly();
            UpdateSetBonusesAfterEquipmentChange();

            // 🆕 บันทึก inventory และ total stats
            SaveEquipmentImmediately();
        }

        return equipSuccess;
    }

    // 🆕 เพิ่ม method ใหม่สำหรับ equip potion
    private bool EquipPotionToSlot(ItemData potionData)
    {

        // หาช่องว่างใน potion slots (0-4)
        int emptySlotIndex = FindEmptyPotionSlot();

        if (emptySlotIndex == -1)
        {
            return false;
        }

        // 🆕 หาจำนวน potion ทั้งหมดใน inventory
        int totalStackCount = GetPotionStackCountFromInventory(potionData);

        // แสดง potion effects ถ้ามี
        if (potionData.Stats.IsPotion())
        {
            string effects = "";
            if (potionData.Stats.healAmount > 0) effects += $"+{potionData.Stats.healAmount}HP ";
            if (potionData.Stats.manaAmount > 0) effects += $"+{potionData.Stats.manaAmount}MP ";
            if (potionData.Stats.healPercentage > 0) effects += $"+{potionData.Stats.healPercentage:P0}HP ";
            if (potionData.Stats.manaPercentage > 0) effects += $"+{potionData.Stats.manaPercentage:P0}MP ";
            Debug.Log($"[Character] 💊 Potion effects: {effects.Trim()}");
        }

        // ใส่ potion ลงช่องที่ว่าง
        potionSlots[emptySlotIndex] = potionData;
        potionStackCounts[emptySlotIndex] = totalStackCount; // 🆕 เก็บจำนวน stack


        // แจ้ง Event สำหรับ UI (ใช้ ItemType.Potion)
        OnItemEquippedToSlot?.Invoke(this, ItemType.Potion, potionData);

        // Force update equipment slots ทันที
        ForceUpdateEquipmentSlotsNow();

        return true;
    }

    private int GetPotionStackCountFromInventory(ItemData potionData)
    {
        if (inventory == null) return 1;

        int totalCount = 0;
        for (int i = 0; i < inventory.CurrentSlots; i++)
        {
            InventoryItem item = inventory.GetItem(i);
            if (item != null && !item.IsEmpty && item.itemData == potionData)
            {
                totalCount += item.stackCount;
            }
        }

        return totalCount;
    }

    // 🆕 เพิ่ม method สำหรับ get potion stack count
    public int GetPotionStackCount(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < potionStackCounts.Count)
        {
            return potionStackCounts[slotIndex];
        }
        return 0;
    }

    // 🆕 เพิ่ม method สำหรับ set potion stack count
    public void SetPotionStackCount(int slotIndex, int stackCount)
    {
        if (slotIndex >= 0 && slotIndex < potionStackCounts.Count)
        {
            potionStackCounts[slotIndex] = stackCount;
        }
    }
   
    public void ApplyBaseStatsOnly()
    {
        try
        {
            var characterData = PersistentPlayerData.Instance?.GetCurrentCharacterData();
            if (characterData == null || !characterData.HasValidBaseStats())
            {
                Debug.LogError("[Character] No valid base stats to apply!");
                return;
            }

            Debug.Log($"[Character] 📊 Applying base stats only (no equipment bonuses)...");

            // เก็บ HP/Mana percentage ก่อนเปลี่ยน stats
            float hpPercentage = MaxHp > 0 ? (float)CurrentHp / MaxHp : 1f;
            float manaPercentage = MaxMana > 0 ? (float)CurrentMana / MaxMana : 1f;

            // Apply เฉพาะ base stats (ไม่รวม equipment bonuses)
            MaxHp = characterData.baseMaxHp;
            MaxMana = characterData.baseMaxMana;
            AttackDamage = characterData.baseAttackDamage;
            MagicDamage = characterData.baseMagicDamage;
            Armor = characterData.baseArmor;
            CriticalChance = characterData.baseCriticalChance;
            UpdateCriticalDamageBonus(characterData.baseCriticalDamageBonus, false);
            MoveSpeed = characterData.baseMoveSpeed;
            HitRate = characterData.baseHitRate;
            EvasionRate = characterData.baseEvasionRate;
            AttackSpeed = characterData.baseAttackSpeed;
            ReductionCoolDown = characterData.baseReductionCoolDown;
            LifeSteal = characterData.baseLifeSteal;
            LifeSteal = characterData.baseAmpDamage;
            MagicArmor = characterData.baseMagicArmor;
            HealthRegen = characterData.baseHealthRegen;

            ManaRegen = characterData.baseManaRegen;

            // ปรับ currentHp และ currentMana ตามเปอร์เซ็นต์เดิม
            CurrentHp = Mathf.RoundToInt(MaxHp * hpPercentage);
            CurrentMana = Mathf.RoundToInt(MaxMana * manaPercentage);
            CurrentHp = Mathf.Clamp(CurrentHp, 1, MaxHp);
            CurrentMana = Mathf.Clamp(CurrentMana, 0, MaxMana);

            // Force update network state
            ForceUpdateNetworkState();

            Debug.Log($"[Character] ✅ Applied base stats only:");
            Debug.Log($"  HP={MaxHp}, ATK={AttackDamage}, LifeSteal={LifeSteal:F1}%");

            // แจ้ง stats changed
            OnStatsChanged?.Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Character] ❌ Error applying base stats: {e.Message}");
        }
    }
    private void ForceUpdateEquipmentSlotsNow()
    {
        EquipmentSlotManager equipmentSlotManager = GetComponent<EquipmentSlotManager>();
        if (equipmentSlotManager != null && equipmentSlotManager.IsConnected())
        {
            equipmentSlotManager.ForceRefreshFromCharacter();
        }

        // หา CombatUIManager แล้วลอง refresh ด้วย
        CombatUIManager uiManager = FindObjectOfType<CombatUIManager>();
        if (uiManager?.equipmentSlotManager != null)
        {
            uiManager.equipmentSlotManager.ForceRefreshFromCharacter();
        }
    }

    // เพิ่ม method สำหรับ unequip
    public bool UnequipPotion(int potionSlotIndex)
    {
        if (potionSlotIndex < 0 || potionSlotIndex >= potionSlots.Count || potionSlots[potionSlotIndex] == null)
        {
            return false;
        }

        ItemData unequippedPotion = potionSlots[potionSlotIndex];
        int stackCount = potionStackCounts[potionSlotIndex]; // 🆕 ดึงจำนวน stack


        // เคลียร์ slot
        potionSlots[potionSlotIndex] = null;
        potionStackCounts[potionSlotIndex] = 0; // 🆕 รีเซ็ต stack count

        // 🆕 ไม่ต้องเพิ่มกลับ inventory ที่นี่ เพราะ ItemDetailPanel จะจัดการเอง

        return true;
    }
    // 🆕 Method ใหม่: คำนวณ total stats จาก equipment ทั้งหมด
    private void ApplyAllEquipmentStats()
    {
        if (equipmentManager == null)
        {
            Debug.LogWarning($"[Character] {CharacterName}: EquipmentManager is null");
            return;
        }

        // ✅ เก็บ HP/Mana percentage ก่อนเปลี่ยน stats
        float hpPercentage = MaxHp > 0 ? (float)CurrentHp / MaxHp : 1f;
        float manaPercentage = MaxMana > 0 ? (float)CurrentMana / MaxMana : 1f;

        Debug.Log($"[Character] Before applying all equipment: HP={CurrentHp}/{MaxHp} ({hpPercentage:P0})");

        // คำนวณ total stats จาก characterEquippedItems ทั้งหมด
        EquipmentStats totalStats = CalculateTotalEquipmentStats();

        // สร้าง EquipmentData ที่มี total stats
        EquipmentData totalEquipmentData = new EquipmentData
        {
            itemName = "Total Equipment",
            stats = totalStats,
            itemIcon = null
        };

        // ส่ง total stats ไปให้ EquipmentManager ครั้งเดียว
        equipmentManager.EquipItem(totalEquipmentData);

        // 🆕 Update set bonuses after equipment stats applied
        UpdateSetBonusesAfterEquipmentChange();

        // ✅ ปรับ currentHp และ currentMana ตามเปอร์เซ็นต์เดิม และ MaxHp/MaxMana ใหม่
        CurrentHp = Mathf.RoundToInt(MaxHp * hpPercentage);
        CurrentMana = Mathf.RoundToInt(MaxMana * manaPercentage);

        // ✅ ตรวจสอบไม่ให้เกินขีดจำกัด
        CurrentHp = Mathf.Clamp(CurrentHp, 1, MaxHp); // อย่างน้อย 1 HP
        CurrentMana = Mathf.Clamp(CurrentMana, 0, MaxMana);

        Debug.Log($"[Character] After applying all equipment: HP={CurrentHp}/{MaxHp} ({(float)CurrentHp / MaxHp:P0})");
    }

    private EquipmentStats CalculateTotalEquipmentStats()
    {
        EquipmentStats totalStats = new EquipmentStats();

        // รวม stats จาก equipment ทุกชิ้น
        foreach (ItemData equippedItem in characterEquippedItems)
        {
            if (equippedItem != null)
            {
                EquipmentStats itemStats = equippedItem.Stats.ToEquipmentStats();

                totalStats.attackDamageBonus += itemStats.attackDamageBonus;
                totalStats.magicDamageBonus += itemStats.magicDamageBonus;
                totalStats.armorBonus += itemStats.armorBonus;
                totalStats.criticalChanceBonus += itemStats.criticalChanceBonus;
                totalStats.criticalMultiplierBonus += itemStats.criticalMultiplierBonus;
                totalStats.maxHpBonus += itemStats.maxHpBonus;
                totalStats.maxManaBonus += itemStats.maxManaBonus;
                totalStats.moveSpeedBonus += itemStats.moveSpeedBonus;
                totalStats.attackSpeedBonus += itemStats.attackSpeedBonus;
                totalStats.hitRateBonus += itemStats.hitRateBonus;
                totalStats.evasionRateBonus += itemStats.evasionRateBonus;
                totalStats.lifeStealBonus += itemStats.lifeStealBonus;
                totalStats.ampDamageBonus += itemStats.ampDamageBonus;
                totalStats.reductionCoolDownBonus += itemStats.reductionCoolDownBonus;
                totalStats.physicalResistanceBonus += itemStats.physicalResistanceBonus;
                totalStats.magicalResistanceBonus += itemStats.magicalResistanceBonus;
                totalStats.magicArmorBonus += itemStats.magicArmorBonus;
                totalStats.manaRegenBonus += itemStats.manaRegenBonus;
                totalStats.healthRegenBonus += itemStats.healthRegenBonus;

            }
        }


        // 🆕 แสดง total stats ทั้งหมดที่มีค่ามากกว่า 0
        List<string> totalStatsList = new List<string>();

        if (totalStats.attackDamageBonus != 0)
            totalStatsList.Add($"ATK+{totalStats.attackDamageBonus}");
        if (totalStats.magicDamageBonus != 0)
            totalStatsList.Add($"MAG+{totalStats.magicDamageBonus}");
        if (totalStats.armorBonus != 0)
            totalStatsList.Add($"ARM+{totalStats.armorBonus}");
        if (totalStats.criticalChanceBonus != 0f)
            totalStatsList.Add($"CRIT+{totalStats.criticalChanceBonus:F1}%");
        if (totalStats.criticalMultiplierBonus != 0f)
            totalStatsList.Add($"CRIT_DMG+{totalStats.criticalMultiplierBonus:F1}%");
        if (totalStats.maxHpBonus != 0)
            totalStatsList.Add($"HP+{totalStats.maxHpBonus}");
        if (totalStats.maxManaBonus != 0)
            totalStatsList.Add($"MP+{totalStats.maxManaBonus}");
        if (totalStats.moveSpeedBonus != 0f)
            totalStatsList.Add($"SPD+{totalStats.moveSpeedBonus:F1}");
        if (totalStats.attackSpeedBonus != 0f)
            totalStatsList.Add($"AS+{totalStats.attackSpeedBonus:F1}%");
        if (totalStats.hitRateBonus != 0f)
            totalStatsList.Add($"HIT+{totalStats.hitRateBonus:F1}%");
        if (totalStats.evasionRateBonus != 0f)
            totalStatsList.Add($"EVA+{totalStats.evasionRateBonus:F1}%");
        if (totalStats.reductionCoolDownBonus != 0f)
            totalStatsList.Add($"CDR+{totalStats.reductionCoolDownBonus:F1}%");
        if (totalStats.lifeStealBonus != 0f)
            totalStatsList.Add($"LST+{totalStats.lifeStealBonus:F1}%"); 
        if (totalStats.ampDamageBonus != 0f)
            totalStatsList.Add($"AMP+{totalStats.ampDamageBonus:F1}%");
        if (totalStats.magicArmorBonus != 0)
            totalStatsList.Add($"MAG_ARM+{totalStats.magicArmorBonus}");
        if (totalStats.manaRegenBonus != 0f)
            totalStatsList.Add($"MP_REGEN+{totalStats.manaRegenBonus:F1}");
        if (totalStats.healthRegenBonus != 0f)
            totalStatsList.Add($"HP_REGEN+{totalStats.healthRegenBonus:F1}");
        if (totalStats.physicalResistanceBonus != 0f)
            totalStatsList.Add($"PHYS_RES+{totalStats.physicalResistanceBonus:F1}%");
        if (totalStats.magicalResistanceBonus != 0f)
            totalStatsList.Add($"MAG_RES+{totalStats.magicalResistanceBonus:F1}%");

        string totalStatsString = totalStatsList.Count > 0 ? string.Join(", ", totalStatsList) : "No total stats";

        return totalStats;
    }

    // เพิ่ม method สำหรับ potion

    // เพิ่ม helper methods
    private int GetSlotIndexForItemType(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Head: return 0;
            case ItemType.Armor: return 1;
            case ItemType.Weapon: return 2;
            case ItemType.Pants: return 3;
            case ItemType.Shoes: return 4;
            case ItemType.Rune: return 5;
            case ItemType.Potion:
                // 🆕 สำหรับ potion: หาช่องว่างใน potion slots (0-4)
                return FindEmptyPotionSlot();
            default: return -1;
        }
    }

    private int FindEmptyPotionSlot()
    {

        for (int i = 0; i < potionSlots.Count; i++)
        {
            if (potionSlots[i] == null)
            {
                return i;
            }
           
        }

        return -1; // เต็มหมด
    }
    private EquipmentData ConvertItemDataToEquipmentData(ItemData itemData)
    {
        if (itemData == null)
        {
            return null;
        }

        // สร้าง EquipmentData จาก ItemData
        EquipmentData equipmentData = new EquipmentData
        {
            itemName = itemData.ItemName,
            stats = itemData.Stats.ToEquipmentStats(), // ใช้ ToEquipmentStats() ที่มีอยู่แล้ว
            itemIcon = itemData.ItemIcon
        };

        return equipmentData;
    }
    // เพิ่ม getter methods
    public ItemData GetEquippedItem(ItemType itemType)
    {
        if (itemType == ItemType.Potion)
        {
            return null;
        }

        int slotIndex = GetSlotIndexForItemType(itemType);

        if (slotIndex >= 0 && slotIndex < characterEquippedItems.Count)
        {
            ItemData item = characterEquippedItems[slotIndex];
            return item;
        }

        return null;
    }

    public ItemData GetPotionInSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < potionSlots.Count)
            return potionSlots[slotIndex];

        return null;
    }

    public bool IsEquipmentSlotEmpty(ItemType itemType)
    {
        ItemData item = GetEquippedItem(itemType);
        return item == null;
    }

    public bool IsPotionSlotEmpty(int slotIndex)
    {
        ItemData potion = GetPotionInSlot(slotIndex);
        return potion == null;
    }

    public List<ItemData> GetAllEquippedItems()
    {
        List<ItemData> allEquipped = new List<ItemData>();

        // เพิ่ม equipment items
        foreach (ItemData item in characterEquippedItems)
        {
            if (item != null)
                allEquipped.Add(item);
        }

        // เพิ่ม potion items
        foreach (ItemData potion in potionSlots)
        {
            if (potion != null)
                allEquipped.Add(potion);
        }

        return allEquipped;
    }

    private void InitializeEquipmentSlots()
    {
        // Initialize equipped items list (6 slots)
        characterEquippedItems.Clear();
        for (int i = 0; i < 6; i++)
        {
            characterEquippedItems.Add(null);
        }

        // Initialize potion slots (5 slots)
        potionSlots.Clear();
        potionStackCounts.Clear();
        for (int i = 0; i < 5; i++)
        {
            potionSlots.Add(null);
            potionStackCounts.Add(0); // 🆕 เริ่มต้นด้วย 0
        }

    }

    public EquipmentSlotManager GetEquipmentSlotManager()
    {
        return equipmentSlotManager;
    }

    private ItemType GetItemTypeFromSlotIndex(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0: return ItemType.Head;
            case 1: return ItemType.Armor;
            case 2: return ItemType.Weapon;
            case 3: return ItemType.Pants;
            case 4: return ItemType.Shoes;
            case 5: return ItemType.Rune;
            default: return ItemType.Weapon;
        }
    }
    public bool UnequipItemData(ItemType itemType)
    {
        if (itemType == ItemType.Potion)
        {
            return false;
        }
        if (GetComponent<NetworkEnemy>() != null)
        {
            return false;
        }

        int slotIndex = GetSlotIndexForItemType(itemType);
        if (slotIndex == -1 || characterEquippedItems[slotIndex] == null)
        {
            return false;
        }

        ItemData unequippedItem = characterEquippedItems[slotIndex];
        characterEquippedItems[slotIndex] = null;


        // คำนวณ total stats ใหม่หลังจาก unequip
        ApplyAllEquipmentStats();

        // แจ้ง Event สำหรับ UI
        OnItemUnequippedFromSlot?.Invoke(this, itemType);

        // Force update equipment slots
        ForceUpdateEquipmentSlotsNow();
        SaveTotalStatsDirectly();
        UpdateSetBonusesAfterEquipmentChange();

        // 🆕 บันทึก equipped items ทันที
        SaveEquipmentImmediately();

        return true;
    }
    public bool UnequipPotionAndReturnToInventory(int potionSlotIndex)
    {
        if (potionSlotIndex < 0 || potionSlotIndex >= potionSlots.Count || potionSlots[potionSlotIndex] == null)
        {
            return false;
        }

        ItemData potionToUnequip = potionSlots[potionSlotIndex];
        int stackCount = GetPotionStackCount(potionSlotIndex);

        // Unequip จาก character
        bool unequipSuccess = UnequipPotion(potionSlotIndex);
        if (!unequipSuccess)
        {
            return false;
        }

        // เพิ่มกลับไป inventory
        if (inventory != null)
        {
            bool addSuccess = inventory.AddItem(potionToUnequip, stackCount);
            if (addSuccess)
            {
                PersistentPlayerData.Instance?.SaveInventoryData(this);
                return true;
            }
            else
            {
                // ถ้าใส่ inventory ไม่ได้ ให้ equip กลับ
                potionSlots[potionSlotIndex] = potionToUnequip;
                SetPotionStackCount(potionSlotIndex, stackCount);
                return false;
            }
        }
        else
        {
            // Equip กลับ
            potionSlots[potionSlotIndex] = potionToUnequip;
            SetPotionStackCount(potionSlotIndex, stackCount);
            return false;
        }
    }

    public bool UnequipAndReturnToInventory(ItemType itemType)
    {
        if (itemType == ItemType.Potion)
        {
            return false;
        }

        // หา equipped item ก่อน unequip
        ItemData equippedItem = GetEquippedItem(itemType);
        if (equippedItem == null)
        {
            return false;
        }

        // Unequip จาก character
        bool unequipSuccess = UnequipItemData(itemType);
        if (!unequipSuccess)
        {
            return false;
        }

        // เพิ่มกลับไป inventory
        if (inventory != null)
        {
            bool addSuccess = inventory.AddItem(equippedItem, 1);
            if (addSuccess)
            {
                PersistentPlayerData.Instance?.SaveInventoryData(this);
                return true;
            }
            else
            {
                // ถ้าใส่ inventory ไม่ได้ ให้ equip กลับ
                EquipItemData(equippedItem);
                return false;
            }
        }
        else
        {
            // Equip กลับ
            EquipItemData(equippedItem);
            return false;
        }
    }

    public bool UsePotion(int potionSlotIndex)
    {
        // ... existing potion use logic ...

        if (potionSlotIndex < 0 || potionSlotIndex >= potionSlots.Count || potionSlotIndex >= 5)
        {
            return false;
        }

        if (Time.time - lastPotionUseTime[potionSlotIndex] < potionCooldown)
        {
            return false;
        }

        ItemData potionData = GetPotionInSlot(potionSlotIndex);
        if (potionData == null)
        {
            return false;
        }

        int currentStackCount = GetPotionStackCount(potionSlotIndex);
        if (currentStackCount <= 0)
        {
            potionSlots[potionSlotIndex] = null;
            SetPotionStackCount(potionSlotIndex, 0);
            return false;
        }


        // ใช้ potion
        bool success = ApplyPotionEffects(potionData);
        if (success)
        {
            // ลด stack count
            int newStackCount = currentStackCount - 1;
            SetPotionStackCount(potionSlotIndex, newStackCount);

            // ถ้า stack หมดแล้ว ให้ล้าง slot
            if (newStackCount <= 0)
            {
                potionSlots[potionSlotIndex] = null;
            }

            // อัปเดต cooldown เฉพาะ slot นี้
            lastPotionUseTime[potionSlotIndex] = Time.time;


            // แจ้ง UI ให้อัปเดตทันที
            ForceUpdatePotionUI(potionSlotIndex);

            // 🆕 บันทึกข้อมูลทันที (รวม total stats)
            StartCoroutine(DelayedPotionSave());

            return true;
        }

        return false;
    }
    public void ClearEquipmentSlotsForReload()
    {
        Debug.Log("[Character] 🧹 Clearing equipment slots for reload (not affecting inventory)...");

        // เคลียร์ equipment slots
        if (characterEquippedItems != null)
        {
            for (int i = 0; i < characterEquippedItems.Count; i++)
            {
                characterEquippedItems[i] = null;
            }
        }

        // เคลียร์ potion slots
        if (potionSlots != null)
        {
            for (int i = 0; i < potionSlots.Count; i++)
            {
                potionSlots[i] = null;
            }
        }

        // เคลียร์ potion stack counts
        if (potionStackCounts != null)
        {
            for (int i = 0; i < potionStackCounts.Count; i++)
            {
                potionStackCounts[i] = 0;
            }
        }

        Debug.Log("[Character] ✅ Equipment slots cleared for reload");
    }
    private System.Collections.IEnumerator DelayedPotionSave()
    {
        yield return new WaitForSeconds(2f); // รอ 2 วินาที

        try
        {
            Debug.Log("[Character] 💾 Delayed potion save...");
            PersistentPlayerData.Instance?.SaveCharacterPotionData(this);
        }
        catch (System.Exception e)
        {
        }
    }

  
    private void SaveEquipmentImmediately()
    {
        try
        {
            PersistentPlayerData.Instance?.SaveEquippedItemsOnly(this);
        }
        catch (System.Exception e)
        {
        }
    }

   

    private void SaveTotalStatsDirectly()
    {
        try
        {
            if (PersistentPlayerData.Instance?.multiCharacterData == null)
            {
                return;
            }

            string characterType = PersistentPlayerData.Instance.GetCurrentActiveCharacter();
            var characterData = PersistentPlayerData.Instance.GetOrCreateCharacterData(characterType);

            if (characterData != null)
            {

                // บันทึก total stats (รวม equipment bonuses)
                characterData.UpdateTotalStats(
                    MaxHp, MaxMana, AttackDamage, MagicDamage, Armor,
                    CriticalChance, CriticalDamageBonus, MoveSpeed,
                    HitRate, EvasionRate, AttackSpeed, ReductionCoolDown,LifeSteal, MagicArmor,HealthRegen, ManaRegen,AmpDamage
                );

                // บันทึกลง Firebase
                PersistentPlayerData.Instance.SavePlayerDataAsync();

            }
        }
        catch (System.Exception e)
        {
        }
    }

    private void ForceUpdatePotionUI(int potionSlotIndex)
    {
        try
        {

            // 1. แจ้ง stats changed event
            OnStatsChanged?.Invoke();

            // 2. Force refresh EquipmentSlotManager - เฉพาะ potion slot ที่ใช้
            var equipmentSlotManager = GetComponent<EquipmentSlotManager>();
            if (equipmentSlotManager != null && equipmentSlotManager.IsConnected())
            {
                equipmentSlotManager.ForceRefreshAfterPotionUse(potionSlotIndex);
            }

            // 3. Force refresh CombatUIManager equipment slots - เฉพาะ potion slot ที่ใช้
            var combatUIManager = FindObjectOfType<CombatUIManager>();
            if (combatUIManager?.equipmentSlotManager != null)
            {
                combatUIManager.equipmentSlotManager.ForceRefreshAfterPotionUse(potionSlotIndex);
            }

            // 4. Force update Canvas
            Canvas.ForceUpdateCanvases();

        }
        catch (System.Exception e)
        {
        }
    }

   

    /// <summary>
    /// ใช้ผลของ potion กับตัวละคร
    /// </summary>
    private bool ApplyPotionEffects(ItemData potionData)
    {
        if (potionData?.Stats == null)
        {
            return false;
        }

        ItemStats stats = potionData.Stats;
        bool appliedAnyEffect = false;

        // รักษาพลังชีวิตแบบคงที่
        if (stats.healAmount > 0)
        {
            int oldHp = currentHp;
            currentHp = Mathf.Min(currentHp + stats.healAmount, maxHp);
            appliedAnyEffect = true;
        }

        // รักษาพลังชีวิตแบบเปอร์เซ็นต์
        if (stats.healPercentage > 0)
        {
            int healAmount = Mathf.RoundToInt(maxHp * stats.healPercentage);
            int oldHp = currentHp;
            currentHp = Mathf.Min(currentHp + healAmount, maxHp);
            appliedAnyEffect = true;
        }

        // ฟื้นฟูมานาแบบคงที่
        if (stats.manaAmount > 0)
        {
            int oldMana = currentMana;
            currentMana = Mathf.Min(currentMana + stats.manaAmount, maxMana);
            appliedAnyEffect = true;
        }

        // ฟื้นฟูมานาแบบเปอร์เซ็นต์
        if (stats.manaPercentage > 0)
        {
            int manaAmount = Mathf.RoundToInt(maxMana * stats.manaPercentage);
            int oldMana = currentMana;
            currentMana = Mathf.Min(currentMana + manaAmount, maxMana);
            appliedAnyEffect = true;
        }

        // ส่ง network update ถ้าเป็น authority
        if (HasStateAuthority && appliedAnyEffect)
        {
            NetworkedCurrentHp = currentHp;
            NetworkedCurrentMana = currentMana;
        }

        return appliedAnyEffect;
    }
    /// <summary>
    /// ตรวจสอบว่า potion สามารถใช้ได้หรือไม่
    /// </summary>
    public bool CanUsePotion(int potionSlotIndex)
    {
        // ตรวจสอบ slot index
        if (potionSlotIndex < 0 || potionSlotIndex >= potionSlots.Count || potionSlotIndex >= 5)
            return false;

        // ตรวจสอบ cooldown เฉพาะ slot นี้
        if (Time.time - lastPotionUseTime[potionSlotIndex] < potionCooldown)
            return false;

        // ตรวจสอบว่ามี potion และมี stack count > 0
        ItemData potionData = GetPotionInSlot(potionSlotIndex);
        if (potionData == null)
            return false;

        int stackCount = GetPotionStackCount(potionSlotIndex);
        return stackCount > 0;
    }

    /// <summary>
    /// ดูเวลา cooldown ที่เหลือสำหรับ slot ที่ระบุ
    /// </summary>
    public float GetPotionCooldownRemaining(int potionSlotIndex)
    {
        if (potionSlotIndex < 0 || potionSlotIndex >= 5)
            return 0f;

        float remaining = potionCooldown - (Time.time - lastPotionUseTime[potionSlotIndex]);
        return Mathf.Max(0f, remaining);
    }

    /// <summary>
    /// ดูเวลา cooldown ที่เหลือสำหรับ slot ที่ระบุ
    /// </summary>

    /// <summary>
    /// ดูเวลา cooldown ที่เหลือ
    /// </summary>
    public bool LoadEquipmentDirectly(ItemData itemData)
    {
        if (itemData == null)
        {
            return false;
        }


        // ตรวจสอบ characterEquippedItems list
        if (characterEquippedItems.Count < 6)
        {
            InitializeEquipmentSlots();
        }

        int slotIndex = GetSlotIndexForItemType(itemData.ItemType);
        if (slotIndex == -1)
        {
            return false;
        }

        // ใส่ item ลง characterEquippedItems โดยตรง
        characterEquippedItems[slotIndex] = itemData;
        UpdateSetBonusesAfterEquipmentChange();

        return true;
    }
    public bool LoadPotionDirectly(ItemData potionData, int slotIndex, int stackCount)
    {
        if (potionData == null || slotIndex < 0 || slotIndex >= 5)
        {
            return false;
        }

        // 🔧 แก้ไข: ตรวจสอบการ duplicate loading อย่างเข้มงวด
        ItemData existingPotion = GetPotionInSlot(slotIndex);
        if (existingPotion != null)
        {
            if (existingPotion.ItemId == potionData.ItemId)
            {
                int existingStackCount = GetPotionStackCount(slotIndex);
               
                return false;
            }
        }


        // ตรวจสอบ potion lists
        if (potionSlots.Count < 5 || potionStackCounts.Count < 5)
        {
            Debug.LogWarning($"[Character] Potion slots not initialized properly");
            InitializeEquipmentSlots();
        }

        // ใส่ potion ลง slot
        potionSlots[slotIndex] = potionData;
        potionStackCounts[slotIndex] = stackCount;

        return true;
    }


    public void ClearAllEquipmentForLoad()
    {

        // เคลียร์ equipment slots
        if (characterEquippedItems != null)
        {
            for (int i = 0; i < characterEquippedItems.Count; i++)
            {
                characterEquippedItems[i] = null;
            }
        }

        // เคลียร์ potion slots
        if (potionSlots != null)
        {
            for (int i = 0; i < potionSlots.Count; i++)
            {
                potionSlots[i] = null;
            }
        }

        // เคลียร์ potion stack counts
        if (potionStackCounts != null)
        {
            for (int i = 0; i < potionStackCounts.Count; i++)
            {
                potionStackCounts[i] = 0;
            }
        }
        ClearSetBonuses();

    }

   

    public void ResetToBaseStats()
    {
        try
        {
            if (characterStats == null)
            {
                return;
            }


            // เก็บ current HP/Mana percentage เพื่อรักษาไว้
            float hpPercentage = MaxHp > 0 ? (float)CurrentHp / MaxHp : 1f;
            float manaPercentage = MaxMana > 0 ? (float)CurrentMana / MaxMana : 1f;

            // Reset ทุก stats กลับเป็นค่า base จาก characterStats
            MaxHp = characterStats.maxHp;
            MaxMana = characterStats.maxMana;
            AttackDamage = characterStats.attackDamage;
            MagicDamage = characterStats.magicDamage;
            Armor = characterStats.arrmor;
            CriticalChance = characterStats.criticalChance;
            CriticalDamageBonus = characterStats.criticalDamageBonus;
            MoveSpeed = characterStats.moveSpeed;
            HitRate = characterStats.hitRate;
            EvasionRate = characterStats.evasionRate;
            AttackSpeed = characterStats.attackSpeed;
            ReductionCoolDown = characterStats.reductionCoolDown;
            LifeSteal = characterStats.lifeSteal;
            AmpDamage = characterStats.ampdamage;
            MagicArmor = characterStats.magicArmor;
            HealthRegen = characterStats.healthRegen;
            ManaRegen = characterStats.manaRegen;
            // คำนวณ HP/Mana ตาม percentage เดิม
            CurrentHp = Mathf.RoundToInt(MaxHp * hpPercentage);
            CurrentMana = Mathf.RoundToInt(MaxMana * manaPercentage);

        }
        catch (System.Exception e)
        {
        }
    }

    #region Knockback System
    [Header("💥 Knockback Settings")]
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.3f;
    [SerializeField] private AnimationCurve knockbackCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Networked] private bool IsBeingKnockedBack { get; set; }
    [Networked] private float KnockbackEndTime { get; set; }

    /// <summary>
    /// ตรวจสอบว่าตัวละครสามารถถูก knockback ได้หรือไม่
    /// </summary>
    private bool CanBeKnockedBack()
    {
        // ✅ เช็ค Dummy
        NetworkEnemy enemy = GetComponent<NetworkEnemy>();
        if (enemy != null && (enemy.IsDummy || enemy.isLobbyDummy))
        {
            return false;
        }

        // ✅ เช็ค Status Effects
        if (HasStatusEffect(StatusEffectType.Stun) || HasStatusEffect(StatusEffectType.Freeze))
        {
            return false;
        }

        // ✅ เช็คว่ากำลังโดน knockback อยู่แล้วหรือไม่ (optional - ป้องกัน knockback ซ้อน)
        if (IsBeingKnockedBack)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Apply knockback ให้กับตัวละคร
    /// </summary>
    public void ApplyKnockback(Vector3 direction, float force = -1f, float duration = -1f)
    {
        if (!HasStateAuthority && !HasInputAuthority) return;

        // 🆕 ตรวจสอบว่าสามารถ knockback ได้หรือไม่
        if (!CanBeKnockedBack())
        {
            Debug.Log($"[Knockback] {CharacterName} cannot be knocked back!");
            return;
        }

        // ใช้ค่า default ถ้าไม่ได้ระบุ
        float actualForce = force > 0 ? force : knockbackForce;
        float actualDuration = duration > 0 ? duration : knockbackDuration;

        Debug.Log($"[Knockback] {CharacterName} knocked back! Force: {actualForce}, Duration: {actualDuration}");

        if (HasInputAuthority)
        {
            RPC_RequestKnockback(direction, actualForce, actualDuration);
        }
        else if (HasStateAuthority)
        {
            RPC_ApplyKnockbackToAll(direction, actualForce, actualDuration);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestKnockback(Vector3 direction, float force, float duration)
    {
        RPC_ApplyKnockbackToAll(direction, force, duration);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ApplyKnockbackToAll(Vector3 direction, float force, float duration)
    {
        StartCoroutine(ExecuteKnockback(direction, force, duration));
    }

    private IEnumerator ExecuteKnockback(Vector3 direction, float force, float duration)
    {
        IsBeingKnockedBack = true;
        KnockbackEndTime = Time.time + duration;

        Vector3 knockbackDirection = direction.normalized;
        knockbackDirection.y = 0; // ไม่ให้กระเด้งขึ้นบน

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // ใช้ curve เพื่อให้ knockback ดู smooth
            float curveValue = knockbackCurve.Evaluate(progress);
            Vector3 knockbackVelocity = knockbackDirection * force * curveValue;

            // ✅ ตรวจสอบกำแพงก่อน apply knockback
            RaycastHit wallHit;
            if (Physics.Raycast(transform.position, knockbackVelocity.normalized, out wallHit,
                knockbackVelocity.magnitude * Time.deltaTime, LayerMask.GetMask("Wall", "Obstacle", "Default")))
            {
                Debug.Log($"[Knockback] {CharacterName} hit wall, stopping knockback");
                break;
            }

            // Apply knockback
            if (rb != null)
            {
                Vector3 newPosition = transform.position + knockbackVelocity * Time.deltaTime;
                rb.MovePosition(newPosition);

                // Sync position (optional - ถ้าใช้วิธีที่ 2)
                SyncKnockbackPosition(newPosition);
            }

            yield return null;
        }

        IsBeingKnockedBack = false;
        Debug.Log($"[Knockback] {CharacterName} knockback ended");
    }

    /// <summary>
    /// ตรวจสอบว่ากำลังโดน knockback อยู่หรือไม่
    /// </summary>
    public bool IsKnockedBack()
    {
        return IsBeingKnockedBack;
    }

    /// <summary>
    /// หยุด knockback ทันที
    /// </summary>
    public void StopKnockback()
    {
        IsBeingKnockedBack = false;
        if (rb != null)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    /// <summary>
    /// Override method นี้ใน Hero และ NetworkEnemy เพื่อ sync position
    /// </summary>
    protected virtual void SyncKnockbackPosition(Vector3 newPosition)
    {
        // Base implementation - ไม่ทำอะไร
        // Hero และ NetworkEnemy จะ override method นี้ถ้าต้องการ
    }
    #endregion
    /// <summary>
    /// Force refresh equipment UI หลัง load
    /// </summary>
    public void ForceRefreshEquipmentAfterLoad()
    {

        // Force update equipment slots ทันที
        ForceUpdateEquipmentSlotsNow();

        // แจ้ง stats changed
        OnStatsChanged?.Invoke();

    }

    private void UpdateSetBonusesAfterEquipmentChange()
    {
        if (setBonusManager == null)
        {
            Debug.LogWarning($"[Character] {CharacterName}: SetBonusManager is null");
            return;
        }

        // รอ 1 frame เพื่อให้ equipment changes apply ก่อน
        StartCoroutine(DelayedSetBonusUpdate());
    }

    private System.Collections.IEnumerator DelayedSetBonusUpdate()
    {
        yield return null; // รอ 1 frame

        try
        {
            setBonusManager.UpdateSetBonuses();
            Debug.Log($"[Character] {CharacterName}: Set bonuses updated");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Character] {CharacterName}: Error updating set bonuses: {e.Message}");
        }
    }

    // 🆕 Method สำหรับดู set bonus info
    public string GetSetBonusInfo()
    {
        if (setBonusManager == null)
        {
            return "Set Bonus Manager not available";
        }

        return setBonusManager.GetSetBonusInfoText();
    }

    // 🆕 Method สำหรับดู active set bonuses
    public List<ActiveSetBonus> GetActiveSetBonuses()
    {
        if (setBonusManager == null)
        {
            return new List<ActiveSetBonus>();
        }

        return setBonusManager.GetCurrentSetBonuses();
    }

    // 🆕 Method สำหรับ force update set bonuses
    public void ForceUpdateSetBonuses()
    {
        if (setBonusManager != null)
        {
            setBonusManager.ForceUpdateSetBonuses();
        }
    }

    // 🆕 Method สำหรับ clear set bonuses
    public void ClearSetBonuses()
    {
        if (setBonusManager != null)
        {
            setBonusManager.ClearAllSetBonuses();
        }
    }

    // 🆕 หา ItemData จาก ID
    private ItemData FindItemDataById(string itemId)
    {
        try
        {
            var database = ItemDatabase.Instance;
            if (database?.GetAllItems() != null)
            {
                foreach (var item in database.GetAllItems())
                {
                    if (item?.ItemId == itemId)
                    {
                        return item;
                    }
                }
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    // 🆕 Force refresh UI สำหรับ auto-fix
  
}
