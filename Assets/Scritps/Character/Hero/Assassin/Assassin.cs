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
        AttackType = AttackType.Physical;
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
        // ✅ คำนวณ damage ใหม่
        int directDamage = GetScaledSkillDamage(0.6f); // 60% ของ base formula
        int poisonDamage = GetScaledPoisonDamage(0.3f); // 30% ของ base formula

        // ทำ direct magic damage ก่อน
        enemy.TakeDamageFromAttacker(0, directDamage, this, DamageType.Magic);

        // ✅ ใส่ status effects
        enemy.ApplyStatusEffect(StatusEffectType.Poison, poisonDamage, 8f);
        enemy.ApplyStatusEffect(StatusEffectType.Weakness, 0, 6f, 0.3f);
        enemy.ApplyStatusEffect(StatusEffectType.Blind, 0, 4f, 0.6f);

        // Passive: โอกาส spread poison
        if (hasVenomMastery && Random.Range(0f, 100f) < 25f)
        {
            SpreadPoisonToNearby(enemy, 4f);
        }

        Debug.Log($"🌫️ Toxic Dash hit {enemy.CharacterName}! Direct: {directDamage}, Poison: {poisonDamage}/s");
    }

    // ========== 💣 Skill 3: Toxic Bomb ==========
    protected override void TryUseSkill3()
    {
        if (!CanUseSkill(skill3ManaCost)) return;

        Collider[] enemies = Physics.OverlapSphere(transform.position, 8f, LayerMask.GetMask("Enemy"));
        Character targetEnemy = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider col in enemies)
        {
            Character enemy = col.GetComponent<Character>();
            if (enemy != null)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    targetEnemy = enemy;
                }
            }
        }

        if (targetEnemy == null)
        {
            Debug.Log("❌ No target found for Shadow Assassination!");
            return;
        }

        UseMana(skill3ManaCost);

        // ✅ ให้ Critical Aura เพื่อรองรับ Physical Build
        if (statusEffectManager != null)
        {
            statusEffectManager.ApplyCriticalAura(6f, 0.4f, 12f);
            Debug.Log($"✅ Applied Critical Aura (+40% for 12s)");
        }

        // ✅ เปลี่ยนจาก GameObject เป็น NetworkObject
        NetworkObject targetNetworkObject = targetEnemy.GetComponent<NetworkObject>();
        if (targetNetworkObject != null)
        {
            RPC_PerformShadowAssassination(targetEnemy.transform.position, targetNetworkObject);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_PerformShadowAssassination(Vector3 targetPosition, NetworkObject targetObject)
    {
        StartCoroutine(ExecuteShadowAssassination(targetPosition, targetObject));
    }

    // ✅ แก้ไข parameter type
    private IEnumerator ExecuteShadowAssassination(Vector3 targetPosition, NetworkObject targetObject)
    {
        Vector3 originalPosition = transform.position;

        // คำนวณตำแหน่งหลังเป้าหมาย
        Vector3 backPosition = targetPosition + (targetPosition - transform.position).normalized * -2f;
        backPosition.y = targetPosition.y;

        // Phase 1: เทเลพอร์ตไปหลังเป้าหมาย
        Debug.Log($"🗡️ [Shadow Assassination] {CharacterName} teleporting behind target!");

        // ซ่อนตัว
        GetComponent<Renderer>().enabled = false;
        yield return new WaitForSeconds(0.2f);

        // ปรากฏที่ตำแหน่งใหม่
        transform.position = backPosition;
        GetComponent<Renderer>().enabled = true;

        // หันหน้าไปหาเป้าหมาย
        Vector3 lookDirection = (targetPosition - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(lookDirection);

        yield return new WaitForSeconds(0.1f);

        // ✅ Phase 2: โจมตี Execution
        Character targetCharacter = targetObject?.GetComponent<Character>();
        if (targetCharacter != null)
        {
            PerformExecutionAttack(targetCharacter);
        }

        Debug.Log($"🗡️ [Shadow Assassination] {CharacterName} completed assassination!");
    }
    private int CalculateExecutionDamage()
    {
        // ✅ Base damage formula: (MagicDamage × 0.7) + (AttackDamage × 0.3) + (Level × 10)
        int magicPortion = Mathf.RoundToInt(MagicDamage * 0.7f);
        int physicalPortion = Mathf.RoundToInt(AttackDamage * 0.3f);
        int levelBonus = GetCurrentLevel() * 10;

        int totalDamage = magicPortion + physicalPortion + levelBonus;

        Debug.Log($"🗡️ [Execution Damage] Magic: {magicPortion} + Physical: {physicalPortion} + Level: {levelBonus} = {totalDamage}");

        return totalDamage;
    }
    private void PerformExecutionAttack(Character target)
    {
        // คำนวณ base damage
        int baseDamage = CalculateExecutionDamage();

        // เช็ค execution bonus
        bool isExecutionRange = target.GetHealthPercentage() < 0.3f;

        if (isExecutionRange)
        {
            baseDamage = Mathf.RoundToInt(baseDamage * 1.5f);
            Debug.Log($"🩸 [Execution] Target HP < 30%! Damage increased by 50%!");
        }

        // ✅ เปลี่ยนจาก UseAdvancedSkillOnTarget เป็น TakeDamageFromAttacker โดยตรง
        int physicalDamage = Mathf.RoundToInt(baseDamage * 0.3f);
        int magicDamage = Mathf.RoundToInt(baseDamage * 0.7f);

        target.TakeDamageFromAttacker(physicalDamage, magicDamage, this, DamageType.Critical);

        // เพิ่ม effect ตาม build
        if (MagicDamage > AttackDamage) // Magic Build
        {
            int poisonDamage = GetScaledPoisonDamage(0.6f); // ✅ เพิ่ม f suffix
            target.ApplyStatusEffect(StatusEffectType.Poison, poisonDamage, 10f);

            // Venom Mastery: spread poison
            if (hasVenomMastery)
            {
                SpreadPoisonToNearby(target, 6f);
            }

            Debug.Log($"🐍 [Magic Build] Applied strong poison to {target.CharacterName}!");
        }
        else // Physical Build
        {
            // ใส่ bleeding effect
            target.ApplyStatusEffect(StatusEffectType.Bleed, 8, 8f);
            target.ApplyStatusEffect(StatusEffectType.ArmorBreak, 0, 10f, 0.5f);

            Debug.Log($"⚔️ [Physical Build] Applied bleeding + armor break to {target.CharacterName}!");
        }

        Debug.Log($"🗡️ Shadow Assassination dealt {baseDamage} damage to {target.CharacterName} (Execution: {isExecutionRange})");
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

        float duration = 20f;
        float tickInterval = 1f;
        float nextTick = 0f;
        float elapsed = 0f;

        // ✅ Super Poison damage ใหม่
        int superPoisonDamage = GetScaledPoisonDamage(0.8f); // 80% ของ base formula

        IsInPlagueCloud = true;
        PlagueCloudEndTime = Time.time + duration;

        Debug.Log($"☠️ [Plague Outbreak] {CharacterName} creates massive poison cloud! ({superPoisonDamage} damage/s)");

        // ✅ ให้ team auras
        if (statusEffectManager != null)
        {
            statusEffectManager.ApplyAttackSpeedAura(12f, 0.5f, 20f);
            statusEffectManager.ApplyDamageAura(12f, 0.4f, 20f);
            statusEffectManager.ApplyProtectionAura(12f, 0.25f, 20f);
            statusEffectManager.ApplyCriticalAura(12f, 0.3f, 20f);

            Debug.Log($"💚 [Plague Outbreak] Team Auras activated!");
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= nextTick)
            {
                // ✅ ผลกระทบต่อศัตรู
                Collider[] enemies = Physics.OverlapSphere(position, 12f, LayerMask.GetMask("Enemy"));
                foreach (Collider col in enemies)
                {
                    Character enemy = col.GetComponent<Character>();
                    if (enemy != null)
                    {
                        // ✅ Super Poison ใช้สูตรใหม่
                        enemy.ApplyStatusEffect(StatusEffectType.Poison, superPoisonDamage, 5f);
                        enemy.ApplyStatusEffect(StatusEffectType.Blind, 0, 8f, 0.8f);
                        enemy.ApplyStatusEffect(StatusEffectType.Weakness, 0, 8f, 0.5f);
                        enemy.ApplyStatusEffect(StatusEffectType.ArmorBreak, 0, 8f, 0.6f);

                        // ✅ เพิ่ม: direct magic damage ทุกวินาที
                        int directDamage = GetScaledSkillDamage(0.3f); // 30% ของ base formula
                        enemy.TakeDamageFromAttacker(0, directDamage, this, DamageType.Magic);

                        // ✅ โอกาส stun
                        if (Random.Range(0f, 100f) < 20f)
                        {
                            enemy.ApplyStatusEffect(StatusEffectType.Stun, 0, 2f);
                            Debug.Log($"☠️ {enemy.CharacterName} stunned by plague cloud!");
                        }

                        Debug.Log($"☠️ Plague Cloud: {superPoisonDamage} poison + {directDamage} direct damage to {enemy.CharacterName}");
                    }
                }

                nextTick += tickInterval;
            }

            yield return null;
        }

        IsInPlagueCloud = false;
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
            if (enemy != null && enemy != sourceEnemy && spreadCount < 3)
            {
                // ✅ ใช้สูตรใหม่
                int spreadDamage = GetScaledPoisonDamage(0.2f); // 20% ของ base formula
                enemy.ApplyStatusEffect(StatusEffectType.Poison, spreadDamage, 5f);

                // โอกาสใส่ weakness
                if (Random.Range(0f, 100f) < 50f)
                {
                    enemy.ApplyStatusEffect(StatusEffectType.Weakness, 0, 4f, 0.2f);
                }

                spreadCount++;
                Debug.Log($"🐍 [Venom Mastery] Poison spread: {spreadDamage}/s to {enemy.CharacterName}!");
            }
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

    // ใน RPC_PerformAssassinAttack method
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_PerformAssassinAttack(NetworkObject enemyObject, bool shouldPoison, bool forceCritical)
    {
        if (enemyObject != null)
        {
            Character enemy = enemyObject.GetComponent<Character>();
            if (enemy != null)
            {
                // ✅ ทำ damage ปกติ (ใช้ระบบเดิม)
                enemy.TakeDamageFromAttacker(AttackDamage, this, DamageType.Normal);

                // ✅ ใส่ poison ถ้ามี Poison Infusion - ใช้สูตรใหม่
                if (shouldPoison)
                {
                    int poisonDamage = GetScaledPoisonDamage(0.4f); // 40% ของ base formula
                    enemy.ApplyStatusEffect(StatusEffectType.Poison, poisonDamage, 8f);

                    // ✅ Venom Mastery: bonus damage ใช้สูตรใหม่
                    if (hasVenomMastery)
                    {
                        int bonusDamage = GetScaledSkillDamage(0.2f); // 20% ของ base formula
                        enemy.TakeDamageFromAttacker(0, bonusDamage, this, DamageType.Magic);
                        Debug.Log($"🐍 [Venom Mastery] poison bonus: {bonusDamage}");
                    }

                    // ✅ Passive: spread poison ใช้สูตรใหม่
                    if (hasVenomMastery && Random.Range(0f, 100f) < 30f)
                    {
                        SpreadPoisonToNearby(enemy, 4f);
                    }

                    Debug.Log($"🐍 Applied poison: {poisonDamage} damage/s for 8s");
                }

                // ✅ Force critical ใช้สูตรใหม่
                if (forceCritical)
                {
                    int critDamage = GetScaledSkillDamage(0.8f); // 80% ของ base formula
                    enemy.TakeDamageFromAttacker(0, critDamage, this, DamageType.Critical);
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

    private int GetScaledPoisonDamage(float multiplier)
    {
        // ✅ ใช้สูตรเดียวกับ Shadow Assassination
        int magicPortion = Mathf.RoundToInt(MagicDamage * 0.7f);
        int physicalPortion = Mathf.RoundToInt(AttackDamage * 0.3f);
        int levelBonus = GetCurrentLevel() * 10;

        int baseDamage = magicPortion + physicalPortion + levelBonus;
        int finalDamage = Mathf.RoundToInt(baseDamage * multiplier);

        Debug.Log($"🐍 [Poison Damage] Base: {baseDamage} × {multiplier} = {finalDamage} (Magic: {magicPortion}, Physical: {physicalPortion}, Level: {levelBonus})");

        return Mathf.Max(1, finalDamage);
    }
    private int GetScaledSkillDamage(float multiplier)
    {
        int magicPortion = Mathf.RoundToInt(MagicDamage * 0.7f);
        int physicalPortion = Mathf.RoundToInt(AttackDamage * 0.3f);
        int levelBonus = GetCurrentLevel() * 10;

        int baseDamage = magicPortion + physicalPortion + levelBonus;
        int finalDamage = Mathf.RoundToInt(baseDamage * multiplier);

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

   
}