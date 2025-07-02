using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Fusion;

/// <summary>
/// จัดการการ drop ของ enemy เมื่อตาย
/// ติดตั้งใน enemy prefab และทำงานร่วมกับ EnemyDropSettings
/// </summary>
public class EnemyDropManager : NetworkBehaviour
{
    [Header("📋 Drop Configuration")]
    [Tooltip("Settings การ drop สำหรับ enemy ตัวนี้")]
    public EnemyDropSettings dropSettings;

    [Header("🎨 Visual Effects")]
    [Tooltip("Prefab สำหรับแสดงเงินที่ drop")]
    public GameObject goldDropEffectPrefab;

    [Tooltip("Prefab สำหรับแสดงเพชรที่ drop")]
    public GameObject gemsDropEffectPrefab;

    [Tooltip("Prefab สำหรับแสดงไอเทมที่ drop")]
    public GameObject itemDropEffectPrefab;

    [Tooltip("Prefab สำหรับแสดงไอเทมหายากที่ drop")]
    public GameObject rareItemDropEffectPrefab;

    [Header("🎯 Drop Behavior")]
    [Tooltip("ระยะที่จะ scatter drops รอบๆ enemy")]
    [Range(0.5f, 5f)]
    public float dropScatterRadius = 2f;

    [Tooltip("ความแรงในการ scatter drops")]
    [Range(1f, 10f)]
    public float dropForce = 3f;

    [Tooltip("ระยะที่ผู้เล่นสามารถเก็บ drops ได้")]
    [Range(1f, 10f)]
    public float collectRange = 3f;

    [Header("🔧 Advanced Settings")]
    [Tooltip("หน่วงเวลาก่อนที่จะ drop (สำหรับ animation)")]
    [Range(0f, 2f)]
    public float dropDelay = 0.5f;

    [Tooltip("เวลาที่ drops จะหายไปเอง")]
    [Range(10f, 120f)]
    public float dropLifetime = 60f;

    [Tooltip("แสดง logs การ drop")]
    public bool showDropLogs = true;

    // Components
    private NetworkEnemy enemy;
    private LevelManager enemyLevelManager;

    // Drop tracking
    private bool hasDropped = false;
    private List<GameObject> spawnedDrops = new List<GameObject>();

    // Cleanup tracking
    private List<GameObject> activePickupTexts = new List<GameObject>();
    private const int MAX_PICKUP_TEXTS = 10; // จำกัดจำนวน texts

    private void Update()
    {
        // ล้าง null references และ texts ที่เก่าเกินไป
        CleanupOldPickupTexts();
    }

    private void CleanupOldPickupTexts()
    {
        // ลบ null references
        activePickupTexts.RemoveAll(text => text == null);

        // ถ้ามีเกินจำนวนที่กำหนด ให้ลบตัวเก่าทิ้ง
        while (activePickupTexts.Count > MAX_PICKUP_TEXTS)
        {
            GameObject oldText = activePickupTexts[0];
            if (oldText != null)
            {
                Destroy(oldText);
            }
            activePickupTexts.RemoveAt(0);
        }
    }

    private void OnDestroy()
    {
        // ล้าง pickup texts เมื่อ object ถูกทำลาย
        ForceCleanupAllPickupTexts();
    }

    private void ForceCleanupAllPickupTexts()
    {
        foreach (GameObject textObj in activePickupTexts)
        {
            if (textObj != null)
            {
                Destroy(textObj);
            }
        }
        activePickupTexts.Clear();
    }

    #region Unity Lifecycle
    private void Awake()
    {
        enemy = GetComponent<NetworkEnemy>();
        enemyLevelManager = GetComponent<LevelManager>();

        // Validate settings
        if (dropSettings == null)
        {
            Debug.LogWarning($"[EnemyDropManager] No drop settings assigned to {gameObject.name}!");
        }
        else if (!dropSettings.ValidateSettings())
        {
            Debug.LogError($"[EnemyDropManager] Invalid drop settings on {gameObject.name}!");
        }
    }

