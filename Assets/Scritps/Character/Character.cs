using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public enum StatusEffectType
{
    None,
    Poison,
    Burn,
    Freeze,
    Stun,
    Bleed
}

public enum DamageType
{
    Normal,
    Critical,
    Magic,
    Poison,
    Burn,
    Freeze,
    Stun,
    Bleed
}

public class Character : NetworkBehaviour
{
    [Header("Base Stats")]
    public CharacterStats characterStats;

    [SerializeField]
    private string characterName;
    public string CharacterName { get { return characterName; } }

    [SerializeField]
    private int currentHp;
    public int CurrentHp { get { return currentHp; } set { currentHp = value; } }

    [SerializeField]
    private int maxHp;
    public int MaxHp { get { return maxHp; } set { maxHp = value; } }

    [SerializeField]
    private int currentMana;
    public int CurrentMana { get { return currentMana; } set { currentMana = value; } }

    [SerializeField]
    private int maxMana;
    public int MaxMana { get { return maxMana; } set { maxMana = value; } }

    [SerializeField]
    private int attackDamage;
    public int AttackDamage { get { return attackDamage; } set { attackDamage = value; } }

    [SerializeField]
    private int armor;
    public int Armor { get { return armor; } set { armor = value; } }

    [SerializeField]
    private float moveSpeed;
    public float MoveSpeed { get { return moveSpeed; } set { moveSpeed = value; } }

    [SerializeField]
    private float attackRange;
    public float AttackRange { get { return attackRange; } set { attackRange = value; } }

    [SerializeField]
    private float attackCooldown;
    public float AttackCooldown { get { return attackCooldown; } set { attackCooldown = value; } }

    [Header("Critical Stats")]
    [SerializeField]
    private float criticalChance = 5f;
    public float CriticalChance { get { return criticalChance; } set { criticalChance = value; } }

    [SerializeField]
    private float criticalMultiplier = 2f;
    public float CriticalMultiplier { get { return criticalMultiplier; } set { criticalMultiplier = value; } }
    // ========== Network Properties ==========
    [Networked] public int NetworkedCurrentHp { get; set; }
    [Networked] public int NetworkedMaxHp { get; set; }
    [Networked] public int NetworkedCurrentMana { get; set; }
    [Networked] public int NetworkedMaxMana { get; set; }
    [Networked] public bool IsNetworkStateReady { get; set; }

    // ========== Network Status Effects ==========
    [Networked] public bool IsPoisoned { get; set; }
    [Networked] public float PoisonDuration { get; set; }
    [Networked] public int PoisonDamagePerTick { get; set; }
    [Networked] public TickTimer PoisonTickTimer { get; set; }

    [Networked] public bool IsBurning { get; set; }
    [Networked] public float BurnDuration { get; set; }
    [Networked] public int BurnDamagePerTick { get; set; }
    [Networked] public float BurnNextTickTime { get; set; }

    // Freeze Status
    [Networked] public bool IsFrozen { get; set; }
    [Networked] public float FreezeDuration { get; set; }
    [Networked] public float OriginalMoveSpeed { get; set; }

    // Stun Status
    [Networked] public bool IsStunned { get; set; }
    [Networked] public float StunDuration { get; set; }

    // Bleed Status
    [Networked] public bool IsBleeding { get; set; }
    [Networked] public float BleedDuration { get; set; }
    [Networked] public int BleedDamagePerTick { get; set; }
    [Networked] public float BleedNextTickTime { get; set; }
    // 🆕 เพิ่ม manual timer สำหรับ poison
    [Networked] public float PoisonNextTickTime { get; set; }

    [Header("Physics")]
    public Rigidbody rb;

    [Header("Visual")]
    public Renderer characterRenderer;
    public Color originalColor;

    [Header("Status Effects ")]
    public Color poisonColor = new Color(0.5f, 1f, 0.5f); // สีเขียวอ่อน
    public GameObject poisonVFX;
    private float poisonTickInterval = 1f; // ทุก 1 วินาที
    public Color burnColor = new Color(1f, 0.3f, 0f);     // สีส้มแดง
    public Color freezeColor = new Color(0.3f, 0.8f, 1f); // สีน้ำเงินอ่อน
    public Color stunColor = new Color(1f, 1f, 0f);       // สีเหลือง
    public Color bleedColor = new Color(0.7f, 0f, 0f);    // สีแดงเข้ม
    public GameObject burnVFX;
    public GameObject freezeVFX;
    public GameObject stunVFX;
    public GameObject bleedVFX;
    private float burnTickInterval = 0.5f;   // ทุก 0.5 วินาที
    private float bleedTickInterval = 0.7f;  // ทุก 0.7 วินาที

