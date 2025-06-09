using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class CharacterVisualManager : NetworkBehaviour
{
    [Header("Visual Components")]
    public Renderer characterRenderer;
    public Color originalColor;

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

        if (characterRenderer != null)
        {
            originalColor = characterRenderer.material.color;
        }
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
                characterRenderer.material.color = burnColor;
            }
            else
            {
                characterRenderer.material.color = GetReturnColor();
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
                characterRenderer.material.color = bleedColor;
            }
            else
            {
                characterRenderer.material.color = GetReturnColor();
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
                characterRenderer.material.color = armorBreakColor;
            }
            else
            {
                characterRenderer.material.color = GetReturnColor();
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
                characterRenderer.material.color = blindColor;
            }
            else
            {
                characterRenderer.material.color = GetReturnColor();
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
                characterRenderer.material.color = weaknessColor;
            }
            else
            {
                characterRenderer.material.color = GetReturnColor();
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
                characterRenderer.material.color = stunColor;
            }
            else
            {
                characterRenderer.material.color = GetReturnColor();
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
                characterRenderer.material.color = freezeColor;
            }
            else
            {
                characterRenderer.material.color = GetReturnColor();
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
                characterRenderer.material.color = poisonColor;
            }
            else
            {
                characterRenderer.material.color = GetReturnColor();
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
        characterRenderer.material.color = flashColor;

        // Flash duration
        float flashDuration = isCritical ? criticalFlashDuration : damageFlashDuration;
        yield return new WaitForSeconds(flashDuration);

        // Return to appropriate color
        Color returnColor = GetReturnColor();
        characterRenderer.material.color = returnColor;

        isTakingDamage = false;
    }

    private IEnumerator PoisonDamageFlash()
    {
        if (characterRenderer == null) yield break;

        isFlashingFromPoison = true;

        // Flash to red briefly
        characterRenderer.material.color = Color.red;
        yield return new WaitForSeconds(statusFlashDuration);

        // Return to poison color or normal color
        if (statusEffectManager.IsPoisoned)
        {
            characterRenderer.material.color = poisonColor;
        }
        else
        {
            characterRenderer.material.color = GetReturnColor();
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
            float colorDifference = Vector4.Distance(characterRenderer.material.color, targetColor);

            if (colorDifference > 0.1f)
            {
                characterRenderer.material.color = targetColor;
            }
        }
    }

    // ========== Public Methods ==========
    public void ForceUpdateColor()
    {
        if (characterRenderer != null && !isTakingDamage && !isFlashingFromPoison)
        {
            characterRenderer.material.color = GetReturnColor();
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
                characterRenderer.material.color = color;
            }
        }
    }

    private IEnumerator TemporaryColorChange(Color color, float duration)
    {
        Color originalColor = characterRenderer.material.color;
        characterRenderer.material.color = color;
        yield return new WaitForSeconds(duration);
        characterRenderer.material.color = GetReturnColor();
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