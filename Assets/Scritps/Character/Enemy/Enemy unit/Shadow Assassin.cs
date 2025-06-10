using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
public class ShadowAssassin : NetworkEnemy
{
    [Header("🥷 Shadow Assassin Settings")]
    [SerializeField] private float stealthDuration = 3f;
    [SerializeField] private float stealthCooldown = 8f;
    [SerializeField] private float nextStealthTime = 0f;
    [SerializeField] private bool isInStealth = false;
    [SerializeField] private float stealthDamageMultiplier = 2f;
    [SerializeField] private float teleportRange = 5f;
    [SerializeField] private float teleportCooldown = 6f;
    [SerializeField] private float nextTeleportTime = 0f;

    protected override void Start()
    {
        base.Start();

        // ตั้งค่าเฉพาะ Assassin - เร็ว, เลือดน้อย, ดาเมจสูง
        MoveSpeed = 4.5f;
        AttackDamage = 30;
        MaxHp = 40;
        CurrentHp = MaxHp;
        AttackRange = 1.2f;
        AttackCooldown = 1f;
        detectRange = 9f;

        Debug.Log($"🥷 Shadow Assassin spawned with {MaxHp} HP!");
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (HasStateAuthority && !IsDead && targetTransform != null)
        {
            CheckSpecialAbilities();
        }
    }

    private void CheckSpecialAbilities()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, targetTransform.position);

        // ใช้ Stealth เมื่อเลือดน้อย
        if (!isInStealth && Runner.SimulationTime >= nextStealthTime &&
            (float)CurrentHp / MaxHp <= 0.5f)
        {
            ActivateStealth();
        }

        // ใช้ Teleport เมื่ออยู่ไกลหรือติดกับ
        if (Runner.SimulationTime >= nextTeleportTime &&
            (distanceToPlayer > teleportRange * 1.5f || distanceToPlayer < AttackRange * 0.5f))
        {
            TeleportToPlayer();
        }
    }

    private void ActivateStealth()
    {
        isInStealth = true;
        nextStealthTime = Runner.SimulationTime + stealthCooldown;

        RPC_ActivateStealth();
        StartCoroutine(StealthDurationCoroutine());

    }

    private IEnumerator StealthDurationCoroutine()
    {
        yield return new WaitForSeconds(stealthDuration);

        isInStealth = false;
        RPC_DeactivateStealth();

        Debug.Log($"🥷 {CharacterName} exits stealth mode!");
    }

    private void TeleportToPlayer()
    {
        if (targetTransform == null) return;

        nextTeleportTime = Runner.SimulationTime + teleportCooldown;

        // หาตำแหน่งข้างหลังผู้เล่น
        Vector3 playerDirection = targetTransform.forward;
        Vector3 teleportPos = targetTransform.position - playerDirection * 2f;

        // ตรวจสอบว่าตำแหน่งปลอดภัย
        if (Physics.CheckSphere(teleportPos, 0.5f) == false)
        {
            Vector3 oldPos = transform.position;
            transform.position = teleportPos;

            RPC_TeleportEffect(oldPos, teleportPos);

            Debug.Log($"🥷 {CharacterName} teleports behind player!");
        }
    }

    protected override void TryAttackTarget()
    {
        if (targetTransform == null) return;

        float distance = Vector3.Distance(transform.position, targetTransform.position);

        if (distance <= AttackRange && Runner.SimulationTime >= nextAttackTime)
        {
            nextAttackTime = Runner.SimulationTime + AttackCooldown;

            int finalDamage = AttackDamage;

            // เพิ่มดาเมจถ้าอยู่ใน stealth
            if (isInStealth)
            {
                finalDamage = Mathf.RoundToInt(AttackDamage * stealthDamageMultiplier);
                isInStealth = false; // หลุด stealth หลังโจมตี
                RPC_DeactivateStealth();
                Debug.Log($"🥷 Stealth attack! Damage increased to {finalDamage}!");
            }

            Hero hero = targetTransform.GetComponent<Hero>();
            if (hero != null)
            {
                hero.TakeDamageFromAttacker(finalDamage, this, DamageType.Normal);

                // มีโอกาสใส่ Bleed
                if (Random.Range(0f, 100f) <= 40f)
                {
                    hero.ApplyStatusEffect(StatusEffectType.Bleed, 5, 6f);
                    Debug.Log("🥷 Applied Bleed effect!");
                }
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ActivateStealth()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Color color = renderer.material.color;
            color.a = 0.3f; // กลายเป็นโปร่งใส
            renderer.material.color = color;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_DeactivateStealth()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Color color = renderer.material.color;
            color.a = 1f; // กลับมาทึบ
            renderer.material.color = color;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TeleportEffect(Vector3 fromPos, Vector3 toPos)
    {
        Debug.Log($"✨ Teleport from {fromPos} to {toPos}");
        // เพิ่ม teleport visual effect
    }
}
