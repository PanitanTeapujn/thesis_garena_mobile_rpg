using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class BloodKnight : Hero
{
    // ========== BloodKnight Specific Properties ==========
    [Header("Blood Knight Stats")]
    public float rageMultiplier = 1.5f;
    public float rageDuration = 5f;

    [Networked] public bool IsRageActive { get; set; }
    [Networked] public float RageTimeRemaining { get; set; }

    // ========== Override Methods ==========
    protected override void ProcessClassSpecificAbilities()
    {
        // Rage ability (Q key)
        if (HasInputAuthority && Input.GetKeyDown(KeyCode.Q) && !IsRageActive)
        {
            RPC_ActivateRage();
        }

        // Update rage timer
        if (IsRageActive && HasStateAuthority)
        {
            RageTimeRemaining -= Runner.DeltaTime;
            if (RageTimeRemaining <= 0)
            {
                IsRageActive = false;
                // Reset stats
            }
        }
    }

    // ========== BloodKnight RPCs ==========
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_ActivateRage()
    {
        IsRageActive = true;
        RageTimeRemaining = rageDuration;
        // Increase stats
        MoveSpeed *= rageMultiplier;

        // Visual effects
        RPC_RageVisualEffect();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_RageVisualEffect()
    {
        // Particle effects, color change, etc.
        // GetComponent<ParticleSystem>()?.Play();
    }
}