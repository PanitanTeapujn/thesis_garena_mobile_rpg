using UnityEngine;
using System.Collections;

public class GlobalMemoryManager : MonoBehaviour
{
    [Header("Global Memory Management")]
    public static GlobalMemoryManager Instance;

    [Header("Settings")]
    public bool enableAutoCleanup = true;
    public float cleanupInterval = 30f;
    public bool enableAutoMemoryManagement = true;
    public int memoryThresholdMB = 500;
    public int criticalMemoryMB = 700;

    [Header("Scene Detection")]
    public bool logSceneChanges = true;
    private string currentSceneName;

    void Awake()
    {
        // Singleton pattern - มีได้แค่ตัวเดียวในเกมทั้งหมด
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ไม่ถูกลบเมื่อเปลี่ยน scene
            Initialize();
        }
        else
        {
            Destroy(gameObject); // ลบตัวซ้ำ
        }
    }

    void Initialize()
    {
        Debug.Log("🌍 Global Memory Manager initialized!");
        currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (enableAutoCleanup)
        {
            InvokeRepeating(nameof(DoQuickCleanup), 10f, cleanupInterval);
        }

        if (enableAutoMemoryManagement)
        {
            InvokeRepeating(nameof(CheckMemoryAndCleanup), 5f, 10f);
        }

        // ฟัง scene change events
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;

        DoQuickCleanup();
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        currentSceneName = scene.name;

        if (logSceneChanges)
        {
            Debug.Log($"🎬 Scene loaded: {scene.name}");
        }

        // ทำความสะอาดแบบ async หลังจาก scene โหลดเสร็จ
        StartCoroutine(DelayedCleanupAfterSceneLoad());
    }

    void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
    {
        if (logSceneChanges)
        {
            Debug.Log($"🎬 Scene unloaded: {scene.name}");
        }

        // ทำความสะอาดแบบเบาๆ เมื่อ unload scene
        StartCoroutine(LightCleanupAfterSceneUnload());
    }

    private System.Collections.IEnumerator DelayedCleanupAfterSceneLoad()
    {
        // รอให้ scene โหลดเสร็จและ frame rate เสถียร
        yield return new WaitForSeconds(1f);
        yield return new WaitForEndOfFrame();

        // ทำความสะอาดแบบเบาๆ
        DoLightCleanup();
    }

    private System.Collections.IEnumerator LightCleanupAfterSceneUnload()
    {
        // รอ 1 frame ก่อนทำความสะอาด
        yield return null;

        // ทำแค่ทำความสะอาดที่จำเป็น
        DoLightCleanup();
    }

    // แทนที่ DoQuickCleanup เดิม
    public void DoQuickCleanup()
    {
        Debug.Log($"🧹 Starting smooth cleanup in scene: {currentSceneName}");

        // ใช้ Coroutine แทนการทำงานทันที
        StartCoroutine(SmoothCleanupCoroutine());
    }

    // เพิ่ม method สำหรับ cleanup แบบเบาๆ
    public void DoLightCleanup()
    {
        Debug.Log($"🧹 Light cleanup in scene: {currentSceneName}");
        StartCoroutine(LightCleanupCoroutine());
    }

    // Cleanup แบบเบาๆ ไม่กระตุก
    private System.Collections.IEnumerator LightCleanupCoroutine()
    {
        // ทำครั้งละน้อยๆ และรอ frame

        // 1. ทำความสะอาด particle systems เบาๆ
        yield return StartCoroutine(CleanupParticleSystemsSmooth());

        // 2. Clear audio cache เบาๆ  
        yield return StartCoroutine(ClearAudioCacheSmoothLight());

        // 3. Resource cleanup เบาๆ
        Resources.UnloadUnusedAssets();
        yield return null; // รอ 1 frame

        System.GC.Collect();
        yield return null; // รอ 1 frame

        LogCurrentMemory();
        Debug.Log("✅ Light cleanup completed smoothly!");
    }

    // Cleanup แบบเต็มแต่ไม่กระตุก
    private System.Collections.IEnumerator SmoothCleanupCoroutine()
    {
        // แบ่งงานออกเป็นหลาย frame เพื่อไม่ให้กระตุก

        // 1. Cleanup particle systems (รอ frame)
        yield return StartCoroutine(CleanupParticleSystemsSmooth());

        // 2. Cleanup inactive objects (รอ frame)
        yield return StartCoroutine(CleanupInactiveObjectsSmooth());

        // 3. Clear audio cache (รอ frame)
        yield return StartCoroutine(ClearAudioCacheSmooth());

        // 4. Resource cleanup แบบค่อยเป็นค่อยไป
        yield return StartCoroutine(SafeResourceCleanupSmooth());

        LogCurrentMemory();
        Debug.Log("✅ Smooth cleanup completed!");
    }

    private System.Collections.IEnumerator CleanupParticleSystemsSmooth()
    {
        ParticleSystem[] allPS = FindObjectsOfType<ParticleSystem>();
        int cleanedCount = 0;
        int processedThisFrame = 0;

        foreach (ParticleSystem ps in allPS)
        {
            if (ps == null) continue;

            // ทำครั้งละ 5 particle systems แล้วรอ frame
            if (processedThisFrame >= 5)
            {
                processedThisFrame = 0;
                yield return null; // รอ 1 frame
            }

            if (ps.isPlaying && (ps.particleCount == 0 || ps.time > 60f))
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                cleanedCount++;
            }

            processedThisFrame++;
        }

        if (cleanedCount > 0)
        {
            Debug.Log($"🧹 Smoothly cleaned {cleanedCount} particle systems");
        }
    }

    private System.Collections.IEnumerator CleanupInactiveObjectsSmooth()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        int markedCount = 0;
        int processedThisFrame = 0;

        foreach (GameObject obj in allObjects)
        {
            if (obj == null) continue;

            // ทำครั้งละ 10 objects แล้วรอ frame
            if (processedThisFrame >= 10)
            {
                processedThisFrame = 0;
                yield return null; // รอ 1 frame
            }

            if (!obj.activeInHierarchy &&
                !IsEssentialObject(obj) &&
                obj.name.Contains("(Clone)") &&
                obj.transform.parent == null)
            {
                obj.tag = "ToDestroy";
                markedCount++;
            }

            processedThisFrame++;
        }

        if (markedCount > 0)
        {
            yield return StartCoroutine(SafeDestroyMarkedObjectsSmooth());
        }
    }

    private System.Collections.IEnumerator SafeDestroyMarkedObjectsSmooth()
    {
        GameObject[] markedObjects = GameObject.FindGameObjectsWithTag("ToDestroy");
        int destroyedCount = 0;

        foreach (GameObject obj in markedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
                destroyedCount++;

                // ลบครั้งละ 3 objects แล้วรอ frame
                if (destroyedCount % 3 == 0)
                {
                    yield return null;
                }
            }
        }

        if (destroyedCount > 0)
        {
            Debug.Log($"🗑️ Smoothly destroyed {destroyedCount} objects");
        }
    }

    private System.Collections.IEnumerator ClearAudioCacheSmooth()
    {
        AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
        int processedThisFrame = 0;
        int stoppedCount = 0;

        foreach (AudioSource source in audioSources)
        {
            if (source == null) continue;

            // ทำครั้งละ 5 audio sources แล้วรอ frame
            if (processedThisFrame >= 5)
            {
                processedThisFrame = 0;
                yield return null;
            }

            if (source.isPlaying && source.time > 300f)
            {
                source.Stop();
                stoppedCount++;
            }

            if (!source.isPlaying && source.clip != null)
            {
                string clipName = source.clip.name.ToLower();
                if (!clipName.Contains("music") &&
                    !clipName.Contains("bgm") &&
                    !clipName.Contains("background"))
                {
                    source.clip.UnloadAudioData();
                }
            }

            processedThisFrame++;
        }

        if (stoppedCount > 0)
        {
            Debug.Log($"🔊 Smoothly cleaned {stoppedCount} audio sources");
        }
    }

    private System.Collections.IEnumerator ClearAudioCacheSmoothLight()
    {
        // เวอร์ชันเบาๆ ทำแค่สิ่งจำเป็น
        AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
        int stoppedCount = 0;

        foreach (AudioSource source in audioSources)
        {
            if (source == null) continue;

            // หยุดแค่ audio ที่เล่นนานมากๆ
            if (source.isPlaying && source.time > 600f) // 10 นาที
            {
                source.Stop();
                stoppedCount++;
            }

            // รอทุก 10 sources
            if (stoppedCount % 10 == 0 && stoppedCount > 0)
            {
                yield return null;
            }
        }
    }

    private System.Collections.IEnumerator SafeResourceCleanupSmooth()
    {
        // ทำความสะอาดทีละขั้นตอนและรอ frame
        for (int i = 0; i < 3; i++)
        {
            Debug.Log($"🔄 Resource cleanup step {i + 1}/3");

            Resources.UnloadUnusedAssets();
            yield return null; // รอ 1 frame

            System.GC.Collect();
            yield return null; // รอ 1 frame

            // พักแป๊บนึง
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void CheckMemoryAndCleanup()
    {
        try
        {
            long totalMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory();
            long totalMemoryMB = totalMemory / (1024 * 1024);

            if (totalMemoryMB > criticalMemoryMB)
            {
                Debug.LogWarning($"🚨 CRITICAL MEMORY in {currentSceneName}: {totalMemoryMB} MB!");
                SafeEmergencyCleanup();
            }
            else if (totalMemoryMB > memoryThresholdMB)
            {
                Debug.LogWarning($"⚠️ HIGH MEMORY in {currentSceneName}: {totalMemoryMB} MB!");
                DoLightCleanup(); // ใช้ light cleanup แทน
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Memory check error: {e.Message}");
        }
    }

    // Static methods สำหรับเรียกใช้จาก script อื่น
    public static void ForceCleanup()
    {
        if (Instance != null)
        {
            Instance.DoQuickCleanup();
        }
    }

    public static void ForceEmergencyCleanup()
    {
        if (Instance != null)
        {
            Instance.SafeEmergencyCleanup();
        }
    }

    public static long GetCurrentMemoryMB()
    {
        try
        {
            return UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory() / (1024 * 1024);
        }
        catch
        {
            return -1;
        }
    }

    // Copy ทุก methods จาก QuickMemoryFix.cs มาใส่ตรงนี้...
    // (SafeResourceCleanup, CleanupParticleSystems, ฯลฯ)

    // ลบ methods เก่าที่ไม่ใช้แล้ว และแทนที่ด้วย method ใหม่
    private void SafeResourceCleanup()
    {
        // ใช้ coroutine version แทน
        StartCoroutine(SafeResourceCleanupSmooth());
    }

    private void CleanupParticleSystems()
    {
        // ใช้ coroutine version แทน
        StartCoroutine(CleanupParticleSystemsSmooth());
    }

    private void CleanupInactiveObjectsSafe()
    {
        // ใช้ coroutine version แทน
        StartCoroutine(CleanupInactiveObjectsSmooth());
    }

    private void ClearAudioCacheSafe()
    {
        // ใช้ coroutine version แทน
        StartCoroutine(ClearAudioCacheSmooth());
    }

    private bool IsEssentialObject(GameObject obj)
    {
        if (obj == null) return false;

        string name = obj.name.ToLower();
        return name.Contains("player") ||
               name.Contains("camera") ||
               name.Contains("manager") ||
               name.Contains("canvas") ||
               name.Contains("light") ||
               name.Contains("eventsystem") ||
               obj.CompareTag("Player") ||
               obj.GetComponent<Camera>() != null ||
               obj.GetComponent<AudioListener>() != null ||
               obj.GetComponent<Canvas>() != null;
    }

    private System.Collections.IEnumerator SafeDestroyMarkedObjects()
    {
        GameObject[] markedObjects = GameObject.FindGameObjectsWithTag("ToDestroy");
        int destroyedCount = 0;

        foreach (GameObject obj in markedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
                destroyedCount++;

                if (destroyedCount % 5 == 0)
                {
                    yield return null;
                }
            }
        }

        if (destroyedCount > 0)
        {
            Debug.Log($"🗑️ Safely destroyed {destroyedCount} objects");
        }

        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }

   

    public void SafeEmergencyCleanup()
    {
        Debug.Log($"🚨 AGGRESSIVE Emergency cleanup in {currentSceneName}!");

        // หยุดทุกอย่างที่ไม่จำเป็นทันที
        StopAllNonEssentialSystems();

        StartCoroutine(AggressiveEmergencyCleanupCoroutine());
    }

    private void StopAllNonEssentialSystems()
    {
        // หยุด particle systems ทั้งหมด
        ParticleSystem[] allPS = FindObjectsOfType<ParticleSystem>();
        foreach (var ps in allPS)
        {
            if (ps != null && ps.isPlaying)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Clear();
            }
        }

        // หยุด animations ที่ไม่สำคัญ
        Animator[] animators = FindObjectsOfType<Animator>();
        foreach (var animator in animators)
        {
            if (animator != null && !IsEssentialObject(animator.gameObject))
            {
                animator.enabled = false;
            }
        }

        // หยุด audio ที่ไม่สำคัญ
        AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
        foreach (var source in audioSources)
        {
            if (source != null && source.isPlaying)
            {
                string clipName = source.clip?.name.ToLower() ?? "";
                if (!clipName.Contains("music") && !clipName.Contains("bgm"))
                {
                    source.Stop();
                    if (source.clip != null)
                    {
                        source.clip.UnloadAudioData();
                    }
                }
            }
        }
    }

    private System.Collections.IEnumerator AggressiveEmergencyCleanupCoroutine()
    {
        Debug.Log("🔥 Starting aggressive cleanup...");

        // 1. Force unload assets ที่ไม่ได้ใช้
        yield return StartCoroutine(ForceUnloadUnusedAssets());

        // 2. Clear render textures
        yield return StartCoroutine(ClearRenderTextures());

        // 3. Destroy pooled objects aggressively
        yield return StartCoroutine(AggressiveObjectCleanup());

        // 4. Clear material cache
        yield return StartCoroutine(ClearMaterialCache());

        // 5. Multiple rounds of deep cleanup
        for (int i = 0; i < 7; i++)
        {
            Debug.Log($"🔄 Deep cleanup round {i + 1}/7");

            Resources.UnloadUnusedAssets();
            yield return new WaitForSeconds(0.2f);

            System.GC.Collect();
            yield return null;

            System.GC.WaitForPendingFinalizers();
            yield return null;

            System.GC.Collect();
            yield return new WaitForSeconds(0.1f);
        }

        // 6. Final desperate measures
        yield return StartCoroutine(DesperateMeasures());

        Debug.Log("✅ Aggressive emergency cleanup completed!");
        LogCurrentMemory();
    }

    private System.Collections.IEnumerator ForceUnloadUnusedAssets()
    {
        Debug.Log("🗑️ Force unloading unused assets...");

        // หา textures ที่ไม่ได้ใช้
        Texture2D[] allTextures = Resources.FindObjectsOfTypeAll<Texture2D>();
        int unloadedTextures = 0;

        for (int i = 0; i < allTextures.Length; i++)
        {
            Texture2D tex = allTextures[i];
            if (tex != null && !IsTextureInUse(tex))
            {
                Resources.UnloadAsset(tex);
                unloadedTextures++;

                if (i % 20 == 0) yield return null; // รอทุก 20 textures
            }
        }

        Debug.Log($"🖼️ Force unloaded {unloadedTextures} textures");

        // หา audio clips ที่ไม่ได้ใช้
        AudioClip[] allClips = Resources.FindObjectsOfTypeAll<AudioClip>();
        int unloadedClips = 0;

        for (int i = 0; i < allClips.Length; i++)
        {
            AudioClip clip = allClips[i];
            if (clip != null && !IsAudioClipInUse(clip))
            {
                clip.UnloadAudioData();
                Resources.UnloadAsset(clip);
                unloadedClips++;

                if (i % 10 == 0) yield return null; // รอทุก 10 clips
            }
        }

        Debug.Log($"🔊 Force unloaded {unloadedClips} audio clips");
    }

    private bool IsTextureInUse(Texture2D texture)
    {
        if (texture == null) return false;

        // เช็คชื่อ texture ที่ระบบต้องใช้
        string texName = texture.name.ToLower();
        if (texName.Contains("default") || texName.Contains("unity") ||
            texName.Contains("ui") || texName.Contains("builtin"))
            return true;

        // เช็ค UI Images
        UnityEngine.UI.Image[] images = FindObjectsOfType<UnityEngine.UI.Image>();
        foreach (var img in images)
        {
            if (img.sprite != null && img.sprite.texture == texture)
                return true;
        }

        // เช็ค Renderers ที่ active
        Renderer[] renderers = FindObjectsOfType<Renderer>();
        foreach (var renderer in renderers)
        {
            if (renderer.gameObject.activeInHierarchy &&
                renderer.material != null &&
                renderer.material.mainTexture == texture)
                return true;
        }

        return false;
    }

    private bool IsAudioClipInUse(AudioClip clip)
    {
        if (clip == null) return false;

        AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
        foreach (var source in audioSources)
        {
            if (source.clip == clip)
                return true;
        }
        return false;
    }

    private System.Collections.IEnumerator ClearRenderTextures()
    {
        Debug.Log("🎬 Clearing render textures...");

        RenderTexture[] renderTextures = Resources.FindObjectsOfTypeAll<RenderTexture>();
        int clearedCount = 0;

        foreach (RenderTexture rt in renderTextures)
        {
            if (rt != null && rt.IsCreated())
            {
                rt.Release();
                clearedCount++;
            }

            if (clearedCount % 5 == 0) yield return null;
        }

        Debug.Log($"🎬 Cleared {clearedCount} render textures");
    }

    private System.Collections.IEnumerator AggressiveObjectCleanup()
    {
        Debug.Log("🗑️ Aggressive object cleanup...");

        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        int destroyedCount = 0;

        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject obj = allObjects[i];
            if (obj == null) continue;

            // ลบ objects ที่ไม่ได้ใช้อย่างจริงจัง
            if (ShouldDestroyAggressively(obj))
            {
                Destroy(obj);
                destroyedCount++;

                if (destroyedCount % 10 == 0) yield return null;
            }

            if (i % 50 == 0) yield return null; // รอทุก 50 objects
        }

        Debug.Log($"🗑️ Aggressively destroyed {destroyedCount} objects");
    }

    private bool ShouldDestroyAggressively(GameObject obj)
    {
        if (obj == null || IsEssentialObject(obj)) return false;

        string name = obj.name.ToLower();

        // ลบ objects เหล่านี้
        return !obj.activeInHierarchy &&
               (name.Contains("(clone)") ||
                name.Contains("pooled") ||
                name.Contains("cached") ||
                name.Contains("temp") ||
                name.Contains("old") ||
                name.Contains("unused") ||
                obj.transform.childCount == 0); // objects ที่ไม่มีลูก
    }

    private System.Collections.IEnumerator ClearMaterialCache()
    {
        Debug.Log("🎨 Clearing material cache...");

        Material[] allMaterials = Resources.FindObjectsOfTypeAll<Material>();
        int unloadedCount = 0;

        for (int i = 0; i < allMaterials.Length; i++)
        {
            Material mat = allMaterials[i];
            if (mat != null && !IsMaterialInUse(mat))
            {
                Resources.UnloadAsset(mat);
                unloadedCount++;

                if (i % 15 == 0) yield return null;
            }
        }

        Debug.Log($"🎨 Unloaded {unloadedCount} unused materials");
    }

    private bool IsMaterialInUse(Material material)
    {
        if (material == null) return false;

        string matName = material.name.ToLower();
        if (matName.Contains("default") || matName.Contains("ui") ||
            matName.Contains("sprite") || matName.Contains("unity"))
            return true;

        Renderer[] renderers = FindObjectsOfType<Renderer>();
        foreach (var renderer in renderers)
        {
            if (renderer.gameObject.activeInHierarchy)
            {
                foreach (var mat in renderer.materials)
                {
                    if (mat == material) return true;
                }
            }
        }

        return false;
    }

    private System.Collections.IEnumerator DesperateMeasures()
    {
        Debug.Log("🚨 DESPERATE MEASURES!");

        // ลด quality settings ชั่วคราว
        int originalTextureLimit = QualitySettings.globalTextureMipmapLimit;
        ShadowResolution originalShadows = QualitySettings.shadowResolution;

        QualitySettings.globalTextureMipmapLimit = 3; // ลด texture quality มาก
        QualitySettings.shadowResolution = ShadowResolution.Low;
        QualitySettings.shadowDistance = 10f;

        yield return new WaitForSeconds(0.5f);

        // Force garbage collection หลายรอบ
        for (int i = 0; i < 10; i++)
        {
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            yield return null;
        }

        // คืน settings หลังจาก 3 วินาที
        yield return new WaitForSeconds(3f);

        QualitySettings.globalTextureMipmapLimit = originalTextureLimit;
        QualitySettings.shadowResolution = originalShadows;

        Debug.Log("🔧 Quality settings restored after desperate cleanup");
    }

    private System.Collections.IEnumerator EmergencyCleanupCoroutine()
    {
        for (int i = 0; i < 5; i++)
        {
            Resources.UnloadUnusedAssets();
            yield return null;

            System.GC.Collect();
            yield return null;

            System.GC.WaitForPendingFinalizers();
            yield return null;
        }

        Debug.Log("✅ Emergency cleanup completed!");
        LogCurrentMemory();
    }

    private void LogCurrentMemory()
    {
        // ใช้ StartCoroutine เพื่อไม่ให้กระตุก
        StartCoroutine(LogMemorySmooth());
    }

    private System.Collections.IEnumerator LogMemorySmooth()
    {
        // แบ่งการอ่าน memory ออกเป็นหลาย frame
        yield return null; // รอ 1 frame ก่อน

        long totalMemory = 0;

        try
        {
            totalMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Memory log error: {e.Message}");
            yield break; // จบ coroutine ทันทีถ้า error
        }

        yield return null; // รออีก 1 frame

        long totalMemoryMB = totalMemory / (1024 * 1024);

        string memoryStatus = "✅ OK";
        if (totalMemoryMB > 500)
            memoryStatus = "🚨 HIGH";
        else if (totalMemoryMB > 300)
            memoryStatus = "⚠️ MEDIUM";

        Debug.Log($"📊 {currentSceneName}: {totalMemoryMB} MB ({memoryStatus})");
    }


    // เพิ่ม method สำหรับ detailed report แยกต่างหาก
    [ContextMenu("Detailed Memory Report")]
    public void GenerateDetailedMemoryReport()
    {
        StartCoroutine(DetailedMemoryReportCoroutine());
    }

    private System.Collections.IEnumerator DetailedMemoryReportCoroutine()
    {
        Debug.Log("📋 Generating detailed memory report...");
        yield return null;

        long totalMemory = 0;
        try
        {
            totalMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Detailed report error: {e.Message}");
            yield break;
        }

        long totalMemoryMB = totalMemory / (1024 * 1024);
        yield return null;

        int gameObjectCount = 0;
        int textureCount = 0;
        int audioCount = 0;

        GameObject[] gameObjects = FindObjectsOfType<GameObject>(true);
        gameObjectCount = gameObjects.Length;
        yield return null;

        Texture2D[] textures = Resources.FindObjectsOfTypeAll<Texture2D>();
        textureCount = textures.Length;
        yield return null;

        AudioClip[] audioClips = Resources.FindObjectsOfTypeAll<AudioClip>();
        audioCount = audioClips.Length;
        yield return null;

        Debug.Log("📊 === DETAILED MEMORY REPORT ===");
        Debug.Log($"💾 Scene: {currentSceneName}");
        Debug.Log($"💾 Total Memory: {totalMemoryMB} MB");
        Debug.Log($"🎮 GameObjects: {gameObjectCount}");
        Debug.Log($"🖼️ Textures: {textureCount}");
        Debug.Log($"🔊 Audio Clips: {audioCount}");
        Debug.Log("📊 === END REPORT ===");
    }


    // Update hotkeys ใหม่
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            SafeEmergencyCleanup();
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            // Memory status แบบเร็ว ไม่กระตุก
            QuickMemoryStatus();
        }

        if (Input.GetKeyDown(KeyCode.F4))
        {
            // Detailed report แบบ coroutine
            GenerateDetailedMemoryReport();
        }
    }

    private void QuickMemoryStatus()
    {
        try
        {
            long memoryMB = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory() / (1024 * 1024);
            Debug.Log($"⚡ Quick Status: {currentSceneName} = {memoryMB} MB");
        }
        catch
        {
            Debug.Log("⚡ Quick Status: Memory read failed");
        }
    }

    void OnDestroy()
    {
        // ยกเลิก event listeners
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }
}