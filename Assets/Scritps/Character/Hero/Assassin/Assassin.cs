using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Assassin : Hero
{
    [Header("🐍 Assassin Skill Settings")]
    [SerializeField] private int skill1ManaCost = 15;
    [SerializeField] private int skill2ManaCost = 20;
    [SerializeField] private int skill3ManaCost = 25;
    [SerializeField] private int skill4ManaCost = 50;

    [Header("🎯 Skill Parameters")]
    public float dashDistance = 8f;
    public float dashSpeed = 20f;
    public GameObject toxicBombPrefab;
    public GameObject plagueCloudPrefab;

    [Header("🎨 Visual Effects")]
    public Material skillRangeIndicatorMaterial;
    private GameObject plagueRangeIndicator;
    private LineRenderer rangeCircle;

    // ========== Network Properties for Skills ==========
    [Networked] public int PoisonInfusionStacks { get; set; }
    [Networked] public bool IsInPlagueCloud { get; set; }
    [Networked] public float PlagueCloudEndTime { get; set; }

    // Passive tracking
    private bool hasVenomMastery = true; // Passive เปิดตลอด

    protected override void Start()
    {
        base.Start();

        // ปรับ cooldown สำหรับ Assassin
        skill1Cooldown = 8f;
        skill2Cooldown = 12f;
        skill3Cooldown = 15f;
        skill4Cooldown = 45f;

        // สร้าง range indicator สำหรับ ultimate
        CreateRangeIndicator();

        Debug.Log($"🐍 Assassin {CharacterName} initialized with Venom Mastery!");
    }

    // ========== 💚 Skill 1: Poison Infusion ==========
    protected override void TryUseSkill1()
    {
        if (!CanUseSkill(skill1ManaCost)) return;

        UseMana(skill1ManaCost);

        // เริ่ม Poison Infusion - ให้ 3 charges
        PoisonInfusionStacks = 3;

        // คำนวณ poison damage ตาม level และ attack damage
        int poisonDamage = GetScaledPoisonDamage(3);

        Debug.Log($"🐍 [Poison Infusion] {CharacterName} gains 3 poison-infused attacks! (Poison: {poisonDamage} damage/s)");

        // Visual effect
        RPC_ShowSkillEffect("PoisonInfusion");
    }

    // ========== 🌫️ Skill 2: Toxic Dash ==========
    protected override void TryUseSkill2()
    {
        if (!CanUseSkill(skill2ManaCost)) return;
        if (HasStatusEffect(StatusEffectType.Stun)) return;

        UseMana(skill2ManaCost);

        // หาทิศทางการ dash (ตามกล้อง)
        Vector3 dashDirection = GetDashDirection();

        RPC_PerformToxicDash(dashDirection);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_PerformToxicDash(Vector3 direction)
    {
        StartCoroutine(ExecuteToxicDash(direction));
    }

    private IEnumerator ExecuteToxicDash(Vector3 direction)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + direction * dashDistance;

        float dashTime = dashDistance / dashSpeed;
        float elapsed = 0f;

        List<Character> hitEnemies = new List<Character>();

        while (elapsed < dashTime)
        {
            elapsed += Time.fixedDeltaTime;
            float progress = elapsed / dashTime;

            // เคลื่อนที่
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);
            transform.position = currentPos;

            // เช็คศัตรูที่ผ่าน
            Collider[] enemies = Physics.OverlapSphere(currentPos, 2f, LayerMask.GetMask("Enemy"));
            foreach (Collider col in enemies)
            {
                Character enemy = col.GetComponent<Character>();
                if (enemy != null && !hitEnemies.Contains(enemy))
                {
                    hitEnemies.Add(enemy);
                    ApplyToxicDashEffects(enemy);
                }
            }

            yield return new WaitForFixedUpdate();
        }

        transform.position = endPos;
        Debug.Log($"🌫️ [Toxic Dash] {CharacterName} dashed through {hitEnemies.Count} enemies!");
    }

    private void ApplyToxicDashEffects(Character enemy)
    {
        // คำนวณ damage ตาม level
        int poisonDamage = GetScaledPoisonDamage(4);

        // ใส่ Poison + Weakness
        enemy.ApplyStatusEffect(StatusEffectType.Poison, poisonDamage, 8f);
        enemy.ApplyStatusEffect(StatusEffectType.Weakness, 0, 6f, 0.3f);

        // Passive: โอกาส spread poison
        if (hasVenomMastery && Random.Range(0f, 100f) < 25f)
        {
            SpreadPoisonToNearby(enemy, 4f);
        }

        Debug.Log($"🌫️ Toxic Dash hit {enemy.CharacterName}! Applied Poison + Weakness");
    }

    // ========== 💣 Skill 3: Toxic Bomb ==========
    protected override void TryUseSkill3()
    {
        if (!CanUseSkill(skill3ManaCost)) return;

        UseMana(skill3ManaCost);

        // หาตำแหน่งที่จะโยน bomb (ข้างหน้า 5 เมตร)
        Vector3 bombPosition = transform.position + transform.forward * 5f;

        RPC_ThrowToxicBomb(bombPosition);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ThrowToxicBomb(Vector3 position)
    {
        StartCoroutine(CreateToxicBombArea(position));
    }

    private IEnumerator CreateToxicBombArea(Vector3 position)
    {
        // สร้าง visual effect (ถ้ามี prefab)
        GameObject bombEffect = null;
        if (toxicBombPrefab != null)
        {
            bombEffect = Instantiate(toxicBombPrefab, position, Quaternion.identity);
        }

        float duration = 8f;
        float tickInterval = 1f;
        float nextTick = 0f;
        float elapsed = 0f;

        int poisonDamage = GetScaledPoisonDamage(2);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= nextTick)
            {
                // หาศัตรูในพื้นที่
                Collider[] enemies = Physics.OverlapSphere(position, 4f, LayerMask.GetMask("Enemy"));
                foreach (Collider col in enemies)
                {
                    Character enemy = col.GetComponent<Character>();
                    if (enemy != null)
                    {
                        enemy.ApplyStatusEffect(StatusEffectType.Poison, poisonDamage, 3f);

                        // Passive: โอกาส spread
                        if (hasVenomMastery && Random.Range(0f, 100f) < 25f)
                        {
                            SpreadPoisonToNearby(enemy, 4f);
                        }
                    }
                }

                nextTick += tickInterval;
            }

            yield return null;
        }

        if (bombEffect != null)
        {
            Destroy(bombEffect);
        }

        Debug.Log($"💣 [Toxic Bomb] Area effect ended at {position}");
    }

    // ========== ☠️ Skill 4: Plague Outbreak (Ultimate) ==========
    protected override void TryUseSkill4()
    {
        if (!CanUseSkill(skill4ManaCost)) return;

        UseMana(skill4ManaCost);

        Vector3 cloudPosition = transform.position;

        RPC_CreatePlagueOutbreak(cloudPosition);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_CreatePlagueOutbreak(Vector3 position)
    {
        // แสดง range indicator สำหรับทุกคน
        RPC_ShowRangeIndicatorAll(position, 12f);

        StartCoroutine(ExecutePlagueOutbreak(position));
    }

    private IEnumerator ExecutePlagueOutbreak(Vector3 position)
    {
        // สร้าง visual effect
        GameObject plagueEffect = null;
        if (plagueCloudPrefab != null)
        {
            plagueEffect = Instantiate(plagueCloudPrefab, position, Quaternion.identity);
        }

        float duration = 15f;
        float tickInterval = 1f;
        float nextTick = 0f;
        float elapsed = 0f;

        // Super Poison damage ตาม level
        int superPoisonDamage = GetScaledPoisonDamage(6);

        IsInPlagueCloud = true;
        PlagueCloudEndTime = Time.time + duration;

        Debug.Log($"☠️ [Plague Outbreak] {CharacterName} creates massive poison cloud! ({superPoisonDamage} damage/s)");

        // ✅ 🌟 เปลี่ยน: ใช้ระบบ Aura แทน ApplyStatusEffect
        // เรียกให้ตัวเองเป็นผู้ให้ aura
        if (statusEffectManager != null)
        {
            statusEffectManager.ApplyAttackSpeedAura(12f, 0.4f, 15f); // +40% attack speed, 12m radius, 15s
            statusEffectManager.ApplyDamageAura(12f, 0.25f, 15f);     // +25% damage, 12m radius, 15s
            statusEffectManager.ApplyProtectionAura(12f, 0.2f, 15f);  // -20% damage taken, 12m radius, 15s

            Debug.Log($"💚 [Plague Outbreak] Providing team auras: +40% attack speed, +25% damage, -20% damage taken!");
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= nextTick)
            {
                // ผลกระทบต่อศัตรู
                Collider[] enemies = Physics.OverlapSphere(position, 12f, LayerMask.GetMask("Enemy"));
                foreach (Collider col in enemies)
                {
                    Character enemy = col.GetComponent<Character>();
                    if (enemy != null)
                    {
                        // Super Poison ที่แรงกว่าปกติ
                        enemy.ApplyStatusEffect(StatusEffectType.Poison, superPoisonDamage, 4f);

                        // เมื่อศัตรูตาย poison จะกระจาย (implement ใน OnEnemyDeath)
                    }
                }

                // ✅ 🌟 เปลี่ยน: ไม่ต้องใส่ aura ให้ทีมแล้ว เพราะระบบ aura จะทำให้อัตโนมัติ
                // ทีมที่อยู่ในรัศมี 12m จะได้ buff อัตโนมัติจากระบบ aura detection

                nextTick += tickInterval;
            }

            yield return null;
        }

        IsInPlagueCloud = false;

        // ซ่อน range indicator สำหรับทุกคน
        RPC_HideRangeIndicatorAll();

        if (plagueEffect != null)
        {
            Destroy(plagueEffect);
        }

        Debug.Log($"☠️ [Plague Outbreak] Effect ended");
    }

    // ========== 🐍 Passive: Venom Mastery ==========
    private void SpreadPoisonToNearby(Character sourceEnemy, float range)
    {
        if (!hasVenomMastery) return;

        Collider[] nearbyEnemies = Physics.OverlapSphere(sourceEnemy.transform.position, range, LayerMask.GetMask("Enemy"));
        foreach (Collider col in nearbyEnemies)
        {
            Character enemy = col.GetComponent<Character>();
            if (enemy != null && enemy != sourceEnemy)
            {
                int spreadDamage = GetScaledPoisonDamage(2);
                enemy.ApplyStatusEffect(StatusEffectType.Poison, spreadDamage, 5f);
                Debug.Log($"🐍 [Venom Mastery] Poison spread to {enemy.CharacterName}!");
            }
        }
    }

    // ========== Override Attack for Poison Infusion ==========
    public override void TryAttack()
    {
        if (!HasInputAuthority || !IsSpawned) return;
        if (Time.time < nextAttackTime) return;

        // ✅ ใช้ enemyLayer จาก base class แทน hardcode
        LayerMask attackLayer = LayerMask.GetMask("Enemy");
        Collider[] enemies = Physics.OverlapSphere(transform.position, AttackRange, attackLayer);

        Debug.Log($"🐍 [Assassin Attack] Checking {enemies.Length} enemies in range {AttackRange}m");

        if (enemies.Length > 0)
        {
            NetworkEnemy nearestEnemy = null;
            float nearestDistance = float.MaxValue;

            foreach (Collider col in enemies)
            {
                NetworkEnemy enemy = col.GetComponent<NetworkEnemy>();
                if (enemy != null && enemy.IsSpawned && !enemy.IsDead)
                {
                    float distance = Vector3.Distance(transform.position, enemy.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = enemy;
                    }
                }
            }

            if (nearestEnemy != null)
            {
                Debug.Log($"🐍 [Assassin] Found target: {nearestEnemy.CharacterName} at distance {nearestDistance:F1}m");

                // เช็ค Poison Infusion
                bool shouldPoison = PoisonInfusionStacks > 0;
                bool forceCritical = PoisonInfusionStacks == 1; // ครั้งที่ 3 (เหลือ 1 stack)

                // ✅ ใช้ RPC เดียวกับ base class แต่เพิ่ม poison logic
                RPC_PerformAssassinAttack(nearestEnemy.Object, shouldPoison, forceCritical);

                // ลด stack
                if (PoisonInfusionStacks > 0)
                {
                    PoisonInfusionStacks--;
                    Debug.Log($"🐍 Poison Infusion: {PoisonInfusionStacks} stacks remaining");
                }

                // ✅ ใช้ GetEffectiveAttackSpeed() แต่มี fallback
                float effectiveAttackSpeed = GetEffectiveAttackSpeed();
                if (effectiveAttackSpeed <= 0) effectiveAttackSpeed = AttackSpeed; // fallback

                float finalAttackCooldown = AttackCooldown / Mathf.Max(0.1f, effectiveAttackSpeed);
                nextAttackTime = Time.time + finalAttackCooldown;

                Debug.Log($"🐍 Assassin attack executed! Speed: {effectiveAttackSpeed:F1}x, Cooldown: {finalAttackCooldown:F1}s");
            }
            else
            {
                Debug.Log($"🐍 [Assassin] No valid enemies found in {enemies.Length} colliders");
            }
        }
        else
        {
            Debug.Log($"🐍 [Assassin] No enemies in attack range {AttackRange}m");
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_PerformAssassinAttack(NetworkObject enemyObject, bool shouldPoison, bool forceCritical)
    {
        if (enemyObject != null)
        {
            Debug.Log($"🐍 [RPC_PerformAssassinAttack] Executing attack...");

            // ✅ ลองหา Component ทั้งสองแบบ
            Character enemy = enemyObject.GetComponent<Character>();
            NetworkEnemy networkEnemy = enemyObject.GetComponent<NetworkEnemy>();

            if (enemy != null)
            {
                Debug.Log($"🐍 Attacking {enemy.CharacterName} with damage {AttackDamage}");

                // ทำ damage ปกติ
                enemy.TakeDamageFromAttacker(AttackDamage, this, DamageType.Normal);

                // ใส่ poison ถ้ามี Poison Infusion
                if (shouldPoison)
                {
                    int poisonDamage = GetScaledPoisonDamage(3);
                    enemy.ApplyStatusEffect(StatusEffectType.Poison, poisonDamage, 8f);
                    Debug.Log($"🐍 Applied poison: {poisonDamage} damage/s for 8s");

                    // Passive: Venom Mastery damage bonus
                    if (hasVenomMastery)
                    {
                        int bonusDamage = Mathf.RoundToInt(AttackDamage * 0.15f);
                        enemy.TakeDamageFromAttacker(bonusDamage, this, DamageType.Normal);
                        Debug.Log($"🐍 [Venom Mastery] +15% damage bonus: {bonusDamage}");
                    }

                    // Passive: โอกาส spread poison
                    if (hasVenomMastery && Random.Range(0f, 100f) < 25f)
                    {
                        SpreadPoisonToNearby(enemy, 4f);
                    }
                }

                // Force critical ถ้าเป็นครั้งที่ 3
                if (forceCritical)
                {
                    int critDamage = Mathf.RoundToInt(AttackDamage * CriticalMultiplier);
                    enemy.TakeDamageFromAttacker(critDamage, this, DamageType.Critical);
                    Debug.Log($"🐍 [Poison Infusion] Final strike - Guaranteed Critical! ({critDamage} damage)");
                }

                RPC_OnAttackHit(enemyObject);
            }
            else if (networkEnemy != null && !networkEnemy.IsDead)
            {
                Debug.Log($"🐍 Attacking NetworkEnemy with damage {AttackDamage}");

                // Fallback สำหรับ NetworkEnemy เก่า
                networkEnemy.TakeDamageFromAttacker(AttackDamage, this, DamageType.Normal);

                // เพิ่ม poison logic สำหรับ NetworkEnemy
                if (shouldPoison)
                {
                    int poisonDamage = GetScaledPoisonDamage(3);
                    networkEnemy.ApplyStatusEffect(StatusEffectType.Poison, poisonDamage, 8f);
                    Debug.Log($"🐍 Applied poison to NetworkEnemy: {poisonDamage} damage/s");
                }

                if (forceCritical)
                {
                    int critDamage = Mathf.RoundToInt(AttackDamage * CriticalMultiplier);
                    networkEnemy.TakeDamageFromAttacker(critDamage, this, DamageType.Critical);
                    Debug.Log($"🐍 Critical strike on NetworkEnemy: {critDamage} damage");
                }

                RPC_OnAttackHit(enemyObject);
            }
            else
            {
                Debug.LogError($"🐍 [RPC_PerformAssassinAttack] No valid Character or NetworkEnemy found on {enemyObject.name}!");
            }
        }
        else
        {
            Debug.LogError($"🐍 [RPC_PerformAssassinAttack] enemyObject is null!");
        }
    }

    // ========== 🎨 Visual Range Indicator System ==========
    private void CreateRangeIndicator()
    {
        // สร้าง GameObject สำหรับ range indicator
        plagueRangeIndicator = new GameObject($"{CharacterName}_PlagueRangeIndicator");
        plagueRangeIndicator.transform.SetParent(transform);

        // เพิ่ม LineRenderer
        rangeCircle = plagueRangeIndicator.AddComponent<LineRenderer>();

        // ตั้งค่า LineRenderer
        rangeCircle.material = CreateRangeIndicatorMaterial();
        rangeCircle.endColor = new Color(0.5f, 0f, 1f, 0.6f); // สีม่วงโปร่งใส
        rangeCircle.startWidth = 0.2f;
        rangeCircle.endWidth = 0.2f;
        rangeCircle.useWorldSpace = true;
        rangeCircle.loop = true;
        rangeCircle.positionCount = 64; // จำนวนจุดสำหรับวงกลม

        // ซ่อนไว้ตอนเริ่ม
        plagueRangeIndicator.SetActive(false);
    }

    private Material CreateRangeIndicatorMaterial()
    {
        // สร้าง material สำหรับ range indicator
        if (skillRangeIndicatorMaterial != null)
        {
            return skillRangeIndicatorMaterial;
        }

        // สร้าง default material
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(0.5f, 0f, 1f, 0.6f); // สีม่วงโปร่งใส
        return mat;
    }

    private void ShowRangeIndicator(Vector3 center, float radius)
    {
        if (plagueRangeIndicator == null || rangeCircle == null) return;

        // เปิดใช้งาน indicator
        plagueRangeIndicator.SetActive(true);

        // สร้างจุดสำหรับวงกลม
        Vector3[] circlePoints = new Vector3[rangeCircle.positionCount];
        float angleStep = 2f * Mathf.PI / rangeCircle.positionCount;

        for (int i = 0; i < rangeCircle.positionCount; i++)
        {
            float angle = i * angleStep;
            float x = center.x + Mathf.Cos(angle) * radius;
            float z = center.z + Mathf.Sin(angle) * radius;
            float y = center.y + 0.1f; // ยกสูงเล็กน้อยเพื่อไม่ให้ติดพื้น

            circlePoints[i] = new Vector3(x, y, z);
        }

        rangeCircle.SetPositions(circlePoints);

        // เริ่ม animation effect
        StartCoroutine(AnimateRangeIndicator());

        Debug.Log($"🎨 [Range Indicator] Showing plague range: {radius}m");
    }

    private void HideRangeIndicator()
    {
        if (plagueRangeIndicator != null)
        {
            plagueRangeIndicator.SetActive(false);
            Debug.Log($"🎨 [Range Indicator] Hidden");
        }
    }

    private IEnumerator AnimateRangeIndicator()
    {
        if (rangeCircle == null) yield break;

        float animationTime = 0f;
        Color originalColor = rangeCircle.endColor;

        while (plagueRangeIndicator.activeInHierarchy)
        {
            animationTime += Time.deltaTime;

            // สร้าง pulsing effect
            float alpha = 0.3f + 0.3f * Mathf.Sin(animationTime * 3f);
            Color newColor = originalColor;
            newColor.a = alpha;
            rangeCircle.endColor = newColor;

            yield return null;
        }

        // รีเซ็ตสีเมื่อเสร็จสิ้น
        rangeCircle.endColor = originalColor;
    }

    // ========== Helper Methods ==========
    private bool CanUseSkill(int manaCost)
    {
        if (CurrentMana < manaCost)
        {
            Debug.Log($"❌ Not enough mana! Need {manaCost}, have {CurrentMana}");
            return false;
        }

        if (HasStatusEffect(StatusEffectType.Stun))
        {
            Debug.Log($"❌ Cannot use skill while stunned!");
            return false;
        }

        return true;
    }

    private int GetScaledPoisonDamage(int baseDamage)
    {
        // ปรับตาม level (แต่ละ level +20% damage)
        int currentLevel = GetCurrentLevel();
        float levelMultiplier = 1f + ((currentLevel - 1) * 0.2f);

        // ปรับตาม attack damage (10% ของ attack damage)
        float attackBonusRatio = 0.1f;
        int attackBonus = Mathf.RoundToInt(AttackDamage * attackBonusRatio);

        int finalDamage = Mathf.RoundToInt((baseDamage + attackBonus) * levelMultiplier);

        return Mathf.Max(1, finalDamage);
    }

    private Vector3 GetDashDirection()
    {
        // ใช้ทิศทางกล้องเป็นหลัก
        if (cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            forward.y = 0;
            return forward.normalized;
        }

        // fallback ใช้ forward ของตัวละคร
        return transform.forward;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowRangeIndicatorAll(Vector3 center, float radius)
    {
        ShowRangeIndicator(center, radius);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_HideRangeIndicatorAll()
    {
        HideRangeIndicator();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowSkillEffect(string effectName)
    {
        Debug.Log($"✨ [Skill Effect] {CharacterName} - {effectName}");
        // TODO: Add visual effects here
    }

    // ========== Event Handlers ==========
    protected override void OnDestroy()
    {
        base.OnDestroy();

        // ทำลาย range indicator
        if (plagueRangeIndicator != null)
        {
            Destroy(plagueRangeIndicator);
        }

        // Subscribe to enemy death events for poison spreading
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        // Update plague cloud status
        if (IsInPlagueCloud && Time.time > PlagueCloudEndTime)
        {
            IsInPlagueCloud = false;
        }
    }

    // ========== Debug Methods ==========
    private void OnDrawGizmosSelected()
    {
        // แสดง attack range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, AttackRange);

        // แสดง dash range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, dashDistance);

        // แสดง plague cloud range
        if (IsInPlagueCloud)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, 12f);
        }

        // แสดงศัตรูในรัศมี
        Collider[] enemies = Physics.OverlapSphere(transform.position, AttackRange, LayerMask.GetMask("Enemy"));
        Gizmos.color = Color.red;
        foreach (Collider col in enemies)
        {
            Gizmos.DrawLine(transform.position, col.transform.position);
        }
    }

    [ContextMenu("Debug Assassin Attack")]
    private void DebugAssassinAttack()
    {
        Debug.Log($"=== 🐍 ASSASSIN DEBUG ===");
        Debug.Log($"AttackRange: {AttackRange}");
        Debug.Log($"AttackDamage: {AttackDamage}");
        Debug.Log($"AttackSpeed: {AttackSpeed}");
        Debug.Log($"HasInputAuthority: {HasInputAuthority}");
        Debug.Log($"IsSpawned: {IsSpawned}");
        Debug.Log($"Time.time: {Time.time:F2}, nextAttackTime: {nextAttackTime:F2}");
        Debug.Log($"PoisonInfusionStacks: {PoisonInfusionStacks}");

        // ตรวจสอบศัตรูในรัศมี
        Collider[] enemies = Physics.OverlapSphere(transform.position, AttackRange, LayerMask.GetMask("Enemy"));
        Debug.Log($"Enemies in range: {enemies.Length}");

        foreach (Collider col in enemies)
        {
            NetworkEnemy enemy = col.GetComponent<NetworkEnemy>();
            if (enemy != null)
            {
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                Debug.Log($"  - {enemy.CharacterName}: {dist:F1}m, IsSpawned: {enemy.IsSpawned}, IsDead: {enemy.IsDead}");
            }
        }
        Debug.Log($"========================");
    }
}