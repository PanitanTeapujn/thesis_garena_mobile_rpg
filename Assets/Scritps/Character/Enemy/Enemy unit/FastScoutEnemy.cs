using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

// ========== 1. Fast Scout Enemy ==========
public class FastScoutEnemy : NetworkEnemy
{
    [Header("Scout Settings")]
    public float dashSpeed = 8f;
    public float dashCooldown = 5f;
    private float nextDashTime = 0f;

    protected override void Start()
    {
        base.Start();

        // Customize stats for scout
        if (HasStateAuthority)
        {
            // Fast but fragile
            MaxHp = Mathf.RoundToInt(MaxHp * 0.7f); // 70% HP
            CurrentHp = MaxHp;
            MoveSpeed *= 1.5f; // 150% speed
            AttackDamage = Mathf.RoundToInt(AttackDamage * 0.8f); // 80% damage
            detectRange *= 1.3f; // Better detection

            // Update network values
            NetworkedMaxHp = MaxHp;
            NetworkedCurrentHp = CurrentHp;

            Debug.Log($"FastScout spawned - HP: {MaxHp}, Speed: {MoveSpeed}, Damage: {AttackDamage}");
        }
    }

    // Override movement for dash ability
    protected override void ImprovedMoveTowardsTarget()
    {
        // Use dash ability if available
        if (HasStateAuthority && Time.time >= nextDashTime && targetTransform != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, targetTransform.position);
            if (distanceToTarget > AttackRange * 2f && distanceToTarget < detectRange)
            {
                StartCoroutine(DashTowardsTarget());
                nextDashTime = Time.time + dashCooldown;
            }
        }

        // Normal movement
        base.ImprovedMoveTowardsTarget();
    }

    private IEnumerator DashTowardsTarget()
    {
        if (targetTransform == null || rb == null) yield break;

        Vector3 dashDirection = (targetTransform.position - transform.position).normalized;

        // Quick dash
        rb.velocity = dashDirection * dashSpeed;
        yield return new WaitForSeconds(0.3f);

        // Return to normal
        rb.velocity = Vector3.zero;

        Debug.Log($"{CharacterName}: Performed dash attack!");
    }
}