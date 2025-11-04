using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class FireSlime : NetworkEnemy
{
    [Header("🔥 Fire Slime Attack Settings")]
    [SerializeField] private float attackRadius = 3f;           // รัศมีการโจมตี
    [SerializeField] private float attackCooldown = 3f;         // ความถี่การโจมตี
    [SerializeField] private float telegraphDuration = 1.5f;    // เวลาขยายวงกลม
    [SerializeField] private float shakeDuration = 0.5f;        // เวลาสั่นตัว
    [SerializeField] private float postAttackCooldown = 1.5f;   // เวลา cooldown หลังโจมตี

    [Header("🎨 Visual Effects")]
    [SerializeField] private GameObject telegraphPrefab;        // Prefab วงกลมสีแดง
    [SerializeField] private ParticleSystem fireAttackEffect;  // Particle ไฟตอนโจมตี
    [SerializeField] private Color telegraphColor = new Color(1f, 0f, 0f, 0.5f);
    [SerializeField] private float telegraphYOffset = -1.16f;   // 🆕 ตำแหน่ง Y ของวงกลม

    [Header("🎯 Attack Behavior")]
    [SerializeField] private float stopDistance = 4f;           // ระยะที่จะหยุดเพื่อเตรียมโจมตี
    [SerializeField] private bool useStatusEffect = true;
    [SerializeField] private int burnDamage = 5;
    [SerializeField] private float burnDuration = 6f;

    // Private variables
    private float nextAttackTime = 0f;
    private bool isAttacking = false;
    private GameObject currentTelegraph;
    private Vector3 originalPosition; // 🆕 เก็บตำแหน่งเดิมก่อนสั่น
    private bool wasMovementEnabled = true; // 🆕 เก็บสถานะการเคลื่อนที่

    private enum AttackPhase
    {
        None,
        Telegraph,
        Charging,
        Execute,
        Cooldown
    }
    private AttackPhase currentPhase = AttackPhase.None;

    protected override void Start()
    {
        base.Start();

        SetupFireSlimeStats();
        originalPosition = transform.position;

        Debug.Log($"🔥 {CharacterName} spawned!");
    }

    private void SetupFireSlimeStats()
    {
        CharacterName = "Fire Slime";
        AttackType = AttackType.Magic;

    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (HasStateAuthority && !IsDead)
        {
            ProcessFireSlimeAttack();
        }
    }

    private void ProcessFireSlimeAttack()
    {
        if (isAttacking) return;

        if (targetTransform == null)
        {
            FindNearestPlayer();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, targetTransform.position);

        if (distanceToPlayer <= stopDistance && Runner.SimulationTime >= nextAttackTime)
        {
            StartCoroutine(PerformFireAttackSequence());
        }
    }

    private IEnumerator PerformFireAttackSequence()
    {
        isAttacking = true;
        currentPhase = AttackPhase.Telegraph;

        // 🆕 หยุดการเคลื่อนที่โดยการปิด AI ของ NetworkEnemy
        StopMovement();

        // บันทึกตำแหน่งก่อนเริ่มโจมตี
        originalPosition = transform.position;

        // 🔴 Phase 1: Telegraph
        Debug.Log($"🔥 {CharacterName}: Telegraph Phase");
        yield return StartCoroutine(ShowTelegraphCircle());

        // 🟡 Phase 2: Charging
        currentPhase = AttackPhase.Charging;
        Debug.Log($"🔥 {CharacterName}: Charging Phase");
        yield return StartCoroutine(ShakeBody());

        // 🔥 Phase 3: Execute
        currentPhase = AttackPhase.Execute;
        Debug.Log($"🔥 {CharacterName}: Execute Attack!");
        ExecuteFireAttack();

        // 💤 Phase 4: Cooldown
        currentPhase = AttackPhase.Cooldown;
        Debug.Log($"🔥 {CharacterName}: Cooldown Phase");
        yield return new WaitForSeconds(postAttackCooldown);

        // 🆕 เปิดการเคลื่อนที่ใหม่
        ResumeMovement();

        // รีเซ็ต
        isAttacking = false;
        currentPhase = AttackPhase.None;
        nextAttackTime = Runner.SimulationTime + attackCooldown;

        Debug.Log($"🔥 {CharacterName}: Ready for next attack");
    }

    // 🆕 หยุดการเคลื่อนที่
    private void StopMovement()
    {
        // ปิด movement โดยการตั้ง MoveSpeed เป็น 0 ชั่วคราว
        wasMovementEnabled = MoveSpeed > 0;
        if (wasMovementEnabled)
        {
            MoveSpeed = 0f;
        }

        // หยุด velocity ของ rigidbody
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }

        // บังคับให้ target เป็น null ชั่วคราว เพื่อไม่ให้ base class เคลื่อนที่
        targetTransform = null;

        Debug.Log($"🔥 {CharacterName}: Movement STOPPED");
    }

    // 🆕 เปิดการเคลื่อนที่ใหม่
    private void ResumeMovement()
    {
        if (wasMovementEnabled)
        {
            // คืนค่า MoveSpeed จาก characterStats
            if (characterStats != null)
            {
                MoveSpeed = characterStats.moveSpeed;
            }
        }

        Debug.Log($"🔥 {CharacterName}: Movement RESUMED");
    }

    private IEnumerator ShowTelegraphCircle()
    {
        // 🆕 สร้าง telegraph ที่ตำแหน่งที่ถูกต้อง
        Vector3 telegraphPosition = new Vector3(
            transform.position.x,
            telegraphYOffset, // ใช้ค่าที่กำหนดไว้
            transform.position.z
        );

        if (telegraphPrefab != null)
        {
            currentTelegraph = Instantiate(telegraphPrefab, telegraphPosition, Quaternion.identity);
        }
        else
        {
            currentTelegraph = CreateProceduralTelegraph(telegraphPosition);
        }

        RPC_ShowTelegraph(telegraphPosition);

        // Animation ขยายวงกลม
        float elapsed = 0f;
        Vector3 startScale = new Vector3(0.1f, 0.05f, 0.1f);
        Vector3 targetScale = new Vector3(attackRadius * 2f, 0.05f, attackRadius * 2f);

        while (elapsed < telegraphDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / telegraphDuration;

            // Smooth expansion
            float smoothProgress = 1f - Mathf.Pow(1f - progress, 3f);

            if (currentTelegraph != null)
            {
                // 🆕 คงตำแหน่ง Y ไว้ ไม่ให้ตามตัวละคร
                currentTelegraph.transform.position = new Vector3(
                    originalPosition.x,
                    telegraphYOffset,
                    originalPosition.z
                );

                currentTelegraph.transform.localScale = Vector3.Lerp(startScale, targetScale, smoothProgress);

                // Pulse effect
                float pulse = Mathf.Sin(elapsed * 8f) * 0.1f + 0.9f;
                Renderer renderer = currentTelegraph.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color color = telegraphColor;
                    color.a = telegraphColor.a * pulse;
                    renderer.material.color = color;
                }
            }

            yield return null;
        }

        // ซ่อน telegraph
        if (currentTelegraph != null)
        {
            Destroy(currentTelegraph);
            currentTelegraph = null;
        }
        RPC_HideTelegraph();
    }

    private IEnumerator ShakeBody()
    {
        float elapsed = 0f;
        float shakeIntensity = 0.15f;
        float shakeSpeed = 25f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;

            // Shake animation (แกว่งซ้าย-ขวา)
            float offsetX = Mathf.Sin(elapsed * shakeSpeed) * shakeIntensity;

            // 🆕 คงตำแหน่ง Y และ Z ไว้ แกว่งแค่ X
            transform.position = new Vector3(
                originalPosition.x + offsetX,
                originalPosition.y,
                originalPosition.z
            );

            yield return null;
        }

        // 🆕 คืนตำแหน่งเดิม
        transform.position = originalPosition;
    }

    private void ExecuteFireAttack()
    {
        // แสดง particle effect
        if (fireAttackEffect != null)
        {
            fireAttackEffect.Play();
        }

        RPC_ShowFireAttackEffect(transform.position);

        // หาผู้เล่นในรัศมี
        Collider[] targets = Physics.OverlapSphere(transform.position, attackRadius, LayerMask.GetMask("Player"));

        foreach (Collider target in targets)
        {
            Hero hero = target.GetComponent<Hero>();
            if (hero != null)
            {
                hero.TakeDamageFromAttacker(0, MagicDamage, this, DamageType.Magic);

                if (useStatusEffect)
                {
                    hero.ApplyStatusEffect(StatusEffectType.Burn, burnDamage, burnDuration);
                    Debug.Log($"🔥 {CharacterName} burned {hero.CharacterName}!");
                }

                Debug.Log($"🔥 {CharacterName} hit {hero.CharacterName} for {MagicDamage} magic damage!");
            }
        }
    }

    // 🆕 รับ position เป็น parameter
    private GameObject CreateProceduralTelegraph(Vector3 position)
    {
        GameObject telegraph = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        telegraph.name = "FireSlime_Telegraph";
        telegraph.transform.position = position;
        telegraph.transform.localScale = new Vector3(0.1f, 0.05f, 0.1f);

        Destroy(telegraph.GetComponent<Collider>());

        Renderer renderer = telegraph.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = telegraphColor;
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            renderer.material = mat;
        }

        return telegraph;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowTelegraph(Vector3 position)
    {
        Debug.Log($"[Client] Showing telegraph at {position}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HideTelegraph()
    {
        Debug.Log("[Client] Hiding telegraph");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowFireAttackEffect(Vector3 position)
    {
        if (fireAttackEffect != null)
        {
            ParticleSystem fx = Instantiate(fireAttackEffect, position, Quaternion.identity);
            Destroy(fx.gameObject, 2f);
        }
        Debug.Log($"[Client] Fire attack effect at {position}");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        // 🆕 แสดงตำแหน่งวงกลม
        Gizmos.color = Color.cyan;
        Vector3 telegraphPos = new Vector3(transform.position.x, telegraphYOffset, transform.position.z);
        Gizmos.DrawWireSphere(telegraphPos, 0.2f);
    }

    protected override void RPC_OnDeath()
    {
        if (currentTelegraph != null)
        {
            Destroy(currentTelegraph);
        }

        base.RPC_OnDeath();
    }
}