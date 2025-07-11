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


    [Header("🎯 Stat Point System")]
    public TextMeshProUGUI availableStatPointsText;
    public TextMeshProUGUI totalStatPointsEarnedText;

    [Header("📈 Stat Upgrade Buttons")]
    public Button upgradeSTRButton;
    public Button upgradeDEXButton;
    public Button upgradeINTButton;
    public Button upgradeMASButton;

    [Header("📊 Stat Upgrade Displays")]
    public TextMeshProUGUI strUpgradeText;      // แสดงจำนวนครั้งที่อัพ STR
    public TextMeshProUGUI dexUpgradeText;      // แสดงจำนวนครั้งที่อัพ DEX
    public TextMeshProUGUI intUpgradeText;      // แสดงจำนวนครั้งที่อัพ INT
    public TextMeshProUGUI masUpgradeText;      // แสดงจำนวนครั้งที่อัพ MAS


    // Start is called before the first frame update
    void Start()
    {
        closeUpgradeLobby.onClick.AddListener(CloseUpgradeLobby);
        if (upgradeSTRButton != null)
            upgradeSTRButton.onClick.AddListener(() => UpgradeStat(StatType.STR));

        if (upgradeDEXButton != null)
            upgradeDEXButton.onClick.AddListener(() => UpgradeStat(StatType.DEX));

        if (upgradeINTButton != null)
            upgradeINTButton.onClick.AddListener(() => UpgradeStat(StatType.INT));

        if (upgradeMASButton != null)
            upgradeMASButton.onClick.AddListener(() => UpgradeStat(StatType.MAS));
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

        // 🆕 อัปเดต stat point information
        UpdateStatPointDisplay();
        UpdateStatUpgradeButtons();

        Debug.Log($"[UpgradeLobby] ✅ Stats updated successfully from Hero object");
    }
    private void UpdateStatPointDisplay()
    {
        try
        {
            var characterData = PersistentPlayerData.Instance?.GetCurrentCharacterData();

            if (characterData != null)
            {
                // แสดง available stat points
                if (availableStatPointsText != null)
                {
                    availableStatPointsText.text = $"Available: {characterData.availableStatPoints}";
                }

                // แสดง total earned stat points
                if (totalStatPointsEarnedText != null)
                {
                    totalStatPointsEarnedText.text = $"Total Earned: {characterData.totalStatPointsEarned}";
                }

                // แสดงจำนวนครั้งที่อัพแต่ละ stat
                if (strUpgradeText != null)
                {
                    strUpgradeText.text = $"STR: {characterData.upgradedSTR}/{localHero.GetCurrentLevel()}";
                }

                if (dexUpgradeText != null)
                {
                    dexUpgradeText.text = $"DEX: {characterData.upgradedDEX}/{localHero.GetCurrentLevel()}";
                }

                if (intUpgradeText != null)
                {
                    intUpgradeText.text = $"INT: {characterData.upgradedINT}/{localHero.GetCurrentLevel()}";
                }

                if (masUpgradeText != null)
                {
                    masUpgradeText.text = $"MAS: {characterData.upgradedMAS}/{localHero.GetCurrentLevel()}";
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UpgradeLobby] ❌ Error updating stat point display: {e.Message}");
        }
    }

    // 🆕 Method สำหรับอัปเดตสถานะปุ่ม upgrade
    private void UpdateStatUpgradeButtons()
    {
        try
        {
            var characterData = PersistentPlayerData.Instance?.GetCurrentCharacterData();

            if (characterData != null)
            {
                // อัปเดตสถานะปุ่มตามเงื่อนไข
                if (upgradeSTRButton != null)
                {
                    upgradeSTRButton.interactable = characterData.CanUpgradeStat(StatType.STR);
                }

                if (upgradeDEXButton != null)
                {
                    upgradeDEXButton.interactable = characterData.CanUpgradeStat(StatType.DEX);
                }

                if (upgradeINTButton != null)
                {
                    upgradeINTButton.interactable = characterData.CanUpgradeStat(StatType.INT);
                }

                if (upgradeMASButton != null)
                {
                    upgradeMASButton.interactable = characterData.CanUpgradeStat(StatType.MAS);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UpgradeLobby] ❌ Error updating upgrade buttons: {e.Message}");
        }
    }

    // 🆕 Method สำหรับ upgrade stat (ยังไม่ implement การเพิ่ม stat จริง)
    private void UpgradeStat(StatType statType)
    {
        try
        {
            var characterData = PersistentPlayerData.Instance?.GetCurrentCharacterData();

            if (characterData != null && characterData.CanUpgradeStat(statType))
            {
                bool success = characterData.UpgradeStat(statType);

                if (success)
                {
                    Debug.Log($"[UpgradeLobby] ✅ Successfully upgraded {statType}");

                    // อัปเดต UI
                    UpdateStatPointDisplay();
                    UpdateStatUpgradeButtons();

                    // บันทึกลง Firebase
                    PersistentPlayerData.Instance.SavePlayerDataAsync();

                    // TODO: ขั้นตอนที่ 4 - เพิ่ม stat จริงให้ตัวละคร
                }
                else
                {
                    Debug.LogWarning($"[UpgradeLobby] ❌ Failed to upgrade {statType}");
                }
            }
            else
            {
                Debug.LogWarning($"[UpgradeLobby] ❌ Cannot upgrade {statType} - insufficient points or max level reached");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UpgradeLobby] ❌ Error upgrading stat: {e.Message}");
        }
    }

    // ✅ Method สำหรับใช้ CharacterProgressData (fallback)

}
