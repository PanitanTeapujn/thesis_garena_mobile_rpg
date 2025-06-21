using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class CharacterVisualManager : NetworkBehaviour
{
    [Header("Visual Components")]
    public Renderer characterRenderer;
    public Color originalColor = Color.white;

    [Header("Status Effect Colors")]
    public Color poisonColor = new Color(0.5f, 1f, 0.5f);
    public Color burnColor = new Color(1f, 0.3f, 0f);
    public Color freezeColor = new Color(0.3f, 0.8f, 1f);
    public Color stunColor = new Color(1f, 1f, 0f);
    public Color bleedColor = new Color(0.7f, 0f, 0f);
    public Color armorBreakColor = new Color(0.8f, 0.8f, 0.3f);
    public Color blindColor = new Color(0.2f, 0.2f, 0.2f);
    public Color weaknessColor = new Color(0.6f, 0.4f, 0.8f);

    [Header("VFX GameObjects")]
    public GameObject poisonVFX;
    public GameObject burnVFX;
    public GameObject freezeVFX;
    public GameObject stunVFX;
    public GameObject bleedVFX;
    public GameObject armorBreakVFX;
    public GameObject blindVFX;
    public GameObject weaknessVFX;

    [Header("Flash Settings")]
    public float damageFlashDuration = 0.2f;
    public float criticalFlashDuration = 0.3f;
    public float statusFlashDuration = 0.2f;

    // ========== Component References ==========
    private Character character;
    private StatusEffectManager statusEffectManager;
    private CombatManager combatManager;

    // ========== Color System Support ==========
    private bool isUsingVertexColors = false;
    private SpriteRenderer spriteRenderer;

    // ========== Flash Control ==========
    private bool isTakingDamage = false;
    private bool isFlashingFromPoison = false;
    private bool isFlashingFromStatus = false;

    protected virtual void Awake()
    {
        character = GetComponent<Character>();
        statusEffectManager = GetComponent<StatusEffectManager>();
        combatManager = GetComponent<CombatManager>();

        if (characterRenderer == null)
            characterRenderer = GetComponent<Renderer>();

        InitializeColorSystem();
    }

    private void InitializeColorSystem()
    {
        if (characterRenderer != null)
        {
            // เช็คว่า material มี _Color property หรือไม่
            if (characterRenderer.material.HasProperty("_Color"))
            {
                // ใช้ material color
                originalColor = characterRenderer.material.color;
                isUsingVertexColors = false;
                Debug.Log("Using Material Color system");
            }
            else
            {
                // ถ้าไม่มี _Color property ให้ใช้ SpriteRenderer
                Debug.Log("Material doesn't have _Color property. Switching to SpriteRenderer.");

                spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    originalColor = spriteRenderer.color;
                    isUsingVertexColors = true;
                    Debug.Log("Using SpriteRenderer Color system");
                }
                else
                {
                    originalColor = Color.white;
                    isUsingVertexColors = true;
                    Debug.LogWarning("No color system found, using default white color");
                }
            }
        }
        else
        {
            Debug.LogWarning("CharacterRenderer is null! Cannot initialize color system.");
        }
    }

    private void SetCharacterColor(Color color)
    {
        if (characterRenderer == null) return;

        if (isUsingVertexColors)
        {
            // ใช้ SpriteRenderer
            if (spriteRenderer != null)
            {
                spriteRenderer.color = color;
            }
        }
        else
        {
            // ใช้ material color (เช็คอีกครั้งเพื่อความปลอดภัย)
            if (characterRenderer.material.HasProperty("_Color"))
            {
                characterRenderer.material.color = color;
            }
            else
            {
                // Fallback ไปใช้ SpriteRenderer
                if (spriteRenderer == null)
                    spriteRenderer = GetComponent<SpriteRenderer>();

                if (spriteRenderer != null)
                {
                    spriteRenderer.color = color;
                    isUsingVertexColors = true;
                }
            }
        }
    }
    private Color GetCharacterColor()
    {
        if (characterRenderer == null) return originalColor;

        if (isUsingVertexColors)
        {
            if (spriteRenderer != null)
            {
                return spriteRenderer.color;
            }
        }
        else
        {
            // เช็คว่ามี _Color property หรือไม่
            if (characterRenderer.material.HasProperty("_Color"))
            {
                return characterRenderer.material.color;
            }
        }

        return originalColor;
    }

    private void ShowBurnEffect(bool show)
    {
        if (burnVFX != null)
        {
            burnVFX.SetActive(show);
        }

        if (characterRenderer != null && !isFlashingFromStatus && !isTakingDamage)
        {
            if (show)
            {
                SetCharacterColor(burnColor);
            }
            else
            {
                SetCharacterColor(GetReturnColor());
            }
        }
    }

    private void ShowBleedEffect(bool show)
    {
        if (bleedVFX != null)
        {
            bleedVFX.SetActive(show);
        }

        if (characterRenderer != null && !isFlashingFromStatus && !isTakingDamage)
        {
            if (show)
            {
                SetCharacterColor(bleedColor);
            }
            else
            {
                SetCharacterColor(GetReturnColor());
            }
        }
    }

    private void ShowArmorBreakEffect(bool show)
    {
        if (armorBreakVFX != null)
        {
            armorBreakVFX.SetActive(show);
        }

        if (characterRenderer != null && !isFlashingFromStatus && !isTakingDamage)
        {
            if (show)
            {
                SetCharacterColor(armorBreakColor);
            }
            else
            {
                SetCharacterColor(GetReturnColor());
            }
        }
    }

    private void ShowBlindEffect(bool show)
    {
        if (blindVFX != null)
        {
            blindVFX.SetActive(show);
        }

        if (characterRenderer != null && !isFlashingFromStatus && !isTakingDamage)
        {
            if (show)
            {
                SetCharacterColor(blindColor);
            }
            else
            {
                SetCharacterColor(GetReturnColor());
            }
        }
    }

    private void ShowWeaknessEffect(bool show)
    {
        if (weaknessVFX != null)
        {
            weaknessVFX.SetActive(show);
        }

        if (characterRenderer != null && !isFlashingFromStatus && !isTakingDamage)
        {
            if (show)
            {
                SetCharacterColor(weaknessColor);
            }
            else
            {
                SetCharacterColor(GetReturnColor());
            }
        }
    }

    private void ShowStunEffect(bool show)
    {
        if (stunVFX != null)
        {
            stunVFX.SetActive(show);
        }

        if (characterRenderer != null && !isFlashingFromStatus && !isTakingDamage)
        {
            if (show)
            {
                SetCharacterColor(stunColor);
            }
            else
            {
                SetCharacterColor(GetReturnColor());
            }
        }
    }

    private void ShowFreezeEffect(bool show)
    {
        if (freezeVFX != null)
        {
            freezeVFX.SetActive(show);
        }

        if (characterRenderer != null && !isFlashingFromStatus && !isTakingDamage)
        {
            if (show)
            {
                SetCharacterColor(freezeColor);
            }
            else
            {
                SetCharacterColor(GetReturnColor());
            }
        }
    }

    protected virtual void Start()
    {
        // Subscribe to events
        StatusEffectManager.OnStatusEffectChanged += HandleStatusEffectVisual;
        StatusEffectManager.OnStatusDamageFlash += HandleStatusDamageFlash;
        CombatManager.OnDamageTaken += HandleDamageFlash;

        // Initialize VFX
        InitializeVFX();
    }

    protected virtual void OnDestroy()
    {
        // Unsubscribe events
        StatusEffectManager.OnStatusEffectChanged -= HandleStatusEffectVisual;
        StatusEffectManager.OnStatusDamageFlash -= HandleStatusDamageFlash;
        CombatManager.OnDamageTaken -= HandleDamageFlash;
    }

    protected virtual void Update()
    {
        // Update visual effects based on network state
        UpdateVisualEffects();
    }

    // ========== VFX Initialization ==========
    private void InitializeVFX()
    {
        if (poisonVFX != null) poisonVFX.SetActive(false);
        if (burnVFX != null) burnVFX.SetActive(false);
        if (freezeVFX != null) freezeVFX.SetActive(false);
        if (stunVFX != null) stunVFX.SetActive(false);
        if (bleedVFX != null) bleedVFX.SetActive(false);
        if (armorBreakVFX != null) armorBreakVFX.SetActive(false);
        if (blindVFX != null) blindVFX.SetActive(false);
        if (weaknessVFX != null) weaknessVFX.SetActive(false);
    }

    // ========== Event Handlers ==========
    private void HandleStatusEffectVisual(Character targetCharacter, StatusEffectType effectType, bool isActive)
    {
        if (targetCharacter != character) return;

        switch (effectType)
        {
            // Magical Effects
            case StatusEffectType.Poison:
                ShowPoisonEffect(isActive);
                break;
            case StatusEffectType.Burn:
                ShowBurnEffect(isActive);
                break;
            case StatusEffectType.Bleed:
                ShowBleedEffect(isActive);
                break;
            case StatusEffectType.Freeze:
                ShowFreezeEffect(isActive);
                break;

            // Physical Effects
            case StatusEffectType.Stun:
                ShowStunEffect(isActive);
                break;
            case StatusEffectType.ArmorBreak:
                ShowArmorBreakEffect(isActive);
                break;
            case StatusEffectType.Blind:
                ShowBlindEffect(isActive);
                break;
            case StatusEffectType.Weakness:
                ShowWeaknessEffect(isActive);
                break;
        }
    }

    private void HandleStatusDamageFlash(Character targetCharacter, StatusEffectType effectType)
    {
        if (targetCharacter != character) return;

        switch (effectType)
        {
            case StatusEffectType.Poison:
                StartCoroutine(PoisonDamageFlash());
                break;
                // เพิ่ม status effects อื่นๆ ต่อไป
        }
    }

    private void HandleDamageFlash(Character targetCharacter, int damage, DamageType damageType, bool isCritical)
    {
        if (targetCharacter != character) return;

        StartCoroutine(DamageFlash(damageType, isCritical));
    }

    // ========== Status Effect Visuals ==========
    private void ShowPoisonEffect(bool show)
    {
        if (poisonVFX != null)
        {
            poisonVFX.SetActive(show);
        }

        if (characterRenderer != null && !isFlashingFromPoison && !isTakingDamage)
        {
            if (show)
            {
                SetCharacterColor(poisonColor);
            }
            else
            {
                SetCharacterColor(GetReturnColor());
            }
        }
    }

    // ========== Flash Effects ==========
    private IEnumerator DamageFlash(DamageType damageType, bool isCritical)
    {
        if (characterRenderer == null) yield break;

        isTakingDamage = true;
        Color flashColor = GetFlashColorByDamageType(damageType, isCritical);

        // Apply flash color
        SetCharacterColor(flashColor);

        // Flash duration
        float flashDuration = isCritical ? criticalFlashDuration : damageFlashDuration;
        yield return new WaitForSeconds(flashDuration);

        // Return to appropriate color
        Color returnColor = GetReturnColor();
        SetCharacterColor(returnColor);

        isTakingDamage = false;
    }

    private IEnumerator PoisonDamageFlash()
    {
        if (characterRenderer == null) yield break;

        isFlashingFromPoison = true;

        // Flash to red briefly
        SetCharacterColor(Color.red);
        yield return new WaitForSeconds(statusFlashDuration);

        // Return to poison color or normal color
        if (statusEffectManager.IsPoisoned)
        {
            SetCharacterColor(poisonColor);
        }
        else
        {
            SetCharacterColor(GetReturnColor());
        }

        isFlashingFromPoison = false;
    }

    // ========== Color Management ==========
    private Color GetFlashColorByDamageType(DamageType damageType, bool isCritical)
    {
        switch (damageType)
        {
            case DamageType.Poison:
                return new Color(0.8f, 0f, 0.8f); // สีม่วง
            case DamageType.Burn:
                return new Color(1f, 0.3f, 0f); // สีส้มแดง
            case DamageType.Freeze:
                return new Color(0.3f, 0.8f, 1f); // สีน้ำเงินอ่อน
            case DamageType.Stun:
                return new Color(1f, 1f, 0f); // สีเหลือง
            case DamageType.Bleed:
                return new Color(0.7f, 0f, 0f); // สีแดงเข้ม
            case DamageType.Critical:
                return new Color(1f, 0.8f, 0f); // สีทอง
            default:
                return isCritical ? new Color(1f, 0.5f, 0f) : Color.red;
        }
    }

    private Color GetReturnColor()
    {
        // ลำดับความสำคัญของสี (สถานะที่สำคัญกว่าจะแสดงก่อน)
        if (statusEffectManager != null)
        {
            // Physical Effects (มีผลกระทบต่อการต่อสู้มาก)
            if (statusEffectManager.IsStunned) return stunColor;        // ⚡ Stun - สำคัญที่สุด
            if (statusEffectManager.IsFrozen) return freezeColor;       // ❄️ Freeze
            if (statusEffectManager.IsBlind) return blindColor;        // 👁️ Blind
            if (statusEffectManager.IsWeak) return weaknessColor;      // 💪 Weakness
            if (statusEffectManager.IsArmorBreak) return armorBreakColor; // 🛡️ Armor Break

            // Magical Effects (damage over time)
            if (statusEffectManager.IsPoisoned) return poisonColor;    // 🧪 Poison
            if (statusEffectManager.IsBurning) return burnColor;       // 🔥 Burn
            if (statusEffectManager.IsBleeding) return bleedColor;     // 🩸 Bleed
        }

        return originalColor;
    }

    // ========== Visual Updates ==========
    private void UpdateVisualEffects()
    {
        // อัพเดท VFX ตาม network state
        if (statusEffectManager != null)
        {
            // Magical Effects VFX
            if (poisonVFX != null && poisonVFX.activeSelf != statusEffectManager.IsPoisoned)
                poisonVFX.SetActive(statusEffectManager.IsPoisoned);

            if (burnVFX != null && burnVFX.activeSelf != statusEffectManager.IsBurning)
                burnVFX.SetActive(statusEffectManager.IsBurning);

            if (bleedVFX != null && bleedVFX.activeSelf != statusEffectManager.IsBleeding)
                bleedVFX.SetActive(statusEffectManager.IsBleeding);

            if (freezeVFX != null && freezeVFX.activeSelf != statusEffectManager.IsFrozen)
                freezeVFX.SetActive(statusEffectManager.IsFrozen);

            // Physical Effects VFX
            if (stunVFX != null && stunVFX.activeSelf != statusEffectManager.IsStunned)
                stunVFX.SetActive(statusEffectManager.IsStunned);

            if (armorBreakVFX != null && armorBreakVFX.activeSelf != statusEffectManager.IsArmorBreak)
                armorBreakVFX.SetActive(statusEffectManager.IsArmorBreak);

            if (blindVFX != null && blindVFX.activeSelf != statusEffectManager.IsBlind)
                blindVFX.SetActive(statusEffectManager.IsBlind);

            if (weaknessVFX != null && weaknessVFX.activeSelf != statusEffectManager.IsWeak)
                weaknessVFX.SetActive(statusEffectManager.IsWeak);
        }

        // อัพเดทสีเฉพาะเมื่อไม่มีการ flash
        if (characterRenderer != null && !isTakingDamage && !isFlashingFromPoison && !isFlashingFromStatus)
        {
            Color targetColor = GetReturnColor();
            Color currentColor = GetCharacterColor();
            float colorDifference = Vector4.Distance(currentColor, targetColor);

            if (colorDifference > 0.1f)
            {
                SetCharacterColor(targetColor);
            }
        }
    }

    // ========== Public Methods ==========
    public void ForceUpdateColor()
    {
        if (characterRenderer != null && !isTakingDamage && !isFlashingFromPoison)
        {
            SetCharacterColor(GetReturnColor());
        }
    }

    public void SetCustomColor(Color color, float duration = 0f)
    {
        if (characterRenderer != null)
        {
            if (duration > 0f)
            {
                StartCoroutine(TemporaryColorChange(color, duration));
            }
            else
            {
                SetCharacterColor(color);
            }
        }
    }

    private IEnumerator TemporaryColorChange(Color color, float duration)
    {
        Color originalColor = GetCharacterColor();
        SetCharacterColor(color);
        yield return new WaitForSeconds(duration);
        SetCharacterColor(GetReturnColor());
    }

    // ========== Debug Methods ==========
    public void TestPoisonFlash()
    {
        StartCoroutine(PoisonDamageFlash());
    }

    public void TestDamageFlash(DamageType damageType, bool isCritical = false)
    {
        StartCoroutine(DamageFlash(damageType, isCritical));
    }
}