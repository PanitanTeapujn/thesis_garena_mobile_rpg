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
        public string sceneName;
        public int bgmIndex;
        [Tooltip("Fade in duration (seconds)")]
        public float fadeInDuration = 1f;
    }

    private int currentBGMIndex = -1;
    private bool isFading = false;

    // ✅ เก็บค่า Volume ไว้ใช้
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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"🎬 Scene loaded: {scene.name}");
        PlayBGMForScene(scene.name);
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
            oldBGM.volume = currentBGMVolume; // ✅ ใช้ค่าที่เก็บไว้
        }

        newBGM.volume = 0f;
        newBGM.Play();

        float targetVolume = currentBGMVolume; // ✅ ใช้ค่าที่เก็บไว้
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
        Bgm[i].volume = currentBGMVolume; // ✅ ตั้งค่า volume
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
                sfx[i].volume = currentSFXVolume; // ✅ ใช้ค่าที่เก็บไว้
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
                sfx[i].volume = Mathf.Clamp01(volume) * currentSFXVolume; // ✅ คูณกับค่า global
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

    // ✅ ปรับปรุงฟังก์ชันควบคุม Volume
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

        // ลองตั้งค่าใน AudioMixer
        if (audioMixer != null)
        {
            float dbValue = volume > 0.0001f ? Mathf.Log10(volume) * 20 : -80f;
            bool success = audioMixer.SetFloat("BGM", dbValue);

            if (!success)
            {
                Debug.LogWarning("⚠️ Failed to set BGM volume in AudioMixer. Using direct AudioSource control.");
            }
        }

        // ✅ ตั้งค่า Volume ของ AudioSource ทุกตัวโดยตรง (สำรอง)
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

        // ✅ อัปเดต volume ของ SFX ที่กำลังเล่นอยู่
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