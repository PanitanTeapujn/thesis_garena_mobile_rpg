using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

public class LoadingManager : MonoBehaviour
{
    [Header("Loading UI")]
    public GameObject loadingPanel;
    public TextMeshProUGUI loadingText;
    public Slider loadingProgressBar;
    public TextMeshProUGUI progressPercentText;

    [Header("Loading Messages")]
    public string[] loadingMessages = {
        "Connecting to server...",
        "Initializing network...",
        "Spawning character...",
        "Loading UI components...",
        "Preparing game world...",
        "Almost ready!"
    };

    [Header("Settings")]
    public float minLoadingTime = 2f; // เวลาโหลดขั้นต่ำ
    public float messageChangeInterval = 0.8f; // เปลี่ยนข้อความทุก 0.8 วินาที
    public bool debugMode = true;

    // สถานะการโหลด
    private bool isLoading = true;
    private bool networkRunnerReady = false;
    private bool playerSpawned = false;
    private bool uiReady = false;
    private bool characterDataLoaded = false;

    // Components ที่เกี่ยวข้อง
    private NetworkRunner networkRunner;
    private PlayerSpawner playerSpawner;
    private Hero localPlayer;
    private CombatUIManager combatUI;

    // Loading animation
    private int currentMessageIndex = 0;
    private float loadingProgress = 0f;
    private float startTime;

    private void Start()
    {
        startTime = Time.time;
        InitializeLoading();
        StartCoroutine(LoadingSequence());
    }

    private void InitializeLoading()
    {
        if (debugMode) Debug.Log("🔄 [LOADING] Initializing loading screen...");

        // แสดง loading panel
        ShowLoadingPanel();

        // หา components ที่จำเป็น
        FindRequiredComponents();

        // เริ่ม loading animation
        StartCoroutine(UpdateLoadingAnimation());
        StartCoroutine(UpdateLoadingMessages());
    }

    private void FindRequiredComponents()
    {
        // หา NetworkRunner
        networkRunner = FindObjectOfType<NetworkRunner>();

        // หา PlayerSpawner
        playerSpawner = FindObjectOfType<PlayerSpawner>();

        // หา CombatUIManager
        combatUI = FindObjectOfType<CombatUIManager>();

        if (debugMode)
        {
            Debug.Log($"🔍 [LOADING] Components found:");
            Debug.Log($"  NetworkRunner: {(networkRunner != null ? "✅" : "❌")}");
            Debug.Log($"  PlayerSpawner: {(playerSpawner != null ? "✅" : "❌")}");
            Debug.Log($"  CombatUI: {(combatUI != null ? "✅" : "❌")}");
        }
    }

