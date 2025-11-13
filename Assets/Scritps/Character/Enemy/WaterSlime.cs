using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class WaterSlime : NetworkEnemy
{
    [Header("🌊 Water Slime Healing Settings")]
    [SerializeField] private float healingRadius = 10f;          // รัศมีการรักษา
    [SerializeField] private float healingCooldown = 12f;        // ความถี่การรักษา
    [SerializeField] private float telegraphDuration = 1.8f;     // เวลาแสดง telegraph
    [SerializeField] private float healingPercentage = 0.2f;     // รักษา 20% ของ Max HP
    [SerializeField] private bool removeDebuffs = true;          // ลบ Debuff หรือไม่

    [Header("🎨 Visual Effects")]
    [SerializeField] private GameObject healingTelegraphPrefab;  // Prefab วงกลมสีฟ้า
    [SerializeField] private ParticleSystem healingWaveEffect;   // Particle คลื่นน้ำ
    [SerializeField] private Color telegraphColor = new Color(0.3f, 0.7f, 1f, 0.5f); // สีฟ้าใส
    [SerializeField] private float telegraphYOffset = -1.16f;

    [Header("🎯 Movement Behavior")]
    [SerializeField] private float keepAwayDistance = 8f;        // ระยะห่างขั้นต่ำจากผู้เล่น
    [SerializeField] private float patrolRadius = 12f;           // รัศมีการเดินวน
    [SerializeField] private float retreatSpeed = 1.2f;          // ความเร็วตอนถอย

    // Private variables
    private float nextHealTime = 0f;
    private bool isHealing = false;
    private GameObject currentTelegraph;
    private Vector3 patrolCenter;
    private Vector3 currentPatrolTarget;
    private float circleAngle = 0f;
    private float circleSpeed = 2f; // ความเร็วหมุนรอบ (รอบต่อวินาที)
    private float circleDistance = 8f; // ระยะห่างจากผู้เล่น
    [Header("🎬 Animation")]
    private Animator animator;
    private const string ANIM_ATTACK = "attack"; // ใช้ animation attack สำหรับ heal

    protected override void Start()
    {
        base.Start();
        SetupWaterSlimeStats();
        patrolCenter = transform.position;
        GenerateNewPatrolTarget();

        // Get Animator
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning($"⚠️ Animator not found on {CharacterName}!");
        }

        Debug.Log($"🌊 {CharacterName} spawned!");
    }

    private void SetupWaterSlimeStats()
    {
        CharacterName = "Water Slime";
        AttackType = AttackType.Magic;

        // Water Slime stats - ตายง่าย
        MaxHp = 100;
        CurrentHp = MaxHp;
        Armor = 5;
        MagicArmor = 5;
        MoveSpeed = 3.5f;
        AttackRange = 0f; // ไม่โจมตี
        detectRange = 15f;

        // เปลี่ยนสี
        
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (HasStateAuthority && !IsDead)
        {
            ProcessWaterSlimeAI();
        }
    }

    // ========== AI System ==========
    // ========== แก้ไข ProcessWaterSlimeAI ==========
    private void ProcessWaterSlimeAI()
    {
        if (isHealing) return;

        FindNearestPlayer();

        // ✅ เช็คว่ามีศัตรูที่ต้องการ Heal หรือไม่
        Character injuredAlly = FindInjuredAlly();

        if (injuredAlly != null)
        {
            float distanceToAlly = Vector3.Distance(transform.position, injuredAlly.transform.position);

            // ✅ ถ้าอยู่ในระยะ Heal แล้ว ให้ Heal
            if (distanceToAlly <= healingRadius && Runner.SimulationTime >= nextHealTime)
            {
                StartCoroutine(PerformHealingWave());
            }
            // ✅ ถ้ายังไม่ถึงระยะ ให้เดินเข้าไปใกล้
            else if (distanceToAlly > healingRadius)
            {
                MoveTowardsAlly(injuredAlly);
            }
            else
            {
                // รอ cooldown - วนรอบผู้เล่น
                CircleAroundPlayer();
            }
        }
        else
        {
            // ✅ ไม่มีใครต้องการ Heal - วนรอบผู้เล่นแทนการ patrol
            if (targetTransform != null)
            {
                CircleAroundPlayer();
            }
            else
            {
                // ไม่มีผู้เล่น - เดินวนรอบปกติ
                ProcessMovement();
            }
        }
    }

    // ========== เพิ่ม Method ใหม่: หาเพื่อนที่บาดเจ็บ ==========
    private Character FindInjuredAlly()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, detectRange, LayerMask.GetMask("Enemy"));

        Character mostInjured = null;
        float lowestHPPercent = 0.8f; // หาคนที่ HP < 80%

        foreach (Collider col in enemies)
        {
            Character enemy = col.GetComponent<Character>();
            if (enemy != null && enemy != this )
            {
                float hpPercent = enemy.GetHealthPercentage();
                if (hpPercent < lowestHPPercent)
                {
                    lowestHPPercent = hpPercent;
                    mostInjured = enemy;
                }
            }
        }

        return mostInjured;
    }

    // ========== เพิ่ม Method ใหม่: เดินเข้าไปหาเพื่อน ==========
    private void MoveTowardsAlly(Character ally)
    {
        if (ally == null) return;

        // คำนวณทิศทางไปหาเพื่อน
        Vector3 direction = (ally.transform.position - transform.position).normalized;
        direction.y = 0;

        // ✅ เช็คว่ามีผู้เล่นขวางทางหรือไม่
        if (targetTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, targetTransform.position);

            // ถ้าผู้เล่นใกล้เกินไป ให้หาทางเลี่ยว
            if (distanceToPlayer < keepAwayDistance)
            {
                // หาทางเลี่ยวโดยเดินวงรอบผู้เล่น
                Vector3 perpendicular = new Vector3(-direction.z, 0, direction.x); // หมุน 90 องศา
                direction = (direction + perpendicular * 0.5f).normalized;
            }
        }

        // เช็คกำแพง
        RaycastHit wallHit;
        float moveDistance = MoveSpeed * Runner.DeltaTime;

        if (!Physics.Raycast(transform.position, direction, out wallHit, moveDistance,
            LayerMask.GetMask("Wall", "Obstacle", "Default")))
        {
            Vector3 newPosition = transform.position + direction * moveDistance;

            if (rb != null)
            {
                rb.MovePosition(newPosition);
            }

            NetworkedPosition = newPosition;
            FlipCharacterTowardsMovement(direction);
        }
        else
        {
            // ถ้าเจอกำแพง ให้หาทางเลี่ยว
            Vector3 avoidDirection = FindAlternativeDirection(direction);
            if (avoidDirection != Vector3.zero)
            {
                Vector3 newPosition = transform.position + avoidDirection * moveDistance;

                if (rb != null)
                {
                    rb.MovePosition(newPosition);
                }

                NetworkedPosition = newPosition;
                FlipCharacterTowardsMovement(avoidDirection);
            }
        }
    }

    // ========== เพิ่ม Method: หาทางเลี่ยวกำแพง ==========
    private Vector3 FindAlternativeDirection(Vector3 blockedDirection)
    {
        float[] angles = { 45f, -45f, 90f, -90f, 135f, -135f };

        foreach (float angle in angles)
        {
            Vector3 altDir = Quaternion.Euler(0, angle, 0) * blockedDirection;

            RaycastHit hit;
            if (!Physics.Raycast(transform.position, altDir.normalized, out hit, MoveSpeed * Runner.DeltaTime * 2f,
                LayerMask.GetMask("Wall", "Obstacle", "Default")))
            {
                return altDir.normalized;
            }
        }

        return Vector3.zero;
    }

    // ========== Movement System ==========
    private void ProcessMovement()
    {
        if (targetTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, targetTransform.position);

            // ถ้าผู้เล่นใกล้เกินไป ให้ค่อยๆ ถอยออกไป
            if (distanceToPlayer < keepAwayDistance)
            {
                RetreatFromPlayer();
                return;
            }

            // ✅ ถ้าอยู่ในระยะปานกลาง ให้วนรอบ
            if (distanceToPlayer < detectRange)
            {
                CircleAroundPlayer();
                return;
            }
        }

        // ไม่มีผู้เล่นใกล้ - เดินวนรอบ Patrol
        PatrolMovement();
    }

    private void RetreatFromPlayer()
    {
        if (targetTransform == null) return;

        // คำนวณทิศทางถอยหลัง
        Vector3 directionAway = (transform.position - targetTransform.position).normalized;
        directionAway.y = 0;

        // เช็คกำแพง
        RaycastHit wallHit;
        float retreatDistance = retreatSpeed * Runner.DeltaTime;

        if (!Physics.Raycast(transform.position, directionAway, out wallHit, retreatDistance,
            LayerMask.GetMask("Wall", "Obstacle", "Default")))
        {
            Vector3 newPosition = transform.position + directionAway * retreatDistance;

            if (rb != null)
            {
                rb.MovePosition(newPosition);
            }

            NetworkedPosition = newPosition;

            // Flip character
            FlipCharacterTowardsMovement(directionAway);
        }
    }

    private void PatrolMovement()
    {
        float distanceToTarget = Vector3.Distance(transform.position, currentPatrolTarget);

        if (distanceToTarget <= 1f)
        {
            GenerateNewPatrolTarget();
        }

        Vector3 direction = (currentPatrolTarget - transform.position).normalized;
        direction.y = 0;

        float moveDistance = MoveSpeed * Runner.DeltaTime;
        Vector3 newPosition = transform.position + direction * moveDistance;

        if (rb != null)
        {
            rb.MovePosition(newPosition);
        }

        NetworkedPosition = newPosition;
        FlipCharacterTowardsMovement(direction);
    }

    private void GenerateNewPatrolTarget()
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        float randomDistance = Random.Range(patrolRadius * 0.5f, patrolRadius);

        currentPatrolTarget = patrolCenter + new Vector3(randomDirection.x, 0, randomDirection.y) * randomDistance;
    }

    // ========== Healing System ==========
    // ========== แก้ไข ShouldHeal - ไม่ต้องใช้แล้ว แต่เก็บไว้สำหรับเช็คเพิ่มเติม ==========
    private bool ShouldHeal()
    {
        // เช็คว่ามีศัตรูที่ต้องการ Heal ในระยะ healingRadius หรือไม่
        Collider[] enemies = Physics.OverlapSphere(transform.position, healingRadius, LayerMask.GetMask("Enemy"));

        foreach (Collider col in enemies)
        {
            Character enemy = col.GetComponent<Character>();
            if (enemy != null && enemy != this )
            {
                if (enemy.GetHealthPercentage() < 0.8f)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private IEnumerator PerformHealingWave()
    {
        isHealing = true;
        StopMovement();

        Debug.Log($"🌊 {CharacterName}: Starting Healing Wave!");

        // Telegraph
        yield return StartCoroutine(ShowHealingTelegraph());

        // Play animation
        if (animator != null)
        {
            animator.SetTrigger(ANIM_ATTACK);
        }

        yield return new WaitForSeconds(0.3f);

        // Execute healing
        ExecuteHealingWave();

        yield return new WaitForSeconds(1f);

        ResumeMovement();
        isHealing = false;
        nextHealTime = Runner.SimulationTime + healingCooldown;

        Debug.Log($"🌊 {CharacterName}: Healing Wave complete!");
    }

    private IEnumerator ShowHealingTelegraph()
    {
        Vector3 telegraphPos = new Vector3(transform.position.x, telegraphYOffset, transform.position.z);

        if (healingTelegraphPrefab != null)
        {
            currentTelegraph = Instantiate(healingTelegraphPrefab, telegraphPos, Quaternion.identity);
        }
        else
        {
            currentTelegraph = CreateProceduralHealingTelegraph(telegraphPos);
        }

        RPC_ShowTelegraph(telegraphPos);

        float elapsed = 0f;
        Vector3 startScale = new Vector3(0.1f, 0.05f, 0.1f);
        Vector3 targetScale = new Vector3(healingRadius * 2f, 0.05f, healingRadius * 2f);

        while (elapsed < telegraphDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / telegraphDuration;

            if (currentTelegraph != null)
            {
                currentTelegraph.transform.position = telegraphPos;
                currentTelegraph.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);

                // Pulse effect
                float pulse = Mathf.Sin(elapsed * 8f) * 0.15f + 0.85f;
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
    }

    private void ExecuteHealingWave()
    {
        // Show healing effect
        if (healingWaveEffect != null)
        {
            GameObject healFX = Instantiate(healingWaveEffect.gameObject, transform.position, Quaternion.identity);

            ParticleSystem ps = healFX.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = new Color(0.3f, 0.7f, 1f, 0.8f); // สีฟ้าน้ำ
                main.startLifetime = 2f;

                ParticleSystem.ShapeModule shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = healingRadius;
            }

            Destroy(healFX, 3f);
        }

        // หาศัตรูในรัศมี
        Collider[] enemies = Physics.OverlapSphere(transform.position, healingRadius, LayerMask.GetMask("Enemy"));

        int healedCount = 0;

        foreach (Collider col in enemies)
        {
            Character enemy = col.GetComponent<Character>();
            if (enemy != null && enemy != this )
            {
                // คำนวณ HP ที่จะรักษา
                int healAmount = Mathf.RoundToInt(enemy.MaxHp * healingPercentage);
                int newHP = Mathf.Min(enemy.CurrentHp + healAmount, enemy.MaxHp);
                int actualHealed = newHP - enemy.CurrentHp;

                if (actualHealed > 0)
                {
                    enemy.CurrentHp = newHP;
                    healedCount++;

                    // แสดง healing text
                    RPC_ShowHealingText(enemy.transform.position, actualHealed);

                    Debug.Log($"🌊 Healed {enemy.CharacterName} for {actualHealed} HP!");
                }

                // ลบ Debuffs
               
            }
        }

        RPC_ShowHealingEffect(transform.position);
        Debug.Log($"🌊 Healing Wave: Healed {healedCount} enemies!");
    }

    // ========== Helper Methods ==========
    private GameObject CreateProceduralHealingTelegraph(Vector3 position)
    {
        GameObject telegraph = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        telegraph.name = "WaterSlime_HealingTelegraph";
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

    protected void FlipCharacterTowardsMovement(Vector3 moveDirection)
    {
        if (moveDirection.magnitude < 0.3f) return;

        Vector3 newScale = transform.localScale;
        if (moveDirection.x > 0.3f)
        {
            newScale.x = Mathf.Abs(newScale.x);
        }
        else if (moveDirection.x < -0.3f)
        {
            newScale.x = -Mathf.Abs(newScale.x);
        }

        transform.localScale = newScale;
    }

    // ========== RPCs ==========
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowTelegraph(Vector3 position)
    {
        Debug.Log($"[Client] Showing healing telegraph at {position}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowHealingEffect(Vector3 position)
    {
        if (healingWaveEffect != null)
        {
            GameObject fx = Instantiate(healingWaveEffect.gameObject, position, Quaternion.identity);
            Destroy(fx, 3f);
        }
        Debug.Log($"[Client] Healing wave effect at {position}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowHealingText(Vector3 position, int healAmount)
    {
        // แสดง floating text "+HP"
        Color healColor = new Color(0.3f, 1f, 0.5f, 1f); // เขียวสดใส
        DamageTextManager.ShowCustomText(position + Vector3.up * 2f, $"+{healAmount} HP", healColor, 1f);
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
    private void CircleAroundPlayer()
    {
        if (targetTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, targetTransform.position);

        // คำนวณตำแหน่งเป้าหมายบนวงกลม
        circleAngle += circleSpeed * Runner.DeltaTime;
        if (circleAngle > 360f) circleAngle -= 360f;

        // หาตำแหน่งบนวงกลมรอบผู้เล่น
        float radians = circleAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(radians) * circleDistance,
            0,
            Mathf.Sin(radians) * circleDistance
        );

        Vector3 targetPosition = targetTransform.position + offset;

        // เดินไปยังตำแหน่งเป้าหมาย
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;

        // ✅ ปรับความเร็วตามระยะห่าง - ถ้าไกลมาก เดินเร็วขึ้น
        float moveSpeed = MoveSpeed;
        if (distanceToPlayer > keepAwayDistance * 2f)
        {
            moveSpeed *= 1.5f; // เดินเร็วขึ้น 50% ถ้าไกลมาก
        }

        // ✅ เช็คว่าผู้เล่นใกล้เกินไปหรือไม่
        if (distanceToPlayer < keepAwayDistance)
        {
            // ถ้าใกล้เกินไป ให้ถอยออกไปให้ห่างขึ้น
            Vector3 retreatDirection = (transform.position - targetTransform.position).normalized;
            retreatDirection.y = 0;
            direction = retreatDirection;
        }

        // เช็คกำแพง
        RaycastHit wallHit;
        float moveDistance = moveSpeed * Runner.DeltaTime;

        if (!Physics.Raycast(transform.position, direction, out wallHit, moveDistance,
            LayerMask.GetMask("Wall", "Obstacle", "Default")))
        {
            Vector3 newPosition = transform.position + direction * moveDistance;

            if (rb != null)
            {
                rb.MovePosition(newPosition);
            }

            NetworkedPosition = newPosition;
            FlipCharacterTowardsMovement(direction);
        }
        else
        {
            // ถ้าเจอกำแพง ให้หมุนไปทางอื่น
            circleAngle += 45f; // กระโดดมุม 45 องศา
            if (circleAngle > 360f) circleAngle -= 360f;
        }
    }
}