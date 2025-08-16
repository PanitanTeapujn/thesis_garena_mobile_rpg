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

    protected virtual void Start()
    {
        // Subscribe to events
        StatusEffectManager.OnStatusEffectChanged += HandleStatusEffectVisual;
        StatusEffectManager.OnStatusDamageFlash += HandleStatusDamageFlash;
        CombatManager.OnDamageTaken += HandleDamageFlash;

        // Initialize VFX
        InitializeVFX();
    }

    private void InitializeColorSystem()
    {
        // หา SpriteRenderer
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            // ✅ ไม่เปลี่ยน Material อัตโนมัติ - ให้ผู้ใช้เปลี่ยนเอง
            originalColor = spriteRenderer.color;
            Debug.Log($"Using SpriteRenderer Color system - Original: {originalColor}");
            Debug.Log($"Current Material: {spriteRenderer.material?.name ?? "Default"}");
            Debug.Log($"Note: Make sure to use a material that supports vertex colors (like Sprites-Default)");
        }
        else
        {
            Debug.LogError("No SpriteRenderer found! Cannot change colors.");
        }
    }

    private void SetCharacterColor(Color color)
    {
        Debug.Log($"[SetCharacterColor] Setting color to: {color} for {character?.CharacterName}");

        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
            Debug.Log($"[SetCharacterColor] Color set to: {spriteRenderer.color}");
        }
        else
        {
            Debug.LogError($"[SetCharacterColor] spriteRenderer is null!");
        }
    }

    private Color GetCharacterColor()
    {
        if (spriteRenderer != null)
        {
            return spriteRenderer.color;
        }
        return originalColor;
    }

    // ========== Status Effect Visual Methods ==========
    private void ShowPoisonEffect(bool show)
    {
        if (poisonVFX != null)
        {
            poisonVFX.SetActive(show);
        }

        if (spriteRenderer != null && !isFlashingFromPoison && !isTakingDamage)
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

    private void ShowBurnEffect(bool show)
    {
        if (burnVFX != null)
        {
            burnVFX.SetActive(show);
        }

        if (spriteRenderer != null && !isFlashingFromStatus && !isTakingDamage)
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

        if (spriteRenderer != null && !isFlashingFromStatus && !isTakingDamage)
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

        if (spriteRenderer != null && !isFlashingFromStatus && !isTakingDamage)
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

        if (spriteRenderer != null && !isFlashingFromStatus && !isTakingDamage)
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

        if (spriteRenderer != null && !isFlashingFromStatus && !isTakingDamage)
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

        if (spriteRenderer != null && !isFlashingFromStatus && !isTakingDamage)
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

        if (spriteRenderer != null && !isFlashingFromStatus && !isTakingDamage)
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

    // ========== Event Handlers ==========
    private void HandleStatusEffectVisual(Character targetCharacter, StatusEffectType effectType, bool isActive)
    {
        if (targetCharacter != character) return;

        switch (effectType)
        {
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
        }
    }

    private void HandleDamageFlash(Character targetCharacter, int damage, DamageType damageType, bool isCritical)
    {
        if (targetCharacter != character) return;

        Debug.Log($"[HandleDamageFlash] Starting damage flash for {character.CharacterName}");
        StartCoroutine(DamageFlash(damageType, isCritical));
    }

    // ========== Flash Effects ==========
    private IEnumerator DamageFlash(DamageType damageType, bool isCritical)
    {
        if (spriteRenderer == null) yield break;

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
        if (spriteRenderer == null) yield break;

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
                return new Color(0.8f, 0f, 0.8f);
            case DamageType.Burn:
                return new Color(1f, 0.3f, 0f);
            case DamageType.Freeze:
                return new Color(0.3f, 0.8f, 1f);
            case DamageType.Stun:
                return new Color(1f, 1f, 0f);
            case DamageType.Bleed:
                return new Color(0.7f, 0f, 0f);
            case DamageType.Critical:
                return new Color(1f, 0.8f, 0f);
            default:
                return isCritical ? new Color(1f, 0.5f, 0f) : Color.red;
        }
    }

    private Color GetReturnColor()
    {
        if (statusEffectManager != null)
        {
            if (statusEffectManager.IsStunned) return stunColor;
            if (statusEffectManager.IsFrozen) return freezeColor;
            if (statusEffectManager.IsBlind) return blindColor;
            if (statusEffectManager.IsWeak) return weaknessColor;
            if (statusEffectManager.IsArmorBreak) return armorBreakColor;
            if (statusEffectManager.IsPoisoned) return poisonColor;
            if (statusEffectManager.IsBurning) return burnColor;
            if (statusEffectManager.IsBleeding) return bleedColor;
        }

        return originalColor;
    }

    // ========== VFX & Updates ==========
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

    protected virtual void Update()
    {
        UpdateVisualEffects();
    }

    private void UpdateVisualEffects()
    {
        if (statusEffectManager != null)
        {
            // Update VFX states
            if (poisonVFX != null && poisonVFX.activeSelf != statusEffectManager.IsPoisoned)
                poisonVFX.SetActive(statusEffectManager.IsPoisoned);

            if (burnVFX != null && burnVFX.activeSelf != statusEffectManager.IsBurning)
                burnVFX.SetActive(statusEffectManager.IsBurning);

            if (bleedVFX != null && bleedVFX.activeSelf != statusEffectManager.IsBleeding)
                bleedVFX.SetActive(statusEffectManager.IsBleeding);

            if (freezeVFX != null && freezeVFX.activeSelf != statusEffectManager.IsFrozen)
                freezeVFX.SetActive(statusEffectManager.IsFrozen);

            if (stunVFX != null && stunVFX.activeSelf != statusEffectManager.IsStunned)
                stunVFX.SetActive(statusEffectManager.IsStunned);

            if (armorBreakVFX != null && armorBreakVFX.activeSelf != statusEffectManager.IsArmorBreak)
                armorBreakVFX.SetActive(statusEffectManager.IsArmorBreak);

            if (blindVFX != null && blindVFX.activeSelf != statusEffectManager.IsBlind)
                blindVFX.SetActive(statusEffectManager.IsBlind);

            if (weaknessVFX != null && weaknessVFX.activeSelf != statusEffectManager.IsWeak)
                weaknessVFX.SetActive(statusEffectManager.IsWeak);
        }

        // Update colors when not flashing
        if (spriteRenderer != null && !isTakingDamage && !isFlashingFromPoison && !isFlashingFromStatus)
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

    protected virtual void OnDestroy()
    {
        // Unsubscribe events
        StatusEffectManager.OnStatusEffectChanged -= HandleStatusEffectVisual;
        StatusEffectManager.OnStatusDamageFlash -= HandleStatusDamageFlash;
        CombatManager.OnDamageTaken -= HandleDamageFlash;
    }

    // ========== Debug & Testing Methods ==========
    [ContextMenu("🔴 Test Red Color")]
    private void TestRedColor()
    {
        Debug.Log($"[TEST] Testing red color for {character?.CharacterName}");
        SetCharacterColor(Color.red);
    }

    [ContextMenu("⚪ Reset Color")]
    private void TestResetColor()
    {
        Debug.Log($"[TEST] Resetting color for {character?.CharacterName}");
        SetCharacterColor(originalColor);
    }

    [ContextMenu("🔥 Test Damage Flash")]
    private void TestDamageFlash()
    {
        Debug.Log($"[TEST] Testing damage flash for {character?.CharacterName}");
        StartCoroutine(DamageFlash(DamageType.Normal, false));
    }

    // ========== Public Methods ==========
    public void ForceUpdateColor()
    {
        if (spriteRenderer != null && !isTakingDamage && !isFlashingFromPoison)
        {
            SetCharacterColor(GetReturnColor());
        }
    }

    public void SetCustomColor(Color color, float duration = 0f)
    {
        if (spriteRenderer != null)
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
}