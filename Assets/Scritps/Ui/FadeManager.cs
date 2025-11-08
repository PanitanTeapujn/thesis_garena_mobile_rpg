using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeManager : MonoBehaviour
{
    [Header("Fade Settings")]
    public Image fadeImage; // Image Component ของ Panel สีดำ
    public float fadeDuration = 1f; // ระยะเวลา Fade (วินาที)

    private void Awake()
    {
        // ตั้งค่าให้โปร่งใสตอนเริ่มต้น
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            gameObject.SetActive(true); // ให้ GameObject นี้ Active เสมอ
        }
    }

    // ✅ Fade เป็นสีดำ (ทึบขึ้น)
    public IEnumerator FadeToBlack()
    {
        if (fadeImage != null)
        {
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeDuration);

                Color c = fadeImage.color;
                c.a = alpha;
                fadeImage.color = c;

                yield return null;
            }

            // ตรวจสอบให้แน่ใจว่าทึบสนิท
            Color finalColor = fadeImage.color;
            finalColor.a = 1f;
            fadeImage.color = finalColor;
        }
    }

    // ✅ Fade จากสีดำ (โปร่งใส)
    public IEnumerator FadeFromBlack()
    {
        if (fadeImage != null)
        {
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);

                Color c = fadeImage.color;
                c.a = alpha;
                fadeImage.color = c;

                yield return null;
            }

            // ตรวจสอบให้แน่ใจว่าโปร่งใสสนิท
            Color finalColor = fadeImage.color;
            finalColor.a = 0f;
            fadeImage.color = finalColor;
        }

        Debug.Log("[FadeManager] Fade complete!");
    }

    // ✅ Fade แบบทันที (ไม่มี Animation)
    public void SetBlackInstant()
    {
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 1f;
            fadeImage.color = c;
        }
    }

    // ✅ ใส (โปร่งใส) แบบทันที
    public void SetTransparentInstant()
    {
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }

    // ✅ ตั้งค่าระยะเวลา Fade
    public void SetFadeDuration(float seconds)
    {
        fadeDuration = Mathf.Max(0.1f, seconds);
    }
}