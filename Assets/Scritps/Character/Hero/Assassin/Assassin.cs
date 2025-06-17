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
    [SerializeField] private int skill4ManaCost = 45;

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
        skill4Cooldown = 20f;

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

        // ✅ 🌟 เปลี่ยน: ให้ Attack Speed Aura แทนการ buff ตัวเอง
        if (statusEffectManager != null)
        {
            statusEffectManager.ApplyAttackSpeedAura(6f, 0.3f, 12f); // +30% attack speed, 6m radius, 12s
            Debug.Log($"✅ Applied Attack Speed Aura (+30% for 12s in 6m radius)");
        }

        Debug.Log($"🐍 [Poison Infusion] {CharacterName} gains 3 poison-infused attacks!");

        // Visual effect
        RPC_ShowSkillEffect("PoisonInfusion");
    }

    // ========== 🌫️ Skill 2: Toxic Dash ==========
    protected override void TryUseSkill2()
    {
        if (!CanUseSkill(skill2ManaCost)) return;
        if (HasStatusEffect(StatusEffectType.Stun)) return;

        UseMana(skill2ManaCost);

        // ✅ 🌟 เปลี่ยน: ให้ Move Speed Aura ก่อน dash
        if (statusEffectManager != null)
        {
            statusEffectManager.ApplyMoveSpeedAura(5f, 0.25f, 10f); // +25% move speed, 5m radius, 10s
            Debug.Log($"✅ Applied Move Speed Aura (+25% for 10s)");
        }

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

        // ✅ ใส่ Poison + Weakness + Blind
        enemy.ApplyStatusEffect(StatusEffectType.Poison, poisonDamage, 8f);
        enemy.ApplyStatusEffect(StatusEffectType.Weakness, 0, 6f, 0.3f);   // 30% damage reduction
        enemy.ApplyStatusEffect(StatusEffectType.Blind, 0, 4f, 0.6f);      // 60% hit/crit reduction

        // Passive: โอกาส spread poison
        if (hasVenomMastery && Random.Range(0f, 100f) < 25f)
        {
            SpreadPoisonToNearby(enemy, 4f);
        }

        Debug.Log($"🌫️ Toxic Dash hit {enemy.CharacterName}! Applied Poison + Weakness + Blind");
    }

    // ========== 💣 Skill 3: Toxic Bomb ==========
    protected override void TryUseSkill3()
    {
        if (!CanUseSkill(skill3ManaCost)) return;

        UseMana(skill3ManaCost);

        // ✅ 🌟 เปลี่ยน: ให้ Damage Aura
        if (statusEffectManager != null)
        {
            statusEffectManager.ApplyDamageAura(6f, 0.35f, 15f); // +35% damage, 6m radius, 15s
            Debug.Log($"✅ Applied Damage Aura (+35% for 15s)");
        }

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

        int poisonDamage = GetScaledPoisonDamage(3); // เพิ่มขึ้น

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
                        // ✅ ใส่ Poison + Armor Break
                        enemy.ApplyStatusEffect(StatusEffectType.Poison, poisonDamage, 4f);
                        enemy.ApplyStatusEffect(StatusEffectType.ArmorBreak, 0, 6f, 0.4f); // 40% armor reduction

                        // Passive: โอกาส spread
                        if (hasVenomMastery && Random.Range(0f, 100f) < 25f)
                        {
                            SpreadPoisonToNearby(enemy, 4f);
                        }

                        Debug.Log($"💣 Toxic Bomb: Applied Poison + Armor Break to {enemy.CharacterName}");
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

        float duration = 20f; // เพิ่มระยะเวลา
        float tickInterval = 1f;
        float nextTick = 0f;
        float elapsed = 0f;

        // Super Poison damage ตาม level
        int superPoisonDamage = GetScaledPoisonDamage(8); // เพิ่มขึ้น

        IsInPlagueCloud = true;
        PlagueCloudEndTime = Time.time + duration;

        Debug.Log($"☠️ [Plague Outbreak] {CharacterName} creates massive poison cloud! ({superPoisonDamage} damage/s)");

        // ✅ 🌟 แก้ไข: ให้ Aura ทีเดียวไม่ซ้ำ
        if (statusEffectManager != null)
        {
            statusEffectManager.ApplyAttackSpeedAura(12f, 0.5f, 20f);  // +50% attack speed, 12m radius, 20s
            statusEffectManager.ApplyDamageAura(12f, 0.4f, 20f);      // +40% damage, 12m radius, 20s
            statusEffectManager.ApplyProtectionAura(12f, 0.25f, 20f); // -25% damage taken, 12m radius, 20s
            statusEffectManager.ApplyCriticalAura(12f, 0.3f, 20f);    // +30% critical chance, 12m radius, 20s

            Debug.Log($"💚 [Plague Outbreak] Team Auras: +50% attack speed, +40% damage, -25% damage taken, +30% critical!");
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= nextTick)
            {
                // ✅ ผลกระทบต่อศัตรู - ใส่ทุก debuffs
                Collider[] enemies = Physics.OverlapSphere(position, 12f, LayerMask.GetMask("Enemy"));
                foreach (Collider col in enemies)
                {
                    Character enemy = col.GetComponent<Character>();
                    if (enemy != null)
                    {
                        // ✅ Super Poison + ทุก debuffs
                        enemy.ApplyStatusEffect(StatusEffectType.Poison, superPoisonDamage, 5f);
                        enemy.ApplyStatusEffect(StatusEffectType.Blind, 0, 8f, 0.8f);        // 80% hit/crit reduction
                        enemy.ApplyStatusEffect(StatusEffectType.Weakness, 0, 8f, 0.5f);     // 50% damage reduction
                        enemy.ApplyStatusEffect(StatusEffectType.ArmorBreak, 0, 8f, 0.6f);   // 60% armor reduction

                        // ✅ เพิ่ม: โอกาส stun
                        if (Random.Range(0f, 100f) < 20f) // 20% chance
                        {
                            enemy.ApplyStatusEffect(StatusEffectType.Stun, 0, 2f);
                            Debug.Log($"☠️ {enemy.CharacterName} stunned by plague cloud!");
                        }

                        Debug.Log($"☠️ Plague Cloud: Applied ALL debuffs to {enemy.CharacterName}");
                    }
                }

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

        int spreadCount = 0;
        foreach (Collider col in nearbyEnemies)
        {
            Character enemy = col.GetComponent<Character>();
            if (enemy != null && enemy != sourceEnemy && spreadCount < 3) // จำกัดไม่เกิน 3 ตัว
            {
                int spreadDamage = GetScaledPoisonDamage(2);
                enemy.ApplyStatusEffect(StatusEffectType.Poison, spreadDamage, 5f);

                // ✅ เพิ่ม: โอกาสใส่ weakness
                if (Random.Range(0f, 100f) < 50f)
                {
                    enemy.ApplyStatusEffect(StatusEffectType.Weakness, 0, 4f, 0.2f);
                }

                spreadCount++;
                Debug.Log($"🐍 [Venom Mastery] Poison spread to {enemy.CharacterName}!");
            }
        }

        if (spreadCount > 0)
        {
            Debug.Log($"🐍 [Venom Mastery] Spread poison to {spreadCount} enemies!");
        }
    }

    // ========== Override Attack for Poison Infusion ==========
    public override void TryAttack()
    {
        if (!HasInputAuthority || !IsSpawned) return;
        if (Time.time < nextAttackTime) return;

        Collider[] enemies = Physics.OverlapSphere(transform.position, AttackRange, LayerMask.GetMask("Enemy"));

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
                // ✅ ง่ายขึ้น: ใช้ RPC เดียว
                bool hasPoisonInfusion = PoisonInfusionStacks > 0;
                bool guaranteedCrit = PoisonInfusionStacks == 1; // ครั้งสุดท้าย

                RPC_PerformAssassinAttack(nearestEnemy.Object, hasPoisonInfusion, guaranteedCrit);

                // ลด stack
                if (PoisonInfusionStacks > 0)
                {
                    PoisonInfusionStacks--;
                    Debug.Log($"🐍 Poison Infusion: {PoisonInfusionStacks} stacks remaining");
                }

                // ✅ ใช้ GetEffectiveAttackSpeed() แทน AttackSpeed
                float effectiveAttackSpeed = GetEffectiveAttackSpeed();
                float finalAttackCooldown = AttackCooldown / Mathf.Max(0.1f, effectiveAttackSpeed);
                nextAttackTime = Time.time + finalAttackCooldown;

                Debug.Log($"🐍 Assassin attack! Speed: {effectiveAttackSpeed:F1}x, Cooldown: {finalAttackCooldown:F1}s");
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_PerformAssassinAttack(NetworkObject enemyObject, bool shouldPoison, bool forceCritical)
    {
        if (enemyObject != null)
        {
            Character enemy = enemyObject.GetComponent<Character>();
            if (enemy != null)
            {
                // ✅ ทำ damage ปกติ
                enemy.TakeDamageFromAttacker(AttackDamage, this, DamageType.Normal);

                // ✅ ใส่ poison ถ้ามี Poison Infusion
                if (shouldPoison)
                {
                    int poisonDamage = GetScaledPoisonDamage(4);
                    enemy.ApplyStatusEffect(StatusEffectType.Poison, poisonDamage, 8f);

                    // ✅ Venom Mastery: +20% damage bonus
                    if (hasVenomMastery)
                    {
                        int bonusDamage = Mathf.RoundToInt(AttackDamage * 0.2f);
                        enemy.TakeDamageFromAttacker(bonusDamage, this, DamageType.Magic);
                        Debug.Log($"🐍 [Venom Mastery] +20% poison bonus: {bonusDamage}");
                    }

                    // ✅ Passive: โอกาส spread poison
                    if (hasVenomMastery && Random.Range(0f, 100f) < 30f) // เพิ่มเป็น 30%
                    {
                        SpreadPoisonToNearby(enemy, 4f);
                    }

                    Debug.Log($"🐍 Applied poison: {poisonDamage} damage/s for 8s");
                }

                // ✅ Force critical ถ้าเป็นครั้งสุดท้าย
                if (forceCritical)
                {
                    int critDamage = Mathf.RoundToInt(AttackDamage * CriticalMultiplier);
                    enemy.TakeDamageFromAttacker(critDamage, this, DamageType.Critical);
                    Debug.Log($"🐍 [Poison Infusion] Final strike - Guaranteed Critical! ({critDamage} damage)");
                }

                RPC_OnAttackHit(enemyObject);
            }
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