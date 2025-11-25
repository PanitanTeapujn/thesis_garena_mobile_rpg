using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class IceBird : NetworkEnemy
{
    [Header("🐦 Ice Bird Dash Settings")]
    [SerializeField] private float dashDistance = 8f;           // ระยะพุ่ง
    [SerializeField] private float attackCooldown = 5f;         // ความถี่การโจมตี
    [SerializeField] private float telegraphDuration = 1.2f;    // เวลาแสดงเตือน
    [SerializeField] private float dashDuration = 0.3f;         // เวลาพุ่ง (เร็วมาก)
    [SerializeField] private float postDashCooldown = 1.5f;     // เวลา cooldown หลังพุ่ง
    [SerializeField] private float dashWidth = 2f;              // ความกว้างของ dash (hitbox)

    [Header("🎨 Visual Effects")]
    [SerializeField] private GameObject telegraphPrefab;        // Prefab สี่เหลี่ยมฟ้า
    [SerializeField] private ParticleSystem dashEffect;         // Particle เกล็ดน้ำแข็ง
    [SerializeField] private TrailRenderer dashTrail;           // Trail หางน้ำแข็ง
    [SerializeField] private Color telegraphColor = new Color(0.5f, 0.8f, 1f, 0.6f); // สีฟ้าน้ำแข็ง
    [SerializeField] private float telegraphYOffset = -1.16f;   // ตำแหน่ง Y ของสี่เหลี่ยม

    [Header("🎯 Attack Behavior")]
    [SerializeField] private float stopDistance = 10f;          // ระยะที่จะหยุดเพื่อเตรียมโจมตี
    [SerializeField] private float stunDuration = 1.5f;         // ระยะเวลา Stun
    [SerializeField] private bool showDashPath = true;          // แสดงเส้นทาง debug

    // Private variables
    private float nextAttackTime = 0f;
    private bool isAttacking = false;
    private GameObject currentTelegraph;
    private Vector3 dashStartPosition;
    private Vector3 dashDirection;
    private List<Hero> hitHeroes = new List<Hero>(); // เก็บว่าโจมตีใครไปแล้วบ้าง

    private enum AttackPhase
    {
        None,
        Telegraph,      // แสดงสี่เหลี่ยม
        Charging,       // สั่นตัว
        Dashing,        // พุ่งไป
        Cooldown        // พักหลังพุ่ง
    }
    private AttackPhase currentPhase = AttackPhase.None;

    protected override void Start()
    {
        base.Start();

        SetupIceBirdStats();
        dashStartPosition = transform.position;

        // Setup Trail
        if (dashTrail != null)
        {
            dashTrail.enabled = false;
            dashTrail.startColor = new Color(0.5f, 0.8f, 1f, 0.8f); // ฟ้าน้ำแข็ง
            dashTrail.endColor = new Color(0.5f, 0.8f, 1f, 0f);
        }

        Debug.Log($"🐦 {CharacterName} spawned!");
    }

    private void SetupIceBirdStats()
    {
        CharacterName = "Ice Bird";
        AttackType = AttackType.Physical;

        // ปรับสี
        Renderer birdRenderer = GetComponent<Renderer>();
        if (birdRenderer != null)
        {
            birdRenderer.material.color = new Color(0.7f, 0.9f, 1f); // ฟ้าอ่อน
        }
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (HasStateAuthority && !IsDead)
        {
            ProcessIceBirdAttack();
        }
    }

    // ระบบโจมตีหลัก
    private void ProcessIceBirdAttack()
    {
        // ถ้ากำลังโจมตีอยู่ ไม่ต้องเช็คอะไร
        if (isAttacking) return;

        // หาผู้เล่น
        if (targetTransform == null)
        {
            // ให้ Manager หา target ให้
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, targetTransform.position);

        // ถ้าอยู่ในระยะที่พอดี และพ้น cooldown แล้ว
        if (distanceToPlayer <= stopDistance && Runner.SimulationTime >= nextAttackTime)
        {
            StartCoroutine(PerformDashAttackSequence());
        }
    }

    // Coroutine โจมตีหลัก
    // แก้ใน PerformDashAttackSequence()
    private IEnumerator PerformDashAttackSequence()
    {
        isAttacking = true;
        currentPhase = AttackPhase.Telegraph;
        hitHeroes.Clear();

        StopMovement();
        dashStartPosition = transform.position;

        // ✅ FIX: หา target ใหม่อีกครั้งก่อนคำนวณทิศทาง (กันกรณี target หาย)
        if (targetTransform == null)
        {
            // ถ้าไม่มี target ให้ยกเลิกการโจมตี
            Debug.LogWarning("⚠️ No target found! Cancelling attack");
            isAttacking = false;
            ResumeMovement();
            yield break;
        }


        // ✅ DEBUG: ดูว่า target มีไหม
        Debug.Log($"🐦 Bird Position: {transform.position}");
        Debug.Log($"🎯 Target: {(targetTransform != null ? targetTransform.name : "NULL")}");

        if (targetTransform != null)
        {
            Debug.Log($"🎯 Player Position: {targetTransform.position}");

            // คำนวณทิศทาง
            Vector3 toPlayer = targetTransform.position - transform.position;
            Debug.Log($"📐 Vector to Player (before normalize): {toPlayer}");

            dashDirection = toPlayer.normalized;
            dashDirection.y = 0; // ไม่พุ่งขึ้น-ลง

            Debug.Log($"➡️ Dash Direction (normalized): {dashDirection}");

            // ✅ วาดเส้นใน Scene view
            Debug.DrawRay(transform.position, dashDirection * dashDistance, Color.red, 3f);
        }
        else
        {
            Debug.LogWarning("⚠️ No target found! Using forward direction");
            dashDirection = transform.forward;
            dashDirection.y = 0;
        }

        // เหลือเหมือนเดิม...
        Debug.Log($"🐦 {CharacterName}: Telegraph Phase");
        yield return StartCoroutine(ShowDashTelegraph());

        currentPhase = AttackPhase.Charging;
        Debug.Log($"🐦 {CharacterName}: Charging Phase");
        yield return StartCoroutine(ShakeBody());

        currentPhase = AttackPhase.Dashing;
        Debug.Log($"🐦 {CharacterName}: DASH!");
        yield return StartCoroutine(PerformDash());

        currentPhase = AttackPhase.Cooldown;
        Debug.Log($"🐦 {CharacterName}: Cooldown Phase");
        yield return new WaitForSeconds(postDashCooldown);

        isAttacking = false;
        currentPhase = AttackPhase.None;
        nextAttackTime = Runner.SimulationTime + attackCooldown;

        ResumeMovement();

        Debug.Log($"🐦 {CharacterName}: Ready for next attack");
    }
    // แสดงสี่เหลี่ยม Telegraph
    // แก้ใน ShowDashTelegraph()
    private IEnumerator ShowDashTelegraph()
    {
        // ✅ FIX: Telegraph เริ่มต้นที่ตำแหน่งนก
        Vector3 telegraphPosition = new Vector3(
            dashStartPosition.x,  // ใช้ dashStartPosition แทน transform.position
            telegraphYOffset,
            dashStartPosition.z
        );

        // ✅ FIX: Rotation ชี้ไปทาง dash
        Quaternion telegraphRotation = Quaternion.LookRotation(dashDirection);

        if (telegraphPrefab != null)
        {
            currentTelegraph = Instantiate(telegraphPrefab, telegraphPosition, telegraphRotation);
        }
        else
        {
            currentTelegraph = CreateProceduralDashTelegraph(telegraphPosition, telegraphRotation);
        }

        RPC_ShowTelegraph(telegraphPosition, dashDirection);

        float elapsed = 0f;
        Vector3 startScale = new Vector3(dashWidth, 0.05f, 0.5f);
        Vector3 targetScale = new Vector3(dashWidth, 0.05f, dashDistance);

        while (elapsed < telegraphDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / telegraphDuration;
            float smoothProgress = 1f - Mathf.Pow(1f - progress, 3f);

            if (currentTelegraph != null)
            {
                // ✅ FIX: ขยายไปข้างหน้า (ไปทาง dashDirection)
                // ตำแหน่ง = จุดเริ่มต้น + (ทิศทาง × ระยะครึ่งหนึ่ง)
                float currentLength = dashDistance * smoothProgress;
                Vector3 offset = dashDirection * (currentLength * 0.5f);

                currentTelegraph.transform.position = new Vector3(
                    dashStartPosition.x + offset.x,
                    telegraphYOffset,
                    dashStartPosition.z + offset.z
                );

                // รักษา rotation ให้ชี้ไปทาง dash ตลอด
                currentTelegraph.transform.rotation = Quaternion.LookRotation(dashDirection);

                // ขยาย scale ตามความยาว
                currentTelegraph.transform.localScale = Vector3.Lerp(startScale, targetScale, smoothProgress);

                // Pulse effect
                float pulse = Mathf.Sin(elapsed * 10f) * 0.15f + 0.85f;
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

        if (currentTelegraph != null)
        {
            Destroy(currentTelegraph);
            currentTelegraph = null;
        }
        RPC_HideTelegraph();
    }


    // สั่นตัว (แบบเดียวกับ Fire Slime)
    private IEnumerator ShakeBody()
    {
        float elapsed = 0f;
        float shakeIntensity = 0.2f;
        float shakeSpeed = 30f;

        while (elapsed < 0.4f) // สั่น 0.4 วินาที
        {
            elapsed += Time.deltaTime;

            // Shake animation
            float offsetX = Mathf.Sin(elapsed * shakeSpeed) * shakeIntensity;

            transform.position = new Vector3(
                dashStartPosition.x + offsetX,
                dashStartPosition.y,
                dashStartPosition.z
            );

            yield return null;
        }

        // คืนตำแหน่งเดิม
        transform.position = dashStartPosition;
    }

    // ปล่อย Dash!
    private IEnumerator PerformDash()
    {
        // เปิด Trail
        if (dashTrail != null)
        {
            dashTrail.enabled = true;
        }

        // แสดง Particle
        if (dashEffect != null)
        {
            dashEffect.Play();
        }

        RPC_ShowDashEffect();

        // พุ่งไป!
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + dashDirection * dashDistance;

        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / dashDuration;

            // Smooth dash (ease-out)
            float smoothProgress = Mathf.Sin(progress * Mathf.PI * 0.5f);

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, smoothProgress);
            transform.position = currentPos;

            // ตรวจสอบการชนผู้เล่น
            CheckDashCollision(currentPos);

            yield return null;
        }

        // ปิด Trail
        if (dashTrail != null)
        {
            dashTrail.enabled = false;
        }

        // หยุด Particle
        if (dashEffect != null)
        {
            dashEffect.Stop();
        }
    }

    // ตรวจสอบการชนผู้เล่นขณะ dash
    private void CheckDashCollision(Vector3 currentPos)
    {
        // ใช้ BoxCast ตรวจจับ (ดีกว่า OverlapBox เพราะตรวจตลอดเส้นทาง)
        Vector3 boxSize = new Vector3(dashWidth, 2f, 1f);
        Collider[] hits = Physics.OverlapBox(currentPos, boxSize * 0.5f, Quaternion.LookRotation(dashDirection), LayerMask.GetMask("Player"));

        foreach (Collider hit in hits)
        {
            Hero hero = hit.GetComponent<Hero>();
            if (hero != null && !hitHeroes.Contains(hero))
            {
                hitHeroes.Add(hero); // เพิ่มในรายชื่อ (ไม่โดนซ้ำ)
                DealDashDamage(hero);
            }
        }
    }

    // ทำดาเมจให้ผู้เล่น
    private void DealDashDamage(Hero hero)
    {
        // ✅ คำนวณทิศทาง knockback (จากนกไปหาผู้เล่น)
        Vector3 knockbackDirection = (hero.transform.position - transform.position).normalized;

        // ✅ 1. ทำดาเมจ
        hero.TakeDamageFromAttacker(AttackDamage, 0, this, DamageType.Normal);

        // ✅ 2. Knockback
        hero.ApplyKnockback(knockbackDirection, 50f, 1f);

        // ✅ 3. ใส่ Stun (หลัง knockback เสร็จ)

      

        // แสดง effect
        RPC_ShowHitEffect(hero.transform.position);
    }

    // สร้าง Telegraph แบบ Procedural (สี่เหลี่ยม)
    private GameObject CreateProceduralDashTelegraph(Vector3 position, Quaternion rotation)
    {
        GameObject telegraph = GameObject.CreatePrimitive(PrimitiveType.Cube);
        telegraph.name = "IceBird_Telegraph";
        telegraph.transform.position = position;

        // ✅ FIX: ใช้ rotation ที่ส่งเข้ามา (ไม่บวก Euler)
        telegraph.transform.rotation = rotation;
        telegraph.transform.localScale = new Vector3(dashWidth, 0.05f, 0.5f);

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

    // หยุด/เปิดการเคลื่อนที่ (เหมือน Fire Slime)
    private void StopMovement()
    {
        MoveSpeed = 0f;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    private void ResumeMovement()
    {
        if (characterStats != null)
        {
            MoveSpeed = characterStats.moveSpeed;
        }
    }

    // ========== RPCs สำหรับ Multiplayer ==========
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowTelegraph(Vector3 position, Vector3 direction)
    {
        Debug.Log($"[Client] Showing Ice Bird telegraph at {position}, direction: {direction}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HideTelegraph()
    {
        Debug.Log("[Client] Hiding Ice Bird telegraph");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowDashEffect()
    {
        // Clients แสดง particle effect
        if (dashEffect != null)
        {
            ParticleSystem fx = Instantiate(dashEffect, transform.position, Quaternion.identity);
            fx.transform.SetParent(transform); // ติดตามตัวมอน
            Destroy(fx.gameObject, 2f);
        }
        Debug.Log("[Client] Ice Bird dashing!");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowHitEffect(Vector3 position)
    {
        // แสดง effect เมื่อโดน (เกล็ดน้ำแข็งระเบิด)
        Debug.Log($"[Client] Ice Bird hit at {position}");
    }

    // ========== Debug Gizmos ==========
    private void OnDrawGizmosSelected()
    {
        // วงกลมระยะหยุด
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        // แสดงเส้นทาง dash
        if (showDashPath && targetTransform != null)
        {
            Vector3 direction = (targetTransform.position - transform.position).normalized;
            direction.y = 0;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + direction * dashDistance);

            // แสดง hitbox
            Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.5f);
            Vector3 boxCenter = transform.position + direction * (dashDistance * 0.5f);
            Gizmos.matrix = Matrix4x4.TRS(boxCenter, Quaternion.LookRotation(direction), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(dashWidth, 2f, dashDistance));
            Gizmos.matrix = Matrix4x4.identity;
        }

        // แสดงตำแหน่ง telegraph
        Gizmos.color = Color.yellow;
        Vector3 telegraphPos = new Vector3(transform.position.x, telegraphYOffset, transform.position.z);
        Gizmos.DrawWireSphere(telegraphPos, 0.3f);
    }

    protected override void RPC_OnDeath()
    {
        // ทำลาย telegraph ถ้ายังมีอยู่
        if (currentTelegraph != null)
        {
            Destroy(currentTelegraph);
        }

        base.RPC_OnDeath();
    }
}