    private void Start()
    {
        // Subscribe to enemy death
        if (enemy != null)
        {
            // We'll call this from NetworkEnemy.RPC_OnDeath()
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// เรียกจาก NetworkEnemy เมื่อ enemy ตาย
    /// </summary>
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

    /// <summary>
    /// บังคับให้ drop ทุกอย่าง (สำหรับทดสอบ)
    /// </summary>
    [ContextMenu("Force Drop Everything")]
    public void ForceDropEverything()
    {
        if (dropSettings == null) return;

        bool originalGuaranteed = dropSettings.guaranteedDropsForTesting;
        dropSettings.guaranteedDropsForTesting = true;

        ExecuteDrops();

        dropSettings.guaranteedDropsForTesting = originalGuaranteed;
    }
    #endregion

    #region Drop Execution
    private IEnumerator ExecuteDropSequence()
    {
        // หน่วงเวลาสำหรับ death animation
        if (dropDelay > 0)
        {
            yield return new WaitForSeconds(dropDelay);
        }

        ExecuteDrops();
    }

    private void ExecuteDrops()
    {
        if (!HasStateAuthority) return;

        int enemyLevel = GetEnemyLevel();
        EnemyDropResult dropResult = CalculateDrops(enemyLevel);

        // Apply drops
        ApplyDrops(dropResult);

        // Create visual effects
        CreateDropVisuals(dropResult);

        // Log results
        if (showDropLogs || dropSettings.showDropLogs)
        {
            LogDropResults(dropResult, enemyLevel);
        }
    }

    private EnemyDropResult CalculateDrops(int enemyLevel)
    {
        EnemyDropResult result = new EnemyDropResult();

        // คำนวณเงิน
        result.goldDropped = dropSettings.CalculateGoldDrop(enemyLevel);

        // คำนวณเพชร
        result.gemsDropped = dropSettings.CalculateGemsDrop(enemyLevel);

        // คำนวณไอเทม
        result.itemsDropped = dropSettings.RollItemDrops(enemyLevel);

        // ตรวจสอบว่ามีไอเทมหายากหรือไม่
        foreach (var item in result.itemsDropped)
        {
            if (item.isRareDrop)
            {
                result.hasRareItems = true;
                break;
            }
        }

        return result;
    }

    private void ApplyDrops(EnemyDropResult dropResult)
    {
        // หา players ในระยะใกล้เคียง
        List<Character> nearbyPlayers = FindNearbyPlayers();

        if (nearbyPlayers.Count == 0)
        {
            if (showDropLogs)
                Debug.Log($"[EnemyDropManager] No players nearby to receive drops from {enemy.CharacterName}");
            return;
        }

        // แบ่งเงินและเพชรให้ players
        if (dropResult.goldDropped > 0)
        {
            long goldPerPlayer = dropResult.goldDropped / nearbyPlayers.Count;
            if (goldPerPlayer > 0)
            {
                foreach (var player in nearbyPlayers)
                {
                    CurrencyManager.AddGoldStatic(goldPerPlayer);
                    RPC_ShowGoldPickup(player.Object, goldPerPlayer);
                }
            }
        }

        if (dropResult.gemsDropped > 0)
        {
            int gemsPerPlayer = dropResult.gemsDropped / nearbyPlayers.Count;
            if (gemsPerPlayer > 0)
            {
                foreach (var player in nearbyPlayers)
                {
                    CurrencyManager.AddGemsStatic(gemsPerPlayer);
                    RPC_ShowGemsPickup(player.Object, gemsPerPlayer);
                }
            }
        }

        // ให้ไอเทมแก่ player คนแรก (หรือสุ่ม)
        if (dropResult.itemsDropped.Count > 0)
        {
            Character targetPlayer = nearbyPlayers[Random.Range(0, nearbyPlayers.Count)];
            var inventory = targetPlayer.GetInventory();

            if (inventory != null)
            {
                foreach (var itemDrop in dropResult.itemsDropped)
                {
                    bool added = inventory.AddItem(itemDrop.itemData, itemDrop.quantity);
                    if (added)
                    {
                        RPC_ShowItemPickup(targetPlayer.Object, itemDrop.itemData.ItemName, itemDrop.quantity, itemDrop.isRareDrop);
                    }
                    else
                    {
                        Debug.LogWarning($"[EnemyDropManager] Could not add {itemDrop.itemData.ItemName} to {targetPlayer.CharacterName}'s inventory");
                    }
                }
            }
        }
    }

    private List<Character> FindNearbyPlayers()
    {
        List<Character> nearbyPlayers = new List<Character>();

        // หา Characters ในระยะ collectRange
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
        if (enemyLevelManager != null)
        {
            return enemyLevelManager.CurrentLevel;
        }
        return 1;
    }
    #endregion

    #region Visual Effects
    private void CreateDropVisuals(EnemyDropResult dropResult)
    {
        Vector3 dropPosition = transform.position;

        // Gold effect
        if (dropResult.goldDropped > 0 && goldDropEffectPrefab != null)
        {
            CreateDropEffect(goldDropEffectPrefab, dropPosition, $"💰 {dropResult.goldDropped:N0}");
        }

        // Gems effect
        if (dropResult.gemsDropped > 0 && gemsDropEffectPrefab != null)
        {
            CreateDropEffect(gemsDropEffectPrefab, dropPosition, $"💎 {dropResult.gemsDropped}");
        }

        // Item effects
        foreach (var itemDrop in dropResult.itemsDropped)
        {
            GameObject effectPrefab = itemDrop.isRareDrop ? rareItemDropEffectPrefab : itemDropEffectPrefab;
            if (effectPrefab != null)
            {
                string text = itemDrop.quantity > 1 ? $"{itemDrop.itemData.ItemName} x{itemDrop.quantity}" : itemDrop.itemData.ItemName;
                CreateDropEffect(effectPrefab, dropPosition, text);
            }
        }
    }

    private void CreateDropEffect(GameObject effectPrefab, Vector3 position, string text)
    {
        if (effectPrefab == null) return;

        // Scatter position
        Vector3 randomOffset = Random.insideUnitSphere * dropScatterRadius;
        randomOffset.y = Mathf.Abs(randomOffset.y); // ให้อยู่เหนือพื้น
        Vector3 spawnPosition = position + randomOffset;

        GameObject effect = Instantiate(effectPrefab, spawnPosition, Quaternion.identity);

        // Set text if possible
        var textComponent = effect.GetComponentInChildren<TMPro.TextMeshPro>();
        if (textComponent != null)
        {
            textComponent.text = text;
        }

        // Add to spawned drops list
        spawnedDrops.Add(effect);

        // Auto destroy after lifetime
        Destroy(effect, dropLifetime);
    }
    #endregion

    #region Network RPCs
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowGoldPickup(NetworkObject playerObject, long amount)
    {
        if (playerObject != null)
        {
            Character character = playerObject.GetComponent<Character>();
            if (character != null)
            {
                ShowPickupMessage($"💰 +{amount:N0} Gold", Color.yellow, character.transform.position);
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
                ShowPickupMessage($"💎 +{amount} Gems", Color.cyan, character.transform.position);
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowItemPickup(NetworkObject playerObject, string itemName, int quantity, bool isRare)
    {
        if (playerObject != null)
        {
            Character character = playerObject.GetComponent<Character>();
            if (character != null)
            {
                string message = quantity > 1 ? $"🎒 +{itemName} x{quantity}" : $"🎒 +{itemName}";
                Color color = isRare ? Color.magenta : Color.white;
                ShowPickupMessage(message, color, character.transform.position);
            }
        }
    }

    private void ShowPickupMessage(string message, Color color, Vector3 position)
    {
        // ตรวจสอบจำนวน texts เก่าก่อน
        CleanupOldPickupTexts();

        // Find or create canvas for pickup messages
        Canvas pickupCanvas = FindPickupCanvas();
        if (pickupCanvas == null)
        {
            pickupCanvas = CreatePickupCanvas();
        }

        // Create UI text instead of TextMesh for better alpha support
        GameObject textObj = new GameObject("PickupText");
        textObj.transform.SetParent(pickupCanvas.transform, false);

        // เพิ่มลง tracking list
        activePickupTexts.Add(textObj);

        // Add RectTransform for UI
        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 50);

        // Add Text component
        var text = textObj.AddComponent<Text>();
        text.text = message;
        text.color = color;
        text.fontSize = 24;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.alignment = TextAnchor.MiddleCenter;

        // Convert world position to screen position
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Vector3 screenPos = mainCamera.WorldToScreenPoint(position + Vector3.up * 2f);
            rectTransform.position = screenPos;
        }

        // Add CanvasGroup for smooth alpha animation
        CanvasGroup canvasGroup = textObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;

        // Animate upward and fade out
        StartCoroutine(AnimatePickupTextUI(textObj, rectTransform, canvasGroup));

        // Fallback destroy after 5 seconds (just in case)
        StartCoroutine(FallbackDestroy(textObj, 5f));
    }

    private IEnumerator FallbackDestroy(GameObject textObj, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (textObj != null)
        {
            Debug.LogWarning("[EnemyDropManager] Fallback destroying pickup text that didn't auto-destroy");
            activePickupTexts.Remove(textObj);
            Destroy(textObj);
        }
    }

    

    private Canvas FindPickupCanvas()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            if (canvas.name == "PickupCanvas")
                return canvas;
        }
        return null;
    }

    private Canvas CreatePickupCanvas()
    {
        GameObject canvasObj = new GameObject("PickupCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // แสดงอยู่บนสุด

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        Debug.Log("[EnemyDropManager] Created PickupCanvas for text effects");
        return canvas;
    }

    private IEnumerator AnimatePickupTextUI(GameObject textObj, RectTransform rectTransform, CanvasGroup canvasGroup)
    {
        if (textObj == null || rectTransform == null || canvasGroup == null)
        {
            yield break;
        }

        float duration = 2.5f;
        Vector3 startPos = rectTransform.position;
        Vector3 endPos = startPos + Vector3.up * 100f; // Move up 100 pixels
        float startTime = Time.time;

        // Animate for duration
        while (Time.time - startTime < duration)
        {
            if (textObj == null || rectTransform == null || canvasGroup == null)
            {
                break;
            }

            float elapsed = Time.time - startTime;
            float progress = elapsed / duration;

            // Move upward
            rectTransform.position = Vector3.Lerp(startPos, endPos, progress);

            // Fade out (start fading after 50% of duration)
            if (progress > 0.5f)
            {
                float fadeProgress = (progress - 0.5f) / 0.5f; // 0 to 1 for second half
                canvasGroup.alpha = 1f - fadeProgress;
            }

            yield return null;
        }

        // Force destroy after animation
        if (textObj != null)
        {
            Destroy(textObj);
        }
    }
    #endregion

    #region Debug & Logging
    private void LogDropResults(EnemyDropResult dropResult, int enemyLevel)
    {
        Debug.Log($"=== {enemy.CharacterName} (Level {enemyLevel}) DROPS ===");

        if (dropResult.goldDropped > 0)
            Debug.Log($"💰 Gold: {dropResult.goldDropped:N0}");

        if (dropResult.gemsDropped > 0)
            Debug.Log($"💎 Gems: {dropResult.gemsDropped}");

        foreach (var itemDrop in dropResult.itemsDropped)
        {
            string rarity = itemDrop.isRareDrop ? " (RARE)" : "";
            string quantity = itemDrop.quantity > 1 ? $" x{itemDrop.quantity}" : "";
            Debug.Log($"🎒 Item: {itemDrop.itemData.ItemName}{quantity}{rarity}");
        }

        if (dropResult.hasRareItems)
            Debug.Log("🌟 RARE ITEM DROPPED!");

        Debug.Log("===============================");
    }

    [ContextMenu("Test Drop Calculation")]
    private void TestDropCalculation()
    {
        if (dropSettings == null)
        {
            Debug.LogError("No drop settings assigned!");
            return;
        }

        int testLevel = GetEnemyLevel();
        EnemyDropResult result = CalculateDrops(testLevel);
        LogDropResults(result, testLevel);
    }

    [ContextMenu("Show Drop Settings Info")]
    private void ShowDropSettingsInfo()
    {
        if (dropSettings == null)
        {
            Debug.LogError("No drop settings assigned!");
            return;
        }

        Debug.Log($"=== DROP SETTINGS INFO ===");
        Debug.Log($"Gold: {dropSettings.minGoldDrop}-{dropSettings.maxGoldDrop} ({dropSettings.goldDropChance}%)");
        Debug.Log($"Gems: {dropSettings.minGemsDrop}-{dropSettings.maxGemsDrop} ({dropSettings.gemsDropChance}%)");
        Debug.Log($"Items: {dropSettings.itemDrops.Count} types, max {dropSettings.maxItemsPerDrop} per drop");
        Debug.Log($"Rare Items: {dropSettings.rareDrops.Count} types");
        Debug.Log($"Level Bonus: {dropSettings.goldLevelBonus}% gold, {dropSettings.dropChanceLevelBonus}% chance");
        Debug.Log("==========================");
    }

    [ContextMenu("Clean All Pickup Texts")]
    private void CleanAllPickupTextsMenu()
    {
        Debug.Log($"[EnemyDropManager] Cleaning {activePickupTexts.Count} pickup texts...");
        ForceCleanupAllPickupTexts();
        Debug.Log("[EnemyDropManager] ✅ All pickup texts cleaned!");
    }

    [ContextMenu("Test Pickup Message")]
    private void TestPickupMessage()
    {
        ShowPickupMessage("💰 +999 Gold (TEST)", Color.yellow, transform.position);
        Debug.Log("[EnemyDropManager] Test pickup message created");
    }
    #endregion

    #region Gizmos
    private void OnDrawGizmosSelected()
    {
        // Draw collect range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, collectRange);

        // Draw scatter radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, dropScatterRadius);
    }
    #endregion
}