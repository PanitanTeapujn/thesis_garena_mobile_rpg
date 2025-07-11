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

    [Header("🔄 Reset Button")]
    public Button resetAllStatsButton;


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

    [Header("💰 Cost Display")]
    public TextMeshProUGUI strCostText;        // แสดงราคาอัพ STR
    public TextMeshProUGUI dexCostText;        // แสดงราคาอัพ DEX
    public TextMeshProUGUI intCostText;        // แสดงราคาอัพ INT
    public TextMeshProUGUI masCostText;        // แสดงราคาอัพ MAS

    [Header("💎 Currency Display")]
    public TextMeshProUGUI currentGoldText;    // แสดงเงินปัจจุบัน
    public TextMeshProUGUI currentGemsText;    // แสดงเพชรปัจจุบัน
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
        if (resetAllStatsButton != null)
            resetAllStatsButton.onClick.AddListener(ResetAllStats);
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
            inventoryHealthText.text = $"HP:{localHero.MaxHp}";
        }

        if (inventoryManaText != null)
        {
            inventoryManaText.text = $"MANA:{localHero.MaxMana}";
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
        UpdateCurrencyDisplay();      // 🆕 เพิ่มการแสดงเงิน

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

                // แสดงจำนวนครั้งที่อัพแต่ละ stat พร้อมราคา
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

                // 🆕 แสดงราคาการอัพเกรดแต่ละ stat
                if (strCostText != null)
                {
                    long strCost = characterData.GetStatUpgradeCost(characterData.upgradedSTR);
                    strCostText.text = $"Cost: {strCost}g";
                }

                if (dexCostText != null)
                {
                    long dexCost = characterData.GetStatUpgradeCost(characterData.upgradedDEX);
                    dexCostText.text = $"Cost: {dexCost}g";
                }

                if (intCostText != null)
                {
                    long intCost = characterData.GetStatUpgradeCost(characterData.upgradedINT);
                    intCostText.text = $"Cost: {intCost}g";
                }

                if (masCostText != null)
                {
                    long masCost = characterData.GetStatUpgradeCost(characterData.upgradedMAS);
                    masCostText.text = $"Cost: {masCost}g";
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UpgradeLobby] ❌ Error updating stat point display: {e.Message}");
        }
    }
    private void UpdateCurrencyDisplay()
    {
        try
        {
            var currencyManager = CurrencyManager.FindCurrencyManager();

            if (currencyManager != null)
            {
                if (currentGoldText != null)
                {
                    currentGoldText.text = $"{currencyManager.GetCurrentGold():N0}";
                }

                if (currentGemsText != null)
                {
                    currentGemsText.text = $"{currencyManager.GetCurrentGems():N0}";
                }
            }
            else
            {
                if (currentGoldText != null)
                {
                    currentGoldText.text = "";
                }

                if (currentGemsText != null)
                {
                    currentGemsText.text = "";
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UpgradeLobby] ❌ Error updating currency display: {e.Message}");
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

                // 🆕 อัปเดตสถานะปุ่ม reset
                if (resetAllStatsButton != null)
                {
                    resetAllStatsButton.interactable = characterData.HasUpgradedStats();
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
                int currentUpgrades = characterData.GetStatUpgrades(statType);
                long upgradeCost = characterData.GetStatUpgradeCost(currentUpgrades);

                Debug.Log($"[UpgradeLobby] 💰 Attempting to upgrade {statType} for {upgradeCost} gold");

                bool success = characterData.UpgradeStat(statType);

                if (success)
                {
                    Debug.Log($"[UpgradeLobby] ✅ Successfully upgraded {statType} for {upgradeCost} gold");

                    // 🆕 Apply stat changes to character immediately
                    ApplyStatUpgradeToCharacter(statType);

                    // อัปเดต UI ทั้งหมด
                    UpdateStatPointDisplay();
                    UpdateStatUpgradeButtons();
                    UpdateCurrencyDisplay();
                    UpgradeLobbyStat(); // Refresh character stats display

                    // บันทึกลง Firebase
                    PersistentPlayerData.Instance.SavePlayerDataAsync();

                    Debug.Log($"[UpgradeLobby] 🎯 {statType} upgrade applied successfully!");
                }
                else
                {
                    Debug.LogWarning($"[UpgradeLobby] ❌ Failed to upgrade {statType} - insufficient gold or other error");
                }
            }
            else
            {
                string reason = GetUpgradeFailureReason(characterData, statType);
                Debug.LogWarning($"[UpgradeLobby] ❌ Cannot upgrade {statType}: {reason}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UpgradeLobby] ❌ Error upgrading stat: {e.Message}");
        }
    }

    // 🆕 Method สำหรับ apply stat upgrade ไปยัง character
    private void ApplyStatUpgradeToCharacter(StatType statType)
    {
        try
        {
            if (localHero == null)
            {
                Debug.LogError("[UpgradeLobby] No local hero to apply upgrade to!");
                return;
            }

            Debug.Log($"[UpgradeLobby] 🎯 Applying {statType} upgrade to character...");

            // 🔧 ขั้นตอนที่ 1: คำนวณ base stats ใหม่ (รวม stat upgrades)
            RecalculateBaseStatsWithUpgrades();

            // 🔧 ขั้นตอนที่ 2: Apply base stats ไปยัง character
            ApplyNewBaseStatsToCharacter();

            // 🔧 ขั้นตอนที่ 3: Apply equipment bonuses ทับ
            localHero.ApplyLoadedEquipmentStats();

            // 🔧 ขั้นตอนที่ 4: บันทึก base stats และ total stats
            SaveStatsToFirebase();

            Debug.Log($"[UpgradeLobby] ✅ Applied {statType} upgrade successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UpgradeLobby] ❌ Error applying upgrade to character: {e.Message}");
        }
    }
    private void RecalculateBaseStatsWithUpgrades()
    {
        try
        {
            var characterData = PersistentPlayerData.Instance?.GetCurrentCharacterData();
            if (characterData == null || localHero?.characterStats == null)
            {
                Debug.LogError("[UpgradeLobby] Missing data for base stats calculation!");
                return;
            }

            // ดึง level bonuses จาก LevelManager
            var levelManager = localHero.GetComponent<LevelManager>();
            if (levelManager == null)
            {
                Debug.LogError("[UpgradeLobby] No LevelManager found!");
                return;
            }

            int currentLevel = levelManager.CurrentLevel;
            int levelBonus = currentLevel - 1;

            // คำนวณ base stats = ScriptableObject + Level bonuses
            int baseHp = localHero.characterStats.maxHp + (levelBonus * levelManager.levelUpStats.hpBonusPerLevel);
            int baseMana = localHero.characterStats.maxMana + (levelBonus * levelManager.levelUpStats.manaBonusPerLevel);
            int baseAttack = localHero.characterStats.attackDamage + (levelBonus * levelManager.levelUpStats.attackDamageBonusPerLevel);
            int baseMagic = localHero.characterStats.magicDamage + (levelBonus * levelManager.levelUpStats.magicDamageBonusPerLevel);
            int baseArmor = localHero.characterStats.arrmor + (levelBonus * levelManager.levelUpStats.armorBonusPerLevel);
            float baseCrit = localHero.characterStats.criticalChance + (levelBonus * levelManager.levelUpStats.criticalChanceBonusPerLevel);
            float baseCritDmg = localHero.characterStats.criticalDamageBonus;
            float baseSpeed = localHero.characterStats.moveSpeed + (levelBonus * levelManager.levelUpStats.moveSpeedBonusPerLevel);
            float baseAtkSpeed = localHero.characterStats.attackSpeed;
            float baseEvasion = localHero.characterStats.evasionRate;
            float baseCdr = localHero.characterStats.reductionCoolDown;
            float baseHit = localHero.characterStats.hitRate;
            float baseLifeSteal = localHero.characterStats.lifeSteal;

            // 🎯 เพิ่ม stat bonuses จาก upgrades
            characterData.GetStatBonuses(out int hpBonus, out int atkBonus, out float critDmgBonus,
                                       out float atkSpeedBonus, out float evaBonus, out float critChanceBonus,
                                       out int magicBonus, out int manaBonus, out float cdrBonus,
                                       out float hitBonus, out float lifeStealBonus, out float speedBonus);

            // รวม bonuses เข้ากับ base stats
            baseHp += hpBonus;
            baseMana += manaBonus;
            baseAttack += atkBonus;
            baseMagic += magicBonus;
            baseCrit += critChanceBonus;
            baseCritDmg += critDmgBonus;
            baseSpeed += speedBonus;
            baseAtkSpeed += atkSpeedBonus;
            baseEvasion += evaBonus;
            baseCdr += cdrBonus;
            baseHit += hitBonus;
            baseLifeSteal += lifeStealBonus;

            // 🔧 บันทึก base stats ใหม่ลง characterData
            characterData.UpdateBaseStats(
                baseHp, baseMana, baseAttack, baseMagic, baseArmor,
                baseCrit, baseCritDmg, baseSpeed, baseHit, baseEvasion,
                baseAtkSpeed, baseCdr, baseLifeSteal
            );

            Debug.Log($"[UpgradeLobby] 📊 Recalculated base stats with upgrades:");
            Debug.Log($"  HP: {baseHp} (+{hpBonus}), ATK: {baseAttack} (+{atkBonus}), MAGIC: {baseMagic} (+{magicBonus})");
            Debug.Log($"  CRIT: {baseCrit:F1}% (+{critChanceBonus:F1}%), CRIT_DMG: {baseCritDmg:F1}% (+{critDmgBonus:F1}%)");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UpgradeLobby] ❌ Error recalculating base stats: {e.Message}");
        }
    }

    // 🆕 Apply base stats ไปยัง character
    private void ApplyNewBaseStatsToCharacter()
    {
        try
        {
            var characterData = PersistentPlayerData.Instance?.GetCurrentCharacterData();
            if (characterData == null || !characterData.HasValidBaseStats())
            {
                Debug.LogError("[UpgradeLobby] No valid base stats to apply!");
                return;
            }

            // เก็บ HP/Mana percentage ก่อนเปลี่ยน stats
            float hpPercentage = localHero.MaxHp > 0 ? (float)localHero.CurrentHp / localHero.MaxHp : 1f;
            float manaPercentage = localHero.MaxMana > 0 ? (float)localHero.CurrentMana / localHero.MaxMana : 1f;

            // Apply base stats ไปยัง character
            localHero.MaxHp = characterData.baseMaxHp;
            localHero.MaxMana = characterData.baseMaxMana;
            localHero.AttackDamage = characterData.baseAttackDamage;
            localHero.MagicDamage = characterData.baseMagicDamage;
            localHero.Armor = characterData.baseArmor;
            localHero.CriticalChance = characterData.baseCriticalChance;
            localHero.UpdateCriticalDamageBonus(characterData.baseCriticalDamageBonus, false);
            localHero.MoveSpeed = characterData.baseMoveSpeed;
            localHero.HitRate = characterData.baseHitRate;
            localHero.EvasionRate = characterData.baseEvasionRate;
            localHero.AttackSpeed = characterData.baseAttackSpeed;
            localHero.ReductionCoolDown = characterData.baseReductionCoolDown;
            localHero.LifeSteal = characterData.baseLifeSteal;

            // ปรับ currentHp และ currentMana ตามเปอร์เซ็นต์เดิม
            localHero.CurrentHp = Mathf.RoundToInt(localHero.MaxHp * hpPercentage);
            localHero.CurrentMana = Mathf.RoundToInt(localHero.MaxMana * manaPercentage);
            localHero.CurrentHp = Mathf.Clamp(localHero.CurrentHp, 1, localHero.MaxHp);
            localHero.CurrentMana = Mathf.Clamp(localHero.CurrentMana, 0, localHero.MaxMana);

            // Force update network state
            localHero.ForceUpdateNetworkState();

            Debug.Log($"[UpgradeLobby] ✅ Applied new base stats to character");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UpgradeLobby] ❌ Error applying base stats to character: {e.Message}");
        }
    }

    // 🆕 บันทึก base stats และ total stats ลง Firebase
    private void SaveStatsToFirebase()
    {
        try
        {
            if (PersistentPlayerData.Instance?.multiCharacterData == null)
            {
                Debug.LogError("[UpgradeLobby] No multiCharacterData to save!");
                return;
            }

            string characterType = PersistentPlayerData.Instance.GetCurrentActiveCharacter();
            var characterData = PersistentPlayerData.Instance.GetOrCreateCharacterData(characterType);

            if (characterData != null)
            {
                // 🔧 บันทึก total stats (base + equipment bonuses) เหมือน SaveTotalStatsDirectly()
                characterData.UpdateTotalStats(
                    localHero.MaxHp, localHero.MaxMana, localHero.AttackDamage, localHero.MagicDamage, localHero.Armor,
                    localHero.CriticalChance, localHero.CriticalDamageBonus, localHero.MoveSpeed,
                    localHero.HitRate, localHero.EvasionRate, localHero.AttackSpeed, localHero.ReductionCoolDown, localHero.LifeSteal
                );

                // บันทึกลง Firebase
                PersistentPlayerData.Instance.SavePlayerDataAsync();

                Debug.Log($"[UpgradeLobby] 💾 Saved both base and total stats to Firebase");
                Debug.Log($"  Total stats: HP={localHero.MaxHp}, ATK={localHero.AttackDamage}, MAGIC={localHero.MagicDamage}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UpgradeLobby] ❌ Error saving stats to Firebase: {e.Message}");
        }
    }
    private void ResetAllStats()
    {
        try
        {
            var characterData = PersistentPlayerData.Instance?.GetCurrentCharacterData();

            if (characterData != null && characterData.HasUpgradedStats())
            {
                int beforePoints = characterData.availableStatPoints;
                int beforeUsed = characterData.totalStatPointsUsed;

                // Reset stats ทั้งหมด
                characterData.ResetAllStats();

                Debug.Log($"[UpgradeLobby] 🔄 Reset Stats: Points {beforePoints} → {characterData.availableStatPoints}, Used {beforeUsed} → {characterData.totalStatPointsUsed}");

                // 🆕 Apply การรีเซ็ตไปยัง character
                ApplyStatResetToCharacter();

                // อัปเดต UI
                UpdateStatPointDisplay();
                UpdateStatUpgradeButtons();
                UpdateCurrencyDisplay();
                UpgradeLobbyStat(); // Refresh character stats display

                // บันทึกลง Firebase
                PersistentPlayerData.Instance.SavePlayerDataAsync();

                Debug.Log($"[UpgradeLobby] ✅ Successfully reset all stats");
            }
            else
            {
                Debug.LogWarning($"[UpgradeLobby] ❌ No stats to reset");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UpgradeLobby] ❌ Error resetting stats: {e.Message}");
        }
    }

    // 🆕 Method สำหรับ apply การรีเซ็ต stats
    private void ApplyStatResetToCharacter()
    {
        try
        {
            if (localHero == null)
            {
                Debug.LogError("[UpgradeLobby] No local hero to apply reset to!");
                return;
            }

            Debug.Log($"[UpgradeLobby] 🔄 Applying stat reset to character...");

            // 🔧 ขั้นตอนที่ 1: คำนวณ base stats ใหม่ (ไม่มี stat upgrades)
            RecalculateBaseStatsWithUpgrades(); // method เดียวกัน แต่ upgrades จะเป็น 0 แล้ว

            // 🔧 ขั้นตอนที่ 2: Apply base stats ไปยัง character
            ApplyNewBaseStatsToCharacter();

            // 🔧 ขั้นตอนที่ 3: Apply equipment bonuses ทับ
            localHero.ApplyLoadedEquipmentStats();

            // 🔧 ขั้นตอนที่ 4: บันทึก base stats และ total stats
            SaveStatsToFirebase();

            Debug.Log($"[UpgradeLobby] ✅ Applied stat reset successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UpgradeLobby] ❌ Error applying reset to character: {e.Message}");
        }
    }
    private string GetUpgradeFailureReason(CharacterProgressData characterData, StatType statType)
    {
        if (characterData == null)
            return "No character data";

        if (characterData.availableStatPoints <= 0)
            return "No stat points available";

        if (characterData.GetStatUpgrades(statType) >= localHero.GetCurrentLevel())
            return "Max level reached for this stat";

        var currencyManager = CurrencyManager.FindCurrencyManager();
        long cost = characterData.GetStatUpgradeCost(characterData.GetStatUpgrades(statType));
        if (currencyManager == null || !currencyManager.HasEnoughGold(cost))
            return $"Insufficient gold (need {cost})";

        return "Unknown";
    }

    // Helper method สำหรับแสดงเหตุผลที่อัพเกรดไม่ได้
   

    // ✅ Method สำหรับใช้ CharacterProgressData (fallback)

}
