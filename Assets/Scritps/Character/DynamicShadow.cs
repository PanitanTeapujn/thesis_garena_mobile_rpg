using UnityEngine;

public class DynamicShadow : MonoBehaviour
{
    [Header("🌑 Shadow Settings")]
    [Tooltip("SpriteRenderer ของตัวละครหลัก")]
    public SpriteRenderer targetSpriteRenderer;

    [Header("🎨 Shadow Appearance")]
    [Tooltip("สีของเงา")]
    public Color shadowColor = new Color(0f, 0f, 0f, 0.5f);

    [Tooltip("ความโปร่งใสของเงา (0-1)")]
    [Range(0f, 1f)]
    public float shadowAlpha = 0.5f;

    [Header("📍 Shadow Position")]
    [Tooltip("ตำแหน่งเงาเทียบกับตัวละคร")]
    public Vector3 shadowOffset = new Vector3(0.1f, -0.3f, 0f);

    [Header("🔧 Shadow Transform")]
    [Tooltip("ขนาดเงาเทียบกับตัวละคร")]
    public Vector3 shadowScale = new Vector3(1f, 0.6f, 1f);

    [Tooltip("การหมุนของเงา (degrees)")]
    public Vector3 shadowRotation = new Vector3(0f, 0f, 0f);

    [Tooltip("การบิดเงาแนวนอน (-1 ถึง 1)")]
    [Range(-1f, 1f)]
    public float skewX = 0f;

    [Header("⚙️ Shadow Behavior")]
    [Tooltip("เงาติดตามตัวละครอัตโนมัติ")]
    public bool followTarget = true;

    [Tooltip("เงาติดตาม Flip ของตัวละคร")]
    public bool mirrorFlip = true;
    [Tooltip("เงาหันทิศเดียวกับตัวละคร (ถ้าไม่เช็ค = หันตรงข้าม)")]
    public bool sameFacing = false; // ✅ เพิ่มใหม่
    [Tooltip("เงาใช้ Sprite เดียวกับตัวละคร")]
    public bool useSameSprite = true;

    [Header("🎭 Advanced Settings")]
    [Tooltip("Sprite เงาแยก (ถ้าไม่ใช้ Sprite เดียวกัน)")]
    public Sprite customShadowSprite;

    [Tooltip("Material เงาพิเศษ")]
    public Material shadowMaterial;

    // ========== Private Variables ==========
    private GameObject shadowObject;
    private SpriteRenderer shadowRenderer;
    private Transform shadowTransform;

    // เก็บค่าเดิมเพื่อเช็คการเปลี่ยนแปลง
    private Vector3 lastTargetScale = Vector3.one;
    private Sprite lastTargetSprite;

