using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Fusion;

/// <summary>
/// จัดการการ drop ของ enemy เมื่อตาย (เฉพาะเงินและเพชร)
/// </summary>
public class EnemyDropManager : NetworkBehaviour
{
    [Header("📋 Drop Configuration")]
    public EnemyDropSettings dropSettings;

    [Header("🎨 Visual Effects")]
    public GameObject goldDropEffectPrefab;
    public GameObject gemsDropEffectPrefab;

    [Header("🎯 Drop Behavior")]
    [Range(0.5f, 5f)]
    public float dropScatterRadius = 2f;
    [Range(1f, 10f)]
    public float dropForce = 3f;
    [Range(1f, 10f)]
    public float collectRange = 3f;

    [Header("🔧 Advanced Settings")]
    [Range(0f, 2f)]
    public float dropDelay = 0.5f;
    [Range(10f, 120f)]
    public float dropLifetime = 60f;
    public bool showDropLogs = true;

    private NetworkEnemy enemy;
    private LevelManager enemyLevelManager;
    private bool hasDropped = false;
    private List<GameObject> spawnedDrops = new List<GameObject>();

    private void Update()
    {
    }

  

    private void Awake()
    {
        enemy = GetComponent<NetworkEnemy>();
        enemyLevelManager = GetComponent<LevelManager>();

        if (dropSettings == null)
        {
            Debug.LogWarning($"[EnemyDropManager] No drop settings assigned to {gameObject.name}!");
        }
    }

    public void TriggerDrops()
    {
        if (hasDropped || !HasStateAuthority) return;
        hasDropped = true;

        if (dropSettings == null)
        {
            Debug.LogWarning($"[EnemyDropManager] No drop settings for {enemy.CharacterName}!");
            return;
        }

        StartCoroutine(ExecuteDropSequence());
    }

    private IEnumerator ExecuteDropSequence()
    {
        if (dropDelay > 0) yield return new WaitForSeconds(dropDelay);
        ExecuteDrops();
    }

    private void ExecuteDrops()
    {
        if (!HasStateAuthority) return;

        int enemyLevel = GetEnemyLevel();

        // คำนวณเฉพาะเงินและเพชร
        long goldDropped = dropSettings.CalculateGoldDrop(enemyLevel);
        int gemsDropped = dropSettings.CalculateGemsDrop(enemyLevel);

        // ✅ หา players ทั้งหมดในฉาก (ไม่จำกัดระยะ)
        List<Character> allPlayers = FindAllPlayers();

        if (allPlayers.Count == 0)
        {
            Debug.LogWarning("[EnemyDropManager] No players in scene!");
            return;
        }

        // Apply drops ให้ทุกคน
        ApplyDropsToAllPlayers(goldDropped, gemsDropped, allPlayers);

        // Create visual effects (ถ้ามี player ใกล้)
        List<Character> nearbyPlayers = FindNearbyPlayers();
        if (nearbyPlayers.Count > 0)
        {
            CreateDropVisuals(goldDropped, gemsDropped);
        }

        // Log results
        if (showDropLogs || dropSettings.showDropLogs)
        {
            Debug.Log($"[EnemyDropManager] {enemy.CharacterName} (Level {enemyLevel}) dropped: {goldDropped} gold, {gemsDropped} gems to {allPlayers.Count} players");
        }
    }

    // ✅ Method ใหม่: หา players ทั้งหมดในฉาก (ไม่จำกัดระยะ)
    private List<Character> FindAllPlayers()
    {
        List<Character> allPlayers = new List<Character>();

        // หา Hero ทั้งหมดในเกม
        Hero[] heroes = FindObjectsOfType<Hero>();

        foreach (Hero hero in heroes)
        {
            if (hero != null && hero.IsSpawned && hero.CurrentHp > 0)
            {
                allPlayers.Add(hero);
            }
        }

        Debug.Log($"[FindAllPlayers] Found {allPlayers.Count} alive players");
        return allPlayers;
    }

    private void ApplyDrops(long goldDropped, int gemsDropped)
    {
        List<Character> nearbyPlayers = FindNearbyPlayers();
        if (nearbyPlayers.Count == 0) return;

        // แบ่งเงินและเพชรให้ players
        if (goldDropped > 0)
        {
            long goldPerPlayer = goldDropped / nearbyPlayers.Count;
            if (goldPerPlayer > 0)
            {
                foreach (var player in nearbyPlayers)
                {
                    CurrencyManager.AddGoldStatic(goldPerPlayer);
                    RPC_ShowGoldPickup(player.Object, goldPerPlayer);
                }

                // บันทึกลง StageRewardTracker
                StageRewardTracker.AddGoldReward(goldDropped);
            }
        }

        if (gemsDropped > 0)
        {
            int gemsPerPlayer = gemsDropped / nearbyPlayers.Count;
            if (gemsPerPlayer > 0)
            {
                foreach (var player in nearbyPlayers)
                {
                    CurrencyManager.AddGemsStatic(gemsPerPlayer);
                    RPC_ShowGemsPickup(player.Object, gemsPerPlayer);
                }

                // บันทึกลง StageRewardTracker
                StageRewardTracker.AddGemsReward(gemsDropped);
            }
        }

        // ❌ ลบบรรทัดนี้ออก - ไม่ track enemy kill ที่นี่แล้ว
        // StageRewardTracker.AddEnemyKill();
    }

