using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class IceSlime : NetworkEnemy
{
    [Header("❄️ Ice Slime Attack Settings")]
    [SerializeField] private float attackRadius = 4f;           // รัศมีครึ่งวงกลม
    [SerializeField] private float attackAngle = 180f;          // มุมครึ่งวงกลม
    [SerializeField] private float attackCooldown = 4f;         // ความถี่การโจมตี
    [SerializeField] private float telegraphDuration = 2f;      // เวลาขยายวงกลม (Slow phase)
    [SerializeField] private float freezeExecuteDelay = 0.5f;   // เวลารอก่อน Freeze
    [SerializeField] private float postAttackCooldown = 1.5f;   // เวลา cooldown หลังโจมตี

    [Header("🎨 Visual Effects")]
    [SerializeField] private GameObject telegraphPrefab;        // Prefab ครึ่งวงกลมสีฟ้า
    [SerializeField] private ParticleSystem iceAttackEffect;    // Particle น้ำแข็งตอนโจมตี
    [SerializeField] private Color telegraphColor = new Color(0.5f, 0.7f, 1f, 0.5f); // สีฟ้าน้ำแข็ง
    [SerializeField] private float telegraphYOffset = -1.16f;   // ตำแหน่ง Y ของวงกลม

    [Header("🎯 Attack Behavior")]
    [SerializeField] private float stopDistance = 5f;           // ระยะที่จะหยุดเพื่อเตรียมโจมตี
    [SerializeField] private bool useStatusEffect = true;
    [SerializeField] private float slowAmount = 0.5f;           // Slow 50%
    [SerializeField] private float slowDuration = 2f;           // Slow นาน 2 วินาที
    [SerializeField] private float freezeDuration = 3f;         // Freeze นาน 3 วินาที

    // Private variables
    private float nextAttackTime = 0f;
    private bool isAttacking = false;
    private GameObject currentTelegraph;
    private Vector3 originalPosition;
    private bool wasMovementEnabled = true;
    private Animator animator;

    // Animation triggers (ใช้เหมือน BossSlime)
    private const string ANIM_ATTACK = "attack";
    private const string ANIM_SKILL1 = "skill1";
    private const string ANIM_SKILL2 = "skill2";

    private enum AttackPhase
    {
        None,
        Telegraph,      // แสดง telegraph + Slow
        Freeze,         // Execute Freeze
        Cooldown
    }
    private AttackPhase currentPhase = AttackPhase.None;

    protected override void Start()
    {
        base.Start();
        SetupIceSlimeStats();
        originalPosition = transform.position;
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogWarning($"⚠️ Animator not found on {CharacterName}!");
        }

        Debug.Log($"❄️ {CharacterName} spawned!");
    }

    private void SetupIceSlimeStats()
    {
        CharacterName = "Ice Slime";
        AttackType = AttackType.Magic;
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (HasStateAuthority && !IsDead)
        {
            ProcessIceSlimeAttack();
        }
    }

    private void ProcessIceSlimeAttack()
    {
        if (isAttacking) return;

        if (targetTransform == null)
        {
            // 🔧 ลบ FindNearestPlayer() ออก - ให้ Manager จัดการ
            // FindNearestPlayer(); // ❌ ลบบรรทัดนี้
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, targetTransform.position);

        if (distanceToPlayer <= stopDistance && Runner.SimulationTime >= nextAttackTime)
        {
            StartCoroutine(PerformIceAttackSequence());
        }
    }

    private IEnumerator PerformIceAttackSequence()
    {
        isAttacking = true;
        currentPhase = AttackPhase.Telegraph;

        // ❄️ หยุดการเคลื่อนที่
        StopMovement();

        // บันทึกตำแหน่งก่อนเริ่มโจมตี
        originalPosition = transform.position;

        // ❄️ Phase 1: Telegraph + Slow
        Debug.Log($"❄️ {CharacterName}: Telegraph Phase (Slow)");
        yield return StartCoroutine(ShowTelegraphAndSlow());

        // ❄️ Phase 2: Freeze Execute
        currentPhase = AttackPhase.Freeze;
        Debug.Log($"❄️ {CharacterName}: Freeze Execute!");

        // Play animation
        RPC_PlayAnimation(ANIM_ATTACK);

        yield return new WaitForSeconds(freezeExecuteDelay);

        ExecuteIceAttack();

        // 💤 Phase 3: Cooldown
        currentPhase = AttackPhase.Cooldown;
        Debug.Log($"❄️ {CharacterName}: Cooldown Phase");
        yield return new WaitForSeconds(postAttackCooldown);

        // ❄️ เปิดการเคลื่อนที่ใหม่
        ResumeMovement();

        // รีเซ็ต
        isAttacking = false;
        currentPhase = AttackPhase.None;
        nextAttackTime = Runner.SimulationTime + attackCooldown;

        Debug.Log($"❄️ {CharacterName}: Ready for next attack");
    }

    // ❄️ หยุดการเคลื่อนที่
    private void StopMovement()
    {
        wasMovementEnabled = MoveSpeed > 0;
        if (wasMovementEnabled)
        {
            MoveSpeed = 0f;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }

        targetTransform = null;

        Debug.Log($"❄️ {CharacterName}: Movement STOPPED");
    }

    // ❄️ เปิดการเคลื่อนที่ใหม่
    private void ResumeMovement()
    {
        if (wasMovementEnabled)
        {
            if (characterStats != null)
            {
                MoveSpeed = characterStats.moveSpeed;
            }
        }

        Debug.Log($"❄️ {CharacterName}: Movement RESUMED");
    }

    // ❄️ แสดง Telegraph + ทำ Slow ผู้เล่นที่อยู่ในพื้นที่
    // ✅ เพิ่มตัวแปรเก็บตำแหน่ง attack center
    private Vector3 attackCenter;

    // ❄️ แสดง Telegraph + ทำ Slow ผู้เล่นที่อยู่ในพื้นที่
    private IEnumerator ShowTelegraphAndSlow()
    {
        // คำนวณทิศทางหันหน้าไปหาผู้เล่น (ครั้งเดียวตอนเริ่มต้น)
        Vector3 directionToPlayer = targetTransform != null ?
            (targetTransform.position - transform.position).normalized : transform.forward;
        directionToPlayer.y = 0;

        // หมุนตัวหันหน้าไปหาผู้เล่น (ครั้งเดียว)
        transform.rotation = Quaternion.LookRotation(directionToPlayer);

        // ✅ แก้ไข: ตำแหน่ง Telegraph อยู่ด้านหลัง
        float forwardOffset = -3f; // ด้านหลัง 3 เมตร

        Vector3 telegraphPosition = transform.position + (transform.forward * forwardOffset);
        telegraphPosition.y = telegraphYOffset;

        // ✅ เก็บตำแหน่งจุดกลางของการโจมตี
        attackCenter = telegraphPosition;

        Quaternion telegraphRotation = transform.rotation;

        if (telegraphPrefab != null)
        {
            currentTelegraph = Instantiate(telegraphPrefab, telegraphPosition, telegraphRotation);
        }
        else
        {
            currentTelegraph = CreateProceduralHalfCircleTelegraph(telegraphPosition, transform.forward);
        }

        RPC_ShowTelegraph(telegraphPosition);

        // ✅ แก้ไข: ขนาด scale
        float elapsed = 0f;
        Vector3 startScale = new Vector3(0.1f, 0.05f, 0.1f);
        Vector3 targetScale = new Vector3(attackRadius * 0.25f, 0.05f, attackRadius * 0.25f); // ✅ เปลี่ยนเป็น 0.25f

        List<Hero> slowedHeroes = new List<Hero>();

        while (elapsed < telegraphDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / telegraphDuration;
            float smoothProgress = 1f - Mathf.Pow(1f - progress, 3f);

            if (currentTelegraph != null)
            {
                currentTelegraph.transform.position = telegraphPosition;
                currentTelegraph.transform.rotation = telegraphRotation;
                currentTelegraph.transform.localScale = Vector3.Lerp(startScale, targetScale, smoothProgress);

                float pulse = Mathf.Sin(elapsed * 8f) * 0.1f + 0.9f;
                Renderer renderer = currentTelegraph.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color color = telegraphColor;
                    color.a = telegraphColor.a * pulse;
                    renderer.material.color = color;
                }
            }

            if (Mathf.FloorToInt(elapsed / 0.2f) > Mathf.FloorToInt((elapsed - Time.deltaTime) / 0.2f))
            {
                ApplySlowInArea(slowedHeroes);
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

    // ✅ แก้ไข: ใช้ attackCenter แทน transform.position
    private void ApplySlowInArea(List<Hero> slowedHeroes)
    {
        Vector3 forward = transform.forward;

        Collider[] targets = Physics.OverlapSphere(attackCenter, attackRadius, LayerMask.GetMask("Player"));

        foreach (Collider target in targets)
        {
            Hero hero = target.GetComponent<Hero>();
            if (hero != null && IsInHalfCircle(hero.transform.position, attackCenter, forward))
            {
                if (useStatusEffect && !slowedHeroes.Contains(hero))
                {
                    hero.ApplyStatusEffect(StatusEffectType.Slow, 0, slowDuration, slowAmount);
                    slowedHeroes.Add(hero);
                    Debug.Log($"❄️ {CharacterName} slowed {hero.CharacterName}!");
                }
            }
        }
    }

    // ✅ แก้ไข: Execute Freeze ใช้ attackCenter
    private void ExecuteIceAttack()
    {
        if (iceAttackEffect != null)
        {
            iceAttackEffect.Play();
        }

        RPC_ShowIceAttackEffect(attackCenter); // ✅ ใช้ attackCenter

        Vector3 forward = transform.forward;

        Collider[] targets = Physics.OverlapSphere(attackCenter, attackRadius, LayerMask.GetMask("Player"));

        foreach (Collider target in targets)
        {
            Hero hero = target.GetComponent<Hero>();
            if (hero != null && IsInHalfCircle(hero.transform.position, attackCenter, forward))
            {
                hero.TakeDamageFromAttacker(0, MagicDamage, this, DamageType.Magic);

                if (useStatusEffect)
                {
                    hero.ApplyStatusEffect(StatusEffectType.Freeze, 0, freezeDuration);
                    Debug.Log($"❄️ {CharacterName} froze {hero.CharacterName}!");
                }

                Debug.Log($"❄️ {CharacterName} hit {hero.CharacterName} for {MagicDamage} magic damage!");
            }
        }
    }

    // ❄️ ตรวจสอบว่าอยู่ในครึ่งวงกลมหรือไม่
    private bool IsInHalfCircle(Vector3 targetPos, Vector3 center, Vector3 forward)
    {
        Vector3 directionToTarget = (targetPos - center).normalized;
        directionToTarget.y = 0;
        forward.y = 0;

        float angle = Vector3.Angle(forward, directionToTarget);
        return angle <= attackAngle / 2f;
    }

    // ❄️ สร้าง Telegraph แบบครึ่งวงกลม
    private GameObject CreateProceduralHalfCircleTelegraph(Vector3 position, Vector3 direction)
    {
        GameObject telegraph = new GameObject("IceSlime_Telegraph");
        telegraph.transform.position = position;
        telegraph.transform.rotation = Quaternion.LookRotation(direction);
        telegraph.transform.localScale = new Vector3(0.1f, 0.05f, 0.1f);

        // สร้าง Mesh ครึ่งวงกลม
        MeshFilter meshFilter = telegraph.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = telegraph.AddComponent<MeshRenderer>();

        meshFilter.mesh = CreateHalfCircleMesh(attackRadius, 32);

        // สร้าง Material
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
        meshRenderer.material = mat;

        return telegraph;
    }

    // ❄️ สร้าง Mesh ครึ่งวงกลม
    private Mesh CreateHalfCircleMesh(float radius, int segments)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // จุดกึ่งกลาง
        vertices.Add(Vector3.zero);

        // สร้างจุดรอบครึ่งวงกลม (180 องศา)
        for (int i = 0; i <= segments / 2; i++)
        {
            float angle = Mathf.Deg2Rad * (i * 360f / segments - 90f); // เริ่มจาก -90° ถึง 90°
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            vertices.Add(new Vector3(x, 0, z));
        }

        // สร้าง triangles
        for (int i = 0; i < segments / 2; i++)
        {
            triangles.Add(0);
            triangles.Add(i + 1);
            triangles.Add(i + 2);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

    // ========== RPCs ==========
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayAnimation(string animationName)
    {
        if (animator == null)
        {
            Debug.LogError($"❌ No Animator on {name}!");
            return;
        }

        bool hasParameter = false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == animationName && param.type == AnimatorControllerParameterType.Trigger)
            {
                hasParameter = true;
                break;
            }
        }

        if (!hasParameter)
        {
            Debug.LogError($"❌ Animator has no Trigger parameter: {animationName}");
            return;
        }

        Debug.Log($"✅ [RPC] Playing animation: {animationName} on {CharacterName}");
        animator.SetTrigger(animationName);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowTelegraph(Vector3 position)
    {
        Debug.Log($"[Client] Showing ice telegraph at {position}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HideTelegraph()
    {
        Debug.Log("[Client] Hiding ice telegraph");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowIceAttackEffect(Vector3 position)
    {
        if (iceAttackEffect != null)
        {
            ParticleSystem fx = Instantiate(iceAttackEffect, position, Quaternion.identity);
            Destroy(fx.gameObject, 2f);
        }
        Debug.Log($"[Client] Ice attack effect at {position}");
    }

    protected override void RPC_OnDeath()
    {
        if (currentTelegraph != null)
        {
            Destroy(currentTelegraph);
        }

        base.RPC_OnDeath();
    }

    // ========== Debug Gizmos ==========
    private void OnDrawGizmosSelected()
    {
        // แสดงครึ่งวงกลมในพื้นที่โจมตี
        Gizmos.color = new Color(0.5f, 0.7f, 1f, 0.3f);
        Vector3 forward = transform.forward;

        // วาดครึ่งวงกลม
        for (int i = 0; i <= 16; i++)
        {
            float angle1 = Mathf.Deg2Rad * (i * attackAngle / 16f - attackAngle / 2f);
            float angle2 = Mathf.Deg2Rad * ((i + 1) * attackAngle / 16f - attackAngle / 2f);

            Vector3 dir1 = Quaternion.Euler(0, Mathf.Rad2Deg * angle1, 0) * forward;
            Vector3 dir2 = Quaternion.Euler(0, Mathf.Rad2Deg * angle2, 0) * forward;

            Vector3 point1 = transform.position + dir1 * attackRadius;
            Vector3 point2 = transform.position + dir2 * attackRadius;

            Gizmos.DrawLine(transform.position, point1);
            Gizmos.DrawLine(point1, point2);
        }
    }
}