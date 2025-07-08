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
    private List<GameObject> activePickupTexts = new List<GameObject>();
    private const int MAX_PICKUP_TEXTS = 10;

    private void Update()
    {
        CleanupOldPickupTexts();
    }

    private void CleanupOldPickupTexts()
    {
        // ลบ null references
        for (int i = activePickupTexts.Count - 1; i >= 0; i--)
        {
            if (activePickupTexts[i] == null)
            {
                activePickupTexts.RemoveAt(i);
            }
        }

        // ถ้ามีเกินจำนวนที่กำหนด ให้ลบตัวเก่าทิ้ง
        while (activePickupTexts.Count > MAX_PICKUP_TEXTS)
        {
            GameObject oldText = activePickupTexts[0];
            if (oldText != null)
            {
                Debug.Log($"[PickupText] Cleanup destroying old text: {oldText.name}");
                Destroy(oldText);
            }
            activePickupTexts.RemoveAt(0);
        }
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

        // Apply drops
        ApplyDrops(goldDropped, gemsDropped);

        // Create visual effects
        CreateDropVisuals(goldDropped, gemsDropped);

        // Log results
        if (showDropLogs || dropSettings.showDropLogs)
        {
          //  Debug.Log($"[EnemyDropManager] {enemy.CharacterName} (Level {enemyLevel}) dropped: {goldDropped} gold, {gemsDropped} gems");
        }
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

    public void ShowPickupMessage(string message, Color color, Vector3 position)
    {
        // ตรวจสอบจำนวน texts เก่าก่อน
        CleanupOldPickupTexts();

        // หา หรือสร้าง pickup canvas
        Canvas pickupCanvas = FindPickupCanvas();
        if (pickupCanvas == null)
        {
            pickupCanvas = CreatePickupCanvas();
        }

        // สร้าง text object
        GameObject textObj = new GameObject("PickupText");
        textObj.transform.SetParent(pickupCanvas.transform, false);

        // เพิ่มลง tracking list
        activePickupTexts.Add(textObj);

        // Add RectTransform for UI
        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(300, 60); // ขนาดใหญ่ขึ้นเล็กน้อย

        // Add Text component
        var text = textObj.AddComponent<Text>();
        text.text = message;
        text.color = color;
        text.fontSize = 28; // ใหญ่ขึ้นเล็กน้อยเพื่อเห็นชัด
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold; // ทำให้เด่นขึ้น

        // เพิ่ม outline เพื่อให้อ่านง่าย
        var outline = textObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, 2);

        // Convert world position to screen position
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Vector3 worldPos = position + Vector3.up * 6f; // ยกสูงขึ้นเล็กน้อย
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
            rectTransform.position = screenPos;
        }

        // Add CanvasGroup for smooth alpha animation
        CanvasGroup canvasGroup = textObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;

        // ✅ เริ่ม animation ทันที
        StartCoroutine(AnimatePickupTextUI(textObj, rectTransform, canvasGroup));

        // ✅ Fallback destroy (ป้องกันไม่หาย)
        StartCoroutine(ForceDestroyAfterTime(textObj, 4f));

       // Debug.Log($"[PickupText] Created: '{message}' at {position}");
    }
    private IEnumerator ForceDestroyAfterTime(GameObject textObj, float time)
    {
        yield return new WaitForSeconds(time);

        if (textObj != null)
        {
         //   Debug.Log($"[PickupText] Force destroying text: {textObj.name}");

            // ลบออกจาก active list
            if (activePickupTexts.Contains(textObj))
            {
                activePickupTexts.Remove(textObj);
            }

            // ลบ object
            Destroy(textObj);
        }
    }

   
    private Canvas FindPickupCanvas()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            if (canvas.name == "PickupCanvas") return canvas;
        }
        return null;
    }

    private Canvas CreatePickupCanvas()
    {
        GameObject canvasObj = new GameObject("PickupCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        return canvas;
    }

    private IEnumerator AnimatePickupTextUI(GameObject textObj, RectTransform rectTransform, CanvasGroup canvasGroup)
    {
        if (textObj == null || rectTransform == null || canvasGroup == null)
        {
            Debug.LogWarning("[PickupText] Missing components for animation");
            yield break;
        }

        float duration = 3f; // เพิ่มเวลาให้เห็นนานขึ้น
        Vector3 startPos = rectTransform.position;
        Vector3 endPos = startPos + Vector3.up * 200f; // ขยับสูงขึ้น
        float startTime = Time.time;

        Debug.Log($"[PickupText] Starting animation for {textObj.name}");

        // Phase 1: แสดงเต็มที่ 1 วินาทีแรก
        yield return new WaitForSeconds(0.5f);

        // Phase 2: เริ่ม fade out และขยับขึ้น
        float fadeStartTime = Time.time;
        float fadeDuration = 1f;

        while (Time.time - fadeStartTime < fadeDuration)
        {
            if (textObj == null || rectTransform == null || canvasGroup == null)
            {
                Debug.LogWarning("[PickupText] Object destroyed during animation");
                break;
            }

            float elapsed = Time.time - fadeStartTime;
            float progress = elapsed / fadeDuration;

            // Move upward
            rectTransform.position = Vector3.Lerp(startPos, endPos, progress);

            // Fade out
            canvasGroup.alpha = 1f - progress;

            yield return null;
        }

        // Phase 3: Force cleanup
        if (textObj != null)
        {
            Debug.Log($"[PickupText] Animation complete, destroying {textObj.name}");

            // ลบออกจาก active list
            if (activePickupTexts.Contains(textObj))
            {
                activePickupTexts.Remove(textObj);
            }

            Destroy(textObj);
        }
    }

    private void OnDestroy()
    {
        // ล้าง pickup texts เมื่อ enemy ถูกทำลาย
        ForceCleanupAllPickupTexts();

        Debug.Log("[EnemyDropManager] Cleaned up on destroy");
    }

    // ✅ เพิ่ม method สำหรับ debug
  

    // ✅ เพิ่ม method สำหรับ force cleanup ทั้งหมด
    [ContextMenu("🧹 Force Cleanup All Pickup Texts")]
    public void ForceCleanupAllPickupTexts()
    {
        Debug.Log($"[PickupText] Force cleaning up {activePickupTexts.Count} pickup texts");

        foreach (GameObject textObj in activePickupTexts)
        {
            if (textObj != null)
            {
                Destroy(textObj);
            }
        }

        activePickupTexts.Clear();
        Debug.Log("[PickupText] All pickup texts cleared");
    }
}