    private void ShowLoadingPanel()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);

            // ตั้งค่าเริ่มต้น
            if (loadingText != null)
                loadingText.text = "Initializing...";

            if (loadingProgressBar != null)
                loadingProgressBar.value = 0f;

            if (progressPercentText != null)
                progressPercentText.text = "0%";

            if (debugMode) Debug.Log("✅ [LOADING] Loading panel shown");
        }
        else
        {
            Debug.LogError("❌ [LOADING] Loading panel not assigned!");
        }
    }

    private void HideLoadingPanel()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
            if (debugMode) Debug.Log("✅ [LOADING] Loading panel hidden");
        }
    }

    private IEnumerator LoadingSequence()
    {
        if (debugMode) Debug.Log("🚀 [LOADING] Starting loading sequence...");

        // 1. รอ NetworkRunner พร้อม
        yield return StartCoroutine(WaitForNetworkRunner());

        // 2. รอ Player Spawn
        yield return StartCoroutine(WaitForPlayerSpawn());

        // 3. รอ UI พร้อม
        yield return StartCoroutine(WaitForUI());

        // 4. รอ Character Data โหลด
        yield return StartCoroutine(WaitForCharacterData());

        // 5. รอเวลาขั้นต่ำ
        yield return StartCoroutine(WaitForMinimumTime());

        // 6. เสร็จแล้ว!
        CompleteLoading();
    }

    private IEnumerator WaitForNetworkRunner()
    {
        if (debugMode) Debug.Log("⏳ [LOADING] Waiting for NetworkRunner...");

        float timeout = 10f;
        float elapsed = 0f;

        while (!networkRunnerReady && elapsed < timeout)
        {
            // ตรวจสอบ NetworkRunner
            if (networkRunner == null)
                networkRunner = FindObjectOfType<NetworkRunner>();

            if (networkRunner != null && networkRunner.IsRunning)
            {
                networkRunnerReady = true;
                loadingProgress = 0.2f; // 20%
                if (debugMode) Debug.Log("✅ [LOADING] NetworkRunner ready!");
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!networkRunnerReady)
        {
            Debug.LogWarning("⚠️ [LOADING] NetworkRunner timeout!");
        }
    }

    private IEnumerator WaitForPlayerSpawn()
    {
        if (debugMode) Debug.Log("⏳ [LOADING] Waiting for player spawn...");

        float timeout = 15f;
        float elapsed = 0f;

        while (!playerSpawned && elapsed < timeout)
        {
            // หา local player
            if (localPlayer == null)
            {
                Hero[] allHeroes = FindObjectsOfType<Hero>();
                foreach (Hero hero in allHeroes)
                {
                    if (hero.HasInputAuthority)
                    {
                        localPlayer = hero;
                        playerSpawned = true;
                        loadingProgress = 0.5f; // 50%
                        if (debugMode) Debug.Log($"✅ [LOADING] Local player found: {hero.CharacterName}");
                        break;
                    }
                }
            }

            elapsed += Time.deltaTime;
            yield return new WaitForSeconds(0.1f);
        }

        if (!playerSpawned)
        {
            Debug.LogWarning("⚠️ [LOADING] Player spawn timeout!");
        }
    }

    private IEnumerator WaitForUI()
    {
        if (debugMode) Debug.Log("⏳ [LOADING] Waiting for UI setup...");

        float timeout = 8f;
        float elapsed = 0f;

        while (!uiReady && elapsed < timeout)
        {
            // ตรวจสอบ CombatUIManager
            if (combatUI == null)
                combatUI = FindObjectOfType<CombatUIManager>();

            if (combatUI != null && combatUI.localHero != null)
            {
                uiReady = true;
                loadingProgress = 0.7f; // 70%
                if (debugMode) Debug.Log("✅ [LOADING] UI ready!");
                break;
            }

            elapsed += Time.deltaTime;
            yield return new WaitForSeconds(0.1f);
        }

        if (!uiReady)
        {
            Debug.LogWarning("⚠️ [LOADING] UI setup timeout!");
            uiReady = true; // อนุญาตให้ดำเนินการต่อ
        }
    }

    private IEnumerator WaitForCharacterData()
    {
        if (debugMode) Debug.Log("⏳ [LOADING] Waiting for character data...");

        float timeout = 5f;
        float elapsed = 0f;

        while (!characterDataLoaded && elapsed < timeout)
        {
            // ตรวจสอบว่า character data โหลดเสร็จ
            if (localPlayer != null && localPlayer.GetInventory() != null)
            {
                characterDataLoaded = true;
                loadingProgress = 0.9f; // 90%
                if (debugMode) Debug.Log("✅ [LOADING] Character data loaded!");
                break;
            }

            elapsed += Time.deltaTime;
            yield return new WaitForSeconds(0.1f);
        }

        if (!characterDataLoaded)
        {
            Debug.LogWarning("⚠️ [LOADING] Character data timeout!");
            characterDataLoaded = true; // อนุญาตให้ดำเนินการต่อ
        }
    }

    private IEnumerator WaitForMinimumTime()
    {
        float elapsedTime = Time.time - startTime;
        float remainingTime = minLoadingTime - elapsedTime;

        if (remainingTime > 0)
        {
            if (debugMode) Debug.Log($"⏳ [LOADING] Waiting for minimum time: {remainingTime:F1}s");
            yield return new WaitForSeconds(remainingTime);
        }
    }

    private void CompleteLoading()
    {
        loadingProgress = 1f; // 100%
        isLoading = false;

        if (debugMode)
        {
            float totalTime = Time.time - startTime;
            Debug.Log($"🎉 [LOADING] Loading complete! Total time: {totalTime:F1}s");
        }

        // รอ 0.5 วินาทีเพื่อให้เห็น 100%
        StartCoroutine(DelayedHide());
    }

    private IEnumerator DelayedHide()
    {
        // อัปเดต UI ให้แสดง 100% ก่อน
        UpdateLoadingUI();
        yield return new WaitForSeconds(0.5f);
        HideLoadingPanel();
    }

    private IEnumerator UpdateLoadingAnimation()
    {
        while (isLoading)
        {
            UpdateLoadingUI();
            yield return new WaitForSeconds(0.1f); // อัปเดตทุก 0.1 วินาที
        }

        // อัปเดตครั้งสุดท้าย
        UpdateLoadingUI();
    }

    private void UpdateLoadingUI()
    {
        // อัปเดต progress bar
        if (loadingProgressBar != null)
        {
            loadingProgressBar.value = loadingProgress;
        }

        // อัปเดต เปอร์เซ็นต์
        if (progressPercentText != null)
        {
            int percent = Mathf.RoundToInt(loadingProgress * 100f);
            progressPercentText.text = $"{percent}%";
        }
    }

    private IEnumerator UpdateLoadingMessages()
    {
        while (isLoading)
        {
            if (loadingText != null && loadingMessages.Length > 0)
            {
                loadingText.text = loadingMessages[currentMessageIndex];
                currentMessageIndex = (currentMessageIndex + 1) % loadingMessages.Length;
            }

            yield return new WaitForSeconds(messageChangeInterval);
        }
    }

    // ✅ Public methods สำหรับรับสถานะจาก external systems
    public void OnNetworkReady()
    {
        networkRunnerReady = true;
        loadingProgress = Mathf.Max(loadingProgress, 0.2f); // อย่างน้อย 20%
        if (debugMode) Debug.Log("📢 [LOADING] Network ready notification received");
    }

    public void OnSpawnStarted()
    {
        loadingProgress = Mathf.Max(loadingProgress, 0.3f); // อย่างน้อย 30%
        if (debugMode) Debug.Log("📢 [LOADING] Spawn started notification received");
    }

    public void OnLocalPlayerReady(Hero localHero)
    {
        if (localHero != null && localHero.HasInputAuthority)
        {
            localPlayer = localHero;
            playerSpawned = true;
            loadingProgress = Mathf.Max(loadingProgress, 0.6f); // อย่างน้อย 60%
            if (debugMode) Debug.Log($"📢 [LOADING] Local player ready: {localHero.CharacterName}");
        }
    }

    public void OnUIReady()
    {
        uiReady = true;
        loadingProgress = Mathf.Max(loadingProgress, 0.8f); // อย่างน้อย 80%
        if (debugMode) Debug.Log("📢 [LOADING] UI ready notification received");
    }

    public void OnCharacterDataReady()
    {
        characterDataLoaded = true;
        loadingProgress = Mathf.Max(loadingProgress, 0.95f); // อย่างน้อย 95%
        if (debugMode) Debug.Log("📢 [LOADING] Character data ready notification received");
    }

    public void ForceCompleteLoading()
    {
        if (debugMode) Debug.Log("🔧 [LOADING] Force completing loading...");

        networkRunnerReady = true;
        playerSpawned = true;
        uiReady = true;
        characterDataLoaded = true;

        StopAllCoroutines();
        CompleteLoading();
    }

    public bool IsLoading()
    {
        return isLoading;
    }

    public float GetLoadingProgress()
    {
        return loadingProgress;
    }

    // Debug methods
    [ContextMenu("🔍 Debug Loading Status")]
    private void DebugLoadingStatus()
    {
        Debug.Log("=== LOADING STATUS ===");
        Debug.Log($"Network Runner Ready: {networkRunnerReady}");
        Debug.Log($"Player Spawned: {playerSpawned}");
        Debug.Log($"UI Ready: {uiReady}");
        Debug.Log($"Character Data Loaded: {characterDataLoaded}");
        Debug.Log($"Loading Progress: {loadingProgress:P0}");
        Debug.Log($"Is Loading: {isLoading}");
        Debug.Log("====================");
    }

    [ContextMenu("⚡ Force Complete Loading")]
    private void DebugForceComplete()
    {
        ForceCompleteLoading();
    }
}