    // ========== Unity Lifecycle ==========
    void Start()
    {
        if (targetSpriteRenderer == null)
        {
            targetSpriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (targetSpriteRenderer == null)
        {
            Debug.LogError($"[DynamicShadow] No target SpriteRenderer found on {gameObject.name}!");
            enabled = false;
            return;
        }

        CreateShadow();
    }

    void Update()
    {
        if (shadowObject == null || targetSpriteRenderer == null) return;

        if (followTarget)
        {
            UpdateShadowPosition();
        }

        UpdateShadowTransform();
        CheckSpriteChange();
        CheckFlipChange();
    }

    // ========== Shadow Creation ==========
    private void CreateShadow()
    {
        // สร้าง Shadow GameObject
        shadowObject = new GameObject($"{gameObject.name}_Shadow");
        shadowObject.transform.SetParent(transform);

        // เพิ่ม SpriteRenderer
        shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
        shadowTransform = shadowObject.transform;

        // ตั้งค่าเริ่มต้น
        SetupShadowRenderer();
        UpdateShadowTransform();
        UpdateShadowPosition();

        Debug.Log($"[DynamicShadow] Created shadow for {gameObject.name}");
    }

    private void SetupShadowRenderer()
    {
        if (shadowRenderer == null) return;

        // ตั้ง Sprite
        if (useSameSprite && targetSpriteRenderer.sprite != null)
        {
            shadowRenderer.sprite = targetSpriteRenderer.sprite;
        }
        else if (customShadowSprite != null)
        {
            shadowRenderer.sprite = customShadowSprite;
        }
        else
        {
            shadowRenderer.sprite = targetSpriteRenderer.sprite;
        }

        // ✅ ตั้ง Material เป็น Sprites-Default
        if (shadowMaterial != null)
        {
            shadowRenderer.material = shadowMaterial;
        }
        else
        {
            // ใช้ Sprites-Default แทน Lit
            Material defaultSpriteMaterial = Resources.GetBuiltinResource<Material>("Sprites-Default.mat");
            if (defaultSpriteMaterial != null)
            {
                shadowRenderer.material = defaultSpriteMaterial;
                Debug.Log("[DynamicShadow] Using Sprites-Default material");
            }
            else
            {
                // ถ้าหาไม่เจอ ให้สร้างใหม่
                Shader spriteShader = Shader.Find("Sprites/Default");
                if (spriteShader != null)
                {
                    shadowRenderer.material = new Material(spriteShader);
                    Debug.Log("[DynamicShadow] Created new Sprites/Default material");
                }
                else
                {
                    shadowRenderer.material = null; // ใช้ default
                    Debug.Log("[DynamicShadow] Using null material (default)");
                }
            }
        }

        // ตั้งสีและ alpha
        UpdateShadowColor();

        // ตั้ง Sorting Layer
        shadowRenderer.sortingLayerName = targetSpriteRenderer.sortingLayerName;
        shadowRenderer.sortingOrder = targetSpriteRenderer.sortingOrder - 1;

        lastTargetSprite = targetSpriteRenderer.sprite;
    }
    // ========== Shadow Updates ==========
    private void UpdateShadowPosition()
    {
        if (shadowTransform == null) return;

        shadowTransform.localPosition = shadowOffset;
    }

    private void UpdateShadowTransform()
    {
        if (shadowTransform == null) return;

        // Scale
        Vector3 finalScale = shadowScale;

        // รองรับ Skew X
        if (skewX != 0f)
        {
            // สร้าง matrix สำหรับ skew
            Matrix4x4 skewMatrix = Matrix4x4.identity;
            skewMatrix.m01 = skewX; // Skew X

            // Apply skew ผ่าง transform
            shadowTransform.localScale = finalScale;

            // หา component ที่มี matrix transform ถ้าต้องการ advanced skew
            // สำหรับ skew ง่ายๆ ใช้ scale.x แทน
            if (skewX > 0)
            {
                finalScale.x *= (1f + Mathf.Abs(skewX));
            }
        }

        shadowTransform.localScale = finalScale;
        shadowTransform.localRotation = Quaternion.Euler(shadowRotation);
    }

    private void UpdateShadowColor()
    {
        if (shadowRenderer == null) return;

        Color finalColor = shadowColor;
        finalColor.a = shadowAlpha;
        shadowRenderer.color = finalColor;
    }

    private void CheckSpriteChange()
    {
        if (!useSameSprite) return;

        if (targetSpriteRenderer.sprite != lastTargetSprite)
        {
            shadowRenderer.sprite = targetSpriteRenderer.sprite;
            lastTargetSprite = targetSpriteRenderer.sprite;
        }
    }

    private void CheckFlipChange()
    {
        if (!mirrorFlip) return;

        Vector3 targetScale = targetSpriteRenderer.transform.localScale;

        if (targetScale != lastTargetScale)
        {
            Vector3 currentShadowScale = shadowScale;

            if (sameFacing)
            {
                // เงาหันทิศเดียวกับตัวละคร
                if (targetScale.x < 0)
                    currentShadowScale.x = -Mathf.Abs(shadowScale.x);
                else if (targetScale.x > 0)
                    currentShadowScale.x = Mathf.Abs(shadowScale.x);
            }
            else
            {
                // เงาหันทิศตรงข้ามกับตัวละคร
                if (targetScale.x < 0)
                    currentShadowScale.x = Mathf.Abs(shadowScale.x);
                else if (targetScale.x > 0)
                    currentShadowScale.x = -Mathf.Abs(shadowScale.x);
            }

            shadowTransform.localScale = currentShadowScale;
            lastTargetScale = targetScale;
        }
    }

    // ========== Public Methods ==========

    /// <summary>
    /// เปลี่ยนสีเงา
    /// </summary>
    public void SetShadowColor(Color color)
    {
        shadowColor = color;
        UpdateShadowColor();
    }

    /// <summary>
    /// เปลี่ยนความโปร่งใสเงา
    /// </summary>
    public void SetShadowAlpha(float alpha)
    {
        shadowAlpha = Mathf.Clamp01(alpha);
        UpdateShadowColor();
    }

    /// <summary>
    /// เปลี่ยนตำแหน่งเงา
    /// </summary>
    public void SetShadowOffset(Vector3 offset)
    {
        shadowOffset = offset;
        UpdateShadowPosition();
    }

    /// <summary>
    /// เปลี่ยนขนาดเงา
    /// </summary>
    public void SetShadowScale(Vector3 scale)
    {
        shadowScale = scale;
        UpdateShadowTransform();
    }

    /// <summary>
    /// เปลี่ยนการหมุนเงา
    /// </summary>
    public void SetShadowRotation(Vector3 rotation)
    {
        shadowRotation = rotation;
        UpdateShadowTransform();
    }

    /// <summary>
    /// เปลี่ยนการบิดเงา
    /// </summary>
    public void SetSkewX(float skew)
    {
        skewX = Mathf.Clamp(skew, -1f, 1f);
        UpdateShadowTransform();
    }

    /// <summary>
    /// แสดง/ซ่อนเงา
    /// </summary>
    public void SetShadowVisible(bool visible)
    {
        if (shadowObject != null)
        {
            shadowObject.SetActive(visible);
        }
    }

    /// <summary>
    /// เปลี่ยน Sprite เงา
    /// </summary>
    public void SetCustomSprite(Sprite sprite)
    {
        customShadowSprite = sprite;
        if (shadowRenderer != null && !useSameSprite)
        {
            shadowRenderer.sprite = sprite;
        }
    }

    // ========== Animation Support ==========

    /// <summary>
    /// Animate เงาไปตำแหน่งใหม่
    /// </summary>
    public void AnimateShadowTo(Vector3 newOffset, float duration)
    {
        if (shadowObject != null)
        {
            StartCoroutine(AnimateOffset(newOffset, duration));
        }
    }

    private System.Collections.IEnumerator AnimateOffset(Vector3 targetOffset, float duration)
    {
        Vector3 startOffset = shadowOffset;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            shadowOffset = Vector3.Lerp(startOffset, targetOffset, progress);
            UpdateShadowPosition();

            yield return null;
        }

        shadowOffset = targetOffset;
        UpdateShadowPosition();
    }

    // ========== Inspector Validation ==========
    private void OnValidate()
    {
        // อัพเดทเงาทันทีเมื่อเปลี่ยนค่าใน Inspector
        if (Application.isPlaying && shadowRenderer != null)
        {
            UpdateShadowColor();
            UpdateShadowTransform();
            UpdateShadowPosition();
        }
    }

    // ========== Cleanup ==========
    private void OnDestroy()
    {
        if (shadowObject != null)
        {
            if (Application.isPlaying)
            {
                Destroy(shadowObject);
            }
            else
            {
                DestroyImmediate(shadowObject);
            }
        }
    }

    // ========== Gizmos ==========
    private void OnDrawGizmosSelected()
    {
        if (targetSpriteRenderer == null) return;

        // วาดจุดที่เงาจะอยู่
        Gizmos.color = Color.yellow;
        Vector3 shadowWorldPos = transform.position + shadowOffset;
        Gizmos.DrawWireCube(shadowWorldPos, Vector3.one * 0.1f);

        // วาดเส้นจากตัวละครไปเงา
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, shadowWorldPos);
    }
}