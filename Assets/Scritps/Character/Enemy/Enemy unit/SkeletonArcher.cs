using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
public class SkeletonArcher : NetworkEnemy
{
    [Header("🏹 Skeleton Archer Settings")]
    [SerializeField] private float shootingRange = 8f;
    [SerializeField] private float optimalRange = 6f;
    [SerializeField] private float arrowSpeed = 10f;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float kiteDistance = 4f; // ระยะที่จะหลบ

    protected override void Start()
    {
        base.Start();

        // ตั้งค่าเฉพาะ Archer - เลือด少, ดาเมจปกติ, โจมตีไกล
        MoveSpeed = 3.5f;
        AttackDamage = 20;
        MaxHp = 45;
        CurrentHp = MaxHp;
        AttackRange = shootingRange;
        AttackCooldown = 2f;
        detectRange = 12f;

        Debug.Log($"🏹 Skeleton Archer spawned with {MaxHp} HP!");
    }

    protected override void ImprovedMoveTowardsTarget()
    {
        if (targetTransform == null || rb == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, targetTransform.position);
        Vector3 directionToPlayer = (targetTransform.position - transform.position).normalized;
        Vector3 moveDirection = Vector3.zero;

        // Archer AI: รักษาระยะห่างที่เหมาะสม
        if (distanceToPlayer < kiteDistance)
        {
            // ใกล้เกินไป - ถอยออก (Kiting)
            moveDirection = -directionToPlayer;
            CurrentState = EnemyState.BackingOff;
        }
        else if (distanceToPlayer > shootingRange)
        {
            // ไกลเกินไป - เข้าใกล้
            moveDirection = directionToPlayer;
            CurrentState = EnemyState.Chasing;
        }
        else if (distanceToPlayer >= kiteDistance && distanceToPlayer <= optimalRange)
        {
            // ระยะที่เหมาะสม - หยุดและยิง
            moveDirection = Vector3.zero;
            CurrentState = EnemyState.Attacking;
        }
        else
        {
            // ระยะปานกลาง - เคลื่อนที่เล็กน้อยเพื่อ positioning
            Vector3 sideDirection = new Vector3(-directionToPlayer.z, 0, directionToPlayer.x);
            moveDirection = sideDirection * 0.5f;
            CurrentState = EnemyState.Positioning;
        }

        // หลีกเลี่ยงศัตรูตัวอื่น
        Vector3 avoidanceForce = CalculateAvoidanceForce();
        moveDirection += avoidanceForce;

        // เคลื่อนที่
        if (moveDirection.magnitude > 0.1f)
        {
            moveDirection.y = 0;
            moveDirection.Normalize();

            Vector3 newPosition = transform.position + moveDirection * MoveSpeed * Runner.DeltaTime;
            rb.MovePosition(newPosition);

            FlipCharacterTowardsMovement(moveDirection);
        }
        else
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }
    }

    protected override void TryAttackTarget()
    {
        if (targetTransform == null) return;

        float distance = Vector3.Distance(transform.position, targetTransform.position);

        if (distance <= shootingRange && Runner.SimulationTime >= nextAttackTime)
        {
            nextAttackTime = Runner.SimulationTime + AttackCooldown;
            ShootArrow();
        }
    }

    private void ShootArrow()
    {
        if (targetTransform == null) return;

        Vector3 direction = (targetTransform.position - transform.position).normalized;
        RPC_ShootArrow(transform.position, direction);

        if (arrowPrefab != null && HasStateAuthority)
        {
            // สร้าง arrow projectile (ถ้าต้องการ physical arrow)
            Vector3 spawnPos = shootPoint != null ? shootPoint.position : transform.position + Vector3.up;

            // สำหรับการยิงแบบ instant hit แทน physical projectile
            StartCoroutine(ArrowTravelTime(direction));
        }
    }

    private IEnumerator ArrowTravelTime(Vector3 direction)
    {
        // คำนวณเวลาการเดินทางของลูกศร
        float distance = Vector3.Distance(transform.position, targetTransform.position);
        float travelTime = distance / arrowSpeed;

        yield return new WaitForSeconds(travelTime);

        // ตรวจสอบการโดนเมื่อลูกศรมาถึง
        if (targetTransform != null)
        {
            float finalDistance = Vector3.Distance(transform.position, targetTransform.position);

            // ตรวจสอบว่าผู้เล่นยังอยู่ในระยะที่เหมาะสม (อาจจะหลบได้)
            if (finalDistance <= shootingRange * 1.2f) // ให้โอกาสหลบเล็กน้อย
            {
                Hero hero = targetTransform.GetComponent<Hero>();
                if (hero != null)
                {
                    hero.TakeDamageFromAttacker(AttackDamage, this, DamageType.Normal);
                    Debug.Log($"🏹 Arrow hits {hero.CharacterName} for {AttackDamage} damage!");
                }
            }
            else
            {
                Debug.Log("🏹 Arrow missed - target moved!");
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShootArrow(Vector3 startPos, Vector3 direction)
    {
        Debug.Log("🏹 Skeleton shoots arrow!");
        // เพิ่ม arrow shooting animation และ sound effect
    }
}
