using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Fusion;

/// <summary>
/// จัดการการ drop item ของ enemy เมื่อตาย
/// </summary>
public class ItemDropManager : NetworkBehaviour
{
    [Header("📋 Item Drop Configuration")]
    [Tooltip("Settings การ drop item สำหรับ enemy ตัวนี้")]
    public ItemDropSettings itemDropSettings;

    [Header("🎯 Drop Behavior")]
    [Range(1f, 15f)]
    public float collectRange = 10f;

    [Header("🔧 Advanced Settings")]
    [Range(0f, 2f)]
    public float dropDelay = 0f;
    public bool showDropLogs = true;

    // Components
    private NetworkEnemy enemy;
    private LevelManager enemyLevelManager;

    // Drop tracking
    private bool hasDropped = false;

    #region Unity Lifecycle
    private void Awake()
    {
        enemy = GetComponent<NetworkEnemy>();
        enemyLevelManager = GetComponent<LevelManager>();

        // Validate settings
        if (itemDropSettings == null)
        {
            Debug.LogWarning($"[ItemDropManager] No item drop settings assigned to {gameObject.name}!");
        }
        else if (!itemDropSettings.ValidateSettings())
        {
            Debug.LogError($"[ItemDropManager] Invalid item drop settings on {gameObject.name}!");
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// เรียกจาก NetworkEnemy เมื่อ enemy ตาย
    /// </summary>
    public void TriggerItemDrops()
    {
        if (hasDropped || !HasStateAuthority) return;

        hasDropped = true;

        if (itemDropSettings == null)
        {
            Debug.LogWarning($"[ItemDropManager] No item drop settings for {enemy.CharacterName}!");
            return;
        }

        StartCoroutine(ExecuteItemDropSequence());
    }

    /// <summary>
    /// บังคับให้ drop items ทั้งหมด (สำหรับทดสอบ)
    /// </summary>
    [ContextMenu("Force Drop Items")]
    public void ForceDropItems()
    {
        if (itemDropSettings == null) return;

        bool originalGuaranteed = itemDropSettings.guaranteedDropsForTesting;
        itemDropSettings.guaranteedDropsForTesting = true;

        ExecuteItemDrops();

        itemDropSettings.guaranteedDropsForTesting = originalGuaranteed;
    }
    #endregion

    #region Drop Execution
    private IEnumerator ExecuteItemDropSequence()
    {
        // หน่วงเวลาสำหรับ death animation
        if (dropDelay > 0)
        {
            yield return new WaitForSeconds(dropDelay);
        }

        ExecuteItemDrops();
    }

    private void ExecuteItemDrops()
    {
        if (!HasStateAuthority) return;

        // หา players ใกล้เคียง
        List<Character> nearbyPlayers = FindNearbyPlayers();
        if (nearbyPlayers.Count == 0)
        {
            Debug.Log("[ItemDropManager] No players nearby for item drops");
            return;
        }

        int enemyLevel = GetEnemyLevel();
        List<ItemDropResult> dropResults = CalculateItemDrops(enemyLevel);

        if (dropResults.Count == 0)
        {
            if (showDropLogs || itemDropSettings.showDropLogs)
            {
                Debug.Log($"[ItemDropManager] {enemy.CharacterName} (Level {enemyLevel}) dropped no items");
            }
            return;
        }

        // Apply drops
        ApplyItemDrops(dropResults, nearbyPlayers);

        // Log results
        if (showDropLogs || itemDropSettings.showDropLogs)
        {
            LogDropResults(dropResults, enemyLevel);
        }
    }

    private List<ItemDropResult> CalculateItemDrops(int enemyLevel)
    {
        List<ItemDropResult> dropResults = new List<ItemDropResult>();

        // ตรวจสอบโอกาส drop โดยรวม
        float effectiveDropChance = itemDropSettings.GetEffectiveDropChance(enemyLevel);

        if (Random.Range(0f, 100f) > effectiveDropChance && !itemDropSettings.guaranteedDropsForTesting)
        {
            return dropResults; // ไม่ drop อะไร
        }

        // หา items ที่สามารถ drop ได้
        List<ItemDropEntry> availableDrops = itemDropSettings.GetAvailableDropsForLevel(enemyLevel);

        if (availableDrops.Count == 0)
        {
            return dropResults;
        }

        // สุ่ม items ที่จะ drop
        List<ItemDropEntry> itemsToRoll = new List<ItemDropEntry>(availableDrops);
        int maxDropsThisTime = Random.Range(1, itemDropSettings.maxItemDrops + 1);
        int successfulDrops = 0;

        for (int attempt = 0; attempt < itemsToRoll.Count && successfulDrops < maxDropsThisTime; attempt++)
        {
            ItemDropEntry dropEntry = itemsToRoll[Random.Range(0, itemsToRoll.Count)];

            // สุ่มว่า item นี้จะ drop หรือไม่
            if (Random.Range(0f, 100f) <= dropEntry.dropChance || itemDropSettings.guaranteedDropsForTesting)
            {
                int quantity = dropEntry.RollQuantity();

                dropResults.Add(new ItemDropResult
                {
                    itemData = dropEntry.itemData,
                    quantity = quantity,
                    isRareItem = dropEntry.itemData.Tier >= ItemTier.Rare
                });

                successfulDrops++;

                // ลบออกจาก list เพื่อไม่ให้ drop ซ้ำ
                itemsToRoll.Remove(dropEntry);
            }
        }

        return dropResults;
    }

    private void ApplyItemDrops(List<ItemDropResult> dropResults, List<Character> nearbyPlayers)
    {
        // เลือก player ที่จะได้ items (สุ่ม)
        Character targetPlayer = nearbyPlayers[Random.Range(0, nearbyPlayers.Count)];

        foreach (var dropResult in dropResults)
        {
            DropItemToPlayer(targetPlayer, dropResult);
        }
    }

    private void DropItemToPlayer(Character player, ItemDropResult dropResult)
    {
        if (player?.GetInventory() == null)
        {
            Debug.LogWarning("[ItemDropManager] Player has no inventory!");
            return;
        }

        bool success = player.GetInventory().AddItem(dropResult.itemData, dropResult.quantity);

        if (success)
        {
            string itemText = dropResult.quantity > 1 ?
                $"{dropResult.itemData.ItemName} x{dropResult.quantity}" :
                dropResult.itemData.ItemName;

            // กำหนดสีและ prefix ตาม tier
            Color messageColor = dropResult.isRareItem ? dropResult.itemData.GetTierColor() : Color.white;
            string prefix = GetDropPrefix(dropResult.itemData.Tier);

            RPC_ShowItemPickup(player.Object, $"{prefix} {itemText}", messageColor);

            // 🆕 บันทึกลง StageRewardTracker
            StageRewardTracker.AddItemReward(dropResult.itemData, dropResult.quantity);

            Debug.Log($"[ItemDropManager] ✅ Gave {itemText} ({dropResult.itemData.GetTierText()}) to {player.CharacterName}");
        }
        else
        {
            Debug.LogWarning($"[ItemDropManager] Failed to add {dropResult.itemData.ItemName} to {player.CharacterName}'s inventory");
        }
    }
    private List<Character> FindNearbyPlayers()
    {
        List<Character> nearbyPlayers = new List<Character>();
        Collider[] playerColliders = Physics.OverlapSphere(transform.position, collectRange, LayerMask.GetMask("Player"));

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

    private int GetEnemyLevel()
    {
        return enemyLevelManager?.CurrentLevel ?? 1;
    }

    private string GetDropPrefix(ItemTier tier)
    {
        switch (tier)
        {
            case ItemTier.Common: return "🎁";
            case ItemTier.Uncommon: return "💚";
            case ItemTier.Rare: return "💙";
            case ItemTier.Epic: return "💜";
            case ItemTier.Legendary: return "💛";
            default: return "🎁";
        }
    }

    private void LogDropResults(List<ItemDropResult> dropResults, int enemyLevel)
    {
        Debug.Log($"[ItemDropManager] {enemy.CharacterName} (Level {enemyLevel}) dropped {dropResults.Count} items:");

        foreach (var result in dropResults)
        {
            string rareText = result.isRareItem ? " ✨ RARE" : "";
            Debug.Log($"  - {result.itemData.ItemName} x{result.quantity} ({result.itemData.GetTierText()}){rareText}");
        }
    }
    #endregion

    #region Network RPCs
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowItemPickup(NetworkObject playerObject, string message, Color color)
    {
        if (playerObject != null)
        {
            Character character = playerObject.GetComponent<Character>();
            if (character != null)
            {
                // หา EnemyDropManager เพื่อใช้ ShowPickupMessage
                EnemyDropManager dropManager = GetComponent<EnemyDropManager>();
                if (dropManager != null)
                {
                    dropManager.ShowPickupMessage(message, color, character.transform.position);
                }
                else
                {
                    Debug.Log($"💝 {character.CharacterName} received: {message}");
                }
            }
        }
    }
    #endregion

    #region Debug & Testing
   
    #endregion
}

/// <summary>
/// ผลลัพธ์การ drop item
/// </summary>
[System.Serializable]
public class ItemDropResult
{
    public ItemData itemData;
    public int quantity;
    public bool isRareItem;
}