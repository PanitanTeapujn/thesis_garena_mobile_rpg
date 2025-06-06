using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class NetworkEnemy : Character
{
    // ========== Network Properties ==========
    [Networked] public int NetworkedCurrentHp { get; set; }
    [Networked] public int NetworkedMaxHp { get; set; }
    [Networked] public NetworkBool IsDead { get; set; }
    [Networked] public PlayerRef CurrentTarget { get; set; }

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
        }
    }

    // ========== Network Update ==========
    public override void FixedUpdateNetwork()
    {
        // Safety check
        if (!IsSpawned) return;

        // Only Host/Server controls enemies
        if (!HasStateAuthority) return;

        if (IsDead) return;

        // Find and attack nearest player
        FindNearestPlayer();
        MoveTowardsTarget();
        TryAttackTarget();

        // Sync health
        NetworkedCurrentHp = CurrentHp;

        // Check death
        if (CurrentHp <= 0 && !IsDead)
        {
            IsDead = true;
            RPC_OnDeath();
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
            Debug.Log($"Enemy {name} attacks {targetHero.CharacterName} for {AttackDamage} damage!");

            // Only the target player's client handles taking damage
            if (targetHero.HasInputAuthority)
            {
                targetHero.TakeDamage(AttackDamage);
            }
        }
    }

    public void TakeDamage(int damage, PlayerRef attacker)
    {
        if (!HasStateAuthority || !IsSpawned) return;

        CurrentHp -= damage;
        NetworkedCurrentHp = CurrentHp;

        Debug.Log($"Enemy {name} takes {damage} damage. HP: {CurrentHp}/{MaxHp}");

        // Visual feedback via RPC
        RPC_OnTakeDamage(damage);

        if (CurrentHp <= 0 && !IsDead)
        {
            IsDead = true;
            RPC_OnDeath();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnTakeDamage(int damage)
    {
        // Visual feedback on all clients
        StartCoroutine(DamageFlash());
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OnDeath()
    {
        Debug.Log($"Enemy {name} died!");

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
    private IEnumerator DamageFlash()
    {
        if (characterRenderer != null)
        {
            Color originalColor = characterRenderer.material.color;
            characterRenderer.material.color = Color.red;
            yield return new WaitForSeconds(0.2f);
            if (!IsDead)
            {
                characterRenderer.material.color = originalColor;
            }
        }
    }

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