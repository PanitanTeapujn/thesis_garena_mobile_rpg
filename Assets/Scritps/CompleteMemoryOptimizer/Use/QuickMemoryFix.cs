using UnityEngine;
using System.Collections;

public class QuickMemoryFix : MonoBehaviour
{
    [Header("Quick Memory Fix")]
    public bool enableAutoCleanup = true;
    public float cleanupInterval = 30f; // ทำความสะอาดทุก 30 วินาที

    void Start()
    {
        Debug.Log("🧹 Quick Memory Fix started!");

        if (enableAutoCleanup)
        {
            InvokeRepeating(nameof(DoQuickCleanup), 10f, cleanupInterval);
        }

        if (enableAutoMemoryManagement)
        {
            InvokeRepeating(nameof(CheckMemoryAndCleanup), 5f, 10f); // เช็คทุก 10 วินาที
        }

        // ทำความสะอาดทันทีเมื่อเริ่มเกม
        DoQuickCleanup();
    }

    public void DoQuickCleanup()
    {
        Debug.Log("🧹 Performing SAFE enhanced memory cleanup...");

        // 1. หยุด particle systems ที่ไม่จำเป็น
        CleanupParticleSystems();

        // 2. ปิด GameObjects ที่ไม่ได้ใช้ (ไม่ใช้ DestroyImmediate)
        CleanupInactiveObjectsSafe();

        // 3. Clear audio ที่ไม่ได้ใช้อย่างปลอดภัย
        ClearAudioCacheSafe();

        // 4. ทำความสะอาด Assets อย่างปลอดภัย
        SafeResourceCleanup();

        Debug.Log("✅ Safe enhanced cleanup completed!");
        LogCurrentMemory();
    }
    private void SafeResourceCleanup()
    {
        // ทำความสะอาดแบบปลอดภัย
        for (int i = 0; i < 3; i++)
        {
            Resources.UnloadUnusedAssets();
            System.GC.Collect();

            // รอให้ GC ทำงานเสร็จ
            System.Threading.Thread.Sleep(100);
        }

        Debug.Log("🔄 Safe resource cleanup completed");
    }

    private void CleanupInactiveObjectsSafe()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        int markedCount = 0;

        foreach (GameObject obj in allObjects)
        {
            if (obj == null) continue;

            // แค่ปิด objects แทนการลบ (ปลอดภัยกว่า)
            if (!obj.activeInHierarchy &&
                !IsEssentialObject(obj) &&
                obj.name.Contains("(Clone)") &&
                obj.transform.parent == null) // เฉพาะ root objects
            {
                // แค่ mark สำหรับลบในภายหลัง
                obj.tag = "ToDestroy";
                markedCount++;
            }
        }

