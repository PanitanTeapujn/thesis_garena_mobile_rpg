using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class StartScreenEffects : MonoBehaviour
{
    // ข้อความ "Touch to Start"
    public TextMeshProUGUI touchToStartText;
    public float fadeInDuration = 1f;
    public float zoomInDuration = 0.5f;
    public float blinkSpeed = 1f;
    public float minAlpha = 0.3f;
    public float maxAlpha = 1f;
    private Vector3 originalScale;
    private bool startBlinking = false;

    // ชื่อเกม (เพิ่มใหม่)
    public TextMeshProUGUI gameTitleText;
    public float titleSlideInDuration = 1f; // เวลาเลื่อนเข้ามา
    public float titleBounceHeight = 100f; // ความสูงที่เลื่อนจากบน
    public float titleFloatSpeed = 1f; // ความเร็วโยกขึ้นลง
    public float titleFloatAmount = 10f; // ระยะโยกขึ้นลง
    public bool enableRainbowEffect = true; // เปิดเอฟเฟกต์สีรุ้ง
    public float rainbowSpeed = 1f; // ความเร็วเปลี่ยนสี
    private Vector3 titleOriginalPos;
    private bool startFloating = false;

    public float scrollSpeed = 0.5f;
    private Vector2 offset;

    public AudioClip startGameSound;
    private AudioSource audioSource;

    public ParticleSystem touchEffect;

    public Color colorStart = Color.blue;
    public Color colorEnd = Color.green;
    public float changeSpeed = 1f;
    private Camera cam;
    private float lerpTime;

    private bool isLoading = false;

    void Start()
    {
        // ตั้งค่า Touch to Start
        originalScale = touchToStartText.transform.localScale;
        Color textColor = touchToStartText.color;
        textColor.a = 0f;
        touchToStartText.color = textColor;

        // ตั้งค่าชื่อเกม
        if (gameTitleText != null)
        {
            titleOriginalPos = gameTitleText.transform.localPosition;
            // เริ่มต้นชื่อเกมอยู่ด้านบนนอกหน้าจอ
            gameTitleText.transform.localPosition = titleOriginalPos + Vector3.up * titleBounceHeight;
            StartCoroutine(ShowGameTitle());
        }

        StartCoroutine(ShowTextEffect());

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = startGameSound;
        audioSource.playOnAwake = false;

        cam = Camera.main;

        if (touchEffect != null)
        {
            touchEffect.Stop();
        }
    }

    void Update()
    {
        // เปลี่ยนสีพื้นหลัง
        lerpTime += Time.deltaTime * changeSpeed;
        cam.backgroundColor = Color.Lerp(colorStart, colorEnd, Mathf.PingPong(lerpTime, 1));

        // เอฟเฟกต์กระพริบของ Touch to Start
        if (startBlinking)
        {
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, Mathf.PingPong(Time.time * blinkSpeed, 1));
            Color textColor = touchToStartText.color;
            textColor.a = alpha;
            touchToStartText.color = textColor;
        }

        // เอฟเฟกต์โยกขึ้นลงของชื่อเกม
        if (startFloating && gameTitleText != null)
        {
            float floatOffset = Mathf.Sin(Time.time * titleFloatSpeed) * titleFloatAmount;
            gameTitleText.transform.localPosition = titleOriginalPos + Vector3.up * floatOffset;
        }

        // เอฟเฟกต์สีรุ้งของชื่อเกม
        if (enableRainbowEffect && gameTitleText != null)
        {
            float hue = Mathf.PingPong(Time.time * rainbowSpeed * 0.1f, 1f);
            gameTitleText.color = Color.HSVToRGB(hue, 0.8f, 1f);
        }

        // ตรวจจับการแตะหน้าจอ
        if (!isLoading && (Input.touchCount > 0 || Input.GetMouseButtonDown(0)))
        {
            StartGame();
        }
    }

    // Coroutine สำหรับแสดงชื่อเกม
    IEnumerator ShowGameTitle()
    {
        // Slide In จากด้านบน พร้อม Bounce
        float elapsedTime = 0f;
        Vector3 startPos = gameTitleText.transform.localPosition;

        while (elapsedTime < titleSlideInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / titleSlideInDuration;

            // ใช้ Ease Out Bounce
            float bounce = Mathf.Sin(t * Mathf.PI * 0.5f);

            gameTitleText.transform.localPosition = Vector3.Lerp(startPos, titleOriginalPos, bounce);

            // Scale Bounce เล็กน้อย
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;
            gameTitleText.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        // Reset Scale
        gameTitleText.transform.localScale = Vector3.one;
        gameTitleText.transform.localPosition = titleOriginalPos;

        // เริ่มโยกขึ้นลง
        startFloating = true;
    }

    void StartGame()
    {
        isLoading = true;

        if (audioSource != null && startGameSound != null)
        {
            audioSource.Play();
        }

        if (touchEffect != null)
        {
            touchEffect.Play();
        }

        StartCoroutine(LoadSceneWithDelay(0.5f));
    }

    IEnumerator LoadSceneWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadNextScene();
    }

    IEnumerator ShowTextEffect()
    {
        // Fade In ข้อความ
        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / fadeInDuration);
            Color textColor = touchToStartText.color;
            textColor.a = alpha;
            touchToStartText.color = textColor;
            yield return null;
        }

        // Zoom In ข้อความ
        elapsedTime = 0f;
        while (elapsedTime < zoomInDuration)
        {
            elapsedTime += Time.deltaTime;
            float scale = Mathf.Lerp(1f, 1.5f, elapsedTime / zoomInDuration);
            touchToStartText.transform.localScale = originalScale * scale;
            yield return null;
        }

        startBlinking = true;
    }

    void LoadNextScene()
    {
        AudioManager.instance.PlaySFX(5, 1f);           
        AudioManager.instance.PlaySFX(6, 0.7f);           
            if (Application.CanStreamedLevelBeLoaded("LoginAndRegister"))
        {
            SceneManager.LoadScene("LoginAndRegister");
        }
        else
        {
            Debug.LogError("Scene 'LoginAndRegister' not found in Build Settings!");
        }
    }
}