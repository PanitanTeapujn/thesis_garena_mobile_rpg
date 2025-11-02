using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    [SerializeField]
    private AudioSource[] bgm;
    public AudioSource[] Bgm { get { return bgm; } }

    [SerializeField]
    private AudioSource[] sfx;
    public AudioSource[] Sfx { get { return sfx; } }

    [SerializeField]
    private AudioMixer audioMixer;

    public static AudioManager instance;

    [Header("🎵 BGM Settings")]
    [SerializeField] private SceneBGMMapping[] sceneBGMMapping;

    // ============================================
    // 🆕 Enemy Hit Sound System - Multiple Types
    // ============================================
    [Header("🔊 Enemy Hit Sound Settings")]
    [Tooltip("การตั้งค่าเสียงสำหรับแต่ละประเภท Enemy")]
    [SerializeField] private EnemyHitSoundMapping[] enemyHitSounds;

    [Header("⏱️ Global Hit Sound Settings")]
    [Tooltip("ระยะเวลาขั้นต่ำระหว่างการเล่นเสียงใดๆ (วินาที)")]
    [SerializeField] private float globalHitSoundCooldown = 0.1f;

    [Tooltip("จำนวนเสียง Hit ทั้งหมดที่เล่นพร้อมกันได้")]
    [SerializeField] private int maxTotalSimultaneousSounds = 5;

    [Header("📊 Volume & Pitch Settings")]
    [Tooltip("ปรับ volume ตามจำนวนเสียงที่เล่นพร้อมกัน")]
    [SerializeField] private bool useGlobalVolumeScaling = true;

    [SerializeField] private float minGlobalVolume = 0.2f;
    [SerializeField] private float maxGlobalVolume = 0.9f;

    [Tooltip("สุ่ม pitch เพื่อให้เสียงหลากหลาย")]
    [SerializeField] private bool useGlobalPitchRandomization = true;
    [SerializeField] private float globalMinPitch = 0.85f;
    [SerializeField] private float globalMaxPitch = 1.15f;

    // 🆕 Enemy Hit Sound Mapping Class
    [System.Serializable]
    public class EnemyHitSoundMapping
    {
        [Header("🎯 Enemy Type")]
        [Tooltip("ชื่อประเภท Enemy (ต้องตรงกับ CharacterName หรือ Tag)")]
        public string enemyTypeName;

        [Tooltip("ใช้ Tag แทนชื่อ? (ถ้าเช็ค จะใช้ GameObject.tag)")]
        public bool useTag = false;

        [Header("🔊 Sound Settings")]
        [Tooltip("SFX Index ที่จะเล่นเมื่อ Enemy ประเภทนี้โดนโจมตี")]
        public int sfxIndex = 0;

        [Tooltip("Volume เฉพาะของ Enemy ประเภทนี้ (0-1)")]
        [Range(0f, 1f)]
        public float volume = 0.8f;

        [Header("⏱️ Cooldown Override")]
        [Tooltip("ใช้ Cooldown เฉพาะ? (ถ้าไม่เช็คจะใช้ global)")]
        public bool useCustomCooldown = false;

        [Tooltip("Cooldown เฉพาะของ Enemy ประเภทนี้")]
        public float customCooldown = 0.15f;

        [Header("🎲 Variation")]
        [Tooltip("สุ่ม Pitch เฉพาะ?")]
        public bool useCustomPitch = false;
        public float minPitch = 0.9f;
        public float maxPitch = 1.1f;

        [Header("📊 Per-Type Limiting")]
        [Tooltip("จำกัดจำนวนเสียงประเภทนี้ที่เล่นพร้อมกัน")]
        public bool limitPerType = false;
        public int maxPerTypeSimultaneous = 2;

        // Runtime tracking
        [System.NonSerialized]
        public float lastPlayTime = 0f;
        [System.NonSerialized]
        public Queue<float> recentHits = new Queue<float>();
    }

    // 🆕 Default fallback sound
    [Header("🔧 Fallback Settings")]
    [Tooltip("SFX Index สำรับ Enemy ที่ไม่มีการตั้งค่า")]
    [SerializeField] private int defaultEnemyHitSFX = 5;
    [SerializeField] private float defaultEnemyHitVolume = 0.7f;

    // Private variables
    private Queue<float> allRecentHits = new Queue<float>();
    private const float HIT_TRACKING_DURATION = 0.5f;

    [System.Serializable]
    public class SceneBGMMapping
    {
        public string sceneName;
        public int bgmIndex;
        [Tooltip("Fade in duration (seconds)")]
        public float fadeInDuration = 1f;
    }

    private int currentBGMIndex = -1;
    private bool isFading = false;

    private float currentBGMVolume = 0.7f;
    private float currentSFXVolume = 1f;
    private float currentMasterVolume = 1f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        instance = this;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        SetMasterVolume(1f);
        SetSFXVolume(1f);
        SetBGMVolume(0.7f);

        string currentScene = SceneManager.GetActiveScene().name;
        PlayBGMForScene(currentScene);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ============================================
    // 🆕 Enhanced Enemy Hit Sound System
    // ============================================

    /// <summary>
    /// เล่นเสียง Enemy โดนโจมตี (มีระบบควบคุมความถี่ + รองรับหลายประเภท)
    /// เรียกใช้จาก NetworkEnemy หรือ CombatManager
    /// </summary>
    /// <param name="enemyCharacter">Character ของ Enemy ที่โดนโจมตี</param>
    public void PlayEnemyHitSound(Character enemyCharacter)
    {
        if (enemyCharacter == null)
        {
            Debug.LogWarning("⚠️ PlayEnemyHitSound: Enemy character is null!");
            return;
        }

        // หา sound mapping ที่ตรงกับ enemy นี้
        EnemyHitSoundMapping soundMapping = FindEnemyHitSoundMapping(enemyCharacter);

        if (soundMapping != null)
        {
            PlayEnemyHitSoundWithMapping(soundMapping);
        }
        else
        {
            // ใช้เสียง default
            PlayDefaultEnemyHitSound();
        }
    }

    /// <summary>
    /// เล่นเสียง Enemy โดนโจมตี (overload ใช้ชื่อ/tag โดยตรง)
    /// </summary>
    public void PlayEnemyHitSound(string enemyTypeNameOrTag, bool isTag = false)
    {
        EnemyHitSoundMapping soundMapping = null;

        foreach (var mapping in enemyHitSounds)
        {
            if (isTag && mapping.useTag && mapping.enemyTypeName == enemyTypeNameOrTag)
            {
                soundMapping = mapping;
                break;
            }
            else if (!isTag && !mapping.useTag && mapping.enemyTypeName == enemyTypeNameOrTag)
            {
                soundMapping = mapping;
                break;
            }
        }

        if (soundMapping != null)
        {
            PlayEnemyHitSoundWithMapping(soundMapping);
        }
        else
        {
            PlayDefaultEnemyHitSound();
        }
    }

    /// <summary>
    /// หา Sound Mapping ที่ตรงกับ Enemy
    /// </summary>
    private EnemyHitSoundMapping FindEnemyHitSoundMapping(Character enemyCharacter)
    {
        foreach (var mapping in enemyHitSounds)
        {
            if (mapping.useTag)
            {
                // ใช้ Tag ในการเช็ค
                if (enemyCharacter.gameObject.CompareTag(mapping.enemyTypeName))
                {
                    return mapping;
                }
            }
            else
            {
                // ใช้ CharacterName ในการเช็ค
                if (enemyCharacter.CharacterName == mapping.enemyTypeName)
                {
                    return mapping;
                }
            }
        }

        return null; // ไม่เจอ mapping ที่ตรงกัน
    }

    /// <summary>
    /// เล่นเสียงตาม Mapping ที่กำหนด
    /// </summary>
    private void PlayEnemyHitSoundWithMapping(EnemyHitSoundMapping mapping)
    {
        float cooldown = mapping.useCustomCooldown ? mapping.customCooldown : globalHitSoundCooldown;

        // เช็ค global cooldown
        if (Time.time - mapping.lastPlayTime < cooldown)
        {
            return;
        }

        // ลบข้อมูลเก่า
        CleanupOldHits(mapping.recentHits);
        CleanupOldHits(allRecentHits);

        // เช็คจำนวนเสียงรวมทั้งหมด
        if (allRecentHits.Count >= maxTotalSimultaneousSounds)
        {
            return;
        }

        // เช็คจำนวนเสียงเฉพาะประเภทนี้
        if (mapping.limitPerType && mapping.recentHits.Count >= mapping.maxPerTypeSimultaneous)
        {
            return;
        }

        // คำนวณ volume
        float volume = CalculateDynamicVolume(mapping);

        // คำนวณ pitch
        float pitch = CalculatePitch(mapping);

        // เล่นเสียง
        PlaySFXWithPitch(mapping.sfxIndex, volume, pitch);

        // อัปเดทข้อมูล
        mapping.lastPlayTime = Time.time;
        mapping.recentHits.Enqueue(Time.time);
        allRecentHits.Enqueue(Time.time);

        Debug.Log($"🔊 Enemy Hit: {mapping.enemyTypeName} | SFX[{mapping.sfxIndex}] | Vol={volume:F2} | Pitch={pitch:F2} | Active={allRecentHits.Count}/{maxTotalSimultaneousSounds}");
    }

    /// <summary>
    /// เล่นเสียง Default เมื่อไม่เจอ Mapping
    /// </summary>
    private void PlayDefaultEnemyHitSound()
    {
        // เช็ค global cooldown แบบง่าย
        CleanupOldHits(allRecentHits);

        if (allRecentHits.Count >= maxTotalSimultaneousSounds)
        {
            return;
        }

        float volume = useGlobalVolumeScaling ? CalculateGlobalVolume() : defaultEnemyHitVolume;
        float pitch = useGlobalPitchRandomization ? Random.Range(globalMinPitch, globalMaxPitch) : 1f;

        PlaySFXWithPitch(defaultEnemyHitSFX, volume, pitch);
        allRecentHits.Enqueue(Time.time);

        Debug.Log($"🔊 Default Enemy Hit | SFX[{defaultEnemyHitSFX}] | Vol={volume:F2} | Pitch={pitch:F2}");
    }

    /// <summary>
    /// คำนวณ Volume แบบ Dynamic ตาม Mapping
    /// </summary>
    private float CalculateDynamicVolume(EnemyHitSoundMapping mapping)
    {
        float baseVolume = mapping.volume;

        if (!useGlobalVolumeScaling)
        {
            return baseVolume;
        }

        int totalActiveSounds = allRecentHits.Count;

        if (totalActiveSounds == 0)
        {
            return baseVolume;
        }

        // ยิ่งมีเสียงเยอะ volume ยิ่งลดลง
        float volumeScale = 1f - ((float)totalActiveSounds / maxTotalSimultaneousSounds);
        volumeScale = Mathf.Clamp01(volumeScale);

        float scaledVolume = Mathf.Lerp(minGlobalVolume, baseVolume, volumeScale);

        return scaledVolume;
    }

    /// <summary>
    /// คำนวณ Global Volume
    /// </summary>
    private float CalculateGlobalVolume()
    {
        int totalActiveSounds = allRecentHits.Count;

        if (totalActiveSounds == 0)
        {
            return maxGlobalVolume;
        }

        float volumeScale = 1f - ((float)totalActiveSounds / maxTotalSimultaneousSounds);
        volumeScale = Mathf.Clamp01(volumeScale);

        return Mathf.Lerp(minGlobalVolume, maxGlobalVolume, volumeScale);
    }

    /// <summary>
    /// คำนวณ Pitch
    /// </summary>
    private float CalculatePitch(EnemyHitSoundMapping mapping)
    {
        if (mapping.useCustomPitch)
        {
            return Random.Range(mapping.minPitch, mapping.maxPitch);
        }
        else if (useGlobalPitchRandomization)
        {
            return Random.Range(globalMinPitch, globalMaxPitch);
        }

        return 1f;
    }

    /// <summary>
    /// ลบข้อมูล hit เก่า
    /// </summary>
    private void CleanupOldHits(Queue<float> hitQueue)
    {
        float currentTime = Time.time;

        while (hitQueue.Count > 0)
        {
            float hitTime = hitQueue.Peek();

            if (currentTime - hitTime > HIT_TRACKING_DURATION)
            {
                hitQueue.Dequeue();
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// เล่น SFX พร้อมปรับ pitch
    /// </summary>
    private void PlaySFXWithPitch(int index, float volume, float pitch)
    {
        if (index < 0 || index >= sfx.Length || sfx[index] == null)
        {
            Debug.LogWarning($"⚠️ SFX[{index}] not found!");
            return;
        }

        if (sfx[index].clip == null)
        {
            Debug.LogWarning($"⚠️ SFX[{index}] has no AudioClip!");
            return;
        }

        AudioSource source = sfx[index];
        source.volume = Mathf.Clamp01(volume) * currentSFXVolume;
        source.pitch = pitch;
        source.Play();
    }

    /// <summary>
    /// รีเซ็ตระบบเสียง Enemy Hit
    /// </summary>
    public void ResetEnemyHitSounds()
    {
        allRecentHits.Clear();

        foreach (var mapping in enemyHitSounds)
        {
            mapping.recentHits.Clear();
            mapping.lastPlayTime = 0f;
        }

        Debug.Log("🔄 Enemy Hit Sound System Reset");
    }

    // ============================================
    // Original AudioManager Methods
    // ============================================

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"🎬 Scene loaded: {scene.name}");
        PlayBGMForScene(scene.name);
        ResetEnemyHitSounds();
    }

    private void PlayBGMForScene(string sceneName)
    {
        foreach (var mapping in sceneBGMMapping)
        {
            if (mapping.sceneName.Equals(sceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"🎵 Found BGM mapping for {sceneName}: BGM[{mapping.bgmIndex}]");

                if (currentBGMIndex == mapping.bgmIndex && Bgm[currentBGMIndex].isPlaying)
                {
                    Debug.Log($"🎵 BGM[{mapping.bgmIndex}] already playing");
                    return;
                }

                StartCoroutine(CrossfadeBGM(mapping.bgmIndex, mapping.fadeInDuration));
                return;
            }
        }

        Debug.LogWarning($"⚠️ No BGM mapping found for scene: {sceneName}");
    }

    private System.Collections.IEnumerator CrossfadeBGM(int newBGMIndex, float fadeInDuration)
    {
        if (isFading) yield break;
        isFading = true;

        float fadeDuration = 1f;
        AudioSource oldBGM = (currentBGMIndex >= 0 && currentBGMIndex < bgm.Length)
            ? bgm[currentBGMIndex] : null;
        AudioSource newBGM = (newBGMIndex >= 0 && newBGMIndex < bgm.Length)
            ? bgm[newBGMIndex] : null;

        if (newBGM == null)
        {
            Debug.LogError($"❌ BGM[{newBGMIndex}] not found!");
            isFading = false;
            yield break;
        }

        if (oldBGM != null && oldBGM.isPlaying)
        {
            float startVolume = oldBGM.volume;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                oldBGM.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
                yield return null;
            }

            oldBGM.Stop();
            oldBGM.volume = currentBGMVolume;
        }

        newBGM.volume = 0f;
        newBGM.Play();

        float targetVolume = currentBGMVolume;
        float elapsed2 = 0f;

        while (elapsed2 < fadeInDuration)
        {
            elapsed2 += Time.deltaTime;
            newBGM.volume = Mathf.Lerp(0f, targetVolume, elapsed2 / fadeInDuration);
            yield return null;
        }

        newBGM.volume = targetVolume;
        currentBGMIndex = newBGMIndex;
        isFading = false;

        Debug.Log($"✅ BGM changed to BGM[{newBGMIndex}]: {newBGM.clip?.name}");
    }

    public void PlayBGM(int i)
    {
        if (i < 0 || i >= bgm.Length)
        {
            Debug.LogWarning($"⚠️ BGM index {i} out of range!");
            return;
        }

        if (currentBGMIndex == i && Bgm[i].isPlaying)
        {
            Debug.Log($"🎵 BGM[{i}] already playing");
            return;
        }

        StopAllBGM();
        Bgm[i].volume = currentBGMVolume;
        Bgm[i].Play();
        currentBGMIndex = i;
        Debug.Log($"🎵 Playing BGM[{i}]: {Bgm[i].clip?.name}");
    }

    public void PlayBGMWithFade(int i, float fadeDuration = 1f)
    {
        if (i < 0 || i >= bgm.Length)
        {
            Debug.LogWarning($"⚠️ BGM index {i} out of range!");
            return;
        }

        StartCoroutine(CrossfadeBGM(i, fadeDuration));
    }

    private void StopAllBGM()
    {
        for (int i = 0; i < bgm.Length; i++)
        {
            if (bgm[i] != null)
                bgm[i].Stop();
        }
    }

    public void PlaySFX(int i)
    {
        if (i < sfx.Length && sfx[i] != null)
        {
            if (sfx[i].clip != null)
            {
                sfx[i].volume = currentSFXVolume;
                sfx[i].Play();
                Debug.Log($"🔊 Playing SFX[{i}]: {sfx[i].clip.name}");
            }
            else
            {
                Debug.LogWarning($"⚠️ SFX[{i}] has no AudioClip assigned!");
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ SFX index {i} is out of range or null!");
        }
    }

    public void PlaySFX(int i, float volume)
    {
        if (i < sfx.Length && sfx[i] != null)
        {
            if (sfx[i].clip != null)
            {
                sfx[i].volume = Mathf.Clamp01(volume) * currentSFXVolume;
                sfx[i].Play();
                Debug.Log($"🔊 Playing SFX[{i}]: {sfx[i].clip.name} at {volume * 100}% volume");
            }
            else
            {
                Debug.LogWarning($"⚠️ SFX[{i}] has no AudioClip assigned!");
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ SFX index {i} is out of range or null!");
        }
    }

    public void SetMasterVolume(float volume)
    {
        currentMasterVolume = Mathf.Clamp01(volume);

        if (audioMixer != null)
        {
            float dbValue = volume > 0.0001f ? Mathf.Log10(volume) * 20 : -80f;
            bool success = audioMixer.SetFloat("Master", dbValue);

            if (!success)
            {
                Debug.LogWarning("⚠️ Failed to set Master volume in AudioMixer. Check Exposed Parameter name!");
            }
        }

        Debug.Log($"🔊 Master Volume: {volume * 100}%");
    }

    public void SetBGMVolume(float volume)
    {
        currentBGMVolume = Mathf.Clamp01(volume);

        if (audioMixer != null)
        {
            float dbValue = volume > 0.0001f ? Mathf.Log10(volume) * 20 : -80f;
            bool success = audioMixer.SetFloat("BGM", dbValue);

            if (!success)
            {
                Debug.LogWarning("⚠️ Failed to set BGM volume in AudioMixer. Using direct AudioSource control.");
            }
        }

        foreach (var audio in bgm)
        {
            if (audio != null && audio.isPlaying)
            {
                audio.volume = currentBGMVolume;
            }
        }

        Debug.Log($"🎵 BGM Volume set to: {volume * 100}%");
    }

    public void SetSFXVolume(float volume)
    {
        currentSFXVolume = Mathf.Clamp01(volume);

        if (audioMixer != null)
        {
            float dbValue = volume > 0.0001f ? Mathf.Log10(volume) * 20 : -80f;
            bool success = audioMixer.SetFloat("SFX", dbValue);

            if (!success)
            {
                Debug.LogWarning("⚠️ Failed to set SFX volume in AudioMixer. Using direct AudioSource control.");
            }
        }

        foreach (var audio in sfx)
        {
            if (audio != null && audio.isPlaying)
            {
                audio.volume = currentSFXVolume;
            }
        }

        Debug.Log($"🔊 SFX Volume set to: {volume * 100}%");
    }

    public void StopCurrentBGM()
    {
        if (currentBGMIndex >= 0 && currentBGMIndex < bgm.Length)
        {
            bgm[currentBGMIndex].Stop();
            Debug.Log($"⏹️ Stopped BGM[{currentBGMIndex}]");
        }
    }

    public void PauseBGM()
    {
        if (currentBGMIndex >= 0 && currentBGMIndex < bgm.Length)
        {
            bgm[currentBGMIndex].Pause();
            Debug.Log($"⏸️ Paused BGM[{currentBGMIndex}]");
        }
    }

    public void ResumeBGM()
    {
        if (currentBGMIndex >= 0 && currentBGMIndex < bgm.Length)
        {
            bgm[currentBGMIndex].UnPause();
            Debug.Log($"▶️ Resumed BGM[{currentBGMIndex}]");
        }
    }
}