    private List<Character> FindNearbyPlayers()
    {
        List<Character> nearbyPlayers = new List<Character>();
        Collider[] playerColliders = Physics.OverlapSphere(
            transform.position,
            collectRange,
            LayerMask.GetMask("Player")
        );

        foreach (Collider col in playerColliders)
        {
            Character character = col.GetComponent<Character>();
            if (character != null && character.IsSpawned && character.CurrentHp > 0)
            {
                nearbyPlayers.Add(character);
            }
        }

        return nearbyPlayers;
    }
    private void ApplyDropsToAllPlayers(long goldDropped, int gemsDropped, List<Character> players)
    {
        if (players.Count == 0) return;

        // แบ่งเงินและเพชรให้ players ทั้งหมด
        if (goldDropped > 0)
        {
            long goldPerPlayer = goldDropped / players.Count;
            if (goldPerPlayer > 0)
            {
                foreach (var player in players)
                {
                    CurrencyManager.AddGoldStatic(goldPerPlayer);
                    RPC_ShowGoldPickup(player.Object, goldPerPlayer);

                    Debug.Log($"[EnemyDropManager] Gave {goldPerPlayer} gold to {player.CharacterName}");
                }

                // บันทึกลง StageRewardTracker
                StageRewardTracker.AddGoldReward(goldDropped);
            }
        }

        if (gemsDropped > 0)
        {
            int gemsPerPlayer = gemsDropped / players.Count;
            if (gemsPerPlayer > 0)
            {
                foreach (var player in players)
                {
                    CurrencyManager.AddGemsStatic(gemsPerPlayer);
                    RPC_ShowGemsPickup(player.Object, gemsPerPlayer);

                    Debug.Log($"[EnemyDropManager] Gave {gemsPerPlayer} gems to {player.CharacterName}");
                }

                // บันทึกลง StageRewardTracker
                StageRewardTracker.AddGemsReward(gemsDropped);
            }
        }
    }

    private int GetEnemyLevel()
    {
        return enemyLevelManager?.CurrentLevel ?? 1;
    }

    private void CreateDropVisuals(long goldDropped, int gemsDropped)
    {
        Vector3 dropPosition = transform.position;

        if (goldDropped > 0 && goldDropEffectPrefab != null)
        {
            CreateDropEffect(goldDropEffectPrefab, dropPosition, $"💰 {goldDropped:N0}");
        }

        if (gemsDropped > 0 && gemsDropEffectPrefab != null)
        {
            CreateDropEffect(gemsDropEffectPrefab, dropPosition, $"💎 {gemsDropped}");
        }
    }

    private void CreateDropEffect(GameObject effectPrefab, Vector3 position, string text)
    {
        if (effectPrefab == null) return;

        Vector3 randomOffset = Random.insideUnitSphere * dropScatterRadius;
        randomOffset.y = Mathf.Abs(randomOffset.y);
        Vector3 spawnPosition = position + randomOffset;

        GameObject effect = Instantiate(effectPrefab, spawnPosition, Quaternion.identity);

        var textComponent = effect.GetComponentInChildren<TMPro.TextMeshPro>();
        if (textComponent != null)
        {
            textComponent.text = text;
        }

        spawnedDrops.Add(effect);
        Destroy(effect, dropLifetime);
    }

    // RPC methods remain the same...
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowGoldPickup(NetworkObject playerObject, long amount)
    {
        if (playerObject != null)
        {
            Character character = playerObject.GetComponent<Character>();
            if (character != null)
            {
                // ✅ ใช้ DamageTextManager แทน
                DamageTextManager.ShowGoldPickup(character.transform.position, amount);
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowGemsPickup(NetworkObject playerObject, int amount)
    {
        if (playerObject != null)
        {
            Character character = playerObject.GetComponent<Character>();
            if (character != null)
            {
                // ✅ ใช้ DamageTextManager แทน
                DamageTextManager.ShowGemsPickup(character.transform.position, amount);
            }
        }
    }

    
   

   
    

   

   

    private void OnDestroy()
    {
        // ล้าง pickup texts เมื่อ enemy ถูกทำลาย

        Debug.Log("[EnemyDropManager] Cleaned up on destroy");
    }

    // ✅ เพิ่ม method สำหรับ debug
  

    // ✅ เพิ่ม method สำหรับ force cleanup ทั้งหมด
   
}