using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class WaterSlime : NetworkEnemy
{
    [Header("🌊 Water Slime Healing Settings")]
    [SerializeField] private float healingRadius = 6f;           // รัศมีการรักษา
    [SerializeField] private float healingCooldown = 12f;        // ความถี่การรักษา
    [SerializeField] private float telegraphDuration = 1.8f;     // เวลาแสดง telegraph
    [SerializeField] private float healingPercentage = 0.2f;     // รักษา 20% ของ Max HP
    [SerializeField] private float postHealCooldown = 1.5f;      // เวลา cooldown หลัง heal

    [Header("🎨 Visual Effects")]
    [SerializeField] private GameObject healingTelegraphPrefab;  // Prefab วงกลมสีฟ้า
    [SerializeField] private ParticleSystem healingWaveEffect;   // Particle คลื่นน้ำ
    [SerializeField] private Color telegraphColor = new Color(0.3f, 0.7f, 1f, 0.5f);
    [SerializeField] private float telegraphYOffset = -1.16f;

    [Header("🎯 Heal Behavior")]
    [SerializeField] private float stopDistance = 8f;            // ระยะที่จะหยุดเพื่อ heal

    [Header("🎬 Animation")]
    private Animator animator;
    private const string ANIM_ATTACK = "attack";

    // Private variables
    private float nextHealTime = 0f;
    private bool isHealing = false;
    private GameObject currentTelegraph;
    private Vector3 originalPosition;
    private bool wasMovementEnabled = true;

    private enum HealPhase
    {
        None,
        Telegraph,
        Execute,
        Cooldown
    }
    private HealPhase currentPhase = HealPhase.None;

    protected override void Start()
    {
        base.Start();
        SetupWaterSlimeStats();
        originalPosition = transform.position;

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

        MaxHp = 100;
        CurrentHp = MaxHp;
        Armor = 5;
        MagicArmor = 5;
        MoveSpeed = 3f;
        AttackRange = 0f;
        detectRange = 15f;
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (HasStateAuthority && !IsDead)
        {
            ProcessWaterSlimeAI();
        }
    }

    // ========== AI System - เหมือน FireSlime ==========
    private void ProcessWaterSlimeAI()
    {
        if (isHealing) return;

        // หา target
        if (targetTransform == null)
        {
            FindNearestPlayer();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, targetTransform.position);

        // ✅ ถ้าอยู่ในระยะ heal และพร้อมใช้สกิล → Heal
        if (distanceToPlayer <= stopDistance && Runner.SimulationTime >= nextHealTime)
        {
            StartCoroutine(PerformHealingSequence());
        }
    }

    // ========== Healing Sequence - เหมือน FireSlime ==========
    private IEnumerator PerformHealingSequence()
    {
        isHealing = true;
        currentPhase = HealPhase.Telegraph;

        // ✅ หยุดการเคลื่อนที่
        StopMovement();
        originalPosition = transform.position;

        // 🔵 Phase 1: Telegraph
        Debug.Log($"🌊 {CharacterName}: Telegraph Phase");
        yield return StartCoroutine(ShowHealingTelegraph());

        // 🌊 Phase 2: Execute
        currentPhase = HealPhase.Execute;
        Debug.Log($"🌊 {CharacterName}: Execute Healing!");

        // Play animation
        if (animator != null)
        {
            animator.SetTrigger(ANIM_ATTACK);
        }

        yield return new WaitForSeconds(0.3f);

        ExecuteHealingWave();

        // 💤 Phase 3: Cooldown
        currentPhase = HealPhase.Cooldown;
        Debug.Log($"🌊 {CharacterName}: Cooldown Phase");
        yield return new WaitForSeconds(postHealCooldown);

        // ✅ เปิดการเคลื่อนที่ใหม่
        ResumeMovement();

        // รีเซ็ต
        isHealing = false;
        currentPhase = HealPhase.None;
        nextHealTime = Runner.SimulationTime + healingCooldown;

        Debug.Log($"🌊 {CharacterName}: Ready for next heal");
    }

    // ========== Movement Control ==========
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

        Debug.Log($"🌊 {CharacterName}: Movement STOPPED");
    }

    private void ResumeMovement()
    {
        if (wasMovementEnabled)
        {
            if (characterStats != null)
            {
                MoveSpeed = characterStats.moveSpeed;
            }
        }

        Debug.Log($"🌊 {CharacterName}: Movement RESUMED");
    }

    // ========== Telegraph ==========
    private IEnumerator ShowHealingTelegraph()
    {
        Vector3 telegraphPosition = new Vector3(
            transform.position.x,
            telegraphYOffset,
            transform.position.z
        );

        if (healingTelegraphPrefab != null)
        {
            currentTelegraph = Instantiate(healingTelegraphPrefab, telegraphPosition, Quaternion.identity);
        }
        else
        {
            currentTelegraph = CreateProceduralHealingTelegraph(telegraphPosition);
        }

        RPC_ShowTelegraph(telegraphPosition);

        float elapsed = 0f;
        Vector3 startScale = new Vector3(0.1f, 0.05f, 0.1f);
        Vector3 targetScale = new Vector3(healingRadius * 2f, 0.05f, healingRadius * 2f);

        while (elapsed < telegraphDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / telegraphDuration;
            float smoothProgress = 1f - Mathf.Pow(1f - progress, 3f);

            if (currentTelegraph != null)
            {
                currentTelegraph.transform.position = new Vector3(
                    originalPosition.x,
                    telegraphYOffset,
                    originalPosition.z
                );

                currentTelegraph.transform.localScale = Vector3.Lerp(startScale, targetScale, smoothProgress);

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

    // ========== Execute Healing ==========
    private void ExecuteHealingWave()
    {
        // แสดง particle effect
        if (healingWaveEffect != null)
        {
            GameObject healFX = Instantiate(healingWaveEffect.gameObject, transform.position, Quaternion.identity);

            ParticleSystem ps = healFX.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = new Color(0.3f, 0.7f, 1f, 0.8f);
                main.startLifetime = 2f;

                ParticleSystem.ShapeModule shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = healingRadius;
            }

            Destroy(healFX, 3f);
        }

        RPC_ShowHealingEffect(transform.position);

        // ✅ หาศัตรูทั้งหมดในรัศมี (ไม่เช็ค HP)
        Collider[] enemies = Physics.OverlapSphere(transform.position, healingRadius, LayerMask.GetMask("Enemy"));

        int healedCount = 0;

        foreach (Collider col in enemies)
        {
            Character enemy = col.GetComponent<Character>();
            if (enemy != null && enemy != this )
            {
                // ✅ Heal ทุกคนในระยะ ไม่เช็ค HP
                int healAmount = Mathf.RoundToInt(enemy.MaxHp * healingPercentage);
                int newHP = Mathf.Min(enemy.CurrentHp + healAmount, enemy.MaxHp);
                int actualHealed = newHP - enemy.CurrentHp;

                if (actualHealed > 0)
                {
                    enemy.CurrentHp = newHP;
                    healedCount++;

                    RPC_ShowHealingText(enemy.transform.position, actualHealed);

                    Debug.Log($"🌊 Healed {enemy.CharacterName} for {actualHealed} HP!");
                }
            }
        }

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
        Color healColor = new Color(0.3f, 1f, 0.5f, 1f);
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
}