        // ลบ objects ที่ mark ไว้อย่างปลอดภัย
        if (markedCount > 0)
        {
            StartCoroutine(SafeDestroyMarkedObjects());
            Debug.Log($"🗑️ Marked {markedCount} objects for safe destruction");
        }
    }
    private void ClearAudioCacheSafe()
    {
        AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
        int stoppedCount = 0;

        foreach (AudioSource source in audioSources)
        {
            if (source == null) continue;

            // หยุดเฉพาะ audio ที่เล่นนานเกินไป
            if (source.isPlaying && source.time > 300f) // 5 นาที
            {
                source.Stop();
                stoppedCount++;
            }

            // ถ้าไม่ได้เล่นและไม่มี clip สำคัญ
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
        }

        Debug.Log($"🔊 Safely cleaned {stoppedCount} audio sources");
    }
    private System.Collections.IEnumerator SafeDestroyMarkedObjects()
    {
        GameObject[] markedObjects = GameObject.FindGameObjectsWithTag("ToDestroy");
        int destroyedCount = 0;

        foreach (GameObject obj in markedObjects)
        {
            if (obj != null)
            {
                Destroy(obj); // ใช้ Destroy แทน DestroyImmediate
                destroyedCount++;

                // ทำทีละ object และรอ frame
                if (destroyedCount % 5 == 0)
                {
                    yield return null;
                }
            }
        }

        Debug.Log($"🗑️ Safely destroyed {destroyedCount} objects");

        // ทำความสะอาดหลังลบ objects
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }
    private void UnloadUnusedTextures()
    {
        // หา textures ทั้งหมดรวมที่ซ่อนอยู่
        Texture2D[] allTextures = Resources.FindObjectsOfTypeAll<Texture2D>();
        int unloadedCount = 0;

        foreach (Texture2D tex in allTextures)
        {
            if (tex == null) continue;

            // ไม่ลบ textures ที่กำลังใช้ใน scene
            if (!IsTextureInUse(tex))
            {
                Resources.UnloadAsset(tex);
                unloadedCount++;
            }
        }

        // Force unload render textures
        RenderTexture[] renderTextures = Resources.FindObjectsOfTypeAll<RenderTexture>();
        foreach (RenderTexture rt in renderTextures)
        {
            if (rt != null && !rt.IsCreated())
            {
                rt.Release();
            }
        }

        Debug.Log($"🖼️ Force unloaded {unloadedCount} unused textures");
    }
    private bool IsTextureInUse(Texture2D texture)
    {
        if (texture == null) return false;

        // เช็คว่า texture กำลังใช้ใน UI
        UnityEngine.UI.Image[] images = FindObjectsOfType<UnityEngine.UI.Image>();
        foreach (var img in images)
        {
            if (img.sprite != null && img.sprite.texture == texture)
                return true;
        }

        // เช็คว่า texture กำลังใช้ใน Renderer
        Renderer[] renderers = FindObjectsOfType<Renderer>();
        foreach (var renderer in renderers)
        {
            if (renderer.material != null && renderer.material.mainTexture == texture)
                return true;
        }

        // ถ้าเป็น texture ของ Unity เอง อย่าลบ
        if (texture.name.Contains("Default") ||
            texture.name.Contains("Unity") ||
            texture.name.StartsWith("UI/"))
            return true;

        return false;
    }
    private void ClearAudioCache()
    {
        // หา audio clips ทั้งหมด
        AudioClip[] allClips = Resources.FindObjectsOfTypeAll<AudioClip>();
        int unloadedCount = 0;

        foreach (AudioClip clip in allClips)
        {
            if (clip == null) continue;

            // เช็คว่ากำลังเล่นอยู่หรือไม่
            if (!IsAudioClipPlaying(clip))
            {
                clip.UnloadAudioData();
                Resources.UnloadAsset(clip);
                unloadedCount++;
            }
        }

        Debug.Log($"🔊 Cleared {unloadedCount} audio clips from cache");
    }

    private bool IsAudioClipPlaying(AudioClip clip)
    {
        AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
        foreach (var source in audioSources)
        {
            if (source.clip == clip && source.isPlaying)
                return true;
        }
        return false;
    }

    private void ClearShaderCache()
    {
        // Clear shader cache (compatible with all Unity versions)
        try
        {
            Shader.WarmupAllShaders();
            System.GC.Collect();
        }
        catch (System.Exception)
        {
            // Skip if not supported
        }

        // หา materials ที่ไม่ได้ใช้
        Material[] allMaterials = Resources.FindObjectsOfTypeAll<Material>();
        int unloadedCount = 0;

        foreach (Material mat in allMaterials)
        {
            if (mat == null) continue;

            // ไม่ลบ material ที่กำลังใช้
            if (!IsMaterialInUse(mat) && !mat.name.Contains("Default") && !mat.name.Contains("UI"))
            {
                Resources.UnloadAsset(mat);
                unloadedCount++;
            }
        }

        Debug.Log($"🎨 Cleared shader cache and {unloadedCount} unused materials");
    }

    private bool IsMaterialInUse(Material material)
    {
        if (material == null) return false;

        Renderer[] renderers = FindObjectsOfType<Renderer>();
        foreach (var renderer in renderers)
        {
            foreach (var mat in renderer.materials)
            {
                if (mat == material) return true;
            }
        }

        return false;
    }
    private void CleanupParticleSystems()
    {
        ParticleSystem[] allPS = FindObjectsOfType<ParticleSystem>();
        int cleanedCount = 0;

        foreach (ParticleSystem ps in allPS)
        {
            if (ps == null) continue;

            // หยุด particle systems ที่ไม่มี particles
            if (ps.isPlaying && ps.particleCount == 0)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                cleanedCount++;
            }

            // หยุด particle systems ที่เล่นนานมาก
            if (ps.isPlaying && ps.time > 60f)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                cleanedCount++;
            }
        }

        if (cleanedCount > 0)
        {
            Debug.Log($"🧹 Cleaned {cleanedCount} particle systems");
        }
    }

    private void CleanupInactiveObjects()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true); // รวม inactive
        int destroyedCount = 0;

        foreach (GameObject obj in allObjects)
        {
            if (obj == null) continue;

            // ลบ objects ที่ไม่ได้ใช้งานและไม่สำคัญ
            if (!obj.activeInHierarchy &&
                !IsEssentialObject(obj) &&
                obj.name.Contains("(Clone)"))
            {
                Destroy(obj);
                destroyedCount++;
            }
        }

        if (destroyedCount > 0)
        {
            Debug.Log($"🗑️ Destroyed {destroyedCount} unused objects");
        }
    }

    private bool IsEssentialObject(GameObject obj)
    {
        if (obj == null) return false;

        // ไม่ลบ objects สำคัญ
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

    private void LogCurrentMemory()
    {
        // ใช้วิธีที่ง่ายกว่าในการเช็ค memory
        try
        {
            // สำหรับ Unity เวอร์ชันใหม่
            long totalMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory();
            long totalMemoryMB = totalMemory / (1024 * 1024);

            Debug.Log($"📊 Current Memory: {totalMemoryMB} MB");

            if (totalMemoryMB > 500)
            {
                Debug.LogError($"🚨 HIGH MEMORY: {totalMemoryMB} MB!");
            }
            else if (totalMemoryMB > 300)
            {
                Debug.LogWarning($"⚠️ Memory usage: {totalMemoryMB} MB");
            }
            else
            {
                Debug.Log($"✅ Memory OK: {totalMemoryMB} MB");
            }
        }
        catch (System.Exception)
        {
            // สำหรับ Unity เวอร์ชันเก่า - ใช้วิธีอื่น
            int textureCount = Resources.FindObjectsOfTypeAll<Texture2D>().Length;
            int gameObjectCount = FindObjectsOfType<GameObject>().Length;

            Debug.Log($"📊 Objects: {gameObjectCount} GameObjects, {textureCount} Textures");
        }
    }

    [ContextMenu("Force Quick Cleanup")]
    public void ForceQuickCleanup()
    {
        DoQuickCleanup();
    }

    [ContextMenu("Count Objects")]
    public void CountObjects()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        Texture2D[] allTextures = Resources.FindObjectsOfTypeAll<Texture2D>();
        ParticleSystem[] allPS = FindObjectsOfType<ParticleSystem>();

        int playingPS = 0;
        foreach (var ps in allPS)
        {
            if (ps.isPlaying) playingPS++;
        }

        Debug.Log($"📊 Object Count:");
        Debug.Log($"   GameObjects: {allObjects.Length}");
        Debug.Log($"   Textures: {allTextures.Length}");
        Debug.Log($"   Particle Systems: {playingPS}/{allPS.Length} playing");

        // แสดง objects ที่มีมากที่สุด
        System.Collections.Generic.Dictionary<string, int> objectCounts =
            new System.Collections.Generic.Dictionary<string, int>();

        foreach (GameObject obj in allObjects)
        {
            if (obj == null) continue;

            string typeName = obj.name.Split('(')[0];
            if (objectCounts.ContainsKey(typeName))
                objectCounts[typeName]++;
            else
                objectCounts[typeName] = 1;
        }

        Debug.Log("🔍 Top Object Types:");
        var sortedObjects = new System.Collections.Generic.List<
            System.Collections.Generic.KeyValuePair<string, int>>(objectCounts);
        sortedObjects.Sort((x, y) => y.Value.CompareTo(x.Value));

        for (int i = 0; i < Mathf.Min(5, sortedObjects.Count); i++)
        {
            Debug.Log($"   {sortedObjects[i].Key}: {sortedObjects[i].Value} objects");
        }
    }

    [ContextMenu("Emergency Cleanup")]
    public void EmergencyCleanup()
    {
        Debug.Log("🚨 EMERGENCY CLEANUP!");

        // หยุด particle systems ทั้งหมด
        ParticleSystem[] allPS = FindObjectsOfType<ParticleSystem>();
        foreach (var ps in allPS)
        {
            if (ps.isPlaying)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        // ทำความสะอาดหลายรอบ
        for (int i = 0; i < 5; i++)
        {
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        Debug.Log("✅ Emergency cleanup completed!");
        LogCurrentMemory();
    }

    void Update()
    {
        // เช็คปุ่ม F2 สำหรับ safe emergency cleanup
        if (Input.GetKeyDown(KeyCode.F2))
        {
            SafeEmergencyCleanup();
        }

        // เช็คปุ่ม F3 สำหรับ memory report
        if (Input.GetKeyDown(KeyCode.F3))
        {
            GenerateMemoryReport();
        }

        // เช็คปุ่ม F4 สำหรับ mobile optimization
        if (Input.GetKeyDown(KeyCode.F4))
        {
            OptimizeForMobile();
        }

        // เช็คปุ่ม F5 สำหรับ performance mode
        if (Input.GetKeyDown(KeyCode.F5))
        {
            EnablePerformanceMode();
        }
    }

    [ContextMenu("Emergency Deep Cleanup")]
    public void EmergencyDeepCleanup()
    {
        Debug.Log("🚨 EMERGENCY DEEP CLEANUP!");

        // 1. หยุด particle systems ทั้งหมด
        ParticleSystem[] allPS = FindObjectsOfType<ParticleSystem>();
        foreach (var ps in allPS)
        {
            if (ps != null && ps.isPlaying)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Clear();
            }
        }

        // 2. Force unload ทุกอย่างที่ไม่จำเป็น
        UnloadUnusedTextures();
        ClearAudioCache();
        ClearShaderCache();

        // 3. Destroy pooled objects
        DestroyPooledObjects();

        // 4. Clear animation cache
        ClearAnimationCache();

        // 5. ทำความสะอาดหลายรอบพร้อม GC
        for (int i = 0; i < 5; i++)
        {
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
        }

        Debug.Log("✅ Emergency deep cleanup completed!");
        LogCurrentMemory();
    }

    private void DestroyPooledObjects()
    {
        // หา objects ที่มีชื่อแบบ pooled
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        int destroyedCount = 0;

        foreach (GameObject obj in allObjects)
        {
            if (obj == null) continue;

            string name = obj.name.ToLower();

            // ลบ objects ที่เป็น pooled หรือ cached
            if ((name.Contains("pooled") ||
                 name.Contains("cached") ||
                 name.Contains("(clone)")) &&
                !obj.activeInHierarchy &&
                !IsEssentialObject(obj))
            {
                DestroyImmediate(obj);
                destroyedCount++;
            }
        }

        Debug.Log($"🗑️ Destroyed {destroyedCount} pooled objects");
    }

    private void ClearAnimationCache()
    {
        // Clear animator cache
        Animator[] animators = FindObjectsOfType<Animator>();
        foreach (var animator in animators)
        {
            if (animator != null && !animator.gameObject.activeInHierarchy)
            {
                animator.Rebind();
            }
        }

        // หา animation clips ที่ไม่ได้ใช้
        AnimationClip[] clips = Resources.FindObjectsOfTypeAll<AnimationClip>();
        int unloadedCount = 0;

        foreach (var clip in clips)
        {
            if (clip != null && !IsAnimationClipInUse(clip))
            {
                Resources.UnloadAsset(clip);
                unloadedCount++;
            }
        }

        Debug.Log($"🎬 Cleared {unloadedCount} animation clips from cache");
    }

    private bool IsAnimationClipInUse(AnimationClip clip)
    {
        Animator[] animators = FindObjectsOfType<Animator>();
        foreach (var animator in animators)
        {
            if (animator.runtimeAnimatorController != null)
            {
                foreach (var animClip in animator.runtimeAnimatorController.animationClips)
                {
                    if (animClip == clip) return true;
                }
            }
        }
        return false;
    }

    // เพิ่ม method สำหรับ log memory แบบละเอียดขึ้น
    private void LogDetailedMemory()
    {
        try
        {
            long totalMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory();
            long totalMemoryMB = totalMemory / (1024 * 1024);

            // นับ assets ต่างๆ
            int textureCount = Resources.FindObjectsOfTypeAll<Texture2D>().Length;
            int audioCount = Resources.FindObjectsOfTypeAll<AudioClip>().Length;
            int materialCount = Resources.FindObjectsOfTypeAll<Material>().Length;
            int meshCount = Resources.FindObjectsOfTypeAll<Mesh>().Length;

            Debug.Log($"📊 Detailed Memory Report:");
            Debug.Log($"   Total Memory: {totalMemoryMB} MB");
            Debug.Log($"   Textures: {textureCount}");
            Debug.Log($"   Audio Clips: {audioCount}");
            Debug.Log($"   Materials: {materialCount}");
            Debug.Log($"   Meshes: {meshCount}");

            if (totalMemoryMB > 500)
            {
                Debug.LogError($"🚨 HIGH MEMORY: {totalMemoryMB} MB! Consider deep cleanup.");
            }
            else if (totalMemoryMB > 300)
            {
                Debug.LogWarning($"⚠️ Memory usage: {totalMemoryMB} MB");
            }
            else
            {
                Debug.Log($"✅ Memory OK: {totalMemoryMB} MB");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Cannot read memory: {e.Message}");
        }
    }
    [ContextMenu("Safe Emergency Cleanup")]
    public void SafeEmergencyCleanup()
    {
        Debug.Log("🚨 SAFE EMERGENCY CLEANUP!");

        // 1. หยุด particle systems ทั้งหมด
        ParticleSystem[] allPS = FindObjectsOfType<ParticleSystem>();
        foreach (var ps in allPS)
        {
            if (ps != null && ps.isPlaying)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Clear();
            }
        }

        // 2. หยุด animations ที่ไม่จำเป็น
        Animator[] animators = FindObjectsOfType<Animator>();
        foreach (var animator in animators)
        {
            if (animator != null && !animator.gameObject.activeInHierarchy)
            {
                animator.enabled = false;
            }
        }

        // 3. ลด quality settings ชั่วคราว
        ReduceQualityTemporary();

        // 4. ทำความสะอาดหลายรอบแบบปลอดภัย
        StartCoroutine(EmergencyCleanupCoroutine());
    }

    private void ReduceQualityTemporary()
    {
        // ลด texture quality ชั่วคราว
        QualitySettings.globalTextureMipmapLimit = 2;

        // ลด shadow quality
        QualitySettings.shadowResolution = ShadowResolution.Low;

        // ปิด vsync
        QualitySettings.vSyncCount = 0;

        Debug.Log("📉 Temporarily reduced quality settings");
    }

    private System.Collections.IEnumerator EmergencyCleanupCoroutine()
    {
        for (int i = 0; i < 5; i++)
        {
            Debug.Log($"🔄 Emergency cleanup round {i + 1}/5");

            Resources.UnloadUnusedAssets();
            yield return null; // รอ 1 frame

            System.GC.Collect();
            yield return null; // รอ 1 frame

            System.GC.WaitForPendingFinalizers();
            yield return null; // รอ 1 frame
        }

        Debug.Log("✅ Safe emergency cleanup completed!");
        LogCurrentMemory();

        // คืน quality settings หลังจาก 5 วินาที
        Invoke(nameof(RestoreQualitySettings), 5f);
    }

    private void RestoreQualitySettings()
    {
        QualitySettings.globalTextureMipmapLimit = 0;
        QualitySettings.shadowResolution = ShadowResolution.Medium;
        QualitySettings.vSyncCount = 1;

        Debug.Log("📈 Quality settings restored");
    }

    // เพิ่ม method สำหรับ monitor memory แบบ realtime
    [ContextMenu("Start Memory Monitor")]
    public void StartMemoryMonitor()
    {
        InvokeRepeating(nameof(MonitorMemory), 1f, 5f);
        Debug.Log("📊 Memory monitor started (every 5 seconds)");
    }

    [ContextMenu("Stop Memory Monitor")]
    public void StopMemoryMonitor()
    {
        CancelInvoke(nameof(MonitorMemory));
        Debug.Log("📊 Memory monitor stopped");
    }

    private void MonitorMemory()
    {
        try
        {
            long totalMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory();
            long totalMemoryMB = totalMemory / (1024 * 1024);

            if (totalMemoryMB > 600)
            {
                Debug.LogWarning($"🚨 HIGH MEMORY DETECTED: {totalMemoryMB} MB - Auto cleanup triggered!");
                DoQuickCleanup();
            }
            else
            {
                Debug.Log($"📊 Memory: {totalMemoryMB} MB");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Memory monitor error: {e.Message}");
        }
    }
    [Header("Auto Memory Management")]
    public bool enableAutoMemoryManagement = true;
    public int memoryThresholdMB = 500; // เริ่ม cleanup เมื่อ memory เกิน 500 MB
    public int criticalMemoryMB = 700;  // emergency cleanup เมื่อ memory เกิน 700 MB

  
    private void CheckMemoryAndCleanup()
    {
        try
        {
            long totalMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory();
            long totalMemoryMB = totalMemory / (1024 * 1024);

            if (totalMemoryMB > criticalMemoryMB)
            {
                Debug.LogWarning($"🚨 CRITICAL MEMORY: {totalMemoryMB} MB! Emergency cleanup!");
                SafeEmergencyCleanup();
            }
            else if (totalMemoryMB > memoryThresholdMB)
            {
                Debug.LogWarning($"⚠️ HIGH MEMORY: {totalMemoryMB} MB! Auto cleanup triggered!");
                DoQuickCleanup();
            }
            else
            {
                Debug.Log($"✅ Memory OK: {totalMemoryMB} MB");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Memory check error: {e.Message}");
        }
    }

    [ContextMenu("Optimize for Mobile")]
    public void OptimizeForMobile()
    {
        Debug.Log("📱 Optimizing for mobile devices...");

        // ลด quality สำหรับมือถือ
        QualitySettings.globalTextureMipmapLimit = 1;
        QualitySettings.shadowResolution = ShadowResolution.Low;
        QualitySettings.shadowDistance = 20f;
        QualitySettings.pixelLightCount = 1;

        // ลด framerate target
        Application.targetFrameRate = 30;

        // ปิด vsync
        QualitySettings.vSyncCount = 0;

        // ทำความสะอาด memory
        DoQuickCleanup();

        Debug.Log("📱 Mobile optimization completed!");
    }

    [ContextMenu("Performance Mode")]
    public void EnablePerformanceMode()
    {
        Debug.Log("⚡ Enabling performance mode...");

        // เพิ่ม framerate
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;

        // ปรับ quality สำหรับ performance
        QualitySettings.globalTextureMipmapLimit = 0;
        QualitySettings.shadowResolution = ShadowResolution.Medium;
        QualitySettings.pixelLightCount = 2;

        // เปิด batch processing
        if (Camera.main != null)
        {
            Camera.main.allowDynamicResolution = true;
        }

        Debug.Log("⚡ Performance mode enabled!");
    }

    [ContextMenu("Memory Report")]
    public void GenerateMemoryReport()
    {
        Debug.Log("📋 Generating detailed memory report...");

        try
        {
            long totalMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory();
            long totalMemoryMB = totalMemory / (1024 * 1024);

            // นับ objects ต่างๆ
            GameObject[] gameObjects = FindObjectsOfType<GameObject>(true);
            Texture2D[] textures = Resources.FindObjectsOfTypeAll<Texture2D>();
            AudioClip[] audioClips = Resources.FindObjectsOfTypeAll<AudioClip>();
            Material[] materials = Resources.FindObjectsOfTypeAll<Material>();
            Mesh[] meshes = Resources.FindObjectsOfTypeAll<Mesh>();
            ParticleSystem[] particleSystems = FindObjectsOfType<ParticleSystem>();

            int activeGameObjects = FindObjectsOfType<GameObject>().Length;
            int playingParticles = 0;

            foreach (var ps in particleSystems)
            {
                if (ps.isPlaying) playingParticles++;
            }

            Debug.Log("📊 === MEMORY REPORT ===");
            Debug.Log($"💾 Total Memory: {totalMemoryMB} MB");
            Debug.Log($"🎮 GameObjects: {activeGameObjects} active / {gameObjects.Length} total");
            Debug.Log($"🖼️ Textures: {textures.Length}");
            Debug.Log($"🔊 Audio Clips: {audioClips.Length}");
            Debug.Log($"🎨 Materials: {materials.Length}");
            Debug.Log($"📐 Meshes: {meshes.Length}");
            Debug.Log($"✨ Particle Systems: {playingParticles} playing / {particleSystems.Length} total");

            // Memory status
            string memoryStatus = "✅ GOOD";
            if (totalMemoryMB > criticalMemoryMB)
                memoryStatus = "🚨 CRITICAL";
            else if (totalMemoryMB > memoryThresholdMB)
                memoryStatus = "⚠️ HIGH";

            Debug.Log($"📊 Memory Status: {memoryStatus}");
            Debug.Log("📋 === END REPORT ===");

        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Report generation error: {e.Message}");
        }
    }
    // Update method ที่ปลอดภัยกว่า

}