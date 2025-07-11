using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Fusion;


public class UpgradeLobby : MonoBehaviour
{

    [Header("Button")]
    public Button closeUpgradeLobby;

    [Header("Panel")]
    public GameObject upgradeLobbyPanel;
    [Header("Character Stats in Lobby")]
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI characterLevelText;
    public Slider inventoryHealthBar;
    public Slider inventoryManaBar;
    public TextMeshProUGUI inventoryHealthText;
    public TextMeshProUGUI inventoryManaText;
    public TextMeshProUGUI attackDamageText;
    public TextMeshProUGUI magicDamageText;
    public TextMeshProUGUI armorText;
    public TextMeshProUGUI moveSpeedText;
    public TextMeshProUGUI criticalChanceText;
    public TextMeshProUGUI criticalDamageText;
    public TextMeshProUGUI hitRateText;
    public TextMeshProUGUI evasionRateText;
    public TextMeshProUGUI attackSpeedText;
    public TextMeshProUGUI reductionCoolDownText;
    public TextMeshProUGUI liftSteal;
    public Hero localHero { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        closeUpgradeLobby.onClick.AddListener(CloseUpgradeLobby);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void CloseUpgradeLobby()
    {
        upgradeLobbyPanel.SetActive(false);
    }
    private Hero FindLocalHero()
    {
        // วิธีที่ 1: หาจาก active character ใน PersistentPlayerData
        if (PersistentPlayerData.Instance != null && PersistentPlayerData.Instance.HasValidData())
        {
            string activeCharacterType = PersistentPlayerData.Instance.GetCurrentActiveCharacter();

            // หา Hero objects ทั้งหมดในฉาก
            Hero[] allHeroes = FindObjectsOfType<Hero>();

            foreach (Hero hero in allHeroes)
            {
                // เช็คว่าเป็นตัวเดียวกับที่เลือกไว้หรือไม่
                if (hero.CharacterName == activeCharacterType ||
                    hero.name.Contains(activeCharacterType))
                {
                    Debug.Log($"[UpgradeLobby] Found matching hero: {hero.CharacterName}");
                    return hero;
                }
            }

            // ถ้าไม่เจอ ให้ใช้ hero แรกที่เจอ
            if (allHeroes.Length > 0)
            {
                Debug.LogWarning($"[UpgradeLobby] No exact match found, using first hero: {allHeroes[0].CharacterName}");
                return allHeroes[0];
            }
        }

        Debug.LogWarning("[UpgradeLobby] No hero found in scene");
        return null;
    }

    // ✅ แก้ไข OpenUpgradeLobby() ให้หา Hero เอง
    public void OpenUpgradeLobby()
    {
        Debug.Log("[UpgradeLobby] Opening upgrade lobby...");

        // หา Hero เอง
        if (localHero == null)
        {
            localHero = FindLocalHero();
        }

        // ถ้ายังไม่เจอ Hero ให้ใช้ข้อมูลจาก PersistentPlayerData
       

        // ใช้ Hero object ที่เจอได้
        UpgradeLobbyStat();
    }

    // ✅ Method สำหรับใช้ Hero object (ใช้ GetAttackSpeedMultiplierForUI())
    public void UpgradeLobbyStat()
    {
        if (localHero == null)
        {
            Debug.LogError("[UpgradeLobby] localHero is null!");
            return;
        }

        Debug.Log($"[UpgradeLobby] Updating stats from Hero: {localHero.CharacterName}");

        // Character Name & Level
        if (characterNameText != null)
        {
            characterNameText.text = localHero.CharacterName;
        }

        if (characterLevelText != null)
        {
            characterLevelText.text = $"Level {localHero.GetCurrentLevel()}";
        }

        // Health & Mana
       

        if (inventoryHealthText != null)
        {
            inventoryHealthText.text = $"HP:{localHero.NetworkedCurrentHp}/{localHero.NetworkedMaxHp}";
        }

       

        if (inventoryManaText != null)
        {
            inventoryManaText.text = $"MANA:{localHero.NetworkedCurrentMana}/{localHero.NetworkedMaxMana}";
        }

        // Combat Stats จาก Hero object
        if (attackDamageText != null)
        {
            attackDamageText.text = $"ATK: {localHero.AttackDamage}";
        }

        if (magicDamageText != null)
        {
            magicDamageText.text = $"MAG: {localHero.MagicDamage}";
        }

        if (armorText != null)
        {
            armorText.text = $"ARM: {localHero.Armor}";
        }

        if (moveSpeedText != null)
        {
            moveSpeedText.text = $"SPD: {localHero.GetEffectiveMoveSpeed():F1}";
        }

        if (criticalChanceText != null)
        {
            criticalChanceText.text = $"CRIT: {localHero.CriticalChance:F1}%";
        }

        if (criticalDamageText != null)
        {
            criticalDamageText.text = $"CRIT DMG: {localHero.GetEffectiveCriticalDamageBonus() * 100f:F1}%";
        }

        if (hitRateText != null)
        {
            hitRateText.text = $"HIT: {localHero.HitRate:F1}%";
        }

        if (evasionRateText != null)
        {
            evasionRateText.text = $"EVA: {localHero.EvasionRate:F1}%";
        }

        if (reductionCoolDownText != null)
        {
            reductionCoolDownText.text = $"CDR: {localHero.ReductionCoolDown:F1}%";
        }

        // ✅ ใช้ GetAttackSpeedMultiplierForUI() เหมือน CombatUIManager
        if (attackSpeedText != null)
        {
            float multiplier = localHero.GetAttackSpeedMultiplierForUI();
            attackSpeedText.text = $"AS: x{multiplier:F2}";
        }

        if (liftSteal != null)
        {
            float effectiveLifesteal = localHero.GetEffectiveLifeSteal();
            liftSteal.text = $"LST: {effectiveLifesteal:F1}%";
        }

        Debug.Log($"[UpgradeLobby] ✅ Stats updated successfully from Hero object");
    }

    // ✅ Method สำหรับใช้ CharacterProgressData (fallback)
   
}
