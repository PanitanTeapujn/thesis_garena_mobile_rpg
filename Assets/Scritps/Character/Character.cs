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

    // 🆕 เพิ่ม manual timer สำหรับ poison
    [Networked] public float PoisonNextTickTime { get; set; }

    [Header("Physics")]
    public Rigidbody rb;

    [Header("Visual")]
    public Renderer characterRenderer;
    public Color originalColor;

    [Header("Status Effects - Poison")]
    public Color poisonColor = new Color(0.5f, 1f, 0.5f); // สีเขียวอ่อน
    public GameObject poisonVFX;
    private float poisonTickInterval = 1f; // ทุก 1 วินาที

    // 🔧 เพิ่มตัวแปรสำหรับจัดการสี
    private bool isTakingDamage = false;
    private bool isFlashingFromPoison = false; // 🆕 ป้องกัน UpdateVisualEffects override

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
                Debug.Log($"[POISON DEBUG] {CharacterName} - Duration: {PoisonDuration:F2}, Current Time: {currentTime:F2}, Next Tick Time: {PoisonNextTickTime:F2}");

                if (currentTime >= PoisonNextTickTime)
                {
                    Debug.Log($"[Poison Tick] {CharacterName} applying poison damage!");
                    ApplyPoisonDamage();
                    // ตั้งเวลาสำหรับ tick ถัดไป
                    PoisonNextTickTime = currentTime + poisonTickInterval;
                    Debug.Log($"[Poison Tick] {CharacterName} next poison tick at: {PoisonNextTickTime:F2}");
                }
            }

            // Check if poison duration expired
            if (IsPoisoned && PoisonDuration > 0)
            {
                PoisonDuration -= Runner.DeltaTime;
                if (PoisonDuration <= 0)
                {
                    Debug.Log($"[Poison] {CharacterName} poison duration expired, removing poison");
                    RemovePoison();
                }
            }
        }
    }

    // ========== Network Damage System ==========
    public virtual void TakeDamage(int damage, DamageType damageType = DamageType.Normal, bool isCritical = false)
    {
        if (!HasStateAuthority && !HasInputAuthority) return;

        int finalDamage = CalculateDamage(damage, isCritical);

        int oldHp = currentHp;
        currentHp -= finalDamage;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);

        Debug.Log($"[TakeDamage] {CharacterName}: {oldHp} -> {currentHp} (damage: {finalDamage}, type: {damageType})");

        // Sync based on authority
        if (HasStateAuthority)
        {
            NetworkedCurrentHp = currentHp;
            RPC_BroadcastHealthUpdate(currentHp);
        }
        else if (HasInputAuthority)
        {
            RPC_UpdateHealth(currentHp);
        }

        // Trigger damage flash for everyone to see
        RPC_TriggerDamageFlash(damageType, isCritical);

        if (currentHp <= 0)
        {
            RPC_OnDeath();
        }
    }

    protected virtual int CalculateDamage(int damage, bool isCritical)
    {
        if (isCritical)
        {
            return damage; // Critical hits ignore armor
        }

        int damageAfterArmor = damage - armor;
        return Mathf.Max(1, damageAfterArmor); // Minimum 1 damage
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

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_TriggerDamageFlash(DamageType damageType, bool isCritical)
    {
        StartCoroutine(NetworkDamageFlash(damageType, isCritical));
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_OnDeath()
    {
        Debug.Log($"{CharacterName} died!");
        // Handle death logic here
    }

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
            case DamageType.Critical:
                return new Color(1f, 1f, 0f); // สีเหลือง
            default:
                return isCritical ? new Color(1f, 0.5f, 0f) : Color.red;
        }
    }

    private Color GetReturnColor()
    {
        if (IsPoisoned)
            return poisonColor;

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

    [ContextMenu("Test Quick Poison Self")]
    public void TestQuickPoisonSelf()
    {
        if (HasStateAuthority)
        {
            Debug.Log($"[TEST] {CharacterName} applies quick poison to self!");
            ApplyPoison(5, 3f); // 5 damage ต่อวินาที เป็นเวลา 3 วินาที
        }
    }

    [ContextMenu("Test Force Remove Poison")]
    public void TestForceRemovePoison()
    {
        if (HasStateAuthority)
        {
            Debug.Log($"[TEST] {CharacterName} force removes poison!");
            RemovePoison();
        }
    }

    [ContextMenu("Debug Poison State")]
    public void DebugPoisonState()
    {
        Debug.Log($"[DEBUG POISON] {CharacterName}:");
        Debug.Log($"  - IsPoisoned: {IsPoisoned}");
        Debug.Log($"  - PoisonDuration: {PoisonDuration}");
        Debug.Log($"  - PoisonDamagePerTick: {PoisonDamagePerTick}");
        Debug.Log($"  - PoisonTickTimer.IsRunning: {!PoisonTickTimer.ExpiredOrNotRunning(Runner)}");
        Debug.Log($"  - isTakingDamage: {isTakingDamage}");
        Debug.Log($"  - isFlashingFromPoison: {isFlashingFromPoison}");
        Debug.Log($"  - HasStateAuthority: {HasStateAuthority}");
        Debug.Log($"  - HasInputAuthority: {HasInputAuthority}");
        Debug.Log($"  - Current Color: {(characterRenderer ? characterRenderer.material.color.ToString() : "No Renderer")}");
        Debug.Log($"  - Poison Color: {poisonColor}");
        Debug.Log($"  - Original Color: {originalColor}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowPoisonEffect(bool show)
    {
        Debug.Log($"[RPC_ShowPoisonEffect] {CharacterName} poison effect: {show}");

        if (poisonVFX != null)
        {
            poisonVFX.SetActive(show);
            Debug.Log($"[RPC_ShowPoisonEffect] {CharacterName} poison VFX set to: {show}");
        }

        // 🔧 เปลี่ยนสีทันทีเหมือนโค้ดเก่า แต่เช็ค isFlashingFromPoison ก่อน
        if (characterRenderer != null && !isFlashingFromPoison)
        {
            if (show)
            {
                characterRenderer.material.color = poisonColor;
                Debug.Log($"[RPC_ShowPoisonEffect] {CharacterName} color changed to poison color: {poisonColor}");
            }
            else
            {
                characterRenderer.material.color = originalColor;
                Debug.Log($"[RPC_ShowPoisonEffect] {CharacterName} color changed to original color: {originalColor}");
            }
        }
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
            Debug.Log($"[UpdateVisualEffects] {CharacterName} poison VFX set to: {IsPoisoned}");
        }

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
    public void DebugNetworkState()
    {
        Debug.Log($"[DEBUG] {CharacterName} Network State:");
        Debug.Log($"  - HasInputAuthority: {HasInputAuthority}");
        Debug.Log($"  - HasStateAuthority: {HasStateAuthority}");
        Debug.Log($"  - IsNetworkStateReady: {IsNetworkStateReady}");
        Debug.Log($"  - CurrentHp: {currentHp} | NetworkedCurrentHp: {NetworkedCurrentHp}");
        Debug.Log($"  - IsPoisoned: {IsPoisoned}, Duration: {PoisonDuration}");
    }

    // ========== Testing Methods ==========
    [ContextMenu("Test Apply Poison")]
    public void TestApplyPoison()
    {
        if (HasStateAuthority)
        {
            ApplyPoison(5, 10f); // 5 damage per tick for 10 seconds
        }
    }

    [ContextMenu("Test Remove Poison")]
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

    [ContextMenu("Test Take Poison Damage")]
    public void TestTakePoisonDamage()
    {
        if (HasInputAuthority)
        {
            TakeDamage(15, DamageType.Poison, false);
        }
    }
}