    // 🔧 เพิ่มตัวแปรสำหรับจัดการสี
    private bool isTakingDamage = false;
    private bool isFlashingFromPoison = false; // 🆕 ป้องกัน UpdateVisualEffects override
    private bool isFlashingFromBurn = false;
    private bool isFlashingFromFreeze = false;
    private bool isFlashingFromStun = false;
    private bool isFlashingFromBleed = false;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        characterRenderer = GetComponent<Renderer>();
        if (characterRenderer != null)
        {
            originalColor = characterRenderer.material.color;
        }
    }

    protected virtual void Start()
    {
        characterName = characterStats.characterName;
        maxHp = characterStats.maxHp;
        currentHp = maxHp;
        maxMana = characterStats.maxMana;
        currentMana = maxMana;
        attackDamage = characterStats.attackDamage;
        armor = characterStats.arrmor;
        moveSpeed = characterStats.moveSpeed;
        attackRange = characterStats.attackRange;
        attackCooldown = characterStats.attackCoolDown;
        criticalChance = characterStats.criticalChance;
        criticalMultiplier = characterStats.criticalMultiplier;
        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationY;
            rb.useGravity = true;
            rb.drag = 1.0f;
            rb.mass = 10f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        // Initialize VFX
        if (poisonVFX != null)
            poisonVFX.SetActive(false);
        if (poisonVFX != null) poisonVFX.SetActive(false);
        if (burnVFX != null) burnVFX.SetActive(false);
        if (freezeVFX != null) freezeVFX.SetActive(false);
        if (stunVFX != null) stunVFX.SetActive(false);
        if (bleedVFX != null) bleedVFX.SetActive(false);
    }

    protected virtual void Update()
    {
        HandleStatusEffects();
    }

    // ========== Fusion Network Methods ==========
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
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority)
        {
            // Sync HP/Mana ทุก frame
            NetworkedCurrentHp = currentHp;
            NetworkedCurrentMana = currentMana;
            NetworkedMaxHp = maxHp;
            NetworkedMaxMana = maxMana;

            // 🔧 ใช้ manual timer แทน TickTimer
            if (IsPoisoned)
            {
                float currentTime = (float)Runner.SimulationTime;
                //Debug.Log($"[POISON DEBUG] {CharacterName} - Duration: {PoisonDuration:F2}, Current Time: {currentTime:F2}, Next Tick Time: {PoisonNextTickTime:F2}");

                if (currentTime >= PoisonNextTickTime)
                {
                  //  Debug.Log($"[Poison Tick] {CharacterName} applying poison damage!");
                    ApplyPoisonDamage();
                    // ตั้งเวลาสำหรับ tick ถัดไป
                    PoisonNextTickTime = currentTime + poisonTickInterval;
                   // Debug.Log($"[Poison Tick] {CharacterName} next poison tick at: {PoisonNextTickTime:F2}");
                }
            }

            // Check if poison duration expired
            if (IsPoisoned && PoisonDuration > 0)
            {
                PoisonDuration -= Runner.DeltaTime;
                if (PoisonDuration <= 0)
                {
                   // Debug.Log($"[Poison] {CharacterName} poison duration expired, removing poison");
                    RemovePoison();
                }
            }
            if (IsBurning)
            {
                float currentTime = (float)Runner.SimulationTime;

                if (currentTime >= BurnNextTickTime)
                {
                    ApplyBurnDamage();
                    BurnNextTickTime = currentTime + burnTickInterval;
                }

                BurnDuration -= Runner.DeltaTime;
                if (BurnDuration <= 0)
                {
                    RemoveBurn();
                }
            }

            // ❄️ Handle Freeze Status
            if (IsFrozen)
            {
                FreezeDuration -= Runner.DeltaTime;
                if (FreezeDuration <= 0)
                {
                    RemoveFreeze();
                }
            }

            // ⚡ Handle Stun Status
            if (IsStunned)
            {
                StunDuration -= Runner.DeltaTime;
                if (StunDuration <= 0)
                {
                    RemoveStun();
                }
            }

            // 🩸 Handle Bleed Status
            if (IsBleeding)
            {
                float currentTime = (float)Runner.SimulationTime;

                if (currentTime >= BleedNextTickTime)
                {
                    ApplyBleedDamage();
                    BleedNextTickTime = currentTime + bleedTickInterval;
                }

                BleedDuration -= Runner.DeltaTime;
                if (BleedDuration <= 0)
                {
                    RemoveBleed();
                }
            }
        }
    }

    #region  // ========== Network Damage System ==========
    public virtual void TakeDamageFromAttacker(int damage, Character attacker, DamageType damageType = DamageType.Normal)
    {
        if (!HasStateAuthority && !HasInputAuthority) return;

        // 🎯 คำนวณ critical จาก attacker's stats
        bool isCritical = false;
        if (attacker != null)
        {
            float critRoll = Random.Range(0f, 100f);
            isCritical = critRoll < attacker.CriticalChance;

            Debug.Log($"[Critical Check] {attacker.CharacterName} rolls {critRoll:F1}% vs {attacker.CriticalChance}% = {(isCritical ? "CRITICAL!" : "Normal")}");
        }

        // เรียก TakeDamage เดิมที่มีการคำนวณ damage แล้ว
        TakeDamage(damage, damageType, isCritical);
    }
    public virtual void TakeDamage(int damage, DamageType damageType = DamageType.Normal, bool isCritical = false)
    {
        if (!HasStateAuthority && !HasInputAuthority) return;

        int finalDamage = CalculateDamage(damage, isCritical);

        int oldHp = currentHp;
        currentHp -= finalDamage;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);

        Debug.Log($"[TakeDamage] {CharacterName}: {oldHp} -> {currentHp} (damage: {finalDamage}, type: {damageType}, critical: {isCritical})");

        // Sync based on authority
        if (HasStateAuthority)
        {
            NetworkedCurrentHp = currentHp;
            RPC_BroadcastHealthUpdate(currentHp);

            // 🔧 เฉพาะ StateAuthority เท่านั้นที่เรียก damage flash
            RPC_TriggerDamageFlash(damageType, isCritical);

            // 🔧 เฉพาะ StateAuthority เท่านั้นที่เรียก death
            if (currentHp <= 0)
            {
                RPC_OnDeath();
            }
        }
        else if (HasInputAuthority)
        {
            RPC_UpdateHealth(currentHp);

            // 🔧 InputAuthority ส่งไปให้ StateAuthority handle
            RPC_RequestDamageFlash(damageType, isCritical);

            // 🔧 InputAuthority ส่งไปให้ StateAuthority handle death
            if (currentHp <= 0)
            {
                RPC_RequestDeath();
            }
        }
    }
    protected virtual int CalculateDamage(int damage, bool isCritical)
    {
        if (isCritical)
        {
            int criticalDamage = Mathf.RoundToInt(damage * criticalMultiplier); // Ignore armor + multiply
            Debug.Log($"[CalculateDamage] Critical Hit! {damage} * {criticalMultiplier} = {criticalDamage} (ignoring {armor} armor)");
            return criticalDamage;
        }

        int damageAfterArmor = damage - armor;
        int finalDamage = Mathf.Max(1, damageAfterArmor);
        Debug.Log($"[CalculateDamage] Normal Hit: {damage} - {armor} armor = {finalDamage}");
        return finalDamage;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_UpdateHealth(int newHp)
    {
        currentHp = newHp;
        NetworkedCurrentHp = newHp;
        Debug.Log($"[RPC_UpdateHealth] Health updated: {newHp} for {CharacterName}");

        RPC_BroadcastHealthUpdate(newHp);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastHealthUpdate(int newHp)
    {
        NetworkedCurrentHp = newHp;
        Debug.Log($"[RPC_BroadcastHealthUpdate] Health broadcasted: {newHp} for {CharacterName}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]  // 🔧 เปลี่ยนเป็น StateAuthority
    private void RPC_TriggerDamageFlash(DamageType damageType, bool isCritical)
    {
        StartCoroutine(NetworkDamageFlash(damageType, isCritical));
    }

    // 🆕 เพิ่ม RPC สำหรับ InputAuthority ขอให้ StateAuthority ทำ damage flash
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestDamageFlash(DamageType damageType, bool isCritical)
    {
        // StateAuthority รับคำขอแล้วส่ง flash ให้ทุกคน
        RPC_TriggerDamageFlash(damageType, isCritical);
    }

    // 🆕 เพิ่ม RPC สำหรับ InputAuthority ขอให้ StateAuthority handle death
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestDeath()
    {
        // StateAuthority รับคำขอแล้วส่ง death ให้ทุกคน
        RPC_OnDeath();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]  // 🔧 เปลี่ยนเป็น StateAuthority
    private void RPC_OnDeath()
    {
        Debug.Log($"{CharacterName} died!");
        // Handle death logic here
    }
    #endregion
    // ========== Network Damage Flash - Everyone Can See ==========
    protected virtual IEnumerator NetworkDamageFlash(DamageType damageType, bool isCritical)
    {
        if (characterRenderer == null) yield break;

        isTakingDamage = true;
        Color flashColor = GetFlashColorByDamageType(damageType, isCritical);

        // Store current color
        Color currentColor = characterRenderer.material.color;

        // Apply flash color
        characterRenderer.material.color = flashColor;

        // Flash duration
        float flashDuration = isCritical ? 0.3f : 0.2f;
        yield return new WaitForSeconds(flashDuration);

        // Return to appropriate color
        Color returnColor = GetReturnColor();
        characterRenderer.material.color = returnColor;

        isTakingDamage = false;
    }

    private Color GetFlashColorByDamageType(DamageType damageType, bool isCritical)
    {
        switch (damageType)
        {
            case DamageType.Poison:
                return new Color(0.8f, 0f, 0.8f); // สีม่วง
            case DamageType.Burn:
                return new Color(1f, 0.3f, 0f); // สีส้มแดง
            case DamageType.Freeze:
                return new Color(0.3f, 0.8f, 1f); // สีน้ำเงินอ่อน
            case DamageType.Stun:
                return new Color(1f, 1f, 0f); // สีเหลือง
            case DamageType.Bleed:
                return new Color(0.7f, 0f, 0f); // สีแดงเข้ม
            case DamageType.Critical:
                return new Color(1f, 0.8f, 0f); // สีทอง
            default:
                return isCritical ? new Color(1f, 0.5f, 0f) : Color.red;
        }
    }

    private Color GetReturnColor()
    {
        // ลำดับความสำคัญของสี (สถานะที่สำคัญกว่าจะแสดงก่อน)
        if (IsPoisoned) return poisonColor;
        if (IsBurning) return burnColor;
        if (IsBleeding) return bleedColor;
        if (IsFrozen) return freezeColor;
        if (IsStunned) return stunColor;

        return originalColor;
    }

    // ========== Poison Status Effect System ==========
    #region Status Effect
    public virtual void ApplyPoison(int damagePerTick, float duration)
    {
        if (!HasStateAuthority) return;

        // 🔧 ปรับปรุงการตั้งค่าพิษ
        bool wasAlreadyPoisoned = IsPoisoned;

        IsPoisoned = true;
        PoisonDamagePerTick = damagePerTick;
        PoisonDuration = duration;

        // 🔧 ตั้งค่า manual timer ให้ tick ทันทีในครั้งแรก
        float currentTime = (float)Runner.SimulationTime;
        if (!wasAlreadyPoisoned)
        {
            PoisonNextTickTime = currentTime + 0.1f; // ให้ tick เร็วในครั้งแรก
            Debug.Log($"[ApplyPoison] {CharacterName} first poison tick scheduled at: {PoisonNextTickTime:F2} (current: {currentTime:F2})");
        }

        Debug.Log($"[ApplyPoison] {CharacterName} is poisoned! {damagePerTick} damage per {poisonTickInterval}s for {duration}s");

        // Visual effects for all clients
        RPC_ShowPoisonEffect(true);
    }

    private void ApplyPoisonDamage()
    {
        if (!IsPoisoned) return;

        Debug.Log($"[ApplyPoisonDamage] {CharacterName} about to take {PoisonDamagePerTick} poison damage");

        // ลดเลือดโดยตรงเหมือนโค้ดเก่า แล้ว sync network
        int oldHp = currentHp;
        currentHp -= PoisonDamagePerTick;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);

        // Update network properties
        NetworkedCurrentHp = currentHp;

        Debug.Log($"[Poison] {CharacterName} takes {PoisonDamagePerTick} poison damage. HP: {currentHp}/{maxHp} (was {oldHp})");

        // 🔧 ใช้ RPC สำหรับ poison damage flash แทน coroutine local
        RPC_TriggerPoisonFlash();

        // Broadcast health change to all clients
        RPC_BroadcastHealthUpdate(currentHp);

        if (currentHp <= 0)
        {
            RPC_OnDeath();
        }
    }

    // 🆕 เพิ่ม RPC สำหรับ poison flash
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TriggerPoisonFlash()
    {
        StartCoroutine(PoisonDamageFlash());
    }

    // 🔧 ปรับปรุง PoisonDamageFlash ให้ใช้ flag
    protected virtual IEnumerator PoisonDamageFlash()
    {
        if (characterRenderer == null) yield break;

        // 🔧 ตั้งค่า flag เพื่อป้องกัน UpdateVisualEffects() override
        isFlashingFromPoison = true;

        // บันทึกสีปัจจุบัน
        Color currentColor = characterRenderer.material.color;

        // เปลี่ยนเป็นสีแดงชั่วคราว
        characterRenderer.material.color = Color.red;

        // รอเวลาสั้นๆ
        yield return new WaitForSeconds(0.2f);

        // เปลี่ยนกลับเป็นสีเดิม (สีพิษ หรือสีปกติ)
        if (IsPoisoned)
        {
            characterRenderer.material.color = poisonColor;
        }
        else
        {
            characterRenderer.material.color = originalColor;
        }

        // 🔧 ปิด flag
        isFlashingFromPoison = false;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowPoisonEffect(bool show)
    {
        if (poisonVFX != null)
        {
            poisonVFX.SetActive(show);
        }

        // 🔧 เปลี่ยนสีทันทีเหมือนโค้ดเก่า แต่เช็ค isFlashingFromPoison ก่อน
        if (characterRenderer != null && !isFlashingFromPoison)
        {
            if (show)
            {
                characterRenderer.material.color = poisonColor;
            }
            else
            {
                characterRenderer.material.color = originalColor;
            }
        }
    }
    public virtual void RemovePoison()
    {
        if (!HasStateAuthority) return;

        bool wasPoisoned = IsPoisoned;
        IsPoisoned = false;
        PoisonDuration = 0f;
        PoisonDamagePerTick = 0;
        PoisonNextTickTime = 0f; // 🔧 รีเซ็ต manual timer

        Debug.Log($"{CharacterName} is no longer poisoned (was poisoned: {wasPoisoned})");

        // Remove visual effects for all clients
        RPC_ShowPoisonEffect(false);
    }
    public virtual void ApplyBurn(int damagePerTick, float duration)
    {
        if (!HasStateAuthority) return;

        bool wasAlreadyBurning = IsBurning;

        IsBurning = true;
        BurnDamagePerTick = damagePerTick;
        BurnDuration = duration;

        float currentTime = (float)Runner.SimulationTime;
        if (!wasAlreadyBurning)
        {
            BurnNextTickTime = currentTime + 0.1f;
        }

        Debug.Log($"[ApplyBurn] {CharacterName} is burning! {damagePerTick} damage per {burnTickInterval}s for {duration}s");
        RPC_ShowBurnEffect(true);
    }

    private void ApplyBurnDamage()
    {
        if (!IsBurning) return;

        int oldHp = currentHp;
        currentHp -= BurnDamagePerTick;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);

        NetworkedCurrentHp = currentHp;
        Debug.Log($"[Burn] {CharacterName} takes {BurnDamagePerTick} burn damage. HP: {currentHp}/{maxHp}");

        RPC_TriggerBurnFlash();
        RPC_BroadcastHealthUpdate(currentHp);

        if (currentHp <= 0)
        {
            RPC_OnDeath();
        }
    }

    public virtual void RemoveBurn()
    {
        if (!HasStateAuthority) return;

        IsBurning = false;
        BurnDuration = 0f;
        BurnDamagePerTick = 0;
        BurnNextTickTime = 0f;

        Debug.Log($"{CharacterName} is no longer burning");
        RPC_ShowBurnEffect(false);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowBurnEffect(bool show)
    {
        if (burnVFX != null)
        {
            burnVFX.SetActive(show);
        }

        if (characterRenderer != null && !isFlashingFromBurn && !isTakingDamage)
        {
            if (show)
            {
                characterRenderer.material.color = burnColor;
            }
            else
            {
                characterRenderer.material.color = GetReturnColor();
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TriggerBurnFlash()
    {
        StartCoroutine(BurnDamageFlash());
    }

    protected virtual IEnumerator BurnDamageFlash()
    {
        if (characterRenderer == null) yield break;

        isFlashingFromBurn = true;
        characterRenderer.material.color = Color.red;
        yield return new WaitForSeconds(0.2f);

        if (IsBurning)
        {
            characterRenderer.material.color = burnColor;
        }
        else
        {
            characterRenderer.material.color = GetReturnColor();
        }

        isFlashingFromBurn = false;
    }

    // ========== ❄️ Freeze Status Effect System ==========
    public virtual void ApplyFreeze(float duration)
    {
        if (!HasStateAuthority) return;

        bool wasAlreadyFrozen = IsFrozen;

        IsFrozen = true;
        FreezeDuration = duration;

        if (!wasAlreadyFrozen)
        {
            OriginalMoveSpeed = moveSpeed;
            moveSpeed *= 0.3f; // ลดความเร็วเหลือ 30%

            // หยุด movement ของ rigidbody
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
            }
        }

        Debug.Log($"[ApplyFreeze] {CharacterName} is frozen for {duration}s! Move speed: {moveSpeed}");
        RPC_ShowFreezeEffect(true);
    }

    public virtual void RemoveFreeze()
    {
        if (!HasStateAuthority) return;

        IsFrozen = false;
        FreezeDuration = 0f;

        // คืนค่าความเร็ว
        moveSpeed = OriginalMoveSpeed;

        Debug.Log($"{CharacterName} is no longer frozen. Move speed restored: {moveSpeed}");
        RPC_ShowFreezeEffect(false);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowFreezeEffect(bool show)
    {
        if (freezeVFX != null)
        {
            freezeVFX.SetActive(show);
        }

        if (characterRenderer != null && !isFlashingFromFreeze && !isTakingDamage)
        {
            if (show)
            {
                characterRenderer.material.color = freezeColor;
            }
            else
            {
                characterRenderer.material.color = GetReturnColor();
            }
        }
    }

    // ========== ⚡ Stun Status Effect System ==========
    public virtual void ApplyStun(float duration)
    {
        if (!HasStateAuthority) return;

        IsStunned = true;
        StunDuration = duration;

        // หยุดการเคลื่อนไหวทั้งหมด
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
        }

        Debug.Log($"[ApplyStun] {CharacterName} is stunned for {duration}s!");
        RPC_ShowStunEffect(true);
    }

    public virtual void RemoveStun()
    {
        if (!HasStateAuthority) return;

        IsStunned = false;
        StunDuration = 0f;

        Debug.Log($"{CharacterName} is no longer stunned");
        RPC_ShowStunEffect(false);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowStunEffect(bool show)
    {
        if (stunVFX != null)
        {
            stunVFX.SetActive(show);
        }

        if (characterRenderer != null && !isFlashingFromStun && !isTakingDamage)
        {
            if (show)
            {
                characterRenderer.material.color = stunColor;
            }
            else
            {
                characterRenderer.material.color = GetReturnColor();
            }
        }
    }

    // ========== 🩸 Bleed Status Effect System ==========
    public virtual void ApplyBleed(int damagePerTick, float duration)
    {
        if (!HasStateAuthority) return;

        bool wasAlreadyBleeding = IsBleeding;

        IsBleeding = true;
        BleedDamagePerTick = damagePerTick;
        BleedDuration = duration;

        float currentTime = (float)Runner.SimulationTime;
        if (!wasAlreadyBleeding)
        {
            BleedNextTickTime = currentTime + 0.1f;
        }

        Debug.Log($"[ApplyBleed] {CharacterName} is bleeding! {damagePerTick} damage per {bleedTickInterval}s for {duration}s");
        RPC_ShowBleedEffect(true);
    }

    private void ApplyBleedDamage()
    {
        if (!IsBleeding) return;

        // 🩸 Bleed ทำดาเมจมากขึ้นเมื่อเลือดน้อย
        float healthPercentage = (float)currentHp / maxHp;
        float bleedMultiplier = Mathf.Lerp(2.0f, 1.0f, healthPercentage); // 1x-2x damage
        int finalDamage = Mathf.RoundToInt(BleedDamagePerTick * bleedMultiplier);

        int oldHp = currentHp;
        currentHp -= finalDamage;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);

        NetworkedCurrentHp = currentHp;
        Debug.Log($"[Bleed] {CharacterName} takes {finalDamage} bleed damage (base: {BleedDamagePerTick} x {bleedMultiplier:F1}). HP: {currentHp}/{maxHp}");

        RPC_TriggerBleedFlash();
        RPC_BroadcastHealthUpdate(currentHp);

        if (currentHp <= 0)
        {
            RPC_OnDeath();
        }
    }

    public virtual void RemoveBleed()
    {
        if (!HasStateAuthority) return;

        IsBleeding = false;
        BleedDuration = 0f;
        BleedDamagePerTick = 0;
        BleedNextTickTime = 0f;

        Debug.Log($"{CharacterName} is no longer bleeding");
        RPC_ShowBleedEffect(false);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowBleedEffect(bool show)
    {
        if (bleedVFX != null)
        {
            bleedVFX.SetActive(show);
        }

        if (characterRenderer != null && !isFlashingFromBleed && !isTakingDamage)
        {
            if (show)
            {
                characterRenderer.material.color = bleedColor;
            }
            else
            {
                characterRenderer.material.color = GetReturnColor();
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TriggerBleedFlash()
    {
        StartCoroutine(BleedDamageFlash());
    }

    protected virtual IEnumerator BleedDamageFlash()
    {
        if (characterRenderer == null) yield break;

        isFlashingFromBleed = true;
        characterRenderer.material.color = Color.red;
        yield return new WaitForSeconds(0.2f);

        if (IsBleeding)
        {
            characterRenderer.material.color = bleedColor;
        }
        else
        {
            characterRenderer.material.color = GetReturnColor();
        }

        isFlashingFromBleed = false;
    }
    // ========== Status Effects Handler ==========
    protected virtual void HandleStatusEffects()
    {
        // Client-side visual updates based on network state
        if (!HasStateAuthority)
        {
            UpdateVisualEffects();
        }
    }

    // 🔧 ปรับปรุง UpdateVisualEffects ให้ไม่ override การ flash
    private void UpdateVisualEffects()
    {
        // Update poison visual effect based on network state
        if (poisonVFX != null && poisonVFX.activeSelf != IsPoisoned)
        {
            poisonVFX.SetActive(IsPoisoned);
        }
        if (poisonVFX != null && poisonVFX.activeSelf != IsPoisoned)
            poisonVFX.SetActive(IsPoisoned);

        if (burnVFX != null && burnVFX.activeSelf != IsBurning)
            burnVFX.SetActive(IsBurning);

        if (freezeVFX != null && freezeVFX.activeSelf != IsFrozen)
            freezeVFX.SetActive(IsFrozen);

        if (stunVFX != null && stunVFX.activeSelf != IsStunned)
            stunVFX.SetActive(IsStunned);

        if (bleedVFX != null && bleedVFX.activeSelf != IsBleeding)
            bleedVFX.SetActive(IsBleeding);


        // 🔧 อัพเดทสีเฉพาะเมื่อไม่มีการ flash
        if (characterRenderer != null && !isTakingDamage && !isFlashingFromPoison)
        {
            Color targetColor = IsPoisoned ? poisonColor : originalColor;

            // ใช้ threshold ในการเปรียบเทียบสี
            float colorDifference = Vector4.Distance(characterRenderer.material.color, targetColor);
            if (colorDifference > 0.1f)
            {
                characterRenderer.material.color = targetColor;
                Debug.Log($"[UpdateVisualEffects] {CharacterName} color changed to: {(IsPoisoned ? "Poison" : "Original")} - {targetColor}");
            }
        }
    }
    #endregion

    // ========== Public Methods for External Use ==========
    public bool IsSpawned => Object != null && Object.IsValid;

    public void ForceUpdateNetworkState()
    {
        if (HasStateAuthority)
        {
            NetworkedMaxHp = maxHp;
            NetworkedCurrentHp = currentHp;
            NetworkedMaxMana = maxMana;
            NetworkedCurrentMana = currentMana;
            IsNetworkStateReady = true;
        }
    }

    // ========== Debug Methods ==========
   

    // ========== Testing Methods ==========
    

   
   
}