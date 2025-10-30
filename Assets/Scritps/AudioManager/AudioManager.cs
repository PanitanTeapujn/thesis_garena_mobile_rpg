using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

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

    [System.Serializable]
    public class SceneBGMMapping
    {
        public string sceneName;  // ชื่อ Scene
        public int bgmIndex;      // Index ของ BGM ที่จะเล่น
        [Tooltip("Fade in duration (seconds)")]
        public float fadeInDuration = 1f;
    }

    private int currentBGMIndex = -1;
    private bool isFading = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        instance = this;

        // ✅ Subscribe to scene change event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        // ✅ ตั้งค่า volume เริ่มต้น
        SetMasterVolume(1f);
        SetSFXVolume(1f);
        SetBGMVolume(0.7f);

        // ✅ เล่น BGM ตาม scene ปัจจุบัน
        string currentScene = SceneManager.GetActiveScene().name;
        PlayBGMForScene(currentScene);
    }

    void OnDestroy()
    {
        // ✅ Unsubscribe เมื่อ destroy
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ✅ เมื่อ Scene โหลดเสร็จ
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"🎬 Scene loaded: {scene.name}");
        PlayBGMForScene(scene.name);
    }

    // ✅ เล่น BGM ตาม Scene
    private void PlayBGMForScene(string sceneName)
    {
        foreach (var mapping in sceneBGMMapping)
        {
            if (mapping.sceneName.Equals(sceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"🎵 Found BGM mapping for {sceneName}: BGM[{mapping.bgmIndex}]");

                // ถ้าเป็น BGM เดียวกันกับที่เล่นอยู่ ไม่ต้องเปลี่ยน
                if (currentBGMIndex == mapping.bgmIndex && Bgm[currentBGMIndex].isPlaying)
                {
                    Debug.Log($"🎵 BGM[{mapping.bgmIndex}] already playing");
                    return;
                }

                // เปลี่ยน BGM แบบ fade
                StartCoroutine(CrossfadeBGM(mapping.bgmIndex, mapping.fadeInDuration));
                return;
            }
        }

        Debug.LogWarning($"⚠️ No BGM mapping found for scene: {sceneName}");
    }

    // ✅ Crossfade BGM (เฟดเก่าออก เฟดใหม่เข้า)
    private System.Collections.IEnumerator CrossfadeBGM(int newBGMIndex, float fadeInDuration)
    {
        if (isFading) yield break; // ป้องกัน fade ซ้อน
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

        // Phase 1: Fade out BGM เก่า (ถ้ามี)
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
            oldBGM.volume = startVolume; // คืนค่า volume
        }

        // Phase 2: เล่น BGM ใหม่และ Fade in
        newBGM.volume = 0f;
        newBGM.Play();

        float targetVolume = 0.7f; // Volume เป้าหมาย
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

    // ✅ เปลี่ยน BGM แบบทันที (ไม่มี fade)
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
        Bgm[i].Play();
        currentBGMIndex = i;
        Debug.Log($"🎵 Playing BGM[{i}]: {Bgm[i].clip?.name}");
    }

    // ✅ เปลี่ยน BGM แบบมี fade
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

    // ✅ SFX Methods (เหมือนเดิม)
    public void PlaySFX(int i)
    {
        if (i < sfx.Length && sfx[i] != null)
        {
            if (sfx[i].clip != null)
            {
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
                sfx[i].volume = Mathf.Clamp01(volume);
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
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20);
    }

    public void SetBGMVolume(float volume)
    {
        audioMixer.SetFloat("BGMVolume", Mathf.Log10(volume) * 20);
    }

    public void SetSFXVolume(float volume)
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20);
    }

    // ✅ Helper: หยุด BGM ปัจจุบัน
    public void StopCurrentBGM()
    {
        if (currentBGMIndex >= 0 && currentBGMIndex < bgm.Length)
        {
            bgm[currentBGMIndex].Stop();
            Debug.Log($"⏹️ Stopped BGM[{currentBGMIndex}]");
        }
    }

    // ✅ Helper: Pause/Resume BGM
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