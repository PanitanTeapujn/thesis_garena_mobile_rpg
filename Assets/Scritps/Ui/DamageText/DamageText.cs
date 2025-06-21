using UnityEngine;
using TMPro;
using System.Collections;

public class DamageText : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshPro damageTextMesh;
    public Canvas damageCanvas;

    [Header("Animation Settings")]
    public float lifetime = 2f;
    public float moveSpeed = 2f;
    public float fadeSpeed = 1f;
    public AnimationCurve moveCurve;
    public AnimationCurve scaleCurve;

    [Header("Visual Settings")]
    public Color normalDamageColor = Color.white;
    public Color criticalDamageColor = Color.red;
    public Color healColor = Color.green;
    public Color poisonColor = Color.green;
    public Color burnColor = Color.red;
    public Color bleedColor = new Color(0.8f, 0, 0, 1f);
    public Color magicDamageColor = Color.cyan;

    private Vector3 originalPosition;
    private Vector3 targetPosition;
    private Vector3 originalScale;
    private float timer = 0f;
    private Camera mainCamera;
    private bool isActive = false;
    [Header("Miss Text Settings")]
    public Color missColor = Color.gray;
    public string missText = "MISS";
    private float baseFontSize = 40f;
    private void Awake()
    {
        // Setup components if not assigned
        if (damageTextMesh == null)
            damageTextMesh = GetComponentInChildren<TextMeshPro>();

        if (damageCanvas == null)
            damageCanvas = GetComponentInChildren<Canvas>();

        // Setup canvas for world space
        if (damageCanvas != null)
        {
            damageCanvas.renderMode = RenderMode.WorldSpace;
            damageCanvas.worldCamera = Camera.main;
        }

        // Setup default animation curves if not set
        if (moveCurve == null || moveCurve.keys.Length == 0)
        {
            moveCurve = new AnimationCurve();
            moveCurve.AddKey(0f, 0f);
            moveCurve.AddKey(1f, 1f);
            // Set tangents for smooth ease out
            for (int i = 0; i < moveCurve.keys.Length; i++)
            {
                moveCurve.SmoothTangents(i, 0f);
            }
        }

        if (scaleCurve == null || scaleCurve.keys.Length == 0)
        {
            scaleCurve = new AnimationCurve();
            scaleCurve.AddKey(0f, 1.2f);    // Start bigger
            scaleCurve.AddKey(0.3f, 1f);    // Normal size
            scaleCurve.AddKey(1f, 0.8f);    // End smaller
            // Set tangents for smooth curve
            for (int i = 0; i < scaleCurve.keys.Length; i++)
            {
                scaleCurve.SmoothTangents(i, 0f);
            }
        }

        originalScale = transform.localScale;
        mainCamera = Camera.main;
    }

    public void Initialize(Vector3 position, int damage, DamageType damageType, bool isCritical = false, bool isHeal = false)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Reset state
        timer = 0f;
        isActive = true;

        // Position setup
        originalPosition = position + GetRandomOffset();
        targetPosition = originalPosition + Vector3.up * moveSpeed;
        transform.position = originalPosition;
        transform.localScale = originalScale;

        // Text setup
        SetDamageText(damage, damageType, isCritical, isHeal);

        // Start animation
        gameObject.SetActive(true);
        StartCoroutine(AnimateDamageText());
    }

    private Vector3 GetRandomOffset()
    {
        // Add random offset to prevent overlapping
        return new Vector3(
            Random.Range(-0.5f, 0.5f),
            Random.Range(0.2f, 0.8f),
            Random.Range(-0.3f, 0.3f)
        );
    }

    private void SetDamageText(int damage, DamageType damageType, bool isCritical, bool isHeal)
    {
        if (damageTextMesh == null) return;

        // Determine text and color
        string text;
        Color textColor;
        float fontSize;

        if (isHeal)
        {
            text = $"+{damage}";
            textColor = healColor;
            fontSize = baseFontSize; // ขนาดปกติ
            damageTextMesh.fontStyle = FontStyles.Normal;
        }
        else
        {
            if (isCritical)
            {
                text = $"CRIT {damage}";
                fontSize = baseFontSize ; // Critical ใหญ่ที่สุด
                damageTextMesh.fontStyle = FontStyles.Bold;
            }
            else
            {
                text = damage.ToString();
                fontSize = baseFontSize; // ขนาดปกติ
                damageTextMesh.fontStyle = FontStyles.Normal;
            }

            textColor = GetDamageColor(damageType, isCritical);
        }

        // Apply text settings
        damageTextMesh.text = text;
        damageTextMesh.color = textColor;
        damageTextMesh.fontSize = fontSize; // ✅ ใช้ font size แทน scale
    }

    private Color GetDamageColor(DamageType damageType, bool isCritical)
    {
        // ✅ ถ้าเป็น critical ให้ใช้สีแดงเสมอ
        if (isCritical)
        {
            return criticalDamageColor; // ใช้สีแดงโดยตรง
        }

        // สำหรับ non-critical ใช้สีตาม damage type
        Color baseColor = damageType switch
        {
            DamageType.Critical => criticalDamageColor,
            DamageType.Magic => magicDamageColor,
            DamageType.Poison => poisonColor,
            DamageType.Burn => burnColor,
            DamageType.Bleed => bleedColor,
            _ => normalDamageColor
        };

        return baseColor;
    }

    private IEnumerator AnimateDamageText()
    {
        while (timer < lifetime && isActive)
        {
            timer += Time.deltaTime;
            float progress = timer / lifetime;

            // Position animation
            Vector3 currentPos = Vector3.Lerp(originalPosition, targetPosition, moveCurve.Evaluate(progress));
            transform.position = currentPos;

            // Scale animation
            float scaleMultiplier = scaleCurve.Evaluate(progress);
            transform.localScale = originalScale * scaleMultiplier;

            // Fade animation
            if (damageTextMesh != null)
            {
                Color currentColor = damageTextMesh.color;
                currentColor.a = Mathf.Lerp(1f, 0f, Mathf.Pow(progress, fadeSpeed));
                damageTextMesh.color = currentColor;
            }

            // Face camera
            FaceCamera();

            yield return null;
        }

        // Return to pool
        ReturnToPool();
    }

    private void FaceCamera()
    {
        if (mainCamera == null) return;

        Vector3 lookDirection = mainCamera.transform.rotation * Vector3.forward;
        Vector3 upDirection = mainCamera.transform.rotation * Vector3.up;
        transform.LookAt(transform.position + lookDirection, upDirection);
    }

    public void ReturnToPool()
    {
        isActive = false;
        StopAllCoroutines();
        gameObject.SetActive(false);

        // Return to manager pool
        DamageTextManager.Instance?.ReturnDamageText(this);
    }

    public void ShowMiss(Vector3 position)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Reset state
        timer = 0f;
        isActive = true;

        // ✅ ใช้ offset พิเศษสำหรับ Miss
        originalPosition = position + GetRandomOffsetForMiss();
        targetPosition = originalPosition + Vector3.up * (moveSpeed * 0.4f);

        transform.position = originalPosition;
        transform.localScale = originalScale;

        // Miss text setup
        SetMissText();

        // Start animation
        gameObject.SetActive(true);
        StartCoroutine(AnimateDamageText());
    }

    private Vector3 GetRandomOffsetForMiss()
    {
        // Miss offset ที่ต่ำกว่า damage ปกติ
        return new Vector3(
            Random.Range(-0.3f, 0.3f),    // แคบกว่า
            Random.Range(0.1f, 0.4f),     // ต่ำกว่า (0.2f-0.8f -> 0.1f-0.4f)
            Random.Range(-0.2f, 0.2f)     // แคบกว่า
        );
    }

    // Overload สำหรับใช้ position ปัจจุบัน
    public void ShowMiss()
    {
        ShowMiss(transform.position);
    }

    private void SetMissText()
    {
        if (damageTextMesh == null) return;

        // Set miss text
        damageTextMesh.text = missText;
        damageTextMesh.color = missColor;
        damageTextMesh.fontStyle = FontStyles.Italic;

        // ✅ Miss ใช้ font size เล็ก
        damageTextMesh.fontSize = baseFontSize * 0.6f; // เล็กมาก
    }

    // เพิ่ม method สำหรับ Initialize แบบเฉพาะ Miss (optional - สำหรับใช้จาก DamageTextManager)
    public void InitializeMiss(Vector3 position)
    {
        transform.position = position;
        ShowMiss();
    }

    private void Update()
    {
        // Safety check - auto return if too far from camera
        if (isActive && mainCamera != null)
        {
            float distance = Vector3.Distance(transform.position, mainCamera.transform.position);
            if (distance > 50f) // Too far away
            {
                ReturnToPool();
            }
        }
    }

    // Public method to stop animation early
    public void StopAnimation()
    {
        if (isActive)
        {
            ReturnToPool();
        }
    }
}