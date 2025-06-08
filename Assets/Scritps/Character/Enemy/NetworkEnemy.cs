using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class NetworkEnemy : Character
{
    // ========== Network Properties ==========
   
    [Networked] public NetworkBool IsDead { get; set; }
    [Networked] public PlayerRef CurrentTarget { get; set; }

    // เพิ่ม Network Position Properties เหมือน Hero
    [Networked] public Vector3 NetworkedPosition { get; set; }
    [Networked] public Vector3 NetworkedVelocity { get; set; }
    [Networked] public Vector3 NetworkedScale { get; set; }
    [Networked] public float NetworkedYRotation { get; set; }

    // ========== Enemy Properties ==========
    [Header("Enemy Settings")]
    public float detectRange = 10f;
    public float attackCheckInterval = 0.5f;

    private float nextTargetCheckTime = 0f;
    private Transform targetTransform;
    private float nextAttackTime = 0f;

    // Check if properly spawned
    public bool IsSpawned => Object != null && Object.IsValid;

    // ========== Unity Lifecycle ==========
    protected override void Start()
    {
        base.Start();
        Debug.Log($"Enemy Start - HasStateAuthority: {HasStateAuthority}");
    }

    // Called when spawned by Fusion
    public override void Spawned()
    {
        Debug.Log($"Enemy Spawned - HasStateAuthority: {HasStateAuthority}");

        // Initialize network properties only on server
        if (HasStateAuthority)
        {
            NetworkedMaxHp = MaxHp;
            NetworkedCurrentHp = CurrentHp;
            IsDead = false;
            // กำหนดค่าเริ่มต้นของ position และ scale
            NetworkedPosition = transform.position;
            NetworkedScale = transform.localScale;
            NetworkedYRotation = transform.eulerAngles.y;
        }
    }

    // ========== Network Update ==========
    public override void FixedUpdateNetwork()
    {
        // เรียก base.FixedUpdateNetwork() ก่อนเพื่อให้ระบบ poison ทำงาน
        base.FixedUpdateNetwork();

        // Safety check
        if (!IsSpawned) return;

        // Server/Host controls enemies
        if (HasStateAuthority)
        {
            if (!IsDead)
            {
                // Enemy AI logic
                FindNearestPlayer();
                MoveTowardsTarget();
                TryAttackTarget();

                // ❌ ลบออก: NetworkedCurrentHp = CurrentHp; // ใช้จาก base class แล้ว

                // Check death
                if (CurrentHp <= 0 && !IsDead)
                {
                    IsDead = true;
                    RPC_OnDeath();
                }
            }

            // อัพเดท Network Properties เหมือน Hero
            NetworkedPosition = transform.position;
            NetworkedScale = transform.localScale;
            NetworkedYRotation = transform.eulerAngles.y;
            if (rb != null)
            {
                NetworkedVelocity = rb.velocity;
            }
        }
        // Remote clients - apply network state
        else
        {
            ApplyNetworkState();
        }
    }

    // ========== Network State Application (เหมือน Hero) ==========
    protected virtual void ApplyNetworkState()
    {
        float positionDistance = Vector3.Distance(transform.position, NetworkedPosition);

        if (positionDistance > 0.1f)
        {
            if (rb != null)
            {
                rb.velocity = NetworkedVelocity;
            }

            float lerpRate = positionDistance > 2f ? 50f : 20f;
            transform.position = Vector3.Lerp(
                transform.position,
                NetworkedPosition,
                Runner.DeltaTime * lerpRate
            );
        }

        // Scale synchronization
        float scaleDistance = Vector3.Distance(transform.localScale, NetworkedScale);
        if (scaleDistance > 0.01f)
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                NetworkedScale,
                Runner.DeltaTime * 15f
            );
        }

        // Rotation synchronization
        float targetYRotation = NetworkedYRotation;
        float currentYRotation = transform.eulerAngles.y;
        float rotationDifference = Mathf.DeltaAngle(currentYRotation, targetYRotation);

        if (Mathf.Abs(rotationDifference) > 1f)
        {
            Quaternion targetRotation = Quaternion.Euler(0, targetYRotation, 0);
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                Runner.DeltaTime * 10f
            );
        }
    }

    // ========== Render Method for Visual Interpolation (เหมือน Hero) ==========
    public override void Render()
    {
        // Visual interpolation สำหรับ remote clients
        if (!HasStateAuthority)
        {
            float alpha = Runner.DeltaTime * 20f;

            if (Vector3.Distance(transform.position, NetworkedPosition) > 0.01f)
            {
                transform.position = Vector3.Lerp(transform.position, NetworkedPosition, alpha);
            }

            if (Vector3.Distance(transform.localScale, NetworkedScale) > 0.001f)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, NetworkedScale, alpha);
            }

            // Rotation interpolation
            float targetYRotation = NetworkedYRotation;
            float currentYRotation = transform.eulerAngles.y;
            float rotationDifference = Mathf.DeltaAngle(currentYRotation, targetYRotation);

            if (Mathf.Abs(rotationDifference) > 1f)
            {
                Quaternion targetRotation = Quaternion.Euler(0, targetYRotation, 0);
                transform.rotation = Quaternion.Lerp(
                    transform.rotation,
                    targetRotation,
                    alpha
                );
            }
        }
    }

    // ========== Enemy AI ==========
    private void FindNearestPlayer()
    {
        if (Time.time < nextTargetCheckTime) return;
        nextTargetCheckTime = Time.time + attackCheckInterval;

        float nearestDistance = float.MaxValue;
        Hero nearestHero = null;

        // Find all heroes in scene
        Hero[] heroes = FindObjectsOfType<Hero>();

        foreach (Hero hero in heroes)
        {
            if (hero == null || !hero.IsSpawned) continue;

            float distance = Vector3.Distance(transform.position, hero.transform.position);
            if (distance < detectRange && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestHero = hero;
            }
        }

        if (nearestHero != null)
        {
            targetTransform = nearestHero.transform;
            CurrentTarget = nearestHero.Object.InputAuthority;
            Debug.Log($"Enemy found target: {nearestHero.CharacterName}");
        }
        else
        {
            targetTransform = null;
            CurrentTarget = PlayerRef.None;
        }
    }

    private void MoveTowardsTarget()
    {
        if (targetTransform == null || rb == null) return;

        Vector3 direction = (targetTransform.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetTransform.position);

        // Move if not in attack range
        if (distance > AttackRange)
        {
            Vector3 newPosition = transform.position + direction * MoveSpeed * Runner.DeltaTime;
            rb.MovePosition(newPosition);

            // Face target
            Vector3 lookDirection = new Vector3(direction.x, 0, direction.z);
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }
    }

    private void TryAttackTarget()
    {
        if (targetTransform == null) return;

        float distance = Vector3.Distance(transform.position, targetTransform.position);

        if (distance <= AttackRange && Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + AttackCooldown;

            // Attack via RPC
            RPC_PerformAttack(CurrentTarget);
        }
    }

    // ========== Combat RPCs ==========
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PerformAttack(PlayerRef targetPlayer)
    {
        // Find the target hero
        Hero targetHero = null;
        Hero[] heroes = FindObjectsOfType<Hero>();

        foreach (Hero hero in heroes)
        {
            if (hero.Object != null && hero.Object.InputAuthority == targetPlayer)
            {
                targetHero = hero;
                break;
            }
        }

        if (targetHero != null)
        {
            Debug.Log($"[ENEMY ATTACK] Target: {targetHero.CharacterName}, HasStateAuthority: {targetHero.HasStateAuthority}, HasInputAuthority: {targetHero.HasInputAuthority}");

            // ทำ damage ปกติก่อน
            if (targetHero.HasInputAuthority)
            {
                targetHero.TakeDamage(AttackDamage, DamageType.Normal, false);
            }

            // ตรวจสอบ Authority ก่อนใส่พิษ
            if (HasStateAuthority && targetHero.HasStateAuthority)
            {
                Debug.Log($"[ENEMY ATTACK] Applying poison to {targetHero.CharacterName}!");
                targetHero.ApplyPoison(20, 5f); // เปลี่ยนจาก 3 เป็น 20 เพื่อเห็นชัดเจน
            }
            else
            {
                Debug.LogWarning($"[ENEMY ATTACK] Cannot apply poison - Enemy Authority: {HasStateAuthority}, Hero Authority: {targetHero.HasStateAuthority}");
            }
        }
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnDeath()
    {
        Debug.Log($"Enemy {name} died!");

        IsDead = true;

        // Death effects on all clients
        if (characterRenderer != null)
        {
            characterRenderer.material.color = Color.gray;
        }

        // Disable collider
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Destroy after delay
        StartCoroutine(DestroyAfterDelay());
    }
    // ========== Visual Effects ==========

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(2f);

        if (HasStateAuthority && Object != null)
        {
            Runner.Despawn(Object);
        }
    }
   

    // ========== Debug ==========
    private void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AttackRange);

        // Line to target
        if (targetTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetTransform.position);
        }
    }
}